using k8s;
using k8s.Models;
using Newtonsoft.Json;

namespace Kaiyuanshe.OpenHackathon.Server.K8S.Models
{
    public abstract class CustomResource : KubernetesObject
    {
        [JsonProperty(PropertyName = "metadata")]
        public V1ObjectMeta Metadata { get; set; }

        //CRD: https://github.com/kaiyuanshe/cloudengine/blob/master/config/crd/bases/hackathon.kaiyuanshe.cn_experiments.yaml
        // group/version can be found in above link. The same values for template/experiment
        public static readonly string Group = "hackathon.kaiyuanshe.cn";
        public static readonly string Version = "v1";
        public static readonly string API_VERSION = "hackathon.kaiyuanshe.cn/v1";
    }

    public abstract class CustomResource<TSpec, TStatus> : CustomResource
    {
        [JsonProperty(PropertyName = "spec")]
        public TSpec Spec { get; set; }

        [JsonProperty(PropertyName = "status")]
        public TStatus Status { get; set; }
    }
}
