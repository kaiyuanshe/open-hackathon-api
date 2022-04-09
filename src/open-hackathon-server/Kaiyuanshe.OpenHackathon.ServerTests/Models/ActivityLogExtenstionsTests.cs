using Kaiyuanshe.OpenHackathon.Server.Models;
using NUnit.Framework;

namespace Kaiyuanshe.OpenHackathon.ServerTests.Models
{
    internal class ActivityLogExtenstionsTests
    {
        [Test]
        public void GetResourceKey()
        {
            Assert.AreEqual("ActivitityLog_CreateHackathon", ActivityLogType.createHackathon.GetResourceKey());
        }
    }
}
