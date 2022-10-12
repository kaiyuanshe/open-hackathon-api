using Kaiyuanshe.OpenHackathon.Server.Auth;
using Kaiyuanshe.OpenHackathon.Server.Biz;
using Kaiyuanshe.OpenHackathon.Server.Biz.Options;
using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Models.Validations;
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.Controllers
{
    /// <summary>
    /// controller to manage team
    /// </summary>
    public class TeamController : HackathonControllerBase
    {
        #region CreateTeam
        /// <summary>
        /// Create a new team.
        /// </summary>
        /// <remarks>
        /// To create a new team, the following prerequisites must be met:
        /// <ul>
        ///     <li>The hackathon is online.</li>
        ///     <li>The hackathon has started and is not ended.</li>
        ///     <li>The current user has enrolled in the hackathon and the enrollment is approved.</li>
        ///     <li>The current user is not a member of another team in the same hackathon.</li>
        ///     <li>A team name must be unique in the same hackathon.</li>
        /// </ul>
        /// </remarks>
        /// <param name="parameter"></param>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <returns>The team</returns>
        /// <response code="200">Success. The response describes a team.</response>
        [HttpPut]
        [ProducesResponseType(typeof(Team), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400, 404, 412)]
        [Route("hackathon/{hackathonName}/team")]
        [Authorize(Policy = AuthConstant.PolicyForSwagger.LoginUser)]
        public async Task<object> CreateTeam(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromBody] Team parameter,
            CancellationToken cancellationToken)
        {
            // validate hackathon
            var hackathon = await HackathonManagement.GetHackathonEntityByNameAsync(hackathonName.ToLower(), cancellationToken);
            var options = new ValidateHackathonOptions
            {
                OnlineRequired = true,
                HackathonOpenRequired = true,
                HackathonName = hackathonName,
            };
            if (await ValidateHackathon(hackathon, options, cancellationToken) == false)
            {
                return options.ValidateResult;
            }

            // validate enrollment
            Debug.Assert(hackathon != null);
            var enrollment = await EnrollmentManagement.GetEnrollmentAsync(hackathon.Name, CurrentUserId, cancellationToken);
            if (enrollment == null)
            {
                return NotFound(string.Format(Resources.Enrollment_NotFound, CurrentUserId, hackathonName));
            }
            var enrollmentOptions = new ValidateEnrollmentOptions
            {
                ApprovedRequired = true,
                HackathonName = hackathonName,
            };
            if (ValidateEnrollment(enrollment, enrollmentOptions) == false)
            {
                return enrollmentOptions.ValidateResult;
            }

            // Check name exitance
            if (await IsTeamNameTaken(hackathonName.ToLower(), parameter.displayName, cancellationToken))
            {
                return PreconditionFailed(string.Format(Resources.Team_NameTaken, parameter.displayName));
            }

            // check team membership
            var teamMember = await TeamManagement.GetTeamMemberAsync(hackathonName.ToLower(), CurrentUserId, default);
            if (teamMember != null)
            {
                return PreconditionFailed(Resources.Team_CreateSecond);
            }

            // create team
            parameter.hackathonName = hackathonName.ToLower();
            parameter.creatorId = CurrentUserId;
            var teamEntity = await TeamManagement.CreateTeamAsync(parameter, cancellationToken);
            var args = new
            {
                hackathonName = hackathon.DisplayName,
                teamName = teamEntity.DisplayName,
                userName = CurrentUserDisplayName,
            };
            await ActivityLogManagement.OnTeamEvent(hackathon.Name, teamEntity.Id, CurrentUserId,
                 ActivityLogType.createTeam, args, cancellationToken);

            var creator = await GetCurrentUserInfo(cancellationToken);
#pragma warning disable CS8604 // Possible null reference argument.
            return Ok(ResponseBuilder.BuildTeam(teamEntity, creator));
#pragma warning restore CS8604 // Possible null reference argument.
        }

        private async Task<bool> IsTeamNameTaken(string hackathonName, string teamName, CancellationToken cancellationToken)
        {
            var teams = await TeamManagement.GetTeamByNameAsync(hackathonName, teamName, cancellationToken);
            return teams.Count() > 0;
        }
        #endregion

        #region CheckNameAvailability
        /// <summary>
        /// Check the team name availability.
        /// </summary>
        /// <remarks>
        /// Check if a team name is valid. <br />
        /// A name could be invalid because it's taken by another team or it's too long. 
        /// Team name(case-sensitive) must be unique in the same hackathon.
        /// Please choose a different name if not valid.
        /// </remarks>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <param name="parameter">parameter including the name to check</param>
        /// <param name="cancellationToken"></param>
        /// <returns>availability and a reason if not available.</returns>
        [HttpPost]
        [Route("hackathon/{hackathonName}/team/checkNameAvailability")]
        [SwaggerErrorResponse(400, 401)]
        [ProducesResponseType(typeof(NameAvailability), StatusCodes.Status200OK)]
        [Authorize(Policy = AuthConstant.PolicyForSwagger.LoginUser)]
        public async Task<object> CheckNameAvailability(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromBody, Required] NameAvailability parameter,
            CancellationToken cancellationToken)
        {
            if (parameter.name.Length > 128)
            {
                return parameter.Invalid(Resources.Team_NameTooLong);
            }

            if (await IsTeamNameTaken(hackathonName.ToLower(), parameter.name, cancellationToken))
            {
                return parameter.AlreadyExists(Resources.Team_NameTaken);
            }

            return parameter.OK();
        }
        #endregion

        #region UpdateTeam
        /// <summary>
        /// Update a team by teamId.
        /// </summary>
        /// <param name="parameter"></param>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <param name="teamId" example="d1e40c38-cc2a-445f-9eab-60c253256c57">unique Guid of the team. Auto-generated on server side.</param>
        /// <returns>The updated team</returns>
        /// <response code="200">Success. The response describes a team.</response>
        [HttpPatch]
        [ProducesResponseType(typeof(Team), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400, 403, 404)]
        [Route("hackathon/{hackathonName}/team/{teamId}")]
        [Authorize(Policy = AuthConstant.PolicyForSwagger.TeamAdministrator)]
        public async Task<object> UpdateTeam(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromRoute, Required, Guid] string teamId,
            [FromBody] Team parameter,
            CancellationToken cancellationToken)
        {
            // validate hackathon
            var hackathon = await HackathonManagement.GetHackathonEntityByNameAsync(hackathonName.ToLower(), cancellationToken);
            var options = new ValidateHackathonOptions
            {
                OnlineRequired = true,
                HackathonOpenRequired = true,
                HackathonName = hackathonName,
            };
            if (await ValidateHackathon(hackathon, options, cancellationToken) == false)
            {
                return options.ValidateResult;
            }

            // validate team
            Debug.Assert(hackathon != null);
            var team = await TeamManagement.GetTeamByIdAsync(hackathonName.ToLower(), teamId, cancellationToken);
            var teamOptions = new ValidateTeamOptions
            {
                TeamAdminRequired = true,
            };
            if (!await ValidateTeam(team, teamOptions, cancellationToken))
            {
                return teamOptions.ValidateResult;
            }

            // check name uniqueness
            Debug.Assert(team != null);
            if (parameter.displayName != null && team.DisplayName != parameter.displayName)
            {
                if (await IsTeamNameTaken(hackathonName.ToLower(), parameter.displayName, cancellationToken))
                {
                    return PreconditionFailed(string.Format(Resources.Team_NameTaken, parameter.displayName));
                }
            }

            team = await TeamManagement.UpdateTeamAsync(parameter, team, cancellationToken);
            var args = new
            {
                hackathonName = hackathon.DisplayName,
                teamName = team.DisplayName,
                adminName = CurrentUserDisplayName,
            };
            await ActivityLogManagement.OnTeamEvent(hackathon.Name, team.Id, CurrentUserId,
                 ActivityLogType.updateTeam, args, cancellationToken);

            var creator = await UserManagement.GetUserByIdAsync(team.CreatorId, cancellationToken);
            Debug.Assert(creator != null);
            return Ok(ResponseBuilder.BuildTeam(team, creator));
        }
        #endregion

        #region GetTeam
        /// <summary>
        /// Query a team by Id.
        /// </summary>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <param name="teamId" example="d1e40c38-cc2a-445f-9eab-60c253256c57">unique Guid of the team. Auto-generated on server side.</param>
        /// <returns>The team info.</returns>
        /// <response code="200">Success. The response describes a team.</response>
        [HttpGet]
        [ProducesResponseType(typeof(Team), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400, 404)]
        [Route("hackathon/{hackathonName}/team/{teamId}")]
        public async Task<object> GetTeam(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromRoute, Required, Guid] string teamId,
            CancellationToken cancellationToken)
        {
            // validate hackathon
            var hackathon = await HackathonManagement.GetHackathonEntityByNameAsync(hackathonName.ToLower(), cancellationToken);
            var options = new ValidateHackathonOptions
            {
                HackathonName = hackathonName,
                UserId = CurrentUserId,
                WritableRequired = false,
            };
            if (await ValidateHackathon(hackathon, options, cancellationToken) == false)
            {
                return options.ValidateResult;
            }

            // validate team
            var team = await TeamManagement.GetTeamByIdAsync(hackathonName.ToLower(), teamId, cancellationToken);
            var teamOptions = new ValidateTeamOptions
            {
            };
            if (!await ValidateTeam(team, teamOptions, cancellationToken))
            {
                return teamOptions.ValidateResult;
            }

            Debug.Assert(team != null);
            var creator = await UserManagement.GetUserByIdAsync(team.CreatorId, cancellationToken);
            Debug.Assert(creator != null);
            return Ok(ResponseBuilder.BuildTeam(team, creator));
        }
        #endregion

        #region GetCurrentTeam
        /// <summary>
        /// Query the team which the current user belongs to.
        /// </summary>
        /// <remarks>
        /// Valid auth token is required. 
        /// Will return the team info if user joined a team of the hackathon, otherwise 404 is returned.
        /// </remarks>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <returns>The team info.</returns>
        /// <response code="200">Success. The response describes a team.</response>
        [HttpGet]
        [ProducesResponseType(typeof(Team), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400, 404)]
        [Route("hackathon/{hackathonName}/team")]
        [Authorize(Policy = AuthConstant.PolicyForSwagger.LoginUser)]
        public async Task<object> GetCurrentTeam(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            CancellationToken cancellationToken)
        {
            // validate hackathon
            var hackathon = await HackathonManagement.GetHackathonEntityByNameAsync(hackathonName.ToLower(), cancellationToken);
            var options = new ValidateHackathonOptions
            {
                HackathonName = hackathonName,
                UserId = CurrentUserId,
                WritableRequired = false,
            };
            if (await ValidateHackathon(hackathon, options, cancellationToken) == false)
            {
                return options.ValidateResult;
            }

            // Query membership and team
            var member = await TeamManagement.GetTeamMemberAsync(hackathonName.ToLower(), CurrentUserId, cancellationToken);
            if (member == null)
            {
                return NotFound(Resources.Team_NotJoined);
            }
            var team = await TeamManagement.GetTeamByIdAsync(hackathonName.ToLower(), member.TeamId, cancellationToken);
            var teamOptions = new ValidateTeamOptions();
            if (!await ValidateTeam(team, teamOptions, cancellationToken))
            {
                return teamOptions.ValidateResult;
            }
            Debug.Assert(team != null);
            var creator = await UserManagement.GetUserByIdAsync(team.CreatorId, cancellationToken);
            Debug.Assert(creator != null);
            return Ok(ResponseBuilder.BuildTeam(team, creator));
        }
        #endregion

        #region ListTeams
        /// <summary>
        /// List paginated teams of a hackathon.
        /// </summary>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <param name="search" example="a">search team by display name. Matched if the displayName contains the pattern.
        /// </param>
        /// <returns>the response contains a list of teams and a nextLink if there are more results.</returns>
        /// <response code="200">Success.</response>
        [HttpGet]
        [ProducesResponseType(typeof(TeamList), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400, 404)]
        [Route("hackathon/{hackathonName}/teams")]
        public async Task<object> ListTeams(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromQuery] Pagination pagination,
            [FromQuery] string search,
            CancellationToken cancellationToken)
        {
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
            Debug.Assert(hackathon != null);

            var teamQueryOptions = new TeamQueryOptions
            {
                HackathonName = hackathon.Name,
                Pagination = pagination,
                NameSearch = search,
            };
            var page = await TeamManagement.ListPaginatedTeamsAsync(teamQueryOptions, cancellationToken);
            var routeValues = new RouteValueDictionary();
            if (pagination.top.HasValue)
            {
                routeValues.Add(nameof(pagination.top), pagination.top.Value);
            }
            var nextLink = BuildNextLinkUrl(routeValues, teamQueryOptions.NextPage);

            List<Tuple<TeamEntity, UserInfo>> tuples = new List<Tuple<TeamEntity, UserInfo>>();
            foreach (var team in page)
            {
                var creator = await UserManagement.GetUserByIdAsync(team.CreatorId, cancellationToken);
                Debug.Assert(creator != null);
                tuples.Add(Tuple.Create(team, creator));
            }
            return Ok(ResponseBuilder.BuildResourceList<TeamEntity, UserInfo, Team, TeamList>(
                    tuples,
                    ResponseBuilder.BuildTeam,
                    nextLink));
        }
        #endregion

        #region DeleteTeam
        /// <summary>
        /// Delete a team.
        /// </summary>
        /// <remarks>
        /// Delete a team by a team admin. <br />
        /// All team members will be removed from the team.
        /// </remarks>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <param name="teamId" example="d1e40c38-cc2a-445f-9eab-60c253256c57">unique Guid of the team. Auto-generated on server side.</param>
        /// <returns></returns>
        /// <response code="204">Deleted. The response indicates the team is removed.</response>
        [HttpDelete]
        [SwaggerErrorResponse(400, 403, 412)]
        [Route("hackathon/{hackathonName}/team/{teamId}")]
        [Authorize(Policy = AuthConstant.PolicyForSwagger.TeamAdministrator)]
        public async Task<object> DeleteTeam(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromRoute, Required, Guid] string teamId,
            CancellationToken cancellationToken)
        {
            // validate hackathon
            var hackathon = await HackathonManagement.GetHackathonEntityByNameAsync(hackathonName.ToLower(), cancellationToken);
            var options = new ValidateHackathonOptions
            {
                OnlineRequired = true,
                HackathonOpenRequired = true,
                HackathonName = hackathonName,
            };
            if (await ValidateHackathon(hackathon, options, cancellationToken) == false)
            {
                return options.ValidateResult;
            }
            Debug.Assert(hackathon != null);

            // Validate team
            var team = await TeamManagement.GetTeamByIdAsync(hackathonName.ToLower(), teamId, cancellationToken);
            if (team == null)
            {
                return NoContent();
            }
            var teamValidateOptions = new ValidateTeamOptions
            {
                TeamAdminRequired = true,
                NoAwardAssignmentRequired = true,
                NoRatingRequired = true,
            };
            if (await ValidateTeam(team, teamValidateOptions) == false)
            {
                return teamValidateOptions.ValidateResult;
            }

            // Delete team and member
            var members = await TeamManagement.ListTeamMembersAsync(hackathonName.ToLower(), teamId, cancellationToken);
            foreach (var member in members)
            {
                await TeamManagement.DeleteTeamMemberAsync(member, cancellationToken);
                var logArgs = new
                {
                    hackathonName = hackathon.DisplayName,
                    teamName = team.DisplayName,
                    operatorName = CurrentUserDisplayName,
                    memberName = member.UserId,
                };
                await ActivityLogManagement.OnTeamMemberEvent(hackathon.Name, team.Id, member.UserId, CurrentUserId,
                     ActivityLogType.deleteTeamMember, logArgs,
                     nameof(Resources.ActivityLog_User_deleteTeamMember2), null, cancellationToken);
            }
            await TeamManagement.DeleteTeamAsync(team, cancellationToken);
            var logArgs2 = new
            {
                hackathonName = hackathon.DisplayName,
                teamName = team.DisplayName,
                adminName = CurrentUserDisplayName,
            };
            await ActivityLogManagement.OnTeamEvent(hackathon.Name, team.Id, CurrentUserId,
                ActivityLogType.deleteTeam, logArgs2, cancellationToken);

            return NoContent();
        }
        #endregion

        #region JoinTeam
        /// <summary>
        /// Join a team.
        /// </summary>
        /// <remarks>
        /// Join a team. UserId is read from the auth token. Prerequisites: <br />
        /// <ul>
        ///     <li>The user must enroll the hackathon first before joining any team.</li>
        ///     <li>An user can join only one team in each hackathon. Repeated request with same teamId ends up 
        ///         with an Update request. Repeated request with different teamId ends with an error response. 
        ///         Quit the current team first if want to join another team.</li>
        /// </ul>
        /// </remarks>
        /// <param name="parameter"></param>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <param name="teamId" example="d1e40c38-cc2a-445f-9eab-60c253256c57">unique Guid of the team. Auto-generated on server side.</param>
        /// <returns>The team member</returns>
        /// <response code="200">Success. The response describes a team member.</response>
        [HttpPut]
        [ProducesResponseType(typeof(TeamMember), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400, 404, 412)]
        [Route("hackathon/{hackathonName}/team/{teamId}/member")]
        [Authorize(Policy = AuthConstant.PolicyForSwagger.LoginUser)]
        public async Task<object> JoinTeam(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromRoute, Required, Guid] string teamId,
            [FromBody] TeamMember parameter,
            CancellationToken cancellationToken)
        {
            return await AddTeamMemberInternalAsync(hackathonName.ToLower(), teamId, CurrentUserId, parameter, false, cancellationToken);
        }
        #endregion

        #region AddTeamMember
        /// <summary>
        /// Add a new member to a team by team admin.
        /// </summary>
        /// <remarks>
        /// UserId of the new member is read from the path. Prerequisites: <br />
        /// <ul>
        ///     <li>The user in access token must have admin access to the team.</li>
        ///     <li>The user must enroll the hackathon first before joining any team.</li>
        ///     <li>An user can join only one team in each hackathon. Repeated request with same teamId ends up 
        ///         with an Update request. Repeated request with different teamId ends with an error response. 
        ///         Quit the current team first if want to join another team.</li>
        /// </ul>
        /// </remarks>
        /// <param name="parameter"></param>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <param name="teamId" example="d1e40c38-cc2a-445f-9eab-60c253256c57">unique Guid of the team. Auto-generated on server side.</param>
        /// <param name="userId" example="1">Id of user</param>
        /// <returns>The team member</returns>
        /// <response code="200">Success. The response describes a team member.</response>
        [HttpPut]
        [ProducesResponseType(typeof(TeamMember), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400, 403, 404, 412)]
        [Route("hackathon/{hackathonName}/team/{teamId}/member/{userId}")]
        [Authorize(Policy = AuthConstant.PolicyForSwagger.TeamAdministrator)]
        public async Task<object> AddTeamMember(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromRoute, Required, Guid] string teamId,
            [FromRoute, Required] string userId,
            [FromBody] TeamMember parameter,
            CancellationToken cancellationToken)
        {
            return await AddTeamMemberInternalAsync(hackathonName.ToLower(), teamId, userId, parameter, true, cancellationToken);
        }

        private async Task<object> AddTeamMemberInternalAsync(string hackathonName, string teamId,
            string userId, TeamMember parameter, bool teamAdminRequired, CancellationToken cancellationToken)
        {
            // validate hackathon
            var hackathon = await HackathonManagement.GetHackathonEntityByNameAsync(hackathonName.ToLower(), cancellationToken);
            var options = new ValidateHackathonOptions
            {
                OnlineRequired = true,
                HackathonOpenRequired = true,
                HackathonName = hackathonName,
            };
            if (await ValidateHackathon(hackathon, options, cancellationToken) == false)
            {
                return options.ValidateResult;
            }

            // validate enrollment
            var enrollment = await EnrollmentManagement.GetEnrollmentAsync(hackathonName.ToLower(), userId, cancellationToken);
            var enrollmentOptions = new ValidateEnrollmentOptions
            {
                ApprovedRequired = true,
                HackathonName = hackathonName,
                UserId = userId,
            };
            if (ValidateEnrollment(enrollment, enrollmentOptions) == false)
            {
                return enrollmentOptions.ValidateResult;
            }

            // Validate team
            Debug.Assert(hackathon != null);
            var team = await TeamManagement.GetTeamByIdAsync(hackathonName.ToLower(), teamId, cancellationToken);
            var teamValidateOptions = new ValidateTeamOptions
            {
                TeamAdminRequired = teamAdminRequired,
            };
            if (await ValidateTeam(team, teamValidateOptions) == false)
            {
                return teamValidateOptions.ValidateResult;
            }
            Debug.Assert(team != null);

            // join team
            var memberInfo = await UserManagement.GetUserByIdAsync(userId, cancellationToken);
            Debug.Assert(memberInfo != null);
            var teamMember = await TeamManagement.GetTeamMemberAsync(hackathonName.ToLower(), userId, cancellationToken);
            if (teamMember == null)
            {
                parameter.hackathonName = hackathonName.ToLower();
                parameter.teamId = team.Id;
                parameter.userId = userId;
                parameter.status = TeamMemberStatus.pendingApproval;
                if (team.AutoApprove || teamAdminRequired)
                {
                    parameter.status = TeamMemberStatus.approved;
                }
                teamMember = await TeamManagement.CreateTeamMemberAsync(parameter, cancellationToken);
                var args = new
                {
                    hackathonName = hackathon.DisplayName,
                    teamName = team.DisplayName,
                    memberName = memberInfo.GetDisplayName(),
                    adminName = CurrentUserDisplayName,
                };
                if (userId != CurrentUserId)
                {
                    await ActivityLogManagement.OnTeamMemberEvent(hackathon.Name, team.Id, userId, CurrentUserId,
                         ActivityLogType.addTeamMember, args,
                         nameof(Resources.ActivityLog_User_addTeamMember2), null, cancellationToken);
                }
                else
                {
                    await ActivityLogManagement.OnTeamEvent(hackathon.Name, team.Id, CurrentUserId, ActivityLogType.joinTeam, args, cancellationToken);
                }
            }
            else
            {
                if (teamMember.TeamId != teamId)
                {
                    return PreconditionFailed(Resources.TeamMember_CannotChangeTeam);
                }

                teamMember = await TeamManagement.UpdateTeamMemberAsync(teamMember, parameter, cancellationToken);
                var args = new
                {
                    memberName = memberInfo.GetDisplayName(),
                    teamName = team.DisplayName,
                };
                await ActivityLogManagement.OnTeamMemberEvent(hackathon.Name, team.Id, userId,
                    CurrentUserId, ActivityLogType.updateTeamMember, args,
                    nameof(Resources.ActivityLog_User_updateTeamMember2), null, cancellationToken);
            }

            return Ok(ResponseBuilder.BuildTeamMember(teamMember, memberInfo));
        }
        #endregion

        #region UpdateTeamMember
        /// <summary>
        /// Update a team member information.
        /// </summary>
        /// <remarks>
        /// This API is eligible for all team members. <br />
        /// The Role/Status cannot be updated. Call other APIs to update Role/Status.
        /// </remarks>
        /// <param name="parameter"></param>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <param name="teamId" example="d1e40c38-cc2a-445f-9eab-60c253256c57">unique Guid of the team. Auto-generated on server side.</param>
        /// <param name="userId" example="1">Id of user</param>
        /// <returns>The updated team member</returns>
        /// <response code="200">Success. The response describes a team member.</response>
        [HttpPatch]
        [ProducesResponseType(typeof(TeamMember), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400, 403, 404)]
        [Route("hackathon/{hackathonName}/team/{teamId}/member/{userId}")]
        [Authorize(Policy = AuthConstant.PolicyForSwagger.TeamMember)]
        public async Task<object> UpdateTeamMember(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromRoute, Required, Guid] string teamId,
            [FromRoute, Required] string userId,
            [FromBody] TeamMember parameter,
            CancellationToken cancellationToken)
        {
            // validate hackathon
            var hackathon = await HackathonManagement.GetHackathonEntityByNameAsync(hackathonName.ToLower(), cancellationToken);
            var options = new ValidateHackathonOptions
            {
                OnlineRequired = true,
                HackathonOpenRequired = true,
                HackathonName = hackathonName,
            };
            if (await ValidateHackathon(hackathon, options, cancellationToken) == false)
            {
                return options.ValidateResult;
            }
            Debug.Assert(hackathon != null);

            // Validate team and member
            var team = await TeamManagement.GetTeamByIdAsync(hackathonName.ToLower(), teamId.ToLower(), cancellationToken);
            var teamMember = await TeamManagement.GetTeamMemberAsync(hackathonName.ToLower(), userId, cancellationToken);
            var teamMemberValidateOption = new ValidateTeamMemberOptions
            {
                TeamId = teamId,
                UserId = userId,
                ApprovedMemberRequired = true
            };
            if (await ValidateTeamMember(team, teamMember, teamMemberValidateOption, cancellationToken) == false)
            {
                return teamMemberValidateOption.ValidateResult;
            }
            Debug.Assert(team != null);
            Debug.Assert(teamMember != null);

            // Update team member
            var user = await UserManagement.GetUserByIdAsync(userId, cancellationToken);
            Debug.Assert(user != null);
            teamMember = await TeamManagement.UpdateTeamMemberAsync(teamMember, parameter, cancellationToken);
            var args = new
            {
                hackathonName = hackathon.DisplayName,
                memberName = user.GetDisplayName(),
                teamName = team.DisplayName,
            };

            await ActivityLogManagement.OnTeamMemberEvent(hackathon.Name, team.Id, userId, CurrentUserId,
                ActivityLogType.updateTeamMember, args,
                nameof(Resources.ActivityLog_User_updateTeamMember2), null, cancellationToken);

            return Ok(ResponseBuilder.BuildTeamMember(teamMember, user));
        }
        #endregion

        #region ApproveTeamMember
        /// <summary>
        /// Approve a team member
        /// </summary>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <param name="teamId" example="d1e40c38-cc2a-445f-9eab-60c253256c57">unique Guid of the team. Auto-generated on server side.</param>
        /// <param name="userId" example="1">Id of user</param>
        /// <returns>The updated team member</returns>
        /// <response code="200">Success. The response describes a team member.</response>
        [HttpPost]
        [ProducesResponseType(typeof(TeamMember), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400, 403, 404)]
        [Route("hackathon/{hackathonName}/team/{teamId}/member/{userId}/approve")]
        [Authorize(Policy = AuthConstant.PolicyForSwagger.TeamAdministrator)]
        public async Task<object> ApproveTeamMember(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromRoute, Required, Guid] string teamId,
            [FromRoute, Required] string userId,
            CancellationToken cancellationToken)
        {
            // validate hackathon
            var hackathon = await HackathonManagement.GetHackathonEntityByNameAsync(hackathonName.ToLower(), cancellationToken);
            var options = new ValidateHackathonOptions
            {
                OnlineRequired = true,
                HackathonOpenRequired = true,
                HackathonName = hackathonName,
            };
            if (await ValidateHackathon(hackathon, options, cancellationToken) == false)
            {
                return options.ValidateResult;
            }
            Debug.Assert(hackathon != null);

            // Validate team
            var team = await TeamManagement.GetTeamByIdAsync(hackathonName.ToLower(), teamId, cancellationToken);
            var teamValidateOptions = new ValidateTeamOptions
            {
                TeamAdminRequired = true
            };
            if (await ValidateTeam(team, teamValidateOptions) == false)
            {
                return teamValidateOptions.ValidateResult;
            }
            Debug.Assert(team != null);

            // Validate team member
            var teamMember = await TeamManagement.GetTeamMemberAsync(hackathonName.ToLower(), userId, cancellationToken);
            var teamMemberValidateOption = new ValidateTeamMemberOptions
            {
                TeamId = teamId,
                UserId = userId,
            };
            if (await ValidateTeamMember(team, teamMember, teamMemberValidateOption, cancellationToken) == false)
            {
                return teamMemberValidateOption.ValidateResult;
            }
            Debug.Assert(teamMember != null);

            // update status
            var user = await UserManagement.GetUserByIdAsync(userId, cancellationToken);
            Debug.Assert(user != null);
            teamMember = await TeamManagement.UpdateTeamMemberStatusAsync(teamMember, TeamMemberStatus.approved, cancellationToken);
            var logArgs = new
            {
                hackathonName = hackathon.DisplayName,
                teamName = team.DisplayName,
                adminName = CurrentUserDisplayName,
                memberName = user.GetDisplayName(),
            };
            await ActivityLogManagement.OnTeamMemberEvent(hackathon.Name, team.Id, userId, CurrentUserId,
                ActivityLogType.approveTeamMember, logArgs,
                nameof(Resources.ActivityLog_User_approveTeamMember2), null, cancellationToken);

            return Ok(ResponseBuilder.BuildTeamMember(teamMember, user));
        }
        #endregion

        #region UpdateTeamMemberRole
        /// <summary>
        /// Change the role of a team member.
        /// </summary>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <param name="teamId" example="d1e40c38-cc2a-445f-9eab-60c253256c57">unique Guid of the team. Auto-generated on server side.</param>
        /// <param name="userId" example="1">Id of user</param>
        /// <returns>The updated team member</returns>
        /// <response code="200">Success. The response describes a team member.</response>
        [HttpPost]
        [ProducesResponseType(typeof(TeamMember), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400, 403, 404)]
        [Route("hackathon/{hackathonName}/team/{teamId}/member/{userId}/updateRole")]
        [Authorize(Policy = AuthConstant.PolicyForSwagger.TeamAdministrator)]
        public async Task<object> UpdateTeamMemberRole(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromRoute, Required, Guid] string teamId,
            [FromRoute, Required] string userId,
            [FromBody] TeamMember parameter,
            CancellationToken cancellationToken)
        {
            // validate hackathon
            var hackathon = await HackathonManagement.GetHackathonEntityByNameAsync(hackathonName.ToLower(), cancellationToken);
            var options = new ValidateHackathonOptions
            {
                OnlineRequired = true,
                HackathonOpenRequired = true,
                HackathonName = hackathonName,
            };
            if (await ValidateHackathon(hackathon, options, cancellationToken) == false)
            {
                return options.ValidateResult;
            }
            Debug.Assert(hackathon != null);

            // Validate team
            var team = await TeamManagement.GetTeamByIdAsync(hackathonName.ToLower(), teamId, cancellationToken);
            var teamValidateOptions = new ValidateTeamOptions
            {
                TeamAdminRequired = true
            };
            if (await ValidateTeam(team, teamValidateOptions) == false)
            {
                return teamValidateOptions.ValidateResult;
            }
            Debug.Assert(team != null);

            // Validate team member
            var teamMember = await TeamManagement.GetTeamMemberAsync(hackathonName.ToLower(), userId, cancellationToken);
            var teamMemberValidateOption = new ValidateTeamMemberOptions
            {
                TeamId = teamId,
                UserId = userId,
            };
            if (await ValidateTeamMember(team, teamMember, teamMemberValidateOption, cancellationToken) == false)
            {
                return teamMemberValidateOption.ValidateResult;
            }
            Debug.Assert(teamMember != null);

            // validate not creator
            if (teamMember.UserId == team.CreatorId)
            {
                return PreconditionFailed(Resources.TeamMember_CannotUpdateCreatorRole);
            }

            // update status
            var memberInfo = await UserManagement.GetUserByIdAsync(userId, cancellationToken);
            teamMember = await TeamManagement.UpdateTeamMemberRoleAsync(teamMember, parameter.role.GetValueOrDefault(teamMember.Role), cancellationToken);
            Debug.Assert(memberInfo != null);
            var args = new
            {
                hackathonName = hackathon.DisplayName,
                adminName = CurrentUserDisplayName,
                memberName = memberInfo.GetDisplayName(),
                teamName = team.DisplayName,
            };
            await ActivityLogManagement.OnTeamMemberEvent(hackathon.Name, team.Id, userId,
                CurrentUserId, ActivityLogType.updateTeamMemberRole, args,
                nameof(Resources.ActivityLog_User_updateTeamMemberRole2), null, cancellationToken);

            return Ok(ResponseBuilder.BuildTeamMember(teamMember, memberInfo));
        }
        #endregion

        #region LeaveTeam
        /// <summary>
        /// Leave a team.
        /// </summary>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <param name="teamId" example="d1e40c38-cc2a-445f-9eab-60c253256c57">unique Guid of the team. Auto-generated on server side.</param>
        /// <returns></returns>
        /// <response code="204">Deleted. The response indicates the member is removed from the team.</response>
        [HttpDelete]
        [SwaggerErrorResponse(400, 404, 412)]
        [Route("hackathon/{hackathonName}/team/{teamId}/member")]
        [Authorize(Policy = AuthConstant.PolicyForSwagger.LoginUser)]
        public async Task<object> LeaveTeam(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromRoute, Required, Guid] string teamId,
            CancellationToken cancellationToken)
        {
            // validate hackathon
            var hackathon = await HackathonManagement.GetHackathonEntityByNameAsync(hackathonName.ToLower(), cancellationToken);
            var options = new ValidateHackathonOptions
            {
                OnlineRequired = true,
                HackathonOpenRequired = true,
                HackathonName = hackathonName,
            };
            if (await ValidateHackathon(hackathon, options, cancellationToken) == false)
            {
                return options.ValidateResult;
            }
            Debug.Assert(hackathon != null);

            // Validate team
            var team = await TeamManagement.GetTeamByIdAsync(hackathon.Name, teamId, cancellationToken);
            var teamValidateOptions = new ValidateTeamOptions
            {
            };
            if (await ValidateTeam(team, teamValidateOptions) == false)
            {
                return teamValidateOptions.ValidateResult;
            }
            Debug.Assert(team != null);

            // Delete team member
            return await DeleteMemberInternalAsync(hackathon, team, CurrentUserId, cancellationToken);
        }
        #endregion

        #region DeleteTeamMember
        /// <summary>
        /// Remove a team member or reject a team member joining request.  Team Admin only.
        /// </summary>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <param name="teamId" example="d1e40c38-cc2a-445f-9eab-60c253256c57">unique Guid of the team. Auto-generated on server side.</param>
        /// <param name="userId" example="1">Id of user</param>
        /// <returns></returns>
        /// <response code="204">Deleted. The response indicates the member is removed from the team.</response>
        [HttpDelete]
        [SwaggerErrorResponse(400, 403, 404, 412)]
        [Route("hackathon/{hackathonName}/team/{teamId}/member/{userId}")]
        [Authorize(Policy = AuthConstant.PolicyForSwagger.TeamAdministrator)]
        public async Task<object> DeleteTeamMember(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromRoute, Required, Guid] string teamId,
            [FromRoute, Required] string userId,
            CancellationToken cancellationToken)
        {
            // validate hackathon
            var hackathon = await HackathonManagement.GetHackathonEntityByNameAsync(hackathonName.ToLower(), cancellationToken);
            var options = new ValidateHackathonOptions
            {
                OnlineRequired = true,
                HackathonOpenRequired = true,
                HackathonName = hackathonName,
            };
            if (await ValidateHackathon(hackathon, options, cancellationToken) == false)
            {
                return options.ValidateResult;
            }
            Debug.Assert(hackathon != null);

            // Validate team
            var team = await TeamManagement.GetTeamByIdAsync(hackathon.Name, teamId, cancellationToken);
            var teamValidateOptions = new ValidateTeamOptions
            {
                TeamAdminRequired = true,
            };
            if (await ValidateTeam(team, teamValidateOptions) == false)
            {
                return teamValidateOptions.ValidateResult;
            }
            Debug.Assert(team != null);

            // Delete team member
            return await DeleteMemberInternalAsync(hackathon, team, userId, cancellationToken);
        }

        private async Task<object> DeleteMemberInternalAsync(HackathonEntity hackathon, TeamEntity team, string userId, CancellationToken cancellationToken)
        {
            var teamMember = await TeamManagement.GetTeamMemberAsync(hackathon.Name, userId, cancellationToken);
            if (teamMember == null)
            {
                // deleted already
                return NoContent();
            }

            // Creator must be the last person who leaves the team, to ensure there is at least one admin at any time.
            var members = await TeamManagement.ListTeamMembersAsync(hackathon.Name, team.Id, cancellationToken);
            if (members.Count() > 1 && userId == team.CreatorId)
            {
                return PreconditionFailed(Resources.TeamMember_LastAdmin);
            }

            // remove it
            await TeamManagement.DeleteTeamMemberAsync(teamMember, cancellationToken);
            var memberInfo = await UserManagement.GetUserByIdAsync(userId, cancellationToken);
            Debug.Assert(memberInfo != null);
            var logArgs = new
            {
                hackathonName = hackathon.DisplayName,
                teamName = team.DisplayName,
                memberName = memberInfo.GetDisplayName(),
                operatorName = CurrentUserDisplayName,
            };
            if (userId == CurrentUserId)
            {
                await ActivityLogManagement.OnTeamEvent(hackathon.Name, team.Id, CurrentUserId,
                     ActivityLogType.leaveTeam, logArgs, cancellationToken);
            }
            else
            {
                await ActivityLogManagement.OnTeamMemberEvent(hackathon.Name, team.Id, userId, CurrentUserId,
                     ActivityLogType.deleteTeamMember, logArgs,
                     nameof(Resources.ActivityLog_User_deleteTeamMember2), null, cancellationToken);
            }

            // dismiss team if all members are removed
            if (members.Count() == 1)
            {
                await TeamManagement.DeleteTeamAsync(team, cancellationToken);
                var logArgs2 = new
                {
                    hackathonName = hackathon.DisplayName,
                    teamName = team.DisplayName,
                    adminName = CurrentUserDisplayName,
                };
                await ActivityLogManagement.OnTeamEvent(hackathon.Name, team.Id, CurrentUserId,
                    ActivityLogType.deleteTeam, logArgs2, cancellationToken);
            }

            return NoContent();
        }
        #endregion

        #region GetTeamMember
        /// <summary>
        /// Query a team member
        /// </summary>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <param name="teamId" example="d1e40c38-cc2a-445f-9eab-60c253256c57">unique Guid of the team. Auto-generated on server side.</param>
        /// <param name="userId" example="1">Id of user</param>
        /// <returns>The team member</returns>
        /// <response code="200">Success. The response describes a team member.</response>
        [HttpGet]
        [ProducesResponseType(typeof(TeamMember), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400, 404)]
        [Route("hackathon/{hackathonName}/team/{teamId}/member/{userId}")]
        public async Task<object> GetTeamMember(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromRoute, Required, Guid] string teamId,
            [FromRoute, Required] string userId,
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

            // Validate team
            var team = await TeamManagement.GetTeamByIdAsync(hackathonName.ToLower(), teamId, cancellationToken);
            var teamValidateOptions = new ValidateTeamOptions
            {
            };
            if (await ValidateTeam(team, teamValidateOptions) == false)
            {
                return teamValidateOptions.ValidateResult;
            }
            Debug.Assert(team != null);

            // Validate team member
            var teamMember = await TeamManagement.GetTeamMemberAsync(hackathonName.ToLower(), userId, cancellationToken);
            var teamMemberValidateOption = new ValidateTeamMemberOptions
            {
                TeamId = teamId,
                UserId = userId,
            };
            if (await ValidateTeamMember(team, teamMember, teamMemberValidateOption, cancellationToken) == false)
            {
                return teamMemberValidateOption.ValidateResult;
            }
            Debug.Assert(teamMember != null);

            var user = await UserManagement.GetUserByIdAsync(userId, cancellationToken);
            Debug.Assert(user != null);
            return Ok(ResponseBuilder.BuildTeamMember(teamMember, user));
        }
        #endregion

        #region ListMembers
        /// <summary>
        /// List paginated members of a team.
        /// </summary>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <param name="teamId" example="d1e40c38-cc2a-445f-9eab-60c253256c57">unique Guid of the team. Auto-generated on server side.</param>
        /// <param name="status" example="approved">optional filter by member status.</param>
        /// <param name="role" example="member">optional filter by role of team member. </param>
        /// <returns>the response contains a list of team members and a nextLink if there are more results.</returns>
        /// <response code="200">Success.</response>
        [HttpGet]
        [ProducesResponseType(typeof(TeamMemberList), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400, 404)]
        [Route("hackathon/{hackathonName}/team/{teamId}/members")]
        public async Task<object> ListMembers(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromRoute, Required, Guid] string teamId,
            [FromQuery] Pagination pagination,
            [FromQuery] TeamMemberRole? role,
            [FromQuery] TeamMemberStatus? status,
            CancellationToken cancellationToken)
        {
            // validate hackathon
            HackathonEntity? hackathon = await HackathonManagement.GetHackathonEntityByNameAsync(hackathonName.ToLower());
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

            // Validate team
            var team = await TeamManagement.GetTeamByIdAsync(hackathon.Name, teamId, cancellationToken);
            var teamValidateOptions = new ValidateTeamOptions
            {
            };
            if (await ValidateTeam(team, teamValidateOptions) == false)
            {
                return teamValidateOptions.ValidateResult;
            }

            // list
            var qureyOptions = new TeamMemberQueryOptions
            {
                Pagination = pagination,
                Status = status,
                Role = role,
            };
            var page = await TeamManagement.ListPaginatedTeamMembersAsync(hackathonName.ToLower(), teamId, qureyOptions, cancellationToken);
            var routeValues = new RouteValueDictionary();
            if (pagination.top.HasValue)
            {
                routeValues.Add(nameof(pagination.top), pagination.top.Value);
            }
            if (status.HasValue)
            {
                routeValues.Add(nameof(status), status.Value);
            }
            if (role.HasValue)
            {
                routeValues.Add(nameof(role), role.Value);
            }
            var nextLink = BuildNextLinkUrl(routeValues, page.ContinuationToken);

            List<Tuple<TeamMemberEntity, UserInfo>> tuples = new List<Tuple<TeamMemberEntity, UserInfo>>();
            foreach (var member in page.Values)
            {
                var user = await UserManagement.GetUserByIdAsync(member.UserId, cancellationToken);
                Debug.Assert(user != null);
                tuples.Add(Tuple.Create(member, user));
            }
            return Ok(ResponseBuilder.BuildResourceList<TeamMemberEntity, UserInfo, TeamMember, TeamMemberList>(
                    tuples,
                    ResponseBuilder.BuildTeamMember,
                    nextLink));
        }
        #endregion

        #region ListAssignmentsByTeam
        /// <summary>
        /// List paginated award assignments of an team.
        /// </summary>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <param name="teamId" example="d1e40c38-cc2a-445f-9eab-60c253256c57">unique Guid of the team. Auto-generated on server side.</param>
        /// <returns>the response contains a list of award assginments and a nextLink if there are more results.</returns>
        /// <response code="200">Success. The response describes a list of award assignments.</response>
        [HttpGet]
        [ProducesResponseType(typeof(AwardAssignmentList), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400, 404)]
        [Route("hackathon/{hackathonName}/team/{teamId}/assignments")]
        public async Task<object> ListAssignmentsByTeam(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromRoute, Required, Guid] string teamId,
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

            // Validate team
            var teamEntity = await TeamManagement.GetTeamByIdAsync(hackathonName.ToLower(), teamId, cancellationToken);
            var teamValidateOptions = new ValidateTeamOptions
            {
            };
            if (await ValidateTeam(teamEntity, teamValidateOptions) == false)
            {
                return teamValidateOptions.ValidateResult;
            }
            Debug.Assert(teamEntity != null);

            // query
            var assignmentQueryOptions = new AwardAssignmentQueryOptions
            {
                Pagination = pagination,
                TeamId = teamId,
                QueryType = AwardAssignmentQueryType.Team,
            };
            var assignments = await AwardManagement.ListPaginatedAssignmentsAsync(hackathonName.ToLower(), assignmentQueryOptions, cancellationToken);
            var routeValues = new RouteValueDictionary();
            if (pagination.top.HasValue)
            {
                routeValues.Add(nameof(pagination.top), pagination.top.Value);
            }
            var nextLink = BuildNextLinkUrl(routeValues, assignmentQueryOptions.NextPage);

            // build resp
            var creator = await UserManagement.GetUserByIdAsync(teamEntity.CreatorId, cancellationToken);
            Debug.Assert(creator != null);
            var team = ResponseBuilder.BuildTeam(teamEntity, creator);
            var resp = await ResponseBuilder.BuildResourceListAsync<AwardAssignmentEntity, AwardAssignment, AwardAssignmentList>(
                assignments,
                (assignment, ct) => Task.FromResult(ResponseBuilder.BuildAwardAssignment(assignment, team, null)),
                nextLink);

            return Ok(resp);
        }
        #endregion

        #region CreateTeamWork
        /// <summary>
        /// Create a new team work.
        /// </summary>
        /// <remarks>
        /// Create a team work for demonstration. All approved team members are allowed to create or update a work. <br />
        /// A team work can be an image, a website, a PPT/Doc and so on. Be sure to input an valid URI. <br />
        /// A <b>maximum of 100</b> works is allowed for each team.
        /// </remarks>
        /// <param name="parameter"></param>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <param name="teamId" example="d1e40c38-cc2a-445f-9eab-60c253256c57">unique Guid of the team. Auto-generated on server side.</param>
        /// <returns>The work</returns>
        /// <response code="200">Success. The response describes a team work.</response>
        [HttpPut]
        [ProducesResponseType(typeof(TeamWork), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400, 404, 412)]
        [Route("hackathon/{hackathonName}/team/{teamId}/work")]
        [Authorize(Policy = AuthConstant.PolicyForSwagger.TeamMember)]
        public async Task<object> CreateTeamWork(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromRoute, Required, Guid] string teamId,
            [FromBody] TeamWork parameter,
            CancellationToken cancellationToken)
        {
            // validate hackathon
            var hackathon = await HackathonManagement.GetHackathonEntityByNameAsync(hackathonName.ToLower(), cancellationToken);
            var options = new ValidateHackathonOptions
            {
                OnlineRequired = true,
                HackathonOpenRequired = true,
                HackathonName = hackathonName,
            };
            if (await ValidateHackathon(hackathon, options, cancellationToken) == false)
            {
                return options.ValidateResult;
            }
            Debug.Assert(hackathon != null);

            // Validate team and member
            var team = await TeamManagement.GetTeamByIdAsync(hackathonName.ToLower(), teamId, cancellationToken);
            var teamMember = await TeamManagement.GetTeamMemberAsync(hackathonName.ToLower(), CurrentUserId, cancellationToken);
            var teamMemberValidateOption = new ValidateTeamMemberOptions
            {
                TeamId = teamId,
                UserId = CurrentUserId,
                ApprovedMemberRequired = true,
            };
            Debug.Assert(team != null);
            if (await ValidateTeamMember(team, teamMember, teamMemberValidateOption, cancellationToken) == false)
            {
                return teamMemberValidateOption.ValidateResult;
            }

            // check work count
            bool canCreate = await WorkManagement.CanCreateTeamWorkAsync(hackathonName.ToLower(), teamId, cancellationToken);
            if (!canCreate)
            {
                return PreconditionFailed(Resources.TeamWork_TooMany);
            }

            // create team work
            parameter.hackathonName = hackathonName.ToLower();
            parameter.teamId = teamId;
            var teamWorkEntity = await WorkManagement.CreateTeamWorkAsync(parameter, cancellationToken);
            var args = new
            {
                hackathonName = hackathon.DisplayName,
                teamName = team.DisplayName,
                userName = CurrentUserDisplayName,
            };
            await ActivityLogManagement.OnTeamEvent(hackathon.Name, team.Id, CurrentUserId, ActivityLogType.createTeamWork, args, cancellationToken);

            return Ok(ResponseBuilder.BuildTeamWork(teamWorkEntity));
        }
        #endregion

        #region UpdateTeamWork
        /// <summary>
        /// Update an existing team work
        /// </summary>
        /// /// <remarks>
        /// Update a team work. All approved team members are allowed to create or update a work. <br />
        /// A team work can be an image, a website, a PPT/Doc and so on. Be sure to input an valid URI. <br />
        /// </remarks>
        /// <param name="parameter"></param>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <param name="teamId" example="d1e40c38-cc2a-445f-9eab-60c253256c57">unique Guid of the team. Auto-generated on server side.</param>
        /// <param name="workId" example="c85e65ef-fd5e-4539-a1f8-bafb7e4f9d74">unique Guid of the work. Auto-generated on server side.</param>
        /// <returns>The work</returns>
        /// <response code="200">Success. The response describes a team work.</response>
        [HttpPatch]
        [ProducesResponseType(typeof(TeamWork), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400, 404)]
        [Route("hackathon/{hackathonName}/team/{teamId}/work/{workId}")]
        [Authorize(Policy = AuthConstant.PolicyForSwagger.TeamMember)]
        public async Task<object> UpdateTeamWork(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromRoute, Required, Guid] string teamId,
            [FromRoute, Required, Guid] string workId,
            [FromBody] TeamWork parameter,
            CancellationToken cancellationToken)
        {
            // validate hackathon
            var hackathon = await HackathonManagement.GetHackathonEntityByNameAsync(hackathonName.ToLower(), cancellationToken);
            var options = new ValidateHackathonOptions
            {
                OnlineRequired = true,
                HackathonOpenRequired = true,
                HackathonName = hackathonName,
            };
            if (await ValidateHackathon(hackathon, options, cancellationToken) == false)
            {
                return options.ValidateResult;
            }
            Debug.Assert(hackathon != null);

            // Validate team and member
            var team = await TeamManagement.GetTeamByIdAsync(hackathon.Name, teamId, cancellationToken);
            var teamMember = await TeamManagement.GetTeamMemberAsync(hackathon.Name, CurrentUserId, cancellationToken);
            var teamMemberValidateOption = new ValidateTeamMemberOptions
            {
                TeamId = teamId,
                UserId = CurrentUserId,
                ApprovedMemberRequired = true,
            };
            Debug.Assert(team != null);
            if (await ValidateTeamMember(team, teamMember, teamMemberValidateOption, cancellationToken) == false)
            {
                return teamMemberValidateOption.ValidateResult;
            }

            // create team work
            var teamWorkEntity = await WorkManagement.GetTeamWorkAsync(hackathon.Name, workId, cancellationToken);
            if (teamWorkEntity == null)
            {
                return NotFound(Resources.TeamWork_NotFound);
            }
            teamWorkEntity = await WorkManagement.UpdateTeamWorkAsync(teamWorkEntity, parameter, cancellationToken);
            var args = new
            {
                hackathonName = hackathon.DisplayName,
                teamName = team.DisplayName,
                memberName = CurrentUserDisplayName,
            };
            await ActivityLogManagement.OnTeamEvent(hackathon.Name, team.Id, CurrentUserId, ActivityLogType.updateTeamWork, args, cancellationToken);

            return Ok(ResponseBuilder.BuildTeamWork(teamWorkEntity));
        }
        #endregion

        #region GetTeamWork
        /// <summary>
        /// Query a team work.
        /// </summary>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <param name="teamId" example="d1e40c38-cc2a-445f-9eab-60c253256c57">unique Guid of the team. Auto-generated on server side.</param>
        /// <param name="workId" example="c85e65ef-fd5e-4539-a1f8-bafb7e4f9d74">unique Guid of the work. Auto-generated on server side.</param>
        /// <returns>The work</returns>
        /// <response code="200">Success. The response describes a team work.</response>
        [HttpGet]
        [ProducesResponseType(typeof(TeamWork), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400, 404)]
        [Route("hackathon/{hackathonName}/team/{teamId}/work/{workId}")]
        public async Task<object> GetTeamWork(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromRoute, Required, Guid] string teamId,
            [FromRoute, Required, Guid] string workId,
            CancellationToken cancellationToken)
        {
            // validate hackathon
            var hackathon = await HackathonManagement.GetHackathonEntityByNameAsync(hackathonName.ToLower(), cancellationToken);
            var options = new ValidateHackathonOptions
            {
                HackathonName = hackathonName,
                OnlineRequired = true,
                WritableRequired = false,
            };
            if (await ValidateHackathon(hackathon, options, cancellationToken) == false)
            {
                return options.ValidateResult;
            }

            // Validate team
            var team = await TeamManagement.GetTeamByIdAsync(hackathonName.ToLower(), teamId, cancellationToken);
            var teamValidateOptions = new ValidateTeamOptions
            {
            };
            if (await ValidateTeam(team, teamValidateOptions) == false)
            {
                return teamValidateOptions.ValidateResult;
            }
            Debug.Assert(team != null);

            // get team work
            var teamWorkEntity = await WorkManagement.GetTeamWorkAsync(hackathonName.ToLower(), workId, cancellationToken);
            if (teamWorkEntity == null)
            {
                return NotFound(Resources.TeamWork_NotFound);
            }
            return Ok(ResponseBuilder.BuildTeamWork(teamWorkEntity));
        }
        #endregion

        #region ListWorksByTeam
        /// <summary>
        /// List paginated works of an team.
        /// </summary>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <param name="teamId" example="d1e40c38-cc2a-445f-9eab-60c253256c57">unique Guid of the team. Auto-generated on server side.</param>
        /// <returns>the response contains a list of award assginments and a nextLink if there are more results.</returns>
        /// <response code="200">Success. The response describes a list of works.</response>
        [HttpGet]
        [ProducesResponseType(typeof(TeamWorkList), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400, 404)]
        [Route("hackathon/{hackathonName}/team/{teamId}/works")]
        public async Task<object> ListWorksByTeam(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromRoute, Required, Guid] string teamId,
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

            // Validate team
            var team = await TeamManagement.GetTeamByIdAsync(hackathonName.ToLower(), teamId, cancellationToken);
            var teamValidateOptions = new ValidateTeamOptions
            {
            };
            if (await ValidateTeam(team, teamValidateOptions) == false)
            {
                return teamValidateOptions.ValidateResult;
            }
            Debug.Assert(team != null);

            // query
            var teamWorkQueryOptions = new TeamWorkQueryOptions
            {
                Pagination = pagination,
            };
            var assignments = await WorkManagement.ListPaginatedWorksAsync(hackathonName.ToLower(), teamId.ToLower(), teamWorkQueryOptions, cancellationToken);
            var routeValues = new RouteValueDictionary();
            if (pagination.top.HasValue)
            {
                routeValues.Add(nameof(pagination.top), pagination.top.Value);
            }
            var nextLink = BuildNextLinkUrl(routeValues, teamWorkQueryOptions.NextPage);

            // build resp
            var resp = ResponseBuilder.BuildResourceList<TeamWorkEntity, TeamWork, TeamWorkList>(
                assignments,
                ResponseBuilder.BuildTeamWork,
                nextLink);

            return Ok(resp);
        }
        #endregion

        #region DeleteTeamWork
        /// <summary>
        /// Delete an team work by workId
        /// </summary>
        /// <remarks>
        /// All approved team members can create/update/delete works of team.
        /// </remarks>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <param name="teamId" example="d1e40c38-cc2a-445f-9eab-60c253256c57">unique Guid of the team. Auto-generated on server side.</param>
        /// <param name="workId" example="c85e65ef-fd5e-4539-a1f8-bafb7e4f9d74">unique Guid of the work. Auto-generated on server side.</param>
        /// <response code="204">Deleted. </response>
        [HttpDelete]
        [SwaggerErrorResponse(400, 404)]
        [Route("hackathon/{hackathonName}/team/{teamId}/work/{workId}")]
        [Authorize(Policy = AuthConstant.PolicyForSwagger.TeamMember)]
        public async Task<object> DeleteTeamWork(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
           [FromRoute, Required, Guid] string teamId,
            [FromRoute, Required, Guid] string workId,
            CancellationToken cancellationToken)
        {
            // validate hackathon
            var hackathon = await HackathonManagement.GetHackathonEntityByNameAsync(hackathonName.ToLower(), cancellationToken);
            var options = new ValidateHackathonOptions
            {
                HackathonName = hackathonName,
                HackathonOpenRequired = true,
                OnlineRequired = true,
            };
            if (await ValidateHackathon(hackathon, options, cancellationToken) == false)
            {
                return options.ValidateResult;
            }
            Debug.Assert(hackathon != null);

            // Validate team and member
            var team = await TeamManagement.GetTeamByIdAsync(hackathonName.ToLower(), teamId, cancellationToken);
            var teamMember = await TeamManagement.GetTeamMemberAsync(hackathonName.ToLower(), CurrentUserId, cancellationToken);
            var teamMemberValidateOption = new ValidateTeamMemberOptions
            {
                TeamId = teamId,
                UserId = CurrentUserId,
                ApprovedMemberRequired = true,
            };
            if (await ValidateTeamMember(team, teamMember, teamMemberValidateOption, cancellationToken) == false)
            {
                return teamMemberValidateOption.ValidateResult;
            }
            Debug.Assert(team != null);

            // Delete work
            var work = await WorkManagement.GetTeamWorkAsync(hackathonName.ToLower(), workId, cancellationToken);
            if (work == null)
            {
                return NoContent();
            }

            await WorkManagement.DeleteTeamWorkAsync(hackathonName.ToLower(), workId, cancellationToken);
            var args = new
            {
                hackathonName = hackathon.DisplayName,
                teamName = team.DisplayName,
                memberName = CurrentUserDisplayName,
            };
            await ActivityLogManagement.OnTeamEvent(hackathon.Name, team.Id, CurrentUserId, ActivityLogType.deleteTeamWork, args, cancellationToken);

            return NoContent();
        }
        #endregion
    }
}