using Kaiyuanshe.OpenHackathon.Server.Storage.BlobContainers;
using Kaiyuanshe.OpenHackathon.Server.Storage.Tables;

namespace Kaiyuanshe.OpenHackathon.Server.Storage
{
    public interface IStorageContext
    {
        IStorageAccountProvider StorageAccountProvider { get; set; }

        IAwardTable AwardTable { get; set; }
        IAwardAssignmentTable AwardAssignmentTable { get; set; }
        IEnrollmentTable EnrollmentTable { get; set; }
        IExperimentTable ExperimentTable { get; set; }
        IHackathonTable HackathonTable { get; set; }
        IHackathonAdminTable HackathonAdminTable { get; set; }
        IJudgeTable JudgeTable { get; set; }
        IRatingTable RatingTable { get; set; }
        IRatingKindTable RatingKindTable { get; set; }
        ITeamTable TeamTable { get; set; }
        ITeamMemberTable TeamMemberTable { get; set; }
        ITemplateTable TemplateTable { get; set; }
        ITeamWorkTable TeamWorkTable { get; set; }
        IUserTable UserTable { get; set; }
        IUserTokenTable UserTokenTable { get; set; }
        IUserBlobContainer UserBlobContainer { get; set; }
        IKubernetesBlobContainer KubernetesBlobContainer { get; set; }
    }


    public class StorageContext : IStorageContext
    {
        public IStorageAccountProvider StorageAccountProvider { get; set; }

        public IAwardTable AwardTable { get; set; }
        public IAwardAssignmentTable AwardAssignmentTable { get; set; }
        public IEnrollmentTable EnrollmentTable { get; set; }
        public IExperimentTable ExperimentTable { get; set; }
        public IHackathonAdminTable HackathonAdminTable { get; set; }
        public IHackathonTable HackathonTable { get; set; }
        public IJudgeTable JudgeTable { get; set; }
        public IRatingTable RatingTable { get; set; }
        public IRatingKindTable RatingKindTable { get; set; }
        public ITeamTable TeamTable { get; set; }
        public ITeamMemberTable TeamMemberTable { get; set; }
        public ITeamWorkTable TeamWorkTable { get; set; }
        public ITemplateTable TemplateTable { get; set; }
        public IUserTable UserTable { get; set; }
        public IUserTokenTable UserTokenTable { get; set; }
        public IUserBlobContainer UserBlobContainer { get; set; }
        public IKubernetesBlobContainer KubernetesBlobContainer { get; set; }

        public StorageContext(IStorageAccountProvider storageAccountProvider)
        {
            StorageAccountProvider = storageAccountProvider;

            // tables
            var storageAccount = storageAccountProvider.HackathonServerStorage;
            TeamWorkTable = new TeamWorkTable(storageAccount, TableNames.TeamWork);
            TemplateTable = new TemplateTable(storageAccount, TableNames.Template);
            UserTable = new UserTable(storageAccount, TableNames.User);
            UserTokenTable = new UserTokenTable(storageAccount, TableNames.UserToken);

            // blob containers
            UserBlobContainer = new UserBlobContainer(storageAccount, BlobContainerNames.StaticWebsite);
            KubernetesBlobContainer = new KubernetesBlobContainer(storageAccount, BlobContainerNames.Kubernetes);
        }
    }
}
