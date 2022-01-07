using System.Collections.Generic;

namespace Kaiyuanshe.OpenHackathon.Server.Storage.Entities
{
    /// <summary>
    /// activity logs of user
    /// PK: userId
    /// RK: Guid
    /// </summary>
    public class UserActivityLogEntity : BaseTableEntity
    {
        /// <summary>
        /// user Id. PartitionKey.
        /// </summary>
        [IgnoreEntityProperty]
        public string UserId
        {
            get { return PartitionKey; }
        }

        /// <summary>
        /// activity log id. RowKey.
        /// </summary>
        [IgnoreEntityProperty]
        public string ActivityId
        {
            get { return RowKey; }
        }

        /// <summary>
        /// type of the activity log. <see cref="ActivityLogType"/>
        /// </summary>
        public string ActivityLogType { get; set; }
    }

    public static class ActivityLogType
    {
        public const string Login = "Login";
    }
}
