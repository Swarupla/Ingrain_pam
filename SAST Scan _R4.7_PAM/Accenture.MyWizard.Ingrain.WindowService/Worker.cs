#region                NameSpaces
using Accenture.MyWizard.Ingrain.WindowService.Models;
using Accenture.MyWizard.Ingrain.WindowService.Services;
using Accenture.MyWizard.SelfServiceAI.WindowService.Models;
using Accenture.MyWizard.SelfServiceAI.WindowService.Service;
using Accenture.MyWizard.Shared.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using WINSERVICEMODELS = Accenture.MyWizard.Ingrain.WindowService.Models;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
using Accenture.MyWizard.Ingrain.DataModels.Models;
using System.Collections.Generic;
using Accenture.MyWizard.Ingrain.DataModels.AICore;
using MongoDB.Bson.Serialization;
using AIModels = Accenture.MyWizard.Ingrain.DataModels.AICore;
using Accenture.MyWizard.Cryptography.EncryptionProviders;
using CryptographyHelper = Accenture.MyWizard.Cryptography;
using OfficeOpenXml.Style;
#endregion

namespace Accenture.MyWizard.Ingrain.WindowService
{
    public class Worker : IHostedService, IDisposable //BackgroundService
    {
        #region Private Members
        //Timers
        System.Timers.Timer timer = new System.Timers.Timer();
        System.Timers.Timer SPPGenericAPICallTimer = new System.Timers.Timer();
        System.Timers.Timer usageTimer = new System.Timers.Timer();
        System.Timers.Timer retrainTimer = new System.Timers.Timer();
        System.Timers.Timer ArchivalPurgingTimer = new System.Timers.Timer();
        System.Timers.Timer spaTimer = new System.Timers.Timer();
        System.Timers.Timer spaTimerTerminateModels = new System.Timers.Timer();
        private string SPE_appId = "fa36e811-a59f-48c0-94a6-9a7ffc8bc8ab";
        //end
        // Mongo connection properties
        private DatabaseProvider databaseProvider;
        private IMongoDatabase _database;
        private IMongoCollection<IngrainRequestQueue> _collection;
        private IMongoCollection<BsonDocument> _deployModelCollection;
        //end
        //Models
        private readonly WINSERVICEMODELS.AppSettings appSettings = null;
        private ProcessStartInfo _start;
        private InstaAutoRetrainService _instaAutoRetrainService;
        private InstaRegressionAutoTrainService _instaRegressionAutoTrainService;
        private AssetUsageTrackingService _assetUsageTrackingService;
        private SimulationGenericAPIService _genericAPICallingService;
        private QueueManagerService _queueManagerService;
        InstaRetrain instaRetrain;
        RegressionRetrain regressionRetrain = null;
        private AIServicesRetrain AIServices;
        InstaRetrain modelMonitorRetrain;
        LogAutoTrainedFeatures autoTrainedFeatures = null;
        private DeployModelService _deployModelService;
        LogAIServiceAutoTrain logAIServiceAutoTrain = null;

        //end
        private int counter = 0;

        // private properties
        private string _pythonExe;
        private string _pythonPy;
        private string _workingDirectory;
        private int _pythonProgressStatus;
        private string _pythonPYPath;
        int lastHour = 0;
        string lastUpdateHour;
        //end

        private System.ComponentModel.IContainer components = null;
        private IngrainResponseData _IngrainResponseData;
        CPUMemoryUtilizeCount cPUMemoryCount;


        static CryptographyHelper.Utility.CryptographyUtility CryptographyUtility = null;
        #endregion

        public Worker()
        {
            appSettings = AppSettingsJson.GetAppSettings().GetSection("AppSettings").Get<WINSERVICEMODELS.AppSettings>();
            //Mongo connection
            databaseProvider = new DatabaseProvider();
            string connectionString = appSettings.connectionString;
            var dataBaseName = MongoUrl.Create(connectionString).DatabaseName;
            MongoClient mongoClient = databaseProvider.GetDatabaseConnection();
            _database = mongoClient.GetDatabase(dataBaseName);
            _collection = _database.GetCollection<IngrainRequestQueue>("SSAI_IngrainRequests");
            _deployModelCollection = _database.GetCollection<BsonDocument>("SSAI_DeployedModels");
            //end

            counter = 0;

            if (appSettings.Environment == CONSTANTS.PAMEnvironment || appSettings.IsDockerPlatform)
            {
                _pythonExe = appSettings.pythonExe;
            }
            else
            {
                _pythonExe = Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory + appSettings.pythonExe);
            }

            _pythonPy = appSettings.pythonPy;

            _workingDirectory = AppDomain.CurrentDomain.BaseDirectory;//appSettings.GetSection("AppSettings").GetSection("pythonWorkingDirectory").Value;
            _pythonProgressStatus = Convert.ToInt32(appSettings.pythonProgressStatus);
            _pythonPYPath = Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory + _pythonPy);

            _instaAutoRetrainService = new InstaAutoRetrainService(databaseProvider);
            _instaRegressionAutoTrainService = new InstaRegressionAutoTrainService(databaseProvider);
            instaRetrain = new InstaRetrain();
            regressionRetrain = new RegressionRetrain();
            _assetUsageTrackingService = new AssetUsageTrackingService();
            _genericAPICallingService = new SimulationGenericAPIService();
            _queueManagerService = new QueueManagerService();
            _deployModelService = new DeployModelService();

            _start = new ProcessStartInfo();


            _deployModelCollection = _database.GetCollection<BsonDocument>("SSAI_DeployedModels");

            CryptographyUtility = new CryptographyHelper.Utility.CryptographyUtility();
            AIServices = new AIServicesRetrain(databaseProvider);
            cPUMemoryCount = new CPUMemoryUtilizeCount();
            _IngrainResponseData = new IngrainResponseData();
            //AutoTrainModel2();
            modelMonitorRetrain = new InstaRetrain();
            autoTrainedFeatures = new LogAutoTrainedFeatures();
            logAIServiceAutoTrain = new LogAIServiceAutoTrain();
        }

        /// <summary>
        /// Starting the Windows Service
        /// </summary>
        /// <param name="args"></param>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            lastUpdateHour = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
            //Debugger.Launch();
            var filterBuilder = Builders<IngrainRequestQueue>.Filter;
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "StartAsync", "Worker Start at " + DateTime.Now.ToString(), string.Empty, string.Empty, string.Empty, string.Empty);

            //LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "spe delete start", "Windows service spe delete started -" + DateTime.Now.ToString());
            //var spe_projection = Builders<IngrainRequestQueue>.Projection.Include(CONSTANTS.CorrelationId).Exclude(CONSTANTS.Id);
            //var spe_Filter = filterBuilder.Eq(CONSTANTS.Status, "null") & filterBuilder.Eq("Function", "AutoTrain") & filterBuilder.Eq(CONSTANTS.TemplateUseCaseID, appSettings.SPETemplateUseCaseID);
            //var spe_ingrainRequests = _collection.Find(spe_Filter).Project<IngrainRequestQueue>(spe_projection).Sort(Builders<IngrainRequestQueue>.Sort.Ascending("CreatedOn")).ToList();
            //if (spe_ingrainRequests.Count > 0)
            //{
            //    for (int i = 0; i < spe_ingrainRequests.Count; i++)AI Autoretrain Manual Trigger start
            //    {
            //        string correlationId = spe_ingrainRequests[i].CorrelationId;
            //        delete_backUpModelSPP(correlationId);
            //    }
            //}
            //LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "spe delete ended", "Windows service spe delete ended -" + DateTime.Now.ToString());

            LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "OnStart", "Windows service started -" + DateTime.Now.ToString(), string.Empty, string.Empty, string.Empty, string.Empty);
            // Window service start - check InProgress , Occupied and New request - Update  to E -Start

            var projection = Builders<IngrainRequestQueue>.Projection.Exclude(CONSTANTS.Id);
            var Filter = filterBuilder.Eq(CONSTANTS.RequestStatus, appSettings.requestInProgress) | filterBuilder.Eq(CONSTANTS.RequestStatus, "Occupied") | filterBuilder.Eq(CONSTANTS.RequestStatus, "New") | filterBuilder.Eq("Status", "P");
            var ingrainRequests = _collection.Find(Filter).Project<IngrainRequestQueue>(projection).Sort(Builders<IngrainRequestQueue>.Sort.Ascending("CreatedOn")).ToList();
            var MappingCollection = _database.GetCollection<PublicTemplateMapping>(CONSTANTS.PublicTemplateMapping);
            var Builder = Builders<PublicTemplateMapping>.Filter;

            SetWorkerServiceStatus();

            Console.WriteLine("SetWorkerServiceStatus logic executed");

            try
            {
                    //exclude for devtest env
                    if (!appSettings.IngrainAPIUrl.Contains("devtest"))
                        {
                            this.DecryptTemplateData();
                        }
                    Console.WriteLine("DecryptTemplateData logic executed");

                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "OnStart", "WINDOWS SERVICE STARTED -" + DateTime.Now.ToString(), string.Empty, string.Empty, string.Empty, string.Empty);
                    lastHour = 1;
                    timer.Elapsed += new ElapsedEventHandler(OnElapsedTime);
                    timer.Interval = Convert.ToDouble(appSettings.timeInterval);
                    timer.Enabled = true;
                    timer.AutoReset = false;
                    Console.WriteLine("OnElapsedTime timer registered");
                    //Triggers for every 2 hours
                    spaTimer.Elapsed += new ElapsedEventHandler(OnElapsedNotificationUpdate);
                    spaTimer.Interval = Convert.ToDouble(appSettings.NotificationTimeInterval);
                    spaTimer.Enabled = true;

                    //Trigers for every 3 hours
                    spaTimerTerminateModels.Elapsed += new ElapsedEventHandler(OnElapsedTerminateModels);
                    spaTimerTerminateModels.Interval = Convert.ToDouble(appSettings.TerminateModelElapsedTimeInterval);
                    spaTimerTerminateModels.Enabled = true;

                    //For AutoRetrain of SPA/Entity models
                    if (appSettings.IsRetrainModelEnabled)
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "OnStart", "OnElapsedRetrainModels Started -" + DateTime.Now.ToString(), string.Empty, string.Empty, string.Empty, string.Empty);
                        retrainTimer.Elapsed += new ElapsedEventHandler(OnElapsedAutoRetrainModels);
                        retrainTimer.Interval = Convert.ToDouble(appSettings.AutoRetrainTimeInterval);
                        retrainTimer.Enabled = true;
                        retrainTimer.AutoReset = true;

                        //Thread retrainErrorModels = new Thread(AutoRetrainModels);
                        //retrainErrorModels.Start();
                        //LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "OnStart", "RetrainModels Started -" + DateTime.Now.ToString(), string.Empty, string.Empty, string.Empty, string.Empty);
                    }

                    if (appSettings.SPPSimulation)
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "CALLSIMULATIONGENERICPREDICTIONREQUEST", "CALLSIMULATIONGENERICPREDICTIONREQUEST STARTED" + DateTime.Now.ToString(), string.Empty, string.Empty, string.Empty, string.Empty);
                        SPPGenericAPICallTimer.Elapsed += new ElapsedEventHandler(CallSimulationGenericPredictionRequest);
                        SPPGenericAPICallTimer.Interval = Convert.ToDouble(appSettings.SPPIntervalForGenericAPICall);
                        SPPGenericAPICallTimer.Enabled = true;
                        SPPGenericAPICallTimer.AutoReset = true;
                    }

                    if (appSettings.IsAssetTrackingRequired.ToLower() == "true")
                    {
                        usageTimer.Elapsed += new ElapsedEventHandler(OnElapsedUsageTracking);
                        usageTimer.Interval = Convert.ToDouble(appSettings.UsageTimeInterval);
                        usageTimer.Enabled = true;
                    }

                    //Models failed with WS restart error. After WS retart Status changed to error
                    if (appSettings.IsRetrainErrorModelsWithService)
                    {
                        //RetrainErrorModelWithWindowsService();
                        usageTimer.Elapsed += new ElapsedEventHandler(OnElapsedRetrainErrorModels);
                        usageTimer.Interval = Convert.ToDouble(appSettings.RetrainErrorModelsTime_TimeInterval);
                        usageTimer.Enabled = true;
                    }

                    //Archival purging 
                    int archivetimeinterval = appSettings.ArchiveTimeInterval * 24 * 1000; // in milli sec
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "OnStart", "Archival purging Timer Set up -" + DateTime.Now.ToString(), string.Empty, string.Empty, string.Empty, string.Empty);
                    ArchivalPurgingTimer.Elapsed += new ElapsedEventHandler(OnElapsedArchivalPurging);
                    ArchivalPurgingTimer.Interval = Convert.ToDouble(archivetimeinterval);//900000 set to 15 mint for Testing purpose//3600000:1hr
                    ArchivalPurgingTimer.Enabled = true;
                    ArchivalPurgingTimer.AutoReset = false;
                }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(Worker), nameof(StartAsync), ex.StackTrace + "--" + ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Given ElapsedTime 1 Sec Time Interval
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void OnElapsedTime(object source, ElapsedEventArgs e)
        {
            try
            {
                lastUpdateHour = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                // Sttaus Update to E - for other than C and E status for more than 3 hours  - Start
                var filterBuilder = Builders<IngrainRequestQueue>.Filter;
                var projection = Builders<IngrainRequestQueue>.Projection.Exclude(CONSTANTS.Id);
                var Filter = (filterBuilder.Ne(CONSTANTS.Status, "C") & filterBuilder.Ne(CONSTANTS.Status, "E")) & (filterBuilder.Eq("pageInfo", "AutoTrain") | filterBuilder.Eq("pageInfo", "TrainAndPredict"));
                var ingrainRequests = _collection.Find(Filter).Project<IngrainRequestQueue>(projection).Sort(Builders<IngrainRequestQueue>.Sort.Ascending("CreatedOn")).ToList();
                if (ingrainRequests.Count > 0)
                {
                    try
                    {
                        foreach (var item in ingrainRequests)
                        {
                            string createdOn = item.CreatedOn;
                            DateTime createdOnDateFormat = DateTime.Parse(createdOn);
                            DateTime lastUpdateHourDateFormat = DateTime.Parse(lastUpdateHour);
                            var hourDiffFromCurrent = (lastUpdateHourDateFormat - createdOnDateFormat).TotalHours;
                            var hourDiffFromCreate = (createdOnDateFormat - lastUpdateHourDateFormat).TotalHours;
                            var MappingCollection = _database.GetCollection<PublicTemplateMapping>(CONSTANTS.PublicTemplateMapping);
                            var Builder = Builders<PublicTemplateMapping>.Filter;
                            var Projection1 = Builders<PublicTemplateMapping>.Projection.Exclude("_id");
                            var filter = Builder.Eq(CONSTANTS.ApplicationID, item.AppID) & Builder.Eq(CONSTANTS.UsecaseID, item.TemplateUseCaseID);
                            var templatedata = MappingCollection.Find(filter).Project<PublicTemplateMapping>(Projection1).FirstOrDefault();
                            if (templatedata != null)
                            {
                                var timeOutValue = templatedata.TimeOutValue;
                                if (!string.IsNullOrEmpty(timeOutValue))
                                {
                                    if (templatedata.UsecaseID == item.TemplateUseCaseID && (hourDiffFromCurrent >= int.Parse(timeOutValue) || hourDiffFromCreate >= int.Parse(timeOutValue)))
                                    {
                                        var UpdateFilter = (filterBuilder.Ne(CONSTANTS.Status, "C") & filterBuilder.Ne(CONSTANTS.Status, "E")) & (filterBuilder.Eq("pageInfo", "AutoTrain") | filterBuilder.Eq("pageInfo", "TrainAndPredict")) & filterBuilder.Eq("CorrelationId", item.CorrelationId);
                                        var updateBuilder = Builders<IngrainRequestQueue>.Update;
                                        var update = updateBuilder.Set(CONSTANTS.Status, "E")
                                            .Set(CONSTANTS.Message, "Status changed to E because process is taking more than 3 hrs")
                                            // .Set(CONSTANTS.RequestStatus, "Status changed to E because process is taking more than 3 hrs")
                                            .Set(CONSTANTS.ModifiedOn, DateTime.Now.ToString(CONSTANTS.DateHoursFormat));

                                        //Killing the python processes which taking 3 hours.
                                        if (item.PythonProcessID > 0)
                                        {
                                            Process processes = Process.GetProcessById(item.PythonProcessID);
                                            processes.Kill();
                                        }

                                        var result = _collection.UpdateMany(UpdateFilter, update);

                                        IngrainResponseData CallBackResponse = new IngrainResponseData
                                        {
                                            CorrelationId = item.CorrelationId,
                                            Status = "E",
                                            Message = "Status changed to E because process is taking more than 3 hrs",
                                            ErrorMessage = "Status changed to E because process is taking more than 3 hrs",
                                        };

                                        if (templatedata != null)
                                        {
                                            item.ApplicationName = templatedata.ApplicationName;
                                        }
                                        GenericAutotrainService _genericAutotrainService = new GenericAutotrainService(databaseProvider);
                                        if (!(string.IsNullOrEmpty(item.AppURL)) && (!(string.IsNullOrEmpty(item.ApplicationName)) && (!string.IsNullOrEmpty(item.AppID))))
                                        {
                                            string callbackResonse = _genericAutotrainService.CallbackResponse(CallBackResponse, item.ApplicationName, item.AppURL, item.ClientId, item.DeliveryconstructId, item.AppID, item.TemplateUseCaseID, item.RequestId, null, item.CreatedByUser);
                                        }
                                        else
                                        {
                                            _genericAutotrainService.CallBackErrorLog(CallBackResponse, item.ApplicationName, item.AppURL, null, item.ClientId, item.DeliveryconstructId, item.AppID, item.TemplateUseCaseID, item.RequestId, null, item.CreatedByUser);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (ArgumentException ex)
                    {
                        LOGGING.LogManager.Logger.LogErrorMessage(typeof(Worker), nameof(OnElapsedRetrainErrorModels), ex.StackTrace + "--" + ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                    }
                    catch (InvalidOperationException ex)
                    {
                        LOGGING.LogManager.Logger.LogErrorMessage(typeof(Worker), nameof(OnElapsedRetrainErrorModels), ex.StackTrace + "--" + ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                    }
                    catch (Exception ex)
                    {
                        LOGGING.LogManager.Logger.LogErrorMessage(typeof(Worker), nameof(OnElapsedRetrainErrorModels), ex.StackTrace + "--" + ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                    }
                }
                // Sttaus Update to E - for other than C and E status for more than 3 hrs  - end     
                if (appSettings.EnableAutoReTrain)
                {
                    Thread AIServices_AutoTrain = new Thread(AIServicesAutoTrain);
                    Thread Clustering_AutoTrain = new Thread(ClusteringAutoTrain);
                    Thread DeveloperPred_AutoTrain = new Thread(DeveloperPredAutoTrain);
                    Thread IASimilar_AtuTrain = new Thread(IASimilarModelAutoTrain);
                    Thread ScrumbanSimilar_AtuTrain = new Thread(AIScrumbanAutoTrain);
                    if (!string.IsNullOrEmpty(appSettings.AIManualTrigger) && appSettings.AIManualTrigger.ToLower() == "yes")
                    {
                        if (lastHour < DateTime.Now.Hour || (lastHour == 23 && DateTime.Now.Hour == 0))
                        {
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "AI Autoretrain Manual Trigger start", "AI Autoretrain Manual Trigger start -" + DateTime.Now.ToString(), string.Empty, string.Empty, string.Empty, string.Empty);
                            lastHour = DateTime.Now.Hour;
                            AIServices_AutoTrain.Start();
                        }
                    }
                    else
                    {
                        var appModelRunAI = appSettings.AIAutoTimeToRun + " " + appSettings.AIAutoTimePeriod;
                        if (appModelRunAI == DateTime.Now.ToString("hh:mm:ss tt"))
                        {
                            AIServices_AutoTrain.Start();
                            Clustering_AutoTrain.Start();
                            DeveloperPred_AutoTrain.Start();
                            IASimilar_AtuTrain.Start();
                            ScrumbanSimilar_AtuTrain.Start();
                        }
                    }
                    Thread modelMonitorThread = new Thread(ModelMonitorAutoRetrain);
                    if (!string.IsNullOrEmpty(appSettings.ManualTrigger) && appSettings.ManualTrigger.ToLower() == "yes")
                    {
                        if (lastHour < DateTime.Now.Hour || (lastHour == 23 && DateTime.Now.Hour == 0))
                        {
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "Model Monitor Manual Trigger start", "Model Monitor Manual Trigger start -" + DateTime.Now.ToString(), string.Empty, string.Empty, string.Empty, string.Empty);
                            lastHour = DateTime.Now.Hour;
                            modelMonitorThread.Start();
                        }
                    }
                    else
                    {
                        var modelMonitorRun = appSettings.MonitorTimeToRun + " " + appSettings.MonitorTimePeriod;
                        if (modelMonitorRun == DateTime.Now.ToString("hh:mm:ss tt"))
                        {
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "Model Monitor Scheduled start", "Model Monitor before start -" + DateTime.Now.ToString(), string.Empty, string.Empty, string.Empty, string.Empty);
                            modelMonitorThread.Start();
                        }
                    }
                }

                LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "OnElapsedTime", "triggering GetMessageRequestsTimeStamp Method ", string.Empty, string.Empty, string.Empty, string.Empty);
                Thread ingrainRequestThread = new Thread(GetMessageRequestsTimeStamp);
                ingrainRequestThread.Start();
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(Worker), nameof(OnElapsedTime), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
        }
        private void OnElapsedNotificationUpdate(object source, ElapsedEventArgs e)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "OnElapsedNotificationUpdate", "Update notification - START" + DateTime.Now.ToString(), string.Empty, string.Empty, string.Empty, string.Empty);

            //For models with Status:E will be notified through call if notificationsent is false(not notified previously)
            GenericAutotrainService genericAutotrainService = new GenericAutotrainService(databaseProvider);
            try
            {
                //Acknowledging the method called for every 3 hrs
                autoTrainedFeatures = new LogAutoTrainedFeatures();
                autoTrainedFeatures.FeatureName = "NotificationUpdate";
                autoTrainedFeatures.ModelList = new string[] { };
                autoTrainedFeatures.Sequence = 1;
                autoTrainedFeatures.ModelsCount = 0;
                autoTrainedFeatures.EndedOn = null;
                autoTrainedFeatures.LogId = Guid.NewGuid().ToString();
                autoTrainedFeatures.FunctionName = "OnElapsedNotificationUpdate - STARTED";
                autoTrainedFeatures.StartedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                InsertAutoTrainLog(autoTrainedFeatures);

                //LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "OnElapsedNotificationUpdate", "SPA Notification Update -" + DateTime.Now.ToString(), string.Empty, string.Empty, string.Empty, string.Empty);
                var filterBuilder = Builders<IngrainRequestQueue>.Filter;
                var projection = Builders<IngrainRequestQueue>.Projection.Exclude(CONSTANTS.Id);
                var Filter = (filterBuilder.Eq(CONSTANTS.Status, "E")) &
                    (filterBuilder.Ne("AppURL", CONSTANTS.Null) | filterBuilder.Ne("AppURL", CONSTANTS.BsonNull) | filterBuilder.Ne(x => x.AppURL, null)) &
                    filterBuilder.Eq("pageInfo", "TrainAndPredict") & filterBuilder.Lt("RetryCount", "3") &
                    (filterBuilder.Eq("SendNotification", CONSTANTS.Null) | filterBuilder.Eq("SendNotification", CONSTANTS.BsonNull) | filterBuilder.Eq(x => x.SendNotification, null) | filterBuilder.Eq(x => x.SendNotification, "Error")) &
                    (filterBuilder.Eq("IsNotificationSent", CONSTANTS.Null) | filterBuilder.Eq("IsNotificationSent", CONSTANTS.BsonNull) | filterBuilder.Eq(x => x.IsNotificationSent, null) | filterBuilder.Eq(x => x.IsNotificationSent, "false"));
                //filterBuilder.Gte(x => x.CreatedOn, "2021-03-01") & filterBuilder.Lte(x => x.CreatedOn, DateTime.Now.Date.ToString());
                //MONGO QUERY: db.SSAI_IngrainRequests.find({"Status" : "E","pageInfo":"TrainAndPredict","AppURL":{$ne:"null"},SendNotification:{$in:["Error",null]},IsNotificationSent:{$in:["false",null]}}).sort({CreatedOn:-1})

                var ingrainRequests = _collection.Find(Filter).Project<IngrainRequestQueue>(projection).Sort(Builders<IngrainRequestQueue>.Sort.Ascending("CreatedOn")).Limit(appSettings.NotificationUpdateQueueLimit).ToList();
                if (ingrainRequests != null && ingrainRequests.Count > 0)
                {
                    //Log the CorrelationId's of Models to be updated
                    autoTrainedFeatures = new LogAutoTrainedFeatures();
                    autoTrainedFeatures.FeatureName = "NotificationUpdate";
                    var AppIntegCollection = _database.GetCollection<AppIntegration>(CONSTANTS.AppIntegration);
                    var CorrelationIds = ingrainRequests.Select(x => x.CorrelationId).ToList();
                    autoTrainedFeatures.ModelList = BsonSerializer.Deserialize<string[]>(JsonConvert.SerializeObject(CorrelationIds));
                    autoTrainedFeatures.Sequence = 2;
                    autoTrainedFeatures.ModelsCount = ingrainRequests.Count;
                    autoTrainedFeatures.EndedOn = null;
                    autoTrainedFeatures.LogId = Guid.NewGuid().ToString();
                    autoTrainedFeatures.FunctionName = "OnElapsedNotificationUpdate";
                    autoTrainedFeatures.StartedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                    InsertAutoTrainLog(autoTrainedFeatures);
                    var filterBuilder1 = Builders<AppIntegration>.Filter;
                    foreach (var request in ingrainRequests)
                    {
                        try
                        {
                            IngrainResponseData responseData = new IngrainResponseData
                            {
                                CorrelationId = request.CorrelationId,
                                Status = CONSTANTS.ErrorMessage,
                                Message = CONSTANTS.TrainingFailed,
                                ErrorMessage = request.Message
                            };

                            var appFilter = filterBuilder1.Eq(CONSTANTS.ApplicationID, request.AppID);
                            var appProjection = Builders<AppIntegration>.Projection.Exclude("_id");
                            var appData = AppIntegCollection.Find(appFilter).Project<AppIntegration>(appProjection).FirstOrDefault();
                            var response = genericAutotrainService.CallbackResponse(responseData, appData.ApplicationName, request.AppURL, request.ClientId, request.DeliveryconstructId, request.AppID, request.TemplateUseCaseID, request.RequestId, null, request.CreatedByUser, request.RetryCount);
                            if (response == "Error")
                            {
                                CustomAppsActivityLog log = new CustomAppsActivityLog()
                                {
                                    CorrelationId = request.CorrelationId,
                                    FeatureName = "OnElapsedNotificationUpdate",
                                    Status = "Error",
                                    ErrorMessage = "Unable to notify app using call back url",
                                    ErrorMethod = "OnElapsedNotificationUpdate foreach loop",
                                    CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                                    CreatedBy = "SYSTEM",
                                };

                                genericAutotrainService.InsertCustomAppsActivityLog(log);
                            }
                        }
                        catch (Exception ex)
                        {
                            LOGGING.LogManager.Logger.LogErrorMessage(typeof(Worker), nameof(OnElapsedNotificationUpdate), ex.StackTrace + "--" + ex.Message, ex, string.Empty, request.CorrelationId, request.ClientId, request.DeliveryconstructId);
                            CustomAppsActivityLog log = new CustomAppsActivityLog()
                            {
                                CorrelationId = request.CorrelationId,
                                FeatureName = "OnElapsedNotificationUpdate",
                                Status = "Error",
                                StackTrace = ex.StackTrace,
                                ErrorMessage = ex.Message,
                                ErrorMethod = "OnElapsedNotificationUpdate catch block",
                                CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                                CreatedBy = "SYSTEM",
                            };

                            genericAutotrainService.InsertCustomAppsActivityLog(log);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(Worker), nameof(OnElapsedNotificationUpdate), ex.StackTrace + "--" + ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                CustomAppsActivityLog log = new CustomAppsActivityLog()
                {
                    FeatureName = "OnElapsedNotificationUpdate",
                    Status = "Error",
                    StackTrace = ex.StackTrace,
                    ErrorMessage = ex.Message,
                    ErrorMethod = "OnElapsedNotificationUpdate main catch block",
                    CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                    CreatedBy = "SYSTEM",
                };

                genericAutotrainService.InsertCustomAppsActivityLog(log);
            }

        }

        /// <summary>
        /// Terminate the models struck for > 2 hrs and notify the app using call back
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void OnElapsedTerminateModels(object source, ElapsedEventArgs e)
        {
            #region SPA Terminate models
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "OnElapsedTerminateModels", "SPA Terminate In-Progress models  -" + DateTime.Now.ToString(), string.Empty, string.Empty, string.Empty, string.Empty);
            GenericAutotrainService genericAutotrainService = new GenericAutotrainService(databaseProvider);
            try
            {
                //Acknowledging the method called for every 3 hrs
                autoTrainedFeatures = new LogAutoTrainedFeatures();
                autoTrainedFeatures.FeatureName = "SPATerminateModels";
                autoTrainedFeatures.ModelList = new string[] { };
                autoTrainedFeatures.Sequence = 1;
                autoTrainedFeatures.ModelsCount = 0;
                autoTrainedFeatures.EndedOn = null;
                autoTrainedFeatures.LogId = Guid.NewGuid().ToString();
                autoTrainedFeatures.FunctionName = "OnElapsedTerminateModels - Initiated";
                autoTrainedFeatures.StartedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                InsertAutoTrainLog(autoTrainedFeatures);

                var filterBuilder = Builders<IngrainRequestQueue>.Filter;
                var projection = Builders<IngrainRequestQueue>.Projection.Exclude(CONSTANTS.Id);
                var filter = (filterBuilder.Eq(CONSTANTS.RequestStatus, CONSTANTS.Occupied) | filterBuilder.Eq(CONSTANTS.RequestStatus, CONSTANTS.In_Progress) | filterBuilder.Eq(CONSTANTS.RequestStatus, CONSTANTS.InProgress) | filterBuilder.Eq(CONSTANTS.Status, "P")) & filterBuilder.Eq("pageInfo", "TrainAndPredict");
                var result = _collection.Find(filter).Project<IngrainRequestQueue>(projection).Sort(Builders<IngrainRequestQueue>.Sort.Ascending("CreatedOn")).ToList();
                if (result != null && result.Count > 0)
                {
                    //Terminate models which struck in Occupied or In Progress for more than 2 hours
                    DateTime currentTime = DateTime.Parse(DateTime.Now.ToString(CONSTANTS.DateHoursFormat));
                    result = result.Where(x => (currentTime - DateTime.Parse(x.CreatedOn)).TotalMinutes > appSettings.TerminateModelTimeInterval).Take(appSettings.NotificationUpdateQueueLimit).ToList();

                    if (result != null && result.Count > 0)
                    {
                        //Log the CorrelationId's of Models to be terminated
                        autoTrainedFeatures = new LogAutoTrainedFeatures();
                        autoTrainedFeatures.FeatureName = "SPATerminateModels";
                        var CorrelationIds = result.Select(x => x.CorrelationId).ToList();
                        autoTrainedFeatures.ModelList = BsonSerializer.Deserialize<string[]>(JsonConvert.SerializeObject(CorrelationIds));
                        autoTrainedFeatures.Sequence = 2;
                        autoTrainedFeatures.ModelsCount = result.Count;
                        autoTrainedFeatures.EndedOn = null;
                        autoTrainedFeatures.LogId = Guid.NewGuid().ToString();
                        autoTrainedFeatures.FunctionName = "OnElapsedTerminateModels";
                        autoTrainedFeatures.StartedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                        InsertAutoTrainLog(autoTrainedFeatures);

                        foreach (var item in result)
                        {
                            try
                            {
                                //Update the sub-sequent models to Error state
                                var filterBuilder2 = Builders<IngrainRequestQueue>.Filter;
                                var filterCorrelation = filterBuilder2.Eq("CorrelationId", item.CorrelationId) & filterBuilder2.Ne("Status", "C") & filterBuilder2.Ne("Status", "E") & filterBuilder2.Ne("Status", "Error");
                                var update = Builders<IngrainRequestQueue>.Update.Set("RequestStatus", CONSTANTS.ErrorMessage).Set("Status", "E").Set("Message", CONSTANTS.ProcessTimeOut);
                                var isUpdated = _collection.UpdateMany(filterCorrelation, update);

                                //Notify the app using CallBackURL
                                IngrainResponseData responseData = new IngrainResponseData
                                {
                                    CorrelationId = item.CorrelationId,
                                    Status = CONSTANTS.ErrorMessage,
                                    Message = CONSTANTS.TrainingFailed,
                                    ErrorMessage = CONSTANTS.ProcessTimeOut
                                };
                                var response = genericAutotrainService.CallbackResponse(responseData, item.ApplicationName, item.AppURL, item.ClientId, item.DeliveryconstructId, item.AppID, item.TemplateUseCaseID, item.RequestId, null, item.CreatedByUser);
                                if (response == "Error")
                                {
                                    CustomAppsActivityLog log = new CustomAppsActivityLog()
                                    {
                                        CorrelationId = item.CorrelationId,
                                        FeatureName = "SPATerminateModels",
                                        Status = "Error",
                                        ErrorMessage = "Unable to notify app using call back url",
                                        ErrorMethod = "OnElapsedTerminateModels foreach loop",
                                        CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                                        CreatedBy = "SYSTEM",
                                    };

                                    genericAutotrainService.InsertCustomAppsActivityLog(log);
                                }
                            }
                            catch (Exception ex)
                            {
                                LOGGING.LogManager.Logger.LogErrorMessage(typeof(Worker), nameof(OnElapsedTerminateModels), ex.StackTrace + "--" + ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                                CustomAppsActivityLog log = new CustomAppsActivityLog()
                                {
                                    CorrelationId = item.CorrelationId,
                                    FeatureName = "SPATerminateModels",
                                    Status = "Error",
                                    StackTrace = ex.StackTrace,
                                    ErrorMessage = ex.Message,
                                    ErrorMethod = "OnElapsedTerminateModels catch block",
                                    CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                                    CreatedBy = "SYSTEM",
                                };

                                genericAutotrainService.InsertCustomAppsActivityLog(log);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(Worker), nameof(OnElapsedTerminateModels), ex.StackTrace + "--" + ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
            #endregion
        }
        private string GetApplicationName(string applicationId)
        {
            var collection = _database.GetCollection<AppIntegration>(CONSTANTS.AppIntegration);
            var filterBuilder = Builders<AppIntegration>.Filter;
            var appFilter = filterBuilder.Eq(CONSTANTS.ApplicationID, applicationId);
            var appProjection = Builders<AppIntegration>.Projection.Exclude("_id").Include("ApplicationName");
            var appData = collection.Find(appFilter).Project<AppIntegration>(appProjection).FirstOrDefault();
            if (appData != null)
                return appData.ApplicationName;
            else
                return null;
        }

        /// <summary>
        /// Triggers every 30 mins to retrain the model.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void OnElapsedAutoRetrainModels(object source, ElapsedEventArgs e)
        {
            this.AutoRetrainModels();
        }
        private void AutoRetrainModels()
        {
            try
            {
                var appModelRun = appSettings.APPAutoTimeToRun + " " + appSettings.APPAutoTimePeriod;
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "AutoRetrainModels", "AutoReTrainTasks START - Time to start:" + appModelRun + " CurrentDateTime: " + DateTime.Now.ToString("hh:mm:ss tt") + "*****", string.Empty, string.Empty, string.Empty, string.Empty);
                if (appModelRun == DateTime.Now.ToString("hh:00:00 tt"))
                {
                    //autoTrainedFeatures = new LogAutoTrainedFeatures();
                    autoTrainedFeatures.FeatureName = "AutoRetrainModels - Started";
                    autoTrainedFeatures.ModelList = new string[] { };
                    autoTrainedFeatures.Sequence = 0;
                    autoTrainedFeatures.ModelsCount = 0;
                    autoTrainedFeatures.EndedOn = null;
                    autoTrainedFeatures.LogId = Guid.NewGuid().ToString();
                    autoTrainedFeatures.FunctionName = "AutoRetrainModels";
                    autoTrainedFeatures.StartedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                    InsertAutoTrainLog(autoTrainedFeatures);

                    #region SPA AUTORETRAIN
                    if (appSettings.Environment != CONSTANTS.PAMEnvironment)
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "AutoRetrainModels", "AutoReTrainTasks STARTED -SPAAutoTrain" + DateTime.Now.ToString("hh:mm:ss tt") + "*****", string.Empty, string.Empty, string.Empty, string.Empty);
                        Thread APPModelAutoTrain = new Thread(SPAAutoTrain);
                        autoTrainedFeatures.FeatureName = "SPAAutoTrain";
                        autoTrainedFeatures.ModelList = new string[] { };
                        autoTrainedFeatures.Sequence = 1;
                        autoTrainedFeatures.ModelsCount = 0;
                        autoTrainedFeatures.EndedOn = null;
                        autoTrainedFeatures.LogId = Guid.NewGuid().ToString();
                        autoTrainedFeatures.FunctionName = "SPAAutoTrain";
                        autoTrainedFeatures.StartedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                        InsertAutoTrainLog(autoTrainedFeatures);
                        APPModelAutoTrain.Start();
                    }
                    #endregion

                    #region INSTAAUTORETRAIN
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "AutoRetrainModels", "InstaAutoRetrain STARTED - InstaAutoRetrain" + DateTime.Now.ToString("hh:mm:ss tt") + "*****", string.Empty, string.Empty, string.Empty, string.Empty);
                    Thread instaAutoThread = new Thread(InstaAutoRetrain);
                    instaAutoThread.Start();
                    #endregion

                    #region RETRAIN ENTITY AND OTHER MODELS                    
                    Thread entityAutoThread = new Thread(EntityAutoRetrain);
                    entityAutoThread.Start();
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "AutoRetrainModels", "AutoReTrainTasks STARTED -EntityAutoRetrain" + DateTime.Now.ToString("hh:mm:ss tt") + "*****", string.Empty, string.Empty, string.Empty, string.Empty);
                    #endregion
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(Worker), nameof(AutoRetrainModels), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
        }
        private void OnElapsedUsageTracking(object source, ElapsedEventArgs e)
        {
            Thread usageTrackingThread = new Thread(_assetUsageTrackingService.AssetUsageTracking);
            usageTrackingThread.Start();
        }
        private void OnElapsedRetrainErrorModels(object source, ElapsedEventArgs e)
        {
            Thread retrainErrorModels = new Thread(RetrainErrorModelWithWindowsService);
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "ONELAPSEDRETRAINERRORMODELS", "RETRAINERRORMODELWITHWINDOWSSERVICE STARTED -" + DateTime.Now.ToString(), string.Empty, string.Empty, string.Empty, string.Empty);
            autoTrainedFeatures.FeatureName = "WSModelRetrain";
            autoTrainedFeatures.ModelList = new string[] { };
            autoTrainedFeatures.Sequence = 1;
            autoTrainedFeatures.ModelsCount = 0;
            autoTrainedFeatures.EndedOn = null;
            autoTrainedFeatures.LogId = Guid.NewGuid().ToString();
            autoTrainedFeatures.FunctionName = "RetrainErrorModelWithWindowsService";
            autoTrainedFeatures.StartedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
            InsertAutoTrainLog(autoTrainedFeatures);
            retrainErrorModels.Start();
        }
        private void OnElapsedArchivalPurging(object source, ElapsedEventArgs e)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "OnElapsedArchivalPurging", "Archival purging Triggered -" + DateTime.Now.ToString(), string.Empty, string.Empty, string.Empty, string.Empty);
            Thread ArchivalPurgingThread = new Thread(ArchivalPurgingTrigger);
            ArchivalPurgingThread.Start();
        }
        public Task StopAsync(CancellationToken cancellationToken)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "OnStop", "WINDOWS SERVICE STOPPED -" + DateTime.Now.ToString(), string.Empty, string.Empty, string.Empty, string.Empty);
            lastUpdateHour = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
            var filterBuilder = Builders<IngrainRequestQueue>.Filter;

            if (appSettings.Environment != CONSTANTS.PAMEnvironment)
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "spe delete start", "Windows service spe delete started -" + DateTime.Now.ToString(), string.Empty, string.Empty, string.Empty, string.Empty);
                var spe_projection = Builders<IngrainRequestQueue>.Projection.Include(CONSTANTS.CorrelationId).Exclude(CONSTANTS.Id);
                var spe_Filter = filterBuilder.Eq(CONSTANTS.Status, "null") & filterBuilder.Eq("Function", "AutoTrain") & filterBuilder.Eq(CONSTANTS.TemplateUseCaseID, appSettings.SPETemplateUseCaseID);
                var spe_ingrainRequests = _collection.Find(spe_Filter).Project<IngrainRequestQueue>(spe_projection).Sort(Builders<IngrainRequestQueue>.Sort.Ascending("CreatedOn")).ToList();
                if (spe_ingrainRequests.Count > 0)
                {
                    for (int i = 0; i < spe_ingrainRequests.Count; i++)
                    {
                        string correlationId = spe_ingrainRequests[i].CorrelationId;
                        delete_backUpModelSPP(correlationId);
                    }
                }
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "spe delete ended", "Windows service spe delete ended -" + DateTime.Now.ToString(), string.Empty, string.Empty, string.Empty, string.Empty);
            }


            // Window service Stop - check InProgress , Occupied and New request - Update  to E -Start

            var projection = Builders<IngrainRequestQueue>.Projection.Exclude(CONSTANTS.Id);
            var Filter = filterBuilder.Eq(CONSTANTS.RequestStatus, appSettings.requestInProgress) | filterBuilder.Eq(CONSTANTS.RequestStatus, "Occupied") | filterBuilder.Eq(CONSTANTS.RequestStatus, "New") | filterBuilder.Eq("Status", "P");
            var ingrainRequests = _collection.Find(Filter).Project<IngrainRequestQueue>(projection).Sort(Builders<IngrainRequestQueue>.Sort.Ascending("CreatedOn")).ToList();
            var MappingCollection = _database.GetCollection<PublicTemplateMapping>(CONSTANTS.PublicTemplateMapping);
            var Builder = Builders<PublicTemplateMapping>.Filter;

            if (ingrainRequests.Count > 0)
            {
                var updateBuilder = Builders<IngrainRequestQueue>.Update;
                var update = updateBuilder.Set(CONSTANTS.Status, "E")
                    .Set(CONSTANTS.Message, CONSTANTS.WSStartStatus)
                    .Set(CONSTANTS.RequestStatus, CONSTANTS.WSStartStatus);
                var result = _collection.UpdateMany(Filter, update);
                var autoTrainRequest = ingrainRequests.FindAll(item => item.Function == "AutoTrain" || item.Function == "AutoRetrain").ToList();
                if (autoTrainRequest.Count > 0)
                {
                    foreach (var item in autoTrainRequest)
                    {
                        IngrainResponseData CallBackResponse = new IngrainResponseData
                        {
                            CorrelationId = item.CorrelationId,
                            Status = "E",
                            Message = CONSTANTS.WSStartStatus,
                            ErrorMessage = CONSTANTS.WSStartStatus,
                        };
                        var Projection1 = Builders<PublicTemplateMapping>.Projection.Exclude("_id");
                        var filter = Builder.Eq(CONSTANTS.ApplicationID, item.AppID) & Builder.Eq(CONSTANTS.UsecaseID, item.TemplateUseCaseID);
                        var templatedata = MappingCollection.Find(filter).Project<PublicTemplateMapping>(Projection1).FirstOrDefault();
                        if (templatedata != null)
                        {
                            if (templatedata.IsMultipleApp == "yes")
                            {
                                var AppIntegCollection = _database.GetCollection<AppIntegration>(CONSTANTS.AppIntegration);
                                var filterBuilder1 = Builders<AppIntegration>.Filter;
                                var AppFilter = filterBuilder1.Eq(CONSTANTS.ApplicationID, item.AppID);

                                var Projection = Builders<AppIntegration>.Projection.Exclude("_id");
                                var AppData = AppIntegCollection.Find(AppFilter).Project<AppIntegration>(Projection).FirstOrDefault();
                                item.ApplicationName = AppData.ApplicationName;
                            }
                            else
                            {
                                item.ApplicationName = templatedata.ApplicationName;
                            }
                        }
                        GenericAutotrainService _genericAutotrainService = new GenericAutotrainService(databaseProvider);
                        if (!(string.IsNullOrEmpty(item.AppURL)) && (!(string.IsNullOrEmpty(item.ApplicationName)) && (!string.IsNullOrEmpty(item.AppID))))
                        {
                            string callbackResonse = _genericAutotrainService.CallbackResponse(CallBackResponse, item.ApplicationName, item.AppURL, item.ClientId, item.DeliveryconstructId, item.AppID, item.TemplateUseCaseID, item.RequestId, null, item.CreatedByUser);
                            if (item.Function == "AutoRetrain" && item.AppID == "a3798931-4028-4f72-8bcd-8bb368cc71a9")
                            {
                                var queueCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
                                var filterBuilder1 = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, item.CorrelationId) & Builders<BsonDocument>.Filter.Eq("Function", item.Function) & Builders<BsonDocument>.Filter.Eq("RequestId", item.RequestId);
                                queueCollection.DeleteMany(filterBuilder1);

                            }
                        }
                        else
                        {
                            _genericAutotrainService.CallBackErrorLog(CallBackResponse, item.ApplicationName, item.AppURL, null, item.ClientId, item.DeliveryconstructId, item.AppID, item.TemplateUseCaseID, item.RequestId, null, item.CreatedByUser);
                        }

                    }
                }
            }
            else
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "INGRAINREQUESTS", "INGRAINREQUESTS NO--" + DateTime.Now.ToString(), string.Empty, string.Empty, string.Empty, string.Empty);
            }
            // Window service stop - check InProgress , Occupied and New request - Update  to E - End

            SetWorkerServiceStatus(false);


            if (SPPGenericAPICallTimer.Enabled)
            {
                SPPGenericAPICallTimer.Stop();
                SPPGenericAPICallTimer.Dispose();
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "StopAsync", "Simulation timer Stopped" + DateTime.Now.ToString(), string.Empty, string.Empty, string.Empty, string.Empty);
            }

            return Task.CompletedTask;
        }
        private void DecryptTemplateData()
        {
            try
            {
                string[] templateUsecaseIds = new string[] { CONSTANTS.AgileDefectPrediction, CONSTANTS.AgileEffortPrediction, CONSTANTS.AgileVelocityPrediction, CONSTANTS.NewTeamAllocation };
                var deployModelcollection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
                var filter = Builders<DeployModelsDto>.Filter.In(CONSTANTS.CorrelationId, templateUsecaseIds) & Builders<DeployModelsDto>.Filter.Eq(x => x.DBEncryptionRequired, true) & Builders<DeployModelsDto>.Filter.Eq(x => x.IsTemplateDataEncyptionUpdated, false);
                var projection = Builders<DeployModelsDto>.Projection.Include(CONSTANTS.CorrelationId).Include("DBEncryptionRequired").Include("IsTemplateDataEncyptionUpdated").Exclude("_id");
                var result = deployModelcollection.Find(filter).Project<DeployModelsDto>(projection).ToList();
                foreach (var item in result)
                {
                    this.EncryptWithNewKey(item.CorrelationId);
                    //Update IsTemplateDataEncyptionUpdated to true
                    var update = Builders<DeployModelsDto>.Update.Set("IsTemplateDataEncyptionUpdated", true);
                    deployModelcollection.UpdateOne(Builders<DeployModelsDto>.Filter.Eq(CONSTANTS.CorrelationId, item.CorrelationId), update);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InferenceEngineService), nameof(DecryptTemplateData), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
        }
        private void EncryptWithNewKey(string correlationId)
        {
            ////For DEDataProcessing
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var dataProcessingProjection = Builders<BsonDocument>.Projection.Include(CONSTANTS.Filters).Include(CONSTANTS.DataModification);
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataProcessing);
            var dataProcessingResult = collection.Find(filter).Project<BsonDocument>(dataProcessingProjection).FirstOrDefault();
            if (dataProcessingResult != null)
            {
                //Decrypt with devtest key
                dataProcessingResult[CONSTANTS.Filters] = AesProvider.Decrypt(dataProcessingResult[CONSTANTS.Filters].AsString, appSettings.DevtestAesKey, appSettings.DevtestAesVector);
                dataProcessingResult[CONSTANTS.DataModification] = AesProvider.Decrypt(dataProcessingResult[CONSTANTS.DataModification].AsString, appSettings.DevtestAesKey, appSettings.DevtestAesVector);

                //Encrypt with current environment key
                if (appSettings.IsAESKeyVault)
                {
                    dataProcessingResult[CONSTANTS.Filters] = CryptographyUtility.Encrypt(dataProcessingResult[CONSTANTS.Filters].AsString);
                    dataProcessingResult[CONSTANTS.DataModification] = CryptographyUtility.Encrypt(dataProcessingResult[CONSTANTS.DataModification].AsString);
                }
                else
                {
                    dataProcessingResult[CONSTANTS.Filters] = AesProvider.Encrypt(dataProcessingResult[CONSTANTS.Filters].AsString, appSettings.aesKey, appSettings.aesVector);
                    dataProcessingResult[CONSTANTS.DataModification] = AesProvider.Encrypt(dataProcessingResult[CONSTANTS.DataModification].AsString, appSettings.aesKey, appSettings.aesVector);
                }

                //Update the document with encrypted data
                var dataModificationUpdate = Builders<BsonDocument>.Update.Set(CONSTANTS.DataModification, dataProcessingResult[CONSTANTS.DataModification]);
                collection.UpdateOne(filter, dataModificationUpdate);

                var filtersUpdate = Builders<BsonDocument>.Update.Set(CONSTANTS.Filters, dataProcessingResult[CONSTANTS.Filters]);
                collection.UpdateOne(filter, filtersUpdate);
            }
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        public void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            //base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
        }

        #endregion

        /// <summary>
        /// GetRequests from Ingrain Request table.
        /// </summary>
        public void GetMessageRequestsTimeStamp()
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "GetMessageRequestsTimeStamp-START", "GetMessageRequestsTimeStamp method triggered" + DateTime.Now.ToString(), string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                var metrics = new SystemUsageDetails();
                if (appSettings.Environment == CONSTANTS.PAMEnvironment)
                {
                    metrics = cPUMemoryCount.GetMetrics(appSettings.Environment, appSettings.IsSaaSPlatform);

                    //var metrics = cPUMemoryCount.GetMetrics();
                    //var metrics = new SystemUsageDetails();
                    List<double> lst = new List<double>();
                    int count = 0;
                    while (count < 10)
                    {
                        metrics = cPUMemoryCount.GetMetrics(appSettings.Environment, appSettings.IsSaaSPlatform);
                        lst.Add(metrics.MemoryUsagePercentage);
                        count++;
                    }
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "GetMessageRequestsTimeStamp-START", "Count is > 10", string.Empty, string.Empty, string.Empty, string.Empty);
                    if (lst.Count > 0)
                    {
                        metrics.MemoryUsagePercentage = lst.Max();
                    }
                }


                LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "GetMessageRequestsTimeStamp-START", "Windows service triggered" + DateTime.Now.ToString(), string.Empty, string.Empty, string.Empty, string.Empty);
                var filterBuilder = Builders<IngrainRequestQueue>.Filter;
                var projection = Builders<IngrainRequestQueue>.Projection.Exclude(CONSTANTS.Id);
                var Filter = filterBuilder.Eq("RequestStatus", appSettings.requestNew) | filterBuilder.Eq("RequestStatus", appSettings.requestInProgress) | filterBuilder.Eq("RequestStatus", "Queued");
                var ingrainRequests = _collection.Find(Filter).Project<IngrainRequestQueue>(projection).Sort(Builders<IngrainRequestQueue>.Sort.Ascending("CreatedOn")).Limit(appSettings.RequestBatchLimit).ToList();
                if (ingrainRequests != null)
                {
                    if (ingrainRequests.Count > 0)
                    {
                        #region Temporary
                        //For SPP Issue more records piled up. Total 58989 Ingrain request in queue 
                        _queueManagerService.RequestBatchLimitInsert(ingrainRequests.Count, CONSTANTS.SSAIRequestBatchLimitMonitor);
                        #endregion

                        var allRequests = ingrainRequests.AsQueryable().Where(x => x.RequestStatus == "New" || x.RequestStatus == "Queued").ToList();
                        var result = allRequests.Where(x => x.pageInfo != "PublishModel" || x.pageInfo != "ForecastModel").ToList();
                        //int inProgressStatus = ingrainRequests.AsQueryable().Where(x => x.RequestStatus == appSettings.requestInProgress && x.pageInfo == "RecommendedAI" && Convert.ToDateTime(x.CreatedOn) >= DateTime.Now.AddHours(-48)).Count();
                        //int ProgressStatus = ingrainRequests.AsQueryable().Where(x => x.RequestStatus == appSettings.requestInProgress && x.Function == "AutoTrain" && Convert.ToDateTime(x.CreatedOn) >= DateTime.Now.AddHours(-48)).Count();
                        int item = -1;
                        try
                        {
                            if (result != null)
                            {
                                for (int i = 0; i < result.Count; i++)
                                {
                                    item = i;
                                    if ((!string.IsNullOrEmpty(result[i].CorrelationId) && result[i].CreatedByUser != null) || result[i].Function == "IngestDataSet")
                                    {
                                        if (!string.IsNullOrEmpty(result[i].CorrelationId) && result[i].CorrelationId.ToUpper().Contains("_BACKUP"))
                                        {
                                            continue;
                                        }
                                        _queueManagerService.UpdateTrainingQueueStatus();
                                        string status = _queueManagerService.GetTrainingQueueStatus();
                                        if (status == "Available")
                                        {
                                            switch (result[i].Function)
                                            {
                                                case "FileUpload":
                                                    _start.FileName = "\"" + _pythonExe + "\"";
                                                    _start.Arguments = string.Format("{0} {1} {2} {3} {4} {5}", _pythonPYPath + "invokeIngestData.py", result[i].CorrelationId, result[i].RequestId, result[i].pageInfo, result[i].CreatedByUser, result[i].IsForAutoTrain ? "True" : "False");
                                                    InvokePython(result[i], filterBuilder);
                                                    break;

                                                case "DataCleanUp":
                                                    _start.FileName = "\"" + _pythonExe + "\"";
                                                    _start.Arguments = string.Format("{0} {1} {2} {3} {4}", _pythonPYPath + "invokeDataCleanup.py", result[i].CorrelationId, result[i].RequestId, result[i].pageInfo, result[i].CreatedByUser);
                                                    InvokePython(result[i], filterBuilder);
                                                    break;

                                                case "AddFeature":
                                                    _start.FileName = "\"" + _pythonExe + "\"";
                                                    _start.Arguments = string.Format("{0} {1} {2} {3} {4}", _pythonPYPath + "invokeAddFeature.py", result[i].CorrelationId, result[i].RequestId, result[i].pageInfo, result[i].CreatedByUser);
                                                    InvokePython(result[i], filterBuilder);
                                                    break;

                                                case "DataTransform":
                                                    _start.FileName = "\"" + _pythonExe + "\"";
                                                    _start.Arguments = string.Format("{0} {1} {2} {3} {4}", _pythonPYPath + "invokeDataPreprocessing.py", result[i].CorrelationId, result[i].RequestId, result[i].pageInfo, result[i].CreatedByUser);
                                                    InvokePython(result[i], filterBuilder);
                                                    break;

                                                case "RecommendedAI":
                                                    if (appSettings.Environment == CONSTANTS.PAMEnvironment)
                                                    {
                                                        if (metrics.MemoryUsagePercentage < 80)
                                                        {
                                                            _start.FileName = "\"" + _pythonExe + "\"";
                                                            _start.Arguments = string.Format("{0} {1} {2} {3} {4} {5}", _pythonPYPath + "invokeRecommendedAI.py", result[i].CorrelationId, result[i].RequestId, result[i].pageInfo, result[i].CreatedByUser, 0);
                                                            InvokePython(result[i], filterBuilder);
                                                        }
                                                        Thread.Sleep(2000);
                                                    }
                                                    else
                                                    {
                                                        _start.FileName = "\"" + _pythonExe + "\"";
                                                        _start.Arguments = string.Format("{0} {1} {2} {3} {4} {5}", _pythonPYPath + "invokeRecommendedAI.py", result[i].CorrelationId, result[i].RequestId, result[i].pageInfo, result[i].CreatedByUser, 0);
                                                        InvokePython(result[i], filterBuilder);
                                                    }
                                                    break;
                                                case CONSTANTS.RetrainRecommendedAI:
                                                    _start.FileName = "\"" + _pythonExe + "\"";
                                                    _start.Arguments = string.Format("{0} {1} {2} {3} {4} {5}", _pythonPYPath + "invokeRecommendedAI.py", result[i].CorrelationId, result[i].RequestId, result[i].pageInfo, result[i].CreatedByUser, 1);
                                                    InvokePython(result[i], filterBuilder);
                                                    break;


                                                case "WFAnalysis":
                                                    _start.FileName = "\"" + _pythonExe + "\"";
                                                    _start.Arguments = string.Format("{0} {1} {2} {3} {4}", _pythonPYPath + "invokeWFAnalysis.py", result[i].CorrelationId, result[i].RequestId, result[i].pageInfo, result[i].CreatedByUser);
                                                    InvokePython(result[i], filterBuilder);
                                                    break;

                                                case "WFIngestData":
                                                    _start.FileName = "\"" + _pythonExe + "\"";
                                                    _start.Arguments = string.Format("{0} {1} {2} {3} {4}", _pythonPYPath + "invokeWFIngestData.py", result[i].CorrelationId, result[i].RequestId, result[i].pageInfo, result[i].CreatedByUser);
                                                    InvokePython(result[i], filterBuilder);
                                                    break;

                                                case "HyperTune":
                                                    _start.FileName = "\"" + _pythonExe + "\"";
                                                    _start.Arguments = string.Format("{0} {1} {2} {3} {4}", _pythonPYPath + "invokeRecommendedAI.py", result[i].CorrelationId, result[i].RequestId, result[i].pageInfo, result[i].CreatedByUser);
                                                    InvokePython(result[i], filterBuilder);
                                                    break;

                                                case "ViewDataQuality":
                                                    _start.FileName = "\"" + _pythonExe + "\"";
                                                    _start.Arguments = string.Format("{0} {1} {2} {3} {4}", _pythonPYPath + "invokeDataCleanup.py", result[i].CorrelationId, result[i].RequestId, result[i].pageInfo, result[i].CreatedByUser);
                                                    InvokePython(result[i], filterBuilder);
                                                    break;

                                                case "PrescriptiveAnalytics":
                                                    _start.FileName = "\"" + _pythonExe + "\"";
                                                    _start.Arguments = string.Format("{0} {1} {2} {3} {4}", _pythonPYPath + "invokePrescriptiveAnalysis.py", result[i].CorrelationId, result[i].RequestId, result[i].pageInfo, result[i].CreatedByUser);
                                                    InvokePython(result[i], filterBuilder);
                                                    break;
                                                case "AutoTrain":
                                                    AutoTrainModel(result[i], filterBuilder);
                                                    break;
                                                case "ModelMonitor":
                                                    _start.FileName = "\"" + _pythonExe + "\"";
                                                    _start.Arguments = string.Format("{0} {1} {2} {3} {4}", _pythonPYPath + "invokeContinuousMonitoring.py", result[i].CorrelationId, result[i].RequestId, result[i].pageInfo, result[i].CreatedByUser);
                                                    InvokePython(result[i], filterBuilder);
                                                    break;
                                                case "Prediction":
                                                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "****PREDICTION START*****", CONSTANTS.START, string.IsNullOrEmpty(Convert.ToString(result[i].CorrelationId)) ? default(Guid) : new Guid(Convert.ToString(result[i].CorrelationId)), Convert.ToString(result[i].AppID), string.Empty, Convert.ToString(result[i].ClientID), Convert.ToString(result[i].DeliveryconstructId));
                                                    //For SPE Prediction
                                                    if (result[i].IsForAPI == false)
                                                    {
                                                        _start.FileName = "\"" + _pythonExe + "\"";
                                                        _start.Arguments = string.Format("{0} {1} {2} {3} {4}", _pythonPYPath + "publishModel.py", result[i].CorrelationId, result[i].UniId, result[i].pageInfo, result[i].CreatedByUser);
                                                        InvokePython(result[i], filterBuilder);
                                                        _queueManagerService.UpdateQueueMonitor();
                                                        GetPrediction(result[i], filterBuilder);
                                                    }
                                                    break;
                                                case CONSTANTS.CascadingModel:
                                                    CascadeAutoTrainModel(result[i], filterBuilder);
                                                    break;
                                                case CONSTANTS.PredictCascade:
                                                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "****CUSTOM CASCADE PREDICTION START*****", CONSTANTS.START, string.IsNullOrEmpty(Convert.ToString(result[i].CorrelationId)) ? default(Guid) : new Guid(Convert.ToString(result[i].CorrelationId)), Convert.ToString(result[i].AppID), string.Empty, Convert.ToString(result[i].ClientID), Convert.ToString(result[i].DeliveryconstructId));
                                                    //For Custom Cascade Prediction
                                                    _start.FileName = "\"" + _pythonExe + "\"";
                                                    _start.Arguments = string.Format("{0} {1} {2} {3} {4}", _pythonPYPath + "predictCascade.py", result[i].CorrelationId, result[i].RequestId, result[i].pageInfo, result[i].CreatedByUser);
                                                    InvokePython(result[i], filterBuilder);
                                                    break;
                                                case "IngestDataSet":
                                                    _start.FileName = "\"" + _pythonExe + "\"";
                                                    _start.Arguments = string.Format("{0} {1} {2} {3} {4}", _pythonPYPath + "OfflineUtility.py", result[i].RequestId, result[i].DataSetUId, result[i].pageInfo, result[i].CreatedByUser);
                                                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "*** pageInfo : IngestDataSet ***", "_start.Arguments: " + _start.Arguments, string.IsNullOrEmpty(result[i].CorrelationId) ? default(Guid) : new Guid(result[i].CorrelationId), result[i].AppID, string.Empty, result[i].ClientID, result[i].DeliveryconstructId);
                                                    InvokePython(result[i], filterBuilder);
                                                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "*** pageInfo : IngestDataSet ***", "InvokePython completed", string.IsNullOrEmpty(result[i].CorrelationId) ? default(Guid) : new Guid(result[i].CorrelationId), result[i].AppID, string.Empty, result[i].ClientID, result[i].DeliveryconstructId);
                                                    break;
                                                case CONSTANTS.TransformIngestedData:
                                                    _start.FileName = "\"" + _pythonExe + "\"";
                                                    _start.Arguments = string.Format("{0} {1} {2} {3} {4} {5}", _pythonPYPath + "transformIngestedData.py", result[i].CorrelationId, result[i].RequestId, result[i].pageInfo, result[i].CreatedByUser, result[i].TemplateUseCaseID);
                                                    InvokePython(result[i], filterBuilder);
                                                    break;
                                                case CONSTANTS.FMTransform:
                                                    TransformingFMModel(result[i], filterBuilder);
                                                    break;
                                                case CONSTANTS.TerminatePythyon:
                                                    TerminatePythonModelTraining(result[i], filterBuilder);
                                                    break;
                                                case CONSTANTS.FMVisualize:
                                                    _start.FileName = "\"" + _pythonExe + "\"";
                                                    _start.Arguments = string.Format("{0} {1} {2} {3} {4} {5}", _pythonPYPath + "invokeFMPrediction.py", result[i].CorrelationId, result[i].FMCorrelationId, result[i].UniId, result[i].pageInfo, result[i].CreatedByUser);
                                                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "****FMVISUALIZE*****", "--FMVISUALIZE[I].PARAMARGS--" + result[i].ParamArgs + "***FMVISUALIZE _START.ARGUMENTS***" + _start.Arguments + "--ParamArgs--" + result[i].ParamArgs, string.IsNullOrEmpty(result[i].CorrelationId) ? default(Guid) : new Guid(result[i].CorrelationId), result[i].AppID, string.Empty, result[i].ClientID, result[i].DeliveryconstructId);
                                                    InvokePython(result[i], filterBuilder);
                                                    break;
                                                case "AutoRetrain":
                                                    AmbulanceLaneAutoReTrainInvoke(result[i]);
                                                    break;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        LOGGING.LogManager.Logger.LogErrorMessage(typeof(Worker), nameof(GetMessageRequestsTimeStamp), "ERROR: CorrelationId/CreatedByUser is null", null, "Result: " + Convert.ToString(result[i]), string.Empty, string.Empty, string.Empty);
                                        UpdateRequiredParamaterError(result[i].RequestId, filterBuilder);
                                    }
                                }
                            }
                            #region Prediction Queue
                            var predictionRequest = allRequests.Where(x => x.pageInfo == "PublishModel" || x.pageInfo == "ForecastModel").ToList();

                            List<IngrainRequestQueue> remainingRequests = new List<IngrainRequestQueue>();
                            List<IngrainRequestQueue> queuedRequests = new List<IngrainRequestQueue>();

                            _queueManagerService.UpdateQueueMonitor();

                            if (predictionRequest != null)
                            {
                                for (int i = 0; i < predictionRequest.Count; i++)
                                {
                                    QueueMonitor queue = _queueManagerService.GetQueueStatus();
                                    List<AppQueue> availableAppQueues = queue.AppWiseQueueDetails.Where(x => x.QueueStatus == "Available").ToList();
                                    bool initiatePrediction = queue.QueueStatus == "Available" && availableAppQueues.Any(c => c.AppId == predictionRequest[i].AppID);
                                    if (initiatePrediction)
                                    {
                                        Thread.Sleep(10);
                                        switch (predictionRequest[i].Function)
                                        {
                                            case "PublishModel":
                                                if (result[i].IsForAPI == false)
                                                {
                                                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "****PUBLISHMODEL*****", "--PREDICTIONREQUEST[I].PARAMARGS--" + predictionRequest[i].ParamArgs, string.IsNullOrEmpty(predictionRequest[i].CorrelationId) ? default(Guid) : new Guid(predictionRequest[i].CorrelationId), predictionRequest[i].AppID, string.Empty, predictionRequest[i].ClientID, predictionRequest[i].DeliveryconstructId);
                                                    _start.FileName = "\"" + _pythonExe + "\"";
                                                    if (predictionRequest[i].ParamArgs == CONSTANTS.True)
                                                    {
                                                        _start.Arguments = string.Format("{0} {1} {2} {3} {4}", _pythonPYPath + "Cascaded.py", predictionRequest[i].CorrelationId, predictionRequest[i].UniId, predictionRequest[i].pageInfo, predictionRequest[i].CreatedByUser);
                                                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "****PUBLISHMODEL*****", "--PREDICTIONREQUEST[I].PARAMARGS--" + predictionRequest[i].ParamArgs + "***CASCADED _START.ARGUMENTS***" + _start.Arguments + "--ParamArgs--" + predictionRequest[i].ParamArgs, string.IsNullOrEmpty(predictionRequest[i].CorrelationId) ? default(Guid) : new Guid(predictionRequest[i].CorrelationId), predictionRequest[i].AppID, string.Empty, predictionRequest[i].ClientID, predictionRequest[i].DeliveryconstructId);
                                                    }
                                                    else
                                                    {
                                                        _start.Arguments = string.Format("{0} {1} {2} {3} {4}", _pythonPYPath + "publishModel.py", predictionRequest[i].CorrelationId, predictionRequest[i].UniId, predictionRequest[i].pageInfo, predictionRequest[i].CreatedByUser);
                                                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "****PUBLISHMODEL*****", "--PREDICTIONREQUEST[I].PARAMARGS--" + predictionRequest[i].ParamArgs + "***NORMAL _START.ARGUMENTS***" + _start.Arguments + "--ParamArgs--" + predictionRequest[i].ParamArgs, string.IsNullOrEmpty(predictionRequest[i].CorrelationId) ? default(Guid) : new Guid(predictionRequest[i].CorrelationId), predictionRequest[i].AppID, string.Empty, predictionRequest[i].ClientID, predictionRequest[i].DeliveryconstructId);
                                                    }
                                                    InvokePython(predictionRequest[i], filterBuilder);
                                                    _queueManagerService.UpdateQueueMonitor();
                                                }
                                                break;
                                            case "PublishModelsTest":
                                                if (result[i].IsForAPI == false)
                                                {
                                                    _start.FileName = "\"" + _pythonExe + "\"";
                                                    _start.Arguments = string.Format("{0} {1} {2} {3} {4}", _pythonPYPath + "publishModel.py", predictionRequest[i].CorrelationId, predictionRequest[i].UniId, predictionRequest[i].pageInfo, predictionRequest[i].CreatedByUser);
                                                    InvokePython(predictionRequest[i], filterBuilder);
                                                    _queueManagerService.UpdateQueueMonitor();
                                                }
                                                break;

                                            case "ForecastModel":
                                                if (result[i].IsForAPI == false)
                                                {
                                                    _start.FileName = "\"" + _pythonExe + "\"";
                                                    _start.Arguments = string.Format("{0} {1} {2} {3} {4}", _pythonPYPath + "forecastModel.py", predictionRequest[i].CorrelationId, predictionRequest[i].UniId, predictionRequest[i].pageInfo, predictionRequest[i].CreatedByUser);
                                                    InvokePython(predictionRequest[i], filterBuilder);
                                                    _queueManagerService.UpdateQueueMonitor();
                                                }
                                                break;

                                        }
                                    }
                                    else
                                    {
                                        remainingRequests.Add(predictionRequest[i]);
                                    }


                                }
                            }
                            if (remainingRequests != null)
                            {
                                for (int i = 0; i < remainingRequests.Count; i++)
                                {
                                    QueueMonitor queue = _queueManagerService.GetQueueStatus();

                                    if (queue.QueueStatus == "Available")
                                    {
                                        Thread.Sleep(10);
                                        switch (remainingRequests[i].Function)
                                        {
                                            case "PublishModel":
                                                if (result[i].IsForAPI == false)
                                                {
                                                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "****PUBLISHMODEL2*****", "--remainingRequests[I].PARAMARGS--" + remainingRequests[i].ParamArgs, string.IsNullOrEmpty(remainingRequests[i].CorrelationId) ? default(Guid) : new Guid(remainingRequests[i].CorrelationId), remainingRequests[i].AppID, "", remainingRequests[i].ClientID, remainingRequests[i].DeliveryconstructId);
                                                    _start.FileName = "\"" + _pythonExe + "\"";
                                                    if (remainingRequests[i].ParamArgs == CONSTANTS.True)
                                                    {
                                                        _start.Arguments = string.Format("{0} {1} {2} {3} {4}", _pythonPYPath + "Cascaded.py", remainingRequests[i].CorrelationId, remainingRequests[i].UniId, remainingRequests[i].pageInfo, remainingRequests[i].CreatedByUser);
                                                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "****PUBLISHMODEL2*****", "--remainingRequests[I].PARAMARGS--" + remainingRequests[i].ParamArgs + "***CASCADED _START.ARGUMENTS***" + _start.Arguments + "--ParamArgs--" + remainingRequests[i].ParamArgs, string.IsNullOrEmpty(remainingRequests[i].CorrelationId) ? default(Guid) : new Guid(remainingRequests[i].CorrelationId), remainingRequests[i].AppID, "", remainingRequests[i].ClientID, remainingRequests[i].DeliveryconstructId);
                                                    }
                                                    else
                                                    {
                                                        _start.Arguments = string.Format("{0} {1} {2} {3} {4}", _pythonPYPath + "publishModel.py", remainingRequests[i].CorrelationId, remainingRequests[i].UniId, remainingRequests[i].pageInfo, remainingRequests[i].CreatedByUser);
                                                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "****PUBLISHMODE2L*****", "--remainingRequests[I].PARAMARGS--" + remainingRequests[i].ParamArgs + "***NORMAL _START.ARGUMENTS***" + _start.Arguments + "--ParamArgs--" + remainingRequests[i].ParamArgs, string.IsNullOrEmpty(remainingRequests[i].CorrelationId) ? default(Guid) : new Guid(remainingRequests[i].CorrelationId), remainingRequests[i].AppID, "", remainingRequests[i].ClientID, remainingRequests[i].DeliveryconstructId);
                                                    }
                                                    InvokePython(remainingRequests[i], filterBuilder);
                                                    _queueManagerService.UpdateQueueMonitor();
                                                    _start.Arguments = string.Format("{0} {1} {2} {3} {4}", _pythonPYPath + "Cascaded.py", remainingRequests[i].CorrelationId, remainingRequests[i].UniId, remainingRequests[i].pageInfo, remainingRequests[i].CreatedByUser);
                                                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "****PUBLISHMODEL2*****", "--remainingRequests[I].PARAMARGS--" + remainingRequests[i].ParamArgs + "***CASCADED _START.ARGUMENTS***" + _start.Arguments + "--ParamArgs--" + remainingRequests[i].ParamArgs, string.IsNullOrEmpty(remainingRequests[i].CorrelationId) ? default(Guid) : new Guid(remainingRequests[i].CorrelationId), remainingRequests[i].AppID, "", remainingRequests[i].ClientID, remainingRequests[i].DeliveryconstructId);
                                                }
                                                break;
                                            case "PublishModelsTest":
                                                if (result[i].IsForAPI == false)
                                                {
                                                    _start.FileName = "\"" + _pythonExe + "\"";
                                                    _start.Arguments = string.Format("{0} {1} {2} {3} {4}", _pythonPYPath + "publishModel.py", remainingRequests[i].CorrelationId, remainingRequests[i].UniId, remainingRequests[i].pageInfo, remainingRequests[i].CreatedByUser);
                                                    InvokePython(remainingRequests[i], filterBuilder);
                                                    _queueManagerService.UpdateQueueMonitor();
                                                }
                                                break;

                                            case "ForecastModel":
                                                if (result[i].IsForAPI == false)
                                                {
                                                    _start.FileName = "\"" + _pythonExe + "\"";
                                                    _start.Arguments = string.Format("{0} {1} {2} {3} {4}", _pythonPYPath + "forecastModel.py", remainingRequests[i].CorrelationId, remainingRequests[i].UniId, remainingRequests[i].pageInfo, remainingRequests[i].CreatedByUser);
                                                    InvokePython(remainingRequests[i], filterBuilder);
                                                    _queueManagerService.UpdateQueueMonitor();
                                                }
                                                break;
                                        }
                                    }
                                    else
                                    {
                                        _queueManagerService.UpdateQueueStatus(remainingRequests[i], filterBuilder);
                                        _queueManagerService.UpdatePublishModel(remainingRequests[i].CorrelationId, remainingRequests[i].UniId);
                                    }
                                }
                            }
                            #endregion
                        }
                        catch (Exception ex)
                        {
                            LOGGING.LogManager.Logger.LogErrorMessage(typeof(Worker), nameof(GetMessageRequestsTimeStamp), ex.StackTrace + "Exception" + ex.Message, ex, item != -1 ? result[item].CorrelationId : null, string.Empty, string.Empty, string.Empty);
                        }
                    }
                }

                timer.Start();
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(Worker), nameof(GetMessageRequestsTimeStamp), ex.StackTrace + "Exception" + ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
        }
        private void InvokePython(IngrainRequestQueue result, FilterDefinitionBuilder<IngrainRequestQueue> filterBuilder)
        {
            var filterCorrelation = filterBuilder.Eq("CorrelationId", result.CorrelationId) & filterBuilder.Eq("RequestId", result.RequestId) & (filterBuilder.Eq("RequestStatus", "New") | filterBuilder.Eq("RequestStatus", "Queued"));
            var update = Builders<IngrainRequestQueue>.Update.Set("RequestStatus", "Occupied").Set("PyTriggerTime", DateTime.Now.ToString());
            var isUpdated = _collection.UpdateOne(filterCorrelation, update);
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "INVOKEPYTHON END", "INVOKEPYTHON--MODIFIEDCOUNT--" + isUpdated.ModifiedCount + "-RequestID-" + result.RequestId, string.IsNullOrEmpty(result.CorrelationId) ? default(Guid) : new Guid(result.CorrelationId), result.AppID, string.Empty, result.ClientID, result.DeliveryconstructId);
            if (isUpdated.ModifiedCount > 0)
            {
                UpdateAndInvokePython(_start, result, filterBuilder);
            }
        }
        private void UpdateRequiredParamaterError(string requestId, FilterDefinitionBuilder<IngrainRequestQueue> filterBuilder)
        {
            var filterCorrelation = filterBuilder.Eq("RequestId", requestId);
            var update = Builders<IngrainRequestQueue>.Update.Set("RequestStatus", "Error").Set(x => x.Message, "Required paramter CorrelationId/CreatedByUser is null").Set(x => x.ModifiedOn, DateTime.Now.ToString()).Set(x => x.Status, "E");
            var isUpdated = _collection.UpdateOne(filterCorrelation, update);
        }
        private void UpdateAndInvokePython(ProcessStartInfo _start, IngrainRequestQueue result, FilterDefinitionBuilder<IngrainRequestQueue> filterBuilder)
        {

            LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "COMMANDLINE EXECUTION", "PYTHON CMD EXECUTION STARTED - PARAMS" + "ARGUEMENTS - " + _start.Arguments + "WorkingDirectory -" + _start.WorkingDirectory, string.IsNullOrEmpty(result.CorrelationId) ? default(Guid) : new Guid(result.CorrelationId), result.AppID, string.Empty, result.ClientID, result.DeliveryconstructId);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Thread trainingThread = new Thread(() => NewProcessAsync(_start, result, filterBuilder));
                trainingThread.Start();
                //NewProcessAsync(_start, result, filterBuilder);   
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                _start.FileName = _pythonExe;
                Thread trainingThread = new Thread(() => NewProcessAsync(_start, result, filterBuilder));
                trainingThread.Start();
                //NewProcessAsync(_start, result, filterBuilder);                
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "COMMANDLINE EXECUTION END -- PARAMS" + "ARGUEMENTS - " + _start.Arguments, "PYTHON CMD EXECUTION END", string.IsNullOrEmpty(result.CorrelationId) ? default(Guid) : new Guid(result.CorrelationId), result.AppID, string.Empty, result.ClientID, result.DeliveryconstructId);
        }
        void RunProcessAsync(ProcessStartInfo _start)
        {
            _start.WorkingDirectory = _workingDirectory;
            _start.UseShellExecute = false;         //Do not use OS shell
            _start.CreateNoWindow = true;           //We don't need new window
            _start.RedirectStandardOutput = true;   //Any output, generated by application will be redirected back
            _start.RedirectStandardError = true;    //Any error in standard output will be redirected back (for example exceptions)

            var processTask = new TaskCompletionSource<int>();

            var process = new Process
            {
                StartInfo = _start,
                EnableRaisingEvents = true,

            };

            process.Exited += (sender, args) =>
            {
                string standardoutput = process.StandardOutput.ReadToEnd();
                string standarderror = process.StandardError.ReadToEnd();
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "StandardError and StandardOutput", "StandardError: " + standarderror + ", StandardOutput: " + standardoutput, string.Empty, string.Empty, string.Empty, string.Empty);
                //LOGGING.LogManager.Logger.LogErrorMessage(typeof(Worker), nameof(RunProcessAsync), "StandardError and StandardOutput:- StandardError: " + standarderror + ", StandardOutput: " + standardoutput, new Exception(), string.Empty, string.Empty, string.Empty, string.Empty);
                processTask.SetResult(process.ExitCode);
                process.Dispose();
            };

            process.Start();

        }
        void NewProcessAsync(ProcessStartInfo _start, IngrainRequestQueue result, FilterDefinitionBuilder<IngrainRequestQueue> filterBuilder)
        {
            int timeout = appSettings.ProcessTimeout * 1000; // in milli sec
            _start.WorkingDirectory = _workingDirectory;
            _start.UseShellExecute = false;         //Do not use OS shell
            _start.CreateNoWindow = true;           //We don't need new window
            _start.RedirectStandardOutput = true;   //Any output, generated by application will be redirected back
            _start.RedirectStandardError = true;    //Any error in standard output will be redirected back (for example exceptions)

            //var processTask = new TaskCompletionSource<int>();
            using (AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
            using (AutoResetEvent errorWaitHandle = new AutoResetEvent(false))
            {
                using (Process process = new Process())
                {
                    process.StartInfo = _start;
                    process.EnableRaisingEvents = true;

                    StringBuilder output = new StringBuilder();
                    StringBuilder error = new StringBuilder();

                    try
                    {
                        process.OutputDataReceived += (sender, e) =>
                        {
                            if (e.Data == null)
                            {
                                outputWaitHandle.Set();
                            }
                            else
                            {
                                output.AppendLine(e.Data);
                            }
                        };
                        process.ErrorDataReceived += (sender, e) =>
                        {
                            if (e.Data == null)
                            {
                                errorWaitHandle.Set();
                            }
                            else
                            {
                                error.AppendLine(e.Data);
                            }
                        };

                        process.Start();
                        int processId = process.Id;
                        var filterCorrelation = filterBuilder.Eq("CorrelationId", result.CorrelationId) & filterBuilder.Eq("RequestId", result.RequestId);

                        var update = Builders<IngrainRequestQueue>.Update.Set(x => x.PythonProcessID, processId);
                        _collection.UpdateOne(filterCorrelation, update);
                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();

                        if (process.WaitForExit(timeout))
                        {
                            process.Exited += (sender, args) =>
                            {
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "StandardError and StandardOutput", "StandardError: " + error.ToString() + ", StandardOutput: " + output.ToString(), string.IsNullOrEmpty(result.CorrelationId) ? Guid.Empty : new Guid(result.CorrelationId), result.AppID, string.Empty, result.ClientID, result.DeliveryconstructId);
                                //processTask.SetResult(process.ExitCode);
                                process.Dispose();
                            };
                        }
                        else
                        {
                            var filterCorrelation1 = filterBuilder.Eq("CorrelationId", result.CorrelationId) & filterBuilder.Eq("RequestId", result.RequestId) & (filterBuilder.Eq("RequestStatus", "In - Progress") | filterBuilder.Eq("RequestStatus", "Occupied"));
                            var Projection2 = Builders<IngrainRequestQueue>.Projection.Exclude("_id");
                            var outcome = _collection.Find(filterCorrelation1).Project<IngrainRequestQueue>(Projection2).FirstOrDefault();
                            if (outcome != null && result.pageInfo == "RecommendedAI")
                            {
                                DateTime dateTime = DateTime.Parse(result.CreatedOn);
                                DateTime currentTime = DateTime.Parse(DateTime.Now.ToString(CONSTANTS.DateHoursFormat));
                                double _timeDiffInMinutes = (currentTime - dateTime).TotalMinutes;

                                var trainedModelCollection = _database.GetCollection<IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
                                var filter = Builders<IngrainRequestQueue>.Filter.Eq(CONSTANTS.CorrelationId, result.CorrelationId) & Builders<IngrainRequestQueue>.Filter.Eq(CONSTANTS.pageInfo, "RecommendedAI") & Builders<IngrainRequestQueue>.Filter.Eq(CONSTANTS.Status, CONSTANTS.C);
                                var completedModels = trainedModelCollection.Find(filter).ToList();
                                if (_timeDiffInMinutes > appSettings.ModelsTrainingTimeLimit && completedModels.Count > 0)
                                {
                                    GenericAutotrainService _genericAutotrainService = new GenericAutotrainService(databaseProvider);
                                    var terminateFilter = Builders<IngrainRequestQueue>.Filter.Eq(CONSTANTS.CorrelationId, result.CorrelationId) & Builders<IngrainRequestQueue>.Filter.Eq(CONSTANTS.pageInfo, "RecommendedAI");
                                    var terminateModels = trainedModelCollection.Find(terminateFilter).ToList();
                                    _genericAutotrainService.TerminateModelsTrainingRequests(result.CorrelationId, terminateModels);
                                }
                                else
                                {
                                    var update1 = Builders<IngrainRequestQueue>.Update.Set("RequestStatus", "Task Complete").Set("Message", "Process Timeout Error, processid -" + process.Id).Set("Status", "E").Set("ModifiedOn", DateTime.Now.ToString(CONSTANTS.DateHoursFormat));
                                    _collection.UpdateOne(filterCorrelation1, update1);
                                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "Process exited after timeout", string.Empty, string.IsNullOrEmpty(result.CorrelationId) ? default(Guid) : new Guid(result.CorrelationId), result.AppID, string.Empty, result.ClientID, result.DeliveryconstructId);
                                    process.Kill();
                                    process.Dispose();
                                }
                            }
                            else
                            {
                                var update1 = Builders<IngrainRequestQueue>.Update.Set("RequestStatus", "Task Complete").Set("Message", "Process Timeout Error, processid -" + process.Id).Set("Status", "E").Set("ModifiedOn", DateTime.Now.ToString(CONSTANTS.DateHoursFormat));
                                _collection.UpdateOne(filterCorrelation1, update1);
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "Process exited after timeout", string.Empty, string.IsNullOrEmpty(result.CorrelationId) ? default(Guid) : new Guid(result.CorrelationId), result.AppID, string.Empty, result.ClientID, result.DeliveryconstructId);
                                process.Kill();
                                process.Dispose();
                            }
                            // Timed out.
                        }
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "StandardError and StandardOutput", "StandardError: " + error.ToString() + ", StandardOutput: " + output.ToString(), string.IsNullOrEmpty(result.CorrelationId) ? Guid.Empty : new Guid(result.CorrelationId), result.AppID, string.Empty, result.ClientID, result.DeliveryconstructId);
                        //output = outputBuilder.ToString();
                    }
                    finally
                    {
                        outputWaitHandle.WaitOne(timeout);
                        errorWaitHandle.WaitOne(timeout);
                    }
                }
            }

        }
        void RunModelTrainingProcessAsync(ProcessStartInfo _start, IngrainRequestQueue result, FilterDefinitionBuilder<IngrainRequestQueue> filterBuilder)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "RunModelTrainingProcessAsync", "RunModelTrainingProcessAsync START", string.IsNullOrEmpty(result.CorrelationId) ? default(Guid) : new Guid(result.CorrelationId), result.AppID, string.Empty, result.ClientID, result.DeliveryconstructId);
            _start.WorkingDirectory = _workingDirectory;
            _start.UseShellExecute = false;         //Do not use OS shell
            _start.CreateNoWindow = true;           //We don't need new window
            _start.RedirectStandardOutput = true;   //Any output, generated by application will be redirected back
            _start.RedirectStandardError = true;    //Any error in standard output will be redirected back (for example exceptions)
            try
            {
                var processTask = new TaskCompletionSource<int>();

                var process = new Process
                {
                    StartInfo = _start,
                    EnableRaisingEvents = true,
                };
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "RunModelTrainingProcessAsync BEFORE PROCESSID", "INVOKEPYTHON--MODIFIEDCOUNT--" + "-RequestID-" + result.RequestId, string.IsNullOrEmpty(result.CorrelationId) ? default(Guid) : new Guid(result.CorrelationId), result.AppID, string.Empty, result.ClientID, result.DeliveryconstructId);
                process.Exited += (sender, args) =>
                {
                    string standardoutput = process.StandardOutput.ReadToEnd();
                    string standarderror = process.StandardError.ReadToEnd();
                    //LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "StandardError and StandardOutput", "StandardError: " + standarderror + ", StandardOutput: " + standardoutput, string.IsNullOrEmpty(result.CorrelationId) ? default(Guid) : new Guid(result.CorrelationId), result.AppID, string.Empty, result.ClientID, result.DeliveryconstructId);
                    LOGGING.LogManager.Logger.LogErrorMessage(typeof(Worker), nameof(RunModelTrainingProcessAsync), "StandardError and StandardOutput:- StandardError: " + standarderror + ", StandardOutput: " + standardoutput, new Exception(), string.Empty, string.Empty, string.Empty, string.Empty);
                    processTask.SetResult(process.ExitCode);
                    process.Dispose();
                };
                process.Start();
                int processId = process.Id;
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "RunModelTrainingProcessAsync AFTER PROCESSID", "INVOKEPYTHON--MODIFIEDCOUNT--" + "-RequestID-" + result.RequestId, string.IsNullOrEmpty(result.CorrelationId) ? default(Guid) : new Guid(result.CorrelationId), result.AppID, string.Empty, result.ClientID, result.DeliveryconstructId);
                var filterCorrelation = filterBuilder.Eq("CorrelationId", result.CorrelationId) & filterBuilder.Eq("RequestId", result.RequestId) & (filterBuilder.Eq("RequestStatus", "New") | filterBuilder.Eq("RequestStatus", "Queued"));
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "RunModelTrainingProcessAsync AFTER PROCESSID", "INVOKEPYTHON--MODIFIEDCOUNT--" + "-RequestID-" + result.RequestId, string.IsNullOrEmpty(result.CorrelationId) ? default(Guid) : new Guid(result.CorrelationId), result.AppID, string.Empty, result.ClientID, result.DeliveryconstructId);
                var update = Builders<IngrainRequestQueue>.Update.Set("RequestStatus", "Occupied").Set("PythonProcessID", processId);
                var isUpdated = _collection.UpdateOne(filterCorrelation, update);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(Worker), nameof(RunModelTrainingProcessAsync), ex.StackTrace + "Exception" + ex.Message, ex, result.AppID, string.Empty, result.ClientID, result.DeliveryconstructId);
            }
        }
        private void AutoTrainModel(IngrainRequestQueue result, FilterDefinitionBuilder<IngrainRequestQueue> filterBuilder)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "AutoTrainModel", "AutoTrainModel service started", string.IsNullOrEmpty(result.CorrelationId) ? default(Guid) : new Guid(result.CorrelationId), result.AppID, string.Empty, result.ClientID, result.DeliveryconstructId);
            var filterCorrelation = filterBuilder.Eq("CorrelationId", result.CorrelationId) & filterBuilder.Eq("RequestId", result.RequestId) & filterBuilder.Eq("RequestStatus", "New");
            var projection = Builders<IngrainRequestQueue>.Projection.Exclude(CONSTANTS.Id);
            GenericAutotrainService genericAutotrainService = new GenericAutotrainService(databaseProvider);
            GenericAutoTrain genericAutoTrain = new GenericAutoTrain();
            var results = _collection.Find(filterCorrelation).Project<IngrainRequestQueue>(projection).FirstOrDefault();
            if (results != null)
            {
                var update = Builders<IngrainRequestQueue>.Update.Set("RequestStatus", "Occupied").Set("Progress", "10%");
                var isUpdated = _collection.UpdateMany(filterCorrelation, update);
                Thread trainingThread = new Thread(() => genericAutotrainService.PrivateModelTraining(results));
                trainingThread.Start();
            }
        }
        private void CascadeAutoTrainModel(IngrainRequestQueue result, FilterDefinitionBuilder<IngrainRequestQueue> filterBuilder)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "CascadeAutoTrainModel", "CascadeAutoTrainModel service started", string.IsNullOrEmpty(result.CorrelationId) ? default(Guid) : new Guid(result.CorrelationId), result.AppID, string.Empty, result.ClientID, result.DeliveryconstructId);
            var filterCorrelation = filterBuilder.Eq("CorrelationId", result.CorrelationId) & filterBuilder.Eq("RequestId", result.RequestId) & filterBuilder.Eq("RequestStatus", "New");
            GenericAutotrainService genericAutotrainService = new GenericAutotrainService(databaseProvider);
            GenericAutoTrain genericAutoTrain = new GenericAutoTrain();
            var results = _collection.Find(filterCorrelation).FirstOrDefault();
            if (results != null)
            {
                var update = Builders<IngrainRequestQueue>.Update.Set("RequestStatus", "Occupied").Set("Progress", "10%");
                var isUpdated = _collection.UpdateMany(filterCorrelation, update);

                Thread trainingThread = new Thread(() => genericAutotrainService.PrivateCascadeModelTraining(results));
                trainingThread.Start();
            }
        }
        private void TransformingFMModel(IngrainRequestQueue result, FilterDefinitionBuilder<IngrainRequestQueue> filterBuilder)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "TransformingFMModel", "TransformingFMModel service started", string.IsNullOrEmpty(result.CorrelationId) ? default(Guid) : new Guid(result.CorrelationId), result.AppID, string.Empty, result.ClientID, result.DeliveryconstructId);
            var filterCorrelation = filterBuilder.Eq("CorrelationId", result.CorrelationId) & filterBuilder.Eq("RequestId", result.RequestId) & filterBuilder.Eq("RequestStatus", "New");
            GenericAutotrainService genericAutotrainService = new GenericAutotrainService(databaseProvider);
            GenericAutoTrain genericAutoTrain = new GenericAutoTrain();
            var results = _collection.Find(filterCorrelation).FirstOrDefault();
            if (results != null)
            {
                var update = Builders<IngrainRequestQueue>.Update.Set("RequestStatus", "Occupied").Set("Progress", "10%");
                var isUpdated = _collection.UpdateMany(filterCorrelation, update);

                Thread fmtrainingThread = new Thread(() => genericAutotrainService.TransformFMModel(results));
                fmtrainingThread.Start();
            }
        }
        //private void testTransformingFMModel()
        //{
        //    var filterBuilder = Builders<IngrainRequestQueue>.Filter;
        //    LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "TransformingFMModel", "TransformingFMModel service started");
        //    var filterCorrelation = filterBuilder.Eq("CorrelationId", "634f7c74-7180-4bb8-bef2-49e211c688cd") & filterBuilder.Eq("pageInfo", "FMTransform");
        //    GenericAutotrainService genericAutotrainService = new GenericAutotrainService(databaseProvider);
        //    GenericAutoTrain genericAutoTrain = new GenericAutoTrain();
        //    var results = _collection.Find(filterCorrelation).FirstOrDefault();
        //    if (results != null)
        //    {
        //        var update = Builders<IngrainRequestQueue>.Update.Set("RequestStatus", "Occupied").Set("Progress", "10%");
        //        var isUpdated = _collection.UpdateMany(filterCorrelation, update);

        //        Thread fmtrainingThread = new Thread(() => genericAutotrainService.TransformFMModel(results));
        //        fmtrainingThread.Start();
        //    }
        //}
        private void GetPrediction(IngrainRequestQueue result, FilterDefinitionBuilder<IngrainRequestQueue> filterBuilder)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "GetPrediction", "GetPrediction service started", string.IsNullOrEmpty(result.CorrelationId) ? default(Guid) : new Guid(result.CorrelationId), result.AppID, string.Empty, result.ClientID, result.DeliveryconstructId);
            var filterCorrelation = filterBuilder.Eq("CorrelationId", result.CorrelationId) & filterBuilder.Eq("RequestId", result.RequestId);
            GenericAutotrainService genericAutotrainService = new GenericAutotrainService(databaseProvider);
            GenericAutoTrain genericAutoTrain = new GenericAutoTrain();
            var results = _collection.Find(filterCorrelation).FirstOrDefault();
            if (results != null)
            {
                Thread trainingThread = new Thread(() => genericAutotrainService.GetPredictionData(results));
                trainingThread.Start();
            }
        }
        private void InstaAutoRetrain()
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), nameof(Worker), "AUTORETRAIN  START ", string.Empty, string.Empty, string.Empty, string.Empty);
            string correlationId = string.Empty;
            try
            {
                List<BsonDocument> result = new List<BsonDocument>();
                var collection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
                DateTime triggerDate = new DateTime();
                //DateTime triggerDate = DateTime.Now.AddDays(Convert.ToDouble(appSettings.InstaAutoDays));
                //string dateFilter = triggerDate.ToString("yyyy-MM-dd");
                var filterBuilder = Builders<BsonDocument>.Filter;
                var regressionBuilder = Builders<DeployModelsDto>.Filter;
                //var filter = filterBuilder.Eq("CorrelationId", "8c252103-b115-4884-ae31-cc7667bfb13a");
                var filter = filterBuilder.Eq("Status", "Deployed") & filterBuilder.Eq(CONSTANTS.IsCarryOutRetraining, true)
                             //& filterBuilder.Lt("ModifiedOn", dateFilter)
                             & (filterBuilder.Ne("InstaId", BsonNull.Value)
                             & filterBuilder.Ne("ModelType", CONSTANTS.Regression) & filterBuilder.Eq("UseCaseID", BsonNull.Value));

                string empty = null;
                //var filter2 = regressionBuilder.Eq("UseCaseID", "208");
                var filter2 = regressionBuilder.Eq(CONSTANTS.Status, CONSTANTS.Deployed) & regressionBuilder.Eq(CONSTANTS.IsCarryOutRetraining, true)
                    //& regressionBuilder.Lt("ModifiedOn", dateFilter)
                    & regressionBuilder.Ne(CONSTANTS.UseCaseID, BsonNull.Value)
                    & regressionBuilder.Ne(CONSTANTS.UseCaseID, empty)
                    & regressionBuilder.Ne(CONSTANTS.InstaId, BsonNull.Value)
                    & regressionBuilder.Ne(CONSTANTS.InstaId, empty);
                var collectionResult = _deployModelCollection.Find(filter).ToList();
                var regressionResult = collection.Find(filter2).SortByDescending(bson => bson.UseCaseID).Project<DeployModelsDto>(Builders<DeployModelsDto>.Projection.Exclude(CONSTANTS.Id)).ToList();
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker)
                                                            , nameof(Worker)
                                                            , "--TOTAL MODELS COUNT-- " + collectionResult.Count + "-- TRIGGER DATE --" + Convert.ToString(triggerDate), string.Empty, string.Empty, string.Empty, string.Empty);
                for (int i = 0; i < collectionResult.Count; i++)
                {
                    try
                    {
                        string modifiedOn = collectionResult[i]["ModifiedOn"].ToString();
                        DateTime modifiedDate = DateTime.Parse(modifiedOn);
                        triggerDate = modifiedDate.AddDays(Convert.ToInt32(collectionResult[i][CONSTANTS.RetrainingFrequencyInDays]));
                        //if (modifiedDate < triggerDate)
                        if (DateTime.Now >= triggerDate)
                        {
                            result.Add(collectionResult[i]);
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker)
                                                                 , nameof(InstaAutoRetrain)
                                                                 , "--CORRELATIONID--" + Convert.ToString(collectionResult[i][CONSTANTS.CorrelationId]), string.Empty, string.Empty, string.Empty, string.Empty);
                        }
                    }
                    catch (Exception ex)
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker)
                                                           , nameof(InstaAutoRetrain)
                                                           , "--TOTAL MODELS COUNT-- " + collectionResult.Count + "-- TRIGGER DATE --" + Convert.ToString(triggerDate), string.Empty, string.Empty, string.Empty, string.Empty);
                    }

                }
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker)
                                                             , nameof(InstaAutoRetrain)
                                                             , "-- FILTERED MODELS COUNT-- " + result.Count, string.Empty, string.Empty, string.Empty, string.Empty);
                for (int i = 0; i < result.Count; i++)
                {
                    try
                    {

                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker)
                                                                 , nameof(InstaAutoRetrain)
                                                                 , "--FOR LOOP CORRELATIONID START--" + Convert.ToString(result[i][CONSTANTS.CorrelationId]), string.Empty, string.Empty, string.Empty, string.Empty);
                        correlationId = result[i][CONSTANTS.CorrelationId].ToString();
                        instaRetrain = _instaAutoRetrainService.IngestData(result[i]);
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker)
                                                                 , nameof(InstaAutoRetrain)
                                                                 , "--INGESTDATA END STATUS--" + instaRetrain.Status + "--MESSAGE--" + instaRetrain.Message, string.IsNullOrEmpty(Convert.ToString(result[i][CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[i][CONSTANTS.CorrelationId])), string.Empty, string.Empty, string.Empty, string.Empty);
                        if (instaRetrain.Status == "C")
                        {
                            instaRetrain = _instaAutoRetrainService.GetInstaAutoDataEngineering(result[i]);
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker)
                                                                     , nameof(InstaAutoRetrain)
                                                                     , "--DATEENGINEERING END STATUS--" + instaRetrain.Status + "--MESSAGE--" + instaRetrain.Message, string.IsNullOrEmpty(Convert.ToString(result[i][CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[i][CONSTANTS.CorrelationId])), string.Empty, string.Empty, string.Empty, string.Empty);
                        }
                        if (instaRetrain.Status == "C")
                        {
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "AUTO TRAIN MODELENGINEERING", "STARTED", string.IsNullOrEmpty(Convert.ToString(result[i][CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[i][CONSTANTS.CorrelationId])), string.Empty, string.Empty, string.Empty, string.Empty);
                            instaRetrain = _instaAutoRetrainService.GetInstaAutoModelEngineering(result[i]);
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), nameof(InstaAutoRetrain), "--MODEL ENGINEERING END STATUS--" + instaRetrain.Status, string.IsNullOrEmpty(Convert.ToString(result[i][CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[i][CONSTANTS.CorrelationId])), string.Empty, string.Empty, string.Empty, string.Empty);
                        }
                        if (instaRetrain.Status == "C")
                        {
                            _instaAutoRetrainService.GetInstaAutoDeployPrediction(result[i]);
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), nameof(InstaAutoRetrain), "--DEPLOY MODEL END--" + instaRetrain.Status, string.IsNullOrEmpty(Convert.ToString(result[i][CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[i][CONSTANTS.CorrelationId])), string.Empty, string.Empty, string.Empty, string.Empty);
                        }
                    }
                    catch (Exception ex)
                    {
                        LOGGING.LogManager.Logger.LogErrorMessage(typeof(Worker), nameof(InstaAutoRetrain), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                    }
                }
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), nameof(InstaAutoRetrain), "REGRESSION STARTING  START ", string.Empty, string.Empty, string.Empty, string.Empty);
                regressionRetrain = _instaRegressionAutoTrainService.StartRegressionModelTraining(regressionResult);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(Worker), nameof(Worker), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), nameof(Worker), "AUTORETRAIN END-", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
        }
        private void EntityAutoRetrain()
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), nameof(Worker), "EntityAutoRetrain  START ", string.Empty, string.Empty, string.Empty, string.Empty);

            var entityTask = _database.GetCollection<AutoReTrainTasks>("AutoReTrainTasks");
            var taskFilter = Builders<AutoReTrainTasks>.Filter.Where(x => x.TaskCode == "ENTITY AUTORETRAIN");
            var tskProj = Builders<AutoReTrainTasks>.Projection.Exclude("_id");
            var tskRes = entityTask.Find(taskFilter).Project<AutoReTrainTasks>(tskProj).FirstOrDefault();

            string correlationId = string.Empty;
            try
            {
                List<BsonDocument> result = new List<BsonDocument>();
                var collection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
                //DateTime triggerDate = DateTime.Now.AddDays(Convert.ToDouble(tskRes.FrequencyInDays));
                DateTime triggerDate = new DateTime();
                //string dateFilter = triggerDate.ToString("yyyy-MM-dd");
                var filterBuilder = Builders<BsonDocument>.Filter;
                FilterDefinition<BsonDocument> filter = null;
                if (tskRes.ManualTrain)
                {
                    List<string> corrIds = tskRes.CorrelationIds.Split(",").ToList();
                    filter = filterBuilder.In("CorrelationId", corrIds) & filterBuilder.Eq(CONSTANTS.IsCarryOutRetraining, true);
                }
                else
                {
                    filter = filterBuilder.Eq("Status", "Deployed") & filterBuilder.Eq(CONSTANTS.IsCarryOutRetraining, true)
                             //& filterBuilder.Lt("ModifiedOn", dateFilter)
                             & ((filterBuilder.Eq("SourceName", "pad") & filterBuilder.Ne("AppId", "5354e9b8-0eb1-4275-b666-6eba15c12b2c"))
                             | filterBuilder.Eq("SourceName", "metric") | filterBuilder.Eq("SourceName", "multisource") | (filterBuilder.Eq("SourceName", "Custom") & filterBuilder.Eq("DataSource", "Phoenix CDM")) | filterBuilder.Eq("SourceName", "Custom")
                             | (filterBuilder.Eq("DataSource", CONSTANTS.Custom) & filterBuilder.Ne("AppId", "89d56036-d70e-48ee-83df-303e493b0c36")));
                }


                string empty = null;

                var collectionResult = _deployModelCollection.Find(filter).ToList();
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker)
                                                            , nameof(Worker)
                                                            , "--TOTAL MODELS COUNT-- " + collectionResult.Count + "-- TRIGGER DATE --" + Convert.ToString(triggerDate), string.Empty, string.Empty, string.Empty, string.Empty);

                var logCollection = _database.GetCollection<InstaLog>("Insta_AutoLog");
                for (int i = 0; i < collectionResult.Count; i++)
                {
                    try
                    {
                        string modifiedOn = collectionResult[i]["ModifiedOn"].ToString();
                        DateTime modifiedDate = DateTime.Parse(modifiedOn);
                        triggerDate = modifiedDate.AddDays(Convert.ToInt32(collectionResult[i][CONSTANTS.RetrainingFrequencyInDays]));

                        if (DateTime.Now >= triggerDate)
                        {
                            result.Add(collectionResult[i]);
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker)
                                                                 , nameof(Worker)
                                                                 , "--CORRELATIONID--" + Convert.ToString(collectionResult[i][CONSTANTS.CorrelationId]), string.Empty, string.Empty, string.Empty, string.Empty);
                        }
                    }
                    catch (Exception ex)
                    {
                        var instaLog = new InstaLog(correlationId, Convert.ToString(result[i]["ModelName"]), "", "Error - EntityAutoRetrain Block 1", ex.Message, Convert.ToString(result[i]["CreatedByUser"]));
                        logCollection.InsertOne(instaLog);
                    }

                }
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker)
                                                             , nameof(Worker)
                                                             , "-- FILTERED MODELS COUNT-- " + result.Count, string.Empty, string.Empty, string.Empty, string.Empty);

                for (int i = 0; i < result.Count; i++)
                {

                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker)
                                                             , nameof(Worker)
                                                             , "--FOR LOOP CORRELATIONID START--" + Convert.ToString(result[i][CONSTANTS.CorrelationId]), string.Empty, string.Empty, string.Empty, string.Empty);
                    try
                    {
                        correlationId = result[i][CONSTANTS.CorrelationId].ToString();
                        var instaLog = new InstaLog(correlationId, Convert.ToString(result[i]["ModelName"]), "IngestData", "Start", "", Convert.ToString(result[i]["CreatedByUser"]));
                        logCollection.InsertOne(instaLog);

                        instaRetrain = _instaAutoRetrainService.IngestData(result[i]);
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker)
                                                                 , nameof(Worker)
                                                                 , "--INGESTDATA END STATUS--" + instaRetrain.Status + "--MESSAGE--" + instaRetrain.Message, string.IsNullOrEmpty(Convert.ToString(result[i][CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[i][CONSTANTS.CorrelationId])), string.Empty, string.Empty, string.Empty, string.Empty);
                        if (instaRetrain.Status == "C")
                        {
                            instaRetrain = _instaAutoRetrainService.GetInstaAutoDataEngineering(result[i]);
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker)
                                                                     , nameof(Worker)
                                                                     , "--DATEENGINEERING END STATUS--" + instaRetrain.Status + "--MESSAGE--" + instaRetrain.Message, string.IsNullOrEmpty(Convert.ToString(result[i][CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[i][CONSTANTS.CorrelationId])), string.Empty, string.Empty, string.Empty, string.Empty);
                        }
                        if (instaRetrain.Status == "C")
                        {
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "AUTO TRAIN MODELENGINEERING", "STARTED", string.IsNullOrEmpty(Convert.ToString(result[i][CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[i][CONSTANTS.CorrelationId])), string.Empty, string.Empty, string.Empty, string.Empty);
                            instaRetrain = _instaAutoRetrainService.GetInstaAutoModelEngineering(result[i]);
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), nameof(Worker), "--MODEL ENGINEERING END STATUS--" + instaRetrain.Status, string.IsNullOrEmpty(Convert.ToString(result[i][CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[i][CONSTANTS.CorrelationId])), string.Empty, string.Empty, string.Empty, string.Empty);
                        }
                        if (instaRetrain.Status == "C")
                        {
                            _instaAutoRetrainService.GetInstaAutoDeployPrediction(result[i]);
                            instaLog = new InstaLog(correlationId, Convert.ToString(result[i]["ModelName"]), "DeployModel", "End", "", Convert.ToString(result[i]["CreatedByUser"]));
                            logCollection.InsertOne(instaLog);
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), nameof(Worker), "--DEPLOY MODEL END--" + instaRetrain.Status, string.IsNullOrEmpty(Convert.ToString(result[i][CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[i][CONSTANTS.CorrelationId])), string.Empty, string.Empty, string.Empty, string.Empty);
                        }
                    }
                    catch (Exception ex)
                    {
                        var instaLog = new InstaLog(correlationId, Convert.ToString(result[i]["ModelName"]), "", "Error - EntityAutoRetrain Block 2", ex.Message, Convert.ToString(result[i]["CreatedByUser"]));
                        logCollection.InsertOne(instaLog);
                    }

                }

            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(Worker), nameof(Worker), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), nameof(Worker), "AUTORETRAIN END-", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
        }
        public List<Service> GetAllAIServices()
        {
            List<Service> serviceList = new List<Service>();
            var serviceCollection = _database.GetCollection<BsonDocument>("Services");
            var filter = Builders<BsonDocument>.Filter.Empty;
            var projection = Builders<BsonDocument>.Projection.Exclude("_id");
            var result = serviceCollection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                serviceList = JsonConvert.DeserializeObject<List<Service>>(result.ToJson());
            }

            return serviceList;
        }
        private void AIServicesAutoTrain()
        {
            try
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIServicesRetrain), nameof(AIServicesAutoTrain), "START", string.Empty, string.Empty, string.Empty, string.Empty);
                string aiServiceAutodays = string.Empty;
                if (!string.IsNullOrEmpty(appSettings.AIManualTrigger) && appSettings.AIManualTrigger.ToLower() == "yes")
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "AI Service Manual Trigger started", "AI Service Manual Trigger started -" + DateTime.Now.ToString(), string.Empty, string.Empty, string.Empty, string.Empty);
                    aiServiceAutodays = "-1";
                }
                else
                {
                    aiServiceAutodays = appSettings.AIAutoTrainDays;
                }
                string aIScrumAutoTrainDays = appSettings.AIScrumAutoTrainDays;
                DateTime scrumbanMonths = DateTime.Now.AddHours(Convert.ToDouble(aIScrumAutoTrainDays));
                string scrumbanDateFilter = scrumbanMonths.ToString("yyyy-MM-dd HH:mm:ss");
                DateTime months = DateTime.Now.AddHours(Convert.ToDouble(aiServiceAutodays));


                string dateFilter = months.ToString("yyyy-MM-dd HH:mm:ss");
                var filterBuilder = Builders<BsonDocument>.Filter;
                try
                {

                    var collection = _database.GetCollection<BsonDocument>(CONSTANTS.AI_CoreModels);
                    //Online retrain models
                    var AIfreqfilter = filterBuilder.Eq("ServiceId", "93df37dc-cc72-4105-9ad2-fd08509bc823") & filterBuilder.Eq(CONSTANTS.ModelStatus, CONSTANTS.Completed) & filterBuilder.Eq(CONSTANTS.IsCarryOutRetraining, true) & (filterBuilder.Eq(CONSTANTS.DataSource, CONSTANTS.Entity) | filterBuilder.Eq(CONSTANTS.DataSource, "Custom")) & filterBuilder.Ne(CONSTANTS.UsecaseId, "6665e35b-b2d1-40cc-b28f-3b795780f34f") & filterBuilder.Ne("ApplicationId", "89d56036-d70e-48ee-83df-303e493b0c36");
                    var AIServiceResult = collection.Find(AIfreqfilter).Sort(Builders<BsonDocument>.Sort.Descending("CreatedOn")).ToList();
                    DateTime triggerDate = new DateTime();
                    if (AIServiceResult.Count > 0)
                    {
                        for (int i = 0; i < AIServiceResult.Count; i++)
                        {
                            string modifiedOn = AIServiceResult[i]["ModifiedOn"].ToString();
                            DateTime modifiedDate = DateTime.Parse(modifiedOn);
                            triggerDate = modifiedDate.AddDays(Convert.ToInt32(AIServiceResult[i][CONSTANTS.RetrainingFrequencyInDays]));
                            if (DateTime.Now >= triggerDate)
                            {
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIServicesRetrain), nameof(AIServicesAutoTrain), "START :" + "SIMILARITYANALYTICS", string.Empty, string.Empty, string.Empty, string.Empty);
                                bool IsSuccess = false;
                                DataModels.AICore.IngestData data = new DataModels.AICore.IngestData();
                                data = AIServices.IngestData(AIServiceResult[i][CONSTANTS.CorrelationId].ToString());
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIServicesRetrain), nameof(AIServicesAutoTrain), "INGESTDATACOMPLETED--" + data.IsIngestionCompleted, string.Empty, string.Empty, string.Empty, string.Empty);
                                if (data.IsIngestionCompleted & data.ServiceCode != CONSTANTS.DEVELOPERPREDICTION)
                                {
                                    IsSuccess = false;
                                    IsSuccess = AIServices.AIServicesTraining(Convert.ToString(AIServiceResult[i][CONSTANTS.CorrelationId]));
                                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIServicesRetrain), nameof(AIServicesAutoTrain), "END", string.Empty, string.Empty, string.Empty, string.Empty);
                                }
                            }
                        }
                    }

                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIServicesRetrain), nameof(AIServicesAutoTrain), "--Similarity Devloper Prediction Retrain end--", string.Empty, string.Empty, string.Empty, string.Empty);
                }
                catch (Exception ex)
                {
                    LOGGING.LogManager.Logger.LogErrorMessage(typeof(Worker), nameof(AIServicesAutoTrain), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(Worker), nameof(AIServicesAutoTrain), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
        }
        private void AIScrumbanAutoTrain()
        {
            try
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIServicesRetrain), nameof(AIServicesAutoTrain), "START", string.Empty, string.Empty, string.Empty, string.Empty);
                string aiServiceAutodays = string.Empty;
                if (!string.IsNullOrEmpty(appSettings.AIManualTrigger) && appSettings.AIManualTrigger.ToLower() == "yes")
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "AI Service Manual Trigger started", "AI Service Manual Trigger started -" + DateTime.Now.ToString(), string.Empty, string.Empty, string.Empty, string.Empty);
                    aiServiceAutodays = "-1";
                }
                else
                {
                    aiServiceAutodays = appSettings.AIAutoTrainDays;
                }
                string aIScrumAutoTrainDays = appSettings.AIScrumAutoTrainDays;
                DateTime scrumbanMonths = DateTime.Now.AddHours(Convert.ToDouble(aIScrumAutoTrainDays));
                string scrumbanDateFilter = scrumbanMonths.ToString("yyyy-MM-dd HH:mm:ss");
                DateTime months = DateTime.Now.AddHours(Convert.ToDouble(aiServiceAutodays));


                string dateFilter = months.ToString("yyyy-MM-dd HH:mm:ss");
                var filterBuilder = Builders<BsonDocument>.Filter;
                try
                {

                    var collection = _database.GetCollection<BsonDocument>(CONSTANTS.AI_CoreModels);
                    //Online retrain models
                    var AIfreqfilter = filterBuilder.Eq("ServiceId", "93df37dc-cc72-4105-9ad2-fd08509bc823") & filterBuilder.Eq(CONSTANTS.ModelStatus, CONSTANTS.Completed) & filterBuilder.Eq(CONSTANTS.IsCarryOutRetraining, true) & filterBuilder.Eq(CONSTANTS.UsecaseId, "6665e35b-b2d1-40cc-b28f-3b795780f34f");
                    var AIServiceResult = collection.Find(AIfreqfilter).Sort(Builders<BsonDocument>.Sort.Descending("CreatedOn")).ToList();
                    DateTime triggerDate = new DateTime();
                    if (AIServiceResult.Count > 0)
                    {
                        for (int i = 0; i < AIServiceResult.Count; i++)
                        {
                            string modifiedOn = AIServiceResult[i]["ModifiedOn"].ToString();
                            DateTime modifiedDate = DateTime.Parse(modifiedOn);
                            triggerDate = modifiedDate.AddDays(Convert.ToInt32(AIServiceResult[i][CONSTANTS.RetrainingFrequencyInDays]));
                            if (DateTime.Now >= triggerDate)
                            {
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIServicesRetrain), nameof(AIServicesAutoTrain), "START :" + "SIMILARITYANALYTICS", string.Empty, string.Empty, string.Empty, string.Empty);
                                bool IsSuccess = false;
                                DataModels.AICore.IngestData data = new DataModels.AICore.IngestData();
                                data = AIServices.IngestData(AIServiceResult[i][CONSTANTS.CorrelationId].ToString());
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIServicesRetrain), nameof(AIServicesAutoTrain), "INGESTDATACOMPLETED--" + data.IsIngestionCompleted, string.Empty, string.Empty, string.Empty, string.Empty);
                                if (data.IsIngestionCompleted & data.ServiceCode != CONSTANTS.DEVELOPERPREDICTION)
                                {
                                    IsSuccess = false;
                                    IsSuccess = AIServices.AIServicesTraining(Convert.ToString(AIServiceResult[i][CONSTANTS.CorrelationId]));
                                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIServicesRetrain), nameof(AIServicesAutoTrain), "END", string.Empty, string.Empty, string.Empty, string.Empty);
                                }
                            }
                        }
                    }

                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIServicesRetrain), nameof(AIServicesAutoTrain), "--Similarity Devloper Prediction Retrain end--", string.Empty, string.Empty, string.Empty, string.Empty);
                }
                catch (Exception ex)
                {
                    LOGGING.LogManager.Logger.LogErrorMessage(typeof(Worker), nameof(AIServicesAutoTrain), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(Worker), nameof(AIServicesAutoTrain), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
        }
        private void IASimilarModelAutoTrain()
        {
            try
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIServicesRetrain), nameof(AIServicesAutoTrain), "START", string.Empty, string.Empty, string.Empty, string.Empty);
                string aiServiceAutodays = string.Empty;
                if (!string.IsNullOrEmpty(appSettings.AIManualTrigger) && appSettings.AIManualTrigger.ToLower() == "yes")
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "AI Service Manual Trigger started", "AI Service Manual Trigger started -" + DateTime.Now.ToString(), string.Empty, string.Empty, string.Empty, string.Empty);
                    aiServiceAutodays = "-1";
                }
                else
                {
                    aiServiceAutodays = appSettings.AIAutoTrainDays;
                }
                string aIScrumAutoTrainDays = appSettings.AIScrumAutoTrainDays;
                DateTime scrumbanMonths = DateTime.Now.AddHours(Convert.ToDouble(aIScrumAutoTrainDays));
                string scrumbanDateFilter = scrumbanMonths.ToString("yyyy-MM-dd HH:mm:ss");
                DateTime months = DateTime.Now.AddHours(Convert.ToDouble(aiServiceAutodays));


                string dateFilter = months.ToString("yyyy-MM-dd HH:mm:ss");
                var filterBuilder = Builders<BsonDocument>.Filter;
                try
                {

                    var collection = _database.GetCollection<BsonDocument>(CONSTANTS.AI_CoreModels);
                    //Online retrain models
                    var AIfreqfilter = filterBuilder.Eq("ServiceId", "93df37dc-cc72-4105-9ad2-fd08509bc823") & filterBuilder.Eq(CONSTANTS.ModelStatus, CONSTANTS.Completed) & filterBuilder.Eq(CONSTANTS.IsCarryOutRetraining, true) & filterBuilder.Eq("ApplicationId", "89d56036-d70e-48ee-83df-303e493b0c36");
                    var AIServiceResult = collection.Find(AIfreqfilter).Sort(Builders<BsonDocument>.Sort.Descending("CreatedOn")).ToList();
                    DateTime triggerDate = new DateTime();
                    if (AIServiceResult.Count > 0)
                    {
                        for (int i = 0; i < AIServiceResult.Count; i++)
                        {
                            string modifiedOn = AIServiceResult[i]["ModifiedOn"].ToString();
                            DateTime modifiedDate = DateTime.Parse(modifiedOn);
                            triggerDate = modifiedDate.AddDays(Convert.ToInt32(AIServiceResult[i][CONSTANTS.RetrainingFrequencyInDays]));
                            if (DateTime.Now >= triggerDate)
                            {
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIServicesRetrain), nameof(AIServicesAutoTrain), "START :" + "SIMILARITYANALYTICS", string.Empty, string.Empty, string.Empty, string.Empty);
                                bool IsSuccess = false;
                                DataModels.AICore.IngestData data = new DataModels.AICore.IngestData();
                                data = AIServices.IngestData(AIServiceResult[i][CONSTANTS.CorrelationId].ToString());
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIServicesRetrain), nameof(AIServicesAutoTrain), "INGESTDATACOMPLETED--" + data.IsIngestionCompleted, string.Empty, string.Empty, string.Empty, string.Empty);
                                if (data.IsIngestionCompleted & data.ServiceCode != CONSTANTS.DEVELOPERPREDICTION)
                                {
                                    IsSuccess = false;
                                    IsSuccess = AIServices.AIServicesTraining(Convert.ToString(AIServiceResult[i][CONSTANTS.CorrelationId]));
                                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIServicesRetrain), nameof(AIServicesAutoTrain), "END", string.Empty, string.Empty, string.Empty, string.Empty);
                                }
                            }
                        }
                    }

                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIServicesRetrain), nameof(AIServicesAutoTrain), "--Similarity Devloper Prediction Retrain end--", string.Empty, string.Empty, string.Empty, string.Empty);
                }
                catch (Exception ex)
                {
                    LOGGING.LogManager.Logger.LogErrorMessage(typeof(Worker), nameof(AIServicesAutoTrain), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(Worker), nameof(AIServicesAutoTrain), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
        }
        private void ClusteringAutoTrain()
        {
            try
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIServicesRetrain), nameof(AIServicesAutoTrain), "START", string.Empty, string.Empty, string.Empty, string.Empty);
                string aiServiceAutodays = string.Empty;
                if (!string.IsNullOrEmpty(appSettings.AIManualTrigger) && appSettings.AIManualTrigger.ToLower() == "yes")
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "AI Service Manual Trigger started", "AI Service Manual Trigger started -" + DateTime.Now.ToString(), string.Empty, string.Empty, string.Empty, string.Empty);
                    aiServiceAutodays = "-1";
                }
                else
                {
                    aiServiceAutodays = appSettings.AIAutoTrainDays;
                }
                string aIScrumAutoTrainDays = appSettings.AIScrumAutoTrainDays;
                DateTime scrumbanMonths = DateTime.Now.AddHours(Convert.ToDouble(aIScrumAutoTrainDays));
                string scrumbanDateFilter = scrumbanMonths.ToString("yyyy-MM-dd HH:mm:ss");
                DateTime months = DateTime.Now.AddHours(Convert.ToDouble(aiServiceAutodays));

                string dateFilter = months.ToString("yyyy-MM-dd HH:mm:ss");
                var filterBuilder = Builders<BsonDocument>.Filter;
                try
                {
                    var ClusterCollection = _database.GetCollection<BsonDocument>(CONSTANTS.Clustering_StatusTable);
                    var ClusterIngestCollection = _database.GetCollection<BsonDocument>(CONSTANTS.Clustering_IngestData);
                    var ClusterfilterBuilder = Builders<BsonDocument>.Filter;

                    //Online retrain models
                    List<BsonDocument> ClusterIngestfreqResult = new List<BsonDocument>();
                    DateTime triggerDate = new DateTime();
                    var statusfilter = ClusterfilterBuilder.Eq(CONSTANTS.Status, CONSTANTS.C) & ClusterfilterBuilder.Eq(CONSTANTS.pageInfo, CONSTANTS.ModelTraining);
                    var ClusterModelsResult = ClusterCollection.Find(statusfilter).ToList();
                    ClusterModelsResult = ClusterModelsResult.Distinct().ToList();
                    if (ClusterModelsResult.Count > 0)
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIServicesRetrain), nameof(AIServicesAutoTrain), "START :" + "CLUSTERING", string.Empty, string.Empty, string.Empty, string.Empty);
                        for (int i = 0; i < ClusterModelsResult.Count; i++)
                        {
                            var corrId = ClusterModelsResult[i][CONSTANTS.CorrelationId].ToString();
                            var ClusterIngestFilter = ClusterfilterBuilder.Eq("ServiceID", "72c38b39-c9fe-4fa6-97f5-6c3adc6ba355") & (ClusterfilterBuilder.Eq(CONSTANTS.DataSource, CONSTANTS.Entity) | ClusterfilterBuilder.Eq(CONSTANTS.DataSource, "Custom")) & ClusterfilterBuilder.Eq(CONSTANTS.CorrelationId, corrId) & ClusterfilterBuilder.Eq(CONSTANTS.IsCarryOutRetraining, true);
                            var ClusterIngestProjection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id).Include("CorrelationId").Include("ModifiedOn").Include("RetrainingFrequencyInDays");
                            var ClusterIngestResult = ClusterIngestCollection.Find(ClusterIngestFilter).Project<BsonDocument>(ClusterIngestProjection).ToList();
                            if (ClusterIngestResult.Count > 0)
                            {
                                for (int j = 0; j < ClusterIngestResult.Count; j++)
                                {
                                    string modifiedOn = ClusterIngestResult[j]["ModifiedOn"].ToString();
                                    DateTime modifiedDate = DateTime.Parse(modifiedOn);
                                    triggerDate = modifiedDate.AddDays(Convert.ToInt32(ClusterIngestResult[j][CONSTANTS.RetrainingFrequencyInDays]));
                                    if (DateTime.Now >= triggerDate)
                                    {
                                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIServicesRetrain), nameof(AIServicesAutoTrain), "CLUSTER Ingest Read START", string.Empty, string.Empty, string.Empty, string.Empty);
                                        bool IsSuccess = false;
                                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIServicesRetrain), nameof(AIServicesAutoTrain), "CLUSTER MODEL START--" + Convert.ToString(ClusterIngestResult[j][CONSTANTS.CorrelationId]), string.Empty, string.Empty, string.Empty, string.Empty);
                                        IsSuccess = AIServices.ClusterServicesTraining(ClusterIngestResult[j][CONSTANTS.CorrelationId].ToString());
                                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIServicesRetrain), nameof(AIServicesAutoTrain), "CLUSTER MODEL END", string.Empty, string.Empty, string.Empty, string.Empty);
                                    }
                                }

                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LOGGING.LogManager.Logger.LogErrorMessage(typeof(Worker), nameof(AIServicesAutoTrain), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(Worker), nameof(AIServicesAutoTrain), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
        }
        private void DeveloperPredAutoTrain()
        {
            try
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIServicesRetrain), nameof(AIServicesAutoTrain), "START", string.Empty, string.Empty, string.Empty, string.Empty);
                string aiServiceAutodays = string.Empty;
                if (!string.IsNullOrEmpty(appSettings.AIManualTrigger) && appSettings.AIManualTrigger.ToLower() == "yes")
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "AI Service Manual Trigger started", "AI Service Manual Trigger started -" + DateTime.Now.ToString(), string.Empty, string.Empty, string.Empty, string.Empty);
                    aiServiceAutodays = "-1";
                }
                else
                {
                    aiServiceAutodays = appSettings.AIAutoTrainDays;
                }
                string aIScrumAutoTrainDays = appSettings.AIScrumAutoTrainDays;
                DateTime scrumbanMonths = DateTime.Now.AddHours(Convert.ToDouble(aIScrumAutoTrainDays));
                string scrumbanDateFilter = scrumbanMonths.ToString("yyyy-MM-dd HH:mm:ss");
                DateTime months = DateTime.Now.AddHours(Convert.ToDouble(aiServiceAutodays));


                string dateFilter = months.ToString("yyyy-MM-dd HH:mm:ss");
                var filterBuilder = Builders<BsonDocument>.Filter;
                try
                {
                    var collection = _database.GetCollection<BsonDocument>(CONSTANTS.AI_CoreModels);
                    var AIfilter = filterBuilder.Eq("ServiceId", "28179a56-38b2-4d69-927d-0a6ba75da377") & filterBuilder.Eq(CONSTANTS.ModelStatus, CONSTANTS.Completed) & filterBuilder.Lte(CONSTANTS.ModifiedOn, dateFilter) & filterBuilder.Eq(CONSTANTS.DataSource, "Phoenix") & filterBuilder.Eq("ServiceId", "28179a56-38b2-4d69-927d-0a6ba75da377");
                    var AIServiceResult = collection.Find(AIfilter).ToList();
                    //AI Services AutoTrain
                    if (AIServiceResult.Count > 0)
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIServicesRetrain), nameof(AIServicesAutoTrain), "START", string.Empty, string.Empty, string.Empty, string.Empty);
                        for (int i = 0; i < AIServiceResult.Count; i++)
                        {
                            bool IsSuccess = false;
                            DataModels.AICore.IngestData data = new DataModels.AICore.IngestData();
                            data = AIServices.IngestData(AIServiceResult[i][CONSTANTS.CorrelationId].ToString());
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIServicesRetrain), nameof(AIServicesAutoTrain), "INGESTDATACOMPLETED--" + data.IsIngestionCompleted, string.Empty, string.Empty, string.Empty, string.Empty);
                            if (data.IsIngestionCompleted & data.ServiceCode != CONSTANTS.DEVELOPERPREDICTION)
                            {
                                IsSuccess = false;
                                IsSuccess = AIServices.AIServicesTraining(Convert.ToString(AIServiceResult[i][CONSTANTS.CorrelationId]));
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIServicesRetrain), nameof(AIServicesAutoTrain), "END", string.Empty, string.Empty, string.Empty, string.Empty);
                            }
                        }
                    }

                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIServicesRetrain), nameof(AIServicesAutoTrain), "--Similarity Devloper Prediction Retrain end--", string.Empty, string.Empty, string.Empty, string.Empty);
                }
                catch (Exception ex)
                {
                    LOGGING.LogManager.Logger.LogErrorMessage(typeof(Worker), nameof(SPAAutoTrain), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                }

            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(Worker), nameof(AIServicesAutoTrain), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
        }
        private void SPAAutoTrain()
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "SPAAutoTrain", "SPAAutoTrain service started", string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
                List<BsonDocument> result = new List<BsonDocument>();
                var filterBuilder = Builders<BsonDocument>.Filter;
                FilterDefinition<BsonDocument> filter = null;

                //App Collection hitting to get Auto Train Days.
                var appCollection = _database.GetCollection<AppIntegration>(CONSTANTS.AppIntegration);
                var appFilter = Builders<AppIntegration>.Filter.Empty;
                var appResults = appCollection.Find(appFilter).ToList();

                int counter = 0;
                if (appResults.Count > 0)
                {
                    for (int i = 0; i < appResults.Count; i++)
                    {
                        try
                        {
                            string appName = string.Empty;
                            if (appResults[i].ApplicationName != null)
                            {
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "SPAAutoTrain", "SPAAutoTrain service started", appResults[i].ApplicationID, string.Empty, appResults[i].clientUId, appResults[i].deliveryConstructUID);
                                appName = appResults[i].ApplicationName.Trim();
                                if (counter == 0)
                                {
                                    AppIntegration data = appResults.Find(x => x.ApplicationName == appSettings.ManualSPAAutoTrain);
                                    if (data != null)
                                    {
                                        appName = data.ApplicationName;
                                        if (appName != null)
                                        {
                                            if (appSettings.SPAAutoTestCorIds != null)
                                            {
                                                if (appSettings.SPAAutoTestCorIds.Length > 0)
                                                {
                                                    try
                                                    {
                                                        //DateTime months = new DateTime();
                                                        //if (data.AutoTrainDays < 1)
                                                        //    months = DateTime.Now.AddDays(-7);
                                                        //else
                                                        //    months = DateTime.Now.AddDays(-Convert.ToDouble(data.AutoTrainDays));
                                                        //string dateFilter = months.ToString(CONSTANTS.DateFormat);
                                                        foreach (var item in appSettings.SPAAutoTestCorIds)
                                                        {
                                                            try
                                                            {
                                                                filter = filterBuilder.Eq(CONSTANTS.CorrelationId, item.Trim()) & filterBuilder.Eq(CONSTANTS.IsCarryOutRetraining, true);
                                                                result = collection.Find(filter).ToList();
                                                                counter++;
                                                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "SPAAutoTrain", "SPAAUTOTRAIN SERVICE APPCONFIGURE CORRELATIONID", string.IsNullOrEmpty(item) ? default(Guid) : new Guid(item), data.ApplicationID, string.Empty, data.clientUId, data.deliveryConstructUID);
                                                                ModelAutoTrainForAPP(result, false);
                                                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), nameof(SPAAutoTrain), "SPAAutoTrain END", data.ApplicationID, string.Empty, data.clientUId, data.deliveryConstructUID);
                                                            }
                                                            catch (Exception ex)
                                                            {
                                                                LOGGING.LogManager.Logger.LogErrorMessage(typeof(Worker), nameof(SPAAutoTrain), ex.Message + ex.StackTrace, ex, data.ApplicationID, string.Empty, data.clientUId, data.deliveryConstructUID);
                                                            }
                                                        }
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        counter++;
                                                        LOGGING.LogManager.Logger.LogErrorMessage(typeof(Worker), nameof(SPAAutoTrain), ex.Message + ex.StackTrace, ex, data.ApplicationID, string.Empty, data.clientUId, data.deliveryConstructUID);
                                                    }
                                                }
                                                counter++;
                                            }
                                        }
                                    }
                                    counter++;
                                    if (counter > 0)
                                    {
                                        ModelRetrainOnAppNames(appResults[i], appName);
                                    }
                                }
                                else
                                {
                                    ModelRetrainOnAppNames(appResults[i], appName);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            LOGGING.LogManager.Logger.LogErrorMessage(typeof(Worker), nameof(SPAAutoTrain), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(Worker), nameof(SPAAutoTrain), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }

        }
        private void ModelRetrainOnAppNames(AppIntegration appResults, string appName)
        {
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
            List<BsonDocument> result = new List<BsonDocument>();
            var filterBuilder = Builders<BsonDocument>.Filter;
            FilterDefinition<BsonDocument> filter = null;
            //int daysToRetrain = 7;
            //if (appResults.AutoTrainDays > 0)
            //{
            //    daysToRetrain = appResults.AutoTrainDays;
            //}
            autoTrainedFeatures.FeatureName = "AutoTrain " + appName.Trim();
            autoTrainedFeatures.FunctionName = "AutoTrain " + appName.Trim();
            autoTrainedFeatures.Sequence = 2;
            autoTrainedFeatures.ModelsCount = 0;
            autoTrainedFeatures.ModelList = new string[] { };
            autoTrainedFeatures.StartedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
            InsertAutoTrainLog(autoTrainedFeatures);

            switch (appName.Trim())
            {
                case "myWizard.ImpactAnalyzer":
                    try
                    {
                        autoTrainedFeatures.FeatureName = "AutoTrain ImpactAnalyzer";
                        autoTrainedFeatures.Sequence = 2;
                        autoTrainedFeatures.ModelsCount = 0;
                        autoTrainedFeatures.FunctionName = "AutoTrain ImpactAnalyzer";
                        autoTrainedFeatures.StartedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                        autoTrainedFeatures.ModelList = new string[] { };
                        InsertAutoTrainLog(autoTrainedFeatures);
                        //int daysToRetrain = 7;
                        //if (Convert.ToInt32(appSettings.IASSAIRetrainFrequency) > 0)
                        //{
                        //    daysToRetrain = Convert.ToInt32(appSettings.IASSAIRetrainFrequency);
                        //}
                        //DateTime months = DateTime.Now.AddDays(-Convert.ToDouble(daysToRetrain));
                        //string dateFilter = months.ToString(CONSTANTS.DateFormat);
                        //filter = filterBuilder.Eq(CONSTANTS.Status, CONSTANTS.Deployed) & filterBuilder.Lt(CONSTANTS.ModifiedOn, dateFilter) & filterBuilder.Eq("AppId", appResults.ApplicationID.Trim()) & filterBuilder.Eq("TemplateUsecaseId", "f0320924-2ee3-4398-ad7c-8bc172abd78d");
                        List<string> iAUsecaseIds = appSettings.IA_SSAIUseCaseIds;
                        filter = filterBuilder.Eq(CONSTANTS.Status, CONSTANTS.Deployed) & filterBuilder.Eq(CONSTANTS.IsCarryOutRetraining, true) & filterBuilder.Eq("AppId", appResults.ApplicationID.Trim()) & filterBuilder.In("TemplateUsecaseId", iAUsecaseIds) & filterBuilder.Eq(CONSTANTS.CreatedByUser, "SYSTEM");
                        result = collection.Find(filter).ToList();

                        autoTrainedFeatures.ModelsCount = result.Count();
                        var CorrelationIds = result.Where(x => x["AppId"].ToString() == "89d56036-d70e-48ee-83df-303e493b0c36").Select(y => y["CorrelationId"]);
                        autoTrainedFeatures.ModelList = BsonSerializer.Deserialize<string[]>(JsonConvert.SerializeObject(CorrelationIds));
                        autoTrainedFeatures.Sequence = 3;
                        autoTrainedFeatures.StartedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                        InsertAutoTrainLog(autoTrainedFeatures);
                        ModelAutoTrainForAPP(result, true);
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), nameof(SPAAutoTrain), "SPAAutoTrain END", appResults.ApplicationID, string.Empty, appResults.clientUId, appResults.deliveryConstructUID);
                    }
                    catch (Exception ex)
                    {
                        LOGGING.LogManager.Logger.LogErrorMessage(typeof(Worker), nameof(SPAAutoTrain), ex.Message + ex.StackTrace, ex, appResults.ApplicationID, string.Empty, appResults.clientUId, appResults.deliveryConstructUID);
                    }
                    break;
                case "Release Planner":
                    try
                    {
                        autoTrainedFeatures.FeatureName = "SPAAutoTrain Release Planner";
                        autoTrainedFeatures.Sequence = 2;
                        autoTrainedFeatures.ModelsCount = 0;
                        autoTrainedFeatures.FunctionName = "SPAAutoTrain Release Planner";
                        autoTrainedFeatures.StartedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                        autoTrainedFeatures.ModelList = new string[] { };
                        InsertAutoTrainLog(autoTrainedFeatures);
                        //DateTime months = DateTime.Now.AddDays(-Convert.ToDouble(daysToRetrain));
                        //string dateFilter = months.ToString(CONSTANTS.DateFormat);
                        //filter = filterBuilder.Eq(CONSTANTS.Status, CONSTANTS.Deployed) & filterBuilder.Lt(CONSTANTS.ModifiedOn, dateFilter) & filterBuilder.Eq("AppId", appResults.ApplicationID.Trim()) & filterBuilder.Eq("TemplateUsecaseId", "f0320924-2ee3-4398-ad7c-8bc172abd78d");
                        filter = filterBuilder.Eq(CONSTANTS.Status, CONSTANTS.Deployed) & filterBuilder.Eq(CONSTANTS.IsCarryOutRetraining, true) & filterBuilder.Eq("AppId", appResults.ApplicationID.Trim()) & filterBuilder.Eq("TemplateUsecaseId", "f0320924-2ee3-4398-ad7c-8bc172abd78d");
                        result = collection.Find(filter).ToList();


                        autoTrainedFeatures.ModelsCount = result.Count();
                        var CorrelationIds = result.Where(x => x["AppId"].ToString() == "9fe508f7-64bc-4f58-899b-78f349707efa").Select(y => y["CorrelationId"]);
                        autoTrainedFeatures.ModelList = BsonSerializer.Deserialize<string[]>(JsonConvert.SerializeObject(CorrelationIds));
                        autoTrainedFeatures.Sequence = 3;
                        autoTrainedFeatures.StartedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                        InsertAutoTrainLog(autoTrainedFeatures);
                        ModelAutoTrainForAPP(result, false);
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), nameof(SPAAutoTrain), "SPAAutoTrain END", appResults.ApplicationID, string.Empty, appResults.clientUId, appResults.deliveryConstructUID);
                    }
                    catch (Exception ex)
                    {
                        LOGGING.LogManager.Logger.LogErrorMessage(typeof(Worker), nameof(SPAAutoTrain), ex.Message + ex.StackTrace, ex, appResults.ApplicationID, string.Empty, appResults.clientUId, appResults.deliveryConstructUID);
                    }
                    break;
                case "CMA":
                    try
                    {
                        autoTrainedFeatures.FeatureName = "SPAAutoTrain CMA";
                        autoTrainedFeatures.Sequence = 2;
                        autoTrainedFeatures.ModelsCount = 0;
                        autoTrainedFeatures.FunctionName = "SPAAutoTrain CMA";
                        autoTrainedFeatures.StartedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                        autoTrainedFeatures.ModelList = new string[] { };
                        InsertAutoTrainLog(autoTrainedFeatures);
                        //DateTime months3 = DateTime.Now.AddDays(-Convert.ToDouble(daysToRetrain));
                        //string dateFilter3 = months3.ToString("yyyy-MM-dd HH:mm:ss");
                        //filter = filterBuilder.Eq(CONSTANTS.IsModelTemplate, false) & filterBuilder.Eq(CONSTANTS.Status, CONSTANTS.Deployed) & filterBuilder.Lt("ModifiedOn", dateFilter3) & (filterBuilder.Eq(CONSTANTS.DataSource, "Pheonix")) & (filterBuilder.Eq("SourceName", "pad")) & (filterBuilder.Eq("LinkedApps.0", "CMA"));
                        filter = filterBuilder.Eq(CONSTANTS.IsModelTemplate, false) & filterBuilder.Eq(CONSTANTS.Status, CONSTANTS.Deployed) & filterBuilder.Eq(CONSTANTS.IsCarryOutRetraining, true) & filterBuilder.Eq(CONSTANTS.DataSource, "Pheonix") & (filterBuilder.Eq("SourceName", "pad")) & (filterBuilder.Eq("LinkedApps.0", "CMA"));
                        result = collection.Find(filter).ToList();
                        autoTrainedFeatures.ModelsCount = result.Count();
                        var CorrelationIds = result.Where(x => x["AppId"].ToString() == "5354e9b8-0eb1-4275-b666-6eba15c12b2c").Select(y => y["CorrelationId"]);
                        autoTrainedFeatures.ModelList = BsonSerializer.Deserialize<string[]>(JsonConvert.SerializeObject(CorrelationIds));
                        autoTrainedFeatures.Sequence = 3;
                        autoTrainedFeatures.StartedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                        InsertAutoTrainLog(autoTrainedFeatures);
                        CMAAutoTrainForAPP(result);
                    }
                    catch (Exception ex)
                    {
                        LOGGING.LogManager.Logger.LogErrorMessage(typeof(Worker), "CMA SPAAUTOTRAIN", ex.Message + ex.StackTrace, ex, appResults.ApplicationID, string.Empty, appResults.clientUId, appResults.deliveryConstructUID);
                    }
                    break;
                case "SPA":
                case "SPA(Velocity)":
                    try
                    {
                        autoTrainedFeatures.FeatureName = "SPAAutoTrain SPA";
                        autoTrainedFeatures.Sequence = 2;
                        autoTrainedFeatures.ModelsCount = 0;
                        autoTrainedFeatures.FunctionName = "SPAAutoTrain SPA";
                        autoTrainedFeatures.StartedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                        autoTrainedFeatures.ModelList = new string[] { };
                        InsertAutoTrainLog(autoTrainedFeatures);
                        //DateTime months = DateTime.Now.AddDays(-Convert.ToDouble(daysToRetrain));
                        //string dateFilter = months.ToString(CONSTANTS.DateFormat);
                        //filter = filterBuilder.Eq(CONSTANTS.Status, CONSTANTS.Deployed) & filterBuilder.Lt(CONSTANTS.ModifiedOn, dateFilter) & filterBuilder.Eq(CONSTANTS.DataSource, CONSTANTS.SPAAPP);
                        filter = filterBuilder.Eq(CONSTANTS.Status, CONSTANTS.Deployed) & filterBuilder.Eq(CONSTANTS.IsCarryOutRetraining, true) & filterBuilder.Eq(CONSTANTS.DataSource, CONSTANTS.SPAAPP);
                        result = collection.Find(filter).ToList();
                        autoTrainedFeatures.ModelsCount = result.Count();
                        var CorrelationIds = result.Where(x => x["AppId"].ToString() == "a3798931-4028-4f72-8bcd-8bb368cc71a9").Select(y => y["CorrelationId"]);
                        var CorrelationIds2 = result.Where(x => x["AppId"].ToString() == "595fa642-5d24-4082-bb4d-99b8df742013").Select(y => y["CorrelationId"]);
                        var totalCorids = CorrelationIds.Concat(CorrelationIds2);
                        autoTrainedFeatures.ModelList = BsonSerializer.Deserialize<string[]>(JsonConvert.SerializeObject(totalCorids));
                        autoTrainedFeatures.Sequence = 3;
                        autoTrainedFeatures.StartedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                        InsertAutoTrainLog(autoTrainedFeatures);
                        ModelAutoTrainForAPP(result, false);
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), nameof(SPAAutoTrain), "SPAAutoTrain END", appResults.ApplicationID, string.Empty, appResults.clientUId, appResults.deliveryConstructUID);
                    }
                    catch (Exception ex)
                    {
                        LOGGING.LogManager.Logger.LogErrorMessage(typeof(Worker), nameof(SPAAutoTrain), ex.Message + ex.StackTrace, ex, appResults.ApplicationID, string.Empty, appResults.clientUId, appResults.deliveryConstructUID);
                    }
                    break;
                case "VDS(SI)":
                    try
                    {
                        autoTrainedFeatures.FeatureName = "SPAAutoTrain VDS(SI)";
                        autoTrainedFeatures.Sequence = 2;
                        autoTrainedFeatures.ModelsCount = 0;
                        autoTrainedFeatures.FunctionName = "SPAAutoTrain VDS(SI)";
                        autoTrainedFeatures.StartedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                        autoTrainedFeatures.ModelList = new string[] { };
                        InsertAutoTrainLog(autoTrainedFeatures);
                        //DateTime months2 = DateTime.Now.AddDays(-Convert.ToDouble(daysToRetrain));
                        //string dateFilter2 = months2.ToString(CONSTANTS.DateFormat);
                        //filter = filterBuilder.Eq(CONSTANTS.Status, CONSTANTS.Deployed) & filterBuilder.Lt(CONSTANTS.ModifiedOn, dateFilter2) & (filterBuilder.Eq(CONSTANTS.DataSource, CONSTANTS.VDS_SI) | filterBuilder.Eq(CONSTANTS.DataSource, CONSTANTS.SPAAPP));
                        filter = filterBuilder.Eq(CONSTANTS.Status, CONSTANTS.Deployed) & filterBuilder.Eq(CONSTANTS.IsCarryOutRetraining, true) & (filterBuilder.Eq(CONSTANTS.DataSource, CONSTANTS.VDS_SI) | filterBuilder.Eq(CONSTANTS.DataSource, CONSTANTS.SPAAPP));
                        result = collection.Find(filter).ToList();
                        autoTrainedFeatures.ModelsCount = result.Count();
                        var CorrelationIds = result.Where(x => x["AppId"].ToString() == "65063df1-7a20-4fb2-9da5-b5800f2ca48c").Select(y => y["CorrelationId"]);
                        autoTrainedFeatures.ModelList = BsonSerializer.Deserialize<string[]>(JsonConvert.SerializeObject(CorrelationIds));
                        autoTrainedFeatures.Sequence = 3;
                        autoTrainedFeatures.StartedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                        InsertAutoTrainLog(autoTrainedFeatures);
                        ModelAutoTrainForAPP(result, false);
                    }
                    catch (Exception ex)
                    {
                        LOGGING.LogManager.Logger.LogErrorMessage(typeof(Worker), nameof(SPAAutoTrain), ex.Message + ex.StackTrace, ex, appResults.ApplicationID, string.Empty, appResults.clientUId, appResults.deliveryConstructUID);
                    }
                    break;
            }
        }
        private void AmbulanceLaneAutoReTrainInvoke(IngrainRequestQueue result)
        {
            try
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), nameof(AmbulanceLaneAutoReTrainInvoke), "AmbulanceLane SPAAutoTrain Start", string.IsNullOrEmpty(result.CorrelationId) ? default(Guid) : new Guid(result.CorrelationId), result.AppID, string.Empty, result.ClientID, result.DeliveryconstructId);
                if (result != null && appSettings.Environment != CONSTANTS.PAMEnvironment)
                {
                    var filterBuilder = Builders<IngrainRequestQueue>.Filter;
                    var filterCorrelation = filterBuilder.Eq("CorrelationId", result.CorrelationId) & filterBuilder.Eq("RequestId", result.RequestId) & filterBuilder.Eq("RequestStatus", "New") & filterBuilder.Eq("Function", "AutoRetrain");
                    var update = Builders<IngrainRequestQueue>.Update.Set("RequestStatus", "Occupied").Set("Progress", "10%").Set("Status", "P");
                    var isUpdated = _collection.UpdateMany(filterCorrelation, update);
                }
                Thread trainingThread = new Thread(() => AmbulanceLaneAutoReTrainModel(result));
                trainingThread.Start();

            }
            catch (Exception ex)
            {

                LOGGING.LogManager.Logger.LogErrorMessage(typeof(Worker), nameof(AmbulanceLaneAutoReTrainInvoke), ex.Message + ex.StackTrace, ex, result.AppID, string.Empty, result.ClientID, result.DeliveryconstructId);
            }
        }
        private void AmbulanceLaneAutoReTrainModel(IngrainRequestQueue result)
        {
            try
            {
                bool ismessageSent = false;
                var MappingCollection = _database.GetCollection<PublicTemplateMapping>(CONSTANTS.PublicTemplateMapping);
                var Builder = Builders<PublicTemplateMapping>.Filter;
                var Projection1 = Builders<PublicTemplateMapping>.Projection.Exclude("_id");
                var filter = Builder.Eq(CONSTANTS.ApplicationID, result.AppID) & Builder.Eq(CONSTANTS.UsecaseID, result.TemplateUseCaseID);
                var templatedata = MappingCollection.Find(filter).Project<PublicTemplateMapping>(Projection1).FirstOrDefault();
                if (templatedata != null)
                {
                    result.ApplicationName = templatedata.ApplicationName;
                }
                InstaRetrain instaRtn = _instaAutoRetrainService.SPAAmbulanceIngestData(result.ToBsonDocument());
                if (instaRtn.Status == "E")
                {
                    ismessageSent = true;
                    this.AmbulanceLaneRetrainFailure(result, instaRtn);
                }
                else if (instaRtn.Status == "C")
                {
                    _instaAutoRetrainService.UpdateRetrainRequestStatus("25%", result.RequestId);
                    instaRtn = _instaAutoRetrainService.GetInstaAutoDataEngineering(result.ToBsonDocument());
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), nameof(Worker), "--SPA Ambulance DATEENGINEERING END STATUS--" + instaRetrain.Status + "--MESSAGE--" + instaRetrain.Message, string.IsNullOrEmpty(result.CorrelationId) ? default(Guid) : new Guid(result.CorrelationId), result.AppID, string.Empty, result.ClientID, result.DeliveryconstructId);
                }
                if (instaRtn.Status == "E")
                {
                    if (!ismessageSent)
                    {
                        ismessageSent = true;
                        this.AmbulanceLaneRetrainFailure(result, instaRtn);
                    }
                }
                else if (instaRtn.Status == "C")
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "SPA Ambulance AUTO TRAIN MODELENGINEERING", "STARTED", string.IsNullOrEmpty(result.CorrelationId) ? default(Guid) : new Guid(result.CorrelationId), result.AppID, string.Empty, result.ClientID, result.DeliveryconstructId);
                    _instaAutoRetrainService.UpdateRetrainRequestStatus("55%", result.RequestId);
                    instaRtn = _instaAutoRetrainService.GetInstaAutoModelEngineering(result.ToBsonDocument());
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), nameof(Worker), "--SPA Ambulance MODEL ENGINEERING END STATUS--" + instaRetrain.Status, string.IsNullOrEmpty(result.CorrelationId) ? default(Guid) : new Guid(result.CorrelationId), result.AppID, string.Empty, result.ClientID, result.DeliveryconstructId);
                }
                if (instaRtn.Status == "E")
                {
                    if (!ismessageSent)
                    {
                        ismessageSent = true;
                        this.AmbulanceLaneRetrainFailure(result, instaRtn);
                    }
                }
                else if (instaRtn.Status == "C")
                {
                    _instaAutoRetrainService.UpdateRetrainRequestStatus("75%", result.RequestId);
                    var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
                    var filterBuilder = Builders<BsonDocument>.Filter;
                    var filterDeploy = filterBuilder.Eq(CONSTANTS.CorrelationId, result.CorrelationId);
                    var deployData = collection.Find(filterDeploy).ToList();
                    var modelType = string.Empty;
                    if (deployData.Count > 0)
                    {
                        if (deployData[0].Contains("ModelType"))
                        {
                            modelType = deployData[0]["ModelType"].ToString();
                        }
                    }
                    instaRtn = _instaAutoRetrainService.GetSPADeployPrediction(result.ToBsonDocument(), modelType);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), nameof(Worker), "--SPA Ambulance DEPLOY MODEL END--" + instaRetrain.Status, string.IsNullOrEmpty(result.CorrelationId) ? default(Guid) : new Guid(result.CorrelationId), result.AppID, string.Empty, result.ClientID, result.DeliveryconstructId);
                }

                if (instaRtn.Status == "E")
                {
                    if (!ismessageSent)
                    {
                        ismessageSent = true;
                        this.AmbulanceLaneRetrainFailure(result, instaRtn);
                    }
                }
                else if (instaRtn.Status == "C")
                {
                    _instaAutoRetrainService.UpdateRetrainRequestStatus("100%", result.RequestId);
                    _IngrainResponseData.CorrelationId = result.CorrelationId;
                    _IngrainResponseData.Message = CONSTANTS.ReTrainingCompleted;
                    _IngrainResponseData.Status = "Completed";
                    _IngrainResponseData.ErrorMessage = CONSTANTS.ReTrainingCompleted;
                    _instaAutoRetrainService.UpdateReTrainingStatus(instaRtn.Status, instaRtn.Message, CONSTANTS.Completed, result.RequestId);
                    _instaAutoRetrainService.CallbackResponse(_IngrainResponseData, result.ApplicationName, result.AppURL, result.ClientID, result.DeliveryconstructId, result.AppID, result.TemplateUseCaseID, result.RequestId, null, result.CreatedByUser);
                }
            }
            catch (Exception ex)
            {
                _IngrainResponseData.CorrelationId = result.CorrelationId;
                _IngrainResponseData.Message = CONSTANTS.RetrainingUnsucess;
                _IngrainResponseData.Status = CONSTANTS.ErrorMessage;
                _IngrainResponseData.ErrorMessage = ex.Message;
                _instaAutoRetrainService.CallbackResponse(_IngrainResponseData, result.ApplicationName, result.AppURL, result.ClientID, result.DeliveryconstructId, result.AppID, result.TemplateUseCaseID, result.RequestId, ex.Message + ex.StackTrace, result.CreatedByUser);
                _instaAutoRetrainService.UpdateReTrainingStatus(CONSTANTS.E, ex.Message, CONSTANTS.ErrorMessage, result.RequestId);
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(Worker), nameof(AmbulanceLaneAutoReTrainModel), ex.Message + ex.StackTrace, ex, result.AppID, string.Empty, result.ClientID, result.DeliveryconstructId);
            }
        }
        private void AmbulanceLaneRetrainFailure(IngrainRequestQueue result, InstaRetrain instaRtn)
        {
            _IngrainResponseData.CorrelationId = result.CorrelationId;
            _IngrainResponseData.Message = CONSTANTS.RetrainingUnsucess;
            _IngrainResponseData.Status = CONSTANTS.ErrorMessage;
            _IngrainResponseData.ErrorMessage = instaRtn.ErrorMessage;
            var queueCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
            var filterBuilder1 = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, result.CorrelationId) & Builders<BsonDocument>.Filter.Eq("RequestId", result.RequestId);
            queueCollection.DeleteMany(filterBuilder1);
            // _instaAutoRetrainService.UpdateReTrainingStatus(instaRtn.Status, instaRtn.ErrorMessage, _IngrainResponseData.Status, result.RequestId);
            _instaAutoRetrainService.CallbackResponse(_IngrainResponseData, result.ApplicationName, result.AppURL, result.ClientID, result.DeliveryconstructId, result.AppID, result.TemplateUseCaseID, result.RequestId, null, result.CreatedByUser);
        }
        private void ModelAutoTrainForAPP(List<BsonDocument> result, bool isIA)
        {
            if (result.Count > 0)
            {
                for (int i = 0; i < result.Count; i++)
                {
                    try
                    {
                        //string applicationId = result[i]["AppId"].ToString();
                        string modifiedOn = result[i]["ModifiedOn"].ToString();
                        DateTime? triggerDate = null;
                        //if (applicationId != appSettings.IA_ApplicationId)
                        //{
                        DateTime modifiedDate = DateTime.Parse(modifiedOn);
                        triggerDate = modifiedDate.AddDays(Convert.ToInt32(result[i][CONSTANTS.RetrainingFrequencyInDays]));
                        //}
                        if (DateTime.Now >= triggerDate)
                        {
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), nameof(ModelAutoTrainForAPP), "--SPA ModelAutoTrainForAPP START", string.IsNullOrEmpty(Convert.ToString(result[i][CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[i][CONSTANTS.CorrelationId])), string.Empty, string.Empty, string.Empty, string.Empty);
                            var logCollection = _database.GetCollection<InstaLog>("Insta_AutoLog");
                            var instaLog = new InstaLog(result[i]["CorrelationId"].ToString(), result[i]["ModelName"].ToString(), "IngestData", "Start", "", result[i]["CreatedByUser"].ToString());
                            logCollection.InsertOne(instaLog);

                            instaRetrain = new InstaRetrain();
                            if (isIA)
                            {
                                instaRetrain = _instaAutoRetrainService.IAIngestData(result[i]);
                            }
                            else
                            {
                                instaRetrain = _instaAutoRetrainService.SPAIngestData(result[i]);
                            }
                            if (instaRetrain.Status == "C")
                            {
                                //instaRetrain.Status = CONSTANTS.E;
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), nameof(ModelAutoTrainForAPP), "SPA AUTO TRAIN DATEENGINEERING - START", string.IsNullOrEmpty(Convert.ToString(result[i][CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[i][CONSTANTS.CorrelationId])), string.Empty, string.Empty, string.Empty, string.Empty);
                                instaRetrain = _instaAutoRetrainService.GetInstaAutoDataEngineering(result[i]);
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), nameof(ModelAutoTrainForAPP), "SPA AUTO TRAIN DATEENGINEERING - END WITH STATUS" + instaRetrain.Status + " & MESSAGE" + instaRetrain.Message, string.IsNullOrEmpty(Convert.ToString(result[i][CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[i][CONSTANTS.CorrelationId])), string.Empty, string.Empty, string.Empty, string.Empty);
                                if (instaRetrain.Status == "C")
                                {
                                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), nameof(ModelAutoTrainForAPP), "SPA AUTO TRAIN MODELENGINEERING - START", string.IsNullOrEmpty(Convert.ToString(result[i][CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[i][CONSTANTS.CorrelationId])), string.Empty, string.Empty, string.Empty, string.Empty);
                                    //instaRetrain.Status = CONSTANTS.E;
                                    instaRetrain = _instaAutoRetrainService.GetInstaAutoModelEngineering(result[i]);
                                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), nameof(ModelAutoTrainForAPP), "SPA AUTO TRAIN MODELENGINEERING - END WITH STATUS" + instaRetrain.Status + " & MESSAGE" + instaRetrain.Message, string.IsNullOrEmpty(Convert.ToString(result[i][CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[i][CONSTANTS.CorrelationId])), string.Empty, string.Empty, string.Empty, string.Empty);
                                    if (instaRetrain.Status == "C")
                                    {
                                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), nameof(ModelAutoTrainForAPP), "SPA AUTO TRAIN DEPLOY MODEL - START", string.IsNullOrEmpty(Convert.ToString(result[i][CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[i][CONSTANTS.CorrelationId])), string.Empty, string.Empty, string.Empty, string.Empty);
                                        _instaAutoRetrainService.GetSPADeployPrediction(result[i], string.Empty);
                                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), nameof(ModelAutoTrainForAPP), "SPA AUTO TRAIN DEPLOY MODEL - END WITH STATUS" + instaRetrain.Status + " & MESSAGE" + instaRetrain.Message, string.IsNullOrEmpty(Convert.ToString(result[i][CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[i][CONSTANTS.CorrelationId])), string.Empty, string.Empty, string.Empty, string.Empty);
                                        if (instaRetrain.Status == CONSTANTS.E)
                                        {
                                            instaRetrain.Status = CONSTANTS.E;
                                            instaLog = new InstaLog(result[i]["CorrelationId"].ToString(), result[i]["ModelName"].ToString(), "SPA AUTO TRAIN DEPLOY MODEL", "Error", instaRetrain.ErrorMessage + "-ERROR at Depoy Model", result[i]["CreatedByUser"].ToString());
                                            logCollection.InsertOne(instaLog);
                                        }
                                    }
                                    else
                                    {
                                        instaRetrain.Status = CONSTANTS.E;
                                        instaLog = new InstaLog(result[i]["CorrelationId"].ToString(), result[i]["ModelName"].ToString(), "SPA AUTO TRAIN MODEL ENGINEERING", "Error", instaRetrain.ErrorMessage + "-ERROR at Model Engineering", result[i]["CreatedByUser"].ToString());
                                        logCollection.InsertOne(instaLog);
                                    }

                                }
                                else
                                {
                                    instaRetrain.Status = CONSTANTS.E;
                                    instaLog = new InstaLog(result[i]["CorrelationId"].ToString(), result[i]["ModelName"].ToString(), "SPA AUTO TRAIN DATEENGINEERING", "Error", instaRetrain.ErrorMessage + "-ERROR at Data Engineering", result[i]["CreatedByUser"].ToString());
                                    logCollection.InsertOne(instaLog);
                                }
                            }
                            else
                            {
                                instaRetrain.Status = CONSTANTS.E;
                                instaLog = new InstaLog(result[i]["CorrelationId"].ToString(), result[i]["ModelName"].ToString(), "SPA AUTO TRAIN INGESTDATA", "Error", instaRetrain.ErrorMessage + "-ERROR at IngestData", result[i]["CreatedByUser"].ToString());
                                logCollection.InsertOne(instaLog);
                            }




                            //if (instaRetrain.Status == "C")
                            //{
                            //    LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "SPA AUTO TRAIN MODELENGINEERING", "STARTED", string.IsNullOrEmpty(Convert.ToString(result[i][CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[i][CONSTANTS.CorrelationId])), string.Empty, string.Empty, string.Empty, string.Empty);
                            //    instaRetrain.Status = CONSTANTS.E;
                            //    instaRetrain = _instaAutoRetrainService.GetInstaAutoModelEngineering(result[i]);
                            //    LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), nameof(Worker), "--SPA MODEL ENGINEERING END STATUS--" + instaRetrain.Status, string.IsNullOrEmpty(Convert.ToString(result[i][CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[i][CONSTANTS.CorrelationId])), string.Empty, string.Empty, string.Empty, string.Empty);
                            //}
                            //else
                            //{
                            //    instaRetrain.Status = CONSTANTS.E;
                            //    instaLog = new InstaLog(result[i]["CorrelationId"].ToString(), result[i]["ModelName"].ToString(), "IngestData", "Start", instaRetrain.ErrorMessage + "-ERROR at Model Engineering", result[i]["CreatedByUser"].ToString());
                            //    logCollection.InsertOne(instaLog);
                            //}
                            //if (instaRetrain.Status == "C")
                            //{
                            //    _instaAutoRetrainService.GetSPADeployPrediction(result[i], string.Empty);
                            //    LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), nameof(Worker), "--SPA DEPLOY MODEL END--" + instaRetrain.Status, string.IsNullOrEmpty(Convert.ToString(result[i][CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[i][CONSTANTS.CorrelationId])), string.Empty, string.Empty, string.Empty, string.Empty);
                            //}
                            //else
                            //{
                            //    instaRetrain.Status = CONSTANTS.E;
                            //    instaLog = new InstaLog(result[i]["CorrelationId"].ToString(), result[i]["ModelName"].ToString(), "IngestData", "Start", instaRetrain.ErrorMessage + "-ERROR at Deploy", result[i]["CreatedByUser"].ToString());
                            //    logCollection.InsertOne(instaLog);
                            //}
                        }
                    }
                    catch (Exception ex)
                    {
                        LOGGING.LogManager.Logger.LogErrorMessage(typeof(Worker), nameof(ModelAutoTrainForAPP), ex.Message + ex.StackTrace, ex, Convert.ToString(result[i]["CorrelationId"]), string.Empty, Convert.ToString(result[i]["ClientUId"]), Convert.ToString(result[i]["DeliveryConstructUID"]));
                    }
                }
            }
        }
        private void CMAAutoTrainForAPP(List<BsonDocument> result)
        {
            string correlationId = string.Empty;
            try
            {
                if (result.Count > 0)
                {
                    for (int i = 0; i < result.Count; i++)
                    {
                        try
                        {
                            string modifiedOn = result[i]["ModifiedOn"].ToString();
                            DateTime modifiedDate = DateTime.Parse(modifiedOn);
                            DateTime triggerDate = modifiedDate.AddDays(Convert.ToInt32(result[i][CONSTANTS.RetrainingFrequencyInDays]));

                            if (DateTime.Now >= triggerDate)
                            {
                                instaRetrain = new InstaRetrain();
                                correlationId = result[i][CONSTANTS.CorrelationId].ToString();
                                instaRetrain = _instaAutoRetrainService.IngestData(result[i]);
                                if (instaRetrain.Status == "C")
                                {
                                    instaRetrain = _instaAutoRetrainService.GetInstaAutoDataEngineering(result[i]);
                                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), nameof(Worker), "--CMA DATEENGINEERING END STATUS--" + instaRetrain.Status + "--MESSAGE--" + instaRetrain.Message, string.IsNullOrEmpty(Convert.ToString(result[i][CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[i][CONSTANTS.CorrelationId])), string.Empty, string.Empty, string.Empty, string.Empty);
                                }
                                if (instaRetrain.Status == "C")
                                {
                                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "CMA AUTO TRAIN MODELENGINEERING", "STARTED", string.IsNullOrEmpty(Convert.ToString(result[i][CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[i][CONSTANTS.CorrelationId])), string.Empty, string.Empty, string.Empty, string.Empty);
                                    instaRetrain = _instaAutoRetrainService.GetInstaAutoModelEngineering(result[i]);
                                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), nameof(Worker), "--CMA MODEL ENGINEERING END STATUS--" + instaRetrain.Status, string.IsNullOrEmpty(Convert.ToString(result[i][CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[i][CONSTANTS.CorrelationId])), string.Empty, string.Empty, string.Empty, string.Empty);
                                }
                                if (instaRetrain.Status == "C")
                                {
                                    _instaAutoRetrainService.GetInstaAutoDeployPrediction(result[i]);
                                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), nameof(Worker), "--CMA DEPLOY MODEL END--" + instaRetrain.Status, string.IsNullOrEmpty(Convert.ToString(result[i][CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[i][CONSTANTS.CorrelationId])), string.Empty, string.Empty, string.Empty, string.Empty);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            LOGGING.LogManager.Logger.LogErrorMessage(typeof(Worker), nameof(CMAAutoTrainForAPP), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(Worker), nameof(Worker), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), nameof(Worker), "CMA AUTORETRAIN END-", default(Guid), string.Empty, string.Empty, string.Empty, string.Empty);
        }
        private void ModelMonitorAutoRetrain()
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), nameof(Worker), "MODELMONITOR AUTORETRAIN  START ", string.Empty, string.Empty, string.Empty, string.Empty);
            string correlationId = string.Empty;
            string[] CorIdarr = new string[] { };
            try
            {
                var collection = _database.GetCollection<DeployModel>(CONSTANTS.SSAIDeployedModels);
                string monitorAutodays = string.Empty;
                if (!string.IsNullOrEmpty(appSettings.ManualTrigger) && appSettings.ManualTrigger.ToLower() == "yes")
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "Model Monitor Manual Trigger started", "Model Monitor Manual Trigger started -" + DateTime.Now.ToString(), string.Empty, string.Empty, string.Empty, string.Empty);
                    monitorAutodays = "-1";
                }
                else
                {
                    monitorAutodays = appSettings.ModelMonitorAutoDays;
                }
                DateTime months = DateTime.Now.AddHours(Convert.ToDouble(monitorAutodays));
                string dateFilter = months.ToString("yyyy-MM-dd HH:mm:ss");
                var filterBuilder = Builders<BsonDocument>.Filter;
                //if (appSettings.ModelMonitorManualTrigger == "yes")
                //{
                //    List<string> correlationIds = appSettings.ModelMonitorCorIds.Split(",").ToList();
                //    if (correlationIds.Count > 0)
                //    {
                //        filter = filterBuilder.In("CorrelationId", correlationIds);
                //    }
                //}
                //else
                //{
                //    filter = filterBuilder.Eq("IsModelTemplate", false) & filterBuilder.Ne("AppId", "89d56036-d70e-48ee-83df-303e493b0c36") & filterBuilder.Ne("ModelType", "TimeSeries") & filterBuilder.Lte("ModifiedOn", dateFilter) & filterBuilder.Eq("Status", "Deployed") & ((filterBuilder.Ne("InstaId", BsonNull.Value) & filterBuilder.Ne("ModelType", CONSTANTS.Regression) & filterBuilder.Eq("UseCaseID", BsonNull.Value)) | (filterBuilder.Eq("SourceName", "pad") & filterBuilder.Ne("AppId", "5354e9b8-0eb1-4275-b666-6eba15c12b2c")) | (filterBuilder.Eq("SourceName", "metric")) | (filterBuilder.Eq("DataSource", CONSTANTS.Custom) & filterBuilder.Nin("AppId", appSettings.ModelMonitorExcludeIds)));
                //}
                //var filter = filterBuilder.Eq("CorrelationId", "16d9ec30-74cb-4541-8496-4c0082454ecc");
                var filter = filterBuilder.Eq("IsModelTemplate", false) & filterBuilder.Ne("AppId", "89d56036-d70e-48ee-83df-303e493b0c36") & filterBuilder.Ne("ModelType", "TimeSeries") & filterBuilder.Lte("ModifiedOn", dateFilter) & filterBuilder.Eq("Status", "Deployed") & ((filterBuilder.Ne("InstaId", BsonNull.Value) & filterBuilder.Ne("ModelType", CONSTANTS.Regression) & filterBuilder.Eq("UseCaseID", BsonNull.Value)) | (filterBuilder.Eq("SourceName", "pad") & filterBuilder.Ne("AppId", "5354e9b8-0eb1-4275-b666-6eba15c12b2c")) | (filterBuilder.Eq("SourceName", "metric")) | (filterBuilder.Eq("DataSource", CONSTANTS.Custom)));
                var result = _deployModelCollection.Find(filter).ToList();
                for (int i = 0; i < result.Count; i++)
                {
                    //added Logs in collection for Model  Monitoring
                    if (i == 0)
                    {
                        var CorrelationIds = result.Select(y => y[CONSTANTS.CorrelationId]);
                        autoTrainedFeatures.FeatureName = "ModelMonitoring";
                        autoTrainedFeatures.ModelList = BsonSerializer.Deserialize<string[]>(JsonConvert.SerializeObject(CorrelationIds));
                        autoTrainedFeatures.Sequence = 1;
                        autoTrainedFeatures.ModelsCount = result.Count;
                        autoTrainedFeatures.EndedOn = null;
                        autoTrainedFeatures.LogId = Guid.NewGuid().ToString();
                        autoTrainedFeatures.FunctionName = "ModelMonitorAutoRetrain";
                        autoTrainedFeatures.ErrorMessage = null;
                        autoTrainedFeatures.StartedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                        InsertAutoTrainLog(autoTrainedFeatures);
                    }
                    //added Logs in collection for Model  Monitoring
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), nameof(Worker), "--Monitor CORRELATIONID--" + Convert.ToString(result[i][CONSTANTS.CorrelationId]) + "--MODELS COUNT-- " + result.Count, string.Empty, string.Empty, string.Empty, string.Empty);
                }
                for (int i = 0; i < result.Count; i++)
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), nameof(Worker), "--Monitor FOR LOOP CORRELATIONID START--" + result[i][CONSTANTS.CorrelationId].ToString(), string.Empty, string.Empty, string.Empty, string.Empty);
                    correlationId = result[i][CONSTANTS.CorrelationId].ToString();
                    CorIdarr = new string[] { correlationId };
                    //added Logs in collection for Model  Monitoring
                    autoTrainedFeatures.FeatureName = "ModelMonitoring";
                    autoTrainedFeatures.ModelList = CorIdarr;
                    autoTrainedFeatures.Sequence = 2;
                    autoTrainedFeatures.ModelsCount = 1;
                    autoTrainedFeatures.EndedOn = null;
                    autoTrainedFeatures.FunctionName = "ModelMonitorAutoRetrain";
                    autoTrainedFeatures.ErrorMessage = "Monitor INGESTDATA";
                    autoTrainedFeatures.StartedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                    InsertAutoTrainLog(autoTrainedFeatures);
                    //added Logs in collection for Model  Monitoring

                    modelMonitorRetrain = _instaAutoRetrainService.IngestData(result[i]);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), nameof(Worker), "--Monitor INGESTDATA END STATUS--" + modelMonitorRetrain.Status + "--MESSAGE--" + modelMonitorRetrain.Message, string.IsNullOrEmpty(Convert.ToString(result[i][CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[i][CONSTANTS.CorrelationId])), string.Empty, string.Empty, string.Empty, string.Empty);
                    if (modelMonitorRetrain.Status == "C")
                    {
                        //added Logs in collection for Model  Monitoring
                        autoTrainedFeatures.FeatureName = "ModelMonitoring";
                        autoTrainedFeatures.ModelList = CorIdarr;
                        autoTrainedFeatures.Sequence = 3;
                        autoTrainedFeatures.ModelsCount = 1;
                        autoTrainedFeatures.EndedOn = null;
                        autoTrainedFeatures.FunctionName = "ModelMonitorAutoRetrain";
                        autoTrainedFeatures.ErrorMessage = "Monitor DATEENGINEERING";
                        autoTrainedFeatures.StartedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                        InsertAutoTrainLog(autoTrainedFeatures);
                        //added Logs in collection for Model  Monitoring
                        modelMonitorRetrain = _instaAutoRetrainService.GetInstaAutoDataEngineering(result[i]);
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), nameof(Worker), "--Monitor DATEENGINEERING END STATUS--" + modelMonitorRetrain.Status + "--MESSAGE--" + modelMonitorRetrain.Message, string.IsNullOrEmpty(Convert.ToString(result[i][CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[i][CONSTANTS.CorrelationId])), string.Empty, string.Empty, string.Empty, string.Empty);
                    }
                    if (modelMonitorRetrain.Status == "C")
                    {
                        //added Logs in collection for Model  Monitoring
                        autoTrainedFeatures.FeatureName = "ModelMonitoring";
                        autoTrainedFeatures.ModelList = CorIdarr;
                        autoTrainedFeatures.Sequence = 4;
                        autoTrainedFeatures.ModelsCount = 1;
                        autoTrainedFeatures.EndedOn = null;
                        autoTrainedFeatures.FunctionName = "ModelMonitorAutoRetrain";
                        autoTrainedFeatures.ErrorMessage = "InitiateModelMonitor - Model Monitor API Start";
                        autoTrainedFeatures.StartedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                        InsertAutoTrainLog(autoTrainedFeatures);
                        //added Logs in collection for Model  Monitoring
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "Monitor - Model Monitor API Start", "STARTED", string.IsNullOrEmpty(Convert.ToString(result[i][CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[i][CONSTANTS.CorrelationId])), string.Empty, string.Empty, string.Empty, string.Empty);
                        modelMonitorRetrain = _instaAutoRetrainService.InitiateModelMonitor(result[i]);
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), nameof(Worker), "--Monitor - Model Monitor API end--" + modelMonitorRetrain.Status, string.IsNullOrEmpty(Convert.ToString(result[i][CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[i][CONSTANTS.CorrelationId])), string.Empty, string.Empty, string.Empty, string.Empty);
                    }
                    if (modelMonitorRetrain.Message != null && modelMonitorRetrain.Message.ToLower() == "unhealthy")
                    {
                        //added Logs in collection for Model  Monitoring
                        autoTrainedFeatures.FeatureName = "ModelMonitoring";
                        autoTrainedFeatures.ModelList = CorIdarr;
                        autoTrainedFeatures.Sequence = 5;
                        autoTrainedFeatures.ModelsCount = 1;
                        autoTrainedFeatures.EndedOn = null;
                        autoTrainedFeatures.FunctionName = "ModelMonitorAutoRetrain";
                        autoTrainedFeatures.ErrorMessage = "Monitor AUTO TRAIN MODELENGINEERING";
                        autoTrainedFeatures.StartedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                        InsertAutoTrainLog(autoTrainedFeatures);
                        //added Logs in collection for Model  Monitoring
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "Monitor AUTO TRAIN MODELENGINEERING", "STARTED", string.IsNullOrEmpty(Convert.ToString(result[i][CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[i][CONSTANTS.CorrelationId])), string.Empty, string.Empty, string.Empty, string.Empty);
                        modelMonitorRetrain = _instaAutoRetrainService.GetInstaAutoModelEngineering(result[i]);
                        if (modelMonitorRetrain.Status == "C")
                        {
                            _instaAutoRetrainService.UpdateDeployedModelHealth(result[i]);
                        }
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), nameof(Worker), "--Monitor MODEL ENGINEERING END STATUS--" + modelMonitorRetrain.Status, string.IsNullOrEmpty(Convert.ToString(result[i][CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[i][CONSTANTS.CorrelationId])), string.Empty, string.Empty, string.Empty, string.Empty);
                    }
                    if (modelMonitorRetrain.Status == "C")
                    {
                        //added Logs in collection for Model  Monitoring
                        autoTrainedFeatures.FeatureName = "ModelMonitoring";
                        autoTrainedFeatures.ModelList = CorIdarr;
                        autoTrainedFeatures.Sequence = 6;
                        autoTrainedFeatures.ModelsCount = 1;
                        autoTrainedFeatures.EndedOn = null;
                        autoTrainedFeatures.FunctionName = "ModelMonitorAutoRetrain";
                        autoTrainedFeatures.ErrorMessage = "DeployModel Start";
                        autoTrainedFeatures.StartedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                        InsertAutoTrainLog(autoTrainedFeatures);
                        //added Logs in collection for Model  Monitoring

                        _instaAutoRetrainService.GetInstaAutoDeployPrediction(result[i]);
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), nameof(Worker), "--Monitor DEPLOY MODEL END--" + modelMonitorRetrain.Status, string.IsNullOrEmpty(Convert.ToString(result[i][CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[i][CONSTANTS.CorrelationId])), string.Empty, string.Empty, string.Empty, string.Empty);

                        //added Logs in collection for Model  Monitoring
                        autoTrainedFeatures.FeatureName = "ModelMonitoring";
                        autoTrainedFeatures.ModelList = CorIdarr;
                        autoTrainedFeatures.Sequence = 7;
                        autoTrainedFeatures.ModelsCount = 1;
                        autoTrainedFeatures.FunctionName = "ModelMonitorAutoRetrain";
                        autoTrainedFeatures.ErrorMessage = "DeployModel End";
                        autoTrainedFeatures.StartedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                        autoTrainedFeatures.EndedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                        InsertAutoTrainLog(autoTrainedFeatures);
                        //added Logs in collection for Model  Monitoring
                    }
                }
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), nameof(Worker), "Model Monitor AUTORETRAIN END", string.Empty, string.Empty, string.Empty, string.Empty);
            }
            catch (Exception ex)
            {
                //added Logs in collection for Model  Monitoring
                autoTrainedFeatures.FeatureName = "ModelMonitoring";
                autoTrainedFeatures.ModelList = CorIdarr;
                autoTrainedFeatures.Sequence = 8;
                autoTrainedFeatures.ModelsCount = 1;
                autoTrainedFeatures.FunctionName = "ModelMonitorAutoRetrain";
                autoTrainedFeatures.ErrorMessage = "Exception: " + ex.Message;
                autoTrainedFeatures.StartedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                autoTrainedFeatures.EndedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                InsertAutoTrainLog(autoTrainedFeatures);
                //added Logs in collection for Model  Monitoring
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(Worker), nameof(Worker), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
            //LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), nameof(Worker), "Model Monitor AUTORETRAIN END-", new Guid(correlationId));
        }
        private void delete_backUpModelSPP(string CorrelationId)
        {
            List<string> collectionNames = new List<string>();
            collectionNames.Add(CONSTANTS.PSIngestedData);
            collectionNames.Add(CONSTANTS.PSBusinessProblem);
            collectionNames.Add(CONSTANTS.DEDataCleanup);
            collectionNames.Add(CONSTANTS.DEDataProcessing);
            collectionNames.Add(CONSTANTS.DeployedPublishModel);
            collectionNames.Add(CONSTANTS.IngrainDeliveryConstruct);
            collectionNames.Add(CONSTANTS.ME_HyperTuneVersion);
            collectionNames.Add(CONSTANTS.SSAIRecommendedTrainedModels);
            collectionNames.Add(CONSTANTS.SSAIUserDetails);
            collectionNames.Add(CONSTANTS.WF_IngestedData);
            collectionNames.Add(CONSTANTS.WF_TestResults_);
            collectionNames.Add(CONSTANTS.WhatIfAnalysis);
            collectionNames.Add(CONSTANTS.DE_DataVisualization);
            collectionNames.Add(CONSTANTS.DEPreProcessedData);
            collectionNames.Add(CONSTANTS.DataCleanUPFilteredData);
            collectionNames.Add(CONSTANTS.MEFeatureSelection);
            collectionNames.Add(CONSTANTS.MERecommendedModels);
            collectionNames.Add(CONSTANTS.ME_TeachAndTest);
            collectionNames.Add(CONSTANTS.SSAIDeployedModels);
            collectionNames.Add(CONSTANTS.SSAIUseCase);
            collectionNames.Add(CONSTANTS.SSAIIngrainRequests);

            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, CorrelationId);
            var filter_backUp = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, CorrelationId + "_backUp");

            foreach (string name in collectionNames)
            {
                var collection = _database.GetCollection<BsonDocument>(name);
                if (collection.Find(filter_backUp).ToList().Count > 0)
                {
                    collection.DeleteMany(filter);
                    update_backUP(CorrelationId, name);
                }
            }
        }
        private void update_backUP(string CorrelationId, string collection_name)
        {
            var update = Builders<BsonDocument>.Update.Set("CorrelationId", CorrelationId.Replace("_backUp", ""));
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, CorrelationId + "_backUp");
            var collection = _database.GetCollection<BsonDocument>(collection_name);
            collection.UpdateMany(filter, update);
        }
        private void TerminatePythonModelTraining(IngrainRequestQueue result, FilterDefinitionBuilder<IngrainRequestQueue> filterBuilder)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), nameof(TerminatePythonModelTraining), CONSTANTS.START + "-PROCESSID-" + result.PythonProcessID, string.IsNullOrEmpty(result.CorrelationId) ? default(Guid) : new Guid(result.CorrelationId), result.AppID, string.Empty, result.ClientID, result.DeliveryconstructId);
            if (result != null)
            {
                var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.RequestId, result.RequestId);
                var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
                try
                {
                    if (result.PythonProcessID > 0)
                    {
                        Process processes = Process.GetProcessById(result.PythonProcessID);
                        if (processes != null)
                        {
                            processes.Kill();
                        }
                        var update = Builders<BsonDocument>.Update.Set(CONSTANTS.RequestStatus, "TaskCompleted").Set(CONSTANTS.Status, CONSTANTS.C).Set(CONSTANTS.Message, CONSTANTS.Success).Set(CONSTANTS.ModifiedOn, DateTime.Now.ToString(CONSTANTS.DateHoursFormat));
                        collection.UpdateMany(filter, update);
                    }
                    else
                    {
                        var update1 = Builders<BsonDocument>.Update.Set(CONSTANTS.RequestStatus, "E").Set(CONSTANTS.Status, CONSTANTS.E).Set(CONSTANTS.Message, CONSTANTS.Error).Set(CONSTANTS.ModifiedOn, DateTime.Now.ToString(CONSTANTS.DateHoursFormat));
                        collection.UpdateMany(filter, update1);
                    }
                }
                catch (ArgumentException ex)
                {
                    var update1 = Builders<BsonDocument>.Update.Set(CONSTANTS.RequestStatus, "Exception").Set(CONSTANTS.Status, CONSTANTS.E).Set(CONSTANTS.Message, ex.Message);
                    collection.UpdateMany(filter, update1);
                    LOGGING.LogManager.Logger.LogErrorMessage(typeof(Worker), nameof(TerminatePythonModelTraining), ex.StackTrace + "--" + ex.Message, ex, result.AppID, string.Empty, result.ClientID, result.DeliveryconstructId);
                }
                catch (InvalidOperationException ex)
                {
                    var update2 = Builders<BsonDocument>.Update.Set(CONSTANTS.RequestStatus, "Exception").Set(CONSTANTS.Status, CONSTANTS.E).Set(CONSTANTS.Message, ex.Message);
                    collection.UpdateMany(filter, update2);
                    LOGGING.LogManager.Logger.LogErrorMessage(typeof(Worker), nameof(TerminatePythonModelTraining), ex.StackTrace + "--" + ex.Message, ex, result.AppID, string.Empty, result.ClientID, result.DeliveryconstructId);
                }
                catch (Exception ex)
                {
                    var update3 = Builders<BsonDocument>.Update.Set(CONSTANTS.RequestStatus, "Exception").Set(CONSTANTS.Status, CONSTANTS.E).Set(CONSTANTS.Message, ex.Message);
                    collection.UpdateMany(filter, update3);
                    LOGGING.LogManager.Logger.LogErrorMessage(typeof(Worker), nameof(TerminatePythonModelTraining), ex.StackTrace + "--" + ex.Message, ex, result.AppID, string.Empty, result.ClientID, result.DeliveryconstructId);
                }
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), nameof(TerminatePythonModelTraining), CONSTANTS.END + "-PROCESSID-" + result.PythonProcessID, string.IsNullOrEmpty(result.CorrelationId) ? default(Guid) : new Guid(result.CorrelationId), result.AppID, string.Empty, result.ClientID, result.DeliveryconstructId);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), nameof(TerminatePythonModelTraining), CONSTANTS.END + "-PROCESSID-" + result.PythonProcessID, string.IsNullOrEmpty(result.CorrelationId) ? Guid.Empty : new Guid(result.CorrelationId), result.AppID, string.Empty, result.ClientID, result.DeliveryconstructId);
        }
        private void RetrainErrorModelWithWindowsService()
        {
            try
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "RetrainErrorModelWithWindowsService", "RETRAINERRORMODELWITHWINDOWSSERVICE SERVICE STARTED TIME- " + DateTime.Now.ToString(), string.Empty, string.Empty, string.Empty, string.Empty);
                string empty = string.Empty;
                autoTrainedFeatures.FeatureName = "WSModelRetrain";
                autoTrainedFeatures.Sequence = 2;
                autoTrainedFeatures.ModelsCount = 0;
                autoTrainedFeatures.FunctionName = "RetrainErrorModelWithWindowsService";
                autoTrainedFeatures.StartedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                autoTrainedFeatures.ModelList = new string[] { };
                InsertAutoTrainLog(autoTrainedFeatures);

                var collection = _database.GetCollection<IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
                DateTime days = DateTime.Now.AddDays(-appSettings.WSRetrainModelDays);
                string dateFilter = days.ToString(CONSTANTS.DateHoursFormat);
                var filterBuilder = Builders<IngrainRequestQueue>.Filter;
                var filter = filterBuilder.Eq(CONSTANTS.Function, CONSTANTS.AutoTrain)
                             & filterBuilder.Eq(CONSTANTS.Status, CONSTANTS.E)
                             & filterBuilder.Gt(CONSTANTS.CreatedOn, dateFilter)
                             & filterBuilder.Ne(CONSTANTS.TemplateUseCaseID, CONSTANTS.Null)
                             & filterBuilder.Ne(CONSTANTS.TemplateUseCaseID, BsonNull.Value)
                             & filterBuilder.Ne(CONSTANTS.TemplateUseCaseID, empty)
                             & filterBuilder.Regex(CONSTANTS.Message, new BsonRegularExpression(CONSTANTS.WSStartStatus));
                var ErrorModels = collection.Find(filter).Project<IngrainRequestQueue>(Builders<IngrainRequestQueue>.Projection.Exclude(CONSTANTS.Id)).ToList();
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "RETRAINERRORMODELWITHWINDOWSSERVICE33", "ERRORMODELS COUNT--" + ErrorModels.Count, string.Empty, string.Empty, string.Empty, string.Empty);
                if (ErrorModels.Count > 0)
                {
                    autoTrainedFeatures.ModelsCount = ErrorModels.Count();
                    var CorrelationIds = ErrorModels.Where(x => x.Function == CONSTANTS.AutoTrain).Select(y => y.CorrelationId);
                    autoTrainedFeatures.ModelList = CorrelationIds.ToArray().Take(10).Select(i => i.ToString()).ToArray();
                    autoTrainedFeatures.Sequence = 3;
                    autoTrainedFeatures.StartedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                    InsertAutoTrainLog(autoTrainedFeatures);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "RetrainErrorModelWithWindowsService", "RETRAINERRORMODELWITHWINDOWSSERVICE SERVICE STARTED2 COUNT--" + ErrorModels.Count, string.Empty, string.Empty, string.Empty, string.Empty);
                    if (ErrorModels.Count < appSettings.NoofModelToRetrain)
                    {
                        foreach (var item in ErrorModels)
                        {
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "RetrainErrorModelWithWindowsService", "RETRAINERRORMODELWITHWINDOWSSERVICE FOREACH CORRELATIONID--" + item.CorrelationId, item.AppID, string.Empty, item.ClientID, item.DeliveryconstructId);
                            //sending Notification to respective app SPA, SPP and VDS
                            SendNotificationForReInitiatingTraining(item);
                            //Delete existing all the requests for the CorrelationId except AutoTrain paggeInfo 
                            DeleteWSErrorModelsRequests(item.CorrelationId);
                            //update the status fields to default and initiate the training.
                            UpdateDefaultRequestVaues(item.RequestId, item.CorrelationId);
                            //End
                            var filterCorrelation = filterBuilder.Eq("CorrelationId", item.CorrelationId) & filterBuilder.Eq("RequestId", item.RequestId);
                            var projection = Builders<IngrainRequestQueue>.Projection.Exclude(CONSTANTS.Id);
                            GenericAutotrainService genericAutotrainService = new GenericAutotrainService(databaseProvider);
                            GenericAutoTrain genericAutoTrain = new GenericAutoTrain();
                            var results = collection.Find(filterCorrelation).Project<IngrainRequestQueue>(projection).FirstOrDefault();
                            if (results != null)
                            {
                                var update = Builders<IngrainRequestQueue>.Update.Set("RequestStatus", "Occupied").Set("Progress", "10%");
                                var isUpdated = _collection.UpdateMany(filterCorrelation, update);
                                Thread trainingThread = new Thread(() => genericAutotrainService.PrivateModelTraining(results));
                                trainingThread.Start();
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < appSettings.NoofModelToRetrain; i++)
                        {
                            //sending Notification to respective app SPA, SPP and VDS
                            SendNotificationForReInitiatingTraining(ErrorModels[i]);
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "RetrainErrorModelWithWindowsService", "RETRAINERRORMODELWITHWINDOWSSERVICE FOREACH CORRELATIONID--" + ErrorModels[i].CorrelationId, ErrorModels[i].AppID, string.Empty, ErrorModels[i].ClientID, ErrorModels[i].DeliveryconstructId);
                            //Delete existing all the requests for the CorrelationId except AutoTrain paggeInfo 
                            DeleteWSErrorModelsRequests(ErrorModels[i].CorrelationId);
                            //update the status fields to default and initiate the training.
                            UpdateDefaultRequestVaues(ErrorModels[i].RequestId, ErrorModels[i].CorrelationId);
                            //End
                            var filterCorrelation = filterBuilder.Eq("CorrelationId", ErrorModels[i].CorrelationId) & filterBuilder.Eq("RequestId", ErrorModels[i].RequestId);
                            var projection = Builders<IngrainRequestQueue>.Projection.Exclude(CONSTANTS.Id);
                            GenericAutotrainService genericAutotrainService = new GenericAutotrainService(databaseProvider);
                            GenericAutoTrain genericAutoTrain = new GenericAutoTrain();
                            var results = collection.Find(filterCorrelation).Project<IngrainRequestQueue>(projection).FirstOrDefault();
                            if (results != null)
                            {
                                var update = Builders<IngrainRequestQueue>.Update.Set("RequestStatus", "Occupied").Set("Progress", "10%");
                                var isUpdated = _collection.UpdateMany(filterCorrelation, update);
                                Thread trainingThread = new Thread(() => genericAutotrainService.PrivateModelTraining(results));
                                trainingThread.Start();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(Worker), nameof(RetrainErrorModelWithWindowsService), ex.StackTrace + "--" + ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                autoTrainedFeatures.EndedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                autoTrainedFeatures.Sequence = 5;
                autoTrainedFeatures.ErrorMessage = ex.Message + "--STACKTRACE--" + ex.StackTrace;
                UpdateAutoTrainLog(autoTrainedFeatures);
            }
            autoTrainedFeatures.EndedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
            autoTrainedFeatures.Sequence = 4;
            UpdateAutoTrainLog(autoTrainedFeatures);
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "RetrainErrorModelWithWindowsService", "RETRAINERRORMODELWITHWINDOWSSERVICE SERVICE END", string.Empty, string.Empty, string.Empty, string.Empty);
        }
        private void UpdateDefaultRequestVaues(string requestId, string correlationId)
        {
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.RequestId, requestId) & Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var update = Builders<BsonDocument>.Update.Set(CONSTANTS.RequestStatus, CONSTANTS.New)
                .Set(CONSTANTS.Progress, CONSTANTS.Null)
                .Set(CONSTANTS.Status, CONSTANTS.Null)
                .Set(CONSTANTS.SendNotification, CONSTANTS.Null)
                .Set(CONSTANTS.IsNotificationSent, CONSTANTS.Null)
                .Set(CONSTANTS.NotificationMessage, CONSTANTS.Null)
                .Set(CONSTANTS.IsRetrainedWSErrorModel, true)
                .Set(CONSTANTS.LastProcessedOn, DateTime.Now.ToString(CONSTANTS.DateHoursFormat))
                .Set(CONSTANTS.WSModelRetrainedOn, DateTime.Now.ToString(CONSTANTS.DateHoursFormat))
                .Set(CONSTANTS.ModifiedOn, DateTime.Now.ToString(CONSTANTS.DateHoursFormat))
                .Set(CONSTANTS.Message, CONSTANTS.Null);
            var isUpdated = collection.UpdateMany(filter, update);
        }
        private void DeleteWSErrorModelsRequests(string correlationId)
        {
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
            //var collection = _database.GetCollection<BsonDocument>("SSAI_IngrainRequests2");
            var filter = Builders<BsonDocument>.Filter.Ne(CONSTANTS.Function, CONSTANTS.AutoTrain) & Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            collection.DeleteMany(filter);
        }
        private void InsertAutoTrainLog(LogAutoTrainedFeatures autoTrainedFeatures)
        {
            var featuresLogCollection = _database.GetCollection<LogAutoTrainedFeatures>("SSAI_LogAutoTrainedFeatures");
            featuresLogCollection.InsertOne(autoTrainedFeatures);
        }
        private void InsertAIServiceAutoTrainLog(LogAIServiceAutoTrain logAIServiceAutoTrain)
        {
            var collection = _database.GetCollection<LogAIServiceAutoTrain>("AIServiceAutoTrainLog");
            collection.InsertOne(logAIServiceAutoTrain);
        }
        private void UpdateAutoTrainLog(LogAutoTrainedFeatures autoTrainedFeatures)
        {
            var filterBuilder = Builders<BsonDocument>.Filter;
            var filterqueue = filterBuilder.Eq("LogId", autoTrainedFeatures.LogId) & filterBuilder.Eq("Sequence", 1);
            var update = Builders<BsonDocument>.Update.Set("EndedOn", autoTrainedFeatures.EndedOn);
            var featuresLogCollection = _database.GetCollection<BsonDocument>("SSAI_LogAutoTrainedFeatures");
            featuresLogCollection.UpdateOne(filterqueue, update);
        }
        public void Dispose()
        {
            timer?.Dispose();
        }
        public void SetWorkerServiceStatus(bool IsStartAsynCalled = true)
        {
            try
            {
                var workerServiceJobsCollection = _database.GetCollection<WorkerServiceInfo>(CONSTANTS.SSAIWorkerServiceJobs);
                var filterBuilder = Builders<WorkerServiceInfo>.Filter;
                var filter = filterBuilder.Eq(CONSTANTS.Status, CONSTANTS.WorkerServiceRunning) | filterBuilder.Eq(CONSTANTS.Status, CONSTANTS.WorkerServiceStopped);
                var workerServiceJobs = workerServiceJobsCollection.Find(filter).ToList();
                string _systemDateTime = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                string _Status = string.Empty;

                if (workerServiceJobs.Count == 0 && IsStartAsynCalled)
                {
                    // Calling Method = 'StartAsync' ; Collection 'SSAI_WorkerServiceJobs' Count = 0 ; Insert into Collection 
                    var oWorkerService = new WorkerServiceInfo();
                    oWorkerService.Status = CONSTANTS.WorkerServiceRunning;
                    oWorkerService.CreatedBy = "SYSTEM";
                    oWorkerService.CreatedOn = _systemDateTime;
                    oWorkerService.ModifiedBy = "SYSTEM";
                    oWorkerService.ModifiedOn = _systemDateTime;
                    oWorkerService.LastStartedOn = _systemDateTime;
                    oWorkerService.Environment = appSettings.Environment;
                    workerServiceJobsCollection.InsertOne(oWorkerService);
                }
                else
                {
                    switch (IsStartAsynCalled)
                    {
                        case true:
                            {
                                // Calling Mehtod - 'StartAsync' ; Set status to 'RUNNING' ; Update 'ModifiedOn' and 'LastStartedOn' fields
                                _Status = CONSTANTS.WorkerServiceRunning;
                                filter = filterBuilder.Eq("Status", CONSTANTS.WorkerServiceStopped);
                                var windowServiceUpdate = Builders<WorkerServiceInfo>.Update.Set("Status", _Status).Set("ModifiedOn", _systemDateTime).Set("LastStartedOn", _systemDateTime);
                                workerServiceJobsCollection.UpdateOne(filter, windowServiceUpdate);
                                break;
                            }
                        case false:
                            {
                                // Calling Method - 'StopAsync' ; Set status = 'STOPPED' ; Update 'ModifiedOn' and 'LastStoppedOn' fields
                                _Status = CONSTANTS.WorkerServiceStopped;
                                filter = filterBuilder.Eq("Status", CONSTANTS.WorkerServiceRunning);
                                var windowServiceUpdate = Builders<WorkerServiceInfo>.Update.Set("Status", _Status).Set("ModifiedOn", _systemDateTime).Set("LastStoppedOn", _systemDateTime);
                                workerServiceJobsCollection.UpdateOne(filter, windowServiceUpdate);
                                break;
                            }
                        default:
                            {
                                // Default : Set status to 'RUNNING' ; Update 'ModifiedOn' and 'LastStartedOn' fields
                                _Status = CONSTANTS.WorkerServiceRunning;
                                filter = filterBuilder.Eq("Status", CONSTANTS.WorkerServiceStopped);
                                var windowServiceUpdate = Builders<WorkerServiceInfo>.Update.Set("Status", _Status).Set("ModifiedOn", _systemDateTime).Set("LastStartedOn", _systemDateTime);
                                workerServiceJobsCollection.UpdateOne(filter, windowServiceUpdate);
                                break;
                            }
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(Worker), nameof(SetWorkerServiceStatus), ex.StackTrace + "--" + ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
        }

        /// <summary>
        /// Given ElapsedTime 120 mins Time Interval
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void CallSimulationGenericPredictionRequest(object source, ElapsedEventArgs e)
        {
            try
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "CallSimulationGenericPredictionRequest", "Simulation Request Triggered" + DateTime.Now.ToString(), string.Empty, string.Empty, string.Empty, string.Empty);
                for (int i = 0; i < appSettings.SPPRequestLimit; i++)
                {
                    Thread ingrainRequestThread = new Thread(_genericAPICallingService.SimulationGenericAPICall);
                    ingrainRequestThread.Start();
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(Worker), nameof(CallSimulationGenericPredictionRequest), ex.StackTrace + "--" + ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
        }
        public void ArchivalPurgingTrigger()
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), nameof(ArchivalPurgingTrigger), "Archival purging Started -" + DateTime.Now.ToString(), string.Empty, string.Empty, string.Empty, string.Empty);
            bool ManualTriggerFlag = false;
            List<string> corrIds = new List<string>();
            int archive_days = Convert.ToInt32(appSettings.archivalDays);
            int ArchivalPurgingInterval = !string.IsNullOrEmpty(appSettings.ArchivalPurgingIntervalInDays) ? Convert.ToInt32(appSettings.ArchivalPurgingIntervalInDays) : 7;
            int days = 0;
            try
            {
                var ManualArchivalTasks = _database.GetCollection<ManualArchivalTasks>("ManualArchivalTasks");
                var ManualFilter = Builders<ManualArchivalTasks>.Filter.Where(x => x.TaskCode == "ARCHIEVE");
                var ManualProj = Builders<ManualArchivalTasks>.Projection.Exclude("_id");
                var Res = ManualArchivalTasks.Find(ManualFilter).Project<ManualArchivalTasks>(ManualProj).FirstOrDefault();
                if (Res != null)
                {
                    if (Res.ManualTrigger)
                    {
                        ManualTriggerFlag = Res.ManualTrigger;
                        corrIds = Res.CorrelationIds.Split(",").ToList();
                        archive_days = Convert.ToInt32(Res.FrequencyInDays);
                        ArchivalPurgingInterval = string.IsNullOrEmpty(Res.RunFrequencyInDays) ? Convert.ToInt32(Res.RunFrequencyInDays) : 1;
                    }
                }
                string LastArchivedDate = string.Empty;
                var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_DeployedModels_archive);
                var filter = Builders<BsonDocument>.Filter.Empty;
                var projection = Builders<BsonDocument>.Projection.Include("ArchiveDate").Exclude(CONSTANTS.Id);
                BsonDocument result = collection.Find(filter).Project<BsonDocument>(projection).SortByDescending(bson => bson["ArchiveDate"]).FirstOrDefault();
                if (result != null)
                {
                    LastArchivedDate = Convert.ToString(result["ArchiveDate"]);
                }
                DateTime lastExetime = string.IsNullOrEmpty(LastArchivedDate) ? DateTime.UtcNow.AddDays(-ArchivalPurgingInterval) : DateTime.Parse(LastArchivedDate);
                DateTime curDate = DateTime.UtcNow;
                days = (int)(curDate - lastExetime).TotalDays;
                if (days >= ArchivalPurgingInterval)
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), nameof(ArchivalPurgingTrigger), "Archiving and Purging Started -" + DateTime.Now.ToString(), string.Empty, string.Empty, string.Empty, string.Empty);
                    DeployModelService deployModelService = new DeployModelService();
                    deployModelService.ArchiveRecords(ManualTriggerFlag, corrIds, archive_days);
                    deployModelService.PurgeRecords();
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), nameof(ArchivalPurgingTrigger), "Archiving and Purging Ended -" + DateTime.Now.ToString(), string.Empty, string.Empty, string.Empty, string.Empty);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(Worker), nameof(ArchivalPurgingTrigger), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), nameof(ArchivalPurgingTrigger), "Archiving and Purging Ended -" + DateTime.Now.ToString() + "-" + Convert.ToString(corrIds.ToJson()), "ManualTriggerFlag:" + Convert.ToString(ManualTriggerFlag), "archive_days:" + Convert.ToString(archive_days), "ArchivalPurgingInterval:" + Convert.ToString(ArchivalPurgingInterval), "days:" + Convert.ToString(days));
        }
        #region Notification to SPP, SPA & VDS
        private string Insert_notification(string ApplicationId, string UserId, string CorrelationId, string AppNotificationUrl, string Status, string Progress, string Message, string TrainingReInitiationMsg, string clientId, string DCId)
        {
            string env = string.Empty;
            var appIntegrationCollection = _database.GetCollection<AppIntegration>(CONSTANTS.AppIntegration);
            var filter = Builders<AppIntegration>.Filter.Where(x => x.ApplicationID == ApplicationId);
            var app = appIntegrationCollection.Find(filter).FirstOrDefault();
            if (app != null)
                env = app.Environment;
            AppNotificationLog appNotificationLog = new AppNotificationLog
            {
                RequestId = Guid.NewGuid().ToString(),
                ClientUId = clientId,
                DeliveryConstructUId = DCId,
                ApplicationId = ApplicationId,
                CorrelationId = CorrelationId,
                UserId = UserId,
                AppNotificationUrl = AppNotificationUrl,
                IsNotified = false,
                Status = Status,
                StatusMessage = Message,
                Progress = Progress,
                CreatedBy = UserId,
                CreatedOn = DateTime.UtcNow.ToString(),
                Message = TrainingReInitiationMsg,
                ErrorMessage = string.Empty,
                Environment = env
            };
            var appNotificationLogCollection = _database.GetCollection<AppNotificationLog>(nameof(AppNotificationLog));
            appNotificationLogCollection.InsertOne(appNotificationLog);

            return "success";
        }
        private void SendNotificationForReInitiatingTraining(IngrainRequestQueue item)
        {
            GenericAutotrainService _genericAutotrainService = new GenericAutotrainService(databaseProvider);
            var Builder = Builders<PublicTemplateMapping>.Filter;
            var MappingCollection = _database.GetCollection<PublicTemplateMapping>(CONSTANTS.PublicTemplateMapping);
            IngrainResponseData CallBackResponse = new IngrainResponseData
            {
                CorrelationId = item.CorrelationId,
                Status = CONSTANTS.TrainingReInitiated,
                Message = CONSTANTS.TrainingReInitiationMsg,
                ErrorMessage = string.Empty,
            };
            var Projection1 = Builders<PublicTemplateMapping>.Projection.Exclude("_id");
            var UseCasefilter = Builder.Eq(CONSTANTS.ApplicationID, item.AppID) & Builder.Eq(CONSTANTS.UsecaseID, item.TemplateUseCaseID);
            var templatedata = MappingCollection.Find(UseCasefilter).Project<PublicTemplateMapping>(Projection1).FirstOrDefault();
            if (templatedata != null)
            {
                if (templatedata.IsMultipleApp == "yes")
                {
                    var AppIntegCollection = _database.GetCollection<AppIntegration>(CONSTANTS.AppIntegration);
                    var filterBuilder1 = Builders<AppIntegration>.Filter;
                    var AppFilter = filterBuilder1.Eq(CONSTANTS.ApplicationID, item.AppID);

                    var Projection = Builders<AppIntegration>.Projection.Exclude("_id");
                    var AppData = AppIntegCollection.Find(AppFilter).Project<AppIntegration>(Projection).FirstOrDefault();
                    item.ApplicationName = AppData.ApplicationName;
                }
                else
                {
                    item.ApplicationName = templatedata.ApplicationName;
                }
            }
            //For SPE Notification
            if (item.ApplicationName == CONSTANTS.SPEAPP && item.AppID == SPE_appId)
            {
                Insert_notification(SPE_appId, item.CreatedByUser, item.CorrelationId, item.AppURL, CONSTANTS.P, "10%", CONSTANTS.TrainingReInitiated, CONSTANTS.TrainingReInitiationMsg, item.ClientId, item.DeliveryconstructId);
            }
            //For SPP Notification
            if (!(string.IsNullOrEmpty(item.AppURL)) && (!(string.IsNullOrEmpty(item.ApplicationName)) && (!string.IsNullOrEmpty(item.AppID))))
            {
                string callbackResonse = _genericAutotrainService.CallbackResponse(CallBackResponse, item.ApplicationName, item.AppURL, item.ClientId, item.DeliveryconstructId, item.AppID, item.TemplateUseCaseID, item.RequestId, null, item.CreatedByUser);
            }
            else
            {
                _genericAutotrainService.CallBackErrorLog(CallBackResponse, item.ApplicationName, item.AppURL, null, item.ClientId, item.DeliveryconstructId, item.AppID, item.TemplateUseCaseID, item.RequestId, null, item.CreatedByUser);
            }
        }
        #endregion Notification to SPP, SPA & VDS
    }
}