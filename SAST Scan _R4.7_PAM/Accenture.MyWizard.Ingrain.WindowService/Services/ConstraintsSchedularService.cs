using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text;
using Accenture.MyWizard.Cryptography.EncryptionProviders;
using PhoenixPayloads = Accenture.MyWizard.Ingrain.WindowService.Models.PhoenixPayloads;
using DATAMODELS = Accenture.MyWizard.Ingrain.DataModels.Models;
using WINSERVICEMODELS = Accenture.MyWizard.Ingrain.WindowService.Models;
using SSAIWINSERVICEMODELS = Accenture.MyWizard.SelfServiceAI.WindowService.Models;
using DATAACCESS = Accenture.MyWizard.Ingrain.WindowService;
using SSAIDATAACCESS = Accenture.MyWizard.SelfServiceAI.WindowService.Service;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Net;
using Newtonsoft.Json;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
using System.Data;
using MongoDB.Bson;
using Accenture.MyWizard.Ingrain.DataModels.AICore;
using Accenture.MyWizard.Ingrain.DataModels.AICore.UseCase;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Threading;
using MongoDB.Bson.Serialization;
using Accenture.MyWizard.Ingrain.WindowService.Services.ConstraintsHelperMethods;
using CryptographyHelper = Accenture.MyWizard.Cryptography;
using Accenture.MyWizard.Ingrain.DataModels.Models;
using AIDataModels = Accenture.MyWizard.Ingrain.DataModels.AICore;
using System.Net.Http.Headers;


namespace Accenture.MyWizard.Ingrain.WindowService.Services
{
    public class ConstraintsSchedularService
    {
        private readonly WINSERVICEMODELS.AppSettings appSettings = null;
        private IMongoDatabase _database;
        private DATAACCESS.DatabaseProvider databaseProvider;
        private readonly string SchedulerUserName = "SYSTEM";
        private bool bClosedSprintIteration = false;
        private bool bIsAmbulanceLane = false;
        private bool bClosedUserStory = false;
        private bool bIsIAApp = false;
        private string AppName = CONSTANTS.INGRAIN;
        private string ProvisionedAppServiceUID = "36b5d37e-f4b7-4395-beb7-85c043202091";
        private readonly string PredictionSchedulerUserName = "SYSTEM";
        DBConnection _DBConnection = null;
        private DBLoggerService _DatabaseLoggerService = null;
        private TokenService _TokenService = null;
        private HttpMethodService _HttpMethodService = null;
        private PhoenixhadoopConnection _PhoenixhadoopConnection = null;

        private readonly string RecommendationTypeUId_DefectRate = "00905010-0007-0000-0000-000000000000";
        private readonly string RecommendationTypeUId_SPI = "00905010-0006-0000-0000-000000000000";
        private readonly string RecommendationTypeUId_SimilarCR = "00905010-0001-0000-0000-000000000000";
        private readonly string RecommendationTypeUId_SimilarUserStory = "00905010-0004-0000-0000-000000000000";
        private readonly string RecommendationTypeUId_SimilarRequirement = "00905010-0005-0000-0000-000000000000";
        private readonly string DraftStateUId = "00904010-0001-0000-0000-000000000000";

        SSAIWINSERVICEMODELS.InstaRetrain instaRetrain;
        private AIServicesRetrain AIServices;

        private SSAIDATAACCESS.InstaAutoRetrainService _instaAutoRetrainService;
        WINSERVICEMODELS.LogAutoTrainedFeatures autoTrainedFeatures = null;
        WINSERVICEMODELS.LogAIServiceAutoTrain logAIServiceAutoTrain = null;


        public string IA_DefectRateUseCaseId { get; set; }
        public string IA_SPIUseCaseId { get; set; }
        public string IA_CRUseCaseId { get; set; }
        public string IA_UserStoryUseCaseId { get; set; }
        public string IA_RequirementUseCaseId { get; set; }
        public string IterationEntityUId { get; set; }
        public string CREntityUId { get; set; }

        public List<string> SSAIUseCaseIds { get; set; }
        public List<string> AIUseCaseIds { get; set; }
        private string _aesKey;
        private string _aesVector;
        private readonly string IngrainAppId = "36b5d37e-f4b7-4395-beb7-85c043202091";


        static CryptographyHelper.Utility.CryptographyUtility CryptographyUtility = null;
        public ConstraintsSchedularService()
        {
            appSettings = AppSettingsJson.GetAppSettings().GetSection("AppSettings").Get<WINSERVICEMODELS.AppSettings>();
            databaseProvider = new DATAACCESS.DatabaseProvider();
            var dataBaseName = MongoUrl.Create(appSettings.connectionString).DatabaseName;
            MongoClient mongoClient = databaseProvider.GetDatabaseConnection();
            _database = mongoClient.GetDatabase(dataBaseName);
            _instaAutoRetrainService = new SSAIDATAACCESS.InstaAutoRetrainService(databaseProvider);
            autoTrainedFeatures = new WINSERVICEMODELS.LogAutoTrainedFeatures();
            AIServices = new AIServicesRetrain(databaseProvider);
            logAIServiceAutoTrain = new WINSERVICEMODELS.LogAIServiceAutoTrain();


            SSAIUseCaseIds = appSettings.IA_SSAIUseCaseIds;
            AIUseCaseIds = appSettings.IA_AIUseCaseIds;
            IterationEntityUId = appSettings.IterationEntityUId;
            CREntityUId = appSettings.CREntityUId;
            _aesKey = AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("aesKey").Value;
            _aesVector = AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("aesVector").Value;
            IA_DefectRateUseCaseId = string.IsNullOrEmpty(SSAIUseCaseIds.ElementAtOrDefault(0)) ? null : SSAIUseCaseIds[0];
            IA_SPIUseCaseId = string.IsNullOrEmpty(SSAIUseCaseIds.ElementAtOrDefault(1)) ? null : SSAIUseCaseIds[1];
            IA_CRUseCaseId = string.IsNullOrEmpty(AIUseCaseIds.ElementAtOrDefault(0)) ? null : AIUseCaseIds[0];
            IA_UserStoryUseCaseId = string.IsNullOrEmpty(AIUseCaseIds.ElementAtOrDefault(1)) ? null : AIUseCaseIds[1];
            IA_RequirementUseCaseId = string.IsNullOrEmpty(AIUseCaseIds.ElementAtOrDefault(2)) ? null : AIUseCaseIds[2];


            _DBConnection = new DBConnection();
            _DatabaseLoggerService = new DBLoggerService();
            _TokenService = new TokenService();
            _HttpMethodService = new HttpMethodService();
            _PhoenixhadoopConnection = new PhoenixhadoopConnection();
            CryptographyUtility = new CryptographyHelper.Utility.CryptographyUtility();
        }

        #region SSAI Training Methods
        public void StartModelTraining(DATAMODELS.DeployModelsDto item)
        {
            bool bAppProvisionedClientDC = false, bTrainingAtTeamLevel = false;
            WINSERVICEMODELS.AppDeliveryConstructs appDeliveryConstructs = null;

            try
            {
                _DatabaseLoggerService.UpdateOfflineRunTime(item);

                //Get List of Training Constrainst selected for ModelTemplate 
                var result = _DBConnection.GetCustomConstraints(item.CorrelationId, CONSTANTS.TrainingConstraint, CONSTANTS.SSAICustomContraints);
                List<string> oCustomConstrainsList = null;
                if (result != null)
                {
                    oCustomConstrainsList = JsonConvert.DeserializeObject<List<string>>(result.ToJson());
                }
                if (oCustomConstrainsList == null)
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(ConstraintsSchedularService), nameof(StartModelTraining), "SSAI Template : '" + item.ModelName + "' - No Contraints Found", "Ingrain Window Service", "", "", "", CONSTANTS.CustomConstraintsLog);
                    return;
                }

                DATAMODELS.AppIntegration oAppIntegration = _DBConnection.FetchAppServiceUID(item.AppId);

                if (oCustomConstrainsList != null)
                {
                    foreach (string ConstraintCode in oCustomConstrainsList)
                    {
                        switch (ConstraintCode)
                        {
                            case "AppProvisionedClientDC":
                                bAppProvisionedClientDC = true;
                                //Fetch the AppServiceUID linked to the Deployed Model's AppId 

                                if (oAppIntegration != null)
                                {
                                    try
                                    {
                                        ProvisionedAppServiceUID = oAppIntegration.ProvisionedAppServiceUID;
                                        if (!string.IsNullOrEmpty(ProvisionedAppServiceUID))
                                        {
                                            //Fetch List of provisioned Client and Delivery Constructs for AppServiceUId - app
                                            appDeliveryConstructs = _PhoenixhadoopConnection.FetchClientsDeliveryConstructs(item.ClientUId, item.DeliveryConstructUID, ProvisionedAppServiceUID);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        LOGGING.LogManager.Logger.LogErrorMessage(typeof(ConstraintsSchedularService), nameof(StartModelTraining), "SSAI Template : '" + item.ModelName + "' - Training terminated ; Exception : " + ex.Message + "- STACKTRACE - " + ex.StackTrace + ex.InnerException, ex, "Ingrain Window Service", string.Empty, string.Empty, string.Empty, CONSTANTS.CustomConstraintsLog);
                                        return;
                                    }
                                }
                                else
                                {
                                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(ConstraintsSchedularService), nameof(StartModelTraining), "SSAI Template : '" + item.ModelName + "' - AppIntegration is null - Training Initiated", "Ingrain Window Service", "", "", "", CONSTANTS.CustomConstraintsLog);
                                }
                                break;

                            //case "SingleModel":
                            //    bMultipleModel = false;
                            //    break;

                            //case "MultipleModel":
                            //    bMultipleModel = true;
                            //    break;

                            case "DCLevelTraining":
                                bTrainingAtTeamLevel = false;
                                break;

                            case "TeamAreaLevel":
                                bTrainingAtTeamLevel = true;
                                break;

                            case "IsAmbulance":
                                bIsAmbulanceLane = true;
                                break;

                            case "ClosedSprintnIteration":
                                bClosedSprintIteration = true;
                                break;

                            case "ClosedUserStory":
                                bClosedUserStory = true;
                                break;

                            default:
                                break;

                        }
                    }
                }

                if (!bAppProvisionedClientDC)
                {
                    try
                    {
                        ProvisionedAppServiceUID = IngrainAppId;
                        //It means the constraint was not selected , so We will first check whether ProvisionedAppServiceUID exists for the APP
                        //aginst which Model Template is deployed
                        if (oAppIntegration != null)
                        {
                            ProvisionedAppServiceUID = (!string.IsNullOrEmpty(oAppIntegration.ProvisionedAppServiceUID)) ? oAppIntegration.ProvisionedAppServiceUID : IngrainAppId;
                        }

                        appDeliveryConstructs = _PhoenixhadoopConnection.FetchClientsDeliveryConstructs(item.ClientUId, item.DeliveryConstructUID, ProvisionedAppServiceUID);
                    }
                    catch (Exception ex)
                    {
                        LOGGING.LogManager.Logger.LogErrorMessage(typeof(ConstraintsSchedularService), nameof(StartModelTraining), "SSAI Template : '" + item.ModelName + "' - Training terminated ; Exception : " + ex.Message + "- STACKTRACE - " + ex.StackTrace + ex.InnerException, ex, "Ingrain Window Service", string.Empty, string.Empty, string.Empty, CONSTANTS.CustomConstraintsLog);
                        return;
                    }
                }

                if (appDeliveryConstructs == null)
                {
                    _DatabaseLoggerService.UpdateOfflineException(item, "AppDeliveryConstructs is null");
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(ConstraintsSchedularService), nameof(StartModelTraining), "SSAI Template : '" + item.ModelName + "' -  AppDeliveryConstructs is null", "Ingrain Window Service", "", item.ClientUId, item.DeliveryConstructUID, CONSTANTS.CustomConstraintsLog);
                    return;
                }
                else if (appDeliveryConstructs.AppServiceClientDeliveryConstructs == null)
                {
                    _DatabaseLoggerService.UpdateOfflineException(item, "appDeliveryConstructs.AppServiceClientDeliveryConstructs is null");
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(ConstraintsSchedularService), nameof(StartModelTraining), "SSAI Template : '" + item.ModelName + "' -  appDeliveryConstructs.AppServiceClientDeliveryConstructs is null", "Ingrain Window Service", "", item.ClientUId, item.DeliveryConstructUID, CONSTANTS.CustomConstraintsLog);
                    return;
                }

                if (appDeliveryConstructs != null)
                {
                    if (appDeliveryConstructs.AppServiceClientDeliveryConstructs.Count > 0)
                    {
                        try
                        {
                            foreach (var provisionedDetails in appDeliveryConstructs.AppServiceClientDeliveryConstructs)
                            {
                                if (!string.IsNullOrEmpty(provisionedDetails.ClientUId) && !string.IsNullOrEmpty(provisionedDetails.DeliveryConstructUId))
                                {
                                    if (bTrainingAtTeamLevel)
                                    {
                                        WINSERVICEMODELS.ConstructsDTO TeamAreaUIdAtDCLevel = _PhoenixhadoopConnection.FetchTeamAreaUID(provisionedDetails.ClientUId, provisionedDetails.DeliveryConstructUId, ProvisionedAppServiceUID);

                                        foreach (WINSERVICEMODELS.DeliveryConstructsDTO TeamArea in TeamAreaUIdAtDCLevel.DeliveryConstructs)
                                        {
                                            string TeamAreaUID = TeamArea.DeliveryConstructUId;
                                            var model = _DBConnection.CheckIfSSAIModelTrained(provisionedDetails.ClientUId, provisionedDetails.DeliveryConstructUId, item.CorrelationId, SchedulerUserName, TeamAreaUID);

                                            if (model != null)
                                            {
                                                // if a Trained model is present , Check if Status = E . If yes , then re-trigger training of this failed model
                                                var status = _DBConnection.GetAutoTrainStatus(model.CorrelationId);
                                                if (status == "E")
                                                {
                                                    TrainSSAIModel(item, provisionedDetails.ClientUId, provisionedDetails.DeliveryConstructUId, model.CorrelationId, model.AppId, ProvisionedAppServiceUID, TeamAreaUID);

                                                }
                                                //else if (status == "Deployed")
                                                //{
                                                //    //if the prior training request is completed , only then we will start training New model
                                                //    // If Constraint = Training of Multiple Model is selected , then trigger training of one more Model
                                                //    if (bMultipleModel)
                                                //    {
                                                //        TrainSSAIModel(item, provisionedDetails.ClientUId, provisionedDetails.DeliveryConstructUId, null, item.AppId, ProvisionedAppServiceUID, TeamAreaUID);
                                                //    }
                                                //}
                                                else if (status == "In Progress")
                                                {
                                                    _DatabaseLoggerService.LogInfoMessageToDB(provisionedDetails.ClientUId, provisionedDetails.DeliveryConstructUId, item.CorrelationId, "", "", "Training", "Training is Inprogress", null);
                                                }
                                            }
                                            else if (model == null)
                                            {
                                                TrainSSAIModel(item, provisionedDetails.ClientUId, provisionedDetails.DeliveryConstructUId, null, item.AppId, ProvisionedAppServiceUID, TeamAreaUID);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        var model = _DBConnection.CheckIfSSAIModelTrained(provisionedDetails.ClientUId, provisionedDetails.DeliveryConstructUId, item.CorrelationId, SchedulerUserName);

                                        if (model != null)
                                        {
                                            // if a Trained model is present , Check if Status = E . If yes , then re-trigger training of this failed model
                                            var status = _DBConnection.GetAutoTrainStatus(model.CorrelationId);
                                            if (status == "E")
                                            {
                                                TrainSSAIModel(item, provisionedDetails.ClientUId, provisionedDetails.DeliveryConstructUId, model.CorrelationId, item.AppId, ProvisionedAppServiceUID, string.Empty);
                                            }
                                            //else if (status == "Deployed")
                                            //{
                                            //    // If Constraint = Training of Multiple Model is selected , then trigger training of one more Model
                                            //    if (bMultipleModel)
                                            //    {
                                            //        TrainSSAIModel(item, provisionedDetails.ClientUId, provisionedDetails.DeliveryConstructUId, null, item.AppId, ProvisionedAppServiceUID, string.Empty);
                                            //    }
                                            //}
                                            else if (status == "In Progress")
                                            {
                                                _DatabaseLoggerService.LogInfoMessageToDB(provisionedDetails.ClientUId, provisionedDetails.DeliveryConstructUId, item.CorrelationId, "", "", "Training", "Training is Inprogress", null);
                                            }
                                        }
                                        else if (model == null)
                                        {
                                            TrainSSAIModel(item, provisionedDetails.ClientUId, provisionedDetails.DeliveryConstructUId, null, item.AppId, ProvisionedAppServiceUID, string.Empty);
                                        }
                                    }
                                }
                                else
                                {
                                    return;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            LOGGING.LogManager.Logger.LogErrorMessage(typeof(ConstraintsSchedularService), nameof(StartModelTraining), "SSAI Template : '" + item.ModelName + "' - Training terminated ; Exception : " + ex.Message + "- STACKTRACE - " + ex.StackTrace + ex.InnerException, ex, "Ingrain Window Service", string.Empty, string.Empty, string.Empty, CONSTANTS.CustomConstraintsLog);
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ConstraintsSchedularService), nameof(StartModelTraining), ex.Message + "- STACKTRACE - " + ex.StackTrace + ex.InnerException, ex, "Ingrain Window Service", string.Empty, string.Empty, string.Empty, CONSTANTS.CustomConstraintsLog);
                _DatabaseLoggerService.UpdateOfflineException(item, ex.Message);
            }
        }
        public void TrainSSAIModel(DATAMODELS.DeployModelsDto item, string clientId, string deliveryConstructId, string correlationId, string AppId, string ProvisionedAppServiceUID, string TeamAreaUID)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ConstraintsSchedularService), nameof(TrainSSAIModel), "SSAI Template : '" + item.ModelName + "' - TrainSSAIModel Method Called", "Ingrain Window Service", "", "", "", CONSTANTS.CustomConstraintsLog);
            //if applicationId is IA we have to do the datacheck
            bool isDataAvailable = CheckHadoopData(clientId,
                                                   deliveryConstructId,
                                                   item.CorrelationId,
                                                   DateTime.UtcNow.AddYears(-2).ToString("yyyy-MM-dd"),
                                                   DateTime.UtcNow.ToString("yyyy-MM-dd"));

            if (isDataAvailable || !bIsIAApp)
            {
                Thread.Sleep(120000);
                DATAMODELS.GenericTrainingRequestDetails GenericTrainingRequestDetails = new DATAMODELS.GenericTrainingRequestDetails();
                GenericTrainingRequestDetails.ClientUId = clientId;
                GenericTrainingRequestDetails.DeliveryConstructUId = deliveryConstructId;
                GenericTrainingRequestDetails.UseCaseId = item.CorrelationId;
                GenericTrainingRequestDetails.DataSource = item.DataSource;
                GenericTrainingRequestDetails.ApplicationId = AppId;
                GenericTrainingRequestDetails.ProvisionedAppServiceUID = ProvisionedAppServiceUID;
                GenericTrainingRequestDetails.TeamAreaUID = TeamAreaUID;
                GenericTrainingRequestDetails.CorrelationId = correlationId;
                GenericTrainingRequestDetails.DataSourceDetails = null;
                GenericTrainingRequestDetails.UserId = SchedulerUserName;
                GenericTrainingRequestDetails.IsAmbulanceLane = "False";

                //if (!string.IsNullOrEmpty(AppName) && AppName.ToUpper() == CONSTANTS.SPAAPP)
                //{
                //    GenericTrainingRequestDetails.ResponseCallbackUrl = "";
                //}

                if (bIsAmbulanceLane)
                {
                    string[] ambulanceLane = new string[] { CONSTANTS.CRHigh, CONSTANTS.CRCritical, CONSTANTS.SRHigh, CONSTANTS.SRCritical, CONSTANTS.PRHigh, CONSTANTS.PRCritical };
                    if (ambulanceLane.Contains(item.CorrelationId))
                        GenericTrainingRequestDetails.IsAmbulanceLane = "True";
                }

                if (GenericTrainingRequestDetails.IsAmbulanceLane == "True")
                {
                    if (bClosedSprintIteration)
                    {
                        Dictionary<string, List<string>> oIterationList = _PhoenixhadoopConnection.FetchClosedSprintandIterations(clientId, deliveryConstructId, ProvisionedAppServiceUID, CONSTANTS.TrainingConstraint);
                        if (oIterationList != null)
                            GenericTrainingRequestDetails.QueryData = oIterationList;
                    }

                    if (bClosedUserStory)
                    {
                        Dictionary<string, List<string>> oIterationList = _PhoenixhadoopConnection.FetchClosedUserStory(clientId, deliveryConstructId, ProvisionedAppServiceUID);
                        if (oIterationList != null)
                            GenericTrainingRequestDetails.QueryData = oIterationList;
                    }
                }

                InvokeSSAITrainingAPI(GenericTrainingRequestDetails);
            }
        }
        public void InvokeSSAITrainingAPI(DATAMODELS.GenericTrainingRequestDetails GenericTrainingRequestDetails)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ConstraintsSchedularService), nameof(InvokeSSAITrainingAPI), "SSAI Template[Correlation ID] : '" + GenericTrainingRequestDetails.UseCaseId + "' - InvokeSSAITrainingAPI Method Called", "Ingrain Window Service", "", GenericTrainingRequestDetails.ClientUId, GenericTrainingRequestDetails.DeliveryConstructUId, CONSTANTS.CustomConstraintsLog);
            string apiPath = "/api/InitiateModelTraining";
            try
            {
                StringContent content = new StringContent(JsonConvert.SerializeObject(GenericTrainingRequestDetails), Encoding.UTF8, "application/json");
                string resourceId = appSettings.resourceId;
                string token = _TokenService.GenerateToken();
                string baseURI = appSettings.IngrainAPIUrl;

                var response = _HttpMethodService.InvokePOSTRequest(token, baseURI, apiPath, content, resourceId);
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(ConstraintsSchedularService), nameof(InvokeSSAITrainingAPI), "SSAI Template[Correlation ID] : '" + GenericTrainingRequestDetails.UseCaseId + "' - Call to Ingrain API " + apiPath + " Failed", "Ingrain Window Service", "", "", "", CONSTANTS.CustomConstraintsLog);
                    _DatabaseLoggerService.LogErrorMessageToDB(GenericTrainingRequestDetails.ClientUId, GenericTrainingRequestDetails.DeliveryConstructUId, GenericTrainingRequestDetails.UseCaseId, "", "", "Training", "Ingrain Training api status code - " + response.StatusCode + "-" + response.Content.ReadAsStringAsync().Result, null);
                }
                else
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(ConstraintsSchedularService), nameof(InvokeSSAITrainingAPI), "SSAI Template[Correlation ID] : '" + GenericTrainingRequestDetails.UseCaseId + "' - Call to Ingrain API " + apiPath + " Successful", "Ingrain Window Service", "", "", "", CONSTANTS.CustomConstraintsLog);
                    _DatabaseLoggerService.LogInfoMessageToDB(GenericTrainingRequestDetails.ClientUId, GenericTrainingRequestDetails.DeliveryConstructUId, GenericTrainingRequestDetails.UseCaseId, "", "", "Training", "Ingrain Training api status code - " + response.StatusCode + "-" + response.Content.ReadAsStringAsync().Result, null);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ConstraintsSchedularService), nameof(InvokeSSAITrainingAPI), "Exception SSAI Template[Correlation ID] - " + GenericTrainingRequestDetails.CorrelationId + ex.Message, ex, "Ingrain Window Service", string.Empty, GenericTrainingRequestDetails.ClientUId, GenericTrainingRequestDetails.DeliveryConstructUId, CONSTANTS.CustomConstraintsLog);
                _DatabaseLoggerService.LogErrorMessageToDB(GenericTrainingRequestDetails.ClientUId, GenericTrainingRequestDetails.DeliveryConstructUId, GenericTrainingRequestDetails.UseCaseId, "", "", "Training", ex.ToString(), null);
                _DatabaseLoggerService.UpdateOfflineException(GenericTrainingRequestDetails, GenericTrainingRequestDetails.CorrelationId + "; "+ GenericTrainingRequestDetails.ClientUId + "; " + GenericTrainingRequestDetails.DeliveryConstructUId + " : " + ex.Message);
            }
        }
        public bool CheckHadoopData(string clientUId, string deliveryConstructUId, string UseCaseId, string startDate, string endDate)
        {
            bool response = false;
            if (UseCaseId == IA_SPIUseCaseId)
            {
                bIsIAApp = true;
                response = _PhoenixhadoopConnection.CheckSPIData(clientUId, deliveryConstructUId, startDate, endDate);
            }
            else if (UseCaseId == IA_DefectRateUseCaseId)
            {
                bIsIAApp = true;
                response = _PhoenixhadoopConnection.CheckDefectRateDate(clientUId, deliveryConstructUId, startDate, endDate);
            }
            else if (UseCaseId == IA_CRUseCaseId)
            {
                bIsIAApp = true;
                response = _PhoenixhadoopConnection.CheckCRData(clientUId, deliveryConstructUId, startDate, endDate);
            }
            else if (UseCaseId == IA_UserStoryUseCaseId)
            {
                bIsIAApp = true;
                response = _PhoenixhadoopConnection.CheckUserStoryData(clientUId, deliveryConstructUId, startDate, endDate);
            }
            else if (UseCaseId == IA_RequirementUseCaseId)
            {
                bIsIAApp = true;
                response = _PhoenixhadoopConnection.CheckRequirementData(clientUId, deliveryConstructUId, startDate, endDate);
            }

            return response;
        }

        #endregion

        #region AI Training Methods
        public void StartAIModelTraining(UsecaseDetails item)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ConstraintsSchedularService), nameof(StartAIModelTraining), "AI Model - '" + item.ModelName + "' - Training Initiated", "Ingrain Window Service", "", "", "", CONSTANTS.CustomConstraintsLog);
            bool bAppProvisionedClientDC = false, bTrainingAtTeamLevel = false;
            WINSERVICEMODELS.AppDeliveryConstructs appDeliveryConstructs = null;

            try
            {
                _DatabaseLoggerService.UpdateAIOfflineRunTime(item);

                string ClientUId = string.Empty, DCUID = string.Empty;
                Service service = _DBConnection.GetAiCoreServiceDetails(item.ServiceId);

                if (service.ServiceCode == "CLUSTERING" || service.ServiceCode == "WORDCLOUD")
                {
                    ClusteringAPIModel AICoreModel = new ClusteringAPIModel();
                    AICoreModel = _DBConnection.GetClusteringModel(item.CorrelationId);

                    ClientUId = AICoreModel.ClientID;
                    DCUID = AICoreModel.DCUID;
                }
                else
                {
                    AIDataModels.AICoreModels AICoreModel = new AIDataModels.AICoreModels();
                    AICoreModel = _DBConnection.GetAICoreModelPath(item.CorrelationId);
                    ClientUId = AICoreModel.ClientId;
                    DCUID = AICoreModel.DeliveryConstructId;
                }

                //Get List of Training Constrainst selected for ModelTemplate
                List<string> oCustomConstrainsList = _DBConnection.GetCustomConstraints(item.CorrelationId, CONSTANTS.TrainingConstraint, CONSTANTS.AICustomContraints);
                if (oCustomConstrainsList == null)
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(ConstraintsSchedularService), nameof(StartAIModelTraining), "AI Model - '" + item.ModelName + "' - No Constraints Found , Training Terminated", "Ingrain Window Service", "", "", "", CONSTANTS.CustomConstraintsLog);
                    return;
                }

                if (oCustomConstrainsList != null)
                {
                    foreach (string ConstraintCode in oCustomConstrainsList)
                    {
                        switch (ConstraintCode)
                        {
                            case "AppProvisionedClientDC":
                                bAppProvisionedClientDC = true;
                                //Fetch the AppServiceUID linked to the Deployed Model's AppId 
                                DATAMODELS.AppIntegration oAppIntegration = _DBConnection.FetchAppServiceUID(item.ApplicationId);
                                if (oAppIntegration != null)
                                {
                                    try
                                    {
                                        ProvisionedAppServiceUID = oAppIntegration.ProvisionedAppServiceUID;
                                        AppName = oAppIntegration.ApplicationName;

                                        if (!string.IsNullOrEmpty(ProvisionedAppServiceUID))
                                        {
                                            //Fetch List of provisioned Client and Delivery Constructs for AppServiceUId - app
                                            appDeliveryConstructs = _PhoenixhadoopConnection.FetchClientsDeliveryConstructs(ClientUId, DCUID, ProvisionedAppServiceUID);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        LOGGING.LogManager.Logger.LogErrorMessage(typeof(ConstraintsSchedularService), nameof(StartAIModelTraining), "AI Model - '" + item.ModelName + "' - Training terminated ; Exception : " + ex.Message + "- STACKTRACE - " + ex.StackTrace + ex.InnerException, ex, "Ingrain Window Service", string.Empty, string.Empty, string.Empty, CONSTANTS.CustomConstraintsLog);
                                        return;
                                    }
                                }
                                break;

                            //case "SingleModel":
                            //    bMultipleModel = false;
                            //    break;

                            //case "MultipleModel":
                            //    bMultipleModel = true;
                            //    break;

                            case "DCLevelTraining":
                                bTrainingAtTeamLevel = false;
                                break;

                            case "TeamAreaLevel":
                                bTrainingAtTeamLevel = true;
                                break;

                            case "IsAmbulance":
                                bIsAmbulanceLane = true;
                                break;

                            case "ClosedSprintnIteration":
                                bClosedSprintIteration = true;
                                break;

                            case "ClosedUserStory":
                                bClosedUserStory = true;
                                break;

                            default:
                                break;

                        }
                    }
                }

                if (!bAppProvisionedClientDC)
                {
                    try
                    {
                        DATAMODELS.AppIntegration oAppIntegration = _DBConnection.FetchAppServiceUID(IngrainAppId);
                        ProvisionedAppServiceUID = oAppIntegration.ProvisionedAppServiceUID;
                        //provisoning check is based on usecase and applicationId
                        //Fetch List of provisioned Client and Delivery Constructs for AppServiceUId for Ingrain APP
                        appDeliveryConstructs = _PhoenixhadoopConnection.FetchClientsDeliveryConstructs(ClientUId, DCUID, ProvisionedAppServiceUID);
                    }
                    catch (Exception ex)
                    {
                        LOGGING.LogManager.Logger.LogErrorMessage(typeof(ConstraintsSchedularService), nameof(StartAIModelTraining), "AI Model - '" + item.ModelName + "' - Training terminated ; Exception : " + ex.Message + "- STACKTRACE - " + ex.StackTrace + ex.InnerException, ex, "Ingrain Window Service", string.Empty, string.Empty, string.Empty, CONSTANTS.CustomConstraintsLog);
                        return;
                    }
                }

                if (appDeliveryConstructs == null)
                {
                    _DatabaseLoggerService.UpdateAIOfflineException(item, "AppDeliveryConstructs is null");

                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(ConstraintsSchedularService), nameof(StartAIModelTraining), "AI Model - '" + item.ModelName + "' - AppDeliveryConstructs is null for Model - " + item.ModelName + " and CorrelationID : " + item.CorrelationId, "Ingrain Window Service", "", ClientUId, DCUID, CONSTANTS.CustomConstraintsLog);
                    return;
                }
                else if (appDeliveryConstructs.AppServiceClientDeliveryConstructs == null)
                {
                    _DatabaseLoggerService.UpdateAIOfflineException(item, "appDeliveryConstructs.AppServiceClientDeliveryConstructs is null");
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(ConstraintsSchedularService), nameof(StartAIModelTraining), "AI Model - '" + item.ModelName + "' - appDeliveryConstructs.AppServiceClientDeliveryConstructs is null for Model - " + item.ModelName + " and CorrelationID : " + item.CorrelationId, "Ingrain Window Service", "", ClientUId, DCUID, CONSTANTS.CustomConstraintsLog);
                    return;
                }

                if (appDeliveryConstructs != null)
                {
                    if (appDeliveryConstructs.AppServiceClientDeliveryConstructs.Count > 0)
                    {
                        try
                        {
                            foreach (var provisionedDetails in appDeliveryConstructs.AppServiceClientDeliveryConstructs)
                            {
                                if (!string.IsNullOrEmpty(provisionedDetails.ClientUId) && !string.IsNullOrEmpty(provisionedDetails.DeliveryConstructUId))
                                {
                                    if (bTrainingAtTeamLevel)
                                    {
                                        WINSERVICEMODELS.ConstructsDTO TeamAreaUIdAtDCLevel = _PhoenixhadoopConnection.FetchTeamAreaUID(provisionedDetails.ClientUId, provisionedDetails.DeliveryConstructUId, ProvisionedAppServiceUID);

                                        foreach (WINSERVICEMODELS.DeliveryConstructsDTO TeamArea in TeamAreaUIdAtDCLevel.DeliveryConstructs)
                                        {
                                            string TeamAreaUID = TeamArea.DeliveryConstructUId;
                                            /// code to train AI service usecase IDs
                                            var model = _DBConnection.CheckIfAIModelTrained(provisionedDetails.ClientUId, provisionedDetails.DeliveryConstructUId, item.UsecaseId, item.ServiceId, PredictionSchedulerUserName, service.ServiceCode, TeamAreaUID);

                                            if (model != null)
                                            {
                                                if (model["Status"] != null && model["Status"] == "E")
                                                {
                                                    if (_DBConnection.DeleteAIModel(model["CorrelationId"].ToString()))
                                                    {

                                                        TrainAIModel(item, provisionedDetails.ClientUId, provisionedDetails.DeliveryConstructUId, PredictionSchedulerUserName, TeamAreaUID, item.ModelName);
                                                    }
                                                }
                                                //else if (model["Status"] == "C")
                                                //{
                                                //    // If Constraint = Training of Multiple Model is selected , then trigger training of one more Model
                                                //    if (bMultipleModel)
                                                //    {
                                                //        TrainAIModel(item, provisionedDetails.ClientUId, provisionedDetails.DeliveryConstructUId, PredictionSchedulerUserName, TeamAreaUID, item.ModelName);
                                                //    }
                                                //}
                                                else
                                                {
                                                    _DatabaseLoggerService.LogInfoMessageToDB(provisionedDetails.ClientUId, provisionedDetails.DeliveryConstructUId, item.UsecaseId, "", "", "Training", "Training already completed/Inprogress", null);
                                                }
                                            }
                                            else if (model == null)
                                            {
                                                TrainAIModel(item, provisionedDetails.ClientUId, provisionedDetails.DeliveryConstructUId, PredictionSchedulerUserName, TeamAreaUID, item.ModelName);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        var model = _DBConnection.CheckIfAIModelTrained(provisionedDetails.ClientUId, provisionedDetails.DeliveryConstructUId, item.UsecaseId, item.ServiceId, PredictionSchedulerUserName, service.ServiceCode);

                                        if (model != null)
                                        {
                                            if (model["Status"] != null && model["Status"] == "E")
                                            {
                                                if (_DBConnection.DeleteAIModel(model["CorrelationId"].ToString()))
                                                    TrainAIModel(item, provisionedDetails.ClientUId, provisionedDetails.DeliveryConstructUId, PredictionSchedulerUserName, string.Empty, item.ModelName);
                                            }
                                            //else if (model["Status"] == "C")
                                            //{
                                            //    // If Constraint = Training of Multiple Model is selected , then trigger training of one more Model
                                            //    if (bMultipleModel)
                                            //    {
                                            //        TrainAIModel(item, provisionedDetails.ClientUId, provisionedDetails.DeliveryConstructUId, PredictionSchedulerUserName, string.Empty, item.ModelName);
                                            //    }
                                            //}
                                            else
                                            {
                                                _DatabaseLoggerService.LogInfoMessageToDB(provisionedDetails.ClientUId, provisionedDetails.DeliveryConstructUId, item.UsecaseId, "", "", "Training", "Training already completed/Inprogress", null);
                                            }
                                        }
                                        else if (model == null)
                                        {
                                            TrainAIModel(item, provisionedDetails.ClientUId, provisionedDetails.DeliveryConstructUId, PredictionSchedulerUserName, string.Empty, item.ModelName);
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            LOGGING.LogManager.Logger.LogErrorMessage(typeof(ConstraintsSchedularService), nameof(StartAIModelTraining), "AI Model - '" + item.ModelName + "' - Training terminated ; Exception : " + ex.Message + "- STACKTRACE - " + ex.StackTrace + ex.InnerException, ex, "Ingrain Window Service", string.Empty, string.Empty, string.Empty, CONSTANTS.CustomConstraintsLog);
                            return;
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ConstraintsSchedularService), nameof(StartAIModelTraining), ex.Message + "- STACKTRACE - " + ex.StackTrace + ex.InnerException, ex, "Ingrain Window Service", string.Empty, string.Empty, string.Empty, CONSTANTS.CustomConstraintsLog);
                _DatabaseLoggerService.UpdateAIOfflineException(item, ex.Message);
            }
        }
        public void TrainAIModel(UsecaseDetails item, string clientId, string deliveryConstructId, string userId, string TeamAreaUID, string templateModelName)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ConstraintsSchedularService), nameof(TrainAIModel), "AI Model - '" + templateModelName + "' UseCaseID : " + item.UsecaseId + " - TrainAIModel Method Called", "Ingrain Window Service", "", clientId, deliveryConstructId, CONSTANTS.CustomConstraintsLog);
            try
            {
                string IsAmbulanceLane = "false";
                Dictionary<string, List<string>> QueryData = null;
                bool isDataAvailable = CheckHadoopData(clientId,
                                                   deliveryConstructId,
                                                   item.UsecaseId,
                                                   DateTime.UtcNow.AddYears(-2).ToString("MM/dd/yyyy"),
                                                   DateTime.UtcNow.ToString("MM/dd/yyyy"));
                if (isDataAvailable || !bIsIAApp)
                {
                    Thread.Sleep(180000);

                    if (bClosedSprintIteration)
                    {
                        Dictionary<string, List<string>> oIterationList = _PhoenixhadoopConnection.FetchClosedSprintandIterations(clientId, deliveryConstructId, ProvisionedAppServiceUID, CONSTANTS.TrainingConstraint);
                        if (oIterationList != null)
                            QueryData = oIterationList;
                    }

                    if (bClosedUserStory)
                    {
                        Dictionary<string, List<string>> oIterationList = _PhoenixhadoopConnection.FetchClosedUserStory(clientId, deliveryConstructId, ProvisionedAppServiceUID);
                        if (oIterationList != null)
                            QueryData = oIterationList;
                    }

                    if (bIsAmbulanceLane)
                    {
                        string[] ambulanceLane = new string[] { CONSTANTS.CRHigh, CONSTANTS.CRCritical, CONSTANTS.SRHigh, CONSTANTS.SRCritical, CONSTANTS.PRHigh, CONSTANTS.PRCritical };
                        if (ambulanceLane.Contains(item.UsecaseId))
                            IsAmbulanceLane = "true";
                    }

                    DATAMODELS.CustomDataInputParams oCustomDataPull = null;
                    DATAMODELS.CustomSourceDTO oCustomSoureDetails = JsonConvert.DeserializeObject<DATAMODELS.CustomSourceDTO>(item.SourceDetails);

                    if (item.SourceName.ToUpper() == CONSTANTS.CustomDataApi.ToUpper())
                    {
                        oCustomDataPull = new DATAMODELS.CustomDataInputParams
                        {
                            SourceType = CONSTANTS.CustomDataApi,
                            Data = JObject.Parse(appSettings.IsAESKeyVault ? CryptographyUtility.Decrypt(oCustomSoureDetails.CustomSource) : AesProvider.Decrypt(oCustomSoureDetails.CustomSource, _aesKey, _aesVector))
                        };
                    }
                    if (item.SourceName.ToUpper() == CONSTANTS.CustomDbQuery.ToUpper())
                    {
                        DATAMODELS.CustomQueryParamArgs CustomQueryParamArgs = JsonConvert.DeserializeObject<DATAMODELS.CustomQueryParamArgs>(item.SourceDetails);
                        var CustomSource = JObject.Parse(appSettings.IsAESKeyVault ? CryptographyUtility.Decrypt(CustomQueryParamArgs.CustomSource) : AesProvider.Decrypt(CustomQueryParamArgs.CustomSource, _aesKey, _aesVector));
                        DATAMODELS.QueryDTO CustomQueryCS = JsonConvert.DeserializeObject<DATAMODELS.QueryDTO>(Convert.ToString(CustomSource));
                        var formContent = new FormUrlEncodedContent(new[]
                       {
                            new KeyValuePair<string, string>("ClientId", clientId),
                            new KeyValuePair<string, string>("DeliveryConstructId", deliveryConstructId),
                            new KeyValuePair<string, string>("ServiceId",item.ServiceId),
                            new KeyValuePair<string, string>("UsecaseId", item.UsecaseId),
                            new KeyValuePair<string, string>("DataSource", item.SourceName),
                            new KeyValuePair<string, string>("ApplicationId",item.ApplicationId),
                            new KeyValuePair<string, string>("ModelName", item.ModelName),
                            new KeyValuePair<string, string>("DataSourceDetails", ""),
                            new KeyValuePair<string, string>("UserId", userId) ,
                            new KeyValuePair<string, string>("TeamAreaUID", TeamAreaUID),
                            new KeyValuePair<string, string>("QueryData", (QueryData !=null) ? QueryData.ToString() : string.Empty),
                            new KeyValuePair<string, string>("IsAmbulanceLane", IsAmbulanceLane),
                            new KeyValuePair<string, string>("CustomDataPull",CustomQueryCS.Query),
                            new KeyValuePair<string, string>("DateColumn",CustomQueryCS.DateColumn )
                        });
                        string resourceId = appSettings.resourceId;
                        string token = _TokenService.GenerateToken();
                        string baseURI = appSettings.IngrainAPIUrl;
                        string apiPath = "/api/InitiateAIModelTraining";
                        var response = _HttpMethodService.InvokePOSTRequestFromData(token, baseURI, apiPath, formContent, resourceId);
                        if (response.StatusCode != HttpStatusCode.OK)
                        {
                            _DatabaseLoggerService.LogErrorMessageToDB(clientId, deliveryConstructId, item.UsecaseId, IterationEntityUId, "", "Training", "Ingrain Training api status code - " + response.StatusCode + "-" + response.Content.ReadAsStringAsync().Result, null);
                        }
                        else
                        {
                            _DatabaseLoggerService.LogInfoMessageToDB(clientId, deliveryConstructId, item.UsecaseId, IterationEntityUId, "", "Training", "Ingrain Training api status code - " + response.StatusCode + "-" + response.Content.ReadAsStringAsync().Result, null);
                            var jsonResult = response.Content.ReadAsStringAsync().Result;
                            JObject result = JObject.Parse(jsonResult);
                            _DBConnection.InsertAICustomConstraintsRequests((result["CorrelationId"]).ToString(), (result["UsecaseId"]).ToString());
                        }
                    }
                    else
                    {
                        var formContent = new FormUrlEncodedContent(new[]
                           {
                            new KeyValuePair<string, string>("ClientId", clientId),
                            new KeyValuePair<string, string>("DeliveryConstructId", deliveryConstructId),
                            new KeyValuePair<string, string>("ServiceId",item.ServiceId),
                            new KeyValuePair<string, string>("UsecaseId", item.UsecaseId),
                            new KeyValuePair<string, string>("DataSource", item.SourceName),
                            new KeyValuePair<string, string>("ApplicationId",item.ApplicationId),
                            new KeyValuePair<string, string>("ModelName", item.ModelName),
                            new KeyValuePair<string, string>("DataSourceDetails", ""),
                            new KeyValuePair<string, string>("UserId", userId) ,
                            new KeyValuePair<string, string>("TeamAreaUID", TeamAreaUID),
                            new KeyValuePair<string, string>("QueryData", (QueryData !=null) ? QueryData.ToString() : string.Empty),
                            new KeyValuePair<string, string>("IsAmbulanceLane", IsAmbulanceLane),
                            new KeyValuePair<string, string>("CustomDataPull",JsonConvert.SerializeObject(oCustomDataPull)),
                            new KeyValuePair<string, string>(CONSTANTS.pad,oCustomSoureDetails.pad ),
                            new KeyValuePair<string, string>(CONSTANTS.metrics,oCustomSoureDetails.metric ),
                            new KeyValuePair<string, string>(CONSTANTS.InstaMl,oCustomSoureDetails.InstaMl ),
                            new KeyValuePair<string, string>(CONSTANTS.EntitiesName,oCustomSoureDetails.EntitiesName ),
                            new KeyValuePair<string, string>(CONSTANTS.MetricNames,oCustomSoureDetails.MetricNames )
                        });
                        string resourceId = appSettings.resourceId;
                        string token = _TokenService.GenerateToken();
                        string baseURI = appSettings.IngrainAPIUrl;
                        string apiPath = "/api/InitiateAIModelTraining";
                        var response = _HttpMethodService.InvokePOSTRequestFromData(token, baseURI, apiPath, formContent, resourceId);
                        if (response.StatusCode != HttpStatusCode.OK)
                        {
                            _DatabaseLoggerService.LogErrorMessageToDB(clientId, deliveryConstructId, item.UsecaseId, IterationEntityUId, "", "Training", "Ingrain Training api status code - " + response.StatusCode + "-" + response.Content.ReadAsStringAsync().Result, null);
                        }
                        else
                        {
                            _DatabaseLoggerService.LogInfoMessageToDB(clientId, deliveryConstructId, item.UsecaseId, IterationEntityUId, "", "Training", "Ingrain Training api status code - " + response.StatusCode + "-" + response.Content.ReadAsStringAsync().Result, null);
                            var jsonResult = response.Content.ReadAsStringAsync().Result;
                            JObject result = JObject.Parse(jsonResult);
                            _DBConnection.InsertAICustomConstraintsRequests((result["CorrelationId"]).ToString(), (result["UsecaseId"]).ToString());

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ConstraintsSchedularService), nameof(TrainAIModel), ex.Message, ex, "Ingrain Window Service", string.Empty, clientId, deliveryConstructId, CONSTANTS.CustomConstraintsLog);
                _DatabaseLoggerService.LogErrorMessageToDB(clientId, deliveryConstructId, item.UsecaseId, CREntityUId, "", "Training", ex.ToString(), null);
            }

        }

        #endregion

        #region Prediction Methods
        public void StartModelPrediction(DATAMODELS.DeployModelsDto item)
        {
            try
            {
                List<string> oCustomConstrainsList = _DBConnection.GetCustomConstraints(item.CorrelationId, CONSTANTS.PredictionConstraint, CONSTANTS.SSAICustomContraints);
                if (oCustomConstrainsList == null)
                {
                    return;
                }
                if (oCustomConstrainsList != null)
                {
                    try
                    {
                        foreach (string ConstraintCode in oCustomConstrainsList)
                        {
                            switch (ConstraintCode)
                            {
                                case "ActiveRelease":
                                    _PhoenixhadoopConnection.FetchPhoenixIterationsList(item, "SSAI");
                                    break;

                                case "CR":
                                    InsertENSNotificationlog(item, CONSTANTS.SSAIAPP);
                                    break;

                                default:
                                    break;

                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LOGGING.LogManager.Logger.LogErrorMessage(typeof(ConstraintsSchedularService), nameof(StartModelPrediction), "SSAI Template : '" + item.ModelName + "' - Training terminated ; Exception : " + ex.Message, ex, "Ingrain Window Service", string.Empty, item.ClientUId, item.DeliveryConstructUID, CONSTANTS.CustomConstraintsLog);
                        return;
                    }
                }
                _DatabaseLoggerService.UpdateOfflinePredRunTime(item);

            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ConstraintsSchedularService), nameof(StartModelPrediction), "SSAI Template : '" + item.ModelName + "' ; " + ex.Message + "- STACKTRACE - " + ex.StackTrace + ex.InnerException, ex, "Ingrain Window Service", string.Empty, item.ClientUId, item.DeliveryConstructUID, CONSTANTS.CustomConstraintsLog);
                _DatabaseLoggerService.UpdateOfflinePredException(item, ex.Message);
            }
        }

        public void StartAIModelPrediction(UsecaseDetails item)
        {
            try
            {
                _DatabaseLoggerService.UpdateAIOfflineRunTime(item);

                List<string> oCustomConstrainsList = _DBConnection.GetCustomConstraints(item.CorrelationId, CONSTANTS.PredictionConstraint, CONSTANTS.AICustomContraints);

                if (oCustomConstrainsList == null)
                {
                    return;
                }

                if (oCustomConstrainsList != null)
                {
                    try
                    {
                        foreach (string ConstraintCode in oCustomConstrainsList)
                        {
                            switch (ConstraintCode)
                            {
                                case "ActiveRelease":
                                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(ConstraintsSchedularService), nameof(StartAIModelPrediction), "Executing -" + item, "Ingrain Window Service", "", "", "", CONSTANTS.CustomConstraintsLog);
                                    _PhoenixhadoopConnection.FetchPhoenixIterationsList(item, CONSTANTS.AIAPP);
                                    break;

                                case "CR":
                                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(ConstraintsSchedularService), nameof(StartAIModelPrediction), "Executing -" + item, "Ingrain Window Service", "", "", "", CONSTANTS.CustomConstraintsLog);
                                    InsertENSNotificationlog(item, CONSTANTS.AIAPP);
                                    break;

                                default:
                                    break;

                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LOGGING.LogManager.Logger.LogErrorMessage(typeof(ConstraintsSchedularService), nameof(StartAIModelPrediction), "AI Template : '" + item.ModelName + "' - Training terminated ; Exception : " + ex.Message, ex, "Ingrain Window Service", string.Empty, string.Empty, string.Empty, CONSTANTS.CustomConstraintsLog);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ConstraintsSchedularService), nameof(StartAIModelPrediction), "AI Template : '" + item.ModelName + "' ; " + ex.Message + " - STACKTRACE - " + ex.StackTrace + ex.InnerException, ex, "Ingrain Window Service", string.Empty, string.Empty, string.Empty, CONSTANTS.CustomConstraintsLog);
                _DatabaseLoggerService.UpdateAIOfflineException(item, ex.Message);
            }
        }



        #endregion

        #region SSAI Retrain Methods
        public void SSAIReTrain()
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ConstraintsSchedularService), nameof(SSAIReTrain), "SSAIReTrain service started", "Ingrain Window Service", string.Empty, string.Empty, string.Empty, CONSTANTS.CustomConstraintsLog);
            try
            {
                var filterBuilder = Builders<BsonDocument>.Filter;
                var filter = filterBuilder.Eq(CONSTANTS.IsOffline, true)
                           & filterBuilder.Eq(CONSTANTS.IsModelTemplate, true)
                           & (filterBuilder.Where(x => "SourceName" != CONSTANTS.file))
                           & filterBuilder.Eq(CONSTANTS.Status, CONSTANTS.Deployed)
                           & filterBuilder.Eq(CONSTANTS.IsCarryOutRetraining, true);

                var DeployedModels = _DBConnection.GetCollectionData<BsonDocument>(filter, CONSTANTS.SSAIDeployedModels);

                foreach (var odeployedModel in DeployedModels)
                {
                    DATAMODELS.DeployModelsDto oModel = JsonConvert.DeserializeObject<DATAMODELS.DeployModelsDto>(odeployedModel.ToJson());

                    if (!oModel.LinkedApps[0].Equals("Ingrain"))
                    {
                        DATAMODELS.ModelRequestStatus oResponse = _DBConnection.GetModelStatus(oModel.ModelName, oModel.CorrelationId);

                        // Pick model for training , either when triggered for the First time 
                        // or the Previous training Request is Completed
                        if (oResponse == null || oResponse.RetrainingStatus == string.Empty || oResponse.RetrainingStatus == CONSTANTS.Completed || oResponse.RetrainingStatus == CONSTANTS.WorkerServiceStopped)
                        {
                            //Set Prediction Status = InProgress
                            _DBConnection.SetModelStatus(oModel.ModelName, oModel.CorrelationId, CONSTANTS.ReTrainingConstraint, CONSTANTS.InProgress);


                            filter = filterBuilder.Eq(CONSTANTS.IsOffline, true)
                               & filterBuilder.Eq(CONSTANTS.IsModelTemplate, false)
                               & (filterBuilder.Where(x => "SourceName" != CONSTANTS.file))
                               & filterBuilder.Eq(CONSTANTS.Status, CONSTANTS.Deployed)
                               & filterBuilder.Eq(CONSTANTS.TemplateUsecaseId, oModel.CorrelationId)
                               & filterBuilder.Eq(CONSTANTS.IsCarryOutRetraining, true);

                            var TrainedModels = _DBConnection.GetCollectionData<BsonDocument>(filter, CONSTANTS.SSAIDeployedModels);

                            //App Collection hitting to get Auto Train Days.
                            var appCollection = _database.GetCollection<DATAMODELS.AppIntegration>(CONSTANTS.AppIntegration);
                            var appFilter = Builders<DATAMODELS.AppIntegration>.Filter.Empty;
                            var appResults = appCollection.Find(appFilter).ToList();

                            int counter = 0;
                            if (appResults.Count > 0)
                            {
                                for (int i = 0; i < appResults.Count; i++)
                                {
                                    string appName = string.Empty;
                                    if (appResults[i].ApplicationName != null)
                                    {
                                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(ConstraintsSchedularService), nameof(SSAIReTrain), "SSAIReTrain service started", "Ingrain Window Service", string.Empty, appResults[i].clientUId, appResults[i].deliveryConstructUID, CONSTANTS.CustomConstraintsLog);
                                        appName = appResults[i].ApplicationName.Trim();
                                        if (counter == 0)
                                        {
                                            DATAMODELS.AppIntegration data = appResults.Find(x => x.ApplicationName == appSettings.ManualSPAAutoTrain);
                                            if (data != null)
                                            {
                                                appName = data.ApplicationName;
                                                if (appName != null)
                                                {
                                                    if (TrainedModels != null)
                                                    {
                                                        try
                                                        {
                                                            counter++;
                                                            ModelAutoTrainForAPP(TrainedModels, false);
                                                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ConstraintsSchedularService), nameof(SSAIReTrain), "SSAIReTrain END", "Ingrain Window Service", string.Empty, data.clientUId, data.deliveryConstructUID, CONSTANTS.CustomConstraintsLog);
                                                        }
                                                        catch (Exception ex)
                                                        {
                                                            counter++;
                                                            LOGGING.LogManager.Logger.LogErrorMessage(typeof(ConstraintsSchedularService), nameof(SSAIReTrain), ex.Message + ex.StackTrace, ex, "Ingrain Window Service", string.Empty, data.clientUId, data.deliveryConstructUID, CONSTANTS.CustomConstraintsLog);
                                                        }
                                                        counter++;
                                                    }
                                                }
                                            }
                                            counter++;
                                        }
                                        else
                                        {
                                            ModelRetrainOnAppNames(appResults[i], appName);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    //Set Prediction Status = Completed
                    _DBConnection.SetModelStatus(oModel.ModelName, oModel.CorrelationId, CONSTANTS.ReTrainingConstraint, CONSTANTS.Completed);

                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ConstraintsSchedularService), nameof(SSAIReTrain), ex.Message + ex.StackTrace, ex, "Ingrain Window Service", string.Empty, string.Empty, string.Empty, CONSTANTS.CustomConstraintsLog);
            }

        }

        private void ModelAutoTrainForAPP(List<BsonDocument> result, bool isIA)
        {
            bool bTriggerRetraining = false;
            if (result.Count > 0)
            {
                for (int i = 0; i < result.Count; i++)
                {
                    // Check whether any Constraint present on not for Retraining
                    bTriggerRetraining = CheckIfRetrain(result[i], CONSTANTS.ReTrainingConstraint, CONSTANTS.SSAICustomContraints);

                    if (bTriggerRetraining)
                    {
                        string modifiedOn = result[i]["ModifiedOn"].ToString();
                        DateTime? triggerDate = null;
                        DateTime modifiedDate = DateTime.Parse(modifiedOn);
                        triggerDate = modifiedDate.AddDays(Convert.ToInt32(result[i][CONSTANTS.RetrainingFrequencyInDays]));
                        if (DateTime.Now >= triggerDate)
                        {
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ConstraintsSchedularService), nameof(ModelAutoTrainForAPP), "SPA ModelAutoTrainForAPP START", string.IsNullOrEmpty(Convert.ToString(result[i][CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[i][CONSTANTS.CorrelationId])), "Ingrain Window Service", string.Empty, string.Empty, string.Empty, CONSTANTS.CustomConstraintsLog);
                            var logCollection = _database.GetCollection<SSAIWINSERVICEMODELS.InstaLog>("Insta_AutoLog");
                            var instaLog = new SSAIWINSERVICEMODELS.InstaLog(result[i]["CorrelationId"].ToString(), result[i]["ModelName"].ToString(), "IngestData", "Start", "", result[i]["CreatedByUser"].ToString());
                            logCollection.InsertOne(instaLog);

                            instaRetrain = new SSAIWINSERVICEMODELS.InstaRetrain();
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
                                instaRetrain.Status = CONSTANTS.E;
                                instaRetrain = _instaAutoRetrainService.GetInstaAutoDataEngineering(result[i]);
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(ConstraintsSchedularService), nameof(ModelAutoTrainForAPP), "SPA DATEENGINEERING END STATUS" + instaRetrain.Status + "MESSAGE" + instaRetrain.Message, string.IsNullOrEmpty(Convert.ToString(result[i][CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[i][CONSTANTS.CorrelationId])), "Ingrain Window Service", string.Empty, string.Empty, string.Empty, CONSTANTS.CustomConstraintsLog);
                            }
                            else
                            {
                                instaRetrain.Status = CONSTANTS.E;
                                instaLog = new SSAIWINSERVICEMODELS.InstaLog(result[i]["CorrelationId"].ToString(), result[i]["ModelName"].ToString(), "IngestData", "Start", instaRetrain.ErrorMessage + "-ERROR at Data Engineering", result[i]["CreatedByUser"].ToString());
                                logCollection.InsertOne(instaLog);
                            }
                            if (instaRetrain.Status == "C")
                            {
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(ConstraintsSchedularService), nameof(ModelAutoTrainForAPP), "STARTED", string.IsNullOrEmpty(Convert.ToString(result[i][CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[i][CONSTANTS.CorrelationId])), "Ingrain Window Service", string.Empty, string.Empty, string.Empty, CONSTANTS.CustomConstraintsLog);
                                instaRetrain.Status = CONSTANTS.E;
                                instaRetrain = _instaAutoRetrainService.GetInstaAutoModelEngineering(result[i]);
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(ConstraintsSchedularService), nameof(ModelAutoTrainForAPP), "SPA MODEL ENGINEERING END STATUS" + instaRetrain.Status, string.IsNullOrEmpty(Convert.ToString(result[i][CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[i][CONSTANTS.CorrelationId])), "Ingrain Window Service", string.Empty, string.Empty, string.Empty, CONSTANTS.CustomConstraintsLog);
                            }
                            else
                            {
                                instaRetrain.Status = CONSTANTS.E;
                                instaLog = new SSAIWINSERVICEMODELS.InstaLog(result[i]["CorrelationId"].ToString(), result[i]["ModelName"].ToString(), "IngestData", "Start", instaRetrain.ErrorMessage + "-ERROR at Model Engineering", result[i]["CreatedByUser"].ToString());
                                logCollection.InsertOne(instaLog);
                            }
                            if (instaRetrain.Status == "C")
                            {
                                _instaAutoRetrainService.GetSPADeployPrediction(result[i], string.Empty);
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(ConstraintsSchedularService), nameof(ModelAutoTrainForAPP), "SPA DEPLOY MODEL END" + instaRetrain.Status, string.IsNullOrEmpty(Convert.ToString(result[i][CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[i][CONSTANTS.CorrelationId])), "Ingrain Window Service", string.Empty, string.Empty, string.Empty, CONSTANTS.CustomConstraintsLog);
                            }
                            else
                            {
                                instaRetrain.Status = CONSTANTS.E;
                                instaLog = new SSAIWINSERVICEMODELS.InstaLog(result[i]["CorrelationId"].ToString(), result[i]["ModelName"].ToString(), "IngestData", "Start", instaRetrain.ErrorMessage + "-ERROR at Deploy", result[i]["CreatedByUser"].ToString());
                                logCollection.InsertOne(instaLog);
                            }
                        }
                    }
                }
            }
        }

        private void ModelRetrainOnAppNames(DATAMODELS.AppIntegration appResults, string appName)
        {
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
            List<BsonDocument> result = new List<BsonDocument>();
            var filterBuilder = Builders<BsonDocument>.Filter;
            FilterDefinition<BsonDocument> filter = null;
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
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(ConstraintsSchedularService), nameof(ModelRetrainOnAppNames), "SSAIReTrain END", "Ingrain Window Service", string.Empty, appResults.clientUId, appResults.deliveryConstructUID, CONSTANTS.CustomConstraintsLog);
                    }
                    catch (Exception ex)
                    {
                        LOGGING.LogManager.Logger.LogErrorMessage(typeof(ConstraintsSchedularService), nameof(ModelRetrainOnAppNames), ex.Message + ex.StackTrace, ex, "Ingrain Window Service", string.Empty, appResults.clientUId, appResults.deliveryConstructUID, CONSTANTS.CustomConstraintsLog);
                    }
                    break;
                case "Release Planner":
                    try
                    {
                        autoTrainedFeatures.FeatureName = "SSAIReTrain Release Planner";
                        autoTrainedFeatures.Sequence = 2;
                        autoTrainedFeatures.ModelsCount = 0;
                        autoTrainedFeatures.FunctionName = "SSAIReTrain Release Planner";
                        autoTrainedFeatures.StartedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                        autoTrainedFeatures.ModelList = new string[] { };
                        InsertAutoTrainLog(autoTrainedFeatures);
                        filter = filterBuilder.Eq(CONSTANTS.Status, CONSTANTS.Deployed) & filterBuilder.Eq(CONSTANTS.IsCarryOutRetraining, true) & filterBuilder.Eq("AppId", appResults.ApplicationID.Trim()) & filterBuilder.Eq("TemplateUsecaseId", "f0320924-2ee3-4398-ad7c-8bc172abd78d");
                        result = collection.Find(filter).ToList();


                        autoTrainedFeatures.ModelsCount = result.Count();
                        var CorrelationIds = result.Where(x => x["AppId"].ToString() == "9fe508f7-64bc-4f58-899b-78f349707efa").Select(y => y["CorrelationId"]);
                        autoTrainedFeatures.ModelList = BsonSerializer.Deserialize<string[]>(JsonConvert.SerializeObject(CorrelationIds));
                        autoTrainedFeatures.Sequence = 3;
                        autoTrainedFeatures.StartedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                        InsertAutoTrainLog(autoTrainedFeatures);
                        ModelAutoTrainForAPP(result, false);
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(ConstraintsSchedularService), nameof(ModelRetrainOnAppNames), "SSAIReTrain END", "Ingrain Window Service", string.Empty, appResults.clientUId, appResults.deliveryConstructUID, CONSTANTS.CustomConstraintsLog);
                    }
                    catch (Exception ex)
                    {
                        LOGGING.LogManager.Logger.LogErrorMessage(typeof(ConstraintsSchedularService), nameof(ModelRetrainOnAppNames), ex.Message + ex.StackTrace, ex, "Ingrain Window Service", string.Empty, appResults.clientUId, appResults.deliveryConstructUID, CONSTANTS.CustomConstraintsLog);
                    }
                    break;
                case "CMA":
                    try
                    {
                        autoTrainedFeatures.FeatureName = "SSAIReTrain CMA";
                        autoTrainedFeatures.Sequence = 2;
                        autoTrainedFeatures.ModelsCount = 0;
                        autoTrainedFeatures.FunctionName = "SSAIReTrain CMA";
                        autoTrainedFeatures.StartedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                        autoTrainedFeatures.ModelList = new string[] { };
                        InsertAutoTrainLog(autoTrainedFeatures);
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
                        LOGGING.LogManager.Logger.LogErrorMessage(typeof(ConstraintsSchedularService), nameof(ModelRetrainOnAppNames), ex.Message + ex.StackTrace, ex, "Ingrain Window Service", string.Empty, appResults.clientUId, appResults.deliveryConstructUID, CONSTANTS.CustomConstraintsLog);
                    }
                    break;
                case "SPA":
                case "SPA(Velocity)":
                    try
                    {
                        autoTrainedFeatures.FeatureName = "SSAIReTrain SPA";
                        autoTrainedFeatures.Sequence = 2;
                        autoTrainedFeatures.ModelsCount = 0;
                        autoTrainedFeatures.FunctionName = "SSAIReTrain SPA";
                        autoTrainedFeatures.StartedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                        autoTrainedFeatures.ModelList = new string[] { };
                        InsertAutoTrainLog(autoTrainedFeatures);
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
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(ConstraintsSchedularService), nameof(ModelRetrainOnAppNames), "SSAIReTrain END", "Ingrain Window Service", string.Empty, appResults.clientUId, appResults.deliveryConstructUID, CONSTANTS.CustomConstraintsLog);
                    }
                    catch (Exception ex)
                    {
                        LOGGING.LogManager.Logger.LogErrorMessage(typeof(ConstraintsSchedularService), nameof(ModelRetrainOnAppNames), ex.Message + ex.StackTrace, ex, "Ingrain Window Service", string.Empty, appResults.clientUId, appResults.deliveryConstructUID, CONSTANTS.CustomConstraintsLog);
                    }
                    break;
                case "VDS(SI)":
                    try
                    {
                        autoTrainedFeatures.FeatureName = "SSAIReTrain VDS(SI)";
                        autoTrainedFeatures.Sequence = 2;
                        autoTrainedFeatures.ModelsCount = 0;
                        autoTrainedFeatures.FunctionName = "SSAIReTrain VDS(SI)";
                        autoTrainedFeatures.StartedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                        autoTrainedFeatures.ModelList = new string[] { };
                        InsertAutoTrainLog(autoTrainedFeatures);
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
                        LOGGING.LogManager.Logger.LogErrorMessage(typeof(ConstraintsSchedularService), nameof(ModelRetrainOnAppNames), ex.Message + ex.StackTrace, ex, "Ingrain Window Service", string.Empty, appResults.clientUId, appResults.deliveryConstructUID, CONSTANTS.CustomConstraintsLog);
                    }
                    break;
            }
        }

        private void CMAAutoTrainForAPP(List<BsonDocument> result)
        {
            bool bTriggerRetraining = false;
            string correlationId = string.Empty;
            try
            {
                if (result.Count > 0)
                {
                    for (int i = 0; i < result.Count; i++)
                    {

                        // Check whether any Constraint present on not for Retraining
                        bTriggerRetraining = CheckIfRetrain(result[i], CONSTANTS.ReTrainingConstraint, CONSTANTS.SSAICustomContraints);

                        if (bTriggerRetraining)
                        {
                            string modifiedOn = result[i]["ModifiedOn"].ToString();
                            DateTime modifiedDate = DateTime.Parse(modifiedOn);
                            DateTime triggerDate = modifiedDate.AddDays(Convert.ToInt32(result[i][CONSTANTS.RetrainingFrequencyInDays]));

                            if (DateTime.Now >= triggerDate)
                            {
                                instaRetrain = new SSAIWINSERVICEMODELS.InstaRetrain();
                                correlationId = result[i][CONSTANTS.CorrelationId].ToString();
                                instaRetrain = _instaAutoRetrainService.IngestData(result[i]);
                                if (instaRetrain.Status == "C")
                                {
                                    instaRetrain = _instaAutoRetrainService.GetInstaAutoDataEngineering(result[i]);
                                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(ConstraintsSchedularService), nameof(CMAAutoTrainForAPP), "CMA DATEENGINEERING END STATUS" + instaRetrain.Status + "MESSAGE" + instaRetrain.Message, string.IsNullOrEmpty(Convert.ToString(result[i][CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[i][CONSTANTS.CorrelationId])), "Ingrain Window Service", string.Empty, string.Empty, string.Empty, CONSTANTS.CustomConstraintsLog);
                                }
                                if (instaRetrain.Status == "C")
                                {
                                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(ConstraintsSchedularService), nameof(CMAAutoTrainForAPP), "STARTED", string.IsNullOrEmpty(Convert.ToString(result[i][CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[i][CONSTANTS.CorrelationId])), "Ingrain Window Service", string.Empty, string.Empty, string.Empty, CONSTANTS.CustomConstraintsLog);
                                    instaRetrain = _instaAutoRetrainService.GetInstaAutoModelEngineering(result[i]);
                                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(ConstraintsSchedularService), nameof(CMAAutoTrainForAPP), "CMA MODEL ENGINEERING END STATUS" + instaRetrain.Status, string.IsNullOrEmpty(Convert.ToString(result[i][CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[i][CONSTANTS.CorrelationId])), "Ingrain Window Service", string.Empty, string.Empty, string.Empty, CONSTANTS.CustomConstraintsLog);
                                }
                                if (instaRetrain.Status == "C")
                                {
                                    _instaAutoRetrainService.GetInstaAutoDeployPrediction(result[i]);
                                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(ConstraintsSchedularService), nameof(CMAAutoTrainForAPP), "CMA DEPLOY MODEL END" + instaRetrain.Status, string.IsNullOrEmpty(Convert.ToString(result[i][CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[i][CONSTANTS.CorrelationId])), "Ingrain Window Service", string.Empty, string.Empty, string.Empty, CONSTANTS.CustomConstraintsLog);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ConstraintsSchedularService), nameof(CMAAutoTrainForAPP), ex.Message + ex.StackTrace, ex, "Ingrain Window Service", string.Empty, string.Empty, string.Empty, CONSTANTS.CustomConstraintsLog);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ConstraintsSchedularService), nameof(CMAAutoTrainForAPP), "CMA AUTORETRAIN END", default(Guid), "Ingrain Window Service", string.Empty, string.Empty, string.Empty, CONSTANTS.CustomConstraintsLog);
        }

        private void InsertAutoTrainLog(WINSERVICEMODELS.LogAutoTrainedFeatures autoTrainedFeatures)
        {
            var featuresLogCollection = _database.GetCollection<WINSERVICEMODELS.LogAutoTrainedFeatures>("SSAI_LogAutoTrainedFeatures");
            featuresLogCollection.InsertOne(autoTrainedFeatures);
        }

        #endregion

        #region AI ReTrain Methods
        public void AIServicesReTrain()
        {
            try
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(ConstraintsSchedularService), nameof(AIServicesReTrain), "AIServicesReTrain START", "Ingrain Window Service", string.Empty, string.Empty, string.Empty, CONSTANTS.CustomConstraintsLog);
                string aiServiceAutodays = string.Empty;
                if (!string.IsNullOrEmpty(appSettings.AIManualTrigger) && appSettings.AIManualTrigger.ToLower() == "yes")
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(ConstraintsSchedularService), nameof(AIServicesReTrain), "AI Service Manual Trigger started -" + DateTime.Now.ToString(), "Ingrain Window Service", string.Empty, string.Empty, string.Empty, CONSTANTS.CustomConstraintsLog);
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

                List<Service> Services = new List<Service>();
                var filterBuilder = Builders<BsonDocument>.Filter;
                var filter = filterBuilder.Empty;
                var result = _DBConnection.GetCollectionData<BsonDocument>(filter, "Services");
                if (result.Count > 0)
                {
                    Services = JsonConvert.DeserializeObject<List<Service>>(result.ToJson());
                }

                if (Services.Count > 0)
                {
                    for (int a = 0; a < Services.Count; a++)
                    {
                        switch (Services[a].ServiceCode)
                        {
                            case "SIMILARITYANALYTICS":

                                try
                                {
                                    var collection = _database.GetCollection<BsonDocument>(CONSTANTS.AI_CoreModels);
                                    //Online retrain models
                                    var AIfreqfilter = filterBuilder.Eq(CONSTANTS.ModelStatus, CONSTANTS.Completed) & filterBuilder.Eq(CONSTANTS.IsCarryOutRetraining, true) & (filterBuilder.Eq(CONSTANTS.DataSource, CONSTANTS.Entity) | filterBuilder.Eq(CONSTANTS.DataSource, "Phoenix") | filterBuilder.Eq(CONSTANTS.UsecaseId, "6665e35b-b2d1-40cc-b28f-3b795780f34f"));
                                    var AIServiceResult = collection.Find(AIfreqfilter).ToList();
                                    //  List<BsonDocument> AIServicefreqResult = new List<BsonDocument>();
                                    DateTime triggerDate = new DateTime();
                                    if (AIServiceResult.Count > 0)
                                    {
                                        for (int i = 0; i < AIServiceResult.Count; i++)
                                        {
                                            bool bTriggerRetraining = CheckIfRetrain(AIServiceResult[i], CONSTANTS.ReTrainingConstraint, CONSTANTS.AICustomContraints);
                                            if (bTriggerRetraining)
                                            {
                                                string modifiedOn = AIServiceResult[i]["ModifiedOn"].ToString();
                                                DateTime modifiedDate = DateTime.Parse(modifiedOn);
                                                triggerDate = modifiedDate.AddDays(Convert.ToInt32(AIServiceResult[i][CONSTANTS.RetrainingFrequencyInDays]));
                                                if (DateTime.Now >= triggerDate)
                                                {
                                                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(ConstraintsSchedularService), nameof(AIServicesReTrain), "SIMILARITYANALYTICS START :" + Services[a].ServiceCode, "Ingrain Window Service", string.Empty, string.Empty, string.Empty, CONSTANTS.CustomConstraintsLog);
                                                    bool IsSuccess = false;
                                                    DataModels.AICore.IngestData data = new DataModels.AICore.IngestData();
                                                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(ConstraintsSchedularService), nameof(AIServicesReTrain), "INGESTDATASTARTED" + AIServiceResult[i][CONSTANTS.CorrelationId].ToString(), "Ingrain Window Service", string.Empty, string.Empty, string.Empty, CONSTANTS.CustomConstraintsLog);
                                                    try { data = AIServices.IngestData(AIServiceResult[i][CONSTANTS.CorrelationId].ToString()); }
                                                    catch (Exception ex)
                                                    {
                                                        logAIServiceAutoTrain.CorrelationId = AIServiceResult[i][CONSTANTS.CorrelationId].ToString();
                                                        logAIServiceAutoTrain.ServiceId = Services[a].ServiceCode;
                                                        logAIServiceAutoTrain.PageInfo = CONSTANTS.IngestData;
                                                        logAIServiceAutoTrain.FunctionName = "AIServicesReTrain";
                                                        logAIServiceAutoTrain.ErrorMessage = ex.Message;
                                                        logAIServiceAutoTrain.EndedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                                                        InsertAIServiceAutoTrainLog(logAIServiceAutoTrain);
                                                    };
                                                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(ConstraintsSchedularService), nameof(AIServicesReTrain), "INGESTDATACOMPLETED" + data.IsIngestionCompleted, "Ingrain Window Service", string.Empty, string.Empty, string.Empty, CONSTANTS.CustomConstraintsLog);
                                                    if (data.IsIngestionCompleted & data.ServiceCode != CONSTANTS.DEVELOPERPREDICTION)
                                                    {
                                                        IsSuccess = false;
                                                        try
                                                        {
                                                            IsSuccess = AIServices.AIServicesTraining(Convert.ToString(AIServiceResult[i][CONSTANTS.CorrelationId]));
                                                        }
                                                        catch (Exception ex)
                                                        {
                                                            logAIServiceAutoTrain.CorrelationId = AIServiceResult[i][CONSTANTS.CorrelationId].ToString();
                                                            logAIServiceAutoTrain.ServiceId = Services[a].ServiceCode;
                                                            logAIServiceAutoTrain.PageInfo = CONSTANTS.IngestData;
                                                            logAIServiceAutoTrain.FunctionName = "AIServicesReTrain AIServicesTraining";
                                                            logAIServiceAutoTrain.ErrorMessage = ex.Message;
                                                            logAIServiceAutoTrain.EndedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                                                            InsertAIServiceAutoTrainLog(logAIServiceAutoTrain);
                                                        };
                                                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(ConstraintsSchedularService), nameof(AIServicesReTrain), "SIMILARITYANALYTICS END", "Ingrain Window Service", string.Empty, string.Empty, string.Empty, CONSTANTS.CustomConstraintsLog);
                                                    }
                                                }
                                            }
                                        }

                                    }

                                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(ConstraintsSchedularService), nameof(AIServicesReTrain), "Similarity Devloper Prediction Retrain end", "Ingrain Window Service", string.Empty, string.Empty, string.Empty, CONSTANTS.CustomConstraintsLog);
                                }
                                catch (Exception ex)
                                {
                                    LOGGING.LogManager.Logger.LogErrorMessage(typeof(ConstraintsSchedularService), nameof(AIServicesReTrain), ex.Message + ex.StackTrace, ex, "Ingrain Window Service", string.Empty, string.Empty, string.Empty, CONSTANTS.CustomConstraintsLog);
                                }
                                break;
                            case "DEVELOPERPREDICTION":
                                try
                                {
                                    var collection = _database.GetCollection<BsonDocument>(CONSTANTS.AI_CoreModels);
                                    var AIfilter = filterBuilder.Eq(CONSTANTS.ModelStatus, CONSTANTS.Completed) & filterBuilder.Lte(CONSTANTS.ModifiedOn, dateFilter) & filterBuilder.Eq(CONSTANTS.DataSource, "Phoenix") & filterBuilder.Eq("ServiceId", "28179a56-38b2-4d69-927d-0a6ba75da377");
                                    var AIServiceResult = collection.Find(AIfilter).ToList();
                                    //AI Services AutoTrain
                                    if (AIServiceResult.Count > 0)
                                    {
                                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(ConstraintsSchedularService), nameof(AIServicesReTrain), "DEVELOPERPREDICTION START", "Ingrain Window Service", string.Empty, string.Empty, string.Empty, CONSTANTS.CustomConstraintsLog);
                                        for (int i = 0; i < AIServiceResult.Count; i++)
                                        {
                                            bool bTriggerRetraining = CheckIfRetrain(AIServiceResult[i], CONSTANTS.ReTrainingConstraint, CONSTANTS.SSAICustomContraints);
                                            if (bTriggerRetraining)
                                            {
                                                bool IsSuccess = false;
                                                DataModels.AICore.IngestData data = new DataModels.AICore.IngestData();
                                                data = AIServices.IngestData(AIServiceResult[i][CONSTANTS.CorrelationId].ToString());
                                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(ConstraintsSchedularService), nameof(AIServicesReTrain), "INGESTDATACOMPLETED" + data.IsIngestionCompleted, "Ingrain Window Service", string.Empty, string.Empty, string.Empty, CONSTANTS.CustomConstraintsLog);
                                                if (data.IsIngestionCompleted & data.ServiceCode != CONSTANTS.DEVELOPERPREDICTION)
                                                {
                                                    IsSuccess = false;
                                                    IsSuccess = AIServices.AIServicesTraining(Convert.ToString(AIServiceResult[i][CONSTANTS.CorrelationId]));
                                                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(ConstraintsSchedularService), nameof(AIServicesReTrain), "DEVELOPERPREDICTION END", "Ingrain Window Service", string.Empty, string.Empty, string.Empty, CONSTANTS.CustomConstraintsLog);
                                                }
                                            }
                                        }
                                    }

                                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(ConstraintsSchedularService), nameof(AIServicesReTrain), "Similarity Devloper Prediction Retrain end", "Ingrain Window Service", string.Empty, string.Empty, string.Empty, CONSTANTS.CustomConstraintsLog);
                                }
                                catch (Exception ex)
                                {
                                    LOGGING.LogManager.Logger.LogErrorMessage(typeof(ConstraintsSchedularService), nameof(AIServicesReTrain), ex.Message + ex.StackTrace, ex, "Ingrain Window Service", string.Empty, string.Empty, string.Empty, CONSTANTS.CustomConstraintsLog);
                                }
                                break;
                            case "CLUSTERING":
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
                                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(ConstraintsSchedularService), nameof(AIServicesReTrain), "CLUSTERING START :" + Services[a].ServiceCode, "Ingrain Window Service", string.Empty, string.Empty, string.Empty, CONSTANTS.CustomConstraintsLog);
                                        for (int i = 0; i < ClusterModelsResult.Count; i++)
                                        {
                                            var corrId = ClusterModelsResult[i][CONSTANTS.CorrelationId].ToString();
                                            var ClusterIngestFilter = (ClusterfilterBuilder.Where(x => "DataSource" != CONSTANTS.file))
                                                                         & ClusterfilterBuilder.Eq(CONSTANTS.CorrelationId, corrId)
                                                                            & ClusterfilterBuilder.Eq(CONSTANTS.IsCarryOutRetraining, true);
                                            var ClusterIngestProjection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
                                            var ClusterIngestResult = ClusterIngestCollection.Find(ClusterIngestFilter).Project<BsonDocument>(ClusterIngestProjection).ToList();

                                            if (ClusterIngestResult.Count > 0)
                                            {
                                                for (int j = 0; j < ClusterIngestResult.Count; j++)
                                                {
                                                    bool bTriggerRetraining = CheckIfRetrain(ClusterIngestResult[j], CONSTANTS.ReTrainingConstraint, CONSTANTS.AICustomContraints);
                                                    if (bTriggerRetraining)
                                                    {
                                                        string modifiedOn = ClusterIngestResult[j]["ModifiedOn"].ToString();
                                                        DateTime modifiedDate = DateTime.Parse(modifiedOn);
                                                        triggerDate = modifiedDate.AddDays(Convert.ToInt32(ClusterIngestResult[j][CONSTANTS.RetrainingFrequencyInDays]));
                                                        if (DateTime.Now >= triggerDate)
                                                        {
                                                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ConstraintsSchedularService), nameof(AIServicesReTrain), "CLUSTER Ingest Read START", "Ingrain Window Service", string.Empty, string.Empty, string.Empty, CONSTANTS.CustomConstraintsLog);
                                                            bool IsSuccess = false;
                                                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ConstraintsSchedularService), nameof(AIServicesReTrain), "CLUSTER MODEL START" + Convert.ToString(ClusterIngestResult[j][CONSTANTS.CorrelationId]), "Ingrain Window Service", string.Empty, string.Empty, string.Empty, CONSTANTS.CustomConstraintsLog);
                                                            try { IsSuccess = AIServices.ClusterServicesTraining(ClusterIngestResult[j][CONSTANTS.CorrelationId].ToString()); }
                                                            catch (Exception ex)
                                                            {
                                                                logAIServiceAutoTrain.CorrelationId = ClusterIngestResult[j][CONSTANTS.CorrelationId].ToString();
                                                                logAIServiceAutoTrain.ServiceId = ClusterIngestResult[j][CONSTANTS.ServiceID].ToString();
                                                                logAIServiceAutoTrain.FunctionName = "AIServicesReTrain ClusterServicesTraining";
                                                                logAIServiceAutoTrain.ErrorMessage = ex.Message;
                                                                logAIServiceAutoTrain.EndedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                                                                InsertAIServiceAutoTrainLog(logAIServiceAutoTrain);
                                                            };
                                                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ConstraintsSchedularService), nameof(AIServicesReTrain), "CLUSTER MODEL END", "Ingrain Window Service", string.Empty, string.Empty, string.Empty, CONSTANTS.CustomConstraintsLog);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    LOGGING.LogManager.Logger.LogErrorMessage(typeof(ConstraintsSchedularService), nameof(AIServicesReTrain), ex.Message + ex.StackTrace, ex, "Ingrain Window Service", string.Empty, string.Empty, string.Empty, CONSTANTS.CustomConstraintsLog);
                                }
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ConstraintsSchedularService), nameof(AIServicesReTrain), ex.Message + ex.StackTrace, ex, "Ingrain Window Service", string.Empty, string.Empty, string.Empty, CONSTANTS.CustomConstraintsLog);
            }
        }

        private void InsertAIServiceAutoTrainLog(WINSERVICEMODELS.LogAIServiceAutoTrain logAIServiceAutoTrain)
        {
            var collection = _database.GetCollection<WINSERVICEMODELS.LogAIServiceAutoTrain>("AIServiceAutoTrainLog");
            collection.InsertOne(logAIServiceAutoTrain);
        }

        private bool CheckIfRetrain(BsonDocument item, string ReTrainingConstraint, string collectionName)
        {
            string ClientUID = string.Empty;
            string DCUID = string.Empty;
            bool bTriggerRetraining = false;

            foreach (var oNode in item.ToArray())
            {
                if (oNode.Name.ToUpper() == "CLIENTUID" || oNode.Name.ToUpper() == "CLIENTID")
                {
                    ClientUID = oNode.Value.ToString();

                }
                if (oNode.Name.ToUpper() == "DCUID" || oNode.Name.ToUpper() == "DELIVERYCONSTRUCTUID")
                {
                    DCUID = oNode.Value.ToString();
                }
            }

            // Check whether any Constraint present on not for Retraining
            //var oRetrainingConstraints = _DBConnection.GetCustomConstraints(result[i][CONSTANTS.CorrelationId].ToString(), CONSTANTS.ReTrainingConstraint, CONSTANTS.AICustomContraints);
            var oRetrainingConstraints = _DBConnection.GetCustomConstraints(item[CONSTANTS.CorrelationId].ToString(), ReTrainingConstraint, collectionName);

            if (oRetrainingConstraints == null)
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(ConstraintsSchedularService), nameof(CheckIfRetrain), "Template : '" + item["ModelName"].ToString() + "' - Constraints Not Found for Re-training", "Ingrain Window Service", "", "", "", CONSTANTS.CustomConstraintsLog);
                return false;
            }
            else
            {
                List<string> oRetrainingConstraintsList = JsonConvert.DeserializeObject<List<string>>(oRetrainingConstraints.ToJson());

                foreach (string ConstraintCode in oRetrainingConstraintsList)
                {
                    switch (ConstraintCode)
                    {
                        case "ClosedUserStory":
                            {
                                Dictionary<string, List<string>> oIterationList = _PhoenixhadoopConnection.FetchClosedUserStory(ClientUID, DCUID, ProvisionedAppServiceUID);
                                if (oIterationList != null && oIterationList.Count > 0)
                                {
                                    bTriggerRetraining = true;
                                }
                                break;
                            }
                        case "ClosedSprintnIteration":
                            {
                                Dictionary<string, List<string>> oIterationList = _PhoenixhadoopConnection.FetchClosedSprintandIterations(ClientUID, DCUID, ProvisionedAppServiceUID, CONSTANTS.ReTrainingConstraint);
                                if (oIterationList != null)
                                {
                                    bTriggerRetraining = true;
                                }
                                break;
                            }
                    }
                }
            }

            return bTriggerRetraining;
        }
        #endregion



        #region other PredictionMethods

        #region Active Release Methods
        public void UpdateNewIterationRecommendationsInPhoenix()
        {
            try
            {
                List<WINSERVICEMODELS.PhoenixIterations> oResponse = null;
                List<WINSERVICEMODELS.PhoenixIterations> phoenixIterations = null;
                oResponse = _DBConnection.GetNotRecommendedIterations();

                if (oResponse != null && oResponse.Count > 0)
                {
                    phoenixIterations = new List<WINSERVICEMODELS.PhoenixIterations>();
                    foreach (var oResult in oResponse)
                    {
                        var predFrequency = _DBConnection.CheckSPIDefectRateFrequency(oResult.TemplateUsecaseId);
                        int dateadded = predFrequency + 3;
                        DateTime pullTime = DateTime.UtcNow.AddDays(-dateadded);
                        if (oResult.LastRecordsPullUpdateTime > pullTime)
                        {
                            phoenixIterations.Add(oResult);
                        }
                    }

                    if (phoenixIterations != null && phoenixIterations.Count > 0)
                    {
                        //here we are pully a distinct list of DC and clientUID
                        var uniqueClientsDCs = phoenixIterations.Select(x => new
                        {
                            x.TrainedModelCorrelationID,
                            x.TemplateUsecaseId,
                            x.ClientUId,
                            x.DeliveryConstructUId,
                        }).Distinct().ToList();

                        foreach (var dc in uniqueClientsDCs)
                        {
                            //fetching list of iterationsUID from complete collection using client and DC uid 
                            var iterations = phoenixIterations.Where(x => x.ClientUId == dc.ClientUId && x.DeliveryConstructUId == dc.DeliveryConstructUId && x.TrainedModelCorrelationID == dc.TrainedModelCorrelationID && x.TemplateUsecaseId == dc.TemplateUsecaseId).Select(x => x.ItemUId).ToList();
                            int i = 0;
                            int skip = 0;
                            do
                            {
                                i = i + 50;
                                UpdateBulkIterationPredictions(dc.ClientUId, dc.DeliveryConstructUId, iterations.Skip(skip).Take(50).ToList(), dc.TrainedModelCorrelationID, dc.TemplateUsecaseId);
                                skip = i;
                            } while (i < iterations.Count);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ConstraintsSchedularService), nameof(UpdateNewIterationRecommendationsInPhoenix), ex.Message, ex, "Ingrain Window Service", string.Empty, string.Empty, string.Empty, CONSTANTS.CustomConstraintsLog);
            }
        }

        public bool UpdateBulkIterationPredictions(string clientUId, string deliveryConstructUId, List<string> iterationUId, string TrainedModelCorrelationID, string TemplateUsecaseId)
        {
            List<string> iterationUId_check = iterationUId;
            bool isSuccess = false;
            bool predResponse = false;
            try
            {
                List<WINSERVICEMODELS.PhoenixPayloads.IterationPredictions> predictions = new List<WINSERVICEMODELS.PhoenixPayloads.IterationPredictions>();

                var model = _DBConnection.CheckIfSSAIModelDeployed(clientUId, deliveryConstructUId, TemplateUsecaseId, PredictionSchedulerUserName);
                if (model != null)
                {
                    string response = _PhoenixhadoopConnection.SSAIBulkPredictionAPI(clientUId, deliveryConstructUId, TemplateUsecaseId, iterationUId); // pass as list
                    if (!string.IsNullOrEmpty(response))
                    {
                        string iterationid = string.Empty;
                        JObject result = JObject.Parse(response);
                        string correlationId = result["CorrelationId"].ToString();
                        string modelUniqueId = result["UniqueId"].ToString();
                        if (result["Status"].ToString() == "C")
                        {

                            JArray predictionResult = JArray.Parse(result["PredictedData"].ToString());
                            //loop throgh predictionresul as it will have mutiple iterationuid
                            for (int i = 0; i < predictionResult.Count; i++)
                            {

                                string predictedValue = predictionResult[i]["predictedValue"].ToString();
                                string uniqueId = predictionResult[i]["UniqueId"].ToString();
                                iterationid = Guid.Parse(uniqueId.Split("_")[1]).ToString();

                                //need to confirm below line of code 
                                var recommendationTypeUId = TemplateUsecaseId == IA_DefectRateUseCaseId ? RecommendationTypeUId_DefectRate : RecommendationTypeUId_SPI;

                                var newRecommendation = new WINSERVICEMODELS.PhoenixPayloads.IntelligentRecommendations();
                                newRecommendation.IntelligentRecommendationTypeUId = recommendationTypeUId;
                                newRecommendation.StateUId = DraftStateUId;

                                newRecommendation.IntelligentRecommendationAttributes = new List<WINSERVICEMODELS.PhoenixPayloads.IntelligentRecommendationAttributes>();

                                var recommendationAttributes1 = new WINSERVICEMODELS.PhoenixPayloads.IntelligentRecommendationAttributes
                                {
                                    FieldName = "IterationUId",
                                    FieldValue = iterationid //from resonse split to get this value
                                };
                                newRecommendation.IntelligentRecommendationAttributes.Add(recommendationAttributes1);

                                var recommendationAttributes2 = new WINSERVICEMODELS.PhoenixPayloads.IntelligentRecommendationAttributes
                                {
                                    FieldName = "PredictedValue",
                                    FieldValue = predictedValue
                                };
                                newRecommendation.IntelligentRecommendationAttributes.Add(recommendationAttributes2);


                                var prediction = new WINSERVICEMODELS.PhoenixPayloads.IterationPredictions
                                {
                                    IterationUid = iterationid, //from resonse split to get this value,
                                    UsecaseId = TemplateUsecaseId,
                                    IntelligentRecommendations = newRecommendation
                                };

                                predictions.Add(prediction);


                                //_DBConnection.LogInfoMessageToDB(clientUId, deliveryConstructUId, "", correlationId, modelUniqueId, IterationEntityUId, string.Join(",", iterationUId), "Prediction", "Response", null);

                            }

                        }
                        else
                        {
                            //_DBConnection.LogInfoMessageToDB(clientUId, deliveryConstructUId, "", correlationId, modelUniqueId, IterationEntityUId, string.Join(",", iterationUId), "Prediction", "Ingrain Prediction API response error -" + result["Message"].ToString(), null);
                        }

                    }
                    else
                    {
                        //  LogInfoMessageToDB(clientUId, deliveryConstructUId, "", IterationEntityUId, string.Join(",", iterationUId), "Prediction", "Null response from Ingrain Prediction API response error", null);
                    }
                }

                var uniqueIterations = predictions.Select(x => new
                {
                    x.IterationUid
                }).Distinct().ToList();

                if (uniqueIterations.Count > 0)
                {
                    var mergeIterationIdList = uniqueIterations.Select(i => i.IterationUid).ToList();
                    var excludedIterations = iterationUId_check.RemoveAll(itr => uniqueIterations.Any(a => a.IterationUid == itr));
                    JArray RecommendationsPayladList = new JArray();
                    string correlationUid = Guid.NewGuid().ToString();
                    for (int i = 0; i < uniqueIterations.Count; i++)
                    {

                        var uniteration = uniqueIterations[i].IterationUid;
                        var recommendations = predictions.Where(x => x.IterationUid == uniteration).Select(x => x.IntelligentRecommendations).ToList();

                        // LogInfoMessageToDB(clientUId, deliveryConstructUId, "", IterationEntityUId, uniteration, "Prediction", "Started Prediction", null);
                        string iterationPayload = _PhoenixhadoopConnection.GetEntityPayload(clientUId, deliveryConstructUId, "Iteration", uniteration);
                        if (string.IsNullOrEmpty(iterationPayload))
                        {
                            _DBConnection.UpdateIterationRecommendationStatusinDB(clientUId, deliveryConstructUId, uniteration, false, "Failed");
                            throw new InvalidDataException("No Data in Phoenix Get API");
                        }
                        dynamic iteration = JsonConvert.DeserializeObject<dynamic>(iterationPayload);
                        JObject entityRecommendation = new JObject();
                        bool newRec = true;

                        if (string.IsNullOrEmpty((string)iteration.Iterations[0].Recommendations))
                        {
                            entityRecommendation["CorrelationUId"] = correlationUid;
                            entityRecommendation["EntityUId"] = IterationEntityUId;
                            entityRecommendation["ItemUId"] = uniteration;
                            entityRecommendation["ItemExternalId"] = iteration.Iterations[0].IterationExternalId;
                            entityRecommendation["ProductInstanceUId"] = iteration.Iterations[0].CreatedByProductInstanceUId;
                            entityRecommendation["CreatedByApp"] = "myWizard.IngrAIn";
                            entityRecommendation["CreatedOn"] = DateTime.UtcNow.ToString("o");
                            entityRecommendation["ModifiedByApp"] = "myWizard.IngrAIn";
                            entityRecommendation["ModifiedOn"] = DateTime.UtcNow.ToString("o");
                            entityRecommendation["CreatedAtSourceOn"] = DateTime.UtcNow.ToString("o");
                            entityRecommendation["ModifiedAtSourceOn"] = DateTime.UtcNow.ToString("o");
                        }
                        else
                        {
                            newRec = false;
                            entityRecommendation = JObject.Parse(Base64Decode((string)iteration.Iterations[0].Recommendations));
                            entityRecommendation["ModifiedAtSourceOn"] = DateTime.UtcNow.ToString("o");
                            entityRecommendation["CorrelationUId"] = correlationUid;
                            entityRecommendation["ModifiedByApp"] = "myWizard.IngrAIn";
                        }

                        foreach (var item in recommendations)
                        {
                            DATAMODELS.CallBackErrorLog callBackErrorLog = new DATAMODELS.CallBackErrorLog
                            {
                                CorrelationId = uniteration,
                                RequestId = Guid.NewGuid().ToString(),
                                UseCaseId = TemplateUsecaseId,
                                ApplicationID = appSettings.IA_ApplicationId,
                                ApplicationName = "IA",
                                ClientId = clientUId,
                                DCID = deliveryConstructUId,
                                Message = "Before update Recommendations" + entityRecommendation.ToString(), //NotificationMessage
                                CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                                ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                                CreatedBy = "SYSTEM",
                                ModifiedBy = "SYSTEM"
                            };

                            var logCollection = _database.GetCollection<DATAMODELS.CallBackErrorLog>("AuditTrailLog");
                            logCollection.InsertOneAsync(callBackErrorLog);

                            if (!newRec)
                            {
                                var oldRecommendations = JArray.Parse(entityRecommendation["IntelligentRecommendations"].ToString());
                                if (oldRecommendations.Count > 0)
                                {
                                    //removing existing recommendation
                                    while (true)
                                    {
                                        JObject jo = oldRecommendations.Children<JObject>().FirstOrDefault(o => o["IntelligentRecommendationTypeUId"].ToString() == item.IntelligentRecommendationTypeUId && o["StateUId"].ToString() == "00904010-0001-0000-0000-000000000000");
                                        if (jo == null)
                                        {
                                            break;
                                        }
                                        else
                                        {
                                            //if acceppted stateUid - dont delte the existing recommendation
                                            if (jo["StateUId"].ToString() == "00904010-0001-0000-0000-000000000000")
                                            {
                                                oldRecommendations.Remove(jo);
                                            }
                                        }

                                    }
                                }

                                //add new recommendation
                                JObject recVal = JObject.Parse(JsonConvert.SerializeObject(item, Formatting.None));
                                oldRecommendations.Add(recVal);
                                entityRecommendation["IntelligentRecommendations"] = oldRecommendations;
                            }
                            else
                            {
                                JObject recVal = JObject.Parse(JsonConvert.SerializeObject(item, Formatting.None));
                                entityRecommendation["IntelligentRecommendations"] = new JArray() { recVal };
                                newRec = false;
                            }

                            //RecommendationsPayladList.Add(entityRecommendation);
                        }
                        RecommendationsPayladList.Add(entityRecommendation);
                        predResponse = true;
                    }

                    var payload = new
                    {
                        Recommendation = Base64Encode(JsonConvert.SerializeObject(RecommendationsPayladList, Formatting.None))
                    };

                    string res = string.Empty;

                    res = _PhoenixhadoopConnection.UpdateEntityRecommendationInPhoenix(payload, clientUId, deliveryConstructUId, correlationUid, "Iteration");

                    if (string.IsNullOrEmpty(res))
                    {
                        //LogErrorMessageToDB(clientUId, deliveryConstructUId, "", IterationEntityUId, string.Join(",", mergeIterationIdList), "Prediction", "Completed -" + res, null);
                        _DBConnection.UpdateBulkIterationRecommendationStatusinDB(clientUId, deliveryConstructUId, mergeIterationIdList, false, "Failed"); // bulk update
                    }
                    else
                    {
                        //LogInfoMessageToDB(clientUId, deliveryConstructUId, "", IterationEntityUId, string.Join(",", mergeIterationIdList), "Prediction", "Completed -" + res + "-Updated CorrelationId from -" + correlationUid + " -to- " + correlationUid, "C");
                        isSuccess = true;
                        _DBConnection.UpdateBulkIterationRecommendationStatusinDB(clientUId, deliveryConstructUId, mergeIterationIdList, true, "Success"); // bulk update
                    }

                    if (excludedIterations > 0)
                    {
                        //LogErrorMessageToDB(clientUId, deliveryConstructUId, "", IterationEntityUId, string.Join(",", iterationUId_check), "Prediction", "Completed -" + res, null);
                        _DBConnection.UpdateBulkIterationRecommendationStatusinDB(clientUId, deliveryConstructUId, iterationUId_check, false, "Failed"); // bulk update
                    }
                }
                else
                {
                    // LogErrorMessageToDB(clientUId, deliveryConstructUId, "", IterationEntityUId, string.Join(",", iterationUId), "Prediction", "No Response from python -", null);
                    _DBConnection.UpdateBulkIterationRecommendationStatusinDB(clientUId, deliveryConstructUId, iterationUId, false, "Failed"); // bulk update
                }

                #region Commented Code
                // var SSAIPredUsecaseIds = GetPredictionUsecaseWithConstraint();
                //foreach (var usecaseId in SSAIPredUsecaseIds)
                //{
                //    var model = _DBConnection.CheckIfSSAIModelDeployed(clientUId, deliveryConstructUId, usecaseId, PredictionSchedulerUserName);
                //    if (model != null)
                //    {
                //        string response = SSAIBulkPredictionAPI(clientUId, deliveryConstructUId, usecaseId, iterationUId); // pass as list
                //        if (!string.IsNullOrEmpty(response))
                //        {
                //            string iterationid = string.Empty;
                //            JObject result = JObject.Parse(response);
                //            string correlationId = result["CorrelationId"].ToString();
                //            string modelUniqueId = result["UniqueId"].ToString();
                //            if (result["Status"].ToString() == "C")
                //            {

                //                JArray predictionResult = JArray.Parse(result["PredictedData"].ToString());
                //                //loop throgh predictionresul as it will have mutiple iterationuid
                //                for (int i = 0; i < predictionResult.Count; i++)
                //                {

                //                    string predictedValue = predictionResult[i]["predictedValue"].ToString();
                //                    string uniqueId = predictionResult[i]["UniqueId"].ToString();
                //                    iterationid = Guid.Parse(uniqueId.Split("_")[1]).ToString();
                //                    var recommendationTypeUId = usecaseId == IA_DefectRateUseCaseId ? RecommendationTypeUId_DefectRate : RecommendationTypeUId_SPI;
                //                    var newRecommendation = new WINSERVICEMODELS.PhoenixPayloads.IntelligentRecommendations();
                //                    newRecommendation.IntelligentRecommendationTypeUId = recommendationTypeUId;
                //                    newRecommendation.StateUId = DraftStateUId;

                //                    newRecommendation.IntelligentRecommendationAttributes = new List<WINSERVICEMODELS.PhoenixPayloads.IntelligentRecommendationAttributes>();

                //                    var recommendationAttributes1 = new WINSERVICEMODELS.PhoenixPayloads.IntelligentRecommendationAttributes
                //                    {
                //                        FieldName = "IterationUId",
                //                        FieldValue = iterationid //from resonse split to get this value
                //                    };
                //                    newRecommendation.IntelligentRecommendationAttributes.Add(recommendationAttributes1);

                //                    var recommendationAttributes2 = new WINSERVICEMODELS.PhoenixPayloads.IntelligentRecommendationAttributes
                //                    {
                //                        FieldName = "PredictedValue",
                //                        FieldValue = predictedValue
                //                    };
                //                    newRecommendation.IntelligentRecommendationAttributes.Add(recommendationAttributes2);


                //                    var prediction = new WINSERVICEMODELS.PhoenixPayloads.IterationPredictions
                //                    {
                //                        IterationUid = iterationid, //from resonse split to get this value,
                //                        UsecaseId = usecaseId,
                //                        IntelligentRecommendations = newRecommendation
                //                    };

                //                    predictions.Add(prediction);


                //                    LogInfoMessageToDB(clientUId, deliveryConstructUId, "", correlationId, modelUniqueId, IterationEntityUId, string.Join(",", iterationUId), "Prediction", "Response", null);

                //                }

                //            }
                //            else
                //            {
                //                LogInfoMessageToDB(clientUId, deliveryConstructUId, "", correlationId, modelUniqueId, IterationEntityUId, string.Join(",", iterationUId), "Prediction", "Ingrain Prediction API response error -" + result["Message"].ToString(), null);
                //            }

                //        }
                //        else
                //        {
                //            LogInfoMessageToDB(clientUId, deliveryConstructUId, "", IterationEntityUId, string.Join(",", iterationUId), "Prediction", "Null response from Ingrain Prediction API response error", null);
                //        }
                //    }




                //    //

                //    var uniqueIterations = predictions.Select(x => new
                //    {
                //        x.IterationUid
                //    }).Distinct().ToList();
                //    if (uniqueIterations.Count > 0)
                //    {
                //        var mergeIterationIdList = uniqueIterations.Select(i => i.IterationUid).ToList();
                //        var excludedIterations = iterationUId_check.RemoveAll(itr => uniqueIterations.Any(a => a.IterationUid == itr));
                //        JArray RecommendationsPayladList = new JArray();
                //        string correlationUid = Guid.NewGuid().ToString();
                //        for (int i = 0; i < uniqueIterations.Count; i++)
                //        {

                //            var uniteration = uniqueIterations[i].IterationUid;
                //            var recommendations = predictions.Where(x => x.IterationUid == uniteration).Select(x => x.IntelligentRecommendations).ToList();



                //            LogInfoMessageToDB(clientUId, deliveryConstructUId, "", IterationEntityUId, uniteration, "Prediction", "Started Prediction", null);
                //            string iterationPayload = GetEntityPayload(clientUId, deliveryConstructUId, "Iteration", uniteration);
                //            if (string.IsNullOrEmpty(iterationPayload))
                //            {
                //                UpdateIterationRecommendationStatusinDB(clientUId, deliveryConstructUId, uniteration, false, "Failed");
                //                throw new InvalidDataException("No Data in Phoenix Get API");
                //            }
                //            dynamic iteration = JsonConvert.DeserializeObject<dynamic>(iterationPayload);
                //            JObject entityRecommendation = new JObject();
                //            bool newRec = true;

                //            if (string.IsNullOrEmpty((string)iteration.Iterations[0].Recommendations))
                //            {
                //                entityRecommendation["CorrelationUId"] = correlationUid;
                //                entityRecommendation["EntityUId"] = IterationEntityUId;
                //                entityRecommendation["ItemUId"] = uniteration;
                //                entityRecommendation["ItemExternalId"] = iteration.Iterations[0].IterationExternalId;
                //                entityRecommendation["ProductInstanceUId"] = iteration.Iterations[0].CreatedByProductInstanceUId;
                //                entityRecommendation["CreatedByApp"] = "myWizard.IngrAIn";
                //                entityRecommendation["CreatedOn"] = DateTime.UtcNow.ToString("o");
                //                entityRecommendation["ModifiedByApp"] = "myWizard.IngrAIn";
                //                entityRecommendation["ModifiedOn"] = DateTime.UtcNow.ToString("o");
                //                entityRecommendation["CreatedAtSourceOn"] = DateTime.UtcNow.ToString("o");
                //                entityRecommendation["ModifiedAtSourceOn"] = DateTime.UtcNow.ToString("o");
                //            }
                //            else
                //            {
                //                newRec = false;
                //                entityRecommendation = JObject.Parse(Base64Decode((string)iteration.Iterations[0].Recommendations));
                //                entityRecommendation["ModifiedAtSourceOn"] = DateTime.UtcNow.ToString("o");
                //                entityRecommendation["CorrelationUId"] = correlationUid;
                //                entityRecommendation["ModifiedByApp"] = "myWizard.IngrAIn";
                //            }

                //            foreach (var item in recommendations)
                //            {
                //                DATAMODELS.CallBackErrorLog callBackErrorLog = new DATAMODELS.CallBackErrorLog
                //                {
                //                    CorrelationId = uniteration,
                //                    RequestId = Guid.NewGuid().ToString(),
                //                    UseCaseId = usecaseId,
                //                    ApplicationID = appSettings.IA_ApplicationId,
                //                    ApplicationName = "IA",
                //                    ClientId = clientUId,
                //                    DCID = deliveryConstructUId,
                //                    Message = "Before update Recommendations" + entityRecommendation.ToString(), //NotificationMessage
                //                    CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                //                    ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                //                    CreatedBy = "SYSTEM",
                //                    ModifiedBy = "SYSTEM"
                //                };

                //                var logCollection = _database.GetCollection<DATAMODELS.CallBackErrorLog>("AuditTrailLog");
                //                logCollection.InsertOneAsync(callBackErrorLog);

                //                if (!newRec)
                //                {
                //                    var oldRecommendations = JArray.Parse(entityRecommendation["IntelligentRecommendations"].ToString());
                //                    if (oldRecommendations.Count > 0)
                //                    {
                //                        //removing existing recommendation
                //                        while (true)
                //                        {
                //                            JObject jo = oldRecommendations.Children<JObject>().FirstOrDefault(o => o["IntelligentRecommendationTypeUId"].ToString() == item.IntelligentRecommendationTypeUId && o["StateUId"].ToString() == "00904010-0001-0000-0000-000000000000");
                //                            if (jo == null)
                //                            {
                //                                break;
                //                            }
                //                            else
                //                            {
                //                                //if acceppted stateUid - dont delte the existing recommendation
                //                                if (jo["StateUId"].ToString() == "00904010-0001-0000-0000-000000000000")
                //                                {
                //                                    oldRecommendations.Remove(jo);
                //                                }
                //                            }

                //                        }
                //                    }

                //                    //add new recommendation
                //                    JObject recVal = JObject.Parse(JsonConvert.SerializeObject(item, Formatting.None));
                //                    oldRecommendations.Add(recVal);
                //                    entityRecommendation["IntelligentRecommendations"] = oldRecommendations;
                //                }
                //                else
                //                {
                //                    JObject recVal = JObject.Parse(JsonConvert.SerializeObject(item, Formatting.None));
                //                    entityRecommendation["IntelligentRecommendations"] = new JArray() { recVal };
                //                    newRec = false;
                //                }

                //                //RecommendationsPayladList.Add(entityRecommendation);
                //            }
                //            RecommendationsPayladList.Add(entityRecommendation);
                //            predResponse = true;
                //        }

                //        //string iterationPayloadNew = GetEntityPayload(clientUId, deliveryConstructUId, "Iteration", iterationUId);
                //        //if (string.IsNullOrEmpty(iterationPayloadNew))
                //        //{
                //        //    UpdateIterationRecommendationStatusinDB(clientUId, deliveryConstructUId, iterationUId, false, "Failed");
                //        //    throw new InvalidDataException("No Data in Phoenix Get API");
                //        //}
                //        //dynamic iterationNew = JsonConvert.DeserializeObject<dynamic>(iterationPayloadNew);
                //        var payload = new
                //        {
                //            Recommendation = Base64Encode(JsonConvert.SerializeObject(RecommendationsPayladList, Formatting.None))
                //        };
                //        //    iterationNew.Iterations[0].Recommendations = Base64Encode(JsonConvert.SerializeObject(entityRecommendation, Formatting.None));
                //        //    iterationNew.Iterations[0].ModifiedAtSourceOn = DateTime.UtcNow.ToString("o");
                //        //    iterationNew.Iterations[0].ModifiedByApp = "myWizard.IngrAIn";
                //        //    string oldCorrelationUId = iterationNew.Iterations[0].CorrelationUId;
                //        //iterationNew.Iterations[0].CorrelationUId = Guid.NewGuid().ToString();
                //        //string correlationUid = entityRecommendation["CorrelationUId"].ToString();
                //        string res = string.Empty;

                //        //if (predResponse)
                //        //{
                //        //res = UpdateEntityRecommendationInPhoenix(iterationNew.Iterations, clientUId, deliveryConstructUId, (string)iterationNew.Iterations[0].CorrelationUId, "Iteration");
                //        res = UpdateEntityRecommendationInPhoenix(payload, clientUId, deliveryConstructUId, correlationUid, "Iteration");

                //        // }

                //        if (string.IsNullOrEmpty(res))
                //        {
                //            LogErrorMessageToDB(clientUId, deliveryConstructUId, "", IterationEntityUId, string.Join(",", mergeIterationIdList), "Prediction", "Completed -" + res, null);
                //            UpdateBulkIterationRecommendationStatusinDB(clientUId, deliveryConstructUId, mergeIterationIdList, false, "Failed"); // bulk update
                //        }
                //        else
                //        {
                //            LogInfoMessageToDB(clientUId, deliveryConstructUId, "", IterationEntityUId, string.Join(",", mergeIterationIdList), "Prediction", "Completed -" + res + "-Updated CorrelationId from -" + correlationUid + " -to- " + correlationUid, "C");
                //            isSuccess = true;
                //            UpdateBulkIterationRecommendationStatusinDB(clientUId, deliveryConstructUId, mergeIterationIdList, true, "Success"); // bulk update
                //        }

                //        if (excludedIterations > 0)
                //        {
                //            LogErrorMessageToDB(clientUId, deliveryConstructUId, "", IterationEntityUId, string.Join(",", iterationUId_check), "Prediction", "Completed -" + res, null);
                //            UpdateBulkIterationRecommendationStatusinDB(clientUId, deliveryConstructUId, iterationUId_check, false, "Failed"); // bulk update
                //        }
                //    }
                //    else
                //    {
                //        LogErrorMessageToDB(clientUId, deliveryConstructUId, "", IterationEntityUId, string.Join(",", iterationUId), "Prediction", "No Response from python -", null);
                //        UpdateBulkIterationRecommendationStatusinDB(clientUId, deliveryConstructUId, iterationUId, false, "Failed"); // bulk update
                //    }

                //}

                #endregion CommentedCode
            }
            catch (Exception ex)
            {
                //update for all iterationUid loop
                _DBConnection.UpdateBulkIterationRecommendationStatusinDB(clientUId, deliveryConstructUId, iterationUId, false, "Failed"); // need to check // bulk update
                                                                                                                                           //  LogErrorMessageToDB(clientUId, deliveryConstructUId, "", string.Join(",", iterationUId), string.Join(",", iterationUId), "Prediction", ex.ToString(), null);
            }
            return isSuccess;
        }

        public string Base64Encode(string text)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(text);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        public string Base64Decode(string base64Data)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64Data);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }

        #endregion Active Release Methods


        #region Change Request
        public void StartENSCRPredictions()
        {

            List<DATAMODELS.ENSEntityNotificationLog> entityNotificationLogs = _DBConnection.ReadCRENSNotification();
            if (entityNotificationLogs.Count > 0)
            {
                UpdatePrediction(entityNotificationLogs);
            }

        }

        public void UpdatePrediction(List<DATAMODELS.ENSEntityNotificationLog> entityNotificationLogs)
        {
            foreach (var log in entityNotificationLogs)
            {
                try
                {
                    bool resp = false;
                    // for Iteration / Release - not in use as Iteration flow s changed wih scheduler  phenix iteration
                    if (log.EntityUId == IterationEntityUId)
                    {
                        string ensPayload = GetENSPayload(log);
                        if (!string.IsNullOrEmpty(ensPayload))
                        {
                            List<WINSERVICEMODELS.PhoenixPayloads.ENSIteration> eNSIteration = JsonConvert.DeserializeObject<List<WINSERVICEMODELS.PhoenixPayloads.ENSIteration>>(ensPayload);

                            foreach (var iteration in eNSIteration)
                            {
                                if (iteration.IterationDeliveryConstructs != null)
                                {
                                    if (_DBConnection.CheckSSAITrainedModels(log.ClientUId, log.DeliveryConstructUId, log.TemplateUseCaseId))
                                    {
                                        resp = UpdateIterationPredictions(log.ClientUId, log.DeliveryConstructUId, iteration.IterationUId, log.TemplateUseCaseId);
                                    }
                                }
                            }

                            if (resp)
                            {
                                _DBConnection.UpdateNotificationStatus(log._id, true, "C", "Successfully Processed");

                            }
                            else
                            {
                                _DBConnection.UpdateNotificationStatus(log._id, true, "E", "Error in prediction");
                            }
                        }
                        else
                        {
                            _DBConnection.UpdateNotificationStatus(log._id, false, "E", "ENS callback payload is null");
                        }

                    }
                    else if (log.EntityUId == CREntityUId)
                    {
                        //callback url  - multiple crs
                        string ensPayload = GetENSPayload(log);
                        if (!string.IsNullOrEmpty(ensPayload))
                        {
                            List<WINSERVICEMODELS.PhoenixPayloads.ENSChangeRequest> eNSChangeRequest = JsonConvert.DeserializeObject<List<WINSERVICEMODELS.PhoenixPayloads.ENSChangeRequest>>(ensPayload);
                            foreach (var cr in eNSChangeRequest)
                            {
                                if (cr.ChangeRequestDeliveryConstructs != null)
                                {
                                    if (_DBConnection.CheckAITrainedModels(log.ClientUId, log.DeliveryConstructUId, log.TemplateUseCaseId))
                                    {
                                        //query api to phoenix - full payload
                                        resp = UpdateChangeRequestPredictions(log, log.DeliveryConstructUId, cr.ChangeRequestUId);
                                    }
                                }


                            }
                            if (resp)
                            {
                                _DBConnection.UpdateNotificationStatus(log._id, true, "C", "Successfully Processed");

                            }
                            else
                            {
                                _DBConnection.UpdateNotificationStatus(log._id, true, "E", "Error in prediction");
                            }

                        }
                        else
                        {
                            _DBConnection.UpdateNotificationStatus(log._id, false, "E", "ENS callback payload is null");
                        }
                    }
                }
                catch (Exception ex)
                {
                    LOGGING.LogManager.Logger.LogErrorMessage(typeof(ConstraintsSchedularService), nameof(UpdatePrediction), ex.Message, ex, "Ingrain Window Service", string.Empty, string.Empty, string.Empty, CONSTANTS.CustomConstraintsLog);
                    _DBConnection.UpdateNotificationStatus(log._id, false, "E", ex.Message);
                }
            }
        }

        public bool UpdateChangeRequestPredictions(DATAMODELS.ENSEntityNotificationLog log, string deliveryConstructUId, string changeRequestUId)
        {
            bool isSuccess = false;
            bool predResponse = false;
            try
            {
                //  LogInfoMessageToDB(clientUId, deliveryConstructUId, "", CREntityUId, changeRequestUId, "Prediction", "Started Prediction", null);
                //query api to phoenix - full payload
                string crPayload = _PhoenixhadoopConnection.GetEntityPayload(log.ClientUId, deliveryConstructUId, "ChangeRequest", changeRequestUId);
                if (string.IsNullOrEmpty(crPayload))
                {
                    throw new InvalidDataException("No Data in Phoenix Get API");
                }

                dynamic changeRequest = JsonConvert.DeserializeObject<dynamic>(crPayload);

                JObject entityRecommendation = new JObject();
                bool newRec = true;
                if (changeRequest != null && changeRequest.ChangeRequests != null)
                {
                    if (string.IsNullOrEmpty((string)changeRequest.ChangeRequests[0].Recommendations))
                    {
                        entityRecommendation["CorrelationUId"] = Guid.NewGuid().ToString();
                        entityRecommendation["EntityUId"] = CREntityUId;
                        entityRecommendation["ItemUId"] = changeRequestUId;
                        entityRecommendation["ItemExternalId"] = changeRequest.ChangeRequests[0].ChangeRequestExternalId;
                        entityRecommendation["ProductInstanceUId"] = changeRequest.ChangeRequests[0].CreatedByProductInstanceUId;
                        entityRecommendation["CreatedByApp"] = "myWizard.IngrAIn";
                        entityRecommendation["CreatedOn"] = DateTime.UtcNow.ToString("o");
                        entityRecommendation["ModifiedByApp"] = "myWizard.IngrAIn";
                        entityRecommendation["ModifiedOn"] = DateTime.UtcNow.ToString("o");
                        entityRecommendation["CreatedAtSourceOn"] = DateTime.UtcNow.ToString("o");
                        entityRecommendation["ModifiedAtSourceOn"] = DateTime.UtcNow.ToString("o");
                    }
                    else
                    {
                        newRec = false;
                        entityRecommendation["CorrelationUId"] = Guid.NewGuid().ToString();
                        entityRecommendation = JObject.Parse(Base64Decode((string)changeRequest.ChangeRequests[0].Recommendations));
                        entityRecommendation["ModifiedAtSourceOn"] = DateTime.UtcNow.ToString("o");
                    }


                    //foreach (var usecaseId in AIUseCaseIds)
                    //{
                    DATAMODELS.CallBackErrorLog callBackErrorLog = new DATAMODELS.CallBackErrorLog
                    {
                        CorrelationId = changeRequestUId,
                        RequestId = Guid.NewGuid().ToString(),
                        UseCaseId = log.TemplateUseCaseId,
                        ApplicationID = appSettings.IA_ApplicationId,
                        ApplicationName = "IA",
                        ClientId = log.ClientUId,
                        DCID = deliveryConstructUId,
                        Message = "Before update Recommendations" + entityRecommendation.ToString(), //NotificationMessage
                        CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                        ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                        CreatedBy = "SYSTEM",
                        ModifiedBy = "SYSTEM"
                    };

                    var logCollection = _database.GetCollection<DATAMODELS.CallBackErrorLog>("AuditTrailLog");

                    logCollection.InsertOneAsync(callBackErrorLog);
                    JObject predictionInputData = new JObject();
                    string recommendationTypeUId = null;
                    if (log.TemplateUseCaseId == IA_CRUseCaseId)
                    {
                        predictionInputData["changerequestuid"] = changeRequest.ChangeRequests[0].ChangeRequestUId;
                        predictionInputData["title"] = changeRequest.ChangeRequests[0].Title;
                        predictionInputData["description"] = changeRequest.ChangeRequests[0].Description;
                        recommendationTypeUId = RecommendationTypeUId_SimilarCR;
                    }

                    if (log.TemplateUseCaseId == IA_UserStoryUseCaseId)
                    {
                        predictionInputData["Title"] = changeRequest.ChangeRequests[0].Title;
                        predictionInputData["Description"] = changeRequest.ChangeRequests[0].Description;
                        recommendationTypeUId = RecommendationTypeUId_SimilarUserStory;
                    }

                    if (log.TemplateUseCaseId == IA_RequirementUseCaseId)
                    {
                        predictionInputData["title"] = changeRequest.ChangeRequests[0].Title;
                        predictionInputData["description"] = changeRequest.ChangeRequests[0].Description;
                        recommendationTypeUId = RecommendationTypeUId_SimilarRequirement;
                    }


                    // Get prediction results
                    string response = _PhoenixhadoopConnection.AIServicePredictionAPI(predictionInputData, log.ClientUId, deliveryConstructUId, log.TemplateUseCaseId, log.ServiceID);


                    // Build recommendation attribute
                    if (!string.IsNullOrEmpty(response))
                    {
                        JObject result = JObject.Parse(response);
                        if (result != null && (bool)result["IsSuccess"] == true)
                        {
                            // LogInfoMessageToDB(clientUId, deliveryConstructUId, "", CREntityUId, changeRequestUId, "Prediction", "Prediction Response - Success", null);
                            if (result["ReturnValue"]["ResponseData"]["status"].ToString() == "True")
                            {
                                JArray predictions = JArray.Parse(result["ReturnValue"]["ResponseData"]["Predictions"].ToString());

                                if (!newRec)
                                {
                                    var oldRecommendations = JArray.Parse(entityRecommendation["IntelligentRecommendations"].ToString());
                                    if (oldRecommendations.Count > 0)
                                    {
                                        //removing existing recommendation
                                        while (true)
                                        {
                                            JObject jo = oldRecommendations.Children<JObject>().FirstOrDefault(o => o["IntelligentRecommendationTypeUId"].ToString() == recommendationTypeUId && o["StateUId"].ToString() == "00904010-0001-0000-0000-000000000000");
                                            if (jo == null)
                                            {
                                                break;
                                            }
                                            else
                                            {
                                                //if acceppted/rejected stateUid - dont delte the existing recommendation
                                                if (jo["StateUId"].ToString() == "00904010-0001-0000-0000-000000000000")
                                                {
                                                    oldRecommendations.Remove(jo);
                                                }
                                            }

                                        }
                                        entityRecommendation["IntelligentRecommendations"] = oldRecommendations;
                                    }

                                }
                                else
                                {
                                    entityRecommendation["IntelligentRecommendations"] = new JArray();
                                    newRec = false;
                                }

                                var newRecommendations = JArray.Parse(entityRecommendation["IntelligentRecommendations"].ToString());
                                for (int i = 1; i < predictions.Count; i++)
                                {
                                    bool addRecommendation = false;
                                    var newRecommendation = new WINSERVICEMODELS.PhoenixPayloads.IntelligentRecommendations();
                                    newRecommendation.StateUId = DraftStateUId;
                                    newRecommendation.IntelligentRecommendationAttributes = new List<WINSERVICEMODELS.PhoenixPayloads.IntelligentRecommendationAttributes>();
                                    foreach (var item in predictions[i])
                                    {
                                        JProperty property = item as JProperty;
                                        if (property != null)
                                        {
                                            if (log.TemplateUseCaseId == IA_CRUseCaseId && predictions[i]["changerequestuid"].ToString() != "Not found")
                                            {
                                                addRecommendation = true;
                                                newRecommendation.IntelligentRecommendationTypeUId = RecommendationTypeUId_SimilarCR;
                                                if (property.Name.ToUpper().Contains("CHANGEREQUESTUID"))
                                                {
                                                    var recommendationAttributes1 = new WINSERVICEMODELS.PhoenixPayloads.IntelligentRecommendationAttributes
                                                    {
                                                        FieldName = "ChangeRequestUId",
                                                        FieldValue = Guid.Parse(predictions[i][property.Name].ToString()).ToString()
                                                    };
                                                    newRecommendation.IntelligentRecommendationAttributes.Add(recommendationAttributes1);
                                                }

                                                if (property.Name.ToUpper().Contains("TITLE"))
                                                {
                                                    var recommendationAttributes2 = new WINSERVICEMODELS.PhoenixPayloads.IntelligentRecommendationAttributes
                                                    {
                                                        FieldName = "Title",
                                                        FieldValue = predictions[i]["title"].ToString()
                                                    };
                                                    newRecommendation.IntelligentRecommendationAttributes.Add(recommendationAttributes2);
                                                }

                                                if (property.Name.ToUpper().Contains("DESCRIPTION"))
                                                {
                                                    var recommendationAttributes3 = new WINSERVICEMODELS.PhoenixPayloads.IntelligentRecommendationAttributes
                                                    {
                                                        FieldName = "Description",
                                                        FieldValue = predictions[i][property.Name].ToString()
                                                    };
                                                    newRecommendation.IntelligentRecommendationAttributes.Add(recommendationAttributes3);
                                                }

                                                if (property.Name.ToUpper().Contains("SCORE"))
                                                {
                                                    var recommendationAttributes4 = new WINSERVICEMODELS.PhoenixPayloads.IntelligentRecommendationAttributes
                                                    {
                                                        FieldName = "Score",
                                                        FieldValue = predictions[i][property.Name].ToString()
                                                    };
                                                    newRecommendation.IntelligentRecommendationAttributes.Add(recommendationAttributes4);
                                                }
                                            }

                                            if (log.TemplateUseCaseId == IA_UserStoryUseCaseId && predictions[i]["workitemuid"].ToString() != "Not found")
                                            {
                                                addRecommendation = true;
                                                newRecommendation.IntelligentRecommendationTypeUId = RecommendationTypeUId_SimilarUserStory;
                                                if (property.Name.ToUpper().Contains("WORKITEMUID"))
                                                {
                                                    var recommendationAttributes1 = new WINSERVICEMODELS.PhoenixPayloads.IntelligentRecommendationAttributes
                                                    {
                                                        FieldName = "WorkItemUId",
                                                        FieldValue = Guid.Parse(predictions[i][property.Name].ToString()).ToString()
                                                    };
                                                    newRecommendation.IntelligentRecommendationAttributes.Add(recommendationAttributes1);
                                                }


                                                if (property.Name.ToUpper().Contains("TITLE"))
                                                {
                                                    var recommendationAttributes2 = new WINSERVICEMODELS.PhoenixPayloads.IntelligentRecommendationAttributes
                                                    {
                                                        FieldName = "Title",
                                                        FieldValue = predictions[i][property.Name].ToString()
                                                    };
                                                    newRecommendation.IntelligentRecommendationAttributes.Add(recommendationAttributes2);
                                                }

                                                if (property.Name.ToUpper().Contains("DESCRIPTION"))
                                                {
                                                    var recommendationAttributes3 = new WINSERVICEMODELS.PhoenixPayloads.IntelligentRecommendationAttributes
                                                    {
                                                        FieldName = "Description",
                                                        FieldValue = predictions[i][property.Name].ToString()
                                                    };
                                                    newRecommendation.IntelligentRecommendationAttributes.Add(recommendationAttributes3);
                                                }

                                                if (property.Name.ToUpper().Contains("SCORE"))
                                                {
                                                    var recommendationAttributes4 = new WINSERVICEMODELS.PhoenixPayloads.IntelligentRecommendationAttributes
                                                    {
                                                        FieldName = "Score",
                                                        FieldValue = predictions[i][property.Name].ToString()
                                                    };

                                                    newRecommendation.IntelligentRecommendationAttributes.Add(recommendationAttributes4);
                                                }
                                            }

                                            if (log.TemplateUseCaseId == IA_RequirementUseCaseId && predictions[i]["requirementuid"].ToString() != "Not found")
                                            {
                                                addRecommendation = true;
                                                newRecommendation.IntelligentRecommendationTypeUId = RecommendationTypeUId_SimilarRequirement;
                                                if (property.Name.ToUpper().Contains("REQUIREMENTUID"))
                                                {
                                                    var recommendationAttributes1 = new WINSERVICEMODELS.PhoenixPayloads.IntelligentRecommendationAttributes
                                                    {
                                                        FieldName = "RequirementUId",
                                                        FieldValue = Guid.Parse(predictions[i][property.Name].ToString()).ToString()
                                                    };
                                                    newRecommendation.IntelligentRecommendationAttributes.Add(recommendationAttributes1);
                                                }

                                                if (property.Name.ToUpper().Contains("TITLE"))
                                                {
                                                    var recommendationAttributes2 = new WINSERVICEMODELS.PhoenixPayloads.IntelligentRecommendationAttributes
                                                    {
                                                        FieldName = "Title",
                                                        FieldValue = predictions[i][property.Name].ToString()
                                                    };
                                                    newRecommendation.IntelligentRecommendationAttributes.Add(recommendationAttributes2);
                                                }

                                                if (property.Name.ToUpper().Contains("DESCRIPTION"))
                                                {
                                                    var recommendationAttributes3 = new WINSERVICEMODELS.PhoenixPayloads.IntelligentRecommendationAttributes
                                                    {
                                                        FieldName = "Description",
                                                        FieldValue = predictions[i][property.Name].ToString()
                                                    };
                                                    newRecommendation.IntelligentRecommendationAttributes.Add(recommendationAttributes3);
                                                }

                                                if (property.Name.ToUpper().Contains("Score"))
                                                {
                                                    var recommendationAttributes4 = new WINSERVICEMODELS.PhoenixPayloads.IntelligentRecommendationAttributes
                                                    {
                                                        FieldName = "Score",
                                                        FieldValue = predictions[i][property.Name].ToString()
                                                    };

                                                    newRecommendation.IntelligentRecommendationAttributes.Add(recommendationAttributes4);
                                                }
                                            }
                                        }
                                    }

                                    JObject recVal = JObject.Parse(JsonConvert.SerializeObject(newRecommendation));
                                    bool takeChangeRequest = true;
                                    foreach (var item in recVal["IntelligentRecommendationAttributes"])
                                    {
                                        if (item["FieldName"].ToString() == "ChangeRequestUId")
                                        {
                                            if (item["FieldValue"].ToString() == changeRequestUId)
                                            {
                                                takeChangeRequest = false;
                                            }
                                            break;
                                        }

                                    }
                                    if (takeChangeRequest)
                                    {
                                        if (addRecommendation)
                                            newRecommendations.Add(recVal);
                                    }
                                }

                                entityRecommendation["IntelligentRecommendations"] = newRecommendations;
                                predResponse = true;
                            }
                            else
                            {
                                // LogErrorMessageToDB(clientUId, deliveryConstructUId, "", CREntityUId, changeRequestUId, "Prediction", "Prediction Response - Ingrain python api gave false in response status", null);
                            }
                        }
                        else
                        {
                            // LogErrorMessageToDB(clientUId, deliveryConstructUId, "", CREntityUId, changeRequestUId, "Prediction", "Prediction Response - Error", null);
                        }


                    }
                    else
                    {
                        //LogErrorMessageToDB(clientUId, deliveryConstructUId, "", CREntityUId, changeRequestUId, "Prediction", "Null response from Ingrain Prediction API", null);
                    }

                    // }

                    var payload = new
                    {
                        Recommendation = Base64Encode(JsonConvert.SerializeObject(entityRecommendation))
                    };
                    string correlationUId = entityRecommendation["CorrelationUId"].ToString();

                    string res = string.Empty;
                    if (predResponse)
                    {
                        res = _PhoenixhadoopConnection.UpdateEntityRecommendationInPhoenix(payload, log.ClientUId, deliveryConstructUId, correlationUId, "ChangeRequest");
                    }

                    if (string.IsNullOrEmpty(res))
                    {
                        // LogErrorMessageToDB(clientUId, deliveryConstructUId, "", CREntityUId, changeRequestUId, "Prediction", "Completed - " + res, null);
                    }
                    else
                    {
                        // LogInfoMessageToDB(clientUId, deliveryConstructUId, "", CREntityUId, changeRequestUId, "Prediction", "Completed -" + res + "-Updated CorrelationId from -" + correlationUId + " -to- " + correlationUId, "C");
                        isSuccess = true;
                    }
                }

            }
            catch (Exception ex)
            {
                //LogErrorMessageToDB(clientUId, deliveryConstructUId, "", CREntityUId, changeRequestUId, "Prediction", ex.ToString(), null);
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ConstraintsSchedularService), nameof(UpdateChangeRequestPredictions), ex.Message, ex, string.Empty, string.Empty, log.ClientUId, deliveryConstructUId, CONSTANTS.CustomConstraintsLog);
            }
            return isSuccess;
        }


        public bool UpdateIterationPredictions(string clientUId, string deliveryConstructUId, string iterationUId, string UseCaseId)
        {
            bool isSuccess = false;
            bool predResponse = false;
            try
            {
                // LogInfoMessageToDB(clientUId, deliveryConstructUId, "", IterationEntityUId, iterationUId, "Prediction", "Started Prediction", null);
                string iterationPayload = _PhoenixhadoopConnection.GetEntityPayload(clientUId, deliveryConstructUId, "Iteration", iterationUId);
                //string iterationPayload = GetEntityPayload(clientUId, deliveryConstructUId, "Iteration", iterationUId);
                if (string.IsNullOrEmpty(iterationPayload))
                {
                    UpdateIterationRecommendationStatusinDB(clientUId, deliveryConstructUId, iterationUId, false, "Failed");
                    throw new InvalidDataException("No Data in Phoenix Get API");
                }
                dynamic iteration = JsonConvert.DeserializeObject<dynamic>(iterationPayload);
                JObject entityRecommendation = new JObject();
                bool newRec = true;
                if (string.IsNullOrEmpty((string)iteration.Iterations[0].Recommendations))
                {
                    entityRecommendation["CorrelationUId"] = IterationEntityUId;
                    entityRecommendation["EntityUId"] = IterationEntityUId;
                    entityRecommendation["ItemUId"] = iterationUId;
                    entityRecommendation["ItemExternalId"] = iteration.Iterations[0].IterationExternalId;
                    entityRecommendation["ProductInstanceUId"] = iteration.Iterations[0].CreatedByProductInstanceUId;
                    entityRecommendation["CreatedByApp"] = "myWizard.IngrAIn";
                    entityRecommendation["CreatedOn"] = DateTime.UtcNow.ToString("o");
                    entityRecommendation["ModifiedByApp"] = "myWizard.IngrAIn";
                    entityRecommendation["ModifiedOn"] = DateTime.UtcNow.ToString("o");
                    entityRecommendation["CreatedAtSourceOn"] = DateTime.UtcNow.ToString("o");
                    entityRecommendation["ModifiedAtSourceOn"] = DateTime.UtcNow.ToString("o");
                }
                else
                {
                    newRec = false;
                    entityRecommendation["CorrelationUId"] = IterationEntityUId;
                    entityRecommendation = JObject.Parse(Base64Decode((string)iteration.Iterations[0].Recommendations));
                    entityRecommendation["ModifiedAtSourceOn"] = DateTime.UtcNow.ToString("o");
                }

                //foreach (var usecaseId in SSAIUseCaseIds)
                //{
                string response = SSAIPredictionAPI(clientUId, deliveryConstructUId, UseCaseId, iterationUId);
                if (!string.IsNullOrEmpty(response))
                {
                    JObject result = JObject.Parse(response);
                    if (result["Status"].ToString() == "C")
                    {
                        JArray predictionResult = JArray.Parse(result["PredictedData"].ToString());

                        string predictedValue = predictionResult[0]["predictedValue"].ToString();

                        var recommendationTypeUId = UseCaseId == IA_DefectRateUseCaseId ? RecommendationTypeUId_DefectRate : RecommendationTypeUId_SPI;

                        var newRecommendation = new WINSERVICEMODELS.PhoenixPayloads.IntelligentRecommendations();
                        newRecommendation.IntelligentRecommendationTypeUId = recommendationTypeUId;
                        newRecommendation.StateUId = DraftStateUId;

                        newRecommendation.IntelligentRecommendationAttributes = new List<WINSERVICEMODELS.PhoenixPayloads.IntelligentRecommendationAttributes>();

                        var recommendationAttributes1 = new WINSERVICEMODELS.PhoenixPayloads.IntelligentRecommendationAttributes
                        {
                            FieldName = "IterationUId",
                            FieldValue = iterationUId
                        };
                        newRecommendation.IntelligentRecommendationAttributes.Add(recommendationAttributes1);

                        var recommendationAttributes2 = new WINSERVICEMODELS.PhoenixPayloads.IntelligentRecommendationAttributes
                        {
                            FieldName = "PredictedValue",
                            FieldValue = predictedValue
                        };
                        newRecommendation.IntelligentRecommendationAttributes.Add(recommendationAttributes2);

                        if (!newRec)
                        {
                            var oldRecommendations = JArray.Parse(entityRecommendation["IntelligentRecommendations"].ToString());
                            if (oldRecommendations.Count > 0)
                            {
                                //removing existing recommendation
                                while (true)
                                {
                                    JObject jo = oldRecommendations.Children<JObject>().FirstOrDefault(o => o["IntelligentRecommendationTypeUId"].ToString() == recommendationTypeUId);
                                    if (jo == null)
                                    {
                                        break;
                                    }
                                    else
                                    {
                                        oldRecommendations.Remove(jo);
                                    }

                                }
                            }

                            //add new recommendation
                            JObject recVal = JObject.Parse(JsonConvert.SerializeObject(newRecommendation, Formatting.None));
                            oldRecommendations.Add(recVal);
                            entityRecommendation["IntelligentRecommendations"] = oldRecommendations;
                        }
                        else
                        {
                            JObject recVal = JObject.Parse(JsonConvert.SerializeObject(newRecommendation, Formatting.None));
                            entityRecommendation["IntelligentRecommendations"] = new JArray() { recVal };
                            newRec = false;
                        }
                        predResponse = true;
                    }
                    else
                    {
                        // LogInfoMessageToDB(clientUId, deliveryConstructUId, "", IterationEntityUId, iterationUId, "Prediction", "Ingrain Prediction API response error -" + result["Message"].ToString(), null);
                    }

                }
                else
                {
                    // LogInfoMessageToDB(clientUId, deliveryConstructUId, "", IterationEntityUId, iterationUId, "Prediction", "Null response from Ingrain Prediction API response error", null);
                }
                //}


                var payload = new
                {
                    Recommendation = Base64Encode(JsonConvert.SerializeObject(entityRecommendation, Formatting.None))
                };
                string correlationUid = entityRecommendation["CorrelationUId"].ToString();
                string res = string.Empty;

                if (predResponse)
                {
                    res = _PhoenixhadoopConnection.UpdateEntityRecommendationInPhoenix(payload, clientUId, deliveryConstructUId, correlationUid, "Iteration");

                }

                if (string.IsNullOrEmpty(res))
                {
                    //  LogErrorMessageToDB(clientUId, deliveryConstructUId, "", IterationEntityUId, iterationUId, "Prediction", "Completed -" + res, null);
                    UpdateIterationRecommendationStatusinDB(clientUId, deliveryConstructUId, iterationUId, false, "Failed");
                }
                else
                {
                    //   LogInfoMessageToDB(clientUId, deliveryConstructUId, "", IterationEntityUId, iterationUId, "Prediction", "Completed -" + res + "-Updated CorrelationId from -" + correlationUid + " -to- " + correlationUid, "C");
                    isSuccess = true;
                    UpdateIterationRecommendationStatusinDB(clientUId, deliveryConstructUId, iterationUId, true, "Success");
                }


            }
            catch (Exception ex)
            {
                UpdateIterationRecommendationStatusinDB(clientUId, deliveryConstructUId, iterationUId, false, "Failed");
                // LogErrorMessageToDB(clientUId, deliveryConstructUId, "", iterationUId, iterationUId, "Prediction", ex.ToString(), null);
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ConstraintsSchedularService), nameof(UpdateIterationPredictions), ex.Message, ex, string.Empty, string.Empty, clientUId, deliveryConstructUId, CONSTANTS.CustomConstraintsLog);
            }
            return isSuccess;

        }

        public string SSAIPredictionAPI(string clientUId, string deliveryConstructUId, string useCaseId, string releaseUId)
        {
            string jsonResult = string.Empty;
            var predictionInput = new
            {
                ClientUId = clientUId,
                DeliveryConstructUId = deliveryConstructUId,
                UseCaseId = useCaseId,
                ReleaseUId = releaseUId, // List of releaseid
                UserId = PredictionSchedulerUserName
            };
            StringContent content = new StringContent(JsonConvert.SerializeObject(predictionInput), Encoding.UTF8, "application/json");
            string token = _TokenService.GenerateToken();
            string baseURI = appSettings.IngrainAPIUrl;
            string apiPath = "/api/ia/UseCasePredictionRequest";
            var response = _HttpMethodService.InvokePOSTRequest(token, baseURI, apiPath, content, string.Empty);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                jsonResult = response.Content.ReadAsStringAsync().Result;
            }
            return jsonResult;
        }

        public void UpdateIterationRecommendationStatusinDB(string clientUId, string deliveryConstructUId, string itemUId, bool status, string message)
        {
            var collection = _database.GetCollection<WINSERVICEMODELS.PhoenixIterations>(CONSTANTS.PhoenixIterations);
            var filter = Builders<WINSERVICEMODELS.PhoenixIterations>.Filter.Where(x => x.ClientUId == clientUId)
                         & Builders<WINSERVICEMODELS.PhoenixIterations>.Filter.Where(x => x.DeliveryConstructUId == deliveryConstructUId)
                         & Builders<WINSERVICEMODELS.PhoenixIterations>.Filter.Where(x => x.ItemUId == itemUId);
            var projection = Builders<WINSERVICEMODELS.PhoenixIterations>.Projection.Exclude("_id");
            var result = collection.Find(filter).Project<WINSERVICEMODELS.PhoenixIterations>(projection).FirstOrDefault();
            if (result != null)
            {
                if (status)
                {
                    var update = Builders<WINSERVICEMODELS.PhoenixIterations>.Update.Set(x => x.isRecommendationCompleted, true)
                                                                                    .Set(x => x.Status, "C")
                                                                                                    .Set(x => x.LastRecommendationUpdateTime, DateTime.UtcNow)
                                                                                                    .Set(x => x.Message, message)
                                                                                                    .Set(x => x.ModifiedOn, DateTime.UtcNow);
                    collection.UpdateOne(filter, update);
                }
                else
                {
                    var update = Builders<WINSERVICEMODELS.PhoenixIterations>.Update.Set(x => x.Message, message)
                                                                                    .Set(x => x.isRecommendationCompleted, true)
                                                                                    .Set(x => x.Status, "E")
                                                                                    .Set(x => x.ModifiedOn, DateTime.UtcNow);
                    collection.UpdateOne(filter, update);
                }


            }

        }

        private string GetENSPayload(DATAMODELS.ENSEntityNotificationLog log)
        {
            try
            {
                string token = _TokenService.GenerateToken();
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Accept.Clear();
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                    httpClient.DefaultRequestHeaders.Add("AppServiceUID", log.AppServiceUID);
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                    var result = httpClient.GetAsync(log.CallbackLink).Result;
                    if (result.StatusCode != HttpStatusCode.OK)
                    {
                        throw new Exception($"{result.Content}" + "" + $"{result.StatusCode}");
                    }

                    return result.Content.ReadAsStringAsync().Result;
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ConstraintsSchedularService), nameof(GetENSPayload), ex.Message, ex, "Ingrain Window Service", string.Empty, string.Empty, string.Empty, CONSTANTS.CustomConstraintsLog);
                return null;
            }

        }

        public void InsertENSNotificationlog(dynamic item, string ServiceLevel)
        {
            var collection = _database.GetCollection<DATAMODELS.ENSEntityNotificationLog>(CONSTANTS.ENSEntityNotificationLog);
            ENSEntityNotification ensNotification = null;
            if (ServiceLevel.ToUpper() == CONSTANTS.SSAIAPP.ToUpper())
            {
                string callbackurl = appSettings.myWizardAPIUrl + "Iterations/Callback?clientUId=" + item.ClientUId.ToString() + "&deliveryConstructUId=" + item.DeliveryConstructUID.ToString() + "&CorrelationUId=" + item.CorrelationId.ToString() + "&CorrelationBatchId=1";
                ensNotification = new ENSEntityNotification()
                {
                    EntityEventMessageStatusUId = CONSTANTS.Null,
                    EntityEventUId = CONSTANTS.Null,
                    WorkItemTypeUId = CONSTANTS.Null,
                    EntityUId = IterationEntityUId,
                    ClientUId = item.ClientUId,
                    DeliveryConstructUId = item.DeliveryConstructUID,
                    SenderApp = "Ingrain",
                    Message = "Ingrain",
                    CallbackLink = callbackurl,
                    StatusReason = CONSTANTS.Null,
                    TemplateUseCaseId = item.CorrelationId,
                    AppServiceUID = ProvisionedAppServiceUID
                };
            }
            else if (ServiceLevel.ToUpper() == CONSTANTS.AIAPP.ToUpper())
            {
                Service service = _DBConnection.GetAiCoreServiceDetails(item.ServiceId);

                if (service.ServiceCode == "CLUSTERING" || service.ServiceCode == "WORDCLOUD")
                {
                    ClusteringAPIModel AICoreModel = new ClusteringAPIModel();

                    //fetch parent Model
                    AICoreModel = _DBConnection.GetClusteringModel(item["CorrelationId"]);

                    string callbackurl = appSettings.myWizardAPIUrl + "/core/v1/ChangeRequests?clientUId=" + AICoreModel.ClientID + "&deliveryConstructUId=" + AICoreModel.DCUID + "&CorrelationUId=" + AICoreModel.CorrelationId + "&CorrelationBatchId=1";
                    ensNotification = new ENSEntityNotification()
                    {
                        EntityEventMessageStatusUId = CONSTANTS.Null,
                        EntityEventUId = CONSTANTS.Null,
                        WorkItemTypeUId = CONSTANTS.Null,
                        EntityUId = CREntityUId,
                        ClientUId = AICoreModel.ClientID,
                        DeliveryConstructUId = AICoreModel.DCUID,
                        SenderApp = "Ingrain",
                        Message = "Ingrain",
                        CallbackLink = callbackurl,
                        StatusReason = CONSTANTS.Null,
                        TemplateUseCaseId = item.UsecaseId,
                        ServiceID = AICoreModel.ServiceID,
                        AppServiceUID = ProvisionedAppServiceUID
                    };


                }
                else
                {
                    AIDataModels.AICoreModels AICoreModel = new AIDataModels.AICoreModels();
                    AICoreModel = _DBConnection.GetAICoreModelPath(item.CorrelationId);

                    string callbackurl = appSettings.myWizardAPIUrl + "/core/v1/ChangeRequests?clientUId=" + AICoreModel.ClientId + "&deliveryConstructUId=" + AICoreModel.DeliveryConstructId + "&CorrelationUId=" + AICoreModel.CorrelationId + "&CorrelationBatchId=1";
                    ensNotification = new ENSEntityNotification()
                    {
                        EntityEventMessageStatusUId = CONSTANTS.Null,
                        EntityEventUId = CONSTANTS.Null,
                        WorkItemTypeUId = CONSTANTS.Null,
                        EntityUId = CREntityUId,
                        ClientUId = AICoreModel.ClientId,
                        DeliveryConstructUId = AICoreModel.DeliveryConstructId,
                        SenderApp = "Ingrain",
                        Message = "Ingrain",
                        CallbackLink = callbackurl,
                        StatusReason = CONSTANTS.Null,
                        TemplateUseCaseId = item.UsecaseId,
                        ServiceID = AICoreModel.ServiceId,
                        AppServiceUID = ProvisionedAppServiceUID
                    };
                }
            }

            DATAMODELS.ENSEntityNotificationLog iteration = new DATAMODELS.ENSEntityNotificationLog(ensNotification);

            collection.InsertOne(iteration);
        }

        #endregion Change Request

        #endregion


    }
}
