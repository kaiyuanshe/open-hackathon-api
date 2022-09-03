using Azure;
using Kaiyuanshe.OpenHackathon.Server;
using Kaiyuanshe.OpenHackathon.Server.Auth;
using Kaiyuanshe.OpenHackathon.Server.Biz;
using Kaiyuanshe.OpenHackathon.Server.Biz.Options;
using Kaiyuanshe.OpenHackathon.Server.Cache;
using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Storage;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Kaiyuanshe.OpenHackathon.Server.Storage.Tables;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.ServerTests.Biz
{
    [TestFixture]
    public class UserManagementTests
    {
        #region AuthingAsync
        [Test]
        public async Task AuthingAsyncTest()
        {
            // input
            var userInfo = new UserInfo
            {
                Id = "id",
                Token = "token",
                Nickname = "nickname"
            };
            var cancellationToken = new CancellationTokenSource().Token;

            // moc
            var storage = new Mock<IStorageContext>();
            var usertable = new Mock<IUserTable>();
            var tokenTable = new Mock<IUserTokenTable>();
            storage.SetupGet(s => s.UserTable).Returns(usertable.Object);
            storage.SetupGet(s => s.UserTokenTable).Returns(tokenTable.Object);
            usertable.Setup(u => u.SaveUserAsync(userInfo, cancellationToken));
            tokenTable.Setup(t => t.InsertOrReplaceAsync(It.Is<UserTokenEntity>(u =>
                u.UserId == "id"
                && u.Token == "token"
                && u.UserDisplayName == "nickname"), cancellationToken));
            var cache = new DefaultCacheProvider(null);

            // test
            var userMgmt = new UserManagement
            {
                StorageContext = storage.Object,
                Cache = cache,
            };
            await userMgmt.AuthingAsync(userInfo, cancellationToken);

            //verify
            Mock.VerifyAll(storage, usertable, tokenTable);
            usertable.VerifyNoOtherCalls();
            tokenTable.VerifyNoOtherCalls();
        }
        #endregion

        #region GetCurrentUserRemotelyAsync
        [TestCase(null, "token")]
        [TestCase("", "token")]
        [TestCase("pool", null)]
        [TestCase("pool", "")]
        public void GetCurrentUserRemotelyAsyncTest(string userPoolId, string accessToken)
        {
            var userMgmt = new UserManagement();
            Assert.ThrowsAsync<ArgumentNullException>(() => userMgmt.GetCurrentUserRemotelyAsync(userPoolId, accessToken));
        }
        #endregion

        #region GetUserBasicClaimsAsync
        [Test]
        public async Task GetCurrentUserClaimsAsyncTestInvalidToken1()
        {
            string token = "token";
            UserTokenEntity? tokenEntity = null;

            var userMgmtMock = new Mock<UserManagement> { CallBase = true };
            userMgmtMock.Setup(m => m.GetTokenEntityAsync(token, default)).ReturnsAsync(tokenEntity);

            var claims = await userMgmtMock.Object.GetUserBasicClaimsAsync(token, default);

            Mock.VerifyAll(userMgmtMock);
            Assert.AreEqual(0, claims.Count());
        }

        [Test]
        public async Task GetCurrentUserClaimsAsyncTestInvalidToken2()
        {
            string token = "token";
            UserTokenEntity tokenEntity = new UserTokenEntity
            {
                TokenExpiredAt = DateTime.UtcNow.AddMinutes(-1)
            };

            var userMgmtMock = new Mock<UserManagement> { CallBase = true };
            userMgmtMock.Setup(m => m.GetTokenEntityAsync(token, default)).ReturnsAsync(tokenEntity);

            var claims = await userMgmtMock.Object.GetUserBasicClaimsAsync(token, default);

            Mock.VerifyAll(userMgmtMock);
            Assert.AreEqual(0, claims.Count());
        }

        [Test]
        public async Task GetCurrentUserClaimsAsyncTestPlatformAdmin()
        {
            string token = "token";
            string userId = "userid";
            UserTokenEntity tokenEntity = new UserTokenEntity
            {
                TokenExpiredAt = DateTime.UtcNow.AddMinutes(1),
                UserId = userId,
                UserDisplayName = "dn"
            };
            Claim claim = new Claim("type", "value", "valueType", "issuer");

            var userMgmtMock = new Mock<UserManagement> { CallBase = true };
            userMgmtMock.Setup(m => m.GetTokenEntityAsync(token, default)).ReturnsAsync(tokenEntity);
            userMgmtMock.Setup(m => m.GetPlatformAdminClaim(userId, default)).ReturnsAsync(claim);

            var claims = await userMgmtMock.Object.GetUserBasicClaimsAsync(token, default);

            Mock.VerifyAll(userMgmtMock);
            Assert.AreEqual(3, claims.Count());

            // user id
            Assert.AreEqual(AuthConstant.ClaimType.UserId, claims.First().Type);
            Assert.AreEqual(userId, claims.First().Value);
            Assert.AreEqual(ClaimValueTypes.String, claims.First().ValueType);
            Assert.AreEqual(AuthConstant.Issuer.Default, claims.First().Issuer);

            // user displayName
            Assert.AreEqual(AuthConstant.ClaimType.UserDisplayName, claims.ElementAt(1).Type);
            Assert.AreEqual("dn", claims.ElementAt(1).Value);
            Assert.AreEqual(ClaimValueTypes.String, claims.ElementAt(1).ValueType);
            Assert.AreEqual(AuthConstant.Issuer.Default, claims.ElementAt(1).Issuer);

            // platform admin
            Assert.AreEqual("type", claims.Last().Type);
            Assert.AreEqual("value", claims.Last().Value);
            Assert.AreEqual("valueType", claims.Last().ValueType);
            Assert.AreEqual("issuer", claims.Last().Issuer);
        }
        #endregion

        #region GetTokenEntityAsync
        [Test]
        public async Task GetTokenEntityAsyncTest()
        {
            // input
            var cToken = new CancellationTokenSource().Token;
            var jwt = "whatever";
            var hash = "ae3d347982977b422948b64011ac14ac76c9ab15898fb562a66a136733aa645fb3a9ccd9bee00cc578c2f44f486af47eb254af7c174244086d174cc52341e63a";
            var tokenEntity = new UserTokenEntity
            {
                UserId = "1",
            };

            // moq
            var storage = new Mock<IStorageContext>();
            var tokenTable = new Mock<IUserTokenTable>();
            storage.SetupGet(s => s.UserTokenTable).Returns(tokenTable.Object);
            tokenTable.Setup(t => t.RetrieveAsync(hash, string.Empty, cToken)).ReturnsAsync(tokenEntity);

            // test
            var userMgmt = new UserManagement
            {
                StorageContext = storage.Object,
            };
            var resp = await userMgmt.GetTokenEntityAsync(jwt, cToken);

            // verify
            Mock.VerifyAll();
            tokenTable.Verify(t => t.RetrieveAsync(hash, string.Empty, cToken), Times.Once);
            tokenTable.VerifyNoOtherCalls();
            Debug.Assert(resp != null);
            Assert.AreEqual("1", resp.UserId);
        }
        #endregion

        #region ValidateTokenAsync
        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public async Task ValidateTokenAsyncTestRequired(string token)
        {
            var userMgmt = new UserManagement();
            var result = await userMgmt.ValidateTokenAsync(token);

            Assert.AreNotEqual(ValidationResult.Success, result);
            Assert.AreEqual(Resources.Auth_Unauthorized, result.ErrorMessage);
        }

        [Test]
        public async Task ValidateTokenAsyncTestNotExist()
        {
            // input
            CancellationToken cancellationToken = CancellationToken.None;
            string token = "whatever";
            string hash = "ae3d347982977b422948b64011ac14ac76c9ab15898fb562a66a136733aa645fb3a9ccd9bee00cc578c2f44f486af47eb254af7c174244086d174cc52341e63a";
            UserTokenEntity? tokenEntity = null;

            // moq
            var moqs = new Moqs();
            moqs.UserTokenTable.Setup(t => t.RetrieveAsync(hash, string.Empty, cancellationToken)).ReturnsAsync(tokenEntity);

            // testing
            var userMgmt = new UserManagement();
            moqs.SetupManagement(userMgmt);
            userMgmt.Cache = new DefaultCacheProvider(new Mock<ILogger<DefaultCacheProvider>>().Object);
            var result = await userMgmt.ValidateTokenAsync(token);

            // verify
            moqs.VerifyAll();
            Assert.AreNotEqual(ValidationResult.Success, result);
            Debug.Assert(result != null);
            Assert.AreEqual(Resources.Auth_Unauthorized, result.ErrorMessage);
        }

        [Test]
        public async Task ValidateTokenAsyncTestExpired()
        {
            // input
            CancellationToken cancellationToken = CancellationToken.None;
            string token = "whatever";
            string hash = "ae3d347982977b422948b64011ac14ac76c9ab15898fb562a66a136733aa645fb3a9ccd9bee00cc578c2f44f486af47eb254af7c174244086d174cc52341e63a";
            UserTokenEntity tokenEntity = new UserTokenEntity
            {
                TokenExpiredAt = DateTime.UtcNow.AddDays(-1),
            };

            // moq
            var storage = new Mock<IStorageContext>();
            var tokenTable = new Mock<IUserTokenTable>();
            storage.SetupGet(s => s.UserTokenTable).Returns(tokenTable.Object);
            tokenTable.Setup(t => t.RetrieveAsync(hash, string.Empty, cancellationToken)).ReturnsAsync(tokenEntity);

            // testing
            var userMgmt = new UserManagement
            {
                StorageContext = storage.Object,
                Cache = new DefaultCacheProvider(new Mock<ILogger<DefaultCacheProvider>>().Object),
            };
            var result = await userMgmt.ValidateTokenAsync(token);

            // verify
            Mock.VerifyAll();
            tokenTable.Verify(t => t.RetrieveAsync(hash, string.Empty, cancellationToken), Times.Once);
            tokenTable.VerifyNoOtherCalls();
            Assert.AreNotEqual(ValidationResult.Success, result);
            Debug.Assert(result != null);
            Debug.Assert(result.ErrorMessage != null);
            Assert.IsTrue(result.ErrorMessage.Contains("expired"));
        }

        [Test]
        public async Task ValidateTokenAsyncTestValid()
        {
            // input
            string token = "whatever";
            string hash = "ae3d347982977b422948b64011ac14ac76c9ab15898fb562a66a136733aa645fb3a9ccd9bee00cc578c2f44f486af47eb254af7c174244086d174cc52341e63a";
            UserTokenEntity tokenEntity = new UserTokenEntity
            {
                TokenExpiredAt = DateTime.UtcNow.AddDays(1),
            };

            // moq
            var moqs = new Moqs();
            moqs.UserTokenTable.Setup(t => t.RetrieveAsync(hash, string.Empty, default)).ReturnsAsync(tokenEntity);

            // testing
            var userMgmt = new UserManagement();
            moqs.SetupManagement(userMgmt);
            userMgmt.Cache = new DefaultCacheProvider(new Mock<ILogger<DefaultCacheProvider>>().Object);
            var result = await userMgmt.ValidateTokenAsync(token);

            // verify
            moqs.VerifyAll();
            Assert.AreEqual(ValidationResult.Success, result);
        }

        [Test]
        public void TaskValidateTokenAsyncTestEntityNull()
        {
            UserTokenEntity? tokenEntity = null;

            var userMgmt = new UserManagement();
            var validationResult = userMgmt.ValidateToken(tokenEntity);

            Assert.AreNotEqual(ValidationResult.Success, validationResult);
            Debug.Assert(validationResult != null);
            Assert.AreEqual(Resources.Auth_Unauthorized, validationResult.ErrorMessage);
        }

        [Test]
        public void TaskValidateTokenAsyncTestEntityExpired()
        {
            UserTokenEntity tokenEntity = new UserTokenEntity { TokenExpiredAt = DateTime.UtcNow.AddMinutes(-1) };

            var userMgmt = new UserManagement();
            var validationResult = userMgmt.ValidateToken(tokenEntity);

            Assert.AreNotEqual(ValidationResult.Success, validationResult);
            Debug.Assert(validationResult != null);
            Debug.Assert(validationResult.ErrorMessage != null);
            Assert.IsTrue(validationResult.ErrorMessage.Contains("expired"));
        }

        [Test]
        public void TaskValidateTokenAsyncTestEntityValid()
        {
            UserTokenEntity tokenEntity = new UserTokenEntity { TokenExpiredAt = DateTime.UtcNow.AddHours(1) };

            var userMgmt = new UserManagement();
            var validationResult = userMgmt.ValidateToken(tokenEntity);

            Assert.AreEqual(ValidationResult.Success, validationResult);
        }
        #endregion

        #region ValidateTokenRemotelyAsync
        [TestCase(null, "token")]
        [TestCase("", "token")]
        [TestCase("pool", null)]
        [TestCase("pool", "")]
        public void ValidateTokenRemotelyAsyncTest(string userPoolId, string accessToken)
        {
            var userMgmt = new UserManagement();
            Assert.ThrowsAsync<ArgumentNullException>(() => userMgmt.ValidateTokenRemotelyAsync(userPoolId, accessToken));
        }
        #endregion

        #region ListTopUsers
        private static IEnumerable ListTopUsersTestData()
        {
            // arg0: db results
            // arg1: top
            // arg2: expected result

            Func<int, IEnumerable<TopUserEntity>> GetList = (count) =>
            {
                return Enumerable.Range(0, count).Select(i => new TopUserEntity { PartitionKey = i.ToString() });
            };

            // with top, top < actual
            yield return new TestCaseData(GetList(15), 6, GetList(6));

            // with top, top > actual
            yield return new TestCaseData(GetList(5), 20, GetList(5));

            // without top, default top < actual
            yield return new TestCaseData(GetList(15), null, GetList(10));

            // without top, default top > actual
            yield return new TestCaseData(GetList(4), null, GetList(4));
        }

        [Test, TestCaseSource(nameof(ListTopUsersTestData))]
        public async Task ListTopUsers(IEnumerable<TopUserEntity> dbResults, int? top, IEnumerable<TopUserEntity> expectedOutput)
        {
            var moqs = new Moqs();
            moqs.TopUserTable.Setup(t => t.QueryEntitiesAsync(null, null, default)).ReturnsAsync(dbResults);

            var management = new UserManagement();
            moqs.SetupManagement(management);

            var actual = await management.ListTopUsers(top, default);

            moqs.VerifyAll();
            Assert.AreEqual(actual.Count(), expectedOutput.Count());
            for (int i = 0; i < actual.Count(); i++)
            {
                Assert.AreEqual(actual.ElementAt(i).Rank, expectedOutput.ElementAt(i).Rank);
            }
        }
        #endregion

        #region GetPlatformAdminClaim
        [Test]
        public async Task GetPlatformRoleClaimTestEnityNotFound()
        {
            // input
            CancellationToken cancellationToken = CancellationToken.None;
            string userId = "userid";
            HackathonAdminEntity entity = null;

            // moq
            var storage = new Mock<IStorageContext>();
            var hackAdminTable = new Mock<IHackathonAdminTable>();
            storage.SetupGet(s => s.HackathonAdminTable).Returns(hackAdminTable.Object);
            hackAdminTable.Setup(t => t.GetPlatformRole(userId, cancellationToken)).ReturnsAsync(entity);

            // test
            var userMgmt = new UserManagement
            {
                StorageContext = storage.Object,
            };
            var claim = await userMgmt.GetPlatformAdminClaim(userId, cancellationToken);

            // verify
            Mock.VerifyAll(storage, hackAdminTable);
            Assert.IsNull(claim);
        }

        [Test]
        public async Task GetPlatformRoleClaimTestValidClaim()
        {
            // input
            CancellationToken cancellationToken = CancellationToken.None;
            string userId = "userid";
            HackathonAdminEntity entity = new HackathonAdminEntity
            {
                PartitionKey = "",
            };

            // moq
            var storage = new Mock<IStorageContext>();
            var hackAdminTable = new Mock<IHackathonAdminTable>();
            storage.SetupGet(s => s.HackathonAdminTable).Returns(hackAdminTable.Object);
            hackAdminTable.Setup(t => t.GetPlatformRole(userId, cancellationToken)).ReturnsAsync(entity);

            // test
            var userMgmt = new UserManagement
            {
                StorageContext = storage.Object,
            };
            var claim = await userMgmt.GetPlatformAdminClaim(userId, cancellationToken);

            // verify
            Mock.VerifyAll(storage, hackAdminTable);
            Assert.IsNotNull(claim);
            Assert.AreEqual(AuthConstant.ClaimType.PlatformAdministrator, claim.Type);
            Assert.AreEqual(userId, claim.Value);
            Assert.AreEqual(ClaimValueTypes.String, claim.ValueType);
            Assert.AreEqual(AuthConstant.Issuer.Default, claim.Issuer);
        }
        #endregion

        #region GetCurrentUserAsync
        [Test]
        public async Task GetCurrentUserAsync()
        {
            string userId = "uid";
            CancellationToken cancellationToken = CancellationToken.None;
            UserInfo userInfo = new UserInfo { };

            // moc
            var storageContext = new Mock<IStorageContext>();
            var userTable = new Mock<IUserTable>();
            storageContext.SetupGet(s => s.UserTable).Returns(userTable.Object);
            userTable.Setup(u => u.GetUserByIdAsync(userId, cancellationToken)).ReturnsAsync(userInfo);

            // test
            var userManagement = new UserManagement
            {
                StorageContext = storageContext.Object,
                Cache = new DefaultCacheProvider(null),
            };
            var result = await userManagement.GetUserByIdAsync(userId, cancellationToken);

            // verify
            Mock.VerifyAll(storageContext, userTable);
            storageContext.VerifyNoOtherCalls();
            userTable.VerifyNoOtherCalls();
            Assert.AreEqual(result, userInfo);
        }
        #endregion

        #region SearchUserAsync
        private static IEnumerable SearchUserTestData()
        {
            var u1 = new UserEntity { Email = "abc@x.com", };
            var u2 = new UserEntity { Email = "bcd@x.com", };
            var u3 = new UserEntity { Email = "cde@x.com", Name = "name3", };
            var u4 = new UserEntity { Email = "def@x.com", Nickname = "Nickname4" };

            // arg0: users
            // arg1: UserQueryOptions
            // arg2: expected resp

            // top=1
            yield return new TestCaseData(
                    new Dictionary<string, Page<UserEntity>>
                    {
                        [""] = Page<UserEntity>.FromValues(new List<UserEntity> { u1, u2, }, "", new Mock<Response>().Object)
                    },
                    new UserQueryOptions { Top = 1, Search = "b" },
                    new List<UserEntity> { u1 }
                );

            // search
            yield return new TestCaseData(
                    new Dictionary<string, Page<UserEntity>>
                    {
                        [""] = Page<UserEntity>.FromValues(new List<UserEntity> { u1, u2, u3, u4 }, "", new Mock<Response>().Object)
                    },
                    new UserQueryOptions { Top = 100, Search = "bc" },
                    new List<UserEntity> { u1, u2 }
                );

            // search by mail
            yield return new TestCaseData(
                    new Dictionary<string, Page<UserEntity>>
                    {
                        [""] = Page<UserEntity>.FromValues(new List<UserEntity> { u1, u2, u3, u4 }, "", new Mock<Response>().Object)
                    },
                    new UserQueryOptions { Top = 100, Search = "c@" },
                    new List<UserEntity> { u1 }
                );

            // search by name
            yield return new TestCaseData(
                    new Dictionary<string, Page<UserEntity>>
                    {
                        [""] = Page<UserEntity>.FromValues(new List<UserEntity> { u1, u2, u3, u4 }, "", new Mock<Response>().Object)
                    },
                    new UserQueryOptions { Top = 100, Search = "Name" },
                    new List<UserEntity> { u3, u4 }
                );

            // multiple pages
            yield return new TestCaseData(
                    new Dictionary<string, Page<UserEntity>>
                    {
                        [""] = Page<UserEntity>.FromValues(new List<UserEntity> { u1, u2, }, "next", new Mock<Response>().Object),
                        ["next"] = Page<UserEntity>.FromValues(new List<UserEntity> { u3, u4 }, "", new Mock<Response>().Object),
                    },
                    new UserQueryOptions { Top = 100, Search = "Name" },
                    new List<UserEntity> { u3, u4 }
                );
        }

        [Test, TestCaseSource(nameof(SearchUserTestData))]
        public async Task SearchUserAsync(Dictionary<string, Page<UserEntity>> users, UserQueryOptions options, List<UserEntity> expected)
        {
            // mock
            var cache = new Mock<ICacheProvider>();
            foreach (var user in users)
            {
                cache.Setup(c => c.GetOrAddAsync(It.Is<CacheEntry<Page<UserEntity>>>(e => e.CacheKey == $"User-list-{user.Key}" && e.AutoRefresh == false), default))
                    .ReturnsAsync(user.Value);
            }

            // test
            var userManagement = new UserManagement
            {
                Cache = cache.Object,
            };
            var resp = await userManagement.SearchUserAsync(options, default);

            // verify
            Assert.AreEqual(expected.Count, resp.Count());
            for (int i = 0; i < resp.Count(); i++)
            {
                Assert.AreEqual(expected[i].Email, resp.ElementAt(i).Email);
            }
            Mock.VerifyAll(cache);
            cache.VerifyNoOtherCalls();
        }
        #endregion
    }
}