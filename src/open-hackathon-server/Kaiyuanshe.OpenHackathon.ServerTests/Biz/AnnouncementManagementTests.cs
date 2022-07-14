using Kaiyuanshe.OpenHackathon.Server.Biz;
using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Moq;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.ServerTests.Biz
{
    internal class AnnouncementManagementTests
    {
        #region Create
        [Test]
        public async Task Create()
        {
            var parameter = new Announcement
            {
                hackathonName = "hack",
                title = "title",
                content = "content",
            };

            var moqs = new Moqs();
            moqs.AnnouncementTable.Setup(a => a.InsertAsync(It.Is<AnnouncementEntity>(e =>
                e.PartitionKey == "hack"
                && e.RowKey.Length == 36
                && e.Content == "content"
                && e.Title == "title"
                && e.CreatedAt != default), default));
            moqs.CacheProvider.Setup(c => c.Remove("Announcement-hack"));

            var managementClient = new AnnouncementManagement();
            moqs.SetupManagement(managementClient);
            var resp = await managementClient.Create(parameter, default);

            moqs.VerifyAll();
            Assert.IsNotNull(resp);
            Assert.AreEqual("hack", resp.HackathonName);
        }
        #endregion
    }
}
