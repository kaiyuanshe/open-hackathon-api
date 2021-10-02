using Kaiyuanshe.OpenHackathon.Server.K8S.Models;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using NUnit.Framework;

namespace Kaiyuanshe.OpenHackathon.ServerTests.K8S
{
    class ExperimentContextTests
    {
        [Test]
        public void BuildCustomResource()
        {
            var context = new ExperimentContext
            {
                ExperimentEntity = new ExperimentEntity
                {
                    PartitionKey = "pk",
                    RowKey = "rk",
                    Paused = true,
                    TemplateName = "tpl",
                    UserId = "uid"
                }
            };

            var cr = context.BuildCustomResource();
            Assert.AreEqual("hackathon.kaiyuanshe.cn/v1", cr.ApiVersion);
            Assert.AreEqual("Experiment", cr.Kind);
            Assert.AreEqual("pk-tpl-uid", cr.Metadata.Name);
            Assert.AreEqual("default", cr.Metadata.NamespaceProperty);
            Assert.AreEqual(3, cr.Metadata.Labels.Count);
            Assert.AreEqual("pk", cr.Metadata.Labels["hackathonName"]);
            Assert.AreEqual("uid", cr.Metadata.Labels["userId"]);
            Assert.AreEqual("tpl", cr.Metadata.Labels["templateName"]);
            Assert.AreEqual("meta-cluster", cr.Spec.ClusterName);
            Assert.AreEqual("pk-tpl", cr.Spec.Template);
            Assert.AreEqual(true, cr.Spec.Pause);
        }
    }
}
