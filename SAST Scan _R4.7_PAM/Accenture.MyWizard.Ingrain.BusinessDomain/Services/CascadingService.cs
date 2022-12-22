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
Module Name     :   CascadingService
Project         :   Accenture.MyWizard.SelfServiceAI.BusinessDomain
Organisation    :   Accenture Technologies Ltd.
Created By      :   RaviA
Created Date    :   10-Nov-2020
Revision History :
Revision No             Initial             Modified Date           Description
1.0                     An                  10-Nov-2020           
\********************************************************************************************************/
#endregion

using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
using Accenture.MyWizard.Ingrain.DataAccess;
using Accenture.MyWizard.Ingrain.DataModels;
using Accenture.MyWizard.Ingrain.DataModels.InstaModels;
using Accenture.MyWizard.Ingrain.DataModels.Models;
using Accenture.MyWizard.Shared.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
using Accenture.MyWizard.Ingrain.BusinessDomain.Common;
using JsonConvert = Newtonsoft.Json.JsonConvert;
using System.Diagnostics;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using OfficeOpenXml;
using System.Data;
using System.Text.RegularExpressions;

namespace Accenture.MyWizard.Ingrain.BusinessDomain.Services
{
    public class CascadingService : ICascadingService
    {
        #region Members
        private CascadeModel _cascadeModel;
        private CustomCascadeModel _customCascade;
        private IEncryptionDecryption _encryptionDecryption;
        private bool _DBEncryptionRequired;
        private MongoClient _mongoClient;
        private IMongoDatabase _database;
        private readonly IOptions<IngrainAppSettings> appSettings;
        DatabaseProvider databaseProvider;
        private CustomMapping _customMapping;
        private CustomModelViewDetails customModelViewDetails;
        CascadeDeployViewModel deployModelView;
        UpdateCascadeModelMapping saveModel;
        private string uniqIdname = string.Empty;
        private string uniqDatatype = string.Empty;
        private string targetColumn = string.Empty;
        int TotalColumns = 0;
        int TotalRows = 0;
        List<string> MainColumns = null;
        DataTable dataTable = null;
        UploadResponse uploadResponse = null;
        FMUploadResponse fMUploadResponse = null;
        Filepath _filepath = null;
        FileUpload fileUpload = null;
        ParentFile parentFile = null;
        IngrainRequestQueue ingrainRequest = null;
        VisualizationViewModel visualizationViewModel = null;
        CascadeInfluencers cascadeInfluencers = null;
        FMVisualizationDTO fMVisualizationDTO = null;
        #endregion
        #region Constructor
        public CascadingService(DatabaseProvider db, IOptions<IngrainAppSettings> settings, IServiceProvider serviceProvider)
        {
            databaseProvider = db;
            appSettings = settings;
            _mongoClient = databaseProvider.GetDatabaseConnection();
            var dataBaseName = MongoUrl.Create(appSettings.Value.connectionString).DatabaseName;
            _database = _mongoClient.GetDatabase(dataBaseName);
            _encryptionDecryption = serviceProvider.GetService<IEncryptionDecryption>();
            _cascadeModel = new CascadeModel();
            _customCascade = new CustomCascadeModel();
            _customMapping = new CustomMapping();
            customModelViewDetails = new CustomModelViewDetails();
            deployModelView = new CascadeDeployViewModel();
            saveModel = new UpdateCascadeModelMapping();
            uploadResponse = new UploadResponse();
            visualizationViewModel = new VisualizationViewModel();
            cascadeInfluencers = new CascadeInfluencers();
            fMVisualizationDTO = new FMVisualizationDTO();
            fMUploadResponse = new FMUploadResponse();
        }
        #endregion
        public CascadeModel GetCascadingModels(string clientUid, string dcUid, string userId, string category, string cascadeId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CascadingService), nameof(GetCascadingModels), "START", string.Empty, string.Empty, clientUid, dcUid);
            try
            {
                string encrypteduser = userId;
                if (!string.IsNullOrEmpty(Convert.ToString(userId)))
                    encrypteduser = _encryptionDecryption.Encrypt(Convert.ToString(userId));
                string empty = null;
                _cascadeModel.DeliveryConstructUID = dcUid;
                _cascadeModel.ClientUid = clientUid;
                _cascadeModel.UserId = userId;
                _cascadeModel.Category = category;
                var collection = _database.GetCollection<DeployedModel>(CONSTANTS.SSAIDeployedModels);
                var filterBuilder = Builders<DeployedModel>.Filter;
                //Public filter
                var publicFilter = Builders<DeployedModel>.Filter.Eq(CONSTANTS.DeliveryConstructUID, dcUid) &
                  Builders<DeployedModel>.Filter.Eq(CONSTANTS.ClientUId, clientUid) &
                  Builders<DeployedModel>.Filter.Eq(CONSTANTS.IsPrivate, false) &
                  Builders<DeployedModel>.Filter.Eq(CONSTANTS.Status, CONSTANTS.Deployed) &
                  Builders<DeployedModel>.Filter.Eq(CONSTANTS.Category, category) &
                  Builders<DeployedModel>.Filter.Eq(CONSTANTS.IsModelTemplate, false) &
                  Builders<DeployedModel>.Filter.Ne(CONSTANTS.IsCascadeModel, true) &
                  Builders<DeployedModel>.Filter.Ne(CONSTANTS.IsFMModel, true) &
                  (Builders<DeployedModel>.Filter.Eq(CONSTANTS.CustomCascadeId, empty) | Builders<DeployedModel>.Filter.Eq(CONSTANTS.CustomCascadeId, CONSTANTS.BsonNull)) &
                  (Builders<DeployedModel>.Filter.Eq(CONSTANTS.FMCorrelationId, empty) | Builders<DeployedModel>.Filter.Eq(CONSTANTS.FMCorrelationId, CONSTANTS.BsonNull)) &
                  (Builders<DeployedModel>.Filter.Eq(CONSTANTS.ModelType, CONSTANTS.Classification) | Builders<DeployedModel>.Filter.Eq(CONSTANTS.ModelType, CONSTANTS.Regression) | Builders<DeployedModel>.Filter.Eq(CONSTANTS.ModelType, CONSTANTS.Multi_Class));

                //cascade template filter
                var cascadeModelTemplateFilter = Builders<DeployedModel>.Filter.Eq(CONSTANTS.DeliveryConstructUID, dcUid) &
                  Builders<DeployedModel>.Filter.Eq(CONSTANTS.ClientUId, clientUid) &
                  Builders<DeployedModel>.Filter.Eq(CONSTANTS.IsIncludedInCascade, true) &
                  Builders<DeployedModel>.Filter.Eq(CONSTANTS.Status, CONSTANTS.Deployed) &
                  Builders<DeployedModel>.Filter.Eq(CONSTANTS.Category, category) &
                  Builders<DeployedModel>.Filter.Eq(CONSTANTS.IsModelTemplate, true) &
                  Builders<DeployedModel>.Filter.Ne(CONSTANTS.IsCascadeModel, true) &
                  Builders<DeployedModel>.Filter.Ne(CONSTANTS.IsFMModel, true) &
                  (Builders<DeployedModel>.Filter.Eq(CONSTANTS.CustomCascadeId, empty) | Builders<DeployedModel>.Filter.Eq(CONSTANTS.CustomCascadeId, CONSTANTS.BsonNull)) &
                  (Builders<DeployedModel>.Filter.Eq(CONSTANTS.FMCorrelationId, empty) | Builders<DeployedModel>.Filter.Eq(CONSTANTS.FMCorrelationId, CONSTANTS.BsonNull)) &
                  (Builders<DeployedModel>.Filter.Eq(CONSTANTS.ModelType, CONSTANTS.Classification) | Builders<DeployedModel>.Filter.Eq(CONSTANTS.ModelType, CONSTANTS.Regression) | Builders<DeployedModel>.Filter.Eq(CONSTANTS.ModelType, CONSTANTS.Multi_Class));

                var filter = filterBuilder.Eq(CONSTANTS.ClientUId, clientUid)
                    & filterBuilder.Eq(CONSTANTS.DeliveryConstructUID, dcUid)
                    & filterBuilder.Eq(CONSTANTS.Status, CONSTANTS.Deployed)
                    & filterBuilder.Ne(CONSTANTS.IsCascadeModel, true)
                    & filterBuilder.Eq(CONSTANTS.Category, category)
                    & filterBuilder.Ne(CONSTANTS.IsModelTemplate, true)
                    & filterBuilder.Eq(CONSTANTS.IsPrivate, true)
                    & (filterBuilder.Eq(CONSTANTS.CreatedByUser, userId) | filterBuilder.Eq(CONSTANTS.CreatedByUser, encrypteduser))
                    & filterBuilder.Ne(CONSTANTS.IsFMModel, true)
                    & (filterBuilder.Eq(CONSTANTS.CustomCascadeId, empty) | filterBuilder.Eq(CONSTANTS.CustomCascadeId, CONSTANTS.BsonNull))
                    & (filterBuilder.Eq(CONSTANTS.FMCorrelationId, empty) | filterBuilder.Eq(CONSTANTS.FMCorrelationId, CONSTANTS.BsonNull))
                    & (filterBuilder.Eq(CONSTANTS.ModelType, CONSTANTS.Classification) | filterBuilder.Eq(CONSTANTS.ModelType, CONSTANTS.Regression) | filterBuilder.Eq(CONSTANTS.ModelType, CONSTANTS.Multi_Class));

                var projection = Builders<DeployedModel>.Projection.Exclude(CONSTANTS.Id);
                var publicModelResult = collection.Find(publicFilter).Project<DeployedModel>(projection).ToList();
                var ModelResult = collection.Find(filter).Project<DeployedModel>(projection).ToList();
                var cascadetemplates = collection.Find(cascadeModelTemplateFilter).Project<DeployedModel>(projection).ToList();
                var modelsList = ModelResult.Concat(publicModelResult).Concat(cascadetemplates).ToList();

                if (modelsList.Count > 0)
                {
                    List<CascadeModelDictionary> modelDictionary = new List<CascadeModelDictionary>();
                    for (int i = 0; i < modelsList.Count; i++)
                    {
                        CascadeModelDictionary cascadeModel = new CascadeModelDictionary();
                        cascadeModel.CorrelationId = modelsList[i].CorrelationId;
                        cascadeModel.ModelName = modelsList[i].ModelName;
                        cascadeModel.ProblemType = modelsList[i].ModelType;
                        cascadeModel.Accuracy = Math.Round(modelsList[i].Accuracy, 2);
                        cascadeModel.ModelType = modelsList[i].ModelVersion;
                        if (modelsList[i].LinkedApps != null)
                        {
                            if (modelsList[i].LinkedApps.Length > 0)
                                cascadeModel.LinkedApps = modelsList[i].LinkedApps[0];
                        }
                        else
                            cascadeModel.LinkedApps = null;
                        cascadeModel.ApplicationID = modelsList[i].AppId;

                        var businessCollection = _database.GetCollection<BusinessProblem>(CONSTANTS.PSBusinessProblem);
                        var filter2 = Builders<BusinessProblem>.Filter.Eq(CONSTANTS.CorrelationId, modelsList[i].CorrelationId);
                        var proj = Builders<BusinessProblem>.Projection.Include(CONSTANTS.TargetColumn).Exclude(CONSTANTS.Id);
                        var res = businessCollection.Find(filter2).Project<BusinessProblem>(proj).ToList();
                        if (res.Count > 0)
                        {
                            cascadeModel.TargetColumn = res[0].TargetColumn;
                        }
                        modelDictionary.Add(cascadeModel);
                    }
                    _cascadeModel.Models = modelDictionary;
                }
                else
                    _cascadeModel.Models = new List<CascadeModelDictionary>();

                if (!string.IsNullOrWhiteSpace(cascadeId) & cascadeId != CONSTANTS.Null & cascadeId != CONSTANTS.BsonNull)
                {
                    var cascadeCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAICascadedModels);
                    var cascadeFilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CascadedId, cascadeId);
                    var projection2 = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
                    var result = cascadeCollection.Find(cascadeFilter).Project<BsonDocument>(projection2).ToList();
                    if (result.Count > 0)
                    {
                        JObject data = JObject.Parse(result[0].ToString());
                        if (!string.IsNullOrEmpty(result[0][CONSTANTS.ModelList].ToString()) & result[0][CONSTANTS.ModelList].ToString() != CONSTANTS.BsonNull)
                        {
                            JObject modelList = JObject.Parse(result[0][CONSTANTS.ModelList].ToString());
                            int i = 1;
                            JObject obj = new JObject();
                            foreach (var item in modelList.Children())
                            {
                                JProperty prop = item as JProperty;
                                if (prop != null)
                                {
                                    CascadeModelDictionary cascadeModel = JsonConvert.DeserializeObject<CascadeModelDictionary>(prop.Value.ToString());
                                    string targetColumn = GetTargetColumn(cascadeModel.CorrelationId);
                                    if (string.IsNullOrEmpty(targetColumn))
                                        modelList["Model" + i][CONSTANTS.TargetColumn] = string.Empty;
                                    else
                                        modelList["Model" + i][CONSTANTS.TargetColumn] = targetColumn;
                                    i++;
                                }
                            }
                            _cascadeModel.ModelsList = modelList;
                        }
                        if (result[0].Contains(CONSTANTS.IsCustomModel))
                        {
                            _cascadeModel.IsCustomModel = Convert.ToBoolean(result[0][CONSTANTS.IsCustomModel]);
                        }
                        _cascadeModel.Status = data[CONSTANTS.Status].ToString();
                        _cascadeModel.ModelName = data[CONSTANTS.ModelName].ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                _cascadeModel.IsException = true;
                _cascadeModel.ErrorMessage = ex.Message + CONSTANTS.ThreeDots + ex.StackTrace;
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(CascadingService), nameof(GetCascadingModels), ex.Message + CONSTANTS.ThreeDots + ex.StackTrace, ex, string.Empty, string.Empty, clientUid, dcUid);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CascadingService), nameof(GetCascadingModels), "END", string.Empty, string.Empty, clientUid, dcUid);
            return _cascadeModel;
        }
        private string GetTargetColumn(string correlationId)
        {
            string targetColumn = string.Empty;
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.PSBusinessProblem);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var result = collection.Find(filter).Project<BsonDocument>(Builders<BsonDocument>.Projection.Include(CONSTANTS.TargetColumn).Exclude(CONSTANTS.Id)).ToList();
            if (result.Count > 0)
            {
                targetColumn = result[0][CONSTANTS.TargetColumn].ToString();
            }
            return targetColumn;
        }
        public CascadeSaveModel SaveCascadeModels(CascadeCollection data)
        {
            CascadeSaveModel saveModel = new CascadeSaveModel();
            saveModel.IsInserted = true;
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CascadingService), nameof(SaveCascadeModels), "START", string.Empty, string.Empty, data.ClientUId, data.DeliveryConstructUID);
            try
            {
                data.Status = CONSTANTS.New;
                var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAICascadedModels);
                data.ModifiedByUser = data.CreatedByUser;
                if (string.IsNullOrEmpty(data.CascadedId))
                {
                    data.CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                    data.ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                    data.CascadedId = Guid.NewGuid().ToString();
                    data._id = Guid.NewGuid().ToString();
                    saveModel.CascadedId = data.CascadedId;
                    bool DBEncryptionRequired = CascadedEncryptDB(data.CascadedId);
                    //if (DBEncryptionRequired)
                    {
                        if (!string.IsNullOrEmpty(Convert.ToString(data.CreatedByUser)))
                            data.CreatedByUser = _encryptionDecryption.Encrypt(Convert.ToString(data.CreatedByUser));
                        if (!string.IsNullOrEmpty(Convert.ToString(data.ModifiedByUser)))
                            data.ModifiedByUser = _encryptionDecryption.Encrypt(Convert.ToString(data.ModifiedByUser));
                    }
                    var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CascadedId, data.CascadedId);
                    var result = collection.Find(filter).ToList();
                    if (result.Count > 0)
                    {
                        saveModel.Status = result[0][CONSTANTS.Status].ToString();
                        saveModel.CascadedId = data.CascadedId;
                        data.ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                        BsonDocument modellist = new BsonDocument();
                        if (data.ModelList != null)
                        {
                            modellist = BsonDocument.Parse(JsonConvert.SerializeObject(data.ModelList));
                        }
                        var builder2 = Builders<BsonDocument>.Update;
                        if (data.isModelUpdated)
                        {
                            var update1 = builder2.Set(CONSTANTS.ModelList, modellist).Set(CONSTANTS.ModelName, data.ModelName).Set(CONSTANTS.Mappings, string.Empty).Set(CONSTANTS.MappingData, string.Empty).Set(CONSTANTS.Status, CONSTANTS.New).Set(CONSTANTS.ModifiedOn, data.ModifiedOn);
                            collection.UpdateMany(filter, update1);
                        }
                        else
                        {
                            var update2 = builder2.Set(CONSTANTS.ModelList, modellist).Set(CONSTANTS.ModelName, data.ModelName).Set(CONSTANTS.ModifiedOn, data.ModifiedOn);
                            collection.UpdateMany(filter, update2);
                        }
                        IncludeModelToCascading(data);
                        if (data.RemovedModels != null)
                        {
                            if (data.RemovedModels.Count() > 0)
                            {
                                IncludeModelToCascading(data.RemovedModels, data.CascadedId);
                            }
                        }
                    }
                    else
                    {
                        var jsonColumns = Newtonsoft.Json.JsonConvert.SerializeObject(data);
                        var insertBsonColumns = BsonSerializer.Deserialize<BsonDocument>(jsonColumns);
                        collection.InsertOne(insertBsonColumns);
                        //Inserting into Deployedmodel Collection
                        InsertDeployedModels(data, false, true);
                        IncludeModelToCascading(data);
                        if (data.RemovedModels != null)
                        {
                            if (data.RemovedModels.Count() > 0)
                            {
                                IncludeModelToCascading(data.RemovedModels, data.CascadedId);
                            }
                        }
                        saveModel.Status = CONSTANTS.New;
                    }
                }
                else
                {
                    saveModel.Status = CONSTANTS.New;
                    var filter4 = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CascadedId, data.CascadedId);
                    var result = collection.Find(filter4).ToList();
                    if (result.Count > 0)
                    {
                        saveModel.Status = result[0][CONSTANTS.Status].ToString();
                        if (result[0][CONSTANTS.Status].ToString() == CONSTANTS.Deployed)
                        {
                            if (data.isModelUpdated)
                            {
                                var deployCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
                                var filter22 = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, saveModel.CascadedId);
                                var builder = Builders<BsonDocument>.Update;
                                var update22 = builder.Set(CONSTANTS.Accuracy, 0).Set(CONSTANTS.Status, CONSTANTS.InProgress).Set(CONSTANTS.ModelURL, string.Empty).Set(CONSTANTS.VDSLink, string.Empty).Set(CONSTANTS.WebServices, string.Empty).Set(CONSTANTS.ModelVersion, string.Empty).Set("AppId", string.Empty).Set(CONSTANTS.DeployedDate, string.Empty).Set(CONSTANTS.ModifiedOn, DateTime.Now.ToString(CONSTANTS.DateHoursFormat)).Set(CONSTANTS.LinkedApps, string.Empty);
                                var updateResult2 = deployCollection.UpdateMany(filter22, update22);
                                saveModel.Status = CONSTANTS.New;
                            }
                        }
                        saveModel.Status = result[0][CONSTANTS.Status].ToString();
                    }
                    saveModel.CascadedId = data.CascadedId;
                    data.ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                    var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CascadedId, data.CascadedId);
                    BsonDocument modellist = new BsonDocument();
                    if (data.ModelList != null)
                    {
                        modellist = BsonDocument.Parse(JsonConvert.SerializeObject(data.ModelList));
                    }
                    var builder2 = Builders<BsonDocument>.Update;
                    if (data.isModelUpdated)
                    {
                        var update1 = builder2.Set(CONSTANTS.ModelList, modellist).Set(CONSTANTS.ModelName, data.ModelName).Set(CONSTANTS.Mappings, string.Empty).Set(CONSTANTS.MappingData, string.Empty).Set(CONSTANTS.ModifiedOn, data.ModifiedOn);
                        collection.UpdateMany(filter, update1);
                    }
                    else
                    {
                        var update2 = builder2.Set(CONSTANTS.ModelList, modellist).Set(CONSTANTS.ModelName, data.ModelName).Set(CONSTANTS.ModifiedOn, data.ModifiedOn);
                        collection.UpdateMany(filter, update2);
                    }
                    IncludeModelToCascading(data);
                    if (data.RemovedModels != null)
                    {
                        if (data.RemovedModels.Count() > 0)
                        {
                            IncludeModelToCascading(data.RemovedModels, data.CascadedId);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                saveModel.IsInserted = false;
                saveModel.IsException = true;
                saveModel.ErrorMessage = ex.Message + CONSTANTS.ThreeDots + ex.StackTrace;
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(CascadingService), nameof(SaveCascadeModels), ex.Message + CONSTANTS.ThreeDots + ex.StackTrace, ex, string.Empty, string.Empty, data.ClientUId, data.DeliveryConstructUID);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CascadingService), nameof(SaveCascadeModels), "END", string.Empty, string.Empty, data.ClientUId, data.DeliveryConstructUID);
            return saveModel;
        }
        public CascadeModelMapping GetMappingModels(string cascadedId)
        {
            CascadeModelMapping cascadeModelMapping = new CascadeModelMapping();
            cascadeModelMapping.CascadedId = cascadedId;
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CascadingService), nameof(GetMappingModels), "START", string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAICascadedModels);
                var projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.MappingData).Exclude(CONSTANTS.Id);
                var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CascadedId, cascadedId);
                var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();
                CascadeModelsCollectionList cascadeModelsCollection = new CascadeModelsCollectionList();
                List<string> inputColumns = new List<string>();
                if (result.Count > 0)
                {
                    JObject cascadeData = JObject.Parse(result[0].ToString());
                    cascadeModelMapping.Category = cascadeData[CONSTANTS.Category].ToString();
                    cascadeModelMapping.ModelName = cascadeData[CONSTANTS.ModelName].ToString();
                    if (cascadeData["IsCustomModel"] != null)
                    {
                        cascadeModelMapping.IsCustomModel = Convert.ToBoolean(cascadeData["IsCustomModel"]);
                    }
                    if (cascadeData[CONSTANTS.Mappings] != null)
                    {
                        if (!string.IsNullOrEmpty(cascadeData[CONSTANTS.Mappings].ToString()))
                        {
                            JObject mappings = JObject.Parse(cascadeData[CONSTANTS.Mappings].ToString());
                            cascadeModelMapping.MappingList = JObject.Parse(cascadeData[CONSTANTS.Mappings].ToString());
                        }
                    }
                    if (cascadeData[CONSTANTS.ModelList] != null)
                    {
                        List<CascadeModelsCollection> listModels = new List<CascadeModelsCollection>();
                        foreach (var item in cascadeData[CONSTANTS.ModelList].Children())
                        {
                            JProperty prop = item as JProperty;
                            if (prop != null)
                            {
                                CascadeModelsCollection models = JsonConvert.DeserializeObject<CascadeModelsCollection>(prop.Value.ToString());
                                listModels.Add(models);
                            }
                        }
                        if (listModels.Count > 0)
                        {
                            if (cascadeModelMapping.IsCustomModel)
                            {
                                JObject mappingData2 = cascadeModelMapping.MappingList;
                                string lastmodelname = string.Format("Model{0}", mappingData2.Children().Count());
                                uniqIdname = mappingData2[lastmodelname][CONSTANTS.UniqueMapping][CONSTANTS.Target].ToString();
                                targetColumn = mappingData2[lastmodelname][CONSTANTS.TargetMapping][CONSTANTS.Target].ToString();
                            }
                            bool fromMapModels = true;
                            JObject modelsMapping = GetMapping(listModels, cascadeModelMapping.IsCustomModel, fromMapModels);
                            if (_customMapping.IsException)
                            {
                                cascadeModelMapping.ErrorMessage = _customMapping.ErrorMessage;
                                cascadeModelMapping.IsError = _customMapping.IsException;
                            }
                            else
                            {
                                cascadeModelMapping.Status = cascadeData[CONSTANTS.Status].ToString();
                                cascadeModelMapping.MappingData = modelsMapping;
                                var bsondDoc = BsonDocument.Parse(modelsMapping.ToString());
                                var update = Builders<BsonDocument>.Update.Set("MappingData", bsondDoc);
                                var updateResult = collection.UpdateOne(filter, update);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                cascadeModelMapping.Status = CONSTANTS.E;
                cascadeModelMapping.IsError = true;
                cascadeModelMapping.ErrorMessage = ex.Message + "-STACKTRACE-" + ex.StackTrace;
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(CascadingService), nameof(GetMappingModels), ex.Message + CONSTANTS.ThreeDots + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CascadingService), nameof(GetMappingModels), "END", string.Empty, string.Empty, string.Empty, string.Empty);
            return cascadeModelMapping;
        }
        public UpdateCascadeModelMapping UpdateCascadeMapping(UpdateCasecadeModel data)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CascadingService), nameof(UpdateCascadeMapping), "START", string.Empty, string.Empty, string.Empty, string.Empty);
            UpdateCascadeModelMapping saveModel = new UpdateCascadeModelMapping();
            try
            {
                saveModel.CascadedId = Convert.ToString(data.CascadedId);
                saveModel.IsInserted = false;
                var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAICascadedModels);
                BsonDocument mappings = new BsonDocument();
                if (data.Mappings != null)
                {
                    JObject mappingData = JObject.Parse(data.Mappings.ToString());
                    if (mappingData != null)
                    {
                        if (!string.IsNullOrEmpty(mappingData.ToString()))
                        {
                            ValidationMapping validationData = MapValidation(mappingData, data.CascadedId, false);
                            if (validationData.IsException || !validationData.IsValidate)
                            {
                                saveModel.IsException = validationData.IsException;
                                saveModel.IsValidate = validationData.IsException;
                                saveModel.ErrorMessage = validationData.ErrorMessage;
                                saveModel.IsInserted = false;
                                return saveModel;
                            }
                        }
                    }
                    saveModel.IsValidate = true;
                    saveModel.Status = CONSTANTS.InProgress;
                    mappings = BsonDocument.Parse(JsonConvert.SerializeObject(data.Mappings));
                    var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CascadedId, data.CascadedId);
                    var update = Builders<BsonDocument>.Update.Set(CONSTANTS.Mappings, mappings).Set(CONSTANTS.Status, CONSTANTS.InProgress).Set(CONSTANTS.ModifiedOn, DateTime.Now.ToString(CONSTANTS.DateHoursFormat)).Set("isModelUpdated", data.isModelUpdated);

                    var result = collection.UpdateMany(filter, update);
                    if (result.ModifiedCount > 0)
                    {
                        saveModel.IsInserted = true;
                    }
                }
            }
            catch (Exception ex)
            {
                saveModel.IsException = true;
                saveModel.ErrorMessage = ex.Message + ex.StackTrace;
                saveModel.IsValidate = false;
                saveModel.IsInserted = false;
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(CascadingService), nameof(UpdateCascadeMapping), ex.Message + CONSTANTS.ThreeDots + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CascadingService), nameof(UpdateCascadeMapping), "END", string.Empty, string.Empty, string.Empty, string.Empty);
            return saveModel;
        }


        public CascadeDeployViewModel GetDeployedModel(string correlationId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CascadingService), "GetDeployedModel", "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            List<CascadeDeployModel> modelsDto = new List<CascadeDeployModel>();
            var modelCollection = _database.GetCollection<CascadeDeployModel>(CONSTANTS.SSAIDeployedModels);
            var projection1 = Builders<CascadeDeployModel>.Projection.Exclude(CONSTANTS.Id);
            var filterBuilder = Builders<CascadeDeployModel>.Filter;
            var filter2 = filterBuilder.Eq(CONSTANTS.CorrelationId, correlationId);
            var modelsData = modelCollection.Find(filter2).Project<CascadeDeployModel>(projection1).ToList();
            bool DBEncryptionRequired = CommonUtility.EncryptDB(correlationId, appSettings);
            string problemType = string.Empty;
            if (modelsData.Count > 0)
            {
                for (int i = 0; i < modelsData.Count; i++)
                {
                    if (DBEncryptionRequired)
                    {
                        if (modelsData[i].InputSample != null && modelsData[i].InputSample.ToString() != "null")
                            modelsData[i].InputSample = _encryptionDecryption.Decrypt(modelsData[i].InputSample);
                        try
                        {
                            if (!string.IsNullOrEmpty(Convert.ToString(modelsData[i].CreatedByUser)))
                                modelsData[i].CreatedByUser = _encryptionDecryption.Decrypt(Convert.ToString(modelsData[i].CreatedByUser));
                        }
                        catch (Exception ex)
                        {
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CascadingService), nameof(GetDeployedModel), ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty);
                        }
                        try
                        {
                            if (!string.IsNullOrEmpty(Convert.ToString(modelsData[i].ModifiedByUser)))
                                modelsData[i].ModifiedByUser = _encryptionDecryption.Decrypt(Convert.ToString(modelsData[i].ModifiedByUser));
                        }
                        catch (Exception ex)
                        {
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CascadingService), nameof(GetDeployedModel), ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty);
                        }
                    }
                    var modelDetails = GetAccuracy(correlationId);
                    modelsData[i].Accuracy = Math.Round(modelDetails.Accuracy, 2);
                    modelsData[i].ModelType = modelDetails.ProblemType;
                    modelsData[i].AppId = modelDetails.ApplicationID;
                    problemType = modelDetails.ProblemType;
                    modelsData[i].ModelVersion = modelDetails.ModelType;
                    modelsDto.Add(modelsData[i]);
                }

                deployModelView.IsIngrainModel = IsIngrainModel(correlationId);
                deployModelView.ModelName = modelsData[0].ModelName;
                deployModelView.DataSource = modelsData[0].DataSource;
                deployModelView.InstaFlag = false;
                deployModelView.Category = modelsData[0].Category;
                deployModelView.BusinessProblem = modelsData[0].DataSource;
                deployModelView.ModelType = problemType;
                deployModelView.IsCascadeModel = true;
            }
            deployModelView.CascadeModel = modelsDto;
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CascadingService), "GetDeployedModel", "END", string.Empty, string.Empty, string.Empty, string.Empty);
            return deployModelView;
        }
        private bool IsIngrainModel(string correlationId)
        {
            bool IsIngrainModel = false;
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAICascadedModels);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CascadedId, correlationId);
            var result = collection.Find(filter).Project<BsonDocument>(Builders<BsonDocument>.Projection.Exclude(CONSTANTS.MappingData).Exclude(CONSTANTS.Id)).ToList();
            if (result.Count > 0)
            {
                JObject modellist = JObject.Parse(result[0][CONSTANTS.ModelList].ToString());
                BsonElement element;
                var exists = result[0].TryGetElement("IsCustomModel", out element);
                if (exists)
                    deployModelView.IsCustomModel = (bool)result[0]["IsCustomModel"];
                else
                    deployModelView.IsCustomModel = false;
                if (modellist != null)
                {
                    foreach (var item in modellist.Children())
                    {
                        JProperty prop = item as JProperty;
                        DeployModelDetails model = JsonConvert.DeserializeObject<DeployModelDetails>(prop.Value.ToString());
                        if (model.LinkedApps == null)
                        {
                            var collection2 = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
                            var filter2 = Builders<DeployModelsDto>.Filter.Eq(CONSTANTS.CorrelationId, model.CorrelationId);
                            var projection = Builders<DeployModelsDto>.Projection.Include(CONSTANTS.LinkedApps).Exclude(CONSTANTS.Id);
                            var result2 = collection2.Find(filter2).Project<DeployModelsDto>(projection).FirstOrDefault();
                            if (result2 != null)
                            {
                                if (result2.LinkedApps.Length > 0)
                                    model.LinkedApps = result2.LinkedApps[0];
                            }
                        }
                        if (model.LinkedApps == "Ingrain")
                        {
                            IsIngrainModel = true;
                            break;
                        }
                    }
                }
            }
            return IsIngrainModel;
        }
        private DeployModelDetails GetAccuracy(string correlationId)
        {
            bool isCustom = false;
            DeployModelDetails modelDetails = new DeployModelDetails();
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAICascadedModels);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CascadedId, correlationId);
            var result = collection.Find(filter).Project<BsonDocument>(Builders<BsonDocument>.Projection.Include(CONSTANTS.ModelList).Include(CONSTANTS.IsCustomModel).Exclude(CONSTANTS.Id)).ToList();
            if (result.Count > 0)
            {
                JObject modellist = JObject.Parse(result[0][CONSTANTS.ModelList].ToString());
                BsonElement element;
                var exists = result[0].TryGetElement("IsCustomModel", out element);
                if (exists)
                    isCustom = (bool)result[0]["IsCustomModel"];
                else
                    isCustom = false;
                if (modellist != null)
                {
                    string requiredModel = string.Empty;
                    if (isCustom)
                        requiredModel = string.Format("Model{0}", modellist.Children().Count() - 1);
                    else
                        requiredModel = string.Format("Model{0}", modellist.Children().Count());
                    modelDetails = JsonConvert.DeserializeObject<DeployModelDetails>(modellist[requiredModel].ToString());
                }
            }
            return modelDetails;
        }
        private void InsertDeployedModels(CascadeCollection data, bool IsCustom, bool IsInsert)
        {
            string[] arr = new string[] { };
            bool encryptDB = false;
            bool DBEncryption = false;
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
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

            DeployModelsDto deployModel = new DeployModelsDto
            {
                _id = Guid.NewGuid().ToString(),
                CorrelationId = data.CascadedId,
                InstaId = null,
                ModelName = data.ModelName,
                Status = CONSTANTS.InProgress,
                ClientUId = data.ClientUId,
                DeliveryConstructUID = data.DeliveryConstructUID,
                DataSource = CONSTANTS.Cascading,
                DeployedDate = null,
                LinkedApps = arr,
                ModelVersion = null,
                ModelType = null,
                SourceName = CONSTANTS.Cascading,
                VDSLink = null,
                InputSample = null,
                IsPrivate = true,
                IsModelTemplate = false,
                DBEncryptionRequired = encryptDB,
                IsCascadeModel = true,
                TrainedModelId = null,
                Frequency = null,
                Category = data.Category,
                CreatedByUser = data.CreatedByUser,
                CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                ModifiedByUser = data.ModifiedByUser,
                ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                Language = CONSTANTS.English,
                IsModelTemplateDataSource = false
            };

            if (IsInsert)
            {
                var jsonColumns = Newtonsoft.Json.JsonConvert.SerializeObject(deployModel);
                var insertBsonColumns = BsonSerializer.Deserialize<BsonDocument>(jsonColumns);
                collection.InsertOne(insertBsonColumns);
                if (IsCustom)
                {
                    dynamic appDetails = GetAppName("Ingrain2");
                    if (appDetails != null)
                    {
                        deployModel.VDSLink = appDetails.BaseURL;
                    }
                    JObject modelsList = JObject.Parse(data.ModelList.ToString());
                    int count = modelsList.Children().Count() - 1;
                    string name = string.Format("Model{0}", count);
                    var deployedModel = JsonConvert.DeserializeObject<DeployModelDetails>(modelsList[name].ToString());
                    string[] arr2 = new string[] { "Ingrain2" };
                    deployModel.DeployedDate = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);

                    deployModel.ModelVersion = deployedModel.ModelType;
                    deployModel.ModelType = deployedModel.ProblemType;
                    deployedModel.Accuracy = deployedModel.Accuracy;
                    ///InputSample
                    //Model2 results
                    string name2 = string.Format("Model{0}", modelsList.Children().Count());
                    var deployedModel2 = JsonConvert.DeserializeObject<DeployModelDetails>(modelsList[name2].ToString());
                    var collection2 = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
                    var filterr = Builders<DeployModelsDto>.Filter.Eq(CONSTANTS.CorrelationId, deployedModel2.CorrelationId);
                    var projection2 = Builders<DeployModelsDto>.Projection.Exclude(CONSTANTS.Id);
                    var deployResult = collection2.Find(filterr).Project<DeployModelsDto>(projection2).FirstOrDefault();
                    //if only 2 models
                    if (modelsList.Children().Count() < 3)
                    {
                        if (deployResult != null)
                        {
                            if (deployResult.Status == CONSTANTS.Deployed)
                            {
                                deployedModel.ModelType = deployResult.ModelVersion;
                                deployedModel.ProblemType = deployResult.ModelType;
                                deployedModel.Accuracy = deployResult.Accuracy;
                                string sampleInput = AddCascadeSampleInput(data);
                                string inputSample = !string.IsNullOrEmpty(sampleInput) ? sampleInput.Replace(System.Environment.NewLine, String.Empty).Replace("\r\n", string.Empty) : null;
                                deployModel.InputSample = inputSample;
                            }
                            else
                            {
                                //Take FirstModel input sample. 2nd model not deployed.
                                var firstdeployedModel = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
                                var model1filter = Builders<DeployModelsDto>.Filter.Eq(CONSTANTS.CorrelationId, deployedModel.CorrelationId);
                                var model1projection = Builders<DeployModelsDto>.Projection.Exclude(CONSTANTS.Id);
                                var model1Result = firstdeployedModel.Find(model1filter).Project<DeployModelsDto>(model1projection).FirstOrDefault();
                                if (model1Result != null && model1Result.Status == CONSTANTS.Deployed)
                                {
                                    bool DBEncryptionRequired = CommonUtility.EncryptDB(deployedModel.CorrelationId, appSettings);
                                    if (DBEncryptionRequired)
                                    {
                                        if (model1Result.InputSample != null && model1Result.InputSample.ToString() != CONSTANTS.Null)
                                            deployModel.InputSample = _encryptionDecryption.Decrypt(model1Result.InputSample);
                                        else
                                            deployModel.InputSample = model1Result.InputSample;
                                    }
                                    else
                                        deployModel.InputSample = model1Result.InputSample;
                                }
                            }
                        }
                    }
                    else
                    {
                        string sampleInput = AddCascadeSampleInput(data);
                        string inputSample = !string.IsNullOrEmpty(sampleInput) ? sampleInput.Replace(System.Environment.NewLine, String.Empty).Replace("\r\n", string.Empty) : null;
                        deployModel.InputSample = inputSample;
                    }

                    string publishurl = appSettings.Value.publishURL;//ConfigurationManager.AppSettings["PublishURL"];
                    string Url = string.Format(publishurl + CONSTANTS.Zero, deployModel.CorrelationId);
                    deployModel.IsPrivate = false;
                    deployModel.IsModelTemplate = false;

                    var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, deployModel.CorrelationId);
                    if (encryptDB)
                        deployModel.InputSample = _encryptionDecryption.Encrypt(deployModel.InputSample);

                    var builder = Builders<BsonDocument>.Update;
                    var update = builder.Set(CONSTANTS.Accuracy, deployedModel.Accuracy)
                        .Set(CONSTANTS.VDSLink, deployModel.VDSLink)
                        .Set(CONSTANTS.ModelURL, Url)
                        .Set(CONSTANTS.LinkedApps, arr2)
                        .Set(CONSTANTS.Status, CONSTANTS.Deployed)
                        .Set(CONSTANTS.WebServices, "webservice")
                        .Set(CONSTANTS.DeployedDate, deployModel.DeployedDate)
                        .Set(CONSTANTS.IsPrivate, false)
                        .Set(CONSTANTS.IsModelTemplate, false)
                        .Set(CONSTANTS.ModelVersion, deployedModel.ModelType)
                        .Set(CONSTANTS.ModelType, deployedModel.ProblemType)
                        .Set(CONSTANTS.InputSample, deployModel.InputSample)
                        .Set(CONSTANTS.DeployedDate, DateTime.Now.ToString(CONSTANTS.DateHoursFormat));
                    var result = collection.UpdateMany(filter, update);
                }
            }
            else
            {
                if (IsCustom)
                {
                    dynamic appDetails = GetAppName("Ingrain2");
                    if (appDetails != null)
                    {
                        deployModel.VDSLink = appDetails.BaseURL;
                    }
                    JObject modelsList = JObject.Parse(data.ModelList.ToString());
                    int count = modelsList.Children().Count() - 1;
                    string name = string.Format("Model{0}", count);
                    var deployedModel = JsonConvert.DeserializeObject<DeployModelDetails>(modelsList[name].ToString());
                    string[] arr2 = new string[] { "Ingrain2" };
                    deployModel.DeployedDate = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);

                    deployModel.ModelVersion = deployedModel.ModelType;
                    deployModel.ModelType = deployedModel.ProblemType;
                    deployedModel.Accuracy = deployedModel.Accuracy;
                    string name2 = string.Format("Model{0}", modelsList.Children().Count());
                    var deployedModel2 = JsonConvert.DeserializeObject<DeployModelDetails>(modelsList[name2].ToString());
                    var collection2 = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
                    var filterr = Builders<DeployModelsDto>.Filter.Eq(CONSTANTS.CorrelationId, deployedModel2.CorrelationId);
                    var projection2 = Builders<DeployModelsDto>.Projection.Exclude(CONSTANTS.Id);
                    var deployResult = collection2.Find(filterr).Project<DeployModelsDto>(projection2).FirstOrDefault();
                    bool DBEncryptionRequired = CommonUtility.EncryptDB(deployedModel.CorrelationId, appSettings);
                    //if only 2 models
                    if (modelsList.Children().Count() < 3)
                    {
                        if (deployResult != null)
                        {
                            if (deployResult.Status == CONSTANTS.Deployed)
                            {
                                deployedModel.ModelType = deployResult.ModelVersion;
                                deployedModel.ProblemType = deployResult.ModelType;
                                deployedModel.Accuracy = deployResult.Accuracy;
                                string sampleInput = AddCascadeSampleInput(data);
                                string inputSample = !string.IsNullOrEmpty(sampleInput) ? sampleInput.Replace(System.Environment.NewLine, String.Empty).Replace("\r\n", string.Empty) : null;
                                deployModel.InputSample = inputSample;
                            }
                            else
                            {
                                //Take FirstModel input sample. 2nd model not deployed.
                                var firstdeployedModel = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
                                var model1filter = Builders<DeployModelsDto>.Filter.Eq(CONSTANTS.CorrelationId, deployedModel.CorrelationId);
                                var model1projection = Builders<DeployModelsDto>.Projection.Exclude(CONSTANTS.Id);
                                var model1Result = firstdeployedModel.Find(model1filter).Project<DeployModelsDto>(model1projection).FirstOrDefault();
                                if (model1Result != null && model1Result.Status == CONSTANTS.Deployed)
                                {
                                    bool DBEncryptionRequired2 = CommonUtility.EncryptDB(deployedModel.CorrelationId, appSettings);
                                    if (DBEncryptionRequired2)
                                    {
                                        if (model1Result.InputSample != null && model1Result.InputSample.ToString() != CONSTANTS.Null)
                                            deployModel.InputSample = _encryptionDecryption.Decrypt(model1Result.InputSample);
                                        else
                                            deployModel.InputSample = model1Result.InputSample;
                                    }
                                    else
                                        deployModel.InputSample = model1Result.InputSample;
                                }
                            }
                        }
                    }
                    else
                    {
                        string sampleInput = AddCascadeSampleInput(data);
                        string inputSample = !string.IsNullOrEmpty(sampleInput) ? sampleInput.Replace(System.Environment.NewLine, String.Empty).Replace("\r\n", string.Empty) : null;
                        deployModel.InputSample = inputSample;
                    }


                    string publishurl = appSettings.Value.publishURL;//ConfigurationManager.AppSettings["PublishURL"];
                    string Url = string.Format(publishurl + CONSTANTS.Zero, deployModel.CorrelationId);
                    deployModel.IsPrivate = false;
                    deployModel.IsModelTemplate = false;

                    DeployedModel deployedModel22 = new DeployedModel();
                    if (encryptDB)
                        deployModel.InputSample = _encryptionDecryption.Encrypt(deployModel.InputSample);
                    var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, deployModel.CorrelationId);
                    var builder = Builders<BsonDocument>.Update;
                    var update = builder.Set(CONSTANTS.Accuracy, deployedModel.Accuracy)
                        .Set(CONSTANTS.VDSLink, deployModel.VDSLink)
                        .Set(CONSTANTS.ModelURL, Url)
                        .Set(CONSTANTS.LinkedApps, arr2)
                        .Set(CONSTANTS.Frequency, deployModel.Frequency)
                        .Set(CONSTANTS.Status, CONSTANTS.Deployed)
                        .Set(CONSTANTS.WebServices, deployModel.WebServices)
                        .Set(CONSTANTS.DeployedDate, deployModel.DeployedDate)
                        .Set(CONSTANTS.IsPrivate, false)
                        .Set(CONSTANTS.IsModelTemplate, false)
                        .Set(CONSTANTS.ModelVersion, deployedModel.ModelType)
                        .Set(CONSTANTS.ModelType, deployedModel.ProblemType)
                        .Set(CONSTANTS.InputSample, deployModel.InputSample)
                        .Set(CONSTANTS.DeployedDate, DateTime.Now.ToString(CONSTANTS.DateHoursFormat));
                    var result = collection.UpdateMany(filter, update);
                }
            }
        }


        private string AddCascadeSampleInput(CascadeCollection result)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DeployModelServices), "AddCascadeSampleInput", "START", string.Empty, string.Empty, result.ClientUId, result.DeliveryConstructUID);
            string sampleInput = string.Empty;
            try
            {
                List<JObject> allModels = new List<JObject>();
                JObject mapping = new JObject();
                JArray listArray = new JArray();
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append("[");
                JObject singleObject = new JObject();
                if (result != null)
                {
                    string ddd = result.ModelList.ToString();

                    JObject data = JObject.Parse(ddd);
                    mapping = JObject.Parse(result.Mappings.ToString());
                    if (data != null)
                    {
                        List<string> corids = new List<string>();
                        foreach (var item in data.Children())
                        {
                            JProperty prop = item as JProperty;
                            if (prop != null)
                            {
                                DeployModelDetails model = JsonConvert.DeserializeObject<DeployModelDetails>(prop.Value.ToString());
                                var collection2 = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_DeployedModels);
                                var filter2 = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, model.CorrelationId);
                                var result2 = collection2.Find(filter2).Project<BsonDocument>(Builders<BsonDocument>.Projection.Include("InputSample").Exclude("_id")).ToList();
                                if (result2.Count > 0)
                                {
                                    if (!string.IsNullOrEmpty(result2[0]["InputSample"].ToString()) & result2[0]["InputSample"].ToString() != CONSTANTS.BsonNull)
                                    {
                                        allModels.Add(JObject.Parse(result2[0].ToString()));
                                        corids.Add(model.CorrelationId);
                                    }

                                }
                            }
                        }
                        if (allModels.Count > 0)
                        {
                            JArray firstModel = new JArray();
                            if (CommonUtility.EncryptDB(corids[0], appSettings))
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
                                for (int j = 0; j < mapping.Count - 1; j++)
                                {
                                    if (j == 0)
                                    {
                                        string modelName = string.Format("Model{0}", j + 1);
                                        JArray removeAraay1 = new JArray();
                                        if (CommonUtility.EncryptDB(corids[j], appSettings))
                                        {
                                            removeAraay1 = JArray.Parse(_encryptionDecryption.Decrypt(allModels[j]["InputSample"].ToString()));
                                        }
                                        else
                                        {
                                            removeAraay1 = JArray.Parse(allModels[j]["InputSample"].ToString());
                                        }
                                        JObject obj1 = JObject.Parse(removeAraay1[i].ToString());
                                        listJobject.Add(obj1);
                                        //Model 1 Start                                        
                                        JArray removeAraay = new JArray();
                                        if (i > 0)
                                        {
                                            if (CommonUtility.EncryptDB(corids[i], appSettings))
                                            {
                                                removeAraay = JArray.Parse(_encryptionDecryption.Decrypt(allModels[i]["InputSample"].ToString()));
                                            }
                                            else
                                            {
                                                removeAraay = JArray.Parse(allModels[i]["InputSample"].ToString());
                                            }
                                            //removeAraay = JArray.Parse(allModels[i]["InputSample"].ToString());
                                        }
                                        else
                                        {
                                            if (CommonUtility.EncryptDB(corids[i + 1], appSettings))
                                            {
                                                removeAraay = JArray.Parse(_encryptionDecryption.Decrypt(allModels[i + 1]["InputSample"].ToString()));
                                            }
                                            else
                                            {
                                                removeAraay = JArray.Parse(allModels[i + 1]["InputSample"].ToString());
                                            }
                                            //removeAraay = JArray.Parse(allModels[i + 1]["InputSample"].ToString());
                                        }

                                        JObject obj2 = JObject.Parse(removeAraay[i].ToString());
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
                                        string[] aa = mapping12.Target.Split("_");
                                        string probStr = aa[0] + "_" + "Proba1";
                                        if (obj2[probStr] != null)
                                        {
                                            obj2.Property(probStr).Remove();
                                        }
                                        listJobject.Add(obj2);
                                    }
                                    else
                                    {
                                        //ID Mapping
                                        JArray removeAraay = new JArray();
                                        if (CommonUtility.EncryptDB(corids[j + 1], appSettings))
                                        {
                                            removeAraay = JArray.Parse(_encryptionDecryption.Decrypt(allModels[j + 1]["InputSample"].ToString()));
                                        }
                                        else
                                        {
                                            removeAraay = JArray.Parse(allModels[j + 1]["InputSample"].ToString());
                                        }
                                        //JArray removeAraay = JArray.Parse(allModels[j + 1]["InputSample"].ToString());
                                        JObject modelIncrement = JObject.Parse(removeAraay[i].ToString());
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
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InstaModelService), nameof(AddCascadeSampleInput), ex.Message, ex, string.Empty, string.Empty, result.ClientUId, result.DeliveryConstructUID);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DeployModelServices), "AddCascadeSampleInput", "END", string.Empty, string.Empty, result.ClientUId, result.DeliveryConstructUID);
            return sampleInput;
        }
        public dynamic GetAppName(string ApplicationName)
        {
            //To get the Application ID
            dynamic AppDetails = null;
            var appCollection = _database.GetCollection<BsonDocument>("AppIntegration");
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
        private void IncludeModelToCascading(CascadeCollection data)
        {
            JObject jdata = JObject.Parse(data.ModelList.ToString());
            foreach (var model in jdata.Children())
            {
                JProperty propModel = model as JProperty;
                if (propModel != null)
                {
                    ModelInclusion modelInclusion = JsonConvert.DeserializeObject<ModelInclusion>(propModel.Value.ToString());
                    var deployCollection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
                    var filter = Builders<DeployModelsDto>.Filter.Eq(CONSTANTS.CorrelationId, modelInclusion.CorrelationId);
                    var projection = Builders<DeployModelsDto>.Projection.Exclude(CONSTANTS.Id);
                    var result = deployCollection.Find(filter).Project<DeployModelsDto>(projection).FirstOrDefault();
                    if (result != null)
                    {
                        if (result.CascadeIdList != null)
                        {
                            if (result.CascadeIdList.Count() > 0)
                            {
                                string[] cascadeId = new string[] { data.CascadedId };
                                if (!Array.Exists(result.CascadeIdList, elem => elem == data.CascadedId))
                                {
                                    string[] CascadeIdList = result.CascadeIdList.Concat(cascadeId).ToArray();
                                    var update = Builders<DeployModelsDto>.Update.Set(CONSTANTS.IsIncludedInCascade, true).Set("CascadeIdList", CascadeIdList);
                                    var updateResult = deployCollection.UpdateOne(filter, update);
                                }
                            }
                            else
                            {
                                string[] CascadeIdList = new string[] { data.CascadedId };
                                var update = Builders<DeployModelsDto>.Update.Set(CONSTANTS.IsIncludedInCascade, true).Set("CascadeIdList", CascadeIdList);
                                var updateResult = deployCollection.UpdateOne(filter, update);
                            }
                        }
                        else
                        {
                            string[] CascadeIdList = new string[] { data.CascadedId };
                            var update = Builders<DeployModelsDto>.Update.Set(CONSTANTS.IsIncludedInCascade, true).Set("CascadeIdList", CascadeIdList);
                            var updateResult = deployCollection.UpdateOne(filter, update);
                        }
                    }

                }
            }
        }
        private void IncludeModelToCascading(string[] removedModels, string cascadedId)
        {
            foreach (var model in removedModels)
            {
                var deployCollection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
                var filter = Builders<DeployModelsDto>.Filter.Eq(CONSTANTS.CorrelationId, model.Trim());
                var projection = Builders<DeployModelsDto>.Projection.Include("CascadeIdList").Exclude(CONSTANTS.Id);
                var result = deployCollection.Find(filter).Project<DeployModelsDto>(projection).FirstOrDefault();
                if (result != null)
                {
                    if (result.CascadeIdList != null)
                    {
                        if (result.CascadeIdList.Count() > 0)
                        {
                            string cascadeIdRemove = cascadedId;
                            if (result.CascadeIdList.Length < 2)
                            {
                                var update = Builders<DeployModelsDto>.Update.Set(CONSTANTS.IsIncludedInCascade, false);
                                var updateResult = deployCollection.UpdateOne(filter, update);
                            }
                            result.CascadeIdList = result.CascadeIdList.Where(val => val != cascadeIdRemove).ToArray();
                            var update3 = Builders<DeployModelsDto>.Update.Set("IsIncludedInCascade", true).Set("CascadeIdList", result.CascadeIdList);
                            var updateResult3 = deployCollection.UpdateOne(filter, update3);
                        }
                        else
                        {
                            var update = Builders<DeployModelsDto>.Update.Set(CONSTANTS.IsIncludedInCascade, false);
                            var updateResult = deployCollection.UpdateOne(filter, update);
                        }
                    }
                }
            }
        }
        private CascadeBsonDocument GetDataFromCollections(string correlationId)
        {
            CascadeBsonDocument cascadeBson = new CascadeBsonDocument();
            var collection2 = _database.GetCollection<BsonDocument>(CONSTANTS.PS_BusinessProblem);
            var projection2 = Builders<BsonDocument>.Projection.Include(CONSTANTS.TargetColumn).Include(CONSTANTS.TargetUniqueIdentifier).Exclude(CONSTANTS.Id);
            var filter2 = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            cascadeBson.BusinessProblemData = collection2.Find(filter2).Project<BsonDocument>(projection2).ToList();

            var dataCleanupCcollection = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataCleanup);
            var projection3 = Builders<BsonDocument>.Projection.Include(CONSTANTS.FeatureName).Include(CONSTANTS.NewFeatureName).Exclude(CONSTANTS.Id);
            var filter3 = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            cascadeBson.DataCleanupData = dataCleanupCcollection.Find(filter3).Project<BsonDocument>(projection3).ToList();

            var filteredCollection = _database.GetCollection<BsonDocument>(CONSTANTS.DataCleanUPFilteredData);
            var projection4 = Builders<BsonDocument>.Projection.Include(CONSTANTS.ColumnUniqueValues).Exclude(CONSTANTS.Id);
            var filter4 = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            cascadeBson.FilteredData = filteredCollection.Find(filter4).Project<BsonDocument>(projection4).ToList();
            return cascadeBson;
        }
        private Dictionary<string, string> GetColumnDataatypes(JObject datacleanup)
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            foreach (var item in datacleanup[CONSTANTS.FeatureName].Children())
            {
                JProperty prop = item as JProperty;
                if (prop != null)
                {
                    foreach (var datatype in datacleanup[CONSTANTS.FeatureName][prop.Name][CONSTANTS.Datatype].Children())
                    {
                        JProperty type = datatype as JProperty;
                        if (type != null)
                        {
                            if (type.Value.ToString() == CONSTANTS.True)
                                dict.Add(prop.Name, type.Name);
                        }
                    }
                }
            }
            return dict;
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

        private JObject GetMapppingDataDetails(string cascadedId)
        {
            JObject mapData = new JObject();
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAICascadedModels);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CascadedId, cascadedId);
            var result = collection.Find(filter).Project<BsonDocument>(Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id)).ToList();
            if (result.Count > 0)
            {
                mapData = JObject.Parse(result[0]["MappingData"].ToString());
            }
            return mapData;
        }
        private ValidationMapping ValidateMaping(JObject mapData, MappingAttributes attributes, int counter, string mapping)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CascadingService), nameof(ValidationMapping), "START", string.Empty, string.Empty, string.Empty, string.Empty);
            ValidationMapping mappingValidation = new ValidationMapping();
            string isValid = string.Empty;
            int idCounter = counter;
            try
            {
                DatatypeDict sourceType = JsonConvert.DeserializeObject<DatatypeDict>(mapData[CONSTANTS.Model + counter][CONSTANTS.InputColumns][attributes.Source].ToString());
                counter = counter + 1;
                //temp fix for critical defect
                if (mapData.ContainsKey(CONSTANTS.Model + counter))
                {
                    DatatypeDict targetType = JsonConvert.DeserializeObject<DatatypeDict>(mapData[CONSTANTS.Model + counter][CONSTANTS.InputColumns][attributes.Target].ToString());
                    if (sourceType != null & targetType != null)
                    {
                        if (idCounter == 1)
                        {
                            if (sourceType.Datatype == CONSTANTS.category || targetType.Datatype == CONSTANTS.category
                            || sourceType.Datatype == CONSTANTS.Text || targetType.Datatype == CONSTANTS.Text
                            || sourceType.Datatype == CONSTANTS.IdDatatype || targetType.Datatype == CONSTANTS.IdDatatype
                            || sourceType.Datatype == CONSTANTS.float64 || targetType.Datatype == CONSTANTS.float64
                            || sourceType.Datatype == CONSTANTS.Float || targetType.Datatype == CONSTANTS.Float
                            || sourceType.Datatype == CONSTANTS.int64 || targetType.Datatype == CONSTANTS.int64
                            || sourceType.Datatype == CONSTANTS.Integer || targetType.Datatype == CONSTANTS.Integer)
                            {
                                string[] sourceArray = null;
                                string[] targetArray = null;
                                if (sourceType.Datatype.Contains(CONSTANTS.float64) || sourceType.Datatype.Contains(CONSTANTS.int64) || sourceType.Datatype.Contains(CONSTANTS.Float) || sourceType.Datatype.Contains(CONSTANTS.Integer))
                                {
                                    double[] srcArray = sourceType.UniqueValues.ToObject<double[]>();
                                    sourceArray = srcArray.Select(x => x.ToString()).ToArray();
                                }
                                if (targetType.Datatype.Contains(CONSTANTS.float64) || targetType.Datatype.Contains(CONSTANTS.int64) || sourceType.Datatype.Contains(CONSTANTS.Float) || sourceType.Datatype.Contains(CONSTANTS.Integer))
                                {
                                    double[] trgArray = targetType.UniqueValues.ToObject<double[]>();
                                    targetArray = trgArray.Select(x => x.ToString()).ToArray();
                                }
                                //Both Source and Target attributes not numeric datatype than converting into string[] Array
                                #region Both Source and Target not numeric datatype than converting into string[] Array
                                if (sourceType.Datatype != CONSTANTS.int64 & sourceType.Datatype != CONSTANTS.float64)
                                {
                                    sourceArray = sourceType.UniqueValues.ToObject<string[]>();
                                }
                                if (targetType.Datatype != CONSTANTS.int64 & targetType.Datatype != CONSTANTS.float64)
                                {
                                    targetArray = targetType.UniqueValues.ToObject<string[]>();
                                }
                                #endregion
                                bool equal = sourceArray.All(elem => targetArray.Contains(elem));
                                if (equal)
                                {
                                    mappingValidation.IsValidate = true;
                                }
                                else
                                {
                                    mappingValidation.IsValidate = false;
                                    if (mapping == CONSTANTS.TargetMapping)
                                        mappingValidation.ErrorMessage = CONSTANTS.TargetValidation;
                                    else
                                        mappingValidation.ErrorMessage = CONSTANTS.IdValidation;
                                    return mappingValidation;
                                }
                            }
                        }
                        else
                        {
                            if (sourceType.Datatype == CONSTANTS.category || targetType.Datatype == CONSTANTS.category
                                || sourceType.Datatype == CONSTANTS.Text || targetType.Datatype == CONSTANTS.Text
                                || sourceType.Datatype == CONSTANTS.IdDatatype || targetType.Datatype == CONSTANTS.IdDatatype)
                            {
                                string[] sourceArray = null;
                                string[] targetArray = null;
                                if (sourceType.Datatype.Contains(CONSTANTS.float64) || sourceType.Datatype.Contains(CONSTANTS.int64))
                                {
                                    double[] srcArray = sourceType.UniqueValues.ToObject<double[]>();
                                    sourceArray = srcArray.Select(x => x.ToString()).ToArray();
                                }
                                if (targetType.Datatype.Contains(CONSTANTS.float64) || targetType.Datatype.Contains(CONSTANTS.int64))
                                {
                                    double[] trgArray = targetType.UniqueValues.ToObject<double[]>();
                                    targetArray = trgArray.Select(x => x.ToString()).ToArray();
                                }
                                //Both Source and Target attributes not numeric datatype than converting into string[] Array
                                #region Both Source and Target not numeric datatype than converting into string[] Array
                                if (sourceType.Datatype != CONSTANTS.int64 & sourceType.Datatype != CONSTANTS.float64)
                                {
                                    sourceArray = sourceType.UniqueValues.ToObject<string[]>();
                                }
                                if (targetType.Datatype != CONSTANTS.int64 & targetType.Datatype != CONSTANTS.float64)
                                {
                                    targetArray = targetType.UniqueValues.ToObject<string[]>();
                                }
                                #endregion
                                bool equal = sourceArray.All(elem => targetArray.Contains(elem));
                                if (equal)
                                {
                                    mappingValidation.IsValidate = true;
                                }
                                else
                                {
                                    mappingValidation.IsValidate = false;
                                    if (mapping == CONSTANTS.TargetMapping)
                                        mappingValidation.ErrorMessage = CONSTANTS.TargetValidation;
                                    else
                                        mappingValidation.ErrorMessage = CONSTANTS.IdValidation;
                                    return mappingValidation;
                                }
                            }
                            else
                            {
                                if (sourceType.Metric >= (targetType.Metric - 10) & sourceType.Metric <= (targetType.Metric + 10))
                                {
                                    mappingValidation.IsValidate = true;
                                }
                                else
                                {
                                    mappingValidation.IsValidate = false;
                                    if (mapping == CONSTANTS.TargetMapping)
                                        mappingValidation.ErrorMessage = CONSTANTS.TargetValidation;
                                    else
                                        mappingValidation.ErrorMessage = CONSTANTS.IdValidation;
                                    return mappingValidation;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                mappingValidation.IsException = true;
                mappingValidation.ErrorMessage = ex.Message;
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(CascadingService), nameof(ValidationMapping), ex.Message + CONSTANTS.ThreeDots + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CascadingService), nameof(ValidationMapping), "END", string.Empty, string.Empty, string.Empty, string.Empty);
            return mappingValidation;
        }
        private ValidationMapping ValidateCustomMaping(JObject mapData, MappingAttributes attributes, int counter, string mapping)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CascadingService), nameof(ValidateCustomMaping), "START", string.Empty, string.Empty, string.Empty, string.Empty);
            ValidationMapping mappingValidation = new ValidationMapping();
            string isValid = string.Empty;
            try
            {
                if (mapData.ToString() != "{}")
                {
                    DatatypeDict sourceType = JsonConvert.DeserializeObject<DatatypeDict>(mapData[CONSTANTS.Model + counter][CONSTANTS.InputColumns][attributes.Source].ToString());
                    counter = counter + 1;
                    if (mapData.ContainsKey(CONSTANTS.Model + counter))
                    {
                        DatatypeDict targetType = JsonConvert.DeserializeObject<DatatypeDict>(mapData[CONSTANTS.Model + counter][CONSTANTS.InputColumns][attributes.Target].ToString());
                        if (sourceType != null & targetType != null)
                        {
                            if (sourceType.Datatype == CONSTANTS.category || targetType.Datatype == CONSTANTS.category
                                || sourceType.Datatype == CONSTANTS.Text || targetType.Datatype == CONSTANTS.Text
                                || sourceType.Datatype == CONSTANTS.IdDatatype || targetType.Datatype == CONSTANTS.IdDatatype
                                || sourceType.Datatype == CONSTANTS.float64 || targetType.Datatype == CONSTANTS.float64
                                || sourceType.Datatype == CONSTANTS.int64 || targetType.Datatype == CONSTANTS.int64
                                || sourceType.Datatype == CONSTANTS.Float || targetType.Datatype == CONSTANTS.Float
                                || sourceType.Datatype == CONSTANTS.Integer || targetType.Datatype == CONSTANTS.Integer)
                            {
                                string[] sourceArray = null;
                                string[] targetArray = null;
                                if (sourceType.Datatype.Contains(CONSTANTS.float64) || sourceType.Datatype.Contains(CONSTANTS.int64))
                                {
                                    double[] srcArray = sourceType.UniqueValues.ToObject<double[]>();
                                    sourceArray = srcArray.Select(x => x.ToString()).ToArray();
                                }
                                if (targetType.Datatype.Contains(CONSTANTS.float64) || targetType.Datatype.Contains(CONSTANTS.int64))
                                {
                                    double[] trgArray = targetType.UniqueValues.ToObject<double[]>();
                                    targetArray = trgArray.Select(x => x.ToString()).ToArray();
                                }
                                //Both Source and Target attributes not numeric datatype than converting into string[] Array
                                #region Both Source and Target not numeric datatype than converting into string[] Array
                                if (sourceType.Datatype != CONSTANTS.int64 & sourceType.Datatype != CONSTANTS.float64)
                                {
                                    sourceArray = sourceType.UniqueValues.ToObject<string[]>();
                                }
                                if (targetType.Datatype != CONSTANTS.int64 & targetType.Datatype != CONSTANTS.float64)
                                {
                                    targetArray = targetType.UniqueValues.ToObject<string[]>();
                                }
                                #endregion
                                long matchedItems = sourceArray.Intersect(targetArray).ToList().Count();
                                long sourceArrayCount = sourceArray.Count();
                                double total = (double)matchedItems / sourceArrayCount;
                                double matchedAverage = total * 100;
                                if (matchedAverage >= appSettings.Value.CascadeTargetPercentage)
                                {
                                    mappingValidation.IsValidate = true;
                                    saveModel.CascadeTargetPercentage = matchedAverage;
                                }
                                else
                                {
                                    mappingValidation.IsValidate = false;
                                    saveModel.CascadeTargetPercentage = matchedAverage;
                                    if (mapping == CONSTANTS.TargetMapping)
                                        mappingValidation.ErrorMessage = CONSTANTS.TargetValidation;
                                    else
                                        mappingValidation.ErrorMessage = CONSTANTS.IdValidation;
                                    return mappingValidation;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                mappingValidation.IsException = true;
                mappingValidation.ErrorMessage = ex.Message;
                saveModel.IsException = true;
                saveModel.ErrorMessage = ex.Message + ex.StackTrace;
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(CascadingService), nameof(ValidateCustomMaping), ex.Message + CONSTANTS.ThreeDots + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CascadingService), nameof(ValidateCustomMaping), "END", string.Empty, string.Empty, string.Empty, string.Empty);
            return mappingValidation;
        }
        public CustomCascadeModel GetCustomCascadeModels(string clientUid, string dcUid, string userId, string category)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CascadingService), nameof(GetCustomCascadeModels), "START", string.Empty, string.Empty, clientUid, dcUid);
            try
            {
                string encrypteduser = userId;
                if (!string.IsNullOrEmpty(Convert.ToString(userId)))
                    encrypteduser = _encryptionDecryption.Encrypt(Convert.ToString(userId));
                string empty = null;
                _customCascade.DeliveryConstructUID = dcUid;
                _customCascade.ClientUid = clientUid;
                _customCascade.UserId = userId;
                if (category == "ADWaterfall")
                    category = "AD";
                if (category == "DevOps")
                    category = "Devops";
                _customCascade.Category = category;
                var collection = _database.GetCollection<DeployedModel>("SSAI_DeployedModels");
                var filterBuilder = Builders<DeployedModel>.Filter;
                //Public filter
                var publicFilter = Builders<DeployedModel>.Filter.Eq(CONSTANTS.DeliveryConstructUID, dcUid) &
                  Builders<DeployedModel>.Filter.Eq(CONSTANTS.ClientUId, clientUid) &
                  Builders<DeployedModel>.Filter.Eq(CONSTANTS.IsPrivate, false) &
                  Builders<DeployedModel>.Filter.Eq(CONSTANTS.Status, CONSTANTS.Deployed) &
                  Builders<DeployedModel>.Filter.Eq(CONSTANTS.Category, category) &
                  Builders<DeployedModel>.Filter.Eq(CONSTANTS.IsModelTemplate, false) &
                  Builders<DeployedModel>.Filter.Ne(CONSTANTS.IsCascadeModel, true) &
                  Builders<DeployedModel>.Filter.Eq(CONSTANTS.IsCascadingButton, true) &
                  Builders<DeployedModel>.Filter.Ne(CONSTANTS.IsFMModel, true) &
                  Builders<DeployedModel>.Filter.Eq(CONSTANTS.IsIncludedinCustomCascade, false) &
                  (Builders<DeployedModel>.Filter.Eq(CONSTANTS.FMCorrelationId, empty) | Builders<DeployedModel>.Filter.Eq(CONSTANTS.FMCorrelationId, CONSTANTS.BsonNull)) &
                  (Builders<DeployedModel>.Filter.Eq(CONSTANTS.ModelType, CONSTANTS.Classification) | Builders<DeployedModel>.Filter.Eq(CONSTANTS.ModelType, CONSTANTS.Regression) | Builders<DeployedModel>.Filter.Eq(CONSTANTS.ModelType, CONSTANTS.Multi_Class));

                //cascade template filter
                var cascadeModelTemplateFilter = Builders<DeployedModel>.Filter.Eq(CONSTANTS.DeliveryConstructUID, dcUid) &
                  Builders<DeployedModel>.Filter.Eq(CONSTANTS.ClientUId, clientUid) &
                  Builders<DeployedModel>.Filter.Eq(CONSTANTS.IsIncludedInCascade, true) &
                  Builders<DeployedModel>.Filter.Eq(CONSTANTS.Status, CONSTANTS.Deployed) &
                  Builders<DeployedModel>.Filter.Eq(CONSTANTS.Category, category) &
                  Builders<DeployedModel>.Filter.Eq(CONSTANTS.IsModelTemplate, true) &
                  Builders<DeployedModel>.Filter.Ne(CONSTANTS.IsCascadeModel, true) &
                  Builders<DeployedModel>.Filter.Eq(CONSTANTS.IsCascadingButton, true) &
                  Builders<DeployedModel>.Filter.Ne(CONSTANTS.IsFMModel, true) &
                  Builders<DeployedModel>.Filter.Eq(CONSTANTS.IsIncludedinCustomCascade, false) &
                  (Builders<DeployedModel>.Filter.Eq(CONSTANTS.FMCorrelationId, empty) | Builders<DeployedModel>.Filter.Eq(CONSTANTS.FMCorrelationId, CONSTANTS.BsonNull)) &
                  (Builders<DeployedModel>.Filter.Eq(CONSTANTS.ModelType, CONSTANTS.Classification) | Builders<DeployedModel>.Filter.Eq(CONSTANTS.ModelType, CONSTANTS.Regression) | Builders<DeployedModel>.Filter.Eq(CONSTANTS.ModelType, CONSTANTS.Multi_Class));

                var filter = filterBuilder.Eq(CONSTANTS.ClientUId, clientUid)
                    & filterBuilder.Eq(CONSTANTS.DeliveryConstructUID, dcUid)
                    & filterBuilder.Eq(CONSTANTS.Status, CONSTANTS.Deployed)
                    & filterBuilder.Ne(CONSTANTS.IsCascadeModel, true)
                    & filterBuilder.Eq(CONSTANTS.Category, category)
                    & filterBuilder.Ne(CONSTANTS.IsModelTemplate, true)
                    & filterBuilder.Eq(CONSTANTS.IsPrivate, true)
                    & filterBuilder.Eq(CONSTANTS.IsCascadingButton, true)
                    & (filterBuilder.Eq(CONSTANTS.CreatedByUser, userId) | filterBuilder.Eq(CONSTANTS.CreatedByUser, encrypteduser))
                    & filterBuilder.Ne(CONSTANTS.IsFMModel, true)
                    & filterBuilder.Eq(CONSTANTS.IsIncludedinCustomCascade, false)
                    & (filterBuilder.Eq(CONSTANTS.FMCorrelationId, empty) | filterBuilder.Eq(CONSTANTS.FMCorrelationId, CONSTANTS.BsonNull))
                    & (filterBuilder.Eq(CONSTANTS.ModelType, CONSTANTS.Classification) | filterBuilder.Eq(CONSTANTS.ModelType, CONSTANTS.Regression) | filterBuilder.Eq(CONSTANTS.ModelType, CONSTANTS.Multi_Class));

                var projection = Builders<DeployedModel>.Projection.Exclude(CONSTANTS.Id);
                var publicModelResult = collection.Find(publicFilter).Project<DeployedModel>(projection).ToList();
                var ModelResult = collection.Find(filter).Project<DeployedModel>(projection).ToList();
                var cascadetemplates = collection.Find(cascadeModelTemplateFilter).Project<DeployedModel>(projection).ToList();
                var modelsList = ModelResult.Concat(publicModelResult).Concat(cascadetemplates).ToList();

                if (modelsList.Count > 0)
                {
                    var requestCollection = _database.GetCollection<IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
                    var predictCacadeProjection = Builders<IngrainRequestQueue>.Projection.Exclude(CONSTANTS.Id);
                    var predictCacadeFilter = Builders<IngrainRequestQueue>.Filter.Eq(CONSTANTS.pageInfo, CONSTANTS.PredictCascade) & Builders<IngrainRequestQueue>.Filter.Eq(CONSTANTS.Status, CONSTANTS.C);
                    var completedPredictCacadeModels = requestCollection.Find(predictCacadeFilter).Project<IngrainRequestQueue>(predictCacadeProjection).ToList();
                    if (completedPredictCacadeModels.Count > 0)
                        modelsList.RemoveAll(x => completedPredictCacadeModels.All(y => x.CorrelationId != y.CorrelationId));
                    List<CustomCascadeModelDictionary> modelDictionary = new List<CustomCascadeModelDictionary>();
                    for (int i = 0; i < modelsList.Count; i++)
                    {
                        CustomCascadeModelDictionary cascadeModel = new CustomCascadeModelDictionary();
                        cascadeModel.CorrelationId = modelsList[i].CorrelationId;
                        cascadeModel.ModelName = modelsList[i].ModelName;
                        cascadeModel.ProblemType = modelsList[i].ModelType;
                        cascadeModel.Accuracy = Math.Round(modelsList[i].Accuracy, 2);
                        cascadeModel.ModelType = modelsList[i].ModelVersion;
                        cascadeModel.IsIncludedinCustomCascade = modelsList[i].IsIncludedinCustomCascade;
                        if (modelsList[i].CustomCascadeId != null & modelsList[i].CustomCascadeId != CONSTANTS.BsonNull & !string.IsNullOrEmpty(modelsList[i].CustomCascadeId))
                        {
                            cascadeModel.CustomCascadeId = modelsList[i].CustomCascadeId;
                            var cascadeCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAICascadedModels);
                            var cascadeFilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CascadedId, modelsList[i].CustomCascadeId);
                            var cascadeProjection = Builders<BsonDocument>.Projection.Include(CONSTANTS.ModelList).Exclude(CONSTANTS.Id);
                            var cascadeResult = cascadeCollection.Find(cascadeFilter).Project<BsonDocument>(cascadeProjection).ToList();
                            if (cascadeResult.Count > 0)
                            {
                                JObject data = JObject.Parse(cascadeResult[0].ToString());
                                cascadeModel.CascadeModelCount = data[CONSTANTS.ModelList].Children().Count();
                            }
                        }
                        if (modelsList[i].LinkedApps != null)
                        {
                            if (modelsList[i].LinkedApps.Length > 0)
                                cascadeModel.LinkedApps = modelsList[i].LinkedApps[0];
                        }
                        else
                            cascadeModel.LinkedApps = null;

                        cascadeModel.ApplicationID = modelsList[i].AppId;
                        var businessCollection = _database.GetCollection<BusinessProblem>(CONSTANTS.PSBusinessProblem);
                        var filter2 = Builders<BusinessProblem>.Filter.Eq(CONSTANTS.CorrelationId, modelsList[i].CorrelationId);
                        var proj = Builders<BusinessProblem>.Projection.Include(CONSTANTS.TargetColumn).Exclude(CONSTANTS.Id);
                        var res = businessCollection.Find(filter2).Project<BusinessProblem>(proj).ToList();
                        if (res.Count > 0)
                        {
                            cascadeModel.TargetColumn = res[0].TargetColumn;
                        }
                        modelDictionary.Add(cascadeModel);
                    }
                    _customCascade.Models = modelDictionary;
                }
            }
            catch (Exception ex)
            {
                _cascadeModel.IsException = true;
                _cascadeModel.ErrorMessage = ex.Message + CONSTANTS.ThreeDots + ex.StackTrace;
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(CascadingService), nameof(GetCustomCascadeModels), ex.Message + CONSTANTS.ThreeDots + ex.StackTrace, ex, string.Empty, string.Empty, clientUid, dcUid);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CascadingService), nameof(GetCustomCascadeModels), "END", string.Empty, string.Empty, clientUid, dcUid);
            return _customCascade;
        }
        public UpdateCascadeModelMapping SaveCustomCascadeModels(CascadeCollection data)
        {
            UpdateCascadeModelMapping saveModel = new UpdateCascadeModelMapping();
            saveModel.IsInserted = true;
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CascadingService), nameof(SaveCustomCascadeModels), "START", string.Empty, string.Empty, data.ClientUId, data.DeliveryConstructUID);
            try
            {
                data.Status = CONSTANTS.New;
                var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAICascadedModels);
                data.ModifiedByUser = data.CreatedByUser;
                saveModel.IsCustomModel = true;
                string targetCorid = string.Empty;
                string sourceCorid = string.Empty;
                if (string.IsNullOrEmpty(data.CascadedId))
                {
                    data.CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                    data.ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                    data.CascadedId = Guid.NewGuid().ToString();
                    saveModel.CascadedId = data.CascadedId;
                    bool DBEncryptionRequired = CascadedEncryptDB(data.CascadedId);
                    if (DBEncryptionRequired)
                    {
                        if (!string.IsNullOrEmpty(Convert.ToString(data.CreatedByUser)))
                            data.CreatedByUser = _encryptionDecryption.Encrypt(Convert.ToString(data.CreatedByUser));
                        if (!string.IsNullOrEmpty(Convert.ToString(data.ModifiedByUser)))
                            data.ModifiedByUser = _encryptionDecryption.Encrypt(Convert.ToString(data.ModifiedByUser));
                    }
                    //Validating mapping at server side
                    if (data.Mappings != null)
                    {
                        JObject modelList = JObject.Parse(data.ModelList.ToString());
                        if (modelList != null)
                        {
                            if (!string.IsNullOrEmpty(modelList.ToString()))
                            {
                                if (modelList.Children().Count() > 0)
                                {
                                    sourceCorid = modelList["Model1"]["CorrelationId"].ToString();
                                    targetCorid = modelList["Model2"]["CorrelationId"].ToString();
                                    _customMapping = GetCustomMappingData(sourceCorid, targetCorid, null, data.UniqIdName, data.UniqDatatype, data.TargetColumn);
                                    if (_customMapping.IsException)
                                    {
                                        saveModel.IsException = _customMapping.IsException;
                                        saveModel.IsValidate = _customMapping.IsException;
                                        saveModel.ErrorMessage = _customMapping.ErrorMessage;
                                        saveModel.IsInserted = false;
                                        return saveModel;
                                    }

                                    GenericCascadeCollection genericCascade = new GenericCascadeCollection
                                    {
                                        _id = Guid.NewGuid().ToString(),
                                        CascadedId = data.CascadedId,
                                        ModelName = data.ModelName,
                                        ClientUId = data.ClientUId,
                                        DeliveryConstructUID = data.DeliveryConstructUID,
                                        Category = data.Category,
                                        ModelList = data.ModelList,
                                        IsCustomModel = true,
                                        Mappings = data.Mappings,
                                        MappingData = _customMapping.MappingData,
                                        Status = data.Status,
                                        CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                                        CreatedByUser = data.CreatedByUser,
                                        ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                                        ModifiedByUser = data.CreatedByUser
                                    };
                                    var collection2 = _database.GetCollection<GenericCascadeCollection>(CONSTANTS.SSAICascadedModels);
                                    var jsonColumns = Newtonsoft.Json.JsonConvert.SerializeObject(genericCascade);
                                    var insertBsonColumns = BsonSerializer.Deserialize<GenericCascadeCollection>(jsonColumns);
                                    collection2.InsertOne(insertBsonColumns);
                                }
                            }
                        }
                        JObject mappingData = JObject.Parse(data.Mappings.ToString());
                        if (mappingData != null)
                        {
                            if (!string.IsNullOrEmpty(mappingData.ToString()))
                            {
                                ValidationMapping validationData = MapValidation(mappingData, data.CascadedId, true);
                                if (validationData.IsException || !validationData.IsValidate)
                                {
                                    saveModel.IsException = validationData.IsException;
                                    saveModel.IsValidate = validationData.IsException;
                                    saveModel.ErrorMessage = validationData.ErrorMessage;
                                    saveModel.IsInserted = false;
                                    return saveModel;
                                }
                            }
                        }
                    }
                    //Inserting into Deployedmodel Collection
                    InsertDeployedModels(data, true, true);
                    IncludeModelToCustomCascading(data);
                    saveModel.Status = CONSTANTS.New;
                }
                else
                {
                    bool DBEncryptionRequired = CascadedEncryptDB(data.CascadedId);
                    if (DBEncryptionRequired)
                    {
                        if (!string.IsNullOrEmpty(Convert.ToString(data.CreatedByUser)))
                            data.CreatedByUser = _encryptionDecryption.Encrypt(Convert.ToString(data.CreatedByUser));
                        if (!string.IsNullOrEmpty(Convert.ToString(data.ModifiedByUser)))
                            data.ModifiedByUser = _encryptionDecryption.Encrypt(Convert.ToString(data.ModifiedByUser));
                    }
                    saveModel.Status = CONSTANTS.New;
                    var filter4 = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CascadedId, data.CascadedId);
                    var projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id).Exclude(CONSTANTS.MappingData);
                    var result = collection.Find(filter4).Project<BsonDocument>(projection).ToList();
                    if (result.Count > 0)
                    {
                        if (data.ModelList != null & data.Mappings != null & data.ModelList.ToString() != CONSTANTS.undefined & data.Mappings.ToString() != CONSTANTS.undefined)
                        {
                            saveModel.CascadedId = data.CascadedId;
                            data.ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CascadedId, data.CascadedId);
                            BsonDocument modellist = new BsonDocument();
                            if (data.ModelList != null)
                            {
                                JObject data2 = JObject.Parse(data.ModelList.ToString());
                                if (data2 != null)
                                {
                                    int i = 1;
                                    foreach (var item in data2.Children())
                                    {
                                        JProperty prop = item as JProperty;
                                        if (string.IsNullOrEmpty(data2[CONSTANTS.Model + i][CONSTANTS.ProblemType].ToString()) || data2[CONSTANTS.Model + i][CONSTANTS.ProblemType].ToString() == CONSTANTS.BsonNull)
                                        {
                                            var collection2 = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
                                            var filter2 = Builders<DeployModelsDto>.Filter.Eq(CONSTANTS.CorrelationId, data2[CONSTANTS.Model + i][CONSTANTS.CorrelationId].ToString());
                                            var projection2 = Builders<DeployModelsDto>.Projection.Exclude(CONSTANTS.Id);
                                            var resultData = collection2.Find(filter2).Project<DeployModelsDto>(projection2).ToList();
                                            if (resultData.Count > 0)
                                            {
                                                if (resultData[0].Status == CONSTANTS.Deployed)
                                                {
                                                    data2[CONSTANTS.Model + i][CONSTANTS.ProblemType] = resultData[0].ModelType;
                                                    data2[CONSTANTS.Model + i][CONSTANTS.Accuracy] = resultData[0].Accuracy;
                                                    data2[CONSTANTS.Model + i][CONSTANTS.ModelType] = resultData[0].ModelVersion;
                                                    data2[CONSTANTS.Model + i][CONSTANTS.ApplicationID] = resultData[0].AppId;
                                                    if (resultData[0].LinkedApps != null)
                                                    {
                                                        if (resultData[0].LinkedApps.Length > 0)
                                                            data2[CONSTANTS.Model + i][CONSTANTS.LinkedApps] = resultData[0].LinkedApps[0];
                                                        else
                                                            data2[CONSTANTS.Model + i][CONSTANTS.LinkedApps] = null;
                                                    }
                                                    else
                                                        data2[CONSTANTS.Model + i][CONSTANTS.LinkedApps] = null;

                                                }
                                                targetCorid = data2[CONSTANTS.Model + i][CONSTANTS.CorrelationId].ToString();
                                            }
                                        }
                                        i++;
                                    }
                                }
                                modellist = BsonDocument.Parse(JsonConvert.SerializeObject(data2));
                            }
                            BsonDocument mappingList = new BsonDocument();
                            if (data.Mappings != null)
                            {
                                mappingList = BsonDocument.Parse(JsonConvert.SerializeObject(data.Mappings));
                            }

                            JObject cascadeData = JObject.Parse(data.ModelList.ToString());
                            if (cascadeData != null & !string.IsNullOrEmpty(cascadeData.ToString()))
                            {
                                List<CascadeModelsCollection> listModels = new List<CascadeModelsCollection>();
                                foreach (var item in cascadeData.Children())
                                {
                                    JProperty prop = item as JProperty;
                                    if (prop != null)
                                    {
                                        CascadeModelsCollection models = JsonConvert.DeserializeObject<CascadeModelsCollection>(prop.Value.ToString());
                                        listModels.Add(models);
                                    }
                                }
                                if (listModels.Count > 0)
                                {
                                    JObject mappingData2 = JObject.Parse(data.Mappings.ToString());
                                    string lastmodelname = string.Format("Model{0}", mappingData2.Children().Count());
                                    uniqIdname = mappingData2[lastmodelname][CONSTANTS.UniqueMapping][CONSTANTS.Target].ToString();
                                    targetColumn = mappingData2[lastmodelname][CONSTANTS.TargetMapping][CONSTANTS.Target].ToString();
                                    JObject modelsMapping = GetMapping(listModels, true, false);
                                    saveModel.Status = result[0][CONSTANTS.Status].ToString();
                                    //saveModel.MappingData = modelsMapping;
                                    var bsondDoc = BsonDocument.Parse(modelsMapping.ToString());
                                    var update = Builders<BsonDocument>.Update.Set("MappingData", bsondDoc);
                                    var updateResult = collection.UpdateOne(filter, update);
                                }
                            }

                            //Validating Mappings at server side
                            if (data.Mappings != null)
                            {
                                JObject mappingData = JObject.Parse(data.Mappings.ToString());
                                if (mappingData != null)
                                {
                                    if (!string.IsNullOrEmpty(mappingData.ToString()))
                                    {
                                        ValidationMapping validationData = MapValidation(mappingData, data.CascadedId, true);
                                        if (validationData.IsException || !validationData.IsValidate)
                                        {
                                            saveModel.IsException = validationData.IsException;
                                            saveModel.IsValidate = validationData.IsException;
                                            saveModel.ErrorMessage = validationData.ErrorMessage;
                                            saveModel.IsInserted = false;
                                            return saveModel;
                                        }
                                        saveModel.IsValidate = validationData.IsValidate;
                                        var builder2 = Builders<BsonDocument>.Update;
                                        if (data.isModelUpdated)
                                        {
                                            var update1 = builder2.Set(CONSTANTS.ModelList, modellist).Set(CONSTANTS.ModelName, data.ModelName).Set(CONSTANTS.Mappings, string.Empty).Set(CONSTANTS.MappingData, mappingList).Set(CONSTANTS.ModifiedOn, data.ModifiedOn);
                                            collection.UpdateMany(filter, update1);
                                        }
                                        else
                                        {
                                            var update2 = builder2.Set(CONSTANTS.ModelList, modellist).Set(CONSTANTS.Mappings, mappingList).Set(CONSTANTS.ModifiedOn, data.ModifiedOn);
                                            collection.UpdateMany(filter, update2);
                                        }
                                    }
                                }
                            }
                            IncludeModelToCustomCascading(data);
                            InsertDeployedModels(data, true, false);
                        }
                    }
                }

                //delete last model
                DeleteModelData(targetCorid);
            }
            catch (Exception ex)
            {
                saveModel.IsInserted = false;
                saveModel.IsException = true;
                saveModel.ErrorMessage = ex.Message + CONSTANTS.ThreeDots + ex.StackTrace;
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(CascadingService), nameof(SaveCustomCascadeModels), ex.Message + CONSTANTS.ThreeDots + ex.StackTrace, ex, string.Empty, string.Empty, data.ClientUId, data.DeliveryConstructUID);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CascadingService), nameof(SaveCustomCascadeModels), "END", string.Empty, string.Empty, data.ClientUId, data.DeliveryConstructUID);
            return saveModel;
        }

        private void DeleteModelData(string targetCorid)
        {
            var builder = Builders<BsonDocument>.Filter;
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, targetCorid);
            //DataCleanup Remove Start   
            var collectionDataCleanUp = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataCleanup);
            var collectionFilteredData = _database.GetCollection<BsonDocument>(CONSTANTS.DataCleanUPFilteredData);
            var collectionUseCase = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
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
            var useCaseFilter = builder.Eq(CONSTANTS.CorrelationId, targetCorid) & builder.Eq(CONSTANTS.pageInfo, CONSTANTS.DataCleanUp);
            var useCaseDataExist = collectionUseCase.Find(useCaseFilter).ToList();
            if (useCaseDataExist.Count > 0)
            {
                collectionUseCase.DeleteOne(useCaseFilter);
            }
            //DataCleanup Remove End
            //Data Transformation Remove Start
            var transformationCollection = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataProcessing);
            var transforamtionResult = transformationCollection.Find(filter).ToList();
            if (transforamtionResult.Count > 0)
            {
                transformationCollection.DeleteOne(filter);
            }
            var useCaseFilter2 = builder.Eq(CONSTANTS.CorrelationId, targetCorid) & builder.Eq(CONSTANTS.pageInfo, CONSTANTS.DataPreprocessing);
            var useCaseDataExist2 = collectionUseCase.Find(useCaseFilter).ToList();
            if (useCaseDataExist2.Count > 0)
            {
                collectionUseCase.DeleteOne(useCaseFilter2);
            }
            //Data Transformation Remove End 
            var featureCollection = _database.GetCollection<BsonDocument>(CONSTANTS.MEFeatureSelection);
            var featureResult = featureCollection.Find(filter).ToList();
            if (featureResult.Count > 0)
            {
                featureCollection.DeleteOne(filter);
            }

            var ingrainRequestCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
            var filterBuilder = Builders<BsonDocument>.Filter;
            var filterIngrainRequest = filterBuilder.Eq(CONSTANTS.CorrelationId, targetCorid) & (filterBuilder.Eq(CONSTANTS.pageInfo, CONSTANTS.DataCleanUp) | filterBuilder.Eq(CONSTANTS.pageInfo, CONSTANTS.DataPreprocessing));
            var result = ingrainRequestCollection.Find(filterIngrainRequest).ToList();
            if (result.Count > 0)
            {
                ingrainRequestCollection.DeleteMany(filter);
            }

            //Add Feature remove
            var collectionAddFeature = _database.GetCollection<BsonDocument>(CONSTANTS.DEAddNewFeature);
            var filterAddFeature = collectionAddFeature.Find(filter).ToList();
            if (filterAddFeature.Count > 0)
            {
                collectionAddFeature.DeleteOne(filter);
            }
        }
        public CustomMapping GetCascadeIdDetails(string sourceCorid, string targetCorid, string cascadeId, string UniqIdName, string UniqDatatype, string TargetColumn)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CascadingService), nameof(GetCascadeIdDetails), "START", string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                _customMapping = GetCustomMappingData(sourceCorid, targetCorid, cascadeId, UniqIdName, UniqDatatype, TargetColumn);
            }
            catch (Exception ex)
            {
                _customMapping.IsException = true;
                _customMapping.ErrorMessage = ex.Message + CONSTANTS.ThreeDots + ex.StackTrace;
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(CascadingService), nameof(GetCascadeIdDetails), ex.Message + CONSTANTS.ThreeDots + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CascadingService), nameof(GetCascadeIdDetails), "END", string.Empty, string.Empty, string.Empty, string.Empty);
            return _customMapping;
        }
        private CustomMapping GetCustomMappingData(string sourceCorid, string targetCorid, string cascadeId, string UniqIdName, string UniqDatatype, string TargetColumn)
        {
            List<string> list = new List<string>() { sourceCorid, targetCorid };
            BsonDocument cascadeResult = null;
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAICascadedModels);
            var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.ModelList).Include(CONSTANTS.Mappings).Include(CONSTANTS.ModelName).Include(CONSTANTS.CascadedId).Exclude(CONSTANTS.Id);
            _customMapping.MappingData = CustomCascadeMapping(list, UniqIdName, UniqDatatype, TargetColumn);
            _customMapping.CascadedId = cascadeId;
            _customMapping.SourceId = sourceCorid;
            _customMapping.TargetId = targetCorid;
            if (!string.IsNullOrEmpty(cascadeId) & cascadeId != CONSTANTS.Null & cascadeId != CONSTANTS.BsonNull & cascadeId != CONSTANTS.undefined)
            {
                var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CascadedId, cascadeId);
                cascadeResult = collection.Find(filter).Project<BsonDocument>(projection).FirstOrDefault();
                _customMapping.ModelName = cascadeResult[CONSTANTS.ModelName].ToString();
                JObject data = JObject.Parse(cascadeResult.ToString());
                if (data != null & !string.IsNullOrEmpty(data.ToString()))
                {
                    _customMapping.CascadeModelsCount = data[CONSTANTS.ModelList].Children().Count();
                    if (data.ContainsKey(CONSTANTS.IsCustomModel))
                        _customMapping.IsCustomModel = Convert.ToBoolean(data[CONSTANTS.IsCustomModel]);
                }
            }
            if (cascadeResult == null)
            {
                _customMapping.CascadeModel = null;
            }
            else
            {
                _customMapping.CascadeModel = JObject.Parse(cascadeResult.ToString());
            }
            return _customMapping;
        }
        private JObject CustomCascadeMapping(List<string> listModels, string UniqIdName, string UniqDatatype, string TargetColumn)
        {
            JObject modelsMapping = new JObject();
            try
            {
                int counter = 1;
                int source = 0;
                foreach (var model in listModels)
                {
                    if (source < 1)
                    {
                        var collectonData = GetDataFromCollections(model);
                        if (collectonData.BusinessProblemData.Count > 0 & collectonData.DataCleanupData.Count > 0 & collectonData.FilteredData.Count > 0)
                        {
                            source++;
                            bool DBEncryptionRequired = CommonUtility.EncryptDB(model, appSettings);
                            if (DBEncryptionRequired)
                            {
                                collectonData.DataCleanupData[0][CONSTANTS.FeatureName] = BsonDocument.Parse(_encryptionDecryption.Decrypt(collectonData.DataCleanupData[0][CONSTANTS.FeatureName].AsString));
                                if (collectonData.DataCleanupData[0].Contains(CONSTANTS.NewFeatureName))
                                {
                                    if (collectonData.DataCleanupData[0][CONSTANTS.NewFeatureName].ToString() != "{ }")
                                        collectonData.DataCleanupData[0][CONSTANTS.NewFeatureName] = BsonDocument.Parse(_encryptionDecryption.Decrypt(collectonData.DataCleanupData[0][CONSTANTS.NewFeatureName].AsString));
                                }
                                collectonData.FilteredData[0][CONSTANTS.ColumnUniqueValues] = BsonDocument.Parse(_encryptionDecryption.Decrypt(collectonData.FilteredData[0][CONSTANTS.ColumnUniqueValues].AsString));
                            }
                            JObject datas = JObject.Parse(collectonData.DataCleanupData[0].ToString());
                            JObject combinedFeatures = new JObject();
                            combinedFeatures = this.CombinedFeatures(datas);
                            if (combinedFeatures != null)
                                collectonData.DataCleanupData[0][CONSTANTS.FeatureName] = BsonDocument.Parse(combinedFeatures.ToString());
                            JObject uniqueData = JObject.Parse(collectonData.FilteredData[0].ToString());
                            JObject datacleanup = JObject.Parse(collectonData.DataCleanupData[0].ToString());
                            var dict = GetColumnDataatypes(datacleanup);
                            JObject InputColumns = new JObject();
                            foreach (var item in dict)
                            {
                                DatatypeDict datatype = new DatatypeDict();
                                datatype.Datatype = item.Value;
                                List<string> stringList = new List<string>();
                                List<double> numericList = new List<double>();
                                foreach (var value in uniqueData[CONSTANTS.ColumnUniqueValues][item.Key].Children())
                                {
                                    if (value != null)
                                    {
                                        if (item.Value == "float64" || item.Value == "int64")
                                        {
                                            numericList.Add(Math.Round(Convert.ToDouble(value)));
                                        }
                                        else
                                        {
                                            stringList.Add(Convert.ToString(value));
                                        }
                                    }
                                }
                                if (item.Value == "float64" || item.Value == "int64")
                                {
                                    datatype.UniqueValues = numericList.ToArray();
                                    datatype.Min = numericList.Min();
                                    datatype.Max = numericList.Max();
                                    datatype.Metric = numericList.Average();
                                }
                                else
                                    datatype.UniqueValues = stringList.ToArray();
                                Dictionary<string, DatatypeDict> keyValues = new Dictionary<string, DatatypeDict>();
                                keyValues.Add(item.Key, datatype);
                                JObject Current = JObject.Parse(JsonConvert.SerializeObject(keyValues));
                                InputColumns.Merge(Current, new Newtonsoft.Json.Linq.JsonMergeSettings
                                {
                                    MergeArrayHandling = Newtonsoft.Json.Linq.MergeArrayHandling.Union
                                });
                            }
                            JObject mainMapping = new JObject();
                            var collection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
                            var filter = Builders<DeployModelsDto>.Filter.Eq(CONSTANTS.CorrelationId, model);
                            var result = collection.Find(filter).Project<DeployModelsDto>(Builders<DeployModelsDto>.Projection.Exclude(CONSTANTS.Id)).FirstOrDefault();
                            if (result != null)
                            {
                                mainMapping[CONSTANTS.ModelName] = result.ModelName;
                                mainMapping[CONSTANTS.ProblemType] = result.ModelType;
                                mainMapping[CONSTANTS.ModelType] = result.ModelVersion;
                                mainMapping[CONSTANTS.CorrelationId] = result.CorrelationId;
                                mainMapping[CONSTANTS.ApplicationID] = result.AppId;
                                if (result.LinkedApps != null)
                                {
                                    if (result.LinkedApps.Length > 0)
                                        mainMapping[CONSTANTS.LinkedApps] = result.LinkedApps[0];
                                    else
                                        mainMapping[CONSTANTS.LinkedApps] = null;
                                }
                                else
                                    mainMapping[CONSTANTS.LinkedApps] = null;
                            }
                            mainMapping[CONSTANTS.TargetColumn] = collectonData.BusinessProblemData[0][CONSTANTS.TargetColumn].ToString();
                            mainMapping[CONSTANTS.TargetUniqueIdentifier] = collectonData.BusinessProblemData[0][CONSTANTS.TargetUniqueIdentifier].ToString();
                            mainMapping[CONSTANTS.InputColumns] = JObject.FromObject(InputColumns);
                            modelsMapping["Model1"] = JObject.FromObject(mainMapping);
                            counter++;
                        }
                    }
                    else
                    {
                        //check datacuration completed or not
                        var dataCleanupcollection = _database.GetCollection<BsonDocument>(CONSTANTS.DE_DataCleanup);
                        var dataCleanupfilter2 = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, listModels[1]);
                        var dataCleanupresult2 = dataCleanupcollection.Find(dataCleanupfilter2).Project<BsonDocument>(Builders<BsonDocument>.Projection.Include(CONSTANTS.CorrelationId).Exclude(CONSTANTS.Id)).FirstOrDefault();
                        if (dataCleanupresult2 != null)
                        {
                            var collectonData = GetDataFromCollections(model);
                            if (collectonData.BusinessProblemData.Count > 0 & collectonData.DataCleanupData.Count > 0 & collectonData.FilteredData.Count > 0)
                            {
                                source++;
                                bool DBEncryptionRequired = CommonUtility.EncryptDB(model, appSettings);
                                if (DBEncryptionRequired)
                                {
                                    collectonData.DataCleanupData[0][CONSTANTS.FeatureName] = BsonDocument.Parse(_encryptionDecryption.Decrypt(collectonData.DataCleanupData[0][CONSTANTS.FeatureName].AsString));
                                    if (collectonData.DataCleanupData[0].Contains(CONSTANTS.NewFeatureName))
                                    {
                                        if (collectonData.DataCleanupData[0][CONSTANTS.NewFeatureName].ToString() != "{ }")
                                            collectonData.DataCleanupData[0][CONSTANTS.NewFeatureName] = BsonDocument.Parse(_encryptionDecryption.Decrypt(collectonData.DataCleanupData[0][CONSTANTS.NewFeatureName].AsString));
                                    }
                                    collectonData.FilteredData[0][CONSTANTS.ColumnUniqueValues] = BsonDocument.Parse(_encryptionDecryption.Decrypt(collectonData.FilteredData[0][CONSTANTS.ColumnUniqueValues].AsString));
                                }
                                JObject datas = JObject.Parse(collectonData.DataCleanupData[0].ToString());
                                JObject combinedFeatures = new JObject();
                                combinedFeatures = this.CombinedFeatures(datas);
                                if (combinedFeatures != null)
                                    collectonData.DataCleanupData[0][CONSTANTS.FeatureName] = BsonDocument.Parse(combinedFeatures.ToString());
                                JObject uniqueData = JObject.Parse(collectonData.FilteredData[0].ToString());
                                JObject datacleanup = JObject.Parse(collectonData.DataCleanupData[0].ToString());
                                var dict = GetColumnDataatypes(datacleanup);
                                JObject InputColumns = new JObject();
                                foreach (var item in dict)
                                {
                                    DatatypeDict datatype = new DatatypeDict();
                                    datatype.Datatype = item.Value;
                                    List<string> stringList = new List<string>();
                                    List<double> numericList = new List<double>();
                                    foreach (var value in uniqueData[CONSTANTS.ColumnUniqueValues][item.Key].Children())
                                    {
                                        if (value != null)
                                        {
                                            if (item.Value == "float64" || item.Value == "int64")
                                            {
                                                numericList.Add(Math.Round(Convert.ToDouble(value)));
                                            }
                                            else
                                            {
                                                stringList.Add(Convert.ToString(value));
                                            }
                                        }
                                    }
                                    if (item.Value == "float64" || item.Value == "int64")
                                    {
                                        datatype.UniqueValues = numericList.ToArray();
                                        datatype.Min = numericList.Min();
                                        datatype.Max = numericList.Max();
                                        datatype.Metric = numericList.Average();
                                    }
                                    else
                                        datatype.UniqueValues = stringList.ToArray();
                                    Dictionary<string, DatatypeDict> keyValues = new Dictionary<string, DatatypeDict>();
                                    keyValues.Add(item.Key, datatype);
                                    JObject Current = JObject.Parse(JsonConvert.SerializeObject(keyValues));
                                    InputColumns.Merge(Current, new Newtonsoft.Json.Linq.JsonMergeSettings
                                    {
                                        MergeArrayHandling = Newtonsoft.Json.Linq.MergeArrayHandling.Union
                                    });
                                }
                                JObject mainMapping = new JObject();
                                var collection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
                                var filter = Builders<DeployModelsDto>.Filter.Eq(CONSTANTS.CorrelationId, model);
                                var result = collection.Find(filter).Project<DeployModelsDto>(Builders<DeployModelsDto>.Projection.Exclude(CONSTANTS.Id)).FirstOrDefault();
                                if (result != null)
                                {
                                    mainMapping[CONSTANTS.ModelName] = result.ModelName;
                                    mainMapping[CONSTANTS.ProblemType] = result.ModelType;
                                    mainMapping[CONSTANTS.ModelType] = result.ModelVersion;
                                    mainMapping[CONSTANTS.CorrelationId] = result.CorrelationId;
                                    mainMapping[CONSTANTS.ApplicationID] = result.AppId;
                                    if (result.LinkedApps != null)
                                    {
                                        if (result.LinkedApps.Length > 0)
                                            mainMapping[CONSTANTS.LinkedApps] = result.LinkedApps[0];
                                        else
                                            mainMapping[CONSTANTS.LinkedApps] = null;
                                    }
                                    else
                                        mainMapping[CONSTANTS.LinkedApps] = null;
                                }
                                mainMapping[CONSTANTS.TargetColumn] = collectonData.BusinessProblemData[0][CONSTANTS.TargetColumn].ToString();
                                mainMapping[CONSTANTS.TargetUniqueIdentifier] = collectonData.BusinessProblemData[0][CONSTANTS.TargetUniqueIdentifier].ToString();
                                mainMapping[CONSTANTS.InputColumns] = JObject.FromObject(InputColumns);
                                modelsMapping["Model2"] = JObject.FromObject(mainMapping);
                                counter++;
                            }
                        }
                        else
                        {
                            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.PSUseCaseDefinition);
                            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, listModels[1]);
                            var result = collection.Find(filter).Project<BsonDocument>(Builders<BsonDocument>.Projection.Include(CONSTANTS.UniqueValues).Exclude(CONSTANTS.Id)).FirstOrDefault();
                            if (result != null)
                            {
                                JObject data = JObject.Parse(result.ToString());
                                if (data != null)
                                {
                                    JObject InputColumns = new JObject();
                                    bool DBEncryptionRequired = CommonUtility.EncryptDB(listModels[1], appSettings);
                                    JObject uniqueData = new JObject();
                                    if (DBEncryptionRequired)
                                    {
                                        result[CONSTANTS.UniqueValues] = BsonDocument.Parse(_encryptionDecryption.Decrypt(result[CONSTANTS.UniqueValues].AsString));
                                        if (result.Contains(CONSTANTS.UniqueValues))
                                        {
                                            uniqueData = JObject.Parse(result[CONSTANTS.UniqueValues].ToString());
                                        }
                                        else
                                        {
                                            uniqueData = JObject.Parse(result.ToString());
                                        }
                                    }
                                    else
                                    {
                                        uniqueData = JObject.Parse(result[CONSTANTS.UniqueValues].ToString());
                                    }
                                    DatatypeDict datatype = new DatatypeDict();
                                    datatype.Datatype = UniqDatatype;
                                    List<string> stringList = new List<string>();
                                    List<double> numericList = new List<double>();
                                    if (uniqueData.ToString() != "{}")
                                    {
                                        foreach (var value in uniqueData[UniqIdName].Children())
                                        {
                                            if (value != null)
                                            {
                                                if (datatype.Datatype == "Float" || datatype.Datatype == "Integer")
                                                {
                                                    numericList.Add(Math.Round(Convert.ToDouble(value)));
                                                }
                                                else
                                                {
                                                    stringList.Add(Convert.ToString(value));
                                                }
                                            }
                                        }
                                        if (datatype.Datatype == "Float" || datatype.Datatype == "Integer")
                                        {
                                            datatype.UniqueValues = numericList.ToArray();
                                            datatype.Min = numericList.Min();
                                            datatype.Max = numericList.Max();
                                            datatype.Metric = numericList.Average();
                                        }
                                        else
                                            datatype.UniqueValues = stringList.ToArray();
                                        Dictionary<string, DatatypeDict> keyValues = new Dictionary<string, DatatypeDict>();
                                        keyValues.Add(UniqIdName, datatype);
                                        JObject Current = JObject.Parse(JsonConvert.SerializeObject(keyValues));
                                        InputColumns.Merge(Current, new Newtonsoft.Json.Linq.JsonMergeSettings
                                        {
                                            MergeArrayHandling = Newtonsoft.Json.Linq.MergeArrayHandling.Union
                                        });

                                        JObject mainMapping = new JObject();
                                        var collection2 = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
                                        var filter2 = Builders<DeployModelsDto>.Filter.Eq(CONSTANTS.CorrelationId, model);
                                        var result2 = collection2.Find(filter2).Project<DeployModelsDto>(Builders<DeployModelsDto>.Projection.Exclude(CONSTANTS.Id)).FirstOrDefault();
                                        if (result2 != null)
                                        {
                                            mainMapping[CONSTANTS.ModelName] = result2.ModelName;
                                            mainMapping[CONSTANTS.ProblemType] = result2.ModelType;
                                            mainMapping[CONSTANTS.ModelType] = result2.ModelVersion;
                                            mainMapping[CONSTANTS.CorrelationId] = result2.CorrelationId;
                                            mainMapping[CONSTANTS.ApplicationID] = result2.AppId;
                                            if (result2.LinkedApps != null)
                                            {
                                                if (result2.LinkedApps.Length > 0)
                                                    mainMapping[CONSTANTS.LinkedApps] = result2.LinkedApps[0];
                                                else
                                                    mainMapping[CONSTANTS.LinkedApps] = null;
                                            }
                                            else
                                                mainMapping[CONSTANTS.LinkedApps] = null;
                                        }
                                        mainMapping[CONSTANTS.TargetColumn] = TargetColumn;
                                        mainMapping[CONSTANTS.TargetUniqueIdentifier] = UniqIdName;
                                        mainMapping[CONSTANTS.InputColumns] = JObject.FromObject(InputColumns);
                                        modelsMapping["Model2"] = JObject.FromObject(mainMapping);
                                        _customMapping.Category = result2.Category;
                                        _customMapping.ClientUid = result2.ClientUId;
                                        _customMapping.DeliveryConstructUID = result2.DeliveryConstructUID;
                                    }
                                    else
                                    {
                                        _customMapping.IsException = true;
                                        _customMapping.ErrorMessage = "UniqueData is not avaiable for the model";
                                    }
                                }
                            }
                        }
                        //end                        
                    }
                }
            }
            catch (Exception ex)
            {
                _customMapping.IsException = true;
                _customMapping.ErrorMessage = ex.Message + CONSTANTS.ThreeDots + ex.StackTrace;
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(CascadingService), nameof(CustomCascadeMapping), ex.Message + CONSTANTS.ThreeDots + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
            return modelsMapping;
        }

        private ValidationMapping MapValidation(JObject mappingData, string cascadedId, bool isCustomModel)
        {
            ValidationMapping validationData = new ValidationMapping();
            for (int i = 0; i < mappingData.Children().Count(); i++)
            {
                string model = string.Format("Model{0}", i + 1);
                MappingAttributes attributes = JsonConvert.DeserializeObject<MappingAttributes>(mappingData[model]["UniqueMapping"].ToString());
                JObject mapData = GetMapppingDataDetails(cascadedId);
                if (mapData != null)
                {
                    if (isCustomModel)
                    {
                        string[] mappingList = new string[] { CONSTANTS.IDMapping };
                        foreach (var mapping in mappingList)
                        {
                            if (!string.IsNullOrEmpty(mapData.ToString()))
                            {
                                validationData = ValidateCustomMaping(mapData, attributes, i + 1, mapping);
                                if (!validationData.IsValidate)
                                {
                                    return validationData;
                                }
                            }
                        }
                    }
                    else
                    {
                        string[] mappingList = new string[] { CONSTANTS.IDMapping, CONSTANTS.TargetMapping };
                        foreach (var mapping in mappingList)
                        {
                            if (!string.IsNullOrEmpty(mapData.ToString()))
                            {
                                validationData = ValidateMaping(mapData, attributes, i + 1, mapping);
                            }
                        }
                    }
                }
            }
            return validationData;
        }
        public CustomModelViewDetails GetCustomCascadeDetails(string cascadeId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CascadingService), nameof(GetCustomCascadeDetails), "START", string.Empty, string.Empty, string.Empty, string.Empty);
            JObject ModelList = new JObject();
            customModelViewDetails.CascadeId = cascadeId;
            try
            {
                var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAICascadedModels);
                var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CascadedId, cascadeId);
                var projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.MappingData).Exclude(CONSTANTS.Id);
                var cascadeResult = collection.Find(filter).Project<BsonDocument>(projection).FirstOrDefault();
                if (cascadeResult != null)
                {
                    JObject data = JObject.Parse(cascadeResult.ToString());
                    customModelViewDetails.Category = cascadeResult[CONSTANTS.Category].ToString();
                    if (data.ContainsKey(CONSTANTS.IsCustomModel))
                    {
                        customModelViewDetails.IsCustomModel = Convert.ToBoolean(cascadeResult[CONSTANTS.IsCustomModel]);
                    }
                    if (data != null & !string.IsNullOrEmpty(data.ToString()))
                    {
                        List<string> corIds = new List<string>();
                        List<DeployModelDetails> deployModels = new List<DeployModelDetails>();
                        foreach (var corid in data[CONSTANTS.ModelList].Children())
                        {
                            if (corid != null)
                            {
                                JProperty prop = corid as JProperty;
                                if (prop != null)
                                {
                                    DeployModelDetails deployModel = JsonConvert.DeserializeObject<DeployModelDetails>(prop.Value.ToString());
                                    corIds.Add(deployModel.CorrelationId);
                                    deployModels.Add(deployModel);
                                }
                            }
                        }
                        if (corIds.Count > 0)
                        {
                            var psCollection = _database.GetCollection<BusinessProblem>(CONSTANTS.PSBusinessProblem);
                            var psFilter = Builders<BusinessProblem>.Filter.In(CONSTANTS.CorrelationId, corIds);
                            var psprojection = Builders<BusinessProblem>.Projection.Include(CONSTANTS.TargetUniqueIdentifier).Include(CONSTANTS.CorrelationId).Include(CONSTANTS.TargetColumn).Include(CONSTANTS.InputColumns).Exclude(CONSTANTS.Id);
                            var psResult = psCollection.Find(psFilter).Project<BusinessProblem>(psprojection).ToList();

                            if (psResult.Count > 0)
                            {
                                if (psResult.Count == corIds.Count)
                                {
                                    for (int i = 0; i < corIds.Count; i++)
                                    {
                                        CustomModelDetails modelDetails = new CustomModelDetails();
                                        modelDetails.ModelName = deployModels[i].ModelName;
                                        string[] cols = psResult.Find(item => item.CorrelationId == deployModels[i].CorrelationId).InputColumns;
                                        JObject Model = new JObject();
                                        int count = i + 1;
                                        if (count > 1)
                                        {
                                            List<string> colList = new List<string>();
                                            var res = Array.FindAll(cols, ele => ele.Contains(deployModels[i - 1].ModelName));
                                            if (res != null && res.Length > 0)
                                            {
                                                if (!res[0].Contains("Proba1"))
                                                    colList.Add(res[0]);
                                                cols = cols.Where(val => val != res[0]).ToArray();
                                            }
                                            foreach (var col in cols)
                                            {
                                                colList.Add(col);
                                            }
                                            modelDetails.InputClumns = colList.ToArray();
                                        }
                                        else
                                        {
                                            modelDetails.InputClumns = psResult.Find(item => item.CorrelationId == deployModels[i].CorrelationId).InputColumns;
                                        }
                                        Model["Model" + count] = JObject.FromObject(JObject.Parse(JsonConvert.SerializeObject(modelDetails)));
                                        ModelList.Merge(Model, new Newtonsoft.Json.Linq.JsonMergeSettings
                                        {
                                            MergeArrayHandling = Newtonsoft.Json.Linq.MergeArrayHandling.Union
                                        });
                                    }
                                }
                                else
                                {
                                    customModelViewDetails.IsException = true;
                                    customModelViewDetails.ErrorMessage = "Model input columns not saved.";
                                }
                            }
                            customModelViewDetails.ModelList = ModelList;

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                customModelViewDetails.IsException = true;
                customModelViewDetails.ErrorMessage = ex.Message + ex.StackTrace;
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(CascadingService), nameof(GetCustomCascadeDetails), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CascadingService), nameof(GetCustomCascadeDetails), "END", string.Empty, string.Empty, string.Empty, string.Empty);
            return customModelViewDetails;
        }
        private Dictionary<string, string> GetCustomDataatypes(string correlationId)
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.PSIngestedData);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var result = collection.Find(filter).Project<BsonDocument>(Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id)).FirstOrDefault();
            if (result != null)
            {
                JObject datatypes = JObject.Parse(result["DataType"].ToString());
                foreach (var item in datatypes.Children())
                {
                    JProperty prop = item as JProperty;
                    if (prop != null)
                    {
                        dict.Add(prop.Name.Trim(), prop.Value.ToString());
                    }
                }
            }
            return dict;
        }
        private void IncludeModelToCustomCascading(CascadeCollection data)
        {
            JObject jdata = JObject.Parse(data.ModelList.ToString());
            int count = 1;
            foreach (var model in jdata.Children())
            {
                if (count < jdata.Children().Count())
                {
                    JProperty propModel = model as JProperty;
                    if (propModel != null)
                    {
                        ModelInclusion modelInclusion = JsonConvert.DeserializeObject<ModelInclusion>(propModel.Value.ToString());
                        var deployCollection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
                        var filter = Builders<DeployModelsDto>.Filter.Eq(CONSTANTS.CorrelationId, modelInclusion.CorrelationId);
                        var projection = Builders<DeployModelsDto>.Projection.Exclude(CONSTANTS.Id);
                        var result = deployCollection.Find(filter).Project<DeployModelsDto>(projection).FirstOrDefault();
                        count++;
                        if (result != null)
                        {
                            if (result.CascadeIdList != null)
                            {
                                if (result.CascadeIdList.Count() > 0)
                                {
                                    var update = Builders<DeployModelsDto>.Update.Set(CONSTANTS.IsIncludedinCustomCascade, true);
                                    var updateResult = deployCollection.UpdateOne(filter, update);
                                    string[] cascadeId = new string[] { data.CascadedId };
                                    if (!Array.Exists(result.CascadeIdList, elem => elem == data.CascadedId))
                                    {
                                        string[] CascadeIdList = result.CascadeIdList.Concat(cascadeId).ToArray();
                                        var update2 = Builders<DeployModelsDto>.Update.Set(CONSTANTS.IsIncludedInCascade, true).Set("CascadeIdList", CascadeIdList);
                                        var updateResult2 = deployCollection.UpdateOne(filter, update2);
                                    }
                                }
                                else
                                {
                                    string[] CascadeIdList = new string[] { data.CascadedId };
                                    UpdateDefinition<DeployModelsDto> update = null;
                                    if (string.IsNullOrEmpty(result.CustomCascadeId) || result.CustomCascadeId == CONSTANTS.BsonNull)
                                        update = Builders<DeployModelsDto>.Update.Set(CONSTANTS.IsIncludedinCustomCascade, true).Set(CONSTANTS.CustomCascadeId, data.CascadedId).Set(CONSTANTS.IsIncludedInCascade, true).Set("CascadeIdList", CascadeIdList);
                                    else
                                        update = Builders<DeployModelsDto>.Update.Set(CONSTANTS.CustomCascadeId, data.CascadedId).Set(CONSTANTS.IsIncludedInCascade, true).Set("CascadeIdList", CascadeIdList);
                                    var updateResult = deployCollection.UpdateOne(filter, update);
                                }
                            }
                            else
                            {
                                string[] CascadeIdList = new string[] { data.CascadedId };
                                UpdateDefinition<DeployModelsDto> update = null;
                                if (string.IsNullOrEmpty(result.CustomCascadeId) || result.CustomCascadeId == CONSTANTS.BsonNull)
                                    update = Builders<DeployModelsDto>.Update.Set(CONSTANTS.IsIncludedinCustomCascade, true).Set(CONSTANTS.CustomCascadeId, data.CascadedId).Set(CONSTANTS.IsIncludedInCascade, true).Set("CascadeIdList", CascadeIdList);
                                else
                                    update = Builders<DeployModelsDto>.Update.Set(CONSTANTS.CustomCascadeId, data.CascadedId).Set(CONSTANTS.IsIncludedInCascade, true).Set("CascadeIdList", CascadeIdList);
                                var updateResult = deployCollection.UpdateOne(filter, update);
                            }
                        }
                    }
                }
                else
                {
                    string[] CascadeIdList = new string[] { data.CascadedId };
                    ModelInclusion modelInclusion = JsonConvert.DeserializeObject<ModelInclusion>(jdata["Model" + jdata.Children().Count()].ToString());
                    var deployCollection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
                    var filter = Builders<DeployModelsDto>.Filter.Eq(CONSTANTS.CorrelationId, modelInclusion.CorrelationId);
                    UpdateDefinition<DeployModelsDto> update2;
                    if (jdata.Children().Count() > 4)
                        update2 = Builders<DeployModelsDto>.Update.Set(CONSTANTS.IsIncludedInCascade, true).Set(CONSTANTS.IsIncludedinCustomCascade, true).Set(CONSTANTS.DataCurationName, CONSTANTS.Cascade).Set(CONSTANTS.CustomCascadeId, data.CascadedId).Set("CascadeIdList", CascadeIdList);
                    else
                        update2 = Builders<DeployModelsDto>.Update.Set(CONSTANTS.IsIncludedInCascade, true).Set(CONSTANTS.DataCurationName, CONSTANTS.Cascade).Set(CONSTANTS.CustomCascadeId, data.CascadedId).Set("CascadeIdList", CascadeIdList);
                    var updateResult2 = deployCollection.UpdateOne(filter, update2);

                    //BusinessProblem updating new columns
                    int j = jdata.Children().Count() - 1;
                    ModelInclusion modelInclusion2 = JsonConvert.DeserializeObject<ModelInclusion>(jdata["Model" + j].ToString());
                    List<string> corids = new List<string>() { modelInclusion2.CorrelationId, modelInclusion.CorrelationId };

                    var businessCollection = _database.GetCollection<BusinessProblem>(CONSTANTS.PSBusinessProblem);
                    var businessFilter = Builders<BusinessProblem>.Filter.In(CONSTANTS.CorrelationId, corids);
                    var result = businessCollection.Find(businessFilter).Project<BusinessProblem>(Builders<BusinessProblem>.Projection.Exclude(CONSTANTS.Id)).ToList();
                    if (result.Count > 0)
                    {
                        foreach (var item in result)
                        {
                            if (item.CorrelationId == modelInclusion.CorrelationId)
                            {
                                var inputCols = item.InputColumns.ToList();

                                if (!inputCols.Contains(modelInclusion2.ModelName + "_" + result.Find(item2 => item2.CorrelationId == modelInclusion2.CorrelationId).TargetColumn))
                                {
                                    if (modelInclusion2.ProblemType == CONSTANTS.Regression)
                                    {
                                        inputCols.Add(modelInclusion2.ModelName + "_" + result.Find(item2 => item2.CorrelationId == modelInclusion2.CorrelationId).TargetColumn);
                                    }
                                    else
                                    {
                                        inputCols.Add(modelInclusion2.ModelName + "_" + result.Find(item2 => item2.CorrelationId == modelInclusion2.CorrelationId).TargetColumn);
                                        inputCols.Add(modelInclusion2.ModelName + "_" + "Proba1");
                                    }
                                    var psFilter = Builders<BusinessProblem>.Filter.Eq(CONSTANTS.CorrelationId, modelInclusion.CorrelationId);
                                    var update = Builders<BusinessProblem>.Update.Set(CONSTANTS.InputColumns, inputCols.ToArray());
                                    var updateResult = businessCollection.UpdateOne(psFilter, update);
                                }
                                break;
                            }
                        }
                    }
                }
            }
        }

        private JObject GetMapping(List<CascadeModelsCollection> listModels, bool isCustom, bool fromMapModels)
        {
            int counter = 1;
            JObject modelsMapping = new JObject();
            bool isDataCleanupSuccess = false;
            if (isCustom)
            {
                var deploycollection = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataCleanup);
                var deployfilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, listModels[listModels.Count - 1].CorrelationId);
                var deployprojection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
                var deployResult = deploycollection.Find(deployfilter).Project<BsonDocument>(deployprojection).ToList();
                if (deployResult.Count > 0)
                {
                    isDataCleanupSuccess = true;
                }
                int source = listModels.Count - 1;
                for (int j = 0; j < listModels.Count; j++)
                {
                    CascadeModelsCollection cascadeModels = listModels[j];
                    if (j < source)
                    {
                        var collectonData = GetDataFromCollections(cascadeModels.CorrelationId);
                        if (collectonData.BusinessProblemData.Count > 0 & collectonData.DataCleanupData.Count > 0 & collectonData.FilteredData.Count > 0)
                        {
                            bool DBEncryptionRequired = CommonUtility.EncryptDB(cascadeModels.CorrelationId, appSettings);
                            if (DBEncryptionRequired)
                            {
                                collectonData.DataCleanupData[0][CONSTANTS.FeatureName] = BsonDocument.Parse(_encryptionDecryption.Decrypt(collectonData.DataCleanupData[0][CONSTANTS.FeatureName].AsString));
                                if (collectonData.DataCleanupData[0].Contains(CONSTANTS.NewFeatureName))
                                {
                                    if (collectonData.DataCleanupData[0][CONSTANTS.NewFeatureName].ToString() != "{ }")
                                        collectonData.DataCleanupData[0][CONSTANTS.NewFeatureName] = BsonDocument.Parse(_encryptionDecryption.Decrypt(collectonData.DataCleanupData[0][CONSTANTS.NewFeatureName].AsString));
                                }
                                collectonData.FilteredData[0][CONSTANTS.ColumnUniqueValues] = BsonDocument.Parse(_encryptionDecryption.Decrypt(collectonData.FilteredData[0][CONSTANTS.ColumnUniqueValues].AsString));
                            }
                            JObject datas = JObject.Parse(collectonData.DataCleanupData[0].ToString());
                            JObject combinedFeatures = new JObject();
                            combinedFeatures = this.CombinedFeatures(datas);
                            if (combinedFeatures != null)
                                collectonData.DataCleanupData[0][CONSTANTS.FeatureName] = BsonDocument.Parse(combinedFeatures.ToString());
                            JObject uniqueData = JObject.Parse(collectonData.FilteredData[0].ToString());
                            JObject datacleanup = JObject.Parse(collectonData.DataCleanupData[0].ToString());
                            var dict = GetColumnDataatypes(datacleanup);
                            JObject InputColumns = new JObject();
                            foreach (var item in dict)
                            {
                                DatatypeDict datatype = new DatatypeDict();
                                datatype.Datatype = item.Value;
                                List<string> stringList = new List<string>();
                                List<double> numericList = new List<double>();
                                foreach (var value in uniqueData[CONSTANTS.ColumnUniqueValues][item.Key].Children())
                                {
                                    if (value != null)
                                    {
                                        if (item.Value == "float64" || item.Value == "int64")
                                        {
                                            numericList.Add(Math.Round(Convert.ToDouble(value), 2));
                                        }
                                        else
                                        {
                                            stringList.Add(Convert.ToString(value));
                                        }
                                    }
                                }
                                if (item.Value == "float64" || item.Value == "int64")
                                {
                                    datatype.UniqueValues = numericList.ToArray();
                                    datatype.Min = numericList.Min();
                                    datatype.Max = numericList.Max();
                                    datatype.Metric = numericList.Average();
                                }
                                else
                                    datatype.UniqueValues = stringList.ToArray();
                                Dictionary<string, DatatypeDict> keyValues = new Dictionary<string, DatatypeDict>();
                                keyValues.Add(item.Key, datatype);
                                JObject Current = JObject.Parse(JsonConvert.SerializeObject(keyValues));
                                InputColumns.Merge(Current, new Newtonsoft.Json.Linq.JsonMergeSettings
                                {
                                    MergeArrayHandling = Newtonsoft.Json.Linq.MergeArrayHandling.Union
                                });
                            }
                            JObject mainMapping = new JObject();
                            mainMapping[CONSTANTS.ModelName] = cascadeModels.ModelName;
                            mainMapping[CONSTANTS.ProblemType] = cascadeModels.ProblemType;
                            mainMapping[CONSTANTS.ModelType] = cascadeModels.ModelType;
                            mainMapping[CONSTANTS.CorrelationId] = cascadeModels.CorrelationId;
                            mainMapping[CONSTANTS.TargetColumn] = collectonData.BusinessProblemData[0][CONSTANTS.TargetColumn].ToString();
                            mainMapping[CONSTANTS.TargetUniqueIdentifier] = collectonData.BusinessProblemData[0][CONSTANTS.TargetUniqueIdentifier].ToString();
                            mainMapping[CONSTANTS.InputColumns] = JObject.FromObject(InputColumns);
                            modelsMapping[CONSTANTS.Model + counter] = JObject.FromObject(mainMapping);
                            counter++;
                        }
                    }
                    else
                    {
                        if (isDataCleanupSuccess)
                        {
                            var collectonData = GetDataFromCollections(cascadeModels.CorrelationId);
                            if (collectonData.BusinessProblemData.Count > 0 & collectonData.DataCleanupData.Count > 0 & collectonData.FilteredData.Count > 0)
                            {
                                bool DBEncryptionRequired = CommonUtility.EncryptDB(cascadeModels.CorrelationId, appSettings);
                                if (DBEncryptionRequired)
                                {
                                    collectonData.DataCleanupData[0][CONSTANTS.FeatureName] = BsonDocument.Parse(_encryptionDecryption.Decrypt(collectonData.DataCleanupData[0][CONSTANTS.FeatureName].AsString));
                                    if (collectonData.DataCleanupData[0].Contains(CONSTANTS.NewFeatureName))
                                    {
                                        if (collectonData.DataCleanupData[0][CONSTANTS.NewFeatureName].ToString() != "{ }")
                                            collectonData.DataCleanupData[0][CONSTANTS.NewFeatureName] = BsonDocument.Parse(_encryptionDecryption.Decrypt(collectonData.DataCleanupData[0][CONSTANTS.NewFeatureName].AsString));
                                    }
                                    collectonData.FilteredData[0][CONSTANTS.ColumnUniqueValues] = BsonDocument.Parse(_encryptionDecryption.Decrypt(collectonData.FilteredData[0][CONSTANTS.ColumnUniqueValues].AsString));
                                }
                                JObject datas = JObject.Parse(collectonData.DataCleanupData[0].ToString());
                                JObject combinedFeatures = new JObject();
                                combinedFeatures = this.CombinedFeatures(datas);
                                if (combinedFeatures != null)
                                    collectonData.DataCleanupData[0][CONSTANTS.FeatureName] = BsonDocument.Parse(combinedFeatures.ToString());
                                JObject uniqueData = JObject.Parse(collectonData.FilteredData[0].ToString());
                                JObject datacleanup = JObject.Parse(collectonData.DataCleanupData[0].ToString());
                                var dict = GetColumnDataatypes(datacleanup);
                                JObject InputColumns = new JObject();
                                foreach (var item in dict)
                                {
                                    DatatypeDict datatype = new DatatypeDict();
                                    datatype.Datatype = item.Value;
                                    List<string> stringList = new List<string>();
                                    List<double> numericList = new List<double>();
                                    foreach (var value in uniqueData[CONSTANTS.ColumnUniqueValues][item.Key].Children())
                                    {
                                        if (value != null)
                                        {
                                            if (item.Value == "float64" || item.Value == "int64")
                                            {
                                                numericList.Add(Math.Round(Convert.ToDouble(value), 2));
                                            }
                                            else
                                            {
                                                stringList.Add(Convert.ToString(value));
                                            }
                                        }
                                    }
                                    if (item.Value == "float64" || item.Value == "int64")
                                    {
                                        datatype.UniqueValues = numericList.ToArray();
                                        datatype.Min = numericList.Min();
                                        datatype.Max = numericList.Max();
                                        datatype.Metric = numericList.Average();
                                    }
                                    else
                                        datatype.UniqueValues = stringList.ToArray();
                                    Dictionary<string, DatatypeDict> keyValues = new Dictionary<string, DatatypeDict>();
                                    keyValues.Add(item.Key, datatype);
                                    JObject Current = JObject.Parse(JsonConvert.SerializeObject(keyValues));
                                    InputColumns.Merge(Current, new Newtonsoft.Json.Linq.JsonMergeSettings
                                    {
                                        MergeArrayHandling = Newtonsoft.Json.Linq.MergeArrayHandling.Union
                                    });
                                }
                                JObject mainMapping = new JObject();
                                mainMapping[CONSTANTS.ModelName] = cascadeModels.ModelName;
                                mainMapping[CONSTANTS.ProblemType] = cascadeModels.ProblemType;
                                mainMapping[CONSTANTS.ModelType] = cascadeModels.ModelType;
                                mainMapping[CONSTANTS.CorrelationId] = cascadeModels.CorrelationId;
                                mainMapping[CONSTANTS.TargetColumn] = collectonData.BusinessProblemData[0][CONSTANTS.TargetColumn].ToString();
                                mainMapping[CONSTANTS.TargetUniqueIdentifier] = collectonData.BusinessProblemData[0][CONSTANTS.TargetUniqueIdentifier].ToString();
                                mainMapping[CONSTANTS.InputColumns] = JObject.FromObject(InputColumns);
                                modelsMapping[CONSTANTS.Model + counter] = JObject.FromObject(mainMapping);
                                counter++;
                            }
                        }
                        else
                        {
                            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.PSUseCaseDefinition);
                            var businessCollection = _database.GetCollection<BsonDocument>(CONSTANTS.PSBusinessProblem);
                            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, cascadeModels.CorrelationId);
                            var result = collection.Find(filter).Project<BsonDocument>(Builders<BsonDocument>.Projection.Include(CONSTANTS.UniqueValues).Exclude(CONSTANTS.Id)).FirstOrDefault();
                            var businessResult = businessCollection.Find(filter).Project<BsonDocument>(Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id)).FirstOrDefault();

                            if (businessResult != null)
                            {
                                string uniqid = businessResult[CONSTANTS.TargetUniqueIdentifier].ToString();
                                string uniqidDatatype = string.Empty;
                                if (result != null)
                                {
                                    JObject data = JObject.Parse(result.ToString());
                                    if (data != null)
                                    {
                                        var dict = GetCustomDataatypes(cascadeModels.CorrelationId);
                                        if (dict.Count > 0)
                                        {
                                            JObject InputColumns = new JObject();
                                            bool DBEncryptionRequired = CommonUtility.EncryptDB(cascadeModels.CorrelationId, appSettings);
                                            JObject uniqueData = new JObject();
                                            if (DBEncryptionRequired)
                                            {
                                                result = BsonDocument.Parse(_encryptionDecryption.Decrypt(result[CONSTANTS.UniqueValues].AsString));
                                                uniqueData = JObject.Parse(result.ToString());
                                            }
                                            else
                                            {
                                                uniqueData = JObject.Parse(result[CONSTANTS.UniqueValues].ToString());
                                            }
                                            //foreach (var item in dict)
                                            var type = dict.FirstOrDefault(x => x.Key == uniqid).Value;
                                            DatatypeDict datatype = new DatatypeDict();
                                            datatype.Datatype = type;
                                            List<string> stringList = new List<string>();
                                            List<double> numericList = new List<double>();
                                            if (fromMapModels)
                                            {
                                                int i = 0;
                                                foreach (var item in dict)
                                                {
                                                    if (i > 0)
                                                    {
                                                        if (!uniqueData.ContainsKey(item.Key))
                                                        {
                                                            _customMapping.ErrorMessage = "Datacuration not completed for the model-" + cascadeModels.ModelName;
                                                            _customMapping.IsException = true;
                                                            return modelsMapping;
                                                        }
                                                        foreach (var value in uniqueData[item.Key].Children())
                                                        {
                                                            if (value != null)
                                                            {

                                                                if (item.Value == "Float" || item.Value == "float64" || item.Value == "Integer" || item.Value == "int64")
                                                                {
                                                                    numericList.Add(Math.Round(Convert.ToDouble(value)));
                                                                }
                                                                else
                                                                {
                                                                    stringList.Add(Convert.ToString(value));
                                                                }
                                                            }
                                                        }
                                                        if (item.Value == "Float" || item.Value == "float64" || item.Value == "Integer" || item.Value == "int64")
                                                        {
                                                            datatype.UniqueValues = numericList.ToArray();
                                                            datatype.Min = numericList.Min();
                                                            datatype.Max = numericList.Max();
                                                            datatype.Metric = numericList.Average();
                                                        }
                                                        else
                                                            datatype.UniqueValues = stringList.ToArray();
                                                        Dictionary<string, DatatypeDict> keyValues = new Dictionary<string, DatatypeDict>();
                                                        keyValues.Add(item.Key, datatype);
                                                        JObject Current = JObject.Parse(JsonConvert.SerializeObject(keyValues));
                                                        InputColumns.Merge(Current, new Newtonsoft.Json.Linq.JsonMergeSettings
                                                        {
                                                            MergeArrayHandling = Newtonsoft.Json.Linq.MergeArrayHandling.Union
                                                        });
                                                    }
                                                    else
                                                    {
                                                        if (modelsMapping != null)
                                                        {
                                                            if (!uniqueData.ContainsKey(item.Key))
                                                            {
                                                                _customMapping.ErrorMessage = "Datacuration not comleted for the model-" + cascadeModels.ModelName;
                                                                _customMapping.IsException = true;
                                                                return modelsMapping;
                                                            }
                                                            foreach (var value in uniqueData[item.Key].Children())
                                                            {
                                                                if (value != null)
                                                                {
                                                                    if (item.Value == "Float" || item.Value == "float64" || item.Value == "Integer" || item.Value == "int64")
                                                                    {
                                                                        numericList.Add(Math.Round(Convert.ToDouble(value), 2));
                                                                    }
                                                                    else
                                                                    {
                                                                        stringList.Add(Convert.ToString(value));
                                                                    }
                                                                }
                                                            }
                                                            if (item.Value == "Float" || item.Value == "float64" || item.Value == "Integer" || item.Value == "int64")
                                                            {
                                                                datatype.UniqueValues = numericList.ToArray();
                                                                datatype.Min = numericList.Min();
                                                                datatype.Max = numericList.Max();
                                                                datatype.Metric = numericList.Average();
                                                            }
                                                            else
                                                                datatype.UniqueValues = stringList.ToArray();
                                                            Dictionary<string, DatatypeDict> keyValues = new Dictionary<string, DatatypeDict>();
                                                            keyValues.Add(item.Key, datatype);
                                                            JObject Current = JObject.Parse(JsonConvert.SerializeObject(keyValues));
                                                            InputColumns.Merge(Current, new Newtonsoft.Json.Linq.JsonMergeSettings
                                                            {
                                                                MergeArrayHandling = Newtonsoft.Json.Linq.MergeArrayHandling.Union
                                                            });
                                                            string lastmodel = string.Format("Model{0}", j);
                                                            string lastTargetCol = modelsMapping[lastmodel][CONSTANTS.TargetColumn].ToString();
                                                            string lastModelName = modelsMapping[lastmodel][CONSTANTS.ModelName].ToString();
                                                            DatatypeDict datatype2 = JsonConvert.DeserializeObject<DatatypeDict>(modelsMapping[lastmodel][CONSTANTS.InputColumns][lastTargetCol].ToString());
                                                            Dictionary<string, DatatypeDict> keyValues2 = new Dictionary<string, DatatypeDict>();
                                                            string colName = lastModelName + "_" + lastTargetCol;
                                                            keyValues2.Add(colName, datatype2);
                                                            JObject Current2 = JObject.Parse(JsonConvert.SerializeObject(keyValues2));
                                                            InputColumns.Merge(Current2, new Newtonsoft.Json.Linq.JsonMergeSettings
                                                            {
                                                                MergeArrayHandling = Newtonsoft.Json.Linq.MergeArrayHandling.Union
                                                            });
                                                        }
                                                    }
                                                    i++;
                                                }
                                            }
                                            else
                                            {
                                                foreach (var value in uniqueData[uniqid].Children())
                                                {
                                                    if (value != null)
                                                    {
                                                        if (type == "Float" || type == "Integer")
                                                        {
                                                            numericList.Add(Math.Round(Convert.ToDouble(value), 2));
                                                        }
                                                        else
                                                        {
                                                            stringList.Add(Convert.ToString(value));
                                                        }
                                                    }
                                                }
                                                if (type == "Float" || type == "Integer")
                                                {
                                                    datatype.UniqueValues = numericList.ToArray();
                                                    datatype.Min = numericList.Min();
                                                    datatype.Max = numericList.Max();
                                                    datatype.Metric = numericList.Average();
                                                }
                                                else
                                                    datatype.UniqueValues = stringList.ToArray();
                                                Dictionary<string, DatatypeDict> keyValues = new Dictionary<string, DatatypeDict>();
                                                keyValues.Add(uniqid, datatype);
                                                JObject Current = JObject.Parse(JsonConvert.SerializeObject(keyValues));
                                                InputColumns.Merge(Current, new Newtonsoft.Json.Linq.JsonMergeSettings
                                                {
                                                    MergeArrayHandling = Newtonsoft.Json.Linq.MergeArrayHandling.Union
                                                });
                                            }

                                            JObject mainMapping = new JObject();
                                            var collection2 = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
                                            var filter2 = Builders<DeployModelsDto>.Filter.Eq(CONSTANTS.CorrelationId, cascadeModels.CorrelationId);
                                            var result2 = collection2.Find(filter2).Project<DeployModelsDto>(Builders<DeployModelsDto>.Projection.Exclude(CONSTANTS.Id)).FirstOrDefault();
                                            if (result2 != null)
                                            {
                                                mainMapping[CONSTANTS.ModelName] = result2.ModelName;
                                                mainMapping[CONSTANTS.ProblemType] = result2.ModelType;
                                                mainMapping[CONSTANTS.ModelType] = result2.ModelVersion;
                                                mainMapping[CONSTANTS.CorrelationId] = result2.CorrelationId;
                                                mainMapping[CONSTANTS.ApplicationID] = result2.AppId;
                                                if (result2.LinkedApps != null)
                                                {
                                                    if (result2.LinkedApps.Length > 0)
                                                        mainMapping[CONSTANTS.LinkedApps] = result2.LinkedApps[0];
                                                    else
                                                        mainMapping[CONSTANTS.LinkedApps] = null;
                                                }
                                                else
                                                    mainMapping[CONSTANTS.LinkedApps] = null;
                                                mainMapping[CONSTANTS.TargetColumn] = businessResult[CONSTANTS.TargetColumn].ToString();
                                                mainMapping[CONSTANTS.TargetUniqueIdentifier] = uniqid;
                                                mainMapping[CONSTANTS.InputColumns] = JObject.FromObject(InputColumns);
                                                //modelsMapping["Model2"] = JObject.FromObject(mainMapping);
                                                modelsMapping[CONSTANTS.Model + counter] = JObject.FromObject(mainMapping);
                                                _customMapping.Category = result2.Category;
                                                _customMapping.ClientUid = result2.ClientUId;
                                                _customMapping.DeliveryConstructUID = result2.DeliveryConstructUID;
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (result != null)
                                {
                                    JObject data = JObject.Parse(result.ToString());
                                    var dict = GetCustomDataatypes(cascadeModels.CorrelationId);
                                    var type = dict.FirstOrDefault(x => x.Key == uniqIdname).Value;
                                    if (data != null)
                                    {
                                        JObject InputColumns = new JObject();
                                        bool DBEncryptionRequired = CommonUtility.EncryptDB(cascadeModels.CorrelationId, appSettings);
                                        JObject uniqueData = new JObject();
                                        if (DBEncryptionRequired)
                                        {
                                            result[CONSTANTS.UniqueValues] = BsonDocument.Parse(_encryptionDecryption.Decrypt(result[CONSTANTS.UniqueValues].AsString));
                                            if (result.Contains(CONSTANTS.UniqueValues))
                                            {
                                                uniqueData = JObject.Parse(result[CONSTANTS.UniqueValues].ToString());
                                            }
                                            else
                                            {
                                                uniqueData = JObject.Parse(result.ToString());
                                            }
                                        }
                                        else
                                        {
                                            uniqueData = JObject.Parse(result[CONSTANTS.UniqueValues].ToString());
                                        }
                                        DatatypeDict datatype = new DatatypeDict();
                                        datatype.Datatype = type;
                                        List<string> stringList = new List<string>();
                                        List<double> numericList = new List<double>();
                                        if (uniqueData.ToString() != "{}")
                                        {
                                            foreach (var value in uniqueData[uniqIdname].Children())
                                            {
                                                if (value != null)
                                                {
                                                    if (datatype.Datatype == "Float" || datatype.Datatype == "Integer")
                                                    {
                                                        numericList.Add(Math.Round(Convert.ToDouble(value), 2));
                                                    }
                                                    else
                                                    {
                                                        stringList.Add(Convert.ToString(value));
                                                    }
                                                }
                                            }
                                            if (datatype.Datatype == "Float" || datatype.Datatype == "Integer")
                                            {
                                                datatype.UniqueValues = numericList.ToArray();
                                                datatype.Min = numericList.Min();
                                                datatype.Max = numericList.Max();
                                                datatype.Metric = numericList.Average();
                                            }
                                            else
                                                datatype.UniqueValues = stringList.ToArray();
                                            Dictionary<string, DatatypeDict> keyValues = new Dictionary<string, DatatypeDict>();
                                            keyValues.Add(uniqIdname, datatype);
                                            JObject Current = JObject.Parse(JsonConvert.SerializeObject(keyValues));
                                            InputColumns.Merge(Current, new Newtonsoft.Json.Linq.JsonMergeSettings
                                            {
                                                MergeArrayHandling = Newtonsoft.Json.Linq.MergeArrayHandling.Union
                                            });

                                            JObject mainMapping = new JObject();
                                            var collection2 = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
                                            var filter2 = Builders<DeployModelsDto>.Filter.Eq(CONSTANTS.CorrelationId, cascadeModels.CorrelationId);
                                            var result2 = collection2.Find(filter2).Project<DeployModelsDto>(Builders<DeployModelsDto>.Projection.Exclude(CONSTANTS.Id)).FirstOrDefault();
                                            if (result2 != null)
                                            {
                                                mainMapping[CONSTANTS.ModelName] = result2.ModelName;
                                                mainMapping[CONSTANTS.ProblemType] = result2.ModelType;
                                                mainMapping[CONSTANTS.ModelType] = result2.ModelVersion;
                                                mainMapping[CONSTANTS.CorrelationId] = result2.CorrelationId;
                                                mainMapping[CONSTANTS.ApplicationID] = result2.AppId;
                                                if (result2.LinkedApps != null)
                                                {
                                                    if (result2.LinkedApps.Length > 0)
                                                        mainMapping[CONSTANTS.LinkedApps] = result2.LinkedApps[0];
                                                    else
                                                        mainMapping[CONSTANTS.LinkedApps] = null;
                                                }
                                                else
                                                    mainMapping[CONSTANTS.LinkedApps] = null;
                                            }
                                            mainMapping[CONSTANTS.TargetColumn] = targetColumn;
                                            mainMapping[CONSTANTS.TargetUniqueIdentifier] = uniqIdname;
                                            mainMapping[CONSTANTS.InputColumns] = JObject.FromObject(InputColumns);
                                            modelsMapping[CONSTANTS.Model + counter] = JObject.FromObject(mainMapping);
                                            _customMapping.Category = result2.Category;
                                            _customMapping.ClientUid = result2.ClientUId;
                                            _customMapping.DeliveryConstructUID = result2.DeliveryConstructUID;
                                        }
                                        else
                                        {
                                            _customMapping.IsException = true;
                                            _customMapping.ErrorMessage = "UniqueData is not avaiable for the model";
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                foreach (var model in listModels)
                {
                    var collectonData = GetDataFromCollections(model.CorrelationId);
                    if (collectonData.BusinessProblemData.Count > 0 & collectonData.DataCleanupData.Count > 0 & collectonData.FilteredData.Count > 0)
                    {
                        bool DBEncryptionRequired = CommonUtility.EncryptDB(model.CorrelationId, appSettings);
                        if (DBEncryptionRequired)
                        {
                            collectonData.DataCleanupData[0][CONSTANTS.FeatureName] = BsonDocument.Parse(_encryptionDecryption.Decrypt(collectonData.DataCleanupData[0][CONSTANTS.FeatureName].AsString));
                            if (collectonData.DataCleanupData[0].Contains(CONSTANTS.NewFeatureName))
                            {
                                if (collectonData.DataCleanupData[0][CONSTANTS.NewFeatureName].ToString() != "{ }")
                                    collectonData.DataCleanupData[0][CONSTANTS.NewFeatureName] = BsonDocument.Parse(_encryptionDecryption.Decrypt(collectonData.DataCleanupData[0][CONSTANTS.NewFeatureName].AsString));
                            }
                            collectonData.FilteredData[0][CONSTANTS.ColumnUniqueValues] = BsonDocument.Parse(_encryptionDecryption.Decrypt(collectonData.FilteredData[0][CONSTANTS.ColumnUniqueValues].AsString));
                        }
                        JObject datas = JObject.Parse(collectonData.DataCleanupData[0].ToString());
                        JObject combinedFeatures = new JObject();
                        combinedFeatures = this.CombinedFeatures(datas);
                        if (combinedFeatures != null)
                            collectonData.DataCleanupData[0][CONSTANTS.FeatureName] = BsonDocument.Parse(combinedFeatures.ToString());
                        JObject uniqueData = JObject.Parse(collectonData.FilteredData[0].ToString());
                        JObject datacleanup = JObject.Parse(collectonData.DataCleanupData[0].ToString());
                        var dict = GetColumnDataatypes(datacleanup);
                        JObject InputColumns = new JObject();
                        foreach (var item in dict)
                        {
                            DatatypeDict datatype = new DatatypeDict();
                            datatype.Datatype = item.Value;
                            List<string> stringList = new List<string>();
                            List<double> numericList = new List<double>();
                            foreach (var value in uniqueData[CONSTANTS.ColumnUniqueValues][item.Key].Children())
                            {
                                if (value != null)
                                {
                                    if (item.Value == "float64" || item.Value == "int64")
                                    {
                                        numericList.Add(Math.Round(Convert.ToDouble(value)));
                                    }
                                    else
                                    {
                                        stringList.Add(Convert.ToString(value));
                                    }
                                }
                            }
                            if (item.Value == "float64" || item.Value == "int64")
                            {
                                datatype.UniqueValues = numericList.ToArray();
                                datatype.Min = numericList.Min();
                                datatype.Max = numericList.Max();
                                datatype.Metric = numericList.Average();
                            }
                            else
                                datatype.UniqueValues = stringList.ToArray();
                            Dictionary<string, DatatypeDict> keyValues = new Dictionary<string, DatatypeDict>();
                            keyValues.Add(item.Key, datatype);
                            JObject Current = JObject.Parse(JsonConvert.SerializeObject(keyValues));
                            InputColumns.Merge(Current, new Newtonsoft.Json.Linq.JsonMergeSettings
                            {
                                MergeArrayHandling = Newtonsoft.Json.Linq.MergeArrayHandling.Union
                            });
                        }
                        JObject mainMapping = new JObject();
                        mainMapping[CONSTANTS.ModelName] = model.ModelName;
                        mainMapping[CONSTANTS.ProblemType] = model.ProblemType;
                        mainMapping[CONSTANTS.ModelType] = model.ModelType;
                        mainMapping[CONSTANTS.CorrelationId] = model.CorrelationId;
                        mainMapping[CONSTANTS.TargetColumn] = collectonData.BusinessProblemData[0][CONSTANTS.TargetColumn].ToString();
                        mainMapping[CONSTANTS.TargetUniqueIdentifier] = collectonData.BusinessProblemData[0][CONSTANTS.TargetUniqueIdentifier].ToString();
                        mainMapping[CONSTANTS.InputColumns] = JObject.FromObject(InputColumns);
                        modelsMapping[CONSTANTS.Model + counter] = JObject.FromObject(mainMapping);
                        counter++;
                    }
                }
            }
            return modelsMapping;
        }
        public CascadeVDSModels GetCascadeVDSModels(string ClientUID, string DCUID, string UserID, string Category, out bool isException, out string ErrorMessage)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CascadingService), nameof(GetCascadeVDSModels), "START", string.Empty, string.Empty, ClientUID, DCUID);
            isException = false;
            ErrorMessage = string.Empty;
            CascadeVDSModels cascadeModelsList = new CascadeVDSModels();
            try
            {
                cascadeModelsList.ClientUID = ClientUID;
                cascadeModelsList.DCUID = DCUID;
                cascadeModelsList.Category = Category;
                var collection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
                var filterBuilder = Builders<DeployModelsDto>.Filter;
                var privateFilter = filterBuilder.Eq(CONSTANTS.ClientUId, ClientUID)
                    & filterBuilder.Eq(CONSTANTS.DeliveryConstructUID, DCUID)
                    & filterBuilder.Eq(CONSTANTS.Category, Category)
                    & filterBuilder.Eq(CONSTANTS.IsCascadeModel, true)
                    & filterBuilder.Eq(CONSTANTS.IsPrivate, true)
                    & filterBuilder.AnyEq(CONSTANTS.LinkedApps, CONSTANTS.VDS_SI);
                var publicFilter = filterBuilder.Eq(CONSTANTS.ClientUId, ClientUID)
                    & filterBuilder.Eq(CONSTANTS.DeliveryConstructUID, DCUID)
                    & filterBuilder.Eq(CONSTANTS.Category, Category)
                    & filterBuilder.Eq(CONSTANTS.IsCascadeModel, true)
                    & filterBuilder.Eq(CONSTANTS.IsModelTemplate, false)
                    & filterBuilder.Eq(CONSTANTS.IsPrivate, false)
                    & filterBuilder.AnyEq(CONSTANTS.LinkedApps, CONSTANTS.VDS_SI);
                var projection = Builders<DeployModelsDto>.Projection.Include(CONSTANTS.ModelName).Include(CONSTANTS.CorrelationId).Include(CONSTANTS.CreatedByUser).Exclude(CONSTANTS.Id);
                var privateResult = collection.Find(privateFilter).Project<DeployModelsDto>(projection).ToList();
                var publicResult = collection.Find(publicFilter).Project<DeployModelsDto>(projection).ToList();
                if (privateResult.Count > 0)
                {
                    if (publicResult.Count > 0)
                    {
                        privateResult = privateResult.Concat(publicResult).ToList();
                    }
                    List<string> corIds = new List<string>();
                    foreach (var item in privateResult)
                    {
                        corIds.Add(item.CorrelationId);
                    }
                    cascadeModelsList.CascadeModels = GetVdsModels(privateResult, corIds);
                }
            }
            catch (Exception ex)
            {
                isException = true;
                ErrorMessage = ex.Message + "***STACKTRACE***" + ex.StackTrace;
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(CascadingService), nameof(GetCascadeVDSModels), ex.Message, ex, string.Empty, string.Empty, ClientUID, DCUID);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CascadingService), nameof(GetCascadeVDSModels), "END", string.Empty, string.Empty, ClientUID, DCUID);
            return cascadeModelsList;
        }
        public CascadeInfluencers GetInfluencers(string CascadedId, out bool isException, out string ErrorMessage)
        {
            isException = false;
            ErrorMessage = string.Empty;
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CascadingService), nameof(GetInfluencers), "START", string.IsNullOrEmpty(CascadedId) ? default(Guid) : new Guid(CascadedId), string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                cascadeInfluencers.CascadedId = CascadedId;
                var collection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
                var filter = Builders<DeployModelsDto>.Filter.Eq(CONSTANTS.CorrelationId, CascadedId);
                var projection = Builders<DeployModelsDto>.Projection.Exclude(CONSTANTS.Id);
                var modelResult = collection.Find(filter).Project<DeployModelsDto>(projection).ToList();
                if (modelResult.Count > 0 && modelResult[0].InputSample != null)
                {
                    cascadeInfluencers.ModelType = modelResult[0].ModelType;
                    cascadeInfluencers.ModelVersion = modelResult[0].ModelVersion;
                    cascadeInfluencers.Category = modelResult[0].Category;
                    cascadeInfluencers.ModelCreatedDate = modelResult[0].CreatedOn;
                    bool DBEncryptionRequired = CommonUtility.EncryptDB(CascadedId, appSettings);
                    JArray dynamicList = new JArray();
                    if (DBEncryptionRequired)
                    {
                        if (modelResult[0].InputSample != null && modelResult[0].InputSample.ToString() != CONSTANTS.Null)
                            dynamicList = JsonConvert.DeserializeObject<JArray>(_encryptionDecryption.Decrypt(modelResult[0].InputSample));
                    }
                    else
                        dynamicList = JsonConvert.DeserializeObject<JArray>(modelResult[0].InputSample);
                    List<string> columns = new List<string>();
                    if (dynamicList.Count > 0)
                    {
                        foreach (var column in dynamicList[0].Children())
                        {
                            var prop = column as JProperty;
                            if (prop != null)
                            {
                                columns.Add(prop.Name);
                            }
                        }
                        cascadeInfluencers.InfluencersList = columns;
                        cascadeInfluencers.InputSample = dynamicList;
                    }
                    //Check Source for the sub models like IsonlyFileupload/IsonlySingleEntity/IsMultipleEntities/IsBoth
                    List<string> CorIds = GetCorIDS(CascadedId);
                    if (CorIds.Count > 0)
                    {
                        var requestCollection = _database.GetCollection<IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
                        var requestfilter = Builders<IngrainRequestQueue>.Filter;
                        var filterResult = requestfilter.In(CONSTANTS.CorrelationId, CorIds) & requestfilter.Eq(CONSTANTS.pageInfo, CONSTANTS.IngestData);
                        var requestProjection = Builders<IngrainRequestQueue>.Projection.Exclude(CONSTANTS.Id);
                        var requestResult = requestCollection.Find(filterResult).Project<IngrainRequestQueue>(requestProjection).ToList();
                        if (requestResult.Count > 0)
                        {
                            GetFlags(requestResult, CorIds);
                        }
                    }
                    //end
                    //Get The VisualizationData
                    var collection2 = _database.GetCollection<VisulizationPrediction>(CONSTANTS.SSAICascadeVisualization);
                    var filterBuilder2 = Builders<VisulizationPrediction>.Filter;
                    var filter2 = filterBuilder2.Eq(CONSTANTS.CorrelationId, CascadedId);
                    var projection2 = Builders<VisulizationPrediction>.Projection.Include("Visualization").Include(CONSTANTS.UniqueId).Include(CONSTANTS.CreatedOn).Exclude(CONSTANTS.Id);
                    var visulizationPrediction = collection2.Find(filter2).Project<VisulizationPrediction>(projection2).SortByDescending(bson => bson.CreatedOn).ToList();
                    if (visulizationPrediction.Count > 0)
                    {
                        cascadeInfluencers.IsVisualizationAvaialble = true;
                        cascadeInfluencers.UniqueId = visulizationPrediction[0].UniqueId;
                        cascadeInfluencers.ModelLastPredictionTime = visulizationPrediction[0].CreatedOn;
                        List<JObject> list = new List<JObject>();
                        for (int i = 0; i < visulizationPrediction.Count; i++)
                        {
                            JObject visulization = JObject.Parse(visulizationPrediction[i].Visualization.ToString());
                            list.Add(visulization);
                        }
                        cascadeInfluencers.Visualization = list;
                    }
                }
            }
            catch (Exception ex)
            {
                isException = true;
                ErrorMessage = ex.Message + "***STACKTRACE***" + ex.StackTrace;
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(CascadingService), nameof(GetInfluencers), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CascadingService), nameof(GetInfluencers), "END", string.IsNullOrEmpty(CascadedId) ? default(Guid) : new Guid(CascadedId), string.Empty, string.Empty, string.Empty, string.Empty);
            return cascadeInfluencers;
        }
        private void GetFlags(List<IngrainRequestQueue> requestResult, List<string> CorIds)
        {
            List<string> type = new List<string>();
            List<string> entityType = new List<string>();
            foreach (var item in requestResult)
            {
                if (item.ParamArgs != null && !string.IsNullOrEmpty(item.ParamArgs))
                {
                    //Deferred Defect: 2027893
                    JObject fileUpload = JObject.Parse(item.ParamArgs);
                    //FileUpload fileUpload = JsonConvert.DeserializeObject<FileUpload>(item.ParamArgs);
                    if (appSettings.Value.Environment.Equals(CONSTANTS.FDSEnvironment) || appSettings.Value.Environment.Equals(CONSTANTS.PAMEnvironment))//Deferred Defect: 1896911
                    {
                        if (fileUpload["Customdetails"].ToString() == CONSTANTS.Null || fileUpload["Customdetails"].ToString() == "" || string.IsNullOrEmpty(fileUpload["Customdetails"].ToString()))
                        {
                            if (fileUpload["fileupload"]["fileList"].ToString() != CONSTANTS.Null || fileUpload["fileupload"]["fileList"].ToString() != "" || !string.IsNullOrEmpty(fileUpload["fileupload"]["fileList"].ToString()))
                            {
                                type.Add("file");
                            }
                        }
                        if (fileUpload["fileupload"]["fileList"].ToString() == CONSTANTS.Null || fileUpload["fileupload"]["fileList"].ToString() == "" || string.IsNullOrEmpty(fileUpload["fileupload"]["fileList"].ToString()))
                        {
                            if (fileUpload["Customdetails"].ToString() != CONSTANTS.Null || fileUpload["Customdetails"].ToString() != "" || !string.IsNullOrEmpty(fileUpload["Customdetails"].ToString()))
                            {
                                type.Add("entity");
                                CustomPayloads Customdetails = JsonConvert.DeserializeObject<CustomPayloads>(fileUpload["Customdetails"].ToString());
                                if (Customdetails != null)
                                {
                                    entityType.Add(Customdetails.HttpMethod);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (fileUpload["pad"].ToString() == CONSTANTS.Null || fileUpload["pad"].ToString() == "" || string.IsNullOrEmpty(fileUpload["pad"].ToString()))
                        {
                            if (fileUpload["fileupload"]["fileList"].ToString() != CONSTANTS.Null || fileUpload["fileupload"]["fileList"].ToString() != "" || !string.IsNullOrEmpty(fileUpload["fileupload"]["fileList"].ToString()))
                            {
                                type.Add("file");
                            }
                        }
                        if (fileUpload["fileupload"]["fileList"].ToString() == CONSTANTS.Null || fileUpload["fileupload"]["fileList"].ToString() == "" || string.IsNullOrEmpty(fileUpload["fileupload"]["fileList"].ToString()))
                        {
                            if (fileUpload["pad"].ToString() != CONSTANTS.Null || fileUpload["pad"].ToString() != "" || !string.IsNullOrEmpty(fileUpload["pad"].ToString()))
                            {
                                type.Add("entity");
                                pad2 pad = JsonConvert.DeserializeObject<pad2>(fileUpload["pad"].ToString());
                                if (pad != null)
                                {
                                    entityType.Add(pad.method);
                                }
                            }
                        }
                    }
                }
            }
            var totalFiles = type.Where(elem => elem == "file");
            var totalEntities = type.Where(elem => elem == "entity");
            int fileCount = totalFiles.Count();
            int entityCount = totalEntities.Count();
            if (entityType.Distinct().Count() == 1 && fileCount < 1)
            {
                cascadeInfluencers.IsonlyFileupload = false;
                cascadeInfluencers.IsonlySingleEntity = true;
                cascadeInfluencers.IsBoth = false;
                cascadeInfluencers.IsMultipleEntities = false;
            }
            else
            {
                if (fileCount == CorIds.Count() && entityCount < 1)
                {
                    cascadeInfluencers.IsonlyFileupload = true;
                    cascadeInfluencers.IsonlySingleEntity = false;
                    cascadeInfluencers.IsBoth = false;
                    cascadeInfluencers.IsMultipleEntities = false;
                }
                else if (entityCount == CorIds.Count() && fileCount < 1)
                {
                    cascadeInfluencers.IsonlyFileupload = false;
                    cascadeInfluencers.IsonlySingleEntity = false;
                    cascadeInfluencers.IsBoth = false;
                    cascadeInfluencers.IsMultipleEntities = true;
                }
                else
                {
                    cascadeInfluencers.IsonlyFileupload = false;
                    cascadeInfluencers.IsonlySingleEntity = false;
                    cascadeInfluencers.IsBoth = true;
                    cascadeInfluencers.IsMultipleEntities = false;
                }
            }
        }
        private List<string> GetCorIDS(string CascadedId)
        {
            List<string> CorIds = new List<string>();
            var cascadeCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAICascadedModels);
            var cascadeFilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CascadedId, CascadedId);
            var cascadeProjection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.MappingData).Exclude(CONSTANTS.Id);
            var cascadeResult = cascadeCollection.Find(cascadeFilter).Project<BsonDocument>(cascadeProjection).ToList();
            if (cascadeResult.Count > 0)
            {
                JObject modelList = JObject.Parse(cascadeResult[0].ToString());
                if (modelList != null && modelList.ToString() != "{}")
                {
                    foreach (var item in modelList[CONSTANTS.ModelList].Children())
                    {
                        if (item != null)
                        {
                            JProperty prop = item as JProperty;
                            if (prop != null)
                            {
                                CascadeModelDictionary modelDictionary = JsonConvert.DeserializeObject<CascadeModelDictionary>(prop.Value.ToString());
                                CorIds.Add(modelDictionary.CorrelationId);
                            }
                        }
                    }
                }
            }
            return CorIds;
        }
        private List<CascadeModels> GetVdsModels(List<DeployModelsDto> result, List<string> corIds)
        {
            List<CascadeModels> listModels = new List<CascadeModels>();
            var cascadeCollection = _database.GetCollection<CascadeDocument>(CONSTANTS.SSAICascadedModels);
            var cascadeFilter = Builders<CascadeDocument>.Filter.In(CONSTANTS.CascadedId, corIds);
            var cascadeProjection = Builders<CascadeDocument>.Projection.Exclude(CONSTANTS.MappingData).Exclude(CONSTANTS.Id);
            var cascadeResult = cascadeCollection.Find(cascadeFilter).Project<CascadeDocument>(cascadeProjection).ToList();
            List<string> idList = new List<string>();
            List<CombinedModel> modelCombined = new List<CombinedModel>();
            foreach (var corid in cascadeResult)
            {
                CombinedModel combinedModel = new CombinedModel();
                if (corid.IsCustomModel)
                {
                    JObject lastModel = JObject.Parse(corid.ModelList.ToString());
                    string modelname = string.Empty;
                    if (lastModel.Children().Count() == 5)
                        modelname = string.Format("Model{0}", lastModel.Children().Count());
                    else
                        modelname = string.Format("Model{0}", lastModel.Children().Count() - 1);
                    CascadeModelDictionary modelDictionary = JsonConvert.DeserializeObject<CascadeModelDictionary>(corid.ModelList[modelname].ToString());
                    idList.Add(modelDictionary.CorrelationId);
                    combinedModel.CascadedId = corid.CascadedId;
                    combinedModel.CorrelationId = modelDictionary.CorrelationId;
                }
                else
                {
                    JObject lastModel = JObject.Parse(corid.ModelList.ToString());
                    int count = lastModel.Children().Count();
                    string modelname = string.Format("Model{0}", count);
                    CascadeModelDictionary modelDictionary = JsonConvert.DeserializeObject<CascadeModelDictionary>(corid.ModelList[modelname].ToString());
                    idList.Add(modelDictionary.CorrelationId);
                    combinedModel.CascadedId = corid.CascadedId;
                    combinedModel.CorrelationId = modelDictionary.CorrelationId;
                }
                modelCombined.Add(combinedModel);
            }
            var businessCollection = _database.GetCollection<BusinessProblem>(CONSTANTS.PSBusinessProblem);
            var psFilter = Builders<BusinessProblem>.Filter.In(CONSTANTS.CorrelationId, idList);
            var psProjection = Builders<BusinessProblem>.Projection.Include(CONSTANTS.CorrelationId).Include(CONSTANTS.BusinessProblems).Exclude(CONSTANTS.Id);
            var businessresult = businessCollection.Find(psFilter).Project<BusinessProblem>(psProjection).ToList();
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CascadingService), "--DEPLOYMODELSCOUNT-" + result.Count + "--CASCADEMODELCOUNT--" + cascadeResult.Count + "---BUSINESSRESULTCOUNT---" + businessresult.Count, "START", string.Empty, string.Empty, string.Empty, string.Empty);
            foreach (var item in result)
            {
                bool DBEncryptionRequired = CommonUtility.EncryptDB(item.CorrelationId, appSettings);
                if (DBEncryptionRequired)
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(Convert.ToString(item.CreatedByUser)))
                        {
                            item.CreatedByUser = _encryptionDecryption.Decrypt(Convert.ToString(item.CreatedByUser));
                        }
                    }
                    catch (Exception ex)
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(CascadingService), nameof(GetVdsModels), ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty);
                    }
                    try
                    {
                        if (!string.IsNullOrEmpty(Convert.ToString(item.ModifiedByUser)))
                        {
                            item.ModifiedByUser = _encryptionDecryption.Decrypt(Convert.ToString(item.ModifiedByUser));
                        }
                    }
                    catch (Exception ex)
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(CascadingService), nameof(GetVdsModels), ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty);
                    }
                }
                CascadeModels cascadeModels = new CascadeModels();
                cascadeModels.ModelName = item.ModelName;
                cascadeModels.CascadedId = item.CorrelationId;
                cascadeModels.UserID = item.CreatedByUser;
                var modelData = modelCombined.Find(x => x.CascadedId == item.CorrelationId);
                if (modelData != null)
                {
                    var lastModelCorId = modelCombined.Find(x => x.CascadedId == item.CorrelationId).CorrelationId;
                    var data = businessresult.Find(x => x.CorrelationId == lastModelCorId);
                    if (data != null)
                        cascadeModels.Description = data.BusinessProblems;
                    else
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(CascadingService), "-CORRELATIONID NOT PRESENT AT PSBUSNIESSPROBLEM COLLECTION--" + lastModelCorId, "START", string.IsNullOrEmpty(lastModelCorId) ? default(Guid) : new Guid(lastModelCorId), string.Empty, string.Empty, string.Empty, string.Empty);
                }
                else
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(CascadingService), "-CORRELATIONID NOT PRESENT AT CASCADE COLLECTION--" + item.CorrelationId, "START", string.IsNullOrEmpty(item.CorrelationId) ? default(Guid) : new Guid(item.CorrelationId), string.Empty, string.Empty, string.Empty, string.Empty);
                }
                listModels.Add(cascadeModels);
            }
            return listModels;
        }
        public UploadResponse UploadData(VisulizationUpload visulization, IFormFileCollection fileCollection, out bool isException, out string ErrorMessage)
        {
            string filePath = string.Empty;
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CascadingService), nameof(UploadData), "END", string.IsNullOrEmpty(visulization.CascadedId) ? default(Guid) : new Guid(visulization.CascadedId), string.Empty, string.Empty, visulization.ClientUID, visulization.DCUID);
            isException = false;
            uploadResponse.CascadedId = visulization.CascadedId;
            uploadResponse.ClinetUID = visulization.ClientUID;
            uploadResponse.DCUID = visulization.DCUID;
            ErrorMessage = string.Empty;
            try
            {
                uploadResponse.UniqueId = Guid.NewGuid().ToString();
                if (visulization.IsFileUpload)
                {
                    IFormFile file = fileCollection[0];
                    filePath = appSettings.Value.UploadFilePath;
                    Directory.CreateDirectory(Path.Combine(filePath, appSettings.Value.SavedModels));
                    //filePath = System.IO.Path.Combine(filePath, appSettings.Value.AppData);
                    filePath = filePath + "//" + appSettings.Value.AppData;
                    System.IO.Directory.CreateDirectory(filePath);
                    if (file.Length <= 0)
                    {
                        uploadResponse.ValidatonMessage = CONSTANTS.FileEmpty;
                        uploadResponse.IsUploaded = false;
                        uploadResponse.Message = CONSTANTS.FileEmpty;
                        uploadResponse.Status = CONSTANTS.E;
                        return uploadResponse;
                    }
                    string completePath = filePath + Path.GetFileName(visulization.CascadedId + "_" + file.FileName);
                    _encryptionDecryption.EncryptFile(file, filePath + Path.GetFileName(visulization.CascadedId + "_" + file.FileName));
                    var Extension = Path.GetExtension(completePath);
                    if (Extension != ".xlsx" & Extension != ".csv")
                    {
                        uploadResponse.ValidatonMessage = CONSTANTS.FileNotSupport;
                        uploadResponse.IsUploaded = false;
                        uploadResponse.Message = CONSTANTS.FileNotSupport;
                        uploadResponse.Status = CONSTANTS.E;
                        return uploadResponse;
                    }
                    uploadFile(visulization, fileCollection, completePath);
                    if (uploadResponse.IsException)
                    {
                        isException = true;
                        ErrorMessage = uploadResponse.ErrorMessage;
                        return uploadResponse;
                    }
                }
                else
                {
                    EntityUpload(visulization);
                }
                CheckUploadData(visulization.CascadedId, uploadResponse.UniqueId);
            }
            catch (Exception ex)
            {
                uploadResponse.Status = CONSTANTS.E;
                isException = true;
                ErrorMessage = ex.Message + "***STACKTRACE***" + ex.StackTrace;
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(CascadingService), nameof(UploadData), ex.Message, ex, string.Empty, string.Empty, visulization.ClientUID, visulization.DCUID);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CascadingService), nameof(UploadData), "END", string.IsNullOrEmpty(visulization.CascadedId) ? default(Guid) : new Guid(visulization.CascadedId), string.Empty, string.Empty, visulization.ClientUID, visulization.DCUID);
            return uploadResponse;
        }
        private void InsertRequests(IngrainRequestQueue ingrainRequest)
        {
            var requestQueue = JsonConvert.SerializeObject(ingrainRequest);
            var insertRequestQueue = BsonSerializer.Deserialize<BsonDocument>(requestQueue);
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
            collection.InsertOne(insertRequestQueue);
        }
        private void uploadFile(VisulizationUpload visulization, IFormFileCollection fileCollection, string filePath)
        {
            try
            {
                IngrainRequestQueue ingrainRequest = null;
                bool DBEncryptionRequired = CommonUtility.EncryptDB(visulization.CascadedId, appSettings);
                string userId = visulization.UserId;
                //if (DBEncryptionRequired)
                {
                    if (!string.IsNullOrEmpty(visulization.UserId))
                    {
                        userId = _encryptionDecryption.Encrypt(Convert.ToString(visulization.UserId));
                    }
                }
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(CascadingService), nameof(UploadData), "UNIID 1--" + uploadResponse.UniqueId, string.IsNullOrEmpty(visulization.CascadedId) ? default(Guid) : new Guid(visulization.CascadedId), string.Empty, string.Empty, visulization.ClientUID, visulization.DCUID);
                ingrainRequest = new IngrainRequestQueue
                {
                    _id = Guid.NewGuid().ToString(),
                    CorrelationId = visulization.CascadedId,
                    DataSetUId = null,
                    RequestId = Guid.NewGuid().ToString(),
                    ProcessId = null,
                    Status = null,
                    ModelName = visulization.ModelName,
                    RequestStatus = CONSTANTS.New,
                    RetryCount = 0,
                    ProblemType = null,
                    Message = null,
                    UniId = uploadResponse.UniqueId,
                    Progress = null,
                    pageInfo = CONSTANTS.CascadeFile,
                    ParamArgs = null,
                    Function = CONSTANTS.FileUpload,
                    CreatedByUser = userId,
                    CreatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    ModifiedByUser = userId,
                    ModifiedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    LastProcessedOn = null
                };
                _filepath = new Filepath();
                string postedFileName = string.Empty;
                if (fileCollection.Count > 0)
                {
                    if (appSettings.Value.IsAESKeyVault && appSettings.Value.Environment == CONSTANTS.PADEnvironment && appSettings.Value.EncryptUploadedFiles)
                    {
                        filePath = filePath + ".enc" + @"""";
                    }
                    else
                        filePath = filePath + @"""";
                    postedFileName = "[" + @"""" + filePath + "]";
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(IngestedDataService), nameof(UploadData), "FILECOLLECTION POSTEDFILENAME: " + postedFileName + ", CORRELATIONID: " + visulization.CascadedId, string.Empty, string.Empty, visulization.ClientUID, visulization.DCUID);
                }
                if (postedFileName != "")
                    _filepath.fileList = postedFileName;
                else
                    _filepath.fileList = "null";
                parentFile = new ParentFile();
                parentFile.Type = CONSTANTS.Null;
                parentFile.Name = CONSTANTS.Null;
                string MappingColumns = string.Empty;
                fileUpload = new FileUpload
                {
                    CorrelationId = visulization.CascadedId,
                    ClientUID = visulization.ClientUID,
                    DeliveryConstructUId = visulization.DCUID,
                    Parent = parentFile,
                    Flag = "Incremental",
                    mapping = MappingColumns,
                    mapping_flag = CONSTANTS.False,
                    pad = CONSTANTS.Null,
                    metric = CONSTANTS.Null,
                    InstaMl = CONSTANTS.Null,
                    fileupload = _filepath,
                    Customdetails = CONSTANTS.Null
                };
                ingrainRequest.ParamArgs = fileUpload.ToJson();
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(IngestedDataService), nameof(uploadFile), "-UNIID2-: " + ingrainRequest.UniId + "--RequestID-" + ingrainRequest.RequestId + "UPLOADRESPONSE UNIQUEID--" + uploadResponse.UniqueId, string.Empty, string.Empty, visulization.ClientUID, visulization.DCUID);
                InsertRequests(ingrainRequest);
                uploadResponse.IsUploaded = true;
                uploadResponse.Message = CONSTANTS.success;
                uploadResponse.Status = CONSTANTS.C;
            }
            catch (Exception ex)
            {
                uploadResponse.Status = CONSTANTS.E;
                uploadResponse.IsException = true;
                uploadResponse.ErrorMessage = ex.Message + "***STACKTRACE***" + ex.StackTrace;
            }
        }
        private void EntityUpload(VisulizationUpload visulization)
        {
            try
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(IngestedDataService), nameof(EntityUpload), "START", string.IsNullOrEmpty(visulization.CascadedId) ? default(Guid) : new Guid(visulization.CascadedId), string.Empty, string.Empty, visulization.ClientUID, visulization.DCUID);
                string Lastpredictiondate = string.Empty;
                var cascadeCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAICascadedModels);
                var cascadeFilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CascadedId, visulization.CascadedId);
                var cascadeProjection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.MappingData).Exclude(CONSTANTS.Id);
                var cascadeResult = cascadeCollection.Find(cascadeFilter).Project<BsonDocument>(cascadeProjection).ToList();
                List<string> corIds = new List<string>();
                if (cascadeResult.Count > 0)
                {
                    JObject model = JObject.Parse(cascadeResult[0].ToString());
                    if (model != null && model.ToString() != "{}")
                    {
                        //JArray modelData = JArray.Parse(model[CONSTANTS.ModelList].ToString());
                        foreach (var corid in model[CONSTANTS.ModelList].Children())
                        {
                            JProperty prop = corid as JProperty;
                            if (prop != null)
                            {
                                var modelDictionary = JsonConvert.DeserializeObject<CascadeModelDictionary>(prop.Value.ToString());
                                if (modelDictionary.CorrelationId != null)
                                {
                                    corIds.Add(modelDictionary.CorrelationId);
                                }
                            }
                        }
                        for (int i = corIds.Count - 1; i >= 0; i--)
                        {
                            var collection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
                            var filterBuilder = Builders<DeployModelsDto>.Filter.Eq(CONSTANTS.CorrelationId, corIds[i]);
                            var projection = Builders<DeployModelsDto>.Projection.Exclude(CONSTANTS.Id);
                            var result = collection.Find(filterBuilder).Project<DeployModelsDto>(projection).ToList();
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(IngestedDataService), nameof(EntityUpload), "EntityUpload SEQUENCE1", string.IsNullOrEmpty(corIds[i]) ? default(Guid) : new Guid(corIds[i]), string.Empty, string.Empty, visulization.ClientUID, visulization.DCUID);
                            if (result.Count > 0)
                            {
                                if (result[0].SourceName == CONSTANTS.pad || result[0].SourceName == CONSTANTS.metrics || ((appSettings.Value.Environment.Equals(CONSTANTS.FDSEnvironment) || appSettings.Value.Environment.Equals(CONSTANTS.PAMEnvironment)) & result[0].SourceName == CONSTANTS.Custom))
                                {
                                    var requestData = GetFileRequestStatus(corIds[i], CONSTANTS.IngestData);
                                    if (requestData != null)
                                    {
                                        if (requestData.ParamArgs != null)
                                        {
                                            string uniqId = Guid.NewGuid().ToString();
                                            bool DBEncryptionRequired = CommonUtility.EncryptDB(visulization.CascadedId, appSettings);
                                            string userId = visulization.UserId;
                                            //if (DBEncryptionRequired)
                                            {
                                                if (!string.IsNullOrEmpty(visulization.UserId))
                                                {
                                                    userId = _encryptionDecryption.Encrypt(Convert.ToString(visulization.UserId));
                                                }
                                            }
                                            ingrainRequest = new IngrainRequestQueue
                                            {
                                                _id = Guid.NewGuid().ToString(),
                                                CorrelationId = visulization.CascadedId,
                                                DataSetUId = null,
                                                RequestId = Guid.NewGuid().ToString(),
                                                ProcessId = null,
                                                Status = null,
                                                ModelName = visulization.ModelName,
                                                RequestStatus = CONSTANTS.New,
                                                RetryCount = 0,
                                                ProblemType = null,
                                                Message = null,
                                                UniId = uploadResponse.UniqueId,
                                                Progress = null,
                                                pageInfo = CONSTANTS.CascadeFile,
                                                ParamArgs = null,
                                                Function = CONSTANTS.FileUpload,
                                                CreatedByUser = userId,
                                                CreatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                                                ModifiedByUser = userId,
                                                ModifiedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                                                LastProcessedOn = null
                                            };
                                            if ((appSettings.Value.Environment.Equals(CONSTANTS.FDSEnvironment) || appSettings.Value.Environment.Equals(CONSTANTS.PAMEnvironment)) & result[0].SourceName == CONSTANTS.Custom)
                                            {
                                                var fileUpload = JsonConvert.DeserializeObject<CustomUploadFile>(requestData.ParamArgs);
                                                fileUpload.Flag = "Incremental";
                                                fileUpload.mapping = CONSTANTS.Null;
                                                fileUpload.pad = CONSTANTS.Null;
                                                fileUpload.metric = CONSTANTS.Null;
                                                fileUpload.InstaMl = CONSTANTS.Null;
                                                Lastpredictiondate = GetLastPredictionDate(corIds[i]);
                                                DateTime dateTime = new DateTime();
                                                if (!string.IsNullOrEmpty(Lastpredictiondate))
                                                    dateTime = Convert.ToDateTime(Lastpredictiondate);
                                                else
                                                    dateTime = Convert.ToDateTime(DateTime.Now.ToString("yyyy/MM/dd"));
                                                fileUpload.Customdetails.InputParameters.StartDate = dateTime.AddDays(-30).ToString("yyyy/MM/dd");
                                                ingrainRequest.ParamArgs = fileUpload.ToJson();
                                            }
                                            else
                                            {
                                                var fileUpload = JsonConvert.DeserializeObject<CascadeFileUpload>(requestData.ParamArgs);
                                                fileUpload.Customdetails = CONSTANTS.Null;
                                                fileUpload.Flag = "Incremental";
                                                var padDetails = JsonConvert.DeserializeObject<pad2>(fileUpload.pad);
                                                Lastpredictiondate = GetLastPredictionDate(corIds[i]);
                                                DateTime dateTime = new DateTime();
                                                if (!string.IsNullOrEmpty(Lastpredictiondate))
                                                    dateTime = Convert.ToDateTime(Lastpredictiondate);
                                                else
                                                    dateTime = Convert.ToDateTime(DateTime.Now.ToString("yyyy/MM/dd"));
                                                padDetails.startDate = dateTime.AddDays(-30).ToString("yyyy/MM/dd");
                                                fileUpload.pad = JsonConvert.SerializeObject(padDetails);
                                                ingrainRequest.ParamArgs = fileUpload.ToJson();
                                            }
                                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(IngestedDataService), nameof(EntityUpload), "EntityUpload SEQUENCE1 INSERT DATA -" + JsonConvert.SerializeObject(ingrainRequest), string.IsNullOrEmpty(corIds[i]) ? default(Guid) : new Guid(corIds[i]), string.Empty, string.Empty, visulization.ClientUID, visulization.DCUID);
                                            InsertRequests(ingrainRequest);
                                            uploadResponse.IsException = false;
                                            uploadResponse.Status = CONSTANTS.C;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                uploadResponse.Status = CONSTANTS.E;
                uploadResponse.IsException = true;
                uploadResponse.ErrorMessage = ex.Message + "***STACKTRACE***" + ex.StackTrace;
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(IngestedDataService), nameof(EntityUpload), "END", string.IsNullOrEmpty(visulization.CascadedId) ? default(Guid) : new Guid(visulization.CascadedId), string.Empty, string.Empty, visulization.ClientUID, visulization.DCUID);
        }
        private string GetLastPredictionDate(string correlationId)
        {
            string date = string.Empty;
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_PublishModel);
            var filterBuilder = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.CreatedOn).Exclude(CONSTANTS.Id);
            BsonDocument result = collection.Find(filterBuilder).Project<BsonDocument>(projection).SortByDescending(bson => bson[CONSTANTS.CreatedOn]).FirstOrDefault();
            if (result != null)
            {
                date = result[CONSTANTS.CreatedOn].ToString();
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(IngestedDataService), nameof(GetLastPredictionDate), "-date-: " + date, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            return date;
        }
        private IngrainRequestQueue GetFileRequestStatus(string correlationId, string pageInfo)
        {
            IngrainRequestQueue ingrainRequest = new IngrainRequestQueue();
            var collection = _database.GetCollection<IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
            var builder = Builders<IngrainRequestQueue>.Filter;
            var filter = builder.Eq(CONSTANTS.CorrelationId, correlationId) & builder.Eq(CONSTANTS.pageInfo, pageInfo);
            return ingrainRequest = collection.Find(filter).ToList().FirstOrDefault();
        }
        private IngrainRequestQueue GetUploadRequestStatus(string correlationId, string UniId)
        {
            IngrainRequestQueue ingrainRequest = new IngrainRequestQueue();
            var collection = _database.GetCollection<IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
            var builder = Builders<IngrainRequestQueue>.Filter;
            var filter = builder.Eq(CONSTANTS.CorrelationId, correlationId) & builder.Eq(CONSTANTS.UniId, UniId);
            return ingrainRequest = collection.Find(filter).ToList().FirstOrDefault();
        }
        private IngrainRequestQueue GetFMFileUploadRequestStatus(string correlationId, string requestId)
        {
            IngrainRequestQueue ingrainRequest = new IngrainRequestQueue();
            var collection = _database.GetCollection<IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
            var builder = Builders<IngrainRequestQueue>.Filter;
            var filter = builder.Eq(CONSTANTS.CorrelationId, correlationId) & builder.Eq(CONSTANTS.RequestId, requestId);
            return ingrainRequest = collection.Find(filter).ToList().FirstOrDefault();
        }
        private void CheckUploadData(string correlationId, string UniId)
        {
            IngrainRequestQueue ingrainRequest = new IngrainRequestQueue();
            bool isPythonSuccess = true;
            int i = 0;
            int j = 0;
            while (isPythonSuccess)
            {
                if (i < 1)
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(IngestedDataService), nameof(uploadFile), "-UNIID3-: " + UniId, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), ingrainRequest.AppID, string.Empty, ingrainRequest.ClientID, ingrainRequest.DeliveryconstructId);
                ingrainRequest = GetUploadRequestStatus(correlationId, UniId);
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(IngestedDataService), nameof(uploadFile), "-UNIID4-: " + ingrainRequest.UniId, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), ingrainRequest.AppID, string.Empty, ingrainRequest.ClientID, ingrainRequest.DeliveryconstructId);
                i++;
                if (ingrainRequest != null)
                {
                    if (j < 1)
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(IngestedDataService), nameof(uploadFile), "-UNIID5-: " + ingrainRequest.UniId + "--REQUESTID--" + ingrainRequest.RequestId, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), ingrainRequest.AppID, string.Empty, ingrainRequest.ClientID, ingrainRequest.DeliveryconstructId);
                    j++;
                    if (ingrainRequest.Status == CONSTANTS.C & ingrainRequest.Progress == CONSTANTS.Hundred)
                    {
                        uploadResponse.ErrorMessage = ingrainRequest.Message;
                        uploadResponse.Status = ingrainRequest.Status;
                        uploadResponse.Message = CONSTANTS.success;
                        uploadResponse.IsUploaded = true;
                        isPythonSuccess = false;
                        insertCascadePredictionRequest(ingrainRequest);
                    }
                    else if (ingrainRequest.Status == CONSTANTS.E)
                    {
                        uploadResponse.ErrorMessage = ingrainRequest.Message;
                        uploadResponse.Status = ingrainRequest.Status;
                        uploadResponse.Message = ingrainRequest.Message;
                        uploadResponse.IsUploaded = false;
                        isPythonSuccess = false;
                    }
                    else
                    {
                        Thread.Sleep(2000);
                    }
                }
            }
        }
        private void insertCascadePredictionRequest(IngrainRequestQueue request)
        {
            IngrainRequestQueue ingrainRequest = new IngrainRequestQueue
            {
                _id = Guid.NewGuid().ToString(),
                CorrelationId = request.CorrelationId,
                RequestId = Guid.NewGuid().ToString(),
                ProcessId = null,
                Status = "I",
                ModelName = request.ModelName,
                RequestStatus = "New",
                RetryCount = 0,
                ProblemType = null,
                Message = null,
                UniId = request.UniId,
                Progress = null,
                pageInfo = CONSTANTS.PublishModel,
                ParamArgs = CONSTANTS.True,
                Function = CONSTANTS.PublishModel,
                CreatedByUser = request.CreatedByUser,
                CreatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                ModifiedByUser = request.CreatedByUser,
                ModifiedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                LastProcessedOn = null
            };
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(IngestedDataService), nameof(insertCascadePredictionRequest), "-UNIID6-: " + request.UniId + "--RequestID-" + ingrainRequest.RequestId, string.IsNullOrEmpty(ingrainRequest.CorrelationId) ? default(Guid) : new Guid(ingrainRequest.CorrelationId), string.Empty, string.Empty, string.Empty, string.Empty);
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(IngestedDataService), nameof(insertCascadePredictionRequest), "-UNIID7-: " + ingrainRequest.UniId + "--RequestID-" + ingrainRequest.RequestId, string.IsNullOrEmpty(ingrainRequest.CorrelationId) ? default(Guid) : new Guid(ingrainRequest.CorrelationId), string.Empty, string.Empty, string.Empty, string.Empty);
            InsertRequests(ingrainRequest);
        }

        public VisualizationViewModel GetCascadePrediction(string correlationId, string uniqId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(IngestedDataService), nameof(GetCascadePrediction), "START UNIQID-" + uniqId, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            List<VisulizationPrediction> visulizationPrediction = new List<VisulizationPrediction>();
            visualizationViewModel.CascadedId = correlationId;
            visualizationViewModel.UniqueId = uniqId;
            IngrainRequestQueue ingrainRequest = new IngrainRequestQueue();
            try
            {
                var deployCollection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
                var deployfilterBuilder = Builders<DeployModelsDto>.Filter.Eq(CONSTANTS.CorrelationId, correlationId.Trim());
                var deployprojection = Builders<DeployModelsDto>.Projection.Exclude(CONSTANTS.Id);
                var deployModelResult = deployCollection.Find(deployfilterBuilder).Project<DeployModelsDto>(deployprojection).ToList();
                if (deployModelResult.Count > 0)
                {
                    visualizationViewModel.ModelCreatedDate = deployModelResult[0].CreatedOn;
                    visualizationViewModel.ModelType = deployModelResult[0].ModelType;
                    visualizationViewModel.ModelVersion = deployModelResult[0].ModelVersion;
                    visualizationViewModel.Category = deployModelResult[0].Category;
                }

                var collection = _database.GetCollection<IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
                var filterBuilder = Builders<IngrainRequestQueue>.Filter;
                var filter = filterBuilder.Eq(CONSTANTS.CorrelationId, correlationId.Trim()) & filterBuilder.Eq("UniId", uniqId.Trim()) & filterBuilder.Eq(CONSTANTS.pageInfo, CONSTANTS.PublishModel);
                var projection = Builders<IngrainRequestQueue>.Projection.Exclude(CONSTANTS.Id);
                ingrainRequest = collection.Find(filter).Project<IngrainRequestQueue>(projection).FirstOrDefault();
                if (ingrainRequest != null)
                {
                    visualizationViewModel.Status = ingrainRequest.Status;
                    visualizationViewModel.Message = ingrainRequest.Message;
                    visualizationViewModel.ErrorMessage = ingrainRequest.Message;
                    visualizationViewModel.Progress = ingrainRequest.Progress;
                    if (ingrainRequest.Status == CONSTANTS.C && ingrainRequest.Progress == CONSTANTS.Hundred)
                    {
                        var collection2 = _database.GetCollection<VisulizationPrediction>(CONSTANTS.SSAICascadeVisualization);
                        var filterBuilder2 = Builders<VisulizationPrediction>.Filter;
                        var filter2 = filterBuilder2.Eq(CONSTANTS.CorrelationId, correlationId) & filterBuilder2.Eq("UniqueId", uniqId.Trim());
                        var projection2 = Builders<VisulizationPrediction>.Projection.Include("Visualization").Include(CONSTANTS.CreatedOn).Exclude(CONSTANTS.Id);
                        visulizationPrediction = collection2.Find(filter2).Project<VisulizationPrediction>(projection2).ToList();
                        if (visulizationPrediction.Count > 0)
                        {
                            List<JObject> list = new List<JObject>();
                            for (int i = 0; i < visulizationPrediction.Count; i++)
                            {
                                JObject visulization = JObject.Parse(visulizationPrediction[i].Visualization.ToString());
                                list.Add(visulization);
                            }
                            visualizationViewModel.ModelLastPredictionTime = visulizationPrediction[0].CreatedOn;
                            visualizationViewModel.Message = CONSTANTS.success;
                            visualizationViewModel.Visualization = list;
                            return visualizationViewModel;
                        }
                    }
                    else if (ingrainRequest.Status == CONSTANTS.E)
                    {
                        visualizationViewModel.ErrorMessage = "Error at python processing";
                        return visualizationViewModel;
                    }
                }
            }
            catch (Exception ex)
            {
                visualizationViewModel.Status = CONSTANTS.E;
                visualizationViewModel.IsException = true;
                visualizationViewModel.ErrorMessage = ex.Message + "***STACKTRACE***" + ex.StackTrace;
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(CascadingService), nameof(UploadData), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(IngestedDataService), nameof(GetCascadePrediction), "END UNIQID-" + uniqId, string.Empty, string.Empty, string.Empty, string.Empty);
            return visualizationViewModel;
        }
        public ShowData ShowData(string cascadeId, string uniqueId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(IngestedDataService), nameof(ShowData), "START UNIQID-" + uniqueId, string.IsNullOrEmpty(cascadeId) ? default(Guid) : new Guid(cascadeId), string.Empty, string.Empty, string.Empty, string.Empty);
            ShowData data = new ShowData();
            data.CascadedId = cascadeId;
            data.UniqueId = uniqueId;
            List<string> corids = new List<string>();
            List<DeployModelsDto> deployModelsDtos = new List<DeployModelsDto>();
            CascadeModelDictionary modelDictionary = new CascadeModelDictionary();
            CascadeModelDictionary modelDictionary2 = new CascadeModelDictionary();
            bool isCutomModel = false;
            try
            {
                JArray PredictionProbability = null;
                JObject FeatureWeights = null;
                List<string> columnsList = new List<string>();
                List<JObject> listArray = new List<JObject>();
                var collection2 = _database.GetCollection<BsonDocument>(CONSTANTS.SSAICascadeVisualization);
                var filterBuilder2 = Builders<BsonDocument>.Filter;
                var filter2 = filterBuilder2.Eq(CONSTANTS.CorrelationId, cascadeId) & filterBuilder2.Eq(CONSTANTS.UniqueId, uniqueId.Trim());
                var projection2 = Builders<BsonDocument>.Projection.Include(CONSTANTS.Visualization).Exclude(CONSTANTS.Id);
                var result = collection2.Find(filter2).Project<BsonDocument>(projection2).FirstOrDefault();

                var cascadeCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAICascadedModels);
                var filterBuilder1 = Builders<BsonDocument>.Filter;
                var filter1 = filterBuilder1.Eq(CONSTANTS.CascadedId, cascadeId);
                var projection1 = Builders<BsonDocument>.Projection.Include(CONSTANTS.IsCustomModel).Include(CONSTANTS.ModelList).Exclude(CONSTANTS.Id);
                var cascadeResult = cascadeCollection.Find(filter1).Project<BsonDocument>(projection1).FirstOrDefault();

                if (cascadeResult.Count() > 0)
                {
                    JObject cascademodel = JObject.Parse(cascadeResult[CONSTANTS.ModelList].ToString());
                    isCutomModel = Convert.ToBoolean(cascadeResult[CONSTANTS.IsCustomModel]);
                    string modelname = string.Format("Model{0}", cascademodel.Children().Count());
                    string modelname2 = string.Format("Model{0}", cascademodel.Children().Count() - 1);

                    if (isCutomModel)
                    {
                        modelDictionary = JsonConvert.DeserializeObject<CascadeModelDictionary>(cascademodel[modelname].ToString());
                        modelDictionary2 = JsonConvert.DeserializeObject<CascadeModelDictionary>(cascademodel[modelname2].ToString());
                        corids.Add(modelDictionary.CorrelationId);
                        corids.Add(modelDictionary2.CorrelationId);
                    }
                    else
                    {
                        modelDictionary = JsonConvert.DeserializeObject<CascadeModelDictionary>(cascademodel[modelname].ToString());
                        corids.Add(modelDictionary.CorrelationId);
                    }

                    if (cascademodel != null)
                    {
                        var collection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
                        var filter = Builders<DeployModelsDto>.Filter.In(CONSTANTS.CorrelationId, corids);
                        var projection = Builders<DeployModelsDto>.Projection.Include(CONSTANTS.CorrelationId).Include(CONSTANTS.Status).Include(CONSTANTS.ModelType).Exclude(CONSTANTS.Id);
                        deployModelsDtos = collection.Find(filter).Project<DeployModelsDto>(projection).ToList();
                    }
                }
                string problemType = string.Empty;
                var deployedModel2 = deployModelsDtos.Find(item => item.CorrelationId == modelDictionary.CorrelationId).Status;
                if (deployedModel2 == CONSTANTS.Deployed)
                {
                    problemType = deployModelsDtos.Find(item => item.CorrelationId == modelDictionary.CorrelationId).ModelType;
                }
                else
                {
                    problemType = deployModelsDtos.Find(item => item.CorrelationId == modelDictionary2.CorrelationId).ModelType;
                }

                if (result != null && deployModelsDtos.Count > 0)
                {
                    if (result[CONSTANTS.Visualization].GetType().ToString() == "MongoDB.Bson.BsonArray")
                    {
                        JArray data2 = JArray.Parse(result[CONSTANTS.Visualization].ToString());
                        int i = 0;
                        foreach (var item in data2[0].Children())
                        {
                            JProperty prop = item as JProperty;
                            if (i < 1)
                                PredictionProbability = JArray.Parse(prop.Value.ToString());
                            if (i > 0)
                                FeatureWeights = JObject.Parse(prop.Value.ToString());
                            i++;
                        }
                    }
                    else
                    {
                        JObject data2 = JObject.Parse(result[CONSTANTS.Visualization].ToString());
                        PredictionProbability = JArray.Parse(data2[CONSTANTS.PredictionProbability].ToString());
                        FeatureWeights = JObject.Parse(data2[CONSTANTS.FeatureWeights].ToString());
                    }
                    if (PredictionProbability.Count > 0)
                    {
                        //To form the table and add the values to the columns
                        foreach (var item in PredictionProbability)
                        {
                            JObject aa = new JObject();
                            aa.Add(item["IdName"].ToString(), item["Id"]);
                            if (problemType == CONSTANTS.Regression)
                            {
                                aa.Add(item["TargetName"].ToString(), item[CONSTANTS.Outcome][0]["value"].ToString());
                                //aa.Add("Target Probability", Convert.ToDouble(item[CONSTANTS.Outcome][0]["value"]));
                            }
                            else
                            {
                                aa.Add(item["TargetName"].ToString(), item[CONSTANTS.Categories][0]["name"].ToString());
                                aa.Add("Target Probability", item[CONSTANTS.Categories][0]["value"]);
                            }

                            foreach (var item2 in FeatureWeights[item["Id"].ToString()]["FeatureValues"].Children())
                            {
                                JProperty prop2 = item2 as JProperty;
                                if (prop2 != null && prop2.Name.ToString() != "DeployedTill")
                                {
                                    if (!aa.ContainsKey(prop2.Name))
                                        aa.Add(prop2.Name, prop2.Value);
                                }
                            }
                            listArray.Add(aa);
                        }
                    }
                    data.Data = listArray;
                }
            }
            catch (Exception ex)
            {
                data.IsException = true;
                data.ErrorMessage = ex.Message + "***STACKTRACE***" + ex.StackTrace;
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(CascadingService), nameof(ShowData), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(IngestedDataService), nameof(ShowData), "END UNIQID-" + uniqueId, string.IsNullOrEmpty(cascadeId) ? default(Guid) : new Guid(cascadeId), string.Empty, string.Empty, string.Empty, string.Empty);
            return data;
        }
        public FMVisualizationDTO GetFmVisualizeDetails(string ClientUID, string DCUID, string UserID, string Category)
        {
            fMVisualizationDTO.Category = Category;
            fMVisualizationDTO.ClientUID = ClientUID;
            fMVisualizationDTO.DCUID = DCUID;
            fMVisualizationDTO.UserId = UserID;
            string encrypteduser = UserID;
            if (!string.IsNullOrEmpty(Convert.ToString(UserID)))
                encrypteduser = _encryptionDecryption.Encrypt(Convert.ToString(UserID));
            //Taking the columnslist from PS_IngestedData collection
            List<IngestDataColumn> ingestData = new List<IngestDataColumn>();
            var collection2 = _database.GetCollection<IngestDataColumn>(CONSTANTS.PSIngestedData);
            var filter2 = Builders<IngestDataColumn>.Filter.Eq(CONSTANTS.CorrelationId, CONSTANTS.FMUseCaseId);
            var projection2 = Builders<IngestDataColumn>.Projection.Include(CONSTANTS.ColumnsList).Include(CONSTANTS.CorrelationId).Exclude(CONSTANTS.Id);
            ingestData = collection2.Find(filter2).Project<IngestDataColumn>(projection2).ToList();
            if (ingestData.Count > 0)
            {
                fMVisualizationDTO.ColumnsList = ingestData[0].ColumnsList;
            }

            //Checking for FM Visuaization available
            var collection = _database.GetCollection<FMVisualizationData>(CONSTANTS.SSAIFMVisualization);
            var filterBuilder = Builders<FMVisualizationData>.Filter;
            var filter = filterBuilder.Eq(CONSTANTS.ClientUID, ClientUID.Trim())
                & filterBuilder.Eq(CONSTANTS.DCUID, DCUID.Trim())
                & (filterBuilder.Eq(CONSTANTS.CreatedByUser, UserID.Trim()) | filterBuilder.Eq(CONSTANTS.CreatedByUser, encrypteduser.Trim()))
                & filterBuilder.Eq(CONSTANTS.Category, Category.Trim());
            var projection = Builders<FMVisualizationData>.Projection.Exclude(CONSTANTS.Id);
            var FmPredictionResults = collection.Find(filter).Project<FMVisualizationData>(projection).SortByDescending(bson => bson.CreatedOn).ToList();
            if (FmPredictionResults.Count > 0)
            {
                for (int i = 0; i < FmPredictionResults.Count; i++)
                {
                    fMVisualizationDTO.FMVisualizeData = JArray.Parse(FmPredictionResults[i].Visualization.ToString());
                }
                fMVisualizationDTO.CorrelationId = FmPredictionResults[0].CorrelationId;
                fMVisualizationDTO.ClientUID = FmPredictionResults[0].ClientUID;
                fMVisualizationDTO.DCUID = FmPredictionResults[0].DCUID;
                fMVisualizationDTO.UserId = FmPredictionResults[0].CreatedByUser;
                fMVisualizationDTO.Category = FmPredictionResults[0].Category;
                fMVisualizationDTO.IsFMDataAvaialble = true;
                DeployModelsDto deployModels = GetFMModelDetails(fMVisualizationDTO.CorrelationId);
                fMVisualizationDTO.FMCorrelationId = deployModels.FMCorrelationId;
            }
            return fMVisualizationDTO;
        }

        public FMVisualizationinProgress GetFMVisualizationinProgress(string ClientUID, string DCUID, string UserID, string Category)
        {
            FMVisualizationinProgress fMVisualizationinProgress = new FMVisualizationinProgress();
            fMVisualizationinProgress.Category = Category;
            fMVisualizationinProgress.ClientUID = ClientUID;
            fMVisualizationinProgress.DCUID = DCUID;
            fMVisualizationinProgress.UserId = UserID;
            string encrypteduser = UserID;
            if (!string.IsNullOrEmpty(Convert.ToString(UserID)))
                encrypteduser = _encryptionDecryption.Encrypt(Convert.ToString(UserID));

            var collection = _database.GetCollection<IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
            var filterBuilder = Builders<IngrainRequestQueue>.Filter;
            var filter = filterBuilder.Eq(CONSTANTS.ClientId, ClientUID.Trim()) & filterBuilder.Eq(CONSTANTS.DeliveryconstructId, DCUID.Trim()) & filterBuilder.Eq(CONSTANTS.Status, CONSTANTS.P) & filterBuilder.Eq(CONSTANTS.Function, CONSTANTS.FMTransform);
            var projection2 = Builders<IngrainRequestQueue>.Projection.Exclude(CONSTANTS.Id);
            var result = collection.Find(filter).Project<IngrainRequestQueue>(projection2).FirstOrDefault();
            if (result != null)
            {
                fMVisualizationinProgress.CorrelationId = result.CorrelationId;
                fMVisualizationinProgress.FMCorrelationId = result.FMCorrelationId;
                fMVisualizationinProgress.Progress = result.Progress;
                fMVisualizationinProgress.Status = result.Status;
                fMVisualizationinProgress.Message = result.Message;
            }


            return fMVisualizationinProgress;
        }

        public FMUploadResponse FmFileUpload(FMFileUpload fMFileUpload, IFormFileCollection fileCollection, out bool isException, out string ErrorMessage)
        {
            if (fMFileUpload.IsRefresh)
            {
                DeployModelsDto deployModels = GetFMModelDetails(fMFileUpload.CorrelationId);
                fMFileUpload.CorrelationId = deployModels.CorrelationId;
                fMFileUpload.FMCorrelationId = deployModels.FMCorrelationId;
            }
            else
            {
                fMFileUpload.CorrelationId = Guid.NewGuid().ToString();
                fMFileUpload.FMCorrelationId = Guid.NewGuid().ToString();
                fMFileUpload.ModelName = "RSP_" + fMFileUpload.CorrelationId;
            }
            string filePath = string.Empty;
            fMUploadResponse.CorrelationId = fMFileUpload.CorrelationId;
            fMUploadResponse.FMCorrelationId = fMFileUpload.FMCorrelationId;
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CascadingService), nameof(FmFileUpload), "START", string.IsNullOrEmpty(fMFileUpload.CorrelationId) ? default(Guid) : new Guid(fMFileUpload.CorrelationId), string.Empty, string.Empty, string.Empty, string.Empty);
            isException = false;
            fMUploadResponse.ClinetUID = fMFileUpload.ClientUID;
            fMUploadResponse.DCUID = fMFileUpload.DCUID;
            ErrorMessage = string.Empty;
            try
            {
                IFormFile file = fileCollection[0];
                filePath = appSettings.Value.UploadFilePath;
                Directory.CreateDirectory(Path.Combine(filePath, appSettings.Value.SavedModels));
                filePath = filePath + "//" + appSettings.Value.AppData;
                System.IO.Directory.CreateDirectory(filePath);
                if (file.Length <= 0)
                {
                    fMUploadResponse.ValidatonMessage = CONSTANTS.FileEmpty;
                    fMUploadResponse.IsUploaded = false;
                    fMUploadResponse.Message = CONSTANTS.FileEmpty;
                    fMUploadResponse.Status = CONSTANTS.E;
                    return fMUploadResponse;
                }
                string completePath = filePath + Path.GetFileName(fMFileUpload.CorrelationId + "_" + file.FileName);
                _encryptionDecryption.EncryptFile(file, filePath + Path.GetFileName(fMFileUpload.CorrelationId + "_" + file.FileName));
                var Extension = Path.GetExtension(completePath);
                if (Extension != ".xlsx" & Extension != ".csv")
                {
                    fMUploadResponse.ValidatonMessage = CONSTANTS.FileNotSupport;
                    fMUploadResponse.IsUploaded = false;
                    fMUploadResponse.Message = CONSTANTS.FileNotSupport;
                    fMUploadResponse.Status = CONSTANTS.E;
                    return fMUploadResponse;
                }
                FMUploadFile(fMFileUpload, fileCollection, completePath, file.FileName);
                if (fMUploadResponse.IsException)
                {
                    isException = true;
                    ErrorMessage = uploadResponse.ErrorMessage;
                    return fMUploadResponse;
                }

                CheckFMUploadData(fMFileUpload.CorrelationId, fMUploadResponse.RequestId, fMFileUpload);
            }
            catch (Exception ex)
            {
                fMUploadResponse.Status = CONSTANTS.E;
                isException = true;
                ErrorMessage = ex.Message + "***STACKTRACE***" + ex.StackTrace;
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(CascadingService), nameof(UploadData), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CascadingService), nameof(FmFileUpload), "END", string.IsNullOrEmpty(fMFileUpload.CorrelationId) ? default(Guid) : new Guid(fMFileUpload.CorrelationId), string.Empty, string.Empty, string.Empty, string.Empty);
            return fMUploadResponse;
        }
        private DeployModelsDto GetFMModelDetails(string correlationId)
        {
            DeployModelsDto deployModels = new DeployModelsDto();
            var collection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
            var filter = Builders<DeployModelsDto>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var projection = Builders<DeployModelsDto>.Projection.Exclude(CONSTANTS.Id);
            var result = collection.Find(filter).Project<DeployModelsDto>(projection).ToList();
            if (result.Count > 0)
            {
                deployModels = result[0];
            }
            return deployModels;
        }
        private void FMUploadFile(FMFileUpload fmFileUpload, IFormFileCollection fileCollection, string filePath, string fileDataSource)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(IngestedDataService), nameof(FMUploadFile), "-START-", string.IsNullOrEmpty(fmFileUpload.CorrelationId) ? default(Guid) : new Guid(fmFileUpload.CorrelationId), string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                IngrainRequestQueue ingrainRequest = null;
                bool DBEncryptionRequired = CommonUtility.EncryptDB(fmFileUpload.CorrelationId, appSettings);
                //if (DBEncryptionRequired)
                {
                    if (!string.IsNullOrEmpty(fmFileUpload.UserId))
                    {
                        fmFileUpload.UserId = _encryptionDecryption.Encrypt(Convert.ToString(fmFileUpload.UserId));
                    }
                }
                ingrainRequest = new IngrainRequestQueue
                {
                    _id = Guid.NewGuid().ToString(),
                    CorrelationId = fmFileUpload.CorrelationId,
                    DataSetUId = null,
                    RequestId = Guid.NewGuid().ToString(),
                    ProcessId = null,
                    FMCorrelationId = fmFileUpload.FMCorrelationId,
                    Status = null,
                    ModelName = fmFileUpload.ModelName,
                    RequestStatus = CONSTANTS.New,
                    RetryCount = 0,
                    ProblemType = null,
                    Message = null,
                    UniId = null,
                    Progress = null,
                    TemplateUseCaseID = CONSTANTS.FMUseCaseId,
                    pageInfo = CONSTANTS.IngestData,
                    ParamArgs = null,
                    Function = CONSTANTS.FileUpload,
                    CreatedByUser = fmFileUpload.UserId,
                    CreatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    ModifiedByUser = fmFileUpload.UserId,
                    ModifiedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    LastProcessedOn = null
                };
                _filepath = new Filepath();
                string postedFileName = string.Empty;
                if (fileCollection.Count > 0)
                {
                    if (appSettings.Value.IsAESKeyVault &&  appSettings.Value.Environment == CONSTANTS.PADEnvironment && appSettings.Value.EncryptUploadedFiles)
                    {
                        filePath = filePath + ".enc" + @"""";
                    }
                    else
                        filePath = filePath + @"""";
                    postedFileName = "[" + @"""" + filePath + "]";
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(IngestedDataService), nameof(FMUploadFile), "FILECOLLECTION POSTEDFILENAME: " + postedFileName + ", CORRELATIONID: " + fmFileUpload.CorrelationId, string.IsNullOrEmpty(fmFileUpload.CorrelationId) ? default(Guid) : new Guid(fmFileUpload.CorrelationId), string.Empty, string.Empty, string.Empty, string.Empty);
                }
                if (postedFileName != "")
                    _filepath.fileList = postedFileName;
                else
                    _filepath.fileList = "null";
                parentFile = new ParentFile();
                parentFile.Type = CONSTANTS.Null;
                parentFile.Name = CONSTANTS.Null;
                string MappingColumns = string.Empty;
                fileUpload = new FileUpload
                {
                    CorrelationId = fmFileUpload.CorrelationId,
                    ClientUID = fmFileUpload.ClientUID,
                    DeliveryConstructUId = fmFileUpload.DCUID,
                    Parent = parentFile,
                    Flag = CONSTANTS.Null,
                    mapping = MappingColumns,
                    mapping_flag = CONSTANTS.False,
                    pad = CONSTANTS.Null,
                    metric = CONSTANTS.Null,
                    InstaMl = CONSTANTS.Null,
                    fileupload = _filepath,
                    Customdetails = CONSTANTS.Null
                };
                if (fmFileUpload.IsRefresh)
                {
                    fileUpload.Flag = "Incremental";
                    ingrainRequest.UniId = Guid.NewGuid().ToString();
                }
                ingrainRequest.ParamArgs = fileUpload.ToJson();
                if (!fmFileUpload.IsRefresh)
                {
                    InsertDeployModels(fmFileUpload, fileDataSource);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(IngestedDataService), nameof(FMUploadFile), "-UNIQUEID-: " + ingrainRequest.UniId + "--RequestID-" + ingrainRequest.RequestId + "UPLOADRESPONSE UNIQUEID--" + fMUploadResponse.RequestId, string.IsNullOrEmpty(fmFileUpload.CorrelationId) ? default(Guid) : new Guid(fmFileUpload.CorrelationId), string.Empty, string.Empty, string.Empty, string.Empty);
                }
                fMUploadResponse.RequestId = ingrainRequest.RequestId;
                fMUploadResponse.UniqueId = ingrainRequest.UniId;
                InsertRequests(ingrainRequest);
            }
            catch (Exception ex)
            {
                fMUploadResponse.Status = CONSTANTS.E;
                fMUploadResponse.IsException = true;
                fMUploadResponse.ErrorMessage = ex.Message + "***STACKTRACE***" + ex.StackTrace;
            }
        }
        private void InsertDeployModels(FMFileUpload fMUpload, string fileDataSource)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CascadingService), nameof(InsertDeployModels), " START", string.IsNullOrEmpty(fMUpload.CorrelationId) ? default(Guid) : new Guid(fMUpload.CorrelationId), string.Empty, string.Empty, fMUpload.ClientUID, fMUpload.DCUID);
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CascadingService), nameof(InsertDeployModels), " START", string.IsNullOrEmpty(fMUpload.CorrelationId) ? default(Guid) : new Guid(fMUpload.CorrelationId), string.Empty, string.Empty, fMUpload.ClientUID, fMUpload.DCUID);
            string[] arr = new string[] { };
            DeployModelsDto deployModel = new DeployModelsDto
            {
                _id = Guid.NewGuid().ToString(),
                CorrelationId = fMUpload.CorrelationId,
                DataSetUId = null,
                InstaId = null,
                ModelName = fMUpload.ModelName,
                Status = CONSTANTS.InProgress,
                ClientUId = fMUpload.ClientUID,
                DeliveryConstructUID = fMUpload.DCUID,
                DataSource = fileDataSource,
                DeployedDate = null,
                LinkedApps = arr,
                ModelVersion = null,
                ModelType = null,
                SourceName = "file",
                VDSLink = null,
                InputSample = null,
                IsPrivate = true,
                IsModelTemplate = false,
                DBEncryptionRequired = true,
                TrainedModelId = null,
                Frequency = null,
                Category = fMUpload.Category,
                CreatedByUser = fMUpload.UserId,
                IsFMModel = true,
                HideFMModel = false,
                FMCorrelationId = fMUpload.FMCorrelationId,
                CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                ModifiedByUser = fMUpload.UserId,
                ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                Language = "english",
                IsModelTemplateDataSource = true
            };
            //Insert Actual FMModel
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
            var jsonColumns = Newtonsoft.Json.JsonConvert.SerializeObject(deployModel);
            var insertBsonColumns = BsonSerializer.Deserialize<BsonDocument>(jsonColumns);
            collection.InsertOne(insertBsonColumns);

            //Insert Duplicate Model(Moddel1) For FM Feature
            string model1Corid = deployModel.FMCorrelationId;
            string model2Corid = deployModel.CorrelationId;
            deployModel._id = Guid.NewGuid().ToString();
            deployModel.CorrelationId = model1Corid;
            deployModel.ModelName = deployModel.ModelName + "_1";
            deployModel.HideFMModel = true;
            deployModel.IsFMModel = false;
            deployModel.FMCorrelationId = model2Corid;
            var jsonColumns2 = Newtonsoft.Json.JsonConvert.SerializeObject(deployModel);
            var insertBsonColumns2 = BsonSerializer.Deserialize<BsonDocument>(jsonColumns2);
            collection.InsertOne(insertBsonColumns2);
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CascadingService), nameof(InsertDeployModels), " INSERTION COMPLETED START", string.IsNullOrEmpty(deployModel.CorrelationId) ? default(Guid) : new Guid(deployModel.CorrelationId), deployModel.AppId, string.Empty, deployModel.ClientUId, deployModel.DeliveryConstructUID);
        }
        private void CheckFMUploadData(string correlationId, string requestId, FMFileUpload fMUpload)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CascadingService), nameof(CheckFMUploadData), "START UNIQUEID--" + requestId, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, fMUpload.ClientUID, fMUpload.DCUID);
            IngrainRequestQueue ingrainRequest = new IngrainRequestQueue();
            bool isPythonSuccess = true;
            while (isPythonSuccess)
            {
                ingrainRequest = GetFMFileUploadRequestStatus(correlationId, requestId);
                if (ingrainRequest != null)
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(CascadingService), nameof(CheckFMUploadData), "CheckFMUploadData--" + ingrainRequest.Status + "-progress-" + ingrainRequest.Progress, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), ingrainRequest.AppID, string.Empty, ingrainRequest.ClientID, ingrainRequest.DeliveryconstructId);
                    if (ingrainRequest.Status == CONSTANTS.C & ingrainRequest.Progress == CONSTANTS.Hundred)
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(CascadingService), nameof(CheckFMUploadData), "FILEUPLOAD SUCCESS START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), ingrainRequest.AppID, string.Empty, ingrainRequest.ClientID, ingrainRequest.DeliveryconstructId);
                        if (fMUpload.IsRefresh)
                        {
                            fMUploadResponse.IsRefresh = true;
                            //Need to call python for Fm Prediction
                            string uniqId = FMVisualizeInsertRequest(fMUpload);
                            fMUploadResponse.ErrorMessage = ingrainRequest.Message;
                            fMUploadResponse.Status = ingrainRequest.Status;
                            fMUploadResponse.Message = CONSTANTS.success;
                            fMUploadResponse.IsUploaded = true;
                            fMUploadResponse.UniqueId = uniqId;
                            isPythonSuccess = false;
                        }
                        else
                        {
                            fMUploadResponse.ErrorMessage = ingrainRequest.Message;
                            fMUploadResponse.Status = ingrainRequest.Status;
                            fMUploadResponse.Message = CONSTANTS.success;
                            fMUploadResponse.IsUploaded = true;
                            isPythonSuccess = false;
                            //
                            //update psingestedData and ps_usecasedefinition
                            var psingestData = _database.GetCollection<BsonDocument>(CONSTANTS.PSIngestedData);
                            var psuseCase = _database.GetCollection<BsonDocument>(CONSTANTS.PSUseCaseDefinition);
                            var filter3 = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, fMUpload.CorrelationId);
                            var update = Builders<BsonDocument>.Update.Set(CONSTANTS.CorrelationId, fMUpload.FMCorrelationId);
                            psingestData.UpdateOne(filter3, update);
                            psuseCase.UpdateOne(filter3, update);
                            //  insertCascadePredictionRequest(ingrainRequest);

                            ProblemTypeDetails2 problemTypeDetails = null;
                            var collection2 = _database.GetCollection<ProblemTypeDetails2>(CONSTANTS.SSAIDeployedModels);
                            var filter22 = Builders<ProblemTypeDetails2>.Filter.Eq(CONSTANTS.CorrelationId, CONSTANTS.FMUseCaseId);
                            var projection2 = Builders<ProblemTypeDetails2>.Projection.Include(CONSTANTS.ModelType).Include(CONSTANTS.LinkedApps).Include("AppId").Exclude(CONSTANTS.Id);
                            problemTypeDetails = collection2.Find(filter22).Project<ProblemTypeDetails2>(projection2).FirstOrDefault();

                            if (problemTypeDetails == null)
                            { problemTypeDetails = new ProblemTypeDetails2(); }
                            problemTypeDetails.FMModel1CorId = fMUpload.FMCorrelationId;
                            //bool DBEncryptionRequired = CommonUtility.EncryptDB(problemTypeDetails.FMModel1CorId, appSettings);
                            //string userId = fMUpload.UserId;
                            //if (DBEncryptionRequired)
                            //{
                            //    if (!string.IsNullOrEmpty(fMUpload.UserId))
                            //    {
                            //        userId = _encryptionDecryption.Encrypt(Convert.ToString(fMUpload.UserId));
                            //    }
                            //}
                            IngrainRequestQueue ingrainRequest2 = new IngrainRequestQueue
                            {
                                _id = Guid.NewGuid().ToString(),
                                CorrelationId = problemTypeDetails.FMModel1CorId,
                                FMCorrelationId = fMUpload.CorrelationId,
                                RequestId = Guid.NewGuid().ToString(),
                                ProcessId = CONSTANTS.Null,
                                IsFMVisualize = true,
                                ModelName = fMUpload.ModelName,
                                Category = fMUpload.Category,
                                Status = CONSTANTS.Null,
                                RequestStatus = CONSTANTS.New,
                                Message = CONSTANTS.Null,
                                RetryCount = 0,
                                AppID = problemTypeDetails.AppId,
                                ProblemType = problemTypeDetails.ModelType,
                                UniId = Guid.NewGuid().ToString(),
                                ClientID = fMUpload.ClientUID,
                                DCID = fMUpload.DCUID,
                                InstaID = CONSTANTS.Null,
                                Progress = CONSTANTS.Null,
                                pageInfo = CONSTANTS.FMTransform,
                                ParamArgs = CONSTANTS.Null,
                                TemplateUseCaseID = CONSTANTS.FMUseCaseId,
                                Function = CONSTANTS.FMTransform,
                                CreatedByUser = fMUpload.UserId,
                                CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                                ModifiedByUser = CONSTANTS.Null,
                                ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                                LastProcessedOn = CONSTANTS.Null,
                                ClientId = fMUpload.ClientUID,
                                DeliveryconstructId = fMUpload.DCUID,
                                UseCaseID = CONSTANTS.Null,
                                EstimatedRunTime = CONSTANTS.Null
                            };
                            if (problemTypeDetails != null)
                            {
                                if (problemTypeDetails.LinkedApps != null)
                                {
                                    if (problemTypeDetails.LinkedApps.Length > 0)
                                    {
                                        ingrainRequest.ApplicationName = problemTypeDetails.LinkedApps[0];
                                    }
                                }
                            }
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CascadingService), nameof(CheckFMUploadData), "FMTransform INSERT START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, fMUpload.ClientUID, fMUpload.DCUID);
                            InsertRequests(ingrainRequest2);
                        }
                    }
                    else if (ingrainRequest.Status == CONSTANTS.E)
                    {
                        fMUploadResponse.ErrorMessage = ingrainRequest.Message;
                        fMUploadResponse.Status = ingrainRequest.Status;
                        fMUploadResponse.Message = ingrainRequest.Message;
                        fMUploadResponse.IsUploaded = false;
                        isPythonSuccess = false;
                    }
                    else
                    {
                        Thread.Sleep(2000);
                    }
                }
            }
        }

        private string FMVisualizeInsertRequest(FMFileUpload result)
        {
            //bool DBEncryptionRequired = CommonUtility.EncryptDB(result.FMCorrelationId, appSettings);
            //string userId = result.UserId;
            //if (DBEncryptionRequired)
            //{
            //    if (!string.IsNullOrEmpty(result.UserId))
            //    {
            //        userId = _encryptionDecryption.Encrypt(Convert.ToString(result.UserId));
            //    }
            //}
            IngrainRequestQueue ingrainRequest = new IngrainRequestQueue
            {
                _id = Guid.NewGuid().ToString(),
                CorrelationId = result.FMCorrelationId,
                FMCorrelationId = result.CorrelationId,
                RequestId = Guid.NewGuid().ToString(),
                ProcessId = CONSTANTS.Null,
                ModelName = result.ModelName,
                Status = CONSTANTS.Null,
                RequestStatus = CONSTANTS.New,
                Message = CONSTANTS.Null,
                RetryCount = 0,
                ProblemType = CONSTANTS.Null,
                UniId = fMUploadResponse.UniqueId,
                InstaID = CONSTANTS.Null,
                Progress = CONSTANTS.Null,
                pageInfo = CONSTANTS.PublishModel,
                ParamArgs = CONSTANTS.Null,
                TemplateUseCaseID = null,
                Function = CONSTANTS.FMVisualize,
                CreatedByUser = result.UserId,
                CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                ModifiedByUser = CONSTANTS.Null,
                ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                LastProcessedOn = CONSTANTS.Null,
                ClientId = result.ClientUID,
                DeliveryconstructId = result.DCUID,
                UseCaseID = CONSTANTS.Null,
                EstimatedRunTime = CONSTANTS.Null
            };
            if (result.IsRefresh)
            {
                ingrainRequest.CorrelationId = result.CorrelationId;
                ingrainRequest.FMCorrelationId = result.FMCorrelationId;
            }
            FMVisualization fMVisualization = new FMVisualization()
            {
                CorrelationId = ingrainRequest.CorrelationId,
                IsIncremental = true,
                Category = result.Category,
                ClientUID = result.ClientUID,
                DCUID = result.DCUID,
                UniqueId = ingrainRequest.UniId
            };
            InsertFMRequests(fMVisualization);
            InsertRequests(ingrainRequest);
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CascadingService), nameof(FMVisualizeInsertRequest), "FMVisualizeInsertRequest request inserted END", string.IsNullOrEmpty(result.FMCorrelationId) ? default(Guid) : new Guid(result.FMCorrelationId), string.Empty, string.Empty, result.ClientUID, result.DCUID);
            Thread.Sleep(2000);
            return ingrainRequest.UniId;
        }
        private void InsertFMRequests(FMVisualization fmData)
        {
            var requestQueue = JsonConvert.SerializeObject(fmData);
            var insertRequestQueue = BsonSerializer.Deserialize<BsonDocument>(requestQueue);
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIFMVisualization);
            collection.InsertOne(insertRequestQueue);
        }
        public FMVisualizeModelTraining GetFMModelTrainingStatus(string correlationId, string FMCorrelationId, string userId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CascadingService), nameof(GetFMModelTrainingStatus), "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            FMVisualizeModelTraining fMVisualize = new FMVisualizeModelTraining();
            fMVisualize.CorrelationId = correlationId;
            fMVisualize.FmCorrelationId = FMCorrelationId;

            fMVisualize.IsModel1Completed = false;
            fMVisualize.IsModel2Completed = false;
            var collection = _database.GetCollection<IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
            var filterBuilder = Builders<IngrainRequestQueue>.Filter;
            var filter = filterBuilder.Eq(CONSTANTS.CorrelationId, FMCorrelationId) & filterBuilder.Eq(CONSTANTS.FMCorrelationId, correlationId.Trim()) & filterBuilder.Eq(CONSTANTS.Function, CONSTANTS.FMTransform);
            var projection2 = Builders<IngrainRequestQueue>.Projection.Exclude(CONSTANTS.Id);
            var result = collection.Find(filter).Project<IngrainRequestQueue>(projection2).FirstOrDefault();
            if (result != null)
            {
                fMVisualize.ModelName = result.ModelName;
                //fMVisualize.Category = correlationId;
                fMVisualize.ClinetUID = result.ClientId;
                fMVisualize.DCUID = result.DeliveryconstructId;
                fMVisualize.UserId = userId;
                switch (result.Progress)
                {
                    case "15%":
                        fMVisualize.ProcessName = "DE";
                        fMVisualize.Status = CONSTANTS.P;
                        fMVisualize.Progress = "30";
                        fMVisualize.UniqueId = result.UniId;
                        break;
                    case "20%":
                        fMVisualize.ProcessName = "ME";
                        fMVisualize.Status = CONSTANTS.P;
                        fMVisualize.Progress = "50";
                        break;
                    case "25%":
                        fMVisualize.ProcessName = "DeployModel";
                        fMVisualize.Status = CONSTANTS.C;
                        fMVisualize.IsModel1Completed = true;
                        fMVisualize.Progress = "100";
                        fMVisualize.UniqueId = result.UniId;
                        break;
                    case "65%":
                        fMVisualize.ProcessName = "DE";
                        fMVisualize.Status = CONSTANTS.P;
                        fMVisualize.Progress = "20";
                        fMVisualize.IsModel1Completed = true;
                        fMVisualize.UniqueId = result.UniId;
                        break;
                    case "80%":
                        fMVisualize.ProcessName = "ME";
                        fMVisualize.Status = CONSTANTS.P;
                        fMVisualize.Progress = "40";
                        fMVisualize.IsModel1Completed = true;
                        fMVisualize.UniqueId = result.UniId;
                        break;
                    case "85%":
                        fMVisualize.ProcessName = "DeployModel";
                        fMVisualize.Status = CONSTANTS.P;
                        fMVisualize.Progress = "70";
                        fMVisualize.IsModel1Completed = true;
                        fMVisualize.UniqueId = result.UniId;
                        break;
                    case "100%":
                        fMVisualize.ProcessName = "ModelDeployed";
                        fMVisualize.Status = CONSTANTS.C;
                        fMVisualize.Progress = "100";
                        fMVisualize.IsModel1Completed = true;
                        fMVisualize.IsModel2Completed = true;
                        fMVisualize.UniqueId = result.UniId;
                        var predictionResult = GetFMPrediction(result.FMCorrelationId, result.UniId);
                        if (predictionResult != null)
                        {
                            fMVisualize.FMVisualizationData = predictionResult.FMVisualizationData;
                        }
                        break;
                }
                if (result.Status == CONSTANTS.E)
                {
                    fMVisualize.Status = CONSTANTS.E;
                    fMVisualize.Message = result.Message;
                    fMVisualize.ErrorMessage = result.Message;
                    DeleteDeployModel(fMVisualize.CorrelationId);
                    DeleteDeployModel(fMVisualize.FmCorrelationId);
                }
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CascadingService), nameof(GetFMModelTrainingStatus), "END", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, fMVisualize.ClinetUID, fMVisualize.DCUID);
            return fMVisualize;
        }
        private void DeleteDeployModel(string CorrelationId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CascadingService), nameof(DeleteDeployModel), "Error Occurs during Training - CorrelationId" + CorrelationId, string.IsNullOrEmpty(CorrelationId) ? default(Guid) : new Guid(CorrelationId), string.Empty, string.Empty, string.Empty, string.Empty);
            var deloymodelCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_DeployedModels);
            var filterBuilder = Builders<BsonDocument>.Filter;
            var filterQueue1 = filterBuilder.Eq("CorrelationId", CorrelationId);
            deloymodelCollection.DeleteMany(filterQueue1);
        }
        public FMPredictionResult GetFMPrediction(string correlationId, string UniqId)
        {
            FMPredictionResult fMPrediction = new FMPredictionResult();
            var collection = _database.GetCollection<IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
            var filterBuilder = Builders<IngrainRequestQueue>.Filter;
            var filter = filterBuilder.Eq(CONSTANTS.CorrelationId, correlationId) & filterBuilder.Eq(CONSTANTS.UniId, UniqId.Trim()) & filterBuilder.Eq(CONSTANTS.Function, CONSTANTS.FMVisualize);
            var projection2 = Builders<IngrainRequestQueue>.Projection.Exclude(CONSTANTS.Id);
            var result = collection.Find(filter).Project<IngrainRequestQueue>(projection2).FirstOrDefault();
            bool DBEncryptionRequired = CommonUtility.EncryptDB(correlationId, appSettings);
            if (DBEncryptionRequired)
            {
                try
                {
                    if (!string.IsNullOrEmpty(Convert.ToString(result.CreatedByUser)))
                        result.CreatedByUser = _encryptionDecryption.Decrypt(Convert.ToString(result.CreatedByUser));
                }
                catch (Exception ex)
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(CascadingService), nameof(GetFMPrediction), ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty);
                }
            }
            if (result != null)
            {
                fMPrediction.Status = result.Status;
                fMPrediction.Progress = result.Progress;
                fMPrediction.Message = result.Message;
                fMPrediction.ErrorMessage = result.Message;
                fMPrediction.CorrelationId = correlationId;
                fMPrediction.FmCorrelationId = result.FMCorrelationId;
                fMPrediction.ClinetUID = result.ClientId;
                fMPrediction.DCUID = result.DeliveryconstructId;
                fMPrediction.UniqueId = UniqId;
                fMPrediction.UserId = result.CreatedByUser;
                if (result.Status == CONSTANTS.C && result.Progress == CONSTANTS.Hundred)
                {
                    var collection2 = _database.GetCollection<FMPredictionData>(CONSTANTS.SSAIFMVisualization);
                    var filterBuilder2 = Builders<FMPredictionData>.Filter;
                    var filter2 = filterBuilder2.Eq(CONSTANTS.CorrelationId, correlationId.Trim())
                        & filterBuilder2.Eq(CONSTANTS.UniqueId, UniqId.Trim());
                    var projection = Builders<FMPredictionData>.Projection.Include("Visualization").Include(CONSTANTS.UniqueId).Include(CONSTANTS.CreatedOn).Exclude(CONSTANTS.Id);
                    var FmPredictionResults = collection2.Find(filter2).Project<FMPredictionData>(projection).ToList();
                    if (FmPredictionResults.Count > 0)
                    {
                        for (int i = 0; i < FmPredictionResults.Count; i++)
                        {
                            fMPrediction.FMVisualizationData = JArray.Parse(FmPredictionResults[i].Visualization.ToString());
                        }
                    }
                }
                else if (result.Status == CONSTANTS.E)
                {
                    return fMPrediction;
                }
                else if (result.Status == CONSTANTS.Null)
                {
                    fMPrediction.Status = CONSTANTS.P;
                    fMPrediction.Progress = "1";
                    fMPrediction.Message = "Prediction inprogress";
                    return fMPrediction;
                }
            }
            return fMPrediction;
        }
        public bool CascadedEncryptDB(string CascadedId)
        {
            var collection = _database.GetCollection<BsonDocument>("SSAI_DeployedModels");
            var filter = Builders<BsonDocument>.Filter.Eq("CascadedId", CascadedId);
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
    }
}
