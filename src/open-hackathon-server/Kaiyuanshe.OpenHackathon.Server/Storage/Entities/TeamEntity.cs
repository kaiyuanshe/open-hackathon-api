namespace Kaiyuanshe.OpenHackathon.Server.Storage.Entities
{
    /// <summary>
    /// entity for team information
    /// 
    /// PK: hackathon name
    /// RK: auto-generated GUID
    /// </summary>
    public class TeamEntity : BaseTableEntity
    {
        /// <summary>
        /// name of Hackathon
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
        /// id of team, RowKey.
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
        /// team display name
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// description about the team
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// whether or not auto approve team joining request.
        /// </summary>
        public bool AutoApprove { get; set; }

        /// <summary>
        /// the id of user who creates the team
        /// </summary>
        public string CreatorId { get; set; }

        /// <summary>
        /// Count of members
        /// </summary>
        public int MembersCount { get; set; }
    }
}
