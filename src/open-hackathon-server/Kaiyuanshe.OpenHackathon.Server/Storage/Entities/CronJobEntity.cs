using System;

namespace Kaiyuanshe.OpenHackathon.Server.Storage.Entities
{
    public class CronJobEntity : BaseTableEntity
    {
        private DateTime _lastExecuteTime;
        // NB. Entity Timestamp was previously used as the execute time, however, it has some drawbacks:
        //     1. It's kind of Azure Storage internal implementation details and client code should not rely on it.
        //     2. Insert / Replace / Merge operation result will not have this field populated.
        //        In this case we need a explicit retrival to get the updated Timestamp.
        // TODO: (20190327) Remove the fallback to Timestamp after 1 active release
        public DateTime LastExecuteTime
        {
            get
            {
                if (_lastExecuteTime == default(DateTime))
                {
                    return Timestamp.UtcDateTime;
                }
                return _lastExecuteTime;
            }
            set
            {
                _lastExecuteTime = value;
            }
        }

        /// <summary>
        /// Set to true to skip a cron job.
        /// </summary>
        public bool Paused { get; set; } = false;
    }
}
