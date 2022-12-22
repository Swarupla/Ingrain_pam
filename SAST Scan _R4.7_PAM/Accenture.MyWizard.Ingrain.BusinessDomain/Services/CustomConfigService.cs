using System;
using Accenture.MyWizard.Ingrain.DataAccess;
using Accenture.MyWizard.Ingrain.DataModels.Models;
using Accenture.MyWizard.Shared.Helpers;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using MongoDB.Bson.Serialization;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using Accenture.MyWizard.Ingrain.BusinessDomain.Common;
using RestSharp;
using System.Text;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net;
using System.IO;
using System.Collections;
using System.Threading.Tasks;
using System.Threading;
using USECASE = Accenture.MyWizard.Ingrain.DataModels.AICore.UseCase;
using AIGENERICSERVICE = Accenture.MyWizard.Ingrain.DataModels.AICore.GenericService;
using Accenture.MyWizard.Ingrain.DataModels.AICore;
using Accenture.MyWizard.Ingrain.DataModels.AICore.UseCase;

namespace Accenture.MyWizard.Ingrain.BusinessDomain.Services
{
    public  class CustomConfigService : ICustomConfigService
    {

        private readonly MongoClient _mongoClient;
        private readonly IMongoDatabase _database;
        private readonly WebHelper webHelper;
        private readonly DatabaseProvider databaseProvider;
        private readonly IOptions<IngrainAppSettings> appSettings;
        private IGenericSelfservice _genericSelfservice;
        private GenericTraining _GenericTraining;
        private VelocityTrainingStatus _trainingStatus;
        private IEncryptionDecryption _encryptionDecryption;
        private CallBackErrorLog CallBackErrorLog;
        private long _dataPointCount;
        private string _dataPointsWarning;
        private CallBackErrorLog auditTrailLog;
        private readonly IOptions<IngrainAppSettings> configSetting;
        private static IClusteringAPIService _clusteringAPI { get; set; }
        private static ICustomDataService _customDataService { set; get; }
        Filepath _filepath = null;
        ParentFile parentFile = null;
        FileUpload fileUpload = null;
        public static IPhoenixTokenService _iPhoenixTokenService { get; set; }
        /// <summary>
        /// CustomConfigService Constructor
        /// </summary>
        /// <param name="db">DatabaseProvider</param>
        /// <param name="settings">IngrainAppSettings</param>
        /// <param name="serviceProvider">serviceProvider</param>
        public CustomConfigService(DatabaseProvider db, IOptions<IngrainAppSettings> Setting, IServiceProvider serviceProvider)
        {
            databaseProvider = db;
            _mongoClient = databaseProvider.GetDatabaseConnection();
            var dataBaseName = MongoUrl.Create(Setting.Value.connectionString).DatabaseName;
            _database = _mongoClient.GetDatabase(dataBaseName);
            appSettings = Setting;
            webHelper = new WebHelper();
            _genericSelfservice = serviceProvider.GetService<IGenericSelfservice>();
            _GenericTraining = new GenericTraining();
            _trainingStatus = new VelocityTrainingStatus();
            _encryptionDecryption = serviceProvider.GetService<IEncryptionDecryption>();
            CallBackErrorLog = new CallBackErrorLog();
            auditTrailLog = new CallBackErrorLog();
            configSetting = Setting;
            _clusteringAPI = serviceProvider.GetService<IClusteringAPIService>();
            _customDataService = serviceProvider.GetService<ICustomDataService>();
            _iPhoenixTokenService = serviceProvider.GetService<IPhoenixTokenService>();
        }

        public object GetCustomConfigurations(string serviceType, string serviceLevel)
        {
            MasterConfigurationDTO oMasterConfig = null;
            List<ConstraintsDTO> oConstraintCollection = null;

            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.CustomConfigurations);
            var builder = Builders<BsonDocument>.Filter;
            //below line is commented as assumption is that constraints are same for all environments
            //var filter = builder.Eq(CONSTANTS.Application, "Ingrain");
            var filter = builder.Empty;
            var projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);

            var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                oMasterConfig = new MasterConfigurationDTO();
                oMasterConfig = JsonConvert.DeserializeObject<MasterConfigurationDTO>(result[0].ToJson());
                if (oMasterConfig.Constraints != null)
                {
                    oConstraintCollection = new List<ConstraintsDTO>();
                    foreach (var oConstraints in oMasterConfig.Constraints)
                    {
                        bool bContraintAtServiceLevel = false;
                        dynamic oServiceLevel = oConstraints.ServiceLevel;

                        foreach (JValue level in oServiceLevel)
                        {
                            if (level.Value.ToString() == serviceLevel)
                            {
                                bContraintAtServiceLevel = true;
                            }
                        }

                        if (oConstraints.Condition.ToUpper() == serviceType.ToUpper() && bContraintAtServiceLevel)
                        {
                            oConstraintCollection.Add(oConstraints);
                        }
                    }
                }
            }
            return oConstraintCollection;
        }

        public void SaveCustomConfigurations(HttpContext httpContext, string ServiceLevel, dynamic dynamicData)
        {
            dynamic oSelectedConfiguration;
            var Training = dynamicData.Training;
            var Prediction = dynamicData.Prediction;
            var Retraining = dynamicData.Retraining;

            string userId = dynamicData.userId;
            string CorrelationID = dynamicData.CorrelationID;
            string UseCaseID = dynamicData.UseCaseID;
            string TemplateUsecaseId = dynamicData.UseCaseID;

            switch (ServiceLevel.ToUpper())
            {
                case CONSTANTS.SSAIAPP:
                    
                    oSelectedConfiguration = new SSAICustomConfiguration();
                    
                    var collection = _database.GetCollection<SSAICustomConfiguration>(CONSTANTS.SSAICustomContraints);
                    var filterBuilder = Builders<SSAICustomConfiguration>.Filter;
                    var AppFilter = filterBuilder.Eq("CorrelationID", CorrelationID);
                    var result = collection.Find(AppFilter).ToList();
                    if (result.Count > 0)
                    {
                        oSelectedConfiguration = result[0];
                        oSelectedConfiguration.ModifiedByUser = userId;
                        oSelectedConfiguration.ModifiedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        collection.DeleteOne(AppFilter);
                    }
                    else
                    {
                        oSelectedConfiguration._id = Guid.NewGuid().ToString();
                        oSelectedConfiguration.ApplicationID = dynamicData.ApplicationID.Value;
                        oSelectedConfiguration.CorrelationID = dynamicData.CorrelationID.Value;
                        oSelectedConfiguration.ModelVersion = dynamicData.ModelVersion.Value;
                        oSelectedConfiguration.ModelType = dynamicData.ModelType.Value;
                        oSelectedConfiguration.CreatedByUser = userId;
                        oSelectedConfiguration.CreatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    }

                    if (Training != null && ((JContainer)Training).HasValues)
                    {
                        oSelectedConfiguration.IsTrainingEnabled = true;
                        oSelectedConfiguration.Training = JsonConvert.DeserializeObject<SelectedConfiguration>(Training.ToString());
                    }
                    if (Prediction != null && ((JContainer)Prediction).HasValues)
                    {
                        oSelectedConfiguration.IsPredictionEnabled = true;
                        oSelectedConfiguration.Prediction = JsonConvert.DeserializeObject<SelectedConfiguration>(Prediction.ToString());
                    }
                    if (Retraining != null && ((JContainer)Retraining).HasValues)
                    {
                        oSelectedConfiguration.IsRetrainingEnabled = true;
                        oSelectedConfiguration.Retraining = JsonConvert.DeserializeObject<SelectedConfiguration>(Retraining.ToString());
                    }

                    var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(oSelectedConfiguration);
                    var insertDocument = BsonSerializer.Deserialize<SSAICustomConfiguration>(jsonData);
                    collection.InsertOne(insertDocument);

                    break;

                case CONSTANTS.AIAPP:

                    oSelectedConfiguration = new AICustomConfiguration();

                    var AIcollection = _database.GetCollection<AICustomConfiguration>(CONSTANTS.AICustomContraints);
                    var AIfilterBuilder = Builders<AICustomConfiguration>.Filter;
                    var AIAppFilter = AIfilterBuilder.Eq("CorrelationID", CorrelationID) & AIfilterBuilder.Eq("UseCaseID", UseCaseID);
                    var response = AIcollection.Find(AIAppFilter).ToList();
                    if (response.Count > 0)
                    {
                        oSelectedConfiguration = response[0];
                        oSelectedConfiguration.ModifiedByUser = userId;
                        oSelectedConfiguration.ModifiedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        AIcollection.DeleteOne(AIAppFilter);
                    }
                    else
                    {
                        string ServiceUID = dynamicData.ServiceId;
                        string UsecaseName = dynamicData.UsecaseName;

                        var AISavedUsecasescollection = _database.GetCollection<BsonDocument>("AISavedUsecases");
                        var AISavedUsecasesfilter =  Builders<BsonDocument>.Filter.Eq("CorrelationId", CorrelationID) & Builders<BsonDocument>.Filter.Eq("ServiceId", ServiceUID) & Builders<BsonDocument>.Filter.Eq("UsecaseId", UseCaseID) ;
                        var AISavedUsecasesresult = AISavedUsecasescollection.Find(AISavedUsecasesfilter).ToList();
                        if (AISavedUsecasesresult.Count > 0)
                        {
                            oSelectedConfiguration._id = Guid.NewGuid().ToString();
                            oSelectedConfiguration.ApplicationID = dynamicData.ApplicationID.Value;
                            oSelectedConfiguration.UseCaseID = AISavedUsecasesresult[0]["UsecaseId"].ToString();
                            oSelectedConfiguration.CorrelationID = dynamicData.CorrelationID.Value;
                            oSelectedConfiguration.TemplateUseCaseID = dynamicData.TemplateUseCaseID.Value;
                            oSelectedConfiguration.CreatedByUser = userId;
                            oSelectedConfiguration.CreatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        }
                        else
                        {
                            throw new Exception("Update Terminated as Document not Found");
                        }
                    }

                    if (Training != null && ((JContainer)Training).HasValues)
                    {
                        oSelectedConfiguration.IsTrainingEnabled = true;
                        oSelectedConfiguration.Training = JsonConvert.DeserializeObject<SelectedConfiguration>(Training.ToString());
                    }
                    if (Prediction != null && ((JContainer)Prediction).HasValues)
                    {
                        oSelectedConfiguration.IsPredictionEnabled = true;
                        oSelectedConfiguration.Prediction = JsonConvert.DeserializeObject<SelectedConfiguration>(Prediction.ToString());
                    }
                    if (Retraining != null && ((JContainer)Retraining).HasValues)
                    {
                        oSelectedConfiguration.IsRetrainingEnabled = true;
                        oSelectedConfiguration.Retraining = JsonConvert.DeserializeObject<SelectedConfiguration>(Retraining.ToString());
                    }

                    var AiJsonData = Newtonsoft.Json.JsonConvert.SerializeObject(oSelectedConfiguration);
                    var insertAIDocument = BsonSerializer.Deserialize<AICustomConfiguration>(AiJsonData);
                    AIcollection.InsertOne(insertAIDocument);

                    break;

                default:
                    break;
            }
        }

        public GenericTraining StartTraining(TrainingRequestDTO RequestPayload)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CustomConfigService), nameof(StartTraining), "InitiateTraining - Started for UseCaseID " + RequestPayload.UseCaseId, string.Empty, string.Empty, RequestPayload.ClientUID, RequestPayload.DeliveryConstructUID);
            var AppIntegCollection = _database.GetCollection<AppIntegration>(CONSTANTS.AppIntegration);
            var filterBuilder = Builders<AppIntegration>.Filter;
            var AppFilter = filterBuilder.Eq(CONSTANTS.ApplicationID, RequestPayload.ApplicationId);
            var Projection = Builders<AppIntegration>.Projection.Exclude(CONSTANTS.Id);
            var IsApplicationExist = AppIntegCollection.Find(AppFilter).Project<AppIntegration>(Projection).FirstOrDefault();
            string CorrelationId = string.Empty;
            if (IsApplicationExist != null)
            {
                if (UpdateTokenInAppIntegration(RequestPayload.ApplicationId) == CONSTANTS.Success && UpdatePublicTemplateMapping(RequestPayload.ApplicationId, RequestPayload.UseCaseId, RequestPayload.QueryData) == CONSTANTS.Success)
                {
                    var MappingCollection = _database.GetCollection<PublicTemplateMapping>(CONSTANTS.PublicTemplateMapping);
                    var Builder = Builders<PublicTemplateMapping>.Filter;
                    var Projection1 = Builders<PublicTemplateMapping>.Projection.Exclude("_id");
                    var filter = Builder.Eq(CONSTANTS.UsecaseID, RequestPayload.UseCaseId);
                    var templatedata = MappingCollection.Find(filter).Project<PublicTemplateMapping>(Projection1).FirstOrDefault();
                    if (templatedata != null)
                    {
                        if (templatedata.IsMultipleApp == "yes")
                        {
                            if (templatedata.ApplicationIDs.Contains(RequestPayload.ApplicationId))
                            {
                                templatedata.ApplicationID = RequestPayload.ApplicationId;
                                templatedata.ApplicationName = IsApplicationExist.ApplicationName;
                            }
                        }
                        //else
                        //{
                        //if (RequestPayload.ApplicationId != templatedata.ApplicationID)  // -- do we need to check these conditions ? as we are already checking the constrainst from WS -- Commenting for now
                        //{
                        //    _GenericTraining.ErrorMessage = CONSTANTS.IsApplicationExist;
                        //    _GenericTraining.Status = "Error";
                        //    return _GenericTraining;
                        //}
                        //}
                        //if (RequestPayload.UseCaseUID == templatedata.UsecaseID && templatedata.UsecaseType == "Team Level")
                        //{
                        //    if (RequestPayload.UseCaseUID == "f0320924-2ee3-4398-ad7c-8bc172abd78d" && string.IsNullOrEmpty(RequestPayload.TeamAreaUId))
                        //    {
                        //        _GenericTraining.ErrorMessage = "Please provide team level UID : TeamAreaUId";
                        //        _GenericTraining.Status = "Error";
                        //        return _GenericTraining;
                        //    }
                        //}

                        //if ((RequestPayload.UseCaseUID == "877f712c-7cdc-435a-acc8-8fea3d26cc18" || RequestPayload.UseCaseUID == "6b0d8eb3-0918-4818-9655-6ca81a4ebf30") && RequestPayload.IsTeamLevelData != "1")
                        //{
                        //    {
                        //        _GenericTraining.ErrorMessage = "Please provide IsTeamLevelData : 1";
                        //        _GenericTraining.Status = "Error";
                        //        return _GenericTraining;
                        //    }
                        //}

                        var _trainingStatus = GetModelStatus(RequestPayload.ClientUID, RequestPayload.DeliveryConstructUID, RequestPayload.ApplicationId, RequestPayload.UseCaseId, RequestPayload.UserID, RequestPayload.TeamAreaUId, templatedata.UsecaseType, templatedata.IsMultipleApp, templatedata.ApplicationIDs);
                        string NewModelCorrelationID = Guid.NewGuid().ToString();
                        string requestId = string.Empty;
                        if (_trainingStatus == null)
                        {
                            //if (IsAmbulanceLane(RequestPayload.UseCaseUID) && RequestPayload.RetrainRequired == "AutoRetrain")
                            if (RequestPayload.IsAmbulanceLane.ToUpper() == "TRUE" && RequestPayload.RetrainRequired == "AutoRetrain")
                            {
                                _GenericTraining.ErrorMessage = "Training is not yet initiated .Please retrain once Training is completed";
                                _GenericTraining.Status = "Error";
                                return _GenericTraining;
                            }
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CustomConfigService), nameof(StartTraining), "CreateTrainingRequest For UseCaseID : "+ RequestPayload.UseCaseId, string.IsNullOrEmpty(NewModelCorrelationID) ? default(Guid) : new Guid(NewModelCorrelationID), string.Empty, string.Empty, RequestPayload.ClientUID, RequestPayload.DeliveryConstructUID);
                            var ingrainRequest = CreateTrainingRequest(NewModelCorrelationID, templatedata, RequestPayload);
                            InsertRequests(ingrainRequest);
                            requestId = ingrainRequest.RequestId;
                            IngrainResponseData CallBackResponse = new IngrainResponseData
                            {
                                CorrelationId = ingrainRequest.CorrelationId,
                                Status = "Initiated",
                                Message = CONSTANTS.TrainingInitiated,
                                ErrorMessage = string.Empty,
                            };

                            _GenericTraining.Message = CONSTANTS.TrainingInitiated;
                            _GenericTraining.CorrelationId = ingrainRequest.CorrelationId;
                            _GenericTraining.Status = CONSTANTS.P;

                            //// this step is required only for SPA App
                            //if (!string.IsNullOrEmpty(templatedata.ApplicationName) && templatedata.ApplicationName.ToUpper() == CONSTANTS.SPAAPP)
                            //{
                            //    string callbackResonse = CallbackResponse(CallBackResponse, templatedata.ApplicationName, RequestPayload.ResponseCallbackUrl, ingrainRequest.ClientId, ingrainRequest.DeliveryconstructId, ingrainRequest.AppID, RequestPayload.UseCaseId, ingrainRequest.RequestId, ingrainRequest.CreatedByUser);
                            //    if (callbackResonse == CONSTANTS.ErrorMessage)
                            //    {
                            //        LOGGING.LogManager.Logger.LogProcessInfo(typeof(CustomConfigService), nameof(StartTraining), "CALL BACK URL ERROR :" + callbackResonse, string.Empty, string.Empty, RequestPayload.ClientUID, RequestPayload.DeliveryConstructUID);
                            //        _GenericTraining.ErrorMessage = CONSTANTS.TokenFailed;
                            //        _GenericTraining.Status = CONSTANTS.ErrorMessage;
                            //        return _GenericTraining;
                            //    }
                            //}
                            return _GenericTraining;
                        }
                        else
                        {
                            requestId = _trainingStatus.UniqueId;
                            if (_trainingStatus.Status == null || _trainingStatus.Status == CONSTANTS.E)
                            {
                                // if (IsAmbulanceLane(RequestPayload.UseCaseUID) && RequestPayload.RetrainRequired == "AutoRetrain")
                                if (RequestPayload.IsAmbulanceLane.ToUpper() == "TRUE" && RequestPayload.RetrainRequired == "AutoRetrain")
                                {
                                    _GenericTraining.CorrelationId = _trainingStatus.CorrelationId;
                                    _GenericTraining.ErrorMessage = "Training is not Complete .Please retrain once Training is completed";
                                    _GenericTraining.Status = "Error";
                                    return _GenericTraining;
                                }
                                if (!string.IsNullOrEmpty(_trainingStatus.CorrelationId))
                                {
                                    NewModelCorrelationID = _trainingStatus.CorrelationId;
                                }
                                if (_trainingStatus.Status == CONSTANTS.E)
                                {
                                    NewModelCorrelationID = Guid.NewGuid().ToString();
                                    _GenericTraining.Status = CONSTANTS.ErrorMessage;
                                    _GenericTraining.CorrelationId = _trainingStatus.CorrelationId;
                                    _GenericTraining.Message = CONSTANTS.Error;
                                    var ingrainRequest = CreateTrainingRequest(NewModelCorrelationID, templatedata, RequestPayload);
                                    InsertRequests(ingrainRequest);

                                    //// this step is required only for SPA App
                                    //if (!string.IsNullOrEmpty(templatedata.ApplicationName) && templatedata.ApplicationName.ToUpper() == CONSTANTS.SPAAPP)
                                    //{
                                    //    IngrainResponseData CallBackResponse = new IngrainResponseData
                                    //    {
                                    //        CorrelationId = ingrainRequest.CorrelationId,
                                    //        Status = "Initiated",
                                    //        Message = CONSTANTS.TrainingInitiated + " ,as previous request " + _trainingStatus.CorrelationId + " failed with Error :" + _trainingStatus.ErrorMessage,
                                    //        ErrorMessage = string.Empty,
                                    //    };
                                    //    string callbackResonse = CallbackResponse(CallBackResponse, templatedata.ApplicationName, RequestPayload.ResponseCallbackUrl, RequestPayload.ClientUID, RequestPayload.DeliveryConstructUID, RequestPayload.ApplicationId, RequestPayload.UseCaseId, requestId, RequestPayload.UserID);
                                    //    _GenericTraining.Message = CallBackResponse.Message;
                                    //}

                                    _GenericTraining.CorrelationId = ingrainRequest.CorrelationId;
                                    _GenericTraining.Status = CONSTANTS.P;
                                    return _GenericTraining;
                                }
                                else
                                {
                                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(CustomConfigService), nameof(StartTraining), "_trainingStatus :" + NewModelCorrelationID + "--" + _trainingStatus.Status + "--NEW--" + NewModelCorrelationID, string.Empty, string.Empty, RequestPayload.ClientUID, RequestPayload.DeliveryConstructUID);
                                    var ingrainRequest = CreateTrainingRequest(NewModelCorrelationID, templatedata, RequestPayload);
                                    InsertRequests(ingrainRequest);
                                    _GenericTraining.Message = CONSTANTS.TrainingInitiated;
                                    _GenericTraining.CorrelationId = ingrainRequest.CorrelationId;
                                    _GenericTraining.Status = CONSTANTS.P;

                                    //// this step is required only for SPA App
                                    //if (!string.IsNullOrEmpty(templatedata.ApplicationName) && templatedata.ApplicationName.ToUpper() == CONSTANTS.SPAAPP)
                                    //{
                                    //    IngrainResponseData CallBackResponse = new IngrainResponseData
                                    //    {
                                    //        CorrelationId = ingrainRequest.CorrelationId,
                                    //        Status = "Initiated",
                                    //        Message = CONSTANTS.TrainingInitiated,
                                    //        ErrorMessage = string.Empty,
                                    //    };
                                    //    string callbackResonse = CallbackResponse(CallBackResponse, templatedata.ApplicationName, RequestPayload.ResponseCallbackUrl, RequestPayload.ClientUID, RequestPayload.DeliveryConstructUID, RequestPayload.ApplicationId, RequestPayload.UseCaseId, requestId, RequestPayload.UserID);

                                    //    if (callbackResonse == CONSTANTS.ErrorMessage)
                                    //    {
                                    //        LOGGING.LogManager.Logger.LogProcessInfo(typeof(CustomConfigService), nameof(StartTraining), "CALL BACK URL ERROR :" + callbackResonse, string.Empty, string.Empty, RequestPayload.ClientUID, RequestPayload.DeliveryConstructUID);
                                    //        _GenericTraining.ErrorMessage = CONSTANTS.TokenFailed;
                                    //        _GenericTraining.Status = CONSTANTS.ErrorMessage;
                                    //        return _GenericTraining;
                                    //    }
                                    //}
                                    return _GenericTraining;
                                }
                            }
                            else
                            {
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(CustomConfigService), nameof(StartTraining), "_trainingStatus:" + _trainingStatus.CorrelationId + "--" + _trainingStatus.Status, string.Empty, string.Empty, RequestPayload.ClientUID, RequestPayload.DeliveryConstructUID);
                                if (_trainingStatus.Status == CONSTANTS.P || _trainingStatus.Status == CONSTANTS.I)
                                {
                                    // if (IsAmbulanceLane(RequestPayload.UseCaseUID) && RequestPayload.RetrainRequired == "AutoRetrain")
                                    if (RequestPayload.IsAmbulanceLane.ToUpper() == "TRUE" && RequestPayload.RetrainRequired == "AutoRetrain")
                                    {
                                        _GenericTraining.CorrelationId = _trainingStatus.CorrelationId;
                                        _GenericTraining.ErrorMessage = "Training is not yet Complete .Please retrain once Training is completed";
                                        _GenericTraining.Status = "Error";
                                        return _GenericTraining;
                                    }
                                    _GenericTraining.Status = _trainingStatus.Status;
                                    _GenericTraining.Message = CONSTANTS.TrainingInprogress;
                                    _GenericTraining.CorrelationId = _trainingStatus.CorrelationId;
                                    return _GenericTraining;
                                }
                                else
                                {
                                    //if (IsAmbulanceLane(RequestPayload.UseCaseUID) && RequestPayload.RetrainRequired == "AutoRetrain")
                                    if (RequestPayload.IsAmbulanceLane.ToUpper() == "TRUE" && RequestPayload.RetrainRequired == "AutoRetrain")
                                    {
                                        var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
                                        var filteRetrain = Builders<BsonDocument>.Filter;
                                        FilterDefinition<BsonDocument> filterQueue = null;
                                        filterQueue = filteRetrain.Eq(CONSTANTS.ClientId, RequestPayload.ClientUID) & filteRetrain.Eq(CONSTANTS.DeliveryconstructId, RequestPayload.DeliveryConstructUID) & filteRetrain.Eq(CONSTANTS.TemplateUseCaseID, RequestPayload.UseCaseId) & filteRetrain.Eq(CONSTANTS.AppID, RequestPayload.ApplicationId & filteRetrain.Eq(CONSTANTS.pageInfo, "AutoRetrain") & filteRetrain.Eq("CorrelationId", _trainingStatus.CorrelationId));
                                        var ProjectionRetrain = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
                                        var result = collection.Find(filterQueue).Project<BsonDocument>(ProjectionRetrain).ToList();
                                        result = result.OrderByDescending(item => item["CreatedOn"]).ToList();
                                        if (result.Count > 0)
                                        {
                                            var correlationId = result[0][CONSTANTS.CorrelationId].ToString();
                                            var errorMessage = result[0][CONSTANTS.Message].ToString();
                                            var status = result[0][CONSTANTS.Status].ToString();
                                            var createdOn = result[0]["CreatedOn"].ToString();
                                            var uniqueId = result[0]["RequestId"].ToString();
                                            var createdByUser = result[0]["CreatedByUser"].ToString();
                                            if (status == "C")
                                            {
                                                var lastUpdateHour = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                                                DateTime createdOnDateFormat = DateTime.Parse(createdOn);
                                                DateTime todaysDate = DateTime.Parse(lastUpdateHour);
                                                var dateDiff = (todaysDate - createdOnDateFormat).TotalDays;
                                                if (dateDiff > 1)
                                                {
                                                    var req = CreateTrainingRequest(_trainingStatus.CorrelationId, templatedata, RequestPayload);
                                                    InsertRequests(req);
                                                    requestId = req.RequestId;
                                                    //// this step is required only for SPA App
                                                    //if (!string.IsNullOrEmpty(templatedata.ApplicationName) && templatedata.ApplicationName.ToUpper() == CONSTANTS.SPAAPP)
                                                    //{
                                                    //    IngrainResponseData ResponseCallBackResponse = new IngrainResponseData
                                                    //    {
                                                    //        CorrelationId = req.CorrelationId,
                                                    //        Status = "ReTrain - Initiated",
                                                    //        Message = CONSTANTS.ReTrainingInitiated,
                                                    //        ErrorMessage = string.Empty,
                                                    //    };
                                                    //    string ResponseCallBackResponseurl = CallbackResponse(ResponseCallBackResponse, templatedata.ApplicationName, RequestPayload.ResponseCallbackUrl, req.ClientId, req.DeliveryconstructId, req.AppID, RequestPayload.UseCaseId, req.RequestId, req.CreatedByUser);
                                                    //}
                                                    _GenericTraining.Message = CONSTANTS.ReTrainingInitiated;
                                                    _GenericTraining.CorrelationId = req.CorrelationId;
                                                    _GenericTraining.Status = CONSTANTS.P;
                                                    return _GenericTraining;
                                                }
                                                else
                                                {
                                                    //// this step is required only for SPA App
                                                    //if (!string.IsNullOrEmpty(templatedata.ApplicationName) && templatedata.ApplicationName.ToUpper() == CONSTANTS.SPAAPP)
                                                    //{
                                                    //    IngrainResponseData ResponseCallBackResponse = new IngrainResponseData
                                                    //    {
                                                    //        CorrelationId = _trainingStatus.CorrelationId,
                                                    //        Status = "Completed",
                                                    //        Message = CONSTANTS.ReTrainingOneDayCheck,
                                                    //        ErrorMessage = CONSTANTS.ReTrainingOneDayCheck,
                                                    //        DataPointsCount = _dataPointCount,
                                                    //        DataPointsWarning = _dataPointsWarning

                                                    //    };
                                                    //    string ResponseCallBackResponseurl = CallbackResponse(ResponseCallBackResponse, templatedata.ApplicationName, RequestPayload.ResponseCallbackUrl, RequestPayload.ClientUID, RequestPayload.DeliveryConstructUID, RequestPayload.ApplicationId, RequestPayload.UseCaseId, uniqueId, createdByUser);
                                                    //}
                                                    _GenericTraining.Message = CONSTANTS.ReTrainingOneDayCheck;
                                                    _GenericTraining.CorrelationId = _trainingStatus.CorrelationId;
                                                    _GenericTraining.Status = "Completed";
                                                    _GenericTraining.DataPointsCount = _dataPointCount;
                                                    _GenericTraining.DataPointsWarning = _dataPointsWarning;
                                                    return _GenericTraining;
                                                }
                                            }
                                            if (status == "E")
                                            {
                                                var queueCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
                                                var filterBuilder1 = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId) & filteRetrain.Eq("RequestId", uniqueId);
                                                queueCollection.DeleteMany(filterBuilder1);
                                                _GenericTraining.Status = CONSTANTS.ErrorMessage;
                                                _GenericTraining.CorrelationId = correlationId;
                                                _GenericTraining.ErrorMessage = errorMessage;
                                                _GenericTraining.Message = CONSTANTS.RetrainingUnsucess;
                                                IngrainResponseData callback = new IngrainResponseData
                                                {
                                                    CorrelationId = correlationId,
                                                    Status = CONSTANTS.ErrorMessage,
                                                    Message = CONSTANTS.RetrainingUnsucess,
                                                    ErrorMessage = errorMessage
                                                };
                                                //// this step is required only for SPA App
                                                //if (!string.IsNullOrEmpty(templatedata.ApplicationName) && templatedata.ApplicationName.ToUpper() == CONSTANTS.SPAAPP)
                                                //{
                                                //    string callbackRes = CallbackResponse(callback, templatedata.ApplicationName, RequestPayload.ResponseCallbackUrl, RequestPayload.ClientUID, RequestPayload.DeliveryConstructUID, RequestPayload.ApplicationId, RequestPayload.UseCaseId, requestId, RequestPayload.UserID);
                                                //}
                                                return _GenericTraining;
                                            }
                                            if (status != "E")
                                            {
                                                _GenericTraining.Status = status;
                                                _GenericTraining.Message = CONSTANTS.ReTrainingInprogress;
                                                _GenericTraining.CorrelationId = _trainingStatus.CorrelationId;
                                                return _GenericTraining;
                                            }
                                        }
                                        var ingrainRequest = CreateTrainingRequest(_trainingStatus.CorrelationId, templatedata, RequestPayload);
                                        InsertRequests(ingrainRequest);
                                        requestId = ingrainRequest.RequestId;

                                        // this step is required only for SPA App
                                        //if (!string.IsNullOrEmpty(templatedata.ApplicationName) && templatedata.ApplicationName.ToUpper() == CONSTANTS.SPAAPP)
                                        //{
                                        //    IngrainResponseData CallBackResponse = new IngrainResponseData
                                        //    {
                                        //        CorrelationId = ingrainRequest.CorrelationId,
                                        //        Status = "ReTrain - Initiated",
                                        //        Message = CONSTANTS.ReTrainingInitiated,
                                        //        ErrorMessage = string.Empty,
                                        //    };
                                        //    string callbackResonse = CallbackResponse(CallBackResponse, templatedata.ApplicationName, RequestPayload.ResponseCallbackUrl, ingrainRequest.ClientId, ingrainRequest.DeliveryconstructId, ingrainRequest.AppID, RequestPayload.UseCaseId, ingrainRequest.RequestId, ingrainRequest.CreatedByUser);
                                        //}

                                        _GenericTraining.Message = CONSTANTS.ReTrainingInitiated;
                                        _GenericTraining.CorrelationId = ingrainRequest.CorrelationId;
                                        _GenericTraining.Status = CONSTANTS.P;
                                        return _GenericTraining;
                                    }
                                    else
                                    {
                                        if (RequestPayload.UseCaseId == "f0320924-2ee3-4398-ad7c-8bc172abd78d")
                                        {
                                            if (templatedata.IsMultipleApp == "yes")
                                            {
                                                if (templatedata.ApplicationIDs.Contains(RequestPayload.ApplicationId))
                                                {
                                                    var collection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
                                                    var builder = Builders<DeployModelsDto>.Filter;
                                                    var filter1 = builder.Eq(CONSTANTS.CorrelationId, _trainingStatus.CorrelationId);
                                                    var builder1 = Builders<DeployModelsDto>.Update;
                                                    var update = builder1.Set(CONSTANTS.IsMutipleApp, true);
                                                    collection.UpdateMany(filter1, update);
                                                }
                                            }
                                            //// this step is required only for SPA App
                                            //if (!string.IsNullOrEmpty(templatedata.ApplicationName) && templatedata.ApplicationName.ToUpper() == CONSTANTS.SPAAPP)
                                            //{
                                            //    IngrainResponseData CallBackResponse = new IngrainResponseData
                                            //    {
                                            //        CorrelationId = _trainingStatus.CorrelationId,
                                            //        Status = "Completed",
                                            //        Message = "Training Completed",
                                            //        ErrorMessage = string.Empty,
                                            //        DataPointsWarning = _dataPointsWarning,
                                            //        DataPointsCount = _dataPointCount
                                            //    };
                                            //    string callbackResonse = CallbackResponse(CallBackResponse, templatedata.ApplicationName, RequestPayload.ResponseCallbackUrl, RequestPayload.ClientUID, RequestPayload.DeliveryConstructUID, RequestPayload.ApplicationId, RequestPayload.UseCaseId, requestId, RequestPayload.UserID);
                                            //}
                                        }
                                        _GenericTraining.Status = "Completed";
                                        _GenericTraining.Message = "Training completed for the usecaseid";
                                        _GenericTraining.CorrelationId = _trainingStatus.CorrelationId;
                                        _GenericTraining.DataPointsCount = _dataPointCount;
                                        _GenericTraining.DataPointsWarning = _dataPointsWarning;
                                        return _GenericTraining;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        _GenericTraining.ErrorMessage = CONSTANTS.UsecaseNotAvailable;
                        _GenericTraining.Status = "Error";
                        return _GenericTraining;
                    }
                }
                else
                {
                    _GenericTraining.ErrorMessage = "Error Updating AppIntegration,PublicTemplatingMapping";
                    _GenericTraining.Status = "Error";
                    return _GenericTraining;
                }
            }
            else
            {
                _GenericTraining.ErrorMessage = CONSTANTS.IsApplicationExist;
                _GenericTraining.Status = "Error";
                return _GenericTraining;
            }
        }


        public AIGENERICSERVICE.TrainingResponse TrainAIServiceModel(HttpContext httpContext, string resourceId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CustomConfigService), nameof(TrainAIServiceModel), CONSTANTS.START, string.Empty, string.Empty, string.Empty, string.Empty);
            AIGENERICSERVICE.TrainingRequest trainingRequest = new AIGENERICSERVICE.TrainingRequest();
            string status = "InProgress";
            string statusMessage = "Training is in progress";
            string flag = CONSTANTS.Null;
            string pageInfo = "Ingest_Train";
            bool isCLusteringService = false;
            bool isIntentEntity = false;
            IFormCollection collection = httpContext.Request.Form;
            trainingRequest.ApplicationId = collection["ApplicationId"];
            trainingRequest.ClientId = collection["ClientId"];
            trainingRequest.DeliveryConstructId = collection["DeliveryConstructId"];
            trainingRequest.ServiceId = collection["ServiceId"];
            trainingRequest.UsecaseId = collection["UsecaseId"];
            trainingRequest.ModelName = collection["ModelName"];
            trainingRequest.DataSource = collection["DataSource"];
            trainingRequest.DataSourceDetails = collection["DataSourceDetails"];
            trainingRequest.UserId = collection["UserId"];
            trainingRequest.ResponseCallBackUrl = collection["ResponseCallbackUrl"];
            string correlationId = collection["CorrelationId"];
            string language = collection["Language"];

            trainingRequest.CustomDataSourceDetails = collection["CustomDataPull"];

            string pyModelName = string.Empty;
            bool isIntentRetrain = false;
            var datasetUId = collection["DataSetUId"];
            var CustomDataSourceDetails = collection["CustomDataPull"];
            string CustomSourceItems = string.Empty;
            trainingRequest.DataSetUId = datasetUId;
            if (trainingRequest.DataSetUId == "undefined" || trainingRequest.DataSetUId == "null")
                trainingRequest.DataSetUId = null;

            string trainingMessage = "Training Initiated";
            CustomInputPayload appPayload = new CustomInputPayload();
            AIGENERICSERVICE.TrainingResponse trainingResponse
                = new AIGENERICSERVICE.TrainingResponse(trainingRequest.ClientId,
                                                        trainingRequest.DeliveryConstructId,
                                                        trainingRequest.ServiceId,
                                                        trainingRequest.UsecaseId,
                                                        trainingRequest.ModelName,
                                                        trainingRequest.UserId);

            AIServiceRequestStatus ingestRequest = new AIServiceRequestStatus();
            ClusteringAPIModel clusteringAPIModel = new ClusteringAPIModel();
            ParentFile parentDetail = new ParentFile();
            MethodReturn<object> message = new MethodReturn<object>();
            string baseUrl = string.Empty;
            string apiPath = string.Empty;
            USECASE.UsecaseDetails usecaseDetails = GetUsecaseDetails(trainingRequest.UsecaseId.Trim());
            bool iscarryOutRetraining = false;
            bool isOnline = false;
            bool isOffline = false;
            JObject training = new JObject();
            JObject prediction = new JObject();
            JObject retraining = new JObject();

            if (usecaseDetails.IsCarryOutRetraining || usecaseDetails.IsOffline || usecaseDetails.IsOnline)
            {
                iscarryOutRetraining = usecaseDetails.IsCarryOutRetraining;
                isOnline = usecaseDetails.IsOnline;
                isOffline = usecaseDetails.IsOffline;
                training = JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(usecaseDetails.Training));// JsonConvert.DeserializeObject<JObject>(usecaseDetails.Training.ToString());
                prediction = JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(usecaseDetails.Prediction));
                retraining = JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(usecaseDetails.Retraining));
            }

            AIModelFrequency aIModelFrequency = new AIModelFrequency();
            ClustModelFrequency clustModelFrequency = new ClustModelFrequency();

            Service service = GetAiCoreServiceDetails(trainingRequest.ServiceId);
            //Asset Usage
            auditTrailLog.ApplicationName = usecaseDetails.ApplicationName;
            auditTrailLog.ApplicationID = trainingRequest.ApplicationId;
            auditTrailLog.ClientId = trainingRequest.ClientId;
            auditTrailLog.DCID = trainingRequest.DeliveryConstructId;
            auditTrailLog.UseCaseId = trainingRequest.UsecaseId;
            auditTrailLog.CreatedBy = trainingRequest.UserId;
            auditTrailLog.ProcessName = CONSTANTS.TrainingName;
            auditTrailLog.UsageType = CONSTANTS.AssetUsage;
            if (string.IsNullOrEmpty(auditTrailLog.ApplicationName))
            {
                auditTrailLog.FeatureName = service.ServiceName;
            }
            else
            {
                auditTrailLog.FeatureName = auditTrailLog.ApplicationName + "-" + service.ServiceName;
            }
            CommonUtility.AuditTrailLog(auditTrailLog, configSetting);

            CheckMandatoryFields(service,  usecaseDetails, trainingRequest);

            if (service.ServiceCode == "CLUSTERING" || service.ServiceCode == "WORDCLOUD")
            {
                isCLusteringService = true;
                baseUrl = appSettings.Value.ClusteringPythonURL;
                apiPath = CONSTANTS.Clustering_ModelTraining;
                bool isExists = _clusteringAPI.GetClusteringModelName(clusteringAPIModel);
                if (isExists && string.IsNullOrEmpty(correlationId))
                {
                    throw new Exception("Model Name already exist");
                }

                clusteringAPIModel.ServiceID = trainingRequest.ServiceId;
                clusteringAPIModel.ClientID = trainingRequest.ClientId;
                clusteringAPIModel.DCUID = trainingRequest.DeliveryConstructId;
                clusteringAPIModel.UserId = trainingRequest.UserId;
                clusteringAPIModel.ModelName = trainingRequest.ModelName;
                clusteringAPIModel.Ngram = usecaseDetails.Ngram;
                clusteringAPIModel.StopWords = usecaseDetails.StopWords;
                clusteringAPIModel.ScoreUniqueName = usecaseDetails.ScoreUniqueName;
                clusteringAPIModel.Threshold_TopnRecords = usecaseDetails.Threshold_TopnRecords;
                clusteringAPIModel.DataSetUId = usecaseDetails.DataSetUID;
                clusteringAPIModel.MaxDataPull = usecaseDetails.MaxDataPull;
                clusteringAPIModel.IsCarryOutRetraining = usecaseDetails.IsCarryOutRetraining;
                clusteringAPIModel.IsOffline = usecaseDetails.IsOffline;
                clusteringAPIModel.IsOnline = usecaseDetails.IsOnline;
                clusteringAPIModel.Training = clustModelFrequency;
                clusteringAPIModel.Retraining = clustModelFrequency;
                clusteringAPIModel.Prediction = clustModelFrequency;
                if (usecaseDetails.IsCarryOutRetraining && retraining != null && ((JContainer)retraining).HasValues)
                {
                    clusteringAPIModel.Retraining = new ClustModelFrequency();
                    this.AssignFrequencyClustering(clusteringAPIModel, clusteringAPIModel.Retraining, retraining, CONSTANTS.Retraining);

                }
                if (clusteringAPIModel.IsOffline)
                {
                    if (training != null && ((JContainer)training).HasValues)
                    {
                        clusteringAPIModel.Training = new ClustModelFrequency();
                        this.AssignFrequencyClustering(clusteringAPIModel, clusteringAPIModel.Training, training, CONSTANTS.Training);
                    }
                    if (prediction != null && ((JContainer)prediction).HasValues)
                    {
                        clusteringAPIModel.Prediction = new ClustModelFrequency();
                        this.AssignFrequencyClustering(clusteringAPIModel, clusteringAPIModel.Prediction, prediction, CONSTANTS.Prediction);
                    }
                }
            }
            else if (service.ServiceCode == "NLPINTENT")
            {
                isIntentEntity = true;
                usecaseDetails.ApplicationId = trainingRequest.ApplicationId;
                baseUrl = appSettings.Value.AICorePythonURL;
                apiPath = service.PrimaryTrainApiUrl;
                ModelsList model = GetAICoreModels(trainingRequest.ClientId, trainingRequest.DeliveryConstructId, trainingRequest.ServiceId, trainingRequest.UserId);
                if (string.IsNullOrEmpty(correlationId) && model.ModelStatus != null)
                {
                    var isModelNameExist = model.ModelStatus.Where(x => x.ModelName.ToLower() == trainingRequest.ModelName.ToLower()).ToList();

                    if (isModelNameExist.Count > 0)
                    {
                        throw new Exception("Model Name already exist");
                    }
                }
            }
            else
            {
                baseUrl = appSettings.Value.AICorePythonURL;
                apiPath = service.PrimaryTrainApiUrl;
                if (trainingRequest.DataSource != "Phoenix" && !string.IsNullOrEmpty(trainingRequest.DataSource))
                {
                    bool isExists = this.GetAIServiceModelName(trainingRequest.ModelName, trainingRequest.ServiceId, trainingRequest.ClientId, trainingRequest.DeliveryConstructId, trainingRequest.UserId);
                    if (isExists && string.IsNullOrEmpty(correlationId))
                    {
                        throw new Exception("Model Name already exist");
                    }
                }
            }
            ////Retrain checking Start
            if (string.IsNullOrEmpty(correlationId))
            {
                correlationId = Guid.NewGuid().ToString();
            }
            else
            {
                if (isCLusteringService)
                {
                    clusteringAPIModel.retrain = true;
                    trainingMessage = "Re-Training Initiated";

                }
                else if (isIntentEntity)
                {
                    var corrDetails = GetAICoreModelPath(correlationId);
                    if (corrDetails == null)
                    {
                        throw new Exception("Correlation id not found");
                    }
                    else
                    {
                        if (corrDetails.ServiceId != trainingRequest.ServiceId)
                        {
                            throw new Exception("ServiceId doesn't match the trained model ServiceId");
                        }
                        if (corrDetails.ModelStatus == "InProgress" || corrDetails.ModelStatus == "Upload InProgress")
                        {
                            throw new Exception("Unable to initiate training, as dataupload/training is already in progress");
                        }
                        if (corrDetails.ClientId != trainingRequest.ClientId || corrDetails.DeliveryConstructId != trainingRequest.DeliveryConstructId)
                        {
                            throw new Exception("ClientId or DCId doesn't match the trained model Client and DC");
                        }
                        else
                        {
                            if (trainingRequest.ModelName == null)
                                trainingRequest.ModelName = corrDetails.ModelName;
                            pyModelName = corrDetails.PythonModelName;
                            pageInfo = "Retrain";
                            status = "InProgress";
                            statusMessage = "ReTrain is in Progress";
                            flag = "AutoRetrain";
                            ingestRequest.CorrelationId = correlationId;
                           
                        }
                    }
                }
                else
                {
                    var corrDetails = GetAICoreModelPath(correlationId);
                    if (corrDetails.UsecaseId != trainingRequest.UsecaseId)
                    {
                        throw new Exception("UsecaseId doesn't match the trained model usecaseId");
                    }
                    if (corrDetails.ServiceId != trainingRequest.ServiceId)
                    {
                        throw new Exception("ServiceId doesn't match the trained model ServiceId");
                    }
                    if (corrDetails.ModelStatus == "InProgress" || corrDetails.ModelStatus == "Upload InProgress")
                    {
                        throw new Exception("Unable to initiate Re-training, as dataupload/training is already in progress");
                    }
                    if (corrDetails.ClientId != trainingRequest.ClientId || corrDetails.DeliveryConstructId != trainingRequest.DeliveryConstructId)
                    {
                        throw new Exception("ClientId or DCId doesn't match the trained model Client and DC");
                    }
                    pageInfo = "Retrain";
                    status = "InProgress";
                    statusMessage = "ReTrain is in Progress";
                    flag = "AutoRetrain";
                }
            }
            //Retrain checking end

            if (isCLusteringService)
            {
                var data = this.GetClusteringDetails(usecaseDetails.CorrelationId);
                if (data.Count == 0)
                {
                    throw new Exception("UsecaseId doesn't match the ServiceId");
                }
                string createdByUser = trainingRequest.UserId;
                if (appSettings.Value.isForAllData)
                {
                    if (!string.IsNullOrEmpty(Convert.ToString(trainingRequest.UserId)))
                        createdByUser = _encryptionDecryption.Encrypt(Convert.ToString(trainingRequest.UserId));
                }
                clusteringAPIModel.CorrelationId = correlationId;
                clusteringAPIModel.SelectedModels = JsonConvert.DeserializeObject<JObject>(data["SelectedModels"].ToString());
                clusteringAPIModel.Columnsselectedbyuser = usecaseDetails.InputColumns;
                clusteringAPIModel.ParamArgs = data["ParamArgs"].ToString();
                clusteringAPIModel.ProblemType = JsonConvert.DeserializeObject<JObject>(data["ProblemType"].ToString());
                clusteringAPIModel.StopWords = usecaseDetails.StopWords;
                clusteringAPIModel.ServiceID = trainingRequest.ServiceId;
                clusteringAPIModel.UniId = Guid.NewGuid().ToString();
                clusteringAPIModel.ModelName = trainingRequest.ModelName;
                clusteringAPIModel.ClientID = trainingRequest.ClientId;
                clusteringAPIModel.DCUID = trainingRequest.DeliveryConstructId;
                clusteringAPIModel.CreatedBy = createdByUser;
                clusteringAPIModel.PageInfo = service.ServiceCode == "WORDCLOUD" ? "wordcloud" : "InvokeIngestData";//"Ingest_Train";
                clusteringAPIModel.CreatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                clusteringAPIModel.ModifiedBy = createdByUser;
                clusteringAPIModel.ModifiedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                clusteringAPIModel.DataSource = trainingRequest.DataSource;
                clusteringAPIModel.Language = language;
                clusteringAPIModel.DataSetUId = trainingRequest.DataSetUId;
                clusteringAPIModel.Ngram = usecaseDetails.Ngram;
                clusteringAPIModel.ScoreUniqueName = usecaseDetails.ScoreUniqueName;
                clusteringAPIModel.Threshold_TopnRecords = usecaseDetails.Threshold_TopnRecords;
                clusteringAPIModel.MaxDataPull = usecaseDetails.MaxDataPull;
                clusteringAPIModel.IsCarryOutRetraining = usecaseDetails.IsCarryOutRetraining;
                clusteringAPIModel.IsOffline = usecaseDetails.IsOffline;
                clusteringAPIModel.IsOnline = usecaseDetails.IsOnline;
                clusteringAPIModel.Training = clustModelFrequency;
                clusteringAPIModel.Retraining = clustModelFrequency;
                clusteringAPIModel.Prediction = clustModelFrequency;
                clusteringAPIModel.UsecaseId = trainingRequest.UsecaseId;
                if (usecaseDetails.IsCarryOutRetraining && retraining != null && ((JContainer)retraining).HasValues)
                {
                    clusteringAPIModel.Retraining = new ClustModelFrequency();
                    this.AssignFrequencyClustering(clusteringAPIModel, clusteringAPIModel.Retraining, retraining, CONSTANTS.Retraining);

                }
                if (clusteringAPIModel.IsOffline)
                {
                    if (training != null && ((JContainer)training).HasValues)
                    {
                        clusteringAPIModel.Training = new ClustModelFrequency();
                        this.AssignFrequencyClustering(clusteringAPIModel, clusteringAPIModel.Training, training, CONSTANTS.Training);
                    }
                    if (prediction != null && ((JContainer)prediction).HasValues)
                    {
                        clusteringAPIModel.Prediction = new ClustModelFrequency();
                        this.AssignFrequencyClustering(clusteringAPIModel, clusteringAPIModel.Prediction, prediction, CONSTANTS.Prediction);
                    }
                }
            }
            else
            {
                if (!isIntentEntity)
                {
                    if (usecaseDetails.ServiceId != trainingRequest.ServiceId)
                    {
                        throw new Exception("UsecaseId doesn't match the ServiceId");
                    }
                }
                ingestRequest.CorrelationId = correlationId;
                ingestRequest.SelectedColumnNames = usecaseDetails.InputColumns;
                ingestRequest.ServiceId = trainingRequest.ServiceId;
                ingestRequest.UniId = Guid.NewGuid().ToString();
                ingestRequest.ModelName = trainingRequest.ModelName;
                ingestRequest.ClientId = trainingRequest.ClientId;
                ingestRequest.DeliveryconstructId = trainingRequest.DeliveryConstructId;
                ingestRequest.CreatedByUser = trainingRequest.UserId;
                ingestRequest.PageInfo = pageInfo;//"Ingest_Train";
                ingestRequest.CreatedOn = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                ingestRequest.ModifiedByUser = trainingRequest.UserId;
                ingestRequest.ModifiedOn = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                ingestRequest.DataSource = trainingRequest.DataSource;
                ingestRequest.Language = language;
                ingestRequest.ApplicationId = trainingRequest.ApplicationId;
                ingestRequest.UsecaseId = trainingRequest.UsecaseId;
                ingestRequest.DataSetUId = trainingRequest.DataSetUId;
                ingestRequest.StopWords = usecaseDetails.StopWords;
                ingestRequest.ScoreUniqueName = usecaseDetails.ScoreUniqueName;
                ingestRequest.Threshold_TopnRecords = usecaseDetails.Threshold_TopnRecords;
                ingestRequest.MaxDataPull = usecaseDetails.MaxDataPull;
            }
            
            //if training data is from phoenix
            if (trainingRequest.DataSource == "Entity")
            {
                _filepath = new Filepath();
                _filepath.fileList = "null";
                parentFile = new ParentFile();
                parentFile.Type = "null";
                parentFile.Name = "null";
                string Entities = string.Empty;
                var entityitems = collection[CONSTANTS.pad];
                var metricitems = collection[CONSTANTS.metrics];
                var InstaMl = collection[CONSTANTS.InstaMl];
                var EntitiesNames = collection[CONSTANTS.EntitiesName];
                var MetricsNames = collection[CONSTANTS.MetricNames];
                if (entityitems.Count() > 0)
                {
                    foreach (var item in entityitems)
                    {
                        if (item != "{}")
                        {
                            Entities += item;
                            // dataSource = "Entity";

                        }
                        else
                            Entities = CONSTANTS.Null;
                    }
                }
                if (isCLusteringService)
                {
                    JObject entities = JObject.Parse(clusteringAPIModel.ParamArgs);
                    FileUpload fileUpload = new FileUpload
                    {
                        CorrelationId = clusteringAPIModel.CorrelationId,
                        ClientUID = clusteringAPIModel.ClientID,
                        DeliveryConstructUId = clusteringAPIModel.DCUID,
                        Parent = parentFile,
                        Flag = CONSTANTS.Null,
                        mapping = CONSTANTS.Null,
                        mapping_flag = CONSTANTS.False,
                        metric = CONSTANTS.Null,
                        InstaMl = CONSTANTS.Null,
                        fileupload = _filepath,
                        Customdetails = CONSTANTS.Null
                    };
                    if (entityitems.Count > 0)
                        fileUpload.pad = Entities;
                    else
                        fileUpload.pad = entities["pad"].ToString();
                    clusteringAPIModel.ParamArgs = fileUpload.ToJson();
                }
                else
                {
                    FileUpload fileUpload = new FileUpload
                    {
                        CorrelationId = ingestRequest.CorrelationId,
                        ClientUID = ingestRequest.ClientId,
                        DeliveryConstructUId = ingestRequest.DeliveryconstructId,
                        Parent = parentFile,
                        Flag = CONSTANTS.Null,
                        mapping = CONSTANTS.Null,
                        mapping_flag = CONSTANTS.False,
                        pad = Entities,
                        metric = CONSTANTS.Null,
                        InstaMl = CONSTANTS.Null,
                        fileupload = _filepath,
                        Customdetails = CONSTANTS.Null

                    };
                    ingestRequest.SourceDetails = fileUpload.ToJson();
                }
            }

            if (trainingRequest.DataSource == "Phoenix")
            {
                var datasourceDetails = JObject.Parse(usecaseDetails.SourceDetails.ToString());
                datasourceDetails["ClientUID"] = trainingRequest.ClientId;
                datasourceDetails["DeliveryConstructUId"] = trainingRequest.DeliveryConstructId;
                datasourceDetails["CorrelationId"] = ingestRequest.CorrelationId;
                if (datasourceDetails["metric"].ToString() != "null" && datasourceDetails["metric"].ToString() != "")
                {
                    JObject metric = JObject.Parse(datasourceDetails["metric"].ToString());
                    metric["startDate"] = metric["endDate"];
                    metric["endDate"] = DateTime.Today.ToString("MM/dd/yyyy");

                    datasourceDetails["metric"] = JsonConvert.SerializeObject(metric, Formatting.None);
                }
                else if (datasourceDetails["pad"].ToString() != "null" && datasourceDetails["pad"].ToString() != "")
                {
                    JObject pad = JObject.Parse(datasourceDetails["pad"].ToString());
                    pad["startDate"] = DateTime.Today.AddYears(-2).ToString("MM/dd/yyyy");
                    pad["endDate"] = DateTime.Today.ToString("MM/dd/yyyy");

                    datasourceDetails["pad"] = JsonConvert.SerializeObject(pad, Formatting.None);
                }
                ingestRequest.ModelName = ingestRequest.ModelName + "_" + ingestRequest.CorrelationId;
                trainingRequest.ModelName = trainingRequest.ModelName + "_" + ingestRequest.CorrelationId;
                trainingResponse.ModelName = trainingRequest.ModelName;
                ingestRequest.SourceDetails = JsonConvert.SerializeObject(datasourceDetails, Formatting.None);
            }

            if (!string.IsNullOrEmpty(trainingRequest.DataSource) && trainingRequest.DataSource.ToUpper() == CONSTANTS.CustomDbQuery.ToUpper())
            {
                if (isCLusteringService)
                {
                    ingestRequest.CorrelationId = clusteringAPIModel.CorrelationId;
                    ingestRequest.ClientId = clusteringAPIModel.ClientID;
                    ingestRequest.DeliveryconstructId = clusteringAPIModel.DCUID;
                }
                if (CustomDataSourceDetails.Count() > 0)
                {
                    foreach (var item in CustomDataSourceDetails)
                    {
                        if (item.Trim() != "{}")
                        {
                            CustomSourceItems += item;
                        }
                        else
                            CustomSourceItems = CONSTANTS.Null;
                    }
                }
                if (CustomSourceItems != CONSTANTS.Null && CustomSourceItems != string.Empty)
                {
                    _filepath = new Filepath();
                    _filepath.fileList = "null";
                    parentFile = new ParentFile();
                    parentFile.Type = "null";
                    parentFile.Name = "null";
                    if (trainingRequest.DataSource.ToUpper() == CONSTANTS.CustomDbQuery.ToUpper())
                    {
                        var DateColumn = Convert.ToString(collection["DateColumn"]);
                        var Query = CustomSourceItems;
                        QueryDTO QueryData = new QueryDTO();

                        if (!string.IsNullOrEmpty(Query))
                        {
                            QueryData.Type = "CustomDbQuery";
                            QueryData.Query = Query;
                            QueryData.DateColumn = DateColumn;
                        }
                        var Data = _encryptionDecryption.Encrypt(JsonConvert.SerializeObject(QueryData));

                        CustomQueryParamArgs CustomQueryData = new CustomQueryParamArgs
                        {
                            CorrelationId = ingestRequest.CorrelationId,
                            ClientUID = ingestRequest.ClientId,
                            E2EUID = CONSTANTS.Null,
                            DeliveryConstructUId = ingestRequest.DeliveryconstructId,
                            Parent = parentFile,
                            Flag = CONSTANTS.Null,
                            mapping = CONSTANTS.Null,
                            mapping_flag = CONSTANTS.False,
                            pad = CONSTANTS.Null,
                            metric = CONSTANTS.Null,
                            InstaMl = CONSTANTS.Null,
                            fileupload = _filepath,
                            Customdetails = CONSTANTS.Null,
                            CustomSource = Data
                        };
                        if (isCLusteringService)
                        {
                            clusteringAPIModel.ParamArgs = CustomQueryData.ToJson();
                        }
                        else
                        {
                            ingestRequest.SourceDetails = CustomQueryData.ToJson();
                        }

                    }
                }
            }

            if (trainingRequest.DataSource == "Custom" || (trainingRequest.DataSource != null && trainingRequest.DataSource.Contains("DataSet")))
            {
                if (isCLusteringService)
                {
                    ingestRequest.CorrelationId = clusteringAPIModel.CorrelationId;
                    ingestRequest.ClientId = clusteringAPIModel.ClientID;
                    ingestRequest.DeliveryconstructId = clusteringAPIModel.DCUID;
                }
                CustomSPAAIFileUpload fileUpload = new CustomSPAAIFileUpload
                {
                    CorrelationId = ingestRequest.CorrelationId,
                    ClientUID = ingestRequest.ClientId,
                    DeliveryConstructUId = ingestRequest.DeliveryconstructId,
                    Parent = parentDetail,
                    Flag = flag,//CONSTANTS.Null,
                    mapping = CONSTANTS.Null,
                    mapping_flag = CONSTANTS.False,//CONSTANTS.Null,
                    pad = CONSTANTS.Null,//string.Empty,
                    metric = CONSTANTS.Null,//string.Empty,
                    InstaMl = CONSTANTS.Null,// string.Empty,
                    fileupload = null,
                };
                AppIntegrationsCredentials appIntegrationsCredentials = new AppIntegrationsCredentials()
                {
                    grant_type = appSettings.Value.Grant_Type,
                    client_id = appSettings.Value.clientId,
                    client_secret = appSettings.Value.clientSecret,
                    resource = resourceId
                };

                AppIntegration appIntegrations = new AppIntegration()
                {
                    ApplicationID = usecaseDetails.ApplicationId,
                    Credentials = JsonConvert.SerializeObject(appIntegrationsCredentials)
                };
                if (trainingRequest.DataSource != "DataSet")
                    this.UpdateAppIntegration(appIntegrations);
                string url = CONSTANTS.Null;
                string bodyParams = CONSTANTS.Null;

                CustomSPAAIPayload customSPAAIPayload = null;
                if (!string.IsNullOrWhiteSpace(trainingRequest.DataSourceDetails.ToString()))
                {
                    var url1 = JsonConvert.DeserializeObject<JObject>(trainingRequest.DataSourceDetails)["Url"];
                    var bodyParams1 = JsonConvert.DeserializeObject<JObject>(trainingRequest.DataSourceDetails)["BodyParams"];

                    customSPAAIPayload = new CustomSPAAIPayload
                    {
                        UsecaseID = trainingRequest.UsecaseId,
                        AppId = trainingRequest.ApplicationId,
                        HttpMethod = CONSTANTS.POST,
                        AppUrl = url1,
                        InputParameters = JsonConvert.SerializeObject(bodyParams1),
                        DateColumn = "DateColumn"
                    };
                }
                else
                {
                    customSPAAIPayload = new CustomSPAAIPayload
                    {
                        UsecaseID = trainingRequest.UsecaseId,
                        AppId = trainingRequest.ApplicationId,
                        HttpMethod = CONSTANTS.POST,
                        AppUrl = CONSTANTS.Null,
                        InputParameters = CONSTANTS.Null,
                        DateColumn = "DateColumn"
                    };
                }
                parentDetail.Type = CONSTANTS.Null;
                parentDetail.Name = CONSTANTS.Null;
                fileUpload.Parent = parentDetail;
                _filepath = new Filepath();
                _filepath.fileList = "null";
                fileUpload.fileupload = _filepath;
                fileUpload.Customdetails = customSPAAIPayload;
                if (isCLusteringService)
                {
                    clusteringAPIModel.ParamArgs = fileUpload.ToJson();
                }
                else
                {
                    ingestRequest.SourceDetails = fileUpload.ToJson();
                }
            }
            //Forming the Paramargs for the training  or retraining END

            if (!string.IsNullOrEmpty(trainingRequest.DataSource) && trainingRequest.DataSource.ToUpper() == CONSTANTS.CustomDataApi.ToUpper())
            {
                if (isCLusteringService)
                {
                    ingestRequest.CorrelationId = clusteringAPIModel.CorrelationId;
                    ingestRequest.ClientId = clusteringAPIModel.ClientID;
                    ingestRequest.DeliveryconstructId = clusteringAPIModel.DCUID;
                }

                var fileParams = JsonConvert.DeserializeObject<CustomInputData>(trainingRequest.CustomDataSourceDetails);
                if (fileParams.Data != null)
                {
                    fileParams.Data.Type = "API";
                }

                if (fileParams.Data.Authentication.UseIngrainAzureCredentials)
                {
                    AzureDetails oAuthCredentials = new AzureDetails
                    {
                        grant_type = appSettings.Value.Grant_Type,
                        client_secret = appSettings.Value.clientSecret,
                        client_id = appSettings.Value.clientId,
                        resource = appSettings.Value.resourceId
                    };
                    string TokenUrl = appSettings.Value.token_Url;
                    string token = _customDataService.CustomUrlToken("Ingrain", oAuthCredentials, TokenUrl);
                    if (!String.IsNullOrEmpty(token))
                    {
                        fileParams.Data.Authentication.Token = token;
                    }
                    else
                    {
                        message.Message = CONSTANTS.IngrainTokenBlank;
                        throw new Exception(message.Message);
                    }
                }

                //Encrypting API related Information
                var Data = _encryptionDecryption.Encrypt(JsonConvert.SerializeObject(fileParams.Data));

                CustomSourceDTO fileUpload = new CustomSourceDTO
                {
                    CorrelationId = correlationId,
                    ClientUID = ingestRequest.ClientId,
                    E2EUID = CONSTANTS.Null,//E2EUID,
                    DeliveryConstructUId = ingestRequest.DeliveryconstructId,
                    Flag = CONSTANTS.Null,
                    mapping = CONSTANTS.Null,
                    mapping_flag = CONSTANTS.False,
                    pad = CONSTANTS.Null,
                    metric = CONSTANTS.Null,
                    InstaMl = CONSTANTS.Null,
                    fileupload = _filepath,
                    StartDate = CONSTANTS.Null,
                    EndDate = CONSTANTS.Null,
                    Customdetails = CONSTANTS.Null,
                };

                parentDetail.Type = CONSTANTS.Null;
                parentDetail.Name = CONSTANTS.Null;
                fileUpload.Parent = parentDetail;
                _filepath = new Filepath();
                _filepath.fileList = "null";
                fileUpload.fileupload = _filepath;
                fileUpload.CustomSource = Data;
                if (isCLusteringService)
                {
                    clusteringAPIModel.ParamArgs = fileUpload.ToJson();
                }
                else
                {
                    ingestRequest.SourceDetails = fileUpload.ToJson();
                }

                CustomDataSourceModel CustomDataSource = new CustomDataSourceModel
                {
                    _id = Guid.NewGuid().ToString(),
                    CorrelationId = correlationId,
                    CustomDataPullType = CONSTANTS.CustomDataApi,
                    CustomSourceDetails = JsonConvert.SerializeObject(fileParams),
                    CreatedByUser = trainingRequest.UserId
                };
                _customDataService.InsertCustomDataSource(CustomDataSource, CONSTANTS.AICustomDataSource);

            }

            if (string.IsNullOrEmpty(trainingRequest.DataSource))
            {
                if (string.IsNullOrEmpty(trainingRequest.ResponseCallBackUrl))
                {
                    throw new Exception("Please provide the ResponseCallBackUrl");
                }
                else
                {
                    CustomSPAAIPayload customSPAAIPayload = null;
                    SPAAIInputParams inputParams = null;
                    CustomSPAAIFileUpload fileUpload = new CustomSPAAIFileUpload
                    {
                        CorrelationId = ingestRequest.CorrelationId,
                        ClientUID = ingestRequest.ClientId,
                        DeliveryConstructUId = ingestRequest.DeliveryconstructId,
                        Parent = parentDetail,
                        Flag = flag,//CONSTANTS.Null,
                        mapping = CONSTANTS.Null,
                        mapping_flag = CONSTANTS.False,//CONSTANTS.Null,
                        pad = CONSTANTS.Null,//string.Empty,
                        metric = CONSTANTS.Null,//string.Empty,
                        InstaMl = CONSTANTS.Null,// string.Empty,
                        fileupload = null,
                    };
                    AppIntegrationsCredentials appIntegrationsCredentials = new AppIntegrationsCredentials()
                    {
                        grant_type = appSettings.Value.Grant_Type,
                        client_id = appSettings.Value.clientId,
                        client_secret = appSettings.Value.clientSecret,
                        resource = string.IsNullOrEmpty(resourceId) ? appSettings.Value.resourceId : resourceId
                    };

                    AppIntegration appIntegrations = new AppIntegration()
                    {
                        ApplicationID = usecaseDetails.ApplicationId,
                        Credentials = JsonConvert.SerializeObject(appIntegrationsCredentials)
                    };
                    this.UpdateAppIntegration(appIntegrations);
                    Uri apiUri = new Uri(appSettings.Value.myWizardAPIUrl);
                    string host = apiUri.GetLeftPart(UriPartial.Authority);
                    Uri apiUri1 = new Uri(usecaseDetails.SourceURL);
                    string urlPath = apiUri1.AbsolutePath;

                    var url = host + urlPath;//usecaseDetails.SourceURL;//"https://devtest-mywizardapi-si.accenture.com/bi/AmbulanceLaneSimilarStories";
                    //var bodyParams = JsonConvert.DeserializeObject<JObject>(trainingRequest.DataSourceDetails)["BodyParams"];
                    //var url = "https://devtest-mywizardapi-si.accenture.com/bi/AmbulanceLaneSimilarStories";
                    inputParams = new SPAAIInputParams
                    {

                        CorrelationId = correlationId,
                        StartDate = DateTime.Now.AddYears(-2).ToString(CONSTANTS.DateFormat),
                        EndDate = DateTime.Now.ToString(CONSTANTS.DateFormat),
                        TotalRecordCount = 0,
                        PageNumber = 1,
                        BatchSize = 5000
                    };
                    customSPAAIPayload = new CustomSPAAIPayload
                    {
                        UsecaseID = trainingRequest.UsecaseId,
                        AppId = trainingRequest.ApplicationId,
                        HttpMethod = CONSTANTS.POST,
                        AppUrl = url.ToString(),
                        InputParameters = inputParams.ToJson(),//BsonDocument.Parse(inputParams.ToJson()),
                        DateColumn = "DateColumn"//BsonDocument.Parse(trainingRequest.DataSourceDetails.BodyParams.ToString())//JObject.Parse(trainingRequest.DataSourceDetails.BodyParams.ToString()) //trainingRequest.DataSourceDetails.BodyParams.ToObject<object>()//JsonConvert.DeserializeObject<JObject>(fileUpload.Customdetails.InputParameters.ToString())//trainingRequest.DataSourceDetails.BodyParams
                    };

                    parentDetail.Type = CONSTANTS.Null;
                    parentDetail.Name = CONSTANTS.Null;
                    fileUpload.Parent = parentDetail;
                    _filepath = new Filepath();
                    _filepath.fileList = "null";
                    fileUpload.fileupload = _filepath;
                    fileUpload.Customdetails = customSPAAIPayload;
                    ingestRequest.SourceDetails = fileUpload.ToJson();
                    ingestRequest.ModelName = ingestRequest.ModelName + "_" + ingestRequest.CorrelationId;
                    trainingRequest.ModelName = trainingRequest.ModelName + "_" + ingestRequest.CorrelationId;
                    trainingResponse.ModelName = trainingRequest.ModelName;
                }
            }
            if (datasetUId == "undefined" || datasetUId == "null")
                ingestRequest.DataSetUId = datasetUId;

            //Python calls for the diff AI Services.
            if (isCLusteringService)
            {
                JObject payload = new JObject();
                this.ClusteringIngestData(clusteringAPIModel);
                payload["CorrelationId"] = clusteringAPIModel.CorrelationId;
                payload["UniId"] = clusteringAPIModel.UniId;
                payload["UserId"] = clusteringAPIModel.UserId;
                payload["pageInfo"] = clusteringAPIModel.PageInfo;
                payload["IsDataUpload"] = true;
                if (clusteringAPIModel.retrain)
                {
                    payload["IsDataUpload"] = false;
                }
                payload["Publish_Case"] = true;
                baseUrl = appSettings.Value.ClusteringPythonURL;
                apiPath = CONSTANTS.Clustering_ModelTraining;
                message = _clusteringAPI.RoutePOSTRequest(string.Empty, new Uri(baseUrl), apiPath, payload, clusteringAPIModel.CorrelationId, clusteringAPIModel.retrain, false);
                trainingResponse.CorrelationId = clusteringAPIModel.CorrelationId;
                trainingResponse.StatusMessage = message.IsSuccess ? "Success" : message.Message;
                trainingResponse.ModelStatus = message.IsSuccess ? trainingMessage : "Error";
                trainingResponse.ApplicationId = usecaseDetails.ApplicationId;
            }
            else if (isIntentEntity)
            {
                InsertAIServiceRequest(ingestRequest);
                CreateAICoreModel(ingestRequest.ClientId,
                                     ingestRequest.DeliveryconstructId,
                                     ingestRequest.ServiceId,
                                     ingestRequest.CorrelationId,
                                     ingestRequest.UniId,
                                     ingestRequest.ModelName,
                                     pyModelName,
                                     status,
                                    statusMessage,
                                     trainingRequest.UserId,
                                     trainingRequest.DataSource, ingestRequest.PageInfo, datasetUId, 0);
                JObject payload = new JObject();
                payload["client_id"] = trainingRequest.ClientId;
                payload["dc_id"] = trainingRequest.DeliveryConstructId;
                payload["correlation_id"] = correlationId;
                payload["ApplicationId"] = trainingRequest.ApplicationId;
                payload["DataSource"] = "Custom";
                payload["DataSourceDetails"] = JObject.Parse(appPayload.ToJson());
                IFormFileCollection fileCollection = null;
                string[] keys = new string[] { };
                message = RoutePOSTRequest(string.Empty, new Uri(baseUrl), apiPath,
                               fileCollection, payload, keys, service.IsReturnArray, correlationId);
                trainingResponse.CorrelationId = ingestRequest.CorrelationId;
                trainingResponse.StatusMessage = message.IsSuccess ? "Success" : message.Message;
                trainingResponse.ModelStatus = message.IsSuccess ? "Training Initiated" : "Error";
                trainingResponse.ApplicationId = usecaseDetails.ApplicationId;
            }
            else
            {
                if ((string.IsNullOrEmpty(trainingRequest.DataSource) && !string.IsNullOrEmpty(trainingRequest.ResponseCallBackUrl)) || trainingRequest.DataSource == "Custom" || trainingRequest.DataSource == "Entity" || trainingRequest.DataSource == "File" || trainingRequest.DataSource == "Phoenix" || trainingRequest.DataSource.Contains("DataSet") || (!string.IsNullOrEmpty(trainingRequest.DataSource) && (trainingRequest.DataSource.ToUpper() == CONSTANTS.CustomDbQuery.ToUpper() || (trainingRequest.DataSource.ToUpper() == CONSTANTS.CustomDataApi.ToUpper()))))
                {
                    if (!string.IsNullOrEmpty(trainingRequest.DataSource) && trainingRequest.DataSource.ToUpper() == CONSTANTS.CustomDbQuery.ToUpper())
                    {
                       CustomDataSourceModel CustomDataSource = new CustomDataSourceModel
                        {
                            _id = Guid.NewGuid().ToString(),
                            CorrelationId = correlationId,
                            CustomDataPullType = "CustomDbQuery",
                            CustomSourceDetails = Convert.ToString(CustomSourceItems),
                            CreatedByUser = trainingRequest.UserId
                        };
                        _customDataService.InsertCustomDataSource(CustomDataSource, CONSTANTS.AICustomDataSource);
                    }
                    ingestRequest.ResponsecallbackUrl = trainingRequest.ResponseCallBackUrl;
                    ingestRequest.Status = "N";
                    InsertAIServiceRequest(ingestRequest);
                    AICoreModels aICoreModels = new AICoreModels();
                    aICoreModels.ClientId = ingestRequest.ClientId;
                    aICoreModels.DeliveryConstructId = ingestRequest.DeliveryconstructId;
                    aICoreModels.ServiceId = ingestRequest.ServiceId;
                    aICoreModels.CorrelationId = ingestRequest.CorrelationId;
                    aICoreModels.UniId = ingestRequest.UniId;
                    aICoreModels.ModelName = ingestRequest.ModelName;
                    aICoreModels.PythonModelName = string.Empty;
                    aICoreModels.ModelStatus = status;
                    aICoreModels.StatusMessage = statusMessage;
                    aICoreModels.CreatedBy = ingestRequest.CreatedByUser;
                    aICoreModels.DataSource = trainingRequest.DataSource;
                    aICoreModels.UsecaseId = trainingRequest.UsecaseId;
                    aICoreModels.ApplicationId = trainingRequest.ApplicationId;
                    aICoreModels.ResponsecallbackUrl = ingestRequest.ResponsecallbackUrl;

                    if (usecaseDetails.ApplicationName == "SPA")
                    {
                        aICoreModels.SendNotification = "True"; // for SPAusecase
                        aICoreModels.IsNotificationSent = "False"; //for SPAusecase
                    }
                    if (datasetUId == "undefined" || datasetUId == "null")
                        aICoreModels.DataSetUId = datasetUId;
                    aICoreModels.MaxDataPull = ingestRequest.MaxDataPull;
                    aICoreModels.IsCarryOutRetraining = iscarryOutRetraining;
                    aICoreModels.IsOnline = isOnline;
                    aICoreModels.IsOffline = isOffline;
                    aICoreModels.Retraining = aIModelFrequency;
                    aICoreModels.Training = aIModelFrequency;
                    aICoreModels.Prediction = aIModelFrequency;
                    if (iscarryOutRetraining && retraining != null && ((JContainer)retraining).HasValues)
                    {
                        aICoreModels.Retraining = new AIModelFrequency();
                        this.AssignFrequency(aICoreModels, aICoreModels.Retraining, retraining, CONSTANTS.Retraining);

                    }
                    if (aICoreModels.IsOffline)
                    {
                        if (training != null && ((JContainer)training).HasValues)
                        {
                            aICoreModels.Training = new AIModelFrequency();
                            this.AssignFrequency(aICoreModels, aICoreModels.Training, training, CONSTANTS.Training);
                        }
                        if (prediction != null && ((JContainer)prediction).HasValues)
                        {
                            aICoreModels.Prediction = new AIModelFrequency();
                            this.AssignFrequency(aICoreModels, aICoreModels.Prediction, prediction, CONSTANTS.Prediction);
                        }
                    }

                    CreateAICoreModel(aICoreModels, ingestRequest.PageInfo);
                                      
                }
                trainingResponse.CorrelationId = ingestRequest.CorrelationId;
                trainingResponse.StatusMessage = "Success";
                trainingResponse.ModelStatus = "Training Initiated";
                trainingResponse.ApplicationId = usecaseDetails.ApplicationId;

                LOGGING.LogManager.Logger.LogProcessInfo(typeof(CustomConfigService), nameof(TrainAIServiceModel), CONSTANTS.END, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), trainingRequest.ApplicationId, "", trainingRequest.ClientId, trainingRequest.DeliveryConstructId);
                return trainingResponse;
            }

            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CustomConfigService), nameof(TrainAIServiceModel), CONSTANTS.END, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), trainingRequest.ApplicationId, "", trainingRequest.ClientId, trainingRequest.DeliveryConstructId);
            return trainingResponse;
        }

        #region AI Training Methods

       public void CheckMandatoryFields(Service service, USECASE.UsecaseDetails usecaseDetails, AIGENERICSERVICE.TrainingRequest trainingRequest)
        {
            //Check for serviceId
            if (service != null && string.IsNullOrEmpty(service.ServiceId))
            {
                throw new Exception("Service Id is not correct");
            }
            //Check usecaseId
            if (string.IsNullOrEmpty(usecaseDetails.UsecaseId))
            {
                throw new Exception("UsecaseId is not correct");
            }
            //Check ApplicationId
            if (usecaseDetails.ApplicationId != trainingRequest.ApplicationId)
            {
                throw new Exception("ApplicationId doesn't match the usecaseId");
            }
            //Check ModelName
            if (string.IsNullOrEmpty(trainingRequest.ModelName))
            {
                throw new Exception("Please provide ModelName");
            }
        }

        #region CreateAICoreModel
        public bool CreateAICoreModel(AICoreModels aiCoreModels, string pageInfo)
        {
          
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CustomConfigService), nameof(CreateAICoreModel), CONSTANTS.START,
                string.IsNullOrEmpty(aiCoreModels.CorrelationId) ? default(Guid) : new Guid(aiCoreModels.CorrelationId), aiCoreModels.ApplicationId, string.Empty, aiCoreModels.ClientId, aiCoreModels.DeliveryConstructId);
            aiCoreModels.PredictionURL = appSettings.Value.AICorePredictionURL + "?=" + aiCoreModels.CorrelationId;
            aiCoreModels.CreatedOn = DateTime.UtcNow.ToString(CONSTANTS.DateHoursFormat);
            aiCoreModels.ModifiedOn = DateTime.UtcNow.ToString(CONSTANTS.DateHoursFormat);
                                                                                          
            aiCoreModels.ModifiedBy = aiCoreModels.CreatedBy;
            var collection = _database.GetCollection<BsonDocument>("AICoreModels");
            var filter = Builders<BsonDocument>.Filter.Eq("CorrelationId", aiCoreModels.CorrelationId) & Builders<BsonDocument>.Filter.Eq("ClientId", aiCoreModels.ClientId) & Builders<BsonDocument>.Filter.Eq("DeliveryConstructId", aiCoreModels.DeliveryConstructId);
            var projection = Builders<BsonDocument>.Projection.Exclude("_id");
            var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();

            if (result.Count > 0)
            {
                AICoreModels temp = JsonConvert.DeserializeObject<AICoreModels>(result[0].ToJson());
                aiCoreModels.CreatedOn = temp.CreatedOn;
                if (pageInfo == "Retrain")
                {
                    var update = Builders<BsonDocument>.Update.Set("ModifiedBy", aiCoreModels.CreatedBy).Set("ModifiedOn", aiCoreModels.ModifiedOn)
                                       .Set("ModelStatus", aiCoreModels.ModelStatus).Set("StatusMessage", aiCoreModels.StatusMessage).Set("DataSource", aiCoreModels.DataSource);
                    collection.UpdateOne(filter, update);
                }
                else
                {
                    var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(aiCoreModels);
                    var insertDocument = BsonSerializer.Deserialize<BsonDocument>(jsonData);
                    collection.DeleteOne(filter);
                    collection.InsertOneAsync(insertDocument);
                }
                return true;
            }
            else
            {
                var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(aiCoreModels);
                var insertDocument = BsonSerializer.Deserialize<BsonDocument>(jsonData);
                collection.InsertOneAsync(insertDocument);
                return true;
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CustomConfigService), nameof(CreateAICoreModel), CONSTANTS.START,
                string.IsNullOrEmpty(aiCoreModels.CorrelationId) ? default(Guid) : new Guid(aiCoreModels.CorrelationId), aiCoreModels.ApplicationId, string.Empty, aiCoreModels.ClientId, aiCoreModels.DeliveryConstructId);
        }

        public bool CreateAICoreModel(string clientid, string deliveryconstructid, string serviceid, string corrid, string uniId, string modelName, string pythonModelName, string modelStatus, string statusMessage, string userid, string dataSource, string pageInfo, string datasetUId, int maxDataPull)
        {
            if (appSettings.Value.isForAllData)
            {
                if (!string.IsNullOrEmpty(userid))
                {
                    userid = _encryptionDecryption.Encrypt(Convert.ToString(userid));
                }
            }
            AICoreModels aiCoreModels = new AICoreModels();
            aiCoreModels.ClientId = clientid;
            aiCoreModels.DeliveryConstructId = deliveryconstructid;
            aiCoreModels.ServiceId = serviceid;
            aiCoreModels.CorrelationId = corrid;
            aiCoreModels.UniId = uniId;
            aiCoreModels.ModelName = modelName;
            aiCoreModels.ModelStatus = modelStatus;
            aiCoreModels.DataSource = dataSource;
            aiCoreModels.StatusMessage = statusMessage;
            aiCoreModels.PythonModelName = pythonModelName;
            aiCoreModels.PredictionURL = appSettings.Value.AICorePredictionURL + "?=" + aiCoreModels.CorrelationId;
            aiCoreModels.CreatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");//DateTime.Now.ToString();
            aiCoreModels.ModifiedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");//DateTime.Now.ToString();
            aiCoreModels.CreatedBy = userid;
            aiCoreModels.ModifiedBy = userid;
            aiCoreModels.DataSetUId = datasetUId;
            aiCoreModels.MaxDataPull = maxDataPull;
            var collection = _database.GetCollection<BsonDocument>("AICoreModels");
            var filter = Builders<BsonDocument>.Filter.Eq("CorrelationId", corrid) & Builders<BsonDocument>.Filter.Eq("ClientId", clientid) & Builders<BsonDocument>.Filter.Eq("DeliveryConstructId", deliveryconstructid);
            var projection = Builders<BsonDocument>.Projection.Exclude("_id");
            var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();

            if (result.Count > 0)
            {
                AICoreModels temp = JsonConvert.DeserializeObject<AICoreModels>(result[0].ToJson());
                aiCoreModels.CreatedOn = temp.CreatedOn;
                aiCoreModels.IsCarryOutRetraining = temp.IsCarryOutRetraining;
                aiCoreModels.IsOnline = temp.IsOnline;
                aiCoreModels.IsOffline = temp.IsOffline;
                aiCoreModels.Training = temp.Training;
                aiCoreModels.Prediction = temp.Prediction;
                aiCoreModels.Retraining = temp.Retraining;
                aiCoreModels.RetrainingFrequencyInDays = temp.RetrainingFrequencyInDays;
                aiCoreModels.TrainingFrequencyInDays = temp.TrainingFrequencyInDays;
                aiCoreModels.PredictionFrequencyInDays = temp.PredictionFrequencyInDays;

                if (pageInfo == "Retrain")
                {
                    var update = Builders<BsonDocument>.Update.Set("ModifiedBy", userid).Set("ModifiedOn", aiCoreModels.ModifiedOn)
                   .Set("ModelStatus", aiCoreModels.ModelStatus).Set("StatusMessage", aiCoreModels.StatusMessage).Set("DataSource", dataSource);
                    collection.UpdateOne(filter, update);
                }
                else
                {
                    var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(aiCoreModels);
                    var insertDocument = BsonSerializer.Deserialize<BsonDocument>(jsonData);
                    collection.DeleteOne(filter);
                    collection.InsertOneAsync(insertDocument);
                }
                return true;
            }
            else
            {
                var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(aiCoreModels);
                var insertDocument = BsonSerializer.Deserialize<BsonDocument>(jsonData);
                collection.InsertOneAsync(insertDocument);
                return true;
            }
        }

        public bool CreateAICoreModel(string clientid, string deliveryconstructid, string serviceid, string corrid, string modelName, string pythonModelName, string modelStatus, string statusMessage, string userid, string usecaseId, string applicationId, string uniId, string trainedBy, string pageInfo, string dataSource)
        {
            AICoreModels aiCoreModels = new AICoreModels();
            aiCoreModels.ClientId = clientid;
            aiCoreModels.DeliveryConstructId = deliveryconstructid;
            aiCoreModels.ServiceId = serviceid;
            aiCoreModels.UniId = uniId;
            aiCoreModels.LastTrainedBy = trainedBy;
            aiCoreModels.CorrelationId = corrid;
            aiCoreModels.ModelName = modelName;
            aiCoreModels.UsecaseId = usecaseId;
            aiCoreModels.ApplicationId = applicationId;
            aiCoreModels.ModelStatus = modelStatus;
            aiCoreModels.DataSource = dataSource;
            aiCoreModels.StatusMessage = statusMessage;
            aiCoreModels.PythonModelName = pythonModelName;
            aiCoreModels.PredictionURL = appSettings.Value.AICorePredictionURL + "?=" + aiCoreModels.CorrelationId;
            aiCoreModels.CreatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");//DateTime.Now.ToString();
            aiCoreModels.ModifiedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");//DateTime.Now.ToString();
            aiCoreModels.CreatedBy = userid;
            aiCoreModels.ModifiedBy = userid;
            var collection = _database.GetCollection<BsonDocument>("AICoreModels");
            var filter = Builders<BsonDocument>.Filter.Eq("CorrelationId", corrid) & Builders<BsonDocument>.Filter.Eq("ClientId", clientid) & Builders<BsonDocument>.Filter.Eq("DeliveryConstructId", deliveryconstructid);
            var projection = Builders<BsonDocument>.Projection.Exclude("_id");
            var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();

            if (result.Count > 0)
            {
                AICoreModels temp = JsonConvert.DeserializeObject<AICoreModels>(result[0].ToJson());
                aiCoreModels.CreatedOn = temp.CreatedOn;
                if (pageInfo == "Retrain")
                {
                    var update = Builders<BsonDocument>.Update.Set("ModifiedBy", userid).Set("ModifiedOn", aiCoreModels.ModifiedOn)
              .Set("ModelStatus", aiCoreModels.ModelStatus).Set("StatusMessage", aiCoreModels.StatusMessage);
                    collection.UpdateOne(filter, update);
                }
                else
                {
                    var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(aiCoreModels);
                    var insertDocument = BsonSerializer.Deserialize<BsonDocument>(jsonData);
                    collection.DeleteOne(filter);
                    collection.InsertOneAsync(insertDocument);
                }
                return true;
            }
            else
            {
                var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(aiCoreModels);
                var insertDocument = BsonSerializer.Deserialize<BsonDocument>(jsonData);
                collection.InsertOneAsync(insertDocument);
                return true;
            }
        }

        #endregion CreateAICoreModel

        #region RoutePOSTRequest
        public MethodReturn<object> RoutePOSTRequest(string token, Uri baseUrl, string apiPath, IFormFileCollection fileCollection,
          JObject requestPayload, string[] fileKeys, bool isReturnArray, string correlationid)
        {
            MethodReturn<object> returnValue = new MethodReturn<object>();

            dynamic PamToken;
            if (appSettings.Value.Environment == CONSTANTS.PAMEnvironment)
            {
                PamToken = _iPhoenixTokenService.GeneratePAMToken();
                if (PamToken != null && PamToken["token"] != string.Empty)
                {
                    token = Convert.ToString(PamToken["token"]);
                }
            }
            else
            {
                token = PythonAIServiceToken();
            }

            string tempPath = null;
            try
            {
                var folderPath = Guid.NewGuid().ToString();
                List<string> lstFilePath = new List<string>();
                if (fileCollection != null)
                {
                    foreach (var file in fileCollection)
                    {
                        var fileName = file.FileName;
                        tempPath = appSettings.Value.AICoreFilespath.ToString();
                        var filePath = tempPath + correlationid + "/" + folderPath + "/" + fileName;
                        if (!Directory.Exists(tempPath + correlationid + "/" + folderPath))
                            Directory.CreateDirectory(tempPath + correlationid + "/" + folderPath);
                        //using (var stream = new FileStream(filePath, FileMode.Create))
                        //{
                        //    file.CopyTo(stream);
                        //}
                        _encryptionDecryption.EncryptFile(file, filePath);
                        if (appSettings.Value.IsAESKeyVault && appSettings.Value.Environment == CONSTANTS.PADEnvironment && appSettings.Value.EncryptUploadedFiles)
                        {
                            filePath = Path.Combine(filePath + ".enc");
                        }
                        lstFilePath.Add(filePath);
                    }
                }
                StringContent content = new StringContent(requestPayload.ToString(), Encoding.UTF8, "application/json");
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(CustomConfigService), nameof(RoutePOSTRequest), requestPayload.ToString(), string.IsNullOrEmpty(correlationid) ? default(Guid) : new Guid(correlationid), string.Empty, string.Empty, string.Empty, string.Empty);

                Task.Run(() =>
                {
                    try
                    {
                        HttpResponseMessage message = webHelper.InvokePOSTRequestWithFiles(token, baseUrl, apiPath, lstFilePath, fileKeys, content);
                        if (message != null && message.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            TrainingResponse ReturnValue = JsonConvert.DeserializeObject<TrainingResponse>(message.Content.ReadAsStringAsync().Result);
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CustomConfigService), nameof(RoutePOSTRequest), message.Content.ReadAsStringAsync().Result, string.IsNullOrEmpty(correlationid) ? default(Guid) : new Guid(correlationid), string.Empty, string.Empty, string.Empty, string.Empty);
                            AICoreModels model = GetAICoreModelPath(correlationid);
                            if (ReturnValue.is_success == "true")
                            {
                                string pyModelname = model.PythonModelName;
                                string modelStatus = model.ModelStatus;
                                CreateAICoreModel(model.ClientId, model.DeliveryConstructId, model.ServiceId, model.CorrelationId, model.UniId, model.ModelName, pyModelname, modelStatus, ReturnValue.message, model.CreatedBy, model.DataSource, null, model.DataSetUId, model.MaxDataPull);
                            }
                            else
                            {
                                if (model.PythonModelName != null && model.PythonModelName != "")
                                    CreateAICoreModel(model.ClientId, model.DeliveryConstructId, model.ServiceId, model.CorrelationId, model.UniId, model.ModelName, model.PythonModelName, "ReTrain Failed", "Python error in retraining-" + ReturnValue.message + ";Evaluation will be done with previously trained data", model.CreatedBy, model.DataSource, null, model.DataSetUId, model.MaxDataPull);
                                else
                                    CreateAICoreModel(model.ClientId, model.DeliveryConstructId, model.ServiceId, model.CorrelationId, model.UniId, model.ModelName, model.PythonModelName, "Error", "Python error in training-" + ReturnValue.message, model.CreatedBy, model.DataSource, null, model.DataSetUId, model.MaxDataPull);
                            }


                        }
                        else
                        {
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CustomConfigService), nameof(RoutePOSTRequest), message.StatusCode.ToString() + message.ReasonPhrase + "_" + message.Content.ReadAsStringAsync().Result, string.IsNullOrEmpty(correlationid) ? default(Guid) : new Guid(correlationid), string.Empty, string.Empty, string.Empty, string.Empty);

                            AICoreModels model = GetAICoreModelPath(correlationid);
                            if (model.PythonModelName != null && model.PythonModelName != "")
                                CreateAICoreModel(model.ClientId, model.DeliveryConstructId, model.ServiceId, model.CorrelationId, model.UniId, model.ModelName, model.PythonModelName, "ReTrain Failed", "Python error in retraining-" + message.ReasonPhrase + "_" + message.Content.ReadAsStringAsync().Result + ";Evaluation will be done with previously trained data", model.CreatedBy, model.DataSource, null, model.DataSetUId, model.MaxDataPull);
                            else
                                CreateAICoreModel(model.ClientId, model.DeliveryConstructId, model.ServiceId, model.CorrelationId, model.UniId, model.ModelName, model.PythonModelName, "Error", "Python error in training-" + message.ReasonPhrase + "_" + message.Content.ReadAsStringAsync().Result, model.CreatedBy, model.DataSource, null, model.DataSetUId, model.MaxDataPull);

                        }
                    }
                    catch (Exception ex)
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(CustomConfigService), nameof(RoutePOSTRequest), ex.Message, string.Empty, string.Empty, string.Empty, string.Empty);
                        AICoreModels model = GetAICoreModelPath(correlationid);
                        if (model.PythonModelName != null && model.PythonModelName != "")
                            CreateAICoreModel(model.ClientId, model.DeliveryConstructId, model.ServiceId, model.CorrelationId, model.UniId, model.ModelName, model.PythonModelName, "ReTrain Failed", "Python API call error in retraining-" + ex.Message + ";Evaluation will be done with previously trained data", model.CreatedBy, model.DataSource, null, model.DataSetUId, model.MaxDataPull);
                        else
                            CreateAICoreModel(model.ClientId, model.DeliveryConstructId, model.ServiceId, model.CorrelationId, model.UniId, model.ModelName, model.PythonModelName, "Error", "Python API call error in training-" + ex.Message, model.CreatedBy, model.DataSource, null, model.DataSetUId, model.MaxDataPull);
                    }



                });
                returnValue.Message = "Training initiated successfully";
                returnValue.IsSuccess = true;

            }
            catch (Exception ex)
            {
                returnValue.IsSuccess = false;
                returnValue.Message = ex.Message;
            }
            return returnValue;
        }

        public MethodReturn<object> RoutePOSTRequest(string token, Uri baseUrl, string apiPath, JObject requestPayload, bool isReturnArray)
        {
            MethodReturn<object> returnValue = new MethodReturn<object>();

            dynamic PamToken;
            if (appSettings.Value.Environment == CONSTANTS.PAMEnvironment)
            {
                PamToken = _iPhoenixTokenService.GeneratePAMToken();
                if (PamToken != null && PamToken["token"] != string.Empty)
                {
                    token = Convert.ToString(PamToken["token"]);
                }
            }
            else
            {
                token = PythonAIServiceToken();
            }

            try
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(CustomConfigService), nameof(RoutePOSTRequest), "requestPayload : " + Convert.ToString(requestPayload), string.Empty, string.Empty, string.Empty, string.Empty);
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(CustomConfigService), nameof(RoutePOSTRequest), "token : " + token + " apiPath : " + apiPath + " baseUrl : " + baseUrl, string.Empty, string.Empty, string.Empty, string.Empty);
                StringContent content = new StringContent(requestPayload.ToString(), Encoding.UTF8, "application/json");
                HttpResponseMessage message = webHelper.InvokePOSTRequest(token, baseUrl.ToString(), apiPath, content);
                if (message != null && message.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    if (isReturnArray)
                        returnValue.ReturnValue = JsonConvert.DeserializeObject<List<JObject>>(message.Content.ReadAsStringAsync().Result);
                    else
                        returnValue.ReturnValue = JsonConvert.DeserializeObject<JObject>(message.Content.ReadAsStringAsync().Result);
                    returnValue.IsSuccess = true;
                }
                else
                {
                    returnValue.IsSuccess = false;
                    returnValue.Message = message.ReasonPhrase + "_" + message.Content.ReadAsStringAsync().Result;
                }
            }
            catch (Exception ex)
            {
                returnValue.IsSuccess = false;
                returnValue.Message = ex.Message;

            }
            return returnValue;
        }

        #endregion RoutePOSTRequest
        public string InsertAIServiceRequest(AIServiceRequestStatus aIServiceRequestStatus)
        {
            if (appSettings.Value.isForAllData)
            {
                if (!string.IsNullOrEmpty(Convert.ToString(aIServiceRequestStatus.CreatedByUser)))
                    aIServiceRequestStatus.CreatedByUser = _encryptionDecryption.Encrypt(Convert.ToString(aIServiceRequestStatus.CreatedByUser));
                if (!string.IsNullOrEmpty(Convert.ToString(aIServiceRequestStatus.ModifiedByUser)))
                    aIServiceRequestStatus.ModifiedByUser = _encryptionDecryption.Encrypt(Convert.ToString(aIServiceRequestStatus.ModifiedByUser));
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CustomConfigService), nameof(InsertAIServiceRequest), CONSTANTS.START
                , string.IsNullOrEmpty(aIServiceRequestStatus.CorrelationId) ? default(Guid) : new Guid(aIServiceRequestStatus.CorrelationId), aIServiceRequestStatus.ApplicationId, string.Empty, aIServiceRequestStatus.ClientId, aIServiceRequestStatus.DeliveryconstructId);
            var collection = _database.GetCollection<BsonDocument>("AIServiceRequestStatus");
            var filter = Builders<BsonDocument>.Filter.Eq("UniId", aIServiceRequestStatus.UniId);
            var result = collection.Find(filter).ToList();
            var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(aIServiceRequestStatus);
            var insertDocument = BsonSerializer.Deserialize<BsonDocument>(jsonData);
            if (result.Count > 0)
            {
                collection.DeleteOne(filter);
            }
            Thread.Sleep(1000);
            collection.InsertOneAsync(insertDocument);
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CustomConfigService), nameof(InsertAIServiceRequest), CONSTANTS.END, aIServiceRequestStatus.ApplicationId, string.Empty, aIServiceRequestStatus.ClientId, aIServiceRequestStatus.DeliveryconstructId);
            return "Success";

        }

        public void InsertAIServiceRequestStatus(AIServiceRequestStatus ingrainRequest)
        {
            if (appSettings.Value.isForAllData)
            {
                if (!string.IsNullOrEmpty(Convert.ToString(ingrainRequest.CreatedByUser)))
                    ingrainRequest.CreatedByUser = _encryptionDecryption.Encrypt(Convert.ToString(ingrainRequest.CreatedByUser));
                if (!string.IsNullOrEmpty(Convert.ToString(ingrainRequest.ModifiedByUser)))
                    ingrainRequest.ModifiedByUser = _encryptionDecryption.Encrypt(Convert.ToString(ingrainRequest.ModifiedByUser));
            }
            var collection = _database.GetCollection<BsonDocument>("AIServiceRequestStatus");
            var filter = Builders<BsonDocument>.Filter.Eq("CorrelationId", ingrainRequest.CorrelationId) & Builders<BsonDocument>.Filter.Eq("ClientId", ingrainRequest.ClientId) & Builders<BsonDocument>.Filter.Eq("DeliveryconstructId", ingrainRequest.DeliveryconstructId) & Builders<BsonDocument>.Filter.Eq("ServiceId", ingrainRequest.ServiceId);
            var projection = Builders<BsonDocument>.Projection.Exclude("_id");
            var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                foreach (var item in result)
                {
                    if (item["SelectedColumnNames"] != BsonNull.Value)
                    {
                        var data = item["SelectedColumnNames"].ToJson();
                        var dataset = JsonConvert.DeserializeObject<List<object>>(data);
                        ingrainRequest.SelectedColumnNames = dataset;
                    }
                }
                var requestQueue = JsonConvert.SerializeObject(ingrainRequest);
                var insertRequestQueue = BsonSerializer.Deserialize<BsonDocument>(requestQueue);
                collection.InsertOne(insertRequestQueue);
            }
            else
            {
                var requestQueue = JsonConvert.SerializeObject(ingrainRequest);
                var insertRequestQueue = BsonSerializer.Deserialize<BsonDocument>(requestQueue);
                collection.InsertOne(insertRequestQueue);
            }

        }

        public void AssignFrequency(AICoreModels aICoreModels, AIModelFrequency frequency, dynamic data, string feature)
        {
            foreach (var z in (JContainer)data)
            {
                string frequencyName = ((Newtonsoft.Json.Linq.JProperty)z).Name;
                frequency.RetryCount = data[CONSTANTS.RetryCount] != null ? Convert.ToInt32(data[CONSTANTS.RetryCount]) : 0;
                if (Convert.ToInt32(((Newtonsoft.Json.Linq.JProperty)z).Value) != 0)
                {
                    switch (frequencyName)
                    {
                        case CONSTANTS.Hourly:
                            frequency.Hourly = data[frequencyName];
                            aICoreModels.TrainingFrequencyInDays = feature.Equals(CONSTANTS.Training) ? frequency.Hourly * 60 : aICoreModels.TrainingFrequencyInDays;
                            aICoreModels.PredictionFrequencyInDays = feature.Equals(CONSTANTS.Prediction) ? frequency.Hourly * 60 : aICoreModels.PredictionFrequencyInDays;
                            break;

                        case CONSTANTS.Daily:
                            frequency.Daily = data[frequencyName];
                            aICoreModels.RetrainingFrequencyInDays = feature.Equals(CONSTANTS.Retraining) ? frequency.Daily : aICoreModels.RetrainingFrequencyInDays;
                            aICoreModels.TrainingFrequencyInDays = feature.Equals(CONSTANTS.Training) ? frequency.Daily : aICoreModels.TrainingFrequencyInDays;
                            aICoreModels.PredictionFrequencyInDays = feature.Equals(CONSTANTS.Prediction) ? frequency.Daily : aICoreModels.PredictionFrequencyInDays;
                            break;

                        case CONSTANTS.Weekly:
                            frequency.Weekly = data[frequencyName];
                            aICoreModels.RetrainingFrequencyInDays = feature.Equals(CONSTANTS.Retraining) ? (frequency.Weekly * 7) : aICoreModels.RetrainingFrequencyInDays;
                            aICoreModels.TrainingFrequencyInDays = feature.Equals(CONSTANTS.Training) ? (frequency.Weekly * 7) : aICoreModels.TrainingFrequencyInDays;
                            aICoreModels.PredictionFrequencyInDays = feature.Equals(CONSTANTS.Prediction) ? (frequency.Weekly * 7) : aICoreModels.PredictionFrequencyInDays;
                            break;

                        case CONSTANTS.Monthly:
                            frequency.Monthly = data[frequencyName];
                            aICoreModels.RetrainingFrequencyInDays = feature.Equals(CONSTANTS.Retraining) ? (frequency.Monthly * 30) : aICoreModels.RetrainingFrequencyInDays;
                            aICoreModels.TrainingFrequencyInDays = feature.Equals(CONSTANTS.Training) ? (frequency.Monthly * 30) : aICoreModels.TrainingFrequencyInDays;
                            aICoreModels.PredictionFrequencyInDays = feature.Equals(CONSTANTS.Prediction) ? (frequency.Monthly * 30) : aICoreModels.PredictionFrequencyInDays;
                            break;

                        case CONSTANTS.Fortnightly:
                            frequency.Fortnightly = data[frequencyName];
                            aICoreModels.RetrainingFrequencyInDays = feature.Equals(CONSTANTS.Retraining) ? (frequency.Fortnightly * 14) : aICoreModels.RetrainingFrequencyInDays;
                            aICoreModels.TrainingFrequencyInDays = feature.Equals(CONSTANTS.Training) ? (frequency.Fortnightly * 14) : aICoreModels.TrainingFrequencyInDays;
                            aICoreModels.PredictionFrequencyInDays = feature.Equals(CONSTANTS.Prediction) ? (frequency.Fortnightly * 14) : aICoreModels.PredictionFrequencyInDays;
                            break;
                    }
                }
            }
        }

        public void AssignFrequencyClustering(ClusteringAPIModel clusteringAPIModel, ClustModelFrequency frequency, dynamic data, string feature)
        {
            foreach (var z in (JContainer)data)
            {
                string frequencyName = ((Newtonsoft.Json.Linq.JProperty)z).Name;
                //string frequencyName = ((JProperty)((JContainer)data).First).Name;
                frequency.RetryCount = data[CONSTANTS.RetryCount] != null ? Convert.ToInt32(data[CONSTANTS.RetryCount]) : 0;
                if (Convert.ToInt32(((Newtonsoft.Json.Linq.JProperty)z).Value) != 0)
                {
                    switch (frequencyName)
                    {
                        case CONSTANTS.Hourly:
                            frequency.Hourly = data[frequencyName];
                            clusteringAPIModel.TrainingFrequencyInDays = feature.Equals(CONSTANTS.Training) ? frequency.Hourly * 60 : clusteringAPIModel.TrainingFrequencyInDays;
                            clusteringAPIModel.PredictionFrequencyInDays = feature.Equals(CONSTANTS.Prediction) ? frequency.Hourly * 60 : clusteringAPIModel.PredictionFrequencyInDays;
                            break;

                        case CONSTANTS.Daily:
                            frequency.Daily = data[frequencyName];
                            clusteringAPIModel.RetrainingFrequencyInDays = feature.Equals(CONSTANTS.Retraining) ? frequency.Daily : clusteringAPIModel.RetrainingFrequencyInDays;
                            clusteringAPIModel.TrainingFrequencyInDays = feature.Equals(CONSTANTS.Training) ? frequency.Daily : clusteringAPIModel.TrainingFrequencyInDays;
                            clusteringAPIModel.PredictionFrequencyInDays = feature.Equals(CONSTANTS.Prediction) ? frequency.Daily : clusteringAPIModel.PredictionFrequencyInDays;
                            break;

                        case CONSTANTS.Weekly:
                            frequency.Weekly = data[frequencyName];
                            clusteringAPIModel.RetrainingFrequencyInDays = feature.Equals(CONSTANTS.Retraining) ? (frequency.Weekly * 7) : clusteringAPIModel.RetrainingFrequencyInDays;
                            clusteringAPIModel.TrainingFrequencyInDays = feature.Equals(CONSTANTS.Training) ? (frequency.Weekly * 7) : clusteringAPIModel.TrainingFrequencyInDays;
                            clusteringAPIModel.PredictionFrequencyInDays = feature.Equals(CONSTANTS.Prediction) ? (frequency.Weekly * 7) : clusteringAPIModel.PredictionFrequencyInDays;
                            break;

                        case CONSTANTS.Monthly:
                            frequency.Monthly = data[frequencyName];
                            clusteringAPIModel.RetrainingFrequencyInDays = feature.Equals(CONSTANTS.Retraining) ? (frequency.Monthly * 30) : clusteringAPIModel.RetrainingFrequencyInDays;
                            clusteringAPIModel.TrainingFrequencyInDays = feature.Equals(CONSTANTS.Training) ? (frequency.Monthly * 30) : clusteringAPIModel.TrainingFrequencyInDays;
                            clusteringAPIModel.PredictionFrequencyInDays = feature.Equals(CONSTANTS.Prediction) ? (frequency.Monthly * 30) : clusteringAPIModel.PredictionFrequencyInDays;
                            break;

                        case CONSTANTS.Fortnightly:
                            frequency.Fortnightly = data[frequencyName];
                            clusteringAPIModel.RetrainingFrequencyInDays = feature.Equals(CONSTANTS.Retraining) ? (frequency.Fortnightly * 14) : clusteringAPIModel.RetrainingFrequencyInDays;
                            clusteringAPIModel.TrainingFrequencyInDays = feature.Equals(CONSTANTS.Training) ? (frequency.Fortnightly * 14) : clusteringAPIModel.TrainingFrequencyInDays;
                            clusteringAPIModel.PredictionFrequencyInDays = feature.Equals(CONSTANTS.Prediction) ? (frequency.Fortnightly * 14) : clusteringAPIModel.PredictionFrequencyInDays;
                            break;
                    }
                }
            }
        }
        public bool ClusteringIngestData(ClusteringAPIModel clusteringAPI)
        {
            clusteringAPI.CreatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            clusteringAPI.ModifiedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            clusteringAPI.CreatedBy = clusteringAPI.UserId;
            clusteringAPI.ModifiedBy = clusteringAPI.UserId;
            clusteringAPI.DBEncryptionRequired = appSettings.Value.DBEncryption;
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.Clustering_IngestData);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, clusteringAPI.CorrelationId) & Builders<BsonDocument>.Filter.Eq(CONSTANTS.ClientID, clusteringAPI.ClientID) & Builders<BsonDocument>.Filter.Eq(CONSTANTS.DCUID, clusteringAPI.DCUID);
            var projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
            var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                if (clusteringAPI.retrain)
                {

                    clusteringAPI._id = Guid.NewGuid().ToString();
                    clusteringAPI.DataSource = result[0]["DataSource"].ToString();
                    if (result[0]["MaxDataPull"] != null)
                        clusteringAPI.MaxDataPull = Convert.ToInt32(result[0]["MaxDataPull"]);
                    var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(clusteringAPI);
                    var insertDocument = BsonSerializer.Deserialize<BsonDocument>(jsonData);
                    collection.DeleteOne(filter);
                    Thread.Sleep(1000);
                    collection.InsertOne(insertDocument);
                }
                else
                {
                    var aIServiceRequestStatus = JsonConvert.DeserializeObject<ClusteringAPIModel>(result[0].ToJson());
                    aIServiceRequestStatus.Columnsselectedbyuser = clusteringAPI.Columnsselectedbyuser;
                    aIServiceRequestStatus.UniId = clusteringAPI.UniId;
                    aIServiceRequestStatus.PageInfo = clusteringAPI.PageInfo;
                    aIServiceRequestStatus.SelectedModels = clusteringAPI.SelectedModels;
                    aIServiceRequestStatus.ProblemType = clusteringAPI.ProblemType;
                    aIServiceRequestStatus.retrain = clusteringAPI.retrain;
                    aIServiceRequestStatus.CreatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    aIServiceRequestStatus.ModifiedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    aIServiceRequestStatus.MaxDataPull = clusteringAPI.MaxDataPull;
                    var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(aIServiceRequestStatus);
                    var insertDocument = BsonSerializer.Deserialize<BsonDocument>(jsonData);
                    collection.DeleteOne(filter);
                    Thread.Sleep(1000);
                    collection.InsertOne(insertDocument);
                }
                                
                return true;
            }
            else
            {
                clusteringAPI._id = Guid.NewGuid().ToString();
                var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(clusteringAPI);
                var insertDocument = BsonSerializer.Deserialize<BsonDocument>(jsonData);
                collection.InsertOne(insertDocument);
                return true;
            }
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

        //Fetch usecase details based on Id
        public USECASE.UsecaseDetails GetUsecaseDetails(string usecaseId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CustomConfigService), nameof(GetUsecaseDetails), CONSTANTS.START, string.Empty, string.Empty, string.Empty, string.Empty);
            USECASE.UsecaseDetails usecaseDetails = new USECASE.UsecaseDetails();
            var useCaseCollection = _database.GetCollection<BsonDocument>("AISavedUsecases");
            var filter = Builders<BsonDocument>.Filter.Eq("UsecaseId", usecaseId);
            var projection = Builders<BsonDocument>.Projection.Exclude("_id");
            var result = useCaseCollection.Find(filter).Project<BsonDocument>(projection).FirstOrDefault();
            if (result != null)
            {
                usecaseDetails = JsonConvert.DeserializeObject<USECASE.UsecaseDetails>(result.ToJson());
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CustomConfigService), nameof(GetUsecaseDetails), CONSTANTS.END, string.Empty, string.Empty, string.Empty, string.Empty);
            return usecaseDetails;

        }

        public Service GetAiCoreServiceDetails(string serviceid)
        {
            Service service = new Service();
            var serviceCollection = _database.GetCollection<BsonDocument>("Services");
            var filter = Builders<BsonDocument>.Filter.Eq("ServiceId", serviceid);
            var projection = Builders<BsonDocument>.Projection.Exclude("_id");
            var result = serviceCollection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                service = JsonConvert.DeserializeObject<Service>(result[0].ToJson());
            }

            return service;
        }

        public ModelsList GetAICoreModels(string clientid, string dcid, string serviceid, string userid)
        {
            string encrypteduser = userid;
            if (!string.IsNullOrEmpty(encrypteduser))
            {
                encrypteduser = _encryptionDecryption.Encrypt(Convert.ToString(encrypteduser));
            }
            List<AICoreModels> serviceList = new List<AICoreModels>();
            ModelsList modelsLists = new ModelsList();
            var serviceCollection = _database.GetCollection<BsonDocument>("AICoreModels");
            var filter = Builders<BsonDocument>.Filter.Eq("ClientId", clientid) & Builders<BsonDocument>.Filter.Eq("DeliveryConstructId", dcid) & Builders<BsonDocument>.Filter.Eq("ServiceId", serviceid) & (Builders<BsonDocument>.Filter.Eq("CreatedBy", userid) | Builders<BsonDocument>.Filter.Eq("CreatedBy", encrypteduser));
            var projection = Builders<BsonDocument>.Projection.Exclude("_id");
            var result = serviceCollection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                if (appSettings.Value.isForAllData)
                {
                    for (int i = 0; i < result.Count; i++)
                    {
                        try
                        {
                            if (result[i].Contains("CreatedBy") && !string.IsNullOrEmpty(Convert.ToString(result[i]["CreatedBy"])))
                                result[i]["CreatedBy"] = _encryptionDecryption.Decrypt(Convert.ToString(result[i]["CreatedBy"]));
                        }
                        catch (Exception) { }
                        try
                        {
                            if (result[i].Contains("ModifiedBy") && !string.IsNullOrEmpty(Convert.ToString(result[i]["ModifiedBy"])))
                                result[i]["ModifiedBy"] = _encryptionDecryption.Decrypt(Convert.ToString(result[i]["ModifiedBy"]));
                        }
                        catch (Exception) { }
                    }
                }
                serviceList = JsonConvert.DeserializeObject<List<AICoreModels>>(result.ToJson());
                modelsLists.ModelStatus = serviceList;
            }

            var corrIdList = serviceList.Select(x => x.CorrelationId).ToArray();


            var ingestCollection = _database.GetCollection<BsonDocument>("AIServiceRequestStatus");
            var ingestFilter = Builders<BsonDocument>.Filter.AnyIn("CorrelationId", corrIdList) & (Builders<BsonDocument>.Filter.Eq("PageInfo", "TrainModel") | Builders<BsonDocument>.Filter.Eq("PageInfo", "Ingest_Train"));//Builders<BsonDocument>.Filter.Eq("Status","C") &
            var ingestProjection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id).Include("CorrelationId").Include("SelectedColumnNames").Include("DataSource").Include(CONSTANTS.ScoreUniqueName);
            var ingestResult = ingestCollection.Find(ingestFilter).Project<BsonDocument>(ingestProjection).ToList();
            if (ingestResult.Count > 0)
            {
                var modelDetailsList = JsonConvert.DeserializeObject<List<ModelColDetails>>(ingestResult.ToJson());
                var modelDetails = modelDetailsList.GroupBy(m => m.CorrelationId)
                                             .Select(g => g.First())
                                             .ToList();
                modelsLists.ModelColumns = modelDetails;
            }

            return modelsLists;
        }

        public bool GetAIServiceModelName(string ModelName, string serviceId, string clientUID, string deliveryUID, string userId)
        {
            string encrypteduser = userId;
            if (!string.IsNullOrEmpty(encrypteduser))
            {
                encrypteduser = _encryptionDecryption.Encrypt(Convert.ToString(encrypteduser));
            }
            var AIServiceRequestStatus = _database.GetCollection<BsonDocument>("AIServiceRequestStatus");
            var AIServiceFilterBuilder = Builders<BsonDocument>.Filter;
            var AIServiceQueue = AIServiceFilterBuilder.Eq("ModelName", ModelName) & AIServiceFilterBuilder.Eq("ServiceId", serviceId) & AIServiceFilterBuilder.Eq("ClientId", clientUID) & AIServiceFilterBuilder.Eq("DeliveryconstructId", deliveryUID) & (AIServiceFilterBuilder.Eq("CreatedByUser", userId) | AIServiceFilterBuilder.Eq("CreatedByUser", encrypteduser));
            var projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
            var result = AIServiceRequestStatus.Find(AIServiceQueue).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                var data = result[0]["ModelName"].ToString();
                if (data.ToLower() == ModelName.ToLower())
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public AICoreModels GetAICoreModelPath(string correlationid)
        {
            AICoreModels serviceList = new AICoreModels();
            var serviceCollection = _database.GetCollection<BsonDocument>("AICoreModels");
            var filter = Builders<BsonDocument>.Filter.Eq("CorrelationId", correlationid);
            var projection = Builders<BsonDocument>.Projection.Exclude("_id");
            var result = serviceCollection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                if (appSettings.Value.isForAllData)
                {
                    for (int i = 0; i < result.Count; i++)
                    {
                        try
                        {
                            if (result[i].Contains("CreatedBy") && !string.IsNullOrEmpty(Convert.ToString(result[i]["CreatedBy"])))
                                result[i]["CreatedBy"] = _encryptionDecryption.Decrypt(Convert.ToString(result[i]["CreatedBy"]));
                        }
                        catch (Exception) { }
                        try
                        {
                            if (result[i].Contains("ModifiedBy") && !string.IsNullOrEmpty(Convert.ToString(result[i]["ModifiedBy"])))
                                result[i]["ModifiedBy"] = _encryptionDecryption.Decrypt(Convert.ToString(result[i]["ModifiedBy"]));
                        }
                        catch (Exception) { }

                    }
                }
                serviceList = JsonConvert.DeserializeObject<AICoreModels>(result[0].ToJson());
            }

            return serviceList;
        }

        public JObject GetClusteringDetails(string correlationId)
        {
            JObject json = new JObject();
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.Clustering_IngestData);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
            var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                var newData = result[0];//ingestData.FirstOrDefault(corr => corr["CorrelationId"] == data.clusteringStatus[i].CorrelationId);
                if (newData != null)
                {
                    //data.clusteringStatus[i].ingestData = newData;
                    var jsonWriterSettings = new MongoDB.Bson.IO.JsonWriterSettings { OutputMode = MongoDB.Bson.IO.JsonOutputMode.Strict };
                    json = JObject.Parse(newData.ToJson<MongoDB.Bson.BsonDocument>(jsonWriterSettings));

                    // data.clusteringStatus[i].ingestData = json;// JsonConvert.DeserializeObject<dynamic>(newData.ToJson());

                }
            }
            return json;
        }

        public string PythonAIServiceToken()
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CustomConfigService), nameof(PythonAIServiceToken), CONSTANTS.START + appSettings.Value.authProvider.ToUpper(), string.Empty, string.Empty, string.Empty, string.Empty);
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
            else if (appSettings.Value.authProvider.ToUpper() == "AZUREAD")
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
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(CustomConfigService), nameof(PythonAIServiceToken), "PYTHON TOKEN PARAMS -- " + requestBuilder, string.Empty, string.Empty, string.Empty, string.Empty);
                IRestResponse response = client.Execute(request);
                string json = response.Content;
                // Retrieve and Return the Access Token                
                var tokenObj = JsonConvert.DeserializeObject(json) as dynamic;
                token = Convert.ToString(tokenObj.access_token);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CustomConfigService), nameof(PythonAIServiceToken), "END -" + appSettings.Value.authProvider.ToUpper(), string.Empty, string.Empty, string.Empty, string.Empty);
            return token;
        }
        
        #endregion AI Training Methods


        #region SSAI Training Methods
        public string UpdatePublicTemplateMapping(string appId, string usecaseId, dynamic data)
        {
            bool isMultiApp = false;
            bool isSingleApp = false;
            Uri apiUri = new Uri(appSettings.Value.myWizardAPIUrl);
            string host = apiUri.GetLeftPart(UriPartial.Authority);

            var MappingCollection = _database.GetCollection<PublicTemplateMapping>(CONSTANTS.PublicTemplateMapping);
            var Builder = Builders<PublicTemplateMapping>.Filter;
            var Projection1 = Builders<PublicTemplateMapping>.Projection.Exclude("_id");
            var filter = Builder.Eq(CONSTANTS.UsecaseID, usecaseId);
            var templatedata = MappingCollection.Find(filter).Project<PublicTemplateMapping>(Projection1).FirstOrDefault();
           
            if (templatedata != null && templatedata.IsMultipleApp == "yes")
            {
                if (templatedata.ApplicationIDs.Contains(appId))
                {
                    isMultiApp = true;

                }
            }
            else if (templatedata != null)
            {
                isSingleApp = true;
            }
            if (isSingleApp || isMultiApp)
            {
                string SourceURL = host;
                string apiPath = string.Empty;
                string SourceName = templatedata.SourceName;
                string queryData = data != null ? data["IterationUID"].ToString(Formatting.None) : templatedata.IterationUID;

                
                    if (!string.IsNullOrEmpty(templatedata.SourceName) && templatedata.SourceName.ToUpper() == CONSTANTS.CustomDataApi.ToUpper())
                    {
                        if (!string.IsNullOrEmpty(templatedata.SourceURL))
                        {
                            ApiDTO oCustomAPIDetails = JsonConvert.DeserializeObject<ApiDTO>(_encryptionDecryption.Decrypt(templatedata.SourceURL));

                        if (oCustomAPIDetails.Authentication.UseIngrainAzureCredentials)
                        {
                          //fetch Custom Source URI from ParamArgs Data
                            apiUri = new Uri(oCustomAPIDetails.ApiUrl);
                            string apihost = apiUri.GetLeftPart(UriPartial.Authority);

                            if (!apihost.Equals(host))
                            {
                                oCustomAPIDetails.ApiUrl = oCustomAPIDetails.ApiUrl.Replace(apihost, host);
                            }
                        }

                        if (oCustomAPIDetails != null && !string.IsNullOrEmpty(oCustomAPIDetails.ApiUrl))
                            {
                                SourceURL = _encryptionDecryption.Encrypt(JsonConvert.SerializeObject(oCustomAPIDetails));
                            }
                        }
                 //   }
                }

                PublicTemplateMapping templateMapping = new PublicTemplateMapping()
                {
                    ApplicationID = templatedata.ApplicationID,
                    SourceName = SourceName,
                    UsecaseID = usecaseId,
                    SourceURL = SourceURL,
                    InputParameters = "",
                    IterationUID = queryData
                };

                return _genericSelfservice.UpdatePublicTemplateMapping(templateMapping);

            }
            else
            {
                return CONSTANTS.NoRecordsFound;
            }
        }

        private VelocityTrainingStatus GetModelStatus(string clientId, string dcId, string applicationId, string usecaseId, string userId, string teamAreaUID, string usecaseType, string isMultiApp, string applicationIds)
        {
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
            var filterBuilder = Builders<BsonDocument>.Filter;
            FilterDefinition<BsonDocument> filterQueue = null;
            //if (usecaseType == "Team Level" && usecaseId == "f0320924-2ee3-4398-ad7c-8bc172abd78d")
            if (!string.IsNullOrEmpty(teamAreaUID))
            {
                filterQueue = filterBuilder.Eq(CONSTANTS.ClientId, clientId) & filterBuilder.Eq(CONSTANTS.DeliveryconstructId, dcId) & filterBuilder.Eq(CONSTANTS.TemplateUseCaseID, usecaseId) & filterBuilder.Eq(CONSTANTS.AppID, applicationId) & filterBuilder.Eq(CONSTANTS.pageInfo, CONSTANTS.TrainAndPredict) & filterBuilder.Eq("TeamAreaUId", teamAreaUID);
            }
            //if (usecaseType == "Team Level" && isMultiApp == "yes")
            if (!string.IsNullOrEmpty(teamAreaUID) && isMultiApp == "yes")
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
                    queueCollection.DeleteMany(filterBuilder1);
                    var deployCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
                    var deployFilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, result[0][CONSTANTS.CorrelationId].ToString());
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

        public bool IsAmbulanceLane(string useCaseId)
        {
            string[] ambulanceLane = new string[] { CONSTANTS.CRHigh, CONSTANTS.CRCritical, CONSTANTS.SRHigh, CONSTANTS.SRCritical, CONSTANTS.PRHigh, CONSTANTS.PRCritical };
            if (ambulanceLane.Contains(useCaseId))
                return true;
            else
                return false;
        }

        private IngrainRequestQueue CreateTrainingRequest(string NewModelCorrelationID, PublicTemplateMapping templatedata, TrainingRequestDTO RequestPayload)
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
                RetrainRequired = RequestPayload.RetrainRequired

            };
            return ingrainRequest;
        }

        private void InsertRequests(IngrainRequestQueue ingrainRequest)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CustomConfigService), nameof(InsertRequests), "Ingrain Request added For TemaplatedUseCaseID : " + ingrainRequest.TemplateUseCaseID, string.IsNullOrEmpty(ingrainRequest.CorrelationId) ? default(Guid) : new Guid(ingrainRequest.CorrelationId), string.Empty, string.Empty, ingrainRequest.ClientID, ingrainRequest.DeliveryconstructId);
            var requestQueue = JsonConvert.SerializeObject(ingrainRequest);
            var insertRequestQueue = BsonSerializer.Deserialize<BsonDocument>(requestQueue);
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
            collection.InsertOne(insertRequestQueue);
        }

        private string CallbackResponse(IngrainResponseData CallBackResponse, string AppName, string baseAddress, string clientId, string DCId, string applicationId, string usecaseId, string requestId, string userId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CustomConfigService), nameof(CallbackResponse), "CallbackResponse - Started :", applicationId, string.Empty, clientId, DCId);
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
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(CustomConfigService), nameof(CallbackResponse), "CallbackResponse - SUCCESS END :" + httpResponse.Content, applicationId, string.Empty, clientId, DCId);
                    return CONSTANTS.success;
                }
                else
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(CustomConfigService), nameof(CallbackResponse), "CallbackResponse - ERROR END :" + statuscode + "--" + httpResponse.Content, applicationId, string.Empty, clientId, DCId);
                    return CONSTANTS.Error;
                }
            }
        }

        private string CustomUrlToken(string ApplicationName)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CustomConfigService), nameof(CustomUrlToken), "CustomUrlToken for Application--" + ApplicationName, string.Empty, string.Empty, string.Empty, string.Empty);
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
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(CustomConfigService), nameof(CustomUrlToken), "Application TOKEN PARAMS -- " + requestBuilder, string.Empty, string.Empty, string.Empty, string.Empty);
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
                }
            }
            return token;
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

        public void UpdateIngrainRequest(string requestId, string notificationMessage, string isNoficationSent, string sendnotification)
        {
            var requestCollection = _database.GetCollection<IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
            var filter = Builders<IngrainRequestQueue>.Filter.Where(x => x.RequestId == requestId);
            var update = Builders<IngrainRequestQueue>.Update.Set(x => x.IsNotificationSent, isNoficationSent).Set(x => x.NotificationMessage, notificationMessage).Set(x => x.SendNotification, sendnotification).Set(x => x.ModifiedOn, DateTime.Now.ToString(CONSTANTS.DateHoursFormat));
            requestCollection.UpdateOne(filter, update);
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
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CustomConfigService), nameof(UpdateAppIntegration), "UpdateAppIntegration - status : " + status
                , appIntegrations.ApplicationID, string.Empty, appIntegrations.clientUId, appIntegrations.deliveryConstructUID);
            return status;
        }

        #endregion SSAI Training Methods
    }
}
