using Autofac;
using Kaiyuanshe.OpenHackathon.Server.Biz;
using Kaiyuanshe.OpenHackathon.Server.Storage.BlobContainers;
using Kaiyuanshe.OpenHackathon.Server.Storage.Tables;
using System;
using System.Linq;
using System.Reflection;

namespace Kaiyuanshe.OpenHackathon.Server.DependencyInjection
{
    public static class ContainerBuilderExtension
    {
        /// <summary>
        /// Register all un-abstract sub types as self without interface support.
        /// Generic not supported. 
        /// </summary>
        public static void RegisterSubTypes(this ContainerBuilder builder, Type abstractType, params Assembly[] assemblies)
        {
            if (abstractType.IsGenericType)
                return;

            builder.RegisterTypes(abstractType.SubTypes(assemblies)).PropertiesAutowired().SingleInstance();
        }

        /// <summary>
        /// Register all un-abstract sub types as direct interface. 
        /// Not registered if no direct interface.
        /// DONT use this method if any of its direct interfaces has multiple implementations.
        /// </summary>
        public static void RegisterSubTypesAsDirectInterfaces(this ContainerBuilder builder, Type abstractType, params Assembly[] assemblies)
        {
            var subTypes = abstractType.IsGenericType ? abstractType.GenericSubTypes(assemblies) : abstractType.SubTypes(assemblies);
            foreach (var subType in subTypes)
            {
                var directInterfaces = subType.GetInterfaces(false);
                if (directInterfaces.Count() > 0)
                {
                    builder.RegisterType(subType).AsSelf().SingleInstance().PropertiesAutowired().As(directInterfaces.ToArray());
                }
            }
        }

        public static void RegisterAzureTablesV2(this ContainerBuilder builder)
        {
            builder.RegisterSubTypesAsDirectInterfaces(typeof(IAzureTableV2<>));
        }

        public static void RegisterAzureBlobContainersV2(this ContainerBuilder builder)
        {
            builder.RegisterSubTypesAsDirectInterfaces(typeof(IAzureBlobContainerV2));
        }

        public static void RegisterManagementClients(this ContainerBuilder builder)
        {
            builder.RegisterSubTypesAsDirectInterfaces(typeof(IManagementClient));
        }
    }
}
