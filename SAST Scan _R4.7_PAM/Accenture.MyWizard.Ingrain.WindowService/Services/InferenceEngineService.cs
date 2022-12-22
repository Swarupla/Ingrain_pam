using Accenture.MyWizard.Shared.Helpers;
using MongoDB.Driver;
using WINSERVICEMODELS = Accenture.MyWizard.Ingrain.WindowService.Models;
using DATAACCESS = Accenture.MyWizard.Ingrain.DataAccess;
using Microsoft.Extensions.Configuration;
using Accenture.MyWizard.Ingrain.DataModels.InferenceEngineModel;
using System.Collections.Generic;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
using System;
using Newtonsoft.Json.Linq;
using AICOREMODELS = Accenture.MyWizard.Ingrain.DataModels.AICore;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using System.Threading;
using System.Linq;
using Accenture.MyWizard.Cryptography.EncryptionProviders;
using CryptographyHelper = Accenture.MyWizard.Cryptography;

namespace Accenture.MyWizard.Ingrain.WindowService.Services
{
    public class InferenceEngineService
    {
        private readonly WINSERVICEMODELS.AppSettings appSettings = null;
        // private IMongoDatabase _database;
        private DATAACCESS.IInferenceEngineDBContext databaseProvider;
        private WebHelper webHelper;
        private InferenceAutoTrainService _inferenceAutoTrainService;
        static CryptographyHelper.Utility.CryptographyUtility CryptographyUtility = null;



        private TokenService _tokenService;
        public InferenceEngineService()
        {
            appSettings = AppSettingsJson.GetAppSettings().GetSection("AppSettings").Get<WINSERVICEMODELS.AppSettings>();
            databaseProvider = new DATAACCESS.InferenceEngineDBContext(appSettings.IEConnectionString, appSettings.certificatePath, appSettings.certificatePassKey, appSettings.aesKey, appSettings.aesVector, appSettings.IsAESKeyVault);
            _tokenService = new TokenService();
            webHelper = new WebHelper();
            _inferenceAutoTrainService = new InferenceAutoTrainService();
            CryptographyUtility = new CryptographyHelper.Utility.CryptographyUtility();
        }


        public List<IERequestQueue> GetIERequests()
        {
            var requests = databaseProvider.IERequestQueueRepository.GetIERequests(appSettings.RequestBatchLimit);
            if (requests.Count > 0)
            {
                requests.RemoveAll(x => x.pageInfo == "AutoReTrain");
            }
            return requests;
        }

        //public void UpdateIERequestToError()
        //{
        //    try
        //    {
        //        List<IERequestQueue> aIServiceRequests = GetInCompleteIERequest();
        //        if (aIServiceRequests != null)
        //        {
        //            foreach (var request in aIServiceRequests)
        //            {
        //                DateTime modifiedDate = request.ModifiedOn;
        //                DateTime curTime = DateTime.UtcNow;
        //                int elapsedTime = (int)(curTime - modifiedDate).TotalMinutes;
        //                if (elapsedTime > 60)
        //                {
        //                    request.Status = "E";
        //                    request.Message = "Request is taking more time to complete";
        //                    databaseProvider.IERequestQueueRepository.UpdateIEServiceRequestStatus(request);
        //                }
        //            }
        //        }
        //    }

        //    catch (Exception ex)
        //    {
        //        LOGGING.LogManager.Logger.LogErrorMessage(typeof(InferenceEngineService), nameof(UpdateIERequestToError), ex.Message, ex);
        //    }

        //}

        //public List<IERequestQueue> GetInCompleteIERequest()
        //{
        //    return databaseProvider.IERequestQueueRepository.GetAllInCompleteIERequest();
        //}


        public IEReTrainTask GetReTrainTask()
        {
            return databaseProvider.IEAutoReTrainRepository.GetTaskDetails("IE AUTORETRAIN");
        }
        public void ReTrainIEModels()
        {
            try
            {
                var task = GetReTrainTask();
                if (task.ManualTrain)
                {
                    List<string> corrIds = task.CorrelationIds.Split(",").ToList();
                    foreach (var corrid in corrIds)
                    {
                        var model = databaseProvider.IEModelRepository.GetIEModel(corrid);
                        if (model != null)
                        {
                            TriggerReTrain(model);
                        }

                    }
                }
                else
                {
                    var ieModels = databaseProvider.IEModelRepository.GetReTrainIEModels();
                    var appDetails = databaseProvider.IEAppIngerationRepository.GetAllApps("PAD");
                    foreach (var model in ieModels)
                    {
                        if (model.ApplicationId != null)
                        {
                            if (model.ApplicationId != "ba58a983-99a8-4030-9d17-29b337b4dd36") // exclude for Release360
                            {
                                var app = appDetails.Where(x => x.ApplicationID == model.ApplicationId).FirstOrDefault();
                                DateTime curTime = DateTime.UtcNow;

                                int diff = (int)(curTime - model.ModifiedOn).TotalDays;
                                if (app != null)
                                {
                                    if (diff >= app.AutoTrainDays)
                                    {
                                        TriggerReTrain(model);
                                    }
                                }
                            }
                        }
                        else
                        {
                            DateTime curTime = DateTime.UtcNow;

                            int diff = (int)(curTime - model.ModifiedOn).TotalDays;

                            if (diff >= Convert.ToInt32(task.FrequencyInDays))
                            {
                                TriggerReTrain(model);
                            }
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InferenceEngineService), nameof(DecryptDevDBIE), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }

        }



        public void TriggerReTrain(IEModel model)
        {
            var newRequest = new IERequestQueue()
            {
                CorrelationId = model.CorrelationId,
                RequestId = Guid.NewGuid().ToString(),
                pageInfo = "AutoReTrain",
                Function = "AutoReTrain",
                Status = "N",
                ClientUId = model.ClientUId,
                DeliveryConstructUId = model.DeliveryConstructUId,
                ParamArgs = null,
                ApplicationId = model.ApplicationId,
                UseCaseId = model.UseCaseId,
                CreatedBy = model.CreatedBy,
                CreatedOn = DateTime.UtcNow,
                ModifiedBy = model.CreatedBy,
                ModifiedOn = DateTime.UtcNow
            };

            databaseProvider.IERequestQueueRepository.IEInsertRequests(newRequest);

            Thread retrainThread = new Thread(() => _inferenceAutoTrainService.ModelAutoReTrain(newRequest));
            retrainThread.Start();

            bool flag = true;

            while (flag)
            {
                Thread.Sleep(5000);
                var request = databaseProvider.IERequestQueueRepository.GetRequestQueuebyequestId(
                    newRequest.CorrelationId,
                    newRequest.pageInfo,
                    newRequest.RequestId);

                if (request.Status == "C" || request.Status == "E")
                {
                    flag = false;
                }
            }
        }

        public void DecryptDevDBIE()
        {
            try
            {
                var usecaseList = databaseProvider.IEUseCaseRepository.GetAllUseCases();
                foreach (var item in usecaseList)
                {
                    if (item.isSavedConfigEncrypted == "yes")
                    {
                        List<IESavedConfig> resp = new List<IESavedConfig>();
                        var savedConfig = databaseProvider.InferenceConfigRepository.GetDevIEConfigs(item.CorrelationId, false);
                        foreach (var rec in savedConfig)
                        {

                            if (rec != null)
                            {
                                var encryption = databaseProvider.InferenceConfigRepository.GetIEModelEncryption(item.CorrelationId);
                                if (encryption.DBEncryptionRequired && rec.InferenceConfigType == "MeasureAnalysis")
                                {
                                    if (rec.FilterValues != null)
                                    {
                                        rec.FilterValues = AesProvider.Decrypt(rec.FilterValues, appSettings.DevtestAesKey, appSettings.DevtestAesVector);
                                    }

                                }
                                resp.Add(rec);

                            }
                        }

                        foreach (var config in resp)
                        {
                            if (config != null)
                            {
                                var encryption = databaseProvider.InferenceConfigRepository.GetIEModelEncryption(item.CorrelationId);
                                if (encryption.DBEncryptionRequired && config.InferenceConfigType == "MeasureAnalysis")
                                {
                                    if (config.FilterValues != null)
                                    {
                                        if (appSettings.IsAESKeyVault)
                                        {
                                            config.FilterValues = CryptographyUtility.Encrypt(config.FilterValues);
                                        }
                                        else
                                        {
                                            config.FilterValues = AesProvider.Encrypt(config.FilterValues, appSettings.aesKey, appSettings.aesVector);
                                        }
                                        databaseProvider.InferenceConfigRepository.UpdateEncrySavedConfig(config);
                                    }

                                }
                            }

                        }
                        databaseProvider.IEUseCaseRepository.UpdateUsecaseFlag(item);
                    }
                }
            }
            catch (Exception ex)
            {

                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InferenceEngineService), nameof(DecryptDevDBIE), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }

        }

        public void InvokeIERequest(IERequestQueue aIServiceRequestStatus)
        {
            try
            {
                aIServiceRequestStatus.Status = "O";
                aIServiceRequestStatus.Message = "Request Initiated";
                aIServiceRequestStatus.RequestStatus = "Occupied";
                databaseProvider.IERequestQueueRepository.UpdateIEServiceRequestStatus(aIServiceRequestStatus);
                string apiPath = string.Empty;
                string baseUrl = appSettings.IEPythonURL;
                JObject payload = new JObject();
                if (aIServiceRequestStatus.pageInfo == "AutoTrain")
                {
                    Thread trainingThread = new Thread(() => _inferenceAutoTrainService.ModelAutoTrain(aIServiceRequestStatus));
                    trainingThread.Start();
                }
                else
                {
                    if (aIServiceRequestStatus.pageInfo == "IngestData")
                    {
                        apiPath = CONSTANTS.IEIngestDataPy;
                    }
                    else
                    {
                        apiPath = CONSTANTS.IENarrativePy;
                        string inferenceConfigId = aIServiceRequestStatus.InferenceConfigId;
                        if (string.IsNullOrEmpty(aIServiceRequestStatus.InferenceConfigId))
                        {
                            inferenceConfigId = "NA";
                        }

                        payload["InferenceConfigId"] = inferenceConfigId;

                    }
                    payload["CorrelationId"] = aIServiceRequestStatus.CorrelationId;
                    payload["RequestId"] = aIServiceRequestStatus.RequestId;
                    payload["UserId"] = aIServiceRequestStatus.CreatedBy;
                    payload["PageInfo"] = aIServiceRequestStatus.pageInfo;
                    var message = RoutePOSTRequest(string.Empty, new Uri(baseUrl), apiPath,
                        payload, false);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(InferenceEngineService), nameof(InvokeIERequest), "AI Request triggered for apiPath - " + baseUrl + apiPath + "parameter -" + payload, string.IsNullOrEmpty(aIServiceRequestStatus.CorrelationId) ? default(Guid) : new Guid(aIServiceRequestStatus.CorrelationId), aIServiceRequestStatus.ApplicationId, string.Empty, aIServiceRequestStatus.ClientUId, aIServiceRequestStatus.DeliveryConstructUId);
                    if (!message.IsSuccess)
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(InferenceEngineService), nameof(InvokeIERequest), "Python Response - " + message.Message, string.IsNullOrEmpty(aIServiceRequestStatus.CorrelationId) ? Guid.Empty : new Guid(aIServiceRequestStatus.CorrelationId), aIServiceRequestStatus.ApplicationId, "", aIServiceRequestStatus.ClientUId, aIServiceRequestStatus.DeliveryConstructUId);
                        aIServiceRequestStatus.Status = "E";
                        aIServiceRequestStatus.Message = message.Message;
                        databaseProvider.IERequestQueueRepository.UpdateIEServiceRequestStatus(aIServiceRequestStatus);
                        if (aIServiceRequestStatus.pageInfo == "IngestData")
                        {
                            databaseProvider.IEModelRepository.UpdateIEModel(aIServiceRequestStatus.CorrelationId, aIServiceRequestStatus.Status, aIServiceRequestStatus.Message, "0");

                        }
                    }
                }



            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InferenceEngineService), nameof(InvokeIERequest), ex.Message, ex, aIServiceRequestStatus.ApplicationId, string.Empty, aIServiceRequestStatus.ClientUId, aIServiceRequestStatus.DeliveryConstructUId);
            }


        }

        public AICOREMODELS.MethodReturn<object> RoutePOSTRequest(string token, Uri baseUrl, string apiPath, JObject requestPayload, bool isReturnArray)
        {
            AICOREMODELS.MethodReturn<object> returnValue = new AICOREMODELS.MethodReturn<object>();
            if (appSettings.Environment == "PAM")
            {
                token = _tokenService.GenerateVDSPAMToken(CONSTANTS.VDSApplicationID_PAM);
            }
            else
            {
                token = _tokenService.GeneratePADToken();
            }
            try
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(InferenceEngineService), nameof(RoutePOSTRequest), "IE Python trigger start- ", default(Guid), string.Empty, string.Empty, string.Empty, string.Empty);

                StringContent content = new StringContent(requestPayload.ToString(), Encoding.UTF8, "application/json");
                HttpResponseMessage message = webHelper.InvokePOSTRequest(token, baseUrl.ToString(), apiPath, content);
                if (message != null && message.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(InferenceEngineService), nameof(RoutePOSTRequest), "IE Python Success- ", default(Guid), string.Empty, string.Empty, string.Empty, string.Empty);
                    if (isReturnArray)

                        returnValue.ReturnValue = JsonConvert.DeserializeObject<List<JObject>>(message.Content.ReadAsStringAsync().Result);
                    else
                        returnValue.ReturnValue = JsonConvert.DeserializeObject<JObject>(message.Content.ReadAsStringAsync().Result);
                    returnValue.IsSuccess = true;
                }
                else
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(InferenceEngineService), nameof(RoutePOSTRequest), "IE Python Failure- ", default(Guid), string.Empty, string.Empty, string.Empty, string.Empty);
                    returnValue.IsSuccess = false;
                    returnValue.Message = message.ReasonPhrase;
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InferenceEngineService), nameof(RoutePOSTRequest), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                returnValue.IsSuccess = false;
                returnValue.Message = ex.Message;

            }
            return returnValue;
        }



    }
}
