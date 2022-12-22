#region Copyright (c) 2018 Accenture . All rights reserved.

// <copyright company="Accenture">
// Copyright (c) 2018 Accenture.  All rights reserved.
// Reproduction or transmission in whole or in part, in any form or by any means, 
// electronic, mechanical or otherwise, is prohibited without the prior written 
// consent of the copyright owner.
// </copyright>

#endregion

#region ProcessDataService Information
/********************************************************************************************************\
Module Name     :   ProcessDataService
Project         :   Accenture.MyWizard.SelfServiceAI.BusinessDomain
Organisation    :   Accenture Technologies Ltd.
Created By      :   RaviA
Created Date    :   30-Jan-2019
Revision History :
Revision No             Initial             Modified Date           Description
1.0                     An                  30-Jan-2019             
\********************************************************************************************************/
#endregion

namespace Accenture.MyWizard.Ingrain.BusinessDomain.Services
{
    #region Namespace
    using Accenture.MyWizard.Ingrain.BusinessDomain.Common;
    using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
    using Accenture.MyWizard.Ingrain.DataAccess;
    using Accenture.MyWizard.Ingrain.DataModels.Models;
    using Accenture.MyWizard.Shared.Helpers;
    using Microsoft.Extensions.Options;
    using MongoDB.Bson;
    using MongoDB.Bson.Serialization;
    using MongoDB.Driver;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Extensions.DependencyInjection;
    using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
    using System.Threading;
    using System.Runtime.CompilerServices;
    #endregion

    /// <summary>
    /// Process Data Service
    /// </summary>
    public class ProcessDataService : IProcessDataService
    {
        #region Members
        private MongoClient _mongoClient;
        private IMongoDatabase _database;
        private PreProcessDTO _preProcessDTO;
        private readonly DatabaseProvider databaseProvider;
        private readonly IModelEngineering _modelEngineering;
        private readonly IIngestedData _ingestedData;
        private readonly IEncryptionDecryption _encryptionDecryption;
        private IOptions<IngrainAppSettings> appSettings { get; set; }
        PreProcessModelDTO preProcessModel;
        private bool autoBinningEnabled { get; set; }

        private bool isMultiClass { get; set; }
        #endregion

        #region Constructors

        /// <summary>
        /// Process Data Service Constructor
        /// </summary>
        /// <param name="db"></param>
        /// <param name="settings"></param>
        /// <param name="serviceProvider"></param>
        public ProcessDataService(DatabaseProvider db, IOptions<IngrainAppSettings> settings, IServiceProvider serviceProvider)
        {
            databaseProvider = db;
            appSettings = settings;
            _mongoClient = databaseProvider.GetDatabaseConnection();
            var dataBaseName = MongoUrl.Create(appSettings.Value.connectionString).DatabaseName;
            _database = _mongoClient.GetDatabase(dataBaseName);
            _preProcessDTO = new PreProcessDTO();
            _modelEngineering = serviceProvider.GetService<IModelEngineering>();
            _ingestedData = serviceProvider.GetService<IIngestedData>();
            preProcessModel = new PreProcessModelDTO();
            _encryptionDecryption = serviceProvider.GetService<IEncryptionDecryption>();
        }
        #endregion

        #region Methods

        /// <summary>
        /// Get the ProcessedModel data. 
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="userId"></param>
        /// <param name="pageInfo"></param>
        /// <returns></returns>
        public DataEngineeringDTO ProcessDataForModelling(string correlationId, string userId, string pageInfo)
        {
            DataEngineeringDTO dataCleanUPData = new DataEngineeringDTO();
            string data = string.Empty;
            var correlationFilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var filter = correlationFilter;
            data = GetProcessData(pageInfo, filter, correlationId);
            JObject serializeData = new JObject();
            if (!string.IsNullOrEmpty(data))
            {
                serializeData = JObject.Parse(data);
                dataCleanUPData.processData = data;
            }
            var BusinessProbCollection = _database.GetCollection<BsonDocument>(CONSTANTS.PSBusinessProblem);
            // ModelEngineeringService modelEngineeringService = new ModelEngineeringService();
            string modelType = _modelEngineering.GetSelectedModel(correlationId);
            var newFieldUpdate = Builders<BsonDocument>.Update.Set(CONSTANTS.ProblemType, modelType);
            var resultBusinessProb = BusinessProbCollection.UpdateOne(filter, newFieldUpdate);
            var projection = Builders<BsonDocument>.Projection.Include("TargetColumn").Include("TargetUniqueIdentifier").Include("IsCustomColumnSelected").Exclude("_id");
            var TargetColumn = BusinessProbCollection.Find(filter).Project<BsonDocument>(projection).FirstOrDefault();
            dataCleanUPData.TargetColumn = TargetColumn["TargetColumn"].ToString();
            dataCleanUPData.TargetUniqueIdentifier = TargetColumn["TargetUniqueIdentifier"].ToString();
            List<string> datasource = new List<string>();
            datasource = CommonUtility.GetDataSourceModel(correlationId, appSettings);
            if (datasource.Count > 0)
            {
                dataCleanUPData.ModelName = datasource[0];
                dataCleanUPData.DataSource = datasource[1];
                dataCleanUPData.ModelType = datasource[2];
                if (!string.IsNullOrEmpty(datasource[4]))
                {
                    dataCleanUPData.InstaFlag = true;
                    dataCleanUPData.Category = datasource[5];
                };
                if (datasource.Count > 2)
                {
                    dataCleanUPData.BusinessProblem = datasource[3];
                    dataCleanUPData.Category = datasource[5];
                }
            }

            #region Add Features
            dataCleanUPData.TextTypeColumnList = this.GetTextTypeColumns(correlationId, dataCleanUPData.TargetColumn, serializeData);
            dataCleanUPData.CleanedUpColumnList = this.GetCleanedUpColumns(correlationId);

            dataCleanUPData.IsCustomColumnSelected = TargetColumn.Contains("IsCustomColumnSelected") ? Convert.ToString(TargetColumn["IsCustomColumnSelected"]) : "False";
            var featureSelection = this.GetAddFeatureOutcome(correlationId);
            if (featureSelection.Count > 0)
            {
                dataCleanUPData.FeatureSelectionData = JObject.Parse(featureSelection[0].ToString());
            }
            //Modified to remove already Created Feature #Bug 1037967 - as per shrayani and Kailash modified
            dataCleanUPData.FeatureDataTypes = this.GetAllFeatureFromDECleanUp(correlationId);//this.GetAllFeatureFromDECleanUp(correlationId);//this.GetCleanUpFeatureDataType(correlationId, dataCleanUPData.FeatureSelectionData);
            #endregion

            dataCleanUPData.IsModelTrained = this.IsModelTrained(correlationId);
            return dataCleanUPData;
        }

        private bool IsModelTrained(string correlationId)
        {
            var columnCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIRecommendedTrainedModels);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var outcome = columnCollection.Find(filter).ToList();
            return outcome.Count > 0 ? true : false;
        }
        private List<BsonDocument> GetAddFeatureOutcome(string correlationId)
        {
            _preProcessDTO.CorrelationId = correlationId;
            var filter = Builders<BsonDocument>.Filter.Eq("CorrelationId", correlationId);
            var featureSelectionProjection = Builders<BsonDocument>.Projection.Include("Feature_Not_Created").Include("Features_Created").Include("Map_Encode_New_Feature").Include("Existing_Features").Exclude("_id");
            var FeatureSelectionCollection = _database.GetCollection<BsonDocument>("DE_AddNewFeature");
            var featureSelectionData = FeatureSelectionCollection.Find(filter).Project<BsonDocument>(featureSelectionProjection).ToList();
            //bool DBEncryptionRequired = CommonUtility.EncryptDB(correlationId, appSettings);
            //if (DBEncryptionRequired)
            //{
            //    if (featureSelectionData.Count > 0)
            //    {
            //        for (var i = 0; i < featureSelectionData.Count; i++)
            //        {
            //            if (featureSelectionData[i].Contains(CONSTANTS.CreatedByUser) && !string.IsNullOrEmpty(Convert.ToString(featureSelectionData[i][CONSTANTS.CreatedByUser])))
            //                featureSelectionData[i][CONSTANTS.CreatedByUser] = _encryptionDecryption.Decrypt(Convert.ToString(featureSelectionData[i][CONSTANTS.CreatedByUser]));
            //            if (featureSelectionData[i].Contains(CONSTANTS.ModifiedByUser) && !string.IsNullOrEmpty(Convert.ToString(featureSelectionData[i][CONSTANTS.ModifiedByUser])))
            //                featureSelectionData[i][CONSTANTS.ModifiedByUser] = _encryptionDecryption.Decrypt(Convert.ToString(featureSelectionData[i][CONSTANTS.ModifiedByUser]));
            //        }
            //    }
            //}
            return featureSelectionData;
        }

        public bool IsMultiClass(string correlationId)
        {
            var problemCollection = _database.GetCollection<DataCleanUpDto>(CONSTANTS.DEDataCleanup);
            var filterRecord = Builders<DataCleanUpDto>.Filter.Eq(x => x.CorrelationId, correlationId);
            var applyProjection = Builders<DataCleanUpDto>.Projection.Include(x => x.Target_ProblemType).Include(x => x.CorrelationId).Exclude(x => x._id);
            var dataCleanup = problemCollection.Find(filterRecord).Project<DataCleanUpDto>(applyProjection).FirstOrDefault();
            if (dataCleanup != null && dataCleanup.Target_ProblemType > 0)
            {
                if (dataCleanup.Target_ProblemType == 3)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Checking that User has Model in UseCase table.
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="userId"></param>
        /// <param name="pageInfo"></param>
        /// <returns>status and progress</returns>
        public string CheckPythonProcess(string correlationId, string pageInfo)
        {
            string userModel = string.Empty;
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq(CONSTANTS.CorrelationId, correlationId) & builder.Eq(CONSTANTS.pageInfo, pageInfo);
            var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.Status).Include(CONSTANTS.Progress).
            Include(CONSTANTS.CorrelationId).Include(CONSTANTS.pageInfo).Include(CONSTANTS.Message).Exclude(CONSTANTS.Id);
            var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                userModel = result[0].ToJson();
            }
            return userModel;
        }
        public string CheckPythonProcess(string correlationId, string pageInfo, string uploadId)
        {
            string userModel = string.Empty;
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIUseCase);
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq(CONSTANTS.CorrelationId, correlationId) & builder.Eq(CONSTANTS.pageInfo, pageInfo) & builder.Eq(CONSTANTS.UniId, uploadId);
            var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.Status).Include(CONSTANTS.Progress).Include(CONSTANTS.CorrelationId).Include(CONSTANTS.pageInfo).Include(CONSTANTS.Message).Exclude(CONSTANTS.Id);
            var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                userModel = result[0].ToJson();
            }
            return userModel;
        }

        public string HyperTunedQueueDetails(string correlationId, string pageInfo, string hyperTuneId)
        {
            List<UseCase> userModels = new List<UseCase>();
            string userModel = string.Empty;
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIUseCase);
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq(CONSTANTS.CorrelationId, correlationId) & builder.Eq(CONSTANTS.pageInfo, pageInfo) & builder.Eq(CONSTANTS.UniId, hyperTuneId);
            var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.Status).Include(CONSTANTS.Progress).Include(CONSTANTS.CorrelationId).Include(CONSTANTS.pageInfo).Include(CONSTANTS.ModelName).Include(CONSTANTS.Message).Exclude(CONSTANTS.Id); //need to add estimated run time
            var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                userModel = result[0].ToJson();
            }
            return userModel;
        }

        public List<UseCase> RecommendedAIQueueDetails(string correlationId, string pageInfo)
        {
            List<UseCase> userModels = new List<UseCase>();
            string userModel = string.Empty;
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIUseCase);
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq(CONSTANTS.CorrelationId, correlationId) & builder.Eq(CONSTANTS.pageInfo, pageInfo);
            var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.Status).Include(CONSTANTS.Progress).Include(CONSTANTS.Model_Name).Include(CONSTANTS.ProblemType).Include(CONSTANTS.Message).Exclude(CONSTANTS.Id);
            var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                for (int i = 0; i < result.Count; i++)
                {
                    UseCase useCase = new UseCase();
                    var parse = JObject.Parse(result[i].ToString());
                    useCase.Status = parse[CONSTANTS.Status].ToString();
                    useCase.Progress = parse[CONSTANTS.Progress].ToString();
                    useCase.ModelName = parse[CONSTANTS.Model_Name].ToString();
                    useCase.ProblemType = parse[CONSTANTS.ProblemType].ToString();
                    userModels.Add(useCase);
                }
            }
            return userModels;
        }

        public bool IsRecommendedAIPythonInvoked(string correlationId)
        {
            bool isPytonCallInvoked;
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIUseCase);
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq(CONSTANTS.CorrelationId, correlationId) & builder.Eq(CONSTANTS.pageInfo, CONSTANTS.ModelTraining);
            var result = collection.Find(filter).ToList();
            return isPytonCallInvoked = result.Count > 0 ? true : false;
        }


        /// <summary>
        /// Filter the ProcessedDataModel based on the Scrreen
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="userId"></param>
        /// <param name="pageInfo"></param>
        /// <param name="filter"></param>
        /// <returns>Screen Processed ModelData</returns>
        private string GetProcessData(string pageInfo, FilterDefinition<BsonDocument> filter, string correlationId)
        {

            IMongoCollection<BsonDocument> collection;
            List<BsonDocument> bsonElements = null;
            string processData = string.Empty;
            bool DBEncryptionRequired = CommonUtility.EncryptDB(correlationId, appSettings);
            switch (pageInfo)
            {
                case CONSTANTS.DataCleanUp:
                    collection = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataCleanup);
                    var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.FeatureName).Include(CONSTANTS.CorrelationToRemove).Include(CONSTANTS.CorrelationId).Include("UnchangedDtypeColumns").Include(CONSTANTS.ProcessedRecords).Include(CONSTANTS.NewFeatureName).Include(CONSTANTS.NewAddFeatures).Exclude(CONSTANTS.Id);
                    bsonElements = collection.Find(filter).Project<BsonDocument>(projection).ToList();
                    if (bsonElements.Count > 0)
                    {
                        BsonDocument processDocument = bsonElements[0];
                        //decrypt db data
                        if (DBEncryptionRequired)
                        {
                            processDocument[CONSTANTS.FeatureName] = BsonDocument.Parse(_encryptionDecryption.Decrypt(bsonElements[0][CONSTANTS.FeatureName].AsString));
                            //if (processDocument.Contains(CONSTANTS.CreatedByUser) && bsonElements[0].Contains(CONSTANTS.CreatedByUser) && !string.IsNullOrEmpty(Convert.ToString(bsonElements[0][CONSTANTS.CreatedByUser])))
                            //{
                            //    processDocument[CONSTANTS.CreatedByUser] = _encryptionDecryption.Decrypt(Convert.ToString(bsonElements[0][CONSTANTS.CreatedByUser]));
                            //}
                            //if (processDocument.Contains(CONSTANTS.ModifiedByUser) && bsonElements[0].Contains(CONSTANTS.ModifiedByUser) && !string.IsNullOrEmpty(Convert.ToString(bsonElements[0][CONSTANTS.ModifiedByUser])))
                            //{
                            //    processDocument[CONSTANTS.ModifiedByUser] = _encryptionDecryption.Decrypt(Convert.ToString(bsonElements[0][CONSTANTS.ModifiedByUser]));
                            //}
                            if (processDocument.Contains(CONSTANTS.NewFeatureName))
                            {
                                if (processDocument[CONSTANTS.NewFeatureName].ToString() != "{ }")
                                    processDocument[CONSTANTS.NewFeatureName] = BsonDocument.Parse(_encryptionDecryption.Decrypt(bsonElements[0][CONSTANTS.NewFeatureName].AsString));
                            }
                        }

                        JObject data = JObject.Parse(processDocument.ToString());
                        JObject combinedFeatures = new JObject();
                        combinedFeatures = this.CombinedFeatures(data);
                        if (combinedFeatures != null)
                            processDocument[CONSTANTS.FeatureName] = BsonDocument.Parse(combinedFeatures.ToString());

                        processData = processDocument.ToString();
                    }
                    break;
            }

            return processData;
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

        /// <summary>
        /// Update default field to False and update the user selected one to True.
        /// </summary>
        /// <param name="data"></param>
        public void InsertDataCleanUp(dynamic data, string correlationId)
        {
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataCleanup);
            var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.FeatureName).Exclude(CONSTANTS.Id);
            var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            bool DBEncryptionRequired = CommonUtility.EncryptDB(correlationId, appSettings);
            if (result.Count > 0)
            {
                //decrypt db values
                if (DBEncryptionRequired)
                    result[0][CONSTANTS.FeatureName] = BsonDocument.Parse(_encryptionDecryption.Decrypt(result[0][CONSTANTS.FeatureName].AsString));


                var featuresData = JObject.Parse(result[0].ToString());

                string scaleModifiedCols = string.Format(CONSTANTS.ScaleModifiedColumns);
                var scaleUpdatemodify = Builders<BsonDocument>.Update.Set(scaleModifiedCols, data.ScaleModifiedColumns);
                collection.UpdateOne(filter, scaleUpdatemodify);

                string datatypeModifiedCols = string.Format(CONSTANTS.DtypeModifiedColumns);
                var datatypeUpdate = Builders<BsonDocument>.Update.Set(datatypeModifiedCols, data.DtypeModifiedColumns);
                collection.UpdateOne(filter, datatypeUpdate);

                foreach (var datatypes in data.datatypes)
                {
                    //var parentData = featuresData[CONSTANTS.FeatureName][datatypes.Name];
                    //if (parentData != null)
                    //{
                    //    foreach (var item in featuresData[CONSTANTS.FeatureName][datatypes.Name][CONSTANTS.Datatype].Children())
                    //    {
                    //        string columnToupdate = string.Format(CONSTANTS.FeatureNameDatatype, datatypes.Name, item.Name);
                    //        var newFieldUpdate = Builders<BsonDocument>.Update.Set(columnToupdate, CONSTANTS.False);
                    //        collection.UpdateOne(filter, newFieldUpdate);
                    //    }
                    //}
                    ////update the user input field to True
                    //string columnToupdate2 = string.Format(CONSTANTS.FeatureNameDatatype, datatypes.Name, datatypes.First.First.Value);
                    //var newFieldUpdate2 = Builders<BsonDocument>.Update.Set(columnToupdate2, CONSTANTS.True);
                    //var result2 = collection.UpdateOne(filter, newFieldUpdate2);

                    string columnToupdate3 = string.Format(CONSTANTS.DtypeModified_Columns, datatypes.Name);
                    var newFieldUpdate3 = Builders<BsonDocument>.Update.Set(columnToupdate3, datatypes.First.First.Value);
                    var result3 = collection.UpdateOne(filter, newFieldUpdate3);

                }
                foreach (var scale in data.scales)
                {
                    var parentData = featuresData[CONSTANTS.FeatureName][scale.Name];
                    if (parentData != null)
                    {
                        foreach (var item in featuresData[CONSTANTS.FeatureName][scale.Name][CONSTANTS.Scale].Children())
                        {
                            featuresData[CONSTANTS.FeatureName][scale.Name]["Scale"][item.Name] = CONSTANTS.False;
                            //string columnToupdate = string.Format(CONSTANTS.FeatureNameScale, scale.Name, item.Name);
                            //var newFieldUpdate = Builders<BsonDocument>.Update.Set(columnToupdate, CONSTANTS.False);
                            //collection.UpdateOne(filter, newFieldUpdate);
                        }
                    }
                    //update the user input field to True
                    featuresData[CONSTANTS.FeatureName][scale.Name]["Scale"][scale.Value.ToString()] = CONSTANTS.True;
                    //string scaleColumn = string.Format(CONSTANTS.FeatureNameScale, scale.Name, scale.Value.ToString());
                    //var scaleUpdate = Builders<BsonDocument>.Update.Set(scaleColumn, CONSTANTS.True);
                    //var scaleResult = collection.UpdateOne(filter, scaleUpdate);

                    string scaleColumn3 = string.Format(CONSTANTS.ScaleModified_Columns, scale.Name);
                    var scaleUpdate3 = Builders<BsonDocument>.Update.Set(scaleColumn3, scale.Value.ToString());
                    var scaleResult3 = collection.UpdateOne(filter, scaleUpdate3);

                }

                // encrypt db values
                if (DBEncryptionRequired)
                {
                    var scaleUpdate = Builders<BsonDocument>.Update.Set(CONSTANTS.FeatureName, _encryptionDecryption.Encrypt(featuresData[CONSTANTS.FeatureName].ToString(Formatting.None)));
                    collection.UpdateOne(filter, scaleUpdate);
                }
                else
                {
                    var Featuredata = BsonDocument.Parse(featuresData[CONSTANTS.FeatureName].ToString());
                    var scaleUpdate = Builders<BsonDocument>.Update.Set(CONSTANTS.FeatureName, Featuredata);
                    collection.UpdateOne(filter, scaleUpdate);
                }


            }
            var preProcessCollection = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataProcessing);
            var dataExistInPreProcess = preProcessCollection.Find(filter).ToList();
            if (dataExistInPreProcess.Count > 0)
            {
                if (data != null)
                    preProcessCollection.DeleteOne(filter);
            }

            var builder = Builders<BsonDocument>.Filter;
            var useCaseFilter = builder.Eq(CONSTANTS.CorrelationId, correlationId) & builder.Eq(CONSTANTS.pageInfo, CONSTANTS.DataPreprocessing);
            var useCaseCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIUseCase);
            var dataExistInUsecase = useCaseCollection.Find(useCaseFilter).ToList();
            if (dataExistInUsecase.Count > 0)
            {
                if (data != null)
                    useCaseCollection.DeleteOne(useCaseFilter);
            }
        }

        /// <summary>
        /// Insert/Update AddFeatures
        /// </summary>
        /// <param name="data">The data</param>
        /// <param name="correlationId">The correlationId</param>
        public void InsertAddFeatures(dynamic data, string correlationId)
        {
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataCleanup);
            var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.CorrelationId).Include(CONSTANTS.NewAddFeatures).Exclude(CONSTANTS.Id);
            var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                var cleanUpData = JObject.Parse(result[0].ToString());
                if (data.NewAddFeatures != null)
                {
                    if (cleanUpData[CONSTANTS.NewAddFeatures] == null)
                        cleanUpData[CONSTANTS.NewAddFeatures] = new JObject();

                    cleanUpData[CONSTANTS.NewAddFeatures] = JObject.Parse(data[CONSTANTS.NewAddFeatures].ToString(Formatting.None).Replace("'", "''"));

                }
                else
                {
                    cleanUpData[CONSTANTS.NewAddFeatures] = string.Empty;
                }

                var newAddFeatures = BsonDocument.Parse(cleanUpData[CONSTANTS.NewAddFeatures].ToString());
                var updateData = Builders<BsonDocument>.Update.Set(CONSTANTS.NewAddFeatures, newAddFeatures);
                collection.UpdateOne(filter, updateData);

                var preProcessCollection = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataProcessing);
                if (preProcessCollection.Find(filter).ToList().Count > 0)
                {
                    preProcessCollection.DeleteMany(filter);
                }
            }
        }

        public string PostPreprocessData(dynamic data, string correlationId)
        {
            string resultData = string.Empty;
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataProcessing);
            var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.Filters).Include(CONSTANTS.MissingValues).Include(CONSTANTS.DataModification).Include(CONSTANTS.DataTransformationApplied).Include(CONSTANTS.SmoteMulticlass).Exclude(CONSTANTS.Id);
            var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            bool DBEncryptionRequired = CommonUtility.EncryptDB(correlationId, appSettings);
            if (result.Count > 0)
            {
                //decrypt db data
                if (DBEncryptionRequired)
                {
                    result[0][CONSTANTS.DataModification] = BsonDocument.Parse(_encryptionDecryption.Decrypt(result[0][CONSTANTS.DataModification].AsString));
                    result[0][CONSTANTS.MissingValues] = BsonDocument.Parse(_encryptionDecryption.Decrypt(result[0][CONSTANTS.MissingValues].AsString));
                    result[0][CONSTANTS.Filters] = BsonDocument.Parse(_encryptionDecryption.Decrypt(result[0][CONSTANTS.Filters].AsString));
                    //if (!string.IsNullOrEmpty(Convert.ToString( result[0][CONSTANTS.CreatedByUser])))
                    //    result[0][CONSTANTS.CreatedByUser] = _encryptionDecryption.Decrypt(Convert.ToString(result[0][CONSTANTS.CreatedByUser]));
                    //if (!string.IsNullOrEmpty(Convert.ToString(result[0][CONSTANTS.ModifiedByUser])))
                    //    result[0][CONSTANTS.ModifiedByUser] = _encryptionDecryption.Decrypt(Convert.ToString(result[0][CONSTANTS.ModifiedByUser]));
                }
                var preProcessData = JObject.Parse(result[0].ToString());
                // New Feature NLP- Text Data Processing by Shreya - Start
                updateTextDataProcessing(data, preProcessData, collection, filter, DBEncryptionRequired);
                // New Feature NLP- Text Data Processing by Shreya - End
                UpdateDataModificationValues(data, preProcessData, collection, filter, DBEncryptionRequired);
                UpdatePreProcessMissingValues(data, preProcessData, collection, filter, DBEncryptionRequired);
                UpdateFilterPreprocessValues(data, preProcessData, collection, filter, DBEncryptionRequired);
                // UpdateNewFeatures( data, preProcessData, collection, filter, DBEncryptionRequired);
                //added to update smote flag
                updateSmoteFlag(data, collection, filter, DBEncryptionRequired);
                return resultData = CONSTANTS.Success;
            }
            else
            {
                return resultData;
            }
        }


        //public void UpdateNewFeatures(dynamic data, JObject preProcessData, IMongoCollection<BsonDocument> collection, FilterDefinition<BsonDocument> filter, bool DBEncryptionRequired)
        //{
        //    if (data.DataModification.NewAddFeatures != null)
        //    {
        //        //BsonDocument doc2 = BsonDocument.Parse(inputData[CONSTANTS.DataModification]["NewAddFeatures"].ToString());
        //        //var newFieldUpdate2 = Builders<BsonDocument>.Update.Set("DataModification.NewAddFeatures", doc2);
        //        //collection.UpdateOne(filter, newFieldUpdate2);

        //        if (preProcessData[CONSTANTS.DataModification]["NewAddFeatures"] == null)
        //        {
        //            preProcessData[CONSTANTS.DataModification]["NewAddFeatures"] = new JObject();
        //            preProcessData[CONSTANTS.DataModification]["NewAddFeatures"] = JObject.Parse(data[CONSTANTS.DataModification]["NewAddFeatures"].ToString(Formatting.None).Replace("'", "''"));
        //        }
        //        else
        //        {
        //            preProcessData[CONSTANTS.DataModification]["NewAddFeatures"] = JObject.Parse(data[CONSTANTS.DataModification]["NewAddFeatures"].ToString(Formatting.None).Replace("'", "''"));
        //        }

        //    }
        //    else
        //    {
        //        //var newFieldUpdate2 = Builders<BsonDocument>.Update.Set("DataModification.NewAddFeatures", "");
        //        //collection.UpdateOne(filter, newFieldUpdate2);
        //        preProcessData[CONSTANTS.DataModification]["NewAddFeatures"] = "";
        //    }

        //    if (DBEncryptionRequired)
        //    {
        //        var addFeaturesUpdate = Builders<BsonDocument>.Update.Set(CONSTANTS.DataModification, _encryptionDecryption.Encrypt(preProcessData[CONSTANTS.DataModification].ToString(Formatting.None)));
        //        collection.UpdateOne(filter, addFeaturesUpdate);
        //    }
        //    else
        //    {
        //        var addFeaturesUpdate = Builders<BsonDocument>.Update.Set(CONSTANTS.DataModification, preProcessData[CONSTANTS.DataModification].ToString(Formatting.None));
        //        collection.UpdateOne(filter, addFeaturesUpdate);
        //    }
        //}

        /// <summary>
        /// NLP - Update Text Data PreProcessing details
        /// </summary>
        /// <param name="data"></param>
        /// <param name="preProcessData"></param>
        /// <param name="collection"></param>
        /// <param name="filter"></param>
        /// <param name="correlationId"></param>
        public void updateTextDataProcessing(dynamic data, JObject preProcessData, IMongoCollection<BsonDocument> collection, FilterDefinition<BsonDocument> filter, bool DBEncryptionRequired)
        {
            List<string> deleteColumns = new List<string>();
            JObject inputData = JObject.Parse(data.ToString());
            if (data.DataModification.TextDataPreprocessing != null)
            {
                if (preProcessData[CONSTANTS.DataModification][CONSTANTS.TextDataPreprocessing] == null)
                {
                    preProcessData[CONSTANTS.DataModification][CONSTANTS.TextDataPreprocessing] = new JObject();
                    preProcessData[CONSTANTS.DataModification][CONSTANTS.TextDataPreprocessing] = JObject.Parse(inputData[CONSTANTS.DataModification][CONSTANTS.TextDataPreprocessing].ToString(Formatting.None).Replace("'", "''"));
                }
                else
                    preProcessData[CONSTANTS.DataModification][CONSTANTS.TextDataPreprocessing] = JObject.Parse(inputData[CONSTANTS.DataModification][CONSTANTS.TextDataPreprocessing].ToString(Formatting.None).Replace("'", "''"));
            }
            else
            {
                preProcessData[CONSTANTS.DataModification][CONSTANTS.TextDataPreprocessing] = new JObject();
                preProcessData[CONSTANTS.DataModification][CONSTANTS.TextDataPreprocessing] = "";
            }

            //if (data.DataModification.NewAddFeatures != null)
            //{
            //    if (preProcessData[CONSTANTS.DataModification]["NewAddFeatures"] == null)
            //    {
            //        preProcessData[CONSTANTS.DataModification]["NewAddFeatures"] = new JObject();
            //        preProcessData[CONSTANTS.DataModification]["NewAddFeatures"] = JObject.Parse(data[CONSTANTS.DataModification]["NewAddFeatures"].ToString(Formatting.None).Replace("'", "''"));
            //    }
            //    else
            //    {
            //        preProcessData[CONSTANTS.DataModification]["NewAddFeatures"] = JObject.Parse(data[CONSTANTS.DataModification]["NewAddFeatures"].ToString(Formatting.None).Replace("'", "''"));
            //    }

            //}
            //else
            //{
            //    preProcessData[CONSTANTS.DataModification]["NewAddFeatures"] = "";
            //}

            if (DBEncryptionRequired)
            {
                var textdataUpdate = Builders<BsonDocument>.Update.Set(CONSTANTS.DataModification, _encryptionDecryption.Encrypt(preProcessData[CONSTANTS.DataModification].ToString(Formatting.None)));
                collection.UpdateOne(filter, textdataUpdate);
            }
            else
            {
                var modificationData = BsonDocument.Parse(preProcessData[CONSTANTS.DataModification].ToString());
                var textdataUpdate = Builders<BsonDocument>.Update.Set(CONSTANTS.DataModification, modificationData);
                collection.UpdateOne(filter, textdataUpdate);
            }
        }

        public void updateSmoteFlag(dynamic data, IMongoCollection<BsonDocument> collection, FilterDefinition<BsonDocument> filter, bool DBEncryptionRequired)
        {
            JObject inputData = JObject.Parse(data.ToString());
            if (data.Smote != null)
            {
                if (data.Smote.Flag != null)
                {
                    var smoteUpdate = Builders<BsonDocument>.Update.Set(CONSTANTS.Smote_Flag, inputData[CONSTANTS.Smote][CONSTANTS.Flag].ToString());
                    collection.UpdateOne(filter, smoteUpdate);
                }
            }
            //For Multiclass
            if (data.SmoteMulticlass != null)
            {
                foreach (var smoteMultiClass in data.SmoteMulticlass)
                {
                    string updateField = string.Format(CONSTANTS.SmoteMulticlassFormat, smoteMultiClass.Name.ToString());
                    var smoteUpdate = Builders<BsonDocument>.Update.Set(updateField, smoteMultiClass.Value.ToString());
                    collection.UpdateMany(filter, smoteUpdate);
                }
            }
        }

        public void updateDataTransformationApplied(string correlationId)
        {
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataProcessing);
            bool DataTransformationApplied = true;
            var newFieldUpdate = Builders<BsonDocument>.Update.Set(CONSTANTS.DataTransformationApplied, DataTransformationApplied);
            var result3 = collection.UpdateOne(filter, newFieldUpdate);

        }
        private void UpdateDataModificationValues(dynamic data, JObject preProcessData, IMongoCollection<BsonDocument> collection, FilterDefinition<BsonDocument> filter, bool DBEncryptionRequired)
        {
            if (data.DataModification.Interpolation != null)
            {
                string dataInterpolation = data.DataModification.Interpolation;
                preProcessData["DataModification"]["Features"][CONSTANTS.Interpolation] = dataInterpolation;
                //string updateField = string.Format(CONSTANTS.DataModification_Features, CONSTANTS.Interpolation);
                //var existfieldUpdate = Builders<BsonDocument>.Update.Set(updateField, dataInterpolation);
                //var existResults = collection.UpdateOne(filter, existfieldUpdate);
            }
            //Update the UI Forwarded Atribute Values
            foreach (var columnBinningValues in data.DataModification.binning)
            {
                JProperty property = columnBinningValues as JProperty;
                if (property != null)
                {
                    foreach (var item in property.Value)
                    {
                        JProperty property2 = item as JProperty;
                        string[] columnValue = property2.Value.ToString().Replace(CONSTANTS.r_n, CONSTANTS.EmptySpace).Replace(CONSTANTS.slash, CONSTANTS.InvertedComma).Replace(CONSTANTS.t, CONSTANTS.EmptySpace).Replace(CONSTANTS.openbraket, CONSTANTS.InvertedComma).Replace(CONSTANTS.closebraket, CONSTANTS.InvertedComma).Replace(CONSTANTS.open_braket, CONSTANTS.InvertedComma).Replace(CONSTANTS.close_braket, CONSTANTS.InvertedComma).Split(CONSTANTS.comma_);
                        for (int i = 1; i < columnValue.Length; i++)
                        {
                            string[] val = columnValue[i].Split(CONSTANTS.colan);
                            // string updateField = string.Format(CONSTANTS.DataModificationColumnBinning, property.Name, property2.Name, val[0].Trim());
                            // var existfieldUpdate = Builders<BsonDocument>.Update.Set(updateField, val[1].Trim());
                            // var existResults = collection.UpdateOne(filter, existfieldUpdate);
                            preProcessData[CONSTANTS.DataModification][CONSTANTS.ColumnBinning][property.Name][property2.Name][val[0].Trim()] = val[1].Trim();
                        }
                    }
                    // string updateField2 = string.Format(CONSTANTS.DataModificationColumnBinning, property.Name, CONSTANTS.ChangeRequest, CONSTANTS.ChangeRequest);
                    // var existfieldUpdate2 = Builders<BsonDocument>.Update.Set(updateField2, CONSTANTS.True);
                    // var existResults2 = collection.UpdateOne(filter, existfieldUpdate2);
                    preProcessData[CONSTANTS.DataModification][CONSTANTS.ColumnBinning][property.Name][CONSTANTS.ChangeRequest][CONSTANTS.ChangeRequest] = CONSTANTS.True;
                }
            }
            foreach (var recommendations in data.DataModification.Skewness)
            {
                JProperty property = recommendations as JProperty;
                if (property != null)
                {
                    var parentData = preProcessData[CONSTANTS.DataModification][CONSTANTS.Features][property.Name][CONSTANTS.Skewness];
                    if (parentData != null)
                    {
                        foreach (var item in preProcessData[CONSTANTS.DataModification][CONSTANTS.Features][property.Name][CONSTANTS.Skewness].Children())
                        {
                            JProperty jProperty = item as JProperty;
                            if (jProperty != null && jProperty.Name != CONSTANTS.Skeweness && jProperty.Value.ToString() == CONSTANTS.True)
                            {
                                // string updateField1 = string.Format(CONSTANTS.DataModificationFeaturesSkewness, property.Name, jProperty.Name);
                                //  var existfieldUpdate1 = Builders<BsonDocument>.Update.Set(updateField1, CONSTANTS.False);
                                //  var existResults1 = collection.UpdateOne(filter, existfieldUpdate1);
                                preProcessData[CONSTANTS.DataModification][CONSTANTS.Features][property.Name][CONSTANTS.Skewness][jProperty.Name] = CONSTANTS.False;
                            }
                        }
                    }
                    string[] columnValue = property.Value.ToString().Replace(CONSTANTS.r_n, CONSTANTS.EmptySpace).Replace(CONSTANTS.slash, CONSTANTS.InvertedComma).Replace(CONSTANTS.t, CONSTANTS.EmptySpace).Replace(CONSTANTS.openbraket, CONSTANTS.InvertedComma).Replace(CONSTANTS.closebraket, CONSTANTS.InvertedComma).Replace(CONSTANTS.open_braket, CONSTANTS.InvertedComma).Replace(CONSTANTS.close_braket, CONSTANTS.InvertedComma).Split(CONSTANTS.colan);
                    // string updateField = string.Format(CONSTANTS.DataModificationFeaturesSkewness, property.Name, columnValue[0].Trim());
                    // var existfieldUpdate = Builders<BsonDocument>.Update.Set(updateField, CONSTANTS.True);
                    // var existResults = collection.UpdateOne(filter, existfieldUpdate);
                    preProcessData[CONSTANTS.DataModification][CONSTANTS.Features][property.Name][CONSTANTS.Skewness][columnValue[0].Trim()] = CONSTANTS.True;

                }
                // string updateField2 = string.Format(CONSTANTS.DataModificationFeatures2, property.Name, CONSTANTS.Skewness, CONSTANTS.ChangeRequest);
                //var existfieldUpdate2 = Builders<BsonDocument>.Update.Set(updateField2, CONSTANTS.True);
                // var existResults2 = collection.UpdateOne(filter, existfieldUpdate2);
                preProcessData[CONSTANTS.DataModification][CONSTANTS.Features][property.Name][CONSTANTS.Skewness][CONSTANTS.ChangeRequest] = CONSTANTS.True;
            }

            foreach (var recommendations in data.DataModification.Outlier)
            {
                JProperty property = recommendations as JProperty;
                if (property != null)
                {
                    var parentData = preProcessData[CONSTANTS.DataModification][CONSTANTS.Features][property.Name][CONSTANTS.Outlier];
                    if (parentData != null)
                    {
                        foreach (var item in preProcessData[CONSTANTS.DataModification][CONSTANTS.Features][property.Name][CONSTANTS.Outlier].Children())
                        {
                            JProperty jProperty = item as JProperty;
                            if (jProperty != null && jProperty.Name != CONSTANTS.Text && jProperty.Name != CONSTANTS.CustomValue)
                            {
                                //string updateField = string.Format(CONSTANTS.DataModificationFeatures, property.Name, jProperty.Name);
                                //var existfieldUpdate = Builders<BsonDocument>.Update.Set(updateField, CONSTANTS.False);
                                //var existResults = collection.UpdateOne(filter, existfieldUpdate);
                                preProcessData[CONSTANTS.DataModification][CONSTANTS.Features][property.Name][CONSTANTS.Outlier][jProperty.Name] = CONSTANTS.False;
                            }
                        }
                    }
                    string[] columnValue = property.Value.ToString().Replace(CONSTANTS.r_n, CONSTANTS.EmptySpace).Replace(CONSTANTS.slash, CONSTANTS.InvertedComma).Replace(CONSTANTS.t, CONSTANTS.EmptySpace).Replace(CONSTANTS.openbraket, CONSTANTS.InvertedComma).Replace(CONSTANTS.closebraket, CONSTANTS.InvertedComma).Replace(CONSTANTS.open_braket, CONSTANTS.InvertedComma).Replace(CONSTANTS.close_braket, CONSTANTS.InvertedComma).Split(CONSTANTS.colan);
                    if (columnValue[0].Trim() == CONSTANTS.CustomValue)
                    {
                        //string updateField = string.Format(CONSTANTS.DataModificationFeatures, property.Name, columnValue[0].Trim());
                        //var existfieldUpdate = Builders<BsonDocument>.Update.Set(updateField, columnValue[1].Trim());
                        //var existResults = collection.UpdateOne(filter, existfieldUpdate);
                        preProcessData[CONSTANTS.DataModification][CONSTANTS.Features][property.Name]["Outlier"][columnValue[0].Trim()] = columnValue[1].Trim();
                    }
                    else
                    {
                        //Already selected Cutom Value changing to Default value. Defect fix 1520948 by Ravi A
                        preProcessData[CONSTANTS.DataModification][CONSTANTS.Features][property.Name]["Outlier"][columnValue[0].Trim()] = CONSTANTS.True;
                        preProcessData[CONSTANTS.DataModification][CONSTANTS.Features][property.Name][CONSTANTS.Outlier][CONSTANTS.CustomValue] = string.Empty;
                    }
                }
                //string updateField2 = string.Format(CONSTANTS.DataModificationFeatures2, property.Name, CONSTANTS.Outlier, CONSTANTS.ChangeRequest);
                //var existfieldUpdate2 = Builders<BsonDocument>.Update.Set(updateField2, CONSTANTS.True);
                //var existResults2 = collection.UpdateOne(filter, existfieldUpdate2);
                preProcessData["DataModification"]["Features"][property.Name][CONSTANTS.Outlier][CONSTANTS.ChangeRequest] = CONSTANTS.True;
            }

            //Update AutoBinning - Only for Classification ModelType
            List<string> autoBinningEnabledColumns = new List<string>();
            if (data.DataModification.AutoBinning != null)
            {
                foreach (var selectedAutoBinningColumns in data.DataModification.AutoBinning)
                {
                    string name = selectedAutoBinningColumns.Name.ToString().Trim();
                    string value = selectedAutoBinningColumns.Value.ToString().Trim();
                    preProcessData[CONSTANTS.DataModification][CONSTANTS.AutoBinning][name] = value;
                    if (value == CONSTANTS.True)
                        autoBinningEnabledColumns.Add(name);
                }
            }

            //encrypt db data
            if (DBEncryptionRequired)
            {
                var dataModificationUpdate = Builders<BsonDocument>.Update.Set(CONSTANTS.DataModification, _encryptionDecryption.Encrypt(preProcessData[CONSTANTS.DataModification].ToString(Formatting.None)));
                collection.UpdateOne(filter, dataModificationUpdate);
            }
            else
            {
                var modificationData = BsonDocument.Parse(preProcessData[CONSTANTS.DataModification].ToString());
                var dataModificationUpdate = Builders<BsonDocument>.Update.Set(CONSTANTS.DataModification, modificationData);
                collection.UpdateOne(filter, dataModificationUpdate);
            }

            foreach (var dataEncoding in data.DataEncoding)
            {
                JProperty property = dataEncoding as JProperty;
                if (property != null)
                {
                    string updateField = string.Format(CONSTANTS.Data_Encoding, property.Name, CONSTANTS.encoding);
                    var existfieldUpdate = Builders<BsonDocument>.Update.Set(updateField, CONSTANTS.InvertedComma);
                    collection.UpdateOne(filter, existfieldUpdate);

                    var encodeData = JsonConvert.DeserializeObject<DataEncoding>(property.Value.ToString());
                    string updateField1 = string.Format(CONSTANTS.Data_Encoding, property.Name, CONSTANTS.encoding);
                    var existfieldUpdate1 = Builders<BsonDocument>.Update.Set(updateField1, encodeData.encoding);
                    collection.UpdateOne(filter, existfieldUpdate1);
                }
                //Api Chnaging the PChangeRequest to False
                string updateField2 = string.Format(CONSTANTS.Data_Encoding, property.Name, CONSTANTS.PChangeRequest);
                var existfieldUpdate2 = Builders<BsonDocument>.Update.Set(updateField2, CONSTANTS.False);
                collection.UpdateOne(filter, existfieldUpdate2);

                //Api changing the ChangeRequest to TRUE
                //string updateField3 = string.Format(CONSTANTS.Data_Encoding, property.Name, CONSTANTS.ChangeRequest);
                //var existfieldUpdate3 = Builders<BsonDocument>.Update.Set(updateField3, CONSTANTS.True);
                //collection.UpdateOne(filter, existfieldUpdate3);

                string fieldToUpdate = string.Format(CONSTANTS.Data_Encoding, property.Name, CONSTANTS.ChangeRequest);
                UpdateDefinition<BsonDocument> updateChangeRequest;
                if (autoBinningEnabledColumns.Count > 0 && autoBinningEnabledColumns.Contains(property.Name))
                {
                    //Api changing the ChangeRequest to FALSE on AutoBinning
                    updateChangeRequest = Builders<BsonDocument>.Update.Set(fieldToUpdate, CONSTANTS.False);
                }
                else
                {
                    //Api changing the ChangeRequest to TRUE
                    updateChangeRequest = Builders<BsonDocument>.Update.Set(fieldToUpdate, CONSTANTS.True);
                }

                collection.UpdateOne(filter, updateChangeRequest);
            }
        }
        private void UpdatePreProcessMissingValues(dynamic data, JObject preProcessData, IMongoCollection<BsonDocument> collection, FilterDefinition<BsonDocument> filter, bool DBEncryptionRequired)
        {
            //Update, UI forwarded values into Collection            
            foreach (var missingValues in data.MissingValues)
            {
                var prop = missingValues as JProperty;
                if (prop != null)
                {
                    var missingValue = preProcessData[CONSTANTS.MissingValues][prop.Name];
                    if (missingValue != null)
                    {
                        foreach (var item in preProcessData[CONSTANTS.MissingValues][prop.Name].Children())
                        {
                            JProperty property = item as JProperty;
                            if (property.Name != CONSTANTS.None)
                            {
                                if (property != null && property.Value.ToString() == CONSTANTS.True)
                                {
                                    string updateField1 = string.Format(CONSTANTS.MissingValues01, prop.Name, property.Name);
                                    //var existfieldUpdate1 = Builders<BsonDocument>.Update.Set(updateField1, CONSTANTS.False);
                                    //var existResults1 = collection.UpdateOne(filter, existfieldUpdate1);
                                    preProcessData["MissingValues"][prop.Name][property.Name] = CONSTANTS.False;
                                }
                            }

                        }
                        string updateField2 = string.Format(CONSTANTS.MissingValues01, prop.Name, CONSTANTS.CustomValue);
                        //  var existfieldUpdate2 = Builders<BsonDocument>.Update.Set(updateField2, CONSTANTS.False);
                        // var existResults2 = collection.UpdateOne(filter, existfieldUpdate2);
                        preProcessData["MissingValues"][prop.Name][CONSTANTS.CustomValue] = CONSTANTS.False;
                        preProcessData["MissingValues"][prop.Name][CONSTANTS.CustomFlag] = CONSTANTS.False;
                    }
                    var jObject = JObject.Parse(prop.Value.ToString());
                    if (jObject != null)
                    {
                        //Update proper data for Custom Value coming from UI 
                        foreach (var value in jObject.Children())
                        {
                            if (value != null)
                            {
                                JProperty jProperty = value as JProperty;
                                var dataType = jProperty.Value.Type;
                                if (jProperty != null)
                                {
                                    if (jProperty.Name != CONSTANTS.None)
                                    {
                                        string updateField = string.Format(CONSTANTS.MissingValues01, missingValues.Name, jProperty.Name);

                                        if (Convert.ToString(dataType) == "Integer")
                                        {
                                            if (jProperty.Value.ToString().Length > 3)
                                            {
                                                //var existfieldUpdate = Builders<BsonDocument>.Update.Set(updateField, jProperty.Value.ToObject<double>());
                                                //var existResults = collection.UpdateOne(filter, existfieldUpdate);
                                                preProcessData["MissingValues"][missingValues.Name][jProperty.Name] = jProperty.Value.ToObject<double>();
                                            }
                                            else
                                            {
                                                // var existfieldUpdate = Builders<BsonDocument>.Update.Set(updateField, jProperty.Value.ToObject<Int32>());
                                                //var existResults = collection.UpdateOne(filter, existfieldUpdate);
                                                preProcessData["MissingValues"][missingValues.Name][jProperty.Name] = jProperty.Value.ToObject<Int32>();
                                            }
                                        }
                                        else
                                        {
                                            //var existfieldUpdate = Builders<BsonDocument>.Update.Set(updateField, jProperty.Value.ToObject<dynamic>());
                                            //var existResults = collection.UpdateOne(filter, existfieldUpdate);
                                            preProcessData["MissingValues"][missingValues.Name][jProperty.Name] = jProperty.Value.ToObject<dynamic>();
                                        }

                                        //string updateField3 = string.Format(CONSTANTS.MissingValues01, missingValues.Name, CONSTANTS.ChangeRequest);
                                        //var existfieldUpdate3 = Builders<BsonDocument>.Update.Set(updateField3, CONSTANTS.True);
                                        //var existResults3 = collection.UpdateOne(filter, existfieldUpdate3);
                                        string updateField3 = string.Format(CONSTANTS.MissingValues01, missingValues.Name, CONSTANTS.ChangeRequest);
                                        preProcessData["MissingValues"][missingValues.Name][CONSTANTS.ChangeRequest] = CONSTANTS.True;
                                    }
                                }
                            }
                        }
                    }
                }

            }
            //encrypt db data
            if (DBEncryptionRequired)
            {
                var MissingValuesUpdate = Builders<BsonDocument>.Update.Set(CONSTANTS.MissingValues, _encryptionDecryption.Encrypt(preProcessData[CONSTANTS.MissingValues].ToString(Formatting.None)));
                collection.UpdateOne(filter, MissingValuesUpdate);
            }
            else
            {
                var MissingValue = BsonDocument.Parse(preProcessData[CONSTANTS.MissingValues].ToString());
                var MissingValuesUpdate = Builders<BsonDocument>.Update.Set(CONSTANTS.MissingValues, MissingValue);
                collection.UpdateOne(filter, MissingValuesUpdate);
            }
        }
        private void UpdateFilterPreprocessValues(dynamic data, JObject preProcessData, IMongoCollection<BsonDocument> collection, FilterDefinition<BsonDocument> filter, bool DBEncryptionRequired)
        {
            //Update, UI forwarded Values into Collection
            foreach (var fileterValues in data.Filters)
            {
                var prop = fileterValues as JProperty;
                if (prop != null)
                {
                    foreach (JToken child in preProcessData[CONSTANTS.Filters][prop.Name].Children())
                    {
                        JProperty property = child as JProperty;
                        if (property != null && property.Value.ToString() == CONSTANTS.True)
                        {
                            //var existfieldUpdate = Builders<BsonDocument>.Update.Set(updateField, CONSTANTS.False);
                            //var existResults = collection.UpdateOne(filter, existfieldUpdate);

                            string updateField = string.Format(CONSTANTS.Filters01, fileterValues.Name, property.Name);
                            preProcessData[CONSTANTS.Filters][fileterValues.Name][property.Name] = CONSTANTS.False;
                        }
                    }
                    JArray jObject = JArray.Parse(prop.Value.ToString());
                    if (jObject != null)
                    {
                        foreach (JObject value in jObject.Children<JObject>())
                        {
                            foreach (JProperty jProperty in value.Properties())
                            {
                                string updateField = string.Format(CONSTANTS.Filters01, fileterValues.Name, jProperty.Name.ToString().Trim());
                                preProcessData[CONSTANTS.Filters][fileterValues.Name][jProperty.Name.ToString().Trim()] = jProperty.Value.ToString().Trim();
                                //var existfieldUpdate = Builders<BsonDocument>.Update.Set(updateField, jProperty.Value.ToString().Trim());
                                //var existResults = collection.UpdateOne(filter, existfieldUpdate);
                            }

                        }
                        string updateField2 = string.Format(CONSTANTS.Filters01, fileterValues.Name, CONSTANTS.ChangeRequest);
                        preProcessData[CONSTANTS.Filters][fileterValues.Name][CONSTANTS.ChangeRequest] = CONSTANTS.True;
                        //var existfieldUpdate2 = Builders<BsonDocument>.Update.Set(updateField2, CONSTANTS.True);
                        //var existResults2 = collection.UpdateOne(filter, existfieldUpdate2);
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

        public bool RemoveQueueRecords(string correlationId, string pageInfo)
        {
            bool removed = false;
            var builder = Builders<BsonDocument>.Filter;
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
            var filter = builder.Eq(CONSTANTS.CorrelationId, correlationId) & builder.Eq(CONSTANTS.pageInfo, pageInfo);
            var result = collection.DeleteMany(filter);
            if (result.DeletedCount > 0)
            {
                removed = true;
            }
            return removed;
        }
        public bool RemoveQueueRecords(string correlationId, string pageInfo, string WFId)
        {
            bool removed = false;
            var builder = Builders<BsonDocument>.Filter;
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIUseCase);
            var filter = builder.Eq(CONSTANTS.CorrelationId, correlationId) & builder.Eq(CONSTANTS.pageInfo, pageInfo) & builder.Eq(CONSTANTS.UniId, WFId);
            var result = collection.DeleteOne(filter);
            if (result.DeletedCount > 0)
            {
                removed = true;
            }
            return removed;
        }
        private List<BsonDocument> GetPreprocessExistData(string correlationId)
        {
            _preProcessDTO.CorrelationId = correlationId;
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var prePropcessProjection = Builders<BsonDocument>.Projection.Include(CONSTANTS.Filters).Include(CONSTANTS.MissingValues).Include(CONSTANTS.DataEncoding).Include(CONSTANTS.DataModification).Include(CONSTANTS.DataTransformationApplied).Include(CONSTANTS.TargetColumn).Include(CONSTANTS.Smote).Include(CONSTANTS.SmoteMulticlass).Exclude(CONSTANTS.Id);
            var dataPreprocessCollection = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataProcessing);
            var preprocessDataExist = dataPreprocessCollection.Find(filter).Project<BsonDocument>(prePropcessProjection).ToList();
            bool DBEncryptionRequired = CommonUtility.EncryptDB(correlationId, appSettings);
            if (preprocessDataExist.Count > 0)
            {
                //decrypt db data
                if (DBEncryptionRequired)
                {
                    JObject serializeData = new JObject();
                    serializeData = JObject.Parse(preprocessDataExist[0].ToString());
                    //var encrypt = _encryptionDecryption.Encrypt(serializeData[CONSTANTS.DataModification].ToString());//To be deleted
                    BsonDocument processDocument = preprocessDataExist[0];
                    preprocessDataExist[0][CONSTANTS.DataModification] = BsonDocument.Parse(_encryptionDecryption.Decrypt(preprocessDataExist[0][CONSTANTS.DataModification].AsString));
                    preprocessDataExist[0][CONSTANTS.MissingValues] = BsonDocument.Parse(_encryptionDecryption.Decrypt(preprocessDataExist[0][CONSTANTS.MissingValues].AsString));
                    preprocessDataExist[0][CONSTANTS.Filters] = BsonDocument.Parse(_encryptionDecryption.Decrypt(preprocessDataExist[0][CONSTANTS.Filters].AsString));
                    //if (!string.IsNullOrEmpty(Convert.ToString(preprocessDataExist[0][CONSTANTS.CreatedByUser])))
                    //    preprocessDataExist[0][CONSTANTS.CreatedByUser] = _encryptionDecryption.Decrypt(preprocessDataExist[0][CONSTANTS.CreatedByUser].AsString);
                    //if (!string.IsNullOrEmpty(Convert.ToString(preprocessDataExist[0][CONSTANTS.ModifiedByUser])))
                    //    preprocessDataExist[0][CONSTANTS.ModifiedByUser] = _encryptionDecryption.Decrypt(preprocessDataExist[0][CONSTANTS.ModifiedByUser].AsString);
                }
            }
            return preprocessDataExist;
        }
        public PreProcessModelDTO GetProcessingData(string correlationId)
        {
            try
            {
                bool DBEncryptionRequired = CommonUtility.EncryptDB(correlationId, appSettings);
                preProcessModel.CorrelationId = correlationId;
                preProcessModel.IsModelTrained = this.IsModelTrained(correlationId);
                bool correlationExist = false;
                var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
                var preprocessDataExist = GetPreprocessExistData(correlationId);
                string processData = string.Empty;

                var dataCleanupCollection = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataCleanup);
                var filterCollection = _database.GetCollection<BsonDocument>(CONSTANTS.DataCleanUPFilteredData);

                var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.FeatureName).Include(CONSTANTS.NewFeatureName).Exclude(CONSTANTS.Id);
                var filteredData = new List<BsonDocument>();
                List<string> columnsList = new List<string>();
                List<string> categoricalColumns = new List<string>();
                List<string> missingColumns = new List<string>();
                List<string> numericalColumns = new List<string>();
                List<string> autoBinningNumericalColumns = new List<string>();
                JObject serializeData = new JObject();
                List<string> dataSource = null;
                dataSource = CommonUtility.GetDataSourceModel(correlationId, appSettings);
                if (dataSource.Count > 0)
                {
                    preProcessModel.ModelName = dataSource[0];
                    preProcessModel.DataSource = dataSource[1];
                    preProcessModel.ModelType = dataSource[2];

                    if (dataSource.Count > 2)
                    {
                        preProcessModel.BusinessProblem = dataSource[3];
                    }
                    if (!string.IsNullOrEmpty(dataSource[4]))
                    {
                        preProcessModel.InstaFlag = true;
                        preProcessModel.Category = dataSource[5];
                    }
                }

                //Checking for MultiClass
                preProcessModel.IsMultiClass = this.IsMultiClass(correlationId);
                isMultiClass = preProcessModel.IsMultiClass;
                //Only for Binary Classfification(not MultiClass) autobinning is done.
                autoBinningEnabled = (preProcessModel.ModelType == CONSTANTS.Classification && !isMultiClass) ? true : false;

                filteredData = dataCleanupCollection.Find(filter).Project<BsonDocument>(projection).ToList();
                if (filteredData.Count > 0)
                {
                    if (DBEncryptionRequired)
                    {
                        filteredData[0][CONSTANTS.FeatureName] = BsonDocument.Parse(_encryptionDecryption.Decrypt(filteredData[0][CONSTANTS.FeatureName].AsString));
                        if (filteredData[0].Contains(CONSTANTS.NewFeatureName))
                        {
                            if (filteredData[0][CONSTANTS.NewFeatureName].ToString() != "{ }")
                                filteredData[0][CONSTANTS.NewFeatureName] = BsonDocument.Parse(_encryptionDecryption.Decrypt(filteredData[0][CONSTANTS.NewFeatureName].AsString));
                        }
                    }

                    JObject datas = JObject.Parse(filteredData[0].ToString());
                    JObject combinedFeatures = new JObject();
                    combinedFeatures = this.CombinedFeatures(datas);
                    if (combinedFeatures != null)
                        filteredData[0][CONSTANTS.FeatureName] = BsonDocument.Parse(combinedFeatures.ToString());

                    serializeData = JObject.Parse(filteredData[0].ToString());
                }

                if (preprocessDataExist.Count > 0)
                {
                    List<string> cols = new List<string>();
                    correlationExist = true;
                    preProcessModel.PreprocessedData = JObject.Parse(preprocessDataExist[0].ToString());
                    // New Feature NLP - Get Text Type Column List - added by Shreya 
                    preProcessModel.TextTypeColumnList = this.GetTextTypeColumns(correlationId, preProcessModel.PreprocessedData["TargetColumn"].ToString(), serializeData);
                    //var featureSelection = this.GetFeatureSelectionData(correlationId);
                    //if (featureSelection.Count > 0)
                    //{
                    //    preProcessModel.FeatureSelectionData = JObject.Parse(featureSelection[0].ToString());
                    //}
                    //Added for Add New Feature Binding
                    //preProcessModel.CleanedUpColumnList = this.GetCleanedUpColumns(correlationId);
                    //Added for handling Missing Values - NA Team issue
                    preProcessModel.FeatureDataTypes = this.GetAllFeatureFromDECleanUp(correlationId);
                    // Added for NLP Cluster 
                    preProcessModel.silhouette_graph = this.GetSilhouette_Graph(correlationId);

                    //Autobinning. For UI purpose only- Returning flag to disable categorical columns in Data Modification & Encoding on page load.
                    if (autoBinningEnabled && preProcessModel.PreprocessedData[CONSTANTS.DataModification][CONSTANTS.AutoBinning] != null)
                    {
                        //filteredData = dataCleanupCollection.Find(filter).Project<BsonDocument>(projection).ToList();
                        preProcessModel.AutoBinningNumericalColumns = this.GetNumericalColumns(filteredData, serializeData); //Need to remove oonce numerical binning implemented in future

                        preProcessModel.AutoBinningDisableColumns = new Dictionary<string, string>();
                        foreach (var i in preProcessModel.PreprocessedData[CONSTANTS.DataModification][CONSTANTS.AutoBinning].Children())
                        {
                            JProperty prop = i as JProperty;
                            if (prop != null && prop.Value.ToString() == CONSTANTS.True)
                                preProcessModel.AutoBinningDisableColumns.Add(prop.Name, CONSTANTS.True);
                        }
                    }
                }
                else
                {
                    preProcessModel.AutoBinningNumericalColumns = new Dictionary<string, string>();
                    //filteredData = dataCleanupCollection.Find(filter).Project<BsonDocument>(projection).ToList();
                    if (filteredData.Count > 0)
                    {
                        //decrypt db data
                        //if (DBEncryptionRequired)
                        //    filteredData[0][CONSTANTS.FeatureName] = BsonDocument.Parse(_encryptionDecryption.Decrypt(filteredData[0][CONSTANTS.FeatureName].AsString));
                        //serializeData = JObject.Parse(filteredData[0].ToString());
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
                                if (property != null && property.Name == CONSTANTS.category && property.Value.ToString() == CONSTANTS.True)
                                {
                                    categoricalColumns.Add(item);
                                    if (value > 0)
                                        missingColumns.Add(item);
                                }
                                if (property != null && (property.Name == CONSTANTS.float64 || property.Name == CONSTANTS.int64) && property.Value.ToString() == CONSTANTS.True)
                                {
                                    if (value > 0)
                                        numericalColumns.Add(item);

                                    if (autoBinningEnabled)
                                    {
                                        autoBinningNumericalColumns.Add(item);
                                        preProcessModel.AutoBinningNumericalColumns.Add(item, CONSTANTS.False); //AutoBinning Numerical Columns                                    
                                    }
                                }
                            }
                        }
                    }
                }

                if (correlationExist)
                {
                    return preProcessModel;
                }
                else
                {
                    //Get DataModificationData
                    //GetModifications(correlationId, categoricalColumns, autoBinningNumericalColumns);
                    GetModifications(filteredData, categoricalColumns, autoBinningNumericalColumns, serializeData);

                    //Getting the Data Encoding Data
                    GetDataEncodingValues(categoricalColumns, serializeData);

                    //This code for filters to be applied
                    var uniqueValueProjection = Builders<BsonDocument>.Projection.Include(CONSTANTS.ColumnUniqueValues).Include(CONSTANTS.target_variable).Exclude(CONSTANTS.Id);
                    var filteredResult = filterCollection.Find(filter).Project<BsonDocument>(uniqueValueProjection).ToList();
                    JObject uniqueData = new JObject();
                    if (filteredResult.Count > 0)
                    {
                        if (DBEncryptionRequired)
                            filteredResult[0][CONSTANTS.ColumnUniqueValues] = BsonDocument.Parse(_encryptionDecryption.Decrypt(filteredResult[0][CONSTANTS.ColumnUniqueValues].AsString));

                        _preProcessDTO.TargetColumn = filteredResult[0][CONSTANTS.target_variable].ToString();
                        try
                        {
                            uniqueData = JObject.Parse(filteredResult[0].ToString());
                        }
                        catch
                        {
                            object Final = JsonConvert.SerializeObject(BsonTypeMapper.MapToDotNetValue(filteredResult[0]));
                            uniqueData = JsonConvert.DeserializeObject(Final.ToString()) as dynamic;
                        }



                        //Getting the Missing Values and Filters Data
                        GetMissingAndFiltersData(missingColumns, categoricalColumns, numericalColumns, uniqueData);
                        InsertToPreprocess(correlationId);
                    }
                }

                ////In Data Transformation - Length of no. of unique values is less than 2, UI will throw an error and disable the next button. Message is fetched from DB
                var collection = _database.GetCollection<BsonDocument>(CONSTANTS.MEFeatureSelection);
                var filterFeatureSelection = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
                var featureProjection = Builders<BsonDocument>.Projection.Include(CONSTANTS.UniqueTarget_message).Exclude(CONSTANTS.Id);
                var outcome = collection.Find(filterFeatureSelection).Project<BsonDocument>(featureProjection).ToList();
                if (outcome.Count > 0)
                {
                    preProcessModel.UniqueTargetMessage = outcome[0].Count() > 0 ? Convert.ToString(outcome[0][CONSTANTS.UniqueTarget_message]) : null;
                }
                // New Feature NLP- Get Text Type Column List - added by Shreya
                preProcessModel.TextTypeColumnList = this.GetTextTypeColumns(correlationId, _preProcessDTO.TargetColumn, serializeData);
                //var featureSelectionData = this.GetFeatureSelectionData(correlationId);
                //if (featureSelectionData.Count > 0)
                //{
                //    preProcessModel.FeatureSelectionData = JObject.Parse(featureSelectionData[0].ToString());
                //}
                //Added for Add New Feature binding
                //preProcessModel.CleanedUpColumnList = this.GetCleanedUpColumns(correlationId);
                //Added for handling Missing Values - NA Team issue
                preProcessModel.FeatureDataTypes = this.GetAllFeatureFromDECleanUp(correlationId);
                return preProcessModel;
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ProcessDataService), "CorrelationID - " + correlationId, ex.Message, ex, "FeatureName: " + appSettings.Value.aesKey, "NewFeatureName: " + appSettings.Value.aesVector, string.Empty, string.Empty); ;
                return preProcessModel;
            }
        }

        private Dictionary<string, string> GetNumericalColumns(List<BsonDocument> filteredData, JObject serializeData)
        {
            Dictionary<string, string> numericalColumns = new Dictionary<string, string>();
            if (filteredData.Count > 0)
            {
                //JObject serializeData = new JObject();
                List<string> columnsList = new List<string>();


                //decrypt db data
                //if (DBEncryptionRequired)
                //    filteredData[0][CONSTANTS.FeatureName] = BsonDocument.Parse(_encryptionDecryption.Decrypt(filteredData[0][CONSTANTS.FeatureName].AsString));

                //serializeData = JObject.Parse(filteredData[0].ToString());
                //Taking all the Columns
                foreach (var features in serializeData[CONSTANTS.FeatureName].Children())
                {
                    JProperty j = features as JProperty;
                    columnsList.Add(j.Name);
                }
                //Get the Numerical Columns
                foreach (var item in columnsList)
                {
                    foreach (JToken attributes in serializeData[CONSTANTS.FeatureName][item][CONSTANTS.Datatype].Children())
                    {
                        var property = attributes as JProperty;
                        var missingValues = serializeData[CONSTANTS.FeatureName][item][CONSTANTS.Missing_Values];
                        double value = (double)missingValues;
                        if (property != null && (property.Name == CONSTANTS.float64 || property.Name == CONSTANTS.int64) && property.Value.ToString() == CONSTANTS.True)
                        {
                            numericalColumns.Add(item, CONSTANTS.False);
                        }
                    }
                }
            }

            return numericalColumns;
        }

        private void GetModifications(List<BsonDocument> filteredData, List<string> categoricalColumns, List<string> numericalColumns, JObject serializeData)
        {
            List<string> binningcolumnsList = new List<string>();
            List<string> recommendedcolumnsList = new List<string>();
            List<string> columnsList = new List<string>();
            Dictionary<string, Dictionary<string, Dictionary<string, string>>> recommendedColumns = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();

            Dictionary<string, Dictionary<string, Dictionary<string, string>>> columnBinning = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();
            Dictionary<string, string> prescriptionData = new Dictionary<string, string>();
            //JObject serializeData = new JObject();

            //bool DBEncryptionRequired = CommonUtility.EncryptDB(correlationId, appSettings);

            //var dataCleanupCollection = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataCleanup);
            //var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            //var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.FeatureName).Exclude(CONSTANTS.Id);
            //var filteredData = dataCleanupCollection.Find(filter).Project<BsonDocument>(projection).ToList();

            if (filteredData.Count > 0)
            {
                //decrypt db data
                //if (DBEncryptionRequired)
                //    filteredData[0][CONSTANTS.FeatureName] = BsonDocument.Parse(_encryptionDecryption.Decrypt(filteredData[0][CONSTANTS.FeatureName].AsString));

                //serializeData = JObject.Parse(filteredData[0].ToString());
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
                        var skewData = serializeData[CONSTANTS.FeatureName][item][CONSTANTS.Skewness];
                        float outValue = (float)outData;
                        string skewValue = (string)skewData;
                        var imbalanced = serializeData[CONSTANTS.FeatureName][item][CONSTANTS.ImBalanced];
                        string imbalancedValue = (string)imbalanced;
                        if (imbalancedValue == CONSTANTS.One)
                        {
                            JProperty jProperty1 = null;
                            string recommendation = string.Format(CONSTANTS.Binning_Message, item);
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
                                        dict.Add(CONSTANTS.ChangeRequest, CONSTANTS.InvertedComma);
                                        binningColumns2.Add(CONSTANTS.ChangeRequest, dict);
                                    }
                                    else
                                    {
                                        dict.Add(CONSTANTS.PChangeRequest, CONSTANTS.InvertedComma);
                                        binningColumns2.Add(CONSTANTS.PChangeRequest, dict);
                                    }
                                }
                            }
                            columnBinning.Add(item, binningColumns2);
                        }
                        else if (imbalancedValue == CONSTANTS.Two)
                        {
                            string removeColumndesc = string.Format(CONSTANTS.column_Message, item);
                            removeImbalancedColumns.Add(item, removeColumndesc);
                        }
                        else if (imbalancedValue == CONSTANTS.Three)
                        {
                            string prescription = string.Format(CONSTANTS.Target_Message, item);
                            prescriptionData.Add(item, prescription);
                        }
                        if (prescriptionData.Count > 0)
                            _preProcessDTO.Prescriptions = prescriptionData;

                        if (outValue > 0)
                        {
                            string strForm = string.Format(CONSTANTS.Columns_Message, item, outValue);
                            outlier.Add(CONSTANTS.Text, strForm);
                            string[] outliers = { CONSTANTS.Mean, CONSTANTS.Median, CONSTANTS.Mode, CONSTANTS.Custom_Value, CONSTANTS.ChangeRequest, CONSTANTS.PChangeRequest };
                            for (int i = 0; i < outliers.Length; i++)
                            {
                                if (i == 3)
                                {
                                    outlier.Add(outliers[i], CONSTANTS.InvertedComma);
                                }
                                else if (i == 4 || i == 5)
                                {
                                    outlier.Add(outliers[i], CONSTANTS.InvertedComma);
                                }
                                else
                                {
                                    outlier.Add(outliers[i], CONSTANTS.False);
                                }
                            }
                        }
                        if (skewValue == CONSTANTS.Yes)
                        {
                            string strForm = string.Format(CONSTANTS.StringFormat3, item);
                            skeweness.Add(CONSTANTS.Skeweness, strForm);
                            string[] skewnessArray = { CONSTANTS.BoxCox, CONSTANTS.Reciprocal, CONSTANTS.Log, CONSTANTS.ChangeRequest, CONSTANTS.PChangeRequest };
                            for (int i = 0; i < skewnessArray.Length; i++)
                            {
                                if (i == 3 || i == 4)
                                {
                                    skeweness.Add(skewnessArray[i], CONSTANTS.InvertedComma);
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
                            fields.Add(CONSTANTS.Skewness, skeweness);
                        if (removeImbalancedColumns.Count > 0)
                            fields.Add(CONSTANTS.RemoveColumn, removeImbalancedColumns);

                        if (fields.Count > 0)
                        {
                            recommendedColumns.Add(item, fields);
                        }
                    }

                    //AutoBinning
                    if (autoBinningEnabled)
                    {
                        List<string> autoBinningColumns = new List<string>();
                        Dictionary<string, string> autoBinning = new Dictionary<string, string>();
                        autoBinningColumns = categoricalColumns.Concat(numericalColumns).ToList();
                        autoBinningColumns.ForEach(column => autoBinning.Add(column, CONSTANTS.False));

                        if (autoBinningColumns.Count > 0)
                            _preProcessDTO.AutoBinning = autoBinning;
                    }
                }
                if (columnBinning.Count > 0)
                    _preProcessDTO.ColumnBinning = columnBinning;
                if (recommendedColumns.Count > 0)
                    _preProcessDTO.RecommendedColumns = recommendedColumns;
            }
        }

        /// <summary>
        /// Insert the Data into Preproces Collection
        /// </summary>
        private void InsertToPreprocess(string correlationId)
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
                JObject autoBinningData = new JObject();

                //DataModification Insertion Format Start
                var recommendedColumnsData = JsonConvert.SerializeObject(_preProcessDTO.RecommendedColumns);
                if (!string.IsNullOrEmpty(recommendedColumnsData) && recommendedColumnsData != CONSTANTS.Null)
                    outlierData = JObject.Parse(recommendedColumnsData);
                var columnBinning = JsonConvert.SerializeObject(_preProcessDTO.ColumnBinning);
                if (!string.IsNullOrEmpty(columnBinning) && columnBinning != CONSTANTS.Null)
                    binningData = JObject.Parse(columnBinning);
                JObject binningObject = new JObject();
                if (binningData != null)
                    binningObject[CONSTANTS.ColumnBinning] = JObject.FromObject(binningData);

                var prescription = JsonConvert.SerializeObject(_preProcessDTO.Prescriptions);
                if (!string.IsNullOrEmpty(prescription) && prescription != CONSTANTS.Null)
                    prescriptionData = JObject.Parse(prescription);

                //AutoBinning     
                //Removing Target column 
                if (_preProcessDTO.AutoBinning != null && _preProcessDTO.AutoBinning.Count > 0)
                    _preProcessDTO.AutoBinning.Remove(_preProcessDTO.TargetColumn);

                var autoBinning = JsonConvert.SerializeObject(_preProcessDTO.AutoBinning);
                if (!string.IsNullOrEmpty(autoBinning) && autoBinning != CONSTANTS.Null)
                    autoBinningData = JObject.Parse(autoBinning);

                //DataModification Insertion Format End

                categoricalJson = JsonConvert.SerializeObject(_preProcessDTO.CategoricalData);
                missingValuesJson = JsonConvert.SerializeObject(_preProcessDTO.MisingValuesData);
                numericJson = JsonConvert.SerializeObject(_preProcessDTO.NumericalData);
                dataEncodingJson = JsonConvert.SerializeObject(_preProcessDTO.DataEncodeData);

                JObject missingValuesObject = new JObject();
                JObject categoricalObject = new JObject();
                JObject numericObject = new JObject();
                JObject encodedData = new JObject();
                if (!string.IsNullOrEmpty(categoricalJson) && categoricalJson != CONSTANTS.Null)
                    categoricalObject = JObject.Parse(categoricalJson);
                if (!string.IsNullOrEmpty(numericJson) && numericJson != CONSTANTS.Null)
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
                if (isMultiClass)
                    smoteFlags.Add(CONSTANTS.UserConsent, CONSTANTS.False);
                else
                    smoteFlags.Add(CONSTANTS.Flag, CONSTANTS.False);

                smoteFlags.Add(CONSTANTS.ChangeRequest, CONSTANTS.False);
                smoteFlags.Add(CONSTANTS.PChangeRequest, CONSTANTS.False);

                var smoteTest = JsonConvert.SerializeObject(smoteFlags);
                JObject smoteData = new JObject();
                smoteData = JObject.Parse(smoteTest);

                JObject processData = new JObject
                {
                    [CONSTANTS.Id] = Guid.NewGuid(),
                    [CONSTANTS.CorrelationId] = _preProcessDTO.CorrelationId
                };
                if (!string.IsNullOrEmpty(_preProcessDTO.Flag))
                    _preProcessDTO.Flag = CONSTANTS.False;
                processData[CONSTANTS.Flag] = _preProcessDTO.Flag;
                //Removing the Target column having lessthan 2 values..important
                bool removeTargetColumn = false;
                if (categoricalObject != null && categoricalObject.ToString() != CONSTANTS.CurlyBraces)
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

                //Retaining Filter for Public Templates
                var businessProblemFilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
                var businessProblemProjection = Builders<BsonDocument>.Projection.Include(CONSTANTS.ParentCorrelationId).Include(CONSTANTS.IsDataTransformationRetained).Include(CONSTANTS.InputColumns).Include(CONSTANTS.TargetColumn).Exclude(CONSTANTS.Id);
                var businessProblemCollection = _database.GetCollection<BsonDocument>(CONSTANTS.PSBusinessProblem);
                var businessProblemData = businessProblemCollection.Find(businessProblemFilter).Project<BsonDocument>(businessProblemProjection).FirstOrDefault();


                //JObject newAddFeatures = new JObject();
                //bool isFeatureAdded = false;
                if (businessProblemData != null)
                {
                    if (businessProblemData.Contains("ParentCorrelationId") && !businessProblemData["ParentCorrelationId"].IsBsonNull)
                    {
                        string parentCorrelationId = Convert.ToString(businessProblemData["ParentCorrelationId"]);
                        if (!string.IsNullOrEmpty(parentCorrelationId) && businessProblemData[CONSTANTS.IsDataTransformationRetained] == false)
                        {
                            var deployModelFilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, parentCorrelationId) & Builders<BsonDocument>.Filter.Eq(CONSTANTS.IsPrivate, false) & Builders<BsonDocument>.Filter.Eq(CONSTANTS.IsModelTemplate, true) & Builders<BsonDocument>.Filter.Eq(CONSTANTS.Status, CONSTANTS.Deployed);
                            var data = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_DeployedModels).Find(deployModelFilter).ToList();

                            if (data.Count > 0)
                            {
                                var p_Filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, parentCorrelationId);
                                var p_Projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.Filters).Include(CONSTANTS.DataModification);
                                var retainData = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataProcessing).Find(p_Filter).Project<BsonDocument>(p_Projection).ToList();

                                if (retainData.Count > 0)
                                {
                                    bool isDBEncryptionRequired = CommonUtility.EncryptDB(parentCorrelationId, appSettings);

                                    if (isDBEncryptionRequired)
                                    {
                                        BsonDocument processDocument = retainData[0];
                                        retainData[0][CONSTANTS.Filters] = BsonDocument.Parse(_encryptionDecryption.Decrypt(retainData[0][CONSTANTS.Filters].AsString));
                                        retainData[0][CONSTANTS.DataModification] = BsonDocument.Parse(_encryptionDecryption.Decrypt(retainData[0][CONSTANTS.DataModification].AsString));
                                        //if (!string.IsNullOrEmpty(Convert.ToString(retainData[0][CONSTANTS.CreatedByUser])))
                                        //    retainData[0][CONSTANTS.CreatedByUser] = _encryptionDecryption.Decrypt(retainData[0][CONSTANTS.CreatedByUser].AsString);
                                        //if (!string.IsNullOrEmpty(Convert.ToString(retainData[0][CONSTANTS.ModifiedByUser])))
                                        //    retainData[0][CONSTANTS.ModifiedByUser] = _encryptionDecryption.Decrypt(retainData[0][CONSTANTS.ModifiedByUser].AsString);
                                    }

                                    //Retaining Filter data
                                    JObject parentFilterData = JObject.Parse(retainData[0][CONSTANTS.Filters].ToString());
                                    if (categoricalObject != null && categoricalObject.ToString() != CONSTANTS.CurlyBraces)
                                        categoricalObject = this.RetainFilterData(categoricalObject, parentFilterData);


                                    //Retaining Add feature data
                                    //JObject parentFeatureData = JObject.Parse(retainData[0][CONSTANTS.DataModification].ToString());
                                    //newAddFeatures = this.RetainFeatureData(businessProblemData, parentFeatureData);
                                    //isFeatureAdded = true;

                                    //IsDataTransformationRetained is set to true once retained filter & feature values
                                    var updateBusinessProblem = Builders<BsonDocument>.Update.Set(CONSTANTS.IsDataTransformationRetained, true);
                                    businessProblemCollection.UpdateMany(businessProblemFilter, updateBusinessProblem);
                                }
                            }
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
                //if (isFeatureAdded && newAddFeatures != null && newAddFeatures.HasValues)
                //    processData[CONSTANTS.DataModification][CONSTANTS.NewAddFeatures] = JObject.FromObject(newAddFeatures);
                if (autoBinningEnabled && autoBinningData != null && autoBinningData.ToString() != CONSTANTS.CurlyBraces)
                    processData[CONSTANTS.DataModification][CONSTANTS.AutoBinning] = (autoBinningData);

                processData[CONSTANTS.TargetColumn] = _preProcessDTO.TargetColumn;
                JObject InterpolationObject = new JObject();
                processData[CONSTANTS.DataModification][CONSTANTS.Features][CONSTANTS.Interpolation] = JObject.FromObject(InterpolationObject);
                //processData[CONSTANTS.Smote] = smoteData;
                if (isMultiClass)
                    processData[CONSTANTS.SmoteMulticlass] = smoteData;
                else
                    processData[CONSTANTS.Smote] = smoteData;
                processData[CONSTANTS.DataTransformationApplied] = _preProcessDTO.DataTransformationApplied;
                processData[CONSTANTS.CreatedOn] = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                processData[CONSTANTS.ModifiedOn] = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);

                JObject PreprocessedData = new JObject
                {
                    ["Filters"] = JObject.FromObject(categoricalObject),
                    ["MissingValues"] = JObject.FromObject(missingValuesObject),
                    ["DataEncoding"] = JObject.FromObject(encodedData),
                    ["DataModification"] = processData[CONSTANTS.DataModification],
                    ["TargetColumn"] = _preProcessDTO.TargetColumn,
                    ["DataTransformationApplied"] = _preProcessDTO.DataTransformationApplied,
                    //["Smote"] = processData[CONSTANTS.Smote],                    
                };

                if (isMultiClass)
                    PreprocessedData["SmoteMulticlass"] = processData[CONSTANTS.SmoteMulticlass];
                else
                    PreprocessedData["Smote"] = processData[CONSTANTS.Smote];

                preProcessModel.PreprocessedData = PreprocessedData;
                bool DBEncryptionRequired = CommonUtility.EncryptDB(_preProcessDTO.CorrelationId, appSettings);
                //encrypt db data
                if (DBEncryptionRequired)
                {
                    processData[CONSTANTS.DataModification] = _encryptionDecryption.Encrypt(processData[CONSTANTS.DataModification].ToString(Formatting.None));
                    processData[CONSTANTS.MissingValues] = _encryptionDecryption.Encrypt(processData[CONSTANTS.MissingValues].ToString(Formatting.None));
                    processData[CONSTANTS.Filters] = _encryptionDecryption.Encrypt(processData[CONSTANTS.Filters].ToString(Formatting.None));
                    //if (processData.ContainsKey(CONSTANTS.CreatedByUser) && !string.IsNullOrEmpty(Convert.ToString(processData[CONSTANTS.CreatedByUser])))
                    //    processData[CONSTANTS.CreatedByUser] = _encryptionDecryption.Encrypt(Convert.ToString(processData[CONSTANTS.CreatedByUser]));
                    //if (processData.ContainsKey(CONSTANTS.ModifiedByUser) && !string.IsNullOrEmpty(Convert.ToString(processData[CONSTANTS.ModifiedByUser])))
                    //    processData[CONSTANTS.ModifiedByUser] = _encryptionDecryption.Encrypt(Convert.ToString(processData[CONSTANTS.ModifiedByUser]));
                }
                var collection = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataProcessing);
                var insertdoc = BsonSerializer.Deserialize<BsonDocument>(processData.ToString());
                collection.InsertOne(insertdoc);
            }
        }

        private JObject RetainFilterData(JObject categoricalObject, JObject parentFilterData)
        {
            JObject childFilterData = categoricalObject;
            List<string> lstParentMainFilter = new List<string>();
            List<string> lstChildMainFilter = new List<string>();
            if (parentFilterData != null && childFilterData != null)
            {
                //Parent record: Adding the name of main filter attributes to list. For Eg: Filter --> City,Country,State
                foreach (var parent in parentFilterData.Children())
                {
                    JProperty prop = parent as JProperty;
                    lstParentMainFilter.Add(prop.Name);
                }
                //Child record: Adding the name of main filter attributes to list. For Eg: Filter --> City,Country,State
                foreach (var child in childFilterData.Children())
                {
                    JProperty prop = child as JProperty;
                    lstChildMainFilter.Add(prop.Name);
                }

                var matchedField = lstChildMainFilter.Where(x => lstParentMainFilter.Contains(x)).ToList();

                foreach (var itemChild in matchedField)
                {
                    foreach (var itemChildData in categoricalObject[itemChild].Children())
                    {
                        JProperty propChild = itemChildData as JProperty;
                        foreach (var itemParentData in parentFilterData[itemChild].Children())
                        {
                            JProperty propParent = itemParentData as JProperty;
                            if (propChild.Name.Equals(propParent.Name))
                            {
                                propChild.Value = propParent.Value;
                                break;
                            }
                        }
                    }
                }
            }

            return categoricalObject;
        }

        /// <summary>
        /// Get the Missing Values and Filter Values
        /// </summary>
        /// <param name="categoricalColumns"></param>
        /// <param name="numericalColumns"></param>
        /// <param name="uniqueData"></param>
        /// <param name="categoricalData"></param>
        /// <param name="numericalData"></param>
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
                        fieldDictionary.Add(value.ToString().Replace(CONSTANTS.Dot, CONSTANTS.u2024).Replace(CONSTANTS.r_n, CONSTANTS.EmptySpace).Replace(CONSTANTS.slash, CONSTANTS.InvertedComma).Replace(CONSTANTS.t, CONSTANTS.EmptySpace), CONSTANTS.False);
                }
                if (fieldDictionary.Count > 0)
                {
                    fieldDictionary.Add(CONSTANTS.ChangeRequest, CONSTANTS.InvertedComma);
                    fieldDictionary.Add(CONSTANTS.PChangeRequest, CONSTANTS.InvertedComma);
                    categoricalDictionary.Add(column, fieldDictionary);
                }
            }
            _preProcessDTO.CategoricalData = categoricalDictionary;

            foreach (var column in missingColumns)
            {
                Dictionary<string, string> fieldDictionary = new Dictionary<string, string>();
                foreach (JToken value in uniqueData[CONSTANTS.ColumnUniqueValues][column].Children())
                {
                    if (!string.IsNullOrEmpty(Convert.ToString(value)))
                        fieldDictionary.Add(value.ToString().Replace(CONSTANTS.Dot, CONSTANTS.u2024).Replace(CONSTANTS.r_n, CONSTANTS.EmptySpace).Replace(CONSTANTS.slash, CONSTANTS.InvertedComma).Replace(CONSTANTS.t, CONSTANTS.EmptySpace), CONSTANTS.False);

                }
                if (fieldDictionary.Count > 0)
                {
                    fieldDictionary.Add(CONSTANTS.ChangeRequest, CONSTANTS.False);
                    fieldDictionary.Add(CONSTANTS.PChangeRequest, CONSTANTS.InvertedComma);
                    fieldDictionary.Add(CONSTANTS.CustomValue, CONSTANTS.InvertedComma);
                    fieldDictionary.Add(CONSTANTS.CustomFlag, CONSTANTS.InvertedComma);
                    if (!fieldDictionary.ContainsKey(CONSTANTS.None))
                        fieldDictionary.Add(CONSTANTS.None, CONSTANTS.InvertedComma);
                    missingData.Add(column, fieldDictionary);
                }
            }
            _preProcessDTO.MisingValuesData = missingData;

            //Numerical Columns Fetching data

            Dictionary<string, string> numericalDictionary = new Dictionary<string, string>();
            string[] numericalValues = { CONSTANTS.Mean, CONSTANTS.Median, CONSTANTS.Mode, CONSTANTS.CustomValue, CONSTANTS.CustomFlag };
            foreach (var column in numericalColumns)
            {
                var value = uniqueData[CONSTANTS.ColumnUniqueValues][column];
                var numericDictionary = new Dictionary<string, string>();
                if (!string.IsNullOrEmpty(Convert.ToString(value)))
                {
                    foreach (var numericColumnn in numericalValues)
                    {
                        if (numericColumnn == CONSTANTS.CustomValue || numericColumnn == CONSTANTS.CustomFlag)
                        {
                            numericDictionary.Add(numericColumnn, CONSTANTS.InvertedComma);
                        }
                        else
                        {
                            numericDictionary.Add(numericColumnn, CONSTANTS.False);
                        }
                    }
                    if (numericDictionary.Count > 0)
                    {
                        numericDictionary.Add(CONSTANTS.ChangeRequest, CONSTANTS.False);
                        numericDictionary.Add(CONSTANTS.PChangeRequest, CONSTANTS.InvertedComma);
                        if (!numericalDictionary.ContainsKey(CONSTANTS.None))
                            numericDictionary.Add(CONSTANTS.None, CONSTANTS.InvertedComma);
                        dataNumerical.Add(column, numericDictionary);
                    }
                }
            }
            _preProcessDTO.NumericalData = dataNumerical;
        }

        /// <summary>
        /// Get preprocess Data Encode Values
        /// </summary>
        /// <param name="categoricalColumns"></param>
        /// <param name="serializeData"></param>
        /// <param name="encodedData"></param>
        private void GetDataEncodingValues(List<string> categoricalColumns, JObject serializeData)
        {
            var encodingData = new Dictionary<string, Dictionary<string, string>>();
            foreach (var column in categoricalColumns)
            {
                var dataEncodingData = new Dictionary<string, string>();
                foreach (JToken scale in serializeData[CONSTANTS.FeatureName][column][CONSTANTS.Scale].Children())
                {
                    if (scale is JProperty property && property.Value.ToString() == CONSTANTS.True)
                    {
                        dataEncodingData.Add(CONSTANTS.Attribute, property.Name);
                        dataEncodingData.Add(CONSTANTS.encoding, CONSTANTS.InvertedComma);
                    }
                }
                if (dataEncodingData.Count > 0)
                {
                    dataEncodingData.Add(CONSTANTS.ChangeRequest, CONSTANTS.InvertedComma);
                    dataEncodingData.Add(CONSTANTS.PChangeRequest, CONSTANTS.InvertedComma);
                    encodingData.Add(column, dataEncodingData);
                }

            }
            _preProcessDTO.DataEncodeData = encodingData;
        }

        public void SmoteTechnique(string correlationId)
        {
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataProcessing);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var builder = Builders<BsonDocument>.Update;
            var update = builder.Set(CONSTANTS.Smote_Flag, CONSTANTS.True).Set(CONSTANTS.Smote_ChangeRequest, CONSTANTS.True);
            var updateResult = collection.UpdateMany(filter, update);
        }


        /// <summary>
        /// Get Columns of type Text for NLP
        /// </summary>
        /// <param name="correlationId">Correlation Id</param>
        /// <param name="targetColumn">Target Column</param>
        /// <returns>Columns of Type Text</returns>
        private List<string> GetTextTypeColumns(string correlationId, string targetColumn, JObject serializeData)
        {
            //JObject serializeData = new JObject();
            List<string> existColumnsList = new List<string>();
            List<string> newColumnsList = new List<string>();
            List<string> textTypeColumnList = new List<string>();

            //Taking all the Feature Columns
            if (serializeData != null)
            {
                var featureExist = serializeData["Feature Name"];
                if (featureExist != null)
                {
                    foreach (var features in serializeData["Feature Name"].Children())
                    {
                        JProperty j = features as JProperty;
                        existColumnsList.Add(j.Name);
                    }
                    foreach (var item in existColumnsList)
                    {
                        if (item != targetColumn)
                        {
                            var textColumnType = serializeData["Feature Name"][item]["Datatype"];
                            if ((Convert.ToString(textColumnType["Text"])) == "True")
                            {
                                textTypeColumnList.Add(item);
                            }
                        }
                    }
                }

                //Taking Add Features Text  columns
                if (serializeData[CONSTANTS.NewFeatureName] != null)
                {
                    foreach (var features in serializeData[CONSTANTS.NewFeatureName].Children())
                    {
                        JProperty j = features as JProperty;
                        newColumnsList.Add(j.Name);
                    }
                    foreach (var item in newColumnsList)
                    {
                        if (item != targetColumn)
                        {
                            var textColumnType = serializeData[CONSTANTS.NewFeatureName][item][CONSTANTS.Datatype];
                            if ((Convert.ToString(textColumnType[CONSTANTS.Text])) == CONSTANTS.True)
                            {
                                textTypeColumnList.Add(item);
                            }
                        }
                    }
                }
            }
            return textTypeColumnList;
        }

        private List<BsonDocument> GetFeatureSelectionData(string correlationId)
        {
            _preProcessDTO.CorrelationId = correlationId;
            var filter = Builders<BsonDocument>.Filter.Eq("CorrelationId", correlationId);
            var featureSelectionProjection = Builders<BsonDocument>.Projection.Include("Feature_Not_Created").Include("Features_Created").Include("Map_Encode_New_Feature").Exclude("_id");
            var FeatureSelectionCollection = _database.GetCollection<BsonDocument>("ME_FeatureSelection");
            var featureSelectionData = FeatureSelectionCollection.Find(filter).Project<BsonDocument>(featureSelectionProjection).ToList();
            return featureSelectionData;
        }


        /// <summary>
        /// Get Cleaned Up Columns List
        /// </summary>
        /// <param name="correlationId">Correlation Id</param>
        /// <param name="targetColumn">Target Column</param>
        /// <returns>Columns of Type Text</returns>
        private List<string> GetCleanedUpColumns(string correlationId)
        {
            JObject serializeData = new JObject();
            List<string> columnsList = new List<string>();
            var dataCleanupCollection = _database.GetCollection<BsonDocument>(CONSTANTS.DE_DataCleanup);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.FeatureName).Exclude(CONSTANTS.Id);
            var filteredData = dataCleanupCollection.Find(filter).Project<BsonDocument>(projection).ToList();
            bool DBEncryptionRequired = CommonUtility.EncryptDB(correlationId, appSettings);
            if (filteredData.Count > 0)
            {
                //decrypt db data
                if (DBEncryptionRequired)
                {
                    filteredData[0][CONSTANTS.FeatureName] = BsonDocument.Parse(_encryptionDecryption.Decrypt(filteredData[0][CONSTANTS.FeatureName].AsString));
                    //if (!string.IsNullOrEmpty(Convert.ToString(filteredData[0][CONSTANTS.CreatedByUser])))
                    //{
                    //    filteredData[0][CONSTANTS.CreatedByUser] = _encryptionDecryption.Decrypt(filteredData[0][CONSTANTS.CreatedByUser].AsString);
                    //}
                    //if (!string.IsNullOrEmpty(Convert.ToString(filteredData[0][CONSTANTS.ModifiedByUser])))
                    //{
                    //    filteredData[0][CONSTANTS.ModifiedByUser] = _encryptionDecryption.Decrypt(filteredData[0][CONSTANTS.ModifiedByUser].AsString);
                    //}
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
                }
            }

            return columnsList;
        }

        /// <summary>
        /// Get All Feature Data Type for CleanUp Add Feature Bind and Remove Added Features
        /// </summary>
        /// <param name="correlationId"></param>
        /// <returns></returns>
        private JObject GetCleanUpFeatureDataType(string correlationId, JObject featureCreated)
        {
            JObject FeatureType = new JObject();
            var dataCleanupCollection = _database.GetCollection<BsonDocument>(CONSTANTS.DataCleanUP_FilteredData);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.types).Exclude(CONSTANTS.Id);
            var filteredData = dataCleanupCollection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (filteredData.Count > 0)
            {
                FeatureType = JObject.Parse(filteredData[0][CONSTANTS.types].ToString());
                // Remove  created Feaature columns
                if (featureCreated != null)
                {
                    foreach (var item in featureCreated.Children())
                    {
                        JProperty child = item as JProperty;
                        if (child.Name == "Features_Created")
                        {
                            var data = child.Value.ToArray();
                            foreach (var feature in data)
                            {
                                if (FeatureType.ContainsKey(feature.ToString()))
                                {
                                    FeatureType.Remove(feature.ToString());
                                }
                            }
                        }
                    }
                }
            }
            return FeatureType;
        }

        /// <summary>
        /// Get All Feature Data Type
        /// </summary>
        /// <param name="correlationId"></param>
        /// <returns></returns>
        private JObject GetAllFeatureDataType(string correlationId)
        {
            JObject FeatureType = new JObject();
            var dataCleanupCollection = _database.GetCollection<BsonDocument>(CONSTANTS.DataCleanUP_FilteredData);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.types).Exclude(CONSTANTS.Id);
            var filteredData = dataCleanupCollection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (filteredData.Count > 0)
            {
                FeatureType = JObject.Parse(filteredData[0][CONSTANTS.types].ToString());
            }
            return FeatureType;
        }

        /// <summary>
        /// Get all feature from Data Clean up
        /// </summary>
        /// <param name="correlationId"></param>
        /// <returns></returns>
        private JObject GetAllFeatureFromDECleanUp(string correlationId)
        {
            JObject data = new JObject();
            Dictionary<string, string> featureDataTypeCol = new Dictionary<string, string>();
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataCleanup);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.FeatureName).Include(CONSTANTS.CorrelationToRemove).Include(CONSTANTS.CorrelationId).Include("UnchangedDtypeColumns").Include(CONSTANTS.ProcessedRecords).Include(CONSTANTS.NewFeatureName).Include(CONSTANTS.NewAddFeatures).Exclude(CONSTANTS.Id);
            var bsonElements = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            bool DBEncryptionRequired = CommonUtility.EncryptDB(correlationId, appSettings);
            if (bsonElements.Count > 0)
            {
                BsonDocument processDocument = bsonElements[0];
                //decrypt db data
                if (DBEncryptionRequired)
                {
                    processDocument[CONSTANTS.FeatureName] = BsonDocument.Parse(_encryptionDecryption.Decrypt(bsonElements[0][CONSTANTS.FeatureName].AsString));
                }
                data = JObject.Parse(processDocument[CONSTANTS.FeatureName].ToString());
                foreach (var item in data.Children())
                {

                    JProperty j = item as JProperty;
                    var dataType = data[j.Name]["Datatype"];
                    foreach (var datatypenode in dataType.Children())
                    {
                        JProperty i = datatypenode as JProperty;
                        if (Convert.ToString(i.Value) == "True")
                        {
                            featureDataTypeCol.Add(j.Name.ToString(), i.Name.ToString());
                        }
                    }
                }
            }
            var newsetdata = JObject.FromObject(featureDataTypeCol);
            return newsetdata;
        }




        /// <summary>
        /// GetSilhouette_Graph
        /// </summary>
        /// <param name="correlationId"></param>
        /// <returns></returns>
        private string GetSilhouette_Graph(string correlationId)
        {
            string silhouette_graph = "";
            var meFeaTureSelection = _database.GetCollection<BsonDocument>(CONSTANTS.ME_FeatureSelection);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.CorrelationId).Include("silhouette_graph").Exclude(CONSTANTS.Id);
            var filteredData = meFeaTureSelection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (filteredData.Count > 0)
            {
                if (filteredData[0].Contains("silhouette_graph"))
                {
                    silhouette_graph = filteredData[0]["silhouette_graph"].ToString();
                }
                else
                {
                    silhouette_graph = "";
                }

            }
            return silhouette_graph;
        }



        /// <summary>
        /// Get Data for Model Processing 
        /// </summary>
        /// <param name="correlationId">correlationId</param>
        /// <param name="userId">userId</param>
        /// <param name="pageInfo">pageInfo</param>
        /// <param name="dataEngineeringDTO">dataEngineeringDTO</param>
        /// <returns></returns>
        public string GetDataForModelProcessing(string correlationId, string userId, string pageInfo, out DataEngineeringDTO dataEngineeringDTO)
        {
            DataEngineeringDTO dataCleanUpData = new DataEngineeringDTO();
            string PythonResult = string.Empty;
            var useCaseData = _ingestedData.GetRequestUsecase(correlationId, pageInfo);
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ProcessDataService), CONSTANTS.ProcessDataUseCaseData + useCaseData, CONSTANTS.START, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            dataCleanUpData.useCaseDetails = useCaseData;
            if (!string.IsNullOrEmpty(useCaseData))
            {
                JObject queueData = JObject.Parse(useCaseData);
                string status = (string)queueData[CONSTANTS.Status];
                string progress = (string)queueData[CONSTANTS.Progress];
                //LOGGING.LogManager.Logger.LogProcessInfo(typeof(ProcessDataService), CONSTANTS.UseCaseTableData, queueData.ToString(), new Guid(correlationId));
                if (status == CONSTANTS.C & progress == CONSTANTS.Hundred)
                {
                    dataCleanUpData = this.ProcessDataForModelling(correlationId, userId, pageInfo);
                    dataCleanUpData.useCaseDetails = useCaseData;
                    dataEngineeringDTO = dataCleanUpData;
                    if (!string.IsNullOrEmpty(dataCleanUpData.processData.ToString()))
                    {
                        return CONSTANTS.C;
                    }
                    else
                    {
                        return CONSTANTS.Empty;
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(status))
                    {
                        JObject queueData2 = JObject.Parse(useCaseData);
                        queueData2[CONSTANTS.Status] = CONSTANTS.P;
                        queueData2[CONSTANTS.Progress] = CONSTANTS.Zero_Value;
                        dataCleanUpData.useCaseDetails = queueData2.ToString();
                    }
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(ProcessDataService), nameof(ProcessDataForModelling), CONSTANTS.End, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                    dataEngineeringDTO = dataCleanUpData;
                    return CONSTANTS.P;
                }
            }
            else
            {
                var ingrainRequest = CommonUtility.InsertIngrainRequest(correlationId, userId, CONSTANTS.DataCleanUp, CONSTANTS.DataCleanUp);
                _ingestedData.InsertRequests(ingrainRequest);
                Thread.Sleep(2000);
                PythonResult resultPython = new PythonResult();
                resultPython.message = CONSTANTS.success;
                resultPython.status = CONSTANTS.True_Value;
                PythonResult = resultPython.ToJson();
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(ProcessDataService), nameof(ProcessDataForModelling), CONSTANTS.End, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                dataEngineeringDTO = dataCleanUpData;
                return CONSTANTS.Success;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="columns"></param>
        /// <param name="dataEngineering"></param>
        /// <returns></returns>
        public string PostCleanedData(dynamic columns, out DataEngineeringDTO dataEngineering)
        {
            DataEngineeringDTO dataEngineeringDTO = new DataEngineeringDTO();
            string correlationId = columns["correlationId"].ToString();
            string userId = columns["userId"].ToString();
            string pageInfo = columns["pageInfo"].ToString();
            var useCaseData = _ingestedData.GetRequestUsecase(correlationId, pageInfo);
            if (string.IsNullOrEmpty(useCaseData))
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(ProcessDataService), nameof(PostCleanedData) + "Empty--" + columns, "START-PostCleanedData", string.Empty, string.Empty, string.Empty, string.Empty);
                this.InsertDataCleanUp(columns, correlationId);
            }
            //Checking Queue Table and Calling the python.
            if (!string.IsNullOrEmpty(useCaseData))
            {
                JObject queueData = JObject.Parse(useCaseData);
                string status = (string)queueData["Status"];
                string progress = (string)queueData["Progress"];
                string message = (string)queueData["Message"];
                dataEngineeringDTO.Status = status;
                dataEngineeringDTO.Progress = progress;
                dataEngineeringDTO.Message = message;
                //LOGGING.LogManager.Logger.LogProcessInfo(typeof(ProcessDataService), "UseCaseTableData", queueData.ToString(), new Guid(correlationId));
                if (status == "C" & progress == "100")
                {
                    dataEngineeringDTO = this.ProcessDataForModelling(correlationId, userId, "DataCleanUp");
                    dataEngineeringDTO.useCaseDetails = useCaseData;
                    dataEngineeringDTO.Status = status;
                    dataEngineeringDTO.Progress = progress;
                    dataEngineeringDTO.Message = message;
                    var deleted = this.RemoveQueueRecords(correlationId, pageInfo);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(ProcessDataService), nameof(PostCleanedData), "RemoveQueueRecords" + deleted, string.Empty, string.Empty, string.Empty, string.Empty);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(ProcessDataService), nameof(PostCleanedData), "END", string.Empty, string.Empty, string.Empty, string.Empty);
                    //return Ok(dataEngineeringDTO);
                    dataEngineering = dataEngineeringDTO;
                    return CONSTANTS.C;
                }
                else
                {
                    if (dataEngineeringDTO.Status == null)
                    {
                        dataEngineeringDTO.Status = "P";
                        dataEngineeringDTO.Progress = "0";
                        dataEngineeringDTO.Message = null;
                    }
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(ProcessDataService), nameof(PostCleanedData), "END", string.Empty, string.Empty, string.Empty, string.Empty);
                    //return Accepted(dataEngineeringDTO);
                    dataEngineering = dataEngineeringDTO;
                    return CONSTANTS.P;
                }
            }
            else
            {
                //IngrainRequestQueue ingrainRequest = new IngrainRequestQueue
                //{
                //    _id = Guid.NewGuid().ToString(),
                //    CorrelationId = correlationId,
                //    RequestId = Guid.NewGuid().ToString(),
                //    ProcessId = null,
                //    Status = null,
                //    ModelName = null,
                //    RequestStatus = "New",
                //    RetryCount = 0,
                //    ProblemType = null,
                //    Message = null,
                //    UniId = null,
                //    Progress = null,
                //    pageInfo = pageInfo,
                //    ParamArgs = "{}",
                //    Function = "DataCleanUp",
                //    CreatedByUser = userId,
                //    CreatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                //    ModifiedByUser = userId,
                //    ModifiedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                //    LastProcessedOn = null,
                //};
                var ingrainRequest = CommonUtility.InsertIngrainRequest(correlationId, userId, pageInfo, CONSTANTS.DataCleanUp);
                _ingestedData.InsertRequests(ingrainRequest);
                Thread.Sleep(2000);
                //PythonResult resultPython = new PythonResult();
                //resultPython.message = "success";
                //resultPython.status = "true";
                //PythonResult = resultPython.ToJson();
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(ProcessDataService), nameof(PostCleanedData), "End", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                // return Ok(new { response = PythonResult });
                //return Ok(PythonResult);
                dataEngineering = dataEngineeringDTO;
                return CONSTANTS.Success;
            }
        }
    }

    #endregion

}

