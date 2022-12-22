using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
using Accenture.MyWizard.Ingrain.DataAccess;
using Accenture.MyWizard.Ingrain.DataModels.Models;
using Accenture.MyWizard.Shared.Helpers;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
using Microsoft.Extensions.DependencyInjection;
using Accenture.MyWizard.Ingrain.BusinessDomain.Common;
using Newtonsoft.Json;
using MongoDB.Bson.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace Accenture.MyWizard.Ingrain.BusinessDomain.Services
{
    public class VDSDataService : IVdsService
    {
        #region Members
        private MongoClient _mongoClient;
        private IMongoDatabase _database;
        private readonly DatabaseProvider databaseProvider;
        private readonly IOptions<IngrainAppSettings> appSettings;
        private const string MultiClass = "Multi_Class";
        private const string Classification = "Classification";
        private IIngestedData _ingestedDataService { get; set; }
        public static IEncryptionDecryption _encryptionDecryption { set; get; }
        public IGenericSelfservice _genericSelfservice { get; set; }
        public IFlushService _flushService { get; set; }
        private CallBackErrorLog auditTrailLog;
        private IFlaskAPI _iFlaskAPIService;
        private GenericModelTrainingResponse _TrainingResponse;
        private SSAIIngrainTrainingStatus _trainingStatus;
        private ISPAVelocityService _VelocityService { get; set; }
        private IDeployedModelService _deployedModelService;
        #endregion

        #region Constructors
        /// <summary>
        /// Constructor to Initialize mongoDB Connection
        /// </summary>
        public VDSDataService(DatabaseProvider db, IOptions<IngrainAppSettings> settings, IServiceProvider serviceProvider)
        {
            databaseProvider = db;
            appSettings = settings;
            _mongoClient = databaseProvider.GetDatabaseConnection();
            var dataBaseName = MongoUrl.Create(appSettings.Value.connectionString).DatabaseName;
            _database = _mongoClient.GetDatabase(dataBaseName);
            _encryptionDecryption = serviceProvider.GetService<IEncryptionDecryption>();
            _ingestedDataService = serviceProvider.GetService<IIngestedData>();
            _genericSelfservice = serviceProvider.GetService<IGenericSelfservice>();
            _flushService = serviceProvider.GetService<IFlushService>();
            _iFlaskAPIService = serviceProvider.GetService<IFlaskAPI>();
            auditTrailLog = new CallBackErrorLog();
            _TrainingResponse = new GenericModelTrainingResponse();
            _trainingStatus = new SSAIIngrainTrainingStatus();
            _deployedModelService = serviceProvider.GetService<IDeployedModelService>();
            _VelocityService = serviceProvider.GetService<ISPAVelocityService>();
        }

        public VDSModelDTO VDSModelDetails(string correlationId, string modelType)
        {
            VDSModelDTO vDSModel = new VDSModelDTO();
            var filter = Builders<BsonDocument>.Filter.Eq("CorrelationId", correlationId);

            var collection_IngestData = _database.GetCollection<BsonDocument>("PS_IngestedData");
            var collection_DataCleanup = _database.GetCollection<BsonDocument>("DE_DataCleanup");
            var collection_FilteredData = _database.GetCollection<BsonDocument>("DataCleanUP_FilteredData");
            var collection_DeployedModels = _database.GetCollection<BsonDocument>("SSAI_DeployedModels");
            var collection_BusinessProblem = _database.GetCollection<BsonDocument>("PS_BusinessProblem");

            var projection = Builders<BsonDocument>.Projection.Include("DataType").Include("SourceDetails").Exclude("_id");
            var ingestCollectionResult = collection_IngestData.Find(filter).Project<BsonDocument>(projection).ToList();
            if (ingestCollectionResult.Count > 0)
            {
                if (ingestCollectionResult[0]["SourceDetails"].ToString().Contains("Entity"))
                {
                    vDSModel.EntityName = ingestCollectionResult[0]["SourceDetails"]["Entity"].ToString();
                }
                var DeployedModelsProjection = Builders<BsonDocument>.Projection.Include("ModelType").Include("ModelName").Include(CONSTANTS.InstaId).Include("ModelURL").Include("DeliveryConstructUID").Include("ClientUId").Include("CorrelationId").Include("CreatedOn").
                    Include("ModifiedOn").Exclude("_id");
                var DeployedModelsResult = collection_DeployedModels.Find(filter).Project<BsonDocument>(DeployedModelsProjection).ToList();
                if (DeployedModelsResult.Count > 0)
                {
                    vDSModel.CorrelationId = DeployedModelsResult[0]["CorrelationId"].ToString();
                    if (string.IsNullOrEmpty(vDSModel.EntityName))
                    {
                        if (DeployedModelsResult[0][CONSTANTS.InstaId].ToString() != CONSTANTS.Null && DeployedModelsResult[0][CONSTANTS.InstaId].ToString() != CONSTANTS.BsonNull && DeployedModelsResult[0][CONSTANTS.InstaId].ToString() != null && DeployedModelsResult[0][CONSTANTS.InstaId].ToString() != "{}")
                        {
                            vDSModel.EntityName = "InstaML";
                        }
                    }

                    if (DeployedModelsResult[0]["ModelType"].ToString() != "BsonNull")
                    {
                        string dbModelType = DeployedModelsResult[0]["ModelType"].ToString();
                        vDSModel.ModelType = dbModelType == MultiClass ? Classification : DeployedModelsResult[0]["ModelType"].ToString();
                    }
                    if (DeployedModelsResult[0]["ModelName"].ToString() != "BsonNull")
                        vDSModel.ModelName = DeployedModelsResult[0]["ModelName"].ToString();
                    if (DeployedModelsResult[0]["ModelURL"].ToString() != "BsonNull")
                        vDSModel.WebServiceURL = DeployedModelsResult[0]["ModelURL"].ToString();
                    vDSModel.ClientID = DeployedModelsResult[0]["ClientUId"].ToString();
                    vDSModel.DCID = DeployedModelsResult[0]["DeliveryConstructUID"].ToString();
                    vDSModel.CreatedDateTime = DeployedModelsResult[0]["CreatedOn"].ToString();
                    vDSModel.LastModifiedDateTime = DeployedModelsResult[0]["ModifiedOn"].ToString();
                }
                var BusinessProbDataProjection = Builders<BsonDocument>.Projection.Exclude("_id");
                var BusinessProbDataResult = collection_BusinessProblem.Find(filter).Project<BsonDocument>(BusinessProbDataProjection).ToList();
                string customTarget = null;
                if (BusinessProbDataResult.Count > 0)
                {
                    vDSModel.TargetIdentifier = BusinessProbDataResult[0]["TargetUniqueIdentifier"].ToString();
                    if (modelType == "TimeSeries")
                    {
                        vDSModel.TimeSeriesColumn = Convert.ToString(BusinessProbDataResult[0]["TimeSeries"]["TimeSeriesColumn"]);
                    }
                    //For Add Feature(DataCuration) --- Get the CustomTargetValue if custom target is selected
                    if (BusinessProbDataResult[0].Contains("IsCustomColumnSelected") && Convert.ToString(BusinessProbDataResult[0]["IsCustomColumnSelected"]) == "True")
                    {
                        customTarget = BusinessProbDataResult[0]["TargetColumn"].ToString();
                    }
                }
                var dataCleanupProjection = Builders<BsonDocument>.Projection.Include("Feature Name").Include(CONSTANTS.NewFeatureName).Include(CONSTANTS.NewAddFeatures).Exclude("_id");
                var dataCleanupResult = collection_DataCleanup.Find(filter).Project<BsonDocument>(dataCleanupProjection).ToList();
                JObject serializedData = null;
                List<string> attributes = new List<string>();
                List<string> addFeatureColumns = new List<string>();
                if (dataCleanupResult.Count > 0)
                {
                    //decrypt db data
                    bool DBEncryptionRequired = CommonUtility.EncryptDB(correlationId, appSettings);
                    if (DBEncryptionRequired)
                    {
                        //if (!string.IsNullOrEmpty(Convert.ToString(dataCleanupResult[0][CONSTANTS.CreatedByUser])))
                        //{
                        //    dataCleanupResult[0][CONSTANTS.CreatedByUser] = _encryptionDecryption.Decrypt(Convert.ToString(dataCleanupResult[0][CONSTANTS.CreatedByUser]));
                        //}
                        //if (!string.IsNullOrEmpty(Convert.ToString(dataCleanupResult[0][CONSTANTS.ModifiedByUser])))
                        //{
                        //    dataCleanupResult[0][CONSTANTS.ModifiedByUser] = _encryptionDecryption.Decrypt(Convert.ToString(dataCleanupResult[0][CONSTANTS.ModifiedByUser]));
                        //}
                        dataCleanupResult[0][CONSTANTS.FeatureName] = BsonDocument.Parse(_encryptionDecryption.Decrypt(dataCleanupResult[0][CONSTANTS.FeatureName].AsString));
                        if (dataCleanupResult[0].Contains(CONSTANTS.NewFeatureName))
                        {
                            if (dataCleanupResult[0][CONSTANTS.NewFeatureName].ToString() != "{ }")
                                dataCleanupResult[0][CONSTANTS.NewFeatureName] = BsonDocument.Parse(_encryptionDecryption.Decrypt(dataCleanupResult[0][CONSTANTS.NewFeatureName].AsString));
                        }
                    }

                    //For Add Feature(DataCuration) --- Only merging of "Feature Name & Feature Name_New Feature" should happen if custom target is selected
                    if (!string.IsNullOrEmpty(customTarget))
                    {
                        JObject datas = JObject.Parse(dataCleanupResult[0].ToString());
                        JObject combinedFeatures = new JObject();
                        combinedFeatures = this.CombinedFeatures(datas, customTarget);
                        if (combinedFeatures != null)
                            dataCleanupResult[0][CONSTANTS.FeatureName] = BsonDocument.Parse(combinedFeatures.ToString());
                    }

                    serializedData = JObject.Parse(dataCleanupResult[0].ToString());
                    foreach (var scales in serializedData["Feature Name"].Children())
                    {
                        var scaleProperties = scales as JProperty;
                        if (scaleProperties != null)
                        {
                            attributes.Add(scaleProperties.Name);
                        }
                    }
                    Dictionary<string, string> scaleDictionary = new Dictionary<string, string>();
                    foreach (var item in attributes)
                    {
                        if (item == vDSModel.TargetIdentifier)
                        {
                            scaleDictionary.Add(item, "Nominal Dimension");
                        }
                        else
                        {
                            foreach (var DTscale in serializedData["Feature Name"][item]["Datatype"].Children())
                            {
                                var DTproperty = DTscale as JProperty;
                                if (DTproperty.Value.ToString() == "True")
                                {
                                    if (DTproperty.Name == "Id")
                                        scaleDictionary.Add(item, "Nominal Dimension");
                                    //Defect- 837217 & User Story 962493 for VDS : needed "Text dimension" for text datatype
                                    else if (DTproperty.Name == "Text")
                                        scaleDictionary.Add(item, "Text");
                                    else if (DTproperty.Name == "category")
                                    {
                                        foreach (var scale in serializedData["Feature Name"][item]["Scale"].Children())
                                        {
                                            var property = scale as JProperty;
                                            if (property.Value.ToString() == "True")
                                            {
                                                scaleDictionary.Add(item, property.Name + " Dimension");
                                            }
                                        }
                                    }
                                    else if (DTproperty.Name == "datetime64[ns]")
                                        scaleDictionary.Add(item, "Date Dimension");
                                    else if (DTproperty.Name == "float64" || DTproperty.Name == "int64")
                                        scaleDictionary.Add(item, "Measure");
                                }
                            }
                        }
                    }
                    vDSModel.DataRoleScale = scaleDictionary;

                    //For Add Feature(DataCuration) -- Get all the Features created through Add Feature.
                    if (serializedData.ContainsKey(CONSTANTS.NewAddFeatures))
                    {
                        foreach (var addFeature in serializedData[CONSTANTS.NewAddFeatures].Children())
                        {
                            JProperty prop = addFeature as JProperty;
                            if (prop.Name != customTarget)
                                addFeatureColumns.Add(prop.Name);
                        }
                    }
                }

                var filteredDataProjection = Builders<BsonDocument>.Projection.Include("ColumnUniqueValues").
                    Include("target_variable").Include("DateFormats").Include("types").Include("inputcols").Include("removedcols").Exclude("_id");
                var filteredDataProjection1 = Builders<BsonDocument>.Projection.Include("ColumnUniqueValues").Exclude("_id");
                var filteredDataResult1 = collection_FilteredData.Find(filter).Project<BsonDocument>(filteredDataProjection1).ToList();
                var filteredDataResult = collection_FilteredData.Find(filter).Project<BsonDocument>(filteredDataProjection).ToList();
                JObject serializedValuesData = null;
                JObject serializedValuesData1 = null;
                JObject serializedDateFormat = null;
                if (filteredDataResult.Count > 0)
                {
                    bool DBEncryptionRequired = CommonUtility.EncryptDB(correlationId, appSettings);
                    if (DBEncryptionRequired)
                    {
                        filteredDataResult[0]["ColumnUniqueValues"] = BsonDocument.Parse(_encryptionDecryption.Decrypt(filteredDataResult[0]["ColumnUniqueValues"].AsString));
                        //if (!string.IsNullOrEmpty(Convert.ToString(filteredDataResult[0]["CreatedByUser"])))
                        //    filteredDataResult[0]["CreatedByUser"] = _encryptionDecryption.Decrypt(filteredDataResult[0]["CreatedByUser"].AsString);
                        //if (!string.IsNullOrEmpty(Convert.ToString(filteredDataResult[0]["ModifiedByUser"])))
                        //    filteredDataResult[0]["ModifiedByUser"] =_encryptionDecryption.Decrypt(filteredDataResult[0]["ModifiedByUser"].AsString);
                    }
                    vDSModel.TargetName = filteredDataResult[0]["target_variable"].ToString();
                    serializedDateFormat = JObject.Parse(filteredDataResult[0]["DateFormats"].ToString());
                    vDSModel.DataType = JObject.Parse(filteredDataResult[0]["types"].ToString());

                    List<string> removedColumns = new List<string>();
                    if (filteredDataResult[0]["removedcols"].IsBsonArray && ((MongoDB.Bson.BsonArray)filteredDataResult[0]["removedcols"]).Count > 0)
                    {
                        for (int i = 0; i < ((MongoDB.Bson.BsonArray)filteredDataResult[0]["removedcols"]).Count; i++)
                        { removedColumns.Add(filteredDataResult[0]["removedcols"][i].ToString()); }

                    }

                    //For Add Feature(DataCuration) --- Except "Custom Target" restrict data type for other Features.
                    if (addFeatureColumns.Count > 0)
                    {
                        JObject dataTypes = new JObject();
                        foreach (var parent in vDSModel.DataType.Children())
                        {
                            JProperty prop = parent as JProperty;
                            if (!addFeatureColumns.Contains(prop.Name))
                                dataTypes.Add(parent);
                        }

                        vDSModel.DataType = dataTypes;
                    }

                    //for removing 'removedcols' frm DataType
                    if (removedColumns.Count > 0)
                    {
                        JObject dataTypes = new JObject();
                        foreach (var parent in vDSModel.DataType.Children())
                        {
                            JProperty prop = parent as JProperty;
                            if (!removedColumns.Contains(prop.Name))
                                dataTypes.Add(parent);
                        }
                        vDSModel.DataType = dataTypes;
                    }

                    //Temparory Fix for the defect#1107986 - DataType coming as Object instead of Text

                    foreach (var parent in vDSModel.DataType.Children())
                    {
                        JProperty prop = parent as JProperty;
                        if (prop.Value.ToString() == "object")
                        {
                            if (vDSModel.DataRoleScale.Count > 0)
                            {
                                var keysWithMatchingValues = vDSModel.DataRoleScale.Where(p => p.Value == "Text").Select(p => p.Key);

                                foreach (var key in keysWithMatchingValues)
                                {
                                    if (prop.Name == key)
                                    {
                                        prop.Value = "Text";
                                    }
                                }
                            }

                        }

                    }

                    vDSModel.UnitDateFormat = serializedDateFormat;

                    //for removing 'removedcols' frm UnitDateFormat
                    if (removedColumns.Count > 0)
                    {
                        JObject unitFor = new JObject();
                        foreach (var parent in vDSModel.UnitDateFormat.Children())
                        {
                            JProperty prop = parent as JProperty;
                            if (!removedColumns.Contains(prop.Name))
                                unitFor.Add(parent);
                        }
                        vDSModel.UnitDateFormat = unitFor;
                    }
                    serializedValuesData = JObject.Parse(filteredDataResult[0].ToString());
                    string ColumnUniqueValue = string.Empty;

                    if (DBEncryptionRequired)
                    {
                        filteredDataResult1[0]["ColumnUniqueValues"] = BsonDocument.Parse(_encryptionDecryption.Decrypt(filteredDataResult1[0]["ColumnUniqueValues"].AsString));
                        //if (!string.IsNullOrEmpty(Convert.ToString(filteredDataResult1[0]["CreatedByUser"])))
                        //    filteredDataResult1[0]["CreatedByUser"] = _encryptionDecryption.Decrypt(filteredDataResult1[0]["CreatedByUser"].AsString);
                        //if (!string.IsNullOrEmpty(Convert.ToString(filteredDataResult1[0]["ModifiedByUser"])))
                        //    filteredDataResult1[0]["ModifiedByUser"] = _encryptionDecryption.Decrypt(filteredDataResult1[0]["ModifiedByUser"].AsString);
                    }
                    serializedValuesData1 = JObject.Parse(filteredDataResult1[0]["ColumnUniqueValues"].ToString());
                    List<string> validColumns = new List<string>();
                    foreach (var columns in serializedValuesData["types"].Children())
                    {
                        JProperty column = columns as JProperty;
                        if (column.Value.ToString() == "float64" || column.Value.ToString() == "int64" || column.Value.ToString() == "datetime64[ns]")
                        {
                            validColumns.Add(column.Name);
                        }

                    }

                    foreach (var values in validColumns)
                    {

                        JObject header2 = (JObject)serializedValuesData.SelectToken("ColumnUniqueValues");
                        header2.Property(values).Remove();
                    }
                    vDSModel.ValidValues = serializedValuesData1;
                    List<string> inputcols = new List<string>();
                    foreach (var item in serializedValuesData["inputcols"].Children())
                    {
                        inputcols.Add(item.ToString());
                    }
                    foreach (var type in serializedValuesData["types"].Children())
                    {
                        JProperty column = type as JProperty;
                        if (!inputcols.Contains(column.Name))
                        {
                            vDSModel.ValidValues.Remove(column.Name);
                            vDSModel.DataType.Remove(column.Name);
                        }
                    }
                    //for removing 'removedcols' frm ValidValues
                    if (removedColumns.Count > 0)
                    {
                        JObject validVal = new JObject();
                        foreach (var parent in vDSModel.ValidValues.Children())
                        {
                            JProperty prop = parent as JProperty;
                            if (!removedColumns.Contains(prop.Name))
                                validVal.Add(parent);
                        }
                        vDSModel.ValidValues = validVal;
                    }

                }
            }
            return vDSModel;
        }

        private JObject CombinedFeatures(JObject datas, string customTarget)
        {
            List<JToken> MergerdFeatures = new List<JToken>();
            if (datas.ContainsKey(CONSTANTS.NewFeatureName) && datas[CONSTANTS.NewFeatureName].HasValues && !string.IsNullOrEmpty(Convert.ToString(datas[CONSTANTS.NewFeatureName])))
            {
                foreach (var featureName in datas[CONSTANTS.FeatureName].Children())
                {
                    JProperty prop = featureName as JProperty;
                    MergerdFeatures.Add(featureName);
                }

                foreach (var newFeatureName in datas[CONSTANTS.NewFeatureName].Children())
                {
                    JProperty prop = newFeatureName as JProperty;
                    if (prop.Name == customTarget)
                        MergerdFeatures.Add(newFeatureName);
                }

                JObject Features = new JObject() { MergerdFeatures };

                return Features;
            }

            return null;
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
                var dsList = result[0]["DataSource"].AsString;
                dataSource = dsList;
            }
            return dataSource;
        }
        public VDSModelDTO VDSManagedInstanceModelDetails(string correlationId, string modelType)
        {
            VDSModelDTO vDSModel = new VDSModelDTO();
            var filter = Builders<BsonDocument>.Filter.Eq("CorrelationId", correlationId);

            var collection_IngestData = _database.GetCollection<BsonDocument>("PS_IngestedData");
            var collection_DataCleanup = _database.GetCollection<BsonDocument>("DE_DataCleanup");
            var collection_FilteredData = _database.GetCollection<BsonDocument>("DataCleanUP_FilteredData");
            var collection_DeployedModels = _database.GetCollection<BsonDocument>("SSAI_DeployedModels");
            var collection_BusinessProblem = _database.GetCollection<BsonDocument>("PS_BusinessProblem");

            var projection = Builders<BsonDocument>.Projection.Include("DataType").Include("SourceDetails").Exclude("_id");
            var ingestCollectionResult = collection_IngestData.Find(filter).Project<BsonDocument>(projection).ToList();
            if (ingestCollectionResult.Count > 0)
            {
                //vDSModel.EntityName = ingestCollectionResult[0]["SourceDetails"]["Entity"].ToString();
                vDSModel.EntityName = GetDefaultEntityName(correlationId);

                var DeployedModelsProjection = Builders<BsonDocument>.Projection.Include("ModelType").Include("ModelName").Include("ModelURL").Include("DeliveryConstructUID").Include("ClientUId").Include("CorrelationId").Include("CreatedOn").
                    Include("ModifiedOn").Exclude("_id");
                var DeployedModelsResult = collection_DeployedModels.Find(filter).Project<BsonDocument>(DeployedModelsProjection).ToList();
                if (DeployedModelsResult.Count > 0)
                {
                    vDSModel.CorrelationId = DeployedModelsResult[0]["CorrelationId"].ToString();
                    if (DeployedModelsResult[0]["ModelType"].ToString() != "BsonNull")
                    {
                        string dbModelType = DeployedModelsResult[0]["ModelType"].ToString();
                        vDSModel.ModelType = dbModelType == MultiClass ? Classification : DeployedModelsResult[0]["ModelType"].ToString();
                    }
                    if (DeployedModelsResult[0]["ModelName"].ToString() != "BsonNull")
                        vDSModel.ModelName = DeployedModelsResult[0]["ModelName"].ToString();
                    if (DeployedModelsResult[0]["ModelURL"].ToString() != "BsonNull")
                        vDSModel.WebServiceURL = DeployedModelsResult[0]["ModelURL"].ToString();
                    vDSModel.ClientID = DeployedModelsResult[0]["ClientUId"].ToString();
                    vDSModel.DCID = DeployedModelsResult[0]["DeliveryConstructUID"].ToString();
                    vDSModel.CreatedDateTime = DeployedModelsResult[0]["CreatedOn"].ToString();
                    vDSModel.LastModifiedDateTime = DeployedModelsResult[0]["ModifiedOn"].ToString();
                }
                var BusinessProbDataProjection = Builders<BsonDocument>.Projection.Exclude("_id");
                var BusinessProbDataResult = collection_BusinessProblem.Find(filter).Project<BsonDocument>(BusinessProbDataProjection).ToList();
                string customTarget = null;
                if (BusinessProbDataResult.Count > 0)
                {
                    vDSModel.TargetIdentifier = BusinessProbDataResult[0]["TargetUniqueIdentifier"].ToString();
                    if (modelType == "TimeSeries")
                    {
                        vDSModel.TimeSeriesColumn = Convert.ToString(BusinessProbDataResult[0]["TimeSeries"]["TimeSeriesColumn"]);
                    }
                }
                var dataCleanupProjection = Builders<BsonDocument>.Projection.Include("Feature Name").Include(CONSTANTS.NewFeatureName).Include(CONSTANTS.NewAddFeatures).Exclude("_id");
                var dataCleanupResult = collection_DataCleanup.Find(filter).Project<BsonDocument>(dataCleanupProjection).ToList();
                JObject serializedData = null;
                List<string> attributes = new List<string>();
                List<string> addFeatureColumns = new List<string>();
                if (dataCleanupResult.Count > 0)
                {
                    //decrypt db data
                    bool DBEncryptionRequired = CommonUtility.EncryptDB(correlationId, appSettings);
                    if (DBEncryptionRequired)
                    {
                        dataCleanupResult[0][CONSTANTS.FeatureName] = BsonDocument.Parse(_encryptionDecryption.Decrypt(dataCleanupResult[0][CONSTANTS.FeatureName].AsString));
                        //if (!string.IsNullOrEmpty(Convert.ToString(dataCleanupResult[0][CONSTANTS.CreatedByUser])))
                        //{
                        //    dataCleanupResult[0][CONSTANTS.CreatedByUser] = _encryptionDecryption.Decrypt(Convert.ToString(dataCleanupResult[0][CONSTANTS.CreatedByUser]));
                        //}
                        //if (!string.IsNullOrEmpty(Convert.ToString(dataCleanupResult[0][CONSTANTS.ModifiedByUser])))
                        //{
                        //    dataCleanupResult[0][CONSTANTS.ModifiedByUser] = _encryptionDecryption.Decrypt(Convert.ToString(dataCleanupResult[0][CONSTANTS.ModifiedByUser]));
                        //}
                        if (dataCleanupResult[0].Contains(CONSTANTS.NewFeatureName))
                        {
                            if (dataCleanupResult[0][CONSTANTS.NewFeatureName].ToString() != "{ }")
                                dataCleanupResult[0][CONSTANTS.NewFeatureName] = BsonDocument.Parse(_encryptionDecryption.Decrypt(dataCleanupResult[0][CONSTANTS.NewFeatureName].AsString));
                        }
                    }
                    //For Add Feature(DataCuration) --- Only merging of "Feature Name & Feature Name_New Feature" should happen if custom target is selected
                    if (!string.IsNullOrEmpty(customTarget))
                    {
                        JObject datas = JObject.Parse(dataCleanupResult[0].ToString());
                        JObject combinedFeatures = new JObject();
                        combinedFeatures = this.CombinedFeatures(datas, customTarget);
                        if (combinedFeatures != null)
                            dataCleanupResult[0][CONSTANTS.FeatureName] = BsonDocument.Parse(combinedFeatures.ToString());
                    }

                    serializedData = JObject.Parse(dataCleanupResult[0].ToString());
                    foreach (var scales in serializedData["Feature Name"].Children())
                    {
                        var scaleProperties = scales as JProperty;
                        if (scaleProperties != null)
                        {
                            attributes.Add(scaleProperties.Name);
                        }
                    }
                    Dictionary<string, string> scaleDictionary = new Dictionary<string, string>();
                    foreach (var item in attributes)
                    {
                        if (item == vDSModel.TargetIdentifier)
                        {
                            scaleDictionary.Add(item, "Nominal Dimension");
                        }
                        else
                        {
                            foreach (var DTscale in serializedData["Feature Name"][item]["Datatype"].Children())
                            {
                                var DTproperty = DTscale as JProperty;
                                if (DTproperty.Value.ToString() == "True")
                                {
                                    if (DTproperty.Name == "Id")
                                        scaleDictionary.Add(item, "Nominal Dimension");
                                    //Defect- 837217 & User Story 962493 for VDS : needed "Text dimension" for text datatype
                                    else if (DTproperty.Name == "Text")
                                        scaleDictionary.Add(item, "Text");
                                    else if (DTproperty.Name == "category")
                                    {
                                        foreach (var scale in serializedData["Feature Name"][item]["Scale"].Children())
                                        {
                                            var property = scale as JProperty;
                                            if (property.Value.ToString() == "True")
                                            {
                                                scaleDictionary.Add(item, property.Name + " Dimension");
                                            }
                                        }
                                    }
                                    else if (DTproperty.Name == "datetime64[ns]")
                                        scaleDictionary.Add(item, "Date Dimension");
                                    else if (DTproperty.Name == "float64" || DTproperty.Name == "int64")
                                        scaleDictionary.Add(item, "Measure");
                                }
                            }
                        }
                    }
                    vDSModel.DataRoleScale = scaleDictionary;
                    //For Add Feature(DataCuration) -- Get all the Features created through Add Feature.
                    if (serializedData.ContainsKey(CONSTANTS.NewAddFeatures))
                    {
                        foreach (var addFeature in serializedData[CONSTANTS.NewAddFeatures].Children())
                        {
                            JProperty prop = addFeature as JProperty;
                            if (prop.Name != customTarget)
                                addFeatureColumns.Add(prop.Name);
                        }
                    }
                }

                var filteredDataProjection = Builders<BsonDocument>.Projection.Include("ColumnUniqueValues").
                    Include("target_variable").Include("DateFormats").Include("types").Include("inputcols").Exclude("_id");
                var filteredDataProjection1 = Builders<BsonDocument>.Projection.Include("ColumnUniqueValues").Exclude("_id");
                var filteredDataResult1 = collection_FilteredData.Find(filter).Project<BsonDocument>(filteredDataProjection1).ToList();
                var filteredDataResult = collection_FilteredData.Find(filter).Project<BsonDocument>(filteredDataProjection).ToList();
                JObject serializedValuesData = null;
                JObject serializedValuesData1 = null;
                JObject serializedDateFormat = null;
                if (filteredDataResult.Count > 0)
                {
                    bool DBEncryptionRequired = CommonUtility.EncryptDB(correlationId, appSettings);
                    if (DBEncryptionRequired)
                    {
                        filteredDataResult[0]["ColumnUniqueValues"] = BsonDocument.Parse(_encryptionDecryption.Decrypt(filteredDataResult[0]["ColumnUniqueValues"].AsString));
                        //if (!string.IsNullOrEmpty(Convert.ToString(filteredDataResult[0]["CreatedByUser"])))
                        //    filteredDataResult[0]["CreatedByUser"] = _encryptionDecryption.Decrypt(filteredDataResult[0]["CreatedByUser"].AsString);
                        //if (!string.IsNullOrEmpty(Convert.ToString(filteredDataResult[0]["ModifiedByUser"])))
                        //    filteredDataResult[0]["ModifiedByUser"] = _encryptionDecryption.Decrypt(filteredDataResult[0]["ModifiedByUser"].AsString);
                    }
                    vDSModel.TargetName = filteredDataResult[0]["target_variable"].ToString();
                    serializedDateFormat = JObject.Parse(filteredDataResult[0]["DateFormats"].ToString());
                    vDSModel.DataType = JObject.Parse(filteredDataResult[0]["types"].ToString());
                    //For Add Feature(DataCuration) --- Except "Custom Target" restrict data type for other Features.
                    if (addFeatureColumns.Count > 0)
                    {
                        JObject dataTypes = new JObject();
                        foreach (var parent in vDSModel.DataType.Children())
                        {
                            JProperty prop = parent as JProperty;
                            if (!addFeatureColumns.Contains(prop.Name))
                                dataTypes.Add(parent);
                        }

                        vDSModel.DataType = dataTypes;
                    }
                    vDSModel.UnitDateFormat = serializedDateFormat;
                    serializedValuesData = JObject.Parse(filteredDataResult[0].ToString());
                    if (DBEncryptionRequired)
                    {
                        filteredDataResult1[0]["ColumnUniqueValues"] = BsonDocument.Parse(_encryptionDecryption.Decrypt(filteredDataResult1[0]["ColumnUniqueValues"].AsString));
                        //if (!string.IsNullOrEmpty(Convert.ToString(filteredDataResult1[0]["CreatedByUser"])))
                        //    filteredDataResult1[0]["CreatedByUser"] = _encryptionDecryption.Decrypt(filteredDataResult1[0]["CreatedByUser"].AsString);
                        //if (!string.IsNullOrEmpty(Convert.ToString(filteredDataResult1[0]["ModifiedByUser"])))
                        //    filteredDataResult1[0]["ModifiedByUser"] =_encryptionDecryption.Decrypt(filteredDataResult1[0]["ModifiedByUser"].AsString);
                    }
                    serializedValuesData1 = JObject.Parse(filteredDataResult1[0]["ColumnUniqueValues"].ToString());
                    List<string> validColumns = new List<string>();
                    IList<string> originalCols = vDSModel.DataType.Properties().Select(p => p.Name).ToList();
                    //foreach(var col in originalCols)
                    //{
                    //    if (!vDSModel.DataRoleScale.ContainsKey(col))
                    //    {
                    //        vDSModel.DataType.Remove(col);
                    //        serializedValuesData1.Remove(col);
                    //        if (vDSModel.UnitDateFormat.ContainsKey(col))
                    //        {
                    //            vDSModel.UnitDateFormat.Remove(col);
                    //        }
                    //    }

                    //}
                    foreach (var columns in serializedValuesData["types"].Children())
                    {
                        JProperty column = columns as JProperty;
                        if (column.Value.ToString() == "float64" || column.Value.ToString() == "int64" || column.Value.ToString() == "datetime64[ns]")
                        {
                            validColumns.Add(column.Name);
                        }

                    }

                    foreach (var values in validColumns)
                    {
                        JObject header2 = (JObject)serializedValuesData.SelectToken("ColumnUniqueValues");
                        header2.Property(values).Remove();
                    }
                    vDSModel.ValidValues = serializedValuesData1;
                    List<string> inputcols = new List<string>();
                    foreach (var item in serializedValuesData["inputcols"].Children())
                    {
                        inputcols.Add(item.ToString());
                    }
                    foreach (var type in serializedValuesData["types"].Children())
                    {
                        JProperty column = type as JProperty;
                        if (!inputcols.Contains(column.Name))
                        {
                            vDSModel.ValidValues.Remove(column.Name);
                            vDSModel.DataType.Remove(column.Name);
                        }
                    }
                    vDSModel.ValidValues = serializedValuesData1;
                }
            }
            return vDSModel;
        }

        public VDSViewModelDTO GetVDSModels(string clientUID, string deliveryConstructUID, string modelType)
        {
            VDSViewModelDTO data = new VDSViewModelDTO();
            var dbCollection = _database.GetCollection<BsonDocument>("SSAI_DeployedModels");
            var filterBuilder = Builders<BsonDocument>.Filter;
            FilterDefinition<BsonDocument> modelTypeFilter = modelType == Classification ? (filterBuilder.Eq("ModelType", modelType) | filterBuilder.Eq("ModelType", MultiClass)) :
               filterBuilder.Eq("ModelType", modelType);
            var filterScenario = filterBuilder.Eq("ClientUId", clientUID) & filterBuilder.Eq("DeliveryConstructUID", deliveryConstructUID) & modelTypeFilter & filterBuilder.Eq(CONSTANTS.Status, CONSTANTS.Deployed) & filterBuilder.Eq(CONSTANTS.IsModelTemplate, false) & (filterBuilder.AnyEq("LinkedApps", "Virtual Data Scientist(VDS)") | filterBuilder.AnyEq("LinkedApps", CONSTANTS.Virtual_Data_Scientist) | filterBuilder.AnyEq("LinkedApps", CONSTANTS.VDS_SI)
            | filterBuilder.AnyEq("LinkedApps", CONSTANTS.VDS_AIOPS));
            var projectionScenario = Builders<BsonDocument>.Projection.Include("CorrelationId").Include("ModelVersion").Include("ClientUId").Include("DeliveryConstructUID").Include("ModelType").Include("ModelName").Include("CreatedOn").Include("ModifiedOn").Exclude("_id");
            var dbData = dbCollection.Find(filterScenario).Project<BsonDocument>(projectionScenario).ToList();

            if (dbData.Count > 0)
            {
                ModelDetails modelDetail;
                data.ModelDetails = new List<ModelDetails>();
                for (int i = 0; i < dbData.Count; i++)
                {
                    modelDetail = new ModelDetails();
                    modelDetail.CorrelationId = dbData[i]["CorrelationId"].ToString();
                    modelDetail.DUID = dbData[i]["DeliveryConstructUID"].ToString();
                    string dbModelType = dbData[i]["ModelType"].ToString();
                    modelDetail.ModelType = dbModelType == MultiClass ? Classification : dbModelType;
                    modelDetail.ModelName = dbData[i]["ModelName"].ToString();
                    modelDetail.CreatedDateTime = dbData[i]["CreatedOn"].ToString();
                    modelDetail.LastModifiedDateTime = dbData[i]["ModifiedOn"].ToString();

                    var collection_TrainedModels = _database.GetCollection<BsonDocument>("SSAI_RecommendedTrainedModels");
                    var projection = Builders<BsonDocument>.Projection.Include("LastTrainDateTime").Exclude("_id");
                    var filterBuilderTM = Builders<BsonDocument>.Filter;
                    var filterTM = filterBuilderTM.Eq("CorrelationId", modelDetail.CorrelationId) & filterBuilderTM.Eq("modelName", dbData[i]["ModelVersion"].ToString());
                    var TrainedModelsResult = collection_TrainedModels.Find(filterTM).Project<BsonDocument>(projection).ToList();
                    if (TrainedModelsResult.Count > 0)
                    {
                        if (TrainedModelsResult[0].Contains("LastTrainDateTime"))
                            modelDetail.LastTrainDateTime = TrainedModelsResult[0]["LastTrainDateTime"].ToString();
                    }

                    data.ModelDetails.Add(modelDetail);
                }

                data.CliendUID = dbData[0]["ClientUId"].ToString();
            }
            return data;
        }
        public VDSViewModelDTO GetVDSManagedInstanceModels(string clientUID, string deliveryConstructUID, string modelType, string environment, string requestType)
        {
            VDSViewModelDTO data = new VDSViewModelDTO();
            var dbCollection = _database.GetCollection<BsonDocument>("SSAI_DeployedModels");
            var filterBuilder = Builders<BsonDocument>.Filter;
            FilterDefinition<BsonDocument> modelTypeFilter = modelType == Classification ? (filterBuilder.Eq("ModelType", modelType) | filterBuilder.Eq("ModelType", MultiClass)) :
               filterBuilder.Eq("ModelType", modelType);
            var filterScenario = filterBuilder.Eq("ClientUId", clientUID) & filterBuilder.Eq("DeliveryConstructUID", deliveryConstructUID) & modelTypeFilter & filterBuilder.Eq(CONSTANTS.Status, CONSTANTS.Deployed) & filterBuilder.Eq(CONSTANTS.IsModelTemplate, false) & (filterBuilder.AnyEq("LinkedApps", appSettings.Value.ApplicationName) | filterBuilder.AnyEq("LinkedApps", "myWizard Managed Instance")) & filterBuilder.Eq(CONSTANTS.Category, requestType) & filterBuilder.Eq("SourceName", "file");
            var projectionScenario = Builders<BsonDocument>.Projection.Include("CorrelationId").Include("ModelVersion").Include("ClientUId").Include("DeliveryConstructUID").Include("ModelType").Include("ModelName").Include("CreatedOn").Include("ModifiedOn").Exclude("_id");
            var dbData = dbCollection.Find(filterScenario).Project<BsonDocument>(projectionScenario).ToList();

            if (dbData.Count > 0)
            {
                ModelDetails modelDetail;
                data.ModelDetails = new List<ModelDetails>();
                for (int i = 0; i < dbData.Count; i++)
                {
                    modelDetail = new ModelDetails();
                    modelDetail.CorrelationId = dbData[i]["CorrelationId"].ToString();
                    modelDetail.DUID = dbData[i]["DeliveryConstructUID"].ToString();
                    string dbModelType = dbData[i]["ModelType"].ToString();
                    modelDetail.ModelType = dbModelType == MultiClass ? Classification : dbModelType;
                    modelDetail.ModelName = dbData[i]["ModelName"].ToString();
                    modelDetail.CreatedDateTime = dbData[i]["CreatedOn"].ToString();
                    modelDetail.LastModifiedDateTime = dbData[i]["ModifiedOn"].ToString();

                    var collection_TrainedModels = _database.GetCollection<BsonDocument>("SSAI_RecommendedTrainedModels");
                    var projection = Builders<BsonDocument>.Projection.Include("LastTrainDateTime").Exclude("_id");
                    var filterBuilderTM = Builders<BsonDocument>.Filter;
                    var filterTM = filterBuilderTM.Eq("CorrelationId", modelDetail.CorrelationId) & filterBuilderTM.Eq("modelName", dbData[i]["ModelVersion"].ToString());
                    var TrainedModelsResult = collection_TrainedModels.Find(filterTM).Project<BsonDocument>(projection).ToList();
                    if (TrainedModelsResult.Count > 0)
                    {
                        if (TrainedModelsResult[0].Contains("LastTrainDateTime"))
                            modelDetail.LastTrainDateTime = TrainedModelsResult[0]["LastTrainDateTime"].ToString();
                    }

                    data.ModelDetails.Add(modelDetail);
                }

                data.CliendUID = dbData[0]["ClientUId"].ToString();
            }
            return data;
        }


        public VdsUseCaseDto VDSUseCaseDetails(string usecaseId)
        {
            VdsUseCaseDto vDSUseCase = new VdsUseCaseDto();
            var filter = Builders<BsonDocument>.Filter.Eq("CorrelationId", usecaseId);

            var collection_IngestData = _database.GetCollection<BsonDocument>("PS_IngestedData");
            var collection_DataCleanup = _database.GetCollection<BsonDocument>("DE_DataCleanup");
            var collection_FilteredData = _database.GetCollection<BsonDocument>("DataCleanUP_FilteredData");
            var collection_DeployedModels = _database.GetCollection<BsonDocument>("SSAI_DeployedModels");
            var collection_BusinessProblem = _database.GetCollection<BsonDocument>("PS_BusinessProblem");

            var projection = Builders<BsonDocument>.Projection.Include("DataType").Include("SourceDetails").Exclude("_id");
            var ingestCollectionResult = collection_IngestData.Find(filter).Project<BsonDocument>(projection).ToList();
            if (ingestCollectionResult.Count > 0)
            {
                vDSUseCase.EntityName = GetDefaultEntityName(usecaseId);

                var DeployedModelsProjection = Builders<BsonDocument>.Projection.Include("ModelType").Include("ModelName").Include("ModelURL").Include("DeliveryConstructUID").Include("ClientUId").Include("CorrelationId").Include("CreatedOn").
                     Include("ModifiedOn").Include("AppId").Include("CreatedByUser").Include("InputSample").Include("Category").Include("IsPrivate").Include("IsModelTemplate").Exclude("_id");
                var DeployedModelsResult = collection_DeployedModels.Find(filter).Project<BsonDocument>(DeployedModelsProjection).ToList();
                if (DeployedModelsResult.Count > 0)
                {
                    //Asset Usage
                    auditTrailLog.CorrelationId = DeployedModelsResult[0]["CorrelationId"].ToString();
                    if (DeployedModelsResult[0]["ClientUId"].ToString() != "BsonNull")
                    {
                        auditTrailLog.ClientId = DeployedModelsResult[0]["ClientUId"].ToString();
                    }
                    if (DeployedModelsResult[0]["DeliveryConstructUID"].ToString() != "BsonNull")
                    {
                        auditTrailLog.DCID = DeployedModelsResult[0]["DeliveryConstructUID"].ToString();
                    }
                    if (DeployedModelsResult[0]["CreatedByUser"].ToString() != "BsonNull")
                    {
                        auditTrailLog.CreatedBy = DeployedModelsResult[0]["CreatedByUser"].ToString();
                    }
                    auditTrailLog.FeatureName = CONSTANTS.AutomatedWorkFlow;
                    auditTrailLog.ProcessName = CONSTANTS.TrainingName;
                    if (DeployedModelsResult[0]["AppId"].ToString() != "BsonNull")
                    {
                        auditTrailLog.ApplicationID = DeployedModelsResult[0]["AppId"].ToString();
                    }
                    auditTrailLog.UsageType = CONSTANTS.AssetUsage;
                    CommonUtility.AuditTrailLog(auditTrailLog, appSettings);

                    vDSUseCase.UseCaseId = DeployedModelsResult[0]["CorrelationId"].ToString();
                    if (DeployedModelsResult[0]["ModelType"].ToString() != "BsonNull")
                    {
                        string dbModelType = DeployedModelsResult[0]["ModelType"].ToString();
                        vDSUseCase.ModelType = dbModelType == MultiClass ? Classification : DeployedModelsResult[0]["ModelType"].ToString();
                    }
                    if (DeployedModelsResult[0]["ModelName"].ToString() != "BsonNull")
                        vDSUseCase.UseCaseName = DeployedModelsResult[0]["ModelName"].ToString();
                    if (DeployedModelsResult[0]["ModelURL"].ToString() != "BsonNull")
                        vDSUseCase.WebServiceURL = DeployedModelsResult[0]["ModelURL"].ToString();

                    vDSUseCase.FunctionalArea = DeployedModelsResult[0]["Category"].ToString();
                    if (vDSUseCase.FunctionalArea == "Others")
                        vDSUseCase.FunctionalArea = "General";
                    if (vDSUseCase.FunctionalArea == "PPM")
                        vDSUseCase.FunctionalArea = "RIAD";
                    vDSUseCase.CreatedDateTime = DeployedModelsResult[0]["CreatedOn"].ToString();
                    vDSUseCase.LastModifiedDateTime = DeployedModelsResult[0]["ModifiedOn"].ToString();
                    vDSUseCase.ProblemType = DeployedModelsResult[0]["ModelType"].ToString();
                    if (vDSUseCase.ProblemType == "Multi_Class")
                        vDSUseCase.ProblemType = "Classification";
                    bool isPrivate = DeployedModelsResult[0]["IsPrivate"].AsBoolean;
                    bool isModelTemplate = DeployedModelsResult[0]["IsModelTemplate"].AsBoolean;
                    if (isModelTemplate)
                        vDSUseCase.ModelType = "ModelTemplate";
                    else if (isPrivate)
                        vDSUseCase.ModelType = "Private";
                    else
                        vDSUseCase.ModelType = "Public";
                    if (vDSUseCase.ModelType != "ModelTemplate")
                    {
                        vDSUseCase.ClientUId = DeployedModelsResult[0]["ClientUId"].ToString();
                        vDSUseCase.DeliveryConstructUId = DeployedModelsResult[0]["DeliveryConstructUID"].ToString();
                    }

                }
                var BusinessProbDataProjection = Builders<BsonDocument>.Projection.Exclude("_id");
                var BusinessProbDataResult = collection_BusinessProblem.Find(filter).Project<BsonDocument>(BusinessProbDataProjection).ToList();
                string customTarget = null;
                if (BusinessProbDataResult.Count > 0)
                {
                    vDSUseCase.TargetIdentifier = BusinessProbDataResult[0]["TargetUniqueIdentifier"].ToString();
                    if (vDSUseCase.ModelType == "TimeSeries")
                    {
                        vDSUseCase.DateColumn = Convert.ToString(BusinessProbDataResult[0]["TimeSeries"]["TimeSeriesColumn"]);
                        vDSUseCase.TargetAggregate = Convert.ToString(BusinessProbDataResult[0]["TimeSeries"]["Aggregation"]);
                        vDSUseCase.Frequency.Add(DeployedModelsResult[0]["Frequency"].ToString());
                        foreach (var model in DeployedModelsResult)
                        {
                            vDSUseCase.Frequency.Add(model["Frequency"].ToString());
                        }
                    }
                    vDSUseCase.Description = BusinessProbDataResult[0]["BusinessProblems"].ToString();
                }
                var dataCleanupProjection = Builders<BsonDocument>.Projection.Include("Feature Name").Include(CONSTANTS.NewFeatureName).Include(CONSTANTS.NewAddFeatures).Exclude("_id");
                var dataCleanupResult = collection_DataCleanup.Find(filter).Project<BsonDocument>(dataCleanupProjection).ToList();
                JObject serializedData = null;
                List<string> attributes = new List<string>();
                List<string> addFeatureColumns = new List<string>();
                if (dataCleanupResult.Count > 0)
                {
                    //decrypt db data
                    bool DBEncryptionRequired = CommonUtility.EncryptDB(usecaseId, appSettings);
                    if (DBEncryptionRequired)
                    {
                        //if (!string.IsNullOrEmpty(Convert.ToString(dataCleanupResult[0][CONSTANTS.CreatedByUser])))
                        //{
                        //    dataCleanupResult[0][CONSTANTS.CreatedByUser] = _encryptionDecryption.Decrypt(Convert.ToString(dataCleanupResult[0][CONSTANTS.CreatedByUser]));
                        //}
                        //if (!string.IsNullOrEmpty(Convert.ToString(dataCleanupResult[0][CONSTANTS.ModifiedByUser])))
                        //{
                        //    dataCleanupResult[0][CONSTANTS.ModifiedByUser] = _encryptionDecryption.Decrypt(Convert.ToString(dataCleanupResult[0][CONSTANTS.ModifiedByUser]));
                        //}
                        dataCleanupResult[0][CONSTANTS.FeatureName] = BsonDocument.Parse(_encryptionDecryption.Decrypt(dataCleanupResult[0][CONSTANTS.FeatureName].AsString));
                        if (dataCleanupResult[0].Contains(CONSTANTS.NewFeatureName))
                        {
                            if (dataCleanupResult[0][CONSTANTS.NewFeatureName].ToString() != "{ }")
                                dataCleanupResult[0][CONSTANTS.NewFeatureName] = BsonDocument.Parse(_encryptionDecryption.Decrypt(dataCleanupResult[0][CONSTANTS.NewFeatureName].AsString));
                        }
                    }
                    //For Add Feature(DataCuration) --- Only merging of "Feature Name & Feature Name_New Feature" should happen if custom target is selected
                    if (!string.IsNullOrEmpty(customTarget))
                    {
                        JObject datas = JObject.Parse(dataCleanupResult[0].ToString());
                        JObject combinedFeatures = new JObject();
                        combinedFeatures = this.CombinedFeatures(datas, customTarget);
                        if (combinedFeatures != null)
                            dataCleanupResult[0][CONSTANTS.FeatureName] = BsonDocument.Parse(combinedFeatures.ToString());
                    }

                    serializedData = JObject.Parse(dataCleanupResult[0].ToString());
                    foreach (var scales in serializedData["Feature Name"].Children())
                    {
                        var scaleProperties = scales as JProperty;
                        if (scaleProperties != null)
                        {
                            attributes.Add(scaleProperties.Name);
                        }
                    }
                    Dictionary<string, string> scaleDictionary = new Dictionary<string, string>();
                    foreach (var item in attributes)
                    {
                        if (item == vDSUseCase.TargetIdentifier)
                        {
                            scaleDictionary.Add(item, "Nominal Dimension");
                        }
                        else
                        {
                            foreach (var DTscale in serializedData["Feature Name"][item]["Datatype"].Children())
                            {
                                var DTproperty = DTscale as JProperty;
                                if (DTproperty.Value.ToString() == "True")
                                {
                                    if (DTproperty.Name == "Id")
                                        scaleDictionary.Add(item, "Nominal Dimension");
                                    else if (DTproperty.Name == "Text")
                                        scaleDictionary.Add(item, "Text");
                                    else if (DTproperty.Name == "category")
                                    {
                                        foreach (var scale in serializedData["Feature Name"][item]["Scale"].Children())
                                        {
                                            var property = scale as JProperty;
                                            if (property.Value.ToString() == "True")
                                            {
                                                scaleDictionary.Add(item, property.Name + " Dimension");
                                            }
                                        }
                                    }
                                    else if (DTproperty.Name == "datetime64[ns]")
                                        scaleDictionary.Add(item, "Date Dimension");
                                    else if (DTproperty.Name == "float64" || DTproperty.Name == "int64")
                                        scaleDictionary.Add(item, "Measure");
                                }
                            }
                        }
                    }
                    vDSUseCase.DataRoleScale = scaleDictionary;
                    //For Add Feature(DataCuration) -- Get all the Features created through Add Feature.
                    if (serializedData.ContainsKey(CONSTANTS.NewAddFeatures))
                    {
                        foreach (var addFeature in serializedData[CONSTANTS.NewAddFeatures].Children())
                        {
                            JProperty prop = addFeature as JProperty;
                            if (prop.Name != customTarget)
                                addFeatureColumns.Add(prop.Name);
                        }
                    }
                }

                var filteredDataProjection = Builders<BsonDocument>.Projection.Include("ColumnUniqueValues").
                    Include("target_variable").Include("DateFormats").Include("types").Include("inputcols").Exclude("_id");
                var filteredDataProjection1 = Builders<BsonDocument>.Projection.Include("ColumnUniqueValues").Exclude("_id");
                var filteredDataResult1 = collection_FilteredData.Find(filter).Project<BsonDocument>(filteredDataProjection1).ToList();
                var filteredDataResult = collection_FilteredData.Find(filter).Project<BsonDocument>(filteredDataProjection).ToList();
                JObject serializedValuesData = null;
                JObject serializedValuesData1 = null;
                JObject serializedDateFormat = null;
                if (filteredDataResult.Count > 0)
                {
                    bool DBEncryptionRequired = CommonUtility.EncryptDB(usecaseId, appSettings);
                    if (DBEncryptionRequired)
                    {
                        filteredDataResult[0]["ColumnUniqueValues"] = BsonDocument.Parse(_encryptionDecryption.Decrypt(filteredDataResult[0]["ColumnUniqueValues"].AsString));
                        //if (!string.IsNullOrEmpty(Convert.ToString(filteredDataResult[0]["CreatedByUser"])))
                        //    filteredDataResult[0]["CreatedByUser"] = _encryptionDecryption.Decrypt(filteredDataResult[0]["CreatedByUser"].AsString);
                        //if (!string.IsNullOrEmpty(Convert.ToString(filteredDataResult[0]["ModifiedByUser"])))
                        //    filteredDataResult[0]["ModifiedByUser"] = _encryptionDecryption.Decrypt(filteredDataResult[0]["ModifiedByUser"].AsString);
                    }
                    vDSUseCase.TargetName = filteredDataResult[0]["target_variable"].ToString();
                    serializedDateFormat = JObject.Parse(filteredDataResult[0]["DateFormats"].ToString());
                    vDSUseCase.DataType = JObject.Parse(filteredDataResult[0]["types"].ToString());
                    //For Add Feature(DataCuration) --- Except "Custom Target" restrict data type for other Features.
                    if (addFeatureColumns.Count > 0)
                    {
                        JObject dataTypes = new JObject();
                        foreach (var parent in vDSUseCase.DataType.Children())
                        {
                            JProperty prop = parent as JProperty;
                            if (!addFeatureColumns.Contains(prop.Name))
                                dataTypes.Add(parent);
                        }

                        vDSUseCase.DataType = dataTypes;
                    }
                    vDSUseCase.UnitDateFormat = serializedDateFormat;
                    serializedValuesData = JObject.Parse(filteredDataResult[0].ToString());
                    if (DBEncryptionRequired)
                        filteredDataResult1[0]["ColumnUniqueValues"] = BsonDocument.Parse(_encryptionDecryption.Decrypt(filteredDataResult1[0]["ColumnUniqueValues"].AsString));
                    serializedValuesData1 = JObject.Parse(filteredDataResult1[0]["ColumnUniqueValues"].ToString());
                    List<string> validColumns = new List<string>();
                    IList<string> originalCols = vDSUseCase.DataType.Properties().Select(p => p.Name).ToList();
                    //foreach (var col in originalCols)
                    //{
                    //    if (!vDSUseCase.DataRoleScale.ContainsKey(col))
                    //    {
                    //        vDSUseCase.DataType.Remove(col);
                    //        serializedValuesData1.Remove(col);
                    //        if (vDSUseCase.UnitDateFormat.ContainsKey(col))
                    //        {
                    //            vDSUseCase.UnitDateFormat.Remove(col);
                    //        }
                    //    }

                    //}
                    foreach (var columns in serializedValuesData["types"].Children())
                    {
                        JProperty column = columns as JProperty;
                        if (column.Value.ToString() == "float64" || column.Value.ToString() == "int64" || column.Value.ToString() == "datetime64[ns]")
                        {
                            validColumns.Add(column.Name);
                        }

                    }

                    foreach (var values in validColumns)
                    {
                        JObject header2 = (JObject)serializedValuesData.SelectToken("ColumnUniqueValues");
                        if (header2.ContainsKey(values))
                            header2.Property(values).Remove();
                    }
                    vDSUseCase.ValidValues = serializedValuesData1;
                    List<string> inputcols = new List<string>();
                    foreach (var item in serializedValuesData["inputcols"].Children())
                    {
                        inputcols.Add(item.ToString());
                    }
                    foreach (var type in serializedValuesData["types"].Children())
                    {
                        JProperty column = type as JProperty;
                        if (!inputcols.Contains(column.Name))
                        {
                            vDSUseCase.ValidValues.Remove(column.Name);
                            vDSUseCase.DataType.Remove(column.Name);
                        }
                    }
                    vDSUseCase.ValidValues = serializedValuesData1;

                    //remove not trained cols
                    string inputSample = DeployedModelsResult[0]["InputSample"].AsString;
                    if (DBEncryptionRequired)
                        inputSample = _encryptionDecryption.Decrypt(inputSample);
                    JArray inptArr = JArray.Parse(inputSample);
                    JObject inptObj = inptArr[0] as JObject;
                    List<string> inputKeys = inptObj.Properties().Select(x => x.Name).ToList();
                    IList<string> dataTypeCol = vDSUseCase.DataType.Properties().Select(p => p.Name).ToList();
                    IList<string> dataRoleCol = vDSUseCase.DataRoleScale.Keys.ToList();
                    IList<string> validValuesCol = vDSUseCase.ValidValues.Properties().Select(p => p.Name).ToList();
                    IList<string> dateCol = vDSUseCase.UnitDateFormat.Properties().Select(p => p.Name).ToList();
                    if (!inputKeys.Contains(vDSUseCase.TargetIdentifier))
                        inputKeys.Add(vDSUseCase.TargetIdentifier);
                    if (!inputKeys.Contains(vDSUseCase.TargetName))
                        inputKeys.Add(vDSUseCase.TargetName);
                    foreach (var col in dataTypeCol)
                    {
                        if (!inputKeys.Contains(col))
                        {
                            vDSUseCase.DataType.Remove(col);
                        }
                    }
                    foreach (var col in dataRoleCol)
                    {
                        if (!inputKeys.Contains(col))
                        {
                            vDSUseCase.DataRoleScale.Remove(col);
                        }
                    }
                    foreach (var col in validValuesCol)
                    {
                        if (!inputKeys.Contains(col))
                        {
                            vDSUseCase.ValidValues.Remove(col);
                        }
                    }
                    foreach (var col in dateCol)
                    {
                        if (!inputKeys.Contains(col))
                        {
                            vDSUseCase.UnitDateFormat.Remove(col);
                        }
                    }
                }
            }
            return vDSUseCase;
        }

        public UseCasePredictionRequestOutput GetUseCasePredictionRequest(UseCasePredictionRequestInput useCasePredictionRequestInput)
        {
            bool DBEncryptionRequired = CommonUtility.EncryptDB(useCasePredictionRequestInput.UseCaseId, appSettings);
            UseCasePredictionRequestOutput useCasePredictionRequestOutput = new UseCasePredictionRequestOutput();
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(VDSDataService), nameof(GetUseCasePredictionRequest), "UsecasePredictionRequest service Started", string.Empty, string.Empty, useCasePredictionRequestInput.ClientUId, useCasePredictionRequestInput.DeliveryConstructUId);
            var modelCollection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
            DeployModelsDto deployedModel = null;
            if (useCasePredictionRequestInput.ModelType == "Public" || useCasePredictionRequestInput.ModelType == "Private")
            {
                var filter = Builders<DeployModelsDto>.Filter.Where(x => x.CorrelationId == useCasePredictionRequestInput.UseCaseId)
                             & Builders<DeployModelsDto>.Filter.Where(x => x.Status == "Deployed");
                var projection = Builders<DeployModelsDto>.Projection.Exclude("_id");
                deployedModel = modelCollection.Find(filter).Project<DeployModelsDto>(projection).FirstOrDefault();
            }
            else
            {
                string encryptedUser = "SYSTEM";
                if (!string.IsNullOrEmpty(Convert.ToString(encryptedUser)))
                {
                    encryptedUser = _encryptionDecryption.Encrypt(Convert.ToString(encryptedUser));
                }
                var builder = Builders<DeployModelsDto>.Filter;
                var filter2 = builder.Where(x => x.ClientUId == useCasePredictionRequestInput.ClientUId)
                              & builder.Where(x => x.DeliveryConstructUID == useCasePredictionRequestInput.DeliveryConstructUId)
                              & builder.Where(x => x.TemplateUsecaseId == useCasePredictionRequestInput.UseCaseId)
                              & builder.Where(x => (x.CreatedByUser == "SYSTEM" || x.CreatedByUser == encryptedUser))
                              & builder.Where(x => x.Status == "Deployed");
                var Projection1 = Builders<DeployModelsDto>.Projection.Exclude("_id");
                deployedModel = modelCollection.Find(filter2).Project<DeployModelsDto>(Projection1).FirstOrDefault();

            }

            if (deployedModel != null)
            {
                //Asset Usage
                auditTrailLog.ClientId = deployedModel.ClientUId;
                auditTrailLog.DCID = deployedModel.DeliveryConstructUID;
                auditTrailLog.ApplicationID = deployedModel.AppId;
                auditTrailLog.FeatureName = CONSTANTS.AutomatedWorkFlow;
                auditTrailLog.ProcessName = CONSTANTS.PredictionName;
                auditTrailLog.UsageType = CONSTANTS.AssetUsage;
                CommonUtility.AuditTrailLog(auditTrailLog, appSettings);

                useCasePredictionRequestOutput.UseCaseId = useCasePredictionRequestInput.UseCaseId;

                //adding phoenix condition
                if (deployedModel.SourceName == "pad" || deployedModel.SourceName == "metric" || deployedModel.SourceName == "multidatasource")
                {
                    var ingest_collection = _database.GetCollection<BsonDocument>(CONSTANTS.PS_IngestedData);
                    var ingest_filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, deployedModel.CorrelationId);
                    var ingest_Projection = Builders<BsonDocument>.Projection.Include("lastDateDict").Exclude("_id");
                    var Ingestdata_result = ingest_collection.Find(ingest_filter).Project<BsonDocument>(ingest_Projection).ToList();
                    if (Ingestdata_result.Count > 0)
                    {
                        IngrainRequestQueue requestQueue = new IngrainRequestQueue();
                        requestQueue = GetFileRequestStatus(deployedModel.CorrelationId, CONSTANTS.IngestData);

                        if (requestQueue != null)
                        {
                            JObject paramArgs = JObject.Parse(requestQueue.ParamArgs);
                            paramArgs[CONSTANTS.Flag] = "Incremental";
                            if (paramArgs["metric"].ToString() != "null")
                            {
                                JObject metric = JObject.Parse(paramArgs["metric"].ToString());
                                //if (Ingestdata_result[0].Contains("lastDateDict")) {
                                //    DateTime lstDicDate = DateTime.Parse(Ingestdata_result[0]["lastDateDict"][0].ToString());
                                //    metric["startDate"] = lstDicDate.AddDays(-30).ToString("MM/dd/yyyy");
                                //}
                                //DateTime lstDicDate = DateTime.Parse(deployedModel.ModifiedOn);
                                DateTime lstDicDate = DateTime.Parse(deployedModel.ModifiedOn);
                                metric["startDate"] = lstDicDate.AddDays(-30).ToString(CONSTANTS.DateHoursFormat);

                                paramArgs["metric"] = JsonConvert.SerializeObject(metric, Formatting.None);
                            }
                            else if (paramArgs["pad"].ToString() != "null")
                            {
                                JObject pad = JObject.Parse(paramArgs["pad"].ToString());
                                //if (Ingestdata_result[0].Contains("lastDateDict"))
                                //{
                                //    DateTime lstDicDate = DateTime.Parse(Ingestdata_result[0]["lastDateDict"][0][0].ToString());
                                //    pad["startDate"] = lstDicDate.AddDays(-30).ToString("MM/dd/yyyy");
                                //}
                                DateTime lstDicDate = DateTime.Parse(deployedModel.ModifiedOn);
                                pad["startDate"] = lstDicDate.AddDays(-30).ToString(CONSTANTS.DateHoursFormat);
                                paramArgs["pad"] = JsonConvert.SerializeObject(pad, Formatting.None);
                            }

                            requestQueue.ParamArgs = paramArgs.ToString(Formatting.None);
                            requestQueue._id = Guid.NewGuid().ToString();
                            requestQueue.RequestId = Guid.NewGuid().ToString();
                            requestQueue.UniId = Guid.NewGuid().ToString();
                            requestQueue.RequestStatus = CONSTANTS.New;
                            requestQueue.Status = CONSTANTS.Null;
                            requestQueue.Progress = CONSTANTS.Null;
                            requestQueue.Message = CONSTANTS.Null;
                            requestQueue.RetryCount = 0;
                            requestQueue.CreatedOn = DateTime.UtcNow.ToString(CONSTANTS.DateHoursFormat);
                            requestQueue.ModifiedOn = DateTime.UtcNow.ToString(CONSTANTS.DateHoursFormat);
                            requestQueue.IsForAutoTrain = false; //Should be false for Prediction avoiding single flow (arch change)
                            string requestId = requestQueue.RequestId;
                            if (deployedModel.ModelType == CONSTANTS.TimeSeries)
                            {
                                requestQueue.Frequency = useCasePredictionRequestInput.Frequency;
                            }
                            else
                            {
                                requestQueue.Frequency = CONSTANTS.Null;
                            }
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(VDSDataService), nameof(GetUseCasePredictionRequest), "requestQueue.UniId" + requestQueue.UniId, string.Empty, string.Empty, useCasePredictionRequestInput.ClientUId, useCasePredictionRequestInput.DeliveryConstructUId);
                            InsertRequests(requestQueue);
                            bool flag = false;

                            Task.Run(() => InsertPublishModelRequest(deployedModel, requestQueue.UniId));
                            //Task.Run(() => InsertPublishModelRequest(deployedModel, "1e6aed81-0165-44ae-b409-5e62b5ddc269"));
                            useCasePredictionRequestOutput.UniqueId = requestQueue.UniId;
                            useCasePredictionRequestOutput.Status = "I";
                            useCasePredictionRequestOutput.StatusMessage = "Prediction request initiated successfully";

                        }
                    }
                }
                if (deployedModel.SourceName == "Custom")
                {
                    var ingest_collection = _database.GetCollection<BsonDocument>(CONSTANTS.PS_IngestedData);
                    var ingest_filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, deployedModel.CorrelationId);
                    var ingest_Projection = Builders<BsonDocument>.Projection.Include("lastDateDict").Exclude("_id");
                    var Ingestdata_result = ingest_collection.Find(ingest_filter).Project<BsonDocument>(ingest_Projection).ToList();
                    if (Ingestdata_result.Count > 0)
                    {
                        IngrainRequestQueue requestQueue = new IngrainRequestQueue();
                        requestQueue = GetFileRequestStatus(deployedModel.CorrelationId, CONSTANTS.IngestData);

                        if (requestQueue != null)
                        {
                            JObject paramArgs = JObject.Parse(requestQueue.ParamArgs);
                            paramArgs[CONSTANTS.Flag] = "Incremental";
                            if (paramArgs["Customdetails"].ToString() != "null")
                            {
                                if (deployedModel.DataSource == "Phoenix CDM")
                                {
                                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(VDSDataService), nameof(GetUseCasePredictionRequest), "Inside-Phoenix CDM", "", "", useCasePredictionRequestInput.ClientUId, useCasePredictionRequestInput.DeliveryConstructUId);
                                    CustomDetails customPayload = JsonConvert.DeserializeObject<CustomDetails>(paramArgs["Customdetails"].ToString());
                                    DateTime lstDateDic = DateTime.Parse(deployedModel.ModifiedOn);
                                    customPayload.InputParameters.FromDate = lstDateDic.AddDays(-30).ToString(CONSTANTS.DateHoursFormat);
                                    customPayload.InputParameters.ToDate = DateTime.UtcNow.ToString(CONSTANTS.DateHoursFormat);
                                    string pyLoad = JsonConvert.SerializeObject(customPayload, Formatting.None);
                                    paramArgs["Customdetails"] = JObject.Parse(pyLoad);
                                }
                                else
                                {
                                    CustomPayloads customPayload = JsonConvert.DeserializeObject<CustomPayloads>(paramArgs["Customdetails"].ToString());
                                    DateTime lstDateDic = DateTime.Parse(deployedModel.ModifiedOn);
                                    customPayload.InputParameters.StartDate = lstDateDic.AddDays(-30).ToString(CONSTANTS.DateHoursFormat);
                                    customPayload.InputParameters.EndDate = DateTime.UtcNow.ToString(CONSTANTS.DateHoursFormat);
                                    string pyLoad = JsonConvert.SerializeObject(customPayload, Formatting.None);
                                    paramArgs["Customdetails"] = JObject.Parse(pyLoad);
                                }
                            }


                            requestQueue.ParamArgs = paramArgs.ToString(Formatting.None);
                            requestQueue._id = Guid.NewGuid().ToString();
                            requestQueue.RequestId = Guid.NewGuid().ToString();
                            requestQueue.UniId = Guid.NewGuid().ToString();
                            requestQueue.RequestStatus = CONSTANTS.New;
                            requestQueue.Status = CONSTANTS.Null;
                            requestQueue.Progress = CONSTANTS.Null;
                            requestQueue.Message = CONSTANTS.Null;
                            requestQueue.RetryCount = 0;
                            requestQueue.CreatedOn = DateTime.UtcNow.ToString(CONSTANTS.DateHoursFormat);
                            requestQueue.ModifiedOn = DateTime.UtcNow.ToString(CONSTANTS.DateHoursFormat);
                            requestQueue.IsForAutoTrain = false; //Should be false for Prediction avoiding single flow (arch change)
                            string requestId = requestQueue.RequestId;
                            if (deployedModel.ModelType == CONSTANTS.TimeSeries)
                            {
                                requestQueue.Frequency = useCasePredictionRequestInput.Frequency;
                            }
                            else
                            {
                                requestQueue.Frequency = CONSTANTS.Null;
                            }
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(VDSDataService), nameof(GetUseCasePredictionRequest), "requestQueue.UniId" + requestQueue.UniId, string.Empty, string.Empty, useCasePredictionRequestInput.ClientUId, useCasePredictionRequestInput.DeliveryConstructUId);
                            InsertRequests(requestQueue);
                            bool flag = false;
                            Task.Run(() => InsertPublishModelRequest(deployedModel, requestQueue.UniId));
                            //Thread.Sleep(2000);
                            useCasePredictionRequestOutput.UniqueId = requestQueue.UniId;
                            useCasePredictionRequestOutput.Status = "I";
                            useCasePredictionRequestOutput.StatusMessage = "Prediction request initiated successfully";

                        }
                    }
                }
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(VDSDataService), nameof(GetUseCasePredictionRequest), "PhoenixPredictionRequest service ended", string.Empty, string.Empty, useCasePredictionRequestInput.ClientUId, useCasePredictionRequestInput.DeliveryConstructUId);
                return useCasePredictionRequestOutput;
            }
            else
            {
                useCasePredictionRequestOutput.Status = CONSTANTS.E;
                useCasePredictionRequestOutput.StatusMessage = "The model is being re-trained. Please try after sometime";
            }



            LOGGING.LogManager.Logger.LogProcessInfo(typeof(VDSDataService), nameof(GetUseCasePredictionRequest), "UsecasePredictionRequest service ended", string.Empty, string.Empty, useCasePredictionRequestInput.ClientUId, useCasePredictionRequestInput.DeliveryConstructUId);
            return useCasePredictionRequestOutput;
        }

        public void InsertPublishModelRequest(DeployModelsDto deployedModel, string uniqueId)
        {
            bool flag = true;
            try
            {
                while (flag)
                {
                    IngrainRequestQueue requestQueue = GetPredictionRequestStatus(uniqueId, CONSTANTS.IngestData);
                    if (requestQueue.Status == "C")
                    {
                        string functionName = null;
                        if (deployedModel.ModelType == CONSTANTS.TimeSeries)
                        {
                            functionName = CONSTANTS.ForecastModel;
                        }
                        else
                        {
                            functionName = CONSTANTS.PublishModel;
                        }
                        this.insertRequest(deployedModel, uniqueId, functionName);
                        flag = false;
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(VDSDataService), nameof(InsertPublishModelRequest), "Incremental pull completed and publish request inserted for -" + uniqueId, string.IsNullOrEmpty(deployedModel.CorrelationId) ? default(Guid) : new Guid(deployedModel.CorrelationId), deployedModel.AppId, string.Empty, deployedModel.ClientUId, deployedModel.DeliveryConstructUID);
                    }
                    if (requestQueue.Status == "E")
                    {
                        string functionName = null;
                        if (deployedModel.ModelType == CONSTANTS.TimeSeries)
                        {
                            functionName = CONSTANTS.ForecastModel;
                        }
                        else
                        {
                            functionName = CONSTANTS.PublishModel;
                        }
                        this.insertErrorRequest(deployedModel, uniqueId, functionName, requestQueue.Message);
                        flag = false;
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(VDSDataService), nameof(InsertPublishModelRequest), "Incremental pull error and publish request inserted for -" + uniqueId, deployedModel.AppId, string.Empty, deployedModel.ClientUId, deployedModel.DeliveryConstructUID);
                    }
                    Thread.Sleep(1000);

                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(VDSDataService), nameof(InsertPublishModelRequest), ex.Message, ex, deployedModel.AppId, string.Empty, deployedModel.ClientUId, deployedModel.DeliveryConstructUID);
            }


        }

        private IngrainRequestQueue GetFileRequestStatus(string correlationId, string pageInfo)
        {
            IngrainRequestQueue ingrainRequest = new IngrainRequestQueue();
            var collection = _database.GetCollection<IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
            var builder = Builders<IngrainRequestQueue>.Filter;
            var filter = builder.Eq(CONSTANTS.CorrelationId, correlationId) & builder.Eq(CONSTANTS.pageInfo, pageInfo);
            var projection = Builders<IngrainRequestQueue>.Projection.Exclude(CONSTANTS.Id);
            return ingrainRequest = collection.Find(filter).Project<IngrainRequestQueue>(projection).ToList().FirstOrDefault();
        }
        public void InsertRequests(IngrainRequestQueue ingrainRequest)
        {
            var requestQueue = JsonConvert.SerializeObject(ingrainRequest);
            var insertRequestQueue = BsonSerializer.Deserialize<BsonDocument>(requestQueue);
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
            collection.InsertOne(insertRequestQueue);
        }


        public UseCasePredictionResponseOutput GetUseCasePredictionResponse(UseCasePredictionResponseInput useCasePredictionResponseInput)
        {
            UseCasePredictionResponseOutput useCasePredictionResponseOutput = new UseCasePredictionResponseOutput();
            useCasePredictionResponseOutput.UseCaseId = useCasePredictionResponseInput.UseCaseId;
            useCasePredictionResponseOutput.UniqueId = useCasePredictionResponseInput.UniqueId;
            useCasePredictionResponseOutput.PageNumber = useCasePredictionResponseInput.PageNumber;

            try
            {
                IngrainRequestQueue requestQueue = GetPredictionRequestStatus(useCasePredictionResponseInput.UniqueId, CONSTANTS.IngestData);
                if (requestQueue != null)
                {
                    if (requestQueue.Status == "C")
                    {
                        return GetPhoenixPredictionsStatus(useCasePredictionResponseInput);
                    }
                    else if (requestQueue.Status == "E")
                    {
                        useCasePredictionResponseOutput.Status = "E";
                        useCasePredictionResponseOutput.ErrorMessage = requestQueue.Message;
                        return useCasePredictionResponseOutput;
                    }
                    else
                    {
                        useCasePredictionResponseOutput.Status = "P";
                        useCasePredictionResponseOutput.ErrorMessage = "Prediction Inprogress";
                        return useCasePredictionResponseOutput;
                    }
                }
                else
                {
                    useCasePredictionResponseOutput.Status = "E";
                    useCasePredictionResponseOutput.ErrorMessage = "Record not found";
                    return useCasePredictionResponseOutput;
                }


            }
            catch (Exception ex)
            {
                useCasePredictionResponseOutput.Status = "E";
                useCasePredictionResponseOutput.ErrorMessage = "Something error in prediction";

                LOGGING.LogManager.Logger.LogErrorMessage(typeof(VDSDataService), nameof(GetUseCasePredictionResponse), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return useCasePredictionResponseOutput;
            }
        }

        private IngrainRequestQueue GetPredictionRequestStatus(string uniqueId, string pageInfo)
        {
            IngrainRequestQueue ingrainRequest = new IngrainRequestQueue();
            var collection = _database.GetCollection<IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
            var builder = Builders<IngrainRequestQueue>.Filter;
            var filter = builder.Eq("UniId", uniqueId) & builder.Eq(CONSTANTS.pageInfo, pageInfo);
            var projection = Builders<IngrainRequestQueue>.Projection.Exclude(CONSTANTS.Id);
            return ingrainRequest = collection.Find(filter).Project<IngrainRequestQueue>(projection).FirstOrDefault();
        }
        private UseCasePredictionResponseOutput GetPhoenixPredictionsStatus(UseCasePredictionResponseInput useCasePredictionResponseInput)
        {
            UseCasePredictionResponseOutput useCasePredictionResponseOutput = new UseCasePredictionResponseOutput();
            useCasePredictionResponseOutput.UseCaseId = useCasePredictionResponseInput.UseCaseId;
            useCasePredictionResponseOutput.UniqueId = useCasePredictionResponseInput.UniqueId;
            useCasePredictionResponseOutput.PageNumber = useCasePredictionResponseInput.PageNumber;
            var builder = Builders<PredictionDTO>.Filter;
            var filter = builder.Eq(CONSTANTS.UniqueId, useCasePredictionResponseInput.UniqueId)
                         & builder.Eq("Chunk_number", useCasePredictionResponseInput.PageNumber);
            var projection = Builders<PredictionDTO>.Projection.Exclude("_id");
            var collection = _database.GetCollection<PredictionDTO>(CONSTANTS.SSAI_PublishModel);
            var result = collection.Find(filter).Project<PredictionDTO>(projection).ToList();

            if (result.Count > 0)
            {
                string status = result[0].Status;
                bool DBEncryptionRequired = CommonUtility.EncryptDB(result[0].CorrelationId, appSettings);

                if (status == "C")
                {
                    if (DBEncryptionRequired)
                    {
                        if (result[0].PredictedData != null)
                            result[0].PredictedData = _encryptionDecryption.Decrypt(result[0].PredictedData);
                        if (!string.IsNullOrEmpty(result[0].Frequency) && result[0].Frequency != CONSTANTS.Null)
                        {
                            useCasePredictionResponseOutput.ActualData = _encryptionDecryption.Decrypt(result[0].PreviousData);
                        }
                        else
                        {
                            useCasePredictionResponseOutput.ActualData = _encryptionDecryption.Decrypt(result[0].ActualData);
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(result[0].Frequency) && result[0].Frequency != CONSTANTS.Null)
                        {
                            useCasePredictionResponseOutput.ActualData = result[0].PreviousData;
                        }
                        else
                        {
                            useCasePredictionResponseOutput.ActualData = result[0].ActualData;
                        }
                    }
                    useCasePredictionResponseOutput.PredictedData = result[0].PredictedData;

                    useCasePredictionResponseOutput.Status = result[0].Status;
                    useCasePredictionResponseOutput.ErrorMessage = result[0].ErrorMessage;
                }
                else if (status == "E")
                {
                    useCasePredictionResponseOutput.Status = result[0].Status;

                    useCasePredictionResponseOutput.ErrorMessage = result[0].ErrorMessage;
                }
                else
                {
                    useCasePredictionResponseOutput.Status = "I";

                    useCasePredictionResponseOutput.ErrorMessage = "Prediction is in Progress";
                }

            }
            useCasePredictionResponseOutput.AvailablePages = GetPublishModelPages(useCasePredictionResponseInput.UniqueId);

            return useCasePredictionResponseOutput;

        }

        private List<string> GetPublishModelPages(string uniqueId)
        {
            List<string> availablePages = new List<string>();
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq(CONSTANTS.UniqueId, uniqueId);
            var projection = Builders<BsonDocument>.Projection.Exclude("_id");
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_PublishModel);
            var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();

            if (result.Count > 0)
            {
                foreach (var req in result)
                {
                    if (!req["Chunk_number"].IsBsonNull)
                    {
                        availablePages.Add(req["Chunk_number"].AsString);
                    }

                }
            }

            return availablePages;
        }
        private void insertRequest(DeployModelsDto deployModels, string uniqueId, string Function, string VDSNotificationUrl = "")
        {
            var featureWeights = new
            {
                FeatureWeights = appSettings.Value.EnableFeatureWeights
            };

            var paramArgs = JsonConvert.SerializeObject(featureWeights, Formatting.None);


            IngrainRequestQueue ingrainRequest = new IngrainRequestQueue
            {
                _id = Guid.NewGuid().ToString(),
                CorrelationId = deployModels.CorrelationId,
                RequestId = Guid.NewGuid().ToString(),
                ProcessId = CONSTANTS.Null,
                Status = CONSTANTS.Null,
                ModelName = CONSTANTS.Null,
                RequestStatus = appSettings.Value.IsFlaskCall ? CONSTANTS.Occupied : CONSTANTS.New,
                RetryCount = 0,
                ProblemType = CONSTANTS.Null,
                Message = CONSTANTS.Null,
                UniId = uniqueId,
                Progress = CONSTANTS.Null,
                pageInfo = Function, // pageInfo 
                ParamArgs = paramArgs,
                Function = Function,
                CreatedByUser = deployModels.CreatedByUser,
                TemplateUseCaseID = deployModels.TemplateUsecaseId,
                CreatedOn = DateTime.UtcNow.ToString(CONSTANTS.DateHoursFormat),
                ModifiedByUser = deployModels.ModifiedByUser,
                ModifiedOn = DateTime.UtcNow.ToString(CONSTANTS.DateHoursFormat),
                LastProcessedOn = CONSTANTS.Null,
                AppID = deployModels.AppId,
                SendNotification = "True",
                IsNotificationSent = "False",
                IsForAPI = appSettings.Value.IsFlaskCall ? true : false,
                AppURL = VDSNotificationUrl != "" ? VDSNotificationUrl : null
            };
            _ingestedDataService.InsertRequests(ingrainRequest);
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(IAService), nameof(insertRequest), "AWF/Generic process before Flask CallPython Triggered", string.Empty, "Flag: " + appSettings.Value.IsFlaskCall, string.Empty, string.Empty);
            if (appSettings.Value.IsFlaskCall)
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(VDSDataService), nameof(insertRequest), "AWF/Generic process Inside Flask CallPython Triggered", _iFlaskAPIService != null ? "true" : "false", string.Empty, string.Empty, string.Empty);
                _iFlaskAPIService.CallPython(ingrainRequest.CorrelationId, ingrainRequest.UniId, ingrainRequest.pageInfo);
            }
            Thread.Sleep(2000);
        }
        private void insertErrorRequest(DeployModelsDto deployModels, string uniqueId, string Function, string message)
        {

            IngrainRequestQueue ingrainRequest = new IngrainRequestQueue
            {
                _id = Guid.NewGuid().ToString(),
                CorrelationId = deployModels.CorrelationId,
                RequestId = Guid.NewGuid().ToString(),
                ProcessId = CONSTANTS.Null,
                Status = CONSTANTS.E,
                ModelName = CONSTANTS.Null,
                RequestStatus = "Task Complete",
                RetryCount = 0,
                ProblemType = CONSTANTS.Null,
                Message = message,
                UniId = uniqueId,
                Progress = CONSTANTS.Null,
                pageInfo = Function, // pageInfo 
                ParamArgs = CONSTANTS.CurlyBraces,
                Function = Function,
                CreatedByUser = deployModels.CreatedByUser,
                TemplateUseCaseID = deployModels.UseCaseID,
                CreatedOn = DateTime.UtcNow.ToString(CONSTANTS.DateHoursFormat),
                ModifiedByUser = deployModels.ModifiedByUser,
                ModifiedOn = DateTime.UtcNow.ToString(CONSTANTS.DateHoursFormat),
                LastProcessedOn = CONSTANTS.Null,
                AppID = deployModels.AppId,
                SendNotification = "True",
                IsNotificationSent = "False"
            };
            _ingestedDataService.InsertRequests(ingrainRequest);
            Thread.Sleep(2000);
        }


        public VdsUseCaseTrainingResponse TrainVDSUseCase(VdsUseCaseTrainingRequest vDSUseCaseTrainingRequest)
        {
            VdsUseCaseTrainingResponse vDSUseCaseTrainingResponse
                = new VdsUseCaseTrainingResponse(vDSUseCaseTrainingRequest.ClientUId
                                                 , vDSUseCaseTrainingRequest.DeliveryConstructUId
                                                 , vDSUseCaseTrainingRequest.ApplicationId
                                                 , vDSUseCaseTrainingRequest.UseCaseId);

            string encryptedUser = "SYSTEM";
            if (!string.IsNullOrEmpty(Convert.ToString(encryptedUser)))
            {
                encryptedUser = _encryptionDecryption.Encrypt(Convert.ToString(encryptedUser));
            }
            var deployModel = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIIngrainRequests);
            var filter = Builders<DeployModelsDto>.Filter.Where(x => x.ClientUId == vDSUseCaseTrainingRequest.ClientUId)
                         & Builders<DeployModelsDto>.Filter.Where(x => x.DeliveryConstructUID == vDSUseCaseTrainingRequest.DeliveryConstructUId)
                         & Builders<DeployModelsDto>.Filter.Where(x => x.AppId == vDSUseCaseTrainingRequest.ApplicationId)
                         & Builders<DeployModelsDto>.Filter.Where(x => x.TemplateUsecaseId == vDSUseCaseTrainingRequest.UseCaseId)
                         & Builders<DeployModelsDto>.Filter.Where(x => (x.CreatedByUser == "SYSTEM" || x.CreatedByUser == encryptedUser));
            var modelDetails = deployModel.Find(filter).FirstOrDefault();

            if (modelDetails != null)
            {
                if (modelDetails.Status == "Deployed")
                {
                    vDSUseCaseTrainingResponse.Status = "C";
                    vDSUseCaseTrainingResponse.StatusMessage = "Model Trained";
                }
                else
                {
                    var ingrainRequestCollection = _database.GetCollection<IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
                    var ingrainRequestFilterBuilder = Builders<IngrainRequestQueue>.Filter;
                    var ingrainRequestFilterQueue = ingrainRequestFilterBuilder.Where(x => x.CorrelationId == modelDetails.CorrelationId)
                                                    & ingrainRequestFilterBuilder.Where(x => x.Function == CONSTANTS.AutoTrain);
                    var ingrainRequestQueueResult = ingrainRequestCollection.Find(ingrainRequestFilterQueue).FirstOrDefault();
                    if (ingrainRequestQueueResult != null)
                    {
                        if (string.IsNullOrEmpty(ingrainRequestQueueResult.Status) || ingrainRequestQueueResult.Status == CONSTANTS.Null)
                            vDSUseCaseTrainingResponse.Status = "P";
                        else
                            vDSUseCaseTrainingResponse.Status = ingrainRequestQueueResult.Status;

                        vDSUseCaseTrainingResponse.StatusMessage = ingrainRequestQueueResult.Message;
                    }
                    else
                    {
                        //start model training
                        _flushService.FlushModel(modelDetails.CorrelationId, "");
                        vDSUseCaseTrainingResponse = StartUseCaseTraining(vDSUseCaseTrainingRequest);

                    }
                }
            }
            else
            {
                vDSUseCaseTrainingResponse = StartUseCaseTraining(vDSUseCaseTrainingRequest);
            }


            return vDSUseCaseTrainingResponse;
        }



        public VdsUseCaseTrainingResponse StartUseCaseTraining(VdsUseCaseTrainingRequest vDSUseCaseTrainingRequest)
        {
            VdsUseCaseTrainingResponse vDSUseCaseTrainingResponse
                = new VdsUseCaseTrainingResponse(vDSUseCaseTrainingRequest.ClientUId
                                                 , vDSUseCaseTrainingRequest.DeliveryConstructUId
                                                 , vDSUseCaseTrainingRequest.ApplicationId
                                                 , vDSUseCaseTrainingRequest.UseCaseId);
            string correlationId = Guid.NewGuid().ToString();
            var publicTemplateCollection = _database.GetCollection<PublicTemplateMapping>(CONSTANTS.PublicTemplateMapping);
            var publicTemplateFilterBuilder = Builders<PublicTemplateMapping>.Filter;
            var publicTemplateFilterQueue = publicTemplateFilterBuilder.Where(x => x.ApplicationID == vDSUseCaseTrainingRequest.ApplicationId)
                                            & publicTemplateFilterBuilder.Where(x => x.UsecaseID == vDSUseCaseTrainingRequest.UseCaseId);
            var Projection = Builders<PublicTemplateMapping>.Projection.Exclude("_id");
            var publicTemplateQueueResult = publicTemplateCollection.Find(publicTemplateFilterQueue).Project<PublicTemplateMapping>(Projection).FirstOrDefault();

            CustomUploadFile paramArgs = CreateParamArgs(vDSUseCaseTrainingRequest, correlationId);
            bool DBEncryptionRequired = CommonUtility.EncryptDB(correlationId, appSettings);
            IngrainRequestQueue ingrainRequest = new IngrainRequestQueue
            {
                _id = Guid.NewGuid().ToString(),
                CorrelationId = correlationId,
                RequestId = Guid.NewGuid().ToString(),
                ProcessId = CONSTANTS.Null,
                //Status = CONSTANTS.Null,
                ModelName = correlationId + "_" + publicTemplateQueueResult.UsecaseName,
                //RequestStatus = CONSTANTS.New,
                RetryCount = 0,
                ProblemType = CONSTANTS.Null,
                //Message = CONSTANTS.Null,
                UniId = CONSTANTS.Null,
                InstaID = CONSTANTS.Null,
                Progress = CONSTANTS.Null,
                pageInfo = CONSTANTS.AutoTrain,
                ParamArgs = JsonConvert.SerializeObject(paramArgs),
                TemplateUseCaseID = publicTemplateQueueResult.UsecaseID,
                Function = CONSTANTS.AutoTrain,
                CreatedByUser = DBEncryptionRequired ? _encryptionDecryption.Encrypt("SYSTEM") : "SYSTEM",
                CreatedOn = DateTime.UtcNow.ToString(CONSTANTS.DateHoursFormat),
                ModifiedByUser = CONSTANTS.Null,
                ModifiedOn = DateTime.UtcNow.ToString(CONSTANTS.DateHoursFormat),
                LastProcessedOn = CONSTANTS.Null,
                AppID = publicTemplateQueueResult.ApplicationID,
                ClientId = vDSUseCaseTrainingRequest.ClientUId,
                DeliveryconstructId = vDSUseCaseTrainingRequest.DeliveryConstructUId,
                UseCaseID = vDSUseCaseTrainingRequest.UseCaseId,
                SendNotification = "True",
                IsNotificationSent = "False",
                //DataSource = CONSTANTS.Null,
                EstimatedRunTime = CONSTANTS.Null
            };
            //bool isdata = _genericSelfservice.CheckRequiredDetails(publicTemplateQueueResult.ApplicationName, publicTemplateQueueResult.UsecaseID);
            //if (isdata)
            //{

            //}
            //else
            //{
            //    ingrainRequest.Status = CONSTANTS.E;
            //    ingrainRequest.RequestStatus = CONSTANTS.ErrorMessage;
            //    ingrainRequest.Message = CONSTANTS.ModelisCreatedbyFileUpload;
            //}
            ingrainRequest.Status = CONSTANTS.Null;
            ingrainRequest.RequestStatus = CONSTANTS.New;
            ingrainRequest.Message = CONSTANTS.Null;
            InsertRequests(ingrainRequest);
            vDSUseCaseTrainingResponse.StatusMessage = CONSTANTS.TrainingResponse;
            vDSUseCaseTrainingResponse.Status = CONSTANTS.I;

            return vDSUseCaseTrainingResponse;

        }



        public CustomUploadFile CreateParamArgs(VdsUseCaseTrainingRequest vDSUseCaseTrainingRequest, string correlationId)
        {
            var templateCollection = _database.GetCollection<IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
            var filter = Builders<IngrainRequestQueue>.Filter.Where(x => x.CorrelationId == vDSUseCaseTrainingRequest.UseCaseId)
                         & Builders<IngrainRequestQueue>.Filter.Where(x => x.pageInfo == CONSTANTS.IngestData);
            var projection = Builders<IngrainRequestQueue>.Projection.Exclude("_id");
            var templateDetails = templateCollection.Find(filter).Project<IngrainRequestQueue>(projection).FirstOrDefault();
            if (templateDetails != null)
            {
                CustomUploadFile customUploadFile = JsonConvert.DeserializeObject<CustomUploadFile>(templateDetails.ParamArgs);

                customUploadFile.ClientUID = vDSUseCaseTrainingRequest.ClientUId;
                customUploadFile.DeliveryConstructUId = vDSUseCaseTrainingRequest.DeliveryConstructUId;
                customUploadFile.CorrelationId = correlationId;
                customUploadFile.Customdetails.InputParameters.ClientID = vDSUseCaseTrainingRequest.ClientUId;
                customUploadFile.Customdetails.InputParameters.E2EUID = vDSUseCaseTrainingRequest.E2EUId;
                customUploadFile.Customdetails.InputParameters.DeliveryConstructID = vDSUseCaseTrainingRequest.DeliveryConstructUId;
                customUploadFile.Customdetails.InputParameters.StartDate = DateTime.UtcNow.AddYears(-1).ToString(CONSTANTS.DateHoursFormat);
                customUploadFile.Customdetails.InputParameters.EndDate = DateTime.UtcNow.ToString(CONSTANTS.DateHoursFormat);

                return customUploadFile;
            }
            else
                return null;

        }

        #region Genric Model Training and Prediction for FDS and PAM
        public GenericModelTrainingResponse StartGenericModelTraining(GenericModelTrainingRequest RequestPayload)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(VDSDataService), nameof(StartGenericModelTraining), "InitiateTraining - Started :", RequestPayload.ApplicationId, RequestPayload.UseCaseId, RequestPayload.ClientUId, RequestPayload.DeliveryConstructUId);
            var AppIntegCollection = _database.GetCollection<AppIntegration>(CONSTANTS.AppIntegration);
            var filterBuilder = Builders<AppIntegration>.Filter;
            var AppFilter = filterBuilder.Eq(CONSTANTS.ApplicationID, RequestPayload.ApplicationId);
            var Projection = Builders<AppIntegration>.Projection.Exclude(CONSTANTS.Id);
            var IsApplicationExist = AppIntegCollection.Find(AppFilter).Project<AppIntegration>(Projection).FirstOrDefault();
            string CorrelationId = string.Empty;
            if (IsApplicationExist != null)
            {
                if (UpdateTokenDetailsInAppIntegration(RequestPayload.ApplicationId) == CONSTANTS.Success && UpdPublicTemplateMapping(RequestPayload.ApplicationId, RequestPayload.UseCaseId) == CONSTANTS.Success)
                {
                    var MappingCollection = _database.GetCollection<PublicTemplateMapping>(CONSTANTS.PublicTemplateMapping);
                    var Builder = Builders<PublicTemplateMapping>.Filter;
                    var Projection1 = Builders<PublicTemplateMapping>.Projection.Exclude("_id");
                    var filter = Builder.Eq(CONSTANTS.UsecaseID, RequestPayload.UseCaseId);
                    var templatedata = MappingCollection.Find(filter).Project<PublicTemplateMapping>(Projection1).FirstOrDefault();

                    if (templatedata != null)
                    {
                        if (templatedata.IsMultipleApp == "yes")
                        {
                            if (templatedata.ApplicationIDs.Contains(RequestPayload.ApplicationId))
                            {
                                templatedata.ApplicationID = RequestPayload.ApplicationId;
                                templatedata.ApplicationName = IsApplicationExist.ApplicationName;
                            }
                            else
                            {
                                _TrainingResponse.ErrorMessage = CONSTANTS.IsApplicationExist;
                                _TrainingResponse.Status = "Error";
                                return _TrainingResponse;
                            }
                        }
                        else
                        {
                            if (RequestPayload.ApplicationId != templatedata.ApplicationID)
                            {
                                _TrainingResponse.ErrorMessage = CONSTANTS.IsApplicationExist;
                                _TrainingResponse.Status = "Error";
                                return _TrainingResponse;
                            }
                        }

                        var _trainingStatus = GetModelStatus(RequestPayload.ClientUId, RequestPayload.DeliveryConstructUId, RequestPayload.ApplicationId, RequestPayload.UseCaseId, RequestPayload.UserId, templatedata.IsMultipleApp, templatedata.ApplicationIDs, Convert.ToString(RequestPayload.DataSourceDetails.RequestType), Convert.ToString(RequestPayload.DataSourceDetails.ServiceType));
                        string NewModelCorrelationID = Guid.NewGuid().ToString();
                        string requestId = string.Empty;
                        if (_trainingStatus == null)
                        {
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(VDSDataService), nameof(StartGenericModelTraining), "TRAINING INITIATING - STARTED :", string.IsNullOrEmpty(NewModelCorrelationID) ? default(Guid) : new Guid(NewModelCorrelationID), string.Empty, string.Empty, RequestPayload.ClientUId, RequestPayload.DeliveryConstructUId);
                            var ingrainRequest = CreateIngrainRequest(NewModelCorrelationID, templatedata, RequestPayload);
                            InsertRequests(ingrainRequest);
                            requestId = ingrainRequest.RequestId;
                            IngrainToVDSNotification CallBackResponse = new IngrainToVDSNotification
                            {
                                CorrelationId = ingrainRequest.CorrelationId,
                                Status = "Initiated",
                                Message = CONSTANTS.TrainingInitiated,
                                ErrorMessage = string.Empty,
                                ClientUId = RequestPayload.ClientUId,
                                DeliveryConstructUId = RequestPayload.DeliveryConstructUId,
                                UseCaseId = RequestPayload.UseCaseId,
                                RequestType = Convert.ToString(RequestPayload.DataSourceDetails.RequestType),
                                ServiceType = Convert.ToString(RequestPayload.DataSourceDetails.ServiceType),
                                E2EUID = Convert.ToString(RequestPayload.DataSourceDetails.E2EUID),
                                Progress = "0%",
                                ProcessingStartTime = ingrainRequest.CreatedOn,
                                ProcessingEndTime = ""
                            };
                            string callbackResonse = CallbackResponse(CallBackResponse, templatedata.ApplicationName, RequestPayload.ResponseCallbackUrl, ingrainRequest.ClientId, ingrainRequest.DeliveryconstructId, ingrainRequest.AppID, RequestPayload.UseCaseId, ingrainRequest.RequestId, ingrainRequest.CreatedByUser);
                            _TrainingResponse.Message = CallBackResponse.Message;
                            _TrainingResponse.CorrelationId = ingrainRequest.CorrelationId;
                            _TrainingResponse.Status = CallBackResponse.Status;
                            _TrainingResponse.Progress = "0%";
                            if (callbackResonse == CONSTANTS.ErrorMessage)
                            {
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(VDSDataService), nameof(StartGenericModelTraining), "CALL BACK URL ERROR :" + callbackResonse, string.Empty, string.Empty, RequestPayload.ClientUId, RequestPayload.DeliveryConstructUId);
                                _TrainingResponse.ErrorMessage = "Error encountered while calling VDS Notification API";
                                return _TrainingResponse;
                            }
                            return _TrainingResponse;
                        }
                        else
                        {
                            requestId = _trainingStatus.UniqueId;
                            if (_trainingStatus.Status == null || _trainingStatus.Status == CONSTANTS.E)
                            {
                                if (!string.IsNullOrEmpty(_trainingStatus.CorrelationId))
                                {
                                    NewModelCorrelationID = _trainingStatus.CorrelationId;
                                }
                                if (_trainingStatus.Status == CONSTANTS.E)
                                {
                                    //deleting old Correlation Id record with Error
                                    var queueCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
                                    var filterBuilder1 = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, _trainingStatus.CorrelationId);
                                    queueCollection.DeleteMany(filterBuilder1);
                                    var deployCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
                                    var deployFilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, _trainingStatus.CorrelationId);
                                    deployCollection.DeleteMany(deployFilter);

                                    NewModelCorrelationID = Guid.NewGuid().ToString();
                                    _TrainingResponse.Status = CONSTANTS.ErrorMessage;
                                    _TrainingResponse.CorrelationId = _trainingStatus.CorrelationId;
                                    _TrainingResponse.Message = CONSTANTS.Error;
                                    var ingrainRequest = CreateIngrainRequest(NewModelCorrelationID, templatedata, RequestPayload);
                                    InsertRequests(ingrainRequest);
                                    IngrainToVDSNotification CallBackResponse = new IngrainToVDSNotification
                                    {
                                        CorrelationId = ingrainRequest.CorrelationId,
                                        Status = "Initiated",
                                        Message = CONSTANTS.TrainingInitiated + " ,as previous request " + _trainingStatus.CorrelationId + " failed with Error :" + _trainingStatus.ErrorMessage,
                                        ErrorMessage = string.Empty,
                                        ClientUId = RequestPayload.ClientUId,
                                        DeliveryConstructUId = RequestPayload.DeliveryConstructUId,
                                        UseCaseId = RequestPayload.UseCaseId,
                                        RequestType = Convert.ToString(RequestPayload.DataSourceDetails.RequestType),
                                        ServiceType = Convert.ToString(RequestPayload.DataSourceDetails.ServiceType),
                                        E2EUID = Convert.ToString(RequestPayload.DataSourceDetails.E2EUID),
                                        Progress = "0%",
                                        ProcessingStartTime = ingrainRequest.CreatedOn,
                                        ProcessingEndTime = ""
                                    };
                                    string callbackResonse = CallbackResponse(CallBackResponse, templatedata.ApplicationName, RequestPayload.ResponseCallbackUrl, RequestPayload.ClientUId, RequestPayload.DeliveryConstructUId, RequestPayload.ApplicationId, RequestPayload.UseCaseId, requestId, RequestPayload.UserId);
                                    _TrainingResponse.Message = CallBackResponse.Message;
                                    _TrainingResponse.CorrelationId = ingrainRequest.CorrelationId;
                                    _TrainingResponse.Status = CallBackResponse.Status;
                                    _TrainingResponse.Progress = "0%";
                                    if (callbackResonse == CONSTANTS.ErrorMessage)
                                    {
                                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(VDSDataService), nameof(StartGenericModelTraining), "CALL BACK URL ERROR :" + callbackResonse, string.Empty, string.Empty, RequestPayload.ClientUId, RequestPayload.DeliveryConstructUId);
                                        _TrainingResponse.ErrorMessage = "Error encountered while calling VDS Notification API";
                                        return _TrainingResponse;
                                    }
                                    return _TrainingResponse;
                                }
                                else
                                {
                                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(VDSDataService), nameof(StartGenericModelTraining), "_trainingStatus :" + NewModelCorrelationID + "--" + _trainingStatus.Status + "--NEW--" + NewModelCorrelationID, string.Empty, string.Empty, RequestPayload.ClientUId, RequestPayload.DeliveryConstructUId);
                                    var ingrainRequest = CreateIngrainRequest(NewModelCorrelationID, templatedata, RequestPayload);
                                    InsertRequests(ingrainRequest);
                                    IngrainToVDSNotification CallBackResponse = new IngrainToVDSNotification
                                    {
                                        CorrelationId = ingrainRequest.CorrelationId,
                                        Status = "Initiated",
                                        Message = CONSTANTS.TrainingInitiated,
                                        ErrorMessage = string.Empty,
                                        ClientUId = RequestPayload.ClientUId,
                                        DeliveryConstructUId = RequestPayload.DeliveryConstructUId,
                                        UseCaseId = RequestPayload.UseCaseId,
                                        RequestType = Convert.ToString(RequestPayload.DataSourceDetails.RequestType),
                                        ServiceType = Convert.ToString(RequestPayload.DataSourceDetails.ServiceType),
                                        E2EUID = Convert.ToString(RequestPayload.DataSourceDetails.E2EUID),
                                        Progress = "0%",
                                        ProcessingStartTime = ingrainRequest.CreatedOn,
                                        ProcessingEndTime = ""
                                    };
                                    string callbackResonse = CallbackResponse(CallBackResponse, templatedata.ApplicationName, RequestPayload.ResponseCallbackUrl, RequestPayload.ClientUId, RequestPayload.DeliveryConstructUId, RequestPayload.ApplicationId, RequestPayload.UseCaseId, requestId, RequestPayload.UserId);
                                    _TrainingResponse.Message = CallBackResponse.Message;
                                    _TrainingResponse.CorrelationId = ingrainRequest.CorrelationId;
                                    _TrainingResponse.Status = CallBackResponse.Status;
                                    _TrainingResponse.Progress = "0%";
                                    if (callbackResonse == CONSTANTS.ErrorMessage)
                                    {
                                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(VDSDataService), nameof(StartGenericModelTraining), "CALL BACK URL ERROR :" + callbackResonse, string.Empty, string.Empty, RequestPayload.ClientUId, RequestPayload.DeliveryConstructUId);
                                        _TrainingResponse.ErrorMessage = "Error encountered while calling VDS Notification API";
                                        return _TrainingResponse;
                                    }
                                    return _TrainingResponse;
                                }
                            }
                            else
                            {
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(VDSDataService), nameof(StartGenericModelTraining), "_trainingStatus:" + _trainingStatus.CorrelationId + "--" + _trainingStatus.Status, string.Empty, string.Empty, RequestPayload.ClientUId, RequestPayload.DeliveryConstructUId);
                                if (_trainingStatus.Status == CONSTANTS.P || _trainingStatus.Status == CONSTANTS.I)
                                {
                                    _TrainingResponse.Status = _trainingStatus.Status;
                                    _TrainingResponse.Message = CONSTANTS.TrainingInprogress;
                                    _TrainingResponse.CorrelationId = _trainingStatus.CorrelationId;
                                    if (_trainingStatus.Progress != CONSTANTS.Null)
                                        _TrainingResponse.Progress = _trainingStatus.Progress;
                                    else
                                        _TrainingResponse.Progress = "0%";

                                    return _TrainingResponse;
                                }
                                else
                                {
                                    if (templatedata.IsMultipleApp == "yes")
                                    {
                                        if (templatedata.ApplicationIDs.Contains(RequestPayload.ApplicationId))
                                        {
                                            var collection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
                                            var builder = Builders<DeployModelsDto>.Filter;
                                            var filter1 = builder.Eq(CONSTANTS.CorrelationId, _trainingStatus.CorrelationId);
                                            var builder1 = Builders<DeployModelsDto>.Update;
                                            var update = builder1.Set(CONSTANTS.IsMutipleApp, true);
                                            collection.UpdateMany(filter1, update);
                                        }
                                    }

                                    IngrainToVDSNotification CallBackResponse = new IngrainToVDSNotification
                                    {
                                        CorrelationId = _trainingStatus.CorrelationId,
                                        Status = "Completed",
                                        Message = "Training completed for the usecaseid",
                                        ErrorMessage = string.Empty,
                                        ClientUId = RequestPayload.ClientUId,
                                        DeliveryConstructUId = RequestPayload.DeliveryConstructUId,
                                        UseCaseId = RequestPayload.UseCaseId,
                                        RequestType = Convert.ToString(RequestPayload.DataSourceDetails.RequestType),
                                        ServiceType = Convert.ToString(RequestPayload.DataSourceDetails.ServiceType),
                                        E2EUID = Convert.ToString(RequestPayload.DataSourceDetails.E2EUID),
                                        Progress = _trainingStatus.Progress,
                                        ProcessingStartTime = _trainingStatus.CreatedOn,
                                        ProcessingEndTime = _trainingStatus.ModifiedOn
                                    };
                                    string callbackResonse = CallbackResponse(CallBackResponse, templatedata.ApplicationName, RequestPayload.ResponseCallbackUrl, RequestPayload.ClientUId, RequestPayload.DeliveryConstructUId, RequestPayload.ApplicationId, RequestPayload.UseCaseId, requestId, RequestPayload.UserId);
                                    _TrainingResponse.Status = CallBackResponse.Status;
                                    _TrainingResponse.Message = CallBackResponse.Message;
                                    _TrainingResponse.CorrelationId = _trainingStatus.CorrelationId;
                                    _TrainingResponse.Progress = _trainingStatus.Progress;
                                    if (callbackResonse == CONSTANTS.ErrorMessage)
                                    {
                                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(VDSDataService), nameof(StartGenericModelTraining), "CALL BACK URL ERROR :" + callbackResonse, string.Empty, string.Empty, RequestPayload.ClientUId, RequestPayload.DeliveryConstructUId);
                                        _TrainingResponse.ErrorMessage = "Error encountered while calling VDS Notification API";
                                        return _TrainingResponse;
                                    }
                                    return _TrainingResponse;
                                }
                            }
                        }

                    }
                    else
                    {
                        _TrainingResponse.ErrorMessage = CONSTANTS.UsecaseNotAvailable;
                        _TrainingResponse.Status = "Error";
                        return _TrainingResponse;
                    }
                }
                else
                {
                    _TrainingResponse.ErrorMessage = "Error Updating AppIntegration and PublicTemplatingMapping details";
                    _TrainingResponse.Status = "Error";
                    return _TrainingResponse;
                }

            }
            else
            {
                _TrainingResponse.ErrorMessage = CONSTANTS.IsApplicationExist;
                _TrainingResponse.Status = "Error";
                return _TrainingResponse;
            }
        }
        private string UpdateTokenDetailsInAppIntegration(string appId)
        {
            AppIntegrationsCredentials appIntegrationsCredentials = new AppIntegrationsCredentials()
            {
                grant_type = appSettings.Value.Grant_Type,
                client_id = appSettings.Value.clientId,
                client_secret = appSettings.Value.clientSecret,
                resource = appSettings.Value.resourceId
            };
            AppIntegration appIntegrations = new AppIntegration()
            {
                ApplicationID = appId,
                Credentials = JsonConvert.SerializeObject(appIntegrationsCredentials)
            };
            return _genericSelfservice.UpdateAppIntegration(appIntegrations);
        }
        private string UpdPublicTemplateMapping(string appId, string usecaseId)
        {
            bool isMultiApp = false;
            bool isSingleApp = false;
            Uri apiUri = new Uri(appSettings.Value.VdsURL);
            string host = apiUri.GetLeftPart(UriPartial.Authority);

            var MappingCollection = _database.GetCollection<PublicTemplateMapping>(CONSTANTS.PublicTemplateMapping);
            var Builder = Builders<PublicTemplateMapping>.Filter;
            var Projection1 = Builders<PublicTemplateMapping>.Projection.Exclude("_id");
            var filter = Builder.Eq(CONSTANTS.UsecaseID, usecaseId);
            var templatedata = MappingCollection.Find(filter).Project<PublicTemplateMapping>(Projection1).FirstOrDefault();
            if (templatedata != null)
            {
                if (templatedata != null && templatedata.IsMultipleApp == "yes")
            {
                if (templatedata.ApplicationIDs.Contains(appId))
                {
                    isMultiApp = true;
                }
            }
            else
                {
                    isSingleApp = true;
                }
            }
            if (isSingleApp || isMultiApp)
            {
                string SourceURL = host;
                string SourceName = string.Empty;
                if (appSettings.Value.Environment.Equals(CONSTANTS.FDSEnvironment) || appSettings.Value.Environment.Equals(CONSTANTS.PAMEnvironment))
                    SourceName = CONSTANTS.VDS_AIOPS;
                if (!string.IsNullOrEmpty(templatedata.SourceName) && templatedata.SourceName.ToUpper() == CONSTANTS.CustomDataApi.ToUpper())
                {
                    SourceName = templatedata.SourceName;
                    if (!string.IsNullOrEmpty(templatedata.SourceURL))
                    {
                        SourceURL = templatedata.SourceURL;
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(templatedata.SourceURL))
                        SourceURL = templatedata.SourceURL;
                    else
                    {
                        Uri apiUri1 = new Uri(appSettings.Value.GetVdsDataURL);
                        string apiPath = apiUri1.AbsolutePath;
                        SourceURL = host + apiPath;
                    }
                }

                PublicTemplateMapping templateMapping = new PublicTemplateMapping()
                {
                    ApplicationID = templatedata.ApplicationID,
                    SourceName = SourceName,
                    UsecaseID = usecaseId,
                    SourceURL = SourceURL,
                    InputParameters = ""
                };
                return _genericSelfservice.UpdatePublicTemplateMapping(templateMapping);
            }
            else
            {
                return CONSTANTS.NoRecordsFound;
            }
        }
        private SSAIIngrainTrainingStatus GetModelStatus(string clientId, string dcId, string applicationId, string usecaseId, string userId, string isMultiApp, string applicationIds, string RequestType, string ServiceType)
        {
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
            var filterBuilder = Builders<BsonDocument>.Filter;
            FilterDefinition<BsonDocument> filterQueue = filterBuilder.Eq(CONSTANTS.ClientId, clientId) & filterBuilder.Eq(CONSTANTS.DeliveryconstructId, dcId) & filterBuilder.Eq(CONSTANTS.TemplateUseCaseID, usecaseId) & filterBuilder.Eq(CONSTANTS.AppID, applicationId) & filterBuilder.Eq(CONSTANTS.pageInfo, CONSTANTS.TrainAndPredict)
                & filterBuilder.Eq("ServiceType", ServiceType) & filterBuilder.Eq("RequestType", RequestType);
            var Projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
            var result = collection.Find(filterQueue).Project<BsonDocument>(Projection).ToList();
            if (result.Count > 0)
            {
                _trainingStatus.CorrelationId = result[0][CONSTANTS.CorrelationId].ToString();
                _trainingStatus.Status = result[0][CONSTANTS.Status].ToString();
                _trainingStatus.ErrorMessage = result[0][CONSTANTS.Message].ToString();
                _trainingStatus.UniqueId = result[0]["RequestId"].ToString();
                _trainingStatus.Progress = result[0]["Progress"].ToString();
                _trainingStatus.CreatedOn = result[0]["CreatedOn"].ToString();
                _trainingStatus.ModifiedOn = result[0]["ModifiedOn"].ToString();
                if (isMultiApp == "yes")
                {
                    if (!applicationIds.Contains(result[0][CONSTANTS.AppID].ToString()))
                    {
                        _trainingStatus = null;
                        return _trainingStatus;
                    }
                }
                if (result[0][CONSTANTS.Status].ToString() == CONSTANTS.Null || result[0][CONSTANTS.Status].ToString() == CONSTANTS.BsonNull)
                    _trainingStatus.Status = CONSTANTS.P;
            }
            else
            {
                _trainingStatus = null;
            }
            return _trainingStatus;
        }
        private IngrainRequestQueue CreateIngrainRequest(string NewModelCorrelationID, PublicTemplateMapping templatedata, GenericModelTrainingRequest RequestPayload)
        {
            VDSInputParams VDSInputParams = new VDSInputParams
            {
                E2EUID = Convert.ToString(RequestPayload.DataSourceDetails.E2EUID),
                RequestType = Convert.ToString(RequestPayload.DataSourceDetails.RequestType),
                ServiceType = Convert.ToString(RequestPayload.DataSourceDetails.ServiceType),
            };
            string UserId = RequestPayload.UserId;
            bool DBEncryptionRequired = CommonUtility.EncryptDB(NewModelCorrelationID, appSettings);
            if (DBEncryptionRequired)
            {
                if (!string.IsNullOrEmpty(Convert.ToString(RequestPayload.UserId)))
                {
                    UserId = _encryptionDecryption.Encrypt(Convert.ToString(RequestPayload.UserId));
                }
            }
            IngrainRequestQueue ingrainRequest = new IngrainRequestQueue
            {
                _id = Guid.NewGuid().ToString(),
                CorrelationId = NewModelCorrelationID,
                RequestId = Guid.NewGuid().ToString(),
                ProcessId = CONSTANTS.Null,
                Status = CONSTANTS.Null,
                ModelName = templatedata.ApplicationName + "_" + templatedata.UsecaseName,
                RequestStatus = CONSTANTS.New,
                RetryCount = 0,
                ProblemType = CONSTANTS.Null,
                Message = CONSTANTS.Null,
                UniId = CONSTANTS.Null,
                InstaID = CONSTANTS.Null,
                Progress = CONSTANTS.Null,
                pageInfo = CONSTANTS.TrainAndPredict,
                ParamArgs = VDSInputParams.ToJson(),
                TemplateUseCaseID = templatedata.UsecaseID,
                Function = CONSTANTS.AutoTrain,
                CreatedByUser = UserId,
                CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                ModifiedByUser = UserId,
                ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                LastProcessedOn = CONSTANTS.Null,
                AppID = templatedata.ApplicationID,
                ClientId = RequestPayload.ClientUId,
                DeliveryconstructId = RequestPayload.DeliveryConstructUId,
                UseCaseID = CONSTANTS.Null,
                EstimatedRunTime = CONSTANTS.Null,
                AppURL = RequestPayload.ResponseCallbackUrl,
                ServiceType = Convert.ToString(RequestPayload.DataSourceDetails.ServiceType),
                RequestType = Convert.ToString(RequestPayload.DataSourceDetails.RequestType)
                //RetrainRequired = RequestPayload.RetrainRequired
            };
            return ingrainRequest;
        }
        private string CallbackResponse(IngrainToVDSNotification CallBackResponse, string AppName, string baseAddress, string clientId, string DCId, string applicationId, string usecaseId, string requestId, string userId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(VDSDataService), nameof(CallbackResponse), "CallbackResponse - Started :", applicationId, string.Empty, clientId, DCId);
            string token = CommonUtility.CustomApplicationUrlToken(applicationId, _encryptionDecryption, appSettings);
            if (token == "" || string.IsNullOrEmpty(token))
                return CONSTANTS.ErrorMessage;
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(VDSDataService), nameof(CallbackResponse), "CallbackResponse - token generated ", applicationId, string.Empty, clientId, DCId);
            string contentType = "application/json";
            var Request = JsonConvert.SerializeObject(CallBackResponse);
            using (var Client = new HttpClient())
            {
                Client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(contentType));

                HttpContent httpContent = new StringContent(Request, Encoding.UTF8, "application/json");
                HttpResponseMessage httpResponse = Client.PostAsync(baseAddress, httpContent).Result;
                var statuscode = httpResponse.StatusCode;
                //Log to DB AuditTraiLog
                auditTrailLog.CorrelationId = CallBackResponse.CorrelationId;
                auditTrailLog.Message = CallBackResponse.Message;
                auditTrailLog.ErrorMessage = CallBackResponse.ErrorMessage;
                auditTrailLog.Status = CallBackResponse.Status;
                auditTrailLog.ApplicationName = AppName;
                auditTrailLog.BaseAddress = baseAddress;
                auditTrailLog.httpResponse = httpResponse;
                auditTrailLog.ClientId = CallBackResponse.ClientUId;
                auditTrailLog.DCID = CallBackResponse.DeliveryConstructUId;
                auditTrailLog.ApplicationID = applicationId;
                auditTrailLog.UseCaseId = CallBackResponse.UseCaseId;
                auditTrailLog.RequestId = requestId;
                auditTrailLog.CreatedBy = userId;
                auditTrailLog.ProcessName = "Training";
                if (CallBackResponse.Status == CONSTANTS.ErrorMessage || CallBackResponse.Status == CONSTANTS.E)
                {
                    auditTrailLog.UsageType = "ERROR";
                }
                else
                {
                    auditTrailLog.UsageType = "INFO";
                }
                _VelocityService.AuditTrailLog(auditTrailLog);

                if (httpResponse.IsSuccessStatusCode)
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(VDSDataService), nameof(CallbackResponse), "CallbackResponse - SUCCESS END :" + httpResponse.Content, applicationId, string.Empty, clientId, DCId);
                    return CONSTANTS.success;
                }
                else
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(VDSDataService), nameof(CallbackResponse), "CallbackResponse - ERROR END :" + statuscode + "--" + httpResponse.Content, applicationId, string.Empty, clientId, DCId);
                    return CONSTANTS.ErrorMessage;
                }
            }
        }
        public GenericModelTrainingResponse IngrainAIAppsGenericTrainingResponse(string CorrelationId)
        {
            var ingrainRequestCollection = _database.GetCollection<IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
            var ingrainRequestFilterBuilder = Builders<IngrainRequestQueue>.Filter;
            var ingrainRequestFilterQueue = ingrainRequestFilterBuilder.Where(x => x.CorrelationId == CorrelationId)
                                            & ingrainRequestFilterBuilder.Where(x => x.Function == CONSTANTS.AutoTrain);
            var ingrainRequestQueueResult = ingrainRequestCollection.Find(ingrainRequestFilterQueue).FirstOrDefault();
            _TrainingResponse.CorrelationId = CorrelationId;
            if (ingrainRequestQueueResult == null)
            {
                _TrainingResponse.Message = CONSTANTS.TrainingNotFound + CorrelationId;
                _TrainingResponse.Status = CONSTANTS.ErrorMessage;
            }
            else
            {
                switch (ingrainRequestQueueResult.Status)
                {
                    case CONSTANTS.I:
                        _TrainingResponse.Message = CONSTANTS.TrainingInitiated;
                        _TrainingResponse.Status = "Initiated";
                        break;
                    case CONSTANTS.P:
                        _TrainingResponse.Message = CONSTANTS.TrainingInProgress;
                        _TrainingResponse.Status = CONSTANTS.InProgress;
                        break;
                    case CONSTANTS.C:
                        _TrainingResponse.Message = CONSTANTS.TrainingCompleted;
                        _TrainingResponse.Status = "Completed";
                        break;
                    case CONSTANTS.E:
                        _TrainingResponse.Message = ingrainRequestQueueResult.Message;
                        _TrainingResponse.ErrorMessage = ingrainRequestQueueResult.RequestStatus;
                        _TrainingResponse.Status = CONSTANTS.ErrorMessage;
                        break;
                    default:
                        _TrainingResponse.Message = CONSTANTS.TrainingInProgress;
                        _TrainingResponse.Status = CONSTANTS.InProgress;
                        break;
                }
                if (ingrainRequestQueueResult.Progress.ToString() != CONSTANTS.Null)
                    _TrainingResponse.Progress = ingrainRequestQueueResult.Progress.ToString();
                else
                    _TrainingResponse.Progress = "0%";
                if (ingrainRequestQueueResult.Status == CONSTANTS.E)
                    _TrainingResponse.Progress = "0%";
            }
            return _TrainingResponse;
        }
        #region Generic Prediction API Request and Response
        public VDSUseCasePredictionOutput IntiateVDSGenericModelPrediction(VDSUseCasePredictionRequest VDSUseCasePredictionRequestInput)
        {
            bool DBEncryptionRequired = CommonUtility.EncryptDB(VDSUseCasePredictionRequestInput.CorrelationId, appSettings);
            VDSUseCasePredictionOutput VDSUseCasePredictionOutput = new VDSUseCasePredictionOutput();
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(VDSDataService), nameof(IntiateVDSGenericModelPrediction), "IntiateVDSGenericModelPrediction service Started for CORRELATIONID: " + VDSUseCasePredictionRequestInput.CorrelationId, string.Empty, string.Empty, VDSUseCasePredictionRequestInput.ClientUID, VDSUseCasePredictionRequestInput.DeliveryConstructUID); ;
            VDSUseCasePredictionOutput.UseCaseId = VDSUseCasePredictionRequestInput.UseCaseUID;
            VDSUseCasePredictionOutput.CorrelationId = VDSUseCasePredictionRequestInput.CorrelationId;
            var modelCollection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
            DeployModelsDto deployedModel = null;
            var filter = Builders<DeployModelsDto>.Filter.Where(x => x.CorrelationId == VDSUseCasePredictionRequestInput.CorrelationId)
                            & Builders<DeployModelsDto>.Filter.Where(x => x.Status == "Deployed") & Builders<DeployModelsDto>.Filter.Where(x => x.DeliveryConstructUID == VDSUseCasePredictionRequestInput.DeliveryConstructUID)
                            & Builders<DeployModelsDto>.Filter.Where(x => x.ClientUId == VDSUseCasePredictionRequestInput.ClientUID) & Builders<DeployModelsDto>.Filter.Where(x => x.AppId == VDSUseCasePredictionRequestInput.AppServiceUID)
                            & Builders<DeployModelsDto>.Filter.Where(x => x.TemplateUsecaseId == VDSUseCasePredictionRequestInput.UseCaseUID);
            var projection = Builders<DeployModelsDto>.Projection.Exclude("_id");
            deployedModel = modelCollection.Find(filter).Project<DeployModelsDto>(projection).FirstOrDefault();

            if (deployedModel != null)
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(VDSDataService), nameof(IntiateVDSGenericModelPrediction), "CORRELATIONID :" + VDSUseCasePredictionRequestInput.CorrelationId + "--Deployed", string.Empty, string.Empty, VDSUseCasePredictionRequestInput.ClientUID, VDSUseCasePredictionRequestInput.DeliveryConstructUID);
                ValidRecordsDetailsModel validRecordsDetailModel = CommonUtility.GetModelDataPointdDetails(VDSUseCasePredictionRequestInput.CorrelationId, appSettings);
                if (validRecordsDetailModel != null)
                {
                    if (validRecordsDetailModel.ValidRecordsDetails != null)
                    {
                        if (validRecordsDetailModel.ValidRecordsDetails.Records[0] >= 4 && validRecordsDetailModel.ValidRecordsDetails.Records[0] < 20)
                        {
                            VDSUseCasePredictionOutput.DataPointsWarning = CONSTANTS.DataPointsMinimum;
                            VDSUseCasePredictionOutput.DataPointsCount = validRecordsDetailModel.ValidRecordsDetails.Records[0];
                        }
                    }
                }
                string FunctionType = string.Empty;
                dynamic actualData = string.Empty;

                if (deployedModel.ModelType == CONSTANTS.TimeSeries)
                {
                    FunctionType = CONSTANTS.ForecastModel;
                    actualData = CONSTANTS.Null;
                }
                else
                {
                    FunctionType = CONSTANTS.PublishModel;
                    actualData = Convert.ToString(VDSUseCasePredictionRequestInput.Data);
                }
                PredictionDTO predictionDTO = new PredictionDTO
                {
                    _id = Guid.NewGuid().ToString(),
                    UniqueId = Guid.NewGuid().ToString(),
                    CorrelationId = deployedModel.CorrelationId,
                    Frequency = deployedModel.Frequency,
                    PredictedData = null,
                    Status = CONSTANTS.I,
                    ErrorMessage = null,
                    TempalteUseCaseId = deployedModel.TemplateUsecaseId,
                    AppID = deployedModel.AppId,
                    Progress = null,
                    CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                    ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                    CreatedByUser = DBEncryptionRequired ? _encryptionDecryption.Encrypt(CONSTANTS.System) : CONSTANTS.System,
                    ModifiedByUser = DBEncryptionRequired ? _encryptionDecryption.Encrypt(CONSTANTS.System) : CONSTANTS.System,
                    StartDates = VDSUseCasePredictionRequestInput.StartDates
                };
                if (DBEncryptionRequired)
                {
                    actualData = _encryptionDecryption.Encrypt(actualData);
                }
                predictionDTO.ActualData = actualData.Replace("\r\n", string.Empty);
                var jsonData = JsonConvert.SerializeObject(predictionDTO);
                var insertDocument = BsonSerializer.Deserialize<BsonDocument>(jsonData);
                var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_PublishModel);
                collection.InsertOne(insertDocument);
                Uri apiUri = new Uri(appSettings.Value.VdsURL);
                string host = apiUri.GetLeftPart(UriPartial.Authority);
                string VDSNotificationUrl = host + Convert.ToString(appSettings.Value.VDSPredictionNotificationUrl);
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(VDSDataService), nameof(IntiateVDSGenericModelPrediction), "VDS PredictionRequest: Actual Data inserted for CorrelationId" + deployedModel.CorrelationId, string.Empty, string.Empty, VDSUseCasePredictionRequestInput.ClientUID, VDSUseCasePredictionRequestInput.DeliveryConstructUID);
                Task.Run(() => insertRequest(deployedModel, predictionDTO.UniqueId, FunctionType, VDSNotificationUrl));
                VDSUseCasePredictionOutput.UniqueId = predictionDTO.UniqueId;
                VDSUseCasePredictionOutput.Status = "I";
                VDSUseCasePredictionOutput.StatusMessage = "Prediction request initiated successfully";
                VDSUseCasePredictionOutput.StartTime = predictionDTO.CreatedOn;
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(VDSDataService), nameof(IntiateVDSGenericModelPrediction), "VDS PredictionRequest Initiated for CorrelationId" + deployedModel.CorrelationId, string.Empty, string.Empty, VDSUseCasePredictionRequestInput.ClientUID, VDSUseCasePredictionRequestInput.DeliveryConstructUID);
                //Asset Usage//Log to DB Audit Trail log
                auditTrailLog.CorrelationId = deployedModel.CorrelationId;
                auditTrailLog.ClientId = deployedModel.ClientUId;
                auditTrailLog.DCID = deployedModel.DeliveryConstructUID;
                auditTrailLog.ApplicationID = deployedModel.AppId;
                auditTrailLog.UseCaseId = deployedModel.TemplateUsecaseId;
                auditTrailLog.FeatureName = CONSTANTS.VDSGenericProcess;
                auditTrailLog.ProcessName = CONSTANTS.PredictionName;
                auditTrailLog.UsageType = "INFO";
                auditTrailLog.CreatedBy = CONSTANTS.System;
                auditTrailLog.Message = VDSUseCasePredictionOutput.StatusMessage;
                auditTrailLog.Status = VDSUseCasePredictionOutput.Status;
                auditTrailLog.RequestId = VDSUseCasePredictionOutput.UniqueId;
                auditTrailLog.BaseAddress = null;
                auditTrailLog.httpResponse = null;
                _VelocityService.AuditTrailLog(auditTrailLog);

                LOGGING.LogManager.Logger.LogProcessInfo(typeof(VDSDataService), nameof(IntiateVDSGenericModelPrediction), "VDS PredictionRequest service ended for CorrelationId" + deployedModel.CorrelationId, string.Empty, string.Empty, VDSUseCasePredictionRequestInput.ClientUID, VDSUseCasePredictionRequestInput.DeliveryConstructUID);
                return VDSUseCasePredictionOutput;
            }
            else
            {
                VDSUseCasePredictionOutput.Status = CONSTANTS.E;
                VDSUseCasePredictionOutput.StatusMessage = "Training is not yet completed for the provided input fields, Please try once Training is completed";
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(VDSDataService), nameof(IntiateVDSGenericModelPrediction), "IntiateVDSGenericModelPrediction service ended with " + VDSUseCasePredictionOutput.StatusMessage + "for CorrelationId" + VDSUseCasePredictionRequestInput.CorrelationId, string.Empty, string.Empty, VDSUseCasePredictionRequestInput.ClientUID, VDSUseCasePredictionRequestInput.DeliveryConstructUID);
            return VDSUseCasePredictionOutput;
        }
        public VDSPredictionResponseOutput GetVDSGenericModelPrediction(VDSPredictionResponseInput useCasePredictionResponseInput)
        {
            VDSPredictionResponseOutput useCasePredictionResponseOutput = new VDSPredictionResponseOutput();
            useCasePredictionResponseOutput.CorrelationId = useCasePredictionResponseInput.CorrelationId;
            useCasePredictionResponseOutput.UniqueId = useCasePredictionResponseInput.UniqueId;
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(VDSDataService), nameof(GetVDSGenericModelPrediction), "GetVDSGenericModelPrediction service Start for CorrelationId" + useCasePredictionResponseInput.CorrelationId, string.Empty, string.Empty, string.Empty, useCasePredictionResponseInput.UniqueId);
            try
            {
                var collection = _database.GetCollection<IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
                var builder = Builders<IngrainRequestQueue>.Filter;
                var filter = builder.Eq("UniId", useCasePredictionResponseInput.UniqueId) & (builder.Eq(CONSTANTS.pageInfo, CONSTANTS.ForecastModel) | builder.Eq(CONSTANTS.pageInfo, CONSTANTS.PublishModel));
                var projection = Builders<IngrainRequestQueue>.Projection.Exclude(CONSTANTS.Id);
                IngrainRequestQueue requestQueue = collection.Find(filter).Project<IngrainRequestQueue>(projection).FirstOrDefault();

                if (requestQueue != null)
                {
                    var builder1 = Builders<PredictionDTO>.Filter;
                    var filter1 = builder1.Eq(CONSTANTS.CorrelationId, useCasePredictionResponseInput.CorrelationId) & builder1.Eq(CONSTANTS.UniqueId, useCasePredictionResponseInput.UniqueId);
                    var projection1 = Builders<PredictionDTO>.Projection.Exclude("_id");
                    var collection1 = _database.GetCollection<PredictionDTO>(CONSTANTS.SSAI_PublishModel);
                    var result = collection1.Find(filter1).Project<PredictionDTO>(projection1).ToList();
                    bool DBEncryptionRequired = CommonUtility.EncryptDB(useCasePredictionResponseInput.CorrelationId, appSettings);
                    if (result.Count > 0)
                    {
                        if (DBEncryptionRequired)
                        {
                            if (result[0].PredictedData != null)
                                result[0].PredictedData = _encryptionDecryption.Decrypt(result[0].PredictedData);

                            result[0].ActualData = _encryptionDecryption.Decrypt(result[0].ActualData);
                            try
                            {
                                if (!string.IsNullOrEmpty(Convert.ToString(result[0].CreatedByUser)))
                                {
                                    result[0].CreatedByUser = _encryptionDecryption.Decrypt(Convert.ToString(result[0].CreatedByUser));
                                }
                            }
                            catch (Exception ex) { LOGGING.LogManager.Logger.LogProcessInfo(typeof(VDSDataService), nameof(GetVDSGenericModelPrediction) + "User is already decrypted....", ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty); }
                            try
                            {
                                if (!string.IsNullOrEmpty(Convert.ToString(result[0].ModifiedByUser)))
                                {
                                    result[0].ModifiedByUser = _encryptionDecryption.Decrypt(Convert.ToString(result[0].ModifiedByUser));
                                }
                            }
                            catch (Exception ex) { LOGGING.LogManager.Logger.LogProcessInfo(typeof(VDSDataService), nameof(GetVDSGenericModelPrediction) + "User is already decrypted....", ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty); }
                        }
                        if (result[0].TempalteUseCaseId == "f97739d7-d3b1-491b-8af1-876485cd3d30")
                            useCasePredictionResponseOutput.ModelType = "Classification";
                        else if (result[0].TempalteUseCaseId == "49d56fe0-1eca-4406-8b52-38724ac3b705")
                            useCasePredictionResponseOutput.ModelType = "Timeseries";
                        else if (result[0].TempalteUseCaseId == "7848b5c2-5167-49ea-9148-00be0da491c6")
                            useCasePredictionResponseOutput.ModelType = "Regression";
                        useCasePredictionResponseOutput.Status = result[0].Status;
                        useCasePredictionResponseOutput.PredictedData = result[0].PredictedData;
                        useCasePredictionResponseOutput.StartTime = result[0].CreatedOn;
                        DateTime currentTime = DateTime.Now;
                        DateTime createdTime = DateTime.Parse(result[0].CreatedOn);
                        TimeSpan span = currentTime.Subtract(createdTime);
                        //PREDICTION TIME OUT ERROR
                        if (span.TotalMinutes > Convert.ToDouble(appSettings.Value.PredictionTimeoutMinutes) && result[0].Status != CONSTANTS.C)
                        {
                            useCasePredictionResponseOutput.Message = result[0].ErrorMessage;
                            useCasePredictionResponseOutput.Status = CONSTANTS.E;
                            useCasePredictionResponseOutput.ErrorMessage = CONSTANTS.PredictionTimeOutError;
                            useCasePredictionResponseOutput.EndTime = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                        }
                        if (result[0].Status == CONSTANTS.C)
                        {
                            useCasePredictionResponseOutput.PredictedData = result[0].PredictedData;
                            useCasePredictionResponseOutput.Message = CONSTANTS.PredictionSuccess;
                            useCasePredictionResponseOutput.EndTime = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                        }
                        else if (result[0].Status == CONSTANTS.P)
                        {
                            useCasePredictionResponseOutput.Status = CONSTANTS.P;
                            useCasePredictionResponseOutput.Message = CONSTANTS.InProgress;
                        }
                        else if (result[0].Status == CONSTANTS.E)
                        {
                            useCasePredictionResponseOutput.Status = CONSTANTS.E;
                            useCasePredictionResponseOutput.ErrorMessage = CONSTANTS.PredictionRequestFailed + " with " + result[0].ErrorMessage;
                            useCasePredictionResponseOutput.EndTime = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                        }
                        else
                        {
                            useCasePredictionResponseOutput.Status = CONSTANTS.E;
                            useCasePredictionResponseOutput.ErrorMessage = result[0].ErrorMessage;
                            useCasePredictionResponseOutput.EndTime = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                        }
                    }
                }
                else
                {
                    useCasePredictionResponseOutput.Status = "E";
                    useCasePredictionResponseOutput.ErrorMessage = "Record not found in Request Queue.";
                }
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(VDSDataService), nameof(GetVDSGenericModelPrediction), "GetVDSGenericModelPrediction service ended with ErrorMessage: " + useCasePredictionResponseOutput.ErrorMessage + ", Message: " + useCasePredictionResponseOutput.Message + ", Status: " + useCasePredictionResponseOutput.Status + " for CorrelationId: " + useCasePredictionResponseInput.CorrelationId, string.Empty, string.Empty, string.Empty, useCasePredictionResponseInput.UniqueId);
                return useCasePredictionResponseOutput;

            }
            catch (Exception ex)
            {
                useCasePredictionResponseOutput.Status = "E";
                useCasePredictionResponseOutput.ErrorMessage = "Something wrong in prediction";

                LOGGING.LogManager.Logger.LogErrorMessage(typeof(VDSDataService), nameof(GetVDSGenericModelPrediction), ex.Message + $"   StackTrace = {ex.StackTrace}", ex, string.Empty, string.Empty, useCasePredictionResponseInput.CorrelationId, useCasePredictionResponseInput.UniqueId);
                return useCasePredictionResponseOutput;
            }
        }
        #endregion Generic Prediction API Request and Response
        #endregion Genric Model Training and Prediction for FDS and PAM
        #endregion
    }
}
