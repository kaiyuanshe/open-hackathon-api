using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.ResponseBuilder;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Kaiyuanshe.OpenHackathon.Server.Storage.Tables;
using Moq;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.ServerTests.Storage
{
    internal class UserTableTests
    {
        [Test]
        public async Task GetUserByIdAsync()
        {
            var entity = new UserEntity();
            var userInfo = new UserInfo { Name = "name" };

            var responseBuilder = new Mock<IResponseBuilder>();
            responseBuilder.Setup(b => b.BuildUser(entity)).Returns(userInfo);

            var userTable = new Mock<UserTable>();
            userTable.Setup(t => t.RetrieveAsync("uid", string.Empty, default)).ReturnsAsync(entity);
            userTable.Object.ResponseBuilder = responseBuilder.Object;

            var resp = await userTable.Object.GetUserByIdAsync("uid", default);

            Assert.AreEqual("name", resp.Name);
            Mock.VerifyAll(responseBuilder, userTable);
            responseBuilder.VerifyNoOtherCalls();
            userTable.VerifyNoOtherCalls();
        }

        [Test]
        public async Task SaveUserAsync()
        {
            var userInfo = new UserInfo
            {
                Name = "name",
                Id = "UID"
            };
            var entity = new UserEntity { Username = "username" };

            var userTable = new Mock<UserTable>();
            userTable.Setup(t => t.InsertOrReplaceAsync(It.Is<UserEntity>(u =>
                u.Name == "name"
                && u.PartitionKey == "uid"
                && u.UserId == "uid"
                && u.RowKey == string.Empty
                && u.ETag == null), default));
            userTable.Setup(u => u.RetrieveAsync("uid", string.Empty, default)).ReturnsAsync(entity);

            var resp = await userTable.Object.SaveUserAsync(userInfo, default);

            Assert.AreEqual("username", resp.Username);
            Mock.VerifyAll(userTable);
            userTable.VerifyNoOtherCalls();
        }
    }
}
