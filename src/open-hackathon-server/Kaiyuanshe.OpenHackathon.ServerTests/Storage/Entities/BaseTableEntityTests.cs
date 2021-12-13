using Azure.Data.Tables;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kaiyuanshe.OpenHackathon.ServerTests.Storage.Entities
{
    public class TestEntity : BaseTableEntity
    {
        string field = "";
        string privateProperty { get; set; }

        [IgnoreEntityProperty]
        public string IgnoredProperty { get; set; }

        [ConvertableEntityProperty]
        public string[] ComplexObject { get; set; }

        public DateTime Date { get; set; }
        public byte[] Binary { get; set; }
        public bool Bool { get; set; }
        public Guid Guid { get; set; }
        public long Long { get; set; }
        public string String { get; set; }
        public double Double { get; set; }
    }

    class BaseTableEntityTests
    {
        [Test]
        public void ToTableEntity()
        {
            var entity = new TestEntity
            {
                PartitionKey = "pk",
                RowKey = "rk",
                ETag = "etag",
                IgnoredProperty = "ignore",
                ComplexObject = new string[] { "a", "b", "c" },
                Date = DateTime.UtcNow,
                Binary = Encoding.UTF8.GetBytes("a"),
                Bool = true,
                Guid = Guid.NewGuid(),
                Long = 1L,
                String = "str",
                Double = 0.1,
            };

            var tableEntity = entity.ToTableEntity();

            Assert.AreEqual("pk", tableEntity.PartitionKey);
            Assert.AreEqual("rk", tableEntity.RowKey);
            Assert.AreEqual("etag", tableEntity.ETag.ToString());
            Assert.IsFalse(tableEntity.ContainsKey("field"));
            Assert.IsFalse(tableEntity.ContainsKey("privateProperty"));
            Assert.IsFalse(tableEntity.ContainsKey("IgnoredProperty"));
            Assert.AreEqual("[\"a\",\"b\",\"c\"]", tableEntity.GetString("ComplexObject"));
            Assert.AreEqual(entity.Date, tableEntity.GetDateTime("Date"));
            Assert.AreEqual(entity.Binary, tableEntity.GetBinary("Binary"));
            Assert.AreEqual(true, tableEntity.GetBoolean("Bool"));
            Assert.AreEqual(entity.Guid, tableEntity.GetGuid("Guid"));
            Assert.AreEqual(1L, tableEntity.GetInt64("Long"));
            Assert.AreEqual("str", tableEntity.GetString("String"));
            Assert.AreEqual(0.1, tableEntity.GetDouble("Double"));
        }

        [Test]
        public void ToBaseEntity()
        {
            var date = DateTimeOffset.UtcNow;
            var guid = Guid.NewGuid();
            var tableEntity = new TableEntity(new Dictionary<string, object>
            {
                { "PartitionKey", "pk" },
                { "RowKey", "rk" },
                { "odata.etag", "etag" },
                { "IgnoredProperty", "ignore" },
                { "ComplexObject", "[\"a\",\"b\",\"c\"]" },
                { "Date", date },
                { "Binary", Encoding.UTF8.GetBytes("a") },
                { "Bool", true },
                { "Guid", guid },
                { "Long", 1L },
                { "String", "str" },
                { "Double", 0.1 }
            });

            var entity = tableEntity.ToBaseTableEntity<TestEntity>();

            Assert.AreEqual("pk", entity.PartitionKey);
            Assert.AreEqual("rk", entity.RowKey);
            Assert.AreEqual("etag", entity.ETag.ToString());
            Assert.IsNull(entity.IgnoredProperty);
            Assert.AreEqual(3, entity.ComplexObject.Length);
            Assert.AreEqual(date.UtcDateTime, entity.Date);
            Assert.AreEqual(Encoding.UTF8.GetBytes("a"), entity.Binary);
            Assert.AreEqual(true, entity.Bool);
            Assert.AreEqual(guid, entity.Guid);
            Assert.AreEqual(1L, entity.Long);
            Assert.AreEqual("str", entity.String);
            Assert.AreEqual(0.1, entity.Double);
        }

        #region EntityWithDefaultValue
        class EntityWithDefaultValue : BaseTableEntity
        {
            public EntityWithDefaultValue()
            {
                Dict = new Dictionary<string, string> { { "k1", "v1" } };
            }

            [ConvertableEntityProperty]
            public Dictionary<string, string> Dict { get; set; }
        }

        [Test]
        public void ToTableEntity_EntityWithDefaultValue()
        {
            var entity = new EntityWithDefaultValue();
            var tableEntity = entity.ToTableEntity();
            Assert.AreEqual("{\"k1\":\"v1\"}", tableEntity.GetString("Dict"));
        }

        [Test]
        public void ToSignalREntity_EntityWithDefaultValue()
        {
            var tableEntity = new TableEntity();
            var entity = tableEntity.ToBaseTableEntity<EntityWithDefaultValue>();
            CollectionAssert.AreEqual(new Dictionary<string, string> { { "k1", "v1" } }, entity.Dict);
        }
        #endregion

        #region Enum Property
        enum Choice
        {
            Yes,
            No,
        }

        class EntityWithEnumProperty : BaseTableEntity
        {
            public Choice Normal { get; set; }

            public Choice? NullableNormal { get; set; }

            [IgnoreEntityProperty]
            public Choice Ignored { get; set; }
        }

        [Test]
        public void ToTableEntity_EntityWithEnumProperty()
        {
            var entity = new EntityWithEnumProperty { Normal = Choice.No, Ignored = Choice.No, NullableNormal = Choice.No };
            var tableEntitty = entity.ToTableEntity();
            Assert.AreEqual(1, tableEntitty.GetInt32("Normal"));
            Assert.AreEqual(1, tableEntitty.GetInt32("NullableNormal"));
            Assert.IsFalse(tableEntitty.ContainsKey("Ignored"));
        }

        [Test]
        public void ToSignalREntity_EntityWithEnumProperty()
        {
            var tableEntitty = new TableEntity(new Dictionary<string, object> {
                { "Normal", 1 },
                { "NullableNormal", 1 },
                { "Ignored", 1 }
            });
            var entity = tableEntitty.ToBaseTableEntity<EntityWithEnumProperty>();
            Assert.AreEqual(Choice.No, entity.Normal);
            Assert.AreEqual(Choice.No, entity.NullableNormal.Value);
            Assert.AreEqual(Choice.Yes, entity.Ignored);
        }

        #endregion

        #region Bytes[] Property
        class BytesProperty : BaseTableEntity
        {
            public byte[] Bytes { get; set; }
        }

        [Test]
        public void ToTableEntity_BytesProperty()
        {
            var entity = new BytesProperty
            {
                Bytes = new byte[] { 0x20, 0x20, 0x20, 0x20 }
            };
            var tableEntity = entity.ToTableEntity();
            CollectionAssert.AreEqual(entity.Bytes, tableEntity.GetBinary("Bytes"));
        }

        [Test]
        public void ToSinalREntity_BytesProperty()
        {
            var tableEntity = new TableEntity(new Dictionary<string, object>
            {
                { "Bytes", new byte[] { 0x20, 0x20, 0x20, 0x20 } }
            });
            var entity = tableEntity.ToBaseTableEntity<BytesProperty>();
            CollectionAssert.AreEqual(new byte[] { 0x20, 0x20, 0x20, 0x20 }, entity.Bytes);
        }
        #endregion

        #region DateTime
        class DateTimeProperty : BaseTableEntity
        {
            public DateTime Date1 { get; set; }
            public DateTime? Date2 { get; set; }
        }

        [Test]
        public void ToTableEntity_DateTimeProperty()
        {
            var date = DateTime.Now;
            var entity = new DateTimeProperty
            {
                Date1 = date,
                Date2 = date,
            };
            var tableEntity = entity.ToTableEntity();
            Assert.AreEqual(date, tableEntity.GetDateTime("Date1").Value);
            Assert.AreEqual(date, tableEntity.GetDateTime("Date2").Value);
        }

        [Test]
        public void ToSignalREntity_DateTimeProperty()
        {
            var date = DateTimeOffset.UtcNow;
            var tableEntity = new TableEntity(new Dictionary<string, object>
            {
                { "Date1", date  },
                { "Date2", date },
            });
            var entity = tableEntity.ToBaseTableEntity<DateTimeProperty>();
            Assert.AreEqual(date.DateTime, entity.Date1);
            Assert.AreEqual(date.DateTime, entity.Date2.Value);
        }
        #endregion
    }
}
