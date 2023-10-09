namespace vxtwitterbot.Functions
{
    public class ConvertLink
    {

        public static string TwitterLink(string link)
        {
            string vxtwitterlink;

            if (link.Contains("twitter.com"))
            {
                vxtwitterlink = link.Replace("twitter.com", "vxtwitter.com");
            }
            else
            {
                vxtwitterlink = link.Replace("x.com", "vxtwitter.com");
            }

            return vxtwitterlink;
        }

    }
}
