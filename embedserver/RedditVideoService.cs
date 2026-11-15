using System.Collections.Concurrent;
using System.Diagnostics;

namespace embedserver
{
    /// <summary>
    /// Reddit stores a video's picture and sound as separate streams. This class has ffmpeg join them into
    /// one mp4, keeps the result on disk, and hands back the file path. The folder is only a cache: any file
    /// can be deleted at any time and will simply be rebuilt the next time someone asks for it.
    /// </summary>
    public class RedditVideoService
    {
        public const int MaxDurationSeconds = 300;                             // longer videos keep Reddit's silent file
        private const long MaxCacheBytes = 20L * 1024 * 1024 * 1024;           // 20 GB
        private static readonly TimeSpan MergeTimeout = TimeSpan.FromSeconds(90);
        private static readonly TimeSpan RetryAfterFailure = TimeSpan.FromMinutes(10);

        private readonly string _dir = Path.Combine(Path.GetTempPath(), "redditvideos");
        private readonly SemaphoreSlim _slots = new(2);                        // at most 2 ffmpeg processes at once
        private readonly ConcurrentDictionary<string, Lazy<Task<bool>>> _inFlight = new();
        private readonly ConcurrentDictionary<string, DateTime> _failedUntil = new();

        public RedditVideoService()
        {
            Directory.CreateDirectory(_dir);
        }

        /// <summary>True when this post has a video with sound that we are willing to merge.</summary>
        public static bool NeedsMerge(RedditPost post)
        {
            RedditVideo? video = post.Video;
            return video != null && video.HasAudio && !video.IsGif
                && video.Duration <= MaxDurationSeconds
                && (video.DashUrl != null || video.HlsUrl != null);
        }

        /// <summary>
        /// Returns the path of the merged file, building it first if needed. Returns null when there is
        /// nothing to merge or the merge failed (never throws) - callers then fall back to the silent file.
        /// If several requests ask for the same video at once, they all wait for the same single merge.
        /// </summary>
        public async Task<string?> GetOrCreateAsync(RedditPost post)
        {
            if (!NeedsMerge(post)) return null;

            string path = Path.Combine(_dir, post.Id + ".mp4");
            if (File.Exists(path))
            {
                try { File.SetLastAccessTimeUtc(path, DateTime.UtcNow); } catch { }   // marks it as recently used
                return path;
            }

            if (_failedUntil.TryGetValue(post.Id, out DateTime until) && until > DateTime.UtcNow) return null;

            Lazy<Task<bool>> merge = _inFlight.GetOrAdd(post.Id, _ => new Lazy<Task<bool>>(() => MergeAsync(post, path)));
            bool ok;
            try
            {
                ok = await merge.Value;
            }
            finally
            {
                _inFlight.TryRemove(post.Id, out _);
            }

            if (!ok) _failedUntil[post.Id] = DateTime.UtcNow + RetryAfterFailure;
            return ok ? path : null;
        }

        private async Task<bool> MergeAsync(RedditPost post, string path)
        {
            // Two descriptions of the same streams; try the second if ffmpeg can't read the first.
            var sources = new[] { post.Video!.DashUrl, post.Video.HlsUrl }
                .Where(url => url != null && url.StartsWith("https://v.redd.it/"))
                .ToList();

            await _slots.WaitAsync();
            try
            {
                foreach (string? source in sources)
                {
                    if (await RunFfmpegAsync(post.Id, source!, path))
                    {
                        TrimCache();
                        return true;
                    }
                }
                return false;
            }
            finally
            {
                _slots.Release();
            }
        }

        private async Task<bool> RunFfmpegAsync(string postId, string source, string path)
        {
            // Write to a temporary name first, so a half-finished file is never served.
            string tmp = Path.Combine(_dir, $"{postId}.{Guid.NewGuid():N}.tmp.mp4");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    RedirectStandardError = true,
                    UseShellExecute = false
                };

                // ArgumentList passes each value as its own argument - nothing is ever interpreted by a shell.
                // "-c copy" joins the streams without re-encoding; "+faststart" lets playback begin immediately.
                foreach (string arg in new[] { "-hide_banner", "-loglevel", "error", "-y", "-i", source,
                                               "-c", "copy", "-movflags", "+faststart", "-f", "mp4", tmp })
                {
                    psi.ArgumentList.Add(arg);
                }

                using Process? process = Process.Start(psi);
                if (process == null) return false;

                // Read ffmpeg's messages while it runs; if nobody reads them, ffmpeg can freeze when the pipe fills up.
                Task<string> errorOutput = process.StandardError.ReadToEndAsync();

                using var timeout = new CancellationTokenSource(MergeTimeout);
                try
                {
                    await process.WaitForExitAsync(timeout.Token);
                }
                catch (OperationCanceledException)
                {
                    try { process.Kill(entireProcessTree: true); } catch { }
                    Console.WriteLine($"Reddit video {postId}: ffmpeg timed out and was stopped");
                    return false;
                }

                string errors = await errorOutput;
                if (process.ExitCode != 0 || !File.Exists(tmp) || new FileInfo(tmp).Length == 0)
                {
                    string shortErrors = errors.Length > 300 ? errors.Substring(0, 300) : errors;
                    Console.WriteLine($"Reddit video {postId}: ffmpeg failed ({process.ExitCode}): {shortErrors.Trim()}");
                    return false;
                }

                long megabytes = new FileInfo(tmp).Length / (1024 * 1024);
                File.Move(tmp, path, overwrite: true);
                Console.WriteLine($"Reddit video {postId}: merged {megabytes} MB in {stopwatch.Elapsed.TotalSeconds:0.0}s");
                return true;
            }
            catch (Exception e)
            {
                // e.g. ffmpeg isn't installed on this machine
                Console.WriteLine($"Reddit video {postId}: {e.GetType().Name}: {e.Message}");
                return false;
            }
            finally
            {
                try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
            }
        }

        /// <summary>Deletes the least recently used videos until the folder is back under the size limit.</summary>
        private void TrimCache()
        {
            try
            {
                var files = new DirectoryInfo(_dir).GetFiles("*.mp4")
                    .Where(f => !f.Name.EndsWith(".tmp.mp4"))
                    .OrderBy(f => f.LastAccessTimeUtc)
                    .ToList();

                long total = files.Sum(f => f.Length);
                foreach (FileInfo file in files)
                {
                    if (total <= MaxCacheBytes) break;
                    total -= file.Length;
                    file.Delete();
                    Console.WriteLine($"Reddit video cache: removed {file.Name}");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Reddit video cache cleanup failed: {e.Message}");
            }
        }
    }
}
