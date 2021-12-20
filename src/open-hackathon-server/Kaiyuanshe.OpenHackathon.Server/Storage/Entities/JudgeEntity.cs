namespace Kaiyuanshe.OpenHackathon.Server.Storage.Entities
{
    /// <summary>
    /// Entity of a hackathon judge.
    /// 
    /// PK: hackathon name.
    /// RK: userId.
    /// </summary>
    public class JudgeEntity : BaseTableEntity
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
        /// id of User. RowKey
        /// </summary>
        [IgnoreEntityProperty]
        public string UserId
        {
            get
            {
                return RowKey;
            }
        }

        /// <summary>
        /// description of the Judge
        /// </summary>
        /// <example>Professor of Computer Science, Tsinghua University</example>
        public string Description { get; set; }
    }
}
