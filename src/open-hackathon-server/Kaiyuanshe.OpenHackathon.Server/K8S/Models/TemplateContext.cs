using k8s.Models;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using System.Collections.Generic;

namespace Kaiyuanshe.OpenHackathon.Server.K8S.Models
{
    public class TemplateContext
    {
        public TemplateEntity TemplateEntity { get; set; }
        public V1Status Status { get; set; }

        public string GetTemplateResourceName()
        {
            return TemplateEntity.Id;
        }

        public IDictionary<string, string> BuildEnvironmentVariables()
        {
            var env = TemplateEntity.EnvironmentVariables == null ?
                new Dictionary<string, string>() :
                new Dictionary<string, string>(TemplateEntity.EnvironmentVariables);

            if (TemplateEntity.Vnc?.userName != null)
            {
                env.Add(EnvNames.User, TemplateEntity.Vnc.userName);
            }
            return env;
        }

        public TemplateResource BuildCustomResource()
        {
            var tr = new TemplateResource
            {
                ApiVersion = CustomResourceDefinition.ApiVersion,
                Kind = Kinds.Template,
                Metadata = new V1ObjectMeta
                {
                    Name = GetTemplateResourceName(),
                    NamespaceProperty = Namespaces.Default,
                    Labels = new Dictionary<string, string>
                    {
                        { Labels.HackathonName, TemplateEntity.HackathonName },
                        { Labels.TemplateId, TemplateEntity.Id },
                    },
                },
                data = new TemplateData
                {
                    ingressPort = TemplateEntity.IngressPort,
                    ingressProtocol = TemplateEntity.IngressProtocol.ToString(),
                    type = "Pod",
                    podTemplate = new PodTemplate
                    {
                        image = TemplateEntity.Image,
                        env = BuildEnvironmentVariables(),
                        command = TemplateEntity.Commands,
                    },
                    vnc = TemplateEntity.Vnc == null ? null : new Vnc
                    {
                        username = TemplateEntity.Vnc.userName,
                        password = TemplateEntity.Vnc.password,
                    },
                },
            };

            return tr;
        }

        public V1Patch BuildPatch()
        {
            var resource = BuildCustomResource();
            var patch = new V1Patch(resource, V1Patch.PatchType.MergePatch);
            return patch;
        }
    }
}
