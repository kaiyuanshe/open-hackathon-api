using DotLiquid.Util;
using Kaiyuanshe.OpenHackathon.Server.Biz;
using Kaiyuanshe.OpenHackathon.Server.Controllers;
using Kaiyuanshe.OpenHackathon.Server.CronJobs;
using Kaiyuanshe.OpenHackathon.Server.ResponseBuilder;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Kaiyuanshe.OpenHackathon.Server.Storage.Mutex;
using Microsoft.Extensions.Logging;
using Moq;
using System.Threading;

namespace Kaiyuanshe.OpenHackathon.ServerTests
{
    internal static class MoqsExtenstions
    {
        public static void SetupController(this Moqs moqs, HackathonControllerBase controller)
        {
            controller.ActivityLogManagement = moqs.ActivityLogManagement.Object;
            controller.AnnouncementManagement = moqs.AnnouncementManagement.Object;
            controller.AwardManagement = moqs.AwardManagement.Object;
            controller.EnrollmentManagement = moqs.EnrollmentManagement.Object;
            controller.ExperimentManagement = moqs.ExperimentManagement.Object;
            controller.FileManagement = moqs.FileManagement.Object;
            controller.HackathonManagement = moqs.HackathonManagement.Object;
            controller.HackathonAdminManagement = moqs.HackathonAdminManagement.Object;
            controller.JudgeManagement = moqs.JudgeManagement.Object;
            controller.OrganizerManagement = moqs.OrganizerManagement.Object;
            controller.RatingManagement = moqs.RatingManagement.Object;
            controller.TeamManagement = moqs.TeamManagement.Object;
            controller.UserManagement = moqs.UserManagement.Object;
            controller.WorkManagement = moqs.WorkManagement.Object;

            controller.AuthorizationService = moqs.AuthorizationService.Object;
            controller.ProblemDetailsFactory = new CustomProblemDetailsFactory();
            controller.ResponseBuilder = new DefaultResponseBuilder();
        }

        public static void SetupManagement<T>(this Moqs moqs, ManagementClient<T> management)
        {
            management.Logger = new Mock<ILogger<T>>().Object;
            management.StorageContext = moqs.StorageContext.Object;
            management.Cache = moqs.CacheProvider.Object;
        }

        public static void SetupCronJob(this Moqs moqs, CronJobBase cronJob)
        {
            cronJob.StorageContext = moqs.StorageContext.Object;
            cronJob.CacheProvider = moqs.CacheProvider.Object;
            cronJob.LoggerFactory = moqs.LoggerFactory.Object;
        }

        public static void SetupNonCurrentCronJob(this Moqs moqs, NonConcurrentCronJob cronJob)
        {
            cronJob.StorageContext = moqs.StorageContext.Object;
            cronJob.CacheProvider = moqs.CacheProvider.Object;
            cronJob.LoggerFactory = moqs.LoggerFactory.Object;
            cronJob.MutexProvider = moqs.MutexProvider.Object;

            var jobName = cronJob.GetType().Name;
            moqs.CronJobTable.Setup(c => c.RetrieveAsync(jobName, jobName, It.IsAny<CancellationToken>())).ReturnsAsync(default(CronJobEntity));
            moqs.MutexProvider.Setup(p => p.GetInstance($"CronJob/{jobName}.lock")).Returns(moqs.Mutex.Object);
            moqs.Mutex.Setup(m => m.TryLockAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Mock<IMutexContext>().Object);
            moqs.CronJobTable.Setup(c => c.InsertOrReplaceAsync(
                It.Is<CronJobEntity>(c => c.PartitionKey == jobName && c.RowKey == jobName),
                It.IsAny<CancellationToken>())
            );
            moqs.Mutex.Setup(m => m.TryReleaseAsync(It.IsAny<CancellationToken>()));
        }
    }
}
