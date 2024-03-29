﻿using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;

namespace Kaiyuanshe.OpenHackathon.Server.Storage.Tables
{
    public interface IUserTokenTable : IAzureTableV2<UserTokenEntity>
    {
    }

    public class UserTokenTable : AzureTableV2<UserTokenEntity>, IUserTokenTable
    {
        protected override string TableName => TableNames.UserToken;
    }
}
