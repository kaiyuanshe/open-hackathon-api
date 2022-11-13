namespace Kaiyuanshe.OpenHackathon.Server.Models
{
    public enum ReportType
    {
        /// <summary>
        /// export all enrollments including those are pending approval.
        /// </summary>
        enrollments,
        /// <summary>
        /// export all teams and their members.
        /// </summary>
        teams,
        /// <summary>
        /// export all team works.
        /// </summary>
        teamWorks,
    }
}
