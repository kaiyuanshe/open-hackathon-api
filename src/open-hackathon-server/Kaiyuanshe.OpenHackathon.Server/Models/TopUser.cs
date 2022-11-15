namespace Kaiyuanshe.OpenHackathon.Server.Models
{
    /// <summary>
    /// Top users that engage in hackathon events.
    /// </summary>
    public class TopUser : ModelBase
    {
        /// <summary>
        /// the rank. Starting from 0. 0 means the highest rank.
        /// </summary>
        /// <example>1</example>
        public int rank { get; internal set; }

        /// <summary>
        /// the score that is used for ranking. The more in score, the higher in rank.
        /// </summary>
        /// <example>100</example>
        public int score { get; internal set; }

        /// <summary>
        /// user id.
        /// </summary>
        /// <example>1</example>
        public string userId { get; internal set; }

        /// <summary>
        /// The user details.
        /// </summary>
        public UserInfo user { get; internal set; }
    }

    /// <summary>
    /// a list of users
    /// </summary>
    public class TopUserList : ResourceList<TopUser>
    {
        /// <summary>
        /// a list of TopUser
        /// </summary>
        public override TopUser[] value { get; set; }
    }
}
