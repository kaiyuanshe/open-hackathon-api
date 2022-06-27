using Kaiyuanshe.OpenHackathon.Server.Biz;
using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Moq;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.ServerTests.Biz
{
    internal class OrganizerManagementTests
    {
        #region CreateOrganizer
        [Test]
        public async Task CreateOrganizer()
        {
            var organizer = new Organizer
            {
                type = OrganizerType.sponsor,
                description = "desc",
                logo = new PictureInfo { description = "d2", name = "n2", uri = "u2" },
                name = "name"
            };

            var moqs = new Moqs();
            moqs.OrganizerTable.Setup(o => o.InsertAsync(It.Is<OrganizerEntity>(e =>
                e.PartitionKey == "hack"
                && e.RowKey.Length == 36
                && e.Type == OrganizerType.sponsor
                && e.Name == "name"
                && e.Description == "desc"
                && e.Logo.description == "d2"
                && e.Logo.name == "n2"
                && e.Logo.uri == "u2"), default));

            var management = new OrganizerManagement();
            moqs.SetupManagement(management);
            await management.CreateOrganizer("hack", organizer, default);

            moqs.VerifyAll();
        }
        #endregion

        #region UpdateOrganizer
        [Test]
        public async Task UpdateOrganizer_NoUpdate()
        {
            var entity = new OrganizerEntity
            {
                Name = "name1",
                Description = "desc1",
                Type = OrganizerType.coorganizer,
                Logo = new PictureInfo { description = "ld1", name = "ln1", uri = "lu1" },
            };
            var organizer = new Organizer();

            var moqs = new Moqs();
            moqs.OrganizerTable.Setup(o => o.MergeAsync(It.Is<OrganizerEntity>(e =>
                e.Type == OrganizerType.coorganizer
                && e.Name == "name1"
                && e.Description == "desc1"
                && e.Logo.description == "ld1"
                && e.Logo.name == "ln1"
                && e.Logo.uri == "lu1"), default));

            var management = new OrganizerManagement();
            moqs.SetupManagement(management);
            await management.UpdateOrganizer(entity, organizer, default);

            moqs.VerifyAll();
        }

        [Test]
        public async Task UpdateOrganizer_UpdateAll()
        {
            var entity = new OrganizerEntity
            {
                Name = "name1",
                Description = "desc1",
                Type = OrganizerType.coorganizer,
                Logo = new PictureInfo { description = "ld1", name = "ln1", uri = "lu1" },
            };
            var organizer = new Organizer
            {
                name = "name2",
                description = "desc2",
                type = OrganizerType.sponsor,
                logo = new PictureInfo { description = "ld2", name = "ln2", uri = "lu2" },
            };

            var moqs = new Moqs();
            moqs.OrganizerTable.Setup(o => o.MergeAsync(It.Is<OrganizerEntity>(e =>
                e.Type == OrganizerType.sponsor
                && e.Name == "name2"
                && e.Description == "desc2"
                && e.Logo.description == "ld2"
                && e.Logo.name == "ln2"
                && e.Logo.uri == "lu2"), default));

            var management = new OrganizerManagement();
            moqs.SetupManagement(management);
            await management.UpdateOrganizer(entity, organizer, default);

            moqs.VerifyAll();
        }
        #endregion
    }
}
