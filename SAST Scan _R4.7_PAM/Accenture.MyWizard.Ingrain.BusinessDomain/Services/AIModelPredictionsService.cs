using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
using Accenture.MyWizard.Ingrain.DataAccess;
using Accenture.MyWizard.Shared.Helpers;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using AIGENERICSERVICE = Accenture.MyWizard.Ingrain.DataModels.AICore.GenericService;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
using Accenture.MyWizard.Ingrain.DataModels.AICore;
using Newtonsoft.Json;
using MongoDB.Bson;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Http;
using System.IO;
using Accenture.MyWizard.Ingrain.DataModels.Models;
using Accenture.MyWizard.Ingrain.DataModels.AICore.GenericService;
using Accenture.MyWizard.Ingrain.DataModels.AICore.UseCase;
using Ninject.Syntax;
using System.Threading;
using Accenture.MyWizard.Ingrain.BusinessDomain.Common;

namespace Accenture.MyWizard.Ingrain.BusinessDomain.Services
{
    public class AIModelPredictionsService : IAIModelPredictionsService
    {

        #region members
        private readonly MongoClient _mongoClient;
        private readonly IMongoDatabase _database;
        private readonly WebHelper webHelper;
        private readonly DatabaseProvider databaseProvider;
        private readonly IngrainAppSettings appSettings;
        private readonly IEncryptionDecryption _encryptionDecryption;
        private readonly IAICoreService _aICoreService;
        private readonly IGenericSelfservice _genericSelfservice;


        #endregion



        #region constructor
        public AIModelPredictionsService(DatabaseProvider db, IOptions<IngrainAppSettings> settings, IServiceProvider serviceProvider)
        {
            databaseProvider = db;
            _mongoClient = databaseProvider.GetDatabaseConnection();
            var dataBaseName = MongoUrl.Create(settings.Value.connectionString).DatabaseName;
            _database = _mongoClient.GetDatabase(dataBaseName);
            appSettings = settings.Value;
            webHelper = new WebHelper();
            _encryptionDecryption = serviceProvider.GetService<IEncryptionDecryption>();
            _aICoreService = serviceProvider.GetService<IAICoreService>();
            _genericSelfservice = serviceProvider.GetService<IGenericSelfservice>();


        }

        #endregion



        #region methods
        public AIServiceRequestStatus GetIngestDataDetails(string correlationid)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIModelPredictionsService), nameof(GetIngestDataDetails), CONSTANTS.START, string.IsNullOrEmpty(correlationid) ? default(Guid) : new Guid(correlationid), string.Empty, string.Empty, string.Empty, string.Empty);
            AIServiceRequestStatus modelDetails = new AIServiceRequestStatus();
            var modelCollection = _database.GetCollection<BsonDocument>(CONSTANTS.AIServiceRequestStatus);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationid)
                         & (Builders<BsonDocument>.Filter.Eq("PageInfo", "TrainModel")
                             | Builders<BsonDocument>.Filter.Eq("PageInfo", "Ingest_Train")
                             | Builders<BsonDocument>.Filter.Eq("PageInfo", "Retrain"));
            var projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
            var result = modelCollection.Find(filter).Project<BsonDocument>(projection).Sort(Builders<BsonDocument>.Sort.Descending("CreatedOn")).ToList();
            if (result.Count > 0)
            {
                modelDetails = JsonConvert.DeserializeObject<AIServiceRequestStatus>(result[0].ToJson());
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIModelPredictionsService), nameof(GetIngestDataDetails), CONSTANTS.END, string.IsNullOrEmpty(correlationid) ? default(Guid) : new Guid(correlationid), string.Empty, string.Empty, string.Empty, string.Empty);
            return modelDetails;
        }

        public void InsertAIServicesPrediction(AIServicesPrediction aIServicesPrediction)
        {
            var predCollection = _database.GetCollection<AIServicesPrediction>(CONSTANTS.AIServicesPrediction);
            var predFilter = Builders<AIServicesPrediction>.Filter.Eq("UniId", aIServicesPrediction.UniId);
            var result = predCollection.Find(predFilter).ToList();
            if (result.Count > 0)
            {
                predCollection.DeleteMany(predFilter);
                predCollection.InsertOne(aIServicesPrediction);
            }
            else
            {
                predCollection.InsertOne(aIServicesPrediction);
            }
        }
        public AIServiceRequestStatus GetAIServiceRequestStatus(string correlationid, string uniqueId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIModelPredictionsService), nameof(GetAIServiceRequestStatus), CONSTANTS.START, string.IsNullOrEmpty(correlationid) ? default(Guid) : new Guid(correlationid), string.Empty, string.Empty, string.Empty, string.Empty);
            AIServiceRequestStatus modelDetails = new AIServiceRequestStatus();
            var modelCollection = _database.GetCollection<BsonDocument>(CONSTANTS.AIServiceRequestStatus);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationid)
                         & Builders<BsonDocument>.Filter.Eq("UniId", uniqueId);

            var projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
            var result = modelCollection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                modelDetails = JsonConvert.DeserializeObject<AIServiceRequestStatus>(result[0].ToJson());
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIModelPredictionsService), nameof(GetAIServiceRequestStatus), CONSTANTS.END, string.IsNullOrEmpty(correlationid) ? default(Guid) : new Guid(correlationid), modelDetails.ApplicationId, string.Empty, modelDetails.ClientId, modelDetails.DeliveryconstructId);
            return modelDetails;
        }
        private List<string> GetPredictionPages(string correlationId, string uniqueId)
        {
            List<string> availablePages = new List<string>();
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq(CONSTANTS.CorrelationId, correlationId) & builder.Eq(CONSTANTS.UniqueId, uniqueId);
            var projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
            var collection = _database.GetCollection<BsonDocument>("AIServicesPrediction");
            var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();

            if (result.Count > 0)
            {
                foreach (var req in result)
                {
                    if (!req["Chunk_number"].IsBsonNull)
                    {
                        availablePages.Add(req["Chunk_number"].AsString);
                    }

                }
            }

            return availablePages;
        }
        public AIGENERICSERVICE.AIModelPredictionResponse GetPredictionReponse(string correlationid, string uniqueId, string chunkNumber)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIModelPredictionsService), nameof(GetPredictionReponse), CONSTANTS.START, string.IsNullOrEmpty(correlationid) ? default(Guid) : new Guid(correlationid), string.Empty, string.Empty, string.Empty, string.Empty);
            AIGENERICSERVICE.AIModelPredictionResponse predictionResponse
                = new AIGENERICSERVICE.AIModelPredictionResponse(correlationid, uniqueId);
            var modelCollection = _database.GetCollection<BsonDocument>(CONSTANTS.AIServicesPrediction);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationid)
                         & Builders<BsonDocument>.Filter.Eq("UniId", uniqueId)
                         & Builders<BsonDocument>.Filter.Eq("Chunk_number", chunkNumber);

            var projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
            var result = modelCollection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                string status = result[0]["Status"].AsString;

                if (status == "C")
                {
                    if (true)
                    {
                        if (result[0]["PredictedData"].AsString != null)
                            result[0]["PredictedData"] = _encryptionDecryption.Decrypt(result[0]["PredictedData"].AsString);
                    }
                    predictionResponse.PredictedData = result[0]["PredictedData"].AsString;
                    predictionResponse.Progress = result[0]["Progress"].AsString;
                    predictionResponse.Status = result[0]["Status"].AsString;
                    predictionResponse.Message = result[0]["ErrorMessage"].AsString;
                }
                else if (status == "E")
                {
                    predictionResponse.Status = result[0]["Status"].AsString;
                    predictionResponse.Progress = result[0]["Progress"].AsString;
                    predictionResponse.Message = result[0]["ErrorMessage"].AsString;
                }
                else
                {
                    predictionResponse.Status = "I";
                    predictionResponse.Progress = result[0]["Progress"].AsString;
                    predictionResponse.Message = "Prediction is in Progress";
                }
                predictionResponse.AvailablePages = GetPredictionPages(correlationid, uniqueId);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIModelPredictionsService), nameof(GetPredictionReponse), CONSTANTS.END, string.IsNullOrEmpty(correlationid) ? default(Guid) : new Guid(correlationid), string.Empty, string.Empty, string.Empty, string.Empty);
            return predictionResponse;
        }

        public AIGENERICSERVICE.AIModelPredictionResponse InitiatePrediction(HttpContext httpContext)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIModelPredictionsService), nameof(InitiatePrediction), CONSTANTS.START, string.Empty, string.Empty, string.Empty, string.Empty);
            string uniqueId = Guid.NewGuid().ToString();
            ParentFile parentDetail = new ParentFile();
            Filepath _filepath = null;

            IFormCollection collection = httpContext.Request.Form;
            string correlationId = collection["CorrelationId"];
            string datasource = collection["DataSource"];
            dynamic datasourceInput = collection["DataSourceDetails"];
            string userId = collection["UserId"];
            string applicationId = collection["ApplicationId"];
            string Language = collection["Language"];

            if (!CommonUtility.IsValidGuid(Convert.ToString(collection["CorrelationId"])))
            {
                throw new Exception(string.Format(CONSTANTS.InValidGUID, "CorrelationId"));
            }
            if (!CommonUtility.IsValidGuid(Convert.ToString(collection["ApplicationId"])))
            {
                throw new Exception(string.Format(CONSTANTS.InValidGUID, "ApplicationId"));
            }

            if (!CommonUtility.IsDataValid(Convert.ToString(collection["DataSource"])))
            {
                throw new Exception(string.Format(CONSTANTS.InValidData, "DataSource"));
            }
            if (!CommonUtility.IsDataValid(Convert.ToString(collection["Language"])))
            {
                throw new Exception(string.Format(CONSTANTS.InValidData, "Language"));
            }
            CommonUtility.ValidateInputFormData(Convert.ToString(collection["DataSourceDetails"]), "DataSourceDetails", false);

            if (!CommonUtility.GetValidUser(Convert.ToString(collection["UserId"])))
            {
                throw new Exception("UserName/UserId is Invalid");
            }
            AIGENERICSERVICE.AIModelPredictionResponse aIModelPredictionResponse
                = new AIGENERICSERVICE.AIModelPredictionResponse(correlationId, uniqueId);
            try
            {
                string baseUrl = string.Empty;
                string apiPath = string.Empty;
                MethodReturn<object> serviceResponse = new MethodReturn<object>();

                AICoreModels modelDetails = _aICoreService.GetAICoreModelPath(correlationId);
                AIServiceRequestStatus sourceInfo = GetIngestDataDetails(correlationId);
                Service service = _aICoreService.GetAiCoreServiceDetails(sourceInfo.ServiceId);

                baseUrl = appSettings.AICorePythonURL;
                apiPath = service.ApiUrl;

                if (modelDetails.ModelStatus == "Completed")
                {
                    AIServiceRequestStatus aIServiceRequestStatus = new AIServiceRequestStatus();
                    aIServiceRequestStatus.CorrelationId = modelDetails.CorrelationId;
                    aIServiceRequestStatus.ServiceId = modelDetails.ServiceId;
                    aIServiceRequestStatus.UniId = uniqueId;
                    aIServiceRequestStatus.PageInfo = "PredictData";
                    aIServiceRequestStatus.ClientId = modelDetails.ClientId;
                    aIServiceRequestStatus.DeliveryconstructId = modelDetails.DeliveryConstructId;
                    aIServiceRequestStatus.ModelName = modelDetails.ModelName;
                    aIServiceRequestStatus.SelectedColumnNames = sourceInfo.SelectedColumnNames;
                    aIServiceRequestStatus.CreatedOn = DateTime.UtcNow.ToString();
                    aIServiceRequestStatus.CreatedByUser = userId;
                    aIServiceRequestStatus.ModifiedOn = DateTime.UtcNow.ToString();
                    aIServiceRequestStatus.ModifiedByUser = userId;
                    aIServiceRequestStatus.Language = Language;

                    if (datasource == "Phoenix")
                    {

                        var datasourceDetails = JObject.Parse(sourceInfo.SourceDetails.ToString());
                        if (datasourceDetails["metric"].ToString() != "null" && datasourceDetails["metric"].ToString() != "")
                        {
                            JObject metric = JObject.Parse(datasourceDetails["metric"].ToString());
                            metric["startDate"] = metric["endDate"];
                            metric["endDate"] = DateTime.Today.ToString("MM/dd/yyyy");

                            datasourceDetails["metric"] = JsonConvert.SerializeObject(metric, Formatting.None);
                        }
                        else if (datasourceDetails["pad"].ToString() != "null" && datasourceDetails["pad"].ToString() != "")
                        {
                            JObject pad = JObject.Parse(datasourceDetails["pad"].ToString());
                            pad["startDate"] = pad["endDate"];
                            pad["endDate"] = DateTime.Today.ToString("MM/dd/yyyy");

                            datasourceDetails["pad"] = JsonConvert.SerializeObject(pad, Formatting.None);
                        }

                        aIServiceRequestStatus.SourceDetails = JsonConvert.SerializeObject(datasourceDetails, Formatting.None);
                        aIServiceRequestStatus.DataSource = sourceInfo.DataSource;


                    }



                    if (datasource == "Custom")
                    {
                        CustomInputPayload appPayload = new CustomInputPayload();
                        CustomUpload customUpload = new CustomUpload
                        {
                            CorrelationId = correlationId,
                            ClientUID = modelDetails.ClientId,
                            DeliveryConstructUId = modelDetails.DeliveryConstructId,
                            Parent = parentDetail,
                            Flag = CONSTANTS.Null,
                            mapping = CONSTANTS.Null,
                            mapping_flag = CONSTANTS.False,//CONSTANTS.Null,
                            pad = CONSTANTS.Null,//string.Empty,
                            metric = CONSTANTS.Null,//string.Empty,
                            InstaMl = CONSTANTS.Null,// string.Empty,
                            fileupload = null,
                        };
                        var url = JsonConvert.DeserializeObject<JObject>(datasourceInput)["Url"];
                        var bodyParams = JsonConvert.DeserializeObject<JObject>(datasourceInput)["BodyParams"];
                        appPayload = new CustomInputPayload
                        {
                            AppId = applicationId,
                            HttpMethod = CONSTANTS.POST,
                            AppUrl = url.ToString(),
                            InputParameters = BsonDocument.Parse(bodyParams.ToString()) //BsonDocument.Parse(trainingRequest.DataSourceDetails.BodyParams.ToString())//JObject.Parse(trainingRequest.DataSourceDetails.BodyParams.ToString()) //trainingRequest.DataSourceDetails.BodyParams.ToObject<object>()//JsonConvert.DeserializeObject<JObject>(fileUpload.Customdetails.InputParameters.ToString())//trainingRequest.DataSourceDetails.BodyParams
                        };
                        parentDetail.Type = CONSTANTS.Null;
                        parentDetail.Name = CONSTANTS.Null;
                        customUpload.Parent = parentDetail;
                        _filepath = new Filepath();
                        _filepath.fileList = "null";
                        customUpload.fileupload = _filepath;
                        customUpload.Customdetails = appPayload;

                        aIServiceRequestStatus.SourceDetails = customUpload.ToJson();
                        aIServiceRequestStatus.DataSource = "Custom";


                    }


                    if (datasource == "File")
                    {
                        string ParentFileName = "undefined";

                        ParentFile parentFile = null;
                        FileUpload fileUpload = null;
                        int counter = 0;
                        string ParentFileNamePath = string.Empty, FilePath = string.Empty, postedFileName = string.Empty, DataSourceFilePath = string.Empty, FileName = string.Empty, SaveFileName = string.Empty;
                        string MappingColumns = string.Empty;
                        string filePath = string.Empty;
                        var fileCollection = httpContext.Request.Form.Files;
                        if (fileCollection.Count != 0)
                        {
                            if (CommonUtility.ValidateFileUploaded(fileCollection))
                            {
                                throw new FormatException(Resource.IngrainResx.InValidFileName);
                            }

                            for (int i = 0; i < fileCollection.Count; i++)
                            {
                                var folderPath = Guid.NewGuid().ToString();
                                var fileName = fileCollection[i].FileName;
                                filePath = appSettings.AICoreFilespath.ToString();

                                var filePath1 = filePath + correlationId + "/" + folderPath + "/" + fileName;
                                if (!Directory.Exists(filePath + correlationId + "/" + folderPath))
                                    Directory.CreateDirectory(filePath + correlationId + "/" + folderPath);
                                var postedFile = fileCollection[i];
                                if (postedFile.Length <= 0)
                                {
                                    throw new Exception(CONSTANTS.FileEmpty);
                                }
                                if (File.Exists(filePath1))
                                {
                                    counter++;
                                    FileName = postedFile.FileName;
                                    string[] strfileName = FileName.Split('.');
                                    FileName = strfileName[0] + "_" + counter;
                                    SaveFileName = FileName + "." + strfileName[1];
                                    _encryptionDecryption.EncryptFile(postedFile, filePath1);


                                }
                                else
                                {
                                    SaveFileName = postedFile.FileName;
                                    _encryptionDecryption.EncryptFile(postedFile, filePath1);
                                }
                                if (ParentFileName != CONSTANTS.undefined)
                                {
                                    if (appSettings.IsAESKeyVault && appSettings.Environment == CONSTANTS.PADEnvironment && appSettings.EncryptUploadedFiles)
                                    {
                                        FilePath = FilePath + "" + filePath1 + ".enc" + @"""" + @",""";
                                    }
                                    else
                                        FilePath = FilePath + "" + filePath1 + @"""" + @",""";
                                    if (postedFile.FileName == ParentFileName)
                                    {
                                        ParentFileNamePath = filePath1;

                                    }
                                }
                                else
                                {
                                    if (appSettings.IsAESKeyVault && appSettings.Environment == CONSTANTS.PADEnvironment && appSettings.EncryptUploadedFiles)
                                    {
                                        FilePath = FilePath + "" + filePath1 + ".enc" + @"""" + @",""";
                                    }
                                    else
                                        FilePath = FilePath + "" + filePath1 + @"""" + @",""";
                                    ParentFileNamePath = ParentFileName;
                                }
                                if (fileCollection.Count > 0)
                                {
                                    postedFileName = "[" + @"""" + FilePath.Remove(FilePath.Length - 2, 2) + "]";
                                }

                            }
                        }
                        _filepath = new Filepath();
                        if (postedFileName != "")
                            _filepath.fileList = postedFileName;
                        else
                            _filepath.fileList = "null";

                        parentFile = new ParentFile();
                        parentFile.Type = "null";
                        parentFile.Name = "null";


                        fileUpload = new FileUpload
                        {
                            CorrelationId = correlationId,
                            ClientUID = modelDetails.ClientId,
                            DeliveryConstructUId = modelDetails.DeliveryConstructId,
                            Parent = parentFile,
                            Flag = CONSTANTS.Null,
                            mapping = CONSTANTS.Null,
                            mapping_flag = CONSTANTS.False,
                            pad = CONSTANTS.Null,
                            metric = CONSTANTS.Null,
                            InstaMl = CONSTANTS.Null,
                            fileupload = _filepath,
                            Customdetails = CONSTANTS.Null

                        };
                        aIServiceRequestStatus.SourceDetails = fileUpload.ToJson();
                        aIServiceRequestStatus.DataSource = "File";

                    }
                    string encryptedUser = userId;
                    if (appSettings.isForAllData)
                    {
                        if (!string.IsNullOrEmpty(Convert.ToString(userId)))
                            encryptedUser = _encryptionDecryption.Encrypt(userId);
                    }
                    _aICoreService.InsertAIServiceRequest(aIServiceRequestStatus);
                    apiPath = apiPath + "?"
                              + "correlationId=" + aIServiceRequestStatus.CorrelationId
                              + "&userId=" + encryptedUser
                              + "&pageInfo=" + aIServiceRequestStatus.PageInfo
                              + "&UniqueId=" + aIServiceRequestStatus.UniId;

                    serviceResponse = _aICoreService.RouteGETRequest(string.Empty, new Uri(baseUrl), apiPath, false);


                    if (serviceResponse.IsSuccess)
                    {
                        aIModelPredictionResponse.Status = "I";
                        aIModelPredictionResponse.Message = "Prediction request initiated successfully";
                    }
                    else
                    {
                        aIModelPredictionResponse.Status = "E";
                        aIModelPredictionResponse.Message = serviceResponse.Message;
                    }

                }
                else
                {
                    aIModelPredictionResponse.Status = "E";
                    aIModelPredictionResponse.Message = "Model is not trained for this correlationId";
                }
            }
            catch (Exception ex)
            {
                aIModelPredictionResponse.Status = "E";
                aIModelPredictionResponse.Message = ex.Message;
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AIModelPredictionsService), nameof(InitiatePrediction), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }


            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIModelPredictionsService), nameof(InitiatePrediction), CONSTANTS.END, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            return aIModelPredictionResponse;

        }



        public AIGENERICSERVICE.AIModelPredictionResponse GetModelPredictionResults(AIGENERICSERVICE.AIModelPredictionRequest aIModelPredictionRequest)
        {
            AIGENERICSERVICE.AIModelPredictionResponse aIModelPredictionResponse
                                                    = new AIGENERICSERVICE.AIModelPredictionResponse(aIModelPredictionRequest.CorrelationId, aIModelPredictionRequest.UniqueId);
            try
            {

                AIServiceRequestStatus aIServiceRequestStatus = GetAIServiceRequestStatus(aIModelPredictionRequest.CorrelationId, aIModelPredictionRequest.UniqueId);

                if (aIServiceRequestStatus.Status == "C")
                {
                    return GetPredictionReponse(aIModelPredictionRequest.CorrelationId, aIModelPredictionRequest.UniqueId, aIModelPredictionRequest.PageNumber);
                }
                else if (aIServiceRequestStatus.Status == "E")
                {
                    aIModelPredictionResponse.Status = "E";
                    aIModelPredictionResponse.Message = aIServiceRequestStatus.Message;
                    return aIModelPredictionResponse;
                }
                else
                {
                    aIModelPredictionResponse.Status = aIServiceRequestStatus.Status;
                    aIModelPredictionResponse.Progress = aIServiceRequestStatus.Progress;
                    return aIModelPredictionResponse;

                }

            }
            catch (Exception ex)
            {
                aIModelPredictionResponse.Status = "E";
                aIModelPredictionResponse.Message = ex.Message;
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AIModelPredictionsService), nameof(GetPredictionReponse), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return aIModelPredictionResponse;
            }
        }

        public AIGENERICSERVICE.AIModelPredictionResponse InitiateTrainAndPrediction(HttpContext httpContext, string resourceId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIModelPredictionsService), nameof(InitiateTrainAndPrediction), CONSTANTS.START, string.Empty, string.Empty, string.Empty, string.Empty);
            string uniqueId = Guid.NewGuid().ToString();
            string correlationId = Guid.NewGuid().ToString();
            ParentFile parentDetail = new ParentFile();
            Filepath _filepath = null;
            bool encryptionFlag = true;

            IFormCollection collection = httpContext.Request.Form;
            string clientId = collection["ClientUId"];
            string deliveryConstructId = collection["DeliveryConstructUId"];
            string usecaseId = collection["UseCaseId"];
            string serviceId = collection["ServiceId"];
            string predInputData = collection["PredictionInputData"];
            string datasource = collection["DataSource"];

            dynamic datasourceInput = collection["DataSourceDetails"];
            string applicationId = collection["ApplicationId"];
            if (!CommonUtility.IsValidGuid(Convert.ToString(collection["ClientUId"])))
            {
                throw new Exception(string.Format(CONSTANTS.InValidGUID, "ClientUId"));
            }
            if (!CommonUtility.IsValidGuid(Convert.ToString(collection["DeliveryConstructUId"])))
            {
                throw new Exception(string.Format(CONSTANTS.InValidGUID, "DeliveryConstructUId"));
            }
            if (!CommonUtility.IsValidGuid(Convert.ToString(collection["UseCaseId"])))
            {
                throw new Exception(string.Format(CONSTANTS.InValidGUID, "UseCaseId"));
            }
            if (!CommonUtility.IsValidGuid(Convert.ToString(collection["ServiceId"])))
            {
                throw new Exception(string.Format(CONSTANTS.InValidGUID, "ServiceId"));
            }
            if (!CommonUtility.IsValidGuid(Convert.ToString(collection["ApplicationId"])))
            {
                throw new Exception(string.Format(CONSTANTS.InValidGUID, "ApplicationId"));
            }

            if (!CommonUtility.IsDataValid(Convert.ToString(collection["PredictionInputData"])))
            {
                throw new Exception(string.Format(CONSTANTS.InValidData, "PredictionInputData"));
            }
            if (!CommonUtility.IsDataValid(Convert.ToString(collection["DataSource"])))
            {
                throw new Exception(string.Format(CONSTANTS.InValidData, "DataSource"));
            }
            CommonUtility.ValidateInputFormData(Convert.ToString(collection["DataSourceDetails"]), "DataSourceDetails", false);
            AppIntegration app = _genericSelfservice.GetAppDetails(applicationId);



            AIGENERICSERVICE.AIModelPredictionResponse aIModelPredictionResponse
                = new AIGENERICSERVICE.AIModelPredictionResponse(correlationId, uniqueId);
            try
            {
                if (app == null)
                    throw new InvalidDataException("Application Id not found");
                else
                {
                    _aICoreService.UpdateTokenInAppIntegration(resourceId, applicationId);
                }

                string baseUrl = string.Empty;
                string apiPath = string.Empty;
                MethodReturn<object> serviceResponse = new MethodReturn<object>();

                UsecaseDetails sourceInfo = _aICoreService.GetUsecaseDetails(usecaseId);
                Service service = _aICoreService.GetAiCoreServiceDetails(sourceInfo.ServiceId);

                baseUrl = appSettings.AICorePythonURL;
                apiPath = service.ApiUrl;


                AIServiceRequestStatus aIServiceRequestStatus = new AIServiceRequestStatus();
                aIServiceRequestStatus.CorrelationId = correlationId;
                aIServiceRequestStatus.ServiceId = serviceId;
                aIServiceRequestStatus.UniId = uniqueId;
                aIServiceRequestStatus.PageInfo = "TrainAndPredict";
                aIServiceRequestStatus.ClientId = clientId;
                aIServiceRequestStatus.DeliveryconstructId = deliveryConstructId;
                aIServiceRequestStatus.ModelName = "";
                aIServiceRequestStatus.SelectedColumnNames = sourceInfo.InputColumns;
                aIServiceRequestStatus.CreatedOn = DateTime.UtcNow.ToString();
                aIServiceRequestStatus.CreatedByUser = app.ApplicationName;
                aIServiceRequestStatus.ModifiedOn = DateTime.UtcNow.ToString();
                aIServiceRequestStatus.ModifiedByUser = app.ApplicationName;

                if (datasource == "Phoenix")
                {

                    var datasourceDetails = JObject.Parse(sourceInfo.SourceDetails.ToString());
                    datasourceDetails["ClientUID"] = clientId;
                    datasourceDetails["DeliveryConstructUId"] = deliveryConstructId;

                    if (datasourceDetails["metric"].ToString() != "null" && datasourceDetails["metric"].ToString() != "")
                    {
                        JObject metric = JObject.Parse(datasourceDetails["metric"].ToString());
                        metric["startDate"] = metric["endDate"];
                        metric["endDate"] = DateTime.Today.ToString("MM/dd/yyyy");

                        datasourceDetails["metric"] = JsonConvert.SerializeObject(metric, Formatting.None);
                    }
                    else if (datasourceDetails["pad"].ToString() != "null" && datasourceDetails["pad"].ToString() != "")
                    {
                        JObject pad = JObject.Parse(datasourceDetails["pad"].ToString());
                        pad["startDate"] = DateTime.Today.AddYears(-2).ToString("MM/dd/yyyy");
                        pad["endDate"] = DateTime.Today.ToString("MM/dd/yyyy");

                        datasourceDetails["pad"] = JsonConvert.SerializeObject(pad, Formatting.None);
                    }

                    aIServiceRequestStatus.SourceDetails = JsonConvert.SerializeObject(datasourceDetails, Formatting.None);
                    aIServiceRequestStatus.DataSource = sourceInfo.SourceName;


                }



                if (datasource == "Custom")
                {
                    CustomInputPayload appPayload = new CustomInputPayload();
                    CustomUpload customUpload = new CustomUpload
                    {
                        CorrelationId = correlationId,
                        ClientUID = clientId,
                        DeliveryConstructUId = deliveryConstructId,
                        Parent = parentDetail,
                        Flag = CONSTANTS.Null,
                        mapping = CONSTANTS.Null,
                        mapping_flag = CONSTANTS.False,//CONSTANTS.Null,
                        pad = CONSTANTS.Null,//string.Empty,
                        metric = CONSTANTS.Null,//string.Empty,
                        InstaMl = CONSTANTS.Null,// string.Empty,
                        fileupload = null,
                    };
                    var url = JsonConvert.DeserializeObject<JObject>(datasourceInput)["Url"];
                    var bodyParams = JsonConvert.DeserializeObject<JObject>(datasourceInput)["BodyParams"];
                    appPayload = new CustomInputPayload
                    {
                        AppId = applicationId,
                        HttpMethod = CONSTANTS.POST,
                        AppUrl = url.ToString(),
                        InputParameters = BsonDocument.Parse(bodyParams.ToString()) //BsonDocument.Parse(trainingRequest.DataSourceDetails.BodyParams.ToString())//JObject.Parse(trainingRequest.DataSourceDetails.BodyParams.ToString()) //trainingRequest.DataSourceDetails.BodyParams.ToObject<object>()//JsonConvert.DeserializeObject<JObject>(fileUpload.Customdetails.InputParameters.ToString())//trainingRequest.DataSourceDetails.BodyParams
                    };
                    parentDetail.Type = CONSTANTS.Null;
                    parentDetail.Name = CONSTANTS.Null;
                    customUpload.Parent = parentDetail;
                    _filepath = new Filepath();
                    _filepath.fileList = "null";
                    customUpload.fileupload = _filepath;
                    customUpload.Customdetails = appPayload;

                    aIServiceRequestStatus.SourceDetails = customUpload.ToJson();
                    aIServiceRequestStatus.DataSource = "Custom";


                }


                if (datasource == "File")
                {
                    string ParentFileName = "undefined";

                    ParentFile parentFile = null;
                    FileUpload fileUpload = null;
                    int counter = 0;
                    string ParentFileNamePath = string.Empty, FilePath = string.Empty, postedFileName = string.Empty, DataSourceFilePath = string.Empty, FileName = string.Empty, SaveFileName = string.Empty;
                    string MappingColumns = string.Empty;
                    string filePath = string.Empty;
                    var fileCollection = httpContext.Request.Form.Files;
                    if (fileCollection.Count != 0)
                    {
                        if (CommonUtility.ValidateFileUploaded(fileCollection))
                        {
                            throw new FormatException(Resource.IngrainResx.InValidFileName);
                        }

                        for (int i = 0; i < fileCollection.Count; i++)
                        {
                            var folderPath = Guid.NewGuid().ToString();
                            var fileName = fileCollection[i].FileName;
                            filePath = appSettings.AICoreFilespath.ToString();

                            var filePath1 = filePath + correlationId + "/" + folderPath + "/" + fileName;
                            if (!Directory.Exists(filePath + correlationId + "/" + folderPath))
                                Directory.CreateDirectory(filePath + correlationId + "/" + folderPath);
                            var postedFile = fileCollection[i];
                            if (postedFile.Length <= 0)
                            {
                                throw new Exception(CONSTANTS.FileEmpty);
                            }
                            if (File.Exists(filePath1))
                            {
                                counter++;
                                FileName = postedFile.FileName;
                                string[] strfileName = FileName.Split('.');
                                FileName = strfileName[0] + "_" + counter;
                                SaveFileName = FileName + "." + strfileName[1];
                                _encryptionDecryption.EncryptFile(postedFile, filePath1);


                            }
                            else
                            {
                                SaveFileName = postedFile.FileName;
                                _encryptionDecryption.EncryptFile(postedFile, filePath1);
                            }
                            if (ParentFileName != CONSTANTS.undefined)
                            {
                                if (appSettings.IsAESKeyVault && appSettings.Environment == CONSTANTS.PADEnvironment && appSettings.EncryptUploadedFiles)
                                {
                                    FilePath = FilePath + "" + filePath1 + ".enc" + @"""" + @",""";
                                }
                                else
                                    FilePath = FilePath + "" + filePath1 + @"""" + @",""";
                                if (postedFile.FileName == ParentFileName)
                                {
                                    ParentFileNamePath = filePath1;

                                }
                            }
                            else
                            {
                                if (appSettings.IsAESKeyVault && appSettings.Environment == CONSTANTS.PADEnvironment && appSettings.EncryptUploadedFiles)
                                {
                                    FilePath = FilePath + "" + filePath1 + ".enc" + @"""" + @",""";
                                }
                                else
                                    FilePath = FilePath + "" + filePath1 + @"""" + @",""";
                                ParentFileNamePath = ParentFileName;
                            }
                            if (fileCollection.Count > 0)
                            {
                                postedFileName = "[" + @"""" + FilePath.Remove(FilePath.Length - 2, 2) + "]";
                            }

                        }
                    }
                    _filepath = new Filepath();
                    if (postedFileName != "")
                        _filepath.fileList = postedFileName;
                    else
                        _filepath.fileList = "null";

                    parentFile = new ParentFile();
                    parentFile.Type = "null";
                    parentFile.Name = "null";


                    fileUpload = new FileUpload
                    {
                        CorrelationId = correlationId,
                        ClientUID = clientId,
                        DeliveryConstructUId = deliveryConstructId,
                        Parent = parentFile,
                        Flag = CONSTANTS.Null,
                        mapping = CONSTANTS.Null,
                        mapping_flag = CONSTANTS.False,
                        pad = CONSTANTS.Null,
                        metric = CONSTANTS.Null,
                        InstaMl = CONSTANTS.Null,
                        fileupload = _filepath,
                        Customdetails = CONSTANTS.Null

                    };
                    aIServiceRequestStatus.SourceDetails = fileUpload.ToJson();
                    aIServiceRequestStatus.DataSource = "File";

                }

                _aICoreService.InsertAIServiceRequest(aIServiceRequestStatus);


                predInputData = encryptionFlag ? _encryptionDecryption.Encrypt(predInputData) : predInputData;
                string UserId = app.ApplicationName;
                if (!string.IsNullOrEmpty(Convert.ToString(UserId)))
                {
                    UserId = _encryptionDecryption.Encrypt(Convert.ToString(UserId));
                }

                AIServicesPrediction aIServicesPrediction = new AIServicesPrediction();
                aIServicesPrediction.CorrelationId = correlationId;
                aIServicesPrediction.UniId = uniqueId;
                aIServicesPrediction.ActualData = predInputData;
                aIServicesPrediction.PageInfo = "TrainAndPredict";

                aIServicesPrediction.Status = "N";
                aIServicesPrediction.Progress = "0";
                aIServicesPrediction.Chunk_number = "0";
                aIServicesPrediction.CreatedBy = UserId;
                aIServicesPrediction.CreatedOn = DateTime.UtcNow.ToString();
                aIServicesPrediction.ModifiedBy = UserId;
                aIServicesPrediction.ModifiedOn = DateTime.UtcNow.ToString();

                InsertAIServicesPrediction(aIServicesPrediction);
                apiPath = apiPath + "?"
                          + "correlationId=" + aIServiceRequestStatus.CorrelationId
                          + "&userId=" + UserId
                          + "&pageInfo=" + aIServiceRequestStatus.PageInfo
                          + "&UniqueId=" + aIServiceRequestStatus.UniId;

                serviceResponse = _aICoreService.RouteGETRequest(string.Empty, new Uri(baseUrl), apiPath, false);



                if (serviceResponse.IsSuccess)
                {
                    bool flag = true;
                    AIServiceRequestStatus reqStatus = null;
                    while (flag)
                    {
                        reqStatus = GetAIServiceRequestStatus(correlationId, uniqueId);
                        if (reqStatus.Status == "C")
                        {
                            AIGENERICSERVICE.AIModelPredictionResponse pred = GetPredictionReponse(correlationId, uniqueId, "0");
                            if (pred.Status == "C" || pred.Status == "E")
                            {
                                return pred;
                            }
                            else
                            {
                                flag = true;
                            }
                        }
                        else if (reqStatus.Status == "E")
                        {

                            AIGENERICSERVICE.AIModelPredictionResponse pred
                                = new AIGENERICSERVICE.AIModelPredictionResponse(correlationId, uniqueId);
                            pred.Status = "E";
                            pred.Message = reqStatus.Message;
                            return pred;
                        }
                        else
                        {
                            flag = true;
                        }

                        Thread.Sleep(1000);

                    }

                }
                else
                {
                    aIModelPredictionResponse.Status = "E";
                    aIModelPredictionResponse.Message = "Python api Error-" + serviceResponse.Message;
                }


            }
            catch (Exception ex)
            {
                aIModelPredictionResponse.Status = "E";
                aIModelPredictionResponse.Message = ex.Message;
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AIModelPredictionsService), nameof(InitiateTrainAndPrediction), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }


            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AIModelPredictionsService), nameof(InitiateTrainAndPrediction), CONSTANTS.END, string.Empty, string.Empty, string.Empty, string.Empty);
            return aIModelPredictionResponse;

        }



        #endregion



    }
}
