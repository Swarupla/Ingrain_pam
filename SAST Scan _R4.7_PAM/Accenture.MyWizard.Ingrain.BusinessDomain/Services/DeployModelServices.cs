#region Copyright (c) 2018 Accenture . All rights reserved.

// <copyright company="Accenture">
// Copyright (c) 2018 Accenture.  All rights reserved.
// Reproduction or transmission in whole or in part, in any form or by any means, 
// electronic, mechanical or otherwise, is prohibited without the prior written 
// consent of the copyright owner.
// </copyright>

#endregion

#region DeployModelServices Information
/********************************************************************************************************\
Module Name     :   DeployModelServices
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
    using Accenture.MyWizard.Ingrain.BusinessDomain.Common;
    using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
    using Accenture.MyWizard.Ingrain.DataAccess;
    using Accenture.MyWizard.Ingrain.DataModels.Models;
    using Accenture.MyWizard.Shared.Helpers;
    using Microsoft.Extensions.Options;
    using Microsoft.Extensions.DependencyInjection;
    #region Namespace  
    using MongoDB.Bson;
    using MongoDB.Bson.Serialization;
    using MongoDB.Driver;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
    using System.Threading;
    using System.Text;
    using System.IO;
    using System.Net;
    #endregion

    public class DeployModelServices : IDeployedModelService
    {
        #region Members
        private MongoClient _mongoClient;
        private IMongoDatabase _database;
        RecommendedAIViewModelDTO recommendedModeldto = new RecommendedAIViewModelDTO();
        private readonly IOptions<IngrainAppSettings> appSettings;
        DatabaseProvider databaseProvider;
        private IEncryptionDecryption _encryptionDecryption;
        private readonly IIngestedData _ingestedData;
        private IScopeSelectorService _ScopeSelector;
        private bool _isCascadeModel;
        private bool _isCustomCascadeModel;
        private bool _isCustomCascadeNotificationRequired;
        private MongoClient _mongoClientAD;
        private IMongoDatabase _databaseAD;
        private string servicename = "";
        #endregion

        #region Constructors
        public DeployModelServices(DatabaseProvider db, IOptions<IngrainAppSettings> settings, IServiceProvider serviceProvider)
        {
            databaseProvider = db;
            appSettings = settings;
            _mongoClient = databaseProvider.GetDatabaseConnection();
            var dataBaseName = MongoUrl.Create(appSettings.Value.connectionString).DatabaseName;
            _database = _mongoClient.GetDatabase(dataBaseName);
            _encryptionDecryption = serviceProvider.GetService<IEncryptionDecryption>();
            _ingestedData = serviceProvider.GetService<IIngestedData>();
            _ScopeSelector = serviceProvider.GetService<IScopeSelectorService>();
            _isCascadeModel = false;
            _isCustomCascadeModel = false;
            _isCustomCascadeNotificationRequired = true;
            //Anomaly Detection Connection
            _mongoClientAD = databaseProvider.GetDatabaseConnection("Anomaly");
            var dataBaseNameAD = MongoUrl.Create(appSettings.Value.AnomalyDetectionCS).DatabaseName;
            _databaseAD = _mongoClientAD.GetDatabase(dataBaseNameAD);
        }
        #endregion

        public RecommedAITrainedModel GetPublishedModels(string correlationId, string ServiceName = "")
        {
            servicename = ServiceName;
            bool DBEncryptionRequired = CommonUtility.EncryptDB(correlationId, appSettings, servicename);
            string problemType = string.Empty;
            List<JObject> trainModelsList = new List<JObject>();
            RecommedAITrainedModel trainedModels = new RecommedAITrainedModel();
            IMongoCollection<BsonDocument> columnCollection;
            IMongoCollection<BsonDocument> cascadeCollection;
            IMongoCollection<BsonDocument> modelCollection;
            if (servicename == "Anomaly")
            {
                columnCollection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.SSAIRecommendedTrainedModels);
                cascadeCollection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.SSAICascadedModels);
                modelCollection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
            }
            else
            {
                columnCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIRecommendedTrainedModels);
                cascadeCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAICascadedModels);
                modelCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
            }
            //var columnCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIRecommendedTrainedModels);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var problemProjection = Builders<BsonDocument>.Projection.Include(CONSTANTS.ProblemType).Exclude(CONSTANTS.Id);
            var problemtypeResult = columnCollection.Find(filter).Project<BsonDocument>(problemProjection).ToList();
            //var cascadeCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAICascadedModels);
            var cascadeFilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CascadedId, correlationId);
            var cascadeProjection = Builders<BsonDocument>.Projection.Exclude("MappingData").Exclude(CONSTANTS.Id);
            var cascadeResult = cascadeCollection.Find(cascadeFilter).Project<BsonDocument>(cascadeProjection).ToList();
            if (cascadeResult.Count > 0)
            {
                DeployModelDetails modeldetails = new DeployModelDetails();
                JObject data = JObject.Parse(cascadeResult[0].ToString());
                foreach (var item in data[CONSTANTS.ModelList].Children())
                {
                    int count = data[CONSTANTS.ModelList].Children().Count();
                    string model = string.Format("Model{0}", count);
                    modeldetails = JsonConvert.DeserializeObject<DeployModelDetails>(data[CONSTANTS.ModelList][model].ToString());
                    problemType = modeldetails.ProblemType;
                    break;
                }
                ProjectionDefinition<BsonDocument> projection = null;
                //if (problemType == "regression" || problemType == "TimeSeries")
                var cascadeFilter2 = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, modeldetails.CorrelationId);
                if (problemType.Equals(CONSTANTS.regression, StringComparison.InvariantCultureIgnoreCase) || problemType == CONSTANTS.TimeSeries)
                {
                    projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.modelName).Include(CONSTANTS.r2ScoreVal_error_rate).Include(CONSTANTS.Id).Include(CONSTANTS.Frequency);
                }
                else
                {
                    projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.modelName).Include(CONSTANTS.Accuracy).Include(CONSTANTS.Id);
                }

                var trainedModel = columnCollection.Find(cascadeFilter2).Project<BsonDocument>(projection).ToList();
                if (trainedModel.Count() > 0)
                {
                    for (int i = 0; i < trainedModel.Count; i++)
                    {
                        if (DBEncryptionRequired)
                        {
                            try
                            {
                                if (trainedModel[i].Contains(CONSTANTS.CreatedByUser) && !string.IsNullOrEmpty(Convert.ToString(trainedModel[i][CONSTANTS.CreatedByUser])))
                                {
                                    trainedModel[i][CONSTANTS.CreatedByUser] = _encryptionDecryption.Decrypt(Convert.ToString(trainedModel[i][CONSTANTS.CreatedByUser]));
                                }
                            }
                            catch (Exception ex) { LOGGING.LogManager.Logger.LogProcessInfo(typeof(DeployModelServices), nameof(GetPublishedModels) + "User is already decrypted....", ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty); }
                            //try
                            //{
                            //    if (trainedModel[i].Contains(CONSTANTS.ModifiedByUser) && !string.IsNullOrEmpty(Convert.ToString(trainedModel[i][CONSTANTS.ModifiedByUser])))
                            //    {
                            //        trainedModel[i][CONSTANTS.ModifiedByUser] = _encryptionDecryption.Decrypt(Convert.ToString(trainedModel[i][CONSTANTS.ModifiedByUser]));
                            //    }
                            //}
                            //catch (Exception) { }
                        }
                        trainModelsList.Add(JObject.Parse(trainedModel[i].ToString()));
                    }
                    trainedModels.TrainedModel = trainModelsList;
                }
                var filterBuilder2 = Builders<BsonDocument>.Filter;
                ProjectionDefinition<BsonDocument> projectionHyperTune = null;
                IMongoCollection<BsonDocument> hyperTuneCollection;
                if (servicename == "Anomaly")
                    hyperTuneCollection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.ME_HyperTuneVersion);
                else
                    hyperTuneCollection = _database.GetCollection<BsonDocument>(CONSTANTS.ME_HyperTuneVersion);

                // var hyperTuneCollection = _database.GetCollection<BsonDocument>(CONSTANTS.ME_HyperTuneVersion);
                var filterHyperTune = filterBuilder2.Eq(CONSTANTS.CorrelationId, correlationId) & filterBuilder2.Eq(CONSTANTS.Temp, true);
                if (problemType.Equals(CONSTANTS.regression, StringComparison.InvariantCultureIgnoreCase) || problemType == CONSTANTS.TimeSeries)
                {
                    projectionHyperTune = Builders<BsonDocument>.Projection.Include(CONSTANTS.VersionName).Include(CONSTANTS.r2ScoreVal_error_rate).Include(CONSTANTS.Id);
                }
                else
                {
                    projectionHyperTune = Builders<BsonDocument>.Projection.Include(CONSTANTS.VersionName).Include(CONSTANTS.Accuracy).Include(CONSTANTS.Id);
                }

                var hyperTuneModel = hyperTuneCollection.Find(cascadeFilter2).Project<BsonDocument>(projectionHyperTune).ToList();

                if (hyperTuneModel.Count() > 0)
                {
                    for (int i = 0; i < hyperTuneModel.Count; i++)
                    {
                        JObject j = new JObject();
                        j[CONSTANTS.Id] = hyperTuneModel[i][CONSTANTS.Id].ToString();
                        j[CONSTANTS.modelName] = hyperTuneModel[i][CONSTANTS.VersionName].ToString();
                        if (problemType.Equals(CONSTANTS.regression, StringComparison.InvariantCultureIgnoreCase) || problemType == CONSTANTS.TimeSeries)
                        {
                            JObject outlierData = new JObject();
                            outlierData[CONSTANTS.error_rate] = hyperTuneModel[i][CONSTANTS.r2ScoreVal][CONSTANTS.error_rate].ToString();
                            j[CONSTANTS.r2ScoreVal] = JObject.FromObject(outlierData);
                        }
                        else
                        {
                            j[CONSTANTS.Accuracy] = hyperTuneModel[i][CONSTANTS.Accuracy].ToString();
                        }
                        if (!(j[CONSTANTS.modelName].ToString() == CONSTANTS.BsonNull))
                        {
                            trainModelsList.Add(j);
                        }
                    }
                    trainedModels.TrainedModel = trainModelsList;
                }
            }
            else
            {
                if (problemtypeResult.Count > 0)
                {
                    problemType = problemtypeResult[0][CONSTANTS.ProblemType].ToString();
                }
                ProjectionDefinition<BsonDocument> projection = null;
                //if (problemType == "regression" || problemType == "TimeSeries")
                if (problemType.Equals(CONSTANTS.regression, StringComparison.InvariantCultureIgnoreCase) || problemType == CONSTANTS.TimeSeries)
                {
                    projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.modelName).Include(CONSTANTS.r2ScoreVal_error_rate).Include(CONSTANTS.Id).Include(CONSTANTS.Frequency).Include("Version");
                }
                else
                {
                    projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.modelName).Include(CONSTANTS.Accuracy).Include(CONSTANTS.Id).Include("Version");
                }

                var trainedModel = columnCollection.Find(filter).Project<BsonDocument>(projection).ToList();
                if (trainedModel.Count() > 0)
                {
                    bool versionAvaialble = false;
                    foreach (var item in trainedModel)
                    {
                        if (!item["Version"].IsBsonNull && item["Version"].ToString() != null)
                        {
                            int val = Convert.ToInt32(item["Version"]);
                            if (val == 1)
                            {
                                versionAvaialble = true;
                                break;
                            }
                        }
                    }
                    for (int i = 0; i < trainedModel.Count; i++)
                    {
                        JObject parsedData = JObject.Parse(trainedModel[i].ToString());
                        if (versionAvaialble)
                        {
                            if (!string.IsNullOrEmpty(Convert.ToString(parsedData["Version"])))
                            {
                                if (Convert.ToInt32(parsedData["Version"]) == 1)
                                {
                                    trainModelsList.Add(parsedData);
                                }
                            }
                        }
                        else
                            trainModelsList.Add(parsedData);
                    }
                    trainedModels.TrainedModel = trainModelsList;
                }
                var filterBuilder2 = Builders<BsonDocument>.Filter;
                ProjectionDefinition<BsonDocument> projectionHyperTune = null;
                IMongoCollection<BsonDocument> hyperTuneCollection;
                if (servicename == "Anomaly")
                    hyperTuneCollection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.ME_HyperTuneVersion);
                else
                    hyperTuneCollection = _database.GetCollection<BsonDocument>(CONSTANTS.ME_HyperTuneVersion);
                //var hyperTuneCollection = _database.GetCollection<BsonDocument>(CONSTANTS.ME_HyperTuneVersion);
                var filterHyperTune = filterBuilder2.Eq(CONSTANTS.CorrelationId, correlationId) & filterBuilder2.Eq(CONSTANTS.Temp, true);
                if (problemType.Equals(CONSTANTS.regression, StringComparison.InvariantCultureIgnoreCase) || problemType == CONSTANTS.TimeSeries)
                {
                    projectionHyperTune = Builders<BsonDocument>.Projection.Include(CONSTANTS.VersionName).Include(CONSTANTS.r2ScoreVal_error_rate).Include(CONSTANTS.Id);
                }
                else
                {
                    projectionHyperTune = Builders<BsonDocument>.Projection.Include(CONSTANTS.VersionName).Include(CONSTANTS.Accuracy).Include(CONSTANTS.Id);
                }

                var hyperTuneModel = hyperTuneCollection.Find(filter).Project<BsonDocument>(projectionHyperTune).ToList();

                if (hyperTuneModel.Count() > 0)
                {
                    for (int i = 0; i < hyperTuneModel.Count; i++)
                    {
                        JObject j = new JObject();
                        j[CONSTANTS.Id] = hyperTuneModel[i][CONSTANTS.Id].ToString();
                        j[CONSTANTS.modelName] = hyperTuneModel[i][CONSTANTS.VersionName].ToString();
                        if (problemType.Equals(CONSTANTS.regression, StringComparison.InvariantCultureIgnoreCase) || problemType == CONSTANTS.TimeSeries)
                        {
                            JObject outlierData = new JObject();
                            outlierData[CONSTANTS.error_rate] = hyperTuneModel[i][CONSTANTS.r2ScoreVal][CONSTANTS.error_rate].ToString();
                            j[CONSTANTS.r2ScoreVal] = JObject.FromObject(outlierData);
                        }
                        else
                        {
                            j[CONSTANTS.Accuracy] = hyperTuneModel[i][CONSTANTS.Accuracy].ToString();
                        }
                        if (!(j[CONSTANTS.modelName].ToString() == CONSTANTS.BsonNull))
                        {
                            trainModelsList.Add(j);
                        }
                    }
                    trainedModels.TrainedModel = trainModelsList;
                }
            }

            //var modelCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
            var projection1 = Builders<BsonDocument>.Projection.Include(CONSTANTS.ModelVersion).Exclude(CONSTANTS.Id);
            var projection2 = Builders<BsonDocument>.Projection.Include(CONSTANTS.IsFMModel).Include(CONSTANTS.IsCascadingButton).Include("IsModelTemplateDataSource").Exclude(CONSTANTS.Id);
            var filterBuilder = Builders<BsonDocument>.Filter;
            var filter2 = filterBuilder.Eq(CONSTANTS.CorrelationId, correlationId) & filterBuilder.Eq(CONSTANTS.Status, CONSTANTS.Deployed);
            var modelsData = modelCollection.Find(filter2).Project<BsonDocument>(projection1).ToList();
            var modelTemplateDataSource = modelCollection.Find(filter).Project<BsonDocument>(projection2).ToList();
            List<DeployedModelVersions> modelVersions = new List<DeployedModelVersions>();
            if (modelsData.Count > 0)
            {
                for (int i = 0; i < modelsData.Count; i++)
                {
                    if (DBEncryptionRequired)
                    {
                        try
                        {
                            if (modelsData[i].Contains(CONSTANTS.CreatedByUser) && !string.IsNullOrEmpty(Convert.ToString(modelsData[i][CONSTANTS.CreatedByUser])))
                            {
                                modelsData[i][CONSTANTS.CreatedByUser] = _encryptionDecryption.Decrypt(Convert.ToString(modelsData[i][CONSTANTS.CreatedByUser]));
                            }
                        }
                        catch (Exception ex) { LOGGING.LogManager.Logger.LogProcessInfo(typeof(DeployModelServices), nameof(GetPublishedModels) + "User is already decrypted....", ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty); }
                        try
                        {
                            if (modelsData[i].Contains(CONSTANTS.ModifiedByUser) && !string.IsNullOrEmpty(Convert.ToString(modelsData[i][CONSTANTS.ModifiedByUser])))
                            {
                                modelsData[i][CONSTANTS.ModifiedByUser] = _encryptionDecryption.Decrypt(Convert.ToString(modelsData[i][CONSTANTS.ModifiedByUser]));
                            }
                        }
                        catch (Exception ex) { LOGGING.LogManager.Logger.LogProcessInfo(typeof(DeployModelServices), nameof(GetPublishedModels) + "User is already decrypted....", ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty); }
                    }
                    var versionsData = JsonConvert.DeserializeObject<DeployedModelVersions>(modelsData[i].ToString());
                    modelVersions.Add(versionsData);

                }
            }
            if (modelTemplateDataSource.Count > 0)
            {
                if (modelTemplateDataSource[0].Contains(CONSTANTS.IsFMModel))
                {
                    trainedModels.IsFmModel = modelTemplateDataSource[0][CONSTANTS.IsFMModel].ToBoolean();
                }
                if (modelTemplateDataSource[0].Contains("IsModelTemplateDataSource"))
                {
                    trainedModels.IsModelTemplateDataSource = modelTemplateDataSource[0]["IsModelTemplateDataSource"].ToBoolean();
                }
                if (modelTemplateDataSource[0].Contains(CONSTANTS.IsCascadingButton))
                {
                    trainedModels.IsCascadingButton = Convert.ToBoolean(modelTemplateDataSource[0][CONSTANTS.IsCascadingButton]);
                }
            }
            trainedModels.DeployedModelVersions = modelVersions;
            ValidRecordsDetailsModel validRecordsDetailModel = CommonUtility.GetModelDataPointdDetails(correlationId, appSettings, servicename);
            if (validRecordsDetailModel != null)
            {
                if (validRecordsDetailModel.ValidRecordsDetails != null)
                {
                    if (validRecordsDetailModel.ValidRecordsDetails.Records[0] >= 4 && validRecordsDetailModel.ValidRecordsDetails.Records[0] < 20)
                    {
                        trainedModels.DataPointsWarning = CONSTANTS.DataPointsMinimum;
                        trainedModels.DataPointsCount = validRecordsDetailModel.ValidRecordsDetails.Records[0];
                    }
                }
            }
            List<string> datasource = new List<string>();
            datasource = CommonUtility.GetDataSourceModel(correlationId, appSettings, servicename);
            List<string> datasourceDetails = new List<string>();
            datasourceDetails = CommonUtility.GetDataSourceModelDetails(correlationId, appSettings, servicename);
            if (cascadeResult.Count > 0)
            {
                trainedModels.ModelName = datasource[0];
                trainedModels.DataSource = datasource[1];
                trainedModels.ModelType = problemType;
                trainedModels.Category = datasource[3];
                trainedModels.BusinessProblems = datasource[1];
            }
            else
            {
                if (datasource.Count > 0)
                {
                    trainedModels.ModelName = datasource[0];
                    trainedModels.DataSource = datasource[1];
                    trainedModels.ModelType = datasource[2];
                    if (datasource.Count > 2)
                        trainedModels.BusinessProblems = datasource[3];
                    if (!string.IsNullOrEmpty(datasource[4]))
                    {
                        trainedModels.InstaFlag = true;
                        trainedModels.Category = datasource[5];
                    }
                }
            }
            trainedModels.Category = datasourceDetails[3];
            return trainedModels;
        }

        public DeployModelViewModel DeployModel(dynamic data, string ServiceName = "")
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DeployModelServices), "DeployModel", "START", string.Empty, string.Empty, string.Empty, string.Empty);
            servicename = ServiceName;
            IMongoCollection<BsonDocument> cascadeCollection;
            IMongoCollection<DeployModelsDto> modelCollection;
            IMongoCollection<AppNotificationLog> notificationCollection;
            IMongoCollection<BsonDocument> requestCollection;
            IMongoCollection<BsonDocument> Recommended_collection;
            IMongoCollection<BsonDocument> savemodels_collection;
            if (servicename == "Anomaly")
            {
                cascadeCollection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.SSAICascadedModels);
                modelCollection = _databaseAD.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
                notificationCollection = _databaseAD.GetCollection<AppNotificationLog>("AppNotificationLog");
                requestCollection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
                Recommended_collection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.SSAIRecommendedTrainedModels);
                savemodels_collection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.SSAI_savedModels);
            }
            else
            {
                cascadeCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAICascadedModels);
                modelCollection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
                notificationCollection = _database.GetCollection<AppNotificationLog>("AppNotificationLog");
                requestCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
                Recommended_collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIRecommendedTrainedModels);
                savemodels_collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_savedModels);
            }
            PublicTemplateMapping templateMapping = new PublicTemplateMapping();
            DeployModelViewModel deployModelView = new DeployModelViewModel();
            List<DeployModelsDto> modelsDto = new List<DeployModelsDto>();
            string[] linkedApps = JsonConvert.DeserializeObject<string[]>(data.LinkedApps.ToString());
            dynamic appDetails = GetAppName(linkedApps[0]);
            string operation = "Created";
            int retentionDuration = 180;
            if (data.ArchivalDays != "{}" && data.ArchivalDays != null && data.ArchivalDays != "")
                retentionDuration = Convert.ToInt32(data.ArchivalDays) * 30;
            PublishModelFrequency frequency = new PublishModelFrequency();
            DeployedModel deployedModel = new DeployedModel
            {
                CorrelationId = data.correlationId.ToString(),
                App = linkedApps,
                IsPrivate = Convert.ToBoolean(data.IsPrivate),
                IsModelTemplate = Convert.ToBoolean(data.IsModelTemplate),
                Status = CONSTANTS.Deployed,
                AppId = data.AppId,
                WebServices = "webservice",
                ModelName = data.ModelName.ToString(),
                DeployedDate = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                UserId = data.userid.ToString(),
                ModelType = data.ModelType.ToString(),
                MaxDataPull = (data.MaxDataPull != null && data.MaxDataPull != string.Empty) ? Convert.ToInt32(data.MaxDataPull) : 0,
                IsCarryOutRetraining = Convert.ToBoolean(data.IsCarryOutRetraining),
                IsOnline = Convert.ToBoolean(data.IsOnline),
                IsOffline = Convert.ToBoolean(data.IsOffline),
                Retraining = frequency,
                Training = frequency,
                Prediction = frequency,
                ArchivalDays = retentionDuration
            };
            if (servicename == "Anomaly")
            {
                if (data.Threshold != "{}" && data.Threshold != null && data.Threshold != "")
                    deployedModel.Threshold = Convert.ToInt32(data.Threshold);
            }
            //var cascadeCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAICascadedModels);
            var cascadeFilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CascadedId, deployedModel.CorrelationId);
            var cascadeProjection = Builders<BsonDocument>.Projection.Exclude("MappingData").Exclude(CONSTANTS.Id);
            var cascadeResult = cascadeCollection.Find(cascadeFilter).Project<BsonDocument>(cascadeProjection).ToList();
            if (cascadeResult.Count > 0)
            {
                JObject cascadeModelList = JObject.Parse(cascadeResult[0].ToString());
                _isCascadeModel = true;
                _isCustomCascadeModel = Convert.ToBoolean(cascadeModelList[CONSTANTS.IsCustomModel]);
                if (deployedModel.IsPrivate == false && deployedModel.IsModelTemplate)
                {
                    if (cascadeModelList != null)
                    {
                        foreach (var item in cascadeModelList[CONSTANTS.ModelList].Children())
                        {
                            JProperty prop = item as JProperty;
                            DeployModelDetails model = JsonConvert.DeserializeObject<DeployModelDetails>(prop.Value.ToString());
                            if (model.LinkedApps == "Ingrain")
                            {
                                deployModelView.IsException = true;
                                deployModelView.ErrorMessage = CONSTANTS.CascadeModelMessage;
                                deployModelView.IsCascadeModel = true;
                                return deployModelView;
                            }
                        }
                    }
                }
            }
            //To Check, user roles for deploy model as public
            if (deployedModel.IsPrivate == false && deployedModel.IsModelTemplate)
            {
                dynamic userrole = null;
                userrole = _ScopeSelector.GetUserRole(deployedModel.UserId);
                if (userrole != null && userrole.Count > 0)
                {
                    if (userrole[0]["AccessPrivilegeCode"].ToString().ToUpper() != "RWD")
                    {
                        throw new Exception("Non Admin User Login");
                    }
                }
            }
            //Note: Defect:2140547 - Applink is applicabl only for VDS. For other Custom Apps it should be null.
            if (appDetails != null)
            {
                AppIntegration oApp = JsonConvert.DeserializeObject<AppIntegration>(JsonConvert.SerializeObject(appDetails));
                string[] applicationName = new string[] { CONSTANTS.VDS_SI, CONSTANTS.VDS, CONSTANTS.VDS_AIOPS };
                //if (linkedApps[0] != CONSTANTS.VDS_SI)
                if (!applicationName.Contains(linkedApps[0]))
                {
                    deployedModel.VdsLink = null;
                }
                else
                {
                    if (oApp.BaseURL != null)
                    {
                        deployedModel.VdsLink = oApp.BaseURL;
                    }
                    else
                    {
                        deployedModel.VdsLink = appSettings.Value.VDSLink;
                    }
                }
            }
            else
            {
                if (servicename != "Anomaly")
                    deployedModel.VdsLink = appSettings.Value.VDSLink;
            }

            //Configuring Retraining/Training/Predicion frequencies before deploying the model.
            //Retraining - The models deployed will trigger through WS at given Retraining frequency in days (For Public/Private/ModelTemplate)
            //Training/Prediction - The models deployed will trigger through WS at given Training/Prediction frequency in days (Only Model Template - Offline)
            if (deployedModel.IsCarryOutRetraining && data.Retraining != null && ((JContainer)data.Retraining).HasValues)
            {
                deployedModel.Retraining = new PublishModelFrequency();
                deployedModel.DeployedDate = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                this.AssignFrequency(deployedModel, deployedModel.Retraining, data.Retraining, CONSTANTS.Retraining);

            }
            if (deployedModel.IsOffline)
            {
                if (data.Training != null && ((JContainer)data.Training).HasValues)
                {
                    deployedModel.Training = new PublishModelFrequency();
                    this.AssignFrequency(deployedModel, deployedModel.Training, data.Training, CONSTANTS.Training);
                }
                if (data.Prediction != null && ((JContainer)data.Prediction).HasValues)
                {
                    deployedModel.Prediction = new PublishModelFrequency();
                    this.AssignFrequency(deployedModel, deployedModel.Prediction, data.Prediction, CONSTANTS.Prediction);
                }
            }


            List<string> deployedTimeSeriesVesrions = new List<string>();
            List<string> trainedModelIds = new List<string>();
            List<double> accuracyList = new List<double>();
            List<string> frequencies = new List<string>();
            if (data.ModelType.ToString() == CONSTANTS.TimeSeries)
            {
                JObject timeSeriesVersions = JObject.Parse(data.ModelVersion.ToString());
                string forecastURL = appSettings.Value.foreCastModel;//ConfigurationManager.AppSettings["ForeCastModel"];
                foreach (var item in timeSeriesVersions.Children())
                {
                    JProperty jProperty = item as JProperty;
                    if (jProperty != null)
                    {
                        foreach (var property in jProperty.Children())
                        {
                            deployedTimeSeriesVesrions.Add(Convert.ToString(property[CONSTANTS.ModelName]));
                            trainedModelIds.Add(Convert.ToString(property[CONSTANTS.ModelId]));
                            accuracyList.Add(Convert.ToDouble(property[CONSTANTS.ModelAccuracy]));
                            frequencies.Add(Convert.ToString(property[CONSTANTS.Frequency]));
                        }
                    }
                }

                if (timeSeriesVersions.Count > 0)
                {
                    for (int i = 0; i < deployedTimeSeriesVesrions.Count; i++)
                    {
                        deployedModel.ModelVersion = deployedTimeSeriesVesrions[i];
                        deployedModel.TrainedModelId = trainedModelIds[i];
                        deployedModel.Accuracy = accuracyList[i];
                        deployedModel.Frequency = frequencies[i];
                        deployedModel.Url = string.Format(forecastURL, data.correlationId, deployedModel.Frequency);
                        deployedModel.ArchivalDays = retentionDuration;
                        deployedModel.DeployedDate = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                        AssignValues(deployedModel, data.ModelType.ToString(), i, data);
                    }
                }
            }
            else
            {
                string publishurl = appSettings.Value.publishURL;//ConfigurationManager.AppSettings["PublishURL"];
                deployedModel.Url = string.Format(publishurl + CONSTANTS.Zero, data.correlationId);
                deployedModel.ModelVersion = data.ModelVersion.ToString();
                deployedModel.Accuracy = Convert.ToDouble(data.Accuracy);
                deployedModel.ArchivalDays = retentionDuration;
                deployedModel.DeployedDate = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                AssignValues(deployedModel, data.ModelType.ToString(), null, data);
            }

            //var projection = Builders<DeployModelsDto>.Projection.Include(CONSTANTS.ModelName).Include(CONSTANTS.Accuracy).
            // Include(CONSTANTS.DeployedDate).Include(CONSTANTS.LinkedApps).Include(CONSTANTS.InputSample).Include(CONSTANTS.VDSLink).Exclude(CONSTANTS.Id);
            //var modelCollection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
            var projection1 = Builders<DeployModelsDto>.Projection.Exclude(CONSTANTS.Id);
            var filterBuilder = Builders<DeployModelsDto>.Filter;
            var filter2 = filterBuilder.Eq(CONSTANTS.CorrelationId, deployedModel.CorrelationId) & filterBuilder.Eq(CONSTANTS.Status, CONSTANTS.Deployed);
            var modelsData = modelCollection.Find(filter2).Project<DeployModelsDto>(projection1).ToList();
            bool DBEncryptionRequired = CommonUtility.EncryptDB(deployedModel.CorrelationId, appSettings, servicename);
            if (modelsData.Count > 0)
            {
                for (int i = 0; i < modelsData.Count; i++)
                {
                    if (DBEncryptionRequired)
                    {
                        try
                        {
                            if (!string.IsNullOrEmpty(Convert.ToString(modelsData[i].CreatedByUser)))
                            {
                                modelsData[i].CreatedByUser = _encryptionDecryption.Decrypt(Convert.ToString(modelsData[i].CreatedByUser));
                            }
                        }
                        catch (Exception ex) { LOGGING.LogManager.Logger.LogProcessInfo(typeof(DeployModelServices), nameof(DeployModel) + "User is already decrypted....", ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty); }
                        try
                        {
                            if (!string.IsNullOrEmpty(Convert.ToString(modelsData[i].ModifiedByUser)))
                            {
                                modelsData[i].ModifiedByUser = _encryptionDecryption.Decrypt(Convert.ToString(modelsData[i].ModifiedByUser));
                            }
                        }
                        catch (Exception ex) { LOGGING.LogManager.Logger.LogProcessInfo(typeof(DeployModelServices), nameof(DeployModel) + "User is already decrypted....", ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty); }
                    }
                    modelsDto.Add(modelsData[i]);
                }
            }
            deployModelView.DeployModels = modelsDto;

            List<string> datasource = new List<string>();
            datasource = CommonUtility.GetDataSourceModel(deployedModel.CorrelationId, appSettings, servicename);
            if (cascadeResult.Count > 0)
            {
                var updatecascade = Builders<BsonDocument>.Update.Set(CONSTANTS.Status, CONSTANTS.Deployed);
                var updatecasCaderesult = cascadeCollection.UpdateOne(cascadeFilter, updatecascade);
                var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, deployedModel.CorrelationId);
                JObject dataResult = JObject.Parse(cascadeResult[0].ToString());
                if (dataResult != null)
                {
                    if (_isCustomCascadeModel)
                    {
                        if (dataResult[CONSTANTS.ModelList].Children().Count() > 2)
                        {
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DeployModelServices), "CASCADING UPDATE", "SAMLPEINPUT UPDATE START" + deployedModel.CorrelationId, string.Empty, string.Empty, string.Empty, string.Empty);
                            string sampleInput = AddCascadeSampleInput(cascadeResult[0]);
                            string inputSample = !string.IsNullOrEmpty(sampleInput) ? sampleInput.Replace(System.Environment.NewLine, String.Empty).Replace("\r\n", string.Empty) : null;

                            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
                            if (DBEncryptionRequired)
                            {
                                var update = Builders<BsonDocument>.Update.Set(CONSTANTS.InputSample, _encryptionDecryption.Encrypt(inputSample)).Set(CONSTANTS.ModelType, deployedModel.ModelType).Set(CONSTANTS.ModelVersion, deployedModel.ModelVersion);
                                var updateresult = collection.UpdateOne(filter, update);
                            }
                            else
                            {
                                var update = Builders<BsonDocument>.Update.Set(CONSTANTS.InputSample, inputSample).Set(CONSTANTS.ModelType, deployedModel.ModelType).Set(CONSTANTS.ModelVersion, deployedModel.ModelVersion);
                                var updateresult = collection.UpdateOne(filter, update);
                            }
                        }
                    }
                    else
                    {
                        IMongoCollection<BsonDocument> collection;
                        if (servicename == "Anomaly")
                            collection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
                        else
                            collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(DeployModelServices), "CASCADING UPDATE", "SAMLPEINPUT UPDATE START" + deployedModel.CorrelationId, string.Empty, string.Empty, string.Empty, string.Empty);
                        string sampleInput = AddCascadeSampleInput(cascadeResult[0]);
                        string inputSample = !string.IsNullOrEmpty(sampleInput) ? sampleInput.Replace(System.Environment.NewLine, String.Empty).Replace("\r\n", string.Empty) : null;
                        //var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
                        if (DBEncryptionRequired)
                        {
                            var update = Builders<BsonDocument>.Update.Set(CONSTANTS.InputSample, _encryptionDecryption.Encrypt(inputSample)).Set(CONSTANTS.ModelType, deployedModel.ModelType).Set(CONSTANTS.ModelVersion, deployedModel.ModelVersion);
                            var updateresult = collection.UpdateOne(filter, update);
                        }
                        else
                        {
                            var update = Builders<BsonDocument>.Update.Set(CONSTANTS.InputSample, inputSample).Set(CONSTANTS.ModelType, deployedModel.ModelType).Set(CONSTANTS.ModelVersion, deployedModel.ModelVersion);
                            var updateresult = collection.UpdateOne(filter, update);
                        }


                    }
                }

                deployModelView.ModelName = datasource[0];
                deployModelView.DataSource = datasource[1];
                deployModelView.ModelType = deployedModel.ModelType;
                deployModelView.Category = datasource[3];
                deployModelView.BusinessProblem = datasource[1];
                if (linkedApps != null && !deployedModel.IsPrivate && deployedModel.IsModelTemplate)
                {
                    JObject cascadeModelList = JObject.Parse(cascadeResult[0].ToString());
                    if (cascadeModelList != null)
                    {
                        foreach (var item in cascadeModelList[CONSTANTS.ModelList].Children())
                        {
                            JProperty prop = item as JProperty;
                            DeployModelDetails model = JsonConvert.DeserializeObject<DeployModelDetails>(prop.Value.ToString());
                            if (model != null)
                            {
                                var Sourcenamefilter2 = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, model.CorrelationId);
                                //Inserting sub model as model template
                                //Public Templates
                                IMongoCollection<BsonDocument> templateCollection;
                                IMongoCollection<BsonDocument> deployedModelCollection;
                                if (servicename == "Anomaly")
                                {
                                    templateCollection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.SSAIPublicTemplates);
                                    deployedModelCollection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
                                }
                                else
                                {
                                    templateCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIPublicTemplates);
                                    deployedModelCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
                                }
                                //var templateCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIPublicTemplates);
                                var templateResult = templateCollection.Find(Sourcenamefilter2).ToList();
                                if (templateResult.Count > 0)
                                {
                                    var templateUpdate = Builders<BsonDocument>.Update.Set(CONSTANTS.ModelName1, deployedModel.Category);
                                    var updateResult = templateCollection.UpdateOne(filter, templateUpdate);
                                }
                                else
                                {
                                    string[] categories = new string[] { CONSTANTS.Application_Development, CONSTANTS.AgileDelivery, CONSTANTS.Devops };
                                    string[] modelNames = new string[] { model.ModelName, deployedModel.Category };
                                    publicTemplateDTO publicTemplate = new publicTemplateDTO
                                    {
                                        _id = Guid.NewGuid().ToString(),
                                        CorrelationId = model.CorrelationId,
                                        CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                                        ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                                        CreatedByUser = DBEncryptionRequired ? _encryptionDecryption.Encrypt(deployedModel.UserId) : deployedModel.UserId,
                                        ModifiedByUser = DBEncryptionRequired ? _encryptionDecryption.Encrypt(deployedModel.UserId) : deployedModel.UserId,
                                        Category = categories,
                                        ModelName = modelNames,
                                        ApplicationName = model.LinkedApps,
                                        ArchivalDays = deployedModel.ArchivalDays
                                    };
                                    var jsonData = JsonConvert.SerializeObject(publicTemplate);
                                    var insertDocument = BsonSerializer.Deserialize<BsonDocument>(jsonData);
                                    templateCollection.InsertOne(insertDocument);
                                }
                                //end
                                var Sourcenamefilter1 = filterBuilder.Eq(CONSTANTS.CorrelationId, model.CorrelationId);
                                var SourceData1 = modelCollection.Find(Sourcenamefilter1).Project<DeployModelsDto>(projection1).FirstOrDefault();
                                templateMapping._id = Guid.NewGuid().ToString();
                                templateMapping.ApplicationName = model.LinkedApps;
                                templateMapping.ApplicationID = model.ApplicationID;
                                templateMapping.UsecaseName = model.ModelName;
                                templateMapping.UsecaseID = model.CorrelationId;
                                templateMapping.CreatedByUser = data.userid.ToString();
                                templateMapping.ModifiedByUser = data.userid.ToString();
                                if (SourceData1 != null)
                                {
                                    if (SourceData1.SourceName == "pad" || SourceData1.SourceName == "metric")
                                    {
                                        templateMapping.SourceName = "Pheonix";
                                    }
                                    else if (templateMapping.ApplicationName == CONSTANTS.SPAVelocityApp)
                                    {
                                        templateMapping.SourceName = CONSTANTS.SPAAPP;
                                    }
                                    else
                                    {
                                        templateMapping.SourceName = "Custom";
                                    }
                                }
                                //var deployedModelCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
                                var filterModel = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, model.CorrelationId);
                                var updateBuilder = Builders<BsonDocument>.Update.Set(CONSTANTS.IsPrivate, false)
                                  .Set(CONSTANTS.IsModelTemplate, true).Set(CONSTANTS.ArchivalDays, retentionDuration);
                                deployedModelCollection.UpdateMany(filterModel, updateBuilder);
                                AddNewMapping(templateMapping);
                            }
                        }
                    }
                }

                //Send Notification to VDS for Cascade Models
                //Need to handle for normal cascade and custom cascade.need to see all scenarios.
                //Start
                if (!deployedModel.IsModelTemplate)
                {
                    //Check for the custom cascade
                    if (_isCustomCascadeModel)
                    {
                        //var notificationCollection = _database.GetCollection<AppNotificationLog>("AppNotificationLog");
                        var notifyBuilder = Builders<AppNotificationLog>.Filter;
                        var notificationFilter = notifyBuilder.Eq(CONSTANTS.Correlation, deployedModel.CorrelationId) & notifyBuilder.Eq("IsCascade", true);
                        var notificationResults = notificationCollection.Find(notificationFilter).ToList();
                        if (notificationResults.Count > 0)
                        {
                            if (notificationResults[0].OperationType == OperationTypes.Created.ToString())
                            {
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(DeployModelServices), "DeployModel OperationTypes-" + OperationTypes.Created.ToString(), "NOTIFICATIONRESULTS END", string.Empty, string.Empty, string.Empty, string.Empty);
                                _isCustomCascadeNotificationRequired = false;
                            }
                        }
                    }
                    SendVDSDeployModelNotification(deployedModel.CorrelationId, OperationTypes.Created.ToString(), true);
                }
                //End
            }
            else
            {
                if (datasource.Count > 0)
                {
                    deployModelView.ModelName = datasource[0];
                    if (datasource.Count > 1)
                        deployModelView.DataSource = datasource[1];
                    if (datasource.Count > 2)
                        deployModelView.ModelType = datasource[2];
                    if (datasource.Count > 3)
                    {
                        deployModelView.BusinessProblem = datasource[3];
                    }
                    if (datasource.Count > 5)
                    {
                        deployModelView.Category = datasource[5];
                    }
                }
            }
            var Sourcenamefilter = filterBuilder.Eq(CONSTANTS.CorrelationId, deployedModel.CorrelationId);
            var SourceData = modelCollection.Find(Sourcenamefilter).Project<DeployModelsDto>(projection1).FirstOrDefault();

            if (linkedApps != null && !deployedModel.IsPrivate && deployedModel.IsModelTemplate)
            {
                if (linkedApps[0] != "Ingrain")
                {
                    templateMapping._id = Guid.NewGuid().ToString();
                    templateMapping.ApplicationName = linkedApps[0];
                    templateMapping.ApplicationID = appDetails.ApplicationID;
                    templateMapping.UsecaseName = data.ModelName.ToString();
                    templateMapping.UsecaseID = data.correlationId.ToString();
                    templateMapping.CreatedByUser = data.userid.ToString();
                    templateMapping.ModifiedByUser = data.userid.ToString();
                    if (cascadeResult.Count > 0)
                        templateMapping.IsCascadeModelTemplate = true;
                    string[] applicationNameSPA = new string[] { CONSTANTS.SPAVelocityApp, CONSTANTS.SPAAPP };
                    //  templateMapping.DateColumn = string.Empty;
                    if (SourceData != null)
                    {
                        if (SourceData.SourceName == "pad" || SourceData.SourceName == "metric")
                        {
                            templateMapping.SourceName = "Pheonix";
                        }
                        else if (applicationNameSPA.Contains(templateMapping.ApplicationName))
                        {
                            templateMapping.SourceName = CONSTANTS.SPAAPP;
                        }
                        else if (SourceData.SourceName == "DataSet")
                        {
                            templateMapping.SourceName = "DataSet";
                        }
                        else
                        {
                            templateMapping.SourceName = "Custom";
                        }

                        if (SourceData.SourceName.ToUpper() == CONSTANTS.CustomDbQuery.ToUpper() || SourceData.SourceName.ToUpper() == CONSTANTS.CustomDataApi.ToUpper())
                        {
                            //var requestCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
                            var CDSfilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, deployedModel.CorrelationId) & Builders<BsonDocument>.Filter.Eq("pageInfo", "IngestData");
                            var resultfmRequest = requestCollection.Find(CDSfilter).Project<BsonDocument>(Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id).Include(CONSTANTS.ParamArgs)).FirstOrDefault();
                            if (resultfmRequest.Count() > 0)
                            {
                                var paramArgs = JObject.Parse(resultfmRequest["ParamArgs"].ToString());
                                string CustomSource = CONSTANTS.Null;
                                foreach (var item in paramArgs.Children())
                                {
                                    JProperty jProperty = item as JProperty;
                                    if (jProperty != null && jProperty.Name == "CustomSource")
                                    {
                                        CustomSource = jProperty.Value.ToString();
                                        templateMapping.SourceURL = CustomSource;
                                    }
                                }
                            }
                            templateMapping.SourceName = SourceData.SourceName;
                        }
                    }
                    AddNewMapping(templateMapping);
                }
            }
            //Custom Cascade feature related prediction
            if (modelsData[0].IsCascadingButton)
            {
                InsertCustomRequest(deployedModel);
            }
            //Custom Cascade Models - Last model input Sample adding to custom cascade model.
            if (modelsData.Count > 0)
            {
                UpdateCustomSampleInput(modelsData[0], deployedModel);
            }
            //check if version 0 exists,if exists delete it and update to 1 to 0
            // var Recommended_collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIRecommendedTrainedModels);
            var Recommended_filterBuilder = Builders<BsonDocument>.Filter;
            var filter0 = Recommended_filterBuilder.Eq(CONSTANTS.CorrelationId, deployedModel.CorrelationId) & Recommended_filterBuilder.Eq(CONSTANTS.VersionAttribute, CONSTANTS.NumericZero);
            var filter1 = Recommended_filterBuilder.Eq(CONSTANTS.CorrelationId, deployedModel.CorrelationId) & Recommended_filterBuilder.Eq(CONSTANTS.VersionAttribute, 1);
            var Recommended_result0 = Recommended_collection.Find(filter0).ToList();
            var Recommended_result1 = Recommended_collection.Find(filter1).ToList();
            var updateVersion = Builders<BsonDocument>.Update.Set(CONSTANTS.VersionAttribute, 0);
            if (Recommended_result1.Count > 0 && Recommended_result0.Count > 0)
            {
                Recommended_collection.DeleteMany(filter0);
                Recommended_collection.UpdateMany(filter1, updateVersion);
            }

            //var savemodels_collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_savedModels);
            var savemodels_projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
            var savedModelResult0 = savemodels_collection.Find(filter0).Project<BsonDocument>(savemodels_projection).ToList();
            var savedModelResult1 = savemodels_collection.Find(filter1).Project<BsonDocument>(savemodels_projection).ToList();
            if (savedModelResult0.Count > 0 && savedModelResult1.Count > 0)
            {
                savemodels_collection.DeleteMany(filter0);
                for (int i = 0; i < savedModelResult1.Count; i++)
                {
                    var filter_filePath = Recommended_filterBuilder.Eq(CONSTANTS.CorrelationId, deployedModel.CorrelationId) & Recommended_filterBuilder.Eq(CONSTANTS.VersionAttribute, 1) & Recommended_filterBuilder.Eq("FileName", savedModelResult1[i]["FileName"].ToString());
                    var filter_Version = Recommended_filterBuilder.Eq(CONSTANTS.CorrelationId, deployedModel.CorrelationId) & Recommended_filterBuilder.Eq(CONSTANTS.VersionAttribute, 1) & Recommended_filterBuilder.Eq("FileName", savedModelResult1[i]["FileName"].ToString());

                    string fileName = savedModelResult1[i]["FileName"].ToString();

                    string filePath = savedModelResult1[i][CONSTANTS.FilePath].ToString();
                    string[] paths = filePath.Split(".pickle");
                    string version = paths[0].Substring(paths[0].Length - 2, 2);

                    if (version == "_1")
                    {
                        string oldfilePath = paths[0].Remove(paths[0].Length - 2, 2) + ".pickle";
                        var updatePath = Builders<BsonDocument>.Update.Set(CONSTANTS.FilePath, oldfilePath);
                        savemodels_collection.UpdateOne(filter_filePath, updatePath);
                        savemodels_collection.UpdateOne(filter_Version, updateVersion);
                        if (File.Exists(oldfilePath) && File.Exists(filePath))
                        {
                            File.Delete(oldfilePath);
                            File.Move(filePath, oldfilePath);
                        }
                        //added file name update for 2 version
                        var filter_fileName = Recommended_filterBuilder.Eq(CONSTANTS.CorrelationId, deployedModel.CorrelationId) & Recommended_filterBuilder.Eq(CONSTANTS.VersionAttribute, 0) & Recommended_filterBuilder.Eq(CONSTANTS.FilePath, paths[0].Remove(paths[0].Length - 2, 2) + ".pickle");

                        if (fileName.Substring(fileName.Length - 2, 2).ToString() == "_1")
                        {
                            string[] names = fileName.Split("_1");
                            var updatefileName = Builders<BsonDocument>.Update.Set("FileName", names[0].ToString());
                            savemodels_collection.UpdateOne(filter_fileName, updatefileName);
                        }
                    }
                }
            }
            if (!_isCascadeModel)
                SendVDSDeployModelNotification(deployedModel.CorrelationId, operation, false);
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DeployModelServices), "DeployModel", "END", string.Empty, string.Empty, string.Empty, string.Empty);
            return deployModelView;
        }

        public void AssignFrequency(DeployedModel deployedModel, PublishModelFrequency frequency, dynamic data, string feature)
        {
            string frequencyName = ((JProperty)((JContainer)data).First).Name;
            frequency.RetryCount = data[CONSTANTS.RetryCount] != null ? Convert.ToInt32(data[CONSTANTS.RetryCount]) : 0;
            switch (frequencyName)
            {
                case CONSTANTS.Hourly:
                    frequency.Hourly = data[frequencyName];
                    deployedModel.TrainingFrequencyInDays = feature.Equals(CONSTANTS.Training) ? frequency.Hourly * 60 : deployedModel.TrainingFrequencyInDays;
                    deployedModel.PredictionFrequencyInDays = feature.Equals(CONSTANTS.Prediction) ? frequency.Hourly * 60 : deployedModel.PredictionFrequencyInDays;
                    break;

                case CONSTANTS.Daily:
                    frequency.Daily = data[frequencyName];
                    deployedModel.RetrainingFrequencyInDays = feature.Equals(CONSTANTS.Retraining) ? frequency.Daily : deployedModel.RetrainingFrequencyInDays;
                    deployedModel.TrainingFrequencyInDays = feature.Equals(CONSTANTS.Training) ? frequency.Daily : deployedModel.TrainingFrequencyInDays;
                    deployedModel.PredictionFrequencyInDays = feature.Equals(CONSTANTS.Prediction) ? frequency.Daily : deployedModel.PredictionFrequencyInDays;
                    break;

                case CONSTANTS.Weekly:
                    frequency.Weekly = data[frequencyName];
                    deployedModel.RetrainingFrequencyInDays = feature.Equals(CONSTANTS.Retraining) ? (frequency.Weekly * 7) : deployedModel.RetrainingFrequencyInDays;
                    deployedModel.TrainingFrequencyInDays = feature.Equals(CONSTANTS.Training) ? (frequency.Weekly * 7) : deployedModel.TrainingFrequencyInDays;
                    deployedModel.PredictionFrequencyInDays = feature.Equals(CONSTANTS.Prediction) ? (frequency.Weekly * 7) : deployedModel.PredictionFrequencyInDays;
                    break;

                case CONSTANTS.Monthly:
                    frequency.Monthly = data[frequencyName];
                    deployedModel.RetrainingFrequencyInDays = feature.Equals(CONSTANTS.Retraining) ? (frequency.Monthly * 30) : deployedModel.RetrainingFrequencyInDays;
                    deployedModel.TrainingFrequencyInDays = feature.Equals(CONSTANTS.Training) ? (frequency.Monthly * 30) : deployedModel.TrainingFrequencyInDays;
                    deployedModel.PredictionFrequencyInDays = feature.Equals(CONSTANTS.Prediction) ? (frequency.Monthly * 30) : deployedModel.PredictionFrequencyInDays;
                    break;

                case CONSTANTS.Fortnightly:
                    frequency.Fortnightly = data[frequencyName];
                    deployedModel.RetrainingFrequencyInDays = feature.Equals(CONSTANTS.Retraining) ? (frequency.Fortnightly * 14) : deployedModel.RetrainingFrequencyInDays;
                    deployedModel.TrainingFrequencyInDays = feature.Equals(CONSTANTS.Training) ? (frequency.Fortnightly * 14) : deployedModel.TrainingFrequencyInDays;
                    deployedModel.PredictionFrequencyInDays = feature.Equals(CONSTANTS.Prediction) ? (frequency.Fortnightly * 14) : deployedModel.PredictionFrequencyInDays;
                    break;
            }
        }

        private void InsertCustomRequest(DeployedModel deployedModel)
        {
            bool DBEncryptionRequired = CommonUtility.EncryptDB(deployedModel.CorrelationId, appSettings);
            string userId = deployedModel.UserId;
            if (DBEncryptionRequired)
            {
                if (!string.IsNullOrEmpty(Convert.ToString(deployedModel.UserId)))
                {
                    userId = _encryptionDecryption.Encrypt(Convert.ToString(deployedModel.UserId));
                }
            }
            IngrainRequestQueue ingrainRequest = new IngrainRequestQueue
            {
                _id = Guid.NewGuid().ToString(),
                CorrelationId = deployedModel.CorrelationId,
                RequestId = Guid.NewGuid().ToString(),
                ProcessId = null,
                Status = null,
                ModelName = null,
                RequestStatus = CONSTANTS.New,
                RetryCount = 0,
                ProblemType = null,
                Message = null,
                UniId = null,
                Progress = null,
                pageInfo = CONSTANTS.PredictCascade,
                ParamArgs = CONSTANTS.CurlyBraces,
                Function = CONSTANTS.PredictCascade,
                CreatedByUser = userId,
                CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                ModifiedByUser = userId,
                ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                LastProcessedOn = null,
            };
            var requestQueue = JsonConvert.SerializeObject(ingrainRequest);
            var insertRequestQueue = BsonSerializer.Deserialize<BsonDocument>(requestQueue);
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
            collection.InsertOne(insertRequestQueue);
        }

        private void UpdateCustomSampleInput(DeployModelsDto modelsData, DeployedModel deployedModel)
        {
            if (modelsData.IsCascadingButton & modelsData.Status == CONSTANTS.Deployed & modelsData.IsIncludedInCascade)
            {
                if (modelsData.CustomCascadeId != null & modelsData.CustomCascadeId != CONSTANTS.BsonNull & modelsData.CustomCascadeId != CONSTANTS.Null)
                {
                    var CustomCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAICascadedModels);
                    var customFilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CascadedId, modelsData.CustomCascadeId);
                    var customProjection = Builders<BsonDocument>.Projection.Include(CONSTANTS.Mappings).Include(CONSTANTS.ModelList).Exclude(CONSTANTS.Id);
                    var customResult = CustomCollection.Find(customFilter).Project<BsonDocument>(customProjection).ToList();
                    if (customResult.Count > 0)
                    {
                        JObject modelData = JObject.Parse(customResult[0].ToString());
                        if (modelData != null & !string.IsNullOrEmpty(modelData.ToString()))
                        {
                            int i = 1;
                            foreach (var item in modelData[CONSTANTS.ModelList].Children())
                            {
                                var prop = item as JProperty;
                                if (prop != null)
                                {
                                    var model = JsonConvert.DeserializeObject<DeployModelDetails>(prop.Value.ToString());
                                    if (deployedModel.CorrelationId == model.CorrelationId)
                                    {
                                        //updating the modellist for the current custom cascade model
                                        string name = string.Format("Model{0}", i);
                                        modelData[CONSTANTS.ModelList][name][CONSTANTS.LinkedApps] = modelsData.LinkedApps[0];
                                        modelData[CONSTANTS.ModelList][name][CONSTANTS.ProblemType] = modelsData.ModelType;
                                        modelData[CONSTANTS.ModelList][name][CONSTANTS.Accuracy] = modelsData.Accuracy;
                                        modelData[CONSTANTS.ModelList][name][CONSTANTS.ModelType] = modelsData.ModelVersion;
                                        modelData[CONSTANTS.ModelList][name][CONSTANTS.ApplicationID] = modelsData.AppId;
                                        //Updating cascade modellist
                                        BsonDocument doc = BsonDocument.Parse(modelData[CONSTANTS.ModelList].ToString());
                                        var updatecascade = Builders<BsonDocument>.Update.Set(CONSTANTS.ModelList, doc);
                                        CustomCollection.UpdateOne(customFilter, updatecascade);


                                        //Updating Input Sample to main custom cascade depoy model
                                        string sampleInput = AddCascadeSampleInput(customResult[0]);
                                        string inputSample = !string.IsNullOrEmpty(sampleInput) ? sampleInput.Replace(System.Environment.NewLine, String.Empty).Replace("\r\n", string.Empty) : null;
                                        var customDeployCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);

                                        var customFilter2 = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, modelsData.CustomCascadeId);
                                        bool DBEncryptionRequired = CommonUtility.EncryptDB(modelsData.CustomCascadeId, appSettings);
                                        if (DBEncryptionRequired)
                                        {
                                            var updateCustom = Builders<BsonDocument>.Update.Set(CONSTANTS.InputSample, _encryptionDecryption.Encrypt(inputSample))
                                                                                        .Set(CONSTANTS.ModelVersion, modelsData.ModelVersion)
                                                                                        .Set(CONSTANTS.ModelType, modelsData.ModelType)
                                                                                        .Set(CONSTANTS.Accuracy, modelsData.Accuracy);
                                            customDeployCollection.UpdateOne(customFilter2, updateCustom);
                                        }
                                        else
                                        {
                                            var updateCustom = Builders<BsonDocument>.Update.Set(CONSTANTS.InputSample, inputSample)
                                            .Set(CONSTANTS.ModelVersion, modelsData.ModelVersion)
                                            .Set(CONSTANTS.ModelType, modelsData.ModelType)
                                            .Set(CONSTANTS.Accuracy, modelsData.Accuracy);
                                            customDeployCollection.UpdateOne(customFilter2, updateCustom);
                                        }

                                    }
                                }
                                i++;
                            }
                        }
                    }
                }
            }
        }
        public void SendVDSDeployModelNotification(string correlationId, string operation, bool isCascadeModel)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DeployModelServices), "SendVDSDeployModelNotification OPERATION-" + operation + "--ISCASCADEMODEL--" + isCascadeModel, "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            IMongoCollection<DeployModelsDto> deployModel;
            IMongoCollection<BsonDocument> cascadeCollection;
            IMongoCollection<BusinessProblem> collection;
            if (servicename == "Anomaly")
            {
                deployModel = _databaseAD.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
                cascadeCollection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.SSAICascadedModels);
                collection = _databaseAD.GetCollection<BusinessProblem>(CONSTANTS.PSBusinessProblem);
            }
            else
            {
                deployModel = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
                cascadeCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAICascadedModels);
                collection = _database.GetCollection<BusinessProblem>(CONSTANTS.PSBusinessProblem);
            }
            //var deployModel = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
            var filter = Builders<DeployModelsDto>.Filter.Where(x => x.CorrelationId == correlationId);
            var projection = Builders<DeployModelsDto>.Projection.Exclude("_id");
            var model = deployModel.Find(filter).Project<DeployModelsDto>(projection).FirstOrDefault();
            List<string> sourceLst = new List<string>() { "Custom", "pad", "metric", "Cascading", "multidatasource" };
            List<string> appLst = new List<string>() { CONSTANTS.VDSApplicationID_PAD, CONSTANTS.VDSApplicationID_FDS, CONSTANTS.VDSApplicationID_PAM };
            List<string> excludedEntities = new List<string> { "Milestone", "Deliverable", "Risk", "Issue" };
            //List<string> categories = new List<string> { "AD", "Agile", "PPM" };
            List<string> categories = new List<string> { "AD", "Agile" };//As per UserStory - 1623286 for AWF

            CascadeModelsCollection models = new CascadeModelsCollection();
            //var cascadeCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAICascadedModels);
            var cascadeFilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CascadedId, correlationId);
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
            //var collection = _database.GetCollection<BusinessProblem>(CONSTANTS.PSBusinessProblem);
            var filter2 = Builders<BusinessProblem>.Filter.Eq(CONSTANTS.CorrelationId, models.CorrelationId);
            var projection2 = Builders<BusinessProblem>.Projection.Include(CONSTANTS.BusinessProblems).Exclude(CONSTANTS.Id);
            var result = collection.Find(filter2).Project<BusinessProblem>(projection2).FirstOrDefault();
            bool isValidSource = sourceLst.Contains(model.SourceName);
            bool validEntity = true;

            if (isCascadeModel && cascadeResult.Count > 0)
            {
                var projection1 = Builders<DeployModelsDto>.Projection.Exclude("_id");
                var filter1 = Builders<DeployModelsDto>.Filter.Where(x => x.CorrelationId == models.CorrelationId);
                var cascadeModelResult = deployModel.Find(filter1).Project<DeployModelsDto>(projection1).FirstOrDefault();
                if (_isCustomCascadeModel)
                {
                    if (cascadeModelResult.Status == CONSTANTS.Deployed)
                    {
                        if (cascadeModelResult != null)
                        {
                            if (categories.Contains(cascadeModelResult.Category))
                                validEntity = !excludedEntities.Contains(cascadeModelResult.DataSource);
                        }
                    }
                    else
                    {
                        JObject data = JObject.Parse(cascadeResult[0].ToString());
                        CascadeModelsCollection custommodels = null;
                        if (data != null)
                        {
                            string model2 = string.Format("Model{0}", data[CONSTANTS.ModelList].Children().Count() - 1);
                            custommodels = JsonConvert.DeserializeObject<CascadeModelsCollection>(data[CONSTANTS.ModelList][model2].ToString());
                        }
                        var previousModelFilter = Builders<DeployModelsDto>.Filter.Where(x => x.CorrelationId == custommodels.CorrelationId);
                        var previousCustomModelResult = deployModel.Find(previousModelFilter).Project<DeployModelsDto>(projection1).FirstOrDefault();
                        if (previousCustomModelResult != null)
                        {
                            if (categories.Contains(previousCustomModelResult.Category))
                                validEntity = !excludedEntities.Contains(previousCustomModelResult.DataSource);
                        }
                    }
                }
                else
                {
                    if (cascadeModelResult != null)
                    {
                        if (categories.Contains(cascadeModelResult.Category))
                            validEntity = !excludedEntities.Contains(cascadeModelResult.DataSource);
                    }
                }
            }
            else
            {
                if (categories.Contains(model.Category))
                    validEntity = !excludedEntities.Contains(model.DataSource);
            }

            if (model.SourceName == "multidatasource")
            {
                IMongoCollection<BsonDocument> collection3;
                if (servicename == "Anomaly")
                    collection3 = _databaseAD.GetCollection<BsonDocument>("PS_MultiFileColumn");
                else
                    collection3 = _database.GetCollection<BsonDocument>("PS_MultiFileColumn");
                //var collection3 = _database.GetCollection<BsonDocument>("PS_MultiFileColumn");
                var builder3 = Builders<BsonDocument>.Projection.Include("CorrelationId").Include("File").Include("Flag").Exclude("_id");
                var columnFilter3 = Builders<BsonDocument>.Filter.Eq("CorrelationId", correlationId);
                var result3 = collection3.Find(columnFilter3).Project<BsonDocument>(builder3).FirstOrDefault();
                if (result3 != null)
                {
                    bool entityFlag = true;
                    try
                    {
                        JObject res = JObject.Parse(result3.ToJson());
                        JObject file = res["File"] as JObject;
                        foreach (var obj in file)
                        {
                            JObject subObj = obj.Value as JObject;
                            if (subObj["FileExtensionOrig"].ToString() != "Entity")
                            {
                                entityFlag = false;
                            }

                        }
                    }
                    catch (Exception ex)
                    {
                        entityFlag = false;
                        LOGGING.LogManager.Logger.LogErrorMessage(typeof(DeployModelServices), nameof(SendVDSDeployModelNotification), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                    }

                    if (!entityFlag)
                        isValidSource = false;



                }
            }
            if (appLst.Contains(model.AppId) && isValidSource && validEntity)
            {
                AppNotificationLog appNotificationLog = new AppNotificationLog();
                if (!model.IsModelTemplate)
                {
                    appNotificationLog.ClientUId = model.ClientUId;
                    appNotificationLog.DeliveryConstructUId = model.DeliveryConstructUID;
                }

                appNotificationLog.CorrelationId = model.CorrelationId;
                appNotificationLog.CreatedDateTime = model.CreatedOn;
                appNotificationLog.Entity = string.Empty;


                appNotificationLog.UseCaseName = model.ModelName;
                appNotificationLog.UserId = model.CreatedByUser;
                if (result != null)
                    appNotificationLog.UseCaseDescription = result.BusinessProblems;
                appNotificationLog.UseCaseId = model.CorrelationId;// for only vds charts
                if (isCascadeModel)
                {
                    appNotificationLog.FunctionalArea = model.Category;
                    appNotificationLog.ProblemType = "Cascade";
                    appNotificationLog.IsCascade = true;
                    if (model.Category == "PPM")
                        appNotificationLog.FunctionalArea = "RIAD";
                    if (model.Category == "AD")
                        appNotificationLog.FunctionalArea = "ADWaterfall";
                    if (model.Category == "Devops")
                        appNotificationLog.FunctionalArea = "DevOps";
                    if (model.Category == "Others")
                        appNotificationLog.FunctionalArea = "General";
                }
                else
                {
                    appNotificationLog.ProblemType = model.ModelType;
                    appNotificationLog.FunctionalArea = model.Category;
                    if (model.Category == "Others")
                        appNotificationLog.FunctionalArea = "General";
                    if (model.Category == "PPM")
                        appNotificationLog.FunctionalArea = "RIAD";
                }
                if (model.IsModelTemplate)
                    appNotificationLog.ModelType = "ModelTemplate";
                else if (model.IsPrivate)
                    appNotificationLog.ModelType = "Private";
                else
                    appNotificationLog.ModelType = "Public";
                appNotificationLog.ApplicationId = model.AppId;
                appNotificationLog.OperationType = operation;

                appNotificationLog.NotificationEventType = "DeployModel";
                if (_isCustomCascadeModel)
                {
                    if (_isCustomCascadeNotificationRequired)
                        appNotificationLog.OperationType = OperationTypes.Created.ToString();
                    else
                        appNotificationLog.OperationType = OperationTypes.Updated.ToString();
                }
                else
                {
                    if (model.IsUpdated == "True")
                    {
                        appNotificationLog.OperationType = "Updated";
                    }
                }
                CommonUtility.SendAppNotification(appNotificationLog, isCascadeModel, servicename);
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(DeployModelServices), "SendVDSDeployModelNotification", "END", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            }
        }
        public string AddNewMapping(PublicTemplateMapping templateMapping)
        {
            bool DBEncryptionRequired = CommonUtility.EncryptDB(templateMapping.UsecaseID, appSettings, servicename);
            if (DBEncryptionRequired)
            {
                if (!string.IsNullOrEmpty(Convert.ToString(templateMapping.CreatedByUser)))
                {
                    templateMapping.CreatedByUser = _encryptionDecryption.Encrypt(Convert.ToString(templateMapping.CreatedByUser));
                }
                if (!string.IsNullOrEmpty(Convert.ToString(templateMapping.ModifiedByUser)))
                {
                    templateMapping.ModifiedByUser = _encryptionDecryption.Encrypt(Convert.ToString(templateMapping.ModifiedByUser));
                }
            }
            templateMapping.CreatedOn = DateTime.UtcNow.ToString();
            templateMapping.ModifiedOn = DateTime.UtcNow.ToString();
            IMongoCollection<BsonDocument> collection;
            if (servicename == "Anomaly")
                collection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.PublicTemplateMapping);
            else
                collection = _database.GetCollection<BsonDocument>(CONSTANTS.PublicTemplateMapping);
            //var collection = _database.GetCollection<BsonDocument>(CONSTANTS.PublicTemplateMapping);
            var filter = Builders<BsonDocument>.Filter.Eq("ApplicationID", templateMapping.ApplicationID) & Builders<BsonDocument>.Filter.Eq("UsecaseID", templateMapping.UsecaseID);
            var result = collection.Find(filter).ToList();
            var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(templateMapping);
            var insertDocument = BsonSerializer.Deserialize<BsonDocument>(jsonData);
            if (result.Count > 0)
            {
                collection.DeleteOne(filter);
            }
            collection.InsertOneAsync(insertDocument);
            return "Success";
        }

        public dynamic GetAppName(string ApplicationName)
        {
            //To get the Application ID
            dynamic AppDetails = null;
            IMongoCollection<BsonDocument> appCollection;
            if (servicename == "Anomaly")
                appCollection = _databaseAD.GetCollection<BsonDocument>("AppIntegration");
            else
                appCollection = _database.GetCollection<BsonDocument>("AppIntegration");
            //var appCollection = _database.GetCollection<BsonDocument>("AppIntegration");
            var filterBuilder = Builders<BsonDocument>.Filter;
            var filter = filterBuilder.Eq("ApplicationName", ApplicationName);
            var Projection = Builders<BsonDocument>.Projection.Include("ApplicationID").Include("ApplicationName").Include("BaseURL").Exclude("_id");
            var ApplicationResult = appCollection.Find(filter).Project<BsonDocument>(Projection).FirstOrDefault();

            if (ApplicationResult != null)
            {
                AppDetails = JsonConvert.DeserializeObject<dynamic>(ApplicationResult.ToJson());
            }
            return AppDetails;
        }

        private void AssignValues(DeployedModel deployedModel, string modelType, int? count, dynamic postData)
        {
            IMongoCollection<BsonDocument> deployedModelCollection;
            if (servicename == "Anomaly")
                deployedModelCollection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
            else
                deployedModelCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
            //var deployedModelCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
            var filterBuilder = Builders<BsonDocument>.Filter;
            var filter = filterBuilder.Eq(CONSTANTS.CorrelationId, deployedModel.CorrelationId);
            //string isUpdated = "False";
            string isUpdated = null;

            var mdl = deployedModelCollection.Find(filter).ToList();
            if (mdl.Count > 0)
            {
                if (mdl[0]["Status"] == "Deployed")
                {
                    isUpdated = "True";
                }
                else
                {
                    if (!mdl[0]["IsUpdated"].IsBsonNull)
                    {
                        isUpdated = Convert.ToString(mdl[0]["IsUpdated"]);
                    }
                }
                if (mdl[0]["IsUpdated"] == "True")
                {
                    isUpdated = "True";
                }
            }
            var frequencyFilter = filterBuilder.Eq(CONSTANTS.CorrelationId, deployedModel.CorrelationId) & filterBuilder.Eq(CONSTANTS.Frequency, deployedModel.Frequency);
            if (modelType == CONSTANTS.TimeSeries)
            {
                var filterRecord = deployedModelCollection.Find(filter).ToList();
                var frequencyExist = deployedModelCollection.Find(frequencyFilter).ToList();
                //TIMESERIES - PRIVATE MODEL
                if (Convert.ToBoolean(deployedModel.IsPrivate))
                {
                    if ((count == 0 && filterRecord.Count == 1) || frequencyExist.Count > 0) //Update
                    {
                        if (servicename == "Anomaly")
                        {
                            var update1 = Builders<BsonDocument>.Update.Set("Threshold", deployedModel.Threshold);
                            deployedModelCollection.UpdateMany(filter, update1);
                        }
                        var builder = Builders<BsonDocument>.Update;
                        var update = this.UpdateModels(builder, deployedModel)
                           .Set(CONSTANTS.IsPrivate, true).Set(CONSTANTS.IsModelTemplate, false)
                           .Set(CONSTANTS.Frequency, deployedModel.Frequency)
                           .Set(CONSTANTS.ArchivalDays, deployedModel.ArchivalDays)
                           .Set("IsUpdated", isUpdated).Set(CONSTANTS.DeployedDate, DateTime.Now.ToString(CONSTANTS.DateHoursFormat))
                          .Set(CONSTANTS.TrainedModelId, deployedModel.TrainedModelId).Set(CONSTANTS.MaxDataPull, deployedModel.MaxDataPull);
                        var result = frequencyExist.Count > 0 ? deployedModelCollection.UpdateMany(frequencyFilter, update) :
                               deployedModelCollection.UpdateMany(filter, update);
                    }
                    else //Insert
                    {
                        var projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
                        var data = deployedModelCollection.Find(filter).Project<BsonDocument>(projection).FirstOrDefault();
                        data[CONSTANTS.IsPrivate] = true;
                        data[CONSTANTS.IsModelTemplate] = false;
                        data[CONSTANTS.Frequency] = deployedModel.Frequency;
                        data[CONSTANTS.TrainedModelId] = deployedModel.TrainedModelId;
                        data["IsUpdated"] = isUpdated;
                        data[CONSTANTS.ArchivalDays] = deployedModel.ArchivalDays;
                        data[CONSTANTS.DeployedDate] = deployedModel.DeployedDate;
                        if (servicename == "Anomaly")
                        {
                            data["Threshold"] = deployedModel.Threshold;
                        }
                        this.InsertModels(data, deployedModel);
                        var insertDocument = BsonSerializer.Deserialize<BsonDocument>(data);
                        deployedModelCollection.InsertOne(insertDocument);
                    }
                }
                //TIME SERIES - MODEL TEMPLATE
                else if (Convert.ToBoolean(deployedModel.IsModelTemplate))
                {
                    if (postData.Category.ToString() != null)
                    {
                        deployedModel.Category = postData.Category.ToString();
                    }
                    if ((count == 0 && filterRecord.Count == 1) || frequencyExist.Count > 0) //Update
                    {
                        var builder = Builders<BsonDocument>.Update;
                        var update = this.UpdateModels(builder, deployedModel)
                          .Set(CONSTANTS.IsPrivate, false)
                          .Set(CONSTANTS.IsModelTemplate, true)
                          .Set(CONSTANTS.Frequency, deployedModel.Frequency)
                          .Set(CONSTANTS.ArchivalDays, deployedModel.ArchivalDays)
                          .Set("IsUpdated", isUpdated).Set(CONSTANTS.DeployedDate, DateTime.Now.ToString(CONSTANTS.DateHoursFormat))
                          .Set(CONSTANTS.TrainedModelId, deployedModel.TrainedModelId).Set(CONSTANTS.MaxDataPull, deployedModel.MaxDataPull);
                        var result = frequencyExist.Count > 0 ? deployedModelCollection.UpdateMany(frequencyFilter, update) :
                               deployedModelCollection.UpdateMany(filter, update);
                    }
                    else//insert
                    {
                        var projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
                        var data = deployedModelCollection.Find(filter).Project<BsonDocument>(projection).FirstOrDefault();
                        data[CONSTANTS.IsPrivate] = false;
                        data[CONSTANTS.IsModelTemplate] = true;
                        data[CONSTANTS.Frequency] = deployedModel.Frequency;
                        data["IsUpdated"] = isUpdated;
                        data[CONSTANTS.TrainedModelId] = deployedModel.TrainedModelId;
                        data[CONSTANTS.IsCascadeModelTemplate] = _isCascadeModel;
                        data[CONSTANTS.ArchivalDays] = deployedModel.ArchivalDays;
                        data[CONSTANTS.DeployedDate] = deployedModel.DeployedDate;
                        this.InsertModels(data, deployedModel);
                        var insertDocument = BsonSerializer.Deserialize<BsonDocument>(data);
                        deployedModelCollection.InsertOne(insertDocument);
                    }

                    //Public Templates
                    this.UpdatePublicTemplate(filter, deployedModel, servicename);
                }
                //PUBLIC MODEL
                else
                {
                    if ((count == 0 && filterRecord.Count == 1) || frequencyExist.Count > 0) //Update
                    {
                        var builder = Builders<BsonDocument>.Update;
                        var update = this.UpdateModels(builder, deployedModel)
                           .Set(CONSTANTS.IsPrivate, false)
                           .Set(CONSTANTS.IsModelTemplate, false)
                           .Set(CONSTANTS.Frequency, deployedModel.Frequency)
                           .Set(CONSTANTS.ArchivalDays, deployedModel.ArchivalDays)
                           .Set("IsUpdated", isUpdated).Set(CONSTANTS.DeployedDate, DateTime.Now.ToString(CONSTANTS.DateHoursFormat))
                           .Set(CONSTANTS.TrainedModelId, deployedModel.TrainedModelId).Set(CONSTANTS.MaxDataPull, deployedModel.MaxDataPull);
                        var result = frequencyExist.Count > 0 ? deployedModelCollection.UpdateMany(frequencyFilter, update) :
                               deployedModelCollection.UpdateMany(filter, update);
                    }
                    else //Insert
                    {
                        var projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
                        var data = deployedModelCollection.Find(filter).Project<BsonDocument>(projection).FirstOrDefault();
                        data[CONSTANTS.IsPrivate] = false;
                        data[CONSTANTS.IsModelTemplate] = false;
                        data[CONSTANTS.Frequency] = deployedModel.Frequency;
                        data["IsUpdated"] = isUpdated;
                        data[CONSTANTS.TrainedModelId] = deployedModel.TrainedModelId;
                        data[CONSTANTS.IsCascadeModelTemplate] = _isCascadeModel;
                        data[CONSTANTS.ArchivalDays] = deployedModel.ArchivalDays;
                        data[CONSTANTS.DeployedDate] = deployedModel.DeployedDate;
                        this.InsertModels(data, deployedModel);
                        var insertDocument = BsonSerializer.Deserialize<BsonDocument>(data);
                        deployedModelCollection.InsertOne(insertDocument);
                    }
                }
            }
            //CLASSIFICATION (or) REGRESSION (or) MULTI-CLASS
            else
            {
                //PRIVATE MODEL
                if (Convert.ToBoolean(deployedModel.IsPrivate))
                {
                    if (servicename == "Anomaly")
                    {
                        var update1 = Builders<BsonDocument>.Update.Set("Threshold", deployedModel.Threshold);
                        deployedModelCollection.UpdateMany(filter, update1);
                    }
                    var builder = Builders<BsonDocument>.Update;
                    var update = this.UpdateModels(builder, deployedModel)
                     .Set(CONSTANTS.IsPrivate, true)
                     .Set(CONSTANTS.ArchivalDays, deployedModel.ArchivalDays)
                     .Set("IsUpdated", isUpdated).Set(CONSTANTS.DeployedDate, DateTime.Now.ToString(CONSTANTS.DateHoursFormat))
                     .Set(CONSTANTS.IsModelTemplate, false).Set(CONSTANTS.MaxDataPull, deployedModel.MaxDataPull);
                    deployedModelCollection.UpdateMany(filter, update);
                }
                //MODEL TEMPLATE
                else if (Convert.ToBoolean(deployedModel.IsModelTemplate))
                {
                    if (postData.Category.ToString() != null)
                    {
                        deployedModel.Category = postData.Category.ToString();
                    }
                    var builder = Builders<BsonDocument>.Update;
                    var update = this.UpdateModels(builder, deployedModel).Set(CONSTANTS.IsPrivate, false).Set(CONSTANTS.IsModelTemplate, true).Set(CONSTANTS.ModelType, deployedModel.ModelType).Set("IsUpdated", isUpdated).Set(CONSTANTS.MaxDataPull, deployedModel.MaxDataPull).Set(CONSTANTS.ArchivalDays, deployedModel.ArchivalDays).Set(CONSTANTS.DeployedDate, DateTime.Now.ToString(CONSTANTS.DateHoursFormat));
                    deployedModelCollection.UpdateMany(filter, update);

                    //Public Templates
                    this.UpdatePublicTemplate(filter, deployedModel, servicename);
                }
                //PUBLIC MODEL
                else
                {
                    var builder = Builders<BsonDocument>.Update;
                    var update = this.UpdateModels(builder, deployedModel).Set(CONSTANTS.IsPrivate, false).Set(CONSTANTS.IsModelTemplate, false).Set("IsUpdated", isUpdated).Set(CONSTANTS.MaxDataPull, deployedModel.MaxDataPull).Set(CONSTANTS.ArchivalDays, deployedModel.ArchivalDays).Set(CONSTANTS.DeployedDate, DateTime.Now.ToString(CONSTANTS.DateHoursFormat));
                    deployedModelCollection.UpdateMany(filter, update);
                }
            }
        }

        private string AddCascadeSampleInput(BsonDocument result)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DeployModelServices), "AddCascadeSampleInput", "START", string.Empty, string.Empty, string.Empty, string.Empty);
            string sampleInput = string.Empty;
            List<JObject> allModels = new List<JObject>();
            JObject mapping = new JObject();
            JArray listArray = new JArray();
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("[");
            JObject singleObject = new JObject();
            if (result != null)
            {
                JObject data = JObject.Parse(result.ToString());
                mapping = JObject.Parse(result[CONSTANTS.Mappings].ToString());
                if (data != null)
                {
                    List<string> corids = new List<string>();
                    foreach (var item in data[CONSTANTS.ModelList].Children())
                    {
                        JProperty prop = item as JProperty;
                        if (prop != null)
                        {
                            IMongoCollection<BsonDocument> collection2;
                            if (servicename == "Anomaly")
                                collection2 = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.SSAI_DeployedModels);
                            else
                                collection2 = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_DeployedModels);
                            DeployModelDetails model = JsonConvert.DeserializeObject<DeployModelDetails>(prop.Value.ToString());
                            //var collection2 = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_DeployedModels);
                            var filter2 = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, model.CorrelationId);
                            var result2 = collection2.Find(filter2).Project<BsonDocument>(Builders<BsonDocument>.Projection.Include("InputSample").Exclude("_id")).ToList();
                            if (result2.Count > 0)
                            {
                                allModels.Add(JObject.Parse(result2[0].ToString()));
                                corids.Add(model.CorrelationId);
                            }
                        }
                    }
                    if (allModels.Count > 0)
                    {
                        JArray firstModel = new JArray();
                        if (CommonUtility.EncryptDB(corids[0], appSettings, servicename))
                        {
                            firstModel = JArray.Parse(_encryptionDecryption.Decrypt(allModels[0]["InputSample"].ToString()));
                        }
                        else
                        {
                            firstModel = JArray.Parse(allModels[0]["InputSample"].ToString());
                        }
                        //JArray firstModel = JArray.Parse(allModels[0]["InputSample"].ToString());
                        for (int i = 0; i < firstModel.Count; i++) // main array loop
                        {
                            List<JObject> listJobject = new List<JObject>();
                            for (int j = 0; j < mapping.Count; j++)
                            {
                                if (j == 0)
                                {
                                    string modelName = string.Format("Model{0}", j + 1);
                                    JArray removeAraay1 = new JArray();
                                    if (CommonUtility.EncryptDB(corids[j], appSettings, servicename))
                                    {
                                        removeAraay1 = JArray.Parse(_encryptionDecryption.Decrypt(allModels[j]["InputSample"].ToString()));
                                    }
                                    else
                                    {
                                        removeAraay1 = JArray.Parse(allModels[j]["InputSample"].ToString());
                                    }
                                    //JArray removeAraay1 = JArray.Parse(allModels[j]["InputSample"].ToString());
                                    JObject obj1 = JObject.Parse(removeAraay1[i].ToString());
                                    listJobject.Add(obj1);
                                    //Model 1 Start                                        
                                    JArray removeArray = new JArray();
                                    if (i > 0)
                                    {
                                        if (CommonUtility.EncryptDB(corids[i], appSettings, servicename))
                                        {
                                            removeArray = JArray.Parse(_encryptionDecryption.Decrypt(allModels[i]["InputSample"].ToString()));
                                        }
                                        else
                                        {
                                            removeArray = JArray.Parse(allModels[i]["InputSample"].ToString());
                                        }
                                        //removeAraay = JArray.Parse(allModels[i]["InputSample"].ToString());
                                    }
                                    else
                                    {
                                        if (CommonUtility.EncryptDB(corids[i + 1], appSettings, servicename))
                                        {
                                            removeArray = JArray.Parse(_encryptionDecryption.Decrypt(allModels[i + 1]["InputSample"].ToString()));
                                        }
                                        else
                                        {
                                            removeArray = JArray.Parse(allModels[i + 1]["InputSample"].ToString());
                                        }
                                        //removeAraay = JArray.Parse(allModels[i + 1]["InputSample"].ToString());
                                    }

                                    JObject obj2 = JObject.Parse(removeArray[i].ToString());
                                    string probaElement = string.Empty;
                                    foreach (var prob in obj2.Children())
                                    {
                                        JProperty prop = prob as JProperty;
                                        if (prop != null)
                                        {
                                            if (prop.Name.Contains("_Proba1"))
                                            {
                                                probaElement = prop.Name;
                                                break;
                                            }
                                        }
                                    }
                                    MappingAttributes mapping1b = JsonConvert.DeserializeObject<MappingAttributes>(mapping[modelName]["UniqueMapping"].ToString());
                                    MappingAttributes mapping12 = JsonConvert.DeserializeObject<MappingAttributes>(mapping[modelName]["TargetMapping"].ToString());
                                    if (!string.IsNullOrEmpty(probaElement))
                                        obj2.Property(probaElement).Remove();
                                    obj2.Property(mapping1b.Target).Remove();
                                    obj2.Property(mapping12.Target).Remove();
                                    listJobject.Add(obj2);
                                }
                                else
                                {
                                    //ID Mapping
                                    JArray removeArray = new JArray();
                                    if (CommonUtility.EncryptDB(corids[j + 1], appSettings, servicename))
                                    {
                                        removeArray = JArray.Parse(_encryptionDecryption.Decrypt(allModels[j + 1]["InputSample"].ToString()));
                                    }
                                    else
                                    {
                                        removeArray = JArray.Parse(allModels[j + 1]["InputSample"].ToString());
                                    }
                                    //JArray removeAraay = JArray.Parse(allModels[j + 1]["InputSample"].ToString());
                                    JObject modelIncrement = JObject.Parse(removeArray[i].ToString());
                                    string probaElement = string.Empty;
                                    foreach (var prob in modelIncrement.Children())
                                    {
                                        JProperty prop = prob as JProperty;
                                        if (prop != null)
                                        {
                                            if (prop.Name.Contains("_Proba1"))
                                            {
                                                probaElement = prop.Name;
                                                break;
                                            }
                                        }
                                    }
                                    string model = string.Format("Model{0}", j + 1);
                                    MappingAttributes mapping1 = JsonConvert.DeserializeObject<MappingAttributes>(mapping[model]["UniqueMapping"].ToString());
                                    //TargetMapping                                      
                                    MappingAttributes mapping12 = JsonConvert.DeserializeObject<MappingAttributes>(mapping[model]["TargetMapping"].ToString());
                                    if (!string.IsNullOrEmpty(probaElement))
                                        modelIncrement.Property(probaElement).Remove();
                                    modelIncrement.Property(mapping1.Target).Remove();
                                    modelIncrement.Property(mapping12.Target).Remove();
                                    listJobject.Add(modelIncrement);
                                }
                            }
                            singleObject = new JObject();
                            JArray mainArray = new JArray();
                            foreach (JObject item in listJobject)
                            {
                                singleObject.Merge(item, new JsonMergeSettings
                                {
                                    // union array values together to avoid duplicates
                                    MergeArrayHandling = MergeArrayHandling.Union
                                });
                            }
                            mainArray.Add(singleObject);
                            listArray.Add(mainArray);
                            stringBuilder.Append(singleObject + ",");
                        }
                        stringBuilder.Length -= 1;
                        stringBuilder.Append("]");
                    }
                    sampleInput = stringBuilder.ToString();
                }
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DeployModelServices), "AddCascadeSampleInput", "END", string.Empty, string.Empty, string.Empty, string.Empty);
            return sampleInput;
        }

        public UpdateDefinition<BsonDocument> UpdateModels(UpdateDefinitionBuilder<BsonDocument> builder, DeployedModel deployedModel)
        {
            return builder.Set(CONSTANTS.Accuracy, deployedModel.Accuracy)
                            .Set(CONSTANTS.ModelURL, deployedModel.Url)
                            .Set(CONSTANTS.VDSLink, deployedModel.VdsLink)
                            .Set(CONSTANTS.LinkedApps, deployedModel.App)
                            .Set(CONSTANTS.Status, deployedModel.Status)
                            .Set("AppId", deployedModel.AppId)
                            .Set(CONSTANTS.WebServices, deployedModel.WebServices)
                            .Set(CONSTANTS.DeployedDate, deployedModel.DeployedDate)
                            .Set(CONSTANTS.ModelVersion, deployedModel.ModelVersion)
                            .Set(CONSTANTS.ModifiedOn, DateTime.Now.ToString(CONSTANTS.DateHoursFormat))
                            .Set(CONSTANTS.IsCarryOutRetraining, deployedModel.IsCarryOutRetraining)
                            .Set(CONSTANTS.IsOnline, deployedModel.IsOnline)
                            .Set(CONSTANTS.IsOffline, deployedModel.IsOffline)
                            .Set(CONSTANTS.Retraining, deployedModel.Retraining)
                            .Set(CONSTANTS.Training, deployedModel.Training)
                            .Set(CONSTANTS.Prediction, deployedModel.Prediction)
                            .Set(CONSTANTS.RetrainingFrequencyInDays, deployedModel.RetrainingFrequencyInDays)
                            .Set(CONSTANTS.TrainingFrequencyInDays, deployedModel.TrainingFrequencyInDays)
                            .Set(CONSTANTS.PredictionFrequencyInDays, deployedModel.PredictionFrequencyInDays)
                            .Set(CONSTANTS.ArchivalDays, deployedModel.ArchivalDays);
        }

        public BsonDocument InsertModels(BsonDocument data, DeployedModel deployedModel)
        {
            data[CONSTANTS.Id] = Guid.NewGuid().ToString();
            data[CONSTANTS.Accuracy] = deployedModel.Accuracy;
            data[CONSTANTS.ModelURL] = deployedModel.Url;
            if (deployedModel.VdsLink != null)
            {
                data[CONSTANTS.VDSLink] = deployedModel.VdsLink;
            }
            else
            {
                data[CONSTANTS.VDSLink] = BsonNull.Value;
            }
            data[CONSTANTS.LinkedApps] = BsonValue.Create(deployedModel.App);
            data[CONSTANTS.Frequency] = deployedModel.Frequency;
            data[CONSTANTS.Status] = deployedModel.Status;
            data["AppId"] = deployedModel.AppId;
            data[CONSTANTS.WebServices] = deployedModel.WebServices;
            data[CONSTANTS.DeployedDate] = deployedModel.DeployedDate;
            data[CONSTANTS.ModelVersion] = deployedModel.ModelVersion;
            data[CONSTANTS.TrainedModelId] = deployedModel.TrainedModelId;
            data[CONSTANTS.MaxDataPull] = deployedModel.MaxDataPull;
            data[CONSTANTS.IsCarryOutRetraining] = deployedModel.IsCarryOutRetraining;
            data[CONSTANTS.IsOnline] = deployedModel.IsOnline;
            data[CONSTANTS.IsOffline] = deployedModel.IsOffline;
            data[CONSTANTS.Retraining] = BsonValue.Create(deployedModel.Retraining.ToBsonDocument());
            data[CONSTANTS.Training] = BsonValue.Create(deployedModel.Training.ToBsonDocument());
            data[CONSTANTS.Prediction] = BsonValue.Create(deployedModel.Prediction.ToBsonDocument());
            data[CONSTANTS.RetrainingFrequencyInDays] = deployedModel.RetrainingFrequencyInDays;
            data[CONSTANTS.TrainingFrequencyInDays] = deployedModel.TrainingFrequencyInDays;
            data[CONSTANTS.PredictionFrequencyInDays] = deployedModel.PredictionFrequencyInDays;
            data[CONSTANTS.ArchivalDays] = deployedModel.ArchivalDays;
            return data;
        }

        private void UpdatePublicTemplate(FilterDefinition<BsonDocument> filter, DeployedModel deployedModel, string ServiceName = "")
        {
            servicename = ServiceName;
            IMongoCollection<BsonDocument> templateCollection;
            if (servicename == "Anomaly")
                templateCollection = _databaseAD.GetCollection<BsonDocument>(CONSTANTS.SSAIPublicTemplates);
            else
                templateCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIPublicTemplates);
            //var templateCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIPublicTemplates);
            var templateResult = templateCollection.Find(filter).ToList();
            if (templateResult.Count > 0)
            {
                var templateUpdate = Builders<BsonDocument>.Update.Set(CONSTANTS.ModelName1, deployedModel.Category);
                var updateResult = templateCollection.UpdateOne(filter, templateUpdate);
            }
            else
            {
                string[] categories = new string[] { CONSTANTS.Application_Development, CONSTANTS.AgileDelivery, CONSTANTS.Devops };
                string[] modelNames = new string[] { deployedModel.ModelName, deployedModel.Category };
                bool DBEncryptionRequired = CommonUtility.EncryptDB(deployedModel.CorrelationId, appSettings, servicename);
                string encryptedUser = deployedModel.UserId;
                if (DBEncryptionRequired)
                {
                    if (!string.IsNullOrEmpty(Convert.ToString(encryptedUser)))
                        encryptedUser = _encryptionDecryption.Encrypt(encryptedUser);
                }
                publicTemplateDTO publicTemplate = new publicTemplateDTO
                {
                    _id = Guid.NewGuid().ToString(),
                    CorrelationId = deployedModel.CorrelationId,
                    CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                    ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                    CreatedByUser = encryptedUser,
                    ModifiedByUser = encryptedUser,
                    Category = categories,
                    IsCascadeModelTemplate = _isCascadeModel,
                    ModelName = modelNames,
                    ArchivalDays = deployedModel.ArchivalDays
                };
                var jsonData = JsonConvert.SerializeObject(publicTemplate);
                var insertDocument = BsonSerializer.Deserialize<BsonDocument>(jsonData);
                templateCollection.InsertOne(insertDocument);
            }
        }

        public DeployModelViewModel GetDeployModel(string correlationId, string ServiceName = "")
        {
            servicename = ServiceName;
            DeployModelViewModel deployModelView = new DeployModelViewModel();
            List<DeployModelsDto> modelsDto = new List<DeployModelsDto>();
            IMongoCollection<DeployModelsDto> modelCollection;
            if (servicename == "Anomaly")
                modelCollection = _databaseAD.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
            else
                modelCollection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
            //var modelCollection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
            var projection1 = Builders<DeployModelsDto>.Projection.Exclude(CONSTANTS.Id);
            var filterBuilder = Builders<DeployModelsDto>.Filter;
            var filter2 = filterBuilder.Eq(CONSTANTS.CorrelationId, correlationId) & filterBuilder.Eq(CONSTANTS.Status, CONSTANTS.Deployed);
            var modelsData = modelCollection.Find(filter2).Project<DeployModelsDto>(projection1).ToList();
            bool DBEncryptionRequired = CommonUtility.EncryptDB(correlationId, appSettings, servicename);
            if (modelsData.Count > 0)
            {
                for (int i = 0; i < modelsData.Count; i++)
                {
                    if (DBEncryptionRequired)
                    {
                        if (modelsData[i].InputSample != null)
                            modelsData[i].InputSample = _encryptionDecryption.Decrypt(modelsData[i].InputSample);
                        try
                        {
                            if (!string.IsNullOrEmpty(Convert.ToString(modelsData[i].CreatedByUser)))
                            {
                                modelsData[i].CreatedByUser = _encryptionDecryption.Decrypt(Convert.ToString(modelsData[i].CreatedByUser));
                            }
                        }
                        catch (Exception ex) { LOGGING.LogManager.Logger.LogProcessInfo(typeof(DeployModelServices), nameof(GetDeployModel) + "User is already decrypted....", ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty); }
                        try
                        {
                            if (!string.IsNullOrEmpty(Convert.ToString(modelsData[i].ModifiedByUser)))
                            {
                                modelsData[i].ModifiedByUser = _encryptionDecryption.Decrypt(Convert.ToString(modelsData[i].ModifiedByUser));
                            }
                        }
                        catch (Exception ex) { LOGGING.LogManager.Logger.LogProcessInfo(typeof(DeployModelServices), nameof(GetDeployModel) + "User is already decrypted....", ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty); }
                    }
                    modelsDto.Add(modelsData[i]);
                }
            }
            deployModelView.DeployModels = modelsDto;
            List<string> datasource = new List<string>();
            datasource = CommonUtility.GetDataSourceModel(correlationId, appSettings, servicename);
            List<string> datasourceDetails = new List<string>();
            datasourceDetails = CommonUtility.GetDataSourceModelDetails(correlationId, appSettings, servicename);
            if (datasource.Count > 0)
            {
                deployModelView.ModelName = datasource[0];
                deployModelView.DataSource = datasource[1];
                deployModelView.ModelType = datasource[2];
                if (datasource.Count > 2)
                    deployModelView.BusinessProblem = datasource[3];
                if (datasource.Count > 4)
                {
                    if (!string.IsNullOrEmpty(datasource[4]))
                    {
                        deployModelView.InstaFlag = true;
                        deployModelView.Category = datasource[5];
                    }
                }
            }
            deployModelView.Category = datasourceDetails[3];
            return deployModelView;
        }

        public void SavePrediction(PredictionDTO predictionDTO)
        {
            bool DBEncryptionRequired = CommonUtility.EncryptDB(predictionDTO.CorrelationId, appSettings);
            if (DBEncryptionRequired)
            {
                if (!string.IsNullOrEmpty(Convert.ToString(predictionDTO.CreatedByUser)))
                {
                    predictionDTO.CreatedByUser = _encryptionDecryption.Encrypt(Convert.ToString(predictionDTO.CreatedByUser));
                }
                if (!string.IsNullOrEmpty(Convert.ToString(predictionDTO.ModifiedByUser)))
                {
                    predictionDTO.ModifiedByUser = _encryptionDecryption.Encrypt(Convert.ToString(predictionDTO.ModifiedByUser));
                }
            }
            var jsonData = JsonConvert.SerializeObject(predictionDTO);
            var insertDocument = BsonSerializer.Deserialize<BsonDocument>(jsonData);
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_PublishModel);
            collection.InsertOne(insertDocument);
        }
        public PredictionDTO GetPrediction(PredictionDTO predictionDTO)
        {
            PredictionDTO prediction = new PredictionDTO();
            var builder = Builders<PredictionDTO>.Filter;
            var filter = builder.Eq(CONSTANTS.CorrelationId, predictionDTO.CorrelationId) & builder.Eq(CONSTANTS.UniqueId, predictionDTO.UniqueId);
            var projection = Builders<PredictionDTO>.Projection.Exclude("_id");
            var collection = _database.GetCollection<PredictionDTO>(CONSTANTS.SSAI_PublishModel);
            var result = collection.Find(filter).Project<PredictionDTO>(projection).ToList();
            bool DBEncryptionRequired = CommonUtility.EncryptDB(predictionDTO.CorrelationId, appSettings);
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
                    catch (Exception ex) { LOGGING.LogManager.Logger.LogProcessInfo(typeof(DeployModelServices), nameof(GetPrediction) + "User is already decrypted....", ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty); }
                    try
                    {
                        if (!string.IsNullOrEmpty(Convert.ToString(result[0].ModifiedByUser)))
                        {
                            result[0].ModifiedByUser = _encryptionDecryption.Decrypt(Convert.ToString(result[0].ModifiedByUser));
                        }
                    }
                    catch (Exception ex) { LOGGING.LogManager.Logger.LogProcessInfo(typeof(DeployModelServices), nameof(GetPrediction) + "User is already decrypted....", ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty); }
                }
                prediction = result[0];
            }
            return prediction;
        }


        /// <summary>
        /// Get VisualizationData
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="modelName"></param>
        /// <returns></returns>
        public object GetVisualizationData(string correlationId, string modelName, bool isPrediction)
        {
            List<BsonDocument> result = new List<BsonDocument>();
            var builder = Builders<BsonDocument>.Filter;
            CascadeViz cascadeModel = new CascadeViz();
            var hyperTunecollection = _database.GetCollection<BsonDocument>(CONSTANTS.ME_HyperTuneVersion);
            var deployCollection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
            var deployFilter = Builders<DeployModelsDto>.Filter.Eq(CONSTANTS.CorrelationId, correlationId.Trim());
            var deployModelResult = deployCollection.Find(deployFilter).Project<DeployModelsDto>(Builders<DeployModelsDto>.Projection.Include(CONSTANTS.IsCascadeModel).Exclude(CONSTANTS.Id)).ToList();
            if (deployModelResult.Count > 0)
            {
                if (deployModelResult[0].IsCascadeModel)
                {
                    cascadeModel = GetCascadeVisualization(correlationId);
                    var filter = builder.Eq(CONSTANTS.CorrelationId, cascadeModel.CorrelationId) & builder.Eq(CONSTANTS.modelName, cascadeModel.ModelName);
                    var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_RecommendedTrainedModels);
                    var projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
                    result = collection.Find(filter).Project<BsonDocument>(projection).ToList();
                }
                else
                {
                    var filter = builder.Eq(CONSTANTS.CorrelationId, correlationId) & builder.Eq(CONSTANTS.modelName, modelName);
                    var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_RecommendedTrainedModels);
                    var projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
                    result = collection.Find(filter).Project<BsonDocument>(projection).ToList();
                }
            }
            var data = new JObject();
            VisualDataModel visualDataModel = new VisualDataModel();
            visualDataModel.RangeTime = new List<object>();
            visualDataModel.Forecast = new List<object>();
            visualDataModel.predictionproba = new List<object>();
            visualDataModel.xlabel = new List<object>();
            visualDataModel.legend = new List<object>();

            List<string> modelDetails = new List<string>();
            if (result.Count > 0)
            {
                var problemType = result[0]["ProblemType"].ToString();
                if (problemType.Equals(CONSTANTS.TimeSeries, StringComparison.InvariantCultureIgnoreCase))
                {
                    var foreCast = result[0]["Forecast"].ToJson();
                    var foreCastList = JsonConvert.DeserializeObject<object[]>(foreCast);
                    if (!isPrediction)
                    {
                        foreCastList = foreCastList.Take(5).ToArray();
                    }
                    var rangeTime = result[0]["RangeTime"].ToJson();
                    var rangeTimeList = JsonConvert.DeserializeObject<object[]>(rangeTime);
                    if (!isPrediction)
                    {
                        rangeTimeList = rangeTimeList.Take(5).ToArray();
                    }
                    visualDataModel.Target = result[0]["Target"].ToString();
                    visualDataModel.Frequency = result[0]["Frequency"].ToString();
                    visualDataModel.RangeTime.AddRange(rangeTimeList);
                    visualDataModel.Forecast.AddRange(foreCastList);
                    if (result[0].Contains("xlabelname"))
                    {
                        var xlabelName = result[0]["xlabelname"].ToString();
                        visualDataModel.xlabelname = xlabelName;
                    }
                    if (result[0].Contains("ylabelname"))
                    {
                        var ylabelName = result[0]["ylabelname"].ToString();
                        visualDataModel.ylabelname = ylabelName;
                    }
                }
                else
                {
                    if (result[0].Contains("visualization") != false)
                    {
                        var xlabel = result[0]["visualization"]["xlabel"].ToJson();
                        var xlabelList = JsonConvert.DeserializeObject<object[]>(xlabel);
                        if (!isPrediction)
                        {
                            xlabelList = xlabelList.Take(5).ToArray();
                        }
                        var predictionproba = result[0]["visualization"]["predictionproba"].ToString();
                        var predictionprobaList = JsonConvert.DeserializeObject<List<object>>(predictionproba);
                        if (!isPrediction)
                        {
                            predictionprobaList = predictionprobaList.Take(5).ToList();
                        }
                        visualDataModel.xlabel.AddRange(xlabelList);
                        visualDataModel.predictionproba.AddRange(predictionprobaList);
                        visualDataModel.target = result[0]["visualization"]["target"].ToString();
                        var legend = result[0]["visualization"]["legend"].ToJson();
                        var legendList = JsonConvert.DeserializeObject<List<object>>(legend);
                        visualDataModel.legend.AddRange(legendList);
                        if (result[0]["visualization"].ToString().Contains("xlabelname"))
                        {
                            var xlabelName = result[0]["visualization"]["xlabelname"].ToString();
                            visualDataModel.xlabelname = xlabelName;
                        }
                        if (result[0]["visualization"].ToString().Contains("ylabelname"))
                        {
                            var ylabelName = result[0]["visualization"]["ylabelname"].ToString();
                            visualDataModel.ylabelname = ylabelName;
                        }


                    }
                }
                visualDataModel.ProblemType = problemType;
                if (deployModelResult[0].IsCascadeModel)
                    modelDetails = CommonUtility.GetDataSourceModel(cascadeModel.CorrelationId, appSettings);
                else
                    modelDetails = CommonUtility.GetDataSourceModel(correlationId, appSettings);

                if (modelDetails.Count > 0)
                {
                    if (modelDetails.Count > 2)
                        visualDataModel.BusinessProblems = Convert.ToString(modelDetails[1]);
                    visualDataModel.ModelName = Convert.ToString(modelDetails[0]);

                    if (deployModelResult[0].IsCascadeModel)
                    {
                        visualDataModel.DataSource = "Cascading";
                        visualDataModel.Category = cascadeModel.Category;
                    }
                    else
                    {
                        visualDataModel.DataSource = Convert.ToString(modelDetails[1]);
                        visualDataModel.Category = Convert.ToString(modelDetails[5]);
                    }
                }
            }
            else
            {
                var HyperFilter = builder.Eq(CONSTANTS.CorrelationId, correlationId) & builder.Eq(CONSTANTS.VersionName, modelName);
                var hyperTuneprojection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
                var HyperTuneResult = hyperTunecollection.Find(HyperFilter).Project<BsonDocument>(hyperTuneprojection).ToList();

                if (HyperTuneResult.Count > 0)
                {
                    var problemType = HyperTuneResult[0]["ProblemType"].ToString();
                    if (HyperTuneResult[0].Contains("visualization") != false)
                    {
                        var xlabel = HyperTuneResult[0]["visualization"]["xlabel"].ToJson();
                        var xlabelList = JsonConvert.DeserializeObject<object[]>(xlabel);
                        if (!isPrediction)
                        {
                            xlabelList = xlabelList.Take(5).ToArray();
                        }
                        var predictionproba = HyperTuneResult[0]["visualization"]["predictionproba"].ToString();
                        var predictionprobaList = JsonConvert.DeserializeObject<List<object>>(predictionproba);
                        if (!isPrediction)
                        {
                            predictionprobaList = predictionprobaList.Take(5).ToList();
                        }
                        visualDataModel.xlabel.AddRange(xlabelList);
                        visualDataModel.predictionproba.AddRange(predictionprobaList);
                        visualDataModel.target = HyperTuneResult[0]["visualization"]["target"].ToString();
                        var legend = HyperTuneResult[0]["visualization"]["legend"].ToJson();
                        var legendList = JsonConvert.DeserializeObject<List<object>>(legend);
                        visualDataModel.legend.AddRange(legendList);
                        if (HyperTuneResult[0]["visualization"].ToString().Contains("xlabelname"))
                        {
                            var xlabelName = HyperTuneResult[0]["visualization"]["xlabelname"].ToString();
                            visualDataModel.xlabelname = xlabelName;
                        }
                        if (HyperTuneResult[0]["visualization"].ToString().Contains("ylabelname"))
                        {
                            var ylabelName = HyperTuneResult[0]["visualization"]["ylabelname"].ToString();
                            visualDataModel.ylabelname = ylabelName;
                        }
                    }
                    visualDataModel.ProblemType = problemType;
                    if (deployModelResult[0].IsCascadeModel)
                        modelDetails = CommonUtility.GetDataSourceModel(cascadeModel.CorrelationId, appSettings);
                    else
                        modelDetails = CommonUtility.GetDataSourceModel(correlationId, appSettings);
                    if (modelDetails.Count > 0)
                    {
                        if (modelDetails.Count > 2)
                            visualDataModel.BusinessProblems = Convert.ToString(modelDetails[3]);
                        visualDataModel.ModelName = Convert.ToString(modelDetails[0]);

                        if (deployModelResult[0].IsCascadeModel)
                        {
                            visualDataModel.DataSource = "Cascading"; ;
                            visualDataModel.Category = cascadeModel.Category;
                        }
                        else
                        {
                            visualDataModel.DataSource = Convert.ToString(modelDetails[1]);
                            visualDataModel.Category = Convert.ToString(modelDetails[5]);
                        }
                    }
                }
            }
            return visualDataModel;
        }
        private CascadeViz GetCascadeVisualization(string cascadeId)
        {
            CascadeViz cascade = new CascadeViz();
            var Collection = _database.GetCollection<CascadeData>(CONSTANTS.SSAICascadedModels);
            var filter = Builders<CascadeData>.Filter.Eq(CONSTANTS.CascadedId, cascadeId.Trim());
            var projection = Builders<CascadeData>.Projection.Include(CONSTANTS.ModelName).Include(CONSTANTS.Category).Include(CONSTANTS.IsCustomModel).Include(CONSTANTS.ModelList).Include(CONSTANTS.CascadedId).Exclude(CONSTANTS.Id);
            var result = Collection.Find(filter).Project<CascadeData>(projection).ToList();
            if (result.Count > 0)
            {
                cascade.Category = result[0].Category;
                JObject modelsList = JObject.Parse(result[0].ModelList.ToString());
                if (modelsList != null && modelsList.ToString() != "{}")
                {
                    string name = string.Empty;
                    int totalCount = modelsList.Children().Count();
                    if (result[0].IsCustomModel)
                    {
                        if (totalCount > 4)
                            name = string.Format("Model{0}", modelsList.Children().Count());
                        else
                        {
                            int count = modelsList.Children().Count() - 1;
                            name = string.Format("Model{0}", count);
                        }
                    }
                    else
                    {
                        name = string.Format("Model{0}", modelsList.Children().Count());
                    }
                    var deployedModel = JsonConvert.DeserializeObject<DeployModelDetails>(modelsList[name].ToString());
                    if (deployedModel != null)
                    {
                        cascade.CorrelationId = deployedModel.CorrelationId;
                        cascade.ModelName = deployedModel.ModelType;
                    }
                }
            }
            return cascade;
        }

        public string PredictionModel(string correlationId, string actualData)
        {
            string predictionResult = string.Empty;
            bool DBEncryptionRequired = CommonUtility.EncryptDB(correlationId, appSettings);
            PredictionDTO predictionDTO = new PredictionDTO
            {
                _id = Guid.NewGuid().ToString(),
                UniqueId = Guid.NewGuid().ToString(),
                //ActualData = actualData,
                CorrelationId = correlationId,
                Frequency = null,
                PredictedData = null,
                Status = "I",
                ErrorMessage = null,
                Progress = null,
                CreatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                ModifiedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                CreatedByUser = "System",
                ModifiedByUser = "System"
            };
            if (DBEncryptionRequired)
                predictionDTO.ActualData = _encryptionDecryption.Encrypt(actualData);
            else
                predictionDTO.ActualData = actualData;
            SavePrediction(predictionDTO);
            DeployModelsDto mdl = GetDeployModelDetails(correlationId);
            BsonDocument cascadeModel = GetCascadeModelDetails(correlationId);
            IngrainRequestQueue ingrainRequest = new IngrainRequestQueue
            {
                _id = Guid.NewGuid().ToString(),
                CorrelationId = correlationId,
                RequestId = Guid.NewGuid().ToString(),
                ProcessId = null,
                Status = "I",//null,
                ModelName = null,
                RequestStatus = "New",
                RetryCount = 0,
                ProblemType = null,
                Message = null,
                UniId = predictionDTO.UniqueId,
                Progress = null,
                pageInfo = "PublishModel", // pageInfo 
                ParamArgs = "{}",
                Function = "PublishModel",
                CreatedByUser = "System",
                CreatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                ModifiedByUser = "System",
                ModifiedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                LastProcessedOn = null
            };
            if (mdl != null)
            {
                ingrainRequest.AppID = mdl.AppId;
            }
            if (mdl.IsCascadeModel)
                ingrainRequest.ParamArgs = CONSTANTS.True;
            _ingestedData.InsertRequests(ingrainRequest);

            Thread.Sleep(2000);

            bool isPrediction = true;
            while (isPrediction)
            {
                var predictionData = GetPrediction(predictionDTO);
                if (predictionData.Status == "C")
                {
                    predictionResult = predictionData.PredictedData;
                    ValidRecordsDetailsModel validRecordsDetailModel = CommonUtility.GetModelDataPointdDetails(correlationId, appSettings);
                    if (validRecordsDetailModel != null)
                    {
                        if (validRecordsDetailModel.ValidRecordsDetails != null)
                        {
                            if (validRecordsDetailModel.ValidRecordsDetails.Records[0] >= 4 && validRecordsDetailModel.ValidRecordsDetails.Records[0] < 20)
                            {
                                predictionData.DataPointsWarning = CONSTANTS.DataPointsMinimum;
                                predictionData.DataPointsCount = validRecordsDetailModel.ValidRecordsDetails.Records[0];
                            }
                        }
                    }
                    isPrediction = false;

                }
                else if (predictionData.Status == "E")
                {
                    isPrediction = false;
                    predictionResult = "Python - Prediction failed : " + predictionData.ErrorMessage;

                }
                else
                {
                    Thread.Sleep(2000);
                    isPrediction = true;
                }
            }

            return predictionResult;

        }

        public PredictionResultDTO PredictionModelPerformance(string correlationId, string actualData)
        {
            bool DBEncryptionRequired = CommonUtility.EncryptDB(correlationId, appSettings);
            PredictionResultDTO _predictionresult = new PredictionResultDTO();
            PredictionDTO predictionDTO = new PredictionDTO
            {
                _id = Guid.NewGuid().ToString(),
                UniqueId = Guid.NewGuid().ToString(),
                //ActualData = Data,
                CorrelationId = correlationId,
                Frequency = null,
                PredictedData = null,
                Status = "I",
                ErrorMessage = null,
                Progress = null,
                CreatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                ModifiedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                CreatedByUser = "System",
                ModifiedByUser = "System"
            };
            if (DBEncryptionRequired)
                predictionDTO.ActualData = _encryptionDecryption.Encrypt(actualData);
            else
                predictionDTO.ActualData = actualData;
            this.SavePrediction(predictionDTO);
            DeployModelsDto mdl = GetDeployModelDetails(correlationId);

            IngrainRequestQueue ingrainRequest = new IngrainRequestQueue
            {
                _id = Guid.NewGuid().ToString(),
                AppID = mdl.AppId,
                CorrelationId = correlationId,
                RequestId = Guid.NewGuid().ToString(),
                ProcessId = null,
                Status = "I",//null,
                ModelName = null,
                RequestStatus = "New",
                RetryCount = 0,
                ProblemType = null,
                Message = null,
                UniId = predictionDTO.UniqueId,
                Progress = null,
                pageInfo = "PublishModel", // pageInfo 
                ParamArgs = "{}",
                Function = "PublishModel",
                CreatedByUser = "System",
                CreatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                ModifiedByUser = "System",
                ModifiedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                LastProcessedOn = null
            };
            _ingestedData.InsertRequests(ingrainRequest);
            Thread.Sleep(2000);
            _predictionresult.CorrelationId = correlationId;
            _predictionresult.UniqueId = predictionDTO.UniqueId;
            var predictionData = this.GetPrediction(predictionDTO);
            if (predictionData.Status == "I")
            {
                _predictionresult.Status = null;
                _predictionresult.Message = "Data received. Data prediction in Process";
            }

            return _predictionresult;

        }

        public DeployModelsDto GetDeployModelDetails(string correlationId)
        {
            DeployModelsDto deployedModel = new DeployModelsDto();
            var modelCollection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
            var projection1 = Builders<DeployModelsDto>.Projection.Exclude(CONSTANTS.Id);
            var filterBuilder = Builders<DeployModelsDto>.Filter;
            var filter2 = filterBuilder.Eq(CONSTANTS.CorrelationId, correlationId.Trim()) & filterBuilder.Eq(CONSTANTS.Status, CONSTANTS.Deployed);
            deployedModel = modelCollection.Find(filter2).Project<DeployModelsDto>(projection1).FirstOrDefault();
            return deployedModel;
        }
        public BsonDocument GetCascadeModelDetails(string correlationId)
        {
            BsonDocument bsonElements = new BsonDocument();
            var modelCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAICascadedModels);
            var projection1 = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.MappingData).Exclude(CONSTANTS.Id);
            var filterBuilder = Builders<BsonDocument>.Filter;
            var filter2 = filterBuilder.Eq(CONSTANTS.CascadedId, correlationId);
            bsonElements = modelCollection.Find(filter2).Project<BsonDocument>(projection1).FirstOrDefault();
            return bsonElements;
        }
        public ForeCastModel ForeCastModel(string CorrelationId, string frequency, string Data)
        {
            ForeCastModel prediction = new ForeCastModel();
            PredictionDTO predictionDTO = new PredictionDTO
            {
                _id = Guid.NewGuid().ToString(),
                UniqueId = Guid.NewGuid().ToString(),
                ActualData = Data,
                CorrelationId = CorrelationId,
                Frequency = frequency,
                PredictedData = null,
                Status = "I",
                ErrorMessage = null,
                Progress = null,
                CreatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                ModifiedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                CreatedByUser = "System",
                ModifiedByUser = "System"
            };
            bool DBEncryptionRequired = CommonUtility.EncryptDB(CorrelationId, appSettings);
            if (DBEncryptionRequired)
                predictionDTO.ActualData = _encryptionDecryption.Encrypt(Data);
            SavePrediction(predictionDTO);
            IngrainRequestQueue ingrainRequest = new IngrainRequestQueue
            {
                _id = Guid.NewGuid().ToString(),
                CorrelationId = CorrelationId,
                RequestId = Guid.NewGuid().ToString(),
                ProcessId = null,
                Status = null,
                ModelName = null,
                RequestStatus = "New",
                RetryCount = 0,
                ProblemType = null,
                Message = null,
                UniId = predictionDTO.UniqueId,
                Progress = null,
                pageInfo = "ForecastModel", // pageInfo 
                ParamArgs = "{}",
                Function = "ForecastModel",
                CreatedByUser = DBEncryptionRequired ? _encryptionDecryption.Encrypt("System") : "System",
                CreatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                ModifiedByUser = DBEncryptionRequired ? _encryptionDecryption.Encrypt("System") : "System",
                ModifiedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                LastProcessedOn = null
            };
            InsertRequests(ingrainRequest);
            Thread.Sleep(2000);
            bool isPrediction = true;
            while (isPrediction)
            {
                var predictionData = GetPrediction(predictionDTO);
                if (predictionData.Status == CONSTANTS.C)
                {
                    prediction.PredictionResult = predictionData.PredictedData;
                    prediction.Status = CONSTANTS.C;
                    ValidRecordsDetailsModel validRecordsDetailModel = CommonUtility.GetModelDataPointdDetails(CorrelationId, appSettings);
                    if (validRecordsDetailModel != null)
                    {
                        if (validRecordsDetailModel.ValidRecordsDetails != null)
                        {
                            if (validRecordsDetailModel.ValidRecordsDetails.Records[0] >= 4 && validRecordsDetailModel.ValidRecordsDetails.Records[0] < 20)
                            {
                                prediction.DataPointsWarning = CONSTANTS.DataPointsMinimum;
                                prediction.DataPointsCount = validRecordsDetailModel.ValidRecordsDetails.Records[0];
                            }
                        }
                    }

                    isPrediction = false;
                }
                else if (predictionData.Status == CONSTANTS.E)
                {
                    isPrediction = false;
                    prediction.Status = CONSTANTS.E;
                    prediction.Message = "Python - Prediction Failed : " + predictionData.ErrorMessage;
                }
                else
                {
                    Thread.Sleep(2000);
                    isPrediction = true;
                }
            }
            return prediction;
        }
        private void InsertRequests(IngrainRequestQueue ingrainRequest)
        {
            var requestQueue = JsonConvert.SerializeObject(ingrainRequest);
            var insertRequestQueue = BsonSerializer.Deserialize<BsonDocument>(requestQueue);
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
            collection.InsertOne(insertRequestQueue);
        }
        public void RetrieveModel(string correlationId)
        {
            var archiveModelCollection = _database.GetCollection<ArchiveModels>(CONSTANTS.SSAI_DeployedModels_archive);
            var projection = Builders<ArchiveModels>.Projection.Exclude(CONSTANTS.Id).Exclude("CollectionValue._id");
            //var filter = Builders<ArchiveModels>.Filter.Empty;
            var filter = Builders<ArchiveModels>.Filter.Eq("CollectionValue.CorrelationId", correlationId);
            var archiveModels = archiveModelCollection.Find(filter).Project<ArchiveModels>(projection).ToList();

            if (archiveModels.Count > 0)
            {
                foreach (var model in archiveModels)
                {
                    if (model.CollectionValue.CorrelationId == correlationId)
                    {
                        if (model.CollectionValue._id == null)
                            model.CollectionValue._id = Guid.NewGuid().ToString();
                        var req = JsonConvert.SerializeObject(model.CollectionValue);
                        var insertRequestQueue = BsonSerializer.Deserialize<DeployModelsDto>(req);
                        var collection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAI_DeployedModels);
                        collection.InsertOne(insertRequestQueue);

                        archiveModelCollection.DeleteOne(filter);
                    }
                }
            }

            var collection_DE_DataCleanup = _database.GetCollection<ArchiveModel>(CONSTANTS.DE_DataCleanup_archive);
            var projection1 = Builders<ArchiveModel>.Projection.Exclude(CONSTANTS.Id);
            var filter1 = Builders<ArchiveModel>.Filter.Eq("CollectionValue.CorrelationId", correlationId);
            var decleanupresult = collection_DE_DataCleanup.Find(filter1).Project<ArchiveModel>(projection1).ToList();
            if (decleanupresult.Count > 0)
            {
                foreach (var doc in decleanupresult)
                {
                    if (doc.CollectionValue[CONSTANTS.CorrelationId] == correlationId)
                    {
                        var insertRequestQueue = BsonSerializer.Deserialize<BsonDocument>(doc.CollectionValue);
                        var collection = _database.GetCollection<BsonDocument>(CONSTANTS.DE_DataCleanup);
                        collection.InsertOne(insertRequestQueue);

                        collection_DE_DataCleanup.DeleteOne(filter1);
                    }
                }
            }

            var collection_DE_DataProcessing = _database.GetCollection<ArchiveModel>(CONSTANTS.DE_DataProcessing_archive);
            var deDataProcessingResult = collection_DE_DataProcessing.Find(filter1).Project<ArchiveModel>(projection1).ToList();
            if (deDataProcessingResult.Count > 0)
            {
                foreach (var doc in deDataProcessingResult)
                {
                    if (doc.CollectionValue[CONSTANTS.CorrelationId] == correlationId)
                    {
                        var insertRequestQueue = BsonSerializer.Deserialize<BsonDocument>(doc.CollectionValue);
                        var collection = _database.GetCollection<BsonDocument>(CONSTANTS.DE_DataProcessing);
                        collection.InsertOne(insertRequestQueue);

                        collection_DE_DataProcessing.DeleteOne(filter1);
                    }
                }
            }

            var collection_DE_NewFeatureData = _database.GetCollection<ArchiveModel>(CONSTANTS.DE_NewFeatureData_archive);
            var DE_NewFeatureDataResult = collection_DE_NewFeatureData.Find(filter1).Project<ArchiveModel>(projection1).ToList();
            if (DE_NewFeatureDataResult.Count > 0)
            {
                foreach (var doc in DE_NewFeatureDataResult)
                {
                    if (doc.CollectionValue[CONSTANTS.CorrelationId] == correlationId)
                    {
                        var insertRequestQueue = BsonSerializer.Deserialize<BsonDocument>(doc.CollectionValue);
                        var collection = _database.GetCollection<BsonDocument>(CONSTANTS.DE_NewFeatureData);
                        collection.InsertOne(insertRequestQueue);

                        collection_DE_NewFeatureData.DeleteOne(filter1);
                    }
                }
            }

            var collection_DE_PreProcessedData = _database.GetCollection<ArchiveModel>(CONSTANTS.DE_PreProcessedData_archive);
            var DE_PreProcessedDataResult = collection_DE_PreProcessedData.Find(filter1).Project<ArchiveModel>(projection1).ToList();
            if (DE_PreProcessedDataResult.Count > 0)
            {
                foreach (var doc in DE_PreProcessedDataResult)
                {
                    if (doc.CollectionValue[CONSTANTS.CorrelationId] == correlationId)
                    {
                        var insertRequestQueue = BsonSerializer.Deserialize<BsonDocument>(doc.CollectionValue);
                        var collection = _database.GetCollection<BsonDocument>(CONSTANTS.DE_PreProcessedData);
                        collection.InsertOne(insertRequestQueue);

                        collection_DE_PreProcessedData.DeleteOne(filter1);
                    }
                }
            }

            var collection_SSAI_savedModels = _database.GetCollection<ArchiveModel>(CONSTANTS.SSAI_savedModels_archive);
            var SSAI_savedModelsResult = collection_SSAI_savedModels.Find(filter1).Project<ArchiveModel>(projection1).ToList();
            if (SSAI_savedModelsResult.Count > 0)
            {
                foreach (var doc in SSAI_savedModelsResult)
                {
                    if (doc.CollectionValue[CONSTANTS.CorrelationId] == correlationId)
                    {
                        var insertRequestQueue = BsonSerializer.Deserialize<BsonDocument>(doc.CollectionValue);
                        var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_savedModels);
                        collection.InsertOne(insertRequestQueue);

                        collection_SSAI_savedModels.DeleteOne(filter1);
                    }
                }
            }

            var collection_PS_BusinessProblem = _database.GetCollection<ArchiveModel>(CONSTANTS.PS_BusinessProblem_archive);
            var PS_BusinessProblemResult = collection_PS_BusinessProblem.Find(filter1).Project<ArchiveModel>(projection1).ToList();
            if (PS_BusinessProblemResult.Count > 0)
            {
                foreach (var doc in PS_BusinessProblemResult)
                {
                    if (doc.CollectionValue[CONSTANTS.CorrelationId] == correlationId)
                    {
                        var insertRequestQueue = BsonSerializer.Deserialize<BsonDocument>(doc.CollectionValue);
                        var collection = _database.GetCollection<BsonDocument>(CONSTANTS.PS_BusinessProblem);
                        collection.InsertOne(insertRequestQueue);

                        collection_PS_BusinessProblem.DeleteOne(filter1);
                    }
                }
            }

            var collection_PS_IngestedData = _database.GetCollection<ArchiveModel>(CONSTANTS.PS_IngestedData_archive);
            var PS_IngestedDataResult = collection_PS_IngestedData.Find(filter1).Project<ArchiveModel>(projection1).ToList();
            if (PS_IngestedDataResult.Count > 0)
            {
                foreach (var doc in PS_IngestedDataResult)
                {
                    if (doc.CollectionValue[CONSTANTS.CorrelationId] == correlationId)
                    {
                        var insertRequestQueue = BsonSerializer.Deserialize<BsonDocument>(doc.CollectionValue);
                        var collection = _database.GetCollection<BsonDocument>(CONSTANTS.PS_IngestedData);
                        collection.InsertOne(insertRequestQueue);

                        collection_PS_IngestedData.DeleteOne(filter1);
                    }
                }
            }

            var collection_PSUseCaseDefinition = _database.GetCollection<ArchiveModel>(CONSTANTS.PS_UsecaseDefinition_archive);
            var PSUseCaseDefinitionResult = collection_PSUseCaseDefinition.Find(filter1).Project<ArchiveModel>(projection1).ToList();
            if (PSUseCaseDefinitionResult.Count > 0)
            {
                foreach (var doc in PSUseCaseDefinitionResult)
                {
                    if (doc.CollectionValue[CONSTANTS.CorrelationId] == correlationId)
                    {
                        var insertRequestQueue = BsonSerializer.Deserialize<BsonDocument>(doc.CollectionValue);
                        var collection = _database.GetCollection<BsonDocument>(CONSTANTS.PSUseCaseDefinition);
                        collection.InsertOne(insertRequestQueue);

                        collection_PSUseCaseDefinition.DeleteOne(filter1);
                    }
                }
            }

            var collection_ME_RecommendedModels = _database.GetCollection<ArchiveModel>(CONSTANTS.ME_RecommendedModels_archive);
            var ME_RecommendedModelsResult = collection_ME_RecommendedModels.Find(filter1).Project<ArchiveModel>(projection1).ToList();
            if (ME_RecommendedModelsResult.Count > 0)
            {
                foreach (var doc in ME_RecommendedModelsResult)
                {
                    if (doc.CollectionValue[CONSTANTS.CorrelationId] == correlationId)
                    {
                        var insertRequestQueue = BsonSerializer.Deserialize<BsonDocument>(doc.CollectionValue);
                        var collection = _database.GetCollection<BsonDocument>(CONSTANTS.ME_RecommendedModels);
                        collection.InsertOne(insertRequestQueue);

                        collection_ME_RecommendedModels.DeleteOne(filter1);
                    }
                }
            }

            var collection_ME_HyperTuneVersion = _database.GetCollection<ArchiveModel>(CONSTANTS.ME_HyperTuneVersion_archive);
            var ME_HyperTuneVersion_archiveResult = collection_ME_HyperTuneVersion.Find(filter1).Project<ArchiveModel>(projection1).ToList();
            if (ME_HyperTuneVersion_archiveResult.Count > 0)
            {
                foreach (var doc in ME_HyperTuneVersion_archiveResult)
                {
                    if (doc.CollectionValue[CONSTANTS.CorrelationId] == correlationId)
                    {
                        var insertRequestQueue = BsonSerializer.Deserialize<BsonDocument>(doc.CollectionValue);
                        var collection = _database.GetCollection<BsonDocument>(CONSTANTS.ME_HyperTuneVersion);
                        collection.InsertOne(insertRequestQueue);

                        collection_ME_HyperTuneVersion.DeleteOne(filter1);
                    }
                }
            }

        }

        public List<DeployModelsDto> GetArchivedRecordList(string userId, string DeliveryConstructUID, string ClientUId)
        {
            string encryptedUser = userId;
            if (!string.IsNullOrEmpty(Convert.ToString(userId)))
                encryptedUser = _encryptionDecryption.Encrypt(Convert.ToString(userId));

            List<DeployModelsDto> deployModelsDtos = new List<DeployModelsDto>();
            var archiveModelCollection = _database.GetCollection<ArchiveModels>(CONSTANTS.SSAI_DeployedModels_archive);
            var projection = Builders<ArchiveModels>.Projection.Exclude(CONSTANTS.Id);

            var filter = (Builders<ArchiveModels>.Filter.Eq("CollectionValue.CreatedByUser", userId) | Builders<ArchiveModels>.Filter.Eq("CollectionValue.CreatedByUser", encryptedUser)) &
                   Builders<ArchiveModels>.Filter.Eq("CollectionValue.DeliveryConstructUID", DeliveryConstructUID) &
                   Builders<ArchiveModels>.Filter.Eq("CollectionValue.ClientUId", ClientUId);

            var archiveModels = archiveModelCollection.Find(filter).Project<ArchiveModels>(projection).ToList();
            bool dbEncryptionRequired;
            if (archiveModels.Count() > 0)
            {
                foreach (var model in archiveModels)
                {
                    dbEncryptionRequired = Convert.ToBoolean(model.CollectionValue.DBEncryptionRequired);
                    if (dbEncryptionRequired)
                    {
                        if (model.CollectionValue.InputSample != null)

                            model.CollectionValue.InputSample = _encryptionDecryption.Decrypt(model.CollectionValue.InputSample);
                        try
                        {
                            if (!string.IsNullOrEmpty(Convert.ToString(model.CollectionValue.CreatedByUser)))
                            {
                                model.CollectionValue.CreatedByUser = _encryptionDecryption.Decrypt(model.CollectionValue.CreatedByUser);
                            }
                        }
                        catch (Exception ex) { LOGGING.LogManager.Logger.LogProcessInfo(typeof(DeployModelServices), nameof(GetArchivedRecordList) + "User is already decrypted....", ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty); }
                        try
                        {
                            if (!string.IsNullOrEmpty(Convert.ToString(model.CollectionValue.ModifiedByUser)))
                            {
                                model.CollectionValue.ModifiedByUser = _encryptionDecryption.Decrypt(model.CollectionValue.ModifiedByUser);
                            }
                        }
                        catch (Exception ex) { LOGGING.LogManager.Logger.LogProcessInfo(typeof(DeployModelServices), nameof(GetArchivedRecordList) + "User is already decrypted....", ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty); }
                    }
                    deployModelsDtos.Add(model.CollectionValue);
                }
            }
            return deployModelsDtos;
        }


    }
}
