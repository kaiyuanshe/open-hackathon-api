using Kaiyuanshe.OpenHackathon.Server.Models;

namespace Kaiyuanshe.OpenHackathon.Server.Storage.Entities
{
    /// <summary>
    /// Represents a hackathon participant. Could be admin, organizer, partner, judge and so on.
    /// 
    /// PK: hackathon Name. 
    /// RK: user Id.
    /// 
    /// PK might be string.Empty for PlatformAdministrator.
    /// </summary>
    public class EnrollmentEntity : BaseTableEntity
    {
        [IgnoreEntityProperty]
        public string HackathonName
        {
            get
            {
                return PartitionKey;
            }
        }
        
        /// <summary>
        /// Id of User. RowKey.
        /// </summary>
        [IgnoreEntityProperty]
        public string UserId
        {
            get
            {
                return RowKey;
            }
        }

        public EnrollmentStatus Status { get; set; }

        [ConvertableEntityProperty]
        public Extension[] Extensions { get; set; }
    }
}
