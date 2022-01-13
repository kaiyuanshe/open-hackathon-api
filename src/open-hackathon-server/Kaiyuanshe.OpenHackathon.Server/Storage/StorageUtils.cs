using System;

namespace Kaiyuanshe.OpenHackathon.Server.Storage
{
    public static class StorageUtils
    {
        /// <summary>
        /// Get a string which is derived from the given time, and is sorted in opposite order as the original time.
        /// </summary>
        /// <remarks>
        /// Azure table sorts the records by partition key and row key in ascending order, and returns the records in the same order.
        /// To get the most recent record according to the time, we can set the partition key or row key as this "inverse time string",
        /// and then the records will be sorted by time in descending order, which allows us to query the latest data more efficiently.
        /// </remarks>
        /// <param name="dateTime">The given time.</param>
        /// <returns></returns>
        public static string InversedTimeKey(DateTime dateTime)
        {
            if (dateTime.Kind == DateTimeKind.Unspecified)
            {
                dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
            }
            dateTime = dateTime.ToUniversalTime();

            // the max value is a number like xxx9999999, add 1 so we get a clear representation when the given dateTime is rounded.
            return (DateTime.MaxValue.Ticks - dateTime.Ticks + 1).ToString("d19");
        }
    }
}
