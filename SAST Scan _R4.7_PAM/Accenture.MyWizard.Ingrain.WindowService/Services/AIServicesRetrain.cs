using Accenture.MyWizard.Ingrain.WindowService.Interfaces;
using Accenture.MyWizard.Ingrain.WindowService.Models;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
using System.Text;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using Newtonsoft.Json.Linq;
using System.Threading;
using RestSharp;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net;
using Accenture.MyWizard.Fortress.Core.Configurations;
using AIModels = Accenture.MyWizard.Ingrain.DataModels.AICore;
using Accenture.MyWizard.Ingrain.DataModels.AICore;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Accenture.MyWizard.Ingrain.WindowService.Services
{
    public class AIServicesRetrain : IAIServicesRetrain
    {
        #region Private Members      
        private readonly DatabaseProvider databaseProvider;
        private MongoClient _mongoClient;
        private IMongoDatabase _database;
        private IConfigurationRoot appSettings;
        private string source;
        private string _aesKey;
        private string _aesVector;
        private string username;
        private string password;
        private string tokenAPIUrl;
        private string authProvider;
        private string token_Url_VDS;
        private string Grant_Type_VDS;
        private string clientId_VDS;
        private string clientSecret_VDS;
        private string scopeStatus_VDS;
        private string resource_ingrain;
        private string baseUrl;
        private string ClusteringPythonURL;
        private string Environment;

        private TokenService _tokenService;

        #endregion
        #region Constructor
        public AIServicesRetrain(DatabaseProvider db)
        {
            databaseProvider = db;
            appSettings = AppSettingsJson.GetAppSettings();
            _mongoClient = databaseProvider.GetDatabaseConnection();
            var dataBaseName = MongoUrl.Create(appSettings.GetSection("AppSettings").GetSection("connectionString").Value).DatabaseName;
            _database = _mongoClient.GetDatabase(dataBaseName);
            source = appSettings.GetSection("AppSettings").GetSection("Source").Value;
            _aesKey = appSettings.GetSection("AppSettings").GetSection("aesKey").Value;
            username = appSettings.GetSection("AppSettings").GetSection("username").Value;
            tokenAPIUrl = appSettings.GetSection("AppSettings").GetSection("tokenAPIUrl").Value;
            authProvider = appSettings.GetSection("AppSettings").GetSection("authProvider").Value;
            token_Url_VDS = appSettings.GetSection("AppSettings").GetSection("token_Url_VDS").Value;
            Grant_Type_VDS = appSettings.GetSection("AppSettings").GetSection("Grant_Type_VDS").Value;
            clientId_VDS = appSettings.GetSection("AppSettings").GetSection("clientId_VDS").Value;
            clientSecret_VDS = appSettings.GetSection("AppSettings").GetSection("clientSecret_VDS").Value;
            scopeStatus_VDS = appSettings.GetSection("AppSettings").GetSection("scopeStatus_VDS").Value;
            resource_ingrain = appSettings.GetSection("AppSettings").GetSection("resource_ingrain").Value;
            baseUrl = appSettings.GetSection("AppSettings").GetSection("AICorePythonURL").Value;
            ClusteringPythonURL = appSettings.GetSection("AppSettings").GetSection("ClusteringPythonURL").Value;
            Environment = appSettings.GetSection("AppSettings").GetSection("Enviroment").Value;
            _tokenService = new TokenService();
        }
            #endregion

            #region AIServices AutoTrain
            public IngestData IngestData(string correlationId)
        {
            IngestData data = new IngestData();
            LogAIServiceAutoTrain logAIServiceAutoTrain = new LogAIServiceAutoTrain();
            try
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIServicesRetrain), nameof(IngestData), "AISERVICES AUTO TRAIN--START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                var collection = _database.GetCollection<AIRequestStatus>(CONSTANTS.AIServiceRequestStatus);
                var filterBuilder = Builders<AIRequestStatus>.Filter;
                var filter = filterBuilder.Eq(CONSTANTS.CorrelationId, correlationId) & filterBuilder.Eq(CONSTANTS.PageInfo, CONSTANTS.IngestData);
                var result = collection.Find(filter).FirstOrDefault();
                if (result != null)
                {
                    result.ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                    result.Status = null;
                    result.RequestStatus = null;
                    result.Progress = null;
                    result.Flag = source;
                    result.Message = null;
                    if (!string.IsNullOrEmpty(result.SourceDetails))
                    {
                        JObject jObj = JObject.Parse(result.SourceDetails);
                        jObj["Flag"] = source;
                        result.SourceDetails = jObj.ToString(Formatting.None);
                    }
                    RemoveQueueRecords(correlationId, CONSTANTS.IngestData);

                    logAIServiceAutoTrain.CorrelationId = correlationId;
                    logAIServiceAutoTrain.ServiceId = result.ServiceId;
                    logAIServiceAutoTrain.PageInfo = CONSTANTS.IngestData;
                    logAIServiceAutoTrain.FunctionName = "InsertRequests";
                    logAIServiceAutoTrain.Message = "InsertRequests Started";
                    logAIServiceAutoTrain.StartedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                    InsertAIServiceAutoTrainLog(logAIServiceAutoTrain);

                    InsertRequests(result);
                    bool callMethod = true;
                    //Invoke Python API for Appending Data
                    AIModels.Service service = GetAiCoreServiceDetails(result.ServiceId);
                    data.ServiceCode = service.ServiceCode;
                    service.PrimaryTrainApiUrl = service.PrimaryTrainApiUrl + "?" + "correlationId=" + correlationId + "&userId=" + result.CreatedByUser + "&pageInfo=" + result.PageInfo + "&UniqueId=" + result.UniId;
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIServicesRetrain), nameof(IngestData), "AISERVICES AUTO TRAIN RouteGETRequest--START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);

                    logAIServiceAutoTrain.FunctionName = "RouteGETRequest";
                    logAIServiceAutoTrain.Message = "RouteGETRequest Started Url :" + baseUrl + service.PrimaryTrainApiUrl;
                    InsertAIServiceAutoTrainLog(logAIServiceAutoTrain);

                    RouteGETRequest(baseUrl, service.PrimaryTrainApiUrl);
                    //Ingesting Data Start
                    while (callMethod)
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIServicesRetrain), nameof(IngestData), "AISERVICES AUTO TRAIN CheckPythonProcess--START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                        var data2 = CheckPythonProcess(correlationId, CONSTANTS.IngestData);
                        if (!string.IsNullOrEmpty(data2))
                        {
                            JObject queueData = JObject.Parse(data2);
                            string Status = (string)queueData[CONSTANTS.Status];
                            string Progress = (string)queueData[CONSTANTS.Progress];
                            string Message = (string)queueData[CONSTANTS.Message];
                            if (Status == CONSTANTS.C & Progress == CONSTANTS.Hundred)
                            {
                                logAIServiceAutoTrain.EndedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                                logAIServiceAutoTrain.FunctionName = "CheckPythonProcess";
                                logAIServiceAutoTrain.Status = Status;
                                logAIServiceAutoTrain.Progress = Progress;
                                logAIServiceAutoTrain.Message = Message;
                                InsertAIServiceAutoTrainLog(logAIServiceAutoTrain);

                                callMethod = false;
                                data.IsIngestionCompleted = true;
                                return data;
                            }
                            else if (Status == CONSTANTS.E)
                            {
                                logAIServiceAutoTrain.EndedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                                logAIServiceAutoTrain.FunctionName = "CheckPythonProcess";
                                logAIServiceAutoTrain.Status = Status;
                                logAIServiceAutoTrain.Progress = Progress;
                                logAIServiceAutoTrain.Message = Message;
                                InsertAIServiceAutoTrainLog(logAIServiceAutoTrain);

                                callMethod = false;
                                data.IsIngestionCompleted = false;
                                return data;
                            }
                            else
                            {
                                logAIServiceAutoTrain.FunctionName = "CheckPythonProcess";
                                logAIServiceAutoTrain.Status = Status;
                                logAIServiceAutoTrain.Progress = Progress;
                                logAIServiceAutoTrain.Message = Message;
                                InsertAIServiceAutoTrainLog(logAIServiceAutoTrain);

                                Thread.Sleep(1000);
                                callMethod = true;
                            }                            
                        }
                    }
                }

                else
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIServicesRetrain), nameof(IngestData), "AISERVICES SCRUMBAN AUTO TRAIN--START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                    var scrumbanFilter = filterBuilder.Eq(CONSTANTS.CorrelationId, correlationId) & filterBuilder.Eq(CONSTANTS.PageInfo, "Ingest_Train");
                    var scrumbanResult = collection.Find(scrumbanFilter).FirstOrDefault();
                    if (scrumbanResult != null)
                    {
                        scrumbanResult.ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                        scrumbanResult.Status = null;
                        scrumbanResult.RequestStatus = null;
                        scrumbanResult.Progress = null;
                        scrumbanResult.Flag = source;
                        scrumbanResult.Message = null;
                        if (!string.IsNullOrEmpty(scrumbanResult.SourceDetails))
                        {
                            JObject jObj = JObject.Parse(scrumbanResult.SourceDetails);
                            jObj["Flag"] = source;
                            scrumbanResult.SourceDetails = jObj.ToString(Formatting.None);
                        }
                        RemoveQueueRecords(correlationId, "Ingest_Train");

                        logAIServiceAutoTrain.CorrelationId = correlationId;
                        logAIServiceAutoTrain.ServiceId = scrumbanResult.ServiceId;
                        logAIServiceAutoTrain.PageInfo = "Ingest_Train";
                        logAIServiceAutoTrain.FunctionName = "InsertRequests";
                        logAIServiceAutoTrain.Message = "InsertRequests Scrumban Started";
                        logAIServiceAutoTrain.StartedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                        InsertAIServiceAutoTrainLog(logAIServiceAutoTrain);

                        InsertRequests(scrumbanResult);
                        bool callMethod = true;
                        //Invoke Python API for Appending Data
                        AIModels.Service service = GetAiCoreServiceDetails(scrumbanResult.ServiceId);
                        data.ServiceCode = service.ServiceCode;
                        service.PrimaryTrainApiUrl = service.PrimaryTrainApiUrl + "?" + "correlationId=" + correlationId + "&userId=" + scrumbanResult.CreatedByUser + "&pageInfo=" + scrumbanResult.PageInfo + "&UniqueId=" + scrumbanResult.UniId;
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIServicesRetrain), nameof(IngestData), "AISERVICES SCRUMBAN AUTO TRAIN RouteGETRequest--START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);

                        logAIServiceAutoTrain.FunctionName = "RouteGETRequest";
                        logAIServiceAutoTrain.Message = "RouteGETRequest Started Url :" + baseUrl + service.PrimaryTrainApiUrl;
                        InsertAIServiceAutoTrainLog(logAIServiceAutoTrain);
                        RouteGETRequest(baseUrl, service.PrimaryTrainApiUrl);
                        //Ingesting Data Start
                        while (callMethod)
                        {
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIServicesRetrain), nameof(IngestData), "AISERVICES SCRUMBAN AUTO TRAIN CheckPythonProcess--START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                            var data2 = CheckPythonProcess(correlationId, "Ingest_Train");
                            if (!string.IsNullOrEmpty(data2))
                            {
                                JObject queueData = JObject.Parse(data2);
                                string Status = (string)queueData[CONSTANTS.Status];
                                string Progress = (string)queueData[CONSTANTS.Progress];
                                string Message = (string)queueData[CONSTANTS.Message];
                                if (Status == CONSTANTS.C & Progress == CONSTANTS.Hundred)
                                {
                                    callMethod = false;
                                    data.IsIngestionCompleted = true;
                                    UpdateAiModel(correlationId);
                                    return data;
                                }
                                else if (Status == CONSTANTS.E)
                                {
                                    callMethod = false;
                                    data.IsIngestionCompleted = false;
                                    return data;
                                }
                                else
                                {
                                    Thread.Sleep(1000);
                                    callMethod = true;
                                }
                                logAIServiceAutoTrain.FunctionName = "CheckPythonProcess";
                                logAIServiceAutoTrain.Status = Status;
                                logAIServiceAutoTrain.Progress = Progress;
                                logAIServiceAutoTrain.Message = Message;
                                InsertAIServiceAutoTrainLog(logAIServiceAutoTrain);
                            }
                        }
                    }
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIServicesRetrain), nameof(IngestData), "AISERVICES SCRUMBAN AUTO TRAIN--END", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                }
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIServicesRetrain), nameof(IngestData), "AISERVICES AUTO TRAIN--END", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                return data;
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AIServicesRetrain), nameof(IngestData), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return data;
            }
        }
        private void InsertAIServiceAutoTrainLog(LogAIServiceAutoTrain logAIServiceAutoTrain)
        {
            var collection = _database.GetCollection<LogAIServiceAutoTrain>("AIServiceAutoTrainLog");
            collection.InsertOne(logAIServiceAutoTrain);
        }
        private bool RouteGETRequest(string baseUrl, string apiPath)
        {
            string token = string.Empty;
            JObject response = new JObject();
            try
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIServicesRetrain), nameof(RouteGETRequest), "ROUTEGETREQUEST--START--" + baseUrl + apiPath, string.Empty, string.Empty, string.Empty, string.Empty);

                if (Environment == "PAM")
                {
                    token = _tokenService.GenerateVDSPAMToken(CONSTANTS.VDSApplicationID_PAM);
                }
                else
                {
                    token = PythonAIServiceToken();
                }

                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIServicesRetrain), nameof(RouteGETRequest), "TOKEN--END--" + token + "--" + baseUrl + apiPath, string.Empty, string.Empty, string.Empty, string.Empty);
                HttpResponseMessage message = InvokeGETRequest(token, baseUrl, apiPath);
                if (message != null && message.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    response = Newtonsoft.Json.JsonConvert.DeserializeObject<JObject>(message.Content.ReadAsStringAsync().Result);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIServicesRetrain), nameof(RouteGETRequest), "PYTHON API FAILED WITH RESPONSE: HTTPSTATUS--" + message.StatusCode + "TOKEN--" + token, string.Empty, string.Empty, string.Empty, string.Empty);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIServicesRetrain), nameof(RouteGETRequest), "ROUTEGETREQUEST--END", string.Empty, string.Empty, string.Empty, string.Empty);
                    return true;
                }
                else
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIServicesRetrain), nameof(RouteGETRequest), "PYTHON API FAILED WITH RESPONSE--URL" + baseUrl + apiPath + "--HTTPSTATUS" + message.StatusCode, string.Empty, string.Empty, string.Empty, string.Empty);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIServicesRetrain), nameof(RouteGETRequest), "ROUTEGETREQUEST--END", string.Empty, string.Empty, string.Empty, string.Empty);
                    return false;
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIServicesRetrain), nameof(RouteGETRequest), "ROUTEGETREQUEST--END", string.Empty, string.Empty, string.Empty, string.Empty);
                return false;
            }
        }
        private bool RoutePOSTRequest(Uri baseUrl, string apiPath, JObject requestPayload)
        {
            string token = string.Empty;
            JObject response = new JObject();
            try
            {

                if (Environment == "PAM")
                {
                    token = _tokenService.GenerateVDSPAMToken(CONSTANTS.VDSApplicationID_PAM);
                }
                else
                {
                    token = PythonAIServiceToken();
                }

                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIServicesRetrain), nameof(RoutePOSTRequest), "PYTHON CALL AUTO TRAIN--START--" + token, string.Empty, string.Empty, string.Empty, string.Empty);
                StringContent content = new StringContent(requestPayload.ToString(), Encoding.UTF8, "application/json");
                HttpResponseMessage message = InvokePOSTRequest(token, baseUrl.ToString(), apiPath, content);
                if (message != null && message.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    response = Newtonsoft.Json.JsonConvert.DeserializeObject<JObject>(message.Content.ReadAsStringAsync().Result);
                }
                else
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIServicesRetrain), nameof(RoutePOSTRequest), "PYTHON CALL AUTO TRAIN FAIL--START--" + token, string.Empty, string.Empty, string.Empty, string.Empty);
                    return false;
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AIServicesRetrain), nameof(RoutePOSTRequest), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return false;
            }
            return false;
        }
        private HttpResponseMessage InvokePOSTRequest(string token, string baseURI, string apiPath, StringContent content)
        {
            using (var client = new HttpClient())
            {
                string uri = baseURI + apiPath;
                client.Timeout = new TimeSpan(0, 30, 0);
                //client.BaseAddress = new Uri(baseURI);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                if (!string.IsNullOrWhiteSpace(token))
                {
                    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                }
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                return client.PostAsync(uri, content).Result;
            }
        }
        private HttpResponseMessage InvokeGETRequest(string token, string baseURI, string apiPath)
        {
            using (var client = new HttpClient())
            {
                string uri = baseURI + apiPath;
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIServicesRetrain), nameof(InvokeGETRequest), "PYTHON URL DETAILS:" + uri.ToString(), string.Empty, string.Empty, string.Empty, string.Empty);
                client.Timeout = new TimeSpan(0, 30, 0);
                //client.BaseAddress = new Uri(baseURI);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                if (!string.IsNullOrWhiteSpace(token))
                {
                    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                }
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                return client.GetAsync(uri).Result;
            }
        }
        private string PythonAIServiceToken()
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIServicesRetrain), nameof(PythonAIServiceToken), CONSTANTS.START + authProvider.ToUpper(), string.Empty, string.Empty, string.Empty, string.Empty);
            dynamic token = string.Empty;
            if (authProvider.ToUpper() == "FORM")
            {
                if (Environment == CONSTANTS.PAMEnvironment)
                {
                    using (var client = new HttpClient())
                    {
                        client.BaseAddress = new Uri(tokenAPIUrl);
                        client.DefaultRequestHeaders.Accept.Clear();
                        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        string json = Newtonsoft.Json.JsonConvert.SerializeObject(new
                        {
                            username = username,
                            password = password
                        });
                        var requestOptions = new StringContent(json, Encoding.UTF8, "application/json");

                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                        var result = client.PostAsync("", requestOptions).Result;
                        if (result.IsSuccessStatusCode)
                        {
                            var result1 = result.Content.ReadAsStringAsync().Result;
                            var tokenObj = Newtonsoft.Json.JsonConvert.DeserializeObject(result1) as dynamic;
                            token = Environment == CONSTANTS.PAMEnvironment ? Convert.ToString(tokenObj.token) :
                                Convert.ToString(tokenObj.access_token);
                        }
                        else
                        {
                            token = CONSTANTS.InvertedComma;
                        }
                    }
                }
                else
                {
                    var tokenendpointurl = Convert.ToString(tokenAPIUrl);
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
                    //if (result.StatusCode != HttpStatusCode.OK)
                    //{
                    //    throw new Exception(string.Format(CONSTANTS.TokenError, result.StatusCode));
                    //}
                    //var result1 = result.Content.ReadAsStringAsync().Result;
                    //var tokenObj = JsonConvert.DeserializeObject(result1) as dynamic;
                    //var token = Convert.ToString(tokenObj.token);                        
                    //LOGGING.LogManager.Logger.LogProcessInfo(typeof(PhoenixTokenService), nameof(GeneratePAMToken), "GenerateToken- END", string.Empty, string.Empty, string.Empty, string.Empty);
                    //return tokenObj;


                    //client.DefaultRequestHeaders.Add(CONSTANTS.username, username);
                    //client.DefaultRequestHeaders.Add(CONSTANTS.password, password);

                    //var postData = Newtonsoft.Json.JsonConvert.SerializeObject(pAMData, Formatting.None, new JsonSerializerSettings()
                    //{
                    //    DateFormatHandling = DateFormatHandling.MicrosoftDateFormat,
                    //    DateParseHandling = DateParseHandling.DateTimeOffset
                    //});
                    //var stringContent = new StringContent(postData, UnicodeEncoding.UTF8, CONSTANTS.APPLICATION_JSON);
                    //var tokenResponse = client.PostAsync(tokenAPIUrl, null).Result;
                    //var tokenResult = tokenResponse.Content.ReadAsStringAsync().Result;
                    //Dictionary<string, string> tokenDictionary = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(tokenResult);
                    //if (tokenDictionary != null)
                    //{
                    //    if (tokenResponse.IsSuccessStatusCode)
                    //    {
                    //        token = Environment == CONSTANTS.PAMEnvironment ? tokenDictionary[CONSTANTS.token].ToString() :
                    //            tokenDictionary[CONSTANTS.access_token].ToString();
                    //    }
                    //    else
                    //    {
                    //        token = CONSTANTS.InvertedComma;
                    //    }
                    //}
                    //else
                    //{
                    //    token = CONSTANTS.InvertedComma;
                    //}
                }

            }
            else
            {

                var client = new RestClient(token_Url_VDS);
                var request = new RestRequest(Method.POST);
                request.AddHeader("cache-control", "no-cache");
                request.AddHeader("content-type", "application/x-www-form-urlencoded");
                request.AddParameter("application/x-www-form-urlencoded", "grant_type=" + Grant_Type_VDS +
                   "&client_id=" + clientId_VDS +
                   "&client_secret=" + clientSecret_VDS +
                   "&scope=" + scopeStatus_VDS +
                   "&resource=" + resource_ingrain,
                   ParameterType.RequestBody);
                var requestBuilder = new StringBuilder();
                foreach (var param in request.Parameters)
                {
                    requestBuilder.AppendFormat("{0}: {1}\r\n", param.Name, param.Value);
                }
                requestBuilder.ToString();
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIServicesRetrain), nameof(PythonAIServiceToken), "PYTHON TOKEN PARAMS -- " + requestBuilder, string.Empty, string.Empty, string.Empty, string.Empty);
                IRestResponse response = client.Execute(request);
                string json = response.Content;
                // Retrieve and Return the Access Token                
                var tokenObj = Newtonsoft.Json.JsonConvert.DeserializeObject(json) as dynamic;
                token = Convert.ToString(tokenObj.access_token);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIServicesRetrain), nameof(PythonAIServiceToken), "END -" + authProvider.ToUpper(), string.Empty, string.Empty, string.Empty, string.Empty);
            return token;
        }

        public bool AIServicesTraining(string correlationId)
        {
            LogAIServiceAutoTrain logAIServiceAutoTrain = new LogAIServiceAutoTrain();
            try
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIServicesRetrain), nameof(AIServicesTraining), "START--" + correlationId, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                bool isTrainingCompleted = false;
                var collection = _database.GetCollection<BsonDocument>(CONSTANTS.AIServiceRequestStatus);
                var filterBuilder = Builders<BsonDocument>.Filter;
                var filter = filterBuilder.Eq(CONSTANTS.CorrelationId, correlationId) & filterBuilder.Eq(CONSTANTS.PageInfo, CONSTANTS.TrainModel);
                var result = collection.Find(filter).FirstOrDefault();
                if (result != null)
                {
                    result["ModifiedOn"] = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                    result["Status"] = BsonNull.Value;
                    result["RequestStatus"] = BsonNull.Value;
                    result["Progress"] = BsonNull.Value;
                    result["Message"] = BsonNull.Value;
                    RemoveQueueRecords(correlationId, CONSTANTS.TrainModel);

                    logAIServiceAutoTrain.CorrelationId = correlationId;
                    logAIServiceAutoTrain.ServiceId = result["ServiceId"].ToString();
                    logAIServiceAutoTrain.PageInfo = CONSTANTS.TrainModel;
                    logAIServiceAutoTrain.FunctionName = "AIServicesTraining";
                    logAIServiceAutoTrain.Message = "AIServicesTraining Started";
                    logAIServiceAutoTrain.StartedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                    InsertAIServiceAutoTrainLog(logAIServiceAutoTrain);

                    InsertRequests(result);
                    JObject payload = new JObject();
                    payload["client_id"] = result["ClientId"].ToString();
                    payload["dc_id"] = result["DeliveryconstructId"].ToString();
                    payload["correlation_id"] = result["CorrelationId"].ToString();

                    logAIServiceAutoTrain.FunctionName = "TrainAIModel";
                    logAIServiceAutoTrain.Message = "TrainAIModel Started";
                    logAIServiceAutoTrain.StartedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                    InsertAIServiceAutoTrainLog(logAIServiceAutoTrain);

                    bool isPythonSuccess = TrainAIModel(result["ServiceId"].ToString(), result["CorrelationId"].ToString(), result["CreatedByUser"].ToString(), result["UniId"].ToString(), payload);
                    if (isPythonSuccess)
                    {
                        bool callMethod = true;
                        //Training Start
                        while (callMethod)
                        {
                            var data = CheckPythonProcess(correlationId, CONSTANTS.TrainModel);
                            if (!string.IsNullOrEmpty(data))
                            {
                                JObject queueData = JObject.Parse(data);
                                string Status = (string)queueData[CONSTANTS.Status];
                                string Progress = (string)queueData[CONSTANTS.Progress];
                                string Message = (string)queueData[CONSTANTS.Message];
                                if (Status == CONSTANTS.C & Progress == CONSTANTS.Hundred)
                                {
                                    logAIServiceAutoTrain.EndedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                                    logAIServiceAutoTrain.FunctionName = "CheckPythonProcess";
                                    logAIServiceAutoTrain.Message = Message;
                                    logAIServiceAutoTrain.Status = Status;
                                    logAIServiceAutoTrain.Progress = Progress;
                                    InsertAIServiceAutoTrainLog(logAIServiceAutoTrain);

                                    callMethod = false;
                                    isTrainingCompleted = true;
                                    UpdateAiModel(correlationId);
                                }
                                else if (Status == CONSTANTS.E)
                                {
                                    logAIServiceAutoTrain.EndedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                                    logAIServiceAutoTrain.FunctionName = "CheckPythonProcess";
                                    logAIServiceAutoTrain.Message = Message;
                                    logAIServiceAutoTrain.Status = Status;
                                    logAIServiceAutoTrain.Progress = Progress;
                                    InsertAIServiceAutoTrainLog(logAIServiceAutoTrain);

                                    callMethod = false;
                                    return false;
                                }
                                else
                                {
                                    logAIServiceAutoTrain.FunctionName = "CheckPythonProcess";
                                    logAIServiceAutoTrain.Message = Message;
                                    logAIServiceAutoTrain.Status = Status;
                                    logAIServiceAutoTrain.Progress = Progress;
                                    InsertAIServiceAutoTrainLog(logAIServiceAutoTrain);

                                    Thread.Sleep(1000);
                                    callMethod = true;
                                }                                
                            }
                        }
                    }
                }
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIServicesRetrain), nameof(AIServicesTraining), "END", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                return isTrainingCompleted;
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AIServicesRetrain), nameof(AIServicesTraining), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return false;
            }
        }
        private void UpdateAiModel(string correlationId)
        {
            var Collection = _database.GetCollection<BsonDocument>(CONSTANTS.AICoreModels);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var update = Builders<BsonDocument>.Update.Set(CONSTANTS.ModifiedOn, DateTime.Now.ToString(CONSTANTS.DateHoursFormat));
            var result = Collection.UpdateOne(filter, update);
        }
        private bool TrainAIModel(string serviceId, string correlationId, string useriId, string uniId, JObject payload)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIServicesRetrain), nameof(TrainAIModel), "STARTED--" + correlationId, string.Empty, string.Empty, string.Empty, string.Empty);
            AIModels.Service service = GetAiCoreServiceDetails(serviceId);
            bool returnValue = false;
            switch (service.ServiceMethod)
            {
                case "GET":
                    service.PrimaryTrainApiUrl = service.PrimaryTrainApiUrl + "?" + "correlationId=" + correlationId + "&userId=" + useriId + "&pageInfo=" + "TrainModel" + "&UniqueId=" + uniId;
                    returnValue = RouteGETRequest(baseUrl, service.PrimaryTrainApiUrl);
                    break;
                case "POST":
                    returnValue = RoutePOSTRequest(new Uri(baseUrl), service.PrimaryTrainApiUrl, payload);
                    break;
            }
            return returnValue;
        }
        #endregion

        #region Clustering AutoTrain
        public bool ClusterServicesTraining(string correlationId)
        {
            try
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIServicesRetrain), nameof(ClusterServicesTraining), "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                bool isIngestComplete = false;
                var collection = _database.GetCollection<ClusteringAPIModel>(CONSTANTS.Clustering_IngestData);
                var filterBuilder = Builders<ClusteringAPIModel>.Filter;
                var filter = filterBuilder.Eq(CONSTANTS.CorrelationId, correlationId) & filterBuilder.Eq(CONSTANTS.PageInfo, CONSTANTS.InvokeIngestData);
                var projection = Builders<ClusteringAPIModel>.Projection.Exclude("_id");
                var result = collection.Find(filter).Project<BsonDocument>(projection).FirstOrDefault();
                if (result != null)
                {
                    result["ModifiedOn"] = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                    result["retrain"] = true;
                    if (!string.IsNullOrEmpty(result["ParamArgs"].ToString()))
                    {
                        JObject jObj = JObject.Parse(result["ParamArgs"].ToString());
                        jObj["Flag"] = source;
                        result["ParamArgs"] = jObj.ToString(Formatting.None);
                    }
                    UpdateExistingCLusterRecords(correlationId, CONSTANTS.InvokeIngestData);
                    // RemoveClusterIngestData(correlationId, CONSTANTS.InvokeIngestData);

                    LogAIServiceAutoTrain logAIServiceAutoTrain = new LogAIServiceAutoTrain();
                    logAIServiceAutoTrain.CorrelationId = correlationId;
                    logAIServiceAutoTrain.ServiceId = result["ServiceID"].ToString();
                    logAIServiceAutoTrain.PageInfo = CONSTANTS.InvokeIngestData;
                    logAIServiceAutoTrain.FunctionName = "ClusterServicesTraining";
                    logAIServiceAutoTrain.Message = "ClusterServicesTraining Started";
                    logAIServiceAutoTrain.StartedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                    InsertAIServiceAutoTrainLog(logAIServiceAutoTrain);

                    InsertClusteringRequest(result);
                    bool isSuccess = TrainPythonCluster(result["CorrelationId"].ToString(), result["UniId"].ToString(), result["CreatedBy"].ToString(), result["PageInfo"].ToString());
                    if (isSuccess)
                    {
                        bool callMethod = true;
                        //Ingesting Data Start
                        while (callMethod)
                        {
                            var data = CheckClusterPythonProcess(correlationId, CONSTANTS.DataCuration);
                            if (!string.IsNullOrEmpty(data))
                            {
                                JObject queueData = JObject.Parse(data);
                                string Status = (string)queueData[CONSTANTS.Status];
                                string Progress = (string)queueData[CONSTANTS.Progress];
                                string Message = (string)queueData[CONSTANTS.Message];                                

                                if (Status == CONSTANTS.C & Progress == CONSTANTS.Hundred)
                                {
                                    logAIServiceAutoTrain.PageInfo = CONSTANTS.DataCuration;
                                    logAIServiceAutoTrain.FunctionName = "CheckClusterPythonProcess";
                                    logAIServiceAutoTrain.Message = Message;
                                    logAIServiceAutoTrain.Status = Status;
                                    logAIServiceAutoTrain.Progress = Progress;
                                    InsertAIServiceAutoTrainLog(logAIServiceAutoTrain);

                                    data = CheckClusterPythonProcess(correlationId, CONSTANTS.DataTransformation);
                                    if (!string.IsNullOrEmpty(data))
                                    {
                                        queueData = JObject.Parse(data);
                                        Status = (string)queueData[CONSTANTS.Status];
                                        Progress = (string)queueData[CONSTANTS.Progress];
                                        Message = (string)queueData[CONSTANTS.Message];
                                        if (Status == CONSTANTS.C & Progress == CONSTANTS.Hundred)
                                        {
                                            logAIServiceAutoTrain.PageInfo = CONSTANTS.DataTransformation;
                                            logAIServiceAutoTrain.FunctionName = "CheckClusterPythonProcess";
                                            logAIServiceAutoTrain.Message = Message;
                                            logAIServiceAutoTrain.Status = Status;
                                            logAIServiceAutoTrain.Progress = Progress;
                                            InsertAIServiceAutoTrainLog(logAIServiceAutoTrain);

                                            data = CheckClusterPythonProcess(correlationId, CONSTANTS.ModelTraining);
                                            if (!string.IsNullOrEmpty(data))
                                            {
                                                queueData = JObject.Parse(data);
                                                Status = (string)queueData[CONSTANTS.Status];
                                                Progress = (string)queueData[CONSTANTS.Progress];
                                                Message = (string)queueData[CONSTANTS.Message];
                                                if (Status == CONSTANTS.C & Progress == CONSTANTS.Hundred)
                                                {
                                                    logAIServiceAutoTrain.EndedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                                                    logAIServiceAutoTrain.PageInfo = CONSTANTS.ModelTraining;
                                                    logAIServiceAutoTrain.Message = Message;
                                                    logAIServiceAutoTrain.Status = Status;
                                                    logAIServiceAutoTrain.Progress = Progress;
                                                    InsertAIServiceAutoTrainLog(logAIServiceAutoTrain);
                                                    callMethod = false;
                                                    return true;
                                                }
                                                else if (Status == CONSTANTS.E)
                                                {
                                                    logAIServiceAutoTrain.EndedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                                                    logAIServiceAutoTrain.PageInfo = CONSTANTS.ModelTraining;
                                                    logAIServiceAutoTrain.Message = Message;
                                                    logAIServiceAutoTrain.Status = Status;
                                                    logAIServiceAutoTrain.Progress = Progress;
                                                    InsertAIServiceAutoTrainLog(logAIServiceAutoTrain);
                                                    callMethod = false;
                                                    return false;
                                                }
                                                else
                                                {
                                                    logAIServiceAutoTrain.PageInfo = CONSTANTS.ModelTraining;
                                                    logAIServiceAutoTrain.Message = Message;
                                                    logAIServiceAutoTrain.Status = Status;
                                                    logAIServiceAutoTrain.Progress = Progress;
                                                    InsertAIServiceAutoTrainLog(logAIServiceAutoTrain);
                                                    Thread.Sleep(1000);
                                                    callMethod = true;
                                                }
                                            }
                                        }
                                        else if (Status == CONSTANTS.E)
                                        {
                                            logAIServiceAutoTrain.EndedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                                            logAIServiceAutoTrain.PageInfo = CONSTANTS.DataTransformation;
                                            logAIServiceAutoTrain.FunctionName = "CheckClusterPythonProcess";
                                            logAIServiceAutoTrain.Message = Message;
                                            logAIServiceAutoTrain.Status = Status;
                                            logAIServiceAutoTrain.Progress = Progress;
                                            InsertAIServiceAutoTrainLog(logAIServiceAutoTrain);
                                            callMethod = false;
                                            return false;
                                        }
                                        else
                                        {
                                            logAIServiceAutoTrain.PageInfo = CONSTANTS.DataTransformation;
                                            logAIServiceAutoTrain.FunctionName = "CheckClusterPythonProcess";
                                            logAIServiceAutoTrain.Message = Message;
                                            logAIServiceAutoTrain.Status = Status;
                                            logAIServiceAutoTrain.Progress = Progress;
                                            InsertAIServiceAutoTrainLog(logAIServiceAutoTrain);
                                            Thread.Sleep(1000);
                                            callMethod = true;
                                        }
                                    }
                                    else if (Status == CONSTANTS.E)
                                    {
                                        logAIServiceAutoTrain.EndedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                                        logAIServiceAutoTrain.PageInfo = CONSTANTS.DataTransformation;
                                        logAIServiceAutoTrain.FunctionName = "CheckClusterPythonProcess";
                                        logAIServiceAutoTrain.Message = Message;
                                        logAIServiceAutoTrain.Status = Status;
                                        logAIServiceAutoTrain.Progress = Progress;
                                        InsertAIServiceAutoTrainLog(logAIServiceAutoTrain);
                                        callMethod = false;
                                        return false;
                                    }
                                    else
                                    {
                                        logAIServiceAutoTrain.PageInfo = CONSTANTS.DataTransformation;
                                        logAIServiceAutoTrain.FunctionName = "CheckClusterPythonProcess";
                                        logAIServiceAutoTrain.Message = Message;
                                        logAIServiceAutoTrain.Status = Status;
                                        logAIServiceAutoTrain.Progress = Progress;
                                        InsertAIServiceAutoTrainLog(logAIServiceAutoTrain);
                                        Thread.Sleep(1000);
                                        callMethod = true;
                                    }
                                }
                                else if (Status == CONSTANTS.E)
                                {
                                    logAIServiceAutoTrain.EndedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                                    logAIServiceAutoTrain.PageInfo = CONSTANTS.DataCuration;
                                    logAIServiceAutoTrain.FunctionName = "CheckClusterPythonProcess";
                                    logAIServiceAutoTrain.Message = Message;
                                    logAIServiceAutoTrain.Status = Status;
                                    logAIServiceAutoTrain.Progress = Progress;
                                    InsertAIServiceAutoTrainLog(logAIServiceAutoTrain);
                                    callMethod = false;
                                    return false;
                                }
                                else
                                {
                                    logAIServiceAutoTrain.PageInfo = CONSTANTS.DataCuration;
                                    logAIServiceAutoTrain.FunctionName = "CheckClusterPythonProcess";
                                    logAIServiceAutoTrain.Message = Message;
                                    logAIServiceAutoTrain.Status = Status;
                                    logAIServiceAutoTrain.Progress = Progress;
                                    InsertAIServiceAutoTrainLog(logAIServiceAutoTrain);

                                    Thread.Sleep(1000);
                                    callMethod = true;
                                }                                
                            }
                        }
                    }
                }
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIServicesRetrain), nameof(ClusterServicesTraining), "END", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                return isIngestComplete;
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AIServicesRetrain), nameof(ClusterServicesTraining), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return false;
            }
        }

        private AIModels.Service GetAiCoreServiceDetails(string serviceid)
        {
            AIModels.Service service = new AIModels.Service();
            var serviceCollection = _database.GetCollection<BsonDocument>("Services");
            var filter = Builders<BsonDocument>.Filter.Eq("ServiceId", serviceid);
            var projection = Builders<BsonDocument>.Projection.Exclude("_id");
            var result = serviceCollection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                service = Newtonsoft.Json.JsonConvert.DeserializeObject<AIModels.Service>(result[0].ToJson());
            }

            return service;
        }
        #endregion

        #region Clustering Python Process
        private bool IngestPythonCluster(ClusteringAPIModel clustering)
        {
            bool isSuccess = false;
            JObject payload = new JObject();
            payload["CorrelationId"] = clustering.CorrelationId;
            payload["UniId"] = clustering.UniId;
            payload["UserId"] = clustering.CreatedBy;
            payload["pageInfo"] = clustering.PageInfo;
            payload["IsDataUpload"] = false;
            payload["Publish_Case"] = false;
            var response = new Response(clustering.ClientID, clustering.DCUID, clustering.ServiceID);
            var clusterbaseUrl = ClusteringPythonURL;
            string apiPath = CONSTANTS.Clustering_ModelTraining;
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIServicesRetrain), nameof(IngestPythonCluster), baseUrl + apiPath, string.IsNullOrEmpty(clustering.CorrelationId) ? default(Guid) : new Guid(clustering.CorrelationId), string.Empty, string.Empty, clustering.ClientID, clustering.DCUID);
            isSuccess = RoutePOSTRequest(string.Empty, new Uri(baseUrl), apiPath, payload, clustering.CorrelationId, false, true);
            return isSuccess;
        }
        private bool TrainPythonCluster(string correlationId, string uniId, string createdBy, string pageInfo)
        {
            bool isSuccess = false;
            JObject payload = new JObject();
            payload["CorrelationId"] = correlationId;
            payload["UniId"] = uniId;
            payload["UserId"] = createdBy;
            payload["pageInfo"] = pageInfo;
            payload["IsDataUpload"] = false;
            payload["Publish_Case"] = false;
            var clusterbaseUrl = ClusteringPythonURL;
            string apiPath = CONSTANTS.Clustering_ModelTraining;
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIServicesRetrain), nameof(IngestPythonCluster), clusterbaseUrl + apiPath, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);

            LogAIServiceAutoTrain logAIServiceAutoTrain = new LogAIServiceAutoTrain();
            logAIServiceAutoTrain.CorrelationId = correlationId;
            logAIServiceAutoTrain.PageInfo = pageInfo;
            logAIServiceAutoTrain.FunctionName = "TrainPythonCluster";
            logAIServiceAutoTrain.Message = "TrainPythonCluster Started URL:" + clusterbaseUrl + apiPath;
            logAIServiceAutoTrain.StartedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
            InsertAIServiceAutoTrainLog(logAIServiceAutoTrain);

            isSuccess = RoutePOSTRequest(string.Empty, new Uri(clusterbaseUrl), apiPath, payload, correlationId, false, false);
            return isSuccess;
        }
        public bool RoutePOSTRequest(string token, Uri baseUrl, string apiPath, JObject requestPayload, string correlationid, bool retrain, bool isIngest)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIServicesRetrain), nameof(RoutePOSTRequest), "START", string.IsNullOrEmpty(correlationid) ? default(Guid) : new Guid(correlationid), string.Empty, string.Empty, string.Empty, string.Empty);
            MethodReturn<object> returnValue = new MethodReturn<object>();
            bool isSuccess = false;

            if (Environment == "PAM")
            {
                token = _tokenService.GenerateVDSPAMToken(CONSTANTS.VDSApplicationID_PAM);
            }
            else
            {
                token = this.PythonAIServiceToken();
            }

            Task.Run(() =>
            {
                try
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIServicesRetrain), nameof(RoutePOSTRequest), "CLUSTERINGPYTHON CALL:" + baseUrl.ToString() + apiPath, string.IsNullOrEmpty(correlationid) ? default(Guid) : new Guid(correlationid), string.Empty, string.Empty, string.Empty, string.Empty);
                    StringContent content = new StringContent(requestPayload.ToString(), Encoding.UTF8, "application/json");
                    HttpResponseMessage message = InvokeClusterRequest(token, baseUrl.ToString(), apiPath, content);
                    if (message.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIServicesRetrain), nameof(RoutePOSTRequest), message.Content.ReadAsStringAsync().Result, string.IsNullOrEmpty(correlationid) ? default(Guid) : new Guid(correlationid), string.Empty, string.Empty, string.Empty, string.Empty);
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIServicesRetrain), nameof(RoutePOSTRequest), "CorrelationId : " + correlationid + " ClusterAPI HTTPSTATUS : " + message.StatusCode, string.IsNullOrEmpty(correlationid) ? default(Guid) : new Guid(correlationid), string.Empty, string.Empty, string.Empty, string.Empty);
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIServicesRetrain), nameof(RoutePOSTRequest), "END", string.IsNullOrEmpty(correlationid) ? default(Guid) : new Guid(correlationid), string.Empty, string.Empty, string.Empty, string.Empty);
                        isSuccess = true;
                    }
                    else
                    {
                        isSuccess = false;
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIServicesRetrain), nameof(RoutePOSTRequest), "CorrelationId : " + correlationid + " ClusterAPI : " + "Python error:" + message.ReasonPhrase + " - " + message.StatusCode, string.IsNullOrEmpty(correlationid) ? default(Guid) : new Guid(correlationid), string.Empty, string.Empty, string.Empty, string.Empty);
                    }
                }
                catch (Exception ex)
                {
                    isSuccess = false;
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIServicesRetrain), nameof(RoutePOSTRequest), "CorrelationId : " + correlationid + " ClusterAPI : Python API call error" + ex.Message, string.Empty, string.Empty, string.Empty, string.Empty);
                }
            });
            return true;
        }
        private HttpResponseMessage InvokeClusterRequest(string token, string baseURI, string apiPath, StringContent content)
        {
            using (var client = new HttpClient())
            {
                string uri = baseURI + apiPath;
                client.Timeout = new TimeSpan(0, 30, 0);
                //client.BaseAddress = new Uri(baseURI);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                if (!string.IsNullOrWhiteSpace(token))
                {
                    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                }
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                return client.PostAsync(uri, content).Result;
            }
        }
        #endregion

        #region PythonProgressCheck
        private string CheckPythonProcess(string correlationId, string pageInfo)
        {
            string userModel = string.Empty;
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.AIServiceRequestStatus);
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq(CONSTANTS.CorrelationId, correlationId) & builder.Eq(CONSTANTS.PageInfo, pageInfo);
            var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.Status).Include(CONSTANTS.Progress).
            Include(CONSTANTS.CorrelationId).Include(CONSTANTS.PageInfo).Include(CONSTANTS.Message).Include(CONSTANTS.UniId).Exclude(CONSTANTS.Id);
            var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                userModel = result[0].ToJson();
            }
            return userModel;
        }
        private void InsertRequests(AIRequestStatus ingrainRequest)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIServicesRetrain), nameof(InsertRequests), "AISERVICES InsertRequests--START", string.IsNullOrEmpty(ingrainRequest.CorrelationId) ? default(Guid) : new Guid(ingrainRequest.CorrelationId), string.Empty, string.Empty, string.Empty, string.Empty);
            var requestQueue = Newtonsoft.Json.JsonConvert.SerializeObject(ingrainRequest);
            var insertRequestQueue = BsonSerializer.Deserialize<BsonDocument>(requestQueue);
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.AIServiceRequestStatus);
            collection.InsertOne(insertRequestQueue);
        }
        private void InsertRequests(BsonDocument ingrainRequest)
        {
            //var requestQueue = Newtonsoft.Json.JsonConvert.SerializeObject(ingrainRequest);
            var insertRequestQueue = BsonSerializer.Deserialize<BsonDocument>(ingrainRequest);
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.AIServiceRequestStatus);
            collection.InsertOne(insertRequestQueue);
        }
        private void InsertRequests(ClusteringStatus ingrainRequest)
        {
            var requestQueue = Newtonsoft.Json.JsonConvert.SerializeObject(ingrainRequest);
            var insertRequestQueue = BsonSerializer.Deserialize<BsonDocument>(requestQueue);
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.ClusteringStatusTable);
            collection.InsertOne(insertRequestQueue);
        }
        private void RemoveQueueRecords(string correlationId, string pageInfo)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIServicesRetrain), nameof(RemoveQueueRecords), "AISERVICES RemoveQueueRecords--START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            var builder = Builders<BsonDocument>.Filter;
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.AIServiceRequestStatus);
            var filter = builder.Eq(CONSTANTS.CorrelationId, correlationId) & builder.Eq(CONSTANTS.PageInfo, pageInfo);
            var result = collection.DeleteMany(filter);
        }
        private void RemoveClusterQueueRecords(string correlationId, string pageInfo)
        {
            var builder = Builders<BsonDocument>.Filter;
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.ClusteringStatusTable);
            var filter = builder.Eq(CONSTANTS.CorrelationId, correlationId) & builder.Eq(CONSTANTS.pageInfo, pageInfo);
            var result = collection.DeleteMany(filter);
        }
        private string CheckClusterPythonProcess(string correlationId, string pageInfo)
        {
            string userModel = string.Empty;
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.Clustering_StatusTable);
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq(CONSTANTS.CorrelationId, correlationId) & builder.Eq(CONSTANTS.pageInfo, pageInfo);
            var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.Status).Include(CONSTANTS.Progress).
            Include(CONSTANTS.CorrelationId).Include(CONSTANTS.pageInfo).Include(CONSTANTS.Message).Include(CONSTANTS.UniId).Exclude(CONSTANTS.Id);
            var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                userModel = result[0].ToJson();
            }
            return userModel;
        }

        private void InsertClusteringRequest(BsonDocument ingrainRequest)
        {
            var insertRequestQueue = BsonSerializer.Deserialize<BsonDocument>(ingrainRequest);
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.Clustering_IngestData);
            collection.InsertOne(insertRequestQueue);
        }

        private void RemoveClusterIngestData(string correlationId, string pageInfo)
        {
            var builder = Builders<BsonDocument>.Filter;
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.Clustering_IngestData);
            var filter = builder.Eq(CONSTANTS.CorrelationId, correlationId) & builder.Eq(CONSTANTS.pageInfo, pageInfo);
            var result = collection.DeleteMany(filter);
        }

        private void UpdateExistingCLusterRecords(string correlationId, string pageInfo)
        {
            var filter2 = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            string updateString = "_backup";
            var updateTrainedModels = Builders<BsonDocument>.Update;
            var updateModels = updateTrainedModels.Set(CONSTANTS.CorrelationId, correlationId + updateString);
            var trainedModelsCollection = _database.GetCollection<BsonDocument>(CONSTANTS.Clustering_IngestData);
            var modelResult = trainedModelsCollection.UpdateMany(filter2, updateModels);
        }


        #endregion
    }
}
