using Accenture.MyWizard.SelfServiceAI.WindowService.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Threading;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
using Accenture.MyWizard.Ingrain.WindowService;
using Microsoft.Extensions.Configuration;
using Accenture.MyWizard.Ingrain.DataAccess;
using Accenture.MyWizard.Ingrain.WindowService.Models;
using MongoDB.Bson;
using Accenture.MyWizard.Ingrain.DataModels.Models;
using MongoDB.Driver;
using Accenture.MyWizard.Ingrain.WindowService.Interfaces;
using Accenture.MyWizard.Ingrain.DataModels.InstaModels;
using Newtonsoft.Json;
using MongoDB.Bson.Serialization;
using Accenture.MyWizard.Ingrain.DataModels;
using Newtonsoft.Json.Linq;
using Accenture.MyWizard.Ingrain.BusinessDomain.Common;
using Accenture.MyWizard.Shared.Helpers;
using Microsoft.Extensions.Options;
using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
using Accenture.MyWizard.Cryptography.EncryptionProviders;
using CryptographyHelper = Accenture.MyWizard.Cryptography;

namespace Accenture.MyWizard.Ingrain.WindowService.Services
{
    public class InstaRegressionAutoTrainService : IInstaRegressionAutoTrainService
    {

        #region Private Members       
        private PreProcessDTO _preProcessDTO;
        private readonly DatabaseProvider databaseProvider;
        private MongoClient _mongoClient;
        private IMongoDatabase _database;
        private IMongoCollection<IngrainRequestQueue> _collection;
        private IMongoCollection<BsonDocument> _deployModelCollection;
        //private readonly IOptions<IngrainAppSettings> appSettings;
        bool insertSuccess = false;
        private RecommedAITrainedModel _recommendedAI;
        private DeployModelViewModel _deployModelViewModel;
        private IConfigurationRoot appSettings;
        private string source;
        private string _aesKey;
        private string _aesVector;
        private RegressionRetrain _regressionRetrain = null;
        private bool _IsAESKeyVault;
        static CryptographyHelper.Utility.CryptographyUtility CryptographyUtility = null;
        #endregion
        public InstaRegressionAutoTrainService(DatabaseProvider db)
        {
            databaseProvider = db;
            appSettings = AppSettingsJson.GetAppSettings();
            _mongoClient = databaseProvider.GetDatabaseConnection();
            var dataBaseName = MongoUrl.Create(appSettings.GetSection("AppSettings").GetSection("connectionString").Value).DatabaseName;
            _database = _mongoClient.GetDatabase(dataBaseName);
            _collection = _database.GetCollection<IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
            _deployModelCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
            _preProcessDTO = new PreProcessDTO();
            _recommendedAI = new RecommedAITrainedModel();
            _deployModelViewModel = new DeployModelViewModel();
            source = appSettings.GetSection("AppSettings").GetSection("Source").Value;
            _regressionRetrain = new RegressionRetrain();
            _aesKey = appSettings.GetSection("AppSettings").GetSection("aesKey").Value;
            _aesVector = appSettings.GetSection("AppSettings").GetSection("aesVector").Value;
            _IsAESKeyVault=Convert.ToBoolean(appSettings.GetSection("AppSettings").GetSection("IsAESKeyVault").Value);
            CryptographyUtility = new CryptographyHelper.Utility.CryptographyUtility();
        }
        public RegressionRetrain StartRegressionModelTraining(List<DeployModelsDto> deployModelList)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaRegressionAutoTrainService), nameof(StartRegressionModelTraining), "REGRESSION TRAINING START", string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                var useCaseIdsList = deployModelList.GroupBy(x => x.UseCaseID).Select(x => x.Key).ToArray();
                foreach (var usecaseID in useCaseIdsList)
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaRegressionAutoTrainService), nameof(StartRegressionModelTraining), "REGRESSION STARTING 2  START ", string.Empty, string.Empty, string.Empty, string.Empty);
                    List<TrainedModels> timeSeriesModels = new List<TrainedModels>();
                    //Dictionary<string, bool> timeSeriesModels = new Dictionary<string, bool>();
                    bool isTimeSeriesSuccess = false;
                    var modelsList = deployModelList.Where(x => x.UseCaseID == usecaseID);
                    var regModel = modelsList.Where(x => x.ModelType == CONSTANTS.Regression).FirstOrDefault();

                    int timeSeriesCount = deployModelList.Where(x => x.ModelType == CONSTANTS.TimeSeries).Count();
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaRegressionAutoTrainService), nameof(StartRegressionModelTraining), "REGRESSION STARTING 3  START -- " + timeSeriesCount, regModel.AppId, string.Empty, regModel.ClientUId, regModel.DeliveryConstructUID);
                    int modelCount = 0;
                    
                    
                    //Regression TimeSeries Model Training

                    List<InstaPayload> instaPayloads = new List<InstaPayload>();

                    foreach (var model in modelsList)
                    {
                        string targetColumn = GetTargetColumn(model);
                        string src = string.Empty;
                        if (model.LinkedApps[0] == CONSTANTS.VDS_AIOPS)
                        {
                            src = CONSTANTS.VDS_AIOPS;
                        }
                        InstaPayload instaPayload = new InstaPayload
                        {
                            InstaId = model.InstaId,
                            CorrelationId = model.CorrelationId,
                            Dimension = "Date",
                            TargetColumn = targetColumn,
                            ProblemType = model.ModelType,
                            UseCaseId = model.UseCaseID,
                            Source = src
                        };
                        instaPayloads.Add(instaPayload);
                    }

                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaRegressionAutoTrainService), nameof(StartRegressionModelTraining), "REGRESSION STARTING 4  START -- " + regModel.CorrelationId, regModel.AppId, string.Empty, regModel.ClientUId, regModel.DeliveryConstructUID);
                    _regressionRetrain = RegressionIngestData(regModel, instaPayloads);
                    #region timeSeries Models Training
                    foreach (var deployModel in modelsList)
                    {
                        if (deployModel.ModelType == CONSTANTS.TimeSeries)
                        {

                            //if (usecaseID == "246")
                            //{                       
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaRegressionAutoTrainService), nameof(StartRegressionModelTraining), "REGRESSION STARTING 45  START -- " + _regressionRetrain.Status, deployModel.AppId, string.Empty, deployModel.ClientUId, deployModel.DeliveryConstructUID);
                            //_regressionRetrain = RegressionIngestData(deployModel);
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaRegressionAutoTrainService), nameof(StartRegressionModelTraining), "REGRESSION STARTING 5  START -- " + _regressionRetrain.Status, deployModel.AppId, string.Empty, deployModel.ClientUId, deployModel.DeliveryConstructUID);
                            if (_regressionRetrain.Status == CONSTANTS.C)
                            {
                                _regressionRetrain.Status = CONSTANTS.E;
                                GetRegressionDataEngineering(deployModel);
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaRegressionAutoTrainService), nameof(StartRegressionModelTraining), "REGRESSION STARTING DE 6  START -- " + _regressionRetrain.Status, deployModel.AppId, string.Empty, deployModel.ClientUId, deployModel.DeliveryConstructUID);
                                if (_regressionRetrain.Status == CONSTANTS.C)
                                {
                                    _regressionRetrain.Status = CONSTANTS.E;
                                    GetRegressionModelEngineering(deployModel);
                                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaRegressionAutoTrainService), nameof(StartRegressionModelTraining), "REGRESSION STARTING ME 7  START -- " + _regressionRetrain.Status, deployModel.AppId, string.Empty, deployModel.ClientUId, deployModel.DeliveryConstructUID);
                                    if (_regressionRetrain.Status == CONSTANTS.C)
                                    {
                                        TrainedModels seriesModel = new TrainedModels();
                                        seriesModel.CorrelationId = deployModel.CorrelationId;
                                        seriesModel.UserID = deployModel.CreatedByUser;
                                        seriesModel.IsSuccess = true;
                                        timeSeriesModels.Add(seriesModel);
                                        _regressionRetrain.Status = CONSTANTS.E;
                                        //GetRegressionDeployPrediction(deployModel);                                            
                                    }
                                    else
                                    {
                                        TrainedModels seriesModel = new TrainedModels();
                                        seriesModel.CorrelationId = deployModel.CorrelationId;
                                        seriesModel.UserID = deployModel.CreatedByUser;
                                        seriesModel.IsSuccess = false;
                                        timeSeriesModels.Add(seriesModel);
                                        foreach (var item in timeSeriesModels)
                                        {
                                            RevertModelsState(item.CorrelationId);
                                        }
                                        break;
                                    }
                                }
                                else
                                    break;
                            }
                            else
                                break;
                            // }
                        }
                    }
                    int count = timeSeriesModels.Where(x => x.IsSuccess == false).Count();
                    if (count > 0)
                    {
                        foreach (var item in timeSeriesModels)
                        {
                            RevertModelsState(item.CorrelationId);
                            break;
                        }
                    }
                    else
                    {
                        foreach (var item in timeSeriesModels)
                        {
                            DeleteBackupRecords(item.CorrelationId);
                            GetRegressionDeployPrediction(item.CorrelationId, CONSTANTS.TimeSeries, item.UserID);
                            if (_regressionRetrain.Status == CONSTANTS.C)
                            {
                                isTimeSeriesSuccess = true;
                                modelCount++;
                            }
                        }
                    }
                    #endregion
                    //Regression Model Training Starting, once TimeSeries Models Trained.
                    #region Regression Model Training
                    if (timeSeriesCount == modelCount & isTimeSeriesSuccess)
                    {
                        foreach (var regressionModel in modelsList)
                        {
                            if (regressionModel.ModelType == CONSTANTS.Regression)
                            {
                                //_regressionRetrain = RegressionIngestData(regressionModel);
                                if (_regressionRetrain.Status == CONSTANTS.C)
                                {
                                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaRegressionAutoTrainService), nameof(StartRegressionModelTraining), "REGRESSION STARTING   START -- " + _regressionRetrain.Status, regressionModel.AppId, string.Empty, regressionModel.ClientUId, regressionModel.DeliveryConstructUID);
                                    _regressionRetrain.Status = CONSTANTS.E;
                                    GetRegressionDataEngineering(regressionModel);
                                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaRegressionAutoTrainService), nameof(StartRegressionModelTraining), "REGRESSION STARTING DE   START -- " + _regressionRetrain.Status, regressionModel.AppId, string.Empty, regressionModel.ClientUId, regressionModel.DeliveryConstructUID);
                                    if (_regressionRetrain.Status == CONSTANTS.C)
                                    {
                                        _regressionRetrain.Status = CONSTANTS.E;
                                        GetRegressionModelEngineering(regressionModel);
                                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaRegressionAutoTrainService), nameof(StartRegressionModelTraining), "REGRESSION STARTING ME  START -- " + _regressionRetrain.Status, regressionModel.AppId, string.Empty, regressionModel.ClientUId, regressionModel.DeliveryConstructUID);
                                        if (_regressionRetrain.Status == CONSTANTS.C)
                                        {
                                            DeleteBackupRecords(regressionModel.CorrelationId);
                                            _regressionRetrain.Status = CONSTANTS.E;
                                            GetRegressionDeployPrediction(regressionModel.CorrelationId, regressionModel.ModelType, regressionModel.CreatedByUser);
                                            if (_regressionRetrain.Status == CONSTANTS.C)
                                            {
                                                break;
                                            }
                                            else
                                                break;
                                        }
                                        else
                                        {
                                            RevertModelsState(regressionModel.CorrelationId);
                                            break;
                                        }
                                    }
                                    else
                                        break;
                                }
                                else
                                    break;
                            }
                        }
                    }
                    #endregion
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InstaRegressionAutoTrainService), nameof(StartRegressionModelTraining), ex.Message + "- STACKTRACE - " + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaRegressionAutoTrainService), nameof(StartRegressionModelTraining), "REGRESSION TRAINING END", string.Empty, string.Empty, string.Empty, string.Empty);
            return _regressionRetrain;
        }
        //private RegressionRetrain RegressionIngestData(DeployModel deployModel)
        //{
        //    LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaRegressionAutoTrainService), nameof(RegressionIngestData), "REGRESSIONINGESTDATA START", new Guid(deployModel.CorrelationId));
        //    try
        //    {
        //        IngestData(deployModel);
        //    }
        //    catch (Exception ex)
        //    {
        //        LOGGING.LogManager.Logger.LogErrorMessage(typeof(InstaRegressionAutoTrainService), nameof(RegressionIngestData), ex.Message + "- STACKTRACE - " + ex.StackTrace + "CorrelationId-" + new Guid(deployModel.CorrelationId), ex);
        //    }
        //    LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaRegressionAutoTrainService), nameof(RegressionIngestData), "REGRESSIONINGESTDATA END", new Guid(deployModel.CorrelationId));
        //    return _regressionRetrain;
        //}
        private RegressionRetrain RegressionIngestData(DeployModelsDto deployModel, List<InstaPayload> instaPayloads)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaRegressionAutoTrainService), nameof(RegressionIngestData), "REGRESSIONINGESTDATA START", string.IsNullOrEmpty(deployModel.CorrelationId) ? default(Guid) : new Guid(deployModel.CorrelationId), deployModel.AppId, string.Empty, deployModel.ClientUId, deployModel.DeliveryConstructUID);
            try
            {
                IngestData(deployModel, instaPayloads);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InstaRegressionAutoTrainService), nameof(RegressionIngestData), ex.Message + "- STACKTRACE - " + ex.StackTrace + "CorrelationId-" + (string.IsNullOrEmpty(deployModel.CorrelationId) ? default(Guid) : new Guid(deployModel.CorrelationId)), ex, deployModel.AppId, string.Empty, deployModel.ClientUId, deployModel.DeliveryConstructUID);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaRegressionAutoTrainService), nameof(RegressionIngestData), "REGRESSIONINGESTDATA END", string.IsNullOrEmpty(deployModel.CorrelationId) ? default(Guid) : new Guid(deployModel.CorrelationId), deployModel.AppId, string.Empty, deployModel.ClientUId, deployModel.DeliveryConstructUID);
            return _regressionRetrain;
        }
        private RegressionRetrain GetRegressionDataEngineering(DeployModelsDto deployModel)
        {
            _regressionRetrain.Status = null;
            _regressionRetrain.Message = null;
            bool isDataCurationCompleted = false;
            bool isDataTransformationCompleted = false;
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaRegressionAutoTrainService), nameof(GetRegressionDataEngineering), "REGRESSION DATAENGINEERING START", string.IsNullOrEmpty(deployModel.CorrelationId) ? default(Guid) : new Guid(deployModel.CorrelationId), deployModel.AppId, string.Empty, deployModel.ClientUId, deployModel.DeliveryConstructUID);
            try
            {
                DataEngineering dataEngineering = new DataEngineering();
                dataEngineering = GetDataCuration(deployModel.CorrelationId, CONSTANTS.DataCleanUp, deployModel.CreatedByUser, deployModel);
                _regressionRetrain.Status = dataEngineering.Status;
                _regressionRetrain.Message = dataEngineering.Message;
                if (dataEngineering.Status == CONSTANTS.C)
                {
                    isDataCurationCompleted = IsDataCurationComplete(deployModel.CorrelationId);
                }
                if (isDataCurationCompleted)
                {
                    //LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaRegressionAutoTrainService), "DATACURATION", "DATACURATION COMPLETED-", new Guid(deployModel.CorrelationId));
                    PreProcessModelDTO preProcessModelDTO = new PreProcessModelDTO();
                    RemoveQueueRecords(deployModel.CorrelationId, CONSTANTS.DataPreprocessing);
                    isDataTransformationCompleted = CreatePreprocess(deployModel.CorrelationId, deployModel.CreatedByUser, CONSTANTS.TimeSeries, deployModel.InstaId);
                    if (isDataTransformationCompleted)
                    {
                        dataEngineering = GetDatatransformation(deployModel.CorrelationId, CONSTANTS.DataPreprocessing, deployModel.CreatedByUser, deployModel);
                        _regressionRetrain.Status = dataEngineering.Status;
                        _regressionRetrain.Message = dataEngineering.Message;
                        if (dataEngineering.Status == CONSTANTS.C)
                        {
                            _regressionRetrain.Status = dataEngineering.Status;
                            _regressionRetrain.Success = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InstaRegressionAutoTrainService), nameof(GetRegressionDataEngineering), ex.Message + "- STACKTRACE - " + ex.StackTrace + "CorrelationId-" + (string.IsNullOrEmpty(deployModel.CorrelationId) ? default(Guid) : new Guid(deployModel.CorrelationId)), ex, deployModel.AppId, string.Empty, deployModel.ClientUId, deployModel.DeliveryConstructUID);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaRegressionAutoTrainService), nameof(GetRegressionDataEngineering), "REGRESSION DATAENGINEERING END", string.IsNullOrEmpty(deployModel.CorrelationId) ? default(Guid) : new Guid(deployModel.CorrelationId), deployModel.AppId, string.Empty, deployModel.ClientUId, deployModel.DeliveryConstructUID);
            return _regressionRetrain;
        }
        private RegressionRetrain GetRegressionModelEngineering(DeployModelsDto result)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaRegressionAutoTrainService), nameof(GetRegressionModelEngineering), "REGRESSION GETREGRESSIONMODELENGINEERING START", string.IsNullOrEmpty(result.CorrelationId) ? default(Guid) : new Guid(result.CorrelationId), result.AppId, string.Empty, result.ClientUId, result.DeliveryConstructUID);
            try
            {
                _regressionRetrain.Status = CONSTANTS.E;
                _regressionRetrain.Success = false;
                //int logCount = 0;
                int errorCount = 0;
                int noOfModelsSelected = Convert.ToInt32(appSettings.GetSection("AppSettings").GetSection("Insta_AutoModels").Value);
                string pythonResult = string.Empty;
                bool isModelTrained = true;
                IngrainRequestQueue ingrainRequest = new IngrainRequestQueue();
                //taking the backup                
                updateExistingModels(result.CorrelationId, CONSTANTS.RecommendedAI);
                RemoveQueueRecords(result.CorrelationId, CONSTANTS.RecommendedAI);
                //invoke python
                while (isModelTrained)
                {
                    int modelsCount = 0;
                ExecuteQueueTable:
                    var useCaseDetails = GetMultipleRequestStatus(result.CorrelationId, CONSTANTS.RecommendedAI);
                    if (useCaseDetails.Count > 0)
                    {
                        for (int i = 0; i < useCaseDetails.Count; i++)
                        {
                            string queueStatus = useCaseDetails[i].Status;
                            if (queueStatus == CONSTANTS.C)
                            {
                                modelsCount++;
                            }
                            if (queueStatus == "E")
                            {
                                errorCount++;
                            }
                        }
                        if (errorCount > 1)
                        {
                            _regressionRetrain.Status = "E";
                            _regressionRetrain.Success = false;
                            _regressionRetrain.PageInfo = CONSTANTS.RecommendedAI;
                            //Delete the whatever trainedmodels and pickle files
                            //DeleteGenerateModels(result.CorrelationId, CONSTANTS.RecommendedAI);
                            //revert the model changes..
                            //RevertModelChanges(result);
                            isModelTrained = false;
                        }
                        if (modelsCount >= noOfModelsSelected)
                        {
                            //If, All models success than delete the backup files.
                            //DeleteBackupRecords(result);
                            _regressionRetrain.Status = CONSTANTS.C;
                            _regressionRetrain.Success = false;
                            _regressionRetrain.PageInfo = CONSTANTS.RecommendedAI;
                            isModelTrained = false;
                        }
                        if (errorCount < 2 && modelsCount <= 2)
                        {
                            modelsCount = 0;
                            errorCount = 0;
                            isModelTrained = true;
                            Thread.Sleep(3000);
                            goto ExecuteQueueTable;
                        }
                        Thread.Sleep(1000);
                    }
                    else
                    {
                        IngrainRequestQueue requestQueue = new IngrainRequestQueue();
                        var recommendedModels = GetModelNames(result.CorrelationId);
                        foreach (var modelName in recommendedModels.Item1)
                        {
                            ingrainRequest._id = Guid.NewGuid().ToString();
                            ingrainRequest.CorrelationId = result.CorrelationId;
                            ingrainRequest.RequestId = Guid.NewGuid().ToString();
                            ingrainRequest.ProcessId = null;
                            ingrainRequest.Status = null;
                            ingrainRequest.ModelName = modelName;
                            ingrainRequest.RequestStatus = CONSTANTS.New;
                            ingrainRequest.RetryCount = 0;
                            ingrainRequest.ProblemType = recommendedModels.Item2;
                            ingrainRequest.Message = null;
                            ingrainRequest.UniId = null;
                            ingrainRequest.Progress = null;
                            ingrainRequest.pageInfo = CONSTANTS.RecommendedAI;
                            ingrainRequest.ParamArgs = "{}";
                            ingrainRequest.Function = CONSTANTS.RecommendedAI;
                            ingrainRequest.CreatedByUser = result.CreatedByUser;
                            ingrainRequest.CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                            ingrainRequest.ModifiedByUser = result.CreatedByUser;
                            ingrainRequest.ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                            ingrainRequest.LastProcessedOn = null;
                            InsertRequests(ingrainRequest);
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaRegressionAutoTrainService), "AUTO TRAIN INSERT REQUEST-" + ingrainRequest.CorrelationId, "END", string.IsNullOrEmpty(result.CorrelationId) ? default(Guid) : new Guid(result.CorrelationId), result.AppId, string.Empty, result.ClientUId, result.DeliveryConstructUID);
                        }
                        Thread.Sleep(2000);
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InstaRegressionAutoTrainService), nameof(GetRegressionModelEngineering), ex.Message + "- STACKTRACE - " + ex.StackTrace, ex, result.AppId, "", result.ClientUId, result.DeliveryConstructUID);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaRegressionAutoTrainService), nameof(GetRegressionModelEngineering), "GETREGRESSIONMODELENGINEERING GETREGRESSIONMODELENGINEERING END", string.IsNullOrEmpty(result.CorrelationId) ? default(Guid) : new Guid(result.CorrelationId), result.AppId, string.Empty, result.ClientUId, result.DeliveryConstructUID);
            return _regressionRetrain;
        }
        private RegressionRetrain GetRegressionDeployPrediction(string correlationId, string problemType, string userId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaRegressionAutoTrainService), nameof(GetRegressionDeployPrediction), "REGRESSION GETREGRESSIONDEPLOYPREDICTION START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            _regressionRetrain.Status = null;
            _regressionRetrain.Success = false;
            try
            {
                var dataEngineering = InstaAutoDeployModel(correlationId, problemType);
                if (dataEngineering.Status == CONSTANTS.C)
                {
                    _regressionRetrain.Status = CONSTANTS.C;
                    _regressionRetrain.Success = true;
                    bool isSucess = InstaAutoPrediction(correlationId, userId);
                    if (isSucess)
                    {
                        _regressionRetrain.Status = CONSTANTS.C;
                        _regressionRetrain.IsPredictionSucess = true;
                        _regressionRetrain.Success = true;
                        _regressionRetrain.Message = "Prediction completed Successfully";
                    }
                    else
                    {
                        _regressionRetrain.Status = "E";
                        _regressionRetrain.IsPredictionSucess = false;
                        _regressionRetrain.Message = "Prediction failed";
                        _regressionRetrain.Success = false;
                    }

                }
                else
                {
                    _regressionRetrain.Status = "E";
                    _regressionRetrain.Message = "DeployModel Failed";
                    _regressionRetrain.Success = false;
                    _regressionRetrain.ErrorMessage = "Deploymodel Failed";
                    return _regressionRetrain;
                }

            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InstaRegressionAutoTrainService), nameof(GetRegressionDeployPrediction), ex.Message + "- STACKTRACE - " + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaRegressionAutoTrainService), nameof(GetRegressionDeployPrediction), "REGRESSION GETREGRESSIONDEPLOYPREDICTION END", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            return _regressionRetrain;
        }
        private List<IngrainRequestQueue> GetMultipleRequestStatus(string correlationId, string pageInfo)
        {
            List<IngrainRequestQueue> ingrainRequest = new List<IngrainRequestQueue>();
            var collection = _database.GetCollection<IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
            var builder = Builders<IngrainRequestQueue>.Filter;
            var filter = builder.Eq(CONSTANTS.CorrelationId, correlationId) & builder.Eq(CONSTANTS.pageInfo, pageInfo);
            return ingrainRequest = collection.Find(filter).ToList();
        }
        private void RevertModelsState(string correlationId)
        {
            //Delete the whatever trainedmodels and pickle files
            DeleteGenerateModels(correlationId, CONSTANTS.RecommendedAI);
            //revert the model changes..
            RevertModelChanges(correlationId);
        }
        private void DeleteBackupRecords(string correlationId)
        {
            // Delete the SSAI_RecommendedTrainedModels records.
            string updateString = "_backup";
            var filter1 = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId + updateString);
            var trainedModelsCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIRecommendedTrainedModels);
            var modelResult = trainedModelsCollection.DeleteMany(filter1);

            // Delete the backup pickle files.
            var savedModelcollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_savedModels);
            var builder1 = Builders<BsonDocument>.Filter;
            var filter2 = builder1.Eq(CONSTANTS.CorrelationId, correlationId + updateString) & builder1.Eq(CONSTANTS.FileType, CONSTANTS.MLDL_Model);
            var projection1 = Builders<BsonDocument>.Projection.Include(CONSTANTS.FilePath).Exclude(CONSTANTS.Id);
            var savedModelResult = savedModelcollection.Find(filter2).Project<BsonDocument>(projection1).ToList();
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
            var deleteFileResult = savedModelcollection.DeleteMany(filter2);

            string problemType = string.Empty;
            var deployedModelCollection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
            var filter3 = Builders<DeployModelsDto>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            //Change deployed models to In - progress
            var resultofDeployedModel = deployedModelCollection.Find(filter3).ToList();
            if (resultofDeployedModel.Count > 0)
            {
                problemType = resultofDeployedModel[0].ModelType;
            }
            string[] arr = new string[] { };
            DeployModelsDto deployModel = new DeployModelsDto
            {
                Status = "In Progress",
                DeployedDate = null,
                ModelVersion = null,
                ModelType = null,
                InputSample = null,
                IsPrivate = false,
                IsModelTemplate = true,
                TrainedModelId = null,
                ModelURL = null
            };
            var updateBuilder = Builders<DeployModelsDto>.Update;
            var update = updateBuilder.Set(CONSTANTS.Accuracy, deployModel.Accuracy)
                .Set("ModelURL", deployModel.ModelURL)
                .Set(CONSTANTS.Status, deployModel.Status)
                .Set("WebServices", deployModel.WebServices)
                .Set("DeployedDate", deployModel.DeployedDate)
                .Set("IsPrivate", true)
                .Set(CONSTANTS.IsModelTemplate, false)
                .Set("ModelVersion", deployModel.ModelVersion);
            var result2 = deployedModelCollection.UpdateMany(filter3, update);

        }
        private void DeleteGenerateModels(string correlationId, string pageInfo)
        {
            string problemType = string.Empty;
            //UseCase Deletion
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq(CONSTANTS.CorrelationId, correlationId) & ((builder.Eq(CONSTANTS.pageInfo, CONSTANTS.RecommendedAI)) | (builder.Eq(CONSTANTS.pageInfo, CONSTANTS.ModelTraining)) | (builder.Eq(CONSTANTS.pageInfo, CONSTANTS.WFTeachTest)));
            var useCaseCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
            var deletedResult = useCaseCollection.DeleteMany(filter);

            //RecommendedTrainedModels deletion
            var filter2 = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var trainedModelsCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIRecommendedTrainedModels);
            var modelResult = trainedModelsCollection.DeleteMany(filter2);


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
        }
        private Tuple<List<string>, string> GetModelNames(string correlationId)
        {
            List<string> modelNames = new List<string>();
            string problemType = string.Empty;
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.MERecommendedModels);
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
        private void RevertModelChanges(string correlationId)
        {
            // Revert the SSAI_RecommendedTrainedModels backup records.
            string updateString = "_backup";
            var filter1 = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId + updateString);
            var trainedModelsCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIRecommendedTrainedModels);
            var update = Builders<BsonDocument>.Update.Set(CONSTANTS.CorrelationId, correlationId);
            var modelResult = trainedModelsCollection.UpdateMany(filter1, update);

            // revert the backup pickle files.
            var savedModelcollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_savedModels);
            var builder2 = Builders<BsonDocument>.Filter;
            var filter4 = builder2.Eq(CONSTANTS.CorrelationId, correlationId) & builder2.Eq(CONSTANTS.FileType, CONSTANTS.MLDL_Model);
            var projection2 = Builders<BsonDocument>.Projection.Include(CONSTANTS.FilePath).Exclude(CONSTANTS.Id);
            var savedModelResult = savedModelcollection.Find(filter4).Project<BsonDocument>(projection2).ToList();
            if (savedModelResult.Count > 0)
            {
                for (int i = 0; i < savedModelResult.Count; i++)
                {
                    string filePath = savedModelResult[i][CONSTANTS.FilePath].ToString();
                    if (File.Exists(filePath))
                    {
                        string filePath2 = filePath.Replace("backup_", string.Empty);
                        File.Move(filePath, filePath2);
                        var update2 = Builders<BsonDocument>.Update.Set(CONSTANTS.FilePath, filePath2).Set(CONSTANTS.CorrelationId, correlationId);
                        savedModelcollection.UpdateMany(filter4, update2);
                    }
                }
            }
        }
        private void updateExistingModels(string correlationId, string pageInfo)
        {
            //string message = string.Empty;
            //UseCase Deletion
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq(CONSTANTS.CorrelationId, correlationId) & ((builder.Eq(CONSTANTS.pageInfo, CONSTANTS.RecommendedAI)) | (builder.Eq(CONSTANTS.pageInfo, CONSTANTS.ModelTraining)) | (builder.Eq(CONSTANTS.pageInfo, CONSTANTS.WFTeachTest)));
            var useCaseCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
            var deletedResult = useCaseCollection.DeleteMany(filter);

            //RecommendedTrainedModels backup
            var filter2 = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            string updateString = "_backup";
            var updateTrainedModels = Builders<BsonDocument>.Update;
            var updateModels = updateTrainedModels.Set(CONSTANTS.CorrelationId, correlationId + updateString);
            var trainedModelsCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIRecommendedTrainedModels);
            var modelResult = trainedModelsCollection.UpdateMany(filter2, updateModels);

            //Taking back up of pickle files
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_savedModels);
            var builder1 = Builders<BsonDocument>.Filter;
            var filter1 = builder1.Eq(CONSTANTS.CorrelationId, correlationId) & builder1.Eq(CONSTANTS.FileType, CONSTANTS.MLDL_Model) & builder1.Eq(CONSTANTS.FileType, CONSTANTS.LE);
            var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.FilePath).Exclude(CONSTANTS.Id);
            var savedModelResult = collection.Find(filter1).Project<BsonDocument>(projection).ToList();
            if (savedModelResult.Count > 0)
            {
                for (int i = 0; i < savedModelResult.Count; i++)
                {
                    string filePath = savedModelResult[i][CONSTANTS.FilePath].ToString();
                    if (File.Exists(filePath))
                    {
                        FileInfo fileInfo = new FileInfo(filePath);
                        var directory = Path.GetDirectoryName(filePath);
                        string[] filenames = fileInfo.Name.Split('.');
                        string filePath2 = Path.Combine(directory, "backup_" + filenames[0] + fileInfo.Extension);
                        File.Move(filePath, filePath2);
                        var update = Builders<BsonDocument>.Update.Set(CONSTANTS.FilePath, filePath2).Set(CONSTANTS.CorrelationId, correlationId + updateString);
                        collection.UpdateOne(filter1, update);
                    }
                }
            }

            //Resetting the Train models to default at ME_RecommendedModels Collection
            var recommendedModelsCollection = _database.GetCollection<BsonDocument>(CONSTANTS.MERecommendedModels);
            var modelsProjection = Builders<BsonDocument>.Projection.Include(CONSTANTS.Selected_Models).Exclude(CONSTANTS.Id);
            var modelsFilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var recommendedModelsResult = recommendedModelsCollection.Find(modelsFilter).Project<BsonDocument>(modelsProjection).ToList();
            JObject recommendedObject = new JObject();
            if (recommendedModelsResult.Count > 0)
            {
                recommendedObject = JObject.Parse(recommendedModelsResult[0].ToString());
                foreach (var item in recommendedObject[CONSTANTS.Selected_Models].Children())
                {
                    var jprop = item as JProperty;
                    if (jprop != null)
                    {
                        var columnToUpdate = string.Format(CONSTANTS.SelectedModels_Train_model, jprop.Name);
                        var updateModel = Builders<BsonDocument>.Update.Set(columnToUpdate, CONSTANTS.True);
                        recommendedModelsCollection.UpdateOne(modelsFilter, updateModel);
                    }
                }
            }
        }
        //private void IngestData(DeployModel deployModel)
        //{
        //    string entityitems = CONSTANTS.Null;
        //    var metricitems = CONSTANTS.Null;
        //    Filepath filepath = new Filepath();
        //    filepath.fileList = CONSTANTS.Null;
        //    IngrainRequestQueue ingrainRequest = null;
        //    IngrainRequestQueue bsonElements = new IngrainRequestQueue();
        //    ingrainRequest = new IngrainRequestQueue
        //    {
        //        _id = Guid.NewGuid().ToString(),
        //        CorrelationId = deployModel.CorrelationId,
        //        RequestId = Guid.NewGuid().ToString(),
        //        ProcessId = null,
        //        Status = null,
        //        ModelName = deployModel.ModelVersion,
        //        RequestStatus = CONSTANTS.New,
        //        RetryCount = 0,
        //        ProblemType = deployModel.ModelType,
        //        Message = null,
        //        UseCaseID = deployModel.UseCaseID,
        //        UniId = null,
        //        Progress = null,
        //        pageInfo = CONSTANTS.IngestData,
        //        ParamArgs = null,
        //        Function = "FileUpload",
        //        CreatedByUser = deployModel.CreatedByUser,
        //        CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
        //        ModifiedByUser = deployModel.CreatedByUser,
        //        ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
        //        LastProcessedOn = null,
        //    };
        //    string targetColumn = GetTargetColumn(deployModel);
        //    InstaPayload instaPayload = new InstaPayload
        //    {
        //        InstaId = deployModel.InstaId,
        //        Dimension = "Date",
        //        TargetColumn = targetColumn,
        //        ProblemType = deployModel.ModelType,
        //        UseCaseId = deployModel.UseCaseID
        //    };
        //    ParentFile parentFile = new ParentFile();
        //    parentFile.Type = CONSTANTS.Null;
        //    parentFile.Name = CONSTANTS.Null;
        //    string MappingColumns = string.Empty;
        //    InstaMLFileUpload fileUpload = new InstaMLFileUpload
        //    {
        //        CorrelationId = deployModel.CorrelationId,
        //        ClientUID = deployModel.ClientUId,
        //        DeliveryConstructUId = deployModel.DeliveryConstructUID,
        //        Parent = parentFile,
        //        Flag = source,
        //        mapping = MappingColumns,
        //        mapping_flag = CONSTANTS.False,
        //        pad = entityitems,
        //        metric = metricitems,
        //        InstaMl = instaPayload,
        //        fileupload = filepath,
        //        Customdetails = CONSTANTS.Null
        //    };
        //    ingrainRequest.ParamArgs = fileUpload.ToJson();
        //    GetIngestData(deployModel, ingrainRequest);
        //}
        private void IngestData(DeployModelsDto deployModel, List<InstaPayload> instaModels)
        {
            string entityitems = CONSTANTS.Null;
            var metricitems = CONSTANTS.Null;
            Filepath filepath = new Filepath();
            filepath.fileList = CONSTANTS.Null;
            IngrainRequestQueue ingrainRequest = null;
            IngrainRequestQueue bsonElements = new IngrainRequestQueue();
            ingrainRequest = new IngrainRequestQueue
            {
                _id = Guid.NewGuid().ToString(),
                CorrelationId = deployModel.CorrelationId,
                RequestId = Guid.NewGuid().ToString(),
                ProcessId = null,
                Status = null,
                ModelName = deployModel.ModelVersion,
                RequestStatus = CONSTANTS.New,
                RetryCount = 0,
                ProblemType = deployModel.ModelType,
                Message = null,
                UseCaseID = deployModel.UseCaseID,
                UniId = null,
                Progress = null,
                pageInfo = CONSTANTS.IngestData,
                ParamArgs = null,
                Function = "FileUpload",
                CreatedByUser = deployModel.CreatedByUser,
                CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                ModifiedByUser = deployModel.CreatedByUser,
                ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                LastProcessedOn = null,
            };
            string targetColumn = GetTargetColumn(deployModel);
            ParentFile parentFile = new ParentFile();
            parentFile.Type = CONSTANTS.Null;
            parentFile.Name = CONSTANTS.Null;
            string MappingColumns = string.Empty;
            InstaMLFileUpload fileUpload = new InstaMLFileUpload
            {
                CorrelationId = deployModel.CorrelationId,
                ClientUID = deployModel.ClientUId,
                DeliveryConstructUId = deployModel.DeliveryConstructUID,
                Parent = parentFile,
                Flag = source,
                mapping = MappingColumns,
                mapping_flag = CONSTANTS.False,
                pad = entityitems,
                metric = metricitems,
                InstaMl = instaModels,
                fileupload = filepath,
                Customdetails = CONSTANTS.Null
            };
            ingrainRequest.ParamArgs = fileUpload.ToJson();
            GetIngestData(deployModel, ingrainRequest);
        }
        private void GetIngestData(DeployModelsDto result, IngrainRequestQueue ingrainRequest)
        {
            DeleteIngestRequest(result.CorrelationId);
            InsertRequests(ingrainRequest);
            Thread.Sleep(1000);
            bool flag = true;
            while (flag)
            {
                IngrainRequestQueue requestQueue = new IngrainRequestQueue();
                requestQueue = GetFileRequestStatus(result.CorrelationId, CONSTANTS.IngestData);
                if (requestQueue != null)
                {
                    if (requestQueue.Status == CONSTANTS.C & requestQueue.Progress == CONSTANTS.Hundred)
                    {
                        flag = false;
                        var instaLog = Log(requestQueue, result);
                        InsertAutoLog(instaLog);
                        _regressionRetrain.Status = requestQueue.Status;
                        _regressionRetrain.Success = true;
                        _regressionRetrain.Message = requestQueue.Message;
                    }
                    else if (requestQueue.Status == "E")
                    {
                        flag = false;
                        var instaLog = Log(requestQueue, result);
                        InsertAutoLog(instaLog);
                        _regressionRetrain.Status = requestQueue.Status;
                        _regressionRetrain.Success = true;
                        _regressionRetrain.ErrorMessage = requestQueue.Message;
                    }
                    else if (requestQueue.Status == "I")
                    {
                        flag = false;
                        _regressionRetrain.Status = requestQueue.Status;
                        _regressionRetrain.Success = true;
                    }
                    else
                    {
                        flag = true;
                        Thread.Sleep(1000);
                    }
                }
                else
                {
                    flag = true;
                    Thread.Sleep(1000);
                }
            }
        }
        private void DeleteIngestRequest(string correlationId)
        {
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq(CONSTANTS.CorrelationId, correlationId) & builder.Eq(CONSTANTS.pageInfo, CONSTANTS.IngestData);
            collection.DeleteMany(filter);
        }
        private void InsertRequests(IngrainRequestQueue ingrainRequest)
        {
            var requestQueue = JsonConvert.SerializeObject(ingrainRequest);
            var insertRequestQueue = BsonSerializer.Deserialize<BsonDocument>(requestQueue);
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
            collection.InsertOne(insertRequestQueue);
        }
        private IngrainRequestQueue GetFileRequestStatus(string correlationId, string pageInfo)
        {
            IngrainRequestQueue ingrainRequest = new IngrainRequestQueue();
            var collection = _database.GetCollection<IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
            var builder = Builders<IngrainRequestQueue>.Filter;
            var filter = builder.Eq(CONSTANTS.CorrelationId, correlationId) & builder.Eq(CONSTANTS.pageInfo, pageInfo);
            return ingrainRequest = collection.Find(filter).ToList().FirstOrDefault();
        }
        private DataEngineering GetDataCuration(string correlationId, string pageInfo, string userId, DeployModelsDto deployModel)
        {
            bool callMethod = true;
            DataEngineering dataEngineering = new DataEngineering();
            string PythonResult = string.Empty;
            IngrainRequestQueue ingrainRequest = new IngrainRequestQueue();
            RemoveQueueRecords(correlationId, CONSTANTS.UpdateDataCleanUp);
            while (callMethod)
            {
                var useCaseData = CheckPythonProcess(correlationId, CONSTANTS.UpdateDataCleanUp);
                if (!string.IsNullOrEmpty(useCaseData))
                {
                    JObject queueData = JObject.Parse(useCaseData);
                    dataEngineering.Status = (string)queueData[CONSTANTS.Status];
                    dataEngineering.Progress = (string)queueData[CONSTANTS.Progress];
                    dataEngineering.Message = (string)queueData[CONSTANTS.Message];
                    if (dataEngineering.Status == CONSTANTS.C & dataEngineering.Progress == CONSTANTS.Hundred)
                    {
                        callMethod = false;
                        dataEngineering.IsComplete = true;
                        Log(ingrainRequest, deployModel);
                        return dataEngineering;
                    }
                    else if (dataEngineering.Status == "E")
                    {
                        Log(ingrainRequest, deployModel);
                        callMethod = false;
                        dataEngineering.IsComplete = false;
                        return dataEngineering;
                    }
                    else
                    {
                        Thread.Sleep(1000);
                        callMethod = true;
                    }
                }
                else
                {
                    ingrainRequest._id = Guid.NewGuid().ToString();
                    ingrainRequest.CorrelationId = deployModel.CorrelationId;
                    ingrainRequest.RequestId = Guid.NewGuid().ToString();
                    ingrainRequest.ProcessId = null;
                    ingrainRequest.Status = null;
                    ingrainRequest.ModelName = deployModel.ModelVersion;
                    ingrainRequest.RequestStatus = CONSTANTS.New;
                    ingrainRequest.RetryCount = 0;
                    ingrainRequest.ProblemType = deployModel.ModelType;
                    ingrainRequest.Message = null;
                    ingrainRequest.UniId = null;
                    ingrainRequest.Progress = null;
                    ingrainRequest.pageInfo = CONSTANTS.UpdateDataCleanUp;
                    ingrainRequest.ParamArgs = "{}";
                    ingrainRequest.Function = CONSTANTS.DataCleanUp;
                    ingrainRequest.CreatedByUser = userId;
                    ingrainRequest.CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                    ingrainRequest.ModifiedByUser = userId;
                    ingrainRequest.ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                    ingrainRequest.LastProcessedOn = null;
                    UpdateDataCleanup(deployModel.CorrelationId);
                    InsertRequests(ingrainRequest);
                    Thread.Sleep(1000);
                }
            }
            return dataEngineering;
        }
        private void RemoveQueueRecords(string correlationId, string pageInfo)
        {
            var builder = Builders<BsonDocument>.Filter;
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
            var filter = builder.Eq(CONSTANTS.CorrelationId, correlationId) & builder.Eq(CONSTANTS.pageInfo, pageInfo);
            var result = collection.DeleteMany(filter);
        }
        private string GetTargetColumn(DeployModelsDto deployModel)
        {
            string targetColumn = string.Empty;
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.PSBusinessProblem);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, deployModel.CorrelationId);
            var document = collection.Find(filter).Project<BsonDocument>(Builders<BsonDocument>.Projection.Include(CONSTANTS.TargetColumn)).FirstOrDefault();
            if (document != null)
            {
                targetColumn = document[CONSTANTS.TargetColumn].AsString;
            }
            return targetColumn;
        }
        private string CheckPythonProcess(string correlationId, string pageInfo)
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
        private void UpdateDataCleanup(string correlationId)
        {
            var ingestCollection = _database.GetCollection<BsonDocument>("DataCleanUP_FilteredData");
            var ingestFilter = Builders<BsonDocument>.Filter.Eq("CorrelationId", correlationId);
            var projection = Builders<BsonDocument>.Projection.Include("types").Exclude("_id");
            var result = ingestCollection.Find(ingestFilter).Project(projection).ToList();
            Dictionary<string, string> dataTypeColumns = new Dictionary<string, string>();
            JObject scaleModifiedCols = new JObject();
            if (result.Count > 0)
            {
                var datatypes = JObject.Parse(result[0].ToString())["types"];
                foreach (var type in datatypes.Children())
                {
                    JProperty jProperty = type as JProperty;
                    if (jProperty != null)
                    {
                        dataTypeColumns.Add(jProperty.Name, jProperty.Value.ToString());
                    }
                }
            }
            var collection = _database.GetCollection<BsonDocument>("DE_DataCleanup");
            var filter = Builders<BsonDocument>.Filter.Eq("CorrelationId", correlationId);
            var scaleUpdatemodify = Builders<BsonDocument>.Update.Set("ScaleModifiedColumns", scaleModifiedCols);
            collection.UpdateOne(filter, scaleUpdatemodify);
            string datatypeModifiedCols = string.Format("DtypeModifiedColumns");
            var datatypeUpdate = Builders<BsonDocument>.Update.Set(datatypeModifiedCols, dataTypeColumns);
            collection.UpdateOne(filter, datatypeUpdate);
        }
        private bool IsDataCurationComplete(string correlationId)
        {
            bool _DBEncryptionRequired = EncryptDB(correlationId);
            bool IsCompleted = false;
            List<string> columnsList = new List<string>();
            List<string> noDatatypeList = new List<string>();
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataCleanup);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.FeatureName).Exclude(CONSTANTS.Id);
            var resultData = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            JObject dataCuration = new JObject();
            if (resultData.Count > 0)
            {
                if (_DBEncryptionRequired)
                {
                    if(_IsAESKeyVault)
                        resultData[0][CONSTANTS.FeatureName] = BsonDocument.Parse(CryptographyUtility.Decrypt(resultData[0][CONSTANTS.FeatureName].AsString));
                    else
                        resultData[0][CONSTANTS.FeatureName] = BsonDocument.Parse(AesProvider.Decrypt(resultData[0][CONSTANTS.FeatureName].AsString, _aesKey, _aesVector));
                }
                dataCuration = JObject.Parse(resultData[0].ToString());
                foreach (var column in dataCuration[CONSTANTS.FeatureName].Children())
                {
                    JProperty property = column as JProperty;
                    columnsList.Add(property.Name.ToString());
                }
                foreach (var column in columnsList)
                {
                    bool datatypeExist = false;
                    foreach (var item in dataCuration[CONSTANTS.FeatureName][column][CONSTANTS.Datatype].Children())
                    {
                        if (item != null)
                        {
                            JProperty property = item as JProperty;
                            if (property.Name != CONSTANTS.Select_Option)
                            {
                                if (property.Value.ToString() == CONSTANTS.True)
                                {
                                    datatypeExist = true;
                                    IsCompleted = true;
                                }
                            }
                            else
                            {
                                string columnToUpdate = string.Format(CONSTANTS.SelectOption, column);
                                var updateField = Builders<BsonDocument>.Update.Set(columnToUpdate, CONSTANTS.False);
                                var updateResult = collection.UpdateOne(filter, updateField);
                            }

                        }
                    }
                    if (_DBEncryptionRequired)
                    {
                        var updateField = Builders<BsonDocument>.Update.Set(CONSTANTS.FeatureName,(_IsAESKeyVault? CryptographyUtility.Encrypt(dataCuration[CONSTANTS.FeatureName].ToString(Formatting.None)) : AesProvider.Encrypt(dataCuration[CONSTANTS.FeatureName].ToString(Formatting.None), _aesKey, _aesVector)));
                        var updateResult = collection.UpdateOne(filter, updateField);
                    }
                    else
                    {
                        var Featuredata = BsonDocument.Parse(dataCuration[CONSTANTS.FeatureName].ToString());
                        var updateField = Builders<BsonDocument>.Update.Set(CONSTANTS.FeatureName, Featuredata);
                        var updateResult = collection.UpdateOne(filter, updateField);
                    }
                    if (!datatypeExist)
                    {
                        dataCuration[CONSTANTS.FeatureName][column][CONSTANTS.Datatype][CONSTANTS.category] = CONSTANTS.True;
                        if (_DBEncryptionRequired)
                        {
                            var updateField = Builders<BsonDocument>.Update.Set(CONSTANTS.FeatureName,(_IsAESKeyVault ? CryptographyUtility.Encrypt(dataCuration[CONSTANTS.FeatureName].ToString(Formatting.None)) : AesProvider.Encrypt(dataCuration[CONSTANTS.FeatureName].ToString(Formatting.None), _aesKey, _aesVector)));
                            var updateResult = collection.UpdateOne(filter, updateField);
                        }
                        else
                        {
                            var Featuredata = BsonDocument.Parse(dataCuration[CONSTANTS.FeatureName].ToString());
                            var updateField = Builders<BsonDocument>.Update.Set(CONSTANTS.FeatureName, Featuredata);
                            var updateResult = collection.UpdateOne(filter, updateField);
                        }
                        //string columnToUpdate = string.Format(CONSTANTS.DatatypeCategory, column);
                        //var updateField = Builders<BsonDocument>.Update.Set(columnToUpdate, CONSTANTS.True);
                        //var updateResult = collection.UpdateOne(filter, updateField);
                        IsCompleted = true;
                    }
                }
            }
            return IsCompleted;
        }
        private DataEngineering GetDatatransformation(string correlationId, string pageInfo, string userId, DeployModelsDto result)
        {
            bool callMethod = true;
            DataEngineering dataEngineering = new DataEngineering();
            string PythonResult = string.Empty;
            IngrainRequestQueue ingrainRequest = new IngrainRequestQueue();
            while (callMethod)
            {
                var useCaseData = CheckPythonProcess(correlationId, CONSTANTS.DataPreprocessing);
                if (!string.IsNullOrEmpty(useCaseData))
                {
                    Thread.Sleep(1000);
                    JObject queueData = JObject.Parse(useCaseData);
                    dataEngineering.Status = (string)queueData[CONSTANTS.Status];
                    dataEngineering.Progress = (string)queueData[CONSTANTS.Progress];
                    dataEngineering.Message = (string)queueData[CONSTANTS.Message];
                    if (dataEngineering.Status == CONSTANTS.C & dataEngineering.Progress == CONSTANTS.Hundred)
                    {
                        callMethod = false;
                        dataEngineering.IsComplete = true;
                        var instaLog = Log(ingrainRequest, result);
                        InsertAutoLog(instaLog);
                        return dataEngineering;
                    }
                    if (dataEngineering.Status == "E")
                    {
                        callMethod = false;
                        dataEngineering.IsComplete = false;
                        var instaLog = Log(ingrainRequest, result);
                        InsertAutoLog(instaLog);
                        return dataEngineering;
                    }
                }
                else
                {
                    ingrainRequest._id = Guid.NewGuid().ToString();
                    ingrainRequest.CorrelationId = correlationId;
                    ingrainRequest.RequestId = Guid.NewGuid().ToString();
                    ingrainRequest.ProcessId = null;
                    ingrainRequest.Status = null;
                    ingrainRequest.ModelName = result.ModelVersion;
                    ingrainRequest.RequestStatus = CONSTANTS.New;
                    ingrainRequest.RetryCount = 0;
                    ingrainRequest.ProblemType = result.ModelType;
                    ingrainRequest.Message = null;
                    ingrainRequest.UniId = null;
                    ingrainRequest.Progress = null;
                    ingrainRequest.pageInfo = CONSTANTS.DataPreprocessing;
                    ingrainRequest.ParamArgs = "{}";
                    ingrainRequest.Function = CONSTANTS.DataTransform;
                    ingrainRequest.CreatedByUser = userId;
                    ingrainRequest.CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                    ingrainRequest.ModifiedByUser = userId;
                    ingrainRequest.ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                    ingrainRequest.LastProcessedOn = null;
                    InsertRequests(ingrainRequest);
                    Thread.Sleep(1000);
                }
            }
            return dataEngineering;
        }
        private List<BsonDocument> GetPreprocessExistData(string correlationId)
        {
            _preProcessDTO.CorrelationId = correlationId;
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var prePropcessProjection = Builders<BsonDocument>.Projection.Include(CONSTANTS.Filters).Include(CONSTANTS.MissingValues).Include(CONSTANTS.DataEncoding).Include(CONSTANTS.DataModification).Include(CONSTANTS.DataTransformationApplied).Include(CONSTANTS.TargetColumn).Exclude(CONSTANTS.Id);
            var dataPreprocessCollection = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataProcessing);
            var preprocessDataExist = dataPreprocessCollection.Find(filter).Project<BsonDocument>(prePropcessProjection).ToList();
            return preprocessDataExist;
        }
        private void RemoveDataPreprocessAttributes(string correlationId, List<string> PreprocessCols, List<string> DEColmnList, string field)
        {
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataProcessing);
            FilterDefinition<BsonDocument> filterDefinition = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            foreach (var column in PreprocessCols)
            {
                bool exists = DEColmnList.Contains(column);
                if (!exists & column != CONSTANTS.Interpolation)
                {
                    UpdateDefinition<BsonDocument> updateDefinition = null;
                    UpdateDefinitionBuilder<BsonDocument> updateDefinitionBuilder = Builders<BsonDocument>.Update;
                    string updateColumn = field + CONSTANTS.Dot + column;
                    if (updateDefinition == null)
                        updateDefinition = updateDefinitionBuilder.Unset(updateColumn);
                    else
                        updateDefinition = updateDefinition.Unset(updateColumn);
                    var updateRes = collection.UpdateOne(filterDefinition, updateDefinition);
                }
            }
        }
        private void RemoveDataPreprocessEncryptAttributes(List<string> DEColmnList, JObject jData, string correlationId, bool isDataModification, string nodeName)
        {
            #region Filters Removing Attributes and Update DB
            var preprocessFilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataProcessing);
            List<string> PreprocessCols = new List<string>();
            if (isDataModification)
            {
                //For DataModification ColumnBinning Update
                foreach (var item in jData[CONSTANTS.ColumnBinning].Children())
                {
                    JProperty j = item as JProperty;
                    PreprocessCols.Add(j.Name);
                }
                JObject columnBinning = new JObject();
                foreach (var column in PreprocessCols)
                {
                    bool exists = DEColmnList.Contains(column);
                    if (!exists)
                    {
                        columnBinning = JObject.Parse(jData[CONSTANTS.ColumnBinning].ToString());
                        columnBinning.Property(column).Remove();
                    }
                }
                jData["ColumnBinning"] = JObject.FromObject(columnBinning);

                //For DataModification Features Update
                PreprocessCols = new List<string>();

                foreach (var item in jData[CONSTANTS.Features].Children())
                {
                    JProperty j = item as JProperty;
                    PreprocessCols.Add(j.Name);
                }
                JObject Features = new JObject();
                foreach (var column in PreprocessCols)
                {
                    bool exists = DEColmnList.Contains(column);
                    if (!exists & column != CONSTANTS.Interpolation)
                    {
                        Features = JObject.Parse(jData[CONSTANTS.Features].ToString());
                        Features.Property(column).Remove();
                    }
                }
                jData["Features"] = JObject.FromObject(Features);
            }
            else
            {
                foreach (var item in jData.Children())
                {
                    JProperty j = item as JProperty;
                    PreprocessCols.Add(j.Name);
                }
                foreach (var column in PreprocessCols)
                {
                    bool exists = DEColmnList.Contains(column);
                    if (!exists)
                    {
                        jData.Property(column).Remove();
                    }
                }
            }
            var filterUpdate = Builders<BsonDocument>.Update.Set(nodeName,(_IsAESKeyVault? CryptographyUtility.Encrypt(jData.ToString(Formatting.None)) : AesProvider.Encrypt(jData.ToString(Formatting.None), _aesKey, _aesVector)));
            collection.UpdateOne(preprocessFilter, filterUpdate);
            #endregion
        }
        private void PreProcessAttributesRemove(string correlationId, JObject DePreprocessData, List<string> DEColmnList)
        {
            List<string> PreprocessCols = new List<string>();

            #region Filters Remove Start
            foreach (var item in DePreprocessData[CONSTANTS.Filters].Children())
            {
                JProperty j = item as JProperty;
                PreprocessCols.Add(j.Name);
            }
            RemoveDataPreprocessAttributes(correlationId, PreprocessCols, DEColmnList, CONSTANTS.Filters);
            #endregion

            #region MissingValues Remove Start
            PreprocessCols = new List<string>();
            foreach (var item in DePreprocessData[CONSTANTS.MissingValues].Children())
            {
                JProperty j = item as JProperty;
                PreprocessCols.Add(j.Name);
            }
            RemoveDataPreprocessAttributes(correlationId, PreprocessCols, DEColmnList, CONSTANTS.MissingValues);
            #endregion

            #region DataEncoding Remove Start
            PreprocessCols = new List<string>();
            foreach (var item in DePreprocessData[CONSTANTS.DataEncoding].Children())
            {
                JProperty j = item as JProperty;
                PreprocessCols.Add(j.Name);
            }
            RemoveDataPreprocessAttributes(correlationId, PreprocessCols, DEColmnList, CONSTANTS.DataEncoding);
            #endregion

            #region DataModification Remove Start
            PreprocessCols = new List<string>();
            foreach (var item in DePreprocessData[CONSTANTS.DataModification][CONSTANTS.ColumnBinning].Children())
            {
                JProperty j = item as JProperty;
                PreprocessCols.Add(j.Name);
            }
            RemoveDataPreprocessAttributes(correlationId, PreprocessCols, DEColmnList, CONSTANTS.DataModification + CONSTANTS.Dot + CONSTANTS.ColumnBinning);
            PreprocessCols = new List<string>();
            foreach (var item in DePreprocessData[CONSTANTS.DataModification][CONSTANTS.Features].Children())
            {
                JProperty j = item as JProperty;
                PreprocessCols.Add(j.Name);
            }
            RemoveDataPreprocessAttributes(correlationId, PreprocessCols, DEColmnList, CONSTANTS.DataModification + CONSTANTS.Dot + CONSTANTS.Features);
            #endregion

        }
        private void PreProcessEncryptAttributesRemove(List<BsonDocument> preprocessDataExist, string correlationId, List<string> DEColmnList)
        {
            JObject Filters = new JObject();
            JObject MissingValues = new JObject();
            JObject DataModification = new JObject();
            JObject DataEncoding = new JObject();
            List<string> PreprocessCols = new List<string>();
            if (_IsAESKeyVault)
            {
                Filters = JObject.Parse(BsonDocument.Parse(CryptographyUtility.Decrypt(preprocessDataExist[0][CONSTANTS.Filters].AsString)).ToString());
                MissingValues = JObject.Parse(BsonDocument.Parse(CryptographyUtility.Decrypt(preprocessDataExist[0][CONSTANTS.MissingValues].AsString)).ToString());
                DataModification = JObject.Parse(BsonDocument.Parse(CryptographyUtility.Decrypt(preprocessDataExist[0][CONSTANTS.DataModification].AsString)).ToString());
            }
            else
            {
                Filters = JObject.Parse(BsonDocument.Parse(AesProvider.Decrypt(preprocessDataExist[0][CONSTANTS.Filters].AsString, _aesKey, _aesVector)).ToString());
                MissingValues = JObject.Parse(BsonDocument.Parse(AesProvider.Decrypt(preprocessDataExist[0][CONSTANTS.MissingValues].AsString, _aesKey, _aesVector)).ToString());
                DataModification = JObject.Parse(BsonDocument.Parse(AesProvider.Decrypt(preprocessDataExist[0][CONSTANTS.DataModification].AsString, _aesKey, _aesVector)).ToString());
            }
            DataEncoding = JObject.Parse(preprocessDataExist[0][CONSTANTS.DataEncoding].ToString());
            List<string> preProcessObjects = new List<string> { CONSTANTS.Filters, CONSTANTS.MissingValues, CONSTANTS.DataModification };
            foreach (var process in preProcessObjects)
            {
                switch (process)
                {
                    case CONSTANTS.Filters:
                        RemoveDataPreprocessEncryptAttributes(DEColmnList, Filters, correlationId, false, process);
                        break;
                    case CONSTANTS.MissingValues:
                        RemoveDataPreprocessEncryptAttributes(DEColmnList, MissingValues, correlationId, false, process);
                        break;
                    case CONSTANTS.DataModification:
                        RemoveDataPreprocessEncryptAttributes(DEColmnList, DataModification, correlationId, true, process);
                        break;
                }
            }
            #region DataEncoding Remove Start
            PreprocessCols = null;
            foreach (var item in DataEncoding[CONSTANTS.DataEncoding].Children())
            {
                JProperty j = item as JProperty;
                PreprocessCols.Add(j.Name);
            }
            RemoveDataPreprocessAttributes(correlationId, PreprocessCols, DEColmnList, CONSTANTS.DataEncoding);
            #endregion
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
        private bool CreatePreprocess(string correlationId, string userId, string problemType, string instaId)
        {
            PreProcessModelDTO preProcessModel = new PreProcessModelDTO
            {
                CorrelationId = correlationId,
                ModelType = problemType
            };
            preProcessModel.ModelType = problemType;
            var preprocessDataExist = GetPreprocessExistData(correlationId);
            if (preprocessDataExist.Count > 0)
            {
                //Starting code
                List<string> DEColmnList = new List<string>();
                JObject serializeDEData = new JObject();
                var deDataCleanup = new List<BsonDocument>();
                var deFilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
                var deprojection = Builders<BsonDocument>.Projection.Include(CONSTANTS.FeatureName).Include(CONSTANTS.NewFeatureName).Exclude(CONSTANTS.Id);
                var deCollection = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataCleanup);
                deDataCleanup = deCollection.Find(deFilter).Project<BsonDocument>(deprojection).ToList();
                bool EncryptionRequired = EncryptDB(correlationId);
                if (deDataCleanup.Count > 0)
                {
                    if (EncryptionRequired)
                    {
                        if(_IsAESKeyVault)
                            deDataCleanup[0][CONSTANTS.FeatureName] = BsonDocument.Parse(CryptographyUtility.Decrypt(deDataCleanup[0][CONSTANTS.FeatureName].AsString));
                        else
                            deDataCleanup[0][CONSTANTS.FeatureName] = BsonDocument.Parse(AesProvider.Decrypt(deDataCleanup[0][CONSTANTS.FeatureName].AsString, _aesKey, _aesVector));
                        if (deDataCleanup[0].Contains(CONSTANTS.NewFeatureName))
                        {
                            if (deDataCleanup[0][CONSTANTS.NewFeatureName].ToString() != "{ }")
                            {
                                if (_IsAESKeyVault)
                                    deDataCleanup[0][CONSTANTS.NewFeatureName] = BsonDocument.Parse(CryptographyUtility.Decrypt(deDataCleanup[0][CONSTANTS.NewFeatureName].AsString));
                                else
                                    deDataCleanup[0][CONSTANTS.NewFeatureName] = BsonDocument.Parse(AesProvider.Decrypt(deDataCleanup[0][CONSTANTS.NewFeatureName].AsString, _aesKey, _aesVector));
                            }
                        }
                    }
                    //Combining new features to Existing Features
                    JObject datas = JObject.Parse(deDataCleanup[0].ToString());
                    JObject combinedFeatures = new JObject();
                    combinedFeatures = this.CombinedFeatures(datas);
                    if (combinedFeatures != null)
                        deDataCleanup[0][CONSTANTS.FeatureName] = BsonDocument.Parse(combinedFeatures.ToString());

                    serializeDEData = JObject.Parse(deDataCleanup[0].ToString());
                    foreach (var features in serializeDEData[CONSTANTS.FeatureName].Children())
                    {
                        JProperty j = features as JProperty;
                        DEColmnList.Add(j.Name);
                    }
                    var collection = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataProcessing);
                    var preprocessFilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
                    var preprocessProjection = Builders<BsonDocument>.Projection.Include(CONSTANTS.Filters).Include(CONSTANTS.MissingValues).Include(CONSTANTS.DataEncoding).Include(CONSTANTS.DataModification).Exclude(CONSTANTS.Id);
                    var PreprocessData = collection.Find(preprocessFilter).Project<BsonDocument>(preprocessProjection).ToList();
                    if (PreprocessData.Count > 0)
                    {
                        if (EncryptionRequired)
                        {
                            //To Remove DataPreProcess Attributes with Encryption
                            PreProcessEncryptAttributesRemove(PreprocessData, correlationId, DEColmnList);
                        }
                        else
                        {
                            //To Remove DataPreProcess Attributes without Encryption
                            JObject DePreprocessData = JObject.Parse(PreprocessData[0].ToString());
                            PreProcessAttributesRemove(correlationId, DePreprocessData, DEColmnList);
                        }
                    }
                }
                //Ending Code
                return insertSuccess = true;
            }
            _preProcessDTO.DataTransformationApplied = true;
            _preProcessDTO.CorrelationId = correlationId;
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            string processData = string.Empty;

            var dataCleanupCollection = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataCleanup);
            var filterCollection = _database.GetCollection<BsonDocument>(CONSTANTS.DataCleanUPFilteredData);
            var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.FeatureName).Exclude(CONSTANTS.Id);
            var filteredData = new List<BsonDocument>();
            List<string> columnsList = new List<string>();
            List<string> categoricalColumns = new List<string>();
            List<string> missingColumns = new List<string>();
            List<string> numericalColumns = new List<string>();
            JObject serializeData = new JObject();
            bool _DBEncryptionRequired = EncryptDB(correlationId);
            filteredData = dataCleanupCollection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (filteredData.Count > 0)
            {
                if (_DBEncryptionRequired)
                {
                    if (_IsAESKeyVault)
                        filteredData[0][CONSTANTS.FeatureName] = BsonDocument.Parse(CryptographyUtility.Decrypt(filteredData[0][CONSTANTS.FeatureName].ToString()));
                    else
                        filteredData[0][CONSTANTS.FeatureName] = BsonDocument.Parse(AesProvider.Decrypt(filteredData[0][CONSTANTS.FeatureName].ToString(), _aesKey, _aesVector));
                }
                serializeData = JObject.Parse(filteredData[0].ToString());
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
                        if (property != null && property.Name == CONSTANTS.Category && property.Value.ToString() == CONSTANTS.True)
                        {
                            categoricalColumns.Add(item);
                            if (value > 0)
                                missingColumns.Add(item);
                        }
                        if (property != null && (property.Name == "float64" || property.Name == "int64") && property.Value.ToString() == CONSTANTS.True)
                        {
                            if (value > 0)
                                numericalColumns.Add(item);
                        }
                    }
                }
                //Get DataModificationData
                GetModifications(correlationId);
                //Getting the Data Encoding Data
                GetDataEncodingValues(categoricalColumns, serializeData);

                //This code for filters to be applied
                var uniqueValueProjection = Builders<BsonDocument>.Projection.Include(CONSTANTS.ColumnUniqueValues).Include(CONSTANTS.target_variable).Exclude(CONSTANTS.Id);
                var filteredResult = filterCollection.Find(filter).Project<BsonDocument>(uniqueValueProjection).ToList();
                JObject uniqueData = new JObject();
                if (filteredResult.Count > 0)
                {
                    if (_DBEncryptionRequired)
                    {
                        if(_IsAESKeyVault)
                            filteredResult[0][CONSTANTS.ColumnUniqueValues] = BsonDocument.Parse(CryptographyUtility.Decrypt(filteredResult[0][CONSTANTS.ColumnUniqueValues].ToString()));
                        else
                            filteredResult[0][CONSTANTS.ColumnUniqueValues] = BsonDocument.Parse(AesProvider.Decrypt(filteredResult[0][CONSTANTS.ColumnUniqueValues].ToString(), _aesKey, _aesVector));
                    }
                    _preProcessDTO.TargetColumn = filteredResult[0][CONSTANTS.target_variable].ToString();
                    uniqueData = JObject.Parse(filteredResult[0].ToString());
                    //Getting the Missing Values and Filters Data
                    GetMissingAndFiltersData(missingColumns, categoricalColumns, numericalColumns, uniqueData);
                    InsertToPreprocess(preProcessModel, instaId);
                    insertSuccess = true;
                }
            }
            return insertSuccess;
        }
        private void GetModifications(string correlationId)
        {
            bool _DBEncryptionRequired = EncryptDB(correlationId);
            List<string> binningcolumnsList = new List<string>();
            List<string> recommendedcolumnsList = new List<string>();
            List<string> columnsList = new List<string>();
            Dictionary<string, Dictionary<string, Dictionary<string, string>>> recommendedColumns = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();

            Dictionary<string, Dictionary<string, Dictionary<string, string>>> columnBinning = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();
            Dictionary<string, string> prescriptionData = new Dictionary<string, string>();
            JObject serializeData = new JObject();


            var dataCleanupCollection = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataCleanup);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.FeatureName).Exclude(CONSTANTS.Id);
            var filteredData = dataCleanupCollection.Find(filter).Project<BsonDocument>(projection).ToList();

            if (filteredData.Count > 0)
            {
                if (_DBEncryptionRequired)
                {
                    if(_IsAESKeyVault)
                        filteredData[0][CONSTANTS.FeatureName] = BsonDocument.Parse(CryptographyUtility.Decrypt(filteredData[0][CONSTANTS.FeatureName].ToString()));
                    else
                        filteredData[0][CONSTANTS.FeatureName] = BsonDocument.Parse(AesProvider.Decrypt(filteredData[0][CONSTANTS.FeatureName].ToString(), _aesKey, _aesVector));
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
                    foreach (var item in columnsList)
                    {
                        Dictionary<string, Dictionary<string, string>> binningColumns2 = new Dictionary<string, Dictionary<string, string>>();
                        Dictionary<string, Dictionary<string, string>> binningColumns3 = new Dictionary<string, Dictionary<string, string>>();
                        Dictionary<string, string> removeImbalancedColumns = new Dictionary<string, string>();

                        Dictionary<string, string> outlier = new Dictionary<string, string>();
                        Dictionary<string, string> skeweness = new Dictionary<string, string>();
                        Dictionary<string, Dictionary<string, string>> fields = new Dictionary<string, Dictionary<string, string>>();
                        var outData = serializeData[CONSTANTS.FeatureName][item][CONSTANTS.Outlier];
                        var skewData = serializeData[CONSTANTS.FeatureName][item][CONSTANTS.Skeweness];
                        float outValue = (float)outData;
                        string skewValue = (string)skewData;
                        var imbalanced = serializeData[CONSTANTS.FeatureName][item][CONSTANTS.ImBalanced];
                        string imbalancedValue = (string)imbalanced;
                        if (imbalancedValue == "1")
                        {
                            JProperty jProperty1 = null;
                            string recommendation = string.Format(CONSTANTS.Recommendation, item);
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
                                        dict.Add(CONSTANTS.ChangeRequest, "");
                                        binningColumns2.Add(CONSTANTS.ChangeRequest, dict);
                                    }
                                    else
                                    {
                                        dict.Add(CONSTANTS.PChangeRequest, "");
                                        binningColumns2.Add(CONSTANTS.PChangeRequest, dict);
                                    }
                                }
                            }
                            columnBinning.Add(item, binningColumns2);
                        }
                        else if (imbalancedValue == "2")
                        {
                            string removeColumndesc = string.Format(CONSTANTS.StringFormat, item);
                            removeImbalancedColumns.Add(item, removeColumndesc);
                        }
                        else if (imbalancedValue == "3")
                        {
                            string prescription = string.Format(CONSTANTS.StringFormat1, item);
                            prescriptionData.Add(item, prescription);
                        }
                        if (prescriptionData.Count > 0)
                            _preProcessDTO.Prescriptions = prescriptionData;

                        if (outValue > 0)
                        {
                            string strForm = string.Format(CONSTANTS.StringFormat2, item, outValue);
                            outlier.Add("Text", strForm);
                            string[] outliers = { CONSTANTS.Mean, CONSTANTS.Median, "Mode", CONSTANTS.CustomValue, CONSTANTS.ChangeRequest, CONSTANTS.PChangeRequest };
                            for (int i = 0; i < outliers.Length; i++)
                            {
                                if (i == 3)
                                {
                                    outlier.Add(outliers[i], "");
                                }
                                else if (i == 4 || i == 5)
                                {
                                    outlier.Add(outliers[i], "");
                                }
                                else
                                {
                                    outlier.Add(outliers[i], CONSTANTS.False);
                                }
                            }
                        }
                        if (skewValue == "Yes")
                        {
                            string strForm = string.Format(CONSTANTS.StringFormat3, item);
                            skeweness.Add(CONSTANTS.Skeweness, strForm);
                            string[] skewnessArray = { CONSTANTS.BoxCox, CONSTANTS.Reciprocal, CONSTANTS.Log, CONSTANTS.ChangeRequest, CONSTANTS.PChangeRequest };
                            for (int i = 0; i < skewnessArray.Length; i++)
                            {
                                if (i == 3 || i == 4)
                                {
                                    skeweness.Add(skewnessArray[i], "");
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
                            fields.Add(CONSTANTS.Skeweness, skeweness);
                        if (removeImbalancedColumns.Count > 0)
                            fields.Add(CONSTANTS.RemoveColumn, removeImbalancedColumns);

                        if (fields.Count > 0)
                        {
                            recommendedColumns.Add(item, fields);
                        }
                    }
                }
                if (columnBinning.Count > 0)
                    _preProcessDTO.ColumnBinning = columnBinning;
                if (recommendedColumns.Count > 0)
                    _preProcessDTO.RecommendedColumns = recommendedColumns;
            }
        }
        private void GetDataEncodingValues(List<string> categoricalColumns, JObject serializeData)
        {
            var encodingData = new Dictionary<string, Dictionary<string, string>>();
            foreach (var column in categoricalColumns)
            {
                var dataEncodingData = new Dictionary<string, string>();
                foreach (JToken scale in serializeData[CONSTANTS.FeatureName][column]["Scale"].Children())
                {
                    if (scale is JProperty property && property.Value.ToString() == CONSTANTS.True)
                    {
                        dataEncodingData.Add(CONSTANTS.Attribute, property.Name);
                        dataEncodingData.Add(CONSTANTS.encoding, CONSTANTS.LabelEncoding);
                    }
                }
                if (dataEncodingData.Count > 0)
                {
                    dataEncodingData.Add(CONSTANTS.ChangeRequest, CONSTANTS.True);
                    dataEncodingData.Add(CONSTANTS.PChangeRequest, "");
                    encodingData.Add(column, dataEncodingData);
                }

            }
            _preProcessDTO.DataEncodeData = encodingData;
        }
        private void InsertToPreprocess(PreProcessModelDTO preProcessModel, string instaId)
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
                //DataModification Insertion Format Start
                var recommendedColumnsData = Newtonsoft.Json.JsonConvert.SerializeObject(_preProcessDTO.RecommendedColumns);
                if (!string.IsNullOrEmpty(recommendedColumnsData) && recommendedColumnsData != "null")
                    outlierData = JObject.Parse(recommendedColumnsData);
                var columnBinning = Newtonsoft.Json.JsonConvert.SerializeObject(_preProcessDTO.ColumnBinning);
                if (!string.IsNullOrEmpty(columnBinning) && columnBinning != "null")
                    binningData = JObject.Parse(columnBinning);
                JObject binningObject = new JObject();
                if (binningData != null)
                    binningObject["ColumnBinning"] = JObject.FromObject(binningData);

                var prescription = Newtonsoft.Json.JsonConvert.SerializeObject(_preProcessDTO.Prescriptions);
                if (!string.IsNullOrEmpty(prescription) && prescription != "null")
                    prescriptionData = JObject.Parse(prescription);
                //DataModification Insertion Format End

                categoricalJson = Newtonsoft.Json.JsonConvert.SerializeObject(_preProcessDTO.CategoricalData);
                missingValuesJson = Newtonsoft.Json.JsonConvert.SerializeObject(_preProcessDTO.MisingValuesData);
                numericJson = Newtonsoft.Json.JsonConvert.SerializeObject(_preProcessDTO.NumericalData);
                dataEncodingJson = Newtonsoft.Json.JsonConvert.SerializeObject(_preProcessDTO.DataEncodeData);

                JObject missingValuesObject = new JObject();
                JObject categoricalObject = new JObject();
                JObject numericObject = new JObject();
                JObject encodedData = new JObject();
                if (!string.IsNullOrEmpty(categoricalJson) && categoricalJson != "null")
                    categoricalObject = JObject.Parse(categoricalJson);
                if (!string.IsNullOrEmpty(numericJson) && numericJson != "null")
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
                smoteFlags.Add("Flag", CONSTANTS.False);
                smoteFlags.Add(CONSTANTS.ChangeRequest, CONSTANTS.False);
                smoteFlags.Add(CONSTANTS.PChangeRequest, CONSTANTS.False);

                var smoteTest = Newtonsoft.Json.JsonConvert.SerializeObject(smoteFlags);
                JObject smoteData = new JObject();
                smoteData = JObject.Parse(smoteTest);

                JObject processData = new JObject
                {
                    [CONSTANTS.Id] = Guid.NewGuid(),
                    [CONSTANTS.CorrelationId] = _preProcessDTO.CorrelationId
                };
                if (!string.IsNullOrEmpty(_preProcessDTO.Flag))
                    _preProcessDTO.Flag = CONSTANTS.False;
                processData["Flag"] = _preProcessDTO.Flag;
                //Removing the Target column having lessthan 2 values..important
                bool removeTargetColumn = false;
                if (categoricalObject != null && categoricalObject.ToString() != "{}")
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
                processData[CONSTANTS.TargetColumn] = _preProcessDTO.TargetColumn;
                JObject InterpolationObject = new JObject();
                processData[CONSTANTS.DataModification][CONSTANTS.Features][CONSTANTS.Interpolation] = JObject.FromObject(InterpolationObject);
                if (preProcessModel.ModelType == CONSTANTS.TimeSeries)
                    processData[CONSTANTS.DataModification][CONSTANTS.Features][CONSTANTS.Interpolation] = "Linear";
                processData["Smote"] = smoteData;
                processData[CONSTANTS.InstaId] = instaId;
                processData[CONSTANTS.DataTransformationApplied] = _preProcessDTO.DataTransformationApplied;
                processData[CONSTANTS.CreatedOn] = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                processData[CONSTANTS.ModifiedOn] = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                bool _DBEncryptionRequired = EncryptDB(preProcessModel.CorrelationId);
                var collection = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataProcessing);
                if (_DBEncryptionRequired)
                {
                    if (_IsAESKeyVault)
                    {
                        processData[CONSTANTS.DataModification] = CryptographyUtility.Encrypt(processData[CONSTANTS.DataModification].ToString(Formatting.None));
                        processData[CONSTANTS.MissingValues] = CryptographyUtility.Encrypt(processData[CONSTANTS.MissingValues].ToString(Formatting.None));
                        processData[CONSTANTS.Filters] = CryptographyUtility.Encrypt(processData[CONSTANTS.Filters].ToString(Formatting.None));
                    }
                    else
                    {
                        processData[CONSTANTS.DataModification] = AesProvider.Encrypt(processData[CONSTANTS.DataModification].ToString(Formatting.None), _aesKey, _aesVector);
                        processData[CONSTANTS.MissingValues] = AesProvider.Encrypt(processData[CONSTANTS.MissingValues].ToString(Formatting.None), _aesKey, _aesVector);
                        processData[CONSTANTS.Filters] = AesProvider.Encrypt(processData[CONSTANTS.Filters].ToString(Formatting.None), _aesKey, _aesVector);
                    }
                }
                var insertdoc = BsonSerializer.Deserialize<BsonDocument>(processData.ToString());
                collection.InsertOne(insertdoc);
                insertSuccess = true;
            }
        }
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
                        fieldDictionary.Add(value.ToString().Replace(".", "\u2024").Replace("\r\n", " ").Replace("\"", "").Replace("\t", " "), CONSTANTS.False);
                }
                if (fieldDictionary.Count > 0)
                {
                    fieldDictionary.Add(CONSTANTS.ChangeRequest, "");
                    fieldDictionary.Add(CONSTANTS.PChangeRequest, "");
                    categoricalDictionary.Add(column, fieldDictionary);
                }
            }
            _preProcessDTO.CategoricalData = categoricalDictionary;

            foreach (var column in missingColumns)
            {
                int i = 0;
                Dictionary<string, string> fieldDictionary = new Dictionary<string, string>();
                foreach (JToken value in uniqueData[CONSTANTS.ColumnUniqueValues][column].Children())
                {
                    if (!string.IsNullOrEmpty(Convert.ToString(value)))
                        if (i == 0)
                        {
                            fieldDictionary.Add(value.ToString().Replace(".", "\u2024").Replace("\r\n", " ").Replace("\"", "").Replace("\t", " "), CONSTANTS.True);
                        }
                        else
                        {
                            fieldDictionary.Add(value.ToString().Replace(".", "\u2024").Replace("\r\n", " ").Replace("\"", "").Replace("\t", " "), CONSTANTS.False);
                        }
                    i++;
                }
                if (fieldDictionary.Count > 0)
                {
                    fieldDictionary.Add(CONSTANTS.ChangeRequest, CONSTANTS.True);
                    fieldDictionary.Add(CONSTANTS.PChangeRequest, "");
                    fieldDictionary.Add(CONSTANTS.CustomValue, "");
                    missingData.Add(column, fieldDictionary);
                }

            }
            _preProcessDTO.MisingValuesData = missingData;

            //Numerical Columns Fetching data

            Dictionary<string, string> numericalDictionary = new Dictionary<string, string>();
            string[] numericalValues = { CONSTANTS.Mean, CONSTANTS.Median, CONSTANTS.Mode, CONSTANTS.CustomValue };
            foreach (var column in numericalColumns)
            {
                var value = uniqueData[CONSTANTS.ColumnUniqueValues][column];
                var numericDictionary = new Dictionary<string, string>();
                if (!string.IsNullOrEmpty(Convert.ToString(value)))
                {
                    foreach (var numericColumnn in numericalValues)
                    {
                        if (numericColumnn == CONSTANTS.CustomValue)
                        {
                            numericDictionary.Add(numericColumnn, "");
                        }
                        else
                        {
                            if (numericColumnn == CONSTANTS.Mean)
                            {
                                numericDictionary.Add(numericColumnn, CONSTANTS.True);
                            }
                            else
                            {
                                numericDictionary.Add(numericColumnn, CONSTANTS.False);
                            }

                        }
                    }
                    if (numericDictionary.Count > 0)
                    {
                        numericDictionary.Add(CONSTANTS.ChangeRequest, CONSTANTS.True);
                        numericDictionary.Add(CONSTANTS.PChangeRequest, "");
                        dataNumerical.Add(column, numericDictionary);
                    }
                }
            }
            _preProcessDTO.NumericalData = dataNumerical;
        }

        private bool EncryptDB(string correlationid)
        {
            var collection = _database.GetCollection<BsonDocument>("SSAI_DeployedModels");
            var filter = Builders<BsonDocument>.Filter.Eq("CorrelationId", correlationid);
            var projection = Builders<BsonDocument>.Projection.Include("DBEncryptionRequired").Include("CorrelationId").Exclude("_id");
            var data = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (data.Count > 0)
            {
                BsonElement element;
                var exists = data[0].TryGetElement("DBEncryptionRequired", out element);
                if (exists)
                    return (bool)data[0]["DBEncryptionRequired"];
                else
                    return false;
            }
            else
            {
                return false;
            }
        }
        private DataEngineering InstaAutoDeployModel(string correlationId, string ProblemType)
        {
            DataEngineering dataEngineering = new DataEngineering();
            try
            {
                ////Gets the records from SSAI_RecommendedTrainedModels collection for frequency & accuracy in order to update in deployed model collection
                _recommendedAI = this.GetTrainedModel(correlationId, ProblemType);
                if (_recommendedAI != null && _recommendedAI.TrainedModel != null && _recommendedAI.TrainedModel.Count > 0)
                {
                    var result = IsDeployModelComplete(correlationId, _recommendedAI, ProblemType);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaRegressionAutoTrainService), "_recommendedAI --" + result, "END", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                    if (result)
                    {
                        dataEngineering.Status = CONSTANTS.C;
                        dataEngineering.Message = "Deploy Model completed successfully";
                    }
                    else
                    {
                        dataEngineering.Status = "V";
                        dataEngineering.Message = "No record found for Correlation Id in Deploy Model";
                    }
                }
                else
                {
                    dataEngineering.Status = "V";
                    dataEngineering.Message = "No models trained for this correlation id, to proceed Deploy Model";
                }
            }
            catch (Exception ex)
            {
                dataEngineering.Message = ex.Message + "---" + ex.StackTrace;
                dataEngineering.Status = "E";
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaRegressionAutoTrainService), nameof(InstaAutoDeployModel), "END", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            return dataEngineering;
        }
        private bool IsDeployModelComplete(string correlationId, RecommedAITrainedModel trainedModel, string problemType)
        {
            bool IsCompleted = false;
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var resultData = collection.Find(filter).ToList();
            if (resultData.Count > 0)
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaRegressionAutoTrainService), "DEPLOYEDMODELS INSIDE --", "END", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                JObject data = JObject.Parse(trainedModel.TrainedModel[0].ToString());
                double accuracy = 0;
                if (resultData[0]["ModelType"].ToString() == CONSTANTS.Multi_Class || resultData[0]["ModelType"].ToString() == CONSTANTS.Classification || resultData[0]["ModelType"].ToString() == CONSTANTS.Text_Classification)
                {
                    accuracy = Convert.ToDouble(data[CONSTANTS.Accuracy]);
                }
                else
                {
                    accuracy = Convert.ToDouble(data[CONSTANTS.r2ScoreVal][CONSTANTS.error_rate]);
                }
                var builder = Builders<BsonDocument>.Update;
                var update = builder.Set(CONSTANTS.Accuracy, accuracy)
                    .Set(CONSTANTS.Status, "Deployed")
                    .Set("DeployedDate", DateTime.Now.ToString(CONSTANTS.DateHoursFormat))
                    .Set("ModelVersion", data["modelName"].ToString())
                    .Set("ModifiedOn", DateTime.Now.ToString(CONSTANTS.DateHoursFormat));
                collection.UpdateMany(filter, update);
                IsCompleted = true;
            }

            return IsCompleted;
        }
        private RecommedAITrainedModel GetTrainedModel(string correlationId, string problemType)
        {
            _recommendedAI = new RecommedAITrainedModel();
            _recommendedAI = GetRecommendedTrainedModels(correlationId);
            ////Gets the max accuracy from list of trained model based on problem type
            if (_recommendedAI != null && _recommendedAI.TrainedModel != null && _recommendedAI.TrainedModel.Count > 0)
            {
                double? maxAccuracy = null;
                switch (problemType)
                {
                    case CONSTANTS.Classification:
                        maxAccuracy = _recommendedAI.TrainedModel.Max(x => (double)x[CONSTANTS.Accuracy]);
                        _recommendedAI.TrainedModel = _recommendedAI.TrainedModel.Where(p => (double)p[CONSTANTS.Accuracy] == maxAccuracy).ToList();
                        break;

                    case CONSTANTS.Multi_Class:
                        maxAccuracy = _recommendedAI.TrainedModel.Max(x => (double)x[CONSTANTS.Accuracy]);
                        _recommendedAI.TrainedModel = _recommendedAI.TrainedModel.Where(p => (double)p[CONSTANTS.Accuracy] == maxAccuracy).ToList();
                        break;
                    case CONSTANTS.Regression:
                        maxAccuracy = _recommendedAI.TrainedModel.Max(x => (double)x[CONSTANTS.r2ScoreVal][CONSTANTS.error_rate]);
                        _recommendedAI.TrainedModel = _recommendedAI.TrainedModel.Where(p => (double)p[CONSTANTS.r2ScoreVal][CONSTANTS.error_rate] == maxAccuracy).ToList();
                        break;
                    case CONSTANTS.TimeSeries:
                        maxAccuracy = _recommendedAI.TrainedModel.Max(x => (double)x[CONSTANTS.r2ScoreVal][CONSTANTS.error_rate]);
                        _recommendedAI.TrainedModel = _recommendedAI.TrainedModel.Where(p => (double)p[CONSTANTS.r2ScoreVal][CONSTANTS.error_rate] == maxAccuracy).ToList();
                        break;
                }
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaRegressionAutoTrainService), nameof(GetTrainedModel) + "----" + _recommendedAI.TrainedModel, "END", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            return _recommendedAI;
        }
        private RecommedAITrainedModel GetRecommendedTrainedModels(string correlationId)
        {
            List<JObject> trainModelsList = new List<JObject>();
            RecommedAITrainedModel trainedModels = new RecommedAITrainedModel();
            var columnCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIRecommendedTrainedModels);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var trainedModel = columnCollection.Find(filter).ToList();
            if (trainedModel.Count() > 0)
            {
                for (int i = 0; i < trainedModel.Count; i++)
                {
                    trainModelsList.Add(JObject.Parse(trainedModel[i].ToString()));
                }
                trainedModels.TrainedModel = trainModelsList;
            }
            return trainedModels;
        }
        private bool InstaAutoPrediction(string correlationId, string userId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaRegressionAutoTrainService), nameof(InstaAutoPrediction), "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            bool ispredictionSucess = false;
            try
            {
                _deployModelViewModel = GetInstaDeployModel(correlationId);
                if (_deployModelViewModel != null && _deployModelViewModel.DeployModels.Count > 0)
                {
                    PredictionDTO predictionData = new PredictionDTO();
                    string frequency = _deployModelViewModel.DeployModels[0].Frequency;
                    PredictionDTO predictionDTO = new PredictionDTO
                    {
                        _id = Guid.NewGuid().ToString(),
                        UniqueId = Guid.NewGuid().ToString(),
                        //ActualData = CONSTANTS.Null,
                        CorrelationId = correlationId,
                        Frequency = frequency,
                        PredictedData = null,
                        Status = CONSTANTS.I,
                        ErrorMessage = null,
                        Progress = null,
                        CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                        ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                        CreatedByUser = userId,
                        ModifiedByUser = userId
                    };
                    bool DBEncryptionRequired = EncryptDB(_preProcessDTO.CorrelationId);
                    if (DBEncryptionRequired)
                        predictionDTO.ActualData =_IsAESKeyVault? CryptographyUtility.Encrypt(CONSTANTS.Null) : AesProvider.Encrypt(CONSTANTS.Null, _aesKey, _aesVector);
                    else
                        predictionDTO.ActualData = CONSTANTS.Null;

                    if (_deployModelViewModel.DeployModels[0].ModelType == CONSTANTS.TimeSeries)
                    {
                        SavePrediction(predictionDTO);
                        IngrainRequestQueue ingrainRequest = new IngrainRequestQueue
                        {
                            _id = Guid.NewGuid().ToString(),
                            CorrelationId = correlationId,
                            RequestId = Guid.NewGuid().ToString(),
                            ProcessId = null,
                            Status = null,
                            ModelName = null,
                            RequestStatus = CONSTANTS.New,
                            RetryCount = 0,
                            ProblemType = null,
                            Message = null,
                            UniId = predictionDTO.UniqueId,
                            Progress = null,
                            pageInfo = CONSTANTS.ForecastModel, // pageInfo 
                            ParamArgs = "{}",
                            Function = CONSTANTS.ForecastModel,
                            CreatedByUser = userId,
                            CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                            ModifiedByUser = userId,
                            ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                            LastProcessedOn = null
                        };
                        InsertRequests(ingrainRequest);
                    }
                    else
                    {
                        predictionDTO.ActualData = _deployModelViewModel.DeployModels[0].InputSample;
                        SavePrediction(predictionDTO);
                        IngrainRequestQueue ingrainRequest = new IngrainRequestQueue
                        {
                            _id = Guid.NewGuid().ToString(),
                            CorrelationId = correlationId,
                            RequestId = Guid.NewGuid().ToString(),
                            ProcessId = null,
                            Status = null,
                            ModelName = null,
                            RequestStatus = CONSTANTS.New,
                            RetryCount = 0,
                            ProblemType = null,
                            Message = null,
                            UniId = predictionDTO.UniqueId,
                            Progress = null,
                            pageInfo = CONSTANTS.PublishModel, // pageInfo 
                            ParamArgs = "{}",
                            Function = CONSTANTS.PublishModel,
                            CreatedByUser = userId,
                            CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                            ModifiedByUser = userId,
                            ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                            LastProcessedOn = null
                        };
                        InsertRequests(ingrainRequest);
                    }
                    Thread.Sleep(2000);
                    bool isPrediction = true;
                    while (isPrediction)
                    {
                        predictionData = GetPrediction(predictionDTO);
                        if (predictionData.Status == CONSTANTS.C)
                        {
                            ispredictionSucess = true;
                            isPrediction = false;
                        }
                        else if (predictionData.Status == "E")
                        {
                            ispredictionSucess = false;
                            isPrediction = false;
                        }
                        else
                        {
                            Thread.Sleep(1000);
                            isPrediction = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InstaRegressionAutoTrainService), nameof(InstaAutoPrediction), ex.Message + "- STACKTRACE - " + ex.StackTrace + "CORRELATIONID-" + (string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId)), ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }

            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaRegressionAutoTrainService), nameof(InstaAutoPrediction), "END", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            return ispredictionSucess;
        }
        private PredictionDTO GetPrediction(PredictionDTO predictionDTO)
        {
            PredictionDTO prediction = new PredictionDTO();
            var builder = Builders<PredictionDTO>.Filter;
            var filter = builder.Eq(CONSTANTS.CorrelationId, predictionDTO.CorrelationId) & builder.Eq("UniqueId", predictionDTO.UniqueId);
            var collection = _database.GetCollection<PredictionDTO>(CONSTANTS.SSAI_PublishModel);
            var result = collection.Find(filter).ToList();
            if (result.Count > 0)
            {
                prediction = result[0];
            }
            return prediction;
        }
        private void SavePrediction(PredictionDTO predictionDTO)
        {
            var jsonData = JsonConvert.SerializeObject(predictionDTO);
            var insertDocument = BsonSerializer.Deserialize<BsonDocument>(jsonData);
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_PublishModel);
            collection.InsertOne(insertDocument);
        }
        private DeployModelViewModel GetInstaDeployModel(string correlationId)
        {
            DeployModelViewModel deployModelView = new DeployModelViewModel();
            List<DeployModelsDto> modelsDto = new List<DeployModelsDto>();
            var modelCollection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
            var projection1 = Builders<DeployModelsDto>.Projection.Exclude(CONSTANTS.Id);
            var filterBuilder = Builders<DeployModelsDto>.Filter;
            var filter2 = filterBuilder.Eq(CONSTANTS.CorrelationId, correlationId) & filterBuilder.Eq(CONSTANTS.Status, "Deployed");
            var modelsData = modelCollection.Find(filter2).Project<DeployModelsDto>(projection1).ToList();
            if (modelsData.Count > 0)
            {
                for (int i = 0; i < modelsData.Count; i++)
                {
                    modelsDto.Add(modelsData[i]);
                }
            }
            deployModelView.DeployModels = modelsDto;
            return deployModelView;
        }
        private InstaLog Log(IngrainRequestQueue requestQueue, DeployModelsDto result)
        {
            InstaLog _instaLog = new InstaLog
            {
                _id = Guid.NewGuid().ToString(),
                InstaId = result.InstaId,
                CorrelationId = result.CorrelationId,
                ModelName = result.ModelType,
                Status = requestQueue.Status,
                ModelVersion = result.ModelVersion,
                Message = requestQueue.Message,
                ErrorMessage = requestQueue.Message,
                PageInfo = requestQueue.pageInfo,
                DeliveryConstructUID = result.DeliveryConstructUID,
                ClientUId = result.ClientUId,
                SourceName = result.SourceName,
                StartDate = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                CreatedByUser = result.CreatedByUser,
                ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                ModifiedByUser = result.CreatedByUser
            };
            return _instaLog;
        }
        private void InsertAutoLog(InstaLog log)
        {
            var requestQueue = JsonConvert.SerializeObject(log);
            var insertRequestQueue = BsonSerializer.Deserialize<BsonDocument>(requestQueue);
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.Insta_AutoLog);
            collection.InsertOne(insertRequestQueue);
        }
    }
}
