using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Reflection;
using System.Xml;
using Accenture.MyWizard.Cryptography;
using System;
using Microsoft.Extensions.Hosting;

namespace Accenture.MyWizard.Ingrain.WebService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            XmlDocument log4netConfig = new XmlDocument();
            log4netConfig.Load(File.OpenRead("log4net.config"));
            var repo = log4net.LogManager.CreateRepository(Assembly.GetEntryAssembly(),
                       typeof(log4net.Repository.Hierarchy.Hierarchy));
            log4net.Config.XmlConfigurator.Configure(repo, log4netConfig["log4net"]);
            CreateHostBuilder(args).Build().Run();
        }
      

        public static IHostBuilder CreateHostBuilder(String[] args)
        {
            return Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                ////Comment below code if PAM Deployment
                //webBuilder.ConfigureKestrel(options =>
                //{
                //    options.Limits.MaxRequestBodySize = null;
                //    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(10);
                //})//.UseUrls(AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("hostingUrl").Value)
                //.UseStartup<Startup>();

                //Uncomment below code if PAM Deployment

                webBuilder.ConfigureKestrel(options =>
                {
                    options.Limits.MaxRequestBodySize = null;
                    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(10);
                })//.UseUrls(AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("hostingUrl").Value) //TODO: OCB- Check commenting for PAM
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
