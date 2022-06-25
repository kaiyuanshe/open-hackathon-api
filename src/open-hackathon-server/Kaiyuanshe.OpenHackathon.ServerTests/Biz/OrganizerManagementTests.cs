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
    }
}
