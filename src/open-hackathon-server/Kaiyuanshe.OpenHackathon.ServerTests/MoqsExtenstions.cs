using Kaiyuanshe.OpenHackathon.Server.Controllers;
using Kaiyuanshe.OpenHackathon.Server.ResponseBuilder;

namespace Kaiyuanshe.OpenHackathon.ServerTests
{
    internal static class MoqsExtenstions
    {
        public static void SetupController(this Moqs moqs, HackathonControllerBase controller)
        {
            controller.HackathonManagement = moqs.HackathonManagement.Object;
            controller.EnrollmentManagement = moqs.EnrollmentManagement.Object;
            controller.UserManagement = moqs.UserManagement.Object;
            controller.ActivityLogManagement = moqs.ActivityLogManagement.Object;
            controller.ProblemDetailsFactory = new CustomProblemDetailsFactory();
            controller.ResponseBuilder = new DefaultResponseBuilder();
            controller.AuthorizationService = moqs.AuthorizationService.Object;
            controller.ExperimentManagement = moqs.ExperimentManagement.Object;
            controller.JudgeManagement = moqs.JudgeManagement.Object;
            controller.RatingManagement = moqs.RatingManagement.Object;
            controller.TeamManagement = moqs.TeamManagement.Object;
            controller.HackathonAdminManagement = moqs.HackathonAdminManagement.Object;
            controller.AwardManagement = moqs.AwardManagement.Object;
        }

    }
}
