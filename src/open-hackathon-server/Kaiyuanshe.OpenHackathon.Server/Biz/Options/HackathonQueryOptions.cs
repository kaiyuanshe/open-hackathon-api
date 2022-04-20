using Kaiyuanshe.OpenHackathon.Server.Models;

namespace Kaiyuanshe.OpenHackathon.Server.Biz.Options
{
    public class HackathonQueryOptions : TableQueryOptions
    {
        /// <summary>
        /// hackathons related to a specified user
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// whether the <see cref="UserId"/> is a platform administrator.
        /// </summary>
        public bool IsPlatformAdmin { get; set; }

        /// <summary>
        /// search in name/displayName/description
        /// </summary>
        public string Search { get; set; }

        /// <summary>
        /// Ordering. default to createdAt.
        /// </summary>
        public HackathonOrderBy? OrderBy { get; set; }

        /// <summary>
        /// type of hackathons. Default to online. 
        /// </summary>
        public HackathonListType? ListType { get; set; }
    }
}
