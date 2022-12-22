using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
using Accenture.MyWizard.Ingrain.DataAccess;
using Accenture.MyWizard.Ingrain.DataModels.AICore.UseCase;
using Accenture.MyWizard.Ingrain.DataModels.Models;
using Accenture.MyWizard.Shared.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using Ninject;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
using System.IdentityModel.Tokens.Jwt;

namespace Accenture.MyWizard.Ingrain.BusinessDomain.Common
{
    public static class CommonUtility
    {
        private static MongoClient _mongoClient;
        private static IMongoDatabase _database;
        private static IngrainAppSettings appSettings;
        private static MongoClient _mongoClientAD;
        private static IMongoDatabase _databaseAD;
        #region Constructors
        /// <summary>
        /// Constructor to Initialize mongoDB Connection
        /// </summary>
        //static CommonUtility()
        //{

        //}
        #endregion Constructors
        #region Methods
        public static List<string> GetDataSourceModel(string correlationId, IOptions<IngrainAppSettings> settings, string ServiceName = "")
        {
            appSettings = settings.Value;
            var databaseProvider = new DatabaseProvider(settings);
            _mongoClient = databaseProvider.GetDatabaseConnection();
            var dataBaseName = MongoUrl.Create(settings.Value.connectionString).DatabaseName;
            _database = _mongoClient.GetDatabase(dataBaseName);
            //Anomaly DB connection
            _mongoClientAD = databaseProvider.GetDatabaseConnection("Anomaly");
            var dataBaseNameAD = MongoUrl.Create(settings.Value.AnomalyDetectionCS).DatabaseName;
            _databaseAD = _mongoClientAD.GetDatabase(dataBaseNameAD);

            IMongoCollection<UserColumns> collection;
            IMongoCollection<BsonDocument> pscollection;
            if (ServiceName == "Anomaly")
            {
                collection = _databaseAD.GetCollection<UserColumns>("SSAI_DeployedModels");
                pscollection = _databaseAD.GetCollection<BsonDocument>("PS_BusinessProblem");
            }
            else
            {
                collection = _database.GetCollection<UserColumns>("SSAI_DeployedModels");
                pscollection = _database.GetCollection<BsonDocument>("PS_BusinessProblem");
            }
            List<string> dataSource = new List<string>();
            //var collection = _database.GetCollection<UserColumns>("SSAI_DeployedModels");
            var filter = Builders<UserColumns>.Filter.Eq("CorrelationId", correlationId);
            var projection = Builders<UserColumns>.Projection.Include("ModelName").Include("DataSource").Include("InstaId").Include("Category").Exclude("_id");
            var data = collection.Find(filter).Project<UserColumns>(projection).ToList();
            if (data.Count > 0)
            {
                dataSource.Add(data[0].ModelName);
                dataSource.Add(data[0].DataSource);
            }
            //var pscollection = _database.GetCollection<BsonDocument>("PS_BusinessProblem");
            var psFilter = Builders<BsonDocument>.Filter.Eq("CorrelationId", correlationId);
            var psProjection = Builders<BsonDocument>.Projection.Include("BusinessProblems").Include("ProblemType").Exclude("_id");
            var psData = pscollection.Find(psFilter).Project<BsonDocument>(psProjection).ToList();
            if (psData.Count > 0)
            {
                dataSource.Add(psData[0]["ProblemType"].ToString());
                dataSource.Add(psData[0]["BusinessProblems"].ToString());
            }
            if (data.Count > 0)
            {
                dataSource.Add(data[0].InstaId);
                dataSource.Add(data[0].Category);
            }
            return dataSource;
        }

        public static List<string> GetDataSourceModelDetails(string correlationId, IOptions<IngrainAppSettings> settings, string ServiceName = "")
        {
            appSettings = settings.Value;
            var databaseProvider = new DatabaseProvider(settings);
            _mongoClient = databaseProvider.GetDatabaseConnection();
            var dataBaseName = MongoUrl.Create(settings.Value.connectionString).DatabaseName;
            _database = _mongoClient.GetDatabase(dataBaseName);
            //Anomaly DB connection
            _mongoClientAD = databaseProvider.GetDatabaseConnection("Anomaly");
            var dataBaseNameAD = MongoUrl.Create(settings.Value.AnomalyDetectionCS).DatabaseName;
            _databaseAD = _mongoClientAD.GetDatabase(dataBaseNameAD);

            IMongoCollection<UserColumns> collection;
            if (ServiceName == "Anomaly")
                collection = _databaseAD.GetCollection<UserColumns>("SSAI_DeployedModels");
            else
                collection = _database.GetCollection<UserColumns>("SSAI_DeployedModels");
            List<string> dataSource = new List<string>();
            //var collection = _database.GetCollection<UserColumns>("SSAI_DeployedModels");
            var filter = Builders<UserColumns>.Filter.Eq("CorrelationId", correlationId);
            var projection = Builders<UserColumns>.Projection.Include("ModelName").Include("DataSource").Include("InstaId").Include("Category").Exclude("_id");
            var data = collection.Find(filter).Project<UserColumns>(projection).ToList();
            if (data.Count > 0)
            {
                dataSource.Add(data[0].ModelName);
                dataSource.Add(data[0].DataSource);
                dataSource.Add(data[0].InstaId);
                dataSource.Add(data[0].Category);
            }
            return dataSource;
        }

        public static CommonAttributes GetCommonAttributes(string correlationId, IOptions<IngrainAppSettings> settings, string ServiceName = "")
        {
            CommonAttributes commonAttributes = new CommonAttributes();
            appSettings = settings.Value;
            var databaseProvider = new DatabaseProvider(settings);
            _mongoClient = databaseProvider.GetDatabaseConnection();
            var dataBaseName = MongoUrl.Create(settings.Value.connectionString).DatabaseName;
            _database = _mongoClient.GetDatabase(dataBaseName);
            //Anomaly DB connection
            _mongoClientAD = databaseProvider.GetDatabaseConnection("Anomaly");
            var dataBaseNameAD = MongoUrl.Create(settings.Value.AnomalyDetectionCS).DatabaseName;
            _databaseAD = _mongoClientAD.GetDatabase(dataBaseNameAD);

            List<string> dataSource = new List<string>();
            //var collection = _database.GetCollection<UserColumns>("SSAI_DeployedModels");
            IMongoCollection<UserColumns> collection;
            IMongoCollection<BsonDocument> pscollection;
            if (ServiceName == "Anomaly")
            {
                collection = _databaseAD.GetCollection<UserColumns>("SSAI_DeployedModels");
                pscollection = _databaseAD.GetCollection<BsonDocument>("PS_BusinessProblem");
            }
            else
            {
                collection = _database.GetCollection<UserColumns>("SSAI_DeployedModels");
                pscollection = _database.GetCollection<BsonDocument>("PS_BusinessProblem");
            }
            var filter = Builders<UserColumns>.Filter.Eq("CorrelationId", correlationId);
            var projection = Builders<UserColumns>.Projection.Include("ModelName").Include("DataSource").Include("InstaId").Include("Category").Exclude("_id");
            var data = collection.Find(filter).Project<UserColumns>(projection).ToList();
            if (data.Count > 0)
            {
                commonAttributes.ModelName = data[0].ModelName;
                commonAttributes.DataSource = data[0].DataSource;
            }
            //var pscollection = _database.GetCollection<BsonDocument>("PS_BusinessProblem");
            var psFilter = Builders<BsonDocument>.Filter.Eq("CorrelationId", correlationId);
            var psProjection = Builders<BsonDocument>.Projection.Include("BusinessProblems").Include("ProblemType").Exclude("_id");
            var psData = pscollection.Find(psFilter).Project<BsonDocument>(psProjection).ToList();
            if (psData.Count > 0)
            {
                commonAttributes.ProblemType = psData[0]["ProblemType"].ToString();
                commonAttributes.BusinessProblems = psData[0]["BusinessProblems"].ToString();
            }
            if (data.Count > 0)
            {
                commonAttributes.InstaId = data[0].InstaId;
                commonAttributes.Category = data[0].Category;
            }
            return commonAttributes;
        }

        /// <summary>
        /// Insert into Ingrain Request Queue
        /// </summary>
        /// <param name="correlationId">CorrelationId</param>
        /// <param name="userId">UserId</param>
        /// <param name="pageInfo">PageInfo</param>
        /// <param name="function">Function<param>
        /// <returns>IngrainRequestQueue</returns>
        public static IngrainRequestQueue InsertIngrainRequest(string correlationId, string userId, string pageInfo, string function)
        {
            IngrainRequestQueue ingrainRequest = new IngrainRequestQueue
            {
                _id = Guid.NewGuid().ToString(),
                CorrelationId = correlationId,
                RequestId = Guid.NewGuid().ToString(),
                ProcessId = null,
                Status = null,
                ModelName = null,
                RequestStatus = CONSTANTS.New,
                RetryCount = 0,
                ProblemType = null,
                Message = null,
                UniId = null,
                Progress = null,
                pageInfo = pageInfo,//CONSTANTS.DataCleanUp,
                ParamArgs = CONSTANTS.CurlyBraces,
                Function = function,//CONSTANTS.DataCleanUp,
                CreatedByUser = userId,
                CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                ModifiedByUser = userId,
                ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                LastProcessedOn = null,
            };
            return ingrainRequest;
        }

        public static bool EncryptDB(string correlationid, IOptions<IngrainAppSettings> settings, string ServiceName = "")
        {
            var databaseProvider = new DatabaseProvider(settings);
            _mongoClient = databaseProvider.GetDatabaseConnection();
            var dataBaseName = MongoUrl.Create(settings.Value.connectionString).DatabaseName;
            _database = _mongoClient.GetDatabase(dataBaseName);
            // var collection = _database.GetCollection<BsonDocument>("SSAI_DeployedModels");
            //Anomaly DB connection
            _mongoClientAD = databaseProvider.GetDatabaseConnection("Anomaly");
            var dataBaseNameAD = MongoUrl.Create(settings.Value.AnomalyDetectionCS).DatabaseName;
            _databaseAD = _mongoClientAD.GetDatabase(dataBaseNameAD);

            IMongoCollection<BsonDocument> collection;
            if (ServiceName == "Anomaly")
                collection = _databaseAD.GetCollection<BsonDocument>("SSAI_DeployedModels");
            else
                collection = _database.GetCollection<BsonDocument>("SSAI_DeployedModels");
            var filter = Builders<BsonDocument>.Filter.Eq("CorrelationId", correlationid);
            var projection = Builders<BsonDocument>.Projection.Include("DBEncryptionRequired").Include("CorrelationId").Exclude("_id");
            var data = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (data.Count > 0)
            {
                BsonElement element;
                var exists = data[0].TryGetElement("DBEncryptionRequired", out element);
                if (exists)
                    return (bool)data[0]["DBEncryptionRequired"];
                else
                    return false;
            }
            else
            {
                return false;
            }
        }

        public static bool DBEncrypt_Clustering(string correlationid, IOptions<IngrainAppSettings> settings)
        {
            var databaseProvider = new DatabaseProvider(settings);
            _mongoClient = databaseProvider.GetDatabaseConnection();
            var dataBaseName = MongoUrl.Create(settings.Value.connectionString).DatabaseName;
            _database = _mongoClient.GetDatabase(dataBaseName);
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.Clustering_IngestData);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationid);
            var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.DBEncryptionRequired).Include(CONSTANTS.CorrelationId).Exclude("_id");
            var data = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (data.Count > 0)
            {
                BsonElement element;
                var exists = data[0].TryGetElement(CONSTANTS.DBEncryptionRequired, out element);
                if (exists)
                    return (bool)data[0][CONSTANTS.DBEncryptionRequired];
                else
                    return false;
            }
            else
            {
                return false;
            }
        }
        public static void SendAppNotification(AppNotificationLog appNotificationLog, bool isCascadeModel, string ServiceName = "")
        {
            IMongoCollection<AppIntegration> appIntegrationCollection;
            IMongoCollection<AppNotificationLog> notificationCollection;
            if (ServiceName == "Anomaly")
            {
                appIntegrationCollection = _databaseAD.GetCollection<AppIntegration>(CONSTANTS.AppIntegration);
                notificationCollection = _databaseAD.GetCollection<AppNotificationLog>("AppNotificationLog");
            }
            else
            {
                appIntegrationCollection = _database.GetCollection<AppIntegration>(CONSTANTS.AppIntegration);
                notificationCollection = _database.GetCollection<AppNotificationLog>("AppNotificationLog");
            }
            appNotificationLog.RequestId = Guid.NewGuid().ToString();
            appNotificationLog.CreatedOn = DateTime.UtcNow.ToString();
            appNotificationLog.RetryCount = 0;
            appNotificationLog.IsNotified = false;

            if (appNotificationLog.ProblemType == "Multi_Class")
            {
                appNotificationLog.ProblemType = "Classification";
            }

            // var appIntegrationCollection = _database.GetCollection<AppIntegration>(CONSTANTS.AppIntegration);
            var filter = Builders<AppIntegration>.Filter.Where(x => x.ApplicationID == appNotificationLog.ApplicationId);
            var app = appIntegrationCollection.Find(filter).FirstOrDefault();

            if (app != null)
            {
                appNotificationLog.Environment = app.Environment;
                if (app.Environment == "PAD")
                {
                    Uri apiUri = new Uri(appSettings.myWizardAPIUrl);
                    string host = apiUri.GetLeftPart(UriPartial.Authority);
                    if (isCascadeModel)
                        appNotificationLog.AppNotificationUrl = appSettings.PerformCascadeOperationsInVDS;

                    else
                        appNotificationLog.AppNotificationUrl = host + "/" + app.AppNotificationUrl;
                }
                else
                {
                    if (isCascadeModel)
                    {
                        Uri apiUri = new Uri(appSettings.myWizardAPIUrl);
                        string host = apiUri.GetLeftPart(UriPartial.Authority);
                        //appNotificationLog.AppNotificationUrl = host + "/" + appSettings.PerformCascadeOperationsInVDS;
                        appNotificationLog.AppNotificationUrl = appSettings.PerformCascadeOperationsInVDSFDS;
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(CommonUtility), nameof(SendAppNotification), "SendAppNotification - end :", default(Guid), "AppNotificationUrl:" + appNotificationLog.AppNotificationUrl, "PerformCascadeOperationsInVDS:" + appSettings.PerformCascadeOperationsInVDS, "host:" + host, "myWizardAPIUrl:" + appSettings.myWizardAPIUrl);
                    }
                    else
                        appNotificationLog.AppNotificationUrl = app.AppNotificationUrl;
                }
            }
            else
            {
                throw new KeyNotFoundException("ApplicationId not found");
            }


            // var notificationCollection = _database.GetCollection<AppNotificationLog>("AppNotificationLog");
            notificationCollection.InsertOne(appNotificationLog);
        }
        public static void AuditTrailLog(CallBackErrorLog auditTrailLog, IOptions<IngrainAppSettings> settings)
        {
            // AuditTrailLog callBackErrorLog = new AuditTrailLog();
            string result = string.Empty;
            string isNoficationSent = "false";
            var databaseProvider = new DatabaseProvider(settings);
            _mongoClient = databaseProvider.GetDatabaseConnection();
            var dataBaseName = MongoUrl.Create(settings.Value.connectionString).DatabaseName;
            _database = _mongoClient.GetDatabase(dataBaseName);

            if (!string.IsNullOrEmpty(auditTrailLog.CorrelationId))
            {
                //Team level Predicton
                var ingrainRequest = GetIngrainRequestDetails(auditTrailLog.CorrelationId, auditTrailLog.ApplicationID, auditTrailLog.UseCaseId);
                if (ingrainRequest != null)
                {
                    if (string.IsNullOrEmpty(auditTrailLog.ClientId))
                    {
                        auditTrailLog.ClientId = ingrainRequest.ClientId;
                    }
                    if (string.IsNullOrEmpty(auditTrailLog.DCID))
                    {
                        auditTrailLog.DCID = ingrainRequest.DCID;
                    }
                    if (string.IsNullOrEmpty(auditTrailLog.ApplicationID))
                    {
                        auditTrailLog.ApplicationID = ingrainRequest.AppID;
                    }
                    if (string.IsNullOrEmpty(auditTrailLog.UseCaseId))
                    {
                        auditTrailLog.UseCaseId = ingrainRequest.TemplateUseCaseID;
                    }
                    if (string.IsNullOrEmpty(auditTrailLog.RequestId))
                    {
                        auditTrailLog.RequestId = ingrainRequest.RequestId;
                    }
                    if (string.IsNullOrEmpty(auditTrailLog.CreatedBy))
                    {
                        auditTrailLog.CreatedBy = ingrainRequest.CreatedByUser;
                    }
                }
            }

            var AppCollection = _database.GetCollection<AppIntegration>(CONSTANTS.AppIntegration);
            var filterBuilder1 = Builders<AppIntegration>.Filter;
            var AppFilter1 = filterBuilder1.Eq("ApplicationName", auditTrailLog.ApplicationName);

            var Projection2 = Builders<AppIntegration>.Projection.Include("ApplicationID").Exclude("_id");
            var AppData1 = AppCollection.Find(AppFilter1).Project<AppIntegration>(Projection2).FirstOrDefault();
            if (AppData1 != null)
            {
                if (string.IsNullOrEmpty(auditTrailLog.ApplicationID))
                {
                    auditTrailLog.ApplicationID = AppData1.ApplicationID;
                }
            }
            if (auditTrailLog.UseCaseId != null && auditTrailLog.ApplicationID != null)
            {
                var MappingCollection = _database.GetCollection<PublicTemplateMapping>(CONSTANTS.PublicTemplateMapping);
                var Builder = Builders<PublicTemplateMapping>.Filter;
                var Projection1 = Builders<PublicTemplateMapping>.Projection.Exclude("_id");
                var filter = Builder.Eq(CONSTANTS.ApplicationID, auditTrailLog.ApplicationID) & Builder.Eq(CONSTANTS.UsecaseID, auditTrailLog.UseCaseId);
                var templatedata = MappingCollection.Find(filter).Project<PublicTemplateMapping>(Projection1).FirstOrDefault();
                if (templatedata != null)
                {
                    if (string.IsNullOrEmpty(auditTrailLog.FeatureName))
                    {
                        auditTrailLog.FeatureName = templatedata.FeatureName;
                    }
                    if (string.IsNullOrEmpty(auditTrailLog.ApplicationName))
                    {
                        auditTrailLog.ApplicationName = templatedata.ApplicationName;
                    }
                }
            }
            if (auditTrailLog.ApplicationID != null)
            {
                var AppIntegCollection = _database.GetCollection<AppIntegration>(CONSTANTS.AppIntegration);
                var filterBuilder = Builders<AppIntegration>.Filter;
                var AppFilter = filterBuilder.Eq("ApplicationID", auditTrailLog.ApplicationID);

                var Projection = Builders<AppIntegration>.Projection.Include("Environment").Include("ApplicationName").Exclude("_id");
                var AppData = AppIntegCollection.Find(AppFilter).Project<AppIntegration>(Projection).FirstOrDefault();
                if (AppData != null)
                {
                    if (string.IsNullOrEmpty(auditTrailLog.Environment))
                    {
                        auditTrailLog.Environment = AppData.Environment;
                    }
                    if (string.IsNullOrEmpty(auditTrailLog.ApplicationName))
                    {
                        auditTrailLog.ApplicationName = AppData.ApplicationName;
                    }
                }
            }
            if (auditTrailLog.UseCaseId != null)
            {
                var aiusecaseDetails = AIUsecaseDetails(auditTrailLog.UseCaseId);
                if (string.IsNullOrEmpty(auditTrailLog.ApplicationName))
                {
                    auditTrailLog.ApplicationName = aiusecaseDetails.ApplicationName;
                }
                if (string.IsNullOrEmpty(auditTrailLog.ApplicationID))
                {
                    auditTrailLog.ApplicationID = aiusecaseDetails.ApplicationId;
                }
                auditTrailLog.FeatureName = auditTrailLog.ApplicationName + "-" + auditTrailLog.FeatureName;
            }
            if (auditTrailLog.httpResponse != null)
            {
                if (auditTrailLog.httpResponse.IsSuccessStatusCode)
                {
                    isNoficationSent = "true";
                    result = auditTrailLog.httpResponse.StatusCode + " - Content: " + auditTrailLog.httpResponse.Content.ReadAsStringAsync().Result;
                }
                else
                {
                    result = auditTrailLog.httpResponse.StatusCode + "-" + auditTrailLog.httpResponse.ReasonPhrase.ToString() + "- Content: " + auditTrailLog.httpResponse.Content.ReadAsStringAsync().Result;
                }
            }
            if (auditTrailLog.Status == CONSTANTS.ErrorMessage && auditTrailLog.ApplicationName == CONSTANTS.SPAAPP)
            {
                auditTrailLog.ErrorMessage = auditTrailLog.ErrorMessage + " -" + "Record will be deleted from IngrainRequest for CorrelationId : " + auditTrailLog.CorrelationId;
            }
            auditTrailLog.CallbackURLResponse = result;
            auditTrailLog.CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
            auditTrailLog.ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
            var logCollection = _database.GetCollection<CallBackErrorLog>(CONSTANTS.AuditTrailLog);
            logCollection.InsertOneAsync(auditTrailLog);
        }

        private static IngrainRequestQueue GetIngrainRequestDetails(string correlationId, string applicationId, string usecaseId)
        {
            IngrainRequestQueue trainingRequest = new IngrainRequestQueue();
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
            var filterBuilder = Builders<BsonDocument>.Filter;
            FilterDefinition<BsonDocument> filterQueue = null;
            if (string.IsNullOrEmpty(applicationId) || string.IsNullOrEmpty(usecaseId))
            {
                filterQueue = filterBuilder.Eq(CONSTANTS.CorrelationId, correlationId) & filterBuilder.Eq("Function", "AutoTrain");
            }
            else
            {
                filterQueue = filterBuilder.Eq(CONSTANTS.CorrelationId, correlationId) & filterBuilder.Eq(CONSTANTS.TemplateUseCaseID, usecaseId) & filterBuilder.Eq(CONSTANTS.AppID, applicationId) & filterBuilder.Eq("Function", "AutoTrain");

            }
            var Projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
            var result = collection.Find(filterQueue).Project<BsonDocument>(Projection).ToList();
            if (result.Count > 0)
            {
                trainingRequest.ClientId = result[0]["ClientId"].ToString();
                trainingRequest.DeliveryconstructId = result[0]["DeliveryconstructId"].ToString();
                if (result[0].Contains("TeamAreaUId"))
                {
                    trainingRequest.TeamAreaUId = result[0]["TeamAreaUId"].ToString();
                }
                trainingRequest.AppID = result[0]["AppID"].ToString();
                trainingRequest.TemplateUseCaseID = result[0]["TemplateUseCaseID"].ToString();
                trainingRequest.RequestId = result[0]["RequestId"].ToString();
                trainingRequest.CreatedByUser = result[0]["CreatedByUser"].ToString();


            }
            return trainingRequest;
        }

        private static UsecaseDetails AIUsecaseDetails(string usecaseId)
        {
            UsecaseDetails useCaseList = new UsecaseDetails();
            try
            {
                var useCaseCollection = _database.GetCollection<BsonDocument>("AISavedUsecases");
                var filter = Builders<BsonDocument>.Filter.Eq("UsecaseId", usecaseId);
                var projection = Builders<BsonDocument>.Projection.Exclude("_id").Include("ApplicationName").Include("ApplicationId");
                var result = useCaseCollection.Find(filter).Project<BsonDocument>(projection).ToList();
                if (result.Count > 0)
                {
                    if (result[0].Contains("ApplicationName"))
                    {
                        useCaseList.ApplicationName = result[0]["ApplicationName"].ToString();
                    }
                    if (result[0].Contains("ApplicationId"))
                    {
                        useCaseList.ApplicationId = result[0]["ApplicationId"].ToString();
                    }
                }
                return useCaseList;
            }
            catch (Exception)
            {
                return useCaseList;
            }
        }

        public static string CallbackResponse(IngrainResponseData CallBackResponse, string AppName, string baseAddress, string clientId, string DCId, string applicationId, string usecaseId, string requestId, string userId, IOptions<IngrainAppSettings> settings, IEncryptionDecryption _encryptionDecryption)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CommonUtility), nameof(CallbackResponse), "CallbackResponse - Started :", string.IsNullOrEmpty(CallBackResponse.CorrelationId) ? default(Guid) : new Guid(CallBackResponse.CorrelationId), applicationId, string.Empty, clientId, DCId);
            string token = CustomUrlToken(AppName, settings, _encryptionDecryption);

            string contentType = "application/json";
            var Request = JsonConvert.SerializeObject(CallBackResponse);
            using (var Client = new HttpClient())
            {
                Client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                Client.DefaultRequestHeaders.Add("AppServiceUId", settings.Value.AppServiceUID);

                Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(contentType));

                HttpContent httpContent = new StringContent(Request, Encoding.UTF8, "application/json");
                HttpResponseMessage httpResponse = Client.PostAsync(baseAddress, httpContent).Result;
                var statuscode = httpResponse.StatusCode;
                //Log to DB AuditTraiLog
                CallBackErrorLog CallBackErrorLog = new CallBackErrorLog();
                CallBackErrorLog.CorrelationId = CallBackResponse.CorrelationId;
                CallBackErrorLog.Message = CallBackResponse.Message;
                CallBackErrorLog.ErrorMessage = CallBackResponse.ErrorMessage;
                CallBackErrorLog.Status = CallBackResponse.Status;
                CallBackErrorLog.ApplicationName = AppName;
                CallBackErrorLog.BaseAddress = baseAddress;
                CallBackErrorLog.httpResponse = httpResponse;
                CallBackErrorLog.ClientId = clientId;
                CallBackErrorLog.DCID = DCId;
                CallBackErrorLog.ApplicationID = applicationId;
                CallBackErrorLog.UseCaseId = usecaseId;
                CallBackErrorLog.RequestId = requestId;
                CallBackErrorLog.CreatedBy = userId;
                CallBackErrorLog.ProcessName = "Training";
                if (CallBackResponse.Status == CONSTANTS.ErrorMessage || CallBackResponse.Status == CONSTANTS.E)
                {
                    CallBackErrorLog.UsageType = "ERROR";
                }
                else
                {
                    CallBackErrorLog.UsageType = "INFO";
                }
                AuditTrailLog(CallBackErrorLog, settings);

                if (httpResponse.IsSuccessStatusCode)
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(CommonUtility), nameof(CallbackResponse), "CallbackResponse - SUCCESS END :" + httpResponse.Content, string.IsNullOrEmpty(CallBackResponse.CorrelationId) ? default(Guid) : new Guid(CallBackResponse.CorrelationId), applicationId, string.Empty, clientId, DCId);
                    return CONSTANTS.success;
                }
                else
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(CommonUtility), nameof(CallbackResponse), "CallbackResponse - ERROR END :" + statuscode + "--" + httpResponse.Content, applicationId, string.Empty, clientId, DCId);
                    return CONSTANTS.Error;
                }
            }
        }

        private static string CustomUrlToken(string ApplicationName, IOptions<IngrainAppSettings> settings, IEncryptionDecryption _encryptionDecryption)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CommonUtility), nameof(CustomUrlToken), "CustomUrlToken for Application--" + ApplicationName, string.Empty, string.Empty, string.Empty, string.Empty);
            var AppIntegCollection = _database.GetCollection<AppIntegration>(CONSTANTS.AppIntegration);
            var filterBuilder = Builders<AppIntegration>.Filter;
            var AppFilter = filterBuilder.Eq("ApplicationName", ApplicationName);

            var Projection = Builders<AppIntegration>.Projection.Include("BaseURL").Include("Authentication").Include("TokenGenerationURL").Include("Credentials").Exclude("_id");
            var AppData = AppIntegCollection.Find(AppFilter).Project<AppIntegration>(Projection).FirstOrDefault();

            dynamic token = string.Empty;

            if (AppData.Authentication == "AzureAD" || AppData.Authentication == "Azure")
            {
                string GrantType = string.Empty, ClientSecert = string.Empty, ClientId = string.Empty, Resource = string.Empty;
                AppData.TokenGenerationURL = _encryptionDecryption.Decrypt(AppData.TokenGenerationURL.ToString());
                AppData.Credentials = BsonDocument.Parse(_encryptionDecryption.Decrypt(AppData.Credentials));

                GrantType = AppData.Credentials.GetValue("grant_type").AsString;
                ClientSecert = AppData.Credentials.GetValue("client_secret").AsString;
                ClientId = AppData.Credentials.GetValue("client_id").AsString;
                Resource = AppData.Credentials.GetValue("resource").AsString;

                var client = new RestClient(AppData.TokenGenerationURL.ToString().Trim());
                var request = new RestRequest(Method.POST);
                request.AddHeader("cache-control", "no-cache");
                request.AddHeader("content-type", "application/x-www-form-urlencoded");
                request.AddParameter("application/x-www-form-urlencoded", "grant_type=" + GrantType +
                "&client_id=" + ClientId +
                "&client_secret=" + ClientSecert +
                "&resource=" + Resource,
                ParameterType.RequestBody);
                var requestBuilder = new StringBuilder();
                foreach (var param in request.Parameters)
                {
                    requestBuilder.AppendFormat("{0}: {1}\r\n", param.Name, param.Value);
                }
                requestBuilder.ToString();
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(CommonUtility), nameof(CustomUrlToken), "Application TOKEN PARAMS -- " + requestBuilder, string.Empty, string.Empty, string.Empty, string.Empty);
                IRestResponse response1 = client.Execute(request);
                string json1 = response1.Content;
                // Retrieve and Return the Access Token                
                var tokenObj = JsonConvert.DeserializeObject(json1) as dynamic;
                token = Convert.ToString(tokenObj.access_token);
                return token;
            }
            else if (AppData.Authentication == "FORM")
            {
                using (var httpClient = new HttpClient())
                {
                    //httpClient.BaseAddress = new Uri(settings.Value.tokenAPIUrl);
                    //httpClient.DefaultRequestHeaders.Accept.Clear();
                    //httpClient.DefaultRequestHeaders.Add("UserName", settings.Value.username);
                    //httpClient.DefaultRequestHeaders.Add("Password", settings.Value.password);
                    //HttpContent content = new StringContent(string.Empty);
                    //System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                    //var result = httpClient.PostAsync("", content).Result;

                    //if (result.StatusCode != System.Net.HttpStatusCode.OK)
                    //{
                    //    throw new Exception(string.Format("Unable to process your request for generating token. Please check your credentials and try again. Status Code: {0}", result.StatusCode));
                    //}

                    //var result1 = result.Content.ReadAsStringAsync().Result;
                    //var tokenObj = JsonConvert.DeserializeObject(result1) as dynamic;

                    //token = Convert.ToString(tokenObj.access_token);

                    //PAM
                    httpClient.BaseAddress = new Uri(appSettings.tokenAPIUrl);
                    httpClient.DefaultRequestHeaders.Accept.Clear();
                    var bodyparams = new
                    {
                        username = appSettings.UserNamePAM,
                        password = appSettings.PasswordPAM
                    };
                    string json = JsonConvert.SerializeObject(bodyparams);
                    HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
                    System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                    var result = httpClient.PostAsync("", content).Result;

                    if (result.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        throw new Exception(string.Format("Unable to process your request for generating token. Please check your credentials and try again. Status Code: {0}", result.StatusCode));
                    }

                    var result1 = result.Content.ReadAsStringAsync().Result;
                    var tokenObj = JsonConvert.DeserializeObject(result1) as dynamic;

                    token = Convert.ToString(tokenObj.access_token);

                }
            }
            return token;
        }

        public static ValidRecordsDetailsModel GetModelDataPointdDetails(string correlationId, IOptions<IngrainAppSettings> settings, string ServiceName = "")
        {
            ValidRecordsDetailsModel recordsDetails = new ValidRecordsDetailsModel();
            var databaseProvider = new DatabaseProvider(settings);
            _mongoClient = databaseProvider.GetDatabaseConnection();
            var dataBaseName = MongoUrl.Create(settings.Value.connectionString).DatabaseName;
            _database = _mongoClient.GetDatabase(dataBaseName);
            //Anomaly DB connection
            _mongoClientAD = databaseProvider.GetDatabaseConnection("Anomaly");
            var dataBaseNameAD = MongoUrl.Create(settings.Value.AnomalyDetectionCS).DatabaseName;
            _databaseAD = _mongoClientAD.GetDatabase(dataBaseNameAD);

            IMongoCollection<BsonDocument> useCaseCollection;
            if (ServiceName == "Anomaly")
                useCaseCollection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.PSUseCaseDefinition);
            else
                useCaseCollection = _database.GetCollection<BsonDocument>(CONSTANTS.PSUseCaseDefinition);

            //var useCaseCollection = _database.GetCollection<BsonDocument>(CONSTANTS.PSUseCaseDefinition);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id).Include(CONSTANTS.ValidRecordsDetails);
            var result = useCaseCollection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                recordsDetails = JsonConvert.DeserializeObject<ValidRecordsDetailsModel>(result[0].ToString());
            }
            return recordsDetails;
        }
        public static ValidRecordsDetailsModel GetModelDataPointdDetails(string correlationId, IMongoDatabase mongoDatabase)
        {
            ValidRecordsDetailsModel recordsDetails = new ValidRecordsDetailsModel();
            var useCaseCollection = mongoDatabase.GetCollection<BsonDocument>(CONSTANTS.PSUseCaseDefinition);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id).Include(CONSTANTS.ValidRecordsDetails);
            var result = useCaseCollection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                recordsDetails = JsonConvert.DeserializeObject<ValidRecordsDetailsModel>(result[0].ToString());
            }
            return recordsDetails;
        }

        /// <summary>
        /// Terminate python process by id
        /// </summary>
        /// <param name="pythonProcessId">The python process identifier</param>
        public static void TerminatePythonProcess(int pythonProcessId)
        {
            if (pythonProcessId > 0)
            {
                Process processes = Process.GetProcessById(pythonProcessId);
                processes.Kill();
            }
        }

        /// <summary>
        /// To validate the file uploaded.
        /// </summary>
        /// <param name="fileCollection">The filecollection</param>
        /// <returns>true if Invalid else false</returns>
        public static bool ValidateFileUploaded(IFormFileCollection fileCollection)
        {
            foreach (var i in fileCollection)
            {
                string[] split = i.FileName.Split(new[] { '.' }, 2);
                if (!CONSTANTS.ValidFileExtensions.Any(x => x.Contains("." + split[1])))
                {
                    return true;
                }
                else
                {
                    if (!CONSTANTS.ValidFileContentType.Contains(i.ContentType))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileCollection">The file collection</param>
        /// <returns>true if Invalid else false</returns>
        public static bool ValidateMyDataSourceFileUploaded(IFormFileCollection fileCollection)
        {
            foreach (var i in fileCollection)
            {
                string[] split = i.FileName.Split(new[] { '.' }, 4);
                if (!CONSTANTS.ValidFileExtensions.Any(x => x.Contains("." + split[1])) || !split[2].Contains(CONSTANTS.MyDataSourceFilePattern))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// To validate the user id
        /// </summary>
        /// <param name="userId">The userId</param>
        /// <returns>true if valid else false</returns>
        public static bool GetValidUser(string userId)
        {
            //if (!string.IsNullOrEmpty(userId))
            //{
            //    string pattern = @"^(([\w-]+\.)+[\w-]+|([a-zA-Z]{1}|[\w-]{2,}))@(([a-zA-Z]+[\w-]+\.)+(?:com|COM))$";
            //    //@"^(([\w-]+\.)+[\w-]+|([a-zA-Z]{1}|[\w-]{2,}))@(([a-zA-Z]+[\w-]+\.)+[a-zA-Z]{2,4})$";//for all extension
            //    if (Regex.IsMatch(userId.Trim(), pattern) || userId.ToLower().Trim() == CONSTANTS.SystemUserId || userId.ToLower().Trim() == CONSTANTS.VDSSystemUserId)
            //    {
            //        return true;
            //    }
            //    else
            //        return false;
            //}
            //else
                return true;
        }

        public static bool IsValidGuid(string guid)
        {
            if (!string.IsNullOrEmpty(guid))
            {
                Guid x;
                return Guid.TryParse(guid.Trim(), out x);
            }

            return true;
        }

        /// <summary>
        /// To check browser name is valid
        /// </summary>
        /// <param name="browser">The browser</param>
        /// <returns>Returns true if valid else false</returns>
        public static bool IsBrowserValid(string browser)
        {
            if (!string.IsNullOrEmpty(browser))
                return CONSTANTS.Browsers.Any(x => x.Contains(browser));

            return true;
        }

        public static bool IsDataValid(string source)
        {
            if (!string.IsNullOrWhiteSpace(source))
            {
                //The following will match any matching set of tags.i.e. <b> this </b>
                //Regex tagRegex = new Regex(@"<\s*([^ >]+)[^>]*>.*?<\s*/\s*\1\s*>");

                //The following will match any single tag. i.e. <b> (it doesn't have to be closed).
                Regex tagRegex = new Regex(@"<[^>]+>");

                return !tagRegex.IsMatch(source.Trim());

                /*var htmlEncode = Regex.Replace(source.Trim(), @"</?(?i:script|embed|object|frameset|frame|iframe|meta|link)(.|\n|\s)*?>", string.Empty,
                                                     RegexOptions.Singleline | RegexOptions.IgnoreCase);
                htmlEncode = Regex.Replace(htmlEncode, @"&lt;/?(?i:script|embed|object|frameset|frame|iframe|meta|link)(.|\n|\s)*?&gt;", string.Empty,
                                                             RegexOptions.Singleline | RegexOptions.IgnoreCase);
                htmlEncode = Regex.Replace(htmlEncode, @"</?(?i:script|embed|object|frameset|frame|iframe|meta|link)(.|\n|\s)*?&gt;", string.Empty,
                                                             RegexOptions.Singleline | RegexOptions.IgnoreCase);
                htmlEncode = Regex.Replace(htmlEncode, @"&lt;/?(?i:script|embed|object|frameset|frame|iframe|meta|link)(.|\n|\s)*?>;", string.Empty,
                                                             RegexOptions.Singleline | RegexOptions.IgnoreCase);
                return source.Equals(htmlEncode);*/
            }


            return true;
        }

        public static bool IsDataValidForApp(string source)
        {
            if (!string.IsNullOrWhiteSpace(source))
            {
                //The following will match any matching set of tags.i.e. <b> this </b>
                //Regex tagRegex = new Regex(@"<\s*([^ >]+)[^>]*>.*?<\s*/\s*\1\s*>");

                //The following will match any single tag. i.e. <b> (it doesn't have to be closed).
                //Regex tagRegex = new Regex(@"<[^>]+>");

                //return !tagRegex.IsMatch(source.Trim());

                var htmlEncode = Regex.Replace(source.Trim(), @"</?(?i:script|embed|object|frameset|frame|iframe|meta|link|p)(.|\n|\s)*?>", string.Empty,
                                                     RegexOptions.Singleline | RegexOptions.IgnoreCase);
                htmlEncode = Regex.Replace(htmlEncode, @"&lt;/?(?i:script|embed|object|frameset|frame|iframe|meta|link|p)(.|\n|\s)*?&gt;", string.Empty,
                                                             RegexOptions.Singleline | RegexOptions.IgnoreCase);
                htmlEncode = Regex.Replace(htmlEncode, @"</?(?i:script|embed|object|frameset|frame|iframe|meta|link|p)(.|\n|\s)*?&gt;", string.Empty,
                                                             RegexOptions.Singleline | RegexOptions.IgnoreCase);
                htmlEncode = Regex.Replace(htmlEncode, @"&lt;/?(?i:script|embed|object|frameset|frame|iframe|meta|link|p)(.|\n|\s)*?>;", string.Empty,
                                                             RegexOptions.Singleline | RegexOptions.IgnoreCase);
                return IsDataValid(htmlEncode);
            }


            return true;
        }



        /// <summary>
        /// To validate the request
        /// </summary>
        /// <param name="value">The value</param>
        /// <param name="fieldName">The fieldName</param>
        /// <param name="guidFlag">The guidFlag</param>
        public static void ValidateInputFormData(string value, string fieldName, bool guidFlag)
        {
            if (value != CONSTANTS.undefined && value != CONSTANTS.Null)
            {
                if (guidFlag)
                {
                    if (!IsValidGuid(Convert.ToString(value)))
                    {
                        throw new Exception(string.Format(CONSTANTS.InValidGUID, fieldName));
                    }
                }
                else
                {
                    if (!IsDataValid(Convert.ToString(value)))
                    {
                        throw new Exception(string.Format(CONSTANTS.InValidData, fieldName));
                    }
                }
            }
        }

        public static void ValidateInputFormData(string value, string fieldName, bool guidFlag, string applicationId)
        {
            if (value != CONSTANTS.undefined && value != CONSTANTS.Null)
            {
                if (guidFlag)
                {
                    if (!IsValidGuid(Convert.ToString(value)))
                    {
                        throw new Exception(string.Format(CONSTANTS.InValidGUID, fieldName));
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(applicationId) && CONSTANTS.ApplicationIds.Contains(applicationId))
                    {
                        if (!IsDataValidForApp(Convert.ToString(value)))
                        {
                            throw new Exception(string.Format(CONSTANTS.InValidData, fieldName));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Get the loggedin user from token
        /// </summary>
        /// <param name="authorization">The authorization</param>
        /// <returns>Returns the user name</returns>
        public static string GetLoggedInUserFromToken(string authorization)
        {
            string loggedInUser = "";
            if (!string.IsNullOrEmpty(authorization) && AuthenticationHeaderValue.TryParse(authorization, out var headerValue))
            {
                var scheme = headerValue.Scheme; //scheme will be "Bearer"
                var parameter = headerValue.Parameter;//parmameter will be the token itself.
                var tokenInfo = GetTokenInfo(parameter);
                if (tokenInfo != null)
                {
                    if (tokenInfo.Keys.Contains("unique_name"))
                        loggedInUser = tokenInfo.FirstOrDefault(x => x.Key == "unique_name").Value;

                    else if (tokenInfo.Keys.Contains("preferred_username"))
                        loggedInUser = tokenInfo.FirstOrDefault(x => x.Key == "preferred_username").Value;
                }
            }

            return loggedInUser;
        }

        /// <summary>
        /// Gets the token info
        /// </summary>
        /// <param name="token">The token</param>
        /// <returns>Returns the token information</returns>
        private static Dictionary<string, string> GetTokenInfo(string token)
        {
            var tokenInfo = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(token))
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtSecurityToken = handler.ReadJwtToken(token);
                var claims = jwtSecurityToken.Claims.ToList();

                foreach (var claim in claims)
                {
                    if (!tokenInfo.ContainsKey(claim.Type))
                    {
                        tokenInfo.Add(claim.Type, claim.Value);
                    }
                }
            }

            return tokenInfo;
        }

        #endregion
        #region Decimal precision
        public static decimal GetDecimalValue(string value, int Precision)
        {
            decimal outputValue;
            if (decimal.TryParse(value, out outputValue))
            {
                outputValue = Math.Round(outputValue, Precision);
            }
            return outputValue;
        }
        public static List<object> GetDataAfterDecimalPrecision(BsonArray Data, int Precision, int noOfRecord, bool showAllRecord)
        {
            noOfRecord = Data.Count < noOfRecord ? Data.Count : noOfRecord;
            int reclmt = !showAllRecord ? noOfRecord : Data.Count;
            List<object> FinalData = new List<object>();
            List<string> DecimalColLst = new List<string>();
            BsonArray ResponseArray = new BsonArray();

            if (Data != null && Data.Count != 0)
            {
                for (var i = 0; i < reclmt; i++)
                {
                    BsonDocument result = Data[i].AsBsonDocument;
                    if (i == 0)
                    {
                        foreach (var elements in result.Elements)
                        {
                            if (elements.Value.IsDouble)
                            {
                                DecimalColLst.Add(elements.Name);
                            }
                        }
                    }
                    if (DecimalColLst.Count > 0)
                    {
                        foreach (var element in DecimalColLst)
                        {
                            try
                            {
                                result[element] = CommonUtility.GetDecimalValue_new(Convert.ToString(result[element].AsNullableDouble).Trim(), Precision);
                            }
                            catch
                            {
                                result[element] = CommonUtility.GetDecimalValue_new(Convert.ToString(result[element]).Trim(), Precision);
                            }
                            //result[element] = CommonUtility.GetDecimalValue_new(Convert.ToString(result[element].AsNullableDouble), Precision);
                        }
                    }
                    ResponseArray.Add(result);
                }
            }
            FinalData = JsonConvert.DeserializeObject<List<object>>(JsonConvert.SerializeObject(BsonTypeMapper.MapToDotNetValue(ResponseArray)));
            return FinalData;
        }
        public static BsonArray GetDataAfterDecimalPrecisionAIService(BsonArray Data, int Precision, int noOfRecord, bool showAllRecord, out bool RequiredFlag)
        {
            RequiredFlag = false;
            noOfRecord = Data.Count < noOfRecord ? Data.Count : noOfRecord;
            int reclmt = !showAllRecord ? noOfRecord : Data.Count;
            List<string> DecimalColLst = new List<string>();
            BsonArray ResponseArray = new BsonArray();
            if (Data != null && Data.Count != 0)
            {
                ResponseArray.Add(Data[0]);
                for (var i = 1; i < reclmt; i++)
                {
                    BsonDocument result = Data[i].AsBsonDocument;
                    if (i == 1)
                    {
                        foreach (var elements in result.Elements)
                        {
                            double outputValue;
                            if (elements.Value.IsDouble)
                            {
                                DecimalColLst.Add(elements.Name);
                            }
                            else if (elements.Name == "score")
                            {
                                DecimalColLst.Add(elements.Name);
                            }
                            else if (double.TryParse(elements.Value.ToString(), out outputValue))
                            {
                                DecimalColLst.Add(elements.Name);
                            }
                        }
                    }
                    if (DecimalColLst.Count > 0)
                    {
                        RequiredFlag = true;
                        foreach (var element in DecimalColLst)
                        {
                            if (element == "score")
                                result[element] = CommonUtility.GetDecimalValue_new(Convert.ToString(result[element]).Trim(), Precision);
                            else
                            {
                                try
                                {
                                    result[element] = CommonUtility.GetDecimalValue_new(Convert.ToString(result[element].AsNullableDouble).Trim(), Precision);
                                }
                                catch
                                {
                                    result[element] = CommonUtility.GetDecimalValue_new(Convert.ToString(result[element]).Trim(), Precision);
                                }

                            }
                        }
                    }
                    ResponseArray.Add(result);
                }
            }
            return ResponseArray;
        }
        public static dynamic GetDecimalValue_new(string value, int Precision)
        {
            if (string.IsNullOrEmpty(value))
                return value;
            decimal outputValue;
            if (decimal.TryParse(value.ToString(), out outputValue))
            {
                if (Precision <= 0)
                    outputValue = Math.Round(outputValue, Precision);
                else
                {
                    if (value.IndexOf('.') < 0)
                        outputValue = Convert.ToDecimal(value + "." + Convert.ToString(Math.Pow(10, Precision)).Substring(1));
                    else if (value.Substring(value.IndexOf('.') + 1).Length < Precision)
                    {
                        int appendZeroCount = Precision - value.Substring(value.IndexOf('.') + 1).Length;
                        outputValue = Convert.ToDecimal(value + Convert.ToString(Math.Pow(10, appendZeroCount)).Substring(1));
                    }
                    else
                        outputValue = Math.Round(outputValue, Precision);
                }
                return outputValue;
            }
            else
            {
                string ResValue = value;
                if (Precision <= 0)
                    ResValue = value;
                else
                {
                    if (value.IndexOf('.') < 0)
                        ResValue = value + "." + Convert.ToString(Math.Pow(10, Precision)).Substring(1);
                    else if (value.Substring(value.IndexOf('.') + 1).Length < Precision)
                    {
                        int appendZeroCount = Precision - value.Substring(value.IndexOf('.') + 1).Length;
                        ResValue = value + Convert.ToString(Math.Pow(10, appendZeroCount)).Substring(1);
                    }
                    else
                    {
                        ResValue = value.Remove(value.IndexOf('.') + 1) + value.Substring(value.IndexOf('.') + 1, Precision);
                    }
                }
                return ResValue;
            }
        }
        #endregion Decimal precision
        public static string CustomApplicationUrlToken(string ApplicationId, IEncryptionDecryption _encryptionDecryption, IOptions<IngrainAppSettings> appSettings)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CommonUtility), nameof(CustomApplicationUrlToken), "CustomUrlToken for Application" + ApplicationId, string.Empty, string.Empty, string.Empty, string.Empty);
            var AppIntegCollection = _database.GetCollection<AppIntegration>(CONSTANTS.AppIntegration);
            var filterBuilder = Builders<AppIntegration>.Filter;
            var AppFilter = filterBuilder.Eq("ApplicationID", ApplicationId);

            var Projection = Builders<AppIntegration>.Projection.Include("ApplicationID").Include("BaseURL").Include("Authentication").Include("TokenGenerationURL").Include("Credentials").Exclude("_id");
            var AppData = AppIntegCollection.Find(AppFilter).Project<AppIntegration>(Projection).FirstOrDefault();

            dynamic token = string.Empty;
            try
            {
                if (AppData.Authentication == "AzureAD" || AppData.Authentication == "Azure")
                {
                    string GrantType = string.Empty, ClientSecert = string.Empty, ClientId = string.Empty, Resource = string.Empty;
                    AppData.TokenGenerationURL = _encryptionDecryption.Decrypt(AppData.TokenGenerationURL.ToString());
                    AppData.Credentials = BsonDocument.Parse(_encryptionDecryption.Decrypt(AppData.Credentials));

                    GrantType = AppData.Credentials.GetValue("grant_type").AsString;
                    ClientSecert = AppData.Credentials.GetValue("client_secret").AsString;
                    ClientId = AppData.Credentials.GetValue("client_id").AsString;
                    Resource = AppData.Credentials.GetValue("resource").AsString;

                    var client = new RestClient(AppData.TokenGenerationURL.ToString().Trim());
                    var request = new RestRequest(Method.POST);
                    request.AddHeader("cache-control", "no-cache");
                    request.AddHeader("content-type", "application/x-www-form-urlencoded");
                    request.AddParameter("application/x-www-form-urlencoded", "grant_type=" + GrantType +
                    "&client_id=" + ClientId +
                    "&client_secret=" + ClientSecert +
                    "&resource=" + Resource,
                    ParameterType.RequestBody);
                    var requestBuilder = new StringBuilder();
                    foreach (var param in request.Parameters)
                    {
                        requestBuilder.AppendFormat("{0}: {1}\r\n", param.Name, param.Value);
                    }
                    requestBuilder.ToString();
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(CommonUtility), nameof(CustomApplicationUrlToken), "TokenURL: " + AppData.TokenGenerationURL + ", Application TOKEN PARAMS -- " + requestBuilder, string.Empty, string.Empty, string.Empty, string.Empty);
                    IRestResponse response1 = client.Execute(request);
                    string json1 = response1.Content;
                    // Retrieve and Return the Access Token                
                    var tokenObj = JsonConvert.DeserializeObject(json1) as dynamic;
                    token = Convert.ToString(tokenObj.access_token);
                    return token;
                }
                else if (AppData.Authentication == "FORM")
                {
                    using (var httpClient = new HttpClient())
                    {
                        if (appSettings.Value.Environment.Equals(CONSTANTS.PAMEnvironment))
                        {
                            httpClient.BaseAddress = new Uri(appSettings.Value.tokenAPIUrl);
                            httpClient.DefaultRequestHeaders.Accept.Clear();
                            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                            string json = JsonConvert.SerializeObject(new
                            {
                                username = Convert.ToString(appSettings.Value.username),
                                password = Convert.ToString(appSettings.Value.password)
                            });
                            var requestOptions = new StringContent(json, Encoding.UTF8, "application/json");

                            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                            var result = httpClient.PostAsync("", requestOptions).Result;
                            if (result.StatusCode != HttpStatusCode.OK)
                            {
                                throw new Exception(string.Format(CONSTANTS.TokenError, result.StatusCode));
                            }
                            var result1 = result.Content.ReadAsStringAsync().Result;
                            var tokenObj = JsonConvert.DeserializeObject(result1) as dynamic;
                            if (appSettings.Value.Environment == CONSTANTS.PAMEnvironment)
                                token = tokenObj != null ? Convert.ToString(tokenObj.token) : CONSTANTS.InvertedComma;
                            else
                                token = tokenObj != null ? Convert.ToString(tokenObj.access_token) : CONSTANTS.InvertedComma;
                            return token;

                        }
                        else
                        {
                            httpClient.BaseAddress = new Uri(appSettings.Value.tokenAPIUrl);
                            httpClient.DefaultRequestHeaders.Accept.Clear();
                            httpClient.DefaultRequestHeaders.Add("UserName", appSettings.Value.username);
                            httpClient.DefaultRequestHeaders.Add("Password", appSettings.Value.password);
                            HttpContent content = new StringContent(string.Empty);
                            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                            var result = httpClient.PostAsync("", content).Result;

                            if (result.StatusCode != System.Net.HttpStatusCode.OK)
                            {
                                throw new Exception(string.Format("Unable to process your request for generating token. Please check your credentials and try again. Status Code: {0}", result.StatusCode));
                            }

                            var result1 = result.Content.ReadAsStringAsync().Result;
                            var tokenObj = JsonConvert.DeserializeObject(result1) as dynamic;

                            token = Convert.ToString(tokenObj.access_token);

                        }
                    }
                }

            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(CommonUtility), nameof(CustomApplicationUrlToken), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);

            }
            return token;
        }
        public static bool IsNameContainSpecialCharacters(string name)
        {
            return Regex.IsMatch(name, "[^A-Za-z0-9\\s]");
        }

    }
}
