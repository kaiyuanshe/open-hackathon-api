﻿using Kaiyuanshe.OpenHackathon.Server.Biz;
using Kaiyuanshe.OpenHackathon.Server.K8S;
using Kaiyuanshe.OpenHackathon.Server.K8S.Models;
using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Storage;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Kaiyuanshe.OpenHackathon.Server.Storage.Tables;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.ServerTests.Biz
{
    class ExperimentManagementTests
    {
        #region CreateTemplateAsync
        [Test]
        public async Task CreateTemplateAsync_Exception()
        {
            Template template = new Template
            {
                hackathonName = "hack",
                name = "default",
                commands = new string[] { "a", "b", "c" },
                environmentVariables = new Dictionary<string, string>
                {
                    { "e1", "v1" },
                    { "e2", "v2" }
                },
                displayName = "dp",
                image = "image",
                ingressPort = 22,
                ingressProtocol = IngressProtocol.vnc,
                vnc = new VncSettings { userName = "un", password = "pw" },
            };
            TemplateEntity entity = new TemplateEntity { PartitionKey = "pk" };

            var logger = new Mock<ILogger<ExperimentManagement>>();

            var templateTable = new Mock<ITemplateTable>();
            templateTable.Setup(p => p.InsertOrReplaceAsync(It.Is<TemplateEntity>(t =>
                t.PartitionKey == "hack" &&
                t.RowKey == "default" &&
                t.Commands.Length == 3 &&
                t.Commands[1] == "b" &&
                t.EnvironmentVariables.Count == 2 &&
                t.EnvironmentVariables.Last().Value == "v2" &&
                t.DisplayName == "dp" &&
                t.Image == "image" &&
                t.IngressPort == 22 &&
                t.IngressProtocol == IngressProtocol.vnc &&
                t.Vnc.userName == "un" &&
                t.Vnc.password == "pw"), default));
            templateTable.Setup(t => t.RetrieveAsync("hack", "default", default)).ReturnsAsync(entity);
            var storageContext = new Mock<IStorageContext>();
            storageContext.SetupGet(p => p.TemplateTable).Returns(templateTable.Object);

            var k8s = new Mock<IKubernetesCluster>();
            k8s.Setup(k => k.CreateOrUpdateTemplateAsync(It.IsAny<TemplateContext>(), default))
                .Throws(new Microsoft.Rest.HttpOperationException("message"));
            var k8sfactory = new Mock<IKubernetesClusterFactory>();
            k8sfactory.Setup(f => f.GetDefaultKubernetes(default)).ReturnsAsync(k8s.Object);

            var management = new ExperimentManagement(logger.Object)
            {
                StorageContext = storageContext.Object,
                KubernetesClusterFactory = k8sfactory.Object,
            };
            var result = await management.CreateTemplateAsync(template, default);

            Mock.VerifyAll(storageContext, templateTable, k8s, k8sfactory);
            storageContext.VerifyAll();
            templateTable.VerifyAll();
            k8s.VerifyNoOtherCalls();
            k8sfactory.VerifyNoOtherCalls();

            Assert.AreEqual("pk", result.TemplateEntity.PartitionKey);
            Assert.AreEqual(500, result.Status.Code);
            Assert.AreEqual("Internal Server Error", result.Status.Reason);
            Assert.AreEqual("message", result.Status.Message);
        }

        [Test]
        public async Task CreateTemplateAsync_Success()
        {
            Template template = new Template
            {
                hackathonName = "hack",
                name = "default",
                commands = new string[] { "a", "b", "c" },
                environmentVariables = new Dictionary<string, string>
                {
                    { "e1", "v1" },
                    { "e2", "v2" }
                },
                displayName = "dp",
                image = "image",
                ingressPort = 22,
                ingressProtocol = IngressProtocol.vnc,
                vnc = new VncSettings { userName = "un", password = "pw" },
            };
            TemplateEntity entity = new TemplateEntity { PartitionKey = "pk" };

            var logger = new Mock<ILogger<ExperimentManagement>>();

            var templateTable = new Mock<ITemplateTable>();
            templateTable.Setup(p => p.InsertOrReplaceAsync(It.Is<TemplateEntity>(t =>
                t.PartitionKey == "hack" &&
                t.RowKey == "default" &&
                t.Commands.Length == 3 &&
                t.Commands[1] == "b" &&
                t.EnvironmentVariables.Count == 2 &&
                t.EnvironmentVariables.Last().Value == "v2" &&
                t.DisplayName == "dp" &&
                t.Image == "image" &&
                t.IngressPort == 22 &&
                t.IngressProtocol == IngressProtocol.vnc &&
                t.Vnc.userName == "un" &&
                t.Vnc.password == "pw"), default));
            templateTable.Setup(t => t.RetrieveAsync("hack", "default", default)).ReturnsAsync(entity);
            var storageContext = new Mock<IStorageContext>();
            storageContext.SetupGet(p => p.TemplateTable).Returns(templateTable.Object);

            var k8s = new Mock<IKubernetesCluster>();
            k8s.Setup(k => k.CreateOrUpdateTemplateAsync(It.IsAny<TemplateContext>(), default));
            var k8sfactory = new Mock<IKubernetesClusterFactory>();
            k8sfactory.Setup(f => f.GetDefaultKubernetes(default)).ReturnsAsync(k8s.Object);

            var management = new ExperimentManagement(logger.Object)
            {
                StorageContext = storageContext.Object,
                KubernetesClusterFactory = k8sfactory.Object,
            };
            var result = await management.CreateTemplateAsync(template, default);

            Mock.VerifyAll(storageContext, templateTable, k8s, k8sfactory);
            storageContext.VerifyAll();
            templateTable.VerifyAll();
            k8s.VerifyNoOtherCalls();
            k8sfactory.VerifyNoOtherCalls();

            Assert.AreEqual("pk", result.TemplateEntity.PartitionKey);
        }
        #endregion

        #region GetTemplateAsync
        [Test]
        public async Task GetTemplateAsync_EntityNotFound()
        {
            TemplateEntity entity = null;

            var templateTable = new Mock<ITemplateTable>();
            templateTable.Setup(e => e.RetrieveAsync("hack", "tn", default)).ReturnsAsync(entity);
            var storageContext = new Mock<IStorageContext>();
            storageContext.SetupGet(s => s.TemplateTable).Returns(templateTable.Object);

            var logger = new Mock<ILogger<ExperimentManagement>>();
            var management = new ExperimentManagement(logger.Object)
            {
                StorageContext = storageContext.Object,
            };

            var result = await management.GetTemplateAsync("hack", "tn", default);

            Mock.VerifyAll(templateTable, storageContext);
            templateTable.VerifyNoOtherCalls();
            storageContext.VerifyNoOtherCalls();

            Assert.IsNull(result);
        }

        [Test]
        public async Task GetTemplateAsync_Exception()
        {
            TemplateEntity entity = new TemplateEntity { };

            var templateTable = new Mock<ITemplateTable>();
            templateTable.Setup(e => e.RetrieveAsync("hack", "tn", default)).ReturnsAsync(entity);
            var storageContext = new Mock<IStorageContext>();
            storageContext.SetupGet(s => s.TemplateTable).Returns(templateTable.Object);

            var k8s = new Mock<IKubernetesCluster>();
            k8s.Setup(k => k.GetTemplateAsync(It.IsAny<TemplateContext>(), default))
                .Throws(new Microsoft.Rest.HttpOperationException("message"));
            var k8sfactory = new Mock<IKubernetesClusterFactory>();
            k8sfactory.Setup(f => f.GetDefaultKubernetes(default)).ReturnsAsync(k8s.Object);

            var logger = new Mock<ILogger<ExperimentManagement>>();
            var management = new ExperimentManagement(logger.Object)
            {
                StorageContext = storageContext.Object,
                KubernetesClusterFactory = k8sfactory.Object,
            };

            var result = await management.GetTemplateAsync("hack", "tn", default);

            Mock.VerifyAll(templateTable, storageContext, k8s, k8sfactory);
            templateTable.VerifyNoOtherCalls();
            storageContext.VerifyNoOtherCalls();
            k8s.VerifyNoOtherCalls();
            k8sfactory.VerifyNoOtherCalls();

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.TemplateEntity);
            Assert.AreEqual(500, result.Status.Code.Value);
            Assert.AreEqual("message", result.Status.Message);
            Assert.AreEqual("Internal Server Error", result.Status.Reason);
        }

        [Test]
        public async Task GetTemplateAsync_Success()
        {
            TemplateEntity entity = new TemplateEntity { };

            var templateTable = new Mock<ITemplateTable>();
            templateTable.Setup(e => e.RetrieveAsync("hack", "tn", default)).ReturnsAsync(entity);
            var storageContext = new Mock<IStorageContext>();
            storageContext.SetupGet(s => s.TemplateTable).Returns(templateTable.Object);

            var k8s = new Mock<IKubernetesCluster>();
            k8s.Setup(k => k.GetTemplateAsync(It.IsAny<TemplateContext>(), default))
                .Callback<TemplateContext, CancellationToken>((c, token) =>
                {
                    c.Status = new k8s.Models.V1Status { Code = 200 };
                });
            var k8sfactory = new Mock<IKubernetesClusterFactory>();
            k8sfactory.Setup(f => f.GetDefaultKubernetes(default)).ReturnsAsync(k8s.Object);

            var logger = new Mock<ILogger<ExperimentManagement>>();
            var management = new ExperimentManagement(logger.Object)
            {
                StorageContext = storageContext.Object,
                KubernetesClusterFactory = k8sfactory.Object,
            };

            var result = await management.GetTemplateAsync("hack", "tn", default);

            Mock.VerifyAll(templateTable, storageContext, k8s, k8sfactory);
            templateTable.VerifyNoOtherCalls();
            storageContext.VerifyNoOtherCalls();
            k8s.VerifyNoOtherCalls();
            k8sfactory.VerifyNoOtherCalls();

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.TemplateEntity);
            Assert.AreEqual(200, result.Status.Code.Value);
        }
        #endregion

        #region CreateExperimentAsync
        [Test]
        public async Task CreateExperimentAsync_Exception()
        {
            var experiment = new Experiment
            {
                hackathonName = "hack",
                templateName = "tn",
                userId = "uid",
            };
            var entity = new ExperimentEntity { PartitionKey = "pk" };

            var experimentTable = new Mock<IExperimentTable>();
            experimentTable.Setup(t => t.InsertOrReplaceAsync(It.Is<ExperimentEntity>(e =>
                e.HackathonName == "hack" &&
                e.RowKey == "c282b009-b95e-b81a-dcf6-fe4d678105f4" &&
                e.Paused == false &&
                e.UserId == "uid" &&
                e.TemplateName == "tn"), default));
            experimentTable.Setup(e => e.RetrieveAsync("hack", "c282b009-b95e-b81a-dcf6-fe4d678105f4", default)).ReturnsAsync(entity);
            var storageContext = new Mock<IStorageContext>();
            storageContext.SetupGet(s => s.ExperimentTable).Returns(experimentTable.Object);

            var k8s = new Mock<IKubernetesCluster>();
            k8s.Setup(k => k.CreateOrUpdateExperimentAsync(It.IsAny<ExperimentContext>(), default))
                .Throws(new Microsoft.Rest.HttpOperationException("message"));
            var k8sfactory = new Mock<IKubernetesClusterFactory>();
            k8sfactory.Setup(f => f.GetDefaultKubernetes(default)).ReturnsAsync(k8s.Object);

            var logger = new Mock<ILogger<ExperimentManagement>>();
            var management = new ExperimentManagement(logger.Object)
            {
                StorageContext = storageContext.Object,
                KubernetesClusterFactory = k8sfactory.Object,
            };

            var result = await management.CreateExperimentAsync(experiment, default);

            Mock.VerifyAll(experimentTable, storageContext, k8s, k8sfactory);
            experimentTable.VerifyNoOtherCalls();
            storageContext.VerifyNoOtherCalls();
            k8s.VerifyNoOtherCalls();
            k8sfactory.VerifyNoOtherCalls();

            Assert.AreEqual("pk", result.ExperimentEntity.PartitionKey);
            Assert.AreEqual(500, result.Status.Code);
            Assert.AreEqual("Internal Server Error", result.Status.Reason);
            Assert.AreEqual("message", result.Status.Message);
        }

        [Test]
        public async Task CreateExperimentAsync_Success()
        {
            var experiment = new Experiment
            {
                hackathonName = "hack",
                templateName = "tn",
                userId = "uid",
            };
            var entity = new ExperimentEntity { PartitionKey = "pk" };

            var experimentTable = new Mock<IExperimentTable>();
            experimentTable.Setup(t => t.InsertOrReplaceAsync(It.Is<ExperimentEntity>(e =>
                e.HackathonName == "hack" &&
                e.RowKey == "c282b009-b95e-b81a-dcf6-fe4d678105f4" &&
                e.Paused == false &&
                e.UserId == "uid" &&
                e.TemplateName == "tn"), default));
            experimentTable.Setup(e => e.RetrieveAsync("hack", "c282b009-b95e-b81a-dcf6-fe4d678105f4", default)).ReturnsAsync(entity);
            var storageContext = new Mock<IStorageContext>();
            storageContext.SetupGet(s => s.ExperimentTable).Returns(experimentTable.Object);

            var k8s = new Mock<IKubernetesCluster>();
            k8s.Setup(k => k.CreateOrUpdateExperimentAsync(It.IsAny<ExperimentContext>(), default));
            var k8sfactory = new Mock<IKubernetesClusterFactory>();
            k8sfactory.Setup(f => f.GetDefaultKubernetes(default)).ReturnsAsync(k8s.Object);

            var logger = new Mock<ILogger<ExperimentManagement>>();
            var management = new ExperimentManagement(logger.Object)
            {
                StorageContext = storageContext.Object,
                KubernetesClusterFactory = k8sfactory.Object,
            };

            var result = await management.CreateExperimentAsync(experiment, default);

            Mock.VerifyAll(experimentTable, storageContext, k8s, k8sfactory);
            experimentTable.VerifyNoOtherCalls();
            storageContext.VerifyNoOtherCalls();
            k8s.VerifyNoOtherCalls();
            k8sfactory.VerifyNoOtherCalls();

            Assert.AreEqual("pk", result.ExperimentEntity.PartitionKey);
        }
        #endregion

        #region GetExperimentAsync
        [Test]
        public async Task GetExperimentAsync_EntityNotFound()
        {
            ExperimentEntity entity = null;

            var experimentTable = new Mock<IExperimentTable>();
            experimentTable.Setup(e => e.RetrieveAsync("hack", "expId", default)).ReturnsAsync(entity);
            var storageContext = new Mock<IStorageContext>();
            storageContext.SetupGet(s => s.ExperimentTable).Returns(experimentTable.Object);

            var logger = new Mock<ILogger<ExperimentManagement>>();
            var management = new ExperimentManagement(logger.Object)
            {
                StorageContext = storageContext.Object,
            };

            var result = await management.GetExperimentAsync("hack", "expId", default);

            Mock.VerifyAll(experimentTable, storageContext);
            experimentTable.VerifyNoOtherCalls();
            storageContext.VerifyNoOtherCalls();

            Assert.IsNull(result);
        }

        [Test]
        public async Task GetExperimentAsync_Exception()
        {
            ExperimentEntity entity = new ExperimentEntity { };

            var experimentTable = new Mock<IExperimentTable>();
            experimentTable.Setup(e => e.RetrieveAsync("hack", "expId", default)).ReturnsAsync(entity);
            var storageContext = new Mock<IStorageContext>();
            storageContext.SetupGet(s => s.ExperimentTable).Returns(experimentTable.Object);

            var k8s = new Mock<IKubernetesCluster>();
            k8s.Setup(k => k.GetExperimentAsync(It.IsAny<ExperimentContext>(), default))
                .Throws(new Microsoft.Rest.HttpOperationException("message"));
            var k8sfactory = new Mock<IKubernetesClusterFactory>();
            k8sfactory.Setup(f => f.GetDefaultKubernetes(default)).ReturnsAsync(k8s.Object);

            var logger = new Mock<ILogger<ExperimentManagement>>();
            var management = new ExperimentManagement(logger.Object)
            {
                StorageContext = storageContext.Object,
                KubernetesClusterFactory = k8sfactory.Object,
            };

            var result = await management.GetExperimentAsync("hack", "expId", default);

            Mock.VerifyAll(experimentTable, storageContext, k8s, k8sfactory);
            experimentTable.VerifyNoOtherCalls();
            storageContext.VerifyNoOtherCalls();
            k8s.VerifyNoOtherCalls();
            k8sfactory.VerifyNoOtherCalls();

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.ExperimentEntity);
            Assert.AreEqual(500, result.Status.Code.Value);
            Assert.AreEqual("message", result.Status.Message);
            Assert.AreEqual("Internal Server Error", result.Status.Reason);
        }

        [Test]
        public async Task GetExperimentAsync_Success()
        {
            ExperimentEntity entity = new ExperimentEntity { };

            var experimentTable = new Mock<IExperimentTable>();
            experimentTable.Setup(e => e.RetrieveAsync("hack", "expId", default)).ReturnsAsync(entity);
            var storageContext = new Mock<IStorageContext>();
            storageContext.SetupGet(s => s.ExperimentTable).Returns(experimentTable.Object);

            var k8s = new Mock<IKubernetesCluster>();
            k8s.Setup(k => k.GetExperimentAsync(It.IsAny<ExperimentContext>(), default))
                .Callback<ExperimentContext, CancellationToken>((c, token) =>
                {
                    c.Status = new ExperimentStatus { Code = 200 };
                });
            var k8sfactory = new Mock<IKubernetesClusterFactory>();
            k8sfactory.Setup(f => f.GetDefaultKubernetes(default)).ReturnsAsync(k8s.Object);

            var logger = new Mock<ILogger<ExperimentManagement>>();
            var management = new ExperimentManagement(logger.Object)
            {
                StorageContext = storageContext.Object,
                KubernetesClusterFactory = k8sfactory.Object,
            };

            var result = await management.GetExperimentAsync("hack", "expId", default);

            Mock.VerifyAll(experimentTable, storageContext, k8s, k8sfactory);
            experimentTable.VerifyNoOtherCalls();
            storageContext.VerifyNoOtherCalls();
            k8s.VerifyNoOtherCalls();
            k8sfactory.VerifyNoOtherCalls();

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.ExperimentEntity);
            Assert.AreEqual(200, result.Status.Code.Value);
        }
        #endregion
    }
}