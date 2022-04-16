using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using NUnit.Framework;
using System.Collections;

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
                null,
                null);

            // malformat
            yield return new TestCaseData(
                new ActivityLogEntity
                {
                    ActivityLogType = "createHackathon",
                },
                new { },
                null,
                null);

            // formated, catetory=Hackathon
            yield return new TestCaseData(
                new ActivityLogEntity
                {
                    ActivityLogType = "createHackathon",
                    Category = ActivityLogCategory.Hackathon,
                },
                new { userName = "un" },
                "un创建了活动。",
                "created by un.");

            // formated, catetory=User
            yield return new TestCaseData(
                new ActivityLogEntity
                {
                    ActivityLogType = "createHackathon",
                    Category = ActivityLogCategory.User,
                },
                new { hackathonName = "hack" },
                "创建了新活动：hack",
                "created a new hackathon: hack");
        }

        [Test, TestCaseSource(nameof(GenerateMessageTestData))]
        public void GenerateMessage(ActivityLogEntity entity, object args, string cn, string en)
        {
            entity.GenerateMessage(args);
            Assert.IsTrue(entity.Messages.ContainsKey("zh-CN"));
            Assert.AreEqual(cn, entity.Messages["zh-CN"]);
            Assert.IsTrue(entity.Messages.ContainsKey("en-US"));
            Assert.AreEqual(en, entity.Messages["en-US"]);
        }
    }
}
