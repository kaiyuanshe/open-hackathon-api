﻿using k8s.Autorest;
using k8s.Models;
using Kaiyuanshe.OpenHackathon.Server.Biz;
using Kaiyuanshe.OpenHackathon.Server.K8S;
using Kaiyuanshe.OpenHackathon.Server.K8S.Models;
using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Storage;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Kaiyuanshe.OpenHackathon.Server.Storage.Tables;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.ServerTests.Biz
{
    class ExperimentManagementTests
    {
        #region CreateOrUpdateTemplateAsync
        [Test]
        public async Task CreateOrUpdateTemplateAsync_K8SException()
        {
            Template template = new Template
            {
                hackathonName = "hack",
                id = "any",
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

            var templateTable = new Mock<ITemplateTable>();
            templateTable.Setup(p => p.RetrieveAsync("hack", "any", default)).ReturnsAsync(default(TemplateEntity));
            templateTable.Setup(p => p.InsertAsync(It.Is<TemplateEntity>(t =>
                t.PartitionKey == "hack" &&
                t.RowKey == "any" &&
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
            var storageContext = new Mock<IStorageContext>();
            storageContext.SetupGet(p => p.TemplateTable).Returns(templateTable.Object);

            var k8s = new Mock<IKubernetesCluster>();
            k8s.Setup(k => k.CreateOrUpdateTemplateAsync(It.IsAny<TemplateContext>(), default))
                .Throws(new HttpOperationException("message"));
            var k8sfactory = new Mock<IKubernetesClusterFactory>();
            k8sfactory.Setup(f => f.GetDefaultKubernetes(default)).ReturnsAsync(k8s.Object);

            var management = new ExperimentManagement()
            {
                StorageContext = storageContext.Object,
                KubernetesClusterFactory = k8sfactory.Object,
            };
            var result = await management.CreateOrUpdateTemplateAsync(template, default);

            Mock.VerifyAll(storageContext, templateTable, k8s, k8sfactory);
            storageContext.VerifyAll();
            templateTable.VerifyAll();
            k8s.VerifyNoOtherCalls();
            k8sfactory.VerifyNoOtherCalls();

            Assert.AreEqual("hack", result.TemplateEntity.PartitionKey);
            Assert.AreEqual(500, result.Status.Code);
            Assert.AreEqual("Internal Server Error", result.Status.Reason);
            Assert.AreEqual("message", result.Status.Message);
        }

        [Test]
        public async Task CreateOrUpdateTemplateAsync_CreateSuccess()
        {
            Template template = new Template
            {
                hackathonName = "hack",
                id = "any",
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

            var templateTable = new Mock<ITemplateTable>();
            templateTable.Setup(p => p.RetrieveAsync("hack", "any", default)).ReturnsAsync(default(TemplateEntity));
            templateTable.Setup(p => p.InsertAsync(It.Is<TemplateEntity>(t =>
                t.PartitionKey == "hack" &&
                t.RowKey == "any" &&
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
            var storageContext = new Mock<IStorageContext>();
            storageContext.SetupGet(p => p.TemplateTable).Returns(templateTable.Object);

            var k8s = new Mock<IKubernetesCluster>();
            k8s.Setup(k => k.CreateOrUpdateTemplateAsync(It.IsAny<TemplateContext>(), default));
            var k8sfactory = new Mock<IKubernetesClusterFactory>();
            k8sfactory.Setup(f => f.GetDefaultKubernetes(default)).ReturnsAsync(k8s.Object);

            var management = new ExperimentManagement()
            {
                StorageContext = storageContext.Object,
                KubernetesClusterFactory = k8sfactory.Object,
            };
            var result = await management.CreateOrUpdateTemplateAsync(template, default);

            Mock.VerifyAll(storageContext, templateTable, k8s, k8sfactory);
            storageContext.VerifyAll();
            templateTable.VerifyAll();
            k8s.VerifyNoOtherCalls();
            k8sfactory.VerifyNoOtherCalls();

            Assert.AreEqual("hack", result.TemplateEntity.PartitionKey);
        }

        [Test]
        public async Task CreateOrUpdateTemplateAsync_UpdateSuccess()
        {
            Template template = new Template
            {
                hackathonName = "hack",
                id = "any",
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
            var entity = new TemplateEntity
            {
                PartitionKey = "hack",
                RowKey = "any",
                Commands = new string[] { "old" },
                EnvironmentVariables = new Dictionary<string, string>
                {
                    { "o1", "o1" },
                },
                DisplayName = "oldDn",
                Image = "oldImage",
                IngressPort = 2222,
                IngressProtocol = IngressProtocol.rdp,
                Vnc = new VncSettings { userName = "oldUn", password = "oldPwd" },
            };


            var templateTable = new Mock<ITemplateTable>();
            templateTable.Setup(p => p.RetrieveAsync("hack", "any", default)).ReturnsAsync(entity);
            templateTable.Setup(p => p.MergeAsync(It.Is<TemplateEntity>(t =>
                t.PartitionKey == "hack" &&
                t.RowKey == "any" &&
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
            var storageContext = new Mock<IStorageContext>();
            storageContext.SetupGet(p => p.TemplateTable).Returns(templateTable.Object);

            var k8s = new Mock<IKubernetesCluster>();
            k8s.Setup(k => k.CreateOrUpdateTemplateAsync(It.IsAny<TemplateContext>(), default));
            var k8sfactory = new Mock<IKubernetesClusterFactory>();
            k8sfactory.Setup(f => f.GetDefaultKubernetes(default)).ReturnsAsync(k8s.Object);

            var management = new ExperimentManagement()
            {
                StorageContext = storageContext.Object,
                KubernetesClusterFactory = k8sfactory.Object,
            };
            var result = await management.CreateOrUpdateTemplateAsync(template, default);

            Mock.VerifyAll(storageContext, templateTable, k8s, k8sfactory);
            storageContext.VerifyAll();
            templateTable.VerifyAll();
            k8s.VerifyNoOtherCalls();
            k8sfactory.VerifyNoOtherCalls();

            Assert.AreEqual("hack", result.TemplateEntity.PartitionKey);
        }
        #endregion

        #region GetTemplateAsync
        [Test]
        public async Task GetTemplateAsync_EntityNotFound()
        {
            TemplateEntity? entity = null;

            var moqs = new Moqs();
            moqs.TemplateTable.Setup(e => e.RetrieveAsync("hack", "tn", default)).ReturnsAsync(entity);

            var management = new ExperimentManagement();
            moqs.SetupManagement(management);
            var result = await management.GetTemplateAsync("hack", "tn", default);

            moqs.VerifyAll();
            Assert.IsNull(result);
        }

        [Test]
        public async Task GetTemplateAsync_Exception()
        {
            TemplateEntity entity = new TemplateEntity { };

            var moqs = new Moqs();
            moqs.TemplateTable.Setup(e => e.RetrieveAsync("hack", "tn", default)).ReturnsAsync(entity);
            moqs.KubernetesCluster.Setup(k => k.GetTemplateAsync(It.IsAny<TemplateContext>(), default))
                .Throws(new HttpOperationException("message"));

            var management = new ExperimentManagement()
            {
                KubernetesClusterFactory = moqs.KubernetesClusterFactory.Object,
            };
            moqs.SetupManagement(management);
            var result = await management.GetTemplateAsync("hack", "tn", default);

            moqs.VerifyAll();
            Debug.Assert(result != null);
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.TemplateEntity);
            Assert.AreEqual(500, result.Status.Code);
            Assert.AreEqual("message", result.Status.Message);
            Assert.AreEqual("Internal Server Error", result.Status.Reason);
        }

        [Test]
        public async Task GetTemplateAsync_Success()
        {
            TemplateEntity entity = new TemplateEntity { };

            var moqs = new Moqs();
            moqs.TemplateTable.Setup(e => e.RetrieveAsync("hack", "tn", default)).ReturnsAsync(entity);

            moqs.KubernetesCluster.Setup(k => k.GetTemplateAsync(It.IsAny<TemplateContext>(), default))
                .Callback<TemplateContext, CancellationToken>((c, token) =>
                {
                    c.Status = new V1Status { Code = 200 };
                });

            var management = new ExperimentManagement()
            {
                KubernetesClusterFactory = moqs.KubernetesClusterFactory.Object,
            };
            moqs.SetupManagement(management);
            var result = await management.GetTemplateAsync("hack", "tn", default);

            moqs.VerifyAll();
            Debug.Assert(result != null);
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.TemplateEntity);
            Assert.AreEqual(200, result.Status.Code);
        }
        #endregion

        #region ListTemplatesAsync
        [Test]
        public async Task ListTemplatesAsync()
        {
            // data
            var entities = new List<TemplateEntity>
            {
                new TemplateEntity{ RowKey="t1" },
                new TemplateEntity{ RowKey="t2" },
            };
            var k8sResources = new List<TemplateResource>
            {
                new TemplateResource(), // empty, will be ignored
                new TemplateResource
                {
                    Metadata = new k8s.Models.V1ObjectMeta
                    {
                        Labels = new Dictionary<string, string>
                        {
                            { Labels.TemplateId, "other" } // not match
                        }
                    }
                },
                new TemplateResource
                {
                    Metadata = new k8s.Models.V1ObjectMeta
                    {
                        Labels = new Dictionary<string, string>
                        {
                            { Labels.TemplateId, "t1" }
                        }
                    }
                },
            };

            // mock
            var templateTable = new Mock<ITemplateTable>();
            templateTable.Setup(p => p.QueryEntitiesAsync("PartitionKey eq 'hack'", null, default)).ReturnsAsync(entities);
            var storageContext = new Mock<IStorageContext>();
            storageContext.SetupGet(p => p.TemplateTable).Returns(templateTable.Object);

            var k8s = new Mock<IKubernetesCluster>();
            k8s.Setup(k => k.ListTemplatesAsync("hack", default)).ReturnsAsync(k8sResources);
            var k8sFactory = new Mock<IKubernetesClusterFactory>();
            k8sFactory.Setup(p => p.GetDefaultKubernetes(default)).ReturnsAsync(k8s.Object);

            // test
            var management = new ExperimentManagement()
            {
                KubernetesClusterFactory = k8sFactory.Object,
                StorageContext = storageContext.Object,
            };
            var result = await management.ListTemplatesAsync("hack", default);

            // verify
            Mock.VerifyAll(templateTable, storageContext, k8s, k8sFactory);
            templateTable.VerifyNoOtherCalls();
            storageContext.VerifyNoOtherCalls();
            k8s.VerifyNoOtherCalls();
            k8sFactory.VerifyNoOtherCalls();

            Assert.AreEqual(2, result.Count());
            Assert.AreEqual("t1", result.First().TemplateEntity.Id);
            Assert.AreEqual(200, result.First().Status.Code);
            Assert.AreEqual("success", result.First().Status.Status);
            Assert.AreEqual("Ok", result.First().Status.Reason);
            Assert.AreEqual("t2", result.Last().TemplateEntity.Id);
            Assert.AreEqual(422, result.Last().Status.Code);
            Assert.AreEqual("failure", result.Last().Status.Status);
            Assert.AreEqual("UnprocessableEntity", result.Last().Status.Reason);
        }
        #endregion

        #region GetTemplateCountAsync
        [Test]
        public async Task GetTemplateCountAsync()
        {
            // data
            var entities = new List<TemplateEntity>
            {
                new TemplateEntity(),
                new TemplateEntity(),
                new TemplateEntity(),
            };

            // mock
            var templateTable = new Mock<ITemplateTable>();
            templateTable.Setup(p => p.QueryEntitiesAsync("PartitionKey eq 'hack'", new string[] { "RowKey" }, default)).ReturnsAsync(entities);
            var storageContext = new Mock<IStorageContext>();
            storageContext.SetupGet(p => p.TemplateTable).Returns(templateTable.Object);

            // test
            var management = new ExperimentManagement()
            {
                StorageContext = storageContext.Object,
            };
            var result = await management.GetTemplateCountAsync("hack", default);

            // verify
            Mock.VerifyAll(templateTable, storageContext);
            templateTable.VerifyNoOtherCalls();
            storageContext.VerifyNoOtherCalls();
            Assert.AreEqual(3, result);
        }
        #endregion

        #region DeleteTemplateAsync
        [Test]
        public async Task DeleteTemplateAsync_EntityNotFound()
        {
            TemplateEntity? entity = null;

            // mock
            var moqs = new Moqs();
            moqs.TemplateTable.Setup(p => p.RetrieveAsync("hack", "tpl", default)).ReturnsAsync(entity);

            // test
            var management = new ExperimentManagement();
            moqs.SetupManagement(management);
            var result = await management.DeleteTemplateAsync("hack", "tpl", default);

            // verify
            moqs.VerifyAll();
            moqs.KubernetesCluster.VerifyNoOtherCalls();
            moqs.KubernetesClusterFactory.VerifyNoOtherCalls();
            Assert.IsNull(result);
        }

        [TestCase(204)]
        [TestCase(409)]
        public async Task DeleteTemplateAsync_Deleted(int statusCode)
        {
            TemplateEntity entity = new TemplateEntity();

            // mock
            var moqs = new Moqs();
            moqs.TemplateTable.Setup(p => p.RetrieveAsync("hack", "tpl", default)).ReturnsAsync(entity);
            moqs.TemplateTable.Setup(p => p.DeleteAsync("hack", "tpl", default));
            moqs.KubernetesCluster.Setup(k => k.DeleteTemplateAsync(It.IsAny<TemplateContext>(), default))
                .Callback<TemplateContext, CancellationToken>((c, ct) =>
                {
                    c.Status = new k8s.Models.V1Status { Code = statusCode };
                });

            // test
            var management = new ExperimentManagement()
            {
                KubernetesClusterFactory = moqs.KubernetesClusterFactory.Object,
            };
            moqs.SetupManagement(management);
            var result = await management.DeleteTemplateAsync("hack", "tpl", default);

            // verify
            moqs.VerifyAll();

            Debug.Assert(result != null);
            Assert.IsNotNull(result.TemplateEntity);
            Assert.AreEqual(statusCode, result.Status.Code);
        }
        #endregion

        #region CleanupKubernetesTemplatesAsync
        [Test]
        public async Task CleanupKubernetesTemplatesAsync()
        {
            var resources = new List<TemplateResource>
            {
                new TemplateResource { Metadata = new V1ObjectMeta { Name = "t1" } },
                new TemplateResource { Metadata = new V1ObjectMeta { Name = "t2" } },
            };

            // mock
            var k8s = new Mock<IKubernetesCluster>();
            k8s.Setup(k => k.ListTemplatesAsync("hack", default)).ReturnsAsync(resources);
            k8s.Setup(k => k.DeleteTemplateAsync("t1", default));
            k8s.Setup(k => k.DeleteTemplateAsync("t2", default));
            var k8sfactory = new Mock<IKubernetesClusterFactory>();
            k8sfactory.Setup(f => f.GetDefaultKubernetes(default)).ReturnsAsync(k8s.Object);

            // test
            var management = new ExperimentManagement()
            {
                KubernetesClusterFactory = k8sfactory.Object,
            };
            await management.CleanupKubernetesTemplatesAsync("hack", default);

            // verify
            Mock.VerifyAll(k8s, k8sfactory);
            k8s.VerifyNoOtherCalls();
            k8sfactory.VerifyNoOtherCalls();
        }
        #endregion

        #region CreateOrUpdateExperimentAsync
        [Test]
        public async Task CreateOrUpdateExperimentAsync_Exception()
        {
            var experiment = new Experiment
            {
                hackathonName = "hack",
                templateId = "tn",
                userId = "uid",
            };
            var entity = new ExperimentEntity { PartitionKey = "pk" };

            var experimentTable = new Mock<IExperimentTable>();
            experimentTable.Setup(e => e.RetrieveAsync("hack", "aaaec1e9-68c8-5eb1-51e4-1131794444ae", default)).ReturnsAsync(entity);
            var storageContext = new Mock<IStorageContext>();
            storageContext.SetupGet(s => s.ExperimentTable).Returns(experimentTable.Object);

            var k8s = new Mock<IKubernetesCluster>();
            k8s.Setup(k => k.CreateOrUpdateExperimentAsync(It.IsAny<ExperimentContext>(), default))
                .Throws(new HttpOperationException("message"));
            var k8sfactory = new Mock<IKubernetesClusterFactory>();
            k8sfactory.Setup(f => f.GetDefaultKubernetes(default)).ReturnsAsync(k8s.Object);


            var management = new ExperimentManagement()
            {
                StorageContext = storageContext.Object,
                KubernetesClusterFactory = k8sfactory.Object,
            };

            var result = await management.CreateOrUpdateExperimentAsync(experiment, default);

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
        public async Task CreateOrUpdateExperimentAsync_Create()
        {
            var experiment = new Experiment
            {
                hackathonName = "hack",
                templateId = "tn",
                userId = "uid",
            };
            ExperimentEntity? entity = null;

            var moqs = new Moqs();
            moqs.ExperimentTable.Setup(t => t.InsertAsync(It.Is<ExperimentEntity>(e =>
                e.HackathonName == "hack" &&
                e.RowKey == "aaaec1e9-68c8-5eb1-51e4-1131794444ae" &&
                e.Paused == false &&
                e.UserId == "uid" &&
                e.TemplateId == "tn"), default));
            moqs.ExperimentTable.Setup(e => e.RetrieveAsync("hack", "aaaec1e9-68c8-5eb1-51e4-1131794444ae", default)).ReturnsAsync(entity);

            moqs.KubernetesCluster.Setup(k => k.CreateOrUpdateExperimentAsync(It.IsAny<ExperimentContext>(), default));

            var management = new ExperimentManagement()
            {
                KubernetesClusterFactory = moqs.KubernetesClusterFactory.Object,
            };
            moqs.SetupManagement(management);
            var result = await management.CreateOrUpdateExperimentAsync(experiment, default);

            moqs.VerifyAll();
            Assert.AreEqual("hack", result.ExperimentEntity.PartitionKey);
        }

        [Test]
        public async Task CreateOrUpdateExperimentAsync_Update()
        {
            var experiment = new Experiment
            {
                hackathonName = "hack",
                templateId = "tn",
                userId = "uid",
            };
            ExperimentEntity entity = new ExperimentEntity { PartitionKey = "pk" };

            var experimentTable = new Mock<IExperimentTable>();
            experimentTable.Setup(e => e.RetrieveAsync("hack", "aaaec1e9-68c8-5eb1-51e4-1131794444ae", default)).ReturnsAsync(entity);
            var storageContext = new Mock<IStorageContext>();
            storageContext.SetupGet(s => s.ExperimentTable).Returns(experimentTable.Object);

            var k8s = new Mock<IKubernetesCluster>();
            k8s.Setup(k => k.CreateOrUpdateExperimentAsync(It.IsAny<ExperimentContext>(), default));
            var k8sfactory = new Mock<IKubernetesClusterFactory>();
            k8sfactory.Setup(f => f.GetDefaultKubernetes(default)).ReturnsAsync(k8s.Object);


            var management = new ExperimentManagement()
            {
                StorageContext = storageContext.Object,
                KubernetesClusterFactory = k8sfactory.Object,
            };

            var result = await management.CreateOrUpdateExperimentAsync(experiment, default);

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
            ExperimentEntity? entity = null;

            var moqs = new Moqs();
            moqs.ExperimentTable.Setup(e => e.RetrieveAsync("hack", "expId", default)).ReturnsAsync(entity);

            var management = new ExperimentManagement();
            moqs.SetupManagement(management);
            var result = await management.GetExperimentAsync("hack", "expId", default);

            moqs.VerifyAll();
            Assert.IsNull(result);
        }

        [Test]
        public async Task GetExperimentAsync_Exception()
        {
            ExperimentEntity entity = new ExperimentEntity { };

            var moqs = new Moqs();
            moqs.ExperimentTable.Setup(e => e.RetrieveAsync("hack", "expId", default)).ReturnsAsync(entity);

            moqs.KubernetesCluster.Setup(k => k.GetExperimentAsync(It.IsAny<ExperimentContext>(), default))
                .Throws(new HttpOperationException("message"));

            var management = new ExperimentManagement()
            {
                StorageContext = moqs.StorageContext.Object,
                KubernetesClusterFactory = moqs.KubernetesClusterFactory.Object,
            };
            var result = await management.GetExperimentAsync("hack", "expId", default);

            moqs.VerifyAll();
            Debug.Assert(result != null);
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.ExperimentEntity);
            Assert.AreEqual(500, result.Status.Code);
            Assert.AreEqual("message", result.Status.Message);
            Assert.AreEqual("Internal Server Error", result.Status.Reason);
        }

        [Test]
        public async Task GetExperimentAsync_Success()
        {
            ExperimentEntity entity = new ExperimentEntity { };

            var moqs = new Moqs();
            moqs.ExperimentTable.Setup(e => e.RetrieveAsync("hack", "expId", default)).ReturnsAsync(entity);
            moqs.KubernetesCluster.Setup(k => k.GetExperimentAsync(It.IsAny<ExperimentContext>(), default))
                .Callback<ExperimentContext, CancellationToken>((c, token) =>
                {
                    c.Status = new ExperimentStatus { Code = 200 };
                });

            var management = new ExperimentManagement()
            {
                StorageContext = moqs.StorageContext.Object,
                KubernetesClusterFactory = moqs.KubernetesClusterFactory.Object,
            };

            var result = await management.GetExperimentAsync("hack", "expId", default);

            moqs.VerifyAll();
            Debug.Assert(result != null);
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.ExperimentEntity);
            Assert.AreEqual(200, result.Status.Code);
        }
        #endregion

        #region ResetExperimentAsync
        [Test]
        public async Task ResetExperimentAsync_EntityNotFound()
        {
            ExperimentEntity? entity = null;

            // mock
            var moqs = new Moqs();
            moqs.ExperimentTable.Setup(p => p.RetrieveAsync("hack", "expr", default)).ReturnsAsync(entity);
            var k8s = new Mock<IKubernetesCluster>();
            var k8sfactory = new Mock<IKubernetesClusterFactory>();

            // test
            var management = new ExperimentManagement()
            {
                StorageContext = moqs.StorageContext.Object,
                KubernetesClusterFactory = k8sfactory.Object,
            };
            var result = await management.ResetExperimentAsync("hack", "expr", default);

            // verify
            moqs.VerifyAll();
            Mock.VerifyAll(k8s, k8sfactory);
            k8s.VerifyNoOtherCalls();
            k8sfactory.VerifyNoOtherCalls();

            Assert.IsNull(result);
        }

        [Test]
        public async Task ResetExperimentAsync_CreateOrUpdateThrow()
        {
            ExperimentEntity entity = new ExperimentEntity();

            // mock
            var moqs = new Moqs();
            moqs.ExperimentTable.Setup(p => p.RetrieveAsync("hack", "expr", default)).ReturnsAsync(entity);
            var k8s = new Mock<IKubernetesCluster>();
            k8s.Setup(k => k.DeleteExperimentAsync(It.IsAny<ExperimentContext>(), default));
            k8s.Setup(k => k.CreateOrUpdateExperimentAsync(It.IsAny<ExperimentContext>(), default))
                .Throws(new Exception("error"));
            var k8sfactory = new Mock<IKubernetesClusterFactory>();
            k8sfactory.Setup(f => f.GetDefaultKubernetes(default)).ReturnsAsync(k8s.Object);

            // test
            var management = new ExperimentManagement()
            {
                StorageContext = moqs.StorageContext.Object,
                KubernetesClusterFactory = k8sfactory.Object,
            };
            var result = await management.ResetExperimentAsync("hack", "expr", default);

            // verify
            moqs.VerifyAll();
            Mock.VerifyAll(k8s, k8sfactory);
            k8s.VerifyNoOtherCalls();
            k8sfactory.VerifyNoOtherCalls();
            Debug.Assert(result != null);
            Assert.IsNotNull(result.ExperimentEntity);
            Assert.AreEqual(500, result.Status.Code);
            Assert.AreEqual("error", result.Status.Message);
        }

        [TestCase(204)]
        [TestCase(409)]
        public async Task ResetExperimentAsync_Reset(int statusCode)
        {
            ExperimentEntity entity = new ExperimentEntity();

            // mock
            var moqs = new Moqs();
            moqs.ExperimentTable.Setup(p => p.RetrieveAsync("hack", "expr", default)).ReturnsAsync(entity);
            var k8s = new Mock<IKubernetesCluster>();
            k8s.Setup(k => k.DeleteExperimentAsync(It.IsAny<ExperimentContext>(), default));
            k8s.Setup(k => k.CreateOrUpdateExperimentAsync(It.IsAny<ExperimentContext>(), default))
                .Callback<ExperimentContext, CancellationToken>((c, ct) =>
                {
                    c.Status = new ExperimentStatus { Code = statusCode };
                });
            var k8sfactory = new Mock<IKubernetesClusterFactory>();
            k8sfactory.Setup(f => f.GetDefaultKubernetes(default)).ReturnsAsync(k8s.Object);

            // test
            var management = new ExperimentManagement()
            {
                StorageContext = moqs.StorageContext.Object,
                KubernetesClusterFactory = k8sfactory.Object,
            };
            var result = await management.ResetExperimentAsync("hack", "expr", default);

            // verify
            moqs.VerifyAll();
            Mock.VerifyAll(k8s, k8sfactory);
            k8s.VerifyNoOtherCalls();
            k8sfactory.VerifyNoOtherCalls();
            Debug.Assert(result != null);
            Assert.IsNotNull(result.ExperimentEntity);
            Assert.AreEqual(statusCode, result.Status.Code);
        }
        #endregion

        #region ListExperimentsAsync
        [TestCase("", "PartitionKey eq 'hack'")]
        [TestCase(null, "PartitionKey eq 'hack'")]
        [TestCase("tpl", "(PartitionKey eq 'hack') and (TemplateId eq 'tpl')")]
        public async Task ListExperimentsAsync(string templateId, string expectedFilter)
        {
            // data
            var hackathon = new HackathonEntity { PartitionKey = "hack" };
            var entities = new List<ExperimentEntity>
            {
                new ExperimentEntity{ RowKey="e1" },
                new ExperimentEntity{ RowKey="e2" },
            };
            var k8sResources = new List<ExperimentResource>
            {
                new ExperimentResource(), // empty, will be ignored
                new ExperimentResource
                {
                    Metadata = new k8s.Models.V1ObjectMeta
                    {
                        Name="other", // not match
                    }
                },
                new ExperimentResource
                {
                    Metadata = new k8s.Models.V1ObjectMeta
                    {
                        Name = "e1",
                    }
                },
            };

            // mock
            var experimentTable = new Mock<IExperimentTable>();
            experimentTable.Setup(p => p.QueryEntitiesAsync(expectedFilter, null, default)).ReturnsAsync(entities);
            var storageContext = new Mock<IStorageContext>();
            storageContext.SetupGet(p => p.ExperimentTable).Returns(experimentTable.Object);

            var k8s = new Mock<IKubernetesCluster>();
            k8s.Setup(k => k.ListExperimentsAsync("hack", templateId, default)).ReturnsAsync(k8sResources);
            var k8sFactory = new Mock<IKubernetesClusterFactory>();
            k8sFactory.Setup(p => p.GetDefaultKubernetes(default)).ReturnsAsync(k8s.Object);

            // test
            var management = new ExperimentManagement()
            {
                KubernetesClusterFactory = k8sFactory.Object,
                StorageContext = storageContext.Object,
            };
            var result = await management.ListExperimentsAsync(hackathon, templateId, default);

            // verify
            Mock.VerifyAll(experimentTable, storageContext, k8s, k8sFactory);
            experimentTable.VerifyNoOtherCalls();
            storageContext.VerifyNoOtherCalls();
            k8s.VerifyNoOtherCalls();
            k8sFactory.VerifyNoOtherCalls();

            Assert.AreEqual(2, result.Count());
            Assert.AreEqual("e1", result.First().ExperimentEntity.Id);
            Assert.AreEqual(200, result.First().Status.Code);
            Assert.AreEqual("success", result.First().Status.Status);
            Assert.AreEqual("Ok", result.First().Status.Reason);
            Assert.AreEqual("e2", result.Last().ExperimentEntity.Id);
            Assert.AreEqual(422, result.Last().Status.Code);
            Assert.AreEqual("failure", result.Last().Status.Status);
            Assert.AreEqual("UnprocessableEntity", result.Last().Status.Reason);
        }

        [TestCase("", "PartitionKey eq 'hack'")]
        [TestCase(null, "PartitionKey eq 'hack'")]
        [TestCase("tpl", "(PartitionKey eq 'hack') and (TemplateId eq 'tpl')")]
        public async Task ListExperimentsAsync_AlreadyCleaned(string templateId, string expectedFilter)
        {
            // data
            var hackathon = new HackathonEntity { PartitionKey = "hack", ExperimentCleaned = true };
            var entities = new List<ExperimentEntity>
            {
                new ExperimentEntity{ RowKey="e1" },
                new ExperimentEntity{ RowKey="e2" },
            };

            // mock
            var experimentTable = new Mock<IExperimentTable>();
            experimentTable.Setup(p => p.QueryEntitiesAsync(expectedFilter, null, default)).ReturnsAsync(entities);
            var storageContext = new Mock<IStorageContext>();
            storageContext.SetupGet(p => p.ExperimentTable).Returns(experimentTable.Object);
            var k8s = new Mock<IKubernetesCluster>();
            var k8sFactory = new Mock<IKubernetesClusterFactory>();

            // test
            var management = new ExperimentManagement()
            {
                KubernetesClusterFactory = k8sFactory.Object,
                StorageContext = storageContext.Object,
            };
            var result = await management.ListExperimentsAsync(hackathon, templateId, default);

            // verify
            Mock.VerifyAll(experimentTable, storageContext, k8s, k8sFactory);
            experimentTable.VerifyNoOtherCalls();
            storageContext.VerifyNoOtherCalls();
            k8s.VerifyNoOtherCalls();
            k8sFactory.VerifyNoOtherCalls();

            Assert.AreEqual(2, result.Count());
            Assert.AreEqual("e1", result.First().ExperimentEntity.Id);
            Assert.AreEqual(422, result.First().Status.Code);
            Assert.AreEqual("UnprocessableEntity", result.First().Status.Reason);
            Assert.AreEqual("e2", result.Last().ExperimentEntity.Id);
            Assert.AreEqual(422, result.Last().Status.Code);
            Assert.AreEqual("UnprocessableEntity", result.Last().Status.Reason);
        }
        #endregion

        #region DeleteExperimentAsync
        [Test]
        public async Task DeleteExperimentAsync_EntityNotFound()
        {
            ExperimentEntity? entity = null;

            // mock
            var moqs = new Moqs();
            moqs.ExperimentTable.Setup(p => p.RetrieveAsync("hack", "expr", default)).ReturnsAsync(entity);
            var k8s = new Mock<IKubernetesCluster>();
            var k8sfactory = new Mock<IKubernetesClusterFactory>();

            // test
            var management = new ExperimentManagement()
            {
                StorageContext = moqs.StorageContext.Object,
                KubernetesClusterFactory = k8sfactory.Object,
            };
            var result = await management.DeleteExperimentAsync("hack", "expr", default);

            // verify
            moqs.VerifyAll();
            Mock.VerifyAll(k8s, k8sfactory);
            k8s.VerifyNoOtherCalls();
            k8sfactory.VerifyNoOtherCalls();

            Assert.IsNull(result);
        }

        [TestCase(204)]
        [TestCase(409)]
        public async Task DeleteExperimentAsync_Deleted(int statusCode)
        {
            ExperimentEntity entity = new ExperimentEntity();

            // mock
            var moqs = new Moqs();
            moqs.ExperimentTable.Setup(p => p.RetrieveAsync("hack", "expr", default)).ReturnsAsync(entity);
            moqs.ExperimentTable.Setup(p => p.DeleteAsync("hack", "expr", default));
            var k8s = new Mock<IKubernetesCluster>();
            k8s.Setup(k => k.DeleteExperimentAsync(It.IsAny<ExperimentContext>(), default))
                .Callback<ExperimentContext, CancellationToken>((c, ct) =>
                {
                    c.Status = new ExperimentStatus { Code = statusCode };
                });
            var k8sfactory = new Mock<IKubernetesClusterFactory>();
            k8sfactory.Setup(f => f.GetDefaultKubernetes(default)).ReturnsAsync(k8s.Object);

            // test
            var management = new ExperimentManagement()
            {
                StorageContext = moqs.StorageContext.Object,
                KubernetesClusterFactory = k8sfactory.Object,
            };
            var result = await management.DeleteExperimentAsync("hack", "expr", default);

            // verify
            moqs.VerifyAll();
            Mock.VerifyAll(k8s, k8sfactory);
            k8s.VerifyNoOtherCalls();
            k8sfactory.VerifyNoOtherCalls();
            Debug.Assert(result != null);
            Assert.IsNotNull(result.ExperimentEntity);
            Assert.AreEqual(statusCode, result.Status.Code);
        }
        #endregion

        #region CleanupKubernetesExperimentsAsync
        [Test]
        public async Task CleanupKubernetesExperimentsAsync()
        {
            var resources = new List<ExperimentResource>
            {
                new ExperimentResource { Metadata = new V1ObjectMeta { Name = "e1" } },
                new ExperimentResource { Metadata = new V1ObjectMeta { Name = "e2" } },
            };

            // mock
            var k8s = new Mock<IKubernetesCluster>();
            k8s.Setup(k => k.ListExperimentsAsync("hack", null, default)).ReturnsAsync(resources);
            k8s.Setup(k => k.DeleteExperimentAsync("e1", default));
            k8s.Setup(k => k.DeleteExperimentAsync("e2", default));
            var k8sfactory = new Mock<IKubernetesClusterFactory>();
            k8sfactory.Setup(f => f.GetDefaultKubernetes(default)).ReturnsAsync(k8s.Object);

            // test
            var management = new ExperimentManagement()
            {
                KubernetesClusterFactory = k8sfactory.Object,
            };
            await management.CleanupKubernetesExperimentsAsync("hack", default);

            // verify
            Mock.VerifyAll(k8s, k8sfactory);
            k8s.VerifyNoOtherCalls();
            k8sfactory.VerifyNoOtherCalls();
        }
        #endregion
    }
}
