using System;
using System.Collections.Generic;
using System.Text;
using WINSERVICEMODELS = Accenture.MyWizard.Ingrain.WindowService.Models;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Linq;
using Accenture.MyWizard.Ingrain.WindowService.Interfaces;
using DATAMODELS = Accenture.MyWizard.Ingrain.DataModels.Models;
using AICOREMODELS = Accenture.MyWizard.Ingrain.DataModels.AICore;
using Accenture.MyWizard.Ingrain.DataModels.Models;
using MongoDB.Bson.Serialization;
using Accenture.MyWizard.Ingrain.WindowService.Models;
using Newtonsoft.Json;

namespace Accenture.MyWizard.Ingrain.WindowService.Services
{
    public class QueueManagerService
    {
        private readonly WINSERVICEMODELS.AppSettings appSettings = null;
        private DatabaseProvider databaseProvider;
        private IMongoDatabase _database;
        private IMongoCollection<DATAMODELS.IngrainRequestQueue> _collection;
        private IMongoCollection<DATAMODELS.DeployModelsDto> _deployModelCollection;
        private IMongoCollection<DATAMODELS.PublishModelDTO> _publishModelCollection;
        private IMongoDatabase _databaseAD;

        public QueueManagerService()
        {
            appSettings = AppSettingsJson.GetAppSettings().GetSection("AppSettings").Get<WINSERVICEMODELS.AppSettings>();
            databaseProvider = new DatabaseProvider();
            string connectionString = appSettings.connectionString;
            var dataBaseName = MongoUrl.Create(connectionString).DatabaseName;
            MongoClient mongoClient = databaseProvider.GetDatabaseConnection();
            _database = mongoClient.GetDatabase(dataBaseName);
            _collection = _database.GetCollection<DATAMODELS.IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
            _deployModelCollection = _database.GetCollection<DATAMODELS.DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
            _publishModelCollection = _database.GetCollection<DATAMODELS.PublishModelDTO>(CONSTANTS.SSAI_PublishModel);
            //Anomaly Detection connection
            var dataBaseNameAD = MongoUrl.Create(appSettings.AnomalyDetectionCS).DatabaseName;
            MongoClient mongoClientAD = databaseProvider.GetDatabaseConnection("Anomaly");
            _databaseAD = mongoClientAD.GetDatabase(dataBaseNameAD);
        }


        public void UpdateQueueMonitor()
        {
            try
            {
                WINSERVICEMODELS.QueueMonitor queueMonitor = new WINSERVICEMODELS.QueueMonitor();
                var queueCollection = _database.GetCollection<WINSERVICEMODELS.QueueMonitor>("QueueMonitor");
                var queueProjection = Builders<WINSERVICEMODELS.QueueMonitor>.Projection.Exclude("_id");
                var queueFilter = Builders<WINSERVICEMODELS.QueueMonitor>.Filter.Eq("QueueName", "PredictionQueue");
                var res = queueCollection.Find(queueFilter).Project<WINSERVICEMODELS.QueueMonitor>(queueProjection).ToList();

                //add record in collection if not available
                if (res.Count <= 0)
                {
                    queueMonitor.QueueName = "PredictionQueue";
                    queueMonitor.CreatedOn = DateTime.UtcNow.ToString();
                    queueMonitor.CreatedBy = "SYSTEM";
                    queueMonitor.ModifiedOn = DateTime.UtcNow.ToString();
                    queueMonitor.ModifiedBy = "SYSTEM";

                    var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(queueMonitor);
                    var insertDocument = BsonSerializer.Deserialize<WINSERVICEMODELS.QueueMonitor>(jsonData);
                    queueCollection.InsertOne(insertDocument);
                }
                else
                {
                    queueMonitor = res[0];
                }


                //fetch requests 

                var filterBuilder = Builders<DATAMODELS.IngrainRequestQueue>.Filter;
                var Filter = (filterBuilder.Eq("RequestStatus", appSettings.requestInProgress) | filterBuilder.Eq("RequestStatus", "Occupied")) & (filterBuilder.Eq("pageInfo", "PublishModel") | filterBuilder.Eq("pageInfo", "ForecastModel"));
                var ingrainRequests = _collection.Find(Filter).ToList();
                var temp = ingrainRequests.Where(x => !string.IsNullOrEmpty(x.AppID)).ToList();
                var notNullRequests = temp.Where(x => (x.RequestStatus == appSettings.requestInProgress && Convert.ToDateTime(x.CreatedOn) >= DateTime.Now.AddMinutes(-5)) || (x.RequestStatus == "Occupied" && Convert.ToDateTime(x.CreatedOn) >= DateTime.Now.AddMinutes(-5)));



                List<WINSERVICEMODELS.AppQueue> groupedRequests = (from request in notNullRequests
                                                                   group request by request.AppID into appGroup
                                                                   select new WINSERVICEMODELS.AppQueue
                                                                   {
                                                                       AppId = appGroup.Key,
                                                                       CurrentInprogressCount = appGroup.Count().ToString(),

                                                                   }).ToList();



                //update queue monitor
                queueMonitor.AppWiseQueueDetails = new List<WINSERVICEMODELS.AppQueue>();
                //groupedRequests.Add(new WINSERVICEMODELS.AppQueue
                //{
                //    AppId = "123",
                //    CurrentInprogressCount = "12"
                //});
                //queueMonitor.TotalQueueLimit = Math.Round(Environment.ProcessorCount * 0.6).ToString();
                queueMonitor.TotalQueueLimit = appSettings.PredictionQueueLimit;
                var appCollection = _database.GetCollection<AppIntegration>(CONSTANTS.AppIntegration);
                var appFilter = Builders<AppIntegration>.Filter.Empty;
                var response = appCollection.Find(appFilter).ToList();
                if (response.Count > 0)
                {
                    int appQueueLimit = Convert.ToInt32(queueMonitor.TotalQueueLimit) / response.Count;

                    int totalInprogressCount = 0;
                    foreach (var app in response)
                    {
                        WINSERVICEMODELS.AppQueue appQueue = new WINSERVICEMODELS.AppQueue();
                        appQueue.AppId = app.ApplicationID;
                        appQueue.QueueLimit = appQueueLimit.ToString();
                        if (groupedRequests.Count > 0)
                        {
                            appQueue.CurrentInprogressCount = groupedRequests.Any(c => c.AppId == appQueue.AppId)
                                ? groupedRequests.Single(c => c.AppId == appQueue.AppId).CurrentInprogressCount : "0";
                        }
                        else
                        {
                            appQueue.CurrentInprogressCount = "0";
                        }

                        appQueue.QueueStatus = Convert.ToInt32(appQueue.CurrentInprogressCount) < Convert.ToInt32(appQueue.QueueLimit) ? "Available" : "Occupied";
                        queueMonitor.AppWiseQueueDetails.Add(appQueue);
                        totalInprogressCount += Convert.ToInt32(appQueue.CurrentInprogressCount);
                    }

                    queueMonitor.CurrentInprogressCount = totalInprogressCount.ToString();
                    if (totalInprogressCount < Convert.ToInt32(queueMonitor.TotalQueueLimit))
                        queueMonitor.QueueStatus = "Available";
                    else
                        queueMonitor.QueueStatus = "Occupied";


                    queueMonitor.ModifiedOn = DateTime.UtcNow.ToString();
                    var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(queueMonitor);
                    var insertDocument = BsonSerializer.Deserialize<WINSERVICEMODELS.QueueMonitor>(jsonData);
                    queueCollection.DeleteOne(queueFilter);
                    queueCollection.InsertOne(insertDocument);
                }


            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(QueueManagerService), nameof(UpdateQueueMonitor), ex.Message + "- STACKTRACE - " + ex.StackTrace + ex.InnerException, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }

        }

        public void UpdateTrainingQueueStatus()
        {
            try
            {
                var queueCollection = _database.GetCollection<WINSERVICEMODELS.QueueMonitor>(nameof(WINSERVICEMODELS.QueueMonitor));
                var queueFilter = Builders<WINSERVICEMODELS.QueueMonitor>.Filter.Where(x => x.QueueName == "TrainingQueue");
                var prj = Builders<WINSERVICEMODELS.QueueMonitor>.Projection.Exclude("_id");
                var collectionResult = queueCollection.Find(queueFilter).Project<WINSERVICEMODELS.QueueMonitor>(prj).FirstOrDefault();
                if(collectionResult == null)
                {
                    WINSERVICEMODELS.QueueMonitor queueMonitor = new WINSERVICEMODELS.QueueMonitor();
                    queueMonitor.QueueName = "TrainingQueue";
                    queueMonitor.CreatedOn = DateTime.UtcNow.ToString();
                    queueMonitor.CreatedBy = "SYSTEM";
                    queueMonitor.ModifiedOn = DateTime.UtcNow.ToString();
                    queueMonitor.ModifiedBy = "SYSTEM";
                    queueMonitor.TotalQueueLimit = appSettings.TrainingQueueLimit;
                    queueCollection.InsertOne(queueMonitor);
                }

                int counter = 0;
                string status = string.Empty;
               
                var requestCollection = _database.GetCollection<DATAMODELS.IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
                var filterBuilder = Builders<DATAMODELS.IngrainRequestQueue>.Filter;
                var Filter = (filterBuilder.Eq("RequestStatus", appSettings.requestInProgress) | filterBuilder.Eq("RequestStatus", "Occupied"))
                             & filterBuilder.In("Function", new List<string>() { "FileUpload", "DataCleanUp", "AddFeature", "DataTransform", "RecommendedAI", "WFAnalysis", "WFIngestData", "HyperTune", "ViewDataQuality", "PrescriptiveAnalytics" });
                var projection = Builders<DATAMODELS.IngrainRequestQueue>.Projection.Exclude("_id");
                var requestQueue = requestCollection.Find(Filter).Project<DATAMODELS.IngrainRequestQueue>(projection).ToList();
                if (requestQueue.Count > 0)
                {
                    var recommendAIRequests = requestQueue.Where(x => x.Function == "RecommendedAI").ToList();
                    counter += recommendAIRequests.Count * 2;
                    var otherRequests = requestQueue.Where(x => x.Function != "RecommendedAI").ToList();
                    counter += otherRequests.Count;
                }

                if(counter < Convert.ToInt32(appSettings.TrainingQueueLimit))
                {
                    status = "Available";
                }
                else
                {
                    status = "Occupied";
                }

                var updateBuilder = Builders<WINSERVICEMODELS.QueueMonitor>.Update.Set(x => x.CurrentInprogressCount, counter.ToString())
                                                                                  .Set(x => x.TotalQueueLimit, appSettings.TrainingQueueLimit)
                                                                                  .Set(x => x.ModifiedOn, DateTime.UtcNow.ToString())
                                                                                  .Set(x => x.QueueStatus,status);
                queueCollection.UpdateOne(queueFilter, updateBuilder);
                                    
               
            }
            catch(Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(QueueManagerService), nameof(UpdateTrainingQueueStatus), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
            
                            
        }

        public void UpdateAITrainingQueueStatus(string AIService)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(QueueManagerService), nameof(UpdateAITrainingQueueStatus), "AIService : " + AIService + CONSTANTS.START, string.Empty, string.Empty, string.Empty, string.Empty);
            string queName = string.Empty;
            if (AIService == "CLUSTERING")
                queName = "ClusteringServiceQueue";
            else if (AIService == "WORDCLOUD")
                queName = "WordCloudServiceQueue";
            else
                queName = "AIServiceTrainingQueue";
            try
            {
                var queueCollection = _database.GetCollection<WINSERVICEMODELS.QueueMonitor>(nameof(WINSERVICEMODELS.QueueMonitor));
                var queueFilter = Builders<WINSERVICEMODELS.QueueMonitor>.Filter.Where(x => x.QueueName == queName);
                var prj = Builders<WINSERVICEMODELS.QueueMonitor>.Projection.Exclude("_id");
                var collectionResult = queueCollection.Find(queueFilter).Project<WINSERVICEMODELS.QueueMonitor>(prj).FirstOrDefault();
                if (collectionResult == null)
                {
                    WINSERVICEMODELS.QueueMonitor queueMonitor = new WINSERVICEMODELS.QueueMonitor();
                    queueMonitor.QueueName = queName;
                    queueMonitor.CreatedOn = DateTime.UtcNow.ToString();
                    queueMonitor.CreatedBy = "SYSTEM";
                    queueMonitor.ModifiedOn = DateTime.UtcNow.ToString();
                    queueMonitor.ModifiedBy = "SYSTEM";
                    queueMonitor.TotalQueueLimit = appSettings.AITrainingQueueLimit;
                    queueCollection.InsertOne(queueMonitor);
                }

                int counter = 0;
                string status = string.Empty;

                var requestCollection = _database.GetCollection<AICOREMODELS.AIServiceRequestStatus>(CONSTANTS.AIServiceRequestStatus);
                var filterBuilder = Builders<AICOREMODELS.AIServiceRequestStatus>.Filter;
                var Filter = filterBuilder.Eq("Status", "P") | filterBuilder.Eq("Status", "I");
                            
                var projection = Builders<AICOREMODELS.AIServiceRequestStatus>.Projection.Exclude("_id");
                var requestQueue = requestCollection.Find(Filter).Project<AICOREMODELS.AIServiceRequestStatus>(projection).ToList();
                if (requestQueue.Count > 0)
                {
                    counter += requestQueue.Count;
                }

                if (counter < Convert.ToInt32(appSettings.AITrainingQueueLimit))
                {
                    status = "Available";
                }
                else
                {
                    status = "Occupied";
                }

                var updateBuilder = Builders<WINSERVICEMODELS.QueueMonitor>.Update.Set(x => x.CurrentInprogressCount, counter.ToString())
                                                                                  .Set(x => x.ModifiedOn, DateTime.UtcNow.ToString())
                                                                                  .Set(x => x.QueueStatus, status)
                                                                                  .Set(x => x.TotalQueueLimit, appSettings.AITrainingQueueLimit);
                queueCollection.UpdateOne(queueFilter, updateBuilder);


            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(QueueManagerService), nameof(UpdateAITrainingQueueStatus), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(QueueManagerService), nameof(UpdateAITrainingQueueStatus), "AIService : " + AIService + CONSTANTS.END, string.Empty, string.Empty, string.Empty, string.Empty);

        }




        public string GetTrainingQueueStatus()
        {
            string status = string.Empty;
            var queueCollection = _database.GetCollection<WINSERVICEMODELS.QueueMonitor>(nameof(WINSERVICEMODELS.QueueMonitor));
            var filter = Builders<WINSERVICEMODELS.QueueMonitor>.Filter.Where(x => x.QueueName == "TrainingQueue");
            var projection = Builders<WINSERVICEMODELS.QueueMonitor>.Projection.Exclude("_id");
            var collectionResult = queueCollection.Find(filter).Project<WINSERVICEMODELS.QueueMonitor>(projection).FirstOrDefault();
            if (collectionResult != null)
            {
                status = collectionResult.QueueStatus;
            }
            else
            {
                status = "Available";
            }
            return status;
        }

        public string GetAITrainingQueueStatus(string AIService)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(QueueManagerService), nameof(GetAITrainingQueueStatus), "AIService : " + AIService + CONSTANTS.START, string.Empty, string.Empty, string.Empty, string.Empty);
            string status = string.Empty;
            string queName = string.Empty;
            if (AIService == "CLUSTERING")
                queName = "ClusteringServiceQueue";
            else if (AIService == "WORDCLOUD")
                queName = "WordCloudServiceQueue";
            else
                queName = "AIServiceTrainingQueue";
            var queueCollection = _database.GetCollection<WINSERVICEMODELS.QueueMonitor>(nameof(WINSERVICEMODELS.QueueMonitor));
            var filter = Builders<WINSERVICEMODELS.QueueMonitor>.Filter.Where(x => x.QueueName == queName);
            var projection = Builders<WINSERVICEMODELS.QueueMonitor>.Projection.Exclude("_id");
            var collectionResult = queueCollection.Find(filter).Project<WINSERVICEMODELS.QueueMonitor>(projection).FirstOrDefault();
            if (collectionResult != null)
            {
                status = collectionResult.QueueStatus;
            }
            else
            {
                status = "Available";
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(QueueManagerService), nameof(GetAITrainingQueueStatus), "AIService : " + AIService + "status : " + status + CONSTANTS.END, string.Empty, string.Empty, string.Empty, string.Empty);
            return status;
        }




        public WINSERVICEMODELS.QueueMonitor GetQueueStatus()
        {
            var queueCollection = _database.GetCollection<WINSERVICEMODELS.QueueMonitor>("QueueMonitor");
            var queueFilter2 = Builders<WINSERVICEMODELS.QueueMonitor>.Filter.Eq("QueueName", "PredictionQueue");
            var projection = Builders<WINSERVICEMODELS.QueueMonitor>.Projection.Exclude("_id");
            var queueRes = queueCollection.Find(queueFilter2).Project<WINSERVICEMODELS.QueueMonitor>(projection).FirstOrDefault();
            return queueRes;
        }

        public void UpdateQueueStatus(DATAMODELS.IngrainRequestQueue result, FilterDefinitionBuilder<DATAMODELS.IngrainRequestQueue> filterBuilder)
        {
            var filterCorrelation = filterBuilder.Eq("CorrelationId", result.CorrelationId) & filterBuilder.Eq("RequestId", result.RequestId) & filterBuilder.Eq("RequestStatus", "New");
            var update = Builders<DATAMODELS.IngrainRequestQueue>.Update.Set("Message", "This request is queued")
                                                                          .Set("RequestStatus", "Queued");
            _collection.UpdateOne(filterCorrelation, update);

        }
        public void UpdatePublishModel(string correlationId, string uniqueId)
        {
            var filterBuilder = Builders<DATAMODELS.PublishModelDTO>.Filter;
            var filterCorrelation = filterBuilder.Eq(CONSTANTS.CorrelationId, correlationId) & filterBuilder.Eq("UniqueId", uniqueId);
            var update = Builders<DATAMODELS.PublishModelDTO>.Update.Set("Status", "I")
                                                                        .Set("ErrorMessage", "This request is queued");
            _publishModelCollection.UpdateOne(filterCorrelation, update);
        }

        public string GetAppId(string correlationId)
        {
            string appId = null;
            //total inprogress requests
            var filterBuilder = Builders<DATAMODELS.DeployModelsDto>.Filter;
            var Filter = filterBuilder.Eq(CONSTANTS.CorrelationId, correlationId);

            var model = _deployModelCollection.Find(Filter).FirstOrDefault();

            if (model != null)
            {
                if (!string.IsNullOrEmpty(model.AppId))
                    appId = model.AppId;
            }
            return appId;
        }

        #region commented code
        //public List<string> GetListOfAppCorrelationIds(string appId)
        //{           
        //    //total inprogress requests
        //    var filterBuilder = Builders<DATAMODELS.DeployModelsDto>.Filter;
        //    var Filter = filterBuilder.Eq("AppId", appId);

        //    var models = _deployModelCollection.Find(Filter).ToList();

        //    if (models.Count > 0)
        //    {

        //    }
        //    return appId;
        //}
        //public int CheckPredictionsQueue(string appId, string requestId)
        //{
        //    int result = 0;
        //    int totalPredictionsQueueLn = Convert.ToInt32(appSettings.PredictionQueueLimit);


        //    //total inprogress requests
        //    var filterBuilder = Builders<WINSERVICEMODELS.IngrainRequests>.Filter;
        //    var Filter = filterBuilder.Eq("RequestStatus", appSettings.requestInProgress)
        //        & (filterBuilder.Eq(CONSTANTS.pageInfo, CONSTANTS.PublishModel) | filterBuilder.Eq(CONSTANTS.pageInfo, CONSTANTS.ForecastModel));
        //    var totalPredictionRequests = _collection.Find(Filter).ToList();

        //    //total queued requests
        //    var Filter2 = (filterBuilder.Eq("RequestStatus", appSettings.requestNew) | filterBuilder.Eq("RequestStatus", "Queued"))
        //        & (filterBuilder.Eq(CONSTANTS.pageInfo, CONSTANTS.PublishModel) | filterBuilder.Eq(CONSTANTS.pageInfo, CONSTANTS.ForecastModel));
        //    var totalQueuedRequests = _collection.Find(Filter).ToList();

        //    //app requests count
        //    var appPredictionRequests = totalPredictionRequests.Where(p => p.AppID == appId).ToList();
        //    var appQueuedRequests = totalQueuedRequests.Where(p => p.AppID == appId).ToList();

        //    WINSERVICEMODELS.IngrainRequests currentRequest = totalQueuedRequests.Where(p => p.RequestId == requestId).FirstOrDefault();


        //    var appIntegrationCollection = _database.GetCollection<BsonDocument>(CONSTANTS.AppIntegration);
        //    var appFilter = Builders<BsonDocument>.Filter.Eq("AppId", appId);
        //    var app = appIntegrationCollection.Find(appFilter).FirstOrDefault();

        //    if (app != null)
        //    {
        //        int appQueuePerc = Convert.ToInt32(app["PredictionQueueLength"].AsString);
        //        int appPredictionQueueLn = (appQueuePerc / 100) * totalPredictionsQueueLn;


        //        //var appPredictionQueueLn = !app["PredictionQueueLength"].IsBsonNull ? Convert.ToInt32(app["PredictionQueueLength"].AsString) : 20;
        //        if (totalPredictionRequests.Count < totalPredictionsQueueLn)
        //        {
        //            if (appPredictionRequests.Count < appPredictionQueueLn)
        //            {
        //                result = 0;
        //            }
        //            else
        //            {
        //                result = appQueuedRequests.Where(p => DateTime.Parse(p.CreatedOn) < DateTime.Parse(currentRequest.CreatedOn)).ToList().Count;
        //            }
        //        }
        //        else
        //        {
        //            result = totalQueuedRequests.Where(p => DateTime.Parse(p.CreatedOn) < DateTime.Parse(currentRequest.CreatedOn)).ToList().Count;
        //        }
        //    }

        //    return result;
        //}
        #endregion


        #region IE
        public void UpdateIETrainingQueueStatus()
        {
            try
            {
                var queueCollection = _database.GetCollection<WINSERVICEMODELS.QueueMonitor>(nameof(WINSERVICEMODELS.QueueMonitor));
                var queueFilter = Builders<WINSERVICEMODELS.QueueMonitor>.Filter.Where(x => x.QueueName == "IETrainingQueue");
                var prj = Builders<WINSERVICEMODELS.QueueMonitor>.Projection.Exclude("_id");
                var collectionResult = queueCollection.Find(queueFilter).Project<WINSERVICEMODELS.QueueMonitor>(prj).FirstOrDefault();
                if (collectionResult == null)
                {
                    WINSERVICEMODELS.QueueMonitor queueMonitor = new WINSERVICEMODELS.QueueMonitor();
                    queueMonitor.QueueName = "IETrainingQueue";
                    queueMonitor.CreatedOn = DateTime.UtcNow.ToString();
                    queueMonitor.CreatedBy = "SYSTEM";
                    queueMonitor.ModifiedOn = DateTime.UtcNow.ToString();
                    queueMonitor.ModifiedBy = "SYSTEM";
                    queueMonitor.TotalQueueLimit = appSettings.AITrainingQueueLimit;
                    queueCollection.InsertOne(queueMonitor);
                }

                int counter = 0;
                string status = string.Empty;

                var requestCollection = _database.GetCollection<IngrainRequestQueue>(CONSTANTS.IE_RequestQueue);
                var filterBuilder = Builders<IngrainRequestQueue>.Filter;
                var Filter = filterBuilder.Eq("Status", "P") | filterBuilder.Eq("Status", "I");

                var projection = Builders<IngrainRequestQueue>.Projection.Exclude("_id");
                var requestQueue = requestCollection.Find(Filter).Project<IngrainRequestQueue>(projection).ToList();
                if (requestQueue.Count > 0)
                {
                    counter += requestQueue.Count;
                }

                if (counter < Convert.ToInt32(appSettings.AITrainingQueueLimit))
                {
                    status = "Available";
                }
                else
                {
                    status = "Occupied";
                }

                var updateBuilder = Builders<WINSERVICEMODELS.QueueMonitor>.Update.Set(x => x.CurrentInprogressCount, counter.ToString())
                                                                                  .Set(x => x.ModifiedOn, DateTime.UtcNow.ToString())
                                                                                  .Set(x => x.QueueStatus, status);
                queueCollection.UpdateOne(queueFilter, updateBuilder);


            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(QueueManagerService), nameof(UpdateIETrainingQueueStatus), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
        }


        public string GetIETrainingQueueStatus()
        {
            string status = string.Empty;
            var queueCollection = _database.GetCollection<WINSERVICEMODELS.QueueMonitor>(nameof(WINSERVICEMODELS.QueueMonitor));
            var filter = Builders<WINSERVICEMODELS.QueueMonitor>.Filter.Where(x => x.QueueName == "IETrainingQueue");
            var projection = Builders<WINSERVICEMODELS.QueueMonitor>.Projection.Exclude("_id");
            var collectionResult = queueCollection.Find(filter).Project<WINSERVICEMODELS.QueueMonitor>(projection).FirstOrDefault();
            if (collectionResult != null)
            {
                status = collectionResult.QueueStatus;
            }
            else
            {
                status = "Available";
            }
            return status;
        }

        #endregion IE

        #region Temporary
        //For SPP Issue more records piled up. Total 58989 Ingrain request in queue 
        public void RequestBatchLimitInsert(int count, string collectionName, string ServiceName ="")
        {
            RequestBatchLimitMonitor request = new RequestBatchLimitMonitor
            {
                Count = count,
                CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
            };
            this.InsertRequests(request, collectionName, ServiceName);
        }

        public void InsertRequests<T>(T data, string collectionName, string ServiceName = "")
        {
            IMongoCollection<BsonDocument> collection;
            if (ServiceName == "Anomaly")
                collection = _databaseAD.GetCollection<BsonDocument>(collectionName);
            else
                collection = _database.GetCollection<BsonDocument>(collectionName);
            var requests = JsonConvert.SerializeObject(data);
            var insertRequests = BsonSerializer.Deserialize<BsonDocument>(requests);
            //var collection = _database.GetCollection<BsonDocument>(collectionName);
            collection.InsertOne(insertRequests);
        }
        #endregion
        #region Anomaly Detection
        public void UpdateADTrainingQueueStatus()
        {
            try
            {
                var queueCollection = _databaseAD.GetCollection<WINSERVICEMODELS.QueueMonitor>("QueueMonitor");
                var queueFilter = Builders<WINSERVICEMODELS.QueueMonitor>.Filter.Where(x => x.QueueName == "ADTrainingQueue");
                var prj = Builders<WINSERVICEMODELS.QueueMonitor>.Projection.Exclude("_id");
                var collectionResult = queueCollection.Find(queueFilter).Project<WINSERVICEMODELS.QueueMonitor>(prj).FirstOrDefault();
                if (collectionResult == null)
                {
                    WINSERVICEMODELS.QueueMonitor queueMonitor = new WINSERVICEMODELS.QueueMonitor();
                    queueMonitor.QueueName = "ADTrainingQueue";
                    queueMonitor.CreatedOn = DateTime.UtcNow.ToString();
                    queueMonitor.CreatedBy = "SYSTEM";
                    queueMonitor.ModifiedOn = DateTime.UtcNow.ToString();
                    queueMonitor.ModifiedBy = "SYSTEM";
                    queueMonitor.TotalQueueLimit = appSettings.AITrainingQueueLimit;
                    queueCollection.InsertOne(queueMonitor);
                }
                int counter = 0;
                string status = string.Empty;

                var requestCollection = _databaseAD.GetCollection<IngrainRequestQueue>("SSAI_IngrainRequests");
                var filterBuilder = Builders<IngrainRequestQueue>.Filter;
                var Filter = filterBuilder.Eq("RequestStatus", appSettings.requestInProgress) | filterBuilder.Eq("RequestStatus", "Occupied");
                var projection = Builders<IngrainRequestQueue>.Projection.Exclude("_id");
                var requestQueue = requestCollection.Find(Filter).Project<IngrainRequestQueue>(projection).ToList();
                if (requestQueue.Count > 0)
                {
                    counter += requestQueue.Count;
                }

                if (counter < Convert.ToInt32(appSettings.AITrainingQueueLimit))
                {
                    status = "Available";
                }
                else
                {
                    status = "Occupied";
                }

                var updateBuilder = Builders<WINSERVICEMODELS.QueueMonitor>.Update.Set(x => x.CurrentInprogressCount, counter.ToString())
                                                                                  .Set(x => x.ModifiedOn, DateTime.UtcNow.ToString())
                                                                                  .Set(x => x.QueueStatus, status);
                queueCollection.UpdateOne(queueFilter, updateBuilder);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(QueueManagerService), nameof(UpdateADTrainingQueueStatus), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
        }


        public string GetADTrainingQueueStatus()
        {
            string status = string.Empty;
            var queueCollection = _databaseAD.GetCollection<WINSERVICEMODELS.QueueMonitor>("QueueMonitor");
            var filter = Builders<WINSERVICEMODELS.QueueMonitor>.Filter.Where(x => x.QueueName == "ADTrainingQueue");
            var projection = Builders<WINSERVICEMODELS.QueueMonitor>.Projection.Exclude("_id");
            var collectionResult = queueCollection.Find(filter).Project<WINSERVICEMODELS.QueueMonitor>(projection).FirstOrDefault();
            if (collectionResult != null)
            {
                status = collectionResult.QueueStatus;
            }
            else
            {
                status = "Available";
            }
            return status;
        }

        #endregion Anomaly Detection
    }
}
