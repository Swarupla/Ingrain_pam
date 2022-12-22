using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
using Accenture.MyWizard.Ingrain.DataAccess;
using Accenture.MyWizard.Ingrain.DataModels.AICore;
using Accenture.MyWizard.Ingrain.DataModels.Models;
using Accenture.MyWizard.Shared.Helpers;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using JsonConvert = Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using System.IO;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
using System.Linq;
using RestSharp;
using System.Threading;
using MongoDB.Bson.IO;
using System.Globalization;
using Accenture.MyWizard.Cryptography.EncryptionProviders;
using Accenture.MyWizard.Ingrain.BusinessDomain.Common;

namespace Accenture.MyWizard.Ingrain.BusinessDomain.Services
{
    /// <summary>
    /// ClusteringAPIService
    /// </summary>
    public class ClusteringAPIService : IClusteringAPIService
    {
        private readonly MongoClient _mongoClient;
        private ClusteringAPIModel ingrainRequest;
        private readonly IMongoDatabase _database;
        private readonly WebHelper webHelper;
        private readonly DatabaseProvider databaseProvider;
        private readonly IngrainAppSettings appSettings;
        //private static IAICoreService _aICoreService { get; set; }
        private static IIngestedData _ingestedDataService { set; get; }
        Filepath _filepath = null;
        ParentFile parentFile = null;
        FileUpload fileUpload = null;
        private IEncryptionDecryption _encryptionDecryption;
        private static ICustomDataService _customDataService { set; get; }

        public static IPhoenixTokenService _iPhoenixTokenService { get; set; }
        /// <summary>
        /// ClusteringAPIService Constructor
        /// </summary>
        /// <param name="db">DatabaseProvider</param>
        /// <param name="settings">IngrainAppSettings</param>
        /// <param name="serviceProvider">serviceProvider</param>
        public ClusteringAPIService(DatabaseProvider db, IOptions<IngrainAppSettings> settings, IServiceProvider serviceProvider)
        {
            databaseProvider = db;
            _mongoClient = databaseProvider.GetDatabaseConnection();
            var dataBaseName = MongoUrl.Create(settings.Value.connectionString).DatabaseName;
            _database = _mongoClient.GetDatabase(dataBaseName);
            appSettings = settings.Value;
            //_aICoreService = serviceProvider.GetService<IAICoreService>();
            webHelper = new WebHelper();
            _ingestedDataService = serviceProvider.GetService<IIngestedData>();
            _encryptionDecryption = serviceProvider.GetService<IEncryptionDecryption>();
            _customDataService = serviceProvider.GetService<ICustomDataService>();
            _iPhoenixTokenService = serviceProvider.GetService<IPhoenixTokenService>();
        }

        /// <summary>
        /// Get All Custering Models
        /// </summary>
        /// <param name="clientid"></param>
        /// <param name="dcid"></param>
        /// <param name="serviceid"></param>
        /// <param name="userid"></param>
        /// <returns></returns>
        public ClusteringModel GetAllCusteringModels(string clientid, string dcid, string serviceid, string userid)
        {
            string encryptedUser = userid;
            if (!string.IsNullOrEmpty(Convert.ToString(userid)))
                encryptedUser = _encryptionDecryption.Encrypt(Convert.ToString(userid));
            var pythonFilePath = appSettings.pyLogsPath;
            if (pythonFilePath != null)
            {
                if (!Directory.Exists(pythonFilePath))
                {
                    Directory.CreateDirectory(pythonFilePath);
                }
            }
            ClusteringModel data = new ClusteringModel();
            List<BsonDocument> ingestData = new List<BsonDocument>();
            List<JObject> trainedModelList = new List<JObject>();
            List<JObject> clusterColmuns = new List<JObject>();
            List<ClusteringStatus> serviceList = new List<ClusteringStatus>();
            List<ClusteringStatus> StatusList = new List<ClusteringStatus>();
            List<ClusteringStatus> ModelTrainingList = new List<ClusteringStatus>();
            List<ClusteringStatus> NewStatusList = new List<ClusteringStatus>();
            List<string> CorrelationIdList = new List<string>();
            List<string> VisulaCorrelationIdList = new List<string>();

            var collection2 = _database.GetCollection<BsonDocument>("Clustering_IngestData");
            var filter2 = Builders<BsonDocument>.Filter.Eq(CONSTANTS.ServiceID, serviceid) & (Builders<BsonDocument>.Filter.Eq("CreatedBy", userid) | Builders<BsonDocument>.Filter.Eq("CreatedBy", encryptedUser));
            var projection2 = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id).Include("CorrelationId").Include("ModifiedOn").Include("SelectedModels").Include("ModelName").Include("ProblemType").Include("Columnsselectedbyuser").Include("DataSource").Include("StopWords").Include("Ngram").Include("ValidColumnsSelected").Include(CONSTANTS.DataSetUId).Include(CONSTANTS.MaxDataPull).Include("mapping");
            var result2 = collection2.Find(filter2).SortByDescending(item => item["ModifiedOn"]).Project<BsonDocument>(projection2).ToList();
            if (result2.Count > 0)
            {
                for (int i = 0; i < result2.Count; i++)
                {
                    result2[i]["ModifiedOn"] = DateTime.Parse(result2[i]["ModifiedOn"].ToString()).ToString("yyyy-MM-dd HH:mm:ss");
                }
                ingestData = result2.ToList();
                ingestData = ingestData.OrderByDescending(item => item["ModifiedOn"]).ToList();

            }
            var serviceCollection = _database.GetCollection<BsonDocument>(CONSTANTS.Clustering_StatusTable);
            Thread.Sleep(2000);
            List<string> CorrelationIdList2 = new List<string>();
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.ServiceID, serviceid) & (Builders<BsonDocument>.Filter.Eq(CONSTANTS.CreatedByUser, userid) | Builders<BsonDocument>.Filter.Eq(CONSTANTS.CreatedByUser, encryptedUser)) & Builders<BsonDocument>.Filter.Eq("ClientID", clientid) & Builders<BsonDocument>.Filter.Eq("DCUID", dcid);
            var projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
            var result = serviceCollection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                for (var i = 0; i < result.Count; i++)
                {
                    bool DBEncryptionRequired = DBEncrypt_Clustering(result[i][CONSTANTS.CorrelationId].ToString());
                    if (DBEncryptionRequired)
                    {
                        try
                        {
                            if (result[i].Contains(CONSTANTS.CreatedByUser) && !string.IsNullOrEmpty(Convert.ToString(result[i][CONSTANTS.CreatedByUser])))
                                result[i][CONSTANTS.CreatedByUser] = _encryptionDecryption.Decrypt(Convert.ToString(result[i][CONSTANTS.CreatedByUser]));
                        }
                        catch (Exception ex)
                        {
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ClusteringAPIService), nameof(GetAllCusteringModels), ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty);
                        }
                        try { 
                            if (result[i].Contains(CONSTANTS.ModifiedByUser) && !string.IsNullOrEmpty(Convert.ToString(result[i][CONSTANTS.ModifiedByUser])))
                                result[i][CONSTANTS.ModifiedByUser] = _encryptionDecryption.Decrypt(Convert.ToString(result[i][CONSTANTS.ModifiedByUser]));
                        }
                        catch (Exception ex)
                        {
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ClusteringAPIService), nameof(GetAllCusteringModels), ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty);
                        }
                    }
                }
                StatusList = JsonConvert.JsonConvert.DeserializeObject<List<ClusteringStatus>>(result.ToJson());
                for (var i = 0; i < StatusList.Count; i++)
                {
                    CorrelationIdList2.Add(StatusList[i].CorrelationId);
                    var correlationId = CONSTANTS.InvertedComma;
                    if (StatusList[i].pageInfo == CONSTANTS.ModelTraining)
                    {
                        VisulaCorrelationIdList.Add(StatusList[i].CorrelationId);
                        correlationId = StatusList[i].CorrelationId;
                        CorrelationIdList.Add(correlationId);
                        StatusList[i].PredictionURL = appSettings.AICorePredictionURL + "?=" + StatusList[i].CorrelationId;
                        if (StatusList[i].Progress == "100")
                        {
                            var collection1 = _database.GetCollection<BsonDocument>("Clustering_TrainedModels");
                            var filter1 = Builders<BsonDocument>.Filter.Eq(CONSTANTS.ServiceID, serviceid) & (Builders<BsonDocument>.Filter.Eq("CreatedBy", userid) | Builders<BsonDocument>.Filter.Eq("CreatedBy", encryptedUser));
                            var projection1 = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id).Include("CorrelationId").Include("Model Metrics").Include("ModelName").Include("Topic_dictionary");
                            var result1 = collection1.Find(filter1).Project<BsonDocument>(projection1).ToList();
                            if (result1.Count > 0)
                            {
                                //for (var k = 0; k < result1.Count; k++)
                                //{
                                //    bool DBEncryptionRequired = DBEncrypt_Clustering(result1[k][CONSTANTS.CorrelationId].ToString());
                                //    if (DBEncryptionRequired)
                                //    {
                                //        if (result1[k].Contains("CreatedBy") && !string.IsNullOrEmpty(Convert.ToString(result1[k]["CreatedBy"])))
                                //            result1[k]["CreatedBy"] = _encryptionDecryption.Decrypt(Convert.ToString(result1[k]["CreatedBy"]));
                                //        if (result1[k].Contains("") && !string.IsNullOrEmpty(Convert.ToString(result1[k][""])))
                                //            result1[k][""] = _encryptionDecryption.Decrypt(Convert.ToString(result1[k][""]));
                                //    }
                                //}
                                trainedModelList = JsonConvert.JsonConvert.DeserializeObject<List<JObject>>(result1.ToJson());
                            }
                            for (var j = 0; j < trainedModelList.Count; j++)
                            {
                                if (correlationId == trainedModelList[j]["CorrelationId"].ToString() && StatusList[i].ModelType == trainedModelList[j]["ModelName"].ToString())
                                {
                                    StatusList[i].Silhouette_Coefficient = trainedModelList[j]["Model Metrics"]["Silhouette Coefficient"].ToObject<dynamic>();
                                    StatusList[i].Clusters = trainedModelList[j]["Model Metrics"]["Clusters"].ToObject<dynamic>();
                                    if (trainedModelList[j].ContainsKey("Topic_dictionary"))
                                    {
                                        bool DBEncryptionRequired = DBEncrypt_Clustering(trainedModelList[j][CONSTANTS.CorrelationId].ToString());
                                        if (DBEncryptionRequired)
                                        {
                                            if (trainedModelList[j].ContainsKey("Topic_dictionary"))
                                            {
                                                if (trainedModelList[j]["Topic_dictionary"].ToString() != "null")
                                                {
                                                    trainedModelList[j]["Topic_dictionary"] = JObject.Parse(_encryptionDecryption.Decrypt(trainedModelList[j]["Topic_dictionary"].ToString()));
                                                }
                                            }
                                        }
                                        StatusList[i].Suggestion = JsonConvert.JsonConvert.DeserializeObject<JObject>(trainedModelList[j]["Topic_dictionary"].ToString().Replace("Cluster ", ""));
                                    }
                                    //StatusList[i].Columnsselectedbyuser = 
                                }
                            }
                        }
                        serviceList.Add(StatusList[i]);
                    }
                }
                StatusList.RemoveAll(item => CorrelationIdList.Contains(item.CorrelationId));
                if (StatusList.Count > 0)
                {
                    for (var i = 0; i < StatusList.Count; i++)
                    {
                        var correlationId = CONSTANTS.InvertedComma;
                        if (StatusList[i].pageInfo == CONSTANTS.DataTransformation && (!CorrelationIdList.Contains(StatusList[i].CorrelationId)))
                        {
                            correlationId = StatusList[i].CorrelationId;
                            CorrelationIdList.Add(correlationId);
                            StatusList[i].PredictionURL = appSettings.AICorePredictionURL + "?=" + StatusList[i].CorrelationId;
                            serviceList.Add(StatusList[i]);
                        }
                    }
                }
                StatusList.RemoveAll(item => CorrelationIdList.Contains(item.CorrelationId));
                if (StatusList.Count > 0)
                {
                    for (var i = 0; i < StatusList.Count; i++)
                    {
                        var correlationId = "";
                        if (StatusList[i].pageInfo == CONSTANTS.DataCuration && (!CorrelationIdList.Contains(StatusList[i].CorrelationId)))
                        {
                            correlationId = StatusList[i].CorrelationId;
                            CorrelationIdList.Add(correlationId);
                            StatusList[i].PredictionURL = appSettings.AICorePredictionURL + "?=" + StatusList[i].CorrelationId;
                            serviceList.Add(StatusList[i]);
                        }
                    }
                }
                StatusList.RemoveAll(item => CorrelationIdList.Contains(item.CorrelationId));
                if (StatusList.Count > 0)
                {
                    for (var i = 0; i < StatusList.Count; i++)
                    {
                        var correlationId = "";
                        if (StatusList[i].pageInfo == "InvokeIngestData" && (!CorrelationIdList.Contains(StatusList[i].CorrelationId)))
                        {
                            correlationId = StatusList[i].CorrelationId;
                            CorrelationIdList.Add(correlationId);
                            StatusList[i].PredictionURL = appSettings.AICorePredictionURL + "?=" + StatusList[i].CorrelationId;
                            serviceList.Add(StatusList[i]);
                        }
                    }
                }
                if (StatusList.Count > 0)
                {
                    for (var i = 0; i < StatusList.Count; i++)
                    {
                        var correlationId = "";
                        if (StatusList[i].pageInfo == "wordcloud" && (!CorrelationIdList.Contains(StatusList[i].CorrelationId)))
                        {
                            correlationId = StatusList[i].CorrelationId;
                            CorrelationIdList.Add(correlationId);
                            StatusList[i].PredictionURL = appSettings.AICorePredictionURL + "?=" + StatusList[i].CorrelationId;
                            serviceList.Add(StatusList[i]);
                        }
                    }
                }
            }
            data.clusteringStatus = serviceList.OrderBy(item => item.ModifiedOn).ToList();
            for (var i = 0; i < data.clusteringStatus.Count; i++)
            {
                //ingestData.FirstOrDefault(correlationId => correlationId["CorelationId"]);
                //ingestData = ingestData.SetSortOrder(SortBy.Ascending("ModifiedOn")).ToList();
                var newData = ingestData.FirstOrDefault(corr => corr["CorrelationId"] == data.clusteringStatus[i].CorrelationId);
                if (newData != null)
                {
                    //data.clusteringStatus[i].ingestData = newData;
                    var jsonWriterSettings = new JsonWriterSettings { OutputMode = JsonOutputMode.Strict };
                    JObject json = JObject.Parse(newData.ToJson<MongoDB.Bson.BsonDocument>(jsonWriterSettings));

                    data.clusteringStatus[i].ingestData = json;// JsonConvert.DeserializeObject<dynamic>(newData.ToJson());
                    var collection1 = _database.GetCollection<BsonDocument>("Clustering_BusinessProblem");
                    var filter1 = Builders<BsonDocument>.Filter.Eq("CorrelationId", data.clusteringStatus[i].CorrelationId);
                    var projection1 = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id).Include("ColumnsList");
                    var result1 = collection1.Find(filter1).Project<BsonDocument>(projection1).ToList();
                    if (result1.Count > 0)
                    {
                        var jsonRes = result1[0]["ColumnsList"].ToJson();
                        data.clusteringStatus[i].ColumnNames = (JsonConvert.JsonConvert.DeserializeObject<List<object>>(jsonRes));
                    }


                }
            }
            // return serviceList;
            var dataProcessingCollection = _database.GetCollection<BsonDocument>(CONSTANTS.Clustering_DataPreprocessing);
            var dtaProcessingFilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.ServiceID, serviceid) & (Builders<BsonDocument>.Filter.Eq("CreatedBy", userid) | Builders<BsonDocument>.Filter.Eq("CreatedBy", encryptedUser));
            var dtaProcessingProjection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id).Include("CorrelationId").Include("Category_columns").Include("Clustering_type")
                .Include("CreatedBy").Include("Numerical_columns").Include("Non_Text_columns").Include("Text_columns").Include("pageInfo");
            var dtaProcessingResult = dataProcessingCollection.Find(dtaProcessingFilter).Project<BsonDocument>(dtaProcessingProjection).ToList();
            if (dtaProcessingResult.Count > 0)
            {
                for (var i = 0; i < dtaProcessingResult.Count; i++)
                {
                    bool DBEncryptionRequired = DBEncrypt_Clustering(dtaProcessingResult[i][CONSTANTS.CorrelationId].ToString());
                    if (DBEncryptionRequired)
                    {
                        try
                        {
                            if (dtaProcessingResult[i].Contains("CreatedBy") && !string.IsNullOrEmpty(Convert.ToString(dtaProcessingResult[i]["CreatedBy"])))
                                dtaProcessingResult[i]["CreatedBy"] = _encryptionDecryption.Decrypt(Convert.ToString(dtaProcessingResult[i]["CreatedBy"]));
                        }
                        catch (Exception ex)
                        {
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ClusteringAPIService), nameof(GetAllCusteringModels), ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty);
                        }
                    }
                }
                clusterColmuns = JsonConvert.JsonConvert.DeserializeObject<List<JObject>>(dtaProcessingResult.ToJson());
            }
            data.ClusteredColumns = clusterColmuns;

            var AIRequestcollection = _database.GetCollection<BsonDocument>(CONSTANTS.AIServiceRequestStatus);
            var AIRequestfilter = Builders<BsonDocument>.Filter.Eq("ServiceId", serviceid) & (Builders<BsonDocument>.Filter.Eq(CONSTANTS.CreatedByUser, userid) | Builders<BsonDocument>.Filter.Eq(CONSTANTS.CreatedByUser, encryptedUser)) & Builders<BsonDocument>.Filter.Eq("ClientId", clientid) & Builders<BsonDocument>.Filter.Eq("DeliveryconstructId", dcid);
            var AIRequestprojection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
            var AIRequestresult = AIRequestcollection.Find(AIRequestfilter).Project<BsonDocument>(AIRequestprojection).ToList();
            List<string> AIRequestCorrelationIdList = new List<string>();
            if (AIRequestresult.Count > 0)
            {
                for (var i = 0; i < AIRequestresult.Count; i++)
                {
                    AIRequestCorrelationIdList.Add(AIRequestresult[i][CONSTANTS.CorrelationId].ToString());
                }
                var firstNotSecond = AIRequestCorrelationIdList.Except(CorrelationIdList2).ToList();

                foreach (string corrid in firstNotSecond)
                {
                    AIRequestfilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, corrid);
                    AIRequestresult = AIRequestcollection.Find(AIRequestfilter).Project<BsonDocument>(AIRequestprojection).ToList();
                    if (AIRequestresult.Count > 0)
                    {
                        ClusteringStatus ingrainRequest = new ClusteringStatus();
                        ingrainRequest.CorrelationId = AIRequestresult[0][CONSTANTS.CorrelationId].ToString();
                        if (AIRequestresult[0][CONSTANTS.Status].ToString() != "C" || AIRequestresult[0][CONSTANTS.Status].ToString() != "E")
                            ingrainRequest.Status = CONSTANTS.P;
                        else
                            ingrainRequest.Status = AIRequestresult[0][CONSTANTS.Status].ToString();
                        if (AIRequestresult[0][CONSTANTS.Progress].ToString() == "BsonNull")
                            ingrainRequest.Progress = "0";
                        else
                            ingrainRequest.Progress = AIRequestresult[0][CONSTANTS.Progress].ToString();
                        if (AIRequestresult[0][CONSTANTS.RequestStatus].ToString() == "BsonNull")
                            if (AIRequestresult[0][CONSTANTS.Status].ToString() == "E")
                                ingrainRequest.RequestStatus = "Error";
                            else
                                ingrainRequest.RequestStatus = "In Progress";
                        else
                            ingrainRequest.RequestStatus = AIRequestresult[0][CONSTANTS.RequestStatus].ToString();
                        if (AIRequestresult[0][CONSTANTS.Message].ToString() == "BsonNull")
                            if (AIRequestresult[0][CONSTANTS.Status].ToString() == "E")
                                ingrainRequest.Message = "Error";
                            else
                                ingrainRequest.Message = "In Progress";
                        else
                            ingrainRequest.Message = AIRequestresult[0][CONSTANTS.Message].ToString();
                        bool DBEncryptionRequired = DBEncrypt_Clustering(corrid);
                        if (DBEncryptionRequired)
                        {
                            try
                            {
                                if (AIRequestresult[0].Contains(CONSTANTS.CreatedByUser) && !string.IsNullOrEmpty(Convert.ToString(AIRequestresult[0][CONSTANTS.CreatedByUser])))
                                    AIRequestresult[0][CONSTANTS.CreatedByUser] = _encryptionDecryption.Decrypt(Convert.ToString(AIRequestresult[0][CONSTANTS.CreatedByUser]));
                            }
                            catch (Exception ex)
                            {
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(ClusteringAPIService), nameof(GetAllCusteringModels), ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty);
                            }
                            try
                            {
                                if (AIRequestresult[0].Contains(CONSTANTS.ModifiedByUser) && !string.IsNullOrEmpty(Convert.ToString(AIRequestresult[0][CONSTANTS.ModifiedByUser])))
                                    AIRequestresult[0][CONSTANTS.ModifiedByUser] = _encryptionDecryption.Decrypt(Convert.ToString(AIRequestresult[0][CONSTANTS.ModifiedByUser]));
                            }
                            catch (Exception ex)
                            {
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(ClusteringAPIService), nameof(GetAllCusteringModels), ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty);
                            }
                        }
                        ingrainRequest.UniId = AIRequestresult[0][CONSTANTS.UniId].ToString();
                        ingrainRequest.pageInfo = AIRequestresult[0][CONSTANTS.PageInfo].ToString();
                        ingrainRequest.CreatedByUser = AIRequestresult[0][CONSTANTS.CreatedByUser].ToString();
                        ingrainRequest.ModifiedOn = AIRequestresult[0][CONSTANTS.ModifiedOn].ToString();
                        ingrainRequest.ModifiedByUser = AIRequestresult[0][CONSTANTS.ModifiedByUser].ToString();
                        ingrainRequest.ServiceID = AIRequestresult[0]["ServiceId"].ToString();
                        ingrainRequest.ModelName = AIRequestresult[0][CONSTANTS.ModelName].ToString();
                        ingrainRequest.PredictionURL = appSettings.AICorePredictionURL + "?=" + ingrainRequest.CorrelationId;
                        if (AIRequestresult[0].ToString().Contains(CONSTANTS.DataSetUId))
                            ingrainRequest.DataSetUId = AIRequestresult[0][CONSTANTS.DataSetUId].ToString();
                        JObject ingData = new JObject();
                        ingData.Add("CorrelationId", ingrainRequest.CorrelationId);
                        ingData.Add("DataSource", AIRequestresult[0]["DataSource"].ToString());
                        ingData.Add("ModelName", AIRequestresult[0]["ModelName"].ToString());
                        ingData.Add("ModifiedOn", AIRequestresult[0][CONSTANTS.ModifiedOn].ToString());
                        ingrainRequest.ingestData = ingData;
                        data.clusteringStatus.Add(ingrainRequest);
                    }
                }

            }
            List<VisulalisationData> visuallist = new List<VisulalisationData>();
            foreach (string corrid in VisulaCorrelationIdList)
            {
                VisulalisationData Visualisation_data = new VisulalisationData();
                var Visualcollection = _database.GetCollection<BsonDocument>(CONSTANTS.ClusteringVisualization);
                var Visualfilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, corrid);
                var Visualprojection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
                var Visualresult = Visualcollection.Find(Visualfilter).Project<BsonDocument>(Visualprojection).ToList();
                if (Visualresult.Count > 0)
                {
                    for (int i = 0; i < Visualresult.Count; i++)
                    {
                        bool DBEncryptionRequired = DBEncrypt_Clustering(Visualresult[i][CONSTANTS.CorrelationId].ToString());
                        string FrequencyCount;
                        string VisualisationResponse;
                        if (DBEncryptionRequired)
                        {
                            VisualisationResponse = _encryptionDecryption.Decrypt(Visualresult[i]["Visualization_Response"].ToString());
                            FrequencyCount = _encryptionDecryption.Decrypt(Visualresult[i]["Frequency_Count"].ToString());
                        }
                        else
                        {
                            VisualisationResponse = Visualresult[i]["Visualization_Response"].ToString();
                            FrequencyCount = Visualresult[i]["Frequency_Count"].ToString();
                        }
                        Visualisation_data.ClientID = Visualresult[i][CONSTANTS.ClientID].ToString();
                        Visualisation_data.DCUID = Visualresult[i][CONSTANTS.DCUID].ToString();
                        Visualisation_data.CorrelationId = Visualresult[i][CONSTANTS.CorrelationId].ToString();
                        Visualisation_data.ModelName = Visualresult[i][CONSTANTS.ModelName].ToString();
                        Visualisation_data.Clustering_type = Visualresult[i]["Clustering_type"].ToString();
                        Visualisation_data.Visualization_Response = JsonConvert.JsonConvert.DeserializeObject<JObject>(VisualisationResponse);
                        Visualisation_data.Frequency_Count = JsonConvert.JsonConvert.DeserializeObject<JObject>(FrequencyCount);
                    }
                    visuallist.Add(Visualisation_data);
                }
            }
            data.VisulalisationDatas = visuallist;
            return data;
        }

        public void AssignFrequency(ClusteringAPIModel clusteringAPIModel, ClustModelFrequency frequency, dynamic data, string feature)
        {
            string frequencyName = ((JProperty)((JContainer)data).First).Name;
            frequency.RetryCount = data[CONSTANTS.RetryCount] != null ? Convert.ToInt32(data[CONSTANTS.RetryCount]) : 0;
            switch (frequencyName)
            {
                case CONSTANTS.Hourly:
                    frequency.Hourly = data[frequencyName];
                    clusteringAPIModel.TrainingFrequencyInDays = feature.Equals(CONSTANTS.Training) ? frequency.Hourly * 60 : clusteringAPIModel.TrainingFrequencyInDays;
                    clusteringAPIModel.PredictionFrequencyInDays = feature.Equals(CONSTANTS.Prediction) ? frequency.Hourly * 60 : clusteringAPIModel.PredictionFrequencyInDays;
                    break;

                case CONSTANTS.Daily:
                    frequency.Daily = data[frequencyName];
                    clusteringAPIModel.RetrainingFrequencyInDays = feature.Equals(CONSTANTS.Retraining) ? frequency.Daily : clusteringAPIModel.RetrainingFrequencyInDays;
                    clusteringAPIModel.TrainingFrequencyInDays = feature.Equals(CONSTANTS.Training) ? frequency.Daily : clusteringAPIModel.TrainingFrequencyInDays;
                    clusteringAPIModel.PredictionFrequencyInDays = feature.Equals(CONSTANTS.Prediction) ? frequency.Daily : clusteringAPIModel.PredictionFrequencyInDays;
                    break;

                case CONSTANTS.Weekly:
                    frequency.Weekly = data[frequencyName];
                    clusteringAPIModel.RetrainingFrequencyInDays = feature.Equals(CONSTANTS.Retraining) ? (frequency.Weekly * 7) : clusteringAPIModel.RetrainingFrequencyInDays;
                    clusteringAPIModel.TrainingFrequencyInDays = feature.Equals(CONSTANTS.Training) ? (frequency.Weekly * 7) : clusteringAPIModel.TrainingFrequencyInDays;
                    clusteringAPIModel.PredictionFrequencyInDays = feature.Equals(CONSTANTS.Prediction) ? (frequency.Weekly * 7) : clusteringAPIModel.PredictionFrequencyInDays;
                    break;

                case CONSTANTS.Monthly:
                    frequency.Monthly = data[frequencyName];
                    clusteringAPIModel.RetrainingFrequencyInDays = feature.Equals(CONSTANTS.Retraining) ? (frequency.Monthly * 30) : clusteringAPIModel.RetrainingFrequencyInDays;
                    clusteringAPIModel.TrainingFrequencyInDays = feature.Equals(CONSTANTS.Training) ? (frequency.Monthly * 30) : clusteringAPIModel.TrainingFrequencyInDays;
                    clusteringAPIModel.PredictionFrequencyInDays = feature.Equals(CONSTANTS.Prediction) ? (frequency.Monthly * 30) : clusteringAPIModel.PredictionFrequencyInDays;
                    break;

                case CONSTANTS.Fortnightly:
                    frequency.Fortnightly = data[frequencyName];
                    clusteringAPIModel.RetrainingFrequencyInDays = feature.Equals(CONSTANTS.Retraining) ? (frequency.Fortnightly * 14) : clusteringAPIModel.RetrainingFrequencyInDays;
                    clusteringAPIModel.TrainingFrequencyInDays = feature.Equals(CONSTANTS.Training) ? (frequency.Fortnightly * 14) : clusteringAPIModel.TrainingFrequencyInDays;
                    clusteringAPIModel.PredictionFrequencyInDays = feature.Equals(CONSTANTS.Prediction) ? (frequency.Fortnightly * 14) : clusteringAPIModel.PredictionFrequencyInDays;
                    break;
            }
        }


        /// <summary>
        /// Clustering Ingest Data
        /// </summary>ProblemType
        /// <param name="clusteringAPI">ClusteringAPIModel Model</param>
        /// <returns></returns>
        public bool ClusteringIngestData(ClusteringAPIModel clusteringAPI)
        {
            //bool DBEncryptionRequired = DBEncrypt_Clustering(clusteringAPI.CorrelationId);
            if (appSettings.isForAllData)
            {
                if (!string.IsNullOrEmpty(Convert.ToString(clusteringAPI.UserId)))
                    clusteringAPI.UserId = _encryptionDecryption.Encrypt(Convert.ToString(clusteringAPI.UserId));
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ClusteringAPIService), nameof(ClusteringIngestData), "clusteringAPIDATA22" + "--" + JsonConvert.JsonConvert.SerializeObject(clusteringAPI),
                string.IsNullOrEmpty(clusteringAPI.CorrelationId) ? default(Guid) : new Guid(clusteringAPI.CorrelationId) , string.Empty,  string.Empty, clusteringAPI.ClientID,clusteringAPI.DCUID);
            clusteringAPI.CreatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            clusteringAPI.ModifiedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            clusteringAPI.CreatedBy = clusteringAPI.UserId;
            clusteringAPI.ModifiedBy = clusteringAPI.UserId;
            clusteringAPI.DBEncryptionRequired = appSettings.DBEncryption;
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.Clustering_IngestData);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, clusteringAPI.CorrelationId) & Builders<BsonDocument>.Filter.Eq(CONSTANTS.ClientID, clusteringAPI.ClientID) & Builders<BsonDocument>.Filter.Eq(CONSTANTS.DCUID, clusteringAPI.DCUID);
            var projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
            var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                if (clusteringAPI.retrain)
                {
                    var aIServiceRequestStatus = JsonConvert.JsonConvert.DeserializeObject<ClusteringAPIModel>(result[0].ToJson());
                    clusteringAPI._id = Guid.NewGuid().ToString();
                    clusteringAPI.DataSource = result[0]["DataSource"].ToString();
                    clusteringAPI.SourceName = aIServiceRequestStatus.SourceName;
                    clusteringAPI.DataSetUId = aIServiceRequestStatus.DataSetUId;//result[0][CONSTANTS.DataSetUId].ToString();
                    clusteringAPI.MaxDataPull = Convert.ToInt32(result[0][CONSTANTS.MaxDataPull]);
                    aIServiceRequestStatus.StopWords = clusteringAPI.StopWords;
                    aIServiceRequestStatus.Ngram = clusteringAPI.Ngram;
                    clusteringAPI.ParamArgs = aIServiceRequestStatus.ParamArgs;
                    clusteringAPI.Language = aIServiceRequestStatus.Language;
                    clusteringAPI.IsCarryOutRetraining = aIServiceRequestStatus.IsCarryOutRetraining;
                    clusteringAPI.IsOnline = aIServiceRequestStatus.IsOnline;
                    clusteringAPI.IsOffline = aIServiceRequestStatus.IsOffline;
                    clusteringAPI.Training = aIServiceRequestStatus.Training;
                    clusteringAPI.Prediction = aIServiceRequestStatus.Prediction;
                    clusteringAPI.Retraining = aIServiceRequestStatus.Retraining;
                    clusteringAPI.RetrainingFrequencyInDays = aIServiceRequestStatus.RetrainingFrequencyInDays;
                    clusteringAPI.TrainingFrequencyInDays = aIServiceRequestStatus.TrainingFrequencyInDays;
                    clusteringAPI.PredictionFrequencyInDays = aIServiceRequestStatus.PredictionFrequencyInDays;
                    var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(clusteringAPI);
                    var insertDocument = BsonSerializer.Deserialize<BsonDocument>(jsonData);
                    collection.DeleteOne(filter);
                    Thread.Sleep(1000);
                    collection.InsertOne(insertDocument);
                }
                else
                {
                    var aIServiceRequestStatus = JsonConvert.JsonConvert.DeserializeObject<ClusteringAPIModel>(result[0].ToJson());
                    clusteringAPI.DataSource = aIServiceRequestStatus.DataSource;
                    clusteringAPI.SourceName = aIServiceRequestStatus.SourceName;
                    aIServiceRequestStatus._id = Guid.NewGuid().ToString();
                    aIServiceRequestStatus.Columnsselectedbyuser = clusteringAPI.Columnsselectedbyuser;
                    aIServiceRequestStatus.UniId = clusteringAPI.UniId;
                    aIServiceRequestStatus.PageInfo = clusteringAPI.PageInfo;
                    aIServiceRequestStatus.SelectedModels = clusteringAPI.SelectedModels;
                    aIServiceRequestStatus.ProblemType = clusteringAPI.ProblemType;
                    aIServiceRequestStatus.retrain = clusteringAPI.retrain;
                    aIServiceRequestStatus.StopWords = clusteringAPI.StopWords;
                    aIServiceRequestStatus.Ngram = clusteringAPI.Ngram;
                    aIServiceRequestStatus.DataSetUId = clusteringAPI.DataSetUId;
                    //aIServiceRequestStatus.DataSource = clusteringAPI.DataSource;                    
                    aIServiceRequestStatus.MaxDataPull = clusteringAPI.MaxDataPull;
                    aIServiceRequestStatus.CreatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    aIServiceRequestStatus.ModifiedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(aIServiceRequestStatus);
                    var insertDocument = BsonSerializer.Deserialize<BsonDocument>(jsonData);
                    collection.DeleteOne(filter);
                    Thread.Sleep(1000);
                    collection.InsertOne(insertDocument);
                }
                return true;
            }
            else
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(ClusteringAPIService), nameof(ClusteringIngestData), "clusteringAPIDATA55" + "--" + JsonConvert.JsonConvert.SerializeObject(clusteringAPI),
                     string.IsNullOrEmpty(clusteringAPI.CorrelationId) ? default(Guid) : new Guid(clusteringAPI.CorrelationId), string.Empty, string.Empty, clusteringAPI.ClientID, clusteringAPI.DCUID);
                var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(clusteringAPI);
                var insertDocument = BsonSerializer.Deserialize<BsonDocument>(jsonData);
                collection.InsertOneAsync(insertDocument);
                return true;
            }
        }



        /// <summary>
        /// ClusteringAsAPI
        /// </summary>
        /// <param name="clusterData"></param>
        /// <param name="httpContext"></param>
        /// <returns></returns>
        public MethodReturn<Response> ClusteringAsAPI(IFormCollection clusterData, HttpContext httpContext)
        {
            JObject payload = new JObject();
            var returnResponse = new MethodReturn<Response>();
            Request request = new Request();
            ClusteringAPIModel clusteringAPI = new ClusteringAPIModel();
            string baseUrl = string.Empty;
            string apiPath = string.Empty;
            var serviceResponse = new MethodReturn<object>();
            foreach (var key in clusterData.Keys)
            {
                clusterData.TryGetValue(key, out var stringVal);
                switch (key)
                {
                    case CONSTANTS.CorrelationId:
                        clusteringAPI.CorrelationId = stringVal.ToString();
                        break;
                    case CONSTANTS.PageInfo:
                        clusteringAPI.PageInfo = stringVal.ToString();
                        break;
                    case CONSTANTS.UserId:
                        clusteringAPI.UserId = stringVal.ToString();
                        break;
                    case CONSTANTS.DCUID:
                        clusteringAPI.DCUID = stringVal.ToString();
                        break;
                    case CONSTANTS.ClientID:
                        clusteringAPI.ClientID = stringVal.ToString();
                        break;
                    case CONSTANTS.UniId:
                        clusteringAPI.UniId = stringVal.ToString();
                        break;
                    case CONSTANTS.Token:
                        clusteringAPI.Token = stringVal.ToString();
                        break;
                    case CONSTANTS.ProblemType:
                        if (!CommonUtility.IsDataValid(Convert.ToString(stringVal)))
                        {
                            throw new Exception(string.Format(CONSTANTS.InValidData, CONSTANTS.ProblemType));
                        }
                        clusteringAPI.ProblemType = JsonConvert.JsonConvert.DeserializeObject<JObject>(stringVal.ToString());
                        break;
                    case CONSTANTS.Selected_Models:
                        if (!CommonUtility.IsDataValid(Convert.ToString(stringVal)))
                        {
                            throw new Exception(string.Format(CONSTANTS.InValidData, CONSTANTS.Selected_Models));
                        }
                        clusteringAPI.SelectedModels = JsonConvert.JsonConvert.DeserializeObject<JObject>(stringVal.ToString());
                        break;
                    case CONSTANTS.ServiceID:
                        clusteringAPI.ServiceID = stringVal.ToString();
                        break;
                    case CONSTANTS.ModelName:
                        clusteringAPI.ModelName = stringVal.ToString();
                        break;
                    case "retrain":
                        clusteringAPI.retrain = Convert.ToBoolean(stringVal.ToString());
                        break;
                    case "selectedColumns":
                        if (!CommonUtility.IsDataValid(Convert.ToString(stringVal)))
                        {
                            throw new Exception(string.Format(CONSTANTS.InValidData, "selectedColumns"));
                        }
                        clusteringAPI.Columnsselectedbyuser = JsonConvert.JsonConvert.DeserializeObject<List<object>>(stringVal.ToString());
                        break;
                    case "StopWords":
                        if (!CommonUtility.IsDataValid(Convert.ToString(stringVal)))
                        {
                            throw new Exception(string.Format(CONSTANTS.InValidData, "StopWords"));
                        }
                        clusteringAPI.StopWords = JsonConvert.JsonConvert.DeserializeObject<List<string>>(stringVal.ToString());
                        break;
                    case "Ngram":
                        if (!CommonUtility.IsDataValid(Convert.ToString(stringVal)))
                        {
                            throw new Exception(string.Format(CONSTANTS.InValidData, "Ngram"));
                        }
                        clusteringAPI.Ngram = JsonConvert.JsonConvert.DeserializeObject<int[]>(stringVal.ToString());
                        break;
                    case CONSTANTS.DataSetUId:
                        if (!string.IsNullOrEmpty(stringVal) && stringVal.ToString() != CONSTANTS.undefined)
                            clusteringAPI.DataSetUId = stringVal.ToString();
                        break;
                    case CONSTANTS.MaxDataPull:
                        if (!string.IsNullOrEmpty(stringVal) && stringVal.ToString() != CONSTANTS.undefined)
                            clusteringAPI.MaxDataPull = Convert.ToInt32(stringVal);
                        break;

                }
            }
            if (!CommonUtility.GetValidUser(clusteringAPI.UserId))
            {
                throw new Exception("UserName/UserId is Invalid");
            }

            if (!CommonUtility.IsValidGuid(clusteringAPI.CorrelationId))
            {
                throw new Exception(string.Format(CONSTANTS.InValidGUID, "CorrelationId"));
            }
            if (!CommonUtility.IsValidGuid(clusteringAPI.ClientID))
            {
                throw new Exception(string.Format(CONSTANTS.InValidGUID, "ClientID"));
            }
            if (!CommonUtility.IsValidGuid(clusteringAPI.ServiceID))
            {
                throw new Exception(string.Format(CONSTANTS.InValidGUID, "ServiceID"));
            }
            if (!CommonUtility.IsValidGuid(clusteringAPI.DCUID))
            {
                throw new Exception(string.Format(CONSTANTS.InValidGUID, "DCUID"));
            }
            if (!CommonUtility.IsValidGuid(clusteringAPI.UniId))
            {
                throw new Exception(string.Format(CONSTANTS.InValidGUID, "UniId"));
            }
            if (!CommonUtility.IsValidGuid(clusteringAPI.DataSetUId))
            {
                throw new Exception(string.Format(CONSTANTS.InValidGUID, "DataSetUId"));
            }

            if (!CommonUtility.IsDataValid(clusteringAPI.PageInfo))
            {
                throw new Exception(string.Format(CONSTANTS.InValidData, "PageInfo"));
            }
            if (!CommonUtility.IsDataValid(clusteringAPI.Token))
            {
                throw new Exception(string.Format(CONSTANTS.InValidData, "Token"));
            }
            if (!CommonUtility.IsDataValid(clusteringAPI.ModelName))
            {
                throw new Exception(string.Format(CONSTANTS.InValidData, "ModelName"));
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ClusteringAPIService), nameof(ClusteringAsAPI), "clusteringAPIDATA11" + "--" + JsonConvert.JsonConvert.SerializeObject(clusteringAPI),
                string.IsNullOrEmpty(clusteringAPI.CorrelationId) ? default(Guid) : new Guid(clusteringAPI.CorrelationId), string.Empty, string.Empty, clusteringAPI.ClientID, clusteringAPI.DCUID);
            Service service = GetAiCoreServiceDetails(clusteringAPI.ServiceID);
            if (clusteringAPI.Columnsselectedbyuser == null)
            {
                throw new Exception("No columns selected");
            }
            else
            {
                if (clusteringAPI.Columnsselectedbyuser.Count <= 0)
                {
                    throw new Exception("No columns selected");
                }
            }
            if (string.IsNullOrEmpty(clusteringAPI.CorrelationId))
            {
                clusteringAPI.CorrelationId = Guid.NewGuid().ToString();
            }
            if (string.IsNullOrEmpty(clusteringAPI.UniId))
            {
                clusteringAPI.UniId = Guid.NewGuid().ToString().Trim();
            }
            var fileCollection = httpContext.Request.Form.Files;
            List<JToken> lstFilePath = new List<JToken>();
            if (CommonUtility.ValidateFileUploaded(fileCollection))
            {
                throw new FormatException(Resource.IngrainResx.InValidFileName);
            }
            //bool DBEncryptionRequired = DBEncrypt_Clustering(clusteringAPI.CorrelationId);
            //string encryptedUser = clusteringAPI.UserId;
            //if (DBEncryptionRequired)
            //{
            //    if (!string.IsNullOrEmpty(Convert.ToString(clusteringAPI.UserId)))
            //        encryptedUser = _encryptionDecryption.Encrypt(Convert.ToString(clusteringAPI.UserId));
            //}

            dynamic PamToken;
            if (appSettings.Environment == CONSTANTS.PAMEnvironment)
            {
                PamToken = _iPhoenixTokenService.GeneratePAMToken();
                if (PamToken != null && PamToken["token"] != string.Empty)
                {
                    clusteringAPI.Token = Convert.ToString(PamToken["token"]);
                }
            }
            else
            {
                clusteringAPI.Token = this.PythonAIServiceToken();
            }

            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(ClusteringAsAPI), "clusteringAPIDATA11" + "--" + JsonConvert.JsonConvert.SerializeObject(clusteringAPI),
                string.IsNullOrEmpty(clusteringAPI.CorrelationId) ? default(Guid) : new Guid(clusteringAPI.CorrelationId), string.Empty, string.Empty, clusteringAPI.ClientID, clusteringAPI.DCUID);
            this.ClusteringIngestData(clusteringAPI);
            payload["CorrelationId"] = clusteringAPI.CorrelationId;
            payload["UniId"] = clusteringAPI.UniId;
            payload["UserId"] = clusteringAPI.UserId;
            payload["pageInfo"] = clusteringAPI.PageInfo;
            payload["IsDataUpload"] = false;
            payload["Publish_Case"] = false;
            var response = new Response(clusteringAPI.ClientID, clusteringAPI.DCUID, clusteringAPI.ServiceID);
            baseUrl = appSettings.ClusteringPythonURL;
            apiPath = service.PrimaryTrainApiUrl;//"clustering/ModelTraining";//CONSTANTS.Clustering_ModelTraining; // "clustering/ModelTraining";//service.PrimaryTrainApiUrl;

            //call py from win service
            AIServiceRequestStatus ingestRequest = new AIServiceRequestStatus();
            ingestRequest.CorrelationId = clusteringAPI.CorrelationId;
            ingestRequest.SelectedColumnNames = clusteringAPI.Columnsselectedbyuser;
            ingestRequest.ModelName = clusteringAPI.ModelName;
            ingestRequest.ClientId = clusteringAPI.ClientID;
            ingestRequest.UniId = clusteringAPI.UniId;
            ingestRequest.DeliveryconstructId = clusteringAPI.DCUID;
            ingestRequest.ServiceId = clusteringAPI.ServiceID;
            ingestRequest.DataSource = clusteringAPI.DataSource;
            ingestRequest.SourceName = clusteringAPI.SourceName;
            ingestRequest.DataSetUId = clusteringAPI.DataSetUId;
            ingestRequest.PageInfo = clusteringAPI.PageInfo;
            ingestRequest.Status = "N";
            ingestRequest.CreatedByUser = clusteringAPI.UserId;
            ingestRequest.CreatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");//DateTime.UtcNow.ToString();
            ingestRequest.ModifiedByUser = clusteringAPI.UserId;
            ingestRequest.ModifiedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            ingestRequest.Payload = payload;
            ingestRequest.baseUrl = baseUrl;
            ingestRequest.apiPath = apiPath;
            ingestRequest.token = string.Empty;
            ingestRequest.MaxDataPull = clusteringAPI.MaxDataPull;
            this.InsertAIServiceRequest(ingestRequest);

            //LOGGING.LogManager.Logger.LogProcessInfo(typeof(ClusteringAPIService), nameof(ClusteringAsAPI), baseUrl + apiPath,
            //    new Guid(ingestRequest.CorrelationId),"","",ingestRequest.ClientId,ingestRequest.DeliveryconstructId);
            //serviceResponse = this.RoutePOSTRequest(string.Empty, new Uri(baseUrl), apiPath,
            //    payload, clusteringAPI.CorrelationId, clusteringAPI.retrain, false);
            //LOGGING.LogManager.Logger.LogProcessInfo(typeof(ClusteringAPIService), nameof(ClusteringIngestData), serviceResponse.ToString(),new Guid(ingestRequest.CorrelationId), "", "", ingestRequest.ClientId, ingestRequest.DeliveryconstructId);

            //adding conditions in routePost here only
            bool isIngest = false;
            if (isIngest)
            {
                serviceResponse.Message = "Success";
            }
            else if (clusteringAPI.retrain)
            {
                serviceResponse.Message = "Re-Training initiated Successfully";
            }
            else
            {
                serviceResponse.Message = "Training initiated Successfully";
            }
            serviceResponse.IsSuccess = true;

            response.ResponseData = serviceResponse.ReturnValue;
            returnResponse.Message = serviceResponse.Message;
            returnResponse.IsSuccess = serviceResponse.IsSuccess;
            response.CorrelationId = clusteringAPI.CorrelationId;
            response.SetResponseDate(DateTime.UtcNow);
            returnResponse.ReturnValue = response;
            return returnResponse;
            //}
        }
        public string InsertAIServiceRequest(AIServiceRequestStatus aIServiceRequestStatus)
        {
            //bool DBEncryptionRequired = DBEncrypt_Clustering(aIServiceRequestStatus.CorrelationId);
            //if (DBEncryptionRequired)
            //{
            //    if (!string.IsNullOrEmpty(Convert.ToString(aIServiceRequestStatus.CreatedByUser)))
            //        aIServiceRequestStatus.CreatedByUser = _encryptionDecryption.Encrypt(Convert.ToString(aIServiceRequestStatus.CreatedByUser));
            //    if (!string.IsNullOrEmpty(Convert.ToString(aIServiceRequestStatus.ModifiedByUser)))
            //        aIServiceRequestStatus.ModifiedByUser = _encryptionDecryption.Encrypt(Convert.ToString(aIServiceRequestStatus.ModifiedByUser));
            //}
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ClusteringAPIService), nameof(InsertAIServiceRequest), "ServiceId : " + aIServiceRequestStatus.ServiceId + CONSTANTS.START, string.Empty, string.Empty, aIServiceRequestStatus.ClientId, aIServiceRequestStatus.DeliveryconstructId);
            var collection = _database.GetCollection<BsonDocument>("AIServiceRequestStatus");
            var filter = Builders<BsonDocument>.Filter.Eq("UniId", aIServiceRequestStatus.UniId);
            var result = collection.Find(filter).ToList();
            var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(aIServiceRequestStatus);
            var insertDocument = BsonSerializer.Deserialize<BsonDocument>(jsonData);
            if (result.Count > 0)
            {
                collection.DeleteOne(filter);
            }
            Thread.Sleep(1000);
            collection.InsertOneAsync(insertDocument);
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ClusteringAPIService), nameof(InsertAIServiceRequest), "ServiceId : " + aIServiceRequestStatus.ServiceId + CONSTANTS.END, string.Empty, string.Empty, aIServiceRequestStatus.ClientId, aIServiceRequestStatus.DeliveryconstructId);
            return "Success";

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="token"></param>
        /// <param name="baseUrl"></param>
        /// <param name="apiPath"></param>
        /// <param name="requestPayload"></param>
        /// <param name="isReturnArray"></param>
        /// <returns></returns>
        public MethodReturn<object> RoutePOSTRequest(string token, Uri baseUrl, string apiPath, JObject requestPayload, string correlationid, bool retrain, bool isIngest)
        {
            MethodReturn<object> returnValue = new MethodReturn<object>();

            dynamic PamToken;
            if (appSettings.Environment == CONSTANTS.PAMEnvironment)
            {
                PamToken = _iPhoenixTokenService.GeneratePAMToken();
                if (PamToken != null && PamToken["token"] != string.Empty)
                {
                    token = Convert.ToString(PamToken["token"]);
                }
            }
            else
            {
                token = this.PythonAIServiceToken();
            }

            Task.Run(() =>
            {
                try
                {
                    StringContent content = new StringContent(requestPayload.ToString(), Encoding.UTF8, "application/json");
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(ClusteringAPIService), nameof(RoutePOSTRequest), "Base URL: " + baseUrl.ToString() + " API Path: " + apiPath, string.IsNullOrEmpty(correlationid) ? default(Guid) : new Guid(correlationid), string.Empty, string.Empty, string.Empty, string.Empty);
                    HttpResponseMessage message = webHelper.InvokePOSTRequest(token, baseUrl.ToString(), apiPath, content);
                    //if (message == null)
                    //{
                    //    returnValue.Message = "Error";
                    //    returnValue.IsSuccess = false;
                    //}
                    if (message.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(ClusteringAPIService), nameof(RoutePOSTRequest), message.Content.ReadAsStringAsync().Result, string.IsNullOrEmpty(correlationid) ? default(Guid) : new Guid(correlationid), string.Empty, string.Empty, string.Empty, string.Empty);
                        //if (isIngest)
                        //{
                        //    returnValue.Message = "Success";
                        //}
                        //else if (retrain)
                        //{
                        //    returnValue.Message = "Re-Training initiated Successfully";
                        //}
                        //else
                        //{
                        //    returnValue.Message = "Training initiated Successfully";
                        //}
                        //returnValue.IsSuccess = true;
                        //returnValue.Message = "Training initiated Successfully";
                        //returnValue.IsSuccess = true;
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(ClusteringAPIService), nameof(RoutePOSTRequest), "CorrelationId : " + correlationid + " ClusterAPI : " + message.StatusCode, string.IsNullOrEmpty(correlationid) ? default(Guid) : new Guid(correlationid), string.Empty, string.Empty, string.Empty, string.Empty);
                    }
                    else
                    {
                        //returnValue.Message = "Python error: " + message.ReasonPhrase;
                        //returnValue.IsSuccess = false;
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(ClusteringAPIService), nameof(RoutePOSTRequest), "CorrelationId : " + correlationid + " ClusterAPI : " + "Python error:" + message.ReasonPhrase + " - " + message.StatusCode, string.IsNullOrEmpty(correlationid) ? default(Guid) : new Guid(correlationid), string.Empty, string.Empty, string.Empty, string.Empty);
                    }
                }
                catch (Exception ex)
                {
                    //returnValue.Message = "Python API call error: " + ex.Message;
                    //returnValue.IsSuccess = false;
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(ClusteringAPIService), nameof(RoutePOSTRequest), "CorrelationId : " + correlationid + " ClusterAPI : Python API call error" + ex.Message, string.Empty, string.Empty, string.Empty, string.Empty);
                }
            });
            if (isIngest)
            {
                returnValue.Message = "Success";
            }
            else if (retrain)
            {
                returnValue.Message = "Re-Training initiated Successfully";
            }
            else
            {
                returnValue.Message = "Training initiated Successfully";
            }
            returnValue.IsSuccess = true;
            return returnValue;
        }

        /// <summary>
        /// Clustering Ingest Data
        /// </summary>
        /// <param name="clusteringAPI">ClusteringAPIModel Model</param>
        /// <returns></returns>
        public bool Evaluate(dynamic data)
        {
            string correlationId = data["CorrelationId"].ToString();
            string clientID = data["ClientID"].ToString();
            string dCUID = data["DCUID"].ToString();
            var collection = _database.GetCollection<BsonDocument>("Clustering_Eval");
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId) & Builders<BsonDocument>.Filter.Eq(CONSTANTS.ClientID, clientID) & Builders<BsonDocument>.Filter.Eq(CONSTANTS.DCUID, dCUID);
            var projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
            var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            //if (result.Count > 0)
            //{
            //    var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(data);
            //    var insertDocument = BsonSerializer.Deserialize<BsonDocument>(jsonData);
            //    collection.DeleteOne(filter);
            //    collection.InsertOneAsync(insertDocument);
            //    return true;
            //}
            //else
            //{
            bool DBEncryptionRequired = DBEncrypt_Clustering(correlationId);
            if (DBEncryptionRequired)
            {
                data["Data"] = _encryptionDecryption.Encrypt(data["Data"].ToString());
                if (!string.IsNullOrEmpty(Convert.ToString(data["UserId"])))
                {
                    data["UserId"] = _encryptionDecryption.Encrypt(data["UserId"].ToString());
                }
            }
            var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(data);

            var insertDocument = BsonSerializer.Deserialize<BsonDocument>(jsonData);
            collection.InsertOneAsync(insertDocument);
            return true;
            //}
        }

        /// <summary>
        /// Clustering Ingest Data
        /// </summary>
        /// <param name="clusteringAPI">ClusteringAPIModel Model</param>
        /// <returns></returns>
        public JObject EvaluateDetails(string correlationId, string uniId)
        {
            JObject data = new JObject();
            var dataProcessingCollection = _database.GetCollection<BsonDocument>("Clustering_EvalTestResults");
            var dtaProcessingFilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId) & Builders<BsonDocument>.Filter.Eq(CONSTANTS.UniId, uniId);
            var dtaProcessingProjection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
            var dtaProcessingResult = dataProcessingCollection.Find(dtaProcessingFilter).Project<BsonDocument>(dtaProcessingProjection).ToList();
            if (dtaProcessingResult.Count > 0)
            {
                for (var i = 0; i < dtaProcessingResult.Count; i++)
                {
                    bool DBEncryptionRequired = DBEncrypt_Clustering(dtaProcessingResult[i][CONSTANTS.CorrelationId].ToString());
                    if (DBEncryptionRequired)
                    {
                        try
                        {
                            if (dtaProcessingResult[i].Contains("CreatedBy") && !string.IsNullOrEmpty(Convert.ToString(dtaProcessingResult[i]["CreatedBy"])))
                                dtaProcessingResult[i]["CreatedBy"] = _encryptionDecryption.Decrypt(Convert.ToString(dtaProcessingResult[i]["CreatedBy"]));
                        }
                        catch (Exception ex)
                        {
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ClusteringAPIService), nameof(EvaluateDetails), ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty);
                        }
                        //    try
                        //    {
                        //        if (dtaProcessingResult[i].Contains(CONSTANTS.ModifiedByUser) && !string.IsNullOrEmpty(Convert.ToString(dtaProcessingResult[i][CONSTANTS.ModifiedByUser])))
                        //            dtaProcessingResult[i][CONSTANTS.ModifiedByUser] = _encryptionDecryption.Decrypt(Convert.ToString(dtaProcessingResult[i][CONSTANTS.ModifiedByUser]));
                        //    }
                        //    catch (Exception) { }
                    }
                }
                data = JObject.Parse(dtaProcessingResult[0].ToString());
            }
            return data;
        }


        public JObject EvaluatePythonCall(string token, Uri baseUrl, string apiPath, JObject requestPayload, string correlationid, string uniId)
        {
            //MethodReturn<object> returnValue = new MethodReturn<object>();
            JObject returnValue = new JObject();
            dynamic PamToken;
            if (appSettings.Environment == CONSTANTS.PAMEnvironment)
            {
                PamToken = _iPhoenixTokenService.GeneratePAMToken();
                if (PamToken != null && PamToken["token"] != string.Empty)
                {
                    token = Convert.ToString(PamToken["token"]);
                }
            }
            else
            {
                token = this.PythonAIServiceToken();
            }

            //Task.Run(() =>
            //{
            try
            {
                StringContent content = new StringContent(requestPayload.ToString(), Encoding.UTF8, "application/json");
                HttpResponseMessage message = webHelper.InvokePOSTRequest(token, baseUrl.ToString(), apiPath, content);
                if (message.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    //TrainingResponse ReturnValue = JsonConvert.DeserializeObject<TrainingResponse>(message.Content.ReadAsStringAsync().Result);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(ClusteringAPIService), nameof(EvaluatePythonCall), message.Content.ReadAsStringAsync().Result, string.IsNullOrEmpty(correlationid) ? default(Guid) : new Guid(correlationid), string.Empty, string.Empty, string.Empty, string.Empty);
                    //returnValue.Message = ReturnValue.message;
                    returnValue = this.EvaluateDetails(correlationid, uniId);
                }
                else
                {
                    returnValue.Add("Message", "Error from python:" + message.ReasonPhrase);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(ClusteringAPIService), nameof(RoutePOSTRequest), message.StatusCode.ToString() + message.ReasonPhrase, string.IsNullOrEmpty(correlationid) ? default(Guid) : new Guid(correlationid), string.Empty, string.Empty, string.Empty, string.Empty);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(ClusteringAPIService), nameof(RoutePOSTRequest), ex.Message, string.Empty, string.Empty, string.Empty, string.Empty);
            }
            //});
            // returnValue.Message = "Training initiated successfully";
            //returnValue.IsSuccess = true;
            return returnValue;
        }

        public bool GetClusteringModelName(ClusteringAPIModel clusteringAPI)
        {
            string encryptedUser = clusteringAPI.UserId;
            if (!string.IsNullOrEmpty(encryptedUser))
                encryptedUser = _encryptionDecryption.Encrypt(encryptedUser);
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.Clustering_IngestData);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.ServiceID, clusteringAPI.ServiceID) & Builders<BsonDocument>.Filter.Eq(CONSTANTS.ClientID, clusteringAPI.ClientID) & Builders<BsonDocument>.Filter.Eq(CONSTANTS.DCUID, clusteringAPI.DCUID) & (Builders<BsonDocument>.Filter.Eq("CreatedBy", clusteringAPI.UserId) | Builders<BsonDocument>.Filter.Eq("CreatedBy", encryptedUser)) & Builders<BsonDocument>.Filter.Eq(CONSTANTS.ModelName, clusteringAPI.ModelName);
            var projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
            var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                var data = result[0]["ModelName"].ToString();
                if (data.ToLower() == clusteringAPI.ModelName.ToLower())
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }


        public string PythonAIServiceToken()
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ClusteringAPIService), nameof(PythonAIServiceToken), CONSTANTS.START + appSettings.authProvider.ToUpper(), string.Empty, string.Empty, string.Empty, string.Empty);
            dynamic token = string.Empty;
            if (appSettings.authProvider.ToUpper() == "FORM")
            {
                var username = Convert.ToString(appSettings.username);
                var password = Convert.ToString(appSettings.password);
                var tokenendpointurl = Convert.ToString(appSettings.tokenAPIUrl);
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

            }
            else if (appSettings.authProvider.ToUpper() == "AZUREAD")
            {

                var client = new RestClient(appSettings.token_Url_VDS);
                var request = new RestRequest(Method.POST);
                request.AddHeader("cache-control", "no-cache");
                request.AddHeader("content-type", "application/x-www-form-urlencoded");
                request.AddParameter("application/x-www-form-urlencoded", "grant_type=" + appSettings.Grant_Type_VDS +
                   //"&client_id=" + appSettings.clientId_VDS +
                   //"&client_secret=" + appSettings.clientSecret_VDS +
                   //"&scope=" + appSettings.scopeStatus_VDS +
                   //"&resource=" + appSettings.clientId_VDS,
                   "&client_id=" + appSettings.clientId_VDS + //"56c1ded1-9805-4bc2-9788-eb4b5a8e07aa" + //appSettings.clientId_clustering +//"56c1ded1-9805-4bc2-9788-eb4b5a8e07aa" +
                    "&client_secret=" + appSettings.clientSecret_VDS + //"@C=IYenjlw1@rdOr7SfsX:GTVR19T4SY" + //appSettings.client_secret_clustering +//"@C=IYenjlw1@rdOr7SfsX:GTVR19T4SY" +
                    "&resource=" + appSettings.resourceId, //"api://f2784d3a-a507-462b-a27a-b8963ceb0f6a", //appSettings.resource_clustering,//"api://f2784d3a-a507-462b-a27a-b8963ceb0f6a",
                   ParameterType.RequestBody);
                var requestBuilder = new StringBuilder();
                foreach (var param in request.Parameters)
                {
                    requestBuilder.AppendFormat("{0}: {1}\r\n", param.Name, param.Value);
                }
                requestBuilder.ToString();
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(ClusteringAPIService), nameof(PythonAIServiceToken), "PYTHON TOKEN PARAMS -- " + requestBuilder, string.Empty, string.Empty, string.Empty, string.Empty);
                IRestResponse response = client.Execute(request);
                string json = response.Content;
                // Retrieve and Return the Access Token                
                var tokenObj = JsonConvert.JsonConvert.DeserializeObject(json) as dynamic;
                token = Convert.ToString(tokenObj.access_token);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ClusteringAPIService), nameof(PythonAIServiceToken), "END -" + appSettings.authProvider.ToUpper(), string.Empty, string.Empty, string.Empty, string.Empty);
            return token;
        }

        public List<object> ClusteringViewData(string correlationId, string modelType)
        {
            List<object> lstInputData = new List<object>();
            var collection = _database.GetCollection<BsonDocument>("Clustering_ViewTrainedData");
            var filter = Builders<BsonDocument>.Filter.Eq("CorrelationId", correlationId) & Builders<BsonDocument>.Filter.Eq("ModelName", modelType);
            var projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id).Include("InputData").Include("CorrelationId").Include("ModelName");
            var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                for (int i = 0; i < result.Count; i++)
                {
                    string json = string.Empty;
                    bool DBEncryptionRequired = DBEncrypt_Clustering(result[i][CONSTANTS.CorrelationId].ToString());
                    if (DBEncryptionRequired)
                        json = _encryptionDecryption.Decrypt(result[i][CONSTANTS.Input_Data].ToString());
                    else
                        json = result[i][CONSTANTS.Input_Data].ToString();
                    lstInputData.AddRange(JsonConvert.JsonConvert.DeserializeObject<List<object>>(json));
                }
            }
            return lstInputData;
        }

        public JObject DownloadPythonCall(string token, Uri baseUrl, string apiPath, JObject requestPayload)
        {
            JObject returnValue = new JObject();

            dynamic PamToken;
            if (appSettings.Environment == CONSTANTS.PAMEnvironment)
            {
                PamToken = _iPhoenixTokenService.GeneratePAMToken();
                if (PamToken != null && PamToken["token"] != string.Empty)
                {
                    token = Convert.ToString(PamToken["token"]);
                }
            }
            else
            {
                token = this.PythonAIServiceToken();
            }

            try
            {
                StringContent content = new StringContent(requestPayload.ToString(), Encoding.UTF8, "application/json");
                HttpResponseMessage message = webHelper.InvokePOSTRequest(token, baseUrl.ToString(), apiPath, content);
                if (message.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(ClusteringAPIService), nameof(DownloadPythonCall), message.Content.ReadAsStringAsync().Result, string.Empty, string.Empty, string.Empty, string.Empty);
                    if (message.Content.ReadAsStringAsync().Result.ToString() == "Sucess!")
                        returnValue.Add("Message", "Success");
                    else
                        returnValue.Add("Message", message.Content.ReadAsStringAsync().Result.ToString());
                }
                else
                {
                    returnValue.Add("Message", "Error from python:" + message.ReasonPhrase);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(ClusteringAPIService), nameof(DownloadPythonCall), message.StatusCode.ToString() + message.ReasonPhrase, string.Empty, string.Empty, string.Empty, string.Empty);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(ClusteringAPIService), nameof(DownloadPythonCall), ex.Message, string.Empty, string.Empty, string.Empty, string.Empty);
            }
            return returnValue;
        }

        public DownloadMappedDataStatus DownloadMappedDataStatus(string correlationId, string modelType, string pageInfo)
        {
            List<object> lstInputData = new List<object>();
            DownloadMappedDataStatus StatusList = new DownloadMappedDataStatus();
            var collection = _database.GetCollection<BsonDocument>("Clustering_StatusTable");
            var filter = Builders<BsonDocument>.Filter.Eq("CorrelationId", correlationId) & Builders<BsonDocument>.Filter.Eq("ModelType", modelType) & Builders<BsonDocument>.Filter.Eq("pageInfo", pageInfo);
            var projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
            var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                for (var i = 0; i < result.Count; i++)
                {
                    bool DBEncryptionRequired = DBEncrypt_Clustering(result[i][CONSTANTS.CorrelationId].ToString());
                    if (DBEncryptionRequired)
                    {
                        try
                        {
                            if (result[i].Contains(CONSTANTS.CreatedByUser) && !string.IsNullOrEmpty(Convert.ToString(result[i][CONSTANTS.CreatedByUser])))
                                result[i][CONSTANTS.CreatedByUser] = _encryptionDecryption.Decrypt(Convert.ToString(result[i][CONSTANTS.CreatedByUser]));
                        }
                        catch (Exception ex)
                        {
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ClusteringAPIService), nameof(DownloadMappedDataStatus), ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty);
                        }
                        try
                        {
                            if (result[i].Contains(CONSTANTS.ModifiedByUser) && !string.IsNullOrEmpty(Convert.ToString(result[i][CONSTANTS.ModifiedByUser])))
                                result[i][CONSTANTS.ModifiedByUser] = _encryptionDecryption.Decrypt(Convert.ToString(result[i][CONSTANTS.ModifiedByUser]));
                        }
                        catch (Exception ex)
                        {
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ClusteringAPIService), nameof(DownloadMappedDataStatus), ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty);
                        }
                    }
                }
                StatusList = JsonConvert.JsonConvert.DeserializeObject<DownloadMappedDataStatus>(result[0].ToJson());
                if (StatusList.Progress == "100" & StatusList.Status == "C")
                {
                    var collection1 = _database.GetCollection<BsonDocument>("Clustering_ViewMappedData");
                    var filter1 = Builders<BsonDocument>.Filter.Eq("CorrelationId", correlationId) & Builders<BsonDocument>.Filter.Eq("ModelName", modelType);
                    var projection1 = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
                    var result1 = collection1.Find(filter1).Project<BsonDocument>(projection1).ToList();
                    for (int i = 0; i < result1.Count; i++)
                    {
                        string json = string.Empty;
                        bool DBEncryptionRequired = DBEncrypt_Clustering(result1[i][CONSTANTS.CorrelationId].ToString());
                        if (DBEncryptionRequired)
                            json = _encryptionDecryption.Decrypt(result1[i][CONSTANTS.Input_Data].ToString());
                        else
                            json = result1[i][CONSTANTS.Input_Data].ToString();
                        lstInputData.AddRange(JsonConvert.JsonConvert.DeserializeObject<List<object>>(json));
                    }
                    StatusList.InputData = lstInputData;
                }
            }
            return StatusList;
        }

        public VisulalisationDataStatus VisulalisationDataStatus(string correlationId, string modelType, string pageInfo)
        {
            VisulalisationDataStatus Visualisationstatus = new VisulalisationDataStatus();
            var collection = _database.GetCollection<BsonDocument>("Clustering_StatusTable");
            var filter = Builders<BsonDocument>.Filter.Eq("CorrelationId", correlationId) & Builders<BsonDocument>.Filter.Eq("ModelType", modelType) & Builders<BsonDocument>.Filter.Eq("pageInfo", pageInfo);
            var projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
            var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                Visualisationstatus = JsonConvert.JsonConvert.DeserializeObject<VisulalisationDataStatus>(result[0].ToJson());
                if (Visualisationstatus.Progress == "100" & Visualisationstatus.Status == "C")
                {
                    var collection1 = _database.GetCollection<BsonDocument>("Clustering_Visualization");
                    result = collection1.Find(filter).Project<BsonDocument>(projection).ToList();
                    for (int i = 0; i < result.Count; i++)
                    {
                        bool DBEncryptionRequired = DBEncrypt_Clustering(result[i][CONSTANTS.CorrelationId].ToString());
                        string FrequencyCount;
                        string VisualisationResponse;
                        if (DBEncryptionRequired)
                        {
                            VisualisationResponse = _encryptionDecryption.Decrypt(result[i]["Visualization_Response"].ToString());
                            FrequencyCount = _encryptionDecryption.Decrypt(result[i]["Frequency_Count"].ToString());
                        }
                        else
                        {
                            VisualisationResponse = result[i]["Visualization_Response"].ToString();
                            FrequencyCount = result[i]["Frequency_Count"].ToString();
                        }
                        Visualisationstatus.Visualization_Response = JsonConvert.JsonConvert.DeserializeObject<JObject>(VisualisationResponse);
                        Visualisationstatus.Frequency_Count = JsonConvert.JsonConvert.DeserializeObject<JObject>(FrequencyCount);
                    }
                }
            }
            else
            {
                Visualisationstatus.CorrelationId = correlationId;
                Visualisationstatus.Status = "New";
                Visualisationstatus.Progress = "0%";
                Visualisationstatus.Message = "Record yet to be inserted by py";
            }
            return Visualisationstatus;
        }

        public MethodReturn<object> AISeriveIngestData
        (string ModelName, string userId, string clientUID,
        string deliveryUID, string ParentFileName, string MappingFlag,
        string Source, string Category, HttpContext httpContext,
        bool DBEncryption, string serviceId, string Language, string pageInfo)
        {
            return AISeriveIngestData(ModelName, userId, clientUID, deliveryUID, ParentFileName, MappingFlag,Source, Category, httpContext, String.Empty,
             DBEncryption, serviceId, Language, pageInfo,string.Empty);
        }

        public MethodReturn<object> AISeriveIngestData
        (string ModelName, string userId, string clientUID,
        string deliveryUID, string ParentFileName, string MappingFlag,
        string Source, string Category, HttpContext httpContext,
        bool DBEncryption, string serviceId, string Language, string pageInfo, string E2EUID)
        {
            return AISeriveIngestData(ModelName, userId, clientUID, deliveryUID, ParentFileName, MappingFlag, Source, Category, httpContext, String.Empty,
             DBEncryption, serviceId, Language, pageInfo, string.Empty);
        }
        public MethodReturn<object> AISeriveIngestData
            (string ModelName, string userId, string clientUID,
            string deliveryUID, string ParentFileName, string MappingFlag,
            string Source, string Category, HttpContext httpContext, string uploadType,
            bool DBEncryption, string serviceId, string Language, string pageInfo, string E2EUID)
        {
            var correlationId = string.Empty;

            MethodReturn<object> message = new MethodReturn<object>();
            JObject payload = new JObject();
            string baseUrl = string.Empty;
            string apiPath = string.Empty;
            bool encryptDB = false;

            if (appSettings.isForAllData)
            {
                if (appSettings.DBEncryption)
                {
                    encryptDB = true;
                }
                else
                {
                    encryptDB = false;
                }
            }
            else
            {
                if (appSettings.DBEncryption && DBEncryption)
                {

                    encryptDB = true;
                }
                else
                {
                    encryptDB = false;
                }

            }
            string modelStatus = "Upload InProgress";
            string modelMessage = "Upload InProgress";
            string dataSource = "File";

            if (string.IsNullOrEmpty(pageInfo))
            {
                pageInfo = "InvokeIngestData";
            }
            string MappingColumns = string.Empty;
            string filePath = string.Empty;
            int counter = 0;
            Service service = GetAiCoreServiceDetails(serviceId);
            string ParentFileNamePath = string.Empty, FilePath = string.Empty, postedFileName = string.Empty, DataSourceFilePath = string.Empty, FileName = string.Empty, SaveFileName = string.Empty;
            var fileCollection = httpContext.Request.Form.Files;
            string Entities = string.Empty, Metrices = string.Empty, InstaML = string.Empty, Entity_Names = string.Empty, Metric_Names = string.Empty, Customdata = string.Empty, CustomSourceItems = string.Empty;
            if (!string.IsNullOrEmpty(ModelName) && !string.IsNullOrEmpty(deliveryUID) && !string.IsNullOrEmpty(clientUID))
            {
                //filePath = appSettings.UploadFilePath;
                //Directory.CreateDirectory(Path.Combine(filePath, appSettings.SavedModels));
                //filePath = System.IO.Path.Combine(filePath, appSettings.AppData);
                //System.IO.Directory.CreateDirectory(filePath);

                IFormCollection collection = httpContext.Request.Form;
                var entityitems = collection[CONSTANTS.pad];
                var metricitems = collection[CONSTANTS.metrics];
                var InstaMl = collection[CONSTANTS.InstaMl];
                var EntitiesNames = collection[CONSTANTS.EntitiesName];
                var MetricsNames = collection[CONSTANTS.MetricNames];
                var Customdetails = collection["Custom"];
                var dataSetUId = collection["DataSetUId"];
                var maxDataPull = collection["MaxDataPull"];
                var CustomDataSourceDetails = collection["CustomDataPull"];
                int MaxDataPull = (maxDataPull.ToString().Trim() != CONSTANTS.Null) ? Convert.ToInt32(maxDataPull) : 0;
                string sourceName = string.Empty;
                
                if (dataSetUId == "undefined" || dataSetUId == "null")
                    dataSetUId = string.Empty;
                correlationId = collection["CorrelationId"];

                if (!CommonUtility.IsValidGuid(Convert.ToString(collection["CorrelationId"])))
                {
                    throw new Exception(string.Format(CONSTANTS.InValidGUID, "CorrelationId"));
                }
                if (!CommonUtility.IsValidGuid(Convert.ToString(collection["DataSetUId"])))
                {
                    throw new Exception(string.Format(CONSTANTS.InValidGUID, "DataSetUId"));
                }

                if (!CommonUtility.IsDataValid(Convert.ToString(collection[CONSTANTS.pad])))
                {
                    throw new Exception(string.Format(CONSTANTS.InValidData, CONSTANTS.pad));
                }
                if (!CommonUtility.IsDataValid(Convert.ToString(collection[CONSTANTS.metrics])))
                {
                    throw new Exception(string.Format(CONSTANTS.InValidData, CONSTANTS.metrics));
                }
                if (!CommonUtility.IsDataValid(Convert.ToString(collection[CONSTANTS.InstaMl])))
                {
                    throw new Exception(string.Format(CONSTANTS.InValidData, CONSTANTS.InstaMl));
                }
                if (!CommonUtility.IsDataValid(Convert.ToString(collection[CONSTANTS.EntitiesName])))
                {
                    throw new Exception(string.Format(CONSTANTS.InValidData, CONSTANTS.EntitiesName));
                }
                if (!CommonUtility.IsDataValid(Convert.ToString(collection[CONSTANTS.MetricNames])))
                {
                    throw new Exception(string.Format(CONSTANTS.InValidData, CONSTANTS.MetricNames));
                }
                if (!CommonUtility.IsDataValid(Convert.ToString(collection["Custom"])))
                {
                    throw new Exception(string.Format(CONSTANTS.InValidData, "Custom"));
                }
                ClusteringAPIModel clusteringAPIModel = new ClusteringAPIModel();
                clusteringAPIModel.ServiceID = serviceId;
                clusteringAPIModel.ClientID = clientUID;
                clusteringAPIModel.UserId = userId;
                clusteringAPIModel.ModelName = ModelName;
                clusteringAPIModel.DCUID = deliveryUID;
                clusteringAPIModel.UserId = userId;
                bool isExists = this.GetClusteringModelName(clusteringAPIModel);
                if (isExists & string.IsNullOrEmpty(correlationId))
                {
                    throw new Exception("Model Name already exist");
                }
                else
                {
                    if (string.IsNullOrEmpty(correlationId))
                    {
                        correlationId = Guid.NewGuid().ToString();
                    }
                    else
                    {
                        pageInfo = "InvokeIngestData";
                        modelStatus = "InProgress";
                        modelMessage = "ReTrain is in Progress";
                    }
                    if (Customdetails.Count() > 0)
                    {
                        foreach (var item in Customdetails)
                        {
                            if (item.Trim() != "{}")
                            {
                                Customdata += item;
                                sourceName = "Custom";
                                dataSource = "Custom";
                                if (appSettings.Environment.Equals(CONSTANTS.FDSEnvironment) || appSettings.Environment.Equals(CONSTANTS.PAMEnvironment))
                                    dataSource = "Entity";
                            }
                            else
                                Customdata = CONSTANTS.Null;
                        }
                    }

                    if (entityitems.Count() > 0)
                    {
                        foreach (var item in entityitems)
                        {
                            if (item.Trim() != "{}")
                            {
                                Entities += item;
                                dataSource = "Entity";
                                sourceName = "pad";
                            }
                            else
                                Entities = CONSTANTS.Null;
                        }
                    }

                    if (metricitems.Count() > 0)
                    {
                        foreach (var item in metricitems)
                        {
                            if (item.Trim() != "{}")
                            {
                                Metrices += item;
                                sourceName = "metric";
                                dataSource = "Metric";
                            }
                            else
                                Metrices = CONSTANTS.Null;
                        }
                    }

                    if (CustomDataSourceDetails.Count() > 0)
                    {
                        foreach (var item in CustomDataSourceDetails)
                        {
                            if (item.Trim() != "{}")
                            {
                                CustomSourceItems += item;
                                dataSource = Source;
                                sourceName = Source;
                            }
                            else
                                CustomSourceItems = CONSTANTS.Null;
                        }
                    }

                    if (InstaMl.Count() > 0)
                    {
                        foreach (var item in InstaMl)
                        {
                            if (item.Trim() != "{}")
                            {
                                InstaML += item;
                                DataSourceFilePath += "InstaMl,";
                                dataSource = "InstaMl";
                            }
                            else
                                InstaML = CONSTANTS.Null;
                        }
                    }
                    if (EntitiesNames.Count > 0)
                    {
                        foreach (var item in EntitiesNames)
                        {
                            if (item.Trim() != "{}")
                            {
                                Entity_Names += item;
                                DataSourceFilePath = Entity_Names + ",";
                            }
                        }
                    }
                    if (MetricsNames.Count() > 0)
                    {
                        foreach (var item in MetricsNames)
                        {
                            if (item.Trim() != "{}")
                            {
                                Metric_Names += item;
                                DataSourceFilePath += Metric_Names + ",";
                            }
                        }
                    }
                    if (!string.IsNullOrEmpty(dataSetUId))
                    {
                        if (uploadType == "ExternalAPIDataSet")
                            sourceName = uploadType;
                        else
                            sourceName = "File_DataSet";
                        dataSource = sourceName;
                    }
                    if (fileCollection.Count != 0)
                    {
                        for (int i = 0; i < fileCollection.Count; i++)
                        {
                            var folderPath = Guid.NewGuid().ToString();
                            var fileName = fileCollection[i].FileName;
                            filePath = appSettings.ClusteringFilespath.ToString();
                            var filePath1 = filePath + correlationId + "/" + folderPath + "/" + fileName;
                            if (!Directory.Exists(filePath + correlationId + "/" + folderPath))
                                Directory.CreateDirectory(filePath + correlationId + "/" + folderPath);
                            var postedFile = fileCollection[i];
                            if (postedFile.Length <= 0)
                            {
                                message.Message = CONSTANTS.FileEmpty;
                                return message;
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
                    if (!string.IsNullOrEmpty(postedFileName))
                    {
                        sourceName = "file";
                    }
                    //Service service = _aICoreService.GetAiCoreServiceDetails(serviceId);
                    ingrainRequest = new ClusteringAPIModel
                    {
                        _id = Guid.NewGuid().ToString(),
                        CorrelationId = correlationId,
                        DataSetUId = dataSetUId,
                        ClientID = clientUID,
                        DCUID = deliveryUID,
                        ServiceID = serviceId,
                        ModelName = ModelName,
                        UniId = Guid.NewGuid().ToString(),
                        ParamArgs = null,
                        CreatedBy = userId,
                        CreatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        ModifiedBy = userId,
                        ModifiedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        UserId = userId,
                        PageInfo = pageInfo,
                        DataSource = dataSource,
                        SourceName = sourceName,
                        MaxDataPull = MaxDataPull
                    };
                    //ingrainRequest = new AIServiceRequestStatus
                    //{
                    //    _id = Guid.NewGuid().ToString(),
                    //    CorrelationId = correlationId,
                    //    ClientId = clientUID,
                    //    DeliveryconstructId = deliveryUID,
                    //    ServiceId = serviceId,
                    //    Status = null,
                    //    ModelName = ModelName,
                    //    RequestStatus = CONSTANTS.New,
                    //    Message = null,
                    //    UniId = Guid.NewGuid().ToString(),
                    //    Progress = null,
                    //    PageInfo = pageInfo,
                    //    SourceDetails = null,
                    //    DataSource = dataSource,
                    //    //ColumnNames = null,
                    //    //SelectedColumnNames = null,
                    //    CreatedByUser = userId,
                    //    CreatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    //    ModifiedByUser = userId,
                    //    ModifiedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    //};

                    _filepath = new Filepath();
                    if (postedFileName != "")
                        _filepath.fileList = postedFileName;
                    else
                        _filepath.fileList = "null";

                    parentFile = new ParentFile();
                    if (ParentFileName != "undefined")
                    {
                        parentFile.Type = Source;
                        if (Source == "file")
                        {
                            parentFile.Name = ParentFileNamePath;
                        }
                        else
                            parentFile.Name = ParentFileName;
                    }
                    else
                    {
                        parentFile.Type = "null";
                        parentFile.Name = "null";
                    }
                    if (Customdata != CONSTANTS.Null && Customdata != string.Empty)
                    {
                        var AppIntegCollection = _database.GetCollection<AppIntegration>(CONSTANTS.AppIntegration);
                        var filterBuilder = Builders<AppIntegration>.Filter;                        
                        var AppFilter = filterBuilder.Eq("ApplicationName", appSettings.ApplicationName);

                        var Projection = Builders<AppIntegration>.Projection.Include("ApplicationID").Exclude("_id");
                        var AppData = AppIntegCollection.Find(AppFilter).Project<AppIntegration>(Projection).FirstOrDefault();

                        var fileParams = JsonConvert.JsonConvert.DeserializeObject<InputParams>(Customdata);
                        InputParams param = new InputParams
                        {
                            ClientID = clientUID,
                            E2EUID = E2EUID = string.IsNullOrEmpty(E2EUID) ? CONSTANTS.Null : E2EUID,
                            DeliveryConstructID = deliveryUID,
                            Environment = fileParams.Environment,
                            RequestType = fileParams.RequestType,
                            ServiceType = fileParams.ServiceType,
                            StartDate = fileParams.StartDate,
                            EndDate = fileParams.EndDate,
                        };
                        CustomPayloads AppPayload = new CustomPayloads
                        {
                            AppId = AppData.ApplicationID,
                            HttpMethod = CONSTANTS.POST,
                            AppUrl = appSettings.GetVdsPAMDataURL,
                            InputParameters = param,
                            AICustom = "True"
                        };
                        CustomUploadFile Customfile = new CustomUploadFile
                        {
                            CorrelationId = correlationId,
                            ClientUID = clientUID,
                            DeliveryConstructUId = deliveryUID,
                            Parent = parentFile,
                            Flag = CONSTANTS.Null,
                            mapping = CONSTANTS.Null,
                            mapping_flag = CONSTANTS.False,
                            pad = CONSTANTS.Null,
                            metric = CONSTANTS.Null,
                            InstaMl = CONSTANTS.Null,
                            fileupload = _filepath,
                            Customdetails = AppPayload,

                        };
                        ingrainRequest.ParamArgs = Customfile.ToJson();
                    }
                    else if (CustomSourceItems != CONSTANTS.Null && CustomSourceItems != string.Empty)
                    {
                        if (Source.ToUpper() == CONSTANTS.CustomDataApi.ToUpper())
                        {
                            var fileParams = JsonConvert.JsonConvert.DeserializeObject<CustomInputData>(CustomSourceItems);
                            if (fileParams.Data != null)
                            {
                                fileParams.Data.Type = "API";
                            }

                            if (fileParams.Data.Authentication.UseIngrainAzureCredentials)
                            {
                                AzureDetails oAuthCredentials = new AzureDetails
                                {
                                    grant_type = appSettings.Grant_Type,
                                    client_secret = appSettings.clientSecret,
                                    client_id = appSettings.clientId,
                                    resource = appSettings.resourceId
                                };

                                string TokenUrl = appSettings.token_Url;
                                string token = _customDataService.CustomUrlToken("Ingrain", oAuthCredentials, TokenUrl);
                                if (!String.IsNullOrEmpty(token))
                                {
                                    fileParams.Data.Authentication.Token = token;
                                }
                                else
                                {
                                    message.Message = CONSTANTS.IngrainTokenBlank;
                                    return message;
                                }
                            }

                            //Encrypting API related Information
                            var Data = _encryptionDecryption.Encrypt(JsonConvert.JsonConvert.SerializeObject(fileParams.Data));

                            CustomSourceDTO CustomAPIData = new CustomSourceDTO
                            {
                                CorrelationId = correlationId,
                                ClientUID = clientUID,
                                E2EUID = E2EUID,
                                DeliveryConstructUId = deliveryUID,
                                Parent = parentFile,
                                Flag = CONSTANTS.Null,
                                mapping = CONSTANTS.Null,
                                mapping_flag = MappingFlag,
                                pad = CONSTANTS.Null,
                                metric = CONSTANTS.Null,
                                InstaMl = CONSTANTS.Null,
                                fileupload = _filepath,
                                StartDate = CONSTANTS.Null,
                                EndDate = CONSTANTS.Null,
                                Customdetails = CONSTANTS.Null,
                                CustomSource = Data,
                                TargetNode = fileParams.Data.TargetNode
                            };

                            ingrainRequest.ParamArgs = CustomAPIData.ToJson();

                            if (fileParams != null)
                            {
                                fileParams.DbEncryption = encryptDB;
                            }

                            CustomDataSourceModel CustomDataSource = new CustomDataSourceModel
                            {
                                _id = Guid.NewGuid().ToString(),
                                CorrelationId = correlationId,
                                CustomDataPullType = CONSTANTS.CustomDataApi,
                                CustomSourceDetails = JsonConvert.JsonConvert.SerializeObject(fileParams),
                                CreatedByUser = userId
                            };
                            _customDataService.InsertCustomDataSource(CustomDataSource, CONSTANTS.AICustomDataSource);
                        }
                        else if (Source.ToUpper() == CONSTANTS.CustomDbQuery.ToUpper())
                        {
                            var DateColumn = Convert.ToString(collection["DateColumn"]);
                            var Query = CustomSourceItems;
                            //var QueryParams = JsonConvert.DeserializeObject<CustomDataInputParams>(CustomSourceItems);
                            QueryDTO QueryData = new QueryDTO();

                            if (!string.IsNullOrEmpty(Query))
                            {
                                QueryData.Type = CONSTANTS.CustomDbQuery;
                                QueryData.Query = Query;
                                QueryData.DateColumn = DateColumn;
                            }
                            var Data = _encryptionDecryption.Encrypt(Newtonsoft.Json.JsonConvert.SerializeObject(QueryData));

                            CustomQueryParamArgs CustomQueryData = new CustomQueryParamArgs
                            {
                                CorrelationId = correlationId,
                                ClientUID = clientUID,
                                E2EUID = E2EUID,
                                DeliveryConstructUId = deliveryUID,
                                Parent = parentFile,
                                Flag = CONSTANTS.Null,
                                mapping = CONSTANTS.Null,
                                mapping_flag = MappingFlag,
                                pad = CONSTANTS.Null,
                                metric = CONSTANTS.Null,
                                InstaMl = CONSTANTS.Null,
                                fileupload = _filepath,
                                Customdetails = CONSTANTS.Null,
                                CustomSource = Data
                            };
                            ingrainRequest.ParamArgs = CustomQueryData.ToJson();

                            CustomDataSourceModel CustomDataSource = new CustomDataSourceModel
                            {
                                _id = Guid.NewGuid().ToString(),
                                CorrelationId = correlationId,
                                CustomDataPullType = CONSTANTS.CustomDbQuery,
                                CustomSourceDetails = Convert.ToString(Query),
                                CreatedByUser = userId
                            };
                            _customDataService.InsertCustomDataSource(CustomDataSource, CONSTANTS.AICustomDataSource);
                        }
                    }
                    else
                    {
                        fileUpload = new FileUpload
                        {
                            CorrelationId = correlationId,
                            ClientUID = clientUID,
                            DeliveryConstructUId = deliveryUID,
                            Parent = parentFile,
                            Flag = CONSTANTS.Null,
                            mapping = MappingColumns,
                            mapping_flag = MappingFlag,
                            pad = Entities,
                            metric = Metrices,
                            InstaMl = InstaML,
                            fileupload = _filepath,
                            Customdetails = CONSTANTS.Null

                        };
                        ingrainRequest.ParamArgs = fileUpload.ToJson();
                    }
                    ingrainRequest.Language = Language;
                    ClustModelFrequency clustModelFrequency = new ClustModelFrequency();
                    if (dataSource == "Entity")
                    {
                        dynamic data = collection;
                        bool iscarryOutRetraining = Convert.ToBoolean(collection["IsCarryOutRetraining"]);
                        bool isOnline = Convert.ToBoolean(collection["IsOnline"]);
                        bool isOffline = Convert.ToBoolean(collection["IsOffline"]);
                        JObject training = new JObject();
                        JObject prediction = new JObject();
                        JObject retraining = new JObject();

                        if (Convert.ToBoolean(collection["IsCarryOutRetraining"]) || Convert.ToBoolean(collection["IsOnline"]) || Convert.ToBoolean(collection["IsOffline"]))
                        {
                            iscarryOutRetraining = Convert.ToBoolean(collection["IsCarryOutRetraining"]);
                            isOnline = Convert.ToBoolean(collection["IsOnline"]);
                            isOffline = Convert.ToBoolean(collection["IsOffline"]);
                            if (collection["Training"].ToString() != "{}")
                                training = Newtonsoft.Json.JsonConvert.DeserializeObject<JObject>(collection["Training"]);
                            if (collection["Prediction"].ToString() != "{}")
                                prediction = Newtonsoft.Json.JsonConvert.DeserializeObject<JObject>(collection["Prediction"]);
                            if (collection["Retraining"].ToString() != "{}")
                                retraining = Newtonsoft.Json.JsonConvert.DeserializeObject<JObject>(collection["Retraining"]);
                        }

                        //ClusteringAPIModel clusteringfreqAPIModel = new ClusteringAPIModel();
                        ingrainRequest.IsCarryOutRetraining = iscarryOutRetraining;
                        ingrainRequest.IsOnline = isOnline;
                        ingrainRequest.IsOffline = isOffline;
                        ingrainRequest.Retraining = clustModelFrequency;
                        ingrainRequest.Training = clustModelFrequency;
                        ingrainRequest.Prediction = clustModelFrequency;
                        if (iscarryOutRetraining && retraining != null && ((JContainer)retraining).HasValues)
                        {
                            ingrainRequest.Retraining = new ClustModelFrequency();
                            this.AssignFrequency(ingrainRequest, ingrainRequest.Retraining, retraining, CONSTANTS.Retraining);

                        }
                        if (ingrainRequest.IsOffline)
                        {
                            if (training != null && ((JContainer)training).HasValues)
                            {
                                ingrainRequest.Training = new ClustModelFrequency();
                                this.AssignFrequency(ingrainRequest, ingrainRequest.Training, training, CONSTANTS.Training);
                            }
                            if (prediction != null && ((JContainer)prediction).HasValues)
                            {
                                ingrainRequest.Prediction = new ClustModelFrequency();
                                this.AssignFrequency(ingrainRequest, ingrainRequest.Prediction, prediction, CONSTANTS.Prediction);
                            }
                        }
                    }
                    this.ClusteringIngestData(ingrainRequest);

                    //if (service.ServiceCode != "CLUSTERING")
                    //{
                    //    this.CreateAICoreModel(ingrainRequest.ClientId, ingrainRequest.DeliveryconstructId, ingrainRequest.ServiceId, ingrainRequest.CorrelationId, ingrainRequest.UniId, ingrainRequest.ModelName, null, modelStatus, modelMessage, ingrainRequest.CreatedByUser, dataSource);
                    //}
                    //payload["correlationId"] = correlationId;
                    //payload["userId"] = userId;
                    //payload["pageInfo"] = pageInfo;
                    //payload["UniqueId"] = ingrainRequest.UniId;
                    //string apipayload = string.Empty;
                    //if (service != null)
                    //{
                    //    if (!string.IsNullOrWhiteSpace(service.PrimaryTrainApiUrl))
                    //    {
                    //        baseUrl = appSettings.AICorePythonURL;
                    //        apiPath = service.PrimaryTrainApiUrl;
                    //        apiPath = apiPath + "?" + "correlationId=" + correlationId + "&userId=" + userId + "&pageInfo=" + pageInfo + "&UniqueId=" + ingrainRequest.UniId;
                    //    }
                    //}
                    //message = _aICoreService.RouteGETRequest(string.Empty, new Uri(baseUrl), apiPath, false);
                    //string encryptedUserId = userId;
                    //if (encryptDB)
                    //{
                    //    if (!string.IsNullOrEmpty(Convert.ToString(userId)))
                    //        encryptedUserId = _encryptionDecryption.Encrypt(Convert.ToString(userId));
                    //}
                    payload["CorrelationId"] = correlationId;//clusteringAPI.CorrelationId;
                    payload["UniId"] = ingrainRequest.UniId;////clusteringAPI.UniId;
                    payload["UserId"] = ingrainRequest.UserId;//clusteringAPI.UserId;
                    payload["pageInfo"] = pageInfo;//clusteringAPI.PageInfo;
                    payload["IsDataUpload"] = true;
                    payload["Publish_Case"] = false;
                    var response = new Response(clientUID, deliveryUID, serviceId);
                    baseUrl = appSettings.ClusteringPythonURL;
                    apiPath = service.PrimaryTrainApiUrl;//"clustering/ModelTraining";//CONSTANTS.Clustering_ModelTraining; // "clustering/ModelTraining";//service.PrimaryTrainApiUrl;

                    //LOGGING.LogManager.Logger.LogProcessInfo(typeof(ClusteringAPIService), nameof(ClusteringAsAPI), baseUrl + apiPath);
                    //message = this.RoutePOSTRequest(string.Empty, new Uri(baseUrl), apiPath,
                    //    payload, correlationId, false, true);
                    MethodReturn<object> returnValue = new MethodReturn<object>();
                    bool isIngest = true;
                    bool retrain = false;

                    //call py from win service
                    AIServiceRequestStatus ingestRequest = new AIServiceRequestStatus();
                    ingestRequest.CorrelationId = ingrainRequest.CorrelationId;
                    ingestRequest.SelectedColumnNames = ingrainRequest.Columnsselectedbyuser;
                    ingestRequest.ModelName = ingrainRequest.ModelName;
                    ingestRequest.ClientId = ingrainRequest.ClientID;
                    ingestRequest.UniId = ingrainRequest.UniId;
                    ingestRequest.DeliveryconstructId = ingrainRequest.DCUID;
                    ingestRequest.ServiceId = ingrainRequest.ServiceID;
                    ingestRequest.DataSource = ingrainRequest.DataSource;
                    ingestRequest.SourceName = ingrainRequest.SourceName;
                    ingestRequest.PageInfo = ingrainRequest.PageInfo;
                    ingestRequest.Status = "N";
                    ingestRequest.CreatedByUser = ingrainRequest.UserId;
                    ingestRequest.CreatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");//DateTime.UtcNow.ToString();
                    ingestRequest.ModifiedByUser = ingrainRequest.UserId;
                    ingestRequest.ModifiedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    ingestRequest.Payload = payload;
                    ingestRequest.baseUrl = baseUrl;
                    ingestRequest.apiPath = apiPath;
                    ingestRequest.token = string.Empty;
                    ingestRequest.DataSetUId = dataSetUId;
                    ingestRequest.MaxDataPull = MaxDataPull;
                    this.InsertAIServiceRequest(ingestRequest);

                    if (isIngest)
                    {
                        returnValue.Message = "Success";
                    }
                    else if (retrain)
                    {
                        returnValue.Message = "Re-Training initiated Successfully";
                    }
                    else
                    {
                        returnValue.Message = "Training initiated Successfully";
                    }
                    returnValue.IsSuccess = true;
                    message = returnValue;
                    message.CorrelationId = correlationId;
                }
            }
            return message;

        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="pageInfo"></param>
        /// <param name="wfId"></param>
        /// <returns></returns>
        public ClusteringStatus ClusteringServiceIngestStatus(string correlationId, string pageInfo)
        {
            ClusteringStatus ingrainRequest = new ClusteringStatus();
            var collection = _database.GetCollection<BsonDocument>("Clustering_StatusTable");
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq(CONSTANTS.CorrelationId, correlationId) & builder.Eq("pageInfo", "InvokeIngestData");
            // return ingrainRequest = collection.Find(filter).ToList().FirstOrDefault();
            var projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
            var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                bool DBEncryptionRequired = DBEncrypt_Clustering(correlationId);
                if (DBEncryptionRequired)
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(Convert.ToString(result[0][CONSTANTS.CreatedByUser])))
                            result[0][CONSTANTS.CreatedByUser] = _encryptionDecryption.Decrypt(Convert.ToString(result[0][CONSTANTS.CreatedByUser]));
                    }
                    catch (Exception ex)
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(ClusteringAPIService), nameof(ClusteringServiceIngestStatus), ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty);
                    }
                    try
                    {
                        if (!string.IsNullOrEmpty(Convert.ToString(result[0][CONSTANTS.ModifiedByUser])))
                            result[0][CONSTANTS.ModifiedByUser] = _encryptionDecryption.Decrypt(Convert.ToString(result[0][CONSTANTS.ModifiedByUser]));
                    }
                    catch (Exception ex)
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(ClusteringAPIService), nameof(ClusteringServiceIngestStatus), ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty);
                    }
                }
                ingrainRequest = JsonConvert.JsonConvert.DeserializeObject<ClusteringStatus>(result[0].ToJson());
                if (ingrainRequest.Progress == "100" & ingrainRequest.Status == "C")
                {
                    var collection1 = _database.GetCollection<BsonDocument>("Clustering_BusinessProblem");
                    var filter1 = Builders<BsonDocument>.Filter.Eq("CorrelationId", correlationId);
                    var projection1 = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id).Include("ColumnsList");
                    var result1 = collection1.Find(filter1).Project<BsonDocument>(projection1).ToList();
                    var json = result1[0]["ColumnsList"].ToJson();
                    var data = (JsonConvert.JsonConvert.DeserializeObject<List<object>>(json));
                    ingrainRequest.ColumnNames = data;
                }
            }
            else
            {
                var AIRequestcollection = _database.GetCollection<BsonDocument>(CONSTANTS.AIServiceRequestStatus);
                var AIRequestbuilder = Builders<BsonDocument>.Filter;
                var AIRequestfilter = AIRequestbuilder.Eq(CONSTANTS.CorrelationId, correlationId) & AIRequestbuilder.Eq("PageInfo", "InvokeIngestData");
                var AIRequestprojection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
                var AIRequestresult = AIRequestcollection.Find(AIRequestfilter).Project<BsonDocument>(AIRequestprojection).ToList();
                if (AIRequestresult.Count > 0)
                {
                    ingrainRequest.CorrelationId = AIRequestresult[0][CONSTANTS.CorrelationId].ToString();
                    bool DBEncryptionRequired = DBEncrypt_Clustering(ingrainRequest.CorrelationId);
                    if (DBEncryptionRequired)
                    {
                        try
                        {
                            if (AIRequestresult[0].Contains(CONSTANTS.CreatedByUser) && !string.IsNullOrEmpty(Convert.ToString(AIRequestresult[0][CONSTANTS.CreatedByUser])))
                                AIRequestresult[0][CONSTANTS.CreatedByUser] = _encryptionDecryption.Decrypt(Convert.ToString(AIRequestresult[0][CONSTANTS.CreatedByUser]));
                        }
                        catch (Exception ex)
                        {
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ClusteringAPIService), nameof(ClusteringServiceIngestStatus), ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty);
                        }
                        try
                        {
                            if (AIRequestresult[0].Contains(CONSTANTS.ModifiedByUser) && !string.IsNullOrEmpty(Convert.ToString(AIRequestresult[0][CONSTANTS.ModifiedByUser])))
                                AIRequestresult[0][CONSTANTS.ModifiedByUser] = _encryptionDecryption.Decrypt(Convert.ToString(AIRequestresult[0][CONSTANTS.ModifiedByUser]));
                        }
                        catch (Exception ex)
                        {
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ClusteringAPIService), nameof(ClusteringServiceIngestStatus), ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty);
                        }
                    }
                    if (AIRequestresult[0][CONSTANTS.Status].ToString() != "C")
                        ingrainRequest.Status = CONSTANTS.P;
                    if (AIRequestresult[0][CONSTANTS.Progress].ToString() == "BsonNull")
                        ingrainRequest.Progress = "0";
                    if (AIRequestresult[0][CONSTANTS.RequestStatus].ToString() == "BsonNull")
                        if (AIRequestresult[0][CONSTANTS.Status].ToString() == "E")
                            ingrainRequest.RequestStatus = "Error";
                        else
                            ingrainRequest.RequestStatus = "In Progress";
                    ingrainRequest.Message = AIRequestresult[0][CONSTANTS.Message].ToString();
                    ingrainRequest.UniId = AIRequestresult[0][CONSTANTS.UniId].ToString();
                    ingrainRequest.pageInfo = AIRequestresult[0][CONSTANTS.PageInfo].ToString();
                    ingrainRequest.CreatedByUser = AIRequestresult[0][CONSTANTS.CreatedByUser].ToString();
                    ingrainRequest.ModifiedOn = AIRequestresult[0][CONSTANTS.ModifiedOn].ToString();
                    ingrainRequest.ModifiedByUser = AIRequestresult[0][CONSTANTS.ModifiedByUser].ToString();
                    ingrainRequest.ServiceID = AIRequestresult[0]["ServiceId"].ToString();
                    JObject ingData = new JObject();
                    ingData.Add("CorrelationId", ingrainRequest.CorrelationId);
                    ingData.Add("DataSource", AIRequestresult[0]["DataSource"].ToString());
                    ingData.Add("ModelName", AIRequestresult[0]["ModelName"].ToString());
                    ingData.Add("ModifiedOn", AIRequestresult[0][CONSTANTS.ModifiedOn].ToString());
                    ingrainRequest.ingestData = ingData;
                }
            }
            return ingrainRequest;
        }

        private bool DBEncrypt_Clustering(string correlationid)
        {
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.Clustering_IngestData);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationid);
            var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.DBEncryptionRequired).Include(CONSTANTS.CorrelationId).Exclude("_id");
            var data = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (data.Count > 0)
            {
                BsonElement element;
                var exists = data[0].TryGetElement(CONSTANTS.DBEncryptionRequired, out element);
                if (exists)
                    return (bool)data[0][CONSTANTS.DBEncryptionRequired];
                else
                    return false;
            }
            else
            {
                return false;
            }
        }


        public string DeleteWordCloud(string correlationId)
        {
            try
            {
                var clusteringIngestData = _database.GetCollection<BsonDocument>(CONSTANTS.Clustering_IngestData);
                var ClusteringStatusTable = _database.GetCollection<BsonDocument>(CONSTANTS.ClusteringStatusTable);
                var clusteringBusinessProblem = _database.GetCollection<BsonDocument>("Clustering_BusinessProblem");
                var AIRequestcollection = _database.GetCollection<BsonDocument>(CONSTANTS.AIServiceRequestStatus);
                var filter = Builders<BsonDocument>.Filter.Eq("CorrelationId", correlationId);
                var projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
                clusteringIngestData.DeleteMany(filter);
                ClusteringStatusTable.DeleteMany(filter);
                clusteringBusinessProblem.DeleteMany(filter);
                AIRequestcollection.DeleteMany(filter);
                return "Success";
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ClusteringAPIService), nameof(DeleteWordCloud), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return ex.Message;
            }









        }



        public MethodReturn<Response> GenerateWordCloud(WordCloudRequest wordCloudRequest)
        {
            var returnResponse = new MethodReturn<Response>();
            var serviceResponse = new MethodReturn<object>();
            var clusteringIngestData = _database.GetCollection<BsonDocument>(CONSTANTS.Clustering_IngestData);
            var filter = Builders<BsonDocument>.Filter.Eq("CorrelationId", wordCloudRequest.CorrelationId);
            var projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
            var result = clusteringIngestData.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                // update word cloud selected columns
                BsonArray selectedColumns = new BsonArray();
                foreach (var column in wordCloudRequest.SelectedColumns)
                {
                    selectedColumns.Add(column);
                }
                BsonArray stopWords = new BsonArray();
                foreach (var word in wordCloudRequest.StopWords)
                {
                    stopWords.Add(word);
                }
                var update = Builders<BsonDocument>.Update.Set("Columnsselectedbyuser", selectedColumns)
                                                          .Set("StopWords", stopWords);
                clusteringIngestData.UpdateOne(filter, update);

                var response = new Response(result[0]["ClientID"].ToString(), result[0]["DCUID"].ToString(), result[0]["ServiceID"].ToString());
                response.CorrelationId = result[0]["CorrelationId"].ToString();
                JObject requestPayload = new JObject();
                requestPayload["CorrelationId"] = wordCloudRequest.CorrelationId;
                requestPayload["pageInfo"] = result[0]["PageInfo"].ToString();
                requestPayload["stopword"] = JArray.Parse(Newtonsoft.Json.JsonConvert.SerializeObject(wordCloudRequest.StopWords));
                requestPayload["max_words"] = 2000;
                Service service = GetAiCoreServiceDetails(result[0]["ServiceID"].ToString());

                serviceResponse = POSTRequest(string.Empty, new Uri(appSettings.ClusteringPythonURL), service.ApiUrl,
                           requestPayload, service.IsReturnArray);
                response.ResponseData = serviceResponse.ReturnValue;
                returnResponse.Message = serviceResponse.Message;
                returnResponse.IsSuccess = serviceResponse.IsSuccess;
                response.SetResponseDate(DateTime.UtcNow);
                returnResponse.ReturnValue = response;

                if (returnResponse.IsSuccess)
                {
                    JObject responseData = JObject.Parse(Newtonsoft.Json.JsonConvert.SerializeObject(returnResponse.ReturnValue.ResponseData));
                    if (responseData.ContainsKey("message"))
                    {
                        if (responseData["message"].ToString() == "Success")
                        {
                            var update1 = Builders<BsonDocument>.Update.Set("ValidColumnsSelected", true);
                            clusteringIngestData.UpdateOne(filter, update1);
                        }
                        else
                        {
                            var update1 = Builders<BsonDocument>.Update.Set("ValidColumnsSelected", false);
                            clusteringIngestData.UpdateOne(filter, update1);
                        }
                    }
                    else
                    {
                        var update1 = Builders<BsonDocument>.Update.Set("ValidColumnsSelected", false);
                        clusteringIngestData.UpdateOne(filter, update1);
                    }

                }
                else
                {
                    var update1 = Builders<BsonDocument>.Update.Set("ValidColumnsSelected", false);
                    clusteringIngestData.UpdateOne(filter, update1);
                }
            }
            else
            {
                returnResponse.Message = "CorrelationId not found";
                returnResponse.IsSuccess = false;
            }




            return returnResponse;
        }


        public MethodReturn<object> POSTRequest(string token, Uri baseUrl, string apiPath, JObject requestPayload, bool isReturnArray)
        {
            MethodReturn<object> returnValue = new MethodReturn<object>();

            dynamic PamToken;
            if (appSettings.Environment == CONSTANTS.PAMEnvironment)
            {
                PamToken = _iPhoenixTokenService.GeneratePAMToken();
                if (PamToken != null && PamToken["token"] != string.Empty)
                {
                    token = Convert.ToString(PamToken["token"]);
                }
            }
            else
            {
                token = PythonAIServiceToken();
            }
            try
            {

                StringContent content = new StringContent(requestPayload.ToString(), Encoding.UTF8, "application/json");
                HttpResponseMessage message = webHelper.InvokePOSTRequest(token, baseUrl.ToString(), apiPath, content);
                if (message != null && message.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    if (isReturnArray)
                        returnValue.ReturnValue = Newtonsoft.Json.JsonConvert.DeserializeObject<List<JObject>>(message.Content.ReadAsStringAsync().Result);
                    else
                        returnValue.ReturnValue = Newtonsoft.Json.JsonConvert.DeserializeObject<JObject>(message.Content.ReadAsStringAsync().Result);
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

            }
            return returnValue;
        }
        public Service GetAiCoreServiceDetails(string serviceid)
        {
            Service service = new Service();
            var serviceCollection = _database.GetCollection<BsonDocument>("Services");
            var filter = Builders<BsonDocument>.Filter.Eq("ServiceId", serviceid);
            var projection = Builders<BsonDocument>.Projection.Exclude("_id");
            var result = serviceCollection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                service = Newtonsoft.Json.JsonConvert.DeserializeObject<Service>(result[0].ToJson());
            }

            return service;
        }
        public void UpdateMapping(string CorrelationId, string ClientID, string DCUID, JObject mapping)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ClusteringAPIService), nameof(UpdateMapping), CONSTANTS.START, string.IsNullOrEmpty(CorrelationId) ? default(Guid) : new Guid(CorrelationId), string.Empty, string.Empty, ClientID, DCUID);
            try
            {
                var collection = _database.GetCollection<BsonDocument>(CONSTANTS.Clustering_IngestData);
                var builder = Builders<BsonDocument>.Filter;
                var filter = builder.Eq(CONSTANTS.CorrelationId, CorrelationId) & builder.Eq(CONSTANTS.ClientID, ClientID) & builder.Eq(CONSTANTS.DCUID, DCUID);
                var projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
                var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();
                var updateMapping = Builders<BsonDocument>.Update.Set("mapping", BsonDocument.Parse(mapping.ToString()));
                collection.UpdateOne(filter, updateMapping);
            }
            catch (Exception ex)
            { LOGGING.LogManager.Logger.LogErrorMessage(typeof(ClusteringAPIService), nameof(UpdateMapping), ex.Message, ex, string.Empty, string.Empty, ClientID, DCUID); }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ClusteringAPIService), nameof(UpdateMapping), CONSTANTS.END, string.IsNullOrEmpty(CorrelationId) ? default(Guid) : new Guid(CorrelationId), string.Empty, string.Empty, ClientID, DCUID);
        }



        public void Delete_oldVisualization(string CorrelationId, string ClientID, string DCUID, string SelectedModel)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ClusteringAPIService), nameof(Delete_oldVisualization), CONSTANTS.START, string.IsNullOrEmpty(CorrelationId) ? default(Guid) : new Guid(CorrelationId), string.Empty, string.Empty, ClientID, DCUID);
            try
            {
                var collection = _database.GetCollection<BsonDocument>(CONSTANTS.Clustering_StatusTable);
                var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, CorrelationId) & Builders<BsonDocument>.Filter.Eq(CONSTANTS.ClientID, ClientID) & Builders<BsonDocument>.Filter.Eq(CONSTANTS.DCUID, DCUID)
                            & Builders<BsonDocument>.Filter.Eq(CONSTANTS.pageInfo, CONSTANTS.ClusteringVisualization) & Builders<BsonDocument>.Filter.Eq(CONSTANTS.ModelType, SelectedModel);
                var projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
                var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();
                if (result.Count > 0)
                    collection.DeleteMany(filter);

            }
            catch (Exception ex)
            { LOGGING.LogManager.Logger.LogErrorMessage(typeof(ClusteringAPIService), nameof(Delete_oldVisualization), ex.Message, ex, string.Empty, string.Empty, ClientID, DCUID); }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ClusteringAPIService), nameof(Delete_oldVisualization), CONSTANTS.END, string.IsNullOrEmpty(CorrelationId) ? default(Guid) : new Guid(CorrelationId), string.Empty, string.Empty, ClientID, DCUID);
        }

        /// <summary>
        /// Get Clustering Record Count
        /// </summary>
        /// <param name="correlationId"></param>
        /// <returns></returns>
        public string GetClusteringRecordCount(string correlationId, string UploadType, string DataSetUId)
        {
            string recordCount = null;
            if (UploadType == "ExternalAPIDataSet" || UploadType == "File_DataSet")
            {
                var dataSetCollection = _database.GetCollection<DataSetInfoDto>(CONSTANTS.DataSetInfo);
                var filter = Builders<DataSetInfoDto>.Filter.Where(x => x.DataSetUId == DataSetUId);
                var dsprojection = Builders<DataSetInfoDto>.Projection.Exclude(CONSTANTS.Id);
                var dresult = dataSetCollection.Find(filter).Project<DataSetInfoDto>(dsprojection).FirstOrDefault();
                if (dresult != null && dresult.ValidRecordsDetails != null)
                {
                    BsonDocument ValidRecordsDetails = new BsonDocument();
                    ValidRecordsDetails = BsonDocument.Parse(JsonConvert.JsonConvert.SerializeObject(dresult.ValidRecordsDetails));
                    if (ValidRecordsDetails.Contains("Msg") && ValidRecordsDetails["Msg"] != BsonNull.Value)
                    {
                        recordCount = ValidRecordsDetails["Msg"].ToString();
                        return recordCount;
                    }
                }
            }
            else
            {
                var AIServiceRequestStatus = _database.GetCollection<BsonDocument>("Clustering_BusinessProblem");
                var AIServiceFilterBuilder = Builders<BsonDocument>.Filter;
                var AIServiceQueue = AIServiceFilterBuilder.Eq("CorrelationId", correlationId);
                var projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
                var result = AIServiceRequestStatus.Find(AIServiceQueue).Project<BsonDocument>(projection).ToList();
                if (result.Count > 0)
                {
                    foreach (var item in result)
                    {
                        if (item.Contains("Ingestion_Message"))
                        {
                            if (item["Ingestion_Message"] != BsonNull.Value)
                            {
                                recordCount = item["Ingestion_Message"].ToString();
                                return recordCount;
                            }
                        }
                    }
                }
            }
            return recordCount;
        }
    }
}
