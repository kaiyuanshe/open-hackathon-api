using System;

namespace Kaiyuanshe.OpenHackathon.Server.Models
{

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
    public class MessageFormatAttribute : Attribute
    {
        public string ResourceKey { get; }

        public MessageFormatAttribute(string resourceKey)
        {
            ResourceKey = resourceKey;
        }
    }

    public enum ActivityLogType
    {
        none,

        // hackathon
        [MessageFormat(nameof(Resources.ActivitityLog_CreateHackathon))]
        createHackathon,
        updateHackathon,
        deleteHackathon,
        publishHackathon, // request publish
        approveHackahton, // approve the publish request
        archiveHackathon, // make it read-only

        // hackathon Admin
        [MessageFormat(nameof(Resources.ActivitityLog_CreateHackathonAdmin))]
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
        deleteTemplate,

        // experiment
        createExperiment,
        updateExperiment,
        deleteExperiment,
    }

}
