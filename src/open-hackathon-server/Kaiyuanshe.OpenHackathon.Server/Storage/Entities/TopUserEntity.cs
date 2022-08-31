namespace Kaiyuanshe.OpenHackathon.Server.Storage.Entities
{
    public class TopUserEntity : BaseTableEntity
    {
        [IgnoreEntityProperty]
        public int Rank
        {
            get
            {
                return int.Parse(PartitionKey);
            }
        }

        public string UserId { get; set; }
        public int Score { get; set; }
    }
}
