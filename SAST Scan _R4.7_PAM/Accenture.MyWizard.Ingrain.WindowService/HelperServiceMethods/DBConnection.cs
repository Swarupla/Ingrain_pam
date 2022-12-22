using System;
using System.Linq;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using Accenture.MyWizard.Cryptography.EncryptionProviders;
using WINSERVICEMODELS = Accenture.MyWizard.Ingrain.WindowService.Models;
using DATAMODELS = Accenture.MyWizard.Ingrain.DataModels.Models;
using DATAACCESS = Accenture.MyWizard.Ingrain.WindowService;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
using AIDataModels = Accenture.MyWizard.Ingrain.DataModels.AICore;
using MongoDB.Bson.Serialization;
using System.IO;
using CryptographyHelper = Accenture.MyWizard.Cryptography;
using Accenture.MyWizard.Ingrain.DataModels.Models;
using Accenture.MyWizard.Ingrain.DataModels.AICore;
using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
using Accenture.MyWizard.Ingrain.DataModels.InferenceEngineModel;

namespace Accenture.MyWizard.Ingrain.WindowService.HelperServiceMethods
{
    class DBConnection
    {
        private readonly WINSERVICEMODELS.AppSettings appSettings = null;
        private IMongoDatabase _database;
        private DATAACCESS.DatabaseProvider databaseProvider;
        private readonly string SchedulerUserName = "SYSTEM";
        public string IterationEntityUId { get; set; }
        static CryptographyHelper.Utility.CryptographyUtility CryptographyUtility = null;
        private bool _IsAESKeyVault;
        private string _aesKey;
        private string _aesVector;

        public DBConnection()
        {
            appSettings = AppSettingsJson.GetAppSettings().GetSection("AppSettings").Get<WINSERVICEMODELS.AppSettings>();
            databaseProvider = new DATAACCESS.DatabaseProvider();
            var dataBaseName = MongoUrl.Create(appSettings.connectionString).DatabaseName;
            MongoClient mongoClient = databaseProvider.GetDatabaseConnection();
            _database = mongoClient.GetDatabase(dataBaseName);
            IterationEntityUId = appSettings.IterationEntityUId;
            CryptographyUtility = new CryptographyHelper.Utility.CryptographyUtility();
            _IsAESKeyVault = Convert.ToBoolean(AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("IsAESKeyVault").Value);
            _aesKey = AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("aesKey").Value;
            _aesVector = AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("aesVector").Value;

        }


        public List<T> GetCollectionData<T>(FilterDefinition<T> filter, string collectionName)
        {
            var deployedModels = _database.GetCollection<T>(collectionName);
            var projection = Builders<T>.Projection.Exclude("_id");
            var result = deployedModels.Find(filter).Project<T>(projection).ToList();
            return result;
        }


        public List<string> GetCustomConstraints(string CorrelationId, string ConstraintsType, string CollectionName)
        {
            List<string> oConstraintsList = null;
            var ConstraintsCollection = _database.GetCollection<BsonDocument>(CollectionName);
            var filter = Builders<BsonDocument>.Filter.Empty;

            switch (ConstraintsType)
            {
                case CONSTANTS.TrainingConstraint:
                    filter = Builders<BsonDocument>.Filter.Eq("IsTrainingEnabled", true) & Builders<BsonDocument>.Filter.Eq("CorrelationID", CorrelationId);
                    break;
                case CONSTANTS.PredictionConstraint:
                    filter = Builders<BsonDocument>.Filter.Eq("IsPredictionEnabled", true) & Builders<BsonDocument>.Filter.Eq("CorrelationID", CorrelationId);
                    break;
                case CONSTANTS.ReTrainingConstraint:
                    filter = Builders<BsonDocument>.Filter.Eq("IsRetrainingEnabled", true) & Builders<BsonDocument>.Filter.Eq("CorrelationID", CorrelationId);
                    break;

                default:
                    return oConstraintsList;
            }

            var projection = Builders<BsonDocument>.Projection.Include(ConstraintsType).Exclude("_id");
            var TargetColumn = ConstraintsCollection.Find(filter).Project<BsonDocument>(projection).FirstOrDefault();
            if (TargetColumn != null)
            {
                var TargetValue = TargetColumn[ConstraintsType]["SelectedConstraints"];
                oConstraintsList = JsonConvert.DeserializeObject<List<string>>(TargetValue.ToJson());
            }
            return oConstraintsList;
        }
        public List<string> GetAICustomConstraints(AIDataModels.UseCase.UsecaseDetails item, string ConstraintsType)
        {
            List<string> oConstraintsList = null;
            var ConstraintsCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAICustomContraints);
            var filter = Builders<BsonDocument>.Filter.Empty;

            switch (ConstraintsType)
            {
                case CONSTANTS.TrainingConstraint:
                    filter = Builders<BsonDocument>.Filter.Eq("IsTrainingEnabled", true) & Builders<BsonDocument>.Filter.Eq("CorrelationID", item.UsecaseId);
                    break;
                case CONSTANTS.PredictionConstraint:
                    filter = Builders<BsonDocument>.Filter.Eq("IsPredictionEnabled", true) & Builders<BsonDocument>.Filter.Eq("CorrelationID", item.UsecaseId);
                    break;

                default:
                    return oConstraintsList;
            }

            var projection = Builders<BsonDocument>.Projection.Include(ConstraintsType).Exclude("_id");
            var TargetColumn = ConstraintsCollection.Find(filter).Project<BsonDocument>(projection).FirstOrDefault();
            if (TargetColumn != null)
            {
                var TargetValue = TargetColumn[ConstraintsType]["SelectedConstraints"];
                oConstraintsList = JsonConvert.DeserializeObject<List<string>>(TargetValue.ToJson());
            }
            return oConstraintsList;
        }
        public List<DATAMODELS.DeployModelsDto> CheckPredictConstraintModels(DATAMODELS.DeployModelsDto item)
        {
            var deployedModels = _database.GetCollection<DATAMODELS.DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
            var filter = Builders<DATAMODELS.DeployModelsDto>.Filter.Eq("TemplateUsecaseId", item.TemplateUsecaseId)
                         & Builders<DATAMODELS.DeployModelsDto>.Filter.Where(x => x.Status == CONSTANTS.Deployed);
            var projection = Builders<DATAMODELS.DeployModelsDto>.Projection.Exclude("_id");
            var result = deployedModels.Find(filter).Project<DATAMODELS.DeployModelsDto>(projection).ToList();
            return result;
        }
        public DATAMODELS.AppIntegration FetchAppServiceUID(string AppId)
        {
            DATAMODELS.AppIntegration AppIntegration = null;
            var AppIntegrationCollection = _database.GetCollection<DATAMODELS.AppIntegration>(CONSTANTS.AppIntegration);
            var AppIntegrationFilterBuilder = Builders<DATAMODELS.AppIntegration>.Filter;
            var AppIntegrationFilterQueue = AppIntegrationFilterBuilder.Where(x => (x.ApplicationID == AppId));
            var Projection1 = Builders<DATAMODELS.AppIntegration>.Projection.Exclude("_id");
            var AppIntegrationQueueResult = AppIntegrationCollection.Find(AppIntegrationFilterQueue).Project<DATAMODELS.AppIntegration>(Projection1).FirstOrDefault();

            string AppServiceUID = string.Empty;
            if (AppIntegrationQueueResult != null)
            {
                AppIntegration = AppIntegrationQueueResult;
            }
            return AppIntegration;
        }
        public DATAMODELS.DeployModelsDto CheckIfSSAIModelTrained(string clientId, string deliverConstructId, string usecaseId, string userId)
        {
            string encryptedUser = userId;
            if (!string.IsNullOrEmpty(Convert.ToString(encryptedUser)))
            {
                encryptedUser = appSettings.IsAESKeyVault? CryptographyUtility.Encrypt(Convert.ToString(encryptedUser)): AesProvider.Encrypt(Convert.ToString(encryptedUser), appSettings.aesKey, appSettings.aesVector);
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
        public DATAMODELS.PublicTemplateMapping FetchTemplateMappingData(string UsecaseID)
        {
            DATAMODELS.PublicTemplateMapping FetchTemplateMappingData = null;
            var PublicTemplateMappingCollection = _database.GetCollection<DATAMODELS.PublicTemplateMapping>(CONSTANTS.PublicTemplateMapping);
            var PublicTemplateMappingFilterBuilder = Builders<DATAMODELS.PublicTemplateMapping>.Filter;
            var PublicTemplateMappingFilterQueue = PublicTemplateMappingFilterBuilder.Where(x => x.UsecaseID == UsecaseID);
            var Projection1 = Builders<DATAMODELS.PublicTemplateMapping>.Projection.Exclude("_id");
            var PublicTemplateMappingQueueResult = PublicTemplateMappingCollection.Find(PublicTemplateMappingFilterQueue).Project<DATAMODELS.PublicTemplateMapping>(Projection1).FirstOrDefault();

            string AppServiceUID = string.Empty;
            if (PublicTemplateMappingQueueResult != null)
            {
                FetchTemplateMappingData = PublicTemplateMappingQueueResult;
            }
            return FetchTemplateMappingData;
        }
        public DATAMODELS.DeployModelsDto CheckIfSSAIModelDeployed(string clientId, string deliverConstructId, string usecaseId, string userId)
        {
            string encryptedUser = userId;
            if (!string.IsNullOrEmpty(Convert.ToString(encryptedUser)))
            {
                encryptedUser = appSettings.IsAESKeyVault ? CryptographyUtility.Encrypt(Convert.ToString(encryptedUser)) : AesProvider.Encrypt(Convert.ToString(encryptedUser), appSettings.aesKey, appSettings.aesVector);
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
        public AIDataModels.AICoreModels GetAICoreModelPath(string correlationid)
        {
            AIDataModels.AICoreModels serviceList = new AIDataModels.AICoreModels();
            var serviceCollection = _database.GetCollection<BsonDocument>("AICoreModels");
            var filter = Builders<BsonDocument>.Filter.Eq("CorrelationId", correlationid);
            var projection = Builders<BsonDocument>.Projection.Exclude("_id");
            var result = serviceCollection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                serviceList = JsonConvert.DeserializeObject<AIDataModels.AICoreModels>(result[0].ToJson());
            }

            return serviceList;
        }
        public ClusteringAPIModel GetClusteringModel(string correlationid)
        {
            ClusteringAPIModel serviceList = new ClusteringAPIModel();
            var serviceCollection = _database.GetCollection<BsonDocument>(CONSTANTS.Clustering_IngestData);
            var filter = Builders<BsonDocument>.Filter.Eq("CorrelationId", correlationid);
            var projection = Builders<BsonDocument>.Projection.Exclude("_id");
            var result = serviceCollection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                serviceList = JsonConvert.DeserializeObject<ClusteringAPIModel>(result[0].ToJson());
            }

            return serviceList;
        }

        public IEModel GetIEModel(string correlationId)
        {
            var collection = _database.GetCollection<IEModel>("IEModels");
            var builder = Builders<IEModel>.Filter;
            var filter = builder.Where(x => x.CorrelationId == correlationId);
            var projection = Builders<IEModel>.Projection.Exclude("_id");
            return collection.Find(filter).Project<IEModel>(projection).FirstOrDefault();
        }

        public ModelsList GetAICoreModels(string clientid, string dcid, string serviceid, string userid)
        {
            string encrypteduser = userid;
            if (!string.IsNullOrEmpty(encrypteduser))
            {
                //  encrypteduser = _encryptionDecryption.Encrypt(Convert.ToString(encrypteduser));
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
                                //        result[i]["CreatedBy"] = _encryptionDecryption.Decrypt(Convert.ToString(result[i]["CreatedBy"]));
                                result[i]["CreatedBy"] = _IsAESKeyVault ? CryptographyUtility.Encrypt(result[i]["CreatedBy"].ToString()) : AesProvider.Encrypt(result[i]["CreatedBy"].ToString(), _aesKey, _aesVector);
                        }
                        catch (Exception) { }
                        try
                        {
                            if (result[i].Contains("ModifiedBy") && !string.IsNullOrEmpty(Convert.ToString(result[i]["ModifiedBy"])))
                                //        result[i]["ModifiedBy"] = _encryptionDecryption.Decrypt(Convert.ToString(result[i]["ModifiedBy"]));
                                result[i]["CreatedBy"] = _IsAESKeyVault ? CryptographyUtility.Encrypt(result[i]["ModifiedBy"].ToString()) : AesProvider.Encrypt(result[i]["ModifiedBy"].ToString(), _aesKey, _aesVector);
                        }
                        catch (Exception) { }
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
        public BsonDocument CheckIfAIModelTrained(string clientId, string deliverConstructId, string usecaseId, string serviceId, string userId, string ServiceCode)
        {
            string encryptedUser = userId;
            if (!string.IsNullOrEmpty(Convert.ToString(encryptedUser)))
            {
                encryptedUser = appSettings.IsAESKeyVault ? CryptographyUtility.Encrypt(Convert.ToString(encryptedUser)) : AesProvider.Encrypt(Convert.ToString(encryptedUser), appSettings.aesKey, appSettings.aesVector);
            }

            var modelCollection = _database.GetCollection<BsonDocument>(CONSTANTS.Clustering_StatusTable);
            var modelfilter = Builders<BsonDocument>.Filter;
            var modelfilterVal = modelfilter.Empty;

            if (ServiceCode == "CLUSTERING" || ServiceCode == "WORDCLOUD")
            {
                modelfilterVal = modelfilter.Eq("pageInfo", "Model Training") & modelfilter.Eq("ClientID", clientId)
                                     & modelfilter.Eq("DCUID", deliverConstructId);
            }
            else
            {
                modelCollection = _database.GetCollection<BsonDocument>(CONSTANTS.AICoreModels);

                modelfilterVal = modelfilter.Eq("ClientId", clientId)
                                     & modelfilter.Eq("DeliveryConstructId", deliverConstructId)
                                     & modelfilter.Eq("ServiceId", serviceId)
                                     & modelfilter.Eq("UsecaseId", usecaseId);
            }
            var projection = Builders<BsonDocument>.Projection.Exclude("_id");
            var model = modelCollection.Find(modelfilterVal).Project<BsonDocument>(projection).FirstOrDefault();
            return model;
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
                            CreatedBy = appSettings.IsAESKeyVault ? CryptographyUtility.Encrypt(SchedulerUserName) : AesProvider.Encrypt(SchedulerUserName, appSettings.aesKey, appSettings.aesVector),
                            CreatedOn = DateTime.UtcNow,
                            ModifiedBy = appSettings.IsAESKeyVault ? CryptographyUtility.Encrypt(SchedulerUserName) : AesProvider.Encrypt(SchedulerUserName, appSettings.aesKey, appSettings.aesVector),
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
        public void InsertAICustomConstraintsRequests(string NewcorrelationId, string TemplateUseCaseID)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DBConnection), nameof(InsertAICustomConstraintsRequests), "Data Insertion started for Custom Constraints for correlation ID --" + NewcorrelationId, string.IsNullOrEmpty(NewcorrelationId) ? default(Guid) : new Guid(NewcorrelationId), "", string.Empty, "", "");
            var collection = _database.GetCollection<DATAMODELS.AICustomConfiguration>(CONSTANTS.AICustomContraints);
            var builder = Builders<DATAMODELS.AICustomConfiguration>.Filter;
            var filter = builder.Eq("UseCaseID", TemplateUseCaseID);
            var Projection = Builders<DATAMODELS.AICustomConfiguration>.Projection.Exclude("_id");
            var customConstraintResponse = collection.Find(filter).Project<DATAMODELS.AICustomConfiguration>(Projection).ToList();

            if (customConstraintResponse != null && customConstraintResponse.Count > 0)
            {
                // Insert a New document
                DATAMODELS.AICustomConfiguration oConfigurtion = new DATAMODELS.AICustomConfiguration
                {
                    _id = Guid.NewGuid().ToString(),
                    CorrelationID = NewcorrelationId,
                    TemplateUseCaseID = TemplateUseCaseID,
                    ApplicationID = customConstraintResponse[0].ApplicationID,
                    IsTrainingEnabled = customConstraintResponse[0].IsTrainingEnabled,
                    Training = customConstraintResponse[0].Training,
                    IsPredictionEnabled = customConstraintResponse[0].IsPredictionEnabled,
                    Prediction = customConstraintResponse[0].Prediction,
                    IsRetrainingEnabled = customConstraintResponse[0].IsRetrainingEnabled,
                    Retraining = customConstraintResponse[0].Retraining,
                    CreatedOn = customConstraintResponse[0].ApplicationID,
                    CreatedByUser = customConstraintResponse[0].ApplicationID
                };

                var jsonData = JsonConvert.SerializeObject(oConfigurtion);
                var insertDocument = BsonSerializer.Deserialize<DATAMODELS.AICustomConfiguration>(jsonData);
                collection.InsertOne(insertDocument);
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(DBConnection), nameof(InsertAICustomConstraintsRequests), "Data Insertion Completed for Ingest Data for corelation ID --" + NewcorrelationId, string.IsNullOrEmpty(NewcorrelationId) ? default(Guid) : new Guid(NewcorrelationId), "", string.Empty, "", "");
            }
        }
        public bool DeleteAIModel(string correlationId)
        {
            //Check it is cluster model or not
            var clustercollection = _database.GetCollection<BsonDocument>(CONSTANTS.Clustering_IngestData);
            var clusterFilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId.Trim());
            var clusterResult = clustercollection.Find(clusterFilter).ToList();
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DBConnection), "DeleteAIModel CLUSTERRESULT - " + clusterResult.Count, "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            bool isDeleted = false;
            if (clusterResult.Count > 0)
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(DBConnection), "DeleteAIModel", "Clustering START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                //check model status and delete
                var clusterStatusCollection = _database.GetCollection<ClusterStatusModel>(CONSTANTS.Clustering_StatusTable);
                var clusterStatusFilter = Builders<ClusterStatusModel>.Filter.Eq(CONSTANTS.CorrelationId, correlationId.Trim());
                var clusterProjection = Builders<ClusterStatusModel>.Projection.Include(CONSTANTS.CorrelationId).Include(CONSTANTS.ModifiedOn).Include(CONSTANTS.Status).Exclude(CONSTANTS.Id);
                var clusterStatusResult = clusterStatusCollection.Find(clusterStatusFilter).Project<ClusterStatusModel>(clusterProjection).ToList();
                if (clusterStatusResult.Count > 0)
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(DBConnection), "DeleteAIModel", "Clustering2 START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                    //Check if model inprogress for more than 2 days than delete it. defect 1625320             
                    var inprogressModels = clusterStatusResult.Where(x => x.Status == CONSTANTS.P).ToList();
                    if (inprogressModels.Count > 0)
                    {
                        DateTime currentTime = DateTime.Now;
                        DateTime modelDateTime = DateTime.Parse(inprogressModels[0].ModifiedOn);
                        double diffInDays = (currentTime - modelDateTime).TotalDays;
                        if (diffInDays >= 1)
                        {
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DBConnection), "DeleteAIModel", "Clustering3 START DIFFINDAYS--" + diffInDays, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                            //Delete cluster model
                            isDeleted = DeleteClusterModels(correlationId);
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DBConnection), "DeleteAIModel DeleteAIModel - " + isDeleted, "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(DBConnection), "DeleteAIModel ELSE", "Clustering START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                        //Delete cluster model with Status Completed, Warninig and Error.
                        isDeleted = DeleteClusterModels(correlationId);
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(DBConnection), "DeleteAIModel DeleteAIModel - " + isDeleted, "START", string.Empty, string.Empty, string.Empty, string.Empty);
                    }
                }
                else
                    isDeleted = DeleteClusterModels(correlationId);
                if (isDeleted)
                    return true;
            }
            else
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(DBConnection), "DeleteAIModel", "AIModel START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                AICoreModels aICoreModels = GetAICoreModelPath(correlationId);
                if (aICoreModels != null && aICoreModels.CorrelationId != null)
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(DBConnection), "DeleteAIModel AICOREMODELSMODELSTATUS - " + aICoreModels.ModelStatus, "START", string.IsNullOrEmpty(aICoreModels.CorrelationId) ? default(Guid) : new Guid(aICoreModels.CorrelationId), string.Empty, string.Empty, string.Empty, string.Empty);
                    if (aICoreModels.ModelStatus == "Completed"
                                        || aICoreModels.ModelStatus == "Error"
                                        || aICoreModels.ModelStatus == "Warning")
                    {
                        //Delete the Ai Model
                        DeleteAIModel(correlationId, aICoreModels);
                        return true;
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
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DBConnection), "DeleteAIModel DeleteAIModel - " + isDeleted, "START", string.IsNullOrEmpty(aICoreModels.CorrelationId) ? default(Guid) : new Guid(aICoreModels.CorrelationId), string.Empty, string.Empty, string.Empty, string.Empty);
                            return true;
                        }
                        else
                            return false;
                    }
                }
                else
                    return false;
            }
            return true;
        }
        private bool DeleteClusterModels(string correlationId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DBConnection), "DeleteClusterModels", "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
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
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(DBConnection), "DeleteClusterModels DELETECOUNT-" + res.DeletedCount, "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
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
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(DBConnection), nameof(DeleteClusterModels), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DBConnection), "DeleteClusterModels", "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            return isdeleted;
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
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(DBConnection), nameof(DeleteAIModel), ex.Message, ex, aICoreModels.ApplicationId, string.Empty, aICoreModels.ClientId, aICoreModels.DeliveryConstructId);
            }

            //Delete Model
            var aICoreModel = _database.GetCollection<BsonDocument>(CONSTANTS.AICoreModels);
            result = aICoreModel.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                aICoreModel.DeleteMany(filter);
            }
        }

    }
}
