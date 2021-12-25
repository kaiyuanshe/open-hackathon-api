using System;

namespace Kaiyuanshe.OpenHackathon.Server.Storage.Entities
{
    /// <summary>
    /// Token, served as cache to avoid calling Authing in each request.
    /// 
    /// PartitionKey: SHA512 hash of Token. Max length of PK/RK is 1K. Length of Token is usually >1K
    /// RowKey: string.Empty
    /// </summary>
    public class UserTokenEntity : BaseTableEntity
    {
        public string UserId { get; set; }

        public string Token { get; set; }

        public DateTime TokenExpiredAt { get; set; }
    }
}
