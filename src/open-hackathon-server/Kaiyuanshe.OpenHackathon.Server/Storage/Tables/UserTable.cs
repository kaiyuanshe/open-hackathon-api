using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.ResponseBuilder;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.Storage.Tables
{
    public interface IUserTable : IAzureTableV2<UserEntity>
    {
        Task<UserInfo?> GetUserByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<UserEntity> SaveUserAsync(UserInfo userInfo, CancellationToken cancellationToken = default);
    }

    public class UserTable : AzureTableV2<UserEntity>, IUserTable
    {
        protected override string TableName => TableNames.User;

        public IResponseBuilder ResponseBuilder { get; set; }

        public async Task<UserInfo?> GetUserByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            var entity = await RetrieveAsync(id.ToLower(), string.Empty, cancellationToken);
            if (entity == null)
                return null;

            return ResponseBuilder.BuildUser(entity);
        }

        public async Task<UserEntity> SaveUserAsync(UserInfo userInfo, CancellationToken cancellationToken = default)
        {
            var entity = new UserEntity
            {
                Address = userInfo.Address,
                Arn = userInfo.Arn,
                Birthdate = userInfo.Birthdate,
                Blocked = userInfo.Blocked.GetValueOrDefault(false),
                Browser = userInfo.Browser,
                City = userInfo.City,
                Company = userInfo.Company,
                Country = userInfo.Country,
                CreatedAt = DateTime.UtcNow,
                Device = userInfo.Device,
                Email = userInfo.Email,
                EmailVerified = userInfo.EmailVerified.GetValueOrDefault(false),
                FamilyName = userInfo.FamilyName,
                Formatted = userInfo.Formatted,
                Gender = userInfo.Gender,
                GivenName = userInfo.GivenName,
                Identities = userInfo.Identities,
                IsDeleted = userInfo.IsDeleted.GetValueOrDefault(false),
                LastIp = userInfo.LastIp,
                LastLogin = userInfo.LastLogin,
                Locale = userInfo.Locale,
                Locality = userInfo.Locality,
                LoginsCount = userInfo.LoginsCount.GetValueOrDefault(1),
                MiddleName = userInfo.MiddleName,
                Name = userInfo.Name,
                Nickname = userInfo.Nickname,
                OAuth = userInfo.OAuth,
                OpenId = userInfo.OpenId,
                PartitionKey = userInfo.Id.ToLower(), // PK
                Password = userInfo.Password,
                Phone = userInfo.Phone,
                PhoneVerified = userInfo.PhoneVerified.GetValueOrDefault(false),
                Photo = userInfo.Photo,
                PostalCode = userInfo.PostalCode,
                PreferredUsername = userInfo.PreferredUsername,
                Profile = userInfo.Profile,
                Province = userInfo.Province,
                Region = userInfo.Region,
                RegisterSource = userInfo.RegisterSource,
                RowKey = string.Empty, //RK
                SignedUp = userInfo.SignedUp,
                StreetAddress = userInfo.StreetAddress,
                Token = userInfo.Token,
                TokenExpiredAt = userInfo.TokenExpiredAt,
                Unionid = userInfo.Unionid,
                Username = userInfo.Username,
                UserPoolId = userInfo.UserPoolId,
                Website = userInfo.Website,
                Zoneinfo = userInfo.Zoneinfo,
            };
            await InsertOrReplaceAsync(entity, cancellationToken);
            return await RetrieveAsync(userInfo.Id.ToLower(), string.Empty);
        }
    }
}
