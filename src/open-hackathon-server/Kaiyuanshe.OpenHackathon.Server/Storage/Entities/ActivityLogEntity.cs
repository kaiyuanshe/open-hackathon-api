﻿using System;
using System.Collections.Generic;

namespace Kaiyuanshe.OpenHackathon.Server.Storage.Entities
{
    /// <summary>
    /// activity logs of user and hackathon
    /// PK: userId or hackathon name
    /// RK: Generated by <see cref="StorageUtils.InversedTimeKey(System.DateTime)"/> plus a random string from guid.
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
        [Obsolete]
        public string CorrelatedUserId { get; set; }

        /// <summary>
        /// related team id. might be null.
        /// </summary>
        [Obsolete]
        public string TeamId { get; set; }

        /// <summary>
        /// auto-generated. RowKey.  Any input will be ignored.
        /// </summary>
        [IgnoreEntityProperty]
        public string ActivityId => RowKey;

        /// <summary>
        /// No need to specify for now.
        /// </summary>
        public ActivityLogCategory Category { get; set; }

        /// <summary>
        /// type of the activity log. <see cref="ActivityLogType"/>
        /// </summary>
        public string ActivityLogType { get; set; }

        /// <summary>
        /// Args to format the message.
        /// </summary>
        [ConvertableEntityProperty]
        [Obsolete]
        public string[] Args { get; set; } = new string[0];

        /// <summary>
        /// Default message of the activity, will used if cannot find the message in Resource file.
        /// </summary>
        [Obsolete]
        public string Message { get; set; }

        /// <summary>
        /// Messages for all cultures. Use CultureInfo.Name as key.
        /// </summary>
        [ConvertableEntityProperty]
        public Dictionary<string, string> Messages { get; set; } = new();

        [Obsolete]
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
