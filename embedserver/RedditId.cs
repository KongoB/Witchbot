using System.Globalization;

namespace embedserver
{
    /// <summary>
    /// Discord only accepts all-digit status IDs, and Twitter and Reddit share the same status routes.
    /// A Reddit post ID ("1x97vel") is a base-36 number, so we convert it to an ordinary number and add
    /// 9,000,000,000,000,000,000. The result is 19 digits starting with 9, which no tweet ID will reach
    /// for decades, so the shared routes can tell the two sites apart.
    /// </summary>
    public static class RedditId
    {
        private const long Offset = 9_000_000_000_000_000_000;
        private const long MaxPostNumber = 200_000_000_000_000_000;   // keeps Offset + number inside a 64-bit long
        private const string Digits = "0123456789abcdefghijklmnopqrstuvwxyz";

        /// <summary>"1x97vel" -> "9000000004187649981". Returns null if the ID isn't valid base 36.</summary>
        public static string? ToStatusId(string postId)
        {
            if (string.IsNullOrEmpty(postId)) return null;

            long number = 0;
            foreach (char c in postId.ToLowerInvariant())
            {
                int digit = Digits.IndexOf(c);
                if (digit < 0) return null;

                number = number * 36 + digit;
                if (number > MaxPostNumber) return null;
            }

            return (Offset + number).ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>The reverse. Returns false for anything that isn't one of our Reddit status IDs (e.g. a tweet ID).</summary>
        public static bool TryToPostId(string statusId, out string postId)
        {
            postId = "";
            if (statusId.Length != 19 || statusId[0] != '9') return false;
            if (!long.TryParse(statusId, NumberStyles.None, CultureInfo.InvariantCulture, out long value)) return false;

            long number = value - Offset;
            if (number <= 0 || number > MaxPostNumber) return false;

            var chars = new Stack<char>();
            while (number > 0)
            {
                chars.Push(Digits[(int)(number % 36)]);
                number /= 36;
            }

            postId = new string(chars.ToArray());
            return true;
        }
    }
}
