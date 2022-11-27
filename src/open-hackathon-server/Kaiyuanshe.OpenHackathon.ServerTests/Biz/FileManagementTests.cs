using Azure.Storage.Sas;
using Kaiyuanshe.OpenHackathon.Server.Auth;
using Kaiyuanshe.OpenHackathon.Server.Biz;
using Kaiyuanshe.OpenHackathon.Server.Models;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Security.Claims;

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
        [TestCase(null)]
        [TestCase("")]
        [TestCase("https://www.foo.com")]
        [TestCase("https://www.foo.com/")]
        public void GetUploadUrlAsync(string host)
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
            var moqs = new Moqs();
            moqs.UserBlobContainer.Setup(c => c.CreateBlobSasToken(
                blobName,
                BlobSasPermissions.Read | BlobSasPermissions.Write
                , It.IsAny<DateTimeOffset>()))
                .Returns(sas);
            moqs.UserBlobContainer.SetupGet(u => u.BlobContainerUri).Returns("https://my.blob.core.chinacloudapi.cn");
            var configuration = MockHelper.CreateConfiguration(new Dictionary<string, string>
            {
                {"Storage:StaticWebSiteHost", host},
            });

            // test
            var fileManagement = new FileManagement
            {
                Configuration = configuration,
            };
            moqs.SetupManagement(fileManagement);
            var result = fileManagement.GetUploadUrl(user, request);

            // verify
            moqs.VerifyAll();

            Assert.AreEqual(5, result.expiration);
            Assert.AreEqual("dir/abc.png", result.filename);
            var expected = string.IsNullOrEmpty(host) ?
                $"https://my.z4.web.core.chinacloudapi.cn/{blobName}" :
                $"https://www.foo.com/{blobName}";
            Assert.AreEqual(expected, result.url);
            Assert.AreEqual($"https://my.blob.core.chinacloudapi.cn/$web/{blobName}?sas", result.uploadUrl);
        }
        #endregion
    }
}
