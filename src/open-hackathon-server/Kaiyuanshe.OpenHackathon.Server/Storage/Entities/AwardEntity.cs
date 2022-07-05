using Kaiyuanshe.OpenHackathon.Server.Models;

namespace Kaiyuanshe.OpenHackathon.Server.Storage.Entities
{
    /// <summary>
    /// entity for award information
    /// 
    /// PK: hackathon name
    /// RK: auto-generated GUID
    /// </summary>
    public class AwardEntity : BaseTableEntity
    {
        /// <summary>
        /// name of Hackathon. PartitionKey
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
        /// id of award. RowKey
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
        public string? Description { get; set; }
        public int Quantity { get; set; }
        public AwardTarget Target { get; set; }

        [ConvertableEntityProperty]
        public PictureInfo[] Pictures { get; set; }
    }
}
