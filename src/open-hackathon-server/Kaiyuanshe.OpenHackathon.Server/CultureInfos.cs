using System.Globalization;

namespace Kaiyuanshe.OpenHackathon.Server
{
    public static class CultureInfos
    {
        public static CultureInfo en_US = new CultureInfo("en-US");
        public static CultureInfo zh_CN = new CultureInfo("zh-CN");

        public static CultureInfo[] SupportedCultures = new CultureInfo[]
        {
            en_US,
            zh_CN,
        };
    }
}
