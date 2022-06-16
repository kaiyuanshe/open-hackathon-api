using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;

namespace Kaiyuanshe.OpenHackathon.Server.Storage.Tables
{
    public interface IRatingTable : IAzureTableV2<RatingEntity>
    {
    }

    public class RatingTable : AzureTableV2<RatingEntity>, IRatingTable
    {
        protected override string TableName => TableNames.Rating;
    }
}
