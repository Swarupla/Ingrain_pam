using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Reflection;
using Accenture.MyWizard.Cryptography;
using Microsoft.Extensions.Hosting;
using System;

namespace Accenture.MyWizard.Ingrain.UI.DotNetWrapper
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var repo = log4net.LogManager.CreateRepository(Assembly.GetEntryAssembly(),
             typeof(log4net.Repository.Hierarchy.Hierarchy));
            var assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            log4net.Config.XmlConfigurator.Configure(repo, new FileInfo(Path.Combine(assemblyFolder, "log4net.config")));
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(String[] args)
        {
            ////Comment below code if PAM Deployment
            //return Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
            //.ConfigureWebHostDefaults(webBuilder =>
            //{
            //    webBuilder.UseUrls(AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("hostingUrl").Value)
            //    .UseStartup<Startup>();
            //})
            //.UseCryptography("appsettings")
            //.UseDefaultServiceProvider((context, options) =>
            //{
            //    options.ValidateOnBuild = true;
            //});

            //Uncomment below code if PAM Deployment

            return Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder//.UseUrls(AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("hostingUrl").Value)
                .UseStartup<Startup>();
            })
            .UseCryptography("appsettings")
            .UseDefaultServiceProvider((context, options) =>
            {
                options.ValidateOnBuild = true;
            });

        }

        public static class AppSettingsJson
        {
            public static IConfigurationRoot GetAppSettings()
            {
                string applicationExeDirectory = ApplicationExeDirectory();

                var builder = new ConfigurationBuilder()
                .SetBasePath(applicationExeDirectory)
                .UseCryptography("appsettings")
                .AddJsonFile("appsettings.json");

                return builder.Build();
            }

            private static string ApplicationExeDirectory()
            {
                var location = System.Reflection.Assembly.GetExecutingAssembly().Location;
                var appRoot = Path.GetDirectoryName(location);

                return appRoot;
            }
        }
    }
}
