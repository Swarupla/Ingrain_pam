using Accenture.MyWizard.Cryptography.EncryptionProviders;
using Accenture.MyWizard.Ingrain.DataModels.InferenceEngineModel;
using Accenture.MyWizard.Shared.Helpers;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text;
using CryptographyHelper = Accenture.MyWizard.Cryptography;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;


namespace Accenture.MyWizard.Ingrain.DataAccess.InferenceEntities
{
    public class IEModelRepository
    {
        private IMongoDatabase _database;
        public string IEModelsCollection { get; set; }
        static CryptographyHelper.Utility.CryptographyUtility CryptographyUtility = null;
        public IEModelRepository(IMongoDatabase database)
        {
            IEModelsCollection = "IEModels";
            _database = database;
            CryptographyUtility = new CryptographyHelper.Utility.CryptographyUtility();
        }
        public void InsertIEModel(IEModel model)
        {            
            var collection = _database.GetCollection<IEModel>(IEModelsCollection);
            collection.InsertOne(model);
        }

        /// <summary>
        /// Get IE based on clientuid,deliveryconstructuid,userid
        /// </summary>
        /// <param name="clientUId"></param>
        /// <param name="deliveryConstructUId"></param>
        /// <param name="userId"></param>
        /// <returns>flush status</returns>

        public List<IEModel> GetIEModel(string clientUId, string deliveryConstructUId, string userId, string FunctionalArea, IOptions<IngrainAppSettings> appSettings)
        {
            string encryptedUser = userId;
            string encryptedIOMUser = "iom_admin";
            encryptedIOMUser = appSettings.Value.IsAESKeyVault ? CryptographyUtility.Encrypt(Convert.ToString(encryptedIOMUser)) : AesProvider.Encrypt(Convert.ToString(encryptedIOMUser), appSettings.Value.aesKey, appSettings.Value.aesVector);
            if (!string.IsNullOrEmpty(Convert.ToString(encryptedUser)))
            {
                encryptedUser = appSettings.Value.IsAESKeyVault ? CryptographyUtility.Encrypt(Convert.ToString(encryptedUser)): AesProvider.Encrypt(Convert.ToString(encryptedUser), appSettings.Value.aesKey, appSettings.Value.aesVector);
            }
            var collection = _database.GetCollection<IEModel>(IEModelsCollection);
            var builder = Builders<IEModel>.Filter;
            FilterDefinition<IEModel> filter = null;
            if (appSettings.Value.Environment.Equals(CONSTANTS.PADEnvironment))
            {
                filter = builder.Where(x => x.ClientUId == clientUId)
                         & builder.Where(x => x.DeliveryConstructUId == deliveryConstructUId)
                         & builder.Where(x => (x.CreatedBy == userId || x.CreatedBy == encryptedUser));
            }
            else//added to fetch Model created by "iom_admin" from VDS end in FDS and PAM reg predictive Dashboard
            {
                filter = builder.Where(x => x.ClientUId == clientUId)
                         & builder.Where(x => x.DeliveryConstructUId == deliveryConstructUId)
                         & builder.Where(x => x.FunctionalArea == FunctionalArea || x.FunctionalArea == "undefined")
                         & builder.Where(x => x.CreatedBy == userId || x.CreatedBy == encryptedUser || x.CreatedBy == encryptedIOMUser || x.CreatedBy == "iom_admin");
            }
            var projection = Builders<IEModel>.Projection.Exclude("_id");
            List < IEModel > list = collection.Find(filter).SortByDescending(z => z.ModifiedOn).Project<IEModel>(projection).ToList();
            if (list.Count > 0)
            {
                foreach (var Item in list)
                {
                    if (Item.DBEncryptionRequired)
                    {
                        try
                        {
                            if(!string.IsNullOrEmpty(Convert.ToString(Item.CreatedBy)))
                                Item.CreatedBy= appSettings.Value.IsAESKeyVault ? CryptographyUtility.Decrypt(Item.CreatedBy): AesProvider.Decrypt(Item.CreatedBy, appSettings.Value.aesKey, appSettings.Value.aesVector);
                        }
                        catch (Exception ex)
                        {
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(IEModelRepository), nameof(GetIEModel), ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty);
                        }
                        try
                        {
                            if (!string.IsNullOrEmpty(Convert.ToString(Item.ModifiedBy)))
                                Item.ModifiedBy = appSettings.Value.IsAESKeyVault ? CryptographyUtility.Decrypt(Item.ModifiedBy): AesProvider.Decrypt(Item.ModifiedBy, appSettings.Value.aesKey, appSettings.Value.aesVector);
                        }
                        catch (Exception ex)
                        {
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(IEModelRepository), nameof(GetIEModel), ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty);
                        }
                    }
                }
            }
            return list;
        }


        public List<IEModel> GetReTrainIEModels()
        {
            var collection = _database.GetCollection<IEModel>(IEModelsCollection);
            var builder = Builders<IEModel>.Filter;
            var filter = (builder.Where(x => x.SourceName == "Entity") | builder.Where(x => x.SourceName == "Custom")) 
                & builder.Where(x => x.Status == "C");
            var projection = Builders<IEModel>.Projection.Exclude("_id");
            return collection.Find(filter).Project<IEModel>(projection).ToList();
        }


        public bool GetIEModelName(string ModelName, string clientUID, string deliveryUID, string userId, IOptions<IngrainAppSettings> appSettings)
        {
            string encryptedUser = userId;
            if (!string.IsNullOrEmpty(Convert.ToString(encryptedUser)))
            {
                encryptedUser = appSettings.Value.IsAESKeyVault ? CryptographyUtility.Encrypt(Convert.ToString(encryptedUser)) : AesProvider.Encrypt(Convert.ToString(encryptedUser), appSettings.Value.aesKey, appSettings.Value.aesVector);
            }
            var collection = _database.GetCollection<IEModel>(IEModelsCollection);
            var builder = Builders<IEModel>.Filter;

            var filter = builder.Where(x => x.ModelName.ToLower() == ModelName.ToLower()) & builder.Where(x => x.ClientUId == clientUID)
                       & builder.Where(x => x.DeliveryConstructUId == deliveryUID)
                       & builder.Where(x => (x.CreatedBy == userId || x.CreatedBy == encryptedUser));
            var projection = Builders<IEModel>.Projection.Exclude("_id");
            var result = collection.Find(filter).Project<IEModel>(projection).ToList();
            if (result.Count > 0)
            {

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Get IE based on correlation id
        /// </summary>
        /// <param name="correlationId"></param>
        /// <returns>flush status</returns>
        public IEModel GetIEModel(string correlationId)
        {
            var collection = _database.GetCollection<IEModel>(IEModelsCollection);
            var builder = Builders<IEModel>.Filter;
            var filter = builder.Where(x => x.CorrelationId == correlationId);
            var projection = Builders<IEModel>.Projection.Exclude("_id");
            return collection.Find(filter).Project<IEModel>(projection).FirstOrDefault();
        }

        public List<BsonDocument> GetMultiFileColumn(string correlationId)
        {
            var collection = _database.GetCollection<BsonDocument>("PS_MultiFileColumn");
            var builder = Builders<BsonDocument>.Projection.Include("CorrelationId").Include("File").Include("Flag").Exclude("_id");
            var columnFilter = Builders<BsonDocument>.Filter.Eq("CorrelationId", correlationId);
            return collection.Find(columnFilter).Project<BsonDocument>(builder).ToList();
        }

        /// <summary>
        /// Delete IE based on correlation id
        /// </summary>
        /// <param name="correlationId"></param>
        /// <returns>flush status</returns>
        public string DeleteIEModel(string correlationId)
        {
            var collection = _database.GetCollection<IEModel>(IEModelsCollection);
            var builder = Builders<IEModel>.Filter;
            var filter = builder.Where(x => x.CorrelationId == correlationId);
            collection.DeleteMany(filter);
            return "Success";
        }

        public void UpdateIEModel(string correlationId, string status, string message, string progress)
        {
            var collection = _database.GetCollection<IEModel>(IEModelsCollection);
            var filterModel = Builders<IEModel>.Filter.Eq("CorrelationId", correlationId);
            var updateBuilder = Builders<IEModel>.Update.Set(x => x.Status, status)
                                                        .Set(x => x.Message, message)
                                                        .Set(x => x.Progress, progress)
                                                        .Set(x => x.ModifiedOn, DateTime.UtcNow);
            collection.UpdateMany(filterModel, updateBuilder);
        }



        public void UpdateIEModelDates(string correlationId, string startDate, string endDate)
        {
            var collection = _database.GetCollection<IEModel>(IEModelsCollection);
            var filterModel = Builders<IEModel>.Filter.Where(x => x.CorrelationId == correlationId);
            var updateBuilder = Builders<IEModel>.Update.Set(x => x.StartDate, startDate)
              .Set(x => x.EndDate, endDate)
              .Set(x => x.ModifiedOn, DateTime.UtcNow);
            collection.UpdateMany(filterModel, updateBuilder);
        }

        public void BackupRecords(string correlationId)
        {
            var collection = _database.GetCollection<IEModel>(IEModelsCollection);
            var filter = Builders<IEModel>.Filter.Where(x => x.CorrelationId == correlationId);
            var projection = Builders<IEModel>.Projection.Exclude("_id");
            var result = collection.Find(filter).Project<IEModel>(projection).FirstOrDefault();
            if(result != null)
            {
                //create backup record
                result.CorrelationId = correlationId + "_backup";   
                collection.InsertOne(result);

            }
            
        }

        public void RestoreRecords(string correlationId)
        {
            //Delete new record
            var collection = _database.GetCollection<IEModel>(IEModelsCollection);
            var filter = Builders<IEModel>.Filter.Where(x => x.CorrelationId == correlationId); 
            collection.DeleteOne(filter);

            //restore old record
            var filterBackup = Builders<IEModel>.Filter.Where(x => x.CorrelationId == correlationId + "_backup");
            var update = Builders<IEModel>.Update.Set(x => x.CorrelationId, correlationId);
            collection.UpdateOne(filterBackup, update);
        }

        public void BackupIngestDataRecords(string correlationId)
        {
            var collection = _database.GetCollection<BsonDocument>("IE_IngestData");
            var filter = Builders<BsonDocument>.Filter.Eq("CorrelationId",correlationId);
            var projection = Builders<BsonDocument>.Projection.Exclude("_id");
            var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                //create backup record
                foreach(var doc in result)
                {
                    doc["CorrelationId"] = correlationId + "_backup";
                    collection.InsertOne(doc);
                }

            }

        }

        public void DeleteIngestData(string correlationId)
        {
            var collection = _database.GetCollection<BsonDocument>("IE_IngestData");
            var filter = Builders<BsonDocument>.Filter.Eq("CorrelationId", correlationId);            
            collection.DeleteMany(filter);

        }
        public void RestoreIngestDataRecords(string correlationId)
        {
            var collection = _database.GetCollection<BsonDocument>("IE_IngestData");
            var filter = Builders<BsonDocument>.Filter.Eq("CorrelationId", correlationId);

            //remove new records
            collection.DeleteMany(filter);

            //restore old records
            var filterBackup = Builders<BsonDocument>.Filter.Eq("CorrelationId", correlationId + "_backup");
            var update = Builders<BsonDocument>.Update.Set("CorrelationId", correlationId);
            collection.UpdateMany(filterBackup, update);

        }

        public void BackupPSMultiFileRecords(string correlationId)
        {
            var collection = _database.GetCollection<BsonDocument>("PS_MultiFileColumn");
            var filter = Builders<BsonDocument>.Filter.Eq("CorrelationId", correlationId);
            var projection = Builders<BsonDocument>.Projection.Exclude("_id");
            var result = collection.Find(filter).Project<BsonDocument>(projection).FirstOrDefault();
            if (result != null)
            {
                //create backup record
                result["CorrelationId"] = correlationId + "_backup";
                collection.InsertOne(result);

            }

        }

        public void DeletePSMultiFileRecords(string correlationId)
        {
            var collection = _database.GetCollection<BsonDocument>("PS_MultiFileColumn");
            var filter = Builders<BsonDocument>.Filter.Eq("CorrelationId", correlationId);
            collection.DeleteMany(filter);

        }
        public void RestorePSMultiFileRecords(string correlationId)
        {
            var collection = _database.GetCollection<BsonDocument>("PS_MultiFileColumn");
            var filter = Builders<BsonDocument>.Filter.Eq("CorrelationId", correlationId);

            //remove new records
            collection.DeleteMany(filter);

            //restore old records
            var filterBackup = Builders<BsonDocument>.Filter.Eq("CorrelationId", correlationId + "_backup");
            var update = Builders<BsonDocument>.Update.Set("CorrelationId", correlationId);
            collection.UpdateMany(filterBackup, update);

        }

        public void BackupUseCaseDefRecords(string correlationId)
        {
            var collection = _database.GetCollection<BsonDocument>("PS_UsecaseDefinition");
            var filter = Builders<BsonDocument>.Filter.Eq("CorrelationId", correlationId);
            var projection = Builders<BsonDocument>.Projection.Exclude("_id");
            var result = collection.Find(filter).Project<BsonDocument>(projection).FirstOrDefault();
            if (result != null)
            {
                //create backup record
                result["CorrelationId"] = correlationId + "_backup";
                collection.InsertOne(result);

            }

        }

        public void DeleteUseCaseDefRecords(string correlationId)
        {
            var collection = _database.GetCollection<BsonDocument>("PS_UsecaseDefinition");
            var filter = Builders<BsonDocument>.Filter.Eq("CorrelationId", correlationId);
            collection.DeleteMany(filter);

        }
        public void RestoreUseCaseDefRecords(string correlationId)
        {
            var collection = _database.GetCollection<BsonDocument>("PS_UsecaseDefinition");
            var filter = Builders<BsonDocument>.Filter.Eq("CorrelationId", correlationId);

            //remove new records
            collection.DeleteMany(filter);

            //restore old records
            var filterBackup = Builders<BsonDocument>.Filter.Eq("CorrelationId", correlationId + "_backup");
            var update = Builders<BsonDocument>.Update.Set("CorrelationId", correlationId);
            collection.UpdateMany(filterBackup, update);

        }

        public List<BsonDocument> GetIEIngestedRecords(string correlationId)
        {
            var dbCollection = _database.GetCollection<BsonDocument>("IE_IngestData");
            var filter = Builders<BsonDocument>.Filter.Eq("CorrelationId", correlationId);
            var projectionScenario = Builders<BsonDocument>.Projection.Include("CorrelationId").Include("InputData").Exclude("_id");
            var dbData = dbCollection.Find(filter).Project<BsonDocument>(projectionScenario).ToList();
            return dbData;
        }
    }
}
