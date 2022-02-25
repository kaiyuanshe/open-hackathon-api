using k8s;
using Kaiyuanshe.OpenHackathon.Server.K8S.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Rest;
using Microsoft.Rest.Serialization;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.K8S
{
    public interface IKubernetesCluster
    {
        Task CreateOrUpdateTemplateAsync(TemplateContext context, CancellationToken cancellationToken);
        Task UpdateTemplateAsync(TemplateContext context, CancellationToken cancellationToken);
        Task<TemplateResource> GetTemplateAsync(TemplateContext context, CancellationToken cancellationToken);
        Task<IEnumerable<TemplateResource>> ListTemplatesAsync(string hackathonName, CancellationToken cancellationToken);
        Task DeleteTemplateAsync(TemplateContext context, CancellationToken cancellationToken);
        Task CreateOrUpdateExperimentAsync(ExperimentContext context, CancellationToken cancellationToken);
        Task<ExperimentResource> GetExperimentAsync(ExperimentContext context, CancellationToken cancellationToken);
    }

    public class KubernetesCluster : IKubernetesCluster
    {
        IKubernetes kubeClient;
        private readonly ILogger logger;

        public KubernetesCluster(
            IKubernetes kubernetes,
            ILogger logger)
        {
            kubeClient = kubernetes;
            this.logger = logger;
        }

        #region CreateOrUpdateTemplateAsync
        public async Task CreateOrUpdateTemplateAsync(TemplateContext context, CancellationToken cancellationToken)
        {
            var cr = await GetTemplateAsync(context, cancellationToken);
            if (cr == null)
            {
                if (context.Status.Code != 404)
                {
                    // in case of other errors, return status directly for manual fix.
                    return;
                }

                // create new
                await CreateTemplateAsync(context, cancellationToken);
            }
            else
            {
                // apply patch
                await UpdateTemplateAsync(context, cancellationToken);
            }
        }

        private async Task CreateTemplateAsync(TemplateContext context, CancellationToken cancellationToken)
        {
            // create if not found
            var customResource = context.BuildCustomResource();
            try
            {
                var resp = await kubeClient.CreateNamespacedCustomObjectWithHttpMessagesAsync(
                    customResource,
                    TemplateResource.Group,
                    TemplateResource.Version,
                    customResource.Metadata.NamespaceProperty ?? "default",
                    TemplateResource.Plural,
                    cancellationToken: cancellationToken);
                logger.TraceInformation($"CreateTemplateAsync. Status: {resp.Response.StatusCode}, reason: {resp.Response.ReasonPhrase}");
                context.Status = new k8s.Models.V1Status
                {
                    Code = (int)resp.Response.StatusCode,
                    Reason = resp.Response.ReasonPhrase,
                };
            }
            catch (HttpOperationException exception)
            {
                if (exception.Response?.Content == null)
                    throw;

                logger.TraceError($"CreateTemplateAsync: {exception.Message}", exception);
                context.Status = JsonConvert.DeserializeObject<k8s.Models.V1Status>(exception.Response.Content);
            }
        }
        #endregion

        #region UpdateTemplateAsync
        public async Task UpdateTemplateAsync(TemplateContext context, CancellationToken cancellationToken)
        {
            var patch = context.BuildPatch();
            try
            {
                var resp = await kubeClient.PatchNamespacedCustomObjectWithHttpMessagesAsync(
                    patch,
                    TemplateResource.Group,
                    TemplateResource.Version,
                    TemplateContext.DefaultNameSpace,
                    TemplateResource.Plural,
                    context.GetTemplateResourceName(),
                    cancellationToken: cancellationToken);

                logger.TraceInformation($"UpdateTemplateAsync. Status: {resp.Response.StatusCode}, reason: {resp.Response.ReasonPhrase}");
                context.Status = new k8s.Models.V1Status
                {
                    Code = (int)resp.Response.StatusCode,
                    Reason = resp.Response.ReasonPhrase,
                };
            }
            catch (HttpOperationException exception)
            {
                if (exception.Response?.Content == null)
                    throw;

                logger.TraceError($"UpdateTemplateAsync: {exception.Message}", exception);
                context.Status = JsonConvert.DeserializeObject<k8s.Models.V1Status>(exception.Response.Content);
            }
        }
        #endregion

        #region GetTemplateAsync
        public async Task<TemplateResource> GetTemplateAsync(TemplateContext context, CancellationToken cancellationToken)
        {
            try
            {
                var cr = await kubeClient.GetNamespacedCustomObjectWithHttpMessagesAsync(
                    CustomResource.Group,
                    CustomResource.Version,
                    TemplateContext.DefaultNameSpace,
                    TemplateResource.Plural,
                    context.GetTemplateResourceName(),
                    null,
                    cancellationToken);

                context.Status = new k8s.Models.V1Status
                {
                    Code = 200,
                    Status = "success"
                };
                var resp = SafeJsonConvert.DeserializeObject<TemplateResource>(cr.Body.ToString());
                return resp;
            }
            catch (HttpOperationException exception)
            {
                if (exception.Response?.Content == null)
                    throw;

                logger.TraceInformation(exception.Response.Content);
                context.Status = JsonConvert.DeserializeObject<k8s.Models.V1Status>(exception.Response.Content);
                return null;
            }
        }
        #endregion

        #region ListTemplatesAsync
        public async Task<IEnumerable<TemplateResource>> ListTemplatesAsync(string hackathonName, CancellationToken cancellationToken)
        {
            try
            {
                var labelSelector = $"{TemplateContext.LabelHackathonName}={hackathonName}";
                var listResp = await kubeClient.ListNamespacedCustomObjectWithHttpMessagesAsync(
                    CustomResource.Group,
                    CustomResource.Version,
                    TemplateContext.DefaultNameSpace,
                    TemplateResource.Plural,
                    labelSelector: labelSelector,
                    cancellationToken: cancellationToken);
                var crl = SafeJsonConvert.DeserializeObject<CustomResourceList<TemplateResource>>(listResp.Body.ToString());
                return crl.Items;
            }
            catch (HttpOperationException exception)
            {
                if (exception.Response?.Content == null)
                    throw;

                logger.TraceInformation(exception.Response.Content);
                return Array.Empty<TemplateResource>();
            }
        }
        #endregion

        #region DeleteTemplateAsync
        public async Task DeleteTemplateAsync(TemplateContext context, CancellationToken cancellationToken)
        {
            try
            {
                await kubeClient.DeleteNamespacedCustomObjectWithHttpMessagesAsync(
                    CustomResource.Group,
                    CustomResource.Version,
                    TemplateContext.DefaultNameSpace,
                    TemplateResource.Plural,
                    context.GetTemplateResourceName(),
                    cancellationToken: cancellationToken);
                context.Status = new k8s.Models.V1Status
                {
                    Code = 204,
                    Status = "success"
                };
            }
            catch (HttpOperationException exception)
            {
                logger.TraceInformation(exception.Response.Content);
                if (exception.Response.StatusCode != HttpStatusCode.NotFound)
                {
                    // ignore 404
                    context.Status = JsonConvert.DeserializeObject<k8s.Models.V1Status>(exception.Response.Content);
                }
                else
                {
                    context.Status = new k8s.Models.V1Status
                    {
                        Code = 204,
                        Status = "success"
                    };
                }
            }
        }
        #endregion

        #region CreateOrUpdateExperimentAsync
        public async Task CreateOrUpdateExperimentAsync(ExperimentContext context, CancellationToken cancellationToken)
        {
            var cr = await GetExperimentAsync(context, cancellationToken);
            if (cr == null)
            {
                if (context.Status.Code != 404)
                {
                    // in case of other errors, return status directly for manual fix.
                    return;
                }

                // create new
                await CreateExperimentAsync(context, cancellationToken);
            }
            else
            {
                context.Status = cr.Status;
                context.Status.Code = cr.Status.Code.GetValueOrDefault(200);
            }
        }

        private async Task CreateExperimentAsync(ExperimentContext context, CancellationToken cancellationToken)
        {
            // create if not found
            var customResource = context.BuildCustomResource();
            try
            {
                var resp = await kubeClient.CreateNamespacedCustomObjectWithHttpMessagesAsync(
                    customResource,
                    CustomResource.Group,
                    CustomResource.Version,
                    customResource.Metadata.NamespaceProperty ?? "default",
                    ExperimentResource.Plural,
                    cancellationToken: cancellationToken);
                logger.TraceInformation($"CreateExperimentAsync. Status: {resp.Response.StatusCode}, reason: {resp.Response.Content.AsString()}");
                context.Status = new ExperimentStatus
                {
                    Reason = resp.Response.ReasonPhrase,
                    Status = resp.Response.ReasonPhrase,
                    Code = (int)resp.Response.StatusCode,
                };
            }
            catch (HttpOperationException exception)
            {
                if (exception.Response?.Content == null)
                    throw;

                context.Status = JsonConvert.DeserializeObject<ExperimentStatus>(exception.Response.Content);
            }
        }
        #endregion

        #region GetExperimentAsync
        public async Task<ExperimentResource> GetExperimentAsync(ExperimentContext context, CancellationToken cancellationToken)
        {
            try
            {
                var cr = await kubeClient.GetNamespacedCustomObjectWithHttpMessagesAsync(
                    CustomResource.Group,
                    CustomResource.Version,
                    context.GetNamespace(),
                    ExperimentResource.Plural,
                    context.GetExperimentResourceName(),
                    null,
                    cancellationToken);
                var exp = SafeJsonConvert.DeserializeObject<ExperimentResource>(cr.Body.ToString());
                context.Status = exp.Status;
                context.Status.Code = exp.Status.Code.GetValueOrDefault(200);
                return exp;
            }
            catch (HttpOperationException exception)
            {
                if (exception.Response?.Content == null)
                    throw;

                logger.TraceInformation(exception.Response.Content);
                context.Status = JsonConvert.DeserializeObject<ExperimentStatus>(exception.Response.Content);
                return null;
            }
        }
        #endregion
    }
}
