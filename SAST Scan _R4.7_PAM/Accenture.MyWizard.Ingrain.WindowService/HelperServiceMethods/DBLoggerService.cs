using System;
using System.Linq;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using Accenture.MyWizard.Cryptography.EncryptionProviders;
using WINSERVICEMODELS = Accenture.MyWizard.Ingrain.WindowService.Models;
using DATAMODELS = Accenture.MyWizard.Ingrain.DataModels.Models;
using DATAACCESS = Accenture.MyWizard.Ingrain.WindowService;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
using AIDataModels = Accenture.MyWizard.Ingrain.DataModels.AICore;

namespace Accenture.MyWizard.Ingrain.WindowService.HelperServiceMethods
{
    class DBLoggerService
    {

        private readonly WINSERVICEMODELS.AppSettings appSettings = null;
        private IMongoDatabase _database;
        private DATAACCESS.DatabaseProvider databaseProvider;
        
        public DBLoggerService()
        {
            appSettings = AppSettingsJson.GetAppSettings().GetSection("AppSettings").Get<WINSERVICEMODELS.AppSettings>();
            databaseProvider = new DATAACCESS.DatabaseProvider();
            var dataBaseName = MongoUrl.Create(appSettings.connectionString).DatabaseName;
            MongoClient mongoClient = databaseProvider.GetDatabaseConnection();
            _database = mongoClient.GetDatabase(dataBaseName);
        }

        public string UpdateOfflineRunTime(DATAMODELS.DeployModelsDto item)
        {
            var collection = _database.GetCollection<DATAMODELS.DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
            var filter = Builders<DATAMODELS.DeployModelsDto>.Filter.Eq("CorrelationId", item.CorrelationId);
            var result = collection.Find(filter).ToList();
            if (result.Count > 0)
            {
                var update = Builders<DATAMODELS.DeployModelsDto>.Update.Set("offlineTrainingRunDate", DateTime.UtcNow.ToString());
                collection.UpdateOne(filter, update);
            }

            return "Success";
        }
        public string UpdateOfflineException(DATAMODELS.DeployModelsDto item, string errorMessage)
        {
            var collection = _database.GetCollection<DATAMODELS.DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
            var filter = Builders<DATAMODELS.DeployModelsDto>.Filter.Eq("CorrelationId", item.CorrelationId);
            var result = collection.Find(filter).ToList();
            if (result.Count > 0)
            {
                var update = Builders<DATAMODELS.DeployModelsDto>.Update.Set("offlineTrainingRunDate", DateTime.UtcNow.ToString()).Set("ExceptionDate", DateTime.UtcNow.ToString()).Set("ExceptionMessage", errorMessage);
                collection.UpdateOne(filter, update);
            }

            return "Success";
        }
       
        public string UpdateAIOfflineRunTime(AIDataModels.UseCase.UsecaseDetails item)
        {
            var collection = _database.GetCollection<AIDataModels.UseCase.UsecaseDetails>(CONSTANTS.SSAIDeployedModels);
            var filter = Builders<AIDataModels.UseCase.UsecaseDetails>.Filter.Eq("UsecaseId", item.UsecaseId);
            var result = collection.Find(filter).ToList();
            if (result.Count > 0)
            {
                var update = Builders<AIDataModels.UseCase.UsecaseDetails>.Update.Set("offlineTrainingRunDate", DateTime.UtcNow.ToString());
                collection.UpdateOne(filter, update);
            }

            return "Success";
        }
        public string UpdateAIOfflineException(AIDataModels.UseCase.UsecaseDetails item, string errorMessage)
        {
            var collection = _database.GetCollection<AIDataModels.UseCase.UsecaseDetails>(CONSTANTS.AISavedUsecases);
            var filter = Builders<AIDataModels.UseCase.UsecaseDetails>.Filter.Eq("UsecaseId", item.UsecaseId);
            var result = collection.Find(filter).ToList();
            if (result.Count > 0)
            {
                var update = Builders<AIDataModels.UseCase.UsecaseDetails>.Update.Set("offlineTrainingRunDate", DateTime.UtcNow.ToString()).Set("ExceptionDate", DateTime.UtcNow.ToString()).Set("ExceptionMessage", errorMessage);
                collection.UpdateOne(filter, update);
            }

            return "Success";
        }

        public string UpdateOfflinePredRunTime(DATAMODELS.DeployModelsDto item)
        {
            var collection = _database.GetCollection<DATAMODELS.DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
            var filter = Builders<DATAMODELS.DeployModelsDto>.Filter.Eq("CorrelationId", item.CorrelationId);
            var result = collection.Find(filter).ToList();
            if (result.Count > 0)
            {
                var update = Builders<DATAMODELS.DeployModelsDto>.Update.Set("offlinePredRunDate", DateTime.UtcNow.ToString());
                collection.UpdateOne(filter, update);
            }

            return "Success";
        }
        public string UpdateOfflinePredException(DATAMODELS.DeployModelsDto item, string errorMessage)
        {
            var collection = _database.GetCollection<DATAMODELS.DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
            var filter = Builders<DATAMODELS.DeployModelsDto>.Filter.Eq("CorrelationId", item.CorrelationId);
            var result = collection.Find(filter).ToList();
            if (result.Count > 0)
            {
                var update = Builders<DATAMODELS.DeployModelsDto>.Update.Set("offlinePredRunDate", DateTime.UtcNow.ToString()).Set("ExceptionDate", DateTime.UtcNow.ToString()).Set("ExceptionMessage", errorMessage);
                collection.UpdateOne(filter, update);
            }

            return "Success";
        }
       
        public void LogInfoMessageToDB(string clientUId, string deliverConstructUId, string useCaseUId, string entityUId, string itemUId, string logType, string logMessage, string status)
        {
            WINSERVICEMODELS.PredictionSchedulerLog predictionSchedulerLog = new WINSERVICEMODELS.PredictionSchedulerLog
            {
                ClientUId = clientUId,
                DeliveryConstructUId = deliverConstructUId,
                UseCaseUId = useCaseUId,
                EntityUId = entityUId,
                ItemUId = itemUId,
                LogType = logType,
                Status = status,
                LogMessage = logMessage,
                LogLevel = "Info",
                CreatedBy = "SYSTEM",
                CreatedOn = DateTime.UtcNow.ToString(),
                ModifiedBy = "SYSTEM",
                ModifiedOn = DateTime.UtcNow.ToString()
            };

            var logCollection = _database.GetCollection<WINSERVICEMODELS.PredictionSchedulerLog>(nameof(WINSERVICEMODELS.PredictionSchedulerLog));
            logCollection.InsertOneAsync(predictionSchedulerLog);
        }
        public void LogInfoMessageToDB(string clientUId, string deliverConstructUId, string useCaseUId, string correlationId, string uniqueId, string entityUId, string itemUId, string logType, string logMessage, string status)
        {
            WINSERVICEMODELS.PredictionSchedulerLog predictionSchedulerLog = new WINSERVICEMODELS.PredictionSchedulerLog
            {
                ClientUId = clientUId,
                DeliveryConstructUId = deliverConstructUId,
                UseCaseUId = useCaseUId,
                CorrelationId = correlationId,
                UniqueId = uniqueId,
                EntityUId = entityUId,
                ItemUId = itemUId,
                LogType = logType,
                Status = status,
                LogMessage = logMessage,
                LogLevel = "Info",
                CreatedBy = "SYSTEM",
                CreatedOn = DateTime.UtcNow.ToString(),
                ModifiedBy = "SYSTEM",
                ModifiedOn = DateTime.UtcNow.ToString()

            };

            var logCollection = _database.GetCollection<WINSERVICEMODELS.PredictionSchedulerLog>(nameof(WINSERVICEMODELS.PredictionSchedulerLog));
            logCollection.InsertOneAsync(predictionSchedulerLog);
        }
       
        public void LogErrorMessageToDB(string clientUId, string deliverConstructUId, string useCaseUId, string entityUId, string itemUId, string logType, string logMessage, string status)
        {
            WINSERVICEMODELS.PredictionSchedulerLog predictionSchedulerLog = new WINSERVICEMODELS.PredictionSchedulerLog
            {
                ClientUId = clientUId,
                DeliveryConstructUId = deliverConstructUId,
                UseCaseUId = useCaseUId,
                EntityUId = entityUId,
                ItemUId = itemUId,
                LogType = logType,
                Status = status,
                LogMessage = logMessage,
                LogLevel = "Error",
                CreatedBy = "SYSTEM",
                CreatedOn = DateTime.UtcNow.ToString(),
                ModifiedBy = "SYSTEM",
                ModifiedOn = DateTime.UtcNow.ToString()
            };

            var logCollection = _database.GetCollection<WINSERVICEMODELS.PredictionSchedulerLog>(nameof(WINSERVICEMODELS.PredictionSchedulerLog));
            logCollection.InsertOneAsync(predictionSchedulerLog);
        }
        public void LogErrorMessageToDB(string clientUId, string deliverConstructUId, string useCaseUId, string correlationId, string uniqueId, string entityUId, string itemUId, string logType, string logMessage, string status)
        {
            WINSERVICEMODELS.PredictionSchedulerLog predictionSchedulerLog = new WINSERVICEMODELS.PredictionSchedulerLog
            {
                ClientUId = clientUId,
                DeliveryConstructUId = deliverConstructUId,
                UseCaseUId = useCaseUId,
                CorrelationId = correlationId,
                UniqueId = uniqueId,
                EntityUId = entityUId,
                ItemUId = itemUId,
                LogType = logType,
                Status = status,
                LogMessage = logMessage,
                LogLevel = "Error",
                CreatedBy = "SYSTEM",
                CreatedOn = DateTime.UtcNow.ToString(),
                ModifiedBy = "SYSTEM",
                ModifiedOn = DateTime.UtcNow.ToString()
            };

            var logCollection = _database.GetCollection<WINSERVICEMODELS.PredictionSchedulerLog>(nameof(WINSERVICEMODELS.PredictionSchedulerLog));
            logCollection.InsertOneAsync(predictionSchedulerLog);
        }
    }
}
