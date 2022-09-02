using DotLiquid.Util;
using Kaiyuanshe.OpenHackathon.Server.Biz;
using Kaiyuanshe.OpenHackathon.Server.Controllers;
using Kaiyuanshe.OpenHackathon.Server.CronJobs;
using Kaiyuanshe.OpenHackathon.Server.ResponseBuilder;
using Microsoft.Extensions.Logging;
using Moq;

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
    }
}
