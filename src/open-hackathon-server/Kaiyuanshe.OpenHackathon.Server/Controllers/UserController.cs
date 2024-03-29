﻿using Kaiyuanshe.OpenHackathon.Server.Auth;
using Kaiyuanshe.OpenHackathon.Server.Biz.Options;
using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Kaiyuanshe.OpenHackathon.Server.Swagger.Annotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.Controllers
{
    public class UserController : HackathonControllerBase
    {
        #region Authing
        /// <summary>
        /// Post the data from Authing after completing the Login process. Open hackathon API
        /// relies on the data for user profile and the token.
        /// </summary>
        /// <param name="parameter"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpPost]
        [SwaggerErrorResponse(400)]
        [Route("login")]
        public async Task<object> Authing([FromBody] UserInfo parameter,
            CancellationToken cancellationToken)
        {
            Debug.Assert(parameter.Token != null);
            var tokenStatus = await UserManagement.ValidateTokenRemotelyAsync(parameter.UserPoolId, parameter.Token, cancellationToken);
            if (!tokenStatus.Status.GetValueOrDefault(false))
            {
                // token invalid
                return BadRequest(string.Format(
                    Resources.Auth_Token_ValidateRemoteFailed,
                    tokenStatus.Code.GetValueOrDefault(0),
                    tokenStatus.Message));
            }


            await UserManagement.AuthingAsync(parameter, cancellationToken);
            var args = new { userName = parameter.GetDisplayName() };
            await ActivityLogManagement.LogUserActivity(parameter.Id, null, parameter.Id, ActivityLogType.login, args, null, cancellationToken);
            return Ok(parameter);
        }
        #endregion

        #region GetUserById
        /// <summary>
        /// Get user info by user id. The info is synced from Authing during login.
        /// </summary>
        /// <param name="userId" example="1">unique id of the user</param>
        /// <param name="cancellationToken"></param>
        /// <returns>the user</returns>
        /// <response code="200">Success. The response describes a user.</response>
        [HttpGet]
        [ProducesResponseType(typeof(UserInfo), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(404)]
        [Route("user/{userId}")]
        public async Task<object> GetUserById([FromRoute, Required] string userId,
            CancellationToken cancellationToken)
        {
            var userInfo = await UserManagement.GetUserByIdAsync(userId, cancellationToken);
            if (userInfo == null)
            {
                return NotFound(Resources.User_NotFound);
            }
            return Ok(userInfo);
        }
        #endregion

        #region ListTopUsers
        /// <summary>
        /// List top users.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>the top users</returns>
        /// <response code="200">Success. The response describes a list of users.</response>
        [HttpGet]
        [ProducesResponseType(typeof(TopUserList), StatusCodes.Status200OK)]
        [Route("user/topUsers")]
        public async Task<object> ListTopUsers(CancellationToken cancellationToken)
        {
            var users = new List<TopUser>();

            var topUsers = await UserManagement.ListTopUsers(10, cancellationToken);
            foreach (var topUser in topUsers)
            {
                var userInfo = await UserManagement.GetUserByIdAsync(topUser.UserId, cancellationToken);
                Debug.Assert(userInfo != null);
                users.Add(ResponseBuilder.BuildTopUser(topUser, userInfo));
            }

            return Ok(new TopUserList
            {
                value = users.ToArray()
            });
        }
        #endregion

        #region SearchUser
        /// <summary>
        /// Search user by keyword. Will only return the top N records specified by parameter `top`. 
        /// `nextLink` is always empty even if there are more than N records in server side.
        /// </summary>
        /// <param name="keyword" example="someName">keyword to search. Will search in Name, Nickname and Email.</param>
        /// <param name="top" example="10">number of records to return. Must be between 1 and 100. Default to 10. </param>
        /// <returns>a list of users</returns>
        /// <response code="200">Success. The response describes a user.</response>
        [HttpPost]
        [ProducesResponseType(typeof(UserInfoList), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400)]
        [Route("user/search")]
        [Authorize(Policy = AuthConstant.PolicyForSwagger.LoginUser)]
        public async Task<object> SearchUser(
            [FromQuery, Required] string keyword,
            [FromQuery, Range(1, 100)] int? top,
            CancellationToken cancellationToken)
        {
            var options = new UserQueryOptions
            {
                Search = keyword,
                Top = top.GetValueOrDefault(10)
            };

            var entities = await UserManagement.SearchUserAsync(options, cancellationToken);
            var users = ResponseBuilder.BuildResourceList<UserEntity, UserInfo, UserInfoList>(entities, ResponseBuilder.BuildUser, null);
            return Ok(users);
        }
        #endregion

        #region GetUploadUrl
        /// <summary>
        /// Get file URLs including a readOnly url and a writable URL to upload.
        /// </summary>
        /// <remarks>
        /// The readOnly `url` is for website rendering. The `uploadUrl` which contains a SAS token is for client to upload the file.
        /// Follow https://docs.microsoft.com/en-us/rest/api/storageservices/put-blob to upload the file after get Sas Token.
        /// Basically PUT the file content to `uploadUrl`with required headers. 
        /// </remarks>
        /// <returns>an object contains and URL for upload and an URL for anonymous read.</returns>
        /// <response code="200">Success. The response contains and URL for upload and an URL for anonymous read.</response>
        [HttpPost]
        [ProducesResponseType(typeof(FileUpload), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400)]
        [Route("user/generateFileUrl")]
        [Authorize(Policy = AuthConstant.PolicyForSwagger.LoginUser)]
        public async Task<object> GetUploadUrl(
            [FromBody] FileUpload parameter,
            CancellationToken cancellationToken)
        {
            var result = FileManagement.GetUploadUrl(User, parameter);
            var args = new
            {
                userName = CurrentUserDisplayName
            };
            await ActivityLogManagement.LogUserActivity(CurrentUserId, null, CurrentUserId, ActivityLogType.fileUpload, args, null, cancellationToken);
            return Ok(result);
        }
        #endregion
    }
}
