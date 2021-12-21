namespace Kaiyuanshe.OpenHackathon.Server.Storage.Entities
{
    /// <summary>
    /// Entity for rating kinds. 
    /// PK: hackathonName.
    /// RK: Auto-generated Guid.
    /// </summary>
    public class RatingKindEntity : BaseTableEntity
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
        /// auto-generated Guid of kind. RowKey.
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
        public int MaximumScore { get; set; }
    }
}
