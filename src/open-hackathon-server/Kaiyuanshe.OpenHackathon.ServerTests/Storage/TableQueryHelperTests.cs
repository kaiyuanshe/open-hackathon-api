using Kaiyuanshe.OpenHackathon.Server.Storage;
using NUnit.Framework;
using System;

namespace Kaiyuanshe.OpenHackathon.ServerTests.Storage
{
    internal class TableQueryHelperTests
    {
        [Test]
        public void PartitionKeyFilter()
        {
            var filter = TableQueryHelper.PartitionKeyFilter("pk");
            Assert.AreEqual("PartitionKey eq 'pk'", filter);
        }

        [Test]
        public void RowKeyStartwithFilter()
        {
            var filter = TableQueryHelper.RowKeyStartwithFilter("pk", "start");
            Assert.AreEqual("(PartitionKey eq 'pk') and (RowKey ge 'start') and (RowKey lt 'staru')", filter);
        }

        [TestCase("a", ComparisonOperator.Equal, "Ab1", ExpectedResult = "a eq 'Ab1'")]
        [TestCase("a", ComparisonOperator.NotEqual, "'b'", ExpectedResult = "a ne '''b'''")]
        [TestCase("a", ComparisonOperator.GreaterThan, "测试", ExpectedResult = "a gt '测试'")]
        [TestCase("a", ComparisonOperator.GreaterThanOrEqual, "~`!@#$%^&*()-_+={}[]|\\<>,.?/\":;", ExpectedResult = "a ge '~`!@#$%^&*()-_+={}[]|\\<>,.?/\":;'")]
        public string FilterForString(string propertyName, ComparisonOperator op, string givenValue)
        {
            return TableQueryHelper.FilterForString(propertyName, op, givenValue);
        }

        [TestCase("a", ComparisonOperator.Equal, new byte[] { 65, 98, 49 }, ExpectedResult = "a eq X'416231'")] // "Ab1"
        public string FilterForBinary(string propertyName, ComparisonOperator op, byte[] givenValue)
        {
            return TableQueryHelper.FilterForBinary(propertyName, op, givenValue);
        }

        [TestCase("a", ComparisonOperator.Equal, true, ExpectedResult = "a eq true")]
        [TestCase("a", ComparisonOperator.NotEqual, false, ExpectedResult = "a ne false")]
        public string FilterForBinary(string propertyName, ComparisonOperator op, bool givenValue)
        {
            return TableQueryHelper.FilterForBool(propertyName, op, givenValue);
        }

        [Test]
        public void FilterForDate()
        {
            var datetimeOffset = new DateTimeOffset(2021, 12, 12, 1, 2, 3, TimeSpan.Zero);
            var filter = TableQueryHelper.FilterForDate("a", ComparisonOperator.GreaterThan, datetimeOffset);
            Assert.AreEqual("a gt datetime'2021-12-12T01:02:03.0000000Z'", filter);
        }

        [TestCase("a", ComparisonOperator.LessThan, 5, ExpectedResult = "a lt 5.0")]
        [TestCase("a", ComparisonOperator.LessThanOrEqual, 5.1, ExpectedResult = "a le 5.1")]
        public string FilterForDouble(string propertyName, ComparisonOperator op, double givenValue)
        {
            return TableQueryHelper.FilterForDouble(propertyName, op, givenValue);
        }

        [TestCase("a", ComparisonOperator.Equal, "8f01fb7e-ebbe-4dcb-b717-e7f40c6daf80", ExpectedResult = "a eq guid'8f01fb7e-ebbe-4dcb-b717-e7f40c6daf80'")]
        public string FilterForGuid(string propertyName, ComparisonOperator op, string givenValue)
        {
            return TableQueryHelper.FilterForGuid(propertyName, op, Guid.Parse(givenValue));
        }

        [Test]
        public void FilterForInt()
        {
            var filter = TableQueryHelper.FilterForInt("a", ComparisonOperator.GreaterThan, 1);
            Assert.AreEqual("a gt 1", filter);
        }

        [Test]
        public void FilterForLong()
        {
            var filter = TableQueryHelper.FilterForLong("a", ComparisonOperator.GreaterThan, 1);
            Assert.AreEqual("a gt 1L", filter);
        }

        [TestCase(ExpectedResult = null)]
        [TestCase("a eq a1", ExpectedResult = "a eq a1")]
        [TestCase("a eq a1", "b ne 'e1'", ExpectedResult = "(a eq a1) and (b ne 'e1')")]
        [TestCase("a eq a1", "b ne 'e1'", "c lt 5L", ExpectedResult = "(a eq a1) and (b ne 'e1') and (c lt 5L)")]
        public string? And(params string[] filters)
        {
            return TableQueryHelper.And(filters);
        }

        [TestCase(ExpectedResult = null)]
        [TestCase("a eq a1", ExpectedResult = "a eq a1")]
        [TestCase("a eq a1", "b ne 'e1'", ExpectedResult = "(a eq a1) or (b ne 'e1')")]
        [TestCase("a eq a1", "b ne 'e1'", "c lt 5L", ExpectedResult = "(a eq a1) or (b ne 'e1') or (c lt 5L)")]
        public string? Or(params string[] filters)
        {
            return TableQueryHelper.Or(filters);
        }
    }
}
