using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
using Accenture.MyWizard.Ingrain.DataAccess;
using Accenture.MyWizard.Ingrain.DataModels.Models;
using Accenture.MyWizard.Shared.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
using Accenture.MyWizard.Ingrain.BusinessDomain.Common;
using Accenture.MyWizard.Cryptography.EncryptionProviders;
using Newtonsoft.Json.Linq;

namespace Accenture.MyWizard.Ingrain.BusinessDomain.Services
{
    public class IAService : IIAService
    {
        #region Private Members
        private MongoClient _mongoClient;
        private IMongoDatabase _database;
        private readonly IOptions<IngrainAppSettings> appSettings;
        private DatabaseProvider databaseProvider;
        private IGenericSelfservice _genericSelfservice;
        private CallBackErrorLog auditTrailLog;
        private IFlaskAPI _iFlaskAPIService; 
        private IEncryptionDecryption _encryptionDecryption;
        #endregion

        #region Constructor
        public IAService(DatabaseProvider db, IOptions<IngrainAppSettings> settings, IServiceProvider serviceProvider)
        {
            databaseProvider = db;
            appSettings = settings;
            _mongoClient = databaseProvider.GetDatabaseConnection();
            var dataBaseName = MongoUrl.Create(appSettings.Value.connectionString).DatabaseName;
            _database = _mongoClient.GetDatabase(dataBaseName);
            _genericSelfservice = serviceProvider.GetService<IGenericSelfservice>();
            _iFlaskAPIService = serviceProvider.GetService<IFlaskAPI>();
            auditTrailLog = new CallBackErrorLog();
            _encryptionDecryption = serviceProvider.GetService<IEncryptionDecryption>();
        }
        #endregion

        #region Public Methods
        public GenericDataResponse InitiateTrainingRequest(TrainingRequestDetails trainingRequestDetails, string resourceId)
        {
            GenericDataResponse genericDataResponse = new GenericDataResponse();

            AppIntegrationsCredentials appIntegrationsCredentials = new AppIntegrationsCredentials()
            {
                grant_type = appSettings.Value.Grant_Type,
                client_id = appSettings.Value.clientId,
                client_secret = appSettings.Value.clientSecret,
                resource = string.IsNullOrEmpty(resourceId) ? appSettings.Value.resourceId : resourceId
            };

            AppIntegration appIntegrations = new AppIntegration()
            {
                ApplicationID = trainingRequestDetails.ApplicationId,
                Credentials = JsonConvert.SerializeObject(appIntegrationsCredentials)
            };

            if (_genericSelfservice.UpdateAppIntegration(appIntegrations) == CONSTANTS.Success)
            {
                genericDataResponse = StartModelTraining(trainingRequestDetails);
            }
            else
            {
                genericDataResponse.Message = CONSTANTS.NoRecordsFound + ", " + CONSTANTS.ApplicationID + ": " + trainingRequestDetails.ApplicationId;
            }

            return genericDataResponse;


        }


        public GenericDataResponse StartModelTraining(TrainingRequestDetails trainingRequestDetails)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(IAService)
                                                      , nameof(StartModelTraining)
                                                      , "IngrainGenericModelTrainingRequest - Started " + ", UserId: " + trainingRequestDetails.UserId
                                                      , string.IsNullOrEmpty(trainingRequestDetails.CorrelationId) ? default(Guid) : new Guid(trainingRequestDetails.CorrelationId)
                                                      , trainingRequestDetails.ApplicationId
                                                      , string.Empty
                                                      , trainingRequestDetails.ClientUId
                                                      , trainingRequestDetails.DeliveryConstructUId);

            DeployModelsDto deployedModel = GetUseCaseDetails(trainingRequestDetails.ClientUId, trainingRequestDetails.DeliveryConstructUId, trainingRequestDetails.UseCaseId, trainingRequestDetails.UserId);


            if (deployedModel != null)
            {
                //Asset usage
                auditTrailLog.ClientId = deployedModel.ClientUId;
                auditTrailLog.DCID = deployedModel.DeliveryConstructUID;
                auditTrailLog.ApplicationID = deployedModel.AppId;
                auditTrailLog.CorrelationId = deployedModel.CorrelationId;
                auditTrailLog.ProcessName = CONSTANTS.TrainingName;
                auditTrailLog.UsageType = CONSTANTS.AssetUsage;
                auditTrailLog.UseCaseId = trainingRequestDetails.UseCaseId;
                // auditTrailLog.FeatureName = CONSTANTS.IA;
                CommonUtility.AuditTrailLog(auditTrailLog, appSettings);
                trainingRequestDetails.CorrelationId = deployedModel.CorrelationId;
            }
            if (string.IsNullOrEmpty(trainingRequestDetails.CorrelationId))
            {
                return InsertTrainingRequest(trainingRequestDetails);
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
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(IAService)
                                                                 , nameof(StartModelTraining)
                                                                 , "ReTraining Request for correlationId - " + trainingRequestDetails.CorrelationId + ",Old Status is -" + ingrainRequestQueueResult.Status
                                                                 , string.IsNullOrEmpty(trainingRequestDetails.CorrelationId) ? default(Guid) : new Guid(trainingRequestDetails.CorrelationId)
                                                                  , trainingRequestDetails.ApplicationId
                                                                  , string.Empty
                                                                  , trainingRequestDetails.ClientUId
                                                                  , trainingRequestDetails.DeliveryConstructUId);
                        return InsertTrainingRequest(trainingRequestDetails);
                    }
                    else
                    {
                        return _genericSelfservice.IngrainGenericTrainingResponse(trainingRequestDetails.CorrelationId);
                    }
                }
                else
                {
                    return _genericSelfservice.IngrainGenericTrainingResponse(trainingRequestDetails.CorrelationId);
                }

            }


        }

        public GenericDataResponse InsertTrainingRequest(TrainingRequestDetails trainingRequestDetails)
        {
            bool isRetraining = !string.IsNullOrEmpty(trainingRequestDetails.CorrelationId);
            string correlationId = string.IsNullOrEmpty(trainingRequestDetails.CorrelationId) ? Guid.NewGuid().ToString() : trainingRequestDetails.CorrelationId;

            if (isRetraining)
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(IAService)
                                                         , nameof(InsertTrainingRequest)
                                                         , "ReTraining correlationId - " + correlationId
                                                          , string.IsNullOrEmpty(trainingRequestDetails.CorrelationId) ? default(Guid) : new Guid(trainingRequestDetails.CorrelationId)
                                                                  , trainingRequestDetails.ApplicationId, string.Empty
                                                                  , trainingRequestDetails.ClientUId
                                                                  , trainingRequestDetails.DeliveryConstructUId);

                _genericSelfservice.IngrainGenericDeleteOldRecordsOnRetraining(trainingRequestDetails, correlationId);
            }
            GenericDataResponse genericDataResponse = new GenericDataResponse();
            var templateDetails = GetTemplateDetails(trainingRequestDetails.UseCaseId);
            if (templateDetails == null)
            {
                genericDataResponse.Message = CONSTANTS.NoRecordsFound + " for UseCaseId: " + trainingRequestDetails.UseCaseId;
                genericDataResponse.Status = CONSTANTS.E;
                genericDataResponse.CorrelationId = correlationId;
                return genericDataResponse;
            }
            //write condition based on sourceName


            var publicTemplateCollection = _database.GetCollection<PublicTemplateMapping>(CONSTANTS.PublicTemplateMapping);
            var publicTemplateFilterBuilder = Builders<PublicTemplateMapping>.Filter;
            var publicTemplateFilterQueue = publicTemplateFilterBuilder.Where(x => x.ApplicationID == trainingRequestDetails.ApplicationId)
                                            & publicTemplateFilterBuilder.Where(x => x.UsecaseID == trainingRequestDetails.UseCaseId);
            var Projection = Builders<PublicTemplateMapping>.Projection.Exclude("_id");
            var publicTemplateQueueResult = publicTemplateCollection.Find(publicTemplateFilterQueue).Project<PublicTemplateMapping>(Projection).FirstOrDefault();
            if (publicTemplateQueueResult == null)
            {
                genericDataResponse.Message = CONSTANTS.NoRecordsFound + " in PublicTemplateMapping.";
                genericDataResponse.Status = CONSTANTS.E;
                genericDataResponse.CorrelationId = correlationId;
                return genericDataResponse;
            }
            string paramAgs = "{}";
            if (templateDetails.SourceName == "pad")
            {
                var collection = _database.GetCollection<IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
                var builder = Builders<IngrainRequestQueue>.Filter;
                var filter = builder.Eq(CONSTANTS.pageInfo, CONSTANTS.IngestData) & builder.Eq(CONSTANTS.CorrelationId, trainingRequestDetails.UseCaseId);
                var result = collection.Find(filter).ToList();
                if (result.Count > 0)
                {
                    var fileUpload = JsonConvert.DeserializeObject<FileUpload>(result[0].ParamArgs);
                    fileUpload.Customdetails = CONSTANTS.Null;
                    fileUpload.CorrelationId = correlationId;
                    fileUpload.ClientUID = trainingRequestDetails.ClientUId;
                    fileUpload.DeliveryConstructUId = trainingRequestDetails.DeliveryConstructUId;
                    //start and enddate also modify
                    var pad = JsonConvert.DeserializeObject<JObject>(fileUpload.pad);
                    pad["startDate"] = DateTime.Today.AddYears(-2).ToString("MM/dd/yyyy");
                    pad["endDate"] = DateTime.Today.ToString("MM/dd/yyyy");
                    fileUpload.pad = pad.ToString().Replace("\r\n", "");
                    paramAgs = JsonConvert.SerializeObject(fileUpload);
                }
                   // ingrainRequest.ParamArgs = fileUpload.ToJson();
                    //var fileUpload = JsonConvert.DeserializeObject<FileUpload>(bsonElements.ParamArgs);
            }
            else
            {
                ParamArgsWithCustomFlag paramArgsWithCustomFlag = CreateParamArgs(trainingRequestDetails, correlationId, publicTemplateQueueResult.SourceFlagName);
                paramAgs = JsonConvert.SerializeObject(paramArgsWithCustomFlag);
            }
            bool DBEncryptionRequired = CommonUtility.EncryptDB(trainingRequestDetails.UseCaseId, appSettings);
            if (DBEncryptionRequired)
            {
                if (!string.IsNullOrEmpty(Convert.ToString(trainingRequestDetails.UserId)))
                {
                    trainingRequestDetails.UserId = _encryptionDecryption.Encrypt(trainingRequestDetails.UserId);
                }
            }

            IngrainRequestQueue ingrainRequest = new IngrainRequestQueue
            {
                _id = Guid.NewGuid().ToString(),
                CorrelationId = correlationId,
                RequestId = Guid.NewGuid().ToString(),
                ProcessId = CONSTANTS.Null,
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
                ParamArgs = paramAgs,//JsonConvert.SerializeObject(paramArgsWithCustomFlag),
                TemplateUseCaseID = publicTemplateQueueResult.UsecaseID,
                Function = CONSTANTS.AutoTrain,
                CreatedByUser = trainingRequestDetails.UserId,
                CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                ModifiedByUser = CONSTANTS.Null,
                ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                LastProcessedOn = CONSTANTS.Null,
                AppID = publicTemplateQueueResult.ApplicationID,
                ClientId = trainingRequestDetails.ClientUId,
                DeliveryconstructId = trainingRequestDetails.DeliveryConstructUId,
                UseCaseID = trainingRequestDetails.UseCaseId,
                //DataSource = CONSTANTS.Null,
                EstimatedRunTime = CONSTANTS.Null
            };
            bool isdata = _genericSelfservice.CheckRequiredDetails(publicTemplateQueueResult.ApplicationName, publicTemplateQueueResult.UsecaseID);
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


            LOGGING.LogManager.Logger.LogProcessInfo(typeof(IAService)
                                                     , nameof(InsertTrainingRequest)
                                                     , "Auto Train function initiated TemplateID:" + publicTemplateQueueResult.UsecaseID + "  NewCorrelationID :" + correlationId + " AppName" + publicTemplateQueueResult.ApplicationName + " UsecaseName " + publicTemplateQueueResult.UsecaseName
                                                     , string.IsNullOrEmpty(ingrainRequest.CorrelationId) ? default(Guid) : new Guid(ingrainRequest.CorrelationId)
                                                     , ingrainRequest.AppID, string.Empty
                                                     , ingrainRequest.ClientID
                                                     , ingrainRequest.DeliveryconstructId);

            _genericSelfservice.InsertRequests(ingrainRequest);
            genericDataResponse.Message = CONSTANTS.TrainingResponse;
            genericDataResponse.Status = CONSTANTS.I;
            genericDataResponse.CorrelationId = correlationId;
            return genericDataResponse;
        }

        private ParamArgsWithCustomFlag CreateParamArgs(TrainingRequestDetails trainingRequestDetails, string correlationId, string sourceFlagName)
        {

            CustomPayloadDetails customArgsDetails = GetCustomArgDetails(trainingRequestDetails.ApplicationId, sourceFlagName, trainingRequestDetails.UseCaseId);


            PublicTemplateMapping templateMapping = new PublicTemplateMapping()
            {
                ApplicationID = trainingRequestDetails.ApplicationId,
                SourceName = trainingRequestDetails.DataSource,
                UsecaseID = trainingRequestDetails.UseCaseId,
                SourceURL = customArgsDetails.AppUrl,
                InputParameters = JsonConvert.SerializeObject(customArgsDetails.InputParameters)
            };

            _genericSelfservice.UpdatePublicTemplateMappingWithoutEncryption(templateMapping);

            Parent parent = new Parent()
            {
                Type = CONSTANTS.Null,
                Name = CONSTANTS.Null
            };

            Fileupload fileupload = new Fileupload()
            {
                fileList = CONSTANTS.Null
            };

            ParamArgsWithCustomFlag paramArgsWithCustomFlag = new ParamArgsWithCustomFlag()
            {

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
                Customdetails = customArgsDetails

            };

            return paramArgsWithCustomFlag;
        }



        private CustomPayloadDetails GetCustomArgDetails(string applicationId, string sourceFlagName, string usecaseId)
        {
            Uri apiUri = new Uri(appSettings.Value.myWizardAPIUrl);
            string host = apiUri.GetLeftPart(UriPartial.Authority);

            CustomPayloadDetails customArgsDetails = null;


            if (sourceFlagName == "IA_Schedule")
            {
                customArgsDetails = new CustomPayloadDetails()
                {

                    CustomFlags = new { FlagName = "IA_Schedule" },
                    AppId = applicationId,
                    UsecaseID = usecaseId,
                    HttpMethod = "POST",
                    AppUrl = host + "/bi/ChangeImpactScheduleCalculationData",
                    InputParameters = new
                    {
                        FromDate = DateTime.UtcNow.AddYears(-2).ToString("yyyy-MM-dd"),
                        ToDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                        ReleaseUID = new List<string>(),
                        Measure_metrics = new List<string>() { "SPI", "EV", "PV", "EDV", "AD121", "ReleaseUId", "processedondate", "ModifiedOn", "complexityuid", "uniqueid" }
                    },
                    DateColumn = CONSTANTS.Null
                };
            }
            if (sourceFlagName == "IA_Defect")
            {
                customArgsDetails = new CustomPayloadDetails()
                {
                    CustomFlags = new { FlagName = "IA_Defect" },
                    AppId = applicationId,
                    UsecaseID = usecaseId,
                    HttpMethod = "POST",
                    AppUrl = host + "/bi/ChangeImpactDefectCalculationData",
                    InputParameters = new
                    {
                        FromDate = DateTime.UtcNow.AddYears(-2).ToString("yyyy-MM-dd"),
                        ToDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                        ReleaseUID = new List<string>(),
                        Measure_metrics = new List<string>() { "DR", "EV", "PV", "AD033", "AD149", "AD058", "ReleaseUId", "processedondate", "complexityuid", "modifiedon", "clientuid", "uniqueid" }
                    },
                    DateColumn = CONSTANTS.Null
                };
            }

            return customArgsDetails;
        }




        public PredictionResultDTO InitiatePrediction(IAPredictionRequest iAPredictionRequest)
        {

            PredictionResultDTO predictionResultDTO = new PredictionResultDTO();
            predictionResultDTO.CorrelationId = iAPredictionRequest.CorrelationId;

            var deployModel = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
            var filter = Builders<DeployModelsDto>.Filter.Eq(CONSTANTS.CorrelationId, iAPredictionRequest.CorrelationId)
                          & Builders<DeployModelsDto>.Filter.Eq(CONSTANTS.Status, "Deployed");
            var projection = Builders<DeployModelsDto>.Projection.Exclude("_id");
            var modelCollection = deployModel.Find(filter).Project<DeployModelsDto>(projection).ToList();
            if (modelCollection.Count > 0)
            {
                IngrainRequestQueue requestQueue = _genericSelfservice.GetFileRequestStatus(iAPredictionRequest.CorrelationId, CONSTANTS.IngestData);

                if (requestQueue != null)
                {
                    dynamic paramArgs = null;
                    string requestId = Guid.NewGuid().ToString();   
                    string uniId = Guid.NewGuid().ToString();


                    paramArgs = JsonConvert.DeserializeObject<object>(requestQueue.ParamArgs);


                    if (!object.ReferenceEquals(null, paramArgs.Customdetails))
                    {
                        if (Convert.ToString(paramArgs.Customdetails) != CONSTANTS.Null && !string.IsNullOrEmpty(Convert.ToString(paramArgs.Customdetails)))
                        {
                            paramArgs = JsonConvert.DeserializeObject<ParamArgsWithCustomFlag>(requestQueue.ParamArgs);
                            paramArgs.Customdetails.InputParameters.FromDate = DateTime.UtcNow.AddDays(-3).ToString("yyyy-MM-dd");
                            paramArgs.Customdetails.InputParameters.ToDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
                            paramArgs.Customdetails.InputParameters.ReleaseUID = new Newtonsoft.Json.Linq.JArray() { iAPredictionRequest.ReleaseUId };
                            paramArgs.Customdetails.DateColumn = CONSTANTS.Null;
                            paramArgs.Customdetails.UsecaseID = modelCollection[0].TemplateUsecaseId;
                            if (paramArgs.Customdetails.UsecaseID.Equals(CONSTANTS.SPI))
                            {
                                paramArgs.Customdetails.CustomFlags = new { FlagName = "IA_Schedule" };
                            }
                            else if (paramArgs.Customdetails.UsecaseID.Equals(CONSTANTS.DefectRate))
                            {
                                paramArgs.Customdetails.CustomFlags = new { FlagName = "IA_Defect" };
                            }
                        }
                        paramArgs.Customdetails.CustomFlags = new { FlagName = "IA_Defect" };
                    }

                    paramArgs.Flag = "Incremental";
                    //paramArgs.Customdetails.InputParameters.ReleaseUID = new List<string>() { iAPredictionRequest.ReleaseUId };
                    requestQueue.ParamArgs = JsonConvert.SerializeObject(paramArgs);
                    requestQueue._id = Guid.NewGuid().ToString();
                    requestQueue.RequestId = requestId;
                    requestQueue.UniId = uniId;
                    requestQueue.RequestStatus = CONSTANTS.New;
                    requestQueue.Status = CONSTANTS.Null;
                    requestQueue.Progress = CONSTANTS.Null;
                    requestQueue.Message = CONSTANTS.Null;
                    requestQueue.RetryCount = 0;
                    requestQueue.CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                    requestQueue.ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                    requestQueue.IsForAutoTrain = false; //Should be false for Prediction avoiding single flow (arch change)
                    _genericSelfservice.InsertRequests(requestQueue);
                    bool flag = true;
                    predictionResultDTO.UniqueId = uniId;
                    while (flag)
                    {
                        IngrainRequestQueue request = _genericSelfservice.GetFileRequestStatusByRequestId(iAPredictionRequest.CorrelationId, CONSTANTS.IngestData, requestId);
                        if (request != null)
                        {
                            if (request.Status == CONSTANTS.C && request.Progress == CONSTANTS.Hundred)
                            {
                                flag = false;
                                IngrainRequestQueue ingrainRequest = new IngrainRequestQueue
                                {
                                    _id = Guid.NewGuid().ToString(),
                                    CorrelationId = iAPredictionRequest.CorrelationId,
                                    RequestId = Guid.NewGuid().ToString(),
                                    ProcessId = CONSTANTS.Null,
                                    Status = CONSTANTS.Null,
                                    ModelName = CONSTANTS.Null,
                                    RequestStatus = appSettings.Value.IsFlaskCall ? CONSTANTS.Occupied : CONSTANTS.New,
                                    RetryCount = 0,
                                    ProblemType = CONSTANTS.Null,
                                    Message = CONSTANTS.Null,
                                    UniId = uniId,
                                    Progress = CONSTANTS.Null,
                                    pageInfo = CONSTANTS.PublishModel, // pageInfo 
                                    ParamArgs = CONSTANTS.CurlyBraces,
                                    Function = CONSTANTS.PublishModel,
                                    CreatedByUser = modelCollection[0].CreatedByUser,
                                    TemplateUseCaseID = modelCollection[0].UseCaseID,
                                    CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                                    ModifiedByUser = modelCollection[0].ModifiedByUser,
                                    ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                                    LastProcessedOn = CONSTANTS.Null,
                                    AppID = modelCollection[0].AppId,
                                    IsForAPI = appSettings.Value.IsFlaskCall ? true : false
                                };
                                _genericSelfservice.InsertRequests(ingrainRequest);
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(IAService), nameof(InitiatePrediction), "IA before Flask CallPython Triggered",  string.Empty, "Flag: " + appSettings.Value.IsFlaskCall, string.Empty, string.Empty);
                                if (appSettings.Value.IsFlaskCall)
                                {
                                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(IAService), nameof(InitiatePrediction), "IA Inside Flask CallPython Triggered", _iFlaskAPIService != null ? "true" : "false", string.Empty, string.Empty, string.Empty);
                                    _iFlaskAPIService.CallPython(ingrainRequest.CorrelationId, ingrainRequest.UniId, ingrainRequest.pageInfo);
                                }

                            }
                            else if (request.Status == CONSTANTS.E)
                            {
                                predictionResultDTO.ErrorMessage = request.Message;
                                predictionResultDTO.Message = CONSTANTS.PhythonError;
                                predictionResultDTO.Status = CONSTANTS.E;
                                return predictionResultDTO;
                            }
                            else if (request.Status == CONSTANTS.I)
                            {
                                predictionResultDTO.ErrorMessage = request.Message;
                                predictionResultDTO.Message = CONSTANTS.PhythonInfo;
                                predictionResultDTO.Status = request.Status;
                                return predictionResultDTO;
                            }
                            else
                            {
                                flag = true;
                                Thread.Sleep(1000);
                            }
                            predictionResultDTO.Status = request.Status;
                        }
                        else
                        {
                            flag = true;
                            Thread.Sleep(2000);
                        }
                    }


                    bool predFlag = true;

                    while (predFlag)
                    {
                        predictionResultDTO = _genericSelfservice.IngrainGenericPredictionResponse(iAPredictionRequest.CorrelationId, uniId);
                        if (predictionResultDTO.Status == "C" || predictionResultDTO.Status == "E")
                        {
                            predFlag = false;
                        }
                        Thread.Sleep(1000);
                    }

                }


            }
            else
            {
                predictionResultDTO.Status = "E";
                predictionResultDTO.Message = "Model not trained";
            }
            return predictionResultDTO;

        }

        public DeployModelsDto GetTemplateDetails(string usecaseId)
        {
            var modelCollection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
            var filter = Builders<DeployModelsDto>.Filter.Where(x => x.Status == CONSTANTS.Deployed)
                        //& Builders<DeployModelsDto>.Filter.Where(x => x.DeliveryConstructUID == deliveryConstructUId)
                        & Builders<DeployModelsDto>.Filter.Where(x => x.CorrelationId == usecaseId);
            //  & Builders<DeployModelsDto>.Filter.Where(x => (x.CreatedByUser == userId || x.CreatedByUser == encryptedUser));
            var result = modelCollection.Find(filter).FirstOrDefault();
            return result;
        }



        public DeployModelsDto GetUseCaseDetails(string clientUId, string deliveryConstructUId, string usecaseId, string userId)
        {
            string encryptedUser = userId;
            if (!string.IsNullOrEmpty(Convert.ToString(encryptedUser)))
            {
                encryptedUser = _encryptionDecryption.Encrypt(Convert.ToString(encryptedUser));
            }
            bool DBEncryptionRequired = CommonUtility.EncryptDB(usecaseId, appSettings);
            var modelCollection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
            var filter = Builders<DeployModelsDto>.Filter.Where(x => x.ClientUId == clientUId)
                         & Builders<DeployModelsDto>.Filter.Where(x => x.DeliveryConstructUID == deliveryConstructUId)
                         & Builders<DeployModelsDto>.Filter.Where(x => x.TemplateUsecaseId == usecaseId)
                         & Builders<DeployModelsDto>.Filter.Where(x => (x.CreatedByUser == userId || x.CreatedByUser == encryptedUser));
            var result = modelCollection.Find(filter).FirstOrDefault();
            return result;
        }


        public IAUseCasePredictionResponse GetUseCasePrediction(IAUseCasePredictionRequest iAUseCasePredictionRequest)
        {
            IAUseCasePredictionResponse iAUseCasePredictionResponse = new IAUseCasePredictionResponse();
            iAUseCasePredictionResponse.ClientUId = iAUseCasePredictionRequest.ClientUId;
            iAUseCasePredictionResponse.DeliveryConstructUId = iAUseCasePredictionRequest.DeliveryConstructUId;
            iAUseCasePredictionResponse.UseCaseId = iAUseCasePredictionRequest.UseCaseId;
            iAUseCasePredictionResponse.ReleaseUId = iAUseCasePredictionRequest.ReleaseUId;
            iAUseCasePredictionResponse.UserId = iAUseCasePredictionRequest.UserId;
            try
            {
                if (iAUseCasePredictionRequest.ReleaseUId.Count > 0)
                {
                    DeployModelsDto deployModelsDto = GetUseCaseDetails(iAUseCasePredictionRequest.ClientUId, iAUseCasePredictionRequest.DeliveryConstructUId, iAUseCasePredictionRequest.UseCaseId, iAUseCasePredictionRequest.UserId);
                    if (deployModelsDto != null)
                    {
                        if (deployModelsDto.Status == "Deployed")
                        {
                            //Asset Usage
                            auditTrailLog.ClientId = deployModelsDto.ClientUId;
                            auditTrailLog.DCID = deployModelsDto.DeliveryConstructUID;
                            auditTrailLog.ApplicationID = deployModelsDto.AppId;
                            auditTrailLog.CorrelationId = deployModelsDto.CorrelationId;
                            auditTrailLog.ProcessName = CONSTANTS.PredictionName;
                            auditTrailLog.UsageType = CONSTANTS.AssetUsage;
                            auditTrailLog.UseCaseId = iAUseCasePredictionRequest.UseCaseId;
                            // auditTrailLog.FeatureName = CONSTANTS.IA;
                            CommonUtility.AuditTrailLog(auditTrailLog, appSettings);
                            IAPredictionRequest iAPredictionRequest = new IAPredictionRequest()
                            {
                                CorrelationId = deployModelsDto.CorrelationId,
                                ReleaseUId = iAUseCasePredictionRequest.ReleaseUId
                            };
                            PredictionResultDTO predictionResultDTO = InitiatePrediction(iAPredictionRequest);

                            iAUseCasePredictionResponse.Status = predictionResultDTO.Status;
                            iAUseCasePredictionResponse.PredictedData = predictionResultDTO.PredictedData;
                            iAUseCasePredictionResponse.Progress = predictionResultDTO.Progress;
                            iAUseCasePredictionResponse.Message = predictionResultDTO.Message;
                            iAUseCasePredictionResponse.UniqueId = predictionResultDTO.UniqueId;
                            iAUseCasePredictionResponse.CorrelationId = predictionResultDTO.CorrelationId;
                        }
                        else
                        {
                            iAUseCasePredictionResponse.Status = "E";
                            iAUseCasePredictionResponse.PredictedData = null;
                            iAUseCasePredictionResponse.Progress = "0";
                            iAUseCasePredictionResponse.Message = "Model not trained";
                        }

                    }
                    else
                    {
                        iAUseCasePredictionResponse.Status = "E";
                        iAUseCasePredictionResponse.PredictedData = null;
                        iAUseCasePredictionResponse.Progress = "0";
                        iAUseCasePredictionResponse.Message = "Model not trained";
                    }

                    return iAUseCasePredictionResponse;
                }
                else
                {
                    
                    iAUseCasePredictionResponse.Status = "E";
                    iAUseCasePredictionResponse.Message = "ReleaseUID cannot be null";
                    return iAUseCasePredictionResponse;
                }
            }
            catch (Exception ex)
            {
                iAUseCasePredictionResponse.Status = "E";
                iAUseCasePredictionResponse.Message = ex.Message;
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(IAService)
                                                              , nameof(IAUseCasePredictionRequest)
                                                              , ex.Message + $"   StackTrace = {ex.StackTrace}", ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return iAUseCasePredictionResponse;

            }
        }

            #endregion
        }
}
