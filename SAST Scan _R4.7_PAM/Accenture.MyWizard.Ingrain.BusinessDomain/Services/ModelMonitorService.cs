using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
using Accenture.MyWizard.Ingrain.DataAccess;
using Accenture.MyWizard.Shared.Helpers;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Accenture.MyWizard.Ingrain.BusinessDomain.Common;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
using CryptographyHelper = Accenture.MyWizard.Cryptography;
using System;
using Accenture.MyWizard.Cryptography.EncryptionProviders;

namespace Accenture.MyWizard.Ingrain.BusinessDomain.Services
{
    public class ModelMonitorService : IModelMonitorService
    {
        private readonly MongoClient _mongoClient;
        private readonly IMongoDatabase _database;
        private readonly WebHelper webHelper;
        private readonly DatabaseProvider databaseProvider;
        private readonly IOptions<IngrainAppSettings> appSettings;
        static CryptographyHelper.Utility.CryptographyUtility CryptographyUtility = null;
        public ModelMonitorService(DatabaseProvider db, IOptions<IngrainAppSettings> settings)
        {
            databaseProvider = db;
            _mongoClient = databaseProvider.GetDatabaseConnection();
            var dataBaseName = MongoUrl.Create(settings.Value.connectionString).DatabaseName;
            _database = _mongoClient.GetDatabase(dataBaseName);
            appSettings = settings;
            webHelper = new WebHelper();
            CryptographyUtility = new CryptographyHelper.Utility.CryptographyUtility();
        }

        public JObject ModelMetrics(string clientid, string dcid,string correlationId)
        {
            var modelMetrics = new JObject();
            var resultSet = new JObject();
            modelMetrics["modelDetails"] = new JObject();
            var collection2 = _database.GetCollection<BsonDocument>("ModelMetrics");
            var filter2 = Builders<BsonDocument>.Filter.Eq("CorrelationId", correlationId) & Builders<BsonDocument>.Filter.Eq("ClientUId", clientid) & Builders<BsonDocument>.Filter.Eq("DeliveryConstructUID", dcid);
            var projection2 = Builders<BsonDocument>.Projection.Exclude("_id").Include("CorrelationId").Include("ModelHealth").Include("Accuracy").Include("InputDrift").Include("TargetVariance").Include("DataQuality").Include("ModifiedOn");
            var result2 = collection2.Find(filter2).SortByDescending(item => item["ModifiedOn"]).Project<BsonDocument>(projection2).ToList();
            //var result2 = collection2.Find(filter2).Project<BsonDocument>(projection2).ToList();
            if (result2.Count > 0)
            {
                var jsonWriterSettings = new JsonWriterSettings { OutputMode = JsonOutputMode.Strict };
                resultSet = JObject.Parse(result2[0].ToJson<BsonDocument>(jsonWriterSettings));
                modelMetrics["modelMetrics"] = resultSet;
            }
            var modelDetails = CommonUtility.GetDataSourceModel(correlationId, appSettings);
            if (modelDetails.Count > 0)
            {
                modelMetrics["modelDetails"]["BusinessProblems"] = modelDetails[3].ToString();
                modelMetrics["modelDetails"]["ModelName"] = modelDetails[0].ToString();
                modelMetrics["modelDetails"]["DataSource"] = modelDetails[1].ToString();
                modelMetrics["modelDetails"]["Category"] = modelDetails[5].ToString();
            }


            return modelMetrics;
        }

        public List<JObject> TrainedModelHistory(string correlationId, string clientid, string dcid)
        {
            var resultSet = new JObject();
            var finalList = new List<JObject>();
            var collection2 = _database.GetCollection<BsonDocument>("TrainedModelHistory") ;
            var filter2 = Builders<BsonDocument>.Filter.Eq("CorrelationId", correlationId) & Builders<BsonDocument>.Filter.Eq("ClientUId", clientid) & Builders<BsonDocument>.Filter.Eq("DeliveryConstructUID", dcid);
            var projection2 = Builders<BsonDocument>.Projection.Exclude("_id");
            var result2 = collection2.Find(filter2).SortByDescending(item => item["ModifiedOn"]).Project<BsonDocument>(projection2).ToList();
            if (result2.Count > 0)
            {
                bool DBEncryptionRequired = EncryptDB(correlationId, CONSTANTS.SSAIDeployedModels);
                for (int i = 0; i < result2.Count; i++)
                {
                    if (DBEncryptionRequired)
                    {
                        try
                        {
                            if (result2[i].Contains(CONSTANTS.CreatedByUser) && !string.IsNullOrEmpty(Convert.ToString(result2[i][CONSTANTS.CreatedByUser])))
                                result2[i][CONSTANTS.CreatedByUser] = appSettings.Value.IsAESKeyVault ? CryptographyUtility.Decrypt(Convert.ToString(result2[i][CONSTANTS.CreatedByUser])): AesProvider.Decrypt(Convert.ToString(result2[i][CONSTANTS.CreatedByUser]), appSettings.Value.aesKey, appSettings.Value.aesVector);
                        }
                        catch (Exception ex) { LOGGING.LogManager.Logger.LogProcessInfo(typeof(ModelMonitorService), nameof(TrainedModelHistory) + "User is already decrypted....", ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty); }
                        try
                        {
                            if (result2[i].Contains(CONSTANTS.ModifiedByUser) && !string.IsNullOrEmpty(Convert.ToString(result2[i][CONSTANTS.ModifiedByUser])))
                                result2[i][CONSTANTS.ModifiedByUser] =appSettings.Value.IsAESKeyVault? CryptographyUtility.Decrypt(Convert.ToString(result2[i][CONSTANTS.ModifiedByUser])):  AesProvider.Decrypt(Convert.ToString(result2[i][CONSTANTS.ModifiedByUser]), appSettings.Value.aesKey, appSettings.Value.aesVector);
                        }
                        catch (Exception ex) { LOGGING.LogManager.Logger.LogProcessInfo(typeof(ModelMonitorService), nameof(TrainedModelHistory) + "User is already decrypted....", ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty); }
                    }
                    var jsonWriterSettings = new JsonWriterSettings { OutputMode = JsonOutputMode.Strict };
                    resultSet = JObject.Parse(result2[i].ToJson<BsonDocument>(jsonWriterSettings));
                    finalList.Add(resultSet);
                }

            }

            return finalList;
        }
        private bool EncryptDB(string correlationid, string collectionname)
        {
            var collection = _database.GetCollection<BsonDocument>(collectionname);
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
    }
}
