using Authing.ApiClient.Types;
using Kaiyuanshe.OpenHackathon.Server;
using Kaiyuanshe.OpenHackathon.Server.Models;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Kaiyuanshe.OpenHackathon.ServerTests.Helpers
{
    [TestFixture]
    public class TypeHelperTests
    {
        [Test]
        public void AsTest()
        {
            var date = DateTime.Now.AddDays(-10);
            var registerSource = new List<string> { "a", "b" };
            var identities = new List<Identity> { new Identity { Openid = "openid", Provider = "provider" } };
            var roles = new PaginatedRoles { List = new List<Role> { new Role { Arn = "arn" } } };
            UserInfo userInfo = new UserInfo
            {
                Address = "address",
                Blocked = true,
                LoginsCount = 10,
                TokenExpiredAt = date,
                RegisterSource = registerSource,
                Identities = identities,
            };

            var user1 = userInfo.As<User>();
            Assert.AreEqual("address", user1.Address);
            Assert.IsNull(user1.Name);
            Assert.IsNull(user1.Openid);
            Assert.AreEqual(true, user1.Blocked.GetValueOrDefault());
            Assert.AreEqual(false, user1.EmailVerified.HasValue);
            Assert.AreEqual(10, user1.LoginsCount.GetValueOrDefault());
            Assert.AreEqual(date.ToString("o"), user1.TokenExpiredAt);
            Assert.AreEqual(identities, user1.Identities);
            //Assert.AreEqual(roles, user1.Roles);

            var user2 = userInfo.As<User>((u) =>
            {
                u.Name = "name";
            });
            Assert.AreEqual("address", user2.Address);
            Assert.AreEqual("name", user2.Name);
            Assert.IsNull(user2.Openid);
            Assert.AreEqual(true, user2.Blocked.GetValueOrDefault());
            Assert.AreEqual(false, user2.EmailVerified.HasValue);
            Assert.AreEqual(10, user2.LoginsCount.GetValueOrDefault());
            Assert.AreEqual(date.ToString("o"), user2.TokenExpiredAt);
            Assert.AreEqual(identities, user2.Identities);
            //Assert.AreEqual(roles, user2.Roles);
        }

        public interface IInheritTest { }
        public abstract class InheritTest : IInheritTest { }
        public interface IInheritTestA : IInheritTest { }
        public abstract class InheritTestA : IInheritTestA { }
        public abstract class InheritTestB : IInheritTest { }

        [Test]
        public void InheritsOrImplements()
        {
            Assert.IsTrue(typeof(InheritTest).InheritsOrImplements(typeof(IInheritTest)));
            Assert.IsTrue(typeof(IInheritTestA).InheritsOrImplements(typeof(IInheritTest)));
            Assert.IsTrue(typeof(InheritTestA).InheritsOrImplements(typeof(IInheritTest)));
            Assert.IsTrue(typeof(InheritTestB).InheritsOrImplements(typeof(IInheritTest)));

            Assert.IsTrue(typeof(InheritTestA).InheritsOrImplements(typeof(IInheritTestA)));
            Assert.IsFalse(typeof(InheritTestB).InheritsOrImplements(typeof(IInheritTestA)));
        }

        [Test]
        public void IsDateTime()
        {
            Assert.IsTrue(DateTime.Now.GetType().IsDateTime());
            DateTime? d = DateTime.Now;
            Assert.IsTrue(d.GetType().IsDateTime());
        }
    }
}
