using System;
using System.Collections.Generic;
using System.Text;
using WINSERVICEMODELS = Accenture.MyWizard.Ingrain.WindowService.Models;
using DATAACCESS = Accenture.MyWizard.Ingrain.WindowService;
using AICOREMODELS = Accenture.MyWizard.Ingrain.DataModels.AICore;
using Accenture.MyWizard.Ingrain.DataModels.InferenceEngineModel;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
using Accenture.MyWizard.Shared.Helpers;
using MongoDB.Driver;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using MongoDB.Bson;

namespace Accenture.MyWizard.Ingrain.WindowService.Services
{
    public class AIModelsService
    {
        private readonly WINSERVICEMODELS.AppSettings appSettings = null;
        private IMongoDatabase _database;
        private DATAACCESS.DatabaseProvider databaseProvider;
        private WebHelper webHelper;

        private TokenService _tokenService;
        private QueueManagerService _queueManagerService;

        public AIModelsService()
        {
            appSettings = AppSettingsJson.GetAppSettings().GetSection("AppSettings").Get<WINSERVICEMODELS.AppSettings>();
            databaseProvider = new DATAACCESS.DatabaseProvider();
            var dataBaseName = MongoUrl.Create(appSettings.connectionString).DatabaseName;
            MongoClient mongoClient = databaseProvider.GetDatabaseConnection();
            _database = mongoClient.GetDatabase(dataBaseName);
            _tokenService = new TokenService();
            webHelper = new WebHelper();
            _queueManagerService = new QueueManagerService();
        }



        public List<AICOREMODELS.AIServiceRequestStatus> GetAIServiceRequests()
        {
            var requestsCollection = _database.GetCollection<BsonDocument>(CONSTANTS.AIServiceRequestStatus);
            var filter = Builders<BsonDocument>.Filter.Eq("Status", "N");
            var projection = Builders<BsonDocument>.Projection.Exclude("_id");
            var result = requestsCollection.Find(filter).Project<BsonDocument>(projection).Limit(appSettings.RequestBatchLimit).ToList();
            List<AICOREMODELS.AIServiceRequestStatus> aIServiceRequestStatuses = new List<AICOREMODELS.AIServiceRequestStatus>();
            if (result.Count > 0)
            {
                #region Temporary
                //For SPP Issue more records piled up. Total 58989 Ingrain request in queue 
                _queueManagerService.RequestBatchLimitInsert(result.Count, CONSTANTS.AIServiceRequestBatchLimitMonitor);
                #endregion

                AICOREMODELS.AIServiceRequestStatus aIServiceRequestStatus = new AICOREMODELS.AIServiceRequestStatus();
                for (int z = 0; z < result.Count; z++)
                {
                    aIServiceRequestStatus.CorrelationId = result[z]["CorrelationId"].ToString();
                    aIServiceRequestStatus.ServiceId = result[z]["ServiceId"].ToString();
                    aIServiceRequestStatus.UniId = result[z]["UniId"].ToString();
                    aIServiceRequestStatus.PageInfo = result[z]["PageInfo"].ToString();
                    aIServiceRequestStatus.ClientId = result[z]["ClientId"].ToString();
                    aIServiceRequestStatus.DeliveryconstructId = result[z]["DeliveryconstructId"].ToString();
                    aIServiceRequestStatus.Status = result[z]["Status"].ToString();
                    aIServiceRequestStatus.ModelName = result[z]["ModelName"].ToString();
                    aIServiceRequestStatus.RequestStatus = result[z]["RequestStatus"].ToString();
                    aIServiceRequestStatus.Message = result[z]["Message"].ToString();
                    aIServiceRequestStatus.Progress = result[z]["Progress"].ToString();

                    aIServiceRequestStatus.ColumnNames = new[] { result[z]["ColumnNames"].ToString() };
                    List<dynamic> selCol = new List<dynamic>();
                    selCol.Add(result[z]["SelectedColumnNames"]);
                    aIServiceRequestStatus.SelectedColumnNames = selCol;
                    aIServiceRequestStatus.SourceDetails = result[z]["SourceDetails"].ToString();
                    aIServiceRequestStatus.DataSource = result[z]["DataSource"].ToString();
                    aIServiceRequestStatus.CreatedByUser = result[z]["CreatedByUser"].ToString();
                    aIServiceRequestStatus.CreatedOn = result[z]["CreatedOn"].ToString();
                    aIServiceRequestStatus.ModifiedByUser = result[z]["ModifiedByUser"].ToString();
                    aIServiceRequestStatus.ModifiedOn = result[z]["ModifiedOn"].ToString();
                    JObject data = new JObject();
                    data = JObject.Parse(result[z].ToString());

                    if (data.ContainsKey("FilterAttribute"))
                    {
                        if (!string.IsNullOrEmpty(result[z]["FilterAttribute"].ToString()) && result[z]["FilterAttribute"].ToString() != "BsonNull")
                            aIServiceRequestStatus.FilterAttribute = (JObject)Newtonsoft.Json.JsonConvert.DeserializeObject(result[z]["FilterAttribute"].ToString());
                    }
                    if (data.ContainsKey("Language"))
                        aIServiceRequestStatus.Language = result[z]["Language"].ToString();
                    if (data.ContainsKey("ResponsecallbackUrl"))
                        aIServiceRequestStatus.ResponsecallbackUrl = result[z]["ResponsecallbackUrl"].ToString();
                    if (data.ContainsKey("UsecaseId"))
                        aIServiceRequestStatus.UsecaseId = result[z]["UsecaseId"].ToString();
                    if (data.ContainsKey("ApplicationId"))
                        aIServiceRequestStatus.ApplicationId = result[z]["ApplicationId"].ToString();
                    if (data.ContainsKey("Payload"))
                    {
                        if (!string.IsNullOrEmpty(result[z]["Payload"].ToString()) && result[z]["Payload"].ToString() != "BsonNull")
                            aIServiceRequestStatus.Payload = (JObject)Newtonsoft.Json.JsonConvert.DeserializeObject(result[z]["Payload"].ToString());
                    }
                    if (data.ContainsKey("baseUrl"))
                        aIServiceRequestStatus.baseUrl = result[z]["baseUrl"].ToString();
                    if (data.ContainsKey("apiPath"))
                        aIServiceRequestStatus.apiPath = result[z]["apiPath"].ToString();
                    if (data.ContainsKey("token"))
                        aIServiceRequestStatus.token = result[z]["token"].ToString();
                    aIServiceRequestStatuses.Add(aIServiceRequestStatus);
                }
            }
            return aIServiceRequestStatuses;


            //var requestsCollection = _database.GetCollection<AICOREMODELS.AIServiceRequestStatus>(CONSTANTS.AIServiceRequestStatus);
            //var filter = Builders<AICOREMODELS.AIServiceRequestStatus>.Filter.Where(x => x.Status == "N");
            //var projection = Builders<AICOREMODELS.AIServiceRequestStatus>.Projection.Exclude("_id");
            //var result = requestsCollection.Find(filter).Project<AICOREMODELS.AIServiceRequestStatus>(projection).ToList();
            //return requestsCollection.Find(filter).Project<AICOREMODELS.AIServiceRequestStatus>(projection).ToList();
        }

        public List<IERequestQueue> GetIERequests()
        {
            var requestsCollection = _database.GetCollection<IERequestQueue>(CONSTANTS.IE_RequestQueue);
            var filter = Builders<IERequestQueue>.Filter.Where(x => x.Status == "N");
            var projection = Builders<IERequestQueue>.Projection.Exclude("_id");
            return requestsCollection.Find(filter).Project<IERequestQueue>(projection).ToList();
        }

        public List<AICOREMODELS.AIServiceRequestStatus> GetInProgressAIServiceRequests()
        {
            var requestsCollection = _database.GetCollection<AICOREMODELS.AIServiceRequestStatus>(CONSTANTS.AIServiceRequestStatus);
            var filter = Builders<AICOREMODELS.AIServiceRequestStatus>.Filter.Where(x => x.Status == "P")
                         | Builders<AICOREMODELS.AIServiceRequestStatus>.Filter.Where(x => x.Status == "I");
            var projection = Builders<AICOREMODELS.AIServiceRequestStatus>.Projection.Exclude("_id");
            return requestsCollection.Find(filter).Project<AICOREMODELS.AIServiceRequestStatus>(projection).ToList();
        }

        public void ChangeAIRequestStatustoError()
        {
            List<AICOREMODELS.AIServiceRequestStatus> aIServiceRequests = GetInProgressAIServiceRequests();
            if (aIServiceRequests != null)
            {
                foreach (var request in aIServiceRequests)
                {
                    DateTime modifiedDate = DateTime.Parse(request.ModifiedOn);
                    DateTime curTime = DateTime.UtcNow;
                    int elapsedTime = (int)(curTime - modifiedDate).TotalMinutes;
                    if (elapsedTime > 30)
                    {
                        request.Status = "E";
                        request.Message = "Request is taking more time to complete";
                        UpdateAIServiceRequestStatus(request);
                    }
                }
            }
        }


        public int UpdateAIServiceRequestStatus(AICOREMODELS.AIServiceRequestStatus aIServiceRequestStatus)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIModelsService), nameof(UpdateAIServiceRequestStatus), CONSTANTS.START, string.IsNullOrEmpty(aIServiceRequestStatus.CorrelationId) ? default(Guid) : new Guid(aIServiceRequestStatus.CorrelationId), aIServiceRequestStatus.ApplicationId, string.Empty, aIServiceRequestStatus.ClientId, aIServiceRequestStatus.DeliveryconstructId);
            if (aIServiceRequestStatus.Status == "O")
            {
                if (aIServiceRequestStatus.PyCallCount == null)
                {
                    aIServiceRequestStatus.PyCallCount = 1;
                }
                else
                {
                    aIServiceRequestStatus.PyCallCount += aIServiceRequestStatus.PyCallCount + 1;
                }
                if (aIServiceRequestStatus.ServiceId == "72c38b39-c9fe-4fa6-97f5-6c3adc6ba355" || aIServiceRequestStatus.ServiceId == "042468f4-db5b-403f-8fbc-e5378077449e")
                {
                    var Statuscollection = _database.GetCollection<BsonDocument>("Clustering_StatusTable");
                    var builder = Builders<BsonDocument>.Filter;
                    var Statusfilter = builder.Eq(CONSTANTS.CorrelationId, aIServiceRequestStatus.CorrelationId) & builder.Eq("UniId", aIServiceRequestStatus.UniId);
                    var Statusupdate = Builders<BsonDocument>.Update.Set("Status", aIServiceRequestStatus.Status)
                                                                                     .Set("Message", aIServiceRequestStatus.Message)
                                                                                     .Set("ModifiedOn", DateTime.UtcNow.ToString());
                    Statuscollection.UpdateOne(Statusfilter, Statusupdate);
                }

                var requestsCollection = _database.GetCollection<AICOREMODELS.AIServiceRequestStatus>(CONSTANTS.AIServiceRequestStatus);
                var filter = Builders<AICOREMODELS.AIServiceRequestStatus>.Filter.Where(x => x.CorrelationId == aIServiceRequestStatus.CorrelationId)
                             & Builders<AICOREMODELS.AIServiceRequestStatus>.Filter.Where(x => x.UniId == aIServiceRequestStatus.UniId)
                             & Builders<AICOREMODELS.AIServiceRequestStatus>.Filter.Where(x => x.Status == "N");
                var update = Builders<AICOREMODELS.AIServiceRequestStatus>.Update.Set(x => x.Status, aIServiceRequestStatus.Status)
                                                                                 .Set(x => x.Message, aIServiceRequestStatus.Message)
                                                                                 .Set(x => x.PyCallCount, aIServiceRequestStatus.PyCallCount)
                                                                                 .Set(x => x.ModifiedOn, DateTime.UtcNow.ToString());
                var res1 = requestsCollection.UpdateOne(filter, update);
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIModelsService), nameof(UpdateAIServiceRequestStatus), CONSTANTS.END, string.IsNullOrEmpty(aIServiceRequestStatus.CorrelationId) ? default(Guid) : new Guid(aIServiceRequestStatus.CorrelationId), aIServiceRequestStatus.ApplicationId, string.Empty, aIServiceRequestStatus.ClientId, aIServiceRequestStatus.DeliveryconstructId);
                return (int)res1.ModifiedCount;
            }
            else
            {
                if (aIServiceRequestStatus.ServiceId == "72c38b39-c9fe-4fa6-97f5-6c3adc6ba355" || aIServiceRequestStatus.ServiceId == "042468f4-db5b-403f-8fbc-e5378077449e")
                {
                    var Statuscollection = _database.GetCollection<BsonDocument>("Clustering_StatusTable");
                    var builder = Builders<BsonDocument>.Filter;
                    var Statusfilter = builder.Eq(CONSTANTS.CorrelationId, aIServiceRequestStatus.CorrelationId) & builder.Eq("UniId", aIServiceRequestStatus.UniId);
                    var Statusupdate = Builders<BsonDocument>.Update.Set("Status", aIServiceRequestStatus.Status)
                                                                                     .Set("Message", aIServiceRequestStatus.Message)
                                                                                     .Set("ModifiedOn", DateTime.UtcNow.ToString());
                    Statuscollection.UpdateOne(Statusfilter, Statusupdate);
                }

                var requestsCollection = _database.GetCollection<AICOREMODELS.AIServiceRequestStatus>(CONSTANTS.AIServiceRequestStatus);
                var filter = Builders<AICOREMODELS.AIServiceRequestStatus>.Filter.Where(x => x.CorrelationId == aIServiceRequestStatus.CorrelationId)
                             & Builders<AICOREMODELS.AIServiceRequestStatus>.Filter.Where(x => x.UniId == aIServiceRequestStatus.UniId);
                var update = Builders<AICOREMODELS.AIServiceRequestStatus>.Update.Set(x => x.Status, aIServiceRequestStatus.Status)
                                                                                 .Set(x => x.Message, aIServiceRequestStatus.Message)
                                                                                 .Set(x => x.PyCallCount, aIServiceRequestStatus.PyCallCount)
                                                                                 .Set(x => x.ModifiedOn, DateTime.UtcNow.ToString());
                var res1 = requestsCollection.UpdateOne(filter, update);
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIModelsService), nameof(UpdateAIServiceRequestStatus), CONSTANTS.END, string.IsNullOrEmpty(aIServiceRequestStatus.CorrelationId) ? default(Guid) : new Guid(aIServiceRequestStatus.CorrelationId), aIServiceRequestStatus.ApplicationId, string.Empty, aIServiceRequestStatus.ClientId, aIServiceRequestStatus.DeliveryconstructId);
                return (int)res1.ModifiedCount;
            }
            

           
        }

        public void UpdateIEServiceRequestStatus(IERequestQueue aIServiceRequestStatus)
        {
            var requestsCollection = _database.GetCollection<IERequestQueue>(CONSTANTS.IE_RequestQueue);
            var filter = Builders<IERequestQueue>.Filter.Where(x => x.CorrelationId == aIServiceRequestStatus.CorrelationId)
                         & Builders<IERequestQueue>.Filter.Where(x => x.RequestId == aIServiceRequestStatus.RequestId);
            var update = Builders<IERequestQueue>.Update.Set(x => x.Status, aIServiceRequestStatus.Status)
                                                                             .Set(x => x.Message, aIServiceRequestStatus.Message)
                                                                             .Set(x => x.ModifiedOn, DateTime.Now);
            requestsCollection.UpdateOne(filter, update);


        }


        public void UpdateAICoreModels(string correlationId, string status, string message)
        {
            var aiCoreModel = _database.GetCollection<AICOREMODELS.AICoreModels>(CONSTANTS.AICoreModels);
            var filter = Builders<AICOREMODELS.AICoreModels>.Filter.Where(x => x.CorrelationId == correlationId);
            var update = Builders<AICOREMODELS.AICoreModels>.Update.Set(x => x.ModelStatus, status)
                                                                   .Set(x => x.StatusMessage, message)
                                                                   .Set(x => x.ModifiedOn, DateTime.UtcNow.ToString());
            aiCoreModel.UpdateOne(filter, update);
        }



        public void InvokeAIRequest(AICOREMODELS.AIServiceRequestStatus aIServiceRequestStatus)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIModelsService), nameof(InvokeAIRequest), CONSTANTS.START, string.IsNullOrEmpty(aIServiceRequestStatus.CorrelationId) ? default(Guid) : new Guid(aIServiceRequestStatus.CorrelationId), aIServiceRequestStatus.ApplicationId, string.Empty, aIServiceRequestStatus.ClientId, aIServiceRequestStatus.DeliveryconstructId);
            try
            {
                aIServiceRequestStatus.Status = "O";
                aIServiceRequestStatus.Message = "Request Initiated";
                var res = UpdateAIServiceRequestStatus(aIServiceRequestStatus);

                if(res > 0)
                {
                    var service = GetAiCoreServiceDetails(aIServiceRequestStatus.ServiceId);
                    string apiPath = string.Empty;
                    bool IsSuccess;
                    if (aIServiceRequestStatus.ServiceId == "72c38b39-c9fe-4fa6-97f5-6c3adc6ba355" || aIServiceRequestStatus.ServiceId == "042468f4-db5b-403f-8fbc-e5378077449e")
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIModelsService), nameof(InvokeAIRequest), aIServiceRequestStatus.baseUrl + aIServiceRequestStatus.apiPath, string.IsNullOrEmpty(aIServiceRequestStatus.CorrelationId) ? default(Guid) : new Guid(aIServiceRequestStatus.CorrelationId), aIServiceRequestStatus.ApplicationId, string.Empty, aIServiceRequestStatus.ClientId, aIServiceRequestStatus.DeliveryconstructId);
                        apiPath = new Uri(appSettings.ClusteringPythonURL) + aIServiceRequestStatus.apiPath;
                        StringContent content = new StringContent(aIServiceRequestStatus.Payload.ToString(), Encoding.UTF8, "application/json");
                        var message = RoutePOSTRequest(string.Empty, new Uri(appSettings.ClusteringPythonURL), aIServiceRequestStatus.apiPath, content);
                        IsSuccess = message.IsSuccess;
                        aIServiceRequestStatus.Message = message.Message;
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIModelsService), nameof(InvokeAIRequest), "message : " + message.ToString(), string.IsNullOrEmpty(aIServiceRequestStatus.CorrelationId) ? default(Guid) : new Guid(aIServiceRequestStatus.CorrelationId), aIServiceRequestStatus.ApplicationId, string.Empty, aIServiceRequestStatus.ClientId, aIServiceRequestStatus.DeliveryconstructId);
                    }
                    else
                    {
                        apiPath = service.PrimaryTrainApiUrl + "?" + "correlationId=" + aIServiceRequestStatus.CorrelationId + "&userId=" + aIServiceRequestStatus.CreatedByUser + "&pageInfo=" + aIServiceRequestStatus.PageInfo + "&UniqueId=" + aIServiceRequestStatus.UniId;
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIModelsService), nameof(InvokeAIRequest), "AI Request triggered for apiPath - " + apiPath, string.IsNullOrEmpty(aIServiceRequestStatus.CorrelationId) ? default(Guid) : new Guid(aIServiceRequestStatus.CorrelationId), aIServiceRequestStatus.ApplicationId, string.Empty, aIServiceRequestStatus.ClientId, aIServiceRequestStatus.DeliveryconstructId);
                        var message = RouteGETRequest(string.Empty, new Uri(appSettings.AIServicePythonUrl), apiPath, false);
                        IsSuccess = message.IsSuccess;
                        aIServiceRequestStatus.Message = message.Message;
                    }
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIModelsService), nameof(InvokeAIRequest), "AI Request ended for apiPath - " + apiPath, string.IsNullOrEmpty(aIServiceRequestStatus.CorrelationId) ? default(Guid) : new Guid(aIServiceRequestStatus.CorrelationId), aIServiceRequestStatus.ApplicationId, string.Empty, aIServiceRequestStatus.ClientId, aIServiceRequestStatus.DeliveryconstructId);
                    if (!IsSuccess)
                    {
                        aIServiceRequestStatus.Status = "E";
                        UpdateAIServiceRequestStatus(aIServiceRequestStatus);
                        UpdateAICoreModels(aIServiceRequestStatus.CorrelationId, "Error", aIServiceRequestStatus.Message);
                    }
                    else
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIModelsService), nameof(InvokeAIRequest), "AI Python call completed for apiPath - " + apiPath, string.IsNullOrEmpty(aIServiceRequestStatus.CorrelationId) ? default(Guid) : new Guid(aIServiceRequestStatus.CorrelationId), aIServiceRequestStatus.ApplicationId, string.Empty, aIServiceRequestStatus.ClientId, aIServiceRequestStatus.DeliveryconstructId);
                    }
                }
                
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AIModelsService), nameof(InvokeAIRequest), ex.Message, ex, aIServiceRequestStatus.ApplicationId,aIServiceRequestStatus.DataSource,aIServiceRequestStatus.ClientId,aIServiceRequestStatus.DeliveryconstructId);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIModelsService), nameof(InvokeAIRequest), CONSTANTS.END, string.IsNullOrEmpty(aIServiceRequestStatus.CorrelationId) ? default(Guid) : new Guid(aIServiceRequestStatus.CorrelationId),aIServiceRequestStatus.ApplicationId, string.Empty, aIServiceRequestStatus.ClientId, aIServiceRequestStatus.DeliveryconstructId);


        }


        public void InvokeIERequest(IERequestQueue aIServiceRequestStatus)
        {
            try
            {
                aIServiceRequestStatus.Status = "O";
                aIServiceRequestStatus.Message = "Request Initiated";
                UpdateIEServiceRequestStatus(aIServiceRequestStatus);
                //   var service = GetAiCoreServiceDetails(aIServiceRequestStatus.ServiceId);
                string apiPath = "/ingestData";
                string baseUrl = "https://devtest-mywizardapi-si.accenture.com/inference";//appSettings.AIServicePythonUrl;
                JObject payload = new JObject();

                payload["CorrelationId"] = aIServiceRequestStatus.CorrelationId;
                payload["RequestId"] = aIServiceRequestStatus.RequestId;
                payload["UserId"] = aIServiceRequestStatus.CreatedBy;
                payload["PageInfo"] = "IngestData";
                var message = RoutePOSTRequest(string.Empty, new Uri(baseUrl), apiPath,
                    payload, false);
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIModelsService), nameof(InvokeIERequest), "AI Request triggered for apiPath - " + apiPath, string.IsNullOrEmpty(aIServiceRequestStatus.CorrelationId) ? default(Guid) : new Guid(aIServiceRequestStatus.CorrelationId), aIServiceRequestStatus.ApplicationId, string.Empty, aIServiceRequestStatus.ClientUId, aIServiceRequestStatus.DeliveryConstructUId);
                if (!message.IsSuccess)
                {
                    aIServiceRequestStatus.Status = "E";
                    aIServiceRequestStatus.Message = message.Message;
                    UpdateIEServiceRequestStatus(aIServiceRequestStatus);
                    // UpdateAICoreModels(aIServiceRequestStatus.CorrelationId, "Error", message.Message);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AIModelsService), nameof(InvokeIERequest), ex.Message, ex,aIServiceRequestStatus.ApplicationId,string.Empty,aIServiceRequestStatus.ClientUId,aIServiceRequestStatus.DeliveryConstructUId);
            }



        }


        public AICOREMODELS.Service GetAiCoreServiceDetails(string serviceid)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIModelsService), nameof(GetAiCoreServiceDetails), CONSTANTS.START, string.Empty, string.Empty, string.Empty, string.Empty);
            AICOREMODELS.Service service = new AICOREMODELS.Service();
            var serviceCollection = _database.GetCollection<AICOREMODELS.Service>("Services");
            var filter = Builders<AICOREMODELS.Service>.Filter.Where(x => x.ServiceId == serviceid);
            var projection = Builders<AICOREMODELS.Service>.Projection.Exclude("_id");
            var result = serviceCollection.Find(filter).Project<AICOREMODELS.Service>(projection).ToList();
            if (result.Count > 0)
            {
                service = result[0];
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIModelsService), nameof(GetAiCoreServiceDetails), CONSTANTS.START, string.Empty, string.Empty, string.Empty, string.Empty);
            return service;
        }

        public AICOREMODELS.MethodReturn<object> RouteGETRequest(string token, Uri baseUrl, string apiPath, bool isReturnArray)
        {
            AICOREMODELS.MethodReturn<object> returnValue = new AICOREMODELS.MethodReturn<object>();

            if (appSettings.Environment == CONSTANTS.PAMEnvironment)
            {
                token = _tokenService.GenerateVDSPAMToken(CONSTANTS.VDSApplicationID_PAM);
            }
            else
            {
                token = _tokenService.GeneratePADToken();
            }
            try
            {
                HttpResponseMessage message = webHelper.InvokeGETRequest(token, baseUrl.ToString(), apiPath);
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
                    returnValue.Message = message.ReasonPhrase+"_"+ message.Content.ReadAsStringAsync().Result;
                }
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIModelsService), nameof(RouteGETRequest), CONSTANTS.END + " AI Service Python Call ended. apiPath: " + apiPath + " Message: " + Convert.ToString(returnValue.Message) + " IsSuccess: " + Convert.ToString(returnValue.IsSuccess) + " ReturnValue: " + Convert.ToString(returnValue.ReturnValue) , string.Empty, string.Empty, string.Empty, string.Empty);
            }
            catch (Exception ex)
            {
                returnValue.IsSuccess = false;
                returnValue.Message = ex.Message;

            }
            return returnValue;
        }


        public AICOREMODELS.MethodReturn<object> RoutePOSTRequest(string token, Uri baseUrl, string apiPath, JObject requestPayload, bool isReturnArray)
        {
            AICOREMODELS.MethodReturn<object> returnValue = new AICOREMODELS.MethodReturn<object>();
            if (appSettings.Environment == CONSTANTS.PAMEnvironment)
            {
                token = _tokenService.GenerateVDSPAMToken(CONSTANTS.VDSApplicationID_PAM);
            }
            else
            {
                token = _tokenService.GeneratePADToken();
            }
            try
            {

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
                    returnValue.Message = message.ReasonPhrase+"_"+ message.Content.ReadAsStringAsync().Result;
                }
            }
            catch (Exception ex)
            {
                returnValue.IsSuccess = false;
                returnValue.Message = ex.Message;

            }
            return returnValue;
        }
        public AICOREMODELS.MethodReturn<object> RoutePOSTRequest(string token, Uri baseUrl, string apiPath, StringContent content)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIModelsService), nameof(RoutePOSTRequest), CONSTANTS.START, string.Empty, string.Empty, string.Empty, string.Empty);
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIModelsService), nameof(RoutePOSTRequest), "token : " + token + "," + "baseUrl : " + baseUrl + "," + "apiPath : " + apiPath + "," + "content : " + JsonConvert.SerializeObject(content), string.Empty, string.Empty, string.Empty, string.Empty);
            AICOREMODELS.MethodReturn<object> returnValue = new AICOREMODELS.MethodReturn<object>();
            if (appSettings.Environment == CONSTANTS.PAMEnvironment)
            {
                token = _tokenService.GenerateVDSPAMToken(CONSTANTS.VDSApplicationID_PAM);
            }
            else
            {
                token = _tokenService.GeneratePADToken();
            }
            try
            {
                HttpResponseMessage message = webHelper.InvokePOSTRequest(token, baseUrl.ToString(), apiPath, content);
                if (message != null && message.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    returnValue.IsSuccess = true;
                }
                else
                {
                    returnValue.IsSuccess = false;
                    returnValue.Message = message.ReasonPhrase;
                }
            }
            catch (Exception ex)
            {
                returnValue.IsSuccess = false;
                returnValue.Message = ex.Message;
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AIModelsService), nameof(RoutePOSTRequest), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIModelsService), nameof(RoutePOSTRequest), CONSTANTS.END, string.Empty, string.Empty, string.Empty, string.Empty);
            return returnValue;
        }

    }




}
