using Kaiyuanshe.OpenHackathon.Server.Auth;
using Kaiyuanshe.OpenHackathon.Server.Biz;
using Kaiyuanshe.OpenHackathon.Server.Biz.Options;
using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Kaiyuanshe.OpenHackathon.Server.Swagger;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.Controllers
{
    public class EnrollmentController : HackathonControllerBase
    {
        #region Enroll
        /// <summary>
        /// Enroll a hackathon.
        /// </summary>
        /// <param name="parameter"></param>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <returns>The enrollment</returns>
        /// <response code="200">Success. The response describes a enrollment.</response>
        [HttpPut]
        [ProducesResponseType(typeof(Enrollment), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400, 404, 412)]
        [Route("hackathon/{hackathonName}/enrollment")]
        [Authorize(Policy = AuthConstant.PolicyForSwagger.LoginUser)]
        public async Task<object> Enroll(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromBody] Enrollment parameter,
            CancellationToken cancellationToken)
        {
            var hackathon = await HackathonManagement.GetHackathonEntityByNameAsync(hackathonName.ToLower());

            var options = new ValidateHackathonOptions
            {
                EnrollmentOpenRequired = true,
                EnrollmentNotFullRequired = true,
                OnlineRequired = true,
                HackathonName = hackathonName,
            };
            if (await ValidateHackathon(hackathon, options, cancellationToken) == false)
            {
                return options.ValidateResult;
            }
            Debug.Assert(hackathon != null);

            var enrollment = await EnrollmentManagement.GetEnrollmentAsync(hackathonName.ToLower(), CurrentUserId, cancellationToken);
            if (enrollment == null)
            {
                parameter.userId = CurrentUserId;
                enrollment = await EnrollmentManagement.CreateEnrollmentAsync(hackathon, parameter, cancellationToken);

                var args = new
                {
                    userName = CurrentUserDisplayName,
                    hackathonName = hackathon.DisplayName,
                };
                await ActivityLogManagement.OnHackathonEvent(hackathon.Name, CurrentUserId, ActivityLogType.createEnrollment, args, cancellationToken);

                var user = await UserManagement.GetUserByIdAsync(CurrentUserId, cancellationToken);
                Debug.Assert(user != null);
                return Ok(ResponseBuilder.BuildEnrollment(enrollment, user));
            }
            else
            {
                // update
                return await UpdateInternalAsync(hackathon, enrollment, parameter, cancellationToken);
            }
        }
        #endregion

        #region Update
        /// <summary>
        /// Update a enrollment. 
        /// </summary>
        /// <remarks>
        /// Update a enrollment. The enrolled user or hackathon admins can update a enrollment.
        /// Note that the status cannot be updated by this API, must call approve/reject API to update the status.
        /// </remarks>
        /// <param name="parameter"></param>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <param name="userId" example="1">Id of user</param>
        /// <returns>The enrollment</returns>
        /// <response code="200">Success. The response describes a enrollment.</response>
        [HttpPatch]
        [ProducesResponseType(typeof(Enrollment), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400, 403, 404)]
        [Route("hackathon/{hackathonName}/enrollment/{userId}")]
        [Authorize(Policy = AuthConstant.PolicyForSwagger.LoginUser)]
        public async Task<object> Update(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromRoute, Required] string userId,
            [FromBody] Enrollment parameter,
            CancellationToken cancellationToken)
        {
            var hackathon = await HackathonManagement.GetHackathonEntityByNameAsync(hackathonName.ToLower());

            var options = new ValidateHackathonOptions
            {
                EnrollmentOpenRequired = true,
                OnlineRequired = true,
                HackathonName = hackathonName,
                HackAdminRequird = (userId != CurrentUserId),
            };
            if (await ValidateHackathon(hackathon, options, cancellationToken) == false)
            {
                return options.ValidateResult;
            }
            Debug.Assert(hackathon != null);

            var enrollment = await EnrollmentManagement.GetEnrollmentAsync(hackathonName.ToLower(), userId, cancellationToken);
            if (enrollment == null)
            {
                return NotFound(string.Format(Resources.Enrollment_NotFound, userId, hackathonName));
            }

            return await UpdateInternalAsync(hackathon, enrollment, parameter, cancellationToken);
        }


        private async Task<object> UpdateInternalAsync(HackathonEntity hackathon, EnrollmentEntity existing, Enrollment request, CancellationToken cancellationToken)
        {
            var extensions = existing.Extensions.Merge(request.extensions);
            if (extensions.Length > Questionnaire.MaxExtensions)
            {
                return BadRequest(string.Format(Resources.Enrollment_TooManyExtensions, Questionnaire.MaxExtensions));
            }

            var enrollment = await EnrollmentManagement.UpdateEnrollmentAsync(existing, request, cancellationToken);
            var args = new
            {
                hackathonName = hackathon.DisplayName,
                userName = CurrentUserDisplayName,
            };
            if (enrollment.UserId == CurrentUserId)
            {
                await ActivityLogManagement.OnHackathonEvent(hackathon.Name, CurrentUserId, ActivityLogType.updateEnrollment, args, cancellationToken);
            }
            else
            {
                await ActivityLogManagement.OnUserEvent(hackathon.Name, enrollment.UserId, CurrentUserId,
                    ActivityLogType.updateEnrollment, args,
                    null, nameof(Resources.ActivityLog_User_updateEnrollment2), cancellationToken);
            }
            var user = await UserManagement.GetUserByIdAsync(existing.UserId, cancellationToken);
            Debug.Assert(user != null);
            return Ok(ResponseBuilder.BuildEnrollment(enrollment, user));
        }

        #endregion

        #region Get
        /// <summary>
        /// Get a hackathon enrollement for current user.
        /// </summary>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <returns>The enrollment</returns>
        /// <response code="200">Success.</response>
        [HttpGet]
        [ProducesResponseType(typeof(Enrollment), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400, 404)]
        [Route("hackathon/{hackathonName}/enrollment")]
        [Authorize(Policy = AuthConstant.PolicyForSwagger.LoginUser)]
        public async Task<object> Get(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            CancellationToken cancellationToken)
        {
            var enrollment = await EnrollmentManagement.GetEnrollmentAsync(hackathonName.ToLower(), CurrentUserId, cancellationToken);
            if (enrollment == null)
            {
                return NotFound(string.Format(Resources.Enrollment_NotFound, CurrentUserId, hackathonName));
            }
            var user = await UserManagement.GetUserByIdAsync(CurrentUserId, cancellationToken);
            Debug.Assert(user != null);
            return Ok(ResponseBuilder.BuildEnrollment(enrollment, user));
        }
        #endregion

        #region Approve && Reject
        /// <summary>
        /// Approve a hackathon enrollement.
        /// </summary>
        /// <param name="parameter"></param>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <param name="userId" example="1">Id of user</param>
        /// <returns>The enrollment</returns>
        /// <response code="200">Success. The enrollment is approved.</response>
        [HttpPost]
        [ProducesResponseType(typeof(Enrollment), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400, 403, 404)]
        [Route("hackathon/{hackathonName}/enrollment/{userId}/approve")]
        [Authorize(Policy = AuthConstant.PolicyForSwagger.HackathonAdministrator)]
        public async Task<object> Approve(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromRoute, Required] string userId,
            [FromBody] Enrollment parameter,
            CancellationToken cancellationToken)
        {
            return await UpdateEnrollmentStatus(hackathonName.ToLower(), userId.ToLower(), EnrollmentStatus.approved, cancellationToken);
        }

        /// <summary>
        /// Reject a hackathon enrollement.
        /// </summary>
        /// <param name="parameter"></param>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <param name="userId" example="1">Id of user</param>
        /// <returns>The enrollment</returns>
        /// <response code="200">Success. The enrollment is approved.</response>
        [HttpPost]
        [ProducesResponseType(typeof(Enrollment), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400, 403, 404)]
        [Route("hackathon/{hackathonName}/enrollment/{userId}/reject")]
        [Authorize(Policy = AuthConstant.PolicyForSwagger.HackathonAdministrator)]
        public async Task<object> Reject(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromRoute, Required] string userId,
            [FromBody] Enrollment parameter,
            CancellationToken cancellationToken)
        {
            return await UpdateEnrollmentStatus(hackathonName.ToLower(), userId.ToLower(), EnrollmentStatus.rejected, cancellationToken);
        }

        private async Task<object> UpdateEnrollmentStatus(string hackathonName, string userId, EnrollmentStatus status, CancellationToken cancellationToken)
        {
            var hackathon = await HackathonManagement.GetHackathonEntityByNameAsync(hackathonName);
            var options = new ValidateHackathonOptions
            {
                HackAdminRequird = true,
                HackathonName = hackathonName,
            };
            if (await ValidateHackathon(hackathon, options) == false)
            {
                return options.ValidateResult;
            }
            Debug.Assert(hackathon != null);

            var enrollment = await EnrollmentManagement.GetEnrollmentAsync(hackathonName, userId);
            if (enrollment == null)
            {
                return NotFound(string.Format(Resources.Enrollment_NotFound, userId, hackathonName));
            }
            Debug.Assert(enrollment != null);

            enrollment = await EnrollmentManagement.UpdateEnrollmentStatusAsync(hackathon, enrollment, status, cancellationToken);
            var enrolledUser = await UserManagement.GetUserByIdAsync(userId, cancellationToken);
            Debug.Assert(enrolledUser != null);
            var activityLogType = status == EnrollmentStatus.approved ? ActivityLogType.approveEnrollment : ActivityLogType.rejectEnrollment;
            var resKeyOfUser = status == EnrollmentStatus.approved ? nameof(Resources.ActivityLog_User_approveEnrollment2) : nameof(Resources.ActivityLog_User_rejectEnrollment2);
            var logArgs = new
            {
                hackathonName = hackathon.DisplayName,
                adminName = CurrentUserDisplayName,
                enrolledUser = enrolledUser.GetDisplayName(),
            };
            await ActivityLogManagement.OnUserEvent(hackathon.Name, userId, CurrentUserId,
                activityLogType, logArgs, resKeyOfUser, null, cancellationToken);

            var user = await UserManagement.GetUserByIdAsync(CurrentUserId, cancellationToken);
            Debug.Assert(user != null);
            return Ok(ResponseBuilder.BuildEnrollment(enrollment, user));
        }
        #endregion

        #region GetById
        /// <summary>
        /// Get a hackathon enrollement of any enrolled user.
        /// </summary>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <param name="userId" example="1">Id of user</param>
        /// <returns>The enrollment</returns>
        /// <response code="200">Success.</response>
        [HttpGet]
        [ProducesResponseType(typeof(Enrollment), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400, 404)]
        [Route("hackathon/{hackathonName}/enrollment/{userId}")]
        public async Task<object> GetById(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromRoute, Required] string userId,
            CancellationToken cancellationToken)
        {
            var hackName = hackathonName.ToLower();
            HackathonEntity? hackathon = await HackathonManagement.GetHackathonEntityByNameAsync(hackName, cancellationToken);
            var options = new ValidateHackathonOptions
            {
                HackathonName = hackathonName,
                UserId = userId,
                WritableRequired = false,
            };
            if (await ValidateHackathon(hackathon, options) == false)
            {
                return options.ValidateResult;
            }
            Debug.Assert(hackathon != null);

            EnrollmentEntity? enrollment = await EnrollmentManagement.GetEnrollmentAsync(hackName, userId.ToLower(), cancellationToken);
            if (enrollment == null)
            {
                return NotFound(string.Format(Resources.Enrollment_NotFound, userId, hackathonName));
            }
            var user = await UserManagement.GetUserByIdAsync(CurrentUserId, cancellationToken);
            Debug.Assert(user != null);

            return Ok(ResponseBuilder.BuildEnrollment(enrollment, user));
        }
        #endregion

        #region ListEnrollments
        /// <summary>
        /// List paginated enrollements of a hackathon.
        /// </summary>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <param name="status" example="approved">filter by enrollment status</param>
        /// <returns>the response contains a list of enrollments and a nextLink if there are more results.</returns>
        /// <response code="200">Success.</response>
        [HttpGet]
        [ProducesResponseType(typeof(EnrollmentList), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400, 404)]
        [Route("hackathon/{hackathonName}/enrollments")]
        public async Task<object> ListEnrollments(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromQuery] Pagination pagination,
            [FromQuery] EnrollmentStatus? status,
            CancellationToken cancellationToken)
        {
            var hackName = hackathonName.ToLower();
            HackathonEntity? hackathon = await HackathonManagement.GetHackathonEntityByNameAsync(hackName, cancellationToken);
            var options = new ValidateHackathonOptions
            {
                HackathonName = hackathonName,
                WritableRequired = false,
            };
            if (await ValidateHackathon(hackathon, options) == false)
            {
                return options.ValidateResult;
            }
            Debug.Assert(hackathon != null);

            var enrollmentOptions = new EnrollmentQueryOptions
            {
                Status = status,
                Pagination = pagination,
            };
            var page = await EnrollmentManagement.ListPaginatedEnrollmentsAsync(hackName, enrollmentOptions, cancellationToken);
            var routeValues = new RouteValueDictionary();
            if (pagination.top.HasValue)
            {
                routeValues.Add(nameof(pagination.top), pagination.top.Value);
            }
            if (status.HasValue)
            {
                routeValues.Add(nameof(status), status.Value);
            }
            var nextLink = BuildNextLinkUrl(routeValues, page.ContinuationToken);

            List<Tuple<EnrollmentEntity, UserInfo>> list = new List<Tuple<EnrollmentEntity, UserInfo>>();
            foreach (var enrollment in page.Values)
            {
                var user = await UserManagement.GetUserByIdAsync(enrollment.UserId, cancellationToken);
                Debug.Assert(user != null);
                list.Add(Tuple.Create(enrollment, user));
            }

            return Ok(ResponseBuilder.BuildResourceList<EnrollmentEntity, UserInfo, Enrollment, EnrollmentList>(
                    list,
                    ResponseBuilder.BuildEnrollment,
                    nextLink));
        }
        #endregion
    }
}
