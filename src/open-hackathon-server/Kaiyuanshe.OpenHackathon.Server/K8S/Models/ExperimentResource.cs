﻿using k8s.Models;
using Kaiyuanshe.OpenHackathon.Server.Models;
using Newtonsoft.Json;

namespace Kaiyuanshe.OpenHackathon.Server.K8S.Models
{
    /// <summary>
    /// Experiment resource definition.
    /// Sample yaml: https://github.com/kaiyuanshe/cloudengine/blob/master/config/samples/hackathon_v1_experiment.yaml
    /// </summary>
    public class ExperimentResource : CustomResource<ExperimentSpec, ExperimentStatus>
    {
        // Properties of CustomResourceDefinition
        // See also: https://github.com/kaiyuanshe/cloudengine/blob/master/config/crd/bases/hackathon.kaiyuanshe.cn_experiments.yaml
        public static readonly string Plural = "experiments";
    }

    public class ExperimentSpec
    {
        [JsonProperty(PropertyName = "pause")]
        public bool Pause { get; set; }

        [JsonProperty(PropertyName = "template")]
        public string Template { get; set; }

        [JsonProperty(PropertyName = "clusterName")]
        public string ClusterName { get; set; }
    }

    public class ExperimentStatus : V1Status
    {
        [JsonProperty(PropertyName = "cluster")]
        public string ClusterName { get; set; }

        [JsonProperty(PropertyName = "ingressIPs")]
        public string[] IngressIPs { get; set; }

        [JsonProperty(PropertyName = "ingressPort")]
        public int IngressPort { get; set; }

        [JsonProperty(PropertyName = "protocol")]
        public IngressProtocol IngressProtocol { get; set; }

        [JsonProperty(PropertyName = "vnc")]
        public Vnc VncConnection { get; set; }
    }
}
