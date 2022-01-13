using Kaiyuanshe.OpenHackathon.Server.Storage;
using NUnit.Framework;
using System;

namespace Kaiyuanshe.OpenHackathon.ServerTests.Storage
{
    internal class StorageUtilsTests
    {
        [Test]
        public void TestInverseKeyOrdering()
        {
            var d = DateTime.UtcNow;
            var d1 = d.AddTicks(1);

            Assert.Greater(StorageUtils.InversedTimeKey(d), StorageUtils.InversedTimeKey(d1));
        }

        [Test]
        public void TestUnspecifiedKindOrdering()
        {
            var d = new DateTime(2018, 6, 5, 10, 26, 00, DateTimeKind.Unspecified);
            var d1 = DateTime.SpecifyKind(d, DateTimeKind.Utc);
            Assert.AreEqual(StorageUtils.InversedTimeKey(d), StorageUtils.InversedTimeKey(d1));
            Assert.Less(StorageUtils.InversedTimeKey(d), StorageUtils.InversedTimeKey(d1.AddTicks(-1)));
        }
    }
}
