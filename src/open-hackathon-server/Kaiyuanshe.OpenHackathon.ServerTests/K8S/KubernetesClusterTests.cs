using k8s;
using k8s.Autorest;
using k8s.Models;
using Kaiyuanshe.OpenHackathon.Server.K8S;
using Kaiyuanshe.OpenHackathon.Server.K8S.Models;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.ServerTests.K8S
{
    class KubernetesClusterTests
    {
        #region CreateOrUpdateTemplateAsync
        [Test]
        public async Task CreateOrUpdateTemplateAsync_OtherError()
        {
            string content = "{\"code\": 422}";

            var moqs = new Moqs();
            moqs.CustomObjects.Setup(k => k.GetNamespacedCustomObjectWithHttpMessagesAsync(
               "hackathon.kaiyuanshe.cn", "v1", "default", "templates",
               "rk",
               null, default))
               .Throws(new HttpOperationException
               {
                   Response = new HttpResponseMessageWrapper(new System.Net.Http.HttpResponseMessage(), content)
               });
            var context = new TemplateContext
            {
                TemplateEntity = new TemplateEntity { PartitionKey = "pk", RowKey = "rk" }
            };

            var kubernetesCluster = new KubernetesCluster(moqs.Kubernetes.Object, moqs.Logger.Object);
            await kubernetesCluster.CreateOrUpdateTemplateAsync(context, default);

            moqs.VerifyAll();
        }

        [Test]
        public async Task CreateOrUpdateTemplateAsync_Create()
        {
            string content = "{\"code\": 404}";

            var moqs = new Moqs();
            moqs.CustomObjects.Setup(k => k.GetNamespacedCustomObjectWithHttpMessagesAsync(
               "hackathon.kaiyuanshe.cn", "v1", "default", "templates",
               "rk",
               null, default))
               .Throws(new HttpOperationException
               {
                   Response = new HttpResponseMessageWrapper(new System.Net.Http.HttpResponseMessage(), content)
               });
            moqs.CustomObjects.Setup(k => k.CreateNamespacedCustomObjectWithHttpMessagesAsync(
                It.IsAny<TemplateResource>(),
                "hackathon.kaiyuanshe.cn", "v1", "default", "templates",
                null, null, null, null, default))
               .ReturnsAsync(new HttpOperationResponse<object>
               {
                   Response = new System.Net.Http.HttpResponseMessage
                   {
                       StatusCode = System.Net.HttpStatusCode.Created,
                       ReasonPhrase = "success"
                   },
               });
            var context = new TemplateContext
            {
                TemplateEntity = new TemplateEntity { PartitionKey = "pk", RowKey = "rk" }
            };

            var kubernetesCluster = new KubernetesCluster(moqs.Kubernetes.Object, moqs.Logger.Object);
            await kubernetesCluster.CreateOrUpdateTemplateAsync(context, default);

            moqs.VerifyAll();
            Assert.AreEqual(201, context.Status.Code);
            Assert.AreEqual("success", context.Status.Reason);
        }

        [Test]
        public async Task CreateOrUpdateTemplateAsync_Patch()
        {
            var moqs = new Moqs();
            moqs.CustomObjects.Setup(k => k.GetNamespacedCustomObjectWithHttpMessagesAsync(
                "hackathon.kaiyuanshe.cn", "v1", "default", "templates",
                "rk",
                null, default))
                .ReturnsAsync(new HttpOperationResponse<object>
                {
                    Body = "{\"kind\":\"template\"}"
                });
            moqs.CustomObjects.Setup(k => k.PatchNamespacedCustomObjectWithHttpMessagesAsync(
                It.IsAny<V1Patch>(),
                "hackathon.kaiyuanshe.cn", "v1", "default", "templates",
                "rk",
                null, null, null, null, default))
                .ReturnsAsync(new HttpOperationResponse<object>
                {
                    Response = new System.Net.Http.HttpResponseMessage
                    {
                        StatusCode = System.Net.HttpStatusCode.OK,
                        ReasonPhrase = "success",
                    },
                });
            var context = new TemplateContext
            {
                TemplateEntity = new TemplateEntity { PartitionKey = "pk", RowKey = "rk" }
            };

            var kubernetesCluster = new KubernetesCluster(moqs.Kubernetes.Object, moqs.Logger.Object);
            await kubernetesCluster.CreateOrUpdateTemplateAsync(context, default);

            moqs.VerifyAll();
            Assert.AreEqual(200, context.Status.Code);
            Assert.AreEqual("success", context.Status.Reason);
        }
        #endregion

        #region UpdateTemplateAsync
        [Test]
        public async Task UpdateTemplateAsync()
        {
            var moqs = new Moqs();
            moqs.CustomObjects.Setup(k => k.PatchNamespacedCustomObjectWithHttpMessagesAsync(
                It.IsAny<V1Patch>(),
                "hackathon.kaiyuanshe.cn", "v1", "default", "templates",
                "rk",
                null, null, null, null, default))
                .ReturnsAsync(new HttpOperationResponse<object>
                {
                    Response = new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        ReasonPhrase = "success",
                    },
                });
            var context = new TemplateContext
            {
                TemplateEntity = new TemplateEntity { PartitionKey = "pk", RowKey = "rk" }
            };

            var kubernetesCluster = new KubernetesCluster(moqs.Kubernetes.Object, moqs.Logger.Object);
            await kubernetesCluster.UpdateTemplateAsync(context, default);

            moqs.VerifyAll();
            Assert.AreEqual(200, context.Status.Code);
            Assert.AreEqual("success", context.Status.Reason);
        }
        #endregion

        #region GetTemplateAsync
        [Test]
        public async Task GetTemplateAsync()
        {
            var moqs = new Moqs();
            moqs.CustomObjects.Setup(k => k.GetNamespacedCustomObjectWithHttpMessagesAsync(
                "hackathon.kaiyuanshe.cn", "v1", "default", "templates",
                "rk",
                null, default))
                .ReturnsAsync(new HttpOperationResponse<object>
                {
                    Body = "{\"kind\":\"template\"}"
                });
            var context = new TemplateContext
            {
                TemplateEntity = new TemplateEntity { PartitionKey = "pk", RowKey = "rk" }
            };

            var kubernetesCluster = new KubernetesCluster(moqs.Kubernetes.Object, moqs.Logger.Object);
            var result = await kubernetesCluster.GetTemplateAsync(context, default);

            moqs.VerifyAll();
            Assert.AreEqual("template", result.Kind);
            Assert.AreEqual(200, context.Status.Code);
            Assert.AreEqual("success", context.Status.Status);
        }
        #endregion

        #region ListTemplatesAsync
        [Test]
        public async Task ListTemplatesAsync()
        {
            var moqs = new Moqs();
            moqs.CustomObjects.Setup(k => k.ListNamespacedCustomObjectWithHttpMessagesAsync(
                "hackathon.kaiyuanshe.cn", "v1", "default", "templates",
                null, null, null, "hackathonName=hack",
                null, null, null, null, null, null, null, default))
                .ReturnsAsync(new HttpOperationResponse<object>
                {
                    Body = "{\"apiVersion\":\"hackathon.kaiyuanshe.cn/v1\",\"items\":[{\"apiVersion\":\"hackathon.kaiyuanshe.cn/v1\",\"data\":{\"ingressPort\":5901,\"ingressProtocol\":\"vnc\",\"type\":\"Pod\"},\"kind\":\"Template\",\"metadata\":{\"labels\":{\"hackathonName\":\"hack\"},\"name\":\"abc\",\"namespace\":\"default\"}}],\"kind\":\"TemplateList\"}"
                });

            var kubernetesCluster = new KubernetesCluster(moqs.Kubernetes.Object, moqs.Logger.Object);
            var result = await kubernetesCluster.ListTemplatesAsync("hack", default);

            moqs.VerifyAll();
            Assert.AreEqual(1, result.Count());
            Assert.AreEqual(5901, result.First().data.ingressPort);
            Assert.AreEqual("Template", result.First().Kind);
            Assert.AreEqual("abc", result.First().Metadata.Name);
        }
        #endregion

        #region DeleteTemplateAsync
        [Test]
        public async Task DeleteTemplateAsync_NotFound()
        {
            var moqs = new Moqs();
            moqs.CustomObjects.Setup(k => k.DeleteNamespacedCustomObjectWithHttpMessagesAsync(
                "hackathon.kaiyuanshe.cn", "v1", "default", "templates",
                "rk",
                null, null, null, null, null, null, default))
                .Throws(new HttpOperationException
                {
                    Response = new HttpResponseMessageWrapper(new HttpResponseMessage(HttpStatusCode.NotFound), "{\"code\":404}")
                }); ;
            var context = new TemplateContext
            {
                TemplateEntity = new TemplateEntity { PartitionKey = "pk", RowKey = "rk" }
            };

            var kubernetesCluster = new KubernetesCluster(moqs.Kubernetes.Object, moqs.Logger.Object);
            await kubernetesCluster.DeleteTemplateAsync(context, default);

            moqs.VerifyAll();
            moqs.Logger.Verify(
               x => x.Log(
                   LogLevel.Information,
                   It.IsAny<EventId>(),
                   It.IsAny<It.IsAnyType>(),
                   It.IsAny<Exception>(),
                   It.IsAny<Func<It.IsAnyType, Exception, string>>()));
            moqs.Logger.VerifyNoOtherCalls();
            Assert.AreEqual(204, context.Status.Code);
        }

        [Test]
        public async Task DeleteTemplateAsync_OtherError()
        {
            var moqs = new Moqs();
            moqs.CustomObjects.Setup(k => k.DeleteNamespacedCustomObjectWithHttpMessagesAsync(
                "hackathon.kaiyuanshe.cn", "v1", "default", "templates",
                "rk",
                null, null, null, null, null, null, default))
                .Throws(new HttpOperationException
                {
                    Response = new HttpResponseMessageWrapper(new HttpResponseMessage(HttpStatusCode.BadRequest), "{\"code\":400}")
                }); ;
            var context = new TemplateContext
            {
                TemplateEntity = new TemplateEntity { PartitionKey = "pk", RowKey = "rk" }
            };

            var kubernetesCluster = new KubernetesCluster(moqs.Kubernetes.Object, moqs.Logger.Object);
            await kubernetesCluster.DeleteTemplateAsync(context, default);

            moqs.VerifyAll();
            moqs.Logger.Verify(
               x => x.Log(
                   LogLevel.Information,
                   It.IsAny<EventId>(),
                   It.IsAny<It.IsAnyType>(),
                   It.IsAny<Exception>(),
                   It.IsAny<Func<It.IsAnyType, Exception, string>>()));
            moqs.Logger.VerifyNoOtherCalls();
            Assert.AreEqual(400, context.Status.Code);
        }

        [Test]
        public async Task DeleteTemplateAsync_Success()
        {
            var moqs = new Moqs();
            moqs.CustomObjects.Setup(k => k.DeleteNamespacedCustomObjectWithHttpMessagesAsync(
                "hackathon.kaiyuanshe.cn", "v1", "default", "templates",
                "rk",
                null, null, null, null, null, null, default));
            var context = new TemplateContext
            {
                TemplateEntity = new TemplateEntity { PartitionKey = "pk", RowKey = "rk" }
            };

            var kubernetesCluster = new KubernetesCluster(moqs.Kubernetes.Object, moqs.Logger.Object);
            await kubernetesCluster.DeleteTemplateAsync(context, default);

            moqs.VerifyAll();
            Assert.AreEqual(204, context.Status.Code);
        }

        [Test]
        public async Task DeleteTemplateAsync2_404()
        {
            var moqs = new Moqs();
            moqs.CustomObjects.Setup(k => k.DeleteNamespacedCustomObjectWithHttpMessagesAsync(
                "hackathon.kaiyuanshe.cn", "v1", "default", "templates",
                "name",
                null, null, null, null, null, null, default)).Throws(new HttpOperationException
                {
                    Response = new HttpResponseMessageWrapper(new HttpResponseMessage(HttpStatusCode.NotFound), "{\"code\":404}")
                });

            var kubernetesCluster = new KubernetesCluster(moqs.Kubernetes.Object, moqs.Logger.Object);
            await kubernetesCluster.DeleteTemplateAsync("name", default);

            moqs.VerifyAll();

        }

        [Test]
        public void DeleteTemplateAsync2_OtherError()
        {
            var moqs = new Moqs();
            moqs.CustomObjects.Setup(k => k.DeleteNamespacedCustomObjectWithHttpMessagesAsync(
                "hackathon.kaiyuanshe.cn", "v1", "default", "templates",
                "name",
                null, null, null, null, null, null, default)).Throws(new HttpOperationException
                {
                    Response = new HttpResponseMessageWrapper(new HttpResponseMessage(HttpStatusCode.InternalServerError), "{\"code\":404}")
                });

            var kubernetesCluster = new KubernetesCluster(moqs.Kubernetes.Object, moqs.Logger.Object);
            Assert.ThrowsAsync<HttpOperationException>(() => kubernetesCluster.DeleteTemplateAsync("name", default));

            moqs.VerifyAll();

        }

        [Test]
        public async Task DeleteTemplateAsync2_Success()
        {
            var moqs = new Moqs();
            moqs.CustomObjects.Setup(k => k.DeleteNamespacedCustomObjectWithHttpMessagesAsync(
                "hackathon.kaiyuanshe.cn", "v1", "default", "templates",
                "name",
                null, null, null, null, null, null, default));

            var kubernetesCluster = new KubernetesCluster(moqs.Kubernetes.Object, moqs.Logger.Object);
            await kubernetesCluster.DeleteTemplateAsync("name", default);

            moqs.VerifyAll();

        }
        #endregion

        #region CreateOrUpdateExperimentAsync
        [Test]
        public async Task CreateOrUpdateExperimentAsync_OtherError()
        {
            string content = "{\"code\": 422}";

            var moqs = new Moqs();
            var kubernetes = new Mock<IKubernetes>();
            moqs.CustomObjects.Setup(k => k.GetNamespacedCustomObjectWithHttpMessagesAsync(
               "hackathon.kaiyuanshe.cn", "v1", "default", "experiments",
               "rk",
               null, default))
               .Throws(new HttpOperationException
               {
                   Response = new HttpResponseMessageWrapper(new System.Net.Http.HttpResponseMessage(), content)
               });
            var context = new ExperimentContext
            {
                ExperimentEntity = new ExperimentEntity
                {
                    PartitionKey = "pk",
                    RowKey = "rk",
                    TemplateId = "tpl",
                    UserId = "uid",
                }
            };

            var kubernetesCluster = new KubernetesCluster(moqs.Kubernetes.Object, moqs.Logger.Object);
            await kubernetesCluster.CreateOrUpdateExperimentAsync(context, default);
            moqs.VerifyAll();

        }

        [Test]
        public async Task CreateOrUpdateExperimentAsync_Create()
        {
            string content = "{\"code\": 404}";

            var moqs = new Moqs();
            moqs.CustomObjects.Setup(k => k.GetNamespacedCustomObjectWithHttpMessagesAsync(
               "hackathon.kaiyuanshe.cn", "v1", "default", "experiments",
               "rk",
               null, default))
               .Throws(new HttpOperationException
               {
                   Response = new HttpResponseMessageWrapper(new System.Net.Http.HttpResponseMessage(), content)
               });
            moqs.CustomObjects.Setup(k => k.CreateNamespacedCustomObjectWithHttpMessagesAsync(
                It.IsAny<ExperimentResource>(),
                "hackathon.kaiyuanshe.cn", "v1", "default", "experiments",
                null, null, null, null, default))
               .ReturnsAsync(new HttpOperationResponse<object>
               {
                   Response = new System.Net.Http.HttpResponseMessage
                   {
                       StatusCode = System.Net.HttpStatusCode.Created,
                       ReasonPhrase = "success"
                   },
               });
            var context = new ExperimentContext
            {
                ExperimentEntity = new ExperimentEntity
                {
                    PartitionKey = "pk",
                    RowKey = "rk",
                    TemplateId = "tpl",
                    UserId = "uid",
                }
            };

            var kubernetesCluster = new KubernetesCluster(moqs.Kubernetes.Object, moqs.Logger.Object);
            await kubernetesCluster.CreateOrUpdateExperimentAsync(context, default);

            moqs.VerifyAll();
            Assert.AreEqual(201, context.Status.Code);
            Assert.AreEqual("success", context.Status.Reason);
        }

        [Test]
        public async Task CreateOrUpdateExperimentAsync_Update()
        {
            var context = new ExperimentContext
            {
                ExperimentEntity = new ExperimentEntity
                {
                    PartitionKey = "pk",
                    RowKey = "rk",
                    TemplateId = "tpl",
                    UserId = "uid",
                }
            };

            var moqs = new Moqs();
            moqs.CustomObjects.Setup(k => k.GetNamespacedCustomObjectWithHttpMessagesAsync(
               "hackathon.kaiyuanshe.cn", "v1", "default", "experiments",
               "rk",
               null, default))
               .ReturnsAsync(new HttpOperationResponse<object>
               {
                   Body = "{\"kind\":\"experiment\",\"status\": {}}"
               });
            moqs.CustomObjects.Setup(k => k.PatchNamespacedCustomObjectWithHttpMessagesAsync(
                 It.IsAny<V1Patch>(),
                 "hackathon.kaiyuanshe.cn", "v1", "default", "experiments",
                 "rk",
                 null, null, null, null, default))
                 .ReturnsAsync(new HttpOperationResponse<object>
                 {
                     Response = new HttpResponseMessage
                     {
                         StatusCode = HttpStatusCode.OK,
                         ReasonPhrase = "success",
                     },
                 });

            var kubernetesCluster = new KubernetesCluster(moqs.Kubernetes.Object, moqs.Logger.Object);
            await kubernetesCluster.CreateOrUpdateExperimentAsync(context, default);

            moqs.VerifyAll();
            Assert.AreEqual(200, context.Status.Code);
            Assert.AreEqual("success", context.Status.Reason);
        }
        #endregion

        #region UpdateExperimentAsync
        [Test]
        public async Task UpdateExperimentAsync()
        {
            var moqs = new Moqs();
            moqs.CustomObjects.Setup(k => k.PatchNamespacedCustomObjectWithHttpMessagesAsync(
                It.IsAny<V1Patch>(),
                "hackathon.kaiyuanshe.cn", "v1", "default", "experiments",
                "rk",
                null, null, null, null, default))
                .ReturnsAsync(new HttpOperationResponse<object>
                {
                    Response = new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        ReasonPhrase = "success",
                    },
                });
            var context = new ExperimentContext
            {
                ExperimentEntity = new ExperimentEntity { PartitionKey = "pk", RowKey = "rk" }
            };

            var kubernetesCluster = new KubernetesCluster(moqs.Kubernetes.Object, moqs.Logger.Object);
            await kubernetesCluster.UpdateExperimentAsync(context, default);

            moqs.VerifyAll();
            Assert.AreEqual(200, context.Status.Code);
            Assert.AreEqual("success", context.Status.Reason);
        }
        #endregion

        #region GetExperimentAsync
        [Test]
        public async Task GetExperimentAsync()
        {
            var moqs = new Moqs();
            moqs.CustomObjects.Setup(k => k.GetNamespacedCustomObjectWithHttpMessagesAsync(
                "hackathon.kaiyuanshe.cn", "v1", "default", "experiments",
                "rk",
                null, default))
                .ReturnsAsync(new HttpOperationResponse<object>
                {
                    Body = "{\"kind\":\"experiment\", \"status\":{ \"code\":204}}"
                });
            var context = new ExperimentContext
            {
                ExperimentEntity = new ExperimentEntity { PartitionKey = "pk", RowKey = "rk" }
            };

            var kubernetesCluster = new KubernetesCluster(moqs.Kubernetes.Object, moqs.Logger.Object);
            var result = await kubernetesCluster.GetExperimentAsync(context, default);

            moqs.VerifyAll();
            Assert.AreEqual("experiment", result.Kind);
            Assert.AreEqual(204, context.Status.Code);
        }
        #endregion

        #region ListExperimentsAsync
        [Test]
        public async Task ListTemplatesAsync_ByHackathon()
        {
            var moqs = new Moqs();
            moqs.CustomObjects.Setup(k => k.ListNamespacedCustomObjectWithHttpMessagesAsync(
                "hackathon.kaiyuanshe.cn", "v1", "default", "experiments",
                null, null, null, "hackathonName=hack",
                null, null, null, null, null, null, null, default))
                .ReturnsAsync(new HttpOperationResponse<object>
                {
                    Body = "{\"apiVersion\":\"hackathon.kaiyuanshe.cn/v1\",\"items\":[{\"apiVersion\":\"hackathon.kaiyuanshe.cn/v1\",\"status\":{\"ingressPort\":5901,\"ingressProtocol\":\"vnc\",\"type\":\"Pod\"},\"kind\":\"Experiment\",\"metadata\":{\"labels\":{\"hackathonName\":\"hack\"},\"name\":\"abc\",\"namespace\":\"default\"}}],\"kind\":\"ExperimentList\"}"
                });

            var kubernetesCluster = new KubernetesCluster(moqs.Kubernetes.Object, moqs.Logger.Object);
            var result = await kubernetesCluster.ListExperimentsAsync("hack", null, default);

            moqs.VerifyAll();
            Assert.AreEqual(1, result.Count());
            Assert.AreEqual(5901, result.First().Status.ingressPort);
            Assert.AreEqual("Experiment", result.First().Kind);
            Assert.AreEqual("abc", result.First().Metadata.Name);
        }

        [Test]
        public async Task ListTemplatesAsync_ByTemplate()
        {
            var moqs = new Moqs();
            moqs.CustomObjects.Setup(k => k.ListNamespacedCustomObjectWithHttpMessagesAsync(
                "hackathon.kaiyuanshe.cn", "v1", "default", "experiments",
                null, null, null, "hackathonName=hack,templateId=tpl",
                null, null, null, null, null, null, null, default))
                .ReturnsAsync(new HttpOperationResponse<object>
                {
                    Body = "{\"apiVersion\":\"hackathon.kaiyuanshe.cn/v1\",\"items\":[{\"apiVersion\":\"hackathon.kaiyuanshe.cn/v1\",\"status\":{\"ingressPort\":5901,\"ingressProtocol\":\"vnc\",\"type\":\"Pod\"},\"kind\":\"Experiment\",\"metadata\":{\"labels\":{\"hackathonName\":\"hack\"},\"name\":\"abc\",\"namespace\":\"default\"}}],\"kind\":\"ExperimentList\"}"
                });

            var kubernetesCluster = new KubernetesCluster(moqs.Kubernetes.Object, moqs.Logger.Object);
            var result = await kubernetesCluster.ListExperimentsAsync("hack", "tpl", default);

            moqs.VerifyAll();
            Assert.AreEqual(1, result.Count());
            Assert.AreEqual(5901, result.First().Status.ingressPort);
            Assert.AreEqual("Experiment", result.First().Kind);
            Assert.AreEqual("abc", result.First().Metadata.Name);
        }
        #endregion

        #region DeleteExperimentAsync
        [Test]
        public async Task DeleteExperimentAsync_NotFound()
        {
            var moqs = new Moqs();
            moqs.CustomObjects.Setup(k => k.DeleteNamespacedCustomObjectWithHttpMessagesAsync(
                "hackathon.kaiyuanshe.cn", "v1", "default", "experiments",
                "rk",
                null, null, null, null, null, null, default))
                .Throws(new HttpOperationException
                {
                    Response = new HttpResponseMessageWrapper(new HttpResponseMessage(HttpStatusCode.NotFound), "{\"code\":404}")
                });
            var context = new ExperimentContext
            {
                ExperimentEntity = new ExperimentEntity { PartitionKey = "pk", RowKey = "rk" }
            };

            var kubernetesCluster = new KubernetesCluster(moqs.Kubernetes.Object, moqs.Logger.Object);
            await kubernetesCluster.DeleteExperimentAsync(context, default);

            moqs.VerifyAll();
            moqs.Logger.Verify(
               x => x.Log(
                   LogLevel.Information,
                   It.IsAny<EventId>(),
                   It.IsAny<It.IsAnyType>(),
                   It.IsAny<Exception>(),
                   It.IsAny<Func<It.IsAnyType, Exception, string>>()));
            moqs.Logger.VerifyNoOtherCalls();
            Assert.AreEqual(204, context.Status.Code);
        }

        [Test]
        public async Task DeleteExperimentAsync_OtherError()
        {
            var moqs = new Moqs();
            moqs.CustomObjects.Setup(k => k.DeleteNamespacedCustomObjectWithHttpMessagesAsync(
                "hackathon.kaiyuanshe.cn", "v1", "default", "experiments",
                "rk",
                null, null, null, null, null, null, default))
                .Throws(new HttpOperationException
                {
                    Response = new HttpResponseMessageWrapper(new HttpResponseMessage(HttpStatusCode.BadRequest), "{\"code\":400}")
                });
            var context = new ExperimentContext
            {
                ExperimentEntity = new ExperimentEntity { PartitionKey = "pk", RowKey = "rk" }
            };

            var kubernetesCluster = new KubernetesCluster(moqs.Kubernetes.Object, moqs.Logger.Object);
            await kubernetesCluster.DeleteExperimentAsync(context, default);

            moqs.VerifyAll();
            moqs.Logger.Verify(
               x => x.Log(
                   LogLevel.Information,
                   It.IsAny<EventId>(),
                   It.IsAny<It.IsAnyType>(),
                   It.IsAny<Exception>(),
                   It.IsAny<Func<It.IsAnyType, Exception, string>>()));
            moqs.Logger.VerifyNoOtherCalls();
            Assert.AreEqual(400, context.Status.Code);
        }

        [Test]
        public async Task DeleteExperimentAsync_Success()
        {
            var moqs = new Moqs();
            moqs.CustomObjects.Setup(k => k.DeleteNamespacedCustomObjectWithHttpMessagesAsync(
                "hackathon.kaiyuanshe.cn", "v1", "default", "experiments",
                "rk",
                null, null, null, null, null, null, default));
            var context = new ExperimentContext
            {
                ExperimentEntity = new ExperimentEntity { PartitionKey = "pk", RowKey = "rk" }
            };

            var kubernetesCluster = new KubernetesCluster(moqs.Kubernetes.Object, moqs.Logger.Object);
            await kubernetesCluster.DeleteExperimentAsync(context, default);

            moqs.VerifyAll();
            Assert.AreEqual(204, context.Status.Code);
        }

        [Test]
        public async Task DeleteExperimentAsync2_404()
        {
            var moqs = new Moqs();
            moqs.CustomObjects.Setup(k => k.DeleteNamespacedCustomObjectWithHttpMessagesAsync(
                "hackathon.kaiyuanshe.cn", "v1", "default", "experiments",
                "name",
                null, null, null, null, null, null, default)).Throws(new HttpOperationException
                {
                    Response = new HttpResponseMessageWrapper(new HttpResponseMessage(HttpStatusCode.NotFound), "{\"code\":404}")
                });

            var kubernetesCluster = new KubernetesCluster(moqs.Kubernetes.Object, moqs.Logger.Object);
            await kubernetesCluster.DeleteExperimentAsync("name", default);

            moqs.VerifyAll();
        }

        [Test]
        public void DeleteExperimentAsync2_OtherError()
        {
            var moqs = new Moqs();
            moqs.CustomObjects.Setup(k => k.DeleteNamespacedCustomObjectWithHttpMessagesAsync(
                "hackathon.kaiyuanshe.cn", "v1", "default", "experiments",
                "name",
                null, null, null, null, null, null, default)).Throws(new HttpOperationException
                {
                    Response = new HttpResponseMessageWrapper(new HttpResponseMessage(HttpStatusCode.InternalServerError), "{\"code\":404}")
                });

            var kubernetesCluster = new KubernetesCluster(moqs.Kubernetes.Object, moqs.Logger.Object);
            Assert.ThrowsAsync<HttpOperationException>(() => kubernetesCluster.DeleteExperimentAsync("name", default));

            moqs.VerifyAll();
        }

        [Test]
        public async Task DeleteExperimentAsync2_Success()
        {
            var moqs = new Moqs();
            moqs.CustomObjects.Setup(k => k.DeleteNamespacedCustomObjectWithHttpMessagesAsync(
                "hackathon.kaiyuanshe.cn", "v1", "default", "experiments",
                "name",
                null, null, null, null, null, null, default));

            var kubernetesCluster = new KubernetesCluster(moqs.Kubernetes.Object, moqs.Logger.Object);
            await kubernetesCluster.DeleteExperimentAsync("name", default);

            moqs.VerifyAll();
        }
        #endregion
    }
}
