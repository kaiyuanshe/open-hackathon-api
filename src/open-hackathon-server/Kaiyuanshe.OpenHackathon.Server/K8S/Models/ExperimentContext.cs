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
                ApiVersion = CustomResourceDefinition.ApiVersion,
                Kind = Kinds.Experiment,
                Metadata = new V1ObjectMeta
                {
                    Name = GetExperimentResourceName(),
                    NamespaceProperty = Namespaces.Default,
                    Labels = new Dictionary<string, string>
                    {
                        { Labels.HackathonName, ExperimentEntity.HackathonName },
                        { Labels.UserId, ExperimentEntity.UserId },
                        { Labels.TemplateId, ExperimentEntity.TemplateId },
                    },
                },
                Spec = new ExperimentSpec
                {
                    custerName = "meta-cluster",
                    template = GetTemplateResourceName(),
                    pause = ExperimentEntity.Paused,
                },
            };
        }

        public string GetExperimentResourceName()
        {
            return $"{ExperimentEntity.HackathonName}-{ExperimentEntity.TemplateId}-{ExperimentEntity.UserId}";
        }

        public string GetTemplateResourceName()
        {
            return $"{ExperimentEntity.HackathonName}-{ExperimentEntity.TemplateId}";
        }
    }
}
