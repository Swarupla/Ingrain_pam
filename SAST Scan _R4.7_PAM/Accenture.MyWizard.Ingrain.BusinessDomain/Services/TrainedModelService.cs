using System;
using System.Collections.Generic;
using System.Text;
using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
using Accenture.MyWizard.Ingrain.DataAccess;
using Accenture.MyWizard.Ingrain.DataModels.Models;
using Accenture.MyWizard.Shared.Helpers;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using MongoDB.Bson;
using System.Linq;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
using Newtonsoft.Json;
using Accenture.MyWizard.Ingrain.DataModels.AICore;
using Newtonsoft.Json.Linq;
using Accenture.MyWizard.Ingrain.DataModels.InferenceEngineModel;

namespace Accenture.MyWizard.Ingrain.BusinessDomain.Services
{
    public class TrainedModelService : ITrainedModelService
    {
        private MongoClient _mongoClient;
        private IMongoDatabase _database;
        private IMongoDatabase _IEdatabase;
        private readonly DatabaseProvider databaseProvider;
        private readonly IOptions<IngrainAppSettings> appSettings;
        private IEncryptionDecryption _encryptionDecryption;
        private IAICoreService _aiCoreService;
        private static IClusteringAPIService _clusteringAPI { get; set; }
        #region Constructors

        enum TemplateType
        {
            InferenceEngine,
            Clustering,
            Other

        }

        enum ServiceType
        {
            SSAI,
            AI
        }

        public TrainedModelService(DatabaseProvider db, IOptions<IngrainAppSettings> settings, IServiceProvider serviceProvider)
        {
            databaseProvider = db;
            appSettings = settings;
            _mongoClient = databaseProvider.GetDatabaseConnection();
            var dataBaseName = MongoUrl.Create(appSettings.Value.connectionString).DatabaseName;
            _database = _mongoClient.GetDatabase(dataBaseName);
            _encryptionDecryption = serviceProvider.GetService<IEncryptionDecryption>();


            var IEdataBaseName = MongoUrl.Create(appSettings.Value.IEConnectionString).DatabaseName;
            _IEdatabase = _mongoClient.GetDatabase(IEdataBaseName);
            _aiCoreService = serviceProvider.GetService<IAICoreService>();
            _clusteringAPI = serviceProvider.GetService<IClusteringAPIService>();
        }
        #endregion

        public class IngrainRequest
        {
            public string AppID { get; set; }
            public string CorrelationId { get; set; }
            public string RequestId { get; set; }
            public string ClientId { get; set; }
            public string DeliveryconstructId { get; set; }
            public string Status { get; set; }
            public string ModelName { get; set; }
            public string RequestStatus { get; set; }
            public string Message { get; set; }
            public string Progress { get; set; }
            public string pageInfo { get; set; }
            public string Function { get; set; }
            public string TemplateUseCaseID { get; set; }
            public string CreatedByUser { get; set; }
            public string FunctionPageInfo { get; set; }
        }
        public dynamic GetTrainedModels(ModelTemplateInput oModelTemplateInput, string serviceType)
        {
            Dictionary<string, List<BsonDocument>> oTrainedModelList = null;
            if (serviceType.ToUpper() == ServiceType.SSAI.ToString().ToUpper())
            {
                //Checking if ModelTemplate Exists for the provided CorrelationId
                var Collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
                var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, oModelTemplateInput.CorrelationId);
                var oModelTemplate = Collection.Find(filter).FirstOrDefault();

                if (oModelTemplate != null)
                {
                    //If Model Template Exists , fetch Trained Models using provided Correlation id as TemplateUseCasId
                    oTrainedModelList = new Dictionary<string, List<BsonDocument>>();

                    filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.TemplateUsecaseId, oModelTemplate["CorrelationId"].ToString());
                    var oTraineModelList = Collection.Find(filter).ToList();

                    if (oTraineModelList.Count > 0)
                    {
                        oTrainedModelList.Add(oModelTemplate["ModelName"].ToString(), oTraineModelList);
                    }
                }
            }
            else if (serviceType.ToUpper() == ServiceType.AI.ToString().ToUpper())
            {
                Service oService = _aiCoreService.GetAiCoreServiceDetails(oModelTemplateInput.ServiceId);

                if (oService != null && string.IsNullOrEmpty(oService.ServiceId))
                {
                    throw new Exception("Service Id is not correct");
                }

                var modelCollection = _database.GetCollection<JObject>(CONSTANTS.Clustering_IngestData);
                var modelfilter = Builders<JObject>.Filter;
                var modelfilterVal = modelfilter.Empty;

                var projection = Builders<JObject>.Projection.Exclude("_id");

                if (oService.ServiceCode == "CLUSTERING" || oService.ServiceCode == "WORDCLOUD")
                {
                    //Check if "Published Use Case" exists for CorrelationID [of Parent Model]
                    modelCollection = _database.GetCollection<JObject>(CONSTANTS.AISavedUsecases);

                    modelfilterVal = modelfilter.Ne("UsecaseId", CONSTANTS.Null)
                                   & modelfilter.Eq("CorrelationId", oModelTemplateInput.CorrelationId.ToString());


                    var oUseCases = modelCollection.Find(modelfilterVal).Project<BsonDocument>(projection).ToList();

                    if (oUseCases.Count > 0)
                    {
                        //if Models in Training exists, fetch the status of the Model
                        foreach (var oUseCase in oUseCases)
                        {
                            oTrainedModelList = new Dictionary<string, List<BsonDocument>>();

                            modelCollection = _database.GetCollection<JObject>(CONSTANTS.Clustering_IngestData);
                            modelfilterVal = (modelfilter.Eq("UsecaseId", oUseCase["UsecaseId"].ToString()) | modelfilter.Eq("ModelName", oUseCase["ModelName"].ToString()))
                                             & modelfilter.Ne("CorrelationId", oUseCase["CorrelationId"].ToString());

                            var oModelsInTraining = modelCollection.Find(modelfilterVal).Project<BsonDocument>(projection).ToList();

                            foreach (var oModelinTraining in oModelsInTraining)
                            {
                                modelCollection = _database.GetCollection<JObject>(CONSTANTS.Clustering_StatusTable);
                                modelfilterVal = modelfilter.Ne("CorrelationId", oModelinTraining["CorrelationId"].ToString());

                                var oTrainedModels = modelCollection.Find(modelfilterVal).Project<BsonDocument>(projection).ToList();

                                if (oTrainedModels.Count > 0)
                                {
                                    oTrainedModelList.Add(oUseCase["UsecaseName"].ToString() + "_" + oModelinTraining["CorrelationId"].ToString(), oTrainedModels.ToList());
                                }
                            }
                        }
                    }
                }
                else
                {
                    //Fetching List of All Published Used Cases using CorrelationID of Parent Model
                    modelCollection = _database.GetCollection<JObject>(CONSTANTS.AISavedUsecases);
                    modelfilterVal = modelfilter.Eq("CorrelationId", oModelTemplateInput.CorrelationId);
                    var oPublishedUseCaseList = modelCollection.Find(modelfilterVal).Project<BsonDocument>(projection).ToList();

                    if (oPublishedUseCaseList.Count > 0)
                    {
                        //if PublishedUseCases Exists , then Fetch List of "Trained Models/Models in Training"
                        oTrainedModelList = new Dictionary<string, List<BsonDocument>>();
                        foreach (var oUseCase in oPublishedUseCaseList)
                        {
                            // Fetch List of "Trained Models/Models in Training" using "UseCaseId" of PublishedUseCase
                            modelCollection = _database.GetCollection<JObject>(CONSTANTS.AIServiceRequestStatus);
                            modelfilterVal = modelfilter.Eq("PageInfo", "Ingest_Train")
                                             & modelfilter.Eq("UsecaseId", oUseCase["UsecaseId"].ToString());
                            var oModelsInTraining = modelCollection.Find(modelfilterVal).Project<BsonDocument>(projection).ToList();

                            if (oModelsInTraining.Count > 0)
                            {
                                oTrainedModelList.Add(oUseCase["UsecaseName"].ToString(), oModelsInTraining);
                            }
                        }
                    }

                }
            }

            return JsonConvert.DeserializeObject<JObject>(oTrainedModelList.ToJson());

        }
    }
}
