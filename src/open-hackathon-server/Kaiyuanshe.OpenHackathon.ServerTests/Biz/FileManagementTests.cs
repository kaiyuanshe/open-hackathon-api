using Azure.Storage.Sas;
using Kaiyuanshe.OpenHackathon.Server.Auth;
using Kaiyuanshe.OpenHackathon.Server.Biz;
using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Storage;
using Kaiyuanshe.OpenHackathon.Server.Storage.BlobContainers;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.ServerTests.Biz
{
    public class FileManagementTests
    {
        #region GetSASExpirationMinitues
        [TestCase(null, 5)]
        [TestCase(31, 30)]
        [TestCase(1, 2)]
        [TestCase(-1, 2)]
        [TestCase(5, 5)]
        public void GetSASExpirationMinitues(int? requestedExpiration, int expectedExpiration)
        {
            var request = new FileUpload { expiration = requestedExpiration };
            var fileManagement = new FileManagement();
            Assert.AreEqual(expectedExpiration, fileManagement.GetSASExpirationMinitues(request));
        }
        #endregion

        #region GetUploadUrlAsync
        [Test]
        public async Task GetUploadUrlAsync()
        {
            var user = new ClaimsPrincipal(
               new ClaimsIdentity(new List<Claim>
               {
                    new Claim(AuthConstant.ClaimType.UserId, "userId")
               }));
            var request = new FileUpload { filename = "dir/abc.png" };
            var sas = "?sas";
            string blobName = $"userId/{DateTime.UtcNow.ToString("yyyy/MM/dd")}/dir/abc.png";

            // mock
            var userBlobContainer = new Mock<IUserBlobContainer>();
            userBlobContainer.Setup(c => c.CreateBlobSasToken(
                blobName,
                BlobSasPermissions.Read | BlobSasPermissions.Write
                , It.IsAny<DateTimeOffset>()))
                .Returns(sas);
            userBlobContainer.SetupGet(u => u.BlobContainerUri).Returns("https://contoso.com");
            var storageContext = new Mock<IStorageContext>();
            storageContext.SetupGet(s => s.UserBlobContainer).Returns(userBlobContainer.Object);

            // test
            var fileManagement = new FileManagement()
            {
                StorageContext = storageContext.Object,
            };
            var result = await fileManagement.GetUploadUrlAsync(user, request, default);

            // verify
            Mock.VerifyAll(userBlobContainer, storageContext);
            userBlobContainer.VerifyNoOtherCalls();
            storageContext.VerifyNoOtherCalls();

            Assert.AreEqual(5, result.expiration);
            Assert.AreEqual("dir/abc.png", result.filename);
            Assert.AreEqual($"{FileManagement.HackathonApiStaticSite}/{blobName}", result.url);
            Assert.AreEqual($"https://contoso.com/$web/{blobName}?sas", result.uploadUrl);
        }
        #endregion
    }
}
