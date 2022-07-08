namespace Kaiyuanshe.OpenHackathon.Server.Storage.Entities
{
    /// <summary>
    /// Entity for announcement.
    /// PK: hackathonName.
    /// RK: Guid.
    /// </summary>
    public class AnnouncementEntity : BaseTableEntity
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
        /// id of announcement. RowKey
        /// </summary>
        [IgnoreEntityProperty]
        public string Id
        {
            get
            {
                return RowKey;
            }
        }

        public string Title { get; set; }

        public string Content { get; set; }
    }
}
