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

        /// <summary>
        /// User who performs the operation.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// The user id on whom the operation perferms.
        /// </summary>
        public string CorrelatedUserId { get; set; }

        /// <summary>
        /// auto-generated activity log id. RowKey. Any input will be ignored.
        /// </summary>
        [IgnoreEntityProperty]
        public string ActivityId
        {
            get { return RowKey; }
        }

        /// <summary>
        /// No need to specify for now.
        /// </summary>
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
}
