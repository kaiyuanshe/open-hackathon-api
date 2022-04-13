using Azure;
using System.Collections.Generic;
using System.Security.Claims;

namespace Kaiyuanshe.OpenHackathon.ServerTests
{
    public static class MockHelper
    {
        public static Page<T> CreatePage<T>(List<T> result, string continuationToken)
        {
            return Page<T>.FromValues(result, continuationToken, null);
        }
    }
}
