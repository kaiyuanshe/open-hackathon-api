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
        // hackathon
        createHackathon,
        updateHackathon,
        deleteHackathon,
        publishHackathon, // request publish
        approveHackahton, // approve the publish request
        archiveHackathon, // make it read-only
        unarchiveHackathon, // make it writable

        // hackathon Admin
        createHackathonAdmin,
        deleteHackathonAdmin,

        // hackathon Organizer
        createOrganizer,
        updateOrganizer,
        deleteOrganizer,

        // team
        createTeam,
        updateTeam,
        deleteTeam,
        joinTeam,
        addTeamMember,
        updateTeamMember,
        approveTeamMember,
        updateTeamMemberRole,
        leaveTeam,
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
        createAwardAssignmentTeam,
        createAwardAssignmentIndividual,
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

        // FileUpload
        fileUpload,

        // template
        createTemplate,
        updateTemplate,
        deleteTemplate,

        // experiment
        createExperiment,
        updateExperiment,
        resetExperiment,
        deleteExperiment,
    }

}
