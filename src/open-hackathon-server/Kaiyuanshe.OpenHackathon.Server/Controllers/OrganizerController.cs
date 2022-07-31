﻿using Kaiyuanshe.OpenHackathon.Server.Auth;
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
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.Controllers
{
    public class OrganizerController : HackathonControllerBase
    {
        #region CreateOrganizer
        /// <summary>
        /// Add a new organizer.
        /// </summary>
        /// <remarks>
        /// Add a company/organization who organizes the event. 
        /// Could a host, organizer, co-organizer, sponsor or title sponsor.
        /// </remarks>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <returns>The created organizer.</returns>
        /// <response code="200">Success. The response describes an organizer.</response>
        [HttpPut]
        [ProducesResponseType(typeof(Organizer), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400, 404)]
        [Route("hackathon/{hackathonName}/organizer")]
        [Authorize(Policy = AuthConstant.PolicyForSwagger.HackathonAdministrator)]
        public async Task<object> CreateOrganizer(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromBody, HttpPutPolicy] Organizer parameter,
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
            Debug.Assert(hackathon != null);

            // create organizer
            parameter.hackathonName = hackathonName.ToLower();
            var organizerEntity = await OrganizerManagement.CreateOrganizer(hackathonName.ToLower(), parameter, cancellationToken);

            var args = new
            {
                hackathonName = hackathon.DisplayName,
                adminName = CurrentUserDisplayName,
                organizerName = parameter.name,
            };
            await ActivityLogManagement.OnHackathonEvent(hackathon.Name, CurrentUserId,
                ActivityLogType.createOrganizer, args, cancellationToken);

            var resp = ResponseBuilder.BuildOrganizer(organizerEntity);
            return Ok(resp);
        }
        #endregion

        #region UpdateOrganizer
        /// <summary>
        /// Update an organizer by id.
        /// </summary>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <param name="organizerId" example="9ffbe751-975f-46a7-b4f7-48f2cc2805b0">Unique id of organizer. Generated by server. Please call the list api to query the id.</param>
        /// <returns>The updated organizer.</returns>
        /// <response code="200">Success. The response describes an organizer.</response>
        [HttpPatch]
        [ProducesResponseType(typeof(Organizer), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400, 404)]
        [Route("hackathon/{hackathonName}/organizer/{organizerId}")]
        [Authorize(Policy = AuthConstant.PolicyForSwagger.HackathonAdministrator)]
        public async Task<object> UpdateOrganizer(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromRoute, Required, Guid] string organizerId,
            [FromBody, Required] Organizer parameter,
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
            Debug.Assert(hackathon != null);

            // query and update organizer.
            var organizerEntity = await OrganizerManagement.GetOrganizerById(hackathon.Name, organizerId, cancellationToken);
            if (organizerEntity == null)
            {
                return NotFound(Resources.Organizer_NotFound);
            }
            organizerEntity = await OrganizerManagement.UpdateOrganizer(organizerEntity, parameter, cancellationToken);

            // logs
            var args = new
            {
                hackathonName = hackathon.DisplayName,
                adminName = CurrentUserDisplayName,
                organizerName = parameter.name,
            };
            await ActivityLogManagement.OnHackathonEvent(hackathon.Name, CurrentUserId,
                ActivityLogType.updateOrganizer, args, cancellationToken);

            var resp = ResponseBuilder.BuildOrganizer(organizerEntity);
            return Ok(resp);
        }
        #endregion

        #region GetOrganizer
        /// <summary>
        /// Get an organizer by id.
        /// </summary>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <param name="organizerId" example="9ffbe751-975f-46a7-b4f7-48f2cc2805b0">Unique id of organizer. Generated by server. Please call the list api to query the id.</param>
        /// <returns>The updated organizer.</returns>
        /// <response code="200">Success. The response describes an organizer.</response>
        [HttpGet]
        [ProducesResponseType(typeof(Organizer), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400, 404)]
        [Route("hackathon/{hackathonName}/organizer/{organizerId}")]
        public async Task<object> GetOrganizer(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromRoute, Required, Guid] string organizerId,
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
            Debug.Assert(hackathon != null);

            // query and update organizer.
            var organizerEntity = await OrganizerManagement.GetOrganizerById(hackathon.Name, organizerId, cancellationToken);
            if (organizerEntity == null)
            {
                return NotFound(Resources.Organizer_NotFound);
            }
            var resp = ResponseBuilder.BuildOrganizer(organizerEntity);
            return Ok(resp);
        }
        #endregion

        #region ListByHackathon
        /// <summary>
        /// List paginated organizers of a hackathon.
        /// </summary>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <returns>the response contains a list of judges and a nextLink if there are more results.</returns>
        /// <response code="200">Success. The response describes a list of organizers and a nullable link to query more results.</response>
        [HttpGet]
        [ProducesResponseType(typeof(OrganizerList), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400, 404)]
        [Route("hackathon/{hackathonName}/organizers")]
        public async Task<object> ListByHackathon(
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
            var queryOptions = new OrganizerQueryOptions
            {
                Pagination = pagination,
            };
            var organizers = await OrganizerManagement.ListPaginatedOrganizersAsync(hackathonName.ToLower(), queryOptions, cancellationToken);
            var routeValues = new RouteValueDictionary();
            if (pagination.top.HasValue)
            {
                routeValues.Add(nameof(pagination.top), pagination.top.Value);
            }
            var nextLink = BuildNextLinkUrl(routeValues, queryOptions.NextPage);

            // build resp
            var resp = ResponseBuilder.BuildResourceList<OrganizerEntity, Organizer, OrganizerList>(
                organizers,
                ResponseBuilder.BuildOrganizer,
                nextLink);

            return Ok(resp);
        }
        #endregion

        #region DeleteOrganizer
        /// <summary>
        /// Delete an organizor of a hackathon. 
        /// </summary>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <param name="organizerId" example="9ffbe751-975f-46a7-b4f7-48f2cc2805b0">Unique id of organizer. Generated by server. Please call the list api to query the id.</param>
        /// <response code="204">Deleted</response>
        [HttpDelete]
        [SwaggerErrorResponse(400, 404)]
        [Route("hackathon/{hackathonName}/organizer/{organizerId}")]
        [Authorize(Policy = AuthConstant.PolicyForSwagger.HackathonAdministrator)]
        public async Task<object> DeleteOrganizer(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromRoute, Guid, Required] string organizerId,
            CancellationToken cancellationToken)
        {
            // validate hackathon
            var hackathon = await HackathonManagement.GetHackathonEntityByNameAsync(hackathonName.ToLower(), cancellationToken);
            var options = new ValidateHackathonOptions
            {
                HackAdminRequird = true,
                HackathonName = hackathonName
            };
            if (await ValidateHackathon(hackathon, options, cancellationToken) == false)
            {
                return options.ValidateResult;
            }
            Debug.Assert(hackathon != null);

            // query and delete judge
            var entity = await OrganizerManagement.GetOrganizerById(hackathon.Name, organizerId, cancellationToken);
            if (entity == null)
            {
                return NoContent();
            }

            await OrganizerManagement.DeleteOrganizer(entity.PartitionKey, entity.RowKey, cancellationToken);
            var args = new
            {
                hackathonName = hackathon.DisplayName,
                adminName = CurrentUserDisplayName,
                organizerName = entity.Name,
            };
            await ActivityLogManagement.OnHackathonEvent(hackathon.Name, CurrentUserId, ActivityLogType.deleteOrganizer, args, cancellationToken);
            return NoContent();
        }
        #endregion
    }
}
