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

namespace Accenture.MyWizard.Ingrain.WindowService.Services.ConstraintsHelperMethods
{
    class DBConnection
    {
        private readonly WINSERVICEMODELS.AppSettings appSettings = null;
        private IMongoDatabase _database;
        private DATAACCESS.DatabaseProvider databaseProvider;
        private readonly string SchedulerUserName = "SYSTEM";
        public string IterationEntityUId { get; set; }
        public string CREntityUId { get; set; }
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
            CREntityUId = appSettings.CREntityUId;
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


        #region SSAI Flow
        public DATAMODELS.DeployModelsDto CheckIfSSAIModelTrained(string clientId, string deliverConstructId, string usecaseId, string userId)
        {
            return CheckIfSSAIModelTrained(clientId, deliverConstructId, usecaseId, userId, string.Empty);
        }

        public DATAMODELS.DeployModelsDto CheckIfSSAIModelTrained(string clientId, string deliverConstructId, string usecaseId, string userId, string TeamAreaUID)
        {
            DATAMODELS.DeployModelsDto deployModel = null;

            string encryptedUser = userId;
            if (!string.IsNullOrEmpty(Convert.ToString(encryptedUser)))
            {
                encryptedUser = appSettings.IsAESKeyVault ? CryptographyUtility.Encrypt(Convert.ToString(encryptedUser)) : AesProvider.Encrypt(Convert.ToString(encryptedUser), appSettings.aesKey, appSettings.aesVector);
            }

            if (!string.IsNullOrEmpty(TeamAreaUID))
            {
                DATAMODELS.IngrainRequestQueue oIngrainRequest = new DATAMODELS.IngrainRequestQueue();
                var IngrainRequestsCollection = _database.GetCollection<DATAMODELS.IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
                var filter = Builders<DATAMODELS.IngrainRequestQueue>.Filter;
                var filterVal = filter.Where(x => x.ClientID == clientId)
                                     & filter.Where(x => x.DeliveryconstructId == deliverConstructId)
                                     & filter.Where(x => x.TemplateUseCaseID == usecaseId)
                                     & filter.Where(x => x.TeamAreaUId == TeamAreaUID)
                                     & filter.Where(x => (x.CreatedByUser == userId || x.CreatedByUser == encryptedUser));

                var projection = Builders<DATAMODELS.IngrainRequestQueue>.Projection.Exclude("_id");

                oIngrainRequest = IngrainRequestsCollection.Find(filterVal).Project<DATAMODELS.IngrainRequestQueue>(projection).FirstOrDefault();

                if (oIngrainRequest != null)
                {
                    var deployModelCollection = _database.GetCollection<DATAMODELS.DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
                    var modelfilter = Builders<DATAMODELS.DeployModelsDto>.Filter;
                    var modelfilterVal = modelfilter.Where(x => x.ClientUId == oIngrainRequest.ClientID)
                                         & modelfilter.Where(x => x.DeliveryConstructUID == oIngrainRequest.DeliveryconstructId)
                                         & modelfilter.Where(x => x.CorrelationId == oIngrainRequest.CorrelationId)
                                         & modelfilter.Where(x => (x.CreatedByUser == userId || x.CreatedByUser == encryptedUser))
                                         & modelfilter.Where(x => (x.Status != "Deployed"));

                    var projection1 = Builders<DATAMODELS.DeployModelsDto>.Projection.Exclude("_id");
                    deployModel = deployModelCollection.Find(modelfilterVal).Project<DATAMODELS.DeployModelsDto>(projection1).FirstOrDefault();

                    if (deployModel == null)
                    {
                        //in case no model are in progress , then need to check if we had deployed trained models
                        modelfilterVal = modelfilter.Where(x => x.ClientUId == oIngrainRequest.ClientID)
                                            & modelfilter.Where(x => x.DeliveryConstructUID == oIngrainRequest.DeliveryconstructId)
                                            & modelfilter.Where(x => x.CorrelationId == oIngrainRequest.CorrelationId)
                                            & modelfilter.Where(x => (x.CreatedByUser == userId || x.CreatedByUser == encryptedUser))
                                            & modelfilter.Where(x => (x.Status == "Deployed"));

                        projection1 = Builders<DATAMODELS.DeployModelsDto>.Projection.Exclude("_id");
                        deployModel = deployModelCollection.Find(modelfilterVal).Project<DATAMODELS.DeployModelsDto>(projection1).FirstOrDefault();
                    }


                }
            }
            else
            {
                // fetching the model is which either "in-progress" or "in error" 
                //won't fetch completed model 
                var deployModelCollection = _database.GetCollection<DATAMODELS.DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
                var modelfilter = Builders<DATAMODELS.DeployModelsDto>.Filter;
                var modelfilterVal = modelfilter.Where(x => x.ClientUId == clientId)
                                     & modelfilter.Where(x => x.DeliveryConstructUID == deliverConstructId)
                                     & modelfilter.Where(x => x.TemplateUsecaseId == usecaseId)
                                     & modelfilter.Where(x => (x.CreatedByUser == userId || x.CreatedByUser == encryptedUser))
                                     & modelfilter.Where(x => (x.Status != "Deployed"));

                var projection = Builders<DATAMODELS.DeployModelsDto>.Projection.Exclude("_id");
                deployModel = deployModelCollection.Find(modelfilterVal).Project<DATAMODELS.DeployModelsDto>(projection).FirstOrDefault();

                if (deployModel == null)
                {
                    //in case no model are in progress , then need to check if we had deployed trained models
                    modelfilterVal = modelfilter.Where(x => x.ClientUId == clientId)
                             & modelfilter.Where(x => x.DeliveryConstructUID == deliverConstructId)
                             & modelfilter.Where(x => x.TemplateUsecaseId == usecaseId)
                             & modelfilter.Where(x => (x.CreatedByUser == userId || x.CreatedByUser == encryptedUser))
                             & modelfilter.Where(x => (x.Status == "Deployed"));

                    projection = Builders<DATAMODELS.DeployModelsDto>.Projection.Exclude("_id");
                    deployModel = deployModelCollection.Find(modelfilterVal).Project<DATAMODELS.DeployModelsDto>(projection).FirstOrDefault();
                }
            }

            return deployModel;
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

        public bool CheckSSAITrainedModels(string clientUId, string deliveryConstructUId, string TemplateUseCaseId)
        {
            string encryptedUser = "SYSTEM";
            if (!string.IsNullOrEmpty(Convert.ToString(encryptedUser)))
            {
                encryptedUser = AesProvider.Encrypt(Convert.ToString(encryptedUser), appSettings.aesKey, appSettings.aesVector);
            }
            var deployedModels = _database.GetCollection<DATAMODELS.DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
            var filter = Builders<DATAMODELS.DeployModelsDto>.Filter.Where(x => x.ClientUId == clientUId)
                         & Builders<DATAMODELS.DeployModelsDto>.Filter.Where(x => x.DeliveryConstructUID == deliveryConstructUId)
                         & Builders<DATAMODELS.DeployModelsDto>.Filter.In("TemplateUsecaseId", TemplateUseCaseId)
                         & Builders<DATAMODELS.DeployModelsDto>.Filter.Where(x => (x.CreatedByUser == "SYSTEM" || x.CreatedByUser == encryptedUser))
                         & Builders<DATAMODELS.DeployModelsDto>.Filter.Where(x => x.Status == CONSTANTS.Deployed);
            var projection = Builders<DATAMODELS.DeployModelsDto>.Projection.Exclude("_id");
            var result = deployedModels.Find(filter).Project<DATAMODELS.DeployModelsDto>(projection).ToList();
            if (result.Count > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
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

        #endregion SSAI Flow

        #region AI Flow

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

        public BsonDocument CheckIfAIModelTrained(string clientId, string deliverConstructId, string usecaseId, string serviceId, string userId, string ServiceCode)
        {
            return CheckIfAIModelTrained(clientId, deliverConstructId, usecaseId, serviceId, userId, ServiceCode, string.Empty);
        }
        public BsonDocument CheckIfAIModelTrained(string clientId, string deliverConstructId, string usecaseId, string serviceId, string userId, string ServiceCode, string TeamAreaUID)
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
                var modelCollection1 = _database.GetCollection<BsonDocument>(CONSTANTS.Clustering_IngestData);
                var modelfilter1 = Builders<BsonDocument>.Filter;
                var modelfilterVal1 = modelfilter1.Empty;


                if (!string.IsNullOrEmpty(TeamAreaUID))
                {
                    modelfilter1 = Builders<BsonDocument>.Filter;
                    modelfilterVal1 = modelfilter1.Eq("TeamAreaUID", TeamAreaUID) & modelfilter1.Eq("UsecaseId", usecaseId) & modelfilter1.Eq("ClientID", clientId)
                                     & modelfilter1.Eq("DCUID", deliverConstructId);
                }
                else
                {
                    modelfilter1 = Builders<BsonDocument>.Filter;
                    modelfilterVal1 = modelfilter1.Eq("UsecaseId", usecaseId) & modelfilter1.Eq("ClientID", clientId)
                                     & modelfilter1.Eq("DCUID", deliverConstructId);
                }

                var projection1 = Builders<BsonDocument>.Projection.Exclude("_id");
                var model1 = modelCollection1.Find(modelfilterVal1).Project<BsonDocument>(projection1).FirstOrDefault();

                modelfilterVal = modelfilter.Eq("ClientID", clientId) & modelfilter.Eq("CorrelationId", model1["CorrelationId"].ToString())
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

        public bool CheckAITrainedModels(string clientUId, string deliveryConstructUId, string TemplateUseCaseId)
        {
            string encryptedUser = "SYSTEM";
            if (!string.IsNullOrEmpty(Convert.ToString(encryptedUser)))
            {
                encryptedUser = AesProvider.Encrypt(Convert.ToString(encryptedUser), appSettings.aesKey, appSettings.aesVector);
            }
            var aiCoreModels = _database.GetCollection<AICoreModels>(CONSTANTS.AICoreModels);
            var filter = Builders<AICoreModels>.Filter.Where(x => x.ClientId == clientUId)
                         & Builders<AICoreModels>.Filter.Where(x => x.DeliveryConstructId == deliveryConstructUId)
                         & Builders<AICoreModels>.Filter.In("UsecaseId", TemplateUseCaseId)
                         & Builders<AICoreModels>.Filter.Where(x => (x.CreatedBy == "SYSTEM" || x.CreatedBy == encryptedUser))
                         & Builders<AICoreModels>.Filter.Where(x => x.ModelStatus == "Completed");
            var projection = Builders<AICoreModels>.Projection.Exclude("_id");
            var result = aiCoreModels.Find(filter).Project<AICoreModels>(projection).ToList();
            if (result.Count > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool DeleteAIModel(string correlationId)
        {
            //Check it is cluster model or not
            var clustercollection = _database.GetCollection<BsonDocument>(CONSTANTS.Clustering_IngestData);
            var clusterFilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId.Trim());
            var clusterResult = clustercollection.Find(clusterFilter).ToList();
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DBConnection), "DeleteAIModel CLUSTERRESULT - " + clusterResult.Count, "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty, CONSTANTS.CustomConstraintsLog);
            bool isDeleted = false;
            if (clusterResult.Count > 0)
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(DBConnection), "DeleteAIModel", "Clustering START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty, CONSTANTS.CustomConstraintsLog);
                //check model status and delete
                var clusterStatusCollection = _database.GetCollection<ClusterStatusModel>(CONSTANTS.Clustering_StatusTable);
                var clusterStatusFilter = Builders<ClusterStatusModel>.Filter.Eq(CONSTANTS.CorrelationId, correlationId.Trim());
                var clusterProjection = Builders<ClusterStatusModel>.Projection.Include(CONSTANTS.CorrelationId).Include(CONSTANTS.ModifiedOn).Include(CONSTANTS.Status).Exclude(CONSTANTS.Id);
                var clusterStatusResult = clusterStatusCollection.Find(clusterStatusFilter).Project<ClusterStatusModel>(clusterProjection).ToList();
                if (clusterStatusResult.Count > 0)
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(DBConnection), "DeleteAIModel", "Clustering2 START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty, CONSTANTS.CustomConstraintsLog);
                    //Check if model inprogress for more than 2 days than delete it. defect 1625320             
                    var inprogressModels = clusterStatusResult.Where(x => x.Status == CONSTANTS.P).ToList();
                    if (inprogressModels.Count > 0)
                    {
                        DateTime currentTime = DateTime.Now;
                        DateTime modelDateTime = DateTime.Parse(inprogressModels[0].ModifiedOn);
                        double diffInDays = (currentTime - modelDateTime).TotalDays;
                        if (diffInDays >= 1)
                        {
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DBConnection), "DeleteAIModel", "Clustering3 START DIFFINDAYS--" + diffInDays, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty, CONSTANTS.CustomConstraintsLog);
                            //Delete cluster model
                            isDeleted = DeleteClusterModels(correlationId);
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DBConnection), "DeleteAIModel DeleteAIModel - " + isDeleted, "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty, CONSTANTS.CustomConstraintsLog);
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(DBConnection), "DeleteAIModel ELSE", "Clustering START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty, CONSTANTS.CustomConstraintsLog);
                        //Delete cluster model with Status Completed, Warninig and Error.
                        isDeleted = DeleteClusterModels(correlationId);
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(DBConnection), "DeleteAIModel DeleteAIModel - " + isDeleted, "START", string.Empty, string.Empty, string.Empty, string.Empty, CONSTANTS.CustomConstraintsLog);
                    }
                }
                else
                    isDeleted = DeleteClusterModels(correlationId);
                if (isDeleted)
                    return true;
            }
            else
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(DBConnection), "DeleteAIModel", "AIModel START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty, CONSTANTS.CustomConstraintsLog);
                AICoreModels aICoreModels = GetAICoreModelPath(correlationId);
                if (aICoreModels != null && aICoreModels.CorrelationId != null)
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(DBConnection), "DeleteAIModel AICOREMODELSMODELSTATUS - " + aICoreModels.ModelStatus, "START", string.IsNullOrEmpty(aICoreModels.CorrelationId) ? default(Guid) : new Guid(aICoreModels.CorrelationId), string.Empty, string.Empty, string.Empty, string.Empty, CONSTANTS.CustomConstraintsLog);
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
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DBConnection), "DeleteAIModel DeleteAIModel - " + isDeleted, "START", string.IsNullOrEmpty(aICoreModels.CorrelationId) ? default(Guid) : new Guid(aICoreModels.CorrelationId), string.Empty, string.Empty, string.Empty, string.Empty, CONSTANTS.CustomConstraintsLog);
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
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(DBConnection), nameof(DeleteAIModel), ex.Message, ex, aICoreModels.ApplicationId, string.Empty, aICoreModels.ClientId, aICoreModels.DeliveryConstructId, CONSTANTS.CustomConstraintsLog);
            }

            //Delete Model
            var aICoreModel = _database.GetCollection<BsonDocument>(CONSTANTS.AICoreModels);
            result = aICoreModel.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                aICoreModel.DeleteMany(filter);
            }
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

        private bool DeleteClusterModels(string correlationId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DBConnection), "DeleteClusterModels", "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty, CONSTANTS.CustomConstraintsLog);
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
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(DBConnection), "DeleteClusterModels DELETECOUNT-" + res.DeletedCount, "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty, CONSTANTS.CustomConstraintsLog);
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
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(DBConnection), nameof(DeleteClusterModels), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty, CONSTANTS.CustomConstraintsLog);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DBConnection), "DeleteClusterModels", "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty, CONSTANTS.CustomConstraintsLog);
            return isdeleted;
        }

        public void InsertAICustomConstraintsRequests(string NewcorrelationId, string TemplateUseCaseID)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DBConnection), nameof(InsertAICustomConstraintsRequests), "Data Insertion started for Custom Constraints for correlation ID --" + NewcorrelationId, string.IsNullOrEmpty(NewcorrelationId) ? default(Guid) : new Guid(NewcorrelationId), "", string.Empty, "", "", CONSTANTS.CustomConstraintsLog);
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
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(DBConnection), nameof(InsertAICustomConstraintsRequests), "Data Insertion Completed for Ingest Data for corelation ID --" + NewcorrelationId, string.IsNullOrEmpty(NewcorrelationId) ? default(Guid) : new Guid(NewcorrelationId), "", string.Empty, "", "", CONSTANTS.CustomConstraintsLog);
            }
        }

        #endregion AI Flow

        public List<WINSERVICEMODELS.PhoenixIterations> GetNotRecommendedIterations()
        {
            var response = new List<WINSERVICEMODELS.PhoenixIterations>();
            var collection = _database.GetCollection<WINSERVICEMODELS.PhoenixIterations>(CONSTANTS.PhoenixIterations);
            var filter = Builders<WINSERVICEMODELS.PhoenixIterations>.Filter.Where(x => x.isRecommendationCompleted == false)//pheonix DB recoomendation update
                                                                                                                             //& Builders<WINSERVICEMODELS.PhoenixIterations>.Filter.Gte(x => x.LastRecordsPullUpdateTime, pullTime)
                         & Builders<WINSERVICEMODELS.PhoenixIterations>.Filter.Where(x => x.IsCustomConstraintModel == true);
            var projection = Builders<WINSERVICEMODELS.PhoenixIterations>.Projection.Exclude("_id");
            var result = collection.Find(filter).Project<WINSERVICEMODELS.PhoenixIterations>(projection).ToList();
            return result;
        }

        public int CheckSPIDefectRateFrequency(string TemplateUsecaseId)
        {
            int predictionFrequency = 0;

            var deployedModels = _database.GetCollection<DATAMODELS.DeployModelsDto>(CONSTANTS.SSAIDeployedModels);

            var filter = Builders<DATAMODELS.DeployModelsDto>.Filter.Eq("CorrelationId", TemplateUsecaseId)
                         & Builders<DATAMODELS.DeployModelsDto>.Filter.Where(x => x.Status == CONSTANTS.Deployed);

            var projection = Builders<DATAMODELS.DeployModelsDto>.Projection.Exclude("_id");
            var result = deployedModels.Find(filter).Project<DATAMODELS.DeployModelsDto>(projection).ToList();
            if (result.Count > 0)
            {
                predictionFrequency = result[0].PredictionFrequencyInDays;
            }
            return predictionFrequency;
        }

        public void UpdateIterationRecommendationStatusinDB(string clientUId, string deliveryConstructUId, string itemUId, bool status, string message)
        {
            var collection = _database.GetCollection<WINSERVICEMODELS.PhoenixIterations>(CONSTANTS.PhoenixIterations);
            var filter = Builders<WINSERVICEMODELS.PhoenixIterations>.Filter.Where(x => x.ClientUId == clientUId)
                         & Builders<WINSERVICEMODELS.PhoenixIterations>.Filter.Where(x => x.DeliveryConstructUId == deliveryConstructUId)
                         & Builders<WINSERVICEMODELS.PhoenixIterations>.Filter.Where(x => x.ItemUId == itemUId);
            var projection = Builders<WINSERVICEMODELS.PhoenixIterations>.Projection.Exclude("_id");
            var result = collection.Find(filter).Project<WINSERVICEMODELS.PhoenixIterations>(projection).FirstOrDefault();
            if (result != null)
            {
                if (status)
                {
                    var update = Builders<WINSERVICEMODELS.PhoenixIterations>.Update.Set(x => x.isRecommendationCompleted, true)
                                                                                    .Set(x => x.Status, "C")
                                                                                                    .Set(x => x.LastRecommendationUpdateTime, DateTime.UtcNow)
                                                                                                    .Set(x => x.Message, message)
                                                                                                    .Set(x => x.ModifiedOn, DateTime.UtcNow);
                    collection.UpdateOne(filter, update);
                }
                else
                {
                    var update = Builders<WINSERVICEMODELS.PhoenixIterations>.Update.Set(x => x.Message, message)
                                                                                    .Set(x => x.isRecommendationCompleted, true)
                                                                                    .Set(x => x.Status, "E")
                                                                                    .Set(x => x.ModifiedOn, DateTime.UtcNow);
                    collection.UpdateOne(filter, update);
                }


            }

        }

        public void UpdateBulkIterationRecommendationStatusinDB(string clientUId, string deliveryConstructUId, List<string> itemUId, bool status, string message)
        {
            var collection = _database.GetCollection<WINSERVICEMODELS.PhoenixIterations>(CONSTANTS.PhoenixIterations);
            var filter = Builders<WINSERVICEMODELS.PhoenixIterations>.Filter.Where(x => x.ClientUId == clientUId)
                         & Builders<WINSERVICEMODELS.PhoenixIterations>.Filter.Where(x => x.DeliveryConstructUId == deliveryConstructUId)
                         & Builders<WINSERVICEMODELS.PhoenixIterations>.Filter.In("ItemUId", itemUId);
            var projection = Builders<WINSERVICEMODELS.PhoenixIterations>.Projection.Exclude("_id");
            var result = collection.Find(filter).Project<WINSERVICEMODELS.PhoenixIterations>(projection).ToList();
            if (result.Count > 0)
            {
                if (status)
                {
                    var update = Builders<WINSERVICEMODELS.PhoenixIterations>.Update.Set(x => x.isRecommendationCompleted, true)
                                                                                    .Set(x => x.Status, "C")
                                                                                                    .Set(x => x.LastRecommendationUpdateTime, DateTime.UtcNow)
                                                                                                    .Set(x => x.Message, message)
                                                                                                    .Set(x => x.ModifiedOn, DateTime.UtcNow);
                    collection.UpdateMany(filter, update);
                }
                else
                {
                    var update = Builders<WINSERVICEMODELS.PhoenixIterations>.Update.Set(x => x.Message, message)
                                                                                    .Set(x => x.isRecommendationCompleted, true)
                                                                                    .Set(x => x.Status, "E")
                                                                                    .Set(x => x.ModifiedOn, DateTime.UtcNow);
                    collection.UpdateMany(filter, update);
                }


            }

        }

        public List<DATAMODELS.ENSEntityNotificationLog> ReadCRENSNotification()
        {
            var ensLogCollection = _database.GetCollection<DATAMODELS.ENSEntityNotificationLog>(nameof(DATAMODELS.ENSEntityNotificationLog));
            var filter = Builders<DATAMODELS.ENSEntityNotificationLog>.Filter;
            var filterQuery = filter.Where(x => !x.isProcessed)
                              & (filter.Where(x => x.RetryCount == null) | filter.Lt(x => x.RetryCount, 3));
            return ensLogCollection.Find(filterQuery).ToList();
        }

        public void UpdateNotificationStatus(ObjectId id, bool isProcessed, string status, string statusMessage)
        {
            var ensLogCollection = _database.GetCollection<DATAMODELS.ENSEntityNotificationLog>(CONSTANTS.ENSEntityNotificationLog);
            var filter = Builders<DATAMODELS.ENSEntityNotificationLog>.Filter.Where(x => x._id == id);
            var projection = Builders<DATAMODELS.ENSEntityNotificationLog>.Projection.Exclude("_id");
            var result = ensLogCollection.Find(filter).Project<DATAMODELS.ENSEntityNotificationLog>(projection).FirstOrDefault();
            if (result != null)
            {
                int? retryCount = result.RetryCount == null ? 1 : result.RetryCount + 1;

                var updateBuilder = Builders<DATAMODELS.ENSEntityNotificationLog>.Update.Set(x => x.isProcessed, isProcessed)
                                                                     .Set(x => x.ProcessedStatus, status)
                                                                     .Set(x => x.ProcessedStatusMessage, statusMessage)
                                                                     .Set(x => x.RetryCount, retryCount)
                                                                     .Set(x => x.ModifiedOn, DateTime.UtcNow.ToString());
                ensLogCollection.UpdateOne(filter, updateBuilder);

            }
        }


        public void SaveIterationIdstoDB(string clientUId, string deliveryConstructUId, string iterationUId, string endOn, string methodologyUId, string TrainedModelCorrelationID, string TemplateUsecaseId)
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
                            ModifiedOn = DateTime.UtcNow,
                            IsCustomConstraintModel = true,
                            TrainedModelCorrelationID = TrainedModelCorrelationID,
                            TemplateUsecaseId = TemplateUsecaseId
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

        public void UpdateIterationstoDB(WINSERVICEMODELS.PhoenixPayloads.IterationQueryResponse iterations, string clientUId, string deliveryConstructUId, string TrainedModelCorrelationID, string TemplateUsecaseId)
        {
            if (iterations != null)
            {
                if (iterations.Iterations.Count > 0)
                {
                    foreach (var iteration in iterations.Iterations)
                    {
                        // consider only waterfall releases (common for all apps if Active Release selected)
                        if (iteration.MethodologyUId == "00200870-0020-0000-0000-000000000000" || iteration.MethodologyUId == "00200870-0030-0000-0000-000000000000")
                        {
                            SaveIterationIdstoDB(clientUId, deliveryConstructUId, iteration.IterationUId, iteration.EndOn, iteration.MethodologyUId, TrainedModelCorrelationID, TemplateUsecaseId);

                        }

                    }
                }
            }
        }

        #region Training Status
        public void SetModelStatus(string ModelTemplateName, string CorrelationId , string servicetype , string Status)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DBConnection), nameof(SetModelStatus), "Model Training Status - 'In - Progress' for : " + CorrelationId, string.IsNullOrEmpty(CorrelationId.ToString()) ? default(Guid) : new Guid(CorrelationId.ToString()), "", string.Empty, "", "", CONSTANTS.CustomConstraintsLog);

            string _TrainingStatus = string.Empty, _PredictionStatus = string.Empty, _RetrainingStatus = string.Empty;
            var collection = _database.GetCollection<DATAMODELS.ModelRequestStatus>(CONSTANTS.SSAIModelRequestStatus);
            var builder = Builders<DATAMODELS.ModelRequestStatus>.Filter;
            var filter = builder.Eq("CorrelationId", CorrelationId);
            var Projection = Builders<DATAMODELS.ModelRequestStatus>.Projection.Exclude("_id");
            var oResponse = collection.Find(filter).Project<DATAMODELS.ModelRequestStatus>(Projection).ToList();


            switch (servicetype)
            {
                case CONSTANTS.TrainingConstraint:
                   _TrainingStatus = Status;
                    break;

                case CONSTANTS.PredictionConstraint:
                    _PredictionStatus = Status;
                    break;

                case CONSTANTS.ReTrainingConstraint:
                    _RetrainingStatus = Status;
                    break;

                default:
                    break;
            }
                
            if (oResponse != null && oResponse.Count == 0)
            {
                // Insert a New document
                DATAMODELS.ModelRequestStatus oModelStatus = new DATAMODELS.ModelRequestStatus
                {
                    _id = Guid.NewGuid().ToString(),
                    ModelTemplateName = ModelTemplateName,
                    CorrelationId = CorrelationId,
                    TrainingStatus = _TrainingStatus,
                    PredictionStatus = _PredictionStatus,
                    RetrainingStatus = _RetrainingStatus,
                    CreatedOn = DateTime.UtcNow.ToString(),
                    CreatedByUser = "SYSTEM - CUSTOM"
                };

                var jsonData = JsonConvert.SerializeObject(oModelStatus);
                var insertDocument = BsonSerializer.Deserialize<DATAMODELS.ModelRequestStatus>(jsonData);
                collection.InsertOne(insertDocument);
            }
            else
            {
                var update = Builders<DATAMODELS.ModelRequestStatus>.Update.Set(CONSTANTS.TrainingConstraint+"Status", Status)
                                                                        .Set(x => x.ModifiedOn, DateTime.UtcNow.ToString());
                collection.UpdateOne(filter, update);
            }
        }

        public DATAMODELS.ModelRequestStatus GetModelStatus(string ModelTemplateName, string CorrelationId)
        {
            DATAMODELS.ModelRequestStatus oResponse = null;
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DBConnection), nameof(GetModelStatus), "Model Training Status for : " + CorrelationId, string.IsNullOrEmpty(CorrelationId.ToString()) ? default(Guid) : new Guid(CorrelationId.ToString()), "", string.Empty, "", "", CONSTANTS.CustomConstraintsLog);
            var collection = _database.GetCollection<DATAMODELS.ModelRequestStatus>(CONSTANTS.SSAIModelRequestStatus);
            var builder = Builders<DATAMODELS.ModelRequestStatus>.Filter;
            var filter = builder.Eq("CorrelationId", CorrelationId);
            var Projection = Builders<DATAMODELS.ModelRequestStatus>.Projection.Exclude("_id");
            var oStatusresponse = collection.Find(filter).Project<DATAMODELS.ModelRequestStatus>(Projection).ToList();

            if (oStatusresponse != null && oStatusresponse.Count > 0)
            {
                oResponse = oStatusresponse[0];
                return oResponse;
            }
            return oResponse;
        }
        
        public List<DATAMODELS.ModelRequestStatus> GetModelStatusList()
        {
            var collection = _database.GetCollection<DATAMODELS.ModelRequestStatus>(CONSTANTS.SSAIModelRequestStatus);
            var builder = Builders<DATAMODELS.ModelRequestStatus>.Filter;
            var filter = builder.Empty;
            var Projection = Builders<DATAMODELS.ModelRequestStatus>.Projection.Exclude("_id");
            var oStatusresponse = collection.Find(filter).Project<DATAMODELS.ModelRequestStatus>(Projection).ToList();

            if (oStatusresponse != null && oStatusresponse.Count > 0)
            {
                return oStatusresponse;
            }
            return oStatusresponse;
        }
        #endregion

    }
}
