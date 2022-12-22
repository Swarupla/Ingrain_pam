using Accenture.MyWizard.Ingrain.BusinessDomain.Common;
using Accenture.MyWizard.Ingrain.DataAccess;
using Accenture.MyWizard.Ingrain.DataModels.Models;
using Accenture.MyWizard.Shared.Helpers;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Accenture.MyWizard.Cryptography.EncryptionProviders;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
using System.Net.Http;
using System.Net.Http.Headers;
using RestSharp;
using System.Net;
using System.Linq;

namespace Accenture.MyWizard.Ingrain.BusinessDomain.Services
{
    public class SPAVelocityService : ISPAVelocityService
    {
        #region Members
        private MongoClient _mongoClient;
        private IMongoDatabase _database;
        private readonly IOptions<IngrainAppSettings> appSettings;
        DatabaseProvider databaseProvider;
        private IEncryptionDecryption _encryptionDecryption;
        private IDeployedModelService _deployedModelService;
        private VelocityTrainingStatus _trainingStatus;
        private VelocityTraining _velocityTraining;
        private IGenericSelfservice _genericSelfservice;
        private CallBackErrorLog CallBackErrorLog;
        private long _dataPointCount;
        private string _dataPointsWarning;
        private IFlaskAPI _iFlaskAPIService;
        #endregion

        #region Constructor
        public SPAVelocityService(DatabaseProvider db, IOptions<IngrainAppSettings> settings, IServiceProvider serviceProvider)
        {
            databaseProvider = db;
            appSettings = settings;
            _mongoClient = databaseProvider.GetDatabaseConnection();
            var dataBaseName = MongoUrl.Create(appSettings.Value.connectionString).DatabaseName;
            _database = _mongoClient.GetDatabase(dataBaseName);
            _encryptionDecryption = serviceProvider.GetService<IEncryptionDecryption>();
            _deployedModelService = serviceProvider.GetService<IDeployedModelService>();
            _trainingStatus = new VelocityTrainingStatus();
            _velocityTraining = new VelocityTraining();
            _genericSelfservice = serviceProvider.GetService<IGenericSelfservice>();
            CallBackErrorLog = new CallBackErrorLog();
            _iFlaskAPIService = serviceProvider.GetService<IFlaskAPI>();
        }
        #endregion

        #region Methods

        /// <summary>
        /// Start Training for the SPA UseCases
        /// </summary>
        /// <param name="RequestPayload"></param>
        /// <returns>Training Initiated</returns>
        public VelocityTraining StartVelocityTraining(Velocity RequestPayload)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(StartVelocityTraining), "InitiateTraining - Started :",string.Empty,string.Empty,RequestPayload.ClientUID,RequestPayload.DeliveryConstructUID);
            var AppIntegCollection = _database.GetCollection<AppIntegration>(CONSTANTS.AppIntegration);
            var filterBuilder = Builders<AppIntegration>.Filter;
            var AppFilter = filterBuilder.Eq(CONSTANTS.ApplicationID, RequestPayload.AppServiceUID);
            var Projection = Builders<AppIntegration>.Projection.Exclude(CONSTANTS.Id);
            var IsApplicationExist = AppIntegCollection.Find(AppFilter).Project<AppIntegration>(Projection).FirstOrDefault();
            string CorrelationId = string.Empty;
            if (IsApplicationExist != null)
            {
                if (UpdateTokenInAppIntegration(RequestPayload.AppServiceUID) == CONSTANTS.Success && UpdatePublicTemplateMapping(RequestPayload.AppServiceUID, RequestPayload.UseCaseUID, RequestPayload.QueryData) == CONSTANTS.Success)
                {
                    var MappingCollection = _database.GetCollection<PublicTemplateMapping>(CONSTANTS.PublicTemplateMapping);
                    var Builder = Builders<PublicTemplateMapping>.Filter;
                    var Projection1 = Builders<PublicTemplateMapping>.Projection.Exclude("_id");
                    //var filter = Builder.Eq(CONSTANTS.ApplicationID, RequestPayload.AppServiceUID) & Builder.Eq(CONSTANTS.UsecaseID, RequestPayload.UseCaseUID);
                    var filter = Builder.Eq(CONSTANTS.UsecaseID, RequestPayload.UseCaseUID);
                    var templatedata = MappingCollection.Find(filter).Project<PublicTemplateMapping>(Projection1).FirstOrDefault();
                    //  if (RequestPayload.UseCaseUID == "f0320924-2ee3-4398-ad7c-8bc172abd78d" || RequestPayload.UseCaseUID == "efd60535-5d15-46a8-961f-c43161e3a326" || RequestPayload.UseCaseUID == "49bf29ca-3408-4ae1-af4d-32963e18670a")//"which sowmya ll provide-team level") //to b replaced
                    if (templatedata != null)
                    {
                        //var templateAppId = string.Empty;
                        if (templatedata.IsMultipleApp == "yes")
                        {
                            if (templatedata.ApplicationIDs.Contains(RequestPayload.AppServiceUID))
                            {
                                templatedata.ApplicationID = RequestPayload.AppServiceUID;
                                templatedata.ApplicationName = IsApplicationExist.ApplicationName;
                                //templateAppId = templatedata.ApplicationID;
                            }
                        }
                        else
                        {
                            if (RequestPayload.AppServiceUID != templatedata.ApplicationID)
                            {

                                _velocityTraining.ErrorMessage = CONSTANTS.IsApplicationExist;
                                _velocityTraining.Status = "Error";
                                this.InsertCallBackErrorLog(RequestPayload, _velocityTraining);
                                return _velocityTraining;

                            }
                        }
                        if (RequestPayload.UseCaseUID == templatedata.UsecaseID && templatedata.UsecaseType == "Team Level")
                        {
                            if (RequestPayload.UseCaseUID == "f0320924-2ee3-4398-ad7c-8bc172abd78d" && string.IsNullOrEmpty(RequestPayload.TeamAreaUId))
                            {
                                _velocityTraining.ErrorMessage = "Please provide team level UID : TeamAreaUId";
                                _velocityTraining.Status = "Error";
                                this.InsertCallBackErrorLog(RequestPayload, _velocityTraining);
                                return _velocityTraining;
                                //throw new Exception("Please provide team level UID : TeamAreaUId");
                            }
                            //else
                            //{
                            //    _velocityTraining.ErrorMessage = "Please provide IsTeamLevelData : 1";
                            //    _velocityTraining.Status = "Error";
                            //    return _velocityTraining;
                            //}
                        }

                        if ((RequestPayload.UseCaseUID == "877f712c-7cdc-435a-acc8-8fea3d26cc18" || RequestPayload.UseCaseUID == "6b0d8eb3-0918-4818-9655-6ca81a4ebf30") && RequestPayload.IsTeamLevelData != "1")
                        {
                            {
                                _velocityTraining.ErrorMessage = "Please provide IsTeamLevelData : 1";
                                _velocityTraining.Status = "Error";
                                this.InsertCallBackErrorLog(RequestPayload, _velocityTraining);
                                return _velocityTraining;
                            }
                        }

                        var _trainingStatus = GetModelStatus(RequestPayload.ClientUID, RequestPayload.DeliveryConstructUID, RequestPayload.AppServiceUID, RequestPayload.UseCaseUID, RequestPayload.UserID, RequestPayload.TeamAreaUId, templatedata.UsecaseType, templatedata.IsMultipleApp, templatedata.ApplicationIDs);
                        string NewModelCorrelationID = Guid.NewGuid().ToString();
                        string requestId = string.Empty;
                        if (_trainingStatus == null)
                        {
                            if (IsAmbulanceLane(RequestPayload.UseCaseUID) && RequestPayload.RetrainRequired == "AutoRetrain")
                            {
                                _velocityTraining.ErrorMessage = "Training is not yet initiated .Please retrain once Training is completed";
                                _velocityTraining.Status = "Error";
                                this.InsertCallBackErrorLog(RequestPayload, _velocityTraining);
                                return _velocityTraining;
                            }
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(StartVelocityTraining), "TRAINING INITIATING - STARTED :", string.IsNullOrEmpty(NewModelCorrelationID) ? default(Guid) : new Guid(NewModelCorrelationID), string.Empty, string.Empty, RequestPayload.ClientUID, RequestPayload.DeliveryConstructUID);
                            var ingrainRequest = InsertSPAPrediction(NewModelCorrelationID, templatedata, RequestPayload);
                            InsertRequests(ingrainRequest);
                            requestId = ingrainRequest.RequestId;
                            CorrelationId = ingrainRequest.CorrelationId;
                            IngrainResponseData CallBackResponse = new IngrainResponseData
                            {
                                CorrelationId = ingrainRequest.CorrelationId,
                                Status = "Initiated",
                                Message = CONSTANTS.TrainingInitiated,
                                ErrorMessage = string.Empty,
                            };
                            string callbackResonse = CallbackResponse(CallBackResponse, templatedata.ApplicationName, RequestPayload.ResponseCallbackUrl, ingrainRequest.ClientId, ingrainRequest.DeliveryconstructId, ingrainRequest.AppID, RequestPayload.UseCaseUID, ingrainRequest.RequestId, ingrainRequest.CreatedByUser);
                            _velocityTraining.Message = CONSTANTS.TrainingInitiated;
                            _velocityTraining.CorrelationId = ingrainRequest.CorrelationId;
                            _velocityTraining.Status = CONSTANTS.P;
                            if (callbackResonse == CONSTANTS.ErrorMessage)
                            {
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(StartVelocityTraining), "CALL BACK URL ERROR :" + callbackResonse, string.Empty, string.Empty, RequestPayload.ClientUID, RequestPayload.DeliveryConstructUID);
                                _velocityTraining.ErrorMessage = CONSTANTS.TokenFailed;
                                _velocityTraining.Status = CONSTANTS.ErrorMessage;
                                return _velocityTraining;
                            }
                            return _velocityTraining;
                        }
                        else
                        {
                            requestId = _trainingStatus.UniqueId;
                            if (_trainingStatus.Status == null || _trainingStatus.Status == CONSTANTS.E)
                            {
                                if (IsAmbulanceLane(RequestPayload.UseCaseUID) && RequestPayload.RetrainRequired == "AutoRetrain")
                                {
                                    _velocityTraining.CorrelationId = _trainingStatus.CorrelationId;
                                    _velocityTraining.ErrorMessage = "Training is not Complete .Please retrain once Training is completed";
                                    _velocityTraining.Status = "Error";
                                    return _velocityTraining;
                                }
                                if (!string.IsNullOrEmpty(_trainingStatus.CorrelationId))
                                {
                                    NewModelCorrelationID = _trainingStatus.CorrelationId;
                                }
                                if (_trainingStatus.Status == CONSTANTS.E)
                                {
                                    NewModelCorrelationID = Guid.NewGuid().ToString();
                                    _velocityTraining.Status = CONSTANTS.ErrorMessage;
                                    _velocityTraining.CorrelationId = _trainingStatus.CorrelationId;
                                    //_velocityTraining.ErrorMessage = _trainingStatus.ErrorMessage;
                                    _velocityTraining.Message = CONSTANTS.Error;
                                    var ingrainRequest = InsertSPAPrediction(NewModelCorrelationID, templatedata, RequestPayload);
                                    InsertRequests(ingrainRequest);
                                    CorrelationId = ingrainRequest.CorrelationId;
                                    IngrainResponseData CallBackResponse = new IngrainResponseData
                                    {
                                        CorrelationId = ingrainRequest.CorrelationId,
                                        Status = "Initiated",
                                        Message = CONSTANTS.TrainingInitiated + " ,as previous request " + _trainingStatus.CorrelationId +" failed with Error :" + _trainingStatus.ErrorMessage,
                                        ErrorMessage = string.Empty,
                                    };
                                    string callbackResonse = CallbackResponse(CallBackResponse, templatedata.ApplicationName, RequestPayload.ResponseCallbackUrl, RequestPayload.ClientUID, RequestPayload.DeliveryConstructUID, RequestPayload.AppServiceUID, RequestPayload.UseCaseUID, requestId, RequestPayload.UserID);
                                    _velocityTraining.Message = CallBackResponse.Message;
                                    _velocityTraining.CorrelationId = ingrainRequest.CorrelationId;
                                    _velocityTraining.Status = CONSTANTS.P;
                                   return _velocityTraining;
                                }
                                else
                                {
                                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(StartVelocityTraining), "_trainingStatus :" + NewModelCorrelationID + "--" + _trainingStatus.Status + "--NEW--" + NewModelCorrelationID, string.Empty, string.Empty, RequestPayload.ClientUID,RequestPayload.DeliveryConstructUID);
                                    var ingrainRequest = InsertSPAPrediction(NewModelCorrelationID, templatedata, RequestPayload);
                                    InsertRequests(ingrainRequest);
                                    CorrelationId = ingrainRequest.CorrelationId;
                                    IngrainResponseData CallBackResponse = new IngrainResponseData
                                    {
                                        CorrelationId = ingrainRequest.CorrelationId,
                                        Status = "Initiated",
                                        Message = CONSTANTS.TrainingInitiated,
                                        ErrorMessage = string.Empty,
                                    };
                                    string callbackResonse = CallbackResponse(CallBackResponse, templatedata.ApplicationName, RequestPayload.ResponseCallbackUrl, RequestPayload.ClientUID, RequestPayload.DeliveryConstructUID, RequestPayload.AppServiceUID, RequestPayload.UseCaseUID, requestId, RequestPayload.UserID);
                                    _velocityTraining.Message = CONSTANTS.TrainingInitiated;
                                    _velocityTraining.CorrelationId = ingrainRequest.CorrelationId;
                                    _velocityTraining.Status = CONSTANTS.P;
                                    if (callbackResonse == CONSTANTS.ErrorMessage)
                                    {
                                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(StartVelocityTraining), "CALL BACK URL ERROR :" + callbackResonse, string.Empty, string.Empty, RequestPayload.ClientUID,RequestPayload.DeliveryConstructUID);
                                        _velocityTraining.ErrorMessage = CONSTANTS.TokenFailed;
                                        _velocityTraining.Status = CONSTANTS.ErrorMessage;
                                        return _velocityTraining;
                                    }
                                    return _velocityTraining;
                                }
                            }
                            else
                            {

                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(StartVelocityTraining), "_trainingStatus:" + _trainingStatus.CorrelationId + "--" + _trainingStatus.Status, string.Empty, string.Empty, RequestPayload.ClientUID, RequestPayload.DeliveryConstructUID);
                                if (_trainingStatus.Status == CONSTANTS.P || _trainingStatus.Status == CONSTANTS.I)
                                {
                                    if (IsAmbulanceLane(RequestPayload.UseCaseUID) && RequestPayload.RetrainRequired == "AutoRetrain")
                                    {
                                        _velocityTraining.CorrelationId = _trainingStatus.CorrelationId;
                                        _velocityTraining.ErrorMessage = "Training is not yet Complete .Please retrain once Training is completed";
                                        _velocityTraining.Status = "Error";
                                        this.InsertCallBackErrorLog(RequestPayload, _velocityTraining);
                                        return _velocityTraining;
                                    }
                                    _velocityTraining.Status = _trainingStatus.Status;
                                    _velocityTraining.Message = CONSTANTS.TrainingInprogress;
                                    _velocityTraining.CorrelationId = _trainingStatus.CorrelationId;
                                    return _velocityTraining;
                                }
                                else
                                {                                   
                                    if (IsAmbulanceLane(RequestPayload.UseCaseUID) && RequestPayload.RetrainRequired == "AutoRetrain")
                                    {
                                        var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
                                        var filteRetrain = Builders<BsonDocument>.Filter;
                                        FilterDefinition<BsonDocument> filterQueue = null;
                                        filterQueue = filteRetrain.Eq(CONSTANTS.ClientId, RequestPayload.ClientUID) & filteRetrain.Eq(CONSTANTS.DeliveryconstructId, RequestPayload.DeliveryConstructUID) & filteRetrain.Eq(CONSTANTS.TemplateUseCaseID, RequestPayload.UseCaseUID) & filteRetrain.Eq(CONSTANTS.AppID, RequestPayload.AppServiceUID) & filteRetrain.Eq(CONSTANTS.pageInfo, "AutoRetrain") & filteRetrain.Eq("CorrelationId", _trainingStatus.CorrelationId);
                                        var ProjectionRetrain = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
                                        var result = collection.Find(filterQueue).Project<BsonDocument>(ProjectionRetrain).ToList();
                                        result = result.OrderByDescending(item => item["CreatedOn"]).ToList();
                                        if (result.Count > 0)
                                        {
                                            //_trainingStatus.CorrelationId = result[0][CONSTANTS.CorrelationId].ToString();
                                            var correlationId = result[0][CONSTANTS.CorrelationId].ToString();

                                            var errorMessage = result[0][CONSTANTS.Message].ToString();
                                            var status = result[0][CONSTANTS.Status].ToString();
                                            var createdOn = result[0]["CreatedOn"].ToString();
                                            var uniqueId = result[0]["RequestId"].ToString();
                                            var createdByUser = result[0]["CreatedByUser"].ToString();
                                            //_trainingStatus.ErrorMessage = result[0][CONSTANTS.Message].ToString();
                                            if (status == "C")
                                            {
                                                var lastUpdateHour = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                                                DateTime createdOnDateFormat = DateTime.Parse(createdOn);
                                                DateTime todaysDate = DateTime.Parse(lastUpdateHour);
                                                var dateDiff = (todaysDate - createdOnDateFormat).TotalDays;
                                                if (dateDiff > 1)
                                                {
                                                    var req = InsertSPAPrediction(_trainingStatus.CorrelationId, templatedata, RequestPayload);
                                                    InsertRequests(req);
                                                    CorrelationId = req.CorrelationId;
                                                    requestId = req.RequestId;
                                                    IngrainResponseData ResponseCallBackResponse = new IngrainResponseData
                                                    {
                                                        CorrelationId = req.CorrelationId,
                                                        Status = "ReTrain - Initiated",
                                                        Message = CONSTANTS.ReTrainingInitiated,
                                                        ErrorMessage = string.Empty,
                                                    };
                                                    string ResponseCallBackResponseurl = CallbackResponse(ResponseCallBackResponse, templatedata.ApplicationName, RequestPayload.ResponseCallbackUrl, req.ClientId, req.DeliveryconstructId, req.AppID, RequestPayload.UseCaseUID, req.RequestId, req.CreatedByUser);
                                                    _velocityTraining.Message = CONSTANTS.ReTrainingInitiated;
                                                    _velocityTraining.CorrelationId = req.CorrelationId;
                                                    _velocityTraining.Status = CONSTANTS.P;
                                                    return _velocityTraining;
                                                }
                                                else
                                                {                                                    
                                                    IngrainResponseData ResponseCallBackResponse = new IngrainResponseData
                                                    {
                                                        CorrelationId = _trainingStatus.CorrelationId,
                                                        Status = "Completed",
                                                        Message = CONSTANTS.ReTrainingOneDayCheck,
                                                        ErrorMessage = CONSTANTS.ReTrainingOneDayCheck,
                                                        DataPointsCount = _dataPointCount,
                                                        DataPointsWarning = _dataPointsWarning

                                                    };                                                    
                                                    string ResponseCallBackResponseurl = CallbackResponse(ResponseCallBackResponse, templatedata.ApplicationName, RequestPayload.ResponseCallbackUrl, RequestPayload.ClientUID, RequestPayload.DeliveryConstructUID, RequestPayload.AppServiceUID, RequestPayload.UseCaseUID, uniqueId, createdByUser);

                                                    _velocityTraining.Message = CONSTANTS.ReTrainingOneDayCheck;
                                                    _velocityTraining.CorrelationId = _trainingStatus.CorrelationId;
                                                    _velocityTraining.Status = "Completed";
                                                    _velocityTraining.DataPointsCount = _dataPointCount;
                                                    _velocityTraining.DataPointsWarning = _dataPointsWarning;
                                                    return _velocityTraining;
                                                }
                                            }


                                            if (status == "E")
                                            {
                                                var queueCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
                                                var filterBuilder1 = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId) & filteRetrain.Eq("RequestId", uniqueId);
                                                queueCollection.DeleteMany(filterBuilder1);
                                                _velocityTraining.Status = CONSTANTS.ErrorMessage;
                                                _velocityTraining.CorrelationId = correlationId;
                                                _velocityTraining.ErrorMessage = errorMessage;
                                                _velocityTraining.Message = CONSTANTS.RetrainingUnsucess;
                                                IngrainResponseData callback = new IngrainResponseData
                                                {
                                                    CorrelationId = correlationId,
                                                    Status = CONSTANTS.ErrorMessage,
                                                    Message = CONSTANTS.RetrainingUnsucess,
                                                    ErrorMessage = errorMessage
                                                };
                                                string callbackRes = CallbackResponse(callback, templatedata.ApplicationName, RequestPayload.ResponseCallbackUrl, RequestPayload.ClientUID, RequestPayload.DeliveryConstructUID, RequestPayload.AppServiceUID, RequestPayload.UseCaseUID, requestId, RequestPayload.UserID);
                                                return _velocityTraining;
                                            }
                                            if (status != "E")
                                            {
                                                _velocityTraining.Status = status;
                                                _velocityTraining.Message = CONSTANTS.ReTrainingInprogress;
                                                _velocityTraining.CorrelationId = _trainingStatus.CorrelationId;
                                                return _velocityTraining;
                                            }
                                        }
                                        var ingrainRequest = InsertSPAPrediction(_trainingStatus.CorrelationId, templatedata, RequestPayload);
                                        InsertRequests(ingrainRequest);
                                        CorrelationId = ingrainRequest.CorrelationId;
                                        requestId = ingrainRequest.RequestId;
                                        IngrainResponseData CallBackResponse = new IngrainResponseData
                                        {
                                            CorrelationId = ingrainRequest.CorrelationId,
                                            Status = "ReTrain - Initiated",
                                            Message = CONSTANTS.ReTrainingInitiated,
                                            ErrorMessage = string.Empty,
                                        };
                                        string callbackResonse = CallbackResponse(CallBackResponse, templatedata.ApplicationName, RequestPayload.ResponseCallbackUrl, ingrainRequest.ClientId, ingrainRequest.DeliveryconstructId, ingrainRequest.AppID, RequestPayload.UseCaseUID, ingrainRequest.RequestId, ingrainRequest.CreatedByUser);
                                        _velocityTraining.Message = CONSTANTS.ReTrainingInitiated;
                                        _velocityTraining.CorrelationId = ingrainRequest.CorrelationId;
                                        _velocityTraining.Status = CONSTANTS.P;
                                        return _velocityTraining;
                                    }
                                    else
                                    {                                        
                                        if (RequestPayload.UseCaseUID == "f0320924-2ee3-4398-ad7c-8bc172abd78d")
                                        {
                                            if (templatedata.IsMultipleApp == "yes")
                                            {
                                                if (templatedata.ApplicationIDs.Contains(RequestPayload.AppServiceUID))
                                                {
                                                    var collection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
                                                    var builder = Builders<DeployModelsDto>.Filter;
                                                    var filter1 = builder.Eq(CONSTANTS.CorrelationId, _trainingStatus.CorrelationId);
                                                    var builder1 = Builders<DeployModelsDto>.Update;
                                                    var update = builder1.Set(CONSTANTS.IsMutipleApp, true);
                                                    collection.UpdateMany(filter1, update);
                                                }
                                            }

                                            IngrainResponseData CallBackResponse = new IngrainResponseData
                                            {
                                                CorrelationId = _trainingStatus.CorrelationId,
                                                Status = "Completed",
                                                Message = "Training Completed",
                                                ErrorMessage = string.Empty,
                                                DataPointsWarning  = _dataPointsWarning,
                                                DataPointsCount = _dataPointCount
                                            };                                            
                                            string callbackResonse = CallbackResponse(CallBackResponse, templatedata.ApplicationName, RequestPayload.ResponseCallbackUrl, RequestPayload.ClientUID, RequestPayload.DeliveryConstructUID, RequestPayload.AppServiceUID, RequestPayload.UseCaseUID, requestId, RequestPayload.UserID);
                                        }
                                        _velocityTraining.Status = "Completed";
                                        _velocityTraining.Message = "Training completed for the usecaseid";
                                        _velocityTraining.CorrelationId = _trainingStatus.CorrelationId;
                                        _velocityTraining.DataPointsCount = _dataPointCount;
                                        _velocityTraining.DataPointsWarning = _dataPointsWarning;
                                        return _velocityTraining;
                                    }
                                }
                            }
                        }

                    }
                    else
                    {
                        _velocityTraining.ErrorMessage = CONSTANTS.UsecaseNotAvailable;
                        _velocityTraining.Status = "Error";
                        this.InsertCallBackErrorLog(RequestPayload, _velocityTraining);
                        return _velocityTraining;
                    }
                }
                else
                {
                    _velocityTraining.ErrorMessage = "Error Updating AppIntegration,PublicTemplatingMapping";
                    _velocityTraining.Status = "Error";
                    this.InsertCallBackErrorLog(RequestPayload, _velocityTraining);
                    return _velocityTraining;
                }

            }
            else
            {
                _velocityTraining.ErrorMessage = CONSTANTS.IsApplicationExist;
                _velocityTraining.Status = "Error";
                return _velocityTraining;
            }
        }

        private void InsertCallBackErrorLog(Velocity velocity, VelocityTraining velocityTraining)
        {
            CallBackErrorLog auditTrailLog = new CallBackErrorLog()
            {
                ApplicationID = velocity.AppServiceUID,
                BaseAddress = velocity.ResponseCallbackUrl,
                httpResponse = null,
                ClientId = velocity.ClientUID,
                DCID = velocity.DeliveryConstructUID,
                UseCaseId = velocity.UseCaseUID,
                RequestId = null,
                CreatedBy = velocity.UserID,
                ProcessName = CONSTANTS.TrainingName,
                UsageType = CONSTANTS.AssetUsage,
                ErrorMessage = velocityTraining.ErrorMessage,
                Status = velocityTraining.Status,
                Message = velocityTraining.Message,
                CorrelationId = velocityTraining.CorrelationId,
                CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat)
            };

            this.AuditTrailLog(auditTrailLog);
        }

        public string UpdateTokenInAppIntegration(string appId)
        {
            AppIntegrationsCredentials appIntegrationsCredentials = new AppIntegrationsCredentials()
            {
                //code comment for Ut testing only
                grant_type = appSettings.Value.Grant_Type,
                client_id = appSettings.Value.clientId,
                client_secret = appSettings.Value.clientSecret,
                resource = appSettings.Value.resourceId
            };

            AppIntegration appIntegrations = new AppIntegration()
            {
                ApplicationID = appId,
                Credentials = JsonConvert.SerializeObject(appIntegrationsCredentials)
            };

            return _genericSelfservice.UpdateAppIntegration(appIntegrations);
        }

        public string UpdatePublicTemplateMapping(string appId, string usecaseId, dynamic data)
        {
            bool isMultiApp = false;
            bool isSingleApp = false;
            Uri apiUri = new Uri(appSettings.Value.myWizardAPIUrl);
            string host = apiUri.GetLeftPart(UriPartial.Authority);

            var MappingCollection = _database.GetCollection<PublicTemplateMapping>(CONSTANTS.PublicTemplateMapping);
            var Builder = Builders<PublicTemplateMapping>.Filter;
            var Projection1 = Builders<PublicTemplateMapping>.Projection.Exclude("_id");
            //var filter = Builder.Eq(CONSTANTS.ApplicationID, appId) & Builder.Eq(CONSTANTS.UsecaseID, usecaseId);
            var filter = Builder.Eq(CONSTANTS.UsecaseID, usecaseId);
            var templatedata = MappingCollection.Find(filter).Project<PublicTemplateMapping>(Projection1).FirstOrDefault();
            if (templatedata!=null && templatedata.IsMultipleApp == "yes")
            {
                if (templatedata.ApplicationIDs.Contains(appId))
                {
                    isMultiApp = true;

                }
            }
            else
            {
                isSingleApp = true;
            }
            if (isSingleApp || isMultiApp)
            {
                string SourceURL = host;
                string SourceName = "SPA";
                if (!string.IsNullOrEmpty(templatedata.SourceName) && templatedata.SourceName.ToUpper() == CONSTANTS.CustomDataApi.ToUpper())
                {
                    SourceName = templatedata.SourceName;
                    if (!string.IsNullOrEmpty(templatedata.SourceURL))
                    {
                       SourceURL = templatedata.SourceURL;
                    }
                }
                else
                {
                   Uri apiUri1 = new Uri(templatedata.SourceURL);
                    string apiPath = apiUri1.AbsolutePath;
                    SourceURL = host + apiPath;
                }
                string queryData = data != null ? data["IterationUID"].ToString(Formatting.None) : templatedata.IterationUID;

                PublicTemplateMapping templateMapping = new PublicTemplateMapping()
                {
                    //ApplicationID = appId,
                    ApplicationID = templatedata.ApplicationID,
                    SourceName = SourceName,
                    UsecaseID = usecaseId,
                    SourceURL = SourceURL,
                    InputParameters = "",
                    //IterationUID = !string.IsNullOrEmpty(queryData) ? queryData : templatedata.IterationUID, //For SPA-AmbulanceLane only
                    IterationUID = queryData
                };

                return _genericSelfservice.UpdatePublicTemplateMapping(templateMapping);
            }
            else
            {
                return CONSTANTS.NoRecordsFound;
            }
        }


        /// <summary>
        /// Get the Velocity Prediction for the usecase
        /// </summary>
        /// <param name="RequestPayload"></param>
        /// <returns>Prediction results</returns>
        public VelocityPrediction GetPrediction(SPAInfo RequestPayload)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(GetPrediction), "Prediction - Started :", string.Empty, string.Empty,RequestPayload.ClientUID,RequestPayload.DeliveryConstructUID);
            VelocityPrediction velocityPrediction = new VelocityPrediction();
            string response = string.Empty;
            var AppIntegCollection = _database.GetCollection<AppIntegration>(CONSTANTS.AppIntegration);
            var filterBuilder = Builders<AppIntegration>.Filter;
            var AppFilter = filterBuilder.Eq(CONSTANTS.ApplicationID, RequestPayload.AppServiceUID);
            var Projection = Builders<AppIntegration>.Projection.Exclude(CONSTANTS.Id);
            var IsApplicationExist = AppIntegCollection.Find(AppFilter).Project<AppIntegration>(Projection).FirstOrDefault();
            string CorrelationId = string.Empty;
            if (IsApplicationExist != null)
            {
                if (UpdateTokenInAppIntegration(RequestPayload.AppServiceUID) == CONSTANTS.Success && UpdatePublicTemplateMapping(RequestPayload.AppServiceUID, RequestPayload.UseCaseUID, null) == CONSTANTS.Success)
                {
                    var MappingCollection = _database.GetCollection<PublicTemplateMapping>(CONSTANTS.PublicTemplateMapping);
                    var Builder = Builders<PublicTemplateMapping>.Filter;
                    var Projection1 = Builders<PublicTemplateMapping>.Projection.Exclude("_id");
                    // var filter = Builder.Eq(CONSTANTS.ApplicationID, RequestPayload.AppServiceUID) & Builder.Eq(CONSTANTS.UsecaseID, RequestPayload.UseCaseUID);
                    var filter = Builder.Eq(CONSTANTS.UsecaseID, RequestPayload.UseCaseUID);
                    var templatedata = MappingCollection.Find(filter).Project<PublicTemplateMapping>(Projection1).FirstOrDefault();
                    if (templatedata != null)
                    {
                        string teamAreaUID = null;
                        if (templatedata.IsMultipleApp == "yes")
                        {
                            if (templatedata.ApplicationIDs.Contains(RequestPayload.AppServiceUID))
                            {
                                templatedata.ApplicationID = RequestPayload.AppServiceUID;
                                templatedata.ApplicationName = IsApplicationExist.ApplicationName;
                                //templateAppId = templatedata.ApplicationID;
                            }
                        }
                        else
                        {
                            if (RequestPayload.AppServiceUID != templatedata.ApplicationID)
                            {
                                velocityPrediction.Message = CONSTANTS.IsApplicationExist;
                                velocityPrediction.Status = CONSTANTS.E;
                                velocityPrediction.ErrorMessage = CONSTANTS.IsApplicationExist;
                                return velocityPrediction;
                            }
                        }
                        //if (RequestPayload.UseCaseUID == "f0320924-2ee3-4398-ad7c-8bc172abd78d" || RequestPayload.UseCaseUID == "49bf29ca-3408-4ae1-af4d-32963e18670a" || RequestPayload.UseCaseUID == "efd60535-5d15-46a8-961f-c43161e3a326") //"which sowmya provide team level")
                        if (RequestPayload.UseCaseUID == templatedata.UsecaseID && templatedata.UsecaseType == "Team Level") //"which sowmya provide team level")
                        {
                            if (RequestPayload.UseCaseUID == "f0320924-2ee3-4398-ad7c-8bc172abd78d")
                            {
                                var clientDCInfo = GetClientDCIngrainRequest(RequestPayload.CorrelationId, RequestPayload.AppServiceUID, RequestPayload.UseCaseUID);
                                RequestPayload.ClientUID = clientDCInfo.ClientId;
                                RequestPayload.DeliveryConstructUID = clientDCInfo.DeliveryconstructId;
                                teamAreaUID = clientDCInfo.TeamAreaUId;
                            }
                        }
                        var _trainingStatus = GetModelStatus(RequestPayload.ClientUID, RequestPayload.DeliveryConstructUID, RequestPayload.AppServiceUID, RequestPayload.UseCaseUID, RequestPayload.UserId, teamAreaUID, templatedata.UsecaseType, templatedata.IsMultipleApp, templatedata.ApplicationIDs);
                        if (_trainingStatus == null)
                        {
                            velocityPrediction.Message = CONSTANTS.NoTrainedModelFound;
                            velocityPrediction.Status = CONSTANTS.E;
                            velocityPrediction.ErrorMessage = CONSTANTS.NoTrainedModelFound;
                            return velocityPrediction;
                        }
                        if (_trainingStatus.Status == CONSTANTS.E || _trainingStatus.Status == CONSTANTS.P || _trainingStatus.Status == CONSTANTS.I)
                        {
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(GetPrediction), "_trainingStatus :" + _trainingStatus.CorrelationId + "--" + _trainingStatus.Status + "--NEW--" + CONSTANTS.NoTrainedModelFound, string.Empty, string.Empty, RequestPayload.ClientUID, RequestPayload.DeliveryConstructUID);
                            velocityPrediction.Message = CONSTANTS.NoTrainedModelFound;
                            velocityPrediction.Status = CONSTANTS.E;
                            velocityPrediction.ErrorMessage = CONSTANTS.NoTrainedModelFound;
                            if (_trainingStatus.Status == CONSTANTS.E)
                                velocityPrediction.ErrorMessage = _trainingStatus.ErrorMessage;
                            return velocityPrediction;
                        }
                        else
                        {
                            if (_trainingStatus.Status == CONSTANTS.C)
                            {
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(GetPrediction), "-TRAININGSTATUS ELSE CORRELATIONID :" + _trainingStatus.CorrelationId + "--" + _trainingStatus.Status, string.Empty, string.Empty, RequestPayload.ClientUID, RequestPayload.DeliveryConstructUID);
                                ValidRecordsDetailsModel validRecordsDetailModel = CommonUtility.GetModelDataPointdDetails(_trainingStatus.CorrelationId, appSettings);
                                if (validRecordsDetailModel != null)
                                {
                                    if (validRecordsDetailModel.ValidRecordsDetails != null)
                                    {
                                        if (validRecordsDetailModel.ValidRecordsDetails.Records[0] >= 4 && validRecordsDetailModel.ValidRecordsDetails.Records[0] < 20)
                                        {
                                            velocityPrediction.DataPointsWarning = CONSTANTS.DataPointsMinimum;
                                            velocityPrediction.DataPointsCount = validRecordsDetailModel.ValidRecordsDetails.Records[0];
                                        }
                                    }
                                }
                                var ModelResponse = GetUseCasePrediction(_trainingStatus.CorrelationId, templatedata, RequestPayload, _trainingStatus.CorrelationId);
                                if (ModelResponse != null & ModelResponse.Status != null)
                                {
                                    velocityPrediction.CorrelationId = ModelResponse.CorrelationId;
                                    velocityPrediction.UniqueId = ModelResponse.UniqueId;
                                    velocityPrediction.UseCaseUID = RequestPayload.UseCaseUID;
                                    if (ModelResponse.Status == CONSTANTS.E)
                                        velocityPrediction.ErrorMessage = ModelResponse.ErrorMessage;
                                    if (ModelResponse.Status == CONSTANTS.C)
                                    {
                                        velocityPrediction.Status = ModelResponse.Status;
                                        velocityPrediction.PredictedData = ModelResponse.PredictedData;
                                        velocityPrediction.Message = CONSTANTS.PredictionSuccess;
                                    }
                                    IngrainResponseData CallBackResponse = new IngrainResponseData
                                    {
                                        CorrelationId = velocityPrediction.CorrelationId,
                                        Status = ModelResponse.Status,
                                        Message = ModelResponse.Status == CONSTANTS.C ? velocityPrediction.Message : velocityPrediction.ErrorMessage,
                                        ErrorMessage = ModelResponse.Status == CONSTANTS.C ? null : ModelResponse.ErrorMessage,
                                        DataPointsCount = velocityPrediction.DataPointsCount,
                                        DataPointsWarning = CONSTANTS.DataPointsMinimum
                                    };
                                    //Log to DB Audir Trail log
                                    CallBackErrorLog.CorrelationId = CallBackResponse.CorrelationId;
                                    CallBackErrorLog.Message = CallBackResponse.Message;
                                    CallBackErrorLog.ErrorMessage = CallBackResponse.ErrorMessage;
                                    CallBackErrorLog.Status = CallBackResponse.Status;
                                    CallBackErrorLog.ApplicationName = null;
                                    CallBackErrorLog.BaseAddress = null;
                                    CallBackErrorLog.httpResponse = null;
                                    CallBackErrorLog.ClientId = RequestPayload.ClientUID;
                                    CallBackErrorLog.DCID = RequestPayload.DeliveryConstructUID;
                                    CallBackErrorLog.ApplicationID = RequestPayload.AppServiceUID;
                                    CallBackErrorLog.UseCaseId = RequestPayload.UseCaseUID;
                                    CallBackErrorLog.RequestId = velocityPrediction.UniqueId;
                                    CallBackErrorLog.CreatedBy = CONSTANTS.System;
                                    CallBackErrorLog.ProcessName = "Prediction";
                                    if (CallBackErrorLog.Status != CONSTANTS.E)
                                    {
                                        CallBackErrorLog.UsageType = "INFO";
                                    }
                                    else
                                    {
                                        CallBackErrorLog.UsageType = "ERROR";
                                    }
                                    AuditTrailLog(CallBackErrorLog);
                                    return velocityPrediction;
                                }
                            }
                            else
                            {
                                velocityPrediction.Message = CONSTANTS.NoTrainedModelFound;
                                velocityPrediction.Status = CONSTANTS.E;
                                velocityPrediction.ErrorMessage = CONSTANTS.NoTrainedModelFound;
                                if (_trainingStatus.Status == CONSTANTS.E)
                                    velocityPrediction.ErrorMessage = _trainingStatus.ErrorMessage;
                                return velocityPrediction;
                            }
                        }
                    }
                    else
                    {
                        velocityPrediction.ErrorMessage = CONSTANTS.UsecaseNotAvailable;
                        velocityPrediction.Status = CONSTANTS.ErrorMessage;
                        return velocityPrediction;
                    }
                }
                else
                {
                    velocityPrediction.ErrorMessage = "Error updating usecase details";
                    velocityPrediction.Status = CONSTANTS.ErrorMessage;
                    return velocityPrediction;
                }

            }
            else
            {
                velocityPrediction.ErrorMessage = CONSTANTS.IsApplicationExist;
                velocityPrediction.Status = CONSTANTS.ErrorMessage;
                return velocityPrediction;
            }
            return velocityPrediction;
        }

        private IngrainRequestQueue GetClientDCIngrainRequest(string correlationId, string applicationId, string usecaseId)
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
                filterQueue = filterBuilder.Eq(CONSTANTS.CorrelationId, correlationId) & filterBuilder.Eq(CONSTANTS.TemplateUseCaseID, usecaseId) & filterBuilder.Eq(CONSTANTS.pageInfo, CONSTANTS.TrainAndPredict);

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
        private VelocityTrainingStatus GetModelStatus(string clientId, string dcId, string applicationId, string usecaseId, string userId, string teamAreaUID, string usecaseType, string isMultiApp, string applicationIds)
        {
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
            var filterBuilder = Builders<BsonDocument>.Filter;
            FilterDefinition<BsonDocument> filterQueue = null;
            if (usecaseType == "Team Level" && usecaseId == "f0320924-2ee3-4398-ad7c-8bc172abd78d")
            {
                filterQueue = filterBuilder.Eq(CONSTANTS.ClientId, clientId) & filterBuilder.Eq(CONSTANTS.DeliveryconstructId, dcId) & filterBuilder.Eq(CONSTANTS.TemplateUseCaseID, usecaseId) & filterBuilder.Eq(CONSTANTS.AppID, applicationId) & filterBuilder.Eq(CONSTANTS.pageInfo, CONSTANTS.TrainAndPredict) & filterBuilder.Eq("TeamAreaUId", teamAreaUID);
            }
            if (usecaseType == "Team Level" && isMultiApp == "yes")
            {
                filterQueue = filterBuilder.Eq(CONSTANTS.ClientId, clientId) & filterBuilder.Eq(CONSTANTS.DeliveryconstructId, dcId) & filterBuilder.Eq(CONSTANTS.TemplateUseCaseID, usecaseId) & filterBuilder.Eq(CONSTANTS.pageInfo, CONSTANTS.TrainAndPredict) & filterBuilder.Eq("TeamAreaUId", teamAreaUID);
            }
            else
            {
                filterQueue = filterBuilder.Eq(CONSTANTS.ClientId, clientId) & filterBuilder.Eq(CONSTANTS.DeliveryconstructId, dcId) & filterBuilder.Eq(CONSTANTS.TemplateUseCaseID, usecaseId) & filterBuilder.Eq(CONSTANTS.AppID, applicationId) & filterBuilder.Eq(CONSTANTS.pageInfo, CONSTANTS.TrainAndPredict);
            }
            var Projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
            var result = collection.Find(filterQueue).Project<BsonDocument>(Projection).ToList();
            if (result.Count > 0)
            {
                _trainingStatus.CorrelationId = result[0][CONSTANTS.CorrelationId].ToString();
                _trainingStatus.Status = result[0][CONSTANTS.Status].ToString();
                _trainingStatus.ErrorMessage = result[0][CONSTANTS.Message].ToString();
                _trainingStatus.UniqueId = result[0]["RequestId"].ToString();
                if (usecaseType == "Team Level" && isMultiApp == "yes")
                {
                    if (!applicationIds.Contains(result[0][CONSTANTS.AppID].ToString()))
                    {
                        _trainingStatus = null;
                        return _trainingStatus;
                    }
                }
                if (result[0][CONSTANTS.Status].ToString() == CONSTANTS.E)
                {
                    var queueCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
                    var filterBuilder1 = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, result[0][CONSTANTS.CorrelationId].ToString());
                    var outcome = queueCollection.Find(filterBuilder1).ToList();
                    //Insert back up to SSAI_IngrainRequests_bk and delete record
                    this.InsertRequestBackup(outcome,CONSTANTS.SSAIIngrainRequestsBackup);                  
                    queueCollection.DeleteMany(filterBuilder1);
                    var deployCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
                    var deployFilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, result[0][CONSTANTS.CorrelationId].ToString());
                    var deployOutcome = deployCollection.Find(deployFilter).ToList();
                    //Insert back up to SSAI_DeployedModels_bk and delete record
                    if (deployOutcome.Count > 0)
                    {
                        this.InsertRequestBackup(deployOutcome, CONSTANTS.SSAIDeployedModelsBackUp);
                    }
                    deployCollection.DeleteMany(deployFilter);
                    _trainingStatus.CorrelationId = result[0][CONSTANTS.CorrelationId].ToString();
                    _trainingStatus.Status = result[0][CONSTANTS.Status].ToString();
                    _trainingStatus.ErrorMessage = result[0][CONSTANTS.Message].ToString();
                    _trainingStatus.UniqueId = result[0]["RequestId"].ToString();
                    return _trainingStatus;
                }
                if (result[0][CONSTANTS.Status].ToString() == CONSTANTS.P || result[0][CONSTANTS.Status].ToString() == CONSTANTS.I || result[0][CONSTANTS.Status].ToString() == CONSTANTS.Null || result[0][CONSTANTS.Status].ToString() == CONSTANTS.BsonNull)
                {
                    _trainingStatus.CorrelationId = result[0][CONSTANTS.CorrelationId].ToString();
                    _trainingStatus.Status = result[0][CONSTANTS.Status].ToString();
                    if (result[0][CONSTANTS.Status].ToString() == CONSTANTS.Null || result[0][CONSTANTS.Status].ToString() == CONSTANTS.BsonNull)
                        _trainingStatus.Status = CONSTANTS.P;
                    _trainingStatus.ErrorMessage = result[0][CONSTANTS.Message].ToString();
                    _trainingStatus.UniqueId = result[0]["RequestId"].ToString();
                    return _trainingStatus;
                }

            }
            else
            {
                _trainingStatus = null;
            }
            return _trainingStatus;
        }

        private void InsertRequestBackup(List<BsonDocument> request,string collectionName)
        {
            foreach(var i in request)
            {
                i["CreatedOn"] = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
            }           
          
            var collection = _database.GetCollection<BsonDocument>(collectionName);
            collection.InsertMany(request);
        }

        
        private VelocityTrainingStatus GetUseCasePrediction(string CorrelationId, PublicTemplateMapping templatedata, SPAInfo RequestPayload, string NewModelCorrelationID)
        {
            var collection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
            var builder = Builders<DeployModelsDto>.Filter;
            PredictionDTO predictionDTO = new PredictionDTO();
            var filter1 = builder.Eq(CONSTANTS.CorrelationId, CorrelationId);
            var Projection2 = Builders<DeployModelsDto>.Projection.Exclude("_id");
            var deployedModel = collection.Find(filter1).Project<DeployModelsDto>(Projection2).ToList();
            {
                for (int i = 0; i < deployedModel.Count; i++)
                {
                    if (deployedModel[i].Status == CONSTANTS.Deployed)
                    {
                        predictionDTO = GetSPAPredictionData(deployedModel[i], RequestPayload);
                        if (predictionDTO != null)
                        {
                            _trainingStatus.Status = predictionDTO.Status;
                            _trainingStatus.UniqueId = predictionDTO.UniqueId;
                            _trainingStatus.CorrelationId = predictionDTO.CorrelationId;
                            _trainingStatus.PredictedData = predictionDTO.PredictedData;
                            DateTime currentTime = DateTime.Now;
                            DateTime createdTime = DateTime.Parse(predictionDTO.CreatedOn);
                            TimeSpan span = currentTime.Subtract(createdTime);
                            //PREDICTION TIME OUT ERROR
                            if (span.TotalMinutes > Convert.ToDouble(appSettings.Value.PredictionTimeoutMinute) && predictionDTO.Status != CONSTANTS.C)
                            {
                                _trainingStatus.Message = CONSTANTS.PredictionTimeOutError + predictionDTO.Status;
                                _trainingStatus.Status = CONSTANTS.E;
                                _trainingStatus.ErrorMessage = CONSTANTS.PredictionTimeOutError;
                                return _trainingStatus;
                            }
                            if (predictionDTO.Status == CONSTANTS.C)
                            {
                                _trainingStatus.PredictedData = predictionDTO.PredictedData;
                                _trainingStatus.Message = CONSTANTS.PredictionSuccess;
                            }
                            else if (predictionDTO.Status == CONSTANTS.E)
                            {
                                _trainingStatus.Status = CONSTANTS.E;
                                _trainingStatus.ErrorMessage = CONSTANTS.PredictionRequestFailed;
                            }
                            else
                            {
                                _trainingStatus.ErrorMessage = predictionDTO.ErrorMessage;
                            }
                        }
                    }
                    else
                    {
                        _trainingStatus.Message = CONSTANTS.ModelNotExists;
                        _trainingStatus.Status = CONSTANTS.E;
                        _trainingStatus.ErrorMessage = CONSTANTS.ModelNotExists;
                    }
                }
            }
            return _trainingStatus;
        }
        private PredictionDTO IsPredictedCompeted(PredictionDTO predictionDTO)
        {
            PredictionDTO predictionData = new PredictionDTO();
            bool isPrediction = true;
            while (isPrediction)
            {
                predictionData = _deployedModelService.GetPrediction(predictionDTO);
                if (predictionData.Status == CONSTANTS.C)
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(IsPredictedCompeted), CONSTANTS.APIResult + predictionData.Status, string.IsNullOrEmpty(predictionData.CorrelationId) ? default(Guid) : new Guid(predictionData.CorrelationId) , string.Empty, string.Empty, string.Empty, string.Empty);
                    isPrediction = false;
                    return predictionData;
                }
                else if (predictionData.Status == CONSTANTS.E)
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(IsPredictedCompeted), CONSTANTS.APIResult + predictionData.Status, string.IsNullOrEmpty(predictionData.CorrelationId) ? default(Guid) : new Guid(predictionData.CorrelationId), string.Empty, string.Empty, string.Empty, string.Empty);
                    isPrediction = false;
                }
                else
                {
                    Thread.Sleep(1000);
                    isPrediction = true;
                }
            }
            return predictionData;
        }
        public void SavePrediction(PredictionDTO predictionDTO)
        {
            var jsonData = JsonConvert.SerializeObject(predictionDTO);
            var insertDocument = BsonSerializer.Deserialize<BsonDocument>(jsonData);
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_PublishModel);

            collection.InsertOne(insertDocument);
        }
        private void InsertRequests(IngrainRequestQueue ingrainRequest)
        {
            var requestQueue = JsonConvert.SerializeObject(ingrainRequest);
            var insertRequestQueue = BsonSerializer.Deserialize<BsonDocument>(requestQueue);
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
            collection.InsertOne(insertRequestQueue);
        }
        private IngrainRequestQueue InsertPrediction(string NewModelCorrelationID, PublicTemplateMapping templatedata, SPAInfo RequestPayload)
        {
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
                CreatedByUser = CONSTANTS.SYSTEM,
                CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                ModifiedByUser = CONSTANTS.SYSTEM,
                ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                LastProcessedOn = CONSTANTS.Null,
                AppID = templatedata.ApplicationID,
                ClientId = RequestPayload.ClientUID,
                DeliveryconstructId = RequestPayload.DeliveryConstructUID,
                UseCaseID = CONSTANTS.Null,
                EstimatedRunTime = CONSTANTS.Null,
            };
            return ingrainRequest;
        }
        private IngrainRequestQueue InsertSPAPrediction(string NewModelCorrelationID, PublicTemplateMapping templatedata, Velocity RequestPayload)
        {
            var FunctionName = string.Empty;
            var PageInfo = string.Empty;
            if (RequestPayload.RetrainRequired == "AutoRetrain")
            {
                FunctionName = RequestPayload.RetrainRequired;
                PageInfo = RequestPayload.RetrainRequired;
            }
            else
            {
                FunctionName = CONSTANTS.AutoTrain;
                PageInfo = CONSTANTS.TrainAndPredict;
            }
            bool DBEncryptionRequired = CommonUtility.EncryptDB(NewModelCorrelationID, appSettings);
            if (DBEncryptionRequired)
            {
                if (!string.IsNullOrEmpty(Convert.ToString(RequestPayload.UserID)))
                {
                    RequestPayload.UserID = _encryptionDecryption.Encrypt(Convert.ToString(RequestPayload.UserID));
                }
            }
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
                pageInfo = PageInfo,//CONSTANTS.TrainAndPredict,
                ParamArgs = CONSTANTS.Null,
                TemplateUseCaseID = templatedata.UsecaseID,
                Function = FunctionName,
                CreatedByUser = RequestPayload.UserID,
                CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                ModifiedByUser = RequestPayload.UserID,
                ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                LastProcessedOn = CONSTANTS.Null,
                AppID = templatedata.ApplicationID,
                ClientId = RequestPayload.ClientUID,
                DeliveryconstructId = RequestPayload.DeliveryConstructUID,
                UseCaseID = CONSTANTS.Null,
                EstimatedRunTime = CONSTANTS.Null,
                AppURL = RequestPayload.ResponseCallbackUrl,
                TeamAreaUId = RequestPayload.TeamAreaUId,
                RetrainRequired = RequestPayload.RetrainRequired,
                IsForAutoTrain = true
            };
            return ingrainRequest;
        }
        private PredictionDTO GetSPAPredictionData(DeployModelsDto deployedModel, SPAInfo info)
        {
            PredictionDTO predictionDTO1 = new PredictionDTO();
            if (deployedModel != null)
            {
                var usecaseId = deployedModel.TemplateUsecaseId;
                var appId = deployedModel.AppId;
                if (info.UseCaseUID == "f0320924-2ee3-4398-ad7c-8bc172abd78d")
                {
                    if (deployedModel.IsMutipleApp)
                    {
                        usecaseId = info.UseCaseUID;
                        appId = info.AppServiceUID;
                    }
                }
                bool DBEncryptionRequired = CommonUtility.EncryptDB(deployedModel.CorrelationId, appSettings);
                string FunctionType = string.Empty;
                //string actualData = string.Empty;
                dynamic actualData = string.Empty;

                if (deployedModel.ModelType == CONSTANTS.TimeSeries)
                {
                    FunctionType = CONSTANTS.ForecastModel;
                    //actualData = DBEncryptionRequired ? _encryptionDecryption.Encrypt(CONSTANTS.Null) : CONSTANTS.Null;
                    actualData= CONSTANTS.Null;
                }
                else
                {
                    FunctionType = CONSTANTS.PublishModel;
                    actualData = Convert.ToString(info.Data);
                }
                PredictionDTO predictionDTO = new PredictionDTO
                {
                    _id = Guid.NewGuid().ToString(),
                    UniqueId = Guid.NewGuid().ToString(),
                    CorrelationId = deployedModel.CorrelationId,
                    Frequency = deployedModel.Frequency,
                    PredictedData = null,
                    Status = CONSTANTS.I,
                    ErrorMessage = null,
                    TempalteUseCaseId = usecaseId,
                    AppID = appId,
                    Progress = null,
                    CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                    ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                    CreatedByUser = DBEncryptionRequired ? _encryptionDecryption.Encrypt(CONSTANTS.System) : CONSTANTS.System,
                    ModifiedByUser = DBEncryptionRequired ? _encryptionDecryption.Encrypt(CONSTANTS.System) : CONSTANTS.System,
                    StartDates = info.StartDates //added for release planner
                };
                if (DBEncryptionRequired)
                {
                    actualData = _encryptionDecryption.Encrypt(actualData);
                }
                if (deployedModel.SourceName == "Custom" || deployedModel.SourceName == CONSTANTS.SPAAPP)
                {
                    predictionDTO.ActualData = actualData.Replace("\r\n", string.Empty);
                    SavePrediction(predictionDTO);
                    this.insertRequest(deployedModel, predictionDTO.UniqueId, FunctionType);
                    predictionDTO1 = IsPredictedCompeted(predictionDTO);
                }
            }
            return predictionDTO1;
        }
        private void insertRequest(DeployModelsDto deployModels, string uniqueId, string Function)
        {
            bool IsForAPI = false;
            var featureWeights = new
            {
                FeatureWeights = appSettings.Value.EnableFeatureWeights
            };

            var paramArgs = JsonConvert.SerializeObject(featureWeights, Formatting.None);

            if (appSettings.Value.IsFlaskCall)
            {
                IsForAPI = true;
            }

            IngrainRequestQueue ingrainRequest = new IngrainRequestQueue
            {
                _id = Guid.NewGuid().ToString(),
                CorrelationId = deployModels.CorrelationId,
                RequestId = Guid.NewGuid().ToString(),
                ProcessId = CONSTANTS.Null,
                Status = CONSTANTS.Null,
                ModelName = CONSTANTS.Null,
                RequestStatus = appSettings.Value.IsFlaskCall ? "Occupied" :CONSTANTS.New,
                RetryCount = 0,
                ProblemType = CONSTANTS.Null,
                Message = CONSTANTS.Null,
                UniId = uniqueId,
                Progress = CONSTANTS.Null,
                pageInfo = Function, // pageInfo 
                ParamArgs = paramArgs,//CONSTANTS.CurlyBraces,
                Function = Function,
                CreatedByUser = deployModels.CreatedByUser,
                TemplateUseCaseID = deployModels.UseCaseID,
                CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                ModifiedByUser = deployModels.ModifiedByUser,
                ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                LastProcessedOn = CONSTANTS.Null,
                AppID = deployModels.AppId,
                IsForAPI = IsForAPI
            };
            InsertRequests(ingrainRequest);

            if (appSettings.Value.IsFlaskCall)
            {
                _iFlaskAPIService.CallPython(deployModels.CorrelationId, uniqueId,ingrainRequest.pageInfo);
            }

            Thread.Sleep(2000);
        }

        public void AuditTrailLog(CallBackErrorLog auditTrailLog)
        {
            string result = string.Empty;
            string isNoficationSent = "false";
            if (!string.IsNullOrEmpty(auditTrailLog.CorrelationId))
            {
                //Team level Predicton
                var ingrainRequest = GetClientDCIngrainRequest(auditTrailLog.CorrelationId, auditTrailLog.ApplicationID, auditTrailLog.UseCaseId);
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
            if (auditTrailLog.Status == CONSTANTS.ErrorMessage)
            {
                auditTrailLog.ErrorMessage = auditTrailLog.ErrorMessage + " -" + "Record will be deleted from IngrainRequest for CorrelationId : " + auditTrailLog.CorrelationId;
            }
            auditTrailLog.CallbackURLResponse = result;
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
        }


        private string CallbackResponse(IngrainResponseData CallBackResponse, string AppName, string baseAddress, string clientId, string DCId, string applicationId, string usecaseId, string requestId, string userId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(CallbackResponse), "CallbackResponse - Started :",applicationId,string.Empty,clientId,DCId);
            string token = CustomUrlToken(AppName);

            string contentType = "application/json";
            var Request = JsonConvert.SerializeObject(CallBackResponse);
            using (var Client = new HttpClient())
            {
                Client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(contentType));

                HttpContent httpContent = new StringContent(Request, Encoding.UTF8, "application/json");
                HttpResponseMessage httpResponse = Client.PostAsync(baseAddress, httpContent).Result;
                var statuscode = httpResponse.StatusCode;
                //Log to DB AuditTraiLog
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
                AuditTrailLog(CallBackErrorLog);

                if (httpResponse.IsSuccessStatusCode)
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(CallbackResponse), "CallbackResponse - SUCCESS END :" + httpResponse.Content, applicationId, string.Empty, clientId, DCId);
                    return CONSTANTS.success;
                }
                else
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(CallbackResponse), "CallbackResponse - ERROR END :" + statuscode + "--" + httpResponse.Content, applicationId, string.Empty, clientId, DCId);
                    return CONSTANTS.ErrorMessage;
                }
            }
        }
        private string CustomUrlToken(string ApplicationName)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(CustomUrlToken), "CustomUrlToken for Application--" + ApplicationName, string.Empty, string.Empty, string.Empty, string.Empty);
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
            else if (AppData.Authentication == "FORM")
            {
                using (var httpClient = new HttpClient())
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
                    token = tokenObj != null ? Convert.ToString(tokenObj.token) : CONSTANTS.InvertedComma;
                    return token;

                    //httpClient.BaseAddress = new Uri(appSettings.Value.tokenAPIUrl);
                    //httpClient.DefaultRequestHeaders.Accept.Clear();
                    //httpClient.DefaultRequestHeaders.Add("UserName", appSettings.Value.username);
                    //httpClient.DefaultRequestHeaders.Add("Password", appSettings.Value.password);
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

                }
            }
            return token;
        }


        public bool IsAmbulanceLane(string useCaseId)
        {
            string[] ambulanceLane = new string[] { CONSTANTS.CRHigh, CONSTANTS.CRCritical, CONSTANTS.SRHigh, CONSTANTS.SRCritical, CONSTANTS.PRHigh, CONSTANTS.PRCritical };
            if (ambulanceLane.Contains(useCaseId))
                return true;
            else
                return false;
        }

        public long GetDatapoints(string useCaseId, string applicationId)
        {
            var dbCollection = _database.GetCollection<PublicTemplateMapping>(CONSTANTS.PublicTemplateMapping);
            var filterBuilder = Builders<PublicTemplateMapping>.Filter;
            var filter = filterBuilder.Eq("ApplicationID", applicationId) & filterBuilder.Eq("UsecaseID", useCaseId);
            var projection = Builders<PublicTemplateMapping>.Projection.Include("DataPoints").Exclude("_id");
            var data = dbCollection.Find(filter).Project<PublicTemplateMapping>(projection).FirstOrDefault();
            return (data != null & data.DataPoints > 0) ? data.DataPoints : 0;           
        }
        #endregion
    }


}
