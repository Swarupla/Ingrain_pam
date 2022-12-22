#region Copyright (c) 2018 Accenture . All rights reserved.

// <copyright company="Accenture">
// Copyright (c) 2018 Accenture.  All rights reserved.
// Reproduction or transmission in whole or in part, in any form or by any means, 
// electronic, mechanical or otherwise, is prohibited without the prior written 
// consent of the copyright owner.
// </copyright>

#endregion

#region ModelEngineeringService Information
/********************************************************************************************************\
Module Name     :   ModelEngineeringService
Project         :   Accenture.MyWizard.SelfServiceAI.BusinessDomain
Organisation    :   Accenture Technologies Ltd.
Created By      :   RaviA
Created Date    :   30-Jan-2019
Revision History :
Revision No             Initial             Modified Date           Description
1.0                     An                  29-Mar-2019             
\********************************************************************************************************/
#endregion

namespace Accenture.MyWizard.Ingrain.BusinessDomain.Services
{
    #region Namespace
    using Accenture.MyWizard.Ingrain.BusinessDomain.Common;
    using Accenture.MyWizard.Resource;
    using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
    using Accenture.MyWizard.Ingrain.DataAccess;
    using Accenture.MyWizard.Ingrain.DataModels.Models;
    using MongoDB.Bson;
    using MongoDB.Bson.Serialization;
    using MongoDB.Driver;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Accenture.MyWizard.Shared.Helpers;
    using Microsoft.Extensions.Options;
    using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
    using System.Threading;
    using Microsoft.Extensions.DependencyInjection;
    #endregion

    public class ModelEngineeringService : IModelEngineering
    {
        #region Members
        private MongoClient _mongoClient;
        private IMongoDatabase _database;
        PreProcessDTO preProcessDTO;
        private SystemUsageDetails _systemUsageDetails;
        //private PerformanceCounter cpuCounter;
        //private PerformanceCounter ramCounter;
        private readonly DatabaseProvider databaseProvider;
        private IOptions<IngrainAppSettings> appSettings { get; set; }

        private static IIngestedData _ingestedDataService { set; get; }
        List<string> keylist = new List<string>();
        private IEncryptionDecryption _encryptionDecryption;
        //Anomaly Detection
        private MongoClient _mongoClientAD;
        private IMongoDatabase _databaseAD;
        private string servicename = "";
        #endregion

        #region Constructors
        public ModelEngineeringService(DatabaseProvider db, IOptions<IngrainAppSettings> settings, IServiceProvider serviceProvider)
        {
            databaseProvider = db;
            appSettings = settings;
            _mongoClient = databaseProvider.GetDatabaseConnection();
            var dataBaseName = MongoUrl.Create(appSettings.Value.connectionString).DatabaseName;
            _database = _mongoClient.GetDatabase(dataBaseName);
            preProcessDTO = new PreProcessDTO();
            _ingestedDataService = serviceProvider.GetService<IIngestedData>();
            _encryptionDecryption = serviceProvider.GetService<IEncryptionDecryption>();
            //Anomaly Detection Connection
            _mongoClientAD = databaseProvider.GetDatabaseConnection("Anomaly");
            var dataBaseNameAD = MongoUrl.Create(appSettings.Value.AnomalyDetectionCS).DatabaseName;
            _databaseAD = _mongoClientAD.GetDatabase(dataBaseNameAD);
        }
        #endregion

        #region Methods

        public TeachAndTestDTO GetScenariosforTeach(string correlationId)
        {
            TeachAndTestDTO teachAndTestDTO = new TeachAndTestDTO();
            var testModelCollection = _database.GetCollection<BsonDocument>(CONSTANTS.WF_TestResults);
            var filterBuilder = Builders<BsonDocument>.Filter;
            var filterScenario = filterBuilder.Eq(CONSTANTS.CorrelationId, correlationId) & filterBuilder.Eq(CONSTANTS.Temp, CONSTANTS.True);
            var projectionScenario = Builders<BsonDocument>.Projection.Include(CONSTANTS.CorrelationId).Include(CONSTANTS.WFId).Include(CONSTANTS.ScenarioName).Include(CONSTANTS.Model).Exclude(CONSTANTS.Id);
            var testModelData = testModelCollection.Find(filterScenario).Project<BsonDocument>(projectionScenario).ToList();
            List<JObject> testData = new List<JObject>();
            //bool DBEncryptionRequired = CommonUtility.EncryptDB(correlationId, appSettings);
            if (testModelData.Count > 0)
            {
                for (int i = 0; i < testModelData.Count; i++)
                {
                    //if (DBEncryptionRequired)
                    //{
                    //    if (testModelData[i].Contains("CreatedBy") && !string.IsNullOrEmpty(Convert.ToString(testModelData[i]["CreatedBy"])))
                    //    {
                    //        testModelData[i]["CreatedBy"] = _encryptionDecryption.Decrypt(Convert.ToString(testModelData[i]["CreatedBy"]));
                    //    }
                    //    if (testModelData[i].Contains(CONSTANTS.ModifiedByUser) && !string.IsNullOrEmpty(Convert.ToString(testModelData[i][CONSTANTS.ModifiedByUser])))
                    //    {
                    //        testModelData[i][CONSTANTS.ModifiedByUser] = _encryptionDecryption.Decrypt(Convert.ToString(testModelData[i][CONSTANTS.ModifiedByUser]));
                    //    }
                    //}
                    testModelData[i][CONSTANTS.ScenarioCase] = CONSTANTS.WFScenario;
                    testData.Add(JObject.Parse(testModelData[i].ToString()));
                }
            }
            var prescriptiveCollection = _database.GetCollection<BsonDocument>(CONSTANTS.PrescriptiveAnalyticsResults);
            var filter = Builders<BsonDocument>.Filter;
            var filterResult = filter.Eq(CONSTANTS.CorrelationId, correlationId) & filterBuilder.Eq(CONSTANTS.Temp, CONSTANTS.True);
            var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.CorrelationId).Include(CONSTANTS.WFId).Include(CONSTANTS.ScenarioName).Include(CONSTANTS.Model).Exclude(CONSTANTS.Id);
            var prescriptiveAnalytics = prescriptiveCollection.Find(filterResult).Project<BsonDocument>(projection).ToList();
            if (prescriptiveAnalytics.Count > 0)
            {
                for (int i = 0; i < prescriptiveAnalytics.Count; i++)
                {
                    //if (DBEncryptionRequired)
                    //{
                    //    if (prescriptiveAnalytics[i].Contains(CONSTANTS.CreatedByUser) && !string.IsNullOrEmpty(Convert.ToString(prescriptiveAnalytics[i][CONSTANTS.CreatedByUser])))
                    //    {
                    //        prescriptiveAnalytics[i][CONSTANTS.CreatedByUser] = _encryptionDecryption.Decrypt(Convert.ToString(prescriptiveAnalytics[i][CONSTANTS.CreatedByUser]));
                    //    }
                    //    if (prescriptiveAnalytics[i].Contains(CONSTANTS.ModifiedByUser) && !string.IsNullOrEmpty(Convert.ToString(prescriptiveAnalytics[i][CONSTANTS.ModifiedByUser])))
                    //    {
                    //        prescriptiveAnalytics[i][CONSTANTS.ModifiedByUser] = _encryptionDecryption.Decrypt(Convert.ToString(prescriptiveAnalytics[i][CONSTANTS.ModifiedByUser]));
                    //    }
                    //}
                    prescriptiveAnalytics[i][CONSTANTS.ScenarioCase] = CONSTANTS.PAScenario;
                    testData.Add(JObject.Parse(prescriptiveAnalytics[i].ToString()));
                }
            }
            teachAndTestDTO.TeachtestModelData = testData;

            return teachAndTestDTO;
        }
        public TeachAndTestDTO GetFeatureForTest(string correlationId, string modelName)
        {
            TeachAndTestDTO teachAndTestDTO = new TeachAndTestDTO();
            teachAndTestDTO.FeatureNameList = new List<object>();
            teachAndTestDTO.TargetColUniqueValues = new List<object>();
            var value = new Dictionary<string, string>();
            var featureData = new List<BsonDocument>();
            var filtereddata = new List<BsonDocument>();
            var filtereddata_problemtype = new List<BsonDocument>();
            var filtereddata_text = new List<BsonDocument>();
            var featureCollection = _database.GetCollection<BsonDocument>(CONSTANTS.MEFeatureSelection);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.FeatureImportance).Include(CONSTANTS.NLP_Flag).Include(CONSTANTS.Clustering_Flag).Exclude(CONSTANTS.Id);
            featureData = featureCollection.Find(filter).Project<BsonDocument>(projection).ToList();
            JObject serializeData = new JObject();
            List<string> featuresList = new List<string>();
            Dictionary<string, string> featuresListSorted = new Dictionary<string, string>();
            List<string> uniqueColumns = new List<string>();
            string targetColumn = string.Empty;
            var typesColumns = new Dictionary<string, string>();

            var uniqueDictionary = new Dictionary<string, Dictionary<string, string>>();

            var filteredCollection = _database.GetCollection<BsonDocument>(CONSTANTS.DataCleanUPFilteredData);
            var projection2 = Builders<BsonDocument>.Projection.Include(CONSTANTS.ColumnUniqueValues).Include(CONSTANTS.types).Include(CONSTANTS.target_variable).Exclude(CONSTANTS.Id);
            filtereddata = filteredCollection.Find(filter).Project<BsonDocument>(projection2).ToList();

            bool DBEncryptionRequired = CommonUtility.EncryptDB(correlationId, appSettings);
            if (featureData.Count > 0)
            {

                serializeData = JObject.Parse(featureData[0].ToString());
                //Taking all the Columns
                foreach (var features in serializeData[CONSTANTS.FeatureImportance].Children())
                {
                    JProperty j = features as JProperty;
                    foreach (var item in j.Children())
                    {
                        if (item[CONSTANTS.Selection].ToString() == CONSTANTS.True)
                        {
                            featuresList.Add(j.Name);
                            var serializedatasort = JObject.Parse(j.Value.ToString());
                            foreach (var sort in serializedatasort.Children())
                            {
                                JProperty j1 = sort as JProperty;
                                if (j1.Name == "Value")
                                    featuresListSorted.Add(j.Name, j1.Value.ToString());
                            }
                        }
                    }

                    ////New Feature - Download - Get All Clumn Names -Added by Shreya
                    //value.Add(j.Name, " ");

                }
                //var names = JsonConvert.SerializeObject(JObject.Parse(value.ToJson()));
                //teachAndTestDTO.FeatureNameList.Add(JsonConvert.DeserializeObject<object>(names));
            }

            if (filtereddata.Count > 0)
            {
                targetColumn = filtereddata[0][CONSTANTS.target_variable].ToString();

                if (DBEncryptionRequired)
                {
                    filtereddata[0][CONSTANTS.ColumnUniqueValues] = BsonDocument.Parse(_encryptionDecryption.Decrypt(filtereddata[0][CONSTANTS.ColumnUniqueValues].AsString));
                    //if (!string.IsNullOrEmpty(Convert.ToString(filtereddata[0][CONSTANTS.CreatedByUser])))
                    //    filtereddata[0][CONSTANTS.CreatedByUser] = _encryptionDecryption.Decrypt(filtereddata[0][CONSTANTS.CreatedByUser].AsString);
                    //if (!string.IsNullOrEmpty(Convert.ToString(filtereddata[0][CONSTANTS.ModifiedByUser])))
                    //    filtereddata[0][CONSTANTS.ModifiedByUser] = _encryptionDecryption.Decrypt(filtereddata[0][CONSTANTS.ModifiedByUser].AsString);

                }
                serializeData = JObject.Parse(filtereddata[0].ToString());
                foreach (var type in serializeData[CONSTANTS.types].Children())
                {
                    JProperty j = type as JProperty;
                    typesColumns.Add(j.Name, j.Value.ToString());
                }

                if (serializeData != null)
                {
                    foreach (var item in serializeData[CONSTANTS.ColumnUniqueValues].Children())
                    {
                        JProperty jProperty = item as JProperty;
                        if (jProperty.Name == targetColumn)
                        {
                            foreach (var predictiveValue in serializeData[CONSTANTS.ColumnUniqueValues][jProperty.Name].Children())
                            {
                                teachAndTestDTO.TargetColUniqueValues.Add(predictiveValue);
                            }
                            break;
                        }
                    }
                }

            }

            foreach (var item in featuresList)
            {
                if (item != targetColumn)
                {
                    var datafeature = new Dictionary<string, string>();
                    List<string> floatList = new List<string>();
                    List<string> intList = new List<string>();
                    if (serializeData[CONSTANTS.ColumnUniqueValues][item] != null)
                    {
                        foreach (JToken attributes in serializeData[CONSTANTS.ColumnUniqueValues][item].Children())
                        {
                            string a;//= attributes.ToString();
                            if (typesColumns.ContainsKey(item) & typesColumns.TryGetValue(item, out a))
                            {
                                if (a == CONSTANTS.float64)
                                {
                                    floatList.Add(attributes.ToString());
                                }
                                else if (a == CONSTANTS.int64)
                                { intList.Add(attributes.ToString()); }
                                else if (a == CONSTANTS.datetime64)
                                {
                                    DateTime timeStampAttribute = attributes.ToObject<DateTime>();
                                    string datetimeAttribute = timeStampAttribute.ToString(CONSTANTS.datetimeAttribute);
                                    datafeature.Add(datetimeAttribute, CONSTANTS.False);
                                }
                                else
                                {
                                    datafeature.Add(attributes.ToString(), CONSTANTS.False);
                                }
                            }
                        }
                        if (intList.Count > 0)
                        {
                            var max = intList.Select(v => int.Parse(v)).Max();
                            var min = intList.Select(v => int.Parse(v)).Min();
                            datafeature.Add(CONSTANTS.MaxintValue + max, CONSTANTS.False);
                            datafeature.Add(CONSTANTS.MinintValue + min, CONSTANTS.False);
                        }
                        if (floatList.Count > 0)
                        {
                            var max = floatList.Select(v => float.Parse(v)).Max();
                            var min = floatList.Select(v => float.Parse(v)).Min();
                            datafeature.Add(CONSTANTS.MaxfloatValue + max, CONSTANTS.False);
                            datafeature.Add(CONSTANTS.MinfloatValue + min, CONSTANTS.False);
                        }
                        uniqueDictionary.Add(item, datafeature);
                    }
                }
            }


            string NLPFlag = CONSTANTS.False_value;
            List<string> textvalues = new List<string>();
            if (featureData[0].Contains(CONSTANTS.NLP_Flag))
            {
                NLPFlag = featureData[0][CONSTANTS.NLP_Flag].ToString();
                //Cluster as Optional - NLP
                if (featureData[0].Contains(CONSTANTS.Clustering_Flag))
                {
                    teachAndTestDTO.Clustering_Flag = featureData[0][CONSTANTS.Clustering_Flag].ToString();
                }
            }

            teachAndTestDTO.NLP_Flag = NLPFlag;

            if (NLPFlag == CONSTANTS.True_Value)
            {
                var filteredCollection1 = _database.GetCollection<BsonDocument>(CONSTANTS.DE_DataCleanup);
                var projection4 = Builders<BsonDocument>.Projection.Include(CONSTANTS.FeatureName).Include(CONSTANTS.NewFeatureName).Exclude(CONSTANTS.Id);
                filtereddata_text = filteredCollection1.Find(filter).Project<BsonDocument>(projection4).ToList();
                if (filtereddata_text.Count() > 0)
                {
                    foreach (var keys in featuresListSorted)
                    {
                        if (teachAndTestDTO.Clustering_Flag == CONSTANTS.False_value) //Cluster as Optional
                        {
                            if (keys.Key.Contains("All_Text") != true)
                            {
                                keylist.Add(keys.Key);
                            }
                        }
                        else
                        {
                            if (keys.Key.Contains("Cluster") != true)
                            {
                                keylist.Add(keys.Key);
                            }
                        }
                    }
                    var keysToRemove = featuresListSorted.Keys.Except(keylist).ToList();

                    foreach (var key in keysToRemove)
                        featuresListSorted.Remove(key);

                    //decrypt db data
                    if (DBEncryptionRequired)
                    {
                        filtereddata_text[0][CONSTANTS.FeatureName] = BsonDocument.Parse(_encryptionDecryption.Decrypt(filtereddata_text[0][CONSTANTS.FeatureName].AsString));
                        if (filtereddata_text[0][CONSTANTS.NewFeatureName] != null & filtereddata_text[0][CONSTANTS.NewFeatureName].ToString() != "{ }")
                        {
                            filtereddata_text[0][CONSTANTS.NewFeatureName] = BsonDocument.Parse(_encryptionDecryption.Decrypt(filtereddata_text[0][CONSTANTS.NewFeatureName].AsString));
                        }
                        //if (!string.IsNullOrEmpty(Convert.ToString(filtereddata_text[0][CONSTANTS.CreatedByUser])))
                        //{
                        //    filtereddata_text[0][CONSTANTS.CreatedByUser] = _encryptionDecryption.Decrypt(filtereddata_text[0][CONSTANTS.CreatedByUser].AsString);
                        //}
                        //if (!string.IsNullOrEmpty(Convert.ToString(filtereddata_text[0][CONSTANTS.ModifiedByUser])))
                        //{
                        //    filtereddata_text[0][CONSTANTS.ModifiedByUser] = _encryptionDecryption.Decrypt(filtereddata_text[0][CONSTANTS.ModifiedByUser].AsString);
                        //}
                    }
                    serializeData = JObject.Parse(filtereddata_text[0].ToString());
                    //Taking all the Columns
                    if (serializeData.Count > 0)
                    {
                        //Existing Feature Attributes Adding
                        foreach (var features in serializeData[CONSTANTS.FeatureName].Children())
                        {
                            JProperty j = features as JProperty;
                            JObject serializeDatafeature = new JObject();
                            serializeDatafeature = JObject.Parse(j.Value.ToString());
                            var textColumnType = serializeData[CONSTANTS.FeatureName][j.Name][CONSTANTS.Datatype];
                            if ((Convert.ToString(textColumnType[CONSTANTS.Text])) == CONSTANTS.True)
                            {
                                textvalues.Add(j.Name);
                                var serializeData1 = JObject.Parse(filtereddata[0].ToString());
                                if (serializeData1[CONSTANTS.ColumnUniqueValues][j.Name] != null)
                                {
                                    var textValue = serializeData1[CONSTANTS.ColumnUniqueValues][j.Name].FirstOrDefault();
                                    featuresListSorted.Add(j.Name, textValue.ToString());
                                }
                                else
                                {
                                    featuresListSorted.Add(j.Name, "0.0");
                                }
                            }
                        }

                        ///New Text Features adding
                        if (serializeData[CONSTANTS.NewFeatureName] != null & serializeData[CONSTANTS.NewFeatureName].ToString() != "{ }")
                        {
                            foreach (var features in serializeData[CONSTANTS.NewFeatureName].Children())
                            {
                                JProperty j = features as JProperty;
                                JObject serializeDatafeature = new JObject();
                                serializeDatafeature = JObject.Parse(j.Value.ToString());
                                var textColumnType = serializeData[CONSTANTS.NewFeatureName][j.Name][CONSTANTS.Datatype];
                                if ((Convert.ToString(textColumnType[CONSTANTS.Text])) == CONSTANTS.True)
                                {
                                    textvalues.Add(j.Name);
                                    var serializeData1 = JObject.Parse(filtereddata[0].ToString());
                                    if (serializeData1[CONSTANTS.ColumnUniqueValues][j.Name] != null)
                                    {
                                        var textValue = serializeData1[CONSTANTS.ColumnUniqueValues][j.Name].FirstOrDefault();
                                        featuresListSorted.Add(j.Name, textValue.ToString());
                                    }
                                    else
                                    {
                                        featuresListSorted.Add(j.Name, "0.0");
                                    }

                                }
                            }
                        }
                    };
                }
                List<string> data = new List<string>();

                foreach (var item in textvalues)
                {
                    var datafeature = new Dictionary<string, string>();
                    if (item != targetColumn)
                    {
                        serializeData = JObject.Parse(filtereddata[0].ToString());
                        if (serializeData[CONSTANTS.ColumnUniqueValues][item] != null)
                        {
                            var textValue = serializeData[CONSTANTS.ColumnUniqueValues][item].FirstOrDefault();
                            datafeature.Add(CONSTANTS.TextBox + textValue.ToString(), CONSTANTS.False);

                        }
                        else
                        {
                            datafeature.Add(CONSTANTS.TextBox_Field, CONSTANTS.False);
                        }
                    }
                    uniqueDictionary.Add(item, datafeature);
                }
            }

            if (targetColumn != string.Empty && featuresListSorted.Count > 0)
                featuresListSorted.Remove(targetColumn);

            List<FeatureImportanceModel> clstrainedModels = new List<FeatureImportanceModel>();
            string FeatureName = string.Empty, trainedmodelname = string.Empty;
            var columnCollection = _database.GetCollection<BsonDocument>("SSAI_RecommendedTrainedModels");
            var filtermodel = Builders<BsonDocument>.Filter.Eq("CorrelationId", correlationId);
            var projectionmodel = Builders<BsonDocument>.Projection.Include("modelName").Include("featureImportance").Exclude("_id");
            var trainedModel = columnCollection.Find(filtermodel).Project<BsonDocument>(projectionmodel).ToList();
            if (trainedModel.Count > 0)
            {
                for (int i = 0; i < trainedModel.Count; i++)
                {

                    serializeData = JObject.Parse(trainedModel[i].ToString());
                    trainedmodelname = serializeData["modelName"].ToString();
                    clstrainedModels.Add(new FeatureImportanceModel
                    {
                        modelName = trainedmodelname,
                        featureImportance = featuresListSorted
                    });
                    //trainedModels.Add(JObject.Parse(trainedModel[i].ToString()));
                }

                teachAndTestDTO.FeatureImportance = clstrainedModels;
            }

            var testModelCollection = _database.GetCollection<BsonDocument>(CONSTANTS.WF_TestResults);
            var filterBuilder = Builders<BsonDocument>.Filter;
            var filterScenario = filterBuilder.Eq(CONSTANTS.CorrelationId, correlationId) & filterBuilder.Eq(CONSTANTS.Temp, CONSTANTS.True) & filterBuilder.Eq(CONSTANTS.Model, modelName);
            var projectionScenario = Builders<BsonDocument>.Projection.Include(CONSTANTS.CorrelationId).Include(CONSTANTS.WFId).Include(CONSTANTS.ScenarioName).Include(CONSTANTS.Model).Include(CONSTANTS.Clustering_Flag).Exclude(CONSTANTS.Id);
            var testModelData = testModelCollection.Find(filterScenario).Project<BsonDocument>(projectionScenario).ToList();
            List<JObject> testData = new List<JObject>();
            if (testModelData.Count > 0)
            {
                for (int i = 0; i < testModelData.Count; i++)
                {
                    //if (DBEncryptionRequired)
                    //{
                    //    if (testModelData[i].Contains(CONSTANTS.CreatedByUser) && !string.IsNullOrEmpty(Convert.ToString(testModelData[i][CONSTANTS.CreatedByUser])))
                    //    {
                    //        testModelData[i][CONSTANTS.CreatedByUser] = _encryptionDecryption.Decrypt(Convert.ToString(testModelData[i][CONSTANTS.CreatedByUser]));
                    //    }
                    //    if (testModelData[i].Contains(CONSTANTS.ModifiedByUser) && !string.IsNullOrEmpty(Convert.ToString(testModelData[i][CONSTANTS.ModifiedByUser])))
                    //    {
                    //        testModelData[i][CONSTANTS.ModifiedByUser] = _encryptionDecryption.Decrypt(Convert.ToString(testModelData[i][CONSTANTS.ModifiedByUser]));
                    //    }
                    //}
                    testModelData[i][CONSTANTS.ScenarioCase] = CONSTANTS.WFScenario;
                    testData.Add(JObject.Parse(testModelData[i].ToString()));
                }
            }

            var prescriptiveCollection = _database.GetCollection<BsonDocument>(CONSTANTS.PrescriptiveAnalyticsResults);
            var PAFilter = Builders<BsonDocument>.Filter;
            var PAFilterResult = PAFilter.Eq(CONSTANTS.CorrelationId, correlationId) & PAFilter.Eq(CONSTANTS.Temp, CONSTANTS.True) & filterBuilder.Eq(CONSTANTS.Model, modelName);
            var PAPojection = Builders<BsonDocument>.Projection.Include(CONSTANTS.CorrelationId).Include(CONSTANTS.WFId).Include(CONSTANTS.ScenarioName).Include(CONSTANTS.Model).Exclude(CONSTANTS.Id);
            var prescriptiveAnalytics = prescriptiveCollection.Find(PAFilterResult).Project<BsonDocument>(PAPojection).ToList();
            if (prescriptiveAnalytics.Count > 0)
            {
                for (int i = 0; i < prescriptiveAnalytics.Count; i++)
                {
                    //if (DBEncryptionRequired)
                    //{
                    //    if (prescriptiveAnalytics[i].Contains(CONSTANTS.CreatedByUser) && !string.IsNullOrEmpty(Convert.ToString(prescriptiveAnalytics[i][CONSTANTS.CreatedByUser])))
                    //    {
                    //        prescriptiveAnalytics[i][CONSTANTS.CreatedByUser] = _encryptionDecryption.Decrypt(Convert.ToString(prescriptiveAnalytics[i][CONSTANTS.CreatedByUser]));
                    //    }
                    //    if (prescriptiveAnalytics[i].Contains(CONSTANTS.ModifiedByUser) && !string.IsNullOrEmpty(Convert.ToString(prescriptiveAnalytics[i][CONSTANTS.ModifiedByUser])))
                    //    {
                    //        prescriptiveAnalytics[i][CONSTANTS.ModifiedByUser] = _encryptionDecryption.Decrypt(Convert.ToString(prescriptiveAnalytics[i][CONSTANTS.ModifiedByUser]));
                    //    }
                    //}
                    prescriptiveAnalytics[i][CONSTANTS.ScenarioCase] = CONSTANTS.PAScenario;
                    testData.Add(JObject.Parse(prescriptiveAnalytics[i].ToString()));
                }
            }

            teachAndTestDTO.TeachtestModelData = testData;

            if (!string.IsNullOrEmpty(JsonConvert.SerializeObject(uniqueDictionary)) && JsonConvert.SerializeObject(uniqueDictionary) != null)
            {
                teachAndTestDTO.TeachtestData = JObject.Parse(JsonConvert.SerializeObject(uniqueDictionary));

                var ingestCollection = _database.GetCollection<BsonDocument>(CONSTANTS.PSIngestedData);
                var ingestFilter = Builders<BsonDocument>.Filter;
                var filteringestData = filterBuilder.Eq(CONSTANTS.CorrelationId, correlationId);
                var projectionIngest = Builders<BsonDocument>.Projection.Include(CONSTANTS.DataType).Exclude(CONSTANTS.Id);
                var ingestData = ingestCollection.Find(filteringestData).Project<BsonDocument>(projectionIngest).ToList();
                if (ingestData.Count > 0)
                {
                    var cols = new List<string>();
                    var allColumns = uniqueDictionary.Keys.ToList();
                    var ingestSerializeData = JObject.Parse(ingestData[0].ToString());
                    foreach (var features in ingestSerializeData[CONSTANTS.DataType].Children())
                    {
                        JProperty j = features as JProperty;
                        cols.Add(j.Name);
                    }
                    var data = cols.ToList();
                    //cols.CopyTo(data);

                    for (int i = 0; i < data.Count; i++)
                    {
                        if (!allColumns.Contains(data[i]) && data[i] != targetColumn)
                        {
                            if (cols.Contains(data[i]))
                            {
                                cols.Remove(data[i]);
                                //data.Add(cols[i]);
                            }
                        }
                    }

                    if (cols.Count > 0)
                    {
                        var columnList = cols.Union(allColumns).ToList();
                        for (int i = 0; i < columnList.Count; i++)
                        {
                            value.Add(columnList[i], CONSTANTS.EmptySpace);
                        }
                        if (value.Count > 0)
                        {
                            var names = JsonConvert.SerializeObject(JObject.Parse(value.ToJson()));
                            teachAndTestDTO.FeatureNameList.Add(JsonConvert.DeserializeObject<object>(names));
                        }
                    }
                }
            }

            List<string> dataSource = null;
            dataSource = CommonUtility.GetDataSourceModel(correlationId, appSettings);
            if (dataSource.Count > 0)
            {
                teachAndTestDTO.ModelName = dataSource[0];
                teachAndTestDTO.DataSource = dataSource[1];
                teachAndTestDTO.ModelType = dataSource[2];
                teachAndTestDTO.BusinessProblem = dataSource[3];
                teachAndTestDTO.Category = dataSource[5];
            }
            return teachAndTestDTO;
        }

        public TeachAndTestDTOforTS GetFeatureForTestforTS(string correlationId, string modelType, string timeSeriesSteps, string modelName)
        {
            TeachAndTestDTOforTS teachAndTestDTO = new TeachAndTestDTOforTS();
            if (modelType == CONSTANTS.TimeSeries)
            {
                var testModelCollection = _database.GetCollection<BsonDocument>(CONSTANTS.WF_TestResults);
                var filterBuilder = Builders<BsonDocument>.Filter;
                var filterScenario = filterBuilder.Eq(CONSTANTS.CorrelationId, correlationId) & filterBuilder.Eq(CONSTANTS.Temp, CONSTANTS.True) & filterBuilder.Eq(CONSTANTS.Model, modelName);
                var projectionScenario = Builders<BsonDocument>.Projection.Include(CONSTANTS.CorrelationId).Include(CONSTANTS.WFId).Include(CONSTANTS.ScenarioName).Include(CONSTANTS.Model).Exclude(CONSTANTS.Id);
                var testModelData = testModelCollection.Find(filterScenario).Project<BsonDocument>(projectionScenario).ToList();
                List<JObject> testResultsData = new List<JObject>();
                //bool DBEncryptionRequired = CommonUtility.EncryptDB(correlationId, appSettings);
                if (testModelData.Count > 0)
                {
                    for (int i = 0; i < testModelData.Count; i++)
                    {
                        //if (DBEncryptionRequired)
                        //{
                        //    if (testModelData[i].Contains(CONSTANTS.CreatedByUser) && !string.IsNullOrEmpty(Convert.ToString(testModelData[i][CONSTANTS.CreatedByUser])))
                        //    {
                        //        testModelData[i][CONSTANTS.CreatedByUser] = _encryptionDecryption.Decrypt(Convert.ToString(testModelData[i][CONSTANTS.CreatedByUser]));
                        //    }
                        //    if (testModelData[i].Contains(CONSTANTS.ModifiedByUser) && !string.IsNullOrEmpty(Convert.ToString(testModelData[i][CONSTANTS.ModifiedByUser])))
                        //    {
                        //        testModelData[i][CONSTANTS.ModifiedByUser] = _encryptionDecryption.Decrypt(Convert.ToString(testModelData[i][CONSTANTS.ModifiedByUser]));
                        //    }
                        //}
                        testResultsData.Add(JObject.Parse(testModelData[i].ToString()));
                    }
                    teachAndTestDTO.TeachtestModelData = testResultsData;
                }

                var featureData = new List<BsonDocument>();
                JObject serializeData = new JObject();
                var featureCollection = _database.GetCollection<BsonDocument>(CONSTANTS.MLDL_ModelsMaster);
                var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.TimeSeriesParams).Exclude(CONSTANTS.Id);
                featureData = featureCollection.Find(Builders<BsonDocument>.Filter.Empty).Project<BsonDocument>(projection).ToList();
                if (featureData.Count > 0)
                {
                    //if (DBEncryptionRequired)
                    //{
                    //    if (featureData[0].Contains(CONSTANTS.CreatedByUser) && !string.IsNullOrEmpty(Convert.ToString(featureData[0][CONSTANTS.CreatedByUser])))
                    //    {
                    //        featureData[0][CONSTANTS.CreatedByUser] = _encryptionDecryption.Decrypt(Convert.ToString(featureData[0][CONSTANTS.CreatedByUser]));
                    //    }
                    //    if (featureData[0].Contains(CONSTANTS.ModifiedByUser) && !string.IsNullOrEmpty(Convert.ToString(featureData[0][CONSTANTS.ModifiedByUser])))
                    //    {
                    //        featureData[0][CONSTANTS.ModifiedByUser] = _encryptionDecryption.Decrypt(Convert.ToString(featureData[0][CONSTANTS.ModifiedByUser]));
                    //    }
                    //}
                    serializeData = JObject.Parse(featureData[0].ToString());
                    var stepsDictionary = new Dictionary<string, JObject>();
                    List<JObject> testData = new List<JObject>();
                    List<string> notTimeColumns = new List<string>();

                    foreach (var steps in serializeData[CONSTANTS.TimeSeriesParams][CONSTANTS.steps].Children())
                    {
                        JProperty stepsTime = steps as JProperty;
                        if (stepsTime.Name != timeSeriesSteps)
                        {
                            notTimeColumns.Add(stepsTime.Name);
                        }
                    }

                    foreach (var ntCols in notTimeColumns)
                    {

                        JObject header2 = (JObject)serializeData.SelectToken(CONSTANTS.TimeSeriesParams_steps);
                        header2.Property(ntCols).Remove();

                    }
                    teachAndTestDTO.TeachtestData = serializeData;
                }
            }

            List<string> dataSource = null;
            dataSource = CommonUtility.GetDataSourceModel(correlationId, appSettings);
            if (dataSource.Count > 0)
            {
                teachAndTestDTO.ModelName = dataSource[0];
                teachAndTestDTO.DataSource = dataSource[1];
                teachAndTestDTO.ModelType = dataSource[2];
                teachAndTestDTO.BusinessProblem = dataSource[3];
                if (dataSource.Count > 4 && !string.IsNullOrEmpty(dataSource[4]))
                {
                    teachAndTestDTO.InstaFLag = true;
                    teachAndTestDTO.Category = dataSource[5];
                }
            }

            return teachAndTestDTO;
        }

        public TeachAndTestDTO GetTeachModels(string correlationId, string WFId, string IstimeSeries, string scenario)
        {

            TeachAndTestDTO teachAndTestDTO = new TeachAndTestDTO();
            var testModelData = new List<BsonDocument>();
            bool DBEncryptionRequired = CommonUtility.EncryptDB(correlationId, appSettings);
            if (scenario == CONSTANTS.PAScenario)
            {
                var predictiveCollection = _database.GetCollection<BsonDocument>(CONSTANTS.PrescriptiveAnalyticsResults);
                var filter1 = Builders<BsonDocument>.Filter;
                var filterBuilder1 = filter1.Eq(CONSTANTS.CorrelationId, correlationId) & filter1.Eq(CONSTANTS.WFId, WFId) & filter1.Eq(CONSTANTS.Temp, CONSTANTS.True);
                var projection1 = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
                testModelData = predictiveCollection.Find(filterBuilder1).Project<BsonDocument>(projection1).ToList();
                if (testModelData.Count > 0)
                {
                    if (DBEncryptionRequired)
                    {
                        if (testModelData[0].Contains(CONSTANTS.Predictions))
                        {
                            testModelData[0][CONSTANTS.Predictions] = BsonDocument.Parse(_encryptionDecryption.Decrypt(testModelData[0][CONSTANTS.Predictions].AsString));
                        }
                        try
                        {
                            if (testModelData[0].Contains("CreatedBy") && !string.IsNullOrEmpty(Convert.ToString(testModelData[0]["CreatedBy"])))
                            {
                                testModelData[0]["CreatedBy"] = _encryptionDecryption.Decrypt(Convert.ToString(testModelData[0]["CreatedBy"]));
                            }
                        }
                        catch (Exception ex) { LOGGING.LogManager.Logger.LogProcessInfo(typeof(ModelEngineeringService), nameof(GetTeachModels) + "User is already decrypted....", ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty); }
                        //try
                        //{
                        //    if (testModelData[0].Contains(CONSTANTS.ModifiedByUser) && !string.IsNullOrEmpty(Convert.ToString(testModelData[0][CONSTANTS.ModifiedByUser])))
                        //    {
                        //        testModelData[0][CONSTANTS.ModifiedByUser] = _encryptionDecryption.Decrypt(testModelData[0][CONSTANTS.ModifiedByUser].AsString);
                        //    }
                        //}
                        //catch (Exception) { }
                    }
                    testModelData[0][CONSTANTS.ScenarioCase] = CONSTANTS.PAScenario;
                    JObject data = JObject.Parse(testModelData[0].ToString());
                    teachAndTestDTO.TeachtestData = data;
                }
            }
            else
            {
                var testModelCollection = _database.GetCollection<BsonDocument>(CONSTANTS.WF_TestResults);
                var filterBuilder = Builders<BsonDocument>.Filter;
                var filter = filterBuilder.Eq(CONSTANTS.CorrelationId, correlationId) & filterBuilder.Eq(CONSTANTS.WFId, WFId) & filterBuilder.Eq(CONSTANTS.Temp, CONSTANTS.True);
                var projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
                testModelData = testModelCollection.Find(filter).Project<BsonDocument>(projection).ToList();
                if (testModelData.Count > 0)
                {
                    if (DBEncryptionRequired)
                    {
                        if (testModelData[0].Contains(CONSTANTS.Predictions))
                        {
                            testModelData[0][CONSTANTS.Predictions] = BsonDocument.Parse(_encryptionDecryption.Decrypt(testModelData[0][CONSTANTS.Predictions].AsString));
                        }
                        try
                        {
                            if (testModelData[0].Contains("CreatedBy") && !string.IsNullOrEmpty(Convert.ToString(testModelData[0]["CreatedBy"])))
                            {
                                testModelData[0]["CreatedBy"] = _encryptionDecryption.Decrypt(Convert.ToString(testModelData[0]["CreatedBy"]));
                            }
                        }
                        catch (Exception ex) { LOGGING.LogManager.Logger.LogProcessInfo(typeof(ModelEngineeringService), nameof(GetTeachModels) + "User is already decrypted....", ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty); }
                        //try
                        //{
                        //    if (testModelData[0].Contains(CONSTANTS.ModifiedByUser) && !string.IsNullOrEmpty(Convert.ToString(testModelData[0][CONSTANTS.ModifiedByUser])))
                        //    {
                        //        testModelData[0][CONSTANTS.ModifiedByUser] = _encryptionDecryption.Decrypt(Convert.ToString(testModelData[0][CONSTANTS.ModifiedByUser]));
                        //    }
                        //}
                        //catch (Exception) { }
                    }
                    testModelData[0][CONSTANTS.ScenarioCase] = CONSTANTS.WFScenario;
                    JObject data = JObject.Parse(testModelData[0].ToString());
                    teachAndTestDTO.TeachtestData = data;
                }
            }

            if (IstimeSeries == CONSTANTS.True_Value)
            {
                var analysisCollection = _database.GetCollection<BsonDocument>(CONSTANTS.WhatIfAnalysis);
                var analysisfilterBuilder = Builders<BsonDocument>.Filter;
                var analysisfilter = analysisfilterBuilder.Eq(CONSTANTS.CorrelationId, correlationId) & analysisfilterBuilder.Eq(CONSTANTS.WFId, WFId);
                var analysisprojection = Builders<BsonDocument>.Projection.Include(CONSTANTS.Steps).Exclude(CONSTANTS.Id);
                testModelData = analysisCollection.Find(analysisfilter).Project<BsonDocument>(analysisprojection).ToList();
                if (testModelData.Count > 0)
                {
                    teachAndTestDTO.steps = testModelData[0][CONSTANTS.Steps].ToString();
                }
            }
            List<string> dataSource = null;
            dataSource = CommonUtility.GetDataSourceModel(correlationId, appSettings);
            if (dataSource.Count > 0)
            {
                teachAndTestDTO.ModelName = dataSource[0];
                teachAndTestDTO.DataSource = dataSource[1];
                teachAndTestDTO.ModelType = dataSource[2];
                teachAndTestDTO.BusinessProblem = dataSource[3];
                teachAndTestDTO.Category = dataSource[5];
            }
            return teachAndTestDTO;
        }
        public FeaturePredictionTestDTO GetFeaturePredictionForTest(string correlationId, string WFId, string steps)
        {
            bool DBEncryptionRequired = CommonUtility.EncryptDB(correlationId, appSettings);
            FeaturePredictionTestDTO featurePredictionTest = new FeaturePredictionTestDTO
            {
                CorrelationId = correlationId
            };
            var predictionData = new List<BsonDocument>();
            var predictionCollection = _database.GetCollection<BsonDocument>(CONSTANTS.WF_TestResults);
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq(CONSTANTS.CorrelationId, correlationId) & builder.Eq(CONSTANTS.WFId, WFId);
            if (!String.IsNullOrEmpty(steps))
            {
                var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.WFId).Include(CONSTANTS.Forecast).Include(CONSTANTS.RangeTime).Include(CONSTANTS.ScenarioName).Include(CONSTANTS.Frequency).Include(CONSTANTS.ProblemType).Include("ProblemTypeFlag").Include(CONSTANTS.Temp).Exclude(CONSTANTS.Id);
                predictionData = predictionCollection.Find(filter).Project<BsonDocument>(projection).ToList();
            }
            else
            {
                var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.WFId).Include(CONSTANTS.Predictions).Include(CONSTANTS.ScenarioName).Include(CONSTANTS.Target).Include(CONSTANTS.ProblemType).Include("ProblemTypeFlag").Include(CONSTANTS.Temp).Exclude(CONSTANTS.Id);
                predictionData = predictionCollection.Find(filter).Project<BsonDocument>(projection).ToList();
            }

            List<JObject> data = new List<JObject>();
            if (predictionData.Count > 0)
            {
                for (int i = 0; i < predictionData.Count; i++)
                {
                    if (DBEncryptionRequired)
                    {
                        if (predictionData[i].Contains(CONSTANTS.Predictions))
                        {
                            predictionData[i][CONSTANTS.Predictions] = BsonDocument.Parse(_encryptionDecryption.Decrypt(predictionData[i][CONSTANTS.Predictions].AsString));
                        }
                        //if (predictionData[i].Contains(CONSTANTS.CreatedByUser) && !string.IsNullOrEmpty(Convert.ToString(predictionData[i][CONSTANTS.CreatedByUser])))
                        //{
                        //    predictionData[i][CONSTANTS.CreatedByUser]= _encryptionDecryption.Decrypt(Convert.ToString(predictionData[i][CONSTANTS.CreatedByUser]));
                        //}
                        //if (predictionData[i].Contains(CONSTANTS.ModifiedByUser) && !string.IsNullOrEmpty(Convert.ToString(predictionData[i][CONSTANTS.ModifiedByUser])))
                        //{
                        //    predictionData[i][CONSTANTS.ModifiedByUser] = _encryptionDecryption.Decrypt(Convert.ToString(predictionData[i][CONSTANTS.ModifiedByUser]));
                        //}
                    }
                    data.Add(JObject.Parse(predictionData[i].ToString()));
                }
                JObject probabilities = new JObject();
                bool IsProbabilities = false;
                for (int i = 0; i < data.Count(); i++)
                {
                    if (predictionData[i].Contains(CONSTANTS.Predictions))
                    {
                        foreach (var item2 in data[i]["Predictions"]["Prediction0"].Children())
                        {
                            JProperty prop = item2 as JProperty;
                            if (prop != null && prop.Name.Contains("Probablities"))
                            {
                                IsProbabilities = true;
                                break;
                            }
                        }
                        if (IsProbabilities)
                        {
                            foreach (var item in data[i]["Predictions"]["Prediction0"]["Probablities"])
                            {
                                JProperty prop = item as JProperty;
                                if (prop != null)
                                {
                                    probabilities.Add(prop.Name.Replace("．", "."), prop.Value);
                                }
                            }
                            data[i]["Predictions"]["Prediction0"]["Probablities"] = JObject.FromObject(probabilities);
                        }
                    }
                }
                featurePredictionTest.PredictionData = data;
            }

            List<string> dataSource = null;
            dataSource = CommonUtility.GetDataSourceModel(correlationId, appSettings);
            if (dataSource.Count > 0)
            {
                featurePredictionTest.ModelName = dataSource[0];
                featurePredictionTest.DataSource = dataSource[1];
                featurePredictionTest.ModelType = dataSource[2];
                featurePredictionTest.BusinessProblem = dataSource[3];
                featurePredictionTest.Category = dataSource[5];
            }
            return featurePredictionTest;
        }
        public FeatureEngineeringDTO GetFeatureAttributes(string correlationId, string ServiceName = "")
        {
            servicename = ServiceName;
            IMongoCollection<BsonDocument> featureCollection;
            IMongoCollection<DeployModelsDto> deploycollection;
            IMongoCollection<BusinessProblemDataDTO> businessCollection;
            if (servicename == "Anomaly")
            {
                featureCollection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.MEFeatureSelection);
                deploycollection = _databaseAD.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
                businessCollection = _databaseAD.GetCollection<BusinessProblemDataDTO>(CONSTANTS.PSBusinessProblem);
            }
            else
            {
                featureCollection = _database.GetCollection<BsonDocument>(CONSTANTS.MEFeatureSelection);
                deploycollection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
                businessCollection = _database.GetCollection<BusinessProblemDataDTO>(CONSTANTS.PSBusinessProblem);
            }

            bool DBEncryptionRequired = CommonUtility.EncryptDB(correlationId, appSettings, servicename);
            FeatureEngineeringDTO modelEngineeringDto = new FeatureEngineeringDTO();
            List<string> featureColumns = new List<string>();
            var featureData = new List<BsonDocument>();
            //var featureCollection = _database.GetCollection<BsonDocument>(CONSTANTS.MEFeatureSelection);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            //var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.FeatureImportance).Include(CONSTANTS.Train_Test_Split).Include(CONSTANTS.KFoldValidation).Include(CONSTANTS.StratifiedSampling).Include(CONSTANTS.Split_Column).Include(CONSTANTS.Clustering_Flag).Exclude(CONSTANTS.Id);
            //added AllData_Flag,changed projection to avoid breaking for old models
            var projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
            featureData = featureCollection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (featureData.Count > 0)
            {
                if (DBEncryptionRequired)
                {
                    try
                    {
                        if (featureData[0].Contains("CreatedBy") && !string.IsNullOrEmpty(Convert.ToString(featureData[0]["CreatedBy"])))
                            featureData[0]["CreatedBy"] = _encryptionDecryption.Decrypt(Convert.ToString(featureData[0]["CreatedBy"]));
                    }
                    catch (Exception ex) { LOGGING.LogManager.Logger.LogProcessInfo(typeof(ModelEngineeringService), nameof(GetFeatureAttributes) + "User is already decrypted....", ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty); }
                    //try
                    //{
                    //    if (featureData[0].Contains("ModifiedBy") && !string.IsNullOrEmpty(Convert.ToString(featureData[0]["ModifiedBy"])))
                    //        featureData[0]["ModifiedBy"] = _encryptionDecryption.Decrypt(Convert.ToString(featureData[0]["ModifiedBy"]));
                    //}
                    //catch (Exception) { }
                }
                JObject data = JObject.Parse(featureData[0].ToString());
                modelEngineeringDto.ProcessData = data;
                modelEngineeringDto.IsModelTrained = this.IsModelTrained(correlationId);
            }

            List<string> dataSource = new List<string>();
            dataSource = CommonUtility.GetDataSourceModel(correlationId, appSettings, servicename);
            if (dataSource.Count > 0)
            {
                modelEngineeringDto.ModelName = dataSource[0];
                modelEngineeringDto.DataSource = dataSource[1];
                modelEngineeringDto.ModelType = dataSource[2];
                modelEngineeringDto.BusinessProblem = dataSource[3];
                if (!string.IsNullOrEmpty(dataSource[4]))
                {
                    modelEngineeringDto.InstaFlag = true;
                    modelEngineeringDto.Category = dataSource[5];
                }
            }
            //var deploycollection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
            var filter3 = Builders<DeployModelsDto>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var projection3 = Builders<DeployModelsDto>.Projection.Include(CONSTANTS.IsIncludedInCascade).Include(CONSTANTS.CustomCascadeId).Include(CONSTANTS.IsCascadingButton).Include(CONSTANTS.IsFMModel).Exclude(CONSTANTS.Id);
            var result = deploycollection.Find(filter3).Project<DeployModelsDto>(projection3).ToList();
            if (result.Count > 0)
            {
                modelEngineeringDto.IsCascadingButton = result[0].IsCascadingButton;
                modelEngineeringDto.IsFMModel = result[0].IsFMModel;
                if (result[0].IsIncludedInCascade && (string.IsNullOrEmpty(result[0].CustomCascadeId) || result[0].CustomCascadeId == CONSTANTS.BsonNull))
                {
                    modelEngineeringDto.IsIncludedInNormalCascade = true;
                }
            }

            //var businessCollection = _database.GetCollection<BusinessProblemDataDTO>(CONSTANTS.PSBusinessProblem);
            var filter4 = Builders<BusinessProblemDataDTO>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var projectionBusiness = Builders<BusinessProblemDataDTO>.Projection.Include(CONSTANTS.TargetUniqueIdentifier).Exclude(CONSTANTS.Id);
            var businessData = businessCollection.Find(filter4).Project<BusinessProblemDataDTO>(projectionBusiness).ToList();
            if (businessData.Count > 0)
            {                
                modelEngineeringDto.TargetUniqueIdentifier = businessData[0].TargetUniqueIdentifier;
            }
            return modelEngineeringDto;
        }

        /// <summary>
        /// Update default field to False and update the user selected one to True.
        /// </summary>
        /// <param name="data"></param>
        public void UpdateFeatures(dynamic data, string correlationId, string ServiceName = "")
        {
            IMongoCollection<BsonDocument> collection;
            IMongoCollection<BsonDocument> recommendedModelCollection;
            IMongoCollection<BsonDocument> deployCollection;
            if (ServiceName == "Anomaly")
            {
                collection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.MEFeatureSelection);
                recommendedModelCollection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.MERecommendedModels);
                deployCollection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
            }
            else
            {
                collection = _database.GetCollection<BsonDocument>(CONSTANTS.MEFeatureSelection);
                recommendedModelCollection = _database.GetCollection<BsonDocument>(CONSTANTS.MERecommendedModels);
                deployCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
            }
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            //var collection = _database.GetCollection<BsonDocument>(CONSTANTS.MEFeatureSelection);
            //var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.FeatureImportance).Include(CONSTANTS.Train_Test_Split).Include(CONSTANTS.KFoldValidation).Include(CONSTANTS.StratifiedSampling).Include(CONSTANTS.Split_Column).Exclude(CONSTANTS.Id);
            var projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
            var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                if (data.FeatureImportance != null)
                {
                    foreach (var features in data.FeatureImportance)
                    {
                        //update the user input fields
                        if (features is JProperty featuresValues)
                        {
                            string[] columnsValue = featuresValues.Value.ToString().Replace(CONSTANTS.r_n, CONSTANTS.EmptySpace).Replace(CONSTANTS.slash, CONSTANTS.InvertedComma).Replace(CONSTANTS.t, CONSTANTS.EmptySpace).Replace(CONSTANTS.openbraket, CONSTANTS.InvertedComma).Replace(CONSTANTS.closebraket, CONSTANTS.InvertedComma).Split(CONSTANTS.comma_);
                            foreach (var column in columnsValue)
                            {
                                string[] values = column.Split(CONSTANTS.colan);
                                string columnToupdate = string.Format(CONSTANTS.FeatureImportance_, features.Name, values[0].Trim());
                                var newFieldUpdate = Builders<BsonDocument>.Update.Set(columnToupdate, values[1].Trim());
                                var result2 = collection.UpdateOne(filter, newFieldUpdate);
                            }
                        }
                    }
                }
                //Update the user Training Data
                if (data.Train_Test_Split != null)
                {
                    foreach (var trainingData in data.Train_Test_Split)
                    {
                        JProperty trainingValues = trainingData as JProperty;
                        if (trainingValues != null)
                        {
                            string columnValue1 = trainingValues.ToString().Replace(CONSTANTS.r_n, CONSTANTS.EmptySpace).Replace(CONSTANTS.slash, CONSTANTS.InvertedComma).Replace(CONSTANTS.t, CONSTANTS.EmptySpace).Replace(CONSTANTS.openbraket, CONSTANTS.InvertedComma).Replace(CONSTANTS.closebraket, CONSTANTS.InvertedComma);
                            string[] values = columnValue1.Split(CONSTANTS.colan);

                            string columnToupdate = string.Format(CONSTANTS.Train_Test_Split_, trainingData.Name, values[1].Trim());
                            var newFieldUpdate = Builders<BsonDocument>.Update.Set(columnToupdate, values[2].Trim());
                            var result2 = collection.UpdateOne(filter, newFieldUpdate);
                        }
                    }
                }

                //Update the user ApplyKFoldValidation Data
                //updating SelectedKFold value
                if (data.KFoldValidation != null)
                {
                    foreach (var KFoldValidation in data.KFoldValidation)
                    {
                        if (KFoldValidation is JProperty KFoldValues)
                        {
                            foreach (var keyValues in KFoldValues.Value)
                            {
                                if (keyValues is JProperty foldValues)
                                {
                                    string columnToupdate = string.Format(CONSTANTS.KFoldValidation_, foldValues.Name.ToString(), foldValues.Name.ToString());
                                    var newFieldUpdate = Builders<BsonDocument>.Update.Set(columnToupdate, foldValues.Value.ToString());
                                    var result2 = collection.UpdateOne(filter, newFieldUpdate);
                                }
                            }
                        }
                    }
                }


                //Update the user StratifiedSampling Data
                if (data.StratifiedSampling != null)
                {
                    foreach (var stratifiedData in data.StratifiedSampling)
                    {
                        JProperty stratifiedValues = stratifiedData as JProperty;
                        if (stratifiedValues != null)
                        {
                            string columnValue1 = stratifiedValues.ToString().Replace(CONSTANTS.r_n, CONSTANTS.EmptySpace).Replace(CONSTANTS.slash, CONSTANTS.InvertedComma).Replace(CONSTANTS.t, CONSTANTS.EmptySpace).Replace(CONSTANTS.openbraket, CONSTANTS.InvertedComma).Replace(CONSTANTS.closebraket, CONSTANTS.InvertedComma);
                            string[] values = columnValue1.Split(CONSTANTS.colan);
                            var newFieldUpdate = Builders<BsonDocument>.Update.Set(CONSTANTS.StratifiedSampling, values[1].Trim());
                            var result2 = collection.UpdateOne(filter, newFieldUpdate);
                        }
                    }
                }
                //Updating Retain to True for RetrainModels
                var filter2 = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
                //var recommendedModelCollection = _database.GetCollection<BsonDocument>(CONSTANTS.MERecommendedModels);
                var result3 = recommendedModelCollection.Find(filter2).ToList();
                if (result3.Count > 0)
                {
                    var updateField = Builders<BsonDocument>.Update.Set(CONSTANTS.retrain, true);
                    var updateResult = recommendedModelCollection.UpdateOne(filter2, updateField);
                }
                //Updating AllData_Flag
                if (data.AllData_Flag != null)
                {
                    if (Convert.ToString(data.AllData_Flag) != "")
                    {
                        var updateField = Builders<BsonDocument>.Update.Set(CONSTANTS.AllData_Flag, Convert.ToBoolean(data.AllData_Flag));
                        var updateResult = collection.UpdateOne(filter, updateField);
                    }
                }
                if (data.IsCascadingButton != null)
                {
                   // var deployCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
                    var deployFilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
                    var deployUpdate = Builders<BsonDocument>.Update.Set(CONSTANTS.IsCascadingButton, Convert.ToBoolean(data.IsCascadingButton));
                    var updateResult = deployCollection.UpdateOne(deployFilter, deployUpdate);
                }
            }
        }

        /// <summary>
        /// Get the selected models from MLDL_ModelsMaster collection based on Problem Type in DE_DataCleanup. 
        /// </summary>
        /// <param name="correlationId">The correlation identifier</param>
        /// <param name="id">The identifier</param>
        /// <returns>Returns the recommended model types.</returns>
        public RecommendedAIViewModelDTO GetRecommendedAI(string correlationId, string ServiceName = "")
        {
            servicename = ServiceName;
            RecommendedAIViewModelDTO modelTypes = new RecommendedAIViewModelDTO
            {
                CorrelationId = correlationId
            };
            IMongoCollection<BsonDocument> columnCollection;
            IMongoCollection<BsonDocument> deployCollection;
            if (servicename == "Anomaly")
            {
                columnCollection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.MERecommendedModels);
                deployCollection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
            }
            else
            {
                columnCollection = _database.GetCollection<BsonDocument>(CONSTANTS.MERecommendedModels);
                deployCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
            }
            //var columnCollection = _database.GetCollection<BsonDocument>(CONSTANTS.MERecommendedModels);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
            var result = columnCollection.Find(filter).Project<BsonDocument>(projection).FirstOrDefault();
            bool DBEncryptionRequired = CommonUtility.EncryptDB(correlationId, appSettings, servicename);
            if (result != null)
            {
                if (DBEncryptionRequired)
                {
                    try
                    {
                        if (result.Contains("CreatedByUser") && !string.IsNullOrEmpty(Convert.ToString(result["CreatedByUser"])))
                        {
                            result["CreatedByUser"] = _encryptionDecryption.Decrypt(Convert.ToString(result["CreatedByUser"]));
                        }
                    }
                    catch (Exception ex) { LOGGING.LogManager.Logger.LogProcessInfo(typeof(ModelEngineeringService), nameof(GetRecommendedAI) + "User is already decrypted....", ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty); }
                    try
                    {
                        if (result.Contains("ModifiedByUser") && !string.IsNullOrEmpty(Convert.ToString(result["ModifiedByUser"])))
                        {
                            result["ModifiedByUser"] = _encryptionDecryption.Decrypt(Convert.ToString(result["ModifiedByUser"]));
                        }
                    }
                    catch (Exception ex) { LOGGING.LogManager.Logger.LogProcessInfo(typeof(ModelEngineeringService), nameof(GetRecommendedAI) + "User is already decrypted....", ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty); }
                }
                modelTypes.SelectedModels = JObject.Parse(result.ToString());
                //var deployCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
                var updateField = Builders<BsonDocument>.Update.Set(CONSTANTS.ModelType, result[CONSTANTS.ProblemType].ToString().Trim());
                var updateResult = deployCollection.UpdateOne(filter, updateField);
            }

            List<string> datasource = new List<string>();
            datasource = CommonUtility.GetDataSourceModel(correlationId, appSettings, servicename);
            if (datasource.Count > 0)
            {
                modelTypes.ModelName = datasource[0];
                modelTypes.DataSource = datasource[1];
                modelTypes.ModelType = datasource[2];
                modelTypes.BusinessProblems = datasource[3];
                if (!string.IsNullOrEmpty(datasource[4]))
                {
                    modelTypes.InstaFlag = true;
                    modelTypes.Category = datasource[5];
                }
            }

            return modelTypes;
        }

        /// <summary>
        /// Updates the recommended model types.
        /// </summary>
        /// <param name="data">The data</param>
        /// <param name="correlationId">The correlation identifier</param>
        /// <returns>Returns the result for updation</returns>
        public void UpdateRecommendedModelTypes(dynamic data, string correlationId, string ServiceName = "")
        {
            IMongoCollection<BsonDocument> collection;
            if (ServiceName == "Anomaly")
                collection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.MERecommendedModels);
            else
                collection = _database.GetCollection<BsonDocument>(CONSTANTS.MERecommendedModels);
            //var collection = _database.GetCollection<BsonDocument>(CONSTANTS.MERecommendedModels);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var project = Builders<BsonDocument>.Projection.Include(CONSTANTS.Selected_Models).Include(CONSTANTS.CorrelationId);
            var result = collection.Find(filter).Project<BsonDocument>(project).ToList();
            if (result.Count > 0)
            {
                if (data != null && data.SelectedModels != null)
                {
                    foreach (var selectedModel in data.SelectedModels)
                    {
                        JProperty model = selectedModel as JProperty;
                        if (model != null)
                        {
                            string columnValue = model.Value.ToString().Replace(CONSTANTS.r_n, CONSTANTS.EmptySpace).Replace(CONSTANTS.slash, CONSTANTS.InvertedComma).Replace(CONSTANTS.t, CONSTANTS.EmptySpace).Replace(CONSTANTS.openbraket, CONSTANTS.InvertedComma).Replace(CONSTANTS.closebraket, CONSTANTS.InvertedComma);
                            string[] values = columnValue.Split(CONSTANTS.colan);
                            string columnToUpdate = string.Format(CONSTANTS.SelectedModels, selectedModel.Name, values[0].Trim());
                            var newFieldUpdate = Builders<BsonDocument>.Update.Set(columnToUpdate, values[1].Trim());
                            var outcome = collection.UpdateOne(filter, newFieldUpdate);
                        }
                    }
                }

            }
        }

        /// <summary>
        /// Get the recommended model types
        /// </summary>
        /// <param name="correlationId">The correlation identifier</param>
        /// <returns>Returns the recommended model types.</returns>       
        public RecommedAITrainedModel GetRecommendedTrainedModels(string correlationId, string approach, string ServiceName = "")
        {
            servicename = ServiceName;
            IMongoCollection<BsonDocument> meCollection;
            IMongoCollection<BsonDocument> columnCollection;
            IMongoCollection<BsonDocument> me_Collection;
            if (servicename == "Anomaly")
            {
                meCollection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.MERecommendedModels);
                columnCollection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.SSAIRecommendedTrainedModels);
                me_Collection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.ME_RecommendedModels);
            }
            else
            {
                meCollection = _database.GetCollection<BsonDocument>(CONSTANTS.MERecommendedModels);
                columnCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIRecommendedTrainedModels);
                me_Collection = _database.GetCollection<BsonDocument>(CONSTANTS.ME_RecommendedModels);
            }
            RecommedAITrainedModel trainedModels = new RecommedAITrainedModel();
            // Take the IsInitiateRetrain flag
            //var meCollection = _database.GetCollection<BsonDocument>(CONSTANTS.MERecommendedModels);
            var mefilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var meprojection = Builders<BsonDocument>.Projection.Include(CONSTANTS.IsInitiateRetrain).Exclude(CONSTANTS.Id);
            var meResult = meCollection.Find(mefilter).Project<BsonDocument>(meprojection).ToList();
            if (meResult.Count > 0)
            {
                if (meResult[0].ToString() != "{ }")
                {
                    if (meResult[0][CONSTANTS.IsInitiateRetrain].ToString() != null)
                    {
                        trainedModels.IsInitiateRetrain = Convert.ToBoolean(meResult[0][CONSTANTS.IsInitiateRetrain]);
                        if (trainedModels.IsInitiateRetrain)
                            return trainedModels;
                    }
                }
            }
            List<JObject> trainModelsList = new List<JObject>();
            if (servicename != "Anomaly")
                trainedModels.SelectedModel = this.GetSelectedModel(correlationId);
            //var columnCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIRecommendedTrainedModels);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            List<BsonDocument> trainedModel;
            if (approach == CONSTANTS.InstaML)
            {
                trainedModel = columnCollection.Find(filter).ToList();
            }
            else
            {
                var projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id).Exclude("visualization");
                trainedModel = columnCollection.Find(filter).Project<BsonDocument>(projection).ToList();
            }
            if (trainedModel.Count() > 0)
            {
                double totalRuntime = 0;
                bool versionAvaialble = false;
                foreach (var item in trainedModel)
                {
                    if (item.TryGetElement("Version", out BsonElement bson))
                    {
                        if (!item["Version"].IsBsonNull && !string.IsNullOrEmpty(Convert.ToString(item["Version"])))
                        {
                            int val = Convert.ToInt32(item["Version"]);
                            if (val == 1)
                            {
                                versionAvaialble = true;
                                break;
                            }
                        }
                    }
                }
                bool DbEncryptiuonRequired = CommonUtility.EncryptDB(correlationId, appSettings, servicename);
                for (int i = 0; i < trainedModel.Count; i++)
                {
                    if (DbEncryptiuonRequired)
                    {
                        try
                        {
                            if (trainedModel[i].Contains(CONSTANTS.CreatedByUser) && !string.IsNullOrEmpty(Convert.ToString(trainedModel[i][CONSTANTS.CreatedByUser])))
                            {
                                trainedModel[i][CONSTANTS.CreatedByUser] = _encryptionDecryption.Decrypt(Convert.ToString(trainedModel[i][CONSTANTS.CreatedByUser]));
                            }
                        }
                        catch (Exception ex) { LOGGING.LogManager.Logger.LogProcessInfo(typeof(ModelEngineeringService), nameof(GetRecommendedTrainedModels) + "User is already decrypted....", ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty); }

                    }
                    if (trainedModel[i]["pageInfo"] == "RecommendedAI" || trainedModel[i]["pageInfo"] == CONSTANTS.RetrainRecommendedAI)
                    {
                        JObject parsedData = JObject.Parse(trainedModel[i].ToString());
                        if (versionAvaialble)
                        {
                            if (((Newtonsoft.Json.Linq.JValue)parsedData["Version"]).Value != null && Convert.ToInt32(parsedData["Version"]) == 1)
                            {
                                trainModelsList.Add(parsedData);
                                totalRuntime += Convert.ToDouble(trainedModel[i]["RunTime"]);
                            }
                        }
                        else
                        {
                            trainModelsList.Add(JObject.Parse(trainedModel[i].ToString()));
                            totalRuntime += Convert.ToDouble(trainedModel[i]["RunTime"]);
                        }
                    }
                }
                //var me_Collection = _database.GetCollection<BsonDocument>(CONSTANTS.ME_RecommendedModels);
                var me_projection = Builders<BsonDocument>.Projection.Include("CPUUsage").Include("CurrentProgress").Include(CONSTANTS.IsInitiateRetrain).Exclude(CONSTANTS.Id);
                var me_Result = me_Collection.Find(filter).Project<BsonDocument>(me_projection).ToList();
                if (me_Result.Count() > 0)
                {
                    if (me_Result[0].AsBsonDocument.TryGetElement("CPUUsage", out BsonElement bson))
                    {
                        trainedModels.CPUUsage = Convert.ToDouble(me_Result[0]["CPUUsage"]);
                        trainedModels.CurrentProgress = Convert.ToDouble(me_Result[0]["CurrentProgress"]);
                    }
                }
                trainedModels.EstimatedRunTime = totalRuntime;
                trainedModels.TrainedModel = trainModelsList;
            }

            List<string> datasource = new List<string>();
            datasource = CommonUtility.GetDataSourceModel(correlationId, appSettings, servicename);
            if (datasource.Count > 0)
            {
                trainedModels.ModelName = datasource[0];
                trainedModels.DataSource = datasource[1];
                trainedModels.ModelType = datasource[2];
                if (!string.IsNullOrEmpty(datasource[4]))
                {
                    trainedModels.InstaFlag = true;
                    trainedModels.Category = datasource[5];
                }
                trainedModels.BusinessProblems = datasource.Count > 1 ? datasource[3] : null;
            }

            return trainedModels;
        }
        /// <summary>
        /// Checking the model is retraining and deleting the old requests if IsInitiateRetrain true
        /// </summary>
        /// <param name="correlationId"></param>
        /// <returns></returns>
        public bool IsInitiateRetrain(string correlationId, string ServiceName = "")
        {
            bool isInitiateRetrain = false;
            IMongoCollection<BsonDocument> collection;
            IMongoCollection<BsonDocument> collection2;
            if (ServiceName == "Anomaly")
            {
                collection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.MERecommendedModels);
                collection2 = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
            }
            else
            {
                collection = _database.GetCollection<BsonDocument>(CONSTANTS.MERecommendedModels);
                collection2 = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
            }
            //var collection = _database.GetCollection<BsonDocument>(CONSTANTS.MERecommendedModels);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.IsInitiateRetrain).Exclude(CONSTANTS.Id);
            var result = collection.Find(filter).FirstOrDefault();
            if (result != null)
            {
                if (result.Contains(CONSTANTS.IsInitiateRetrain))
                    isInitiateRetrain = Convert.ToBoolean(result[CONSTANTS.IsInitiateRetrain]);
            }
            if (isInitiateRetrain)
            {
               // var collection2 = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
                var builder2 = Builders<BsonDocument>.Filter;
                var filter2 = builder2.Eq(CONSTANTS.CorrelationId, correlationId) & (builder2.Eq(CONSTANTS.pageInfo, CONSTANTS.RecommendedAI) | builder2.Eq(CONSTANTS.pageInfo, CONSTANTS.RetrainRecommendedAI));
                collection2.DeleteMany(filter2);
            }
            return isInitiateRetrain;
        }
        public List<IngrainRequestQueue> GetMultipleRequestStatus(string correlationId, string pageInfo, string ServiceName = "")
        {
            IMongoCollection<IngrainRequestQueue> collection;
            if (ServiceName == "Anomaly")
                collection = _databaseAD.GetCollection<IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
            else
                collection = _database.GetCollection<IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
            bool DbEncryptiuonRequired = CommonUtility.EncryptDB(correlationId, appSettings, ServiceName);
            List<IngrainRequestQueue> ingrainRequest = new List<IngrainRequestQueue>();
            //var collection = _database.GetCollection<IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
            var builder = Builders<IngrainRequestQueue>.Filter;
            if (pageInfo == CONSTANTS.RecommendedAI)
            {
                var filter = builder.Eq(CONSTANTS.CorrelationId, correlationId) & (builder.Eq(CONSTANTS.pageInfo, pageInfo) | builder.Eq(CONSTANTS.pageInfo, CONSTANTS.RetrainRecommendedAI));
                ingrainRequest = collection.Find(filter).ToList();
            }
            else
            {
                var filter = builder.Eq(CONSTANTS.CorrelationId, correlationId) & builder.Eq(CONSTANTS.pageInfo, pageInfo);
                ingrainRequest = collection.Find(filter).ToList();
            }
            if (ingrainRequest.Count > 0)
            {
                if (DbEncryptiuonRequired)
                {
                    for (int i = 0; i < ingrainRequest.Count; i++)
                    {
                        try
                        {
                            if (!string.IsNullOrEmpty(Convert.ToString(ingrainRequest[i].CreatedByUser)))
                                ingrainRequest[i].CreatedByUser = _encryptionDecryption.Decrypt(Convert.ToString(ingrainRequest[i].CreatedByUser));
                        }
                        catch (Exception ex) { LOGGING.LogManager.Logger.LogProcessInfo(typeof(ModelEngineeringService), nameof(GetMultipleRequestStatus) + "User is already decrypted....", ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty); }
                        try
                        {
                            if (!string.IsNullOrEmpty(Convert.ToString(ingrainRequest[i].ModifiedByUser)))
                                ingrainRequest[i].ModifiedByUser= _encryptionDecryption.Decrypt(Convert.ToString(ingrainRequest[i].ModifiedByUser));
                        }
                        catch (Exception ex) { LOGGING.LogManager.Logger.LogProcessInfo(typeof(ModelEngineeringService), nameof(GetMultipleRequestStatus) + "User is already decrypted....", ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty); }
                    }
                }
            }
            return ingrainRequest;
        }
        public RetraingStatus GetRetrain(string correlationId, string ServiceName = "")
        {
            IMongoCollection<BsonDocument> collection;
            if (ServiceName == "Anomaly")
                collection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.MERecommendedModels);
            else
                collection = _database.GetCollection<BsonDocument>(CONSTANTS.MERecommendedModels);
            RetraingStatus retrain = new RetraingStatus();
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
           // var collection = _database.GetCollection<BsonDocument>(CONSTANTS.MERecommendedModels);
            var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.retrain).Include(CONSTANTS.IsInitiateRetrain).Exclude(CONSTANTS.Id);
            var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                retrain.Retrain = Convert.ToBoolean(result[0][CONSTANTS.retrain]);
                if (result[0].Contains(CONSTANTS.IsInitiateRetrain))
                    retrain.IsIntiateRetrain = Convert.ToBoolean(result[0][CONSTANTS.IsInitiateRetrain]);
            }
            return retrain;
        }
        public string DeleteExistingModels(string correlationId, string pageInfo)
        {
            string message = string.Empty;
            string problemType = string.Empty;
            //UseCase Deletion
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq(CONSTANTS.CorrelationId, correlationId) & ((builder.Eq(CONSTANTS.pageInfo, CONSTANTS.RecommendedAI)) | (builder.Eq(CONSTANTS.pageInfo, CONSTANTS.ModelTraining)) | (builder.Eq(CONSTANTS.pageInfo, CONSTANTS.WFTeachTest)));
            var useCaseCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
            var deletedResult = useCaseCollection.DeleteMany(filter);
            if (deletedResult.DeletedCount > 0)
            {
                message = CONSTANTS.Success;
            }

            //RecommendedTrainedModels deletion
            var filter2 = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var trainedModelsCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIRecommendedTrainedModels);
            var modelResult = trainedModelsCollection.DeleteMany(filter2);
            if (modelResult.DeletedCount > 0)
            {
                message = CONSTANTS.Success;
            }
            // Change deployed models to In-progress
            var deployedModelCollection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
            var filter3 = Builders<DeployModelsDto>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var resultofDeployedModel = deployedModelCollection.Find(filter3).ToList();
            if (resultofDeployedModel.Count > 0)
            {
                problemType = resultofDeployedModel[0].ModelType;
            }
            string[] arr = new string[] { };
            DeployModelsDto deployModel = new DeployModelsDto
            {
                Status = CONSTANTS.InProgress,
                DeployedDate = null,
                ModelVersion = null,
                ModelType = null,
                VDSLink = null,
                InputSample = null,
                IsPrivate = false,
                IsModelTemplate = true,
                TrainedModelId = null,
                ModelURL = null
            };
            var updateBuilder = Builders<DeployModelsDto>.Update;
            var update = updateBuilder.Set(CONSTANTS.Accuracy, deployModel.Accuracy)
                .Set(CONSTANTS.ModelURL, deployModel.ModelURL)
                .Set(CONSTANTS.VDSLink, deployModel.VDSLink)
                .Set(CONSTANTS.Status, deployModel.Status)
                .Set(CONSTANTS.WebServices, deployModel.WebServices)
                .Set(CONSTANTS.DeployedDate, deployModel.DeployedDate)
                .Set(CONSTANTS.IsPrivate, true)
                .Set(CONSTANTS.IsModelTemplate, false)
                .Set(CONSTANTS.ModelVersion, deployModel.ModelVersion)
                .Set("IsUpdated", "True");
            var result = deployedModelCollection.UpdateMany(filter3, update);

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

            //Resetting the Train models to default at ME_RecommendedModels Collection
            var recommendedModelsCollection = _database.GetCollection<BsonDocument>(CONSTANTS.MERecommendedModels);
            var modelsProjection = Builders<BsonDocument>.Projection.Include(CONSTANTS.Selected_Models).Exclude(CONSTANTS.Id);
            var modelsFilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var recommendedModelsResult = recommendedModelsCollection.Find(modelsFilter).Project<BsonDocument>(modelsProjection).ToList();
            JObject recommendedObject = new JObject();
            if (recommendedModelsResult.Count > 0)
            {
                switch (problemType)
                {
                    case CONSTANTS.Classification:
                        string turnOnclsModels = appSettings.Value.Ingrain_Classification_OnModels;
                        string[] clsOnModels = turnOnclsModels.Split(CONSTANTS.comma_);
                        foreach (var item in clsOnModels)
                        {
                            message = updateDefaultModelsOn(item, correlationId);

                        }
                        string turnoffnclsModels = appSettings.Value.Ingrain_Classification_OffModels;
                        string[] clsOffModels = turnoffnclsModels.Split(CONSTANTS.comma_);
                        foreach (var item in clsOffModels)
                        {
                            message = updateDefaultModelsOff(item, correlationId);
                        }
                        break;
                    case CONSTANTS.TimeSeries:
                        string turnOnTSModels = appSettings.Value.Ingrain_TS_OnModels;
                        string[] tsmodels = turnOnTSModels.Split(CONSTANTS.comma_);
                        foreach (var item in tsmodels)
                        {
                            message = updateDefaultModelsOn(item, correlationId);
                        }
                        string turnOffTSModels = appSettings.Value.Ingrain_TS_OffModels;
                        string[] tsoffmodels = turnOffTSModels.Split(CONSTANTS.comma_);
                        foreach (var item in tsoffmodels)
                        {
                            message = updateDefaultModelsOff(item, correlationId);
                        }
                        break;
                    case CONSTANTS.Regression:
                        string turnOnregModels = appSettings.Value.Ingrain_Regresison_OnModels;
                        string[] regOnmodels = turnOnregModels.Split(CONSTANTS.comma_);
                        foreach (var item in regOnmodels)
                        {
                            message = updateDefaultModelsOn(item, correlationId);
                        }
                        string turnOffregModels = appSettings.Value.Ingrain_Regresison_OffModels;
                        string[] regOffmodels = turnOffregModels.Split(CONSTANTS.comma_);
                        foreach (var item in regOffmodels)
                        {
                            message = updateDefaultModelsOff(item, correlationId);
                        }
                        break;
                    case CONSTANTS.Multi_Class:
                        string turnOnMlcModels = appSettings.Value.Ingrain_MultiClass_OnModels;
                        string[] multiOnmodels = turnOnMlcModels.Split(CONSTANTS.comma_);
                        foreach (var item in multiOnmodels)
                        {
                            message = updateDefaultModelsOn(item, correlationId);
                        }

                        string turnOffmlcModels = appSettings.Value.Ingrain_MultiClass_OffModels;
                        string[] multioffModels = turnOffmlcModels.Split(CONSTANTS.comma_);
                        foreach (var item in multioffModels)
                        {
                            message = updateDefaultModelsOff(item, correlationId);
                        }
                        break;
                }
            }

            //Bug Fix by Shreya - Delete old Teach and Test Results
            var teachTestCollection = _database.GetCollection<BsonDocument>(CONSTANTS.WF_TestResults);
            var teachTestFilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var deleteSavedTest = teachTestCollection.DeleteMany(teachTestFilter);

            //Bug fix by Shreya - Delete old Saved Hyper Tune 
            var hyperTuneCollection = _database.GetCollection<BsonDocument>(CONSTANTS.ME_HyperTuneVersion);
            var hyperTuneFilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var deleteSavedHyperTune = hyperTuneCollection.DeleteMany(hyperTuneFilter);

            //Delete old PA Saved Scenaio 
            var pACollection = _database.GetCollection<BsonDocument>(CONSTANTS.PrescriptiveAnalyticsResults);
            var pAFilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var deleteSavedPA = pACollection.DeleteMany(pAFilter);

            return message;
        }

        public void UpdateIsRetrainFlag(string correlationId, string ServiceName = "")
        {
            //update IsInitiateRetrain
            IMongoCollection<BsonDocument> recommendedModelsCollection;
            if (ServiceName == "Anomaly")
                recommendedModelsCollection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.MERecommendedModels);
            else
                recommendedModelsCollection = _database.GetCollection<BsonDocument>(CONSTANTS.MERecommendedModels);
            //var recommendedModelsCollection = _database.GetCollection<BsonDocument>(CONSTANTS.MERecommendedModels);
            var modelsFilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var updateModel = Builders<BsonDocument>.Update.Set(CONSTANTS.IsInitiateRetrain, false);
            recommendedModelsCollection.UpdateOne(modelsFilter, updateModel);

        }
        public string UpdateExistingModels(string correlationId, string pageInfo)
        {
            string message = string.Empty;
            //update iSIntiateRetrain
            var recommendedModelsCollection = _database.GetCollection<BsonDocument>(CONSTANTS.MERecommendedModels);
            var modelsFilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var updateModel = Builders<BsonDocument>.Update.Set(CONSTANTS.IsInitiateRetrain, true);
            recommendedModelsCollection.UpdateOne(modelsFilter, updateModel);

            //fetch problemType
            string problemType = string.Empty;
            var deployedModelCollection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
            var filter3 = Builders<DeployModelsDto>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var resultofDeployedModel = deployedModelCollection.Find(filter3).ToList();
            if (resultofDeployedModel.Count > 0)
            {
                //updating model status to inprogress on retraining the model
                var updateStatus = Builders<DeployModelsDto>.Update.Set(CONSTANTS.Status, CONSTANTS.InProgress).Set("IsUpdated","True");
                deployedModelCollection.UpdateOne(filter3, updateStatus);
                problemType = resultofDeployedModel[0].ModelType;
            }
            //Resetting the Train models to default at ME_RecommendedModels Collection
            var modelsProjection = Builders<BsonDocument>.Projection.Include(CONSTANTS.Selected_Models).Exclude(CONSTANTS.Id);
            var recommendedModelsResult = recommendedModelsCollection.Find(modelsFilter).Project<BsonDocument>(modelsProjection).ToList();
            if (recommendedModelsResult.Count > 0)
            {
                switch (problemType)
                {
                    case CONSTANTS.Classification:
                        string turnOnclsModels = appSettings.Value.Ingrain_Classification_OnModels;
                        string[] clsOnModels = turnOnclsModels.Split(CONSTANTS.comma_);
                        foreach (var item in clsOnModels)
                        {
                            message = updateDefaultModelsOn(item, correlationId);

                        }
                        string turnoffnclsModels = appSettings.Value.Ingrain_Classification_OffModels;
                        string[] clsOffModels = turnoffnclsModels.Split(CONSTANTS.comma_);
                        foreach (var item in clsOffModels)
                        {
                            message = updateDefaultModelsOff(item, correlationId);
                        }
                        break;
                    case CONSTANTS.TimeSeries:
                        string turnOnTSModels = appSettings.Value.Ingrain_TS_OnModels;
                        string[] tsmodels = turnOnTSModels.Split(CONSTANTS.comma_);
                        foreach (var item in tsmodels)
                        {
                            message = updateDefaultModelsOn(item, correlationId);
                        }
                        string turnOffTSModels = appSettings.Value.Ingrain_TS_OffModels;
                        string[] tsoffmodels = turnOffTSModels.Split(CONSTANTS.comma_);
                        foreach (var item in tsoffmodels)
                        {
                            message = updateDefaultModelsOff(item, correlationId);
                        }
                        break;
                    case CONSTANTS.Regression:
                        string turnOnregModels = appSettings.Value.Ingrain_Regresison_OnModels;
                        string[] regOnmodels = turnOnregModels.Split(CONSTANTS.comma_);
                        foreach (var item in regOnmodels)
                        {
                            message = updateDefaultModelsOn(item, correlationId);
                        }
                        string turnOffregModels = appSettings.Value.Ingrain_Regresison_OffModels;
                        string[] regOffmodels = turnOffregModels.Split(CONSTANTS.comma_);
                        foreach (var item in regOffmodels)
                        {
                            message = updateDefaultModelsOff(item, correlationId);
                        }
                        break;
                    case CONSTANTS.Multi_Class:
                        string turnOnMlcModels = appSettings.Value.Ingrain_MultiClass_OnModels;
                        string[] multiOnmodels = turnOnMlcModels.Split(CONSTANTS.comma_);
                        foreach (var item in multiOnmodels)
                        {
                            message = updateDefaultModelsOn(item, correlationId);
                        }

                        string turnOffmlcModels = appSettings.Value.Ingrain_MultiClass_OffModels;
                        string[] multioffModels = turnOffmlcModels.Split(CONSTANTS.comma_);
                        foreach (var item in multioffModels)
                        {
                            message = updateDefaultModelsOff(item, correlationId);
                        }
                        break;
                }
            }

             //Bug Fix by Shreya - Delete old Teach and Test Results
            var teachTestCollection = _database.GetCollection<BsonDocument>(CONSTANTS.WF_TestResults);            
            teachTestCollection.DeleteMany(modelsFilter);

            //Bug fix by Shreya - Delete old Saved Hyper Tune 
            var hyperTuneCollection = _database.GetCollection<BsonDocument>(CONSTANTS.ME_HyperTuneVersion);            
            hyperTuneCollection.DeleteMany(modelsFilter);

            //Delete old PA Saved Scenaio 
            var pACollection = _database.GetCollection<BsonDocument>(CONSTANTS.PrescriptiveAnalyticsResults);            
            pACollection.DeleteMany(modelsFilter);
            
            return message;
        }

        private string updateDefaultModelsOn(string modelName, string correlationId)
        {
            string message = string.Empty;
            var recommendedModelsCollection = _database.GetCollection<BsonDocument>(CONSTANTS.MERecommendedModels);
            var modelsFilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var columnToUpdate = string.Format(CONSTANTS.SelectedModels_Train_model, modelName);
            var updateModel = Builders<BsonDocument>.Update.Set(columnToUpdate, CONSTANTS.True);
            recommendedModelsCollection.UpdateOne(modelsFilter, updateModel);
            message = CONSTANTS.Success;
            return message;
        }
        private string updateDefaultModelsOff(string modelName, string correlationId)
        {
            string message = string.Empty;
            var recommendedModelsCollection = _database.GetCollection<BsonDocument>(CONSTANTS.MERecommendedModels);
            var modelsFilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var columnToUpdate = string.Format(CONSTANTS.SelectedModels_Train_model, modelName);
            var updateModel = Builders<BsonDocument>.Update.Set(columnToUpdate, CONSTANTS.False);
            recommendedModelsCollection.UpdateOne(modelsFilter, updateModel);
            message = CONSTANTS.Success;
            return message;
        }
        public void DeleteTrainedModel(string correlationId)
        {
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIRecommendedTrainedModels);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var correaltionExist = collection.Find(filter).ToList();
            if (correaltionExist.Count > 0)
            {
                collection.DeleteOne(filter);
            }
        }

        public string GetSelectedModel(string correlationId)
        {
            IMongoCollection<DataCleanUpDto> problemCollection;
            if (servicename == "Anomaly")
                problemCollection = _databaseAD.GetCollection<DataCleanUpDto>(CONSTANTS.DEDataCleanup);
            else
                problemCollection = _database.GetCollection<DataCleanUpDto>(CONSTANTS.DEDataCleanup);
            //var problemCollection = _database.GetCollection<DataCleanUpDto>(CONSTANTS.DEDataCleanup);
            var filterRecord = Builders<DataCleanUpDto>.Filter.Eq(x => x.CorrelationId, correlationId);
            var applyProjection = Builders<DataCleanUpDto>.Projection.Include(x => x.Target_ProblemType).Include(x => x.CorrelationId).Exclude(x => x._id);
            var dataCleanup = problemCollection.Find(filterRecord).Project<DataCleanUpDto>(applyProjection).FirstOrDefault();
            string modelType = string.Empty;
            if (dataCleanup != null && dataCleanup.Target_ProblemType > 0)
            {
                switch (dataCleanup.Target_ProblemType)
                {
                    case 1:
                        modelType = CONSTANTS.Regression;
                        break;

                    case 2:
                    case 3:
                        modelType = CONSTANTS.Classification;
                        break;
                    case 4:
                        modelType = CONSTANTS.TimeSeries;
                        break;
                }
            }

            return modelType;
        }

        public void InsertColumns(WhatIFAnalysis data)
        {
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, data.CorrelationId);
            data._id = Guid.NewGuid().ToString();
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.WhatIfAnalysis);
            var jsonColumns = Newtonsoft.Json.JsonConvert.SerializeObject(data);
            var insertBsonColumns = BsonSerializer.Deserialize<BsonDocument>(jsonColumns);
            collection.InsertOne(insertBsonColumns);
        }

        public string RemoveColumns(string correlationId, string[] prescriptionColumns)
        {
            var columnCollection = _database.GetCollection<BsonDocument>(CONSTANTS.MEFeatureSelection);
            string test = string.Empty;

            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var result = columnCollection.Find(filter).ToList();
            StringBuilder resultReport = new StringBuilder();
            foreach (var columns in prescriptionColumns)
            {
                string field = string.Format(CONSTANTS.FeatureImportance_Selection, columns);
                var update = Builders<BsonDocument>.Update.Set(field, CONSTANTS.False);
                columnCollection.UpdateMany(filter, update);
                resultReport.Append(CONSTANTS.Updatedfor + columns + CONSTANTS.comma);
                //var script = @"db.ME_FeatureSelection.update({ CorrelationId: " + qoute + correlationId + qoute + " }, { $set : { " + qoute + "FeatureImportance." + columns + ".Selection" + qoute + " : " + qoute + "False" + qoute + "}})";
                //var doc = new BsonDocument() { { "eval", script } };
                //var command = new BsonDocumentCommand<BsonDocument>(doc);
                //string response = _database.RunCommand(command).ToString().Replace("\"", "");
                //if (response.Contains("nModified : 1.0"))
                //{
                //    resultReport.Append("Updated for: " + columns + ",");
                //}
            }
            test = resultReport.ToString();
            return test;
        }

        /// <summary>
        /// Gets the ingested data based on correlation id
        /// </summary>
        /// <param name="correlationId">The correlation identifier</param>
        /// <returns>Returns the matching record ColumnUniqueValues from ME_FeatureSelection & WF_IngestedData collection.</returns>
        public TeachAndTestDTO GetIngestedData(string correlationId)
        {
            TeachAndTestDTO teachAndTestDTO = new TeachAndTestDTO();
            var featureData = new List<BsonDocument>();
            var filtereddata = new List<BsonDocument>();
            var filtereddata_text = new List<BsonDocument>();
            var featureCollection = _database.GetCollection<BsonDocument>(CONSTANTS.MEFeatureSelection);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var filter2 = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.FeatureImportance).Include(CONSTANTS.NLP_Flag).Exclude(CONSTANTS.Id);
            featureData = featureCollection.Find(filter2).Project<BsonDocument>(projection).ToList();
            JObject serializeData = new JObject();
            List<string> featuresList = new List<string>();
            Dictionary<string, string> featuresListSorted = new Dictionary<string, string>();
            List<string> uniqueColumns = new List<string>();

            var typesColumns = new Dictionary<string, string>();

            var uniqueDictionary = new Dictionary<string, Dictionary<string, string>>();

            var filteredCollection = _database.GetCollection<BsonDocument>(CONSTANTS.WF_IngestedData);
            var projection2 = Builders<BsonDocument>.Projection.Include(CONSTANTS.ColumnUniqueValues).Include(CONSTANTS.DataTypes).Exclude(CONSTANTS.Id);
            filtereddata = filteredCollection.Find(filter).Project<BsonDocument>(projection2).ToList();
            bool DBEncryptionRequired = CommonUtility.EncryptDB(correlationId, appSettings);
            if (featureData.Count > 0)
            {

                serializeData = JObject.Parse(featureData[0].ToString());
                //Taking all the Columns
                foreach (var features in serializeData[CONSTANTS.FeatureImportance].Children())
                {
                    JProperty j = features as JProperty;
                    featuresList.Add(j.Name);
                }
            }
            if (filtereddata.Count > 0)
            {
                if (DBEncryptionRequired)
                {
                    filtereddata[0][CONSTANTS.ColumnUniqueValues] = BsonDocument.Parse(_encryptionDecryption.Decrypt(filtereddata[0][CONSTANTS.ColumnUniqueValues].AsString));
                    //if ( !string.IsNullOrEmpty(Convert.ToString(filtereddata[0][CONSTANTS.CreatedByUser])))
                    //{
                    //    filtereddata[0][CONSTANTS.CreatedByUser] = _encryptionDecryption.Decrypt(Convert.ToString(filtereddata[0][CONSTANTS.CreatedByUser]));
                    //}
                    //if (!string.IsNullOrEmpty(Convert.ToString(filtereddata[0][CONSTANTS.ModifiedByUser])))
                    //{
                    //    filtereddata[0][CONSTANTS.ModifiedByUser] = _encryptionDecryption.Decrypt(Convert.ToString(filtereddata[0][CONSTANTS.ModifiedByUser]));
                    //}
                }
                var parsestring = Newtonsoft.Json.JsonConvert.SerializeObject(BsonTypeMapper.MapToDotNetValue(filtereddata[0]));
                serializeData = JObject.Parse(parsestring);
                foreach (var type in serializeData[CONSTANTS.DataTypes].Children())
                {
                    JProperty j = type as JProperty;
                    typesColumns.Add(j.Name, j.Value.ToString());
                }
            }

            foreach (var item in featuresList)
            {
                var datafeature = new Dictionary<string, string>();
                List<string> floatList = new List<string>();
                List<string> intList = new List<string>();
                if (serializeData[CONSTANTS.ColumnUniqueValues][item] != null)
                {
                    foreach (JToken attributes in serializeData[CONSTANTS.ColumnUniqueValues][item].Children())
                    {
                        string a;//= attributes.ToString();
                        if (typesColumns.ContainsKey(item) & typesColumns.TryGetValue(item, out a))
                        {
                            if (a == CONSTANTS.float64)
                            {
                                floatList.Add(attributes.ToString());
                            }
                            else if (a == CONSTANTS.int64)
                            { intList.Add(attributes.ToString()); }
                            else
                            {
                                datafeature.Add(attributes.ToString(), CONSTANTS.False);
                            }
                        }
                    }
                    if (intList.Count > 0)
                    {
                        var max = intList.Select(v => int.Parse(v)).Max();
                        var min = intList.Select(v => int.Parse(v)).Min();
                        datafeature.Add(CONSTANTS.MaxintValue + max, CONSTANTS.False);
                        datafeature.Add(CONSTANTS.MinintValue + min, CONSTANTS.False);
                    }
                    if (floatList.Count > 0)
                    {
                        var max = floatList.Select(v => float.Parse(v)).Max();
                        var min = floatList.Select(v => float.Parse(v)).Min();
                        datafeature.Add(CONSTANTS.MaxfloatValue + max, CONSTANTS.False);
                        datafeature.Add(CONSTANTS.MinfloatValue + min, CONSTANTS.False);
                    }
                    uniqueDictionary.Add(item, datafeature);
                }

            }
            //Adddedto fetch Text Columns
            string NLPFlag = CONSTANTS.False_value;
            List<string> textvalues = new List<string>();
            if (featureData[0].Contains(CONSTANTS.NLP_Flag))
            {
                NLPFlag = featureData[0][CONSTANTS.NLP_Flag].ToString();
            }
            teachAndTestDTO.NLP_Flag = NLPFlag;
            if (NLPFlag == CONSTANTS.True_Value)
            {
                var filteredCollection1 = _database.GetCollection<BsonDocument>(CONSTANTS.DE_DataCleanup);
                var projection4 = Builders<BsonDocument>.Projection.Include(CONSTANTS.FeatureName).Exclude(CONSTANTS.Id);
                filtereddata_text = filteredCollection1.Find(filter).Project<BsonDocument>(projection4).ToList();
                if (filtereddata_text.Count() > 0)
                {
                    foreach (var keys in featuresListSorted)
                    {
                        if (keys.Key.Contains("Cluster") != true)
                        {
                            keylist.Add(keys.Key);
                        }
                    }
                    var keysToRemove = featuresListSorted.Keys.Except(keylist).ToList();

                    foreach (var key in keysToRemove)
                        featuresListSorted.Remove(key);

                    if (DBEncryptionRequired)
                    {
                        filtereddata_text[0][CONSTANTS.FeatureName] = BsonDocument.Parse(_encryptionDecryption.Decrypt(filtereddata_text[0][CONSTANTS.FeatureName].AsString));
                        //if (!string.IsNullOrEmpty(Convert.ToString(filtereddata_text[0][CONSTANTS.CreatedByUser])))
                        //{
                        //    filtereddata_text[0][CONSTANTS.CreatedByUser] = _encryptionDecryption.Decrypt(filtereddata_text[0][CONSTANTS.CreatedByUser].AsString);
                        //}
                        //if (!string.IsNullOrEmpty(Convert.ToString(filtereddata_text[0][CONSTANTS.ModifiedByUser])))
                        //{
                        //    filtereddata_text[0][CONSTANTS.ModifiedByUser] = _encryptionDecryption.Decrypt(filtereddata_text[0][CONSTANTS.ModifiedByUser].AsString);
                        //}
                    }
                    serializeData = JObject.Parse(filtereddata_text[0].ToString());
                    //Taking all the Columns
                    if (serializeData.Count > 0)
                    {
                        foreach (var features in serializeData[CONSTANTS.FeatureName].Children())
                        {
                            JProperty j = features as JProperty;
                            JObject serializeDatafeature = new JObject();
                            serializeDatafeature = JObject.Parse(j.Value.ToString());
                            var textColumnType = serializeData[CONSTANTS.FeatureName][j.Name][CONSTANTS.Datatype];
                            if ((Convert.ToString(textColumnType[CONSTANTS.Text])) == CONSTANTS.True)
                            {
                                textvalues.Add(j.Name);
                                var serializeData1 = JObject.Parse(filtereddata[0].ToString());
                                if (serializeData1[CONSTANTS.ColumnUniqueValues][j.Name] != null)
                                {
                                    var textValue = serializeData1[CONSTANTS.ColumnUniqueValues][j.Name].FirstOrDefault();
                                    featuresListSorted.Add(j.Name, textValue.ToString());
                                }
                                else
                                {
                                    featuresListSorted.Add(j.Name, "0.0");
                                }
                            }
                        }
                    }
                }
                List<string> data = new List<string>();

                foreach (var item in textvalues)
                {
                    var datafeature = new Dictionary<string, string>();
                    serializeData = JObject.Parse(filtereddata[0].ToString());
                    if (serializeData[CONSTANTS.ColumnUniqueValues][item] != null)
                    {
                        var textValue = serializeData[CONSTANTS.ColumnUniqueValues][item].FirstOrDefault();
                        datafeature.Add(CONSTANTS.TextBox + textValue.ToString(), CONSTANTS.False);

                    }
                    else
                    {
                        datafeature.Add(CONSTANTS.TextBox_Field, CONSTANTS.False);
                    }
                    uniqueDictionary.Add(item, datafeature);
                }
            }

            List<string> dataSource = null;
            dataSource = CommonUtility.GetDataSourceModel(correlationId, appSettings);
            if (dataSource.Count > 0)
            {
                teachAndTestDTO.ModelName = dataSource[0];
                teachAndTestDTO.DataSource = dataSource[1];
                teachAndTestDTO.ModelType = dataSource[2];
                teachAndTestDTO.BusinessProblem = dataSource[3];
                teachAndTestDTO.Category = dataSource[5];
            }

            if (!string.IsNullOrEmpty(JsonConvert.SerializeObject(uniqueDictionary)) && JsonConvert.SerializeObject(uniqueDictionary) != null)
                teachAndTestDTO.TeachtestData = JObject.Parse(JsonConvert.SerializeObject(uniqueDictionary));
            return teachAndTestDTO;
        }
        public TestScenarioModelDTO GetTestScenarios(string correlationId, string modelName)
        {
            TestScenarioModelDTO testScenarioModel = new TestScenarioModelDTO();
            List<JObject> listTestData = new List<JObject>();
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.WF_TestResults);
            var builder = Builders<BsonDocument>.Filter;
            bool DbEncryptionRequired = CommonUtility.EncryptDB(correlationId, appSettings);
            List<string> testScenarios = new List<string>();
            List<List<string>> scenariosSteps = new List<List<string>>();

            var filter = builder.Eq(CONSTANTS.CorrelationId, correlationId) & builder.Eq(CONSTANTS.Model, modelName.Trim()) & builder.Eq(CONSTANTS.Temp, CONSTANTS.True);
            var projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
            var testData = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (testData.Count > 0)
            {
                for (int i = 0; i < testData.Count; i++)
                {
                    if (DbEncryptionRequired)
                    {
                        if (testData[i].Contains(CONSTANTS.Predictions))
                        {
                            testData[i][CONSTANTS.Predictions] = BsonDocument.Parse(_encryptionDecryption.Decrypt(testData[i][CONSTANTS.Predictions].AsString));
                        }
                        try
                        {
                            if (testData[i].Contains("CreatedBy") && !string.IsNullOrEmpty(Convert.ToString(testData[i]["CreatedBy"])))
                            {
                                testData[i]["CreatedBy"] =_encryptionDecryption.Decrypt(testData[i]["CreatedBy"].AsString);
                            }
                        }
                        catch (Exception ex) { LOGGING.LogManager.Logger.LogProcessInfo(typeof(ModelEngineeringService), nameof(GetTestScenarios) + "User is already decrypted....", ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty); }
                    }
                    testData[i][CONSTANTS.ScenarioCase] = CONSTANTS.WFScenario;
                    JObject data = JObject.Parse(testData[i].ToString());
                    listTestData.Add(data);
                    testScenarios.Add(testData[i][CONSTANTS.ScenarioName].ToString());
                    if (testData[i][CONSTANTS.ProblemType].ToString() == CONSTANTS.TimeSeries)
                    {
                        var analysisCollection = _database.GetCollection<BsonDocument>(CONSTANTS.WhatIfAnalysis);
                        var analysisfilterBuilder = Builders<BsonDocument>.Filter;
                        var analysisfilter = analysisfilterBuilder.Eq(CONSTANTS.CorrelationId, correlationId) & analysisfilterBuilder.Eq(CONSTANTS.WFId, testData[i][CONSTANTS.WFId].ToString());
                        var analysisprojection = Builders<BsonDocument>.Projection.Include(CONSTANTS.Steps).Exclude(CONSTANTS.Id);
                        var testModelData = new List<BsonDocument>();
                        testModelData = analysisCollection.Find(analysisfilter).Project<BsonDocument>(analysisprojection).ToList();
                        if (testModelData.Count > 0)
                        {
                            List<string> stepsList = new List<string>();
                            stepsList.Add(CONSTANTS.WF_Id + testData[i][CONSTANTS.WFId].ToString());
                            stepsList.Add(CONSTANTS.Scenario_Name + testData[i][CONSTANTS.ScenarioName].ToString());
                            stepsList.Add(CONSTANTS.Steps_ + testModelData[0][CONSTANTS.Steps].ToString());
                            scenariosSteps.Add(stepsList);
                        }
                    }
                }
                testScenarioModel.steps = scenariosSteps;
            }

            var PACollection = _database.GetCollection<BsonDocument>(CONSTANTS.PrescriptiveAnalyticsResults);
            var PAFilter = builder.Eq(CONSTANTS.CorrelationId, correlationId) & builder.Eq(CONSTANTS.Model, modelName.Trim()) & builder.Eq(CONSTANTS.Temp, CONSTANTS.True);
            var PAProjection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
            var PAResult = PACollection.Find(PAFilter).Project<BsonDocument>(PAProjection).ToList();
            if (PAResult.Count > 0)
            {
                for (int i = 0; i < PAResult.Count; i++)
                {
                    if (DbEncryptionRequired)
                    {
                        if (PAResult[i].Contains(CONSTANTS.Predictions))
                        {
                            PAResult[i][CONSTANTS.Predictions] = BsonDocument.Parse(_encryptionDecryption.Decrypt(PAResult[i][CONSTANTS.Predictions].AsString));
                        }
                        try
                        {
                            if (PAResult[i].Contains("CreatedBy") && !string.IsNullOrEmpty(Convert.ToString(PAResult[i]["CreatedBy"])))
                            {
                                PAResult[i]["CreatedBy"] = _encryptionDecryption.Decrypt(Convert.ToString(PAResult[i]["CreatedBy"]));
                            }
                        }
                        catch (Exception ex) { LOGGING.LogManager.Logger.LogProcessInfo(typeof(ModelEngineeringService), nameof(GetTestScenarios) + "User is already decrypted....", ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty); }
                    }
                    PAResult[i][CONSTANTS.ScenarioCase] = CONSTANTS.PAScenario;
                    JObject data = JObject.Parse(PAResult[i].ToString());
                    listTestData.Add(data);
                    testScenarios.Add(PAResult[i][CONSTANTS.ScenarioName].ToString());
                }
            }
            if (testData.Count > 0 || PAResult.Count > 0)
            {
                testScenarioModel.TestScenarios = testScenarios;
                testScenarioModel.TestData = listTestData;
            }

            List<string> dataSource = null;
            dataSource = CommonUtility.GetDataSourceModel(correlationId, appSettings);
            if (dataSource.Count > 0)
            {
                testScenarioModel.ModelName = dataSource[0];
                testScenarioModel.DataSource = dataSource[1];
                testScenarioModel.ModelType = dataSource[2];
                if (dataSource.Count > 2)
                {
                    testScenarioModel.BusinessProblem = dataSource[3];
                    testScenarioModel.Category = dataSource[5];
                }
            }
            return testScenarioModel;

        }

        public Tuple<List<string>, string> GetModelNames(string correlationId, string ServiceName = "")
        {
            List<string> modelNames = new List<string>();
            string problemType = string.Empty;
            IMongoCollection<BsonDocument> collection;
            if (ServiceName == "Anomaly")
                collection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.MERecommendedModels);
            else
                collection = _database.GetCollection<BsonDocument>(CONSTANTS.MERecommendedModels);
            //var collection = _database.GetCollection<BsonDocument>(CONSTANTS.MERecommendedModels);
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
        public bool IsModelsTrained(string correlationId, string ServiceName = "")
        {
            IMongoCollection<BsonDocument> columnCollection;
            if (ServiceName == "Anomaly")
                columnCollection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.SSAIRecommendedTrainedModels);
            else
                columnCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIRecommendedTrainedModels);
            //var columnCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIRecommendedTrainedModels);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var result = columnCollection.Find(filter).ToList();
            if (result.Count > 0)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Save the data
        /// </summary>
        /// <param name="data">The data</param>
        /// <param name="correlationId">The correlation identifier</param>
        /// <param name="wfId">The wf identifier</param>        
        public string SaveTestResults(dynamic data, string correlationId, string wfId)
        {
            string message = string.Empty;
            if (data.scenario == null)
            {
                message = CONSTANTS.EmptyScenario;
                return message;
            }
            string scenarioName = data.SenarioName.ToString().Trim();
            string scenarioCase = data.scenario.ToString();
            var WFCollection = _database.GetCollection<BsonDocument>(CONSTANTS.WF_TestResults);
            var WFilterBuilder = Builders<BsonDocument>.Filter;
            var WFFilter = WFilterBuilder.Eq(CONSTANTS.CorrelationId, correlationId);
            var WFProject = Builders<BsonDocument>.Projection.Include(CONSTANTS.CorrelationId).Include(CONSTANTS.WFId).Include(CONSTANTS.ScenarioName).Include(CONSTANTS.Temp).Exclude(CONSTANTS.Id);
            var WFResult = WFCollection.Find(WFFilter).Project<BsonDocument>(WFProject).ToList();

            //Checking for duplicate scenario name in db.
            bool duplicateExists = this.IsScenarioNameExists(WFResult, scenarioName);

            if (duplicateExists)
            {
                message = CONSTANTS.Duplicate;
                return message;
            }
            else
            {
                var PACollection = _database.GetCollection<BsonDocument>(CONSTANTS.PrescriptiveAnalyticsResults);
                var PAFilterBuilder = Builders<BsonDocument>.Filter;
                var PAFilter = PAFilterBuilder.Eq(CONSTANTS.CorrelationId, correlationId);
                var PAProject = Builders<BsonDocument>.Projection.Include(CONSTANTS.CorrelationId).Include(CONSTANTS.WFId).Include(CONSTANTS.ScenarioName).Include(CONSTANTS.Temp).Exclude(CONSTANTS.Id);
                var PAResult = PACollection.Find(PAFilter).Project<BsonDocument>(PAProject).ToList();

                //Checking for duplicate scenario name in db.
                duplicateExists = this.IsScenarioNameExists(PAResult, scenarioName);
                if (duplicateExists)
                {
                    message = CONSTANTS.Duplicate;
                    return message;
                }
            }
            if (data.scenario == CONSTANTS.PAScenario)
            {
                var collection = _database.GetCollection<BsonDocument>(CONSTANTS.PrescriptiveAnalyticsResults);
                var filterBuilder = Builders<BsonDocument>.Filter;
                var filter = filterBuilder.Eq(CONSTANTS.CorrelationId, correlationId) & filterBuilder.Eq(CONSTANTS.WFId, wfId);
                var project = Builders<BsonDocument>.Projection.Include(CONSTANTS.CorrelationId).Include(CONSTANTS.WFId).Include(CONSTANTS.ScenarioName).Include(CONSTANTS.Temp).Exclude(CONSTANTS.Id);
                var result = collection.Find(filter).Project<BsonDocument>(project).ToList();
                if (result.Count > 0)
                {
                    if (data != null)
                    {
                        List<UpdateDefinition<BsonDocument>> updateList = new List<UpdateDefinition<BsonDocument>>();
                        updateList.Add(Builders<BsonDocument>.Update.Set(CONSTANTS.Temp, CONSTANTS.True));
                        updateList.Add(Builders<BsonDocument>.Update.Set(CONSTANTS.ScenarioName, data.SenarioName.ToString()));
                        var update = Builders<BsonDocument>.Update.Combine(updateList);
                        var outcome = collection.UpdateMany(filter, update);
                        message = CONSTANTS.Success;
                        return message;
                    }
                }
            }
            else
            {
                var collection = _database.GetCollection<BsonDocument>(CONSTANTS.WF_TestResults);
                var filterBuilder = Builders<BsonDocument>.Filter;
                var filter = filterBuilder.Eq(CONSTANTS.CorrelationId, correlationId) & filterBuilder.Eq(CONSTANTS.WFId, wfId);
                var project = Builders<BsonDocument>.Projection.Include(CONSTANTS.CorrelationId).Include(CONSTANTS.WFId).Include(CONSTANTS.ScenarioName).Include(CONSTANTS.Temp).Exclude(CONSTANTS.Id);
                var result = collection.Find(filter).Project<BsonDocument>(project).ToList();
                if (result.Count > 0)
                {
                    if (data != null)
                    {
                        List<UpdateDefinition<BsonDocument>> updateList = new List<UpdateDefinition<BsonDocument>>();
                        updateList.Add(Builders<BsonDocument>.Update.Set(CONSTANTS.Temp, CONSTANTS.True));
                        updateList.Add(Builders<BsonDocument>.Update.Set(CONSTANTS.ScenarioName, data.SenarioName.ToString()));
                        var update = Builders<BsonDocument>.Update.Combine(updateList);
                        var outcome = collection.UpdateMany(filter, update);
                        message = CONSTANTS.Success;
                        return message;
                    }
                }
            }
            return message;
        }

        /// <summary>
        /// Checking for duplicate Scenario Name 
        /// </summary>
        /// <param name="document"></param>
        /// <param name="scenarioName"></param>
        /// <returns></returns>
        private bool IsScenarioNameExists(List<BsonDocument> document, string scenarioName)
        {
            if (document.Count > 0)
            {
                List<BsonDocument> lstScenarioName = new List<BsonDocument>();
                foreach (var i in document)
                {
                    string existingScenarioName = i[CONSTANTS.ScenarioName].ToString().Trim();
                    if (existingScenarioName.Equals(scenarioName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        lstScenarioName.Add(i);
                    }
                }

                return lstScenarioName.Count() > 0 ? true : false;
            }

            return false;
        }


        /// <summary>
        /// Get Presccriptive Analytics
        /// </summary>
        /// <param name="data"></param>
        /// <param name="prescriptiveAnalytics"></param>
        /// <returns></returns>
        public string PrescriptiveAnalytics(dynamic data, out PrescriptiveAnalyticsResult prescriptiveAnalytics)
        {
            PrescriptiveAnalyticsResult prescriptiveAnalyticResult = new PrescriptiveAnalyticsResult();
            string correlationId = data[CONSTANTS.CorrelationId].ToString();
            string wfId = data[CONSTANTS.WFId].ToString();
            string userId = data[CONSTANTS.CreatedByUser].ToString();
            string modelType = data[CONSTANTS.ModelType].ToString();
            string pageInfo = data[CONSTANTS.PageInfo].ToString();
            string model = data[CONSTANTS.model].ToString();
            bool isNewRequest = data["isNewRequest"];

            #region VALIDATIONS
            CommonUtility.ValidateInputFormData(correlationId, CONSTANTS.CorrelationId, true);
            CommonUtility.ValidateInputFormData(wfId, CONSTANTS.WFId, true);
            CommonUtility.ValidateInputFormData(modelType, CONSTANTS.ModelType, false);
            CommonUtility.ValidateInputFormData(pageInfo, CONSTANTS.PageInfo, false);
            CommonUtility.ValidateInputFormData(model, CONSTANTS.model, false);
            #endregion

            if (isNewRequest)
            {
                var paResult = this.UpdatePrescriptiveIngrainRequest(data, userId, correlationId, wfId, pageInfo, modelType, model);
                if (paResult.Message == CONSTANTS.Success)
                {
                    prescriptiveAnalytics = paResult;
                    return CONSTANTS.Success;
                }
            }
            IngrainRequestQueue requestQueue = _ingestedDataService.GetFileRequestStatus(correlationId, pageInfo, wfId);
            if (requestQueue != null)
            {
                prescriptiveAnalyticResult.Status = requestQueue.Status;
                prescriptiveAnalyticResult.Progress = requestQueue.Progress;
                prescriptiveAnalyticResult.Message = requestQueue.Message;
                prescriptiveAnalyticResult.WFId = data.WFId;
                prescriptiveAnalyticResult.CorrelationId = data.CorrelationId;
                if (requestQueue.Status == CONSTANTS.C & requestQueue.Progress == CONSTANTS.Hundred)
                {
                    prescriptiveAnalyticResult.Message = requestQueue.Message;
                    prescriptiveAnalyticResult = this.GetPrescriptiveAnalyticsResult(correlationId, wfId);
                    prescriptiveAnalyticResult.Status = requestQueue.Status;
                    prescriptiveAnalyticResult.Progress = requestQueue.Progress;
                    prescriptiveAnalyticResult.WFId = data.WFId;
                    prescriptiveAnalytics = prescriptiveAnalyticResult;
                    //this.RemovePARequest(correlationId, wfId, pageInfo);
                    return CONSTANTS.C;

                }
                else if (requestQueue.Status == CONSTANTS.E)
                {
                    prescriptiveAnalytics = prescriptiveAnalyticResult;
                    //this.RemovePARequest(correlationId, wfId, pageInfo);
                    return CONSTANTS.PhythonError;
                }
                else if (requestQueue.Status == CONSTANTS.I)
                {
                    prescriptiveAnalytics = prescriptiveAnalyticResult;
                    //  this.RemovePARequest(correlationId, wfId, pageInfo);
                    return CONSTANTS.PhythonInfo;
                }
                else
                {
                    if (string.IsNullOrEmpty(requestQueue.Status))
                    {
                        prescriptiveAnalyticResult.Status = CONSTANTS.P;
                        prescriptiveAnalyticResult.Progress = "0";
                        prescriptiveAnalyticResult.Message = requestQueue.Message;
                    }
                    prescriptiveAnalytics = prescriptiveAnalyticResult;
                    return CONSTANTS.P;
                }
            }
            else
            {
                IngrainRequestQueue ingrainRequest = new IngrainRequestQueue
                {
                    _id = Guid.NewGuid().ToString(),
                    CorrelationId = data.CorrelationId,
                    RequestId = Guid.NewGuid().ToString(),
                    ProcessId = null,
                    Status = null,
                    ModelName = data.model,
                    RequestStatus = CONSTANTS.New,
                    RetryCount = 0,
                    ProblemType = data.ModelType,
                    Message = null,
                    UniId = data.WFId,
                    Progress = null,
                    pageInfo = CONSTANTS.PrescriptiveAnalytics,
                    ParamArgs = null,
                    Function = CONSTANTS.PrescriptiveAnalytics,
                    CreatedByUser = data.CreatedByUser,
                    CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                    ModifiedByUser = data.CreatedByUser,
                    ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                    LastProcessedOn = null,
                };
                PAModel predictiveAnalytics = new PAModel
                {
                    WfId = data.WFId,
                    desired_value = data.Desired_Value
                };
                ingrainRequest.ParamArgs = predictiveAnalytics.ToJson();
                _ingestedDataService.InsertRequests(ingrainRequest);
                Thread.Sleep(2000);
                prescriptiveAnalyticResult.Message = CONSTANTS.Success;
                prescriptiveAnalyticResult.Status = CONSTANTS.True;
                prescriptiveAnalytics = prescriptiveAnalyticResult;
                return CONSTANTS.Success;
            }
        }

        /// <summary>
        /// UpdatePrescriptiveIngrainRequest
        /// </summary>
        /// <param name="data"></param>
        /// <param name="userId"></param>
        /// <param name="correlationId"></param>
        /// <param name="wfId"></param>
        /// <param name="pageInfo"></param>
        /// <param name="problemType"></param>
        public PrescriptiveAnalyticsResult UpdatePrescriptiveIngrainRequest(dynamic data, string userId, string correlationId, string wfId, string pageInfo, string problemType, string model)
        {
            bool DBencryptionRequired = CommonUtility.EncryptDB(correlationId, appSettings);
            if (DBencryptionRequired)
            {
                if (!string.IsNullOrEmpty(Convert.ToString(userId)))
                    userId = _encryptionDecryption.Encrypt(Convert.ToString(userId));
            }
            PrescriptiveAnalyticsResult pa = new PrescriptiveAnalyticsResult();
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
            var filterProjection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
            var filterbuilder = Builders<BsonDocument>.Filter;
            var filter = filterbuilder.Eq(CONSTANTS.CorrelationId, correlationId) & filterbuilder.Eq(CONSTANTS.pageInfo, pageInfo) & filterbuilder.Eq(CONSTANTS.UniId, wfId);
            var ingrainRequestCollection = collection.Find(filter).Project<BsonDocument>(filterProjection).FirstOrDefault();
            if (ingrainRequestCollection != null)
            {
                var paramArgs = JObject.Parse(ingrainRequestCollection[CONSTANTS.ParamArgs].ToString());
                PAModel paModel = new PAModel();
                foreach (var item in paramArgs.Children())
                {
                    JProperty jProperty = item as JProperty;
                    if (jProperty != null)
                    {
                        string propertyname = jProperty.Name;
                        if (propertyname == CONSTANTS.desired_value)
                        {
                            paModel.desired_value = data.Desired_Value;
                            paModel.WfId = wfId;
                            var builder = Builders<BsonDocument>.Update;
                            var update = builder
                                  .Set(CONSTANTS.RequestStatus, CONSTANTS.New)
                                  .Set(CONSTANTS.RetryCount, 0)
                                  .Set(CONSTANTS.ParamArgs, paModel.ToJson())
                                  .Set(CONSTANTS.ModifiedByUser, userId)
                             .Set(CONSTANTS.ModelName, model)
                             .Set(CONSTANTS.Status, CONSTANTS.Null)
                             .Set(CONSTANTS.Message, CONSTANTS.Null)
                             .Set(CONSTANTS.Progress, "0")
                                  .Set(CONSTANTS.ModifiedOn, DateTime.Now.ToString(CONSTANTS.DateFormat));
                            collection.UpdateMany(filter, update);
                        }
                    }
                }
                pa.Message = CONSTANTS.Success;
                pa.Status = CONSTANTS.True;
                return pa;
            }
            return pa;
        }

        public PrescriptiveAnalyticsResult GetPrescriptiveAnalyticsResult(string CorrelationId, string WFId)
        {
            var prescriptiveResult = new PrescriptiveAnalyticsResult();
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.PrescriptiveAnalyticsResults);
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq(CONSTANTS.CorrelationId, CorrelationId) & builder.Eq(CONSTANTS.WFId, WFId);
            var projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
            var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                bool DBEncryptionRequired = CommonUtility.EncryptDB(CorrelationId, appSettings);
                if (DBEncryptionRequired)
                {
                    result[0][CONSTANTS.Predictions] = BsonDocument.Parse(_encryptionDecryption.Decrypt(result[0][CONSTANTS.Predictions].AsString));
                    try
                    {
                        if (result[0].Contains("CreatedBy") && !string.IsNullOrEmpty(Convert.ToString(result[0]["CreatedBy"])))
                        {
                            result[0]["CreatedBy"] = _encryptionDecryption.Decrypt(Convert.ToString(result[0]["CreatedBy"]));
                        }
                    }
                    catch (Exception ex) { LOGGING.LogManager.Logger.LogProcessInfo(typeof(ModelEngineeringService), nameof(GetPrescriptiveAnalyticsResult) + "User is already decrypted....", ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty); }
                    //try
                    //{
                    //    if (result[0].Contains(CONSTANTS.ModifiedByUser) && !string.IsNullOrEmpty(Convert.ToString(result[0][CONSTANTS.ModifiedByUser])))
                    //    {
                    //        result[0][CONSTANTS.ModifiedByUser] = _encryptionDecryption.Decrypt(Convert.ToString(result[0][CONSTANTS.ModifiedByUser]));
                    //    }
                    //}
                    //catch (Exception) { }
                }
                JObject data = JObject.Parse(result[0].ToString());
                prescriptiveResult.TeachtestData = data;
                return prescriptiveResult;
            }
            else
            {
                prescriptiveResult.Message = CONSTANTS.PAErrorMsg;
                return prescriptiveResult;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="wFId"></param>
        /// <returns></returns>
        public bool DeletePrescriptiveAnalytics(string correlationId, string wFId)
        {
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq(CONSTANTS.CorrelationId, correlationId) & builder.Eq(CONSTANTS.WFId, wFId);
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.PrescriptiveAnalyticsResults);
            var deletedResult = collection.Find(filter).ToList();
            if (deletedResult.Count > 0)
            {
                collection.DeleteOne(filter);
            }
            return true;
        }


        /// <summary>
        /// Get Presccriptive Analytics
        /// </summary>
        /// <param name="data"></param>
        /// <param name="prescriptiveAnalytics"></param>
        /// <returns></returns>
        public string RunTest(dynamic dynamicColumns, out FeaturePredictionTestDTO featurPrediction)
        {
            FeaturePredictionTestDTO featurePredictionTest = new FeaturePredictionTestDTO();
            if (dynamicColumns.CorrelationId != null && dynamicColumns.CorrelationId != "undefined")
            {
                string WFId = Guid.NewGuid().ToString();
                if (string.IsNullOrEmpty(dynamicColumns.WFId))
                {
                    dynamicColumns.WFId = WFId;
                }
                IngrainRequestQueue requestQueue = _ingestedDataService.GetFileRequestStatus(dynamicColumns.CorrelationId, "WFTeachTest", dynamicColumns.WFId);
                if (requestQueue == null)
                {
                    bool DBEncryptionRequired = CommonUtility.EncryptDB(dynamicColumns.CorrelationId, appSettings);
                    if (DBEncryptionRequired)
                    {
                        if (dynamicColumns.Features != null)
                        {
                            dynamicColumns.Features = _encryptionDecryption.Encrypt(dynamicColumns.Features.ToString(Formatting.None));
                        }
                        if (!string.IsNullOrEmpty(Convert.ToString(dynamicColumns.ModifiedByUser)))
                        {
                            dynamicColumns.ModifiedByUser = _encryptionDecryption.Encrypt(Convert.ToString(dynamicColumns.ModifiedByUser));
                        }
                        if (!string.IsNullOrEmpty(Convert.ToString(dynamicColumns.CreatedByUser)))
                        {
                            dynamicColumns.CreatedByUser = _encryptionDecryption.Encrypt(Convert.ToString(dynamicColumns.CreatedByUser));
                        }
                    }
                    this.InsertColumns(dynamicColumns);
                }
                if (requestQueue != null)
                {
                    featurePredictionTest.Status = requestQueue.Status;
                    featurePredictionTest.Progress = requestQueue.Progress;
                    featurePredictionTest.Message = requestQueue.Message;
                    featurePredictionTest.WFId = dynamicColumns.WFId;
                    if (requestQueue.Status == CONSTANTS.C & requestQueue.Progress == CONSTANTS.Hundred)
                    {
                        featurePredictionTest = this.GetFeaturePredictionForTest(dynamicColumns.CorrelationId, dynamicColumns.WFId, dynamicColumns.Steps);
                        featurePredictionTest.Status = requestQueue.Status;
                        featurePredictionTest.Progress = requestQueue.Progress;
                        featurePredictionTest.Message = requestQueue.Message;
                        featurePredictionTest.WFId = dynamicColumns.WFId;
                        featurPrediction = featurePredictionTest;
                        return CONSTANTS.C;
                    }
                    else if (requestQueue.Status == CONSTANTS.E)
                    {
                        featurPrediction = featurePredictionTest;
                        return CONSTANTS.PhythonError;
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(requestQueue.Status))
                        {
                            featurePredictionTest.Status = CONSTANTS.P;
                            featurePredictionTest.Progress = "0";
                            featurePredictionTest.Message = requestQueue.Message;
                        }
                        featurPrediction = featurePredictionTest;
                        return CONSTANTS.P;
                    }
                }
                else
                {
                    bool DBEncryptionRequired = CommonUtility.EncryptDB(dynamicColumns.CorrelationId, appSettings);
                    if (DBEncryptionRequired)
                    {
                        try
                        {
                            if (!string.IsNullOrEmpty(Convert.ToString(dynamicColumns.CreatedByUser)))
                                dynamicColumns.CreatedByUser = _encryptionDecryption.Decrypt(Convert.ToString(dynamicColumns.CreatedByUser));
                        }
                        catch (Exception ex) { LOGGING.LogManager.Logger.LogProcessInfo(typeof(ModelEngineeringService), nameof(RunTest) + "User is already decrypted....", ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty); }
                    }
                    IngrainRequestQueue ingrainRequest = new IngrainRequestQueue
                    {
                        _id = Guid.NewGuid().ToString(),
                        CorrelationId = dynamicColumns.CorrelationId,
                        RequestId = Guid.NewGuid().ToString(),
                        ProcessId = null,
                        Status = null,
                        ModelName = dynamicColumns.model,
                        RequestStatus = CONSTANTS.New,
                        RetryCount = 0,
                        ProblemType = null,
                        Message = null,
                        UniId = WFId,
                        Progress = null,
                        pageInfo = CONSTANTS.WFTeachTest,
                        ParamArgs = null,
                        Function = CONSTANTS.WFAnalysis,
                        CreatedByUser = dynamicColumns.CreatedByUser,
                        CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                        ModifiedByUser = dynamicColumns.CreatedByUser,
                        ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                        LastProcessedOn = null,
                    };
                    WfAnalysisParams wfAnalysis = new WfAnalysisParams
                    {
                        WfId = WFId,
                        Bulk = dynamicColumns.bulkData
                    };
                    ingrainRequest.ParamArgs = wfAnalysis.ToJson();
                    _ingestedDataService.InsertRequests(ingrainRequest);
                    Thread.Sleep(2000);
                    featurePredictionTest.Message = CONSTANTS.Success;
                    featurePredictionTest.WFId = WFId;
                    featurePredictionTest.Status = CONSTANTS.True;
                   LOGGING.LogManager.Logger.LogProcessInfo(typeof(ModelEngineeringService), nameof(RunTest), "END", string.IsNullOrEmpty(ingrainRequest.CorrelationId) ? default(Guid) : new Guid(ingrainRequest.CorrelationId),ingrainRequest.AppID, string.Empty,ingrainRequest.ClientID,ingrainRequest.DeliveryconstructId);
                    featurPrediction = featurePredictionTest;
                    return CONSTANTS.Success;
                }
            }
            else
            {
                featurPrediction = featurePredictionTest;
                return CONSTANTS.Empty;
            }
        }


        public bool RemovePARequest(string correlationId, string wfId, string pageInfo)
        {
            bool removed = false;
            var builder = Builders<BsonDocument>.Filter;
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
            var filter = builder.Eq(CONSTANTS.CorrelationId, correlationId) & builder.Eq(CONSTANTS.UniId, wfId) & builder.Eq(CONSTANTS.pageInfo, pageInfo);
            var result = collection.DeleteOne(filter);
            if (result.DeletedCount > 0)
            {
                removed = true;
            }
            return removed;
        }

        public SystemUsageDetails GetSystemUsageDetails()
        {
            _systemUsageDetails = new SystemUsageDetails();
            //Process proc = Process.GetCurrentProcess();
            //proc.Refresh();
            //cpuCounter = new PerformanceCounter(CONSTANTS.Processor, CONSTANTS.ProcessorTime, CONSTANTS._Total);
            //cpuCounter.NextValue();
            //////Performance counter objects in general need two values sampled at 1 - 2 seconds apart to be able to give accurate readings
            //System.Threading.Thread.Sleep(1000);
            //_systemUsageDetails.CPUUsage = cpuCounter.NextValue();
            //////Working Set - Private : Gets the amount of physical memory allocated for the assosiated process that cannot be shared with other process. (In Bytes)
            //ramCounter = new PerformanceCounter(CONSTANTS.Process, CONSTANTS.WorkingSetPrivate, proc.ProcessName);
            //_systemUsageDetails.MemoryUsageInMB = (ramCounter.NextValue() / 1024) / 1024f; ////Converted KB to MB
            //proc.Dispose();
            return _systemUsageDetails;
        }
        private bool IsModelTrained(string correlationId)
        {
            IMongoCollection<BsonDocument> columnCollection;
            if ( servicename == "Anomaly")
                columnCollection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.SSAIRecommendedTrainedModels);
            else
                columnCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIRecommendedTrainedModels);
            //var columnCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIRecommendedTrainedModels);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var outcome = columnCollection.Find(filter).ToList();
            return outcome.Count > 0 ? true : false;
        }
        public void InsertUsage(double CurrentProgress, double CPUUsage, string CorrelationId, string ServiceName = "")
        {
            IMongoCollection<BsonDocument> collection;
            if (ServiceName == "Anomaly")
                collection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.ME_RecommendedModels);
            else
                collection = _database.GetCollection<BsonDocument>(CONSTANTS.ME_RecommendedModels);
            //var collection = _database.GetCollection<BsonDocument>(CONSTANTS.ME_RecommendedModels);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, CorrelationId);
            var builder = Builders<BsonDocument>.Update;
            var update = builder
                  .Set("CPUUsage", CPUUsage)
                  .Set("CurrentProgress", CurrentProgress);
            collection.UpdateOne(filter, update);
        }
        public void TerminateModelsTrainingRequests(string correlationId, List<IngrainRequestQueue> requests, string ServiceName = "")
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ModelEngineeringService), nameof(TerminateModelsTrainingRequests), "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId) , string.Empty, string.Empty, string.Empty, string.Empty);
            //updating the inprogress models to terminate state.
            IMongoCollection<BsonDocument> collection;
            if (ServiceName == "Anomaly")
                collection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
            else
                collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
            //var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
            var update = Builders<BsonDocument>.Update.Set("RequestStatus", "Terminated").Set(CONSTANTS.Status, CONSTANTS.E).Set(CONSTANTS.ModifiedOn, DateTime.Now.ToString(CONSTANTS.DateHoursFormat)).Set(CONSTANTS.Message, "Terminated");
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId)
                & Builders<BsonDocument>.Filter.Eq(CONSTANTS.Function, CONSTANTS.RecommendedAI)
                & Builders<BsonDocument>.Filter.Ne(CONSTANTS.Status, CONSTANTS.C);
            var filter2 = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId)
                & Builders<BsonDocument>.Filter.Eq("RequestStatus", "Terminated");
            var requestResult = collection.Find(filter2).ToList();
            bool DBEncryptionRequired = CommonUtility.EncryptDB(correlationId, appSettings, ServiceName);
            if (requestResult.Count < 1)
            {
                var result = collection.UpdateMany(filter, update);
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(ModelEngineeringService), nameof(TerminateModelsTrainingRequests), "START MODIFIED COUNT :" + result.ModifiedCount + result.MatchedCount, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                foreach (var item in requests)
                {
                    if (item.Status != CONSTANTS.C)
                    {
                        if (DBEncryptionRequired)
                        {
                            if (!string.IsNullOrEmpty(Convert.ToString(item.CreatedByUser)))
                            {
                                item.CreatedByUser = _encryptionDecryption.Encrypt(Convert.ToString(item.CreatedByUser));
                            }
                        }
                        IngrainRequestQueue ingrainRequest = new IngrainRequestQueue()
                        {
                            _id = Guid.NewGuid().ToString(),
                            CorrelationId = correlationId,
                            RequestId = Guid.NewGuid().ToString(),
                            ProcessId = null,
                            Status = null,
                            ModelName = item.ModelName,
                            RequestStatus = CONSTANTS.New,
                            RetryCount = 0,
                            ProblemType = null,
                            Message = null,
                            UniId = null,
                            PythonProcessID = item.PythonProcessID,
                            Progress = null,
                            pageInfo = CONSTANTS.TerminatePythyon,
                            ParamArgs = null,
                            Function = CONSTANTS.TerminatePythyon,
                            CreatedByUser = item.CreatedByUser,
                            CreatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                            ModifiedByUser = item.CreatedByUser,
                            ModifiedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                            LastProcessedOn = null,
                        };
                        var jsonColumns = Newtonsoft.Json.JsonConvert.SerializeObject(ingrainRequest);
                        var insertBsonColumns = BsonSerializer.Deserialize<BsonDocument>(jsonColumns);
                        collection.InsertOne(insertBsonColumns);
                        Thread.Sleep(1000);
                    }
                }
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ModelEngineeringService), nameof(TerminateModelsTrainingRequests), "END ", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
        }
        #endregion
    }
}
