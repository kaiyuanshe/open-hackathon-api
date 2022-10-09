using Kaiyuanshe.OpenHackathon.Server.Models;

namespace Kaiyuanshe.OpenHackathon.Server.Storage.Entities
{
    /// <summary>
    /// Represents a team member.
    /// 
    /// PK: Hackathon name.
    /// RK: Guid.
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
        public string MemberId
        {
            get
            {
                return RowKey;
            }
        }

        [BackwardCompatible(nameof(RowKey))]
        public string UserId { get; set; }

        public string TeamId { get; set; }

        public string Description { get; set; }

        public TeamMemberRole Role { get; set; }

        public TeamMemberStatus Status { get; set; }
    }
}
