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
                    template = ExperimentEntity.TemplateId,
                    pause = ExperimentEntity.Paused,
                },
            };
        }

        public V1Patch BuildPatch()
        {
            var resource = BuildCustomResource();
            var patch = new V1Patch(resource, V1Patch.PatchType.MergePatch);
            return patch;
        }

        public string GetExperimentResourceName()
        {
            return ExperimentEntity.Id;
        }
    }
}
