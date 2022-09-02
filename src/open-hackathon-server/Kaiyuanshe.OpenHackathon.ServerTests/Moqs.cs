using k8s;
using Kaiyuanshe.OpenHackathon.Server.Biz;
using Kaiyuanshe.OpenHackathon.Server.Cache;
using Kaiyuanshe.OpenHackathon.Server.Storage;
using Kaiyuanshe.OpenHackathon.Server.Storage.Tables;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Moq;

namespace Kaiyuanshe.OpenHackathon.ServerTests
{
    internal class Moqs
    {
        public Mock<ILogger> Logger = new();
        public Mock<ILoggerFactory> LoggerFactory = new();

        #region Storage
        public Mock<IStorageContext> StorageContext { get; } = new();
        public Mock<IActivityLogTable> ActivityLogTable { get; } = new();
        public Mock<IAnnouncementTable> AnnouncementTable { get; } = new();
        public Mock<IAwardAssignmentTable> AwardAssignmentTable { get; } = new();
        public Mock<IExperimentTable> ExperimentTable { get; } = new();
        public Mock<IHackathonTable> HackathonTable { get; } = new();
        public Mock<IHackathonAdminTable> HackathonAdminTable { get; } = new();
        public Mock<IJudgeTable> JudgeTable { get; set; } = new();
        public Mock<IOrganizerTable> OrganizerTable { get; set; } = new();
        public Mock<ITeamTable> TeamTable { get; set; } = new();
        public Mock<ITeamMemberTable> TeamMemberTable { get; set; } = new();
        public Mock<ITeamWorkTable> TeamWorkTable { get; set; } = new();
        public Mock<ITopUserTable> TopUserTable { get; set; } = new();
        public Mock<IUserTokenTable> UserTokenTable { get; set; } = new();
        #endregion

        #region Biz
        public Mock<IAnnouncementManagement> AnnouncementManagement { get; } = new();
        public Mock<IHackathonManagement> HackathonManagement { get; } = new();
        public Mock<IEnrollmentManagement> EnrollmentManagement { get; } = new();
        public Mock<IUserManagement> UserManagement { get; } = new();
        public Mock<IActivityLogManagement> ActivityLogManagement { get; } = new();
        public Mock<IAuthorizationService> AuthorizationService { get; } = new();
        public Mock<IExperimentManagement> ExperimentManagement { get; } = new();
        public Mock<IJudgeManagement> JudgeManagement { get; } = new();
        public Mock<IRatingManagement> RatingManagement { get; } = new();
        public Mock<ITeamManagement> TeamManagement { get; } = new();
        public Mock<IHackathonAdminManagement> HackathonAdminManagement { get; } = new();
        public Mock<IAwardManagement> AwardManagement { get; } = new();
        public Mock<IWorkManagement> WorkManagement { get; } = new();
        public Mock<IFileManagement> FileManagement { get; } = new();
        public Mock<IOrganizerManagement> OrganizerManagement { get; } = new();
        #endregion

        #region K8s
        public Mock<IKubernetes> Kubernetes = new();
        public Mock<ICustomObjectsOperations> CustomObjects = new();
        #endregion

        public Mock<ICacheProvider> CacheProvider { get; } = new();

        public Moqs()
        {
            LoggerFactory.Setup(l => l.CreateLogger(It.IsAny<string>())).Returns(Logger.Object);

            StorageContext.Setup(p => p.ActivityLogTable).Returns(ActivityLogTable.Object);
            StorageContext.Setup(p => p.AnnouncementTable).Returns(AnnouncementTable.Object);
            StorageContext.Setup(p => p.AwardAssignmentTable).Returns(AwardAssignmentTable.Object);
            StorageContext.Setup(p => p.ExperimentTable).Returns(ExperimentTable.Object);
            StorageContext.Setup(p => p.HackathonTable).Returns(HackathonTable.Object);
            StorageContext.Setup(p => p.HackathonAdminTable).Returns(HackathonAdminTable.Object);
            StorageContext.Setup(p => p.JudgeTable).Returns(JudgeTable.Object);
            StorageContext.Setup(p => p.OrganizerTable).Returns(OrganizerTable.Object);
            StorageContext.Setup(p => p.TeamTable).Returns(TeamTable.Object);
            StorageContext.Setup(p => p.TeamMemberTable).Returns(TeamMemberTable.Object);
            StorageContext.Setup(p => p.TeamWorkTable).Returns(TeamWorkTable.Object);
            StorageContext.Setup(p => p.TopUserTable).Returns(TopUserTable.Object);
            StorageContext.Setup(p => p.UserTokenTable).Returns(UserTokenTable.Object);

            Kubernetes.Setup(k => k.CustomObjects).Returns(CustomObjects.Object);
        }

        public void VerifyAll()
        {
            #region Storage
            Mock.VerifyAll(ActivityLogTable, AnnouncementTable, AwardAssignmentTable,
                ExperimentTable, HackathonTable,
                HackathonAdminTable, JudgeTable, OrganizerTable,
                TeamTable, TeamMemberTable, TeamWorkTable, TopUserTable,
                UserTokenTable);

            ActivityLogTable.VerifyNoOtherCalls();
            AnnouncementTable.VerifyNoOtherCalls();
            AwardAssignmentTable.VerifyNoOtherCalls();
            ExperimentTable.VerifyNoOtherCalls();
            HackathonTable.VerifyNoOtherCalls();
            HackathonAdminTable?.VerifyNoOtherCalls();
            JudgeTable.VerifyNoOtherCalls();
            OrganizerTable.VerifyNoOtherCalls();
            TeamTable.VerifyNoOtherCalls();
            TeamMemberTable.VerifyNoOtherCalls();
            TeamWorkTable.VerifyNoOtherCalls();
            TopUserTable.VerifyNoOtherCalls();
            UserTokenTable.VerifyNoOtherCalls();
            #endregion

            #region Biz
            Mock.VerifyAll(ActivityLogManagement, AnnouncementManagement, AuthorizationService,
                AwardManagement, EnrollmentManagement, ExperimentManagement,
                FileManagement, HackathonAdminManagement, HackathonManagement,
                JudgeManagement, RatingManagement, TeamManagement,
                UserManagement, WorkManagement);

            ActivityLogManagement.VerifyNoOtherCalls();
            AnnouncementManagement.VerifyNoOtherCalls();
            AuthorizationService.VerifyNoOtherCalls();
            AwardManagement.VerifyNoOtherCalls();
            EnrollmentManagement.VerifyNoOtherCalls();
            ExperimentManagement.VerifyNoOtherCalls();
            FileManagement.VerifyNoOtherCalls();
            HackathonAdminManagement.VerifyNoOtherCalls();
            HackathonManagement.VerifyNoOtherCalls();
            JudgeManagement.VerifyNoOtherCalls();
            RatingManagement.VerifyNoOtherCalls();
            TeamManagement.VerifyNoOtherCalls();
            UserManagement.VerifyNoOtherCalls();
            WorkManagement.VerifyNoOtherCalls();
            #endregion

            #region k8s
            Mock.VerifyAll(CustomObjects);
            CustomObjects.VerifyNoOtherCalls();
            #endregion

            Mock.VerifyAll(CacheProvider);
            CacheProvider?.VerifyNoOtherCalls();
        }
    }
}
