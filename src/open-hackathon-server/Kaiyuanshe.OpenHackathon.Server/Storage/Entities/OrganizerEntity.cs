﻿using Kaiyuanshe.OpenHackathon.Server.Models;

namespace Kaiyuanshe.OpenHackathon.Server.Storage.Entities
{
    /// <summary>
    /// Entity for hackathon organizers.
    /// 
    /// PK: hackathonName; 
    /// RK: Guid
    /// </summary>
    public class OrganizerEntity : BaseTableEntity
    {
        /// <summary>
        /// name of Hackathon. PartitionKey
        /// </summary>
        [IgnoreEntityProperty]
        public string HackathonName
        {
            get { return PartitionKey; }
        }

        /// <summary>
        /// id of organizer. RowKey
        /// </summary>
        [IgnoreEntityProperty]
        public string Id
        {
            get
            {
                return RowKey;
            }
        }

        public string Name { get; set; }
        public string Description { get; set; }
        public OrganizerType Type { get; set; }
        [ConvertableEntityProperty]
        public PictureInfo Logo { get; set; }
        public string Url { get; set; }
    }
}
