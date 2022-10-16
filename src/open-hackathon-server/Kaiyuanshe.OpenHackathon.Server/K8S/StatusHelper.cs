using k8s.Models;
using Kaiyuanshe.OpenHackathon.Server.K8S.Models;

namespace Kaiyuanshe.OpenHackathon.Server.K8S
{
    public static class StatusHelper
    {
        public static V1Status InternalServerError()
        {
            return new V1Status
            {
                Code = 500,
                Reason = "Internal Server Error"
            };
        }

        public static ExperimentStatus InternalExperimentError()
        {
            return new ExperimentStatus
            {
                Code = 500,
                Reason = "Internal Server Error"
            };
        }
    }
}
