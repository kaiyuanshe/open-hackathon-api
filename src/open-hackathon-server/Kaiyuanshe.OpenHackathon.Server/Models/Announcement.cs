using Kaiyuanshe.OpenHackathon.Server.Models.Validations;
using System.ComponentModel.DataAnnotations;

namespace Kaiyuanshe.OpenHackathon.Server.Models
{
    /// <summary>
    /// Represents an announcement.
    /// </summary>
    public class Announcement : ModelBase
    {
        /// <summary>
        /// name of hackathon
        /// </summary>
        /// <example>foo</example>
        public string hackathonName { get; internal set; }

        /// <summary>
        /// auto-generated id of the announcement.
        /// </summary>
        /// <example>fa896287-ab7a-4429-a84d-701d8d312a28</example>
        public string id { get; internal set; }

        /// <summary>
        /// Optional title.
        /// </summary>
        /// <example>sample title</example>
        [MaxLength(256)]
        public string title { get; set; }

        /// <summary>
        /// Content. Can be rich-text.
        /// </summary>
        /// <example>This is a sample content.</example>
        [MaxLength(10240)]
        [RequiredIfPut]
        public string content { get; set; }
    }

    /// <summary>
    /// a list of Announcement
    /// </summary>
    public class AnnouncementList : ResourceList<Announcement>
    {
        /// <summary>
        /// a list of Announcement
        /// </summary>
        public override Announcement[] value { get; set; }
    }
}
