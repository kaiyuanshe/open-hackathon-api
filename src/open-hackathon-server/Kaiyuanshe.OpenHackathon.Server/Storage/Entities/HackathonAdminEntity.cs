namespace Kaiyuanshe.OpenHackathon.Server.Storage.Entities
{
    /// <summary>
    /// Represents a hackathon administrator.
    /// 
    /// PK: hackathon Name. 
    /// RK: user Id.
    /// 
    /// PK might be string.Empty for PlatformAdministrator.
    /// </summary>
    public class HackathonAdminEntity : BaseTableEntity
    {
        [IgnoreEntityProperty]
        public string HackathonName
        {
            get
            {
                return PartitionKey;
            }
        }

        [IgnoreEntityProperty]
        public string UserId
        {
            get
            {
                return RowKey;
            }
        }

        public bool IsPlatformAdministrator()
        {
            return string.IsNullOrEmpty(HackathonName);
        }
    }
}
