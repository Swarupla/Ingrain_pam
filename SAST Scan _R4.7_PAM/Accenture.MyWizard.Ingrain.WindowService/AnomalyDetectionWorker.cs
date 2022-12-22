using Accenture.MyWizard.Ingrain.DataModels.InferenceEngineModel;
using Accenture.MyWizard.Ingrain.WindowService.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using WINSERVICEMODELS = Accenture.MyWizard.Ingrain.WindowService.Models;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
using Accenture.MyWizard.Ingrain.WindowService.Models;
using Accenture.MyWizard.SelfServiceAI.WindowService.Models;
using Accenture.MyWizard.SelfServiceAI.WindowService.Service;
using Accenture.MyWizard.Shared.Helpers;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using RestSharp;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Accenture.MyWizard.Ingrain.DataModels.Models;
using Accenture.MyWizard.Ingrain.DataModels.AICore;
using MongoDB.Bson.Serialization;
using AIModels = Accenture.MyWizard.Ingrain.DataModels.AICore;
using Accenture.MyWizard.Cryptography.EncryptionProviders;
using CryptographyHelper = Accenture.MyWizard.Cryptography;

namespace Accenture.MyWizard.Ingrain.WindowService
{
    public class AnomalyDetectionWorker : IHostedService, IDisposable
    {
        // Mongo connection properties
        private DatabaseProvider databaseProvider;
        private IMongoDatabase _database;
        private string servicename = "Anomaly";
        //Timers
        System.Timers.Timer timer = new System.Timers.Timer();

        private AnomalyDetectionWService _AnomalyDetectionWService = null;
        private QueueManagerService _queueManagerService;

        private readonly WINSERVICEMODELS.AppSettings appSettings = null;
        public AnomalyDetectionWorker()
        {
            appSettings = AppSettingsJson.GetAppSettings().GetSection("AppSettings").Get<WINSERVICEMODELS.AppSettings>();
            //Mongo connection
            databaseProvider = new DatabaseProvider();
            var dataBaseName = MongoUrl.Create(appSettings.AnomalyDetectionCS).DatabaseName;
            MongoClient mongoClient = databaseProvider.GetDatabaseConnection(servicename);
            _database = mongoClient.GetDatabase(dataBaseName);
            _AnomalyDetectionWService = new AnomalyDetectionWService();
            _queueManagerService = new QueueManagerService();
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionWorker), "OnStart", "AnomalyDetection WINDOWS SERVICE STARTED -" + DateTime.Now.ToString(), string.Empty, string.Empty, string.Empty, string.Empty);
            timer.Elapsed += new ElapsedEventHandler(OnElapsedTime);
            timer.Interval = Convert.ToDouble(appSettings.timeInterval);
            timer.Enabled = true;
            timer.AutoReset = false;

            return Task.CompletedTask;
        }
        private void OnElapsedTime(object source, ElapsedEventArgs e)
        {
            InvokeADServiceRequests();
        }
        public void InvokeADServiceRequests()
        {
            try
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionWService), "InvokeADServiceRequests Start", string.Empty, default(Guid) , string.Empty, string.Empty, string.Empty, string.Empty);
                List<IngrainRequestQueue> ingrainRequestQueue = _AnomalyDetectionWService.GetQueueRequests();
                if (ingrainRequestQueue != null)
                {
                    if (ingrainRequestQueue.Count > 0)
                        _queueManagerService.RequestBatchLimitInsert(ingrainRequestQueue.Count, CONSTANTS.ADRequestBatchLimitMonitor, "Anomaly");

                    foreach (var request in ingrainRequestQueue)
                    {
                        _queueManagerService.UpdateADTrainingQueueStatus();
                        string status = _queueManagerService.GetADTrainingQueueStatus();
                        if (status == "Available")
                        {
                            _AnomalyDetectionWService.InvokeADRequest(request);
                        }
                        else
                        {
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionWService), "InvokeADServiceRequests- Queue is Stuck due to Occupied/In-Progress records", string.Empty, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty);
                        }
                    }
                }
            }
            catch (Exception ex)
            {

                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InferenceEngineWorker), nameof(InvokeADServiceRequests), ex.Message + "- STACKTRACE - " + ex.StackTrace + ex.InnerException, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
            timer.Start();
        }
        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
        public void Dispose()
        {
            timer?.Dispose();
        }
    }
}
