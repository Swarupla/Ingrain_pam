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
using Accenture.MyWizard.Ingrain.DataModels.Models;
using System.Diagnostics;
using Accenture.MyWizard.Ingrain.WindowService.Models;
using Accenture.MyWizard.Ingrain.WindowService.Services;
using Accenture.MyWizard.SelfServiceAI.WindowService.Models;
using Accenture.MyWizard.SelfServiceAI.WindowService.Service;
using Microsoft.Extensions.Hosting;
using MongoDB.Bson;
using RestSharp;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Timers;
using Accenture.MyWizard.Ingrain.DataModels.AICore;
using MongoDB.Bson.Serialization;
using AIModels = Accenture.MyWizard.Ingrain.DataModels.AICore;
using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;

namespace Accenture.MyWizard.Ingrain.WindowService.Services
{
    public class AnomalyDetectionWService
    {
        private DatabaseProvider databaseProviderAD;
        private IMongoDatabase _databaseAD;
        private string servicename = "Anomaly";
        private readonly WINSERVICEMODELS.AppSettings appSettings = null;
        private WebHelper webHelper;
        private InferenceAutoTrainService _inferenceAutoTrainService;
        static CryptographyHelper.Utility.CryptographyUtility CryptographyUtility = null;
        private TokenService _tokenService;
        private IMongoCollection<IngrainRequestQueue> _Requestcollection;
        private TokenService _TokenService = null;
        public AnomalyDetectionWService()
        {
            appSettings = AppSettingsJson.GetAppSettings().GetSection("AppSettings").Get<WINSERVICEMODELS.AppSettings>();
            //Mongo connection
            databaseProviderAD = new DatabaseProvider();
            var dataBaseName = MongoUrl.Create(appSettings.AnomalyDetectionCS).DatabaseName;
            MongoClient mongoClientAD = databaseProviderAD.GetDatabaseConnection(servicename);
            _databaseAD = mongoClientAD.GetDatabase(dataBaseName);

            _tokenService = new TokenService();
            webHelper = new WebHelper();
            _inferenceAutoTrainService = new InferenceAutoTrainService();
            CryptographyUtility = new CryptographyHelper.Utility.CryptographyUtility();
            _TokenService = new TokenService();
            _Requestcollection = _databaseAD.GetCollection<IngrainRequestQueue>("SSAI_IngrainRequests");
        }
        public List<IngrainRequestQueue> GetQueueRequests()
        {
            var filterBuilder = Builders<IngrainRequestQueue>.Filter;
            var filter = filterBuilder.Where(x => x.RequestStatus == CONSTANTS.New) | filterBuilder.Where(x => x.RequestStatus == "Queued");
            var projection = Builders<IngrainRequestQueue>.Projection.Exclude("_id");
            var requests = _Requestcollection.Find(filter).Project<IngrainRequestQueue>(projection).Limit(appSettings.RequestBatchLimit).ToList();
            return requests;
        }
        public void InvokeADRequest(IngrainRequestQueue ingrainRequestQueue)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionWService), "InvokeADRequest Start", "RequestID-" + ingrainRequestQueue.RequestId + " Cor id-", string.IsNullOrEmpty(ingrainRequestQueue.CorrelationId) ? default(Guid) : new Guid(ingrainRequestQueue.CorrelationId), ingrainRequestQueue.AppID, string.Empty, ingrainRequestQueue.ClientID, ingrainRequestQueue.DeliveryconstructId);
            var filterBuilder = Builders<IngrainRequestQueue>.Filter;
            try
            {
                if (!string.IsNullOrEmpty(ingrainRequestQueue.CorrelationId))
                {
                    var filterCorrelation = filterBuilder.Eq("CorrelationId", ingrainRequestQueue.CorrelationId) & filterBuilder.Eq("RequestId", ingrainRequestQueue.RequestId) & (filterBuilder.Eq("RequestStatus", "New") | filterBuilder.Eq("RequestStatus", "Queued"));
                    var update = Builders<IngrainRequestQueue>.Update.Set("RequestStatus", "Occupied").Set("PyTriggerTime", DateTime.Now.ToString()).Set("Message", "Request Initiated");
                    var isUpdated = _Requestcollection.UpdateOne(filterCorrelation, update);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionWService), "InvokeADRequest Start", "MODIFIEDCOUNT--" + isUpdated.ModifiedCount + "-RequestID-" + ingrainRequestQueue.RequestId + " Cor id-", string.IsNullOrEmpty(ingrainRequestQueue.CorrelationId) ? default(Guid) : new Guid(ingrainRequestQueue.CorrelationId), ingrainRequestQueue.AppID, string.Empty, ingrainRequestQueue.ClientID, ingrainRequestQueue.DeliveryconstructId);
                    if (isUpdated.ModifiedCount > 0)
                    {
                        switch (ingrainRequestQueue.Function)
                        {
                            case "FileUpload":
                                CallPythonForService(ingrainRequestQueue, CONSTANTS.Anomaly_IngestData);
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionWorker), "InvokeADRequest", "InvokeADRequest-FileUpload before Python call" + "-RequestID-" + ingrainRequestQueue.RequestId + " Cor id-", string.IsNullOrEmpty(ingrainRequestQueue.CorrelationId) ? default(Guid) : new Guid(ingrainRequestQueue.CorrelationId), ingrainRequestQueue.AppID, string.Empty, ingrainRequestQueue.ClientID, ingrainRequestQueue.DeliveryconstructId);
                                break;
                            case "DataTransform":
                                CallPythonForService(ingrainRequestQueue, CONSTANTS.Anomaly_DataTransform);
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionWorker), "InvokeADRequest", "InvokeADRequest-DataTransform before Python call" + "-RequestID-" + ingrainRequestQueue.RequestId + " Cor id-", string.IsNullOrEmpty(ingrainRequestQueue.CorrelationId) ? default(Guid) : new Guid(ingrainRequestQueue.CorrelationId), ingrainRequestQueue.AppID, string.Empty, ingrainRequestQueue.ClientID, ingrainRequestQueue.DeliveryconstructId);
                                break;
                            case "RecommendedAI":
                                CallPythonForService(ingrainRequestQueue, CONSTANTS.Anomaly_ModelTraining);
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionWorker), "InvokeADRequest", "InvokeADRequest-RecommendedAI before Python call" + "-RequestID-" + ingrainRequestQueue.RequestId + " Cor id-", string.IsNullOrEmpty(ingrainRequestQueue.CorrelationId) ? default(Guid) : new Guid(ingrainRequestQueue.CorrelationId), ingrainRequestQueue.AppID, string.Empty, ingrainRequestQueue.ClientID, ingrainRequestQueue.DeliveryconstructId);
                                break;
                        }
                    }
                    else
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionWService), "InvokeADRequest End without any processing", "-RequestID-" + ingrainRequestQueue.RequestId + " Cor id-", string.IsNullOrEmpty(ingrainRequestQueue.CorrelationId) ? default(Guid) : new Guid(ingrainRequestQueue.CorrelationId), ingrainRequestQueue.AppID, string.Empty, ingrainRequestQueue.ClientID, ingrainRequestQueue.DeliveryconstructId);
                    }
                }
                else
                {
                    LOGGING.LogManager.Logger.LogErrorMessage(typeof(AnomalyDetectionWService), nameof(InvokeADRequest), "ERROR: CorrelationId is null", null, "RequestId: " + Convert.ToString(ingrainRequestQueue.RequestId), string.Empty, string.Empty, string.Empty);
                    UpdateErrorInRequestQueue(ingrainRequestQueue.CorrelationId, ingrainRequestQueue.RequestId, "Required paramter CorrelationId is null");
                }
            }
            catch (Exception ex)
            {
                UpdateErrorInRequestQueue(ingrainRequestQueue.CorrelationId, ingrainRequestQueue.RequestId, ex.Message);
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AnomalyDetectionWorker), nameof(InvokeADRequest), ex.StackTrace + "--" + ex.Message, ex, ingrainRequestQueue.AppID, string.Empty, ingrainRequestQueue.ClientID, ingrainRequestQueue.DeliveryconstructId);
            }
        }
        private void UpdateErrorInRequestQueue(string CorrelationId, string requestId, string ErrorMsg)
        {
            var filterBuilder = Builders<IngrainRequestQueue>.Filter;
            var filterCorrelation = filterBuilder.Eq("RequestId", requestId);
            var update = Builders<IngrainRequestQueue>.Update.Set("RequestStatus", "Error").Set(x => x.Message, ErrorMsg).Set(x => x.ModifiedOn, DateTime.Now.ToString()).Set(x => x.Status, "E");
            var isUpdated = _Requestcollection.UpdateOne(filterCorrelation, update);

            //var deployModelcollection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
            //var filterdeployModel = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, CorrelationId);
            //var deployModelupdate = Builders<BsonDocument>.Update.Set("RequestStatus", "Error").Set(x => x.Message, ErrorMsg).Set(x => x.ModifiedOn, DateTime.Now.ToString()).Set(x => x.Status, "E");
            //var isUpdateddeployModel = deployModelcollection.UpdateOne(filterdeployModel, deployModelupdate);
        }
        public void CallPythonForService(IngrainRequestQueue ingrainRequest, string apiPath)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionWService), nameof(CallPythonForService), "CallPythonForService Triggered", string.Empty, string.Empty, string.Empty, string.Empty);
            var serviceResponse = new MethodReturn<object>();
            Uri apiUri = new Uri(appSettings.IngrainAPIUrl);
            string baseUrl = apiUri.GetLeftPart(UriPartial.Authority);
            string token = _TokenService.GenerateToken();

            FlaskForServiceDTO FlaskForServiceobj = new FlaskForServiceDTO();
            FlaskForServiceobj.correlationId = ingrainRequest.CorrelationId;
            FlaskForServiceobj.requestId = ingrainRequest.RequestId;
            FlaskForServiceobj.pageInfo = ingrainRequest.pageInfo;
            FlaskForServiceobj.userId = ingrainRequest.CreatedByUser;

            JObject payload = JObject.Parse(JsonConvert.SerializeObject(FlaskForServiceobj));
            try
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionWService), nameof(CallPythonForService), "before Python Call" + ingrainRequest.RequestId, string.Empty, string.Empty, string.Empty, string.Empty);
                serviceResponse = RoutePOSTRequest(token, new Uri(baseUrl), apiPath, payload);
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionWService), nameof(CallPythonForService), "token: " + token + " ,pageInfo: " + ingrainRequest.pageInfo, string.Empty, string.Empty, string.Empty, string.Empty);
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionWService), nameof(CallPythonForService), "baseUrl: " + baseUrl.ToString() + "apiPath: " + apiPath.ToString() + "payload: " + payload.ToString() + " ,pageInfo: " + ingrainRequest.pageInfo + " RequestId: " + ingrainRequest.RequestId, string.Empty, string.Empty, string.Empty, string.Empty);
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionWService), nameof(CallPythonForService), "Python - call END. serviceResponse: " + serviceResponse + ",pageInfo: " + ingrainRequest.pageInfo + " RequestId: " + ingrainRequest.RequestId, string.Empty, string.Empty, string.Empty, string.Empty);
                if (!serviceResponse.IsSuccess || (serviceResponse.IsSuccess && serviceResponse.Message == "Invoke Ingestdata Failed" ))
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionWService), nameof(CallPythonForService), "Python - Not SUCCESS--END. serviceResponse: " + serviceResponse + ",pageInfo: " + ingrainRequest.pageInfo + " RequestId: " + ingrainRequest.RequestId, string.Empty, string.Empty, string.Empty, string.Empty);
                    UpdateErrorInRequestQueue(FlaskForServiceobj.correlationId, FlaskForServiceobj.requestId, "Python Error: " + serviceResponse.Message);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AnomalyDetectionWService), nameof(CallPythonForService), "Exception - CallPythonForService Call : " + ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
        }
        public MethodReturn<object> RoutePOSTRequest(string token, Uri baseUrl, string apiPath, JObject requestPayload)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionWService), nameof(RoutePOSTRequest), "RoutePOSTRequest - Called", default(Guid), string.Empty, string.Empty, string.Empty, string.Empty);
            MethodReturn<object> returnValue = new MethodReturn<object>();
            try
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionWService), nameof(RoutePOSTRequest), "requestPayload : " + Convert.ToString(requestPayload), string.Empty, string.Empty, string.Empty, string.Empty);
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionWService), nameof(RoutePOSTRequest), "token : " + token + " apiPath : " + apiPath + " baseUrl : " + baseUrl, string.Empty, string.Empty, string.Empty, string.Empty);
                StringContent content = new StringContent(requestPayload.ToString(), Encoding.UTF8, "application/json");
                HttpResponseMessage message = webHelper.InvokePOSTRequest(token, baseUrl.ToString(), apiPath, content);
                if (message != null && message.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    returnValue.IsSuccess = true;
                    returnValue.Message = message.Content.ReadAsStringAsync().Result;
                }
                else
                {
                    returnValue.IsSuccess = false;
                    returnValue.Message = message.ReasonPhrase + "_" + message.Content.ReadAsStringAsync().Result;
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionWService), nameof(RoutePOSTRequest), ex.StackTrace, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty);
                returnValue.IsSuccess = false;
                returnValue.Message = ex.Message;
            }
            return returnValue;
        }
    }
}
