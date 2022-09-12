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
        #region GetResourceKey
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
        #endregion

        #region GenerateMessage
        private static IEnumerable GenerateMessageTestData()
        {
            // arg0: ActivityLogEntity
            // arg1: dynamic args
            // arg2: resourceKey
            // arg3: expected message in en-US(zh-CN cannot be tested on Github Actions, the resource files cannot be loaded)
            // arg4: expected resourceKey

            // unknown log type
            yield return new TestCaseData(
                new ActivityLogEntity
                {
                    ActivityLogType = "unknown",
                },
                null,
                null,
                null,
                "ActivityLog_Hackathon_unknown");

            // malformat
            yield return new TestCaseData(
                new ActivityLogEntity
                {
                    ActivityLogType = "createHackathon",
                },
                new { },
                null,
                null,
                "ActivityLog_Hackathon_createHackathon");

            // formated, catetory=Hackathon
            yield return new TestCaseData(
                new ActivityLogEntity
                {
                    ActivityLogType = "createHackathon",
                    Category = ActivityLogCategory.Hackathon,
                },
                new { userName = "un", unknown = "any" },
                null,
                "Created by user: un.",
                "ActivityLog_Hackathon_createHackathon");

            // formated, catetory=User
            yield return new TestCaseData(
                new ActivityLogEntity
                {
                    ActivityLogType = "createHackathon",
                    Category = ActivityLogCategory.User,
                },
                new { hackathonName = "hack" },
                null,
                "[hack]Created a new hackathon: hack",
                "ActivityLog_User_createHackathon");

            // formated, resourceKey
            yield return new TestCaseData(
                new ActivityLogEntity
                {
                    ActivityLogType = "createHackathon",
                    Category = ActivityLogCategory.User,
                },
                new { hackathonName = "hack" },
                "User_NotFound",
                "The specified user is not found.",
                "User_NotFound");
        }

        [Test, TestCaseSource(nameof(GenerateMessageTestData))]
        public void GenerateMessage(ActivityLogEntity entity, object args, string resourceKey, string expectedEn, string expectedKey)
        {
            entity.GenerateMessage(args, resourceKey);

            Assert.IsTrue(entity.Messages.ContainsKey("zh-CN"));
            Assert.AreEqual(expectedKey, entity.MessageResourceKey);
            if (expectedEn != null)
                Assert.IsNotNull(entity.Messages["zh-CN"]);
            // CultureInfo unable to set/get on Github actions
            Assert.IsTrue(entity.Messages.ContainsKey("en-US"));
            Assert.AreEqual(expectedEn, entity.Messages["en-US"]);
        }
        #endregion

        #region GetMessage
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
        public string GetMessage(Dictionary<string, string?> messages)
        {
            CultureInfo.CurrentUICulture = CultureInfos.zh_CN;
            var entity = new ActivityLogEntity { Messages = messages };
            return entity.GetMessage();
        }
        #endregion

        #region GetMessageFormat
        private static IEnumerable GetMessageFormatTestData()
        {
            // null entity
            yield return new TestCaseData(null).Returns(null);

            // null resource key
            yield return new TestCaseData(new ActivityLogEntity
            {
                Messages = new Dictionary<string, string?>
                {
                    ["en-US"] = "en"
                }
            }).Returns("en");

            // resource key doesn't exist
            yield return new TestCaseData(new ActivityLogEntity
            {
                MessageResourceKey = "abcdef",
                Messages = new Dictionary<string, string?>
                {
                    ["en-US"] = "en"
                }
            }).Returns("en");

            // got from resources
            yield return new TestCaseData(new ActivityLogEntity
            {
                MessageResourceKey = "User_NotFound",
                Messages = new Dictionary<string, string?>
                {
                    ["en-US"] = "en"
                }
            }).Returns("The specified user is not found.");
        }

        [Test, TestCaseSource(nameof(GetMessageFormatTestData))]
        public string? GetMessageFormat(ActivityLogEntity entity)
        {
            return entity.GetMessageFormat();
        }
        #endregion

        #region EnsureResources
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
        #endregion
    }
}
