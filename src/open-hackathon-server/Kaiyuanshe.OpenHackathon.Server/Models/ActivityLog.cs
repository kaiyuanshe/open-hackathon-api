namespace Kaiyuanshe.OpenHackathon.Server.Models
{
    /// <summary>
    /// activity logs
    /// </summary>
    public class ActivityLog : ModelBase
    {
        /// <summary>
        /// name of hackathon
        /// </summary>
        /// <example>foo</example>
        public string hackathonName { get; internal set; }

        /// <summary>
        /// id of user who performs the operation.
        /// </summary>
        /// <example>1</example>
        public string userId { get; internal set; }

        /// <summary>
        /// related team id. might be null.
        /// </summary>
        /// <example>d1e40c38-cc2a-445f-9eab-60c253256c57</example>
        public string teamId { get; set; }

        /// <summary>
        /// The user id on whom the operation perferms.
        /// </summary>
        /// <example>2</example>
        public string correlatedUserId { get; internal set; }

        /// <summary>
        /// auto-generated activity log id.
        /// </summary>
        /// <example>323fed7f-447e-4c2e-854c-1421e2439208</example>
        public string activityId { get; internal set; }

        /// <summary>
        /// Key message related to the activity.
        /// </summary>
        /// <example>Something happens.</example>
        public string message { get; set; }

        /// <summary>
        /// type of the activity log.
        /// </summary>
        public ActivityLogType activityLogType { get; internal set; }
    }

    public enum ActivityLogType
    {
        // hackathon
        createHackathon,
        updateHackathon,
        deleteHackathon,
        publishHackathon, // request publish
        approveHackahton, // approve the publish request
        archiveHackathon, // make it read-only

        // hackathon Admin
        createHackathonAdmin,
        deleteHackathonAdmin,

        // team
        createTeam,
        updateTeam,
        deleteTeam,
        createTeamMember,
        updateTeamMember,
        deleteTeamMember,
        createTeamWork,
        updateTeamWork,
        deleteTeamWork,

        // Award
        createAward,
        updateAward,
        deleteAward,

        // Award
        createJudge,
        updateJudge,
        deleteJudge,

        // AwardAssignment
        createAwardAssignment,
        updateAwardAssignment,
        deleteAwardAssignment,

        // Enrollment
        createEnrollment,
        updateEnrollment,
        approveEnrollment,
        rejectEnrollment,

        // Rating
        createRatingKind,
        updateRatingKind,
        deleteRatingKind,
        createRating,
        updateRating,
        deleteRating,

        // login
        login,

        // template
        createTemplate,
        updateTemplate,

        // experiment
        createExperiment,
    }
}
