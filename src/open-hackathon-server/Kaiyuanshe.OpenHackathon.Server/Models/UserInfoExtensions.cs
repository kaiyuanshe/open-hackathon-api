namespace Kaiyuanshe.OpenHackathon.Server.Models
{
    public static class UserInfoExtensions
    {
        public static string GetDisplayName(this UserInfo userInfo)
        {
            if (userInfo == null)
                return string.Empty;

            return userInfo.Nickname ?? userInfo.Username ?? userInfo.Name ?? userInfo.Email ?? userInfo.Id;
        }
    }
}
