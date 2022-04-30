using Kaiyuanshe.OpenHackathon.Server.Biz;
using Kaiyuanshe.OpenHackathon.Server.Controllers;
using Kaiyuanshe.OpenHackathon.Server.ResponseBuilder;
using Microsoft.AspNetCore.Authorization;
using Moq;
using System;

namespace Kaiyuanshe.OpenHackathon.ServerTests.Controllers
{
    [Obsolete]
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
        public Mock<ITeamManagement> TeamManagement { get; }
        public Mock<IHackathonAdminManagement> HackathonAdminManagement { get; }
        public Mock<IAwardManagement> AwardManagement { get; }

        public MockControllerContext()
        {
            HackathonManagement = new Mock<IHackathonManagement>();
            EnrollmentManagement = new Mock<IEnrollmentManagement>();
            UserManagement = new Mock<IUserManagement>();
            ActivityLogManagement = new Mock<IActivityLogManagement>();
            AuthorizationService = new Mock<IAuthorizationService>();
            ExperimentManagement = new Mock<IExperimentManagement>();
            JudgeManagement = new Mock<IJudgeManagement>();
            TeamManagement = new Mock<ITeamManagement>();
            RatingManagement = new Mock<IRatingManagement>();
            HackathonAdminManagement = new Mock<IHackathonAdminManagement>();
            AwardManagement = new Mock<IAwardManagement>();
        }

        [Obsolete]
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
            controller.TeamManagement = TeamManagement.Object;
            controller.HackathonAdminManagement = HackathonAdminManagement.Object;
            controller.AwardManagement = AwardManagement.Object;
        }

        [Obsolete]
        public void VerifyAll()
        {
            Mock.VerifyAll(HackathonManagement, EnrollmentManagement, UserManagement,
                ActivityLogManagement, AuthorizationService, ExperimentManagement, JudgeManagement,
                RatingManagement, TeamManagement, HackathonAdminManagement, AwardManagement);

            HackathonManagement.VerifyNoOtherCalls();
            EnrollmentManagement.VerifyNoOtherCalls();
            UserManagement.VerifyNoOtherCalls();
            ActivityLogManagement.VerifyNoOtherCalls();
            AuthorizationService.VerifyNoOtherCalls();
            ExperimentManagement.VerifyNoOtherCalls();
            JudgeManagement.VerifyNoOtherCalls();
            TeamManagement.VerifyNoOtherCalls();
            RatingManagement.VerifyNoOtherCalls();
            HackathonAdminManagement.VerifyNoOtherCalls();
            AwardManagement.VerifyNoOtherCalls();
        }
    }
}
