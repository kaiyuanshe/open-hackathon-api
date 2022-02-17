using Kaiyuanshe.OpenHackathon.Server.K8S.Models;
using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using NUnit.Framework;
using System.Collections.Generic;

namespace Kaiyuanshe.OpenHackathon.ServerTests.K8S
{
    class TemplateContextTests
    {
        [Test]
        public void BuildCustomResource()
        {
            var context = new TemplateContext
            {
                TemplateEntity = new TemplateEntity
                {
                    PartitionKey = "pk",
                    RowKey = "rk",
                    IngressPort = 5901,
                    IngressProtocol = Server.Models.IngressProtocol.vnc,
                    Image = "image",
                    EnvironmentVariables = new Dictionary<string, string>
                    {
                        { "key", "value" }
                    },
                    Commands = new string[] { "a" },
                    Vnc = new VncSettings
                    {
                        userName = "un",
                        password = "pwd"
                    },
                }
            };

            var cr = context.BuildCustomResource();
            Assert.AreEqual("hackathon.kaiyuanshe.cn/v1", cr.ApiVersion);
            Assert.AreEqual("Template", cr.Kind);
            Assert.AreEqual("pk-rk", cr.Metadata.Name);
            Assert.AreEqual("default", cr.Metadata.NamespaceProperty);
            Assert.AreEqual(2, cr.Metadata.Labels.Count);
            Assert.AreEqual("pk", cr.Metadata.Labels["hackathonName"]);
            Assert.AreEqual("rk", cr.Metadata.Labels["templateName"]);
            Assert.AreEqual(5901, cr.data.ingressPort);
            Assert.AreEqual("vnc", cr.data.ingressProtocol);
            Assert.AreEqual("Pod", cr.data.type);
            Assert.AreEqual("image", cr.data.podTemplate.image);
            Assert.AreEqual("a", cr.data.podTemplate.command[0]);
            Assert.AreEqual("value", cr.data.podTemplate.env["key"]);
            Assert.AreEqual("un", cr.data.podTemplate.env["USER"]);
            Assert.AreEqual("un", cr.data.vnc.username);
            Assert.AreEqual("pwd", cr.data.vnc.password);
        }
    }
}
