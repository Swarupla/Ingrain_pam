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
using Accenture.MyWizard.Ingrain.BusinessDomain.Common;
using Accenture.MyWizard.Cryptography.EncryptionProviders;
using RestSharp;
using System.Text;
using System.Net;
using System.Net.Http.Headers;
using CryptographyHelper = Accenture.MyWizard.Cryptography;

namespace Accenture.MyWizard.SelfServiceAI.WindowService.Service
{
    public class InstaAutoRetrainService : IInstaAutoRetrainService
    {
        #region Private Members       
        private PreProcessDTO _preProcessDTO;
        private readonly DatabaseProvider databaseProvider;
        private MongoClient _mongoClient;
        private IMongoDatabase _database;
        private IMongoCollection<IngrainRequestQueue> _collection;
        private IMongoCollection<BsonDocument> _deployModelCollection;
        private InstaRetrain _instaRetrain;
        bool insertSuccess = false;
        private RecommedAITrainedModel _recommendedAI;
        private DeployModelViewModel _deployModelViewModel;
        private IConfigurationRoot appSettings;
        private string source;
        private string _aesKey;
        private string _aesVector;
        private bool _IsAESKeyVault;
        static CryptographyHelper.Utility.CryptographyUtility CryptographyUtility = null;
        private string _myWizardAPIUrl;

        private string _authProvider;
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
        #endregion
        public InstaAutoRetrainService(DatabaseProvider db)
        {
            databaseProvider = db;
            appSettings = AppSettingsJson.GetAppSettings();
            _mongoClient = databaseProvider.GetDatabaseConnection();
            var dataBaseName = MongoUrl.Create(appSettings.GetSection("AppSettings").GetSection("connectionString").Value).DatabaseName;
            _database = _mongoClient.GetDatabase(dataBaseName);
            _collection = _database.GetCollection<IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
            _deployModelCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
            _instaRetrain = new InstaRetrain();
            _preProcessDTO = new PreProcessDTO();
            _recommendedAI = new RecommedAITrainedModel();
            _deployModelViewModel = new DeployModelViewModel();
            source = appSettings.GetSection("AppSettings").GetSection("Source").Value;
            _aesKey = appSettings.GetSection("AppSettings").GetSection("aesKey").Value;
            _aesVector = appSettings.GetSection("AppSettings").GetSection("aesVector").Value;
            _myWizardAPIUrl = appSettings.GetSection("AppSettings").GetSection("myWizardAPIUrl").Value;

            _token_Url = appSettings.GetSection("AppSettings").GetSection("tokenAPIUrl").Value;
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

            _IsAESKeyVault = Convert.ToBoolean(appSettings.GetSection("AppSettings").GetSection("IsAESKeyVault").Value);
            CryptographyUtility = new CryptographyHelper.Utility.CryptographyUtility();
        }
        public InstaRetrain IngestData(BsonDocument result)
        {
            try
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaAutoRetrainService), CONSTANTS.IngestData, "InstaAutoRetrainService START", string.IsNullOrEmpty(Convert.ToString(result[CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[CONSTANTS.CorrelationId])), string.Empty, string.Empty, string.Empty, string.Empty);
                string entityitems = CONSTANTS.Null;
                var metricitems = CONSTANTS.Null;
                Filepath filepath = new Filepath();
                filepath.fileList = CONSTANTS.Null;
                IngrainRequestQueue ingrainRequest = null;
                IngrainRequestQueue bsonElements = new IngrainRequestQueue();
                if (result.Contains("SourceName"))
                {
                    if (result["SourceName"].ToString() == "pad" || result["SourceName"].ToString() == "metric" || result["DataSource"].ToString() == CONSTANTS.Custom || result["SourceName"].ToString() == CONSTANTS.Custom)
                    {
                        bsonElements = GetRequestDetails(result[CONSTANTS.CorrelationId].ToString());
                        if (bsonElements.ParamArgs != null)
                        {
                            string appId = null;
                            string templateUsecaseId = null;
                            if (result.Contains("AppId"))
                            {
                                appId = result["AppId"].ToString() != "BsonNull" ? result["AppId"].ToString() : null;
                            }
                            if (result.Contains("TemplateUsecaseId"))
                            {
                                templateUsecaseId = result["TemplateUsecaseId"].ToString() != "BsonNull" ? result["TemplateUsecaseId"].ToString() : null;
                            }
                            ingrainRequest = new IngrainRequestQueue
                            {
                                _id = Guid.NewGuid().ToString(),
                                CorrelationId = result[CONSTANTS.CorrelationId].ToString(),
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
                                UseCaseID = CONSTANTS.Null,
                                AppID = appId,
                                TemplateUseCaseID = templateUsecaseId,
                                pageInfo = CONSTANTS.IngestData,
                                //ParamArgs = aa.ToJson(),
                                Function = "FileUpload",
                                CreatedByUser = result[CONSTANTS.CreatedByUser].ToString(),
                                CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                                ModifiedByUser = result[CONSTANTS.CreatedByUser].ToString(),
                                ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                                LastProcessedOn = null,
                            };
                            if (result["SourceName"].ToString() == CONSTANTS.Custom && (result["LinkedApps"][0].ToString() == CONSTANTS.VDS_SI || result["LinkedApps"][0].ToString() == CONSTANTS.VDS_AIOPS) && result["DataSource"].ToString() == "Phoenix CDM")
                            {
                                //LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaAutoRetrainService), CONSTANTS.IngestData, "AGILE - GENERIC FORM PARAM ARG START");
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaAutoRetrainService), CONSTANTS.IngestData, "GENERIC API MODEL INSERTION START", string.IsNullOrEmpty(Convert.ToString(result[CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[CONSTANTS.CorrelationId])), appId, string.Empty, string.Empty, string.Empty);
                                var fileParams = JsonConvert.DeserializeObject<AgileFileUploadCustom>(bsonElements.ParamArgs);
                                fileParams.Flag = source;
                                fileParams.Customdetails.InputParameters.FromDate = DateTime.Parse(result["ModifiedOn"].ToString()).ToString(CONSTANTS.DateHoursFormat);
                                fileParams.Customdetails.InputParameters.ToDate = DateTime.UtcNow.ToString(CONSTANTS.DateHoursFormat);
                                fileParams.Customdetails.AICustom = CONSTANTS.Null;
                                var data = BsonDocument.Parse(JsonConvert.SerializeObject(fileParams));
                                //LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaAutoRetrainService), CONSTANTS.IngestData, "AGILE - PARAM ARG END & MODEL INSERTION START--" + data);
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaAutoRetrainService), CONSTANTS.IngestData, "GENERIC API MODEL INSERTION START--" + data, string.IsNullOrEmpty(Convert.ToString(result[CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[CONSTANTS.CorrelationId])), appId, string.Empty, string.Empty, string.Empty);
                                ingrainRequest.ParamArgs = data.ToJson();
                            }
                            else if (result["SourceName"].ToString() == CONSTANTS.Custom && (result["LinkedApps"][0].ToString() == CONSTANTS.VDS_AIOPS || result["LinkedApps"][0].ToString() == CONSTANTS.VDS_SI || result["LinkedApps"][0].ToString() == CONSTANTS.VDS))
                            {
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaAutoRetrainService), CONSTANTS.IngestData, "GENERIC API MODEL INSERTION START", string.IsNullOrEmpty(Convert.ToString(result[CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[CONSTANTS.CorrelationId])), appId, string.Empty, string.Empty, string.Empty);
                                var fileParams = JsonConvert.DeserializeObject<CustomUploadFile>(bsonElements.ParamArgs);
                                fileParams.Flag = source;
                                //fileParams.Customdetails.InputParameters.StartDate = GetLastDataDict(result[CONSTANTS.CorrelationId].ToString());
                                fileParams.Customdetails.InputParameters.StartDate = DateTime.Parse(result["ModifiedOn"].ToString()).ToString(CONSTANTS.DateHoursFormat);
                                fileParams.Customdetails.InputParameters.EndDate = DateTime.UtcNow.ToString(CONSTANTS.DateHoursFormat);
                                var data = BsonDocument.Parse(JsonConvert.SerializeObject(fileParams));
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaAutoRetrainService), CONSTANTS.IngestData, "GENERIC API MODEL INSERTION START--" + data, string.IsNullOrEmpty(Convert.ToString(result[CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[CONSTANTS.CorrelationId])), appId, string.Empty, string.Empty, string.Empty);
                                ingrainRequest.ParamArgs = data.ToJson();
                            }
                            else if (result["DataSource"].ToString() == CONSTANTS.Custom && (result["LinkedApps"][0].ToString() != CONSTANTS.VDS_AIOPS || result["LinkedApps"][0].ToString() != CONSTANTS.VDS_SI || result["LinkedApps"][0].ToString() != CONSTANTS.VDS))
                            {
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaAutoRetrainService), CONSTANTS.IngestData, "GENERIC API MODEL INSERTION START", string.IsNullOrEmpty(Convert.ToString(result[CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[CONSTANTS.CorrelationId])), appId, string.Empty, string.Empty, string.Empty);
                                var fileParams = JsonConvert.DeserializeObject<AutoTrainFileUpload>(bsonElements.ParamArgs);
                                fileParams.Flag = source;
                                fileParams.Customdetails.InputParameters.DateColumn = GetLastDataDict(result[CONSTANTS.CorrelationId].ToString());
                                var data = BsonDocument.Parse(JsonConvert.SerializeObject(fileParams));
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaAutoRetrainService), CONSTANTS.IngestData, "GENERIC API MODEL INSERTION START--" + data, string.IsNullOrEmpty(Convert.ToString(result[CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[CONSTANTS.CorrelationId])), appId, string.Empty, string.Empty, string.Empty);
                                ingrainRequest.ParamArgs = data.ToJson();
                            }
                            else if (result["TemplateUsecaseId"].ToString() == CONSTANTS.BsonNull || string.IsNullOrEmpty(result["TemplateUsecaseId"].ToString()))
                            {
                                var fileUpload = JsonConvert.DeserializeObject<FileUpload>(bsonElements.ParamArgs);
                                fileUpload.Customdetails = CONSTANTS.Null;
                                fileUpload.Flag = source;
                                ingrainRequest.ParamArgs = fileUpload.ToJson();
                            }
                            else if ((result["SourceName"].ToString() == "pad" || result["SourceName"].ToString() == "metric") & result["TemplateUsecaseId"].ToString() != null)
                            {
                                var fileUpload = JsonConvert.DeserializeObject<FileUpload>(bsonElements.ParamArgs);
                                fileUpload.Customdetails = CONSTANTS.Null;
                                fileUpload.Flag = source;
                                ingrainRequest.ParamArgs = fileUpload.ToJson();
                            }
                            GetIngestData(result, ingrainRequest);
                        }
                    }
                    else
                    {
                        if (result.Contains("InstaId"))
                        {
                            if (Convert.ToString(result["InstaId"]) != null)
                            {
                                ingrainRequest = new IngrainRequestQueue
                                {
                                    _id = Guid.NewGuid().ToString(),
                                    CorrelationId = result[CONSTANTS.CorrelationId].ToString(),
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
                                    pageInfo = CONSTANTS.IngestData,
                                    ParamArgs = null,
                                    Function = "FileUpload",
                                    CreatedByUser = result[CONSTANTS.CreatedByUser].ToString(),
                                    CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                                    ModifiedByUser = result[CONSTANTS.CreatedByUser].ToString(),
                                    ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                                    LastProcessedOn = null,
                                };
                                if (result.Contains("UseCaseID"))
                                {
                                    ingrainRequest.UseCaseID = result[CONSTANTS.UseCaseID].ToString() == "BsonNull" ? CONSTANTS.Null : result[CONSTANTS.UseCaseID].ToString();
                                }
                                else
                                {
                                    ingrainRequest.UseCaseID = CONSTANTS.Null;
                                }
                                string src = string.Empty;
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaAutoRetrainService), nameof(IngestData), Convert.ToString(result["LinkedApps"][0]), string.Empty, string.Empty, string.Empty, string.Empty);
                                if (result["LinkedApps"][0].ToString() == CONSTANTS.VDS_SI)
                                {
                                    //src = CONSTANTS.VDS_AIOPS;
                                    src = CONSTANTS.VDS_SI;
                                }

                                InstaPayload instaPayload = new InstaPayload
                                {
                                    InstaId = result[CONSTANTS.InstaId].ToString(),
                                    CorrelationId = result[CONSTANTS.CorrelationId].ToString(),
                                    Dimension = "Date",
                                    TargetColumn = CONSTANTS.Value,
                                    ProblemType = CONSTANTS.TimeSeries,
                                    UseCaseId = CONSTANTS.Null,
                                    Source = src
                                };
                                List<InstaPayload> instaPayloads = new List<InstaPayload>();
                                instaPayloads.Add(instaPayload);
                                ParentFile parentFile = new ParentFile();
                                parentFile.Type = CONSTANTS.Null;
                                parentFile.Name = CONSTANTS.Null;
                                string MappingColumns = string.Empty;
                                InstaMLFileUpload fileUpload = new InstaMLFileUpload
                                {
                                    CorrelationId = result[CONSTANTS.CorrelationId].ToString(),
                                    ClientUID = result[CONSTANTS.ClientUId].ToString(),
                                    DeliveryConstructUId = result[CONSTANTS.DeliveryConstructUID].ToString(),
                                    Parent = parentFile,
                                    Flag = source,
                                    mapping = MappingColumns,
                                    mapping_flag = CONSTANTS.False,
                                    pad = entityitems,
                                    metric = metricitems,
                                    InstaMl = instaPayloads,
                                    fileupload = filepath,
                                    Customdetails = CONSTANTS.Null
                                };
                                ingrainRequest.ParamArgs = fileUpload.ToJson();
                                GetIngestData(result, ingrainRequest);
                            }
                        }
                    }
                }
                else
                {
                    if (result.Contains("InstaId"))
                    {
                        if (Convert.ToString(result["InstaId"]) != null)
                        {
                            ingrainRequest = new IngrainRequestQueue
                            {
                                _id = Guid.NewGuid().ToString(),
                                CorrelationId = result[CONSTANTS.CorrelationId].ToString(),
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
                                pageInfo = CONSTANTS.IngestData,
                                ParamArgs = null,
                                Function = "FileUpload",
                                CreatedByUser = result[CONSTANTS.CreatedByUser].ToString(),
                                CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                                ModifiedByUser = result[CONSTANTS.CreatedByUser].ToString(),
                                ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                                LastProcessedOn = null,
                            };
                            if (result.Contains("UseCaseID"))
                            {
                                ingrainRequest.UseCaseID = result[CONSTANTS.UseCaseID].ToString() == "BsonNull" ? CONSTANTS.Null : result[CONSTANTS.UseCaseID].ToString();
                            }
                            else
                            {
                                ingrainRequest.UseCaseID = CONSTANTS.Null;
                            }
                            string src = string.Empty;
                            if (result["LinkedApps"][0].ToString() == CONSTANTS.VDS_AIOPS)
                            {
                                src = CONSTANTS.VDS_AIOPS;
                            }

                            InstaPayload instaPayload = new InstaPayload
                            {
                                InstaId = result[CONSTANTS.InstaId].ToString(),
                                CorrelationId = result[CONSTANTS.CorrelationId].ToString(),
                                Dimension = "Date",
                                TargetColumn = CONSTANTS.Value,
                                ProblemType = CONSTANTS.TimeSeries,
                                UseCaseId = CONSTANTS.Null,
                                Source = src
                            };
                            List<InstaPayload> instaPayloads = new List<InstaPayload>();
                            instaPayloads.Add(instaPayload);
                            ParentFile parentFile = new ParentFile();
                            parentFile.Type = CONSTANTS.Null;
                            parentFile.Name = CONSTANTS.Null;
                            string MappingColumns = string.Empty;
                            InstaMLFileUpload fileUpload = new InstaMLFileUpload
                            {
                                CorrelationId = result[CONSTANTS.CorrelationId].ToString(),
                                ClientUID = result[CONSTANTS.ClientUId].ToString(),
                                DeliveryConstructUId = result[CONSTANTS.DeliveryConstructUID].ToString(),
                                Parent = parentFile,
                                Flag = source,
                                mapping = MappingColumns,
                                mapping_flag = CONSTANTS.False,
                                pad = entityitems,
                                metric = metricitems,
                                InstaMl = instaPayloads,
                                fileupload = filepath,
                                Customdetails = CONSTANTS.Null
                            };
                            ingrainRequest.ParamArgs = fileUpload.ToJson();
                            GetIngestData(result, ingrainRequest);
                        }
                    }
                }

                LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaAutoRetrainService), CONSTANTS.IngestData, "InstaAutoRetrainService END - ", string.IsNullOrEmpty(Convert.ToString(result[CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[CONSTANTS.CorrelationId])), string.Empty, string.Empty, string.Empty, string.Empty);
                return _instaRetrain;
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InstaAutoRetrainService), CONSTANTS.IngestData, "InstaAutoRetrainService END CORRELATIONID-- " + Convert.ToString(result[CONSTANTS.CorrelationId]), ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return _instaRetrain;
            }

        }

        private string GetLastDataDict(string CorrelationId)
        {
            string lastDateDict = string.Empty;
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.PS_IngestedData);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, CorrelationId);
            var Projection = Builders<BsonDocument>.Projection.Include("lastDateDict").Exclude("_id");
            var Ingestdata = collection.Find(filter).Project<BsonDocument>(Projection).ToList();
            if (Ingestdata.Count > 0)
            {
                if (Ingestdata[0].Contains("lastDateDict") && Ingestdata[0]["lastDateDict"].ToString() != "{ }")
                {
                    var lastpulleddate = Ingestdata[0]["lastDateDict"]["Custom"].ToString();
                    if (lastpulleddate.Contains("DateColumn"))
                    {
                        lastDateDict = Ingestdata[0]["lastDateDict"]["Custom"]["DateColumn"].ToString();
                    }
                    else if (lastpulleddate.Contains("StartOn"))
                    {
                        lastDateDict = Ingestdata[0]["lastDateDict"]["Custom"]["StartOn"].ToString();
                    }
                }
            }
            return lastDateDict;
        }

        private void GetIngestData(BsonDocument result, IngrainRequestQueue ingrainRequest)
        {
            //Take back up of ingestData request
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
            var builder = Builders<BsonDocument>.Filter;
            DeleteIngestRequest(ingrainRequest.CorrelationId + "_backup");
            Thread.Sleep(1000);
            var filter = builder.Eq(CONSTANTS.CorrelationId, ingrainRequest.CorrelationId) & builder.Eq(CONSTANTS.pageInfo, CONSTANTS.IngestData);
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaAutoRetrainService), "GetIngestData", "INSERTING REQUEST --" + ingrainRequest.CorrelationId, string.IsNullOrEmpty(ingrainRequest.CorrelationId) ? default(Guid) : new Guid(ingrainRequest.CorrelationId), ingrainRequest.AppID, "result.CorId:" + Convert.ToString(result[CONSTANTS.CorrelationId].ToString()) + Convert.ToString(ingrainRequest.RequestStatus) + Convert.ToString(ingrainRequest.Status), ingrainRequest.ClientID, ingrainRequest.DeliveryconstructId);
            var update = Builders<BsonDocument>.Update.Set(CONSTANTS.CorrelationId, ingrainRequest.CorrelationId + "_backup");
            var updateResult = collection.UpdateOne(filter, update);
            //back up end
            DeleteIngestRequest(result[CONSTANTS.CorrelationId].ToString());
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaAutoRetrainService), "GetIngestData", "INSERTING REQUEST --" + ingrainRequest.CorrelationId, string.IsNullOrEmpty(ingrainRequest.CorrelationId) ? default(Guid) : new Guid(ingrainRequest.CorrelationId), ingrainRequest.AppID, string.Empty, ingrainRequest.ClientID, ingrainRequest.DeliveryconstructId);
            InsertRequests(ingrainRequest);
            Thread.Sleep(2000);
            bool flag = true;
            while (flag)
            {
                IngrainRequestQueue requestQueue = new IngrainRequestQueue();
                requestQueue = GetFileRequestStatus(result[CONSTANTS.CorrelationId].ToString(), CONSTANTS.IngestData);
                if (requestQueue != null)
                {
                    if (requestQueue.Status == CONSTANTS.C & requestQueue.Progress == CONSTANTS.Hundred)
                    {
                        flag = false;
                        var instaLog = Log(requestQueue, result);
                        InsertAutoLog(instaLog);
                        _instaRetrain.Status = requestQueue.Status;
                        _instaRetrain.Success = true;
                        _instaRetrain.Message = requestQueue.Message;
                    }
                    else if (requestQueue.Status == "E")
                    {
                        flag = false;
                        var instaLog = Log(requestQueue, result);
                        InsertAutoLog(instaLog);
                        _instaRetrain.Status = requestQueue.Status;
                        _instaRetrain.Success = false;
                        _instaRetrain.ErrorMessage = requestQueue.Message;
                    }
                    else if (requestQueue.Status == "I")
                    {
                        //flag = false;
                        _instaRetrain.Status = requestQueue.Status;
                        _instaRetrain.Success = true;
                    }
                    else
                    {
                        //flag = true;
                        Thread.Sleep(1000);
                    }
                }
                else
                {
                    flag = true;
                    Thread.Sleep(1000);
                }
            }
        }
        public InstaRetrain GetInstaAutoDataEngineering(BsonDocument result)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaAutoRetrainService), "GETINSTAAUTODATAENGINEERING", "GETINSTAAUTODATAENGINEERING START", string.IsNullOrEmpty(Convert.ToString(result[CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[CONSTANTS.CorrelationId])), string.Empty, string.Empty, string.Empty, string.Empty);
            bool isDataCurationCompleted = false;
            bool isDataTransformationCompleted = false;
            DataEngineering dataEngineering = new DataEngineering();
            try
            {
                dataEngineering = GetDataCuration(result[CONSTANTS.CorrelationId].ToString(), CONSTANTS.DataCleanUp, result[CONSTANTS.CreatedByUser].ToString(), result);
                _instaRetrain.Status = dataEngineering.Status;
                _instaRetrain.Message = dataEngineering.Message;
                if (dataEngineering.Status == CONSTANTS.C)
                {
                    isDataCurationCompleted = IsDataCurationComplete(result[CONSTANTS.CorrelationId].ToString());
                }
                if (isDataCurationCompleted)
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaAutoRetrainService), "DATACURATION", "DATACURATION COMPLETED-", string.IsNullOrEmpty(Convert.ToString(result[CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[CONSTANTS.CorrelationId])), string.Empty, string.Empty, string.Empty, string.Empty);
                    PreProcessModelDTO preProcessModelDTO = new PreProcessModelDTO();
                    isDataTransformationCompleted = CreatePreprocess(result[CONSTANTS.CorrelationId].ToString(), result[CONSTANTS.CreatedByUser].ToString(), CONSTANTS.TimeSeries, result[CONSTANTS.InstaId].ToString());
                    if (isDataTransformationCompleted)
                    {
                        RemoveQueueRecords(result[CONSTANTS.CorrelationId].ToString(), CONSTANTS.DataPreprocessing);
                        dataEngineering = GetDatatransformation(result[CONSTANTS.CorrelationId].ToString(), CONSTANTS.DataPreprocessing, result[CONSTANTS.CreatedByUser].ToString(), result);
                        _instaRetrain.Status = dataEngineering.Status;
                        _instaRetrain.Message = dataEngineering.Message;
                        if (dataEngineering.Status == CONSTANTS.C)
                        {
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaAutoRetrainService), "INSTA DATATRANSFORMATION", "DATATRANSFORMATION END", string.IsNullOrEmpty(Convert.ToString(result[CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[CONSTANTS.CorrelationId])), string.Empty, string.Empty, string.Empty, string.Empty);
                            _instaRetrain.Status = dataEngineering.Status;
                            _instaRetrain.Success = true;

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InstaAutoRetrainService), nameof(GetInstaAutoDataEngineering), ex.Message + "- STACKTRACE - " + ex.StackTrace + "CorrelationId-" + (string.IsNullOrEmpty(Convert.ToString(result[CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[CONSTANTS.CorrelationId]))), ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaAutoRetrainService), nameof(GetInstaAutoDataEngineering), "END", string.IsNullOrEmpty(Convert.ToString(result[CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[CONSTANTS.CorrelationId])), string.Empty, string.Empty, string.Empty, string.Empty);
            return _instaRetrain;
        }
        public InstaRetrain GetInstaAutoModelEngineering(BsonDocument result)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaAutoRetrainService), nameof(GetInstaAutoModelEngineering), "START", string.IsNullOrEmpty(Convert.ToString(result[CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[CONSTANTS.CorrelationId])), string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                //int logCount = 0;
                int errorCount = 0;
                int noOfModelsSelected = Convert.ToInt32(appSettings.GetSection("AppSettings").GetSection("Insta_AutoModels").Value);
                string pythonResult = string.Empty;
                bool isModelTrained = true;
                IngrainRequestQueue ingrainRequest = new IngrainRequestQueue();
                //taking the backup                
                updateExistingModels(result[CONSTANTS.CorrelationId].ToString(), CONSTANTS.RecommendedAI);
                RemoveQueueRecords(result[CONSTANTS.CorrelationId].ToString(), CONSTANTS.RecommendedAI);
                //invoke python
                while (isModelTrained)
                {
                    int modelsCount = 0;
                ExecuteQueueTable:
                    var useCaseDetails = GetMultipleRequestStatus(result[CONSTANTS.CorrelationId].ToString(), CONSTANTS.RecommendedAI);
                    if (useCaseDetails.Count > 0)
                    {
                        for (int i = 0; i < useCaseDetails.Count; i++)
                        {
                            string queueStatus = useCaseDetails[i].Status;
                            if (queueStatus == CONSTANTS.C)
                            {
                                modelsCount++;
                            }
                            if (queueStatus == "E")
                            {
                                errorCount++;
                            }
                        }
                        if (errorCount > 1)
                        {
                            _instaRetrain.Status = "E";
                            _instaRetrain.Success = false;
                            _instaRetrain.PageInfo = CONSTANTS.RecommendedAI;
                            //Delete the whatever trainedmodels and pickle files
                            DeleteGenerateModels(result[CONSTANTS.CorrelationId].ToString(), CONSTANTS.RecommendedAI);
                            //revert the model changes..
                            RevertModelChanges(result);
                            isModelTrained = false;
                        }
                        if (modelsCount >= noOfModelsSelected)
                        {
                            //If, All models success than delete the backup files.
                            DeleteBackupRecords(result);
                            _instaRetrain.Status = CONSTANTS.C;
                            _instaRetrain.Success = false;
                            _instaRetrain.PageInfo = CONSTANTS.RecommendedAI;
                            isModelTrained = false;
                        }
                        if (errorCount < 2 && modelsCount <= 2)
                        {
                            modelsCount = 0;
                            errorCount = 0;
                            isModelTrained = true;
                            Thread.Sleep(3000);
                            goto ExecuteQueueTable;
                        }
                        Thread.Sleep(1000);
                    }
                    else
                    {
                        IngrainRequestQueue requestQueue = new IngrainRequestQueue();
                        var recommendedModels = GetModelNames(result[CONSTANTS.CorrelationId].ToString());
                        foreach (var modelName in recommendedModels.Item1)
                        {
                            ingrainRequest._id = Guid.NewGuid().ToString();
                            ingrainRequest.CorrelationId = result[CONSTANTS.CorrelationId].ToString();
                            ingrainRequest.RequestId = Guid.NewGuid().ToString();
                            ingrainRequest.ProcessId = null;
                            ingrainRequest.Status = null;
                            ingrainRequest.ModelName = modelName;
                            ingrainRequest.RequestStatus = CONSTANTS.New;
                            ingrainRequest.RetryCount = 0;
                            ingrainRequest.ProblemType = recommendedModels.Item2;
                            ingrainRequest.Message = null;
                            ingrainRequest.UniId = null;
                            ingrainRequest.Progress = null;
                            ingrainRequest.pageInfo = CONSTANTS.RecommendedAI;
                            ingrainRequest.ParamArgs = "{}";
                            ingrainRequest.Function = CONSTANTS.RecommendedAI;
                            ingrainRequest.CreatedByUser = result[CONSTANTS.CreatedByUser].ToString();
                            ingrainRequest.CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                            ingrainRequest.ModifiedByUser = result[CONSTANTS.CreatedByUser].ToString();
                            ingrainRequest.ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                            ingrainRequest.LastProcessedOn = null;
                            InsertRequests(ingrainRequest);
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaAutoRetrainService), "AUTO TRAIN INSERT REQUEST-" + ingrainRequest.CorrelationId, "END", string.IsNullOrEmpty(Convert.ToString(result[CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[CONSTANTS.CorrelationId])), string.Empty, string.Empty, string.Empty, string.Empty);
                        }
                        Thread.Sleep(2000);
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InstaAutoRetrainService), nameof(GetInstaAutoModelEngineering), ex.Message + "- STACKTRACE - " + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaAutoRetrainService), nameof(GetInstaAutoModelEngineering), "END", string.IsNullOrEmpty(Convert.ToString(result[CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[CONSTANTS.CorrelationId])), string.Empty, string.Empty, string.Empty, string.Empty);
            return _instaRetrain;
        }
        public InstaRetrain GetInstaAutoDeployPrediction(BsonDocument result)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaAutoRetrainService), nameof(GetInstaAutoDeployPrediction), "START", string.IsNullOrEmpty(Convert.ToString(result[CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[CONSTANTS.CorrelationId])), string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                var dataEngineering = InstaAutoDeployModel(result[CONSTANTS.CorrelationId].ToString(), result[CONSTANTS.CreatedByUser].ToString(), result["ModelType"].ToString());
                if (dataEngineering.Status == CONSTANTS.C)
                {
                    _instaRetrain.Status = CONSTANTS.C;
                    _instaRetrain.Success = true;
                    bool isSucess = InstaAutoPrediction(result[CONSTANTS.CorrelationId].ToString(), result[CONSTANTS.CreatedByUser].ToString());
                    if (isSucess)
                    {
                        _instaRetrain.Status = CONSTANTS.C;
                        _instaRetrain.IsPredictionSucess = true;
                        _instaRetrain.Success = true;
                        _instaRetrain.Message = "Prediction completed Successfully";
                    }
                    else
                    {
                        _instaRetrain.Status = "E";
                        _instaRetrain.IsPredictionSucess = false;
                        _instaRetrain.Message = "Prediction failed";
                        _instaRetrain.Success = false;
                    }

                }
                else
                {
                    _instaRetrain.Status = "E";
                    _instaRetrain.Message = "DeployModel Failed";
                    _instaRetrain.Success = false;
                    _instaRetrain.ErrorMessage = "Deploymodel Failed";
                    return _instaRetrain;
                }

            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InstaAutoRetrainService), nameof(GetInstaAutoDeployPrediction), ex.Message + "- STACKTRACE - " + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaAutoRetrainService), nameof(GetInstaAutoDeployPrediction), "END", string.IsNullOrEmpty(Convert.ToString(result[CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[CONSTANTS.CorrelationId])), string.Empty, string.Empty, string.Empty, string.Empty);
            return _instaRetrain;
        }
        private bool InstaAutoPrediction(string correlationId, string userId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaAutoRetrainService), nameof(InstaAutoPrediction), "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            bool ispredictionSucess = false;
            try
            {
                _deployModelViewModel = GetInstaDeployModel(correlationId);
                if (_deployModelViewModel != null && _deployModelViewModel.DeployModels.Count > 0)
                {
                    PredictionDTO predictionData = new PredictionDTO();
                    string frequency = _deployModelViewModel.DeployModels[0].Frequency;
                    PredictionDTO predictionDTO = new PredictionDTO
                    {
                        _id = Guid.NewGuid().ToString(),
                        UniqueId = Guid.NewGuid().ToString(),
                        CorrelationId = correlationId,
                        Frequency = frequency,
                        PredictedData = null,
                        Status = CONSTANTS.I,
                        ErrorMessage = null,
                        Progress = null,
                        CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                        ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                        CreatedByUser = userId,
                        ModifiedByUser = userId
                    };
                    bool DBEncryptionRequired = EncryptDB(_preProcessDTO.CorrelationId);
                    if (DBEncryptionRequired)
                    {
                        if (_IsAESKeyVault)
                            predictionDTO.ActualData = CryptographyUtility.Encrypt(CONSTANTS.Null);
                        else
                            predictionDTO.ActualData = AesProvider.Encrypt(CONSTANTS.Null, _aesKey, _aesVector);
                    }
                    else
                        predictionDTO.ActualData = CONSTANTS.Null;

                    if (_deployModelViewModel.DeployModels[0].ModelType == CONSTANTS.TimeSeries)
                    {
                        SavePrediction(predictionDTO);
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
                            UniId = predictionDTO.UniqueId,
                            Progress = null,
                            pageInfo = CONSTANTS.ForecastModel, // pageInfo 
                            ParamArgs = "{}",
                            Function = CONSTANTS.ForecastModel,
                            CreatedByUser = userId,
                            CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                            ModifiedByUser = userId,
                            ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                            LastProcessedOn = null
                        };
                        InsertRequests(ingrainRequest);
                    }
                    else
                    {
                        predictionDTO.ActualData = _deployModelViewModel.DeployModels[0].InputSample;
                        SavePrediction(predictionDTO);
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
                            UniId = predictionDTO.UniqueId,
                            Progress = null,
                            pageInfo = CONSTANTS.PublishModel, // pageInfo 
                            ParamArgs = "{}",
                            Function = CONSTANTS.PublishModel,
                            CreatedByUser = userId,
                            CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                            ModifiedByUser = userId,
                            ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                            LastProcessedOn = null
                        };
                        InsertRequests(ingrainRequest);
                    }
                    Thread.Sleep(2000);
                    bool isPrediction = true;
                    while (isPrediction)
                    {
                        predictionData = GetPrediction(predictionDTO);
                        if (predictionData.Status == CONSTANTS.C)
                        {
                            ispredictionSucess = true;
                            isPrediction = false;
                        }
                        else if (predictionData.Status == "E")
                        {
                            ispredictionSucess = false;
                            isPrediction = false;
                        }
                        else
                        {
                            Thread.Sleep(1000);
                            isPrediction = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InstaAutoRetrainService), nameof(InstaAutoPrediction), ex.Message + "- STACKTRACE - " + ex.StackTrace + "CORRELATIONID-" + (string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId)), ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }

            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaAutoRetrainService), nameof(InstaAutoPrediction), "END", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            return ispredictionSucess;
        }
        private DeployModelViewModel GetInstaDeployModel(string correlationId)
        {
            DeployModelViewModel deployModelView = new DeployModelViewModel();
            List<DeployModelsDto> modelsDto = new List<DeployModelsDto>();
            var modelCollection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
            var projection1 = Builders<DeployModelsDto>.Projection.Exclude(CONSTANTS.Id);
            var filterBuilder = Builders<DeployModelsDto>.Filter;
            var filter2 = filterBuilder.Eq(CONSTANTS.CorrelationId, correlationId) & filterBuilder.Eq(CONSTANTS.Status, "Deployed");
            var modelsData = modelCollection.Find(filter2).Project<DeployModelsDto>(projection1).ToList();
            if (modelsData.Count > 0)
            {
                for (int i = 0; i < modelsData.Count; i++)
                {
                    modelsDto.Add(modelsData[i]);
                }
            }
            deployModelView.DeployModels = modelsDto;
            return deployModelView;
        }
        private PredictionDTO GetPrediction(PredictionDTO predictionDTO)
        {
            PredictionDTO prediction = new PredictionDTO();
            var builder = Builders<PredictionDTO>.Filter;
            var filter = builder.Eq(CONSTANTS.CorrelationId, predictionDTO.CorrelationId) & builder.Eq("UniqueId", predictionDTO.UniqueId);
            var collection = _database.GetCollection<PredictionDTO>(CONSTANTS.SSAI_PublishModel);
            var result = collection.Find(filter).ToList();
            if (result.Count > 0)
            {
                prediction = result[0];
            }
            return prediction;
        }
        private void SavePrediction(PredictionDTO predictionDTO)
        {
            var jsonData = JsonConvert.SerializeObject(predictionDTO);
            var insertDocument = BsonSerializer.Deserialize<BsonDocument>(jsonData);
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_PublishModel);
            collection.InsertOne(insertDocument);
        }
        private DataEngineering InstaAutoDeployModel(string correlationId, string userId, string ProblemType)
        {
            DataEngineering dataEngineering = new DataEngineering();
            try
            {
                ////Gets the records from SSAI_RecommendedTrainedModels collection for frequency & accuracy in order to update in deployed model collection
                _recommendedAI = this.GetTrainedModel(correlationId, ProblemType);

                if (_recommendedAI != null && _recommendedAI.TrainedModel != null && _recommendedAI.TrainedModel.Count > 0)
                {
                    var result = IsDeployModelComplete(correlationId, _recommendedAI, ProblemType);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaAutoRetrainService), "_recommendedAI --" + result, "END", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                    if (result)
                    {
                        dataEngineering.Status = CONSTANTS.C;
                        dataEngineering.Message = "Deploy Model completed successfully";
                    }
                    else
                    {
                        dataEngineering.Status = "V";
                        dataEngineering.Message = "No record found for Correlation Id in Deploy Model";
                    }
                }
                else
                {
                    dataEngineering.Status = "V";
                    dataEngineering.Message = "No models trained for this correlation id, to proceed Deploy Model";
                }
            }
            catch (Exception ex)
            {
                dataEngineering.Message = ex.Message + "---" + ex.StackTrace;
                dataEngineering.Status = "E";
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaAutoRetrainService), nameof(InstaAutoDeployModel), "END", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            return dataEngineering;
        }

        private RecommedAITrainedModel GetTrainedModel(string correlationId, string problemType)
        {
            _recommendedAI = new RecommedAITrainedModel();
            _recommendedAI = GetRecommendedTrainedModels(correlationId);
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

                    case CONSTANTS.Multi_Class:
                        maxAccuracy = _recommendedAI.TrainedModel.Max(x => (double)x[CONSTANTS.Accuracy]);
                        _recommendedAI.TrainedModel = _recommendedAI.TrainedModel.Where(p => (double)p[CONSTANTS.Accuracy] == maxAccuracy).ToList();
                        break;
                    case CONSTANTS.Regression:
                        maxAccuracy = _recommendedAI.TrainedModel.Max(x => (double)x[CONSTANTS.r2ScoreVal][CONSTANTS.error_rate]);
                        _recommendedAI.TrainedModel = _recommendedAI.TrainedModel.Where(p => (double)p[CONSTANTS.r2ScoreVal][CONSTANTS.error_rate] == maxAccuracy).ToList();
                        break;
                    case CONSTANTS.TimeSeries:
                        maxAccuracy = _recommendedAI.TrainedModel.Max(x => (double)x[CONSTANTS.r2ScoreVal][CONSTANTS.error_rate]);
                        _recommendedAI.TrainedModel = _recommendedAI.TrainedModel.Where(p => (double)p[CONSTANTS.r2ScoreVal][CONSTANTS.error_rate] == maxAccuracy).ToList();
                        break;
                }
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaAutoRetrainService), nameof(GetTrainedModel) + "----" + _recommendedAI.TrainedModel, "END", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            return _recommendedAI;
        }
        private RecommedAITrainedModel GetRecommendedTrainedModels(string correlationId)
        {
            List<JObject> trainModelsList = new List<JObject>();
            RecommedAITrainedModel trainedModels = new RecommedAITrainedModel();
            var columnCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIRecommendedTrainedModels);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var trainedModel = columnCollection.Find(filter).ToList();
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
            string publishURL = appSettings.GetSection("AppSettings").GetSection("publishURL").Value;
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var resultData = collection.Find(filter).ToList();
            if (resultData.Count > 0)
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaAutoRetrainService), "DEPLOYEDMODELS INSIDE --", "END", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                JObject data = JObject.Parse(trainedModel.TrainedModel[0].ToString());
                double accuracy = 0;
                if (resultData[0]["ModelType"].ToString() == CONSTANTS.Multi_Class || resultData[0]["ModelType"].ToString() == CONSTANTS.Classification || resultData[0]["ModelType"].ToString() == CONSTANTS.Text_Classification)
                {
                    accuracy = Convert.ToDouble(data[CONSTANTS.Accuracy]);
                }
                else
                {
                    accuracy = Convert.ToDouble(data[CONSTANTS.r2ScoreVal][CONSTANTS.error_rate]);
                }
                var builder = Builders<BsonDocument>.Update;
                var update = builder.Set(CONSTANTS.Accuracy, accuracy)
                    .Set(CONSTANTS.Status, "Deployed")
                    .Set("DeployedDate", DateTime.Now.ToString(CONSTANTS.DateHoursFormat))
                    .Set("ModelVersion", data["modelName"].ToString())
                    .Set("IsUpdated", "True")
                    .Set(CONSTANTS.ModelURL, string.Format(publishURL + CONSTANTS.Zero, correlationId))
                    .Set(CONSTANTS.IsPrivate, false)
                    .Set(CONSTANTS.IsModelTemplate, false)
                    .Set("ModifiedOn", DateTime.Now.ToString(CONSTANTS.DateHoursFormat));
                collection.UpdateMany(filter, update);
                IsCompleted = true;
                SendVDSDeployModelNotification(correlationId, "Updated");
            }

            return IsCompleted;
        }
        private void DeleteGenerateModels(string correlationId, string pageInfo)
        {
            string problemType = string.Empty;
            //UseCase Deletion
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq(CONSTANTS.CorrelationId, correlationId) & ((builder.Eq(CONSTANTS.pageInfo, CONSTANTS.RecommendedAI)) | (builder.Eq(CONSTANTS.pageInfo, CONSTANTS.ModelTraining)) | (builder.Eq(CONSTANTS.pageInfo, CONSTANTS.WFTeachTest)));
            var useCaseCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
            var deletedResult = useCaseCollection.DeleteMany(filter);

            //RecommendedTrainedModels deletion
            var filter2 = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var trainedModelsCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIRecommendedTrainedModels);
            var modelResult = trainedModelsCollection.DeleteMany(filter2);


            //Delete the pickle file physically.
            var savedModelcollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_savedModels);
            var builder2 = Builders<BsonDocument>.Filter;
            var filter4 = builder2.Eq(CONSTANTS.CorrelationId, correlationId) & builder.Eq(CONSTANTS.FileType, CONSTANTS.MLDL_Model);
            var projection2 = Builders<BsonDocument>.Projection.Include(CONSTANTS.FilePath).Exclude(CONSTANTS.Id);
            var savedModelResult = savedModelcollection.Find(filter4).Project<BsonDocument>(projection2).ToList();
            if (savedModelResult.Count > 0)
            {
                for (int i = 0; i < savedModelResult.Count; i++)
                {
                    string filePath = savedModelResult[i][CONSTANTS.FilePath].ToString();
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }
            }
            var deleteFileResult = savedModelcollection.DeleteMany(filter4);
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
        private List<IngrainRequestQueue> GetMultipleRequestStatus(string correlationId, string pageInfo)
        {
            List<IngrainRequestQueue> ingrainRequest = new List<IngrainRequestQueue>();
            var collection = _database.GetCollection<IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
            var builder = Builders<IngrainRequestQueue>.Filter;
            var filter = builder.Eq(CONSTANTS.CorrelationId, correlationId) & builder.Eq(CONSTANTS.pageInfo, pageInfo);
            return ingrainRequest = collection.Find(filter).ToList();
        }
        private void RevertModelChanges(BsonDocument result)
        {
            // Revert the SSAI_RecommendedTrainedModels backup records.
            string updateString = "_backup";
            var filter1 = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, result[CONSTANTS.CorrelationId].ToString() + updateString);
            var trainedModelsCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIRecommendedTrainedModels);
            var update = Builders<BsonDocument>.Update.Set(CONSTANTS.CorrelationId, result[CONSTANTS.CorrelationId].ToString());
            var modelResult = trainedModelsCollection.UpdateMany(filter1, update);

            // revert the backup pickle files.
            var savedModelcollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_savedModels);
            var builder2 = Builders<BsonDocument>.Filter;
            var filter4 = builder2.Eq(CONSTANTS.CorrelationId, result[CONSTANTS.CorrelationId].ToString()) & builder2.Eq(CONSTANTS.FileType, CONSTANTS.MLDL_Model);
            var projection2 = Builders<BsonDocument>.Projection.Include(CONSTANTS.FilePath).Exclude(CONSTANTS.Id);
            var savedModelResult = savedModelcollection.Find(filter4).Project<BsonDocument>(projection2).ToList();
            if (savedModelResult.Count > 0)
            {
                for (int i = 0; i < savedModelResult.Count; i++)
                {
                    string filePath = savedModelResult[i][CONSTANTS.FilePath].ToString();
                    if (File.Exists(filePath))
                    {
                        string filePath2 = filePath.Replace("backup_", string.Empty);
                        File.Move(filePath, filePath2);
                        var update2 = Builders<BsonDocument>.Update.Set(CONSTANTS.FilePath, filePath2).Set(CONSTANTS.CorrelationId, result[CONSTANTS.CorrelationId].ToString());
                        savedModelcollection.UpdateMany(filter4, update2);
                    }
                }
            }
        }
        private void DeleteBackupRecords(BsonDocument result)
        {
            // Delete the SSAI_RecommendedTrainedModels records.
            string updateString = "_backup";
            var filter1 = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, result[CONSTANTS.CorrelationId].ToString() + updateString);
            var trainedModelsCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIRecommendedTrainedModels);
            var modelResult = trainedModelsCollection.DeleteMany(filter1);

            // Delete the backup pickle files.
            var savedModelcollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_savedModels);
            var builder1 = Builders<BsonDocument>.Filter;
            var filter2 = builder1.Eq(CONSTANTS.CorrelationId, result[CONSTANTS.CorrelationId].ToString() + updateString) & builder1.Eq(CONSTANTS.FileType, CONSTANTS.MLDL_Model);
            var projection1 = Builders<BsonDocument>.Projection.Include(CONSTANTS.FilePath).Exclude(CONSTANTS.Id);
            var savedModelResult = savedModelcollection.Find(filter2).Project<BsonDocument>(projection1).ToList();
            if (savedModelResult.Count > 0)
            {
                for (int i = 0; i < savedModelResult.Count; i++)
                {
                    string filePath = savedModelResult[i][CONSTANTS.FilePath].ToString();
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }
            }
            var deleteFileResult = savedModelcollection.DeleteMany(filter2);

            string problemType = string.Empty;
            string modelUrl = string.Empty;
            var deployedModelCollection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
            var filter3 = Builders<DeployModelsDto>.Filter.Eq(CONSTANTS.CorrelationId, result[CONSTANTS.CorrelationId].ToString());
            //Change deployed models to In - progress
            var resultofDeployedModel = deployedModelCollection.Find(filter3).ToList();
            if (resultofDeployedModel.Count > 0)
            {
                problemType = resultofDeployedModel[0].ModelType;
                modelUrl = resultofDeployedModel[0].ModelURL;
            }
            string[] arr = new string[] { };
            DeployModelsDto deployModel = new DeployModelsDto
            {
                Status = "In Progress",
                DeployedDate = null,
                ModelVersion = null,
                ModelType = null,
                InputSample = null,
                IsPrivate = false,
                IsModelTemplate = true,
                TrainedModelId = null,
                ModelURL = null
            };
            var updateBuilder = Builders<DeployModelsDto>.Update;
            var update = updateBuilder.Set(CONSTANTS.Accuracy, deployModel.Accuracy)
                //.Set("ModelURL", deployModel.ModelURL)
                .Set("ModelURL", modelUrl)
                .Set(CONSTANTS.Status, deployModel.Status)
                .Set("WebServices", deployModel.WebServices)
                .Set("DeployedDate", deployModel.DeployedDate)
                .Set("IsPrivate", true)
                .Set("IsUpdated", "True")
                .Set(CONSTANTS.IsModelTemplate, false)
                .Set("ModelVersion", deployModel.ModelVersion);
            var result2 = deployedModelCollection.UpdateMany(filter3, update);
            SendVDSDeployModelNotification(result[CONSTANTS.CorrelationId].ToString(), "Updated");

        }
        private void updateExistingModels(string correlationId, string pageInfo)
        {
            //string message = string.Empty;
            //UseCase Deletion
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq(CONSTANTS.CorrelationId, correlationId) & ((builder.Eq(CONSTANTS.pageInfo, CONSTANTS.RecommendedAI)) | (builder.Eq(CONSTANTS.pageInfo, CONSTANTS.ModelTraining)) | (builder.Eq(CONSTANTS.pageInfo, CONSTANTS.WFTeachTest)));
            var useCaseCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
            var deletedResult = useCaseCollection.DeleteMany(filter);

            //RecommendedTrainedModels backup
            var filter2 = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            string updateString = "_backup";
            var updateTrainedModels = Builders<BsonDocument>.Update;
            var updateModels = updateTrainedModels.Set(CONSTANTS.CorrelationId, correlationId + updateString);
            var trainedModelsCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIRecommendedTrainedModels);
            var modelResult = trainedModelsCollection.UpdateMany(filter2, updateModels);

            //Taking back up of pickle files
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_savedModels);
            var builder1 = Builders<BsonDocument>.Filter;
            var filter1 = builder1.Eq(CONSTANTS.CorrelationId, correlationId) & builder1.Eq(CONSTANTS.FileType, CONSTANTS.MLDL_Model) & builder1.Eq(CONSTANTS.FileType, CONSTANTS.LE);
            var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.FilePath).Exclude(CONSTANTS.Id);
            var savedModelResult = collection.Find(filter1).Project<BsonDocument>(projection).ToList();
            if (savedModelResult.Count > 0)
            {
                for (int i = 0; i < savedModelResult.Count; i++)
                {
                    string filePath = savedModelResult[i][CONSTANTS.FilePath].ToString();
                    if (File.Exists(filePath))
                    {
                        FileInfo fileInfo = new FileInfo(filePath);
                        var directory = Path.GetDirectoryName(filePath);
                        string[] filenames = fileInfo.Name.Split('.');
                        string filePath2 = Path.Combine(directory, "backup_" + filenames[0] + fileInfo.Extension);
                        File.Move(filePath, filePath2);
                        var update = Builders<BsonDocument>.Update.Set(CONSTANTS.FilePath, filePath2).Set(CONSTANTS.CorrelationId, correlationId + updateString);
                        collection.UpdateOne(filter1, update);
                    }
                }
            }

            //Resetting the Train models to default at ME_RecommendedModels Collection
            var recommendedModelsCollection = _database.GetCollection<BsonDocument>(CONSTANTS.MERecommendedModels);
            var modelsProjection = Builders<BsonDocument>.Projection.Include(CONSTANTS.Selected_Models).Exclude(CONSTANTS.Id);
            var modelsFilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var recommendedModelsResult = recommendedModelsCollection.Find(modelsFilter).Project<BsonDocument>(modelsProjection).ToList();
            JObject recommendedObject = new JObject();
            if (recommendedModelsResult.Count > 0)
            {
                recommendedObject = JObject.Parse(recommendedModelsResult[0].ToString());
                foreach (var item in recommendedObject[CONSTANTS.Selected_Models].Children())
                {
                    var jprop = item as JProperty;
                    if (jprop != null)
                    {
                        var columnToUpdate = string.Format(CONSTANTS.SelectedModels_Train_model, jprop.Name);
                        var updateModel = Builders<BsonDocument>.Update.Set(columnToUpdate, CONSTANTS.True);
                        recommendedModelsCollection.UpdateOne(modelsFilter, updateModel);
                    }
                }
            }
        }
        private DataEngineering GetDatatransformation(string correlationId, string pageInfo, string userId, BsonDocument result)
        {
            bool callMethod = true;
            DataEngineering dataEngineering = new DataEngineering();
            string PythonResult = string.Empty;
            IngrainRequestQueue ingrainRequest = new IngrainRequestQueue();
            while (callMethod)
            {
                var useCaseData = CheckPythonProcess(correlationId, CONSTANTS.DataPreprocessing);
                if (!string.IsNullOrEmpty(useCaseData))
                {
                    Thread.Sleep(1000);
                    JObject queueData = JObject.Parse(useCaseData);
                    dataEngineering.Status = (string)queueData[CONSTANTS.Status];
                    dataEngineering.Progress = (string)queueData[CONSTANTS.Progress];
                    dataEngineering.Message = (string)queueData[CONSTANTS.Message];
                    if (dataEngineering.Status == CONSTANTS.C & dataEngineering.Progress == CONSTANTS.Hundred)
                    {
                        callMethod = false;
                        dataEngineering.IsComplete = true;
                        var instaLog = Log(ingrainRequest, result);
                        InsertAutoLog(instaLog);
                        return dataEngineering;
                    }
                    if (dataEngineering.Status == "E")
                    {
                        callMethod = false;
                        dataEngineering.IsComplete = false;
                        var instaLog = Log(ingrainRequest, result);
                        InsertAutoLog(instaLog);
                        return dataEngineering;
                    }
                }
                else
                {
                    ingrainRequest._id = Guid.NewGuid().ToString();
                    ingrainRequest.CorrelationId = correlationId;
                    ingrainRequest.RequestId = Guid.NewGuid().ToString();
                    ingrainRequest.ProcessId = null;
                    ingrainRequest.Status = null;
                    ingrainRequest.ModelName = null;
                    ingrainRequest.RequestStatus = CONSTANTS.New;
                    ingrainRequest.RetryCount = 0;
                    ingrainRequest.ProblemType = null;
                    ingrainRequest.Message = null;
                    ingrainRequest.UniId = null;
                    ingrainRequest.Progress = null;
                    ingrainRequest.pageInfo = CONSTANTS.DataPreprocessing;
                    ingrainRequest.ParamArgs = "{}";
                    ingrainRequest.Function = CONSTANTS.DataTransform;
                    ingrainRequest.CreatedByUser = userId;
                    ingrainRequest.CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                    ingrainRequest.ModifiedByUser = userId;
                    ingrainRequest.ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                    ingrainRequest.LastProcessedOn = null;
                    InsertRequests(ingrainRequest);
                    Thread.Sleep(1000);
                }
            }
            return dataEngineering;
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
        private void RemoveDataPreprocessAttributes(string correlationId, List<string> PreprocessCols, List<string> DEColmnList, string field)
        {
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataProcessing);
            FilterDefinition<BsonDocument> filterDefinition = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            foreach (var column in PreprocessCols)
            {
                bool exists = DEColmnList.Contains(column);
                if (!exists & column != CONSTANTS.Interpolation)
                {
                    UpdateDefinition<BsonDocument> updateDefinition = null;
                    UpdateDefinitionBuilder<BsonDocument> updateDefinitionBuilder = Builders<BsonDocument>.Update;
                    string updateColumn = field + CONSTANTS.Dot + column;
                    if (updateDefinition == null)
                        updateDefinition = updateDefinitionBuilder.Unset(updateColumn);
                    else
                        updateDefinition = updateDefinition.Unset(updateColumn);
                    var updateRes = collection.UpdateOne(filterDefinition, updateDefinition);
                }
            }
        }
        private void RemoveDataPreprocessEncryptAttributes(List<string> DEColmnList, JObject jData, string correlationId, bool isDataModification, string nodeName)
        {
            #region Filters Removing Attributes and Update DB
            var preprocessFilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataProcessing);
            List<string> PreprocessCols = new List<string>();
            if (isDataModification)
            {
                //For DataModification ColumnBinning Update
                if (jData[CONSTANTS.ColumnBinning] != null)
                {
                    foreach (var item in jData[CONSTANTS.ColumnBinning].Children())
                    {
                        JProperty j = item as JProperty;
                        PreprocessCols.Add(j.Name);
                    }
                    JObject columnBinning = new JObject();
                    foreach (var column in PreprocessCols)
                    {
                        bool exists = DEColmnList.Contains(column);
                        if (!exists)
                        {
                            columnBinning = JObject.Parse(jData[CONSTANTS.ColumnBinning].ToString());
                            columnBinning.Property(column).Remove();
                        }
                    }
                    jData["ColumnBinning"] = JObject.FromObject(columnBinning);
                }

                //For DataModification Features Update
                PreprocessCols = new List<string>();

                foreach (var item in jData[CONSTANTS.Features].Children())
                {
                    JProperty j = item as JProperty;
                    PreprocessCols.Add(j.Name);
                }
                JObject Features = new JObject();
                foreach (var column in PreprocessCols)
                {
                    bool exists = DEColmnList.Contains(column);
                    if (!exists & column != CONSTANTS.Interpolation)
                    {
                        Features = JObject.Parse(jData[CONSTANTS.Features].ToString());
                        Features.Property(column).Remove();
                    }
                }
                jData["Features"] = JObject.FromObject(Features);
            }
            else
            {
                foreach (var item in jData.Children())
                {
                    JProperty j = item as JProperty;
                    PreprocessCols.Add(j.Name);
                }
                foreach (var column in PreprocessCols)
                {
                    bool exists = DEColmnList.Contains(column);
                    if (!exists)
                    {
                        jData.Property(column).Remove();
                    }
                }
            }
            var filterUpdate = Builders<BsonDocument>.Update.Set(nodeName, (_IsAESKeyVault ? CryptographyUtility.Encrypt(jData.ToString(Formatting.None)) : AesProvider.Encrypt(jData.ToString(Formatting.None), _aesKey, _aesVector)));
            collection.UpdateOne(preprocessFilter, filterUpdate);
            #endregion
        }
        private void PreProcessAttributesRemove(string correlationId, JObject DePreprocessData, List<string> DEColmnList)
        {
            List<string> PreprocessCols = new List<string>();

            #region Filters Remove Start
            foreach (var item in DePreprocessData[CONSTANTS.Filters].Children())
            {
                JProperty j = item as JProperty;
                PreprocessCols.Add(j.Name);
            }
            RemoveDataPreprocessAttributes(correlationId, PreprocessCols, DEColmnList, CONSTANTS.Filters);
            #endregion

            #region MissingValues Remove Start
            PreprocessCols = new List<string>();
            foreach (var item in DePreprocessData[CONSTANTS.MissingValues].Children())
            {
                JProperty j = item as JProperty;
                PreprocessCols.Add(j.Name);
            }
            RemoveDataPreprocessAttributes(correlationId, PreprocessCols, DEColmnList, CONSTANTS.MissingValues);
            #endregion

            #region DataEncoding Remove Start
            PreprocessCols = new List<string>();
            foreach (var item in DePreprocessData[CONSTANTS.DataEncoding].Children())
            {
                JProperty j = item as JProperty;
                PreprocessCols.Add(j.Name);
            }
            RemoveDataPreprocessAttributes(correlationId, PreprocessCols, DEColmnList, CONSTANTS.DataEncoding);
            #endregion

            #region DataModification Remove Start
            PreprocessCols = new List<string>();
            foreach (var item in DePreprocessData[CONSTANTS.DataModification][CONSTANTS.ColumnBinning].Children())
            {
                JProperty j = item as JProperty;
                PreprocessCols.Add(j.Name);
            }
            RemoveDataPreprocessAttributes(correlationId, PreprocessCols, DEColmnList, CONSTANTS.DataModification + CONSTANTS.Dot + CONSTANTS.ColumnBinning);
            PreprocessCols = new List<string>();
            foreach (var item in DePreprocessData[CONSTANTS.DataModification][CONSTANTS.Features].Children())
            {
                JProperty j = item as JProperty;
                PreprocessCols.Add(j.Name);
            }
            RemoveDataPreprocessAttributes(correlationId, PreprocessCols, DEColmnList, CONSTANTS.DataModification + CONSTANTS.Dot + CONSTANTS.Features);
            #endregion

        }
        private void PreProcessEncryptAttributesRemove(List<BsonDocument> preprocessDataExist, string correlationId, List<string> DEColmnList)
        {
            JObject Filters = new JObject();
            JObject MissingValues = new JObject();
            JObject DataModification = new JObject();
            JObject DataEncoding = new JObject();
            List<string> PreprocessCols = new List<string>();
            if (_IsAESKeyVault)
            {
                Filters = JObject.Parse(BsonDocument.Parse(CryptographyUtility.Decrypt(preprocessDataExist[0][CONSTANTS.Filters].AsString)).ToString());
                MissingValues = JObject.Parse(BsonDocument.Parse(CryptographyUtility.Decrypt(preprocessDataExist[0][CONSTANTS.MissingValues].AsString)).ToString());
                DataModification = JObject.Parse(BsonDocument.Parse(CryptographyUtility.Decrypt(preprocessDataExist[0][CONSTANTS.DataModification].AsString)).ToString());
            }
            else
            {
                Filters = JObject.Parse(BsonDocument.Parse(AesProvider.Decrypt(preprocessDataExist[0][CONSTANTS.Filters].AsString, _aesKey, _aesVector)).ToString());
                MissingValues = JObject.Parse(BsonDocument.Parse(AesProvider.Decrypt(preprocessDataExist[0][CONSTANTS.MissingValues].AsString, _aesKey, _aesVector)).ToString());
                DataModification = JObject.Parse(BsonDocument.Parse(AesProvider.Decrypt(preprocessDataExist[0][CONSTANTS.DataModification].AsString, _aesKey, _aesVector)).ToString());
            }
            DataEncoding = JObject.Parse(preprocessDataExist[0][CONSTANTS.DataEncoding].ToString());
            List<string> preProcessObjects = new List<string> { CONSTANTS.Filters, CONSTANTS.MissingValues, CONSTANTS.DataModification };
            foreach (var process in preProcessObjects)
            {
                switch (process)
                {
                    case CONSTANTS.Filters:
                        RemoveDataPreprocessEncryptAttributes(DEColmnList, Filters, correlationId, false, process);
                        break;
                    case CONSTANTS.MissingValues:
                        RemoveDataPreprocessEncryptAttributes(DEColmnList, MissingValues, correlationId, false, process);
                        break;
                    case CONSTANTS.DataModification:
                        RemoveDataPreprocessEncryptAttributes(DEColmnList, DataModification, correlationId, true, process);
                        break;
                }
            }
            #region DataEncoding Remove Start
            PreprocessCols = new List<string>();
            foreach (var item in DataEncoding.Children())
            {
                JProperty j = item as JProperty;
                PreprocessCols.Add(j.Name);
            }
            RemoveDataPreprocessAttributes(correlationId, PreprocessCols, DEColmnList, CONSTANTS.DataEncoding);
            #endregion
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
        private bool CreatePreprocess(string correlationId, string userId, string problemType, string instaId)
        {
            PreProcessModelDTO preProcessModel = new PreProcessModelDTO
            {
                CorrelationId = correlationId,
                ModelType = problemType
            };
            preProcessModel.ModelType = problemType;
            var preprocessDataExist = GetPreprocessExistData(correlationId);
            if (preprocessDataExist.Count > 0)
            {
                //Starting code
                List<string> DEColmnList = new List<string>();
                JObject serializeDEData = new JObject();
                var deDataCleanup = new List<BsonDocument>();
                var deFilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
                var deprojection = Builders<BsonDocument>.Projection.Include(CONSTANTS.FeatureName).Include(CONSTANTS.NewFeatureName).Exclude(CONSTANTS.Id);
                var deCollection = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataCleanup);
                deDataCleanup = deCollection.Find(deFilter).Project<BsonDocument>(deprojection).ToList();
                bool EncryptionRequired = EncryptDB(correlationId);
                if (deDataCleanup.Count > 0)
                {
                    if (EncryptionRequired)
                    {
                        if (_IsAESKeyVault)
                            deDataCleanup[0][CONSTANTS.FeatureName] = BsonDocument.Parse(CryptographyUtility.Decrypt(deDataCleanup[0][CONSTANTS.FeatureName].AsString));
                        else
                            deDataCleanup[0][CONSTANTS.FeatureName] = BsonDocument.Parse(AesProvider.Decrypt(deDataCleanup[0][CONSTANTS.FeatureName].AsString, _aesKey, _aesVector));
                        if (deDataCleanup[0].Contains(CONSTANTS.NewFeatureName))
                        {
                            if (deDataCleanup[0][CONSTANTS.NewFeatureName].ToString() != "{ }")
                            {
                                if (_IsAESKeyVault)
                                    deDataCleanup[0][CONSTANTS.NewFeatureName] = BsonDocument.Parse(CryptographyUtility.Decrypt(deDataCleanup[0][CONSTANTS.NewFeatureName].AsString));
                                else
                                    deDataCleanup[0][CONSTANTS.NewFeatureName] = BsonDocument.Parse(AesProvider.Decrypt(deDataCleanup[0][CONSTANTS.NewFeatureName].AsString, _aesKey, _aesVector));
                            }
                        }
                    }
                    //Combining new features to Existing Features
                    JObject datas = JObject.Parse(deDataCleanup[0].ToString());
                    JObject combinedFeatures = new JObject();
                    combinedFeatures = this.CombinedFeatures(datas);
                    if (combinedFeatures != null)
                        deDataCleanup[0][CONSTANTS.FeatureName] = BsonDocument.Parse(combinedFeatures.ToString());

                    serializeDEData = JObject.Parse(deDataCleanup[0].ToString());
                    foreach (var features in serializeDEData[CONSTANTS.FeatureName].Children())
                    {
                        JProperty j = features as JProperty;
                        DEColmnList.Add(j.Name);
                    }
                    var collection = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataProcessing);
                    var preprocessFilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
                    var preprocessProjection = Builders<BsonDocument>.Projection.Include(CONSTANTS.Filters).Include(CONSTANTS.MissingValues).Include(CONSTANTS.DataEncoding).Include(CONSTANTS.DataModification).Exclude(CONSTANTS.Id);
                    var PreprocessData = collection.Find(preprocessFilter).Project<BsonDocument>(preprocessProjection).ToList();
                    if (PreprocessData.Count > 0)
                    {
                        if (EncryptionRequired)
                        {
                            //To Remove DataPreProcess Attributes with Encryption
                            PreProcessEncryptAttributesRemove(PreprocessData, correlationId, DEColmnList);
                            RemoveEncryptionPChnageRequest(PreprocessData, correlationId);
                        }
                        else
                        {
                            //To Remove DataPreProcess Attributes without Encryption
                            JObject DePreprocessData = JObject.Parse(PreprocessData[0].ToString());
                            PreProcessAttributesRemove(correlationId, DePreprocessData, DEColmnList);
                            RemovePChangeRequest(DePreprocessData, correlationId);
                        }
                    }
                }
                //Ending Code
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
            bool DBEncryptionRequired = EncryptDB(correlationId);
            if (filteredData.Count > 0)
            {
                //decrypt db data
                if (DBEncryptionRequired)
                {
                    if (_IsAESKeyVault)
                        filteredData[0][CONSTANTS.FeatureName] = BsonDocument.Parse(CryptographyUtility.Decrypt(filteredData[0][CONSTANTS.FeatureName].AsString));
                    else
                        filteredData[0][CONSTANTS.FeatureName] = BsonDocument.Parse(AesProvider.Decrypt(filteredData[0][CONSTANTS.FeatureName].AsString, _aesKey, _aesVector));
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
                        if (property != null && property.Name == CONSTANTS.Category && property.Value.ToString() == CONSTANTS.True)
                        {
                            categoricalColumns.Add(item);
                            if (value > 0)
                                missingColumns.Add(item);
                        }
                        if (property != null && (property.Name == "float64" || property.Name == "int64") && property.Value.ToString() == CONSTANTS.True)
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
                    //decrypt db data
                    if (DBEncryptionRequired)
                    {
                        if (_IsAESKeyVault)
                            filteredResult[0][CONSTANTS.ColumnUniqueValues] = BsonDocument.Parse(CryptographyUtility.Decrypt(filteredResult[0][CONSTANTS.ColumnUniqueValues].AsString));
                        else
                            filteredResult[0][CONSTANTS.ColumnUniqueValues] = BsonDocument.Parse(AesProvider.Decrypt(filteredResult[0][CONSTANTS.ColumnUniqueValues].AsString, _aesKey, _aesVector));
                    }
                    _preProcessDTO.TargetColumn = filteredResult[0][CONSTANTS.target_variable].ToString();
                    uniqueData = JObject.Parse(filteredResult[0].ToString());
                    //Getting the Missing Values and Filters Data
                    GetMissingAndFiltersData(missingColumns, categoricalColumns, numericalColumns, uniqueData);
                    InsertToPreprocess(preProcessModel, instaId);
                    insertSuccess = true;
                }
            }
            return insertSuccess;
        }
        private void RemoveEncryptionPChnageRequest(List<BsonDocument> preprocessDataExist, string correlationId)
        {
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataProcessing);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            JObject Filters = new JObject();
            JObject MissingValues = new JObject();
            JObject DataModification = new JObject();
            JObject DataEncoding = new JObject();
            List<string> PreprocessCols = new List<string>();
            if (_IsAESKeyVault)
            {
                Filters = JObject.Parse(BsonDocument.Parse(CryptographyUtility.Decrypt(preprocessDataExist[0][CONSTANTS.Filters].AsString)).ToString());
                MissingValues = JObject.Parse(BsonDocument.Parse(CryptographyUtility.Decrypt(preprocessDataExist[0][CONSTANTS.MissingValues].AsString)).ToString());
                DataModification = JObject.Parse(BsonDocument.Parse(CryptographyUtility.Decrypt(preprocessDataExist[0][CONSTANTS.DataModification].AsString)).ToString());
            }
            else
            {
                Filters = JObject.Parse(BsonDocument.Parse(AesProvider.Decrypt(preprocessDataExist[0][CONSTANTS.Filters].AsString, _aesKey, _aesVector)).ToString());
                MissingValues = JObject.Parse(BsonDocument.Parse(AesProvider.Decrypt(preprocessDataExist[0][CONSTANTS.MissingValues].AsString, _aesKey, _aesVector)).ToString());
                DataModification = JObject.Parse(BsonDocument.Parse(AesProvider.Decrypt(preprocessDataExist[0][CONSTANTS.DataModification].AsString, _aesKey, _aesVector)).ToString());
            }
            DataEncoding = JObject.Parse(preprocessDataExist[0][CONSTANTS.DataEncoding].ToString());

            List<string> preProcessObjects = new List<string> { CONSTANTS.Filters, CONSTANTS.MissingValues, CONSTANTS.DataModification, CONSTANTS.DataEncoding };
            foreach (var process in preProcessObjects)
            {
                switch (process)
                {
                    case CONSTANTS.Filters:
                        bool isUpdated = false;
                        foreach (var item in Filters.Children())
                        {
                            JProperty prop = item as JProperty;
                            if (prop != null)
                            {
                                PythonInput pythonInput = JsonConvert.DeserializeObject<PythonInput>(Filters[prop.Name].ToString());
                                if (pythonInput != null && !string.IsNullOrEmpty(pythonInput.PChangeRequest) && pythonInput.PChangeRequest != CONSTANTS.Null)
                                {
                                    isUpdated = true;
                                    Filters[prop.Name][CONSTANTS.PChangeRequest] = "";
                                }
                            }
                        }
                        if (isUpdated)
                        {
                            var PChangeRequestUpdate = Builders<BsonDocument>.Update.Set(CONSTANTS.Filters, (_IsAESKeyVault ? CryptographyUtility.Encrypt(Filters.ToString(Formatting.None)) : AesProvider.Encrypt(Filters.ToString(Formatting.None), _aesKey, _aesVector)));
                            collection.UpdateOne(filter, PChangeRequestUpdate);
                        }
                        break;
                    case CONSTANTS.MissingValues:
                        bool isUpdated2 = false;
                        foreach (var item in MissingValues.Children())
                        {
                            JProperty prop = item as JProperty;
                            if (prop != null)
                            {
                                PythonInput pythonInput = JsonConvert.DeserializeObject<PythonInput>(MissingValues[prop.Name].ToString());
                                if (pythonInput != null && !string.IsNullOrEmpty(pythonInput.PChangeRequest) && pythonInput.PChangeRequest != CONSTANTS.Null)
                                {
                                    isUpdated2 = true;
                                    MissingValues[prop.Name][CONSTANTS.PChangeRequest] = "";
                                }
                            }
                        }
                        if (isUpdated2)
                        {
                            var PChangeRequestUpdate = Builders<BsonDocument>.Update.Set(CONSTANTS.MissingValues, (_IsAESKeyVault ? CryptographyUtility.Encrypt(MissingValues.ToString(Formatting.None)) : AesProvider.Encrypt(MissingValues.ToString(Formatting.None), _aesKey, _aesVector)));
                            collection.UpdateOne(filter, PChangeRequestUpdate);
                        }
                        break;
                    case CONSTANTS.DataModification:
                        bool isUpdated3 = false;
                        if (DataModification[CONSTANTS.ColumnBinning] != null)
                        {
                            foreach (var item in DataModification[CONSTANTS.ColumnBinning].Children())
                            {
                                JProperty prop = item as JProperty;
                                if (prop != null)
                                {
                                    PythonInput pythonInput = JsonConvert.DeserializeObject<PythonInput>(DataModification[CONSTANTS.ColumnBinning][prop.Name]["PChangeRequest"].ToString());
                                    if (pythonInput != null && !string.IsNullOrEmpty(pythonInput.PChangeRequest) && pythonInput.PChangeRequest != CONSTANTS.Null)
                                    {
                                        isUpdated3 = true;
                                        DataModification[CONSTANTS.ColumnBinning][prop.Name]["PChangeRequest"][CONSTANTS.PChangeRequest] = "";
                                    }
                                }
                            }
                        }

                        //DataModification Features update
                        foreach (var item in DataModification[CONSTANTS.Features].Children())
                        {
                            JProperty prop = item as JProperty;
                            if (prop != null)
                            {
                                foreach (var item2 in DataModification[CONSTANTS.Features][prop.Name].Children())
                                {
                                    JProperty prop2 = item2 as JProperty;
                                    if (prop2 != null)
                                    {
                                        if (prop2.Name == CONSTANTS.Outlier)
                                        {
                                            PythonInput pythonInput = JsonConvert.DeserializeObject<PythonInput>(DataModification[CONSTANTS.Features][prop.Name][CONSTANTS.Outlier].ToString());
                                            if (pythonInput != null && !string.IsNullOrEmpty(pythonInput.PChangeRequest) && pythonInput.PChangeRequest != CONSTANTS.Null)
                                            {
                                                isUpdated3 = true;
                                                DataModification[CONSTANTS.Features][prop.Name][CONSTANTS.Outlier][CONSTANTS.PChangeRequest] = "";
                                            }
                                        }
                                        if (prop2.Name == CONSTANTS.Skewness)
                                        {
                                            PythonInput pythonInput = JsonConvert.DeserializeObject<PythonInput>(DataModification[CONSTANTS.Features][prop.Name][CONSTANTS.Skewness].ToString());
                                            if (pythonInput != null && !string.IsNullOrEmpty(pythonInput.PChangeRequest) && pythonInput.PChangeRequest != CONSTANTS.Null)
                                            {
                                                isUpdated3 = true;
                                                DataModification[CONSTANTS.Features][prop.Name][CONSTANTS.Skewness][CONSTANTS.PChangeRequest] = "";
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        if (isUpdated3)
                        {
                            var PChangeRequestUpdate = Builders<BsonDocument>.Update.Set(CONSTANTS.DataModification, (_IsAESKeyVault ? CryptographyUtility.Encrypt(DataModification.ToString(Formatting.None)) : AesProvider.Encrypt(DataModification.ToString(Formatting.None), _aesKey, _aesVector)));
                            collection.UpdateOne(filter, PChangeRequestUpdate);
                        }
                        break;
                    case CONSTANTS.DataEncoding:
                        bool isUpdated4 = false;
                        foreach (var item in DataEncoding.Children())
                        {
                            JProperty prop = item as JProperty;
                            if (prop != null)
                            {
                                PythonInput pythonInput = JsonConvert.DeserializeObject<PythonInput>(DataEncoding[prop.Name].ToString());
                                if (pythonInput != null && !string.IsNullOrEmpty(pythonInput.PChangeRequest) && pythonInput.PChangeRequest != CONSTANTS.Null && pythonInput.PChangeRequest.ToLower() != "false")
                                {
                                    isUpdated4 = true;
                                    DataEncoding[prop.Name][CONSTANTS.PChangeRequest] = "";
                                }
                            }
                        }
                        if (isUpdated4)
                        {
                            var PChangeRequestUpdate = Builders<BsonDocument>.Update.Set(CONSTANTS.DataEncoding, (_IsAESKeyVault ? CryptographyUtility.Encrypt(DataEncoding.ToString(Formatting.None)) : AesProvider.Encrypt(DataEncoding.ToString(Formatting.None), _aesKey, _aesVector)));
                            collection.UpdateOne(filter, PChangeRequestUpdate);
                        }
                        break;
                }
            }
        }
        private void RemovePChangeRequest(JObject data, string correlationId)
        {
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataProcessing);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            List<string> preProcessObjects = new List<string> { CONSTANTS.Filters, CONSTANTS.MissingValues, CONSTANTS.DataModification, CONSTANTS.DataEncoding };
            foreach (var process in preProcessObjects)
            {
                switch (process)
                {
                    case CONSTANTS.Filters:
                        bool isUpdated = false;
                        foreach (var item in data[process].Children())
                        {
                            JProperty prop = item as JProperty;
                            if (prop != null)
                            {
                                PythonInput pythonInput = JsonConvert.DeserializeObject<PythonInput>(data[process][prop.Name].ToString());
                                if (pythonInput != null && !string.IsNullOrEmpty(pythonInput.PChangeRequest) && pythonInput.PChangeRequest != CONSTANTS.Null)
                                {
                                    isUpdated = true;
                                    data[process][prop.Name][CONSTANTS.PChangeRequest] = "";
                                }
                            }
                        }
                        if (isUpdated)
                        {
                            var Filters = BsonDocument.Parse(data[CONSTANTS.Filters].ToString());
                            var PChangeRequestUpdate = Builders<BsonDocument>.Update.Set(CONSTANTS.Filters, Filters);
                            collection.UpdateOne(filter, PChangeRequestUpdate);
                        }
                        break;
                    case CONSTANTS.MissingValues:
                        bool isUpdated2 = false;
                        foreach (var item in data[process].Children())
                        {
                            JProperty prop = item as JProperty;
                            if (prop != null)
                            {
                                PythonInput pythonInput = JsonConvert.DeserializeObject<PythonInput>(data[process][prop.Name].ToString());
                                if (pythonInput != null && !string.IsNullOrEmpty(pythonInput.PChangeRequest) && pythonInput.PChangeRequest != CONSTANTS.Null)
                                {
                                    isUpdated2 = true;
                                    data[process][prop.Name][CONSTANTS.PChangeRequest] = "";
                                }
                            }
                        }
                        if (isUpdated2)
                        {
                            var MissingValue = BsonDocument.Parse(data[CONSTANTS.MissingValues].ToString());
                            var PChangeRequestUpdate = Builders<BsonDocument>.Update.Set(CONSTANTS.MissingValues, MissingValue);
                            collection.UpdateOne(filter, PChangeRequestUpdate);
                        }
                        break;
                    case CONSTANTS.DataModification:
                        bool isUpdated3 = false;
                        foreach (var item in data[process][CONSTANTS.ColumnBinning].Children())
                        {
                            JProperty prop = item as JProperty;
                            if (prop != null)
                            {
                                PythonInput pythonInput = JsonConvert.DeserializeObject<PythonInput>(data[process][CONSTANTS.ColumnBinning][prop.Name]["PChangeRequest"].ToString());
                                if (pythonInput != null && !string.IsNullOrEmpty(pythonInput.PChangeRequest) && pythonInput.PChangeRequest != CONSTANTS.Null)
                                {
                                    isUpdated3 = true;
                                    data[process][CONSTANTS.ColumnBinning][prop.Name]["PChangeRequest"][CONSTANTS.PChangeRequest] = "";
                                }
                            }
                        }

                        //DataModification Features update
                        foreach (var item in data[process][CONSTANTS.Features].Children())
                        {
                            JProperty prop = item as JProperty;
                            if (prop != null)
                            {
                                foreach (var item2 in data[process][CONSTANTS.Features][prop.Name].Children())
                                {
                                    JProperty prop2 = item2 as JProperty;
                                    if (prop2 != null)
                                    {
                                        if (prop2.Name == CONSTANTS.Outlier)
                                        {
                                            PythonInput pythonInput = JsonConvert.DeserializeObject<PythonInput>(data[process][CONSTANTS.Features][prop.Name][CONSTANTS.Outlier].ToString());
                                            if (pythonInput != null && !string.IsNullOrEmpty(pythonInput.PChangeRequest) && pythonInput.PChangeRequest != CONSTANTS.Null)
                                            {
                                                isUpdated3 = true;
                                                data[process][CONSTANTS.Features][prop.Name][CONSTANTS.Outlier][CONSTANTS.PChangeRequest] = "";
                                            }
                                        }
                                        if (prop2.Name == CONSTANTS.Skewness)
                                        {
                                            PythonInput pythonInput = JsonConvert.DeserializeObject<PythonInput>(data[process][CONSTANTS.Features][prop.Name][CONSTANTS.Skewness].ToString());
                                            if (pythonInput != null && !string.IsNullOrEmpty(pythonInput.PChangeRequest) && pythonInput.PChangeRequest != CONSTANTS.Null)
                                            {
                                                isUpdated3 = true;
                                                data[process][CONSTANTS.Features][prop.Name][CONSTANTS.Skewness][CONSTANTS.PChangeRequest] = "";
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        if (isUpdated3)
                        {
                            var DataModification = BsonDocument.Parse(data[CONSTANTS.DataModification].ToString());
                            var PChangeRequestUpdate = Builders<BsonDocument>.Update.Set(CONSTANTS.DataModification, DataModification);
                            collection.UpdateOne(filter, PChangeRequestUpdate);
                        }
                        break;
                    case CONSTANTS.DataEncoding:
                        bool isUpdated4 = false;
                        foreach (var item in data[process].Children())
                        {
                            JProperty prop = item as JProperty;
                            if (prop != null)
                            {
                                PythonInput pythonInput = JsonConvert.DeserializeObject<PythonInput>(data[process][prop.Name].ToString());
                                if (pythonInput != null && !string.IsNullOrEmpty(pythonInput.PChangeRequest) && pythonInput.PChangeRequest != CONSTANTS.Null && pythonInput.PChangeRequest.ToLower() != "false")
                                {
                                    isUpdated4 = true;
                                    data[process][prop.Name][CONSTANTS.PChangeRequest] = "";
                                }
                            }
                        }
                        if (isUpdated4)
                        {
                            var dataEncoding = BsonDocument.Parse(data[CONSTANTS.DataEncoding].ToString());
                            var PChangeRequestUpdate = Builders<BsonDocument>.Update.Set(CONSTANTS.DataEncoding, dataEncoding);
                            collection.UpdateOne(filter, PChangeRequestUpdate);
                        }
                        break;
                }
            }
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
            bool DBEncryptionRequired = EncryptDB(correlationId);
            if (filteredData.Count > 0)
            {
                if (DBEncryptionRequired)
                {
                    if (_IsAESKeyVault)
                        filteredData[0][CONSTANTS.FeatureName] = BsonDocument.Parse(CryptographyUtility.Decrypt(filteredData[0][CONSTANTS.FeatureName].AsString));
                    else
                        filteredData[0][CONSTANTS.FeatureName] = BsonDocument.Parse(AesProvider.Decrypt(filteredData[0][CONSTANTS.FeatureName].AsString, _aesKey, _aesVector));
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
                        var skewData = serializeData[CONSTANTS.FeatureName][item][CONSTANTS.Skeweness];
                        float outValue = (float)outData;
                        string skewValue = (string)skewData;
                        var imbalanced = serializeData[CONSTANTS.FeatureName][item][CONSTANTS.ImBalanced];
                        string imbalancedValue = (string)imbalanced;
                        if (imbalancedValue == "1")
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
                                        dict.Add(CONSTANTS.ChangeRequest, "");
                                        binningColumns2.Add(CONSTANTS.ChangeRequest, dict);
                                    }
                                    else
                                    {
                                        dict.Add(CONSTANTS.PChangeRequest, "");
                                        binningColumns2.Add(CONSTANTS.PChangeRequest, dict);
                                    }
                                }
                            }
                            columnBinning.Add(item, binningColumns2);
                        }
                        else if (imbalancedValue == "2")
                        {
                            string removeColumndesc = string.Format(CONSTANTS.StringFormat, item);
                            removeImbalancedColumns.Add(item, removeColumndesc);
                        }
                        else if (imbalancedValue == "3")
                        {
                            string prescription = string.Format(CONSTANTS.StringFormat1, item);
                            prescriptionData.Add(item, prescription);
                        }
                        if (prescriptionData.Count > 0)
                            _preProcessDTO.Prescriptions = prescriptionData;

                        if (outValue > 0)
                        {
                            string strForm = string.Format(CONSTANTS.StringFormat2, item, outValue);
                            outlier.Add("Text", strForm);
                            string[] outliers = { CONSTANTS.Mean, CONSTANTS.Median, "Mode", CONSTANTS.CustomValue, CONSTANTS.ChangeRequest, CONSTANTS.PChangeRequest };
                            for (int i = 0; i < outliers.Length; i++)
                            {
                                if (i == 3)
                                {
                                    outlier.Add(outliers[i], "");
                                }
                                else if (i == 4 || i == 5)
                                {
                                    outlier.Add(outliers[i], "");
                                }
                                else
                                {
                                    outlier.Add(outliers[i], CONSTANTS.False);
                                }
                            }
                        }
                        if (skewValue == "Yes")
                        {
                            string strForm = string.Format(CONSTANTS.StringFormat3, item);
                            skeweness.Add(CONSTANTS.Skeweness, strForm);
                            string[] skewnessArray = { CONSTANTS.BoxCox, CONSTANTS.Reciprocal, CONSTANTS.Log, CONSTANTS.ChangeRequest, CONSTANTS.PChangeRequest };
                            for (int i = 0; i < skewnessArray.Length; i++)
                            {
                                if (i == 3 || i == 4)
                                {
                                    skeweness.Add(skewnessArray[i], "");
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
                            fields.Add(CONSTANTS.Skeweness, skeweness);
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
                foreach (JToken scale in serializeData[CONSTANTS.FeatureName][column]["Scale"].Children())
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
                    dataEncodingData.Add(CONSTANTS.PChangeRequest, "");
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


            foreach (var column in categoricalColumns)
            {
                Dictionary<string, string> fieldDictionary = new Dictionary<string, string>();
                foreach (JToken value in uniqueData[CONSTANTS.ColumnUniqueValues][column].Children())
                {
                    if (!string.IsNullOrEmpty(Convert.ToString(value)))
                        fieldDictionary.Add(value.ToString().Replace(".", "\u2024").Replace("\r\n", " ").Replace("\"", "").Replace("\t", " "), CONSTANTS.False);
                }
                if (fieldDictionary.Count > 0)
                {
                    fieldDictionary.Add(CONSTANTS.ChangeRequest, "");
                    fieldDictionary.Add(CONSTANTS.PChangeRequest, "");
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
                            fieldDictionary.Add(value.ToString().Replace(".", "\u2024").Replace("\r\n", " ").Replace("\"", "").Replace("\t", " "), CONSTANTS.True);
                        }
                        else
                        {
                            fieldDictionary.Add(value.ToString().Replace(".", "\u2024").Replace("\r\n", " ").Replace("\"", "").Replace("\t", " "), CONSTANTS.False);
                        }
                    i++;
                }
                if (fieldDictionary.Count > 0)
                {
                    fieldDictionary.Add(CONSTANTS.ChangeRequest, CONSTANTS.True);
                    fieldDictionary.Add(CONSTANTS.PChangeRequest, "");
                    fieldDictionary.Add(CONSTANTS.CustomValue, "");
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
                            numericDictionary.Add(numericColumnn, "");
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
                        numericDictionary.Add(CONSTANTS.PChangeRequest, "");
                        dataNumerical.Add(column, numericDictionary);
                    }
                }
            }
            _preProcessDTO.NumericalData = dataNumerical;
        }
        private void InsertToPreprocess(PreProcessModelDTO preProcessModel, string instaId)
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
                if (!string.IsNullOrEmpty(recommendedColumnsData) && recommendedColumnsData != "null")
                    outlierData = JObject.Parse(recommendedColumnsData);
                var columnBinning = Newtonsoft.Json.JsonConvert.SerializeObject(_preProcessDTO.ColumnBinning);
                if (!string.IsNullOrEmpty(columnBinning) && columnBinning != "null")
                    binningData = JObject.Parse(columnBinning);
                JObject binningObject = new JObject();
                if (binningData != null)
                    binningObject["ColumnBinning"] = JObject.FromObject(binningData);

                var prescription = Newtonsoft.Json.JsonConvert.SerializeObject(_preProcessDTO.Prescriptions);
                if (!string.IsNullOrEmpty(prescription) && prescription != "null")
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
                if (!string.IsNullOrEmpty(categoricalJson) && categoricalJson != "null")
                    categoricalObject = JObject.Parse(categoricalJson);
                if (!string.IsNullOrEmpty(numericJson) && numericJson != "null")
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
                smoteFlags.Add("Flag", CONSTANTS.False);
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
                processData["Flag"] = _preProcessDTO.Flag;
                //Removing the Target column having lessthan 2 values..important
                bool removeTargetColumn = false;
                if (categoricalObject != null && categoricalObject.ToString() != "{}")
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
                    processData[CONSTANTS.DataModification][CONSTANTS.Features][CONSTANTS.Interpolation] = "Linear";
                processData["Smote"] = smoteData;
                processData[CONSTANTS.InstaId] = instaId;
                processData[CONSTANTS.DataTransformationApplied] = _preProcessDTO.DataTransformationApplied;
                processData[CONSTANTS.CreatedOn] = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                processData[CONSTANTS.ModifiedOn] = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);

                bool DBEncryptionRequired = EncryptDB(_preProcessDTO.CorrelationId);
                //encrypt db data
                if (DBEncryptionRequired)
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
                var collection = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataProcessing);
                var insertdoc = BsonSerializer.Deserialize<BsonDocument>(processData.ToString());
                collection.InsertOne(insertdoc);
                insertSuccess = true;
            }
        }
        private DataEngineering GetDataCuration(string correlationId, string pageInfo, string userId, BsonDocument result)
        {
            bool callMethod = true;
            DataEngineering dataEngineering = new DataEngineering();
            string PythonResult = string.Empty;
            IngrainRequestQueue ingrainRequest = new IngrainRequestQueue();
            RemoveQueueRecords(correlationId, CONSTANTS.UpdateDataCleanUp);
            while (callMethod)
            {
                var useCaseData = CheckPythonProcess(correlationId, CONSTANTS.UpdateDataCleanUp);
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
                        Log(ingrainRequest, result);
                        return dataEngineering;
                    }
                    else if (dataEngineering.Status == "E")
                    {
                        Log(ingrainRequest, result);
                        callMethod = false;
                        dataEngineering.IsComplete = false;
                        return dataEngineering;
                    }
                    else
                    {
                        Thread.Sleep(1000);
                        callMethod = true;
                    }
                }
                else
                {
                    ingrainRequest._id = Guid.NewGuid().ToString();
                    ingrainRequest.CorrelationId = correlationId;
                    ingrainRequest.RequestId = Guid.NewGuid().ToString();
                    ingrainRequest.ProcessId = null;
                    ingrainRequest.Status = null;
                    ingrainRequest.ModelName = null;
                    ingrainRequest.RequestStatus = CONSTANTS.New;
                    ingrainRequest.RetryCount = 0;
                    ingrainRequest.ProblemType = null;
                    ingrainRequest.Message = null;
                    ingrainRequest.UniId = null;
                    ingrainRequest.Progress = null;
                    ingrainRequest.pageInfo = CONSTANTS.UpdateDataCleanUp;
                    ingrainRequest.ParamArgs = "{}";
                    ingrainRequest.Function = CONSTANTS.DataCleanUp;
                    ingrainRequest.CreatedByUser = userId;
                    ingrainRequest.CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                    ingrainRequest.ModifiedByUser = userId;
                    ingrainRequest.ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                    ingrainRequest.LastProcessedOn = null;
                    UpdateDataCleanup(correlationId);
                    var paramArg = "AutoRetrain";
                    ingrainRequest.ParamArgs = paramArg.ToJson();
                    InsertRequests(ingrainRequest);
                    Thread.Sleep(1000);
                }
            }
            return dataEngineering;
        }
        private void UpdateDataCleanup(string correlationId)
        {
            var ingestCollection = _database.GetCollection<BsonDocument>("DataCleanUP_FilteredData");
            var ingestFilter = Builders<BsonDocument>.Filter.Eq("CorrelationId", correlationId);
            var projection = Builders<BsonDocument>.Projection.Include("types").Include("removedcols").Exclude("_id");
            var result = ingestCollection.Find(ingestFilter).Project(projection).ToList();
            Dictionary<string, string> dataTypeColumns = new Dictionary<string, string>();
            List<string> removedColsList = new List<string>();
            JObject scaleModifiedCols = new JObject();
            if (result.Count > 0)
            {
                var datatypes = JObject.Parse(result[0].ToString())["types"];
                var removedCols = JObject.Parse(result[0].ToString())["removedcols"];
                foreach (var type in datatypes.Children())
                {
                    JProperty jProperty = type as JProperty;
                    if (jProperty != null)
                    {
                        dataTypeColumns.Add(jProperty.Name, jProperty.Value.ToString());
                    }
                }
                // added to remove the less data quality as suggested by Ravi and nitin
                foreach (var item in removedCols)
                {
                    dataTypeColumns.Remove(item.ToString());
                    removedColsList.Add(item.ToString());

                }
            }
            var collection = _database.GetCollection<BsonDocument>("DE_DataCleanup");
            var filter = Builders<BsonDocument>.Filter.Eq("CorrelationId", correlationId);
            var scaleUpdatemodify = Builders<BsonDocument>.Update.Set("ScaleModifiedColumns", scaleModifiedCols);
            collection.UpdateOne(filter, scaleUpdatemodify);
            string datatypeModifiedCols = string.Format("DtypeModifiedColumns");
            var datatypeUpdate = Builders<BsonDocument>.Update.Set(datatypeModifiedCols, dataTypeColumns);
            collection.UpdateOne(filter, datatypeUpdate);
        }
        private string CheckPythonProcess(string correlationId, string pageInfo)
        {
            string userModel = string.Empty;
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq(CONSTANTS.CorrelationId, correlationId) & builder.Eq(CONSTANTS.pageInfo, pageInfo);
            var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.Status).Include(CONSTANTS.Progress).
            Include(CONSTANTS.CorrelationId).Include(CONSTANTS.pageInfo).Include(CONSTANTS.Message).Include("RequestId").Exclude(CONSTANTS.Id);
            var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                userModel = result[0].ToJson();

            }
            return userModel;
        }
        private bool IsDataCurationComplete(string correlationId)
        {
            bool IsCompleted = false;
            List<string> columnsList = new List<string>();
            List<string> noDatatypeList = new List<string>();
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataCleanup);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.FeatureName).Exclude(CONSTANTS.Id);
            var resultData = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            JObject dataCuration = new JObject();
            bool DBEncryptionRequired = EncryptDB(correlationId);
            if (resultData.Count > 0)
            {
                //decrypt db data
                if (DBEncryptionRequired)
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
                                dataCuration[CONSTANTS.FeatureName][column][CONSTANTS.Datatype][CONSTANTS.Select_Option] = CONSTANTS.False;//testing purpose true
                                //string columnToUpdate = string.Format(CONSTANTS.SelectOption, column);
                                //var updateField = Builders<BsonDocument>.Update.Set(columnToUpdate, CONSTANTS.False);
                                //var updateResult = collection.UpdateOne(filter, updateField);
                                if (DBEncryptionRequired)
                                {
                                    var scaleUpdate = Builders<BsonDocument>.Update.Set(CONSTANTS.FeatureName, _IsAESKeyVault ? CryptographyUtility.Encrypt(dataCuration[CONSTANTS.FeatureName].ToString(Formatting.None)) : AesProvider.Encrypt(dataCuration[CONSTANTS.FeatureName].ToString(Formatting.None), _aesKey, _aesVector));
                                    collection.UpdateOne(filter, scaleUpdate);
                                }
                                else
                                {
                                    var Featuredata = BsonDocument.Parse(dataCuration[CONSTANTS.FeatureName].ToString());
                                    var scaleUpdate = Builders<BsonDocument>.Update.Set(CONSTANTS.FeatureName, Featuredata);
                                    collection.UpdateOne(filter, scaleUpdate);
                                }
                            }

                        }
                    }
                    if (!datatypeExist)
                    {
                        //string columnToUpdate = string.Format(CONSTANTS.DatatypeCategory, column);
                        //var updateField = Builders<BsonDocument>.Update.Set(columnToUpdate, CONSTANTS.True);
                        //var updateResult = collection.UpdateOne(filter, updateField);

                        dataCuration[CONSTANTS.FeatureName][column][CONSTANTS.Datatype][CONSTANTS.category] = CONSTANTS.True;
                        if (DBEncryptionRequired)
                        {
                            var scaleUpdate = Builders<BsonDocument>.Update.Set(CONSTANTS.FeatureName, _IsAESKeyVault ? CryptographyUtility.Encrypt(dataCuration[CONSTANTS.FeatureName].ToString(Formatting.None)) : AesProvider.Encrypt(dataCuration[CONSTANTS.FeatureName].ToString(Formatting.None), _aesKey, _aesVector));
                            collection.UpdateOne(filter, scaleUpdate);
                        }
                        else
                        {
                            var Featuredata = BsonDocument.Parse(dataCuration[CONSTANTS.FeatureName].ToString());
                            var scaleUpdate = Builders<BsonDocument>.Update.Set(CONSTANTS.FeatureName, Featuredata);
                            collection.UpdateOne(filter, scaleUpdate);
                        }





                        IsCompleted = true;
                    }
                }
            }
            return IsCompleted;
        }
        private void InsertRequests(IngrainRequestQueue ingrainRequest)
        {
            var requestQueue = JsonConvert.SerializeObject(ingrainRequest);
            var insertRequestQueue = BsonSerializer.Deserialize<BsonDocument>(requestQueue);
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
            collection.InsertOne(insertRequestQueue);
        }
        private void DeleteIngestRequest(string correlationId)
        {
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq(CONSTANTS.CorrelationId, correlationId) & builder.Eq(CONSTANTS.pageInfo, CONSTANTS.IngestData);
            var DelteCount = collection.DeleteMany(filter).DeletedCount;
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaAutoRetrainService), "DeleteIngestRequest", "Deleting previous REQUEST --" + correlationId, default(Guid), Convert.ToString(DelteCount), string.Empty, string.Empty, string.Empty);
        }
        private IngrainRequestQueue GetRequestDetails(string correlationId)
        {
            IngrainRequestQueue ingrainRequest = new IngrainRequestQueue();
            List<IngrainRequestQueue> result = new List<IngrainRequestQueue>();
            var collection = _database.GetCollection<IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
            var builder = Builders<IngrainRequestQueue>.Filter;
            var filter = builder.Eq(CONSTANTS.CorrelationId, correlationId) & builder.Eq(CONSTANTS.pageInfo, CONSTANTS.IngestData);
            result = collection.Find(filter).ToList();
            if (result.Count > 0)
            {
                return ingrainRequest = result[0];
            }
            return ingrainRequest;
        }
        private IngrainRequestQueue GetFileRequestStatus(string correlationId, string pageInfo)
        {
            IngrainRequestQueue ingrainRequest = new IngrainRequestQueue();
            var collection = _database.GetCollection<IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
            var builder = Builders<IngrainRequestQueue>.Filter;
            var filter = builder.Eq(CONSTANTS.CorrelationId, correlationId) & builder.Eq(CONSTANTS.pageInfo, pageInfo);
            return ingrainRequest = collection.Find(filter).ToList().FirstOrDefault();
        }
        private void InsertAutoLog(InstaLog log)
        {
            var requestQueue = JsonConvert.SerializeObject(log);
            var insertRequestQueue = BsonSerializer.Deserialize<BsonDocument>(requestQueue);
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.Insta_AutoLog);
            collection.InsertOne(insertRequestQueue);
        }
        private void RemoveQueueRecords(string correlationId, string pageInfo)
        {
            var builder = Builders<BsonDocument>.Filter;
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
            var filter = builder.Eq(CONSTANTS.CorrelationId, correlationId) & builder.Eq(CONSTANTS.pageInfo, pageInfo);
            var result = collection.DeleteMany(filter);
        }
        private InstaLog Log(IngrainRequestQueue requestQueue, BsonDocument result)
        {
            string instaId = null;
            string modelVersion = null;
            string deliveryconstructId = null;
            string clientUId = null;
            string sourceName = null;
            if (result.Contains(CONSTANTS.InstaId))
            {
                instaId = result[CONSTANTS.InstaId].ToString();
            }
            if (result.Contains(CONSTANTS.InstaID))
            {
                instaId = result[CONSTANTS.InstaID].ToString();
            }
            if (result.Contains(CONSTANTS.ModelVersion))
            {
                modelVersion = result[CONSTANTS.ModelVersion].ToString();
            }
            if (result.Contains(CONSTANTS.DeliveryConstructUID))
            {
                deliveryconstructId = result[CONSTANTS.DeliveryConstructUID].ToString();
            }
            if (result.Contains(CONSTANTS.DeliveryconstructId))
            {
                deliveryconstructId = result[CONSTANTS.DeliveryconstructId].ToString();
            }
            if (result.Contains(CONSTANTS.ClientUId))
            {
                clientUId = result[CONSTANTS.ClientUId].ToString();
            }
            if (result.Contains(CONSTANTS.ClientId))
            {
                clientUId = result[CONSTANTS.ClientId].ToString();
            }
            if (result.Contains("SourceName"))
            {
                sourceName = result["SourceName"].ToString();
            }
            InstaLog _instaLog = new InstaLog
            {
                _id = Guid.NewGuid().ToString(),
                InstaId = instaId,
                CorrelationId = result[CONSTANTS.CorrelationId].ToString(),
                ModelName = result[CONSTANTS.ModelName].ToString(),
                Status = requestQueue.Status,
                ModelVersion = modelVersion,
                Message = requestQueue.Message,
                ErrorMessage = requestQueue.Message,
                PageInfo = requestQueue.pageInfo,
                DeliveryConstructUID = deliveryconstructId,
                ClientUId = clientUId,
                SourceName = sourceName,
                StartDate = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                CreatedOn = result[CONSTANTS.CreatedOn].ToString(),
                CreatedByUser = result[CONSTANTS.CreatedByUser].ToString(),
                ModifiedOn = result[CONSTANTS.ModifiedOn].ToString(),
                ModifiedByUser = result["ModifiedByUser"].ToString()
            };
            return _instaLog;
        }

        private bool EncryptDB(string correlationid)
        {
            var collection = _database.GetCollection<BsonDocument>("SSAI_DeployedModels");
            var filter = Builders<BsonDocument>.Filter.Eq("CorrelationId", correlationid);
            var projection = Builders<BsonDocument>.Projection.Include("DBEncryptionRequired").Include("CorrelationId").Exclude("_id");
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
        public void SendVDSDeployModelNotification(string correlationId, string operation)
        {
            var deployModel = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
            var filter = Builders<DeployModelsDto>.Filter.Where(x => x.CorrelationId == correlationId);
            var projection = Builders<DeployModelsDto>.Projection.Exclude("_id");
            var model = deployModel.Find(filter).Project<DeployModelsDto>(projection).FirstOrDefault();
            List<string> sourceLst = new List<string>() { "Custom", "pad", "metric", "Cascading", "multidatasource" };
            List<string> appLst = new List<string>() { CONSTANTS.VDSApplicationID_PAD, CONSTANTS.VDSApplicationID_FDS, CONSTANTS.VDSApplicationID_PAM };

            bool isValidSource = sourceLst.Contains(model.SourceName);
            if (model.SourceName == "multidatasource")
            {
                var collection3 = _database.GetCollection<BsonDocument>("PS_MultiFileColumn");
                var builder3 = Builders<BsonDocument>.Projection.Include("CorrelationId").Include("File").Include("Flag").Exclude("_id");
                var columnFilter3 = Builders<BsonDocument>.Filter.Eq("CorrelationId", correlationId);
                var result3 = collection3.Find(columnFilter3).Project<BsonDocument>(builder3).FirstOrDefault();
                if (result3 != null)
                {
                    bool entityFlag = true;
                    try
                    {
                        JObject res = JObject.Parse(result3.ToJson());
                        JObject file = res["File"] as JObject;
                        foreach (var obj in file)
                        {
                            JObject subObj = obj.Value as JObject;
                            if (subObj["FileExtensionOrig"].ToString() != "Entity")
                            {
                                entityFlag = false;
                            }

                        }
                    }
                    catch (Exception ex)
                    {
                        entityFlag = false;
                        LOGGING.LogManager.Logger.LogErrorMessage(typeof(InstaAutoRetrainService), nameof(SendVDSDeployModelNotification), ex.Message, ex, model.AppId, string.Empty, model.ClientUId, model.DeliveryConstructUID);
                    }

                    if (!entityFlag)
                        isValidSource = false;



                }
            }

            if (appLst.Contains(model.AppId) && isValidSource)
            {
                AppNotificationLog appNotificationLog = new AppNotificationLog();
                if (!model.IsModelTemplate)
                {
                    appNotificationLog.ClientUId = model.ClientUId;
                    appNotificationLog.DeliveryConstructUId = model.DeliveryConstructUID;
                }

                appNotificationLog.CorrelationId = model.CorrelationId;
                appNotificationLog.UseCaseId = model.CorrelationId;// for only vds charts
                appNotificationLog.ProblemType = model.ModelType;
                if (model.IsModelTemplate)
                    appNotificationLog.ModelType = "ModelTemplate";
                else if (model.IsPrivate)
                    appNotificationLog.ModelType = "Private";
                else
                    appNotificationLog.ModelType = "Public";
                appNotificationLog.ApplicationId = model.AppId;
                appNotificationLog.OperationType = operation;
                appNotificationLog.FunctionalArea = model.Category;
                appNotificationLog.NotificationEventType = "DeployModel";
                if (model.Category == "Others")
                    appNotificationLog.FunctionalArea = "General";
                if (model.Category == "PPM")
                    appNotificationLog.FunctionalArea = "RIAD";
                if (model.IsUpdated == "True")
                {
                    appNotificationLog.OperationType = "Updated";
                }

                SendAppNotification(appNotificationLog);
            }


        }


        public void SendAppNotification(AppNotificationLog appNotificationLog)
        {
            appNotificationLog.RequestId = Guid.NewGuid().ToString();
            appNotificationLog.CreatedOn = DateTime.UtcNow.ToString();
            appNotificationLog.RetryCount = 0;
            appNotificationLog.IsNotified = false;
            if (appNotificationLog.ProblemType == "Multi_Class")
                appNotificationLog.ProblemType = "Classification";

            var appIntegrationCollection = _database.GetCollection<AppIntegration>(CONSTANTS.AppIntegration);
            var filter = Builders<AppIntegration>.Filter.Where(x => x.ApplicationID == appNotificationLog.ApplicationId);
            var app = appIntegrationCollection.Find(filter).FirstOrDefault();

            if (app != null)
            {
                appNotificationLog.Environment = app.Environment;
                if (app.Environment == "PAD")
                {
                    Uri apiUri = new Uri(_myWizardAPIUrl);
                    string host = apiUri.GetLeftPart(UriPartial.Authority);
                    appNotificationLog.AppNotificationUrl = host + "/" + app.AppNotificationUrl;
                }
                else
                {
                    appNotificationLog.AppNotificationUrl = app.AppNotificationUrl;
                }
            }
            else
            {
                throw new KeyNotFoundException("ApplicationId not found");
            }


            var notificationCollection = _database.GetCollection<AppNotificationLog>("AppNotificationLog");
            notificationCollection.InsertOne(appNotificationLog);
        }

        public Object getDeployedModelAccuracy(string correlationId, string modelName)
        {
            var value = new Object();
            var collection = _database.GetCollection<BsonDocument>("SSAI_RecommendedTrainedModels");
            var filter = Builders<BsonDocument>.Filter.Eq("CorrelationId", correlationId) & Builders<BsonDocument>.Filter.Eq("modelName", modelName);
            var projection = Builders<BsonDocument>.Projection.Include("r2ScoreVal").Include("CorrelationId").Include("Accuracy").Include("ProblemType").Exclude("_id");
            var data = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (data.Count > 0)
            {
                if (data[0].Contains("r2ScoreVal"))
                {
                    value = data[0]["r2ScoreVal"]["error_rate"].ToString();
                }
                else if (data[0].Contains("Accuracy"))
                {
                    value = data[0]["Accuracy"].ToString();
                }
            }
            return value;
        }

        public string UpdateDeployedModelHealth(BsonDocument result)
        {
            var modelHeath = "";
            string correlationId = result["CorrelationId"].ToString();
            var collection = _database.GetCollection<BsonDocument>("SSAI_DeployedModels");
            var filter = Builders<BsonDocument>.Filter.Eq("CorrelationId", correlationId);
            var projection = Builders<BsonDocument>.Projection.Include("ModelHealth").Include("CorrelationId").Exclude("_id");
            var data = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (data.Count > 0)
            {
                modelHeath = data[0]["ModelHealth"].ToString();
                var newFieldUpdate = Builders<BsonDocument>.Update.Set("ModelHealth", "Healthy");
                var updated = collection.UpdateOne(filter, newFieldUpdate);

            }
            var collection2 = _database.GetCollection<BsonDocument>("ModelMetrics");
            var filter2 = Builders<BsonDocument>.Filter.Eq("CorrelationId", correlationId);
            var projection2 = Builders<BsonDocument>.Projection.Exclude("_id").Include("CorrelationId").Include("ModelHealth").Include("ModifiedOn");
            var result2 = collection2.Find(filter2).SortByDescending(item => item["ModifiedOn"]).Project<BsonDocument>(projection2).ToList();
            if (result2.Count > 0)
            {
                modelHeath = result2[0]["ModelHealth"].ToString();
                var newFieldUpdate = Builders<BsonDocument>.Update.Set("ModelHealth", "Healthy");
                var updated = collection2.UpdateMany(filter2, newFieldUpdate);
            }
            return modelHeath;
        }


        public InstaRetrain InitiateModelMonitor(BsonDocument result)
        {
            string correlationId = result["CorrelationId"].ToString();
            var data = this.getDeployedModelAccuracy(correlationId, result["ModelVersion"].ToString());
            bool callMethod = true;
            InstaRetrain dataEngineering = new InstaRetrain();
            string PythonResult = string.Empty;
            IngrainRequestQueue ingrainRequest = new IngrainRequestQueue();
            RemoveQueueRecords(correlationId, "ModelMonitor");
            while (callMethod)
            {
                var useCaseData = CheckPythonProcess(correlationId, "ModelMonitor");
                if (!string.IsNullOrEmpty(useCaseData))
                {
                    JObject queueData = JObject.Parse(useCaseData);
                    dataEngineering.Status = (string)queueData[CONSTANTS.Status];
                    // dataEngineering.Progress = (string)queueData[CONSTANTS.Progress];
                    dataEngineering.Message = (string)queueData[CONSTANTS.Message];
                    if (dataEngineering.Status == CONSTANTS.C)
                    {
                        var healthStatus = ModelMetrics(correlationId, (string)queueData["RequestId"]);
                        dataEngineering.Message = healthStatus;
                        callMethod = false;
                        //    dataEngineering.IsComplete = true;
                        //Log(ingrainRequest, result);
                        return dataEngineering;
                    }
                    else if (dataEngineering.Status == "E")
                    {
                        // Log(ingrainRequest, result);
                        callMethod = false;
                        //  dataEngineering.IsComplete = false;
                        return dataEngineering;
                    }
                    else
                    {
                        Thread.Sleep(1000);
                        callMethod = true;
                    }
                }
                else
                {
                    string newId = Guid.NewGuid().ToString();
                    ingrainRequest._id = Guid.NewGuid().ToString();
                    ingrainRequest.CorrelationId = correlationId;
                    ingrainRequest.RequestId = newId;//Guid.NewGuid().ToString();
                    ingrainRequest.ProcessId = null;
                    ingrainRequest.Status = null;
                    ingrainRequest.ModelName = result["ModelName"].ToString();
                    ingrainRequest.RequestStatus = CONSTANTS.New;
                    ingrainRequest.RetryCount = 0;
                    ingrainRequest.ProblemType = null;
                    ingrainRequest.Message = null;
                    ingrainRequest.UniId = newId;
                    ingrainRequest.Progress = null;
                    ingrainRequest.pageInfo = "ModelMonitor";
                    ingrainRequest.ParamArgs = "{}";
                    ingrainRequest.Function = "ModelMonitor";
                    ingrainRequest.CreatedByUser = result["CreatedByUser"].ToString();
                    ingrainRequest.CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                    ingrainRequest.ModifiedByUser = result["CreatedByUser"].ToString();
                    ingrainRequest.ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                    ingrainRequest.LastProcessedOn = null;
                    //ingrainRequest.Accuracy = data;
                    ingrainRequest.TriggerType = "Scheduler";
                    var paramArg = new ModelMetric()
                    {
                        DeployedAccuracy = data,
                        ModelName = result["ModelVersion"].ToString()
                    };

                    ingrainRequest.ParamArgs = paramArg.ToJson();
                    // UpdateDataCleanup(correlationId);
                    InsertRequests(ingrainRequest);
                    Thread.Sleep(1000);
                }
            }
            return dataEngineering;
        }

        public InstaRetrain SPAAmbulanceIngestData(BsonDocument result)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaAutoRetrainService), "SPAAmbulanceIngestData", "SPAAmbulanceIngestData START", string.IsNullOrEmpty(Convert.ToString(result[CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[CONSTANTS.CorrelationId])), string.Empty, string.Empty, string.Empty, string.Empty);
            Filepath filepath = new Filepath();
            filepath.fileList = CONSTANTS.Null;
            IngrainRequestQueue ingrainRequest = null;
            IngrainRequestQueue bsonElements = new IngrainRequestQueue();
            try
            {
                bsonElements = GetRequestDetails(result[CONSTANTS.CorrelationId].ToString());
                if (bsonElements.ParamArgs != null)
                {
                    ingrainRequest = new IngrainRequestQueue
                    {
                        _id = Guid.NewGuid().ToString(),
                        CorrelationId = result[CONSTANTS.CorrelationId].ToString(),
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
                        TemplateUseCaseID = result["TemplateUseCaseID"].ToString(),//bsonElements.TemplateUseCaseID,
                        AppID = result["AppID"].ToString(),
                        UseCaseID = CONSTANTS.Null,
                        pageInfo = CONSTANTS.IngestData,
                        Function = "FileUpload",
                        CreatedByUser = result[CONSTANTS.CreatedByUser].ToString(),
                        CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                        ModifiedByUser = result[CONSTANTS.CreatedByUser].ToString(),
                        ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                        LastProcessedOn = null,
                    };
                    var fileUpload = JsonConvert.DeserializeObject<SPAFileUpload>(bsonElements.ParamArgs);
                    //var aa = JsonConvert.DeserializeObject<pad>(fileUpload.pad);
                    var lastDataPull = GetLastDataDict(result[CONSTANTS.CorrelationId].ToString());
                    if (string.IsNullOrEmpty(lastDataPull))
                    {

                        var modelCollection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
                        var projection1 = Builders<DeployModelsDto>.Projection.Exclude(CONSTANTS.Id);
                        var filterBuilder = Builders<DeployModelsDto>.Filter;
                        var filter2 = filterBuilder.Eq(CONSTANTS.CorrelationId, result[CONSTANTS.CorrelationId].ToString()) & filterBuilder.Eq(CONSTANTS.Status, "Deployed");
                        var modelsData = modelCollection.Find(filter2).Project<DeployModelsDto>(projection1).ToList();
                        if (modelsData.Count > 0)
                        {
                            var createdDate = Convert.ToDateTime(modelsData[0].CreatedOn).AddYears(-2).ToString("yyyy-MM-dd");
                            if (Convert.ToString(fileUpload.Customdetails) != "null" && Convert.ToString(fileUpload.Customdetails.InputParameters) != "null")
                                fileUpload.Customdetails.InputParameters.StartDate = createdDate;
                        }
                    }
                    else
                    {
                        if (Convert.ToString(fileUpload.Customdetails) != "null" && Convert.ToString(fileUpload.Customdetails.InputParameters) != "null")
                            fileUpload.Customdetails.InputParameters.StartDate = Convert.ToDateTime(lastDataPull).ToString("yyyy-MM-dd");
                    }

                    if (Convert.ToString(fileUpload.Customdetails) != "null")
                    {
                        fileUpload.Customdetails.DateColumn = CONSTANTS.Null;
                        if (Convert.ToString(fileUpload.Customdetails.InputParameters) != "null")
                        {
                            fileUpload.Customdetails.InputParameters.StartDate = Convert.ToDateTime(GetLastDataDict(result[CONSTANTS.CorrelationId].ToString())).ToString("yyyy-MM-dd");//fileUpload.Customdetails.InputParameters.EndDate;
                            fileUpload.Customdetails.InputParameters.EndDate = DateTime.Now.ToString("yyyy-MM-dd");
                            if (string.IsNullOrEmpty(fileUpload.Customdetails.InputParameters.TeamAreaUId))
                                fileUpload.Customdetails.InputParameters.TeamAreaUId = CONSTANTS.Null;

                            if (string.IsNullOrEmpty(fileUpload.Customdetails.InputParameters.IterationUId))
                                fileUpload.Customdetails.InputParameters.IterationUId = CONSTANTS.Null;

                            var mappingData = this.GetPublicMapping(ingrainRequest.TemplateUseCaseID, "");
                            fileUpload.Customdetails.InputParameters.IterationUId = mappingData[0].IterationUID;
                        }
                    }


                    fileUpload.Flag = source;

                    //fileUpload.Customdetails.InputParameters.StartDate = Convert.ToDateTime(GetLastDataDict(result[CONSTANTS.CorrelationId].ToString())).ToString("yyyy-MM-dd");//fileUpload.Customdetails.InputParameters.EndDate;
                    //if (fileUpload.Customdetails != "null" && fileUpload.Customdetails.InputParameters != "null") 
                    //    fileUpload.Customdetails.InputParameters.EndDate = DateTime.Now.ToString("yyyy-MM-dd");
                    //fileUpload.Flag = source;
                    //if (fileUpload.Customdetails != "null" && fileUpload.Customdetails.InputParameters != "null" && string.IsNullOrEmpty(fileUpload.Customdetails.InputParameters.TeamAreaUId))
                    //{
                    //    fileUpload.Customdetails.InputParameters.TeamAreaUId = CONSTANTS.Null;
                    //}
                    //if (fileUpload.Customdetails != "null" && fileUpload.Customdetails.InputParameters != "null" && string.IsNullOrEmpty(fileUpload.Customdetails.InputParameters.IterationUId))
                    //{
                    //    fileUpload.Customdetails.InputParameters.IterationUId = CONSTANTS.Null;
                    //}
                    //var mappingData = this.GetPublicMapping(ingrainRequest.TemplateUseCaseID, "");
                    //if (fileUpload.Customdetails.InputParameters != "null") 
                    //    fileUpload.Customdetails.InputParameters.IterationUId = mappingData[0].IterationUID;
                    //if (fileUpload.Customdetails != "null") 
                    //    fileUpload.Customdetails.DateColumn = CONSTANTS.Null;
                    ingrainRequest.ParamArgs = fileUpload.ToJson();
                    GetIngestData(result, ingrainRequest);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InstaAutoRetrainService), nameof(SPAAmbulanceIngestData) + "SPAAmbulanceIngestData END CORRELATIONID --" + Convert.ToString(result[CONSTANTS.CorrelationId]), ex.Message + "- STACKTRACE - " + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaAutoRetrainService), "SPAAmbulanceIngestData", "SPAAmbulanceIngestData END", string.IsNullOrEmpty(Convert.ToString(result[CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[CONSTANTS.CorrelationId])), string.Empty, string.Empty, string.Empty, string.Empty);
            return _instaRetrain;
        }

        private List<PublicTemplateMapping> GetPublicMapping(string UsecaseID, string AppId)
        {
            var MappingCollection = _database.GetCollection<PublicTemplateMapping>(CONSTANTS.PublicTemplateMapping);
            var filterBuilder = Builders<PublicTemplateMapping>.Filter;
            var Projection = Builders<PublicTemplateMapping>.Projection.Exclude(CONSTANTS.Id);
            //var filter = filterBuilder.Eq("UsecaseID", UsecaseID) & filterBuilder.Eq(CONSTANTS.ApplicationID, AppId);
            var filter = filterBuilder.Eq("UsecaseID", UsecaseID);
            var result = MappingCollection.Find(filter).Project<PublicTemplateMapping>(Projection).ToList();
            List<PublicTemplateMapping> MappingList = null;
            if (result.Count() > 0)
            {
                if (result[0].DateColumn == null)
                {
                    result[0].DateColumn = "DateColumn";
                }
                MappingList = JsonConvert.DeserializeObject<List<PublicTemplateMapping>>(JsonConvert.SerializeObject(result));
            }
            return MappingList;
        }
        public string ModelMetrics(string correlationId, string requestId)
        {
            string modelHealth = string.Empty;
            var collection2 = _database.GetCollection<BsonDocument>("ModelMetrics");
            var filter2 = Builders<BsonDocument>.Filter.Eq("CorrelationId", correlationId) & Builders<BsonDocument>.Filter.Eq("RequestId", requestId);
            var projection2 = Builders<BsonDocument>.Projection.Exclude("_id").Include("CorrelationId").Include("ModelHealth").Include("ModifiedOn");
            var result2 = collection2.Find(filter2).SortByDescending(item => item["ModifiedOn"]).Project<BsonDocument>(projection2).ToList();
            if (result2.Count > 0)
            {
                modelHealth = result2[0]["ModelHealth"].ToString();

            }

            return modelHealth;
        }

        public InstaRetrain SPAIngestData(BsonDocument result)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaAutoRetrainService), "SPAIngestData", "SPAIngestData START", string.IsNullOrEmpty(Convert.ToString(result[CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[CONSTANTS.CorrelationId])), string.Empty, string.Empty, string.Empty, string.Empty);
            Filepath filepath = new Filepath();
            filepath.fileList = CONSTANTS.Null;
            IngrainRequestQueue ingrainRequest = null;
            IngrainRequestQueue bsonElements = new IngrainRequestQueue();
            try
            {
                bsonElements = GetRequestDetails(result[CONSTANTS.CorrelationId].ToString());
                if (bsonElements.ParamArgs != null)
                {
                    string appId = null;
                    string templateUsecaseId = null;
                    if (result.Contains("AppId")) //AppId
                    {
                        appId = Convert.ToString(result["AppId"]) != "BsonNull" ? Convert.ToString(result["AppId"]) : null;
                    }
                    if (result.Contains("TemplateUsecaseId"))
                    {
                        templateUsecaseId = Convert.ToString(result["TemplateUsecaseId"]) != "BsonNull" ? Convert.ToString(result["TemplateUsecaseId"]) : null;
                    }
                    ingrainRequest = new IngrainRequestQueue
                    {
                        _id = Guid.NewGuid().ToString(),
                        CorrelationId = result[CONSTANTS.CorrelationId].ToString(),
                        RequestId = Guid.NewGuid().ToString(),
                        ProcessId = null,
                        AppID = appId,
                        Status = null,
                        ModelName = null,
                        RequestStatus = CONSTANTS.New,
                        RetryCount = 0,
                        ProblemType = null,
                        Message = null,
                        UniId = null,
                        Progress = null,
                        TemplateUseCaseID = templateUsecaseId,
                        UseCaseID = CONSTANTS.Null,
                        pageInfo = CONSTANTS.IngestData,
                        Function = "FileUpload",
                        CreatedByUser = result[CONSTANTS.CreatedByUser].ToString(),
                        CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                        ModifiedByUser = result[CONSTANTS.CreatedByUser].ToString(),
                        ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                        LastProcessedOn = null,
                    };
                    var fileUpload = JsonConvert.DeserializeObject<SPAFileUpload>(bsonElements.ParamArgs);
                    //var aa = JsonConvert.DeserializeObject<pad>(fileUpload.pad);
                    var lastDataPull = GetLastDataDict(result[CONSTANTS.CorrelationId].ToString());
                    if (string.IsNullOrEmpty(lastDataPull))
                    {

                        var modelCollection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
                        var projection1 = Builders<DeployModelsDto>.Projection.Exclude(CONSTANTS.Id);
                        var filterBuilder = Builders<DeployModelsDto>.Filter;
                        var filter2 = filterBuilder.Eq(CONSTANTS.CorrelationId, result[CONSTANTS.CorrelationId].ToString()) & filterBuilder.Eq(CONSTANTS.Status, "Deployed");
                        var modelsData = modelCollection.Find(filter2).Project<DeployModelsDto>(projection1).ToList();
                        if (modelsData.Count > 0)
                        {
                            var createdDate = Convert.ToDateTime(modelsData[0].CreatedOn).AddYears(-2).ToString("yyyy-MM-dd");
                            if (Convert.ToString(fileUpload.Customdetails) != "null" && Convert.ToString(fileUpload.Customdetails.InputParameters) != "null")
                                fileUpload.Customdetails.InputParameters.StartDate = createdDate;
                        }
                    }
                    else
                    {
                        //if (fileUpload.Customdetails != "null" && fileUpload.Customdetails.InputParameters != "null")  
                        if (Convert.ToString(fileUpload.Customdetails) != "null" && Convert.ToString(fileUpload.Customdetails.InputParameters) != "null")
                            fileUpload.Customdetails.InputParameters.StartDate = Convert.ToDateTime(lastDataPull).ToString("yyyy-MM-dd");
                    }
                    //fileUpload.Customdetails.InputParameters.StartDate = Convert.ToDateTime(fileUpload.Customdetails.InputParameters.EndDate).ToString("yyyy-MM-dd");
                    if (result["TemplateUsecaseId"].ToString() == "f0320924-2ee3-4398-ad7c-8bc172abd78d"
                        || result["TemplateUsecaseId"].ToString() == "49bf29ca-3408-4ae1-af4d-32963e18670a"
                        || result["TemplateUsecaseId"].ToString() == "169881ad-dc85-4bf8-bc67-7b1212836a97"
                        || result["TemplateUsecaseId"].ToString() == "2dcf1c54-099b-4711-b9a2-f08ad96d71b7"
                        || result["TemplateUsecaseId"].ToString() == "49bf29ca-3408-4ae1-af4d-32963e18670a"
                        || result["TemplateUsecaseId"].ToString() == "5cab6ea1-8af4-4f74-8359-e053629d2b98"
                        || result["TemplateUsecaseId"].ToString() == "64a6c5be-0ecb-474e-b970-06c960d6f7b7"
                        || result["TemplateUsecaseId"].ToString() == "668bb66a-86c6-46e6-9f98-c0bc9b3e4eb2"
                        || result["TemplateUsecaseId"].ToString() == "6761146a-0eef-4b39-8dd8-33c786e4fb86"
                        || result["TemplateUsecaseId"].ToString() == "68bf25f3-6df8-4e14-98fa-34918d2eeff1"
                        || result["TemplateUsecaseId"].ToString() == "6b0d8eb3-0918-4818-9655-6ca81a4ebf30"
                        || result["TemplateUsecaseId"].ToString() == "877f712c-7cdc-435a-acc8-8fea3d26cc18"
                        || result["TemplateUsecaseId"].ToString() == "be0c67a1-4320-461e-9aff-06a545824a32"
                        || result["TemplateUsecaseId"].ToString() == "efd60535-5d15-46a8-961f-c43161e3a326"
                        || result["TemplateUsecaseId"].ToString() == "8d3772f2-f19b-4403-840d-2fb230ac630f"
                        || result["TemplateUsecaseId"].ToString() == "5e307382-b102-4c06-858f-8346d81710fe"
                        || result["TemplateUsecaseId"].ToString() == "fa52b2ab-6d7f-4a97-b834-78af04791ddf")
                    {
                        //if (fileUpload.Customdetails != "null" && fileUpload.Customdetails.InputParameters != "null")
                        if (Convert.ToString(fileUpload.Customdetails) != "null" && Convert.ToString(fileUpload.Customdetails.InputParameters) != "null")
                            fileUpload.Customdetails.InputParameters.EndDate = DateTime.Now.ToString("yyyy-MM-dd");
                    }
                    else
                    {
                        if (Convert.ToString(fileUpload.Customdetails) != "null" && Convert.ToString(fileUpload.Customdetails.InputParameters) != "null")
                        {
                            fileUpload.Customdetails.InputParameters.EndDate = DateTime.Now.ToString("yyyy-MM-dd");
                            if (string.IsNullOrEmpty(fileUpload.Customdetails.InputParameters.TeamAreaUId))
                            {
                                fileUpload.Customdetails.InputParameters.TeamAreaUId = CONSTANTS.Null;
                            }
                            if (string.IsNullOrEmpty(fileUpload.Customdetails.InputParameters.IterationUId))
                            {
                                fileUpload.Customdetails.InputParameters.IterationUId = CONSTANTS.Null;
                            }
                        }

                        if (Convert.ToString(fileUpload.Customdetails) != "null")
                        {
                            fileUpload.Customdetails.DateColumn = CONSTANTS.Null;
                            if (string.IsNullOrEmpty(fileUpload.Customdetails.UsecaseID))
                            {
                                fileUpload.Customdetails.UsecaseID = CONSTANTS.Null;
                            }
                        }
                        fileUpload.Flag = source;


                        //if (fileUpload.Customdetails != "null" && fileUpload.Customdetails.InputParameters != "null") 
                        //    fileUpload.Customdetails.InputParameters.EndDate = DateTime.Now.ToString("yyyy-MM-dd");                        
                        //if (fileUpload.Customdetails != "null" && fileUpload.Customdetails.InputParameters != "null" && string.IsNullOrEmpty(fileUpload.Customdetails.InputParameters.TeamAreaUId))
                        //{
                        //    fileUpload.Customdetails.InputParameters.TeamAreaUId = CONSTANTS.Null;
                        //}
                        //if (fileUpload.Customdetails != "null" && fileUpload.Customdetails.InputParameters != "null" && string.IsNullOrEmpty(fileUpload.Customdetails.InputParameters.IterationUId))
                        //{
                        //    fileUpload.Customdetails.InputParameters.IterationUId = CONSTANTS.Null;
                        //}

                        //if (fileUpload.Customdetails != "null") fileUpload.Customdetails.DateColumn = CONSTANTS.Null;

                        //if (fileUpload.Customdetails != "null" && string.IsNullOrEmpty(fileUpload.Customdetails.UsecaseID))
                        //    fileUpload.Customdetails.UsecaseID = CONSTANTS.Null;
                        //fileUpload.Flag = source;
                    }

                    ingrainRequest.ParamArgs = fileUpload.ToJson();
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaAutoRetrainService), "GetIngestData", "GetIngestData Started", string.IsNullOrEmpty(Convert.ToString(result[CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[CONSTANTS.CorrelationId])), string.Empty, string.Empty, string.Empty, string.Empty);
                    GetIngestData(result, ingrainRequest);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InstaAutoRetrainService), nameof(SPAIngestData), ex.Message + "- STACKTRACE - " + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaAutoRetrainService), "SPAIngestData", "SPAIngestData END", string.IsNullOrEmpty(Convert.ToString(result[CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[CONSTANTS.CorrelationId])), string.Empty, string.Empty, string.Empty, string.Empty);
            return _instaRetrain;
        }

        public InstaRetrain IAIngestData(BsonDocument result)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaAutoRetrainService), "IAIngestData", "IAIngestData START", string.IsNullOrEmpty(Convert.ToString(result[CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[CONSTANTS.CorrelationId])), string.Empty, string.Empty, string.Empty, string.Empty);
            Filepath filepath = new Filepath();
            filepath.fileList = CONSTANTS.Null;
            IngrainRequestQueue ingrainRequest = null;
            IngrainRequestQueue bsonElements = new IngrainRequestQueue();
            try
            {
                bsonElements = GetRequestDetails(result[CONSTANTS.CorrelationId].ToString());
                if (bsonElements.ParamArgs != null)
                {
                    string appId = null;
                    string templateUsecaseId = null;
                    if (result.Contains("AppId"))
                    {
                        appId = result["AppId"].ToString() != "BsonNull" ? result["AppId"].ToString() : null;
                    }
                    if (result.Contains("TemplateUsecaseId"))
                    {
                        templateUsecaseId = result["TemplateUsecaseId"].ToString() != "BsonNull" ? result["TemplateUsecaseId"].ToString() : null;
                    }
                    ingrainRequest = new IngrainRequestQueue
                    {
                        _id = Guid.NewGuid().ToString(),
                        CorrelationId = result[CONSTANTS.CorrelationId].ToString(),
                        RequestId = Guid.NewGuid().ToString(),
                        ProcessId = null,
                        AppID = appId,
                        Status = null,
                        ModelName = null,
                        RequestStatus = CONSTANTS.New,
                        RetryCount = 0,
                        ProblemType = null,
                        Message = null,
                        UniId = null,
                        Progress = null,
                        TemplateUseCaseID = templateUsecaseId,
                        UseCaseID = CONSTANTS.Null,
                        pageInfo = CONSTANTS.IngestData,
                        Function = "FileUpload",
                        CreatedByUser = result[CONSTANTS.CreatedByUser].ToString(),
                        CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                        ModifiedByUser = result[CONSTANTS.CreatedByUser].ToString(),
                        ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                        LastProcessedOn = null,
                    };
                    var fileUpload = JsonConvert.DeserializeObject<ParamArgsWithCustomFlag>(bsonElements.ParamArgs);
                    //var aa = JsonConvert.DeserializeObject<pad>(fileUpload.pad);
                    var lastDataPull = GetLastDataDict(result[CONSTANTS.CorrelationId].ToString());
                    if (string.IsNullOrEmpty(lastDataPull))
                    {

                        var modelCollection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
                        var projection1 = Builders<DeployModelsDto>.Projection.Exclude(CONSTANTS.Id);
                        var filterBuilder = Builders<DeployModelsDto>.Filter;
                        var filter2 = filterBuilder.Eq(CONSTANTS.CorrelationId, result[CONSTANTS.CorrelationId].ToString()) & filterBuilder.Eq(CONSTANTS.Status, "Deployed");
                        var modelsData = modelCollection.Find(filter2).Project<DeployModelsDto>(projection1).ToList();
                        if (modelsData.Count > 0)
                        {
                            var createdDate = Convert.ToDateTime(modelsData[0].CreatedOn).AddYears(-2).ToString("yyyy-MM-dd");
                            fileUpload.Customdetails.InputParameters.FromDate = createdDate;
                        }
                    }
                    else
                    {
                        fileUpload.Customdetails.InputParameters.FromDate = Convert.ToDateTime(lastDataPull).ToString("yyyy-MM-dd");
                    }

                    fileUpload.Customdetails.InputParameters.ToDate = DateTime.Now.ToString("yyyy-MM-dd");
                    fileUpload.Flag = source;
                    fileUpload.Customdetails.DateColumn = CONSTANTS.Null;
                    fileUpload.Customdetails.InputParameters.DateColumn = CONSTANTS.Null;
                    if (string.IsNullOrEmpty(fileUpload.Customdetails.UsecaseID))
                        fileUpload.Customdetails.UsecaseID = CONSTANTS.Null;
                    ingrainRequest.ParamArgs = JsonConvert.SerializeObject(fileUpload);
                    GetIngestData(result, ingrainRequest);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InstaAutoRetrainService), nameof(IAIngestData), ex.Message + "- STACKTRACE - " + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaAutoRetrainService), "IAIngestData", "IAIngestData END", string.IsNullOrEmpty(Convert.ToString(result[CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[CONSTANTS.CorrelationId])), string.Empty, string.Empty, string.Empty, string.Empty);
            return _instaRetrain;
        }

        public InstaRetrain GetSPADeployPrediction(BsonDocument result, string modelType)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaAutoRetrainService), nameof(GetSPADeployPrediction), "START", string.IsNullOrEmpty(Convert.ToString(result[CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[CONSTANTS.CorrelationId])), string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                var deployModelType = string.Empty;
                if (string.IsNullOrEmpty(modelType))
                {
                    deployModelType = result["ModelType"].ToString();
                }
                else
                {
                    deployModelType = modelType;
                }
                var dataEngineering = InstaAutoDeployModel(result[CONSTANTS.CorrelationId].ToString(), result[CONSTANTS.CreatedByUser].ToString(), deployModelType);
                if (dataEngineering.Status == CONSTANTS.C)
                {
                    _instaRetrain.Status = CONSTANTS.C;
                    _instaRetrain.Success = true;
                    var predictionResult = SPAGetPrediction(result[CONSTANTS.CorrelationId].ToString(), result[CONSTANTS.CreatedByUser].ToString());
                    if (predictionResult.Status == CONSTANTS.C)
                    {
                        _instaRetrain.Status = CONSTANTS.C;
                        _instaRetrain.IsPredictionSucess = true;
                        _instaRetrain.Success = true;
                        _instaRetrain.Message = "Prediction completed Successfully";
                    }
                    else
                    {
                        _instaRetrain.Status = "E";
                        _instaRetrain.IsPredictionSucess = false;
                        _instaRetrain.Message = "Prediction failed";
                        _instaRetrain.Success = false;
                    }
                }
                else if (dataEngineering.Status == CONSTANTS.E)
                {
                    _instaRetrain.Status = CONSTANTS.E;
                    _instaRetrain.Message = "DeployModel Failed";
                    _instaRetrain.Success = false;
                    _instaRetrain.ErrorMessage = dataEngineering.Message;
                    return _instaRetrain;
                }
                else
                {
                    _instaRetrain.Status = dataEngineering.Status;
                    _instaRetrain.Message = dataEngineering.Message;
                    _instaRetrain.Success = true;
                    _instaRetrain.ErrorMessage = null;
                    return _instaRetrain;
                }

            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InstaAutoRetrainService), nameof(GetSPADeployPrediction), ex.Message + "- STACKTRACE - " + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaAutoRetrainService), nameof(GetSPADeployPrediction), "END", string.IsNullOrEmpty(Convert.ToString(result[CONSTANTS.CorrelationId])) ? default(Guid) : new Guid(Convert.ToString(result[CONSTANTS.CorrelationId])), string.Empty, string.Empty, string.Empty, string.Empty);
            return _instaRetrain;
        }
        private SPAPredictionDTO SPAGetPrediction(string correlationId, string userId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaAutoRetrainService), nameof(SPAGetPrediction), "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            SPAPredictionDTO sPAPredictionDTO = new SPAPredictionDTO();
            try
            {
                _deployModelViewModel = GetInstaDeployModel(correlationId);
                if (_deployModelViewModel != null && _deployModelViewModel.DeployModels.Count > 0)
                {
                    PredictionDTO predictionData = new PredictionDTO();
                    string frequency = _deployModelViewModel.DeployModels[0].Frequency;
                    PredictionDTO predictionDTO = new PredictionDTO
                    {
                        _id = Guid.NewGuid().ToString(),
                        UniqueId = Guid.NewGuid().ToString(),
                        CorrelationId = correlationId,
                        Frequency = frequency,
                        PredictedData = null,
                        Status = CONSTANTS.I,
                        ErrorMessage = null,
                        Progress = null,
                        CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                        ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                        CreatedByUser = userId,
                        ModifiedByUser = userId
                    };
                    bool DBEncryptionRequired = EncryptDB(_preProcessDTO.CorrelationId);
                    if (DBEncryptionRequired)
                        predictionDTO.ActualData = _IsAESKeyVault ? CryptographyUtility.Encrypt(CONSTANTS.Null) : AesProvider.Encrypt(CONSTANTS.Null, _aesKey, _aesVector);
                    else
                        predictionDTO.ActualData = CONSTANTS.Null;

                    if (_deployModelViewModel.DeployModels[0].ModelType == CONSTANTS.TimeSeries)
                    {
                        SavePrediction(predictionDTO);
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
                            UniId = predictionDTO.UniqueId,
                            Progress = null,
                            pageInfo = CONSTANTS.ForecastModel, // pageInfo 
                            ParamArgs = "{}",
                            Function = CONSTANTS.ForecastModel,
                            CreatedByUser = userId,
                            CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                            ModifiedByUser = userId,
                            ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                            LastProcessedOn = null
                        };
                        InsertRequests(ingrainRequest);
                    }
                    else
                    {
                        predictionDTO.ActualData = _deployModelViewModel.DeployModels[0].InputSample;
                        SavePrediction(predictionDTO);
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
                            UniId = predictionDTO.UniqueId,
                            Progress = null,
                            pageInfo = CONSTANTS.PublishModel, // pageInfo 
                            ParamArgs = "{}",
                            Function = CONSTANTS.PublishModel,
                            CreatedByUser = userId,
                            CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                            ModifiedByUser = userId,
                            ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                            LastProcessedOn = null
                        };
                        InsertRequests(ingrainRequest);
                    }
                    Thread.Sleep(2000);
                    bool isPrediction = true;
                    while (isPrediction)
                    {
                        predictionData = GetPrediction(predictionDTO);
                        if (predictionData.Status == CONSTANTS.C)
                        {
                            sPAPredictionDTO.Status = predictionData.Status;
                            sPAPredictionDTO.PredictedData = JObject.Parse(predictionData.PredictedData);
                            isPrediction = false;
                        }
                        else if (predictionData.Status == "E")
                        {
                            sPAPredictionDTO.Status = predictionData.Status;
                            isPrediction = false;
                        }
                        else
                        {
                            Thread.Sleep(1000);
                            isPrediction = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InstaAutoRetrainService), nameof(SPAGetPrediction), ex.Message + "- STACKTRACE - " + ex.StackTrace + "CORRELATIONID-" + (string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId)), ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }

            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaAutoRetrainService), nameof(SPAGetPrediction), "END", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            return sPAPredictionDTO;
        }

        private string CustomUrlToken(string ApplicationName)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaAutoRetrainService), nameof(CustomUrlToken), "CustomUrlToken for Application" + ApplicationName, string.Empty, string.Empty, string.Empty, string.Empty);
            var AppIntegCollection = _database.GetCollection<AppIntegration>(CONSTANTS.AppIntegration);
            var filterBuilder = Builders<AppIntegration>.Filter;
            var AppFilter = filterBuilder.Eq("ApplicationName", ApplicationName);

            var Projection = Builders<AppIntegration>.Projection.Include("BaseURL").Include("Authentication").Include("TokenGenerationURL").Include("Credentials").Exclude("_id");
            var AppData = AppIntegCollection.Find(AppFilter).Project<AppIntegration>(Projection).FirstOrDefault();

            dynamic token = string.Empty;
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaAutoRetrainService), nameof(CustomUrlToken), "CustomUrlToken for Application" + AppData.ApplicationName, AppData.ApplicationID, string.Empty, AppData.clientUId, AppData.deliveryConstructUID);
            if (AppData.Authentication == "AzureAD" || AppData.Authentication == "Azure")
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
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaAutoRetrainService), nameof(CustomUrlToken), "Application TOKEN PARAMS -- " + requestBuilder, AppData.ApplicationID, string.Empty, AppData.clientUId, AppData.deliveryConstructUID);
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
            }
            return token;
        }

        public string CallbackResponse(IngrainResponseData CallBackResponse, string ApplicationName, string baseAddress, string clientId, string DCId, string applicationId, string usecaseId, string requestId, string errorTrace, string userId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaAutoRetrainService), nameof(CallbackResponse), "START -CallbackResponse Initiated- Data-" + JsonConvert.SerializeObject(CallBackResponse), string.IsNullOrEmpty(CallBackResponse.CorrelationId) ? default(Guid) : new Guid(CallBackResponse.CorrelationId), applicationId, string.Empty, clientId, DCId);
            string token = CustomUrlToken(ApplicationName);

            string contentType = "application/json";
            var Request = JsonConvert.SerializeObject(CallBackResponse);
            if (_authProvider.ToUpper() == "FORM" || _authProvider.ToUpper() == "AZUREAD")
            {
                using (var Client = new HttpClient())
                {
                    Client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                    Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(contentType));
                    HttpContent httpContent = new StringContent(Request, Encoding.UTF8, "application/json");
                    HttpResponseMessage httpResponse = Client.PostAsync(baseAddress, httpContent).Result;
                    var statuscode = httpResponse.StatusCode;
                    CallBackErrorLog(CallBackResponse, ApplicationName, baseAddress, httpResponse, clientId, DCId, applicationId, usecaseId, requestId, errorTrace, userId);
                    if (httpResponse.IsSuccessStatusCode)
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaAutoRetrainService), nameof(CallbackResponse), "CALLBACKRESPONSE SUCCESS- TOKEN--" + token + "BASE_ADDERESS: " + baseAddress + "-HTTPCONTENT-" + httpResponse.Content.ReadAsStringAsync().Result, string.IsNullOrEmpty(CallBackResponse.CorrelationId) ? default(Guid) : new Guid(CallBackResponse.CorrelationId), applicationId, string.Empty, clientId, DCId);
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaAutoRetrainService), nameof(CallbackResponse), "END", string.IsNullOrEmpty(CallBackResponse.CorrelationId) ? default(Guid) : new Guid(CallBackResponse.CorrelationId), applicationId, string.Empty, clientId, DCId);
                        return "Success";
                    }
                    else
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaAutoRetrainService), nameof(CallbackResponse), "CALLBACKRESPONSE - CALLBACK API ERROR:- HTTP RESPONSE-" + httpResponse.StatusCode + "-HTTPCONTENT-" + httpResponse.Content.ReadAsStringAsync().Result, string.IsNullOrEmpty(CallBackResponse.CorrelationId) ? default(Guid) : new Guid(CallBackResponse.CorrelationId), applicationId, string.Empty, clientId, DCId);
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaAutoRetrainService), nameof(CallbackResponse), "END", string.IsNullOrEmpty(CallBackResponse.CorrelationId) ? default(Guid) : new Guid(CallBackResponse.CorrelationId), applicationId, string.Empty, clientId, DCId);
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

        public void CallBackErrorLog(IngrainResponseData CallBackResponse, string AppName, string baseAddress, HttpResponseMessage httpResponse, string clientId, string DCId, string applicationId, string usecaseId, string requestId, string errorTrace, string userId)
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
                    result = httpResponse.StatusCode + "-" + httpResponse.ReasonPhrase.ToString() + "- Content: " + httpResponse.Content.ReadAsStringAsync().Result;
                }
            }
            if (CallBackResponse.Status == CONSTANTS.ErrorMessage && AppName == CONSTANTS.SPAAPP)
            {
                CallBackResponse.ErrorMessage = CallBackResponse.ErrorMessage + " -" + "Record will be deleted from IngrainRequest for CorrelationId : " + CallBackResponse.CorrelationId;
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
            //if (baseAddress != null)
            //{
            //    UpdateIngrainRequest(requestId, CallBackResponse.Message + "" + result, isNoficationSent, CallBackResponse.Status);
            //}
        }


        public void UpdateReTrainingStatus(string Status, string Message, string RequestStatus, string requestId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaAutoRetrainService), nameof(UpdateReTrainingStatus), "UPDATETRAININGSTATUS - STARTED :", string.IsNullOrEmpty(requestId) ? default(Guid) : new Guid(requestId), string.Empty, string.Empty, string.Empty, string.Empty);
            var collection = _database.GetCollection<IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
            var filterBuilder = Builders<IngrainRequestQueue>.Filter;
            var filterCorrelation = filterBuilder.Eq("RequestId", requestId);
            var update = Builders<IngrainRequestQueue>.Update.Set("RequestStatus", RequestStatus).Set("Status", Status).Set("Message", Message);
            var isUpdated = collection.UpdateMany(filterCorrelation, update);
        }


        public void UpdateRetrainRequestStatus(string progressPercentage, string requestId)
        {
            var collection = _database.GetCollection<IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
            var filterBuilder = Builders<IngrainRequestQueue>.Filter;
            var filterCorrelation = filterBuilder.Eq("RequestId", requestId);
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
    }
}
