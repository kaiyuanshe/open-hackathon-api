﻿using Kaiyuanshe.OpenHackathon.Server.Models;
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
    public interface IUserTable : IAzureTable<DynamicTableEntity>
    {
        Task<UserInfo> GetUserByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<DynamicTableEntity> SaveUserAsync(UserInfo userInfo, CancellationToken cancellationToken = default);
    }

    public class UserTable : AzureTable<DynamicTableEntity>, IUserTable
    {
        public UserTable(CloudStorageAccount storageAccount, string tableName) : base(storageAccount, tableName)
        {
        }

        public async Task<UserInfo> GetUserByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            var entity = await RetrieveAsync(id.ToLower(), string.Empty, cancellationToken);
            UserInfo resp = new UserInfo();
            return entity.ToModel(resp);
        }

        public async Task<DynamicTableEntity> SaveUserAsync(UserInfo userInfo, CancellationToken cancellationToken = default)
        {
            var entity = userInfo.ToTableEntity(userInfo.Id.ToLower(), string.Empty);
            await InsertOrReplaceAsync(entity);
            return await RetrieveAsync(userInfo.Id.ToLower(), string.Empty);
        }
    }
}