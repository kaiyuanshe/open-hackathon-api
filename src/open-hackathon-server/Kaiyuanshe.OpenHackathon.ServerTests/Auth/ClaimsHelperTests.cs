using Kaiyuanshe.OpenHackathon.Server.Auth;
using NUnit.Framework;
using System.Collections.Generic;
using System.Security.Claims;

namespace Kaiyuanshe.OpenHackathon.ServerTests.Auth
{
    [TestFixture]
    public class ClaimsHelperTests
    {
        [Test]
        public void IsPlatformAdministratorTest()
        {
            Assert.IsFalse(ClaimsHelper.IsPlatformAdministrator(null));

            Assert.IsFalse(ClaimsHelper.IsPlatformAdministrator(new ClaimsPrincipal(
                new ClaimsIdentity())));

            Assert.IsFalse(ClaimsHelper.IsPlatformAdministrator(new ClaimsPrincipal(
                new ClaimsIdentity(new List<Claim> { }))));

            Assert.IsFalse(ClaimsHelper.IsPlatformAdministrator(new ClaimsPrincipal(
                new ClaimsIdentity(new List<Claim>
                {
                    new Claim("type", "value")
                }))));

            Assert.IsTrue(ClaimsHelper.IsPlatformAdministrator(new ClaimsPrincipal(
               new ClaimsIdentity(new List<Claim>
               {
                    new Claim("type", "value"),
                    new Claim(AuthConstant.ClaimType.PlatformAdministrator, "foo")
               }))));

        }

        [Test]
        public void GetUserIdTest()
        {
            Assert.AreEqual(string.Empty, ClaimsHelper.GetUserId(null));

            Assert.AreEqual(string.Empty, ClaimsHelper.GetUserId(new ClaimsPrincipal()));

            Assert.AreEqual(string.Empty, ClaimsHelper.GetUserId(new ClaimsPrincipal(new ClaimsIdentity())));

            Assert.AreEqual(string.Empty, ClaimsHelper.GetUserId(new ClaimsPrincipal(new ClaimsIdentity(new List<Claim> { }))));

            Assert.AreEqual(string.Empty, ClaimsHelper.GetUserId(new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new Claim("type", "value")
            }))));

            Assert.AreEqual("uid", ClaimsHelper.GetUserId(new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new Claim("type", "value"),
                new Claim(AuthConstant.ClaimType.UserId, "uid"),
            }))));
        }

        [Test]
        public void GetUserDisplayNameTest()
        {
            Assert.AreEqual(string.Empty, ClaimsHelper.GetUserDisplayName(null));

            Assert.AreEqual(string.Empty, ClaimsHelper.GetUserDisplayName(new ClaimsPrincipal()));

            Assert.AreEqual(string.Empty, ClaimsHelper.GetUserDisplayName(new ClaimsPrincipal(new ClaimsIdentity())));

            Assert.AreEqual(string.Empty, ClaimsHelper.GetUserDisplayName(new ClaimsPrincipal(new ClaimsIdentity(new List<Claim> { }))));

            Assert.AreEqual(string.Empty, ClaimsHelper.GetUserDisplayName(new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new Claim("type", "value"),
                new Claim(AuthConstant.ClaimType.UserId, "uid"),
            }))));

            Assert.AreEqual("dn", ClaimsHelper.GetUserDisplayName(new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new Claim("type", "value"),
                new Claim(AuthConstant.ClaimType.UserId, "uid"),
                new Claim(AuthConstant.ClaimType.UserDisplayName, "dn"),
            }))));
        }
    }
}
