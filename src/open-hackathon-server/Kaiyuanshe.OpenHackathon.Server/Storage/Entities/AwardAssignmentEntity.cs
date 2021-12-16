namespace Kaiyuanshe.OpenHackathon.Server.Storage.Entities
{
    /// <summary>
    /// entity for assignment info for an award
    /// 
    /// PK: Hackathon name
    /// RK: assignment id. Auto-generated GUID.
    /// </summary>
    public class AwardAssignmentEntity : BaseTableEntity
    {
        /// <summary>
        /// name of hackathon. PK.
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
        /// Auto-generated GUID, RK
        /// </summary>
        [IgnoreEntityProperty]
        public string AssignmentId
        {
            get
            {
                return RowKey;
            }
        }

        /// <summary>
        /// id of the assignee. userId(if award.Target is individual) or teamId(if award.Target is team).
        /// </summary>
        public string AssigneeId { get; set; }

        public string AwardId { get; set; }

        public string Description { get; set; }
    }
}
