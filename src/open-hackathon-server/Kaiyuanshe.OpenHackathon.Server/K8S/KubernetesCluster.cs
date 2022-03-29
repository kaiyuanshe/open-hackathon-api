﻿using k8s;
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
        Task DeleteTemplateAsync(string templateResourceName, CancellationToken cancellationToken);
        Task CreateOrUpdateExperimentAsync(ExperimentContext context, CancellationToken cancellationToken);
        Task UpdateExperimentAsync(ExperimentContext context, CancellationToken cancellationToken);
        Task<ExperimentResource> GetExperimentAsync(ExperimentContext context, CancellationToken cancellationToken);
        Task<IEnumerable<ExperimentResource>> ListExperimentsAsync(string hackathonName, string templateId = null, CancellationToken cancellationToken = default);
        Task DeleteExperimentAsync(ExperimentContext context, CancellationToken cancellationToken);
        Task DeleteExperimentAsync(string experimentResourceName, CancellationToken cancellationToken);
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
                    CustomResourceDefinition.Group,
                    CustomResourceDefinition.Version,
                    customResource.Metadata.NamespaceProperty ?? "default",
                    CustomResourceDefinition.Plurals.Templates,
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
                    CustomResourceDefinition.Group,
                    CustomResourceDefinition.Version,
                    Namespaces.Default,
                    CustomResourceDefinition.Plurals.Templates,
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
                    CustomResourceDefinition.Group,
                    CustomResourceDefinition.Version,
                    Namespaces.Default,
                    CustomResourceDefinition.Plurals.Templates,
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
                var labelSelector = $"{Labels.HackathonName}={hackathonName}";
                var listResp = await kubeClient.ListNamespacedCustomObjectWithHttpMessagesAsync(
                    CustomResourceDefinition.Group,
                    CustomResourceDefinition.Version,
                    Namespaces.Default,
                    CustomResourceDefinition.Plurals.Templates,
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
                    CustomResourceDefinition.Group,
                    CustomResourceDefinition.Version,
                    Namespaces.Default,
                    CustomResourceDefinition.Plurals.Templates,
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

        public async Task DeleteTemplateAsync(string templateResourceName, CancellationToken cancellationToken)
        {
            try
            {
                await kubeClient.DeleteNamespacedCustomObjectWithHttpMessagesAsync(
                    CustomResourceDefinition.Group,
                    CustomResourceDefinition.Version,
                    Namespaces.Default,
                    CustomResourceDefinition.Plurals.Templates,
                    templateResourceName,
                    cancellationToken: cancellationToken);
            }
            catch (HttpOperationException exception)
            {
                logger.TraceInformation(exception.Response.Content);
                if (exception.Response.StatusCode != HttpStatusCode.NotFound)
                {
                    // ignore 404 only
                    throw;
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
                // apply patch
                await UpdateExperimentAsync(context, cancellationToken);
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
                    CustomResourceDefinition.Group,
                    CustomResourceDefinition.Version,
                    customResource.Metadata.NamespaceProperty ?? "default",
                    CustomResourceDefinition.Plurals.Experiments,
                    cancellationToken: cancellationToken);
                logger.TraceInformation($"CreateExperimentAsync. Status: {resp.Response.StatusCode}, reason: {resp.Response.ReasonPhrase}");
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

        #region UpdateExperimentAsync
        public async Task UpdateExperimentAsync(ExperimentContext context, CancellationToken cancellationToken)
        {
            var patch = context.BuildPatch();
            try
            {
                var resp = await kubeClient.PatchNamespacedCustomObjectWithHttpMessagesAsync(
                    patch,
                    CustomResourceDefinition.Group,
                    CustomResourceDefinition.Version,
                    Namespaces.Default,
                    CustomResourceDefinition.Plurals.Experiments,
                    context.GetExperimentResourceName(),
                    cancellationToken: cancellationToken);

                logger.TraceInformation($"UpdateExperimentAsync. Status: {resp.Response.StatusCode}, reason: {resp.Response.ReasonPhrase}");
                context.Status = new ExperimentStatus
                {
                    Code = (int)resp.Response.StatusCode,
                    Reason = resp.Response.ReasonPhrase,
                };
            }
            catch (HttpOperationException exception)
            {
                if (exception.Response?.Content == null)
                    throw;

                logger.TraceError($"UpdateExperimentAsync: {exception.Message}", exception);
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
                    CustomResourceDefinition.Group,
                    CustomResourceDefinition.Version,
                    Namespaces.Default,
                    CustomResourceDefinition.Plurals.Experiments,
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

        #region ListExperimentsAsync
        public async Task<IEnumerable<ExperimentResource>> ListExperimentsAsync(string hackathonName, string templateId = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var labelSelector = $"{Labels.HackathonName}={hackathonName}";
                if (!string.IsNullOrWhiteSpace(templateId))
                {
                    labelSelector += $",{Labels.TemplateId}={templateId}";
                }
                var listResp = await kubeClient.ListNamespacedCustomObjectWithHttpMessagesAsync(
                    CustomResourceDefinition.Group,
                    CustomResourceDefinition.Version,
                    Namespaces.Default,
                    CustomResourceDefinition.Plurals.Experiments,
                    labelSelector: labelSelector,
                    cancellationToken: cancellationToken);
                var crl = SafeJsonConvert.DeserializeObject<CustomResourceList<ExperimentResource>>(listResp.Body.ToString());
                return crl.Items;
            }
            catch (HttpOperationException exception)
            {
                if (exception.Response?.Content == null)
                    throw;

                logger.TraceInformation(exception.Response.Content);
                return Array.Empty<ExperimentResource>();
            }
        }
        #endregion

        #region DeleteExperimentAsync
        public async Task DeleteExperimentAsync(ExperimentContext context, CancellationToken cancellationToken)
        {
            try
            {
                await kubeClient.DeleteNamespacedCustomObjectWithHttpMessagesAsync(
                    CustomResourceDefinition.Group,
                    CustomResourceDefinition.Version,
                    Namespaces.Default,
                    CustomResourceDefinition.Plurals.Experiments,
                    context.GetExperimentResourceName(),
                    cancellationToken: cancellationToken);
                context.Status = new ExperimentStatus
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
                    context.Status = JsonConvert.DeserializeObject<ExperimentStatus>(exception.Response.Content);
                }
                else
                {
                    context.Status = new ExperimentStatus
                    {
                        Code = 204,
                        Status = "success"
                    };
                }
            }
        }

        public async Task DeleteExperimentAsync(string experimentResourceName, CancellationToken cancellationToken)
        {
            try
            {
                await kubeClient.DeleteNamespacedCustomObjectWithHttpMessagesAsync(
                    CustomResourceDefinition.Group,
                    CustomResourceDefinition.Version,
                    Namespaces.Default,
                    CustomResourceDefinition.Plurals.Experiments,
                    experimentResourceName,
                    cancellationToken: cancellationToken);
            }
            catch (HttpOperationException exception)
            {
                logger.TraceInformation(exception.Response.Content);
                if (exception.Response.StatusCode != HttpStatusCode.NotFound)
                {
                    // ignore 404
                    throw;
                }
            }
        }
        #endregion  
    }
}
