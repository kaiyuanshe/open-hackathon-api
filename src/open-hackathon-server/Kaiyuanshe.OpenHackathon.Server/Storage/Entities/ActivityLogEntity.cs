namespace Kaiyuanshe.OpenHackathon.Server.Storage.Entities
{
    /// <summary>
    /// activity logs of user and hackathon
    /// PK: userId or hackathon name
    /// RK: Guid
    /// </summary>
    public class ActivityLogEntity : BaseTableEntity
    {
        public string HackathonName { get; set; }

        public string UserId { get; set; }

        /// <summary>
        /// activity log id. RowKey.
        /// </summary>
        [IgnoreEntityProperty]
        public string ActivityId
        {
            get { return RowKey; }
        }

        public ActivityLogCategory Category { get; set; }

        /// <summary>
        /// type of the activity log. <see cref="ActivityLogType"/>
        /// </summary>
        public string ActivityLogType { get; set; }

        public ActivityLogEntity Clone()
        {
            return (ActivityLogEntity)MemberwiseClone();
        }
    }

    public enum ActivityLogCategory
    {
        Hackathon,
        User,
    }

    public static class ActivityLogType
    {
        public const string Login = "Login";
    }
}
