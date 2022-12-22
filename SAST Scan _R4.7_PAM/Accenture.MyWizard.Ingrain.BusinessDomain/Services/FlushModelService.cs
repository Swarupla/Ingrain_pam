using Accenture.MyWizard.Cryptography.EncryptionProviders;
using Accenture.MyWizard.Ingrain.BusinessDomain.Common;
using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
using Accenture.MyWizard.Ingrain.DataAccess;
using Accenture.MyWizard.Ingrain.DataModels.Models;
using Accenture.MyWizard.Shared.Helpers;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Configuration;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
using MongoDB.Bson.Serialization;
using CryptographyHelper = Accenture.MyWizard.Cryptography;

namespace Accenture.MyWizard.Ingrain.BusinessDomain.Services
{
    public class FlushModelService : IFlushService
    {
        #region Members
        private MongoClient _mongoClient;
        private IMongoDatabase _database;
        private readonly IOptions<IngrainAppSettings> appSettings;
        DatabaseProvider databaseProvider;
        static CryptographyHelper.Utility.CryptographyUtility CryptographyUtility = null;
        private MongoClient _mongoClientAD;
        private IMongoDatabase _databaseAD;
        #endregion

        #region Constructors
        /// <summary>
        /// Constructor to Initialize mongoDB Connection
        /// </summary>
        public FlushModelService(DatabaseProvider db, IOptions<IngrainAppSettings> settings)
        {
            databaseProvider = db;
            appSettings = settings;
            _mongoClient = databaseProvider.GetDatabaseConnection();
            var dataBaseName = MongoUrl.Create(appSettings.Value.connectionString).DatabaseName;
            _database = _mongoClient.GetDatabase(dataBaseName);
            CryptographyUtility = new CryptographyHelper.Utility.CryptographyUtility();
            //Anomaly Detection Connection
            _mongoClientAD = databaseProvider.GetDatabaseConnection("Anomaly");
            var dataBaseNameAD = MongoUrl.Create(appSettings.Value.AnomalyDetectionCS).DatabaseName;
            _databaseAD = _mongoClientAD.GetDatabase(dataBaseNameAD);
        }
        #endregion

        public string FlushModel(string CorrelationId, string flushFlag, string ServiceName = "")
        {
            string flushStatus = string.Empty;
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, CorrelationId);
            var cascadeFilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CascadedId, CorrelationId);
            var cascadeProjection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
            IMongoCollection<DeployModelsDto> deployModelCollection;
            IMongoCollection<BsonDocument> collection_savedModels;
            IMongoCollection<BsonDocument> collection_IngestedData;
            if (ServiceName == "Anomaly")
            {
                deployModelCollection = _databaseAD.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
                collection_savedModels = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.SSAI_savedModels);
                collection_IngestedData = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.PSIngestedData);
            }
            else
            {
                deployModelCollection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
                collection_savedModels = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_savedModels);
                collection_IngestedData = _database.GetCollection<BsonDocument>(CONSTANTS.PSIngestedData);
            }
            //var deployModelCollection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
            var filter2 = Builders<DeployModelsDto>.Filter.Where(x => x.CorrelationId == CorrelationId);
            var mdl = deployModelCollection.Find(filter2).FirstOrDefault();
            List<string> appLst = new List<string>() { CONSTANTS.VDSApplicationID_PAD, CONSTANTS.VDSApplicationID_FDS, CONSTANTS.VDSApplicationID_PAM };
            if (mdl != null)
            {
                if (mdl.IsCascadeModel)
                {
                    if (mdl.IsCascadeModel && !mdl.IsModelTemplate)
                    {
                        if (appLst.Contains(mdl.AppId))
                            SendVDSDeployModelNotification(mdl, OperationTypes.Deleted.ToString(), true);
                    }
                }
                else
                {
                    if (appLst.Contains(mdl.AppId))
                    {
                        SendVDSDeployModelNotification(mdl, OperationTypes.Deleted.ToString(), false);
                    }
                }
            }


            //var collection_savedModels = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_savedModels);
            var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.FilePath).Exclude(CONSTANTS.Id);
            var correaltionExist = collection_savedModels.Find(filter).Project<BsonDocument>(projection).ToList();
            if (correaltionExist.Count > 0)
            {
                for (int i = 0; i < correaltionExist.Count; i++)
                {
                    string FileToDelete = correaltionExist[i][CONSTANTS.FilePath].ToString();
                    if (File.Exists(FileToDelete))
                    { File.Delete(FileToDelete); }
                }

                collection_savedModels.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.SuccessSSAI_savedModels;
            }
            if (flushFlag == CONSTANTS.FlushIngestedData)
            {
                //var collection_IngestedData = _database.GetCollection<BsonDocument>(CONSTANTS.PSIngestedData);
                if (collection_IngestedData.Find(filter).ToList().Count > 0)
                {
                    collection_IngestedData.DeleteMany(filter);
                    flushStatus = flushStatus + CONSTANTS.SuccessPS_IngestedData;
                }
            }
            else
            {
                IMongoCollection<BsonDocument> collection_BusinessProblem;
                IMongoCollection<BsonDocument> collection_DataCleanup;
                IMongoCollection<BsonDocument> collection_DataProcessing;
                IMongoCollection<BsonDocument> collection_DeployedPublishModel;
                IMongoCollection<BsonDocument> collection_IngrainDeliveryConstruct;
                IMongoCollection<BsonDocument> collection_HyperTuneVersion;
                IMongoCollection<BsonDocument> collection_SSAI_RecommendedTrainedModels;
                IMongoCollection<BsonDocument> collection_SSAI_UserDetails;
                IMongoCollection<BsonDocument> collection_WF_IngestedData;
                IMongoCollection<BsonDocument> collection_WF_TestResults;
                IMongoCollection<BsonDocument> collection_WhatIfAnalysis;
                IMongoCollection<BsonDocument> collection_DataVisualization;
                IMongoCollection<BsonDocument> collection_PreProcessedData  ;
                IMongoCollection<BsonDocument> collection_FilteredData ;
                IMongoCollection<BsonDocument> collection_FeatureSelection  ;
                IMongoCollection<BsonDocument> collection_RecommendedModels ;
                IMongoCollection<BsonDocument> collection_TeachAndTest ;
                IMongoCollection<BsonDocument> collection_DeliveryConstructStructures;
                IMongoCollection<BsonDocument> collection_DeployedModels ;
                IMongoCollection<BsonDocument> collection_PublicTemplates ;
                IMongoCollection<BsonDocument> collection_PublishModel ;
                IMongoCollection<BsonDocument> collection_UseCase ;
                IMongoCollection<BsonDocument> collection_CascadeModel ;
                IMongoCollection<BsonDocument> collection_SSAIIngrainRequests;
                IMongoCollection<BsonDocument> SSAICascadeVisualization ;
                IMongoCollection<BsonDocument> SSAIFMVisualization;
                if (ServiceName == "Anomaly")
                {
                    collection_BusinessProblem = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.PSBusinessProblem);
                    collection_DataCleanup = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.DEDataCleanup);
                    collection_DataProcessing = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.DEDataProcessing);

                    collection_DeployedPublishModel = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.DeployedPublishModel);
                    collection_IngrainDeliveryConstruct = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.IngrainDeliveryConstruct);
                    collection_HyperTuneVersion = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.ME_HyperTuneVersion);
                    collection_SSAI_RecommendedTrainedModels = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.SSAIRecommendedTrainedModels);
                    collection_SSAI_UserDetails = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.SSAIUserDetails);
                    collection_WF_IngestedData = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.WF_IngestedData);
                    collection_WF_TestResults = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.WF_TestResults_);
                    collection_WhatIfAnalysis = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.WhatIfAnalysis);

                    collection_DataVisualization = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.DE_DataVisualization);
                    collection_PreProcessedData = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.DEPreProcessedData);
                    collection_FilteredData = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.DataCleanUPFilteredData);
                    collection_FeatureSelection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.MEFeatureSelection);
                    collection_RecommendedModels = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.MERecommendedModels);
                    collection_TeachAndTest = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.ME_TeachAndTest);

                    collection_DeliveryConstructStructures = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.SSAI_DeliveryConstructStructures);
                    collection_DeployedModels = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
                    collection_PublicTemplates = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.SSAIPublicTemplates);
                    collection_PublishModel = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.SSAI_PublishModel);
                    collection_UseCase = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.SSAIUseCase);
                    collection_CascadeModel = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.SSAICascadedModels);
                    collection_SSAIIngrainRequests = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
                    SSAICascadeVisualization = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.SSAICascadeVisualization);
                    SSAIFMVisualization = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.SSAIFMVisualization);

                }
                else
                {
                    collection_BusinessProblem = _database.GetCollection<BsonDocument>(CONSTANTS.PSBusinessProblem);
                    collection_DataCleanup = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataCleanup);
                    collection_DataProcessing = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataProcessing);
                    collection_IngestedData = _database.GetCollection<BsonDocument>(CONSTANTS.PSIngestedData);

                    collection_DeployedPublishModel = _database.GetCollection<BsonDocument>(CONSTANTS.DeployedPublishModel);
                    collection_IngrainDeliveryConstruct = _database.GetCollection<BsonDocument>(CONSTANTS.IngrainDeliveryConstruct);
                    collection_HyperTuneVersion = _database.GetCollection<BsonDocument>(CONSTANTS.ME_HyperTuneVersion);
                    collection_SSAI_RecommendedTrainedModels = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIRecommendedTrainedModels);
                    collection_SSAI_UserDetails = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIUserDetails);
                    collection_WF_IngestedData = _database.GetCollection<BsonDocument>(CONSTANTS.WF_IngestedData);
                    collection_WF_TestResults = _database.GetCollection<BsonDocument>(CONSTANTS.WF_TestResults_);
                    collection_WhatIfAnalysis = _database.GetCollection<BsonDocument>(CONSTANTS.WhatIfAnalysis);

                    collection_DataVisualization = _database.GetCollection<BsonDocument>(CONSTANTS.DE_DataVisualization);
                    collection_PreProcessedData = _database.GetCollection<BsonDocument>(CONSTANTS.DEPreProcessedData);
                    collection_FilteredData = _database.GetCollection<BsonDocument>(CONSTANTS.DataCleanUPFilteredData);
                    collection_FeatureSelection = _database.GetCollection<BsonDocument>(CONSTANTS.MEFeatureSelection);
                    collection_RecommendedModels = _database.GetCollection<BsonDocument>(CONSTANTS.MERecommendedModels);
                    collection_TeachAndTest = _database.GetCollection<BsonDocument>(CONSTANTS.ME_TeachAndTest);

                    collection_DeliveryConstructStructures = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_DeliveryConstructStructures);
                    collection_DeployedModels = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
                    collection_PublicTemplates = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIPublicTemplates);
                    collection_PublishModel = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_PublishModel);
                    collection_UseCase = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIUseCase);
                    collection_CascadeModel = _database.GetCollection<BsonDocument>(CONSTANTS.SSAICascadedModels);
                    collection_SSAIIngrainRequests = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
                    SSAICascadeVisualization = _database.GetCollection<BsonDocument>(CONSTANTS.SSAICascadeVisualization);
                    SSAIFMVisualization = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIFMVisualization);
                }
                //var collection_IngestedData = _database.GetCollection<BsonDocument>(CONSTANTS.PSIngestedData);
                //var collection_BusinessProblem = _database.GetCollection<BsonDocument>(CONSTANTS.PSBusinessProblem);
                if (flushFlag != CONSTANTS.FlushBusinessProb)
                {
                    if (collection_BusinessProblem.Find(filter).ToList().Count > 0)
                    {
                        collection_BusinessProblem.DeleteMany(filter);
                        flushStatus = flushStatus + CONSTANTS.SuccessPS_BusinessProblem;
                    }
                    if (collection_IngestedData.Find(filter).ToList().Count > 0)
                    {
                        collection_IngestedData.DeleteMany(filter);
                        flushStatus = flushStatus + CONSTANTS.SuccessPS_IngestedData;
                    }
                }
                //var collection_DataCleanup = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataCleanup);
               // var collection_DataProcessing = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataProcessing);
                //var collection_DeployedPublishModel = _database.GetCollection<BsonDocument>(CONSTANTS.DeployedPublishModel);
                //var collection_IngrainDeliveryConstruct = _database.GetCollection<BsonDocument>(CONSTANTS.IngrainDeliveryConstruct);
                //var collection_HyperTuneVersion = _database.GetCollection<BsonDocument>(CONSTANTS.ME_HyperTuneVersion);
                //var collection_SSAI_RecommendedTrainedModels = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIRecommendedTrainedModels);
                //var collection_SSAI_UserDetails = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIUserDetails);
                //var collection_WF_IngestedData = _database.GetCollection<BsonDocument>(CONSTANTS.WF_IngestedData);
                //var collection_WF_TestResults = _database.GetCollection<BsonDocument>(CONSTANTS.WF_TestResults_);
                //var collection_WhatIfAnalysis = _database.GetCollection<BsonDocument>(CONSTANTS.WhatIfAnalysis);

                //var collection_DataVisualization = _database.GetCollection<BsonDocument>(CONSTANTS.DE_DataVisualization);
                //var collection_PreProcessedData = _database.GetCollection<BsonDocument>(CONSTANTS.DEPreProcessedData);
                //var collection_FilteredData = _database.GetCollection<BsonDocument>(CONSTANTS.DataCleanUPFilteredData);
                //var collection_FeatureSelection = _database.GetCollection<BsonDocument>(CONSTANTS.MEFeatureSelection);
                //var collection_RecommendedModels = _database.GetCollection<BsonDocument>(CONSTANTS.MERecommendedModels);
                //var collection_TeachAndTest = _database.GetCollection<BsonDocument>(CONSTANTS.ME_TeachAndTest);

                //var collection_DeliveryConstructStructures = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_DeliveryConstructStructures);
                //var collection_DeployedModels = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
                //var collection_PublicTemplates = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIPublicTemplates);
                //var collection_PublishModel = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_PublishModel);
                //var collection_UseCase = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIUseCase);
                //var collection_CascadeModel = _database.GetCollection<BsonDocument>(CONSTANTS.SSAICascadedModels);
                //var collection_SSAIIngrainRequests = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
                //var SSAICascadeVisualization = _database.GetCollection<BsonDocument>(CONSTANTS.SSAICascadeVisualization);
                //var SSAIFMVisualization = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIFMVisualization);

                var cascadeResult = collection_CascadeModel.Find(cascadeFilter).Project<BsonDocument>(cascadeProjection).ToList();
                if (cascadeResult.Count > 0)
                {
                    JObject data = JObject.Parse(cascadeResult[0].ToString());
                    if (data != null)
                    {
                        if (data[CONSTANTS.ModelList].Children().Count() > 0)
                        {
                            string cascadeId = data["CascadedId"].ToString();
                            bool isCusomtModel = Convert.ToBoolean(data[CONSTANTS.IsCustomModel]);
                            string model1Corid = data[CONSTANTS.ModelList]["Model1"][CONSTANTS.CorrelationId].ToString();
                            int i = 1;
                            foreach (var item in data[CONSTANTS.ModelList].Children())
                            {
                                var model = item as JProperty;
                                if (model != null)
                                {
                                    if (isCusomtModel)
                                    {
                                        if (i > 1)
                                        {
                                            IMongoCollection<BusinessProblem> businesscolection;
                                            if (ServiceName == "Anomaly")
                                                businesscolection = _databaseAD.GetCollection<BusinessProblem>(CONSTANTS.PSBusinessProblem);
                                            else
                                                businesscolection = _database.GetCollection<BusinessProblem>(CONSTANTS.PSBusinessProblem);
                                            //Revert the model to normal model.
                                            CascadeModelsCollection cascadeModel = JsonConvert.DeserializeObject<CascadeModelsCollection>(model.Value.ToString());
                                            string modelCount = string.Format("Model{0}", i - 1);
                                            CascadeModelsCollection previousModel = JsonConvert.DeserializeObject<CascadeModelsCollection>(data[CONSTANTS.MappingData][modelCount].ToString());
                                            //var businesscolection = _database.GetCollection<BusinessProblem>(CONSTANTS.PSBusinessProblem);
                                            var businessFilter = Builders<BusinessProblem>.Filter.Eq(CONSTANTS.CorrelationId, cascadeModel.CorrelationId);
                                            var psProjection = Builders<BusinessProblem>.Projection.Include(CONSTANTS.InputColumns).Exclude(CONSTANTS.Id);
                                            var psResult = businesscolection.Find(businessFilter).Project<BusinessProblem>(psProjection).ToList();
                                            if (psResult.Count > 0)
                                            {
                                                //Reverting model
                                                string val = null;
                                                IMongoCollection<DeployModelsDto> collection;
                                                if (ServiceName == "Anomaly")
                                                    collection = _databaseAD.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
                                                else
                                                    collection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
                                                //var collection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
                                                var filter3 = Builders<DeployModelsDto>.Filter.Eq(CONSTANTS.CorrelationId, cascadeModel.CorrelationId);
                                                var deployProjection = Builders<DeployModelsDto>.Projection.Exclude(CONSTANTS.Id);

                                                var deployResult = collection.Find(filter3).Project<DeployModelsDto>(deployProjection).FirstOrDefault();
                                                if (deployResult != null)
                                                {
                                                    if (deployResult.CascadeIdList.Length > 0)
                                                    {
                                                        string[] CascadeIdList = deployResult.CascadeIdList.Where(val => val != cascadeId).ToArray();
                                                        if (isCusomtModel)
                                                        {
                                                            string[] arr = new string[] { };
                                                            var update = Builders<DeployModelsDto>.Update.Set(CONSTANTS.CustomCascadeId, val)
                                                          .Set(CONSTANTS.IsIncludedInCascade, false)
                                                          .Set(CONSTANTS.IsIncludedinCustomCascade, false)
                                                          .Set(CONSTANTS.CascadeIdList, CascadeIdList)
                                                          .Set(CONSTANTS.DataCurationName, val);
                                                            var updateResult2 = collection.UpdateOne(filter3, update);
                                                        }
                                                        else
                                                        {
                                                            string[] arr = new string[] { };
                                                            var update = Builders<DeployModelsDto>.Update.Set(CONSTANTS.CustomCascadeId, val)
                                                          .Set(CONSTANTS.IsIncludedInCascade, false)
                                                          .Set(CONSTANTS.IsIncludedinCustomCascade, false)
                                                          .Set(CONSTANTS.CascadeIdList, arr);
                                                            var updateResult3 = collection.UpdateOne(filter3, update);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        if (isCusomtModel)
                                                        {
                                                            string[] arr = new string[] { };
                                                            var update = Builders<DeployModelsDto>.Update.Set(CONSTANTS.CustomCascadeId, val)
                                                          .Set(CONSTANTS.IsIncludedInCascade, false)
                                                          .Set(CONSTANTS.IsIncludedinCustomCascade, false)
                                                          .Set(CONSTANTS.CascadeIdList, arr)
                                                          .Set(CONSTANTS.DataCurationName, val);
                                                            var updateResult4 = collection.UpdateOne(filter3, update);
                                                        }
                                                        else
                                                        {
                                                            string[] arr = new string[] { };
                                                            var update = Builders<DeployModelsDto>.Update.Set(CONSTANTS.CustomCascadeId, val)
                                                          .Set(CONSTANTS.IsIncludedInCascade, false)
                                                          .Set(CONSTANTS.IsIncludedinCustomCascade, false)
                                                          .Set(CONSTANTS.CascadeIdList, arr);
                                                            var updateResult5 = collection.UpdateOne(filter3, update);
                                                        }
                                                    }
                                                }
                                                //END

                                                //updating PSBusinessProblem
                                                string targetColumn = previousModel.ModelName + "_" + previousModel.TargetColumn;
                                                string probaItem = previousModel.ModelName + "_" + "Proba1";
                                                string[] inputColumns = psResult[0].InputColumns.Where(e => e != targetColumn).ToArray();
                                                string[] inputColumns2 = inputColumns.Where(e => e != probaItem).ToArray();

                                                var psUpdate = Builders<BusinessProblem>.Update.Set(CONSTANTS.InputColumns, inputColumns2);
                                                var updateResult = businesscolection.UpdateOne(businessFilter, psUpdate);
                                                //End
                                            }
                                            i++;
                                        }
                                        else
                                        {
                                            string val = null;
                                            IMongoCollection<DeployModelsDto> collection;
                                            if (ServiceName == "Anomaly")
                                                collection = _databaseAD.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
                                            else
                                                collection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
                                            //var collection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
                                            var filter3 = Builders<DeployModelsDto>.Filter.Eq(CONSTANTS.CorrelationId, model1Corid);
                                            var deployProjection = Builders<DeployModelsDto>.Projection.Exclude(CONSTANTS.Id);

                                            var deployResult = collection.Find(filter3).Project<DeployModelsDto>(deployProjection).FirstOrDefault();
                                            if (deployResult != null)
                                            {
                                                if (deployResult.CascadeIdList.Length > 0)
                                                {
                                                    string[] CascadeIdList = deployResult.CascadeIdList.Where(val => val != cascadeId).ToArray();
                                                    if (isCusomtModel)
                                                    {
                                                        string[] arr = new string[] { };
                                                        var update = Builders<DeployModelsDto>.Update.Set(CONSTANTS.CustomCascadeId, val)
                                                      .Set(CONSTANTS.IsIncludedInCascade, false)
                                                      .Set(CONSTANTS.IsIncludedinCustomCascade, false)
                                                      .Set(CONSTANTS.CascadeIdList, CascadeIdList)
                                                      .Set(CONSTANTS.DataCurationName, val);
                                                        var updateResult = collection.UpdateOne(filter3, update);
                                                    }
                                                    else
                                                    {
                                                        string[] arr = new string[] { };
                                                        var update = Builders<DeployModelsDto>.Update.Set(CONSTANTS.CustomCascadeId, val)
                                                      .Set(CONSTANTS.IsIncludedInCascade, false)
                                                      .Set(CONSTANTS.IsIncludedinCustomCascade, false)
                                                      .Set(CONSTANTS.CascadeIdList, arr);
                                                        var updateResult = collection.UpdateOne(filter3, update);
                                                    }
                                                }
                                                else
                                                {
                                                    if (isCusomtModel)
                                                    {
                                                        string[] arr = new string[] { };
                                                        var update = Builders<DeployModelsDto>.Update.Set(CONSTANTS.CustomCascadeId, val)
                                                      .Set(CONSTANTS.IsIncludedInCascade, false)
                                                      .Set(CONSTANTS.IsIncludedinCustomCascade, false)
                                                      .Set(CONSTANTS.CascadeIdList, arr)
                                                      .Set(CONSTANTS.DataCurationName, val);
                                                        var updateResult = collection.UpdateOne(filter3, update);
                                                    }
                                                    else
                                                    {
                                                        string[] arr = new string[] { };
                                                        var update = Builders<DeployModelsDto>.Update.Set(CONSTANTS.CustomCascadeId, val)
                                                      .Set(CONSTANTS.IsIncludedInCascade, false)
                                                      .Set(CONSTANTS.IsIncludedinCustomCascade, false)
                                                      .Set(CONSTANTS.CascadeIdList, arr);
                                                        var updateResult = collection.UpdateOne(filter3, update);
                                                    }
                                                }


                                            }
                                            i++;
                                        }
                                    }
                                    else
                                    {
                                        CascadeModelsCollection cascadeModel = JsonConvert.DeserializeObject<CascadeModelsCollection>(model.Value.ToString());
                                        string val = null;
                                        IMongoCollection<DeployModelsDto> collection;
                                        if (ServiceName == "Anomaly")
                                            collection = _databaseAD.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
                                        else
                                            collection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
                                       // var collection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
                                        var filter3 = Builders<DeployModelsDto>.Filter.Eq(CONSTANTS.CorrelationId, model1Corid);
                                        var deployProjection = Builders<DeployModelsDto>.Projection.Exclude(CONSTANTS.Id);

                                        var deployResult = collection.Find(filter3).Project<DeployModelsDto>(deployProjection).FirstOrDefault();
                                        if (deployResult != null)
                                        {
                                            if (deployResult.CascadeIdList.Length > 0)
                                            {
                                                string[] CascadeIdList = deployResult.CascadeIdList.Where(val => val != cascadeId).ToArray();
                                                if (isCusomtModel)
                                                {
                                                    var update = Builders<DeployModelsDto>.Update.Set(CONSTANTS.CustomCascadeId, val)
                                                  .Set(CONSTANTS.IsIncludedInCascade, false)
                                                  .Set(CONSTANTS.IsIncludedinCustomCascade, false)
                                                  .Set(CONSTANTS.CascadeIdList, CascadeIdList)
                                                  .Set(CONSTANTS.DataCurationName, val);
                                                    var updateResult = collection.UpdateOne(filter3, update);
                                                }
                                                else
                                                {
                                                    if (deployResult.CascadeIdList != null)
                                                    {
                                                        string[] arr = new string[] { };
                                                        if (deployResult.CascadeIdList.Count() > 0)
                                                        {
                                                            if (deployResult.CascadeIdList.Length < 2)
                                                            {
                                                                var update = Builders<DeployModelsDto>.Update.Set(CONSTANTS.IsIncludedInCascade, false).Set("CascadeIdList", arr);
                                                                var updateResult = collection.UpdateOne(filter3, update);
                                                            }
                                                            else
                                                            {
                                                                deployResult.CascadeIdList = deployResult.CascadeIdList.Where(val => val != cascadeId).ToArray();
                                                                var update3 = Builders<DeployModelsDto>.Update.Set("IsIncludedInCascade", true).Set("CascadeIdList", deployResult.CascadeIdList);
                                                                var updateResult3 = collection.UpdateOne(filter3, update3);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            var update = Builders<DeployModelsDto>.Update.Set(CONSTANTS.IsIncludedInCascade, false).Set("CascadeIdList", arr);
                                                            var updateResult = collection.UpdateOne(filter3, update);
                                                        }
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                string[] arr2 = new string[] { };
                                                if (isCusomtModel)
                                                {
                                                    var update = Builders<DeployModelsDto>.Update.Set(CONSTANTS.CustomCascadeId, val)
                                                  .Set(CONSTANTS.IsIncludedInCascade, false)
                                                  .Set(CONSTANTS.IsIncludedinCustomCascade, false)
                                                  .Set(CONSTANTS.CascadeIdList, arr2)
                                                  .Set(CONSTANTS.DataCurationName, val);
                                                    var updateResult = collection.UpdateOne(filter3, update);
                                                }
                                                else
                                                {
                                                    if (deployResult.CascadeIdList != null)
                                                    {
                                                        string[] arr = new string[] { };
                                                        if (deployResult.CascadeIdList.Count() > 0)
                                                        {
                                                            if (deployResult.CascadeIdList.Length < 2)
                                                            {
                                                                var update = Builders<DeployModelsDto>.Update.Set(CONSTANTS.IsIncludedInCascade, false).Set("CascadeIdList", arr);
                                                                var updateResult = collection.UpdateOne(filter3, update);
                                                            }
                                                            else
                                                            {
                                                                deployResult.CascadeIdList = deployResult.CascadeIdList.Where(val => val != cascadeId).ToArray();
                                                                var update3 = Builders<DeployModelsDto>.Update.Set("IsIncludedInCascade", true).Set("CascadeIdList", deployResult.CascadeIdList);
                                                                var updateResult3 = collection.UpdateOne(filter3, update3);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            var update = Builders<DeployModelsDto>.Update.Set(CONSTANTS.IsIncludedInCascade, false).Set("CascadeIdList", arr);
                                                            var updateResult = collection.UpdateOne(filter3, update);
                                                        }
                                                    }
                                                }
                                            }

                                        }

                                    }
                                }
                            }
                        }
                        collection_CascadeModel.DeleteMany(cascadeFilter);
                        flushStatus = flushStatus + CONSTANTS.SSAICascadedModels;
                    }
                }
                if (collection_SSAIIngrainRequests.Find(filter).ToList().Count > 0)
                {
                    collection_SSAIIngrainRequests.DeleteMany(filter);
                    flushStatus = flushStatus + CONSTANTS.SSAIIngrainRequests;
                }
                if (collection_SSAIIngrainRequests.Find(filter).ToList().Count > 0)
                {
                    collection_SSAIIngrainRequests.DeleteMany(filter);
                    flushStatus = flushStatus + CONSTANTS.SSAIIngrainRequests;
                }

                if (collection_DataCleanup.Find(filter).ToList().Count > 0)
                {
                    collection_DataCleanup.DeleteMany(filter);
                    flushStatus = flushStatus + CONSTANTS.SuccessDE_DataCleanup;
                }

                if (collection_DataProcessing.Find(filter).ToList().Count > 0)
                {
                    collection_DataProcessing.DeleteMany(filter);
                    flushStatus = flushStatus + CONSTANTS.SuccessDE_DataProcessing;
                }
                if (collection_DeployedPublishModel.Find(filter).ToList().Count > 0)
                {
                    collection_DeployedPublishModel.DeleteMany(filter);
                    flushStatus = flushStatus + CONSTANTS.SuccessDE_DeployedPublishModel;
                }
                if (collection_IngrainDeliveryConstruct.Find(filter).ToList().Count > 0)
                {
                    collection_IngrainDeliveryConstruct.DeleteMany(filter);
                    flushStatus = flushStatus + CONSTANTS.SuccessDE_IngrainDeliveryConstruct;
                }
                if (collection_HyperTuneVersion.Find(filter).ToList().Count > 0)
                {
                    collection_HyperTuneVersion.DeleteMany(filter);
                    flushStatus = flushStatus + CONSTANTS.SuccessME_HyperTuneVersion;
                }
                if (collection_SSAI_RecommendedTrainedModels.Find(filter).ToList().Count > 0)
                {
                    collection_SSAI_RecommendedTrainedModels.DeleteMany(filter);
                    flushStatus = flushStatus + CONSTANTS.SuccessSSAI_RecommendedTrainedModels;
                }
                if (collection_SSAI_UserDetails.Find(filter).ToList().Count > 0)
                {
                    collection_SSAI_UserDetails.DeleteMany(filter);
                    flushStatus = flushStatus + CONSTANTS.SuccessSSAI_UserDetails;
                }
                if (collection_WF_IngestedData.Find(filter).ToList().Count > 0)
                {
                    collection_WF_IngestedData.DeleteMany(filter);
                    flushStatus = flushStatus + CONSTANTS.SuccessWF_IngestedData;
                }
                if (collection_WF_TestResults.Find(filter).ToList().Count > 0)
                {
                    collection_WF_TestResults.DeleteMany(filter);
                    flushStatus = flushStatus + CONSTANTS.SuccessWF_TestResults;
                }
                if (collection_WhatIfAnalysis.Find(filter).ToList().Count > 0)
                {
                    collection_WhatIfAnalysis.DeleteMany(filter);
                    flushStatus = flushStatus + CONSTANTS.SuccessWhatIfAnalysis;
                }

                if (collection_DataVisualization.Find(filter).ToList().Count > 0)
                {
                    collection_DataVisualization.DeleteMany(filter);
                    flushStatus = flushStatus + CONSTANTS.SuccessDE_DataVisualization;
                }
                if (collection_PreProcessedData.Find(filter).ToList().Count > 0)
                {
                    collection_PreProcessedData.DeleteMany(filter);
                    flushStatus = flushStatus + CONSTANTS.SuccessDE_PreProcessedData;
                }
                if (collection_FilteredData.Find(filter).ToList().Count > 0)
                {
                    collection_FilteredData.DeleteMany(filter);
                    flushStatus = flushStatus + CONSTANTS.SuccessDataCleanUP_FilteredData;
                }
                if (collection_FeatureSelection.Find(filter).ToList().Count > 0)
                {
                    collection_FeatureSelection.DeleteMany(filter);
                    flushStatus = flushStatus + CONSTANTS.SuccessME_FeatureSelection;
                }
                if (collection_RecommendedModels.Find(filter).ToList().Count > 0)
                {
                    collection_RecommendedModels.DeleteMany(filter);
                    flushStatus = flushStatus + CONSTANTS.SuccessME_RecommendedModels;
                }
                if (collection_TeachAndTest.Find(filter).ToList().Count > 0)
                {
                    collection_TeachAndTest.DeleteMany(filter);
                    flushStatus = flushStatus + " + success for ME_TeachAndTest";
                }

                if (collection_DeliveryConstructStructures.Find(filter).ToList().Count > 0)
                {
                    collection_DeliveryConstructStructures.DeleteMany(filter);
                    flushStatus = flushStatus + " + success for SSAI_DeliveryConstructStructures";
                }
                if (SSAICascadeVisualization.Find(filter).ToList().Count > 0)
                {
                    SSAICascadeVisualization.DeleteMany(filter);
                    flushStatus = flushStatus + " + success for SSAI_CascadeVisualization";
                }
                if (SSAIFMVisualization.Find(filter).ToList().Count > 0)
                {
                    SSAIFMVisualization.DeleteMany(filter);
                    flushStatus = flushStatus + " + success for SSAI_FMVisualization";
                }

                var deployedModelResult = collection_DeployedModels.Find(filter).ToList();
                if (deployedModelResult.Count > 0)
                {
                    bool isFmModel = false;
                    if (deployedModelResult[0].Contains(CONSTANTS.IsFMModel))
                    {
                        isFmModel = Convert.ToBoolean(deployedModelResult[0][CONSTANTS.IsFMModel]);
                        if (isFmModel)
                        {
                            string fmcorrelationId = deployedModelResult[0][CONSTANTS.FMCorrelationId].ToString();
                            var fmFilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, fmcorrelationId);
                            collection_DeployedModels.DeleteMany(fmFilter);
                            flushStatus = flushStatus + " + success for FM SSAI_DeployedModels";
                            //For hide model request deletion
                            collection_SSAIIngrainRequests.DeleteMany(fmFilter);
                            flushStatus = flushStatus + " + success for FM SSAIIngrainRequests";
                        }
                    }
                    collection_DeployedModels.DeleteMany(filter);
                    flushStatus = flushStatus + " + success for SSAI_DeployedModels";
                }
                if (collection_PublicTemplates.Find(filter).ToList().Count > 0)
                {
                    collection_PublicTemplates.DeleteMany(filter);
                    flushStatus = flushStatus + " + success for SSAI_PublicTemplates";
                }
                if (collection_PublishModel.Find(filter).ToList().Count > 0)
                {
                    collection_PublishModel.DeleteMany(filter);
                    flushStatus = flushStatus + " + success for SSAI_PublishModel";
                }
                if (collection_UseCase.Find(filter).ToList().Count > 0)
                {
                    collection_UseCase.DeleteMany(filter);
                    flushStatus = flushStatus + " + success for SSAI_UseCase";
                }
            }
            return flushStatus;
        }

        public string FlushModelSPP(string CorrelationId, string flushFlag)
        {
            string flushStatus = string.Empty;
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, CorrelationId);

            var collection_savedModels = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_savedModels);
            var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.FilePath).Exclude(CONSTANTS.Id);
            var correaltionExist = collection_savedModels.Find(filter).Project<BsonDocument>(projection).ToList();
            if (correaltionExist.Count > 0)
            {
                for (int i = 0; i < correaltionExist.Count; i++)
                {
                    string FileToDelete = correaltionExist[i][CONSTANTS.FilePath].ToString();
                    if (File.Exists(FileToDelete))
                    { File.Delete(FileToDelete); }
                }

                collection_savedModels.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.SuccessSSAI_savedModels;
            }
            if (flushFlag == CONSTANTS.FlushIngestedData)
            {
                var collection_IngestedData = _database.GetCollection<BsonDocument>(CONSTANTS.PSIngestedData);
                if (collection_IngestedData.Find(filter).ToList().Count > 0)
                {
                    collection_IngestedData.DeleteMany(filter);
                    flushStatus = flushStatus + CONSTANTS.SuccessPS_IngestedData;
                }
            }
            else
            {
                var collection_IngestedData = _database.GetCollection<BsonDocument>(CONSTANTS.PSIngestedData);
                var collection_BusinessProblem = _database.GetCollection<BsonDocument>(CONSTANTS.PSBusinessProblem);
                if (flushFlag != CONSTANTS.FlushBusinessProb)
                {
                    if (collection_BusinessProblem.Find(filter).ToList().Count > 0)
                    {
                        collection_BusinessProblem.DeleteMany(filter);
                        flushStatus = flushStatus + CONSTANTS.SuccessPS_BusinessProblem;
                    }
                    if (collection_IngestedData.Find(filter).ToList().Count > 0)
                    {
                        collection_IngestedData.DeleteMany(filter);
                        flushStatus = flushStatus + CONSTANTS.SuccessPS_IngestedData;
                    }
                }
                var collection_DataCleanup = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataCleanup);
                var collection_DataProcessing = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataProcessing);
                var collection_DeployedPublishModel = _database.GetCollection<BsonDocument>(CONSTANTS.DeployedPublishModel);
                var collection_IngrainDeliveryConstruct = _database.GetCollection<BsonDocument>(CONSTANTS.IngrainDeliveryConstruct);
                var collection_HyperTuneVersion = _database.GetCollection<BsonDocument>(CONSTANTS.ME_HyperTuneVersion);
                var collection_SSAI_RecommendedTrainedModels = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIRecommendedTrainedModels);
                var collection_SSAI_UserDetails = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIUserDetails);
                var collection_WF_IngestedData = _database.GetCollection<BsonDocument>(CONSTANTS.WF_IngestedData);
                var collection_WF_TestResults = _database.GetCollection<BsonDocument>(CONSTANTS.WF_TestResults_);
                var collection_WhatIfAnalysis = _database.GetCollection<BsonDocument>(CONSTANTS.WhatIfAnalysis);

                var collection_DataVisualization = _database.GetCollection<BsonDocument>(CONSTANTS.DE_DataVisualization);
                var collection_PreProcessedData = _database.GetCollection<BsonDocument>(CONSTANTS.DEPreProcessedData);
                var collection_FilteredData = _database.GetCollection<BsonDocument>(CONSTANTS.DataCleanUPFilteredData);
                var collection_FeatureSelection = _database.GetCollection<BsonDocument>(CONSTANTS.MEFeatureSelection);
                var collection_RecommendedModels = _database.GetCollection<BsonDocument>(CONSTANTS.MERecommendedModels);
                var collection_TeachAndTest = _database.GetCollection<BsonDocument>(CONSTANTS.ME_TeachAndTest);

                var collection_DeliveryConstructStructures = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_DeliveryConstructStructures);
                var collection_DeployedModels = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
                var collection_PublicTemplates = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIPublicTemplates);
                var collection_PublishModel = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_PublishModel);
                var collection_UseCase = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIUseCase);

                var collection_SSAIIngrainRequests = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);

                if (collection_SSAIIngrainRequests.Find(filter).ToList().Count > 0)
                {
                    var filter2 = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, CorrelationId)
                                  & Builders<BsonDocument>.Filter.Ne(CONSTANTS.pageInfo, CONSTANTS.PublishModel);
                    collection_SSAIIngrainRequests.DeleteMany(filter2);
                    flushStatus = flushStatus + CONSTANTS.SSAIIngrainRequests;
                }

                if (collection_DataCleanup.Find(filter).ToList().Count > 0)
                {
                    collection_DataCleanup.DeleteMany(filter);
                    flushStatus = flushStatus + CONSTANTS.SuccessDE_DataCleanup;
                }

                if (collection_DataProcessing.Find(filter).ToList().Count > 0)
                {
                    collection_DataProcessing.DeleteMany(filter);
                    flushStatus = flushStatus + CONSTANTS.SuccessDE_DataProcessing;
                }
                if (collection_DeployedPublishModel.Find(filter).ToList().Count > 0)
                {
                    collection_DeployedPublishModel.DeleteMany(filter);
                    flushStatus = flushStatus + CONSTANTS.SuccessDE_DeployedPublishModel;
                }
                if (collection_IngrainDeliveryConstruct.Find(filter).ToList().Count > 0)
                {
                    collection_IngrainDeliveryConstruct.DeleteMany(filter);
                    flushStatus = flushStatus + CONSTANTS.SuccessDE_IngrainDeliveryConstruct;
                }
                if (collection_HyperTuneVersion.Find(filter).ToList().Count > 0)
                {
                    collection_HyperTuneVersion.DeleteMany(filter);
                    flushStatus = flushStatus + CONSTANTS.SuccessME_HyperTuneVersion;
                }
                if (collection_SSAI_RecommendedTrainedModels.Find(filter).ToList().Count > 0)
                {
                    collection_SSAI_RecommendedTrainedModels.DeleteMany(filter);
                    flushStatus = flushStatus + CONSTANTS.SuccessSSAI_RecommendedTrainedModels;
                }
                if (collection_SSAI_UserDetails.Find(filter).ToList().Count > 0)
                {
                    collection_SSAI_UserDetails.DeleteMany(filter);
                    flushStatus = flushStatus + CONSTANTS.SuccessSSAI_UserDetails;
                }
                if (collection_WF_IngestedData.Find(filter).ToList().Count > 0)
                {
                    collection_WF_IngestedData.DeleteMany(filter);
                    flushStatus = flushStatus + CONSTANTS.SuccessWF_IngestedData;
                }
                if (collection_WF_TestResults.Find(filter).ToList().Count > 0)
                {
                    collection_WF_TestResults.DeleteMany(filter);
                    flushStatus = flushStatus + CONSTANTS.SuccessWF_TestResults;
                }
                if (collection_WhatIfAnalysis.Find(filter).ToList().Count > 0)
                {
                    collection_WhatIfAnalysis.DeleteMany(filter);
                    flushStatus = flushStatus + CONSTANTS.SuccessWhatIfAnalysis;
                }

                if (collection_DataVisualization.Find(filter).ToList().Count > 0)
                {
                    collection_DataVisualization.DeleteMany(filter);
                    flushStatus = flushStatus + CONSTANTS.SuccessDE_DataVisualization;
                }
                if (collection_PreProcessedData.Find(filter).ToList().Count > 0)
                {
                    collection_PreProcessedData.DeleteMany(filter);
                    flushStatus = flushStatus + CONSTANTS.SuccessDE_PreProcessedData;
                }
                if (collection_FilteredData.Find(filter).ToList().Count > 0)
                {
                    collection_FilteredData.DeleteMany(filter);
                    flushStatus = flushStatus + CONSTANTS.SuccessDataCleanUP_FilteredData;
                }
                if (collection_FeatureSelection.Find(filter).ToList().Count > 0)
                {
                    collection_FeatureSelection.DeleteMany(filter);
                    flushStatus = flushStatus + CONSTANTS.SuccessME_FeatureSelection;
                }
                if (collection_RecommendedModels.Find(filter).ToList().Count > 0)
                {
                    collection_RecommendedModels.DeleteMany(filter);
                    flushStatus = flushStatus + CONSTANTS.SuccessME_RecommendedModels;
                }
                if (collection_TeachAndTest.Find(filter).ToList().Count > 0)
                {
                    collection_TeachAndTest.DeleteMany(filter);
                    flushStatus = flushStatus + " + success for ME_TeachAndTest";
                }


                if (collection_DeliveryConstructStructures.Find(filter).ToList().Count > 0)
                {
                    collection_DeliveryConstructStructures.DeleteMany(filter);
                    flushStatus = flushStatus + " + success for SSAI_DeliveryConstructStructures";
                }
                if (collection_DeployedModels.Find(filter).ToList().Count > 0)
                {
                    collection_DeployedModels.DeleteMany(filter);
                    flushStatus = flushStatus + " + success for SSAI_DeployedModels";
                }
                if (collection_PublicTemplates.Find(filter).ToList().Count > 0)
                {
                    collection_PublicTemplates.DeleteMany(filter);
                    flushStatus = flushStatus + " + success for SSAI_PublicTemplates";
                }

                if (collection_UseCase.Find(filter).ToList().Count > 0)
                {
                    collection_UseCase.DeleteMany(filter);
                    flushStatus = flushStatus + " + success for SSAI_UseCase";
                }
            }
            return flushStatus;
        }
        public void SendVDSDeployModelNotification(DeployModelsDto mdl, string operation, bool isCascadeModel)
        {
            AppNotificationLog appNotificationLog = new AppNotificationLog();
            if (isCascadeModel)
            {
                CascadeModelsCollection models = new CascadeModelsCollection();
                var cascadeCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAICascadedModels);
                var cascadeFilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CascadedId, mdl.CorrelationId);
                var cascadeProjection = Builders<BsonDocument>.Projection.Include(CONSTANTS.ModelList).Exclude(CONSTANTS.Id);
                var cascadeResult = cascadeCollection.Find(cascadeFilter).Project<BsonDocument>(cascadeProjection).ToList();
                if (cascadeResult.Count > 0)
                {
                    JObject data = JObject.Parse(cascadeResult[0].ToString());
                    if (data != null)
                    {
                        string model2 = string.Format("Model{0}", data[CONSTANTS.ModelList].Children().Count());
                        models = JsonConvert.DeserializeObject<CascadeModelsCollection>(data[CONSTANTS.ModelList][model2].ToString());
                    }
                }
                var collection = _database.GetCollection<BusinessProblem>(CONSTANTS.PSBusinessProblem);
                var filter2 = Builders<BusinessProblem>.Filter.Eq(CONSTANTS.CorrelationId, models.CorrelationId);
                var projection2 = Builders<BusinessProblem>.Projection.Include(CONSTANTS.BusinessProblems).Exclude(CONSTANTS.Id);
                var result = collection.Find(filter2).Project<BusinessProblem>(projection2).FirstOrDefault();

                appNotificationLog.UseCaseName = mdl.ModelName;
                if (result != null)
                    appNotificationLog.UseCaseDescription = result.BusinessProblems;
            }

            appNotificationLog.ApplicationId = mdl.AppId;
            appNotificationLog.ClientUId = mdl.ClientUId;
            appNotificationLog.DeliveryConstructUId = mdl.DeliveryConstructUID;
            appNotificationLog.CorrelationId = mdl.CorrelationId;
            appNotificationLog.CreatedDateTime = mdl.CreatedOn;
            appNotificationLog.Entity = string.Empty;
            appNotificationLog.CreatedBy = mdl.CreatedByUser;
            appNotificationLog.ModifiedBy = mdl.CreatedByUser;
            appNotificationLog.UserId = mdl.CreatedByUser;
            appNotificationLog.UseCaseId = mdl.CorrelationId;// for only vds charts
            appNotificationLog.FunctionalArea = mdl.Category;
            if (isCascadeModel)
            {
                appNotificationLog.ProblemType = "Cascade";
                appNotificationLog.IsCascade = true;
                if (mdl.Category == "AD")
                    appNotificationLog.FunctionalArea = "ADWaterfall";
                if (mdl.Category == "Devops")
                    appNotificationLog.FunctionalArea = "DevOps";
                if (mdl.Category == "Others")
                    appNotificationLog.FunctionalArea = "General";
            }
            else
            {
                appNotificationLog.ProblemType = mdl.ModelType;
                appNotificationLog.FunctionalArea = mdl.Category;
                if (mdl.Category == "Others")
                    appNotificationLog.FunctionalArea = "General";
                if (mdl.Category == "PPM")
                    appNotificationLog.FunctionalArea = "RIAD";
            }
            if (mdl.IsModelTemplate)
            {
                appNotificationLog.ModelType = "ModelTemplate";
            }
            else
            {
                if (mdl.IsPrivate)
                {
                    appNotificationLog.ModelType = "Private";
                }
                else
                {
                    appNotificationLog.ModelType = "Public";
                }
            }

            appNotificationLog.OperationType = OperationTypes.Deleted.ToString();
            appNotificationLog.NotificationEventType = "DeployModel";

            List<string> datasource = new List<string>();
            var attributes = CommonUtility.GetCommonAttributes(appNotificationLog.CorrelationId, appSettings);
            CommonUtility.SendAppNotification(appNotificationLog, isCascadeModel);

        }
        public string FlushAllModels(string clientuid, string deliveryconstructid, string userid, string flushFlag)
        {
            string flushStatus = string.Empty;
            string encryptedUser = userid;
            if (!string.IsNullOrEmpty(Convert.ToString(userid)))
                encryptedUser = appSettings.Value.IsAESKeyVault ? CryptographyUtility.Encrypt(Convert.ToString(userid)) : AesProvider.Encrypt(Convert.ToString(userid), appSettings.Value.aesKey, appSettings.Value.aesVector);
            var filter1 = clientuid != null ? Builders<BsonDocument>.Filter.Eq("ClientUId", clientuid) : null;
            var filter2 = deliveryconstructid != null ? Builders<BsonDocument>.Filter.Eq("DeliveryConstructUID", deliveryconstructid) : null;
            var filter3 = ((userid != null ? Builders<BsonDocument>.Filter.Eq("CreatedByUser", userid) : null) | (encryptedUser != null ? Builders<BsonDocument>.Filter.Eq("CreatedByUser", encryptedUser) : null));
            var filterCorrId = filter1;
            if (filter2 != null)
            {
                filterCorrId = filterCorrId & filter2;
                if (filter3 != null)
                    filterCorrId = filterCorrId & filter3;
            }
            else if (filter3 != null)
            {
                filterCorrId = filterCorrId & filter3;
            }

            var collection_deployedModels = _database.GetCollection<BsonDocument>("SSAI_DeployedModels");
            var proj = Builders<BsonDocument>.Projection.Include("CorrelationId").Exclude("_id");
            var correlationList = collection_deployedModels.Find(filterCorrId).Project<BsonDocument>(proj).ToList();
            string[] crList = new string[correlationList.Count];
            for (int i = 0; i < correlationList.Count; i++)
            {
                crList[i] = correlationList[i]["CorrelationId"].ToString();
            }

            if (correlationList.Count > 0)
            {

                var filter = Builders<BsonDocument>.Filter.AnyIn("CorrelationId", crList);

                var collection_savedModels = _database.GetCollection<BsonDocument>("SSAI_savedModels");
                var projection = Builders<BsonDocument>.Projection.Include("FilePath").Exclude("_id");
                var correaltionExist = collection_savedModels.Find(filter).Project<BsonDocument>(projection).ToList();
                if (correaltionExist.Count > 0)
                {
                    for (int i = 0; i < correaltionExist.Count; i++)
                    {
                        string FileToDelete = correaltionExist[i]["FilePath"].ToString();
                        if (File.Exists(FileToDelete))
                        { File.Delete(FileToDelete); }
                    }

                    collection_savedModels.DeleteMany(filter);
                    flushStatus = flushStatus + " + success for SSAI_savedModels";
                }
                if (flushFlag == "Flush only IngestedData")
                {
                    var collection_IngestedData = _database.GetCollection<BsonDocument>("PS_IngestedData");
                    if (collection_IngestedData.Find(filter).ToList().Count > 0)
                    {
                        collection_IngestedData.DeleteMany(filter);
                        flushStatus = flushStatus + " + success for PS_IngestedData";
                    }
                }
                else
                {
                    var collection_IngestedData = _database.GetCollection<BsonDocument>("PS_IngestedData");
                    var collection_BusinessProblem = _database.GetCollection<BsonDocument>("PS_BusinessProblem");
                    if (flushFlag != "Flush All except BusinessProb")
                    {
                        if (collection_BusinessProblem.Find(filter).ToList().Count > 0)
                        {
                            collection_BusinessProblem.DeleteMany(filter);
                            flushStatus = flushStatus + " + success for PS_BusinessProblem";
                        }
                        if (collection_IngestedData.Find(filter).ToList().Count > 0)
                        {
                            collection_IngestedData.DeleteMany(filter);
                            flushStatus = flushStatus + " + success for PS_IngestedData";
                        }
                    }
                    var collection_DataCleanup = _database.GetCollection<BsonDocument>("DE_DataCleanup");
                    var collection_DataProcessing = _database.GetCollection<BsonDocument>("DE_DataProcessing");
                    var collection_DeployedPublishModel = _database.GetCollection<BsonDocument>("DeployedPublishModel");
                    var collection_IngrainDeliveryConstruct = _database.GetCollection<BsonDocument>("IngrainDeliveryConstruct");
                    var collection_HyperTuneVersion = _database.GetCollection<BsonDocument>("ME_HyperTuneVersion");
                    var collection_SSAI_RecommendedTrainedModels = _database.GetCollection<BsonDocument>("SSAI_RecommendedTrainedModels");
                    var collection_SSAI_UserDetails = _database.GetCollection<BsonDocument>("SSAI_UserDetails");
                    var collection_WF_IngestedData = _database.GetCollection<BsonDocument>("WF_IngestedData");
                    var collection_WF_TestResults = _database.GetCollection<BsonDocument>("WF_TestResults");
                    var collection_WhatIfAnalysis = _database.GetCollection<BsonDocument>("WhatIfAnalysis");

                    var collection_DataVisualization = _database.GetCollection<BsonDocument>("DE_DataVisualization");
                    var collection_PreProcessedData = _database.GetCollection<BsonDocument>("DE_PreProcessedData");
                    var collection_FilteredData = _database.GetCollection<BsonDocument>("DataCleanUP_FilteredData");
                    var collection_FeatureSelection = _database.GetCollection<BsonDocument>("ME_FeatureSelection");
                    var collection_RecommendedModels = _database.GetCollection<BsonDocument>("ME_RecommendedModels");
                    var collection_TeachAndTest = _database.GetCollection<BsonDocument>("ME_TeachAndTest");

                    var collection_DeliveryConstructStructures = _database.GetCollection<BsonDocument>("SSAI_DeliveryConstructStructures");
                    var collection_DeployedModels = _database.GetCollection<BsonDocument>("SSAI_DeployedModels");
                    var collection_PublicTemplates = _database.GetCollection<BsonDocument>("SSAI_PublicTemplates");
                    var collection_PublishModel = _database.GetCollection<BsonDocument>("SSAI_PublishModel");
                    var collection_UseCase = _database.GetCollection<BsonDocument>("SSAI_UseCase");


                    if (collection_DataCleanup.Find(filter).ToList().Count > 0)
                    {
                        collection_DataCleanup.DeleteMany(filter);
                        flushStatus = flushStatus + " + success for DE_DataCleanup";
                    }

                    if (collection_DataProcessing.Find(filter).ToList().Count > 0)
                    {
                        collection_DataProcessing.DeleteMany(filter);
                        flushStatus = flushStatus + " + success for DE_DataProcessing";
                    }
                    if (collection_DeployedPublishModel.Find(filter).ToList().Count > 0)
                    {
                        collection_DeployedPublishModel.DeleteMany(filter);
                        flushStatus = flushStatus + " + success for DeployedPublishModel";
                    }

                    if (collection_HyperTuneVersion.Find(filter).ToList().Count > 0)
                    {
                        collection_HyperTuneVersion.DeleteMany(filter);
                        flushStatus = flushStatus + " + success for ME_HyperTuneVersion";
                    }
                    if (collection_SSAI_RecommendedTrainedModels.Find(filter).ToList().Count > 0)
                    {
                        collection_SSAI_RecommendedTrainedModels.DeleteMany(filter);
                        flushStatus = flushStatus + " + success for SSAI_RecommendedTrainedModels";
                    }
                    if (collection_SSAI_UserDetails.Find(filter).ToList().Count > 0)
                    {
                        collection_SSAI_UserDetails.DeleteMany(filter);
                        flushStatus = flushStatus + " + success for SSAI_UserDetails";
                    }
                    if (collection_WF_IngestedData.Find(filter).ToList().Count > 0)
                    {
                        collection_WF_IngestedData.DeleteMany(filter);
                        flushStatus = flushStatus + " + success for WF_IngestedData";
                    }
                    if (collection_WF_TestResults.Find(filter).ToList().Count > 0)
                    {
                        collection_WF_TestResults.DeleteMany(filter);
                        flushStatus = flushStatus + " + success for WF_TestResults";
                    }
                    if (collection_WhatIfAnalysis.Find(filter).ToList().Count > 0)
                    {
                        collection_WhatIfAnalysis.DeleteMany(filter);
                        flushStatus = flushStatus + " + success for WhatIfAnalysis";
                    }

                    if (collection_DataVisualization.Find(filter).ToList().Count > 0)
                    {
                        collection_DataVisualization.DeleteMany(filter);
                        flushStatus = flushStatus + " + success for DE_DataVisualization";
                    }
                    if (collection_PreProcessedData.Find(filter).ToList().Count > 0)
                    {
                        collection_PreProcessedData.DeleteMany(filter);
                        flushStatus = flushStatus + " + success for DE_PreProcessedData";
                    }
                    if (collection_FilteredData.Find(filter).ToList().Count > 0)
                    {
                        collection_FilteredData.DeleteMany(filter);
                        flushStatus = flushStatus + " + success for DataCleanUP_FilteredData";
                    }
                    if (collection_FeatureSelection.Find(filter).ToList().Count > 0)
                    {
                        collection_FeatureSelection.DeleteMany(filter);
                        flushStatus = flushStatus + " + success for ME_FeatureSelection";
                    }
                    if (collection_RecommendedModels.Find(filter).ToList().Count > 0)
                    {
                        collection_RecommendedModels.DeleteMany(filter);
                        flushStatus = flushStatus + " + success for ME_RecommendedModels";
                    }
                    if (collection_TeachAndTest.Find(filter).ToList().Count > 0)
                    {
                        collection_TeachAndTest.DeleteMany(filter);
                        flushStatus = flushStatus + " + success for ME_TeachAndTest";
                    }


                    if (collection_DeliveryConstructStructures.Find(filter).ToList().Count > 0)
                    {
                        collection_DeliveryConstructStructures.DeleteMany(filter);
                        flushStatus = flushStatus + " + success for SSAI_DeliveryConstructStructures";
                    }
                    if (collection_DeployedModels.Find(filter).ToList().Count > 0)
                    {
                        collection_DeployedModels.DeleteMany(filter);
                        flushStatus = flushStatus + " + success for SSAI_DeployedModels";
                    }
                    if (collection_PublicTemplates.Find(filter).ToList().Count > 0)
                    {
                        collection_PublicTemplates.DeleteMany(filter);
                        flushStatus = flushStatus + " + success for SSAI_PublicTemplates";
                    }
                    if (collection_PublishModel.Find(filter).ToList().Count > 0)
                    {
                        collection_PublishModel.DeleteMany(filter);
                        flushStatus = flushStatus + " + success for SSAI_PublishModel";
                    }
                    if (collection_UseCase.Find(filter).ToList().Count > 0)
                    {
                        collection_UseCase.DeleteMany(filter);
                        flushStatus = flushStatus + " + success for SSAI_UseCase";
                    }
                }
            }
            return flushStatus;
        }

        public string InstaMLDeleteModel(string CorrelationId)
        {
            string flushStatus = string.Empty;
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, CorrelationId);
            var collection_savedModels = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_savedModels);
            var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.FilePath).Exclude(CONSTANTS.Id);
            var correaltionExist = collection_savedModels.Find(filter).Project<BsonDocument>(projection).ToList();
            if (correaltionExist.Count > 0)
            {
                for (int i = 0; i < correaltionExist.Count; i++)
                {
                    string FileToDelete = correaltionExist[i][CONSTANTS.FilePath].ToString();
                    if (File.Exists(FileToDelete))
                    { File.Delete(FileToDelete); }
                }

                collection_savedModels.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.SuccessSSAI_savedModels;
            }

            var collection_BusinessProblem = _database.GetCollection<BsonDocument>(CONSTANTS.PSBusinessProblem);
            if (collection_BusinessProblem.Find(filter).ToList().Count > 0)
            {
                collection_BusinessProblem.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.SuccessPS_BusinessProblem;
            }

            var collection_IngestedData = _database.GetCollection<BsonDocument>(CONSTANTS.PSIngestedData);
            if (collection_IngestedData.Find(filter).ToList().Count > 0)
            {
                collection_IngestedData.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.SuccessPS_IngestedData;
            }

            var collection_DataCleanup = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataCleanup);
            var collection_DataProcessing = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataProcessing);
            var collection_IngrainDeliveryConstruct = _database.GetCollection<BsonDocument>(CONSTANTS.IngrainDeliveryConstruct);
            var collection_HyperTuneVersion = _database.GetCollection<BsonDocument>(CONSTANTS.ME_HyperTuneVersion);
            var collection_SSAI_RecommendedTrainedModels = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIRecommendedTrainedModels);
            var collection_WF_IngestedData = _database.GetCollection<BsonDocument>(CONSTANTS.WF_IngestedData);
            var collection_WF_TestResults = _database.GetCollection<BsonDocument>(CONSTANTS.WF_TestResults_);
            var collection_WhatIfAnalysis = _database.GetCollection<BsonDocument>(CONSTANTS.WhatIfAnalysis);
            var collection_PreProcessedData = _database.GetCollection<BsonDocument>(CONSTANTS.DEPreProcessedData);
            var collection_FilteredData = _database.GetCollection<BsonDocument>(CONSTANTS.DataCleanUPFilteredData);
            var collection_FeatureSelection = _database.GetCollection<BsonDocument>(CONSTANTS.MEFeatureSelection);
            var collection_RecommendedModels = _database.GetCollection<BsonDocument>(CONSTANTS.MERecommendedModels);
            var collection_TeachAndTest = _database.GetCollection<BsonDocument>(CONSTANTS.ME_TeachAndTest);

            var collection_DeliveryConstructStructures = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_DeliveryConstructStructures);
            var collection_DeployedModels = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
            var collection_PublicTemplates = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIPublicTemplates);
            var collection_PublishModel = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_PublishModel);
            var collection_UseCase = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIUseCase);

            if (collection_DataCleanup.Find(filter).ToList().Count > 0)
            {
                collection_DataCleanup.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.SuccessDE_DataCleanup;
            }

            if (collection_DataProcessing.Find(filter).ToList().Count > 0)
            {
                collection_DataProcessing.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.SuccessDE_DataProcessing;
            }
            if (collection_IngrainDeliveryConstruct.Find(filter).ToList().Count > 0)
            {
                collection_IngrainDeliveryConstruct.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.SuccessDE_IngrainDeliveryConstruct;
            }
            if (collection_HyperTuneVersion.Find(filter).ToList().Count > 0)
            {
                collection_HyperTuneVersion.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.SuccessME_HyperTuneVersion;
            }
            if (collection_SSAI_RecommendedTrainedModels.Find(filter).ToList().Count > 0)
            {
                collection_SSAI_RecommendedTrainedModels.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.SuccessSSAI_RecommendedTrainedModels;
            }
            if (collection_WF_IngestedData.Find(filter).ToList().Count > 0)
            {
                collection_WF_IngestedData.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.SuccessWF_IngestedData;
            }
            if (collection_WF_TestResults.Find(filter).ToList().Count > 0)
            {
                collection_WF_TestResults.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.SuccessWF_TestResults;
            }
            if (collection_WhatIfAnalysis.Find(filter).ToList().Count > 0)
            {
                collection_WhatIfAnalysis.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.SuccessWhatIfAnalysis;
            }

            if (collection_PreProcessedData.Find(filter).ToList().Count > 0)
            {
                collection_PreProcessedData.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.SuccessDE_PreProcessedData;
            }
            if (collection_FilteredData.Find(filter).ToList().Count > 0)
            {
                collection_FilteredData.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.SuccessDataCleanUP_FilteredData;
            }
            if (collection_FeatureSelection.Find(filter).ToList().Count > 0)
            {
                collection_FeatureSelection.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.SuccessME_FeatureSelection;
            }
            if (collection_RecommendedModels.Find(filter).ToList().Count > 0)
            {
                collection_RecommendedModels.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.SuccessME_RecommendedModels;
            }
            if (collection_TeachAndTest.Find(filter).ToList().Count > 0)
            {
                collection_TeachAndTest.DeleteMany(filter);
                flushStatus = flushStatus + " + success for ME_TeachAndTest";
            }


            if (collection_DeliveryConstructStructures.Find(filter).ToList().Count > 0)
            {
                collection_DeliveryConstructStructures.DeleteMany(filter);
                flushStatus = flushStatus + " + success for SSAI_DeliveryConstructStructures";
            }
            if (collection_DeployedModels.Find(filter).ToList().Count > 0)
            {
                collection_DeployedModels.DeleteMany(filter);
                flushStatus = flushStatus + " + success for SSAI_DeployedModels";
            }
            if (collection_PublicTemplates.Find(filter).ToList().Count > 0)
            {
                collection_PublicTemplates.DeleteMany(filter);
                flushStatus = flushStatus + " + success for SSAI_PublicTemplates";
            }
            if (collection_PublishModel.Find(filter).ToList().Count > 0)
            {
                collection_PublishModel.DeleteMany(filter);
                flushStatus = flushStatus + " + success for SSAI_PublishModel";
            }
            if (collection_UseCase.Find(filter).ToList().Count > 0)
            {
                collection_UseCase.DeleteMany(filter);
                flushStatus = flushStatus + " + success for SSAI_UseCase";
            }

            return flushStatus;
        }
        public string DataSourceDelete(string correlationId, string ServiceName = "")
        {
            string flushStatus = string.Empty;
            IMongoCollection<BsonDocument> collection_savedModels;
            if (ServiceName == "Anomaly")
                collection_savedModels = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.SSAI_savedModels);
            else
                collection_savedModels = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_savedModels);
            var ingrainRequestFilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId) & Builders<BsonDocument>.Filter.Ne(CONSTANTS.pageInfo, CONSTANTS.IngestData);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            //var collection_savedModels = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_savedModels);
            var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.FilePath).Exclude(CONSTANTS.Id);
            var correaltionExist = collection_savedModels.Find(filter).Project<BsonDocument>(projection).ToList();
            if (correaltionExist.Count > 0)
            {
                for (int i = 0; i < correaltionExist.Count; i++)
                {
                    string FileToDelete = correaltionExist[i][CONSTANTS.FilePath].ToString();
                    if (File.Exists(FileToDelete))
                    { File.Delete(FileToDelete); }
                }

                collection_savedModels.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.SuccessSSAI_savedModels;
            }
            IMongoCollection<BsonDocument> collection_BusinessProblem;
            IMongoCollection<BsonDocument> collection_DataCleanup;
            IMongoCollection<BsonDocument> collection_DataProcessing;
            IMongoCollection<BsonDocument> collection_IngrainDeliveryConstruct;
            IMongoCollection<BsonDocument> collection_HyperTuneVersion;
            IMongoCollection<BsonDocument> collection_SSAI_RecommendedTrainedModels;
            IMongoCollection<BsonDocument> collection_WF_IngestedData;
            IMongoCollection<BsonDocument> collection_WF_TestResults;
            IMongoCollection<BsonDocument> collection_WhatIfAnalysis;
            IMongoCollection<BsonDocument> collection_PreProcessedData;
            IMongoCollection<BsonDocument> collection_FilteredData;
            IMongoCollection<BsonDocument> collection_FeatureSelection;
            IMongoCollection<BsonDocument> collection_RecommendedModels;
            IMongoCollection<BsonDocument> collection_TeachAndTest;
            IMongoCollection<BsonDocument> collection_DeliveryConstructStructures;
            IMongoCollection<BsonDocument> collection_DeployedModels;
            IMongoCollection<BsonDocument> collection_PublicTemplates;
            IMongoCollection<BsonDocument> collection_PublishModel;
            IMongoCollection<BsonDocument> collection_UseCase;
            IMongoCollection<BsonDocument> collection_IngrainRequests;
            if (ServiceName == "Anomaly")
            {
                collection_BusinessProblem = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.PSBusinessProblem);

                collection_DataCleanup = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.DEDataCleanup);
                collection_DataProcessing = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.DEDataProcessing);
                collection_IngrainDeliveryConstruct = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.IngrainDeliveryConstruct);
                collection_HyperTuneVersion = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.ME_HyperTuneVersion);
                collection_SSAI_RecommendedTrainedModels = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.SSAIRecommendedTrainedModels);
                collection_WF_IngestedData = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.WF_IngestedData);
                collection_WF_TestResults = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.WF_TestResults_);
                collection_WhatIfAnalysis = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.WhatIfAnalysis);
                collection_PreProcessedData = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.DEPreProcessedData);
                collection_FilteredData = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.DataCleanUPFilteredData);
                collection_FeatureSelection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.MEFeatureSelection);
                collection_RecommendedModels = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.MERecommendedModels);
                collection_TeachAndTest = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.ME_TeachAndTest);

                collection_DeliveryConstructStructures = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.SSAI_DeliveryConstructStructures);
                collection_DeployedModels = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
                collection_PublicTemplates = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.SSAIPublicTemplates);
                collection_PublishModel = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.SSAI_PublishModel);
                collection_UseCase = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.SSAIUseCase);
                collection_IngrainRequests = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
            }
            else
            {
                collection_BusinessProblem = _database.GetCollection<BsonDocument>(CONSTANTS.PSBusinessProblem);

                collection_DataCleanup = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataCleanup);
                collection_DataProcessing = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataProcessing);
                collection_IngrainDeliveryConstruct = _database.GetCollection<BsonDocument>(CONSTANTS.IngrainDeliveryConstruct);
                collection_HyperTuneVersion = _database.GetCollection<BsonDocument>(CONSTANTS.ME_HyperTuneVersion);
                collection_SSAI_RecommendedTrainedModels = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIRecommendedTrainedModels);
                collection_WF_IngestedData = _database.GetCollection<BsonDocument>(CONSTANTS.WF_IngestedData);
                collection_WF_TestResults = _database.GetCollection<BsonDocument>(CONSTANTS.WF_TestResults_);
                collection_WhatIfAnalysis = _database.GetCollection<BsonDocument>(CONSTANTS.WhatIfAnalysis);
                collection_PreProcessedData = _database.GetCollection<BsonDocument>(CONSTANTS.DEPreProcessedData);
                collection_FilteredData = _database.GetCollection<BsonDocument>(CONSTANTS.DataCleanUPFilteredData);
                collection_FeatureSelection = _database.GetCollection<BsonDocument>(CONSTANTS.MEFeatureSelection);
                collection_RecommendedModels = _database.GetCollection<BsonDocument>(CONSTANTS.MERecommendedModels);
                collection_TeachAndTest = _database.GetCollection<BsonDocument>(CONSTANTS.ME_TeachAndTest);

                collection_DeliveryConstructStructures = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_DeliveryConstructStructures);
                collection_DeployedModels = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
                collection_PublicTemplates = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIPublicTemplates);
                collection_PublishModel = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_PublishModel);
                collection_UseCase = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIUseCase);
                collection_IngrainRequests = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
            }

            if (collection_DataCleanup.Find(filter).ToList().Count > 0)
            {
                collection_DataCleanup.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.SuccessDE_DataCleanup;
            }

            if (collection_DataProcessing.Find(filter).ToList().Count > 0)
            {
                collection_DataProcessing.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.SuccessDE_DataProcessing;
            }
            if (collection_IngrainDeliveryConstruct.Find(filter).ToList().Count > 0)
            {
                collection_IngrainDeliveryConstruct.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.SuccessDE_IngrainDeliveryConstruct;
            }
            if (collection_HyperTuneVersion.Find(filter).ToList().Count > 0)
            {
                collection_HyperTuneVersion.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.SuccessME_HyperTuneVersion;
            }
            if (collection_SSAI_RecommendedTrainedModels.Find(filter).ToList().Count > 0)
            {
                collection_SSAI_RecommendedTrainedModels.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.SuccessSSAI_RecommendedTrainedModels;
            }
            if (collection_WF_IngestedData.Find(filter).ToList().Count > 0)
            {
                collection_WF_IngestedData.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.SuccessWF_IngestedData;
            }
            if (collection_WF_TestResults.Find(filter).ToList().Count > 0)
            {
                collection_WF_TestResults.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.SuccessWF_TestResults;
            }
            if (collection_WhatIfAnalysis.Find(filter).ToList().Count > 0)
            {
                collection_WhatIfAnalysis.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.SuccessWhatIfAnalysis;
            }

            if (collection_PreProcessedData.Find(filter).ToList().Count > 0)
            {
                collection_PreProcessedData.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.SuccessDE_PreProcessedData;
            }
            if (collection_FilteredData.Find(filter).ToList().Count > 0)
            {
                collection_FilteredData.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.SuccessDataCleanUP_FilteredData;
            }
            if (collection_FeatureSelection.Find(filter).ToList().Count > 0)
            {
                collection_FeatureSelection.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.SuccessME_FeatureSelection;
            }
            if (collection_RecommendedModels.Find(filter).ToList().Count > 0)
            {
                collection_RecommendedModels.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.SuccessME_RecommendedModels;
            }
            if (collection_TeachAndTest.Find(filter).ToList().Count > 0)
            {
                collection_TeachAndTest.DeleteMany(filter);
                flushStatus = flushStatus + " + success for ME_TeachAndTest";
            }


            if (collection_DeliveryConstructStructures.Find(filter).ToList().Count > 0)
            {
                collection_DeliveryConstructStructures.DeleteMany(filter);
                flushStatus = flushStatus + " + success for SSAI_DeliveryConstructStructures";
            }
            //if (collection_DeployedModels.Find(filter).ToList().Count > 0)
            //{
            //    collection_DeployedModels.DeleteMany(filter);
            //    flushStatus = flushStatus + " + success for SSAI_DeployedModels";
            //}
            if (collection_PublicTemplates.Find(filter).ToList().Count > 0)
            {
                collection_PublicTemplates.DeleteMany(filter);
                flushStatus = flushStatus + " + success for SSAI_PublicTemplates";
            }
            if (collection_PublishModel.Find(filter).ToList().Count > 0)
            {
                collection_PublishModel.DeleteMany(filter);
                flushStatus = flushStatus + " + success for SSAI_PublishModel";
            }
            if (collection_UseCase.Find(filter).ToList().Count > 0)
            {
                collection_UseCase.DeleteMany(filter);
                flushStatus = flushStatus + " + success for SSAI_UseCase";
            }

            if (collection_IngrainRequests.Find(ingrainRequestFilter).ToList().Count > 0)
            {
                collection_IngrainRequests.DeleteMany(ingrainRequestFilter);
                flushStatus = flushStatus + CONSTANTS.SuccessSSAI_IngrainRequests;
            }

            return flushStatus;
        }
        public void DeleteBaseData(string correlationId)
        {
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.PSIngestedData);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            collection.DeleteMany(filter);
        }

        private string FlushByCollections(FilterDefinition<BsonDocument> filter, string message)
        {
            string flushStatus = string.Empty;

            var collection_DeployedModels = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_DeployedModels);
            var collection_CascadeVisualization = _database.GetCollection<BsonDocument>(CONSTANTS.SSAICascadeVisualization);
            var collection_savedModels = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_savedModels);
            var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.FilePath).Exclude(CONSTANTS.Id);
            var correaltionExist = collection_savedModels.Find(filter).Project<BsonDocument>(projection).ToList();
            if (correaltionExist.Count > 0)
            {
                for (int i = 0; i < correaltionExist.Count; i++)
                {
                    string FileToDelete = correaltionExist[i][CONSTANTS.FilePath].ToString();
                    if (File.Exists(FileToDelete))
                    { File.Delete(FileToDelete); }
                }

                collection_savedModels.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.SSAI_savedModels + message;
            }
            var collection_IngestedData = _database.GetCollection<BsonDocument>(CONSTANTS.PS_IngestedData);
            var collection_BusinessProblem = _database.GetCollection<BsonDocument>(CONSTANTS.PS_BusinessProblem);
            var collection_DataCleanup = _database.GetCollection<BsonDocument>(CONSTANTS.DE_DataCleanup);
            var collection_DataProcessing = _database.GetCollection<BsonDocument>(CONSTANTS.DE_DataProcessing);
            var collection_SSAI_RecommendedTrainedModels = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_RecommendedTrainedModels);
            var collection_FilteredData = _database.GetCollection<BsonDocument>(CONSTANTS.DataCleanUP_FilteredData);
            var collection_DE_DataProcessing = _database.GetCollection<BsonDocument>(CONSTANTS.DE_DataProcessing);
            var collection_DEPreProcessedData = _database.GetCollection<BsonDocument>(CONSTANTS.DEPreProcessedData);
            var collection_FeatureSelection = _database.GetCollection<BsonDocument>(CONSTANTS.ME_FeatureSelection);
            var collection_RecommendedModels = _database.GetCollection<BsonDocument>(CONSTANTS.ME_RecommendedModels);
            var collection_HyperTuneVersion = _database.GetCollection<BsonDocument>(CONSTANTS.ME_HyperTuneVersion);
            var collection_WF_IngestedData = _database.GetCollection<BsonDocument>(CONSTANTS.WF_IngestedData);
            var collection_WF_TestResults = _database.GetCollection<BsonDocument>(CONSTANTS.WF_TestResults_);
            var collection_WhatIfAnalysis = _database.GetCollection<BsonDocument>(CONSTANTS.WhatIfAnalysis);
            var collection_PublicTemplates = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_PublicTemplates);
            var collection_PublishModel = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_PublishModel);
            var collection_IngrainRequests = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
            var collection_DeployedPublishModel = _database.GetCollection<BsonDocument>(CONSTANTS.DeployedPublishModel);
            var collection_IngrainDeliveryConstruct = _database.GetCollection<BsonDocument>(CONSTANTS.IngrainDeliveryConstruct);
            var collection_PreProcessedData = _database.GetCollection<BsonDocument>(CONSTANTS.DEPreProcessedData);
            var collection_UseCase = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIUseCase);
            var collection_CascadeModel = _database.GetCollection<BsonDocument>(CONSTANTS.SSAICascadedModels);
            var collection_FMVisualization = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIFMVisualization);
            var collection_PSUseCaseDefinition = _database.GetCollection<BsonDocument>(CONSTANTS.PSUseCaseDefinition);
            var collection_AuditTrailLog = _database.GetCollection<BsonDocument>(CONSTANTS.AuditTrailLog);
            var collection_PrescriptiveAnalyticsResults = _database.GetCollection<BsonDocument>(CONSTANTS.PrescriptiveAnalyticsResults);
            var collection_PublicTemplateMapping = _database.GetCollection<BsonDocument>(CONSTANTS.PublicTemplateMapping);

            var collection_SSAI_UserDetails = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_UserDetails);
            var collection_DataVisualization = _database.GetCollection<BsonDocument>(CONSTANTS.DE_DataVisualization);
            var collection_DeliveryConstructStructures = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_DeliveryConstructStructures);

            var collection_DE_AddNewFeature = _database.GetCollection<BsonDocument>(CONSTANTS.DEAddNewFeature);
            if (collection_DE_AddNewFeature.Find(filter).ToList().Count > 0)
            {
                collection_IngestedData.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.DEAddNewFeature + message;
            }
            var collection_TeachAndTest = _database.GetCollection<BsonDocument>(CONSTANTS.ME_TeachAndTest);
            if (collection_TeachAndTest.Find(filter).ToList().Count > 0)
            {
                collection_TeachAndTest.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.ME_TeachAndTest + message;
            }
            var collection_AICoreModels = _database.GetCollection<BsonDocument>(CONSTANTS.AICoreModels);
            if (collection_AICoreModels.Find(filter).ToList().Count > 0)
            {
                collection_AICoreModels.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.AICoreModels + message;
            }
            var collection_AISavedUsecases = _database.GetCollection<BsonDocument>(CONSTANTS.AISavedUsecases);
            if (collection_AISavedUsecases.Find(filter).ToList().Count > 0)
            {
                collection_AISavedUsecases.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.AISavedUsecases + message;
            }
            var collection_AIServiceIngestData = _database.GetCollection<BsonDocument>(CONSTANTS.AIServiceIngestData);
            if (collection_AIServiceIngestData.Find(filter).ToList().Count > 0)
            {
                collection_AIServiceIngestData.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.AIServiceIngestData + message;
            }
            var collection_AIServiceRequestStatus = _database.GetCollection<BsonDocument>(CONSTANTS.AIServiceRequestStatus);
            if (collection_AIServiceRequestStatus.Find(filter).ToList().Count > 0)
            {
                collection_AIServiceRequestStatus.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.AIServiceRequestStatus + message;
            }
            var collection_AIServicesPrediction = _database.GetCollection<BsonDocument>(CONSTANTS.AIServicesPrediction);
            if (collection_AIServicesPrediction.Find(filter).ToList().Count > 0)
            {
                collection_AIServicesPrediction.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.AIServicesPrediction + message;
            }
            var collection_Clustering_DataPreprocessing = _database.GetCollection<BsonDocument>(CONSTANTS.Clustering_DataPreprocessing);
            if (collection_Clustering_DataPreprocessing.Find(filter).ToList().Count > 0)
            {
                collection_Clustering_DataPreprocessing.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.Clustering_DataPreprocessing + message;
            }
            var collection_Clustering_IngestData = _database.GetCollection<BsonDocument>(CONSTANTS.Clustering_IngestData);
            if (collection_Clustering_IngestData.Find(filter).ToList().Count > 0)
            {
                collection_Clustering_IngestData.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.Clustering_IngestData + message;
            }
            var collection_Clustering_StatusTable = _database.GetCollection<BsonDocument>(CONSTANTS.Clustering_StatusTable);
            if (collection_Clustering_StatusTable.Find(filter).ToList().Count > 0)
            {
                collection_Clustering_StatusTable.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.Clustering_StatusTable + message;
            }
            var collection_Clustering_Visualization = _database.GetCollection<BsonDocument>(CONSTANTS.ClusteringVisualization);
            if (collection_Clustering_Visualization.Find(filter).ToList().Count > 0)
            {
                collection_Clustering_Visualization.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.ClusteringVisualization + message;
            }
            var collection_AICore_Preprocessing = _database.GetCollection<BsonDocument>(CONSTANTS.AICore_Preprocessing);
            if (collection_AICore_Preprocessing.Find(filter).ToList().Count > 0)
            {
                collection_AICore_Preprocessing.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.AICore_Preprocessing + message;
            }
            var collection_AIServiceRecordsDetails = _database.GetCollection<BsonDocument>(CONSTANTS.AIServiceRecordsDetails);
            if (collection_AIServiceRecordsDetails.Find(filter).ToList().Count > 0)
            {
                collection_AIServiceRecordsDetails.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.AIServiceRecordsDetails + message;
            }
            var collection_AIServicesSentimentPrediction = _database.GetCollection<BsonDocument>(CONSTANTS.AIServicesSentimentPrediction);
            if (collection_AIServicesSentimentPrediction.Find(filter).ToList().Count > 0)
            {
                collection_AIServicesSentimentPrediction.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.AIServicesSentimentPrediction + message;
            }
            var collection_AIServicesTextSummaryPrediction = _database.GetCollection<BsonDocument>(CONSTANTS.AIServicesTextSummaryPrediction);
            if (collection_AIServicesTextSummaryPrediction.Find(filter).ToList().Count > 0)
            {
                collection_AIServicesTextSummaryPrediction.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.AIServicesSentimentPrediction + message;
            }
            var collection_AppNotificationLog = _database.GetCollection<BsonDocument>(CONSTANTS.AppNotificationLog);
            if (collection_AppNotificationLog.Find(filter).ToList().Count > 0)
            {
                collection_AppNotificationLog.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.AppNotificationLog + message;
            }
            var collection_Clustering_BusinessProblem = _database.GetCollection<BsonDocument>(CONSTANTS.Clustering_BusinessProblem);
            if (collection_Clustering_BusinessProblem.Find(filter).ToList().Count > 0)
            {
                collection_Clustering_BusinessProblem.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.Clustering_BusinessProblem + message;
            }
            var collection_Clustering_DE_DataCleanup = _database.GetCollection<BsonDocument>(CONSTANTS.Clustering_DE_DataCleanup);
            if (collection_Clustering_DE_DataCleanup.Find(filter).ToList().Count > 0)
            {
                collection_Clustering_DE_DataCleanup.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.Clustering_DE_DataCleanup + message;
            }
            var collection_Clustering_DE_PreProcessedData = _database.GetCollection<BsonDocument>(CONSTANTS.Clustering_DE_PreProcessedData);
            if (collection_Clustering_DE_PreProcessedData.Find(filter).ToList().Count > 0)
            {
                collection_Clustering_DE_PreProcessedData.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.Clustering_DE_PreProcessedData + message;
            }
            var collection_Clustering_DataCleanUP_FilteredData = _database.GetCollection<BsonDocument>(CONSTANTS.Clustering_DataCleanUP_FilteredData);
            if (collection_Clustering_DataCleanUP_FilteredData.Find(filter).ToList().Count > 0)
            {
                collection_Clustering_DataCleanUP_FilteredData.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.Clustering_DataCleanUP_FilteredData + message;
            }
            var collection_Clustering_Eval = _database.GetCollection<BsonDocument>(CONSTANTS.Clustering_Eval);
            if (collection_Clustering_Eval.Find(filter).ToList().Count > 0)
            {
                collection_Clustering_Eval.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.Clustering_Eval + message;
            }
            var collection_Clustering_EvalTestResults = _database.GetCollection<BsonDocument>(CONSTANTS.Clustering_EvalTestResults);
            if (collection_Clustering_EvalTestResults.Find(filter).ToList().Count > 0)
            {
                collection_Clustering_EvalTestResults.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.Clustering_EvalTestResults + message;
            }
            var collection_Clustering_SSAI_savedModels = _database.GetCollection<BsonDocument>(CONSTANTS.Clustering_SSAI_savedModels);
            if (collection_Clustering_SSAI_savedModels.Find(filter).ToList().Count > 0)
            {
                collection_Clustering_SSAI_savedModels.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.Clustering_SSAI_savedModels + message;
            }
            var collection_Clustering_TrainedModels = _database.GetCollection<BsonDocument>(CONSTANTS.Clustering_TrainedModels);
            if (collection_Clustering_TrainedModels.Find(filter).ToList().Count > 0)
            {
                collection_Clustering_TrainedModels.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.Clustering_TrainedModels + message;
            }
            var collection_Clustering_ViewMappedData = _database.GetCollection<BsonDocument>(CONSTANTS.Clustering_ViewMappedData);
            if (collection_Clustering_ViewMappedData.Find(filter).ToList().Count > 0)
            {
                collection_Clustering_ViewMappedData.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.Clustering_ViewMappedData + message;
            }
            var collection_Clustering_ViewTrainedData = _database.GetCollection<BsonDocument>(CONSTANTS.Clustering_ViewTrainedData);
            if (collection_Clustering_ViewTrainedData.Find(filter).ToList().Count > 0)
            {
                collection_Clustering_ViewTrainedData.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.Clustering_ViewTrainedData + message;
            }
            var collection_DE_NewFeatureData = _database.GetCollection<BsonDocument>(CONSTANTS.DE_NewFeatureData);
            if (collection_DE_NewFeatureData.Find(filter).ToList().Count > 0)
            {
                collection_DE_NewFeatureData.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.DE_NewFeatureData + message;
            }
            var collection_PredictionSchedulerLog = _database.GetCollection<BsonDocument>(CONSTANTS.PredictionSchedulerLog);
            if (collection_PredictionSchedulerLog.Find(filter).ToList().Count > 0)
            {
                collection_PredictionSchedulerLog.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.PredictionSchedulerLog + message;
            }
            var collection_SSAI_PredictedData = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_PredictedData);
            if (collection_SSAI_PredictedData.Find(filter).ToList().Count > 0)
            {
                collection_SSAI_PredictedData.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.SSAI_PredictedData + message;
            }



            var deploycollection = collection_DeployedModels.Find(filter).ToList();
            if (deploycollection.Count > 0)
            {
                bool isFmModel = false;
                if (deploycollection[0].Contains(CONSTANTS.IsFMModel))
                {
                    isFmModel = Convert.ToBoolean(deploycollection[0][CONSTANTS.IsFMModel]);
                    if (isFmModel)
                    {
                        string fmcorrelationId = deploycollection[0][CONSTANTS.FMCorrelationId].ToString();
                        var fmFilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, fmcorrelationId);
                        collection_DeployedModels.DeleteMany(fmFilter);
                        flushStatus = flushStatus + " FM SSAI_DeployedModels" + message;
                        //For hide model request deletion
                        collection_IngrainRequests.DeleteMany(fmFilter);
                        flushStatus = flushStatus + " FM SSAIIngrainRequests" + message;
                    }
                }
                collection_DeployedModels.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.SSAI_DeployedModels + message;
            }
            if (collection_IngestedData.Find(filter).ToList().Count > 0)
            {
                collection_IngestedData.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.PS_IngestedData + message;
            }
            if (collection_DataCleanup.Find(filter).ToList().Count > 0)
            {
                collection_DataCleanup.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.DE_DataCleanup + message;
            }
            if (collection_DE_DataProcessing.Find(filter).ToList().Count > 0)
            {
                collection_DE_DataProcessing.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.DE_DataProcessing + message;
            }
            if (collection_SSAI_RecommendedTrainedModels.Find(filter).ToList().Count > 0)
            {
                collection_SSAI_RecommendedTrainedModels.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.SSAI_RecommendedTrainedModels + message;
            }
            if (collection_FilteredData.Find(filter).ToList().Count > 0)
            {
                collection_FilteredData.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.DataCleanUP_FilteredData + message;
            }
            if (collection_FeatureSelection.Find(filter).ToList().Count > 0)
            {
                collection_FeatureSelection.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.ME_FeatureSelection + message;
            }
            if (collection_RecommendedModels.Find(filter).ToList().Count > 0)
            {
                collection_RecommendedModels.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.ME_RecommendedModels + message;
            }
            if (collection_PublicTemplates.Find(filter).ToList().Count > 0)
            {
                collection_PublicTemplates.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.SSAI_PublicTemplates + message;
            }
            if (collection_PublishModel.Find(filter).ToList().Count > 0)
            {
                collection_PublishModel.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.SSAI_PublishModel + message;
            }
            if (collection_IngrainRequests.Find(filter).ToList().Count > 0)
            {
                collection_IngrainRequests.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.SSAIIngrainRequests + message;
            }

            if (collection_DeployedPublishModel.Find(filter).ToList().Count > 0)
            {
                collection_DeployedPublishModel.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.DeployedPublishModel + message;
            }
            if (collection_IngrainDeliveryConstruct.Find(filter).ToList().Count > 0)
            {
                collection_IngrainDeliveryConstruct.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.IngrainDeliveryConstruct + message;
            }
            if (collection_HyperTuneVersion.Find(filter).ToList().Count > 0)
            {
                collection_HyperTuneVersion.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.ME_HyperTuneVersion + message;
            }
            if (collection_SSAI_UserDetails.Find(filter).ToList().Count > 0)
            {
                collection_SSAI_UserDetails.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.SSAI_UserDetails + message;
            }
            if (collection_WF_IngestedData.Find(filter).ToList().Count > 0)
            {
                collection_WF_IngestedData.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.WF_IngestedData + message;
            }
            if (collection_WF_TestResults.Find(filter).ToList().Count > 0)
            {
                collection_WF_TestResults.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.WF_TestResults_ + message;
            }
            if (collection_WhatIfAnalysis.Find(filter).ToList().Count > 0)
            {
                collection_WhatIfAnalysis.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.WhatIfAnalysis + message;
            }
            if (collection_DataVisualization.Find(filter).ToList().Count > 0)
            {
                collection_DataVisualization.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.DE_DataVisualization + message;
            }
            if (collection_DEPreProcessedData.Find(filter).ToList().Count > 0)
            {
                collection_DEPreProcessedData.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.DE_PreProcessedData + message;
            }
            if (collection_PreProcessedData.Find(filter).ToList().Count > 0)
            {
                collection_PreProcessedData.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.DE_PreProcessedData + message;
            }
            if (collection_TeachAndTest.Find(filter).ToList().Count > 0)
            {
                collection_TeachAndTest.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.ME_TeachAndTest + message;
            }
            if (collection_DeliveryConstructStructures.Find(filter).ToList().Count > 0)
            {
                collection_DeliveryConstructStructures.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.SSAI_DeliveryConstructStructures + message;
            }
            if (collection_UseCase.Find(filter).ToList().Count > 0)
            {
                collection_UseCase.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.SSAIUseCase + message;
            }
            if (collection_CascadeModel.Find(filter).ToList().Count > 0)
            {
                collection_CascadeModel.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.SSAICascadedModels + message;
            }
            if (collection_CascadeVisualization.Find(filter).ToList().Count > 0)
            {
                collection_CascadeVisualization.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.SSAICascadeVisualization + message;
            }
            if (collection_FMVisualization.Find(filter).ToList().Count > 0)
            {
                collection_FMVisualization.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.SSAIFMVisualization + message;
            }
            if (collection_BusinessProblem.Find(filter).ToList().Count > 0)
            {
                collection_BusinessProblem.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.PS_BusinessProblem + message;
            }
            if (collection_FilteredData.Find(filter).ToList().Count > 0)
            {
                collection_FilteredData.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.DataCleanUP_FilteredData + message;
            }
            if (collection_PSUseCaseDefinition.Find(filter).ToList().Count > 0)
            {
                collection_PSUseCaseDefinition.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.PSUseCaseDefinition + message;
            }
            if (collection_AuditTrailLog.Find(filter).ToList().Count > 0)
            {
                collection_AuditTrailLog.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.AuditTrailLog + message;
            }
            if (collection_PrescriptiveAnalyticsResults.Find(filter).ToList().Count > 0)
            {
                collection_PrescriptiveAnalyticsResults.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.PrescriptiveAnalyticsResults + message;
            }
            if (collection_PublicTemplateMapping.Find(filter).ToList().Count > 0)
            {
                collection_PublicTemplateMapping.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.PublicTemplateMapping + message;
            }

            return flushStatus;
        }
        public string Validate(string date)
        {
            DateTime dt;
            if (!DateTime.TryParseExact(date, CONSTANTS.DateHoursFormat,
                            System.Globalization.CultureInfo.InvariantCulture,
                            DateTimeStyles.None, out dt))
            {
                return CONSTANTS.Invalid;
            }
            else
            {
                return CONSTANTS.Valid;
            }
        }
        public string DeleteDateRange(string StartDate, string EndDate)
        {
            string flushStatus = string.Empty;
            var filter = Builders<BsonDocument>.Filter.Gte(CONSTANTS.CreatedOn, StartDate) & Builders<BsonDocument>.Filter.Lte(CONSTANTS.CreatedOn, EndDate);
            var filter1 = Builders<BsonDocument>.Filter.Gte(CONSTANTS.CreatedDate, StartDate) & Builders<BsonDocument>.Filter.Lte(CONSTANTS.CreatedDate, EndDate);
            string message = string.Format(CONSTANTS.FlushMessage, StartDate, EndDate);
            var collection_SSAI_DeployedModels = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_DeployedModels);
            if (collection_SSAI_DeployedModels.Find(filter).ToList().Count > 0)
            {
                collection_SSAI_DeployedModels.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.SSAI_DeployedModels + message;
            }
            var collection_AIServicesSentimentPrediction = _database.GetCollection<BsonDocument>(CONSTANTS.AIServicesSentimentPrediction);
            if (collection_AIServicesSentimentPrediction.Find(filter).ToList().Count > 0)
            {
                collection_AIServicesSentimentPrediction.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.AIServicesSentimentPrediction + message;
            }
            var collection_DE_DataProcessing = _database.GetCollection<BsonDocument>(CONSTANTS.DE_DataProcessing);
            if (collection_DE_DataProcessing.Find(filter).ToList().Count > 0)
            {
                collection_DE_DataProcessing.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.DE_DataProcessing + message;
            }
            var collection_PS_BusinessProblem = _database.GetCollection<BsonDocument>(CONSTANTS.PS_BusinessProblem);
            if (collection_PS_BusinessProblem.Find(filter).ToList().Count > 0)
            {
                collection_PS_BusinessProblem.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.PS_BusinessProblem + message;
            }
            var collection_DE_PreProcessedData = _database.GetCollection<BsonDocument>(CONSTANTS.DE_PreProcessedData);
            if (collection_DE_PreProcessedData.Find(filter).ToList().Count > 0)
            {
                collection_DE_PreProcessedData.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.DE_PreProcessedData + message;
            }
            var collection_WF_IngestedData = _database.GetCollection<BsonDocument>(CONSTANTS.WF_IngestedData);
            if (collection_WF_IngestedData.Find(filter).ToList().Count > 0)
            {
                collection_WF_IngestedData.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.WF_IngestedData + message;
            }
            var collection_WF_TestResults = _database.GetCollection<BsonDocument>(CONSTANTS.WF_TestResults);
            if (collection_WF_TestResults.Find(filter).ToList().Count > 0)
            {
                collection_WF_TestResults.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.WF_TestResults + message;
            }
            var collection_WhatIfAnalysis = _database.GetCollection<BsonDocument>(CONSTANTS.WhatIfAnalysis);
            if (collection_WhatIfAnalysis.Find(filter).ToList().Count > 0)
            {
                collection_WhatIfAnalysis.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.WhatIfAnalysis + message;
            }
            var collection_SSAI_PublicTemplates = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_PublicTemplates);
            if (collection_SSAI_PublicTemplates.Find(filter).ToList().Count > 0)
            {
                collection_SSAI_PublicTemplates.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.SSAI_PublicTemplates + message;
            }
            var collection_SSAI_IngrainRequests = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
            if (collection_SSAI_IngrainRequests.Find(filter).ToList().Count > 0)
            {
                collection_SSAI_IngrainRequests.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.SSAIIngrainRequests + message;
            }
            var collection_SSAI_PublishModel = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_PublishModel);
            if (collection_SSAI_PublishModel.Find(filter).ToList().Count > 0)
            {
                collection_SSAI_PublishModel.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.SSAI_PublishModel + message;
            }
            var collection_AuditTrailLog = _database.GetCollection<BsonDocument>(CONSTANTS.AuditTrailLog);
            if (collection_AuditTrailLog.Find(filter).ToList().Count > 0)
            {
                collection_AuditTrailLog.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.AuditTrailLog + message;
            }
            var collection_PrescriptiveAnalyticsResults = _database.GetCollection<BsonDocument>(CONSTANTS.PrescriptiveAnalyticsResults);
            if (collection_PrescriptiveAnalyticsResults.Find(filter).ToList().Count > 0)
            {
                collection_PrescriptiveAnalyticsResults.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.PrescriptiveAnalyticsResults + message;
            }
            var collection_PublicTemplateMapping = _database.GetCollection<BsonDocument>(CONSTANTS.PublicTemplateMapping);
            if (collection_PublicTemplateMapping.Find(filter).ToList().Count > 0)
            {
                collection_PublicTemplateMapping.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.PublicTemplateMapping + message;
            }
            var collection_Clustering_IngestData = _database.GetCollection<BsonDocument>(CONSTANTS.Clustering_IngestData);
            if (collection_Clustering_IngestData.Find(filter).ToList().Count > 0)
            {
                collection_Clustering_IngestData.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.Clustering_IngestData + message;
            }
            var collection_AIServiceRecordsDetails = _database.GetCollection<BsonDocument>(CONSTANTS.AIServiceRecordsDetails);
            if (collection_AIServiceRecordsDetails.Find(filter).ToList().Count > 0)
            {
                collection_AIServiceRecordsDetails.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.AIServiceRecordsDetails + message;
            }
            var collection_AppNotificationLog = _database.GetCollection<BsonDocument>(CONSTANTS.AppNotificationLog);
            if (collection_AppNotificationLog.Find(filter).ToList().Count > 0)
            {
                collection_AppNotificationLog.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.AppNotificationLog + message;
            }
            var collection_Clustering_BusinessProblem = _database.GetCollection<BsonDocument>(CONSTANTS.Clustering_BusinessProblem);
            if (collection_Clustering_BusinessProblem.Find(filter).ToList().Count > 0)
            {
                collection_Clustering_BusinessProblem.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.Clustering_BusinessProblem + message;
            }
            var collection_Clustering_DE_PreProcessedData = _database.GetCollection<BsonDocument>(CONSTANTS.Clustering_DE_PreProcessedData);
            if (collection_Clustering_DE_PreProcessedData.Find(filter).ToList().Count > 0)
            {
                collection_Clustering_DE_PreProcessedData.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.Clustering_DE_PreProcessedData + message;
            }
            var collection_Clustering_Eval = _database.GetCollection<BsonDocument>(CONSTANTS.Clustering_Eval);
            if (collection_Clustering_Eval.Find(filter1).ToList().Count > 0)
            {
                collection_Clustering_Eval.DeleteMany(filter1);
                flushStatus = flushStatus + CONSTANTS.Clustering_Eval + message;
            }
            var collection_Clustering_EvalTestResults = _database.GetCollection<BsonDocument>(CONSTANTS.Clustering_EvalTestResults);
            if (collection_Clustering_EvalTestResults.Find(filter).ToList().Count > 0)
            {
                collection_Clustering_EvalTestResults.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.Clustering_EvalTestResults + message;
            }
            var collection_PredictionSchedulerLog = _database.GetCollection<BsonDocument>(CONSTANTS.PredictionSchedulerLog);
            if (collection_PredictionSchedulerLog.Find(filter).ToList().Count > 0)
            {
                collection_PredictionSchedulerLog.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.PredictionSchedulerLog + message;
            }
            var collection_PS_IngestedData = _database.GetCollection<BsonDocument>(CONSTANTS.PS_IngestedData);
            var Projection = Builders<BsonDocument>.Projection.Exclude("InputData").Exclude(CONSTANTS.Id);
            var PS_IngestedDataResult = collection_PS_IngestedData.Find(filter).Project<BsonDocument>(Projection).ToList();
            if (PS_IngestedDataResult.Count > 0)
            {
                collection_PS_IngestedData.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.PS_IngestedData + message;
            }
            var collection_Clustering_ViewMappedData = _database.GetCollection<BsonDocument>(CONSTANTS.Clustering_ViewMappedData);
            var Clustering_ViewMappedDataResult = collection_Clustering_ViewMappedData.Find(filter).Project<BsonDocument>(Projection).ToList();
            if (Clustering_ViewMappedDataResult.Count > 0)
            {
                collection_Clustering_ViewMappedData.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.Clustering_ViewMappedData + message;
            }
            var collection_Clustering_ViewTrainedData = _database.GetCollection<BsonDocument>(CONSTANTS.Clustering_ViewTrainedData);
            var Clustering_ViewTrainedDataResult = collection_Clustering_ViewTrainedData.Find(filter).Project<BsonDocument>(Projection).ToList();
            if (Clustering_ViewTrainedDataResult.Count > 0)
            {
                collection_Clustering_ViewTrainedData.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.Clustering_ViewTrainedData + message;
            }
            var collection_DE_NewFeatureData = _database.GetCollection<BsonDocument>(CONSTANTS.DE_NewFeatureData);
            var collection_DE_NewFeatureDataResult = collection_DE_NewFeatureData.Find(filter).Project<BsonDocument>(Projection).ToList();
            if (collection_DE_NewFeatureDataResult.Count > 0)
            {
                collection_DE_NewFeatureData.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.DE_NewFeatureData + message;
            }
            var collection_SSAI_PredictedData = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_PredictedData);
            var Projection1 = Builders<BsonDocument>.Projection.Exclude("PredictedResult").Exclude(CONSTANTS.Id);
            var collection_SSAI_PredictedDataResult = collection_SSAI_PredictedData.Find(filter).Project<BsonDocument>(Projection1).ToList();
            if (collection_SSAI_PredictedDataResult.Count > 0)
            {
                collection_SSAI_PredictedData.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.SSAI_PredictedData + message;
            }
            var collection_SSAI_CascadeVisualization = _database.GetCollection<BsonDocument>(CONSTANTS.SSAICascadeVisualization);
            var Projection2 = Builders<BsonDocument>.Projection.Exclude("Visualization").Exclude(CONSTANTS.Id);
            var SSAI_CascadeVisualizationResult = collection_SSAI_CascadeVisualization.Find(filter).Project<BsonDocument>(Projection2).ToList();
            if (SSAI_CascadeVisualizationResult.Count > 0)
            {
                collection_SSAI_CascadeVisualization.DeleteMany(filter);
                flushStatus = flushStatus + CONSTANTS.SSAICascadeVisualization + message;
            }

            return flushStatus;
        }
        public string DeleteCorrelationIds(string[] correlationIds)
        {
            string flushStatus = string.Empty;
            foreach (var corId in correlationIds)
            {
                CommonUtility.ValidateInputFormData(Convert.ToString(corId), "CorrelationId", true);
                var deployModelCollection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
                var Projection1 = Builders<DeployModelsDto>.Projection.Exclude(CONSTANTS.Id);
                var filter1 = Builders<DeployModelsDto>.Filter.Eq(CONSTANTS.CorrelationId, corId);
                var mdl = deployModelCollection.Find(filter1).Project<DeployModelsDto>(Projection1).FirstOrDefault();
                List<string> appLst = new List<string>() { CONSTANTS.VDSApplicationID_PAD, CONSTANTS.VDSApplicationID_FDS, CONSTANTS.VDSApplicationID_PAM };
                if (mdl != null)
                {
                    if (mdl.IsCascadeModel == true)
                    {
                        if (mdl.IsCascadeModel && !mdl.IsModelTemplate)
                        {
                            if (appLst.Contains(mdl.AppId))
                            {
                                SendVDSDeployModelNotification(mdl, OperationTypes.Deleted.ToString(), true);
                            }
                        }
                    }
                    else
                    {
                        if (appLst.Contains(mdl.AppId))
                        {
                            SendVDSDeployModelNotification(mdl, OperationTypes.Deleted.ToString(), false);
                        }
                    }
                }
                var cascadeFilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CascadedId, corId);
                var cascadeProjection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
                var collection_CascadeModel = _database.GetCollection<BsonDocument>(CONSTANTS.SSAICascadedModels);
                var cascadeResult = collection_CascadeModel.Find(cascadeFilter).Project<BsonDocument>(cascadeProjection).ToList();
                if (cascadeResult.Count > 0)
                {
                    JObject data = JObject.Parse(cascadeResult[0].ToString());
                    if (data != null)
                    {
                        if (data[CONSTANTS.ModelList].Children().Count() > 0)
                        {
                            string cascadeId = data["CascadedId"].ToString();
                            bool isCusomtModel = Convert.ToBoolean(data[CONSTANTS.IsCustomModel]);
                            string model1Corid = data[CONSTANTS.ModelList]["Model1"][CONSTANTS.CorrelationId].ToString();
                            int i = 1;
                            foreach (var item in data[CONSTANTS.ModelList].Children())
                            {
                                var model = item as JProperty;
                                if (model != null)
                                {
                                    if (isCusomtModel)
                                    {
                                        if (i > 1)
                                        {
                                            //Revert the model to normal model.
                                            CascadeModelsCollection cascadeModel = JsonConvert.DeserializeObject<CascadeModelsCollection>(model.Value.ToString());
                                            string modelCount = string.Format("Model{0}", i - 1);
                                            CascadeModelsCollection previousModel = JsonConvert.DeserializeObject<CascadeModelsCollection>(data[CONSTANTS.MappingData][modelCount].ToString());
                                            var businesscolection = _database.GetCollection<BusinessProblem>(CONSTANTS.PSBusinessProblem);
                                            var businessFilter = Builders<BusinessProblem>.Filter.Eq(CONSTANTS.CorrelationId, cascadeModel.CorrelationId);
                                            var psProjection = Builders<BusinessProblem>.Projection.Include(CONSTANTS.InputColumns).Exclude(CONSTANTS.Id);
                                            var psResult = businesscolection.Find(businessFilter).Project<BusinessProblem>(psProjection).ToList();
                                            if (psResult.Count > 0)
                                            {
                                                //Reverting model
                                                string val = null;
                                                var collection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
                                                var filter3 = Builders<DeployModelsDto>.Filter.Eq(CONSTANTS.CorrelationId, cascadeModel.CorrelationId);
                                                var deployProjection = Builders<DeployModelsDto>.Projection.Exclude(CONSTANTS.Id);

                                                var deployResult = collection.Find(filter3).Project<DeployModelsDto>(deployProjection).FirstOrDefault();
                                                if (deployResult != null)
                                                {
                                                    if (deployResult.CascadeIdList.Length > 0)
                                                    {
                                                        string[] CascadeIdList = deployResult.CascadeIdList.Where(val => val != cascadeId).ToArray();
                                                        if (isCusomtModel)
                                                        {
                                                            string[] arr = new string[] { };
                                                            var update = Builders<DeployModelsDto>.Update.Set(CONSTANTS.CustomCascadeId, val)
                                                          .Set(CONSTANTS.IsIncludedInCascade, false)
                                                          .Set(CONSTANTS.IsIncludedinCustomCascade, false)
                                                          .Set(CONSTANTS.CascadeIdList, CascadeIdList)
                                                          .Set(CONSTANTS.DataCurationName, val);
                                                            var updateResult2 = collection.UpdateOne(filter3, update);
                                                        }
                                                        else
                                                        {
                                                            string[] arr = new string[] { };
                                                            var update = Builders<DeployModelsDto>.Update.Set(CONSTANTS.CustomCascadeId, val)
                                                          .Set(CONSTANTS.IsIncludedInCascade, false)
                                                          .Set(CONSTANTS.IsIncludedinCustomCascade, false)
                                                          .Set(CONSTANTS.CascadeIdList, arr);
                                                            var updateResult3 = collection.UpdateOne(filter3, update);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        if (isCusomtModel)
                                                        {
                                                            string[] arr = new string[] { };
                                                            var update = Builders<DeployModelsDto>.Update.Set(CONSTANTS.CustomCascadeId, val)
                                                          .Set(CONSTANTS.IsIncludedInCascade, false)
                                                          .Set(CONSTANTS.IsIncludedinCustomCascade, false)
                                                          .Set(CONSTANTS.CascadeIdList, arr)
                                                          .Set(CONSTANTS.DataCurationName, val);
                                                            var updateResult4 = collection.UpdateOne(filter3, update);
                                                        }
                                                        else
                                                        {
                                                            string[] arr = new string[] { };
                                                            var update = Builders<DeployModelsDto>.Update.Set(CONSTANTS.CustomCascadeId, val)
                                                          .Set(CONSTANTS.IsIncludedInCascade, false)
                                                          .Set(CONSTANTS.IsIncludedinCustomCascade, false)
                                                          .Set(CONSTANTS.CascadeIdList, arr);
                                                            var updateResult5 = collection.UpdateOne(filter3, update);
                                                        }
                                                    }
                                                }
                                                //END

                                                //updating PSBusinessProblem
                                                string targetColumn = previousModel.ModelName + "_" + previousModel.TargetColumn;
                                                string probaItem = previousModel.ModelName + "_" + "Proba1";
                                                string[] inputColumns = psResult[0].InputColumns.Where(e => e != targetColumn).ToArray();
                                                string[] inputColumns2 = inputColumns.Where(e => e != probaItem).ToArray();

                                                var psUpdate = Builders<BusinessProblem>.Update.Set(CONSTANTS.InputColumns, inputColumns2);
                                                var updateResult = businesscolection.UpdateOne(businessFilter, psUpdate);
                                                //End
                                            }
                                            i++;
                                        }
                                        else
                                        {
                                            string val = null;
                                            var collection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
                                            var filter3 = Builders<DeployModelsDto>.Filter.Eq(CONSTANTS.CorrelationId, model1Corid);
                                            var deployProjection = Builders<DeployModelsDto>.Projection.Exclude(CONSTANTS.Id);

                                            var deployResult = collection.Find(filter3).Project<DeployModelsDto>(deployProjection).FirstOrDefault();
                                            if (deployResult != null)
                                            {
                                                if (deployResult.CascadeIdList.Length > 0)
                                                {
                                                    string[] CascadeIdList = deployResult.CascadeIdList.Where(val => val != cascadeId).ToArray();
                                                    if (isCusomtModel)
                                                    {
                                                        string[] arr = new string[] { };
                                                        var update = Builders<DeployModelsDto>.Update.Set(CONSTANTS.CustomCascadeId, val)
                                                      .Set(CONSTANTS.IsIncludedInCascade, false)
                                                      .Set(CONSTANTS.IsIncludedinCustomCascade, false)
                                                      .Set(CONSTANTS.CascadeIdList, CascadeIdList)
                                                      .Set(CONSTANTS.DataCurationName, val);
                                                        var updateResult = collection.UpdateOne(filter3, update);
                                                    }
                                                    else
                                                    {
                                                        string[] arr = new string[] { };
                                                        var update = Builders<DeployModelsDto>.Update.Set(CONSTANTS.CustomCascadeId, val)
                                                      .Set(CONSTANTS.IsIncludedInCascade, false)
                                                      .Set(CONSTANTS.IsIncludedinCustomCascade, false)
                                                      .Set(CONSTANTS.CascadeIdList, arr);
                                                        var updateResult = collection.UpdateOne(filter3, update);
                                                    }
                                                }
                                                else
                                                {
                                                    if (isCusomtModel)
                                                    {
                                                        string[] arr = new string[] { };
                                                        var update = Builders<DeployModelsDto>.Update.Set(CONSTANTS.CustomCascadeId, val)
                                                      .Set(CONSTANTS.IsIncludedInCascade, false)
                                                      .Set(CONSTANTS.IsIncludedinCustomCascade, false)
                                                      .Set(CONSTANTS.CascadeIdList, arr)
                                                      .Set(CONSTANTS.DataCurationName, val);
                                                        var updateResult = collection.UpdateOne(filter3, update);
                                                    }
                                                    else
                                                    {
                                                        string[] arr = new string[] { };
                                                        var update = Builders<DeployModelsDto>.Update.Set(CONSTANTS.CustomCascadeId, val)
                                                      .Set(CONSTANTS.IsIncludedInCascade, false)
                                                      .Set(CONSTANTS.IsIncludedinCustomCascade, false)
                                                      .Set(CONSTANTS.CascadeIdList, arr);
                                                        var updateResult = collection.UpdateOne(filter3, update);
                                                    }
                                                }


                                            }
                                            i++;
                                        }
                                    }
                                    else
                                    {
                                        CascadeModelsCollection cascadeModel = JsonConvert.DeserializeObject<CascadeModelsCollection>(model.Value.ToString());
                                        string val = null;
                                        var collection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
                                        var filter3 = Builders<DeployModelsDto>.Filter.Eq(CONSTANTS.CorrelationId, model1Corid);
                                        var deployProjection = Builders<DeployModelsDto>.Projection.Exclude(CONSTANTS.Id);

                                        var deployResult = collection.Find(filter3).Project<DeployModelsDto>(deployProjection).FirstOrDefault();
                                        if (deployResult != null)
                                        {
                                            if (deployResult.CascadeIdList.Length > 0)
                                            {
                                                string[] CascadeIdList = deployResult.CascadeIdList.Where(val => val != cascadeId).ToArray();
                                                if (isCusomtModel)
                                                {
                                                    var update = Builders<DeployModelsDto>.Update.Set(CONSTANTS.CustomCascadeId, val)
                                                  .Set(CONSTANTS.IsIncludedInCascade, false)
                                                  .Set(CONSTANTS.IsIncludedinCustomCascade, false)
                                                  .Set(CONSTANTS.CascadeIdList, CascadeIdList)
                                                  .Set(CONSTANTS.DataCurationName, val);
                                                    var updateResult = collection.UpdateOne(filter3, update);
                                                }
                                                else
                                                {
                                                    if (deployResult.CascadeIdList != null)
                                                    {
                                                        string[] arr = new string[] { };
                                                        if (deployResult.CascadeIdList.Count() > 0)
                                                        {
                                                            if (deployResult.CascadeIdList.Length < 2)
                                                            {
                                                                var update = Builders<DeployModelsDto>.Update.Set(CONSTANTS.IsIncludedInCascade, false).Set("CascadeIdList", arr);
                                                                var updateResult = collection.UpdateOne(filter3, update);
                                                            }
                                                            else
                                                            {
                                                                deployResult.CascadeIdList = deployResult.CascadeIdList.Where(val => val != cascadeId).ToArray();
                                                                var update3 = Builders<DeployModelsDto>.Update.Set("IsIncludedInCascade", true).Set("CascadeIdList", deployResult.CascadeIdList);
                                                                var updateResult3 = collection.UpdateOne(filter3, update3);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            var update = Builders<DeployModelsDto>.Update.Set(CONSTANTS.IsIncludedInCascade, false).Set("CascadeIdList", arr);
                                                            var updateResult = collection.UpdateOne(filter3, update);
                                                        }
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                string[] arr2 = new string[] { };
                                                if (isCusomtModel)
                                                {
                                                    var update = Builders<DeployModelsDto>.Update.Set(CONSTANTS.CustomCascadeId, val)
                                                  .Set(CONSTANTS.IsIncludedInCascade, false)
                                                  .Set(CONSTANTS.IsIncludedinCustomCascade, false)
                                                  .Set(CONSTANTS.CascadeIdList, arr2)
                                                  .Set(CONSTANTS.DataCurationName, val);
                                                    var updateResult = collection.UpdateOne(filter3, update);
                                                }
                                                else
                                                {
                                                    if (deployResult.CascadeIdList != null)
                                                    {
                                                        string[] arr = new string[] { };
                                                        if (deployResult.CascadeIdList.Count() > 0)
                                                        {
                                                            if (deployResult.CascadeIdList.Length < 2)
                                                            {
                                                                var update = Builders<DeployModelsDto>.Update.Set(CONSTANTS.IsIncludedInCascade, false).Set("CascadeIdList", arr);
                                                                var updateResult = collection.UpdateOne(filter3, update);
                                                            }
                                                            else
                                                            {
                                                                deployResult.CascadeIdList = deployResult.CascadeIdList.Where(val => val != cascadeId).ToArray();
                                                                var update3 = Builders<DeployModelsDto>.Update.Set("IsIncludedInCascade", true).Set("CascadeIdList", deployResult.CascadeIdList);
                                                                var updateResult3 = collection.UpdateOne(filter3, update3);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            var update = Builders<DeployModelsDto>.Update.Set(CONSTANTS.IsIncludedInCascade, false).Set("CascadeIdList", arr);
                                                            var updateResult = collection.UpdateOne(filter3, update);
                                                        }
                                                    }
                                                }
                                            }

                                        }

                                    }
                                }
                            }
                        }
                        collection_CascadeModel.DeleteMany(cascadeFilter);
                        flushStatus = flushStatus + CONSTANTS.SSAICascadedModels;
                    }
                }

                var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, corId);
                string message = string.Format(CONSTANTS.FlushCorrelationMessage, corId);
                flushStatus = FlushByCollections(filter, message);

            }
            return flushStatus;
        }
        public string DeleteClientDCIds(string[] ClientId, string[] DCId)
        {
            string flushStatus = string.Empty;

            foreach (var clientId in ClientId)
            {
                CommonUtility.ValidateInputFormData(Convert.ToString(clientId), CONSTANTS.ClientUID, true);
                foreach (var dcId in DCId)
                {
                    CommonUtility.ValidateInputFormData(Convert.ToString(dcId), CONSTANTS.DeliveryConstructUID, true);
                    var filter1 = Builders<BsonDocument>.Filter.Eq(CONSTANTS.ClientUID, clientId) | Builders<BsonDocument>.Filter.Eq(CONSTANTS.ClientUId, clientId) | Builders<BsonDocument>.Filter.Eq(CONSTANTS.ClientId, clientId) | Builders<BsonDocument>.Filter.Eq(CONSTANTS.ClientID, clientId);
                    var filter2 = Builders<BsonDocument>.Filter.Eq(CONSTANTS.DeliveryConstructUID, dcId) | Builders<BsonDocument>.Filter.Eq(CONSTANTS.DeliveryConstructId, dcId) | Builders<BsonDocument>.Filter.Eq(CONSTANTS.DeliveryConstructUId, dcId) | Builders<BsonDocument>.Filter.Eq(CONSTANTS.DeliveryconstructId, dcId) | Builders<BsonDocument>.Filter.Eq(CONSTANTS.DCUID, dcId) | Builders<BsonDocument>.Filter.Eq(CONSTANTS.DCID, dcId);
                    var filter = filter1 & filter2;
                    string message = string.Format(CONSTANTS.FlushIdMessage, clientId, dcId);
                    var collection_SSAI_DeployedModels = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_DeployedModels);
                    if (collection_SSAI_DeployedModels.Find(filter).ToList().Count > 0)
                    {
                        collection_SSAI_DeployedModels.DeleteMany(filter);
                        flushStatus = flushStatus + CONSTANTS.SSAI_DeployedModels + message;
                    }
                    var collection_PS_BusinessProblem = _database.GetCollection<BsonDocument>(CONSTANTS.PS_BusinessProblem);
                    if (collection_PS_BusinessProblem.Find(filter).ToList().Count > 0)
                    {
                        collection_PS_BusinessProblem.DeleteMany(filter);
                        flushStatus = flushStatus + CONSTANTS.PS_BusinessProblem + message;
                    }
                    var collection_SSAI_IngrainRequests = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
                    if (collection_SSAI_IngrainRequests.Find(filter).ToList().Count > 0)
                    {
                        collection_SSAI_IngrainRequests.DeleteMany(filter);
                        flushStatus = flushStatus + CONSTANTS.SSAIIngrainRequests + message;
                    }
                    var collection_AuditTrailLog = _database.GetCollection<BsonDocument>(CONSTANTS.AuditTrailLog);
                    if (collection_AuditTrailLog.Find(filter).ToList().Count > 0)
                    {
                        collection_AuditTrailLog.DeleteMany(filter);
                        flushStatus = flushStatus + CONSTANTS.AuditTrailLog + message;
                    }
                    var collection_AIServiceRequestStatus = _database.GetCollection<BsonDocument>(CONSTANTS.AIServiceRequestStatus);
                    if (collection_AIServiceRequestStatus.Find(filter).ToList().Count > 0)
                    {
                        collection_AIServiceRequestStatus.DeleteMany(filter);
                        flushStatus = flushStatus + CONSTANTS.AIServiceRequestStatus + message;
                    }
                    var collection_Clustering_DataPreprocessing = _database.GetCollection<BsonDocument>(CONSTANTS.Clustering_DataPreprocessing);
                    if (collection_Clustering_DataPreprocessing.Find(filter).ToList().Count > 0)
                    {
                        collection_Clustering_DataPreprocessing.DeleteMany(filter);
                        flushStatus = flushStatus + CONSTANTS.Clustering_DataPreprocessing + message;
                    }
                    var collection_Clustering_IngestData = _database.GetCollection<BsonDocument>(CONSTANTS.Clustering_IngestData);
                    if (collection_Clustering_IngestData.Find(filter).ToList().Count > 0)
                    {
                        collection_Clustering_IngestData.DeleteMany(filter);
                        flushStatus = flushStatus + CONSTANTS.Clustering_IngestData + message;
                    }
                    var collection_AIServiceRecordsDetails = _database.GetCollection<BsonDocument>(CONSTANTS.AIServiceRecordsDetails);
                    if (collection_AIServiceRecordsDetails.Find(filter).ToList().Count > 0)
                    {
                        collection_AIServiceRecordsDetails.DeleteMany(filter);
                        flushStatus = flushStatus + CONSTANTS.AIServiceRecordsDetails + message;
                    }
                    var collection_AppNotificationLog = _database.GetCollection<BsonDocument>(CONSTANTS.AppNotificationLog);
                    if (collection_AppNotificationLog.Find(filter).ToList().Count > 0)
                    {
                        collection_AppNotificationLog.DeleteMany(filter);
                        flushStatus = flushStatus + CONSTANTS.AppNotificationLog + message;
                    }
                    var collection_Clustering_Eval = _database.GetCollection<BsonDocument>(CONSTANTS.Clustering_Eval);
                    if (collection_Clustering_Eval.Find(filter).ToList().Count > 0)
                    {
                        collection_Clustering_Eval.DeleteMany(filter);
                        flushStatus = flushStatus + CONSTANTS.Clustering_Eval + message;
                    }
                    var collection_Clustering_EvalTestResults = _database.GetCollection<BsonDocument>(CONSTANTS.Clustering_EvalTestResults);
                    if (collection_Clustering_EvalTestResults.Find(filter).ToList().Count > 0)
                    {
                        collection_Clustering_EvalTestResults.DeleteMany(filter);
                        flushStatus = flushStatus + CONSTANTS.Clustering_EvalTestResults + message;
                    }
                    var collection_PredictionSchedulerLog = _database.GetCollection<BsonDocument>(CONSTANTS.PredictionSchedulerLog);
                    if (collection_PredictionSchedulerLog.Find(filter).ToList().Count > 0)
                    {
                        collection_PredictionSchedulerLog.DeleteMany(filter);
                        flushStatus = flushStatus + CONSTANTS.PredictionSchedulerLog + message;
                    }
                    var collection_Clustering_Visualization = _database.GetCollection<BsonDocument>(CONSTANTS.Clustering_Visualization);
                    var Projection = Builders<BsonDocument>.Projection.Exclude("Visualization_Response").Exclude(CONSTANTS.Id);
                    var Clustering_VisualizationResult = collection_Clustering_Visualization.Find(filter).Project<BsonDocument>(Projection).ToList();
                    if (Clustering_VisualizationResult.Count > 0)
                    {
                        collection_Clustering_Visualization.DeleteMany(filter);
                        flushStatus = flushStatus + CONSTANTS.Clustering_Visualization + message;
                    }
                    var collection_Clustering_TrainedModels = _database.GetCollection<BsonDocument>(CONSTANTS.Clustering_TrainedModels);
                    if (collection_Clustering_TrainedModels.Find(filter).ToList().Count > 0)
                    {
                        collection_Clustering_TrainedModels.DeleteMany(filter);
                        flushStatus = flushStatus + CONSTANTS.Clustering_TrainedModels + message;
                    }

                }
            }

            return flushStatus;
        }
        public string DeleteDateClientDCIds(string date, string[] ClientUId, string[] DCId)
        {
            string flushStatus = string.Empty;

            foreach (var clientId in ClientUId)
            {
                CommonUtility.ValidateInputFormData(Convert.ToString(clientId), CONSTANTS.ClientUId, true);
                foreach (var dcId in DCId)
                {
                    CommonUtility.ValidateInputFormData(Convert.ToString(dcId), CONSTANTS.DeliveryConstructUID, true);
                    var filter1 = Builders<BsonDocument>.Filter.Eq(CONSTANTS.ClientUID, clientId) | Builders<BsonDocument>.Filter.Eq(CONSTANTS.ClientUId, clientId) | Builders<BsonDocument>.Filter.Eq(CONSTANTS.ClientId, clientId) | Builders<BsonDocument>.Filter.Eq(CONSTANTS.ClientID, clientId);
                    var filter2 = Builders<BsonDocument>.Filter.Eq(CONSTANTS.DeliveryConstructUID, dcId) | Builders<BsonDocument>.Filter.Eq(CONSTANTS.DeliveryConstructId, dcId) | Builders<BsonDocument>.Filter.Eq(CONSTANTS.DeliveryConstructUId, dcId) | Builders<BsonDocument>.Filter.Eq(CONSTANTS.DeliveryconstructId, dcId) | Builders<BsonDocument>.Filter.Eq(CONSTANTS.DCUID, dcId) | Builders<BsonDocument>.Filter.Eq(CONSTANTS.DCID, dcId);
                    var filter3 = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CreatedOn, date) | Builders<BsonDocument>.Filter.Eq(CONSTANTS.CreatedDate, date);
                    var filter = filter1 & filter2 & filter3;
                    string message = string.Format(CONSTANTS.FlushDateAndIdMessage, date, clientId, dcId);

                    var collection_SSAI_DeployedModels = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_DeployedModels);
                    if (collection_SSAI_DeployedModels.Find(filter).ToList().Count > 0)
                    {
                        collection_SSAI_DeployedModels.DeleteMany(filter);
                        flushStatus = flushStatus + CONSTANTS.SSAI_DeployedModels + message;
                    }
                    var collection_PS_BusinessProblem = _database.GetCollection<BsonDocument>(CONSTANTS.PS_BusinessProblem);
                    if (collection_PS_BusinessProblem.Find(filter).ToList().Count > 0)
                    {
                        collection_PS_BusinessProblem.DeleteMany(filter);
                        flushStatus = flushStatus + CONSTANTS.PS_BusinessProblem + message;
                    }
                    var collection_SSAI_IngrainRequests = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
                    if (collection_SSAI_IngrainRequests.Find(filter).ToList().Count > 0)
                    {
                        collection_SSAI_IngrainRequests.DeleteMany(filter);
                        flushStatus = flushStatus + CONSTANTS.SSAIIngrainRequests + message;
                    }
                    var collection_AIServiceRequestStatus = _database.GetCollection<BsonDocument>(CONSTANTS.AIServiceRequestStatus);
                    if (collection_AIServiceRequestStatus.Find(filter).ToList().Count > 0)
                    {
                        collection_AIServiceRequestStatus.DeleteMany(filter);
                        flushStatus = flushStatus + CONSTANTS.AIServiceRequestStatus + message;
                    }
                    var collection_AIServiceRecordsDetails = _database.GetCollection<BsonDocument>(CONSTANTS.AIServiceRecordsDetails);
                    if (collection_AIServiceRecordsDetails.Find(filter).ToList().Count > 0)
                    {
                        collection_AIServiceRecordsDetails.DeleteMany(filter);
                        flushStatus = flushStatus + CONSTANTS.AIServiceRecordsDetails + message;
                    }
                    var collection_Clustering_Eval = _database.GetCollection<BsonDocument>(CONSTANTS.Clustering_Eval);
                    if (collection_Clustering_Eval.Find(filter).ToList().Count > 0)
                    {
                        collection_Clustering_Eval.DeleteMany(filter);
                        flushStatus = flushStatus + CONSTANTS.Clustering_Eval + message;
                    }
                    var collection_Clustering_IngestData = _database.GetCollection<BsonDocument>(CONSTANTS.Clustering_IngestData);
                    if (collection_Clustering_IngestData.Find(filter).ToList().Count > 0)
                    {
                        collection_Clustering_IngestData.DeleteMany(filter);
                        flushStatus = flushStatus + CONSTANTS.Clustering_IngestData + message;
                    }
                    var collection_Clustering_EvalTestResults = _database.GetCollection<BsonDocument>(CONSTANTS.Clustering_EvalTestResults);
                    if (collection_Clustering_EvalTestResults.Find(filter).ToList().Count > 0)
                    {
                        collection_Clustering_EvalTestResults.DeleteMany(filter);
                        flushStatus = flushStatus + CONSTANTS.Clustering_EvalTestResults + message;
                    }
                    var collection_AuditTrailLog = _database.GetCollection<BsonDocument>(CONSTANTS.AuditTrailLog);
                    if (collection_AuditTrailLog.Find(filter).ToList().Count > 0)
                    {
                        collection_AuditTrailLog.DeleteMany(filter);
                        flushStatus = flushStatus + CONSTANTS.AuditTrailLog + message;
                    }
                    var collection_AppNotificationLog = _database.GetCollection<BsonDocument>(CONSTANTS.AppNotificationLog);
                    if (collection_AppNotificationLog.Find(filter).ToList().Count > 0)
                    {
                        collection_AppNotificationLog.DeleteMany(filter);
                        flushStatus = flushStatus + CONSTANTS.AppNotificationLog + message;
                    }
                    var collection_PredictionSchedulerLog = _database.GetCollection<BsonDocument>(CONSTANTS.PredictionSchedulerLog);
                    if (collection_PredictionSchedulerLog.Find(filter).ToList().Count > 0)
                    {
                        collection_PredictionSchedulerLog.DeleteMany(filter);
                        flushStatus = flushStatus + CONSTANTS.PredictionSchedulerLog + message;
                    }

                }
            }

            return flushStatus;
        }


        public string userRole(string correlationId, string userid, string ServiceName = "")
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(FlushModelService), nameof(userRole), "--USERID--" + userid, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            string user = string.Empty;
            if (!string.IsNullOrEmpty(userid))
            {
                string[] userIds = userid.Split('@');
                if (userIds.Length == 1)
                    user = userid + "@accenture.com";
                else
                    user = userid;
            }
            string encryptedUserId = userid;
            bool DBEncryptionRequired = CommonUtility.EncryptDB(correlationId, appSettings,ServiceName);
            if (DBEncryptionRequired)
            {
                if (!string.IsNullOrEmpty(Convert.ToString(userid)))
                    encryptedUserId = appSettings.Value.IsAESKeyVault ? CryptographyUtility.Encrypt(Convert.ToString(userid)) : AesProvider.Encrypt(Convert.ToString(userid), appSettings.Value.aesKey, appSettings.Value.aesVector);
            }
            IMongoCollection<BsonDocument> collection;
            if (ServiceName == "Anomaly")
                collection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
            else
                collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
           // var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
            var filterProjection = Builders<BsonDocument>.Projection.Exclude("_id");
            var filterbuilder = Builders<BsonDocument>.Filter;
            var filter = filterbuilder.Eq(CONSTANTS.CorrelationId, correlationId) & (filterbuilder.Eq(CONSTANTS.CreatedByUser, userid) | filterbuilder.Eq(CONSTANTS.CreatedByUser, encryptedUserId));
            var ingrainrequestCollection = collection.Find(filter).Project<BsonDocument>(filterProjection).FirstOrDefault();
            if (string.IsNullOrEmpty(userid))
            {
                return "Valid";
            }
            if (ingrainrequestCollection != null)
            {
                return "Valid";
            }
            else
            {
                return "InValid";
            }
        }
    }
}