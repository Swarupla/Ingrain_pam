using Accenture.MyWizard.Ingrain.DataModels;
using Accenture.MyWizard.Ingrain.DataModels.Models;
using Accenture.MyWizard.Ingrain.WindowService.Models;
using Accenture.MyWizard.SelfServiceAI.WindowService.Models;
using Accenture.MyWizard.Shared.Helpers;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
using Accenture.MyWizard.Ingrain.WindowService;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using Accenture.MyWizard.Ingrain.WindowService.Interfaces;
using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
using Accenture.MyWizard.Cryptography.EncryptionProviders;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using RestSharp;
using Accenture.MyWizard.Ingrain.BusinessDomain.Common;
using DATAMODELS = Accenture.MyWizard.Ingrain.DataModels.Models;
using WINSERVICEMODELS = Accenture.MyWizard.Ingrain.WindowService.Models;
using System.Collections;
using CryptographyHelper = Accenture.MyWizard.Cryptography;

namespace Accenture.MyWizard.Ingrain.WindowService.Services
{
    public class GenericAutotrainService : IGenericAutoTrainService
    {
        #region Members
        private MongoClient _mongoClient;
        private IMongoDatabase _database;
        bool insertSuccess = false;
        DatabaseProvider databaseProvider;
        GenericTemplatemapping _templateInfo;
        GenericAutoTrain _GenericDataResponse;
        private PreProcessDTO _preProcessDTO;
        private RecommedAITrainedModel _recommendedAI;
        private PublicTemplateMapping _Mapping;
        private string ParamArgs = null;
        private IConfigurationRoot appSettings;
        private string _requestId = null;
        private string _CallbackURL = null;
        private string _CheckPrediction = null;
        private string NewCorrelationId = null;
        private JObject _textdata;
        private string paramArgsCustomMultipleFetch;
        private string _aesKey;
        private string _aesVector;
        private bool _DBEncryptionRequired = true; // for models default encryption is set to true
        private bool _TemplateDBEncryptionRequired = false;
        private IngrainResponseData _IngrainResponseData;
        private List<string> _Frequency = new List<string>();
        private string _authProvider;
        private string _parentCorrelationId;
        private bool _isGenericModel;
        //private string _myWizardAPIUrl;
        //private string _AzureADAuthProviderConfigFilePath;
        public string tokenapiURL;
        public string username;
        public string password;
        private string _resourceId;
        private string _token_Url;
        private string _Grant_Type;
        private string _clientId;
        private string _clientSecret;
        private string _scopeStatus;
        private string _language;
        private string _AppServiceUId;
        private double _PredictionTimeoutMinutes;
        private bool _dbEncryption;
        private bool _isForAllData;
        private string SPE_appId = "fa36e811-a59f-48c0-94a6-9a7ffc8bc8ab";
        string problemType = string.Empty;
        private bool IsUpdateDataClenaup = false;
        private double _timeDiffInMinutes = 0;
        private double _modelsTrainingTimeLimit = 15;
        private string Environment;
        private bool IsBusinessProblemDataAvailable;
        private readonly WINSERVICEMODELS.AppSettings appConfigSettings = null;
        private bool _IsAESKeyVault;
        static CryptographyHelper.Utility.CryptographyUtility CryptographyUtility = null;
        List<string> VDSUseCaseIds = new List<string>() { "f97739d7-d3b1-491b-8af1-876485cd3d30", "49d56fe0-1eca-4406-8b52-38724ac3b705", "7848b5c2-5167-49ea-9148-00be0da491c6" };
        #endregion

        #region Constructor
        public GenericAutotrainService(DatabaseProvider db)
        {
            databaseProvider = db;
            appSettings = AppSettingsJson.GetAppSettings();
            appConfigSettings = AppSettingsJson.GetAppSettings().GetSection("AppSettings").Get<WINSERVICEMODELS.AppSettings>();
            _mongoClient = databaseProvider.GetDatabaseConnection();
            var dataBaseName = MongoUrl.Create(appSettings.GetSection("AppSettings").GetSection("connectionString").Value).DatabaseName;
            _database = _mongoClient.GetDatabase(dataBaseName);
            _preProcessDTO = new PreProcessDTO();
            _templateInfo = new GenericTemplatemapping();
            _GenericDataResponse = new GenericAutoTrain();
            _Mapping = new PublicTemplateMapping();
            _textdata = new JObject();
            _aesKey = appSettings.GetSection("AppSettings").GetSection("aesKey").Value;
            _aesVector = appSettings.GetSection("AppSettings").GetSection("aesVector").Value;
            _IngrainResponseData = new IngrainResponseData();
            _token_Url = appSettings.GetSection("AppSettings").GetSection("token_Url").Value;
            _language = appSettings.GetSection("AppSettings").GetSection("languageURL").Value;
            _clientId = appSettings.GetSection("AppSettings").GetSection("clientId").Value;
            _clientSecret = appSettings.GetSection("AppSettings").GetSection("clientSecret").Value;
            _scopeStatus = appSettings.GetSection("AppSettings").GetSection("scopeStatus").Value;
            _Grant_Type = appSettings.GetSection("AppSettings").GetSection("Grant_Type").Value;
            _resourceId = appSettings.GetSection("AppSettings").GetSection("resourceId").Value;
            _authProvider = appSettings.GetSection("AppSettings").GetSection("authProvider").Value;

            tokenapiURL = appSettings.GetSection("AppSettings").GetSection("tokenAPIUrl").Value;
            username = appSettings.GetSection("AppSettings").GetSection("username").Value;
            password = appSettings.GetSection("AppSettings").GetSection("password").Value;
            _AppServiceUId = appSettings.GetSection("AppSettings").GetSection("AppServiceUId").Value;
            _PredictionTimeoutMinutes = Convert.ToDouble(appSettings.GetSection("AppSettings").GetSection("PredictionTimeoutMinutes").Value);
            _dbEncryption = Convert.ToBoolean(appSettings.GetSection("AppSettings").GetSection("DBEncryption").Value);
            _isForAllData = Convert.ToBoolean(appSettings.GetSection("AppSettings").GetSection("isForAllData").Value);
            _modelsTrainingTimeLimit = Convert.ToDouble(appSettings.GetSection("AppSettings").GetSection("ModelsTrainingTimeLimit").Value);
            Environment = appSettings.GetSection("AppSettings").GetSection("Environment").Value;
            _IsAESKeyVault = Convert.ToBoolean(appSettings.GetSection("AppSettings").GetSection("IsAESKeyVault").Value);
            CryptographyUtility = new CryptographyHelper.Utility.CryptographyUtility();
        }
        #endregion 
        public GenericAutoTrain PrivateModelTraining(IngrainRequestQueue result)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(PrivateModelTraining), "PrivateModelTraining - Started :", string.IsNullOrEmpty(result.CorrelationId) ? default(Guid) : new Guid(result.CorrelationId), result.AppID, string.Empty, result.ClientID, result.DeliveryconstructId);
            try
            {
                NewCorrelationId = result.CorrelationId;
                _requestId = result.RequestId;
                _CheckPrediction = result.pageInfo;
                _CallbackURL = result.AppURL;
                _templateInfo.ClientUID = result.ClientId;
                _templateInfo.DCID = result.DeliveryconstructId;
                _templateInfo.pageInfo = result.pageInfo;
                _templateInfo.Function = result.Function;
                _Mapping.CreatedByUser = result.CreatedByUser;
                _Mapping.UsecaseID = result.TemplateUseCaseID;
                _Mapping.ApplicationID = result.AppID;
                paramArgsCustomMultipleFetch = result.ParamArgs;// JsonConvert.SerializeObject(result.ParamArgs);                
                bool status = true;
                _isGenericModel = false;
                List<PublicTemplateMapping> MappingResult = new List<PublicTemplateMapping>();
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(PrivateModelTraining), "Correlation Id : " + NewCorrelationId, string.IsNullOrEmpty(NewCorrelationId) ? default(Guid) : new Guid(NewCorrelationId), result.AppID, string.Empty, _templateInfo.ClientUID, _templateInfo.DCID);
                MappingResult = GetDataMapping(_Mapping.UsecaseID, _Mapping.ApplicationID);
                if (MappingResult != null && MappingResult.Count > 0)
                {
                    _Mapping.UsecaseName = MappingResult[0].UsecaseName;
                    _Mapping.SourceName = MappingResult[0].SourceName;
                    _Mapping.SourceURL = MappingResult[0].SourceURL;
                    if (MappingResult[0].IsMultipleApp == "yes")
                    {
                        var AppIntegCollection = _database.GetCollection<AppIntegration>(CONSTANTS.AppIntegration);
                        var filterBuilder1 = Builders<AppIntegration>.Filter;
                        var AppFilter = filterBuilder1.Eq(CONSTANTS.ApplicationID, result.AppID);

                        var Projection = Builders<AppIntegration>.Projection.Exclude("_id");
                        var AppData = AppIntegCollection.Find(AppFilter).Project<AppIntegration>(Projection).FirstOrDefault();
                        _Mapping.ApplicationName = AppData.ApplicationName;
                    }
                    else
                    {
                        _Mapping.ApplicationName = MappingResult[0].ApplicationName;
                    }
                    _Mapping.DateColumn = MappingResult[0].DateColumn;
                    _Mapping.IterationUID = MappingResult[0].IterationUID;
                    if (_templateInfo.pageInfo == CONSTANTS.TrainAndPredict)
                    {
                        if ((Environment.Equals(CONSTANTS.FDSEnvironment) || Environment.Equals(CONSTANTS.PAMEnvironment)) & _Mapping.SourceName == CONSTANTS.VDS_AIOPS)
                        {
                            var fileParams = JsonConvert.DeserializeObject<VDSInputParams>(paramArgsCustomMultipleFetch);
                            _templateInfo.ModelName = _Mapping.ApplicationName + "_" + _Mapping.UsecaseName + "_" + fileParams.RequestType + "_" + fileParams.ServiceType;
                        }
                        else
                            _templateInfo.ModelName = _Mapping.ApplicationName + "_" + _Mapping.UsecaseName;
                    }
                    else
                        _templateInfo.ModelName = _Mapping.ApplicationName + "_" + _Mapping.UsecaseName + "_" + NewCorrelationId;
                    _Mapping.TeamAreaUId = result.TeamAreaUId;
                    string callbackResonse = string.Empty;
                    GetBusinessProblemData(_Mapping.UsecaseID);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(PrivateModelTraining), "_Mapping.SourceName--" + _Mapping.SourceName + "-_Mapping.SourceURL- " + _Mapping.SourceURL + "- _Mapping.Application Name- " + _Mapping.ApplicationName + "-_Mapping.IsMultiple- " + MappingResult[0].IsMultipleApp, string.IsNullOrEmpty(NewCorrelationId) ? default(Guid) : new Guid(NewCorrelationId), result.AppID, string.Empty, _templateInfo.ClientUID, _templateInfo.DCID);
                    if (IsBusinessProblemDataAvailable)
                    {
                        if (_Mapping.SourceName == "DataSet")
                        {
                            _templateInfo.ParamArgs = paramArgsCustomMultipleFetch;
                            _isGenericModel = true;
                        }
                        else if ((Environment.Equals(CONSTANTS.FDSEnvironment) || Environment.Equals(CONSTANTS.PAMEnvironment)) & _Mapping.SourceName == CONSTANTS.VDS_AIOPS)
                        {
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(PrivateModelTraining), "INSIDE VDS()AIOPS-APP ---_Mapping.SourceName--" + _Mapping.SourceName + "-_Mapping.SourceURL-" + _Mapping.SourceURL, string.IsNullOrEmpty(NewCorrelationId) ? default(Guid) : new Guid(NewCorrelationId), result.AppID, string.Empty, _templateInfo.ClientUID, _templateInfo.DCID);
                            SourceCustomdata(NewCorrelationId, _Mapping.SourceName, _Mapping.ApplicationName);
                            _isGenericModel = true;
                        }
                        else if (_Mapping.SourceName == "Custom" && !string.IsNullOrEmpty(_Mapping.SourceURL))
                        {
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(PrivateModelTraining), "INSIDE --_Mapping.SourceName--" + _Mapping.SourceName + "-_Mapping.SourceURL-" + _Mapping.SourceURL, string.IsNullOrEmpty(NewCorrelationId) ? default(Guid) : new Guid(NewCorrelationId), result.AppID, string.Empty, _templateInfo.ClientUID, _templateInfo.DCID);

                            if (!string.IsNullOrEmpty(paramArgsCustomMultipleFetch) && paramArgsCustomMultipleFetch != CONSTANTS.Null) //SPE
                            {
                                _templateInfo.ParamArgs = paramArgsCustomMultipleFetch;
                                if (_Mapping.ApplicationName == "myWizard.ImpactAnalyzer")
                                {
                                    _isGenericModel = true;
                                }
                            }
                            else //Generic
                            {
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(PrivateModelTraining), "INSIDE ELSE--_Mapping.SourceName--" + _Mapping.SourceName + "-_Mapping.SourceURL-" + _Mapping.SourceURL, string.IsNullOrEmpty(NewCorrelationId) ? default(Guid) : new Guid(NewCorrelationId), result.AppID, string.Empty, _templateInfo.ClientUID, _templateInfo.DCID);
                                SourceCustomdata(NewCorrelationId, null, null);
                                _isGenericModel = true;
                            }
                        }
                        else if (_Mapping.SourceName == CONSTANTS.SPAAPP) // & !string.IsNullOrEmpty(_Mapping.SourceURL)
                        {
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(PrivateModelTraining), "INSIDE SPA-APP ---_Mapping.SourceName--" + _Mapping.SourceName + "-_Mapping.SourceURL-" + _Mapping.SourceURL, string.IsNullOrEmpty(NewCorrelationId) ? default(Guid) : new Guid(NewCorrelationId), result.AppID, string.Empty, _templateInfo.ClientUID, _templateInfo.DCID);
                            //SourceCustomdata(NewCorrelationId, CONSTANTS.SPAAPP);
                            SourceCustomdata(NewCorrelationId, CONSTANTS.SPAAPP, _Mapping.ApplicationName);
                            _isGenericModel = true;
                        }
                        else if (_Mapping.SourceName.ToUpper() == "CustomMultiple".ToUpper() && !string.IsNullOrEmpty(_Mapping.SourceURL))
                            _templateInfo.ParamArgs = paramArgsCustomMultipleFetch;
                        else if (_Mapping.SourceName.ToUpper() == "CustomSingle".ToUpper() && !string.IsNullOrEmpty(_Mapping.SourceURL))
                            _templateInfo.ParamArgs = paramArgsCustomMultipleFetch;
                        else if (_Mapping.SourceName.ToUpper() == CONSTANTS.CustomDbQuery.ToUpper() || _Mapping.SourceName.ToUpper() == CONSTANTS.CustomDataApi.ToUpper())
                        {
                            status = GetCustomDataIngrainRequestData(_Mapping.UsecaseID, NewCorrelationId, _Mapping.SourceName, _templateInfo.ClientUID, _templateInfo.DCID);
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(PrivateModelTraining), "GetCustomDataIngrainRequestData status--" + status, string.IsNullOrEmpty(NewCorrelationId) ? default(Guid) : new Guid(NewCorrelationId), result.AppID, string.Empty, _templateInfo.ClientUID, _templateInfo.DCID);
                            _isGenericModel = true;
                        }
                        else
                        {
                            status = GetIngrainRequestData(_Mapping.UsecaseID, NewCorrelationId, _Mapping.SourceName, _templateInfo.ClientUID, _templateInfo.DCID);
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(PrivateModelTraining), "GetIngrainRequestData status--" + status, string.IsNullOrEmpty(NewCorrelationId) ? default(Guid) : new Guid(NewCorrelationId), result.AppID, string.Empty, _templateInfo.ClientUID, _templateInfo.DCID);
                            _isGenericModel = true;
                        }
                        if (status)
                        {
                            InsertBusinessProblem(_templateInfo, NewCorrelationId);
                            CreateInstaModel(_templateInfo.ProblemType, NewCorrelationId, result.DataSetUId);
                            IngestDataInsertRequests(NewCorrelationId, _templateInfo.ClientUID, _templateInfo.DCID, _templateInfo.ProblemType, _templateInfo.TargetColumn, _templateInfo.AppicationID, _templateInfo.ParamArgs, result.DataSetUId, _templateInfo.Function);
                            InsertCustomConstraintsRequests(NewCorrelationId, _Mapping.UsecaseID);
                            Thread.Sleep(1000);
                            ValidatingIngestDataCompletion(NewCorrelationId, _templateInfo);
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(PrivateModelTraining), "***VALIDATINGINGESTDATACOMPLETION ENDED****" + _GenericDataResponse.Status, string.IsNullOrEmpty(NewCorrelationId) ? default(Guid) : new Guid(NewCorrelationId), result.AppID, string.Empty, _templateInfo.ClientUID, _templateInfo.DCID);

                            _IngrainResponseData.CorrelationId = NewCorrelationId;
                            //add notification log
                            if (_templateInfo.AppicationID == SPE_appId)
                                Insert_notification(SPE_appId, result.CreatedByUser, NewCorrelationId, result.AppURL, CONSTANTS.P, "25%", _GenericDataResponse.Message, _GenericDataResponse.ErrorMessage);

                            if (_GenericDataResponse.Status == CONSTANTS.C)
                            {
                                StartModelTraining(_templateInfo, NewCorrelationId, result.AppURL);
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(PrivateModelTraining), "GENERICDATARESPONSE- STATUS" + _GenericDataResponse.Status, string.IsNullOrEmpty(NewCorrelationId) ? default(Guid) : new Guid(NewCorrelationId), result.AppID, string.Empty, _templateInfo.ClientUID, _templateInfo.DCID);

                                if (_GenericDataResponse.Status == "C")
                                {
                                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(PrivateModelTraining), "GENERICDATARESPONSE- REQUESTID--" + _requestId + "**GENERICDATARESPONSE***" + _GenericDataResponse.Status, string.IsNullOrEmpty(NewCorrelationId) ? default(Guid) : new Guid(NewCorrelationId), result.AppID, string.Empty, _templateInfo.ClientUID, _templateInfo.DCID);

                                    UpdateTrainingStatus(_GenericDataResponse.Status, _GenericDataResponse.Message, CONSTANTS.Completed, NewCorrelationId, _templateInfo.ClientUID, _templateInfo.DCID, _Mapping.ApplicationID, _Mapping.UsecaseID, null, _Mapping.CreatedByUser);
                                    if (_CheckPrediction == CONSTANTS.TrainAndPredict && (_templateInfo.AppicationID == CONSTANTS.VDSApplicationID_FDS || _templateInfo.AppicationID == CONSTANTS.VDSApplicationID_PAM))
                                    {
                                        var fileParams = JsonConvert.DeserializeObject<VDSInputParams>(paramArgsCustomMultipleFetch);
                                        IngrainToVDSNotification VDSCallbackResponsedata = new IngrainToVDSNotification
                                        {
                                            CorrelationId = NewCorrelationId,
                                            Status = "Completed",
                                            Message = "Training completed for the usecaseid",
                                            ErrorMessage = string.Empty,
                                            ClientUId = _templateInfo.ClientUID,
                                            DeliveryConstructUId = _templateInfo.DCID,
                                            UseCaseId = _Mapping.UsecaseID,
                                            RequestType = result.RequestType,
                                            ServiceType = result.ServiceType,
                                            E2EUID = fileParams.E2EUID,
                                            Progress = "100%",
                                            ProcessingStartTime = result.CreatedOn,
                                            ProcessingEndTime = DateTime.Now.ToString(CONSTANTS.DateHoursFormat)
                                        };
                                        string VDScallbackResonse = VDSCallbackResponse(VDSCallbackResponsedata, _Mapping.ApplicationName, result.AppURL, _templateInfo.ClientUID, _templateInfo.DCID, _Mapping.ApplicationID, _Mapping.UsecaseID, _requestId, null, _Mapping.CreatedByUser);
                                        return _GenericDataResponse;
                                    }
                                    if (_CheckPrediction == CONSTANTS.TrainAndPredict)
                                    {
                                        _IngrainResponseData.CorrelationId = NewCorrelationId;
                                        _IngrainResponseData.Message = CONSTANTS.TrainingCompleted;
                                        _IngrainResponseData.Status = "Completed";
                                        _ = _templateInfo.AppicationID == SPE_appId ? Insert_notification(SPE_appId, result.CreatedByUser, NewCorrelationId, result.AppURL, CONSTANTS.C, "100%", CONSTANTS.TrainingCompleted, _GenericDataResponse.ErrorMessage) : CallbackResponse(_IngrainResponseData, _Mapping.ApplicationName, result.AppURL, _templateInfo.ClientUID, _templateInfo.DCID, _Mapping.ApplicationID, _Mapping.UsecaseID, _requestId, null, _Mapping.CreatedByUser);
                                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(PrivateModelTraining), "TRAINING SUCCESS--Status" + _GenericDataResponse.Status, string.IsNullOrEmpty(NewCorrelationId) ? default(Guid) : new Guid(NewCorrelationId), result.AppID, string.Empty, _templateInfo.ClientUID, _templateInfo.DCID);

                                    }
                                    if (!string.IsNullOrEmpty(_CallbackURL) & _CheckPrediction != CONSTANTS.TrainAndPredict)
                                    {
                                        if (_Mapping.ApplicationName == CONSTANTS.SPEAPP)
                                        {
                                            _IngrainResponseData.CorrelationId = NewCorrelationId;
                                            _IngrainResponseData.Message = CONSTANTS.TrainingCompleted;
                                            _IngrainResponseData.Status = CONSTANTS.C;
                                            _ = _templateInfo.AppicationID == SPE_appId ? Insert_notification(SPE_appId, result.CreatedByUser, NewCorrelationId, result.AppURL, CONSTANTS.C, "100%", CONSTANTS.TrainingCompleted, _GenericDataResponse.ErrorMessage) : CallbackResponse(_IngrainResponseData, _Mapping.ApplicationName, result.AppURL, _templateInfo.ClientUID, _templateInfo.DCID, _Mapping.ApplicationID, _Mapping.UsecaseID, _requestId, null, _Mapping.CreatedByUser);
                                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(PrivateModelTraining), "TRAINING SUCCESS--Status" + _GenericDataResponse.Status, string.IsNullOrEmpty(NewCorrelationId) ? default(Guid) : new Guid(NewCorrelationId), result.AppID, string.Empty, _templateInfo.ClientUID, _templateInfo.DCID);

                                        }
                                    }
                                    this.delete_backUpModelSPP(NewCorrelationId, _GenericDataResponse.Status);
                                }
                                else
                                {
                                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(PrivateModelTraining), "GENERICDATARESPONSE -STATUS ELSE" + _GenericDataResponse.Status, string.IsNullOrEmpty(NewCorrelationId) ? default(Guid) : new Guid(NewCorrelationId), result.AppID, string.Empty, _templateInfo.ClientUID, _templateInfo.DCID);

                                    UpdateTrainingStatus(_GenericDataResponse.Status, _GenericDataResponse.ErrorMessage, CONSTANTS.ErrorMessage, NewCorrelationId, _templateInfo.ClientUID, _templateInfo.DCID, _Mapping.ApplicationID, _Mapping.UsecaseID, null, _Mapping.CreatedByUser);
                                    if (_CheckPrediction == CONSTANTS.TrainAndPredict && (_templateInfo.AppicationID == CONSTANTS.VDSApplicationID_FDS || _templateInfo.AppicationID == CONSTANTS.VDSApplicationID_PAM))
                                    {
                                        var fileParams = JsonConvert.DeserializeObject<VDSInputParams>(paramArgsCustomMultipleFetch);
                                        IngrainToVDSNotification VDSCallbackResponsedata = new IngrainToVDSNotification
                                        {
                                            CorrelationId = NewCorrelationId,
                                            Status = CONSTANTS.ErrorMessage,
                                            Message = CONSTANTS.TrainingFailed,
                                            ErrorMessage = _GenericDataResponse.ErrorMessage,
                                            ClientUId = _templateInfo.ClientUID,
                                            DeliveryConstructUId = _templateInfo.DCID,
                                            UseCaseId = _Mapping.UsecaseID,
                                            RequestType = result.RequestType,
                                            ServiceType = result.ServiceType,
                                            E2EUID = fileParams.E2EUID,
                                            Progress = "0%",
                                            ProcessingStartTime = result.CreatedOn,
                                            ProcessingEndTime = DateTime.Now.ToString(CONSTANTS.DateHoursFormat)
                                        };
                                        string VDScallbackResonse = VDSCallbackResponse(VDSCallbackResponsedata, _Mapping.ApplicationName, result.AppURL, _templateInfo.ClientUID, _templateInfo.DCID, _Mapping.ApplicationID, _Mapping.UsecaseID, _requestId, null, _Mapping.CreatedByUser);
                                        return _GenericDataResponse;
                                    }
                                    if (_CheckPrediction == CONSTANTS.TrainAndPredict)
                                    {
                                        _IngrainResponseData.CorrelationId = NewCorrelationId;
                                        _IngrainResponseData.Message = CONSTANTS.TrainingFailed;
                                        _IngrainResponseData.Status = CONSTANTS.ErrorMessage;
                                        _IngrainResponseData.ErrorMessage = _GenericDataResponse.ErrorMessage;
                                        _ = _templateInfo.AppicationID == SPE_appId ? Insert_notification(SPE_appId, result.CreatedByUser, NewCorrelationId, result.AppURL, _GenericDataResponse.Status, "100%", _GenericDataResponse.Message, _GenericDataResponse.ErrorMessage) : CallbackResponse(_IngrainResponseData, _Mapping.ApplicationName, result.AppURL, _templateInfo.ClientUID, _templateInfo.DCID, _Mapping.ApplicationID, _Mapping.UsecaseID, _requestId, null, _Mapping.CreatedByUser);
                                    }
                                    if (!string.IsNullOrEmpty(_CallbackURL) & _CheckPrediction != CONSTANTS.TrainAndPredict)
                                    {
                                        if (_Mapping.ApplicationName == CONSTANTS.SPEAPP)
                                        {
                                            _IngrainResponseData.CorrelationId = NewCorrelationId;
                                            _IngrainResponseData.Message = CONSTANTS.TrainingFailed;
                                            _IngrainResponseData.Status = CONSTANTS.E;
                                            _IngrainResponseData.ErrorMessage = _GenericDataResponse.ErrorMessage;
                                            _ = _templateInfo.AppicationID == SPE_appId ? Insert_notification(SPE_appId, result.CreatedByUser, NewCorrelationId, result.AppURL, _GenericDataResponse.Status, "100%", _GenericDataResponse.Message, _GenericDataResponse.ErrorMessage) : CallbackResponse(_IngrainResponseData, _Mapping.ApplicationName, result.AppURL, _templateInfo.ClientUID, _templateInfo.DCID, _Mapping.ApplicationID, _Mapping.UsecaseID, _requestId, null, _Mapping.CreatedByUser);
                                        }
                                    }
                                }
                                this.delete_backUpModelSPP(NewCorrelationId, _GenericDataResponse.Status);
                                return _GenericDataResponse;
                            }
                            else
                            {
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(PrivateModelTraining), "***GENERICDATARESPONSE-STATUS****" + _GenericDataResponse.Status, string.IsNullOrEmpty(NewCorrelationId) ? default(Guid) : new Guid(NewCorrelationId), result.AppID, string.Empty, _templateInfo.ClientUID, _templateInfo.DCID);

                                UpdateTrainingStatus(_GenericDataResponse.Status, _GenericDataResponse.ErrorMessage, CONSTANTS.ErrorMessage, NewCorrelationId, _templateInfo.ClientUID, _templateInfo.DCID, _Mapping.ApplicationID, _Mapping.UsecaseID, null, _Mapping.CreatedByUser);
                                if (_CheckPrediction == CONSTANTS.TrainAndPredict && (_templateInfo.AppicationID == CONSTANTS.VDSApplicationID_FDS || _templateInfo.AppicationID == CONSTANTS.VDSApplicationID_PAM))
                                {
                                    var fileParams = JsonConvert.DeserializeObject<VDSInputParams>(paramArgsCustomMultipleFetch);
                                    IngrainToVDSNotification VDSCallbackResponsedata = new IngrainToVDSNotification
                                    {
                                        CorrelationId = NewCorrelationId,
                                        Status = CONSTANTS.ErrorMessage,
                                        Message = CONSTANTS.TrainingFailed,
                                        ErrorMessage = _GenericDataResponse.ErrorMessage,
                                        ClientUId = _templateInfo.ClientUID,
                                        DeliveryConstructUId = _templateInfo.DCID,
                                        UseCaseId = _Mapping.UsecaseID,
                                        RequestType = result.RequestType,
                                        ServiceType = result.ServiceType,
                                        E2EUID = fileParams.E2EUID,
                                        Progress = "0%",
                                        ProcessingStartTime = result.CreatedOn,
                                        ProcessingEndTime = DateTime.Now.ToString(CONSTANTS.DateHoursFormat)
                                    };
                                    string VDScallbackResonse = VDSCallbackResponse(VDSCallbackResponsedata, _Mapping.ApplicationName, result.AppURL, _templateInfo.ClientUID, _templateInfo.DCID, _Mapping.ApplicationID, _Mapping.UsecaseID, _requestId, null, _Mapping.CreatedByUser);
                                    return _GenericDataResponse;
                                }
                                if (_CheckPrediction == CONSTANTS.TrainAndPredict)
                                {
                                    _IngrainResponseData.Message = CONSTANTS.TrainingFailed;
                                    _IngrainResponseData.CorrelationId = NewCorrelationId;
                                    _IngrainResponseData.Status = CONSTANTS.ErrorMessage;
                                    _IngrainResponseData.ErrorMessage = _GenericDataResponse.ErrorMessage;
                                    _ = _templateInfo.AppicationID == SPE_appId ? Insert_notification(SPE_appId, result.CreatedByUser, NewCorrelationId, result.AppURL, _GenericDataResponse.Status, "100%", _GenericDataResponse.Message, _GenericDataResponse.ErrorMessage) : CallbackResponse(_IngrainResponseData, _Mapping.ApplicationName, result.AppURL, _templateInfo.ClientUID, _templateInfo.DCID, _Mapping.ApplicationID, _Mapping.UsecaseID, _requestId, null, _Mapping.CreatedByUser);
                                    //CallbackResponse(_IngrainResponseData, _Mapping.ApplicationName, result.AppURL, _templateInfo.ClientUID, _templateInfo.DCID, _Mapping.ApplicationID, _Mapping.UsecaseID, _requestId, null, _Mapping.CreatedByUser);
                                }

                                if (!string.IsNullOrEmpty(_CallbackURL) & _CheckPrediction != CONSTANTS.TrainAndPredict)
                                {
                                    if (_Mapping.ApplicationName == CONSTANTS.SPEAPP)
                                    {
                                        _IngrainResponseData.Message = CONSTANTS.TrainingFailed;
                                        _IngrainResponseData.Status = CONSTANTS.E;
                                        _IngrainResponseData.CorrelationId = NewCorrelationId;
                                        _IngrainResponseData.ErrorMessage = _GenericDataResponse.ErrorMessage;
                                        _ = _templateInfo.AppicationID == SPE_appId ? Insert_notification(SPE_appId, result.CreatedByUser, NewCorrelationId, result.AppURL, _GenericDataResponse.Status, "100%", _GenericDataResponse.Message, _GenericDataResponse.ErrorMessage) : CallbackResponse(_IngrainResponseData, _Mapping.ApplicationName, result.AppURL, _templateInfo.ClientUID, _templateInfo.DCID, _Mapping.ApplicationID, _Mapping.UsecaseID, _requestId, null, _Mapping.CreatedByUser);
                                    }
                                }
                                this.delete_backUpModelSPP(NewCorrelationId, _GenericDataResponse.Status);
                                return _GenericDataResponse;
                            }
                        }
                        else
                        {
                            _GenericDataResponse.ErrorMessage = CONSTANTS.ParamArgsNull;
                            _GenericDataResponse.Status = "E";
                            UpdateTrainingStatus(_GenericDataResponse.Status, _GenericDataResponse.ErrorMessage, CONSTANTS.ErrorMessage, NewCorrelationId, _templateInfo.ClientUID, _templateInfo.DCID, _Mapping.ApplicationID, _Mapping.UsecaseID, null, _Mapping.CreatedByUser);
                            if (_CheckPrediction == CONSTANTS.TrainAndPredict && (_templateInfo.AppicationID == CONSTANTS.VDSApplicationID_FDS || _templateInfo.AppicationID == CONSTANTS.VDSApplicationID_PAM))
                            {
                                var fileParams = JsonConvert.DeserializeObject<VDSInputParams>(paramArgsCustomMultipleFetch);
                                IngrainToVDSNotification VDSCallbackResponsedata = new IngrainToVDSNotification
                                {
                                    CorrelationId = NewCorrelationId,
                                    Status = CONSTANTS.ErrorMessage,
                                    Message = CONSTANTS.TrainingFailed,
                                    ErrorMessage = _GenericDataResponse.ErrorMessage,
                                    ClientUId = _templateInfo.ClientUID,
                                    DeliveryConstructUId = _templateInfo.DCID,
                                    UseCaseId = _Mapping.UsecaseID,
                                    RequestType = result.RequestType,
                                    ServiceType = result.ServiceType,
                                    E2EUID = fileParams.E2EUID,
                                    Progress = "0%",
                                    ProcessingStartTime = result.CreatedOn,
                                    ProcessingEndTime = DateTime.Now.ToString(CONSTANTS.DateHoursFormat)
                                };
                                string VDScallbackResonse = VDSCallbackResponse(VDSCallbackResponsedata, _Mapping.ApplicationName, result.AppURL, _templateInfo.ClientUID, _templateInfo.DCID, _Mapping.ApplicationID, _Mapping.UsecaseID, _requestId, null, _Mapping.CreatedByUser);
                                return _GenericDataResponse;
                            }
                            if (_CheckPrediction == CONSTANTS.TrainAndPredict)
                            {
                                _IngrainResponseData.Message = CONSTANTS.TrainingFailed;
                                _IngrainResponseData.CorrelationId = NewCorrelationId;
                                _IngrainResponseData.Status = CONSTANTS.ErrorMessage;
                                _IngrainResponseData.ErrorMessage = _GenericDataResponse.ErrorMessage;
                                _ = _templateInfo.AppicationID == SPE_appId ? Insert_notification(SPE_appId, result.CreatedByUser, NewCorrelationId, result.AppURL, _GenericDataResponse.Status, "100%", _GenericDataResponse.Message, _GenericDataResponse.ErrorMessage) : CallbackResponse(_IngrainResponseData, _Mapping.ApplicationName, result.AppURL, _templateInfo.ClientUID, _templateInfo.DCID, _Mapping.ApplicationID, _Mapping.UsecaseID, _requestId, null, _Mapping.CreatedByUser);
                                //CallbackResponse(_IngrainResponseData, _Mapping.ApplicationName, result.AppURL, _templateInfo.ClientUID, _templateInfo.DCID, _Mapping.ApplicationID, _Mapping.UsecaseID, _requestId, null, _Mapping.CreatedByUser);
                            }
                            if (!string.IsNullOrEmpty(_CallbackURL) & _CheckPrediction != CONSTANTS.TrainAndPredict)
                            {
                                if (_Mapping.ApplicationName == CONSTANTS.SPEAPP)
                                {
                                    _IngrainResponseData.Message = CONSTANTS.TrainingFailed;
                                    _IngrainResponseData.Status = CONSTANTS.E;
                                    _IngrainResponseData.CorrelationId = NewCorrelationId;
                                    _IngrainResponseData.ErrorMessage = _GenericDataResponse.ErrorMessage;
                                    _ = _templateInfo.AppicationID == SPE_appId ? Insert_notification(SPE_appId, result.CreatedByUser, NewCorrelationId, result.AppURL, _GenericDataResponse.Status, "100%", _GenericDataResponse.Message, _GenericDataResponse.ErrorMessage) : CallbackResponse(_IngrainResponseData, _Mapping.ApplicationName, result.AppURL, _templateInfo.ClientUID, _templateInfo.DCID, _Mapping.ApplicationID, _Mapping.UsecaseID, _requestId, null, _Mapping.CreatedByUser);
                                }
                            }
                            this.delete_backUpModelSPP(NewCorrelationId, _GenericDataResponse.Status);
                            return _GenericDataResponse;
                        }
                    }
                    else
                    {
                        _GenericDataResponse.ErrorMessage = CONSTANTS.BusinessProblem + string.Empty + CONSTANTS.DataNotAvailable;
                        _GenericDataResponse.Status = "E";
                        UpdateTrainingStatus(_GenericDataResponse.Status, _GenericDataResponse.ErrorMessage, CONSTANTS.ErrorMessage, NewCorrelationId, _templateInfo.ClientUID, _templateInfo.DCID, _Mapping.ApplicationID, _Mapping.UsecaseID, null, _Mapping.CreatedByUser);
                        if (_CheckPrediction == CONSTANTS.TrainAndPredict && (_templateInfo.AppicationID == CONSTANTS.VDSApplicationID_FDS || _templateInfo.AppicationID == CONSTANTS.VDSApplicationID_PAM))
                        {
                            var fileParams = JsonConvert.DeserializeObject<VDSInputParams>(paramArgsCustomMultipleFetch);
                            IngrainToVDSNotification VDSCallbackResponsedata = new IngrainToVDSNotification
                            {
                                CorrelationId = NewCorrelationId,
                                Status = CONSTANTS.ErrorMessage,
                                Message = CONSTANTS.TrainingFailed,
                                ErrorMessage = _GenericDataResponse.ErrorMessage,
                                ClientUId = _templateInfo.ClientUID,
                                DeliveryConstructUId = _templateInfo.DCID,
                                UseCaseId = _Mapping.UsecaseID,
                                RequestType = result.RequestType,
                                ServiceType = result.ServiceType,
                                E2EUID = fileParams.E2EUID,
                                Progress = "0%",
                                ProcessingStartTime = result.CreatedOn,
                                ProcessingEndTime = DateTime.Now.ToString(CONSTANTS.DateHoursFormat)
                            };
                            string VDScallbackResonse = VDSCallbackResponse(VDSCallbackResponsedata, _Mapping.ApplicationName, result.AppURL, _templateInfo.ClientUID, _templateInfo.DCID, _Mapping.ApplicationID, _Mapping.UsecaseID, _requestId, null, _Mapping.CreatedByUser);
                            return _GenericDataResponse;
                        }
                        if (_CheckPrediction == CONSTANTS.TrainAndPredict)
                        {
                            _IngrainResponseData.CorrelationId = NewCorrelationId;
                            _IngrainResponseData.Message = CONSTANTS.TrainingFailed;
                            _IngrainResponseData.Status = CONSTANTS.ErrorMessage;
                            _IngrainResponseData.ErrorMessage = _GenericDataResponse.ErrorMessage;
                            _ = _templateInfo.AppicationID == SPE_appId ? Insert_notification(SPE_appId, result.CreatedByUser, NewCorrelationId, result.AppURL, _GenericDataResponse.Status, "100%", _GenericDataResponse.Message, _GenericDataResponse.ErrorMessage) : CallbackResponse(_IngrainResponseData, _Mapping.ApplicationName, result.AppURL, _templateInfo.ClientUID, _templateInfo.DCID, _Mapping.ApplicationID, _Mapping.UsecaseID, _requestId, null, _Mapping.CreatedByUser);
                        }
                        if (!string.IsNullOrEmpty(_CallbackURL) & _CheckPrediction != CONSTANTS.TrainAndPredict)
                        {
                            if (_Mapping.ApplicationName == CONSTANTS.SPEAPP)
                            {
                                _IngrainResponseData.CorrelationId = NewCorrelationId;
                                _IngrainResponseData.Message = CONSTANTS.TrainingFailed;
                                _IngrainResponseData.Status = CONSTANTS.E;
                                _IngrainResponseData.ErrorMessage = _GenericDataResponse.ErrorMessage;
                                _ = _templateInfo.AppicationID == SPE_appId ? Insert_notification(SPE_appId, result.CreatedByUser, NewCorrelationId, result.AppURL, _GenericDataResponse.Status, "100%", _GenericDataResponse.Message, _GenericDataResponse.ErrorMessage) : CallbackResponse(_IngrainResponseData, _Mapping.ApplicationName, result.AppURL, _templateInfo.ClientUID, _templateInfo.DCID, _Mapping.ApplicationID, _Mapping.UsecaseID, _requestId, null, _Mapping.CreatedByUser);
                            }
                        }
                        this.delete_backUpModelSPP(NewCorrelationId, _GenericDataResponse.Status);
                    }
                }
                else
                {
                    _GenericDataResponse.ErrorMessage = CONSTANTS.TemplateNotAvailable;
                    _GenericDataResponse.Status = "E";
                    UpdateTrainingStatus(_GenericDataResponse.Status, _GenericDataResponse.ErrorMessage, CONSTANTS.ErrorMessage, NewCorrelationId, _templateInfo.ClientUID, _templateInfo.DCID, _Mapping.ApplicationID, _Mapping.UsecaseID, null, _Mapping.CreatedByUser);
                    if (_CheckPrediction == CONSTANTS.TrainAndPredict && (_templateInfo.AppicationID == CONSTANTS.VDSApplicationID_FDS || _templateInfo.AppicationID == CONSTANTS.VDSApplicationID_PAM))
                    {
                        var fileParams = JsonConvert.DeserializeObject<VDSInputParams>(paramArgsCustomMultipleFetch);
                        IngrainToVDSNotification VDSCallbackResponsedata = new IngrainToVDSNotification
                        {
                            CorrelationId = NewCorrelationId,
                            Status = CONSTANTS.ErrorMessage,
                            Message = CONSTANTS.TrainingFailed,
                            ErrorMessage = _GenericDataResponse.ErrorMessage,
                            ClientUId = _templateInfo.ClientUID,
                            DeliveryConstructUId = _templateInfo.DCID,
                            UseCaseId = _Mapping.UsecaseID,
                            RequestType = result.RequestType,
                            ServiceType = result.ServiceType,
                            E2EUID = fileParams.E2EUID,
                            Progress = "0%",
                            ProcessingStartTime = result.CreatedOn,
                            ProcessingEndTime = DateTime.Now.ToString(CONSTANTS.DateHoursFormat)
                        };
                        string VDScallbackResonse = VDSCallbackResponse(VDSCallbackResponsedata, _Mapping.ApplicationName, result.AppURL, _templateInfo.ClientUID, _templateInfo.DCID, _Mapping.ApplicationID, _Mapping.UsecaseID, _requestId, null, _Mapping.CreatedByUser);
                        return _GenericDataResponse;
                    }
                    if (_CheckPrediction == CONSTANTS.TrainAndPredict)
                    {
                        _IngrainResponseData.CorrelationId = NewCorrelationId;
                        _IngrainResponseData.Message = CONSTANTS.TrainingFailed;
                        _IngrainResponseData.Status = CONSTANTS.ErrorMessage;
                        _IngrainResponseData.ErrorMessage = _GenericDataResponse.ErrorMessage;
                        _ = _templateInfo.AppicationID == SPE_appId ? Insert_notification(SPE_appId, result.CreatedByUser, NewCorrelationId, result.AppURL, _GenericDataResponse.Status, "100%", _GenericDataResponse.Message, _GenericDataResponse.ErrorMessage) : CallbackResponse(_IngrainResponseData, _Mapping.ApplicationName, result.AppURL, _templateInfo.ClientUID, _templateInfo.DCID, _Mapping.ApplicationID, _Mapping.UsecaseID, _requestId, null, _Mapping.CreatedByUser);
                    }
                    if (!string.IsNullOrEmpty(_CallbackURL) & _CheckPrediction != CONSTANTS.TrainAndPredict)
                    {
                        if (_Mapping.ApplicationName == CONSTANTS.SPEAPP)
                        {
                            _IngrainResponseData.CorrelationId = NewCorrelationId;
                            _IngrainResponseData.Message = CONSTANTS.TrainingFailed;
                            _IngrainResponseData.Status = CONSTANTS.E;
                            _IngrainResponseData.ErrorMessage = _GenericDataResponse.ErrorMessage;
                            _ = _templateInfo.AppicationID == SPE_appId ? Insert_notification(SPE_appId, result.CreatedByUser, NewCorrelationId, result.AppURL, _GenericDataResponse.Status, "100%", _GenericDataResponse.Message, _GenericDataResponse.ErrorMessage) : CallbackResponse(_IngrainResponseData, _Mapping.ApplicationName, result.AppURL, _templateInfo.ClientUID, _templateInfo.DCID, _Mapping.ApplicationID, _Mapping.UsecaseID, _requestId, null, _Mapping.CreatedByUser);
                        }
                    }
                    this.delete_backUpModelSPP(NewCorrelationId, _GenericDataResponse.Status);
                }

            }

            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(GenericAutotrainService), nameof(PrivateModelTraining), ex.Message + "- STACKTRACE - " + ex.StackTrace + ex.InnerException, ex, _templateInfo.AppicationID, string.Empty, _templateInfo.ClientUID, _templateInfo.DCID);
                _GenericDataResponse.ErrorMessage = ex.Message;
                _GenericDataResponse.Status = CONSTANTS.E;
                _GenericDataResponse.Message = CONSTANTS.TrainingException;
                UpdateTrainingStatus(CONSTANTS.E, ex.Message, CONSTANTS.ErrorMessage, NewCorrelationId, _templateInfo.ClientUID, _templateInfo.DCID, _Mapping.ApplicationID, _Mapping.UsecaseID, ex.StackTrace, _Mapping.CreatedByUser);
                if (_CheckPrediction == CONSTANTS.TrainAndPredict && (_templateInfo.AppicationID == CONSTANTS.VDSApplicationID_FDS || _templateInfo.AppicationID == CONSTANTS.VDSApplicationID_PAM))
                {
                    var fileParams = JsonConvert.DeserializeObject<VDSInputParams>(paramArgsCustomMultipleFetch);
                    IngrainToVDSNotification VDSCallbackResponsedata = new IngrainToVDSNotification
                    {
                        CorrelationId = NewCorrelationId,
                        Status = CONSTANTS.ErrorMessage,
                        Message = CONSTANTS.TrainingFailed,
                        ErrorMessage = ex.Message,
                        ClientUId = _templateInfo.ClientUID,
                        DeliveryConstructUId = _templateInfo.DCID,
                        UseCaseId = _Mapping.UsecaseID,
                        RequestType = result.RequestType,
                        ServiceType = result.ServiceType,
                        E2EUID = fileParams.E2EUID,
                        Progress = "0%",
                        ProcessingStartTime = result.CreatedOn,
                        ProcessingEndTime = DateTime.Now.ToString(CONSTANTS.DateHoursFormat)
                    };
                    string VDScallbackResonse = VDSCallbackResponse(VDSCallbackResponsedata, _Mapping.ApplicationName, result.AppURL, _templateInfo.ClientUID, _templateInfo.DCID, _Mapping.ApplicationID, _Mapping.UsecaseID, _requestId, null, _Mapping.CreatedByUser);
                    return _GenericDataResponse;
                }
                if (_CheckPrediction == CONSTANTS.TrainAndPredict)
                {
                    _IngrainResponseData.CorrelationId = NewCorrelationId;
                    _IngrainResponseData.Status = "Error";
                    _IngrainResponseData.Message = _GenericDataResponse.Message;
                    _IngrainResponseData.ErrorMessage = ex.Message;
                    CallbackResponse(_IngrainResponseData, _Mapping.ApplicationName, _CallbackURL, _templateInfo.ClientUID, _templateInfo.DCID, _Mapping.ApplicationID, _Mapping.UsecaseID, _requestId, ex.StackTrace, _Mapping.CreatedByUser);
                }
                if (!string.IsNullOrEmpty(_CallbackURL) & _CheckPrediction != CONSTANTS.TrainAndPredict)
                {
                    if (_Mapping.ApplicationName == CONSTANTS.SPEAPP)
                    {
                        _IngrainResponseData.CorrelationId = NewCorrelationId;
                        _IngrainResponseData.Status = CONSTANTS.E;
                        _IngrainResponseData.Message = _GenericDataResponse.Message;
                        _IngrainResponseData.ErrorMessage = _GenericDataResponse.ErrorMessage;
                        CallbackResponse(_IngrainResponseData, _Mapping.ApplicationName, _CallbackURL, _templateInfo.ClientUID, _templateInfo.DCID, _Mapping.ApplicationID, _Mapping.UsecaseID, _requestId, ex.StackTrace, _Mapping.CreatedByUser);
                    }
                }
                this.delete_backUpModelSPP(NewCorrelationId, _GenericDataResponse.Status);

                CustomAppsActivityLog log = new CustomAppsActivityLog();
                log.CorrelationId = NewCorrelationId;
                log.FeatureName = "PrivateModelTraining";
                log.StackTrace = ex.StackTrace;
                log.ErrorMessage = ex.Message;
                log.ErrorMethod = "PrivateModelTraining";
                log.CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                log.CreatedBy = "SYSTEM";
                if (_templateInfo != null)
                {
                    log.ClientId = _templateInfo.ClientUID;
                    log.DCID = _templateInfo.DCID;
                    log.UseCaseId = _templateInfo.UseCaseID;
                    log.ApplicationID = _templateInfo.AppicationID;
                }

                this.InsertCustomAppsActivityLog(log);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(PrivateModelTraining), "PrivateModelTraining - Ended :", string.IsNullOrEmpty(NewCorrelationId) ? default(Guid) : new Guid(NewCorrelationId), result.AppID, string.Empty, _templateInfo.ClientUID, _templateInfo.DCID);
            return _GenericDataResponse;
        }

        private string Insert_notification(string ApplicationId, string UserId, string CorrelationId, string AppNotificationUrl, string Status, string Progress, string Message, string ErrorMessage)
        {
            try
            {
                string env = string.Empty;
                var appIntegrationCollection = _database.GetCollection<DATAMODELS.AppIntegration>(CONSTANTS.AppIntegration);
                var filter = Builders<DATAMODELS.AppIntegration>.Filter.Where(x => x.ApplicationID == ApplicationId);
                var app = appIntegrationCollection.Find(filter).FirstOrDefault();
                if (app != null)
                    env = app.Environment;
                DATAMODELS.AppNotificationLog appNotificationLog = new DATAMODELS.AppNotificationLog
                {
                    RequestId = Guid.NewGuid().ToString(),
                    ClientUId = _templateInfo.ClientUID,
                    DeliveryConstructUId = _templateInfo.DCID,
                    ApplicationId = ApplicationId,
                    CorrelationId = CorrelationId,
                    UserId = UserId,
                    AppNotificationUrl = AppNotificationUrl,
                    IsNotified = false,
                    Status = Status,
                    Progress = Progress,
                    CreatedBy = UserId,
                    CreatedOn = DateTime.UtcNow.ToString(),
                    Message = Message,
                    ErrorMessage = ErrorMessage,
                    Environment = env
                };
                var appNotificationLogCollection = _database.GetCollection<DATAMODELS.AppNotificationLog>(nameof(DATAMODELS.AppNotificationLog));
                appNotificationLogCollection.InsertOne(appNotificationLog);
            }
            catch (Exception ex)
            {
                CustomAppsActivityLog log = new CustomAppsActivityLog();
                log.CorrelationId = CorrelationId;
                log.FeatureName = "PrivateModelTraining";
                log.StackTrace = ex.StackTrace;
                log.ErrorMessage = ex.Message;
                log.ErrorMethod = "Insert_notification";
                log.CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                log.CreatedBy = "SYSTEM";
                log.ApplicationID = ApplicationId;

                this.InsertCustomAppsActivityLog(log);
            }

            return "success";
        }
        private List<PublicTemplateMapping> GetDataMapping(string UsecaseID, string AppId)
        {
            List<PublicTemplateMapping> MappingList = null;
            try
            {
                var MappingCollection = _database.GetCollection<PublicTemplateMapping>(CONSTANTS.PublicTemplateMapping);
            var filterBuilder = Builders<PublicTemplateMapping>.Filter;
            var Projection = Builders<PublicTemplateMapping>.Projection.Exclude(CONSTANTS.Id);
            //var filter = filterBuilder.Eq("UsecaseID", UsecaseID) & filterBuilder.Eq(CONSTANTS.ApplicationID, AppId);
            var filter = filterBuilder.Eq("UsecaseID", UsecaseID);
            var result = MappingCollection.Find(filter).Project<PublicTemplateMapping>(Projection).ToList();            
            if (result.Count() > 0)
            {
                if (result[0].DateColumn == null)
                {
                    result[0].DateColumn = "DateColumn";
                }
                _Mapping.InputParameters = result[0].InputParameters;
                MappingList = JsonConvert.DeserializeObject<List<PublicTemplateMapping>>(JsonConvert.SerializeObject(result));
                }
            }
            catch (Exception ex)
            {
                CustomAppsActivityLog log = new CustomAppsActivityLog();
                log.CorrelationId = NewCorrelationId;
                log.FeatureName = "PrivateModelTraining";
                log.StackTrace = ex.StackTrace;
                log.ErrorMessage = ex.Message;
                log.ErrorMethod = "GetDataMapping";
                log.CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                log.CreatedBy = "SYSTEM";
                if (_templateInfo != null)
                {
                    log.ClientId = _templateInfo.ClientUID;
                    log.DCID = _templateInfo.DCID;
                }

                this.InsertCustomAppsActivityLog(log);
            }

            return MappingList;
        }

        public void InsertCustomAppsActivityLog(CustomAppsActivityLog customAppsActivityLog)
        {
            var collection = _database.GetCollection<CustomAppsActivityLog>("CustomAppsActivityLog");
            collection.InsertOne(customAppsActivityLog);
        }

        private void UpdateTrainingStatus(string Status, string Message, string RequestStatus, string correlationId, string clientId, string DCId, string applicationId, string usecaseId, string errorTrack, string userId)
        {
            try
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(UpdateTrainingStatus), "UPDATETRAININGSTATUS - STARTED CorrelationId: " + correlationId + " ApplicationId: " + applicationId, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), applicationId, string.Empty, clientId, DCId);
                var collection = _database.GetCollection<IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
                var filterBuilder = Builders<IngrainRequestQueue>.Filter;
                var filterCorrelation = filterBuilder.Eq("RequestId", _requestId);
                var update = Builders<IngrainRequestQueue>.Update.Set("RequestStatus", RequestStatus).Set("Status", Status).Set("Message", Message);
                var isUpdated = collection.UpdateMany(filterCorrelation, update);
                _IngrainResponseData.CorrelationId = correlationId;
                _IngrainResponseData.Status = Status;
                _IngrainResponseData.Message = Message;
                _IngrainResponseData.ErrorMessage = Message;
                CallBackErrorLog(_IngrainResponseData, null, null, null, clientId, DCId, applicationId, usecaseId, _requestId, errorTrack, userId);
            }
            catch (Exception ex)
            {
                CustomAppsActivityLog log = new CustomAppsActivityLog();
                log.CorrelationId = correlationId;
                log.ClientId = clientId;
                log.DCID = DCId;
                log.UseCaseId = usecaseId;
                log.FeatureName = "PrivateModelTraining";
                log.ApplicationID = applicationId;
                log.StackTrace = ex.StackTrace;
                log.ErrorMessage = ex.Message;
                log.ErrorMethod = "UpdateTrainingStatus";
                log.CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                log.CreatedBy = "SYSTEM";

                this.InsertCustomAppsActivityLog(log);
            }
        }
        private void GetBusinessProblemData(string CorrelationId)
        {
            try
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(GetBusinessProblemData), "GetBusinessProblemData - Started :", string.IsNullOrEmpty(CorrelationId) ? default(Guid) : new Guid(CorrelationId), string.Empty, string.Empty, string.Empty, string.Empty);
                var filterBuilder = Builders<BsonDocument>.Filter;
                var BusinessProblemCollection = _database.GetCollection<BsonDocument>(CONSTANTS.PSBusinessProblem);
                var filter = filterBuilder.Eq("CorrelationId", CorrelationId);
                var Projection = Builders<BsonDocument>.Projection.Exclude("_id");
                var result = BusinessProblemCollection.Find(filter).Project<BsonDocument>(Projection).ToList();
                IsBusinessProblemDataAvailable = false;
                JObject BusinessProblem = new JObject();
                if (result.Count > 0)
                {
                    BusinessProblem = JObject.Parse(result[0].ToString());
                    var AvailableColumns = BusinessProblem["AvailableColumns"];
                    var TimeSeries = BusinessProblem["TimeSeries"];
                    var InputColumns = BusinessProblem["InputColumns"];

                    _templateInfo.CorrelationID = result[0][CONSTANTS.CorrelationId].ToString();
                    _templateInfo.BusinessProblems = result[0][CONSTANTS.BusinessProblems].ToString();
                    //_templateInfo.ClientUID = _Mapping.ClientId;
                    //_templateInfo.DCID = _Mapping.DCID;
                    _templateInfo.TimeSeries = TimeSeries;
                    _templateInfo.ProblemType = result[0]["ProblemType"].ToString();
                    _templateInfo.InputColumns = InputColumns;
                    _templateInfo.AvailableColumns = AvailableColumns;
                    _templateInfo.TargetColumn = result[0]["TargetColumn"].ToString();
                    _templateInfo.TargetUniqueIdentifier = result[0]["TargetUniqueIdentifier"].IsBsonNull ? null : result[0]["TargetUniqueIdentifier"].ToString();
                    _templateInfo.CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                    _templateInfo.UseCaseID = _Mapping.UsecaseID;
                    _templateInfo.AppicationID = _Mapping.ApplicationID;
                    _parentCorrelationId = CorrelationId;
                    IsBusinessProblemDataAvailable = true;
                }

                LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(GetBusinessProblemData), "GetBusinessProblemData - Ended :", string.IsNullOrEmpty(CorrelationId) ? default(Guid) : new Guid(CorrelationId), _templateInfo.AppicationID, string.Empty, string.Empty, string.Empty);
            }
            catch (Exception ex)
            {
                CustomAppsActivityLog log = new CustomAppsActivityLog()
                {
                    CorrelationId = CorrelationId,
                    FeatureName = "PrivateModelTraining",
                    UseCaseId = _Mapping.UsecaseID,
                    ApplicationID = _Mapping.ApplicationID,
                    StackTrace = ex.StackTrace,
                    ErrorMessage = ex.Message,
                    ErrorMethod = "GetBusinessProblemData",
                    CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                    CreatedBy = "SYSTEM",
                };

                this.InsertCustomAppsActivityLog(log);
            }
        }


        private void SourceCustomdata(string correlationId, string appName, string applicationName)
        {
            try
            {
                string entityitems = CONSTANTS.Null;
                var metricitems = CONSTANTS.Null;
                Filepath filepath = new Filepath();
                filepath.fileList = CONSTANTS.Null;
                string InstaML = CONSTANTS.Null;
                SPAInputParams inputParams = null;
                CustomPayload AppPayload = null;
                CustomSPAPayload spaPayload = null;
                VDSInputParams VDSInputParams = null;
                VDSGenericPayloads VDSGenericPayloads = null;
                if (appName != null & appName == CONSTANTS.SPAAPP)//appName is the SourceName here
                {
                    inputParams = new SPAInputParams
                    {
                        CorrelationId = correlationId,
                        StartDate = DateTime.Now.AddYears(-2).ToString(CONSTANTS.DateFormat),
                        EndDate = DateTime.Now.ToString(CONSTANTS.DateFormat),
                        TotalRecordCount = 0,
                        PageNumber = 1,
                        BatchSize = 500,
                        IterationUId = _Mapping.IterationUID != null ? _Mapping.IterationUID : CONSTANTS.Null,
                        TeamAreaUId = _Mapping.TeamAreaUId != null ? _Mapping.TeamAreaUId : CONSTANTS.Null
                    };
                    spaPayload = new CustomSPAPayload
                    {
                        AppId = _Mapping.ApplicationID,
                        UsecaseID = _Mapping.UsecaseID,
                        HttpMethod = CONSTANTS.POST,
                        AppUrl = _Mapping.SourceURL,
                        InputParameters = inputParams,
                        DateColumn = _Mapping.DateColumn
                    };
                }
                else if (appName != null & (Environment.Equals(CONSTANTS.FDSEnvironment) || Environment.Equals(CONSTANTS.PAMEnvironment)) & (appName == CONSTANTS.VDS_AIOPS))//appName is the SourceName here
                {
                    string RequestType = CONSTANTS.Null;
                    string ServiceType = CONSTANTS.Null;
                    string E2EUID = CONSTANTS.Null;
                    if (!string.IsNullOrEmpty(paramArgsCustomMultipleFetch) && paramArgsCustomMultipleFetch != CONSTANTS.Null)
                    {
                        var VDSRequestPayload = JsonConvert.DeserializeObject<VDSInputParams>(paramArgsCustomMultipleFetch);
                        RequestType = VDSRequestPayload.RequestType;
                        ServiceType = VDSRequestPayload.ServiceType;
                        E2EUID = VDSRequestPayload.E2EUID;
                    }
                    VDSInputParams = new VDSInputParams
                    {
                        //CorrelationId = correlationId,
                        ClientID = _templateInfo.ClientUID,
                        E2EUID = E2EUID,
                        DeliveryConstructID = _templateInfo.DCID,
                        Environment = Environment,
                        RequestType = RequestType,
                        ServiceType = ServiceType,
                        StartDate = DateTime.Now.AddYears(-2).ToString(CONSTANTS.DateFormat),
                        EndDate = DateTime.Now.ToString(CONSTANTS.DateFormat),
                        //TotalRecordCount = 0,
                        //PageNumber = 1,
                        //BatchSize = 500,
                    };
                    VDSGenericPayloads = new VDSGenericPayloads
                    {
                        AppId = _Mapping.ApplicationID,
                        UsecaseID = _Mapping.UsecaseID,
                        HttpMethod = CONSTANTS.POST,
                        AppUrl = _Mapping.SourceURL,
                        InputParameters = VDSInputParams,
                        AICustom = _Mapping.UsecaseID == "83814995-3ff6-45ae-a9eb-809fc4ce3dcd" ? "True" : "False"
                    };
                }
                else
                {
                    AppPayload = new CustomPayload
                    {
                        AppId = _Mapping.ApplicationID,
                        UsecaseID = _Mapping.UsecaseID,
                        HttpMethod = CONSTANTS.POST,
                        AppUrl = _Mapping.SourceURL,
                        InputParameters = _Mapping.InputParameters,
                        DateColumn = _Mapping.DateColumn
                    };
                }

                ParentFile parentFile = new ParentFile();
                parentFile.Name = CONSTANTS.Null;
                parentFile.Type = CONSTANTS.Null;
                if (appName != null & appName == CONSTANTS.SPAAPP)
                {
                    CustomSPAFileUpload fileUpload = new CustomSPAFileUpload
                    {
                        CorrelationId = correlationId,
                        ClientUID = _templateInfo.ClientUID,
                        DeliveryConstructUId = _templateInfo.DCID,
                        Parent = parentFile,
                        Flag = CONSTANTS.Null,
                        mapping = CONSTANTS.Null,
                        mapping_flag = CONSTANTS.False,
                        pad = entityitems,
                        metric = metricitems,
                        InstaMl = InstaML,
                        fileupload = filepath,
                        Customdetails = spaPayload,
                    };
                    _templateInfo.ParamArgs = fileUpload.ToJson();
                }
                else if (appName != null & (Environment.Equals(CONSTANTS.FDSEnvironment) || Environment.Equals(CONSTANTS.PAMEnvironment)) & appName == CONSTANTS.VDS_AIOPS)
                {
                    VDSGenericParamArgs fileUpload = new VDSGenericParamArgs
                    {
                        CorrelationId = correlationId,
                        ClientUID = _templateInfo.ClientUID,
                        DeliveryConstructUId = _templateInfo.DCID,
                        Parent = parentFile,
                        Flag = CONSTANTS.Null,
                        mapping = CONSTANTS.Null,
                        mapping_flag = CONSTANTS.False,
                        pad = entityitems,
                        metric = metricitems,
                        InstaMl = InstaML,
                        fileupload = filepath,
                        Customdetails = VDSGenericPayloads,
                    };
                    _templateInfo.ParamArgs = fileUpload.ToJson();
                }
                else
                {
                    CustomFileUpload fileUpload = new CustomFileUpload
                    {
                        CorrelationId = correlationId,
                        ClientUID = _templateInfo.ClientUID,
                        DeliveryConstructUId = _templateInfo.DCID,
                        Parent = parentFile,
                        Flag = CONSTANTS.Null,
                        mapping = CONSTANTS.Null,
                        mapping_flag = CONSTANTS.False,
                        pad = entityitems,
                        metric = metricitems,
                        InstaMl = InstaML,
                        fileupload = filepath,
                        Customdetails = AppPayload,
                    };
                    _templateInfo.ParamArgs = fileUpload.ToJson();
                }
            }
            catch (Exception ex)
            {
                CustomAppsActivityLog log = new CustomAppsActivityLog();
                log.CorrelationId = correlationId;
                log.FeatureName = "PrivateModelTraining";
                log.StackTrace = ex.StackTrace;
                log.ErrorMessage = ex.Message;
                log.ErrorMethod = "SourceCustomdata";
                log.CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                log.CreatedBy = "SYSTEM";
                if (_templateInfo != null)
                {
                    log.ClientId = _templateInfo.ClientUID;
                    log.DCID = _templateInfo.DCID;
                    log.UseCaseId = _templateInfo.UseCaseID;
                    log.ApplicationID = _templateInfo.AppicationID;
                }

                this.InsertCustomAppsActivityLog(log);
            }
        }

        private bool GetIngrainRequestData(string oldCorrelationId, string NewCorrelationId, string appName, string clientId, string dcId)
        {
            bool status = false;
            try
            {
                var filterBuilder = Builders<BsonDocument>.Filter;
                var IngrainRequestCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
                var IngrainRequestFilter = filterBuilder.Eq("CorrelationId", oldCorrelationId) & filterBuilder.Eq("pageInfo", CONSTANTS.IngestData);
                var Projection = Builders<BsonDocument>.Projection.Include("ParamArgs").Exclude("_id");
                var requestdata = IngrainRequestCollection.Find(IngrainRequestFilter).Project<BsonDocument>(Projection).ToList();
                if (requestdata.Count > 0)
                {
                    ParamArgs = requestdata[0]["ParamArgs"].ToString();
                    var fileParams = JsonConvert.DeserializeObject<FileUpload>(ParamArgs);
                    fileParams.CorrelationId = NewCorrelationId;
                    fileParams.ClientUID = clientId;
                    fileParams.DeliveryConstructUId = dcId;
                    if (!string.IsNullOrEmpty(fileParams.pad) && fileParams.pad != CONSTANTS.Null)
                    {
                        JObject pad = JObject.Parse(fileParams.pad);
                        pad["startDate"] = DateTime.UtcNow.AddYears(-2).ToString("MM/dd/yyyy");
                        pad["endDate"] = DateTime.UtcNow.ToString("MM/dd/yyyy");
                        fileParams.pad = pad.ToString(Formatting.None);
                    }
                    if (!string.IsNullOrEmpty(fileParams.metric) && fileParams.metric != CONSTANTS.Null)
                    {
                        JObject metric = JObject.Parse(fileParams.metric);
                        metric["startDate"] = DateTime.UtcNow.AddYears(-2).ToString("MM/dd/yyyy");
                        metric["endDate"] = DateTime.UtcNow.ToString("MM/dd/yyyy");
                        fileParams.metric = metric.ToString(Formatting.None);
                    }


                    if (fileParams.Flag == "AutoRetrain")
                    {
                        fileParams.Flag = CONSTANTS.Null;
                    }
                    if (appName == CONSTANTS.SPAAPP && fileParams.fileupload != null)
                        fileParams.fileupload.fileList = CONSTANTS.Null;

                    _templateInfo.ParamArgs = fileParams.ToJson();
                    status = true;
                }
                else
                {
                    _GenericDataResponse.Message = "Public Template ParamArgs Details is Null";
                    _GenericDataResponse.Status = "E";
                    status = false;

                }
            }
            catch (Exception ex)
            {
                CustomAppsActivityLog log = new CustomAppsActivityLog()
                {
                    CorrelationId = NewCorrelationId,
                    ClientId = clientId,
                    DCID = dcId,
                    FeatureName = "PrivateModelTraining",
                    ApplicationName = appName,
                    StackTrace = ex.StackTrace,
                    ErrorMessage = ex.Message + " ,OldCorrelationId: " + oldCorrelationId,
                    ErrorMethod = "GetIngrainRequestData",
                    CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                    CreatedBy = "SYSTEM",
                };

                this.InsertCustomAppsActivityLog(log);
            }

            return status;
        }

        private void InsertBusinessProblem(GenericTemplatemapping templateInfo, string CorrelationId)
        {
            try
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(InsertBusinessProblem), "Data Insertion started for Bussisnes Problem for corelation ID --" + CorrelationId, string.IsNullOrEmpty(CorrelationId) ? default(Guid) : new Guid(CorrelationId), templateInfo.AppicationID, string.Empty, templateInfo.ClientUID, templateInfo.DCID);
            BusinessProblemInstaDTO businessProblemData = new BusinessProblemInstaDTO
            {
                BusinessProblems = templateInfo.BusinessProblems,
                TargetColumn = templateInfo.TargetColumn,
                InputColumns = templateInfo.InputColumns,
                AvailableColumns = templateInfo.AvailableColumns,
                TargetUniqueIdentifier = _templateInfo.TargetUniqueIdentifier,
                CorrelationId = CorrelationId,
                TimeSeries = templateInfo.TimeSeries,
                ProblemType = templateInfo.ProblemType,
                CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                CreatedByUser = _Mapping.CreatedByUser,
                ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                ModifiedByUser = _Mapping.CreatedByUser,
                _id = Guid.NewGuid().ToString(),
                TemplateUsecaseID = templateInfo.UseCaseID,
                ClientUId = templateInfo.ClientUID,
                DeliveryConstructUID = templateInfo.DCID,
                AppId = templateInfo.AppicationID
            };

            //SPA Velocity training added CustomDays value to 0.
            if (templateInfo.pageInfo == CONSTANTS.TrainAndPredict)
            {
                if (templateInfo.ProblemType == CONSTANTS.TimeSeries)
                {
                    var timeSeriesObject = JObject.Parse(templateInfo.TimeSeries.ToString());
                    JObject frequency = JObject.FromObject(timeSeriesObject[CONSTANTS.Frequency]);
                    foreach (var item in timeSeriesObject[CONSTANTS.Frequency].Children())
                    {
                        JProperty prop = item as JProperty;
                        if (prop != null)
                        {
                            if (prop.Name == "8")
                            {
                                JObject data = (JObject)Newtonsoft.Json.JsonConvert.DeserializeObject(prop.Value.ToString());
                                JObject customDays = new JObject
                            {
                            { "Name", "CustomDays" },
                            { "Steps", data["Steps"].ToString()},
                            { "value", "0" }
                            };
                                frequency.Property("8").Remove();
                                frequency.Add("8", JObject.FromObject(customDays));
                                timeSeriesObject.Property("Frequency").Remove();
                                timeSeriesObject.Add("Frequency", JObject.FromObject(frequency));
                                timeSeriesObject["Aggregation"] = "None";
                                businessProblemData.TimeSeries = timeSeriesObject;
                            }

                        }
                    }
                }
            }


            var jsonColumns = JsonConvert.SerializeObject(businessProblemData);
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.PSBusinessProblem);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, CorrelationId);
            collection.DeleteMany(filter);
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(InsertBusinessProblem), "Record Deleted from Bussisnes Problem for corelation ID -- " + CorrelationId, string.IsNullOrEmpty(CorrelationId) ? default(Guid) : new Guid(CorrelationId), templateInfo.AppicationID, string.Empty, templateInfo.ClientUID, templateInfo.DCID);
            Thread.Sleep(2000);
            var insertBsonColumns = BsonSerializer.Deserialize<BsonDocument>(jsonColumns);
            collection.InsertOne(insertBsonColumns);
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(InsertBusinessProblem), "New Record Inserted from Bussisnes Problem for corelation ID -- " + businessProblemData.CorrelationId, string.IsNullOrEmpty(CorrelationId) ? default(Guid) : new Guid(CorrelationId), templateInfo.AppicationID, string.Empty, templateInfo.ClientUID, templateInfo.DCID);
            }
            catch (Exception ex)
            {
                CustomAppsActivityLog log = new CustomAppsActivityLog();
                log.CorrelationId = CorrelationId;
                log.FeatureName = "PrivateModelTraining";
                log.StackTrace = ex.StackTrace;
                log.ErrorMessage = ex.Message;
                log.ErrorMethod = "InsertBusinessProblem";
                log.CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                log.CreatedBy = "SYSTEM";
                if (_templateInfo != null)
                {
                    log.ClientId = _templateInfo.ClientUID;
                    log.DCID = _templateInfo.DCID;
                    log.UseCaseId = _templateInfo.UseCaseID;
                    log.ApplicationID = _templateInfo.AppicationID;
                }

                this.InsertCustomAppsActivityLog(log);
            }
        }

        public DeployModelsDto GetDeployModelDetails(string correlationId)
        {
            var modelCollection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
            var filter = Builders<DeployModelsDto>.Filter.Where(x => x.CorrelationId == correlationId);
            var projection = Builders<DeployModelsDto>.Projection.Exclude(x => x.CorrelationId).Exclude(x => x._id);
            var result = modelCollection.Find(filter).Project<DeployModelsDto>(projection).FirstOrDefault();
            return result != null ? result : null;
        }

        private void IngestDataInsertRequests(string correlationId, string clientUId, string deliveryConstructUID, string problemType, string targetColumn, string AppicationID, string ParamArgs, string dataSetUId)
        {
            IngestDataInsertRequests(correlationId, clientUId, deliveryConstructUID, problemType, targetColumn, AppicationID, ParamArgs, dataSetUId, string.Empty);
        }

        private void IngestDataInsertRequests(string correlationId, string clientUId, string deliveryConstructUID, string problemType, string targetColumn, string AppicationID, string ParamArgs, string dataSetUId, string FunctionName)
        {
            try
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(IngestDataInsertRequests), "Data Insertion started for Ingest Data for corelation ID --" + correlationId, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), AppicationID, string.Empty, clientUId, deliveryConstructUID);
                DeployModelsDto deployedModel = GetDeployModelDetails(_Mapping.UsecaseID);
                dataSetUId = string.IsNullOrEmpty(dataSetUId) ? null : dataSetUId;

                bool _bForAutoTrain = false;
                string _RequestId = Guid.NewGuid().ToString();

                if (FunctionName == CONSTANTS.AutoTrain)
                {
                    _bForAutoTrain = true;
                    _RequestId = _requestId;
                }

                IngrainRequestQueue ingrainRequest = new IngrainRequestQueue
                {
                    _id = Guid.NewGuid().ToString(),
                    DataSetUId = dataSetUId,
                    CorrelationId = correlationId,
                    //RequestId = _RequestId,
                    RequestId = Guid.NewGuid().ToString(),
                    ProcessId = CONSTANTS.Null,
                    Status = CONSTANTS.Null,
                    ModelName = _templateInfo.ModelName,
                    RequestStatus = CONSTANTS.New,
                    RetryCount = 0,
                    ProblemType = problemType,
                    Message = CONSTANTS.Null,
                    UniId = CONSTANTS.Null,
                    InstaID = CONSTANTS.Null,
                    Progress = CONSTANTS.Null,
                    pageInfo = CONSTANTS.IngestData,
                    ParamArgs = ParamArgs,
                    TemplateUseCaseID = _Mapping.UsecaseID,
                    Function = CONSTANTS.FileUpload,
                    CreatedByUser = _Mapping.CreatedByUser,
                    CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                    ModifiedByUser = _Mapping.CreatedByUser,
                    ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                    LastProcessedOn = CONSTANTS.Null,
                    AppID = AppicationID,
                    ClientId = _templateInfo.ClientUID,
                    DeliveryconstructId = _templateInfo.DCID,
                    IsForAutoTrain = _bForAutoTrain,
                    AutoTrainRequestId = _RequestId
                };
                InsertRequests(ingrainRequest);
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(IngestDataInsertRequests), "Data Insertion Completed for Ingest Data for corelation ID --" + correlationId + " and Request ID: - " + ingrainRequest.RequestId, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), AppicationID, string.Empty, clientUId, deliveryConstructUID);
            }
            catch (Exception ex)
            {
                CustomAppsActivityLog log = new CustomAppsActivityLog();
                log.CorrelationId = correlationId;
                log.FeatureName = "PrivateModelTraining";
                log.StackTrace = ex.StackTrace;
                log.ErrorMessage = ex.Message;
                log.ErrorMethod = "IngestDataInsertRequests";
                log.CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                log.CreatedBy = "SYSTEM";
                if (_templateInfo != null)
                {
                    log.ClientId = _templateInfo.ClientUID;
                    log.DCID = _templateInfo.DCID;
                    log.UseCaseId = _templateInfo.UseCaseID;
                    log.ApplicationID = _templateInfo.AppicationID;
                }

                this.InsertCustomAppsActivityLog(log);
            }
        }

        private void InsertCustomConstraintsRequests(string NewcorrelationId, string UseCaseID)
        {
            try
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(InsertCustomConstraintsRequests), "Data Insertion started for Custom Constraints for correlation ID --" + NewcorrelationId, string.IsNullOrEmpty(NewcorrelationId) ? default(Guid) : new Guid(NewcorrelationId), "", string.Empty, "", "");
                var collection = _database.GetCollection<SSAICustomConfiguration>(CONSTANTS.SSAICustomContraints);
                var builder = Builders<SSAICustomConfiguration>.Filter;
                var filter = builder.Eq("CorrelationID", UseCaseID);
                var Projection = Builders<SSAICustomConfiguration>.Projection.Exclude("_id");
                var customConstraintResponse = collection.Find(filter).Project<SSAICustomConfiguration>(Projection).ToList();

                if (customConstraintResponse != null && customConstraintResponse.Count > 0)
                {
                    // Insert a New document
                    SSAICustomConfiguration oConfigurtion = new SSAICustomConfiguration
                    {
                        _id = Guid.NewGuid().ToString(),
                        CorrelationID = NewcorrelationId,
                        UseCaseID = UseCaseID,
                        ApplicationID = customConstraintResponse[0].ApplicationID,
                        ModelVersion = customConstraintResponse[0].ModelVersion,
                        ModelType = customConstraintResponse[0].ModelType,
                        IsTrainingEnabled = customConstraintResponse[0].IsTrainingEnabled,
                        Training = customConstraintResponse[0].Training,
                        IsPredictionEnabled = customConstraintResponse[0].IsPredictionEnabled,
                        Prediction = customConstraintResponse[0].Prediction,
                        IsRetrainingEnabled = customConstraintResponse[0].IsRetrainingEnabled,
                        Retraining = customConstraintResponse[0].Retraining,
                        CreatedOn = customConstraintResponse[0].ApplicationID,
                        CreatedByUser = customConstraintResponse[0].ApplicationID
                    };

                    var jsonData = JsonConvert.SerializeObject(oConfigurtion);
                    var insertDocument = BsonSerializer.Deserialize<SSAICustomConfiguration>(jsonData);
                    collection.InsertOne(insertDocument);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(InsertCustomConstraintsRequests), "Data Insertion Completed for Ingest Data for corelation ID --" + NewcorrelationId, string.IsNullOrEmpty(NewcorrelationId) ? default(Guid) : new Guid(NewcorrelationId), "", string.Empty, "", "");
                }
            }
            catch (Exception ex)
            {
                CustomAppsActivityLog log = new CustomAppsActivityLog();
                log.CorrelationId = NewcorrelationId;
                log.FeatureName = "PrivateModelTraining";
                log.StackTrace = ex.StackTrace;
                log.ErrorMessage = ex.Message;
                log.ErrorMethod = "IngestDataInsertRequests";
                log.CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                log.CreatedBy = "SYSTEM";
                log.UseCaseId = UseCaseID;

                this.InsertCustomAppsActivityLog(log);
            }
        }

        private void InsertRequests(IngrainRequestQueue ingrainRequest)
        {
            var requestQueue = JsonConvert.SerializeObject(ingrainRequest);
            var insertRequestQueue = BsonSerializer.Deserialize<BsonDocument>(requestQueue);
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
            collection.InsertOne(insertRequestQueue);
        }
        private void InsertFMRequests(FMVisualization fmData)
        {
            var requestQueue = JsonConvert.SerializeObject(fmData);
            var insertRequestQueue = BsonSerializer.Deserialize<BsonDocument>(requestQueue);
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIFMVisualization);
            collection.InsertOne(insertRequestQueue);
        }

        private void ValidatingIngestDataCompletion(string ModelCorrelationID, GenericTemplatemapping templateinfo)
        {
            bool isdataIngested = true;
            List<IngrainRequestQueue> result = new List<IngrainRequestQueue>();
            while (isdataIngested)
            {
                try
                {
                    result = this.GetMultipleRequestStatus(ModelCorrelationID, CONSTANTS.IngestData);
                    if (result.Count > 0)
                    {
                        _GenericDataResponse.Status = result[0].Status;
                        _GenericDataResponse.Message = result[0].Message;
                        switch (result[0].Status)
                        {
                            case "C":
                                if (result[0].Progress == CONSTANTS.Hundred)
                                {
                                    bool datamatched = ValidateIngestedDataWithTemplates(ModelCorrelationID, templateinfo);
                                    if (datamatched)
                                    {
                                        UpdateRequestStatus("25%");
                                        _GenericDataResponse.Message = CONSTANTS.IngestDataSuccess;
                                    }
                                    isdataIngested = false;
                                }
                                break;

                            case "E":
                                _GenericDataResponse.Status = CONSTANTS.E;
                                _GenericDataResponse.ErrorMessage = result[0].Message;
                                isdataIngested = false;
                                UpdateTrainingStatus(_GenericDataResponse.Status, _GenericDataResponse.ErrorMessage, CONSTANTS.ErrorMessage, ModelCorrelationID, templateinfo.ClientUID, templateinfo.DCID, _Mapping.ApplicationID, _Mapping.UsecaseID, null, _Mapping.CreatedByUser);
                                break;

                            default:
                                Thread.Sleep(2000);
                                isdataIngested = true;
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _GenericDataResponse.Status = CONSTANTS.E;
                    _GenericDataResponse.ErrorMessage = result[0].Message;
                    isdataIngested = false;
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(ValidatingIngestDataCompletion) + _GenericDataResponse.ErrorMessage, ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty);
                    UpdateTrainingStatus(_GenericDataResponse.Status, _GenericDataResponse.ErrorMessage, CONSTANTS.ErrorMessage, ModelCorrelationID, templateinfo.ClientUID, templateinfo.DCID, _Mapping.ApplicationID, _Mapping.UsecaseID, null, _Mapping.CreatedByUser);

                    CustomAppsActivityLog log = new CustomAppsActivityLog();
                    log.CorrelationId = ModelCorrelationID;
                    log.FeatureName = "PrivateModelTraining";
                    log.StackTrace = ex.StackTrace;
                    log.ErrorMessage = ex.Message;
                    log.ErrorMethod = "ValidatingIngestDataCompletion";
                    log.CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                    log.CreatedBy = "SYSTEM";
                    if (templateinfo != null)
                    {
                        log.ClientId = templateinfo.ClientUID;
                        log.DCID = templateinfo.DCID;
                        log.UseCaseId = templateinfo.UseCaseID;
                        log.ApplicationID = templateinfo.AppicationID;
                    }

                    this.InsertCustomAppsActivityLog(log);
                }
            }
        }

        private List<IngrainRequestQueue> GetMultipleRequestStatus(string correlationId, string pageInfo)
        {
            List<IngrainRequestQueue> ingrainRequest = new List<IngrainRequestQueue>();
            var collection = _database.GetCollection<IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
            var builder = Builders<IngrainRequestQueue>.Filter;
            var filter = builder.Eq(CONSTANTS.pageInfo, pageInfo) & builder.Eq(CONSTANTS.CorrelationId, correlationId);
            return ingrainRequest = collection.Find(filter).ToList();

        }

        private void UpdateRequestStatus(string progressPercentage)
        {
            try
            {
                var collection = _database.GetCollection<IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
                var filterBuilder = Builders<IngrainRequestQueue>.Filter;
                var filterCorrelation = filterBuilder.Eq("RequestId", _requestId);

                if (_templateInfo.Function == CONSTANTS.AutoTrain)
                {
                    filterCorrelation = filterBuilder.Eq("RequestId", _requestId) & filterBuilder.Eq(CONSTANTS.Function, CONSTANTS.AutoTrain);
                }

                if (progressPercentage == "100%")
                {
                    var update = Builders<IngrainRequestQueue>.Update.Set("Progress", progressPercentage).Set(CONSTANTS.Status, CONSTANTS.C);
                    var isUpdated = collection.UpdateMany(filterCorrelation, update);
                }
                else
                {
                    var update = Builders<IngrainRequestQueue>.Update.Set("Progress", progressPercentage).Set(CONSTANTS.Status, CONSTANTS.P);
                    var isUpdated = collection.UpdateMany(filterCorrelation, update);
                }
            }
            catch (Exception ex)
            {
                CustomAppsActivityLog log = new CustomAppsActivityLog();
                log.FeatureName = "PrivateModelTraining";
                log.StackTrace = ex.StackTrace;
                log.ErrorMessage = ex.Message;
                log.ErrorMethod = "UpdateRequestStatus";
                log.CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                log.CreatedBy = "SYSTEM";
                log.RequestId = _requestId;

                this.InsertCustomAppsActivityLog(log);
            }
        }

        private void CreateInstaModel(string problemType, string correlationId, string dataSetUId)
        {
            try
            {
                var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);

                string Language = getlanguage(_templateInfo.ClientUID, _templateInfo.DCID);
                var filterBuilder = Builders<BsonDocument>.Filter;
                var projection1 = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
                var Sourcenamefilter = filterBuilder.Eq(CONSTANTS.CorrelationId, _Mapping.UsecaseID);
                var SourceData = collection.Find(Sourcenamefilter).Project<BsonDocument>(projection1).ToList();

                var AppIntegCollection = _database.GetCollection<AppIntegration>(CONSTANTS.AppIntegration);
                var filterBuilder1 = Builders<AppIntegration>.Filter;
                var AppFilter = filterBuilder1.Eq("ApplicationName", _Mapping.ApplicationName);

                var Projection = Builders<AppIntegration>.Projection.Include("BaseURL").Exclude("_id");
                var AppData = AppIntegCollection.Find(AppFilter).Project<AppIntegration>(Projection).FirstOrDefault();
                if (AppData != null)
                {
                    string[] applicationName = new string[] { CONSTANTS.VDS_SI, CONSTANTS.VDS, CONSTANTS.VDS_AIOPS };
                    if (!applicationName.Contains(_Mapping.ApplicationName))
                    {
                        AppData.BaseURL = null;
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(AppData.BaseURL))
                        {
                            var myWizardAPIUrl = appSettings.GetSection("AppSettings").GetSection("VDSLink").Value;
                            Uri apiUri = new Uri(myWizardAPIUrl);
                            string host = apiUri.GetLeftPart(UriPartial.Authority);
                            Uri apiUri1 = new Uri(AppData.BaseURL);
                            string apiPath = apiUri1.AbsolutePath;
                            AppData.BaseURL = host + apiPath;
                        }
                        else
                        {
                            AppData.BaseURL = appSettings.GetSection("AppSettings").GetSection("VDSLink").Value;
                        }
                    }
                }
                BsonElement element;
                var exists = SourceData[0].TryGetElement("DBEncryptionRequired", out element);
                //if (exists)
                //    _DBEncryptionRequired = (bool)SourceData[0]["DBEncryptionRequired"];
                //else
                //    _DBEncryptionRequired = false;
                if (exists)
                    _TemplateDBEncryptionRequired = (bool)SourceData[0]["DBEncryptionRequired"];
                else
                    _TemplateDBEncryptionRequired = false;

                if (_isForAllData)
                {
                    _DBEncryptionRequired = true;
                }
                else
                {
                    _DBEncryptionRequired = _TemplateDBEncryptionRequired;
                }
                dataSetUId = string.IsNullOrEmpty(dataSetUId) ? null : dataSetUId;
                string category = string.Empty;
                if (VDSUseCaseIds.Contains(_Mapping.UsecaseID))
                {
                    if (!string.IsNullOrEmpty(paramArgsCustomMultipleFetch) && paramArgsCustomMultipleFetch != CONSTANTS.Null)
                    {
                        var VDSRequestPayload = JsonConvert.DeserializeObject<VDSInputParams>(paramArgsCustomMultipleFetch);
                        category = VDSRequestPayload.RequestType;
                    }
                }

                string[] arr = new string[] { };
                PublishModelFrequency frequency = new PublishModelFrequency();
                for (int i = 0; i < SourceData.Count; i++)
                {
                    DeployModelsDto deployModel = new DeployModelsDto
                    {
                        _id = Guid.NewGuid().ToString(),
                        CorrelationId = correlationId,
                        ModelName = _templateInfo.ModelName,
                        Category = VDSUseCaseIds.Contains(_Mapping.UsecaseID) ? category : SourceData[i][CONSTANTS.Category].ToString(),
                        Status = CONSTANTS.InProgress,
                        ClientUId = _templateInfo.ClientUID,
                        DeliveryConstructUID = _templateInfo.DCID,
                        DataSource = _Mapping.SourceName,
                        DeployedDate = null,
                        LinkedApps = arr,
                        TemplateUsecaseId = _templateInfo.UseCaseID,
                        ModelVersion = null,
                        ModelType = _templateInfo.ProblemType,
                        VDSLink = AppData.BaseURL,
                        DataSetUId = dataSetUId,
                        InputSample = null,
                        IsIncludedInCascade = false,
                        IsPrivate = true,
                        IsModelTemplate = false,
                        DBEncryptionRequired = _DBEncryptionRequired,
                        TrainedModelId = null,
                        Frequency = SourceData[i]["Frequency"].ToString(),
                        CreatedByUser = _Mapping.CreatedByUser,
                        CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                        ModifiedByUser = _Mapping.CreatedByUser,
                        ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                        AppId = _templateInfo.AppicationID,
                        Language = Language,
                        IsCarryOutRetraining = false,
                        IsOffline = false,
                        IsOnline = false,
                        MaxDataPull = SourceData[i].Contains(CONSTANTS.MaxDataPull) ? Convert.ToInt32(SourceData[i][CONSTANTS.MaxDataPull]) : 0,
                        Retraining = frequency,
                        Training = frequency,
                        Prediction = frequency,
                    };
                    if (_isGenericModel)
                    {
                        if (Convert.ToBoolean(SourceData[i][CONSTANTS.IsCarryOutRetraining])) //&& !Convert.ToBoolean(SourceData[i][CONSTANTS.IsOffline]))
                        {
                            deployModel.IsCarryOutRetraining = Convert.ToBoolean(SourceData[i][CONSTANTS.IsCarryOutRetraining]);
                            deployModel.IsOffline = Convert.ToBoolean(SourceData[i][CONSTANTS.IsOffline]);
                            deployModel.IsOnline = Convert.ToBoolean(SourceData[i][CONSTANTS.IsOnline]);
                            deployModel.Retraining = BsonSerializer.Deserialize<PublishModelFrequency>(SourceData[i][CONSTANTS.Retraining].ToJson());
                            deployModel.RetrainingFrequencyInDays = Convert.ToInt32(SourceData[i][CONSTANTS.RetrainingFrequencyInDays]);
                        }
                    }

                    if (Convert.ToBoolean(SourceData[i][CONSTANTS.IsOffline]))
                    {
                        deployModel.Training = BsonSerializer.Deserialize<PublishModelFrequency>(SourceData[i][CONSTANTS.Training].ToJson());
                        deployModel.TrainingFrequencyInDays = Convert.ToInt32(SourceData[i][CONSTANTS.TrainingFrequencyInDays]);
                        deployModel.Prediction = BsonSerializer.Deserialize<PublishModelFrequency>(SourceData[i][CONSTANTS.Prediction].ToJson());
                        deployModel.PredictionFrequencyInDays = Convert.ToInt32(SourceData[i][CONSTANTS.PredictionFrequencyInDays]);
                    }
                    if (_templateInfo.pageInfo == CONSTANTS.TrainAndPredict)
                    {
                        if (_Mapping.SourceName == CONSTANTS.VDS_AIOPS)
                        {
                            deployModel.SourceName = _Mapping.SourceName;
                            deployModel.DataSource = _Mapping.SourceName;
                        }
                        else
                        {
                            deployModel.SourceName = CONSTANTS.SPAAPP;
                            deployModel.DataSource = CONSTANTS.SPAAPP;
                        }
                    }
                    else
                    {
                        if (SourceData[0]["SourceName"] == "pad" | SourceData[0]["SourceName"] == "metric")
                        {
                            deployModel.SourceName = SourceData[0]["SourceName"].ToString();
                        }
                        else
                        {
                            deployModel.SourceName = "Custom";
                        }
                    }
                    _Frequency.Add(SourceData[i]["Frequency"].ToString());
                    var jsonColumns = JsonConvert.SerializeObject(deployModel);
                    collection.DeleteOne(Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId));
                    var insertBsonColumns = BsonSerializer.Deserialize<BsonDocument>(jsonColumns);
                    collection.InsertOne(insertBsonColumns);
                }
            }
            catch (Exception ex)
            {
                CustomAppsActivityLog log = new CustomAppsActivityLog();
                log.CorrelationId = correlationId;
                log.FeatureName = "PrivateModelTraining";
                log.StackTrace = ex.StackTrace;
                log.ErrorMessage = ex.Message;
                log.ErrorMethod = "CreateInstaModel";
                log.CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                log.CreatedBy = "SYSTEM";
                if (_templateInfo != null)
                {
                    log.ClientId = _templateInfo.ClientUID;
                    log.DCID = _templateInfo.DCID;
                    log.UseCaseId = _templateInfo.UseCaseID;
                    log.ApplicationID = _templateInfo.AppicationID;
                }

                this.InsertCustomAppsActivityLog(log);
            }
        }
        private void CreateCascadeInstaModel(string problemType, string correlationId)
        {
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);

            string Language = getlanguage(_templateInfo.ClientUID, _templateInfo.DCID);
            var filterBuilder = Builders<BsonDocument>.Filter;
            var projection1 = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
            var Sourcenamefilter = filterBuilder.Eq(CONSTANTS.CorrelationId, _Mapping.UsecaseID);
            var SourceData = collection.Find(Sourcenamefilter).Project<BsonDocument>(projection1).ToList();

            var AppIntegCollection = _database.GetCollection<AppIntegration>(CONSTANTS.AppIntegration);
            var filterBuilder1 = Builders<AppIntegration>.Filter;
            var AppFilter = filterBuilder1.Eq("ApplicationName", _Mapping.ApplicationName);

            var Projection = Builders<AppIntegration>.Projection.Include("BaseURL").Exclude("_id");
            var AppData = AppIntegCollection.Find(AppFilter).Project<AppIntegration>(Projection).FirstOrDefault();

            BsonElement element;
            var exists = SourceData[0].TryGetElement("DBEncryptionRequired", out element);
            //if (exists)
            //    _DBEncryptionRequired = (bool)SourceData[0]["DBEncryptionRequired"];
            //else
            //    _DBEncryptionRequired = false;
            if (exists)
                _TemplateDBEncryptionRequired = (bool)SourceData[0]["DBEncryptionRequired"];
            else
                _TemplateDBEncryptionRequired = false;
            if (_isForAllData)
            {
                _DBEncryptionRequired = true;
            }
            else
            {
                _DBEncryptionRequired = _TemplateDBEncryptionRequired;
            }
            string[] arr = new string[] { };
            for (int i = 0; i < SourceData.Count; i++)
            {
                DeployModelsDto deployModel = new DeployModelsDto
                {
                    _id = Guid.NewGuid().ToString(),
                    CorrelationId = correlationId,
                    ModelName = _templateInfo.ModelName,
                    Category = SourceData[i][CONSTANTS.Category].ToString(),
                    Status = CONSTANTS.InProgress,
                    ClientUId = _templateInfo.ClientUID,
                    DeliveryConstructUID = _templateInfo.DCID,
                    DataSource = _Mapping.SourceName,
                    DeployedDate = null,
                    LinkedApps = arr,
                    TemplateUsecaseId = _templateInfo.UseCaseID,
                    ModelVersion = null,
                    ModelType = _templateInfo.ProblemType,
                    VDSLink = AppData.BaseURL,
                    InputSample = null,
                    IsIncludedInCascade = true,
                    IsPrivate = true,
                    IsModelTemplate = false,
                    DBEncryptionRequired = _DBEncryptionRequired,
                    TrainedModelId = null,
                    Frequency = SourceData[i]["Frequency"].ToString(),
                    CreatedByUser = _Mapping.CreatedByUser,
                    CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                    ModifiedByUser = _Mapping.CreatedByUser,
                    ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                    AppId = _templateInfo.AppicationID,
                    Language = Language

                };
                if (_templateInfo.pageInfo == CONSTANTS.TrainAndPredict)
                {
                    deployModel.SourceName = CONSTANTS.SPAAPP;
                    deployModel.DataSource = CONSTANTS.SPAAPP;
                }
                else
                {
                    if (SourceData[0]["SourceName"] == "pad" | SourceData[0]["SourceName"] == "metric")
                    {
                        deployModel.SourceName = SourceData[0]["SourceName"].ToString();
                    }
                    else
                    {
                        deployModel.SourceName = "Custom";
                    }
                }
                _Frequency.Add(SourceData[i]["Frequency"].ToString());
                var jsonColumns = JsonConvert.SerializeObject(deployModel);
                collection.DeleteOne(Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId));
                var insertBsonColumns = BsonSerializer.Deserialize<BsonDocument>(jsonColumns);
                collection.InsertOne(insertBsonColumns);
            }
        }
        public string getlanguage(string ClientUID, string DCID)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutoTrain), nameof(getlanguage), "getlanguage - Started :", string.Empty, string.Empty, ClientUID, DCID);
            var token = getToken();
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutoTrain), nameof(getlanguage), "getlanguage TOKEN - Started :" + token, string.Empty, string.Empty, ClientUID, DCID);
            if (string.IsNullOrEmpty(token))
                return null;
            string JsonStringResult = "False";
            HttpClient httpClient;
            if (_authProvider.ToUpper() == "FORM" || _authProvider.ToUpper() == "AZUREAD")
            {
                httpClient = new HttpClient();
            }
            else
            {
                HttpClientHandler hnd = new HttpClientHandler();
                hnd.UseDefaultCredentials = true;
                httpClient = new HttpClient(hnd);
            }

            setHeaderAgile(token, httpClient);
            var tipAMURL = String.Format("?ClientUId=" + Convert.ToString(ClientUID) + "&DeliveryConstructUId=" + DCID);
            HttpContent content = new StringContent(string.Empty);
            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            var result = httpClient.PostAsync(tipAMURL, null).Result;
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutoTrain), nameof(getlanguage), "getlanguage URl :" + tipAMURL, string.Empty, string.Empty, string.Empty, string.Empty);
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutoTrain), nameof(getlanguage), "getlanguage response : Status Code - " + result.StatusCode + ", Content - " + result.Content, string.Empty, string.Empty, string.Empty, string.Empty);

            if (result.StatusCode != System.Net.HttpStatusCode.OK)
            {
                return "english";
                // throw new Exception(string.Format("GetMetricData: Unable to get data. Please check your credentials and try again. Status Code: {0}", result.StatusCode));
            }
            var result1 = result.Content.ReadAsStringAsync().Result;
            var resultobj = JsonConvert.DeserializeObject(result1) as dynamic;
            JsonStringResult = Convert.ToString(resultobj.language);
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutoTrain), nameof(getlanguage), "Language :" + JsonStringResult, string.Empty, string.Empty, ClientUID, DCID);
            return JsonStringResult;

        }

        private void setHeaderAgile(string token, HttpClient httpClient)
        {
            if (_authProvider.ToUpper() == "FORM")
            {
                httpClient.BaseAddress = new Uri(_language);
                httpClient.DefaultRequestHeaders.Accept.Clear();

                httpClient.DefaultRequestHeaders.Add("UserName", username);
                httpClient.DefaultRequestHeaders.Add("Password", password);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                httpClient.DefaultRequestHeaders.Add("AppServiceUID", _AppServiceUId);
            }
            else if (_authProvider.ToUpper() == "AZUREAD")
            {
                httpClient.BaseAddress = new Uri(_language);
                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Add("AppServiceUID", _AppServiceUId);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
            }
            else
            {
                httpClient.BaseAddress = new Uri(_language);
                httpClient.DefaultRequestHeaders.Accept.Clear();

                httpClient.DefaultRequestHeaders.Add("UserEmailId", username);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.Add("AppServiceUID", _AppServiceUId);
            }
        }

        private string getToken()
        {
            dynamic token = string.Empty;
            if (_authProvider.ToUpper() == "FORM")
            {
                using (var httpClient = new HttpClient())
                {
                    if (Environment == CONSTANTS.PAMEnvironment)
                    {
                        httpClient.BaseAddress = new Uri(tokenapiURL);
                        httpClient.DefaultRequestHeaders.Accept.Clear();
                        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                        string json = JsonConvert.SerializeObject(new
                        {
                            username = Convert.ToString(username),
                            password = Convert.ToString(password)
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
                        if (Environment == CONSTANTS.PAMEnvironment)
                            token = tokenObj != null ? Convert.ToString(tokenObj.token) : CONSTANTS.InvertedComma;
                        else
                            token = tokenObj != null ? Convert.ToString(tokenObj.access_token) : CONSTANTS.InvertedComma;
                        return token;

                    }
                    else
                    {
                        httpClient.BaseAddress = new Uri(tokenapiURL);
                        httpClient.DefaultRequestHeaders.Accept.Clear();
                        httpClient.DefaultRequestHeaders.Add("UserName", username);
                        httpClient.DefaultRequestHeaders.Add("Password", password);
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
                    //httpClient.BaseAddress = new Uri(tokenapiURL);
                    //httpClient.DefaultRequestHeaders.Accept.Clear();
                    //httpClient.DefaultRequestHeaders.Add("UserName", username);
                    //httpClient.DefaultRequestHeaders.Add("Password", password);
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
            else if (_authProvider.ToUpper() == "AZUREAD")
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    var formContent = new FormUrlEncodedContent(new[]
                    {
                            new KeyValuePair<string, string>("grant_type", _Grant_Type),
                            new KeyValuePair<string, string>("client_id", _clientId),
                            new KeyValuePair<string, string>("client_secret",_clientSecret),
                            new KeyValuePair<string, string>("resource",_resourceId)
                        });

                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                    var tokenResult = client.PostAsync(_token_Url, formContent).Result;
                    Dictionary<string, string> tokenDictionary = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(tokenResult.Content.ReadAsStringAsync().Result);
                    if (tokenDictionary != null)
                    {
                        return tokenDictionary[CONSTANTS.access_token].ToString();
                    }
                    else
                    {
                        return string.Empty;
                    }

                }
            }
            return token;
        }
        private bool ValidateIngestedDataWithTemplates(string correlationId, GenericTemplatemapping genericTemplateInfo)
        {
            bool isIngestedColumnMatched = false;
            bool isTargetUniqueIdentifier;
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.PS_IngestedData);
            var builder = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var IngestdataCompare = collection.Find(builder).ToList();
            if (IngestdataCompare.Count > 0)
            {
                var coulumnlistitems = IngestdataCompare[0]["ColumnsList"].ToString();

                bool isTargetColumnMatched = coulumnlistitems.Contains(genericTemplateInfo.TargetColumn);
                if (!string.IsNullOrEmpty(genericTemplateInfo.TargetUniqueIdentifier))
                    isTargetUniqueIdentifier = coulumnlistitems.Contains(genericTemplateInfo.TargetUniqueIdentifier);
                else
                    isTargetUniqueIdentifier = true;

                // LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(ValidateIngestedDataWithTemplates), "isTargetColumnMatched :" + isTargetColumnMatched);
                if (isTargetColumnMatched == true && isTargetUniqueIdentifier == true)
                {
                    isIngestedColumnMatched = true;
                }
            }
            return isIngestedColumnMatched;
        }

        private void StartModelTraining(GenericTemplatemapping templateInfo, string CorrelationId, string AppURL)
        {
            try
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(StartModelTraining), "StartModelTraining - Started :", string.IsNullOrEmpty(CorrelationId) ? default(Guid) : new Guid(CorrelationId), templateInfo.AppicationID, string.Empty, templateInfo.ClientUID, templateInfo.DCID);
                IList<string> ProcessName = new List<string>() { CONSTANTS.DataEngineering, CONSTANTS.ModelEngineering, CONSTANTS.DeployModel };
                string progressPercent = "25%";
                string status = "P";
                string message = "Training InProgress";
                if (_GenericDataResponse.Status == CONSTANTS.C)
                {
                    foreach (var Processname in ProcessName)
                    {
                        switch (Processname)
                        {
                            //case "DE":
                            //    //Validating uniqueness of TargetColumn. If less than 90% DE is not triggered.
                            //    bool validateUniqueness = this.ValidateUniqueness(NewCorrelationId, _templateInfo.TargetUniqueIdentifier);
                            //    if (!validateUniqueness)
                            //    {
                            //        _GenericDataResponse.Message = CONSTANTS.UniquenessMessage;
                            //        _GenericDataResponse.Status = CONSTANTS.E;
                            //        status = CONSTANTS.E;
                            //        message = CONSTANTS.UniquenessMessage;
                            //        _GenericDataResponse.ErrorMessage = CONSTANTS.UniquenessMessage;
                            //        LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(StartModelTraining), "Uniqueness validation failed", string.IsNullOrEmpty(CorrelationId) ? default(Guid) : new Guid(CorrelationId), templateInfo.AppicationID, string.Empty, templateInfo.ClientUID, templateInfo.DCID);
                            //        if (!_GenericDataResponse.ErrorMessage.Contains(CONSTANTS.WSStartStatus))
                            //            DeleteDeployModel(CorrelationId);
                            //        return;
                            //    }
                            //    StartDataEngineering(CorrelationId, _Mapping.CreatedByUser, templateInfo.ProblemType, templateInfo.AppicationID);
                            //    if (_GenericDataResponse.Status == CONSTANTS.E)
                            //    {
                            //        _GenericDataResponse.Message = CONSTANTS.DE_Error;
                            //        status = _GenericDataResponse.Status;
                            //        message = _GenericDataResponse.Message;
                            //        if (!_GenericDataResponse.ErrorMessage.Contains(CONSTANTS.WSStartStatus))
                            //            DeleteDeployModel(CorrelationId);
                            //        return;
                            //    }
                            //    else
                            //    {
                            //        LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(StartModelTraining), "StartDataEngineering Success:", string.IsNullOrEmpty(CorrelationId) ? default(Guid) : new Guid(CorrelationId), templateInfo.AppicationID, string.Empty, templateInfo.ClientUID, templateInfo.DCID);

                            //        progressPercent = "50%";
                            //        UpdateRequestStatus(progressPercent);
                            //    }
                            //    break;

                            case "ME":
                                bool bDataProcessingCompleted = false;
                                while (!bDataProcessingCompleted)
                                {
                                    var IngrainCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
                                    var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, CorrelationId) & Builders<BsonDocument>.Filter.Eq(CONSTANTS.Function, CONSTANTS.AutoTrain) & Builders<BsonDocument>.Filter.Eq(CONSTANTS.FunctionPageInfo, "ME");
                                    var resultantData = IngrainCollection.Find(filter).ToList();
                                    if (resultantData.Count > 0)
                                    {
                                        bDataProcessingCompleted = true;
                                        StartModelEngineering(CorrelationId, _Mapping.CreatedByUser, templateInfo.ProblemType, templateInfo.AppicationID);
                                    }
                                }

                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(StartModelTraining), "StartModelEngineering -Generic dataresponse - Status = " + _GenericDataResponse.Status, string.IsNullOrEmpty(CorrelationId) ? default(Guid) : new Guid(CorrelationId), templateInfo.AppicationID, string.Empty, templateInfo.ClientUID, templateInfo.DCID);
                                if (_GenericDataResponse.Status == CONSTANTS.E)
                                {
                                    _GenericDataResponse.Message = CONSTANTS.ME_Error;
                                    status = _GenericDataResponse.Status;
                                    message = _GenericDataResponse.Message;
                                    if (!_GenericDataResponse.ErrorMessage.Contains(CONSTANTS.WSStartStatus))
                                        DeleteDeployModel(CorrelationId);
                                    return;
                                }
                                else
                                {
                                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(StartModelTraining), "StartModelEngineering Success:", string.IsNullOrEmpty(CorrelationId) ? default(Guid) : new Guid(CorrelationId), templateInfo.AppicationID, string.Empty, templateInfo.ClientUID, templateInfo.DCID);
                                    progressPercent = "75%";
                                    UpdateRequestStatus(progressPercent);
                                }
                                break;

                            case "DM":
                                DeployModel(CorrelationId, _Mapping.CreatedByUser, templateInfo.ProblemType, templateInfo.AppicationID);
                                if (_GenericDataResponse.Status == CONSTANTS.E)
                                {
                                    _GenericDataResponse.Message = CONSTANTS.DM_Error;
                                    status = _GenericDataResponse.Status;
                                    message = _GenericDataResponse.Message;
                                    if (!_GenericDataResponse.ErrorMessage.Contains(CONSTANTS.WSStartStatus))
                                        DeleteDeployModel(CorrelationId);
                                    return;
                                }
                                else
                                {
                                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(StartModelTraining), "DeployModel Success:", string.IsNullOrEmpty(CorrelationId) ? default(Guid) : new Guid(CorrelationId), templateInfo.AppicationID, string.Empty, templateInfo.ClientUID, templateInfo.DCID);
                                    progressPercent = "100%";
                                    status = CONSTANTS.C;
                                    message = CONSTANTS.TrainingCompleted;
                                    UpdateRequestStatus(progressPercent);
                                }
                                break;
                        }
                        if (_templateInfo.AppicationID == SPE_appId)
                            Insert_notification(SPE_appId, templateInfo.CreatedByUser, NewCorrelationId, AppURL, status, progressPercent, message, _GenericDataResponse.ErrorMessage);
                    }
                }
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(StartModelTraining), "StartModelTraining - Ended :", string.IsNullOrEmpty(CorrelationId) ? default(Guid) : new Guid(CorrelationId), templateInfo.AppicationID, string.Empty, templateInfo.ClientUID, templateInfo.DCID);
            }
            catch (Exception ex)
            {
                CustomAppsActivityLog log = new CustomAppsActivityLog();
                log.CorrelationId = CorrelationId;
                log.FeatureName = "PrivateModelTraining";
                log.StackTrace = ex.StackTrace;
                log.ErrorMessage = ex.Message;
                log.ErrorMethod = "StartModelTraining";
                log.CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                log.CreatedBy = "SYSTEM";
                if (templateInfo != null)
                {
                    log.ClientId = templateInfo.ClientUID;
                    log.DCID = templateInfo.DCID;
                    log.UseCaseId = templateInfo.UseCaseID;
                    log.ApplicationID = templateInfo.AppicationID;
                }

                this.InsertCustomAppsActivityLog(log);
            }
        }

        private bool ValidateUniqueness(string correlationId, string targetUniqueIdentifier)
        {
            var useCaseCollection = _database.GetCollection<UserColumns>(CONSTANTS.PSUseCaseDefinition);
            var filter = Builders<UserColumns>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var useCaseprojection = Builders<UserColumns>.Projection.Include(CONSTANTS.CorrelationId).Include(CONSTANTS.UniquenessDetails).Exclude(CONSTANTS.Id);
            var resultantData = useCaseCollection.Find(filter).Project<BsonDocument>(useCaseprojection).ToList();
            JObject uniquenessDetails = null;

            if (resultantData.Count > 0)
            {
                uniquenessDetails = JObject.Parse(resultantData[0][CONSTANTS.UniquenessDetails].ToString());
                if (uniquenessDetails != null)
                {
                    foreach (var parent in uniquenessDetails.Children())
                    {
                        JProperty prop = parent as JProperty;
                        if (prop.Name == targetUniqueIdentifier && Convert.ToDouble(prop.Value["Percent"]) < 90)
                            return false;
                    }
                }
            }

            return true;
        }

        private void DeleteDeployModel(string CorrelationId)
        {
            try
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(DeleteDeployModel), "Error Occurs during Training - CorrelationId" + CorrelationId, string.Empty, string.Empty, string.Empty, string.Empty);
                var deloymodelCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_DeployedModels);
                var filterBuilder = Builders<BsonDocument>.Filter;
                var filterQueue1 = filterBuilder.Eq("CorrelationId", CorrelationId);
                deloymodelCollection.DeleteMany(filterQueue1);
            }
            catch (Exception ex)
            {
                CustomAppsActivityLog log = new CustomAppsActivityLog();
                log.CorrelationId = CorrelationId;
                log.FeatureName = "PrivateModelTraining";
                log.StackTrace = ex.StackTrace;
                log.ErrorMessage = ex.Message;
                log.ErrorMethod = "DeleteDeployModel";
                log.CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                log.CreatedBy = "SYSTEM";

                this.InsertCustomAppsActivityLog(log);
            }
        }
        private void DeleteCascadeModel(string CorrelationId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(DeleteCascadeModel), "Error Occurs during Training - CorrelationId" + CorrelationId, string.Empty, string.Empty, string.Empty, string.Empty);
            var deloymodelCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAICascadedModels);
            var filterBuilder = Builders<BsonDocument>.Filter;
            var filterQueue1 = filterBuilder.Eq(CONSTANTS.CascadedId, CorrelationId);
            deloymodelCollection.DeleteMany(filterQueue1);
        }
        private void StartDataEngineering(string correlationId, string userId, string ProblemType, string AppID)
        {
            bool isDataCurationCompleted = false;
            bool isDataTransformationCompleted = false;
            DataEngineering dataEngineering = new DataEngineering();
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(StartDataEngineering), "Generic Self Service StartDataEngineering is Started", string.IsNullOrEmpty(NewCorrelationId) ? default(Guid) : new Guid(NewCorrelationId), AppID, string.Empty, string.Empty, string.Empty);
            try
            {
                dataEngineering = GetDataCuration(correlationId, CONSTANTS.DataCleanUp, userId, AppID);

                _GenericDataResponse.Status = dataEngineering.Status;
                if (dataEngineering.Status == CONSTANTS.E)
                {
                    _GenericDataResponse.ErrorMessage = Resource.IngrainResx.ErrorDataEngineering + dataEngineering.Message;
                }
                if (dataEngineering.Status == CONSTANTS.C)
                {

                    ///get template table data
                    JObject modifiedCols = GetDataCleanupOfTemplate(_Mapping.UsecaseID);
                    if (modifiedCols != null && modifiedCols.ToString() != "{}")
                    {
                        if (modifiedCols["DtypeModifiedColumns"] != null || modifiedCols["ScaleModifiedColumns"] != null)
                        {
                            InsertModifiedColsinDataCleannUp(modifiedCols, correlationId);
                            dataEngineering = IsUpdateCleanUpComplete(correlationId, "UpdateDataCleanUp", userId, AppID);
                            if (dataEngineering.Status == "E")
                            {
                                _GenericDataResponse.ErrorMessage = Resource.IngrainResx.ErrorDataEngineering + dataEngineering.Message;
                                isDataCurationCompleted = false;
                            }

                        }
                    }
                    isDataCurationCompleted = this.IsDataCurationComplete(correlationId, false);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(GetDataCuration), "DataCleanup Completed", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), AppID, string.Empty, string.Empty, string.Empty);
                    /////insert dtyp, scale modified as it is
                    ///take dtype attr and update in feature names datatype,scale
                    ///inert request in ssai
                }

                LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(StartDataEngineering), "isDataCurationCompleted: " + isDataCurationCompleted, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), AppID, string.Empty, string.Empty, string.Empty);
                if (isDataCurationCompleted)
                {
                    PreProcessModelDTO preProcessModelDTO = new PreProcessModelDTO();
                    isDataTransformationCompleted = this.CreatePreprocess(correlationId, userId, ProblemType);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(GetDataCuration), "Datatranform Formed", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), AppID, string.Empty, string.Empty, string.Empty);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(StartDataEngineering), "isDataTransformationCompleted: " + isDataTransformationCompleted, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), AppID, string.Empty, string.Empty, string.Empty);
                    if (isDataTransformationCompleted)
                    {
                        dataEngineering = GetDatatransformation(correlationId, CONSTANTS.DataPreprocessing, userId, AppID);
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(GetDataCuration), "DataPreprocess Apply completed", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), AppID, string.Empty, string.Empty, string.Empty);
                        _GenericDataResponse.Status = dataEngineering.Status;
                        switch (dataEngineering.Status)
                        {
                            case CONSTANTS.E:
                                _GenericDataResponse.ErrorMessage = Resource.IngrainResx.ErrorDataEngineering + dataEngineering.Message;
                                break;

                            case CONSTANTS.C:
                                _GenericDataResponse.Message = Resource.IngrainResx.DataEngineering;
                                break;
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(GenericAutotrainService), nameof(StartDataEngineering), ex.Message, ex, AppID, string.Empty, string.Empty, string.Empty);
                _GenericDataResponse.ErrorMessage = ex.Message + CONSTANTS.ThreeDots + ex.StackTrace;
                _GenericDataResponse.Status = CONSTANTS.E;
                _GenericDataResponse.ErrorMessage = Resource.IngrainResx.EngineeringDataError;

            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(StartDataEngineering), "Generic Self Service StartFMDataEngineering is Ended", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), AppID, string.Empty, string.Empty, string.Empty);
        }
        private dynamic GetDataCleanupOfTemplate(string usecaseId)
        {

            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, usecaseId);
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataCleanup);
            var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.DtypeModifiedColumns).Include(CONSTANTS.ScaleModifiedColumns).Exclude(CONSTANTS.Id);
            var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                dynamic dynamicColumns = JsonConvert.DeserializeObject(result[0].ToJson());
                return dynamicColumns;
            }
            else
            {
                return null;
            }
        }
        private void InsertModifiedColsinDataCleannUp(dynamic data, string correlationId)
        {
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataCleanup);
            var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.FeatureName).Exclude(CONSTANTS.Id);
            var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                //decrypt db values
                if (_DBEncryptionRequired)
                {
                    if (_IsAESKeyVault)
                        result[0][CONSTANTS.FeatureName] = BsonDocument.Parse(CryptographyUtility.Decrypt(result[0][CONSTANTS.FeatureName].AsString));
                    else
                        result[0][CONSTANTS.FeatureName] = BsonDocument.Parse(AesProvider.Decrypt(result[0][CONSTANTS.FeatureName].AsString, _aesKey, _aesVector));
                }


                var featuresData = JObject.Parse(result[0].ToString());

                if (data.ScaleModifiedColumns != null)
                {
                    string scaleModifiedCols = string.Format(CONSTANTS.ScaleModifiedColumns);
                    var scaleUpdatemodify = Builders<BsonDocument>.Update.Set(scaleModifiedCols, BsonDocument.Parse(data.ScaleModifiedColumns.ToString()));
                    collection.UpdateOne(filter, scaleUpdatemodify);
                }
                if (data.DtypeModifiedColumns != null)
                {
                    string datatypeModifiedCols = string.Format(CONSTANTS.DtypeModifiedColumns);
                    var datatypeUpdate = Builders<BsonDocument>.Update.Set(datatypeModifiedCols, BsonDocument.Parse(data.DtypeModifiedColumns.ToString()));
                    collection.UpdateOne(filter, datatypeUpdate);
                }

                if (data.DtypeModifiedColumns != null)
                {
                    foreach (var cols in data.DtypeModifiedColumns)
                    {
                        var parentData = featuresData[CONSTANTS.FeatureName][cols.Name];
                        if (parentData != null)
                        {
                            foreach (var item in featuresData[CONSTANTS.FeatureName][cols.Name][CONSTANTS.Datatype].Children())
                            {
                                featuresData[CONSTANTS.FeatureName][cols.Name]["Datatype"][item.Name] = CONSTANTS.False;

                            }
                        }
                        //update the user input field to True
                        featuresData[CONSTANTS.FeatureName][cols.Name]["Datatype"][cols.Value.ToString()] = CONSTANTS.True;

                    }
                }
                if (data.ScaleModifiedColumns != null)
                {
                    foreach (var cols in data.ScaleModifiedColumns)
                    {
                        var parentData = featuresData[CONSTANTS.FeatureName][cols.Name];
                        if (parentData != null)
                        {
                            foreach (var item in featuresData[CONSTANTS.FeatureName][cols.Name][CONSTANTS.Scale].Children())
                            {
                                featuresData[CONSTANTS.FeatureName][cols.Name]["Scale"][item.Name] = CONSTANTS.False;

                            }
                        }
                        //update the user input field to True
                        featuresData[CONSTANTS.FeatureName][cols.Name]["Scale"][cols.Value.ToString()] = CONSTANTS.True;

                    }
                }


                // encrypt db values
                if (_DBEncryptionRequired)
                {
                    var scaleUpdate = Builders<BsonDocument>.Update.Set(CONSTANTS.FeatureName, (_IsAESKeyVault ? CryptographyUtility.Encrypt(featuresData[CONSTANTS.FeatureName].ToString(Formatting.None)) : AesProvider.Encrypt(featuresData[CONSTANTS.FeatureName].ToString(Formatting.None), _aesKey, _aesVector)));
                    collection.UpdateOne(filter, scaleUpdate);
                }
                else
                {
                    var Featuredata = BsonDocument.Parse(featuresData[CONSTANTS.FeatureName].ToString());
                    var scaleUpdate = Builders<BsonDocument>.Update.Set(CONSTANTS.FeatureName, Featuredata);
                    collection.UpdateOne(filter, scaleUpdate);
                }


            }

        }
        private DataEngineering IsUpdateCleanUpComplete(string correlationId, string pageInfo, string userId, string ApplicationId)
        {
            bool callMethod = true;
            DataEngineering dataEngineering = new DataEngineering();
            string PythonResult = string.Empty;
            while (callMethod)
            {
                Thread.Sleep(2000);
                var useCaseData = CheckPythonProcess(correlationId, pageInfo);
                if (!string.IsNullOrEmpty(useCaseData))
                {
                    JObject queueData = JObject.Parse(useCaseData);
                    dataEngineering.Status = (string)queueData[CONSTANTS.Status];
                    dataEngineering.Progress = (string)queueData[CONSTANTS.Progress];
                    dataEngineering.Message = (string)queueData[CONSTANTS.Message];
                    if (dataEngineering.Status == CONSTANTS.C && dataEngineering.Progress == CONSTANTS.Hundred)
                    {
                        callMethod = false;
                        dataEngineering.IsComplete = true;
                        return dataEngineering;
                    }
                    else if (dataEngineering.Status == CONSTANTS.E)
                    {
                        callMethod = false;
                        dataEngineering.IsComplete = false;
                        return dataEngineering;
                    }
                }
                else
                {
                    IngrainRequestQueue ingrainRequest = new IngrainRequestQueue
                    {
                        _id = Guid.NewGuid().ToString(),
                        CorrelationId = correlationId,
                        RequestId = Guid.NewGuid().ToString(),
                        ProcessId = CONSTANTS.Null,
                        Status = CONSTANTS.Null,
                        ModelName = CONSTANTS.Null,
                        RequestStatus = CONSTANTS.New,
                        RetryCount = 0,
                        ProblemType = CONSTANTS.Null,
                        Message = CONSTANTS.Null,
                        UniId = CONSTANTS.Null,
                        Progress = CONSTANTS.Null,
                        pageInfo = pageInfo,
                        ParamArgs = CONSTANTS.CurlyBraces,
                        Function = CONSTANTS.DataCleanUp,
                        CreatedByUser = userId,
                        CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                        ModifiedByUser = userId,
                        ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                        LastProcessedOn = CONSTANTS.Null,
                        AppID = ApplicationId,
                        ClientId = _templateInfo.ClientUID,
                        DeliveryconstructId = _templateInfo.DCID,
                        TemplateUseCaseID = _Mapping.UsecaseID
                    };
                    InsertRequests(ingrainRequest);

                }
            }
            return dataEngineering;
        }
        private DataEngineering GetDataCuration(string correlationId, string pageInfo, string userId, string ApplicationId)
        {
            bool callMethod = true;
            DataEngineering dataEngineering = new DataEngineering();
            string PythonResult = string.Empty;
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(GetDataCuration), "Generic Self Service StartDataEngineering is Started", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), ApplicationId, string.Empty, string.Empty, string.Empty);
            while (callMethod)
            {
                Thread.Sleep(2000);
                var useCaseData = CheckPythonProcess(correlationId, CONSTANTS.DataCleanUp);
                if (!string.IsNullOrEmpty(useCaseData))
                {
                    JObject queueData = JObject.Parse(useCaseData);
                    dataEngineering.Status = (string)queueData[CONSTANTS.Status];
                    dataEngineering.Progress = (string)queueData[CONSTANTS.Progress];
                    dataEngineering.Message = (string)queueData[CONSTANTS.Message];
                    if (dataEngineering.Status == CONSTANTS.C & dataEngineering.Progress == CONSTANTS.Hundred)
                    {
                        callMethod = false;
                        dataEngineering.IsComplete = true;
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(GetDataCuration), "DataCleanup completed", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), ApplicationId, string.Empty, string.Empty, string.Empty);
                        return dataEngineering;
                    }
                    else if (dataEngineering.Status == CONSTANTS.E)
                    {
                        callMethod = false;
                        dataEngineering.IsComplete = false;
                        return dataEngineering;
                    }
                }
                else
                {
                    IngrainRequestQueue ingrainRequest = new IngrainRequestQueue
                    {
                        _id = Guid.NewGuid().ToString(),
                        CorrelationId = correlationId,
                        RequestId = Guid.NewGuid().ToString(),
                        ProcessId = CONSTANTS.Null,
                        Status = CONSTANTS.Null,
                        ModelName = CONSTANTS.Null,
                        RequestStatus = CONSTANTS.New,
                        RetryCount = 0,
                        ProblemType = CONSTANTS.Null,
                        Message = CONSTANTS.Null,
                        UniId = CONSTANTS.Null,
                        Progress = CONSTANTS.Null,
                        pageInfo = CONSTANTS.DataCleanUp,
                        ParamArgs = CONSTANTS.CurlyBraces,
                        Function = CONSTANTS.DataCleanUp,
                        CreatedByUser = userId,
                        CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                        ModifiedByUser = userId,
                        ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                        LastProcessedOn = CONSTANTS.Null,
                        AppID = ApplicationId,
                        ClientId = _templateInfo.ClientUID,
                        DeliveryconstructId = _templateInfo.DCID,
                        TemplateUseCaseID = _Mapping.UsecaseID
                    };
                    InsertRequests(ingrainRequest);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(GetDataCuration), "DataCleanup Request inserted", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), ApplicationId, string.Empty, string.Empty, string.Empty);

                }
            }
            return dataEngineering;
        }


        private bool IsDataCurationComplete(string correlationId, bool isFmModel)
        {
            bool IsCompleted = false;
            bool TextPreProcessing = false;
            List<string> columnsList = new List<string>();
            List<string> TextColumnsList = new List<string>();
            List<string> noDatatypeList = new List<string>();
            Dictionary<string, string> dtypeColumns = new Dictionary<string, string>();
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataCleanup);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.Target_ProblemType).Include(CONSTANTS.FeatureName).Exclude(CONSTANTS.Id);
            var resultData = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            JObject dataCuration = new JObject();
            if (resultData.Count > 0)
            {
                if (resultData[0].Contains(CONSTANTS.Target_ProblemType))
                {
                    int type = Convert.ToInt32(resultData[0][CONSTANTS.Target_ProblemType]);
                    if (type > 0)
                    {
                        switch (type)
                        {
                            case 1:
                                problemType = CONSTANTS.Regression;
                                break;

                            case 2:
                            case 3:
                                problemType = CONSTANTS.Classification;
                                break;
                            case 4:
                                problemType = CONSTANTS.TimeSeries;
                                break;
                        }
                    }

                }
                //decrypt db data
                if (_DBEncryptionRequired)
                {
                    if (_IsAESKeyVault)
                        resultData[0][CONSTANTS.FeatureName] = BsonDocument.Parse(CryptographyUtility.Decrypt(resultData[0][CONSTANTS.FeatureName].ToString()));
                    else
                        resultData[0][CONSTANTS.FeatureName] = BsonDocument.Parse(AesProvider.Decrypt(resultData[0][CONSTANTS.FeatureName].ToString(), _aesKey, _aesVector));
                }

                dataCuration = JObject.Parse(resultData[0].ToString());
                foreach (var column in dataCuration[CONSTANTS.FeatureName].Children())
                {
                    JProperty property = column as JProperty;
                    columnsList.Add(property.Name.ToString());
                }
                foreach (var column in columnsList)
                {
                    bool datatypeExist = false;

                    foreach (var item in dataCuration[CONSTANTS.FeatureName][column][CONSTANTS.Datatype].Children())
                    {
                        if (item != null)
                        {
                            JProperty property = item as JProperty;
                            if (property.Name != CONSTANTS.Select_Option)
                            {
                                if (property.Value.ToString() == CONSTANTS.True)
                                {
                                    datatypeExist = true;
                                    IsCompleted = true;
                                }
                            }
                            else
                            {
                                dataCuration[CONSTANTS.FeatureName][column][CONSTANTS.Datatype]["Select Option"] = CONSTANTS.False;
                            }
                            // Text PreProcessing Changes
                            if (property.Name == "Text")
                            {
                                if (property.Value.ToString() == CONSTANTS.True)
                                {
                                    TextColumnsList.Add(column.ToString());
                                    TextPreProcessing = true;
                                }
                            }

                        }
                    }

                    if (_DBEncryptionRequired)
                    {
                        var updateField = Builders<BsonDocument>.Update.Set(CONSTANTS.FeatureName, (_IsAESKeyVault ? CryptographyUtility.Encrypt(dataCuration[CONSTANTS.FeatureName].ToString(Formatting.None)) : AesProvider.Encrypt(dataCuration[CONSTANTS.FeatureName].ToString(Formatting.None), _aesKey, _aesVector)));
                        var updateResult = collection.UpdateOne(filter, updateField);
                    }
                    else
                    {
                        var Featuredata = BsonDocument.Parse(dataCuration[CONSTANTS.FeatureName].ToString());
                        var updateField = Builders<BsonDocument>.Update.Set(CONSTANTS.FeatureName, Featuredata);
                        var updateResult = collection.UpdateOne(filter, updateField);
                    }
                    if (isFmModel)
                    {
                        if (column == CONSTANTS.ReleaseSuccessProbability)
                        {
                            dataCuration[CONSTANTS.FeatureName][column][CONSTANTS.Datatype][CONSTANTS.float64] = CONSTANTS.True;
                            if (_DBEncryptionRequired)
                            {
                                var updateField = Builders<BsonDocument>.Update.Set(CONSTANTS.FeatureName, (_IsAESKeyVault ? CryptographyUtility.Encrypt(dataCuration[CONSTANTS.FeatureName].ToString(Formatting.None)) : AesProvider.Encrypt(dataCuration[CONSTANTS.FeatureName].ToString(Formatting.None), _aesKey, _aesVector)));
                                var updateResult = collection.UpdateOne(filter, updateField);
                            }
                            else
                            {
                                var Featuredata = BsonDocument.Parse(dataCuration[CONSTANTS.FeatureName].ToString());
                                var updateField = Builders<BsonDocument>.Update.Set(CONSTANTS.FeatureName, Featuredata);
                                var updateResult = collection.UpdateOne(filter, updateField);
                            }
                        }
                    }
                    if (!datatypeExist)
                    {
                        IsUpdateDataClenaup = true;
                        dtypeColumns.Add(column, CONSTANTS.category);
                        if (isFmModel)
                            dataCuration[CONSTANTS.FeatureName][column][CONSTANTS.Datatype][CONSTANTS.float64] = CONSTANTS.True;
                        else
                            dataCuration[CONSTANTS.FeatureName][column][CONSTANTS.Datatype][CONSTANTS.category] = CONSTANTS.True;
                        if (_DBEncryptionRequired)
                        {
                            var updateField = Builders<BsonDocument>.Update.Set(CONSTANTS.FeatureName, (_IsAESKeyVault ? CryptographyUtility.Encrypt(dataCuration[CONSTANTS.FeatureName].ToString(Formatting.None)) : AesProvider.Encrypt(dataCuration[CONSTANTS.FeatureName].ToString(Formatting.None), _aesKey, _aesVector)));
                            var updateResult = collection.UpdateOne(filter, updateField);
                        }
                        else
                        {
                            var Featuredata = BsonDocument.Parse(dataCuration[CONSTANTS.FeatureName].ToString());
                            var updateField = Builders<BsonDocument>.Update.Set(CONSTANTS.FeatureName, Featuredata);
                            var updateResult = collection.UpdateOne(filter, updateField);
                        }
                        IsCompleted = true;
                    }
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(IsDataCurationComplete), "TextPreProcessing status" + TextPreProcessing, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                }

                if (TextPreProcessing)
                {
                    UpdateTextDataProcessing(correlationId, TextColumnsList);
                }
                //Calling UpdateDatacleanup
                if (IsUpdateDataClenaup)
                {
                    Dictionary<string, string> scalemodifiedColumns = new Dictionary<string, string>();
                    //Update the dtypemodifiedcolumns
                    string scaleModifiedCols = string.Format(CONSTANTS.ScaleModifiedColumns);
                    var scaleUpdatemodify = Builders<BsonDocument>.Update.Set(scaleModifiedCols, scalemodifiedColumns);
                    collection.UpdateOne(filter, scaleUpdatemodify);

                    string datatypeModifiedCols = string.Format(CONSTANTS.DtypeModifiedColumns);
                    var datatypeUpdate = Builders<BsonDocument>.Update.Set(datatypeModifiedCols, dtypeColumns);
                    collection.UpdateOne(filter, datatypeUpdate);


                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(IsDataCurationComplete), "ISUPDATECLEANUPCOMPLETE ISUPDATEDATACLENAUP22--" + IsUpdateDataClenaup + "-ISFMMODEL-" + isFmModel + "--USERID--" + _Mapping.CreatedByUser + _Mapping.ApplicationID, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), _Mapping.ApplicationID, string.Empty, string.Empty, string.Empty);
                    var dataEngineering = IsUpdateCleanUpComplete(correlationId, "UpdateDataCleanUp", _Mapping.CreatedByUser, _Mapping.ApplicationID);
                    if (dataEngineering.Status == CONSTANTS.C)
                        IsCompleted = true;
                    else
                        IsCompleted = false;
                }
            }
            return IsCompleted;
        }

        private void UpdateTextDataProcessing(string correlationId, List<string> TextColumnsList)
        {
            // From Parent Template cloning the Text data preprocessing as it is to Child Private Model.
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, _templateInfo.CorrelationID);
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.DE_DataProcessing);
            var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.DataModification).Exclude(CONSTANTS.Id);
            var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            JObject serializeData = new JObject();
            if (result.Count > 0)
            {
                // if (_DBEncryptionRequired)
                if (_TemplateDBEncryptionRequired) //template reading the data , so take flag from template
                {
                    if (_IsAESKeyVault)
                        result[0][CONSTANTS.DataModification] = BsonDocument.Parse(CryptographyUtility.Decrypt(result[0][CONSTANTS.DataModification].ToString()));
                    else
                        result[0][CONSTANTS.DataModification] = BsonDocument.Parse(AesProvider.Decrypt(result[0][CONSTANTS.DataModification].ToString(), _aesKey, _aesVector));
                }
                serializeData = JObject.Parse(result[0][CONSTANTS.DataModification].ToString());
                if (serializeData[CONSTANTS.TextDataPreprocessing].ToString() != "")
                {
                    _textdata = JObject.FromObject(serializeData[CONSTANTS.TextDataPreprocessing]);
                }
                else
                {
                    //Defect : 767310


                    string[] value = new string[] { };
                    JArray jarrayObj = new JArray() { 3, 3 };
                    preprocessData preprocess = new preprocessData()
                    {
                        Lemmitize = CONSTANTS.True,
                        Stemming = CONSTANTS.False,
                        Pos = CONSTANTS.False,
                        Stopwords = value,
                        Least_Frequent = 0,
                        Most_Frequent = 0
                    };

                    var textpreprocess = Newtonsoft.Json.JsonConvert.SerializeObject(preprocess);
                    JObject Data = new JObject();
                    Data = JObject.Parse(textpreprocess);

                    foreach (var name in TextColumnsList)
                    {
                        _textdata.Add(name, Data);
                    }

                    _textdata.Add(new JProperty(CONSTANTS.Feature_Generator, CONSTANTS.Count_Vectorizer));
                    _textdata.Add(new JProperty(CONSTANTS.Ngrams, jarrayObj));
                    _textdata.Add(new JProperty(CONSTANTS.NumberOfCluster, 1));
                    if (_textdata.ContainsKey(CONSTANTS.TextColumnsDeletedByUser))
                    { _textdata.Remove(CONSTANTS.TextColumnsDeletedByUser); }
                    _textdata.Add(new JProperty(CONSTANTS.TextColumnsDeletedByUser, value));
                }
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(IsDataCurationComplete), "UpdateTextDataProcessing Completed", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
        }

        private bool CreatePreprocess(string correlationId, string userId, string problemType)
        {
            PreProcessModelDTO preProcessModel = new PreProcessModelDTO
            {
                CorrelationId = correlationId,
                ModelType = problemType
            };
            preProcessModel.ModelType = problemType;
            var preprocessDataExist = GetPreprocessExistData(correlationId);
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(CreatePreprocess), "CreatePreprocess Data Exist" + preprocessDataExist.Count, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            if (preprocessDataExist.Count > 0)
            {
                return insertSuccess = true;
            }
            _preProcessDTO.DataTransformationApplied = true;
            _preProcessDTO.CorrelationId = correlationId;
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            string processData = string.Empty;

            var dataCleanupCollection = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataCleanup);
            var filterCollection = _database.GetCollection<BsonDocument>(CONSTANTS.DataCleanUPFilteredData);
            var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.FeatureName).Exclude(CONSTANTS.Id);
            var filteredData = new List<BsonDocument>();
            List<string> columnsList = new List<string>();
            List<string> categoricalColumns = new List<string>();
            List<string> missingColumns = new List<string>();
            List<string> numericalColumns = new List<string>();
            JObject serializeData = new JObject();

            filteredData = dataCleanupCollection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (filteredData.Count > 0)
            {
                if (_DBEncryptionRequired)
                {
                    if (_IsAESKeyVault)
                        filteredData[0][CONSTANTS.FeatureName] = BsonDocument.Parse(CryptographyUtility.Decrypt(filteredData[0][CONSTANTS.FeatureName].ToString()));
                    else
                        filteredData[0][CONSTANTS.FeatureName] = BsonDocument.Parse(AesProvider.Decrypt(filteredData[0][CONSTANTS.FeatureName].ToString(), _aesKey, _aesVector));

                }
                serializeData = JObject.Parse(filteredData[0].ToString());
                //Taking all the Columns
                foreach (var features in serializeData[CONSTANTS.FeatureName].Children())
                {
                    JProperty j = features as JProperty;
                    columnsList.Add(j.Name);
                }
                //Get the Categorical Columns and Numerical Columns
                foreach (var item in columnsList)
                {
                    foreach (JToken attributes in serializeData[CONSTANTS.FeatureName][item][CONSTANTS.Datatype].Children())
                    {
                        var property = attributes as JProperty;
                        var missingValues = serializeData[CONSTANTS.FeatureName][item][CONSTANTS.Missing_Values];
                        double value = (double)missingValues;
                        if (property != null && property.Name == CONSTANTS.category && property.Value.ToString() == CONSTANTS.True)
                        {
                            categoricalColumns.Add(item);
                            if (value > 0)
                                missingColumns.Add(item);
                        }
                        if (property != null && (property.Name == CONSTANTS.float64 || property.Name == CONSTANTS.int64) && property.Value.ToString() == CONSTANTS.True)
                        {
                            if (value > 0)
                                numericalColumns.Add(item);
                        }
                    }
                }
                //Get DataModificationData
                GetModifications(correlationId);
                //Getting the Data Encoding Data
                GetDataEncodingValues(categoricalColumns, serializeData);

                //This code for filters to be applied
                var uniqueValueProjection = Builders<BsonDocument>.Projection.Include(CONSTANTS.ColumnUniqueValues).Include(CONSTANTS.target_variable).Exclude(CONSTANTS.Id);
                var filteredResult = filterCollection.Find(filter).Project<BsonDocument>(uniqueValueProjection).ToList();
                JObject uniqueData = new JObject();
                if (filteredResult.Count > 0)
                {
                    if (_DBEncryptionRequired)
                    {
                        if (_IsAESKeyVault)
                            filteredResult[0][CONSTANTS.ColumnUniqueValues] = BsonDocument.Parse(CryptographyUtility.Decrypt(filteredResult[0][CONSTANTS.ColumnUniqueValues].ToString()));
                        else
                            filteredResult[0][CONSTANTS.ColumnUniqueValues] = BsonDocument.Parse(AesProvider.Decrypt(filteredResult[0][CONSTANTS.ColumnUniqueValues].ToString(), _aesKey, _aesVector));
                    }

                    _preProcessDTO.TargetColumn = filteredResult[0][CONSTANTS.target_variable].ToString();
                    uniqueData = JObject.Parse(filteredResult[0].ToString());
                    //Getting the Missing Values and Filters Data
                    GetMissingAndFiltersData(missingColumns, categoricalColumns, numericalColumns, uniqueData);
                    InsertToPreprocess(preProcessModel);
                }
            }
            return insertSuccess;
        }

        private List<BsonDocument> GetPreprocessExistData(string correlationId)
        {
            _preProcessDTO.CorrelationId = correlationId;
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var prePropcessProjection = Builders<BsonDocument>.Projection.Include(CONSTANTS.Filters).Include(CONSTANTS.MissingValues).Include(CONSTANTS.DataEncoding).Include(CONSTANTS.DataModification).Include(CONSTANTS.DataTransformationApplied).Include(CONSTANTS.TargetColumn).Exclude(CONSTANTS.Id);
            var dataPreprocessCollection = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataProcessing);
            var preprocessDataExist = dataPreprocessCollection.Find(filter).Project<BsonDocument>(prePropcessProjection).ToList();
            return preprocessDataExist;
        }

        private void GetModifications(string correlationId)
        {

            List<string> binningcolumnsList = new List<string>();
            List<string> recommendedcolumnsList = new List<string>();
            List<string> columnsList = new List<string>();
            Dictionary<string, Dictionary<string, Dictionary<string, string>>> recommendedColumns = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();

            Dictionary<string, Dictionary<string, Dictionary<string, string>>> columnBinning = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();
            Dictionary<string, string> prescriptionData = new Dictionary<string, string>();
            JObject serializeData = new JObject();


            var dataCleanupCollection = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataCleanup);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.FeatureName).Exclude(CONSTANTS.Id);
            var filteredData = dataCleanupCollection.Find(filter).Project<BsonDocument>(projection).ToList();

            if (filteredData.Count > 0)
            {
                if (_DBEncryptionRequired)
                {
                    if (_IsAESKeyVault)
                        filteredData[0][CONSTANTS.FeatureName] = BsonDocument.Parse(CryptographyUtility.Decrypt(filteredData[0][CONSTANTS.FeatureName].ToString()));
                    else
                        filteredData[0][CONSTANTS.FeatureName] = BsonDocument.Parse(AesProvider.Decrypt(filteredData[0][CONSTANTS.FeatureName].ToString(), _aesKey, _aesVector));
                }

                serializeData = JObject.Parse(filteredData[0].ToString());
                //Taking all the Columns
                var featureExist = serializeData[CONSTANTS.FeatureName];
                if (featureExist != null)
                {
                    foreach (var features in serializeData[CONSTANTS.FeatureName].Children())
                    {
                        JProperty j = features as JProperty;
                        columnsList.Add(j.Name);
                    }
                    foreach (var item in columnsList)
                    {
                        Dictionary<string, Dictionary<string, string>> binningColumns2 = new Dictionary<string, Dictionary<string, string>>();
                        Dictionary<string, Dictionary<string, string>> binningColumns3 = new Dictionary<string, Dictionary<string, string>>();
                        Dictionary<string, string> removeImbalancedColumns = new Dictionary<string, string>();

                        Dictionary<string, string> outlier = new Dictionary<string, string>();
                        Dictionary<string, string> skeweness = new Dictionary<string, string>();
                        Dictionary<string, Dictionary<string, string>> fields = new Dictionary<string, Dictionary<string, string>>();
                        var outData = serializeData[CONSTANTS.FeatureName][item][CONSTANTS.Outlier];
                        var skewData = serializeData[CONSTANTS.FeatureName][item][CONSTANTS.Skewness];
                        float outValue = (float)outData;
                        string skewValue = (string)skewData;
                        var imbalanced = serializeData[CONSTANTS.FeatureName][item][CONSTANTS.ImBalanced];
                        string imbalancedValue = (string)imbalanced;
                        if (imbalancedValue == CONSTANTS.One)
                        {
                            JProperty jProperty1 = null;
                            string recommendation = string.Format(CONSTANTS.Recommendation, item);
                            var imbalancedColumns = serializeData[CONSTANTS.FeatureName][item][CONSTANTS.BinningValues];

                            foreach (var child1 in imbalancedColumns.Children())
                            {
                                Dictionary<string, string> binningColumns1 = new Dictionary<string, string>();
                                jProperty1 = child1 as JProperty;
                                foreach (var child2 in jProperty1.Children())
                                {
                                    if (child2 != null)
                                    {
                                        binningColumns1.Add(CONSTANTS.SubCatName, child2[CONSTANTS.SubCatName].ToString().Trim());
                                        binningColumns1.Add(CONSTANTS.Value, child2[CONSTANTS.Value].ToString().Trim());
                                        List<string> list = new List<string> { CONSTANTS.Binning, CONSTANTS.NewName };
                                        foreach (var binning in list)
                                        {
                                            binningColumns1.Add(binning, CONSTANTS.False);
                                        }
                                        binningColumns2.Add(jProperty1.Name, binningColumns1);
                                    }
                                }
                            }
                            if (binningColumns2.Count > 0)
                            {
                                for (int i = 0; i < 2; i++)
                                {
                                    Dictionary<string, string> dict = new Dictionary<string, string>();
                                    if (i == 0)
                                    {
                                        dict.Add(CONSTANTS.ChangeRequest, CONSTANTS.InvertedComma);
                                        binningColumns2.Add(CONSTANTS.ChangeRequest, dict);
                                    }
                                    else
                                    {
                                        dict.Add(CONSTANTS.PChangeRequest, CONSTANTS.InvertedComma);
                                        binningColumns2.Add(CONSTANTS.PChangeRequest, dict);
                                    }
                                }
                            }
                            columnBinning.Add(item, binningColumns2);
                        }
                        else if (imbalancedValue == CONSTANTS.Two)
                        {
                            string removeColumndesc = string.Format(CONSTANTS.StringFormat, item);
                            removeImbalancedColumns.Add(item, removeColumndesc);
                        }
                        else if (imbalancedValue == CONSTANTS.Three)
                        {
                            string prescription = string.Format(CONSTANTS.StringFormat1, item);
                            prescriptionData.Add(item, prescription);
                        }
                        if (prescriptionData.Count > 0)
                            _preProcessDTO.Prescriptions = prescriptionData;

                        if (outValue > 0)
                        {
                            string strForm = string.Format(CONSTANTS.StringFormat2, item, outValue);
                            outlier.Add(CONSTANTS.Text, strForm);
                            string[] outliers = { CONSTANTS.Mean, CONSTANTS.Median, CONSTANTS.Mode, CONSTANTS.CustomValue, CONSTANTS.ChangeRequest, CONSTANTS.PChangeRequest };
                            for (int i = 0; i < outliers.Length; i++)
                            {
                                if (i == 3)
                                {
                                    outlier.Add(outliers[i], CONSTANTS.InvertedComma);
                                }
                                else if (i == 4 || i == 5)
                                {
                                    outlier.Add(outliers[i], CONSTANTS.InvertedComma);
                                }
                                else
                                {
                                    outlier.Add(outliers[i], CONSTANTS.False);
                                }
                            }
                        }
                        if (skewValue == CONSTANTS.Yes)
                        {
                            string strForm = string.Format(CONSTANTS.StringFormat3, item);
                            skeweness.Add(CONSTANTS.Skeweness, strForm);
                            string[] skewnessArray = { CONSTANTS.BoxCox, CONSTANTS.Reciprocal, CONSTANTS.Log, CONSTANTS.ChangeRequest, CONSTANTS.PChangeRequest };
                            for (int i = 0; i < skewnessArray.Length; i++)
                            {
                                if (i == 3 || i == 4)
                                {
                                    skeweness.Add(skewnessArray[i], CONSTANTS.InvertedComma);
                                }
                                else
                                {
                                    skeweness.Add(skewnessArray[i], CONSTANTS.False);
                                }
                            }
                        }
                        if (outlier.Count > 0)
                            fields.Add(CONSTANTS.Outlier, outlier);
                        if (skeweness.Count > 0)
                            fields.Add(CONSTANTS.Skewness, skeweness);
                        if (removeImbalancedColumns.Count > 0)
                            fields.Add(CONSTANTS.RemoveColumn, removeImbalancedColumns);

                        if (fields.Count > 0)
                        {
                            recommendedColumns.Add(item, fields);
                        }
                    }
                }
                if (columnBinning.Count > 0)
                    _preProcessDTO.ColumnBinning = columnBinning;
                if (recommendedColumns.Count > 0)
                    _preProcessDTO.RecommendedColumns = recommendedColumns;
            }
        }

        private void GetDataEncodingValues(List<string> categoricalColumns, JObject serializeData)
        {
            var encodingData = new Dictionary<string, Dictionary<string, string>>();
            foreach (var column in categoricalColumns)
            {
                var dataEncodingData = new Dictionary<string, string>();
                foreach (JToken scale in serializeData[CONSTANTS.FeatureName][column][CONSTANTS.Scale].Children())
                {
                    if (scale is JProperty property && property.Value.ToString() == CONSTANTS.True)
                    {
                        dataEncodingData.Add(CONSTANTS.Attribute, property.Name);
                        dataEncodingData.Add(CONSTANTS.encoding, CONSTANTS.LabelEncoding);
                    }
                }
                if (dataEncodingData.Count > 0)
                {
                    dataEncodingData.Add(CONSTANTS.ChangeRequest, CONSTANTS.True);
                    dataEncodingData.Add(CONSTANTS.PChangeRequest, CONSTANTS.InvertedComma);
                    encodingData.Add(column, dataEncodingData);
                }
            }
            _preProcessDTO.DataEncodeData = encodingData;
        }
        private void GetMissingAndFiltersData(List<string> missingColumns, List<string> categoricalColumns, List<string> numericalColumns, JObject uniqueData)
        {
            var missingData = new Dictionary<string, Dictionary<string, string>>();
            var categoricalDictionary = new Dictionary<string, Dictionary<string, string>>();
            var missingDictionary = new Dictionary<string, Dictionary<string, string>>();
            var dataNumerical = new Dictionary<string, Dictionary<string, string>>();
            //Get Filters of ModelTemplate
            var dataProcessingCollection = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataProcessing);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, _Mapping.UsecaseID);
            var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.Filters).Exclude(CONSTANTS.Id);
            var filteredData = dataProcessingCollection.Find(filter).Project<BsonDocument>(projection).ToList();
            Dictionary<string, string> templateFilters = new Dictionary<string, string>();
            Dictionary<string, Dictionary<string, string>> templateFilterValue = new Dictionary<string, Dictionary<string, string>>();
            var deployCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
            var filterBuilder = Builders<BsonDocument>.Filter;
            var projection1 = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
            var Sourcenamefilter = filterBuilder.Eq(CONSTANTS.CorrelationId, _Mapping.UsecaseID);
            var SourceData = deployCollection.Find(Sourcenamefilter).Project<BsonDocument>(projection1).ToList();
            BsonElement element;
            bool isTemplateDbEncrypted = false;
            var exists = SourceData[0].TryGetElement("DBEncryptionRequired", out element);
            if (exists)
                isTemplateDbEncrypted = (bool)SourceData[0]["DBEncryptionRequired"];
            else
                isTemplateDbEncrypted = false;
            foreach (var column in categoricalColumns)
            {
                bool isFilterApplied = false;
                bool isChangeRequest = false;
                Dictionary<string, string> fieldDictionary = new Dictionary<string, string>();
                foreach (JToken value in uniqueData[CONSTANTS.ColumnUniqueValues][column].Children())
                {
                    if (!string.IsNullOrEmpty(Convert.ToString(value)))
                    {
                        if (filteredData.Count > 0)
                        {
                            foreach (var item in filteredData)
                            {
                                var serializeData = new JObject();
                                if (isTemplateDbEncrypted)
                                {
                                    if (_IsAESKeyVault)
                                        serializeData = JObject.Parse(BsonDocument.Parse(CryptographyUtility.Decrypt(filteredData[0][CONSTANTS.Filters].AsString)).ToString());
                                    else
                                        serializeData = JObject.Parse(BsonDocument.Parse(AesProvider.Decrypt(filteredData[0][CONSTANTS.Filters].AsString, _aesKey, _aesVector)).ToString());
                                }
                                else
                                {
                                    serializeData = JObject.Parse(filteredData[0][CONSTANTS.Filters].ToString());
                                }
                                //Taking Filter Columns
                                if (serializeData[column] != null)
                                {
                                    foreach (var newFilter in serializeData[column].Children())
                                    {
                                        var i = newFilter as JProperty;
                                        //Check if the modelTemplate has Filter applied True
                                        if (Convert.ToString(value) == Convert.ToString(i.Name) && Convert.ToString(i.Value) == "True")
                                        {
                                            isFilterApplied = true;
                                            break;
                                        }
                                        //Check if the modelTemplate has Filter applied False
                                        else if (Convert.ToString(value) == Convert.ToString(i.Name) && Convert.ToString(i.Value) == "False")
                                        {
                                            isFilterApplied = false;
                                            break;
                                        }
                                        else
                                        {
                                            isFilterApplied = false;
                                        }
                                    }
                                }
                                if (isFilterApplied)
                                {
                                    // Training request Filter set to True as per the ModelTemplate
                                    isChangeRequest = true;
                                    fieldDictionary.Add(value.ToString().Replace(CONSTANTS.Dot, CONSTANTS.u2024).Replace(CONSTANTS.r_n, CONSTANTS.EmptySpace).Replace(CONSTANTS.t, CONSTANTS.EmptySpace), CONSTANTS.True);
                                }
                                else
                                {
                                    // Training request Filter set to False as per the ModelTemplate
                                    fieldDictionary.Add(value.ToString().Replace(CONSTANTS.Dot, CONSTANTS.u2024).Replace(CONSTANTS.r_n, CONSTANTS.EmptySpace).Replace(CONSTANTS.t, CONSTANTS.EmptySpace), CONSTANTS.False);
                                }

                            }
                        }
                        else
                        {
                            fieldDictionary.Add(value.ToString().Replace(CONSTANTS.Dot, CONSTANTS.u2024).Replace(CONSTANTS.r_n, CONSTANTS.EmptySpace).Replace(CONSTANTS.t, CONSTANTS.EmptySpace), CONSTANTS.False);
                        }
                    }
                }
                if (fieldDictionary.Count > 0)
                {
                    string changeRequestValue = CONSTANTS.InvertedComma;
                    if (isChangeRequest)
                    {
                        changeRequestValue = CONSTANTS.True;
                    }
                    fieldDictionary.Add(CONSTANTS.ChangeRequest, changeRequestValue);
                    fieldDictionary.Add(CONSTANTS.PChangeRequest, CONSTANTS.InvertedComma);
                    categoricalDictionary.Add(column, fieldDictionary);
                }
            }
            _preProcessDTO.CategoricalData = categoricalDictionary;

            foreach (var column in missingColumns)
            {
                int i = 0;
                Dictionary<string, string> fieldDictionary = new Dictionary<string, string>();
                foreach (JToken value in uniqueData[CONSTANTS.ColumnUniqueValues][column].Children())
                {
                    if (!string.IsNullOrEmpty(Convert.ToString(value)))
                        if (i == 0)
                        {
                            fieldDictionary.Add(value.ToString().Replace(CONSTANTS.Dot, CONSTANTS.u2024).Replace(CONSTANTS.r_n, CONSTANTS.EmptySpace).Replace(CONSTANTS.t, CONSTANTS.EmptySpace), CONSTANTS.True);
                        }
                        else
                        {
                            fieldDictionary.Add(value.ToString().Replace(CONSTANTS.Dot, CONSTANTS.u2024).Replace(CONSTANTS.r_n, CONSTANTS.EmptySpace).Replace(CONSTANTS.t, CONSTANTS.EmptySpace), CONSTANTS.False);
                        }
                    i++;
                }
                if (fieldDictionary.Count > 0)
                {
                    fieldDictionary.Add(CONSTANTS.ChangeRequest, CONSTANTS.True);
                    fieldDictionary.Add(CONSTANTS.PChangeRequest, CONSTANTS.InvertedComma);
                    fieldDictionary.Add(CONSTANTS.CustomValue, CONSTANTS.InvertedComma);
                    missingData.Add(column, fieldDictionary);
                }

            }
            _preProcessDTO.MisingValuesData = missingData;

            //Numerical Columns Fetching data

            Dictionary<string, string> numericalDictionary = new Dictionary<string, string>();
            string[] numericalValues = { CONSTANTS.Mean, CONSTANTS.Median, CONSTANTS.Mode, CONSTANTS.CustomValue };
            foreach (var column in numericalColumns)
            {
                var value = uniqueData[CONSTANTS.ColumnUniqueValues][column];
                var numericDictionary = new Dictionary<string, string>();
                if (!string.IsNullOrEmpty(Convert.ToString(value)))
                {
                    foreach (var numericColumnn in numericalValues)
                    {
                        if (numericColumnn == CONSTANTS.CustomValue)
                        {
                            numericDictionary.Add(numericColumnn, CONSTANTS.InvertedComma);
                        }
                        else
                        {
                            if (numericColumnn == CONSTANTS.Mean)
                            {
                                numericDictionary.Add(numericColumnn, CONSTANTS.True);
                            }
                            else
                            {
                                numericDictionary.Add(numericColumnn, CONSTANTS.False);
                            }

                        }
                    }
                    if (numericDictionary.Count > 0)
                    {
                        numericDictionary.Add(CONSTANTS.ChangeRequest, CONSTANTS.True);
                        numericDictionary.Add(CONSTANTS.PChangeRequest, CONSTANTS.InvertedComma);
                        dataNumerical.Add(column, numericDictionary);
                    }
                }
            }
            _preProcessDTO.NumericalData = dataNumerical;
        }

        private void InsertToPreprocess(PreProcessModelDTO preProcessModel)
        {
            string categoricalJson = string.Empty;
            string missingValuesJson = string.Empty;
            string numericJson = string.Empty;
            string dataEncodingJson = string.Empty;
            if (_preProcessDTO.CategoricalData != null || _preProcessDTO.NumericalData != null || _preProcessDTO.DataEncodeData != null || _preProcessDTO.ColumnBinning != null)
            {
                JObject outlierData = new JObject();
                JObject prescriptionData = new JObject();
                JObject binningData = new JObject();
                //DataModification Insertion Format Start
                var recommendedColumnsData = Newtonsoft.Json.JsonConvert.SerializeObject(_preProcessDTO.RecommendedColumns);
                if (!string.IsNullOrEmpty(recommendedColumnsData) && recommendedColumnsData != CONSTANTS.Null)
                    outlierData = JObject.Parse(recommendedColumnsData);
                var columnBinning = Newtonsoft.Json.JsonConvert.SerializeObject(_preProcessDTO.ColumnBinning);
                if (!string.IsNullOrEmpty(columnBinning) && columnBinning != CONSTANTS.Null)
                    binningData = JObject.Parse(columnBinning);
                JObject binningObject = new JObject();
                if (binningData != null)
                    binningObject[CONSTANTS.ColumnBinning] = JObject.FromObject(binningData);

                var prescription = Newtonsoft.Json.JsonConvert.SerializeObject(_preProcessDTO.Prescriptions);
                if (!string.IsNullOrEmpty(prescription) && prescription != CONSTANTS.Null)
                    prescriptionData = JObject.Parse(prescription);
                //DataModification Insertion Format End

                categoricalJson = Newtonsoft.Json.JsonConvert.SerializeObject(_preProcessDTO.CategoricalData);
                missingValuesJson = Newtonsoft.Json.JsonConvert.SerializeObject(_preProcessDTO.MisingValuesData);
                numericJson = Newtonsoft.Json.JsonConvert.SerializeObject(_preProcessDTO.NumericalData);
                dataEncodingJson = Newtonsoft.Json.JsonConvert.SerializeObject(_preProcessDTO.DataEncodeData);

                JObject missingValuesObject = new JObject();
                JObject categoricalObject = new JObject();
                JObject numericObject = new JObject();
                JObject encodedData = new JObject();
                if (!string.IsNullOrEmpty(categoricalJson) && categoricalJson != CONSTANTS.Null)
                    categoricalObject = JObject.Parse(categoricalJson);
                if (!string.IsNullOrEmpty(numericJson) && numericJson != CONSTANTS.Null)
                    numericObject = JObject.Parse(numericJson);
                if (!string.IsNullOrEmpty(missingValuesJson) && missingValuesJson != null)
                    missingValuesObject = JObject.Parse(missingValuesJson);
                missingValuesObject.Merge(numericObject, new Newtonsoft.Json.Linq.JsonMergeSettings
                {
                    MergeArrayHandling = Newtonsoft.Json.Linq.MergeArrayHandling.Union
                });
                if (!string.IsNullOrEmpty(dataEncodingJson))
                    encodedData = JObject.Parse(dataEncodingJson);

                Dictionary<string, string> smoteFlags = new Dictionary<string, string>();
                smoteFlags.Add(CONSTANTS.Flag, CONSTANTS.False);
                smoteFlags.Add(CONSTANTS.ChangeRequest, CONSTANTS.False);
                smoteFlags.Add(CONSTANTS.PChangeRequest, CONSTANTS.False);

                var smoteTest = Newtonsoft.Json.JsonConvert.SerializeObject(smoteFlags);
                JObject smoteData = new JObject();
                smoteData = JObject.Parse(smoteTest);

                JObject processData = new JObject
                {
                    [CONSTANTS.Id] = Guid.NewGuid(),
                    [CONSTANTS.CorrelationId] = _preProcessDTO.CorrelationId
                };
                if (!string.IsNullOrEmpty(_preProcessDTO.Flag))
                    _preProcessDTO.Flag = CONSTANTS.False;
                processData[CONSTANTS.Flag] = _preProcessDTO.Flag;
                //Removing the Target column having lessthan 2 values..important
                bool removeTargetColumn = false;
                if (categoricalObject != null && categoricalObject.ToString() != CONSTANTS.CurlyBraces)
                {
                    if (categoricalObject[_preProcessDTO.TargetColumn] != null)
                    {
                        if (categoricalObject[_preProcessDTO.TargetColumn].Children().Count() <= 4)
                        {
                            removeTargetColumn = true;
                        }
                        if (removeTargetColumn)
                        {
                            JObject header = (JObject)categoricalObject;
                            header.Property(_preProcessDTO.TargetColumn).Remove();
                        }
                    }
                }

                processData[CONSTANTS.Filters] = JObject.FromObject(categoricalObject);
                if (missingValuesObject != null)
                    processData[CONSTANTS.MissingValues] = JObject.FromObject(missingValuesObject);
                if (encodedData != null)
                    processData[CONSTANTS.DataEncoding] = JObject.FromObject(encodedData);
                if (binningObject != null)
                    processData[CONSTANTS.DataModification] = JObject.FromObject(binningObject);
                if (outlierData != null)
                    processData[CONSTANTS.DataModification][CONSTANTS.Features] = JObject.FromObject(outlierData);
                if (prescriptionData != null)
                    processData[CONSTANTS.DataModification][CONSTANTS.Prescriptions] = JObject.FromObject(prescriptionData);
                processData[CONSTANTS.TargetColumn] = _preProcessDTO.TargetColumn;
                JObject InterpolationObject = new JObject();
                processData[CONSTANTS.DataModification][CONSTANTS.Features][CONSTANTS.Interpolation] = JObject.FromObject(InterpolationObject);
                if (preProcessModel.ModelType == CONSTANTS.TimeSeries)
                    processData[CONSTANTS.DataModification][CONSTANTS.Features][CONSTANTS.Interpolation] = CONSTANTS.Linear;
                if (_textdata != null)
                {
                    processData[CONSTANTS.DataModification][CONSTANTS.TextDataPreprocessing] = _textdata;
                }
                processData[CONSTANTS.DataModification][CONSTANTS.NewAddFeatures] = CONSTANTS.InvertedComma;
                processData[CONSTANTS.Smote] = smoteData;
                //processData[CONSTANTS.InstaId] = useCaseId; // need to check usecaseid updated
                processData[CONSTANTS.DataTransformationApplied] = _preProcessDTO.DataTransformationApplied;
                processData[CONSTANTS.CreatedOn] = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                processData[CONSTANTS.ModifiedOn] = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);

                var collection = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataProcessing);
                if (_DBEncryptionRequired)
                {
                    if (_IsAESKeyVault)
                    {
                        processData[CONSTANTS.DataModification] = CryptographyUtility.Encrypt(processData[CONSTANTS.DataModification].ToString(Formatting.None));
                        processData[CONSTANTS.MissingValues] = CryptographyUtility.Encrypt(processData[CONSTANTS.MissingValues].ToString(Formatting.None));
                        processData[CONSTANTS.Filters] = CryptographyUtility.Encrypt(processData[CONSTANTS.Filters].ToString(Formatting.None));
                    }
                    else
                    {
                        processData[CONSTANTS.DataModification] = AesProvider.Encrypt(processData[CONSTANTS.DataModification].ToString(Formatting.None), _aesKey, _aesVector);
                        processData[CONSTANTS.MissingValues] = AesProvider.Encrypt(processData[CONSTANTS.MissingValues].ToString(Formatting.None), _aesKey, _aesVector);
                        processData[CONSTANTS.Filters] = AesProvider.Encrypt(processData[CONSTANTS.Filters].ToString(Formatting.None), _aesKey, _aesVector);
                    }

                }
                var insertdoc = BsonSerializer.Deserialize<BsonDocument>(processData.ToString());
                collection.InsertOne(insertdoc);
                insertSuccess = true;
            }
        }

        private DataEngineering GetDatatransformation(string correlationId, string pageInfo, string userId, string AppID)
        {
            bool callMethod = true;
            DataEngineering dataEngineering = new DataEngineering();
            string PythonResult = string.Empty;
            while (callMethod)
            {
                Thread.Sleep(2000);
                var useCaseData = CheckPythonProcess(correlationId, CONSTANTS.DataPreprocessing);
                if (!string.IsNullOrEmpty(useCaseData))
                {
                    JObject queueData = JObject.Parse(useCaseData);
                    dataEngineering.Status = (string)queueData[CONSTANTS.Status];
                    dataEngineering.Progress = (string)queueData[CONSTANTS.Progress];
                    dataEngineering.Message = (string)queueData[CONSTANTS.Message];
                    if (dataEngineering.Status == CONSTANTS.C & dataEngineering.Progress == CONSTANTS.Hundred)
                    {
                        callMethod = false;
                        dataEngineering.IsComplete = true;
                        return dataEngineering;
                    }
                    if (dataEngineering.Status == CONSTANTS.E)
                    {
                        callMethod = false;
                        dataEngineering.IsComplete = false;
                        return dataEngineering;
                    }
                }
                else
                {
                    IngrainRequestQueue ingrainRequest = new IngrainRequestQueue
                    {
                        _id = Guid.NewGuid().ToString(),
                        CorrelationId = correlationId,
                        RequestId = Guid.NewGuid().ToString(),
                        ProcessId = CONSTANTS.Null,
                        Status = CONSTANTS.Null,
                        ModelName = CONSTANTS.Null,
                        RequestStatus = CONSTANTS.New,
                        RetryCount = 0,
                        ProblemType = CONSTANTS.Null,
                        Message = CONSTANTS.Null,
                        UniId = CONSTANTS.Null,
                        Progress = CONSTANTS.Null,
                        pageInfo = CONSTANTS.DataPreprocessing,
                        ParamArgs = CONSTANTS.CurlyBraces,
                        Function = CONSTANTS.DataTransform,
                        CreatedByUser = userId,
                        CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                        ModifiedByUser = userId,
                        ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                        LastProcessedOn = CONSTANTS.Null,
                        AppID = AppID,
                        ClientId = _templateInfo.ClientUID,
                        DeliveryconstructId = _templateInfo.DCID
                    };
                    InsertRequests(ingrainRequest);
                }
            }
            return dataEngineering;
        }

        private void StartModelEngineering(string correlationId, string userId, string ProblemType, string AppID)
        {
            //UpdateMEFeatureSelection(_Mapping.UsecaseID, correlationId);
            //Thread.Sleep(1000);
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(StartModelEngineering), "StartModelEngineering Started", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), AppID, string.Empty, string.Empty, string.Empty);
            string pythonResult = string.Empty;
            bool isModelTrained = true;
            try
            {

                while (isModelTrained)
                {
                    int SuccessCount = 0;
                    int errorCount = 0;
                    int noOfModelsSelected = 0;

                    var useCaseDetails = GetMultipleRequestStatus(correlationId, CONSTANTS.RecommendedAI);
                    if (useCaseDetails.Count > 0)
                    {
                        DateTime dateTime = DateTime.Parse(useCaseDetails[0].CreatedOn);
                        DateTime currentTime = DateTime.Parse(DateTime.Now.ToString(CONSTANTS.DateHoursFormat));
                        _timeDiffInMinutes = (currentTime - dateTime).TotalMinutes;
                        noOfModelsSelected = useCaseDetails.Count;
                        List<int> progressList = new List<int>();
                        for (int i = 0; i < useCaseDetails.Count; i++)
                        {

                            string queueStatus = useCaseDetails[i].Status;
                            if (queueStatus == CONSTANTS.C)
                            {
                                SuccessCount++;
                            }
                            if (queueStatus == CONSTANTS.E)
                            {
                                errorCount++;
                            }
                        }
                        if (errorCount == noOfModelsSelected)
                        {
                            isModelTrained = false;
                            _GenericDataResponse.Status = CONSTANTS.E;
                            _GenericDataResponse.ErrorMessage = Resource.IngrainResx.ModelsFailedTraining;
                        }
                        //If appsettings "ModelsTrainingTimeLimit" time exceeds the current model training and atleast one model training completed
                        //than we can kill remaining processes and we can show the completed models at UI
                        else if (_timeDiffInMinutes > _modelsTrainingTimeLimit && SuccessCount > 0)
                        {
                            //Kill the already existing process of the python
                            //insert the request to terminate the python process for the remaining inprgress models.
                            TerminateModelsTrainingRequests(correlationId, useCaseDetails);
                            isModelTrained = false;
                            _GenericDataResponse.Status = CONSTANTS.C;
                            _GenericDataResponse.Message = Resource.IngrainResx.ModelEngineeringcompleted;
                            //End
                        }
                        else if (SuccessCount + errorCount == noOfModelsSelected)
                        {
                            isModelTrained = false;
                            _GenericDataResponse.Status = CONSTANTS.C;
                            _GenericDataResponse.Message = Resource.IngrainResx.ModelEngineeringcompleted;
                        }

                        Thread.Sleep(2000);
                    }
                    //else
                    //{
                    //    IngrainRequestQueue requestQueue = new IngrainRequestQueue();
                    //    this.UpdateRecommendedModels(correlationId, ProblemType);
                    //    Thread.Sleep(1000);
                    //    var recommendedModels = GetModelNames(correlationId);
                    //    foreach (var modelName in recommendedModels.Item1)
                    //    {
                    //        IngrainRequestQueue ingrainRequest = new IngrainRequestQueue
                    //        {
                    //            _id = Guid.NewGuid().ToString(),
                    //            CorrelationId = correlationId,
                    //            RequestId = Guid.NewGuid().ToString(),
                    //            ProcessId = CONSTANTS.Null,
                    //            Status = CONSTANTS.Null,
                    //            ModelName = modelName,
                    //            RequestStatus = CONSTANTS.New,
                    //            RetryCount = 0,
                    //            ProblemType = recommendedModels.Item2,
                    //            Message = CONSTANTS.Null,
                    //            UniId = CONSTANTS.Null,
                    //            TemplateUseCaseID = _Mapping.UsecaseID,
                    //            Progress = CONSTANTS.Null,
                    //            pageInfo = CONSTANTS.RecommendedAI,
                    //            ParamArgs = CONSTANTS.CurlyBraces,
                    //            Function = CONSTANTS.RecommendedAI,
                    //            CreatedByUser = userId,
                    //            CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                    //            ModifiedByUser = userId,
                    //            ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                    //            LastProcessedOn = CONSTANTS.Null,
                    //            AppID = AppID,
                    //            ClientId = _templateInfo.ClientUID,
                    //            DeliveryconstructId = _templateInfo.DCID,

                    //        };
                    //        // LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(StartModelEngineering), CONSTANTS.BeforeInsertRequest, new Guid(correlationId));
                    //        InsertRequests(ingrainRequest);
                    //    }
                    //    Thread.Sleep(2000);
                    //}
                }

            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(GenericAutotrainService), nameof(StartModelEngineering), ex.Message + "***CorrelationId = " + correlationId + "***", ex, AppID, string.Empty, string.Empty, string.Empty);
                _GenericDataResponse.ErrorMessage = ex.Message + CONSTANTS.ThreeDots + ex.StackTrace;
                _GenericDataResponse.Status = CONSTANTS.E;
                _GenericDataResponse.ErrorMessage = Resource.IngrainResx.ErrorModelEngineering;

                CustomAppsActivityLog log = new CustomAppsActivityLog();
                log.CorrelationId = correlationId;
                log.FeatureName = "PrivateModelTraining";
                log.StackTrace = ex.StackTrace;
                log.ErrorMessage = ex.Message;
                log.ErrorMethod = "StartModelEngineering";
                log.CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                log.CreatedBy = "SYSTEM";
                if (_templateInfo != null)
                {
                    log.ClientId = _templateInfo.ClientUID;
                    log.DCID = _templateInfo.DCID;
                    log.UseCaseId = _templateInfo.UseCaseID;
                    log.ApplicationID = _templateInfo.AppicationID;
                }

                this.InsertCustomAppsActivityLog(log);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(StartModelEngineering), "StartModelEngineering Ended", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), AppID, string.Empty, string.Empty, string.Empty);
            //return response;
        }

        private void UpdateMEFeatureSelection(string templateId, string correlationId)
        {
            var templateCollection = _database.GetCollection<BsonDocument>(CONSTANTS.MEFeatureSelection);
            var templateFilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, templateId);
            var templateProjection = Builders<BsonDocument>.Projection.Exclude("_id");
            var templateDetail = templateCollection.Find(templateFilter).Project<BsonDocument>(templateProjection).ToList();
            if (templateDetail.Count > 0)
            {
                BsonDocument template = templateDetail[0];
                bool allDataFlag = template.GetValue("AllData_Flag", false).AsBoolean;
                var modelCollection = _database.GetCollection<BsonDocument>(CONSTANTS.MEFeatureSelection);
                var modelFilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
                var modelProjection = Builders<BsonDocument>.Projection.Exclude("_id");
                var modelDetail = modelCollection.Find(modelFilter).Project<BsonDocument>(modelProjection).ToList();
                if (modelDetail.Count > 0)
                {
                    BsonDocument model = modelDetail[0];
                    if (model.Contains("AllData_Flag"))
                    {
                        modelCollection.UpdateOne(modelFilter, Builders<BsonDocument>.Update.Set("AllData_Flag", allDataFlag));
                    }
                    else
                    {
                        model.Add("AllData_Flag", allDataFlag);
                        modelCollection.DeleteOne(modelFilter);
                        modelCollection.InsertOne(model);
                    }
                }
            }

        }
        private Tuple<List<string>, string> GetModelNames(string correlationId)
        {
            List<string> modelNames = new List<string>();
            string problemType = string.Empty;
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.MERecommendedModels);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var project = Builders<BsonDocument>.Projection.Include(CONSTANTS.Selected_Models).Include(CONSTANTS.CorrelationId).Include(CONSTANTS.ProblemType).Exclude(CONSTANTS.Id);
            var result = collection.Find(filter).Project<BsonDocument>(project).ToList();
            JObject serializeData = new JObject();
            if (result.Count > 0)
            {
                serializeData = JObject.Parse(result[0].ToString());
                foreach (var selectedModels in serializeData[CONSTANTS.Selected_Models].Children())
                {
                    JProperty j = selectedModels as JProperty;
                    if (Convert.ToString(j.Value[CONSTANTS.Train_model]) == CONSTANTS.True)
                    {
                        modelNames.Add(j.Name);
                    }
                }

                problemType = result[0][CONSTANTS.ProblemType].ToString();
            }

            return Tuple.Create(modelNames, problemType);
        }
        private void UpdateRecommendedModels(string correlationId, string problemType)
        {
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.MERecommendedModels);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var projection = Builders<BsonDocument>.Projection.Include("SelectedModels").Exclude("_id");
            var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(UpdateRecommendedModels), "UpdateRecommendedModels : ", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            JObject recommendedObject = new JObject();
            if (result.Count > 0)
            {
                recommendedObject = JObject.Parse(result[0].ToString());
                foreach (var item in recommendedObject["SelectedModels"].Children())
                {
                    JProperty jProperty = item as JProperty;
                    if (jProperty != null)
                    {
                        var columnToUpdate = string.Format("SelectedModels.{0}.Train_model", jProperty.Name.ToString());
                        var updateModel = Builders<BsonDocument>.Update.Set(columnToUpdate, CONSTANTS.True);
                        collection.UpdateOne(filter, updateModel);
                    }
                }
            }
        }

        private void DeployModel(string correlationId, string userId, string ProblemType, string AppID)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(DeployModel), "DeployModel STARTED:" + ProblemType + "APPID--" + AppID, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), AppID, string.Empty, string.Empty, string.Empty);
            try
            {
                //Gets the records from SSAI_RecommendedTrainedModels collection for frequency & accuracy in order to update in deployed model collection
                _recommendedAI = this.GetTrainedModel(correlationId, ProblemType);

                ////Gets the records from SSAI_RecommendedTrainedModels collection for frequency & accuracy in order to update in deployed model collection

                LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(DeployModel), "RecommendedAI TrainModel Count :" + _recommendedAI.TrainedModel.Count, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), AppID, string.Empty, string.Empty, string.Empty);

                if (_recommendedAI != null && _recommendedAI.TrainedModel != null && _recommendedAI.TrainedModel.Count > 0)
                {
                    bool result = this.IsDeployModelComplete(correlationId, _recommendedAI, ProblemType);
                    if (result)
                    {
                        //  LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(DeployModel), "_GenericDataResponse Application name" + _GenericDataResponse.ApplicationName + "   UseCase Name" + _GenericDataResponse.UseCaseName);
                        _GenericDataResponse.ApplicationName = _GenericDataResponse.ApplicationName;
                        _GenericDataResponse.UseCaseName = _GenericDataResponse.UseCaseName;
                        _GenericDataResponse.Status = CONSTANTS.C;
                        _GenericDataResponse.Message = Resource.IngrainResx.DeployModelcompleted;
                    }
                    else
                    {
                        _GenericDataResponse.ApplicationName = _GenericDataResponse.ApplicationName;
                        _GenericDataResponse.UseCaseName = _GenericDataResponse.UseCaseName;
                        _GenericDataResponse.Status = CONSTANTS.E;
                        _GenericDataResponse.ErrorMessage = Resource.IngrainResx.NoRecordFound;
                    }
                }
                else
                {
                    _GenericDataResponse.Status = CONSTANTS.E;
                    _GenericDataResponse.ErrorMessage = Resource.IngrainResx.Nomodelstrained;
                }


            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(GenericAutotrainService), nameof(DeployModel), ex.Message, ex, AppID, string.Empty, string.Empty, string.Empty);
                _GenericDataResponse.ErrorMessage = ex.Message + CONSTANTS.ThreeDots + ex.StackTrace;
                _GenericDataResponse.Status = CONSTANTS.E;
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(DeployModel), "DeployModel Ended", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), AppID, string.Empty, string.Empty, string.Empty);
        }

        private RecommedAITrainedModel GetTrainedModel(string correlationId, string problemType)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(GetTrainedModel), "GetTrainedModel start", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            _recommendedAI = new RecommedAITrainedModel();
            _recommendedAI = this.GetRecommendedTrainedModels(correlationId);
            ////Gets the max accuracy from list of trained model based on problem type
            if (_recommendedAI != null && _recommendedAI.TrainedModel != null && _recommendedAI.TrainedModel.Count > 0)
            {
                double? maxAccuracy = null;
                switch (problemType)
                {
                    case CONSTANTS.Classification:
                        maxAccuracy = _recommendedAI.TrainedModel.Max(x => (double)x[CONSTANTS.Accuracy]);
                        _recommendedAI.TrainedModel = _recommendedAI.TrainedModel.Where(p => (double)p[CONSTANTS.Accuracy] == maxAccuracy).ToList();
                        break;

                    case CONSTANTS.Regression:
                    case CONSTANTS.TimeSeries:
                        List<JObject> trainModelsList = new List<JObject>();
                        RecommedAITrainedModel timeseriesmodel = new RecommedAITrainedModel();

                        var groups = _recommendedAI.TrainedModel.GroupBy(x => (string)x["Frequency"]);

                        foreach (var items in groups)
                        {
                            maxAccuracy = items.Max(x => (double)x[CONSTANTS.r2ScoreVal][CONSTANTS.error_rate]);
                            var model = items.Where(p => (double)p[CONSTANTS.r2ScoreVal][CONSTANTS.error_rate] == maxAccuracy).ToList();
                            timeseriesmodel.TrainedModel = model;
                            trainModelsList.Add(JObject.Parse(timeseriesmodel.TrainedModel[0].ToString()));
                        }

                        _recommendedAI.TrainedModel = trainModelsList;
                        break;
                }
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(GetTrainedModel), "GetTrainedModel END", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            return _recommendedAI;
        }
        private RecommedAITrainedModel GetRecommendedTrainedModels(string correlationId)
        {
            List<JObject> trainModelsList = new List<JObject>();
            RecommedAITrainedModel trainedModels = new RecommedAITrainedModel();
            var columnCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIRecommendedTrainedModels);
            var projection = Builders<BsonDocument>.Projection.Exclude("visualization");
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var trainedModel = columnCollection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (trainedModel.Count() > 0)
            {
                for (int i = 0; i < trainedModel.Count; i++)
                {
                    trainModelsList.Add(JObject.Parse(trainedModel[i].ToString()));
                }
                trainedModels.TrainedModel = trainModelsList;
            }

            return trainedModels;
        }

        private bool IsDeployModelComplete(string correlationId, RecommedAITrainedModel trainedModel, string problemType)
        {
            bool IsCompleted = false;
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var resultData = collection.Find(filter).ToList();
            string foreCastModel = appSettings.GetSection("AppSettings").GetSection("foreCastModel").Value;
            string publishURL = appSettings.GetSection("AppSettings").GetSection("publishURL").Value;
            if (resultData.Count > 0)
            {
                string[] linkedApps = new string[] { _Mapping.ApplicationName };
                if (problemType != CONSTANTS.TimeSeries)
                {
                    JObject data = JObject.Parse(trainedModel.TrainedModel[0].ToString());

                    var builder = Builders<BsonDocument>.Update;
                    var update = builder.Set(CONSTANTS.Accuracy, problemType == CONSTANTS.Classification ? (double)data[CONSTANTS.Accuracy] : (double)data[CONSTANTS.r2ScoreVal][CONSTANTS.error_rate])
                        .Set(CONSTANTS.ModelURL, string.Format(publishURL + CONSTANTS.Zero, correlationId))
                        .Set(CONSTANTS.LinkedApps, linkedApps)
                        .Set(CONSTANTS.Status, CONSTANTS.Deployed)
                        .Set(CONSTANTS.DeployedDate, DateTime.Now.ToString(CONSTANTS.DateHoursFormat))
                        .Set(CONSTANTS.IsPrivate, false)
                        .Set(CONSTANTS.IsModelTemplate, false)
                        .Set(CONSTANTS.ModelVersion, data[CONSTANTS.modelName].ToString());

                    collection.UpdateMany(filter, update);
                    IsCompleted = true;
                }
                else
                {
                    for (int i = 0; i < trainedModel.TrainedModel.Count(); i++)
                    {
                        JObject Timeseriesdata = JObject.Parse(trainedModel.TrainedModel[i].ToString());
                        var filterBuilder = Builders<BsonDocument>.Filter;
                        var TimeSeriesfilter = filterBuilder.Eq(CONSTANTS.CorrelationId, correlationId) & filterBuilder.Eq(CONSTANTS.Frequency, Timeseriesdata[CONSTANTS.Frequency].ToString());
                        var TimeSeriesresultData = collection.Find(filter).ToList();
                        if (TimeSeriesresultData.Count() > 0)
                        {

                            var builder = Builders<BsonDocument>.Update;
                            var update = builder.Set(CONSTANTS.Accuracy, problemType == CONSTANTS.Classification ? (double)Timeseriesdata[CONSTANTS.Accuracy] : (double)Timeseriesdata[CONSTANTS.r2ScoreVal][CONSTANTS.error_rate])
                                .Set(CONSTANTS.ModelURL, string.Format(foreCastModel, correlationId, Timeseriesdata[CONSTANTS.Frequency].ToString()))
                                .Set(CONSTANTS.LinkedApps, linkedApps)
                                .Set(CONSTANTS.Status, CONSTANTS.Deployed)
                                .Set(CONSTANTS.DeployedDate, DateTime.Now.ToString(CONSTANTS.DateHoursFormat))
                                .Set(CONSTANTS.IsPrivate, false)
                                .Set(CONSTANTS.IsModelTemplate, false)
                                .Set(CONSTANTS.ModelVersion, Timeseriesdata[CONSTANTS.modelName].ToString())
                                .Set(CONSTANTS.Frequency, Timeseriesdata[CONSTANTS.Frequency].ToString())
                                .Set(CONSTANTS.TrainedModelId, Timeseriesdata[CONSTANTS.Id].ToString());

                            collection.UpdateMany(TimeSeriesfilter, update);
                            IsCompleted = true;
                        }
                    }

                }

            }
            return IsCompleted;
        }

        private string CheckPythonProcess(string correlationId, string pageInfo)
        {
            string userModel = string.Empty;
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq(CONSTANTS.CorrelationId, correlationId) & builder.Eq(CONSTANTS.pageInfo, pageInfo);
            var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.Status).Include(CONSTANTS.Progress).
            Include(CONSTANTS.CorrelationId).Include(CONSTANTS.pageInfo).Include(CONSTANTS.Message).Exclude(CONSTANTS.Id);
            var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                userModel = result[0].ToJson();
            }
            return userModel;
        }
        private string CheckFMVisualizePrediction(string correlationId, string uniqId)
        {
            string userModel = string.Empty;
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq(CONSTANTS.CorrelationId, correlationId) & builder.Eq(CONSTANTS.pageInfo, CONSTANTS.PublishModel) & builder.Eq(CONSTANTS.UniId, uniqId);
            var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.Status).Include(CONSTANTS.Progress).
            Include(CONSTANTS.CorrelationId).Include(CONSTANTS.pageInfo).Include(CONSTANTS.Message).Exclude(CONSTANTS.Id);
            var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                userModel = result[0].ToJson();
            }
            return userModel;
        }

        private void GetPrediction(string CorrelationId)
        {
            var collection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
            var builder = Builders<DeployModelsDto>.Filter;
            var filter = builder.Eq("CorrelationId", CorrelationId);

            var Projection1 = Builders<DeployModelsDto>.Projection.Exclude("_id");
            var deployedModel = collection.Find(filter).Project<DeployModelsDto>(Projection1).ToList();
            if (deployedModel != null)
            {
                for (int i = 0; i < deployedModel.Count; i++)
                {
                    string FunctionType = string.Empty;
                    string actualData = string.Empty;
                    if (deployedModel[i].ModelType != CONSTANTS.TimeSeries)
                    {
                        FunctionType = CONSTANTS.PublishModel;
                        actualData = deployedModel[i].InputSample;
                    }
                    else
                    {
                        FunctionType = CONSTANTS.ForecastModel;
                        if (_DBEncryptionRequired)
                        {
                            if (_IsAESKeyVault)
                                actualData = CryptographyUtility.Encrypt(CONSTANTS.Null);
                            else
                                actualData = AesProvider.Encrypt(CONSTANTS.Null, _aesKey, _aesVector);
                        }
                        else
                            actualData = CONSTANTS.Null;
                    }

                    PredictionDTO predictionDTO = new PredictionDTO
                    {
                        _id = Guid.NewGuid().ToString(),
                        UniqueId = Guid.NewGuid().ToString(),
                        ActualData = actualData,
                        CorrelationId = CorrelationId,
                        Frequency = deployedModel[i].Frequency,
                        PredictedData = CONSTANTS.Null,
                        Status = CONSTANTS.I,
                        ErrorMessage = CONSTANTS.Null,
                        Progress = CONSTANTS.Null,
                        CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                        ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                        CreatedByUser = _DBEncryptionRequired ? (_IsAESKeyVault ? CryptographyUtility.Encrypt(CONSTANTS.System) : AesProvider.Encrypt(CONSTANTS.System, _aesKey, _aesVector)) : CONSTANTS.System,
                        ModifiedByUser = _DBEncryptionRequired ? (_IsAESKeyVault ? CryptographyUtility.Encrypt(CONSTANTS.System) : AesProvider.Encrypt(CONSTANTS.System, _aesKey, _aesVector)) : CONSTANTS.System
                    };

                    this.SavePrediction(predictionDTO);

                    this.insertRequest(deployedModel[i], predictionDTO.UniqueId, FunctionType);

                    IsPredictedCompeted(predictionDTO, deployedModel[i]);

                }

            }
        }
        public void SavePrediction(PredictionDTO predictionDTO)
        {
            var jsonData = JsonConvert.SerializeObject(predictionDTO);
            var insertDocument = BsonSerializer.Deserialize<BsonDocument>(jsonData);
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_PublishModel);
            collection.InsertOne(insertDocument);
        }


        private IngrainRequestQueue GetFileRequestStatus(string correlationId, string pageInfo)
        {
            IngrainRequestQueue ingrainRequest = new IngrainRequestQueue();
            var collection = _database.GetCollection<IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
            var builder = Builders<IngrainRequestQueue>.Filter;
            var filter = builder.Eq(CONSTANTS.CorrelationId, correlationId) & builder.Eq(CONSTANTS.pageInfo, pageInfo);
            var projection = Builders<IngrainRequestQueue>.Projection.Exclude(CONSTANTS.Id);
            return ingrainRequest = collection.Find(filter).Project<IngrainRequestQueue>(projection).ToList().FirstOrDefault();
        }

        private IngrainRequestQueue GetFileRequestStatusByRequestId(string correlationId, string pageInfo, string requestId)
        {
            IngrainRequestQueue ingrainRequest = new IngrainRequestQueue();
            var collection = _database.GetCollection<IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
            var builder = Builders<IngrainRequestQueue>.Filter;
            var filter = builder.Eq(CONSTANTS.CorrelationId, correlationId) & builder.Eq(CONSTANTS.pageInfo, pageInfo) & builder.Eq("RequestId", requestId);
            var projection = Builders<IngrainRequestQueue>.Projection.Exclude(CONSTANTS.Id);
            return ingrainRequest = collection.Find(filter).Project<IngrainRequestQueue>(projection).ToList().FirstOrDefault();
        }

        private void insertRequest(DeployModelsDto deployModels, string uniqueId, string Function)
        {
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
                pageInfo = Function,
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
            InsertRequests(ingrainRequest);
            Thread.Sleep(2000);
        }

        private void IsPredictedCompeted(PredictionDTO predictionDTO, DeployModelsDto deployModels)
        {
            PredictionDTO predictionData = new PredictionDTO();

            bool isPrediction = true;
            while (isPrediction)
            {
                predictionData = GetPrediction(predictionDTO);

                IngrainResponseData CallBackResponse = new IngrainResponseData
                {
                    CorrelationId = deployModels.CorrelationId,
                };

                if (predictionData.Status == CONSTANTS.C)
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(IsPredictedCompeted), "Status" + predictionData.Status + "Correlation ID" + predictionData.CorrelationId + "  PredictedData" + predictionData.PredictedData, deployModels.AppId, string.Empty, deployModels.ClientUId, deployModels.DeliveryConstructUID);
                    CallBackResponse.Status = CONSTANTS.C;
                    CallBackResponse.Message = "Prediction Completed";
                    CallBackResponse.ErrorMessage = string.Empty;
                    ValidRecordsDetailsModel validRecordsDetailModel = CommonUtility.GetModelDataPointdDetails(deployModels.CorrelationId, _database);
                    if (validRecordsDetailModel != null)
                    {
                        if (validRecordsDetailModel.ValidRecordsDetails != null)
                        {
                            _GenericDataResponse.DataPointsCount = validRecordsDetailModel.ValidRecordsDetails.Records[0];
                            if (validRecordsDetailModel.ValidRecordsDetails.Records[0] >= 4 && validRecordsDetailModel.ValidRecordsDetails.Records[0] < 20)
                            {
                                CallBackResponse.DataPointsWarning = CONSTANTS.DataPointsMinimum;
                                CallBackResponse.DataPointsCount = validRecordsDetailModel.ValidRecordsDetails.Records[0];
                                _GenericDataResponse.DataPointsWarning = CONSTANTS.DataPointsMinimum;
                                _GenericDataResponse.DataPointsCount = validRecordsDetailModel.ValidRecordsDetails.Records[0];
                            }
                        }
                    }
                    CallbackResponse(CallBackResponse, _Mapping.ApplicationName, _CallbackURL, deployModels.ClientUId, deployModels.DeliveryConstructUID, deployModels.AppId, deployModels.UseCaseID, predictionDTO.UniqueId, null, _Mapping.CreatedByUser);

                    _GenericDataResponse.Message = CONSTANTS.Success;
                    _GenericDataResponse.Status = predictionData.Status;

                    isPrediction = false;
                }
                else if (predictionData.Status == CONSTANTS.E)
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(IsPredictedCompeted), "Status" + predictionData.Status + "Correlation ID" + predictionData.CorrelationId + "  PredictedData" + predictionData.PredictedData, deployModels.AppId, string.Empty, deployModels.ClientUId, deployModels.DeliveryConstructUID);
                    CallBackResponse.Status = CONSTANTS.E;
                    CallBackResponse.Message = string.Empty;
                    CallBackResponse.ErrorMessage = predictionData.ErrorMessage;

                    CallbackResponse(CallBackResponse, _Mapping.ApplicationName, _CallbackURL, deployModels.ClientUId, deployModels.DeliveryConstructUID, deployModels.AppId, deployModels.UseCaseID, predictionDTO.UniqueId, null, _Mapping.CreatedByUser);

                    _GenericDataResponse.ErrorMessage = predictionData.ErrorMessage;
                    _GenericDataResponse.Status = predictionData.Status;
                    isPrediction = false;
                }
                else
                {
                    Thread.Sleep(1000);
                    isPrediction = true;
                }
            }
        }

        public PredictionDTO GetPrediction(PredictionDTO predictionDTO)
        {
            PredictionDTO prediction = new PredictionDTO();
            var builder = Builders<PredictionDTO>.Filter;
            var filter = builder.Eq(CONSTANTS.CorrelationId, predictionDTO.CorrelationId) & builder.Eq(CONSTANTS.UniqueId, predictionDTO.UniqueId);
            var projection = Builders<PredictionDTO>.Projection.Exclude("_id");
            var collection = _database.GetCollection<PredictionDTO>(CONSTANTS.SSAI_PublishModel);
            var result = collection.Find(filter).Project<PredictionDTO>(projection).ToList();
            if (result.Count > 0)
            {
                if (_DBEncryptionRequired)
                {
                    if (result[0].Status == CONSTANTS.C)
                    {
                        if (result[0].PredictedData != null)
                        {
                            if (_IsAESKeyVault)
                                result[0].PredictedData = CryptographyUtility.Decrypt(result[0].PredictedData);
                            else
                                result[0].PredictedData = AesProvider.Decrypt(result[0].PredictedData, _aesKey, _aesVector);
                        }
                        try
                        {
                            if (!string.IsNullOrEmpty(Convert.ToString(result[0].CreatedByUser)))
                            {
                                if (_IsAESKeyVault)
                                    result[0].CreatedByUser = CryptographyUtility.Decrypt(Convert.ToString(result[0].CreatedByUser));
                                else
                                    result[0].CreatedByUser = AesProvider.Decrypt(Convert.ToString(result[0].CreatedByUser), _aesKey, _aesVector);
                            }
                        }
                        catch (Exception ex) { LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(GetPrediction) + "User is already decrypted....", ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty); }
                        try
                        {
                            if (!string.IsNullOrEmpty(Convert.ToString(result[0].ModifiedByUser)))
                            {
                                if (_IsAESKeyVault)
                                    result[0].ModifiedByUser = CryptographyUtility.Decrypt(Convert.ToString(result[0].ModifiedByUser));
                                else
                                    result[0].ModifiedByUser = AesProvider.Decrypt(Convert.ToString(result[0].ModifiedByUser), _aesKey, _aesVector);
                            }
                        }
                        catch (Exception ex) { LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(GetPrediction) + "User is already decrypted....", ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty); }
                    }
                }
                prediction = result[0];
            }
            return prediction;
        }

        public string CallbackResponse(IngrainResponseData CallBackResponse, string ApplicationName, string baseAddress, string clientId, string DCId, string applicationId, string usecaseId, string requestId, string errorTrace, string userId)
        {
          return  CallbackResponse(CallBackResponse, ApplicationName, baseAddress, clientId, DCId, applicationId, usecaseId, requestId, errorTrace, userId, 0);
        }
        public string CallbackResponse(IngrainResponseData CallBackResponse, string ApplicationName, string baseAddress, string clientId, string DCId, string applicationId, string usecaseId, string requestId, string errorTrace, string userId, int retryCount)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(CallbackResponse), "START -CallbackResponse Initiated- Data-" + JsonConvert.SerializeObject(CallBackResponse), string.IsNullOrEmpty(CallBackResponse.CorrelationId) ? default(Guid) : new Guid(CallBackResponse.CorrelationId), applicationId, string.Empty, clientId, DCId);
            try
            {
                string token = CustomUrlToken(applicationId);

                if (!string.IsNullOrEmpty(baseAddress))
                {
                    string contentType = "application/json";
                    var Request = JsonConvert.SerializeObject(CallBackResponse);
                    if (_authProvider.ToUpper() == "FORM" || _authProvider.ToUpper() == "AZUREAD")
                    {
                        using (var Client = new HttpClient())
                        {
                            if (!string.IsNullOrEmpty(token))
                            {
                                Client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                                Client.DefaultRequestHeaders.Add("AppServiceUId", _AppServiceUId);
                                Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(contentType));
                                HttpContent httpContent = new StringContent(Request, Encoding.UTF8, "application/json");
                                HttpResponseMessage httpResponse = Client.PostAsync(baseAddress, httpContent).Result;
                                var statuscode = httpResponse.StatusCode;
                                CallBackErrorLog(CallBackResponse, ApplicationName, baseAddress, httpResponse, clientId, DCId, applicationId, usecaseId, requestId, errorTrace, userId, retryCount);
                                if (httpResponse.IsSuccessStatusCode)
                                {
                                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(CallbackResponse), " ApplicationName-- " + ApplicationName + " CALLBACKRESPONSE SUCCESS- TOKEN--" + token + "BASE_ADDERESS: " + baseAddress + "-HTTPCONTENT-" + httpResponse.Content.ReadAsStringAsync().Result, string.IsNullOrEmpty(CallBackResponse.CorrelationId) ? default(Guid) : new Guid(CallBackResponse.CorrelationId), applicationId, string.Empty, clientId, DCId);
                                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(CallbackResponse), "END", string.IsNullOrEmpty(CallBackResponse.CorrelationId) ? default(Guid) : new Guid(CallBackResponse.CorrelationId), applicationId, string.Empty, clientId, DCId);
                                    return "Success";
                                }
                                else
                                {
                                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(CallbackResponse), " ApplicationName-- " + ApplicationName + "  CALLBACKRESPONSE - CALLBACK API ERROR:- HTTP RESPONSE-" + httpResponse.StatusCode + "-HTTPCONTENT-" + httpResponse.Content.ReadAsStringAsync().Result, string.IsNullOrEmpty(CallBackResponse.CorrelationId) ? default(Guid) : new Guid(CallBackResponse.CorrelationId), applicationId, string.Empty, clientId, DCId);
                                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(CallbackResponse), "END", string.IsNullOrEmpty(CallBackResponse.CorrelationId) ? default(Guid) : new Guid(CallBackResponse.CorrelationId), applicationId, string.Empty, clientId, DCId);
                                    return "Error";
                                }
                            }
                            else
                            {
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(CallbackResponse), " ApplicationName-- " + ApplicationName + "  CALLBACKRESPONSE - Token is Null- " + (string.IsNullOrEmpty(CallBackResponse.CorrelationId) ? default(Guid) : new Guid(CallBackResponse.CorrelationId)), applicationId, string.Empty, clientId, DCId);
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(CallbackResponse), "END", string.IsNullOrEmpty(CallBackResponse.CorrelationId) ? default(Guid) : new Guid(CallBackResponse.CorrelationId), applicationId, string.Empty, clientId, DCId);
                                return "Error";
                            }
                        }
                    }
                    else
                    {
                        HttpClientHandler hnd = new HttpClientHandler();
                        hnd.UseDefaultCredentials = true;
                        using (var Client = new HttpClient(hnd))
                        {
                            Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(contentType));

                            HttpContent httpContent = new StringContent(Request, Encoding.UTF8, "application/json");
                            HttpResponseMessage httpResponse = Client.PostAsync(baseAddress, httpContent).Result;
                            var statuscode = httpResponse.StatusCode;
                            if (httpResponse.IsSuccessStatusCode)
                            {
                                return "Success";

                            }
                            else
                            {
                                return "Error";
                            }
                        }
                    }
                }
                else
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(CallbackResponse), " ApplicationName-- " + ApplicationName + " CALLBACKRESPONSE - BASE_ADDERESS NULL : " + baseAddress, string.IsNullOrEmpty(CallBackResponse.CorrelationId) ? default(Guid) : new Guid(CallBackResponse.CorrelationId), applicationId, string.Empty, clientId, DCId);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(CallbackResponse), "END", string.IsNullOrEmpty(CallBackResponse.CorrelationId) ? default(Guid) : new Guid(CallBackResponse.CorrelationId), applicationId, string.Empty, clientId, DCId);
                    return "Success";
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(CallbackResponse), " ApplicationName-- " + ApplicationName + " CALLBACKRESPONSE - BASE_ADDERESS NULL : " + baseAddress, string.IsNullOrEmpty(CallBackResponse.CorrelationId) ? default(Guid) : new Guid(CallBackResponse.CorrelationId), applicationId, string.Empty, clientId, DCId);
                CallBackResponse.ErrorMessage = "Token generation error (or) Response callback error";
                CallBackErrorLog(CallBackResponse, ApplicationName, baseAddress, null, clientId, DCId, applicationId, usecaseId, requestId, errorTrace, userId);
                return "Error";
            }
        }

        private string CustomUrlToken(string ApplicationId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(CustomUrlToken), "CustomUrlToken for Application" + ApplicationId, string.Empty, string.Empty, string.Empty, string.Empty);
            var AppIntegCollection = _database.GetCollection<AppIntegration>(CONSTANTS.AppIntegration);
            var filterBuilder = Builders<AppIntegration>.Filter;
            var AppFilter = filterBuilder.Eq("ApplicationID", ApplicationId);

            var Projection = Builders<AppIntegration>.Projection.Include("ApplicationID").Include("BaseURL").Include("Authentication").Include("TokenGenerationURL").Include("Credentials").Exclude("_id");
            var AppData = AppIntegCollection.Find(AppFilter).Project<AppIntegration>(Projection).FirstOrDefault();

            string status = UpdateAppIntegration(AppData);

            dynamic token = string.Empty;
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(CustomUrlToken), "CustomUrlToken for Application" + AppData.ApplicationName, string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {

                if (AppData.Authentication == "AzureAD" || AppData.Authentication == "Azure")
                {
                    if (status == CONSTANTS.Success)
                    {
                        string GrantType = string.Empty, ClientSecert = string.Empty, ClientId = string.Empty, Resource = string.Empty;
                        if (_IsAESKeyVault)
                        {
                            if (AppData.TokenGenerationURL != null)
                            {
                                AppData.TokenGenerationURL = AesProvider.Decrypt(AppData.TokenGenerationURL.ToString(), _aesKey, _aesVector);
                            }

                            if (AppData.Credentials != null)
                            {
                                AppData.Credentials = BsonDocument.Parse(AesProvider.Decrypt(AppData.Credentials, _aesKey, _aesVector));
                            }
                        }
                        else
                        {
                            AppData.TokenGenerationURL = AesProvider.Decrypt(AppData.TokenGenerationURL.ToString(), _aesKey, _aesVector);
                            AppData.Credentials = BsonDocument.Parse(AesProvider.Decrypt(AppData.Credentials, _aesKey, _aesVector));
                        }

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
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(CustomUrlToken), "Application TOKEN PARAMS -- " + requestBuilder, string.Empty, string.Empty, string.Empty, string.Empty);
                        IRestResponse response1 = client.Execute(request);
                        string json1 = response1.Content;
                        // Retrieve and Return the Access Token                
                        var tokenObj = JsonConvert.DeserializeObject(json1) as dynamic;
                        token = Convert.ToString(tokenObj.access_token);
                        return token;
                    }
                    else
                    {
                        return token;
                    }
                }
                else if (AppData.Authentication == "FORM")
                {
                    using (var httpClient = new HttpClient())
                    {
                        if (Environment.Equals(CONSTANTS.PAMEnvironment))
                        {
                            httpClient.BaseAddress = new Uri(tokenapiURL);
                            httpClient.DefaultRequestHeaders.Accept.Clear();
                            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                            string json = JsonConvert.SerializeObject(new
                            {
                                username = Convert.ToString(username),
                                password = Convert.ToString(password)
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
                            if (Environment == CONSTANTS.PAMEnvironment)
                                token = tokenObj != null ? Convert.ToString(tokenObj.token) : CONSTANTS.InvertedComma;
                            else
                                token = tokenObj != null ? Convert.ToString(tokenObj.access_token) : CONSTANTS.InvertedComma;
                            return token;

                        }
                        else
                        {
                            httpClient.BaseAddress = new Uri(tokenapiURL);
                            httpClient.DefaultRequestHeaders.Accept.Clear();
                            httpClient.DefaultRequestHeaders.Add("UserName", username);
                            httpClient.DefaultRequestHeaders.Add("Password", password);
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
                        //httpClient.BaseAddress = new Uri(tokenapiURL);
                        //httpClient.DefaultRequestHeaders.Accept.Clear();
                        //httpClient.DefaultRequestHeaders.Add("UserName", username);
                        //httpClient.DefaultRequestHeaders.Add("Password", password);
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

            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(GenericAutotrainService), nameof(CustomUrlToken), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);

            }
            return token;
        }



        public string UpdateAppIntegration(AppIntegration appData)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(UpdateAppIntegration), "UpdateAppIntegration - AppId : " + appData.ApplicationID, appData.ApplicationID, string.Empty, appData.clientUId, appData.deliveryConstructUID);
            AppIntegrationsCredentials appIntegrationsCredentials = new AppIntegrationsCredentials()
            {
                grant_type = _Grant_Type,
                client_id = _clientId,
                client_secret = _clientSecret,
                resource = _resourceId
            };

            AppIntegration appIntegrations = new AppIntegration()
            {
                ApplicationID = appData.ApplicationID,
                Credentials = JsonConvert.SerializeObject(appIntegrationsCredentials)
            };
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(UpdateAppIntegration), "UpdateAppIntegration - appIntegrations appId : " + appIntegrations.ApplicationID, appData.ApplicationID, string.Empty, appData.clientUId, appData.deliveryConstructUID);
            var collection = _database.GetCollection<AppIntegration>(CONSTANTS.AppIntegration);
            var filter = Builders<AppIntegration>.Filter.Where(x => x.ApplicationID == appIntegrations.ApplicationID);
            var Projection = Builders<AppIntegration>.Projection.Exclude("_id");
            var result = collection.Find(filter).Project<AppIntegration>(Projection).FirstOrDefault();

            var status = CONSTANTS.NoRecordsFound;
            if (result != null)
            {
                var update = Builders<AppIntegration>.Update
                    .Set(x => x.Authentication, _authProvider)
                    .Set(x => x.TokenGenerationURL, (_IsAESKeyVault ? CryptographyUtility.Encrypt(_token_Url) : AesProvider.Encrypt(_token_Url, _aesKey, _aesVector)))
                    .Set(x => x.Credentials, (IEnumerable)(_IsAESKeyVault ? CryptographyUtility.Encrypt(appIntegrations.Credentials) : AesProvider.Encrypt(appIntegrations.Credentials, _aesKey, _aesVector)))
                    .Set(x => x.ModifiedByUser, (_IsAESKeyVault ? CryptographyUtility.Encrypt(username) : AesProvider.Encrypt(username, _aesKey, _aesVector)))
                    .Set(x => x.ModifiedOn, DateTime.UtcNow.ToString());
                collection.UpdateOne(filter, update);
                status = CONSTANTS.Success;
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(UpdateAppIntegration), "UpdateAppIntegration - status : " + status, appData.ApplicationID, string.Empty, appData.clientUId, appData.deliveryConstructUID);
            return status;
        }

        public void GetPredictionData(IngrainRequestQueue ingrainRequests)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(GetPredictionData), "--START UniqueId--" + ingrainRequests.UniId, string.IsNullOrEmpty(ingrainRequests.CorrelationId) ? default(Guid) : new Guid(ingrainRequests.CorrelationId), ingrainRequests.AppID, string.Empty, ingrainRequests.ClientID, ingrainRequests.DeliveryconstructId);
            try
            {
                var collection = _database.GetCollection<IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
                var filterBuilder = Builders<IngrainRequestQueue>.Filter;
                var filter = filterBuilder.Eq(CONSTANTS.CorrelationId, ingrainRequests.CorrelationId) & filterBuilder.Eq(CONSTANTS.UniId, ingrainRequests.UniId);
                var result = collection.Find(filter).ToList();
                PredictionDTO predictionData = new PredictionDTO
                {
                    UniqueId = ingrainRequests.UniId,
                    CorrelationId = ingrainRequests.CorrelationId,
                    Status = CONSTANTS.P,
                    Progress = CONSTANTS.PredictionUnderProcess
                };
                if (result.Count > 0)
                {
                    bool isPrediction = true;
                    while (isPrediction)
                    {
                        IngrainPredictionData ingrainPrediction = new IngrainPredictionData();
                        ingrainPrediction.CorrelationId = ingrainRequests.CorrelationId;
                        ingrainPrediction.UniqueId = ingrainRequests.UniId;
                        predictionData = this.GetSPEPrediction(predictionData);
                        isPrediction = false;
                        DateTime currentTime = DateTime.Now;
                        DateTime createdTime = DateTime.Parse(predictionData.CreatedOn);
                        TimeSpan span = currentTime.Subtract(createdTime);
                        if (span.TotalMinutes > _PredictionTimeoutMinutes && predictionData.Status != CONSTANTS.C)
                        {
                            ingrainPrediction.Message = CONSTANTS.PredictionTimeOut;
                            ingrainPrediction.ErrorMessage = "Prediction Taking long time - PredictionTimeOut Error";
                            ingrainPrediction.Status = "E";
                            isPrediction = false;
                            var result2 = IngrainPredictionCallback(ingrainPrediction, ingrainRequests.AppID, ingrainRequests.AppURL, ingrainRequests.ClientId, ingrainRequests.DeliveryconstructId, ingrainRequests.TemplateUseCaseID, ingrainRequests.RequestId, null, ingrainRequests.CreatedByUser);
                            UpdateRequestQueue(ingrainRequests.RequestId, "True", result2);
                        }
                        else if (predictionData.Status == CONSTANTS.E)
                        {
                            string response = JsonConvert.SerializeObject(predictionData);
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(GetPredictionData), CONSTANTS.Prediction + response.Replace(CONSTANTS.slash, string.Empty), string.IsNullOrEmpty(ingrainRequests.CorrelationId) ? default(Guid) : new Guid(ingrainRequests.CorrelationId), ingrainRequests.AppID, string.Empty, ingrainRequests.ClientID, ingrainRequests.DeliveryconstructId);
                            ingrainPrediction.Status = CONSTANTS.E;
                            ingrainPrediction.Message = CONSTANTS.Error;
                            isPrediction = false;
                            ingrainPrediction.ErrorMessage = "Python: Error While Prediction : " + predictionData.ErrorMessage + " Status:" + predictionData.Status;
                            var result2 = IngrainPredictionCallback(ingrainPrediction, ingrainRequests.AppID, ingrainRequests.AppURL, ingrainRequests.ClientId, ingrainRequests.DeliveryconstructId, ingrainRequests.TemplateUseCaseID, ingrainRequests.RequestId, null, ingrainRequests.CreatedByUser);
                            UpdateRequestQueue(ingrainRequests.RequestId, "True", result2);
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(GetPredictionData), "END SPE PREDICTION ERROR--", string.IsNullOrEmpty(ingrainRequests.CorrelationId) ? default(Guid) : new Guid(ingrainRequests.CorrelationId), ingrainRequests.AppID, string.Empty, ingrainRequests.ClientID, ingrainRequests.DeliveryconstructId);
                        }
                        else if (predictionData.Status == CONSTANTS.C)
                        {
                            ingrainPrediction.Message = CONSTANTS.Success;
                            ingrainPrediction.Status = predictionData.Status;
                            ingrainPrediction.PredictedData = predictionData.PredictedData;
                            isPrediction = false;
                            var result2 = IngrainPredictionCallback(ingrainPrediction, ingrainRequests.AppID, ingrainRequests.AppURL, ingrainRequests.ClientId, ingrainRequests.DeliveryconstructId, ingrainRequests.TemplateUseCaseID, ingrainRequests.RequestId, null, ingrainRequests.CreatedByUser);
                            UpdateRequestQueue(ingrainRequests.RequestId, "True", result2);
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(GetPredictionData), "END SPE PREDICTION COMPLETED", string.IsNullOrEmpty(ingrainRequests.CorrelationId) ? default(Guid) : new Guid(ingrainRequests.CorrelationId), ingrainRequests.AppID, string.Empty, ingrainRequests.ClientID, ingrainRequests.DeliveryconstructId);
                        }
                        else
                        {
                            isPrediction = true;
                            Thread.Sleep(3000);
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(GetPredictionData), "END SPE ELSE BLOCK", string.IsNullOrEmpty(ingrainRequests.CorrelationId) ? default(Guid) : new Guid(ingrainRequests.CorrelationId), ingrainRequests.AppID, string.Empty, ingrainRequests.ClientID, ingrainRequests.DeliveryconstructId);
                            //retry++;
                            //Thread.Sleep(3000);
                            //if (retry > 3)
                            //{
                            //    isPrediction = false;
                            //    ingrainPrediction.Status = "E";
                            //    ingrainPrediction.ErrorMessage = "UniqueId not found";
                            //    var result2 = IngrainPredictionCallback(ingrainPrediction, ingrainRequests.ApplicationName, ingrainRequests.AppURL);
                            //}
                            //else
                            //{
                            //    isPrediction = true;
                            //}
                        }
                    }
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(GetPredictionData), "END", string.IsNullOrEmpty(ingrainRequests.CorrelationId) ? default(Guid) : new Guid(ingrainRequests.CorrelationId), ingrainRequests.AppID, string.Empty, ingrainRequests.ClientID, ingrainRequests.DeliveryconstructId);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(GenericAutotrainService), nameof(GetPredictionData), ex.Message + "- STACKTRACE - " + ex.StackTrace + ex.InnerException, ex, ingrainRequests.AppID, string.Empty, ingrainRequests.ClientID, ingrainRequests.DeliveryconstructId);
                _IngrainResponseData.CorrelationId = ingrainRequests.CorrelationId;
                _IngrainResponseData.Status = "Error";
                _IngrainResponseData.Message = CONSTANTS.Error;
                _IngrainResponseData.ErrorMessage = ex.Message;
                _IngrainResponseData.DataPointsWarning = _GenericDataResponse.DataPointsWarning;
                _IngrainResponseData.DataPointsCount = _GenericDataResponse.DataPointsCount;
                CallBackErrorLog(_IngrainResponseData, ingrainRequests.ApplicationName, ingrainRequests.AppURL, null, ingrainRequests.ClientId, ingrainRequests.DeliveryconstructId, ingrainRequests.AppID, ingrainRequests.TemplateUseCaseID, ingrainRequests.RequestId, ex.StackTrace, ingrainRequests.CreatedByUser);
            }


        }

        public void UpdateRequestQueue(string requestId, string isNotificationSent, string result)
        {
            var requestCollection = _database.GetCollection<IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
            var filter = Builders<IngrainRequestQueue>.Filter.Where(x => x.RequestId == requestId);
            var update = Builders<IngrainRequestQueue>.Update.Set(x => x.IsNotificationSent, isNotificationSent).Set(x => x.NotificationMessage, result);
            requestCollection.UpdateOne(filter, update);
        }
        private PredictionDTO GetSPEPrediction(PredictionDTO predictionDTO)
        {
            PredictionDTO prediction = new PredictionDTO();
            var builder = Builders<PredictionDTO>.Filter;
            var filter = builder.Eq(CONSTANTS.CorrelationId, predictionDTO.CorrelationId) & builder.Eq(CONSTANTS.UniqueId, predictionDTO.UniqueId);
            var projection = Builders<PredictionDTO>.Projection.Exclude("_id");
            var collection = _database.GetCollection<PredictionDTO>(CONSTANTS.SSAI_PublishModel);
            var result = collection.Find(filter).Project<PredictionDTO>(projection).ToList();
            bool DBEncryptionRequired = this.EncryptDB(predictionDTO.CorrelationId);
            if (result.Count > 0)
            {
                if (DBEncryptionRequired)
                {
                    if (result[0].PredictedData != null)
                    {
                        if (_IsAESKeyVault)
                            result[0].PredictedData = CryptographyUtility.Decrypt(result[0].PredictedData);
                        else
                            result[0].PredictedData = AesProvider.Decrypt(result[0].PredictedData, _aesKey, _aesVector);
                    }

                    if (_IsAESKeyVault)
                        result[0].ActualData = CryptographyUtility.Decrypt(result[0].ActualData);
                    else
                        result[0].ActualData = AesProvider.Decrypt(result[0].ActualData, _aesKey, _aesVector);

                    try
                    {
                        if (!string.IsNullOrEmpty(Convert.ToString(result[0].CreatedByUser)))
                        {
                            if (_IsAESKeyVault)
                                result[0].CreatedByUser = CryptographyUtility.Decrypt(Convert.ToString(result[0].CreatedByUser));
                            else
                                result[0].CreatedByUser = AesProvider.Decrypt(Convert.ToString(result[0].CreatedByUser), _aesKey, _aesVector);
                        }
                    }
                    catch (Exception ex) { LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(GetSPEPrediction) + "User is already decrypted....", ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty); }
                    try
                    {
                        if (!string.IsNullOrEmpty(Convert.ToString(result[0].ModifiedByUser)))
                        {
                            if (_IsAESKeyVault)
                                result[0].ModifiedByUser = CryptographyUtility.Decrypt(Convert.ToString(result[0].ModifiedByUser));
                            else
                                result[0].ModifiedByUser = AesProvider.Decrypt(Convert.ToString(result[0].ModifiedByUser), _aesKey, _aesVector);
                        }
                    }
                    catch (Exception ex) { LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(GetSPEPrediction) + "User is already decrypted....", ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty); }
                }
                prediction = result[0];
            }
            return prediction;
        }
        private bool EncryptDB(string correlationid)
        {
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
            var filter = Builders<BsonDocument>.Filter.Eq("CorrelationId", correlationid);
            var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.DBEncryptionRequired).Include(CONSTANTS.CorrelationId).Exclude(CONSTANTS.Id);
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
        private string IngrainPredictionCallback(IngrainPredictionData CallBackResponse, string applicationId, string baseAddress, string clientId, string DCId, string usecaseId, string requestId, string errorTrace, string userId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(IngrainPredictionCallback), "INGRAINPREDICTIONCALLBACK INITIATED Data--" + CallBackResponse.Status + "-" + CallBackResponse.Message + "-" + CallBackResponse.ErrorMessage + "--baseAddress--" + baseAddress, string.IsNullOrEmpty(CallBackResponse.CorrelationId) ? default(Guid) : new Guid(CallBackResponse.CorrelationId), applicationId, string.Empty, clientId, DCId);
            string returnMessage = "Error";
            try
            {
                string token = CustomUrlTokenAppId(applicationId);
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(IngrainPredictionCallback), "INGRAINPREDICTIONCALLBACK TOKEN--" + token, string.IsNullOrEmpty(CallBackResponse.CorrelationId) ? default(Guid) : new Guid(CallBackResponse.CorrelationId), applicationId, string.Empty, clientId, DCId);
                string contentType = "application/json";
                var Request = JsonConvert.SerializeObject(CallBackResponse);
                using (var Client = new HttpClient())
                {
                    Client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                    Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(contentType));
                    HttpContent httpContent = new StringContent(Request, Encoding.UTF8, "application/json");
                    HttpResponseMessage httpResponse = Client.PostAsync(baseAddress, httpContent).Result;
                    var statuscode = httpResponse.StatusCode;
                    _IngrainResponseData.CorrelationId = CallBackResponse.CorrelationId;
                    _IngrainResponseData.Status = CallBackResponse.Status;
                    _IngrainResponseData.Message = CallBackResponse.Message;
                    _IngrainResponseData.ErrorMessage = CallBackResponse.ErrorMessage;
                    _IngrainResponseData.DataPointsWarning = _GenericDataResponse.DataPointsWarning;
                    _IngrainResponseData.DataPointsCount = _GenericDataResponse.DataPointsCount;

                    CallBackErrorLog(_IngrainResponseData, applicationId, baseAddress, null, clientId, DCId, applicationId, usecaseId, CallBackResponse.UniqueId, null, userId);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(IngrainPredictionCallback), "INGRAINPREDICTIONCALLBACK STATUSCODE :" + httpResponse.StatusCode + "--URL--" + baseAddress + "--INGRAIN PAYLOAD--" + JsonConvert.SerializeObject(CallBackResponse), string.IsNullOrEmpty(CallBackResponse.CorrelationId) ? default(Guid) : new Guid(CallBackResponse.CorrelationId), applicationId, string.Empty, clientId, DCId);
                    if (httpResponse.IsSuccessStatusCode)
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(IngrainPredictionCallback), "IngrainPredictionCallback SUCCESS- TOKEN--" + token + "BASE_ADDERESS: " + baseAddress + "-HTTPCONTENT-" + httpResponse.Content.ReadAsStringAsync().Result, string.IsNullOrEmpty(CallBackResponse.CorrelationId) ? default(Guid) : new Guid(CallBackResponse.CorrelationId), applicationId, string.Empty, clientId, DCId);
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(IngrainPredictionCallback), "END SUCCESS", string.IsNullOrEmpty(CallBackResponse.CorrelationId) ? default(Guid) : new Guid(CallBackResponse.CorrelationId), applicationId, string.Empty, clientId, DCId);
                        returnMessage = "Success";
                    }
                    else
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(IngrainPredictionCallback), "IngrainPredictionCallback SUCCESS- TOKEN--" + token + "BASE_ADDERESS: " + baseAddress + "-HTTPCONTENT-" + httpResponse.Content.ReadAsStringAsync().Result, string.IsNullOrEmpty(CallBackResponse.CorrelationId) ? default(Guid) : new Guid(CallBackResponse.CorrelationId), applicationId, string.Empty, clientId, DCId);
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(IngrainPredictionCallback), "END ERROR", string.IsNullOrEmpty(CallBackResponse.CorrelationId) ? default(Guid) : new Guid(CallBackResponse.CorrelationId), applicationId, string.Empty, clientId, DCId);
                        returnMessage = "Error-" + httpResponse.ReasonPhrase + "-" + httpResponse.Content.ReadAsStringAsync().Result;
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(GenericAutotrainService), nameof(IngrainPredictionCallback), ex.Message + "- STACKTRACE - " + ex.StackTrace + ex.InnerException, ex, applicationId, string.Empty, clientId, DCId);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(IngrainPredictionCallback), "END", string.IsNullOrEmpty(CallBackResponse.CorrelationId) ? default(Guid) : new Guid(CallBackResponse.CorrelationId), applicationId, string.Empty, clientId, DCId);
            return returnMessage;

        }

        private string CustomUrlTokenAppId(string appId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(CustomUrlTokenAppId), "CUSTOMURLTOKENAPPID FOR APPLICATIONID--" + appId, appId, string.Empty, string.Empty, string.Empty);
            var AppIntegCollection = _database.GetCollection<AppIntegration>(CONSTANTS.AppIntegration);
            var filterBuilder = Builders<AppIntegration>.Filter;
            var AppFilter = filterBuilder.Eq("ApplicationID", appId);

            var Projection = Builders<AppIntegration>.Projection.Include("ApplicationID").Include("BaseURL").Include("Authentication").Include("TokenGenerationURL").Include("Credentials").Exclude("_id");
            var AppData = AppIntegCollection.Find(AppFilter).Project<AppIntegration>(Projection).FirstOrDefault();
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(CustomUrlTokenAppId), "CUSTOMURLTOKENAPPID APPIntegration collection" + AppData.ApplicationName, AppData.ApplicationID, string.Empty, AppData.clientUId, AppData.deliveryConstructUID);
            string status = UpdateAppIntegration(AppData);
            dynamic token = string.Empty;
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(CustomUrlTokenAppId), "CUSTOMURLTOKENAPPID for Application" + AppData.ApplicationName, AppData.ApplicationID, string.Empty, AppData.clientUId, AppData.deliveryConstructUID);
            if (AppData.Authentication == "AzureAD" || AppData.Authentication == "Azure")
            {
                if (status == CONSTANTS.Success)
                {
                    string GrantType = string.Empty, ClientSecert = string.Empty, ClientId = string.Empty, Resource = string.Empty;
                    if (_IsAESKeyVault)
                    {
                        AppData.TokenGenerationURL = CryptographyUtility.Decrypt(AppData.TokenGenerationURL.ToString());
                        AppData.Credentials = BsonDocument.Parse(CryptographyUtility.Decrypt(AppData.Credentials));
                    }
                    else
                    {
                        AppData.TokenGenerationURL = AesProvider.Decrypt(AppData.TokenGenerationURL.ToString(), _aesKey, _aesVector);
                        AppData.Credentials = BsonDocument.Parse(AesProvider.Decrypt(AppData.Credentials, _aesKey, _aesVector));
                    }

                    GrantType = AppData.Credentials.GetValue("grant_type").AsString;
                    ClientSecert = AppData.Credentials.GetValue("client_secret").AsString;
                    ClientId = AppData.Credentials.GetValue("client_id").AsString;
                    if (AppData.Credentials.GetValue("resource").ToString() != CONSTANTS.BsonNull & AppData.Credentials.GetValue("resource").ToString() != null)
                    {
                        Resource = AppData.Credentials.GetValue("resource").AsString;
                    }
                    else
                    {
                        Resource = null;
                    }
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
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(CustomUrlTokenAppId), "Application TOKEN PARAMS -- " + requestBuilder, AppData.ApplicationID, string.Empty, AppData.clientUId, AppData.deliveryConstructUID);
                    IRestResponse response1 = client.Execute(request);
                    string json1 = response1.Content;
                    // Retrieve and Return the Access Token                
                    var tokenObj = JsonConvert.DeserializeObject(json1) as dynamic;
                    token = Convert.ToString(tokenObj.access_token);
                    return token;
                }
                else
                {
                    return token;
                }
            }
            return token;
        }
        public GenericAutoTrain PrivateCascadeModelTraining(IngrainRequestQueue result)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(PrivateCascadeModelTraining), "PRIVATECASCADEMODELTRAINING - STARTED :", string.IsNullOrEmpty(result.CorrelationId) ? default(Guid) : new Guid(result.CorrelationId), result.AppID, string.Empty, result.ClientID, result.DeliveryconstructId);
            bool isAllModelsSuccess = false;
            List<string> listCorids = new List<string>();
            try
            {
                _requestId = result.RequestId;
                var cascadeCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAICascadedModels);
                var cascadeFilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CascadedId, result.TemplateUseCaseID);
                var cascadeProjection = Builders<BsonDocument>.Projection.Exclude("MappingData").Exclude(CONSTANTS.Id);
                var cascadeResult = cascadeCollection.Find(cascadeFilter).Project<BsonDocument>(cascadeProjection).ToList();
                if (cascadeResult.Count > 0)
                {
                    JObject cascadeModelList = JObject.Parse(cascadeResult[0].ToString());
                    if (cascadeModelList != null)
                    {
                        foreach (var item in cascadeModelList[CONSTANTS.ModelList].Children())
                        {
                            JProperty prop = item as JProperty;
                            if (prop != null)
                            {
                                DeployModelDetails model = JsonConvert.DeserializeObject<DeployModelDetails>(prop.Value.ToString());
                                NewCorrelationId = Guid.NewGuid().ToString();
                                listCorids.Add(NewCorrelationId);
                                _templateInfo.ClientUID = result.ClientId;
                                _templateInfo.DCID = result.DeliveryconstructId;
                                _templateInfo.pageInfo = result.pageInfo;
                                _Mapping.CreatedByUser = result.CreatedByUser;
                                _Mapping.UsecaseID = model.CorrelationId;
                                _Mapping.ApplicationID = model.ApplicationID;
                                paramArgsCustomMultipleFetch = result.ParamArgs;
                                bool status = true;
                                List<PublicTemplateMapping> MappingResult = new List<PublicTemplateMapping>();
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(PrivateCascadeModelTraining), "Correlation Id :", string.IsNullOrEmpty(NewCorrelationId) ? default(Guid) : new Guid(NewCorrelationId), result.AppID, string.Empty, _templateInfo.ClientUID, _templateInfo.DCID);
                                MappingResult = GetDataMapping(_Mapping.UsecaseID, _Mapping.ApplicationID);
                                if (MappingResult.Count > 0)
                                {
                                    _Mapping.UsecaseName = MappingResult[0].UsecaseName;
                                    _Mapping.SourceName = MappingResult[0].SourceName;
                                    _Mapping.SourceURL = MappingResult[0].SourceURL;
                                    _Mapping.ApplicationName = MappingResult[0].ApplicationName;
                                    _Mapping.DateColumn = MappingResult[0].DateColumn;
                                    _templateInfo.ModelName = _Mapping.ApplicationName + "_" + _Mapping.UsecaseName + "_" + NewCorrelationId;
                                    string callbackResonse = string.Empty;
                                    GetBusinessProblemData(_Mapping.UsecaseID);
                                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(PrivateCascadeModelTraining), "_Mapping.SourceName--" + _Mapping.SourceName + "-_Mapping.SourceURL-" + _Mapping.SourceURL, string.IsNullOrEmpty(NewCorrelationId) ? default(Guid) : new Guid(NewCorrelationId), result.AppID, string.Empty, _templateInfo.ClientUID, _templateInfo.DCID);
                                    if (_Mapping.SourceName == "Custom" && !string.IsNullOrEmpty(_Mapping.SourceURL))
                                    {
                                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(PrivateCascadeModelTraining), "INSIDE --_Mapping.SourceName--" + _Mapping.SourceName + "-_Mapping.SourceURL-" + _Mapping.SourceURL, string.IsNullOrEmpty(NewCorrelationId) ? default(Guid) : new Guid(NewCorrelationId), result.AppID, string.Empty, _templateInfo.ClientUID, _templateInfo.DCID);
                                        if (!string.IsNullOrEmpty(paramArgsCustomMultipleFetch) && paramArgsCustomMultipleFetch != CONSTANTS.Null)
                                        {
                                            _templateInfo.ParamArgs = paramArgsCustomMultipleFetch;
                                        }
                                        else
                                        {
                                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(PrivateCascadeModelTraining), "INSIDE ELSE--_Mapping.SourceName--" + _Mapping.SourceName + "-_Mapping.SourceURL-" + _Mapping.SourceURL, string.IsNullOrEmpty(NewCorrelationId) ? default(Guid) : new Guid(NewCorrelationId), result.AppID, string.Empty, _templateInfo.ClientUID, _templateInfo.DCID);
                                            SourceCustomdata(NewCorrelationId, null, null);
                                        }
                                    }
                                    else if (_Mapping.SourceName == CONSTANTS.SPAAPP)
                                    {
                                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(PrivateCascadeModelTraining), "INSIDE SPA-APP ---_Mapping.SourceName--" + _Mapping.SourceName + "-_Mapping.SourceURL-" + _Mapping.SourceURL, string.IsNullOrEmpty(NewCorrelationId) ? default(Guid) : new Guid(NewCorrelationId), result.AppID, string.Empty, _templateInfo.ClientUID, _templateInfo.DCID);
                                        SourceCustomdata(NewCorrelationId, CONSTANTS.SPAAPP, _Mapping.ApplicationName);
                                    }
                                    else if (_Mapping.SourceName.ToUpper() == "CustomMultiple".ToUpper() && !string.IsNullOrEmpty(_Mapping.SourceURL))
                                        _templateInfo.ParamArgs = paramArgsCustomMultipleFetch;
                                    else if (_Mapping.SourceName.ToUpper() == "CustomSingle".ToUpper() && !string.IsNullOrEmpty(_Mapping.SourceURL))
                                        _templateInfo.ParamArgs = paramArgsCustomMultipleFetch;
                                    else
                                    {
                                        status = GetIngrainRequestData(_Mapping.UsecaseID, NewCorrelationId, _Mapping.SourceName, _templateInfo.ClientUID, _templateInfo.DCID);
                                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(PrivateCascadeModelTraining), "GetIngrainRequestData status" + status, string.IsNullOrEmpty(NewCorrelationId) ? default(Guid) : new Guid(NewCorrelationId), result.AppID, string.Empty, _templateInfo.ClientUID, _templateInfo.DCID);
                                    }
                                    if (status)
                                    {
                                        InsertBusinessProblem(_templateInfo, NewCorrelationId);
                                        CreateCascadeInstaModel(_templateInfo.ProblemType, NewCorrelationId);
                                        IngestDataInsertRequests(NewCorrelationId, _templateInfo.ClientUID, _templateInfo.DCID, _templateInfo.ProblemType, _templateInfo.TargetColumn, _templateInfo.AppicationID, _templateInfo.ParamArgs, result.DataSetUId);
                                        Thread.Sleep(1000);
                                        ValidatingIngestDataCompletion(NewCorrelationId, _templateInfo);
                                        _IngrainResponseData.CorrelationId = NewCorrelationId;
                                        if (_GenericDataResponse.Status == CONSTANTS.C)
                                        {
                                            StartModelTraining(_templateInfo, NewCorrelationId, result.AppURL);
                                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(PrivateCascadeModelTraining), "_GenericDataResponse.Status" + _GenericDataResponse.Status, string.IsNullOrEmpty(NewCorrelationId) ? default(Guid) : new Guid(NewCorrelationId), result.AppID, string.Empty, _templateInfo.ClientUID, _templateInfo.DCID);
                                            if (_GenericDataResponse.Status == "C")
                                            {
                                                isAllModelsSuccess = true;
                                                UpdateRequestStatus("50%");
                                                UpdateTrainingStatus(_GenericDataResponse.Status, _GenericDataResponse.Message, CONSTANTS.InProgress, NewCorrelationId, _templateInfo.ClientUID, _templateInfo.DCID, _Mapping.ApplicationID, _Mapping.UsecaseID, null, _Mapping.CreatedByUser);
                                            }
                                            else
                                            {
                                                isAllModelsSuccess = false;
                                                UpdateTrainingStatus(_GenericDataResponse.Status, _GenericDataResponse.ErrorMessage, CONSTANTS.ErrorMessage, NewCorrelationId, _templateInfo.ClientUID, _templateInfo.DCID, _Mapping.ApplicationID, _Mapping.UsecaseID, null, _Mapping.CreatedByUser);
                                                break;
                                            }
                                        }
                                        else
                                        {
                                            isAllModelsSuccess = false;
                                            UpdateTrainingStatus(_GenericDataResponse.Status, _GenericDataResponse.ErrorMessage, CONSTANTS.ErrorMessage, NewCorrelationId, _templateInfo.ClientUID, _templateInfo.DCID, _Mapping.ApplicationID, _Mapping.UsecaseID, null, _Mapping.CreatedByUser);
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        _GenericDataResponse.ErrorMessage = "Cascade Model Template, ParamArgs Details is Null";
                                        _GenericDataResponse.Status = "E";
                                        UpdateTrainingStatus(_GenericDataResponse.Status, _GenericDataResponse.ErrorMessage, CONSTANTS.ErrorMessage, NewCorrelationId, _templateInfo.ClientUID, _templateInfo.DCID, _Mapping.ApplicationID, _Mapping.UsecaseID, null, _Mapping.CreatedByUser);
                                        return _GenericDataResponse;
                                    }
                                }
                            }
                        }
                        if (isAllModelsSuccess)
                        {
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(PrivateCascadeModelTraining), "CASCADE MODEL STARTED - STARTED --:" + listCorids.ToArray(), string.IsNullOrEmpty(result.CorrelationId) ? default(Guid) : new Guid(result.CorrelationId), result.AppID, string.Empty, _templateInfo.ClientUID, _templateInfo.DCID);
                            //Forming modellist for cascadeModel
                            UpdateRequestStatus("75%");
                            CascadeModelInsertion(listCorids, cascadeModelList, result);
                            UpdateRequestStatus("100%");
                            UpdateTrainingStatus(_GenericDataResponse.Status, _GenericDataResponse.Message, CONSTANTS.Completed, NewCorrelationId, _templateInfo.ClientUID, _templateInfo.DCID, _Mapping.ApplicationID, _Mapping.UsecaseID, null, _Mapping.CreatedByUser);
                        }
                        else
                        {
                            _GenericDataResponse.Status = CONSTANTS.E;
                            _GenericDataResponse.ErrorMessage = _GenericDataResponse.ErrorMessage + "-cascademodels error-" + "one of the cascade sub model failed - " + listCorids.ToArray();
                            UpdateTrainingStatus(_GenericDataResponse.Status, _GenericDataResponse.ErrorMessage, CONSTANTS.ErrorMessage, NewCorrelationId, _templateInfo.ClientUID, _templateInfo.DCID, _Mapping.ApplicationID, _Mapping.UsecaseID, null, _Mapping.CreatedByUser);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(GenericAutotrainService), nameof(PrivateCascadeModelTraining), ex.Message + "- STACKTRACE - " + ex.StackTrace + ex.InnerException, ex, result.AppID, string.Empty, _templateInfo.ClientUID, _templateInfo.DCID);
                _GenericDataResponse.ErrorMessage = CONSTANTS.TrainingException;
                _GenericDataResponse.Status = CONSTANTS.E;
                foreach (var item in listCorids)
                {
                    DeleteDeployModel(item);
                }
                DeleteDeployModel(result.CorrelationId);
                DeleteCascadeModel(result.CorrelationId);
                UpdateTrainingStatus(_GenericDataResponse.Status, _GenericDataResponse.ErrorMessage, CONSTANTS.ErrorMessage, NewCorrelationId, _templateInfo.ClientUID, _templateInfo.DCID, _Mapping.ApplicationID, _Mapping.UsecaseID, ex.StackTrace, _Mapping.CreatedByUser);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(PrivateCascadeModelTraining), "PRIVATECASCADEMODELTRAINING - Ended :", string.IsNullOrEmpty(result.CorrelationId) ? default(Guid) : new Guid(result.CorrelationId), result.AppID, string.Empty, _templateInfo.ClientUID, _templateInfo.DCID);
            return _GenericDataResponse;
        }
        private void CascadeModelInsertion(List<string> listCorids, JObject cascadeModelList, IngrainRequestQueue result)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(CascadeModelInsertion), "CASCADEMODELINSERTION - STARTED :", string.IsNullOrEmpty(result.CorrelationId) ? default(Guid) : new Guid(result.CorrelationId), result.AppID, string.Empty, _templateInfo.ClientUID, _templateInfo.DCID);
            JObject ModelList = new JObject();
            List<CascadeModelDictionary> listModels = new List<CascadeModelDictionary>();
            int k = 1;
            foreach (var corid in listCorids)
            {
                var deployCollection = _database.GetCollection<DeployedModel>(CONSTANTS.SSAIDeployedModels);
                var filter = Builders<DeployedModel>.Filter.Eq(CONSTANTS.CorrelationId, corid);
                var ModelResult = deployCollection.Find(filter).FirstOrDefault();
                if (ModelResult != null)
                {
                    CascadeModelDictionary cascadeModel = new CascadeModelDictionary();
                    cascadeModel.CorrelationId = ModelResult.CorrelationId;
                    cascadeModel.ModelName = ModelResult.ModelName;
                    cascadeModel.ProblemType = ModelResult.ModelType;
                    cascadeModel.Accuracy = ModelResult.Accuracy;
                    cascadeModel.ModelType = ModelResult.ModelVersion;
                    cascadeModel.LinkedApps = ModelResult.LinkedApps[0];
                    cascadeModel.ApplicationID = ModelResult.AppId;
                    listModels.Add(cascadeModel);
                    ModelList["Model" + k] = JObject.Parse(JsonConvert.SerializeObject(cascadeModel));
                    k++;
                }
            }
            CreateCascadeModel(result, cascadeModelList, ModelList, listModels);
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(CascadeModelInsertion), "CASCADEMODELINSERTION - END :", string.IsNullOrEmpty(result.CorrelationId) ? default(Guid) : new Guid(result.CorrelationId), result.AppID, string.Empty, _templateInfo.ClientUID, _templateInfo.DCID);
        }

        private void CreateCascadeModel(IngrainRequestQueue result, JObject cascadeModelList, JObject ModelList, List<CascadeModelDictionary> listModels)
        {
            try
            {
                var cascadeCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAICascadedModels);
                //Assigninig attribbutes for cascade model
                GenericCascadeCollection data = new GenericCascadeCollection();
                data._id = Guid.NewGuid().ToString();
                data.CascadedId = result.CorrelationId;
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(CreateCascadeModel), "CREATECASCADEMODEL - STARTED :", string.IsNullOrEmpty(data.CascadedId) ? default(Guid) : new Guid(data.CascadedId), result.AppID, string.Empty, result.ClientID, result.DeliveryconstructId);
                data.Status = CONSTANTS.Deployed;
                data.ModelList = ModelList;
                data.Mappings = cascadeModelList[CONSTANTS.Mappings];
                data.ModelName = result.ModelName;
                data.ClientUId = Convert.ToString(cascadeModelList[CONSTANTS.ClientUId]);
                data.CreatedByUser = result.CreatedByUser;
                data.DeliveryConstructUID = Convert.ToString(cascadeModelList[CONSTANTS.DeliveryConstructUID]);
                data.Category = Convert.ToString(cascadeModelList[CONSTANTS.Category]);
                data.ModifiedByUser = result.CreatedByUser;
                data.CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                data.ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                //Inserting to cascade model
                var jsonColumns = Newtonsoft.Json.JsonConvert.SerializeObject(data);
                var insertBsonColumns = BsonSerializer.Deserialize<BsonDocument>(jsonColumns);
                cascadeCollection.InsertOne(insertBsonColumns);
                UpdateRequestStatus("85%");

                //form Mappping data
                var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAICascadedModels);
                var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.ModelList).Exclude(CONSTANTS.Id);
                var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CascadedId, result.CorrelationId);
                var result2 = collection.Find(filter).Project<BsonDocument>(projection).ToList();
                CascadeModelsCollectionList cascadeModelsCollection = new CascadeModelsCollectionList();
                List<string> inputColumns = new List<string>();
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(CreateCascadeModel), "CreateCascadeModel MAPPING - START :", string.IsNullOrEmpty(result.CorrelationId) ? default(Guid) : new Guid(result.CorrelationId), result.AppID, string.Empty, data.ClientUId, data.DeliveryConstructUID);
                if (result2.Count > 0)
                {
                    JObject cascadeData = JObject.Parse(result2[0][CONSTANTS.ModelList].ToString());
                    if (cascadeData != null)
                    {
                        List<CascadeModelsCollection> listModels2 = new List<CascadeModelsCollection>();
                        foreach (var item in cascadeData.Children())
                        {
                            if (item != null)
                            {
                                JProperty prop = item as JProperty;
                                if (prop != null)
                                {
                                    CascadeModelsCollection models = JsonConvert.DeserializeObject<CascadeModelsCollection>(prop.Value.ToString());
                                    listModels2.Add(models);
                                }
                            }
                        }
                        if (listModels2.Count > 0)
                        {
                            JObject modelsMapping = GetMapping(listModels2);
                            var bsondDoc = BsonDocument.Parse(modelsMapping.ToString());
                            var update = Builders<BsonDocument>.Update.Set("MappingData", bsondDoc);
                            var updateResult = collection.UpdateOne(filter, update);
                        }
                    }
                }
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(CreateCascadeModel), "CreateCascadeModel MAPPING - END :", string.IsNullOrEmpty(result.CorrelationId) ? default(Guid) : new Guid(result.CorrelationId), result.AppID, string.Empty, result.ClientID, result.DeliveryconstructId);
                //END            
                InsertDeployedModels(data, listModels, result);
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(CreateCascadeModel), "CREATECASCADEMODEL - END :", string.IsNullOrEmpty(data.CascadedId) ? default(Guid) : new Guid(data.CascadedId), result.AppID, string.Empty, result.ClientID, result.DeliveryconstructId);
            }
            catch (Exception ex)
            {
                UpdateRequestStatus("85%");
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(GenericAutotrainService), nameof(CreateCascadeModel), ex.Message + "- STACKTRACE - " + ex.StackTrace + ex.InnerException, ex, result.AppID, string.Empty, result.ClientID, result.DeliveryconstructId);
            }
        }
        private void InsertDeployedModels(GenericCascadeCollection data, List<CascadeModelDictionary> listModel, IngrainRequestQueue result)
        {
            CascadeModelDictionary modelDictionary = new CascadeModelDictionary();
            string publishURL = appSettings.GetSection("AppSettings").GetSection("publishURL").Value;
            string[] arr = new string[] { result.ApplicationName };
            bool encryptDB = false;
            bool DBEncryption = false;
            if (_isForAllData == true)
            {
                if (_dbEncryption == true)
                {
                    encryptDB = true;
                }
                else
                {
                    encryptDB = false;
                }
            }
            else
            {
                if (_dbEncryption == true && DBEncryption == true)
                {

                    encryptDB = true;
                }
                else
                {
                    encryptDB = false;
                }
            }
            if (listModel.Count > 0)
            {
                modelDictionary = listModel[listModel.Count - 1];
            }
            string sampleInput = string.Empty;
            string inputSample = string.Empty;
            var cascadeCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAICascadedModels);
            var cascadeFilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CascadedId, data.CascadedId);
            var cascadeProjection = Builders<BsonDocument>.Projection.Exclude("MappingData").Exclude(CONSTANTS.Id);
            var cascadeResult = cascadeCollection.Find(cascadeFilter).Project<BsonDocument>(cascadeProjection).ToList();
            if (cascadeResult.Count > 0)
            {
                sampleInput = AddCascadeSampleInput(cascadeResult[0]);
                inputSample = !string.IsNullOrEmpty(sampleInput) ? sampleInput.Replace(System.Environment.NewLine, String.Empty).Replace("\r\n", string.Empty) : null;
            }
            var AppIntegCollection = _database.GetCollection<AppIntegration>(CONSTANTS.AppIntegration);
            var filterBuilder1 = Builders<AppIntegration>.Filter;
            var AppFilter = filterBuilder1.Eq("ApplicationName", _Mapping.ApplicationName);

            var Projection = Builders<AppIntegration>.Projection.Include("BaseURL").Exclude("_id");
            var AppData = AppIntegCollection.Find(AppFilter).Project<AppIntegration>(Projection).FirstOrDefault();
            DeployModelsDto deployModel = new DeployModelsDto
            {
                _id = Guid.NewGuid().ToString(),
                CorrelationId = data.CascadedId,
                InstaId = null,
                ModelName = data.ModelName,
                Status = CONSTANTS.Deployed,
                ClientUId = data.ClientUId,
                DeliveryConstructUID = data.DeliveryConstructUID,
                DataSource = CONSTANTS.Cascading,
                DeployedDate = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                LinkedApps = arr,
                WebServices = "webservice",
                TemplateUsecaseId = result.TemplateUseCaseID,
                Accuracy = modelDictionary.Accuracy,
                AppId = modelDictionary.ApplicationID,
                ModelVersion = modelDictionary.ModelType,
                ModelType = modelDictionary.ProblemType,
                SourceName = CONSTANTS.Cascading,
                ModelURL = string.Format(publishURL + CONSTANTS.Zero, result.CorrelationId),
                InputSample = inputSample,
                IsPrivate = false,
                IsModelTemplate = false,
                DBEncryptionRequired = encryptDB,
                IsCascadeModel = true,
                TrainedModelId = null,
                Frequency = null,
                Category = data.Category,
                CreatedByUser = data.CreatedByUser,
                CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                ModifiedByUser = data.ModifiedByUser,
                ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                Language = CONSTANTS.English,
                IsModelTemplateDataSource = false
            };
            if (AppData != null)
            {
                deployModel.VDSLink = AppData.BaseURL;
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(InsertDeployedModels), "INSERTDEPLOYEDMODELS - END :", string.IsNullOrEmpty(deployModel.CorrelationId) ? default(Guid) : new Guid(deployModel.CorrelationId), deployModel.AppId, string.Empty, deployModel.ClientUId, deployModel.DeliveryConstructUID);
            var jsonColumns = Newtonsoft.Json.JsonConvert.SerializeObject(deployModel);
            var insertBsonColumns = BsonSerializer.Deserialize<BsonDocument>(jsonColumns);
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
            collection.InsertOne(insertBsonColumns);
            UpdateRequestStatus("90%");
        }

        private string AddCascadeSampleInput(BsonDocument result)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), "AddCascadeSampleInput", "START", string.Empty, string.Empty, string.Empty, string.Empty);
            string sampleInput = string.Empty;
            try
            {
                List<JObject> allModels = new List<JObject>();
                JObject mapping = new JObject();
                JArray listArray = new JArray();
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append("[");
                JObject singleObject = new JObject();
                if (result != null)
                {
                    JObject data = JObject.Parse(result.ToString());
                    mapping = JObject.Parse(result[CONSTANTS.Mappings].ToString());
                    if (data != null)
                    {
                        List<string> corids = new List<string>();
                        foreach (var item in data[CONSTANTS.ModelList].Children())
                        {
                            JProperty prop = item as JProperty;
                            if (prop != null)
                            {
                                DeployModelDetails model = JsonConvert.DeserializeObject<DeployModelDetails>(prop.Value.ToString());
                                var collection2 = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_DeployedModels);
                                var filter2 = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, model.CorrelationId);
                                var result2 = collection2.Find(filter2).Project<BsonDocument>(Builders<BsonDocument>.Projection.Include("InputSample").Exclude("_id")).ToList();
                                if (result2.Count > 0)
                                {
                                    allModels.Add(JObject.Parse(result2[0].ToString()));
                                    corids.Add(model.CorrelationId);
                                }
                            }
                        }
                        if (allModels.Count > 0)
                        {
                            JArray firstModel = new JArray();
                            if (this.EncryptDB(corids[0]))// CommonUtility.EncryptDB(corids[0], appSettings))
                            {
                                if (_IsAESKeyVault)
                                    firstModel = JArray.Parse(CryptographyUtility.Decrypt(allModels[0]["InputSample"].ToString()));
                                else
                                    firstModel = JArray.Parse(AesProvider.Decrypt(allModels[0]["InputSample"].ToString(), _aesKey, _aesVector));
                            }
                            else
                            {
                                firstModel = JArray.Parse(allModels[0]["InputSample"].ToString());
                            }
                            //JArray firstModel = JArray.Parse(allModels[0]["InputSample"].ToString());
                            for (int i = 0; i < firstModel.Count; i++) // main array loop
                            {
                                List<JObject> listJobject = new List<JObject>();
                                for (int j = 0; j < mapping.Count; j++)
                                {
                                    if (j == 0)
                                    {
                                        string modelName = string.Format("Model{0}", j + 1);
                                        JArray removeAraay1 = new JArray();
                                        if (this.EncryptDB(corids[j]))//(CommonUtility.EncryptDB(corids[j], appSettings))
                                        {
                                            if (_IsAESKeyVault)
                                                removeAraay1 = JArray.Parse(CryptographyUtility.Decrypt(allModels[j]["InputSample"].ToString()));
                                            else
                                                removeAraay1 = JArray.Parse(AesProvider.Decrypt(allModels[j]["InputSample"].ToString(), _aesKey, _aesVector));
                                        }
                                        else
                                        {
                                            removeAraay1 = JArray.Parse(allModels[j]["InputSample"].ToString());
                                        }
                                        //JArray removeAraay1 = JArray.Parse(allModels[j]["InputSample"].ToString());
                                        JObject obj1 = JObject.Parse(removeAraay1[i].ToString());
                                        listJobject.Add(obj1);
                                        //Model 1 Start                                        
                                        JArray removeAraay = new JArray();
                                        if (i > 0)
                                        {
                                            if (this.EncryptDB(corids[i]))//(CommonUtility.EncryptDB(corids[i], appSettings))
                                            {
                                                if (_IsAESKeyVault)
                                                    removeAraay = JArray.Parse(CryptographyUtility.Decrypt(allModels[i]["InputSample"].ToString()));
                                                else
                                                    removeAraay = JArray.Parse(AesProvider.Decrypt(allModels[i]["InputSample"].ToString(), _aesKey, _aesVector));
                                            }
                                            else
                                            {
                                                removeAraay = JArray.Parse(allModels[i]["InputSample"].ToString());
                                            }
                                            //removeAraay = JArray.Parse(allModels[i]["InputSample"].ToString());
                                        }
                                        else
                                        {
                                            if (this.EncryptDB(corids[i + 1]))//(CommonUtility.EncryptDB(corids[i + 1], appSettings))
                                            {
                                                if (_IsAESKeyVault)
                                                    removeAraay = JArray.Parse(CryptographyUtility.Decrypt(allModels[i + 1]["InputSample"].ToString()));
                                                else
                                                    removeAraay = JArray.Parse(AesProvider.Decrypt(allModels[i + 1]["InputSample"].ToString(), _aesKey, _aesVector));
                                            }
                                            else
                                            {
                                                removeAraay = JArray.Parse(allModels[i + 1]["InputSample"].ToString());
                                            }
                                            //removeAraay = JArray.Parse(allModels[i + 1]["InputSample"].ToString());
                                        }

                                        JObject obj2 = JObject.Parse(removeAraay[i].ToString());
                                        MappingAttributes mapping1b = JsonConvert.DeserializeObject<MappingAttributes>(mapping[modelName]["UniqueMapping"].ToString());
                                        MappingAttributes mapping12 = JsonConvert.DeserializeObject<MappingAttributes>(mapping[modelName]["TargetMapping"].ToString());
                                        obj2.Property(mapping1b.Target).Remove();
                                        obj2.Property(mapping12.Target).Remove();
                                        listJobject.Add(obj2);
                                    }
                                    else
                                    {
                                        //ID Mapping
                                        JArray removeAraay = new JArray();
                                        if (this.EncryptDB(corids[j + 1]))//(CommonUtility.EncryptDB(corids[j + 1], appSettings))
                                        {
                                            if (_IsAESKeyVault)
                                                removeAraay = JArray.Parse(CryptographyUtility.Decrypt(allModels[j + 1]["InputSample"].ToString()));
                                            else
                                                removeAraay = JArray.Parse(AesProvider.Decrypt(allModels[j + 1]["InputSample"].ToString(), _aesKey, _aesVector));
                                        }
                                        else
                                        {
                                            removeAraay = JArray.Parse(allModels[j + 1]["InputSample"].ToString());
                                        }
                                        //JArray removeAraay = JArray.Parse(allModels[j + 1]["InputSample"].ToString());
                                        JObject modelIncrement = JObject.Parse(removeAraay[i].ToString());
                                        string model = string.Format("Model{0}", j + 1);
                                        MappingAttributes mapping1 = JsonConvert.DeserializeObject<MappingAttributes>(mapping[model]["UniqueMapping"].ToString());
                                        //TargetMapping                                      
                                        MappingAttributes mapping12 = JsonConvert.DeserializeObject<MappingAttributes>(mapping[model]["TargetMapping"].ToString());
                                        modelIncrement.Property(mapping1.Target).Remove();
                                        modelIncrement.Property(mapping12.Target).Remove();
                                        listJobject.Add(modelIncrement);
                                    }

                                }
                                singleObject = new JObject();
                                JArray mainArray = new JArray();
                                foreach (JObject item in listJobject)
                                {
                                    singleObject.Merge(item, new JsonMergeSettings
                                    {
                                        // union array values together to avoid duplicates
                                        MergeArrayHandling = MergeArrayHandling.Union
                                    });
                                }
                                mainArray.Add(singleObject);
                                listArray.Add(mainArray);
                                stringBuilder.Append(singleObject + ",");
                            }
                            stringBuilder.Length -= 1;
                            stringBuilder.Append("]");
                        }
                        sampleInput = stringBuilder.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(GenericAutotrainService), nameof(AddCascadeSampleInput), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), "AddCascadeSampleInput", "END", string.Empty, string.Empty, string.Empty, string.Empty);
            return sampleInput;
        }

        private JObject GetMapping(List<CascadeModelsCollection> listModels)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), "GetMapping", "START", string.Empty, string.Empty, string.Empty, string.Empty);
            int counter = 1;
            JObject modelsMapping = new JObject();
            foreach (var model in listModels)
            {
                var collectonData = GetDataFromCollections(model.CorrelationId);
                if (collectonData.BusinessProblemData.Count > 0 & collectonData.DataCleanupData.Count > 0 & collectonData.FilteredData.Count > 0)
                {
                    bool DBEncryptionRequired = this.EncryptDB(model.CorrelationId);
                    if (DBEncryptionRequired)
                    {
                        if (_IsAESKeyVault)
                            collectonData.DataCleanupData[0][CONSTANTS.FeatureName] = BsonDocument.Parse(CryptographyUtility.Decrypt(collectonData.DataCleanupData[0][CONSTANTS.FeatureName].AsString));
                        else
                            collectonData.DataCleanupData[0][CONSTANTS.FeatureName] = BsonDocument.Parse(AesProvider.Decrypt(collectonData.DataCleanupData[0][CONSTANTS.FeatureName].AsString, _aesKey, _aesVector));
                        if (collectonData.DataCleanupData[0].Contains(CONSTANTS.NewFeatureName))
                        {
                            if (collectonData.DataCleanupData[0][CONSTANTS.NewFeatureName].ToString() != "{ }")
                            {
                                if (_IsAESKeyVault)
                                    collectonData.DataCleanupData[0][CONSTANTS.NewFeatureName] = BsonDocument.Parse(CryptographyUtility.Decrypt(collectonData.DataCleanupData[0][CONSTANTS.NewFeatureName].AsString));
                                else
                                    collectonData.DataCleanupData[0][CONSTANTS.NewFeatureName] = BsonDocument.Parse(AesProvider.Decrypt(collectonData.DataCleanupData[0][CONSTANTS.NewFeatureName].AsString, _aesKey, _aesVector));
                            }
                        }
                        if (_IsAESKeyVault)
                            collectonData.FilteredData[0][CONSTANTS.ColumnUniqueValues] = BsonDocument.Parse(CryptographyUtility.Decrypt(collectonData.FilteredData[0][CONSTANTS.ColumnUniqueValues].AsString));
                        else
                            collectonData.FilteredData[0][CONSTANTS.ColumnUniqueValues] = BsonDocument.Parse(AesProvider.Decrypt(collectonData.FilteredData[0][CONSTANTS.ColumnUniqueValues].AsString, _aesKey, _aesVector));
                    }
                    JObject datas = JObject.Parse(collectonData.DataCleanupData[0].ToString());
                    JObject combinedFeatures = new JObject();
                    combinedFeatures = this.CombinedFeatures(datas);
                    if (combinedFeatures != null)
                        collectonData.DataCleanupData[0][CONSTANTS.FeatureName] = BsonDocument.Parse(combinedFeatures.ToString());
                    JObject uniqueData = JObject.Parse(collectonData.FilteredData[0].ToString());
                    JObject datacleanup = JObject.Parse(collectonData.DataCleanupData[0].ToString());
                    var dict = GetColumnDataatypes(datacleanup);
                    JObject InputColumns = new JObject();
                    foreach (var item in dict)
                    {
                        DatatypeDict datatype = new DatatypeDict();
                        datatype.Datatype = item.Value;
                        List<string> stringList = new List<string>();
                        List<double> numericList = new List<double>();
                        foreach (var value in uniqueData[CONSTANTS.ColumnUniqueValues][item.Key].Children())
                        {
                            if (value != null)
                            {
                                if (item.Value == "float64" || item.Value == "int64")
                                {
                                    numericList.Add(Convert.ToDouble(value));
                                }
                                else
                                {
                                    stringList.Add(Convert.ToString(value));
                                }
                            }
                        }
                        if (item.Value == "float64" || item.Value == "int64")
                        {
                            datatype.UniqueValues = numericList.ToArray();
                            datatype.Min = numericList.Min();
                            datatype.Max = numericList.Max();
                            datatype.Metric = numericList.Average();
                        }
                        else
                            datatype.UniqueValues = stringList.ToArray();
                        Dictionary<string, DatatypeDict> keyValues = new Dictionary<string, DatatypeDict>();
                        keyValues.Add(item.Key, datatype);
                        JObject Current = JObject.Parse(JsonConvert.SerializeObject(keyValues));
                        InputColumns.Merge(Current, new Newtonsoft.Json.Linq.JsonMergeSettings
                        {
                            MergeArrayHandling = Newtonsoft.Json.Linq.MergeArrayHandling.Union
                        });
                    }
                    JObject mainMapping = new JObject();
                    mainMapping[CONSTANTS.ModelName] = model.ModelName;
                    mainMapping[CONSTANTS.ProblemType] = model.ProblemType;
                    mainMapping[CONSTANTS.ModelType] = model.ModelType;
                    mainMapping[CONSTANTS.CorrelationId] = model.CorrelationId;
                    mainMapping[CONSTANTS.TargetColumn] = collectonData.BusinessProblemData[0][CONSTANTS.TargetColumn].ToString();
                    mainMapping[CONSTANTS.TargetUniqueIdentifier] = collectonData.BusinessProblemData[0][CONSTANTS.TargetUniqueIdentifier].ToString();
                    mainMapping[CONSTANTS.InputColumns] = JObject.FromObject(InputColumns);
                    modelsMapping[CONSTANTS.Model + counter] = JObject.FromObject(mainMapping);
                    counter++;
                }
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), "GetMapping", "END", string.Empty, string.Empty, string.Empty, string.Empty);
            return modelsMapping;
        }
        private Dictionary<string, string> GetColumnDataatypes(JObject datacleanup)
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            foreach (var item in datacleanup[CONSTANTS.FeatureName].Children())
            {
                JProperty prop = item as JProperty;
                if (prop != null)
                {
                    foreach (var datatype in datacleanup[CONSTANTS.FeatureName][prop.Name][CONSTANTS.Datatype].Children())
                    {
                        JProperty type = datatype as JProperty;
                        if (type != null)
                        {
                            if (type.Value.ToString() == CONSTANTS.True)
                                dict.Add(prop.Name, type.Name);
                        }
                    }
                }
            }
            return dict;
        }
        private JObject CombinedFeatures(JObject datas)
        {
            List<string> lstNewFeatureName = new List<string>();
            List<string> lstFeatureName = new List<string>();
            if (datas.ContainsKey(CONSTANTS.NewFeatureName) && datas[CONSTANTS.NewFeatureName].HasValues && !string.IsNullOrEmpty(Convert.ToString(datas[CONSTANTS.NewFeatureName])))
            {
                foreach (var child in datas[CONSTANTS.FeatureName].Children())
                {
                    JProperty prop = child as JProperty;
                    lstFeatureName.Add(prop.Name);
                }

                List<JToken> lstNewFeature = new List<JToken>();
                foreach (var child in datas[CONSTANTS.NewFeatureName].Children())
                {
                    JProperty prop = child as JProperty;
                    lstNewFeatureName.Add(prop.Name);
                    if (!lstFeatureName.Contains(prop.Name))
                        lstNewFeature.Add(child);
                }

                List<JToken> MergerdFeatures = new List<JToken>();
                foreach (var feature in datas[CONSTANTS.FeatureName].Children())
                {
                    JProperty prop = feature as JProperty;
                    if (!lstNewFeatureName.Contains(prop.Name))
                    {
                        MergerdFeatures.Add(feature);
                    }
                    else
                    {
                        foreach (var newFeature in datas[CONSTANTS.NewFeatureName].Children())
                        {
                            JProperty addFeature = newFeature as JProperty;
                            if (prop.Name.Equals(addFeature.Name))
                            {
                                MergerdFeatures.Add(newFeature);
                                break;
                            }
                        }
                    }
                }

                if (lstNewFeature.Count > 0)
                    MergerdFeatures.AddRange(lstNewFeature);

                JObject Features = new JObject() { MergerdFeatures };

                return Features;
            }

            return null;
        }
        private CascadeBsonDocument GetDataFromCollections(string correlationId)
        {
            CascadeBsonDocument cascadeBson = new CascadeBsonDocument();
            var collection2 = _database.GetCollection<BsonDocument>(CONSTANTS.PS_BusinessProblem);
            var projection2 = Builders<BsonDocument>.Projection.Include(CONSTANTS.TargetColumn).Include(CONSTANTS.TargetUniqueIdentifier).Exclude(CONSTANTS.Id);
            var filter2 = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            cascadeBson.BusinessProblemData = collection2.Find(filter2).Project<BsonDocument>(projection2).ToList();

            var dataCleanupCcollection = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataCleanup);
            var projection3 = Builders<BsonDocument>.Projection.Include(CONSTANTS.FeatureName).Include(CONSTANTS.NewFeatureName).Exclude(CONSTANTS.Id);
            var filter3 = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            cascadeBson.DataCleanupData = dataCleanupCcollection.Find(filter3).Project<BsonDocument>(projection3).ToList();

            var filteredCollection = _database.GetCollection<BsonDocument>(CONSTANTS.DataCleanUPFilteredData);
            var projection4 = Builders<BsonDocument>.Projection.Include(CONSTANTS.ColumnUniqueValues).Exclude(CONSTANTS.Id);
            var filter4 = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            cascadeBson.FilteredData = filteredCollection.Find(filter4).Project<BsonDocument>(projection4).ToList();
            return cascadeBson;
        }
        private void delete_backUpModelSPP(string CorrelationId, string status)
        {
            try
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
                var filterBuilder = Builders<BsonDocument>.Filter;
                var filter_error = filterBuilder.Eq(CONSTANTS.CorrelationId, CorrelationId) & filterBuilder.Eq(CONSTANTS.Status, CONSTANTS.E);

                foreach (string name in collectionNames)
                {
                    var collection = _database.GetCollection<BsonDocument>(name);
                    if (status == CONSTANTS.C)
                    {
                        if (collection.Find(filter_backUp).ToList().Count > 0)
                        {
                            collection.DeleteMany(filter_backUp);
                        }
                    }
                    else
                    {
                        if (collection.Find(filter_backUp).ToList().Count > 0)
                        {
                            if (name == CONSTANTS.SSAIIngrainRequests)
                            {
                                if (collection.Find(filter_error).ToList().Count > 0)
                                {
                                    var filter_CorrIderr = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, CorrelationId + "_error");
                                    collection.DeleteMany(filter_CorrIderr);
                                    var update = Builders<BsonDocument>.Update.Set("CorrelationId", CorrelationId + "_error");
                                    collection.UpdateOne(filter_error, update);
                                }
                            }
                            collection.DeleteMany(filter);
                            update_backUP(CorrelationId, name);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CustomAppsActivityLog log = new CustomAppsActivityLog();
                log.CorrelationId = CorrelationId;
                log.FeatureName = "PrivateModelTraining";
                log.StackTrace = ex.StackTrace;
                log.ErrorMessage = ex.Message;
                log.ErrorMethod = "delete_backUpModelSPP";
                log.CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                log.CreatedBy = "SYSTEM";

                this.InsertCustomAppsActivityLog(log);
            }
        }
        private void update_backUP(string CorrelationId, string collection_name)
        {
            var update = Builders<BsonDocument>.Update.Set("CorrelationId", CorrelationId.Replace("_backUp", ""));
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, CorrelationId + "_backUp");
            var collection = _database.GetCollection<BsonDocument>(collection_name);
            collection.UpdateMany(filter, update);
        }

        public void CallBackErrorLog(IngrainResponseData CallBackResponse, string AppName, string baseAddress, HttpResponseMessage httpResponse, string clientId, string DCId, string applicationId, string usecaseId, string requestId, string errorTrace, string userId)
        {
            CallBackErrorLog(CallBackResponse, AppName, baseAddress, httpResponse, clientId, DCId, applicationId, usecaseId, requestId, errorTrace, userId, 0);
        } 
        
        public void CallBackErrorLog(IngrainResponseData CallBackResponse, string AppName, string baseAddress, HttpResponseMessage httpResponse, string clientId, string DCId, string applicationId, string usecaseId, string requestId, string errorTrace, string userId, int retryCount)
        {
            string result = string.Empty;
            string isNoficationSent = "false";
           
            if (httpResponse != null)
            {
                if (httpResponse.IsSuccessStatusCode)
                {
                    isNoficationSent = "true";
                    result = httpResponse.StatusCode + " - Content: " + httpResponse.Content.ReadAsStringAsync().Result;                    
                }
                else
                {
                    retryCount += 1;
                    result = httpResponse.StatusCode + "-" + httpResponse.ReasonPhrase.ToString() + "- Content: " + httpResponse.Content.ReadAsStringAsync().Result;

                    if (CallBackResponse.Status == CONSTANTS.ErrorMessage && AppName == CONSTANTS.SPAAPP)
                    {
                        CallBackResponse.ErrorMessage = CallBackResponse.ErrorMessage + " -" + "Record will be deleted from IngrainRequest for CorrelationId : " + CallBackResponse.CorrelationId;
                    }
                }
            }
          
            if (errorTrace != null)
            {
                CallBackResponse.ErrorMessage = CallBackResponse.ErrorMessage + "StackTrace :" + errorTrace;
            }
            CallBackErrorLog callBackErrorLog = new CallBackErrorLog
            {
                CorrelationId = CallBackResponse.CorrelationId,
                RequestId = requestId,
                UseCaseId = usecaseId,
                UniqueId = requestId,
                ApplicationID = applicationId,
                ClientId = clientId,
                DCID = DCId,
                BaseAddress = baseAddress,
                Message = CallBackResponse.Message, //NotificationMessage
                ErrorMessage = CallBackResponse.ErrorMessage, // isNofication as yes whenever its called
                Status = CallBackResponse.Status, //sendnotification
                CallbackURLResponse = result,//CallBackResponse,
                CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                CreatedBy = userId,
                ModifiedBy = userId
            };

            var logCollection = _database.GetCollection<CallBackErrorLog>("AuditTrailLog");
            //var filterBuilder = Builders<CallBackErrorLog>.Filter;
            //var filter = filterBuilder.Eq("RequestId", callBackErrorLog.RequestId);
            //var Projection = Builders<CallBackErrorLog>.Projection.Exclude("_id");
            //var logResult = logCollection.Find(filter).Project<CallBackErrorLog>(Projection).FirstOrDefault();

            logCollection.InsertOneAsync(callBackErrorLog);
            //update ingrai Request collection
            if (baseAddress != null)
            {
                UpdateIngrainRequest(requestId, CallBackResponse.Message + "" + result, isNoficationSent, CallBackResponse.Status, retryCount);
            }
        }

        public void UpdateIngrainRequest(string requestId, string notificationMessage, string isNoficationSent, string sendnotification, int retryCount )
        {
            var requestCollection = _database.GetCollection<IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
            var filter = Builders<IngrainRequestQueue>.Filter.Where(x => x.RequestId == requestId);
            var update = Builders<IngrainRequestQueue>.Update.Set(x => x.IsNotificationSent, isNoficationSent).Set(x => x.NotificationMessage, notificationMessage).Set(x => x.SendNotification, sendnotification).Set(x => x.ModifiedOn, DateTime.Now.ToString(CONSTANTS.DateHoursFormat)).Set(x => x.RetryCount, retryCount);
            requestCollection.UpdateOne(filter, update);
        }

        public void TransformFMModel(IngrainRequestQueue result)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(TransformFMModel), "TransformFM START", string.IsNullOrEmpty(result.CorrelationId) ? default(Guid) : new Guid(result.CorrelationId), result.AppID, string.Empty, result.ClientID, result.DeliveryconstructId);
            try
            {
                var deployCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
                var filterBuilder = Builders<BsonDocument>.Filter;
                var projection1 = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
                var Sourcenamefilter = filterBuilder.Eq(CONSTANTS.CorrelationId, result.CorrelationId);
                var SourceData = deployCollection.Find(Sourcenamefilter).Project<BsonDocument>(projection1).ToList();
                BsonElement element;
                var exists = SourceData[0].TryGetElement("DBEncryptionRequired", out element);
                //if (exists)
                //    _DBEncryptionRequired = (bool)SourceData[0]["DBEncryptionRequired"];
                //else
                //    _DBEncryptionRequired = false;
                if (exists)
                    _TemplateDBEncryptionRequired = (bool)SourceData[0]["DBEncryptionRequired"];
                else
                    _TemplateDBEncryptionRequired = false;
                _requestId = result.RequestId;
                NewCorrelationId = result.CorrelationId;
                _requestId = result.RequestId;
                _CheckPrediction = result.pageInfo;
                _CallbackURL = result.AppURL;
                _templateInfo.ClientUID = result.ClientId;
                _templateInfo.DCID = result.DeliveryconstructId;
                _templateInfo.pageInfo = result.pageInfo;
                _Mapping.CreatedByUser = result.CreatedByUser;
                _Mapping.UsecaseID = result.TemplateUseCaseID;
                _Mapping.ApplicationID = result.AppID;
                bool status = true;
                List<PublicTemplateMapping> MappingResult = new List<PublicTemplateMapping>();
                MappingResult = GetDataMapping(_Mapping.UsecaseID, _Mapping.ApplicationID);
                if (MappingResult.Count > 0)
                {
                    _Mapping.UsecaseName = MappingResult[0].UsecaseName;
                    _Mapping.SourceName = MappingResult[0].SourceName;
                    _Mapping.SourceURL = MappingResult[0].SourceURL;
                    _Mapping.ApplicationName = MappingResult[0].ApplicationName;
                    _Mapping.DateColumn = MappingResult[0].DateColumn;
                    _Mapping.IterationUID = MappingResult[0].IterationUID;
                    _templateInfo.ModelName = _Mapping.ApplicationName + "_" + _Mapping.UsecaseName;
                    _Mapping.TeamAreaUId = result.TeamAreaUId;
                    string callbackResonse = string.Empty;
                    GetBusinessProblemData(_Mapping.UsecaseID);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(TransformFMModel), "_Mapping.SourceName--" + _Mapping.SourceName + "-_Mapping.SourceURL-" + _Mapping.SourceURL, result.AppID, string.Empty, _templateInfo.ClientUID, _templateInfo.DCID);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(PrivateModelTraining), "GetIngrainRequestData status--" + status, string.IsNullOrEmpty(NewCorrelationId) ? default(Guid) : new Guid(NewCorrelationId), result.AppID, string.Empty, _templateInfo.ClientUID, _templateInfo.DCID);

                    InsertBusinessProblem(_templateInfo, NewCorrelationId);

                }
                _GenericDataResponse.Status = null;
                StartFMDataEngineering(result.CorrelationId, result.CreatedByUser, result.ProblemType, result.AppID, false);
                if (_GenericDataResponse.Status == CONSTANTS.C)
                {
                    UpdateRequestStatus("15%");
                    _GenericDataResponse.Status = null;
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(TransformFMModel), "TransformFM Model Engineering starting", string.IsNullOrEmpty(result.CorrelationId) ? default(Guid) : new Guid(result.CorrelationId), result.AppID, string.Empty, result.ClientID, result.DeliveryconstructId);
                    StartModelEngineering(result.CorrelationId, result.CreatedByUser, result.ProblemType, result.AppID);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(TransformFMModel), "TransformFM Model Engineering Completed Staus--" + _GenericDataResponse.Status, string.IsNullOrEmpty(result.CorrelationId) ? default(Guid) : new Guid(result.CorrelationId), result.AppID, string.Empty, result.ClientID, result.DeliveryconstructId);
                    if (_GenericDataResponse.Status == CONSTANTS.C)
                    {
                        UpdateRequestStatus("20%");
                        _GenericDataResponse.Status = null;
                        DeployModel(result.CorrelationId, result.CreatedByUser, result.ProblemType, result.AppID);
                        if (_GenericDataResponse.Status == CONSTANTS.C)
                        {
                            UpdateRequestStatus("25%");
                            DataEngineering dataEngineering2 = InsertCustomRequest(result.CorrelationId, result.CreatedByUser);
                            _GenericDataResponse.Status = dataEngineering2.Status;
                            _GenericDataResponse.Message = dataEngineering2.Message;
                            _GenericDataResponse.ErrorMessage = dataEngineering2.Message;
                            if (dataEngineering2.Status == CONSTANTS.C)
                            {
                                UpdateRequestStatus("30%");
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(TransformFMModel), "TransformFM DeployModel commpleted Status--" + _GenericDataResponse.Status, string.IsNullOrEmpty(result.CorrelationId) ? default(Guid) : new Guid(result.CorrelationId), result.AppID, string.Empty, result.ClientID, result.DeliveryconstructId);
                                //Create new model with new correlationId and add python data transformation request.
                                TransformFMIngestedData(result, result.FMCorrelationId);
                                UpdateRequestStatus("40%");
                                //Check the TransformIngestedData Status
                                DataEngineering dataEngineering = CheckFMTransformStatus(result.FMCorrelationId);
                                if (dataEngineering.Status == CONSTANTS.C)
                                {
                                    InsertFMBusinessProblem(_templateInfo, result.FMCorrelationId);
                                    //Start Second model training
                                    FMActualModelTraining(result);
                                    if (result.IsFMVisualize)
                                    {
                                        //For FM Visualization need to call  the python
                                        string uniqId = FMVisualizeInsertRequest(result);
                                        //check the prediction completed or not
                                        DataEngineering predictionResult = GetFmVisualizePredictionresult(result, uniqId);
                                        if (predictionResult.Status == CONSTANTS.C && predictionResult.Progress == CONSTANTS.Hundred)
                                        {
                                            UpdateRequestStatus("100%");
                                            UpdateTrainingStatus(CONSTANTS.C, CONSTANTS.Completed, CONSTANTS.Completed, result.CorrelationId, _templateInfo.ClientUID, _templateInfo.DCID, _Mapping.ApplicationID, _Mapping.UsecaseID, null, _Mapping.CreatedByUser);
                                        }
                                        else
                                        {
                                            UpdateTrainingStatus(CONSTANTS.E, predictionResult.Message, CONSTANTS.Error, result.CorrelationId, _templateInfo.ClientUID, _templateInfo.DCID, _Mapping.ApplicationID, _Mapping.UsecaseID, null, _Mapping.CreatedByUser);
                                            DeleteDeployModel(result.FMCorrelationId);
                                            DeleteDeployModel(result.CorrelationId);
                                        }
                                    }
                                    else
                                    {
                                        UpdateRequestStatus("100%");
                                        UpdateTrainingStatus(CONSTANTS.C, CONSTANTS.Completed, CONSTANTS.Completed, result.CorrelationId, _templateInfo.ClientUID, _templateInfo.DCID, _Mapping.ApplicationID, _Mapping.UsecaseID, null, _Mapping.CreatedByUser);
                                    }
                                }
                                if (dataEngineering.Status == CONSTANTS.E)
                                {
                                    UpdateTrainingStatus(_GenericDataResponse.Status, _GenericDataResponse.Message, _GenericDataResponse.Status, result.CorrelationId, result.ClientID, result.DeliveryconstructId, result.AppID, result.TemplateUseCaseID, _GenericDataResponse.ErrorMessage, result.CreatedByUser);
                                    DeleteDeployModel(result.FMCorrelationId);
                                    DeleteDeployModel(result.CorrelationId);
                                }
                            }
                        }
                    }
                    if (_GenericDataResponse.Status == CONSTANTS.E)
                    {
                        UpdateTrainingStatus(_GenericDataResponse.Status, _GenericDataResponse.Message, _GenericDataResponse.Status, result.CorrelationId, result.ClientID, result.DeliveryconstructId, result.AppID, result.TemplateUseCaseID, _GenericDataResponse.ErrorMessage, result.CreatedByUser);
                        DeleteDeployModel(result.FMCorrelationId);
                        DeleteDeployModel(result.CorrelationId);
                    }
                }
                if (_GenericDataResponse.Status == CONSTANTS.E)
                {
                    UpdateTrainingStatus(_GenericDataResponse.Status, _GenericDataResponse.Message, _GenericDataResponse.Status, result.CorrelationId, result.ClientID, result.DeliveryconstructId, result.AppID, result.TemplateUseCaseID, _GenericDataResponse.ErrorMessage, result.CreatedByUser);
                    DeleteDeployModel(result.FMCorrelationId);
                    DeleteDeployModel(result.CorrelationId);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(GenericAutotrainService), nameof(TransformFMModel), ex.Message, ex, result.AppID, string.Empty, result.ClientID, result.DeliveryconstructId);
                _GenericDataResponse.ErrorMessage = ex.Message + CONSTANTS.ThreeDots + ex.StackTrace;
                _GenericDataResponse.Status = CONSTANTS.E;
                _GenericDataResponse.Message = Resource.IngrainResx.EngineeringDataError;
                UpdateTrainingStatus(_GenericDataResponse.Status, _GenericDataResponse.Message, _GenericDataResponse.Status, result.CorrelationId, result.ClientID, result.DeliveryconstructId, result.AppID, result.TemplateUseCaseID, _GenericDataResponse.ErrorMessage, result.CreatedByUser);
                DeleteDeployModel(result.FMCorrelationId);
                DeleteDeployModel(result.CorrelationId);
            }

        }
        private void InsertFMBusinessProblem(GenericTemplatemapping templateInfo, string CorrelationId)
        {
            BusinessProblemDataDTO businessProblemData = new BusinessProblemDataDTO
            {
                BusinessProblems = templateInfo.BusinessProblems,
                TargetColumn = CONSTANTS.ReleaseSuccessProbability,
                TargetUniqueIdentifier = CONSTANTS.ReleaseName,
                CorrelationId = CorrelationId,
                TimeSeries = templateInfo.TimeSeries,
                ProblemType = null,
                CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                CreatedByUser = _Mapping.CreatedByUser,
                ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                ModifiedByUser = _Mapping.CreatedByUser,
                _id = Guid.NewGuid().ToString(),
                ClientUId = templateInfo.ClientUID,
                DeliveryConstructUID = templateInfo.DCID,
                AppId = templateInfo.AppicationID,
                IsCustomColumnSelected = CONSTANTS.False
            };
            //Get the columnlist from psingestedData
            var collection = _database.GetCollection<IngestDataColumn>(CONSTANTS.PSIngestedData);
            var filter = Builders<IngestDataColumn>.Filter.Eq(CONSTANTS.CorrelationId, CorrelationId);
            var projection = Builders<IngestDataColumn>.Projection.Include(CONSTANTS.ColumnsList).Include(CONSTANTS.CorrelationId).Exclude(CONSTANTS.Id);
            var psresult = collection.Find(filter).Project<IngestDataColumn>(projection).ToList();
            if (psresult.Count > 0)
            {
                var list = new List<string>(psresult[0].ColumnsList);
                list.Remove(businessProblemData.TargetColumn);
                list.Remove(businessProblemData.TargetUniqueIdentifier);
                businessProblemData.InputColumns = list.ToArray();
            }
            businessProblemData.AvailableColumns = new string[] { };
            var jsonColumns = JsonConvert.SerializeObject(businessProblemData);
            var pscollection = _database.GetCollection<BsonDocument>(CONSTANTS.PSBusinessProblem);
            var psfilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, CorrelationId);
            pscollection.DeleteOne(psfilter);
            Thread.Sleep(2000);
            var insertBsonColumns = BsonSerializer.Deserialize<BsonDocument>(jsonColumns);
            pscollection.InsertOne(insertBsonColumns);
        }
        private DataEngineering InsertCustomRequest(string correlationId, string userId)
        {
            DataEngineering dataEngineering = new DataEngineering();
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
                pageInfo = CONSTANTS.PredictCascade,
                ParamArgs = CONSTANTS.CurlyBraces,
                Function = CONSTANTS.PredictCascade,
                CreatedByUser = userId,
                CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                ModifiedByUser = userId,
                ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                LastProcessedOn = null,
            };
            var requestQueue = JsonConvert.SerializeObject(ingrainRequest);
            var insertRequestQueue = BsonSerializer.Deserialize<BsonDocument>(requestQueue);
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
            collection.InsertOne(insertRequestQueue);
            Thread.Sleep(2000);
            bool callMethod = true;
            while (callMethod)
            {
                var useCaseData = CheckPythonProcess(correlationId, CONSTANTS.PredictCascade);
                if (!string.IsNullOrEmpty(useCaseData))
                {
                    JObject queueData = JObject.Parse(useCaseData);
                    dataEngineering.Status = (string)queueData[CONSTANTS.Status];
                    dataEngineering.Progress = (string)queueData[CONSTANTS.Progress];
                    dataEngineering.Message = (string)queueData[CONSTANTS.Message];
                    if (dataEngineering.Status == CONSTANTS.C & dataEngineering.Progress == CONSTANTS.Hundred)
                    {
                        callMethod = false;
                        dataEngineering.IsComplete = true;
                    }
                    else if (dataEngineering.Status == CONSTANTS.E)
                    {
                        callMethod = false;
                        dataEngineering.IsComplete = false;
                    }
                    else
                    {
                        Thread.Sleep(1000);
                    }
                }
            }
            return dataEngineering;
        }
        private void StartFMDataEngineering(string correlationId, string userId, string ProblemType, string AppID, bool isFmModel)
        {
            bool isDataCurationCompleted = false;
            bool isDataTransformationCompleted = false;
            DataEngineering dataEngineering = new DataEngineering();
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(StartFMDataEngineering), "Generic Self Service StartDataEngineering is Started", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), AppID, string.Empty, string.Empty, string.Empty);
            try
            {
                dataEngineering = GetDataCuration(correlationId, CONSTANTS.DataCleanUp, userId, AppID);

                _GenericDataResponse.Status = dataEngineering.Status;
                if (dataEngineering.Status == CONSTANTS.E)
                {
                    _GenericDataResponse.ErrorMessage = Resource.IngrainResx.ErrorDataEngineering + dataEngineering.Message;
                }
                if (dataEngineering.Status == CONSTANTS.C)
                {
                    isDataCurationCompleted = this.IsDataCurationComplete(correlationId, isFmModel);
                }

                LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(StartFMDataEngineering), "isDataCurationCompleted: " + isDataCurationCompleted, AppID, string.Empty, string.Empty, string.Empty);
                if (isDataCurationCompleted)
                {
                    PreProcessModelDTO preProcessModelDTO = new PreProcessModelDTO();
                    isDataTransformationCompleted = this.CreatePreprocess(correlationId, userId, ProblemType);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(StartFMDataEngineering), "isDataTransformationCompleted: " + isDataTransformationCompleted, AppID, string.Empty, string.Empty, string.Empty);
                    if (isDataTransformationCompleted)
                    {
                        dataEngineering = GetDatatransformation(correlationId, CONSTANTS.DataPreprocessing, userId, AppID);
                        _GenericDataResponse.Status = dataEngineering.Status;
                        switch (dataEngineering.Status)
                        {
                            case CONSTANTS.E:
                                _GenericDataResponse.ErrorMessage = Resource.IngrainResx.ErrorDataEngineering + dataEngineering.Message;
                                break;

                            case CONSTANTS.C:
                                _GenericDataResponse.Message = Resource.IngrainResx.DataEngineering;
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(GenericAutotrainService), nameof(StartFMDataEngineering), ex.Message, ex, AppID, string.Empty, string.Empty, string.Empty);
                _GenericDataResponse.ErrorMessage = ex.Message + CONSTANTS.ThreeDots + ex.StackTrace;
                _GenericDataResponse.Status = CONSTANTS.E;
                _GenericDataResponse.Message = Resource.IngrainResx.EngineeringDataError;
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(StartFMDataEngineering), "Generic Self Service StartFMDataEngineering is Ended", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), AppID, string.Empty, string.Empty, string.Empty);
        }
        private void TransformFMIngestedData(IngrainRequestQueue result, string correlationId)
        {
            IngrainRequestQueue ingrainRequest = new IngrainRequestQueue
            {
                _id = Guid.NewGuid().ToString(),
                CorrelationId = correlationId,
                RequestId = Guid.NewGuid().ToString(),
                ProcessId = CONSTANTS.Null,
                ModelName = result.ModelName,
                Status = CONSTANTS.Null,
                RequestStatus = CONSTANTS.New,
                Message = CONSTANTS.Null,
                RetryCount = 0,
                ProblemType = CONSTANTS.Null,
                UniId = CONSTANTS.Null,
                InstaID = CONSTANTS.Null,
                Progress = CONSTANTS.Null,
                pageInfo = CONSTANTS.TransformIngestedData,
                ParamArgs = CONSTANTS.Null,
                TemplateUseCaseID = result.CorrelationId,
                Function = CONSTANTS.TransformIngestedData,
                CreatedByUser = result.CreatedByUser,
                CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                ModifiedByUser = CONSTANTS.Null,
                ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                LastProcessedOn = CONSTANTS.Null,
                ClientId = result.ClientId,
                DeliveryconstructId = result.DeliveryconstructId,
                UseCaseID = CONSTANTS.Null,
                EstimatedRunTime = CONSTANTS.Null
            };
            InsertRequests(ingrainRequest);
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(TransformFMIngestedData), "TransformFMIngestedData request inserted END", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), result.AppID, string.Empty, result.ClientID, result.DeliveryconstructId);
            Thread.Sleep(2000);
        }

        private DataEngineering CheckFMTransformStatus(string correlationId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(CheckFMTransformStatus), "CheckFMTransformStatusStarted", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            bool callMethod = true;
            DataEngineering dataEngineering = new DataEngineering();
            try
            {
                while (callMethod)
                {
                    var useCaseData = CheckPythonProcess(correlationId, CONSTANTS.TransformIngestedData);
                    if (!string.IsNullOrEmpty(useCaseData))
                    {
                        JObject queueData = JObject.Parse(useCaseData);
                        dataEngineering.Status = (string)queueData[CONSTANTS.Status];
                        dataEngineering.Progress = (string)queueData[CONSTANTS.Progress];
                        dataEngineering.Message = (string)queueData[CONSTANTS.Message];
                        _GenericDataResponse.Status = dataEngineering.Status;
                        _GenericDataResponse.Message = dataEngineering.Message;
                        _GenericDataResponse.ErrorMessage = dataEngineering.Message;
                        if (dataEngineering.Status == CONSTANTS.C & dataEngineering.Progress == CONSTANTS.Hundred)
                        {
                            callMethod = false;
                            dataEngineering.IsComplete = true;
                            return dataEngineering;
                        }
                        else if (dataEngineering.Status == CONSTANTS.E)
                        {
                            callMethod = false;
                            dataEngineering.IsComplete = false;
                            return dataEngineering;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(GenericAutotrainService), nameof(CheckFMTransformStatus), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
            return dataEngineering;
        }

        private void FMActualModelTraining(IngrainRequestQueue result)
        {
            _GenericDataResponse.Status = null;
            StartFMDataEngineering(result.FMCorrelationId, result.CreatedByUser, problemType, result.AppID, true);
            if (_GenericDataResponse.Status == CONSTANTS.C)
            {
                UpdateRequestStatus("65%");
                _GenericDataResponse.Status = null;
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(TransformFMModel), "TRANSFORMFM SECOND MODELENGINEERING STARTING", string.IsNullOrEmpty(result.FMCorrelationId) ? default(Guid) : new Guid(result.FMCorrelationId), result.AppID, string.Empty, result.ClientID, result.DeliveryconstructId);
                StartModelEngineering(result.FMCorrelationId, result.CreatedByUser, problemType, result.AppID);
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(TransformFMModel), "TRANSFORMFM SECOND MODELENGINEERING COMPLETED--" + _GenericDataResponse.Status, string.IsNullOrEmpty(result.FMCorrelationId) ? default(Guid) : new Guid(result.FMCorrelationId), result.AppID, string.Empty, result.ClientID, result.DeliveryconstructId);
                if (_GenericDataResponse.Status == CONSTANTS.C)
                {
                    UpdateRequestStatus("80%");
                    _GenericDataResponse.Status = null;
                    DeployModel(result.FMCorrelationId, result.CreatedByUser, problemType, result.AppID);
                    UpdateRequestStatus("85%");
                }
            }
            if (_GenericDataResponse.Status == CONSTANTS.E)
            {
                UpdateTrainingStatus(_GenericDataResponse.Status, _GenericDataResponse.Message, _GenericDataResponse.Status, result.CorrelationId, result.ClientID, result.DeliveryconstructId, result.AppID, result.TemplateUseCaseID, _GenericDataResponse.ErrorMessage, result.CreatedByUser);
                DeleteDeployModel(result.FMCorrelationId);
                DeleteDeployModel(result.CorrelationId);
            }
        }

        private string FMVisualizeInsertRequest(IngrainRequestQueue result)
        {
            IngrainRequestQueue ingrainRequest = new IngrainRequestQueue
            {
                _id = Guid.NewGuid().ToString(),
                CorrelationId = result.FMCorrelationId,
                FMCorrelationId = result.CorrelationId,
                RequestId = Guid.NewGuid().ToString(),
                ProcessId = CONSTANTS.Null,
                ModelName = result.ModelName,
                Status = CONSTANTS.Null,
                RequestStatus = CONSTANTS.New,
                Message = CONSTANTS.Null,
                RetryCount = 0,
                ProblemType = CONSTANTS.Null,
                UniId = result.UniId,
                InstaID = CONSTANTS.Null,
                Progress = CONSTANTS.Null,
                pageInfo = CONSTANTS.PublishModel,
                ParamArgs = CONSTANTS.Null,
                TemplateUseCaseID = null,
                Function = CONSTANTS.FMVisualize,
                CreatedByUser = result.CreatedByUser,
                CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                ModifiedByUser = CONSTANTS.Null,
                ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                LastProcessedOn = CONSTANTS.Null,
                ClientId = result.ClientId,
                DeliveryconstructId = result.DeliveryconstructId,
                UseCaseID = CONSTANTS.Null,
                EstimatedRunTime = CONSTANTS.Null
            };
            FMVisualization fMVisualization = new FMVisualization()
            {
                CorrelationId = result.FMCorrelationId,
                IsIncremental = false,
                Category = result.Category,
                ClientUID = result.ClientId,
                DCUID = result.DeliveryconstructId,
                UniqueId = result.UniId
            };
            InsertFMRequests(fMVisualization);
            InsertRequests(ingrainRequest);
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(TransformFMIngestedData), "TransformFMIngestedData request inserted END", string.IsNullOrEmpty(result.FMCorrelationId) ? default(Guid) : new Guid(result.FMCorrelationId), result.AppID, string.Empty, result.ClientID, result.DeliveryconstructId);
            Thread.Sleep(2000);
            return ingrainRequest.UniId;
        }
        private DataEngineering GetFmVisualizePredictionresult(IngrainRequestQueue result, string uniqId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(GetFmVisualizePredictionresult), "CheckFMTransformStatusStarted", string.IsNullOrEmpty(result.FMCorrelationId) ? default(Guid) : new Guid(result.FMCorrelationId), result.AppID, string.Empty, result.ClientID, result.DeliveryconstructId);
            bool callMethod = true;
            DataEngineering dataEngineering = new DataEngineering();
            try
            {
                while (callMethod)
                {
                    var useCaseData = CheckFMVisualizePrediction(result.FMCorrelationId, uniqId);
                    if (!string.IsNullOrEmpty(useCaseData))
                    {
                        JObject queueData = JObject.Parse(useCaseData);
                        dataEngineering.Status = (string)queueData[CONSTANTS.Status];
                        dataEngineering.Progress = (string)queueData[CONSTANTS.Progress];
                        dataEngineering.Message = (string)queueData[CONSTANTS.Message];
                        _GenericDataResponse.Status = dataEngineering.Status;
                        _GenericDataResponse.Message = dataEngineering.Message;
                        _GenericDataResponse.ErrorMessage = dataEngineering.Message;
                        if (dataEngineering.Status == CONSTANTS.C & dataEngineering.Progress == CONSTANTS.Hundred)
                        {
                            callMethod = false;
                            dataEngineering.IsComplete = true;
                            return dataEngineering;
                        }
                        else if (dataEngineering.Status == CONSTANTS.E)
                        {
                            callMethod = false;
                            dataEngineering.IsComplete = false;
                            return dataEngineering;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(GenericAutotrainService), nameof(GetFmVisualizePredictionresult), ex.Message, ex, result.AppID, string.Empty, result.ClientID, result.DeliveryconstructId);
            }
            return dataEngineering;
        }
        public void TerminateModelsTrainingRequests(string correlationId, List<IngrainRequestQueue> requests)
        {
            try
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(TerminateModelsTrainingRequests), "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                //updating the inprogress models to terminate state.            
                var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
                var update = Builders<BsonDocument>.Update.Set("RequestStatus", "Terminated").Set(CONSTANTS.Status, CONSTANTS.E).Set(CONSTANTS.ModifiedOn, DateTime.Now.ToString(CONSTANTS.DateHoursFormat)).Set(CONSTANTS.Message, "Terminated");
                var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId)
                    & Builders<BsonDocument>.Filter.Eq(CONSTANTS.Function, CONSTANTS.RecommendedAI)
                    & Builders<BsonDocument>.Filter.Ne(CONSTANTS.Status, CONSTANTS.C);
                var filter2 = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId)
                    & Builders<BsonDocument>.Filter.Eq("RequestStatus", "Terminated");
                var requestResult = collection.Find(filter2).ToList();
                if (requestResult.Count < 1)
                {
                    var result = collection.UpdateMany(filter, update);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(TerminateModelsTrainingRequests), "START MODIFIED COUNT :" + result.ModifiedCount + result.MatchedCount, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                    foreach (var item in requests)
                    {
                        if (item.Status != CONSTANTS.C)
                        {
                            IngrainRequestQueue ingrainRequest = new IngrainRequestQueue()
                            {
                                _id = Guid.NewGuid().ToString(),
                                CorrelationId = correlationId,
                                RequestId = Guid.NewGuid().ToString(),
                                ProcessId = null,
                                Status = null,
                                ModelName = item.ModelName,
                                RequestStatus = CONSTANTS.New,
                                RetryCount = 0,
                                ProblemType = null,
                                Message = null,
                                UniId = null,
                                PythonProcessID = item.PythonProcessID,
                                Progress = null,
                                pageInfo = CONSTANTS.TerminatePythyon,
                                ParamArgs = null,
                                Function = CONSTANTS.TerminatePythyon,
                                CreatedByUser = item.CreatedByUser,
                                CreatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                                ModifiedByUser = item.CreatedByUser,
                                ModifiedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                                LastProcessedOn = null,
                            };
                            var jsonColumns = Newtonsoft.Json.JsonConvert.SerializeObject(ingrainRequest);
                            var insertBsonColumns = BsonSerializer.Deserialize<BsonDocument>(jsonColumns);
                            collection.InsertOne(insertBsonColumns);
                            Thread.Sleep(1000);
                        }
                    }
                }
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(TerminateModelsTrainingRequests), "END ", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            }
            catch (Exception ex)
            {
                CustomAppsActivityLog log = new CustomAppsActivityLog();
                log.CorrelationId = correlationId;
                log.FeatureName = "PrivateModelTraining";
                log.StackTrace = ex.StackTrace;
                log.ErrorMessage = ex.Message;
                log.ErrorMethod = "TerminateModelsTrainingRequests";
                log.CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                log.CreatedBy = "SYSTEM";

                this.InsertCustomAppsActivityLog(log);
            }
        }
        private bool GetCustomDataIngrainRequestData(string oldCorrelationId, string NewCorrelationId, string appName, string clientId, string dcId)
        {
            bool status = false;
            try
            {
                var filterBuilder = Builders<BsonDocument>.Filter;
                var IngrainRequestCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
                var IngrainRequestFilter = filterBuilder.Eq("CorrelationId", oldCorrelationId) & filterBuilder.Eq("pageInfo", CONSTANTS.IngestData);
                var Projection = Builders<BsonDocument>.Projection.Include("ParamArgs").Exclude("_id");
                var requestdata = IngrainRequestCollection.Find(IngrainRequestFilter).Project<BsonDocument>(Projection).ToList();                
                if (requestdata.Count > 0)
                {
                    ParamArgs = requestdata[0]["ParamArgs"].ToString();
                    if (appName.ToUpper() == CONSTANTS.CustomDbQuery.ToUpper())
                    {
                        var fileParams = JsonConvert.DeserializeObject<CustomQueryParamArgs>(ParamArgs);
                        fileParams.CorrelationId = NewCorrelationId;
                        fileParams.ClientUID = clientId;
                        fileParams.DeliveryConstructUId = dcId;
                        _templateInfo.ParamArgs = fileParams.ToJson();
                        status = true;
                    }
                    else if (appName.ToUpper() == CONSTANTS.CustomDataApi.ToUpper())
                    {
                        var fileParams = JsonConvert.DeserializeObject<CustomSourceDTO>(ParamArgs);
                        fileParams.CorrelationId = NewCorrelationId;
                        fileParams.ClientUID = clientId;
                        fileParams.DeliveryConstructUId = dcId;

                        var CustomData = JsonConvert.DeserializeObject<ApiDTO>(_IsAESKeyVault ? CryptographyUtility.Decrypt(fileParams.CustomSource) : AesProvider.Decrypt(fileParams.CustomSource, _aesKey, _aesVector));
                        //JsonConvert.DeserializeObject<ApiDTO>(fileParams.CustomSource);
                        CustomData.BodyParam["StartDate"] = DateTime.Now.AddYears(-2).ToString(CONSTANTS.DateFormat);
                        CustomData.BodyParam["EndDate"] = DateTime.Now.ToString(CONSTANTS.DateFormat);

                        if (CustomData.Authentication.UseIngrainAzureCredentials)
                        {
                            //fetch Ingrain API URI from App setting
                            Uri apiUri = new Uri(appConfigSettings.IngrainAPIUrl);
                            string host = apiUri.GetLeftPart(UriPartial.Authority);

                            //fetch Custom Source URI from ParamArgs Data
                            apiUri = new Uri(CustomData.ApiUrl);
                            string apihost = apiUri.GetLeftPart(UriPartial.Authority);

                            if (!apihost.Equals(host))
                            {
                                CustomData.ApiUrl = CustomData.ApiUrl.Replace(apihost, host);
                            }

                            string token = getToken();
                            CustomData.Authentication.Token = token;
                        }

                        if (_IsAESKeyVault)
                            fileParams.CustomSource = CryptographyUtility.Encrypt(JsonConvert.SerializeObject(CustomData));
                        else
                            fileParams.CustomSource = AesProvider.Encrypt(JsonConvert.SerializeObject(CustomData), _aesKey, _aesVector);
                        _templateInfo.ParamArgs = fileParams.ToJson();
                        status = true;
                    }
                }
                else
                {
                    _GenericDataResponse.Message = "Public Template ParamArgs Details is Null";
                    _GenericDataResponse.Status = "E";
                    status = false;
                }
            }
            catch (Exception ex)
            {
                CustomAppsActivityLog log = new CustomAppsActivityLog()
                {
                    CorrelationId = NewCorrelationId,
                    ClientId = clientId,
                    DCID = dcId,
                    FeatureName = "PrivateModelTraining",
                    UseCaseId = _Mapping != null ? _Mapping.UsecaseID : null,
                    ApplicationID = _Mapping != null ? _Mapping.ApplicationID : null,
                    ApplicationName = appName,
                    StackTrace = ex.StackTrace,
                    ErrorMessage = ex.Message + " ,OldCorrelationId: " + oldCorrelationId,
                    ErrorMethod = "GetCustomDataIngrainRequestData",
                    CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                    CreatedBy = "SYSTEM",
                };

                this.InsertCustomAppsActivityLog(log);
            }

            return status;
        }
        public string VDSCallbackResponse(IngrainToVDSNotification VDSCallbackResponsedata, string ApplicationName, string baseAddress, string clientId, string DCId, string applicationId, string usecaseId, string requestId, string errorTrace, string userId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(VDSCallbackResponse), "START -VDSCallbackResponse Initiated- Data-" + JsonConvert.SerializeObject(VDSCallbackResponsedata), string.IsNullOrEmpty(VDSCallbackResponsedata.CorrelationId) ? default(Guid) : new Guid(VDSCallbackResponsedata.CorrelationId), applicationId, string.Empty, clientId, DCId);
            string token = CustomUrlToken(applicationId);

            _IngrainResponseData.CorrelationId = VDSCallbackResponsedata.CorrelationId;
            _IngrainResponseData.Message = VDSCallbackResponsedata.Message;
            _IngrainResponseData.Status = VDSCallbackResponsedata.Status;

            if (!string.IsNullOrEmpty(baseAddress))
            {
                string contentType = "application/json";
                var Request = JsonConvert.SerializeObject(VDSCallbackResponsedata);
                if (_authProvider.ToUpper() == "FORM" || _authProvider.ToUpper() == "AZUREAD")
                {
                    using (var Client = new HttpClient())
                    {
                        if (!string.IsNullOrEmpty(token))
                        {
                            Client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                            Client.DefaultRequestHeaders.Add("AppServiceUId", _AppServiceUId);
                            Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(contentType));
                            HttpContent httpContent = new StringContent(Request, Encoding.UTF8, "application/json");
                            HttpResponseMessage httpResponse = Client.PostAsync(baseAddress, httpContent).Result;
                            var statuscode = httpResponse.StatusCode;
                            CallBackErrorLog(_IngrainResponseData, ApplicationName, baseAddress, httpResponse, clientId, DCId, applicationId, usecaseId, requestId, errorTrace, userId);
                            if (httpResponse.IsSuccessStatusCode)
                            {
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(VDSCallbackResponse), " ApplicationName-- " + ApplicationName + " CALLBACKRESPONSE SUCCESS- TOKEN--" + token + "BASE_ADDERESS: " + baseAddress + "-HTTPCONTENT-" + httpResponse.Content.ReadAsStringAsync().Result, string.IsNullOrEmpty(VDSCallbackResponsedata.CorrelationId) ? default(Guid) : new Guid(VDSCallbackResponsedata.CorrelationId), applicationId, string.Empty, clientId, DCId);
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(VDSCallbackResponse), "END", string.IsNullOrEmpty(VDSCallbackResponsedata.CorrelationId) ? default(Guid) : new Guid(VDSCallbackResponsedata.CorrelationId), applicationId, string.Empty, clientId, DCId);
                                return "Success";
                            }
                            else
                            {
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(VDSCallbackResponse), " ApplicationName-- " + ApplicationName + "  CALLBACKRESPONSE - CALLBACK API ERROR:- HTTP RESPONSE-" + httpResponse.StatusCode + "-HTTPCONTENT-" + httpResponse.Content.ReadAsStringAsync().Result, string.IsNullOrEmpty(VDSCallbackResponsedata.CorrelationId) ? default(Guid) : new Guid(VDSCallbackResponsedata.CorrelationId), applicationId, string.Empty, clientId, DCId);
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(VDSCallbackResponse), "END", string.IsNullOrEmpty(VDSCallbackResponsedata.CorrelationId) ? default(Guid) : new Guid(VDSCallbackResponsedata.CorrelationId), applicationId, string.Empty, clientId, DCId);
                                return "Error";
                            }
                        }
                        else
                        {
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(VDSCallbackResponse), " ApplicationName-- " + ApplicationName + "  CALLBACKRESPONSE - Token is Null- " + (string.IsNullOrEmpty(VDSCallbackResponsedata.CorrelationId) ? default(Guid) : new Guid(VDSCallbackResponsedata.CorrelationId)), applicationId, string.Empty, clientId, DCId);
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(VDSCallbackResponse), "END", string.IsNullOrEmpty(VDSCallbackResponsedata.CorrelationId) ? default(Guid) : new Guid(VDSCallbackResponsedata.CorrelationId), applicationId, string.Empty, clientId, DCId);
                            return "Error";
                        }
                    }
                }
                else
                {
                    HttpClientHandler hnd = new HttpClientHandler();
                    hnd.UseDefaultCredentials = true;
                    using (var Client = new HttpClient(hnd))
                    {
                        Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(contentType));

                        HttpContent httpContent = new StringContent(Request, Encoding.UTF8, "application/json");
                        HttpResponseMessage httpResponse = Client.PostAsync(baseAddress, httpContent).Result;
                        var statuscode = httpResponse.StatusCode;
                        if (httpResponse.IsSuccessStatusCode)
                        {
                            return "Success";

                        }
                        else
                        {
                            return "Error";
                        }
                    }
                }
            }
            else
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(VDSCallbackResponse), " ApplicationName-- " + ApplicationName + " VDSCallbackResponse - BASE_ADDERESS NULL : " + baseAddress, string.IsNullOrEmpty(VDSCallbackResponsedata.CorrelationId) ? default(Guid) : new Guid(VDSCallbackResponsedata.CorrelationId), applicationId, string.Empty, clientId, DCId);
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(VDSCallbackResponse), "END", string.IsNullOrEmpty(VDSCallbackResponsedata.CorrelationId) ? default(Guid) : new Guid(VDSCallbackResponsedata.CorrelationId), applicationId, string.Empty, clientId, DCId);
                return "Success";
            }

        }
    }
}

