using k8s.Models;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using System.Collections.Generic;

namespace Kaiyuanshe.OpenHackathon.Server.K8S.Models
{
    public class ExperimentContext
    {
        public ExperimentEntity ExperimentEntity { get; set; }
        public ExperimentStatus Status { get; set; }

        public ExperimentResource BuildCustomResource()
        {
            return new ExperimentResource
            {
                ApiVersion = "hackathon.kaiyuanshe.cn/v1",
                Kind = "Experiment",
                Metadata = new V1ObjectMeta
                {
                    Name = GetExperimentResourceName(),
                    NamespaceProperty = GetNamespace(),
                    Labels = new Dictionary<string, string>
                    {
                        { "hackathonName", ExperimentEntity.HackathonName },
                        { "userId", ExperimentEntity.UserId },
                        { "templateName", ExperimentEntity.TemplateName },
                    },
                },
                Spec = new ExperimentSpec
                {
                    ClusterName = "meta-cluster",
                    Template = GetTemplateResourceName(),
                    Pause = ExperimentEntity.Paused,
                },
            };
        }

        public string GetExperimentResourceName()
        {
            return $"{ExperimentEntity.TemplateName}-{ExperimentEntity.UserId}";
        }

        public string GetTemplateResourceName()
        {
            return $"{ExperimentEntity.HackathonName}-{ExperimentEntity.TemplateName}";
        }

        public string GetNamespace()
        {
            return "default";
        }
    }
}
