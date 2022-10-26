using System.ComponentModel.DataAnnotations;

namespace Kaiyuanshe.OpenHackathon.Server.Models
{
    /// <summary>
    /// a class indicates a questionnaire
    /// </summary>
    public class Questionnaire : ModelBase
    {
        public const int MaxExtensions = 20;

        /// <summary>
        /// name of hackathon
        /// </summary>
        /// <example>foo</example>
        public string hackathonName { get; internal set; }

        /// <summary>
        /// Extra properties. A maximum of 20 extensions are allowed.
        /// `name`(case-sensitive) must be unique. If not unique, the last item with same name will be saved, others are ignored.
        /// </summary>
        [MaxLength(MaxExtensions)]
        public Extension[] extensions { get; set; }
    }
}
