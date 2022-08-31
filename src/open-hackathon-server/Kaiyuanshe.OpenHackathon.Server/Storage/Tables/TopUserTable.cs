using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.Storage.Tables
{
    public interface ITopUserTable : IAzureTableV2<TopUserEntity>
    {
        Task BatchUpdateTopUsers(Dictionary<string, int> scores, CancellationToken cancellationToken);
    }

    public class TopUserTable : AzureTableV2<TopUserEntity>, ITopUserTable
    {
        const int MaxRecords = 100;
        protected override string TableName => TableNames.TopUser;

        public async Task BatchUpdateTopUsers(Dictionary<string, int> scores, CancellationToken cancellationToken)
        {
            var data = scores.OrderByDescending(kv => kv.Value)
                .Take(MaxRecords)
                .Select((kv, index) => new TopUserEntity
                {
                    CreatedAt = DateTime.UtcNow,
                    PartitionKey = index.ToString(),
                    RowKey = index.ToString(),
                    Score = kv.Value,
                    UserId = kv.Key
                });

            foreach (var entity in data)
            {
                await InsertOrReplaceAsync(entity, cancellationToken);
            }
        }
    }
}
