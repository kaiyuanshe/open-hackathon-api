using Azure;
using Kaiyuanshe.OpenHackathon.Server.Auth;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Collections.Generic;
using System.Security.Claims;

namespace Kaiyuanshe.OpenHackathon.ServerTests
{
    public static class MockHelper
    {
        public static IConfiguration CreateConfiguration(Dictionary<string, string> initialData)
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(initialData)
                .Build();

            return configuration;
        }

        public static Page<T> CreatePage<T>(List<T> result, string continuationToken)
        {
            return Page<T>.FromValues(result, continuationToken, new Mock<Response>().Object);
        }

        public static ClaimsPrincipal ClaimsPrincipalWithUserId(string userId)
        {
            var user = new ClaimsPrincipal(
                    new ClaimsIdentity(new List<Claim>
                    {
                        new Claim(AuthConstant.ClaimType.UserId, userId)
                    })
                );

            return user;
        }
    }
}
