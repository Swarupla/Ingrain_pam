using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text;
using WINSERVICEMODELS = Accenture.MyWizard.Ingrain.WindowService.Models;
using DATAACCESS = Accenture.MyWizard.Ingrain.WindowService;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Net;
using RestSharp;
using Accenture.MyWizard.Ingrain.WindowService.Interfaces;
using System.Threading.Tasks;
using System.Linq;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
using Accenture.MyWizard.Ingrain.WindowService.Models;
using MongoDB.Bson.Serialization;
using MongoDB.Bson;
using Accenture.MyWizard.Ingrain.BusinessDomain.Common;
using System.IO;

namespace Accenture.MyWizard.Ingrain.WindowService.Services
{
    public class DeployModelService : IDeployModelService
    {
        private readonly WINSERVICEMODELS.AppSettings appSettings = null;
        private IMongoDatabase _database;
        private DATAACCESS.DatabaseProvider databaseProvider;
        private readonly int _archiveDays;
        //Added UseCaseIDs to be excluded for Archival and purging
        List<string> ExcludedUseCaselst = new List<string> {
        "68bf25f3-6df8-4e14-98fa-34918d2eeff1", "5cab6ea1-8af4-4f74-8359-e053629d2b98",
        "64a6c5be-0ecb-474e-b970-06c960d6f7b7","f0320924-2ee3-4398-ad7c-8bc172abd78d",
        "6b0d8eb3-0918-4818-9655-6ca81a4ebf30", "877f712c-7cdc-435a-acc8-8fea3d26cc18",
        "6d254151-a081-4943-87d4-476ebd6f64d4","8d3772f2-f19b-4403-840d-2fb230ac630f",
        "668bb66a-86c6-46e6-9f98-c0bc9b3e4eb2","6761146a-0eef-4b39-8dd8-33c786e4fb86",
        "169881ad-dc85-4bf8-bc67-7b1212836a97","be0c67a1-4320-461e-9aff-06a545824a32",
        "fa52b2ab-6d7f-4a97-b834-78af04791ddf","5b7667e6-b48a-4bc4-8082-587f407df5b5",
        "7741301f-678e-478e-b76b-0b9d99014497", "68bf25f3-6df8-4e14-98fa-34918d2eeff1",
        "f0320924-2ee3-4398-ad7c-8bc172abd78d","672fb32c-05ca-4871-a2a8-d3b2adc5bf1e",
        "471ae90c-55ef-49b4-8e5b-8938dde32fa9", "a71ff6fd-4711-4b42-b41c-39ef95dedb75",
        "5751b534-ed7a-4af7-92d9-a6f8e815a39f", "77776d46-0e3a-4b0f-8961-f3bf68f196ec",
        "b80c113d-8edd-4bc7-8333-eefb399b46e2", "b54f4da6-ebcb-4b03-8189-8f6bad701ff3",
        "14f72caf-59bd-4d8c-b0f6-06d5d4d1fd7b", "f2e5af7b-e504-4d21-9bb7-f50a1c8f90d6"
        };


        public DeployModelService()
        {
            appSettings = AppSettingsJson.GetAppSettings().GetSection("AppSettings").Get<WINSERVICEMODELS.AppSettings>();
            databaseProvider = new DATAACCESS.DatabaseProvider();
            var dataBaseName = MongoUrl.Create(appSettings.connectionString).DatabaseName;
            MongoClient mongoClient = databaseProvider.GetDatabaseConnection();
            _database = mongoClient.GetDatabase(dataBaseName);
            _archiveDays = Convert.ToInt32(appSettings.archivalDays);                   
        }     

        public void ArchiveRecords(bool ManualTrigger, List<string> corrIds, int archive_days)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DeployModelService), nameof(ArchiveRecords), "Archival Process - Started ", string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                var filterBuilder = Builders<ArchiveDeployModelsDto>.Filter;
                FilterDefinition<ArchiveDeployModelsDto> Corfilter = null;
                if (ManualTrigger)
                {
                    Corfilter = filterBuilder.In("CorrelationId", corrIds);
                }
                else
                {
                    Corfilter = filterBuilder.Empty;
                }

                var collection_SSAIDeployedModels = _database.GetCollection<ArchiveDeployModelsDto>(CONSTANTS.SSAIDeployedModels);
                var project = Builders<ArchiveDeployModelsDto>.Projection.Exclude(CONSTANTS.Id);
                var SSAIDeployedModels_result = collection_SSAIDeployedModels.Find(Corfilter).Project<ArchiveDeployModelsDto>(project).ToList();
                if (SSAIDeployedModels_result.Count > 0)
                {
                    foreach (var doc in SSAIDeployedModels_result)
                    {
                        if (!ExcludedUseCaselst.Contains(doc.CorrelationId))
                        {
                            if ((Convert.ToInt32(doc.ArchivalDays) == 0 ? Convert.ToDateTime(doc.ModifiedOn) < DateTime.Now.AddDays(archive_days) : Convert.ToDateTime(doc.ModifiedOn) < DateTime.Now.AddDays(-Convert.ToInt32(doc.ArchivalDays))) 
                                || ( ManualTrigger && Convert.ToDateTime(doc.ModifiedOn) < DateTime.Now.AddDays(archive_days)))
                            {
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(DeployModelService), nameof(ArchiveRecords), "CorrelationID of Archiving Model : " + doc.CorrelationId, string.Empty, string.Empty, string.Empty, string.Empty);
                                var archiveDeployModel = new ArchiveModels
                                {
                                    CollectionName = CONSTANTS.SSAIDeployedModels,
                                    CollectionValue = doc,
                                    ArchiveDate = DateTime.Now.ToString(CONSTANTS.DateHoursFormat)
                                };
                                var deployarchivecollection = _database.GetCollection<ArchiveModels>(CONSTANTS.SSAI_DeployedModels_archive);
                                deployarchivecollection.InsertOne(archiveDeployModel);

                                var deployfilter = Builders<ArchiveDeployModelsDto>.Filter.Eq(CONSTANTS.CorrelationId, doc.CorrelationId);
                                collection_SSAIDeployedModels.DeleteOne(deployfilter);

                                // Collections that can be removed (not retained for archival)
                                var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, doc.CorrelationId);
                                var collection_SSAI_PublishModel = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_PublishModel);
                                var collection_SSAI_IngrainRequests = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
                                var collection_PrescriptiveAnalyticsResults = _database.GetCollection<BsonDocument>(CONSTANTS.PrescriptiveAnalyticsResults);
                                var collection_WF_IngestedData = _database.GetCollection<BsonDocument>(CONSTANTS.WF_IngestedData);
                                var collection_WF_TestResults = _database.GetCollection<BsonDocument>(CONSTANTS.WF_TestResults);
                                var collection_WhatIfAnalysis = _database.GetCollection<BsonDocument>(CONSTANTS.WhatIfAnalysis);

                                if (collection_SSAI_PublishModel.Find(filter).ToList().Count > 0)
                                {
                                    collection_SSAI_PublishModel.DeleteMany(filter);
                                }
                                if (collection_SSAI_IngrainRequests.Find(filter).ToList().Count > 0)
                                {
                                    collection_SSAI_IngrainRequests.DeleteMany(filter);
                                }
                                if (collection_PrescriptiveAnalyticsResults.Find(filter).ToList().Count > 0)
                                {
                                    collection_PrescriptiveAnalyticsResults.DeleteMany(filter);
                                }
                                if (collection_WF_IngestedData.Find(filter).ToList().Count > 0)
                                {
                                    collection_WF_IngestedData.DeleteMany(filter);
                                }
                                if (collection_WF_TestResults.Find(filter).ToList().Count > 0)
                                {
                                    collection_WF_TestResults.DeleteMany(filter);
                                }
                                if (collection_WhatIfAnalysis.Find(filter).ToList().Count > 0)
                                {
                                    collection_WhatIfAnalysis.DeleteMany(filter);
                                }

                                //collections to be retained
                                var collection_DE_DataCleanup = _database.GetCollection<BsonDocument>(CONSTANTS.DE_DataCleanup);
                                var projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
                                var DE_DataCleanup_result = collection_DE_DataCleanup.Find(filter).Project<BsonDocument>(projection).ToList();
                                if (DE_DataCleanup_result.Count > 0)
                                {
                                    foreach (var result in DE_DataCleanup_result)
                                    {
                                        var archiveMod = new ArchiveModel
                                        {
                                            CollectionName = CONSTANTS.DE_DataCleanup,
                                            CollectionValue = result,
                                            ArchiveDate = DateTime.Now.ToString(CONSTANTS.DateHoursFormat)
                                        };
                                        var archivecollection = _database.GetCollection<ArchiveModel>(CONSTANTS.DE_DataCleanup_archive);
                                        archivecollection.InsertOne(archiveMod);

                                        var datacleanupfilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, doc.CorrelationId);
                                        collection_DE_DataCleanup.DeleteMany(datacleanupfilter);
                                    }
                                }
                                var collection_DE_DataProcessing = _database.GetCollection<BsonDocument>(CONSTANTS.DE_DataProcessing);
                                var DE_DataProcessing_result = collection_DE_DataProcessing.Find(filter).Project<BsonDocument>(projection).ToList();
                                if (DE_DataProcessing_result.Count >0)
                                {
                                    foreach (var result in DE_DataProcessing_result)
                                    {
                                        var archiveMod = new ArchiveModel
                                        {
                                            CollectionName = CONSTANTS.DE_DataProcessing,
                                            CollectionValue = result,
                                            ArchiveDate = DateTime.Now.ToString(CONSTANTS.DateHoursFormat)
                                        };
                                        var archivecollection = _database.GetCollection<ArchiveModel>(CONSTANTS.DE_DataProcessing_archive);
                                        archivecollection.InsertOne(archiveMod);

                                        var myfilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, doc.CorrelationId);
                                        collection_DE_DataProcessing.DeleteOne(myfilter);
                                    }
                                }
                                var collection_DE_NewFeatureData = _database.GetCollection<BsonDocument>(CONSTANTS.DE_NewFeatureData);
                                var collection_DE_NewFeatureData_result = collection_DE_NewFeatureData.Find(filter).Project<BsonDocument>(projection).ToList();
                                if (collection_DE_NewFeatureData_result.Count>0)
                                {
                                    foreach(var result in collection_DE_NewFeatureData_result)
                                    {
                                        var archiveMod = new ArchiveModel
                                        {
                                            CollectionName = CONSTANTS.DE_NewFeatureData,
                                            CollectionValue = result,
                                            ArchiveDate = DateTime.Now.ToString(CONSTANTS.DateHoursFormat)
                                        };
                                        var archivecollection = _database.GetCollection<ArchiveModel>(CONSTANTS.DE_NewFeatureData_archive);
                                        archivecollection.InsertOne(archiveMod);

                                        var myfilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, doc.CorrelationId);
                                        collection_DE_NewFeatureData.DeleteOne(myfilter);
                                    }
                                }

                                var collection_DE_PreProcessedData = _database.GetCollection<BsonDocument>(CONSTANTS.DE_PreProcessedData);
                                var collection_DE_PreProcessedData_result = collection_DE_PreProcessedData.Find(filter).Project<BsonDocument>(projection).ToList();
                                if (collection_DE_PreProcessedData_result.Count > 0)
                                {
                                    foreach (var result in collection_DE_PreProcessedData_result)
                                    {
                                        var archiveMod = new ArchiveModel
                                        {
                                            CollectionName = CONSTANTS.DE_PreProcessedData,
                                            CollectionValue = result,
                                            ArchiveDate = DateTime.Now.ToString(CONSTANTS.DateHoursFormat)
                                        };
                                        var archivecollection = _database.GetCollection<ArchiveModel>(CONSTANTS.DE_PreProcessedData_archive);
                                        archivecollection.InsertOne(archiveMod);

                                        var myfilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, doc.CorrelationId);
                                        collection_DE_PreProcessedData.DeleteOne(myfilter);
                                    }
                                }

                                var collection_SSAI_savedModels = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_savedModels);
                                var collection_SSAI_savedModels_result = collection_SSAI_savedModels.Find(filter).Project<BsonDocument>(projection).ToList();
                                if (collection_SSAI_savedModels_result.Count > 0)
                                {
                                    foreach (var result in collection_SSAI_savedModels_result)
                                    {
                                        var archiveMod = new ArchiveModel
                                        {
                                            CollectionName = CONSTANTS.SSAI_savedModels,
                                            CollectionValue = result,
                                            ArchiveDate = DateTime.Now.ToString(CONSTANTS.DateHoursFormat)
                                        };
                                        var archivecollection = _database.GetCollection<ArchiveModel>(CONSTANTS.SSAI_savedModels_archive);
                                        archivecollection.InsertOne(archiveMod);

                                        var myfilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, doc.CorrelationId);
                                        collection_SSAI_savedModels.DeleteOne(myfilter);
                                    }
                                }
                                var collection_PS_BusinessProblem = _database.GetCollection<BsonDocument>(CONSTANTS.PS_BusinessProblem);
                                var collection_PS_BusinessProblem_result = collection_PS_BusinessProblem.Find(filter).Project<BsonDocument>(projection).ToList();
                                if (collection_PS_BusinessProblem_result.Count > 0)
                                {
                                    foreach (var result in collection_PS_BusinessProblem_result)
                                    {
                                        var archiveMod = new ArchiveModel
                                        {
                                            CollectionName = CONSTANTS.PS_BusinessProblem,
                                            CollectionValue = result,
                                            ArchiveDate = DateTime.Now.ToString(CONSTANTS.DateHoursFormat)
                                        };
                                        var archivecollection = _database.GetCollection<ArchiveModel>(CONSTANTS.PS_BusinessProblem_archive);
                                        archivecollection.InsertOne(archiveMod);

                                        var myfilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, doc.CorrelationId);
                                        collection_PS_BusinessProblem.DeleteOne(myfilter);
                                    }
                                }
                                var collection_PS_IngestedData = _database.GetCollection<BsonDocument>(CONSTANTS.PS_IngestedData);
                                var collection_PS_IngestedData_result = collection_PS_IngestedData.Find(filter).Project<BsonDocument>(projection).ToList();
                                if (collection_PS_IngestedData_result.Count > 0)
                                {
                                    foreach (var result in collection_PS_IngestedData_result)
                                    {
                                        var archiveMod = new ArchiveModel
                                        {
                                            CollectionName = CONSTANTS.PS_IngestedData,
                                            CollectionValue = result,
                                            ArchiveDate = DateTime.Now.ToString(CONSTANTS.DateHoursFormat)
                                        };
                                        var archivecollection = _database.GetCollection<ArchiveModel>(CONSTANTS.PS_IngestedData_archive);
                                        archivecollection.InsertOne(archiveMod);

                                        var myfilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, doc.CorrelationId);
                                        collection_PS_IngestedData.DeleteOne(myfilter);
                                    }
                                }
                                var collection_PS_UsecaseDefinition = _database.GetCollection<BsonDocument>(CONSTANTS.PSUseCaseDefinition);
                                var collection_PS_UsecaseDefinition_result = collection_PS_UsecaseDefinition.Find(filter).Project<BsonDocument>(projection).ToList();
                                if (collection_PS_UsecaseDefinition_result.Count > 0)
                                {
                                    foreach (var result in collection_PS_UsecaseDefinition_result)
                                    {
                                        var archiveMod = new ArchiveModel
                                        {
                                            CollectionName = CONSTANTS.PSUseCaseDefinition,
                                            CollectionValue = result,
                                            ArchiveDate = DateTime.Now.ToString(CONSTANTS.DateHoursFormat)
                                        };
                                        var archivecollection = _database.GetCollection<ArchiveModel>(CONSTANTS.PS_UsecaseDefinition_archive);
                                        archivecollection.InsertOne(archiveMod);

                                        var myfilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, doc.CorrelationId);
                                        collection_PS_UsecaseDefinition.DeleteOne(myfilter);
                                    }
                                }
                                var collection_ME_RecommendedModels = _database.GetCollection<BsonDocument>(CONSTANTS.ME_RecommendedModels);
                                var collection_ME_RecommendedModels_result = collection_ME_RecommendedModels.Find(filter).Project<BsonDocument>(projection).ToList();
                                if (collection_ME_RecommendedModels_result.Count > 0)
                                {
                                    foreach (var result in collection_ME_RecommendedModels_result)
                                    {
                                        var archiveMod = new ArchiveModel
                                        {
                                            CollectionName = CONSTANTS.ME_RecommendedModels,
                                            CollectionValue = result,
                                            ArchiveDate = DateTime.Now.ToString(CONSTANTS.DateHoursFormat)
                                        };
                                        var archivecollection = _database.GetCollection<ArchiveModel>(CONSTANTS.ME_RecommendedModels_archive);
                                        archivecollection.InsertOne(archiveMod);

                                        var myfilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, doc.CorrelationId);
                                        collection_ME_RecommendedModels.DeleteOne(myfilter);
                                    }
                                }
                                var collection_ME_HyperTuneVersion = _database.GetCollection<BsonDocument>(CONSTANTS.ME_HyperTuneVersion);
                                var collection_ME_HyperTuneVersion_result = collection_ME_HyperTuneVersion.Find(filter).Project<BsonDocument>(projection).ToList();
                                if (collection_ME_HyperTuneVersion_result.Count > 0)
                                {
                                    foreach (var result in collection_ME_HyperTuneVersion_result)
                                    {
                                        var archiveMod = new ArchiveModel
                                        {
                                            CollectionName = CONSTANTS.ME_HyperTuneVersion,
                                            CollectionValue = result,
                                            ArchiveDate = DateTime.Now.ToString(CONSTANTS.DateHoursFormat)
                                        };
                                        var archivecollection = _database.GetCollection<ArchiveModel>(CONSTANTS.ME_HyperTuneVersion_archive);
                                        archivecollection.InsertOne(archiveMod);

                                        var myfilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, doc.CorrelationId);
                                        collection_ME_HyperTuneVersion.DeleteOne(myfilter);
                                    }
                                }

                                var collection_DEAddNewFeature = _database.GetCollection<BsonDocument>(CONSTANTS.DEAddNewFeature);
                                var collection_DEAddNewFeature_result = collection_DEAddNewFeature.Find(filter).Project<BsonDocument>(projection).ToList();
                                if (collection_DEAddNewFeature_result.Count > 0)
                                {
                                    foreach (var result in collection_DEAddNewFeature_result)
                                    {
                                        var archiveMod = new ArchiveModel
                                        {
                                            CollectionName = CONSTANTS.DEAddNewFeature,
                                            CollectionValue = result,
                                            ArchiveDate = DateTime.Now.ToString(CONSTANTS.DateHoursFormat)
                                        };
                                        var archivecollection = _database.GetCollection<ArchiveModel>(CONSTANTS.DE_AddNewFeature_archive);
                                        archivecollection.InsertOne(archiveMod);

                                        var myfilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, doc.CorrelationId);
                                        collection_DEAddNewFeature.DeleteOne(myfilter);
                                    }
                                }
                                var collection_DataCleanUP_FilteredData = _database.GetCollection<BsonDocument>(CONSTANTS.DataCleanUP_FilteredData);
                                var collection_DataCleanUP_FilteredData_result = collection_DataCleanUP_FilteredData.Find(filter).Project<BsonDocument>(projection).ToList();
                                if (collection_DataCleanUP_FilteredData_result.Count > 0)
                                {
                                    foreach (var result in collection_DataCleanUP_FilteredData_result)
                                    {
                                        var archiveMod = new ArchiveModel
                                        {
                                            CollectionName = CONSTANTS.DataCleanUP_FilteredData,
                                            CollectionValue = result,
                                            ArchiveDate = DateTime.Now.ToString(CONSTANTS.DateHoursFormat)
                                        };
                                        var archivecollection = _database.GetCollection<ArchiveModel>(CONSTANTS.DataCleanUP_FilteredData_archive);
                                        archivecollection.InsertOne(archiveMod);

                                        var myfilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, doc.CorrelationId);
                                        collection_DataCleanUP_FilteredData.DeleteOne(myfilter);
                                    }
                                }

                                var collection_ME_FeatureSelection = _database.GetCollection<BsonDocument>(CONSTANTS.ME_FeatureSelection);
                                var collection_ME_FeatureSelection_result = collection_ME_FeatureSelection.Find(filter).Project<BsonDocument>(projection).ToList();
                                if (collection_ME_FeatureSelection_result.Count > 0)
                                {
                                    foreach (var result in collection_ME_FeatureSelection_result)
                                    {
                                        var archiveMod = new ArchiveModel
                                        {
                                            CollectionName = CONSTANTS.ME_FeatureSelection,
                                            CollectionValue = result,
                                            ArchiveDate = DateTime.Now.ToString(CONSTANTS.DateHoursFormat)
                                        };
                                        var archivecollection = _database.GetCollection<ArchiveModel>(CONSTANTS.ME_FeatureSelection_archive);
                                        archivecollection.InsertOne(archiveMod);

                                        var myfilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, doc.CorrelationId);
                                        collection_ME_FeatureSelection.DeleteOne(myfilter);
                                    }
                                }

                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(DeployModelService), nameof(ArchiveRecords), "Archival Process - Ended " + ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DeployModelService), nameof(ArchiveRecords), "Archival Process - Ended ", string.Empty, string.Empty, string.Empty, string.Empty);
        }        

        public void PurgeRecords()
        {
            // Purging the records which are archived more than 6 months                
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DeployModelService), nameof(PurgeRecords), "Purging Process - Started ", string.Empty, string.Empty, string.Empty, string.Empty);
            List<string> collectionList = new List<string> {
                CONSTANTS.DE_DataCleanup_archive,
                CONSTANTS.DE_DataProcessing_archive,
                CONSTANTS.DE_NewFeatureData_archive,
                CONSTANTS.DE_PreProcessedData_archive,
                CONSTANTS.PS_BusinessProblem_archive,
                CONSTANTS.PS_IngestedData_archive,
                CONSTANTS.PS_UsecaseDefinition_archive,
                CONSTANTS.ME_RecommendedModels_archive,
                CONSTANTS.ME_HyperTuneVersion_archive
            };

            ArchiveDeployModelsDto deployModelsDtos = new ArchiveDeployModelsDto();
            var DeployModelcollection = _database.GetCollection<ArchiveModels>(CONSTANTS.SSAI_DeployedModels_archive);
            var projection = Builders<ArchiveModels>.Projection.Exclude(CONSTANTS.Id).Exclude("CollectionValue._id"); 
            var filter = Builders<ArchiveModels>.Filter.Empty;
            var result = DeployModelcollection.Find(filter).Project<ArchiveModels>(projection).ToList();
            if (result.Count > 0)
            {
                foreach (var doc in result)
                {
                    deployModelsDtos = doc.CollectionValue;
                    if (!ExcludedUseCaselst.Contains(deployModelsDtos.CorrelationId))
                    {
                        if (Convert.ToDateTime(doc.ArchiveDate) < DateTime.Now.AddDays(CONSTANTS.PurgeDays))
                        {
                            DeployModelcollection.DeleteOne(filter);
                            var Purgingfilter = Builders<BsonDocument>.Filter.Eq("CollectionValue.CorrelationId", deployModelsDtos.CorrelationId);
                            foreach (var coll in collectionList)
                            {
                                var collection = _database.GetCollection<BsonDocument>(coll);
                                if (collection.Find(Purgingfilter).ToList().Count > 0)
                                {
                                    collection.DeleteMany(Purgingfilter);
                                }
                            }


                            //Deleting pickle files physically
                            var savedcollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_savedModels_archive);
                            var projection1 = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
                            var savedModelResult = savedcollection.Find(Purgingfilter).Project<BsonDocument>(projection1).ToList();
                            if (savedModelResult.Count > 0)
                            {
                                foreach (var filedoc in savedModelResult)
                                {
                                    if (Convert.ToDateTime(filedoc["ArchiveDate"]) < DateTime.Now.AddDays(CONSTANTS.PurgeDays))
                                    {
                                        if (Convert.ToString(filedoc["CollectionValue"][CONSTANTS.FileType]) == CONSTANTS.MLDL_Model)
                                        {
                                            string filePath = filedoc["CollectionValue"][CONSTANTS.FilePath].ToString();
                                            if (File.Exists(filePath))
                                            {
                                                File.Delete(filePath);
                                            }
                                        }
                                        savedcollection.DeleteOne(Purgingfilter);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            //archive deploymodel based on retention duration inputs from UI
            //var deploycollection = _database.GetCollection<ArchiveDeployModelsDto>(CONSTANTS.SSAI_DeployedModels);
            //var deployfilter = Builders<ArchiveDeployModelsDto>.Filter.Empty;
            //var deployprojection = Builders<ArchiveDeployModelsDto>.Projection.Exclude(CONSTANTS.Id);
            //var deployResult = deploycollection.Find(deployfilter).Project<ArchiveDeployModelsDto>(deployprojection).ToList();
            //if (deployResult.Count > 0)
            //{
            //    foreach (var doc in deployResult)
            //    {
            //        int val = Convert.ToInt32(doc.ArchivalDays);
            //        if (val > 0)
            //        {
            //            DateTime deployedDate = Convert.ToDateTime(doc.DeployedDate);
            //            DateTime archDate = deployedDate.AddDays(val);
            //            if (Convert.ToString(archDate) == DateTime.Now.ToString(CONSTANTS.DateHoursFormat))
            //            {
            //                var arch = new ArchiveModels
            //                {
            //                    CollectionName = CONSTANTS.SSAI_DeployedModels,
            //                    CollectionValue = doc,
            //                    ArchiveDate = DateTime.Now.ToString(CONSTANTS.DateHoursFormat)
            //                };

            //                var deployarchivecollection = _database.GetCollection<ArchiveModels>(CONSTANTS.SSAI_DeployedModels_archive);
            //                deployarchivecollection.InsertOne(arch);

            //                deploycollection.DeleteOne(deployfilter);

            //            }
            //        }
            //    }
            //}
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DeployModelService), nameof(PurgeRecords), "Purging Process - Ended ", string.Empty, string.Empty, string.Empty, string.Empty);
        }

      
    }
}
