﻿namespace Kaiyuanshe.OpenHackathon.Server.Storage.Entities
{
    /// <summary>
    /// entity for ratings. 
    /// PK: hackathon name. 
    /// RK: auto-generated Guid.
    /// </summary>
    public class RatingEntity : BaseTableEntity
    {
        /// <summary>
        /// name of Hackathon, PartitionKey
        /// </summary>
        [IgnoreEntityProperty]
        public string HackathonName
        {
            get
            {
                return PartitionKey;
            }
        }

        /// <summary>
        /// Auto-generated Guid of the rating. RowKey.
        /// </summary>
        [IgnoreEntityProperty]
        public string Id
        {
            get
            {
                return RowKey;
            }
        }

        /// <summary>
        /// user id of judge
        /// </summary>
        public string JudgeId { get; set; }

        /// <summary>
        /// id of rating kind
        /// </summary>
        public string RatingKindId { get; set; }

        /// <summary>
        /// id of the team.
        /// </summary>
        public string TeamId { get; set; }

        /// <summary>
        /// rating(score). an integer between 0 and allowed maximum value.
        /// </summary>
        public int Score { get; set; }

        /// <summary>
        /// description of the rating
        /// </summary>
        /// <example>amazing team</example>
        public string Description { get; set; }
    }
}
