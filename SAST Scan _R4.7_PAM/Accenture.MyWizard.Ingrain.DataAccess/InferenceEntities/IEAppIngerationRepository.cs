using Accenture.MyWizard.Ingrain.DataModels.InferenceEngineModel;
//using Accenture.MyWizard.Ingrain.DataModels.Models;
using Accenture.MyWizard.Shared.Helpers;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System;
using System.Collections;
using System.Collections.Generic;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;

namespace Accenture.MyWizard.Ingrain.DataAccess.InferenceEntities
{
    public class IEAppIngerationRepository
    {
        private readonly IOptions<IngrainAppSettings> appSettings;
        private IMongoDatabase _database;
        public IEAppIngerationRepository(IMongoDatabase database)
        {
            _database = database;
        }
        public IEAppIntegration GetAppDetailsOnAppName(string appName)
        {
            var AppIntegCollection = _database.GetCollection<IEAppIntegration>(CONSTANTS.AppIntegration);
            var filterBuilder = Builders<IEAppIntegration>.Filter;
            var AppFilter = filterBuilder.Eq("ApplicationName", appName);//"Ingrain");

            var Projection = Builders<IEAppIntegration>.Projection.Include("ApplicationID").Exclude("_id");
            return AppIntegCollection.Find(AppFilter).Project<IEAppIntegration>(Projection).FirstOrDefault();

        }

        public IEAppIntegration GetAppDetailsOnId(string appId)
        {
            var AppIntegCollection = _database.GetCollection<IEAppIntegration>(CONSTANTS.AppIntegration);
            var filterBuilder = Builders<IEAppIntegration>.Filter;
            var AppFilter = filterBuilder.Eq("ApplicationID", appId);//"Ingrain");

            var Projection = Builders<IEAppIntegration>.Projection.Exclude("_id");
            return AppIntegCollection.Find(AppFilter).Project<IEAppIntegration>(Projection).FirstOrDefault();

        }

        public List<IEAppDetails> GetAllAppDetails(string environment)
        {
            List<IEAppDetails> ApplicationDetails = new List<IEAppDetails>();
            var builder = Builders<IEAppDetails>.Filter;
            var appCollection = _database.GetCollection<IEAppDetails>(CONSTANTS.AppIntegration);
            FilterDefinition<IEAppDetails> filter;
            if (string.IsNullOrEmpty(environment) || environment == "null" || environment == "undefined")
            {
                filter = builder.Eq("Environment", "PAD");
            }
            else
            {
                filter = builder.Eq("Environment", environment);
            }
            var Projection = Builders<IEAppDetails>.Projection.Include("ApplicationID").Include("ApplicationName").Exclude("_id");
            return appCollection.Find(filter).Project<IEAppDetails>(Projection).ToList();
        }


        public List<IEAppIntegration> GetAllApps(string environment)
        {
            List<IEAppIntegration> ApplicationDetails = new List<IEAppIntegration>();
            var builder = Builders<IEAppIntegration>.Filter;
            var appCollection = _database.GetCollection<IEAppIntegration>(CONSTANTS.AppIntegration);
            FilterDefinition<IEAppIntegration> filter;
            if (string.IsNullOrEmpty(environment) || environment == "null" || environment == "undefined")
            {
                filter = builder.Eq("Environment", "PAD");
            }
            else
            {
                filter = builder.Eq("Environment", environment);
            }
            var Projection = Builders<IEAppIntegration>.Projection.Exclude("_id");
            return appCollection.Find(filter).Project<IEAppIntegration>(Projection).ToList();
        }

        public void InsertAppNotification(IEAppNotificationLog model)
        {
            var collection = _database.GetCollection<IEAppNotificationLog>("AppNotificationLog");
            collection.InsertOne(model);
        }

        public List<IEAppNotificationLog> GetAppNoficationLog(int notificationMaxRetryCount)
        {

            var notificationLogCol = _database.GetCollection<IEAppNotificationLog>("AppNotificationLog");
            var filter = Builders<IEAppNotificationLog>.Filter.Where(x => !x.IsNotified)
                         & Builders<IEAppNotificationLog>.Filter.Where(x => x.RetryCount <= notificationMaxRetryCount);
            var projection = Builders<IEAppNotificationLog>.Projection.Exclude("_id");

            var appNotification = notificationLogCol.Find(filter).Project<IEAppNotificationLog>(projection).ToList();
            return appNotification;
        }

        public void UpdateAppNotificationLog(string requestId, bool isSuccess, int retryCount, string message)
        {
            var notificationLogCollection = _database.GetCollection<IEAppNotificationLog>("AppNotificationLog");
            var filter = Builders<IEAppNotificationLog>.Filter.Where(x => x.RequestId == requestId);
            var updateDoc = Builders<IEAppNotificationLog>.Update.Set(x => x.IsNotified, isSuccess)
                                                               .Set(x => x.RetryCount, retryCount)
                                                               .Set(x => x.NotificationResponseMessage, message)
                                                               .Set(x => x.ModifiedOn, DateTime.Now);
            notificationLogCollection.UpdateOne(filter, updateDoc);
        }


        public List<IEAppIntegration> GetAppName(string appName, string environment)
        {
            var collection = _database.GetCollection<IEAppIntegration>(CONSTANTS.AppIntegration);
            var filter = Builders<IEAppIntegration>.Filter.Eq("ApplicationName", appName) & Builders<IEAppIntegration>.Filter.Eq("Environment", environment);
            var Projection = Builders<IEAppIntegration>.Projection.Exclude("_id");
            var result = collection.Find(filter).Project<IEAppIntegration>(Projection).ToList();
            return result;
        }
        public void InsertAppNotification(IEAppIntegration model)
        {
            var collection = _database.GetCollection<IEAppIntegration>(CONSTANTS.AppIntegration);
            collection.InsertOneAsync(model);
        }


        public string UpdateAppIntegration(IEAppIntegration appIntegrations, string authProvider, string token_Url,string credential, string username)
        {
            var collection = _database.GetCollection<IEAppIntegration>(CONSTANTS.AppIntegration);
            var filter = Builders<IEAppIntegration>.Filter.Where(x => x.ApplicationID == appIntegrations.ApplicationID);
            var Projection = Builders<IEAppIntegration>.Projection.Exclude("_id");
            var result = collection.Find(filter).Project<IEAppIntegration>(Projection).FirstOrDefault();

            var status = CONSTANTS.NoRecordsFound;
            if (result != null)
            {
                var update = Builders<IEAppIntegration>.Update
                    .Set(x => x.Authentication, authProvider)
                    .Set(x => x.TokenGenerationURL, token_Url)
                    .Set(x => x.Credentials, (IEnumerable)(credential))
                    .Set(x => x.ModifiedByUser, username)
                    .Set(x => x.ModifiedOn, DateTime.UtcNow.ToString());
                collection.UpdateOne(filter, update);
                status = CONSTANTS.Success;
            }
            return status;
        }

     

    }
}
