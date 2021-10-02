using k8s.Models;

namespace Kaiyuanshe.OpenHackathon.Server.K8S
{
    public static class V1StatusExtensions
    {
        public static bool IsFailed(this V1Status status)
        {
            if (status == null)
                return false;

            var code = status?.Code;
            if (code.HasValue)
            {
                return code.Value >= 400;
            }
            return false;
        }
    }
}
