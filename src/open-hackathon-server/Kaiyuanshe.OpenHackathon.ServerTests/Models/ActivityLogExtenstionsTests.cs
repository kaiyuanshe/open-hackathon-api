using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using NUnit.Framework;
using System.Collections;

namespace Kaiyuanshe.OpenHackathon.ServerTests.Models
{
    internal class ActivityLogExtenstionsTests
    {
        [Test]
        public void GetResourceKey()
        {
            Assert.AreEqual("", ActivityLogType.none.GetResourceKey());
            Assert.AreEqual("ActivitityLog_CreateHackathon", ActivityLogType.createHackathon.GetResourceKey());
        }

        private static IEnumerable GetMessageTestData()
        {
            // null
            yield return new TestCaseData(null)
                .Returns(null);

            // unknown log type
            yield return new TestCaseData(new ActivityLogEntity
            {
                ActivityLogType = "unknown",
                Message = "msg"
            }).Returns("msg");

            // no format key
            yield return new TestCaseData(new ActivityLogEntity
            {
                ActivityLogType = "none",
                Message = "msg"
            }).Returns("msg");

            // malformat
            yield return new TestCaseData(new ActivityLogEntity
            {
                ActivityLogType = "createHackathon",
                Message = "msg"
            }).Returns("msg");

            // formated
            yield return new TestCaseData(new ActivityLogEntity
            {
                ActivityLogType = "createHackathon",
                Message = "msg",
                Args = new[] { "uid", "hack" },
            }).Returns("uid created a new hackathon: hack.");
        }

        [Test, TestCaseSource(nameof(GetMessageTestData))]
        public string GetMessage(ActivityLogEntity entity)
        {
            return entity.GetMessage();
        }
    }
}
