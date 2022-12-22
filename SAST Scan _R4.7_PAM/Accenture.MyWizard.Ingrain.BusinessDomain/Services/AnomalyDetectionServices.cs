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
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Accenture.MyWizard.Cryptography.EncryptionProviders;
#endregion

namespace Accenture.MyWizard.Ingrain.BusinessDomain.Services
{
    public class AnomalyDetectionServices : IAnomalyDetection
    {
        #region Members      
        private readonly IOptions<IngrainAppSettings> appSettings;
        DatabaseProvider databaseProvider;
        private MongoClient _mongoClientAD;
        private IMongoDatabase _databaseAD;
        string ServiceName = "Anomaly";

        private IEncryptionDecryption _encryptionDecryption;
        private readonly IIngestedData _ingestedData;
        public static IProcessDataService processDataService { set; get; }
        #endregion

        #region Constructors    
        /// <summary>
        /// Constructor to Initialize the objects
        /// </summary>
        public AnomalyDetectionServices(DatabaseProvider db, IOptions<IngrainAppSettings> settings, IServiceProvider serviceProvider)
        {
            ServiceName = "Anomaly";
            appSettings = settings;
            databaseProvider = db;
            _mongoClientAD = databaseProvider.GetDatabaseConnection(ServiceName);
            var dataBaseName = MongoUrl.Create(appSettings.Value.AnomalyDetectionCS).DatabaseName;
            _databaseAD = _mongoClientAD.GetDatabase(dataBaseName);
            //Services
            _encryptionDecryption = serviceProvider.GetService<IEncryptionDecryption>();
            _ingestedData = serviceProvider.GetService<IIngestedData>();
            processDataService = serviceProvider.GetService<IProcessDataService>();
        }
        #endregion

        #region Methods 
        public UseCaseSave InsertColumns(BusinessProblemDataDTO data)
        {
            UseCaseSave useCase = new UseCaseSave();
            useCase.IsInserted = true;
            try
            {
                var collection = _databaseAD.GetCollection<BusinessProblemDataDTO>(CONSTANTS.PSBusinessProblem);
                string correlationId = data.CorrelationId.ToString();
                var builder = Builders<BsonDocument>.Filter;
                var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
                var filter2 = Builders<BusinessProblemDataDTO>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
                var correaltionExist = collection.Find(filter2).ToList();
                if (correaltionExist.Count > 0)
                {
                    string existingProblemType = string.Empty;
                    if (!string.IsNullOrEmpty(Convert.ToString(correaltionExist[0].ProblemType)))
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionServices), nameof(InsertColumns) + "-INSIDE EXIST 1-" + data, "START", string.IsNullOrEmpty(data.CorrelationId) ? default(Guid) : new Guid(data.CorrelationId), data.AppId, string.Empty, data.ClientUId, data.DeliveryConstructUID);
                        existingProblemType = correaltionExist[0].ProblemType;
                        string UIProblemType = data.ProblemType;
                        if (data.ProblemType == CONSTANTS.File || data.ProblemType == CONSTANTS.TimeSeries)
                        {
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionServices), nameof(InsertColumns) + "-INSIDE EXIST 2-" + data, "START", string.IsNullOrEmpty(data.CorrelationId) ? default(Guid) : new Guid(data.CorrelationId), data.AppId, string.Empty, data.ClientUId, data.DeliveryConstructUID);
                            if (existingProblemType != UIProblemType)
                            {
                                //Flush(data.CorrelationId);
                            }
                        }
                    }
                    collection.DeleteOne(filter2);
                }
                bool DBEncryptionRequired = CommonUtility.EncryptDB(correlationId, appSettings, ServiceName);
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
                
                var collection2 = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.PSBusinessProblem);
                var jsonColumns = Newtonsoft.Json.JsonConvert.SerializeObject(data);
                var insertBsonColumns = BsonSerializer.Deserialize<BsonDocument>(jsonColumns);
                collection2.InsertOne(insertBsonColumns);
            }
            catch (Exception ex)
            {
                useCase.IsInserted = false;
                useCase.ErrorMessage = ex.Message + ex.StackTrace;
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AnomalyDetectionServices), nameof(InsertColumns), ex.Message + "StackTrace-" + ex.StackTrace, ex, data.AppId, string.Empty, data.ClientUId, data.DeliveryConstructUID);
            }
            return useCase;
        }
        public PythonResult GetStatusForDEAndDTProcess(string correlationId, string pageInfo, string userId)
        {
            string Result = string.Empty;
            DataEngineeringDTO dataCleanUpData = new DataEngineeringDTO();
            PythonResult resultPython = new PythonResult();
            var useCaseData = _ingestedData.GetRequestUsecase(correlationId, pageInfo, ServiceName);
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionServices), "GetStatusForDEAndDTProcess" + useCaseData, CONSTANTS.START, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            if (!string.IsNullOrEmpty(useCaseData))
            {
                JObject queueData = JObject.Parse(useCaseData);
                string status = (string)queueData[CONSTANTS.Status];
                string progress = (string)queueData[CONSTANTS.Progress];
                string Message = (string)queueData[CONSTANTS.Message];
                if (status == CONSTANTS.C & progress == CONSTANTS.Hundred)
                {
                    resultPython.message = Message + " with Progress: " + progress;
                    resultPython.status = CONSTANTS.Completed;
                }
                else if (status == CONSTANTS.E)
                {
                    resultPython.message = Message + " with Progress: " + progress;
                    resultPython.status = CONSTANTS.ErrorMessage;
                }
                else if (status == CONSTANTS.P)
                {
                    resultPython.message = Message + " with Progress: " + progress;
                    resultPython.status = CONSTANTS.In_Progress;
                }
                else
                {
                    resultPython.status = CONSTANTS.In_Progress;
                    if (string.IsNullOrEmpty(status))
                        resultPython.message = CONSTANTS.In_Progress;
                    else
                        resultPython.message = Message + " with Progress: " + progress;

                }
            }
            else 
            {
                if (pageInfo == "DataPreprocessing")
                {
                    IngrainRequestQueue ingrainRequest = new IngrainRequestQueue
                    {
                        _id = Guid.NewGuid().ToString(),
                        CorrelationId = correlationId,
                        RequestId = Guid.NewGuid().ToString(),
                        ProcessId = null,
                        Status = null,
                        ModelName = null,
                        RequestStatus = "New",
                        RetryCount = 0,
                        ProblemType = null,
                        Message = null,
                        UniId = null,
                        Progress = null,
                        pageInfo = "DataPreprocessing",
                        ParamArgs = "{}",
                        Function = "DataTransform",
                        CreatedByUser = userId,
                        CreatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        ModifiedByUser = userId,
                        ModifiedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        LastProcessedOn = null,
                    };
                    _ingestedData.InsertRequests(ingrainRequest, ServiceName);
                    Thread.Sleep(2000);
                    resultPython.message = "DataPreprocessing Record Inserted";
                    resultPython.status = CONSTANTS.In_Progress;
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionServices), nameof(GetStatusForDEAndDTProcess), "DataTransform record inserted", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                }
                else
                {
                    resultPython.message = "Python Error: Data Curation record is not inserted.";
                    resultPython.status = CONSTANTS.ErrorMessage;
                }
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionServices), nameof(GetStatusForDEAndDTProcess), CONSTANTS.End, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            return resultPython;
        }
        public string InsertRecommendedModelDtls(string correlationId, string modelType, string userId)
        {
            double EstimatedRunTime = 0;
            string res = "Success";
            try
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionServices), "InsertRecommendedModelDtls- ", CONSTANTS.START, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                var featureCollection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.MEFeatureSelection);
                var recommendedModelCollection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.MERecommendedModels);
                var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
                var meprojection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
                var meResult = recommendedModelCollection.Find(filter).Project<BsonDocument>(meprojection).ToList();
                if (meResult.Count > 0)
                {
                    return res;
                    //recommendedModelCollection.DeleteOne(filter);
                }
                var featureData = new List<BsonDocument>();
                var projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id).Include("EstimatedRunTime");
                featureData = featureCollection.Find(filter).Project<BsonDocument>(projection).ToList();
                if (featureData.Count > 0)
                {
                    if (featureData[0].Contains("EstimatedRunTime") && featureData[0].AsBsonDocument.TryGetElement("EstimatedRunTime", out BsonElement bson))
                        EstimatedRunTime = Convert.ToDouble(featureData[0]["EstimatedRunTime"]);
                    //else
                    //    return "Python Error: EstimatedRunTime is null";
                }
                bool DBEncryptionRequired = CommonUtility.EncryptDB(correlationId, appSettings, ServiceName);
                if (DBEncryptionRequired)
                    userId = _encryptionDecryption.Encrypt(Convert.ToString(userId));
                JObject SelectedModels = new JObject();
                string Models = string.Empty;
                if (modelType == CONSTANTS.Regression || modelType == CONSTANTS.File)
                    Models = appSettings.Value.AnomalyDetectionRegressionModels;
                else if (modelType == CONSTANTS.TimeSeries)
                    Models = appSettings.Value.AnomalyDetectionTimeseriesModels;
                if (Models == null || Models == "")
                    return "Recommended Model Configuration is null.";
                string[] ModelList = Models.Split(CONSTANTS.comma_);
                foreach (var item in ModelList)
                {
                    SelectedModels.Add(item, new JObject(new JProperty("Train_model", "True"), new JProperty("EstimatedRunTime", EstimatedRunTime)));
                }
                RecommendedTrainedModelAD recommendedTrainedModelAD = new RecommendedTrainedModelAD
                {
                    CorrelationId = correlationId,
                    CreatedByUser = userId,
                    CreatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    ModifiedByUser = userId,
                    ModifiedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    ProblemType = modelType,
                    SelectedModels = SelectedModels,
                    pageInfo = "DataPreprocessing",
                    retrain = false,
                };
                var jsonColumns = Newtonsoft.Json.JsonConvert.SerializeObject(recommendedTrainedModelAD);
                var insertBsonColumns = BsonSerializer.Deserialize<BsonDocument>(jsonColumns);
                recommendedModelCollection.InsertOne(insertBsonColumns);
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionServices), nameof(InsertRecommendedModelDtls), CONSTANTS.End, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            }
            catch (Exception ex)
            {
                res = "Exception: " + ex.Message;
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AnomalyDetectionServices), nameof(InsertRecommendedModelDtls), CONSTANTS.End + " " + ex.Message + "StackTrace-" + ex.StackTrace, ex, correlationId, modelType, String.Empty, String.Empty);
            }
            return res;
        }
        public IngrainRequestQueue GetRequestStatusbyCoridandPageInfo(string correlationId, string pageInfo)
        {
            var collection = _databaseAD.GetCollection<IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
            IngrainRequestQueue ingrainRequest = new IngrainRequestQueue();
            var builder = Builders<IngrainRequestQueue>.Filter;
            var filter = builder.Eq(CONSTANTS.CorrelationId, correlationId) & builder.Eq(CONSTANTS.pageInfo, pageInfo);
            return ingrainRequest = collection.Find(filter).ToList().FirstOrDefault();
        }
        //public RecommedAITrainedModel GetPublishedModels(string correlationId)
        //{
        //    bool DBEncryptionRequired = CommonUtility.EncryptDB(correlationId, appSettings, ServiceName);
        //    string problemType = string.Empty;
        //    List<JObject> trainModelsList = new List<JObject>();
        //    RecommedAITrainedModel trainedModels = new RecommedAITrainedModel();
        //    var columnCollection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.SSAIRecommendedTrainedModels);
        //    var cascadeCollection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.SSAICascadedModels);
        //    var modelCollection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
        //    var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
        //    var problemProjection = Builders<BsonDocument>.Projection.Include(CONSTANTS.ProblemType).Exclude(CONSTANTS.Id);
        //    var problemtypeResult = columnCollection.Find(filter).Project<BsonDocument>(problemProjection).ToList();
        //    {
        //        if (problemtypeResult.Count > 0)
        //        {
        //            problemType = problemtypeResult[0][CONSTANTS.ProblemType].ToString();
        //        }
        //        ProjectionDefinition<BsonDocument> projection = null;
        //        //if (problemType == "regression" || problemType == "TimeSeries")
        //        if (problemType.Equals(CONSTANTS.regression, StringComparison.InvariantCultureIgnoreCase) || problemType == CONSTANTS.TimeSeries)
        //        {
        //            projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.modelName).Include(CONSTANTS.r2ScoreVal_error_rate).Include(CONSTANTS.Id).Include(CONSTANTS.Frequency).Include("Version");
        //        }
        //        else
        //        {
        //            projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.modelName).Include(CONSTANTS.Accuracy).Include(CONSTANTS.Id).Include("Version");
        //        }

        //        var trainedModel = columnCollection.Find(filter).Project<BsonDocument>(projection).ToList();
        //        if (trainedModel.Count() > 0)
        //        {
        //            bool versionAvaialble = false;
        //            foreach (var item in trainedModel)
        //            {
        //                if (!item["Version"].IsBsonNull && item["Version"].ToString() != null)
        //                {
        //                    int val = Convert.ToInt32(item["Version"]);
        //                    if (val == 1)
        //                    {
        //                        versionAvaialble = true;
        //                        break;
        //                    }
        //                }
        //            }
        //            for (int i = 0; i < trainedModel.Count; i++)
        //            {
        //                JObject parsedData = JObject.Parse(trainedModel[i].ToString());
        //                if (versionAvaialble)
        //                {
        //                    if (!string.IsNullOrEmpty(Convert.ToString(parsedData["Version"])))
        //                    {
        //                        if (Convert.ToInt32(parsedData["Version"]) == 1)
        //                        {
        //                            trainModelsList.Add(parsedData);
        //                        }
        //                    }
        //                }
        //                else
        //                    trainModelsList.Add(parsedData);
        //            }
        //            trainedModels.TrainedModel = trainModelsList;
        //        }
        //        var filterBuilder2 = Builders<BsonDocument>.Filter;
        //        ProjectionDefinition<BsonDocument> projectionHyperTune = null;
        //        IMongoCollection<BsonDocument> hyperTuneCollection;
        //        if (servicename == "Anomaly")
        //            hyperTuneCollection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.ME_HyperTuneVersion);
        //        else
        //            hyperTuneCollection = _database.GetCollection<BsonDocument>(CONSTANTS.ME_HyperTuneVersion);
        //        //var hyperTuneCollection = _database.GetCollection<BsonDocument>(CONSTANTS.ME_HyperTuneVersion);
        //        var filterHyperTune = filterBuilder2.Eq(CONSTANTS.CorrelationId, correlationId) & filterBuilder2.Eq(CONSTANTS.Temp, true);
        //        if (problemType.Equals(CONSTANTS.regression, StringComparison.InvariantCultureIgnoreCase) || problemType == CONSTANTS.TimeSeries)
        //        {
        //            projectionHyperTune = Builders<BsonDocument>.Projection.Include(CONSTANTS.VersionName).Include(CONSTANTS.r2ScoreVal_error_rate).Include(CONSTANTS.Id);
        //        }
        //        else
        //        {
        //            projectionHyperTune = Builders<BsonDocument>.Projection.Include(CONSTANTS.VersionName).Include(CONSTANTS.Accuracy).Include(CONSTANTS.Id);
        //        }

        //        var hyperTuneModel = hyperTuneCollection.Find(filter).Project<BsonDocument>(projectionHyperTune).ToList();

        //        if (hyperTuneModel.Count() > 0)
        //        {
        //            for (int i = 0; i < hyperTuneModel.Count; i++)
        //            {
        //                JObject j = new JObject();
        //                j[CONSTANTS.Id] = hyperTuneModel[i][CONSTANTS.Id].ToString();
        //                j[CONSTANTS.modelName] = hyperTuneModel[i][CONSTANTS.VersionName].ToString();
        //                if (problemType.Equals(CONSTANTS.regression, StringComparison.InvariantCultureIgnoreCase) || problemType == CONSTANTS.TimeSeries)
        //                {
        //                    JObject outlierData = new JObject();
        //                    outlierData[CONSTANTS.error_rate] = hyperTuneModel[i][CONSTANTS.r2ScoreVal][CONSTANTS.error_rate].ToString();
        //                    j[CONSTANTS.r2ScoreVal] = JObject.FromObject(outlierData);
        //                }
        //                else
        //                {
        //                    j[CONSTANTS.Accuracy] = hyperTuneModel[i][CONSTANTS.Accuracy].ToString();
        //                }
        //                if (!(j[CONSTANTS.modelName].ToString() == CONSTANTS.BsonNull))
        //                {
        //                    trainModelsList.Add(j);
        //                }
        //            }
        //            trainedModels.TrainedModel = trainModelsList;
        //        }
        //    }

        //    //var modelCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
        //    var projection1 = Builders<BsonDocument>.Projection.Include(CONSTANTS.ModelVersion).Exclude(CONSTANTS.Id);
        //    var projection2 = Builders<BsonDocument>.Projection.Include(CONSTANTS.IsFMModel).Include(CONSTANTS.IsCascadingButton).Include("IsModelTemplateDataSource").Exclude(CONSTANTS.Id);
        //    var filterBuilder = Builders<BsonDocument>.Filter;
        //    var filter2 = filterBuilder.Eq(CONSTANTS.CorrelationId, correlationId) & filterBuilder.Eq(CONSTANTS.Status, CONSTANTS.Deployed);
        //    var modelsData = modelCollection.Find(filter2).Project<BsonDocument>(projection1).ToList();
        //    var modelTemplateDataSource = modelCollection.Find(filter).Project<BsonDocument>(projection2).ToList();
        //    List<DeployedModelVersions> modelVersions = new List<DeployedModelVersions>();
        //    if (modelsData.Count > 0)
        //    {
        //        for (int i = 0; i < modelsData.Count; i++)
        //        {
        //            if (DBEncryptionRequired)
        //            {
        //                try
        //                {
        //                    if (modelsData[i].Contains(CONSTANTS.CreatedByUser) && !string.IsNullOrEmpty(Convert.ToString(modelsData[i][CONSTANTS.CreatedByUser])))
        //                    {
        //                        modelsData[i][CONSTANTS.CreatedByUser] = _encryptionDecryption.Decrypt(Convert.ToString(modelsData[i][CONSTANTS.CreatedByUser]));
        //                    }
        //                }
        //                catch (Exception ex) { LOGGING.LogManager.Logger.LogProcessInfo(typeof(DeployModelServices), nameof(GetPublishedModels) + "User is already decrypted....", ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty); }
        //                try
        //                {
        //                    if (modelsData[i].Contains(CONSTANTS.ModifiedByUser) && !string.IsNullOrEmpty(Convert.ToString(modelsData[i][CONSTANTS.ModifiedByUser])))
        //                    {
        //                        modelsData[i][CONSTANTS.ModifiedByUser] = _encryptionDecryption.Decrypt(Convert.ToString(modelsData[i][CONSTANTS.ModifiedByUser]));
        //                    }
        //                }
        //                catch (Exception ex) { LOGGING.LogManager.Logger.LogProcessInfo(typeof(DeployModelServices), nameof(GetPublishedModels) + "User is already decrypted....", ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty); }
        //            }
        //            var versionsData = JsonConvert.DeserializeObject<DeployedModelVersions>(modelsData[i].ToString());
        //            modelVersions.Add(versionsData);

        //        }
        //    }
        //    if (modelTemplateDataSource.Count > 0)
        //    {
        //        if (modelTemplateDataSource[0].Contains(CONSTANTS.IsFMModel))
        //        {
        //            trainedModels.IsFmModel = modelTemplateDataSource[0][CONSTANTS.IsFMModel].ToBoolean();
        //        }
        //        if (modelTemplateDataSource[0].Contains("IsModelTemplateDataSource"))
        //        {
        //            trainedModels.IsModelTemplateDataSource = modelTemplateDataSource[0]["IsModelTemplateDataSource"].ToBoolean();
        //        }
        //        if (modelTemplateDataSource[0].Contains(CONSTANTS.IsCascadingButton))
        //        {
        //            trainedModels.IsCascadingButton = Convert.ToBoolean(modelTemplateDataSource[0][CONSTANTS.IsCascadingButton]);
        //        }
        //    }
        //    trainedModels.DeployedModelVersions = modelVersions;
        //    ValidRecordsDetailsModel validRecordsDetailModel = CommonUtility.GetModelDataPointdDetails(correlationId, appSettings, servicename);
        //    if (validRecordsDetailModel != null)
        //    {
        //        if (validRecordsDetailModel.ValidRecordsDetails != null)
        //        {
        //            if (validRecordsDetailModel.ValidRecordsDetails.Records[0] >= 4 && validRecordsDetailModel.ValidRecordsDetails.Records[0] < 20)
        //            {
        //                trainedModels.DataPointsWarning = CONSTANTS.DataPointsMinimum;
        //                trainedModels.DataPointsCount = validRecordsDetailModel.ValidRecordsDetails.Records[0];
        //            }
        //        }
        //    }
        //    List<string> datasource = new List<string>();
        //    datasource = CommonUtility.GetDataSourceModel(correlationId, appSettings, servicename);
        //    List<string> datasourceDetails = new List<string>();
        //    datasourceDetails = CommonUtility.GetDataSourceModelDetails(correlationId, appSettings, servicename);
        //    if (cascadeResult.Count > 0)
        //    {
        //        trainedModels.ModelName = datasource[0];
        //        trainedModels.DataSource = datasource[1];
        //        trainedModels.ModelType = problemType;
        //        trainedModels.Category = datasource[3];
        //        trainedModels.BusinessProblems = datasource[1];
        //    }
        //    else
        //    {
        //        if (datasource.Count > 0)
        //        {
        //            trainedModels.ModelName = datasource[0];
        //            trainedModels.DataSource = datasource[1];
        //            trainedModels.ModelType = datasource[2];
        //            if (datasource.Count > 2)
        //                trainedModels.BusinessProblems = datasource[3];
        //            if (!string.IsNullOrEmpty(datasource[4]))
        //            {
        //                trainedModels.InstaFlag = true;
        //                trainedModels.Category = datasource[5];
        //            }
        //        }
        //    }
        //    trainedModels.Category = datasourceDetails[3];
        //    return trainedModels;
        //}
        public dynamic GetEncryptedDecryptedValue(string Value, string AesKey, string AesVector, bool IsEncryption)
        {
            if (IsEncryption)
                return AesProvider.Encrypt(Value, AesKey, AesVector);
            else
                return AesProvider.Decrypt(Value, AesKey, AesVector);
        }
        #endregion
    }
}
