using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Reflection;

namespace Kaiyuanshe.OpenHackathon.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            LoadAssemblies();
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging(loggerBuilder =>
                {
                })
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });

        public static void LoadAssemblies()
        {
            var assemblies = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, ".resources.dll", SearchOption.AllDirectories);
            foreach (var assembly in assemblies)
            {
                Assembly.Load(assembly);
            }
        }
    }
}
