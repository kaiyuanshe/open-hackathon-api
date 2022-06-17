﻿using Authing.ApiClient.Types;
using Kaiyuanshe.OpenHackathon.Server;
using Kaiyuanshe.OpenHackathon.Server.Biz;
using Kaiyuanshe.OpenHackathon.Server.Biz.Options;
using Kaiyuanshe.OpenHackathon.Server.Controllers;
using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.ResponseBuilder;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.ServerTests.Controllers
{
    [TestFixture]
    public class UserControllerTests
    {
        #region AuthingTestWithInvalidToken
        [Test]
        public async Task AuthingTestWithInvalidToken()
        {
            // input
            var parameter = new UserInfo { Token = "token", UserPoolId = "pool" };
            var jwtTokenStatus = new JWTTokenStatus { Status = false, Code = 400, Message = "Some Message" };

            // Moq
            var loginManagerMoq = new Mock<IUserManagement>();
            loginManagerMoq.Setup(p => p.ValidateTokenRemotelyAsync("pool", "token", default)).ReturnsAsync(jwtTokenStatus);

            // test
            var controller = new UserController
            {
                UserManagement = loginManagerMoq.Object,
                ProblemDetailsFactory = new CustomProblemDetailsFactory(),
            };
            var resp = await controller.Authing(parameter, default);

            // Verify
            Mock.VerifyAll(loginManagerMoq);
            loginManagerMoq.VerifyNoOtherCalls();

            AssertHelper.AssertObjectResult(resp, 400, p =>
            {
                Assert.IsTrue(p.Detail.Contains("Some Message"));
            });
        }
        #endregion

        #region GetUserById
        [Test]
        public async Task GetUserById_NotFound()
        {
            string userId = "uid";
            UserInfo userInfo = null;

            // mock
            var userManagement = new Mock<IUserManagement>();
            userManagement.Setup(u => u.GetUserByIdAsync(userId, default))
                .ReturnsAsync(userInfo);

            // test
            var controller = new UserController
            {
                UserManagement = userManagement.Object,
                ProblemDetailsFactory = new CustomProblemDetailsFactory(),
            };
            var result = await controller.GetUserById(userId, default);

            // verify
            Mock.VerifyAll(userManagement);
            userManagement.VerifyNoOtherCalls();

            AssertHelper.AssertObjectResult(result, 404, Resources.User_NotFound);
        }

        [Test]
        public async Task GetUserById()
        {
            string userId = "uid";
            UserInfo userInfo = new UserInfo { };

            // mock
            var userManagement = new Mock<IUserManagement>();
            userManagement.Setup(u => u.GetUserByIdAsync(userId, default))
                .ReturnsAsync(userInfo);

            // test
            var controller = new UserController
            {
                UserManagement = userManagement.Object,
            };
            var result = await controller.GetUserById(userId, default);

            // verify
            Mock.VerifyAll(userManagement);
            userManagement.VerifyNoOtherCalls();
            var resp = AssertHelper.AssertOKResult<UserInfo>(result);
            Assert.AreEqual(resp, userInfo);
        }

        #endregion

        #region SearchUser
        [Test]
        public async Task SearchUser()
        {
            var entities = new List<UserEntity>
            {
                new UserEntity
                {
                    Name="name"
                }
            };

            // mock
            var moqs = new Moqs();
            moqs.UserManagement.Setup(p => p.SearchUserAsync(It.Is<UserQueryOptions>(o => o.Search == "s" && o.Top == 3), default)).ReturnsAsync(entities);

            // test
            var controller = new UserController();
            moqs.SetupController(controller);
            var result = await controller.SearchUser("s", 3, default);

            // verify
            Mock.VerifyAll(moqs.UserManagement);
            moqs.UserManagement.VerifyNoOtherCalls();

            var users = AssertHelper.AssertOKResult<UserInfoList>(result);
            Assert.AreEqual(1, users.value.Length);
            Assert.AreEqual("name", users.value[0].Name);
        }
        #endregion

        #region GetUploadUrl
        [Test]
        public async Task GetUploadUrl()
        {
            // input
            var parameter = new FileUpload { };
            var uploaded = new FileUpload { expiration = 10, filename = "user/avatar.jpg" };

            // mock
            var moqs = new Moqs();
            moqs.FileManagement.Setup(u => u.GetUploadUrlAsync(It.IsAny<ClaimsPrincipal>(), parameter, default)).ReturnsAsync(uploaded);
            moqs.ActivityLogManagement.Setup(u => u.LogUserActivity("", null, "", ActivityLogType.fileUpload, It.IsAny<object>(), null, default));

            // test
            var controller = new UserController();
            moqs.SetupController(controller);
            var result = await controller.GetUploadUrl(parameter, default);

            // verify
            moqs.VerifyAll();
            FileUpload resp = AssertHelper.AssertOKResult<FileUpload>(result);
            Assert.AreEqual(10, resp.expiration.Value);
            Assert.AreEqual("user/avatar.jpg", resp.filename);
        }
        #endregion
    }
}
