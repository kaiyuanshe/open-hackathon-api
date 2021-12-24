using Kaiyuanshe.OpenHackathon.Server.Models;

namespace Kaiyuanshe.OpenHackathon.Server.Storage.Entities
{
    /// <summary>
    /// Entity of TeamWork.
    /// 
    /// PK: HackathonName.
    /// RK: auto-generated GUID
    /// </summary>
    public class TeamWorkEntity : BaseTableEntity
    {
        /// <summary>
        /// PartitionKey
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
        /// RowKey
        /// </summary>
        [IgnoreEntityProperty]
        public string Id
        {
            get
            {
                return RowKey;
            }
        }

        public string TeamId { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public TeamWorkType Type { get; set; }

        public string Url { get; set; }
    }
}
