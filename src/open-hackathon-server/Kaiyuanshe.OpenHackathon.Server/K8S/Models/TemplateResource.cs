using Newtonsoft.Json;
using System.Collections.Generic;

namespace Kaiyuanshe.OpenHackathon.Server.K8S.Models
{
    /// <summary>
    /// Template resource definition.
    /// sample yaml: https://github.com/kaiyuanshe/cloudengine/blob/master/config/samples/hackathon_v1_template.yaml
    /// </summary>
    public class TemplateResource : CustomResource
    {
        // Properties of CustomResourceDefinition
        // See also: https://github.com/kaiyuanshe/cloudengine/blob/master/config/crd/bases/hackathon.kaiyuanshe.cn_templates.yaml
        public static readonly string Plural = "templates";

        public TemplateData data { get; set; }
    }

    public class TemplateData
    {
        public string type { get; set; }

        public PodTemplate podTemplate { get; set; }

        public string ingressProtocol { get; set; }

        public int ingressPort { get; set; }

        public Vnc vnc { get; set; }
    }

    public class PodTemplate
    {
        public string image { get; set; }

        public string[] command { get; set; }

        public IDictionary<string, string> env { get; set; }
    }

    public class Vnc
    {
        [JsonProperty(PropertyName = "username")]
        public string username { get; set; }

        [JsonProperty(PropertyName = "password")]
        public string password { get; set; }
    }
}
