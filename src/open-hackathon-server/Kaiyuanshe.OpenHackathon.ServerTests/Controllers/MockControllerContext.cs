using Kaiyuanshe.OpenHackathon.Server.Biz;
using Kaiyuanshe.OpenHackathon.Server.Controllers;
using Kaiyuanshe.OpenHackathon.Server.ResponseBuilder;
using Microsoft.AspNetCore.Authorization;
using Moq;

namespace Kaiyuanshe.OpenHackathon.ServerTests.Controllers
{
    internal class MockControllerContext
    {
        public Mock<IHackathonManagement> HackathonManagement { get; }
        public Mock<IEnrollmentManagement> EnrollmentManagement { get; }
        public Mock<IUserManagement> UserManagement { get; }
        public Mock<IActivityLogManagement> ActivityLogManagement { get; }
        public Mock<IAuthorizationService> AuthorizationService { get; }
        public Mock<IExperimentManagement> ExperimentManagement { get; }
        public Mock<IJudgeManagement> JudgeManagement { get; }
        public Mock<IRatingManagement> RatingManagement { get; }

        public MockControllerContext()
        {
            HackathonManagement = new Mock<IHackathonManagement>();
            EnrollmentManagement = new Mock<IEnrollmentManagement>();
            UserManagement = new Mock<IUserManagement>();
            ActivityLogManagement = new Mock<IActivityLogManagement>();
            AuthorizationService = new Mock<IAuthorizationService>();
            ExperimentManagement = new Mock<IExperimentManagement>();
            JudgeManagement = new Mock<IJudgeManagement>();
            RatingManagement = new Mock<IRatingManagement>();
        }

        public void SetupController(HackathonControllerBase controller)
        {
            controller.HackathonManagement = HackathonManagement.Object;
            controller.EnrollmentManagement = EnrollmentManagement.Object;
            controller.UserManagement = UserManagement.Object;
            controller.ActivityLogManagement = ActivityLogManagement.Object;
            controller.ProblemDetailsFactory = new CustomProblemDetailsFactory();
            controller.ResponseBuilder = new DefaultResponseBuilder();
            controller.AuthorizationService = AuthorizationService.Object;
            controller.ExperimentManagement = ExperimentManagement.Object;
            controller.JudgeManagement = JudgeManagement.Object;
            controller.RatingManagement = RatingManagement.Object;
        }
    }
}
