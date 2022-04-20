using Kaiyuanshe.OpenHackathon.Server.Storage;
using Kaiyuanshe.OpenHackathon.Server.Storage.Tables;
using Moq;

namespace Kaiyuanshe.OpenHackathon.ServerTests
{
    internal class MockStorageContext
    {
        Mock<IStorageContext> StorageContext;
        public Mock<IActivityLogTable> ActivityLogTable { get; set; }
        public Mock<IExperimentTable> ExperimentTable { get; }
        public Mock<IHackathonTable> HackathonTable { get; }
        public Mock<IHackathonAdminTable> HackathonAdminTable { get; }

        public MockStorageContext()
        {
            ActivityLogTable = new Mock<IActivityLogTable>();
            ExperimentTable = new Mock<IExperimentTable>();
            HackathonTable = new Mock<IHackathonTable>();
            HackathonAdminTable = new Mock<IHackathonAdminTable>();

            StorageContext = new Mock<IStorageContext>();
            StorageContext.Setup(p => p.ActivityLogTable).Returns(ActivityLogTable.Object);
            StorageContext.Setup(p => p.ExperimentTable).Returns(ExperimentTable.Object);
            StorageContext.Setup(p => p.HackathonTable).Returns(HackathonTable.Object);
            StorageContext.Setup(p => p.HackathonAdminTable).Returns(HackathonAdminTable.Object);
        }

        public IStorageContext Object => StorageContext.Object;

        public void VerifyAll()
        {
            Mock.VerifyAll(ActivityLogTable, ExperimentTable, HackathonTable, HackathonAdminTable);

            ActivityLogTable.VerifyNoOtherCalls();
            ExperimentTable.VerifyNoOtherCalls();
            HackathonTable.VerifyNoOtherCalls();
            HackathonAdminTable?.VerifyNoOtherCalls();
        }
    }
}
