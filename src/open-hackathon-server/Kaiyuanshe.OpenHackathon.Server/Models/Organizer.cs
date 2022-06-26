using Kaiyuanshe.OpenHackathon.Server.Models.Validations;
using System.ComponentModel.DataAnnotations;

namespace Kaiyuanshe.OpenHackathon.Server.Models
{
    /// <summary>
    /// Represents an organizer who organizes/sponsors the hackathon.
    /// </summary>
    public class Organizer : ModelBase
    {
        /// <summary>
        /// name of hackathon
        /// </summary>
        /// <example>foo</example>
        public string hackathonName { get; internal set; }

        /// <summary>
        /// auto-generated id of the organizer.
        /// </summary>
        /// <example>9ffbe751-975f-46a7-b4f7-48f2cc2805b0</example>
        public string id { get; internal set; }

        /// <summary>
        /// Name of the organizer.
        /// </summary>
        /// <example>开源社</example>
        [MaxLength(64)]
        [RequiredIfPut]
        public string name { get; set; }

        /// <summary>
        /// description of the organzier
        /// </summary>
        /// <example>专注于“开源治理、社区发展、国际接轨、开源项目”的开源社区联盟.</example>
        [MaxLength(256)]
        public string description { get; set; }

        /// <summary>
        /// target whom the award is given. team or individual. team by default.
        /// </summary>
        /// <example>sponsor</example>
        [RequiredIfPut]
        public OrganizerType? type { get; set; }

        /// <summary>
        /// A logo picture.
        /// </summary>
        [RequiredIfPut]
        public PictureInfo logo { get; set; }
    }

    /// <summary>
    /// a list of Organizer
    /// </summary>
    public class OrganizerList : ResourceList<Organizer>
    {
        /// <summary>
        /// a list of Organizer
        /// </summary>
        public override Organizer[] value { get; set; }
    }

    /// <summary>
    /// Type of organizer. host:主办, organizer:承办，coorganzer:协办, sponsor:赞助, titleSponsor:冠名
    /// </summary>
    public enum OrganizerType
    {
        /// <summary>
        /// 主办
        /// </summary>
        host,
        /// <summary>
        /// 承办
        /// </summary>
        organizer,
        /// <summary>
        /// 协办
        /// </summary>
        coorganizer,
        /// <summary>
        /// 赞助
        /// </summary>
        sponsor,
        /// <summary>
        /// 冠名
        /// </summary>
        titleSponsor,
    }
}
