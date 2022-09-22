using Azure.Data.Tables;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Kaiyuanshe.OpenHackathon.Server.Storage
{
    public static class TableQueryHelper
    {
        private static readonly char[] ContinuationTokenSplit = new char[1] { ' ' };

        public static string PartitionKeyFilter(string partitionKey)
        {
            return FilterForString(nameof(ITableEntity.PartitionKey), ComparisonOperator.Equal, partitionKey);
        }

        /// <summary>
        /// Generate to Table query filter to simulate `startWith`. Must use this filter together with a PartitionKey.
        /// </summary>
        /// <param name="startsWith"></param>
        /// <returns></returns>
        public static string RowKeyStartwithFilter(string partitionKey, string startsWith)
        {
            var length = startsWith.Length - 1;
            var newLastChar = (char)(startsWith[length] + 1);
            var newStartsWith = startsWith.Substring(0, length) + newLastChar;

            var filter = And(
                PartitionKeyFilter(partitionKey),
                FilterForString(nameof(ITableEntity.RowKey), ComparisonOperator.GreaterThanOrEqual, startsWith),
                FilterForString(nameof(ITableEntity.RowKey), ComparisonOperator.LessThan, newStartsWith)
            );

            Debug.Assert(filter != null);
            return filter;
        }

        public static string FilterForString(string propertyName, ComparisonOperator op, string givenValue)
        {
            givenValue = givenValue ?? string.Empty;
            string operand = string.Format(CultureInfo.InvariantCulture, "'{0}'", givenValue.Replace("'", "''"));
            return string.Format(CultureInfo.InvariantCulture, "{0} {1} {2}", propertyName, op.ToOperator(), operand);
        }

        public static string FilterForBinary(string propertyName, ComparisonOperator op, byte[] givenValue)
        {
            AssertNotNull("value", givenValue);
            StringBuilder sb = new StringBuilder();
            foreach (byte b in givenValue)
            {
                sb.AppendFormat("{0:x2}", b);
            }
            string operand = string.Format(CultureInfo.InvariantCulture, "X'{0}'", sb.ToString());
            return GenerateFilter(propertyName, op, operand);
        }

        public static string FilterForBool(string propertyName, ComparisonOperator op, bool givenValue)
        {
            string operand = givenValue ? "true" : "false";
            return GenerateFilter(propertyName, op, operand);
        }

        public static string FilterForDate(string propertyName, ComparisonOperator op, DateTimeOffset givenValue)
        {
            string datetime = givenValue.UtcDateTime.ToString("o", CultureInfo.InvariantCulture);
            string operand = string.Format(CultureInfo.InvariantCulture, "datetime'{0}'", datetime);
            return GenerateFilter(propertyName, op, operand);
        }

        public static string FilterForDouble(string propertyName, ComparisonOperator op, double givenValue)
        {
            string value = Convert.ToString(givenValue, CultureInfo.InvariantCulture);
            string operand = (int.TryParse(value, out var _) ? string.Format(CultureInfo.InvariantCulture, "{0}.0", value) : value);
            return GenerateFilter(propertyName, op, operand);
        }

        public static string FilterForGuid(string propertyName, ComparisonOperator op, Guid givenValue)
        {
            AssertNotNull("value", givenValue);
            string operand = string.Format(CultureInfo.InvariantCulture, "guid'{0}'", givenValue.ToString());
            return GenerateFilter(propertyName, op, operand);
        }

        public static string FilterForInt(string propertyName, ComparisonOperator op, int givenValue)
        {
            string operand = Convert.ToString(givenValue, CultureInfo.InvariantCulture);
            return GenerateFilter(propertyName, op, operand);
        }

        public static string FilterForLong(string propertyName, ComparisonOperator op, long givenValue)
        {
            string value = Convert.ToString(givenValue, CultureInfo.InvariantCulture);
            string operand = string.Format(CultureInfo.InvariantCulture, "{0}L", value);
            return GenerateFilter(propertyName, op, operand);
        }

        public static string? And(params string[] filters)
        {
            return CombineFilters("and", filters);
        }

        public static string? Or(params string[] filters)
        {
            return CombineFilters("or", filters);
        }

        static string? CombineFilters(string operatorString, params string[] filters)
        {
            if (filters == null || filters.Length == 0)
                return null;

            if (filters.Length == 1)
                return filters[0];

            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append($"({filters[0]})");
            for (int i = 1; i < filters.Length; i++)
            {
                stringBuilder.Append($" {operatorString} ({filters[i]})");
            }
            return stringBuilder.ToString();
        }

        static string GenerateFilter(string propertyName, ComparisonOperator op, string operand)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} {1} {2}", propertyName, op.ToOperator(), operand);
        }

        static void AssertNotNull(string paramName, object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(paramName);
            }
        }
    }
}
