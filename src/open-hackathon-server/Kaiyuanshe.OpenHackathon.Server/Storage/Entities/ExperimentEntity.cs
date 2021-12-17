namespace Kaiyuanshe.OpenHackathon.Server.Storage.Entities
{
    /// <summary>
    /// Entity for Experiment. 
    /// PK: hackathonName.
    /// RK: auto-generated Guid.
    /// </summary>
    public class ExperimentEntity : BaseTableEntity
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
        /// id of experiment. RowKey
        /// </summary>
        [IgnoreEntityProperty]
        public string Id
        {
            get
            {
                return RowKey;
            }
        }

        public string TemplateName { get; set; }

        public string UserId { get; set; }

        /// <summary>
        /// paused. retained for future use. `false` for now.
        /// </summary>
        public bool Paused { get; set; }
    }
}
