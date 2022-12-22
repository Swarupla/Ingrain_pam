using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
using Accenture.MyWizard.Ingrain.DataAccess;
using Accenture.MyWizard.Ingrain.DataModels.AICore;
using Accenture.MyWizard.Shared.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using RestSharp;
using System.Threading.Tasks;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
using AIGENERICSERVICE = Accenture.MyWizard.Ingrain.DataModels.AICore.GenericService;
using USECASE = Accenture.MyWizard.Ingrain.DataModels.AICore.UseCase;
using Microsoft.Extensions.DependencyInjection;
using Accenture.MyWizard.Ingrain.DataModels.Models;
using System.Threading;
using System.Security.Policy;
using System.Transactions;
using System.Collections;
using MongoDB.Bson.Serialization.IdGenerators;
using System.Runtime.InteropServices;
using Accenture.MyWizard.Ingrain.BusinessDomain.Common;
using System.Globalization;
using Accenture.MyWizard.Ingrain.DataModels.AICore.UseCase;

namespace Accenture.MyWizard.Ingrain.BusinessDomain.Services
{
    public class AICoreService : IAICoreService
    {
        private readonly MongoClient _mongoClient;
        private readonly IMongoDatabase _database;
        private readonly WebHelper webHelper;
        private readonly DatabaseProvider databaseProvider;
        private readonly IngrainAppSettings appSettings;
        private IEncryptionDecryption _encryptionDecryption;
        private IngestedDataDTO _ingestedData = null;
        private AIServiceRequestStatus ingrainRequest;
        private ClusteringAPIModel clusteringAPIModel;
        Filepath _filepath = null;
        ParentFile parentFile = null;
        FileUpload fileUpload = null;
        private CallBackErrorLog auditTrailLog;
        private readonly IOptions<IngrainAppSettings> configSetting;
        private IGenericSelfservice _genericSelfservice;
        private static IClusteringAPIService _clusteringAPI { get; set; }
        private static IDataSetsService dataSetsService { get; set; }
        private static ICustomDataService _customDataService { set; get; }
        public static IPhoenixTokenService _iPhoenixTokenService { get; set; }
        public AICoreService(DatabaseProvider db, IOptions<IngrainAppSettings> settings, IServiceProvider serviceProvider)
        {
            databaseProvider = db;
            _mongoClient = databaseProvider.GetDatabaseConnection();
            var dataBaseName = MongoUrl.Create(settings.Value.connectionString).DatabaseName;
            _database = _mongoClient.GetDatabase(dataBaseName);
            appSettings = settings.Value;
            webHelper = new WebHelper();
            _encryptionDecryption = serviceProvider.GetService<IEncryptionDecryption>();
            _clusteringAPI = serviceProvider.GetService<IClusteringAPIService>();
            dataSetsService = serviceProvider.GetService<IDataSetsService>();
            _genericSelfservice = serviceProvider.GetService<IGenericSelfservice>();
            auditTrailLog = new CallBackErrorLog();
            configSetting = settings;
            _customDataService = serviceProvider.GetService<ICustomDataService>();
            _iPhoenixTokenService = serviceProvider.GetService<IPhoenixTokenService>();
        }




        public List<Service> GetAllAICoreServices()
        {
            List<Service> serviceList = new List<Service>();
            var serviceCollection = _database.GetCollection<BsonDocument>("Services");
            var filter = Builders<BsonDocument>.Filter.Empty;
            var projection = Builders<BsonDocument>.Projection.Exclude("_id");
            var result = serviceCollection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                serviceList = JsonConvert.DeserializeObject<List<Service>>(result.ToJson());
            }

            return serviceList;
        }
        public List<AICoreModels> GetAllAICoreModels()
        {
            List<AICoreModels> serviceList = new List<AICoreModels>();
            var serviceCollection = _database.GetCollection<BsonDocument>("AICoreModels");
            var filter = Builders<BsonDocument>.Filter.Empty;
            var projection = Builders<BsonDocument>.Projection.Exclude("_id");
            var result = serviceCollection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                if (appSettings.isForAllData)
                {
                    for (int i = 0; i < result.Count; i++)
                    {
                        try
                        {
                            if (result[i].Contains("CreatedBy") && !string.IsNullOrEmpty(Convert.ToString(result[i]["CreatedBy"])))
                                result[i]["CreatedBy"] = _encryptionDecryption.Decrypt(Convert.ToString(result[i]["CreatedBy"]));
                        }
                        catch (Exception ex)
                        {
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(GetAllAICoreModels), ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty);
                        }
                        try
                        {
                            if (result[i].Contains("ModifiedBy") && !string.IsNullOrEmpty(Convert.ToString(result[i]["ModifiedBy"])))
                                result[i]["ModifiedBy"] = _encryptionDecryption.Decrypt(Convert.ToString(result[i]["ModifiedBy"]));
                        }
                        catch (Exception ex)
                        {
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(GetAllAICoreModels), ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty);
                        }
                    }
                }
                serviceList = JsonConvert.DeserializeObject<List<AICoreModels>>(result.ToJson());
            }

            return serviceList;
        }
        public ModelsList GetAICoreModels(string clientid, string dcid, string serviceid, string userid)
        {
            string encrypteduser = userid;
            if (!string.IsNullOrEmpty(encrypteduser))
            {
                encrypteduser = _encryptionDecryption.Encrypt(Convert.ToString(encrypteduser));
            }
            List<AICoreModels> serviceList = new List<AICoreModels>();
            ModelsList modelsLists = new ModelsList();
            var serviceCollection = _database.GetCollection<BsonDocument>("AICoreModels");
            var filter = Builders<BsonDocument>.Filter.Eq("ClientId", clientid) & Builders<BsonDocument>.Filter.Eq("DeliveryConstructId", dcid) & Builders<BsonDocument>.Filter.Eq("ServiceId", serviceid) & (Builders<BsonDocument>.Filter.Eq("CreatedBy", userid) | Builders<BsonDocument>.Filter.Eq("CreatedBy", encrypteduser));
            var projection = Builders<BsonDocument>.Projection.Exclude("_id");
            var result = serviceCollection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                if (appSettings.isForAllData)
                {
                    for (int i = 0; i < result.Count; i++)
                    {
                        try
                        {
                            if (result[i].Contains("CreatedBy") && !string.IsNullOrEmpty(Convert.ToString(result[i]["CreatedBy"])))
                                result[i]["CreatedBy"] = _encryptionDecryption.Decrypt(Convert.ToString(result[i]["CreatedBy"]));
                        }
                        catch (Exception ex)
                        {
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(GetAICoreModels), ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty);
                        }
                        try
                        {
                            if (result[i].Contains("ModifiedBy") && !string.IsNullOrEmpty(Convert.ToString(result[i]["ModifiedBy"])))
                                result[i]["ModifiedBy"] = _encryptionDecryption.Decrypt(Convert.ToString(result[i]["ModifiedBy"]));
                        }
                        catch (Exception ex)
                        {
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(GetAICoreModels), ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty);
                        }
                    }
                }
                serviceList = JsonConvert.DeserializeObject<List<AICoreModels>>(result.ToJson());
                modelsLists.ModelStatus = serviceList;
            }

            var corrIdList = serviceList.Select(x => x.CorrelationId).ToArray();


            var ingestCollection = _database.GetCollection<BsonDocument>("AIServiceRequestStatus");
            var ingestFilter = Builders<BsonDocument>.Filter.AnyIn("CorrelationId", corrIdList) & (Builders<BsonDocument>.Filter.Eq("PageInfo", "TrainModel") | Builders<BsonDocument>.Filter.Eq("PageInfo", "Ingest_Train"));//Builders<BsonDocument>.Filter.Eq("Status","C") &
            var ingestProjection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id).Include("CorrelationId").Include("SelectedColumnNames").Include("DataSource").Include(CONSTANTS.ScoreUniqueName);
            var ingestResult = ingestCollection.Find(ingestFilter).Project<BsonDocument>(ingestProjection).ToList();
            if (ingestResult.Count > 0)
            {
                var modelDetailsList = JsonConvert.DeserializeObject<List<ModelColDetails>>(ingestResult.ToJson());
                var modelDetails = modelDetailsList.GroupBy(m => m.CorrelationId)
                                             .Select(g => g.First())
                                             .ToList();
                modelsLists.ModelColumns = modelDetails;
            }

            return modelsLists;
        }


        public string AddAICoreService(Service service)
        {
            service.ServiceId = Guid.NewGuid().ToString();
            var collection = _database.GetCollection<BsonDocument>("Services");
            var filter = Builders<BsonDocument>.Filter.Eq("ServiceId", service.ServiceId);

            var result = collection.Find(filter).ToList();
            var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(service);
            var insertDocument = BsonSerializer.Deserialize<BsonDocument>(jsonData);
            if (result.Count > 0)
            {
                collection.DeleteOne(filter);
            }
            Thread.Sleep(1000);
            collection.InsertOneAsync(insertDocument);
            return "Success";

        }

        public bool CreateAICoreModel(AICoreModels aiCoreModels, string pageInfo)
        {
            //if (appSettings.isForAllData)
            //{
            //    if (!string.IsNullOrEmpty(Convert.ToString(aiCoreModels.CreatedBy)))
            //        aiCoreModels.CreatedBy = _encryptionDecryption.Encrypt(Convert.ToString(aiCoreModels.CreatedBy));
            //}
            //AICoreModels aiCoreModels = new AICoreModels();
            //aiCoreModels.ClientId = clientid;
            //aiCoreModels.DeliveryConstructId = deliveryconstructid;
            //aiCoreModels.ServiceId = serviceid;
            //aiCoreModels.CorrelationId = corrid;
            //aiCoreModels.UniId = uniId;
            //aiCoreModels.ModelName = modelName;
            //aiCoreModels.ModelStatus = modelStatus;
            //aiCoreModels.DataSource = dataSource;
            //aiCoreModels.StatusMessage = statusMessage;
            //aiCoreModels.PythonModelName = pythonModelName;
            //aiCoreModels.UsecaseId = usecaseId;
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(CreateAICoreModel), CONSTANTS.START,
                string.IsNullOrEmpty(aiCoreModels.CorrelationId) ? default(Guid) : new Guid(aiCoreModels.CorrelationId), aiCoreModels.ApplicationId, string.Empty, aiCoreModels.ClientId, aiCoreModels.DeliveryConstructId);
            aiCoreModels.PredictionURL = appSettings.AICorePredictionURL + "?=" + aiCoreModels.CorrelationId;
            aiCoreModels.CreatedOn = DateTime.UtcNow.ToString(CONSTANTS.DateHoursFormat);//DateTime.Now.ToString();
            aiCoreModels.ModifiedOn = DateTime.UtcNow.ToString(CONSTANTS.DateHoursFormat);//DateTime.Now.ToString();
                                                                                          //   aiCoreModels.CreatedBy = userid;
            aiCoreModels.ModifiedBy = aiCoreModels.CreatedBy;
            var collection = _database.GetCollection<BsonDocument>("AICoreModels");
            var filter = Builders<BsonDocument>.Filter.Eq("CorrelationId", aiCoreModels.CorrelationId) & Builders<BsonDocument>.Filter.Eq("ClientId", aiCoreModels.ClientId) & Builders<BsonDocument>.Filter.Eq("DeliveryConstructId", aiCoreModels.DeliveryConstructId);
            var projection = Builders<BsonDocument>.Projection.Exclude("_id");
            var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();

            if (result.Count > 0)
            {
                AICoreModels temp = JsonConvert.DeserializeObject<AICoreModels>(result[0].ToJson());
                aiCoreModels.CreatedOn = temp.CreatedOn;

                if (pageInfo == "Retrain")
                {
                    var update = Builders<BsonDocument>.Update.Set("ModifiedBy", aiCoreModels.CreatedBy).Set("ModifiedOn", aiCoreModels.ModifiedOn)
                                       .Set("ModelStatus", aiCoreModels.ModelStatus).Set("StatusMessage", aiCoreModels.StatusMessage).Set("DataSource", aiCoreModels.DataSource);
                    collection.UpdateOne(filter, update);
                }
                else
                {
                    var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(aiCoreModels);
                    var insertDocument = BsonSerializer.Deserialize<BsonDocument>(jsonData);
                    collection.DeleteOne(filter);
                    collection.InsertOneAsync(insertDocument);
                }
                return true;
            }
            else
            {
                var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(aiCoreModels);
                var insertDocument = BsonSerializer.Deserialize<BsonDocument>(jsonData);
                collection.InsertOneAsync(insertDocument);
                return true;
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(CreateAICoreModel), CONSTANTS.START,
                string.IsNullOrEmpty(aiCoreModels.CorrelationId) ? default(Guid) : new Guid(aiCoreModels.CorrelationId), aiCoreModels.ApplicationId, string.Empty, aiCoreModels.ClientId, aiCoreModels.DeliveryConstructId);
        }

        public bool CreateAICoreModel(string clientid, string deliveryconstructid, string serviceid, string corrid, string uniId, string modelName, string pythonModelName, string modelStatus, string statusMessage, string userid, string dataSource, string pageInfo, string datasetUId, int maxDataPull)
        {
            if (appSettings.isForAllData)
            {
                if (!string.IsNullOrEmpty(userid))
                {
                    userid = _encryptionDecryption.Encrypt(Convert.ToString(userid));
                }
            }
            AICoreModels aiCoreModels = new AICoreModels();
            aiCoreModels.ClientId = clientid;
            aiCoreModels.DeliveryConstructId = deliveryconstructid;
            aiCoreModels.ServiceId = serviceid;
            aiCoreModels.CorrelationId = corrid;
            aiCoreModels.UniId = uniId;
            aiCoreModels.ModelName = modelName;
            aiCoreModels.ModelStatus = modelStatus;
            aiCoreModels.DataSource = dataSource;
            aiCoreModels.StatusMessage = statusMessage;
            aiCoreModels.PythonModelName = pythonModelName;
            aiCoreModels.PredictionURL = appSettings.AICorePredictionURL + "?=" + aiCoreModels.CorrelationId;
            aiCoreModels.CreatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");//DateTime.Now.ToString();
            aiCoreModels.ModifiedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");//DateTime.Now.ToString();
            aiCoreModels.CreatedBy = userid;
            aiCoreModels.ModifiedBy = userid;
            aiCoreModels.DataSetUId = datasetUId;
            aiCoreModels.MaxDataPull = maxDataPull;
            var collection = _database.GetCollection<BsonDocument>("AICoreModels");
            var filter = Builders<BsonDocument>.Filter.Eq("CorrelationId", corrid) & Builders<BsonDocument>.Filter.Eq("ClientId", clientid) & Builders<BsonDocument>.Filter.Eq("DeliveryConstructId", deliveryconstructid);
            var projection = Builders<BsonDocument>.Projection.Exclude("_id");
            var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();

            if (result.Count > 0)
            {
                AICoreModels temp = JsonConvert.DeserializeObject<AICoreModels>(result[0].ToJson());
                aiCoreModels.CreatedOn = temp.CreatedOn;
                aiCoreModels.IsCarryOutRetraining = temp.IsCarryOutRetraining;
                aiCoreModels.IsOnline = temp.IsOnline;
                aiCoreModels.IsOffline = temp.IsOffline;
                aiCoreModels.Training = temp.Training;
                aiCoreModels.Prediction = temp.Prediction;
                aiCoreModels.Retraining = temp.Retraining;
                aiCoreModels.RetrainingFrequencyInDays = temp.RetrainingFrequencyInDays;
                aiCoreModels.TrainingFrequencyInDays = temp.TrainingFrequencyInDays;
                aiCoreModels.PredictionFrequencyInDays = temp.PredictionFrequencyInDays;

                if (pageInfo == "Retrain")
                {
                    var update = Builders<BsonDocument>.Update.Set("ModifiedBy", userid).Set("ModifiedOn", aiCoreModels.ModifiedOn)
                   .Set("ModelStatus", aiCoreModels.ModelStatus).Set("StatusMessage", aiCoreModels.StatusMessage).Set("DataSource", dataSource);
                    collection.UpdateOne(filter, update);
                }
                else
                {
                    var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(aiCoreModels);
                    var insertDocument = BsonSerializer.Deserialize<BsonDocument>(jsonData);
                    collection.DeleteOne(filter);
                    collection.InsertOneAsync(insertDocument);
                }
                return true;
            }
            else
            {
                var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(aiCoreModels);
                var insertDocument = BsonSerializer.Deserialize<BsonDocument>(jsonData);
                collection.InsertOneAsync(insertDocument);
                return true;
            }
        }
        public void AssignFrequency(AICoreModels aICoreModels, AIModelFrequency frequency, dynamic data, string feature)
        {
            foreach (var z in (JContainer)data)
            {
                string frequencyName = ((Newtonsoft.Json.Linq.JProperty)z).Name;
                frequency.RetryCount = data[CONSTANTS.RetryCount] != null ? Convert.ToInt32(data[CONSTANTS.RetryCount]) : 0;
                if (Convert.ToInt32(((Newtonsoft.Json.Linq.JProperty)z).Value) != 0)
                {
                    switch (frequencyName)
                    {
                        case CONSTANTS.Hourly:
                            frequency.Hourly = data[frequencyName];
                            aICoreModels.TrainingFrequencyInDays = feature.Equals(CONSTANTS.Training) ? frequency.Hourly * 60 : aICoreModels.TrainingFrequencyInDays;
                            aICoreModels.PredictionFrequencyInDays = feature.Equals(CONSTANTS.Prediction) ? frequency.Hourly * 60 : aICoreModels.PredictionFrequencyInDays;
                            break;

                        case CONSTANTS.Daily:
                            frequency.Daily = data[frequencyName];
                            aICoreModels.RetrainingFrequencyInDays = feature.Equals(CONSTANTS.Retraining) ? frequency.Daily : aICoreModels.RetrainingFrequencyInDays;
                            aICoreModels.TrainingFrequencyInDays = feature.Equals(CONSTANTS.Training) ? frequency.Daily : aICoreModels.TrainingFrequencyInDays;
                            aICoreModels.PredictionFrequencyInDays = feature.Equals(CONSTANTS.Prediction) ? frequency.Daily : aICoreModels.PredictionFrequencyInDays;
                            break;

                        case CONSTANTS.Weekly:
                            frequency.Weekly = data[frequencyName];
                            aICoreModels.RetrainingFrequencyInDays = feature.Equals(CONSTANTS.Retraining) ? (frequency.Weekly * 7) : aICoreModels.RetrainingFrequencyInDays;
                            aICoreModels.TrainingFrequencyInDays = feature.Equals(CONSTANTS.Training) ? (frequency.Weekly * 7) : aICoreModels.TrainingFrequencyInDays;
                            aICoreModels.PredictionFrequencyInDays = feature.Equals(CONSTANTS.Prediction) ? (frequency.Weekly * 7) : aICoreModels.PredictionFrequencyInDays;
                            break;

                        case CONSTANTS.Monthly:
                            frequency.Monthly = data[frequencyName];
                            aICoreModels.RetrainingFrequencyInDays = feature.Equals(CONSTANTS.Retraining) ? (frequency.Monthly * 30) : aICoreModels.RetrainingFrequencyInDays;
                            aICoreModels.TrainingFrequencyInDays = feature.Equals(CONSTANTS.Training) ? (frequency.Monthly * 30) : aICoreModels.TrainingFrequencyInDays;
                            aICoreModels.PredictionFrequencyInDays = feature.Equals(CONSTANTS.Prediction) ? (frequency.Monthly * 30) : aICoreModels.PredictionFrequencyInDays;
                            break;

                        case CONSTANTS.Fortnightly:
                            frequency.Fortnightly = data[frequencyName];
                            aICoreModels.RetrainingFrequencyInDays = feature.Equals(CONSTANTS.Retraining) ? (frequency.Fortnightly * 14) : aICoreModels.RetrainingFrequencyInDays;
                            aICoreModels.TrainingFrequencyInDays = feature.Equals(CONSTANTS.Training) ? (frequency.Fortnightly * 14) : aICoreModels.TrainingFrequencyInDays;
                            aICoreModels.PredictionFrequencyInDays = feature.Equals(CONSTANTS.Prediction) ? (frequency.Fortnightly * 14) : aICoreModels.PredictionFrequencyInDays;
                            break;
                    }
                }
            }
        }

        public void AssignFrequencyClustering(ClusteringAPIModel clusteringAPIModel, ClustModelFrequency frequency, dynamic data, string feature)
        {
            foreach (var z in (JContainer)data)
            {
                string frequencyName = ((Newtonsoft.Json.Linq.JProperty)z).Name;
                //string frequencyName = ((JProperty)((JContainer)data).First).Name;
                frequency.RetryCount = data[CONSTANTS.RetryCount] != null ? Convert.ToInt32(data[CONSTANTS.RetryCount]) : 0;
                if (Convert.ToInt32(((Newtonsoft.Json.Linq.JProperty)z).Value) != 0)
                {
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
            }
        }
        public void AssignFrequencyUsecase(USECASE.UsecaseDetails usecaseDetails, UsecaseFrequency frequency, dynamic data, string feature)
        {
            foreach (var z in (JContainer)data)
            {
                string frequencyName = ((Newtonsoft.Json.Linq.JProperty)z).Name;
                if (Convert.ToInt32(((Newtonsoft.Json.Linq.JProperty)z).Value) != 0)
                {
                    switch (frequencyName)
                    {
                        case CONSTANTS.Hourly:
                            usecaseDetails.TrainingFrequencyInDays = feature.Equals(CONSTANTS.Training) ? frequency.Hourly * 60 : usecaseDetails.TrainingFrequencyInDays;
                            usecaseDetails.PredictionFrequencyInDays = feature.Equals(CONSTANTS.Prediction) ? frequency.Hourly * 60 : usecaseDetails.PredictionFrequencyInDays;
                            break;

                        case CONSTANTS.Daily:
                            usecaseDetails.RetrainingFrequencyInDays = feature.Equals(CONSTANTS.Retraining) ? frequency.Daily : usecaseDetails.RetrainingFrequencyInDays;
                            usecaseDetails.TrainingFrequencyInDays = feature.Equals(CONSTANTS.Training) ? frequency.Daily : usecaseDetails.TrainingFrequencyInDays;
                            usecaseDetails.PredictionFrequencyInDays = feature.Equals(CONSTANTS.Prediction) ? frequency.Daily : usecaseDetails.PredictionFrequencyInDays;
                            break;

                        case CONSTANTS.Weekly:
                            usecaseDetails.RetrainingFrequencyInDays = feature.Equals(CONSTANTS.Retraining) ? (frequency.Weekly * 7) : usecaseDetails.RetrainingFrequencyInDays;
                            usecaseDetails.TrainingFrequencyInDays = feature.Equals(CONSTANTS.Training) ? (frequency.Weekly * 7) : usecaseDetails.TrainingFrequencyInDays;
                            usecaseDetails.PredictionFrequencyInDays = feature.Equals(CONSTANTS.Prediction) ? (frequency.Weekly * 7) : usecaseDetails.PredictionFrequencyInDays;
                            break;

                        case CONSTANTS.Monthly:
                            usecaseDetails.RetrainingFrequencyInDays = feature.Equals(CONSTANTS.Retraining) ? (frequency.Monthly * 30) : usecaseDetails.RetrainingFrequencyInDays;
                            usecaseDetails.TrainingFrequencyInDays = feature.Equals(CONSTANTS.Training) ? (frequency.Monthly * 30) : usecaseDetails.TrainingFrequencyInDays;
                            usecaseDetails.PredictionFrequencyInDays = feature.Equals(CONSTANTS.Prediction) ? (frequency.Monthly * 30) : usecaseDetails.PredictionFrequencyInDays;
                            break;

                        case CONSTANTS.Fortnightly:
                            usecaseDetails.RetrainingFrequencyInDays = feature.Equals(CONSTANTS.Retraining) ? (frequency.Fortnightly * 14) : usecaseDetails.RetrainingFrequencyInDays;
                            usecaseDetails.TrainingFrequencyInDays = feature.Equals(CONSTANTS.Training) ? (frequency.Fortnightly * 14) : usecaseDetails.TrainingFrequencyInDays;
                            usecaseDetails.PredictionFrequencyInDays = feature.Equals(CONSTANTS.Prediction) ? (frequency.Fortnightly * 14) : usecaseDetails.PredictionFrequencyInDays;
                            break;
                    }
                }
            }
        }
        public bool InsertAICoreModel(AIServiceRequestStatus aIServiceRequestStatus, AICoreModels aICorefreqModels, string modelStatus, string statusMessage)
        {
            //if (appSettings.isForAllData)
            //{
            //    if (!string.IsNullOrEmpty(aIServiceRequestStatus.CreatedByUser))
            //    {
            //        aIServiceRequestStatus.CreatedByUser = _encryptionDecryption.Encrypt(Convert.ToString(aIServiceRequestStatus.CreatedByUser));
            //    }
            //}
            AIModelFrequency aIModelFrequency = new AIModelFrequency();
            AICoreModels aiCoreModels = new AICoreModels();
            aiCoreModels.ClientId = aIServiceRequestStatus.ClientId;
            aiCoreModels.DeliveryConstructId = aIServiceRequestStatus.DeliveryconstructId;
            aiCoreModels.ServiceId = aIServiceRequestStatus.ServiceId;
            aiCoreModels.CorrelationId = aIServiceRequestStatus.CorrelationId;
            aiCoreModels.UniId = aIServiceRequestStatus.UniId;
            aiCoreModels.ModelName = aIServiceRequestStatus.ModelName;
            aiCoreModels.ModelStatus = modelStatus;
            aiCoreModels.DataSource = aIServiceRequestStatus.DataSource;
            aiCoreModels.StatusMessage = statusMessage;
            aiCoreModels.PythonModelName = null;
            aiCoreModels.PredictionURL = appSettings.AICorePredictionURL + "?=" + aiCoreModels.CorrelationId;
            aiCoreModels.CreatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");//DateTime.Now.ToString();
            aiCoreModels.ModifiedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");//DateTime.Now.ToString();
            aiCoreModels.CreatedBy = aIServiceRequestStatus.CreatedByUser;
            aiCoreModels.ModifiedBy = aIServiceRequestStatus.ModifiedByUser;
            aiCoreModels.DataSetUId = aIServiceRequestStatus.DataSetUId;
            aiCoreModels.MaxDataPull = aIServiceRequestStatus.MaxDataPull;
            aiCoreModels.IsCarryOutRetraining = aICorefreqModels.IsCarryOutRetraining;
            aiCoreModels.IsOnline = aICorefreqModels.IsOnline;
            aiCoreModels.IsOffline = aICorefreqModels.IsOffline;
            aiCoreModels.Retraining = aICorefreqModels.Retraining;
            aiCoreModels.Training = aICorefreqModels.Training;
            aiCoreModels.Prediction = aICorefreqModels.Prediction;
            aiCoreModels.RetrainingFrequencyInDays = aICorefreqModels.RetrainingFrequencyInDays;
            aiCoreModels.TrainingFrequencyInDays = aICorefreqModels.TrainingFrequencyInDays;
            aiCoreModels.PredictionFrequencyInDays = aICorefreqModels.PredictionFrequencyInDays;


            var collection = _database.GetCollection<BsonDocument>("AICoreModels");
            var filter = Builders<BsonDocument>.Filter.Eq("CorrelationId", aIServiceRequestStatus.CorrelationId) & Builders<BsonDocument>.Filter.Eq("ClientId", aIServiceRequestStatus.ClientId) & Builders<BsonDocument>.Filter.Eq("DeliveryConstructId", aIServiceRequestStatus.DeliveryconstructId);
            var projection = Builders<BsonDocument>.Projection.Exclude("_id");
            var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();

            if (result.Count > 0)
            {
                AICoreModels temp = JsonConvert.DeserializeObject<AICoreModels>(result[0].ToJson());
                aiCoreModels.CreatedOn = temp.CreatedOn;
                if (aIServiceRequestStatus.PageInfo == "Retrain")
                {
                    var update = Builders<BsonDocument>.Update.Set("ModifiedBy", aIServiceRequestStatus.ModifiedByUser).Set("ModifiedOn", aiCoreModels.ModifiedOn)
                   .Set("ModelStatus", aiCoreModels.ModelStatus).Set("StatusMessage", aiCoreModels.StatusMessage).Set("DataSource", aIServiceRequestStatus.DataSource);
                    collection.UpdateOne(filter, update);
                }
                else
                {
                    var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(aiCoreModels);
                    var insertDocument = BsonSerializer.Deserialize<BsonDocument>(jsonData);
                    collection.DeleteOne(filter);
                    collection.InsertOneAsync(insertDocument);
                }
                return true;
            }
            else
            {
                var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(aiCoreModels);
                var insertDocument = BsonSerializer.Deserialize<BsonDocument>(jsonData);
                collection.InsertOneAsync(insertDocument);
                return true;
            }
        }

        public bool CreateAICoreModel(string clientid, string deliveryconstructid, string serviceid, string corrid, string modelName, string pythonModelName, string modelStatus, string statusMessage, string userid, string usecaseId, string applicationId, string uniId, string trainedBy, string pageInfo, string dataSource)
        {
            AICoreModels aiCoreModels = new AICoreModels();
            aiCoreModels.ClientId = clientid;
            aiCoreModels.DeliveryConstructId = deliveryconstructid;
            aiCoreModels.ServiceId = serviceid;
            aiCoreModels.UniId = uniId;
            aiCoreModels.LastTrainedBy = trainedBy;
            aiCoreModels.CorrelationId = corrid;
            aiCoreModels.ModelName = modelName;
            aiCoreModels.UsecaseId = usecaseId;
            aiCoreModels.ApplicationId = applicationId;
            aiCoreModels.ModelStatus = modelStatus;
            aiCoreModels.DataSource = dataSource;
            aiCoreModels.StatusMessage = statusMessage;
            aiCoreModels.PythonModelName = pythonModelName;
            aiCoreModels.PredictionURL = appSettings.AICorePredictionURL + "?=" + aiCoreModels.CorrelationId;
            aiCoreModels.CreatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");//DateTime.Now.ToString();
            aiCoreModels.ModifiedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");//DateTime.Now.ToString();
            aiCoreModels.CreatedBy = userid;
            aiCoreModels.ModifiedBy = userid;
            var collection = _database.GetCollection<BsonDocument>("AICoreModels");
            var filter = Builders<BsonDocument>.Filter.Eq("CorrelationId", corrid) & Builders<BsonDocument>.Filter.Eq("ClientId", clientid) & Builders<BsonDocument>.Filter.Eq("DeliveryConstructId", deliveryconstructid);
            var projection = Builders<BsonDocument>.Projection.Exclude("_id");
            var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();

            if (result.Count > 0)
            {
                AICoreModels temp = JsonConvert.DeserializeObject<AICoreModels>(result[0].ToJson());
                aiCoreModels.CreatedOn = temp.CreatedOn;
                if (pageInfo == "Retrain")
                {
                    var update = Builders<BsonDocument>.Update.Set("ModifiedBy", userid).Set("ModifiedOn", aiCoreModels.ModifiedOn)
              .Set("ModelStatus", aiCoreModels.ModelStatus).Set("StatusMessage", aiCoreModels.StatusMessage);
                    collection.UpdateOne(filter, update);
                }
                else
                {
                    var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(aiCoreModels);
                    var insertDocument = BsonSerializer.Deserialize<BsonDocument>(jsonData);
                    collection.DeleteOne(filter);
                    collection.InsertOneAsync(insertDocument);
                }
                return true;
            }
            else
            {
                var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(aiCoreModels);
                var insertDocument = BsonSerializer.Deserialize<BsonDocument>(jsonData);
                collection.InsertOneAsync(insertDocument);
                return true;
            }
        }
        public AICoreModels GetAICoreModelPath(string correlationid)
        {
            AICoreModels serviceList = null;
            var serviceCollection = _database.GetCollection<BsonDocument>("AICoreModels");
            var filter = Builders<BsonDocument>.Filter.Eq("CorrelationId", correlationid);
            var projection = Builders<BsonDocument>.Projection.Exclude("_id");
            var result = serviceCollection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                serviceList = new AICoreModels();
                if (appSettings.isForAllData)
                {
                    for (int i = 0; i < result.Count; i++)
                    {
                        try
                        {
                            if (result[i].Contains("CreatedBy") && !string.IsNullOrEmpty(Convert.ToString(result[i]["CreatedBy"])))
                                result[i]["CreatedBy"] = _encryptionDecryption.Decrypt(Convert.ToString(result[i]["CreatedBy"]));
                        }
                        catch (Exception ex)
                        {
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(GetAICoreModelPath), ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty);
                        }
                        try
                        {
                            if (result[i].Contains("ModifiedBy") && !string.IsNullOrEmpty(Convert.ToString(result[i]["ModifiedBy"])))
                                result[i]["ModifiedBy"] = _encryptionDecryption.Decrypt(Convert.ToString(result[i]["ModifiedBy"]));
                        }
                        catch (Exception ex)
                        {
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(GetAICoreModelPath), ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty);
                        }

                    }
                }
                serviceList = JsonConvert.DeserializeObject<AICoreModels>(result[0].ToJson());
            }

            return serviceList;
        }

        public AICoreModels GetAICoreModelByUsecaseId(string clientId, string deliveryConstructId, string applicationId, string serviceId, string usecaseId)
        {
            AICoreModels serviceList = new AICoreModels();
            var serviceCollection = _database.GetCollection<BsonDocument>("AICoreModels");
            var filter = Builders<BsonDocument>.Filter.Eq("ClientId", clientId)
                         & Builders<BsonDocument>.Filter.Eq("DeliveryConstructId", deliveryConstructId)
                          & Builders<BsonDocument>.Filter.Eq("ApplicationId", applicationId)
                           & Builders<BsonDocument>.Filter.Eq("ServiceId", serviceId)
                            & Builders<BsonDocument>.Filter.Eq("UsecaseId", usecaseId)
                            & Builders<BsonDocument>.Filter.Eq("ModelStatus", "Completed");
            var projection = Builders<BsonDocument>.Projection.Exclude("_id");
            var result = serviceCollection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                serviceList = JsonConvert.DeserializeObject<AICoreModels>(result[0].ToJson());
            }

            return serviceList;
        }

        public bool CheckAICoreModelByUsecaseId(string clientId, string deliveryConstructId, string applicationId, string serviceId, string usecaseId)
        {
            AICoreModels serviceList = new AICoreModels();
            var serviceCollection = _database.GetCollection<BsonDocument>("AICoreModels");
            var filter = Builders<BsonDocument>.Filter.Eq("ClientId", clientId)
                         & Builders<BsonDocument>.Filter.Eq("DeliveryConstructId", deliveryConstructId)
                          & Builders<BsonDocument>.Filter.Eq("ApplicationId", applicationId)
                           & Builders<BsonDocument>.Filter.Eq("ServiceId", serviceId)
                            & Builders<BsonDocument>.Filter.Eq("UsecaseId", usecaseId)
                            & (Builders<BsonDocument>.Filter.Eq("ModelStatus", "Completed") | Builders<BsonDocument>.Filter.Eq("ModelStatus", "InProgress"));
            var projection = Builders<BsonDocument>.Projection.Exclude("_id");
            var result = serviceCollection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                return true;
                //serviceList = JsonConvert.DeserializeObject<AICoreModels>(result[0].ToJson());
            }
            else
            {
                return false;
            }
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
                service = JsonConvert.DeserializeObject<Service>(result[0].ToJson());
            }

            return service;
        }


        /// <summary>
        /// SimilarityAnalytics & Nextword & DeveloperPrediction
        /// </summary>
        /// <param name="token"></param>
        /// <param name="baseUrl"></param>
        /// <param name="apiPath"></param>
        /// <param name="isReturnArray"></param>
        /// <returns></returns>
        public MethodReturn<object> RouteGETRequest(string token, Uri baseUrl, string apiPath, bool isReturnArray)
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
                    returnValue.Message = message.ReasonPhrase + "_" + message.Content.ReadAsStringAsync().Result;
                }
            }
            catch (Exception ex)
            {
                returnValue.IsSuccess = false;
                returnValue.Message = ex.Message;

            }
            return returnValue;
        }
        public MethodReturn<object> RoutePOSTRequest(string token, Uri baseUrl, string apiPath, IFormFileCollection fileCollection,
           JObject requestPayload, string[] fileKeys, bool isReturnArray, string correlationid)
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

            string tempPath = null;
            try
            {
                var folderPath = Guid.NewGuid().ToString();
                List<string> lstFilePath = new List<string>();
                if (fileCollection != null)
                {
                    foreach (var file in fileCollection)
                    {
                        var fileName = file.FileName;
                        tempPath = appSettings.AICoreFilespath.ToString();
                        var filePath = tempPath + correlationid + "/" + folderPath + "/" + fileName;
                        if (!Directory.Exists(tempPath + correlationid + "/" + folderPath))
                            Directory.CreateDirectory(tempPath + correlationid + "/" + folderPath);
                        //using (var stream = new FileStream(filePath, FileMode.Create))
                        //{
                        //    file.CopyTo(stream);
                        //}
                        _encryptionDecryption.EncryptFile(file, filePath);
                        if (appSettings.IsAESKeyVault && appSettings.Environment == CONSTANTS.PADEnvironment && appSettings.EncryptUploadedFiles)
                        {
                            filePath = Path.Combine(filePath + ".enc");
                        }
                        lstFilePath.Add(filePath);
                    }
                }
                StringContent content = new StringContent(requestPayload.ToString(), Encoding.UTF8, "application/json");
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(RoutePOSTRequest), requestPayload.ToString(), string.IsNullOrEmpty(correlationid) ? default(Guid) : new Guid(correlationid), string.Empty, string.Empty, string.Empty, string.Empty);

                Task.Run(() =>
                {
                    try
                    {
                        HttpResponseMessage message = webHelper.InvokePOSTRequestWithFiles(token, baseUrl, apiPath, lstFilePath, fileKeys, content);
                        if (message != null && message.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            TrainingResponse ReturnValue = JsonConvert.DeserializeObject<TrainingResponse>(message.Content.ReadAsStringAsync().Result);
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(RoutePOSTRequest), message.Content.ReadAsStringAsync().Result, string.IsNullOrEmpty(correlationid) ? default(Guid) : new Guid(correlationid), string.Empty, string.Empty, string.Empty, string.Empty);
                            AICoreModels model = GetAICoreModelPath(correlationid);
                            if (ReturnValue.is_success == "true")
                            {
                                string pyModelname = model.PythonModelName;
                                string modelStatus = model.ModelStatus;
                                CreateAICoreModel(model.ClientId, model.DeliveryConstructId, model.ServiceId, model.CorrelationId, model.UniId, model.ModelName, pyModelname, modelStatus, ReturnValue.message, model.CreatedBy, model.DataSource, null, model.DataSetUId, model.MaxDataPull);
                            }
                            else
                            {
                                if (model.PythonModelName != null && model.PythonModelName != "")
                                    CreateAICoreModel(model.ClientId, model.DeliveryConstructId, model.ServiceId, model.CorrelationId, model.UniId, model.ModelName, model.PythonModelName, "ReTrain Failed", "Python error in retraining-" + ReturnValue.message + ";Evaluation will be done with previously trained data", model.CreatedBy, model.DataSource, null, model.DataSetUId, model.MaxDataPull);
                                else
                                    CreateAICoreModel(model.ClientId, model.DeliveryConstructId, model.ServiceId, model.CorrelationId, model.UniId, model.ModelName, model.PythonModelName, "Error", "Python error in training-" + ReturnValue.message, model.CreatedBy, model.DataSource, null, model.DataSetUId, model.MaxDataPull);
                            }


                        }
                        else
                        {
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(RoutePOSTRequest), message.StatusCode.ToString() + message.ReasonPhrase + "_" + message.Content.ReadAsStringAsync().Result, string.IsNullOrEmpty(correlationid) ? default(Guid) : new Guid(correlationid), string.Empty, string.Empty, string.Empty, string.Empty);

                            AICoreModels model = GetAICoreModelPath(correlationid);
                            if (model.PythonModelName != null && model.PythonModelName != "")
                                CreateAICoreModel(model.ClientId, model.DeliveryConstructId, model.ServiceId, model.CorrelationId, model.UniId, model.ModelName, model.PythonModelName, "ReTrain Failed", "Python error in retraining-" + message.ReasonPhrase + "_" + message.Content.ReadAsStringAsync().Result + ";Evaluation will be done with previously trained data", model.CreatedBy, model.DataSource, null, model.DataSetUId, model.MaxDataPull);
                            else
                                CreateAICoreModel(model.ClientId, model.DeliveryConstructId, model.ServiceId, model.CorrelationId, model.UniId, model.ModelName, model.PythonModelName, "Error", "Python error in training-" + message.ReasonPhrase + "_" + message.Content.ReadAsStringAsync().Result, model.CreatedBy, model.DataSource, null, model.DataSetUId, model.MaxDataPull);

                        }
                    }
                    catch (Exception ex)
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(RoutePOSTRequest), ex.Message, string.Empty, string.Empty, string.Empty, string.Empty);
                        AICoreModels model = GetAICoreModelPath(correlationid);
                        if (model.PythonModelName != null && model.PythonModelName != "")
                            CreateAICoreModel(model.ClientId, model.DeliveryConstructId, model.ServiceId, model.CorrelationId, model.UniId, model.ModelName, model.PythonModelName, "ReTrain Failed", "Python API call error in retraining-" + ex.Message + ";Evaluation will be done with previously trained data", model.CreatedBy, model.DataSource, null, model.DataSetUId, model.MaxDataPull);
                        else
                            CreateAICoreModel(model.ClientId, model.DeliveryConstructId, model.ServiceId, model.CorrelationId, model.UniId, model.ModelName, model.PythonModelName, "Error", "Python API call error in training-" + ex.Message, model.CreatedBy, model.DataSource, null, model.DataSetUId, model.MaxDataPull);
                    }



                });
                returnValue.Message = "Training initiated successfully";
                returnValue.IsSuccess = true;

            }
            catch (Exception ex)
            {
                returnValue.IsSuccess = false;
                returnValue.Message = ex.Message;
            }
            return returnValue;
        }

        public MethodReturn<object> RoutePOSTRequest(string token, Uri baseUrl, string apiPath, JObject requestPayload, bool isReturnArray)
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
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(RoutePOSTRequest), "requestPayload : " + Convert.ToString(requestPayload), string.Empty, string.Empty, string.Empty, string.Empty);
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(RoutePOSTRequest), "token : " + token + " apiPath : " + apiPath + " baseUrl : " + baseUrl, string.Empty, string.Empty, string.Empty, string.Empty);
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
                    returnValue.Message = message.ReasonPhrase + "_" + message.Content.ReadAsStringAsync().Result;
                }
            }
            catch (Exception ex)
            {
                returnValue.IsSuccess = false;
                returnValue.Message = ex.Message;

            }
            return returnValue;
        }

        public void UpdateAICoreModels(string correlationId, string status, string message)
        {
            var aiCoreModel = _database.GetCollection<AICoreModels>(CONSTANTS.AICoreModels);
            var filter = Builders<AICoreModels>.Filter.Where(x => x.CorrelationId == correlationId);
            var update = Builders<AICoreModels>.Update.Set(x => x.ModelStatus, status)
                                                                   .Set(x => x.StatusMessage, message)
                                                                   .Set(x => x.ModifiedOn, DateTime.UtcNow.ToString());
            aiCoreModel.UpdateOne(filter, update);
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
        public string RouteIngestPOSTRequest(string token, Uri baseUrl, string apiPath, JObject requestPayload, string correlationid)
        {
            //MethodReturn<object> returnValue = new MethodReturn<object>();
            string ingestMessage = string.Empty;

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
                //if (message == null)
                //{
                //    returnValue.Message = "Error";
                //    returnValue.IsSuccess = false;
                //}
                if (message.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(RoutePOSTRequest), message.Content.ReadAsStringAsync().Result, string.IsNullOrEmpty(correlationid) ? default(Guid) : new Guid(correlationid), string.Empty, string.Empty, string.Empty, string.Empty);
                    ingestMessage = "Data ingestion is InProgress";
                    // returnValue.IsSuccess = true;
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(ClusteringAPIService), nameof(RoutePOSTRequest), "CorrelationId : " + correlationid + " ClusterAPI : " + message.StatusCode, string.IsNullOrEmpty(correlationid) ? default(Guid) : new Guid(correlationid), string.Empty, string.Empty, string.Empty, string.Empty);
                }
                else
                {
                    ingestMessage = "Python error:" + message.ReasonPhrase + "_" + message.Content.ReadAsStringAsync().Result;
                    //returnValue.IsSuccess = false;
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(ClusteringAPIService), nameof(RoutePOSTRequest), "CorrelationId : " + correlationid + " ClusterAPI : " + "Python error:" + message.ReasonPhrase + " - " + message.StatusCode, new Guid(correlationid), string.Empty, string.Empty, string.Empty, string.Empty);
                }
            }
            catch (Exception ex)
            {
                ingestMessage = "Python API call error" + ex.Message;
                //returnValue.Message = "Python API call error" + ex.Message;
                //returnValue.IsSuccess = false;
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(ClusteringAPIService), nameof(RoutePOSTRequest), "CorrelationId : " + correlationid + " ClusterAPI : Python API call error" + ex.Message, string.Empty, string.Empty, string.Empty, string.Empty);
            }
            //});

            return ingestMessage;
        }

        public bool ValidateAccess(string clientid, string dcid, string serviceid)
        {
            throw new NotImplementedException();
        }

        public string PythonAIServiceToken()
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(PythonAIServiceToken), CONSTANTS.START + appSettings.authProvider.ToUpper(), string.Empty, string.Empty, string.Empty, string.Empty);
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
                   "&client_id=" + appSettings.clientId_VDS +
                   "&client_secret=" + appSettings.clientSecret_VDS +
                   "&scope=" + appSettings.scopeStatus_VDS +
                   "&resource=" + appSettings.resource_ingrain,
                   ParameterType.RequestBody);
                var requestBuilder = new StringBuilder();
                foreach (var param in request.Parameters)
                {
                    requestBuilder.AppendFormat("{0}: {1}\r\n", param.Name, param.Value);
                }
                requestBuilder.ToString();
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(PythonAIServiceToken), "PYTHON TOKEN PARAMS -- " + requestBuilder, string.Empty, string.Empty, string.Empty, string.Empty);
                IRestResponse response = client.Execute(request);
                string json = response.Content;
                // Retrieve and Return the Access Token                
                var tokenObj = JsonConvert.DeserializeObject(json) as dynamic;
                token = Convert.ToString(tokenObj.access_token);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(PythonAIServiceToken), "END -" + appSettings.authProvider.ToUpper(), string.Empty, string.Empty, string.Empty, string.Empty);
            return token;
        }

        public MethodReturn<object> AISeriveIngestData(string ModelName, string userId, string clientUID, string deliveryUID, string ParentFileName, string MappingFlag, string Source, string Category, HttpContext httpContext, string uploadType, bool DBEncryption, string serviceId, string Language, string E2EUID)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(AISeriveIngestData), "START", string.Empty, string.Empty, string.Empty, string.Empty);
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
            string flag = CONSTANTS.Null;
            string pageInfo = CONSTANTS.IngestData;
            string MappingColumns = string.Empty;
            string filePath = string.Empty;
            int counter = 0;
            string ParentFileNamePath = string.Empty, FilePath = string.Empty, postedFileName = string.Empty, DataSourceFilePath = string.Empty, FileName = string.Empty, SaveFileName = string.Empty;
            var fileCollection = httpContext.Request.Form.Files;
            string Entities = string.Empty, Metrices = string.Empty, InstaML = string.Empty, Entity_Names = string.Empty, Metric_Names = string.Empty, Customdata = string.Empty, CustomSourceItems = string.Empty;
            if (!string.IsNullOrEmpty(ModelName) && !string.IsNullOrEmpty(deliveryUID) && !string.IsNullOrEmpty(clientUID))
            {

                IFormCollection collection = httpContext.Request.Form;
                var entityitems = collection[CONSTANTS.pad];
                var metricitems = collection[CONSTANTS.metrics];
                var InstaMl = collection[CONSTANTS.InstaMl];
                var EntitiesNames = collection[CONSTANTS.EntitiesName];
                var MetricsNames = collection[CONSTANTS.MetricNames];
                var Customdetails = collection["Custom"];
                var dataSetUId = collection["DataSetUId"];
                var maxDataPull = collection["MaxDataPull"];
                int MaxDataPull = (maxDataPull.ToString().Trim() != CONSTANTS.Null) ? Convert.ToInt32(maxDataPull) : 0;


                AIModelFrequency aIModelFrequency = new AIModelFrequency();

                var CustomDataSourceDetails = collection["CustomDataPull"];
                string sourceName = string.Empty;
                if (dataSetUId == "undefined" || dataSetUId == "null")
                    dataSetUId = string.Empty;
                if (!string.IsNullOrEmpty(dataSetUId) && dataSetUId != CONSTANTS.Null && dataSetUId != CONSTANTS.BsonNull)
                {
                    if (!dataSetsService.CheckDataSetExists(dataSetUId))
                        throw new Exception(CONSTANTS.InvalidInput);
                }
                correlationId = collection["CorrelationId"];
                bool isExists = this.GetAIServiceModelName(ModelName, serviceId, clientUID, deliveryUID, userId);
                if (isExists & string.IsNullOrEmpty(correlationId))
                {
                    throw new Exception("Model Name already exist");
                }
                else
                {
                    AIServiceRequestStatus modelDetails = new AIServiceRequestStatus();
                    if (string.IsNullOrEmpty(correlationId))
                    {
                        correlationId = Guid.NewGuid().ToString();
                    }
                    else
                    {
                        pageInfo = "Retrain";
                        modelStatus = "InProgress";
                        modelMessage = "ReTrain is in Progress";
                        flag = "AutoRetrain";
                        modelDetails = GetAIModelConfig(correlationId, "TrainModel");
                    }
                    if (Customdetails.Count() > 0)
                    {
                        foreach (var item in Customdetails)
                        {
                            if (item.Trim() != "{}")
                            {
                                Customdata += item;
                                sourceName = "Custom";
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
                                dataSource = "Metrics";
                                sourceName = "metric";
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
                        if (!string.IsNullOrEmpty(postedFileName))
                        {
                            sourceName = "file";
                        }
                    }
                    Service service = this.GetAiCoreServiceDetails(serviceId);
                    ingrainRequest = new AIServiceRequestStatus
                    {
                        CorrelationId = correlationId,
                        DataSetUId = dataSetUId,
                        ClientId = clientUID,
                        DeliveryconstructId = deliveryUID,
                        ServiceId = serviceId,
                        //Status = null,
                        Status = "N",
                        ModelName = ModelName,
                        RequestStatus = CONSTANTS.New,
                        Message = null,
                        UniId = Guid.NewGuid().ToString(),
                        Progress = null,
                        PageInfo = pageInfo,
                        SourceDetails = null,
                        DataSource = dataSource,
                        SourceName = sourceName,
                        //ColumnNames = null,
                        //SelectedColumnNames = null,
                        CreatedByUser = userId,
                        CreatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        ModifiedByUser = userId,
                        ModifiedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        ScoreUniqueName = modelDetails.ScoreUniqueName,
                        Threshold_TopnRecords = modelDetails.Threshold_TopnRecords,
                        StopWords = modelDetails.StopWords,
                        MaxDataPull = MaxDataPull
                    };

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
                        var AppFilter = appSettings.Environment.Equals(CONSTANTS.PAMEnvironment) ? filterBuilder.Eq("ApplicationName", CONSTANTS.VDS) : filterBuilder.Eq("ApplicationName", "VDS(AIOPS)"); //TODO: App name need to be VDS(AIOPS) for PAM.

                        var Projection = Builders<AppIntegration>.Projection.Include("ApplicationID").Exclude("_id");
                        var AppData = AppIntegCollection.Find(AppFilter).Project<AppIntegration>(Projection).FirstOrDefault();

                        var fileParams = JsonConvert.DeserializeObject<InputParams>(Customdata);
                        InputParams param = new InputParams
                        {
                            ClientID = clientUID,
                            E2EUID = string.IsNullOrEmpty(E2EUID) ? CONSTANTS.Null : E2EUID, //TODO: OCB: removed in FDS
                            DeliveryConstructID = deliveryUID,
                            Environment = fileParams.Environment,
                            RequestType = fileParams.RequestType,
                            ServiceType = fileParams.ServiceType,
                            StartDate = fileParams.StartDate,
                            EndDate = fileParams.EndDate
                        };
                        CustomPayloads AppPayload = new CustomPayloads
                        {
                            AICustom = "True",
                            AppId = AppData.ApplicationID,
                            HttpMethod = CONSTANTS.POST,
                            AppUrl = appSettings.GetVdsDataURL,
                            InputParameters = param
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
                        ingrainRequest.SourceDetails = Customfile.ToJson();
                    }
                    else if (CustomSourceItems != CONSTANTS.Null && CustomSourceItems != string.Empty)
                    {
                        if (Source.ToUpper() == CONSTANTS.CustomDataApi.ToUpper())
                        {
                            var fileParams = JsonConvert.DeserializeObject<CustomInputData>(CustomSourceItems);
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
                            var Data = _encryptionDecryption.Encrypt(JsonConvert.SerializeObject(fileParams.Data));

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

                            ingrainRequest.SourceDetails = CustomAPIData.ToJson();

                            if (fileParams != null)
                            {
                                fileParams.DbEncryption = encryptDB;
                            }

                            CustomDataSourceModel CustomDataSource = new CustomDataSourceModel
                            {
                                _id = Guid.NewGuid().ToString(),
                                CorrelationId = correlationId,
                                CustomDataPullType = CONSTANTS.CustomDataApi,
                                CustomSourceDetails = JsonConvert.SerializeObject(fileParams),
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
                            var Data = _encryptionDecryption.Encrypt(JsonConvert.SerializeObject(QueryData));

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
                            ingrainRequest.SourceDetails = CustomQueryData.ToJson();

                            CustomDataSourceModel CustomDataSource = new CustomDataSourceModel
                            {
                                _id = Guid.NewGuid().ToString(),
                                CorrelationId = correlationId,
                                CustomDataPullType = CONSTANTS.CustomDbQuery,
                                CustomSourceDetails = Convert.ToString(CustomSourceItems),
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
                            Flag = flag,
                            mapping = MappingColumns,
                            mapping_flag = MappingFlag,
                            pad = Entities,
                            metric = Metrices,
                            InstaMl = InstaML,
                            fileupload = _filepath,
                            Customdetails = CONSTANTS.Null

                        };
                        ingrainRequest.SourceDetails = fileUpload.ToJson();
                    }
                    ingrainRequest.Language = Language;
                    this.InsertAIServiceRequestStatus(ingrainRequest);
                    if (dataSource == "Entity")
                    {
                        dynamic data = collection;
                        bool iscarryOutRetraining = false;
                        bool isOnline = false;
                        bool isOffline = false;
                        JObject training = new JObject();
                        JObject prediction = new JObject();
                        JObject retraining = new JObject();

                        if ((collection["IsCarryOutRetraining"].ToString().Trim() != "undefined" && Convert.ToBoolean(collection["IsCarryOutRetraining"]))
                                || (collection["IsOnline"].ToString().Trim() != "undefined" && Convert.ToBoolean(collection["IsOnline"]))
                                    || (collection["IsOffline"].ToString().Trim() != "undefined" && Convert.ToBoolean(collection["IsOffline"])))
                        {
                            iscarryOutRetraining = Convert.ToBoolean(collection["IsCarryOutRetraining"]);
                            isOnline = Convert.ToBoolean(collection["IsOnline"]);
                            isOffline = Convert.ToBoolean(collection["IsOffline"]);
                            if (collection["Training"].ToString() != "{}")
                                training = JsonConvert.DeserializeObject<JObject>(collection["Training"]);
                            if (collection["Prediction"].ToString() != "{}")
                                prediction = JsonConvert.DeserializeObject<JObject>(collection["Prediction"]);
                            if (collection["Retraining"].ToString() != "{}")
                                retraining = JsonConvert.DeserializeObject<JObject>(collection["Retraining"]);
                        }


                        AICoreModels aICorefreqModels = new AICoreModels();
                        aICorefreqModels.IsCarryOutRetraining = iscarryOutRetraining;
                        aICorefreqModels.IsOnline = isOnline;
                        aICorefreqModels.IsOffline = isOffline;
                        aICorefreqModels.Retraining = aIModelFrequency;
                        aICorefreqModels.Training = aIModelFrequency;
                        aICorefreqModels.Prediction = aIModelFrequency;
                        if (iscarryOutRetraining && retraining != null && ((JContainer)retraining).HasValues)
                        {
                            aICorefreqModels.Retraining = new AIModelFrequency();
                            this.AssignFrequency(aICorefreqModels, aICorefreqModels.Retraining, retraining, CONSTANTS.Retraining);

                        }
                        if (aICorefreqModels.IsOffline)
                        {
                            if (training != null && ((JContainer)training).HasValues)
                            {
                                aICorefreqModels.Training = new AIModelFrequency();
                                this.AssignFrequency(aICorefreqModels, aICorefreqModels.Training, training, CONSTANTS.Training);
                            }
                            if (prediction != null && ((JContainer)prediction).HasValues)
                            {
                                aICorefreqModels.Prediction = new AIModelFrequency();
                                this.AssignFrequency(aICorefreqModels, aICorefreqModels.Prediction, prediction, CONSTANTS.Prediction);
                            }
                        }
                        this.InsertAICoreModel(ingrainRequest, aICorefreqModels, modelStatus, modelMessage);
                    }
                    else
                        this.CreateAICoreModel(ingrainRequest.ClientId, ingrainRequest.DeliveryconstructId, ingrainRequest.ServiceId, ingrainRequest.CorrelationId, ingrainRequest.UniId, ingrainRequest.ModelName, null, modelStatus, modelMessage, userId, dataSource, pageInfo, dataSetUId, MaxDataPull);
                    string encrypteduser = userId;
                    if (appSettings.isForAllData)
                    {
                        if (!string.IsNullOrEmpty(Convert.ToString(userId)))
                            encrypteduser = _encryptionDecryption.Encrypt(Convert.ToString(userId));
                    }
                    payload["correlationId"] = correlationId;
                    payload["userId"] = encrypteduser;
                    payload["pageInfo"] = pageInfo;
                    payload["UniqueId"] = ingrainRequest.UniId;
                    string apipayload = string.Empty;
                    if (service != null)
                    {
                        if (!string.IsNullOrWhiteSpace(service.PrimaryTrainApiUrl))
                        {
                            baseUrl = appSettings.AICorePythonURL;
                            apiPath = service.PrimaryTrainApiUrl;
                            apiPath = apiPath + "?" + "correlationId=" + correlationId + "&userId=" + encrypteduser + "&pageInfo=" + pageInfo + "&UniqueId=" + ingrainRequest.UniId;
                        }
                    }
                    //message = RouteGETRequest(string.Empty, new Uri(baseUrl), apiPath, false);
                    message.CorrelationId = correlationId;
                    message.IsSuccess = true;
                    JObject responseData = new JObject();
                    responseData.Add("message", "Success");
                    responseData.Add("status", "True");
                    message.ReturnValue = responseData;
                }
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(AISeriveIngestData), "END", string.Empty, string.Empty, string.Empty, string.Empty);
            return message;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ingrainRequest"></param>
        public void InsertAIServiceRequestStatus(AIServiceRequestStatus ingrainRequest)
        {
            if (appSettings.isForAllData)
            {
                if (!string.IsNullOrEmpty(Convert.ToString(ingrainRequest.CreatedByUser)))
                    ingrainRequest.CreatedByUser = _encryptionDecryption.Encrypt(Convert.ToString(ingrainRequest.CreatedByUser));
                if (!string.IsNullOrEmpty(Convert.ToString(ingrainRequest.ModifiedByUser)))
                    ingrainRequest.ModifiedByUser = _encryptionDecryption.Encrypt(Convert.ToString(ingrainRequest.ModifiedByUser));
            }
            var collection = _database.GetCollection<BsonDocument>("AIServiceRequestStatus");
            var filter = Builders<BsonDocument>.Filter.Eq("CorrelationId", ingrainRequest.CorrelationId) & Builders<BsonDocument>.Filter.Eq("ClientId", ingrainRequest.ClientId) & Builders<BsonDocument>.Filter.Eq("DeliveryconstructId", ingrainRequest.DeliveryconstructId) & Builders<BsonDocument>.Filter.Eq("ServiceId", ingrainRequest.ServiceId);
            var projection = Builders<BsonDocument>.Projection.Exclude("_id");
            var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                foreach (var item in result)
                {
                    if (item["SelectedColumnNames"] != BsonNull.Value)
                    {
                        var data = item["SelectedColumnNames"].ToJson();
                        var dataset = JsonConvert.DeserializeObject<List<object>>(data);
                        ingrainRequest.SelectedColumnNames = dataset;
                    }
                }
                var requestQueue = JsonConvert.SerializeObject(ingrainRequest);
                var insertRequestQueue = BsonSerializer.Deserialize<BsonDocument>(requestQueue);
                collection.InsertOne(insertRequestQueue);
            }
            else
            {
                var requestQueue = JsonConvert.SerializeObject(ingrainRequest);
                var insertRequestQueue = BsonSerializer.Deserialize<BsonDocument>(requestQueue);
                collection.InsertOne(insertRequestQueue);
            }

        }

        /// <summary>
        /// AI service Ingest Status
        /// </summary>
        /// <param name="correlationId">CorrelationId</param>
        /// <param name="pageInfo">PageInfo</param>
        /// <returns>AIs</returns>
        public AIServiceRequestStatus AIServiceIngestStatus(string correlationId, string pageInfo)
        {
            var Jobject = new JObject();
            AIServiceRequestStatus ingrainRequest = new AIServiceRequestStatus();
            var collection = _database.GetCollection<AIServiceRequestStatus>("AIServiceRequestStatus");
            var builder = Builders<AIServiceRequestStatus>.Filter;
            var filter = builder.Eq(CONSTANTS.CorrelationId, correlationId) & builder.Eq("PageInfo", pageInfo);
            var projection = Builders<AIServiceRequestStatus>.Projection.Exclude(CONSTANTS.Id);
            ingrainRequest = collection.Find(filter).Project<AIServiceRequestStatus>(projection).ToList().FirstOrDefault();
            if (ingrainRequest != null)
            {
                if (appSettings.isForAllData)
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(Convert.ToString(ingrainRequest.CreatedByUser)))
                            ingrainRequest.CreatedByUser = _encryptionDecryption.Decrypt(Convert.ToString(ingrainRequest.CreatedByUser));
                    }
                    catch (Exception ex)
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(AIServiceIngestStatus), ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty);
                    }
                    try
                    {
                        if (!string.IsNullOrEmpty(Convert.ToString(ingrainRequest.ModifiedByUser)))
                            ingrainRequest.ModifiedByUser = _encryptionDecryption.Decrypt(Convert.ToString(ingrainRequest.ModifiedByUser));
                    }
                    catch (Exception ex)
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(AIServiceIngestStatus), ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty);
                    }
                }
                if (ingrainRequest.Status == "C")
                {
                    var aiServicePreProcess = _database.GetCollection<BsonDocument>("AICore_Preprocessing");
                    var builder1 = Builders<BsonDocument>.Filter;
                    var aiCoreFilter = builder1.Eq(CONSTANTS.CorrelationId, correlationId) & builder1.Ne("Filters", BsonNull.Value);
                    var aiCoreProjection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id).Include("CorrelationId").Include("Filters");
                    var result = aiServicePreProcess.Find(aiCoreFilter).Project<BsonDocument>(aiCoreProjection).ToList();
                    if (result.Count > 0)
                    {
                        try
                        {
                            result[0][CONSTANTS.Filters] = BsonDocument.Parse(_encryptionDecryption.Decrypt(result[0][CONSTANTS.Filters].AsString));
                            Jobject = JObject.Parse(result[0]["Filters"].ToString());
                        }
                        catch (Exception)
                        {
                            Jobject = JObject.Parse(result[0]["Filters"].ToString());
                        }
                        ingrainRequest.FilterAttribute = Jobject;
                    }
                }
            }

            return ingrainRequest;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="pageInfo"></param>
        /// <param name="wfId"></param>
        /// <returns></returns>
        public JObject RetrainModelDetails(string correlationId)
        {
            JObject sourceDetails = new JObject();

            var clusterCollection = _database.GetCollection<BsonDocument>(CONSTANTS.Clustering_IngestData);
            var filter2 = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var clusterProjection = Builders<BsonDocument>.Projection.Include("DataSource").Include("ParamArgs").Exclude(CONSTANTS.Id);
            var clusterResult = clusterCollection.Find(filter2).Project<BsonDocument>(clusterProjection).ToList();
            if (clusterResult.Count > 0)
            {
                JObject paditem = JObject.Parse(clusterResult[0].ToString());
                if (appSettings.Environment.Equals(CONSTANTS.FDSEnvironment) || appSettings.Environment.Equals(CONSTANTS.PAMEnvironment))
                {
                    var Customdetails = JsonConvert.DeserializeObject<JObject>(paditem["ParamArgs"].ToString())["Customdetails"];
                    if (Customdetails.HasValues)
                    {
                        Customdetails = JsonConvert.DeserializeObject<JObject>(paditem["ParamArgs"].ToString())["Customdetails"]["InputParameters"];
                        sourceDetails["Custom"] = Customdetails;
                        sourceDetails["Source"] = clusterResult[0]["DataSource"].ToString();
                    }
                    else
                    {
                        var pad = JsonConvert.DeserializeObject<JObject>(paditem["ParamArgs"].ToString())["pad"];
                        sourceDetails["pad"] = pad;
                        sourceDetails["Source"] = clusterResult[0]["DataSource"].ToString();
                    }
                }
                else
                {
                    var pad = JsonConvert.DeserializeObject<JObject>(paditem["ParamArgs"].ToString())["pad"];
                    sourceDetails["pad"] = pad;
                    sourceDetails["Source"] = clusterResult[0]["DataSource"].ToString();
                }
            }
            else
            {
                AIServiceRequestStatus ingrainRequest = new AIServiceRequestStatus();
                var collection = _database.GetCollection<AIServiceRequestStatus>("AIServiceRequestStatus");
                var builder = Builders<AIServiceRequestStatus>.Filter;
                var filter = builder.Eq(CONSTANTS.CorrelationId, correlationId);
                var projection = Builders<AIServiceRequestStatus>.Projection.Exclude("_id").Include("SourceDetails").Include("DataSource");
                ingrainRequest = collection.Find(filter).Project<AIServiceRequestStatus>(projection).ToList().FirstOrDefault();
                if (appSettings.Environment.Equals(CONSTANTS.FDSEnvironment) || appSettings.Environment.Equals(CONSTANTS.PAMEnvironment))
                {
                    var Customdetails = JsonConvert.DeserializeObject<JObject>(ingrainRequest.SourceDetails)["Customdetails"];
                    if (Customdetails.HasValues)
                    {
                        Customdetails = JsonConvert.DeserializeObject<JObject>(ingrainRequest.SourceDetails)["Customdetails"]["InputParameters"];
                        sourceDetails["Custom"] = Customdetails;
                        sourceDetails["Source"] = ingrainRequest.DataSource;
                    }
                    else
                    {
                        var pad = JsonConvert.DeserializeObject<JObject>(ingrainRequest.SourceDetails)["pad"];
                        sourceDetails["pad"] = pad;
                        sourceDetails["Source"] = ingrainRequest.DataSource;
                    }
                }
                else
                {
                    var pad = JsonConvert.DeserializeObject<JObject>(ingrainRequest.SourceDetails)["pad"];
                    sourceDetails["pad"] = pad;
                    sourceDetails["Source"] = ingrainRequest.DataSource;
                }
            }
            return sourceDetails;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ingrainRequest"></param>
        public void UpdateSelectedColumn(dynamic selectedColumns, string correlationId)
        {
            var collection = _database.GetCollection<BsonDocument>("AIServiceRequestStatus");
            var filter = Builders<BsonDocument>.Filter.Eq("CorrelationId", correlationId);
            var result = collection.Find(filter).ToList();
            if (result.Count > 0)
            {
                var update = Builders<BsonDocument>.Update.Set("SelectedColumnNames", selectedColumns);
                collection.UpdateOne(filter, update);
            }
        }

        public string InsertTrainrequest(dynamic selectedColumns, string correlationId, dynamic filterAttribute)
        {

            AIServiceRequestStatus aIServiceRequestStatus = new AIServiceRequestStatus();
            AIServiceRequestStatus ingrainRequest = new AIServiceRequestStatus();
            var collection = _database.GetCollection<BsonDocument>("AIServiceRequestStatus");
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq(CONSTANTS.CorrelationId, correlationId) & builder.Eq("PageInfo", "IngestData") & builder.Eq("Status", "C");
            var projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
            var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                if (appSettings.isForAllData)
                {
                    try
                    {
                        if (result[0].Contains("CreatedByUser") && !string.IsNullOrEmpty(Convert.ToString(result[0]["CreatedByUser"])))
                            result[0]["CreatedByUser"] = _encryptionDecryption.Decrypt(Convert.ToString(result[0]["CreatedByUser"]));
                    }
                    catch (Exception ex)
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(InsertTrainrequest), ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty);
                    }
                    try
                    {
                        if (result[0].Contains("ModifiedByUser") && !string.IsNullOrEmpty(Convert.ToString(result[0]["ModifiedByUser"])))
                            result[0]["ModifiedByUser"] = _encryptionDecryption.Decrypt(Convert.ToString(result[0]["ModifiedByUser"]));
                    }
                    catch (Exception ex)
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(InsertTrainrequest), ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty);
                    }
                }
                aIServiceRequestStatus = JsonConvert.DeserializeObject<AIServiceRequestStatus>(result[0].ToJson());
                aIServiceRequestStatus.SelectedColumnNames = selectedColumns;
                aIServiceRequestStatus.UniId = Guid.NewGuid().ToString();
                aIServiceRequestStatus.PageInfo = "TrainModel";
                aIServiceRequestStatus.Status = null;
                aIServiceRequestStatus.RequestStatus = "New";
                aIServiceRequestStatus.Progress = null;
                aIServiceRequestStatus.Message = null;
                aIServiceRequestStatus.CreatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                aIServiceRequestStatus.ModifiedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                InsertAIServiceRequest(aIServiceRequestStatus);
            }

            var collection1 = _database.GetCollection<BsonDocument>("AICore_Preprocessing");
            var builder1 = Builders<BsonDocument>.Filter;
            var filter1 = builder1.Eq(CONSTANTS.CorrelationId, correlationId) & builder.Ne("Filters", BsonNull.Value);
            var ingestProjection1 = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id).Include("Filters");
            var result1 = collection1.Find(filter1).Project<BsonDocument>(ingestProjection1).ToList();
            if (result1.Count > 0)
            {
                result1[0][CONSTANTS.Filters] = BsonDocument.Parse(_encryptionDecryption.Decrypt(result1[0][CONSTANTS.Filters].AsString));
                //if (result1[0].Contains(CONSTANTS.CreatedByUser) && !string.IsNullOrEmpty(Convert.ToString(result1[0][CONSTANTS.CreatedByUser])))
                //{
                //    result1[0][CONSTANTS.CreatedByUser] = _encryptionDecryption.Decrypt(Convert.ToString(result1[0][CONSTANTS.CreatedByUser]));
                //}
                //if (result1[0].Contains(CONSTANTS.ModifiedByUser) && !string.IsNullOrEmpty(Convert.ToString(result1[0][CONSTANTS.ModifiedByUser])))
                //{
                //    result1[0][CONSTANTS.ModifiedByUser] = _encryptionDecryption.Decrypt(Convert.ToString(result1[0][CONSTANTS.ModifiedByUser]));
                //}
                var preProcessData = JObject.Parse(result1[0].ToString());
                UpdateAIServiceFilterPreprocess(filterAttribute, preProcessData, collection1, filter1, true);
            }
            return aIServiceRequestStatus.UniId;

        }
        public string InsertTrainrequestBulkMultiple(dynamic selectedColumns, string correlationId, dynamic filterAttribute, string scoreUniqueName, dynamic threshold_TopnRecords, dynamic stopWords, int maxDataPull)
        {

            AIServiceRequestStatus aIServiceRequestStatus = new AIServiceRequestStatus();
            AIServiceRequestStatus ingrainRequest = new AIServiceRequestStatus();
            var collection = _database.GetCollection<BsonDocument>("AIServiceRequestStatus");
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq(CONSTANTS.CorrelationId, correlationId) & builder.Eq("PageInfo", "IngestData") & builder.Eq("Status", "C");
            var projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
            var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                if (appSettings.isForAllData)
                {
                    try
                    {
                        if (result[0].Contains("CreatedByUser") && !string.IsNullOrEmpty(Convert.ToString(result[0]["CreatedByUser"])))
                            result[0]["CreatedByUser"] = _encryptionDecryption.Decrypt(Convert.ToString(result[0]["CreatedByUser"]));
                    }
                    catch (Exception ex)
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(InsertTrainrequestBulkMultiple), ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty);
                    }
                    try
                    {
                        if (result[0].Contains("ModifiedByUser") && !string.IsNullOrEmpty(Convert.ToString(result[0]["ModifiedByUser"])))
                            result[0]["ModifiedByUser"] = _encryptionDecryption.Decrypt(Convert.ToString(result[0]["ModifiedByUser"]));
                    }
                    catch (Exception ex)
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(InsertTrainrequestBulkMultiple), ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty);
                    }
                }

                aIServiceRequestStatus = JsonConvert.DeserializeObject<AIServiceRequestStatus>(result[0].ToJson());
                aIServiceRequestStatus.SelectedColumnNames = selectedColumns;
                aIServiceRequestStatus.UniId = Guid.NewGuid().ToString();
                aIServiceRequestStatus.PageInfo = "TrainModel";
                aIServiceRequestStatus.Status = null;
                aIServiceRequestStatus.RequestStatus = "New";
                aIServiceRequestStatus.Progress = null;
                aIServiceRequestStatus.Message = null;
                aIServiceRequestStatus.CreatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                aIServiceRequestStatus.ModifiedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                aIServiceRequestStatus.ScoreUniqueName = scoreUniqueName;
                aIServiceRequestStatus.Threshold_TopnRecords = threshold_TopnRecords;
                aIServiceRequestStatus.StopWords = stopWords;
                aIServiceRequestStatus.MaxDataPull = maxDataPull;
                InsertAIServiceRequest(aIServiceRequestStatus);
            }

            var collection1 = _database.GetCollection<BsonDocument>("AICore_Preprocessing");
            var builder1 = Builders<BsonDocument>.Filter;
            var filter1 = builder1.Eq(CONSTANTS.CorrelationId, correlationId) & builder.Ne("Filters", BsonNull.Value);
            var ingestProjection1 = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id).Include("Filters");
            var result1 = collection1.Find(filter1).Project<BsonDocument>(ingestProjection1).ToList();
            if (result1.Count > 0)
            {
                result1[0][CONSTANTS.Filters] = BsonDocument.Parse(_encryptionDecryption.Decrypt(result1[0][CONSTANTS.Filters].AsString));
                //if (!string.IsNullOrEmpty(Convert.ToString(result1[0][CONSTANTS.CreatedByUser])))
                //{
                //    result1[0][CONSTANTS.CreatedByUser] = _encryptionDecryption.Decrypt(result1[0][CONSTANTS.CreatedByUser].AsString);
                //}
                //if (!string.IsNullOrEmpty(Convert.ToString(result1[0][CONSTANTS.ModifiedByUser])))
                //{
                //    result1[0][CONSTANTS.ModifiedByUser] = _encryptionDecryption.Decrypt(result1[0][CONSTANTS.ModifiedByUser].AsString);
                //}
                var preProcessData = JObject.Parse(result1[0].ToString());
                UpdateAIServiceFilterPreprocess(filterAttribute, preProcessData, collection1, filter1, true);
            }
            return aIServiceRequestStatus.UniId;

        }
        public void UpdateAIServiceRequestStatus(string corrId, string uniId, string status, string message)
        {
            var requestsCollection = _database.GetCollection<AIServiceRequestStatus>(CONSTANTS.AIServiceRequestStatus);
            var filter = Builders<AIServiceRequestStatus>.Filter.Where(x => x.CorrelationId == corrId)
                         & Builders<AIServiceRequestStatus>.Filter.Where(x => x.UniId == uniId);
            var update = Builders<AIServiceRequestStatus>.Update.Set(x => x.Status, status)
                                                                             .Set(x => x.Message, message)
                                                                             .Set(x => x.ModifiedOn, DateTime.UtcNow.ToString());
            var res1 = requestsCollection.UpdateOne(filter, update);
        }


        private void UpdateAIServiceFilterPreprocess(dynamic data, JObject preProcessData, IMongoCollection<BsonDocument> collection, FilterDefinition<BsonDocument> filter, bool DBEncryptionRequired)
        {
            //Update, UI forwarded Values into Collection
            foreach (var fileterValues in data)
            {
                var prop = fileterValues as JProperty;
                if (prop != null)
                {
                    foreach (JToken child in preProcessData[CONSTANTS.Filters][prop.Name].Children())
                    {
                        JProperty property = child as JProperty;
                        if (property != null && property.Value.ToString() == CONSTANTS.True)
                        {
                            string updateField = string.Format(CONSTANTS.Filters01, fileterValues.Name, property.Name);
                            //preProcessData[CONSTANTS.Filters]["FilterDisplayedinUI"] = CONSTANTS.False;
                        }
                    }
                    JObject jObject = JObject.Parse(prop.Value.ToString());//JArray.Parse(prop.Value.ToString());
                    if (jObject != null)
                    {
                        foreach (JProperty jProperty in jObject.Children())
                        {
                            //foreach (JProperty jProperty in value.Properties())
                            //{
                            string updateField = string.Format(CONSTANTS.Filters01, fileterValues.Name, jProperty.Name.ToString().Trim());
                            preProcessData[CONSTANTS.Filters][fileterValues.Name][jProperty.Name.ToString().Trim()] = jProperty.Value.ToString().Trim();
                            //}

                        }
                        //  string updateField2 = string.Format(CONSTANTS.Filters01, fileterValues.Name, CONSTANTS.ChangeRequest);
                        //preProcessData[CONSTANTS.Filters]["FilterDisplayedinUI"] = CONSTANTS.True;
                    }

                }
            }
            //encrypt db data
            if (DBEncryptionRequired)
            {
                var FiltersValuesUpdate = Builders<BsonDocument>.Update.Set(CONSTANTS.Filters, _encryptionDecryption.Encrypt(preProcessData[CONSTANTS.Filters].ToString(Formatting.None)));
                collection.UpdateOne(filter, FiltersValuesUpdate);
            }
            else
            {

                var FiltersValue = BsonDocument.Parse(preProcessData[CONSTANTS.Filters].ToString());
                var FiltersValuesUpdate = Builders<BsonDocument>.Update.Set(CONSTANTS.Filters, FiltersValue);
                collection.UpdateOne(filter, FiltersValuesUpdate);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="pageInfo"></param>
        /// <param name="wfId"></param>
        /// <returns></returns>
        public bool GetAIServiceModelName(string ModelName, string serviceId, string clientUID, string deliveryUID, string userId)
        {
            string encrypteduser = userId;
            if (!string.IsNullOrEmpty(encrypteduser))
            {
                encrypteduser = _encryptionDecryption.Encrypt(Convert.ToString(encrypteduser));
            }
            var AIServiceRequestStatus = _database.GetCollection<BsonDocument>("AIServiceRequestStatus");
            var AIServiceFilterBuilder = Builders<BsonDocument>.Filter;
            var AIServiceQueue = AIServiceFilterBuilder.Eq("ModelName", ModelName) & AIServiceFilterBuilder.Eq("ServiceId", serviceId) & AIServiceFilterBuilder.Eq("ClientId", clientUID) & AIServiceFilterBuilder.Eq("DeliveryconstructId", deliveryUID) & (AIServiceFilterBuilder.Eq("CreatedByUser", userId) | AIServiceFilterBuilder.Eq("CreatedByUser", encrypteduser));
            var projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
            var result = AIServiceRequestStatus.Find(AIServiceQueue).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                var data = result[0]["ModelName"].ToString();
                if (data.ToLower() == ModelName.ToLower())
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

        public bool ClusteringIngestData(ClusteringAPIModel clusteringAPI)
        {
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

                    clusteringAPI._id = Guid.NewGuid().ToString();
                    clusteringAPI.DataSource = result[0]["DataSource"].ToString();
                    if (result[0]["MaxDataPull"] != null)
                        clusteringAPI.MaxDataPull = Convert.ToInt32(result[0]["MaxDataPull"]);
                    var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(clusteringAPI);
                    var insertDocument = BsonSerializer.Deserialize<BsonDocument>(jsonData);
                    collection.DeleteOne(filter);
                    Thread.Sleep(1000);
                    collection.InsertOne(insertDocument);
                }
                else
                {
                    var aIServiceRequestStatus = JsonConvert.DeserializeObject<ClusteringAPIModel>(result[0].ToJson());
                    aIServiceRequestStatus.Columnsselectedbyuser = clusteringAPI.Columnsselectedbyuser;
                    aIServiceRequestStatus.UniId = clusteringAPI.UniId;
                    aIServiceRequestStatus.PageInfo = clusteringAPI.PageInfo;
                    aIServiceRequestStatus.SelectedModels = clusteringAPI.SelectedModels;
                    aIServiceRequestStatus.ProblemType = clusteringAPI.ProblemType;
                    aIServiceRequestStatus.retrain = clusteringAPI.retrain;
                    aIServiceRequestStatus.CreatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    aIServiceRequestStatus.ModifiedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    aIServiceRequestStatus.MaxDataPull = clusteringAPI.MaxDataPull;
                    var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(aIServiceRequestStatus);
                    var insertDocument = BsonSerializer.Deserialize<BsonDocument>(jsonData);
                    collection.DeleteOne(filter);
                    Thread.Sleep(1000);
                    collection.InsertOne(insertDocument);
                }

                //var updateBuilder = Builders<BsonDocument>.Update;
                //var update = updateBuilder.Set("PageInfo", clusteringAPI.PageInfo)
                //    .Set("SelectedModels", clusteringAPI.SelectedModels.ToJson())
                //    .Set("ProblemType", clusteringAPI.ProblemType.ToJson())
                //    .Set("SelectedColumns", clusteringAPI.SelectedColumns.ToJson())
                //    .Set("retrain", clusteringAPI.retrain)ss
                //    .Set("CreatedOn", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                //    .Set("ModifiedOn", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                //collection.UpdateMany(filter, update);
                return true;
            }
            else
            {
                clusteringAPI._id = Guid.NewGuid().ToString();
                var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(clusteringAPI);
                var insertDocument = BsonSerializer.Deserialize<BsonDocument>(jsonData);
                collection.InsertOne(insertDocument);
                return true;
            }
        }

        public JObject GetClusteringDetails(string correlationId)
        {
            JObject json = new JObject();
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.Clustering_IngestData);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
            var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                var newData = result[0];//ingestData.FirstOrDefault(corr => corr["CorrelationId"] == data.clusteringStatus[i].CorrelationId);
                if (newData != null)
                {
                    //data.clusteringStatus[i].ingestData = newData;
                    var jsonWriterSettings = new MongoDB.Bson.IO.JsonWriterSettings { OutputMode = MongoDB.Bson.IO.JsonOutputMode.Strict };
                    json = JObject.Parse(newData.ToJson<MongoDB.Bson.BsonDocument>(jsonWriterSettings));

                    // data.clusteringStatus[i].ingestData = json;// JsonConvert.DeserializeObject<dynamic>(newData.ToJson());

                }
            }
            return json;
        }


        // Initiate training through Generic API
        public AIGENERICSERVICE.TrainingResponse TrainAIServiceModel(HttpContext httpContext, string resourceId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(TrainAIServiceModel), CONSTANTS.START, string.Empty, string.Empty, string.Empty, string.Empty);
            AIGENERICSERVICE.TrainingRequest trainingRequest = new AIGENERICSERVICE.TrainingRequest();
            string status = "InProgress";
            string statusMessage = "Training is in progress";
            string flag = CONSTANTS.Null;
            string pageInfo = "Ingest_Train";
            bool isCLusteringService = false;
            bool isIntentEntity = false;
            IFormCollection collection = httpContext.Request.Form;
            if (!CommonUtility.GetValidUser(Convert.ToString(collection["UserId"])))
            {
                throw new Exception("UserName/UserId is Invalid");
            }
            trainingRequest.ApplicationId = collection["ApplicationId"];
            trainingRequest.ClientId = collection["ClientId"];
            trainingRequest.DeliveryConstructId = collection["DeliveryConstructId"];
            trainingRequest.ServiceId = collection["ServiceId"];
            trainingRequest.UsecaseId = collection["UsecaseId"];
            trainingRequest.ModelName = collection["ModelName"];
            trainingRequest.DataSource = collection["DataSource"];
            trainingRequest.DataSourceDetails = collection["DataSourceDetails"];
            trainingRequest.UserId = collection["UserId"];
            trainingRequest.ResponseCallBackUrl = collection["ResponseCallbackUrl"];
            string correlationId = collection["CorrelationId"];
            string language = collection["Language"];

            trainingRequest.CustomDataSourceDetails = collection["CustomDataPull"];

            string pyModelName = string.Empty;
            bool isIntentRetrain = false;
            var datasetUId = collection["DataSetUId"];
            var CustomDataSourceDetails = collection["CustomDataPull"];
            string CustomSourceItems = string.Empty;
            trainingRequest.DataSetUId = datasetUId;
            if (trainingRequest.DataSetUId == "undefined" || trainingRequest.DataSetUId == "null")
                trainingRequest.DataSetUId = null;

            string trainingMessage = "Training Initiated";
            CustomInputPayload appPayload = new CustomInputPayload();
            AIGENERICSERVICE.TrainingResponse trainingResponse
                = new AIGENERICSERVICE.TrainingResponse(trainingRequest.ClientId,
                                                        trainingRequest.DeliveryConstructId,
                                                        trainingRequest.ServiceId,
                                                        trainingRequest.UsecaseId,
                                                        trainingRequest.ModelName,
                                                        trainingRequest.UserId);

            AIServiceRequestStatus ingestRequest = new AIServiceRequestStatus();
            ClusteringAPIModel clusteringAPIModel = new ClusteringAPIModel();
            ParentFile parentDetail = new ParentFile();
            MethodReturn<object> message = new MethodReturn<object>();
            string baseUrl = string.Empty;
            string apiPath = string.Empty;
            USECASE.UsecaseDetails usecaseDetails = GetUsecaseDetails(trainingRequest.UsecaseId.Trim());
            bool iscarryOutRetraining = false;
            bool isOnline = false;
            bool isOffline = false;
            JObject training = new JObject();
            JObject prediction = new JObject();
            JObject retraining = new JObject();

            if (usecaseDetails.IsCarryOutRetraining || usecaseDetails.IsOffline || usecaseDetails.IsOnline)
            {
                iscarryOutRetraining = usecaseDetails.IsCarryOutRetraining;
                isOnline = usecaseDetails.IsOnline;
                isOffline = usecaseDetails.IsOffline;
                training = JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(usecaseDetails.Training));// JsonConvert.DeserializeObject<JObject>(usecaseDetails.Training.ToString());
                prediction = JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(usecaseDetails.Prediction));
                retraining = JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(usecaseDetails.Retraining));
            }

            AIModelFrequency aIModelFrequency = new AIModelFrequency();
            ClustModelFrequency clustModelFrequency = new ClustModelFrequency();


            Service service = GetAiCoreServiceDetails(trainingRequest.ServiceId);
            //Asset Usage
            auditTrailLog.ApplicationName = usecaseDetails.ApplicationName;
            auditTrailLog.ApplicationID = trainingRequest.ApplicationId;
            auditTrailLog.ClientId = trainingRequest.ClientId;
            auditTrailLog.DCID = trainingRequest.DeliveryConstructId;
            auditTrailLog.UseCaseId = trainingRequest.UsecaseId;
            auditTrailLog.CreatedBy = trainingRequest.UserId;
            auditTrailLog.ProcessName = CONSTANTS.TrainingName;
            auditTrailLog.UsageType = CONSTANTS.AssetUsage;
            if (string.IsNullOrEmpty(auditTrailLog.ApplicationName))
            {
                auditTrailLog.FeatureName = service.ServiceName;
            }
            else
            {
                auditTrailLog.FeatureName = auditTrailLog.ApplicationName + "-" + service.ServiceName;
            }
            CommonUtility.AuditTrailLog(auditTrailLog, configSetting);
            //Check for serviceId
            if (service != null && string.IsNullOrEmpty(service.ServiceId))
            {
                throw new Exception("Service Id is not correct");
            }
            //Check usecaseId
            if (string.IsNullOrEmpty(usecaseDetails.UsecaseId))
            {
                throw new Exception("UsecaseId is not correct");
            }
            //Check ApplicationId
            if (usecaseDetails.ApplicationId != trainingRequest.ApplicationId)
            {
                throw new Exception("ApplicationId doesn't match the usecaseId");
            }
            //Check ModelName
            if (string.IsNullOrEmpty(trainingRequest.ModelName))
            {
                throw new Exception("Please provide ModelName");
            }
            if (service.ServiceCode == "CLUSTERING" || service.ServiceCode == "WORDCLOUD")
            {
                isCLusteringService = true;
                baseUrl = appSettings.ClusteringPythonURL;
                apiPath = CONSTANTS.Clustering_ModelTraining;
                bool isExists = _clusteringAPI.GetClusteringModelName(clusteringAPIModel);
                if (isExists && string.IsNullOrEmpty(correlationId))
                {
                    throw new Exception("Model Name already exist");
                }

                clusteringAPIModel.ServiceID = trainingRequest.ServiceId;
                clusteringAPIModel.ClientID = trainingRequest.ClientId;
                clusteringAPIModel.DCUID = trainingRequest.DeliveryConstructId;
                clusteringAPIModel.UserId = trainingRequest.UserId;
                clusteringAPIModel.ModelName = trainingRequest.ModelName;
                clusteringAPIModel.Ngram = usecaseDetails.Ngram;
                clusteringAPIModel.StopWords = usecaseDetails.StopWords;
                clusteringAPIModel.ScoreUniqueName = usecaseDetails.ScoreUniqueName;
                clusteringAPIModel.Threshold_TopnRecords = usecaseDetails.Threshold_TopnRecords;
                clusteringAPIModel.DataSetUId = usecaseDetails.DataSetUID;
                clusteringAPIModel.MaxDataPull = usecaseDetails.MaxDataPull;
                clusteringAPIModel.IsCarryOutRetraining = usecaseDetails.IsCarryOutRetraining;
                clusteringAPIModel.IsOffline = usecaseDetails.IsOffline;
                clusteringAPIModel.IsOnline = usecaseDetails.IsOnline;
                clusteringAPIModel.Training = clustModelFrequency;
                clusteringAPIModel.Retraining = clustModelFrequency;
                clusteringAPIModel.Prediction = clustModelFrequency;
                if (usecaseDetails.IsCarryOutRetraining && retraining != null && ((JContainer)retraining).HasValues)
                {
                    clusteringAPIModel.Retraining = new ClustModelFrequency();
                    this.AssignFrequencyClustering(clusteringAPIModel, clusteringAPIModel.Retraining, retraining, CONSTANTS.Retraining);

                }
                if (clusteringAPIModel.IsOffline)
                {
                    if (training != null && ((JContainer)training).HasValues)
                    {
                        clusteringAPIModel.Training = new ClustModelFrequency();
                        this.AssignFrequencyClustering(clusteringAPIModel, clusteringAPIModel.Training, training, CONSTANTS.Training);
                    }
                    if (prediction != null && ((JContainer)prediction).HasValues)
                    {
                        clusteringAPIModel.Prediction = new ClustModelFrequency();
                        this.AssignFrequencyClustering(clusteringAPIModel, clusteringAPIModel.Prediction, prediction, CONSTANTS.Prediction);
                    }
                }
            }
            else if (service.ServiceCode == "NLPINTENT")
            {
                isIntentEntity = true;
                usecaseDetails.ApplicationId = trainingRequest.ApplicationId;
                baseUrl = appSettings.AICorePythonURL;
                apiPath = service.PrimaryTrainApiUrl;
                ModelsList model = GetAICoreModels(trainingRequest.ClientId, trainingRequest.DeliveryConstructId, trainingRequest.ServiceId, trainingRequest.UserId);
                if (string.IsNullOrEmpty(correlationId) && model.ModelStatus != null)
                {
                    var isModelNameExist = model.ModelStatus.Where(x => x.ModelName.ToLower() == trainingRequest.ModelName.ToLower()).ToList();

                    if (isModelNameExist.Count > 0)
                    {
                        throw new Exception("Model Name already exist");
                    }
                }
            }
            else
            {
                baseUrl = appSettings.AICorePythonURL;
                apiPath = service.PrimaryTrainApiUrl;
                if (trainingRequest.DataSource != "Phoenix" && !string.IsNullOrEmpty(trainingRequest.DataSource))
                {
                    bool isExists = this.GetAIServiceModelName(trainingRequest.ModelName, trainingRequest.ServiceId, trainingRequest.ClientId, trainingRequest.DeliveryConstructId, trainingRequest.UserId);
                    if (isExists && string.IsNullOrEmpty(correlationId))
                    {
                        throw new Exception("Model Name already exist");
                    }
                }
            }
            ////Retrain checking Start
            if (string.IsNullOrEmpty(correlationId))
            {
                correlationId = Guid.NewGuid().ToString();
            }
            else
            {
                if (isCLusteringService)
                {
                    clusteringAPIModel.retrain = true;
                    trainingMessage = "Re-Training Initiated";

                }
                else if (isIntentEntity)
                {
                    var corrDetails = GetAICoreModelPath(correlationId);
                    if (corrDetails == null)
                    {
                        throw new Exception("Correlation id not found");
                    }
                    else
                    {
                        if (corrDetails.ServiceId != trainingRequest.ServiceId)
                        {
                            throw new Exception("ServiceId doesn't match the trained model ServiceId");
                        }
                        if (corrDetails.ModelStatus == "InProgress" || corrDetails.ModelStatus == "Upload InProgress")
                        {
                            throw new Exception("Unable to initiate training, as dataupload/training is already in progress");
                        }
                        if (corrDetails.ClientId != trainingRequest.ClientId || corrDetails.DeliveryConstructId != trainingRequest.DeliveryConstructId)
                        {
                            throw new Exception("ClientId or DCId doesn't match the trained model Client and DC");
                        }
                        else
                        {
                            //clientId = corrDetails.ClientId;
                            //deliveryConstructId = corrDetails.DeliveryConstructId;
                            //serviceId = corrDetails.ServiceId;
                            if (trainingRequest.ModelName == null)
                                trainingRequest.ModelName = corrDetails.ModelName;
                            pyModelName = corrDetails.PythonModelName;
                            pageInfo = "Retrain";
                            status = "InProgress";
                            statusMessage = "ReTrain is in Progress";
                            flag = "AutoRetrain";
                            ingestRequest.CorrelationId = correlationId;
                            isIntentRetrain = true;
                        }
                    }
                }
                else
                {
                    var corrDetails = GetAICoreModelPath(correlationId);
                    if (corrDetails.UsecaseId != trainingRequest.UsecaseId)
                    {
                        throw new Exception("UsecaseId doesn't match the trained model usecaseId");
                    }
                    if (corrDetails.ServiceId != trainingRequest.ServiceId)
                    {
                        throw new Exception("ServiceId doesn't match the trained model ServiceId");
                    }
                    if (corrDetails.ModelStatus == "InProgress" || corrDetails.ModelStatus == "Upload InProgress")
                    {
                        throw new Exception("Unable to initiate Re-training, as dataupload/training is already in progress");
                    }
                    if (corrDetails.ClientId != trainingRequest.ClientId || corrDetails.DeliveryConstructId != trainingRequest.DeliveryConstructId)
                    {
                        throw new Exception("ClientId or DCId doesn't match the trained model Client and DC");
                    }
                    pageInfo = "Retrain";
                    status = "InProgress";
                    statusMessage = "ReTrain is in Progress";
                    flag = "AutoRetrain";
                }
            }
            //Retrain checking end

            if (isCLusteringService)
            {
                var data = this.GetClusteringDetails(usecaseDetails.CorrelationId);
                if (data.Count == 0)
                {
                    throw new Exception("UsecaseId doesn't match the ServiceId");
                }
                string createdByUser = trainingRequest.UserId;
                //if (appSettings.isForAllData)
                {
                    if (!string.IsNullOrEmpty(Convert.ToString(trainingRequest.UserId)))
                    {
                        createdByUser = _encryptionDecryption.Encrypt(Convert.ToString(trainingRequest.UserId));
                        trainingRequest.UserId = createdByUser;
                    }
                }
                clusteringAPIModel.CorrelationId = correlationId;
                clusteringAPIModel.SelectedModels = JsonConvert.DeserializeObject<JObject>(data["SelectedModels"].ToString());
                //_clusteringAPI.ClusteringIngestData(clusteringAPIModel);
                clusteringAPIModel.Columnsselectedbyuser = usecaseDetails.InputColumns;
                clusteringAPIModel.ParamArgs = data["ParamArgs"].ToString();
                clusteringAPIModel.ProblemType = JsonConvert.DeserializeObject<JObject>(data["ProblemType"].ToString());
                clusteringAPIModel.StopWords = usecaseDetails.StopWords;
                clusteringAPIModel.ServiceID = trainingRequest.ServiceId;
                clusteringAPIModel.UniId = Guid.NewGuid().ToString();
                clusteringAPIModel.ModelName = trainingRequest.ModelName;
                clusteringAPIModel.ClientID = trainingRequest.ClientId;
                clusteringAPIModel.DCUID = trainingRequest.DeliveryConstructId;
                clusteringAPIModel.CreatedBy = createdByUser;
                clusteringAPIModel.PageInfo = service.ServiceCode == "WORDCLOUD" ? "wordcloud" : "InvokeIngestData";//"Ingest_Train";
                clusteringAPIModel.CreatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                clusteringAPIModel.ModifiedBy = createdByUser;
                clusteringAPIModel.ModifiedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                clusteringAPIModel.DataSource = trainingRequest.DataSource;
                clusteringAPIModel.Language = language;
                clusteringAPIModel.DataSetUId = trainingRequest.DataSetUId;
                clusteringAPIModel.Ngram = usecaseDetails.Ngram;
                clusteringAPIModel.ScoreUniqueName = usecaseDetails.ScoreUniqueName;
                clusteringAPIModel.Threshold_TopnRecords = usecaseDetails.Threshold_TopnRecords;
                clusteringAPIModel.MaxDataPull = usecaseDetails.MaxDataPull;
                clusteringAPIModel.IsCarryOutRetraining = usecaseDetails.IsCarryOutRetraining;
                clusteringAPIModel.IsOffline = usecaseDetails.IsOffline;
                clusteringAPIModel.IsOnline = usecaseDetails.IsOnline;
                clusteringAPIModel.Training = clustModelFrequency;
                clusteringAPIModel.Retraining = clustModelFrequency;
                clusteringAPIModel.Prediction = clustModelFrequency;
                clusteringAPIModel.UserId = createdByUser;
                if (usecaseDetails.IsCarryOutRetraining && retraining != null && ((JContainer)retraining).HasValues)
                {
                    clusteringAPIModel.Retraining = new ClustModelFrequency();
                    this.AssignFrequencyClustering(clusteringAPIModel, clusteringAPIModel.Retraining, retraining, CONSTANTS.Retraining);

                }
                if (clusteringAPIModel.IsOffline)
                {
                    if (training != null && ((JContainer)training).HasValues)
                    {
                        clusteringAPIModel.Training = new ClustModelFrequency();
                        this.AssignFrequencyClustering(clusteringAPIModel, clusteringAPIModel.Training, training, CONSTANTS.Training);
                    }
                    if (prediction != null && ((JContainer)prediction).HasValues)
                    {
                        clusteringAPIModel.Prediction = new ClustModelFrequency();
                        this.AssignFrequencyClustering(clusteringAPIModel, clusteringAPIModel.Prediction, prediction, CONSTANTS.Prediction);
                    }
                }
            }
            else
            {
                if (!isIntentEntity)
                {
                    if (usecaseDetails.ServiceId != trainingRequest.ServiceId)
                    {
                        throw new Exception("UsecaseId doesn't match the ServiceId");
                    }
                }
                ingestRequest.CorrelationId = correlationId;
                ingestRequest.SelectedColumnNames = usecaseDetails.InputColumns;
                ingestRequest.ServiceId = trainingRequest.ServiceId;
                ingestRequest.UniId = Guid.NewGuid().ToString();
                ingestRequest.ModelName = trainingRequest.ModelName;
                ingestRequest.ClientId = trainingRequest.ClientId;
                ingestRequest.DeliveryconstructId = trainingRequest.DeliveryConstructId;
                ingestRequest.CreatedByUser = trainingRequest.UserId;
                ingestRequest.PageInfo = pageInfo;//"Ingest_Train";
                ingestRequest.CreatedOn = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                ingestRequest.ModifiedByUser = trainingRequest.UserId;
                ingestRequest.ModifiedOn = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                ingestRequest.DataSource = trainingRequest.DataSource;
                ingestRequest.Language = language;
                ingestRequest.ApplicationId = trainingRequest.ApplicationId;
                ingestRequest.UsecaseId = trainingRequest.UsecaseId;
                ingestRequest.DataSetUId = trainingRequest.DataSetUId;
                ingestRequest.StopWords = usecaseDetails.StopWords;
                ingestRequest.ScoreUniqueName = usecaseDetails.ScoreUniqueName;
                ingestRequest.Threshold_TopnRecords = usecaseDetails.Threshold_TopnRecords;
                ingestRequest.MaxDataPull = usecaseDetails.MaxDataPull;
            }
            //if training data is file input
            //Forming the Paramargs for the training  or retraining Start
            if (trainingRequest.DataSource == "File")
            {
                string ParentFileName = "undefined";
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
                        if (isCLusteringService)
                        {
                            filePath = appSettings.ClusteringFilespath.ToString();
                        }
                        else
                        {
                            filePath = appSettings.AICoreFilespath.ToString();

                        }
                        var filePath1 = filePath + correlationId + "/" + folderPath + "/" + fileName;
                        if (!Directory.Exists(filePath + correlationId + "/" + folderPath))
                            Directory.CreateDirectory(filePath + correlationId + "/" + folderPath);
                        var postedFile = fileCollection[i];
                        if (postedFile.Length <= 0)
                        {
                            message.Message = CONSTANTS.FileEmpty;
                            //return message;
                            throw new Exception(message.Message);
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
                if (isCLusteringService)
                {
                    FileUpload fileUpload = new FileUpload
                    {
                        CorrelationId = clusteringAPIModel.CorrelationId,
                        ClientUID = clusteringAPIModel.ClientID,
                        DeliveryConstructUId = clusteringAPIModel.DCUID,
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

                    clusteringAPIModel.ParamArgs = fileUpload.ToJson();
                }
                else
                {
                    FileUpload fileUpload = new FileUpload
                    {
                        CorrelationId = ingestRequest.CorrelationId,
                        ClientUID = ingestRequest.ClientId,
                        DeliveryConstructUId = ingestRequest.DeliveryconstructId,
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

                    ingestRequest.SourceDetails = fileUpload.ToJson();
                }
            }


            //if training data is from phoenix
            if (trainingRequest.DataSource == "Entity")
            {
                _filepath = new Filepath();
                _filepath.fileList = "null";
                parentFile = new ParentFile();
                parentFile.Type = "null";
                parentFile.Name = "null";
                string Entities = string.Empty;
                var entityitems = collection[CONSTANTS.pad];
                var metricitems = collection[CONSTANTS.metrics];
                var InstaMl = collection[CONSTANTS.InstaMl];
                var EntitiesNames = collection[CONSTANTS.EntitiesName];
                var MetricsNames = collection[CONSTANTS.MetricNames];
                if (entityitems.Count() > 0)
                {
                    foreach (var item in entityitems)
                    {
                        if (item != "{}")
                        {
                            Entities += item;
                            // dataSource = "Entity";

                        }
                        else
                            Entities = CONSTANTS.Null;
                    }
                }
                if (isCLusteringService)
                {
                    JObject entities = JObject.Parse(clusteringAPIModel.ParamArgs);
                    FileUpload fileUpload = new FileUpload
                    {
                        CorrelationId = clusteringAPIModel.CorrelationId,
                        ClientUID = clusteringAPIModel.ClientID,
                        DeliveryConstructUId = clusteringAPIModel.DCUID,
                        Parent = parentFile,
                        Flag = CONSTANTS.Null,
                        mapping = CONSTANTS.Null,
                        mapping_flag = CONSTANTS.False,
                        metric = CONSTANTS.Null,
                        InstaMl = CONSTANTS.Null,
                        fileupload = _filepath,
                        Customdetails = CONSTANTS.Null
                    };
                    if (entityitems.Count > 0)
                        fileUpload.pad = Entities;
                    else
                        fileUpload.pad = entities["pad"].ToString();
                    clusteringAPIModel.ParamArgs = fileUpload.ToJson();
                }
                else
                {
                    FileUpload fileUpload = new FileUpload
                    {
                        CorrelationId = ingestRequest.CorrelationId,
                        ClientUID = ingestRequest.ClientId,
                        DeliveryConstructUId = ingestRequest.DeliveryconstructId,
                        Parent = parentFile,
                        Flag = CONSTANTS.Null,
                        mapping = CONSTANTS.Null,
                        mapping_flag = CONSTANTS.False,
                        pad = Entities,
                        metric = CONSTANTS.Null,
                        InstaMl = CONSTANTS.Null,
                        fileupload = _filepath,
                        Customdetails = CONSTANTS.Null

                    };
                    ingestRequest.SourceDetails = fileUpload.ToJson();
                }
            }

            if (trainingRequest.DataSource == "Phoenix")
            {
                var datasourceDetails = JObject.Parse(usecaseDetails.SourceDetails.ToString());
                datasourceDetails["ClientUID"] = trainingRequest.ClientId;
                datasourceDetails["DeliveryConstructUId"] = trainingRequest.DeliveryConstructId;
                datasourceDetails["CorrelationId"] = ingestRequest.CorrelationId;
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
                ingestRequest.ModelName = ingestRequest.ModelName + "_" + ingestRequest.CorrelationId;
                trainingRequest.ModelName = trainingRequest.ModelName + "_" + ingestRequest.CorrelationId;
                trainingResponse.ModelName = trainingRequest.ModelName;
                ingestRequest.SourceDetails = JsonConvert.SerializeObject(datasourceDetails, Formatting.None);
            }

            if (!string.IsNullOrEmpty(trainingRequest.DataSource) && trainingRequest.DataSource.ToUpper() == CONSTANTS.CustomDbQuery.ToUpper())
            {
                if (isCLusteringService)
                {
                    ingestRequest.CorrelationId = clusteringAPIModel.CorrelationId;
                    ingestRequest.ClientId = clusteringAPIModel.ClientID;
                    ingestRequest.DeliveryconstructId = clusteringAPIModel.DCUID;
                }
                if (CustomDataSourceDetails.Count() > 0)
                {
                    foreach (var item in CustomDataSourceDetails)
                    {
                        if (item.Trim() != "{}")
                        {
                            CustomSourceItems += item;
                        }
                        else
                            CustomSourceItems = CONSTANTS.Null;
                    }
                }
                if (CustomSourceItems != CONSTANTS.Null && CustomSourceItems != string.Empty)
                {
                    _filepath = new Filepath();
                    _filepath.fileList = "null";
                    parentFile = new ParentFile();
                    parentFile.Type = "null";
                    parentFile.Name = "null";
                    if (trainingRequest.DataSource.ToUpper() == CONSTANTS.CustomDbQuery.ToUpper())
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
                        var Data = _encryptionDecryption.Encrypt(JsonConvert.SerializeObject(QueryData));

                        CustomQueryParamArgs CustomQueryData = new CustomQueryParamArgs
                        {
                            CorrelationId = ingestRequest.CorrelationId,
                            ClientUID = ingestRequest.ClientId,
                            //E2EUID = E2EUID,
                            E2EUID = CONSTANTS.Null,
                            DeliveryConstructUId = ingestRequest.DeliveryconstructId,
                            Parent = parentFile,
                            Flag = CONSTANTS.Null,
                            mapping = CONSTANTS.Null,
                            mapping_flag = CONSTANTS.False,
                            pad = CONSTANTS.Null,
                            metric = CONSTANTS.Null,
                            InstaMl = CONSTANTS.Null,
                            fileupload = _filepath,
                            Customdetails = CONSTANTS.Null,
                            CustomSource = Data
                        };
                        if (isCLusteringService)
                        {
                            clusteringAPIModel.ParamArgs = CustomQueryData.ToJson();
                        }
                        else
                        {
                            ingestRequest.SourceDetails = CustomQueryData.ToJson();
                        }

                    }
                }
            }

            if (trainingRequest.DataSource == "Custom" || (trainingRequest.DataSource != null && trainingRequest.DataSource.Contains("DataSet")))
            {
                if (isCLusteringService)
                {
                    ingestRequest.CorrelationId = clusteringAPIModel.CorrelationId;
                    ingestRequest.ClientId = clusteringAPIModel.ClientID;
                    ingestRequest.DeliveryconstructId = clusteringAPIModel.DCUID;
                }
                CustomSPAAIFileUpload fileUpload = new CustomSPAAIFileUpload
                {
                    CorrelationId = ingestRequest.CorrelationId,
                    ClientUID = ingestRequest.ClientId,
                    DeliveryConstructUId = ingestRequest.DeliveryconstructId,
                    Parent = parentDetail,
                    Flag = flag,//CONSTANTS.Null,
                    mapping = CONSTANTS.Null,
                    mapping_flag = CONSTANTS.False,//CONSTANTS.Null,
                    pad = CONSTANTS.Null,//string.Empty,
                    metric = CONSTANTS.Null,//string.Empty,
                    InstaMl = CONSTANTS.Null,// string.Empty,
                    fileupload = null,
                };
                AppIntegrationsCredentials appIntegrationsCredentials = new AppIntegrationsCredentials()
                {
                    grant_type = appSettings.Grant_Type,
                    client_id = appSettings.clientId,
                    client_secret = appSettings.clientSecret,
                    resource = resourceId
                };

                AppIntegration appIntegrations = new AppIntegration()
                {
                    ApplicationID = usecaseDetails.ApplicationId,
                    Credentials = JsonConvert.SerializeObject(appIntegrationsCredentials)
                };
                if (trainingRequest.DataSource != "DataSet")
                    this.UpdateAppIntegration(appIntegrations);
                string url = CONSTANTS.Null;
                string bodyParams = CONSTANTS.Null;

                //appPayload = new CustomInputPayload
                //{
                //    AppId = trainingRequest.ApplicationId,
                //    HttpMethod = CONSTANTS.POST,
                //    AppUrl = url.ToString(),
                //    InputParameters = BsonDocument.Parse(bodyParams.ToString()) //BsonDocument.Parse(trainingRequest.DataSourceDetails.BodyParams.ToString())//JObject.Parse(trainingRequest.DataSourceDetails.BodyParams.ToString()) //trainingRequest.DataSourceDetails.BodyParams.ToObject<object>()//JsonConvert.DeserializeObject<JObject>(fileUpload.Customdetails.InputParameters.ToString())//trainingRequest.DataSourceDetails.BodyParams
                //};
                CustomSPAAIPayload customSPAAIPayload = null;
                if (!string.IsNullOrWhiteSpace(trainingRequest.DataSourceDetails.ToString()))
                {
                    var url1 = JsonConvert.DeserializeObject<JObject>(trainingRequest.DataSourceDetails)["Url"];
                    var bodyParams1 = JsonConvert.DeserializeObject<JObject>(trainingRequest.DataSourceDetails)["BodyParams"];

                    customSPAAIPayload = new CustomSPAAIPayload
                    {
                        UsecaseID = trainingRequest.UsecaseId,
                        AppId = trainingRequest.ApplicationId,
                        HttpMethod = CONSTANTS.POST,
                        AppUrl = url1,
                        InputParameters = JsonConvert.SerializeObject(bodyParams1),
                        DateColumn = "DateColumn"
                    };
                }
                else
                {
                    customSPAAIPayload = new CustomSPAAIPayload
                    {
                        UsecaseID = trainingRequest.UsecaseId,
                        AppId = trainingRequest.ApplicationId,
                        HttpMethod = CONSTANTS.POST,
                        AppUrl = CONSTANTS.Null,
                        InputParameters = CONSTANTS.Null,
                        DateColumn = "DateColumn"
                    };
                }
                parentDetail.Type = CONSTANTS.Null;
                parentDetail.Name = CONSTANTS.Null;
                fileUpload.Parent = parentDetail;
                _filepath = new Filepath();
                _filepath.fileList = "null";
                fileUpload.fileupload = _filepath;
                fileUpload.Customdetails = customSPAAIPayload;
                if (isCLusteringService)
                {
                    clusteringAPIModel.ParamArgs = fileUpload.ToJson();
                }
                else
                {
                    ingestRequest.SourceDetails = fileUpload.ToJson();
                }
            }
            //Forming the Paramargs for the training  or retraining END


            if (!string.IsNullOrEmpty(trainingRequest.DataSource) && trainingRequest.DataSource.ToUpper() == CONSTANTS.CustomDataApi.ToUpper())
            {
                if (isCLusteringService)
                {
                    ingestRequest.CorrelationId = clusteringAPIModel.CorrelationId;
                    ingestRequest.ClientId = clusteringAPIModel.ClientID;
                    ingestRequest.DeliveryconstructId = clusteringAPIModel.DCUID;
                }

                var fileParams = JsonConvert.DeserializeObject<CustomInputData>(trainingRequest.CustomDataSourceDetails);
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
                        throw new Exception(message.Message);
                    }
                }

                //Encrypting API related Information
                var Data = _encryptionDecryption.Encrypt(JsonConvert.SerializeObject(fileParams.Data));

                CustomSourceDTO fileUpload = new CustomSourceDTO
                {
                    CorrelationId = correlationId,
                    ClientUID = ingestRequest.ClientId,
                    E2EUID = CONSTANTS.Null,//E2EUID,
                    DeliveryConstructUId = ingestRequest.DeliveryconstructId,
                    Flag = CONSTANTS.Null,
                    mapping = CONSTANTS.Null,
                    mapping_flag = CONSTANTS.False,
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

                parentDetail.Type = CONSTANTS.Null;
                parentDetail.Name = CONSTANTS.Null;
                fileUpload.Parent = parentDetail;
                _filepath = new Filepath();
                _filepath.fileList = "null";
                fileUpload.fileupload = _filepath;
                if (isCLusteringService)
                {
                    clusteringAPIModel.ParamArgs = fileUpload.ToJson();
                }
                else
                {
                    ingestRequest.SourceDetails = fileUpload.ToJson();
                }

                CustomDataSourceModel CustomDataSource = new CustomDataSourceModel
                {
                    _id = Guid.NewGuid().ToString(),
                    CorrelationId = correlationId,
                    CustomDataPullType = CONSTANTS.CustomDataApi,
                    CustomSourceDetails = JsonConvert.SerializeObject(fileParams),
                    CreatedByUser = trainingRequest.UserId
                };
                _customDataService.InsertCustomDataSource(CustomDataSource, CONSTANTS.AICustomDataSource);

            }

            if (string.IsNullOrEmpty(trainingRequest.DataSource))
            {
                if (string.IsNullOrEmpty(trainingRequest.ResponseCallBackUrl))
                {
                    throw new Exception("Please provide the ResponseCallBackUrl");
                }
                else
                {
                    CustomSPAAIPayload customSPAAIPayload = null;
                    SPAAIInputParams inputParams = null;
                    CustomSPAAIFileUpload fileUpload = new CustomSPAAIFileUpload
                    {
                        CorrelationId = ingestRequest.CorrelationId,
                        ClientUID = ingestRequest.ClientId,
                        DeliveryConstructUId = ingestRequest.DeliveryconstructId,
                        Parent = parentDetail,
                        Flag = flag,//CONSTANTS.Null,
                        mapping = CONSTANTS.Null,
                        mapping_flag = CONSTANTS.False,//CONSTANTS.Null,
                        pad = CONSTANTS.Null,//string.Empty,
                        metric = CONSTANTS.Null,//string.Empty,
                        InstaMl = CONSTANTS.Null,// string.Empty,
                        fileupload = null,
                    };
                    AppIntegrationsCredentials appIntegrationsCredentials = new AppIntegrationsCredentials()
                    {
                        grant_type = appSettings.Grant_Type,
                        client_id = appSettings.clientId,
                        client_secret = appSettings.clientSecret,
                        resource = string.IsNullOrEmpty(resourceId) ? appSettings.resourceId : resourceId
                    };

                    AppIntegration appIntegrations = new AppIntegration()
                    {
                        ApplicationID = usecaseDetails.ApplicationId,
                        Credentials = JsonConvert.SerializeObject(appIntegrationsCredentials)
                    };
                    this.UpdateAppIntegration(appIntegrations);
                    Uri apiUri = new Uri(appSettings.myWizardAPIUrl);
                    string host = apiUri.GetLeftPart(UriPartial.Authority);
                    Uri apiUri1 = new Uri(usecaseDetails.SourceURL);
                    string urlPath = apiUri1.AbsolutePath;

                    var url = host + urlPath;//usecaseDetails.SourceURL;//"https://devtest-mywizardapi-si.accenture.com/bi/AmbulanceLaneSimilarStories";
                    //var bodyParams = JsonConvert.DeserializeObject<JObject>(trainingRequest.DataSourceDetails)["BodyParams"];
                    //var url = "https://devtest-mywizardapi-si.accenture.com/bi/AmbulanceLaneSimilarStories";
                    inputParams = new SPAAIInputParams
                    {

                        CorrelationId = correlationId,
                        StartDate = DateTime.Now.AddYears(-2).ToString(CONSTANTS.DateFormat),
                        EndDate = DateTime.Now.ToString(CONSTANTS.DateFormat),
                        TotalRecordCount = 0,
                        PageNumber = 1,
                        BatchSize = 5000
                    };
                    customSPAAIPayload = new CustomSPAAIPayload
                    {
                        UsecaseID = trainingRequest.UsecaseId,
                        AppId = trainingRequest.ApplicationId,
                        HttpMethod = CONSTANTS.POST,
                        AppUrl = url.ToString(),
                        InputParameters = inputParams.ToJson(),//BsonDocument.Parse(inputParams.ToJson()),
                        DateColumn = "DateColumn"//BsonDocument.Parse(trainingRequest.DataSourceDetails.BodyParams.ToString())//JObject.Parse(trainingRequest.DataSourceDetails.BodyParams.ToString()) //trainingRequest.DataSourceDetails.BodyParams.ToObject<object>()//JsonConvert.DeserializeObject<JObject>(fileUpload.Customdetails.InputParameters.ToString())//trainingRequest.DataSourceDetails.BodyParams
                    };

                    parentDetail.Type = CONSTANTS.Null;
                    parentDetail.Name = CONSTANTS.Null;
                    fileUpload.Parent = parentDetail;
                    _filepath = new Filepath();
                    _filepath.fileList = "null";
                    fileUpload.fileupload = _filepath;
                    fileUpload.Customdetails = customSPAAIPayload;
                    ingestRequest.SourceDetails = fileUpload.ToJson();
                    ingestRequest.ModelName = ingestRequest.ModelName + "_" + ingestRequest.CorrelationId;
                    ingestRequest.DataSource = "Scrumban";//added against 2145771
                    trainingRequest.ModelName = trainingRequest.ModelName + "_" + ingestRequest.CorrelationId;
                    trainingResponse.ModelName = trainingRequest.ModelName;
                }
            }
            //if (datasetUId == "undefined" || datasetUId == "null")
            //    ingestRequest.DataSetUId = datasetUId;

            //Python calls for the diff AI Services.
            if (isCLusteringService)
            {
                JObject payload = new JObject();
                //if (datasetUId == "undefined" || datasetUId == "null")
                //    clusteringAPIModel.DataSetUId = datasetUId;
                this.ClusteringIngestData(clusteringAPIModel);
                payload["CorrelationId"] = clusteringAPIModel.CorrelationId;
                payload["UniId"] = clusteringAPIModel.UniId;
                payload["UserId"] = clusteringAPIModel.UserId;
                payload["pageInfo"] = clusteringAPIModel.PageInfo;
                payload["IsDataUpload"] = true;
                if (clusteringAPIModel.retrain)
                {
                    payload["IsDataUpload"] = false;
                }
                payload["Publish_Case"] = true;
                baseUrl = appSettings.ClusteringPythonURL;
                apiPath = CONSTANTS.Clustering_ModelTraining;
                message = _clusteringAPI.RoutePOSTRequest(string.Empty, new Uri(baseUrl), apiPath, payload, clusteringAPIModel.CorrelationId, clusteringAPIModel.retrain, false);
                trainingResponse.CorrelationId = clusteringAPIModel.CorrelationId;
                trainingResponse.StatusMessage = message.IsSuccess ? "Success" : message.Message;
                trainingResponse.ModelStatus = message.IsSuccess ? trainingMessage : "Error";
                trainingResponse.ApplicationId = usecaseDetails.ApplicationId;
            }
            else if (isIntentEntity)
            {
                InsertAIServiceRequest(ingestRequest);
                CreateAICoreModel(ingestRequest.ClientId,
                                     ingestRequest.DeliveryconstructId,
                                     ingestRequest.ServiceId,
                                     ingestRequest.CorrelationId,
                                     ingestRequest.UniId,
                                     ingestRequest.ModelName,
                                     pyModelName,
                                     status,
                                    statusMessage,
                                     trainingRequest.UserId,
                                     ingestRequest.DataSource, ingestRequest.PageInfo, datasetUId, 0);//ingestRequest.DataSource added against 2145771
                JObject payload = new JObject();
                payload["client_id"] = trainingRequest.ClientId;
                payload["dc_id"] = trainingRequest.DeliveryConstructId;
                payload["correlation_id"] = correlationId;
                payload["ApplicationId"] = trainingRequest.ApplicationId;
                payload["DataSource"] = "Custom";
                payload["DataSourceDetails"] = JObject.Parse(appPayload.ToJson());
                IFormFileCollection fileCollection = null;
                string[] keys = new string[] { };
                message = RoutePOSTRequest(string.Empty, new Uri(baseUrl), apiPath,
                               fileCollection, payload, keys, service.IsReturnArray, correlationId);
                ////RoutePOSTRequest(string.Empty, new Uri(baseUrl), apiPath,
                //                payload, service.IsReturnArray);
                trainingResponse.CorrelationId = ingestRequest.CorrelationId;
                trainingResponse.StatusMessage = message.IsSuccess ? "Success" : message.Message;
                trainingResponse.ModelStatus = message.IsSuccess ? "Training Initiated" : "Error";
                trainingResponse.ApplicationId = usecaseDetails.ApplicationId;
            }
            else
            {
                if ((string.IsNullOrEmpty(trainingRequest.DataSource) && !string.IsNullOrEmpty(trainingRequest.ResponseCallBackUrl)) || trainingRequest.DataSource == "Custom" || trainingRequest.DataSource == "Entity" || trainingRequest.DataSource == "File" || trainingRequest.DataSource == "Phoenix" || trainingRequest.DataSource.Contains("DataSet") || (!string.IsNullOrEmpty(trainingRequest.DataSource) && (trainingRequest.DataSource.ToUpper() == CONSTANTS.CustomDbQuery.ToUpper() || trainingRequest.DataSource.ToUpper() == CONSTANTS.CustomDataApi.ToUpper())))
                {
                    if (!string.IsNullOrEmpty(trainingRequest.DataSource) && trainingRequest.DataSource.ToUpper() == CONSTANTS.CustomDbQuery.ToUpper())
                    {
                        //var CustomQueryParams = JsonConvert.DeserializeObject<CustomDataInputParams>(CustomSourceItems);
                        CustomDataSourceModel CustomDataSource = new CustomDataSourceModel
                        {
                            _id = Guid.NewGuid().ToString(),
                            CorrelationId = correlationId,
                            CustomDataPullType = CONSTANTS.CustomDbQuery,
                            CustomSourceDetails = Convert.ToString(CustomSourceItems),
                            CreatedByUser = trainingRequest.UserId
                        };
                        _customDataService.InsertCustomDataSource(CustomDataSource, CONSTANTS.AICustomDataSource);
                    }
                    ingestRequest.ResponsecallbackUrl = trainingRequest.ResponseCallBackUrl;
                    ingestRequest.Status = "N";
                    InsertAIServiceRequest(ingestRequest);
                    AICoreModels aICoreModels = new AICoreModels();
                    aICoreModels.ClientId = ingestRequest.ClientId;
                    aICoreModels.DeliveryConstructId = ingestRequest.DeliveryconstructId;
                    aICoreModels.ServiceId = ingestRequest.ServiceId;
                    aICoreModels.CorrelationId = ingestRequest.CorrelationId;
                    aICoreModels.UniId = ingestRequest.UniId;
                    aICoreModels.ModelName = ingestRequest.ModelName;
                    aICoreModels.PythonModelName = string.Empty;
                    aICoreModels.ModelStatus = status;
                    aICoreModels.StatusMessage = statusMessage;
                    aICoreModels.CreatedBy = ingestRequest.CreatedByUser;
                    aICoreModels.DataSource = ingestRequest.DataSource;
                    aICoreModels.UsecaseId = trainingRequest.UsecaseId;
                    aICoreModels.ApplicationId = trainingRequest.ApplicationId;
                    aICoreModels.ResponsecallbackUrl = ingestRequest.ResponsecallbackUrl;

                    if (usecaseDetails.ApplicationName == "SPA")
                    {
                        aICoreModels.SendNotification = "True"; // for SPAusecase
                        aICoreModels.IsNotificationSent = "False"; //for SPAusecase
                    }
                    //if (datasetUId == "undefined" || datasetUId == "null")
                    //    aICoreModels.DataSetUId = datasetUId;
                    aICoreModels.MaxDataPull = ingestRequest.MaxDataPull;
                    aICoreModels.IsCarryOutRetraining = iscarryOutRetraining;
                    aICoreModels.IsOnline = isOnline;
                    aICoreModels.IsOffline = isOffline;
                    aICoreModels.Retraining = aIModelFrequency;
                    aICoreModels.Training = aIModelFrequency;
                    aICoreModels.Prediction = aIModelFrequency;
                    if (iscarryOutRetraining && retraining != null && ((JContainer)retraining).HasValues)
                    {
                        aICoreModels.Retraining = new AIModelFrequency();
                        this.AssignFrequency(aICoreModels, aICoreModels.Retraining, retraining, CONSTANTS.Retraining);

                    }
                    if (aICoreModels.IsOffline)
                    {
                        if (training != null && ((JContainer)training).HasValues)
                        {
                            aICoreModels.Training = new AIModelFrequency();
                            this.AssignFrequency(aICoreModels, aICoreModels.Training, training, CONSTANTS.Training);
                        }
                        if (prediction != null && ((JContainer)prediction).HasValues)
                        {
                            aICoreModels.Prediction = new AIModelFrequency();
                            this.AssignFrequency(aICoreModels, aICoreModels.Prediction, prediction, CONSTANTS.Prediction);
                        }
                    }

                    CreateAICoreModel(aICoreModels, ingestRequest.PageInfo);

                    //apiPath = apiPath + "?" + "correlationId=" + correlationId + "&userId=" + trainingRequest.UserId + "&pageInfo=" + ingestRequest.PageInfo + "&UniqueId=" + ingestRequest.UniId;
                    //message = RouteGETRequest(string.Empty, new Uri(baseUrl), apiPath, false);
                }
                trainingResponse.CorrelationId = ingestRequest.CorrelationId;
                //trainingResponse.StatusMessage = message.IsSuccess ? "Success" : message.Message;
                //trainingResponse.ModelStatus = message.IsSuccess ? "Training Initiated" : "Error";
                trainingResponse.StatusMessage = "Success";
                trainingResponse.ModelStatus = "Training Initiated";
                trainingResponse.ApplicationId = usecaseDetails.ApplicationId;
                //Log to DB
                //if (!string.IsNullOrEmpty(trainingRequest.ApplicationId))
                //{
                //    auditTrailLog.ApplicationName = usecaseDetails.ApplicationName;
                //    auditTrailLog.ApplicationID = trainingRequest.ApplicationId;
                //    auditTrailLog.ClientId = trainingRequest.ClientId;
                //    auditTrailLog.DCID = trainingRequest.DeliveryConstructId;
                //    auditTrailLog.UseCaseId = trainingRequest.UsecaseId;
                //    auditTrailLog.CreatedBy = trainingRequest.UserId;
                //    auditTrailLog.ProcessName = CONSTANTS.TrainingName;
                //    auditTrailLog.UsageType = CONSTANTS.INFOUsage;
                //    auditTrailLog.BaseAddress = ingestRequest.ResponsecallbackUrl;
                //    auditTrailLog.CorrelationId = ingestRequest.CorrelationId;
                //    CommonUtility.AuditTrailLog(auditTrailLog, configSetting);
                //}
                //if (!string.IsNullOrEmpty(trainingRequest.ResponseCallBackUrl))
                //{
                //    IngrainResponseData CallBackResponse = new IngrainResponseData
                //    {
                //        CorrelationId = ingestRequest.CorrelationId,
                //        Status = message.IsSuccess ? "Initiated" : CONSTANTS.ErrorMessage,
                //        Message = trainingResponse.ModelStatus,
                //        ErrorMessage = message.Message
                //    };
                //    string callbackResonse = CommonUtility.CallbackResponse(CallBackResponse, usecaseDetails.ApplicationName, trainingRequest.ResponseCallBackUrl, trainingRequest.ClientId, trainingRequest.DeliveryConstructId, trainingRequest.ApplicationId, trainingRequest.UsecaseId, ingestRequest.UniId, ingestRequest.CreatedByUser, configSetting, _encryptionDecryption);
                //}
            }
            //Python calls for diff AInService END
            //Python calls for diff AInService END

            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(TrainAIServiceModel), CONSTANTS.END, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), trainingRequest.ApplicationId, "", trainingRequest.ClientId, trainingRequest.DeliveryConstructId);
            return trainingResponse;
        }

        // Initiate training for bulk data
        public AIGENERICSERVICE.BulkTraining.BulkTrainingResponse TrainAIServiceModelBulkTrain(HttpContext httpContext)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(TrainAIServiceModelBulkTrain), CONSTANTS.START, string.Empty, string.Empty, string.Empty, string.Empty);
            //validation 
            AIGENERICSERVICE.BulkTraining.BulkTrainingResponse trainingResponseError = new AIGENERICSERVICE.BulkTraining.BulkTrainingResponse();

            AIGENERICSERVICE.BulkTraining.BulkTrainingRequest trainingRequest = new AIGENERICSERVICE.BulkTraining.BulkTrainingRequest();
            IFormCollection collection = httpContext.Request.Form;

            trainingRequest.ClientID = collection["ClientId"];
            trainingRequest.DeliveryConstructID = collection["DeliveryConstructId"];
            trainingRequest.ServiceID = collection["ServiceId"];
            trainingRequest.ApplicationID = collection["ApplicationId"];
            trainingRequest.UsecaseID = collection["UsecaseId"];
            trainingRequest.ModelName = collection["ModelName"];
            trainingRequest.UserID = collection["UserId"];
            trainingRequest.DataSource = collection["DataSource"];
            if (!CommonUtility.GetValidUser(Convert.ToString(collection["UserId"])))
            {
                throw new Exception("UserName/UserId is Invalid");
            }
            try
            {
                trainingRequest.DataSourceDetails = JsonConvert.DeserializeObject<AIGENERICSERVICE.BulkTraining.TODataSourceDetails>(collection["DataSourceDetails"]);
                trainingRequest.ConfigurationDetails = JsonConvert.DeserializeObject<AIGENERICSERVICE.BulkTraining.TOConfigurationDetails>(collection["ConfigurationDetails"]);
                if (Convert.ToDateTime(trainingRequest.DataSourceDetails.StartDate) > Convert.ToDateTime(trainingRequest.DataSourceDetails.EndDate))
                {
                    trainingResponseError.ApplicationId = trainingRequest.ApplicationID;
                    trainingResponseError.ClientId = trainingRequest.ClientID;
                    trainingResponseError.DeliveryConstructId = trainingRequest.DeliveryConstructID;
                    trainingResponseError.ServiceId = trainingRequest.ServiceID;
                    trainingResponseError.UsecaseId = trainingRequest.UsecaseID;
                    trainingResponseError.ModelName = trainingRequest.ModelName;
                    trainingResponseError.UserId = trainingRequest.UserID;
                    trainingResponseError.ModelStatus = "Training is not initiated";
                    trainingResponseError.StatusMessage = "Invalid dates provided as input. Please provide EndDate greater than StartDate";
                    return trainingResponseError;
                }
            }
            catch (Exception ex)
            {
                trainingResponseError.ApplicationId = trainingRequest.ApplicationID;
                trainingResponseError.ClientId = trainingRequest.ClientID;
                trainingResponseError.DeliveryConstructId = trainingRequest.DeliveryConstructID;
                trainingResponseError.ServiceId = trainingRequest.ServiceID;
                trainingResponseError.UsecaseId = trainingRequest.UsecaseID;
                trainingResponseError.ModelName = trainingRequest.ModelName;
                trainingResponseError.UserId = trainingRequest.UserID;
                trainingResponseError.ModelStatus = CONSTANTS.IncompleteTrainingRequest;
                trainingResponseError.StatusMessage = "Incorrect DataSourceDetails/ConfigurationDetails";
                return trainingResponseError;
            }
            trainingRequest.ResponseCallBackUrl = collection["ResponseCallbackUrl"];

            trainingResponseError = AssignValues(trainingRequest, out bool validationStatus);
            if (!validationStatus)
                return trainingResponseError;

            USECASE.UsecaseDetails usecaseDetails = GetUsecaseDetails(trainingRequest.UsecaseID);
            if (string.IsNullOrEmpty(usecaseDetails.UsecaseId) || string.IsNullOrWhiteSpace(usecaseDetails.UsecaseId))
            {
                trainingResponseError.ApplicationId = trainingRequest.ApplicationID;
                trainingResponseError.ClientId = trainingRequest.ClientID;
                trainingResponseError.DeliveryConstructId = trainingRequest.DeliveryConstructID;
                trainingResponseError.ServiceId = trainingRequest.ServiceID;
                trainingResponseError.UsecaseId = trainingRequest.UsecaseID;
                trainingResponseError.ModelName = trainingRequest.ModelName;
                trainingResponseError.UserId = trainingRequest.UserID;
                trainingResponseError.ModelStatus = CONSTANTS.IncompleteTrainingRequest;
                trainingResponseError.StatusMessage = CONSTANTS.UsecaseNotAvailable;
                return trainingResponseError;
            }
            string scoreUniqueName = string.Empty;
            var aIRequestStatus = _database.GetCollection<BsonDocument>(CONSTANTS.AIServiceRequestStatus);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, usecaseDetails.CorrelationId) & Builders<BsonDocument>.Filter.Eq(CONSTANTS.PageInfo, CONSTANTS.TrainModel);
            var Projection = Builders<BsonDocument>.Projection.Exclude("_id").Include(CONSTANTS.ScoreUniqueName);
            var result = aIRequestStatus.Find(filter).Project<BsonDocument>(Projection).ToList();
            if (result.Count > 0)
            {
                scoreUniqueName = "defectuid";
                if (result[0].Contains("ScoreUniqueName"))
                    scoreUniqueName = result[0][CONSTANTS.ScoreUniqueName].ToString();

            }

            string correlationId = Guid.NewGuid().ToString();

            trainingResponseError = ValidateValues(trainingRequest, out bool ValuevalidationStatus);
            if (!ValuevalidationStatus)
                return trainingResponseError;

            var AppIntegCollection = _database.GetCollection<AppIntegration>(CONSTANTS.AppIntegration);
            var filterBuilder = Builders<AppIntegration>.Filter;
            var AppFilter = filterBuilder.Eq(CONSTANTS.ApplicationID, trainingRequest.ApplicationID);
            var projection = Builders<AppIntegration>.Projection.Exclude(CONSTANTS.Id);
            var IsApplicationExist = AppIntegCollection.Find(AppFilter).Project<AppIntegration>(projection).FirstOrDefault();
            if (IsApplicationExist != null)
            {
                if (UpdateTokenInAppIntegration(trainingRequest.ApplicationID) != CONSTANTS.Success || UpdateUseCaseTemplate(trainingRequest.ApplicationID, usecaseDetails) != CONSTANTS.Success)
                {
                    trainingResponseError.ApplicationId = trainingRequest.ApplicationID;
                    trainingResponseError.ClientId = trainingRequest.ClientID;
                    trainingResponseError.DeliveryConstructId = trainingRequest.DeliveryConstructID;
                    trainingResponseError.ServiceId = trainingRequest.ServiceID;
                    trainingResponseError.UsecaseId = trainingRequest.UsecaseID;
                    trainingResponseError.ModelName = trainingRequest.ModelName;
                    trainingResponseError.UserId = trainingRequest.UserID;
                    trainingResponseError.ModelStatus = CONSTANTS.IncompleteTrainingRequest;
                    trainingResponseError.StatusMessage = "Error Updating AppIntegration,PublicTemplatingMapping";
                    return trainingResponseError;
                }
            }
            else
            {
                trainingResponseError.ApplicationId = trainingRequest.ApplicationID;
                trainingResponseError.ClientId = trainingRequest.ClientID;
                trainingResponseError.DeliveryConstructId = trainingRequest.DeliveryConstructID;
                trainingResponseError.ServiceId = trainingRequest.ServiceID;
                trainingResponseError.UsecaseId = trainingRequest.UsecaseID;
                trainingResponseError.ModelName = trainingRequest.ModelName;
                trainingResponseError.UserId = trainingRequest.UserID;
                trainingResponseError.ModelStatus = CONSTANTS.IncompleteTrainingRequest;
                trainingResponseError.StatusMessage = CONSTANTS.IsApplicationExist;
                return trainingResponseError;
            }


            TOAI_InputParams inputParams = new TOAI_InputParams
            {
                CorrelationId = correlationId,
                StartDate = trainingRequest.DataSourceDetails.StartDate,
                EndDate = trainingRequest.DataSourceDetails.EndDate,
                TotalRecordCount = 0,
                PageNumber = 1,
                BatchSize = 5000,
                ReleaseID = trainingRequest.DataSourceDetails.ReleaseID
            };

            Uri apiUri = new Uri(appSettings.myWizardAPIUrl);
            string host = apiUri.GetLeftPart(UriPartial.Authority);
            Uri apiUri1 = new Uri(usecaseDetails.SourceURL);
            string urlPath = apiUri1.AbsolutePath;
            var url = host + urlPath;

            CustomSPAAIPayload customSPAAIPayload = new CustomSPAAIPayload
            {
                AppId = trainingRequest.ApplicationID,
                HttpMethod = CONSTANTS.POST,
                AppUrl = url.ToString(),
                InputParameters = inputParams.ToJson(),
                DateColumn = CONSTANTS.Null,
                UsecaseID = trainingRequest.UsecaseID,
            };
            ParentFile parentDetail = new ParentFile();
            parentDetail.Type = CONSTANTS.Null;
            parentDetail.Name = CONSTANTS.Null;

            _filepath = new Filepath();
            _filepath.fileList = CONSTANTS.Null;

            CustomSPAAIFileUpload fileUpload = new CustomSPAAIFileUpload
            {
                CorrelationId = correlationId,
                ClientUID = trainingRequest.ClientID,
                DeliveryConstructUId = trainingRequest.DeliveryConstructID,
                Parent = parentDetail,
                Flag = CONSTANTS.Null,
                mapping = CONSTANTS.Null,
                mapping_flag = CONSTANTS.False,
                pad = CONSTANTS.Null,
                metric = CONSTANTS.Null,
                InstaMl = CONSTANTS.Null,
                fileupload = _filepath,
                Customdetails = customSPAAIPayload
            };

            AIServiceRequestStatus ingestRequest = new AIServiceRequestStatus
            {
                CorrelationId = correlationId,
                ServiceId = trainingRequest.ServiceID,
                UniId = Guid.NewGuid().ToString(),
                PageInfo = CONSTANTS.IngestTrain,
                ClientId = trainingRequest.ClientID,
                DeliveryconstructId = trainingRequest.DeliveryConstructID,
                Status = "I",
                ModelName = trainingRequest.ModelName + "_" + correlationId,
                SourceDetails = fileUpload.ToJson(),
                DataSource = trainingRequest.DataSource,
                CreatedByUser = trainingRequest.UserID,
                CreatedOn = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                ModifiedByUser = trainingRequest.UserID,
                ModifiedOn = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                ResponsecallbackUrl = trainingRequest.ResponseCallBackUrl,
                UsecaseId = trainingRequest.UsecaseID,
                ApplicationId = trainingRequest.ApplicationID,
                ScoreUniqueName = scoreUniqueName,
                Threshold_TopnRecords = trainingRequest.ConfigurationDetails.Threshold_TopnRecords,
                StopWords = trainingRequest.ConfigurationDetails.StopWords
            };

            InsertAIServiceRequest(ingestRequest);

            AICoreModels aICoreModels = new AICoreModels
            {
                ClientId = trainingRequest.ClientID,
                DeliveryConstructId = trainingRequest.DeliveryConstructID,
                ServiceId = trainingRequest.ServiceID,
                CorrelationId = correlationId,
                UniId = ingestRequest.UniId,
                ModelName = trainingRequest.ModelName + "_" + correlationId,
                PythonModelName = string.Empty,
                ModelStatus = "InProgress",
                StatusMessage = "Training is in progress",
                CreatedBy = ingestRequest.CreatedByUser,
                DataSource = trainingRequest.DataSource,
                UsecaseId = trainingRequest.UsecaseID,
                ApplicationId = trainingRequest.ApplicationID,
                ResponsecallbackUrl = trainingRequest.ResponseCallBackUrl
            };

            CreateAICoreModel(aICoreModels, ingestRequest.PageInfo);

            Service service = GetAiCoreServiceDetails(trainingRequest.ServiceID);

            string baseUrl = string.Empty;
            string apiPath = string.Empty;
            var serviceResponse = new MethodReturn<object>();
            if (!string.IsNullOrWhiteSpace(service.PrimaryTrainApiUrl))
            {
                baseUrl = appSettings.AICorePythonURL;
                apiPath = service.PrimaryTrainApiUrl;
            }
            string encrypteduser = trainingRequest.UserID;
            if (appSettings.isForAllData)
            {
                if (!string.IsNullOrEmpty(Convert.ToString(trainingRequest.UserID)))
                    encrypteduser = _encryptionDecryption.Encrypt(Convert.ToString(trainingRequest.UserID));
            }
            apiPath = apiPath + "?" + "correlationId=" + correlationId + "&userId=" + encrypteduser + "&pageInfo=" + ingestRequest.PageInfo + "&UniqueId=" + ingestRequest.UniId;
            serviceResponse = RouteGETRequest(string.Empty, new Uri(baseUrl), apiPath, service.IsReturnArray);

            AIGENERICSERVICE.BulkTraining.BulkTrainingResponse trainingResponse
                = new AIGENERICSERVICE.BulkTraining.BulkTrainingResponse
                {
                    ApplicationId = trainingRequest.ApplicationID,
                    ClientId = trainingRequest.ClientID,
                    DeliveryConstructId = trainingRequest.DeliveryConstructID,
                    ServiceId = trainingRequest.ServiceID,
                    UsecaseId = trainingRequest.UsecaseID,
                    ModelName = trainingRequest.ModelName,
                    UserId = trainingRequest.UserID,
                    CorrelationId = correlationId,
                    UniqueId = ingestRequest.UniId,
                    ModelStatus = CONSTANTS.TrainingInitiated,
                    StatusMessage = serviceResponse.Message
                };
            if (serviceResponse.IsSuccess)
                trainingResponse.ModelStatus = CONSTANTS.TrainingInitiated;
            else
                trainingResponse.ModelStatus = CONSTANTS.TrainingFailed;
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(TrainAIServiceModelBulkTrain), CONSTANTS.END,
                string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), trainingRequest.ApplicationID, string.Empty, trainingRequest.ClientID, trainingRequest.DeliveryConstructID);
            return trainingResponse;
        }
        private AIGENERICSERVICE.BulkTraining.BulkTrainingResponse AssignValues(AIGENERICSERVICE.BulkTraining.BulkTrainingRequest trainingRequest, out bool validationStatus)
        {
            validationStatus = true;
            //validation 
            AIGENERICSERVICE.BulkTraining.BulkTrainingResponse trainingResponseError = new AIGENERICSERVICE.BulkTraining.BulkTrainingResponse();
            trainingResponseError.ApplicationId = trainingRequest.ApplicationID;
            trainingResponseError.ClientId = trainingRequest.ClientID;
            trainingResponseError.DeliveryConstructId = trainingRequest.DeliveryConstructID;
            trainingResponseError.ServiceId = trainingRequest.ServiceID;
            trainingResponseError.UsecaseId = trainingRequest.UsecaseID;
            trainingResponseError.ModelName = trainingRequest.ModelName;
            trainingResponseError.UserId = trainingRequest.UserID;
            if ((string.IsNullOrEmpty(Convert.ToString(trainingRequest.ClientID)) || string.IsNullOrEmpty(Convert.ToString(trainingRequest.DeliveryConstructID))
               || string.IsNullOrEmpty(Convert.ToString(trainingRequest.ServiceID)) || string.IsNullOrEmpty(Convert.ToString(trainingRequest.ApplicationID))
               || string.IsNullOrEmpty(Convert.ToString(trainingRequest.UsecaseID)) || string.IsNullOrEmpty(Convert.ToString(trainingRequest.ModelName))
               || string.IsNullOrEmpty(Convert.ToString(trainingRequest.UserID)) || string.IsNullOrEmpty(Convert.ToString(trainingRequest.DataSource))
               || string.IsNullOrEmpty(Convert.ToString(trainingRequest.DataSourceDetails)) || string.IsNullOrEmpty(Convert.ToString(trainingRequest.ConfigurationDetails))
               || string.IsNullOrEmpty(Convert.ToString(trainingRequest.DataSourceDetails.ReleaseID)) || string.IsNullOrEmpty(Convert.ToString(trainingRequest.DataSourceDetails.StartDate)) || string.IsNullOrEmpty(Convert.ToString(trainingRequest.DataSourceDetails.EndDate)))
               ||
               (string.IsNullOrWhiteSpace(Convert.ToString(trainingRequest.ClientID)) || string.IsNullOrWhiteSpace(Convert.ToString(trainingRequest.DeliveryConstructID))
               || string.IsNullOrWhiteSpace(Convert.ToString(trainingRequest.ServiceID)) || string.IsNullOrWhiteSpace(Convert.ToString(trainingRequest.ApplicationID))
               || string.IsNullOrWhiteSpace(Convert.ToString(trainingRequest.UsecaseID)) || string.IsNullOrWhiteSpace(Convert.ToString(trainingRequest.ModelName))
               || string.IsNullOrWhiteSpace(Convert.ToString(trainingRequest.UserID)) || string.IsNullOrWhiteSpace(Convert.ToString(trainingRequest.DataSource))
               || string.IsNullOrWhiteSpace(Convert.ToString(trainingRequest.DataSourceDetails)) || string.IsNullOrWhiteSpace(Convert.ToString(trainingRequest.ConfigurationDetails))
               || string.IsNullOrWhiteSpace(Convert.ToString(trainingRequest.DataSourceDetails.ReleaseID)) || string.IsNullOrWhiteSpace(Convert.ToString(trainingRequest.DataSourceDetails.StartDate)) || string.IsNullOrWhiteSpace(Convert.ToString(trainingRequest.DataSourceDetails.EndDate))))
            {
                validationStatus = false;
                trainingResponseError.ModelStatus = CONSTANTS.IncompleteTrainingRequest;
                trainingResponseError.StatusMessage = Resource.IngrainResx.InputEmpty;
            }
            else if (Convert.ToString(trainingRequest.ClientID) == CONSTANTS.undefined || Convert.ToString(trainingRequest.DeliveryConstructID) == CONSTANTS.undefined
                || Convert.ToString(trainingRequest.ServiceID) == CONSTANTS.undefined || Convert.ToString(trainingRequest.ApplicationID) == CONSTANTS.undefined
                || Convert.ToString(trainingRequest.UsecaseID) == CONSTANTS.undefined || Convert.ToString(trainingRequest.ModelName) == CONSTANTS.undefined
                || Convert.ToString(trainingRequest.UserID) == CONSTANTS.undefined || Convert.ToString(trainingRequest.DataSource) == CONSTANTS.undefined
                || Convert.ToString(trainingRequest.DataSourceDetails) == CONSTANTS.undefined || Convert.ToString(trainingRequest.ConfigurationDetails) == CONSTANTS.undefined
                || Convert.ToString(trainingRequest.DataSourceDetails.ReleaseID) == CONSTANTS.undefined || Convert.ToString(trainingRequest.DataSourceDetails.StartDate) == CONSTANTS.undefined || Convert.ToString(trainingRequest.DataSourceDetails.EndDate) == CONSTANTS.undefined)
            {
                validationStatus = false;
                trainingResponseError.ModelStatus = CONSTANTS.IncompleteTrainingRequest;
                trainingResponseError.StatusMessage = CONSTANTS.InutFieldsUndefined;
            }
            else if (Convert.ToString(trainingRequest.ClientID) == CONSTANTS.Null || Convert.ToString(trainingRequest.DeliveryConstructID) == CONSTANTS.Null
              || Convert.ToString(trainingRequest.ServiceID) == CONSTANTS.Null || Convert.ToString(trainingRequest.ApplicationID) == CONSTANTS.Null
              || Convert.ToString(trainingRequest.UsecaseID) == CONSTANTS.Null || Convert.ToString(trainingRequest.ModelName) == CONSTANTS.Null
              || Convert.ToString(trainingRequest.UserID) == CONSTANTS.Null || Convert.ToString(trainingRequest.DataSource) == CONSTANTS.Null
              || Convert.ToString(trainingRequest.DataSourceDetails) == CONSTANTS.Null || Convert.ToString(trainingRequest.ConfigurationDetails) == CONSTANTS.Null
              || Convert.ToString(trainingRequest.DataSourceDetails.ReleaseID) == CONSTANTS.Null || Convert.ToString(trainingRequest.DataSourceDetails.StartDate) == CONSTANTS.Null || Convert.ToString(trainingRequest.DataSourceDetails.EndDate) == CONSTANTS.Null)
            {
                validationStatus = false;
                trainingResponseError.ModelStatus = CONSTANTS.IncompleteTrainingRequest;
                trainingResponseError.StatusMessage = Resource.IngrainResx.InputFieldsAreNull;
            }
            return trainingResponseError;
        }

        private AIGENERICSERVICE.BulkTraining.BulkTrainingResponse ValidateValues(AIGENERICSERVICE.BulkTraining.BulkTrainingRequest trainingRequest, out bool ValuevalidationStatus)
        {
            ValuevalidationStatus = true;
            AIGENERICSERVICE.BulkTraining.BulkTrainingResponse trainingResponseError = new AIGENERICSERVICE.BulkTraining.BulkTrainingResponse();
            trainingResponseError.ApplicationId = trainingRequest.ApplicationID;
            trainingResponseError.ClientId = trainingRequest.ClientID;
            trainingResponseError.DeliveryConstructId = trainingRequest.DeliveryConstructID;
            trainingResponseError.ServiceId = trainingRequest.ServiceID;
            trainingResponseError.UsecaseId = trainingRequest.UsecaseID;
            trainingResponseError.ModelName = trainingRequest.ModelName;
            trainingResponseError.UserId = trainingRequest.UserID;
            if (string.IsNullOrWhiteSpace(trainingRequest.DataSourceDetails.ReleaseID[0]))
            {
                ValuevalidationStatus = false;
                trainingResponseError.ModelStatus = CONSTANTS.InvalidInput;
                trainingResponseError.StatusMessage = CONSTANTS.InvalidReleaseID;
            }

            DateTime parsed;
            bool StartDatevalid = DateTime.TryParseExact(trainingRequest.DataSourceDetails.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed);
            bool EndDatevalid = DateTime.TryParseExact(trainingRequest.DataSourceDetails.EndDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed);
            if (!StartDatevalid || !EndDatevalid)
            {
                ValuevalidationStatus = false;
                trainingResponseError.ModelStatus = CONSTANTS.InvalidInput;
                trainingResponseError.StatusMessage = CONSTANTS.DateFormat;
            }
            //if (Convert.ToDateTime(trainingRequest.DataSourceDetails.EndDate) > DateTime.Now)
            //{
            //    ValuevalidationStatus = false;
            //    trainingResponseError.ModelStatus = CONSTANTS.InvalidInput;
            //    trainingResponseError.StatusMessage = CONSTANTS.EndDateGreaterThanToday;
            //}

            if (trainingRequest.ConfigurationDetails.Threshold_TopnRecords.key == CONSTANTS.Threshold || trainingRequest.ConfigurationDetails.Threshold_TopnRecords.key == CONSTANTS.Top_n)
            {
                if (trainingRequest.ConfigurationDetails.Threshold_TopnRecords.key == CONSTANTS.Threshold && (Convert.ToDecimal(trainingRequest.ConfigurationDetails.Threshold_TopnRecords.value) < 0 || Convert.ToDecimal(trainingRequest.ConfigurationDetails.Threshold_TopnRecords.value) > 1))
                {
                    ValuevalidationStatus = false;
                    trainingResponseError.ModelStatus = CONSTANTS.InvalidInput;
                    trainingResponseError.StatusMessage = CONSTANTS.InvalidThresholdOrTop_n;
                }
                else if (trainingRequest.ConfigurationDetails.Threshold_TopnRecords.key == CONSTANTS.Top_n && (trainingRequest.ConfigurationDetails.Threshold_TopnRecords.value < 1 || Convert.ToInt64(trainingRequest.ConfigurationDetails.Threshold_TopnRecords.value) > 25000))
                {
                    ValuevalidationStatus = false;
                    trainingResponseError.ModelStatus = CONSTANTS.InvalidInput;
                    trainingResponseError.StatusMessage = CONSTANTS.InvalidThresholdOrTop_n;
                }
            }
            else
            {
                ValuevalidationStatus = false;
                trainingResponseError.ModelStatus = CONSTANTS.InvalidInput;
                trainingResponseError.StatusMessage = CONSTANTS.InvalidInput;
            }
            return trainingResponseError;
        }
        public string UpdateAppIntegration(AppIntegration appIntegrations)
        {
            var collection = _database.GetCollection<AppIntegration>(CONSTANTS.AppIntegration);
            var filter = Builders<AppIntegration>.Filter.Where(x => x.ApplicationID == appIntegrations.ApplicationID);
            var Projection = Builders<AppIntegration>.Projection.Exclude("_id");
            var result = collection.Find(filter).Project<AppIntegration>(Projection).FirstOrDefault();

            var status = CONSTANTS.NoRecordsFound;
            if (result != null)
            {
                var update = Builders<AppIntegration>.Update
                    .Set(x => x.Authentication, appSettings.authProvider)
                    .Set(x => x.TokenGenerationURL, _encryptionDecryption.Encrypt(appSettings.token_Url))
                    .Set(x => x.Credentials, (IEnumerable)(_encryptionDecryption.Encrypt(appIntegrations.Credentials)))
                    .Set(x => x.ModifiedByUser, _encryptionDecryption.Encrypt(Convert.ToString(appSettings.username)))
                    .Set(x => x.ModifiedOn, DateTime.UtcNow.ToString());
                collection.UpdateOne(filter, update);
                status = CONSTANTS.Success;
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(UpdateAppIntegration), "UpdateAppIntegration - status : " + status
                , appIntegrations.ApplicationID, string.Empty, appIntegrations.clientUId, appIntegrations.deliveryConstructUID);
            return status;
        }

        public string InsertAIServiceRequest(AIServiceRequestStatus aIServiceRequestStatus)
        {
            if (appSettings.isForAllData)
            {
                if (!string.IsNullOrEmpty(Convert.ToString(aIServiceRequestStatus.CreatedByUser)))
                    aIServiceRequestStatus.CreatedByUser = _encryptionDecryption.Encrypt(Convert.ToString(aIServiceRequestStatus.CreatedByUser));
                if (!string.IsNullOrEmpty(Convert.ToString(aIServiceRequestStatus.ModifiedByUser)))
                    aIServiceRequestStatus.ModifiedByUser = _encryptionDecryption.Encrypt(Convert.ToString(aIServiceRequestStatus.ModifiedByUser));
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(InsertAIServiceRequest), CONSTANTS.START
                , string.IsNullOrEmpty(aIServiceRequestStatus.CorrelationId) ? default(Guid) : new Guid(aIServiceRequestStatus.CorrelationId), aIServiceRequestStatus.ApplicationId, string.Empty, aIServiceRequestStatus.ClientId, aIServiceRequestStatus.DeliveryconstructId);
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
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(InsertAIServiceRequest), CONSTANTS.END, aIServiceRequestStatus.ApplicationId, string.Empty, aIServiceRequestStatus.ClientId, aIServiceRequestStatus.DeliveryconstructId);
            return "Success";

        }
        public void InsertAIServicePredictionRequest(AICoreModels aICoreModels)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(InsertAIServicePredictionRequest), CONSTANTS.START
                , string.IsNullOrEmpty(aICoreModels.CorrelationId) ? default(Guid) : new Guid(aICoreModels.CorrelationId), aICoreModels.ApplicationId, string.Empty, aICoreModels.ClientId, aICoreModels.DeliveryConstructId);
            AIGENERICSERVICE.BulkTraining.AIServiceStatusResponse aIServiceStatusResponse = new AIGENERICSERVICE.BulkTraining.AIServiceStatusResponse();
            aIServiceStatusResponse.CorrelationId = aICoreModels.CorrelationId;
            aIServiceStatusResponse.ClientId = aICoreModels.ClientId;
            aIServiceStatusResponse.DeliveryconstructId = aICoreModels.DeliveryConstructId;
            aIServiceStatusResponse.ModelName = aICoreModels.ModelName;
            aIServiceStatusResponse.PageInfo = CONSTANTS.PredictionStatus;
            aIServiceStatusResponse.UniqueId = aICoreModels.UniId;
            aIServiceStatusResponse.UsecaseId = aICoreModels.UsecaseId;
            aIServiceStatusResponse.Status = string.Empty;
            aIServiceStatusResponse.ServiceId = aICoreModels.ServiceId;
            aIServiceStatusResponse.Progress = string.Empty;
            aIServiceStatusResponse.Message = string.Empty;
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.AIServicesPrediction);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, aIServiceStatusResponse.CorrelationId) & Builders<BsonDocument>.Filter.Eq(CONSTANTS.UniqueId, aIServiceStatusResponse.UniqueId) & Builders<BsonDocument>.Filter.Eq(CONSTANTS.PageInfo, CONSTANTS.PredictionStatus);
            var result = collection.Find(filter).ToList();
            var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(aIServiceStatusResponse);
            var insertDocument = BsonSerializer.Deserialize<BsonDocument>(jsonData);
            if (result.Count > 0)
            {
                collection.DeleteOne(filter);
            }
            Thread.Sleep(1000);
            collection.InsertOneAsync(insertDocument);
            Thread.Sleep(2000);
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(InsertAIServicePredictionRequest), CONSTANTS.END
                , aICoreModels.ApplicationId, string.Empty, aICoreModels.ClientId, aICoreModels.DeliveryConstructId);
        }



        public object SaveUsecase(USECASE.UsecaseDetails usecaseDetails)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(SaveUsecase), CONSTANTS.START, string.IsNullOrEmpty(usecaseDetails.CorrelationId) ? default(Guid) : new Guid(usecaseDetails.CorrelationId), usecaseDetails.ApplicationId, string.Empty, string.Empty, string.Empty);
            usecaseDetails.UsecaseId = Guid.NewGuid().ToString();

            bool iscarryOutRetraining = Convert.ToBoolean(usecaseDetails.IsCarryOutRetraining);
            bool isOnline = Convert.ToBoolean(usecaseDetails.IsOnline);
            bool isOffline = Convert.ToBoolean(usecaseDetails.IsOffline);
            JObject training = new JObject();
            JObject prediction = new JObject();
            JObject retraining = new JObject();

            if (Convert.ToBoolean(usecaseDetails.IsCarryOutRetraining) || Convert.ToBoolean(usecaseDetails.IsOnline) || Convert.ToBoolean(usecaseDetails.IsOffline))
            {
                iscarryOutRetraining = Convert.ToBoolean(usecaseDetails.IsCarryOutRetraining);
                isOnline = Convert.ToBoolean(usecaseDetails.IsOnline);
                isOffline = Convert.ToBoolean(usecaseDetails.IsOffline);
                training = JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(usecaseDetails.Training));
                prediction = JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(usecaseDetails.Prediction));
                retraining = JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(usecaseDetails.Retraining));
            }

            if (iscarryOutRetraining && retraining != null && ((JContainer)retraining).HasValues)
            {
                this.AssignFrequencyUsecase(usecaseDetails, usecaseDetails.Retraining, retraining, CONSTANTS.Retraining);
            }
            if (usecaseDetails.IsOffline)
            {
                if (training != null && ((JContainer)training).HasValues)
                {
                    this.AssignFrequencyUsecase(usecaseDetails, usecaseDetails.Training, training, CONSTANTS.Training);
                }
                if (prediction != null && ((JContainer)prediction).HasValues)
                {
                    this.AssignFrequencyUsecase(usecaseDetails, usecaseDetails.Prediction, prediction, CONSTANTS.Prediction);
                }
            }

            var serviceDetails = this.GetAiCoreServiceDetails(usecaseDetails.ServiceId);
            if (serviceDetails.ServiceCode == "CLUSTERING")
            {
                ClusteringAPIModel clusteringAPI = this.GetClusteringModelName(usecaseDetails.CorrelationId);
                usecaseDetails.InputColumns = clusteringAPI.Columnsselectedbyuser;
                usecaseDetails.ModelName = clusteringAPI.ModelName;
                usecaseDetails.SourceName = clusteringAPI.DataSource;
                usecaseDetails.SourceDetails = clusteringAPI.ParamArgs;
                usecaseDetails.StopWords = clusteringAPI.StopWords;
                usecaseDetails.Ngram = clusteringAPI.Ngram;
                usecaseDetails.CreatedBy = clusteringAPI.CreatedBy;
                usecaseDetails.DataSetUID = clusteringAPI.DataSetUId;
                usecaseDetails.MaxDataPull = clusteringAPI.MaxDataPull;
            }
            else if (serviceDetails.ServiceCode == "WORDCLOUD")
            {
                ClusteringAPIModel clusteringAPI = this.GetClusteringModelName(usecaseDetails.CorrelationId);
                usecaseDetails.InputColumns = clusteringAPI.Columnsselectedbyuser;
                usecaseDetails.ModelName = clusteringAPI.ModelName;
                usecaseDetails.SourceName = clusteringAPI.DataSource;
                usecaseDetails.SourceDetails = clusteringAPI.ParamArgs;
                usecaseDetails.StopWords = clusteringAPI.StopWords;
                usecaseDetails.CreatedBy = clusteringAPI.CreatedBy;
                usecaseDetails.DataSetUID = clusteringAPI.DataSetUId;
                usecaseDetails.MaxDataPull = clusteringAPI.MaxDataPull;
            }
            else
            {
                AIServiceRequestStatus modelDetails = GetAIModelConfig(usecaseDetails.CorrelationId, "TrainModel");
                usecaseDetails.InputColumns = modelDetails.SelectedColumnNames;
                usecaseDetails.ModelName = modelDetails.ModelName;
                usecaseDetails.SourceName = modelDetails.DataSource;
                usecaseDetails.SourceDetails = modelDetails.SourceDetails;
                usecaseDetails.ScoreUniqueName = modelDetails.ScoreUniqueName;
                usecaseDetails.Threshold_TopnRecords = modelDetails.Threshold_TopnRecords;
                usecaseDetails.StopWords = modelDetails.StopWords;
                usecaseDetails.CreatedBy = modelDetails.CreatedByUser;
                usecaseDetails.DataSetUID = modelDetails.DataSetUId;
                usecaseDetails.MaxDataPull = modelDetails.MaxDataPull;
            }
            var collection1 = _database.GetCollection<BsonDocument>("AISavedUsecases");
            var filter1 = Builders<BsonDocument>.Filter.Eq("UsecaseName", usecaseDetails.UsecaseName);
            var result1 = collection1.Find(filter1).ToList();
            if (result1.Count > 0)
            {
                foreach (var item in result1)
                {
                    if (item["UsecaseName"].ToString().ToLower() == usecaseDetails.UsecaseName.ToLower())
                    {
                        throw new Exception("Usecase Name already exist");
                    }
                }

            }
            if (!string.IsNullOrEmpty(usecaseDetails.DataSetUID) && usecaseDetails.DataSetUID != CONSTANTS.Null && usecaseDetails.DataSetUID != CONSTANTS.BsonNull)
            {
                if (!dataSetsService.CheckDataSetExists(usecaseDetails.DataSetUID))
                    throw new Exception(CONSTANTS.InvalidInput);
            }
            var collection = _database.GetCollection<BsonDocument>("AISavedUsecases");
            var filter = Builders<BsonDocument>.Filter.Eq("UsecaseId", usecaseDetails.UsecaseId);
            var result = collection.Find(filter).ToList();
            var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(usecaseDetails);
            var insertDocument = BsonSerializer.Deserialize<BsonDocument>(jsonData);
            if (result.Count > 0)
            {
                collection.DeleteOne(filter);
            }
            Thread.Sleep(1000);
            collection.InsertOneAsync(insertDocument);
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(SaveUsecase), CONSTANTS.END, string.IsNullOrEmpty(usecaseDetails.CorrelationId) ? default(Guid) : new Guid(usecaseDetails.CorrelationId), usecaseDetails.ApplicationId, string.Empty, string.Empty, string.Empty);
            return usecaseDetails;

        }


        public AIServiceRequestStatus GetAIModelConfig(string correlationid, string pageInfo)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(GetAIModelConfig), CONSTANTS.START, string.IsNullOrEmpty(correlationid) ? default(Guid) : new Guid(correlationid), string.Empty, string.Empty, string.Empty, string.Empty);
            AIServiceRequestStatus modelDetails = new AIServiceRequestStatus();
            var modelCollection = _database.GetCollection<BsonDocument>("AIServiceRequestStatus");
            var filter = Builders<BsonDocument>.Filter.Eq("CorrelationId", correlationid) & (Builders<BsonDocument>.Filter.Eq("PageInfo", pageInfo) | Builders<BsonDocument>.Filter.Eq("PageInfo", "Ingest_Train"));
            var projection = Builders<BsonDocument>.Projection.Exclude("_id");
            var result = modelCollection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                modelDetails = JsonConvert.DeserializeObject<AIServiceRequestStatus>(result[0].ToJson());
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(GetAIModelConfig), CONSTANTS.END, string.IsNullOrEmpty(correlationid) ? default(Guid) : new Guid(correlationid), string.Empty, string.Empty, string.Empty, string.Empty);
            return modelDetails;
        }
        public ClusteringAPIModel GetClusteringModelName(string correlationId)
        {
            ClusteringAPIModel modelDetails = new ClusteringAPIModel();
            var collection = _database.GetCollection<ClusteringAPIModel>(CONSTANTS.Clustering_IngestData);
            var filter = Builders<ClusteringAPIModel>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var projection = Builders<ClusteringAPIModel>.Projection.Exclude(CONSTANTS.Id).Include("ModelName").Include("ParamArgs").Include("Columnsselectedbyuser").Include("DataSource").Include("StopWords").Include("Ngram").Include(CONSTANTS.CreatedByUser);
            var result = collection.Find(filter).Project<ClusteringAPIModel>(projection).ToList();
            if (result.Count > 0)
            {
                modelDetails = JsonConvert.DeserializeObject<ClusteringAPIModel>(result[0].ToJson());
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(GetClusteringModelName), CONSTANTS.END, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            return modelDetails;
        }

        //Fetch list of use cases based on serviceId
        public List<USECASE.UsecaseDetails> FetchUseCaseDetails(string serviceId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(FetchUseCaseDetails), CONSTANTS.START, string.Empty, string.Empty, string.Empty, string.Empty);
            List<USECASE.UsecaseDetails> useCaseList = new List<USECASE.UsecaseDetails>();
            var useCaseCollection = _database.GetCollection<BsonDocument>("AISavedUsecases");
            var filter = Builders<BsonDocument>.Filter.Eq("ServiceId", serviceId);
            var projection = Builders<BsonDocument>.Projection.Exclude("_id");
            var result = useCaseCollection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                useCaseList = JsonConvert.DeserializeObject<List<USECASE.UsecaseDetails>>(result.ToJson());
                if (appSettings.isForAllData)
                {
                    foreach (var Model in useCaseList)
                    {
                        try
                        {
                            if (!string.IsNullOrEmpty(Convert.ToString(Model.CreatedBy)))
                                Model.CreatedBy = _encryptionDecryption.Decrypt(Convert.ToString(Model.CreatedBy));
                        }
                        catch (Exception ex)
                        {
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(FetchUseCaseDetails), ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty);
                        }
                        try
                        {
                            if (!string.IsNullOrEmpty(Convert.ToString(Model.ModifiedBy)))
                                Model.ModifiedBy = _encryptionDecryption.Decrypt(Convert.ToString(Model.ModifiedBy));
                        }
                        catch (Exception ex)
                        {
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(FetchUseCaseDetails), ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty);
                        }
                    }
                }
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(FetchUseCaseDetails), CONSTANTS.END, string.Empty, string.Empty, string.Empty, string.Empty);
            return useCaseList;
        }




        //Fetch usecase details based on Id
        public USECASE.UsecaseDetails GetUsecaseDetails(string usecaseId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(GetUsecaseDetails), CONSTANTS.START, string.Empty, string.Empty, string.Empty, string.Empty);
            USECASE.UsecaseDetails usecaseDetails = new USECASE.UsecaseDetails();
            var useCaseCollection = _database.GetCollection<BsonDocument>("AISavedUsecases");
            var filter = Builders<BsonDocument>.Filter.Eq("UsecaseId", usecaseId);
            var projection = Builders<BsonDocument>.Projection.Exclude("_id");
            var result = useCaseCollection.Find(filter).Project<BsonDocument>(projection).FirstOrDefault();
            if (result != null)
            {
                usecaseDetails = JsonConvert.DeserializeObject<USECASE.UsecaseDetails>(result.ToJson());
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(GetUsecaseDetails), CONSTANTS.END, string.Empty, string.Empty, string.Empty, string.Empty);
            return usecaseDetails;

        }

        public AIGENERICSERVICE.BulkPrediction.BulkPredictionData GetBulkPredictionDetails(string correlationId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(GetBulkPredictionDetails), CONSTANTS.START, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            AIGENERICSERVICE.BulkPrediction.BulkPredictionData bulkPredictionDetails = new AIGENERICSERVICE.BulkPrediction.BulkPredictionData();
            var useCaseCollection = _database.GetCollection<BsonDocument>(CONSTANTS.AIServicesPrediction);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId) & Builders<BsonDocument>.Filter.Eq("Page_number", 1) & Builders<BsonDocument>.Filter.Eq(CONSTANTS.PageInfo, "Prediction File");
            var projection = Builders<BsonDocument>.Projection.Exclude("_id");
            var result = useCaseCollection.Find(filter).Project<BsonDocument>(projection).FirstOrDefault();
            if (result.Count() > 0)
            {
                if (result.Contains(CONSTANTS.DBEncryptionRequired) && result[0][CONSTANTS.DBEncryptionRequired] != null && (bool)result[0][CONSTANTS.DBEncryptionRequired])
                {
                    try
                    {
                        if (result.Contains("CreatedBy") && !string.IsNullOrEmpty(Convert.ToString(result["CreatedBy"])))
                        {
                            result["CreatedBy"] = _encryptionDecryption.Decrypt(Convert.ToString(result["CreatedBy"]));
                        }
                    }
                    catch (Exception ex)
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(GetBulkPredictionDetails), ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty);
                    }
                    try
                    {
                        if (result.Contains("ModifiedBy") && !string.IsNullOrEmpty(Convert.ToString(result["ModifiedBy"])))
                        {
                            result["ModifiedBy"] = _encryptionDecryption.Decrypt(Convert.ToString(result["ModifiedBy"]));
                        }
                    }
                    catch (Exception ex)
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(GetBulkPredictionDetails), ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty);
                    }
                }
                bulkPredictionDetails = JsonConvert.DeserializeObject<AIGENERICSERVICE.BulkPrediction.BulkPredictionData>(result.ToJson());
                for (int i = 2; i <= bulkPredictionDetails.Total_pages; i++)
                {
                    filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId) & Builders<BsonDocument>.Filter.Eq("Page_number", i) & Builders<BsonDocument>.Filter.Eq(CONSTANTS.PageInfo, "Prediction File");
                    result = useCaseCollection.Find(filter).Project<BsonDocument>(projection).FirstOrDefault();
                    AIGENERICSERVICE.BulkPrediction.BulkPredictionData bulkPredictionDetails2 = new AIGENERICSERVICE.BulkPrediction.BulkPredictionData();
                    bulkPredictionDetails2 = JsonConvert.DeserializeObject<AIGENERICSERVICE.BulkPrediction.BulkPredictionData>(result.ToJson());
                    bulkPredictionDetails.InputData = bulkPredictionDetails.InputData + bulkPredictionDetails2.InputData;
                }
            }
            else
                bulkPredictionDetails.Status = "No Prediction records found";
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(GetBulkPredictionDetails), CONSTANTS.END, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            return bulkPredictionDetails;

        }

        public AIServiceRequestStatus GetAIServiceTrainedRequest(string correlationid)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(GetAIServiceTrainedRequest), CONSTANTS.START, string.IsNullOrEmpty(correlationid) ? default(Guid) : new Guid(correlationid), string.Empty, string.Empty, string.Empty, string.Empty);
            AIServiceRequestStatus modelDetails = new AIServiceRequestStatus();
            var modelCollection = _database.GetCollection<BsonDocument>("AIServiceRequestStatus");
            var filter = Builders<BsonDocument>.Filter.Eq("CorrelationId", correlationid) & Builders<BsonDocument>.Filter.Eq("PageInfo", "IngestData") & Builders<BsonDocument>.Filter.Eq("Status", "C");
            var projection = Builders<BsonDocument>.Projection.Exclude("_id");
            var result = modelCollection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                modelDetails = JsonConvert.DeserializeObject<AIServiceRequestStatus>(result[0].ToJson());
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(GetAIServiceTrainedRequest), CONSTANTS.END, string.IsNullOrEmpty(correlationid) ? default(Guid) : new Guid(correlationid), string.Empty, string.Empty, string.Empty, string.Empty);
            return modelDetails;
        }


        public MethodReturn<Response> DeveloperPredictionTraining(string clientId, string deliveryConstructId, string serviceId, string applicationId, string usecaseId, string modelName, string userId, bool isManual, string correlationId, bool retrain)
        {
            //Get usecase Details
            string flag = CONSTANTS.Null;
            string pageInfo = "IngestData";
            string modelMessage = "Training is in progress";
            var returnResponse = new MethodReturn<Response>();
            //var serviceResponse = new MethodReturn<object>();
            var response = new Response(clientId, deliveryConstructId, serviceId);
            AIServiceRequestStatus ingestRequest = new AIServiceRequestStatus();
            USECASE.UsecaseDetails usecaseDetails = GetUsecaseDetails(usecaseId);
            var trainedData = GetAIServiceTrainedRequest(correlationId);
            if (string.IsNullOrEmpty(correlationId))
            {
                correlationId = Guid.NewGuid().ToString();
                // uniId = Guid.NewGuid().ToString();
            }
            ParentFile parentDetail = new ParentFile();
            if (retrain)
            {
                pageInfo = "Retrain";
                modelMessage = "ReTrain is in Progress";
                flag = "AutoRetrain";
            }
            string encryptedUser = userId;
            if (appSettings.isForAllData)
            {
                if (!string.IsNullOrEmpty(userId))
                {
                    encryptedUser = _encryptionDecryption.Encrypt(Convert.ToString(userId));
                }
            }
            ingestRequest.CorrelationId = correlationId;
            ingestRequest.SelectedColumnNames = usecaseDetails.InputColumns;
            ingestRequest.ModelName = modelName;
            ingestRequest.ClientId = clientId;
            ingestRequest.UniId = Guid.NewGuid().ToString();
            ingestRequest.DeliveryconstructId = deliveryConstructId;
            ingestRequest.ServiceId = serviceId;
            ingestRequest.DataSource = usecaseDetails.SourceName;
            ingestRequest.PageInfo = pageInfo;//"IngestData";
                                              //changes to N to include this in window service for notification
                                              //ingestRequest.Status = "New";
            ingestRequest.Status = "N";
            ingestRequest.CreatedByUser = userId;
            ingestRequest.CreatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");//DateTime.UtcNow.ToString();
            ingestRequest.ModifiedByUser = userId;
            ingestRequest.ModifiedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");//DateTime.UtcNow.ToString();


            FileUpload fileUpload = new FileUpload
            {
                CorrelationId = ingestRequest.CorrelationId,
                ClientUID = ingestRequest.ClientId,
                DeliveryConstructUId = ingestRequest.DeliveryconstructId,
                Parent = parentDetail,
                Flag = flag,//CONSTANTS.Null,
                mapping = CONSTANTS.Null,
                mapping_flag = CONSTANTS.False,
                pad = string.Empty,
                metric = string.Empty,
                InstaMl = string.Empty,
                fileupload = null,
                Customdetails = CONSTANTS.Null
            };


            //if training data is from phoenix
            if (usecaseDetails.SourceName == "Phoenix")
            {
                //Create ingest request in AIServicerequest Collection
                parentDetail.Type = "null";
                parentDetail.Name = "null";
                fileUpload.Parent = parentDetail;
                _filepath = new Filepath();
                _filepath.fileList = "null";
                fileUpload.fileupload = _filepath;

                fileUpload.pad = usecaseDetails.SourceDetails;
                if (retrain)
                {
                    fileUpload.pad = trainedData.SourceDetails;
                    var padData = JObject.Parse(fileUpload.pad.ToString());

                    var pad = JObject.Parse(padData["pad"].ToString());
                    //var endDate = 
                    //   var pad = JsonConvert.DeserializeObject<JObject>(fileUpload.pad);
                    pad["startDate"] = pad["endDate"];
                    pad["endDate"] = DateTime.Today.ToString("MM/dd/yyyy");
                    fileUpload.pad = pad.ToString().Replace("\r\n", "");

                }
                else
                {
                    var pad = JsonConvert.DeserializeObject<JObject>(fileUpload.pad);
                    pad["startDate"] = DateTime.Today.AddYears(-2).ToString("MM/dd/yyyy");
                    pad["endDate"] = DateTime.Today.ToString("MM/dd/yyyy");
                    fileUpload.pad = pad.ToString().Replace("\r\n", "");
                }

                ingestRequest.SourceDetails = fileUpload.ToJson();

                InsertAIServiceRequest(ingestRequest);
                string trainedBy = "";
                if (isManual)
                {
                    trainedBy = "Manual";
                }
                else
                {
                    trainedBy = "Scheduler";
                }
                CreateAICoreModel(clientId, deliveryConstructId, serviceId, correlationId, modelName, string.Empty, "InProgress", modelMessage, encryptedUser, usecaseId, applicationId, ingestRequest.UniId, trainedBy, ingestRequest.PageInfo, ingestRequest.DataSource);

                //Call Python API
                Service service = GetAiCoreServiceDetails(serviceId);
                string baseUrl = appSettings.AICorePythonURL;
                string apiPath = service.PrimaryTrainApiUrl;
                apiPath = apiPath + "?" + "correlationId=" + correlationId + "&userId=" + encryptedUser + "&pageInfo=" + ingestRequest.PageInfo + "&UniqueId=" + ingestRequest.UniId;
                try
                {
                    //changed to py call from window service
                    response.CorrelationId = correlationId;
                    JObject responseData = new JObject();
                    responseData.Add("message", "Success");
                    responseData.Add("status", "True");
                    response.ResponseData = responseData;
                    //response.ResponseData = serviceResponse.ReturnValue;
                    returnResponse.ReturnValue = response;
                    returnResponse.IsSuccess = true;//serviceResponse.IsSuccess;
                    returnResponse.Message = "";//serviceResponse.Message;
                }
                catch (Exception ex)
                {
                    response.CorrelationId = correlationId;
                    returnResponse.ReturnValue = response;
                    returnResponse.IsSuccess = false;//serviceResponse.IsSuccess;
                    returnResponse.Message = ex.Message;
                }
            }
            return returnResponse;
        }

        public MethodReturn<Response> DeveloperPredictEvaluate(DeveloperPredictRequest developerPredictRequest)
        {
            var returnResponse = new MethodReturn<Response>();
            var serviceResponse = new MethodReturn<object>();
            var response = new Response(developerPredictRequest.ClientId, developerPredictRequest.DeliveryConstructId, developerPredictRequest.ServiceId);
            DP_PythonInput p_PythonInput = new DP_PythonInput();
            DataModels.AICore.BodyParams bodyParams = new DataModels.AICore.BodyParams();

            AICoreModels aICoreModels = GetAICoreModelByUsecaseId(developerPredictRequest.ClientId, developerPredictRequest.DeliveryConstructId, developerPredictRequest.ApplicationId, developerPredictRequest.ServiceId, developerPredictRequest.UsecaseId);
            Service service = GetAiCoreServiceDetails(developerPredictRequest.ServiceId);

            //Asset Usage
            auditTrailLog.CorrelationId = aICoreModels.CorrelationId;
            auditTrailLog.ApplicationID = aICoreModels.ApplicationId;
            auditTrailLog.ClientId = aICoreModels.ClientId;
            auditTrailLog.DCID = aICoreModels.DeliveryConstructId;
            auditTrailLog.UseCaseId = aICoreModels.UsecaseId;
            auditTrailLog.CreatedBy = aICoreModels.CreatedBy;
            auditTrailLog.ProcessName = CONSTANTS.PredictionName;
            auditTrailLog.UsageType = CONSTANTS.AssetUsage;
            auditTrailLog.FeatureName = service.ServiceName;

            CommonUtility.AuditTrailLog(auditTrailLog, configSetting);
            if (appSettings.isForAllData)
            {
                if (!string.IsNullOrEmpty(Convert.ToString(developerPredictRequest.UserId)))
                    developerPredictRequest.UserId = _encryptionDecryption.Encrypt(Convert.ToString(developerPredictRequest.UserId));
            }
            bodyParams.ClientId = developerPredictRequest.ClientId;
            bodyParams.DeliveryConstructId = developerPredictRequest.DeliveryConstructId;
            bodyParams.WorkItemExternalId = developerPredictRequest.WorkItemExternalId;
            bodyParams.WorkItemType = developerPredictRequest.WorkItemType;
            p_PythonInput.CorrelationId = aICoreModels.CorrelationId;
            p_PythonInput.UniqueId = aICoreModels.UniId;
            p_PythonInput.PageInfo = "IngestData";
            p_PythonInput.UserId = developerPredictRequest.UserId;
            p_PythonInput.Params = bodyParams.ToJson();
            string payload = JsonConvert.SerializeObject(p_PythonInput);
            JObject jObject = JsonConvert.DeserializeObject<JObject>(payload);
            //Service service = GetAiCoreServiceDetails(developerPredictRequest.ServiceId);
            response.CorrelationId = aICoreModels.CorrelationId;
            string baseUrl = appSettings.AICorePythonURL;
            string apiPath = service.ApiUrl;
            serviceResponse = this.RoutePOSTRequest(string.Empty, new Uri(baseUrl), apiPath,
             jObject, service.IsReturnArray);
            response.ResponseData = serviceResponse.ReturnValue;
            returnResponse.Message = serviceResponse.Message;
            returnResponse.IsSuccess = serviceResponse.IsSuccess;
            response.SetResponseDate(DateTime.UtcNow);
            returnResponse.ReturnValue = response;
            return returnResponse;
        }

        public string DeleteAIModel(string correlationId)
        {
            //Check it is cluster model or not
            var clustercollection = _database.GetCollection<BsonDocument>(CONSTANTS.Clustering_IngestData);
            var clusterFilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId.Trim());
            var clusterResult = clustercollection.Find(clusterFilter).ToList();
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), "DeleteAIModel CLUSTERRESULT - " + clusterResult.Count, "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            bool isDeleted = false;
            if (clusterResult.Count > 0)
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), "DeleteAIModel", "Clustering START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                //check model status and delete
                var clusterStatusCollection = _database.GetCollection<ClusterStatusModel>(CONSTANTS.Clustering_StatusTable);
                var clusterStatusFilter = Builders<ClusterStatusModel>.Filter.Eq(CONSTANTS.CorrelationId, correlationId.Trim());
                var clusterProjection = Builders<ClusterStatusModel>.Projection.Include(CONSTANTS.CorrelationId).Include(CONSTANTS.ModifiedOn).Include(CONSTANTS.Status).Exclude(CONSTANTS.Id);
                var clusterStatusResult = clusterStatusCollection.Find(clusterStatusFilter).Project<ClusterStatusModel>(clusterProjection).ToList();
                if (clusterStatusResult.Count > 0)
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), "DeleteAIModel", "Clustering2 START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                    //Check if model inprogress for more than 2 days than delete it. defect 1625320             
                    var inprogressModels = clusterStatusResult.Where(x => x.Status == CONSTANTS.P).ToList();
                    if (inprogressModels.Count > 0)
                    {
                        DateTime currentTime = DateTime.Now;
                        DateTime modelDateTime = DateTime.Parse(inprogressModels[0].ModifiedOn);
                        double diffInDays = (currentTime - modelDateTime).TotalDays;
                        if (diffInDays >= 1)
                        {
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), "DeleteAIModel", "Clustering3 START DIFFINDAYS--" + diffInDays, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                            //Delete cluster model
                            isDeleted = DeleteClusterModels(correlationId);
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), "DeleteAIModel DeleteAIModel - " + isDeleted, "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                        }
                        else
                        {
                            return "Model cannot be deleted as model status is in progress";
                        }
                    }
                    else
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), "DeleteAIModel ELSE", "Clustering START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                        //Delete cluster model with Status Completed, Warninig and Error.
                        isDeleted = DeleteClusterModels(correlationId);
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), "DeleteAIModel DeleteAIModel - " + isDeleted, "START", string.Empty, string.Empty, string.Empty, string.Empty);
                    }
                }
                else
                    isDeleted = DeleteClusterModels(correlationId);
                if (isDeleted)
                    return "Model deleted successfully";
            }
            else
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), "DeleteAIModel", "AIModel START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                AICoreModels aICoreModels = GetAICoreModelPath(correlationId);
                if (aICoreModels != null && aICoreModels.CorrelationId != null)
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), "DeleteAIModel AICOREMODELSMODELSTATUS - " + aICoreModels.ModelStatus, "START", string.IsNullOrEmpty(aICoreModels.CorrelationId) ? default(Guid) : new Guid(aICoreModels.CorrelationId), string.Empty, string.Empty, string.Empty, string.Empty);
                    if (aICoreModels.ModelStatus == "Completed"
                                        || aICoreModels.ModelStatus == "Error"
                                        || aICoreModels.ModelStatus == "Warning")
                    {
                        //Delete the Ai Model
                        DeleteAIModel(correlationId, aICoreModels);
                        return "Model deleted successfully";
                    }
                    else
                    {
                        //Check if model inprogress for more than 2 days than delete it.
                        DateTime currentTime = DateTime.Now;
                        DateTime modelDateTime = DateTime.Parse(aICoreModels.CreatedOn);
                        double diffInDays = (currentTime - modelDateTime).TotalDays;
                        if (diffInDays >= 1)
                        {
                            //Delete cluster model
                            DeleteAIModel(correlationId, aICoreModels);
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), "DeleteAIModel DeleteAIModel - " + isDeleted, "START", string.IsNullOrEmpty(aICoreModels.CorrelationId) ? default(Guid) : new Guid(aICoreModels.CorrelationId), string.Empty, string.Empty, string.Empty, string.Empty);
                            return "Model deleted successfully";
                        }
                        else
                            return "Model cannot be deleted as model status is in progress";
                    }
                }
                else
                    return CONSTANTS.NoRecordsFound;
            }
            return "Model deleted successfully";
        }
        private void DeleteAIModel(string correlationId, AICoreModels aICoreModels)
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


            //Delete Model
            var aICoreModel = _database.GetCollection<BsonDocument>(CONSTANTS.AICoreModels);
            result = aICoreModel.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                aICoreModel.DeleteMany(filter);
            }
        }
        private bool DeleteClusterModels(string correlationId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), "DeleteClusterModels", "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            bool isdeleted = false;
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId.Trim());
            try
            {
                var Clustering_BusinessProblem = _database.GetCollection<BsonDocument>("Clustering_BusinessProblem");
                var Clustering_DE_DataCleanup = _database.GetCollection<BsonDocument>("Clustering_DE_DataCleanup");
                var Clustering_DE_PreProcessedData = _database.GetCollection<BsonDocument>("Clustering_DE_PreProcessedData");
                var Clustering_DataCleanUP_FilteredData = _database.GetCollection<BsonDocument>("Clustering_DataCleanUP_FilteredData");
                var Clustering_DataPreprocessing = _database.GetCollection<BsonDocument>("Clustering_DataPreprocessing");
                var Clustering_Eval = _database.GetCollection<BsonDocument>("Clustering_Eval");
                var Clustering_EvalTestResults = _database.GetCollection<BsonDocument>("Clustering_EvalTestResults");
                var Clustering_SSAI_savedModels = _database.GetCollection<BsonDocument>("Clustering_SSAI_savedModels");
                var Clustering_IngestData = _database.GetCollection<BsonDocument>("Clustering_IngestData");
                var Clustering_TrainedModels = _database.GetCollection<BsonDocument>("Clustering_TrainedModels");
                var Clustering_StatusTable = _database.GetCollection<BsonDocument>("Clustering_StatusTable");
                var Clustering_ViewMappedData = _database.GetCollection<BsonDocument>("Clustering_ViewMappedData");
                var Clustering_ViewTrainedData = _database.GetCollection<BsonDocument>("Clustering_ViewTrainedData");
                var Clustering_Visualization = _database.GetCollection<BsonDocument>("Clustering_Visualization");
                var AIServiceRequestStatus = _database.GetCollection<BsonDocument>("AIServiceRequestStatus");

                //Delete Start
                Clustering_BusinessProblem.DeleteMany(filter);
                Clustering_DE_DataCleanup.DeleteMany(filter);
                Clustering_DE_PreProcessedData.DeleteMany(filter);
                Clustering_DataCleanUP_FilteredData.DeleteMany(filter);
                Clustering_DataPreprocessing.DeleteMany(filter);
                Clustering_Eval.DeleteMany(filter);
                Clustering_EvalTestResults.DeleteMany(filter);

                Clustering_IngestData.DeleteMany(filter);
                Clustering_TrainedModels.DeleteMany(filter);
                Clustering_StatusTable.DeleteMany(filter);
                Clustering_ViewMappedData.DeleteMany(filter);
                Clustering_ViewTrainedData.DeleteMany(filter);
                Clustering_Visualization.DeleteMany(filter);
                var res = AIServiceRequestStatus.DeleteMany(filter);
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), "DeleteClusterModels DELETECOUNT-" + res.DeletedCount, "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                var projection = Builders<BsonDocument>.Projection.Include("FilePath").Exclude(CONSTANTS.Id);
                var result = Clustering_SSAI_savedModels.Find(filter).Project<BsonDocument>(projection).ToList();
                if (result.Count > 0)
                {
                    string filepath = result[0]["FilePath"].ToString();
                    if (!string.IsNullOrEmpty(filepath))
                    {
                        if (File.Exists(filepath))
                        {
                            File.Delete(filepath);
                        }
                    }
                }
                Clustering_SSAI_savedModels.DeleteMany(filter);
                isdeleted = true;
                //Delete End	
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AICoreService), nameof(DeleteAIUsecase), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), "DeleteClusterModels", "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            return isdeleted;
        }

        public string DeleteAIUsecase(string usecaseId)
        {
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.UsecaseId, usecaseId);
            var projection = Builders<BsonDocument>.Projection.Exclude("_id");

            //Check is cluster or not
            var clustercollection = _database.GetCollection<BsonDocument>(CONSTANTS.Clustering_IngestData);
            var clusterResult = clustercollection.Find(filter).Project<BsonDocument>(projection).ToList();
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), "DeleteAIUsecase CLUSTERRESULTUSECASE - " + clusterResult.Count, "START", string.Empty, string.Empty, string.Empty, string.Empty);
            if (clusterResult.Count > 0)
            {
                throw new Exception("Usecase cannot be deleted as it is mapped with other models");
            }
            else
            {
                //Check usecase id mapping
                var aICoreModel = _database.GetCollection<BsonDocument>(CONSTANTS.AICoreModels);
                var result = aICoreModel.Find(filter).Project<BsonDocument>(projection).ToList();
                if (result.Count > 0)
                {
                    throw new Exception("Usecase cannot be deleted as it is mapped with other models");
                }
                else
                {
                    var aISavedUsecase = _database.GetCollection<BsonDocument>(CONSTANTS.AISavedUsecases);
                    result = aISavedUsecase.Find(filter).Project<BsonDocument>(projection).ToList();
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), "DeleteAIUsecase CLUSTERRESULTUSECASE result2 - " + result.Count, "START", string.Empty, string.Empty, string.Empty, string.Empty);
                    if (result.Count > 0)
                    {
                        aISavedUsecase.DeleteMany(filter);
                    }
                }
            }
            return "Usecase deleted successfully";
        }

        public void UpdateTokenInAppIntegration(string resourceId, string applicationId)
        {
            AppIntegrationsCredentials appIntegrationsCredentials = new AppIntegrationsCredentials()
            {
                grant_type = appSettings.Grant_Type,
                client_id = appSettings.clientId,
                client_secret = appSettings.clientSecret,
                resource = resourceId
            };

            AppIntegration appIntegrations = new AppIntegration()
            {
                ApplicationID = applicationId,
                Credentials = JsonConvert.SerializeObject(appIntegrationsCredentials)
            };
            this.UpdateAppIntegration(appIntegrations);
        }

        public AICoreModels GetUseCaseDetails(string clientId, string deliverConstructId, string usecaseId, string serviceId, string userId)
        {
            string encrypteduser = userId;
            if (!string.IsNullOrEmpty(encrypteduser))
            {
                encrypteduser = _encryptionDecryption.Encrypt(encrypteduser);
            }
            var modelCollection = _database.GetCollection<AICoreModels>(CONSTANTS.AICoreModels);
            var modelfilter = Builders<AICoreModels>.Filter;
            var modelfilterVal = modelfilter.Where(x => x.ClientId == clientId)
                                 & modelfilter.Where(x => x.DeliveryConstructId == deliverConstructId)
                                 & modelfilter.Where(x => x.ServiceId == serviceId)
                                 & modelfilter.Where(x => x.UsecaseId == usecaseId)
                                 & modelfilter.Where(x => (x.CreatedBy == userId || x.CreatedBy == encrypteduser))
                                 & modelfilter.Where(x => x.ModelStatus == "Completed");
            var projection = Builders<AICoreModels>.Projection.Exclude("_id");
            var model = modelCollection.Find(modelfilterVal).Project<AICoreModels>(projection).FirstOrDefault();
            if (model != null)
            {
                if (appSettings.isForAllData)
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(Convert.ToString(model.CreatedBy)))
                        {
                            model.CreatedBy = _encryptionDecryption.Decrypt(Convert.ToString(model.CreatedBy));
                        }
                    }
                    catch (Exception ex)
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(GetUseCaseDetails), ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty);
                    }
                    try
                    {
                        if (!string.IsNullOrEmpty(Convert.ToString(model.ModifiedBy)))
                        {
                            model.ModifiedBy = _encryptionDecryption.Decrypt(Convert.ToString(model.ModifiedBy));
                        }
                    }
                    catch (Exception ex)
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(GetUseCaseDetails), ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty);
                    }
                }
            }
            return model;


        }

        /// <summary>
        /// Get AI Model Training Status
        /// </summary>
        /// <param name="correlationid"></param>
        /// <returns></returns>
        public AIModelStatus GetAIModelTrainingStatus(string correlationid)
        {
            AIModelStatus serviceList = new AIModelStatus();
            // AICoreModels aICoreModels = new AICoreModels();
            var serviceCollection = _database.GetCollection<BsonDocument>("AICoreModels");
            var filter = Builders<BsonDocument>.Filter.Eq("CorrelationId", correlationid);
            //var projection = Builders<BsonDocument>.Projection.Exclude("_id");
            var projection = Builders<BsonDocument>.Projection.Exclude("_id").Include("CorrelationId").Include("ModelStatus").Include("StatusMessage");
            var result = serviceCollection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                if (appSettings.isForAllData)
                {
                    try
                    {
                        if (result[0].Contains("CreatedBy") && !string.IsNullOrEmpty(Convert.ToString(result[0]["CreatedBy"])))
                            result[0]["CreatedBy"] = _encryptionDecryption.Decrypt(Convert.ToString(result[0]["CreatedBy"]));
                    }
                    catch (Exception ex)
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(GetAIModelTrainingStatus), ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty);
                    }
                    try
                    {
                        if (result[0].Contains("ModifiedBy") && !string.IsNullOrEmpty(Convert.ToString(result[0]["ModifiedBy"])))
                            result[0]["ModifiedBy"] = _encryptionDecryption.Decrypt(Convert.ToString(result[0]["ModifiedBy"]));
                    }
                    catch (Exception ex)
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(GetAIModelTrainingStatus), ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty);
                    }
                }
                // aICoreModels = JsonConvert.DeserializeObject<AICoreModels>(result[0].ToJson());
                serviceList = JsonConvert.DeserializeObject<AIModelStatus>(result[0].ToJson());
                //if (serviceList.ModelStatus == "Completed")
                //{
                //    USECASE.UsecaseDetails usecaseDetails = GetUsecaseDetails(aICoreModels.UsecaseId);
                //    string callbackurl = GetSPAAICallBackUrl(correlationid);
                //    //if (!string.IsNullOrEmpty(callbackurl))
                //    //{
                //    //    IngrainResponseData CallBackResponse = new IngrainResponseData
                //    //    {
                //    //        CorrelationId = serviceList.CorrelationId,
                //    //        Status = serviceList.ModelStatus,
                //    //        Message = serviceList.StatusMessage,
                //    //        ErrorMessage = string.Empty
                //    //    };
                //    //    string callbackResonse = CommonUtility.CallbackResponse(CallBackResponse, usecaseDetails.ApplicationName, callbackurl, aICoreModels.ClientId, aICoreModels.DeliveryConstructId, aICoreModels.ApplicationId, aICoreModels.UsecaseId, aICoreModels.UniId, aICoreModels.CreatedBy, configSetting, _encryptionDecryption);
                //    //}
                //}
                //if (serviceList.ModelStatus == "Error" || serviceList.ModelStatus == "Warning")
                //{
                //    USECASE.UsecaseDetails usecaseDetails = GetUsecaseDetails(aICoreModels.UsecaseId);
                //    string callbackurl = GetSPAAICallBackUrl(correlationid);
                //    //if (!string.IsNullOrEmpty(callbackurl))
                //    //{
                //    //    IngrainResponseData CallBackResponse = new IngrainResponseData
                //    //    {
                //    //        CorrelationId = serviceList.CorrelationId,
                //    //        Status = CONSTANTS.Error,
                //    //        Message = serviceList.StatusMessage,
                //    //        ErrorMessage = serviceList.StatusMessage
                //    //    };
                //    //    string callbackResonse = CommonUtility.CallbackResponse(CallBackResponse, usecaseDetails.ApplicationName, callbackurl, aICoreModels.ClientId, aICoreModels.DeliveryConstructId, aICoreModels.ApplicationId, aICoreModels.UsecaseId, aICoreModels.UniId, aICoreModels.CreatedBy, configSetting, _encryptionDecryption);
                //    //}
                //}
            }

            return serviceList;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="correlationId"></param>
        /// <returns></returns>
        public string GetSPAAICallBackUrl(string correlationId)
        {
            string responseCallbackurl = null;
            var AIServiceRequestStatus = _database.GetCollection<BsonDocument>("AIServiceRequestStatus");
            var AIServiceFilterBuilder = Builders<BsonDocument>.Filter;
            var AIServiceQueue = AIServiceFilterBuilder.Eq("CorrelationId", correlationId);
            var projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
            var result = AIServiceRequestStatus.Find(AIServiceQueue).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                foreach (var item in result)
                {
                    if (item.Contains("ResponsecallbackUrl"))
                    {
                        if (item["ResponsecallbackUrl"] != BsonNull.Value)
                        {
                            responseCallbackurl = item["ResponsecallbackUrl"].ToString();
                            return responseCallbackurl;
                        }
                    }
                }
            }
            return responseCallbackurl;
        }

        public AICoreModels GetModelOnUsecaseId(string clientId, string deliveryConstructId, string applicationId, string serviceId, string usecaseId)
        {
            AICoreModels serviceList = new AICoreModels();
            var serviceCollection = _database.GetCollection<BsonDocument>("AICoreModels");
            var filter = Builders<BsonDocument>.Filter.Eq("ClientId", clientId)
                         & Builders<BsonDocument>.Filter.Eq("DeliveryConstructId", deliveryConstructId)
                          & Builders<BsonDocument>.Filter.Eq("ApplicationId", applicationId)
                           & Builders<BsonDocument>.Filter.Eq("ServiceId", serviceId)
                            & Builders<BsonDocument>.Filter.Eq("UsecaseId", usecaseId);
            //   & Builders<BsonDocument>.Filter.Eq("ModelStatus", "Completed");
            var projection = Builders<BsonDocument>.Projection.Exclude("_id");
            var result = serviceCollection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                if (appSettings.isForAllData)
                {
                    try
                    {
                        if (result[0].Contains("CreatedBy") && !string.IsNullOrEmpty(Convert.ToString(result[0]["CreatedBy"])))
                            result[0]["CreatedBy"] = _encryptionDecryption.Decrypt(Convert.ToString(result[0]["CreatedBy"]));
                    }
                    catch (Exception ex)
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(GetModelOnUsecaseId), ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty);
                    }
                    try
                    {
                        if (result[0].Contains("ModifiedBy") && !string.IsNullOrEmpty(Convert.ToString(result[0]["ModifiedBy"])))
                            result[0]["ModifiedBy"] = _encryptionDecryption.Decrypt(Convert.ToString(result[0]["ModifiedBy"]));
                    }
                    catch (Exception ex)
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(GetModelOnUsecaseId), ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty);
                    }
                }
                serviceList = JsonConvert.DeserializeObject<AICoreModels>(result[0].ToJson());
            }

            return serviceList;
        }

        public void InsertTextSummary(AIGetSummaryModel aIServicesPrediction)
        {
            aIServicesPrediction.ActualData = _encryptionDecryption.Encrypt(aIServicesPrediction.ActualData);
            aIServicesPrediction.CreatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            aIServicesPrediction.ModifiedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            //if (appSettings.isForAllData)
            //{
            //    if (!string.IsNullOrEmpty(Convert.ToString(aIServicesPrediction.CreatedBy)))
            //    {
            //        aIServicesPrediction.CreatedBy = _encryptionDecryption.Encrypt(Convert.ToString(aIServicesPrediction.CreatedBy));
            //    }
            //    if (!string.IsNullOrEmpty(Convert.ToString(aIServicesPrediction.ModifiedBy)))
            //    {
            //        aIServicesPrediction.ModifiedBy = _encryptionDecryption.Encrypt(Convert.ToString(aIServicesPrediction.ModifiedBy));
            //    }
            //}
            var predCollection = _database.GetCollection<AIGetSummaryModel>("AIServicesTextSummaryPrediction");
            var predFilter = Builders<AIGetSummaryModel>.Filter.Eq("UniId", aIServicesPrediction.UniId);
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="correlationId"></param>
        /// <returns></returns>
        public AIGetSummaryModel GetTextSummaryStatus(string correlationId, bool isSource)
        {
            AIGetSummaryModel serviceList = new AIGetSummaryModel();
            var serviceCollection = _database.GetCollection<AIGetSummaryModel>("AIServicesTextSummaryPrediction");
            var filter = Builders<AIGetSummaryModel>.Filter.Eq("CorrelationId", correlationId);
            var projection = Builders<AIGetSummaryModel>.Projection.Exclude("_id").Include("CorrelationId").Include("Status").Include("Progress").Include("ErrorMessage").Include("PredictedData").Include("SourceDetails");
            var result = serviceCollection.Find(filter).Project<AIGetSummaryModel>(projection).ToList();
            if (result.Count > 0)
            {
                serviceList = result[0];
                if (serviceList.PredictedData != null)
                {
                    serviceList.PredictedData = _encryptionDecryption.Decrypt(serviceList.PredictedData);
                }
                try
                {
                    if (!string.IsNullOrEmpty(Convert.ToString(serviceList.CreatedBy)))
                    {
                        serviceList.CreatedBy = _encryptionDecryption.Decrypt(Convert.ToString(serviceList.CreatedBy));
                    }
                }
                catch (Exception ex)
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(GetTextSummaryStatus), ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty);
                }

                try
                {
                    if (!string.IsNullOrEmpty(Convert.ToString(serviceList.ModifiedBy)))
                    {
                        serviceList.ModifiedBy = _encryptionDecryption.Decrypt(Convert.ToString(serviceList.ModifiedBy));
                    }
                }
                catch (Exception ex)
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(GetTextSummaryStatus), ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty);
                }

                if (!isSource)
                {
                    serviceList.SourceDetails = null;
                }
            }
            return serviceList;
        }


        public SimilarityPredictionResponse GetSimilarityPredictions(SimilarityPredictionRequest similarityPredictionRequest)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(GetSimilarityPredictions), CONSTANTS.START, string.Empty, string.Empty, string.Empty, string.Empty);
            SimilarityPredictionResponse similarityPredictionResponse = new SimilarityPredictionResponse();
            similarityPredictionResponse.CorrelationId = similarityPredictionRequest.CorrelationId;
            similarityPredictionResponse.UniqueId = similarityPredictionRequest.UniqueId;
            similarityPredictionResponse.PageNumber = similarityPredictionRequest.PageNumber;

            AIGENERICSERVICE.BulkPrediction.BulkPredictionData bulkPredictionDetails = new AIGENERICSERVICE.BulkPrediction.BulkPredictionData();
            var useCaseCollection = _database.GetCollection<BsonDocument>(CONSTANTS.AIServicesPrediction);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, similarityPredictionRequest.CorrelationId)
                         & Builders<BsonDocument>.Filter.Eq("UniqueId", similarityPredictionRequest.UniqueId)
                         & Builders<BsonDocument>.Filter.Eq(CONSTANTS.PageInfo, "Prediction File");
            var projection = Builders<BsonDocument>.Projection.Exclude("_id");
            var result = useCaseCollection.Find(filter).Project<BsonDocument>(projection).CountDocuments();
            if (result > 0)
            {
                similarityPredictionResponse.TotalPageCount = (int)result;
                var filterDoc = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, similarityPredictionRequest.CorrelationId)
                         & Builders<BsonDocument>.Filter.Eq("UniqueId", similarityPredictionRequest.UniqueId)
                         & Builders<BsonDocument>.Filter.Eq("Page_number", similarityPredictionRequest.PageNumber)
                         & Builders<BsonDocument>.Filter.Eq(CONSTANTS.PageInfo, "Prediction File");
                var resultDoc = useCaseCollection.Find(filterDoc).Project<BsonDocument>(projection).FirstOrDefault();

                if (resultDoc != null)
                {
                    bulkPredictionDetails = JsonConvert.DeserializeObject<AIGENERICSERVICE.BulkPrediction.BulkPredictionData>(resultDoc.ToJson());
                    similarityPredictionResponse.TotalRecordCount = bulkPredictionDetails.TotalRecordCount;
                    similarityPredictionResponse.PredictedData = _encryptionDecryption.Decrypt(bulkPredictionDetails.InputData);
                    if (similarityPredictionRequest.Bulk == "Bulk")
                        similarityPredictionResponse.PredictedData = JsonConvert.DeserializeObject<object>(similarityPredictionResponse.PredictedData);
                    similarityPredictionResponse.Status = "Completed";
                    similarityPredictionResponse.StatusMessage = bulkPredictionDetails.StatusMessage;
                }
                else
                {
                    similarityPredictionResponse.Status = "Error";
                    similarityPredictionResponse.StatusMessage = "Error in prediction chunks";
                }
            }
            else
            {
                similarityPredictionResponse.Status = "Error";
                similarityPredictionResponse.StatusMessage = "No Prediction records found";
            }

            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(GetSimilarityPredictions), CONSTANTS.END, string.Empty, string.Empty, string.Empty, string.Empty);
            return similarityPredictionResponse;
        }

        public void SendTONotifications(AICoreModels aICoreModels, string status, string message)
        {
            //if (appSettings.isForAllData)
            //{
            //    if (!string.IsNullOrEmpty(Convert.ToString(aICoreModels.CreatedBy)))
            //        aICoreModels.CreatedBy = _encryptionDecryption.Encrypt(Convert.ToString(aICoreModels.CreatedBy));
            //}
            AppNotificationLog appNotificationLog = new AppNotificationLog()
            {
                ApplicationId = aICoreModels.ApplicationId,
                ClientUId = aICoreModels.ClientId,
                DeliveryConstructUId = aICoreModels.DeliveryConstructId,
                UseCaseId = aICoreModels.UsecaseId,
                UserId = aICoreModels.CreatedBy,
                CorrelationId = aICoreModels.CorrelationId,
                UniqueId = aICoreModels.UniId
            };
            if (status == CONSTANTS.C)
            {
                appNotificationLog.Status = "Completed";
                appNotificationLog.StatusMessage = "Success";
            }
            else
            {
                appNotificationLog.Status = "Error";
                appNotificationLog.StatusMessage = message;
            }

            SendAppNotification(appNotificationLog);
        }
        public void SendAppNotification(AppNotificationLog appNotificationLog)
        {
            appNotificationLog.RequestId = Guid.NewGuid().ToString();
            appNotificationLog.CreatedOn = DateTime.UtcNow.ToString();
            appNotificationLog.RetryCount = 0;
            appNotificationLog.IsNotified = false;

            var appIntegrationCollection = _database.GetCollection<AppIntegration>(CONSTANTS.AppIntegration);
            var filter = Builders<AppIntegration>.Filter.Where(x => x.ApplicationID == appNotificationLog.ApplicationId);
            var app = appIntegrationCollection.Find(filter).FirstOrDefault();

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

        public AIGENERICSERVICE.BulkTraining.AIServiceStatusResponse GetAIServiceRequestStatus(AIGENERICSERVICE.BulkTraining.AIServiceStatusRequest aIServiceStatusRequest)
        {
            AIGENERICSERVICE.BulkTraining.AIServiceStatusResponse aIServiceStatusResponse = new AIGENERICSERVICE.BulkTraining.AIServiceStatusResponse();
            var aiserviceRequestCollection = _database.GetCollection<BsonDocument>(CONSTANTS.AIServiceRequestStatus);
            var aiservicePredictionCollection = _database.GetCollection<AIGENERICSERVICE.BulkTraining.AIServiceStatusResponse>(CONSTANTS.AIServicesPrediction);
            if (aIServiceStatusRequest.PageInfo == CONSTANTS.IngestTrain)
            {
                var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, aIServiceStatusRequest.CorrelationId)
                    & Builders<BsonDocument>.Filter.Eq(CONSTANTS.UniId, aIServiceStatusRequest.UniqueId)
                    & Builders<BsonDocument>.Filter.Eq(CONSTANTS.PageInfo, aIServiceStatusRequest.PageInfo);
                var projection = Builders<BsonDocument>.Projection.Exclude("_id");
                var result = aiserviceRequestCollection.Find(filter).Project<BsonDocument>(projection).ToList();
                if (result.Count > 0)
                {
                    aIServiceStatusResponse = JsonConvert.DeserializeObject<AIGENERICSERVICE.BulkTraining.AIServiceStatusResponse>(result[0].ToString());
                    aIServiceStatusResponse.UniqueId = result[0][CONSTANTS.UniId].ToString();
                }
                else
                    aIServiceStatusResponse.Message = CONSTANTS.NoRecordsFound;
            }
            else if (aIServiceStatusRequest.PageInfo == CONSTANTS.PredictionStatus)
            {
                var filter = Builders<AIGENERICSERVICE.BulkTraining.AIServiceStatusResponse>.Filter.Where(x => x.CorrelationId == aIServiceStatusRequest.CorrelationId & x.UniqueId == aIServiceStatusRequest.UniqueId & x.PageInfo == aIServiceStatusRequest.PageInfo);
                var projection = Builders<AIGENERICSERVICE.BulkTraining.AIServiceStatusResponse>.Projection.Exclude("_id");
                var result = aiservicePredictionCollection.Find(filter).Project<AIGENERICSERVICE.BulkTraining.AIServiceStatusResponse>(projection).ToList();
                if (result.Count > 0)
                {
                    aIServiceStatusResponse = JsonConvert.DeserializeObject<AIGENERICSERVICE.BulkTraining.AIServiceStatusResponse>(result[0].ToJson());
                }
                else
                    aIServiceStatusResponse.Message = CONSTANTS.NoRecordsFound;
            }
            return aIServiceStatusResponse;
        }

        public string UpdateTokenInAppIntegration(string appId)
        {
            AppIntegrationsCredentials appIntegrationsCredentials = new AppIntegrationsCredentials()
            {
                //code comment for Ut testing only
                grant_type = configSetting.Value.Grant_Type,
                client_id = configSetting.Value.clientId,
                client_secret = configSetting.Value.clientSecret,
                resource = configSetting.Value.resourceId
            };

            AppIntegration appIntegrations = new AppIntegration()
            {
                ApplicationID = appId,
                Credentials = JsonConvert.SerializeObject(appIntegrationsCredentials)
            };

            return _genericSelfservice.UpdateAppIntegration(appIntegrations);
        }
        public string UpdateUseCaseTemplate(string appId, USECASE.UsecaseDetails usecaseDetails)
        {
            Uri apiUri = new Uri(configSetting.Value.myWizardAPIUrl);
            string host = apiUri.GetLeftPart(UriPartial.Authority);

            Uri apiUri1 = new Uri(usecaseDetails.SourceURL);
            string apiPath = apiUri1.AbsolutePath;

            var status = CONSTANTS.NoRecordsFound;
            var useCaseCollection = _database.GetCollection<BsonDocument>("AISavedUsecases");
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.UsecaseId, usecaseDetails.UsecaseId) & Builders<BsonDocument>.Filter.Eq(CONSTANTS.ApplicationId, appId);
            var projection = Builders<BsonDocument>.Projection.Exclude("_id");
            var result = useCaseCollection.Find(filter).Project<BsonDocument>(projection).FirstOrDefault();
            if (result != null)
            {
                var update = Builders<BsonDocument>.Update
                     .Set("SourceName", usecaseDetails.SourceName)
                     .Set("SourceURL", host + apiPath)
                     .Set(CONSTANTS.ModifiedByUser, appSettings.isForAllData ? _encryptionDecryption.Encrypt(Convert.ToString(configSetting.Value.username)) : configSetting.Value.username)
                     .Set(CONSTANTS.ModifiedOn, DateTime.UtcNow.ToString());
                useCaseCollection.UpdateOne(filter, update);
                status = CONSTANTS.Success;
            }


            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(UpdateUseCaseTemplate), "UpdateUseCaseTemplate - status : " + status, usecaseDetails.ApplicationId, string.Empty, string.Empty, string.Empty);
            return status;


        }


        /// <summary>
        /// Get Similarity Record Count
        /// </summary>
        /// <param name="correlationId"></param>
        /// <returns></returns>
        public string GetSimilarityRecordCount(string correlationId)
        {
            string recordCount = null;
            var AIServiceRequestStatus = _database.GetCollection<BsonDocument>("AIServiceRequestStatus");
            var AIServiceFilterBuilder = Builders<BsonDocument>.Filter;
            var AIServiceQueue = AIServiceFilterBuilder.Eq("CorrelationId", correlationId);
            var projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
            var result = AIServiceRequestStatus.Find(AIServiceQueue).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                foreach (var item in result)
                {
                    if (item["DataSource"] == "ExternalAPIDataSet" || item["DataSource"] == "File_DataSet")
                    {

                        var dataSetCollection = _database.GetCollection<DataSetInfoDto>(CONSTANTS.DataSetInfo);
                        var filter = Builders<DataSetInfoDto>.Filter.Where(x => x.DataSetUId == item["DataSetUId"]);
                        var dsprojection = Builders<DataSetInfoDto>.Projection.Exclude(CONSTANTS.Id);
                        var dresult = dataSetCollection.Find(filter).Project<DataSetInfoDto>(dsprojection).FirstOrDefault();
                        if (dresult != null && dresult.ValidRecordsDetails != null)
                        {
                            BsonDocument ValidRecordsDetails = new BsonDocument();
                            ValidRecordsDetails = BsonDocument.Parse(JsonConvert.SerializeObject(dresult.ValidRecordsDetails));
                            if (ValidRecordsDetails.Contains("Msg") && ValidRecordsDetails["Msg"] != BsonNull.Value)
                            {
                                recordCount = ValidRecordsDetails["Msg"].ToString();
                                return recordCount;
                            }
                        }
                    }
                    else
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

        public ModelsList GetAICoreModelsAESKeyVault(string clientid, string dcid, string serviceid, string userid)
        {
            string encrypteduser = userid;
            if (!string.IsNullOrEmpty(encrypteduser))
            {
                encrypteduser = _encryptionDecryption.EncryptAESVaultKey(Convert.ToString(encrypteduser));
            }
            List<AICoreModels> serviceList = new List<AICoreModels>();
            ModelsList modelsLists = new ModelsList();
            var serviceCollection = _database.GetCollection<BsonDocument>("AICoreModels");
            var filter = Builders<BsonDocument>.Filter.Eq("ClientId", clientid) & Builders<BsonDocument>.Filter.Eq("DeliveryConstructId", dcid) & Builders<BsonDocument>.Filter.Eq("ServiceId", serviceid) & (Builders<BsonDocument>.Filter.Eq("CreatedBy", userid) | Builders<BsonDocument>.Filter.Eq("CreatedBy", encrypteduser));
            var projection = Builders<BsonDocument>.Projection.Exclude("_id");
            var result = serviceCollection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                if (appSettings.isForAllData)
                {
                    for (int i = 0; i < result.Count; i++)
                    {
                        try
                        {
                            if (result[i].Contains("CreatedBy") && !string.IsNullOrEmpty(Convert.ToString(result[i]["CreatedBy"])))
                                result[i]["CreatedBy"] = _encryptionDecryption.DecryptAESVaultKey(Convert.ToString(result[i]["CreatedBy"]));
                        }
                        catch (Exception ex) { LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(GetAICoreModelsAESKeyVault) + "User is already decrypted....", ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty); }
                        try
                        {
                            if (result[i].Contains("ModifiedBy") && !string.IsNullOrEmpty(Convert.ToString(result[i]["ModifiedBy"])))
                                result[i]["ModifiedBy"] = _encryptionDecryption.DecryptAESVaultKey(Convert.ToString(result[i]["ModifiedBy"]));
                        }
                        catch (Exception ex) { LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(GetAICoreModelsAESKeyVault) + "User is already decrypted....", ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty); }
                    }
                }
                serviceList = JsonConvert.DeserializeObject<List<AICoreModels>>(result.ToJson());
                modelsLists.ModelStatus = serviceList;
            }
            return modelsLists;
        }

        public SAPredictionStatusResponse GetSAMultipleBulkPredictionStatus(SAPredictionStatus SAPredictionStatus)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(GetSAMultipleBulkPredictionStatus), CONSTANTS.START, string.Empty, string.Empty, string.Empty, string.Empty);
            SAPredictionStatusResponse SAPredictionStatusResponse = new SAPredictionStatusResponse();
            SAPredictionStatusResponse.CorrelationId = SAPredictionStatus.CorrelationId;
            SAPredictionStatusResponse.UniqueId = SAPredictionStatus.UniqueId;

            AIServiceRequestStatus PredictionStatus = new AIServiceRequestStatus();
            var useCaseCollection = _database.GetCollection<BsonDocument>("AIServiceRequestStatus");
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, SAPredictionStatus.CorrelationId)
                         & Builders<BsonDocument>.Filter.Eq("UniId", SAPredictionStatus.UniqueId)
                         & Builders<BsonDocument>.Filter.Eq(CONSTANTS.PageInfo, "Prediction Status");
            var projection = Builders<BsonDocument>.Projection.Exclude("_id");
            var result = useCaseCollection.Find(filter).Project<BsonDocument>(projection).FirstOrDefault();
            if (result != null)
            {
                PredictionStatus = JsonConvert.DeserializeObject<AIServiceRequestStatus>(result.ToJson());
                SAPredictionStatusResponse.Status = PredictionStatus.Status;
                SAPredictionStatusResponse.StatusMessage = PredictionStatus.Message;
            }
            else
            {
                SAPredictionStatusResponse.Status = "E";
                SAPredictionStatusResponse.StatusMessage = "Error while initiating Prediction";
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(GetSAMultipleBulkPredictionStatus), CONSTANTS.END, string.Empty, string.Empty, string.Empty, string.Empty);
            return SAPredictionStatusResponse;
        }
    }
}
