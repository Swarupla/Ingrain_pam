namespace Accenture.MyWizard.Ingrain.BusinessDomain.Services
{
    #region Namespace References
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
    using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
    #endregion
    public class CloneDataService : ICloneService
    {
        #region Members
        private MongoClient _mongoClient;
        private IMongoDatabase _database;
        List<string> tblNames = new List<string>();
        private DataCleanUpDto _dataCleanUp;
        private readonly IOptions<IngrainAppSettings> appSettings;
        DatabaseProvider databaseProvider;        
        #endregion

        #region Constructors
        /// <summary>
        /// Constructor to Initialize mongoDB Connection
        /// </summary>
        public CloneDataService(DatabaseProvider db, IOptions<IngrainAppSettings> settings)
        {
            databaseProvider = db;
            appSettings = settings;
            _mongoClient = databaseProvider.GetDatabaseConnection();
            var dataBaseName = MongoUrl.Create(appSettings.Value.connectionString).DatabaseName;
            _database = _mongoClient.GetDatabase(dataBaseName);     
        }
        #endregion
        public void ColumnsforClone(string correlationId, string newCorrId, string newId, string newModelName, string collectionName, string userId, string deliveryConstructUID, string clientUId, out string cloneStatus)
        {
            cloneStatus = string.Empty;
            var collection = _database.GetCollection<BsonDocument>(collectionName);
            var projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var result = new List<BsonDocument>();
            result = collection.Find(filter).Project<BsonDocument>(projection).ToList();

            if (result.Count > 0)
            {
                for (int z = 0; z < result.Count; z++)
                {
                    JObject data = new JObject();
                    data = JObject.Parse(result[z].ToString());                  
                    if (data.ContainsKey(CONSTANTS.ModelName))
                    {
                        data[CONSTANTS.ModelName] = newModelName;
                    }
                    if (data.ContainsKey(CONSTANTS.CorrelationId))
                    {
                        data[CONSTANTS.CorrelationId] = newCorrId;
                        data.Add(CONSTANTS.Id, Guid.NewGuid().ToString()); //To avoid inserting ObjectId
                    }
                    if (data.ContainsKey(CONSTANTS.CreatedByUser))
                    {
                        data[CONSTANTS.CreatedByUser] = userId;
                    }
                    if (collectionName == CONSTANTS.SSAI_DeployedModels)
                    {                       
                        this.DeployedModl(data, newCorrId);
                    }
                    if (data.ContainsKey(CONSTANTS.ClientUId))
                    {
                        data[CONSTANTS.ClientUId] = clientUId;
                    }
                    if (data.ContainsKey(CONSTANTS.ClientId))
                    {
                        data[CONSTANTS.ClientId] = clientUId;
                    }
                    if (data.ContainsKey(CONSTANTS.DeliveryConstructUID))
                    {
                        data[CONSTANTS.DeliveryConstructUID] = deliveryConstructUID;
                    }
                    if (data.ContainsKey(CONSTANTS.DeliveryconstructId))
                    {
                        data[CONSTANTS.DeliveryconstructId] = deliveryConstructUID;
                    }
                    if (data.ContainsKey(CONSTANTS.CreatedOn))
                    {
                        data[CONSTANTS.CreatedOn] = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                    }
                    if (data.ContainsKey(CONSTANTS.ParamArgs))
                    {
                        var paramArgss = Convert.ToString(data["ParamArgs"]);
                        if (!string.IsNullOrEmpty(paramArgss) && paramArgss != CONSTANTS.Null && paramArgss != CONSTANTS.CurlyBraces)
                        {
                            var paramArgs = JObject.Parse(Convert.ToString(data["ParamArgs"]));
                            paramArgs["CorrelationId"] = newCorrId;
                            paramArgs["ClientUID"] = clientUId;
                            paramArgs["DeliveryConstructUId"] = deliveryConstructUID;
                            if (paramArgs.ContainsKey("Customdetails"))
                            {
                                var customDetails = Convert.ToString(paramArgs["Customdetails"]);
                                if (customDetails != CONSTANTS.Null && customDetails != CONSTANTS.CurlyBraces)
                                {
                                    paramArgs["Customdetails"]["InputParameters"]["CorrelationId"] = newCorrId;
                                }
                            }

                            data["ParamArgs"] = JsonConvert.SerializeObject(paramArgs, Formatting.None);
                        }
                    }

                    InsertClone(data, collectionName);
                    cloneStatus = collectionName + CONSTANTS.EmptySpace;
                }
            }
        }

        /// <summary>
        /// Clone DeployedModel data
        /// </summary>
        /// <param name="data"></param>
        /// <param name="correlationId"></param>
        private void DeployedModl(JObject data, string newCorrelationId)
        {
            if (data.ContainsKey(CONSTANTS.IsPrivate) && data.ContainsKey(CONSTANTS.IsModelTemplate))
            {
                data[CONSTANTS.IsPrivate] = true;
                data[CONSTANTS.IsModelTemplate] = false;
                
            }
            if (data.ContainsKey(CONSTANTS.ModelURL))
            {
                if (Convert.ToString(data[CONSTANTS.ModelType]) == CONSTANTS.TimeSeries)
                {
                    string forecastURL = appSettings.Value.foreCastModel;
                    data[CONSTANTS.ModelURL] = string.Format(forecastURL, newCorrelationId, data[CONSTANTS.Frequency]);
                }
                else
                {
                    string publishurl = appSettings.Value.publishURL;
                    data[CONSTANTS.ModelURL] = string.Format(publishurl + CONSTANTS.Zero, newCorrelationId);
                }
            }
        }

        public void InsertClone(JObject data, string Collection)
        {
            var jsonColumns = Newtonsoft.Json.JsonConvert.SerializeObject(data);
            var insertBsonColumns = BsonSerializer.Deserialize<BsonDocument>(jsonColumns);
            var collection = _database.GetCollection<BsonDocument>(Collection);
            collection.InsertOne(insertBsonColumns);
        }


        public void UpdateDeployedModels(string correlationId)
        {
            var collection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
            var filter = Builders<DeployModelsDto>.Filter.Where(x => x.CorrelationId == correlationId);
            var projection = Builders<DeployModelsDto>.Projection.Exclude("_id");
            var result = collection.Find(filter).Project<DeployModelsDto>(projection).FirstOrDefault();
            if(result != null)
            {
                var update = Builders<DeployModelsDto>.Update.Set(x => x.Status, CONSTANTS.InProgress)
                                                             .Set(x => x.LinkedApps, new string[] { })
                                                             .Set(x => x.AppId, null)
                                                             .Set(x => x.InputSample, null)
                                                             .Set(x => x.VDSLink, null)
                                                             .Set(x => x.IsUpdated, "False")
                                                             .Set(x => x.ModelVersion, null);
                collection.UpdateOne(filter, update);
            }
        }

    }
}
