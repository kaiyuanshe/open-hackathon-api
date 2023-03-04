using Kaiyuanshe.OpenHackathon.Server.Models;
using System.Collections.Generic;

namespace Kaiyuanshe.OpenHackathon.Server.Storage.Entities
{
    /// <summary>
    /// Entity for hackathon project template repos.
    ///
    /// PK: hackathonName;
    /// RK: Guid;
    /// </summary>
    public class TemplateRepoEntity : BaseTableEntity
    {
        /// <summary>
        /// name of hackathon. PartitionKey
        /// </summary>
        [IgnoreEntityProperty]
        public string HackathonName
        {
            get { return PartitionKey; }
        }

        /// <summary>
        /// id of TemplateRepo. RowKey
        /// </summary>
        [IgnoreEntityProperty]
        public string Id
        {
            get
            {
                return RowKey;
            }
        }

        /// <summary>
        /// GitHub repo url.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// is GitHub repo info fetched.
        /// </summary>
        public bool IsFetched { get; set; }

        /// <summary>
        /// GitHub repo languages.
        /// </summary>
        [ConvertableEntityProperty]
        public IDictionary<string, string>? RepoLanguages { get; set; }

        /// <summary>
        /// GitHub repo topics.
        /// </summary>
        [ConvertableEntityProperty]
        public IList<string>? RepoTopics { get; set; }
    }
}
