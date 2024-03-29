﻿using Kaiyuanshe.OpenHackathon.Server.Storage.BlobContainers;
using Kaiyuanshe.OpenHackathon.Server.Storage.Tables;

namespace Kaiyuanshe.OpenHackathon.Server.Storage
{
    public interface IStorageContext
    {
        IActivityLogTable ActivityLogTable { get; set; }
        IAnnouncementTable AnnouncementTable { get; set; }
        IAwardTable AwardTable { get; set; }
        IAwardAssignmentTable AwardAssignmentTable { get; set; }
        ICronJobTable CronJobTable { get; set; }
        IEnrollmentTable EnrollmentTable { get; set; }
        IExperimentTable ExperimentTable { get; set; }
        IHackathonTable HackathonTable { get; set; }
        IHackathonAdminTable HackathonAdminTable { get; set; }
        IJudgeTable JudgeTable { get; set; }
        IOrganizerTable OrganizerTable { get; set; }
        IQuestionnaireTable QuestionnaireTable { get; set; }
        IRatingTable RatingTable { get; set; }
        IRatingKindTable RatingKindTable { get; set; }
        ITeamTable TeamTable { get; set; }
        ITeamMemberTable TeamMemberTable { get; set; }
        ITeamWorkTable TeamWorkTable { get; set; }
        ITemplateRepoTable TemplateRepoTable { get; set; }
        ITemplateTable TemplateTable { get; set; }
        ITopUserTable TopUserTable { get; set; }
        IUserTable UserTable { get; set; }
        IUserTokenTable UserTokenTable { get; set; }

        IKubernetesBlobContainer KubernetesBlobContainer { get; set; }
        IMutexBlobContainer MutexBlobContainer { get; set; }
        IUserBlobContainer UserBlobContainer { get; set; }
        IReportsContainer ReportsContainer { get; set; }
    }

    public class StorageContext : IStorageContext
    {
        public IActivityLogTable ActivityLogTable { get; set; }
        public IAwardTable AwardTable { get; set; }
        public IAnnouncementTable AnnouncementTable { get; set; }
        public IAwardAssignmentTable AwardAssignmentTable { get; set; }
        public ICronJobTable CronJobTable { get; set; }
        public IEnrollmentTable EnrollmentTable { get; set; }
        public IExperimentTable ExperimentTable { get; set; }
        public IHackathonAdminTable HackathonAdminTable { get; set; }
        public IHackathonTable HackathonTable { get; set; }
        public IJudgeTable JudgeTable { get; set; }
        public IOrganizerTable OrganizerTable { get; set; }
        public IQuestionnaireTable QuestionnaireTable { get; set; }
        public IRatingTable RatingTable { get; set; }
        public IRatingKindTable RatingKindTable { get; set; }
        public ITeamTable TeamTable { get; set; }
        public ITeamMemberTable TeamMemberTable { get; set; }
        public ITeamWorkTable TeamWorkTable { get; set; }
        public ITemplateRepoTable TemplateRepoTable { get; set; }
        public ITemplateTable TemplateTable { get; set; }
        public ITopUserTable TopUserTable { get; set; }
        public IUserTable UserTable { get; set; }
        public IUserTokenTable UserTokenTable { get; set; }
        public IKubernetesBlobContainer KubernetesBlobContainer { get; set; }
        public IMutexBlobContainer MutexBlobContainer { get; set; }
        public IReportsContainer ReportsContainer { get; set; }
        public IUserBlobContainer UserBlobContainer { get; set; }
    }
}
