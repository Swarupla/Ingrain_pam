using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text;
using Accenture.MyWizard.Cryptography.EncryptionProviders;
using WINSERVICEMODELS = Accenture.MyWizard.Ingrain.WindowService.Models;
using DATAMODELS = Accenture.MyWizard.Ingrain.DataModels.Models;
using DATAACCESS = Accenture.MyWizard.Ingrain.WindowService;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net;
using Newtonsoft.Json;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
using System.Threading.Tasks;
using System.Data;
using MongoDB.Bson;
using Accenture.MyWizard.Ingrain.DataModels.AICore;
using Accenture.MyWizard.Ingrain.DataModels.AICore.UseCase;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Threading;
using System.Collections;
using Accenture.MyWizard.Ingrain.BusinessDomain.Common;
using CryptographyHelper = Accenture.MyWizard.Cryptography;

namespace Accenture.MyWizard.Ingrain.WindowService.Services
{
    public class PredictionSchedulerService
    {
        private readonly WINSERVICEMODELS.AppSettings appSettings = null;
        private IMongoDatabase _database;
        private DATAACCESS.DatabaseProvider databaseProvider;
        private string IA_AppServiceUId = null;
        public string IA_ApplicationId { get; set; }
        public string IA_ServiceId { get; set; }
        public string IA_DefectRateUseCaseId { get; set; }
        public string IA_SPIUseCaseId { get; set; }
        public string IA_CRUseCaseId { get; set; }
        public string IA_UserStoryUseCaseId { get; set; }
        public string IA_RequirementUseCaseId { get; set; }
        public string IterationEntityUId { get; set; }
        public string CREntityUId { get; set; }

        public List<string> SSAIUseCaseIds { get; set; }
        public List<string> AIUseCaseIds { get; set; }

        private readonly string _cRQueryAPI = "ChangeRequests/Query?clientUId={0}&deliveryConstructUId={1}&includeCompleteHierarchy=true";
        private readonly string _iterationQueryAPI = "Iterations/Query?clientUId={0}&deliveryConstructUId={1}&includeCompleteHierarchy=true";
        private readonly string _cRMergeAPI = "ChangeRequests1?clientUId={0}&deliveryConstructUId={1}&IsInternalUpdateOnly=1";
        private readonly string _recommendationMergeCRAPI = "EntityRecommendation1?clientUId={0}&deliveryConstructUId={1}";
        private readonly string _recommendationMergeAPI = "EntityRecommendations1?clientUId={0}&deliveryConstructUId={1}";
        private readonly string _iterationMergeAPI = "MergeIterations?clientUId={0}&deliveryConstructUId={1}&IsInternalUpdateOnly=1";
        private readonly string PredictionSchedulerUserName = "SYSTEM";
        private readonly string DraftStateUId = "00904010-0001-0000-0000-000000000000";
        private readonly string RecommendationTypeUId_DefectRate = "00905010-0007-0000-0000-000000000000";
        private readonly string RecommendationTypeUId_SPI = "00905010-0006-0000-0000-000000000000";
        private readonly string RecommendationTypeUId_SimilarCR = "00905010-0001-0000-0000-000000000000";
        private readonly string RecommendationTypeUId_SimilarUserStory = "00905010-0004-0000-0000-000000000000";
        private readonly string RecommendationTypeUId_SimilarRequirement = "00905010-0005-0000-0000-000000000000";
        private readonly string _hadoopCRApi = "/bi/V1/Entity/?ClientUId={0}&DeliveryConstructUId={1}";
        private readonly string _hadoopUserStoryApi = "/bi/V1/Entity/?ClientUId={0}&DeliveryConstructUId={1}";
        private readonly string _hadooopRequirementApi = "/bi/V1/Entity/?ClientUId={0}&DeliveryConstructUId={1}";
        private readonly string _hadoopSPIApi = "/bi/ChangeImpactScheduleCalculationData";
        private readonly string _hadoopDefectRateApi = "/bi/ChangeImpactDefectCalculationData";
        private readonly string UserStoryEntityUId = "00020040-0200-0000-0000-000000000000";
        private readonly string RequirementEntityUId = "00020070-0700-0000-0000-000000000000";

        static CryptographyHelper.Utility.CryptographyUtility CryptographyUtility = null;

        List<string> CRENSfieldvalues { get; set; }




        public PredictionSchedulerService()
        {
            appSettings = AppSettingsJson.GetAppSettings().GetSection("AppSettings").Get<WINSERVICEMODELS.AppSettings>();
            databaseProvider = new DATAACCESS.DatabaseProvider();
            var dataBaseName = MongoUrl.Create(appSettings.connectionString).DatabaseName;
            MongoClient mongoClient = databaseProvider.GetDatabaseConnection();
            _database = mongoClient.GetDatabase(dataBaseName);
            IA_AppServiceUId = appSettings.IA_AppServiceUId;
            IA_ApplicationId = appSettings.IA_ApplicationId;
            IA_ServiceId = appSettings.IA_ServiceId;
            SSAIUseCaseIds = appSettings.IA_SSAIUseCaseIds; // set usecaseids in order
            AIUseCaseIds = appSettings.IA_AIUseCaseIds;
            IterationEntityUId = appSettings.IterationEntityUId;
            CREntityUId = appSettings.CREntityUId;

            IA_DefectRateUseCaseId = string.IsNullOrEmpty(SSAIUseCaseIds.ElementAtOrDefault(0)) ? null : SSAIUseCaseIds[0];
            IA_SPIUseCaseId = string.IsNullOrEmpty(SSAIUseCaseIds.ElementAtOrDefault(1)) ? null : SSAIUseCaseIds[1];
            IA_CRUseCaseId = string.IsNullOrEmpty(AIUseCaseIds.ElementAtOrDefault(0)) ? null : AIUseCaseIds[0];
            IA_UserStoryUseCaseId = string.IsNullOrEmpty(AIUseCaseIds.ElementAtOrDefault(1)) ? null : AIUseCaseIds[1];
            IA_RequirementUseCaseId = string.IsNullOrEmpty(AIUseCaseIds.ElementAtOrDefault(2)) ? null : AIUseCaseIds[2];
            CRENSfieldvalues = appSettings.CRENSfieldvalues.Split(",").ToList();
            CryptographyUtility = new CryptographyHelper.Utility.CryptographyUtility();
        }





        public void StartModelTraining(DATAMODELS.DeployModelsDto item)
        {
            try
            {
                //provisoning check is based on usecase and applicationId

                UpdateTaskStatus(0, "IATRAININGSCHEDULER", false, "I", "Training in progress");
                //Fetch List of provisioned Client and Delivery Constructs for AppServiceUId
                WINSERVICEMODELS.AppDeliveryConstructs appDeliveryConstructs = FetchClientsDeliveryConstructs(IA_AppServiceUId);

                if (appDeliveryConstructs == null)
                {
                    UpdateOfflineException(item, "AppDeliveryConstructs is null");
                    throw new ArgumentNullException("AppDeliveryConstructs is null");
                }
                else if (appDeliveryConstructs.AppServiceClientDeliveryConstructs == null)
                {
                    UpdateOfflineException(item, "appDeliveryConstructs.AppServiceClientDeliveryConstructs is null");
                    throw new ArgumentNullException("appDeliveryConstructs.AppServiceClientDeliveryConstructs is null");
                }

                //Training SSAI usecase IDs
                //  if (SSAIUseCaseIds.Count > 0)
                //{
                //  foreach (var usecaseId in SSAIUseCaseIds)
                //{
                if (appDeliveryConstructs != null)
                {
                    if (appDeliveryConstructs.AppServiceClientDeliveryConstructs.Count > 0)
                    {
                        foreach (var provisionedDetails in appDeliveryConstructs.AppServiceClientDeliveryConstructs)
                        {
                            if (!string.IsNullOrEmpty(provisionedDetails.ClientUId) && !string.IsNullOrEmpty(provisionedDetails.DeliveryConstructUId))
                            {
                                var model = CheckIfSSAIModelTrained(provisionedDetails.ClientUId, provisionedDetails.DeliveryConstructUId, item.CorrelationId, PredictionSchedulerUserName);
                                if (model != null)
                                {
                                    var status = GetAutoTrainStatus(model.CorrelationId);
                                    if (status == "E")
                                    {
                                        TrainSSAIModel(provisionedDetails.ClientUId, provisionedDetails.DeliveryConstructUId, item.CorrelationId, model.CorrelationId);
                                    }
                                    else
                                    {
                                        LogInfoMessageToDB(provisionedDetails.ClientUId, provisionedDetails.DeliveryConstructUId, item.CorrelationId, "", "", "Training", "Training already completed/Inprogress", null);
                                    }
                                }
                                else
                                {
                                    TrainSSAIModel(provisionedDetails.ClientUId, provisionedDetails.DeliveryConstructUId, item.CorrelationId, null);
                                }

                            }
                        }
                    }
                }
                //  }
                //}


                //Training AI Service usecase IDs


                if (AIUseCaseIds.Count > 0)
                {

                    foreach (var usecaseId in AIUseCaseIds)
                    {
                        if (appDeliveryConstructs != null)
                        {
                            if (appDeliveryConstructs.AppServiceClientDeliveryConstructs.Count > 0)
                            {
                                foreach (var provisionedDetails in appDeliveryConstructs.AppServiceClientDeliveryConstructs)
                                {
                                    if (!string.IsNullOrEmpty(provisionedDetails.ClientUId) && !string.IsNullOrEmpty(provisionedDetails.DeliveryConstructUId))
                                    {
                                        /// code to train AI service usecase IDs
                                        var model = CheckIfAIModelTrained(provisionedDetails.ClientUId, provisionedDetails.DeliveryConstructUId, usecaseId, IA_ServiceId, PredictionSchedulerUserName);
                                        if (model != null)
                                        {
                                            if (model.ModelStatus == "Error" || model.ModelStatus == "Warning")
                                            {
                                                if (DeleteAIModel(model.CorrelationId))
                                                    TrainAIModel(provisionedDetails.ClientUId, provisionedDetails.DeliveryConstructUId, usecaseId, IA_ServiceId, PredictionSchedulerUserName);
                                            }
                                            else
                                            {
                                                LogInfoMessageToDB(provisionedDetails.ClientUId, provisionedDetails.DeliveryConstructUId, usecaseId, "", "", "Training", "Training already completed/Inprogress", null);
                                            }
                                        }
                                        else
                                        {
                                            LogInfoMessageToDB(provisionedDetails.ClientUId, provisionedDetails.DeliveryConstructUId, usecaseId, IA_ServiceId, "", "Training", "Training already completed/Inprogress", null);
                                            TrainAIModel(provisionedDetails.ClientUId, provisionedDetails.DeliveryConstructUId, usecaseId, IA_ServiceId, PredictionSchedulerUserName);
                                        }

                                    }
                                }
                            }
                        }
                    }
                }
                UpdateTaskStatus(0, "IATRAININGSCHEDULER", true, "C", "Training Completed");
                UpdateOfflineRunTime(item);

            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(PredictionSchedulerService), nameof(StartModelTraining), ex.Message + "- STACKTRACE - " + ex.StackTrace + ex.InnerException, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                UpdateTaskStatus(0, "IATRAININGSCHEDULER", true, "E", ex.Message);
                UpdateOfflineException(item, ex.Message);
            }
        }

        //public void StartIASimilarTraining(DATAMODELS.DeployModelsDto item)
        //{
        //    try
        //    {
        //        //provisoning check is based on usecase and applicationId

        //        UpdateTaskStatus(0, "IATRAININGSCHEDULER", false, "I", "Training in progress");
        //        //Fetch List of provisioned Client and Delivery Constructs for AppServiceUId
        //        WINSERVICEMODELS.AppDeliveryConstructs appDeliveryConstructs = FetchClientsDeliveryConstructs(IA_AppServiceUId);

        //        if (appDeliveryConstructs == null)
        //        {
        //            UpdateOfflineException(item, "AppDeliveryConstructs is null");
        //            throw new ArgumentNullException("AppDeliveryConstructs is null");
        //        }
        //        else if (appDeliveryConstructs.AppServiceClientDeliveryConstructs == null)
        //        {
        //            UpdateOfflineException(item, "appDeliveryConstructs.AppServiceClientDeliveryConstructs is null");
        //            throw new ArgumentNullException("appDeliveryConstructs.AppServiceClientDeliveryConstructs is null");
        //        }
        //        if (AIUseCaseIds.Count > 0)
        //        {

        //            foreach (var usecaseId in AIUseCaseIds)
        //            {
        //                if (appDeliveryConstructs != null)
        //                {
        //                    if (appDeliveryConstructs.AppServiceClientDeliveryConstructs.Count > 0)
        //                    {
        //                        foreach (var provisionedDetails in appDeliveryConstructs.AppServiceClientDeliveryConstructs)
        //                        {
        //                            if (!string.IsNullOrEmpty(provisionedDetails.ClientUId) && !string.IsNullOrEmpty(provisionedDetails.DeliveryConstructUId))
        //                            {
        //                                /// code to train AI service usecase IDs
        //                                var model = CheckIfAIModelTrained(provisionedDetails.ClientUId, provisionedDetails.DeliveryConstructUId, usecaseId, IA_ServiceId, PredictionSchedulerUserName);
        //                                if (model != null)
        //                                {
        //                                    if (model.ModelStatus == "Error" || model.ModelStatus == "Warning")
        //                                    {
        //                                        if (DeleteAIModel(model.CorrelationId))
        //                                            TrainAIModel(provisionedDetails.ClientUId, provisionedDetails.DeliveryConstructUId, usecaseId, IA_ServiceId, PredictionSchedulerUserName);
        //                                    }
        //                                    else
        //                                    {
        //                                        LogInfoMessageToDB(provisionedDetails.ClientUId, provisionedDetails.DeliveryConstructUId, usecaseId, "", "", "Training", "Training already completed/Inprogress", null);
        //                                    }
        //                                }
        //                                else
        //                                {
        //                                    TrainAIModel(provisionedDetails.ClientUId, provisionedDetails.DeliveryConstructUId, usecaseId, IA_ServiceId, PredictionSchedulerUserName);
        //                                }
        //                            }
        //                            //else
        //                            //{
        //                            //    TrainAIModel(provisionedDetails.ClientUId, provisionedDetails.DeliveryConstructUId, usecaseId, IA_ServiceId, PredictionSchedulerUserName);
        //                            //}

        //                        }
        //                    }
        //                }
        //            }
        //        }
        //        UpdateTaskStatus(0, "IATRAININGSCHEDULER", true, "C", "Training Completed");
        //        UpdateOfflineRunTime(item);

        //    }
        //    catch (Exception ex)
        //    {
        //        LOGGING.LogManager.Logger.LogErrorMessage(typeof(PredictionSchedulerService), nameof(StartModelTraining), ex.Message + "- STACKTRACE - " + ex.StackTrace + ex.InnerException, ex, string.Empty, string.Empty, string.Empty, string.Empty);
        //        UpdateTaskStatus(0, "IATRAININGSCHEDULER", true, "E", ex.Message);
        //        UpdateOfflineException(item, ex.Message);
        //    }
        //}
        public void StartIASimilarTraining()
        {
            try
            {
                //provisoning check is based on usecase and applicationId

                UpdateTaskStatus(0, "IASIMILARTRAININGSCHEDULER", false, "I", "Training in progress");
                //Fetch List of provisioned Client and Delivery Constructs for AppServiceUId
                WINSERVICEMODELS.AppDeliveryConstructs appDeliveryConstructs = FetchClientsDeliveryConstructs(IA_AppServiceUId);

                if (appDeliveryConstructs == null)
                {
                    //UpdateOfflineException(item, "AppDeliveryConstructs is null");
                    throw new ArgumentNullException("AppDeliveryConstructs is null");
                }
                else if (appDeliveryConstructs.AppServiceClientDeliveryConstructs == null)
                {
                    //UpdateOfflineException(item, "appDeliveryConstructs.AppServiceClientDeliveryConstructs is null");
                    throw new ArgumentNullException("appDeliveryConstructs.AppServiceClientDeliveryConstructs is null");
                }
                if (AIUseCaseIds.Count > 0)
                {

                    foreach (var usecaseId in AIUseCaseIds)
                    {
                        if (appDeliveryConstructs != null)
                        {
                            if (appDeliveryConstructs.AppServiceClientDeliveryConstructs.Count > 0)
                            {
                                foreach (var provisionedDetails in appDeliveryConstructs.AppServiceClientDeliveryConstructs)
                                {
                                    if (!string.IsNullOrEmpty(provisionedDetails.ClientUId) && !string.IsNullOrEmpty(provisionedDetails.DeliveryConstructUId))
                                    {
                                        /// code to train AI service usecase IDs
                                        var model = CheckIfAIModelTrained(provisionedDetails.ClientUId, provisionedDetails.DeliveryConstructUId, usecaseId, IA_ServiceId, PredictionSchedulerUserName);
                                        if (model != null)
                                        {
                                            if (model.ModelStatus == "Error" || model.ModelStatus == "Warning")
                                            {
                                                if (DeleteAIModel(model.CorrelationId))
                                                    TrainAIModel(provisionedDetails.ClientUId, provisionedDetails.DeliveryConstructUId, usecaseId, IA_ServiceId, PredictionSchedulerUserName);
                                            }
                                            else
                                            {
                                                LogInfoMessageToDB(provisionedDetails.ClientUId, provisionedDetails.DeliveryConstructUId, usecaseId, "", "", "Training", "Training already completed/Inprogress", null);
                                            }
                                        }
                                        else
                                        {
                                            TrainAIModel(provisionedDetails.ClientUId, provisionedDetails.DeliveryConstructUId, usecaseId, IA_ServiceId, PredictionSchedulerUserName);
                                        }

                                    }
                                }
                            }
                        }
                    }
                }
                UpdateTaskStatus(0, "IASIMILARTRAININGSCHEDULER", true, "C", "Training Completed");
                //UpdateOfflineRunTime(item);

            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(PredictionSchedulerService), nameof(StartModelTraining), ex.Message + "- STACKTRACE - " + ex.StackTrace + ex.InnerException, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                UpdateTaskStatus(0, "IASIMILARTRAININGSCHEDULER", true, "E", ex.Message);
                //UpdateOfflineException(item, ex.Message);
            }
        }


        public void ReTrainAIServiceModels(int retrainHrs)
        {
            try
            {
                UpdateTaskStatus(0, "IARETRAINSCHEDULER", false, "I", "Training in progress");
                List<AICoreModels> aiModels = GetCompletedAIServiceModels();
                if (aiModels != null)
                {
                    foreach (var model in aiModels)
                    {
                        DateTime modifiedDate = DateTime.Parse(model.ModifiedOn);
                        DateTime curTime = DateTime.Now;
                        int diff = (int)(curTime - modifiedDate).TotalHours;
                        if (diff > retrainHrs)
                        {
                            if (DeleteAIModel(model.CorrelationId))
                            {
                                TrainAIModel(model.ClientId, model.DeliveryConstructId, model.UsecaseId, IA_ServiceId, PredictionSchedulerUserName);
                                bool flag = true;
                                while (flag)
                                {
                                    Thread.Sleep(60000);
                                    AICoreModels mdl = CheckIfAIModelTrained(model.ClientId,
                                                                             model.DeliveryConstructId,
                                                                             model.UsecaseId,
                                                                             IA_ServiceId,
                                                                             PredictionSchedulerUserName);
                                    DateTime createdTime = DateTime.Parse(mdl.CreatedOn);
                                    DateTime curDate = DateTime.UtcNow;

                                    int elapsedMin = (int)(curDate - createdTime).TotalMinutes;
                                    if (mdl.ModelStatus == "Completed" || mdl.ModelStatus == "Error" || mdl.ModelStatus == "Warning" || elapsedMin > 10)
                                    {
                                        flag = false;
                                    }
                                }


                            }

                        }

                    }
                }
                UpdateTaskStatus(0, "IARETRAINSCHEDULER", true, "C", "Training Completed");
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(PredictionSchedulerService), nameof(ReTrainAIServiceModels), ex.Message + "---ErrorMessageInfo--" + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                UpdateTaskStatus(0, "IARETRAINSCHEDULER", true, "E", ex.Message + ex.StackTrace);
            }


        }



        public List<DATAMODELS.ENSEntityNotificationLog> ReadENSNotification(string clientUId)
        {
            var ensLogCollection = _database.GetCollection<DATAMODELS.ENSEntityNotificationLog>(nameof(DATAMODELS.ENSEntityNotificationLog));
            var filter = Builders<DATAMODELS.ENSEntityNotificationLog>.Filter;
            var filterQuery = filter.Where(x => x.ClientUId == clientUId)
                              & filter.Where(x => !x.isProcessed);
            return ensLogCollection.Find(filterQuery).ToList();
        }
        public List<DATAMODELS.ENSEntityNotificationLog> ReadIterationENSNotification(string clientUId)
        {
            var ensLogCollection = _database.GetCollection<DATAMODELS.ENSEntityNotificationLog>(nameof(DATAMODELS.ENSEntityNotificationLog));
            var filter = Builders<DATAMODELS.ENSEntityNotificationLog>.Filter;
            var filterQuery = filter.Where(x => x.ClientUId == clientUId)
                              & filter.Where(x => !x.isProcessed)
                              & filter.Where(x => x.EntityUId == IterationEntityUId);
            return ensLogCollection.Find(filterQuery).ToList();
        }

        public List<DATAMODELS.ENSEntityNotificationLog> ReadCRENSNotification(string clientUId)
        {
            var ensLogCollection = _database.GetCollection<DATAMODELS.ENSEntityNotificationLog>(nameof(DATAMODELS.ENSEntityNotificationLog));
            var filter = Builders<DATAMODELS.ENSEntityNotificationLog>.Filter;
            var filterQuery = filter.Where(x => x.ClientUId == clientUId)
                              & filter.Where(x => !x.isProcessed)
                              & filter.Where(x => x.EntityUId == CREntityUId)
                              & (filter.Where(x => x.RetryCount == null) | filter.Lt(x => x.RetryCount, 3));
            return ensLogCollection.Find(filterQuery).ToList();
        }

        //not in use 
        public void UpdatePredictionsForHistoricalEntities()
        {
            try
            {
                UpdateTaskStatus(0, "IAPREDICTIONSCHEDULER", false, "I", "Predictions in progress");
                WINSERVICEMODELS.AppDeliveryConstructs appDeliveryConstructs = FetchClientsDeliveryConstructs(IA_AppServiceUId);
                foreach (var dc in appDeliveryConstructs.AppServiceClientDeliveryConstructs)
                {
                    if (!string.IsNullOrEmpty(dc.ClientUId) && !string.IsNullOrEmpty(dc.DeliveryConstructUId))
                    {
                        foreach (var usecaseId in SSAIUseCaseIds)
                        {
                            var model = CheckIfSSAIModelTrained(dc.ClientUId, dc.DeliveryConstructUId, usecaseId, PredictionSchedulerUserName);
                            if (model != null)
                            {
                                if (model.Status == "Deployed")
                                {
                                    List<string> iterationUIdLst = FetchSSAIColumnUniqueValues(model.CorrelationId, "ReleaseUId");
                                    foreach (var iterationUId in iterationUIdLst)
                                    {
                                        Guid guid = Guid.Parse(iterationUId); // changing in guid format
                                        UpdateIterationPredictions(dc.ClientUId, dc.DeliveryConstructUId, guid.ToString());
                                    }
                                }


                            }
                        }
                        var crModel = CheckIfAIModelTrained(dc.ClientUId, dc.DeliveryConstructUId, IA_CRUseCaseId, IA_ServiceId, PredictionSchedulerUserName);
                        if (crModel != null)
                        {
                            if (crModel.ModelStatus == "Completed")
                            {
                                List<string> changeRequestUIdLst = FetchAIColumnUniqueValues(crModel.CorrelationId, "changerequestuid");
                                foreach (var usecaseId in AIUseCaseIds)
                                {
                                    var model = CheckIfAIModelTrained(dc.ClientUId, dc.DeliveryConstructUId, usecaseId, IA_ServiceId, PredictionSchedulerUserName);
                                    if (model != null)
                                    {
                                        if (model.ModelStatus == "Completed")
                                        {
                                            foreach (var changeRequestUId in changeRequestUIdLst)
                                            {
                                                Guid guid = Guid.Parse(changeRequestUId); //changing in guid format
                                                UpdateChangeRequestPredictions(dc.ClientUId, dc.DeliveryConstructUId, guid.ToString());
                                            }
                                        }

                                    }
                                }
                            }

                        }
                    }



                }
                UpdateTaskStatus(0, "IAPREDICTIONSCHEDULER", true, "C", "Completed");
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(PredictionSchedulerService), nameof(UpdatePredictionsForHistoricalEntities), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                UpdateTaskStatus(0, "IAPREDICTIONSCHEDULER", true, "E", ex.Message);
            }

        }


        public void TriggerENSPredictions()
        {
            WINSERVICEMODELS.AppDeliveryConstructs appDeliveryConstructs = FetchClientsDeliveryConstructs(IA_AppServiceUId);
            if (appDeliveryConstructs != null)
            {
                if (appDeliveryConstructs.AppServiceClientDeliveryConstructs != null)
                {
                    List<string> clientIdsLst = appDeliveryConstructs.AppServiceClientDeliveryConstructs.Select(x => x.ClientUId).Distinct().ToList();

                    foreach (var client in clientIdsLst)
                    {
                        List<string> dcsLst = appDeliveryConstructs.AppServiceClientDeliveryConstructs.Where(x => x.ClientUId == client).Select(x => x.DeliveryConstructUId).ToList();
                        List<DATAMODELS.ENSEntityNotificationLog> entityNotificationLogs = ReadENSNotification(client);
                        UpdatePredictionBasedOnENSNotification(entityNotificationLogs, dcsLst);
                    }
                }
            }

        }



        // not being used now
        public void TriggerIterationENSPredictions()
        {
            WINSERVICEMODELS.AppDeliveryConstructs appDeliveryConstructs = FetchClientsDeliveryConstructs(IA_AppServiceUId);
            if (appDeliveryConstructs != null)
            {
                if (appDeliveryConstructs.AppServiceClientDeliveryConstructs != null)
                {
                    List<string> clientIdsLst = appDeliveryConstructs.AppServiceClientDeliveryConstructs.Select(x => x.ClientUId).Distinct().ToList();

                    foreach (var client in clientIdsLst)
                    {
                        List<string> dcsLst = appDeliveryConstructs.AppServiceClientDeliveryConstructs.Where(x => x.ClientUId == client).Select(x => x.DeliveryConstructUId).ToList();
                        List<DATAMODELS.ENSEntityNotificationLog> entityNotificationLogs = ReadIterationENSNotification(client);
                        UpdatePredictionBasedOnENSNotification(entityNotificationLogs, dcsLst);
                    }
                }
            }

        }
        public void TriggerCRENSPredictions()
        {
            //Get the Provisoned DCS
            WINSERVICEMODELS.AppDeliveryConstructs appDeliveryConstructs = FetchClientsDeliveryConstructs(IA_AppServiceUId);
            if (appDeliveryConstructs != null)
            {
                if (appDeliveryConstructs.AppServiceClientDeliveryConstructs != null)
                {
                    List<string> clientIdsLst = appDeliveryConstructs.AppServiceClientDeliveryConstructs.Select(x => x.ClientUId).Distinct().ToList();

                    foreach (var client in clientIdsLst)
                    {
                        List<string> dcsLst = appDeliveryConstructs.AppServiceClientDeliveryConstructs.Where(x => x.ClientUId == client).Select(x => x.DeliveryConstructUId).ToList();
                        List<DATAMODELS.ENSEntityNotificationLog> entityNotificationLogs = ReadCRENSNotification(client);
                        if (entityNotificationLogs.Count > 0)
                        {
                            UpdatePredictionBasedOnENSNotification(entityNotificationLogs, dcsLst);
                        }

                    }
                }
            }

        }


        public void UpdatePredictionBasedOnENSNotification(List<DATAMODELS.ENSEntityNotificationLog> entityNotificationLogs, List<string> provisionedDCs)
        {
            bool bTriggerCRENSPredictions = false;
            foreach (var log in entityNotificationLogs)
            {
                try
                {
                    if (log.EntityEventUId == "20110200-0001-0000-0000-000000000000"
                        || log.EntityEventUId == "20110200-0001-0001-0000-000000000000"
                        || log.EntityEventUId == "00020100-0600-0000-0000-011000000000"
                        || log.EntityEventUId == "00020100-0600-0000-0000-021000000000")
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(PredictionSchedulerService), nameof(UpdatePredictionBasedOnENSNotification), "If block - EntityEventUId is created", "ClientUID: " + log.ClientUId, "DCUID: " + log.DeliveryConstructUId, "IterationEntityUId: " + IterationEntityUId, string.Empty);
                        UpdateNotificationStatus(log._id, true, "C", "EntityEventUId is created");
                    }
                    else if (log.SenderApp == "myWizard.IngrAIn")
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(PredictionSchedulerService), nameof(UpdatePredictionBasedOnENSNotification), "else if - Skipped as sender app is Ingrain", "ClientUID: " + log.ClientUId, "DCUID: " + log.DeliveryConstructUId, "IterationEntityUId: " + IterationEntityUId, string.Empty);
                        UpdateNotificationStatus(log._id, true, "C", "Skipped as sender app is Ingrain");
                    }
                    else
                    {                        
                        bool resp = false;
                        bool containsProvisionedDCs = false;
                        // for Iteration / Release --- not in use as Iteration flow s changed wih scheduler  phenix iteration
                        if (log.EntityUId == IterationEntityUId)
                        {
                            LogInfoMessageToDB(log.ClientUId, log.DeliveryConstructUId, "", IterationEntityUId, "", "Prediction", "ENS Notification start Id:" + Convert.ToString(log._id), null);
                            string ensPayload = GetENSPayload(log.CallbackLink);
                            if (!string.IsNullOrEmpty(ensPayload))
                            {                                
                                List<WINSERVICEMODELS.PhoenixPayloads.ENSIteration> eNSIteration = JsonConvert.DeserializeObject<List<WINSERVICEMODELS.PhoenixPayloads.ENSIteration>>(ensPayload);

                                foreach (var iteration in eNSIteration)
                                {
                                    if (iteration.IterationDeliveryConstructs != null)
                                    {
                                        var checkDCs = iteration.IterationDeliveryConstructs.Where(x => provisionedDCs.Contains(x.DeliveryConstructUId)).Select(x => x.DeliveryConstructUId).ToList();

                                        if (checkDCs.Count > 0)
                                        {
                                            containsProvisionedDCs = true;                                            
                                            if (CheckSSAITrainedModels(log.ClientUId, checkDCs[0]))
                                            {
                                                LogInfoMessageToDB(log.ClientUId, checkDCs[0], "", IterationEntityUId, iteration.IterationUId, "Prediction", "Item in ENS Notification Id:" + Convert.ToString(log._id), null);
                                                resp = UpdateIterationPredictions(log.ClientUId, checkDCs[0], iteration.IterationUId);
                                            }

                                        }
                                    }

                                }

                                if (!containsProvisionedDCs)
                                {
                                    UpdateNotificationStatus(log._id, true, "E", "ENS call back payload doesnot contain provisioned DCs");
                                }
                                else if (resp)
                                {
                                    UpdateNotificationStatus(log._id, true, "C", "Successfully Processed");

                                }
                                else
                                {
                                    UpdateNotificationStatus(log._id, true, "E", "Error in prediction");
                                }
                            }
                            else
                            {
                                UpdateNotificationStatus(log._id, false, "E", "ENS callback payload is null");
                            }

                        }
                        else if (log.EntityUId == CREntityUId)
                        {
                            LogInfoMessageToDB(log.ClientUId, log.DeliveryConstructUId, "", CREntityUId, "", "Prediction", "ENS Notification start Id:" + Convert.ToString(log._id), null);
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(PredictionSchedulerService), nameof(UpdatePredictionBasedOnENSNotification), "else if - CREntityUId" + CREntityUId, "ClientUID: " + log.ClientUId, "DCUID: " + log.DeliveryConstructUId, "IterationEntityUId: " + IterationEntityUId, string.Empty);
                            //callback url  - multiple crs
                            string ensPayload = GetENSPayload(log.CallbackLink);
                            if (!string.IsNullOrEmpty(ensPayload))
                            {
                                List<WINSERVICEMODELS.PhoenixPayloads.ENSChangeRequest> eNSChangeRequest = JsonConvert.DeserializeObject<List<WINSERVICEMODELS.PhoenixPayloads.ENSChangeRequest>>(ensPayload);
                                foreach (var cr in eNSChangeRequest)
                                {
                                    if (cr.ChangeRequestExtensions != null)
                                    {
                                        var ofieldValues = cr.ChangeRequestExtensions.Where(x => x.FieldName == "ChangedAttributes").Select(x => x.FieldValue).ToList();
                                        if (ofieldValues.Count > 0)
                                        {
                                            foreach (var value in ofieldValues[0].Split(","))
                                            {
                                                if (CRENSfieldvalues.Contains(value))
                                                {
                                                    bTriggerCRENSPredictions = true;
                                                    break;
                                                }
                                            }
                                        }
                                    }


                                    if (cr.ChangeRequestDeliveryConstructs != null && bTriggerCRENSPredictions)
                                    {
                                        var checkDCs = cr.ChangeRequestDeliveryConstructs.Where(x => provisionedDCs.Contains(x.DeliveryConstructUId)).Select(x => x.DeliveryConstructUId).ToList();
                                        if (checkDCs.Count > 0)
                                        {
                                            containsProvisionedDCs = true;                                            
                                            if (CheckAITrainedModels(log.ClientUId, checkDCs[0]))
                                            {
                                                LogInfoMessageToDB(log.ClientUId, checkDCs[0], "", CREntityUId, cr.ChangeRequestUId, "Prediction", "Item in ENS Notification Id:" + Convert.ToString(log._id), null);
                                                //query api to phoenix - full payload
                                                resp = UpdateChangeRequestPredictions(log.ClientUId, checkDCs[0], cr.ChangeRequestUId);
                                            }
                                            else
                                            {
                                                LogInfoMessageToDB(log.ClientUId, checkDCs[0], "", CREntityUId, cr.ChangeRequestUId, "Prediction", "No trained models available", null);
                                            }

                                        }
                                    }


                                }
                                if (!containsProvisionedDCs)
                                {
                                    UpdateNotificationStatus(log._id, true, "E", "ENS call back payload doesnot contain provisioned DCs");
                                }
                                else if (resp)
                                {
                                    UpdateNotificationStatus(log._id, true, "C", "Successfully Processed");

                                }
                                else
                                {
                                    UpdateNotificationStatus(log._id, true, "E", "Error in prediction");
                                }

                            }
                            else
                            {
                                UpdateNotificationStatus(log._id, false, "E", "ENS callback payload is null");
                            }

                        }
                        else
                        {
                            UpdateNotificationStatus(log._id, true, "C", "Skipped Notification,since its not Iteration or CR");
                        }
                    }

                }
                catch (Exception ex)
                {
                    LOGGING.LogManager.Logger.LogErrorMessage(typeof(PredictionSchedulerService), nameof(UpdatePredictionBasedOnENSNotification), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                    UpdateNotificationStatus(log._id, false, "E", ex.Message);
                }
            }
        }


        public void UpdateNotificationStatus(ObjectId id, bool isProcessed, string status, string statusMessage)
        {
            var ensLogCollection = _database.GetCollection<DATAMODELS.ENSEntityNotificationLog>(CONSTANTS.ENSEntityNotificationLog);
            var filter = Builders<DATAMODELS.ENSEntityNotificationLog>.Filter.Where(x => x._id == id);
            var projection = Builders<DATAMODELS.ENSEntityNotificationLog>.Projection.Exclude("_id");
            var result = ensLogCollection.Find(filter).Project<DATAMODELS.ENSEntityNotificationLog>(projection).FirstOrDefault();
            if (result != null)
            {
                int? retryCount = result.RetryCount == null ? 1 : result.RetryCount + 1;

                var updateBuilder = Builders<DATAMODELS.ENSEntityNotificationLog>.Update.Set(x => x.isProcessed, isProcessed)
                                                                     .Set(x => x.ProcessedStatus, status)
                                                                     .Set(x => x.ProcessedStatusMessage, statusMessage)
                                                                     .Set(x => x.RetryCount, retryCount)
                                                                     .Set(x => x.ModifiedOn, DateTime.UtcNow.ToString());
                ensLogCollection.UpdateOne(filter, updateBuilder);

            }
        }

        public bool CheckIfPredictionCompleted(string clientUId, string deliveryConstructUId, string entityUId, string itemUId)
        {
            try
            {
                var logCollection = _database.GetCollection<WINSERVICEMODELS.PredictionSchedulerLog>(nameof(WINSERVICEMODELS.PredictionSchedulerLog));
                var filter = Builders<WINSERVICEMODELS.PredictionSchedulerLog>.Filter.Where(x => x.ClientUId == clientUId)
                             & Builders<WINSERVICEMODELS.PredictionSchedulerLog>.Filter.Where(x => x.DeliveryConstructUId == deliveryConstructUId)
                             & Builders<WINSERVICEMODELS.PredictionSchedulerLog>.Filter.Where(x => x.EntityUId == entityUId)
                             & Builders<WINSERVICEMODELS.PredictionSchedulerLog>.Filter.Where(x => x.ItemUId == itemUId)
                             & Builders<WINSERVICEMODELS.PredictionSchedulerLog>.Filter.Where(x => x.Status == "C");
                var projection = Builders<WINSERVICEMODELS.PredictionSchedulerLog>.Projection.Exclude("_id");
                var result = logCollection.Find(filter).Project<WINSERVICEMODELS.PredictionSchedulerLog>(projection).ToList();
                if (result != null)
                {
                    if (result.Count > 0)
                    {
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                return false;
            }

        }
        public void UpdatePredictionCompletedtoM(string clientUId, string deliveryConstructUId, string entityUId, string itemUId)
        {
            try
            {
                var logCollection = _database.GetCollection<WINSERVICEMODELS.PredictionSchedulerLog>(nameof(WINSERVICEMODELS.PredictionSchedulerLog));
                var filter = Builders<WINSERVICEMODELS.PredictionSchedulerLog>.Filter.Where(x => x.ClientUId == clientUId)
                             & Builders<WINSERVICEMODELS.PredictionSchedulerLog>.Filter.Where(x => x.DeliveryConstructUId == deliveryConstructUId)
                             & Builders<WINSERVICEMODELS.PredictionSchedulerLog>.Filter.Where(x => x.EntityUId == entityUId)
                             & Builders<WINSERVICEMODELS.PredictionSchedulerLog>.Filter.Where(x => x.ItemUId == itemUId)
                             & Builders<WINSERVICEMODELS.PredictionSchedulerLog>.Filter.Where(x => x.Status == "C");

                var update = Builders<WINSERVICEMODELS.PredictionSchedulerLog>.Update.Set(x => x.Status, "M");
                logCollection.UpdateMany(filter, update);

            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(PredictionSchedulerService), nameof(UpdatePredictionCompletedtoM), ex.Message, ex, string.Empty, string.Empty, clientUId, deliveryConstructUId);
            }

        }

        public bool UpdateChangeRequestPredictions(string clientUId, string deliveryConstructUId, string changeRequestUId)
        {
            bool isSuccess = false;
            bool predResponse = false;
            try
            {
                LogInfoMessageToDB(clientUId, deliveryConstructUId, "", CREntityUId, changeRequestUId, "Prediction", "Started Prediction", null);
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(PredictionSchedulerService), nameof(UpdateChangeRequestPredictions), "Started Prediction", string.Empty, string.Empty, string.Empty, string.Empty);
                //query api to phoenix - full payload
                string crPayload = GetEntityPayload(clientUId, deliveryConstructUId, "ChangeRequest", changeRequestUId);
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
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(PredictionSchedulerService), nameof(UpdateChangeRequestPredictions), "Recommendations - Null/Empty Assiging values", "ItemUId/ChangeRequestUId: " + changeRequestUId, "ItemExternalId/ChangeRequestExternalId: " + changeRequest.ChangeRequests[0].ChangeRequestExternalId, string.Empty, "newRec: " + newRec);
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


                    foreach (var usecaseId in AIUseCaseIds)
                    {
                        DATAMODELS.CallBackErrorLog callBackErrorLog = new DATAMODELS.CallBackErrorLog
                        {
                            CorrelationId = changeRequestUId,
                            RequestId = Guid.NewGuid().ToString(),
                            UseCaseId = usecaseId,
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
                        JObject predictionInputData = new JObject();
                        string recommendationTypeUId = null;
                        if (usecaseId == IA_CRUseCaseId)
                        {
                            predictionInputData["changerequestuid"] = changeRequest.ChangeRequests[0].ChangeRequestUId;
                            predictionInputData["title"] = changeRequest.ChangeRequests[0].Title;
                            predictionInputData["description"] = changeRequest.ChangeRequests[0].Description;
                            recommendationTypeUId = RecommendationTypeUId_SimilarCR;
                        }

                        if (usecaseId == IA_UserStoryUseCaseId)
                        {
                            predictionInputData["Title"] = changeRequest.ChangeRequests[0].Title;
                            predictionInputData["Description"] = changeRequest.ChangeRequests[0].Description;
                            recommendationTypeUId = RecommendationTypeUId_SimilarUserStory;
                        }

                        if (usecaseId == IA_RequirementUseCaseId)
                        {
                            predictionInputData["title"] = changeRequest.ChangeRequests[0].Title;
                            predictionInputData["description"] = changeRequest.ChangeRequests[0].Description;
                            recommendationTypeUId = RecommendationTypeUId_SimilarRequirement;
                        }


                        // Get prediction results
                        string response = AIServicePredictionAPI(predictionInputData, clientUId, deliveryConstructUId, usecaseId, IA_ServiceId);


                        // Build recommendation attribute
                        if (!string.IsNullOrEmpty(response))
                        {
                            JObject result = JObject.Parse(response);
                            if (result != null && (bool)result["IsSuccess"] == true)
                            {
                                LogInfoMessageToDB(clientUId, deliveryConstructUId, "", CREntityUId, changeRequestUId, "Prediction", "Prediction Response - Success", null);
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
                                                if (usecaseId == IA_CRUseCaseId && predictions[i]["changerequestuid"].ToString() != "Not found")
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

                                                if (usecaseId == IA_UserStoryUseCaseId && predictions[i]["workitemuid"].ToString() != "Not found")
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

                                                if (usecaseId == IA_RequirementUseCaseId && predictions[i]["requirementuid"].ToString() != "Not found")
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
                                    LogErrorMessageToDB(clientUId, deliveryConstructUId, "", CREntityUId, changeRequestUId, "Prediction", "Prediction Response - Ingrain python api gave false in response status", null);
                                }
                            }
                            else
                            {
                                LogErrorMessageToDB(clientUId, deliveryConstructUId, "", CREntityUId, changeRequestUId, "Prediction", "Prediction Response - Error", null);
                            }


                        }
                        else
                        {
                            LogErrorMessageToDB(clientUId, deliveryConstructUId, "", CREntityUId, changeRequestUId, "Prediction", "Null response from Ingrain Prediction API", null);
                        }

                    }


                    //string crPayloadNew = GetEntityPayload(clientUId, deliveryConstructUId, "ChangeRequest", changeRequestUId);
                    //if (string.IsNullOrEmpty(crPayloadNew))
                    //{
                    //    throw new InvalidDataException("No Data in Phoenix Get API");
                    //}

                    //dynamic changeRequestNew = JsonConvert.DeserializeObject<dynamic>(crPayloadNew);


                    var payload = new
                    {
                        Recommendation = Base64Encode(JsonConvert.SerializeObject(entityRecommendation))
                    };
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(PredictionSchedulerService), nameof(UpdateChangeRequestPredictions), "Base64 - " + payload.Recommendation, string.Empty, string.Empty, string.Empty, string.Empty);
                    //changeRequestNew.ChangeRequests[0].ModifiedOn = DateTime.UtcNow.ToString("o");
                    //changeRequestNew.ChangeRequests[0].ModifiedByApp = "myWizard.IngrAIn";
                    string correlationUId = entityRecommendation["CorrelationUId"].ToString();
                    // changeRequestNew.ChangeRequests[0].CorrelationUId = Guid.NewGuid().ToString();

                    string res = string.Empty;
                    if (predResponse)
                    {
                        res = UpdateEntityRecommendationInPhoenix(payload, clientUId, deliveryConstructUId, correlationUId, "ChangeRequest");
                    }

                    if (string.IsNullOrEmpty(res))
                    {
                        LogErrorMessageToDB(clientUId, deliveryConstructUId, "", CREntityUId, changeRequestUId, "Prediction", "Completed - " + res, null);
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(PredictionSchedulerService), nameof(UpdateChangeRequestPredictions), "Update Phoenix - Prediction Completed but Failed", string.Empty, string.Empty, string.Empty, string.Empty);
                    }
                    else
                    {
                        LogInfoMessageToDB(clientUId, deliveryConstructUId, "", CREntityUId, changeRequestUId, "Prediction", "Completed -" + res + "-Updated CorrelationId from -" + correlationUId + " -to- " + correlationUId, "C");                        
                        isSuccess = true;
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(PredictionSchedulerService), nameof(UpdateIterationPredictions), "Update Phoenix - Prediction Completed Success", string.Empty, string.Empty, string.Empty, string.Empty);
                    }
                }

            }
            catch (Exception ex)
            {
                LogErrorMessageToDB(clientUId, deliveryConstructUId, "", CREntityUId, changeRequestUId, "Prediction", ex.ToString(), null);
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(PredictionSchedulerService), nameof(UpdateChangeRequestPredictions), ex.Message, ex, string.Empty, string.Empty, clientUId, deliveryConstructUId);
            }
            return isSuccess;
        }


        public bool UpdateBulkIterationPredictions(string clientUId, string deliveryConstructUId, List<string> iterationUId)
        {
            List<string> iterationUId_check = iterationUId;
            bool isSuccess = false;
            bool predResponse = false;
            try
            {
                List<WINSERVICEMODELS.PhoenixPayloads.IterationPredictions> predictions = new List<WINSERVICEMODELS.PhoenixPayloads.IterationPredictions>();
                foreach (var usecaseId in SSAIUseCaseIds)
                {
                    var model = CheckIfSSAIModelDeployed(clientUId, deliveryConstructUId, usecaseId, PredictionSchedulerUserName);
                    if (model != null)
                    {
                        string response = SSAIBulkPredictionAPI(clientUId, deliveryConstructUId, usecaseId, iterationUId); // pass as list
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
                                    var recommendationTypeUId = usecaseId == IA_DefectRateUseCaseId ? RecommendationTypeUId_DefectRate : RecommendationTypeUId_SPI;
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
                                        UsecaseId = usecaseId,
                                        IntelligentRecommendations = newRecommendation
                                    };

                                    predictions.Add(prediction);


                                    LogInfoMessageToDB(clientUId, deliveryConstructUId, "", correlationId, modelUniqueId, IterationEntityUId, string.Join(",", iterationUId), "Prediction", "Response", null);

                                }

                            }
                            else
                            {
                                LogInfoMessageToDB(clientUId, deliveryConstructUId, "", correlationId, modelUniqueId, IterationEntityUId, string.Join(",", iterationUId), "Prediction", "Ingrain Prediction API response error -" + result["Message"].ToString(), null);
                            }

                        }
                        else
                        {
                            LogInfoMessageToDB(clientUId, deliveryConstructUId, "", IterationEntityUId, string.Join(",", iterationUId), "Prediction", "Null response from Ingrain Prediction API response error", null);
                        }
                    }




                    //

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



                            LogInfoMessageToDB(clientUId, deliveryConstructUId, "", IterationEntityUId, uniteration, "Prediction", "Started Prediction", null);
                            string iterationPayload = GetEntityPayload(clientUId, deliveryConstructUId, "Iteration", uniteration);
                            if (string.IsNullOrEmpty(iterationPayload))
                            {
                                UpdateIterationRecommendationStatusinDB(clientUId, deliveryConstructUId, uniteration, false, "Failed");
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
                                    UseCaseId = usecaseId,
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

                        //string iterationPayloadNew = GetEntityPayload(clientUId, deliveryConstructUId, "Iteration", iterationUId);
                        //if (string.IsNullOrEmpty(iterationPayloadNew))
                        //{
                        //    UpdateIterationRecommendationStatusinDB(clientUId, deliveryConstructUId, iterationUId, false, "Failed");
                        //    throw new InvalidDataException("No Data in Phoenix Get API");
                        //}
                        //dynamic iterationNew = JsonConvert.DeserializeObject<dynamic>(iterationPayloadNew);
                        var payload = new
                        {
                            Recommendation = Base64Encode(JsonConvert.SerializeObject(RecommendationsPayladList, Formatting.None))
                        };
                        //    iterationNew.Iterations[0].Recommendations = Base64Encode(JsonConvert.SerializeObject(entityRecommendation, Formatting.None));
                        //    iterationNew.Iterations[0].ModifiedAtSourceOn = DateTime.UtcNow.ToString("o");
                        //    iterationNew.Iterations[0].ModifiedByApp = "myWizard.IngrAIn";
                        //    string oldCorrelationUId = iterationNew.Iterations[0].CorrelationUId;
                        //iterationNew.Iterations[0].CorrelationUId = Guid.NewGuid().ToString();
                        //string correlationUid = entityRecommendation["CorrelationUId"].ToString();
                        string res = string.Empty;

                        //if (predResponse)
                        //{
                        //res = UpdateEntityRecommendationInPhoenix(iterationNew.Iterations, clientUId, deliveryConstructUId, (string)iterationNew.Iterations[0].CorrelationUId, "Iteration");
                        res = UpdateEntityRecommendationInPhoenix(payload, clientUId, deliveryConstructUId, correlationUid, "Iteration");

                        // }

                        if (string.IsNullOrEmpty(res))
                        {
                            LogErrorMessageToDB(clientUId, deliveryConstructUId, "", IterationEntityUId, string.Join(",", mergeIterationIdList), "Prediction", "Completed -" + res, null);
                            UpdateBulkIterationRecommendationStatusinDB(clientUId, deliveryConstructUId, mergeIterationIdList, false, "Failed"); // bulk update
                        }
                        else
                        {
                            LogInfoMessageToDB(clientUId, deliveryConstructUId, "", IterationEntityUId, string.Join(",", mergeIterationIdList), "Prediction", "Completed -" + res + "-Updated CorrelationId from -" + correlationUid + " -to- " + correlationUid, "C");
                            isSuccess = true;
                            UpdateBulkIterationRecommendationStatusinDB(clientUId, deliveryConstructUId, mergeIterationIdList, true, "Success"); // bulk update
                        }

                        if (excludedIterations > 0)
                        {
                            LogErrorMessageToDB(clientUId, deliveryConstructUId, "", IterationEntityUId, string.Join(",", iterationUId_check), "Prediction", "Completed -" + res, null);
                            UpdateBulkIterationRecommendationStatusinDB(clientUId, deliveryConstructUId, iterationUId_check, false, "Failed"); // bulk update
                        }
                    }
                    else
                    {
                        LogErrorMessageToDB(clientUId, deliveryConstructUId, "", IterationEntityUId, string.Join(",", iterationUId), "Prediction", "No Response from python -", null);
                        UpdateBulkIterationRecommendationStatusinDB(clientUId, deliveryConstructUId, iterationUId, false, "Failed"); // bulk update
                    }

                }
            }
            catch (Exception ex)
            {
                //update for all iterationUid loop
                UpdateBulkIterationRecommendationStatusinDB(clientUId, deliveryConstructUId, iterationUId, false, "Failed"); // need to check // bulk update
                LogErrorMessageToDB(clientUId, deliveryConstructUId, "", string.Join(",", iterationUId), string.Join(",", iterationUId), "Prediction", ex.ToString(), null);
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(PredictionSchedulerService), nameof(UpdateIterationPredictions), ex.Message, ex, string.Empty, string.Empty, clientUId, deliveryConstructUId);
            }
            return isSuccess;

        }



        public bool UpdateIterationPredictions(string clientUId, string deliveryConstructUId, string iterationUId)
        {
            bool isSuccess = false;
            bool predResponse = false;
            try
            {
                LogInfoMessageToDB(clientUId, deliveryConstructUId, "", IterationEntityUId, iterationUId, "Prediction", "Started Prediction", null);
                string iterationPayload = GetEntityPayload(clientUId, deliveryConstructUId, "Iteration", iterationUId);
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

                foreach (var usecaseId in SSAIUseCaseIds)
                {
                    string response = SSAIPredictionAPI(clientUId, deliveryConstructUId, usecaseId, iterationUId);
                    if (!string.IsNullOrEmpty(response))
                    {
                        JObject result = JObject.Parse(response);
                        if (result["Status"].ToString() == "C")
                        {
                            JArray predictionResult = JArray.Parse(result["PredictedData"].ToString());

                            string predictedValue = predictionResult[0]["predictedValue"].ToString();

                            var recommendationTypeUId = usecaseId == IA_DefectRateUseCaseId ? RecommendationTypeUId_DefectRate : RecommendationTypeUId_SPI;
                            


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
                            LogInfoMessageToDB(clientUId, deliveryConstructUId, "", IterationEntityUId, iterationUId, "Prediction", "Ingrain Prediction API response error -" + result["Message"].ToString(), null);
                        }

                    }
                    else
                    {
                        LogInfoMessageToDB(clientUId, deliveryConstructUId, "", IterationEntityUId, iterationUId, "Prediction", "Null response from Ingrain Prediction API response error", null);
                    }
                }


                //string iterationPayloadNew = GetEntityPayload(clientUId, deliveryConstructUId, "Iteration", iterationUId);
                //if (string.IsNullOrEmpty(iterationPayloadNew))
                //{
                //    UpdateIterationRecommendationStatusinDB(clientUId, deliveryConstructUId, iterationUId, false, "Failed");
                //    throw new InvalidDataException("No Data in Phoenix Get API");
                //}
                //dynamic iterationNew = JsonConvert.DeserializeObject<dynamic>(iterationPayloadNew);
                var payload = new
                {
                    Recommendation = Base64Encode(JsonConvert.SerializeObject(entityRecommendation, Formatting.None))
                };                
                //    iterationNew.Iterations[0].Recommendations = Base64Encode(JsonConvert.SerializeObject(entityRecommendation, Formatting.None));
                //    iterationNew.Iterations[0].ModifiedAtSourceOn = DateTime.UtcNow.ToString("o");
                //    iterationNew.Iterations[0].ModifiedByApp = "myWizard.IngrAIn";
                //    string oldCorrelationUId = iterationNew.Iterations[0].CorrelationUId;
                //iterationNew.Iterations[0].CorrelationUId = Guid.NewGuid().ToString();
                string correlationUid = entityRecommendation["CorrelationUId"].ToString();
                string res = string.Empty;

                if (predResponse)
                {
                    //res = UpdateEntityRecommendationInPhoenix(iterationNew.Iterations, clientUId, deliveryConstructUId, (string)iterationNew.Iterations[0].CorrelationUId, "Iteration");
                    res = UpdateEntityRecommendationInPhoenix(payload, clientUId, deliveryConstructUId, correlationUid, "Iteration");

                }

                if (string.IsNullOrEmpty(res))
                {
                    LogErrorMessageToDB(clientUId, deliveryConstructUId, "", IterationEntityUId, iterationUId, "Prediction", "Completed -" + res, null);
                    UpdateIterationRecommendationStatusinDB(clientUId, deliveryConstructUId, iterationUId, false, "Failed");                    
                }
                else
                {
                    LogInfoMessageToDB(clientUId, deliveryConstructUId, "", IterationEntityUId, iterationUId, "Prediction", "Completed -" + res + "-Updated CorrelationId from -" + correlationUid + " -to- " + correlationUid, "C");
                    isSuccess = true;
                    UpdateIterationRecommendationStatusinDB(clientUId, deliveryConstructUId, iterationUId, true, "Success");                    
                }


            }
            catch (Exception ex)
            {
                UpdateIterationRecommendationStatusinDB(clientUId, deliveryConstructUId, iterationUId, false, "Failed");
                LogErrorMessageToDB(clientUId, deliveryConstructUId, "", iterationUId, iterationUId, "Prediction", ex.ToString(), null);
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(PredictionSchedulerService), nameof(UpdateIterationPredictions), ex.Message, ex, string.Empty, string.Empty, clientUId, deliveryConstructUId);
            }
            return isSuccess;

        }


        public List<string> FetchSSAIColumnUniqueValues(string correlationId, string columnName)
        {
            List<string> colUniqueValues = null;
            var filteredDataCol = _database.GetCollection<BsonDocument>(CONSTANTS.DataCleanUPFilteredData);
            var filter = Builders<BsonDocument>.Filter.Eq("CorrelationId", correlationId);
            var result = filteredDataCol.Find(filter).FirstOrDefault();
            if (result != null)
            {
                colUniqueValues = JsonConvert.DeserializeObject<List<string>>(result["ColumnUniqueValues"][columnName].ToJson());
            }
            return colUniqueValues;
        }

        public List<string> FetchAIColumnUniqueValues(string correlationId, string columnName)
        {
            List<string> colUniqueValues = null;
            var ingestDataCol = _database.GetCollection<BsonDocument>(CONSTANTS.AIServiceIngestData);
            var filter = Builders<BsonDocument>.Filter.Eq("CorrelationId", correlationId);
            var projectionBuilder = Builders<BsonDocument>.Projection.Include("ColumnUniqueValues");
            var result = ingestDataCol.Find(filter).Project<BsonDocument>(projectionBuilder).FirstOrDefault();
            if (result != null)
            {
                colUniqueValues = JsonConvert.DeserializeObject<List<string>>(result["ColumnUniqueValues"][columnName].ToJson());
            }
            return colUniqueValues;
        }

        private string GetENSPayload(string callbackLink)
        {
            try
            {
                string token = GenerateToken();
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Accept.Clear();
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                    httpClient.DefaultRequestHeaders.Add("AppServiceUID", appSettings.AppServiceUId);
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                    var result = httpClient.GetAsync(callbackLink).Result;
                    if (result.StatusCode != HttpStatusCode.OK)
                    {
                        throw new Exception($"{result.Content}" + "" + $"{result.StatusCode}");
                    }

                    return result.Content.ReadAsStringAsync().Result;
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(PredictionSchedulerService), nameof(GetENSPayload), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return null;
            }

        }


        private string GetEntityPayload(string clientUId, string deliveryConstructUId, string entityName, string itemUId)
        {            
            string token = GenerateToken();
            string jsonResult = string.Empty;
            if (entityName == "ChangeRequest")
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(PredictionSchedulerService), nameof(GetEntityPayload), "ChangeRequest", "Entity Name" + entityName, string.Empty, string.Empty, string.Empty);
                string apiPath = String.Format(_cRQueryAPI, clientUId, deliveryConstructUId);
                var postContent = new
                {
                    ClientUId = clientUId,
                    DeliveryConstructUId = deliveryConstructUId,
                    ChangeRequestUId = itemUId,
                    IncludeRecommendations = true
                };
                StringContent content = new StringContent(JsonConvert.SerializeObject(postContent), Encoding.UTF8, "application/json");
                HttpResponseMessage httpResponseMessage = PhoenixPOSTRequest(token, appSettings.myWizardAPIUrl, apiPath, content, null);
                if (httpResponseMessage.StatusCode == HttpStatusCode.OK)
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(PredictionSchedulerService), nameof(GetEntityPayload), "Status 200 - OK", string.Empty, string.Empty, string.Empty, string.Empty);
                    jsonResult = httpResponseMessage.Content.ReadAsStringAsync().Result;
                }
                else
                {
                    //add logger
                    LogErrorMessageToDB(clientUId, deliveryConstructUId, "", CREntityUId, itemUId, "Prediction", "Phoenix Get query error - " + httpResponseMessage.StatusCode + "-" + httpResponseMessage.ReasonPhrase.ToString(), null);
                }

            }
            else if (entityName == "Iteration")
            {                
                string apiPath = String.Format(_iterationQueryAPI, clientUId, deliveryConstructUId);
                var postContent = new
                {
                    ClientUId = clientUId,
                    DeliveryConstructUId = deliveryConstructUId,
                    IterationUId = itemUId,
                    IncludeRecommendations = true
                };
                StringContent content = new StringContent(JsonConvert.SerializeObject(postContent), Encoding.UTF8, "application/json");
                HttpResponseMessage httpResponseMessage = PhoenixPOSTRequest(token, appSettings.myWizardAPIUrl, apiPath, content, null);
                if (httpResponseMessage.StatusCode == HttpStatusCode.OK)
                {                    
                    jsonResult = httpResponseMessage.Content.ReadAsStringAsync().Result;
                }
                else
                {
                    // add logger
                    LogErrorMessageToDB(clientUId, deliveryConstructUId, "", IterationEntityUId, itemUId, "Prediction", "Phoenix Get query error - " + httpResponseMessage.StatusCode + "-" + httpResponseMessage.ReasonPhrase.ToString(), null);
                }
            }
            return jsonResult;
        }



        public string InvokeGetMethod(string routeUrl, string appServiceUId)
        {
            string token = GenerateToken();
            using (var httpClient = new HttpClient())
            {

                httpClient.BaseAddress = new Uri(appSettings.myWizardAPIUrl);
                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                httpClient.DefaultRequestHeaders.Add("AppServiceUID", appSettings.AppServiceUId);
                var ClinetStruct = String.Format(routeUrl);
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                var result = httpClient.GetAsync(ClinetStruct).Result;
                if (result.StatusCode != HttpStatusCode.OK)
                {
                    throw new Exception($"{result.Content}" + "" + $"{result.StatusCode}");
                }

                return result.Content.ReadAsStringAsync().Result;
            }
        }



        public string GetValidClientId(string appserviceUId)
        {
            //LOGGING.LogManager.Logger.LogProcessInfo(typeof(PredictionSchedulerService), nameof(GetValidClientId), "FETCH CLIENTS START");
            string clientUId = null;
            string routeUrl = "AccountClients?clientUId=" + appSettings.ClientUID + "&deliveryConstructUId=null";

            var jsonStringResult = InvokeGetMethod(routeUrl, appserviceUId);

            if (!string.IsNullOrEmpty(jsonStringResult))
            {
                List<WINSERVICEMODELS.ClientDetails> clientDetails = JsonConvert.DeserializeObject<List<WINSERVICEMODELS.ClientDetails>>(jsonStringResult);

                if (clientDetails.Count > 0)
                {
                    clientUId = clientDetails[0].ClientUId;
                }
            }
            return clientUId;
        }



        public WINSERVICEMODELS.AppDeliveryConstructs FetchClientsDeliveryConstructs(string appserviceUId)
        {
            WINSERVICEMODELS.AppDeliveryConstructs appDeliveryConstructs = null;
            string validClientUId = GetValidClientId(appserviceUId);
            string routeUrl = "AppService?clientUId=" + validClientUId + "&deliveryConstructUId=00000000-0000-0000-0000-000000000000&appServiceUId=" + appserviceUId + "&languageUId=null";
            string jsonResult = InvokeGetMethod(routeUrl, appserviceUId);
            if (!string.IsNullOrEmpty(jsonResult))
            {
                appDeliveryConstructs = JsonConvert.DeserializeObject<WINSERVICEMODELS.AppDeliveryConstructs>(jsonResult);

            }
            return appDeliveryConstructs;
        }






        public string GetAutoTrainStatus(string correlationId)
        {
            var ingrainRequestCollection = _database.GetCollection<DATAMODELS.IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
            var ingrainRequestFilterBuilder = Builders<DATAMODELS.IngrainRequestQueue>.Filter;
            var ingrainRequestFilterQueue = ingrainRequestFilterBuilder.Where(x => x.CorrelationId == correlationId)
                                            & ingrainRequestFilterBuilder.Where(x => x.Function == CONSTANTS.AutoTrain);
            var Projection1 = Builders<DATAMODELS.IngrainRequestQueue>.Projection.Exclude("_id");
            var ingrainRequestQueueResult = ingrainRequestCollection.Find(ingrainRequestFilterQueue).Project<DATAMODELS.IngrainRequestQueue>(Projection1).FirstOrDefault();
            if (ingrainRequestQueueResult != null)
            {
                if (ingrainRequestQueueResult.Status == CONSTANTS.C)
                {
                    return "C";
                }
                else if (ingrainRequestQueueResult.Status == CONSTANTS.E)
                {
                    return "E";
                }
                else
                {
                    return "P";
                }
            }
            else
            {
                return "E";
            }
        }

        public DATAMODELS.DeployModelsDto CheckIfSSAIModelDeployed(string clientId, string deliverConstructId, string usecaseId, string userId)
        {
            string encryptedUser = userId;
            if (!string.IsNullOrEmpty(Convert.ToString(encryptedUser)))
            {
                if (appSettings.IsAESKeyVault)
                {
                    encryptedUser = CryptographyUtility.Encrypt(Convert.ToString(encryptedUser));
                }
                else
                {
                    encryptedUser = AesProvider.Encrypt(Convert.ToString(encryptedUser), appSettings.aesKey, appSettings.aesVector);
                }
            }
            var deployModelCollection = _database.GetCollection<DATAMODELS.DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
            var modelfilter = Builders<DATAMODELS.DeployModelsDto>.Filter;
            var modelfilterVal = modelfilter.Where(x => x.ClientUId == clientId)
                                 & modelfilter.Where(x => x.DeliveryConstructUID == deliverConstructId)
                                 & modelfilter.Where(x => x.TemplateUsecaseId == usecaseId)
                                 & modelfilter.Where(x => (x.CreatedByUser == userId || x.CreatedByUser == encryptedUser))
            & modelfilter.Where(x => x.Status == "Deployed");
            var projection = Builders<DATAMODELS.DeployModelsDto>.Projection.Exclude("_id");
            return deployModelCollection.Find(modelfilterVal).Project<DATAMODELS.DeployModelsDto>(projection).FirstOrDefault();

        }

        public DATAMODELS.DeployModelsDto CheckIfSSAIModelTrained(string clientId, string deliverConstructId, string usecaseId, string userId)
        {
            string encryptedUser = userId;
            if (!string.IsNullOrEmpty(Convert.ToString(encryptedUser)))
            {
                if (appSettings.IsAESKeyVault)
                {
                    encryptedUser = CryptographyUtility.Encrypt(Convert.ToString(encryptedUser));
                }
                else
                {
                    encryptedUser = AesProvider.Encrypt(Convert.ToString(encryptedUser), appSettings.aesKey, appSettings.aesVector);
                }
            }
            var deployModelCollection = _database.GetCollection<DATAMODELS.DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
            var modelfilter = Builders<DATAMODELS.DeployModelsDto>.Filter;
            var modelfilterVal = modelfilter.Where(x => x.ClientUId == clientId)
                                 & modelfilter.Where(x => x.DeliveryConstructUID == deliverConstructId)
                                 & modelfilter.Where(x => x.TemplateUsecaseId == usecaseId)
                                 & modelfilter.Where(x => (x.CreatedByUser == userId || x.CreatedByUser == encryptedUser));
            var projection = Builders<DATAMODELS.DeployModelsDto>.Projection.Exclude("_id");
            return deployModelCollection.Find(modelfilterVal).Project<DATAMODELS.DeployModelsDto>(projection).FirstOrDefault();

        }

        public AICoreModels CheckIfAIModelTrained(string clientId, string deliverConstructId, string usecaseId, string serviceId, string userId)
        {
            string encryptedUser = userId;
            if (!string.IsNullOrEmpty(Convert.ToString(encryptedUser)))
            {
                if (appSettings.IsAESKeyVault)
                {
                    encryptedUser = CryptographyUtility.Encrypt(Convert.ToString(encryptedUser));
                }
                else
                {
                    encryptedUser = AesProvider.Encrypt(Convert.ToString(encryptedUser), appSettings.aesKey, appSettings.aesVector);
                }
            }
            string correlationId = null;
            var modelCollection = _database.GetCollection<AICoreModels>(CONSTANTS.AICoreModels);
            var modelfilter = Builders<AICoreModels>.Filter;
            var modelfilterVal = modelfilter.Where(x => x.ClientId == clientId)
                                 & modelfilter.Where(x => x.DeliveryConstructId == deliverConstructId)
                                 & modelfilter.Where(x => x.ServiceId == serviceId)
                                 & modelfilter.Where(x => x.UsecaseId == usecaseId)
                                 & modelfilter.Where(x => (x.CreatedBy == userId || x.CreatedBy == encryptedUser));
            var projection = Builders<AICoreModels>.Projection.Exclude("_id");
            var model = modelCollection.Find(modelfilterVal).Project<AICoreModels>(projection).FirstOrDefault();
            if (model != null)
            {
                LogInfoMessageToDB(clientId, deliverConstructId, usecaseId, "", serviceId, "Training + userId:" + userId + ", EncryptedUser: " + encryptedUser, "CheckIfAIModelTrained-not null", null);
            }
            else
            {
                LogInfoMessageToDB(clientId, deliverConstructId, usecaseId, "", serviceId, "Training + userId:" + userId + ", EncryptedUser: " + encryptedUser, "CheckIfAIModelTrained-null", null);
            }

            return model;
        }
        public bool DeleteAIModel(string correlationId)
        {
            AICoreModels aICoreModels = GetAICoreModelPath(correlationId);
            if (aICoreModels.ModelStatus == "Completed"
                || aICoreModels.ModelStatus == "Error"
                || aICoreModels.ModelStatus == "Warning")
            {
                var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
                var projection = Builders<BsonDocument>.Projection.Exclude("_id");

                //Delete from AIService Request Status
                var aIRequestStatus = _database.GetCollection<BsonDocument>(CONSTANTS.AIServiceRequestStatus);
                var result = aIRequestStatus.Find(filter).Project<BsonDocument>(projection).ToList();
                if (result.Count > 0)
                {
                    aIRequestStatus.DeleteMany(filter);
                }


                //Delete from AIService IngestData
                var aIIngestData = _database.GetCollection<BsonDocument>(CONSTANTS.AIServiceIngestData);
                result = aIIngestData.Find(filter).Project<BsonDocument>(projection).ToList();
                if (result.Count > 0)
                {
                    aIIngestData.DeleteMany(filter);
                }

                //Delete Model Files
                try
                {
                    if (aICoreModels.ServiceId == "50f2232a-0182-4cda-96fc-df8f3ccd216c")
                    {
                        if (!string.IsNullOrEmpty(aICoreModels.ModelPath))
                        {
                            Directory.Delete(aICoreModels.ModelPath, true);
                        }
                    }
                    else
                    {

                        if (!string.IsNullOrEmpty(aICoreModels.ModelPath))
                        {
                            if (File.Exists(aICoreModels.ModelPath))
                            {
                                File.Delete(aICoreModels.ModelPath);
                            }
                        }

                    }
                }
                catch (Exception ex)
                {
                    LOGGING.LogManager.Logger.LogErrorMessage(typeof(PredictionSchedulerService), nameof(DeleteAIModel), ex.Message, ex, aICoreModels.ApplicationId, string.Empty, aICoreModels.ClientId, aICoreModels.DeliveryConstructId);
                }




                //Delete Model
                var aICoreModel = _database.GetCollection<BsonDocument>(CONSTANTS.AICoreModels);
                result = aICoreModel.Find(filter).Project<BsonDocument>(projection).ToList();
                if (result.Count > 0)
                {
                    aICoreModel.DeleteMany(filter);
                }
                return true;
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
                serviceList = JsonConvert.DeserializeObject<AICoreModels>(result[0].ToJson());
            }

            return serviceList;
        }
        public void TrainSSAIModel(string clientId, string deliveryConstructId, string usecaseId, string correlationId)
        {
            //if applicationId is IA we have to do the datacheck
            bool isDataAvailable = CheckHadoopData(clientId,
                                                   deliveryConstructId,
                                                   usecaseId,
                                                   DateTime.UtcNow.AddYears(-2).ToString("yyyy-MM-dd"),
                                                   DateTime.UtcNow.ToString("yyyy-MM-dd"));
            if ((isDataAvailable & (usecaseId == IA_SPIUseCaseId || usecaseId == IA_DefectRateUseCaseId))
                || (usecaseId != IA_SPIUseCaseId & usecaseId != IA_DefectRateUseCaseId))
            {
                Thread.Sleep(120000);
                DATAMODELS.TrainingRequestDetails trainingRequestDetails = new DATAMODELS.TrainingRequestDetails();
                trainingRequestDetails.ClientUId = clientId;
                trainingRequestDetails.DeliveryConstructUId = deliveryConstructId;
                trainingRequestDetails.UseCaseId = usecaseId;
                trainingRequestDetails.DataSource = "Custom";
                trainingRequestDetails.ApplicationId = IA_ApplicationId;
                trainingRequestDetails.CorrelationId = correlationId;
                trainingRequestDetails.DataSourceDetails = null;
                trainingRequestDetails.UserId = PredictionSchedulerUserName;
                InvokeSSAITrainingAPI(trainingRequestDetails);
            }

        }




        public void TrainAIModel(string clientId, string deliveryConstructId, string usecaseId, string serviceId, string userId)
        {
            try
            {
                bool isDataAvailable = CheckHadoopData(clientId,
                                                   deliveryConstructId,
                                                   usecaseId,
                                                   DateTime.UtcNow.AddYears(-2).ToString("MM/dd/yyyy"),
                                                   DateTime.UtcNow.ToString("MM/dd/yyyy"));

                LogErrorMessageToDB(clientId, deliveryConstructId, usecaseId, IterationEntityUId, "", "TrainAIModel Hadoop", "IsDataAvailable - " + isDataAvailable, null);
                if (isDataAvailable)
                {
                    Thread.Sleep(180000);
                    var formContent = new FormUrlEncodedContent(new[]
                       {
                            new KeyValuePair<string, string>("ClientId", clientId),
                            new KeyValuePair<string, string>("DeliveryConstructId", deliveryConstructId),
                            new KeyValuePair<string, string>("ServiceId",serviceId),
                            new KeyValuePair<string, string>("UsecaseId", usecaseId),
                            new KeyValuePair<string, string>("DataSource", "Phoenix"),
                            new KeyValuePair<string, string>("ApplicationId",IA_ApplicationId),
                            new KeyValuePair<string, string>("ModelName", "model"),
                            new KeyValuePair<string, string>("DataSourceDetails", ""),
                            new KeyValuePair<string, string>("UserId", userId)
                        });
                    string resourceId = appSettings.resourceId;
                    string token = GenerateToken();
                    string baseURI = appSettings.IngrainAPIUrl;
                    string apiPath = "/api/AIModelTraining";
                    var response = InvokePOSTRequestFromData(token, baseURI, apiPath, formContent, resourceId);
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        LogErrorMessageToDB(clientId, deliveryConstructId, usecaseId, IterationEntityUId, "", "Training", "Ingrain Training api status code - " + response.StatusCode + "-" + response.Content.ReadAsStringAsync().Result, null);
                    }
                    else
                    {
                        LogInfoMessageToDB(clientId, deliveryConstructId, usecaseId, IterationEntityUId, "", "Training", "Ingrain Training api status code - " + response.StatusCode + "-" + response.Content.ReadAsStringAsync().Result, null);
                    }
                }


            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(PredictionSchedulerService), nameof(TrainAIModel), ex.Message, ex, string.Empty, string.Empty, clientId, deliveryConstructId);
                LogErrorMessageToDB(clientId, deliveryConstructId, usecaseId, CREntityUId, "", "Training", ex.ToString(), null);
            }

        }


        public void InvokeSSAITrainingAPI(DATAMODELS.TrainingRequestDetails trainingRequestDetails)
        {
            try
            {
                StringContent content = new StringContent(JsonConvert.SerializeObject(trainingRequestDetails), Encoding.UTF8, "application/json");
                string resourceId = appSettings.resourceId;
                string token = GenerateToken();
                string baseURI = appSettings.IngrainAPIUrl;
                string apiPath = "/api/ia/InitiateTraining";
                var response = InvokePOSTRequest(token, baseURI, apiPath, content, resourceId);
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    LogErrorMessageToDB(trainingRequestDetails.ClientUId, trainingRequestDetails.DeliveryConstructUId, trainingRequestDetails.UseCaseId, IterationEntityUId, "", "Training", "Ingrain Training api status code - " + response.StatusCode + "-" + response.Content.ReadAsStringAsync().Result, null);
                }
                else
                {
                    LogInfoMessageToDB(trainingRequestDetails.ClientUId, trainingRequestDetails.DeliveryConstructUId, trainingRequestDetails.UseCaseId, IterationEntityUId, "", "Training", "Ingrain Training api status code - " + response.StatusCode + "-" + response.Content.ReadAsStringAsync().Result, null);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(PredictionSchedulerService), nameof(InvokeSSAITrainingAPI), ex.Message, ex, trainingRequestDetails.ApplicationId, string.Empty, trainingRequestDetails.ClientUId, trainingRequestDetails.DeliveryConstructUId);
                LogErrorMessageToDB(trainingRequestDetails.ClientUId, trainingRequestDetails.DeliveryConstructUId, trainingRequestDetails.UseCaseId, IterationEntityUId, "", "Training", ex.ToString(), null);
            }

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
            string token = GenerateToken();
            string baseURI = appSettings.IngrainAPIUrl;
            string apiPath = "/api/ia/UseCasePredictionRequest";
            var response = InvokePOSTRequest(token, baseURI, apiPath, content, string.Empty);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(PredictionSchedulerService), nameof(SSAIPredictionAPI), "Ingrain - Prediction 200 ok Success", string.Empty, string.Empty, string.Empty, string.Empty);
                jsonResult = response.Content.ReadAsStringAsync().Result;
            }
            return jsonResult;
        }

        public string SSAIBulkPredictionAPI(string clientUId, string deliveryConstructUId, string useCaseId, List<string> releaseUId)
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
            string token = GenerateToken();
            string baseURI = appSettings.IngrainAPIUrl;
            string apiPath = "/api/ia/UseCasePredictionRequest";
            var response = InvokePOSTRequest(token, baseURI, apiPath, content, string.Empty);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                jsonResult = response.Content.ReadAsStringAsync().Result;
            }
            return jsonResult;
        }

        public string AIServicePredictionAPI(JObject data, string clientUId, string deliveryConstructUId, string useCaseId, string serviceId)
        {
            string jsonResult = string.Empty;
            var predictionInput = new
            {
                ClientUId = clientUId,
                DeliveryConstructUId = deliveryConstructUId,
                UseCaseId = useCaseId,
                ServiceId = serviceId,
                UserId = PredictionSchedulerUserName,
                Data = new JArray() { data }
            };
            StringContent content = new StringContent(JsonConvert.SerializeObject(predictionInput), Encoding.UTF8, "application/json");
            string token = GenerateToken();
            string baseURI = appSettings.IngrainAPIUrl;
            string apiPath = "/api/EvaluateUseCase";
            var response = InvokePOSTRequest(token, baseURI, apiPath, content, string.Empty);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                jsonResult = response.Content.ReadAsStringAsync().Result;
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(PredictionSchedulerService), nameof(AIServicePredictionAPI), "STATUS 200 OK", string.Empty, string.Empty, string.Empty, string.Empty);
            }           

            return jsonResult;
        }


        public string UpdateEntityRecommendationInPhoenix(dynamic payload
                                                        , string clientUId, string deliveryConstructUId, string correlationUId, string entityName)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(PredictionSchedulerService), nameof(UpdateEntityRecommendationInPhoenix), "START", string.Empty, string.Empty, string.Empty, string.Empty);
            string jsonResult = string.Empty;
            string apiPath = string.Empty;

            if (entityName == "Iteration")
            {
                apiPath = String.Format(_recommendationMergeAPI, clientUId, deliveryConstructUId);                
            }

            if (entityName == "ChangeRequest")
            {
                apiPath = String.Format(_recommendationMergeCRAPI, clientUId, deliveryConstructUId);
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(PredictionSchedulerService), nameof(UpdateEntityRecommendationInPhoenix), "clientUId: " + clientUId, "deliveryConstructUId" + deliveryConstructUId, "correlationUId" + correlationUId, "entityName" + entityName, "apiPath" + apiPath);
            }
            StringContent content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            //StringContent content = new StringContent(JsonConvert.SerializeObject(trainingRequestDetails), Encoding.UTF8, "application/json");
            string resourceId = appSettings.resourceId;
            string token = GenerateToken();

            HttpResponseMessage response = PhoenixPOSTRequest(token, appSettings.myWizardAPIUrl, apiPath, content, correlationUId);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                jsonResult = response.Content.ReadAsStringAsync().Result;
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(PredictionSchedulerService), nameof(UpdateEntityRecommendationInPhoenix), "STATUS 200 Ok", string.Empty, string.Empty, string.Empty, string.Empty);
            }
            else
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(PredictionSchedulerService), nameof(UpdateEntityRecommendationInPhoenix), "STATUS NOT 200 Ok", string.Empty, string.Empty, string.Empty, string.Empty);
                LogErrorMessageToDB(clientUId, deliveryConstructUId, "", "", "", "Prediction", "Phoenix update prediction error- " + response.StatusCode + "-" + response.Content.ReadAsStringAsync().Result, null);
            }
            return jsonResult;
        }


        public string UpdateTaskStatus(int updatedRecords, string taskCode, bool isCompleted, string status, string message)
        {
            var collection = _database.GetCollection<BsonDocument>("AutoTrainModelTasks");
            var filter = Builders<BsonDocument>.Filter.Eq("TaskCode", taskCode);
            var result = collection.Find(filter).ToList();
            if (result.Count > 0)
            {
                var update = Builders<BsonDocument>.Update.Set("IsCompleted", isCompleted)
                                                          .Set("UpdateRecords", updatedRecords)
                                                          .Set("LastExecutedDate", DateTime.UtcNow.ToString())
                                                          .Set("Status", status)
                                                          .Set("Message", message);
                collection.UpdateOne(filter, update);
            }

            return "Success";
        }



        public string UpdateOfflineRunTime(DATAMODELS.DeployModelsDto item)
        {
            var collection = _database.GetCollection<DATAMODELS.DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
            var filter = Builders<DATAMODELS.DeployModelsDto>.Filter.Eq("CorrelationId", item.CorrelationId);
            var result = collection.Find(filter).ToList();
            if (result.Count > 0)
            {
                var update = Builders<DATAMODELS.DeployModelsDto>.Update.Set("offlineRunDate", DateTime.UtcNow.ToString());
                collection.UpdateOne(filter, update);
            }

            return "Success";
        }


        public string UpdateOfflineException(DATAMODELS.DeployModelsDto item, string errorMessage)
        {
            var collection = _database.GetCollection<DATAMODELS.DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
            var filter = Builders<DATAMODELS.DeployModelsDto>.Filter.Eq("CorrelationId", item.CorrelationId);
            var result = collection.Find(filter).ToList();
            if (result.Count > 0)
            {
                var update = Builders<DATAMODELS.DeployModelsDto>.Update.Set("offlineRunDate", DateTime.UtcNow.ToString()).Set("ExceptionDate", DateTime.UtcNow.ToString()).Set("ExceptionMessage", errorMessage);
                collection.UpdateOne(filter, update);
            }

            return "Success";
        }







        public HttpResponseMessage InvokePOSTRequestFromData(string token, string baseURI, string apiPath, FormUrlEncodedContent content, string resourceId)
        {
            using (var client = new HttpClient())
            {
                string uri = baseURI + apiPath;
                client.Timeout = new TimeSpan(0, 30, 0);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                if (!string.IsNullOrWhiteSpace(token))
                {
                    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                }
                if (!string.IsNullOrEmpty(resourceId))
                {
                    client.DefaultRequestHeaders.Add("resourceId", resourceId);
                }
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                return client.PostAsync(uri, content).Result;
            }
        }

        public HttpResponseMessage InvokePOSTRequest(string token, string baseURI, string apiPath, StringContent content, string resourceId)
        {
            using (var client = new HttpClient())
            {
                string uri = baseURI + apiPath;
                client.Timeout = new TimeSpan(0, 30, 0);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("AppServiceUId", appSettings.AppServiceUId);
                if (!string.IsNullOrWhiteSpace(token))
                {
                    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                }
                if (!string.IsNullOrEmpty(resourceId))
                {
                    client.DefaultRequestHeaders.Add("resourceId", resourceId);
                }
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                return client.PostAsync(uri, content).Result;
            }
        }


        public HttpResponseMessage PhoenixPOSTRequest(string token, string baseURI, string apiPath, StringContent content, string correlationUId)
        {
            using (var client = new HttpClient())
            {
                string uri = baseURI + apiPath;
                client.Timeout = new TimeSpan(0, 30, 0);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                if (!string.IsNullOrWhiteSpace(token))
                {
                    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                }
                client.DefaultRequestHeaders.Add("AppServiceUId", appSettings.AppServiceUId);
                if (!string.IsNullOrEmpty(correlationUId))
                {
                    client.DefaultRequestHeaders.Add("CorrelationUId", correlationUId);
                }

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                return client.PostAsync(uri, content).Result;
            }
        }


        public void LogInfoMessageToDB(string clientUId, string deliverConstructUId, string useCaseUId, string entityUId, string itemUId, string logType, string logMessage, string status)
        {
            WINSERVICEMODELS.PredictionSchedulerLog predictionSchedulerLog = new WINSERVICEMODELS.PredictionSchedulerLog
            {
                ClientUId = clientUId,
                DeliveryConstructUId = deliverConstructUId,
                UseCaseUId = useCaseUId,
                EntityUId = entityUId,
                ItemUId = itemUId,
                LogType = logType,
                Status = status,
                LogMessage = logMessage,
                LogLevel = "Info",
                CreatedBy = "SYSTEM",
                CreatedOn = DateTime.UtcNow.ToString(),
                ModifiedBy = "SYSTEM",
                ModifiedOn = DateTime.UtcNow.ToString()
            };

            var logCollection = _database.GetCollection<WINSERVICEMODELS.PredictionSchedulerLog>(nameof(WINSERVICEMODELS.PredictionSchedulerLog));
            logCollection.InsertOneAsync(predictionSchedulerLog);
        }


        public void LogInfoMessageToDB(string clientUId, string deliverConstructUId, string useCaseUId, string correlationId, string uniqueId, string entityUId, string itemUId, string logType, string logMessage, string status)
        {
            WINSERVICEMODELS.PredictionSchedulerLog predictionSchedulerLog = new WINSERVICEMODELS.PredictionSchedulerLog
            {
                ClientUId = clientUId,
                DeliveryConstructUId = deliverConstructUId,
                UseCaseUId = useCaseUId,
                CorrelationId = correlationId,
                UniqueId = uniqueId,
                EntityUId = entityUId,
                ItemUId = itemUId,
                LogType = logType,
                Status = status,
                LogMessage = logMessage,
                LogLevel = "Info",
                CreatedBy = "SYSTEM",
                CreatedOn = DateTime.UtcNow.ToString(),
                ModifiedBy = "SYSTEM",
                ModifiedOn = DateTime.UtcNow.ToString()

            };

            var logCollection = _database.GetCollection<WINSERVICEMODELS.PredictionSchedulerLog>(nameof(WINSERVICEMODELS.PredictionSchedulerLog));
            logCollection.InsertOneAsync(predictionSchedulerLog);
        }


        public void LogErrorMessageToDB(string clientUId, string deliverConstructUId, string useCaseUId, string entityUId, string itemUId, string logType, string logMessage, string status)
        {
            WINSERVICEMODELS.PredictionSchedulerLog predictionSchedulerLog = new WINSERVICEMODELS.PredictionSchedulerLog
            {
                ClientUId = clientUId,
                DeliveryConstructUId = deliverConstructUId,
                UseCaseUId = useCaseUId,
                EntityUId = entityUId,
                ItemUId = itemUId,
                LogType = logType,
                Status = status,
                LogMessage = logMessage,
                LogLevel = "Error",
                CreatedBy = "SYSTEM",
                CreatedOn = DateTime.UtcNow.ToString(),
                ModifiedBy = "SYSTEM",
                ModifiedOn = DateTime.UtcNow.ToString()
            };

            var logCollection = _database.GetCollection<WINSERVICEMODELS.PredictionSchedulerLog>(nameof(WINSERVICEMODELS.PredictionSchedulerLog));
            logCollection.InsertOneAsync(predictionSchedulerLog);
        }

        public void LogErrorMessageToDB(string clientUId, string deliverConstructUId, string useCaseUId, string correlationId, string uniqueId, string entityUId, string itemUId, string logType, string logMessage, string status)
        {
            WINSERVICEMODELS.PredictionSchedulerLog predictionSchedulerLog = new WINSERVICEMODELS.PredictionSchedulerLog
            {
                ClientUId = clientUId,
                DeliveryConstructUId = deliverConstructUId,
                UseCaseUId = useCaseUId,
                CorrelationId = correlationId,
                UniqueId = uniqueId,
                EntityUId = entityUId,
                ItemUId = itemUId,
                LogType = logType,
                Status = status,
                LogMessage = logMessage,
                LogLevel = "Error",
                CreatedBy = "SYSTEM",
                CreatedOn = DateTime.UtcNow.ToString(),
                ModifiedBy = "SYSTEM",
                ModifiedOn = DateTime.UtcNow.ToString()
            };

            var logCollection = _database.GetCollection<WINSERVICEMODELS.PredictionSchedulerLog>(nameof(WINSERVICEMODELS.PredictionSchedulerLog));
            logCollection.InsertOneAsync(predictionSchedulerLog);
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

        public string GenerateToken()
        {
            dynamic token = string.Empty;
            if (appSettings.authProvider.ToUpper() == "FORM")
            {
                using (var httpClient = new HttpClient())
                {
                    if (appSettings.Environment.Equals(CONSTANTS.PAMEnvironment))
                    {
                        httpClient.BaseAddress = new Uri(appSettings.tokenAPIUrl);
                        httpClient.DefaultRequestHeaders.Accept.Clear();
                        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                        string json = JsonConvert.SerializeObject(new
                        {
                            username = Convert.ToString(appSettings.username),
                            password = Convert.ToString(appSettings.password)
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
                        if (appSettings.Environment == CONSTANTS.PAMEnvironment)
                            token = tokenObj != null ? Convert.ToString(tokenObj.token) : CONSTANTS.InvertedComma;
                        else
                            token = tokenObj != null ? Convert.ToString(tokenObj.access_token) : CONSTANTS.InvertedComma;
                        return token;

                    }
                    else
                    {
                        httpClient.BaseAddress = new Uri(appSettings.tokenAPIUrl);
                        httpClient.DefaultRequestHeaders.Accept.Clear();
                        httpClient.DefaultRequestHeaders.Add("UserName", appSettings.username);
                        httpClient.DefaultRequestHeaders.Add("Password", appSettings.password);
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
                    //httpClient.BaseAddress = new Uri(appSettings.tokenAPIUrl);
                    //httpClient.DefaultRequestHeaders.Accept.Clear();
                    //httpClient.DefaultRequestHeaders.Add("UserName", appSettings.username);
                    //httpClient.DefaultRequestHeaders.Add("Password", appSettings.password);
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
            else
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    var formContent = new FormUrlEncodedContent(new[]
                    {
                            new KeyValuePair<string, string>("grant_type", appSettings.Grant_Type),
                            new KeyValuePair<string, string>("client_id", appSettings.clientId),
                            new KeyValuePair<string, string>("client_secret",appSettings.clientSecret),
                            new KeyValuePair<string, string>("resource",appSettings.resourceId)
                        });

                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                    var tokenResult = client.PostAsync(appSettings.token_Url, formContent).Result;
                    Dictionary<string, string> tokenDictionary = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(tokenResult.Content.ReadAsStringAsync().Result);
                    return tokenDictionary[CONSTANTS.access_token].ToString();
                }
            }
            return token;
        }


        public bool CheckSSAITrainedModels(string clientUId, string deliveryConstructUId)
        {
            string encryptedUser = PredictionSchedulerUserName;
            if (!string.IsNullOrEmpty(Convert.ToString(encryptedUser)))
            {
                if (appSettings.IsAESKeyVault)
                {
                    encryptedUser = CryptographyUtility.Encrypt(Convert.ToString(encryptedUser));
                }
                else
                {
                    encryptedUser = AesProvider.Encrypt(Convert.ToString(encryptedUser), appSettings.aesKey, appSettings.aesVector);
                }
            }
            var deployedModels = _database.GetCollection<DATAMODELS.DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
            var filter = Builders<DATAMODELS.DeployModelsDto>.Filter.Where(x => x.ClientUId == clientUId)
                         & Builders<DATAMODELS.DeployModelsDto>.Filter.Where(x => x.DeliveryConstructUID == deliveryConstructUId)
                         & Builders<DATAMODELS.DeployModelsDto>.Filter.In("TemplateUsecaseId", SSAIUseCaseIds)
                         & Builders<DATAMODELS.DeployModelsDto>.Filter.Where(x => (x.CreatedByUser == PredictionSchedulerUserName || x.CreatedByUser == encryptedUser))
                         & Builders<DATAMODELS.DeployModelsDto>.Filter.Where(x => x.Status == CONSTANTS.Deployed);
            var projection = Builders<DATAMODELS.DeployModelsDto>.Projection.Exclude("_id");
            var result = deployedModels.Find(filter).Project<DATAMODELS.DeployModelsDto>(projection).ToList();            
            if (result.Count > 0)
            {
                return true;
            }
            else
            {
                return false;
            }


        }

        public bool CheckAITrainedModels(string clientUId, string deliveryConstructUId)
        {
            string encryptedUser = PredictionSchedulerUserName;
            if (!string.IsNullOrEmpty(Convert.ToString(encryptedUser)))
            {
                if (appSettings.IsAESKeyVault)
                {
                    encryptedUser = CryptographyUtility.Encrypt(Convert.ToString(encryptedUser));
                }
                else
                {
                    encryptedUser = AesProvider.Encrypt(Convert.ToString(encryptedUser), appSettings.aesKey, appSettings.aesVector);
                }
            }
            var aiCoreModels = _database.GetCollection<AICoreModels>(CONSTANTS.AICoreModels);
            var filter = Builders<AICoreModels>.Filter.Where(x => x.ClientId == clientUId)
                         & Builders<AICoreModels>.Filter.Where(x => x.DeliveryConstructId == deliveryConstructUId)
                         & Builders<AICoreModels>.Filter.In("UsecaseId", AIUseCaseIds)
                         & Builders<AICoreModels>.Filter.Where(x => (x.CreatedBy == PredictionSchedulerUserName || x.CreatedBy == encryptedUser))
                         & Builders<AICoreModels>.Filter.Where(x => x.ModelStatus == "Completed");
            var projection = Builders<AICoreModels>.Projection.Exclude("_id");
            var result = aiCoreModels.Find(filter).Project<AICoreModels>(projection).ToList();
            //db.AICoreModels.find({"ClientId": "6e158a40-ca3a-455a-a150-5b8a20c41acc",UsecaseId:{$in:["471ae90c-55ef-49b4-8e5b-8938dde32fa9", "672fb32c-05ca-4871-a2a8-d3b2adc5bf1e", "a71ff6fd-4711-4b42-b41c-39ef95dedb75"]},CreatedBy:"SYSTEM",ModelStatus:"Completed"})
            LogInfoMessageToDB(clientUId, deliveryConstructUId, "", CREntityUId, "", "Prediction", "CheckAITrainedModels result:" + result.Count, null);
            if (result.Count > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public List<AICoreModels> GetCompletedAIServiceModels()
        {
            string encryptedUser = PredictionSchedulerUserName;
            if (!string.IsNullOrEmpty(Convert.ToString(encryptedUser)))
            {
                if (appSettings.IsAESKeyVault)
                {
                    encryptedUser = CryptographyUtility.Encrypt(Convert.ToString(encryptedUser));
                }
                else
                {
                    encryptedUser = AesProvider.Encrypt(Convert.ToString(encryptedUser), appSettings.aesKey, appSettings.aesVector);
                }
            }
            List<AICoreModels> aiCoreModels = null;
            try
            {
                var aiModelsCollection = _database.GetCollection<AICoreModels>(nameof(AICoreModels));
                var filter = (Builders<AICoreModels>.Filter.Eq("CreatedBy", PredictionSchedulerUserName) | Builders<AICoreModels>.Filter.Eq("CreatedBy", encryptedUser))
                             & Builders<AICoreModels>.Filter.In("UsecaseId", AIUseCaseIds)
                             & Builders<AICoreModels>.Filter.Eq("ModelStatus", "Completed");
                var projection = Builders<AICoreModels>.Projection.Exclude("_id");
                aiCoreModels = aiModelsCollection.Find(filter).Project<AICoreModels>(projection).ToList();
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(PredictionSchedulerService), nameof(RemoveAIServiceModels), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }

            return aiCoreModels;
        }

        public void RemoveAIServiceModels()
        {
            try
            {
                string encryptedUser = PredictionSchedulerUserName;
                if (!string.IsNullOrEmpty(Convert.ToString(encryptedUser)))
                {
                    if (appSettings.IsAESKeyVault)
                    {
                        encryptedUser = CryptographyUtility.Encrypt(Convert.ToString(encryptedUser));
                    }
                    else
                    {
                        encryptedUser = AesProvider.Encrypt(Convert.ToString(encryptedUser), appSettings.aesKey, appSettings.aesVector);
                    }
                }
                var aiModelsCollection = _database.GetCollection<AICoreModels>(nameof(AICoreModels));
                var filter = (Builders<AICoreModels>.Filter.Eq("CreatedBy", PredictionSchedulerUserName) | Builders<AICoreModels>.Filter.Eq("CreatedBy", encryptedUser))
                             & Builders<AICoreModels>.Filter.In("UsecaseId", AIUseCaseIds)
                             & Builders<AICoreModels>.Filter.Eq("ModelStatus", "InProgress");
                var projection = Builders<AICoreModels>.Projection.Exclude("_id");
                var aiModels = aiModelsCollection.Find(filter).Project<AICoreModels>(projection).ToList();
                if (aiModels.Count > 0)
                {
                    foreach (var model in aiModels)
                    {
                        DateTime createdOn = DateTime.Parse(model.CreatedOn);
                        DateTime currTime = DateTime.Now;
                        double hours = (currTime - createdOn).TotalHours;
                        if (hours > 2)
                        {
                            var filter2 = Builders<AICoreModels>.Filter.Where(x => x.CorrelationId == model.CorrelationId);
                            aiModelsCollection.DeleteOne(filter2);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(PredictionSchedulerService), nameof(RemoveAIServiceModels), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
        }

        public void ClearENSNotifications()
        {
            try
            {
                var ensCollection = _database.GetCollection<DATAMODELS.ENSEntityNotificationLog>(nameof(DATAMODELS.ENSEntityNotificationLog));
                var filter = Builders<DATAMODELS.ENSEntityNotificationLog>.Filter.Empty;
                var result = ensCollection.Find(filter).ToList();

                if (result.Count > 0)
                {
                    DateTime dateNow = DateTime.UtcNow.AddDays(-7);
                    foreach (var rec in result)
                    {
                        DateTime recDate = DateTime.Parse(rec.CreatedOn);
                        if (recDate < dateNow)
                        {
                            var filter2 = Builders<DATAMODELS.ENSEntityNotificationLog>.Filter.Where(x => x._id == rec._id);
                            ensCollection.DeleteOne(filter2);
                        }
                    }
                }
                var logCollection = _database.GetCollection<WINSERVICEMODELS.PredictionSchedulerLog>(nameof(WINSERVICEMODELS.PredictionSchedulerLog));
                var filter3 = Builders<WINSERVICEMODELS.PredictionSchedulerLog>.Filter.Empty;
                var result3 = logCollection.Find(filter3).ToList();

                if (result3.Count > 0)
                {
                    DateTime dateNow = DateTime.UtcNow.AddDays(-7);
                    foreach (var rec in result3)
                    {
                        DateTime recDate = DateTime.Parse(rec.CreatedOn);
                        if (recDate < dateNow)
                        {
                            var filter5 = Builders<WINSERVICEMODELS.PredictionSchedulerLog>.Filter.Where(x => x._id == rec._id);
                            logCollection.DeleteOne(filter5);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(PredictionSchedulerService), nameof(ClearENSNotifications), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }

        }


        #region CheckHadoopData

        // currently let the code be same 
        public bool CheckHadoopData(string clientUId, string deliveryConstructUId, string UseCaseId, string startDate, string endDate)
        {
            bool response = false;
            if (UseCaseId == IA_SPIUseCaseId)
            {
                response = CheckSPIData(clientUId, deliveryConstructUId, startDate, endDate);
            }
            else if (UseCaseId == IA_DefectRateUseCaseId)
            {
                response = CheckDefectRateDate(clientUId, deliveryConstructUId, startDate, endDate);
            }
            else if (UseCaseId == IA_CRUseCaseId)
            {
                response = CheckCRData(clientUId, deliveryConstructUId, startDate, endDate);
            }
            else if (UseCaseId == IA_UserStoryUseCaseId)
            {
                response = CheckUserStoryData(clientUId, deliveryConstructUId, startDate, endDate);
            }
            else if (UseCaseId == IA_RequirementUseCaseId)
            {
                response = CheckRequirementData(clientUId, deliveryConstructUId, startDate, endDate);
            }

            return response;
        }

        public bool CheckSPIData(string clientUId, string deliveryConstructUId, string startDate, string endDate)
        {
            bool haveSPIData = false;
            try
            {
                var requestPayload = new
                {
                    ClientUID = clientUId,
                    DeliveryConstructUId = new List<string>() { deliveryConstructUId },
                    Measure_metrics = new List<string>() { "SPI", "EV", "PV", "EDV", "AD121", "ReleaseUId", "processedondate", "ModifiedOn", "complexityuid" },
                    PageNumber = 1,
                    TotalRecordCount = 0,
                    BatchSize = 20,
                    StartDate = startDate,
                    EndDate = endDate
                };

                StringContent content = new StringContent(JsonConvert.SerializeObject(requestPayload), Encoding.UTF8, "application/json");
                string token = GenerateToken();

                Uri baseUri = new Uri(appSettings.myWizardAPIUrl);
                string host = baseUri.GetLeftPart(UriPartial.Authority);
                var response = InvokePOSTRequest(token, host, _hadoopSPIApi, content, null);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var jsonResult = response.Content.ReadAsStringAsync().Result;
                    JObject result = JObject.Parse(jsonResult);
                    if (result.ContainsKey("TotalRecordCount"))
                    {
                        int recCount = Convert.ToInt32(result["TotalRecordCount"].ToString());
                        if (recCount > 20)
                        {
                            haveSPIData = true;
                        }
                        else
                        {
                            LogInfoMessageToDB(clientUId, deliveryConstructUId, IA_SPIUseCaseId, IterationEntityUId, null, "Training", "Total Record count in SPI API is -" + recCount, "");
                        }
                    }
                }
                else
                {
                    LogErrorMessageToDB(clientUId, deliveryConstructUId, IA_SPIUseCaseId, IterationEntityUId, null, "Training", "Hadoop SPI API returned-" + response.ReasonPhrase + "-" + response.Content.ReadAsStringAsync().Result, "E");
                }
            }
            catch (Exception ex)
            {
                LogErrorMessageToDB(clientUId, deliveryConstructUId, IA_SPIUseCaseId, IterationEntityUId, null, "Training", "Exception checking SPI Data-" + ex.Message, "E");
            }
            return haveSPIData;
        }


        public bool CheckDefectRateDate(string clientUId, string deliveryConstructUId, string startDate, string endDate)
        {
            bool haveDefectRateData = false;
            try
            {
                var requestPayload = new
                {
                    ClientUID = clientUId,
                    DeliveryConstructUId = new List<string>() { deliveryConstructUId },
                    Measure_metrics = new List<string>() { "DR", "EV", "PV", "AD033", "AD149", "AD058", "ReleaseUId", "processedondate", "complexityuid", "modifiedon", "clientuid" },
                    PageNumber = 1,
                    TotalRecordCount = 0,
                    BatchSize = 20,
                    StartDate = startDate,
                    EndDate = endDate
                };

                StringContent content = new StringContent(JsonConvert.SerializeObject(requestPayload), Encoding.UTF8, "application/json");
                string token = GenerateToken();

                Uri baseUri = new Uri(appSettings.myWizardAPIUrl);
                string host = baseUri.GetLeftPart(UriPartial.Authority);
                var response = InvokePOSTRequest(token, host, _hadoopDefectRateApi, content, null);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var jsonResult = response.Content.ReadAsStringAsync().Result;
                    JObject result = JObject.Parse(jsonResult);
                    if (result.ContainsKey("TotalRecordCount"))
                    {
                        int recCount = Convert.ToInt32(result["TotalRecordCount"].ToString());
                        if (recCount > 20)
                        {
                            haveDefectRateData = true;
                        }
                        else
                        {
                            LogInfoMessageToDB(clientUId, deliveryConstructUId, IA_DefectRateUseCaseId, IterationEntityUId, null, "Training", "Total Record count in Defect Rate API is -" + recCount, "");
                        }
                    }
                }
                else
                {
                    LogErrorMessageToDB(clientUId, deliveryConstructUId, IA_DefectRateUseCaseId, IterationEntityUId, null, "Training", "Hadoop DefectRate API returned-" + response.ReasonPhrase + "-" + response.Content.ReadAsStringAsync().Result, "E");
                }
            }
            catch (Exception ex)
            {
                LogErrorMessageToDB(clientUId, deliveryConstructUId, IA_DefectRateUseCaseId, IterationEntityUId, null, "Training", "Exception checking DefectRate Data-" + ex.Message, "E");
            }
            return haveDefectRateData;
        }


        public bool CheckCRData(string clientUId, string deliveryConstructUId, string startDate, string endDate)
        {
            bool haveCRData = false;
            try
            {
                var requestPayload = new
                {
                    ClientUID = clientUId,
                    DeliveryConstructUId = deliveryConstructUId,
                    EntityUId = CREntityUId,
                    ColumnList = "comments,clientuid,createdbyproductinstanceuid,modifiedon,rowstatusuid,createdbyuser,actualcost,owner,reference,createdbyapp,description,typeuid,createdatsourcebyuser,delegatedto,stateuid,priorityuid,committedeffort,details,modifiedbyuser,title,createdon,requestor,severityuid,committedcost,changerequestexternalid,modifiedbyapp,createdatsourceon,rowversion,changerequestid,modifiedatsourcebyuser,changerequestuid,benefits,reasonforchangerequest,externalid,nextapprover,impactonbusiness,requirementid,requestowner,approverlist,stateexternalid,resourceemailaddress,plannedenddate,errorcallbacklink,changerequestextensions,changerequestassociations,changerequestdeliveryconstructs,deliveryconstructuid,TeamAreaExternalId,TeamAreaName",
                    RowStatusUId = "00100000-0000-0000-0000-000000000000",
                    PageNumber = "1",
                    TotalRecordCount = "0",
                    BatchSize = "20",
                    FromDate = startDate,
                    ToDate = endDate
                };

                StringContent content = new StringContent(JsonConvert.SerializeObject(requestPayload), Encoding.UTF8, "application/json");
                string token = GenerateToken();

                Uri baseUri = new Uri(appSettings.myWizardAPIUrl);
                string host = baseUri.GetLeftPart(UriPartial.Authority);
                string apiPath = String.Format(_hadoopCRApi, clientUId, deliveryConstructUId);
                var response = InvokePOSTRequest(token, host, apiPath, content, null);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var jsonResult = response.Content.ReadAsStringAsync().Result;
                    JObject result = JObject.Parse(jsonResult);
                    if (result.ContainsKey("TotalRecordCount"))
                    {
                        int recCount = Convert.ToInt32(result["TotalRecordCount"].ToString());
                        if (recCount > 20)
                        {
                            haveCRData = true;
                        }
                        else
                        {
                            LogInfoMessageToDB(clientUId, deliveryConstructUId, null, CREntityUId, null, "Training", "Total Record count in CR API is -" + recCount, "");
                        }
                    }
                }
                else
                {
                    LogErrorMessageToDB(clientUId, deliveryConstructUId, null, CREntityUId, null, "Training", "Hadoop CR API returned-" + response.ReasonPhrase + "-" + response.Content.ReadAsStringAsync().Result, "E");
                }
            }
            catch (Exception ex)
            {
                LogErrorMessageToDB(clientUId, deliveryConstructUId, null, CREntityUId, null, "Training", "Exception checking CR Data-" + ex.Message, "E");
            }
            return haveCRData;



        }


        public bool CheckUserStoryData(string clientUId, string deliveryConstructUId, string startDate, string endDate)
        {
            bool haveUserStoryData = false;
            try
            {
                var requestPayload = new
                {
                    ClientUID = clientUId,
                    DeliveryConstructUId = deliveryConstructUId,
                    EntityUId = UserStoryEntityUId,
                    ColumnList = "costofdelay,AssignedAtSourceToUser,iterationuidexternalvalue,currencyuididvalue,storypointestimated,businesscriticality,risk,starton,releaseuididvalue,stateuid,categoryuid,identifiedby,teamarea,storypointcompleted,iterationuididvalue,project,probabilityuididvalue,reference,EffortCompleted,summary,completedon,typeuid,valuearea,stateuididvalue,iterationuid,targetstarton,priorityuid,priorityuididvalue,severityuididvalue,comments,severityuid,effortestimated,businessvalue,statereason,releaseuidexternalvalue,riskreduction,assignedatsourceuser,description,effortremaining,currencyuid,targetendon,title,acceptancecriteria,teamareauid,commentsfieldvalue,identifiedon,probabilityuid,workitemuid,workitemexternalid,createdon,modifiedon,createdbyproductinstanceuid,workitemassociations",
                    WorkItemTypeUId = "00020040020000100040000000000000",
                    RowStatusUId = "00100000-0000-0000-0000-000000000000",
                    PageNumber = "1",
                    TotalRecordCount = "0",
                    BatchSize = "20",
                    FromDate = startDate,
                    ToDate = endDate
                };

                StringContent content = new StringContent(JsonConvert.SerializeObject(requestPayload), Encoding.UTF8, "application/json");
                string token = GenerateToken();

                Uri baseUri = new Uri(appSettings.myWizardAPIUrl);
                string host = baseUri.GetLeftPart(UriPartial.Authority);
                string apiPath = String.Format(_hadoopUserStoryApi, clientUId, deliveryConstructUId);
                var response = InvokePOSTRequest(token, host, apiPath, content, null);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var jsonResult = response.Content.ReadAsStringAsync().Result;
                    JObject result = JObject.Parse(jsonResult);
                    if (result.ContainsKey("TotalRecordCount"))
                    {
                        int recCount = Convert.ToInt32(result["TotalRecordCount"].ToString());
                        if (recCount > 20)
                        {
                            haveUserStoryData = true;
                        }
                        else
                        {
                            LogInfoMessageToDB(clientUId, deliveryConstructUId, null, UserStoryEntityUId, null, "Training", "Total Record count in userstory API is -" + recCount, "");
                        }
                    }
                }
                else
                {
                    LogErrorMessageToDB(clientUId, deliveryConstructUId, null, UserStoryEntityUId, null, "Training", "Hadoop Userstory API returned-" + response.ReasonPhrase + "-" + response.Content.ReadAsStringAsync().Result, "E");
                }
            }
            catch (Exception ex)
            {
                LogErrorMessageToDB(clientUId, deliveryConstructUId, null, UserStoryEntityUId, null, "Training", "Exception checking userstory Data-" + ex.Message, "E");
            }
            return haveUserStoryData;

        }



        public bool CheckRequirementData(string clientUId, string deliveryConstructUId, string startDate, string endDate)
        {
            bool haveRequirementData = false;
            try
            {
                var requestPayload = new
                {
                    ClientUID = clientUId,
                    DeliveryConstructUId = deliveryConstructUId,
                    EntityUId = RequirementEntityUId,
                    ColumnList = "clientuid,actualendon,actualstarton,ascore,assignedatsourcetouser,assignedtoresourceuid,assignedtouser,businessvalue,comments,complexityuid,createdatsourcebyuser,createdatsourceon,createdbyapp,createdbyproductinstanceuid,createdbyuser,createdon,delegatedtoatsource,description,effortestimated,escore,externalreviewer,externalrevieweratsource,forecastendon,forecaststarton,internalreviewer,internalrevieweratsource,iscore,modifiedatsourcebyuser,modifiedatsourceon,modifiedbyapp,modifiedbyuser,modifiedon,nscore,project,qualityscore,releaseuid,requirementexternalid,requirementid,requirementtypeuid,requirementuid,riskreduction,sscore,stateuid,title,tscore,vscore,wsjf,rowstatusuid,phaseuid,workstream,commentsfieldvalue,impactedvalue,assignedtouserfieldvalue,businessvaluefieldvalue,complexityuidfieldvalue,riskreductionfieldvalue,priorityuid,qualityscorefieldvalue,identifiedon,requestowner,qscore,reference,actualstartonfieldvalue,details,escalationlevel,actualendonfieldvalue,resourceemailaddress,effortestimatedfieldvalue,externalreviewerfieldvalue,internalreviewerfieldvalue,identifiedby,requirementassociations,requirementextensions,deliveryconstructuid",
                    RowStatusUId = "00100000-0000-0000-0000-000000000000",
                    PageNumber = "1",
                    TotalRecordCount = "0",
                    BatchSize = "20",
                    FromDate = startDate,
                    ToDate = endDate
                };

                StringContent content = new StringContent(JsonConvert.SerializeObject(requestPayload), Encoding.UTF8, "application/json");
                string token = GenerateToken();

                Uri baseUri = new Uri(appSettings.myWizardAPIUrl);
                string host = baseUri.GetLeftPart(UriPartial.Authority);
                string apiPath = String.Format(_hadooopRequirementApi, clientUId, deliveryConstructUId);
                var response = InvokePOSTRequest(token, host, apiPath, content, null);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var jsonResult = response.Content.ReadAsStringAsync().Result;
                    JObject result = JObject.Parse(jsonResult);
                    if (result.ContainsKey("TotalRecordCount"))
                    {
                        int recCount = Convert.ToInt32(result["TotalRecordCount"].ToString());
                        if (recCount > 20)
                        {
                            haveRequirementData = true;
                        }
                        else
                        {
                            LogInfoMessageToDB(clientUId, deliveryConstructUId, null, RequirementEntityUId, null, "Training", "Total Record count in requirement API is -" + recCount, "");
                        }
                    }
                }
                else
                {
                    LogErrorMessageToDB(clientUId, deliveryConstructUId, null, RequirementEntityUId, null, "Training", "Hadoop requirement API returned-" + response.ReasonPhrase + "-" + response.Content.ReadAsStringAsync().Result, "E");
                }
            }
            catch (Exception ex)
            {
                LogErrorMessageToDB(clientUId, deliveryConstructUId, null, RequirementEntityUId, null, "Training", "Exception checking requirement Data-" + ex.Message, "E");
            }
            return haveRequirementData;
        }








        #endregion




        #region Iterations Predictions scheduler



        public void UpdateCredentialsInAppIntegration()
        {
            var appIntegrationsCredentials = new
            {
                grant_type = appSettings.Grant_Type,
                client_id = appSettings.clientId,
                client_secret = appSettings.clientSecret,
                resource = appSettings.resourceId
            };

            DATAMODELS.AppIntegration appIntegrations = new DATAMODELS.AppIntegration()
            {
                ApplicationID = IA_ApplicationId,
                Credentials = JsonConvert.SerializeObject(appIntegrationsCredentials)
            };

            UpdateAppIntegration(appIntegrations);
        }

        public string UpdateAppIntegration(DATAMODELS.AppIntegration appIntegrations)
        {
            var collection = _database.GetCollection<DATAMODELS.AppIntegration>(CONSTANTS.AppIntegration);
            var filter = Builders<DATAMODELS.AppIntegration>.Filter.Where(x => x.ApplicationID == appIntegrations.ApplicationID);
            var Projection = Builders<DATAMODELS.AppIntegration>.Projection.Exclude("_id");
            var result = collection.Find(filter).Project<DATAMODELS.AppIntegration>(Projection).FirstOrDefault();

            var status = CONSTANTS.NoRecordsFound;
            if (result != null)
            {
                var update = Builders<DATAMODELS.AppIntegration>.Update
                    .Set(x => x.Authentication, appSettings.authProvider)
                    .Set(x => x.TokenGenerationURL,(appSettings.IsAESKeyVault? CryptographyUtility.Encrypt(appSettings.token_Url) : AesProvider.Encrypt(appSettings.token_Url, appSettings.aesKey, appSettings.aesVector)))
                    .Set(x => x.Credentials, (IEnumerable)(appSettings.IsAESKeyVault ? CryptographyUtility.Encrypt(appIntegrations.Credentials) : AesProvider.Encrypt(appIntegrations.Credentials, appSettings.aesKey, appSettings.aesVector)))
                    .Set(x => x.ModifiedByUser, (appSettings.IsAESKeyVault ? CryptographyUtility.Encrypt(PredictionSchedulerUserName) : AesProvider.Encrypt(PredictionSchedulerUserName, appSettings.aesKey, appSettings.aesVector)))
                    .Set(x => x.ModifiedOn, DateTime.UtcNow.ToString());
                collection.UpdateOne(filter, update);
                status = CONSTANTS.Success;
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(PredictionSchedulerService), nameof(UpdateAppIntegration), "UpdateAppIntegration - status : " + status, appIntegrations.ApplicationID, string.Empty, appIntegrations.clientUId, appIntegrations.deliveryConstructUID);
            return status;
        }
        ////old method
        //public void UpdateNewIterationRecommendationsInPhoenix()
        //{
        //    try
        //    {
        //        List<WINSERVICEMODELS.PhoenixIterations> phoenixIterations = null;

        //        phoenixIterations = GetNotRecommendedIterations();
        //        if (phoenixIterations.Count > 0)
        //        {
        //            foreach (var iteration in phoenixIterations)
        //            {
        //                UpdateIterationPredictions(iteration.ClientUId, iteration.DeliveryConstructUId, iteration.ItemUId);
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        LOGGING.LogManager.Logger.LogErrorMessage(typeof(PredictionSchedulerService), nameof(UpdateNewIterationRecommendationsInPhoenix), ex.Message, ex);
        //    }
        //}
        //new method
        public void UpdateNewIterationRecommendationsInPhoenix()
        {
            try
            {
                List<WINSERVICEMODELS.PhoenixIterations> phoenixIterations = null;

                phoenixIterations = GetNotRecommendedIterations();
                if (phoenixIterations.Count > 0)
                {
                    var uniqueClientsDCs = phoenixIterations.Select(x => new
                    {
                        x.ClientUId,
                        x.DeliveryConstructUId
                    }).Distinct().ToList();

                    foreach (var dc in uniqueClientsDCs)
                    {
                        var iterations = phoenixIterations.Where(x => x.ClientUId == dc.ClientUId && x.DeliveryConstructUId == dc.DeliveryConstructUId).Select(x => x.ItemUId).ToList();
                        int i = 0;
                        int skip = 0;
                        do
                        {
                            i = i + 50;
                            UpdateBulkIterationPredictions(dc.ClientUId, dc.DeliveryConstructUId, iterations.Skip(skip).Take(50).ToList());
                            skip = i;
                        } while (i < iterations.Count);
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(PredictionSchedulerService), nameof(UpdateNewIterationRecommendationsInPhoenix), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
        }

        public void UpdateIterationRecommendationsInPhoenix()
        {
            try
            {
                List<WINSERVICEMODELS.PhoenixIterations> phoenixIterations = null;
                List<WINSERVICEMODELS.PhoenixIterations> oldphoenixIterations = new List<WINSERVICEMODELS.PhoenixIterations>();

                //errored once
                phoenixIterations = GetErroredIterations();
                if (phoenixIterations.Count > 0)
                {
                    foreach (var iteration in phoenixIterations)
                    {
                        DateTime curTime = DateTime.UtcNow;
                        int diff = (int)(curTime - iteration.ModifiedOn).TotalHours;
                        if (diff > 8)
                        {
                            oldphoenixIterations.Add(iteration);
                        }
                    }
                    if (oldphoenixIterations.Count > 0)
                    {
                        var uniqueClientsDCs = oldphoenixIterations.Select(x => new
                        {
                            x.ClientUId,
                            x.DeliveryConstructUId
                        }).Distinct().ToList();

                        foreach (var dc in uniqueClientsDCs)
                        {
                            var iterations = oldphoenixIterations.Where(x => x.ClientUId == dc.ClientUId && x.DeliveryConstructUId == dc.DeliveryConstructUId).Select(x => x.ItemUId).ToList();
                            int i = 0;
                            int skip = 0;
                            do
                            {
                                i = i + 50;
                                UpdateBulkIterationPredictions(dc.ClientUId, dc.DeliveryConstructUId, iterations.Skip(skip).Take(50).ToList());
                                skip = i;
                            } while (i < iterations.Count);
                        }
                        // UpdateIterationPredictions(iteration.ClientUId, iteration.DeliveryConstructUId, iteration.ItemUId);

                    }
                }

                //already predicted once
                phoenixIterations = GetIterationsRecommendationstobeUpdated();

                if (phoenixIterations.Count > 0)
                {
                    foreach (var iteration in phoenixIterations)
                    {
                        var uniqueClientsDCs = phoenixIterations.Select(x => new
                        {
                            x.ClientUId,
                            x.DeliveryConstructUId
                        }).Distinct().ToList();

                        foreach (var dc in uniqueClientsDCs)
                        {
                            var iterations = phoenixIterations.Where(x => x.ClientUId == dc.ClientUId && x.DeliveryConstructUId == dc.DeliveryConstructUId).Select(x => x.ItemUId).ToList();
                            int i = 0;
                            int skip = 0;
                            do
                            {
                                i = i + 50;
                                UpdateBulkIterationPredictions(dc.ClientUId, dc.DeliveryConstructUId, iterations.Skip(skip).Take(50).ToList());
                                skip = i;
                            } while (i < iterations.Count);
                        }
                        // UpdateIterationPredictions(iteration.ClientUId, iteration.DeliveryConstructUId, iteration.ItemUId);
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(PredictionSchedulerService), nameof(UpdateIterationRecommendationsInPhoenix), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }

        }

        public int CheckSPIDefectRateFrequency()
        {
            int predictionFrequency = 0;
            string encryptedUser = PredictionSchedulerUserName;
            if (!string.IsNullOrEmpty(Convert.ToString(encryptedUser)))
            {
                if (appSettings.IsAESKeyVault)
                {
                    encryptedUser = CryptographyUtility.Encrypt(Convert.ToString(encryptedUser));
                }
                else
                {
                    encryptedUser = AesProvider.Encrypt(Convert.ToString(encryptedUser), appSettings.aesKey, appSettings.aesVector);
                }
            }
            var deployedModels = _database.GetCollection<DATAMODELS.DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
            var filter = Builders<DATAMODELS.DeployModelsDto>.Filter.In("CorrelationId", SSAIUseCaseIds)
                         //  & Builders<DATAMODELS.DeployModelsDto>.Filter.Where(x => (x.CreatedByUser == PredictionSchedulerUserName || x.CreatedByUser == encryptedUser))
                         & Builders<DATAMODELS.DeployModelsDto>.Filter.Where(x => x.Status == CONSTANTS.Deployed);
            var projection = Builders<DATAMODELS.DeployModelsDto>.Projection.Exclude("_id");
            var result = deployedModels.Find(filter).Project<DATAMODELS.DeployModelsDto>(projection).ToList();
            if (result.Count > 0)
            {
                predictionFrequency = result[0].PredictionFrequencyInDays;
            }
            return predictionFrequency;


        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public List<UsecaseDetails> CheckIASimilarTrainFrequency()
        {
            //we have to take all which has offline selected from deployed Models screen - (shld not be for file)
            var deployedModels = _database.GetCollection<UsecaseDetails>(CONSTANTS.AISavedUsecases);
            var filter = Builders<UsecaseDetails>.Filter.In("CorrelationId", AIUseCaseIds);
            var projection = Builders<UsecaseDetails>.Projection.Exclude("_id");
            var result = deployedModels.Find(filter).Project<UsecaseDetails>(projection).ToList();
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public List<DATAMODELS.DeployModelsDto> CheckIASSAITrainFrequency()
        {
            //we have to take all which has offline selected from deployed Models screen - (shld not be for file)
            var deployedModels = _database.GetCollection<DATAMODELS.DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
            var filter = //Builders<DATAMODELS.DeployModelsDto>.Filter.In("CorrelationId", SSAIUseCaseIds)
                 Builders<DATAMODELS.DeployModelsDto>.Filter.Eq(CONSTANTS.IsOffline, true)
                 & Builders<DATAMODELS.DeployModelsDto>.Filter.Eq(CONSTANTS.IsModelTemplate, true)
                 & (Builders<DATAMODELS.DeployModelsDto>.Filter.Where(x => x.SourceName != CONSTANTS.file) |
                Builders<DATAMODELS.DeployModelsDto>.Filter.In("CorrelationId", SSAIUseCaseIds))
                         //  & Builders<DATAMODELS.DeployModelsDto>.Filter.Where(x => (x.CreatedByUser == PredictionSchedulerUserName || x.CreatedByUser == encryptedUser))
                         & Builders<DATAMODELS.DeployModelsDto>.Filter.Where(x => x.Status == CONSTANTS.Deployed);

            var projection = Builders<DATAMODELS.DeployModelsDto>.Projection.Exclude("_id");
            var result = deployedModels.Find(filter).Project<DATAMODELS.DeployModelsDto>(projection).ToList();
            return result;
        }

        public void FetchIterationsList()
        {
            try
            {
                UpdateTaskStatus(0, "IAITERATIONSPULL", false, "I", "Iterations Fetch in progress");
                List<DATAMODELS.DeployModelsDto> deployModels = CheckSPIDefectRateModels();
                if (deployModels.Count > 0)
                {
                    var uniqueClientsDCs = deployModels.Select(x => new
                    {
                        x.ClientUId,
                        x.DeliveryConstructUID
                    }).Distinct().ToList();


                    if (uniqueClientsDCs.Count > 0)
                    {
                        foreach (var dc in uniqueClientsDCs)
                        {
                            string result = string.Empty;
                            WINSERVICEMODELS.PhoenixPayloads.IterationQueryResponse iterationQueryResponse = null;

                            result = InvokeIterationsQueryAPI(dc.ClientUId, dc.DeliveryConstructUID, 1); // endon shld be futuredata

                            if (!string.IsNullOrEmpty(result))
                            {
                                iterationQueryResponse = JsonConvert.DeserializeObject<WINSERVICEMODELS.PhoenixPayloads.IterationQueryResponse>(result);
                                UpdateIterationstoDB(iterationQueryResponse, dc.ClientUId, dc.DeliveryConstructUID);
                                if (iterationQueryResponse.TotalPageCount > 1)
                                {
                                    for (int i = 2; i < iterationQueryResponse.TotalPageCount + 1; i++)
                                    {
                                        string result1 = InvokeIterationsQueryAPI(dc.ClientUId, dc.DeliveryConstructUID, i);
                                        if (!string.IsNullOrEmpty(result1))
                                        {
                                            WINSERVICEMODELS.PhoenixPayloads.IterationQueryResponse iterationQueryResponse1
                                            = JsonConvert.DeserializeObject<WINSERVICEMODELS.PhoenixPayloads.IterationQueryResponse>(result1);
                                            UpdateIterationstoDB(iterationQueryResponse1, dc.ClientUId, dc.DeliveryConstructUID);
                                        }
                                        else
                                        {
                                            break;
                                        }

                                    }
                                }
                            }
                        }
                    }
                }
                UpdateTaskStatus(0, "IAITERATIONSPULL", true, "C", "Iterations Fetch Completed");
            }
            catch (Exception ex)
            {
                UpdateTaskStatus(0, "IAITERATIONSPULL", true, "E", ex.Message);
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(PredictionSchedulerService), nameof(FetchIterationsList), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }

        }

        public void UpdateIterationstoDB(WINSERVICEMODELS.PhoenixPayloads.IterationQueryResponse iterations, string clientUId, string deliveryConstructUId)
        {
            if (iterations != null)
            {
                if (iterations.Iterations.Count > 0)
                {
                    foreach (var iteration in iterations.Iterations)
                    {
                        // consider only waterfall releases
                        if (iteration.MethodologyUId == "00200870-0020-0000-0000-000000000000" || iteration.MethodologyUId == "00200870-0030-0000-0000-000000000000")
                        {
                            SaveIterationIdstoDB(clientUId, deliveryConstructUId, iteration.IterationUId, iteration.EndOn, iteration.MethodologyUId);

                        }

                    }
                }
            }
        }




        public void SaveIterationIdstoDB(string clientUId, string deliveryConstructUId, string iterationUId, string endOn, string methodologyUId)
        {
            var collection = _database.GetCollection<WINSERVICEMODELS.PhoenixIterations>("PhoenixIterations");
            var filter = Builders<WINSERVICEMODELS.PhoenixIterations>.Filter.Where(x => x.ClientUId == clientUId)
                         & Builders<WINSERVICEMODELS.PhoenixIterations>.Filter.Where(x => x.DeliveryConstructUId == deliveryConstructUId)
                         & Builders<WINSERVICEMODELS.PhoenixIterations>.Filter.Where(x => x.ItemUId == iterationUId);
            var projection = Builders<WINSERVICEMODELS.PhoenixIterations>.Projection.Exclude("_id");
            var result = collection.Find(filter).Project<WINSERVICEMODELS.PhoenixIterations>(projection).FirstOrDefault();
            if (result == null)
            {
                if (!string.IsNullOrEmpty(endOn))
                {
                    DateTime endDate = DateTime.Parse(endOn);
                    DateTime currDate = DateTime.UtcNow;
                    if (endDate > currDate)
                    {
                        WINSERVICEMODELS.PhoenixIterations iteration = new WINSERVICEMODELS.PhoenixIterations()
                        {
                            ClientUId = clientUId,
                            DeliveryConstructUId = deliveryConstructUId,
                            EntityUId = IterationEntityUId,
                            ItemUId = iterationUId,
                            MethodologyUId = methodologyUId,
                            isRecommendationCompleted = false,
                            LastRecordsPullUpdateTime = DateTime.UtcNow,
                            IterationEndOn = DateTime.Parse(endOn),
                            CreatedBy =appSettings.IsAESKeyVault? CryptographyUtility.Encrypt(PredictionSchedulerUserName): AesProvider.Encrypt(PredictionSchedulerUserName, appSettings.aesKey, appSettings.aesVector),
                            CreatedOn = DateTime.UtcNow,
                            ModifiedBy =appSettings.IsAESKeyVault? CryptographyUtility.Encrypt(PredictionSchedulerUserName): AesProvider.Encrypt(PredictionSchedulerUserName, appSettings.aesKey, appSettings.aesVector),
                            ModifiedOn = DateTime.UtcNow
                        };
                        collection.InsertOne(iteration);
                    }
                }

            }
            else
            {
                if (!string.IsNullOrEmpty(endOn))
                {
                    DateTime endDate = DateTime.Parse(endOn);
                    DateTime currDate = DateTime.UtcNow;
                    if (endDate > currDate)
                    {
                        var update = Builders<WINSERVICEMODELS.PhoenixIterations>.Update.Set(x => x.LastRecordsPullUpdateTime, DateTime.UtcNow)
                                                                                        .Set(x => x.IterationEndOn, endDate)
                                                                                        .Set(x => x.MethodologyUId, methodologyUId)
                                                                                        .Set(x => x.ModifiedOn, DateTime.UtcNow);
                        collection.UpdateOne(filter, update);
                    }
                    else
                    {
                        collection.DeleteOne(filter);
                    }
                }

            }
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


        public void UpdateBulkIterationRecommendationStatusinDB(string clientUId, string deliveryConstructUId, List<string> itemUId, bool status, string message)
        {
            var collection = _database.GetCollection<WINSERVICEMODELS.PhoenixIterations>(CONSTANTS.PhoenixIterations);
            var filter = Builders<WINSERVICEMODELS.PhoenixIterations>.Filter.Where(x => x.ClientUId == clientUId)
                         & Builders<WINSERVICEMODELS.PhoenixIterations>.Filter.Where(x => x.DeliveryConstructUId == deliveryConstructUId)
                         & Builders<WINSERVICEMODELS.PhoenixIterations>.Filter.In("ItemUId", itemUId);
            var projection = Builders<WINSERVICEMODELS.PhoenixIterations>.Projection.Exclude("_id");
            var result = collection.Find(filter).Project<WINSERVICEMODELS.PhoenixIterations>(projection).ToList();
            if (result.Count > 0)
            {
                if (status)
                {
                    var update = Builders<WINSERVICEMODELS.PhoenixIterations>.Update.Set(x => x.isRecommendationCompleted, true)
                                                                                    .Set(x => x.Status, "C")
                                                                                                    .Set(x => x.LastRecommendationUpdateTime, DateTime.UtcNow)
                                                                                                    .Set(x => x.Message, message)
                                                                                                    .Set(x => x.ModifiedOn, DateTime.UtcNow);
                    collection.UpdateMany(filter, update);
                }
                else
                {
                    var update = Builders<WINSERVICEMODELS.PhoenixIterations>.Update.Set(x => x.Message, message)
                                                                                    .Set(x => x.isRecommendationCompleted, true)
                                                                                    .Set(x => x.Status, "E")
                                                                                    .Set(x => x.ModifiedOn, DateTime.UtcNow);
                    collection.UpdateMany(filter, update);
                }


            }

        }

        public string InvokeIterationsQueryAPI(string clientUId, string deliveryConstructUId, int pageNumber)
        {
            string jsonResult = null;
            string token = GenerateToken();
            string apiPath = String.Format(_iterationQueryAPI, clientUId, deliveryConstructUId);
            var postContent = new
            {
                ClientUId = clientUId,
                DeliveryConstructUId = deliveryConstructUId,
                PageNumber = pageNumber,
                BatchSize = 100
            };
            StringContent content = new StringContent(JsonConvert.SerializeObject(postContent), Encoding.UTF8, "application/json");
            HttpResponseMessage httpResponseMessage = PhoenixPOSTRequest(token, appSettings.myWizardAPIUrl, apiPath, content, null);
            if (httpResponseMessage.StatusCode == HttpStatusCode.OK)
            {
                jsonResult = httpResponseMessage.Content.ReadAsStringAsync().Result;
            }
            else
            {
                // add logger

            }
            return jsonResult;
        }

        public List<DATAMODELS.DeployModelsDto> CheckSPIDefectRateModels()
        {
            string encryptedUser = PredictionSchedulerUserName;
            if (!string.IsNullOrEmpty(Convert.ToString(encryptedUser)))
            {
                if (appSettings.IsAESKeyVault)
                {
                    encryptedUser = CryptographyUtility.Encrypt(Convert.ToString(encryptedUser));
                }
                else
                {
                    encryptedUser = AesProvider.Encrypt(Convert.ToString(encryptedUser), appSettings.aesKey, appSettings.aesVector);
                }
            }
            var deployedModels = _database.GetCollection<DATAMODELS.DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
            var filter = Builders<DATAMODELS.DeployModelsDto>.Filter.In("TemplateUsecaseId", SSAIUseCaseIds)
                         & Builders<DATAMODELS.DeployModelsDto>.Filter.Where(x => (x.CreatedByUser == PredictionSchedulerUserName || x.CreatedByUser == encryptedUser))
                         & Builders<DATAMODELS.DeployModelsDto>.Filter.Where(x => x.Status == CONSTANTS.Deployed);
            var projection = Builders<DATAMODELS.DeployModelsDto>.Projection.Exclude("_id");
            var result = deployedModels.Find(filter).Project<DATAMODELS.DeployModelsDto>(projection).ToList();
            return result;
        }

        public List<WINSERVICEMODELS.PhoenixIterations> GetNotRecommendedIterations()
        {
            var predFrequency = CheckSPIDefectRateFrequency();
            DateTime pullTime = DateTime.UtcNow.AddDays(-predFrequency);
            DateTime modifiedTime = DateTime.UtcNow.AddHours(-1);
            var collection = _database.GetCollection<WINSERVICEMODELS.PhoenixIterations>(CONSTANTS.PhoenixIterations);
            var filter = Builders<WINSERVICEMODELS.PhoenixIterations>.Filter.Where(x => x.isRecommendationCompleted == false)
                         & Builders<WINSERVICEMODELS.PhoenixIterations>.Filter.Gte(x => x.LastRecordsPullUpdateTime, pullTime);


            var projection = Builders<WINSERVICEMODELS.PhoenixIterations>.Projection.Exclude("_id");
            var result = collection.Find(filter).Project<WINSERVICEMODELS.PhoenixIterations>(projection).ToList();
            return result;
        }


        public List<WINSERVICEMODELS.PhoenixIterations> GetErroredIterations()
        {
            DateTime pullTime = DateTime.UtcNow.AddDays(-3);
            DateTime modifiedTime = DateTime.UtcNow.AddHours(-1);
            var collection = _database.GetCollection<WINSERVICEMODELS.PhoenixIterations>(CONSTANTS.PhoenixIterations);
            var filter = Builders<WINSERVICEMODELS.PhoenixIterations>.Filter.Where(x => x.isRecommendationCompleted == true)
                         & Builders<WINSERVICEMODELS.PhoenixIterations>.Filter.Where(x => x.Status == "E")
                         & Builders<WINSERVICEMODELS.PhoenixIterations>.Filter.Gte(x => x.LastRecordsPullUpdateTime, pullTime);


            var projection = Builders<WINSERVICEMODELS.PhoenixIterations>.Projection.Exclude("_id");
            var result = collection.Find(filter).Project<WINSERVICEMODELS.PhoenixIterations>(projection).ToList();
            return result;
        }


        public List<WINSERVICEMODELS.PhoenixIterations> GetIterationsRecommendationstobeUpdated()
        {
            var collection = _database.GetCollection<WINSERVICEMODELS.PhoenixIterations>(CONSTANTS.PhoenixIterations);
            var filter = Builders<WINSERVICEMODELS.PhoenixIterations>.Filter.Where(x => x.isRecommendationCompleted)
                         & Builders<WINSERVICEMODELS.PhoenixIterations>.Filter.Where(x => x.Status == "C")
                         & Builders<WINSERVICEMODELS.PhoenixIterations>.Filter.Gt(x => x.LastRecordsPullUpdateTime, DateTime.UtcNow.AddDays(-3))
                         & Builders<WINSERVICEMODELS.PhoenixIterations>.Filter.Lt(x => x.LastRecommendationUpdateTime, DateTime.UtcNow.AddDays(-1));
            var projection = Builders<WINSERVICEMODELS.PhoenixIterations>.Projection.Exclude("_id");
            var result = collection.Find(filter).Project<WINSERVICEMODELS.PhoenixIterations>(projection).ToList();
            return result;
        }

        #endregion
    }
}
