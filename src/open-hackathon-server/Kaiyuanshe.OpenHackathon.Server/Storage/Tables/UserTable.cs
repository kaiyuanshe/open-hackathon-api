using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.Storage.Tables
{
    public interface IUserTable : IAzureTable<Microsoft.WindowsAzure.Storage.Table.DynamicTableEntity>
    {
        Task<UserInfo> GetUserByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<Microsoft.WindowsAzure.Storage.Table.DynamicTableEntity> SaveUserAsync(UserInfo userInfo, CancellationToken cancellationToken = default);
    }

    public class UserTable : AzureTable<Microsoft.WindowsAzure.Storage.Table.DynamicTableEntity>, IUserTable
    {
        public UserTable(CloudStorageAccount storageAccount, string tableName) : base(storageAccount, tableName)
        {
        }

        public async Task<UserInfo> GetUserByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            var entity = await RetrieveAsync(id.ToLower(), string.Empty, cancellationToken);
            if (entity == null)
                return null;

            UserInfo resp = new UserInfo();
            return entity.ToModel(resp, (m) =>
            {
                m.updatedAt = entity.Timestamp.UtcDateTime;
                if (entity.Properties.ContainsKey("SignedUp")
                && DateTime.TryParse(entity.Properties["SignedUp"].StringValue, out DateTime signedUp))
                {
                    m.createdAt = signedUp;
                    m.Token = null;
                    m.Password = null;
                }
            });
        }

        public async Task<Microsoft.WindowsAzure.Storage.Table.DynamicTableEntity> SaveUserAsync(UserInfo userInfo, CancellationToken cancellationToken = default)
        {
            var entity = userInfo.ToTableEntity(userInfo.Id.ToLower(), string.Empty);
            await InsertOrReplaceAsync(entity);
            return await RetrieveAsync(userInfo.Id.ToLower(), string.Empty);
        }
    }
}
