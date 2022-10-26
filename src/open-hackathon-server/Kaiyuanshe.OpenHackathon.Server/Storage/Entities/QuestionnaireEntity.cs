using Kaiyuanshe.OpenHackathon.Server.Models;

namespace Kaiyuanshe.OpenHackathon.Server.Storage.Entities
{
    /// <summary>
    /// Entity for hackathon questionnaires.
    ///
    /// PK: hackathonName;
    /// RK: string.Empty;
    /// </summary>
    public class QuestionnaireEntity : BaseTableEntity
    {
        /// <summary>
        /// name of Hackathon. PartitionKey
        /// </summary>
        [IgnoreEntityProperty]
        public string HackathonName
        {
            get { return PartitionKey; }
        }

        [ConvertableEntityProperty]
        public Extension[] Extensions { get; set; }
    }
}
