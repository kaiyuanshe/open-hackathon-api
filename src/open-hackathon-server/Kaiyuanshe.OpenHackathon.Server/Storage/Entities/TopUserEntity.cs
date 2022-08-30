namespace Kaiyuanshe.OpenHackathon.Server.Storage.Entities
{
    public class TopUserEntity : BaseTableEntity
    {
        [IgnoreEntityProperty]
        public string UserId
        {
            get
            {
                return PartitionKey;
            }
        }

        public int Rank { get; set; }
        public int Score { get; set; }
    }
}
