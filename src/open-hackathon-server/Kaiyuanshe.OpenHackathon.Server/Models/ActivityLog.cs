﻿namespace Kaiyuanshe.OpenHackathon.Server.Models
{
    /// <summary>
    /// activity logs
    /// </summary>
    public class ActivityLog : ModelBase
    {
        /// <summary>
        /// name of hackathon
        /// </summary>
        /// <example>foo</example>
        public string hackathonName { get; internal set; }

        /// <summary>
        /// id of user who performs the operation.
        /// </summary>
        /// <example>1</example>
        public string userId { get; internal set; }

        /// <summary>
        /// The user id on whom the operation perferms.
        /// </summary>
        /// <example>2</example>
        public string correlatedUserId { get; internal set; }

        /// <summary>
        /// auto-generated activity log id.
        /// </summary>
        /// <example>323fed7f-447e-4c2e-854c-1421e2439208</example>
        public string activityId { get; internal set; }

        /// <summary>
        /// Key message related to the activity.
        /// </summary>
        /// <example>Something happens.</example>
        public string message { get; set; }

        /// <summary>
        /// type of the activity log.
        /// </summary>
        public ActivityLogType activityLogType { get; internal set; }
    }

    public enum ActivityLogType
    {
        // hackathon Admin
        createHackathonAdmin,
        deleteHackathonAdmin,

        // Award
        createAward,
        updateAward,
        deleteAward,

        // login
        login,
    }
}