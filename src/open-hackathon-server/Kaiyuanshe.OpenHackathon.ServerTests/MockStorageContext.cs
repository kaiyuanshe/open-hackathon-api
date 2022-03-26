using Kaiyuanshe.OpenHackathon.Server.Storage;
using Kaiyuanshe.OpenHackathon.Server.Storage.Tables;
using Moq;

namespace Kaiyuanshe.OpenHackathon.ServerTests
{
    internal class MockStorageContext
    {
        Mock<IStorageContext> StorageContext;
        public Mock<IExperimentTable> ExperimentTable { get; }
        public Mock<IHackathonTable> HackathonTable { get; }

        public MockStorageContext()
        {
            ExperimentTable = new Mock<IExperimentTable>();
            HackathonTable = new Mock<IHackathonTable>();

            StorageContext = new Mock<IStorageContext>();
            StorageContext.SetupGet(p => p.ExperimentTable).Returns(ExperimentTable.Object);
            StorageContext.Setup(p => p.HackathonTable).Returns(HackathonTable.Object);
        }

        public IStorageContext Object => StorageContext.Object;

        public void VerifyAll()
        {
            Mock.VerifyAll(ExperimentTable, HackathonTable);

            ExperimentTable.VerifyNoOtherCalls();
            HackathonTable.VerifyNoOtherCalls();
        }
    }
}
