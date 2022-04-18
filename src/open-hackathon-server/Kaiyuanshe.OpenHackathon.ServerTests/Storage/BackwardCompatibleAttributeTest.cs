using Azure.Data.Tables;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using NUnit.Framework;
using System.Collections.Generic;

namespace Kaiyuanshe.OpenHackathon.ServerTests.Storage
{
    [TestFixture]
    public class BackwardCompatibleAttributeTest
    {
        [Test]
        public void GetValueByName()
        {
            var properties = new Dictionary<string, object>
            {
                { "old", "value" },
            };
            var entity = new TableEntity(properties);
            var attr = new BackwardCompatibleAttribute("old");
            Assert.AreEqual("value", attr.GetValue(entity));
        }

        [Test]
        public void GetValueByNameNotFound()
        {
            var properties = new Dictionary<string, object>();
            var entity = new TableEntity(properties);
            var attr = new BackwardCompatibleAttribute("old");
            Assert.IsNull(attr.GetValue(entity));
        }
    }
}
