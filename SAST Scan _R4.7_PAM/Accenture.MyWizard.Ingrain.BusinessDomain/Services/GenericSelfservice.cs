#region Copyright (c) 2018 Accenture . All rights reserved.

// <copyright company="Accenture">
// Copyright (c) 2018 Accenture.  All rights reserved.
// Reproduction or transmission in whole or in part, in any form or by any means, 
// electronic, mechanical or otherwise, is prohibited without the prior written 
// consent of the copyright owner.
// </copyright>

#endregion

#region ModelEngineeringService Information
/********************************************************************************************************\
Module Name     :   GenericSelfservice
Project         :   Accenture.MyWizard.SelfServiceAI.BusinessDomain
Organisation    :   Accenture Technologies Ltd.
Created By      :   Thanyaasri Manickam
Created Date    :   10-MAR-2020  
Revision History :
Revision No             Initial             Modified Date           Description
1.0                     An                  10-MAR-2020            
\********************************************************************************************************/
#endregion


using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
using Accenture.MyWizard.Ingrain.BusinessDomain.Common;
using Accenture.MyWizard.Ingrain.DataAccess;
using Accenture.MyWizard.Ingrain.DataModels;
using Accenture.MyWizard.Ingrain.DataModels.InstaModels;
using Accenture.MyWizard.Ingrain.DataModels.Models;
using Accenture.MyWizard.Shared.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
using JsonConvert = Newtonsoft.Json.JsonConvert;
using System.Collections;
using Accenture.MyWizard.Cryptography.EncryptionProviders;

using System.Net.Http.Headers;
using System.Globalization;
using Accenture.MyWizard.Ingrain.DataModels.AICore;

namespace Accenture.MyWizard.Ingrain.BusinessDomain.Services
{
    public class GenericSelfservice : IGenericSelfservice
    {

        #region Members
        private MongoClient _mongoClient;
        private IMongoDatabase _database;
        private readonly IOptions<IngrainAppSettings> appSettings;
        DatabaseProvider databaseProvider;
        GenericDataResponse _GenericDataResponse;
        private IIngestedData _ingestedDataService { get; set; }
        private PublicTemplateMapping _Mapping;
        private IDeployedModelService _deployedModelService { get; set; }
        private DeployModelViewModel _deployModelViewModel;
        TemplateModelPrediction _modelPrediction;
        private IEncryptionDecryption _encryptionDecryption;
        private PredictionResultDTO _predictionresult;
        private IScopeSelectorService _ScopeSelector;
        private readonly IFlushService _flushService;
        private ModelTrainingStatus ModelTrainingStatus;
        private TrainingRequestDetails _trainingRequestDetails;
        private CallBackErrorLog CallBackErrorLog;
        private IFlaskAPI _iFlaskAPIService;
        private MongoClient _mongoClientAD;
        private IMongoDatabase _databaseAD;
        #endregion

        #region Constructor
        public GenericSelfservice(DatabaseProvider db, IOptions<IngrainAppSettings> settings, IServiceProvider serviceProvider)
        {
            databaseProvider = db;
            appSettings = settings;
            _mongoClient = databaseProvider.GetDatabaseConnection();
            var dataBaseName = MongoUrl.Create(appSettings.Value.connectionString).DatabaseName;
            _database = _mongoClient.GetDatabase(dataBaseName);
            _Mapping = new PublicTemplateMapping();
            _GenericDataResponse = new GenericDataResponse();
            _modelPrediction = new TemplateModelPrediction();
            _predictionresult = new PredictionResultDTO();
            _deployedModelService = serviceProvider.GetService<IDeployedModelService>();
            _ingestedDataService = serviceProvider.GetService<IIngestedData>();
            _encryptionDecryption = serviceProvider.GetService<IEncryptionDecryption>();
            _ScopeSelector = serviceProvider.GetService<IScopeSelectorService>();
            _flushService = serviceProvider.GetService<IFlushService>();
            ModelTrainingStatus = new ModelTrainingStatus();
            _trainingRequestDetails = new TrainingRequestDetails();
            CallBackErrorLog = new CallBackErrorLog();
            _iFlaskAPIService = serviceProvider.GetService<IFlaskAPI>();
            //Anomaly Detection
            _mongoClientAD = databaseProvider.GetDatabaseConnection("Anomaly");
            var dataBaseNameAD = MongoUrl.Create(appSettings.Value.AnomalyDetectionCS).DatabaseName;
            _databaseAD = _mongoClientAD.GetDatabase(dataBaseNameAD);
        }
        #endregion

        #region Public Methods


        public GenericDataResponse PublicTemplateModelTraning(string ClientUID, string DCID, string UserId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(PublicTemplateModelTraning), "PublicTemplateModelTraning - Started :", string.Empty, string.Empty, ClientUID, DCID);
            //LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(PublicTemplateModelTraning), "ClientID" + ClientUID + "DCID" + DCID + "UserId" + UserId);
            List<PublicTemplateMapping> MappingResult = new List<PublicTemplateMapping>();

            MappingResult = GetDataMapping(ClientUID, false);
            string encryptedUser = UserId;
            string IngrainUserId = UserId;
            bool DBEncryptionRequired = CommonUtility.EncryptDB(Guid.NewGuid().ToString(), appSettings);
            if (!string.IsNullOrEmpty(Convert.ToString(UserId)))
            {
                encryptedUser = _encryptionDecryption.Encrypt(Convert.ToString(encryptedUser));
                if (DBEncryptionRequired)
                {
                    IngrainUserId = _encryptionDecryption.Encrypt(Convert.ToString(IngrainUserId));
                }
            }
            if (MappingResult.Count > 0)
            {
                foreach (var templatedata in MappingResult)
                {
                    var queueCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
                    var filterBuilder = Builders<BsonDocument>.Filter;
                    var filterQueue = filterBuilder.Eq("ClientId", ClientUID) & filterBuilder.Eq("DeliveryconstructId", DCID) & filterBuilder.Eq("Function", "AutoTrain") & filterBuilder.Eq("TemplateUseCaseID", templatedata.UsecaseID) & (filterBuilder.Eq("CreatedByUser", UserId) | (filterBuilder.Eq("CreatedByUser", encryptedUser)));
                    var queueResult = queueCollection.Find(filterQueue).FirstOrDefault();

                    if (queueResult == null)
                    {
                        string NewModelCorrelationID = Guid.NewGuid().ToString();

                        IngrainRequestQueue ingrainRequest = new IngrainRequestQueue
                        {
                            _id = Guid.NewGuid().ToString(),
                            CorrelationId = NewModelCorrelationID,
                            RequestId = Guid.NewGuid().ToString(),
                            ProcessId = CONSTANTS.Null,
                            ApplicationName = templatedata.ApplicationName,
                            //Status = CONSTANTS.Null,
                            ModelName = templatedata.ApplicationName + "_" + templatedata.UsecaseName,
                            //  RequestStatus = CONSTANTS.New,
                            RetryCount = 0,
                            ProblemType = CONSTANTS.Null,
                            //  Message = CONSTANTS.Null,
                            UniId = CONSTANTS.Null,
                            InstaID = CONSTANTS.Null,
                            Progress = CONSTANTS.Null,
                            pageInfo = CONSTANTS.AutoTrain,
                            ParamArgs = CONSTANTS.Null,
                            TemplateUseCaseID = templatedata.UsecaseID,
                            Function = CONSTANTS.AutoTrain,
                            CreatedByUser = IngrainUserId,
                            CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                            ModifiedByUser = CONSTANTS.Null,
                            ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                            LastProcessedOn = CONSTANTS.Null,
                            AppID = templatedata.ApplicationID,
                            ClientId = ClientUID,
                            DeliveryconstructId = DCID,
                            UseCaseID = CONSTANTS.Null,
                            //DataSource = CONSTANTS.Null,
                            EstimatedRunTime = CONSTANTS.Null
                        };
                        bool isdata = CheckRequiredDetails(templatedata.ApplicationName, templatedata.UsecaseID);
                        if (isdata)
                        {
                            ingrainRequest.Status = CONSTANTS.Null;
                            ingrainRequest.RequestStatus = CONSTANTS.New;
                            ingrainRequest.Message = CONSTANTS.Null;
                        }
                        else
                        {
                            ingrainRequest.Status = CONSTANTS.E;
                            ingrainRequest.RequestStatus = CONSTANTS.ErrorMessage;
                            ingrainRequest.Message = "The Model is created using File Upload. Please contact Ingrain support team to train it.";
                        }
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(PublicTemplateModelTraning), "Auto Train function initiated TemplateID:" + templatedata.UsecaseID + "  NewCorrelationID :" + NewModelCorrelationID + " AppName" + templatedata.ApplicationName + " UsecaseName " + templatedata.UsecaseName, string.Empty, string.Empty, ClientUID, DCID);
                        InsertRequests(ingrainRequest);
                        _GenericDataResponse.Message = CONSTANTS.TrainingResponse;
                        _GenericDataResponse.Status = CONSTANTS.C;
                    }
                    else
                    {
                        _GenericDataResponse.Message = CONSTANTS.TrainingMessage;
                        _GenericDataResponse.Status = CONSTANTS.I;
                    }
                }
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(PublicTemplateModelTraning), "ModelTraning - Ended :", string.Empty, string.Empty, ClientUID, DCID);
            return _GenericDataResponse;
        }
        public bool CheckRequiredDetails(string ApplicationName, string UsecaseID)
        {
            var AppIntegCollection = _database.GetCollection<AppIntegration>(CONSTANTS.AppIntegration);
            var filterBuilder = Builders<AppIntegration>.Filter;
            var AppFilter = filterBuilder.Eq("ApplicationName", ApplicationName);

            var Projection = Builders<AppIntegration>.Projection.Include("BaseURL").Include("Authentication").Include("TokenGenerationURL").Include("Credentials").Exclude("_id");
            var AppData = AppIntegCollection.Find(AppFilter).Project<AppIntegration>(Projection).FirstOrDefault();

            var MappingCollection = _database.GetCollection<PublicTemplateMapping>(CONSTANTS.PublicTemplateMapping);
            var Builder = Builders<PublicTemplateMapping>.Filter;
            var Projection1 = Builders<PublicTemplateMapping>.Projection.Exclude("_id");
            var filter = Builder.Eq("ApplicationName", ApplicationName) & Builder.Eq("UsecaseID", UsecaseID);
            var templatedata = MappingCollection.Find(filter).Project<PublicTemplateMapping>(Projection1).FirstOrDefault();

            if (AppData != null && templatedata != null)
            {
                if (templatedata.SourceName == "Custom" || templatedata.SourceName == null)
                {
                    if (string.IsNullOrEmpty(templatedata.SourceURL) || templatedata.InputParameters == null || string.IsNullOrEmpty(AppData.Authentication)
                        || string.IsNullOrEmpty(AppData.TokenGenerationURL) || AppData.Credentials == null)
                    {
                        return false;
                    }
                }
            }
            else
            {
                return false;
            }
            return true;

        }
        public bool CheckRequireCascadedDetails(string ApplicationName, string UsecaseID)
        {
            var AppIntegCollection = _database.GetCollection<AppIntegration>(CONSTANTS.AppIntegration);
            var filterBuilder = Builders<AppIntegration>.Filter;
            var AppFilter = filterBuilder.Eq("ApplicationName", ApplicationName);

            var Projection = Builders<AppIntegration>.Projection.Include("BaseURL").Include("Authentication").Include("TokenGenerationURL").Include("Credentials").Exclude("_id");
            var AppData = AppIntegCollection.Find(AppFilter).Project<AppIntegration>(Projection).FirstOrDefault();

            var MappingCollection = _database.GetCollection<PublicTemplateMapping>(CONSTANTS.PublicTemplateMapping);
            var Builder = Builders<PublicTemplateMapping>.Filter;
            var Projection1 = Builders<PublicTemplateMapping>.Projection.Exclude("_id");
            var filter = Builder.Eq("ApplicationName", ApplicationName) & Builder.Eq("UsecaseID", UsecaseID);
            var templatedata = MappingCollection.Find(filter).Project<PublicTemplateMapping>(Projection1).FirstOrDefault();

            if (AppData != null && templatedata != null)
            {
                if (templatedata.SourceName == "Custom" || templatedata.SourceName == null)
                {
                    return true;
                }
            }
            else
            {
                return false;
            }
            return true;

        }
        public void InsertRequests(IngrainRequestQueue ingrainRequest)
        {
            var requestQueue = JsonConvert.SerializeObject(ingrainRequest);
            var insertRequestQueue = BsonSerializer.Deserialize<BsonDocument>(requestQueue);
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
            collection.InsertOne(insertRequestQueue);
        }
        public List<AppDetails> AllAppDetails(string clientUId, string deliveryConstructUID, string CorrelationId, string Environment, string ServiceName = "")
        {
            IMongoCollection<BsonDocument> appCollection;
            IMongoCollection<DeployModelsDto> collection;
            if (ServiceName == "Anomaly")
            {
                appCollection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.AppIntegration);
                collection = _databaseAD.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
            }
            else
            {
                appCollection = _database.GetCollection<BsonDocument>(CONSTANTS.AppIntegration);
                collection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
            }

            List<AppDetails> ApplicationDetails = new List<AppDetails>();
            bool isAppList = false;
            //var appCollection = _database.GetCollection<BsonDocument>(CONSTANTS.AppIntegration);
            var filterBuilder = Builders<BsonDocument>.Filter;
            var Projection = Builders<BsonDocument>.Projection.Include("ApplicationID").Include("ApplicationName").Exclude("_id");


            //var collection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
            var builder = Builders<DeployModelsDto>.Filter;
            var filter1 = builder.Eq("CorrelationId", CorrelationId);
            var Projection1 = Builders<DeployModelsDto>.Projection.Include("LinkedApps").Include("Status").Include("IsPrivate").Include(CONSTANTS.IsModelTemplate).Include("TemplateUsecaseId").Include("InstaId").Exclude("_id");
            var deployedModel = collection.Find(filter1).Project<DeployModelsDto>(Projection1).FirstOrDefault();

            if (deployedModel != null)
            {
                if ((deployedModel.IsPrivate.ToString().ToLower() == "false") && deployedModel.IsModelTemplate.ToString().ToLower() == "true")  // Public Models
                {
                    if ((deployedModel.Status == "Deployed"))
                    {
                        isAppList = true;
                    }
                    else
                    {
                        isAppList = false;
                    }

                }
                else // Private Models
                {
                    if ((!string.IsNullOrEmpty(deployedModel.TemplateUsecaseId)))
                    {
                        isAppList = true;
                    }
                    else
                    {
                        isAppList = false;
                    }

                }

                //if ((!string.IsNullOrEmpty(deployedModel.InstaId)))
                //{
                //    if (deployedModel.Status == "Deployed")
                //    {
                //        isAppList = true;
                //    }
                //    else
                //    {
                //        isAppList = true;
                //        string[] linkedApps = new string[] { CONSTANTS.VDS_SI };
                //        if (deployedModel.LinkedApps != null)
                //        {
                //            deployedModel.LinkedApps = linkedApps;
                //        }
                //    }

                //}

                if (deployedModel.LinkedApps != null)
                {
                    if (deployedModel.LinkedApps.Count() != 0)
                    {
                        if (deployedModel.LinkedApps[0] == "Virtual Data Scientist(VDS)")
                        {
                            deployedModel.LinkedApps[0] = CONSTANTS.VDS_SI;
                        }
                    }
                }

            }

            if (Environment != "null")
            {
                if (Environment == CONSTANTS.FDSEnvironment || Environment == CONSTANTS.PAMEnvironment)
                {
                    var filter = (filterBuilder.Eq("clientUId", clientUId) & filterBuilder.Eq("isDefault", false)) | (filterBuilder.Eq("Environment", Environment) & filterBuilder.Eq("isDefault", true));
                    var ApplicationResult = appCollection.Find(filter).Project<BsonDocument>(Projection).ToList();
                    if (ApplicationResult.Count() > 0)
                    {
                        ApplicationDetails = JsonConvert.DeserializeObject<List<AppDetails>>(ApplicationResult.ToJson());
                        if (isAppList)
                        {
                            ApplicationDetails.RemoveAll(x => x.ApplicationName != deployedModel.LinkedApps[0]);
                        }
                    }
                }
                else
                {
                    var filter = (filterBuilder.Eq("clientUId", clientUId) & filterBuilder.Eq("isDefault", false)) | (filterBuilder.Eq("Environment", "PAD") & filterBuilder.Eq("isDefault", true));
                    var ApplicationResult = appCollection.Find(filter).Project<BsonDocument>(Projection).ToList();
                    if (ApplicationResult.Count() > 0)
                    {
                        ApplicationDetails = JsonConvert.DeserializeObject<List<AppDetails>>(ApplicationResult.ToJson());
                        if (isAppList)
                        {
                            ApplicationDetails.RemoveAll(x => x.ApplicationName != deployedModel.LinkedApps[0]);
                        }
                    }
                }
            }
            else
            {
                var filter = (filterBuilder.Eq("clientUId", clientUId) & filterBuilder.Eq("isDefault", false)) | (filterBuilder.Eq("Environment", "PAD") & filterBuilder.Eq("isDefault", true));
                var ApplicationResult = appCollection.Find(filter).Project<BsonDocument>(Projection).ToList();
                if (ApplicationResult.Count() > 0)
                {
                    ApplicationDetails = JsonConvert.DeserializeObject<List<AppDetails>>(ApplicationResult.ToJson());
                    if (isAppList)
                    {
                        ApplicationDetails.RemoveAll(x => x.ApplicationName != deployedModel.LinkedApps[0]);
                    }
                }
            }

            return ApplicationDetails;
        }

        public bool IsAppEditable(string CorrelationId)
        {

            var collection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
            var builder = Builders<DeployModelsDto>.Filter;
            var filter = builder.Eq("CorrelationId", CorrelationId);
            var Projection = Builders<DeployModelsDto>.Projection.Include("LinkedApps").Include("Status").Include("IsPrivate").Include(CONSTANTS.IsModelTemplate).Include("TemplateUsecaseId").Include("InstaId").Exclude("_id");
            var deployedModel = collection.Find(filter).Project<DeployModelsDto>(Projection).FirstOrDefault();

            if (deployedModel != null)
            {
                if ((deployedModel.IsPrivate.ToString().ToLower() == "false") && deployedModel.IsModelTemplate.ToString().ToLower() == "true")// Public Models
                {
                    if ((deployedModel.Status == "Deployed"))
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }

                }
                else // Private Models
                {
                    if ((!string.IsNullOrEmpty(deployedModel.TemplateUsecaseId)) | (!string.IsNullOrEmpty(deployedModel.InstaId)))
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }

                }

            }
            return true;
        }
        public string AddNewApp(AppIntegration appIntegrations, string ServiceName = "")
        {
            IMongoCollection<BsonDocument> collection;
            if (ServiceName == "Anomaly")
                collection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.AppIntegration);
            else
                collection = _database.GetCollection<BsonDocument>(CONSTANTS.AppIntegration);
            appIntegrations.ApplicationID = Guid.NewGuid().ToString();
            appIntegrations.CreatedOn = DateTime.UtcNow.ToString();
            appIntegrations.ModifiedOn = DateTime.UtcNow.ToString();
            appIntegrations.ModifiedByUser = appIntegrations.CreatedByUser;
            AppIntegration appIntegration = new AppIntegration();
           // var collection = _database.GetCollection<BsonDocument>(CONSTANTS.AppIntegration);
            var filter = Builders<BsonDocument>.Filter.Eq("ApplicationName", appIntegrations.ApplicationName) & Builders<BsonDocument>.Filter.Eq("Environment", appIntegrations.Environment);
            var Projection = Builders<BsonDocument>.Projection.Exclude("_id");
            var result = collection.Find(filter).Project<BsonDocument>(Projection).ToList();

            if (result.Count > 0)
            {
                List<AppIntegration> appList = JsonConvert.DeserializeObject<List<AppIntegration>>(result.ToJson());

                if (appList.Any(s => s.isDefault == true) || appList.Any(s => s.clientUId == appIntegrations.clientUId))
                {
                    throw new Exception(CONSTANTS.ApplicationStatus);
                }

            }

            if (!string.IsNullOrEmpty(appIntegrations.TokenGenerationURL))
            {
                appIntegrations.TokenGenerationURL = _encryptionDecryption.Encrypt(appIntegrations.TokenGenerationURL);
            }
            if (appIntegrations.Credentials != null)
            {
                appIntegrations.Credentials = _encryptionDecryption.Encrypt(JsonConvert.SerializeObject(appIntegrations.Credentials, Formatting.None));
            }
            if (!string.IsNullOrEmpty(Convert.ToString(appIntegrations.CreatedByUser)))
            {
                appIntegrations.CreatedByUser = _encryptionDecryption.Encrypt(Convert.ToString(appIntegrations.CreatedByUser));
            }
            if (!string.IsNullOrEmpty(Convert.ToString(appIntegrations.ModifiedByUser)))
            {
                appIntegrations.ModifiedByUser = _encryptionDecryption.Encrypt(Convert.ToString(appIntegrations.ModifiedByUser));
            }
            if (string.IsNullOrEmpty(appIntegrations.clientUId))
            {
                appIntegrations.isDefault = true;
            }
            else
            {
                appIntegration.isDefault = false;
            }
            if (string.IsNullOrEmpty(appIntegrations.Environment))
            {
                appIntegrations.Environment = "PAD";
            }
            var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(appIntegrations);
            var insertDocument = BsonSerializer.Deserialize<BsonDocument>(jsonData);
            collection.InsertOneAsync(insertDocument);
            return appIntegrations.ApplicationID;



        }


        public AppIntegration GetAppDetails(string applicationId)
        {
            var appCollection = _database.GetCollection<AppIntegration>(CONSTANTS.AppIntegration);
            var appFilter = Builders<AppIntegration>.Filter.Eq("ApplicationID", applicationId);
            var appProjection = Builders<AppIntegration>.Projection.Exclude("_id");
            var result = appCollection.Find(appFilter).Project<AppIntegration>(appProjection).FirstOrDefault();
            return result;
        }

        public DeleteAppResponse deleteAppName(string applicationId)
        {
            DeleteAppResponse response = new DeleteAppResponse();
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(deleteAppName), "deleteAppName Started", applicationId, string.Empty, string.Empty, string.Empty);
            var MappingCollection = _database.GetCollection<PublicTemplateMapping>(CONSTANTS.PublicTemplateMapping);
            var Builder = Builders<PublicTemplateMapping>.Filter;
            var Projection1 = Builders<PublicTemplateMapping>.Projection.Exclude("_id");
            var filter1 = Builders<PublicTemplateMapping>.Filter.Eq("ApplicationID", applicationId);
            var Appexist = MappingCollection.Find(filter1).Project<PublicTemplateMapping>(Projection1).ToList();
            if (Appexist.Count > 0)
            {
                response.Message = "Application cannot be deleted, it's already mapped to a Usecase";
                response.Status = "I";
                return response;
            }
            else
            {
                var AppIntegCollection = _database.GetCollection<AppIntegration>(CONSTANTS.AppIntegration);
                var filterBuilder = Builders<AppIntegration>.Filter;
                var Projection = Builders<AppIntegration>.Projection.Exclude("_id");
                var AppFilter = filterBuilder.Eq("ApplicationID", applicationId);
                var Applicationexist = AppIntegCollection.Find(AppFilter).Project<AppIntegration>(Projection).FirstOrDefault();
                if (Applicationexist != null)
                {
                    if (!Applicationexist.isDefault)
                    {
                        AppIntegCollection.DeleteMany(AppFilter);
                        response.Message = "Application Successfully deleted";
                        response.Status = "C";
                        return response;
                    }
                    else
                    {
                        response.Message = "Cannot delete default applications";
                        response.Status = "I";
                        return response;
                    }
                }
                response.Message = "Application not Available";
                response.Status = "I";
                return response;
            }

        }

        public List<TrainingStatus> GetDefaultModelsTrainingStatus(string clientid, string dcid, string userid)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(GetDefaultModelsTrainingStatus), "GetDefaultModelsTrainingStatus STARTED", string.Empty, string.Empty, clientid, dcid);
            //List<IngrainDeliveryConstruct> userrole = null;

            //To Check Admin user
            //Get list of template mappings
            string encryptedUser = userid;
            if (!string.IsNullOrEmpty(Convert.ToString(userid)))
                encryptedUser = _encryptionDecryption.Encrypt(Convert.ToString(userid));
            //userrole = _ScopeSelector.GetUserRole(userid);            
            string userRole = null;
            if (appSettings.Value.Environment == CONSTANTS.FDSEnvironment || appSettings.Value.Environment == CONSTANTS.PAMEnvironment)
            {
                userRole = "client admin";
            }
            else
            {
                List<IngrainDeliveryConstruct> userdetail = _ScopeSelector.GetUserRole(userid);
                userRole = userdetail[0].AccessRoleName.ToString().ToLower();
            }

            List<TrainingStatus> mappingDetails = new List<TrainingStatus>();
            if (!string.IsNullOrEmpty(userRole))
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(GetDefaultModelsTrainingStatus), "GetDefaultModelsTrainingStatus ACCESSROLENAME --" + userRole, string.Empty, string.Empty, clientid, dcid);
                if (userRole.Contains("admin") || userRole.Contains("solution architect"))
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(GetDefaultModelsTrainingStatus), "GetDefaultModelsTrainingStatus USERID --" + userid, string.Empty, string.Empty, clientid, dcid);
                    // Get list of template mappings
                    List<TrainingStatus> cascadeMappingDetails = null;
                    var templateCollection = _database.GetCollection<BsonDocument>(CONSTANTS.PublicTemplateMapping);
                    var filterBuilder = Builders<BsonDocument>.Filter;
                    var filterBuilder2 = Builders<BsonDocument>.Filter;
                    var filter = filterBuilder.Eq(CONSTANTS.IsCascadeModelTemplate, false) & (filterBuilder.Ne(CONSTANTS.Application, CONSTANTS.SPAAPP) & filterBuilder.Ne(CONSTANTS.Application, CONSTANTS.SPAVelocityApp));
                    var cascadeFilter = filterBuilder2.Eq(CONSTANTS.IsCascadeModelTemplate, true) & (filterBuilder2.Ne(CONSTANTS.Application, CONSTANTS.SPAAPP) & filterBuilder2.Ne(CONSTANTS.Application, CONSTANTS.SPAVelocityApp));
                    var Projection = Builders<BsonDocument>.Projection.Include("ApplicationID").Include("UsecaseID").Include("ApplicationName").Include("UsecaseName").Include(CONSTANTS.IsCascadeModelTemplate).Exclude("_id");
                    var result = templateCollection.Find(filter).Project<BsonDocument>(Projection).ToList();
                    var cascadeResult = templateCollection.Find(cascadeFilter).Project<BsonDocument>(Projection).ToList();
                    if (result.Count > 0)
                    {
                        mappingDetails = JsonConvert.DeserializeObject<List<TrainingStatus>>(result.ToJson());
                        if (cascadeResult.Count > 0)
                        {
                            cascadeMappingDetails = JsonConvert.DeserializeObject<List<TrainingStatus>>(cascadeResult.ToJson());
                            mappingDetails = mappingDetails.Concat(cascadeMappingDetails).ToList();
                        }
                    }

                    if (mappingDetails != null)
                    {
                        foreach (var template in mappingDetails)
                        {
                            var filterBuilder1 = Builders<AppIntegration>.Filter;
                            var AppIntegCollection = _database.GetCollection<AppIntegration>(CONSTANTS.AppIntegration);
                            var AppFilter = filterBuilder1.Where(x => x.ApplicationID == template.ApplicationID);
                            var Projection1 = Builders<AppIntegration>.Projection.Exclude("_id");
                            var AppData = AppIntegCollection.Find(AppFilter).Project<AppIntegration>(Projection1).ToList();
                            if (AppData.Count > 0)
                            {
                                for (int i = 0; i < AppData.Count; i++)
                                {
                                    if (AppData[i].ApplicationName.Trim() == template.ApplicationName.Trim())
                                    {
                                        if (!AppData[i].isDefault)
                                        {
                                            if (AppData[0].clientUId != clientid)
                                            {
                                                template.ApplicationName = "D";
                                            }
                                        }
                                    }
                                }
                            }
                            if (AppData.Count() == 0)
                            {
                                template.ApplicationName = "D";
                            }
                        }
                        mappingDetails.RemoveAll(x => x.ApplicationName == "D");
                    }

                    if (mappingDetails != null)
                    {
                        //Get model training status of templates from ssai_ingrainrequests queue
                        IngrainRequestQueue requestQueue = null;
                        var queueCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
                        foreach (var template in mappingDetails)
                        {
                            //To Check Mandatory information is Updated in Collection
                            bool isvalid = false;
                            if (template.IsCascadeModelTemplate)
                                isvalid = CheckRequireCascadedDetails(template.ApplicationName, template.UsecaseID);
                            else
                                isvalid = CheckRequiredDetails(template.ApplicationName, template.UsecaseID);
                            // bool isvalid = CheckRequiredDetails(template.ApplicationName, template.UsecaseID);
                            if (isvalid)
                            {
                                BsonDocument queueResult = null;
                                var projection2 = Builders<BsonDocument>.Projection.Include("CorrelationId").Include("ModelName").Include(CONSTANTS.Status).Include(CONSTANTS.Message).Include("Progress").Include("TemplateUseCaseID").Include("ClientId").Include("DeliveryconstructId").Include("CreatedByUser").Exclude("_id");
                                if (template.IsCascadeModelTemplate)
                                {
                                    var filterQueue = filterBuilder.Eq("ClientId", clientid) & filterBuilder.Eq("DeliveryconstructId", dcid) & filterBuilder.Eq("Function", CONSTANTS.CascadingModel) & filterBuilder.Eq("TemplateUseCaseID", template.UsecaseID) & (filterBuilder.Eq("CreatedByUser", userid) | filterBuilder.Eq("CreatedByUser", encryptedUser));
                                    queueResult = queueCollection.Find(filterQueue).Project<BsonDocument>(projection2).FirstOrDefault();
                                }
                                else
                                {
                                    var filterQueue = filterBuilder.Eq("ClientId", clientid) & filterBuilder.Eq("DeliveryconstructId", dcid) & filterBuilder.Eq("Function", "AutoTrain") & filterBuilder.Eq("TemplateUseCaseID", template.UsecaseID) & (filterBuilder.Eq("CreatedByUser", userid) | filterBuilder.Eq("CreatedByUser", encryptedUser));
                                    queueResult = queueCollection.Find(filterQueue).Project<BsonDocument>(projection2).FirstOrDefault();
                                }
                                if (queueResult != null)
                                {
                                    requestQueue = JsonConvert.DeserializeObject<IngrainRequestQueue>(queueResult.ToJson());
                                    bool DBEncryptionRequired = CommonUtility.EncryptDB(requestQueue.CorrelationId, appSettings);
                                    if (DBEncryptionRequired)
                                    {
                                        try
                                        {
                                            if (!string.IsNullOrEmpty(Convert.ToString(requestQueue.CreatedByUser)))
                                            {
                                                requestQueue.CreatedByUser = _encryptionDecryption.Decrypt(Convert.ToString(requestQueue.CreatedByUser));
                                            }
                                        }
                                        catch (Exception ex) { LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(GetDefaultModelsTrainingStatus) + "User is already decrypted....", ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty); }
                                    }
                                    template.Status = requestQueue.Status;
                                    template.ModelName = requestQueue.ModelName;
                                    template.Progress = requestQueue.Progress;
                                    template.ClientId = requestQueue.ClientId;
                                    template.CreatedByUser = requestQueue.CreatedByUser;
                                    template.CreatedOn = requestQueue.CreatedOn;
                                    template.DeliveryconstructId = requestQueue.DeliveryconstructId;
                                    template.CorrelationId = requestQueue.CorrelationId;
                                    template.Message = requestQueue.Message;
                                }

                                if (template.Status == null && template.ModelName == null)
                                {
                                    template.ModelName = template.UsecaseName;
                                    template.Status = "I";
                                    template.Message = "Training is not initiated";
                                }
                            }
                            else
                            {
                                template.Status = "D";
                            }

                        }
                        mappingDetails.RemoveAll(x => x.Status == "D");
                    }
                    return mappingDetails;
                }
                else
                {
                    throw new Exception("Non Admin user login");
                }
            }
            else
            {
                throw new Exception("Non Admin user login");
            }
        }

        public string GetAppModelsTrainingStatus(string clientid, string dcid, string userid)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(GetAppModelsTrainingStatus), "START", string.Empty, string.Empty, clientid, dcid);
            //To Check Admin user
            string encryptedUser = userid;
            if (!string.IsNullOrEmpty(Convert.ToString(userid)))
                encryptedUser = _encryptionDecryption.Encrypt(Convert.ToString(userid));
            List<IngrainDeliveryConstruct> userrole = null;
            userrole = _ScopeSelector.GetUserRole(userid);
            if (userrole[0].AccessRoleName.ToLower().Contains("admin"))
            {
                //Get list of template mappings           
                List<TrainingStatus> mappingDetails = null;
                List<TrainingStatus> cascadeMappingDetails = null;
                var templateCollection = _database.GetCollection<BsonDocument>(CONSTANTS.PublicTemplateMapping);
                var filterBuilder = Builders<BsonDocument>.Filter;
                var filterBuilder2 = Builders<BsonDocument>.Filter;
                var filter = filterBuilder.Eq(CONSTANTS.IsCascadeModelTemplate, false) & (filterBuilder.Ne(CONSTANTS.Application, CONSTANTS.SPAAPP) & filterBuilder.Ne(CONSTANTS.Application, CONSTANTS.SPAVelocityApp));
                var cascadeFilter = filterBuilder2.Eq(CONSTANTS.IsCascadeModelTemplate, true) & (filterBuilder2.Ne(CONSTANTS.Application, CONSTANTS.SPAAPP) & filterBuilder2.Ne(CONSTANTS.Application, CONSTANTS.SPAVelocityApp));
                var Projection = Builders<BsonDocument>.Projection.Include("ApplicationID").Include("UsecaseID").Include("ApplicationName").Include("UsecaseName").Include(CONSTANTS.IsCascadeModelTemplate).Exclude("_id");
                var result = templateCollection.Find(filter).Project<BsonDocument>(Projection).ToList();
                var cascadeResult = templateCollection.Find(cascadeFilter).Project<BsonDocument>(Projection).ToList();
                if (result.Count > 0)
                {
                    mappingDetails = JsonConvert.DeserializeObject<List<TrainingStatus>>(result.ToJson());
                    if (cascadeResult.Count > 0)
                    {
                        cascadeMappingDetails = JsonConvert.DeserializeObject<List<TrainingStatus>>(cascadeResult.ToJson());
                        mappingDetails = mappingDetails.Concat(cascadeMappingDetails).ToList();
                    }
                }

                if (mappingDetails != null)
                {
                    foreach (var template in mappingDetails)
                    {
                        var filterBuilder1 = Builders<AppIntegration>.Filter;
                        var AppIntegCollection = _database.GetCollection<AppIntegration>(CONSTANTS.AppIntegration);
                        var AppFilter = filterBuilder1.Where(x => x.ApplicationID == template.ApplicationID);
                        var Projection1 = Builders<AppIntegration>.Projection.Exclude("_id");
                        var AppData = AppIntegCollection.Find(AppFilter).Project<AppIntegration>(Projection1).ToList();
                        if (AppData.Count > 0)
                        {
                            if (!AppData[0].isDefault)
                            {
                                if (AppData[0].clientUId != clientid)
                                {
                                    template.ApplicationName = "D";
                                }
                            }
                        }
                        if (AppData.Count() == 0)
                        {
                            template.ApplicationName = "D";
                        }
                    }
                    mappingDetails.RemoveAll(x => x.ApplicationName == "D");
                }
                if (mappingDetails != null)
                {
                    //Get model training status of templates from ssai_ingrainrequests queue
                    IngrainRequestQueue requestQueue = null;
                    var queueCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
                    foreach (var template in mappingDetails)
                    {
                        //To Check Mandatory information is Updated in Collection
                        bool isvalid = false;
                        if (template.IsCascadeModelTemplate)
                            isvalid = CheckRequireCascadedDetails(template.ApplicationName, template.UsecaseID);
                        else
                            isvalid = CheckRequiredDetails(template.ApplicationName, template.UsecaseID);

                        //To Check Mandatory information is Updated in Collection
                        // bool isvalid = CheckRequiredDetails(template.ApplicationName, template.UsecaseID);
                        if (isvalid)
                        {
                            BsonDocument queueResult = null;
                            var projection2 = Builders<BsonDocument>.Projection.Include("CorrelationId").Include("ModelName").Include("RequestStatus").Include("Progress").Include("TemplateUseCaseID").Include("ClientId").Include("DeliveryconstructId").Include("CreatedByUser").Exclude("_id");
                            if (template.IsCascadeModelTemplate)
                            {
                                var filterQueue = filterBuilder.Eq("ClientId", clientid) & filterBuilder.Eq("DeliveryconstructId", dcid) & filterBuilder.Eq("Function", CONSTANTS.CascadingModel) & filterBuilder.Eq("TemplateUseCaseID", template.UsecaseID) & (filterBuilder.Eq("CreatedByUser", userid) | filterBuilder.Eq("CreatedByUser", encryptedUser));
                                queueResult = queueCollection.Find(filterQueue).Project<BsonDocument>(projection2).FirstOrDefault();
                            }
                            else
                            {
                                var filterQueue = filterBuilder.Eq("ClientId", clientid) & filterBuilder.Eq("DeliveryconstructId", dcid) & filterBuilder.Eq("Function", "AutoTrain") & filterBuilder.Eq("TemplateUseCaseID", template.UsecaseID) & (filterBuilder.Eq("CreatedByUser", userid) | filterBuilder.Eq("CreatedByUser", encryptedUser));
                                queueResult = queueCollection.Find(filterQueue).Project<BsonDocument>(projection2).FirstOrDefault();
                            }

                            if (queueResult != null)
                            {
                                requestQueue = JsonConvert.DeserializeObject<IngrainRequestQueue>(queueResult.ToJson());
                                bool DBEncryptionRequired = CommonUtility.EncryptDB(requestQueue.CorrelationId, appSettings);
                                if (DBEncryptionRequired)
                                {
                                    try
                                    {
                                        if (!string.IsNullOrEmpty(Convert.ToString(requestQueue.CreatedByUser)))
                                        {
                                            requestQueue.CreatedByUser = _encryptionDecryption.Decrypt(Convert.ToString(requestQueue.CreatedByUser));
                                        }
                                    }
                                    catch (Exception ex) { LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(GetAppModelsTrainingStatus) + "User is already decrypted....", ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty); }
                                }
                                template.Status = requestQueue.RequestStatus;
                                template.ModelName = requestQueue.ModelName;
                                template.Progress = requestQueue.Progress;
                                template.ClientId = requestQueue.ClientId;
                                template.CreatedByUser = requestQueue.CreatedByUser;

                                template.DeliveryconstructId = requestQueue.DeliveryconstructId;
                                template.CorrelationId = requestQueue.CorrelationId;
                            }
                        }
                        else
                        {
                            template.Status = "D";
                        }
                    }
                    //mappingDetails.RemoveAll(x => x.Status == "D");
                    var checkNewModels = mappingDetails.Where(x => x.Status == null).ToList();
                    var checkCascadeNewModels = mappingDetails.Where(x => (x.IsCascadeModelTemplate.Equals(true) & x.Status == null)).ToList();
                    if (checkNewModels.Count > 0 & checkCascadeNewModels.Count > 0)
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(GetAppModelsTrainingStatus) + "--CHECKNEWMODELS COUNT--" + checkNewModels.Count + "-CHECKCASCADENEWMODELS-" + checkCascadeNewModels, "END", string.Empty, string.Empty, clientid, dcid);
                        return "BothNew";
                    }
                    else if (checkNewModels.Count > 0 & checkCascadeNewModels.Count == 0)
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(GetAppModelsTrainingStatus) + "--CHECKNEWMODELS COUNT--" + checkNewModels.Count + "-CHECKCASCADENEWMODELS-" + checkCascadeNewModels, "END", string.Empty, string.Empty, clientid, dcid);
                        return "New";
                    }
                    else if (checkNewModels.Count == 0 & checkCascadeNewModels.Count > 0)
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(GetAppModelsTrainingStatus) + "--CHECKNEWMODELS COUNT--" + checkNewModels.Count + "-CHECKCASCADENEWMODELS-" + checkCascadeNewModels, "END", string.Empty, string.Empty, clientid, dcid);
                        return "CascadeNew";
                    }
                    else
                    {
                        var checkInProgress = mappingDetails.Where(x => x.Status == "Occupied").ToList();
                        var checkCascadeProgress = mappingDetails.Where(x => (x.IsCascadeModelTemplate.Equals(true) & x.Status == "Occupied")).ToList();
                        if ((checkInProgress.Count > 0) | (checkCascadeProgress.Count > 0))
                        {
                            return "InProgress";
                        }
                        else
                        {
                            return "Completed";
                        }
                    }
                }
                else
                {
                    return "Completed";
                }
            }
            else
            {
                throw new Exception("Non Admin user login");
            }
        }
        public List<object> GetIngestedData(string correlationId, int noOfRecord, string datecolumn)
        {
            List<dynamic> lstInputData = new List<dynamic>();
            List<dynamic> incrementalData = new List<dynamic>();

            var dbCollection = _database.GetCollection<BsonDocument>(CONSTANTS.PSIngestedData);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var projectionScenario = Builders<BsonDocument>.Projection.Include(CONSTANTS.CorrelationId).Include(CONSTANTS.Input_Data).Include(CONSTANTS.ColumnsList).Exclude(CONSTANTS.Id);
            var dbData = dbCollection.Find(filter).Project<BsonDocument>(projectionScenario).ToList();
            bool DBEncryptionRequired = CommonUtility.EncryptDB(correlationId, appSettings);
            if (dbData.Count > 0)
            {

                for (int i = 0; i < dbData.Count; i++)
                {
                    if (DBEncryptionRequired)
                        dbData[i][CONSTANTS.Input_Data] = _encryptionDecryption.Decrypt(dbData[i][CONSTANTS.Input_Data].ToString());
                    var json = dbData[i][CONSTANTS.Input_Data].ToString();
                    lstInputData.AddRange(JsonConvert.DeserializeObject<List<object>>(json));
                }
                if (!string.IsNullOrEmpty(datecolumn))
                {
                    DateTime fromDt = DateTime.Parse(datecolumn);
                    foreach (var data in lstInputData)
                    {
                        DateTime dateVal = DateTime.Parse(data.DateColumn.ToObject<string>());
                        if (dateVal >= fromDt && dateVal < DateTime.Now)
                        {
                            incrementalData.Add(data);
                        }
                    }

                    return incrementalData.Take(noOfRecord).ToList();
                }
                else
                {

                    //for (int i = 0; i < dbData.Count; i++)
                    //{
                    //    if (DBEncryptionRequired)
                    //        dbData[i][CONSTANTS.Input_Data] = BsonDocument.Parse(_encryptionDecryption.Decrypt(dbData[i][CONSTANTS.Input_Data].AsString));
                    //    var json = dbData[i][CONSTANTS.Input_Data].ToString();
                    //    lstInputData.AddRange(JsonConvert.DeserializeObject<List<object>>(json));
                    //}


                    return lstInputData.Take(noOfRecord).ToList();
                }

            }
            return lstInputData;

        }

        public List<object> GetAIServiceIngestedData(string correlationId, int noOfRecord, string datecolumn)
        {
            List<dynamic> lstInputData = new List<dynamic>();
            List<dynamic> incrementalData = new List<dynamic>();

            var dbCollection = _database.GetCollection<BsonDocument>(CONSTANTS.AIServiceIngestData);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var projectionScenario = Builders<BsonDocument>.Projection.Include(CONSTANTS.CorrelationId).Include(CONSTANTS.Input_Data).Exclude(CONSTANTS.Id);
            var dbData = dbCollection.Find(filter).Project<BsonDocument>(projectionScenario).ToList();
            //bool DBEncryptionRequired = CommonUtility.EncryptDB(correlationId, appSettings);
            if (dbData.Count > 0)
            {
                for (int i = 0; i < dbData.Count; i++)
                {
                    try
                    {
                        dbData[i][CONSTANTS.Input_Data] = _encryptionDecryption.Decrypt(dbData[i][CONSTANTS.Input_Data].ToString());
                    }
                    catch (Exception ex)
                    {
                        dbData[i][CONSTANTS.Input_Data] = AesProvider.Decrypt(dbData[i][CONSTANTS.Input_Data].ToString(), "YmFzZTY0ZW5jb2Rlc3RyaQ==", "YmFzZTY0ZW5jb2Rlc3RyaQ==");
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(GetDefaultModelsTrainingStatus) + dbData[i][CONSTANTS.Input_Data].ToString(), ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty);
                    }
                    //if (DBEncryptionRequired)
                    var json = dbData[i][CONSTANTS.Input_Data].ToString();
                    lstInputData.AddRange(JsonConvert.DeserializeObject<List<object>>(json));
                }
                if (!string.IsNullOrEmpty(datecolumn))
                {
                    DateTime fromDt = DateTime.Parse(datecolumn);
                    foreach (var data in lstInputData)
                    {
                        DateTime dateVal = DateTime.Parse(data.DateColumn.ToObject<string>());
                        if (dateVal >= fromDt && dateVal < DateTime.Now)
                        {
                            incrementalData.Add(data);
                        }
                    }

                    return incrementalData.Take(noOfRecord).ToList();
                }
                else
                {
                    return lstInputData.Take(noOfRecord).ToList();
                }

            }
            return lstInputData;

        }


        public List<object> GetClusteringIngestedData(string correlationId, int noOfRecord, string datecolumn)
        {
            List<dynamic> lstInputData = new List<dynamic>();
            List<dynamic> incrementalData = new List<dynamic>();

            var dbCollection = _database.GetCollection<BsonDocument>("Clustering_BusinessProblem");
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var projectionScenario = Builders<BsonDocument>.Projection.Include(CONSTANTS.CorrelationId).Include(CONSTANTS.Input_Data).Exclude(CONSTANTS.Id);
            var dbData = dbCollection.Find(filter).Project<BsonDocument>(projectionScenario).ToList();
            bool DBEncryptionRequired = CommonUtility.DBEncrypt_Clustering(correlationId, appSettings);
            if (dbData.Count > 0)
            {
                for (int i = 0; i < dbData.Count; i++)
                {
                    try
                    {
                        if (DBEncryptionRequired)
                        {
                            dbData[i][CONSTANTS.Input_Data] = _encryptionDecryption.Decrypt(dbData[i][CONSTANTS.Input_Data].ToString());
                        }
                    }
                    catch (Exception ex)
                    {
                        if (DBEncryptionRequired)
                        {
                            dbData[i][CONSTANTS.Input_Data] = AesProvider.Decrypt(dbData[i][CONSTANTS.Input_Data].ToString(), "YmFzZTY0ZW5jb2Rlc3RyaQ==", "YmFzZTY0ZW5jb2Rlc3RyaQ==");
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(GetClusteringIngestedData) + dbData[i][CONSTANTS.Input_Data].ToString(), ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty);
                        }
                    }
                    //if (DBEncryptionRequired)
                    var json = dbData[i][CONSTANTS.Input_Data].ToString();
                    lstInputData.AddRange(JsonConvert.DeserializeObject<List<object>>(json));
                }
                if (!string.IsNullOrEmpty(datecolumn))
                {
                    DateTime fromDt = DateTime.Parse(datecolumn);
                    foreach (var data in lstInputData)
                    {
                        DateTime dateVal = DateTime.Parse(data.DateColumn.ToObject<string>());
                        if (dateVal >= fromDt && dateVal < DateTime.Now)
                        {
                            incrementalData.Add(data);
                        }
                    }

                    return incrementalData.Take(noOfRecord).ToList();
                }
                else
                {
                    return lstInputData.Take(noOfRecord).ToList();
                }

            }
            return lstInputData;

        }

        /// <summary>
        /// SPA App related templates we will not train
        /// </summary>
        /// <param name="ClientUID"></param>
        /// <param name="isCascadeModel"></param>
        /// <returns></returns>
        private List<PublicTemplateMapping> GetDataMapping(string ClientUID, bool isCascadeModel)
        {
            List<PublicTemplateMapping> mappingDetails = null;

            var MappingCollection = _database.GetCollection<PublicTemplateMapping>(CONSTANTS.PublicTemplateMapping);
            var filterBuilder = Builders<PublicTemplateMapping>.Filter;
            FilterDefinition<PublicTemplateMapping> filter = null;
            if (isCascadeModel)
            {
                //filter = filterBuilder.Eq(CONSTANTS.IsCascadeModelTemplate, true);
                filter = filterBuilder.Eq(CONSTANTS.IsCascadeModelTemplate, true) & (filterBuilder.Ne(CONSTANTS.Application, CONSTANTS.SPAAPP) & filterBuilder.Ne(CONSTANTS.Application, CONSTANTS.SPAVelocityApp));
            }
            else
            {
                //filter = filterBuilder.Eq(CONSTANTS.IsCascadeModelTemplate, false);
                filter = filterBuilder.Eq(CONSTANTS.IsCascadeModelTemplate, false) & (filterBuilder.Ne(CONSTANTS.Application, CONSTANTS.SPAAPP) & filterBuilder.Ne(CONSTANTS.Application, CONSTANTS.SPAVelocityApp));
            }
            var Projection = Builders<PublicTemplateMapping>.Projection.Exclude("_id");
            var result = MappingCollection.Find(filter).Project<PublicTemplateMapping>(Projection).ToList();
            if (result.Count > 0)
            {
                mappingDetails = JsonConvert.DeserializeObject<List<PublicTemplateMapping>>(JsonConvert.SerializeObject(result));
            }

            if (mappingDetails != null)
            {
                foreach (var template in mappingDetails)
                {
                    var filterBuilder1 = Builders<AppIntegration>.Filter;
                    var AppIntegCollection = _database.GetCollection<AppIntegration>(CONSTANTS.AppIntegration);
                    var AppFilter = filterBuilder1.Where(x => x.ApplicationID == template.ApplicationID);
                    var Projection1 = Builders<AppIntegration>.Projection.Exclude("_id");
                    var AppData = AppIntegCollection.Find(AppFilter).Project<AppIntegration>(Projection1).ToList();
                    if (AppData.Count > 0)
                    {
                        if (!AppData[0].isDefault)
                        {
                            if (AppData[0].clientUId != ClientUID)
                            {
                                template.ApplicationName = "D";
                            }
                        }
                    }
                    if (AppData.Count() == 0)
                    {
                        template.ApplicationName = "D";
                    }
                }
                mappingDetails.RemoveAll(x => x.ApplicationName == "D");
            }
            return mappingDetails;
        }

        public TemplateModelPrediction GetPublicTemplatesModelsPredictions(GenericApiData genericApiData)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(GetPublicTemplatesModelsPredictions), "Predictions Started", genericApiData.ApplicationID, string.Empty, genericApiData.ClientID, genericApiData.DCID);

            _modelPrediction.UseCaseName = genericApiData.UseCaseName;
            _modelPrediction.ApplicationName = genericApiData.ApplicationName;
            string prediction = string.Empty;
            var collection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
            var builder = Builders<DeployModelsDto>.Filter;
            var filter = builder.Eq("TemplateUsecaseId", genericApiData.UseCaseID)
                & builder.Eq("AppId", genericApiData.ApplicationID) & builder.Eq("ClientUId", genericApiData.ClientID) & builder.Eq("DeliveryConstructUID", genericApiData.DCID);
            var Projection1 = Builders<DeployModelsDto>.Projection.Exclude("_id");
            var deployedModel = collection.Find(filter).Project<DeployModelsDto>(Projection1).FirstOrDefault();

            if (deployedModel != null)
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(GetPublicTemplatesModelsPredictions), "deployedModel Status" + deployedModel.Status + "  genericApiData.UseCaseID" + genericApiData.UseCaseID + "  " + genericApiData.ApplicationName + " genericApiData.Usecsaename " + genericApiData.UseCaseName, genericApiData.ApplicationID, string.Empty, genericApiData.ClientID, genericApiData.DCID);
                _modelPrediction.CorrelationId = deployedModel.CorrelationId;
                if (deployedModel.Status == CONSTANTS.Deployed)
                {
                    bool DBEncryptionRequired = CommonUtility.EncryptDB(deployedModel.CorrelationId, appSettings);

                    if (deployedModel.ModelType != CONSTANTS.TimeSeries)
                    {
                        PredictionDTO predictionDTO = new PredictionDTO
                        {
                            _id = Guid.NewGuid().ToString(),
                            UniqueId = Guid.NewGuid().ToString(),
                            //ActualData = incrementaldata,
                            CorrelationId = deployedModel.CorrelationId,
                            Frequency = null,
                            PredictedData = null,
                            Status = CONSTANTS.I,
                            ErrorMessage = null,
                            Progress = null,
                            CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                            ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                            CreatedByUser = deployedModel.CreatedByUser,
                            ModifiedByUser = deployedModel.ModifiedByUser,
                            AppID = genericApiData.ApplicationID,
                            TempalteUseCaseId = genericApiData.UseCaseID
                        };
                        //adding phoenix condition
                        if (genericApiData.DataSource == "Pheonix")
                        {
                            var ingest_collection = _database.GetCollection<BsonDocument>(CONSTANTS.PS_IngestedData);
                            var ingest_filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, deployedModel.CorrelationId);
                            var ingest_Projection = Builders<BsonDocument>.Projection.Include("lastDateDict").Exclude("_id");
                            var Ingestdata_result = ingest_collection.Find(ingest_filter).Project<BsonDocument>(ingest_Projection).ToList();
                            if (Ingestdata_result.Count > 0)
                            {
                                IngrainRequestQueue requestQueue = new IngrainRequestQueue();
                                requestQueue = GetFileRequestStatus(deployedModel.CorrelationId, CONSTANTS.IngestData);

                                if (requestQueue != null)
                                {
                                    JObject paramArgs = JObject.Parse(requestQueue.ParamArgs);
                                    paramArgs[CONSTANTS.Flag] = "Incremental";
                                    if (paramArgs["metric"].ToString() != "null")
                                    {
                                        JObject metric = JObject.Parse(paramArgs["metric"].ToString());
                                        if (Ingestdata_result[0].Contains("lastDateDict"))
                                            metric["startDate"] = Ingestdata_result[0]["lastDateDict"][0].ToString();
                                        paramArgs["metric"] = JsonConvert.SerializeObject(metric, Formatting.None);
                                    }
                                    else if (paramArgs["pad"].ToString() != "null")
                                    {
                                        JObject pad = JObject.Parse(paramArgs["pad"].ToString());
                                        if (Ingestdata_result[0].Contains("lastDateDict"))
                                            pad["startDate"] = Ingestdata_result[0]["lastDateDict"][0][0].ToString();
                                        paramArgs["pad"] = JsonConvert.SerializeObject(pad, Formatting.None);
                                    }
                                    paramArgs["UniqueId"] = predictionDTO.UniqueId;
                                    requestQueue.ParamArgs = paramArgs.ToString(Formatting.None);
                                    requestQueue._id = Guid.NewGuid().ToString();
                                    requestQueue.RequestId = Guid.NewGuid().ToString();
                                    requestQueue.RequestStatus = CONSTANTS.New;
                                    requestQueue.Status = CONSTANTS.Null;
                                    requestQueue.Progress = CONSTANTS.Null;
                                    requestQueue.Message = CONSTANTS.Null;
                                    requestQueue.RetryCount = 0;
                                    requestQueue.CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                                    requestQueue.ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                                    string requestId = requestQueue.RequestId;
                                    if (deployedModel.ModelType == CONSTANTS.TimeSeries)
                                    {
                                        requestQueue.Frequency = genericApiData.Frequency;
                                    }
                                    else
                                    {
                                        requestQueue.Frequency = CONSTANTS.Null;
                                    }

                                    InsertRequests(requestQueue);
                                    bool flag = true;
                                    Thread.Sleep(2000);
                                    while (flag)
                                    {
                                        requestQueue = GetFileRequestStatusByRequestId(deployedModel.CorrelationId, CONSTANTS.IngestData, requestId);
                                        if (requestQueue != null)
                                        {
                                            if (requestQueue.Status == CONSTANTS.C && requestQueue.Progress == CONSTANTS.Hundred)
                                            {
                                                flag = false;
                                                this.insertRequest(deployedModel, predictionDTO.UniqueId, CONSTANTS.PublishModel);
                                                IsPredictedCompeted(predictionDTO);
                                            }
                                            else if (requestQueue.Status == CONSTANTS.E)
                                            {
                                                _modelPrediction.ErrorMessage = requestQueue.Message;
                                                _modelPrediction.Message = CONSTANTS.PhythonError;
                                                _modelPrediction.Status = CONSTANTS.E;
                                                return _modelPrediction;
                                            }
                                            else if (requestQueue.Status == CONSTANTS.I)
                                            {
                                                _modelPrediction.ErrorMessage = requestQueue.Message;
                                                _modelPrediction.Message = CONSTANTS.PhythonInfo;
                                                _modelPrediction.Status = requestQueue.Status;
                                                return _modelPrediction;
                                            }
                                            else
                                            {
                                                flag = true;
                                                Thread.Sleep(1000);
                                            }
                                            _modelPrediction.Status = requestQueue.Status;
                                        }
                                        else
                                        {
                                            flag = true;
                                            Thread.Sleep(2000);
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            string incrementaldata = GetActualData(deployedModel.CorrelationId, genericApiData.ApplicationName, genericApiData.UseCaseName, genericApiData);
                            JArray Jdata = JArray.Parse(incrementaldata);
                            string jsonString = JsonConvert.SerializeObject(Jdata);
                            string data = jsonString.Replace("null", @"""""");
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(GetPublicTemplatesModelsPredictions), "deployedModel CorrelationId : " + deployedModel.CorrelationId + " No.of Records available :" + Jdata.Count(), genericApiData.ApplicationID, string.Empty, genericApiData.ClientID, genericApiData.DCID);

                            // db data encrypt
                            if (DBEncryptionRequired)
                            {
                                predictionDTO.ActualData = _encryptionDecryption.Encrypt(data);
                            }
                            else
                            {
                                predictionDTO.ActualData = data;
                            }
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(GetPublicTemplatesModelsPredictions), "Deploy model corelation ID :" + deployedModel.CorrelationId + "  predictionDTO.UniqueId " + predictionDTO.UniqueId, genericApiData.ApplicationID, string.Empty, genericApiData.ClientID, genericApiData.DCID);
                            SavePrediction(predictionDTO);
                            this.insertRequest(deployedModel, predictionDTO.UniqueId, CONSTANTS.PublishModel);
                            IsPredictedCompeted(predictionDTO);
                        }
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(GetPublicTemplatesModelsPredictions), "Predictions Ended", genericApiData.ApplicationID, string.Empty, genericApiData.ClientID, genericApiData.DCID);
                        return _modelPrediction;
                    }
                    else
                    {
                        //_deployModelViewModel = _deployedModelService.GetDeployModel(deployedModel.CorrelationId);
                        //string frequency = _deployModelViewModel.DeployModels[0].Frequency;
                        PredictionDTO predictionDTO = new PredictionDTO
                        {
                            _id = Guid.NewGuid().ToString(),
                            UniqueId = Guid.NewGuid().ToString(),
                            // ActualData = incrementaldata,
                            CorrelationId = deployedModel.CorrelationId,
                            Frequency = genericApiData.Frequency,
                            PredictedData = null,
                            Status = CONSTANTS.I,
                            ErrorMessage = null,
                            Progress = null,
                            CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                            ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                            CreatedByUser = deployedModel.CreatedByUser,
                            ModifiedByUser = deployedModel.ModifiedByUser,
                            AppID = genericApiData.ApplicationID,
                            TempalteUseCaseId = genericApiData.UseCaseID
                        };
                        if (genericApiData.DataSource == "Pheonix")
                        {
                            var ingest_collection = _database.GetCollection<BsonDocument>(CONSTANTS.PS_IngestedData);
                            var ingest_filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, deployedModel.CorrelationId);
                            var ingest_Projection = Builders<BsonDocument>.Projection.Include("lastDateDict").Exclude("_id");
                            var Ingestdata_result = ingest_collection.Find(ingest_filter).Project<BsonDocument>(ingest_Projection).ToList();
                            if (Ingestdata_result.Count > 0)
                            {

                                IngrainRequestQueue requestQueue = new IngrainRequestQueue();
                                requestQueue = GetFileRequestStatus(deployedModel.CorrelationId, CONSTANTS.IngestData);

                                if (requestQueue != null)
                                {
                                    JObject paramArgs = JObject.Parse(requestQueue.ParamArgs);
                                    paramArgs[CONSTANTS.Flag] = "Incremental";
                                    if (paramArgs["metric"].ToString() != "null")
                                    {
                                        JObject metric = JObject.Parse(paramArgs["metric"].ToString());
                                        if (Ingestdata_result[0].Contains("lastDateDict"))
                                            metric["startDate"] = Ingestdata_result[0]["lastDateDict"][0].ToString();
                                        paramArgs["metric"] = JsonConvert.SerializeObject(metric, Formatting.None);
                                    }
                                    else if (paramArgs["pad"].ToString() != "null")
                                    {
                                        JObject pad = JObject.Parse(paramArgs["pad"].ToString());
                                        if (Ingestdata_result[0].Contains("lastDateDict"))
                                            pad["startDate"] = Ingestdata_result[0]["lastDateDict"][0][0].ToString();
                                        paramArgs["pad"] = JsonConvert.SerializeObject(pad, Formatting.None);
                                    }
                                    paramArgs["UniqueId"] = predictionDTO.UniqueId;
                                    requestQueue.ParamArgs = paramArgs.ToString(Formatting.None);
                                    requestQueue._id = Guid.NewGuid().ToString();
                                    requestQueue.RequestId = Guid.NewGuid().ToString();
                                    requestQueue.RequestStatus = CONSTANTS.New;
                                    requestQueue.Status = CONSTANTS.Null;
                                    requestQueue.Progress = CONSTANTS.Null;
                                    requestQueue.Message = CONSTANTS.Null;
                                    requestQueue.RetryCount = 0;
                                    requestQueue.CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                                    requestQueue.ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                                    if (deployedModel.ModelType == CONSTANTS.TimeSeries)
                                    {
                                        requestQueue.Frequency = genericApiData.Frequency;
                                    }
                                    else
                                    {
                                        requestQueue.Frequency = CONSTANTS.Null;
                                    }
                                    string requestId = requestQueue.RequestId;

                                    InsertRequests(requestQueue);
                                    bool flag = true;
                                    Thread.Sleep(2000);
                                    while (flag)
                                    {
                                        requestQueue = GetFileRequestStatusByRequestId(deployedModel.CorrelationId, CONSTANTS.IngestData, requestId);
                                        if (requestQueue != null)
                                        {
                                            if (requestQueue.Status == CONSTANTS.C && requestQueue.Progress == CONSTANTS.Hundred)
                                            {
                                                flag = false;
                                                this.insertRequest(deployedModel, predictionDTO.UniqueId, CONSTANTS.ForecastModel);
                                                IsPredictedCompeted(predictionDTO);
                                            }
                                            else if (requestQueue.Status == CONSTANTS.E)
                                            {
                                                _modelPrediction.ErrorMessage = requestQueue.Message;
                                                _modelPrediction.Message = CONSTANTS.PhythonError;
                                                _modelPrediction.Status = CONSTANTS.E;
                                                return _modelPrediction;
                                            }
                                            else if (requestQueue.Status == CONSTANTS.I)
                                            {
                                                _modelPrediction.ErrorMessage = requestQueue.Message;
                                                _modelPrediction.Message = CONSTANTS.PhythonInfo;
                                                _modelPrediction.Status = requestQueue.Status;
                                                return _modelPrediction;
                                            }
                                            else
                                            {
                                                flag = true;
                                                Thread.Sleep(1000);
                                            }
                                            _modelPrediction.Status = requestQueue.Status;
                                        }
                                        else
                                        {
                                            flag = true;
                                            Thread.Sleep(2000);
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            //string incrementaldata = GetActualData(deployedModel.CorrelationId, genericApiData.ApplicationName, genericApiData.UseCaseName, genericApiData);
                            //JArray Jdata = JArray.Parse(incrementaldata);
                            //string jsonString = JsonConvert.SerializeObject(Jdata);
                            //string data = jsonString.Replace("null", @"""""");
                            //LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(GetPublicTemplatesModelsPredictions), "deployedModel CorrelationId : " + deployedModel.CorrelationId + " No.of Records available :" + Jdata.Count());


                            // db data encrypt
                            if (DBEncryptionRequired)
                            {
                                predictionDTO.ActualData = _encryptionDecryption.Encrypt(CONSTANTS.Null);
                            }
                            else
                            {
                                predictionDTO.ActualData = CONSTANTS.Null;
                            }
                            SavePrediction(predictionDTO);
                            this.insertRequest(deployedModel, predictionDTO.UniqueId, CONSTANTS.ForecastModel);
                            IsPredictedCompeted(predictionDTO);
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(GetPublicTemplatesModelsPredictions), "predictionDTO" + predictionDTO.UniqueId, genericApiData.ApplicationID, string.Empty, genericApiData.ClientID, genericApiData.DCID);
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(GetPublicTemplatesModelsPredictions), "Predictions Ended", genericApiData.ApplicationID, string.Empty, genericApiData.ClientID, genericApiData.DCID);
                            return _modelPrediction;
                        }
                    }
                }
                else
                {
                    _modelPrediction.Status = CONSTANTS.E;
                    _modelPrediction.Message = CONSTANTS.ModelNotTrained;
                    _modelPrediction.ErrorMessage = CONSTANTS.ModelNotTrained;

                }
            }
            else
            {
                _modelPrediction.Status = CONSTANTS.E;
                _modelPrediction.Message = CONSTANTS.ModelNotAvailable;
                _modelPrediction.ErrorMessage = CONSTANTS.ModelNotAvailable;
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(GetPublicTemplatesModelsPredictions), "Predictions Ended", genericApiData.ApplicationID, string.Empty, genericApiData.ClientID, genericApiData.DCID);
            return _modelPrediction;
        }
        public PhoenixPredictionStatus PhoenixPredictionRequest(GenericApiData genericApiData)
        {
            PhoenixPredictionStatus phoenixPredictionStatus = new PhoenixPredictionStatus();
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(PhoenixPredictionRequest), "PhoenixPredictionRequest service Started", genericApiData.ApplicationID, string.Empty, genericApiData.ClientID, genericApiData.DCID);
            phoenixPredictionStatus.ApplicationName = genericApiData.ApplicationName;
            phoenixPredictionStatus.UseCaseName = genericApiData.UseCaseName;

            var collection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
            var builder = Builders<DeployModelsDto>.Filter;
            var filter = builder.Eq("TemplateUsecaseId", genericApiData.UseCaseID)
                & builder.Eq("AppId", genericApiData.ApplicationID) & builder.Eq("ClientUId", genericApiData.ClientID) & builder.Eq("DeliveryConstructUID", genericApiData.DCID) & builder.Eq("Status", "Deployed");
            var Projection1 = Builders<DeployModelsDto>.Projection.Exclude("_id");
            var deployedModel = collection.Find(filter).Project<DeployModelsDto>(Projection1).FirstOrDefault();

            if (deployedModel != null)
            {
                phoenixPredictionStatus.CorrelationId = deployedModel.CorrelationId;

                //adding phoenix condition
                if (genericApiData.DataSource == "Phoenix")
                {
                    var ingest_collection = _database.GetCollection<BsonDocument>(CONSTANTS.PS_IngestedData);
                    var ingest_filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, deployedModel.CorrelationId);
                    var ingest_Projection = Builders<BsonDocument>.Projection.Include("lastDateDict").Exclude("_id");
                    var Ingestdata_result = ingest_collection.Find(ingest_filter).Project<BsonDocument>(ingest_Projection).ToList();
                    if (Ingestdata_result.Count > 0)
                    {
                        IngrainRequestQueue requestQueue = new IngrainRequestQueue();
                        requestQueue = GetFileRequestStatus(deployedModel.CorrelationId, CONSTANTS.IngestData);

                        if (requestQueue != null)
                        {
                            JObject paramArgs = JObject.Parse(requestQueue.ParamArgs);
                            paramArgs[CONSTANTS.Flag] = "Incremental";
                            if (paramArgs["metric"].ToString() != "null")
                            {
                                JObject metric = JObject.Parse(paramArgs["metric"].ToString());
                                if (Ingestdata_result[0].Contains("lastDateDict"))
                                    metric["startDate"] = Ingestdata_result[0]["lastDateDict"][0].ToString();
                                paramArgs["metric"] = JsonConvert.SerializeObject(metric, Formatting.None);
                            }
                            else if (paramArgs["pad"].ToString() != "null")
                            {
                                JObject pad = JObject.Parse(paramArgs["pad"].ToString());
                                if (Ingestdata_result[0].Contains("lastDateDict"))
                                    pad["startDate"] = Ingestdata_result[0]["lastDateDict"][0][0].ToString();
                                paramArgs["pad"] = JsonConvert.SerializeObject(pad, Formatting.None);
                            }
                            //paramArgs["UniqueId"] = predictionDTO.UniqueId;
                            requestQueue.ParamArgs = paramArgs.ToString(Formatting.None);
                            requestQueue._id = Guid.NewGuid().ToString();
                            requestQueue.RequestId = Guid.NewGuid().ToString();
                            requestQueue.UniId = Guid.NewGuid().ToString();
                            requestQueue.RequestStatus = CONSTANTS.New;
                            requestQueue.Status = CONSTANTS.Null;
                            requestQueue.Progress = CONSTANTS.Null;
                            requestQueue.Message = CONSTANTS.Null;
                            requestQueue.RetryCount = 0;
                            requestQueue.CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                            requestQueue.ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                            string requestId = requestQueue.RequestId;
                            if (deployedModel.ModelType == CONSTANTS.TimeSeries)
                            {
                                requestQueue.Frequency = genericApiData.Frequency;
                            }
                            else
                            {
                                requestQueue.Frequency = CONSTANTS.Null;
                            }
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(PhoenixPredictionRequest), "requestQueue.UniId" + requestQueue.UniId, genericApiData.ApplicationID, string.Empty, genericApiData.ClientID, genericApiData.DCID);
                            InsertRequests(requestQueue);
                            //Thread.Sleep(2000);
                            phoenixPredictionStatus.UniqueId = requestQueue.UniId;
                            phoenixPredictionStatus.Status = "I";
                            phoenixPredictionStatus.Message = "Prediction request initiated successfully";

                        }
                    }
                }

                LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(PhoenixPredictionRequest), "PhoenixPredictionRequest service ended", genericApiData.ApplicationID, string.Empty, genericApiData.ClientID, genericApiData.DCID);
                return phoenixPredictionStatus;
            }

            else
            {
                phoenixPredictionStatus.Status = CONSTANTS.E;
                phoenixPredictionStatus.Message = "Model not available for the UseCaseId";
                phoenixPredictionStatus.ErrorMessage = "Model not available for the UseCaseId";
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(PhoenixPredictionRequest), "PhoenixPredictionRequest service ended", genericApiData.ApplicationID, string.Empty, genericApiData.ClientID, genericApiData.DCID);
            return phoenixPredictionStatus;
        }


        private List<string> GetPublishModelPages(string correlationId, string uniqueId)
        {
            List<string> availablePages = new List<string>();
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq(CONSTANTS.CorrelationId, correlationId) & builder.Eq(CONSTANTS.UniqueId, uniqueId);
            var projection = Builders<BsonDocument>.Projection.Exclude("_id");
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_PublishModel);
            var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();

            if (result.Count > 0)
            {
                foreach (var req in result)
                {
                    if (!req["Chunk_number"].IsBsonNull)
                    {
                        availablePages.Add(req["Chunk_number"].AsString);
                    }

                }
            }

            return availablePages;
        }


        public PhoenixPredictionsOutput GetPhoenixPrediction(PhoenixPredictionsInput phoenixPredictionsInput)
        {
            PhoenixPredictionsOutput phoenixPredictionsOutput = new PhoenixPredictionsOutput();
            phoenixPredictionsOutput.CorrelationId = phoenixPredictionsInput.CorrelationId;
            phoenixPredictionsOutput.UniqueId = phoenixPredictionsInput.UniqueId;
            phoenixPredictionsOutput.PageNumber = phoenixPredictionsInput.PageNumber;

            var collectionDM = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
            var builderDM = Builders<DeployModelsDto>.Filter;
            var filterDM = builderDM.Eq("CorrelationId", phoenixPredictionsInput.CorrelationId);
            var Projection1 = Builders<DeployModelsDto>.Projection.Exclude("_id");
            var deployedModel = collectionDM.Find(filterDM).Project<DeployModelsDto>(Projection1).FirstOrDefault();
            string FunctionName = string.Empty;
            if (deployedModel != null)
            {

                if (deployedModel.ModelType == CONSTANTS.TimeSeries)
                {
                    FunctionName = CONSTANTS.ForecastModel;
                }
                else
                {
                    FunctionName = CONSTANTS.PublishModel;
                }
            }

            IngrainRequestQueue requestQueue = GetPredictionRequestStatus(phoenixPredictionsInput.CorrelationId, phoenixPredictionsInput.UniqueId, CONSTANTS.IngestData);

            if (requestQueue != null)
            {
                if (requestQueue.Status == "C")
                {
                    IngrainRequestQueue predictionStatus = GetPredictionRequestStatus(phoenixPredictionsInput.CorrelationId, phoenixPredictionsInput.UniqueId, FunctionName);
                    if (predictionStatus != null)
                    {
                        return GetPhoenixPredictionsStatus(phoenixPredictionsInput, string.Empty);
                    }
                    else
                    {
                        if (deployedModel != null)
                        {
                            var builder = Builders<BsonDocument>.Filter;
                            var filter = builder.Eq(CONSTANTS.CorrelationId, phoenixPredictionsInput.CorrelationId) & builder.Eq(CONSTANTS.UniqueId, phoenixPredictionsInput.UniqueId);
                            var projection = Builders<BsonDocument>.Projection.Exclude("_id");
                            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_PublishModel);
                            var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();
                            if (result.Count > 0)
                            {
                                if (deployedModel.DBEncryptionRequired)
                                {
                                    result[0]["ActualData"] = _encryptionDecryption.Decrypt(result[0]["ActualData"].ToString());
                                }
                                JArray Jdata = JArray.Parse(result[0]["ActualData"].ToString());
                                string jsonString = JsonConvert.SerializeObject(Jdata);
                                string data = jsonString.Replace("null", @"""""");
                                var update = Builders<BsonDocument>.Update.Set("ActualData", data);
                                var isUpdated = collection.UpdateMany(filter, update);
                            }


                            this.insertRequest(deployedModel, phoenixPredictionsInput.UniqueId, FunctionName);
                            return GetPhoenixPredictionsStatus(phoenixPredictionsInput, deployedModel.ModelType);
                        }
                        else
                        {
                            phoenixPredictionsOutput.Status = "E";
                            phoenixPredictionsOutput.ErrorMessage = "CorrelationId not available";
                            return phoenixPredictionsOutput;
                        }
                    }

                }
                else
                {
                    phoenixPredictionsOutput.Status = requestQueue.Status;
                    phoenixPredictionsOutput.ErrorMessage = requestQueue.Message;
                    return phoenixPredictionsOutput;
                }
            }
            else
            {
                phoenixPredictionsOutput.Status = "E";
                phoenixPredictionsOutput.ErrorMessage = "Unique Id not available, please initiate prediction request";
                return phoenixPredictionsOutput;
            }



        }


        public PhoenixPredictionsOutput GetPhoenixPredictionsStatus(PhoenixPredictionsInput phoenixPredictionsInput, string ProblemType)
        {
            PhoenixPredictionsOutput phoenixPredictionsOutput = new PhoenixPredictionsOutput();
            phoenixPredictionsOutput.CorrelationId = phoenixPredictionsInput.CorrelationId;
            phoenixPredictionsOutput.UniqueId = phoenixPredictionsInput.UniqueId;
            phoenixPredictionsOutput.PageNumber = phoenixPredictionsInput.PageNumber;
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq(CONSTANTS.CorrelationId, phoenixPredictionsInput.CorrelationId)
                         & builder.Eq(CONSTANTS.UniqueId, phoenixPredictionsInput.UniqueId)
                         & builder.Eq("Chunk_number", phoenixPredictionsInput.PageNumber);
            var projection = Builders<BsonDocument>.Projection.Exclude("_id");
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_PublishModel);
            var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            bool DBEncryptionRequired = CommonUtility.EncryptDB(phoenixPredictionsInput.CorrelationId, appSettings);
            if (result.Count > 0)
            {
                string status = result[0]["Status"].AsString;

                if (status == "C")
                {
                    if (DBEncryptionRequired)
                    {
                        if (result[0]["PredictedData"].AsString != null)
                            result[0]["PredictedData"] = _encryptionDecryption.Decrypt(result[0]["PredictedData"].AsString);
                    }
                    phoenixPredictionsOutput.PredictedData = result[0]["PredictedData"].AsString;
                    phoenixPredictionsOutput.Progress = result[0]["Progress"].AsString;
                    phoenixPredictionsOutput.Status = result[0]["Status"].AsString;
                    ValidRecordsDetailsModel validRecordsDetailModel = CommonUtility.GetModelDataPointdDetails(phoenixPredictionsInput.CorrelationId, appSettings);
                    if (validRecordsDetailModel != null)
                    {
                        if (validRecordsDetailModel.ValidRecordsDetails != null)
                        {
                            if (validRecordsDetailModel.ValidRecordsDetails.Records[0] >= 4 && validRecordsDetailModel.ValidRecordsDetails.Records[0] < 20)
                            {
                                phoenixPredictionsOutput.DataPointsWarning = CONSTANTS.DataPointsMinimum;
                                phoenixPredictionsOutput.DataPointsCount = validRecordsDetailModel.ValidRecordsDetails.Records[0];
                            }
                        }
                    }
                    phoenixPredictionsOutput.ErrorMessage = result[0]["ErrorMessage"].AsString;
                }
                else if (status == "E")
                {
                    phoenixPredictionsOutput.Status = result[0]["Status"].AsString;
                    phoenixPredictionsOutput.Progress = result[0]["Progress"].AsString;
                    phoenixPredictionsOutput.ErrorMessage = result[0]["ErrorMessage"].AsString;
                }
                else
                {
                    phoenixPredictionsOutput.Status = "I";
                    phoenixPredictionsOutput.Progress = result[0]["Progress"].AsString;
                    phoenixPredictionsOutput.ErrorMessage = "Prediction is in Progress";
                }

            }
            phoenixPredictionsOutput.AvailablePages = GetPublishModelPages(phoenixPredictionsInput.CorrelationId, phoenixPredictionsInput.UniqueId);
            if (ProblemType == CONSTANTS.TimeSeries)
            {
                List<string> availablePages = new List<string>();
                phoenixPredictionsOutput.PageNumber = string.Empty;
                phoenixPredictionsOutput.PageNumber = string.Empty;
                phoenixPredictionsOutput.AvailablePages = availablePages;
            }
            return phoenixPredictionsOutput;

        }
        public IngrainRequestQueue GetFileRequestStatus(string correlationId, string pageInfo)
        {
            IngrainRequestQueue ingrainRequest = new IngrainRequestQueue();
            var collection = _database.GetCollection<IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
            var builder = Builders<IngrainRequestQueue>.Filter;
            var filter = builder.Eq(CONSTANTS.CorrelationId, correlationId) & builder.Eq(CONSTANTS.pageInfo, pageInfo);
            var projection = Builders<IngrainRequestQueue>.Projection.Exclude(CONSTANTS.Id);
            return ingrainRequest = collection.Find(filter).Project<IngrainRequestQueue>(projection).ToList().FirstOrDefault();
        }
        private IngrainRequestQueue GetPredictionRequestStatus(string correlationId, string uniqueId, string pageInfo)
        {
            IngrainRequestQueue ingrainRequest = new IngrainRequestQueue();
            var collection = _database.GetCollection<IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
            var builder = Builders<IngrainRequestQueue>.Filter;
            var filter = builder.Eq(CONSTANTS.CorrelationId, correlationId) & builder.Eq("UniId", uniqueId) & builder.Eq(CONSTANTS.pageInfo, pageInfo);
            var projection = Builders<IngrainRequestQueue>.Projection.Exclude(CONSTANTS.Id);
            return ingrainRequest = collection.Find(filter).Project<IngrainRequestQueue>(projection).FirstOrDefault();
        }
        public IngrainRequestQueue GetFileRequestStatusByRequestId(string correlationId, string pageInfo, string requestId)
        {
            IngrainRequestQueue ingrainRequest = new IngrainRequestQueue();
            var collection = _database.GetCollection<IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
            var builder = Builders<IngrainRequestQueue>.Filter;
            var filter = builder.Eq(CONSTANTS.CorrelationId, correlationId) & builder.Eq(CONSTANTS.pageInfo, pageInfo) & builder.Eq("RequestId", requestId);
            var projection = Builders<IngrainRequestQueue>.Projection.Exclude(CONSTANTS.Id);
            return ingrainRequest = collection.Find(filter).Project<IngrainRequestQueue>(projection).ToList().FirstOrDefault();
        }
        public void SavePrediction(PredictionDTO predictionDTO)
        {
            var jsonData = JsonConvert.SerializeObject(predictionDTO);
            var insertDocument = BsonSerializer.Deserialize<BsonDocument>(jsonData);
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_PublishModel);

            collection.InsertOne(insertDocument);
        }
        private string GetActualData(string CorrelationId, string ApplicationName, string UsecaseName, GenericApiData genericApiData)
        {

            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(GetActualData), "Get Incremental data for prediction", string.IsNullOrEmpty(CorrelationId) ? default(Guid) : new Guid(CorrelationId), string.Empty, string.Empty, string.Empty, string.Empty);

            string resultString = string.Empty;
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.PS_IngestedData);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, CorrelationId);
            var Projection = Builders<BsonDocument>.Projection.Include("lastDateDict").Exclude("_id");
            var Ingestdata = collection.Find(filter).Project<BsonDocument>(Projection).ToList();

            if (Ingestdata.Count > 0)
            {
                var MappingCollection = _database.GetCollection<PublicTemplateMapping>(CONSTANTS.PublicTemplateMapping);
                var Builder = Builders<PublicTemplateMapping>.Filter;
                var Projection1 = Builders<PublicTemplateMapping>.Projection.Exclude("_id");
                var filter1 = Builder.Eq("ApplicationID", genericApiData.ApplicationID) & Builder.Eq("UsecaseID", genericApiData.UseCaseID);
                //var templatedata1 = MappingCollection.Find(filter1).Project<PublicTemplateMapping>(Projection1).FirstOrDefault();
                string columnToUpdate = string.Format("InputParameters", "dateColumn");
                var update = Builders<PublicTemplateMapping>.Update.Set(columnToUpdate, Ingestdata[0]["lastDateDict"][0][0].ToString());
                //var update = Builders<PublicTemplateMapping>.Update.Set("InputParameters.dateColumn", Ingestdata[0]["lastDateDict"][0][0].ToString());
                var isUpdated = MappingCollection.UpdateMany(filter1, update);

                var templatedata = MappingCollection.Find(filter1).Project<PublicTemplateMapping>(Projection1).FirstOrDefault();

                string token = CustomUrlToken(ApplicationName);
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(GetActualData), " CustomURL Token" + token, string.IsNullOrEmpty(CorrelationId) ? default(Guid) : new Guid(CorrelationId), string.Empty, string.Empty, string.Empty, string.Empty);
                if (token != null)
                {
                    if (genericApiData.DataSourceDetails != null)
                    {
                        genericApiData.DataSourceDetails.BodyParams.DateColumn = Ingestdata[0]["lastDateDict"][0][0].ToString();
                        //var RequestParameters = JsonConvert.SerializeObject(templatedata.InputParameters);
                        var RequestParameters = JsonConvert.SerializeObject(genericApiData.DataSourceDetails.BodyParams);
                        var client = new RestClient(genericApiData.DataSourceDetails.URL);
                        var request = new RestRequest(Method.POST);
                        request.AddHeader("cache-control", "no-cache");
                        request.AddHeader("content-type", "application/json");
                        request.AddHeader(CONSTANTS.Authorization, CONSTANTS.bearer + token);
                        request.AddParameter("application/json; charset=utf-8", RequestParameters,
                           ParameterType.RequestBody);
                        IRestResponse response = client.Execute(request);
                        resultString = response.Content;
                    }
                }
            }
            return resultString;
        }

        private string CustomUrlToken(string ApplicationName)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(CustomUrlToken), "CustomUrlToken for Application" + ApplicationName, string.Empty, string.Empty, string.Empty, string.Empty);
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
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(CustomUrlToken), "Application TOKEN PARAMS -- " + requestBuilder, string.Empty, string.Empty, string.Empty, string.Empty);
                IRestResponse response1 = client.Execute(request);
                string json1 = response1.Content;
                // Retrieve and Return the Access Token                
                var tokenObj = JsonConvert.DeserializeObject(json1) as dynamic;
                token = Convert.ToString(tokenObj.access_token);
                return token;
            }
            return token;
        }
        private void insertRequest(DeployModelsDto deployModels, string uniqueId, string Function)
        {
            bool DBEncryptionRequired = CommonUtility.EncryptDB(deployModels.CorrelationId, appSettings);
            if (DBEncryptionRequired)
            {
                try
                {
                    if (!string.IsNullOrEmpty(Convert.ToString(deployModels.CreatedByUser)))
                        deployModels.CreatedByUser = _encryptionDecryption.Decrypt(Convert.ToString(deployModels.CreatedByUser));
                }
                catch (Exception ex) { LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(insertRequest) + "User is already decrypted....", ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty); }
                try
                {
                    if (!string.IsNullOrEmpty(Convert.ToString(deployModels.ModifiedByUser)))
                        deployModels.ModifiedByUser = _encryptionDecryption.Decrypt(Convert.ToString(deployModels.ModifiedByUser));
                }
                catch (Exception ex) { LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(insertRequest) + "User is already decrypted....", ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty); }
            }
            IngrainRequestQueue ingrainRequest = new IngrainRequestQueue
            {
                _id = Guid.NewGuid().ToString(),
                CorrelationId = deployModels.CorrelationId,
                RequestId = Guid.NewGuid().ToString(),
                ProcessId = CONSTANTS.Null,
                Status = CONSTANTS.Null,
                ModelName = CONSTANTS.Null,
                RequestStatus = CONSTANTS.New,
                RetryCount = 0,
                ProblemType = CONSTANTS.Null,
                Message = CONSTANTS.Null,
                UniId = uniqueId,
                Progress = CONSTANTS.Null,
                pageInfo = Function, // pageInfo 
                ParamArgs = CONSTANTS.CurlyBraces,
                Function = Function,
                CreatedByUser = deployModels.CreatedByUser,
                TemplateUseCaseID = deployModels.UseCaseID,
                CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                ModifiedByUser = deployModels.ModifiedByUser,
                ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                LastProcessedOn = CONSTANTS.Null,
                AppID = deployModels.AppId
            };
            _ingestedDataService.InsertRequests(ingrainRequest);
            Thread.Sleep(2000);
        }

        private bool IsPredictedCompeted(PredictionDTO predictionDTO)
        {
            PredictionDTO predictionData = new PredictionDTO();
            bool IsPredicted = false;
            bool isPrediction = true;
            while (isPrediction)
            {
                predictionData = _deployedModelService.GetPrediction(predictionDTO);
                if (predictionData.Status == CONSTANTS.C)
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(GetPublicTemplatesModelsPredictions), CONSTANTS.APIResult + predictionData.Status, string.IsNullOrEmpty(predictionDTO.CorrelationId) ? default(Guid) : new Guid(predictionDTO.CorrelationId), predictionDTO.AppID, string.Empty, string.Empty, string.Empty);
                    _modelPrediction.PredictedData = predictionData.PredictedData;
                    _modelPrediction.Message = CONSTANTS.Success;
                    _modelPrediction.Status = predictionData.Status;
                    _modelPrediction.CorrelationId = predictionData.CorrelationId;
                    ValidRecordsDetailsModel validRecordsDetailModel = CommonUtility.GetModelDataPointdDetails(predictionDTO.CorrelationId, appSettings);
                    if (validRecordsDetailModel != null)
                    {
                        if (validRecordsDetailModel.ValidRecordsDetails != null)
                        {
                            if (validRecordsDetailModel.ValidRecordsDetails.Records[0] >= 4 && validRecordsDetailModel.ValidRecordsDetails.Records[0] < 20)
                            {
                                _modelPrediction.DataPointsWarning = CONSTANTS.DataPointsMinimum;
                                _modelPrediction.DataPointsCount = validRecordsDetailModel.ValidRecordsDetails.Records[0];
                            }
                        }
                    }
                    isPrediction = false;
                    IsPredicted = true;
                    return IsPredicted;
                }
                else if (predictionData.Status == CONSTANTS.E)
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(GetPublicTemplatesModelsPredictions), CONSTANTS.APIResult + predictionData.Status, string.IsNullOrEmpty(predictionDTO.CorrelationId) ? default(Guid) : new Guid(predictionDTO.CorrelationId), predictionDTO.AppID, string.Empty, string.Empty, string.Empty);
                    _modelPrediction.ErrorMessage = predictionData.ErrorMessage;
                    _modelPrediction.Status = predictionData.Status;
                    _modelPrediction.CorrelationId = predictionData.CorrelationId;
                    isPrediction = false;
                    IsPredicted = false;
                }
                else
                {
                    Thread.Sleep(1000);
                    isPrediction = true;
                }
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(IsPredictedCompeted), "ISPREDICTEDCOMPETED - END :" + JsonConvert.SerializeObject(predictionData), string.IsNullOrEmpty(predictionDTO.CorrelationId) ? default(Guid) : new Guid(predictionDTO.CorrelationId), predictionDTO.AppID, string.Empty, string.Empty, string.Empty);
            return IsPredicted;
        }


        public GenericDataResponse ModelReTraning(List<PrivateModelDetails> ModelData)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(ModelReTraning), "ModelReTraning - Started :", string.Empty, string.Empty, string.Empty, string.Empty);
            bool isdata = true;
            bool IsCascadeModelTemplate = false;
            if (ModelData.Count() > 0)
            {
                foreach (var model in ModelData)
                {
                    if (!(string.IsNullOrEmpty(model.ClientID)) && !(string.IsNullOrEmpty(model.DCID)) && !(string.IsNullOrEmpty(model.UserId)))
                    {

                        if (!CommonUtility.GetValidUser(model.UserId))
                        {
                            throw new Exception("UserName/UserId is Invalid");
                        }
                        CommonUtility.ValidateInputFormData(model.ModelName, "ModelName", false);
                        CommonUtility.ValidateInputFormData(model.ApplicationName, "ApplicationName", false);
                        CommonUtility.ValidateInputFormData(model.ClientID, "ClientID", true);
                        CommonUtility.ValidateInputFormData(model.DCID, "DCID", true);
                        CommonUtility.ValidateInputFormData(model.Status, "Status", false);
                        CommonUtility.ValidateInputFormData(model.ApplicationId, "ApplicationId", true);
                        CommonUtility.ValidateInputFormData(model.CorrelationId, "CorrelationId", true);
                        CommonUtility.ValidateInputFormData(model.DataSource, "DataSource", false);
                        CommonUtility.ValidateInputFormData(model.UsecaseID, "UsecaseID", true);
                        CommonUtility.ValidateInputFormData(Convert.ToString(model.DataSourceDetails?.Url), "DataSourceDetailsUrl", false);
                        CommonUtility.ValidateInputFormData(Convert.ToString(model.DataSourceDetails?.HttpMethod), "DataSourceDetailsHttpMethod", false);
                        CommonUtility.ValidateInputFormData(Convert.ToString(model.DataSourceDetails?.FetchType), "DataSourceDetailsFetchType", false);
                        CommonUtility.ValidateInputFormData(Convert.ToString(model.DataSourceDetails?.BatchSize), "DataSourceDetailsBatchSize", false);
                        CommonUtility.ValidateInputFormData(Convert.ToString(model.DataSourceDetails?.TotalNoOfRecords), "DataSourceDetailsTotalNoOfRecords", false);
                        CommonUtility.ValidateInputFormData(Convert.ToString(model.DataSourceDetails?.BodyParams), "DataSourceDetailsBodyParams", false);

                        var cascollection = _database.GetCollection<BsonDocument>(CONSTANTS.PublicTemplateMapping);
                        var ff = Builders<BsonDocument>.Filter.Eq(CONSTANTS.UsecaseID, model.UsecaseID);
                        var projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
                        var res = cascollection.Find(ff).Project<BsonDocument>(projection).ToList();
                        if (res.Count > 0)
                        {
                            BsonElement element;
                            var exists = res[0].TryGetElement("IsCascadeModelTemplate", out element);
                            if (exists)
                                IsCascadeModelTemplate = (bool)res[0]["IsCascadeModelTemplate"];
                            else
                                IsCascadeModelTemplate = false;
                        }


                        string NewModelCorrelationID = Guid.NewGuid().ToString();

                        var appCollection = _database.GetCollection<BsonDocument>(CONSTANTS.AppIntegration);
                        var filterBuilder = Builders<BsonDocument>.Filter;
                        var filter = filterBuilder.Eq("ApplicationID", model.ApplicationId);
                        var Projection = Builders<BsonDocument>.Projection.Include("ApplicationName").Exclude("_id");
                        var ApplicationResult = appCollection.Find(filter).Project<BsonDocument>(Projection).ToList();


                        bool DBEncryptionRequired = CommonUtility.EncryptDB(NewModelCorrelationID, appSettings);
                        string createdByUser = model.UserId;
                        if (DBEncryptionRequired)
                        {
                            if (!string.IsNullOrEmpty(Convert.ToString(model.UserId)))
                            {
                                createdByUser = _encryptionDecryption.Encrypt(Convert.ToString(model.UserId));
                            }
                        }
                        IngrainRequestQueue ingrainRequest = new IngrainRequestQueue
                        {
                            _id = Guid.NewGuid().ToString(),
                            CorrelationId = NewModelCorrelationID,
                            RequestId = Guid.NewGuid().ToString(),
                            ProcessId = CONSTANTS.Null,
                            RetryCount = 0,
                            ProblemType = CONSTANTS.Null,
                            UniId = CONSTANTS.Null,
                            InstaID = CONSTANTS.Null,
                            Progress = CONSTANTS.Null,
                            pageInfo = CONSTANTS.AutoTrain,
                            ParamArgs = CONSTANTS.Null,
                            TemplateUseCaseID = model.UsecaseID,
                            Function = CONSTANTS.AutoTrain,
                            CreatedByUser = createdByUser,
                            CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                            ModifiedByUser = CONSTANTS.Null,
                            ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                            LastProcessedOn = CONSTANTS.Null,
                            AppID = model.ApplicationId,
                            ClientId = model.ClientID,
                            DeliveryconstructId = model.DCID,
                            UseCaseID = CONSTANTS.Null,
                            EstimatedRunTime = CONSTANTS.Null
                        };
                        if (model.Status != "I")
                        {
                            ingrainRequest.ModelName = model.ModelName;
                        }
                        else
                        {
                            ingrainRequest.ModelName = model.ApplicationName + "_" + model.ModelName;
                        }
                        if (IsCascadeModelTemplate)
                        {
                            ingrainRequest.pageInfo = CONSTANTS.CascadingModel;
                            ingrainRequest.Function = CONSTANTS.CascadingModel;
                            ingrainRequest.ModelName = model.ApplicationName;
                            isdata = CheckRequiredCascadeDetails(model.ApplicationName, model.UsecaseID);
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(PublicTemplateModelTraning), "ModelReTrain Cascade Template function initiated", string.IsNullOrEmpty(ingrainRequest.CorrelationId) ? default(Guid) : new Guid(ingrainRequest.CorrelationId), string.Empty, string.Empty, string.Empty, string.Empty);
                        }
                        else
                        {
                            isdata = CheckRequiredDetails(model.ApplicationName, model.UsecaseID);
                        }

                        if (isdata)
                        {
                            ingrainRequest.Status = CONSTANTS.Null;
                            ingrainRequest.RequestStatus = CONSTANTS.New;
                            ingrainRequest.Message = CONSTANTS.Null;
                        }
                        else
                        {
                            ingrainRequest.Status = CONSTANTS.E;
                            ingrainRequest.RequestStatus = CONSTANTS.ErrorMessage;
                            ingrainRequest.Message = "The Model is created using File Upload. Please contact Ingrain support team to train it.";
                        }

                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(PublicTemplateModelTraning), "ModelReTrain function initiated", string.IsNullOrEmpty(ingrainRequest.CorrelationId) ? default(Guid) : new Guid(ingrainRequest.CorrelationId), string.Empty, string.Empty, string.Empty, string.Empty);
                        InsertRequests(ingrainRequest);
                        _GenericDataResponse.Message = CONSTANTS.TrainingResponse;
                        _GenericDataResponse.Status = CONSTANTS.C;
                        _GenericDataResponse.CorrelationId = ingrainRequest.CorrelationId;

                        if (model.CorrelationId != null)
                        {
                            var deloymodelCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_DeployedModels);
                            //var filterBuilder = Builders<BsonDocument>.Filter;
                            var filterQueue = filterBuilder.Eq("ClientUId", model.ClientID) & filterBuilder.Eq("DeliveryConstructUID", model.DCID) & filterBuilder.Eq("CorrelationId", model.CorrelationId) & filterBuilder.Eq("AppId", model.ApplicationId);
                            var queueResult = deloymodelCollection.Find(filterQueue).FirstOrDefault();
                            if (queueResult != null)
                            {
                                deloymodelCollection.DeleteMany(filterQueue);
                            }

                            var queueCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
                            var filterQueue1 = filterBuilder.Eq("CorrelationId", model.CorrelationId);
                            queueCollection.DeleteMany(filterQueue1);
                        }
                    }
                    else
                    {
                        _GenericDataResponse.Message = "Client Id, DCID and User id Values is Null. Please try again";
                        _GenericDataResponse.Status = CONSTANTS.I;
                    }

                }

            }
            else
            {
                _GenericDataResponse.Message = "Models not Trained";
                _GenericDataResponse.Status = CONSTANTS.I;
            }
            return _GenericDataResponse;
        }

        public GenericDataResponse TemplateModelTraining(List<TemplateTrainingInput> ModelData)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(TemplateModelTraining), "ModelReTraning - Started :", string.Empty, string.Empty, string.Empty, string.Empty);
            bool isdata = true;
            if (ModelData.Count() > 0)
            {
                foreach (var model in ModelData)
                {
                    if (!(string.IsNullOrEmpty(model.ClientId)) && !(string.IsNullOrEmpty(model.DeliveryConstructId)) && !(string.IsNullOrEmpty(model.UserId)))
                    {

                        CommonUtility.ValidateInputFormData(Convert.ToString(model.ApplicationId), "ApplicationId", true);
                        CommonUtility.ValidateInputFormData(Convert.ToString(model.ClientId), "ClientId", true);
                        CommonUtility.ValidateInputFormData(Convert.ToString(model.DeliveryConstructId), "DeliveryConstructId", true);
                        CommonUtility.ValidateInputFormData(Convert.ToString(model.UsecaseId), "UsecaseId", true);
                        CommonUtility.ValidateInputFormData(Convert.ToString(model.ModelName), "ModelName", false);
                        CommonUtility.ValidateInputFormData(Convert.ToString(model.DataSource), "DataSource", false);
                        CommonUtility.ValidateInputFormData(Convert.ToString(model.DataSourceDetails.Url), "DataSourceDetailsUrl", false);
                        CommonUtility.ValidateInputFormData(Convert.ToString(model.DataSourceDetails.HttpMethod), "DataSourceDetailsHttpMethod", false);
                        CommonUtility.ValidateInputFormData(Convert.ToString(model.DataSourceDetails.FetchType), "DataSourceDetailsFetchType", false);
                        CommonUtility.ValidateInputFormData(Convert.ToString(model.DataSourceDetails.BatchSize), "DataSourceDetailsBatchSize", false);
                        CommonUtility.ValidateInputFormData(Convert.ToString(model.DataSourceDetails.TotalNoOfRecords), "DataSourceDetailsTotalNoOfRecords", false);
                        CommonUtility.ValidateInputFormData(Convert.ToString(model.DataSourceDetails.BodyParams), "DataSourceDetailsBodyParams", false);

                        string NewModelCorrelationID = Guid.NewGuid().ToString();

                        var appCollection = _database.GetCollection<BsonDocument>(CONSTANTS.AppIntegration);
                        var filterBuilder = Builders<BsonDocument>.Filter;
                        var filter = filterBuilder.Eq("ApplicationID", model.ApplicationId);
                        var Projection = Builders<BsonDocument>.Projection.Include("ApplicationName").Exclude("_id");
                        var ApplicationResult = appCollection.Find(filter).Project<BsonDocument>(Projection).ToList();


                        bool DBEncryptionRequired = CommonUtility.EncryptDB(NewModelCorrelationID, appSettings);
                        string createdByUser = model.UserId;
                        if (DBEncryptionRequired)
                        {
                            if (!string.IsNullOrEmpty(Convert.ToString(model.UserId)))
                            {
                                createdByUser = _encryptionDecryption.Encrypt(Convert.ToString(model.UserId));
                            }
                        }
                        IngrainRequestQueue ingrainRequest = new IngrainRequestQueue
                        {
                            _id = Guid.NewGuid().ToString(),
                            CorrelationId = NewModelCorrelationID,
                            RequestId = Guid.NewGuid().ToString(),
                            ProcessId = CONSTANTS.Null,
                            // Status = CONSTANTS.Null,
                            // ModelName = ApplicationResult[0]["ApplicationName"].ToString() + "_" + model.ModelName,
                            // RequestStatus = CONSTANTS.New,
                            RetryCount = 0,
                            ProblemType = CONSTANTS.Null,
                            // Message = CONSTANTS.Null,
                            UniId = CONSTANTS.Null,
                            InstaID = CONSTANTS.Null,
                            Progress = CONSTANTS.Null,
                            pageInfo = CONSTANTS.AutoTrain,
                            ParamArgs = CONSTANTS.Null,
                            TemplateUseCaseID = model.UsecaseId,
                            Function = CONSTANTS.AutoTrain,
                            CreatedByUser = createdByUser,
                            CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                            ModifiedByUser = CONSTANTS.Null,
                            ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                            LastProcessedOn = CONSTANTS.Null,
                            AppID = model.ApplicationId,
                            ClientId = model.ClientId,
                            DeliveryconstructId = model.DeliveryConstructId,
                            UseCaseID = CONSTANTS.Null,
                            // DataSource=CONSTANTS.Null,
                            EstimatedRunTime = CONSTANTS.Null
                        };

                        ingrainRequest.ModelName = ApplicationResult[0]["ApplicationName"].ToString() + "_" + model.ModelName;
                        isdata = CheckRequiredDetails(ApplicationResult[0]["ApplicationName"].ToString(), model.UsecaseId);
                        if (isdata)
                        {
                            ingrainRequest.Status = CONSTANTS.Null;
                            ingrainRequest.RequestStatus = CONSTANTS.New;
                            ingrainRequest.Message = CONSTANTS.Null;
                        }
                        else
                        {
                            ingrainRequest.Status = CONSTANTS.E;
                            ingrainRequest.RequestStatus = CONSTANTS.ErrorMessage;
                            ingrainRequest.Message = "The Model is created using File Upload. Please contact Ingrain support team to train it.";
                        }

                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(PublicTemplateModelTraning), "ModelReTrain function initiated", string.IsNullOrEmpty(ingrainRequest.CorrelationId) ? default(Guid) : new Guid(ingrainRequest.CorrelationId), ingrainRequest.AppID, string.Empty, ingrainRequest.ClientID, ingrainRequest.DCID);
                        InsertRequests(ingrainRequest);
                        _GenericDataResponse.Message = CONSTANTS.TrainingResponse;
                        _GenericDataResponse.Status = CONSTANTS.I;
                        _GenericDataResponse.CorrelationId = ingrainRequest.CorrelationId;
                        IngrainResponseData CallBackResponse = new IngrainResponseData
                        {
                            CorrelationId = ingrainRequest.CorrelationId,
                            Status = CONSTANTS.I,
                            Message = CONSTANTS.TrainingResponse,
                            ErrorMessage = string.Empty
                        };
                        CallBackErrorLog.CorrelationId = CallBackResponse.CorrelationId;
                        CallBackErrorLog.Status = CallBackResponse.Status;
                        CallBackErrorLog.Message = CallBackResponse.Message;
                        CallBackErrorLog.ErrorMessage = CallBackResponse.ErrorMessage;
                        CallBackErrorLog.RequestId = ingrainRequest.RequestId;
                        CallBackErrorLog.CreatedBy = CONSTANTS.System;
                        CallBackErrorLog.ProcessName = CONSTANTS.TrainingName;
                        CallBackErrorLog.UsageType = CONSTANTS.INFOUsage;
                        CallBackErrorLog.ClientId = ingrainRequest.ClientId;
                        CallBackErrorLog.DCID = ingrainRequest.DeliveryconstructId;
                        // CallBackErrorLog.BaseAddress = trainingRequestDetails.IngrainTrainingResponseCallBackUrl;
                        CallBackErrorLog.ApplicationID = model.ApplicationId;
                        // CallBackErrorLog.ApplicationName = publicTemplateQueueResult.ApplicationName;
                        CallBackErrorLog.UseCaseId = model.UsecaseId;
                        AuditTrailLog(CallBackErrorLog);
                        // CallBackErrorLog(CallBackResponse, null, null, null, ingrainRequest.ClientId, ingrainRequest.DeliveryconstructId, model.ApplicationId, model.UsecaseId, ingrainRequest.RequestId, CONSTANTS.System, "Training");

                    }
                    else
                    {
                        _GenericDataResponse.Message = "Client Id, DCID and User id Values is Null. Please try again";
                        _GenericDataResponse.Status = CONSTANTS.E;
                        IngrainResponseData CallBackResponse = new IngrainResponseData
                        {
                            CorrelationId = null,
                            Status = CONSTANTS.I,
                            Message = CONSTANTS.TrainingResponse,
                            ErrorMessage = string.Empty
                        };
                        CallBackErrorLog.CorrelationId = CallBackResponse.CorrelationId;
                        CallBackErrorLog.Status = CallBackResponse.Status;
                        CallBackErrorLog.Message = CallBackResponse.Message;
                        CallBackErrorLog.ErrorMessage = CallBackResponse.ErrorMessage;
                        // CallBackErrorLog.RequestId = ingrainRequest.RequestId;
                        CallBackErrorLog.CreatedBy = CONSTANTS.System;
                        CallBackErrorLog.ProcessName = "Training";
                        CallBackErrorLog.UsageType = "INFO";
                        CallBackErrorLog.ClientId = model.ClientId;
                        CallBackErrorLog.DCID = model.DeliveryConstructId;
                        // CallBackErrorLog.BaseAddress = trainingRequestDetails.IngrainTrainingResponseCallBackUrl;
                        CallBackErrorLog.ApplicationID = model.ApplicationId;
                        // CallBackErrorLog.ApplicationName = publicTemplateQueueResult.ApplicationName;
                        CallBackErrorLog.UseCaseId = model.UsecaseId;
                        AuditTrailLog(CallBackErrorLog);
                        // CallBackErrorLog(CallBackResponse, null, null, null, model.ClientId, model.DeliveryConstructId, model.ApplicationId, model.UsecaseId, null, CONSTANTS.System, "Training");
                    }

                }

            }
            else
            {
                _GenericDataResponse.Message = "Models not Trained";
                _GenericDataResponse.Status = CONSTANTS.E;
            }
            return _GenericDataResponse;
        }

        public string GetGenericModelStatus(string clientId, string dcId, string applicationId, string usecaseId)
        {
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
            var filterBuilder = Builders<BsonDocument>.Filter;
            //var filterQueue = filterBuilder.Eq("ClientId", clientId) & filterBuilder.Eq("DeliveryconstructId", dcId) & filterBuilder.Eq("TemplateUseCaseID", usecaseId) & filterBuilder.Eq("AppID", applicationId);
            var filterQueue = filterBuilder.Eq("TemplateUseCaseID", usecaseId) & filterBuilder.Eq("AppID", applicationId);
            var Projection = Builders<BsonDocument>.Projection.Exclude("_id");
            var result = collection.Find(filterQueue).Project<BsonDocument>(Projection).FirstOrDefault();
            if (result != null)
            {
                return result["CorrelationId"].ToString();
            }
            else
            {
                return null;
            }
        }
        private ModelTrainingStatus GetSPAModelStatus(string clientId, string dcId, string applicationId, string usecaseId)
        {
            ModelTrainingStatus trainingStatus = new ModelTrainingStatus();
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
            var filterBuilder = Builders<BsonDocument>.Filter;
            var filterQueue = filterBuilder.Eq("ClientId", clientId) & filterBuilder.Eq("DeliveryconstructId", dcId) & filterBuilder.Eq("TemplateUseCaseID", usecaseId) & filterBuilder.Eq("AppID", applicationId);
            var Projection = Builders<BsonDocument>.Projection.Exclude("_id");
            var result = collection.Find(filterQueue).Project<BsonDocument>(Projection).ToList();
            if (result.Count > 0)
            {
                for (int i = 0; i < result.Count; i++)
                {
                    if (result[i][CONSTANTS.Status].ToString() == CONSTANTS.E || result[i][CONSTANTS.Status].ToString() == CONSTANTS.I || result[i][CONSTANTS.Status].ToString() == CONSTANTS.M)
                    {
                        var queueCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
                        var filterBuilder1 = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, result[i][CONSTANTS.CorrelationId].ToString());
                        queueCollection.DeleteMany(filterBuilder1);
                        trainingStatus.CorrelationId = result[i][CONSTANTS.CorrelationId].ToString();
                        trainingStatus.PageInfo = result[i][CONSTANTS.pageInfo].ToString();
                        trainingStatus.Status = result[i][CONSTANTS.Status].ToString();
                        trainingStatus.ErrorMessage = result[i][CONSTANTS.Message].ToString();
                        return trainingStatus;
                    }
                    if (result[i][CONSTANTS.Status].ToString() == CONSTANTS.P || result[i][CONSTANTS.Status].ToString() == CONSTANTS.I)
                    {
                        trainingStatus.CorrelationId = result[i][CONSTANTS.CorrelationId].ToString();
                        trainingStatus.PageInfo = result[i][CONSTANTS.pageInfo].ToString();
                        trainingStatus.Status = result[i][CONSTANTS.Status].ToString();
                        trainingStatus.ErrorMessage = result[i][CONSTANTS.Message].ToString();
                        return trainingStatus;
                    }
                }
                var collection2 = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
                var filterBuilder2 = Builders<BsonDocument>.Filter;
                var filterQueue2 = filterBuilder2.Eq(CONSTANTS.CorrelationId, result[0][CONSTANTS.CorrelationId].ToString()) & filterBuilder2.Eq(CONSTANTS.Status, CONSTANTS.Deployed);
                var deployResult = collection2.Find(filterQueue2).ToList();
                if (deployResult.Count > 0)
                {
                    if (deployResult[0][CONSTANTS.Status] == CONSTANTS.Deployed)
                    {
                        trainingStatus.CorrelationId = deployResult[0][CONSTANTS.CorrelationId].ToString();
                        trainingStatus.Status = CONSTANTS.C;
                    }
                    else
                    {
                        trainingStatus.CorrelationId = deployResult[0][CONSTANTS.CorrelationId].ToString();
                        trainingStatus.Status = CONSTANTS.P;
                    }
                    return trainingStatus;
                }
                else
                {
                    return trainingStatus;
                }
            }
            else
                return trainingStatus;
        }
        public string UpdateAppIntegration(AppIntegration appIntegrations)
        {
            var collection = _database.GetCollection<AppIntegration>(CONSTANTS.AppIntegration);
            var filter = Builders<AppIntegration>.Filter.Where(x => x.ApplicationID == appIntegrations.ApplicationID);
            var Projection = Builders<AppIntegration>.Projection.Exclude("_id");
            var result = collection.Find(filter).Project<AppIntegration>(Projection).FirstOrDefault();

            var status = CONSTANTS.NoRecordsFound;
            if (result != null)
            {
                var update = Builders<AppIntegration>.Update
                    .Set(x => x.Authentication, appSettings.Value.authProvider)
                    .Set(x => x.TokenGenerationURL, _encryptionDecryption.Encrypt(appSettings.Value.token_Url))
                    .Set(x => x.Credentials, (IEnumerable)(_encryptionDecryption.Encrypt(appIntegrations.Credentials)))
                    .Set(x => x.ModifiedByUser, _encryptionDecryption.Encrypt(Convert.ToString(appSettings.Value.username)))
                    .Set(x => x.ModifiedOn, DateTime.UtcNow.ToString());
                collection.UpdateOne(filter, update);
                status = CONSTANTS.Success;
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(UpdateAppIntegration), "UpdateAppIntegration - status : " + status, appIntegrations.ApplicationID, string.Empty, appIntegrations.clientUId, appIntegrations.deliveryConstructUID);
            return status;
        }

        public string UpdatePublicTemplateMapping(PublicTemplateMapping templateMapping)
        {
            var collection = _database.GetCollection<PublicTemplateMapping>(CONSTANTS.PublicTemplateMapping);
            var filter = Builders<PublicTemplateMapping>.Filter.Where(x => x.ApplicationID == templateMapping.ApplicationID)
                & Builders<PublicTemplateMapping>.Filter.Where(x => x.UsecaseID == templateMapping.UsecaseID);
            var Projection = Builders<PublicTemplateMapping>.Projection.Exclude("_id");
            var result = collection.Find(filter).Project<PublicTemplateMapping>(Projection).FirstOrDefault();
            var status = CONSTANTS.NoRecordsFound;
            if (result != null)
            {
                var update = Builders<PublicTemplateMapping>.Update
                    .Set(x => x.SourceName, templateMapping.SourceName)
                    .Set(x => x.SourceURL, templateMapping.SourceURL)
                    .Set(x => x.InputParameters, (IEnumerable)(_encryptionDecryption.Encrypt(templateMapping.InputParameters)))
                    .Set(x => x.ModifiedByUser, _encryptionDecryption.Encrypt(Convert.ToString(appSettings.Value.username)))
                    .Set(x => x.ModifiedOn, DateTime.UtcNow.ToString())
                    .Set(x => x.IterationUID, templateMapping.IterationUID);
                collection.UpdateOne(filter, update);
                status = CONSTANTS.Success;
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(UpdatePublicTemplateMapping), "UpdatePublicTemplateMapping - status : " + status, templateMapping.ApplicationID, string.Empty, string.Empty, string.Empty);
            return status;
        }
        public string UpdatePublicTemplateMappingWithoutEncryption(PublicTemplateMapping templateMapping)
        {
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.PublicTemplateMapping);
            var filter = Builders<BsonDocument>.Filter.Eq("ApplicationID", templateMapping.ApplicationID)
                & Builders<BsonDocument>.Filter.Eq("UsecaseID", templateMapping.UsecaseID);
            var Projection = Builders<BsonDocument>.Projection.Exclude("_id");
            var result = collection.Find(filter).Project<BsonDocument>(Projection).FirstOrDefault();
            var status = CONSTANTS.NoRecordsFound;
            if (result != null)
            {
                BsonDocument bsons = BsonDocument.Parse(templateMapping.InputParameters);
                var update = Builders<BsonDocument>.Update
                    .Set("SourceName", templateMapping.SourceName)
                    .Set("SourceURL", templateMapping.SourceURL)
                    .Set("InputParameters", bsons)
                    .Set("ModifiedByUser", appSettings.Value.username)
                    .Set("ModifiedOn", DateTime.UtcNow.ToString());
                collection.UpdateOne(filter, update);
                status = CONSTANTS.Success;
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(IngrainGenericTrainingRequest), "UpdatePublicTemplateMapping - status : " + status, templateMapping.ApplicationID, string.Empty, string.Empty, string.Empty);
            return status;
        }

        public void IngrainGenericDeleteOldRecordsOnRetraining(TrainingRequestDetails trainingRequestDetails, string correlationId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice)
                                                     , nameof(IngrainGenericDeleteOldRecordsOnRetraining)
                                                     , "Deleting correlation Id Details START : " + correlationId, trainingRequestDetails.ApplicationId, string.Empty, string.Empty, string.Empty);
            try
            {
                _flushService.FlushModelSPP(correlationId, "");
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(GenericSelfservice)
                                                          , nameof(IngrainGenericDeleteOldRecordsOnRetraining)
                                                          , ex.Message
                                                          , ex, trainingRequestDetails.ApplicationId, string.Empty, string.Empty, string.Empty);
            }

            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice)
                                                     , nameof(IngrainGenericDeleteOldRecordsOnRetraining)
                                                     , "Deleting correlation Id Details END: " + correlationId, trainingRequestDetails.ApplicationId, string.Empty, string.Empty, string.Empty);

        }

        private ParamArgsCustomMultipleFetch CreateParamArgsCustomMultipleFetch(TrainingRequestDetails trainingRequestDetails, string correlationId)
        {
            CustomMultipleFetch customMultipleFetch = new CustomMultipleFetch()
            {
                ApplicationId = trainingRequestDetails.ApplicationId,
                AppServiceUId = appSettings.Value.AppServiceUID,
                DataSource = trainingRequestDetails.DataSource,
                HttpMethod = trainingRequestDetails.DataSourceDetails.HttpMethod,
                Url = trainingRequestDetails.DataSourceDetails.Url,
                FetchType = trainingRequestDetails.DataSourceDetails.FetchType,
                BatchSize = trainingRequestDetails.DataSourceDetails.BatchSize,
                TotalNoOfRecords = trainingRequestDetails.DataSourceDetails.TotalNoOfRecords,
                BodyParams = trainingRequestDetails.DataSourceDetails.BodyParams,
                Data = appSettings.Value.DBEncryption ? _encryptionDecryption.Encrypt(trainingRequestDetails.Data) : trainingRequestDetails.Data,
                DataFlag = trainingRequestDetails.DataFlag
            };

            Parent parent = new Parent()
            {
                Type = CONSTANTS.Null,
                Name = CONSTANTS.Null
            };

            Fileupload fileupload = new Fileupload()
            {
                fileList = CONSTANTS.Null
            };

            ParamArgsCustomMultipleFetch paramArgsCustomMultipleFetch = new ParamArgsCustomMultipleFetch()
            {
                Customdetails = CONSTANTS.Null,
                CorrelationId = correlationId,
                ClientUID = trainingRequestDetails.ClientUId,
                DeliveryConstructUId = trainingRequestDetails.DeliveryConstructUId,
                Parent = parent,
                Flag = CONSTANTS.Null,
                mapping = CONSTANTS.Null,
                mapping_flag = "False",
                pad = CONSTANTS.Null,
                metric = CONSTANTS.Null,
                InstaMl = CONSTANTS.Null,
                fileupload = fileupload,
                CustomMultipleFetch = customMultipleFetch
            };

            return paramArgsCustomMultipleFetch;
        }

        private void IngrainGenericModelInsertTrainingRequest(TrainingRequestDetails trainingRequestDetails, string status)
        {
            bool isRetraining = !string.IsNullOrEmpty(trainingRequestDetails.CorrelationId);
            string correlationId = string.IsNullOrEmpty(trainingRequestDetails.CorrelationId) ? Guid.NewGuid().ToString() : trainingRequestDetails.CorrelationId;
            trainingRequestDetails.DataFlag = string.IsNullOrEmpty(trainingRequestDetails.DataFlag) ? "FullDump" : trainingRequestDetails.DataFlag;
            if (!string.IsNullOrEmpty(trainingRequestDetails.CorrelationId))
            {
                if (status != CONSTANTS.E && status != null && status != CONSTANTS.BsonNull)
                {
                    bool Clientvalid = checkClient(trainingRequestDetails.CorrelationId, trainingRequestDetails.ClientUId, trainingRequestDetails.DeliveryConstructUId);
                    if (!Clientvalid)
                    {
                        _GenericDataResponse.ErrorMessage = "Enter valid ClientUId and DeliveryConstructUId";
                        _GenericDataResponse.Status = CONSTANTS.E;
                        return;
                    }
                }
            }
            if (isRetraining)
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice)
                                                         , nameof(IngrainGenericModelInsertTrainingRequest)
                                                         , "ReTraining correlationId - " + correlationId, trainingRequestDetails.ApplicationId, string.Empty, string.Empty, string.Empty);
                if (trainingRequestDetails.DataFlag == "FullDump")
                    IngrainGenericDeleteOldRecordsOnRetraining(trainingRequestDetails, correlationId);
            }
            if (trainingRequestDetails.DataFlag == "Incremental")
            {
                if (string.IsNullOrEmpty(trainingRequestDetails.CorrelationId))
                {
                    _GenericDataResponse.ErrorMessage = "CorrelationId cannot be empty for Incremental DataFlag";
                    _GenericDataResponse.Status = CONSTANTS.E;
                    return;
                }
                else
                    this.BackUpModelSPP(correlationId, "Incremental");
            }

            var publicTemplateCollection = _database.GetCollection<PublicTemplateMapping>(CONSTANTS.PublicTemplateMapping);
            var publicTemplateFilterBuilder = Builders<PublicTemplateMapping>.Filter;
            var publicTemplateFilterQueue = publicTemplateFilterBuilder.Where(x => x.ApplicationID == trainingRequestDetails.ApplicationId)
                                            & publicTemplateFilterBuilder.Where(x => x.UsecaseID == trainingRequestDetails.UseCaseId);
            var Projection = Builders<PublicTemplateMapping>.Projection.Exclude("_id");
            var publicTemplateQueueResult = publicTemplateCollection.Find(publicTemplateFilterQueue).Project<PublicTemplateMapping>(Projection).FirstOrDefault();

            ParamArgsCustomMultipleFetch paramArgsCustomMultipleFetch = CreateParamArgsCustomMultipleFetch(trainingRequestDetails, correlationId);

            bool DBEncryptionRequired = CommonUtility.EncryptDB(correlationId, appSettings);
            string createdByUser = trainingRequestDetails.UserId;
            //if (DBEncryptionRequired)
            {
                if (!string.IsNullOrEmpty(Convert.ToString(trainingRequestDetails.UserId)))
                {
                    createdByUser = _encryptionDecryption.Encrypt(Convert.ToString(trainingRequestDetails.UserId));
                }
            }
            IngrainRequestQueue ingrainRequest = new IngrainRequestQueue
            {
                _id = Guid.NewGuid().ToString(),
                CorrelationId = correlationId,
                RequestId = Guid.NewGuid().ToString(),
                ProcessId = CONSTANTS.Null,
                ApplicationName = publicTemplateQueueResult.ApplicationName,
                //Status = CONSTANTS.Null,
                ModelName = correlationId + "_" + publicTemplateQueueResult.UsecaseName,
                //RequestStatus = CONSTANTS.New,
                RetryCount = 0,
                ProblemType = CONSTANTS.Null,
                //Message = CONSTANTS.Null,
                UniId = CONSTANTS.Null,
                InstaID = CONSTANTS.Null,
                Progress = CONSTANTS.Null,
                pageInfo = CONSTANTS.AutoTrain,
                ParamArgs = JsonConvert.SerializeObject(paramArgsCustomMultipleFetch),
                TemplateUseCaseID = publicTemplateQueueResult.UsecaseID,
                Function = CONSTANTS.AutoTrain,
                CreatedByUser = createdByUser,
                CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                ModifiedByUser = CONSTANTS.Null,
                ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                LastProcessedOn = CONSTANTS.Null,
                AppID = publicTemplateQueueResult.ApplicationID,
                ClientId = trainingRequestDetails.ClientUId,
                DeliveryconstructId = trainingRequestDetails.DeliveryConstructUId,
                UseCaseID = trainingRequestDetails.UseCaseId,
                //DataSource = CONSTANTS.Null,
                EstimatedRunTime = CONSTANTS.Null,
                AppURL = trainingRequestDetails.IngrainTrainingResponseCallBackUrl
            };
            bool isdata = CheckRequiredDetails(publicTemplateQueueResult.ApplicationName, publicTemplateQueueResult.UsecaseID);
            if (isdata)
            {
                ingrainRequest.Status = CONSTANTS.Null;
                ingrainRequest.RequestStatus = CONSTANTS.New;
                ingrainRequest.Message = CONSTANTS.Null;
            }
            else
            {
                ingrainRequest.Status = CONSTANTS.E;
                ingrainRequest.RequestStatus = CONSTANTS.ErrorMessage;
                ingrainRequest.Message = CONSTANTS.ModelisCreatedbyFileUpload;
            }

            if (isRetraining)
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice)
                                                         , nameof(IngrainGenericModelInsertTrainingRequest)
                                                         , "ModelReTrain function initiated", string.IsNullOrEmpty(trainingRequestDetails.CorrelationId) ? default(Guid) : new Guid(trainingRequestDetails.CorrelationId), trainingRequestDetails.ApplicationId, string.Empty, string.Empty, string.Empty);
            else
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice)
                                                         , nameof(IngrainGenericModelInsertTrainingRequest)
                                                         , "Auto Train function initiated TemplateID:" + publicTemplateQueueResult.UsecaseID + "  NewCorrelationID :" + correlationId + " AppName" + publicTemplateQueueResult.ApplicationName + " UsecaseName " + publicTemplateQueueResult.UsecaseName, trainingRequestDetails.ApplicationId, string.Empty, string.Empty, string.Empty);

            InsertRequests(ingrainRequest);
            if (!string.IsNullOrEmpty(trainingRequestDetails.IngrainTrainingResponseCallBackUrl))
            {
                IngrainResponseData CallBackResponse = new IngrainResponseData
                {
                    CorrelationId = ingrainRequest.CorrelationId,
                    Status = CONSTANTS.I,
                    Message = CONSTANTS.TrainingInitiated,
                    ErrorMessage = string.Empty,
                };
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(InitiateTraining), "Call Back URL STARTING--URL--  :" + trainingRequestDetails.IngrainTrainingResponseCallBackUrl, string.IsNullOrEmpty(trainingRequestDetails.CorrelationId) ? default(Guid) : new Guid(trainingRequestDetails.CorrelationId), trainingRequestDetails.ApplicationId, string.Empty, string.Empty, string.Empty);
                string callbackResonse = CallbackResponse(CallBackResponse, publicTemplateQueueResult.ApplicationName, trainingRequestDetails.IngrainTrainingResponseCallBackUrl, ingrainRequest.ClientID, ingrainRequest.DCID, ingrainRequest.AppID, ingrainRequest.TemplateUseCaseID, ingrainRequest.RequestId, ingrainRequest.CreatedByUser);
                if (callbackResonse == CONSTANTS.ErrorMessage)
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(InitiateTraining), "Call Back URL Token Response :" + callbackResonse, string.IsNullOrEmpty(trainingRequestDetails.CorrelationId) ? default(Guid) : new Guid(trainingRequestDetails.CorrelationId), trainingRequestDetails.ApplicationId, string.Empty, string.Empty, string.Empty);
                    _GenericDataResponse.ErrorMessage = CONSTANTS.TokenFailed;
                    _GenericDataResponse.Status = CONSTANTS.E;
                    _GenericDataResponse.CorrelationId = correlationId;
                }
            }
            else
            {
                IngrainResponseData CallBackResponse = new IngrainResponseData
                {
                    CorrelationId = ingrainRequest.CorrelationId,
                    Status = CONSTANTS.I,
                    Message = CONSTANTS.TrainingInitiated,
                    ErrorMessage = string.Empty
                };
                CallBackErrorLog.CorrelationId = CallBackResponse.CorrelationId;
                CallBackErrorLog.Status = CallBackResponse.Status;
                CallBackErrorLog.Message = CallBackResponse.Message;
                CallBackErrorLog.ErrorMessage = CallBackResponse.ErrorMessage;
                CallBackErrorLog.RequestId = ingrainRequest.RequestId;
                CallBackErrorLog.CreatedBy = ingrainRequest.CreatedByUser;
                CallBackErrorLog.ProcessName = "Training";
                CallBackErrorLog.UsageType = "INFO";
                CallBackErrorLog.ClientId = ingrainRequest.ClientID;
                CallBackErrorLog.DCID = ingrainRequest.DeliveryconstructId;
                CallBackErrorLog.BaseAddress = trainingRequestDetails.IngrainTrainingResponseCallBackUrl;
                CallBackErrorLog.ApplicationID = ingrainRequest.AppID;
                CallBackErrorLog.ApplicationName = publicTemplateQueueResult.ApplicationName;
                CallBackErrorLog.UseCaseId = ingrainRequest.TemplateUseCaseID;
                AuditTrailLog(CallBackErrorLog);
                // CallBackErrorLog(CallBackResponse, publicTemplateQueueResult.ApplicationName, trainingRequestDetails.IngrainTrainingResponseCallBackUrl, null, ingrainRequest.ClientID, ingrainRequest.DeliveryconstructId, ingrainRequest.AppID, ingrainRequest.TemplateUseCaseID, ingrainRequest.RequestId, ingrainRequest.CreatedByUser, "Training");
            }
            _GenericDataResponse.Message = CONSTANTS.TrainingResponse;
            _GenericDataResponse.Status = CONSTANTS.I;
            _GenericDataResponse.Progress = "0%";
            _GenericDataResponse.CorrelationId = correlationId;
        }

        private bool checkClient(string correlationId, string clientUId, string deliveryConstructUId)
        {
            bool checkClient = false;
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.ClientUId).Include(CONSTANTS.DeliveryConstructUID).Include(CONSTANTS.CorrelationId).Exclude("_id");
            var result = collection.Find(filter).Project<BsonDocument>(projection).FirstOrDefault();
            if (result != null)
            {
                if (result[CONSTANTS.ClientUId].ToString().Trim() == clientUId.Trim() && result[CONSTANTS.DeliveryConstructUID].ToString().Trim() == deliveryConstructUId.Trim())
                    checkClient = true;
            }
            return checkClient;
        }

        private void BackUpModelSPP(string CorrelationId, string dataFlag)
        {
            if (dataFlag == "Incremental")
            {
                var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, CorrelationId);
                List<string> collectionNames = new List<string>();
                collectionNames.Add(CONSTANTS.PSIngestedData);
                collectionNames.Add(CONSTANTS.PSBusinessProblem);
                collectionNames.Add(CONSTANTS.DEDataCleanup);
                collectionNames.Add(CONSTANTS.DEDataProcessing);
                //collectionNames.Add(CONSTANTS.DeployedPublishModel);
                collectionNames.Add(CONSTANTS.IngrainDeliveryConstruct);
                collectionNames.Add(CONSTANTS.ME_HyperTuneVersion);
                collectionNames.Add(CONSTANTS.SSAIRecommendedTrainedModels);
                //collectionNames.Add(CONSTANTS.SSAIUserDetails);
                collectionNames.Add(CONSTANTS.WF_IngestedData);
                collectionNames.Add(CONSTANTS.WF_TestResults_);
                collectionNames.Add(CONSTANTS.WhatIfAnalysis);
                // collectionNames.Add(CONSTANTS.DE_DataVisualization);
                collectionNames.Add(CONSTANTS.DEPreProcessedData);
                collectionNames.Add(CONSTANTS.DataCleanUPFilteredData);
                collectionNames.Add(CONSTANTS.MEFeatureSelection);
                collectionNames.Add(CONSTANTS.MERecommendedModels);
                collectionNames.Add(CONSTANTS.ME_TeachAndTest);
                collectionNames.Add(CONSTANTS.SSAIDeployedModels);
                collectionNames.Add(CONSTANTS.SSAIUseCase);
                collectionNames.Add(CONSTANTS.SSAIIngrainRequests);

                foreach (string name in collectionNames)
                {
                    if (name == CONSTANTS.PSIngestedData)
                        Insert_backUP(CorrelationId);
                    else
                    {
                        var collection = _database.GetCollection<BsonDocument>(name);
                        if (collection.Find(filter).ToList().Count > 0)
                        {
                            var update = Builders<BsonDocument>.Update.Set(CONSTANTS.CorrelationId, CorrelationId + "_backUp");
                            collection.UpdateMany(filter, update);
                        }
                    }
                }

            }
        }
        private void Insert_backUP(string CorrelationId)
        {
            string newCorrId = CorrelationId + "_backUp";
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.PSIngestedData);
            var projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, CorrelationId);
            var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();

            if (result.Count > 0)
            {
                for (int z = 0; z < result.Count; z++)
                {
                    JObject data = new JObject();
                    data = JObject.Parse(result[z].ToString());
                    if (data.ContainsKey(CONSTANTS.CorrelationId))
                    {
                        data[CONSTANTS.CorrelationId] = newCorrId;
                        data.Add(CONSTANTS.Id, Guid.NewGuid().ToString()); //To avoid inserting ObjectId
                    }
                    var jsonColumns = Newtonsoft.Json.JsonConvert.SerializeObject(data);
                    var insertBsonColumns = BsonSerializer.Deserialize<BsonDocument>(jsonColumns);
                    collection.InsertOne(insertBsonColumns);
                }
            }
        }

        public GenericDataResponse IngrainGenericModelTrainingRequest(TrainingRequestDetails trainingRequestDetails)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice)
                                                     , nameof(IngrainGenericModelTrainingRequest)
                                                     , "IngrainGenericModelTrainingRequest - Started :", trainingRequestDetails.ApplicationId, string.Empty, string.Empty, string.Empty);
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice)
                                                    , nameof(IngrainGenericModelTrainingRequest)
                                                    , "ClientID: " + trainingRequestDetails.ClientUId + ", DCID: " + trainingRequestDetails.DeliveryConstructUId + ", UserId: " + trainingRequestDetails.UserId, string.IsNullOrEmpty(trainingRequestDetails.CorrelationId) ? default(Guid) : new Guid(trainingRequestDetails.CorrelationId), trainingRequestDetails.ApplicationId, string.Empty, string.Empty, string.Empty);
            _trainingRequestDetails = trainingRequestDetails;
            if (string.IsNullOrEmpty(trainingRequestDetails.CorrelationId))
            {
                IngrainGenericModelInsertTrainingRequest(trainingRequestDetails, null);
            }
            else
            {
                var ingrainRequestCollection = _database.GetCollection<IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
                var ingrainRequestFilterBuilder = Builders<IngrainRequestQueue>.Filter;
                var ingrainRequestFilterQueue = ingrainRequestFilterBuilder.Where(x => x.CorrelationId == trainingRequestDetails.CorrelationId)
                                                & ingrainRequestFilterBuilder.Where(x => x.Function == CONSTANTS.AutoTrain);
                var Projection1 = Builders<IngrainRequestQueue>.Projection.Exclude("_id");
                var ingrainRequestQueueResult = ingrainRequestCollection.Find(ingrainRequestFilterQueue).Project<IngrainRequestQueue>(Projection1).FirstOrDefault();
                if (ingrainRequestQueueResult != null)
                {
                    if (ingrainRequestQueueResult.Status == CONSTANTS.C || ingrainRequestQueueResult.Status == CONSTANTS.E)
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice)
                                                                 , nameof(IngrainGenericModelTrainingRequest)
                                                                 , "ReTraining Request for correlationId - " + trainingRequestDetails.CorrelationId + ",Old Status is -" + ingrainRequestQueueResult.Status, trainingRequestDetails.ApplicationId, string.Empty, string.Empty, string.Empty);
                        IngrainGenericModelInsertTrainingRequest(trainingRequestDetails, ingrainRequestQueueResult.Status);
                    }
                    else
                    {
                        IngrainGenericTrainingResponse(trainingRequestDetails.CorrelationId);
                    }
                }
                else
                {
                    IngrainGenericTrainingResponse(trainingRequestDetails.CorrelationId);
                }
                //if (ingrainRequestQueueResult == null || ingrainRequestQueueResult.Status != CONSTANTS.C)
                //    IngrainGenericTrainingResponse(trainingRequestDetails.CorrelationId);
                //else if (ingrainRequestQueueResult.Status == CONSTANTS.C)
                //    IngrainGenericModelInsertTrainingRequest(trainingRequestDetails);

                _GenericDataResponse.CorrelationId = trainingRequestDetails.CorrelationId;
            }

            return _GenericDataResponse;
        }
        public string UpdateMappingCollections(TemplateTrainingInput trainingRequestDetails, string resourceId)
        {
            AppIntegrationsCredentials appIntegrationsCredentials = new AppIntegrationsCredentials()
            {
                grant_type = appSettings.Value.Grant_Type,
                client_id = appSettings.Value.clientId,
                client_secret = appSettings.Value.clientSecret,
                resource = resourceId
            };

            AppIntegration appIntegrations = new AppIntegration()
            {
                ApplicationID = trainingRequestDetails.ApplicationId,
                Credentials = JsonConvert.SerializeObject(appIntegrationsCredentials)
            };

            PublicTemplateMapping templateMapping = new PublicTemplateMapping()
            {
                ApplicationID = trainingRequestDetails.ApplicationId,
                SourceName = trainingRequestDetails.DataSource,
                UsecaseID = trainingRequestDetails.UsecaseId,
                SourceURL = trainingRequestDetails.DataSourceDetails.Url,
                InputParameters = JsonConvert.SerializeObject(trainingRequestDetails.DataSourceDetails.BodyParams)
            };

            if (UpdateAppIntegration(appIntegrations) == CONSTANTS.Success && UpdatePublicTemplateMappingWithoutEncryption(templateMapping) == CONSTANTS.Success)
            {
                return "Success";
            }
            else
            {
                return "Fail";
            }

        }
        public GenericDataResponse IngrainGenericTrainingRequest(TrainingRequestDetails trainingRequestDetails, string resourceId)
        {
            AppIntegrationsCredentials appIntegrationsCredentials = new AppIntegrationsCredentials()
            {
                grant_type = appSettings.Value.Grant_Type,
                client_id = appSettings.Value.clientId,
                client_secret = appSettings.Value.clientSecret,
                resource = resourceId
            };

            AppIntegration appIntegrations = new AppIntegration()
            {
                ApplicationID = trainingRequestDetails.ApplicationId,
                Credentials = JsonConvert.SerializeObject(appIntegrationsCredentials)
            };

            PublicTemplateMapping templateMapping = new PublicTemplateMapping()
            {
                ApplicationID = trainingRequestDetails.ApplicationId,
                SourceName = trainingRequestDetails.DataSource + trainingRequestDetails.DataSourceDetails.FetchType,
                UsecaseID = trainingRequestDetails.UseCaseId,
                SourceURL = trainingRequestDetails.DataSourceDetails.Url,
                InputParameters = JsonConvert.SerializeObject(appIntegrationsCredentials)
            };

            if (UpdateAppIntegration(appIntegrations) == CONSTANTS.Success && UpdatePublicTemplateMapping(templateMapping) == CONSTANTS.Success)
            {
                _GenericDataResponse = IngrainGenericModelTrainingRequest(trainingRequestDetails);
            }
            else
            {
                _GenericDataResponse.Message = CONSTANTS.NoRecordsFound + ", " + CONSTANTS.ApplicationID + ": " + trainingRequestDetails.ApplicationId;
            }

            return _GenericDataResponse;
        }

        public GenericDataResponse IngrainGenericTrainingResponse(string CorrelationId)
        {
            var ingrainRequestCollection = _database.GetCollection<IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
            var ingrainRequestFilterBuilder = Builders<IngrainRequestQueue>.Filter;
            var ingrainRequestFilterQueue = ingrainRequestFilterBuilder.Where(x => x.CorrelationId == CorrelationId)
                                            & ingrainRequestFilterBuilder.Where(x => x.Function == CONSTANTS.AutoTrain);
            var ingrainRequestQueueResult = ingrainRequestCollection.Find(ingrainRequestFilterQueue).FirstOrDefault();
            _GenericDataResponse.CorrelationId = CorrelationId;
            if (ingrainRequestQueueResult == null)
            {
                IngrainGenericModelInsertTrainingRequest(_trainingRequestDetails, CONSTANTS.E);
                //_GenericDataResponse.Message = CONSTANTS.TrainingNotFound + CorrelationId;
                //_GenericDataResponse.Status = CONSTANTS.E;
            }
            else
            {
                switch (ingrainRequestQueueResult.Status)
                {
                    case CONSTANTS.I:
                        _GenericDataResponse.Message = CONSTANTS.TrainingInitiated;
                        _GenericDataResponse.Status = CONSTANTS.I;
                        break;
                    case CONSTANTS.P:
                        _GenericDataResponse.Message = CONSTANTS.TrainingInProgress;
                        _GenericDataResponse.Status = CONSTANTS.P;
                        break;
                    case CONSTANTS.C:
                        _GenericDataResponse.Message = CONSTANTS.TrainingCompleted;
                        _GenericDataResponse.Status = CONSTANTS.C;
                        break;
                    case CONSTANTS.E:
                        _GenericDataResponse.Message = ingrainRequestQueueResult.Message;
                        _GenericDataResponse.Status = CONSTANTS.E;
                        break;
                    default:
                        _GenericDataResponse.Message = CONSTANTS.TrainingInProgress;
                        _GenericDataResponse.Status = CONSTANTS.P;
                        break;
                }
                _GenericDataResponse.Progress = ingrainRequestQueueResult.Progress.ToString();
            }
            return _GenericDataResponse;
        }

        public TemplateModelPrediction IngrainGenericPredictionRequest(string CorrelationId, string actualData, string predictionCallbackUrl)
        {
            bool IsForAPI = false;
            if (!string.IsNullOrEmpty(CorrelationId))
            {
                DeployModelsDto mdl = _deployedModelService.GetDeployModelDetails(CorrelationId);
                if (mdl == null)
                {
                    _modelPrediction.CorrelationId = CorrelationId;
                    _modelPrediction.Message = "The Model is either not trained or In-progress. Please try after sometime";
                    _modelPrediction.Status = CONSTANTS.E;
                    return _modelPrediction;
                }
                bool DBEncryptionRequired = CommonUtility.EncryptDB(CorrelationId, appSettings);
                PredictionDTO predictionDTO = new PredictionDTO
                {
                    _id = Guid.NewGuid().ToString(),
                    UniqueId = Guid.NewGuid().ToString(),
                    ActualData = DBEncryptionRequired ? _encryptionDecryption.Encrypt(actualData) : actualData,
                    CorrelationId = CorrelationId,
                    Frequency = null,
                    PredictedData = null,
                    Status = CONSTANTS.I,
                    ErrorMessage = null,
                    Progress = null,
                    CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                    ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                    CreatedByUser = DBEncryptionRequired ? _encryptionDecryption.Encrypt(CONSTANTS.System) : CONSTANTS.System,
                    ModifiedByUser = DBEncryptionRequired ? _encryptionDecryption.Encrypt(CONSTANTS.System) : CONSTANTS.System
                };
                SavePrediction(predictionDTO);

                if (appSettings.Value.IsFlaskCall)
                {
                    IsForAPI = true;
                }

                //DeployModelsDto mdl = _deployedModelService.GetDeployModelDetails(CorrelationId);
                IngrainRequestQueue ingrainRequest = new IngrainRequestQueue
                {
                    _id = Guid.NewGuid().ToString(),
                    AppID = mdl.AppId,
                    CorrelationId = CorrelationId,
                    RequestId = Guid.NewGuid().ToString(),
                    ProcessId = null,
                    Status = CONSTANTS.I,
                    ModelName = null,
                    RequestStatus = appSettings.Value.IsFlaskCall ? "Occupied" : CONSTANTS.New,
                    RetryCount = 0,
                    ProblemType = null,
                    Message = null,
                    UniId = predictionDTO.UniqueId,
                    Progress = null,
                    pageInfo = CONSTANTS.PublishModel,
                    ParamArgs = CONSTANTS.CurlyBraces,
                    Function = CONSTANTS.Prediction,
                    CreatedByUser = CONSTANTS.System,
                    CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                    ModifiedByUser = CONSTANTS.System,
                    ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                    LastProcessedOn = null,
                    AppURL = predictionCallbackUrl,
                    IsForAPI = IsForAPI
                };
                _ingestedDataService.InsertRequests(ingrainRequest);

                if (appSettings.Value.IsFlaskCall)
                {
                    _iFlaskAPIService.CallPython(CorrelationId, predictionDTO.UniqueId, ingrainRequest.pageInfo);
                }


                _modelPrediction.CorrelationId = CorrelationId;
                _modelPrediction.UniqueId = predictionDTO.UniqueId;
                _modelPrediction.Status = CONSTANTS.I;
                _modelPrediction.Message = CONSTANTS.PredictionUnderProcess;
                IngrainResponseData CallBackResponse = new IngrainResponseData
                {
                    CorrelationId = CorrelationId,
                    Status = CONSTANTS.I,
                    Message = CONSTANTS.PredictionUnderProcess,
                    ErrorMessage = string.Empty
                };
                CallBackErrorLog.CorrelationId = CallBackResponse.CorrelationId;
                CallBackErrorLog.Status = CallBackResponse.Status;
                CallBackErrorLog.Message = CallBackResponse.Message;
                CallBackErrorLog.ErrorMessage = CallBackResponse.ErrorMessage;
                CallBackErrorLog.RequestId = _modelPrediction.UniqueId;
                CallBackErrorLog.CreatedBy = CONSTANTS.System;
                CallBackErrorLog.ProcessName = "Prediction";
                CallBackErrorLog.UsageType = "INFO";
                CallBackErrorLog.ClientId = mdl.ClientUId;
                CallBackErrorLog.DCID = mdl.DeliveryConstructUID;
                CallBackErrorLog.BaseAddress = predictionCallbackUrl;
                CallBackErrorLog.ApplicationID = mdl.AppId;
                AuditTrailLog(CallBackErrorLog);
            }
            else
            {
                _modelPrediction.Message = CONSTANTS.ErrorMessage;
                _modelPrediction.Status = CONSTANTS.E;
                IngrainResponseData CallBackResponse = new IngrainResponseData
                {
                    CorrelationId = CorrelationId,
                    Status = CONSTANTS.E,
                    Message = CONSTANTS.ErrorMessage,
                    ErrorMessage = "Correlation ID is null"
                };
                CallBackErrorLog.CorrelationId = CallBackResponse.CorrelationId;
                CallBackErrorLog.Status = CallBackResponse.Status;
                CallBackErrorLog.Message = CallBackResponse.Message;
                CallBackErrorLog.ErrorMessage = CallBackResponse.ErrorMessage;
                CallBackErrorLog.RequestId = _modelPrediction.UniqueId;
                CallBackErrorLog.CreatedBy = CONSTANTS.System;
                CallBackErrorLog.ProcessName = "Prediction";
                CallBackErrorLog.UsageType = "ERROR";
                CallBackErrorLog.BaseAddress = predictionCallbackUrl;
                AuditTrailLog(CallBackErrorLog);
            }
            return _modelPrediction;
        }

        public PredictionResultDTO IngrainGenericPredictionResponse(string CorrelationId, string UniqueId)
        {
            bool isPrediction = true;
            PredictionDTO predictionData = new PredictionDTO
            {
                UniqueId = UniqueId,
                CorrelationId = CorrelationId,
                Status = CONSTANTS.P,
                Progress = CONSTANTS.PredictionUnderProcess
            };
            int retry = 0;
            while (isPrediction)
            {
                predictionData = _deployedModelService.GetPrediction(predictionData);

                isPrediction = false;
                DateTime currentTime = DateTime.Now;
                DateTime createdTime = DateTime.Parse(predictionData.CreatedOn);
                TimeSpan span = currentTime.Subtract(createdTime);
                if (span.TotalMinutes > Convert.ToDouble(appSettings.Value.PredictionTimeoutMinutes) && predictionData.Status != CONSTANTS.C)
                {
                    _predictionresult.Message = CONSTANTS.PredictionTimeOut;
                    _predictionresult.CorrelationId = CorrelationId;
                    _predictionresult.UniqueId = UniqueId;
                    _predictionresult.Status = "E";
                    _predictionresult.Progress = predictionData.Progress;
                    isPrediction = false;
                    IngrainResponseData CallBackResponse = new IngrainResponseData
                    {
                        CorrelationId = CorrelationId,
                        Status = CONSTANTS.E,
                        Message = CONSTANTS.PredictionTimeOut,
                        ErrorMessage = CONSTANTS.PredictionTimeOut
                    };
                    CallBackErrorLog.CorrelationId = CallBackResponse.CorrelationId;
                    CallBackErrorLog.Status = CallBackResponse.Status;
                    CallBackErrorLog.Message = CallBackResponse.Message;
                    CallBackErrorLog.ErrorMessage = CallBackResponse.ErrorMessage;
                    CallBackErrorLog.RequestId = UniqueId;
                    CallBackErrorLog.CreatedBy = CONSTANTS.System;
                    CallBackErrorLog.ProcessName = "Prediction";
                    CallBackErrorLog.UsageType = "ERROR";
                    AuditTrailLog(CallBackErrorLog);
                    return _predictionresult;
                }
                if (predictionData.Status == CONSTANTS.I || predictionData.Status == CONSTANTS.P)
                {
                    _predictionresult.Status = CONSTANTS.P;
                    _predictionresult.Message = CONSTANTS.PredictionUnderProcess;
                }
                else if (predictionData.Status == CONSTANTS.E)
                {
                    string response = JsonConvert.SerializeObject(_predictionresult);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(IngrainGenericPredictionResponse), CONSTANTS.Prediction + response.Replace(CONSTANTS.slash, string.Empty), string.IsNullOrEmpty(predictionData.CorrelationId) ? default(Guid) : new Guid(predictionData.CorrelationId), predictionData.AppID, string.Empty, string.Empty, string.Empty);
                    _predictionresult.ErrorMessage = "Python: Error While Prediction : " + predictionData.ErrorMessage + " Status:" + predictionData.Status;
                    IngrainResponseData CallBackResponse = new IngrainResponseData
                    {
                        CorrelationId = CorrelationId,
                        Status = CONSTANTS.E,
                        Message = _predictionresult.ErrorMessage,
                        ErrorMessage = _predictionresult.ErrorMessage
                    };
                    CallBackErrorLog.CorrelationId = CallBackResponse.CorrelationId;
                    CallBackErrorLog.Status = CallBackResponse.Status;
                    CallBackErrorLog.Message = CallBackResponse.Message;
                    CallBackErrorLog.ErrorMessage = CallBackResponse.ErrorMessage;
                    CallBackErrorLog.RequestId = UniqueId;
                    CallBackErrorLog.CreatedBy = CONSTANTS.System;
                    CallBackErrorLog.ProcessName = "Prediction";
                    CallBackErrorLog.UsageType = "ERROR";
                    AuditTrailLog(CallBackErrorLog);
                }
                else if (predictionData.Status == CONSTANTS.C)
                {
                    _predictionresult.PredictedData = predictionData.PredictedData;
                    _predictionresult.Message = "Prediction Completed";
                    ValidRecordsDetailsModel validRecordsDetailModel = CommonUtility.GetModelDataPointdDetails(CorrelationId, appSettings);
                    if (validRecordsDetailModel != null)
                    {
                        if (validRecordsDetailModel.ValidRecordsDetails != null)
                        {
                            if (validRecordsDetailModel.ValidRecordsDetails.Records[0] >= 4 && validRecordsDetailModel.ValidRecordsDetails.Records[0] < 20)
                            {
                                _predictionresult.DataPointsWarning = CONSTANTS.DataPointsMinimum;
                                _predictionresult.DataPointsCount = validRecordsDetailModel.ValidRecordsDetails.Records[0];
                            }
                        }
                    }
                    IngrainResponseData CallBackResponse = new IngrainResponseData
                    {
                        CorrelationId = CorrelationId,
                        Status = CONSTANTS.C,
                        Message = _predictionresult.Message,
                        ErrorMessage = _predictionresult.Message
                    };
                    CallBackErrorLog.CorrelationId = CallBackResponse.CorrelationId;
                    CallBackErrorLog.Status = CallBackResponse.Status;
                    CallBackErrorLog.Message = CallBackResponse.Message;
                    CallBackErrorLog.ErrorMessage = CallBackResponse.ErrorMessage;
                    CallBackErrorLog.RequestId = UniqueId;
                    CallBackErrorLog.CreatedBy = CONSTANTS.System;
                    CallBackErrorLog.ProcessName = "Prediction";
                    CallBackErrorLog.UsageType = "INFO";
                    AuditTrailLog(CallBackErrorLog);
                }

                else
                {
                    retry++;
                    Thread.Sleep(1000);
                    if (retry > 3)
                    {
                        isPrediction = false;
                        predictionData.Status = "E";
                        _predictionresult.ErrorMessage = "UniqueId not found";
                    }
                    else
                    {
                        isPrediction = true;
                    }

                }


            }

            _predictionresult.CorrelationId = CorrelationId;
            _predictionresult.UniqueId = UniqueId;
            _predictionresult.Status = predictionData.Status;
            _predictionresult.Progress = predictionData.Progress;

            return _predictionresult;
        }

        public List<dynamic> GetVDSData(VDSParams inputParams)
        {
            string collectionName = string.Empty;
            string ServiceType = string.Empty;
            DateTime startDate = DateTime.Parse(inputParams.StartDate).ToUniversalTime();
            DateTime endDate = DateTime.Parse(inputParams.EndDate).ToUniversalTime();

            var filterBuilder = Builders<BsonDocument>.Filter;
            FilterDefinition<BsonDocument> filter;


            if (inputParams.RequestType == "AM")
            {
                switch (inputParams.ServiceType)
                {
                    case "Incidents":
                        collectionName = "incidentCollection";
                        break;
                    case "ProblemTickets":
                        collectionName = "problemCollection";
                        break;
                    case "ServiceRequests":
                        collectionName = "serviceRequestCollection";
                        break;
                    case "WorkRequests":
                        collectionName = "workRequestCollection";
                        break;
                    default:
                        break;

                }

                filter = filterBuilder.Eq("ClientUId", inputParams.ClientID) & filterBuilder.Eq("EndToEndUId", inputParams.DeliveryConstructID) & filterBuilder.Gte("ReportedDateTime", startDate) & filterBuilder.Lte("ReportedDateTime", endDate);
            }
            else if (inputParams.RequestType == "IO")
            {
                collectionName = "ioTicketCollection";
                switch (inputParams.ServiceType)
                {
                    case "Incidents":
                        ServiceType = "Incidents";
                        break;
                    case "ProblemTickets":
                        ServiceType = "ProblemTickets";
                        break;
                    case "ServiceRequests":
                        ServiceType = "ServiceRequests";
                        break;
                    case "WorkRequests":
                        ServiceType = "ChangeManagement";
                        break;
                    default:
                        break;

                }

                filter = filterBuilder.Eq("ClientUId", inputParams.ClientID) & filterBuilder.Eq("EndToEndUId", inputParams.DeliveryConstructID) & filterBuilder.Gte("ReportedDateTime", startDate) & filterBuilder.Lte("ReportedDateTime", endDate) & filterBuilder.Eq("ServiceType", ServiceType) & filterBuilder.Eq("Domain", inputParams.RequestType);
            }
            else
            {
                throw new Exception("Invalid Request Type parameter");
            }



            var appCollection = _database.GetCollection<BsonDocument>(collectionName);
            var ApplicationResult = appCollection.Aggregate().Match(filter)
                                    .AppendStage<BsonDocument>(new BsonDocument() {
                                        {
                                            "$addFields",new BsonDocument()
                                            {
                                                {"DateColumn","$ReportedDateTime" }
                                            }
                                        }
                                    })
                                  .ToList();


            List<dynamic> result = new List<dynamic>();
            if (ApplicationResult.Count > 0)
            {
                var jsonString = JsonConvert.SerializeObject(ApplicationResult.ConvertAll(d => BsonTypeMapper.MapToDotNetValue(d)), Formatting.Indented);

                result = JsonConvert.DeserializeObject<List<dynamic>>(jsonString);
            }

            return result;
        }
        public List<dynamic> EncrypDecryptData(string ApplicationName, string Type)
        {
            var AppIntegCollection = _database.GetCollection<BsonDocument>(CONSTANTS.AppIntegration);
            var filterBuilder = Builders<BsonDocument>.Filter;
            var AppFilter = filterBuilder.Eq("ApplicationName", ApplicationName);

            var Projection = Builders<BsonDocument>.Projection.Include("BaseURL").Include("Authentication").Include("TokenGenerationURL").Include("Credentials").Exclude("_id");
            var AppData = AppIntegCollection.Find(AppFilter).Project<BsonDocument>(Projection).ToList();


            List<dynamic> result = new List<dynamic>();
            if (AppData.Count > 0)
            {

                if (Type == "Decrypt")
                {

                    AppData[0]["TokenGenerationURL"] = _encryptionDecryption.Decrypt(AppData[0]["TokenGenerationURL"].ToString());
                    AppData[0]["Credentials"] = _encryptionDecryption.Decrypt(AppData[0]["Credentials"].AsString);

                    var TokenURL = Builders<BsonDocument>.Update.Set("TokenGenerationURL", AppData[0]["TokenGenerationURL"].ToString());
                    AppIntegCollection.UpdateMany(AppFilter, TokenURL);

                    var Featuredata = BsonDocument.Parse(AppData[0]["Credentials"].ToString());
                    var Credentials = Builders<BsonDocument>.Update.Set("Credentials", Featuredata);
                    AppIntegCollection.UpdateMany(AppFilter, Credentials);
                }
                else
                {

                    AppData[0]["TokenGenerationURL"] = _encryptionDecryption.Encrypt(AppData[0]["TokenGenerationURL"].ToString());
                    AppData[0]["Credentials"] = _encryptionDecryption.Encrypt(AppData[0]["Credentials"].ToString());

                    var TokenURL = Builders<BsonDocument>.Update.Set("TokenGenerationURL", AppData[0]["TokenGenerationURL"].ToString());
                    AppIntegCollection.UpdateMany(AppFilter, TokenURL);

                    var Credentials = Builders<BsonDocument>.Update.Set("Credentials", AppData[0]["Credentials"].ToString());
                    AppIntegCollection.UpdateMany(AppFilter, Credentials);

                }

                var AppData1 = AppIntegCollection.Find(AppFilter).Project<BsonDocument>(Projection).ToList();
                var jsonString = JsonConvert.SerializeObject(AppData1.ConvertAll(d => BsonTypeMapper.MapToDotNetValue(d)), Formatting.Indented);
                //var jsonWriterSettings = new JsonWriterSettings { OutputMode = JsonOutputMode.Strict };
                //Console.WriteLine(ApplicationResult.ToJson(jsonWriterSettings));
                result = JsonConvert.DeserializeObject<List<dynamic>>(jsonString);
            }

            return result;
            // return (AppData);
        }


        public void BulkPredictionsTest(string correlationId, int noOfRequest, int recordsPerRequest)
        {
            var _deployModelCollection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
            var filter = Builders<DeployModelsDto>.Filter.Eq(CONSTANTS.CorrelationId, correlationId) & Builders<DeployModelsDto>.Filter.Eq("Status", "Deployed");
            var projection = Builders<DeployModelsDto>.Projection.Exclude("_id");
            var result = _deployModelCollection.Find(filter).Project<DeployModelsDto>(projection).FirstOrDefault();
            if (result != null)
            {
                string temp = string.Empty;

                if (result.DBEncryptionRequired)
                    temp = _encryptionDecryption.Decrypt(result.InputSample);
                else
                    temp = result.InputSample;

                List<object> inputSample = JsonConvert.DeserializeObject<List<object>>(temp);
                List<object> predictionInput = new List<object>();
                for (int i = 0; i < recordsPerRequest; i++)
                {
                    predictionInput.Add(inputSample[0]);
                }
                string jsonPredictionInput = JsonConvert.SerializeObject(predictionInput);
                string apiUrl = new Uri(appSettings.Value.myWizardAPIUrl).Host;
                apiUrl = "https://" + apiUrl + "/ingrain/api/IngrainGenericPredictionRequest?CorrelationId=" + correlationId;
                string token = IngrainToken();
                using (var client = new HttpClient())
                {
                    string uri = apiUrl;
                    client.DefaultRequestHeaders.Accept.Clear();
                    if (!string.IsNullOrWhiteSpace(token))
                    {
                        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                    }

                    for (int i = 0; i < noOfRequest; i++)
                    {
                        var formContent = new FormUrlEncodedContent(new[]
                        {
                         new KeyValuePair<string, string>("PredictionRequestData", jsonPredictionInput)
                        });

                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                        var res = client.PostAsync(uri, formContent).Result;
                    }
                }


            }

        }

        public string IngrainToken()
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(IngrainToken), CONSTANTS.START + appSettings.Value.authProvider.ToUpper(), string.Empty, string.Empty, string.Empty, string.Empty);
            dynamic token = string.Empty;
            if (appSettings.Value.authProvider.ToUpper() == "FORM")
            {
                var username = Convert.ToString(appSettings.Value.username);
                var password = Convert.ToString(appSettings.Value.password);
                var tokenendpointurl = Convert.ToString(appSettings.Value.tokenAPIUrl);
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add(CONSTANTS.username, username);
                    client.DefaultRequestHeaders.Add(CONSTANTS.password, password);

                    var tokenResponse = client.PostAsync(tokenendpointurl, null).Result;
                    var tokenResult = tokenResponse.Content.ReadAsStringAsync().Result;
                    Dictionary<string, string> tokenDictionary = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(tokenResult);
                    if (tokenDictionary != null)
                    {
                        if (tokenResponse.IsSuccessStatusCode)
                        {
                            token = tokenDictionary[CONSTANTS.access_token].ToString();
                        }
                        else
                        {
                            token = CONSTANTS.InvertedComma;
                        }
                    }
                    else
                    {
                        token = CONSTANTS.InvertedComma;
                    }
                }

            }
            else
            {

                var client = new RestClient(appSettings.Value.token_Url_VDS);
                var request = new RestRequest(Method.POST);
                request.AddHeader("cache-control", "no-cache");
                request.AddHeader("content-type", "application/x-www-form-urlencoded");
                request.AddParameter("application/x-www-form-urlencoded", "grant_type=" + appSettings.Value.Grant_Type_VDS +
                   "&client_id=" + appSettings.Value.clientId_VDS +
                   "&client_secret=" + appSettings.Value.clientSecret_VDS +
                   "&scope=" + appSettings.Value.scopeStatus_VDS +
                   "&resource=" + appSettings.Value.resource_ingrain,
                   ParameterType.RequestBody);
                var requestBuilder = new StringBuilder();
                foreach (var param in request.Parameters)
                {
                    requestBuilder.AppendFormat("{0}: {1}\r\n", param.Name, param.Value);
                }
                requestBuilder.ToString();
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(IngrainToken), "PYTHON TOKEN PARAMS -- " + requestBuilder, string.Empty, string.Empty, string.Empty, string.Empty);
                IRestResponse response = client.Execute(request);
                string json = response.Content;
                // Retrieve and Return the Access Token                
                var tokenObj = JsonConvert.DeserializeObject(json) as dynamic;
                token = Convert.ToString(tokenObj.access_token);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(IngrainToken), "END -" + appSettings.Value.authProvider.ToUpper(), string.Empty, string.Empty, string.Empty, string.Empty);
            return token;
        }

        public void UpdateResponseData(string data)
        {
            var dataContent = JObject.Parse(data.ToString());
            bool DBEncryptionRequired = CommonUtility.EncryptDB(Convert.ToString(dataContent["CorrelationId"]), appSettings);
            if (DBEncryptionRequired)
            {
                if (dataContent.ContainsKey(CONSTANTS.CreatedByUser) && !string.IsNullOrEmpty(Convert.ToString(dataContent[CONSTANTS.CreatedByUser])))
                {
                    if (!CommonUtility.GetValidUser(Convert.ToString(dataContent[CONSTANTS.CreatedByUser])))
                    {
                        throw new Exception("UserName/UserId is Invalid");
                    }
                    dataContent[CONSTANTS.CreatedByUser] = _encryptionDecryption.Encrypt(Convert.ToString(dataContent[CONSTANTS.CreatedByUser]));
                }
                if (dataContent.ContainsKey(CONSTANTS.ModifiedByUser) && !string.IsNullOrEmpty(Convert.ToString(dataContent[CONSTANTS.ModifiedByUser])))
                {
                    if (!CommonUtility.GetValidUser(Convert.ToString(dataContent[CONSTANTS.ModifiedByUser])))
                    {
                        throw new Exception("UserName/UserId is Invalid");
                    }
                    dataContent[CONSTANTS.ModifiedByUser] = _encryptionDecryption.Encrypt(Convert.ToString(dataContent[CONSTANTS.ModifiedByUser]));
                }
                if (dataContent.ContainsKey(CONSTANTS.UserId) && !string.IsNullOrEmpty(Convert.ToString(dataContent[CONSTANTS.UserId])))
                {
                    if (!CommonUtility.GetValidUser(Convert.ToString(dataContent[CONSTANTS.UserId])))
                    {
                        throw new Exception("UserName/UserId is Invalid");
                    }
                    dataContent[CONSTANTS.UserId] = _encryptionDecryption.Encrypt(Convert.ToString(dataContent[CONSTANTS.UserId]));
                }
            }
            var insertResponseData = BsonSerializer.Deserialize<BsonDocument>(data);
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.CallBackResponseData);
            collection.InsertOne(insertResponseData);
        }

        public ModelTrainingStatus InitiateTraining(string data)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(InitiateTraining), "InitiateTraining - Started :", string.Empty, string.Empty, string.Empty, string.Empty);
            RequestData RequestPayload = new RequestData();
            string response = string.Empty;
            RequestPayload = JsonConvert.DeserializeObject<RequestData>(data);
            if (RequestPayload.ResponseCallbackUrl == "")
            {
                ModelTrainingStatus.ErrorMessage = CONSTANTS.ResponseCallbackUrlNull;
                return ModelTrainingStatus;
            }
            var AppIntegCollection = _database.GetCollection<AppIntegration>(CONSTANTS.AppIntegration);
            var filterBuilder = Builders<AppIntegration>.Filter;
            var AppFilter = filterBuilder.Eq(CONSTANTS.ApplicationID, RequestPayload.AppServiceUId);
            var Projection = Builders<AppIntegration>.Projection.Exclude(CONSTANTS.Id);
            var IsApplicationExist = AppIntegCollection.Find(AppFilter).Project<AppIntegration>(Projection).FirstOrDefault();
            string CorrelationId = string.Empty;
            if (IsApplicationExist != null)
            {
                var MappingCollection = _database.GetCollection<PublicTemplateMapping>(CONSTANTS.PublicTemplateMapping);
                var Builder = Builders<PublicTemplateMapping>.Filter;
                var Projection1 = Builders<PublicTemplateMapping>.Projection.Exclude("_id");
                var filter = Builder.Eq(CONSTANTS.ApplicationID, RequestPayload.AppServiceUId) & Builder.Eq(CONSTANTS.UsecaseID, RequestPayload.UseCaseUId);
                var templatedata = MappingCollection.Find(filter).Project<PublicTemplateMapping>(Projection1).FirstOrDefault();
                if (templatedata != null)
                {
                    var modelTrainingStatus = GetSPAModelStatus(RequestPayload.ClientUId, RequestPayload.DeliveryConstructUId, RequestPayload.AppServiceUId, RequestPayload.UseCaseUId);
                    string NewModelCorrelationID = Guid.NewGuid().ToString();
                    if (modelTrainingStatus.Status == null || modelTrainingStatus.Status == CONSTANTS.E)
                    {
                        if (!string.IsNullOrEmpty(modelTrainingStatus.CorrelationId))
                        {
                            NewModelCorrelationID = modelTrainingStatus.CorrelationId;
                        }
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(InitiateTraining), "MODELTRAININGSTATUS :" + modelTrainingStatus.CorrelationId + "--" + modelTrainingStatus.Status + "--NEW--" + NewModelCorrelationID, string.Empty, string.Empty, string.Empty, string.Empty);
                        var ingrainRequest = InsertSPAPrediction(NewModelCorrelationID, templatedata, RequestPayload);
                        InsertRequests(ingrainRequest);
                        IngrainResponseData CallBackResponse = new IngrainResponseData
                        {
                            CorrelationId = ingrainRequest.CorrelationId,
                            Status = CONSTANTS.I,
                            Message = CONSTANTS.TrainingInitiated,
                            ErrorMessage = string.Empty
                        };
                        string callbackResonse = CallbackResponse(CallBackResponse, templatedata.ApplicationName, RequestPayload.ResponseCallbackUrl, RequestPayload.ClientUId, RequestPayload.DeliveryConstructUId, RequestPayload.AppServiceUId, RequestPayload.UseCaseUId, ingrainRequest.RequestId, ingrainRequest.CreatedByUser);
                        if (callbackResonse == CONSTANTS.ErrorMessage)
                        {
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(InitiateTraining), "Call Back URL Token Response :" + callbackResonse, string.Empty, string.Empty, string.Empty, string.Empty);
                            ModelTrainingStatus.ErrorMessage = CONSTANTS.TokenFailed;
                            return ModelTrainingStatus;
                        }
                        ModelTrainingStatus.Message = CONSTANTS.TrainingInitiated;
                        ModelTrainingStatus.CorrelationId = ingrainRequest.CorrelationId;
                        ModelTrainingStatus.ClientUId = RequestPayload.ClientUId;
                        ModelTrainingStatus.DeliveryConstructUId = RequestPayload.DeliveryConstructUId;
                        return ModelTrainingStatus;
                    }
                    else
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(InitiateTraining), "MODELTRAININGSTATUS ELSE:" + modelTrainingStatus.CorrelationId + "--" + modelTrainingStatus.Status, string.Empty, string.Empty, string.Empty, string.Empty);
                        if (modelTrainingStatus.Status == CONSTANTS.P || modelTrainingStatus.Status == CONSTANTS.I)
                        {
                            ModelTrainingStatus.Status = modelTrainingStatus.Status;
                            ModelTrainingStatus.Message = CONSTANTS.TrainingInprogress;
                            ModelTrainingStatus.CorrelationId = modelTrainingStatus.CorrelationId;
                            ModelTrainingStatus.PageInfo = modelTrainingStatus.PageInfo;
                            ModelTrainingStatus.ClientUId = RequestPayload.ClientUId;
                            ModelTrainingStatus.DeliveryConstructUId = RequestPayload.DeliveryConstructUId;
                            return ModelTrainingStatus;
                        }
                        var ModelResponse = GetSPAPrediction(modelTrainingStatus.CorrelationId, templatedata, RequestPayload, NewModelCorrelationID);
                        if (ModelResponse != null & ModelResponse.Status != null)
                        {
                            ModelTrainingStatus.ClientUId = RequestPayload.ClientUId;
                            ModelTrainingStatus.CorrelationId = ModelResponse.CorrelationId;
                            ModelTrainingStatus.IsPredicted = ModelResponse.IsPredicted;
                            ModelTrainingStatus.DeliveryConstructUId = RequestPayload.DeliveryConstructUId;
                            if (ModelResponse.Status == CONSTANTS.E)
                                ModelTrainingStatus.ErrorMessage = CONSTANTS.TokenFailed;
                            if (ModelResponse.Status == CONSTANTS.C)
                                ModelTrainingStatus.Status = ModelResponse.Status;
                            return ModelTrainingStatus;
                        }
                    }
                }
                else
                {
                    ModelTrainingStatus.ErrorMessage = CONSTANTS.UsecaseNotAvailable; ;
                    return ModelTrainingStatus;
                }
            }
            else
            {
                ModelTrainingStatus.ErrorMessage = CONSTANTS.IsApplicationExist;
                return ModelTrainingStatus;
            }
            return ModelTrainingStatus;
        }


        private IngrainRequestQueue InsertSPAPrediction(string NewModelCorrelationID, PublicTemplateMapping templatedata, RequestData RequestPayload)
        {
            bool DBEncryptionRequired = CommonUtility.EncryptDB(NewModelCorrelationID, appSettings);
            IngrainRequestQueue ingrainRequest = new IngrainRequestQueue
            {
                _id = Guid.NewGuid().ToString(),
                CorrelationId = NewModelCorrelationID,
                RequestId = Guid.NewGuid().ToString(),
                ProcessId = CONSTANTS.Null,
                Status = CONSTANTS.Null,
                ModelName = templatedata.ApplicationName + "_" + templatedata.UsecaseName,
                RequestStatus = CONSTANTS.New,
                RetryCount = 0,
                ProblemType = CONSTANTS.Null,
                Message = CONSTANTS.Null,
                UniId = CONSTANTS.Null,
                InstaID = CONSTANTS.Null,
                Progress = CONSTANTS.Null,
                pageInfo = CONSTANTS.TrainAndPredict,
                ParamArgs = CONSTANTS.Null,
                TemplateUseCaseID = templatedata.UsecaseID,
                Function = CONSTANTS.AutoTrain,
                CreatedByUser = DBEncryptionRequired ? _encryptionDecryption.Encrypt(CONSTANTS.SYSTEM) : CONSTANTS.SYSTEM,
                CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                ModifiedByUser = DBEncryptionRequired ? _encryptionDecryption.Encrypt(CONSTANTS.SYSTEM) : CONSTANTS.SYSTEM,
                ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                LastProcessedOn = CONSTANTS.Null,
                AppID = templatedata.ApplicationID,
                ClientId = RequestPayload.ClientUId,
                DeliveryconstructId = RequestPayload.DeliveryConstructUId,
                UseCaseID = CONSTANTS.Null,
                EstimatedRunTime = CONSTANTS.Null,
                AppURL = RequestPayload.ResponseCallbackUrl
            };
            return ingrainRequest;
        }
        private ModelTrainingStatus GetSPAPrediction(string CorrelationId, PublicTemplateMapping templatedata, RequestData RequestPayload, string NewModelCorrelationID)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(GetSPAPrediction), "GETSPAPREDICTION - STARTED :", string.IsNullOrEmpty(CorrelationId) ? default(Guid) : new Guid(CorrelationId), templatedata.ApplicationID, string.Empty, RequestPayload.ClientUId, RequestPayload.DeliveryConstructUId);
            ModelTrainingStatus modelTraining = new ModelTrainingStatus();
            string response = string.Empty;
            var collection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
            var builder = Builders<DeployModelsDto>.Filter;
            var filter1 = builder.Eq(CONSTANTS.CorrelationId, CorrelationId);
            var Projection2 = Builders<DeployModelsDto>.Projection.Exclude("_id");
            var deployedModel = collection.Find(filter1).Project<DeployModelsDto>(Projection2).ToList();
            {
                for (int i = 0; i < deployedModel.Count; i++)
                {
                    if (deployedModel[i].Status == CONSTANTS.Deployed)
                    {
                        modelTraining = GetPrediction(CorrelationId, templatedata, RequestPayload.ResponseCallbackUrl, deployedModel[i]);
                    }
                    else
                    {
                        var ingrainRequest = InsertSPAPrediction(NewModelCorrelationID, templatedata, RequestPayload);
                        InsertRequests(ingrainRequest);
                        if (deployedModel != null)
                        {
                            collection.DeleteMany(filter1);
                        }
                        var queueCollection = _database.GetCollection<IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
                        var filterBuilder1 = Builders<IngrainRequestQueue>.Filter;
                        var filterQueue1 = filterBuilder1.Where(x => x.CorrelationId == CorrelationId);
                        queueCollection.DeleteMany(filterQueue1);
                        IngrainResponseData CallBackResponse = new IngrainResponseData
                        {
                            CorrelationId = ingrainRequest.CorrelationId,
                            Status = CONSTANTS.I,
                            Message = CONSTANTS.ReTrainingInitiated,
                            ErrorMessage = string.Empty,
                        };
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(GetSPAPrediction), "GETSPAPREDICTION - CALLBACK API STARTED:", string.IsNullOrEmpty(CallBackResponse.CorrelationId) ? default(Guid) : new Guid(CallBackResponse.CorrelationId), string.Empty, string.Empty, string.Empty, string.Empty);
                        string callbackResonse = CallbackResponse(CallBackResponse, templatedata.ApplicationName, RequestPayload.ResponseCallbackUrl, ingrainRequest.ClientID, ingrainRequest.DCID, ingrainRequest.AppID, ingrainRequest.TemplateUseCaseID, ingrainRequest.RequestId, ingrainRequest.CreatedByUser);
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(GetSPAPrediction), "GETSPAPREDICTION - CALLBACK API END:" + callbackResonse, string.Empty, string.Empty, string.Empty, string.Empty);
                        if (callbackResonse == CONSTANTS.ErrorMessage)
                        {
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(InitiateTraining), "Call Back URL Token Response :" + callbackResonse, string.Empty, string.Empty, string.Empty, string.Empty);
                            modelTraining.ErrorMessage = CONSTANTS.TokenFailed;
                            modelTraining.Status = CONSTANTS.E;
                            modelTraining.CorrelationId = ingrainRequest.CorrelationId;
                            modelTraining.ClientUId = ingrainRequest.ClientId;
                            modelTraining.DeliveryConstructUId = ingrainRequest.DeliveryconstructId;
                        }
                    }
                }
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(GetSPAPrediction), "GETSPAPREDICTION - END:", string.Empty, string.Empty, string.Empty, string.Empty);
            return modelTraining;
        }

        private string CallbackResponse(IngrainResponseData CallBackResponse, string AppName, string baseAddress, string clientId, string DCId, string applicationId, string usecaseId, string requestId, string userId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(CallbackResponse), "CALLBACKRESPONSE - CALLBACK API STARTED:", applicationId, string.Empty, clientId, DCId);
            string token = CustomUrlToken(AppName);
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(CallbackResponse), "CALLBACKRESPONSE - CALLBACK TOKEN:" + token, applicationId, string.Empty, clientId, DCId);
            string contentType = "application/json";
            var Request = JsonConvert.SerializeObject(CallBackResponse);
            using (var Client = new HttpClient())
            {
                Client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                Client.DefaultRequestHeaders.Add("AppServiceUId", appSettings.Value.AppServiceUID);
                Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(contentType));

                HttpContent httpContent = new StringContent(Request, Encoding.UTF8, "application/json");
                HttpResponseMessage httpResponse = Client.PostAsync(baseAddress, httpContent).Result;
                var statuscode = httpResponse.StatusCode;
                CallBackErrorLog.CorrelationId = CallBackResponse.CorrelationId;
                CallBackErrorLog.Status = CallBackResponse.Status;
                CallBackErrorLog.ErrorMessage = CallBackResponse.ErrorMessage;
                CallBackErrorLog.Message = CallBackResponse.Message;
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
                //add for ussgaType
                if (CallBackResponse.Status != CONSTANTS.E || CallBackResponse.Status != CONSTANTS.ErrorMessage)
                {
                    CallBackErrorLog.UsageType = "INFO";
                }
                else
                {
                    CallBackErrorLog.UsageType = "ERROR";
                }
                AuditTrailLog(CallBackErrorLog);
                if (httpResponse.IsSuccessStatusCode)
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(CallbackResponse), "CALLBACKRESPONSE - CALLBACK API SUCCESS:", string.Empty, string.Empty, string.Empty, string.Empty);
                    return CONSTANTS.success;
                }
                else
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(CallbackResponse), "CALLBACKRESPONSE - CALLBACK API ERROR:- HTTP RESPONSE-" + httpResponse.StatusCode + "-HTTPCONTENT-" + httpResponse.Content.ReadAsStringAsync().Result, string.Empty, string.Empty, string.Empty, string.Empty);
                    return CONSTANTS.Error;
                }
            }
        }
        private ModelTrainingStatus GetPrediction(string CorrelationId, PublicTemplateMapping templatedata, string ResponseCallbackUrl, DeployModelsDto deployedModel)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(GetPrediction), "GETPREDICTION - STARTED :", string.IsNullOrEmpty(CorrelationId) ? default(Guid) : new Guid(CorrelationId), deployedModel.AppId, string.Empty, deployedModel.ClientUId, deployedModel.DeliveryConstructUID);
            ModelTrainingStatus modelTraining = new ModelTrainingStatus();
            if (deployedModel != null)
            {
                bool DBEncryptionRequired = CommonUtility.EncryptDB(deployedModel.CorrelationId, appSettings);
                string FunctionType = string.Empty;
                string actualData = string.Empty;
                if (deployedModel.ModelType == CONSTANTS.TimeSeries)
                {
                    FunctionType = CONSTANTS.ForecastModel;
                    actualData = DBEncryptionRequired ? _encryptionDecryption.Encrypt(CONSTANTS.Null) : CONSTANTS.Null;
                }
                else
                {
                    FunctionType = CONSTANTS.PublishModel;
                    actualData = deployedModel.InputSample;
                }
                PredictionDTO predictionDTO = new PredictionDTO
                {
                    _id = Guid.NewGuid().ToString(),
                    UniqueId = Guid.NewGuid().ToString(),
                    CorrelationId = CorrelationId,
                    Frequency = deployedModel.Frequency,
                    PredictedData = null,
                    Status = CONSTANTS.I,
                    ErrorMessage = null,
                    TempalteUseCaseId = deployedModel.TemplateUsecaseId,
                    AppID = deployedModel.AppId,
                    Progress = null,
                    CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                    ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                    CreatedByUser = DBEncryptionRequired ? _encryptionDecryption.Encrypt(CONSTANTS.System) : CONSTANTS.System,
                    ModifiedByUser = DBEncryptionRequired ? _encryptionDecryption.Encrypt(CONSTANTS.System) : CONSTANTS.System
                };
                if (deployedModel.SourceName == "Custom" || deployedModel.SourceName == CONSTANTS.SPAAPP)
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(GetPrediction), "GETPREDICTIONINSERTION - STARTED :", string.IsNullOrEmpty(CorrelationId) ? default(Guid) : new Guid(CorrelationId), deployedModel.AppId, string.Empty, deployedModel.ClientUId, deployedModel.DeliveryConstructUID);
                    //string actualData = getIncrementalData(deployedModel.CorrelationId, templatedata, deployedModel.TemplateUsecaseId);
                    predictionDTO.ActualData = actualData;
                    SavePrediction(predictionDTO);
                    this.insertRequest(deployedModel, predictionDTO.UniqueId, FunctionType);
                    bool isPredicted = IsPredictedCompeted(predictionDTO);
                    if (isPredicted)
                    {
                        modelTraining.CorrelationId = deployedModel.CorrelationId;
                        modelTraining.ClientUId = deployedModel.ClientUId;
                        modelTraining.DeliveryConstructUId = deployedModel.DeliveryConstructUID;
                        modelTraining.Status = CONSTANTS.C;
                        modelTraining.DataPointsCount = _modelPrediction.DataPointsCount;
                        modelTraining.DataPointsWarning = _modelPrediction.DataPointsWarning;
                        modelTraining.IsPredicted = true;
                    }
                    else
                    {
                        modelTraining.CorrelationId = deployedModel.CorrelationId;
                        modelTraining.ClientUId = deployedModel.ClientUId;
                        modelTraining.DeliveryConstructUId = deployedModel.DeliveryConstructUID;
                        modelTraining.Status = CONSTANTS.E;
                        modelTraining.IsPredicted = false;
                    }
                }
                else
                {
                    var ingest_collection = _database.GetCollection<BsonDocument>(CONSTANTS.PS_IngestedData);
                    var ingest_filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, deployedModel.CorrelationId);
                    var ingest_Projection = Builders<BsonDocument>.Projection.Include("lastDateDict").Exclude("_id");
                    var Ingestdata_result = ingest_collection.Find(ingest_filter).Project<BsonDocument>(ingest_Projection).ToList();
                    if (Ingestdata_result.Count > 0)
                    {
                        IngrainRequestQueue requestQueue = new IngrainRequestQueue();
                        requestQueue = GetFileRequestStatus(deployedModel.CorrelationId, CONSTANTS.IngestData);
                        if (requestQueue != null)
                        {
                            JObject paramArgs = JObject.Parse(requestQueue.ParamArgs);
                            paramArgs[CONSTANTS.Flag] = "Incremental";
                            if (paramArgs["metric"].ToString() != "null")
                            {
                                JObject metric = JObject.Parse(paramArgs["metric"].ToString());
                                if (Ingestdata_result[0].Contains("lastDateDict"))
                                    metric["startDate"] = Ingestdata_result[0]["lastDateDict"][0].ToString();
                                paramArgs["metric"] = JsonConvert.SerializeObject(metric, Formatting.None);
                            }
                            else if (paramArgs["pad"].ToString() != "null")
                            {
                                JObject pad = JObject.Parse(paramArgs["pad"].ToString());
                                if (Ingestdata_result[0].Contains("lastDateDict"))
                                    pad["startDate"] = Ingestdata_result[0]["lastDateDict"][0][0].ToString();
                                paramArgs["pad"] = JsonConvert.SerializeObject(pad, Formatting.None);
                            }
                            paramArgs["UniqueId"] = predictionDTO.UniqueId;
                            requestQueue.ParamArgs = paramArgs.ToString(Formatting.None);
                            requestQueue._id = Guid.NewGuid().ToString();
                            requestQueue.RequestId = Guid.NewGuid().ToString();
                            requestQueue.RequestStatus = CONSTANTS.New;
                            requestQueue.Status = CONSTANTS.Null;
                            requestQueue.Progress = CONSTANTS.Null;
                            requestQueue.Message = CONSTANTS.Null;
                            requestQueue.RetryCount = 0;
                            requestQueue.CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                            requestQueue.ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                            string requestId = requestQueue.RequestId;

                            InsertRequests(requestQueue);
                            bool flag = true;
                            Thread.Sleep(2000);

                            while (flag)
                            {
                                IngrainRequestQueue requestQueue1 = new IngrainRequestQueue();
                                requestQueue1 = GetFileRequestStatusByRequestId(deployedModel.CorrelationId, CONSTANTS.IngestData, requestId);
                                if (requestQueue1 != null)
                                {

                                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(GetPrediction), requestQueue1.Message + requestQueue1.Status + "  CorrelationId  " + requestQueue1.CorrelationId + "  Reuest id  " + requestQueue1.RequestId, string.Empty, string.Empty, requestQueue.ClientID, requestQueue.DeliveryconstructId);
                                    if (requestQueue1.Status == CONSTANTS.C && requestQueue1.Progress == CONSTANTS.Hundred)
                                    {
                                        flag = false;
                                        var builder = Builders<BsonDocument>.Filter;
                                        var filter = builder.Eq(CONSTANTS.CorrelationId, predictionDTO.CorrelationId) & builder.Eq(CONSTANTS.UniqueId, predictionDTO.UniqueId);
                                        var projection = Builders<BsonDocument>.Projection.Exclude("_id");
                                        var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_PublishModel);
                                        var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();
                                        if (result.Count > 0)
                                        {
                                            if (deployedModel.DBEncryptionRequired)
                                            {
                                                result[0]["ActualData"] = _encryptionDecryption.Decrypt(result[0]["ActualData"].ToString());
                                            }
                                            JArray Jdata = JArray.Parse(result[0]["ActualData"].ToString());
                                            string jsonString = JsonConvert.SerializeObject(Jdata);
                                            string data = jsonString.Replace("null", @"""""");
                                            var update = Builders<BsonDocument>.Update.Set("ActualData", data);
                                            var isUpdated = collection.UpdateMany(filter, update);
                                        }
                                        this.insertRequest(deployedModel, predictionDTO.UniqueId, FunctionType);
                                        IsPredictedCompeted(predictionDTO);
                                    }

                                    else if (requestQueue1.Status == CONSTANTS.E)
                                    {
                                        flag = false;
                                        IngrainResponseData CallBackResponsedata = new IngrainResponseData
                                        {
                                            CorrelationId = deployedModel.CorrelationId,
                                            Status = requestQueue1.Status,
                                            Message = string.Empty,
                                            ErrorMessage = requestQueue1.Message
                                        };
                                        //CallbackResponse(CallBackResponsedata, templatedata.ApplicationName, ResponseCallbackUrl);
                                    }
                                    else
                                    {
                                        Thread.Sleep(1000);
                                        flag = true;
                                    }
                                }

                            }
                        }
                    }
                }
                IngrainResponseData CallBackResponse = new IngrainResponseData
                {
                    CorrelationId = deployedModel.CorrelationId,
                    Status = _modelPrediction.Status,
                    Message = _modelPrediction.Message,
                    ErrorMessage = _modelPrediction.ErrorMessage,
                };
                //string callbackResonse = CallbackResponse(CallBackResponse, templatedata.ApplicationName, ResponseCallbackUrl);
            }
            return modelTraining;
        }

        private string getIncrementalData(string CorrelationId, PublicTemplateMapping templatedata, string UseCaseID)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(GetActualData), "Get Incremental data for prediction", string.IsNullOrEmpty(CorrelationId) ? default(Guid) : new Guid(CorrelationId), templatedata.ApplicationID, string.Empty, string.Empty, string.Empty);

            string resultString = string.Empty;
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.PS_IngestedData);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, CorrelationId);
            var Projection = Builders<BsonDocument>.Projection.Include("lastDateDict").Exclude("_id");
            var Ingestdata = collection.Find(filter).Project<BsonDocument>(Projection).ToList();

            if (Ingestdata.Count > 0)
            {

                PublicTemplateMapping Mapping = new PublicTemplateMapping()
                {
                    InputParameters = JsonConvert.SerializeObject(templatedata.InputParameters)
                };
                var data = JsonConvert.DeserializeObject(Mapping.InputParameters);
                //data[templatedata.DateColumn] = Ingestdata[0]["lastDateDict"][0][0].ToString();
                data["DateColumn"] = Ingestdata[0]["lastDateDict"][0][0].ToString();

                var collection1 = _database.GetCollection<BsonDocument>(CONSTANTS.PublicTemplateMapping);
                var filter1 = Builders<BsonDocument>.Filter.Eq("UsecaseName", templatedata.UsecaseName)
                    & Builders<BsonDocument>.Filter.Eq("ApplicationName", templatedata.ApplicationName);
                var Projection1 = Builders<BsonDocument>.Projection.Exclude("_id");
                var result = collection1.Find(filter1).Project<BsonDocument>(Projection1).FirstOrDefault();
                bool DBEncryptionRequired = CommonUtility.EncryptDB(CorrelationId, appSettings);
                if (result != null)
                {
                    BsonDocument bsons = BsonDocument.Parse(JsonConvert.SerializeObject(data));
                    var update = Builders<BsonDocument>.Update
                        .Set("InputParameters", bsons)
                        .Set("ModifiedByUser", DBEncryptionRequired ? _encryptionDecryption.Encrypt(appSettings.Value.username) : appSettings.Value.username)
                        .Set("ModifiedOn", DateTime.UtcNow.ToString());
                    collection1.UpdateOne(filter1, update);
                }
                string token = CustomUrlToken(templatedata.ApplicationName);

                LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(GetActualData), " CustomURL Token" + token, string.IsNullOrEmpty(CorrelationId) ? default(Guid) : new Guid(CorrelationId), templatedata.ApplicationID, string.Empty, string.Empty, string.Empty);
                if (token != null)
                {
                    var RequestParameters = JsonConvert.SerializeObject(templatedata.InputParameters);
                    var client = new RestClient(templatedata.SourceURL);
                    var request = new RestRequest(Method.POST);
                    request.AddHeader("cache-control", "no-cache");
                    request.AddHeader("content-type", "application/json");
                    request.AddHeader(CONSTANTS.Authorization, CONSTANTS.bearer + token);
                    request.AddParameter("application/json; charset=utf-8", RequestParameters,
                       ParameterType.RequestBody);
                    IRestResponse response = client.Execute(request);
                    resultString = response.Content;
                }
            }
            return resultString;
        }
        public string UpdateGenericModelMandatoryDetails(string data)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(UpdateGenericModelMandatoryDetails), "UpdateGenericModelMandatoryDetails - Started :", string.Empty, string.Empty, string.Empty, string.Empty);
            UpdatePublicTemplatedata Updatedata = new UpdatePublicTemplatedata();
            Updatedata = JsonConvert.DeserializeObject<UpdatePublicTemplatedata>(data);
            string ApplicationName = Updatedata.ApplicationName;
            string UsecaseName = Updatedata.UsecaseName;
            AppIntegrationsCredentials appIntegrationsCredentials = new AppIntegrationsCredentials()
            {
                grant_type = appSettings.Value.Grant_Type,
                client_id = appSettings.Value.clientId,
                client_secret = appSettings.Value.clientSecret,
                resource = Updatedata.Resource
            };
            string status = string.Empty;
            PublicTemplateMapping templateMapping = new PublicTemplateMapping()
            {
                SourceURL = Updatedata.SourceURL,
                InputParameters = JsonConvert.SerializeObject(Updatedata.InputParameters)
            };
            if (ApplicationName != null)
            {
                var collection = _database.GetCollection<AppIntegration>(CONSTANTS.AppIntegration);
                var filter = Builders<AppIntegration>.Filter.Eq("ApplicationName", ApplicationName);
                var Projection = Builders<AppIntegration>.Projection.Exclude("_id");
                var Appresult = collection.Find(filter).Project<AppIntegration>(Projection).ToList();

                if (Appresult.Count > 0)
                {
                    var update = Builders<AppIntegration>.Update
                 .Set(x => x.Authentication, appSettings.Value.authProvider)
                 .Set(x => x.TokenGenerationURL, _encryptionDecryption.Encrypt(appSettings.Value.token_Url))
                 .Set(x => x.Credentials, (IEnumerable)(_encryptionDecryption.Encrypt(JsonConvert.SerializeObject(appIntegrationsCredentials))))
                 .Set(x => x.ModifiedByUser, _encryptionDecryption.Encrypt(Convert.ToString(appSettings.Value.username)))
                 .Set(x => x.ModifiedOn, DateTime.UtcNow.ToString());
                    collection.UpdateOne(filter, update);
                    status = "Updated Successfully";
                }
            }

            if (UsecaseName != null)
            {
                var collection = _database.GetCollection<BsonDocument>(CONSTANTS.PublicTemplateMapping);
                var filter = Builders<BsonDocument>.Filter.Eq("UsecaseName", UsecaseName)
                    & Builders<BsonDocument>.Filter.Eq("ApplicationName", ApplicationName);
                var Projection = Builders<BsonDocument>.Projection.Exclude("_id");
                var result = collection.Find(filter).Project<BsonDocument>(Projection).FirstOrDefault();
                if (result != null)
                {
                    BsonDocument bsons = BsonDocument.Parse(JsonConvert.SerializeObject(Updatedata.InputParameters));
                    var update = Builders<BsonDocument>.Update
                        .Set("SourceURL", Updatedata.SourceURL)
                        .Set("InputParameters", bsons)
                        .Set("DateColumn", Updatedata.DateColumn)
                        .Set("ModifiedByUser", _encryptionDecryption.Encrypt(appSettings.Value.username))
                        .Set("ModifiedOn", DateTime.UtcNow.ToString());
                    collection.UpdateOne(filter, update);
                    status = "Updated Successfully";
                }
            }
            return status;

        }
        public List<IngrainResponseData> GetCallBackUrlData(string AppId, string UsecaseId, string ClientId, string DCID)
        {
            List<IngrainResponseData> lstInputData = new List<IngrainResponseData>();

            var dbCollection = _database.GetCollection<IngrainResponseData>(CONSTANTS.CallBackResponseData);


            var filterBuilder = Builders<IngrainResponseData>.Filter;
            var filterQueue = filterBuilder.Eq("UseCaseUId", UsecaseId) & filterBuilder.Eq("AppServiceUId", AppId) & filterBuilder.Eq("ClientUId", ClientId)
                & filterBuilder.Eq("DeliveryConstructUId", DCID);
            var projectionScenario = Builders<IngrainResponseData>.Projection.Exclude(CONSTANTS.Id);
            var Result = dbCollection.Find(filterQueue).Project<IngrainResponseData>(projectionScenario).ToList();

            return Result;
        }
        public GenericDataResponse GetCascadeModelTemplateTraining(string ClientUID, string DCID, string UserId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(GetCascadeModelTemplateTraining), "Cascade ModelTemplate Training - Started :", string.Empty, string.Empty, ClientUID, DCID);
            List<PublicTemplateMapping> MappingResult = new List<PublicTemplateMapping>();

            MappingResult = GetDataMapping(ClientUID, true);

            string encryptedUser = UserId;
            if (!string.IsNullOrEmpty(Convert.ToString(UserId)))
            {
                encryptedUser = _encryptionDecryption.Encrypt(Convert.ToString(UserId));
            }
            if (MappingResult != null)
            {
                if (MappingResult.Count > 0)
                {
                    foreach (var templatedata in MappingResult)
                    {
                        var queueCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
                        var filterBuilder = Builders<BsonDocument>.Filter;
                        var filterQueue = filterBuilder.Eq("ClientId", ClientUID) & filterBuilder.Eq("DeliveryconstructId", DCID) & filterBuilder.Eq("Function", CONSTANTS.CascadingModel) & filterBuilder.Eq("TemplateUseCaseID", templatedata.UsecaseID) & (filterBuilder.Eq("CreatedByUser", UserId) | filterBuilder.Eq("CreatedByUser", encryptedUser));
                        var queueResult = queueCollection.Find(filterQueue).FirstOrDefault();

                        if (queueResult == null)
                        {
                            if (CommonUtility.EncryptDB(templatedata.UsecaseID, appSettings))
                            {
                                if (!string.IsNullOrEmpty(Convert.ToString(UserId)))
                                {
                                    UserId = _encryptionDecryption.Encrypt(Convert.ToString(UserId));
                                }
                            }
                            string NewModelCorrelationID = Guid.NewGuid().ToString();

                            IngrainRequestQueue ingrainRequest = new IngrainRequestQueue
                            {
                                _id = Guid.NewGuid().ToString(),
                                CorrelationId = NewModelCorrelationID,
                                RequestId = Guid.NewGuid().ToString(),
                                ProcessId = CONSTANTS.Null,
                                ApplicationName = templatedata.ApplicationName,
                                ModelName = templatedata.ApplicationName + "_" + templatedata.UsecaseName,
                                RetryCount = 0,
                                ProblemType = CONSTANTS.Null,
                                UniId = CONSTANTS.Null,
                                InstaID = CONSTANTS.Null,
                                Progress = CONSTANTS.Null,
                                pageInfo = CONSTANTS.CascadingModel,
                                ParamArgs = CONSTANTS.Null,
                                TemplateUseCaseID = templatedata.UsecaseID,
                                Function = CONSTANTS.CascadingModel,
                                CreatedByUser = UserId,
                                CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                                ModifiedByUser = CONSTANTS.Null,
                                ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                                LastProcessedOn = CONSTANTS.Null,
                                AppID = templatedata.ApplicationID,
                                ClientId = ClientUID,
                                DeliveryconstructId = DCID,
                                UseCaseID = CONSTANTS.Null,
                                EstimatedRunTime = CONSTANTS.Null
                            };
                            bool isdata = CheckRequiredCascadeDetails(templatedata.ApplicationName, templatedata.UsecaseID);
                            if (isdata)
                            {
                                ingrainRequest.Status = CONSTANTS.Null;
                                ingrainRequest.RequestStatus = CONSTANTS.New;
                                ingrainRequest.Message = CONSTANTS.Null;
                            }
                            else
                            {
                                ingrainRequest.Status = CONSTANTS.E;
                                ingrainRequest.RequestStatus = CONSTANTS.ErrorMessage;
                                ingrainRequest.Message = "The Model is created using File Upload. Please contact Ingrain support team to train it.";
                                return _GenericDataResponse;
                            }
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(PublicTemplateModelTraning), "Cascade Auto Train function initiated TemplateID:" + templatedata.UsecaseID + "  NewCorrelationID :" + NewModelCorrelationID + " AppName" + templatedata.ApplicationName + " UsecaseName " + templatedata.UsecaseName, string.IsNullOrEmpty(ingrainRequest.CorrelationId) ? default(Guid) : new Guid(ingrainRequest.CorrelationId), string.Empty, string.Empty, ingrainRequest.ClientID, ingrainRequest.DeliveryconstructId);
                            InsertRequests(ingrainRequest);
                            _GenericDataResponse.Message = CONSTANTS.TrainingResponse;
                            _GenericDataResponse.Status = CONSTANTS.C;
                        }
                        else
                        {
                            _GenericDataResponse.Message = CONSTANTS.TrainingMessage;
                            _GenericDataResponse.Status = CONSTANTS.I;
                        }
                    }
                }
            }

            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(PublicTemplateModelTraning), "Cascade ModelTemplate Training - Ended :", string.Empty, string.Empty, ClientUID, DCID);
            return _GenericDataResponse;
        }
        private bool CheckRequiredCascadeDetails(string ApplicationName, string UsecaseID)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), "CheckRequiredCascadeDetails", "START", string.Empty, string.Empty, string.Empty, string.Empty);
            var AppIntegCollection = _database.GetCollection<AppIntegration>(CONSTANTS.AppIntegration);
            var filterBuilder = Builders<AppIntegration>.Filter;
            var AppFilter = filterBuilder.Eq("ApplicationName", ApplicationName);

            var Projection = Builders<AppIntegration>.Projection.Include("BaseURL").Include("Authentication").Include("TokenGenerationURL").Include("Credentials").Exclude("_id");
            var AppData = AppIntegCollection.Find(AppFilter).Project<AppIntegration>(Projection).FirstOrDefault();

            var MappingCollection = _database.GetCollection<PublicTemplateMapping>(CONSTANTS.PublicTemplateMapping);
            var Builder = Builders<PublicTemplateMapping>.Filter;
            var Projection1 = Builders<PublicTemplateMapping>.Projection.Exclude("_id");
            var filter = Builder.Eq("ApplicationName", ApplicationName) & Builder.Eq("UsecaseID", UsecaseID);
            var templatedata = MappingCollection.Find(filter).Project<PublicTemplateMapping>(Projection1).FirstOrDefault();

            if (AppData != null && templatedata != null)
            {
                if (templatedata.SourceName == "Custom" || templatedata.SourceName == null)
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), "CheckRequiredCascadeDetails", "END", string.Empty, string.Empty, string.Empty, string.Empty);
                    return true;
                }
            }
            else
            {
                return false;
            }
            return true;

        }

        /// <summary>
        /// Audit Trail Log
        /// </summary>
        /// <param name="correlationId">Correlation Id</param>
        /// <returns></returns>
        public List<CallBackErrorLog> GetAuditTrailLog(string correlationId)
        {
            var logCollection = _database.GetCollection<CallBackErrorLog>(CONSTANTS.AuditTrailLog);
            var filterBuilder = Builders<CallBackErrorLog>.Filter;
            var filter = filterBuilder.Eq("CorrelationId", correlationId);
            var Projection = Builders<CallBackErrorLog>.Projection.Exclude("_id");
            var logResult = logCollection.Find(filter).Project<CallBackErrorLog>(Projection).ToList();
            if (logResult.Count > 0)
            {
                foreach (var log in logResult)
                {
                    bool DBEncryptionRequired = CommonUtility.EncryptDB(log.CorrelationId, appSettings);
                    if (DBEncryptionRequired)
                    {
                        try
                        {
                            if (!string.IsNullOrEmpty(Convert.ToString(log.CreatedBy)))
                            {
                                log.CreatedBy = _encryptionDecryption.Decrypt(Convert.ToString(log.CreatedBy));
                            }
                        }
                        catch (Exception ex) { LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(GetAuditTrailLog) + "User is already decrypted....", ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty); }
                        try
                        {
                            if (!string.IsNullOrEmpty(Convert.ToString(log.ModifiedBy)))
                            {
                                log.ModifiedBy = _encryptionDecryption.Decrypt(Convert.ToString(log.ModifiedBy));
                            }
                        }
                        catch (Exception ex) { LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(GetAuditTrailLog) + "User is already decrypted....", ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty); }
                    }
                }
            }
            return logResult;
        }

        public void AuditTrailLog(CallBackErrorLog auditTrailLog)
        {
            string result = string.Empty;
            string isNoficationSent = "false";
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

            auditTrailLog.CallbackURLResponse = result;//CallBackResponse,
            auditTrailLog.CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
            auditTrailLog.ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
            var logCollection = _database.GetCollection<CallBackErrorLog>(CONSTANTS.AuditTrailLog);
            logCollection.InsertOneAsync(auditTrailLog);
            //update ingrai Request collection
            if (auditTrailLog.BaseAddress != null && auditTrailLog.httpResponse != null)
            {
                UpdateIngrainRequest(auditTrailLog.RequestId, auditTrailLog.Message + "" + result, isNoficationSent, auditTrailLog.Status);

            }
        }

        public void UpdateIngrainRequest(string requestId, string notificationMessage, string isNoficationSent, string sendnotification)
        {
            var requestCollection = _database.GetCollection<IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
            var filter = Builders<IngrainRequestQueue>.Filter.Where(x => x.RequestId == requestId);
            var update = Builders<IngrainRequestQueue>.Update.Set(x => x.IsNotificationSent, isNoficationSent).Set(x => x.NotificationMessage, notificationMessage).Set(x => x.SendNotification, sendnotification).Set(x => x.ModifiedOn, DateTime.Now.ToString(CONSTANTS.DateHoursFormat));
            requestCollection.UpdateOne(filter, update);
            #endregion
        }
        public List<FMModelTrainingStatus> GetFMModelStatus(string clientid, string dcid, string userid)
        {
            List<FMModelTrainingStatus> mappingDetails = new List<FMModelTrainingStatus>();
            string encryptedUser = userid;
            if (!string.IsNullOrEmpty(Convert.ToString(userid)))
                encryptedUser = _encryptionDecryption.Encrypt(Convert.ToString(userid));
            var queueCollection = _database.GetCollection<IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
            var projection = Builders<IngrainRequestQueue>.Projection.Exclude(CONSTANTS.Id);
            var filterBuilder = Builders<IngrainRequestQueue>.Filter;
            var filterQueue = filterBuilder.Eq(CONSTANTS.ClientId, clientid) & filterBuilder.Eq(CONSTANTS.DeliveryconstructId, dcid) & filterBuilder.Eq(CONSTANTS.Function, CONSTANTS.FMTransform) & filterBuilder.Eq(CONSTANTS.TemplateUseCaseID, CONSTANTS.FMUseCaseId) & (filterBuilder.Eq(CONSTANTS.CreatedByUser, userid) | filterBuilder.Eq(CONSTANTS.CreatedByUser, encryptedUser));
            var queueResult = queueCollection.Find(filterQueue).Project<IngrainRequestQueue>(projection).ToList();
            if (queueResult.Count > 0)
            {
                foreach (var item in queueResult)
                {
                    bool DBEncryptionRequired = CommonUtility.EncryptDB(item.CorrelationId, appSettings);
                    if (DBEncryptionRequired)
                    {
                        try
                        {
                            if (!string.IsNullOrEmpty(Convert.ToString(item.CreatedByUser)))
                            {
                                item.CreatedByUser = _encryptionDecryption.Decrypt(Convert.ToString(item.CreatedByUser));
                            }
                        }
                        catch (Exception ex) { LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(GetFMModelStatus) + "User is already decrypted....", ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty); }
                    }
                    FMModelTrainingStatus fMModelTraining = new FMModelTrainingStatus();
                    fMModelTraining.Status = item.Status;
                    fMModelTraining.Progress = item.Progress;
                    fMModelTraining.ClientId = item.ClientId;
                    fMModelTraining.CreatedByUser = item.CreatedByUser;
                    fMModelTraining.CreatedOn = item.CreatedOn;
                    fMModelTraining.DeliveryconstructId = item.DeliveryconstructId;
                    fMModelTraining.CorrelationId = item.CorrelationId;
                    fMModelTraining.Message = item.Message;
                    fMModelTraining.FMCorrelationId = item.FMCorrelationId;
                    if (item.ModelName != null)
                    {
                        string[] modelname = item.ModelName.Split("_");
                        if (modelname != null && modelname.Length > 0)
                            fMModelTraining.ModelName = modelname[0];
                    }
                    if (item.Status == null || item.Status == CONSTANTS.BsonNull || string.IsNullOrEmpty(item.Status))
                    {
                        fMModelTraining.Status = "I";
                        fMModelTraining.Message = "Training is in progress";
                    }
                    mappingDetails.Add(fMModelTraining);
                }
            }
            return mappingDetails;
        }

        private IngrainRequestQueue GetIngrainRequestDetails(string correlationId, string applicationId, string usecaseId)
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
        public DataPoints UpdateDataPoints(long UsecasedataPoints, long AppDataPoints, string usecaseId, string ApplicationId)
        {
            DataPoints dataPoints = new DataPoints();
            if (!string.IsNullOrEmpty(usecaseId))
            {
                var collection = _database.GetCollection<BsonDocument>(CONSTANTS.PublicTemplateMapping);
                var filter = Builders<BsonDocument>.Filter.Eq("UsecaseID", usecaseId);
                var update = Builders<BsonDocument>.Update.Set(CONSTANTS.DataPoints, UsecasedataPoints);
                var updateresult = collection.UpdateMany(filter, update);
                if (updateresult.ModifiedCount > 0)
                {
                    dataPoints.Message = CONSTANTS.UpdatedRecords;
                    dataPoints.IsUpdated = true;
                }
            }

            //Updating App Collection
            if (!string.IsNullOrEmpty(ApplicationId))
            {
                var appCollection = _database.GetCollection<BsonDocument>(CONSTANTS.AppIntegration);
                var appFilter = Builders<BsonDocument>.Filter.Eq("ApplicationID", ApplicationId);
                var appUpdate = Builders<BsonDocument>.Update.Set(CONSTANTS.DataPoints, AppDataPoints);
                dataPoints.IsUpdated = false;
                var appUpdateresult = appCollection.UpdateOne(appFilter, appUpdate);
                if (appUpdateresult.ModifiedCount > 0)
                {
                    dataPoints.Message = CONSTANTS.UpdatedRecords;
                    dataPoints.IsUpdated = true;
                }
            }
            return dataPoints;
        }
    }
}
