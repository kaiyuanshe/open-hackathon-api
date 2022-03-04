using k8s.Models;
using Kaiyuanshe.OpenHackathon.Server.K8S;
using Kaiyuanshe.OpenHackathon.Server.K8S.Models;
using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Storage;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.Biz
{
    public interface IExperimentManagement
    {
        Task<TemplateContext> CreateOrUpdateTemplateAsync(Template template, CancellationToken cancellationToken);
        Task<TemplateContext> GetTemplateAsync(string hackathonName, string templateName, CancellationToken cancellationToken);
        Task<IEnumerable<TemplateContext>> ListTemplatesAsync(string hackathonName, CancellationToken cancellationToken);
        Task<int> GetTemplateCountAsync(string hackathonName, CancellationToken cancellationToken);
        Task<TemplateContext> DeleteTemplateAsync(string hackathonName, string templateId, CancellationToken cancellationToken);
        Task<ExperimentContext> CreateExperimentAsync(Experiment experiment, CancellationToken cancellationToken);
        Task<ExperimentContext> GetExperimentAsync(string hackathonName, string experimentId, CancellationToken cancellationToken);
    }

    public class ExperimentManagement : ManagementClientBase, IExperimentManagement
    {
        private readonly ILogger logger;

        public IKubernetesClusterFactory KubernetesClusterFactory { get; set; }

        public ExperimentManagement(ILogger<ExperimentManagement> logger)
        {
            this.logger = logger;
        }

        #region CreateOrUpdateTemplateAsync
        public async Task<TemplateContext> CreateOrUpdateTemplateAsync(Template request, CancellationToken cancellationToken)
        {
            if (request == null)
                return null;

            request.id ??= Guid.NewGuid().ToString();
            var entity = await StorageContext.TemplateTable.RetrieveAsync(request.hackathonName, request.id, cancellationToken);
            if (entity == null)
            {
                entity = new TemplateEntity
                {
                    PartitionKey = request.hackathonName,
                    RowKey = request.id.ToLower(),
                    Commands = request.commands,
                    CreatedAt = DateTime.UtcNow,
                    EnvironmentVariables = request.environmentVariables,
                    Image = request.image,
                    DisplayName = request.displayName ?? "default",
                    IngressPort = request.ingressPort.GetValueOrDefault(5901),
                    IngressProtocol = request.ingressProtocol.GetValueOrDefault(IngressProtocol.vnc),
                    Vnc = request.vnc,
                };
                await StorageContext.TemplateTable.InsertAsync(entity, cancellationToken);
            }
            else
            {
                entity.Commands = request.commands ?? entity.Commands;
                entity.EnvironmentVariables = request.environmentVariables ?? entity.EnvironmentVariables;
                entity.Image = request.image ?? entity.Image;
                entity.DisplayName = request.displayName ?? entity.DisplayName;
                entity.IngressPort = request.ingressPort.GetValueOrDefault(entity.IngressPort);
                entity.IngressProtocol = request.ingressProtocol.GetValueOrDefault(entity.IngressProtocol);
                if (request.vnc != null)
                {
                    entity.Vnc = entity.Vnc ?? new VncSettings();
                    entity.Vnc.userName = request.vnc.userName ?? entity.Vnc.userName;
                    entity.Vnc.password = request.vnc.password ?? entity.Vnc.password;
                }
                await StorageContext.TemplateTable.MergeAsync(entity, cancellationToken);
            }

            // call K8S API.
            var context = new TemplateContext { TemplateEntity = entity };
            var kubernetesCluster = await KubernetesClusterFactory.GetDefaultKubernetes(cancellationToken);
            try
            {
                await kubernetesCluster.CreateOrUpdateTemplateAsync(context, cancellationToken);
            }
            catch (Exception e)
            {
                logger.TraceError($"Internal error: {e.Message}", e);
                context.Status = new k8s.Models.V1Status
                {
                    Code = 500,
                    Reason = "Internal Server Error",
                    Message = e.Message,
                };
            }
            return context;
        }
        #endregion

        #region GetTemplateAsync
        public async Task<TemplateContext> GetTemplateAsync(string hackathonName, string templateId, CancellationToken cancellationToken)
        {
            if (hackathonName == null || templateId == null)
                return null;

            var entity = await StorageContext.TemplateTable.RetrieveAsync(hackathonName, templateId.ToLower(), cancellationToken);
            if (entity == null)
                return null;

            var context = new TemplateContext
            {
                TemplateEntity = entity,
            };
            var kubernetesCluster = await KubernetesClusterFactory.GetDefaultKubernetes(cancellationToken);
            try
            {
                await kubernetesCluster.GetTemplateAsync(context, cancellationToken);
            }
            catch (Exception e)
            {
                logger.TraceError($"Internal error: {e.Message}", e);
                context.Status = new ExperimentStatus
                {
                    Code = 500,
                    Reason = "Internal Server Error",
                    Message = e.Message,
                };
            }
            return context;
        }
        #endregion

        #region ListTemplatesAsync
        public async Task<IEnumerable<TemplateContext>> ListTemplatesAsync(string hackathonName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(hackathonName))
                return Array.Empty<TemplateContext>();

            // get storage entities
            var filter = TableQueryHelper.PartitionKeyFilter(hackathonName);
            var templateEntities = await StorageContext.TemplateTable.QueryEntitiesAsync(filter, null, cancellationToken);

            // get k8s resources
            var kubernetesCluster = await KubernetesClusterFactory.GetDefaultKubernetes(cancellationToken);
            var k8sResources = await kubernetesCluster.ListTemplatesAsync(hackathonName, cancellationToken);

            // compose resp
            Func<TemplateResource, string, bool> selector = (k8sResource, tempId) =>
            {
                if (k8sResource?.Metadata?.Labels != null
                    && k8sResource.Metadata.Labels.TryGetValue(Labels.TemplateId, out string templateId)
                    && tempId == templateId)
                {
                    return true;
                }
                return false;
            };
            return templateEntities.Select(entity =>
            {
                var kr = k8sResources.FirstOrDefault(k => selector(k, entity.Id));
                var status = new V1Status
                {
                    Code = kr != null ? 200 : 422,
                    Status = kr != null ? "success" : "failure",
                    Reason = kr != null ? "Ok" : "UnprocessableEntity",
                    Message = kr != null ? "Ok" : "Template not ready in kubernetes. Please call Patch template API to recover it.",
                };
                var context = new TemplateContext
                {
                    TemplateEntity = entity,
                    Status = status,
                };

                return context;
            });
        }
        #endregion

        #region GetTemplateCountAsync
        public async Task<int> GetTemplateCountAsync(string hackathonName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(hackathonName))
                return 0;

            // get storage entities
            var filter = TableQueryHelper.PartitionKeyFilter(hackathonName);
            var entities = await StorageContext.TemplateTable.QueryEntitiesAsync(filter, new string[] { nameof(TemplateEntity.RowKey) }, cancellationToken);
            return entities.Count();
        }
        #endregion

        #region DeleteTemplateAsync
        public async Task<TemplateContext> DeleteTemplateAsync(string hackathonName, string templateId, CancellationToken cancellationToken)
        {
            var entity = await StorageContext.TemplateTable.RetrieveAsync(hackathonName, templateId, cancellationToken);
            if (entity == null)
                return null;

            // delete k8s first
            var kubernetesCluster = await KubernetesClusterFactory.GetDefaultKubernetes(cancellationToken);
            var context = new TemplateContext { TemplateEntity = entity };
            await kubernetesCluster.DeleteTemplateAsync(context, cancellationToken);

            // delete storage
            await StorageContext.TemplateTable.DeleteAsync(hackathonName, templateId, cancellationToken);
            return context;
        }
        #endregion

        #region CreateExperimentAsync
        public async Task<ExperimentContext> CreateExperimentAsync(Experiment experiment, CancellationToken cancellationToken)
        {
            if (experiment == null)
                return null;

            var entity = new ExperimentEntity
            {
                PartitionKey = experiment.hackathonName,
                RowKey = GetExperimentRowKey(experiment.userId, experiment.templateId),
                CreatedAt = DateTime.UtcNow,
                Paused = false,
                TemplateId = experiment.templateId,
                UserId = experiment.userId,
            };
            await StorageContext.ExperimentTable.InsertOrReplaceAsync(entity, cancellationToken);

            // call k8s api
            entity = await StorageContext.ExperimentTable.RetrieveAsync(entity.PartitionKey, entity.RowKey, cancellationToken);
            var context = new ExperimentContext
            {
                ExperimentEntity = entity,
            };
            var kubernetesCluster = await KubernetesClusterFactory.GetDefaultKubernetes(cancellationToken);
            try
            {
                await kubernetesCluster.CreateOrUpdateExperimentAsync(context, cancellationToken);
            }
            catch (Exception e)
            {
                logger.TraceError($"Internal error: {e.Message}", e);
                context.Status = new ExperimentStatus
                {
                    Code = 500,
                    Reason = "Internal Server Error",
                    Message = e.Message,
                };
            }
            return context;
        }
        #endregion

        #region GetExperimentAsync
        public async Task<ExperimentContext> GetExperimentAsync(string hackathonName, string experimentId, CancellationToken cancellationToken)
        {
            if (hackathonName == null || experimentId == null)
                return null;

            var entity = await StorageContext.ExperimentTable.RetrieveAsync(hackathonName, experimentId, cancellationToken);
            if (entity == null)
                return null;

            var context = new ExperimentContext { ExperimentEntity = entity };
            var kubernetesCluster = await KubernetesClusterFactory.GetDefaultKubernetes(cancellationToken);
            try
            {
                await kubernetesCluster.GetExperimentAsync(context, cancellationToken);
            }
            catch (Exception e)
            {
                logger.TraceError($"Internal error: {e.Message}", e);
                context.Status = new ExperimentStatus
                {
                    Code = 500,
                    Reason = "Internal Server Error",
                    Message = e.Message,
                };
            }
            return context;
        }
        #endregion

        private string GetExperimentRowKey(string userId, string templateName)
        {
            string salt = $"{userId}-{templateName}".ToLower();
            return DigestHelper.String2Guid(salt).ToString();
        }
    }
}
