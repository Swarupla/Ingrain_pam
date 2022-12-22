#region Copyright (c) 2019 Accenture . All rights reserved.

// <copyright company="Accenture">
// Copyright (c) 2019 Accenture.  All rights reserved.
// Reproduction or transmission in whole or in part, in any form or by any means, 
// electronic, mechanical or otherwise, is prohibited without the prior written 
// consent of the copyright owner.
// </copyright>

#endregion
using Accenture.MyWizard.Cryptography.EncryptionProviders;
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
using System.Configuration;
using System.Linq;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
using CryptographyHelper = Accenture.MyWizard.Cryptography;

namespace Accenture.MyWizard.SelfServiceAI.BusinessDomain.Services
{
    public class HyperTuneService : IHyperTune
    {
        #region Members
        private MongoClient _mongoClient;
        private IMongoDatabase _database;
        private readonly IOptions<IngrainAppSettings> appSettings;
        DatabaseProvider databaseProvider;
        static CryptographyHelper.Utility.CryptographyUtility CryptographyUtility = null;
        #endregion

        #region Constructor
        public HyperTuneService(DatabaseProvider db, IOptions<IngrainAppSettings> settings)
        {
            databaseProvider = db;
            appSettings = settings;
            _mongoClient = databaseProvider.GetDatabaseConnection();
            var dataBaseName = MongoUrl.Create(appSettings.Value.connectionString).DatabaseName;
            _database = _mongoClient.GetDatabase(dataBaseName);
            CryptographyUtility = new CryptographyHelper.Utility.CryptographyUtility();
        }
        #endregion
        public HyperParametersDTO GetHyperTuneData(string modelName, string correlationId)
        {
            HyperParametersDTO hyperParameters = new HyperParametersDTO();
            List<FloatHyperParameters> floatHyperParametersList = new List<FloatHyperParameters>();
            List<IntegerHyperParameters> integerHyperParametersList = new List<IntegerHyperParameters>();
            List<SavedHyperVersions> hyperVersionsList = new List<SavedHyperVersions>();
            var masterCollection = _database.GetCollection<BsonDocument>("MLDL_ModelsMaster");
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq("CorrelationId", correlationId) & builder.Eq("modelName", modelName);
            var emptyFilter = Builders<BsonDocument>.Filter.Empty;
            string model = string.Format("HyperTuneParams.{0}", modelName);
            var projection = Builders<BsonDocument>.Projection.Include("HyperTuneParams").Exclude("_id");
            var masterData = masterCollection.Find(emptyFilter).Project<BsonDocument>(projection).ToList();

            var recommededCollcetion = _database.GetCollection<BsonDocument>("SSAI_RecommendedTrainedModels");
            var defaultValueResult = recommededCollcetion.Find(filter).Project<BsonDocument>(Builders<BsonDocument>.Projection.Include("ModelParams").Exclude("_id")).ToList();
            JObject recommendedJObject = new JObject();
            if (defaultValueResult.Count > 0)
            {
                recommendedJObject = JObject.Parse(defaultValueResult[0].ToString());
            }

            JObject masterResults = new JObject();
            Dictionary<string, Dictionary<string, string>> stringAttributes = new Dictionary<string, Dictionary<string, string>>();

            if (masterData.Count > 0)
            {
                masterResults = JObject.Parse(masterData[0].ToString());
                foreach (JToken result in masterResults["HyperTuneParams"][modelName].Children())
                {
                    JProperty jProperty = result as JProperty;
                    Dictionary<float, float> keyValues = new Dictionary<float, float>();
                    if (jProperty.Value.Count() > 1)
                    {
                        foreach (JToken jToken in jProperty.Children())
                        {
                            if (jToken != null)
                            {
                                var datatype = jToken[0].Type;
                                var minValue = jToken[0];
                                int count = jToken.Count();
                                var maxValue = jToken[count - 1];
                                switch (datatype.ToString())
                                {
                                    case "Float":
                                        FloatHyperParameters attributes = new FloatHyperParameters
                                        {
                                            MinValue = minValue.ToObject<float>(),
                                            MaxValue = maxValue.ToObject<float>(),
                                            AttributeName = jProperty.Name.ToString()
                                        };
                                        if (recommendedJObject.Count > 0)
                                        {
                                            var value = recommendedJObject["ModelParams"][jProperty.Name.ToString()];
                                            if (value != null)
                                                attributes.DefaultValue = value.ToObject<float>();
                                        }

                                        floatHyperParametersList.Add(attributes);
                                        break;
                                    case "Integer":
                                        {
                                            IntegerHyperParameters attributes2 = new IntegerHyperParameters
                                            {
                                                MinValue = minValue.ToObject<Int32>(),
                                                MaxValue = maxValue.ToObject<Int32>(),
                                                AttributeName = jProperty.Name.ToString()
                                            };
                                            if (recommendedJObject.Count > 0)
                                            {
                                                var value = recommendedJObject["ModelParams"][jProperty.Name.ToString()];
                                                if (value != null)
                                                    attributes2.DefaultValue = value.ToObject<Int32>();
                                            }

                                            integerHyperParametersList.Add(attributes2);
                                            break;
                                        }
                                    case "String":
                                        {
                                            Dictionary<string, string> stringValues = new Dictionary<string, string>();
                                            string defaultValue = string.Empty;
                                            if (recommendedJObject.Count > 0)
                                            {
                                                var value = recommendedJObject["ModelParams"][jProperty.Name.ToString()];
                                                if (value != null)
                                                    defaultValue = value.ToString();
                                            }
                                            foreach (var values in jToken.Children())
                                            {
                                                if (values.ToString() == defaultValue)
                                                {
                                                    stringValues.Add(values.ToString(), "True");
                                                }
                                                else
                                                {
                                                    stringValues.Add(values.ToString(), "False");
                                                }
                                            }
                                            stringAttributes.Add(jProperty.Name.ToString(), stringValues);
                                            break;
                                        }
                                }
                            }
                        }
                    }
                    else
                    {
                        switch (jProperty.Value.Type.ToString())
                        {
                            case "String":
                                {
                                    Dictionary<string, string> stringValues = new Dictionary<string, string>();
                                    string defaultValue = string.Empty;
                                    if (recommendedJObject.Count > 0)
                                    {
                                        var value = recommendedJObject["ModelParams"][jProperty.Name.ToString()];
                                        if (value != null)
                                            defaultValue = value.ToString();
                                    }
                                    if (jProperty.Value.ToString() == defaultValue)
                                    {
                                        stringValues.Add(jProperty.Value.ToString(), "True");
                                    }
                                    else
                                    {
                                        stringValues.Add(jProperty.Value.ToString(), "False");
                                    }

                                    stringAttributes.Add(jProperty.Name.ToString(), stringValues);
                                    break;
                                }
                            case "Integer":
                                {
                                    IntegerHyperParameters attributes = new IntegerHyperParameters
                                    {
                                        MinValue = 0,
                                        MaxValue = jProperty.Value.ToObject<Int32>(),
                                        AttributeName = jProperty.Name.ToString()
                                    };
                                    if (recommendedJObject.Count > 0)
                                    {
                                        var value = recommendedJObject["ModelParams"][jProperty.Name.ToString()];
                                        if (value != null)
                                            attributes.DefaultValue = value.ToObject<Int32>();
                                    }

                                    integerHyperParametersList.Add(attributes);
                                    break;
                                }
                            case "Float":
                                {
                                    FloatHyperParameters attributes = new FloatHyperParameters
                                    {
                                        MinValue = 0,
                                        MaxValue = jProperty.Value.ToObject<float>(),
                                        AttributeName = jProperty.Name.ToString()
                                    };
                                    if (recommendedJObject.Count > 0)
                                    {
                                        var value = recommendedJObject["ModelParams"][jProperty.Name.ToString()];
                                        if (value != null)
                                            attributes.DefaultValue = value.ToObject<float>();
                                    }
                                    floatHyperParametersList.Add(attributes);
                                    break;
                                }
                        }
                    }
                }
            }

            //Get the Saved Hypertune Versions Start
            var hypermodelCollection = _database.GetCollection<BsonDocument>("ME_HyperTuneVersion");
            var builder2 = Builders<BsonDocument>.Filter;
            var savedHyperFilter = builder2.Eq("CorrelationId", correlationId) & builder2.Eq("Temp", true);

            var savedHyperProjection = Builders<BsonDocument>.Projection.Include("CorrelationId").Include("HTId").Include("VersionName").Exclude("_id");
            var savedHyperVersionResult = hypermodelCollection.Find(savedHyperFilter).Project<BsonDocument>(savedHyperProjection).ToList();
            if (savedHyperVersionResult.Count > 0)
            {
                for (int i = 0; i < savedHyperVersionResult.Count; i++)
                {
                    SavedHyperVersions hyperVersions = new SavedHyperVersions();
                    var parse = JObject.Parse(savedHyperVersionResult[i].ToString());
                    hyperVersions.CorrelationId = parse["CorrelationId"].ToString();
                    hyperVersions.HTId = parse["HTId"].ToString();
                    hyperVersions.VersionName = parse["VersionName"].ToString();
                    hyperVersionsList.Add(hyperVersions);
                }
            }
            //Get the Saved Hypertune Versions Start
            hyperParameters.IntegerHyperParameters = integerHyperParametersList;
            hyperParameters.FloatHyperParameters = floatHyperParametersList;
            hyperParameters.StringHyperParameters = stringAttributes;
            hyperParameters.SavedHyperVersions = hyperVersionsList;
            List<string> list = new List<string>();
            list = CommonUtility.GetDataSourceModel(correlationId, appSettings);
            if (list.Count > 0)
            {
                hyperParameters.ModelName = list[0];
                hyperParameters.DataSource = list[1];
                hyperParameters.ModelType = list[2];
                if (list.Count > 2)
                {
                    hyperParameters.BusinessProblem = list[3];
                    hyperParameters.Category = list[5];
                }
            }
            return hyperParameters;
        }

        public void PostHyperTuning(HyperTuningDTO data)
        {
            bool DBEncryptionRequired = CommonUtility.EncryptDB(data.CorrelationId, appSettings);
            if (DBEncryptionRequired)
            {
                if (!string.IsNullOrEmpty(Convert.ToString(data.CreatedByUser)))
                    data.CreatedByUser =appSettings.Value.IsAESKeyVault ? CryptographyUtility.Encrypt(Convert.ToString(data.CreatedByUser)) : AesProvider.Encrypt(Convert.ToString(data.CreatedByUser), appSettings.Value.aesKey, appSettings.Value.aesVector);
                if (!string.IsNullOrEmpty(Convert.ToString(data.ModifiedByUser)))
                    data.ModifiedByUser = appSettings.Value.IsAESKeyVault ? CryptographyUtility.Encrypt(Convert.ToString(data.ModifiedByUser)): AesProvider.Encrypt(Convert.ToString(data.ModifiedByUser), appSettings.Value.aesKey, appSettings.Value.aesVector);

            }
            var filter = Builders<BsonDocument>.Filter.Eq("CorrelationId", data.CorrelationId);
            data._id = Guid.NewGuid().ToString();
            var collection = _database.GetCollection<BsonDocument>("ME_HyperTuneVersion");
            var jsonColumns = Newtonsoft.Json.JsonConvert.SerializeObject(data);
            var insertBsonColumns = BsonSerializer.Deserialize<BsonDocument>(jsonColumns);
            collection.InsertOne(insertBsonColumns);
        }

        /// <summary>
        /// Get the hyper tuned trained model
        /// </summary>
        /// <param name="correlationId">The corrrelation identifier</param>
        /// <param name="hyperTuneId">The hypertune identifier</param>
        /// <param name="versionName">The version name</param>
        /// <returns>Returns the record</returns>
        public HyperTuningTrainedModel GetHyperTunedTrainedModels(string correlationId, string hyperTuneId, string versionName)
        {
            bool DBEncryptionRequired = CommonUtility.EncryptDB(correlationId, appSettings);
            List<JObject> trainModelsList = new List<JObject>();
            HyperTuningTrainedModel trainedModels = new HyperTuningTrainedModel();
            var columnCollection = _database.GetCollection<BsonDocument>("ME_HyperTuneVersion");
            var filterBuilder = Builders<BsonDocument>.Filter;
            var filter = string.IsNullOrEmpty(versionName) ? (filterBuilder.Eq("CorrelationId", correlationId) & filterBuilder.Eq("HTId", hyperTuneId)) :
                (filterBuilder.Eq("CorrelationId", correlationId) & filterBuilder.Eq("HTId", hyperTuneId) & filterBuilder.Eq("VersionName", versionName));
            var projection = Builders<BsonDocument>.Projection.Exclude("_id");
            var trainedModel = columnCollection.Find(filter).Project<BsonDocument>(projection).ToList();
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
                                trainedModel[i][CONSTANTS.CreatedByUser] = appSettings.Value.IsAESKeyVault ? CryptographyUtility.Decrypt(Convert.ToString(trainedModel[i][CONSTANTS.CreatedByUser])): AesProvider.Decrypt(Convert.ToString(trainedModel[i][CONSTANTS.CreatedByUser]), appSettings.Value.aesKey, appSettings.Value.aesVector);
                            }
                        }
                        catch (Exception ex) { LOGGING.LogManager.Logger.LogProcessInfo(typeof(HyperTuneService), nameof(GetHyperTunedTrainedModels) + "User is already decrypted....", ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty); }
                        try
                        {
                            if (trainedModel[i].Contains(CONSTANTS.ModifiedByUser) && !string.IsNullOrEmpty(Convert.ToString(trainedModel[i][CONSTANTS.ModifiedByUser])))
                            {
                                trainedModel[i][CONSTANTS.ModifiedByUser] = appSettings.Value.IsAESKeyVault ? CryptographyUtility.Decrypt(Convert.ToString(trainedModel[i][CONSTANTS.ModifiedByUser])): AesProvider.Decrypt(Convert.ToString(trainedModel[i][CONSTANTS.ModifiedByUser]), appSettings.Value.aesKey, appSettings.Value.aesVector);
                            }
                        }
                        catch (Exception ex) { LOGGING.LogManager.Logger.LogProcessInfo(typeof(HyperTuneService), nameof(GetHyperTunedTrainedModels) + "User is already decrypted....", ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty); }
                    }
                    trainModelsList.Add(JObject.Parse(trainedModel[i].ToString()));
                    if (trainedModel[i].AsBsonDocument.TryGetElement("CPUUsage", out BsonElement bson))
                        trainedModels.CPUUsage = Convert.ToDouble(trainedModel[0]["CPUUsage"]);
                }
                trainedModels.TrainedModel = trainModelsList;
            }

            List<string> datasource = new List<string>();
            datasource = CommonUtility.GetDataSourceModel(correlationId, appSettings);
            if (datasource.Count > 0)
            {
                trainedModels.ModelName = datasource[0];
                trainedModels.DataSource = datasource[1];
                trainedModels.ModelType = datasource[2];
                trainedModels.BusinessProblem = datasource.Count > 2 ? datasource[3] : null;
                trainedModels.Category = datasource[5];
            }

            return trainedModels;
        }

        /// <summary>
        /// Saves the hyper tune version
        /// </summary>
        /// <param name="data">The data</param>
        /// <param name="correlationId">The correlation identifier</param>
        /// <param name="hyperTuneId">The hyper tune identifier</param>  
        public void SaveHyperTuneVersion(dynamic data, string correlationId, string hyperTuneId)
        {
            var collection = _database.GetCollection<BsonDocument>("ME_HyperTuneVersion");
            var filterBuilder = Builders<BsonDocument>.Filter;
            var filter = filterBuilder.Eq("CorrelationId", correlationId) & filterBuilder.Eq("HTId", hyperTuneId);
            var project = Builders<BsonDocument>.Projection.Include("CorrelationId").Include("HTId").Include("VersionName").Include("Temp").Exclude("_id");
            var result = collection.Find(filter).Project<BsonDocument>(project).ToList();
            if (result.Count > 0)
            {
                if (data != null)
                {
                    List<UpdateDefinition<BsonDocument>> updateList = new List<UpdateDefinition<BsonDocument>>
                    {
                        Builders<BsonDocument>.Update.Set("Temp", true),
                        Builders<BsonDocument>.Update.Set("VersionName", data.VersionName.ToString())
                    };
                    var update = Builders<BsonDocument>.Update.Combine(updateList);
                    var outcome = collection.UpdateMany(filter, update);
                }
            }
        }

        /// <summary>
        /// Gets the hyper tuning versions
        /// </summary>
        /// <param name="correlationId">The correlation identifier</param>
        /// <param name="hyperTuneId">The hyper tune identifier</param>
        /// <returns>Returns the result.</returns>
        public HyperTuningDTO GetHyperTuningVersions(string correlationId, string hyperTuneId)
        {
            HyperTuningDTO data = new HyperTuningDTO();
            var dbCollection = _database.GetCollection<BsonDocument>("ME_HyperTuneVersion");
            var filterBuilder = Builders<BsonDocument>.Filter;
            var filterScenario = filterBuilder.Eq("CorrelationId", correlationId) & filterBuilder.Eq("Temp", true);
            var projectionScenario = Builders<BsonDocument>.Projection.Include("CorrelationId").Include("HTId").Include("VersionName").Include("ModelName").Exclude("_id");
            var dbData = dbCollection.Find(filterScenario).Project<BsonDocument>(projectionScenario).ToList();
            List<JObject> lstHyperTunedVersionData = new List<JObject>();
            if (dbData.Count > 0)
            {
                for (int i = 0; i < dbData.Count; i++)
                {
                    lstHyperTunedVersionData.Add(JObject.Parse(dbData[i].ToString()));
                }
                data.HyperTunedVersionData = lstHyperTunedVersionData;
            }
            return data;
        }
        public void InsertUsage(double CPUUsage, string CorrelationId, string HTId)
        {
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.ME_HyperTuneVersion);
            var filterBuilder = Builders<BsonDocument>.Filter;
            var filter = filterBuilder.Eq(CONSTANTS.CorrelationId, CorrelationId) & filterBuilder.Eq("HTId", HTId);
            var cpuUsageUpdate = Builders<BsonDocument>.Update.Set("CPUUsage", CPUUsage);
            collection.UpdateOne(filter, cpuUsageUpdate);
        }
    }
}
