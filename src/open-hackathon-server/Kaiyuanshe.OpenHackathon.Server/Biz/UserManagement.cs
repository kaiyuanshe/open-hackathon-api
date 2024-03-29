﻿using Authing.ApiClient.Auth;
using Authing.ApiClient.Types;
using Azure;
using Kaiyuanshe.OpenHackathon.Server.Auth;
using Kaiyuanshe.OpenHackathon.Server.Biz.Options;
using Kaiyuanshe.OpenHackathon.Server.Cache;
using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.Biz
{
    public interface IUserManagement
    {
        /// <summary>
        /// Handle data from succesful Authing login.
        /// </summary>
        /// <param name="loginInfo">data from Authig</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task AuthingAsync(UserInfo loginInfo, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get User info from local table
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<UserInfo?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Search user by name or email.
        /// </summary>
        Task<IEnumerable<UserEntity>> SearchUserAsync(UserQueryOptions options, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get User remotely from Authing
        /// </summary>
        /// <param name="userPoolId"></param>
        /// <param name="token"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<User> GetCurrentUserRemotelyAsync(string userPoolId, string token, CancellationToken cancellationToken = default);

        Task<IEnumerable<TopUserEntity>> ListTopUsers(int? top = 10, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get basic Claims of user associated with an Token. Return empty list if token is invalid.
        /// Resource-based claims are not included for performance reaons.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IEnumerable<Claim>> GetUserBasicClaimsAsync(string token, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get <seealso cref="UserTokenEntity"/> using AccessToken.
        /// </summary>
        /// <param name="token">AccessToken from Authing/Github</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<UserTokenEntity?> GetTokenEntityAsync(string token, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validate AccessToken locally without calling Authing's API remotely.
        /// </summary>
        /// <param name="token">the token to validate</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<ValidationResult?> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validate <see cref="UserTokenEntity"/> locally which contains an AccessToken/>
        /// </summary>
        /// <returns></returns>
        ValidationResult? ValidateToken(UserTokenEntity tokenEntity);

        /// <summary>
        /// Validate AccessToken by calling Authing Api
        /// </summary>
        /// <param name="userPoolId">Authing's user Pool Id</param>
        /// <param name="token">the Token to validate</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<JWTTokenStatus> ValidateTokenRemotelyAsync(string userPoolId, string token, CancellationToken cancellationToken = default);
    }

    /// <inheritdoc cref="IUserManagement"/>
    public class UserManagement : ManagementClient<UserManagement>, IUserManagement
    {
        public async Task AuthingAsync(UserInfo userInfo, CancellationToken cancellationToken = default)
        {
            userInfo.Id = userInfo.Id.ToLower();
            await StorageContext.UserTable.SaveUserAsync(userInfo, cancellationToken);
            Cache.Remove(CacheKeys.GetCacheKey(CacheEntryType.User, userInfo.Id));

            // UserTokenEntity. 
            if (!string.IsNullOrEmpty(userInfo.Token))
            {
                var userToken = new UserTokenEntity
                {
                    PartitionKey = DigestHelper.SHA512Digest(userInfo.Token),
                    RowKey = string.Empty,
                    UserId = userInfo.Id,
                    UserDisplayName = userInfo.GetDisplayName(),
                    TokenExpiredAt = userInfo.TokenExpiredAt,
                    Token = userInfo.Token,
                    CreatedAt = DateTime.UtcNow,
                };
                await StorageContext.UserTokenTable.InsertOrReplaceAsync(userToken, cancellationToken);
            }
        }

        public async Task<User> GetCurrentUserRemotelyAsync(string userPoolId, string token, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userPoolId))
                throw new ArgumentNullException(string.Format(Resources.Parameter_Required, nameof(userPoolId)));
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentNullException(string.Format(Resources.Parameter_Required, nameof(token)));

            //var authenticationClient = new AuthenticationClient(userPoolId);
            var authenticationClient = new AuthenticationClient(options => options.UserPoolId = userPoolId);
            authenticationClient.AccessToken = token;
            return await authenticationClient.GetCurrentUser(cancellationToken);
        }

        public async Task<IEnumerable<TopUserEntity>> ListTopUsers(int? top = 10, CancellationToken cancellationToken = default)
        {
            var topUsers = await StorageContext.TopUserTable.QueryEntitiesAsync(null, null, cancellationToken);
            var result = topUsers.OrderBy(t => t.Rank).Take(top.GetValueOrDefault(10));
            return result;
        }

        public async Task<IEnumerable<Claim>> GetUserBasicClaimsAsync(string token, CancellationToken cancellationToken = default)
        {
            IList<Claim> claims = new List<Claim>();

            var tokenEntity = await GetTokenEntityAsync(token, cancellationToken);
            var tokenValidationResult = ValidateToken(tokenEntity);
            if (tokenValidationResult != ValidationResult.Success)
            {
                // token invalid
                return claims;
            }

            // User Id
            Debug.Assert(tokenEntity != null);
            claims.Add(ClaimsHelper.UserId(tokenEntity.UserId));
            claims.Add(ClaimsHelper.UserDisplayName(tokenEntity.UserDisplayName));

            // PlatformAdministrator
            var pa = await GetPlatformAdminClaim(tokenEntity.UserId, cancellationToken);
            if (pa != null)
            {
                claims.Add(pa);
            }

            return claims;
        }

        public virtual async Task<UserTokenEntity?> GetTokenEntityAsync(string token, CancellationToken cancellationToken = default)
        {
            string hash = DigestHelper.SHA512Digest(token);
            return await StorageContext.UserTokenTable.RetrieveAsync(hash, string.Empty, cancellationToken);
        }

        public async Task<ValidationResult?> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
        {
            // the token must not be empty
            if (string.IsNullOrWhiteSpace(token))
                return new ValidationResult(Resources.Auth_Unauthorized);

            string tokenCacheKey = CacheKeys.GetCacheKey(CacheEntryType.Token, DigestHelper.SHA512Digest(token));
            return await Cache.GetOrAddAsync(tokenCacheKey, TimeSpan.FromMinutes(5), async (c) =>
            {
                var tokenEntity = await GetTokenEntityAsync(token, cancellationToken);
                return ValidateToken(tokenEntity);
            }, false, cancellationToken);
        }

        public ValidationResult? ValidateToken(UserTokenEntity? tokenEntity)
        {
            // existence
            if (tokenEntity == null)
                return new ValidationResult(Resources.Auth_Unauthorized);

            // expiry
            if (tokenEntity.TokenExpiredAt < DateTime.UtcNow)
            {
                return new ValidationResult(string.Format(Resources.Auth_TokenExpired, tokenEntity.TokenExpiredAt.ToString("o")));
            }

            return ValidationResult.Success;
        }

        public async Task<JWTTokenStatus> ValidateTokenRemotelyAsync(string userPoolId, string token, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userPoolId))
                throw new ArgumentNullException(string.Format(Resources.Parameter_Required, nameof(userPoolId)));
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentNullException(string.Format(Resources.Parameter_Required, nameof(token)));

            //var authenticationClient = new AuthenticationClient(userPoolId);
            var authenticationClient = new AuthenticationClient(options => options.UserPoolId = userPoolId);
            authenticationClient.AccessToken = token;
            var jwtTokenStatus = await authenticationClient.CheckLoginStatus(token, cancellationToken);
            return jwtTokenStatus;
        }

        internal virtual async Task<Claim?> GetPlatformAdminClaim(string userId, CancellationToken cancellationToken)
        {
            var admin = await StorageContext.HackathonAdminTable.GetPlatformRole(userId, cancellationToken);
            if (admin != null && admin.IsPlatformAdministrator())
            {
                return ClaimsHelper.PlatformAdministrator(userId);
            }

            return null;
        }

        #region GetUserByIdAsync
        public async Task<UserInfo?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return null;
            }

            return await Cache.GetOrAddAsync(
                CacheKeys.GetCacheKey(CacheEntryType.User, userId),
                TimeSpan.FromHours(1),
                ct => StorageContext.UserTable.GetUserByIdAsync(userId, ct),
                false,
                cancellationToken);
        }
        #endregion

        #region SearchUserAsync
        public async Task<IEnumerable<UserEntity>> SearchUserAsync(UserQueryOptions options, CancellationToken cancellationToken = default)
        {
            List<UserEntity> results = new List<UserEntity>();

            Func<UserEntity, bool> filter = (u) =>
            {
                return (u.UserId != null && u.UserId.Contains(options.Search, StringComparison.OrdinalIgnoreCase))
                    || (u.Email != null && u.Email.Contains(options.Search, StringComparison.OrdinalIgnoreCase))
                    || (u.Phone != null && u.Phone.Contains(options.Search, StringComparison.OrdinalIgnoreCase))
                    || (u.Username != null && u.Username.Contains(options.Search, StringComparison.OrdinalIgnoreCase))
                    || (u.Name != null && u.Name.Contains(options.Search, StringComparison.OrdinalIgnoreCase))
                    || (u.GivenName != null && u.GivenName.Contains(options.Search, StringComparison.OrdinalIgnoreCase))
                    || (u.FamilyName != null && u.FamilyName.Contains(options.Search, StringComparison.OrdinalIgnoreCase))
                    || (u.MiddleName != null && u.MiddleName.Contains(options.Search, StringComparison.OrdinalIgnoreCase))
                    || (u.Nickname != null && u.Nickname.Contains(options.Search, StringComparison.OrdinalIgnoreCase));
            };

            string? continuationToken = "";
            do
            {
                var entities = await GetCachedUsersByPage(continuationToken, cancellationToken);
                results.AddRange(entities.Values.Where(filter));
                continuationToken = entities.ContinuationToken;
            } while (results.Count < options.Top && !string.IsNullOrWhiteSpace(continuationToken));

            return results.Take(options.Top);
        }

        private async Task<Page<UserEntity>> GetCachedUsersByPage(string continuationToken = "", CancellationToken cancellationToken = default)
        {
            return await Cache.GetOrAddAsync(
                CacheKeys.GetCacheKey(CacheEntryType.User, $"list-{continuationToken}"),
                TimeSpan.FromMinutes(15),
                ct => StorageContext.UserTable.ExecuteQuerySegmentedAsync(null, continuationToken, null, null, ct),
                false,
                cancellationToken);
        }
        #endregion
    }
}
