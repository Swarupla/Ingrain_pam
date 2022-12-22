using System;
using System.Collections.Generic;
using System.Text;
using WINSERVICEMODELS = Accenture.MyWizard.Ingrain.WindowService.Models;
using DATAACCESS = Accenture.MyWizard.Ingrain.DataAccess;
using Accenture.MyWizard.Ingrain.DataModels.InferenceEngineModel;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
using Accenture.MyWizard.Shared.Helpers;
using Microsoft.Extensions.Configuration;
using System.Threading;
using System.Linq;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Accenture.MyWizard.Cryptography.EncryptionProviders;
using CryptographyHelper = Accenture.MyWizard.Cryptography;

namespace Accenture.MyWizard.Ingrain.WindowService.Services
{
    public class InferenceAutoTrainService
    {
        private readonly WINSERVICEMODELS.AppSettings appSettings = null;

        private DATAACCESS.IInferenceEngineDBContext _inferenceEngineDBContext;
        private WebHelper webHelper;
        public List<string> IEGenricVDSUsecases { get; set; }


        private TokenService _tokenService;
        static CryptographyHelper.Utility.CryptographyUtility CryptographyUtility = null;
        public InferenceAutoTrainService()
        {
            appSettings = AppSettingsJson.GetAppSettings().GetSection("AppSettings").Get<WINSERVICEMODELS.AppSettings>();
            _inferenceEngineDBContext = new DATAACCESS.InferenceEngineDBContext(appSettings.IEConnectionString, appSettings.certificatePath, appSettings.certificatePassKey, appSettings.aesKey, appSettings.aesVector, appSettings.IsAESKeyVault);
            _tokenService = new TokenService();
            webHelper = new WebHelper();
            IEGenricVDSUsecases = appSettings.IEGenricVDSUsecases;
            CryptographyUtility = new CryptographyHelper.Utility.CryptographyUtility();
        }


        public void ModelAutoTrain(IERequestQueue autoTrainRequest)
        {
            try
            {
                TrainModel(autoTrainRequest);
                var request = _inferenceEngineDBContext.IERequestQueueRepository.GetRequestQueuebyequestId(autoTrainRequest.CorrelationId, autoTrainRequest.pageInfo, autoTrainRequest.RequestId);
                if (request.Status == "P" && request.Progress == "25")
                {
                    GenerateFeaturesAndCombinations(request);
                    request = _inferenceEngineDBContext.IERequestQueueRepository.GetRequestQueuebyequestId(autoTrainRequest.CorrelationId, autoTrainRequest.pageInfo, autoTrainRequest.RequestId);
                }
                if (request.Status == "P" && request.Progress == "50")
                {
                    GenerateInferences(request);
                }
                request = _inferenceEngineDBContext.IERequestQueueRepository.GetRequestQueuebyequestId(autoTrainRequest.CorrelationId, autoTrainRequest.pageInfo, autoTrainRequest.RequestId);

                if ((request.Status == "C" || request.Status == "E") && request.ApplicationId != null)
                {
                    SendIEPublishNofication(request.ApplicationId, request.CorrelationId, "Created", request.Status, request.Message);
                }

            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InferenceAutoTrainService), nameof(ModelAutoTrain), ex.Message + ex.StackTrace, ex, autoTrainRequest.ApplicationId, string.Empty, autoTrainRequest.ClientUId, autoTrainRequest.DeliveryConstructUId);
                _inferenceEngineDBContext.IERequestQueueRepository.UpdateAutoTrainRequest(
                       autoTrainRequest.RequestId,
                       autoTrainRequest.CorrelationId,
                       "AutoTrain",
                       "E",
                       ex.Message,
                       "100");
                if (autoTrainRequest.ApplicationId != null)
                {
                    SendIEPublishNofication(autoTrainRequest.ApplicationId, autoTrainRequest.CorrelationId, "Created", "E", ex.Message);
                }
            }

        }

        public void ModelAutoReTrain(IERequestQueue autoReTrainRequest)
        {
            try
            {
                BackupRecords(autoReTrainRequest);
                ReTrainModel(autoReTrainRequest);
                var request = _inferenceEngineDBContext.IERequestQueueRepository.GetRequestQueuebyequestId(autoReTrainRequest.CorrelationId, autoReTrainRequest.pageInfo, autoReTrainRequest.RequestId);
                var reTrainTask = _inferenceEngineDBContext.IEAutoReTrainRepository.GetTaskDetails("IE AUTORETRAIN");
                if (request.Status == "P" && request.Progress == "25")
                {
                    var configs = _inferenceEngineDBContext.InferenceConfigRepository.GetAllIEConfigs(autoReTrainRequest.CorrelationId, false);
                    IEAppIntegration app = new IEAppIntegration();

                    int? configDays = Convert.ToInt32(reTrainTask.ConfigRefreshDays);
                    if (autoReTrainRequest.ApplicationId != null)
                    {
                        app = _inferenceEngineDBContext.IEAppIngerationRepository.GetAppDetailsOnId(autoReTrainRequest.ApplicationId);
                        configDays = app.ConfigRefreshDays != null ? app.ConfigRefreshDays : configDays;
                    }

                    if (configs.Count > 0)
                    {
                        DateTime curTime = DateTime.UtcNow;
                        int? diff = (int)(curTime - configs[0].ModifiedOn).TotalDays;
                        if (diff >= configDays)
                        {
                            RegenerateFeaturesAndCombinations(request);
                        }
                        else
                        {
                            _inferenceEngineDBContext.IERequestQueueRepository.UpdateAutoTrainRequest(
                               autoReTrainRequest.RequestId,
                               autoReTrainRequest.CorrelationId,
                               "AutoReTrain",
                               "P",
                               "Request is in progress, skipped feature regeneration",
                               "50");
                        }
                    }
                    else
                    {
                        _inferenceEngineDBContext.IERequestQueueRepository.UpdateAutoTrainRequest(
                           autoReTrainRequest.RequestId,
                           autoReTrainRequest.CorrelationId,
                           "AutoReTrain",
                           "P",
                           "Request is in progress, skipped feature regeneration",
                           "50");
                    }


                    request = _inferenceEngineDBContext.IERequestQueueRepository.GetRequestQueuebyequestId(autoReTrainRequest.CorrelationId, autoReTrainRequest.pageInfo, autoReTrainRequest.RequestId);
                }
                if (request.Status == "P" && request.Progress == "50")
                {
                    var configs = _inferenceEngineDBContext.InferenceConfigRepository.GetAllIEConfigs(autoReTrainRequest.CorrelationId, false);
                    IEAppIntegration app = new IEAppIntegration();

                    int? configDays = Convert.ToInt32(reTrainTask.ConfigRefreshDays);
                    if (autoReTrainRequest.ApplicationId != null)
                    {
                        app = _inferenceEngineDBContext.IEAppIngerationRepository.GetAppDetailsOnId(autoReTrainRequest.ApplicationId);
                        configDays = app.ConfigRefreshDays != null ? app.ConfigRefreshDays : configDays;
                    }

                    if (configs.Count > 0)
                    {
                        DateTime curTime = DateTime.UtcNow;
                        int? diff = (int)(curTime - configs[0].ModifiedOn).TotalDays;
                        if (diff >= configDays)
                        {
                            UpdateIEConfigs(autoReTrainRequest);
                        }
                    }


                    ReGenerateInferences(request);
                }
                request = _inferenceEngineDBContext.IERequestQueueRepository.GetRequestQueuebyequestId(autoReTrainRequest.CorrelationId, autoReTrainRequest.pageInfo, autoReTrainRequest.RequestId);
                if (request.Status == "C")
                {
                    DeleteBackupRecords(autoReTrainRequest);
                }
                if (request.Status == "E")
                {
                    RestoreRecords(autoReTrainRequest);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InferenceAutoTrainService), nameof(ModelAutoTrain), ex.Message + ex.StackTrace, ex, autoReTrainRequest.ApplicationId, string.Empty, autoReTrainRequest.ClientUId, autoReTrainRequest.DeliveryConstructUId);
                _inferenceEngineDBContext.IERequestQueueRepository.UpdateAutoTrainRequest(
                       autoReTrainRequest.RequestId,
                       autoReTrainRequest.CorrelationId,
                       "AutoReTrain",
                       "E",
                       ex.Message,
                       "100");
                RestoreRecords(autoReTrainRequest);
            }

        }


        public void BackupRecords(IERequestQueue autoReTrainRequest)
        {
            _inferenceEngineDBContext.IEModelRepository.BackupRecords(autoReTrainRequest.CorrelationId);
            _inferenceEngineDBContext.IEModelRepository.BackupIngestDataRecords(autoReTrainRequest.CorrelationId);
            _inferenceEngineDBContext.IEModelRepository.BackupPSMultiFileRecords(autoReTrainRequest.CorrelationId);
            _inferenceEngineDBContext.IEModelRepository.BackupUseCaseDefRecords(autoReTrainRequest.CorrelationId);
            _inferenceEngineDBContext.InferenceConfigRepository.BackupIEConfig(autoReTrainRequest.CorrelationId);
            _inferenceEngineDBContext.InferenceConfigRepository.BackupFeatureComb(autoReTrainRequest.CorrelationId);
            _inferenceEngineDBContext.InferenceConfigRepository.BackupSavedConfig(autoReTrainRequest.CorrelationId);
            _inferenceEngineDBContext.InferenceConfigRepository.BackUpResults(autoReTrainRequest.CorrelationId);
        }

        public void DeleteBackupRecords(IERequestQueue autoReTrainRequest)
        {
            _inferenceEngineDBContext.IEModelRepository.DeleteIEModel(autoReTrainRequest.CorrelationId + "_backup");
            _inferenceEngineDBContext.IEModelRepository.DeleteIngestData(autoReTrainRequest.CorrelationId + "_backup");
            _inferenceEngineDBContext.IEModelRepository.DeletePSMultiFileRecords(autoReTrainRequest.CorrelationId + "_backup");
            _inferenceEngineDBContext.IEModelRepository.DeleteUseCaseDefRecords(autoReTrainRequest.CorrelationId + "_backup");
            _inferenceEngineDBContext.InferenceConfigRepository.DeleteConfig(autoReTrainRequest.CorrelationId + "_backup");
            _inferenceEngineDBContext.InferenceConfigRepository.DeleteFeatureCombination(autoReTrainRequest.CorrelationId + "_backup");
            _inferenceEngineDBContext.InferenceConfigRepository.DeleteIEConfig(autoReTrainRequest.CorrelationId + "_backup");
            _inferenceEngineDBContext.InferenceConfigRepository.DeleteInferenceResults(autoReTrainRequest.CorrelationId + "_backup");
            _inferenceEngineDBContext.IERequestQueueRepository.DeleteQueueRequest(autoReTrainRequest.CorrelationId + "_backup");
        }



        public void RestoreRecords(IERequestQueue autoReTrainRequest)
        {
            _inferenceEngineDBContext.IEModelRepository.RestoreRecords(autoReTrainRequest.CorrelationId);
            _inferenceEngineDBContext.IEModelRepository.RestoreIngestDataRecords(autoReTrainRequest.CorrelationId);
            _inferenceEngineDBContext.IEModelRepository.RestorePSMultiFileRecords(autoReTrainRequest.CorrelationId);
            _inferenceEngineDBContext.IEModelRepository.RestoreUseCaseDefRecords(autoReTrainRequest.CorrelationId);
            _inferenceEngineDBContext.InferenceConfigRepository.RestoreIEConfig(autoReTrainRequest.CorrelationId);
            _inferenceEngineDBContext.InferenceConfigRepository.RestoreFeatureComb(autoReTrainRequest.CorrelationId);
            _inferenceEngineDBContext.InferenceConfigRepository.RestoreSavedConfig(autoReTrainRequest.CorrelationId);
            _inferenceEngineDBContext.InferenceConfigRepository.RestoreResults(autoReTrainRequest.CorrelationId);
        }

        public void TrainModel(IERequestQueue autoTrainRequest)
        {
            var useCase = _inferenceEngineDBContext.IEUseCaseRepository.GetUseCase(autoTrainRequest.UseCaseId);
            var useCaseModel = _inferenceEngineDBContext.IEModelRepository.GetIEModel(useCase.CorrelationId);

            //delete existing model

            _inferenceEngineDBContext.IEModelRepository.DeleteIEModel(autoTrainRequest.CorrelationId);
            _inferenceEngineDBContext.InferenceConfigRepository.DeleteConfig(autoTrainRequest.CorrelationId);

            _inferenceEngineDBContext.IERequestQueueRepository.DeleteAutoTrainQueueRequest(autoTrainRequest.CorrelationId);
            _inferenceEngineDBContext.InferenceConfigRepository.DeletePublishedConfig(autoTrainRequest.CorrelationId);



            _inferenceEngineDBContext.IERequestQueueRepository.UpdateAutoTrainRequest(
                        autoTrainRequest.RequestId,
                        autoTrainRequest.CorrelationId,
                        "AutoTrain",
                        "P",
                        "Request is in progress",
                        "10");

            string FunctionalArea = useCaseModel.FunctionalArea;
            string Entity = useCaseModel.Entity;
            string SourceName = useCaseModel.SourceName;
            string ModelName = useCase.UseCaseName + "_" + autoTrainRequest.CorrelationId;
            if ((appSettings.Environment.Equals(CONSTANTS.FDSEnvironment) || appSettings.Environment.Equals(CONSTANTS.PAMEnvironment)) && autoTrainRequest.SourceName == "VDS")
            {
                var fileParams = JsonConvert.DeserializeObject<IEFileUpload>(autoTrainRequest.ParamArgs);
                if (fileParams != null && Convert.ToString(fileParams.Customdetails) != null && Convert.ToString(fileParams.Customdetails.InputParameters) != null)
                {
                    FunctionalArea = fileParams.Customdetails.InputParameters.RequestType;
                    Entity = fileParams.Customdetails.InputParameters.ServiceType;
                    SourceName = autoTrainRequest.SourceName;
                    ModelName = useCase.UseCaseName + "_" + autoTrainRequest.CorrelationId + "_" + FunctionalArea + "_" + Entity;
                }
            }
            var iEModel = new IEModel
            {
                CorrelationId = autoTrainRequest.CorrelationId,
                ClientUId = autoTrainRequest.ClientUId,
                DeliveryConstructUId = autoTrainRequest.DeliveryConstructUId,
                ApplicationId = autoTrainRequest.ApplicationId,
                UseCaseId = autoTrainRequest.UseCaseId,
                DBEncryptionRequired = useCaseModel.DBEncryptionRequired,
                FunctionalArea = FunctionalArea,
                Entity = Entity,
                SourceName = SourceName,
                IsPrivate = true,
                ModelName = ModelName,
                CreatedBy = autoTrainRequest.CreatedBy,
                CreatedOn = DateTime.UtcNow,
                ModifiedBy = autoTrainRequest.ModifiedBy,
                ModifiedOn = DateTime.UtcNow,
                Status = "P",
                Message = "In-Progress",
                IsIEPublish = autoTrainRequest.IsIEPublish,
                IsPublishUseCase = false
            };
            var Customdetails = JObject.Parse(autoTrainRequest.ParamArgs.ToString());
            if (Customdetails.ContainsKey("Customdetails"))
            {
                if (Customdetails["Customdetails"].ToString() != "null" && !string.IsNullOrEmpty(Customdetails["Customdetails"].ToString()))
                {
                    var pad = JObject.Parse(Customdetails["Customdetails"].ToString());
                    if (pad.ContainsKey("InputParameters"))
                    {
                        //if (pad["InputParameters"].Contains("StartDate"))
                        //{
                        //    iEModel.StartDate = Convert.ToString(pad["InputParameters"]["StartDate"].ToString());
                        //}
                        //if (pad["InputParameters"].Contains("EndDate"))
                        //{
                        //    iEModel.EndDate = Convert.ToString(pad["InputParameters"]["EndDate"].ToString());
                        //}
                        if (pad["InputParameters"].ToString() != "null" && !string.IsNullOrEmpty(pad["InputParameters"].ToString()))
                        {
                            var InputParameters = JObject.Parse(pad["InputParameters"].ToString());
                            if (InputParameters.ContainsKey("StartDate") && !string.IsNullOrEmpty(InputParameters["StartDate"].ToString()))
                            {
                                iEModel.StartDate = Convert.ToString(InputParameters["StartDate"].ToString());
                            }
                            if (InputParameters.ContainsKey("EndDate") && !string.IsNullOrEmpty(InputParameters["EndDate"].ToString()))
                            {
                                iEModel.EndDate = Convert.ToString(InputParameters["EndDate"].ToString());
                            }
                        }
                    }
                }

            }
            _inferenceEngineDBContext.IEModelRepository.InsertIEModel(iEModel);

            var ingrainRequest = new IERequestQueue
            {
                CorrelationId = autoTrainRequest.CorrelationId,
                DataSetUId = autoTrainRequest.DataSetUId,
                RequestId = Guid.NewGuid().ToString(),
                ClientUId = autoTrainRequest.ClientUId,
                DeliveryConstructUId = autoTrainRequest.DeliveryConstructUId,
                Status = "N",
                RequestStatus = CONSTANTS.New,
                Message = null,
                Progress = null,
                pageInfo = CONSTANTS.IngestData,
                ParamArgs = autoTrainRequest.ParamArgs,
                Function = CONSTANTS.FileUpload,
                CreatedBy = autoTrainRequest.CreatedBy,
                CreatedOn = DateTime.UtcNow,
                ModifiedBy = autoTrainRequest.ModifiedBy,
                ModifiedOn = DateTime.UtcNow
            };


            _inferenceEngineDBContext.IERequestQueueRepository.IEInsertRequests(ingrainRequest);

            bool flag = true;
            while (flag)
            {
                Thread.Sleep(2000);
                var ingestRequest = _inferenceEngineDBContext.IERequestQueueRepository.GetRequestByPageInfo(
                    autoTrainRequest.CorrelationId,
                    CONSTANTS.IngestData);

                if (ingestRequest.Status == "C" || ingestRequest.Status == "E")
                {
                    _inferenceEngineDBContext.IEModelRepository.UpdateIEModel(
                        ingestRequest.CorrelationId,
                        ingestRequest.Status,
                        ingestRequest.Message,
                        ingestRequest.Progress);

                    string status = ingestRequest.Status == "C" ? "P" : "E";
                    string progress = ingestRequest.Status == "C" ? "25" : "100";
                    string message = ingestRequest.Status == "C" ? "Request is in Progress" : ingestRequest.Message;

                    _inferenceEngineDBContext.IERequestQueueRepository.UpdateAutoTrainRequest(
                        autoTrainRequest.RequestId,
                        autoTrainRequest.CorrelationId,
                        "AutoTrain",
                        status,
                        message,
                        progress);


                    flag = false;

                }

            }




        }



        public void ReTrainModel(IERequestQueue autoReTrainRequest)
        {
            var model = _inferenceEngineDBContext.IEModelRepository.GetIEModel(autoReTrainRequest.CorrelationId);
            _inferenceEngineDBContext.IERequestQueueRepository.UpdateAutoTrainRequest(
                        autoReTrainRequest.RequestId,
                        autoReTrainRequest.CorrelationId,
                        "AutoReTrain",
                        "P",
                        "Request is in progress",
                        "10");

            if (model.SourceName == "Entity" || model.SourceName == "metric" || model.SourceName == CONSTANTS.Custom)
            {
                var ingestRequest = _inferenceEngineDBContext.IERequestQueueRepository.GetRequestByPageInfo(model.CorrelationId, CONSTANTS.IngestData);
                if (ingestRequest.ParamArgs != null)
                {
                    var newRequest = new IERequestQueue
                    {
                        CorrelationId = model.CorrelationId,
                        RequestId = Guid.NewGuid().ToString(),
                        Status = "N",
                        RequestStatus = CONSTANTS.New,
                        Message = null,
                        Progress = null,
                        pageInfo = CONSTANTS.IngestData,
                        Function = "FileUpload",
                        CreatedBy = model.CreatedBy,
                        CreatedOn = DateTime.UtcNow,
                        ModifiedBy = model.ModifiedBy,
                        ModifiedOn = DateTime.UtcNow,

                    };
                    if (model.SourceName == CONSTANTS.Custom)
                    {
                        var fileParams = JsonConvert.DeserializeObject<IEFileUpload>(ingestRequest.ParamArgs);
                        fileParams.Flag = "AutoRetrain";
                        fileParams.Customdetails.InputParameters.StartDate = DateTime.Parse(model.EndDate).ToString(CONSTANTS.DateHoursFormat);
                        fileParams.Customdetails.InputParameters.EndDate = DateTime.UtcNow.ToString(CONSTANTS.DateHoursFormat);
                        if (!appSettings.Environment.Equals(CONSTANTS.PADEnvironment))
                            fileParams.Customdetails.InputParameters.DateColumn = _inferenceEngineDBContext.IERequestQueueRepository.GetIELastDataDict(model.CorrelationId);
                        newRequest.ParamArgs = JsonConvert.SerializeObject(fileParams);
                    }
                    else if (model.SourceName == "Entity" || model.SourceName == "metric")
                    {
                        var fileUpload = JsonConvert.DeserializeObject<IEFileUpload>(ingestRequest.ParamArgs);
                        fileUpload.Customdetails = CONSTANTS.Null;
                        fileUpload.Flag = "AutoRetrain";
                        newRequest.ParamArgs = JsonConvert.SerializeObject(fileUpload);
                    }


                    DateTime startDate = DateTime.Parse(model.StartDate);
                    DateTime endDate = DateTime.Parse(model.EndDate);

                    DateTime curTime = DateTime.UtcNow;

                    int diffInDays = (int)(curTime - endDate).TotalDays;


                    _inferenceEngineDBContext.IERequestQueueRepository.BackupRecords(newRequest.CorrelationId, CONSTANTS.IngestData);


                    _inferenceEngineDBContext.IEModelRepository.UpdateIEModelDates(model.CorrelationId,
                                                                                   startDate.AddDays(diffInDays).ToString("MM/dd/yyyy"),
                                                                                   curTime.ToString("MM/dd/yyyy"));
                    _inferenceEngineDBContext.IERequestQueueRepository.IEInsertRequests(newRequest);

                    bool flag = true;
                    while (flag)
                    {
                        Thread.Sleep(2000);
                        var request = _inferenceEngineDBContext.IERequestQueueRepository.GetRequestByPageInfo(
                            model.CorrelationId,
                            CONSTANTS.IngestData);

                        if (request.Status == "C")
                        {
                            _inferenceEngineDBContext.IEModelRepository.UpdateIEModel(
                                request.CorrelationId,
                                request.Status,
                                request.Message,
                                request.Progress);

                            _inferenceEngineDBContext.IERequestQueueRepository.UpdateAutoTrainRequest(
                                autoReTrainRequest.RequestId,
                                autoReTrainRequest.CorrelationId,
                                "AutoReTrain",
                                "P",
                                "Request is in progress",
                                "25");

                            flag = false;

                        }
                        else if (request.Status == "E")
                        {
                            _inferenceEngineDBContext.IERequestQueueRepository.UpdateAutoTrainRequest(
                                autoReTrainRequest.RequestId,
                                autoReTrainRequest.CorrelationId,
                                "AutoReTrain",
                                "E",
                                request.Message,
                                "100");
                            flag = false;

                        }
                    }
                }
                else
                {
                    _inferenceEngineDBContext.IERequestQueueRepository.UpdateAutoTrainRequest(
                      autoReTrainRequest.RequestId,
                      autoReTrainRequest.CorrelationId,
                      "AutoReTrain",
                      "E",
                      "ParamArgs are null",
                      "100");
                }

            }
            else
            {
                _inferenceEngineDBContext.IERequestQueueRepository.UpdateAutoTrainRequest(
                       autoReTrainRequest.RequestId,
                       autoReTrainRequest.CorrelationId,
                       "AutoReTrain",
                       "E",
                       "Invalid source name-" + model.SourceName,
                       "100");
            }

        }

        public void GenerateFeaturesAndCombinations(IERequestQueue autoTrainRequest)
        {

            _inferenceEngineDBContext.InferenceConfigRepository.DeleteFeatureCombination(autoTrainRequest.CorrelationId);

            var useCase = _inferenceEngineDBContext.IEUseCaseRepository.GetUseCase(autoTrainRequest.UseCaseId);
            var ieConfigs = GetIEConfigs(useCase.CorrelationId);
            var metricConfigs = ieConfigs.Where(x => x.InferenceConfigType == "MeasureAnalysis").ToList();
            if (metricConfigs.Count > 0)
            {
                string status = string.Empty;
                string message = string.Empty;
                foreach (var config in metricConfigs)
                {
                    bool flag = true;
                    var col = new JObject();

                    col.Add("Metric", config.MetricColumn);
                    col.Add("date", config.DateColumn);
                    if (config.FilterValues != null)
                    {
                        col.Add("FilterValues", JObject.Parse(config.FilterValues));
                    }
                    while (flag)
                    {
                        IEREsponse iEREsponse = TriggerFeatureCombination(autoTrainRequest.CorrelationId, autoTrainRequest.CreatedBy, null, col, false);
                        if (iEREsponse.Status == "C" || iEREsponse.Status == "E")
                        {
                            flag = false;

                            if (iEREsponse.Status == "E")
                            {
                                status = "E";
                                message = "Python Error in GetFeatures" + "**" + iEREsponse.Message;
                            }
                        }
                        else
                        {
                            Thread.Sleep(1000);
                        }

                    }
                }

                string progress = status != "E" ? "50" : "100";
                message = string.IsNullOrEmpty(message) ? "Request is in Progress" : message;
                status = string.IsNullOrEmpty(status) ? "P" : status;

                _inferenceEngineDBContext.IERequestQueueRepository.UpdateAutoTrainRequest(
                                autoTrainRequest.RequestId,
                                autoTrainRequest.CorrelationId,
                                "AutoTrain",
                                status,
                                message,
                                progress);
            }
        }

        public void RegenerateFeaturesAndCombinations(IERequestQueue autoReTrainRequest)
        {
            _inferenceEngineDBContext.IERequestQueueRepository.BackupRecords(autoReTrainRequest.CorrelationId, "GetFeatures");
            // _inferenceEngineDBContext.InferenceConfigRepository.DeleteConfig(autoReTrainRequest.CorrelationId);
            _inferenceEngineDBContext.InferenceConfigRepository.DeleteFeatureCombination(autoReTrainRequest.CorrelationId);
            var ieConfigs = GetIEConfigsIncludeAutoGen(autoReTrainRequest.CorrelationId);
            var metricConfigs = ieConfigs.Where(x => x.InferenceConfigType == "MeasureAnalysis").ToList();
            if (metricConfigs.Count > 0)
            {
                string status = string.Empty;
                string message = string.Empty;
                foreach (var config in metricConfigs)
                {
                    bool flag = true;
                    var col = new JObject();

                    col.Add("Metric", config.MetricColumn);
                    col.Add("date", config.DateColumn);
                    if (config.FilterValues != null)
                    {
                        col.Add("FilterValues", JObject.Parse(config.FilterValues));
                    }

                    IEREsponse iEREsponse = TriggerFeatureCombination(autoReTrainRequest.CorrelationId, autoReTrainRequest.CreatedBy, config.InferenceConfigId, col, true);
                    while (flag)
                    {
                        if (iEREsponse.Status == "C" || iEREsponse.Status == "E")
                        {
                            flag = false;

                            if (iEREsponse.Status == "E")
                            {
                                status = "E";
                                message = message + "**" + iEREsponse.Message;
                            }
                        }
                        else
                        {
                            iEREsponse = TriggerFeatureCombination(autoReTrainRequest.CorrelationId, autoReTrainRequest.CreatedBy, config.InferenceConfigId, col, false);
                            Thread.Sleep(1000);
                        }

                    }
                }

                string progress = status != "E" ? "50" : "100";
                message = string.IsNullOrEmpty(message) ? "Request is in Progress" : message;
                status = string.IsNullOrEmpty(status) ? "P" : status;

                _inferenceEngineDBContext.IERequestQueueRepository.UpdateAutoTrainRequest(
                               autoReTrainRequest.RequestId,
                               autoReTrainRequest.CorrelationId,
                               "AutoReTrain",
                               status,
                               message,
                               progress);
            }
            else
            {
                _inferenceEngineDBContext.IERequestQueueRepository.UpdateAutoTrainRequest(
                               autoReTrainRequest.RequestId,
                               autoReTrainRequest.CorrelationId,
                               "AutoReTrain",
                               "P",
                               "No metric config present to regenerate features",
                               "50");
            }
        }

        public void GenerateInferences(IERequestQueue autoTrainRequest)
        {

            //delete old configs
            _inferenceEngineDBContext.InferenceConfigRepository.DeleteIEConfig(autoTrainRequest.CorrelationId);
            _inferenceEngineDBContext.InferenceConfigRepository.DeleteInferenceResults(autoTrainRequest.CorrelationId);
            string UserId= autoTrainRequest.CreatedBy;
            List<string> requestIds = new List<string>();
            var useCase = _inferenceEngineDBContext.IEUseCaseRepository.GetUseCase(autoTrainRequest.UseCaseId);
            var ieConfigs = GetIEConfigs(useCase.CorrelationId);
            if (ieConfigs.Count > 0)
            {
                var metricConfigs = ieConfigs.Where(x => x.InferenceConfigType == "MeasureAnalysis").ToList();
                foreach (var config in metricConfigs)
                {
                    string useCaseconfigId = config.InferenceConfigId;
                    var newConfig = config;
                    newConfig.CorrelationId = autoTrainRequest.CorrelationId;
                    newConfig.InferenceConfigId = Guid.NewGuid().ToString();
                    newConfig.CreatedOn = DateTime.UtcNow;
                    newConfig.ModifiedOn = DateTime.UtcNow;
                    newConfig.CreatedBy = UserId;
                    newConfig.ModifiedBy = UserId;

                    _inferenceEngineDBContext.InferenceConfigRepository.InsertIEConfig(newConfig);

                    var newRequest = new IERequestQueue()
                    {
                        RequestId = Guid.NewGuid().ToString(),
                        CorrelationId = newConfig.CorrelationId,
                        InferenceConfigId = newConfig.InferenceConfigId,
                        InferenceConfigType = newConfig.InferenceConfigType,
                        Status = "N",
                        pageInfo = newConfig.InferenceConfigType == "VolumetricAnalysis" ? "GenerateVolumetric" : "GenerateNarratives",
                        Function = newConfig.InferenceConfigType == "VolumetricAnalysis" ? "GenerateVolumetric" : "GenerateNarratives",
                        CreatedBy = UserId,
                        CreatedOn = DateTime.UtcNow,
                        ModifiedBy = UserId,
                        ModifiedOn = DateTime.UtcNow
                    };
                    _inferenceEngineDBContext.IERequestQueueRepository.IEInsertRequests(newRequest);

                    requestIds.Add(newRequest.RequestId);

                    var volConfig = ieConfigs.Where(x => x.InferenceConfigId == useCaseconfigId && x.InferenceConfigType == "VolumetricAnalysis").FirstOrDefault();
                    if (volConfig != null)
                    {
                        var newVolConfig = volConfig;
                        newVolConfig.CorrelationId = autoTrainRequest.CorrelationId;
                        newVolConfig.InferenceConfigId = newConfig.InferenceConfigId;
                        newVolConfig.CreatedOn = DateTime.UtcNow;
                        newVolConfig.ModifiedOn = DateTime.UtcNow;
                        newVolConfig.CreatedBy = UserId;
                        newVolConfig.ModifiedBy = UserId;

                        _inferenceEngineDBContext.InferenceConfigRepository.InsertIEConfig(newVolConfig);

                        var newVolRequest = new IERequestQueue()
                        {
                            RequestId = Guid.NewGuid().ToString(),
                            CorrelationId = newVolConfig.CorrelationId,
                            InferenceConfigId = newVolConfig.InferenceConfigId,
                            InferenceConfigType = newVolConfig.InferenceConfigType,
                            Status = "N",
                            pageInfo = newVolConfig.InferenceConfigType == "VolumetricAnalysis" ? "GenerateVolumetric" : "GenerateNarratives",
                            Function = newVolConfig.InferenceConfigType == "VolumetricAnalysis" ? "GenerateVolumetric" : "GenerateNarratives",
                            CreatedBy = newVolConfig.CreatedBy,
                            CreatedOn = DateTime.UtcNow,
                            ModifiedBy = newVolConfig.ModifiedBy,
                            ModifiedOn = DateTime.UtcNow
                        };
                        _inferenceEngineDBContext.IERequestQueueRepository.IEInsertRequests(newVolRequest);

                        requestIds.Add(newVolRequest.RequestId);
                    }
                }


                if (requestIds.Count > 0)
                {
                    bool flag = true;
                    while (flag)
                    {
                        var requests = _inferenceEngineDBContext.IERequestQueueRepository.GetIERequestQueueByCorrId(autoTrainRequest.CorrelationId);
                        var configRequests = requests.Where(x => requestIds.Contains(x.RequestId)).ToList();

                        var notCompleted = configRequests.Where(x => x.Status != "C" && x.Status != "E").ToList();

                        if (notCompleted.Count > 0)
                        {
                            Thread.Sleep(2000);
                        }
                        else
                        {
                            flag = false;
                            var errored = configRequests.Where(x => x.Status == "E").ToList();
                            if (errored.Count > 0)
                            {
                                string ErrorMessage = "Python Error in Generate Inferences";
                                if (errored.Count != requestIds.Count)
                                {
                                    var VolumetricError = configRequests.Where(x => x.InferenceConfigType == "VolumetricAnalysis" && x.Status == "E").ToList();
                                    ErrorMessage = ErrorMessage + " for " + (VolumetricError.Count > 0 ? "VolumetricAnalysis" : "MeasureAnalysis");
                                }
                                _inferenceEngineDBContext.IERequestQueueRepository.UpdateAutoTrainRequest(
                                    autoTrainRequest.RequestId,
                                    autoTrainRequest.CorrelationId,
                                    "AutoTrain",
                                    "E",
                                    ErrorMessage,
                                    "100");
                            }
                            else
                            {
                                _inferenceEngineDBContext.IERequestQueueRepository.UpdateAutoTrainRequest(
                                   autoTrainRequest.RequestId,
                                   autoTrainRequest.CorrelationId,
                                   "AutoTrain",
                                   "C",
                                   "Request completed",
                                   "100");
                            }
                        }
                    }
                }
            }
        }



        public void UpdateIEConfigs(IERequestQueue autoReTrainRequest)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InferenceAutoTrainService), "UpdateIEConfig Started-", "START", string.IsNullOrEmpty(autoReTrainRequest.CorrelationId) ? default(Guid) : new Guid(autoReTrainRequest.CorrelationId), autoReTrainRequest.ApplicationId, string.Empty, autoReTrainRequest.ClientUId, autoReTrainRequest.DeliveryConstructUId);
            var ieConfigs = GetIEConfigsIncludeAutoGen(autoReTrainRequest.CorrelationId);
            if (ieConfigs.Count > 0)
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(InferenceAutoTrainService), "GetIEConfig Executed-", "ConfigCount" + ieConfigs.Count, string.IsNullOrEmpty(autoReTrainRequest.CorrelationId) ? default(Guid) : new Guid(autoReTrainRequest.CorrelationId), autoReTrainRequest.ApplicationId, string.Empty, autoReTrainRequest.ClientUId, autoReTrainRequest.DeliveryConstructUId);
                foreach (var config in ieConfigs)
                {
                    if (config.InferenceConfigType == "MeasureAnalysis")
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(InferenceAutoTrainService), "IEConfig Loop Started-", "ConfigType - MeasureAnalysis :Id" + config.InferenceConfigId, string.IsNullOrEmpty(autoReTrainRequest.CorrelationId) ? default(Guid) : new Guid(autoReTrainRequest.CorrelationId), autoReTrainRequest.ApplicationId, string.Empty, autoReTrainRequest.ClientUId, autoReTrainRequest.DeliveryConstructUId);
                        var featureCombinationsData = _inferenceEngineDBContext.InferenceConfigRepository.GetFeatureCombination(autoReTrainRequest.CorrelationId, config.MetricColumn, config.DateColumn);
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(InferenceAutoTrainService), "FeatureCom Loop Started-", "ConfigType - MeasureAnalysis :Id" + config.InferenceConfigId, string.IsNullOrEmpty(autoReTrainRequest.CorrelationId) ? default(Guid) : new Guid(autoReTrainRequest.CorrelationId), autoReTrainRequest.ApplicationId, string.Empty, autoReTrainRequest.ClientUId, autoReTrainRequest.DeliveryConstructUId);
                        var features = featureCombinationsData.Features ?? featureCombinationsData.Features.ToList();
                        if (config.DeselectedFeatures != null)
                        {
                            if (config.DeselectedFeatures.Count > 0)
                            {
                                foreach (var ft in config.DeselectedFeatures.ToList())
                                {
                                    features.Remove(ft);
                                }
                            }
                        }
                        config.Features = features;
                        var featureComb = featureCombinationsData.FeatureCombinations ?? featureCombinationsData.FeatureCombinations.ToList();
                        if (config.DeselectedFeatureCombinations != null)
                        {
                            if (config.DeselectedFeatureCombinations.Count > 0)
                            {
                                featureComb.RemoveAll(x => config.DeselectedFeatureCombinations.Any(y => x.FeatureName.Equals(y.FeatureName) && x.ConnectedFeatures.Equals(y.ConnectedFeatures)));
                                //foreach (var ft in config.DeselectedFeatureCombinations.ToList())
                                //{
                                //    //featureComb.RemoveAll(x => x.ConnectedFeatures.Equals(ft.ConnectedFeatures) && x.FeatureName.Equals(ft.FeatureName));
                                //    featureComb.RemoveAll(x => ft.);
                                //    //featureComb.Remove(ft);                                    
                                //}
                            }
                        }
                        config.FeatureCombinations = featureComb;
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(InferenceAutoTrainService), "Before Update DB-", "ConfigType - MeasureAnalysis :Id" + config.InferenceConfigId, string.IsNullOrEmpty(autoReTrainRequest.CorrelationId) ? default(Guid) : new Guid(autoReTrainRequest.CorrelationId), autoReTrainRequest.ApplicationId, string.Empty, autoReTrainRequest.ClientUId, autoReTrainRequest.DeliveryConstructUId);
                        _inferenceEngineDBContext.InferenceConfigRepository.UpdateIEConfig(config);
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(InferenceAutoTrainService), "After Update DB-", "ConfigType - MeasureAnalysis :Id" + config.InferenceConfigId, string.IsNullOrEmpty(autoReTrainRequest.CorrelationId) ? default(Guid) : new Guid(autoReTrainRequest.CorrelationId), autoReTrainRequest.ApplicationId, string.Empty, autoReTrainRequest.ClientUId, autoReTrainRequest.DeliveryConstructUId);

                    }

                    if (config.InferenceConfigType == "VolumetricAnalysis")
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(InferenceAutoTrainService), "IEConfig Loop Started-", "ConfigType - Volumetric :Id" + config.InferenceConfigId, string.IsNullOrEmpty(autoReTrainRequest.CorrelationId) ? default(Guid) : new Guid(autoReTrainRequest.CorrelationId), autoReTrainRequest.ApplicationId, string.Empty, autoReTrainRequest.ClientUId, autoReTrainRequest.DeliveryConstructUId);
                        var dimensionsData = _inferenceEngineDBContext.InferenceConfigRepository.GetConfig(autoReTrainRequest.CorrelationId);
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(InferenceAutoTrainService), "PythonIEConfig Loop Started-", "ConfigType - Volumetric :Id" + config.InferenceConfigId, string.IsNullOrEmpty(autoReTrainRequest.CorrelationId) ? default(Guid) : new Guid(autoReTrainRequest.CorrelationId), autoReTrainRequest.ApplicationId, string.Empty, autoReTrainRequest.ClientUId, autoReTrainRequest.DeliveryConstructUId);
                        var dimensions = dimensionsData.DimensionsList;
                        if (config.DeselectedDimensions != null)
                        {
                            if (config.DeselectedDimensions.Count > 0)
                            {
                                foreach (var ft in config.DeselectedDimensions)
                                {
                                    dimensions.Remove(ft);
                                }
                            }
                        }
                        config.Dimensions = dimensions;
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(InferenceAutoTrainService), "Before Update DB-", "ConfigType - Volumetric :Id" + config.InferenceConfigId, string.IsNullOrEmpty(autoReTrainRequest.CorrelationId) ? default(Guid) : new Guid(autoReTrainRequest.CorrelationId), autoReTrainRequest.ApplicationId, string.Empty, autoReTrainRequest.ClientUId, autoReTrainRequest.DeliveryConstructUId);
                        _inferenceEngineDBContext.InferenceConfigRepository.UpdateIEConfig(config);
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(InferenceAutoTrainService), "After Update DB-", "ConfigType - Volumetric :Id" + config.InferenceConfigId, string.IsNullOrEmpty(autoReTrainRequest.CorrelationId) ? default(Guid) : new Guid(autoReTrainRequest.CorrelationId), autoReTrainRequest.ApplicationId, string.Empty, autoReTrainRequest.ClientUId, autoReTrainRequest.DeliveryConstructUId);
                    }
                }

                _inferenceEngineDBContext.IERequestQueueRepository.UpdateAutoTrainRequest(
                                  autoReTrainRequest.RequestId,
                                  autoReTrainRequest.CorrelationId,
                                  "AutoReTrain",
                                  "P",
                                  "Request is in progress",
                                  "75");
            }
        }



        public void ReGenerateInferences(IERequestQueue autoReTrainRequest)
        {

            //delete old configs           
            _inferenceEngineDBContext.InferenceConfigRepository.DeleteInferenceResults(autoReTrainRequest.CorrelationId);
            _inferenceEngineDBContext.IERequestQueueRepository.BackupRecords(autoReTrainRequest.CorrelationId, "GenerateVolumetric");
            _inferenceEngineDBContext.IERequestQueueRepository.BackupRecords(autoReTrainRequest.CorrelationId, "GenerateNarratives");

            List<string> requestIds = new List<string>();
            var ieConfigs = GetIEConfigsIncludeAutoGen(autoReTrainRequest.CorrelationId);
            if (ieConfigs.Count > 0)
            {
                foreach (var config in ieConfigs)
                {
                    var newRequest = new IERequestQueue()
                    {
                        RequestId = Guid.NewGuid().ToString(),
                        CorrelationId = config.CorrelationId,
                        InferenceConfigId = config.InferenceConfigId,
                        InferenceConfigType = config.InferenceConfigType,
                        Status = "N",
                        pageInfo = config.InferenceConfigType == "VolumetricAnalysis" ? "GenerateVolumetric" : "GenerateNarratives",
                        Function = config.InferenceConfigType == "VolumetricAnalysis" ? "GenerateVolumetric" : "GenerateNarratives",
                        CreatedBy = config.CreatedBy,
                        CreatedOn = DateTime.UtcNow,
                        ModifiedBy = config.ModifiedBy,
                        ModifiedOn = DateTime.UtcNow
                    };
                    _inferenceEngineDBContext.IERequestQueueRepository.IEInsertRequests(newRequest);

                    requestIds.Add(newRequest.RequestId);
                }


                if (requestIds.Count > 0)
                {
                    bool flag = true;
                    while (flag)
                    {
                        var requests = _inferenceEngineDBContext.IERequestQueueRepository.GetIERequestQueueByCorrId(autoReTrainRequest.CorrelationId);
                        var configRequests = requests.Where(x => requestIds.Contains(x.RequestId)).ToList();

                        var notCompleted = configRequests.Where(x => x.Status != "C" && x.Status != "E").ToList();

                        if (notCompleted.Count > 0)
                        {
                            Thread.Sleep(2000);
                        }
                        else
                        {
                            flag = false;
                            //var errored = configRequests.Where(x => x.Status == "E").ToList();
                            //if (errored.Count > 0)
                            //{
                            //    _inferenceEngineDBContext.IERequestQueueRepository.UpdateAutoTrainRequest(
                            //        autoReTrainRequest.RequestId,
                            //        autoReTrainRequest.CorrelationId,
                            //        "AutoReTrain",
                            //        "E",
                            //        "Error in Generate Inferences",
                            //        "100");
                            //}
                            //else
                            //{
                            //    _inferenceEngineDBContext.IERequestQueueRepository.UpdateAutoTrainRequest(
                            //       autoReTrainRequest.RequestId,
                            //       autoReTrainRequest.CorrelationId,
                            //       "AutoReTrain",
                            //       "C",
                            //       "Request completed",
                            //       "100");
                            //}
                            var success = configRequests.Where(x => x.Status == "C").ToList();
                            if (success.Count > 0)
                            {
                                _inferenceEngineDBContext.IERequestQueueRepository.UpdateAutoTrainRequest(
                                  autoReTrainRequest.RequestId,
                                  autoReTrainRequest.CorrelationId,
                                  "AutoReTrain",
                                  "C",
                                  "Request completed",
                                  "100");
                            }
                            else
                            {
                                _inferenceEngineDBContext.IERequestQueueRepository.UpdateAutoTrainRequest(
                                   autoReTrainRequest.RequestId,
                                   autoReTrainRequest.CorrelationId,
                                   "AutoReTrain",
                                   "E",
                                   "Error in Generate Inferences",
                                   "100");

                            }
                        }
                    }
                }
            }
            else
            {
                _inferenceEngineDBContext.IERequestQueueRepository.UpdateAutoTrainRequest(
                                   autoReTrainRequest.RequestId,
                                   autoReTrainRequest.CorrelationId,
                                   "AutoReTrain",
                                   "C",
                                   "Request completed",
                                   "100");
            }
        }










        public List<IESavedConfig> GetIEConfigs(string correlationId)
        {
            return _inferenceEngineDBContext.InferenceConfigRepository.GetAllIEConfigs(correlationId, false);
        }

        public List<IESavedConfig> GetIEConfigsIncludeAutoGen(string correlationId)
        {
            return _inferenceEngineDBContext.InferenceConfigRepository.GetAllIEConfigs(correlationId, true);
        }


        public IEREsponse TriggerFeatureCombination(string correlationId, string userId, string inferenceConfigId, dynamic dynamicColumns, bool isNewRequest)
        {
            IEREsponse iEREsponse = new IEREsponse();
            IERequestQueue ieRequest = new IERequestQueue();
            IERequestQueue requestQueue = new IERequestQueue();
            IEFeatureCombination featureCombination = new IEFeatureCombination();
            var selectedMetric = string.Empty;
            var selectedDate = string.Empty;
            dynamic selectedData = dynamicColumns;
            dynamic selectedFilter = null;
            foreach (var item in JObject.Parse(dynamicColumns.ToString()))
            {
                JProperty j = item as JProperty;
                if (j.Name == "Metric")
                {
                    selectedMetric = j.Value.ToString();
                }
                if (j.Name == "date")
                {
                    selectedDate = j.Value.ToString();
                }
                if (j.Name == "FilterValues")
                {
                    var values = JsonConvert.DeserializeObject<object>(selectedData.ToString());
                    foreach (var key in new string[] { "Metric", "date" })
                    {

                        values.Remove(key);
                    }
                    selectedFilter = ((values["FilterValues"].ToString(Formatting.None)));
                }
            }
            List<IESavedConfig> iESavedConfigs = _inferenceEngineDBContext.InferenceConfigRepository.GetAllIEConfigs(correlationId, false);
            foreach (var item in iESavedConfigs)
            {
                if (item.DateColumn == selectedDate && item.MetricColumn == selectedMetric && (string.IsNullOrEmpty(inferenceConfigId) || item.InferenceConfigId != inferenceConfigId))
                {
                    throw new Exception("Configuration already created with selected Metric and Date column. Please select a different combination");
                }
            }
            var data = _inferenceEngineDBContext.InferenceConfigRepository.GetFeatureCombination(correlationId, selectedMetric, selectedDate);
            if (data != null && data.FeatureCombinations != null && data.Features.Count > 0)
            {
                requestQueue = _inferenceEngineDBContext.IERequestQueueRepository.GetRequestQueuebyequestId(correlationId, "GetFeatures", data.RequestId);
                if (requestQueue.Status == "C" && isNewRequest)
                {
                    var isDelete = _inferenceEngineDBContext.IERequestQueueRepository.DeleteQueueRequestById(correlationId, "GetFeatures", requestQueue.RequestId);
                    var isFeatureDelte = _inferenceEngineDBContext.InferenceConfigRepository.DeleteFeatureCombination(correlationId, requestQueue.RequestId);
                    if (isDelete && isFeatureDelte)
                    {
                        ieRequest.CorrelationId = correlationId;
                        ieRequest.RequestId = Guid.NewGuid().ToString();
                        ieRequest.RequestStatus = CONSTANTS.New;
                        ieRequest.pageInfo = "GetFeatures";
                        ieRequest.ParamArgs = "{}";
                        ieRequest.Function = "GetFeatures";
                        ieRequest.CreatedBy = userId;
                        ieRequest.CreatedOn = DateTime.Now;
                        ieRequest.ModifiedBy = userId;
                        ieRequest.ModifiedOn = DateTime.Now;
                        ieRequest.Status = "N";
                        ieRequest.ParamArgs = dynamicColumns.ToString(Formatting.None);
                        _inferenceEngineDBContext.IERequestQueueRepository.IEInsertRequests(ieRequest);


                        featureCombination.CorrelationId = correlationId;
                        featureCombination.RequestId = ieRequest.RequestId;
                        featureCombination.MetricColumn = selectedMetric;
                        featureCombination.DateColumn = selectedDate;
                        featureCombination.FilterValues = selectedFilter;
                        _inferenceEngineDBContext.InferenceConfigRepository.InsertFeatureCombination(featureCombination);
                        requestQueue = _inferenceEngineDBContext.IERequestQueueRepository.GetRequestQueuebyequestId(correlationId, ieRequest.pageInfo, ieRequest.RequestId);

                    }
                }
            }
            else if (data == null)
            {
                ieRequest.CorrelationId = correlationId;
                ieRequest.RequestId = Guid.NewGuid().ToString();
                ieRequest.RequestStatus = CONSTANTS.New;
                ieRequest.pageInfo = "GetFeatures";
                ieRequest.ParamArgs = "{}";
                ieRequest.Function = "GetFeatures";
                ieRequest.CreatedBy = userId;
                ieRequest.CreatedOn = DateTime.Now;
                ieRequest.ModifiedBy = userId;
                ieRequest.ModifiedOn = DateTime.Now;
                ieRequest.Status = "N";
                ieRequest.ParamArgs = dynamicColumns.ToString().Replace(CONSTANTS.r_n, string.Empty);
                _inferenceEngineDBContext.IERequestQueueRepository.IEInsertRequests(ieRequest);


                featureCombination.CorrelationId = correlationId;
                featureCombination.RequestId = ieRequest.RequestId;
                featureCombination.MetricColumn = selectedMetric;
                featureCombination.DateColumn = selectedDate;
                featureCombination.FilterValues = selectedFilter;
                _inferenceEngineDBContext.InferenceConfigRepository.InsertFeatureCombination(featureCombination);
                requestQueue = _inferenceEngineDBContext.IERequestQueueRepository.GetRequestQueuebyequestId(correlationId, ieRequest.pageInfo, ieRequest.RequestId);

            }
            else if (data.FeatureCombinations == null || data.Features.Count == 0)
            {
                requestQueue = _inferenceEngineDBContext.IERequestQueueRepository.GetRequestQueuebyequestId(correlationId, "GetFeatures", data.RequestId);
                if (requestQueue.Status == "E" && isNewRequest)
                {
                    var isDelete = _inferenceEngineDBContext.IERequestQueueRepository.DeleteQueueRequestById(correlationId, "GetFeatures", requestQueue.RequestId);
                    var isFeatureDelte = _inferenceEngineDBContext.InferenceConfigRepository.DeleteFeatureCombination(correlationId, requestQueue.RequestId);
                    if (isDelete && isFeatureDelte)
                    {
                        ieRequest.CorrelationId = correlationId;
                        ieRequest.RequestId = Guid.NewGuid().ToString();
                        ieRequest.RequestStatus = CONSTANTS.New;
                        ieRequest.pageInfo = "GetFeatures";
                        ieRequest.ParamArgs = "{}";
                        ieRequest.Function = "GetFeatures";
                        ieRequest.CreatedBy = userId;
                        ieRequest.CreatedOn = DateTime.Now;
                        ieRequest.ModifiedBy = userId;
                        ieRequest.ModifiedOn = DateTime.Now;
                        ieRequest.Status = "N";
                        ieRequest.ParamArgs = dynamicColumns.ToString().Replace(CONSTANTS.r_n, string.Empty);
                        _inferenceEngineDBContext.IERequestQueueRepository.IEInsertRequests(ieRequest);


                        featureCombination.CorrelationId = correlationId;
                        featureCombination.RequestId = ieRequest.RequestId;
                        featureCombination.MetricColumn = selectedMetric;
                        featureCombination.DateColumn = selectedDate;
                        featureCombination.FilterValues = selectedFilter;
                        _inferenceEngineDBContext.InferenceConfigRepository.InsertFeatureCombination(featureCombination);
                        requestQueue = _inferenceEngineDBContext.IERequestQueueRepository.GetRequestQueuebyequestId(correlationId, ieRequest.pageInfo, ieRequest.RequestId);

                    }
                }

            }
            iEREsponse.CorrelationId = requestQueue.CorrelationId;
            iEREsponse.Status = requestQueue.Status;
            iEREsponse.Message = requestQueue.Message;
            iEREsponse.Progress = requestQueue.Progress;
            iEREsponse.RequestId = requestQueue.RequestId;
            iEREsponse.MetricColumn = selectedMetric;
            iEREsponse.DateColumn = selectedDate;
            return iEREsponse;

        }


        public string SendIEPublishNofication(string applicationId, string correlationId, string operation, string status, string message)
        {
            try
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(InferenceAutoTrainService), "SendIEPublishNofication OPERATION-" + operation, "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), applicationId, string.Empty, string.Empty, string.Empty);
                var ieModel = _inferenceEngineDBContext.IEModelRepository.GetIEModel(correlationId);
                if (ieModel != null)
                {
                    IEAppNotificationLog appNotificationLog = new IEAppNotificationLog();
                    appNotificationLog.ClientUId = ieModel.ClientUId;
                    appNotificationLog.DeliveryConstructUId = ieModel.DeliveryConstructUId;
                    appNotificationLog.CorrelationId = correlationId;
                    appNotificationLog.UseCaseId = ieModel.UseCaseId;
                    appNotificationLog.CreatedDateTime = ieModel.CreatedOn;
                    appNotificationLog.Entity = ieModel.Entity;
                    appNotificationLog.UseCaseName = string.Empty;
                    appNotificationLog.UserId = ieModel.CreatedBy;
                    appNotificationLog.FunctionalArea = ieModel.FunctionalArea;
                    appNotificationLog.Status = status;
                    appNotificationLog.Message = message;


                    appNotificationLog.ApplicationId = applicationId;
                    appNotificationLog.OperationType = operation;

                    appNotificationLog.NotificationEventType = "Inferences";
                    string host = appSettings.IngrainAPIUrl;
                    string IEIngrainCallbacklink = CONSTANTS.IERawRespCallBackURL;
                    if (IEGenricVDSUsecases.Contains(appNotificationLog.UseCaseId))// for generic VDS flow in FDS and PAM
                        IEIngrainCallbacklink = CONSTANTS.IEGenericRespCallBackURL;
                    string apiPath = String.Format(IEIngrainCallbacklink, correlationId, applicationId, null);
                    appNotificationLog.CallBackLink = host + apiPath;
                    this.SendAppNotification(appNotificationLog);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(InferenceAutoTrainService), "SendIEPublishNofication", "END", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), applicationId, string.Empty, string.Empty, string.Empty);
                    return "Success";
                }
                return "Model Doesn't exist";
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InferenceAutoTrainService), nameof(SendIEPublishNofication), ex.Message, ex, applicationId, string.Empty, string.Empty, string.Empty);
                return ex.Message;
            }
        }
        public void SendAppNotification(IEAppNotificationLog appNotificationLog)
        {
            appNotificationLog.RequestId = Guid.NewGuid().ToString();
            appNotificationLog.CreatedOn = DateTime.UtcNow.ToString();
            //appNotificationLog.ModifiedOn = DateTime.UtcNow;
            appNotificationLog.RetryCount = 0;
            appNotificationLog.IsNotified = false;

            var app = _inferenceEngineDBContext.IEAppIngerationRepository.GetAppDetailsOnId(appNotificationLog.ApplicationId);

            if (app != null)
            {
                appNotificationLog.Environment = app.Environment;
                if (app.Environment == "PAD")
                {
                    Uri apiUri = new Uri(appSettings.myWizardAPIUrl);
                    string host = apiUri.GetLeftPart(UriPartial.Authority);

                    appNotificationLog.AppNotificationUrl = host + "/" + app.AppNotificationUrl;
                }
                else
                {
                    if (IEGenricVDSUsecases.Contains(appNotificationLog.UseCaseId))// for generic VDS flow in FDS and PAM
                    {
                        Uri apiUri = new Uri(appSettings.VdsURL);
                        string host = apiUri.GetLeftPart(UriPartial.Authority);
                        appNotificationLog.AppNotificationUrl = host + Convert.ToString(appSettings.VDSIEGenericNotificationUrl);
                    }
                    else
                        appNotificationLog.AppNotificationUrl = app.AppNotificationUrl;
                }
            }
            else
            {
                throw new KeyNotFoundException("ApplicationId not found");
            }

            if (IEGenricVDSUsecases.Contains(appNotificationLog.UseCaseId))// for generic VDS flow in FDS and PAM
            {
                var useCase = _inferenceEngineDBContext.IEUseCaseRepository.GetUseCase(appNotificationLog.UseCaseId);
                if (useCase != null)
                {
                    appNotificationLog.UseCaseName = useCase.UseCaseName;
                    appNotificationLog.UseCaseDescription = useCase.UseCaseDescription;
                }
                if (appNotificationLog.Status == "C")
                    appNotificationLog.Progress = "100%";
                else 
                    appNotificationLog.Progress = "0%";
                appNotificationLog.UserId = appSettings.IsAESKeyVault ? CryptographyUtility.Decrypt(appNotificationLog.UserId) : AesProvider.Decrypt(appNotificationLog.UserId, appSettings.aesKey, appSettings.aesVector);
            }

            _inferenceEngineDBContext.IEAppIngerationRepository.InsertAppNotification(appNotificationLog);
        }
    }
}
