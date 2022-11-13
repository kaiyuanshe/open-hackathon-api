using System.Net.Http;
using Autofac;
using Kaiyuanshe.OpenHackathon.Server.Cache;
using Kaiyuanshe.OpenHackathon.Server.CronJobs;
using Kaiyuanshe.OpenHackathon.Server.K8S;
using Kaiyuanshe.OpenHackathon.Server.ResponseBuilder;
using Kaiyuanshe.OpenHackathon.Server.Storage;
using Kaiyuanshe.OpenHackathon.Server.Storage.Mutex;
using Quartz;
using Quartz.Spi;

namespace Kaiyuanshe.OpenHackathon.Server.DependencyInjection
{
    public class HackathonDefaultModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // Storage
            builder.RegisterType<StorageCredentialProvider>().As<IStorageCredentialProvider>().PropertiesAutowired().SingleInstance();
            builder.RegisterAzureTablesV2();
            builder.RegisterAzureBlobContainersV2();
            builder.RegisterType<StorageContext>().As<IStorageContext>().PropertiesAutowired().SingleInstance();
            builder.RegisterType<TraceIdHttpPipelinePolicyFactory>().As<ITraceIdHttpPipelinePolicyFactory>().PropertiesAutowired().SingleInstance();
            builder.RegisterType<MutexProvider>().As<IMutexProvider>().PropertiesAutowired().SingleInstance();

            // Biz
            builder.RegisterManagementClients();

            // Kubernetes
            builder.RegisterType<KubernetesConfigProvider>().As<IKubernetesConfigProvider>().PropertiesAutowired().SingleInstance();
            builder.RegisterType<KubernetesClusterFactory>().As<IKubernetesClusterFactory>().PropertiesAutowired().SingleInstance();

            // Response
            builder.RegisterType<DefaultResponseBuilder>().As<IResponseBuilder>().PropertiesAutowired().SingleInstance();

            // Cache
            builder.RegisterType<CacheProviderFactory>().As<ICacheProviderFactory>().PropertiesAutowired().SingleInstance();
            builder.Register<ICacheProvider>(container => container.Resolve<ICacheProviderFactory>().CreateCacheProvider()).SingleInstance();

            // HttpClient
            // builder.RegisterType<HttpClientHelper>().As<IHttpClientHelper>().SingleInstance().PropertiesAutowired();
            // builder.Register<HttpClient>(container => container.Resolve<IHttpClientFactory>().CreateClient()).SingleInstance();

            // CronJob
            builder.RegisterTypes(typeof(ICronJob).SubTypes()).SingleInstance().PropertiesAutowired();
            builder.RegisterType<CronJobFactory>().AsSelf().As<IJobFactory>().SingleInstance().PropertiesAutowired();
            builder.RegisterType<CronJobSchedulerFactory>().AsSelf().As<ISchedulerFactory>().SingleInstance().PropertiesAutowired();
            builder.RegisterType<CronJobScheduler>().As<ICronJobScheduler>().SingleInstance().PropertiesAutowired();
        }
    }
}
