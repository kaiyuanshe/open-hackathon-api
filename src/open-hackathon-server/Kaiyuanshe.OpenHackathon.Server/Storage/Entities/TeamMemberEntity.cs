using Kaiyuanshe.OpenHackathon.Server.Models;

namespace Kaiyuanshe.OpenHackathon.Server.Storage.Entities
{
    /// <summary>
    /// Represents a team member.
    /// 
    /// PK: Hackathon name.
    /// RK: UserId.
    /// </summary>
    public class TeamMemberEntity : BaseTableEntity
    {
        /// <summary>
        /// PartitionKey.
        /// </summary>
        [IgnoreEntityProperty]
        public string HackathonName
        {
            get
            {
                return PartitionKey;
            }
        }

        [IgnoreEntityProperty]
        public string UserId
        {
            get
            {
                return RowKey;
            }
        }

        public string TeamId { get; set; }

        public string Description { get; set; }

        public TeamMemberRole Role { get; set; }

        public TeamMemberStatus Status { get; set; }
    }
}
