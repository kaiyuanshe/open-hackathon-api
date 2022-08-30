using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;

namespace Kaiyuanshe.OpenHackathon.Server.Storage.Tables
{
    public interface ITopUserTable : IAzureTableV2<TopUserEntity>
    {
    }

    public class TopUserTable : AzureTableV2<TopUserEntity>, ITopUserTable
    {
        protected override string TableName => TableNames.TopUser;
    }
}
