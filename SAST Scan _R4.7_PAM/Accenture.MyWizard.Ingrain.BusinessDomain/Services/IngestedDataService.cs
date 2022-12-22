#region Copyright (c) 2018 Accenture . All rights reserved.

// <copyright company="Accenture">
// Copyright (c) 2018 Accenture.  All rights reserved.
// Reproduction or transmission in whole or in part, in any form or by any means, 
// electronic, mechanical or otherwise, is prohibited without the prior written 
// consent of the copyright owner.
// </copyright>

#endregion

#region IngestedService Information
/********************************************************************************************************\
Module Name     :   IngestedDataService
Project         :   Accenture.MyWizard.SelfServiceAI.BusinessDomain
Organisation    :   Accenture Technologies Ltd.
Created By      :   RaviA
Created Date    :   02-Jan-2019
Revision History :
Revision No             Initial             Modified Date           Description
1.0                     An                  02-Jan-2019             
\********************************************************************************************************/
#endregion


namespace Accenture.MyWizard.Ingrain.BusinessDomain.Services
{
    #region Namespace References
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
    using System.Configuration;
    using System.Linq;
    using System.Text;
    using System.Web;
    using Microsoft.AspNetCore.Http;
    using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
    using System.IO;
    using System.Collections.Specialized;
    using System.Threading;
    using Microsoft.Extensions.DependencyInjection;
    using System.Globalization;
    using System.Net.Http;
    using System.Net;
    using System.Net.Http.Headers;
    using RestSharp;
    #endregion
    public class IngestedDataService : IIngestedData

    {
        #region Members
        private MongoClient _mongoClient;
        private IMongoDatabase _database;
        private readonly IOptions<IngrainAppSettings> appSettings;
        DatabaseProvider databaseProvider;
        private IngrainRequestQueue ingrainRequest;
        private IngestedDataDTO _ingestedData = null;
        Filepath _filepath = null;
        AgileFilepath _agilefilepath = null;
        ParentFile parentFile = null;
        FileUpload fileUpload = null;
        AgileFileUpload agileFileUpload = null;
        private IFlushService _flushService { get; set; }
        private IEncryptionDecryption _encryptionDecryption;
        private static ICustomDataService _customDataService { set; get; }
        private MongoClient _mongoClientAD;
        private IMongoDatabase _databaseAD;
        private string servicename = "";
        #endregion

        #region Constructors
        /// <summary>
        /// Constructor to Initialize mongoDB Connection
        /// </summary>
        public IngestedDataService(DatabaseProvider db, IOptions<IngrainAppSettings> settings, IServiceProvider serviceProvider)
        {
            databaseProvider = db;
            appSettings = settings;
            _mongoClient = databaseProvider.GetDatabaseConnection();
            var dataBaseName = MongoUrl.Create(appSettings.Value.connectionString).DatabaseName;
            _database = _mongoClient.GetDatabase(dataBaseName);
            //Anomaly Detection
            _mongoClientAD = databaseProvider.GetDatabaseConnection("Anomaly");
            var dataBaseNameAD = MongoUrl.Create(appSettings.Value.AnomalyDetectionCS).DatabaseName;
            _databaseAD = _mongoClientAD.GetDatabase(dataBaseNameAD);
            // _flushService = serviceProvider.GetService<IFlushService>();
            _flushService = new FlushModelService(databaseProvider, appSettings);
            _encryptionDecryption = serviceProvider.GetService<IEncryptionDecryption>();
            _customDataService = serviceProvider.GetService<ICustomDataService>();
        }
        #endregion

        #region Methods        

        /// <summary>
        /// Ingest the data which is read from the Files or any Source.
        /// </summary>
        /// <param name="inputData"></param>
        /// <returns>Data Insertion Success</returns>
        public void InsertData(IngestedDataDTO ingestedData)
        {
            var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(ingestedData);
            var insertDocument = BsonSerializer.Deserialize<BsonDocument>(jsonData);
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.PSIngestedData);
            collection.InsertOne(insertDocument);
        }

        /// <summary>
        /// Ingest the data which is read from the Files or any Source.
        /// </summary>
        /// <param name="inputData"></param>
        /// <returns>Data Insertion Success</returns>
        public void InsertDataSource(IngestedDataDTO ingestedData)
        {

            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, ingestedData.CorrelationId);
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.PSIngestedData);

            var correaltionExist = collection.Find(filter).ToList();
            if (correaltionExist.Count > 0)
            {

                collection.DeleteMany(filter);
            }
            var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(ingestedData);
            var insertDocument = BsonSerializer.Deserialize<BsonDocument>(jsonData);
            collection.InsertOne(insertDocument);
        }

        /// <summary>
        /// Add Dynamic fields to Database
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="jsonColumns"></param>
        /// <returns>Insetion Successfull</returns>
        public void AddFiledToSSAIBAL(string correlationId, dynamic jsonColumns)
        {
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.PEIngestedData);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            foreach (var columns in jsonColumns)
            {
                string column = string.Format(CONSTANTS.InputData, columns.Name);
                var update = Builders<BsonDocument>.Update.Set(column, columns.Value.Value);
                var result = collection.UpdateOne(filter, update);
            }
        }
        public string GetPytonProcessData(string correlationId, string pageInfo)
        {
            string queueData = string.Empty;
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIUseCase);
            var filter1 = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var filter2 = Builders<BsonDocument>.Filter.Eq(CONSTANTS.Steps, pageInfo);
            var filter = filter1 & filter2;
            var result = collection.Find(filter).ToList();
            if (result.Count > 0)
            {
                queueData = result[0].ToString();
            }
            return queueData;
        }
        public UserColumns GetColumns(string correlationId, bool IsTemplate, bool newModel, string ServiceName = "")
        {
            servicename = ServiceName;
            UserColumns userColumns = new UserColumns();
            List<UserColumns> result = null;
            bool isCascadedModel = false;
            IMongoCollection<BsonDocument> deployModelCollection;
            if (servicename == "Anomaly")
                deployModelCollection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.SSAICascadedModels);
            else
                deployModelCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAICascadedModels);

            //var deployModelCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAICascadedModels);
            var deployFilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CascadedId, correlationId);
            var deployProjection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.MappingData).Exclude(CONSTANTS.Id);
            var deployModelResult = deployModelCollection.Find(deployFilter).Project<BsonDocument>(deployProjection).ToList();
            string[] arr = new string[] { };
            if (deployModelResult.Count > 0)
            {
                isCascadedModel = true;
            }
            if (isCascadedModel)
            {
                List<string> inputColumnsList = new List<string>();
                JObject cascadedData = JObject.Parse(deployModelResult[0].ToString());
                if (cascadedData != null)
                {
                    foreach (var item in cascadedData[CONSTANTS.ModelList].Children())
                    {
                        if (item != null)
                        {
                            JProperty prop = item as JProperty;
                            var data = JsonConvert.DeserializeObject<DeployModelDetails>(prop.Value.ToString());
                            IMongoCollection<BsonDocument> pscollection;
                            if (servicename == "Anomaly")
                                pscollection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.PSBusinessProblem);
                            else
                                pscollection = _database.GetCollection<BsonDocument>(CONSTANTS.PSBusinessProblem);
                            //var pscollection = _database.GetCollection<BsonDocument>(CONSTANTS.PSBusinessProblem);
                            var psFilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, data.CorrelationId);
                            var psProjection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.MappingData).Exclude(CONSTANTS.Id);
                            var psResult = pscollection.Find(psFilter).Project<BsonDocument>(psProjection).ToList();
                            if (psResult.Count > 0)
                            {
                                var columns = psResult[0]["InputColumns"].AsBsonArray;
                                foreach (var col in columns)
                                {
                                    inputColumnsList.Add(col.ToString());
                                }
                                if (prop.Name == "Model" + cascadedData[CONSTANTS.ModelList].Children().Count())
                                {
                                    userColumns.TargetColumn = psResult[0][CONSTANTS.TargetColumn].ToString();
                                    userColumns.TargetUniqueIdentifier = psResult[0][CONSTANTS.TargetUniqueIdentifier].ToString();
                                }
                            }
                            IMongoCollection<UserColumns> columnCollection;
                            if (servicename == "Anomaly")
                                columnCollection = _databaseAD.GetCollection<UserColumns>(CONSTANTS.PSIngestedData);
                            else
                                columnCollection = _database.GetCollection<UserColumns>(CONSTANTS.PSIngestedData);
                            //var columnCollection = _database.GetCollection<UserColumns>(CONSTANTS.PSIngestedData);
                            var filter = Builders<UserColumns>.Filter.Eq(CONSTANTS.CorrelationId, data.CorrelationId);
                            var projection = Builders<UserColumns>.Projection.Include(CONSTANTS.ColumnsList).Include(CONSTANTS.DataType).Include(CONSTANTS.Category).Exclude(CONSTANTS.Id);
                            var AvailableColumns = columnCollection.Find(filter).Project<UserColumns>(projection).ToList();
                            Dictionary<string, string> valuePairs = new Dictionary<string, string>();
                            if (AvailableColumns.Count > 0)
                            {
                                var dataTypeColumns = AvailableColumns[0].DataType;
                                var BsonElements = dataTypeColumns.ToBsonDocument().ToList();
                                for (int i = 0; i < BsonElements.Count; i++)
                                {
                                    valuePairs.Add(BsonElements[i].Name, BsonElements[i].Value.ToString());
                                }
                                userColumns.DataTypeColumns = valuePairs;
                            }
                        }

                    }
                    if (inputColumnsList.Count > 0)
                    {
                        userColumns.InputColumns = inputColumnsList.Distinct().ToArray();
                        if (cascadedData[CONSTANTS.Status].ToString() == CONSTANTS.Deployed)
                            userColumns.IsModelTrained = true;
                        userColumns.IsModelDeployed = this.IsModelDeployed(correlationId);
                        userColumns.Category = cascadedData[CONSTANTS.Category].ToString();
                        userColumns.ModelName = cascadedData["ModelName"].ToString();
                        userColumns.DataSource = CONSTANTS.Cascading;
                        userColumns.BusinessProblems = CONSTANTS.Cascading;
                        userColumns.InstaFLag = false;
                        userColumns.AvailableColumns = arr;
                        userColumns.ColumnsList = arr;
                        userColumns.IsCascadeModel = true;
                        userColumns.InstaFLag = false;
                        return userColumns;
                    }
                }
            }
            else
            {
                IMongoCollection<UserColumns> columnCollection;
                if (servicename == "Anomaly")
                    columnCollection = _databaseAD.GetCollection<UserColumns>(CONSTANTS.PSIngestedData);
                else
                    columnCollection = _database.GetCollection<UserColumns>(CONSTANTS.PSIngestedData);
               // var columnCollection = _database.GetCollection<UserColumns>(CONSTANTS.PSIngestedData);
                var filter = Builders<UserColumns>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
                var projection = Builders<UserColumns>.Projection.Include(CONSTANTS.ColumnsList).Include(CONSTANTS.DataType).Include(CONSTANTS.Category).Exclude(CONSTANTS.Id);
                var AvailableColumns = columnCollection.Find(filter).Project<UserColumns>(projection).ToList();

                IMongoCollection<UserColumns> collection;
                if (servicename == "Anomaly")
                    collection = _databaseAD.GetCollection<UserColumns>(CONSTANTS.PSBusinessProblem);
                else
                    collection = _database.GetCollection<UserColumns>(CONSTANTS.PSBusinessProblem);
                //var collection = _database.GetCollection<UserColumns>(CONSTANTS.PSBusinessProblem);
                Dictionary<string, string> valuePairs = new Dictionary<string, string>();
                userColumns.IsModelTrained = userColumns.IsModelDeployed = false;

                IMongoCollection<UserColumns> useCaseCollection;
                if (servicename == "Anomaly")
                    useCaseCollection = _databaseAD.GetCollection<UserColumns>(CONSTANTS.PSUseCaseDefinition);
                else
                    useCaseCollection = _database.GetCollection<UserColumns>(CONSTANTS.PSUseCaseDefinition);
                //var useCaseCollection = _database.GetCollection<UserColumns>(CONSTANTS.PSUseCaseDefinition);
                var useCasefilter = Builders<UserColumns>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
                var useCaseprojection = Builders<UserColumns>.Projection.Include("UniqueIdentifier").Include(CONSTANTS.CorrelationId).Include(CONSTANTS.UniquenessDetails).Include(CONSTANTS.ValidRecordsDetails).Exclude(CONSTANTS.Id);
                var resultantData = useCaseCollection.Find(useCasefilter).Project<BsonDocument>(useCaseprojection).ToList();

                //Custom Cascade 
                IMongoCollection<DeployModelsDto> deployColection;
                if (servicename == "Anomaly")
                    deployColection = _databaseAD.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
                else
                    deployColection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
                //var deployColection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
                var filter3 = Builders<DeployModelsDto>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
                var projection3 = Builders<DeployModelsDto>.Projection.Exclude(CONSTANTS.Id);
                var deployResult = deployColection.Find(filter3).Project<DeployModelsDto>(projection3).ToList();
                if (deployResult.Count > 0)
                {
                    userColumns.IsModelTemplateDataSource = deployResult[0].IsModelTemplateDataSource;
                    userColumns.Category = deployResult[0].Category;
                    //Changes for TargetUniqueIdentifier adding for entity by default
                    string[] sourceNames = new string[] { CONSTANTS.pad, CONSTANTS.Custom, CONSTANTS.Metric, CONSTANTS.multidatasource };                    
                    if (sourceNames.Contains(deployResult[0].SourceName))
                    {
                        BsonElement element;
                        if (resultantData.Count > 0)
                        {
                            var exists = resultantData[0].TryGetElement("UniqueIdentifier", out element);
                            if (exists)
                            {
                                userColumns.IsEntityModel = true;
                                userColumns.TargetUniqueIdentifier = !resultantData[0]["UniqueIdentifier"].IsBsonNull ? Convert.ToString(resultantData[0]["UniqueIdentifier"]) : null;
                            }
                        } else
                        {
                            userColumns.TargetUniqueIdentifier = null;
                        }
                    }
                    userColumns.IsIncludedinCustomCascade = deployResult[0].IsIncludedinCustomCascade;
                    userColumns.CustomCascadeId = deployResult[0].CustomCascadeId;
                    userColumns.IsFMModel = deployResult[0].IsFMModel;
                    if (deployResult[0].SourceName.ToUpper() == CONSTANTS.CustomDbQuery.ToUpper() || deployResult[0].SourceName.ToUpper() == CONSTANTS.CustomDataApi.ToUpper())
                    {
                        userColumns.CustomDataPullType = deployResult[0].SourceName;
                    }
                    if (deployResult[0].CustomCascadeId != null & deployResult[0].CustomCascadeId != CONSTANTS.Null & deployResult[0].CustomCascadeId != CONSTANTS.BsonNull)
                    {
                        IMongoCollection<BsonDocument> cascadeModelCollection;
                        if (servicename == "Anomaly")
                            cascadeModelCollection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.SSAICascadedModels);
                        else
                            cascadeModelCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAICascadedModels);
                        //var cascadeModelCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAICascadedModels);
                        var cascadeFilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CascadedId, deployResult[0].CustomCascadeId);
                        var cascadeProjection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.MappingData).Exclude(CONSTANTS.Id);
                        var cascadeModelResult = cascadeModelCollection.Find(cascadeFilter).Project<BsonDocument>(cascadeProjection).ToList();
                        if (cascadeModelResult.Count > 0)
                        {
                            JObject data = JObject.Parse(cascadeModelResult[0].ToString());
                            if (data != null & !string.IsNullOrEmpty(data.ToString()))
                            {
                                userColumns.CascadeModelsCount = data[CONSTANTS.ModelList].Children().Count();
                                int i = 1;
                                foreach (var item in data[CONSTANTS.ModelList].Children())
                                {
                                    var prop = item as JProperty;
                                    if (prop != null)
                                    {
                                        CascadeModelDictionary cascadeModel = JsonConvert.DeserializeObject<CascadeModelDictionary>(prop.Value.ToString());
                                        if (cascadeModel.CorrelationId == correlationId)
                                        {
                                            int modelCount = i - 1;
                                            if (modelCount > 0)
                                            {
                                                string modelname = string.Format("Model{0}", modelCount);
                                                CascadeModelDictionary cascadeModel2 = JsonConvert.DeserializeObject<CascadeModelDictionary>(data[CONSTANTS.ModelList][modelname].ToString());
                                                userColumns.PreviousModelName = cascadeModel2.ModelName;
                                                IMongoCollection<UserColumns> collectionPs;
                                                if (servicename == "Anomaly")
                                                    collectionPs = _databaseAD.GetCollection<UserColumns>(CONSTANTS.PSBusinessProblem);
                                                else
                                                    collectionPs = _database.GetCollection<UserColumns>(CONSTANTS.PSBusinessProblem);
                                                //var collectionPs = _database.GetCollection<UserColumns>(CONSTANTS.PSBusinessProblem);
                                                var psfilter = Builders<UserColumns>.Filter.Eq(CONSTANTS.CorrelationId, cascadeModel2.CorrelationId);
                                                var proj = Builders<UserColumns>.Projection.Exclude(CONSTANTS.Id);
                                                var targtResult = collectionPs.Find(psfilter).Project<UserColumns>(proj).ToList();
                                                if (targtResult.Count > 0)
                                                {
                                                    userColumns.PreviousTargetColumn = targtResult[0].TargetColumn;
                                                }
                                                break;
                                            }
                                        }
                                    }
                                    i++;
                                }

                            }
                        }
                    }
                }
                if (resultantData.Count > 0)
                {
                    userColumns.ValidRecordsDetails = JObject.Parse(resultantData[0][CONSTANTS.ValidRecordsDetails].ToString());
                    userColumns.UniquenessDetails = JObject.Parse(resultantData[0][CONSTANTS.UniquenessDetails].ToString());
                }

                if (newModel)  //New Model Columns
                {
                    List<string> datasource = null;
                    if (AvailableColumns.Count > 0)
                    {
                        userColumns.ColumnsList = AvailableColumns[0].ColumnsList;
                        var dataTypeColumns = AvailableColumns[0].DataType;
                        var BsonElements = dataTypeColumns.ToBsonDocument().ToList();
                        for (int i = 0; i < BsonElements.Count; i++)
                        {
                            valuePairs.Add(BsonElements[i].Name, BsonElements[i].Value.ToString());
                        }
                        userColumns.DataTypeColumns = valuePairs;
                        datasource = CommonUtility.GetDataSourceModel(correlationId, appSettings, servicename);
                        var attributes = CommonUtility.GetCommonAttributes(correlationId, appSettings, servicename);
                        if (datasource.Count > 0)
                        {
                            userColumns.ModelName = attributes.ModelName;
                            userColumns.DataSource = attributes.DataSource;
                            if (newModel)
                            {
                                if (attributes.InstaId != null & attributes.InstaId != CONSTANTS.BsonNull)
                                    userColumns.InstaFLag = true;
                                else
                                    userColumns.InstaFLag = false;
                                userColumns.Category = attributes.Category;
                                userColumns.BusinessProblems = attributes.BusinessProblems;
                            }
                            else
                            {
                                if (attributes.InstaId != null & attributes.InstaId != CONSTANTS.BsonNull)
                                    userColumns.InstaFLag = true;
                                else
                                    userColumns.InstaFLag = false;
                                userColumns.Category = attributes.Category;
                                userColumns.BusinessProblems = attributes.BusinessProblems;
                            }
                        }
                        return userColumns;
                    }
                }
                else if (IsTemplate) //Template Model Columns
                {
                    var templateColumns = Builders<UserColumns>.Projection.Include(CONSTANTS.TargetColumn).Include(CONSTANTS.BusinessProblems).Include(CONSTANTS.AvailableColumns).Include(CONSTANTS.InputColumns)
                        .Include(CONSTANTS.TargetUniqueIdentifier)//Fix for bug no.557915 by Lochan Rao Pawar
                        .Include(CONSTANTS.TimeSeries)
                        .Exclude(CONSTANTS.Id);
                    result = collection.Find(filter).Project<UserColumns>(templateColumns).ToList();
                    if (result.Count > 0)
                    {
                        if (result[0].InputColumns != null)
                            userColumns.InputColumns = result[0].InputColumns;
                        else
                            userColumns.InputColumns = arr;
                        userColumns.TargetColumn = result[0].TargetColumn;
                        userColumns.BusinessProblems = result[0].BusinessProblems;
                        userColumns.AvailableColumns = result[0].AvailableColumns;
                        //Start - Fix for bug no.557915 by Lochan Rao Pawar
                        userColumns.TargetUniqueIdentifier = result[0].TargetUniqueIdentifier;
                        userColumns.IsModelTrained = this.IsModelTrained(correlationId);
                        userColumns.IsModelDeployed = this.IsModelDeployed(correlationId);
                        //End   - Fix for bug no.557915 by Lochan Rao Pawar
                        var timeSeriesData = result[0].TimeSeries;
                        if (timeSeriesData != null)
                        {
                            if (!string.IsNullOrEmpty(timeSeriesData.ToString().Replace(CONSTANTS.openbraket, CONSTANTS.InvertedComma).Replace(CONSTANTS.closebraket, CONSTANTS.InvertedComma).Trim()))
                            {
                                JObject jTimeSeries = JObject.Parse(result[0].TimeSeries.ToString());
                                //Bug : 892178 Suggestion2 requires frequencyList
                                userColumns.FrequencyList = jTimeSeries;
                                userColumns.Aggregation = jTimeSeries["Aggregation"].ToString();
                                userColumns.TimeSeriesColumn = jTimeSeries["TimeSeriesColumn"].ToString();
                            }
                        }
                    }
                }
                else //My Model Columns
                {
                    IMongoCollection<UserColumns> collection2;
                    if (servicename == "Anomaly")
                        collection2 = _databaseAD.GetCollection<UserColumns>(CONSTANTS.PSBusinessProblem);
                    else
                        collection2 = _database.GetCollection<UserColumns>(CONSTANTS.PSBusinessProblem);
                    //var collection2 = _database.GetCollection<UserColumns>(CONSTANTS.PSBusinessProblem);
                    var modelColumns = Builders<UserColumns>.Projection.Include(CONSTANTS.InputColumns).
                   Include(CONSTANTS.TargetColumn).Include(CONSTANTS.BusinessProblems).Include(CONSTANTS.AvailableColumns).
                   Include(CONSTANTS.TimeSeries).Include(CONSTANTS.TargetUniqueIdentifier).Exclude(CONSTANTS.Id);
                    var columnFilter = Builders<UserColumns>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
                    var userColumnsList = collection2.Find(columnFilter).Project<UserColumns>(modelColumns).ToList();
                    if (userColumnsList.Count > 0)
                    {
                        if (userColumnsList[0].InputColumns != null)
                            userColumns.InputColumns = userColumnsList[0].InputColumns;
                        else
                            userColumns.InputColumns = arr;

                        userColumns.TargetColumn = userColumnsList[0].TargetColumn;
                        userColumns.BusinessProblems = userColumnsList[0].BusinessProblems;
                        userColumns.AvailableColumns = userColumnsList[0].AvailableColumns;
                        userColumns.TargetUniqueIdentifier = userColumnsList[0].TargetUniqueIdentifier;
                        userColumns.IsModelTrained = this.IsModelTrained(correlationId);
                        userColumns.IsModelDeployed = this.IsModelDeployed(correlationId);
                        var timeSeriesData = userColumnsList[0].TimeSeries;
                        List<string> datasource = null;
                        datasource = CommonUtility.GetDataSourceModel(correlationId, appSettings, servicename);
                        if (datasource.Count > 0)
                            userColumns.Category = datasource[5];
                        if (timeSeriesData != null)
                        {
                            if (!string.IsNullOrEmpty(timeSeriesData.ToString().Replace(CONSTANTS.openbraket, CONSTANTS.InvertedComma).Replace(CONSTANTS.closebraket, CONSTANTS.InvertedComma).Trim()))
                            {
                                JObject jTimeSeries = JObject.Parse(userColumnsList[0].TimeSeries.ToString());
                                userColumns.FrequencyList = jTimeSeries;
                                userColumns.Aggregation = jTimeSeries["Aggregation"].ToString();
                                userColumns.TimeSeriesColumn = jTimeSeries["TimeSeriesColumn"].ToString();
                            }
                        }
                    }
                    else
                    {
                        userColumns.TargetUniqueIdentifier = userColumnsList[0].TargetUniqueIdentifier;
                    }
                }
                if (AvailableColumns.Count > 0)
                {
                    if (servicename == "Anomaly")
                    {
                        if (AvailableColumns[0].ColumnsList != null && AvailableColumns[0].ColumnsList.Count() == 1 )
                        {
                            if (AvailableColumns[0].DataType != null)
                            {
                                var BsonElements = AvailableColumns[0].DataType.ToBsonDocument().ToList();
                                if(BsonElements[0].Value.ToString() == "Integer")
                                    userColumns.TargetColumn = AvailableColumns[0].ColumnsList[0];
                            }
                        }
                    }
                    if (string.IsNullOrEmpty(userColumns.TargetColumn))
                    {
                        userColumns.ColumnsList = AvailableColumns[0].ColumnsList;
                    }
                    else
                    {
                        List<string> columns = new List<string>();
                        if (AvailableColumns[0].ColumnsList != null)
                        {
                            foreach (var column in AvailableColumns[0].ColumnsList)
                            {
                                if (column != userColumns.TargetColumn)
                                    columns.Add(column);
                            }
                            userColumns.ColumnsList = columns.ToArray();
                        }

                    }
                    var dataTypeColumns = AvailableColumns[0].DataType;
                    if (dataTypeColumns != null)
                    {
                        var BsonElements = dataTypeColumns.ToBsonDocument().ToList();
                        for (int i = 0; i < BsonElements.Count; i++)
                        {
                            valuePairs.Add(BsonElements[i].Name, BsonElements[i].Value.ToString());
                        }
                    }

                    userColumns.DataTypeColumns = valuePairs;
                    List<string> datasource = null;
                    datasource = CommonUtility.GetDataSourceModel(correlationId, appSettings, servicename);
                    var attributes = CommonUtility.GetCommonAttributes(correlationId, appSettings, servicename);
                    if (datasource.Count > 0)
                    {
                        userColumns.ModelName = attributes.ModelName;
                        userColumns.DataSource = attributes.DataSource;
                        if (datasource.Count > 4)
                            userColumns.BusinessProblems = datasource[3];
                        if (newModel)
                        {
                            if (attributes.InstaId != null & attributes.InstaId != CONSTANTS.BsonNull)
                                userColumns.InstaFLag = true;
                            else
                                userColumns.InstaFLag = false;
                            userColumns.Category = attributes.Category;
                            userColumns.BusinessProblems = attributes.BusinessProblems;
                        }
                        else
                        {
                            if (attributes.InstaId != null & attributes.InstaId != CONSTANTS.BsonNull)
                                userColumns.InstaFLag = true;
                            else
                                userColumns.InstaFLag = false;
                            userColumns.Category = attributes.Category;
                            userColumns.BusinessProblems = attributes.BusinessProblems;
                        }
                    }
                    return userColumns;
                }
                userColumns.IsCascadeModel = false;
            }
            return userColumns;
        }
        public List<PublishDeployedModel> GetPublishModels(string userId, string dateFilter, string DeliveryConstructUID, string ClientUId, string ServiceName = "")
        {
            servicename = ServiceName;
            List<PublishDeployedModel> modelList = new List<PublishDeployedModel>();
            try
            {
                if (dateFilter == CONSTANTS.Null || string.IsNullOrEmpty(dateFilter))
                {
                    DateTime months = DateTime.Now.AddDays(-180);
                    dateFilter = months.ToString(CONSTANTS.DateFormat);
                }
                string userId5 = string.Empty;
                string userId2 = userId.Split("@")[0];                
                string userId3 = userId2 + "@mwphoenix.onmicrosoft.com";
                string[] userId4 = null;
                if (userId.Contains("\\\\"))
                {
                    userId4 = userId.Split("\\\\");
                    if (userId4.Length > 1)
                        userId5 = userId4[1];
                }
                else
                {
                    userId4 = userId.Split("\\");
                    if (userId4.Length > 1)
                        userId5 = userId4[1];
                }
                IMongoCollection<PublishDeployedModel> collection;
                IMongoCollection<IngrainRequestQueue> requestCollection;
                if (servicename == "Anomaly")
                {
                    collection = _databaseAD.GetCollection<PublishDeployedModel>(CONSTANTS.SSAIDeployedModels);
                    requestCollection = _databaseAD.GetCollection<IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
                }
                else
                {
                    collection = _database.GetCollection<PublishDeployedModel>(CONSTANTS.SSAIDeployedModels);
                    requestCollection = _database.GetCollection<IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
                }

                //var collection = _database.GetCollection<PublishDeployedModel>(CONSTANTS.SSAIDeployedModels);
                //Private filter
                string encryptedUser5 = userId5;
                string encryptedUser = userId;
                string encryptedUser2 = userId2;
                string encryptedUser3 = userId3;
                if (!string.IsNullOrEmpty(Convert.ToString(userId5)))
                    encryptedUser5 = _encryptionDecryption.Encrypt(Convert.ToString(userId5));
                if (!string.IsNullOrEmpty(Convert.ToString(userId2)))
                    encryptedUser2 = _encryptionDecryption.Encrypt(Convert.ToString(userId2));
                if (!string.IsNullOrEmpty(Convert.ToString(userId)))
                    encryptedUser = _encryptionDecryption.Encrypt(Convert.ToString(userId));
                if (!string.IsNullOrEmpty(Convert.ToString(userId3)))
                    encryptedUser3 = _encryptionDecryption.Encrypt(Convert.ToString(userId3));
                FilterDefinition<PublishDeployedModel> filter = null;
                if (string.IsNullOrEmpty(userId5))
                {
                    filter = (Builders<PublishDeployedModel>.Filter.Regex(CONSTANTS.CreatedByUser, userId2) | Builders<PublishDeployedModel>.Filter.Regex(CONSTANTS.CreatedByUser, encryptedUser2) | Builders<PublishDeployedModel>.Filter.Eq(CONSTANTS.CreatedByUser, userId) | Builders<PublishDeployedModel>.Filter.Eq(CONSTANTS.CreatedByUser, userId2) | Builders<PublishDeployedModel>.Filter.Eq(CONSTANTS.CreatedByUser, userId3)
                        | Builders<PublishDeployedModel>.Filter.Eq(CONSTANTS.CreatedByUser, encryptedUser2) | Builders<PublishDeployedModel>.Filter.Eq(CONSTANTS.CreatedByUser, encryptedUser)| Builders<PublishDeployedModel>.Filter.Eq(CONSTANTS.CreatedByUser, encryptedUser3)) &
                    Builders<PublishDeployedModel>.Filter.Eq(CONSTANTS.DeliveryConstructUID, DeliveryConstructUID) &
                    Builders<PublishDeployedModel>.Filter.Eq(CONSTANTS.ClientUId, ClientUId) &
                    Builders<PublishDeployedModel>.Filter.Gt(CONSTANTS.ModifiedOn, dateFilter) &
                    Builders<PublishDeployedModel>.Filter.Eq(CONSTANTS.IsPrivate, true) &
                    Builders<PublishDeployedModel>.Filter.Eq(CONSTANTS.IsModelTemplate, false) &
                    Builders<PublishDeployedModel>.Filter.Ne(CONSTANTS.HideFMModel, true) &
                    //removing backUp records if any
                    !Builders<PublishDeployedModel>.Filter.Regex(CONSTANTS.CorrelationId, "^.*_backUp.*");
                }
                else
                {
                    filter = (Builders<PublishDeployedModel>.Filter.Regex(CONSTANTS.CreatedByUser, userId5) | Builders<PublishDeployedModel>.Filter.Regex(CONSTANTS.CreatedByUser, encryptedUser5) | Builders<PublishDeployedModel>.Filter.Regex(CONSTANTS.CreatedByUser, userId2) | Builders<PublishDeployedModel>.Filter.Regex(CONSTANTS.CreatedByUser, encryptedUser2) | Builders<PublishDeployedModel>.Filter.Eq(CONSTANTS.CreatedByUser, userId)| Builders<PublishDeployedModel>.Filter.Eq(CONSTANTS.CreatedByUser, encryptedUser) | Builders<PublishDeployedModel>.Filter.Eq(CONSTANTS.CreatedByUser, userId2)| Builders<PublishDeployedModel>.Filter.Eq(CONSTANTS.CreatedByUser, encryptedUser2) | Builders<PublishDeployedModel>.Filter.Eq(CONSTANTS.CreatedByUser, userId3) | Builders<PublishDeployedModel>.Filter.Eq(CONSTANTS.CreatedByUser, encryptedUser3)) &
                    Builders<PublishDeployedModel>.Filter.Eq(CONSTANTS.DeliveryConstructUID, DeliveryConstructUID) &
                    Builders<PublishDeployedModel>.Filter.Eq(CONSTANTS.ClientUId, ClientUId) &
                    Builders<PublishDeployedModel>.Filter.Gt(CONSTANTS.ModifiedOn, dateFilter) &
                    Builders<PublishDeployedModel>.Filter.Eq(CONSTANTS.IsPrivate, true) &
                    Builders<PublishDeployedModel>.Filter.Eq(CONSTANTS.IsModelTemplate, false) &
                    Builders<PublishDeployedModel>.Filter.Ne(CONSTANTS.HideFMModel, true) &
                    //removing backUp records if any
                    !Builders<PublishDeployedModel>.Filter.Regex(CONSTANTS.CorrelationId, "^.*_backUp.*");
                }

                //Public filter
                var publicFilter = Builders<PublishDeployedModel>.Filter.Eq(CONSTANTS.DeliveryConstructUID, DeliveryConstructUID) &
                  Builders<PublishDeployedModel>.Filter.Eq(CONSTANTS.ClientUId, ClientUId) &
                  Builders<PublishDeployedModel>.Filter.Gt(CONSTANTS.ModifiedOn, dateFilter) &
                  Builders<PublishDeployedModel>.Filter.Eq(CONSTANTS.IsPrivate, false) &
                  Builders<PublishDeployedModel>.Filter.Eq(CONSTANTS.IsModelTemplate, false) &
                  Builders<PublishDeployedModel>.Filter.Ne(CONSTANTS.HideFMModel, true) &
                  //removing backUp records if any
                  !Builders<PublishDeployedModel>.Filter.Regex(CONSTANTS.CorrelationId, "^.*_backUp.*");

                //Model Template filter
                var modelTemplateFilter = Builders<PublishDeployedModel>.Filter.Eq(CONSTANTS.IsPrivate, false) &
                   Builders<PublishDeployedModel>.Filter.Eq(CONSTANTS.ClientUId, ClientUId) &
                  (Builders<PublishDeployedModel>.Filter.Eq(CONSTANTS.CreatedByUser, userId) | Builders<PublishDeployedModel>.Filter.Eq(CONSTANTS.CreatedByUser, encryptedUser) | Builders<PublishDeployedModel>.Filter.Eq(CONSTANTS.CreatedByUser, userId2) | Builders<PublishDeployedModel>.Filter.Eq(CONSTANTS.CreatedByUser, encryptedUser2) | Builders<PublishDeployedModel>.Filter.Eq(CONSTANTS.CreatedByUser, userId3) | Builders<PublishDeployedModel>.Filter.Eq(CONSTANTS.CreatedByUser, encryptedUser3)) &
                  Builders<PublishDeployedModel>.Filter.Eq(CONSTANTS.DeliveryConstructUID, DeliveryConstructUID) &
                  Builders<PublishDeployedModel>.Filter.Eq(CONSTANTS.IsModelTemplate, true) &
                  Builders<PublishDeployedModel>.Filter.Gt(CONSTANTS.ModifiedOn, dateFilter) &
                  Builders<PublishDeployedModel>.Filter.Ne(CONSTANTS.HideFMModel, true) &
                  //removing backUp records if any
                  !Builders<PublishDeployedModel>.Filter.Regex(CONSTANTS.CorrelationId, "^.*_backUp.*");

                var projection = Builders<PublishDeployedModel>.Projection.Exclude(CONSTANTS.Id);

                var privateModels = collection.Find(filter).Project<PublishDeployedModel>(projection).ToList();
                var publicModels = collection.Find(publicFilter).Project<PublishDeployedModel>(projection).ToList();
                var modelTemplateModels = collection.Find(modelTemplateFilter).Project<PublishDeployedModel>(projection).ToList();

                modelList = privateModels.Concat(publicModels).Concat(modelTemplateModels).ToList();

                bool dbEncryptionRequired;
                if (modelList.Count() > 0)
                {
                    foreach (var item in modelList)
                    {
                        dbEncryptionRequired = CommonUtility.EncryptDB(item.CorrelationId, appSettings, servicename);
                        if (dbEncryptionRequired)
                        {
                            if (item.InputSample != null)
                                item.InputSample = _encryptionDecryption.Decrypt(item.InputSample);

                            try
                            {
                                if (!string.IsNullOrEmpty(Convert.ToString(item.CreatedByUser)))
                                {
                                    item.CreatedByUser = _encryptionDecryption.Decrypt(item.CreatedByUser);
                                }
                            }
                            catch (Exception ex) { LOGGING.LogManager.Logger.LogProcessInfo(typeof(IngestedDataService), nameof(GetPublishModels) + "User is already decrypted....", ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty); }
                            try
                            {
                                if (!string.IsNullOrEmpty(Convert.ToString(item.ModifiedByUser)))
                                {
                                    item.ModifiedByUser = _encryptionDecryption.Decrypt(item.ModifiedByUser);
                                }
                            }
                            catch (Exception ex) { LOGGING.LogManager.Logger.LogProcessInfo(typeof(IngestedDataService), nameof(GetPublishModels) + "User is already decrypted....", ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty); }
                        }

                        item.Accuracy = Math.Round(item.Accuracy, 2);
                        //Only Public
                        if (!item.IsPrivate && !item.IsModelTemplate && item.CreatedByUser != userId)
                        {
                            item.IsReadOnlyAccess = true;
                        }
                        //if (dbEncryptionRequired)
                        //{
                        //    try
                        //    {
                        //        if (!string.IsNullOrEmpty(Convert.ToString(item.CreatedByUser)))
                        //        {
                        //            item.CreatedByUser = _encryptionDecryption.Decrypt(item.CreatedByUser);
                        //        }
                        //    }
                        //    catch (Exception) { }
                        //    try
                        //    {
                        //        if (!string.IsNullOrEmpty(Convert.ToString(item.ModifiedByUser)))
                        //        {
                        //            item.ModifiedByUser = _encryptionDecryption.Decrypt(item.ModifiedByUser);
                        //        }
                        //    }
                        //    catch (Exception) { }
                        //}
                        if (item.LinkedApps != null)
                        {
                            if (item.LinkedApps.Length > 0)
                            {
                                item.LinkedApp = item.LinkedApps[0];
                            }
                        }
                        if (item.IsFMModel)
                        {
                            if (item.FMCorrelationId != null)
                            {
                                //var requestCollection = _database.GetCollection<IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
                                var filterBuilder = Builders<IngrainRequestQueue>.Filter;
                                var filterqueue = filterBuilder.Eq(CONSTANTS.CorrelationId, item.FMCorrelationId) & filterBuilder.Eq(CONSTANTS.Function, CONSTANTS.FMTransform) & filterBuilder.Eq(CONSTANTS.TemplateUseCaseID, CONSTANTS.FMUseCaseId);
                                var resultfmRequest = requestCollection.Find(filterqueue).Project<IngrainRequestQueue>(Builders<IngrainRequestQueue>.Projection.Exclude(CONSTANTS.Id)).ToList();
                                if (resultfmRequest.Count > 0)
                                {
                                    RequestStatus request = new RequestStatus();
                                    request.Status = resultfmRequest[0].Status;
                                    request.Progress = resultfmRequest[0].Progress;
                                    item.FmModelStaus = request;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(IngestedDataService), nameof(GetPublishModels), ex.Message, ex, string.Empty, string.Empty, ClientUId, DeliveryConstructUID);
            }
            return modelList;
        }

        public void ColumnsFlushforBusinessProblem(string correlationId, string userId, string clientUID, string deliveryUID)
        {
            bool DBEncryptionRequired = CommonUtility.EncryptDB(correlationId, appSettings, servicename);
            if (DBEncryptionRequired)
            {
                if (!string.IsNullOrEmpty(Convert.ToString(userId)))
                {
                    userId = _encryptionDecryption.Encrypt(Convert.ToString(userId));
                }
            }
            List<BusinessProblemDataDTO> list = new List<BusinessProblemDataDTO>();
            BusinessProblemDataDTO businessProblemDataDTO = new BusinessProblemDataDTO();
            IMongoCollection<BsonDocument> collection;
            if (servicename == "Anomaly")
                collection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.PSBusinessProblem);
            else
                collection = _database.GetCollection<BsonDocument>(CONSTANTS.PSBusinessProblem);
            //var collection = _database.GetCollection<BsonDocument>(CONSTANTS.PSBusinessProblem);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var result = collection.Find(filter).ToList();

            if (result.Count > 0)
            {
                businessProblemDataDTO._id = result[0][CONSTANTS.Id].ToString();
                businessProblemDataDTO.BusinessProblems = result[0][CONSTANTS.BusinessProblems].ToString();
                businessProblemDataDTO.CorrelationId = result[0][CONSTANTS.CorrelationId].ToString();
                businessProblemDataDTO.CreatedByUser = userId;
                businessProblemDataDTO.ModifiedByUser = userId;
                businessProblemDataDTO.ClientUId = clientUID;
                businessProblemDataDTO.DeliveryConstructUID = deliveryUID;

                collection.DeleteMany(filter);

                var jsonColumns = Newtonsoft.Json.JsonConvert.SerializeObject(businessProblemDataDTO);
                var insertBsonColumns = BsonSerializer.Deserialize<BsonDocument>(jsonColumns);
                collection = _database.GetCollection<BsonDocument>(CONSTANTS.PSBusinessProblem);
                collection.InsertOne(insertBsonColumns);
            }
        }

        public void DeleteIngestUseCase(string correlationId)
        {
            IMongoCollection<BsonDocument> collection;
            if (servicename == "Anomaly")
                collection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
            else
                collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);

            //var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq(CONSTANTS.CorrelationId, correlationId) & builder.Eq(CONSTANTS.pageInfo, CONSTANTS.IngestData);
            collection.DeleteOne(filter);
        }
        public UseCaseSave InsertColumns(BusinessProblemDataDTO data, string ServiceName = "")
        {
            servicename = ServiceName;
            UseCaseSave useCase = new UseCaseSave();
            useCase.IsInserted = true;
            try
            {
                IMongoCollection<BusinessProblemDataDTO> collection;
                if (servicename == "Anomaly")
                    collection = _databaseAD.GetCollection<BusinessProblemDataDTO>(CONSTANTS.PSBusinessProblem);
                else
                    collection = _database.GetCollection<BusinessProblemDataDTO>(CONSTANTS.PSBusinessProblem);
                string correlationId = data.CorrelationId.ToString();
                bool isProblemTypechange = false;
                var builder = Builders<BsonDocument>.Filter;
                var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
                var filter2 = Builders<BusinessProblemDataDTO>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
                //var collection = _database.GetCollection<BusinessProblemDataDTO>(CONSTANTS.PSBusinessProblem);
                var correaltionExist = collection.Find(filter2).ToList();
                if (correaltionExist.Count > 0)
                {
                    string existingProblemType = string.Empty;
                    if (!string.IsNullOrEmpty(Convert.ToString(correaltionExist[0].ProblemType)))
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(IngestedDataService), nameof(InsertColumns) + "-INSIDE EXIST 1-" + data, "START", string.IsNullOrEmpty(data.CorrelationId) ? default(Guid) : new Guid(data.CorrelationId),data.AppId,string.Empty,data.ClientUId,data.DeliveryConstructUID);
                        existingProblemType = correaltionExist[0].ProblemType;
                        string UIProblemType = data.ProblemType;
                        if (data.ProblemType == CONSTANTS.File || data.ProblemType == CONSTANTS.TimeSeries)
                        {
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(IngestedDataService), nameof(InsertColumns) + "-INSIDE EXIST 2-" + data, "START", string.IsNullOrEmpty(data.CorrelationId) ? default(Guid) : new Guid(data.CorrelationId), data.AppId, string.Empty, data.ClientUId, data.DeliveryConstructUID);
                            if (existingProblemType != UIProblemType)
                            {
                                Flush(data.CorrelationId);
                                isProblemTypechange = true;
                            }
                        }
                    }
                    collection.DeleteOne(filter2);
                }
                bool DBEncryptionRequired = CommonUtility.EncryptDB(correlationId, appSettings, servicename);
                if (DBEncryptionRequired)
                {
                    if (!string.IsNullOrEmpty(Convert.ToString(data.CreatedByUser)))
                    {
                        data.CreatedByUser = _encryptionDecryption.Encrypt(Convert.ToString(data.CreatedByUser));
                    }
                    if (!string.IsNullOrEmpty(Convert.ToString(data.ModifiedByUser)))
                    {
                        data.ModifiedByUser = _encryptionDecryption.Encrypt(Convert.ToString(data.ModifiedByUser));
                    }
                }
                data._id = Guid.NewGuid().ToString();
                IMongoCollection<BsonDocument> collection2;
                if (servicename == "Anomaly")
                    collection2 = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.PSBusinessProblem);
                else
                    collection2 = _database.GetCollection<BsonDocument>(CONSTANTS.PSBusinessProblem);
                var jsonColumns = Newtonsoft.Json.JsonConvert.SerializeObject(data);
                var insertBsonColumns = BsonSerializer.Deserialize<BsonDocument>(jsonColumns);
                //var collection2 = _database.GetCollection<BsonDocument>(CONSTANTS.PSBusinessProblem);
                collection2.InsertOne(insertBsonColumns);

                if (!isProblemTypechange)
                //DataCleanup Remove Start   
                {
                    IMongoCollection<BsonDocument> collectionDataCleanUp;
                    IMongoCollection<BsonDocument> collectionFilteredData;
                    IMongoCollection<BsonDocument> collectionUseCase;
                    IMongoCollection<BsonDocument> transformationCollection;
                    IMongoCollection<BsonDocument> featureCollection;
                    IMongoCollection<BsonDocument> ingrainRequestCollection;
                    IMongoCollection<BsonDocument> collectionAddFeature;
                    if (servicename == "Anomaly")
                    {
                        collectionDataCleanUp = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.DEDataCleanup);
                        collectionFilteredData = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.DataCleanUPFilteredData);
                        collectionUseCase = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
                        transformationCollection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.DEDataProcessing);
                        featureCollection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.MEFeatureSelection);
                        ingrainRequestCollection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
                        collectionAddFeature = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.DEAddNewFeature);
                    }
                    else
                    {
                        collectionDataCleanUp = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataCleanup);
                        collectionFilteredData = _database.GetCollection<BsonDocument>(CONSTANTS.DataCleanUPFilteredData);
                        collectionUseCase = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);

                        transformationCollection = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataProcessing);
                        featureCollection = _database.GetCollection<BsonDocument>(CONSTANTS.MEFeatureSelection);
                        ingrainRequestCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
                        collectionAddFeature = _database.GetCollection<BsonDocument>(CONSTANTS.DEAddNewFeature);
                    }
                    //var collectionDataCleanUp = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataCleanup);
                    //var collectionFilteredData = _database.GetCollection<BsonDocument>(CONSTANTS.DataCleanUPFilteredData);
                    //var collectionUseCase = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
                    var DataCleanExist = collectionDataCleanUp.Find(filter).ToList();
                    if (DataCleanExist.Count > 0)
                    {
                        collectionDataCleanUp.DeleteOne(filter);
                    }
                    var FilteredDataExist = collectionFilteredData.Find(filter).ToList();
                    if (FilteredDataExist.Count > 0)
                    {
                        collectionFilteredData.DeleteOne(filter);
                    }
                    var useCaseFilter = builder.Eq(CONSTANTS.CorrelationId, correlationId) & builder.Eq(CONSTANTS.pageInfo, CONSTANTS.DataCleanUp);
                    var useCaseDataExist = collectionUseCase.Find(useCaseFilter).ToList();
                    if (useCaseDataExist.Count > 0)
                    {
                        collectionUseCase.DeleteOne(useCaseFilter);
                    }
                    //DataCleanup Remove End
                    //Data Transformation Remove Start
                    //var transformationCollection = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataProcessing);
                    var transforamtionResult = transformationCollection.Find(filter).ToList();
                    if (transforamtionResult.Count > 0)
                    {
                        transformationCollection.DeleteOne(filter);
                    }
                    var useCaseFilter2 = builder.Eq(CONSTANTS.CorrelationId, correlationId) & builder.Eq(CONSTANTS.pageInfo, CONSTANTS.DataPreprocessing);
                    var useCaseDataExist2 = collectionUseCase.Find(useCaseFilter).ToList();
                    if (useCaseDataExist2.Count > 0)
                    {
                        collectionUseCase.DeleteOne(useCaseFilter2);
                    }
                    //Data Transformation Remove End
                    //var featureCollection = _database.GetCollection<BsonDocument>(CONSTANTS.MEFeatureSelection);
                    var featureResult = featureCollection.Find(filter).ToList();
                    if (featureResult.Count > 0)
                    {
                        featureCollection.DeleteOne(filter);
                    }
                    
                    //var ingrainRequestCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
                    var filterBuilder = Builders<BsonDocument>.Filter;
                    var filterIngrainRequest = filterBuilder.Eq(CONSTANTS.CorrelationId, correlationId) & (filterBuilder.Eq(CONSTANTS.pageInfo, CONSTANTS.DataCleanUp) | filterBuilder.Eq(CONSTANTS.pageInfo, CONSTANTS.DataPreprocessing));
                    var result = ingrainRequestCollection.Find(filterIngrainRequest).ToList();
                    if (result.Count > 0)
                    {
                        ingrainRequestCollection.DeleteMany(filter);
                    }

                    //Add Feature remove 
                    //var collectionAddFeature = _database.GetCollection<BsonDocument>(CONSTANTS.DEAddNewFeature);
                    var filterAddFeature = collectionAddFeature.Find(filter).ToList();
                    if (filterAddFeature.Count > 0)
                    {
                        collectionAddFeature.DeleteOne(filter);
                    }
                }
            }
            catch (Exception ex)
            {
                useCase.IsInserted = false;
                useCase.ErrorMessage = ex.Message + ex.StackTrace;
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(IngestedDataService), nameof(InsertColumns), ex.Message + "StackTrace-" + ex.StackTrace, ex, data.AppId, string.Empty, data.ClientUId, data.DeliveryConstructUID);
            }
            return useCase;
        }

        public void Flush(string CorrelationId)
        {
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, CorrelationId);
            var filter1 = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, CorrelationId) & (Builders<BsonDocument>.Filter.Ne(CONSTANTS.pageInfo, CONSTANTS.IngestData));

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

            }
            // var collection_DataCleanup = _database.GetCollection<BsonDocument>(CONSTANTS.in);
            var collection_DataCleanup = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataCleanup);
            var collection_DataProcessing = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataProcessing);
            // var collection_DeployedPublishModel = _database.GetCollection<BsonDocument>(CONSTANTS.DeployedPublishModel);
            var collection_IngrainDeliveryConstruct = _database.GetCollection<BsonDocument>(CONSTANTS.IngrainDeliveryConstruct);
            var collection_HyperTuneVersion = _database.GetCollection<BsonDocument>(CONSTANTS.ME_HyperTuneVersion);
            var collection_SSAI_RecommendedTrainedModels = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIRecommendedTrainedModels);
            // var collection_SSAI_UserDetails = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIUserDetails);
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
            var collection_UseCase = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
            var collection_PA = _database.GetCollection<BsonDocument>(CONSTANTS.PrescriptiveAnalyticsResults);

            if (collection_DataCleanup.Find(filter).ToList().Count > 0)
            {
                collection_DataCleanup.DeleteMany(filter);

            }

            if (collection_DataProcessing.Find(filter).ToList().Count > 0)
            {
                collection_DataProcessing.DeleteMany(filter);

            }

            if (collection_IngrainDeliveryConstruct.Find(filter).ToList().Count > 0)
            {
                collection_IngrainDeliveryConstruct.DeleteMany(filter);

            }
            if (collection_HyperTuneVersion.Find(filter).ToList().Count > 0)
            {
                collection_HyperTuneVersion.DeleteMany(filter);

            }
            if (collection_SSAI_RecommendedTrainedModels.Find(filter).ToList().Count > 0)
            {
                collection_SSAI_RecommendedTrainedModels.DeleteMany(filter);

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
            if (collection_PA.Find(filter).ToList().Count > 0)
            {
                collection_PA.DeleteMany(filter);

            }
            if (collection_DataVisualization.Find(filter).ToList().Count > 0)
            {
                collection_DataVisualization.DeleteMany(filter);

            }
            if (collection_PreProcessedData.Find(filter).ToList().Count > 0)
            {
                collection_PreProcessedData.DeleteMany(filter);

            }
            if (collection_FilteredData.Find(filter).ToList().Count > 0)
            {
                collection_FilteredData.DeleteMany(filter);

            }
            if (collection_FeatureSelection.Find(filter).ToList().Count > 0)
            {
                collection_FeatureSelection.DeleteMany(filter);

            }
            if (collection_RecommendedModels.Find(filter).ToList().Count > 0)
            {
                collection_RecommendedModels.DeleteMany(filter);

            }
            if (collection_TeachAndTest.Find(filter).ToList().Count > 0)
            {
                collection_TeachAndTest.DeleteMany(filter);
            }
            if (collection_UseCase.Find(filter1).ToList().Count > 0)
            {
                collection_UseCase.DeleteMany(filter1);
            }

            if (collection_DeliveryConstructStructures.Find(filter).ToList().Count > 0)
            {
                collection_DeliveryConstructStructures.DeleteMany(filter);
            }
            var mdl = collection_DeployedModels.Find(filter).ToList();
            if (mdl.Count > 0)
            {
                string isUpdated = "False";
                if (mdl[0]["Status"] == "Deployed")
                {
                    isUpdated = "True";
                }
                DeployedModel deployedModel = new DeployedModel();
                var builder = Builders<BsonDocument>.Update;
                var update = builder.Set(CONSTANTS.Accuracy, deployedModel.Accuracy)
                    .Set(CONSTANTS.ModelURL, deployedModel.Url)
                    .Set(CONSTANTS.VDSLink, deployedModel.VdsLink)
                    .Set(CONSTANTS.LinkedApps, deployedModel.App)
                    .Set(CONSTANTS.Frequency, deployedModel.Frequency)
                    .Set(CONSTANTS.Status, CONSTANTS.InProgress)
                    .Set(CONSTANTS.WebServices, deployedModel.WebServices)
                    .Set(CONSTANTS.DeployedDate, deployedModel.DeployedDate)
                    .Set(CONSTANTS.IsPrivate, true)
                    .Set(CONSTANTS.IsModelTemplate, false)
                    .Set(CONSTANTS.ModelVersion, deployedModel.ModelVersion)
                    .Set(CONSTANTS.ModelType, string.Empty)
                    .Set(CONSTANTS.InputSample, string.Empty)
                    .Set("IsUpdated", isUpdated)
                    .Set(CONSTANTS.TrainedModelId, deployedModel.TrainedModelId);
                var result = collection_DeployedModels.UpdateMany(filter, update);

            }
            if (collection_PublicTemplates.Find(filter).ToList().Count > 0)
            {
                collection_PublicTemplates.DeleteMany(filter);

            }
            if (collection_PublishModel.Find(filter).ToList().Count > 0)
            {
                collection_PublishModel.DeleteMany(filter);

            }


        }

        public void InsertPreProcessData(PreProcessDataDto data)
        {
            var jsonColumns = Newtonsoft.Json.JsonConvert.SerializeObject(data);
            var insertBsonColumns = BsonSerializer.Deserialize<BsonDocument>(jsonColumns);
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.DEPreProcessedData);
            collection.InsertOne(insertBsonColumns);
        }
        public void InsertRequests(IngrainRequestQueue ingrainRequest, string ServiceName = "")
        {
            servicename = ServiceName;
            bool DBEncryptionRequired = CommonUtility.EncryptDB(ingrainRequest.CorrelationId, appSettings, servicename);
            if (DBEncryptionRequired)
            {
                if (!string.IsNullOrEmpty(Convert.ToString(ingrainRequest.CreatedByUser)))
                {
                    ingrainRequest.CreatedByUser = _encryptionDecryption.Encrypt(Convert.ToString(ingrainRequest.CreatedByUser));
                }
                if (!string.IsNullOrEmpty(Convert.ToString(ingrainRequest.ModifiedByUser)))
                {
                    ingrainRequest.ModifiedByUser = _encryptionDecryption.Encrypt(Convert.ToString(ingrainRequest.ModifiedByUser));
                }
            }
            IMongoCollection<BsonDocument> collection;
            if (servicename == "Anomaly")
                collection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
            else
                collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
            var requestQueue = JsonConvert.SerializeObject(ingrainRequest);
            var insertRequestQueue = BsonSerializer.Deserialize<BsonDocument>(requestQueue);
            //var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
            collection.InsertOne(insertRequestQueue);
        }
        public IngrainRequestQueue GetFileRequestStatus(string correlationId, string pageInfo)
        {
            IMongoCollection<IngrainRequestQueue> collection;
            if (servicename == "Anomaly")
                collection = _databaseAD.GetCollection<IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
            else
                collection = _database.GetCollection<IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
            IngrainRequestQueue ingrainRequest = new IngrainRequestQueue();
            //var collection = _database.GetCollection<IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
            var builder = Builders<IngrainRequestQueue>.Filter;
            var filter = builder.Eq(CONSTANTS.CorrelationId, correlationId) & builder.Eq(CONSTANTS.pageInfo, pageInfo);
            //var filter = Builders<IngrainRequestQueue>.Filter.Eq("CorrelationId", correlationId);
            return ingrainRequest = collection.Find(filter).ToList().FirstOrDefault();
            //if(result.Count > 0)
            //{
            //    return result[0];
            //}
            //else
            //{
            //    return ingrainRequest;
            //}            
        }
        public ValidRecordsDetailsModel GetDataPoints(string correlationId)
        {
            //Less Data Points
            ValidRecordsDetailsModel validRecordsDetailModel = CommonUtility.GetModelDataPointdDetails(correlationId, appSettings);            
            return validRecordsDetailModel;
        }
        public IngrainRequestQueue GetFileRequestStatus(string correlationId, string pageInfo, string wfId)
        {
            IMongoCollection<IngrainRequestQueue> collection;
            if (servicename == "Anomaly")
                collection = _databaseAD.GetCollection<IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
            else
                collection = _database.GetCollection<IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
            IngrainRequestQueue ingrainRequest = new IngrainRequestQueue();
            //var collection = _database.GetCollection<IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
            var builder = Builders<IngrainRequestQueue>.Filter;
            var filter = builder.Eq(CONSTANTS.CorrelationId, correlationId) & builder.Eq(CONSTANTS.pageInfo, pageInfo) & builder.Eq(CONSTANTS.UniId, wfId);
            var projection = Builders<IngrainRequestQueue>.Projection.Exclude(CONSTANTS.Id);
            return ingrainRequest = collection.Find(filter).Project<IngrainRequestQueue>(projection).ToList().FirstOrDefault();
            //if(result.Count > 0)
            //{
            //    return result[0];
            //}
            //else
            //{
            //    return ingrainRequest;
            //}
        }
        public string GetRequestUsecase(string correlationId, string pageInfo, string ServiceName = "")
        {
            string ingrainRequest = string.Empty;
            IMongoCollection<BsonDocument> collection;
            if (ServiceName == "Anomaly")
                collection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
            else
                collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
            //var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq(CONSTANTS.CorrelationId, correlationId) & builder.Eq(CONSTANTS.pageInfo, pageInfo);
            var projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
            var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            bool DBEncryptionRequired = CommonUtility.EncryptDB(correlationId, appSettings, ServiceName);
            if (result.Count > 0)
            {
                if (DBEncryptionRequired)
                {
                    try
                    {
                        if (result[0].Contains("CreatedByUser") && !string.IsNullOrEmpty(Convert.ToString(result[0]["CreatedByUser"])))
                            result[0]["CreatedByUser"] = _encryptionDecryption.Decrypt(Convert.ToString(result[0]["CreatedByUser"]));
                    }
                    catch (Exception ex) { LOGGING.LogManager.Logger.LogProcessInfo(typeof(IngestedDataService), nameof(GetRequestUsecase) + "User is already decrypted....", ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty); }
                    try
                    {
                        if (result[0].Contains("ModifiedByUser") && !string.IsNullOrEmpty(Convert.ToString(result[0]["ModifiedByUser"])))
                            result[0]["ModifiedByUser"] = _encryptionDecryption.Decrypt(Convert.ToString(result[0]["ModifiedByUser"]));
                    }
                    catch (Exception ex) { LOGGING.LogManager.Logger.LogProcessInfo(typeof(IngestedDataService), nameof(GetRequestUsecase) + "User is already decrypted....", ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty); }
                }
                ingrainRequest = result[0].ToJson();
            }
            return ingrainRequest;
        }

        public string GetDefaultEntityName(string correlationid)
        {
            string dataSource = string.Empty;
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq(CONSTANTS.CorrelationId, correlationid);
            var projection = Builders<BsonDocument>.Projection.Include("DataSource").Include(CONSTANTS.CorrelationId).Exclude(CONSTANTS.Id);
            var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                var dsList = result[0]["DataSource"].AsString.Split(",");
                dataSource = dsList[dsList.Length - 1];
            }
            return dataSource;
        }

        public List<IngrainRequestQueue> GetMultipleRequestStatus(string correlationId, string pageInfo)
        {
            List<IngrainRequestQueue> ingrainRequest = new List<IngrainRequestQueue>();
            var collection = _database.GetCollection<IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
            var builder = Builders<IngrainRequestQueue>.Filter;
            var filter = builder.Eq(CONSTANTS.CorrelationId, correlationId) & builder.Eq(CONSTANTS.pageInfo, pageInfo);
            return ingrainRequest = collection.Find(filter).ToList();
        }
        public void InsertDeployModels(IngestedDataDTO data, bool encryptionFlag, bool IsModelTemplateDataSource, string dataSetUId, string usecaseId)
        {
            string[] arr = new string[] { };
            PublishModelFrequency frequency = new PublishModelFrequency();
            DeployModelsDto deployModel = new DeployModelsDto
            {
                _id = Guid.NewGuid().ToString(),
                CorrelationId = data.CorrelationId,
                DataSetUId = dataSetUId,
                InstaId = null,
                ModelName = data.ModelName,
                Status = CONSTANTS.InProgress,
                ClientUId = data.ClientUID,
                DeliveryConstructUID = data.DeliveryConstructUID,
                DataSource = data.DataSource,
                DeployedDate = null,
                LinkedApps = arr,
                ModelVersion = null,
                ModelType = null,
                SourceName = data.SourceName,
                VDSLink = null,
                InputSample = null,
                IsPrivate = true,
                IsModelTemplate = false,
                DBEncryptionRequired = encryptionFlag,
                TrainedModelId = null,
                Frequency = null,
                Category = data.Category,
                CreatedByUser = encryptionFlag ? _encryptionDecryption.Encrypt(Convert.ToString(data.CreatedByUser)) : data.CreatedByUser,
                CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                ModifiedByUser = encryptionFlag ? _encryptionDecryption.Encrypt(Convert.ToString(data.ModifiedByUser)) : data.ModifiedByUser,
                ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                Language = data.Language,
                IsModelTemplateDataSource = IsModelTemplateDataSource,
                MaxDataPull = 0,
                IsCarryOutRetraining = false,
                IsOffline = false,
                IsOnline = false,
                Training = frequency,
                Prediction = frequency,
                Retraining = frequency                
            };
            if (usecaseId != "" && !string.IsNullOrEmpty(usecaseId) && usecaseId != CONSTANTS.undefined)
            {
                if (usecaseId == CONSTANTS.FMUseCaseId)
                    deployModel.IsFMModel = true;
                if(IsModelTemplateDataSource)
                {
                    IMongoCollection<DeployModelsDto> modelCollection;
                    if (servicename == "Anomaly")
                        modelCollection = _databaseAD.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
                    else
                        modelCollection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
                    //var modelCollection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
                    var projection = Builders<DeployModelsDto>.Projection.Include(CONSTANTS.MaxDataPull).Exclude(CONSTANTS.Id);                   
                    var filter = Builders<DeployModelsDto>.Filter.Eq(CONSTANTS.CorrelationId, usecaseId);
                    var modelsData = modelCollection.Find(filter).Project<DeployModelsDto>(projection).FirstOrDefault();
                    if (modelsData != null)
                    {
                        deployModel.MaxDataPull = Convert.ToInt32(modelsData.MaxDataPull);
                    }
                }
            }
            IMongoCollection<BsonDocument> collection;
            if (servicename == "Anomaly")
                collection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
            else
                collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
            var jsonColumns = Newtonsoft.Json.JsonConvert.SerializeObject(deployModel);
            var insertBsonColumns = BsonSerializer.Deserialize<BsonDocument>(jsonColumns);
            //var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
            collection.InsertOne(insertBsonColumns);
        }

        public void InsertDataSourceDeployModels(IngestedDataDTO data, bool encryptionFlag, string dataSetUId)
        {
            string[] arr = new string[] { };
            PublishModelFrequency frequency = new PublishModelFrequency();
            DeployModelsDto deployModel = new DeployModelsDto
            {
                _id = Guid.NewGuid().ToString(),
                CorrelationId = data.CorrelationId,
                DataSetUId = dataSetUId,
                ModelName = data.ModelName,
                Status = CONSTANTS.InProgress,
                ClientUId = data.ClientUID,
                DeliveryConstructUID = data.DeliveryConstructUID,
                DataSource = data.DataSource,
                Category = data.Category,
                DeployedDate = null,
                LinkedApps = arr,
                ModelVersion = null,
                ModelType = null,
                VDSLink = null,
                InputSample = null,
                IsPrivate = true,
                IsModelTemplate = false,
                DBEncryptionRequired = encryptionFlag,
                TrainedModelId = null,
                Frequency = null,
                CreatedByUser = encryptionFlag ? _encryptionDecryption.Encrypt(Convert.ToString(data.CreatedByUser)) : data.CreatedByUser,
                CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                ModifiedByUser = encryptionFlag ? _encryptionDecryption.Encrypt(Convert.ToString(data.ModifiedByUser)) : data.ModifiedByUser,
                ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                Language = data.Language,
                SourceName = data.SourceName,
                IsCarryOutRetraining = false,
                IsOffline = false,
                IsOnline = false,
                Retraining = frequency,
                Training = frequency,
                Prediction = frequency               
            };

            IMongoCollection<BsonDocument> collection;
            if (servicename == "Anomaly")
                collection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
            else
                collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, data.CorrelationId);
            //var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);

            var correaltionExist = collection.Find(filter).ToList();
            if (correaltionExist.Count > 0)
            {
                if (correaltionExist[0]["Status"] == "Deployed")
                {
                    deployModel.IsUpdated = "True";
                }
                collection.DeleteMany(filter);
            }
            var jsonColumns = Newtonsoft.Json.JsonConvert.SerializeObject(deployModel);
            var insertBsonColumns = BsonSerializer.Deserialize<BsonDocument>(jsonColumns);
            collection.InsertOne(insertBsonColumns);
        }

        public PublicTemplateModel GetPublicTemplates(string category, string ServiceName = "")
        {
            servicename = ServiceName;
            IMongoCollection<BsonDocument> collection;
            IMongoCollection<BsonDocument> pscollection;
            IMongoCollection<BsonDocument> deployCollection;
            IMongoCollection<BsonDocument> cascadeCollection;
            if (servicename == "Anomaly")
            {
                collection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.SSAIPublicTemplates);
                pscollection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.PSBusinessProblem);
                deployCollection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.SSAI_DeployedModels);
                cascadeCollection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.SSAICascadedModels);
            }
            else
            {
                collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIPublicTemplates);
                pscollection = _database.GetCollection<BsonDocument>(CONSTANTS.PSBusinessProblem);
                deployCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_DeployedModels);
                cascadeCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAICascadedModels);
            }
            PublicTemplateModel publicTemplateModel = new PublicTemplateModel();
            List<PublicTemplates> list = new List<PublicTemplates>();

            //var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIPublicTemplates);
            var builder = Builders<BsonDocument>.Filter;
            var result = new List<BsonDocument>();
            string[] systemIntegrationCategories = new string[] { CONSTANTS.Application_Development, CONSTANTS.releaseManagement, CONSTANTS.AD, CONSTANTS.SystemIntegration };
            if (category == CONSTANTS.Application_Development || category == CONSTANTS.releaseManagement)
            {
                //var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.ModelName1, CONSTANTS.Application_Development) | Builders<BsonDocument>.Filter.Eq(CONSTANTS.ModelName1, CONSTANTS.releaseManagement);
                var filter = Builders<BsonDocument>.Filter.In(CONSTANTS.ModelName1, systemIntegrationCategories);
                var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.ModelName).Include(CONSTANTS.CorrelationId).Include("IsMarketPlaceTemplate").Exclude(CONSTANTS.Id);
                result = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            }
            else
            {
                var filter = builder.Eq(CONSTANTS.ModelName1, category);
                var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.ModelName).Include(CONSTANTS.CorrelationId).Include(CONSTANTS.Custom).Include("IsMarketPlaceTemplate").Exclude(CONSTANTS.Id);
                result = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            }

           // var pscollection = _database.GetCollection<BsonDocument>(CONSTANTS.PSBusinessProblem);
            var psProjection = Builders<BsonDocument>.Projection.Include(CONSTANTS.BusinessProblems).Include(CONSTANTS.CorrelationId).Exclude(CONSTANTS.Id);
            var psData = pscollection.Find(new BsonDocument()).Project<BsonDocument>(psProjection).ToList();

            //var deployCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_DeployedModels);
            var deployFilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.Status, CONSTANTS.Deployed);
            var deployProjection = Builders<BsonDocument>.Projection.Include(CONSTANTS.LinkedApps).Include("Category").Include(CONSTANTS.CorrelationId).Exclude(CONSTANTS.Id);
            var deployData = deployCollection.Find(deployFilter).Project<BsonDocument>(deployProjection).ToList();

            var categories = collection.Find(new BsonDocument()).
               Project<BsonDocument>(Builders<BsonDocument>.
               Projection.Include(CONSTANTS.Category).Exclude(CONSTANTS.Id)).FirstOrDefault().ToString();
            if (result.Count > 0)
            {
                publicTemplateModel.Categories = categories;
                foreach (var bp in psData)
                {
                    PublicTemplates publicTemplates = new PublicTemplates();
                    foreach (var item in result)
                    {
                        if (bp[CONSTANTS.CorrelationId].ToString() == item[CONSTANTS.CorrelationId].ToString())
                        {
                            publicTemplates.BusinessProblem = bp[CONSTANTS.BusinessProblems].ToString();
                            publicTemplates.CorrelationId = item[CONSTANTS.CorrelationId].ToString();
                            publicTemplates.ModelName = item[CONSTANTS.ModelName].ToString();
                            publicTemplates.IsCascadeModelTemplate = false;
                            if (item.Contains("IsMarketPlaceTemplate"))
                            {
                                publicTemplates.IsMarketPlaceTemplate = item["IsMarketPlaceTemplate"].ToBoolean();
                            }
                            if (item.Contains("Custom"))
                                publicTemplates.Custom = item["Custom"].ToString();
                            else
                                publicTemplates.Custom = null;

                            foreach (var appName in deployData)
                            {
                                if (appName[CONSTANTS.CorrelationId].ToString() == item[CONSTANTS.CorrelationId].ToString())
                                {
                                    var app = appName[CONSTANTS.LinkedApps].ToString();
                                    publicTemplates.LinkedApp = app;
                                    if (appName.Contains("Category"))
                                        publicTemplates.Category = appName["Category"].ToString();
                                }
                            }
                            list.Add(publicTemplates);
                        }
                    }
                }
            }
            if (result.Count > 0)
            {

                foreach (var item in result)
                {
                    PublicTemplates publicTemplates = new PublicTemplates();
                    //var cascadeCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAICascadedModels);
                    var cascadeFilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CascadedId, item[CONSTANTS.CorrelationId].ToString());
                    var cascadeProjection = Builders<BsonDocument>.Projection.Exclude("MappingData").Exclude(CONSTANTS.Id);
                    var cascadeResult = cascadeCollection.Find(cascadeFilter).Project<BsonDocument>(cascadeProjection).ToList();
                    if (cascadeResult.Count > 0)
                    {
                        publicTemplates.BusinessProblem = CONSTANTS.Cascading;
                        publicTemplates.CorrelationId = item[CONSTANTS.CorrelationId].ToString();
                        publicTemplates.ModelName = item[CONSTANTS.ModelName].ToString();
                        publicTemplates.IsCascadeModelTemplate = true;
                        if (item.Contains("Custom"))
                            publicTemplates.Custom = item["Custom"].ToString();
                        else
                            publicTemplates.Custom = null;

                        foreach (var appName in deployData)
                        {
                            if (appName[CONSTANTS.CorrelationId].ToString() == item[CONSTANTS.CorrelationId].ToString())
                            {
                                var app = appName[CONSTANTS.LinkedApps].ToString();
                                publicTemplates.LinkedApp = app;
                                if (appName.Contains("Category"))
                                    publicTemplates.Category = appName["Category"].ToString();
                            }
                        }
                        list.Add(publicTemplates);
                    }
                }
                publicTemplateModel.publicTemplates = list;
            }
            return publicTemplateModel;
        }

        /// <summary>
        /// Split the file into 15 mb Documents based on the one Row size.
        /// </summary>
        /// <param name="ingestedData"></param>
        /// <param name="dynaimcFileData"></param>
        /// <param name="rowsPerDocument"></param>
        /// <param name=""></param>
        public void AssignValues(IngestedDataDTO ingestedData, dynamic dynaimcFileData, double rowsPerDocument, List<string> columns, IFormFile postedFile)
        {
            List<string> ingestedDataList = new List<string>();

            ingestedData.CorrelationId = Guid.NewGuid().ToString();
            bool DBEncryptionRequired = CommonUtility.EncryptDB(ingestedData.CorrelationId, appSettings);
            double rows = rowsPerDocument;
            int row = 0;
            long length = dynaimcFileData.Count;
            double j = length;
            while (j > 0)
            {
                for (; row <= rowsPerDocument; row++)
                {
                    if (row == length)
                        break;
                    var str = Convert.ToString(dynaimcFileData[row]);
                    ingestedDataList.Add(str);
                }
                ingestedData.Inputdata = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(ingestedDataList));
                StringBuilder columnBuilder = new StringBuilder();
                foreach (var column in columns)
                {
                    columnBuilder.Append(column + CONSTANTS.comma);
                }
                columnBuilder.Remove(columnBuilder.Length - 1, 1);
                string[] columnsArray = columnBuilder.ToString().Split(CONSTANTS.comma_);
                ingestedData._id = Guid.NewGuid().ToString();
                ingestedData.DataSource = postedFile.FileName;
                ingestedData.userRole = "A";
                ingestedData.Sourcetype = System.IO.Path.GetExtension(postedFile.FileName);
                ingestedData.Size = postedFile.Length;
                ingestedData.AppId = Guid.NewGuid().ToString();
                ingestedData.ShortDescription = postedFile.FileName;
                ingestedData.CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                ingestedData.ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                ingestedData.ColumnsList = columnsArray;
                if (DBEncryptionRequired)
                {
                    if (!string.IsNullOrEmpty(Convert.ToString(ingestedData.CreatedByUser)))
                    {
                        ingestedData.CreatedByUser = _encryptionDecryption.Encrypt(Convert.ToString(ingestedData.CreatedByUser));
                    }
                    if (!string.IsNullOrEmpty(Convert.ToString(ingestedData.ModifiedByUser)))
                    {
                        ingestedData.ModifiedByUser = _encryptionDecryption.Encrypt(Convert.ToString(ingestedData.ModifiedByUser));
                    }
                }
                InsertData(ingestedData);
                ingestedDataList.Clear();
                row = row + 1; ;
                rowsPerDocument = rowsPerDocument + rows;
                j = j - rows;
            }
        }

        /// <summary>
        /// Split the file into 15 mb Documents based on the one Row size.
        /// </summary>
        /// <param name="ingestedData"></param>
        /// <param name="dynaimcFileData"></param>
        /// <param name="rowsPerDocument"></param>
        /// <param name=""></param>
        public void DataSourceAssignValues(IngestedDataDTO ingestedData, dynamic dynaimcFileData, double rowsPerDocument, List<string> columns, IFormFile postedFile, string correlationId)
        {
            List<string> ingestedDataList = new List<string>();

            if (ingestedData.CorrelationId == CONSTANTS.InvertedComma)
            {
                ingestedData.CorrelationId = Guid.NewGuid().ToString();
            }
            else
            {
                ingestedData.CorrelationId = correlationId;
            }
            bool DBEncryptionRequired = CommonUtility.EncryptDB(ingestedData.CorrelationId, appSettings);
            double rows = rowsPerDocument;
            int row = 0;
            long length = dynaimcFileData.Count;
            double j = length;
            while (j > 0)
            {
                for (; row <= rowsPerDocument; row++)
                {
                    if (row == length)
                        break;
                    var str = Convert.ToString(dynaimcFileData[row]);
                    ingestedDataList.Add(str);
                }
                ingestedData.Inputdata = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(ingestedDataList));
                StringBuilder columnBuilder = new StringBuilder();
                foreach (var column in columns)
                {
                    columnBuilder.Append(column + CONSTANTS.comma);
                }
                columnBuilder.Remove(columnBuilder.Length - 1, 1);
                string[] columnsArray = columnBuilder.ToString().Split(CONSTANTS.comma_);
                ingestedData._id = Guid.NewGuid().ToString();
                ingestedData.DataSource = postedFile.FileName;
                ingestedData.userRole = CONSTANTS.A;
                ingestedData.Sourcetype = System.IO.Path.GetExtension(postedFile.FileName);
                ingestedData.Size = postedFile.Length;
                ingestedData.AppId = Guid.NewGuid().ToString();
                ingestedData.ShortDescription = postedFile.FileName;
                ingestedData.CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                ingestedData.ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                ingestedData.ColumnsList = columnsArray;
                if (DBEncryptionRequired)
                {
                    if (!string.IsNullOrEmpty(Convert.ToString(ingestedData.CreatedByUser)))
                    {
                        ingestedData.CreatedByUser = _encryptionDecryption.Encrypt(Convert.ToString(ingestedData.CreatedByUser));
                    }
                    if (!string.IsNullOrEmpty(Convert.ToString(ingestedData.ModifiedByUser)))
                    {
                        ingestedData.ModifiedByUser = _encryptionDecryption.Encrypt(Convert.ToString(ingestedData.ModifiedByUser));
                    }
                }
                InsertDataSource(ingestedData);
                ingestedDataList.Clear();
                row = row + 1; ;
                rowsPerDocument = rowsPerDocument + rows;
                j = j - rows;
            }
        }

        public string RemoveColumns(string correlationId, string[] prescriptionColumns)
        {
            var columnCollection = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataCleanup);
            string test = string.Empty;
            var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.FeatureName).Include(CONSTANTS.CorrelationToRemove).Exclude(CONSTANTS.Id);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var result = columnCollection.Find(filter).Project<BsonDocument>(projection).ToList();
            StringBuilder resultReport = new StringBuilder();
            List<string> dataList = new List<string>();
            bool DBEncryptionRequired = CommonUtility.EncryptDB(correlationId, appSettings);
            if (result.Count() > 0)
            {
                for (int i = 0; i < result.Count; i++)
                {
                    //decrypt db data
                    if (DBEncryptionRequired)
                    {
                        //BsonDocument processDocument = result[i];
                        //processDocument[CONSTANTS.FeatureName] = BsonDocument.Parse(_encryptionDecryption.Decrypt(result[i][CONSTANTS.FeatureName].AsString));
                        //result[i][CONSTANTS.FeatureName] = processDocument[CONSTANTS.FeatureName].ToString();
                        result[i][CONSTANTS.FeatureName] = BsonDocument.Parse(_encryptionDecryption.Decrypt(result[i][CONSTANTS.FeatureName].AsString));
                    }
                    JObject data = new JObject();
                    data = JObject.Parse(result[i].ToString());
                    var featureExist = data[CONSTANTS.FeatureName];
                    if (featureExist != null)
                    {
                        JObject removeColumns = new JObject();
                        foreach (var columns in prescriptionColumns)
                        {
                            foreach (var features in data[CONSTANTS.FeatureName].Children().ToList())
                            {
                                string script = string.Empty;
                                JProperty jpfeatures = features as JProperty;
                                if (jpfeatures.Name == columns)
                                {
                                    JObject header = (JObject)data[CONSTANTS.FeatureName];
                                    header.Property(columns).Remove();
                                    // BsonDocument doc = BsonDocument.Parse(data.ToString());
                                    // var update = Builders<BsonDocument>.Update.Set(CONSTANTS.FeatureName, doc[CONSTANTS.FeatureName]);
                                    // columnCollection.UpdateMany(filter, update);
                                    if (!dataList.Contains(columns))
                                    {
                                        dataList.Add(columns);
                                    }
                                }

                                else
                                {
                                    foreach (var featuresChild in data[CONSTANTS.FeatureName][jpfeatures.Name][CONSTANTS.Correlation].Children().ToList())
                                    {
                                        JProperty jpfeaturesChild = featuresChild as JProperty;
                                        if (jpfeaturesChild.Value.ToString() == columns)
                                        {
                                            //string values = string.Format("Feature Name.{0}.Correlation", jpfeatures.Name);
                                            //JObject header = (JObject)data.SelectToken(values);
                                            JObject header = (JObject)data[CONSTANTS.FeatureName][jpfeatures.Name][CONSTANTS.Correlation];
                                            header.Property(jpfeaturesChild.Name).Remove();
                                            data[CONSTANTS.FeatureName][jpfeatures.Name][CONSTANTS.Correlation] = header;
                                            //BsonDocument doc = BsonDocument.Parse(data.ToString());
                                            //string field = string.Format(CONSTANTS.Feature_Name_0_Correlation, jpfeatures.Name);
                                            //var update = Builders<BsonDocument>.Update.Set(field, doc[CONSTANTS.FeatureName][jpfeatures.Name][CONSTANTS.Correlation]);
                                            //columnCollection.UpdateMany(filter, update);
                                            if (!dataList.Contains(columns))
                                            {
                                                dataList.Add(columns);
                                            }
                                        }
                                    }
                                }

                                resultReport.Append(CONSTANTS.Updated_in_Feature_Name_for + columns + CONSTANTS.comma);
                            }

                            foreach (var features in data[CONSTANTS.CorrelationToRemove].Children().ToList())
                            {
                                string scriptCorrelation = string.Empty;
                                JProperty jpfeatures = features as JProperty;
                                if (jpfeatures.Name == columns)
                                {
                                    //JObject header = (JObject)data.SelectToken("CorrelationToRemove");
                                    JObject header = (JObject)data.SelectToken(CONSTANTS.CorrelationToRemove);
                                    header.Property(columns).Remove();
                                    BsonDocument doc = BsonDocument.Parse(data.ToString());
                                    var update = Builders<BsonDocument>.Update.Set(CONSTANTS.CorrelationToRemove, doc[CONSTANTS.CorrelationToRemove]);
                                    columnCollection.UpdateMany(filter, update);
                                    if (!dataList.Contains(columns))
                                    {
                                        dataList.Add(columns);
                                    }
                                }

                                else
                                {
                                    foreach (var featuresChild in data[CONSTANTS.CorrelationToRemove][jpfeatures.Name].Children().ToList())
                                    {
                                        string keyValue = featuresChild.Path[featuresChild.Path.Length - 2].ToString();
                                        JProperty jpfeaturesChild = featuresChild as JProperty;
                                        if (featuresChild.ToString() == columns)
                                        {
                                            List<string> newArray = data[CONSTANTS.CorrelationToRemove][jpfeatures.Name].ToObject<List<string>>();
                                            newArray.Remove(columns);
                                            var jArray = JArray.FromObject(newArray);
                                            data[CONSTANTS.CorrelationToRemove][jpfeatures.Name] = jArray;
                                            BsonDocument doc = BsonDocument.Parse(data.ToString());
                                            string field = string.Format(CONSTANTS.CorrelationToRemove_0, jpfeatures.Name);
                                            var update = Builders<BsonDocument>.Update.Set(field, doc[CONSTANTS.CorrelationToRemove][jpfeatures.Name]);
                                            columnCollection.UpdateMany(filter, update);
                                            if (!dataList.Contains(columns))
                                            {
                                                dataList.Add(columns);
                                            }
                                        }
                                    }
                                }

                                resultReport.Append(CONSTANTS.Updated_in_CorrelationToRemove_for + columns + CONSTANTS.comma);
                            }
                        }

                        // encrypt db values
                        if (DBEncryptionRequired)
                        {
                            var featuredataUpdate = Builders<BsonDocument>.Update.Set(CONSTANTS.FeatureName, _encryptionDecryption.Encrypt(data[CONSTANTS.FeatureName].ToString(Formatting.None)));
                            columnCollection.UpdateMany(filter, featuredataUpdate);
                        }
                        else
                        {
                            var Featuredata = BsonDocument.Parse(data[CONSTANTS.FeatureName].ToString());
                            var featuredataUpdate = Builders<BsonDocument>.Update.Set(CONSTANTS.FeatureName, Featuredata);
                            columnCollection.UpdateMany(filter, featuredataUpdate);
                        }
                    }
                }
            }
            if ((dataList != null) && (dataList.Count > 0))
            {
                string[] strArr = dataList.ToArray();
                var filteredCollection = _database.GetCollection<BsonDocument>(CONSTANTS.DataCleanUPFilteredData);
                var newFieldUpdate = Builders<BsonDocument>.Update.Set(CONSTANTS.removedcols, strArr);
                var result3 = filteredCollection.UpdateOne(filter, newFieldUpdate);
                resultReport.Append(CONSTANTS.Updated_in_DataCleanUP_FilteredData);
            }
            test = resultReport.ToString();
            return test;
        }

        public bool IsModelNameExist(string modelName, string ServiceName = "")
        {
            IMongoCollection<BsonDocument> columnCollection;
            IMongoCollection<BsonDocument> cascadeCollection;
            if (ServiceName == "Anomaly")
            {
                columnCollection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
                cascadeCollection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.SSAICascadedModels);
            }
            else
            {
                columnCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
                cascadeCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAICascadedModels);
            }
            //var columnCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.ModelName, modelName);
            var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.CorrelationId).Include(CONSTANTS.ModelName).Exclude(CONSTANTS.Id);
            var result = columnCollection.Find(filter).Project<BsonDocument>(projection).ToList();

            //var cascadeCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAICascadedModels);
            var filter2 = Builders<BsonDocument>.Filter.Eq(CONSTANTS.ModelName, modelName);
            var projection2 = Builders<BsonDocument>.Projection.Include(CONSTANTS.CascadedId).Include(CONSTANTS.ModelName).Exclude(CONSTANTS.Id);
            var result2 = cascadeCollection.Find(filter2).Project<BsonDocument>(projection2).ToList();
            if (result.Count > 0 || result2.Count > 0)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Gets the result whether the model is trained or not
        /// </summary>
        /// <param name="correlationId">The correlation identifier</param>
        /// <returns>Returns boolean value</returns>
        private bool IsModelTrained(string correlationId)
        {
            IMongoCollection<BsonDocument> columnCollection;
            if (servicename == "Anomaly")
                columnCollection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.SSAIRecommendedTrainedModels);
            else
                columnCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIRecommendedTrainedModels);
            //var columnCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIRecommendedTrainedModels);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var outcome = columnCollection.Find(filter).ToList();
            return outcome.Count > 0 ? true : false;
        }

        /// <summary>
        /// Gets the result whether the model is deployed or not
        /// </summary>
        /// <param name="correlationId">The correlation identifier</param>
        /// <returns>Returns boolean value</returns>
        private bool IsModelDeployed(string correlationId)
        {
            IMongoCollection<BsonDocument> dbCollection;
            if (servicename == "Anomaly")
                dbCollection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
            else
                dbCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
            //var dbCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
            var filterBuilder = Builders<BsonDocument>.Filter;
            var filter = filterBuilder.Eq(CONSTANTS.CorrelationId, correlationId) & filterBuilder.Eq(CONSTANTS.Status, CONSTANTS.Deployed);
            var outcome = dbCollection.Find(filter).ToList();
            return outcome.Count > 0 ? true : false;
        }

        public string GetIngrainRequestCollection(string userId, string uploadfiletype, string mappingflag, string correlationid, string pageInfo,
           string modelname, string clientUID, string deliveryUID, HttpContext httpContext, string category, string uploadtype, string statusFlag)
        {
            bool flag = true;
            string mappingcolumns = string.Empty, DataSourceFilePath = string.Empty;

            IFormCollection formcollection = httpContext.Request.Form;
            var EntitiesNames = formcollection[CONSTANTS.EntitiesName];
            var MetricsNames = formcollection[CONSTANTS.MetricNames];
            if (EntitiesNames.Count > 0)
            {
                foreach (var item in EntitiesNames)
                {
                    if (item != "{}")
                        DataSourceFilePath = item + ",";
                }
            }
            if (MetricsNames.Count() > 0)
            {
                foreach (var item in MetricsNames)
                {
                    if (item != "{}")
                    {
                        DataSourceFilePath += item + ",";
                    }
                }
            }
            var fileCollection = httpContext.Request.Form.Files;
            if (CommonUtility.ValidateFileUploaded(fileCollection))
            {
                throw new FormatException(Resource.IngrainResx.InValidFileName);
            }
            if (fileCollection.Count() > 0)
            {
                for (int i = 0; i < fileCollection.Count; i++)
                {
                    var postedfile = fileCollection[i];
                    DataSourceFilePath += "" + postedfile.FileName + ",";
                }
            }

            if (statusFlag == "" || statusFlag == "undefined")
            {
                var collection = _database.GetCollection<BsonDocument>("SSAI_IngrainRequests");
                var filterProjection = Builders<BsonDocument>.Projection.Exclude("_id");
                var filterbuilder = Builders<BsonDocument>.Filter;
                var filter = filterbuilder.Eq("CorrelationId", correlationid) & filterbuilder.Eq("pageInfo", pageInfo);
                var ingrainrequestCollection = collection.Find(filter).Project<BsonDocument>(filterProjection).FirstOrDefault();                
                bool DBEncryptionRequired = CommonUtility.EncryptDB(correlationid, appSettings);
                var mapping = formcollection[CONSTANTS.mappingPayload];
                if (mapping.Count > 0)
                {
                    foreach (var item in mapping)
                    {
                        if (item != "{}")
                            mappingcolumns = item;
                    }
                }
                if (ingrainrequestCollection.Count() > 0)
                {
                    var paramArgs = JObject.Parse(ingrainrequestCollection["ParamArgs"].ToString());
                    FileUpload fileUpload = new FileUpload();
                    JObject serialize = new JObject();
                    string parentvalue = string.Empty;
                    foreach (var item in paramArgs.Children())
                    {
                        JObject serializeData = new JObject();
                        JProperty jProperty = item as JProperty;
                        if (jProperty != null)
                        {
                            string propertyname = jProperty.Name;
                            switch (propertyname)
                            {
                                case "mapping":
                                    fileUpload.mapping = mappingcolumns;
                                    break;
                                case "mapping_flag":
                                    fileUpload.mapping_flag = mappingflag;
                                    break;
                                case "CorrelationId":
                                    fileUpload.CorrelationId = jProperty.Value.ToString();
                                    break;
                                case "Flag":
                                    fileUpload.Flag = CONSTANTS.Null;
                                    break;
                                case "fileupload":
                                    Filepath filepath = new Filepath();
                                    string filepathvalue = jProperty.Value.ToString();
                                    if (filepathvalue != "")
                                    {
                                        serialize = JObject.Parse(filepathvalue);
                                        foreach (var child in serialize.Children())
                                        {
                                            JProperty jProperty1 = child as JProperty;
                                            if (jProperty1 != null)
                                            {
                                                filepath.fileList = jProperty1.Value.ToString();
                                                fileUpload.fileupload = filepath;
                                            }
                                        }
                                    }
                                    else
                                        fileUpload = null;
                                    break;
                                case "ClientUID":
                                    fileUpload.ClientUID = jProperty.Value.ToString();
                                    break;
                                case "DeliveryConstructUId":
                                    fileUpload.DeliveryConstructUId = jProperty.Value.ToString();
                                    break;
                                case "pad":
                                    fileUpload.pad = jProperty.Value.ToString();
                                    break;
                                case "Parent":
                                    ParentFile parentFile = new ParentFile();
                                    parentvalue = jProperty.Value.ToString();
                                    if (parentvalue != "")
                                    {
                                        serialize = JObject.Parse(parentvalue);
                                        foreach (var child in serialize.Children())
                                        {
                                            JProperty jProperty1 = child as JProperty;
                                            if (jProperty1 != null)
                                            {
                                                if (jProperty1.Name == "Type")
                                                    parentFile.Type = jProperty1.Value.ToString();
                                                else
                                                    parentFile.Name = jProperty1.Value.ToString();
                                                fileUpload.Parent = parentFile;
                                            }
                                        }
                                    }
                                    else
                                        parentFile = null;
                                    break;
                                case "metric":
                                    fileUpload.metric = jProperty.Value.ToString();
                                    break;
                                case "InstaMl":
                                    fileUpload.InstaMl = jProperty.Value.ToString();
                                    break;
                                case "Customdetails":
                                    fileUpload.Customdetails = CONSTANTS.Null;
                                    break;

                            }
                        }
                    }

                    if (DBEncryptionRequired)
                    {
                        if (!string.IsNullOrEmpty(userId))
                        {
                            userId = _encryptionDecryption.Encrypt(userId);
                        }
                    }
                    string Empty = null;
                    var builder = Builders<BsonDocument>.Update;
                    var update = builder.Set("ProcessId", Empty)
                          .Set("Status", Empty)
                          .Set("ModelName", Empty)
                          .Set("RequestStatus", CONSTANTS.New)
                          .Set("RetryCount", 0)
                          .Set("ProblemType", Empty)
                          .Set("Message", Empty)
                          .Set("UniId", Empty)
                          .Set("Progress", Empty)
                          .Set("ParamArgs", fileUpload.ToJson())
                     .Set("ModifiedByUser", userId)
                          .Set("ModifiedOn", DateTime.Now.ToString(CONSTANTS.DateFormat));
                    collection.UpdateMany(filter, update);

                   

                    _ingestedData = new IngestedDataDTO();
                    //while (flag)
                    //{
                    IngrainRequestQueue requestQueue = new IngrainRequestQueue();
                    //requestQueue = IngestedData.GetFileRequestStatus(correlationId, Constant.IngestData);
                    requestQueue = GetFileRequestStatus(correlationid, "IngestData");
                    string result = CheckQueueTable(correlationid);

                    if (requestQueue != null)
                    {
                        PythonCategory pythonCategory = new PythonCategory();
                        PythonInfo pythonInfo = new PythonInfo();
                        // LOGGING.LogManager.Logger.LogProcessInfo(type, Constant.UseCaseTableData, requestQueue.ToString(), new Guid(correlationId));
                        if (requestQueue.Status == "C" & requestQueue.Progress == "100")
                        {
                            //flag = false;
                            _ingestedData.CorrelationId = correlationid;
                            _ingestedData.ModelName = modelname;
                            _ingestedData.CreatedByUser = userId;
                            _ingestedData.ModifiedByUser = userId;
                            _ingestedData.ClientUID = clientUID;
                            _ingestedData.DeliveryConstructUID = deliveryUID;
                            _ingestedData.Category = category;
                            _ingestedData.SourceName = CONSTANTS.multidatasource;
                            if (DataSourceFilePath != "")
                                _ingestedData.DataSource = DataSourceFilePath.Remove(DataSourceFilePath.Length - 1, 1);//postedFile.FileName;
                            UpdateDeployModels(_ingestedData);
                            return CONSTANTS.Success;
                        }
                        else if (requestQueue.Status == "E")
                        {
                            DeleteDeployedModel(correlationid);
                            return CONSTANTS.PhythonError;
                        }
                        else if (requestQueue.Status == "P")
                        {
                            return CONSTANTS.PhythonProgress;
                        }
                        else if (requestQueue.Status == "I")
                        {
                            return CONSTANTS.PhythonInfo;
                        }
                        else
                        {
                            return CONSTANTS.New;
                        }
                    }
                }
                return string.Empty;
            }
            else
            {
                IngrainRequestQueue requestQueue = new IngrainRequestQueue();
                requestQueue = GetFileRequestStatus(correlationid, "IngestData");
                _ingestedData = new IngestedDataDTO();
                if (requestQueue != null)
                {
                    PythonCategory pythonCategory = new PythonCategory();
                    PythonInfo pythonInfo = new PythonInfo();
                    // LOGGING.LogManager.Logger.LogProcessInfo(type, Constant.UseCaseTableData, requestQueue.ToString(), new Guid(correlationId));
                    if (requestQueue.Status == "C" & requestQueue.Progress == "100")
                    {
                        _ingestedData.CorrelationId = correlationid;
                        _ingestedData.ModelName = modelname;
                        _ingestedData.CreatedByUser = userId;
                        _ingestedData.ModifiedByUser = userId;
                        _ingestedData.ClientUID = clientUID;
                        _ingestedData.DeliveryConstructUID = deliveryUID;
                        _ingestedData.Category = category;
                        _ingestedData.SourceName = CONSTANTS.multidatasource;
                        if (DataSourceFilePath != "")
                            _ingestedData.DataSource = DataSourceFilePath.Remove(DataSourceFilePath.Length - 1, 1);//postedFile.FileName;
                        UpdateDeployModels(_ingestedData);
                        return CONSTANTS.Success;
                    }
                    else if (requestQueue.Status == "E")
                    {
                        DeleteDeployedModel(correlationid);
                        return CONSTANTS.PhythonError;
                    }
                    else if (requestQueue.Status == "P")
                    {
                        return CONSTANTS.PhythonProgress;
                    }
                    else if (requestQueue.Status == "I")
                    {
                        return CONSTANTS.PhythonInfo;
                    }
                    else
                    {
                        return CONSTANTS.New;
                    }
                }
                return string.Empty;
            }
            //return CONSTANTS.Success;

        }
        public string UploadFiles(string CorrelationId, string ModelName, string userId, string clientUID, string deliveryUID, string ParentFileName, string MappingFlag, string Source, string Category, Type type, string E2EUID, HttpContext httpContext, string ClusterFlag, string EntityStartDate, string EntityEndDate, bool DBEncryption, string oldCorrelationID, string Language, bool IsModelTemplateDataSource, string CorrelationId_status, out string requestQueueStatus, string usecaseId, string ServiceName = "")
        {
            servicename = ServiceName;
            bool encryptDB = false;

            if (appSettings.Value.isForAllData == true)
            {
                if (appSettings.Value.DBEncryption == true)
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
                if (appSettings.Value.DBEncryption == true && DBEncryption == true)
                {

                    encryptDB = true;
                }
                else
                {
                    encryptDB = false;
                }

            }


            string MappingColumns = string.Empty;
            string filePath = string.Empty;
            // var postedFile;

            bool flag = true;
            int counter = 0;
            string ParentFileNamePath = string.Empty, FilePath = string.Empty, postedFileName = string.Empty, DataSourceFilePath = string.Empty, FileName = string.Empty, SaveFileName = string.Empty;
            //System.Web.HttpFileCollection fileCollection = System.Web.HttpContext.Current.Request.Files;
            requestQueueStatus = string.Empty;
            // var fileCollection = HttpContext.Request.Form.Files;
            if (CorrelationId_status == "" || CorrelationId_status == "undefined")
            {
                var fileCollection = httpContext.Request.Form.Files;
                string correlationId = CorrelationId;

                string Entities = string.Empty, Metrices = string.Empty, InstaML = string.Empty, Entity_Names = string.Empty, Metric_Names = string.Empty, Customdata = string.Empty, CustomSourceItems = string.Empty;
                if (!string.IsNullOrEmpty(ModelName) && !string.IsNullOrEmpty(deliveryUID) && !string.IsNullOrEmpty(clientUID))
                {

                    filePath = appSettings.Value.UploadFilePath;
                    Directory.CreateDirectory(Path.Combine(filePath, appSettings.Value.SavedModels));
                    filePath = System.IO.Path.Combine(filePath, appSettings.Value.AppData);
                    System.IO.Directory.CreateDirectory(filePath);

                    // NameValueCollection collection = HttpContext.Current.Request.Form;
                    IFormCollection collection = httpContext.Request.Form;
                    var entityitems = collection[CONSTANTS.pad];
                    var metricitems = collection[CONSTANTS.metrics];
                    var InstaMl = collection[CONSTANTS.InstaMl];
                    var EntitiesNames = collection[CONSTANTS.EntitiesName];
                    var MetricsNames = collection[CONSTANTS.MetricNames];
                    var Customdetails = collection["Custom"];
                    var dataSetUId = collection["DataSetUId"];
                    var CustomDataSourceDetails= collection["CustomDataPull"];

                    if (dataSetUId == "undefined" || dataSetUId == "null")
                        dataSetUId = string.Empty;

                    if (Customdetails.Count() > 0)
                    {
                        foreach (var item in Customdetails)
                        {
                            if (item.Trim() != "{}")
                            {
                                Customdata += item;
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
                            }
                            else
                                Metrices = CONSTANTS.Null;
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

                    _ingestedData = new IngestedDataDTO();
                    _ingestedData.CorrelationId = correlationId;
                    if (fileCollection.Count != 0)
                    {
                        //LOGGING.LogManager.Logger.LogProcessInfo(typeof(IngestedDataService), nameof(UploadFiles), "fileCollection START", new Guid(correlationId), "", "", "",""); //TODO: Remove log
                        for (int i = 0; i < fileCollection.Count; i++)
                        {
                            var postedFile = fileCollection[i];
                            if (postedFile.Length <= 0)
                                return CONSTANTS.FileEmpty;
                            if (File.Exists(filePath + Path.GetFileName(correlationId + "_" + postedFile.FileName)))
                            {
                                counter++;
                                FileName = postedFile.FileName;
                                string[] strfileName = FileName.Split('.');
                                FileName = strfileName[0] + "_" + counter;
                                SaveFileName = FileName + "." + strfileName[1];
                                _encryptionDecryption.EncryptFile(postedFile, filePath + Path.GetFileName(correlationId + "_" + SaveFileName));
                            }
                            else
                            {
                                SaveFileName = postedFile.FileName;
                                _encryptionDecryption.EncryptFile(postedFile, filePath + Path.GetFileName(correlationId + "_" + SaveFileName));
                            }
                            if (DataSourceFilePath != "")
                            {
                                DataSourceFilePath += "" + postedFile.FileName + ",";
                            }
                            else
                                DataSourceFilePath = DataSourceFilePath + "" + postedFile.FileName + ",";
                            if (ParentFileName != CONSTANTS.undefined)
                            {
                                if (appSettings.Value.IsAESKeyVault && appSettings.Value.Environment == CONSTANTS.PADEnvironment && appSettings.Value.EncryptUploadedFiles)
                                    FilePath = FilePath + "" + filePath + correlationId + "_" + SaveFileName +".enc" + @"""" + @",""";
                                else
                                    FilePath = FilePath + "" + filePath + correlationId + "_" + SaveFileName + @"""" + @",""";
                                if (postedFile.FileName == ParentFileName)
                                {
                                    ParentFileNamePath = filePath + correlationId + "_" + SaveFileName;
                                }
                            }
                            else
                            {
                                if (appSettings.Value.IsAESKeyVault &&  appSettings.Value.Environment == CONSTANTS.PADEnvironment && appSettings.Value.EncryptUploadedFiles)
                                    FilePath = FilePath + "" + filePath + correlationId + "_" + SaveFileName + ".enc" + @"""" + @",""";
                                else
                                    FilePath = FilePath + "" + filePath + correlationId + "_" + SaveFileName + @"""" + @",""";
                                ParentFileNamePath = ParentFileName;
                                //LOGGING.LogManager.Logger.LogProcessInfo(typeof(IngestedDataService), nameof(UploadFiles), "FILECOLLECTION FILEPATH: " + FilePath + ", PARENTFILENAME: " + ParentFileNamePath + ", CORRELATIONID: " + correlationId); //TODO: Remove log                                
                            }
                            if (fileCollection.Count > 0)
                            {
                                postedFileName = "[" + @"""" + FilePath.Remove(FilePath.Length - 2, 2) + "]";
                                //LOGGING.LogManager.Logger.LogProcessInfo(typeof(IngestedDataService), nameof(UploadFiles), "FILECOLLECTION POSTEDFILENAME: " + postedFileName + ", CORRELATIONID: " + correlationId); //TODO: Remove log
                            }
                            //LOGGING.LogManager.Logger.LogProcessInfo(typeof(IngestedDataService), nameof(UploadFiles), "fileCollection END"); //TODO: Remove log
                        }
                    }
                    flag = true;
                    ingrainRequest = new IngrainRequestQueue
                    {
                        _id = Guid.NewGuid().ToString(),
                        CorrelationId = correlationId,
                        DataSetUId = dataSetUId,
                        RequestId = Guid.NewGuid().ToString(),
                        ProcessId = null,
                        Status = null,
                        ModelName = ModelName,
                        RequestStatus = CONSTANTS.New,
                        RetryCount = 0,
                        ProblemType = null,
                        Message = null,
                        UniId = null,
                        Progress = null,
                        pageInfo = CONSTANTS.IngestData,
                        ParamArgs = null,
                        Function = CONSTANTS.FileUpload,
                        CreatedByUser = userId,
                        CreatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        ModifiedByUser = userId,
                        ModifiedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        LastProcessedOn = null,

                    };

                    AgileCase agileCase = new AgileCase();
                    if (oldCorrelationID != "undefined")
                    {
                        _agilefilepath = new AgileFilepath();
                        agileCase.oldcorrelationid = oldCorrelationID;
                        if (postedFileName != "")
                            _agilefilepath.fileList = postedFileName;
                        else
                            _agilefilepath.fileList = "null";
                        _agilefilepath.AgileUsecase = agileCase;

                    }
                    else
                    {
                        _filepath = new Filepath();
                        if (postedFileName != "")
                            _filepath.fileList = postedFileName;
                        else
                            _filepath.fileList = "null";
                    }

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

                    string Flag = CONSTANTS.Null;
                    if (servicename == "Anomaly")
                    {
                        if (!(Entities == CONSTANTS.Null && Customdetails == CONSTANTS.Null))
                            Flag = "Incremental";
                    }
                        
                    if (ClusterFlag == "True")
                    {
                        IMongoCollection<AppIntegration> AppIntegCollection;
                        if (servicename == "Anomaly")
                            AppIntegCollection = _databaseAD.GetCollection<AppIntegration>(CONSTANTS.AppIntegration);
                        else
                            AppIntegCollection = _database.GetCollection<AppIntegration>(CONSTANTS.AppIntegration);
                        var filterBuilder = Builders<AppIntegration>.Filter;
                        var AppFilter = filterBuilder.Eq("ApplicationName", "Ingrain");

                        var Projection = Builders<AppIntegration>.Projection.Include("ApplicationID").Exclude("_id");
                        var AppData = AppIntegCollection.Find(AppFilter).Project<AppIntegration>(Projection).FirstOrDefault();

                        var fileParams = JsonConvert.DeserializeObject<InputParams>(Customdata);
                        InputParameter param = new InputParameter
                        {
                            correlationid = correlationId,
                            FromDate = EntityStartDate,
                            ToDate = EntityEndDate,
                            noOfRecord = 10000.0
                        };

                        CustomFlag customFlag = new CustomFlag
                        {
                            FlagName = "AgileUsecase"
                        };

                        Customdetails AppPayload = new Customdetails
                        {
                            CustomFlags = customFlag,
                            AppId = AppData.ApplicationID,
                            HttpMethod = "POST",
                            AppUrl = "Null",
                            InputParameters = param,
                            AICustom = "False"
                        };

                        CustomUploadEntity Customfile = new CustomUploadEntity
                        {
                            CorrelationId = correlationId,
                            ClientUID = clientUID,
                            DeliveryConstructUId = deliveryUID,
                            Parent = parentFile,
                            Flag = Flag,//CONSTANTS.Null,
                            mapping = CONSTANTS.Null,
                            mapping_flag = CONSTANTS.False,
                            pad = CONSTANTS.Null,
                            metric = CONSTANTS.Null,
                            InstaMl = CONSTANTS.Null,
                            fileupload = _filepath,
                            Customdetails = AppPayload,

                        };
                        DataSourceFilePath = DataSourceFilePath + "Phoenix CDM" + ",";

                        _ingestedData.Category = Category;

                        ingrainRequest.ParamArgs = Customfile.ToJson();
                    }
                    else
                    {
                        if (Customdata != CONSTANTS.Null && Customdata != string.Empty)
                        {
                            IMongoCollection<AppIntegration> AppIntegCollection;
                            if (servicename == "Anomaly")
                                AppIntegCollection = _databaseAD.GetCollection<AppIntegration>(CONSTANTS.AppIntegration);
                            else
                                AppIntegCollection = _database.GetCollection<AppIntegration>(CONSTANTS.AppIntegration);
                            var filterBuilder = Builders<AppIntegration>.Filter;                            
                            var AppFilter = filterBuilder.Eq("ApplicationName", appSettings.Value.ApplicationName);

                            var Projection = Builders<AppIntegration>.Projection.Include("ApplicationID").Exclude("_id");
                            var AppData = AppIntegCollection.Find(AppFilter).Project<AppIntegration>(Projection).FirstOrDefault();

                            var fileParams = JsonConvert.DeserializeObject<InputParams>(Customdata);
                            InputParams param = new InputParams
                            {
                                ClientID = clientUID,
                                E2EUID = !string.IsNullOrEmpty(E2EUID) ? E2EUID : CONSTANTS.Null,
                                DeliveryConstructID = deliveryUID,
                                Environment = fileParams.Environment,
                                RequestType = fileParams.RequestType,
                                ServiceType = fileParams.ServiceType,
                                StartDate = fileParams.StartDate,
                                EndDate = fileParams.EndDate
                            };
                            CustomPayloads AppPayload = new CustomPayloads
                            {
                                AppId = AppData.ApplicationID,
                                HttpMethod = CONSTANTS.POST,
                                AppUrl = appSettings.Value.GetVdsPAMDataURL,
                                InputParameters = param,
                                AICustom = "False"
                            };
                            CustomUploadFile Customfile = new CustomUploadFile
                            {
                                CorrelationId = correlationId,
                                ClientUID = clientUID,
                                DeliveryConstructUId = deliveryUID,
                                Parent = parentFile,
                                Flag = Flag,//CONSTANTS.Null,
                                mapping = CONSTANTS.Null,
                                mapping_flag = CONSTANTS.False,
                                pad = CONSTANTS.Null,
                                metric = CONSTANTS.Null,
                                InstaMl = CONSTANTS.Null,
                                fileupload = _filepath,
                                Customdetails = AppPayload,

                            };
                            DataSourceFilePath = DataSourceFilePath + param.ServiceType + ",";
                            //_ingestedData.DataSource = param.ServiceType;
                            _ingestedData.Category = param.RequestType;

                            ingrainRequest.ParamArgs = Customfile.ToJson();
                        }
                        else if (CustomSourceItems != CONSTANTS.Null && CustomSourceItems != string.Empty)
                        {
                            if (Source.ToUpper() == CONSTANTS.CustomDbQuery.ToUpper())
                            {
                                var DateColumn = Convert.ToString(collection["DateColumn"]);
                                _filepath = new Filepath();
                                _filepath.fileList = "null";
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
                                _ingestedData.DataSource = Source;
                                ingrainRequest.ParamArgs = CustomQueryData.ToJson();
                                _ingestedData.Category = Category;
                            }
                            else if (Source.ToUpper() == CONSTANTS.CustomDataApi.ToUpper())
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
                                        grant_type =  appSettings.Value.Grant_Type,
                                        client_secret =  appSettings.Value.clientSecret,
                                        client_id = appSettings.Value.clientId,
                                        resource =  appSettings.Value.resourceId
                                    };

                                    string TokenUrl = appSettings.Value.token_Url;
                                    string token = _customDataService.CustomUrlToken("Ingrain", oAuthCredentials, TokenUrl);
                                    if (!String.IsNullOrEmpty(token))
                                    {
                                        fileParams.Data.Authentication.Token = token;
                                    }
                                    else
                                    {
                                        return CONSTANTS.IngrainTokenBlank;
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

                                _ingestedData.DataSource = Source;
                                _ingestedData.Category = Category;
                                ingrainRequest.ParamArgs = CustomAPIData.ToJson();
                            }
                        }
                        else
                        {

                            if (oldCorrelationID != "undefined")
                            {
                                agileFileUpload = new AgileFileUpload
                                {
                                    CorrelationId = correlationId,
                                    ClientUID = clientUID,
                                    DeliveryConstructUId = deliveryUID,
                                    Parent = parentFile,
                                    Flag = Flag,//CONSTANTS.Null,
                                    mapping = MappingColumns,
                                    mapping_flag = MappingFlag,
                                    pad = Entities,
                                    metric = Metrices,
                                    InstaMl = InstaML,
                                    fileupload = _agilefilepath,
                                    Customdetails = CONSTANTS.Null

                                };
                                ingrainRequest.ParamArgs = agileFileUpload.ToJson();
                            }
                            else
                            {
                                fileUpload = new FileUpload
                                {
                                    CorrelationId = correlationId,
                                    ClientUID = clientUID,
                                    DeliveryConstructUId = deliveryUID,
                                    Parent = parentFile,
                                    Flag = Flag,//CONSTANTS.Null,
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
                        }
                    }

                    if (string.IsNullOrEmpty(_ingestedData.SourceName))
                    {
                        if (Entities != CONSTANTS.Null)
                        {
                            _ingestedData.SourceName = "pad";
                        }
                        else if (Metrices != CONSTANTS.Null)
                        {
                            _ingestedData.SourceName = "metric";
                        }
                        else if (!string.IsNullOrEmpty(postedFileName))
                        {
                            _ingestedData.SourceName = "file";
                        }

                        if (Source == "Custom")
                        {
                            _ingestedData.SourceName = "Custom";
                        }
                        if (Source.ToUpper() == CONSTANTS.CustomDbQuery.ToUpper())
                        {
                            _ingestedData.SourceName = CONSTANTS.CustomDbQuery;
                        }
                        if (Source.ToUpper() == CONSTANTS.CustomDataApi.ToUpper())
                        {
                            _ingestedData.SourceName = Source;
                        }
                    }

                    //storing dbencryption flag in ssai_deployedmodels----------------------
                    _ingestedData.ModelName = ModelName;
                    _ingestedData.CreatedByUser = userId;
                    _ingestedData.ModifiedByUser = userId;
                    _ingestedData.ClientUID = clientUID;
                    _ingestedData.DeliveryConstructUID = deliveryUID;
                    _ingestedData.Language = Language;
                    if (!string.IsNullOrEmpty(dataSetUId))
                        _ingestedData.SourceName = "DataSet";

                    if (Customdata == CONSTANTS.Null || Customdata == string.Empty)
                    {
                        _ingestedData.Category = Category;
                    }
                    if (DataSourceFilePath != "")
                        _ingestedData.DataSource = DataSourceFilePath.Remove(DataSourceFilePath.Length - 1, 1);//postedFile.FileName;


                    if (Source.ToUpper() == CONSTANTS.CustomDbQuery.ToUpper())
                    {
                        //var CustomQueryParams = JsonConvert.DeserializeObject<CustomDataInputParams>(CustomSourceItems);

                        CustomDataSourceModel CustomDataSource = new CustomDataSourceModel
                        {
                            _id = Guid.NewGuid().ToString(),
                            CorrelationId = correlationId,
                            CustomDataPullType = CONSTANTS.CustomDbQuery,
                            CustomSourceDetails = Convert.ToString(CustomSourceItems),
                            CreatedByUser = userId
                        };
                        _customDataService.InsertCustomDataSource(CustomDataSource, CONSTANTS.SSAICustomDataSource);
                    }
                    else if (Source.ToUpper() == CONSTANTS.CustomDataApi.ToUpper()) {
                        CustomInputData CustomfileParams = JsonConvert.DeserializeObject<CustomInputData>(CustomSourceItems);
                        if (CustomfileParams != null)
                        {
                            CustomfileParams.DbEncryption = encryptDB;
                            if (CustomfileParams.Data != null)
                            {
                                CustomfileParams.Data.Type = "API";
                            }
                        }
                       
                        CustomDataSourceModel CustomDataSource = new CustomDataSourceModel
                        {
                            _id = Guid.NewGuid().ToString(),
                            CorrelationId = correlationId,
                            CustomDataPullType = CONSTANTS.CustomDataApi,
                            CustomSourceDetails = JsonConvert.SerializeObject(CustomfileParams),
                            CreatedByUser = userId
                        };
                        _customDataService.InsertCustomDataSource(CustomDataSource, CONSTANTS.SSAICustomDataSource);
                    }

                    InsertDeployModels(_ingestedData, encryptDB, IsModelTemplateDataSource, dataSetUId, usecaseId);
                    //--------------------------------------------------------------

                    InsertRequests(ingrainRequest, servicename);
                    Thread.Sleep(1000);

                    //while (flag)
                    //{
                    IngrainRequestQueue requestQueue = new IngrainRequestQueue();
                    //requestQueue = IngestedData.GetFileRequestStatus(correlationId, Constant.IngestData);
                    requestQueue = GetFileRequestStatus(correlationId, "IngestData");
                    // string result = CheckQueueTable(correlationId);
                    //requestQueueStatus = string.Empty;
                    if (requestQueue != null)
                    {
                        PythonCategory pythonCategory = new PythonCategory();
                        PythonInfo pythonInfo = new PythonInfo();
                        requestQueueStatus = requestQueue.Status;
                         //LOGGING.LogManager.Logger.LogProcessInfo(type, Constant.UseCaseTableData, requestQueue.ToString(), string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId),"","","","" );
                        if (requestQueue.Status == "C" & requestQueue.Progress == "100")
                        {
                            return CONSTANTS.Success;

                        }
                        else if (requestQueue.Status == "M" & requestQueue.Progress == "100")
                        {
                            return CONSTANTS.Success;
                        }
                        else if (requestQueue.Status == "E")
                        {
                            DeleteDeployedModel(correlationId);
                            return CONSTANTS.PhythonError;

                        }
                        else if (requestQueue.Status == "I")
                        {
                            DeleteDeployedModel(correlationId);
                            return CONSTANTS.PhythonInfo;
                        }
                        else if (requestQueue.Status == "P")
                        {
                            return CONSTANTS.PhythonProgress;
                        }
                        else
                        {
                            return CONSTANTS.New;
                        }
                    }
                    else
                    {
                        //flag = true;
                        Thread.Sleep(2000);
                    }
                }
                // }

                return string.Empty;
            }
            else
            {
                IngrainRequestQueue requestQueue = new IngrainRequestQueue();
                requestQueue = GetFileRequestStatus(CorrelationId_status, "IngestData");
                if (requestQueue != null)
                {
                    PythonCategory pythonCategory = new PythonCategory();
                    PythonInfo pythonInfo = new PythonInfo();
                    requestQueueStatus = requestQueue.Status;
                    if (requestQueue.Status == "C" & requestQueue.Progress == "100")
                    {
                        if (usecaseId != null && usecaseId != CONSTANTS.undefined)
                        {
                            if (usecaseId == CONSTANTS.FMUseCaseId)
                            {
                                //Call the FM scenario. Initiate request for FM scenario at windows service.
                                //bool isFMStatus = CheckMatchedcolumns(usecaseId, CorrelationId_status);                                
                                    ProblemTypeDetails2 problemtype = FMUpdateModelName(CorrelationId_status, ModelName, usecaseId);
                                    IngrainRequestQueue ingrainRequest = new IngrainRequestQueue
                                    {
                                        _id = Guid.NewGuid().ToString(),
                                        CorrelationId = problemtype.FMModel1CorId,
                                        RequestId = Guid.NewGuid().ToString(),
                                        ProcessId = CONSTANTS.Null,
                                        ModelName = ModelName,
                                        Status = CONSTANTS.Null,
                                        RequestStatus = CONSTANTS.New,
                                        Message = CONSTANTS.Null,
                                        RetryCount = 0,
                                        AppID = problemtype.AppId,
                                        ProblemType = problemtype.ModelType,
                                        UniId = CONSTANTS.Null,
                                        InstaID = CONSTANTS.Null,
                                        Progress = CONSTANTS.Null,
                                        pageInfo = CONSTANTS.FMTransform,
                                        ParamArgs = CONSTANTS.Null,
                                        TemplateUseCaseID = usecaseId,
                                        FMCorrelationId = CorrelationId_status,
                                        Function = CONSTANTS.FMTransform,
                                        CreatedByUser = userId,
                                        CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                                        ModifiedByUser = CONSTANTS.Null,
                                        ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                                        LastProcessedOn = CONSTANTS.Null,
                                        ClientId = clientUID,
                                        DeliveryconstructId = deliveryUID,
                                        UseCaseID = CONSTANTS.Null,
                                        EstimatedRunTime = CONSTANTS.Null
                                    };
                                    if (problemtype != null)
                                    {
                                        if (problemtype.LinkedApps != null)
                                        {
                                            if (problemtype.LinkedApps.Length > 0)
                                            {
                                                ingrainRequest.ApplicationName = problemtype.LinkedApps[0];
                                            }
                                        }
                                    }
                                    InsertRequests(ingrainRequest, servicename);
                               
                            }
                        }
                        return CONSTANTS.Success;
                    }
                    else if (requestQueue.Status == "M" & requestQueue.Progress == "100")
                    {
                        return CONSTANTS.Success;
                        // flag = false;
                    }
                    else if (requestQueue.Status == "E")
                    {
                        DeleteDeployedModel(CorrelationId_status);
                        return CONSTANTS.PhythonError;

                    }
                    else if (requestQueue.Status == "I")
                    {
                        DeleteDeployedModel(CorrelationId_status);
                        return CONSTANTS.PhythonInfo;
                    }
                    else if (requestQueue.Status == "P")
                    {
                        return CONSTANTS.PhythonProgress;
                    }
                    else
                    {
                        return CONSTANTS.New;
                    }
                }

                return string.Empty;
            }

        }
        private bool CheckMatchedcolumns(string usecaseId, string correlationId)
        {
            bool isMatched = true;
            List<string> corids = new List<string>() { usecaseId, correlationId };
            try
            {
                //For Current Model
                List<IngestDataColumn> ingestDatas1 = new List<IngestDataColumn>();
                IMongoCollection<IngestDataColumn> collection;
                IMongoCollection<IngestDataColumn> collection2;
                if (servicename == "Anomaly")
                {
                    collection = _databaseAD.GetCollection<IngestDataColumn>(CONSTANTS.PSIngestedData);
                    collection2 = _databaseAD.GetCollection<IngestDataColumn>(CONSTANTS.PSIngestedData);
                }
                else
                {
                    collection = _database.GetCollection<IngestDataColumn>(CONSTANTS.PSIngestedData);
                    collection2 = _database.GetCollection<IngestDataColumn>(CONSTANTS.PSIngestedData);
                }
                //var collection = _database.GetCollection<IngestDataColumn>(CONSTANTS.PSIngestedData);
                var filter = Builders<IngestDataColumn>.Filter.Eq(CONSTANTS.CorrelationId, correlationId.Trim());
                var projection = Builders<IngestDataColumn>.Projection.Include(CONSTANTS.ColumnsList).Include(CONSTANTS.CorrelationId).Exclude(CONSTANTS.Id);
                ingestDatas1 = collection.Find(filter).Project<IngestDataColumn>(projection).ToList();

                ///For Template
                List<IngestDataColumn> ingestDatas2 = new List<IngestDataColumn>();
                //var collection2 = _database.GetCollection<IngestDataColumn>(CONSTANTS.PSIngestedData);
                var filter2 = Builders<IngestDataColumn>.Filter.Eq(CONSTANTS.CorrelationId, usecaseId.Trim());
                var projection2 = Builders<IngestDataColumn>.Projection.Include(CONSTANTS.ColumnsList).Include(CONSTANTS.CorrelationId).Exclude(CONSTANTS.Id);
                ingestDatas2 = collection2.Find(filter2).Project<IngestDataColumn>(projection2).ToList();
                if (ingestDatas1.Count > 0)
                {
                    if (ingestDatas1[0].ColumnsList.Count() == ingestDatas2[0].ColumnsList.Count())
                    {
                        bool equal = ingestDatas1[0].ColumnsList.All(elem => ingestDatas2[0].ColumnsList.Contains(elem));
                        if (!equal)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(IngestedDataService), nameof(CheckMatchedcolumns) + "--isMatched3--" + "false", "END", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                        return false;
                    }
                }
                else
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(IngestedDataService), nameof(CheckMatchedcolumns) + "--isMatched4--" + "false", "END", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                    return false;
                }

            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(IngestedDataService), nameof(CheckMatchedcolumns), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
            return isMatched;
        }

        private ProblemTypeDetails2 FMUpdateModelName(string correlationId, string modelname, string usecaseId)
        {
            IMongoCollection<DeployModelsDto> collection;
            if (servicename == "Anomaly")
                collection = _databaseAD.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
            else
                collection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);

            ProblemTypeDetails2 problemTypeDetails = new ProblemTypeDetails2();
            //var collection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
            var filter = Builders<DeployModelsDto>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var projection = Builders<DeployModelsDto>.Projection.Exclude(CONSTANTS.Id);
            var result = collection.Find(filter).Project<DeployModelsDto>(projection).ToList();
            string corId = Guid.NewGuid().ToString();
            if (result.Count > 0)
            {
                string modelName = modelname + "_1";
                string[] arr = new string[] { };
                DeployModelsDto deployModel = new DeployModelsDto
                {
                    _id = Guid.NewGuid().ToString(),
                    CorrelationId = corId,
                    InstaId = null,
                    ModelName = modelName,
                    Status = CONSTANTS.InProgress,
                    ClientUId = result[0].ClientUId,
                    DeliveryConstructUID = result[0].DeliveryConstructUID,
                    DataSource = result[0].DataSource,
                    DeployedDate = null,
                    LinkedApps = arr,
                    ModelVersion = null,
                    ModelType = null,
                    SourceName = result[0].SourceName,
                    VDSLink = null,
                    InputSample = null,
                    IsPrivate = true,
                    IsModelTemplate = false,
                    DBEncryptionRequired = result[0].DBEncryptionRequired,
                    TrainedModelId = null,
                    Frequency = null,
                    Category = result[0].Category,
                    CreatedByUser = result[0].CreatedByUser,
                    CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                    ModifiedByUser = result[0].ModifiedByUser,
                    ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                    Language = result[0].Language,
                    FMCorrelationId = correlationId,
                    HideFMModel = true,
                    IsModelTemplateDataSource = result[0].IsModelTemplateDataSource
                };

                var jsonColumns = Newtonsoft.Json.JsonConvert.SerializeObject(deployModel);
                var insertBsonColumns = BsonSerializer.Deserialize<BsonDocument>(jsonColumns);
                IMongoCollection<BsonDocument> deplocollection;
                if (servicename == "Anomaly")
                    deplocollection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
                else
                    deplocollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
                //var deplocollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
                deplocollection.InsertOne(insertBsonColumns);
            }
            IMongoCollection<ProblemTypeDetails2> collection2;
            IMongoCollection<BsonDocument> psingestData;
            IMongoCollection<BsonDocument> psuseCase;
            if (servicename == "Anomaly")
            {
                collection2 = _databaseAD.GetCollection<ProblemTypeDetails2>(CONSTANTS.SSAIDeployedModels);
                psingestData = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.PSIngestedData);
                psuseCase = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.PSUseCaseDefinition);
            }
            else
            {
                collection2 = _database.GetCollection<ProblemTypeDetails2>(CONSTANTS.SSAIDeployedModels);
                psingestData = _database.GetCollection<BsonDocument>(CONSTANTS.PSIngestedData);
                psuseCase = _database.GetCollection<BsonDocument>(CONSTANTS.PSUseCaseDefinition);
            }
            //var collection2 = _database.GetCollection<ProblemTypeDetails2>(CONSTANTS.SSAIDeployedModels);
            var filter22 = Builders<ProblemTypeDetails2>.Filter.Eq(CONSTANTS.CorrelationId, usecaseId);
            var projection2 = Builders<ProblemTypeDetails2>.Projection.Include(CONSTANTS.ModelType).Include(CONSTANTS.LinkedApps).Include("AppId").Exclude(CONSTANTS.Id);
            problemTypeDetails = collection2.Find(filter22).Project<ProblemTypeDetails2>(projection2).FirstOrDefault();
            problemTypeDetails.FMModel1CorId = corId;

            //update psingestedData and ps_usecasedefinition
            //var psingestData = _database.GetCollection<BsonDocument>(CONSTANTS.PSIngestedData);
            //var psuseCase = _database.GetCollection<BsonDocument>(CONSTANTS.PSUseCaseDefinition);
            var filter3 = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var update = Builders<BsonDocument>.Update.Set(CONSTANTS.CorrelationId, corId);
            psingestData.UpdateOne(filter3, update);
            psuseCase.UpdateOne(filter3, update);

            //updating fmmodel at main model
            var update2 = Builders<DeployModelsDto>.Update.Set(CONSTANTS.IsFMModel, true).Set("FMCorrelationId", corId);
            collection.UpdateOne(filter, update2);
            return problemTypeDetails;
        }
        public string DataSourceUploadFiles(string correlationId, string ModelName, string userId, string clientUID, string deliveryUID, string ParentFileName, string MappingFlag, string Source, string Category, Type type, string E2EUID, HttpContext httpContext, string ClusterFlag, string EntityStartDate, string EntityEndDate, bool DBEncryption, string oldCorrelationID, string Language, string CorrelationId_status, string usecaseId, string ServiceName = "")
        {

            servicename = ServiceName;
            bool encryptDB = DBEncryption;
            string filePath = string.Empty;
            string MappingColumns = string.Empty;
            int counter = 0;
            string ParentFileNamePath = string.Empty, FilePath = string.Empty, postedFileName = string.Empty, DataSourceFilePath = string.Empty, FileName = string.Empty, SaveFileName = string.Empty;
            filePath = appSettings.Value.UploadFilePath;
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(IngestedDataService), nameof(DataSourceUploadFiles) + "SEQUENCE 1 --CorrelationId--" + correlationId + "--CorrelationId_status--" + CorrelationId_status, "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            if (CorrelationId_status == "" || CorrelationId_status == "undefined")
            {
                Directory.CreateDirectory(Path.Combine(filePath, appSettings.Value.SavedModels));

                filePath = System.IO.Path.Combine(filePath, appSettings.Value.AppData);
                System.IO.Directory.CreateDirectory(filePath);

                string Entities = string.Empty, Metrices = string.Empty, InstaML = string.Empty, Entity_Names = string.Empty, Metric_Names = string.Empty, Customdata = string.Empty;
                string CustomSourceItems = string.Empty;
                var fileCollection = httpContext.Request.Form.Files;

                if (string.IsNullOrEmpty(correlationId))
                {
                    return CONSTANTS.CorrelatioUIDEmpty;
                }
                else
                {
                    IFormCollection collection = httpContext.Request.Form;
                    var entityitems = collection[CONSTANTS.pad];
                    var metricitems = collection[CONSTANTS.metrics];
                    var InstaMl = collection[CONSTANTS.InstaMl];
                    var EntitiesNames = collection[CONSTANTS.EntitiesName];
                    var MetricsNames = collection[CONSTANTS.MetricNames];
                    var Customdetails = collection["Custom"];
                    var dataSetUId = collection["DataSetUId"];
                    var CustomDataSourceDetails = collection["CustomDataPull"];

                    if (dataSetUId == "undefined" || dataSetUId == "null")
                        dataSetUId = string.Empty;

                    if (Customdetails.Count() > 0)
                    {
                        foreach (var item in Customdetails)
                        {
                            if (item != "{}")
                            {
                                Customdata += item;
                            }
                            else
                                Customdata = CONSTANTS.Null;
                        }
                    }

                    if (entityitems.Count() > 0)
                    {
                        foreach (var item in entityitems)
                        {
                            if (item != "{}")
                            {
                                Entities += item;
                            }
                            else
                                Entities = CONSTANTS.Null;
                        }
                    }

                    if (metricitems.Count() > 0)
                    {
                        foreach (var item in metricitems)
                        {
                            if (item != "{}")
                            {
                                Metrices += item;
                            }
                            else
                                Metrices = CONSTANTS.Null;
                        }
                    }

                    if (InstaMl.Count() > 0)
                    {
                        foreach (var item in InstaMl)
                        {
                            if (item != "{}")
                            {
                                InstaML += item;
                            }
                            else
                                InstaML = CONSTANTS.Null;
                        }
                    }
                    if (EntitiesNames.Count > 0)
                    {
                        foreach (var item in EntitiesNames)
                        {
                            if (item != "{}")
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
                            if (item != "{}")
                            {
                                Metric_Names += item;
                                DataSourceFilePath += Metric_Names + ",";
                            }
                        }
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
                    _ingestedData = new IngestedDataDTO();
                    _ingestedData.CorrelationId = correlationId;

                    //if (fileCollection.Count <= 0)
                    //    return CONSTANTS.FileNotExist;

                    if (fileCollection.Count != 0)
                    {
                        for (int i = 0; i < fileCollection.Count; i++)
                        {
                            var postedFile = fileCollection[i];
                            if (postedFile.Length <= 0)
                                return CONSTANTS.FileEmpty;
                            //return Content(HttpStatusCode.NoContent, Resource.SSAIResx.FileEmpty);

                            if (File.Exists(filePath + Path.GetFileName(correlationId + "_" + postedFile.FileName)))
                            {
                                counter++;
                                FileName = postedFile.FileName;
                                string[] strfileName = FileName.Split('.');
                                FileName = strfileName[0] + "_" + counter;
                                SaveFileName = FileName + "." + strfileName[1];
                                _encryptionDecryption.EncryptFile(postedFile, filePath + Path.GetFileName(correlationId + "_" + SaveFileName));
                            }
                            else
                            {
                                SaveFileName = postedFile.FileName;
                                _encryptionDecryption.EncryptFile(postedFile, filePath + Path.GetFileName(correlationId + "_" + SaveFileName));

                            }
                            if (DataSourceFilePath != "")
                            {
                                DataSourceFilePath += "" + postedFile.FileName + ",";
                            }
                            else
                                DataSourceFilePath = DataSourceFilePath + "" + postedFile.FileName + ",";
                            if (fileCollection.Count > 1)
                            {
                                if (appSettings.Value.IsAESKeyVault && appSettings.Value.Environment == CONSTANTS.PADEnvironment && appSettings.Value.EncryptUploadedFiles)
                                    FilePath = FilePath + "" + filePath + correlationId + "_" + SaveFileName + ".enc" + @"""" + @",""";
                                else
                                    FilePath = FilePath + "" + filePath + correlationId + "_" + SaveFileName + @"""" + @",""";
                                if (postedFile.FileName == ParentFileName)
                                {
                                    ParentFileNamePath = filePath + correlationId + "_" + SaveFileName;

                                }
                            }
                            else
                            {
                                if (appSettings.Value.IsAESKeyVault && appSettings.Value.Environment == CONSTANTS.PADEnvironment && appSettings.Value.EncryptUploadedFiles)
                                    FilePath = FilePath + "" + filePath + correlationId + "_" + SaveFileName + ".enc" + @"""" + @",""";
                                else
                                    FilePath = FilePath + "" + filePath + correlationId + "_" + SaveFileName + @"""" + @",""";
                                ParentFileNamePath = ParentFileName;
                            }
                        }
                        if (fileCollection.Count > 0)
                        {
                            postedFileName = "[" + @"""" + FilePath.Remove(FilePath.Length - 2, 2) + "]";
                        }
                    }

                    DeleteIngestUseCase(correlationId);
                    IMongoCollection<BsonDocument> collection1;
                    if (servicename == "Anomaly")
                        collection1 = _databaseAD.GetCollection<BsonDocument>("PS_IngestedData");
                    else
                        collection1 = _database.GetCollection<BsonDocument>("PS_IngestedData");

                    //var collection1 = _database.GetCollection<BsonDocument>("PS_IngestedData");
                    var filter = Builders<BsonDocument>.Filter.Eq("CorrelationId", correlationId);
                    collection1.DeleteMany(filter);

                    ingrainRequest = new IngrainRequestQueue
                    {
                        _id = Guid.NewGuid().ToString(),
                        CorrelationId = correlationId,
                        DataSetUId = dataSetUId,
                        RequestId = Guid.NewGuid().ToString(),
                        ProcessId = null,
                        Status = null,
                        ModelName = ModelName,
                        RequestStatus = CONSTANTS.New,
                        RetryCount = 0,
                        ProblemType = null,
                        Message = null,
                        UniId = null,
                        Progress = null,
                        pageInfo = CONSTANTS.IngestData,
                        ParamArgs = null,
                        Function = CONSTANTS.FileUpload,
                        CreatedByUser = userId,
                        CreatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        ModifiedByUser = userId,
                        ModifiedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        LastProcessedOn = null,

                    };

                    AgileCase agileCase = new AgileCase();
                    if (oldCorrelationID != "undefined")
                    {
                        _agilefilepath = new AgileFilepath();
                        agileCase.oldcorrelationid = oldCorrelationID;
                        if (postedFileName != "")
                            _agilefilepath.fileList = postedFileName;
                        else
                            _agilefilepath.fileList = "null";
                        _agilefilepath.AgileUsecase = agileCase;

                    }
                    else
                    {
                        _filepath = new Filepath();
                        if (postedFileName != "")
                            _filepath.fileList = postedFileName;
                        else
                            _filepath.fileList = "null";
                    }

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
                    string Flag = CONSTANTS.Null;
                    if (servicename == "Anomaly")
                    {
                        if (!(Entities == CONSTANTS.Null && Customdetails == CONSTANTS.Null))
                            Flag = "Incremental";
                    }
                    if (ClusterFlag == "True")
                    {
                        IMongoCollection<AppIntegration> AppIntegCollection;
                        if (servicename == "Anomaly")
                            AppIntegCollection = _databaseAD.GetCollection<AppIntegration>(CONSTANTS.AppIntegration);
                        else
                            AppIntegCollection = _database.GetCollection<AppIntegration>(CONSTANTS.AppIntegration);
                        //var AppIntegCollection = _database.GetCollection<AppIntegration>(CONSTANTS.AppIntegration);
                        var filterBuilder = Builders<AppIntegration>.Filter;
                        var AppFilter = filterBuilder.Eq("ApplicationName", "Ingrain");

                        var Projection = Builders<AppIntegration>.Projection.Include("ApplicationID").Exclude("_id");
                        var AppData = AppIntegCollection.Find(AppFilter).Project<AppIntegration>(Projection).FirstOrDefault();

                        var fileParams = JsonConvert.DeserializeObject<InputParams>(Customdata);
                        InputParameter param = new InputParameter
                        {
                            correlationid = correlationId,
                            FromDate = EntityStartDate,
                            ToDate = EntityEndDate,
                            noOfRecord = 10000.0
                        };

                        CustomFlag customFlag = new CustomFlag
                        {
                            FlagName = "AgileUsecase"
                        };

                        Customdetails AppPayload = new Customdetails
                        {
                            CustomFlags = customFlag,
                            AppId = AppData.ApplicationID,
                            HttpMethod = "POST",
                            AppUrl = "Null",
                            InputParameters = param
                        };

                        CustomUploadEntity Customfile = new CustomUploadEntity
                        {
                            CorrelationId = correlationId,
                            ClientUID = clientUID,
                            DeliveryConstructUId = deliveryUID,
                            Parent = parentFile,
                            Flag = Flag,//CONSTANTS.Null,
                            mapping = CONSTANTS.Null,
                            mapping_flag = CONSTANTS.False,
                            pad = CONSTANTS.Null,
                            metric = CONSTANTS.Null,
                            InstaMl = CONSTANTS.Null,
                            fileupload = _filepath,
                            Customdetails = AppPayload,

                        };
                        DataSourceFilePath = DataSourceFilePath + "Phoenix CDM" + ",";

                        _ingestedData.Category = Category;

                        ingrainRequest.ParamArgs = Customfile.ToJson();
                    }
                    else
                    {
                        if (Customdata != CONSTANTS.Null && Customdata != string.Empty)
                        {
                            IMongoCollection<AppIntegration> AppIntegCollection;
                            if (servicename == "Anomaly")
                                AppIntegCollection = _databaseAD.GetCollection<AppIntegration>(CONSTANTS.AppIntegration);
                            else
                                AppIntegCollection = _database.GetCollection<AppIntegration>(CONSTANTS.AppIntegration);
                            //var AppIntegCollection = _database.GetCollection<AppIntegration>(CONSTANTS.AppIntegration);
                            var filterBuilder = Builders<AppIntegration>.Filter;
                            var AppFilter = filterBuilder.Eq("ApplicationName", appSettings.Value.ApplicationName);

                            var Projection = Builders<AppIntegration>.Projection.Include("ApplicationID").Exclude("_id");
                            var AppData = AppIntegCollection.Find(AppFilter).Project<AppIntegration>(Projection).FirstOrDefault();


                            var fileParams = JsonConvert.DeserializeObject<InputParams>(Customdata);
                            InputParams param = new InputParams
                            {
                                ClientID = clientUID,
                                E2EUID = E2EUID,
                                DeliveryConstructID = deliveryUID,
                                Environment = fileParams.Environment,
                                RequestType = fileParams.RequestType,
                                ServiceType = fileParams.ServiceType,
                                StartDate = fileParams.StartDate,
                                EndDate = fileParams.EndDate
                            };
                            CustomPayloads AppPayload = new CustomPayloads
                            {
                                AppId = AppData.ApplicationID,
                                HttpMethod = CONSTANTS.POST,
                                AppUrl = appSettings.Value.GetVdsPAMDataURL,
                                InputParameters = param,
                                AICustom = "False"
                            };
                            CustomUploadFile Customfile = new CustomUploadFile
                            {
                                CorrelationId = correlationId,
                                ClientUID = clientUID,
                                DeliveryConstructUId = deliveryUID,
                                Parent = parentFile,
                                Flag = Flag,//CONSTANTS.Null,
                                mapping = CONSTANTS.Null,
                                mapping_flag = CONSTANTS.False,
                                pad = CONSTANTS.Null,
                                metric = CONSTANTS.Null,
                                InstaMl = CONSTANTS.Null,
                                fileupload = _filepath,
                                Customdetails = AppPayload,

                            };
                            DataSourceFilePath = DataSourceFilePath + param.ServiceType + ",";
                            _ingestedData.Category = param.RequestType;
                            ingrainRequest.ParamArgs = Customfile.ToJson();

                        }
                        else if (CustomSourceItems != CONSTANTS.Null && CustomSourceItems != string.Empty)
                        {
                            if (Source.ToUpper() == CONSTANTS.CustomDbQuery.ToUpper())
                            {
                                var DateColumn = Convert.ToString(collection["DateColumn"]);
                                _filepath = new Filepath();
                                _filepath.fileList = "null";
                                var Query = CustomSourceItems;
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
                                ingrainRequest.ParamArgs = CustomQueryData.ToJson();
                            }
                            else if (Source.ToUpper() == CONSTANTS.CustomDataApi.ToUpper())
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
                                        grant_type = appSettings.Value.Grant_Type,
                                        client_secret = appSettings.Value.clientSecret,
                                        client_id = appSettings.Value.clientId,
                                        resource = appSettings.Value.resourceId
                                    };

                                    string TokenUrl = appSettings.Value.token_Url;
                                    string token = _customDataService.CustomUrlToken("Ingrain", oAuthCredentials, TokenUrl);
                                    if (!String.IsNullOrEmpty(token))
                                    {
                                        fileParams.Data.Authentication.Token = token;
                                    }
                                    else
                                    {
                                        return CONSTANTS.IngrainTokenBlank;
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

                                _ingestedData.DataSource = Source;
                                _ingestedData.Category = Category;
                                ingrainRequest.ParamArgs = CustomAPIData.ToJson();
                            }
                        }
                        else
                        {
                            if (oldCorrelationID != "undefined")
                            {
                                agileFileUpload = new AgileFileUpload
                                {
                                    CorrelationId = correlationId,
                                    ClientUID = clientUID,
                                    DeliveryConstructUId = deliveryUID,
                                    Parent = parentFile,
                                    Flag = Flag,//CONSTANTS.Null,
                                    mapping = MappingColumns,
                                    mapping_flag = MappingFlag,
                                    pad = Entities,
                                    metric = Metrices,
                                    InstaMl = InstaML,
                                    fileupload = _agilefilepath,
                                    Customdetails = CONSTANTS.Null

                                };
                                ingrainRequest.ParamArgs = agileFileUpload.ToJson();
                            }
                            else
                            {

                                fileUpload = new FileUpload
                                {
                                    CorrelationId = correlationId,
                                    ClientUID = clientUID,
                                    DeliveryConstructUId = deliveryUID,
                                    Parent = parentFile,
                                    Flag = Flag,//CONSTANTS.Null,
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
                        }
                    }

                    if (string.IsNullOrEmpty(_ingestedData.SourceName))
                    {
                        if (Entities != CONSTANTS.Null)
                        {
                            _ingestedData.SourceName = "pad";
                        }
                        else if (Metrices != CONSTANTS.Null)
                        {
                            _ingestedData.SourceName = "metric";
                        }
                        else if (!string.IsNullOrEmpty(postedFileName))
                        {
                            _ingestedData.SourceName = "file";
                        }

                        if (Source == "Custom")
                        {
                            _ingestedData.SourceName = "Custom";
                        }
                        if (Source.ToUpper() == CONSTANTS.CustomDbQuery.ToUpper())
                        {
                            _ingestedData.SourceName = CONSTANTS.CustomDbQuery;
                            _ingestedData.DataSource = CONSTANTS.CustomDbQuery;
                        }
                        if (Source.ToUpper() == CONSTANTS.CustomDataApi.ToUpper())
                        {
                            _ingestedData.SourceName = Source;
                        }
                    }

                    //storing dbencryption flag in ssai_deployedmodels----------------------
                    _ingestedData.ModelName = ModelName;
                    _ingestedData.CreatedByUser = userId;
                    _ingestedData.ModifiedByUser = userId;
                    _ingestedData.ClientUID = clientUID;
                    _ingestedData.DeliveryConstructUID = deliveryUID;
                    _ingestedData.Language = Language;
                    if (!string.IsNullOrEmpty(dataSetUId))
                        _ingestedData.SourceName = "DataSet";
                    if (Customdata == CONSTANTS.Null || Customdata == string.Empty)
                    {
                        _ingestedData.Category = Category;
                    }
                    if (DataSourceFilePath != "")
                        _ingestedData.DataSource = DataSourceFilePath.Remove(DataSourceFilePath.Length - 1, 1);//postedFile.FileName;   

                    if (Source.ToUpper() == CONSTANTS.CustomDbQuery.ToUpper())
                    {
                        CustomDataSourceModel CustomDataSource = new CustomDataSourceModel
                        {
                            _id = Guid.NewGuid().ToString(),
                            CorrelationId = correlationId,
                            CustomDataPullType = CONSTANTS.CustomDbQuery,
                            CustomSourceDetails = Convert.ToString(CustomSourceItems),
                            CreatedByUser = userId
                        };
                        _customDataService.InsertUpdateCustomDataSource(CustomDataSource, CONSTANTS.SSAICustomDataSource);
                    }
                    else if (Source.ToUpper() == CONSTANTS.CustomDataApi.ToUpper())
                    {
                        CustomInputData CustomfileParams = JsonConvert.DeserializeObject<CustomInputData>(CustomSourceItems);
                        if (CustomfileParams != null)
                        {
                            CustomfileParams.DbEncryption = encryptDB;
                            if (CustomfileParams.Data != null)
                            {
                                CustomfileParams.Data.Type = "API";
                            }
                        }

                        CustomDataSourceModel CustomDataSource = new CustomDataSourceModel
                        {
                            _id = Guid.NewGuid().ToString(),
                            CorrelationId = correlationId,
                            CustomDataPullType = CONSTANTS.CustomDataApi,
                            CustomSourceDetails = JsonConvert.SerializeObject(CustomfileParams),
                            CreatedByUser = userId
                        };
                        _customDataService.InsertUpdateCustomDataSource(CustomDataSource, CONSTANTS.SSAICustomDataSource);
                    }
                    InsertDataSourceDeployModels(_ingestedData, encryptDB, dataSetUId);
                    //--------------------------------------------------------------


                    InsertRequests(ingrainRequest, servicename);
                    Thread.Sleep(2000);
                    IngrainRequestQueue requestQueue = new IngrainRequestQueue();
                    requestQueue = GetFileRequestStatus(correlationId, CONSTANTS.IngestData);
                    if (requestQueue != null)
                    {
                        if (requestQueue.Status == CONSTANTS.C & requestQueue.Progress == CONSTANTS.Hundred)
                        {
                            ColumnsFlushforBusinessProblem(correlationId, userId, clientUID, deliveryUID);

                            string flushStatus = _flushService.DataSourceDelete(correlationId, servicename);
                            return CONSTANTS.Success;

                        }
                        else if (requestQueue.Status == CONSTANTS.M & requestQueue.Progress == CONSTANTS.Hundred)
                        {
                            ColumnsFlushforBusinessProblem(correlationId, userId, clientUID, deliveryUID);
                            string flushStatus = _flushService.DataSourceDelete(correlationId, servicename);
                            return CONSTANTS.Success;
                        }
                        else if (requestQueue.Status == CONSTANTS.E)
                        {
                            DeleteDeployedModel(correlationId);
                            return CONSTANTS.PhythonError;
                        }
                        else if (requestQueue.Status == CONSTANTS.I)
                        {
                            DeleteDeployedModel(correlationId);
                            return CONSTANTS.PhythonInfo;
                        }
                        else if (requestQueue.Status == CONSTANTS.P)
                        {
                            return CONSTANTS.PhythonProgress;
                        }
                        else
                        {
                            return CONSTANTS.New;
                        }
                    }
                    else
                    {
                        // flag = true;
                        Thread.Sleep(2000);
                    }
                    //  }
                    return string.Empty;
                }
            }
            else
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(IngestedDataService), nameof(DataSourceUploadFiles) + "--SEQUENCE 2 ELSE CorrelationId--" + correlationId + "--CorrelationId_status--" + CorrelationId_status, "START", string.IsNullOrEmpty(CorrelationId_status) ? default(Guid) : new Guid(CorrelationId_status), string.Empty, string.Empty, string.Empty, string.Empty);
                IngrainRequestQueue requestQueue = new IngrainRequestQueue();
                requestQueue = GetFileRequestStatus(CorrelationId_status, "IngestData");
                if (requestQueue != null)
                {
                    PythonCategory pythonCategory = new PythonCategory();
                    PythonInfo pythonInfo = new PythonInfo();
                    //LOGGING.LogManager.Logger.LogProcessInfo(type, Constant.UseCaseTableData, requestQueue.ToString(), string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId));
                    if (requestQueue.Status == "C" & requestQueue.Progress == "100")
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(IngestedDataService), nameof(DataSourceUploadFiles) + "--SEQUENCE 3 ELSE CorrelationId--" + correlationId + "--CorrelationId_status--" + CorrelationId_status, "START", string.IsNullOrEmpty(CorrelationId_status) ? default(Guid) : new Guid(CorrelationId_status), string.Empty, string.Empty, string.Empty, string.Empty);
                        ColumnsFlushforBusinessProblem(correlationId, userId, clientUID, deliveryUID);

                        string flushStatus = _flushService.DataSourceDelete(correlationId, servicename);
                        IMongoCollection<DeployModelsDto> deployModelCollection;
                        if (servicename == "Anomaly")
                            deployModelCollection = _databaseAD.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
                        else
                            deployModelCollection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
                        //var deployModelCollection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
                        var filter2 = Builders<DeployModelsDto>.Filter.Where(x => x.CorrelationId == CorrelationId_status);
                        var mdl = deployModelCollection.Find(filter2).FirstOrDefault();
                        if (mdl != null)
                        {
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(IngestedDataService), nameof(DataSourceUploadFiles) + "--SEQUENCE 4 ELSE CorrelationId--" + correlationId + "--CorrelationId_status--" + CorrelationId_status, "START", string.IsNullOrEmpty(CorrelationId_status) ? default(Guid) : new Guid(CorrelationId_status), string.Empty, string.Empty, string.Empty, string.Empty);
                            string flushStatus2 = _flushService.DataSourceDelete(mdl.FMCorrelationId, servicename);
                        }
                        //For Fm Featue
                        if (usecaseId != null && usecaseId != CONSTANTS.undefined)
                        {
                            if (usecaseId == CONSTANTS.FMUseCaseId)
                            {
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(IngestedDataService), nameof(DataSourceUploadFiles) + "--SEQUENCE 5 ELSE CorrelationId--" + correlationId + "--CorrelationId_status--" + CorrelationId_status, "START", string.IsNullOrEmpty(CorrelationId_status) ? default(Guid) : new Guid(CorrelationId_status), string.Empty, string.Empty, string.Empty, string.Empty);
                                //Call the FM scenario. Initiate request for FM scenario at windows service.
                                bool isFMStatus = CheckMatchedcolumns(usecaseId, CorrelationId_status);
                                if (isFMStatus)
                                {
                                    ProblemTypeDetails2 problemtype = FMUpdateModelName(CorrelationId_status, ModelName, usecaseId);
                                    IngrainRequestQueue ingrainRequest = new IngrainRequestQueue
                                    {
                                        _id = Guid.NewGuid().ToString(),
                                        CorrelationId = problemtype.FMModel1CorId,
                                        RequestId = Guid.NewGuid().ToString(),
                                        ProcessId = CONSTANTS.Null,
                                        ModelName = ModelName,
                                        Status = CONSTANTS.Null,
                                        RequestStatus = CONSTANTS.New,
                                        Message = CONSTANTS.Null,
                                        RetryCount = 0,
                                        AppID = problemtype.AppId,
                                        ProblemType = problemtype.ModelType,
                                        UniId = CONSTANTS.Null,
                                        InstaID = CONSTANTS.Null,
                                        Progress = CONSTANTS.Null,
                                        pageInfo = CONSTANTS.FMTransform,
                                        ParamArgs = CONSTANTS.Null,
                                        TemplateUseCaseID = usecaseId,
                                        FMCorrelationId = CorrelationId_status,
                                        Function = CONSTANTS.FMTransform,
                                        CreatedByUser = userId,
                                        CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                                        ModifiedByUser = CONSTANTS.Null,
                                        ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                                        LastProcessedOn = CONSTANTS.Null,
                                        ClientId = clientUID,
                                        DeliveryconstructId = deliveryUID,
                                        UseCaseID = CONSTANTS.Null,
                                        EstimatedRunTime = CONSTANTS.Null
                                    };
                                    if (problemtype != null)
                                    {
                                        if (problemtype.LinkedApps != null)
                                        {
                                            if (problemtype.LinkedApps.Length > 0)
                                            {
                                                ingrainRequest.ApplicationName = problemtype.LinkedApps[0];
                                            }
                                        }
                                    }
                                    InsertRequests(ingrainRequest, servicename);
                                }
                                else
                                {
                                    DeleteDeployedModel(CorrelationId_status);
                                    return CONSTANTS.FmUseCaseFail;
                                }
                            }
                        }
                        return CONSTANTS.Success;
                    }
                    else if (requestQueue.Status == "M" & requestQueue.Progress == "100")
                    {
                        ColumnsFlushforBusinessProblem(correlationId, userId, clientUID, deliveryUID);
                        //string flushStatus = getColumns.DataSourceDelete(correlationId);                                                 
                        string flushStatus = _flushService.DataSourceDelete(correlationId, servicename);
                        return CONSTANTS.Success;
                    }
                    else if (requestQueue.Status == "E")
                    {
                        DeleteDeployedModel(CorrelationId_status);
                        return CONSTANTS.PhythonError;

                    }
                    else if (requestQueue.Status == "I")
                    {
                        DeleteDeployedModel(CorrelationId_status);
                        return CONSTANTS.PhythonInfo;
                    }
                    else if (requestQueue.Status == "P")
                    {
                        return CONSTANTS.PhythonProgress;
                    }
                    else
                    {
                        return CONSTANTS.New;
                    }
                }
                return string.Empty;
            }
        }
        public FileUploadColums GetFilesColumns(string correlationId, string ParentFileName, string ModelName, string ServiceName = "")
        {
            servicename = ServiceName;
            FileUploadColums fileUploadColums = new FileUploadColums();
            IMongoCollection<BsonDocument> collection;
            if (servicename == "Anomaly")
                collection = _databaseAD.GetCollection<BsonDocument>("PS_MultiFileColumn");
            else
                collection = _database.GetCollection<BsonDocument>("PS_MultiFileColumn");
            //var collection = _database.GetCollection<BsonDocument>("PS_MultiFileColumn");
            var builder = Builders<BsonDocument>.Projection.Include("CorrelationId").Include("File").Include("Flag").Exclude("_id");
            var columnFilter = Builders<BsonDocument>.Filter.Eq("CorrelationId", correlationId);
            var UploadColums = collection.Find(columnFilter).Project<BsonDocument>(builder).ToList();
            if (UploadColums.Count > 0)
            {
                var file = JObject.Parse(UploadColums[0]["File"].ToString());
                fileUploadColums.CorrelationId = UploadColums[0]["CorrelationId"].ToString();
                fileUploadColums.Flag = UploadColums[0]["Flag"].ToString();
                fileUploadColums.ParentFileName = ParentFileName;
                fileUploadColums.ModelName = ModelName;
                List<FileColumns> fileColumns = new List<FileColumns>();
                bool ParentFlag = false;
                int flagfile_entity = 0;
                int flagmetric = 0;
                if (file != null)
                {
                    foreach (var item in file.Children())
                    {
                        JObject serializeData = new JObject();
                        JProperty jProperty = item as JProperty;
                        JObject serializeDataCols = new JObject();
                        if (jProperty != null)
                        {
                            //string filename = jProperty.Name.Remove(0, correlationId.Length);
                            Dictionary<string, string> DiColumn = new Dictionary<string, string>();
                            string column = jProperty.Value.ToString();
                            serializeData = JObject.Parse(column.ToString());
                            List<string> colsname = new List<string>();
                            colsname.Add(serializeData["Columns"].ToString());
                            string fileExtension = serializeData["FileExtensionOrig"].ToString();
                            string filename = string.Empty;
                            if (fileExtension == "csv" || fileExtension == "xlsx" || fileExtension == "Entity")
                            {
                                flagfile_entity = 1;
                            }
                            else
                            {
                                flagmetric = 1;
                            }

                            if (fileExtension != "Custom")
                            {
                                filename = jProperty.Name.Remove(0, correlationId.Length + 1);
                                if (filename + "." + fileExtension == ParentFileName)
                                {
                                    ParentFlag = true;
                                }
                                else
                                {
                                    ParentFlag = false;
                                }
                            }
                            else
                            {
                                filename = jProperty.Name;
                                if (filename == ParentFileName)
                                {
                                    ParentFlag = true;
                                }
                                else
                                {
                                    ParentFlag = false;
                                }
                            }
                            foreach (var item1 in serializeData["Columns"].Children())
                            {
                                JProperty jProperty1 = item1 as JProperty;
                                DiColumn.Add(jProperty1.Name, jProperty1.Value.ToString());

                            }
                            fileColumns.Add(new FileColumns
                            {
                                FileName = filename,//.Remove(0, 1),
                                FileColumn = DiColumn,
                                ParentFileFlag = ParentFlag
                            });
                        }
                    }
                    if (flagmetric == 1)
                        fileUploadColums.Fileflag = false;
                    else
                        fileUploadColums.Fileflag = true;

                    fileUploadColums.File = fileColumns;
                }
            }
            return fileUploadColums;
        }
        private string CheckQueueTable(string correlationId)
        {
            //var processModelObject = NinjectCoreBinding.NinjectKernel.Get<IProcessDataService>();
            string userModel = string.Empty;
            var collection = _database.GetCollection<BsonDocument>("SSAI_IngrainRequests");
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq("CorrelationId", correlationId) & builder.Eq("pageInfo", "IngestData");
            var projection = Builders<BsonDocument>.Projection.Include("Status").Include("Progress").
            Include("CorrelationId").Include("pageInfo").Include("Message").Exclude("_id");
            var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            //bool DBEncryptionRequired = CommonUtility.EncryptDB(correlationId, appSettings);
            if (result.Count > 0)
            {
                //if (DBEncryptionRequired)
                //{
                //    if (!string.IsNullOrEmpty(Convert.ToString(result[0]["CreatedByUser"])))
                //        result[0]["CreatedByUser"] = _encryptionDecryption.Decrypt(Convert.ToString(result[0]["CreatedByUser"]));
                //    if (!string.IsNullOrEmpty(Convert.ToString(result[0]["ModifiedByUser"])))
                //        result[0]["ModifiedByUser"] = _encryptionDecryption.Decrypt(Convert.ToString(result[0]["ModifiedByUser"]));
                //}
                userModel = result[0].ToJson();
            }
            var queueTableData = userModel;
            //return userModel;
            // var queueTableData = processModelObject.CheckPythonProcess(correlationId, Constant.IngestData);
            return queueTableData;

        }

        public void UpdateDeployModels(IngestedDataDTO data)
        {
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
            var filter = Builders<BsonDocument>.Filter.Eq("CorrelationId", data.CorrelationId);
            var result = collection.Find(filter).ToList();
            if (result.Count > 0)
            {
                var update = Builders<BsonDocument>.Update.Set("SourceName", data.SourceName).Set("DataSource", data.DataSource);
                collection.UpdateOne(filter, update);
            }
        }

        public void DeleteDeployedModel(string correlationid)
        {
            IMongoCollection<BsonDocument> collection;
            if (servicename == "Anomaly")
                collection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
            else
                collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);

            //var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
            var filter = Builders<BsonDocument>.Filter.Eq("CorrelationId", correlationid);
            var result = collection.Find(filter).ToList();

            if (result.Count > 0)
                collection.DeleteOne(filter);
        }

        /// <summary>
        /// View Uploaded Excel Data
        /// </summary>
        /// <param name="correlationId">Correlation Id</param>
        /// <returns></returns>
        public List<object> ViewUploadedData(string correlationId, int Precision, string ServiceName = "")
        {
            servicename = ServiceName;
            var Max = System.Decimal.MaxValue;
            var Min = System.Decimal.MinValue;
            var res=CommonUtility.GetDecimalValue_new("19", 2);
            var data = "";
            var excelData = new List<object>();


            BsonArray inputD = new BsonArray();
            IMongoCollection<DeployModelsDto> deployCollection;
            if (servicename == "Anomaly")
                deployCollection = _databaseAD.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
            else
                deployCollection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
            //var deployCollection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
            var deployfilterPS = Builders<DeployModelsDto>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var deployProjection = Builders<DeployModelsDto>.Projection.Exclude(CONSTANTS.Id);
            var deployResult = deployCollection.Find(deployfilterPS).Project<DeployModelsDto>(deployProjection).ToList();
            bool DBEncryptionRequired = false;
            if (deployResult.Count > 0)
            {
                DBEncryptionRequired = deployResult[0].DBEncryptionRequired;
                List<BsonDocument> inputData = new List<BsonDocument>();
                if (string.IsNullOrEmpty(deployResult[0].DataSetUId) || deployResult[0].DataSetUId == BsonNull.Value || deployResult[0].DataSetUId == CONSTANTS.Null || deployResult[0].DataSetUId == CONSTANTS.BsonNull)
                {
                    IMongoCollection<BsonDocument> columnCollection;
                    if (servicename == "Anomaly")
                        columnCollection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.PS_IngestedData);
                    else
                        columnCollection = _database.GetCollection<BsonDocument>(CONSTANTS.PS_IngestedData);
                    //var columnCollection = _database.GetCollection<BsonDocument>(CONSTANTS.PS_IngestedData);
                    var filterPS = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
                    var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.Input_Data).Exclude(CONSTANTS.Id);
                    inputData = columnCollection.Find(filterPS).Project<BsonDocument>(projection).ToList();
                }
                else
                {
                    IMongoCollection<BsonDocument> DataSetCollection;
                    if (servicename == "Anomaly")
                        DataSetCollection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.DataSet_IngestData);
                    else
                        DataSetCollection = _database.GetCollection<BsonDocument>(CONSTANTS.DataSet_IngestData);
                    //var DataSetCollection = _database.GetCollection<BsonDocument>(CONSTANTS.DataSet_IngestData);
                    var dataSetfilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.DataSetUId, deployResult[0].DataSetUId);
                    var dataSetprojection = Builders<BsonDocument>.Projection.Include(CONSTANTS.Input_Data).Exclude(CONSTANTS.Id);
                    inputData = DataSetCollection.Find(dataSetfilter).Project<BsonDocument>(dataSetprojection).ToList();
                }
                if (inputData.Count > 0)
                {
                    for (int i = 0; i < inputData.Count; i++)
                    {
                        if (DBEncryptionRequired)
                        {
                            data = _encryptionDecryption.Decrypt(inputData[i][CONSTANTS.Input_Data].AsString);
                        }
                        else
                        {
                            data = inputData[i][CONSTANTS.Input_Data].ToString();
                        }
                            //List<object> count = JsonConvert.DeserializeObject<List<object>>(data);
                            //excelData.AddRange(count);
                            //    if (excelData.Count > 100)
                            //        break;
                            inputD.AddRange(BsonSerializer.Deserialize<BsonArray>(data));
                            if (inputD.Count > 100)
                            break;
                    }
                }
                excelData=CommonUtility.GetDataAfterDecimalPrecision(inputD, Precision, 100, false);
            }
            return excelData.Take(100).ToList();
        }

        public dynamic DownloadTemplateFun(string correlationId)
        {
            ColumnList columnList = new ColumnList();
            columnList.ColumnListDetails = new List<object>();

            var cols = new Dictionary<string, string>();
            var columnCollection = _database.GetCollection<BsonDocument>(CONSTANTS.PS_IngestedData);
            var filterPS = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var projection = Builders<BsonDocument>.Projection.Include("ColumnsList").Exclude(CONSTANTS.Id);
            var inputData = columnCollection.Find(filterPS).Project<BsonDocument>(projection).ToList();

            if (inputData.Count > 0)
            {
                var ingestSerializeData = JObject.Parse(inputData[0].ToString());
                foreach (var features in ingestSerializeData["ColumnsList"].Children())
                {
                    cols.Add(features.ToString(), CONSTANTS.EmptySpace);
                }
                if (cols.Count > 0)
                {
                    var names = JsonConvert.SerializeObject(JObject.Parse(cols.ToJson()));
                    columnList.ColumnListDetails.Add(JsonConvert.DeserializeObject<object>(names));
                }
                return columnList;
            }
            return null;
        }

        public Inputvalidation GetInputvalidation(string correlationId, string pageInfo, string userId, string deliveryConstructUID, string clientUId, bool isTemplateModel)
        {
            Inputvalidation inputvalidation = new Inputvalidation();
            bool issucess = false;
            //bool isReadOnlyUser = this.ValidateUserAccess(correlationId, userId, deliveryConstructUID, clientUId, isTemplateModel);
            //if (!isReadOnlyUser)
            //{
            //    inputvalidation.Status = CONSTANTS.E;
            //    inputvalidation.Message = CONSTANTS.Val_AccessDenied;
            //}
            //else
            //{
            switch (pageInfo)
            {
                case "DataCleanup":
                    issucess = DataCleanupValidation(correlationId);
                    if (issucess)
                    {
                        inputvalidation.Status = CONSTANTS.C;
                    }
                    else
                    {
                        inputvalidation.Status = CONSTANTS.E;
                        inputvalidation.Message = CONSTANTS.Val_DataCleanup;
                    }
                    break;
                case "DataTransform":
                    issucess = DataTransformValidation(correlationId);
                    if (issucess)
                    {
                        inputvalidation.Status = CONSTANTS.C;
                    }
                    else
                    {
                        inputvalidation.Status = CONSTANTS.E;
                        inputvalidation.Message = CONSTANTS.Val_Datatransform;
                    }
                    break;
                case "RecommendedAI":
                    issucess = RecommendedAIValidation(correlationId);
                    if (issucess)
                    {
                        inputvalidation.Status = CONSTANTS.C;
                    }
                    else
                    {
                        inputvalidation.Status = CONSTANTS.E;
                        inputvalidation.Message = CONSTANTS.Val_RecommendedAI;
                    }
                    break;
                case "RestartTrain":
                    issucess = RestartTrainValidation(correlationId);
                    if (issucess)
                    {
                        inputvalidation.Status = CONSTANTS.C;
                    }
                    else
                    {
                        inputvalidation.Status = CONSTANTS.E;
                        inputvalidation.Message = CONSTANTS.Val_Retrain;
                    }
                    break;
            }
            //}

            return inputvalidation;
        }

        private bool ValidateUserAccess(string correlationId, string userId, string deliveryConstructUID, string clientUId, bool isTemplateModel)
        {
            if (isTemplateModel)
            {
                return true;
            }
            else
            {
                var collection = _database.GetCollection<PublishModelDTO>(CONSTANTS.SSAIDeployedModels);
                var filter = Builders<PublishModelDTO>.Filter.Eq(CONSTANTS.CorrelationId, correlationId) &
                    Builders<PublishModelDTO>.Filter.Eq(CONSTANTS.CreatedByUser, userId) &
                    Builders<PublishModelDTO>.Filter.Eq(CONSTANTS.DeliveryConstructUID, deliveryConstructUID) &
                    Builders<PublishModelDTO>.Filter.Eq(CONSTANTS.ClientUId, clientUId) & Builders<PublishModelDTO>.Filter.Ne(CONSTANTS.IsModelTemplate, true);

                var projection = Builders<PublishModelDTO>.Projection.Include(CONSTANTS.IsPrivate).Include(CONSTANTS.IsModelTemplate).Include(CONSTANTS.DeliveryConstructUID).Include(CONSTANTS.CreatedByUser).Include(CONSTANTS.ClientUId).Include(CONSTANTS.CorrelationId).Exclude(CONSTANTS.Id);
                var models = collection.Find(filter).Project<PublishModelDTO>(projection).ToList();

                if (models.Count > 0)
                {
                    return true;   //Editable           
                }

                return false; //ReadOnlyUser
            }
        }

        private bool DataCleanupValidation(string correlationId)
        {
            bool isSuccess = true;
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataCleanup);
            var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.FeatureName).Exclude(CONSTANTS.Id);
            var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                bool DBEncryptionRequired = CommonUtility.EncryptDB(correlationId, appSettings);
                BsonDocument processDocument = result[0];
                if (DBEncryptionRequired)
                {
                    processDocument[CONSTANTS.FeatureName] = BsonDocument.Parse(_encryptionDecryption.Decrypt(result[0][CONSTANTS.FeatureName].AsString));
                }
                JObject data = JObject.Parse(processDocument.ToString());
                List<string> mainColumns = new List<string>();
                if (data != null)
                {
                    foreach (JProperty item in data[CONSTANTS.FeatureName].Children())
                    {
                        mainColumns.Add(item.Name);
                    }
                    foreach (var column in mainColumns)
                    {
                        foreach (JProperty item in data[CONSTANTS.FeatureName][column][CONSTANTS.Datatype].Children())
                        {
                            if (item != null)
                            {
                                if (item.Name == CONSTANTS.Select_Option & item.Value.ToString() == CONSTANTS.True)
                                {
                                    isSuccess = false;
                                }
                            }
                        }
                    }
                }
            }
            return isSuccess;
        }
        private bool DataTransformValidation(string correlationId)
        {
            bool isSuccess = false;
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataProcessing);
            var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.DataTransformationApplied).Exclude(CONSTANTS.Id);
            var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                if (Convert.ToBoolean(result[0][CONSTANTS.DataTransformationApplied]) == true)
                    isSuccess = true;
            }
            return isSuccess;
        }
        private bool RecommendedAIValidation(string correlationId)
        {
            bool isSuccess = false;
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIRecommendedTrainedModels);
            var result = collection.Find(filter).ToList();
            if (result.Count > 0)
            {
                isSuccess = true;
            }
            return isSuccess;
        }
        private bool RestartTrainValidation(string correlationId)
        {
            bool isSuccess = false;
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.MERecommendedModels);
            var result = collection.Find(filter).ToList();
            if (result.Count > 0)
            {
                isSuccess = Convert.ToBoolean(result[0][CONSTANTS.retrain]);
            }
            return isSuccess;
        }
        
        #endregion
    }
}