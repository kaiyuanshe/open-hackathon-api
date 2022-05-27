using Kaiyuanshe.OpenHackathon.Server.Auth;
using Kaiyuanshe.OpenHackathon.Server.Biz;
using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Models.Validations;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Kaiyuanshe.OpenHackathon.Server.Swagger;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.Controllers
{
    public class AwardController : HackathonControllerBase
    {
        #region CreateAward
        /// <summary>
        /// Create a new award.
        /// </summary>
        /// <remarks>
        /// A hackathon can create up to 100 awards. Each award can be assigned to individuals or teams. 
        /// </remarks>
        /// <param name="parameter"></param>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <returns>The award</returns>
        /// <response code="200">Success. The response describes an award.</response>
        [HttpPut]
        [ProducesResponseType(typeof(Award), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400, 404, 412)]
        [Route("hackathon/{hackathonName}/award")]
        [Authorize(Policy = AuthConstant.PolicyForSwagger.HackathonAdministrator)]
        public async Task<object> CreateAward(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromBody] Award parameter,
            CancellationToken cancellationToken)
        {
            // validate hackathon
            var hackathon = await HackathonManagement.GetHackathonEntityByNameAsync(hackathonName.ToLower(), cancellationToken);
            var options = new ValidateHackathonOptions
            {
                HackAdminRequird = true,
                HackathonName = hackathonName,
            };
            if (await ValidateHackathon(hackathon, options, cancellationToken) == false)
            {
                return options.ValidateResult;
            }

            // check award count
            bool canCreate = await AwardManagement.CanCreateNewAward(hackathonName.ToLower(), cancellationToken);
            if (!canCreate)
            {
                return PreconditionFailed(Resources.Award_TooMany);
            }

            // create award
            parameter.hackathonName = hackathonName.ToLower();
            var awardEntity = await AwardManagement.CreateAwardAsync(hackathonName.ToLower(), parameter, cancellationToken);
            var logArgs = new
            {
                hackathonName = hackathon.DisplayName,
                userName = CurrentUserDisplayName,
                awardName = awardEntity.Name,
            };
            await ActivityLogManagement.OnHackathonEvent(hackathon.Name, CurrentUserId,
                ActivityLogType.createAward, logArgs, cancellationToken);

            return Ok(ResponseBuilder.BuildAward(awardEntity));
        }
        #endregion

        #region GetAward
        /// <summary>
        /// Query an award
        /// </summary>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <param name="awardId" example="c877c675-4c97-4deb-9e48-97d079fa4b72">unique Guid of the award. Auto-generated on server side.</param>
        /// <returns>The award</returns>
        /// <response code="200">Success. The response describes an award.</response>
        [HttpGet]
        [ProducesResponseType(typeof(Award), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400, 404)]
        [Route("hackathon/{hackathonName}/award/{awardId}")]
        public async Task<object> GetAward(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromRoute, Required, Guid] string awardId,
            CancellationToken cancellationToken)
        {
            // validate hackathon
            var hackathon = await HackathonManagement.GetHackathonEntityByNameAsync(hackathonName.ToLower(), cancellationToken);
            var options = new ValidateHackathonOptions
            {
                HackathonName = hackathonName,
                WritableRequired = false,
            };
            if (await ValidateHackathon(hackathon, options, cancellationToken) == false)
            {
                return options.ValidateResult;
            }

            // get award
            var award = await AwardManagement.GetAwardByIdAsync(hackathonName.ToLower(), awardId, cancellationToken);
            var awardOptions = new ValidateAwardOptions { };
            if (await ValidateAward(award, awardOptions, cancellationToken) == false)
            {
                return awardOptions.ValidateResult;
            }
            return Ok(ResponseBuilder.BuildAward(award));
        }
        #endregion

        #region UpdateAward
        /// <summary>
        /// Update an award
        /// </summary>
        /// <param name="parameter"></param>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <param name="awardId" example="c877c675-4c97-4deb-9e48-97d079fa4b72">unique Guid of the award. Auto-generated on server side.</param>
        /// <returns>The award</returns>
        /// <response code="200">Success. The response describes an award.</response>
        [HttpPatch]
        [ProducesResponseType(typeof(Award), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400, 403, 404, 412)]
        [Route("hackathon/{hackathonName}/award/{awardId}")]
        [Authorize(Policy = AuthConstant.PolicyForSwagger.HackathonAdministrator)]
        public async Task<object> UpdateAward(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromRoute, Required, Guid] string awardId,
            [FromBody] Award parameter,
            CancellationToken cancellationToken)
        {
            // validate hackathon
            var hackathon = await HackathonManagement.GetHackathonEntityByNameAsync(hackathonName.ToLower(), cancellationToken);
            var options = new ValidateHackathonOptions
            {
                HackAdminRequird = true,
                HackathonName = hackathonName,
            };
            if (await ValidateHackathon(hackathon, options, cancellationToken) == false)
            {
                return options.ValidateResult;
            }

            // validate award
            var award = await AwardManagement.GetAwardByIdAsync(hackathonName.ToLower(), awardId, cancellationToken);
            var awardOptions = new ValidateAwardOptions
            {
                TargetChangableRequired = true,
                NewTarget = parameter.target.GetValueOrDefault(award.Target),
            };
            if (await ValidateAward(award, awardOptions, cancellationToken) == false)
            {
                return awardOptions.ValidateResult;
            }

            // update award
            var updated = await AwardManagement.UpdateAwardAsync(award, parameter, cancellationToken);
            await ActivityLogManagement.LogActivity(new ActivityLogEntity
            {
                ActivityLogType = ActivityLogType.updateAward.ToString(),
                HackathonName = hackathonName.ToLower(),
                OperatorId = CurrentUserId,
                Message = updated.Name,
            }, cancellationToken);
            return Ok(ResponseBuilder.BuildAward(updated));
        }
        #endregion

        #region ListAwards
        /// <summary>
        /// List paginated awards of a hackathon.
        /// </summary>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <returns>the response contains a list of awards and a nextLink if there are more results.</returns>
        /// <response code="200">Success.</response>
        [HttpGet]
        [ProducesResponseType(typeof(AwardList), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400, 404)]
        [Route("hackathon/{hackathonName}/awards")]
        public async Task<object> ListAwards(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromQuery] Pagination pagination,
            CancellationToken cancellationToken)
        {
            // validate hackathon
            var hackathon = await HackathonManagement.GetHackathonEntityByNameAsync(hackathonName.ToLower());
            var options = new ValidateHackathonOptions
            {
                HackathonName = hackathonName,
                WritableRequired = false,
            };
            if (await ValidateHackathon(hackathon, options) == false)
            {
                return options.ValidateResult;
            }

            // query
            var awardQueryOptions = new AwardQueryOptions
            {
                Pagination = pagination,
            };
            var awards = await AwardManagement.ListPaginatedAwardsAsync(hackathonName.ToLower(), awardQueryOptions, cancellationToken);
            var routeValues = new RouteValueDictionary();
            if (pagination.top.HasValue)
            {
                routeValues.Add(nameof(pagination.top), pagination.top.Value);
            }
            var nextLink = BuildNextLinkUrl(routeValues, awardQueryOptions.NextPage);
            return Ok(ResponseBuilder.BuildResourceList<AwardEntity, Award, AwardList>(
                    awards,
                    ResponseBuilder.BuildAward,
                    nextLink));
        }
        #endregion

        #region DeleteAward
        /// <summary>
        /// Delete an award.
        /// </summary>
        /// <remarks>
        /// Delete a award that is not assigned to any individual or team. 
        /// If assigned, must delete the assignment(s) first.
        /// </remarks>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <param name="awardId" example="c877c675-4c97-4deb-9e48-97d079fa4b72">unique Guid of the award. Auto-generated on server side.</param>
        /// <response code="204">Success. The response indicates that an award is deleted.</response>
        [HttpDelete]
        [SwaggerErrorResponse(400, 404)]
        [Route("hackathon/{hackathonName}/award/{awardId}")]
        [Authorize(Policy = AuthConstant.PolicyForSwagger.HackathonAdministrator)]
        public async Task<object> DeleteAward(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromRoute, Required, Guid] string awardId,
            CancellationToken cancellationToken)
        {
            // validate hackathon
            var hackathon = await HackathonManagement.GetHackathonEntityByNameAsync(hackathonName.ToLower(), cancellationToken);
            var options = new ValidateHackathonOptions
            {
                HackAdminRequird = true,
                HackathonName = hackathonName,
            };
            if (await ValidateHackathon(hackathon, options, cancellationToken) == false)
            {
                return options.ValidateResult;
            }

            // get award
            var award = await AwardManagement.GetAwardByIdAsync(hackathonName.ToLower(), awardId, cancellationToken);
            if (award == null)
            {
                return NoContent();
            }
            var assignmentCount = await AwardManagement.GetAssignmentCountAsync(hackathonName.ToLower(), award.Id, cancellationToken);
            if (assignmentCount > 0)
            {
                return PreconditionFailed(Resources.Award_CannotDeleteAssigned);
            }

            // delete award
            await AwardManagement.DeleteAwardAsync(award, cancellationToken);
            var logArgs = new
            {
                hackathonName = hackathon.DisplayName,
                adminName = CurrentUserDisplayName,
                awardName = award.Name,
            };
            await ActivityLogManagement.OnHackathonEvent(hackathon.Name, CurrentUserId,
                ActivityLogType.deleteAward, logArgs, cancellationToken);

            return NoContent();
        }
        #endregion

        #region BuildAwardAssignment
        private async Task<AwardAssignment> BuildAwardAssignment(
            AwardAssignmentEntity awardAssignmentEntity,
            AwardEntity awardEntity,
            CancellationToken cancellationToken)
        {
            switch (awardEntity.Target)
            {
                case AwardTarget.team:
                    var teamEntity = await TeamManagement.GetTeamByIdAsync(awardEntity.HackathonName, awardAssignmentEntity.AssigneeId, cancellationToken);
                    var teamCreator = await UserManagement.GetUserByIdAsync(teamEntity.CreatorId, cancellationToken);
                    var team = ResponseBuilder.BuildTeam(teamEntity, teamCreator);
                    return ResponseBuilder.BuildAwardAssignment(awardAssignmentEntity, team, null);
                case AwardTarget.individual:
                    var user = await UserManagement.GetUserByIdAsync(awardAssignmentEntity.AssigneeId, cancellationToken);
                    return ResponseBuilder.BuildAwardAssignment(awardAssignmentEntity, null, user);
                default:
                    return null;
            }
        }
        #endregion

        #region CreateAwardAssignment
        /// <summary>
        /// Assign an award.
        /// </summary>
        /// <remarks>
        /// If target of the award is 'team', the assigneeId must be an id of a team. 
        /// Similarly, must be a valid userId if 'indiviual'. <br />
        /// In either case, can not assign more than the allowed maximum count. <br />
        /// The same team/user cannot be awarded the same award more than once. 
        /// Repeated calls have the same effect as an Update(Patch) operation.
        /// </remarks>
        /// <param name="parameter"></param>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <param name="awardId" example="c877c675-4c97-4deb-9e48-97d079fa4b72">unique Guid of the award. Auto-generated on server side.</param>
        /// <returns>The award assignment</returns>
        /// <response code="200">Success. The response describes an award assignment.</response>
        [HttpPut]
        [ProducesResponseType(typeof(AwardAssignment), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400, 403, 404, 412)]
        [Route("hackathon/{hackathonName}/award/{awardId}/assignment")]
        [Authorize(Policy = AuthConstant.PolicyForSwagger.HackathonAdministrator)]
        public async Task<object> CreateAwardAssignment(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromRoute, Required, Guid] string awardId,
            [FromBody, HttpPutPolicy] AwardAssignment parameter,
            CancellationToken cancellationToken)
        {
            // validate hackathon
            var hackathon = await HackathonManagement.GetHackathonEntityByNameAsync(hackathonName.ToLower(), cancellationToken);
            var options = new ValidateHackathonOptions
            {
                HackAdminRequird = true,
                HackathonName = hackathonName,
            };
            if (await ValidateHackathon(hackathon, options, cancellationToken) == false)
            {
                return options.ValidateResult;
            }

            // validate award
            var awardEntity = await AwardManagement.GetAwardByIdAsync(hackathonName.ToLower(), awardId, cancellationToken);
            var validateAwardOptions = new ValidateAwardOptions
            {
                QuantityCheckRequired = true,
                AssigneeExistRequired = true,
                AssigneeId = parameter.assigneeId,
            };
            if (await ValidateAward(awardEntity, validateAwardOptions, cancellationToken) == false)
            {
                return validateAwardOptions.ValidateResult;
            }

            parameter.hackathonName = hackathonName.ToLower();
            parameter.awardId = awardId;
            var assignment = await AwardManagement.CreateOrUpdateAssignmentAsync(parameter, cancellationToken);
            var logArgs = new
            {
                hackathonName = hackathon.DisplayName,
                operatorName = CurrentUserDisplayName,
                awardName = awardEntity.Name,
                teamName = validateAwardOptions.TeamToAssign?.DisplayName ?? "",
                userName = validateAwardOptions.UserToAssign?.GetDisplayName() ?? "",
            };
            if (awardEntity.Target == AwardTarget.individual)
            {
                await ActivityLogManagement.OnUserEvent(hackathon.Name, parameter.assigneeId, CurrentUserId,
                    ActivityLogType.createAwardAssignmentIndividual, logArgs,
                    nameof(Resources.ActivityLog_User_createAwardAssignmentIndividual2), null, cancellationToken);
            }
            else
            {
                await ActivityLogManagement.OnTeamEvent(hackathon.Name, parameter.assigneeId, CurrentUserId,
                    ActivityLogType.createAwardAssignmentTeam, logArgs, cancellationToken);
            }
            return Ok(await BuildAwardAssignment(assignment, awardEntity, cancellationToken));
        }
        #endregion

        #region UpdateAwardAssignment
        /// <summary>
        /// Update an award assignment. AwardId and AssigneeId will not be updated. 
        /// </summary>
        /// <remarks>
        /// Note that the AwardId and AssigneeId cannot not be updated. 
        /// To update AwardId/AssigneeId, please delete the assignment and assign again with a different AwardId/AssigneeId.
        /// </remarks>
        /// <param name="parameter"></param>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <param name="awardId" example="c877c675-4c97-4deb-9e48-97d079fa4b72">unique Guid of the award. Auto-generated on server side.</param>
        /// <param name="assignmentId" example="270d61b3-c676-403b-b582-cc38dfe122e4">unique Guid of the assignment. Auto-generated on server side.</param>
        /// <returns>The award assignment</returns>
        /// <response code="200">Success. The response describes an award assignment.</response>
        [HttpPatch]
        [ProducesResponseType(typeof(AwardAssignment), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400, 403, 404)]
        [Route("hackathon/{hackathonName}/award/{awardId}/assignment/{assignmentId}")]
        [Authorize(Policy = AuthConstant.PolicyForSwagger.HackathonAdministrator)]
        public async Task<object> UpdateAwardAssignment(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromRoute, Required, Guid] string awardId,
            [FromRoute, Required, Guid] string assignmentId,
            [FromBody] AwardAssignment parameter,
            CancellationToken cancellationToken)
        {
            // validate hackathon
            var hackathon = await HackathonManagement.GetHackathonEntityByNameAsync(hackathonName.ToLower(), cancellationToken);
            var options = new ValidateHackathonOptions
            {
                HackAdminRequird = true,
                HackathonName = hackathonName,
            };
            if (await ValidateHackathon(hackathon, options, cancellationToken) == false)
            {
                return options.ValidateResult;
            }

            // validate award
            var awardEntity = await AwardManagement.GetAwardByIdAsync(hackathonName.ToLower(), awardId, cancellationToken);
            var validateAwardOptions = new ValidateAwardOptions
            {
            };
            if (await ValidateAward(awardEntity, validateAwardOptions, cancellationToken) == false)
            {
                return validateAwardOptions.ValidateResult;
            }

            // update assignment
            var assignment = await AwardManagement.GetAssignmentAsync(hackathonName.ToLower(), assignmentId, cancellationToken);
            if (assignment == null)
            {
                return NotFound(Resources.AwardAssignment_NotFound);
            }
            assignment = await AwardManagement.UpdateAssignmentAsync(assignment, parameter, cancellationToken);
            var logArgs = new
            {
                hackathonName = hackathon.DisplayName,
                adminName = CurrentUserDisplayName,
                awardName = awardEntity.Name,
            };
            if (awardEntity.Target == AwardTarget.team)
            {
                await ActivityLogManagement.OnTeamEvent(hackathon.Name, assignment.AssigneeId, CurrentUserId,
                     ActivityLogType.updateAwardAssignment, logArgs, cancellationToken);
            }
            else
            {
                await ActivityLogManagement.OnUserEvent(hackathon.Name, assignment.AssigneeId, CurrentUserId,
                     ActivityLogType.updateAwardAssignment, logArgs,
                     nameof(Resources.ActivityLog_User_updateAwardAssignment2), null, cancellationToken);
            }

            return Ok(await BuildAwardAssignment(assignment, awardEntity, cancellationToken));
        }
        #endregion

        #region GetAwardAssignment
        /// <summary>
        /// Get an award assignment by assignmentId
        /// </summary>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <param name="awardId" example="c877c675-4c97-4deb-9e48-97d079fa4b72">unique Guid of the award. Auto-generated on server side.</param>
        /// <param name="assignmentId" example="270d61b3-c676-403b-b582-cc38dfe122e4">unique Guid of the assignment. Auto-generated on server side.</param>
        /// <returns>The award assignment</returns>
        /// <response code="200">Success. The response describes an award assignment.</response>
        [HttpGet]
        [ProducesResponseType(typeof(AwardAssignment), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400, 404)]
        [Route("hackathon/{hackathonName}/award/{awardId}/assignment/{assignmentId}")]
        public async Task<object> GetAwardAssignment(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromRoute, Required, Guid] string awardId,
            [FromRoute, Required, Guid] string assignmentId,
            CancellationToken cancellationToken)
        {
            // validate hackathon
            var hackathon = await HackathonManagement.GetHackathonEntityByNameAsync(hackathonName.ToLower(), cancellationToken);
            var options = new ValidateHackathonOptions
            {
                HackathonName = hackathonName,
                WritableRequired = false,
            };
            if (await ValidateHackathon(hackathon, options, cancellationToken) == false)
            {
                return options.ValidateResult;
            }

            // validate award
            var awardEntity = await AwardManagement.GetAwardByIdAsync(hackathonName.ToLower(), awardId, cancellationToken);
            var validateAwardOptions = new ValidateAwardOptions
            {
            };
            if (await ValidateAward(awardEntity, validateAwardOptions, cancellationToken) == false)
            {
                return validateAwardOptions.ValidateResult;
            }

            // update assignment
            var assignment = await AwardManagement.GetAssignmentAsync(hackathonName.ToLower(), assignmentId, cancellationToken);
            if (assignment == null)
            {
                return NotFound(Resources.AwardAssignment_NotFound);
            }
            return Ok(await BuildAwardAssignment(assignment, awardEntity, cancellationToken));
        }
        #endregion

        #region ListAssignmentsByAward
        /// <summary>
        /// List paginated assignments of an award.
        /// </summary>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <param name="awardId" example="c877c675-4c97-4deb-9e48-97d079fa4b72">unique Guid of the award. Auto-generated on server side.</param>
        /// <returns>the response contains a list of award assginments and a nextLink if there are more results.</returns>
        /// <response code="200">Success. The response describes a list of award assignments.</response>
        [HttpGet]
        [ProducesResponseType(typeof(AwardAssignmentList), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400, 404)]
        [Route("hackathon/{hackathonName}/award/{awardId}/assignments")]
        public async Task<object> ListAssignmentsByAward(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromRoute, Required, Guid] string awardId,
            [FromQuery] Pagination pagination,
            CancellationToken cancellationToken)
        {
            // validate hackathon
            var hackathon = await HackathonManagement.GetHackathonEntityByNameAsync(hackathonName.ToLower());
            var options = new ValidateHackathonOptions
            {
                HackathonName = hackathonName,
                WritableRequired = false,
            };
            if (await ValidateHackathon(hackathon, options) == false)
            {
                return options.ValidateResult;
            }

            // validate award
            var awardEntity = await AwardManagement.GetAwardByIdAsync(hackathonName.ToLower(), awardId, cancellationToken);
            var validateAwardOptions = new ValidateAwardOptions
            {
            };
            if (await ValidateAward(awardEntity, validateAwardOptions, cancellationToken) == false)
            {
                return validateAwardOptions.ValidateResult;
            }

            // query
            var assignmentQueryOptions = new AwardAssignmentQueryOptions
            {
                Pagination = pagination,
                AwardId = awardId,
                QueryType = AwardAssignmentQueryType.Award,
            };
            var assignments = await AwardManagement.ListPaginatedAssignmentsAsync(hackathonName.ToLower(), assignmentQueryOptions, cancellationToken);
            var routeValues = new RouteValueDictionary();
            if (pagination.top.HasValue)
            {
                routeValues.Add(nameof(pagination.top), pagination.top.Value);
            }
            var nextLink = BuildNextLinkUrl(routeValues, assignmentQueryOptions.NextPage);

            var resp = await ResponseBuilder.BuildResourceListAsync<AwardAssignmentEntity, AwardAssignment, AwardAssignmentList>(
                assignments,
                (assignment, ct) => BuildAwardAssignment(assignment, awardEntity, ct),
                nextLink);

            return Ok(resp);
        }
        #endregion

        #region ListAssignmentsByHackathon
        /// <summary>
        /// List paginated assignments of a hackathon.
        /// </summary>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <returns>the response contains a list of award assginments and a nextLink if there are more results.</returns>
        /// <response code="200">Success. The response describes a list of award assignments.</response>
        [HttpGet]
        [ProducesResponseType(typeof(AwardAssignmentList), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400, 404)]
        [Route("hackathon/{hackathonName}/assignments")]
        public async Task<object> ListAssignmentsByHackathon(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromQuery] Pagination pagination,
            CancellationToken cancellationToken)
        {
            // validate hackathon
            var hackathon = await HackathonManagement.GetHackathonEntityByNameAsync(hackathonName.ToLower());
            var options = new ValidateHackathonOptions
            {
                HackathonName = hackathonName,
                WritableRequired = false,
            };
            if (await ValidateHackathon(hackathon, options) == false)
            {
                return options.ValidateResult;
            }

            // query
            var assignmentQueryOptions = new AwardAssignmentQueryOptions
            {
                Pagination = pagination,
                QueryType = AwardAssignmentQueryType.Hackathon,
            };
            var assignments = await AwardManagement.ListPaginatedAssignmentsAsync(hackathonName.ToLower(), assignmentQueryOptions, cancellationToken);
            var routeValues = new RouteValueDictionary();
            if (pagination.top.HasValue)
            {
                routeValues.Add(nameof(pagination.top), pagination.top.Value);
            }
            var nextLink = BuildNextLinkUrl(routeValues, assignmentQueryOptions.NextPage);

            var awards = await AwardManagement.ListAwardsAsync(hackathonName.ToLower(), cancellationToken);
            var resp = await ResponseBuilder.BuildResourceListAsync<AwardAssignmentEntity, AwardAssignment, AwardAssignmentList>(
                assignments,
                async (assignment, ct) =>
                {
                    var awardEntity = awards.SingleOrDefault(a => a.Id == assignment.AwardId);
                    if (awardEntity == null)
                        return null;
                    return await BuildAwardAssignment(assignment, awardEntity, ct);
                },
                nextLink);

            return Ok(resp);
        }
        #endregion

        #region DeleteAwardAssignment
        /// <summary>
        /// Delete an award assignment by assignmentId.
        /// </summary>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <param name="awardId" example="c877c675-4c97-4deb-9e48-97d079fa4b72">unique Guid of the award. Auto-generated on server side.</param>
        /// <param name="assignmentId" example="270d61b3-c676-403b-b582-cc38dfe122e4">unique Guid of the assignment. Auto-generated on server side.</param>
        /// <returns></returns>
        /// <response code="204">Deleted. </response>
        [HttpDelete]
        [SwaggerErrorResponse(400, 404)]
        [Route("hackathon/{hackathonName}/award/{awardId}/assignment/{assignmentId}")]
        [Authorize(Policy = AuthConstant.PolicyForSwagger.HackathonAdministrator)]
        public async Task<object> DeleteAwardAssignment(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromRoute, Required, Guid] string awardId,
            [FromRoute, Required, Guid] string assignmentId,
            CancellationToken cancellationToken)
        {
            // validate hackathon
            var hackathon = await HackathonManagement.GetHackathonEntityByNameAsync(hackathonName.ToLower(), cancellationToken);
            var options = new ValidateHackathonOptions
            {
                HackathonName = hackathonName,
                HackAdminRequird = true,
            };
            if (await ValidateHackathon(hackathon, options, cancellationToken) == false)
            {
                return options.ValidateResult;
            }

            // validate award
            var awardEntity = await AwardManagement.GetAwardByIdAsync(hackathonName.ToLower(), awardId, cancellationToken);
            var validateAwardOptions = new ValidateAwardOptions
            {
            };
            if (await ValidateAward(awardEntity, validateAwardOptions, cancellationToken) == false)
            {
                return validateAwardOptions.ValidateResult;
            }

            // Delete assignment
            var assignment = await AwardManagement.GetAssignmentAsync(hackathonName.ToLower(), assignmentId, cancellationToken);
            if (assignment == null)
            {
                return NoContent();
            }
            await AwardManagement.DeleteAssignmentAsync(hackathonName.ToLower(), assignmentId, cancellationToken);

            var logArgs = new
            {
                hackathonName = hackathon.Name,
                adminName = CurrentUserDisplayName,
                awardName = awardEntity.Name,
            };
            if (awardEntity.Target == AwardTarget.individual)
            {
                await ActivityLogManagement.OnUserEvent(hackathon.Name, assignment.AssigneeId, CurrentUserId,
                    ActivityLogType.deleteAwardAssignment, logArgs,
                    nameof(Resources.ActivityLog_User_deleteAwardAssignment2), null, cancellationToken);
            }
            else
            {
                await ActivityLogManagement.OnTeamEvent(hackathon.Name, assignment.AssigneeId, CurrentUserId,
                    ActivityLogType.deleteAwardAssignment, logArgs, cancellationToken);
            }
            return NoContent();
        }
        #endregion
    }
}
