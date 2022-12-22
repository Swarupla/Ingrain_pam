using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Accenture.MyWizard.Cryptography;
using Microsoft.Extensions.Hosting.Systemd;
using Microsoft.Extensions.Hosting.WindowsServices;
using System.Reflection;

namespace Accenture.MyWizard.Ingrain.WindowService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            // var mappingAppCoreModule = new MAPPINGAPPCORE.Module();
            IHostBuilder builder = Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {                    
                    services.AddHostedService<Worker>();
                    services.AddHostedService<TrainModelsWorker>();
                    //services.AddHostedService<ConstraintsWorker>();
                    services.AddHostedService<AIServiceWorker>();
                    services.AddHostedService<InferenceEngineWorker>();
                    services.AddHostedService<AssetUsageTrackerWorker>();
                   // services.AddHostedService<AnomalyDetectionWorker>();
                });

            if(WindowsServiceHelpers.IsWindowsService())
            {
                builder.UseWindowsService();
            }
            if(SystemdHelpers.IsSystemdService())
            {
                builder.UseSystemd();
            }

            return builder;
        }

   }

    public static class AppSettingsJson
    {
        public static IConfigurationRoot GetAppSettings()
        {
            var repo = log4net.LogManager.CreateRepository(Assembly.GetEntryAssembly(),
             typeof(log4net.Repository.Hierarchy.Hierarchy));
            var assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            log4net.Config.XmlConfigurator.Configure(repo, new FileInfo(Path.Combine(assemblyFolder, "log4net.config")));
           
            LOGGING.LogManager.SetLogFolderPath(string.Empty);
            string applicationExeDirectory = ApplicationExeDirectory();

            var builder = new ConfigurationBuilder()
            .SetBasePath(applicationExeDirectory)
            //.UseCryptography("appsettings")
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
