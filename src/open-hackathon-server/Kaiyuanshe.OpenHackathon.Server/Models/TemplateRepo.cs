using Kaiyuanshe.OpenHackathon.Server.Models.Validations;
using System.Collections.Generic;

namespace Kaiyuanshe.OpenHackathon.Server.Models
{
    /// <summary>
    /// Represents an template repo pinned for a hackathon.
    /// </summary>
    public class TemplateRepo : ModelBase
    {
        /// <summary>
        /// name of hackathon.
        /// </summary>
        /// <example>foo</example>
        public string hackathonName { get; internal set; }

        /// <summary>
        /// auto-generated id of the TemplareRepo.
        /// </summary>
        /// <example>9ffbe751-975f-46a7-b4f7-48f2cc2805b0</example>
        public string id { get; internal set; }

        /// <summary>
        /// GitHub repo url. So far, only GitHub repo is supported for fetching metadata.
        /// </summary>
        /// <example>https://github.com/idea2app/Next-Bootstrap-ts</example>
        [AbsoluteUri]
        [RequiredIfPut]
        public string url { get; set; }

        /// <summary>
        /// is GitHub repo info fetched.
        /// </summary>
        public bool? isFetched { get; internal set; }

        /// <summary>
        /// GitHub repo languages.
        /// </summary>
        /// <example>C=78769,Python=7769</example>
        public IDictionary<string, string>? repoLanguages { get; internal set; }

        /// <summary>
        /// GitHub repo topics.
        /// </summary>
        /// <example>bootstrap,template,typescript,nextjs,scaffold</example>
        public IList<string>? repoTopics { get; internal set; }

        public TemplateRepo ShallowCopy()
        {
            return (TemplateRepo)this.MemberwiseClone();
        }
    }

    /// <summary>
    /// a list of TemplateRepo
    /// </summary>
    public class TemplateRepoList : ResourceList<TemplateRepo>
    {
        /// <summary>
        /// a list of TemplateRepo
        /// </summary>
        public override TemplateRepo[] value { get; set; }
    }
}
