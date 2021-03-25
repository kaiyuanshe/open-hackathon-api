﻿using Kaiyuanshe.OpenHackathon.Server.Biz;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.Auth
{
    public class HackathonAdministratorRequirement : IAuthorizationRequirement
    {

    }

    public class HackathonAdministratorHandler : AuthorizationHandler<HackathonAdministratorRequirement, HackathonEntity>
    {
        IHackathonManagement hackathonManagement { get; set; }
        public HackathonAdministratorHandler(IHackathonManagement hackathonManagement)
        {
            this.hackathonManagement = hackathonManagement;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, HackathonAdministratorRequirement requirement, HackathonEntity resource)
        {
            if (ClaimsHelper.IsPlatformAdministrator(context.User))
            {
                context.Succeed(requirement);
                return;
            }

            string userId = context?.User?.Claims?.FirstOrDefault(c => c.Type == AuthConstant.ClaimType.UserId)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                // anonymous user
                return;
            }

            var hackathonAdmins = await hackathonManagement.ListHackathonAdminAsync(resource.Name);
            if (hackathonAdmins.Any(a => a.UserId == userId))
            {
                context.Succeed(requirement);
            }
        }
    }
}