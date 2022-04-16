using Kaiyuanshe.OpenHackathon.Server;
using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace Kaiyuanshe.OpenHackathon.ServerTests.Models
{
    internal class ActivityLogExtenstionsTests
    {
        [TestCase(ActivityLogType.createAward, ActivityLogCategory.User, "ActivityLog_User_createAward")]
        [TestCase(ActivityLogType.createHackathon, ActivityLogCategory.Hackathon, "ActivityLog_Hackathon_createHackathon")]
        public void GetResourceKey(ActivityLogType type, ActivityLogCategory category, string expectedKey)
        {
            var entity = new ActivityLogEntity
            {
                ActivityLogType = type.ToString(),
                Category = category,
            };
            Assert.AreEqual(expectedKey, entity.GetResourceKey());
        }

        private static IEnumerable GenerateMessageTestData()
        {
            // arg0: ActivityLogEntity
            // arg1: dynamic args
            // arg2: expected message in zh-CN
            // arg3: expected message in en-US

            // unknown log type
            yield return new TestCaseData(
                new ActivityLogEntity
                {
                    ActivityLogType = "unknown",
                },
                null,
                null);

            // malformat
            yield return new TestCaseData(
                new ActivityLogEntity
                {
                    ActivityLogType = "createHackathon",
                },
                new { },
                null);

            // formated, catetory=Hackathon
            yield return new TestCaseData(
                new ActivityLogEntity
                {
                    ActivityLogType = "createHackathon",
                    Category = ActivityLogCategory.Hackathon,
                },
                new { userName = "un", unknown = "any" },
                "created by un.");

            // formated, catetory=User
            yield return new TestCaseData(
                new ActivityLogEntity
                {
                    ActivityLogType = "createHackathon",
                    Category = ActivityLogCategory.User,
                },
                new { hackathonName = "hack" },
                "created a new hackathon: hack");
        }

        [Test, TestCaseSource(nameof(GenerateMessageTestData))]
        public void GenerateMessage(ActivityLogEntity entity, object args, string en)
        {
            entity.GenerateMessage(args);
            Assert.IsTrue(entity.Messages.ContainsKey("zh-CN"));
            if (en != null)
                Assert.IsNotNull(entity.Messages["zh-CN"]);
            // CultureInfo unable to set/get on Github actions
            Assert.IsTrue(entity.Messages.ContainsKey("en-US"));
            Assert.AreEqual(en, entity.Messages["en-US"]);
        }

        private static IEnumerable GetMessageTestData()
        {
            yield return new TestCaseData(new Dictionary<string, string>
            {
            }).Returns(null);

            yield return new TestCaseData(new Dictionary<string, string>
            {
                ["zh-CN"] = "cn"
            }).Returns("cn");

            yield return new TestCaseData(new Dictionary<string, string>
            {
                ["en-US"] = "en"
            }).Returns("en");

            yield return new TestCaseData(new Dictionary<string, string>
            {
                ["zh-CN"] = "cn",
                ["en-US"] = "en"
            }).Returns("cn");
        }

        [Test, TestCaseSource(nameof(GetMessageTestData))]
        public string GetMessage(Dictionary<string, string> messages)
        {
            CultureInfo.CurrentUICulture = CultureInfos.zh_CN;
            var entity = new ActivityLogEntity { Messages = messages };
            return entity.GetMessage();
        }

        /// <summary>
        /// Keep updating this Test for more cases whenever we add a new combination of category+type
        /// </summary>
        [TestCase(ActivityLogCategory.Hackathon, ActivityLogType.createHackathon)]
        [TestCase(ActivityLogCategory.User, ActivityLogType.createHackathon)]
        public void EnsureResources(ActivityLogCategory category, ActivityLogType type)
        {
            var entity = new ActivityLogEntity
            {
                Category = category,
                ActivityLogType = type.ToString(),
            };
            var resourceKey = entity.GetResourceKey();
            foreach (var culture in CultureInfos.SupportedCultures)
            {
                var messageFormat = Resources.ResourceManager.GetString(resourceKey, culture);
                Assert.IsNotNull(messageFormat);
            }
        }
    }
}
