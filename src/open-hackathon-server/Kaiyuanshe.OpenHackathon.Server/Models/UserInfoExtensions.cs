namespace Kaiyuanshe.OpenHackathon.Server.Models
{
    public static class UserInfoExtensions
    {
        public static string ActivitLogName(this UserInfo user)
        {
            if (user == null)
                return string.Empty;

            return user.Nickname ?? user.Username ?? user.Name ?? user.Email ?? user.Id;
        }
    }
}
