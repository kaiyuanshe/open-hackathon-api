using Kaiyuanshe.OpenHackathon.Server.Swagger.Annotations;
using Microsoft.AspNetCore.Authorization;

namespace Kaiyuanshe.OpenHackathon.ServerTests.Swagger
{
    public class FakeController
    {
        public void ActionWithNoParameters()
        { }

        public void ActionWithEnumParameter(FakeEnum param1)
        { }

        [Authorize]
        public void ActionWithTokenRequired()
        { }

        [SwaggerErrorResponse(400, 401)]
        [SwaggerErrorResponse(400, 403, 412)]
        [SwaggerErrorResponse(403, 404, 429)]
        public void ActionWithErrorResponseCodes()
        { }
    }
}
