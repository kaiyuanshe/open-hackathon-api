using k8s;
using Kaiyuanshe.OpenHackathon.Server.Biz;
using Kaiyuanshe.OpenHackathon.Server.Cache;
using Kaiyuanshe.OpenHackathon.Server.K8S;
using Kaiyuanshe.OpenHackathon.Server.Storage;
using Kaiyuanshe.OpenHackathon.Server.Storage.BlobContainers;
using Kaiyuanshe.OpenHackathon.Server.Storage.Mutex;
using Kaiyuanshe.OpenHackathon.Server.Storage.Tables;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Moq;
using System.Threading;
using System.Net.Http;
using Microsoft.Extensions.Options;

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
        public Mock<IAwardTable> AwardTable { get; } = new();
        public Mock<IAwardAssignmentTable> AwardAssignmentTable { get; } = new();
        public Mock<ICronJobTable> CronJobTable { get; } = new();
        public Mock<IEnrollmentTable> EnrollmentTable { get; } = new();
        public Mock<IExperimentTable> ExperimentTable { get; } = new();
        public Mock<IHackathonTable> HackathonTable { get; } = new();
        public Mock<IHackathonAdminTable> HackathonAdminTable { get; } = new();
        public Mock<IJudgeTable> JudgeTable { get; set; } = new();
        public Mock<IOrganizerTable> OrganizerTable { get; set; } = new();
        public Mock<IQuestionnaireTable> QuestionnaireTable { get; set; } = new();
        public Mock<IRatingKindTable> RatingKindTable { get; set; } = new();
        public Mock<IRatingTable> RatingTable { get; set; } = new();
        public Mock<ITeamTable> TeamTable { get; set; } = new();
        public Mock<ITeamMemberTable> TeamMemberTable { get; set; } = new();
        public Mock<ITeamWorkTable> TeamWorkTable { get; set; } = new();
        public Mock<ITemplateRepoTable> TemplateRepoTable { get; set; } = new();
        public Mock<ITemplateTable> TemplateTable { get; set; } = new();
        public Mock<ITopUserTable> TopUserTable { get; set; } = new();
        public Mock<IUserTable> UserTable { get; set; } = new();
        public Mock<IUserTokenTable> UserTokenTable { get; set; } = new();
        // contains
        public Mock<IReportsContainer> ReportsContainer { get; set; } = new();
        public Mock<IUserBlobContainer> UserBlobContainer { get; set; } = new();
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
        public Mock<IQuestionnaireManagement> QuestionnaireManagement { get; } = new();
        public Mock<ITemplateRepoManagement> TemplateRepoManagement { get; } = new();
        #endregion

        #region K8s
        public Mock<ICustomObjectsOperations> CustomObjects = new();
        public Mock<IKubernetes> Kubernetes = new();
        public Mock<IKubernetesCluster> KubernetesCluster = new();
        public Mock<IKubernetesClusterFactory> KubernetesClusterFactory = new();
        #endregion

        public Mock<ICacheProvider> CacheProvider { get; } = new();

        public Mock<HttpMessageHandler> HttpMessageHandler { get; } = new();
        public Mock<IHttpClientFactory> HttpClientFactory { get; } = new();

        #region Mutex
        public Mock<IMutexProvider> MutexProvider { get; } = new();
        public Mock<IMutex> Mutex { get; } = new();
        #endregion

        public Moqs()
        {
            LoggerFactory.Setup(l => l.CreateLogger(It.IsAny<string>())).Returns(Logger.Object);

            StorageContext.Setup(p => p.ActivityLogTable).Returns(ActivityLogTable.Object);
            StorageContext.Setup(p => p.AnnouncementTable).Returns(AnnouncementTable.Object);
            StorageContext.Setup(p => p.AwardTable).Returns(AwardTable.Object);
            StorageContext.Setup(p => p.AwardAssignmentTable).Returns(AwardAssignmentTable.Object);
            StorageContext.Setup(p => p.CronJobTable).Returns(CronJobTable.Object);
            StorageContext.Setup(p => p.EnrollmentTable).Returns(EnrollmentTable.Object);
            StorageContext.Setup(p => p.ExperimentTable).Returns(ExperimentTable.Object);
            StorageContext.Setup(p => p.HackathonTable).Returns(HackathonTable.Object);
            StorageContext.Setup(p => p.HackathonAdminTable).Returns(HackathonAdminTable.Object);
            StorageContext.Setup(p => p.JudgeTable).Returns(JudgeTable.Object);
            StorageContext.Setup(p => p.OrganizerTable).Returns(OrganizerTable.Object);
            StorageContext.Setup(p => p.QuestionnaireTable).Returns(QuestionnaireTable.Object);
            StorageContext.Setup(p => p.RatingKindTable).Returns(RatingKindTable.Object);
            StorageContext.Setup(p => p.RatingTable).Returns(RatingTable.Object);
            StorageContext.Setup(p => p.TeamTable).Returns(TeamTable.Object);
            StorageContext.Setup(p => p.TeamMemberTable).Returns(TeamMemberTable.Object);
            StorageContext.Setup(p => p.TeamWorkTable).Returns(TeamWorkTable.Object);
            StorageContext.Setup(p => p.TemplateRepoTable).Returns(TemplateRepoTable.Object);
            StorageContext.Setup(p => p.TemplateTable).Returns(TemplateTable.Object);
            StorageContext.Setup(p => p.TopUserTable).Returns(TopUserTable.Object);
            StorageContext.Setup(p => p.UserTable).Returns(UserTable.Object);
            StorageContext.Setup(p => p.UserTokenTable).Returns(UserTokenTable.Object);

            StorageContext.Setup(p => p.ReportsContainer).Returns(ReportsContainer.Object);
            StorageContext.Setup(p => p.UserBlobContainer).Returns(UserBlobContainer.Object);

            Kubernetes.Setup(k => k.CustomObjects).Returns(CustomObjects.Object);
            KubernetesClusterFactory.Setup(k => k.GetDefaultKubernetes(It.IsAny<CancellationToken>())).ReturnsAsync(KubernetesCluster.Object);

            HttpClientFactory.Setup(_ => _.CreateClient(Options.DefaultName)).Returns(() => new HttpClient(HttpMessageHandler.Object));
        }

        public void VerifyAll()
        {
            #region Storage
            Mock.VerifyAll(ActivityLogTable, AnnouncementTable, AwardTable, AwardAssignmentTable,
                CronJobTable, EnrollmentTable, ExperimentTable, HackathonTable,
                HackathonAdminTable, JudgeTable, OrganizerTable, QuestionnaireTable,
                RatingKindTable, RatingTable,
                TeamTable, TeamMemberTable, TeamWorkTable, TemplateRepoTable, TemplateTable,
                TopUserTable, UserTable, UserTokenTable, ReportsContainer, UserBlobContainer);

            ActivityLogTable.VerifyNoOtherCalls();
            AnnouncementTable.VerifyNoOtherCalls();
            AwardTable.VerifyNoOtherCalls();
            AwardAssignmentTable.VerifyNoOtherCalls();
            CronJobTable.VerifyNoOtherCalls();
            EnrollmentTable.VerifyNoOtherCalls();
            ExperimentTable.VerifyNoOtherCalls();
            HackathonTable.VerifyNoOtherCalls();
            HackathonAdminTable?.VerifyNoOtherCalls();
            JudgeTable.VerifyNoOtherCalls();
            OrganizerTable.VerifyNoOtherCalls();
            QuestionnaireTable.VerifyNoOtherCalls();
            RatingKindTable.VerifyNoOtherCalls();
            RatingTable.VerifyNoOtherCalls();
            TeamTable.VerifyNoOtherCalls();
            TeamMemberTable.VerifyNoOtherCalls();
            TeamWorkTable.VerifyNoOtherCalls();
            TemplateRepoTable.VerifyNoOtherCalls();
            TemplateTable.VerifyNoOtherCalls();
            TopUserTable.VerifyNoOtherCalls();
            UserTable.VerifyNoOtherCalls();
            UserTokenTable.VerifyNoOtherCalls();

            ReportsContainer.VerifyNoOtherCalls();
            UserBlobContainer.VerifyNoOtherCalls();
            #endregion

            #region Biz
            Mock.VerifyAll(ActivityLogManagement, AnnouncementManagement, AuthorizationService,
                AwardManagement, EnrollmentManagement, ExperimentManagement,
                FileManagement, HackathonAdminManagement, HackathonManagement,
                JudgeManagement, OrganizerManagement, QuestionnaireManagement, RatingManagement,
                TeamManagement, TemplateRepoManagement, UserManagement, WorkManagement);

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
            OrganizerManagement.VerifyNoOtherCalls();
            QuestionnaireManagement.VerifyNoOtherCalls();
            RatingManagement.VerifyNoOtherCalls();
            TeamManagement.VerifyNoOtherCalls();
            TemplateRepoManagement.VerifyNoOtherCalls();
            UserManagement.VerifyNoOtherCalls();
            WorkManagement.VerifyNoOtherCalls();
            #endregion

            #region k8s
            Mock.VerifyAll(CustomObjects, KubernetesCluster);
            CustomObjects.VerifyNoOtherCalls();
            KubernetesCluster.VerifyNoOtherCalls();
            #endregion

            Mock.VerifyAll(CacheProvider);
            CacheProvider?.VerifyNoOtherCalls();

            Mock.VerifyAll(HttpMessageHandler);
            // There're followed calls to HttpMessageHandler.Dispose(True)
            // HttpMessageHandler?.VerifyNoOtherCalls();
            // No need to verify HttpClientFactory since we've verified HttpMessageHandler
            // Mock.VerifyAll(HttpClientFactory);
            // HttpClientFactory?.VerifyNoOtherCalls();

            #region Mutex
            Mock.VerifyAll(MutexProvider, Mutex);
            MutexProvider.VerifyNoOtherCalls();
            Mutex.VerifyNoOtherCalls();
            #endregion
        }
    }
}
