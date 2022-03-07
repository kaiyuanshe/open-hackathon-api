using k8s.Models;
using Kaiyuanshe.OpenHackathon.Server.Models;

namespace Kaiyuanshe.OpenHackathon.Server.K8S.Models
{
    /// <summary>
    /// Experiment resource definition.
    /// Sample yaml: https://github.com/kaiyuanshe/cloudengine/blob/master/config/samples/hackathon_v1_experiment.yaml
    /// </summary>
    public class ExperimentResource : CustomResource<ExperimentSpec, ExperimentStatus>
    {
    }

    public class ExperimentSpec
    {
        public bool pause { get; set; }

        public string template { get; set; }

        public string clusterName { get; set; }
    }

    public class ExperimentStatus : V1Status
    {
        public string cluster { get; set; }

        public string[] ingressIPs { get; set; }

        public int ingressPort { get; set; }

        public IngressProtocol protocol { get; set; }

        public Vnc vnc { get; set; }
    }
}
