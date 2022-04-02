using Kaiyuanshe.OpenHackathon.Server.Biz;
using Kaiyuanshe.OpenHackathon.Server.CronJobs.Jobs;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.ServerTests.CronJobs
{
    internal class CleanupK8SJobTests
    {
        [Test]
        public async Task ExecuteAsync()
        {
            // data
            var hackathons = new List<HackathonEntity>
            {
                new HackathonEntity{ PartitionKey="hack"},
            };

            // mock
            var experimentManagement = new Mock<IExperimentManagement>();
            experimentManagement.Setup(e => e.CleanupKubernetesExperimentsAsync("hack", It.IsAny<CancellationToken>()));
            experimentManagement.Setup(e => e.CleanupKubernetesTemplatesAsync("hack", It.IsAny<CancellationToken>()));
            var storageContext = new MockStorageContext();
            storageContext.HackathonTable.Setup(h => h.ExecuteQueryAsync(
                "(ExperimentCleaned eq false) and (ReadOnly eq true)",
                It.IsAny<Func<HackathonEntity, Task>>(), null, null, It.IsAny<CancellationToken>()))
                .Callback<string, Func<HackathonEntity, Task>, int?, IEnumerable<string>, CancellationToken>(
                (f, func, l, s, t) =>
                {
                    foreach (var h in hackathons)
                    {
                        func(h);
                    }
                });
            storageContext.HackathonTable.Setup(h => h.MergeAsync(
                It.Is<HackathonEntity>(e => e.PartitionKey == "hack" && e.ExperimentCleaned == true),
                It.IsAny<CancellationToken>()));

            var job = new CleanupK8SJob
            {
                ExperimentManagement = experimentManagement.Object,
                StorageContext = storageContext.Object,
            };
            await job.ExecuteNow(null);

            Mock.VerifyAll(experimentManagement);
            experimentManagement.Verify(e => e.CleanupKubernetesExperimentsAsync("hack", It.IsAny<CancellationToken>()), Times.Once);
            experimentManagement.Verify(e => e.CleanupKubernetesTemplatesAsync("hack", It.IsAny<CancellationToken>()), Times.Once);

            storageContext.VerifyAll();
        }
    }
}
