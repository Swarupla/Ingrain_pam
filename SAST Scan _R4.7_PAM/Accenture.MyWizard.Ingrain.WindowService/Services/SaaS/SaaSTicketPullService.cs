
namespace Accenture.MyWizard.Ingrain.WindowService.Services
{
    #region Namespace References
    using MongoDB.Driver;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using WINSERVICEMODELS = Accenture.MyWizard.Ingrain.WindowService.Models;
    using DATAACCESS = Accenture.MyWizard.Ingrain.WindowService;
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Net;
    using RestSharp;
    using Accenture.MyWizard.Ingrain.WindowService.Interfaces;
    using System.Threading.Tasks;
    using System.Linq;
    using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
    using MongoDB.Bson;
    using Accenture.MyWizard.Ingrain.WindowService.Models;
    using Accenture.MyWizard.Ingrain.DataModels.Models;
    using MongoDB.Bson.Serialization;


    // using Microsoft.Extensions.DependencyInjection;
    #endregion
    public class SaaSTicketPullService : ISaaSTicketPullService
    {
        private readonly WINSERVICEMODELS.AppSettings appSettings = null;
        private readonly IMongoDatabase _database;

        public SaaSTicketPullService()
        {
            appSettings = AppSettingsJson.GetAppSettings().GetSection("AppSettings").Get<WINSERVICEMODELS.AppSettings>();
            DATAACCESS.DatabaseProvider databaseProvider = new DATAACCESS.DatabaseProvider();
            var dataBaseName = MongoUrl.Create(appSettings.connectionString).DatabaseName;
            MongoClient mongoClient = databaseProvider.GetDatabaseConnection();
            _database = mongoClient.GetDatabase(dataBaseName);

        }
        public List<ClientInfo> FetchProvisionedE2EID()
        {
            try
            {
                List<ClientInfo> E2EIDInfo = new List<ClientInfo>();
                var collection = _database.GetCollection<ATRProvisionRequestDto>("CAMConfiguration");
                var filterBuilder = Builders<ATRProvisionRequestDto>.Filter.Empty;
                var doc = collection.Find(filterBuilder).Project(Builders<ATRProvisionRequestDto>.Projection.Exclude("_id")).ToList();
                var data = BsonSerializer.Deserialize<List<ATRProvisionRequestDto>>(doc.ToJson());
                if (data.Count > 0)
                {

                    foreach (var E2EID in data)
                    {
                        var E2EDetail = FetchSaaSProvisionedDetails(E2EID);
                        if (E2EDetail != null && E2EDetail.CAMConfigDetails != null && E2EDetail.SAASProvisionDetails != null)
                            E2EIDInfo.Add(E2EDetail);
                    }

                    return E2EIDInfo;
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(SaaSTicketPullService), "FetchProvisionedE2EID", ex.Message, ex.InnerException, string.Empty, string.Empty, string.Empty, string.Empty);
            }

            return null;
        }

        public ClientInfo FetchSaaSProvisionedDetails(ATRProvisionRequestDto atrdata)
        {
            ClientInfo E2EInfo = new ClientInfo();
            E2EInfo.CAMConfigDetails = BsonSerializer.Deserialize<ATRProvisionRequestDto>(atrdata.ToJson());
            SAASProvisionDetails sAASProvisionDetails = new SAASProvisionDetails();
            var collection = _database.GetCollection<SAASProvisionDetails>(CONSTANTS.DB_SAASProvisionDetails);
            var filterBuilder = Builders<SAASProvisionDetails>.Filter;
            var filter = filterBuilder.Eq("E2EUID", atrdata.E2EUId);
            var doc = collection.Find(filter).Project(Builders<SAASProvisionDetails>.Projection.Exclude("_id")).ToList();
            if (doc.Count > 0)
            {
                sAASProvisionDetails = BsonSerializer.Deserialize<SAASProvisionDetails>(doc[0].ToJson());
                E2EInfo.SAASProvisionDetails = sAASProvisionDetails;

            }
            else
            {
                //TODO ATRTicketPullService
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(SaaSTicketPullService), "FetchSaaSProvisionedDetails", string.Format(CONSTANTS.MSG_NoProvisionForSaaS, atrdata.E2EUId), string.Empty, string.Empty, string.Empty, string.Empty);
            }

            return E2EInfo;
        }

        public bool CheckPAMHistoricalPullTracker(string ClientUId, string EndToEndUId, string entityType, string pullType)
        {
            try
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(SaaSTicketPullService), "CheckPAMHistoricalPullTracker", "Inside CheckPAMHistoricalPullTracker method", string.Empty, string.Empty, string.Empty, string.Empty);

                var collection = _database.GetCollection<BsonDocument>(CONSTANTS.DB_PAMHistoricalAndDeltaPullTracker);
                var filterBuilder = Builders<BsonDocument>.Filter;
                var filter = FilterDefinition<BsonDocument>.Empty;
                //List<int> flag = new List<int>();
                List<int> flag = new List<int>() { 1, 2 }; //1-Completed, 2-In Progress
                //flag.Add(1);//completed
                //flag.Add(2);//in progress             
                filter = filterBuilder.Eq("ClientUId", ClientUId) & filterBuilder.Eq("PullType", pullType) & filterBuilder.Eq("EntityType", entityType) & (filterBuilder.In("Flag", flag));
                var result = collection.Find(filter).ToList();
                if (result != null && result.Count > 0)
                {
                    return true;
                }
            }
            catch (Exception Ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(SaaSTicketPullService), "CheckPAMHistoricalPullTracker", Ex.Message, Ex.InnerException, string.Empty, string.Empty, string.Empty, string.Empty);
            }

            return false;
        }

        public bool DeleteHistory(string clientUId, string e2eUId, string entityType)
        {
            bool status = false;
            try
            {
                switch (entityType)
                {
                    case "Incident":
                        status = DeleteCollection(clientUId, e2eUId, CONSTANTS.DB_IncidentCollection);
                        break;
                    case "Problem":
                        status = DeleteCollection(clientUId, e2eUId, CONSTANTS.DB_ProblemCollection);
                        break;
                    case "Service_Request":
                        status = DeleteCollection(clientUId, e2eUId, CONSTANTS.DB_ServiceRequestCollection);
                        break;
                    case "Ticket":
                        status = DeleteCollection(clientUId, e2eUId, CONSTANTS.DB_IncidentCollection);
                        status = DeleteCollection(clientUId, e2eUId, CONSTANTS.DB_ProblemCollection);
                        status = DeleteCollection(clientUId, e2eUId, CONSTANTS.DB_ServiceRequestCollection);
                        status = DeleteCollection(clientUId, e2eUId, CONSTANTS.DB_IOTicketCollection);
                        status = DeleteCollection(clientUId, e2eUId, CONSTANTS.DB_AOTaskCollection);
                        status = DeleteCollection(clientUId, e2eUId, CONSTANTS.DB_IOTaskCollection);
                        break;
                }
            }
            catch (Exception Ex)
            {
                status = false;
                throw Ex;
            }

            return status;
        }

        public bool DeleteCollection(string ClientUId, string EndToEndUId, string collectionName)
        {
            bool status = false;
            try
            {
                //Log.Information("Inside PAMDemographicsDA_DeleteHistory method");

                var filterBuilder = Builders<BsonDocument>.Filter;
                var Filter = filterBuilder.Eq("ClientUId", ClientUId) & filterBuilder.Eq("EndToEndUId", EndToEndUId);
                var Collection = _database.GetCollection<BsonDocument>(collectionName);
                var Result = Collection.DeleteMany(Filter);
                status = true;
            }
            catch (Exception Ex)
            {
                status = false;

            }
            return status;
        }

        public void InsertPAMHistoricalPullTracker(string clientUId, string EndToEndUId, string entityType, string pullType, DateTime startDate, DateTime endDate)
        {
            try
            {
                var collection = _database.GetCollection<PAMHistoricalPullTracker>(CONSTANTS.DB_PAMHistoricalAndDeltaPullTracker);
                var trackerFilter = collection.Find(c => c.ClientUId == clientUId && c.EndToEndUId == EndToEndUId && c.EntityType == entityType && c.PullType == pullType);
                var filterBuilder = Builders<PAMHistoricalPullTracker>.Filter;
                var filter = filterBuilder.Eq("ClientUId", clientUId) & filterBuilder.Eq("EndToEndUId", EndToEndUId) & filterBuilder.Eq("PullType", pullType) & filterBuilder.Eq("EntityType", entityType);
                if (trackerFilter.Count() > 0)
                {
                    collection.DeleteMany(filter);
                }
                PAMHistoricalPullTracker objPAMTrackerData = new PAMHistoricalPullTracker();
                objPAMTrackerData.ClientUId = clientUId;
                objPAMTrackerData.EndToEndUId = EndToEndUId;
                objPAMTrackerData.EntityType = entityType;
                objPAMTrackerData.Flag = 2;
                objPAMTrackerData.PullType = pullType;
                objPAMTrackerData.StartDate = startDate;
                objPAMTrackerData.EndDate = endDate;
                objPAMTrackerData.Status = CONSTANTS.PAMHistoricalProgressStatus;
                objPAMTrackerData.ProcessingStartTime = DateTime.UtcNow;
                collection.InsertOne(objPAMTrackerData);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(SaaSTicketPullService), "insertPAMHistoricalPullTracker", ex.Message, ex.InnerException, string.Empty, string.Empty, string.Empty, string.Empty);
                throw ex;
            }
        }

        public List<PAMHistoricalPullTracker> FetchPAMHistoricalPullTracker(string clientUId, string EndToEndUId, string entityType, string pullType)
        {
            try
            {
                var collection = _database.GetCollection<PAMHistoricalPullTracker>(CONSTANTS.DB_PAMHistoricalAndDeltaPullTracker);
                var filterBuilder = Builders<PAMHistoricalPullTracker>.Filter;
                var filter = filterBuilder.Eq("ClientUId", clientUId) & filterBuilder.Eq("EndToEndUId", EndToEndUId) & filterBuilder.Eq("PullType", pullType) & filterBuilder.Eq("EntityType", entityType);
                var doc = collection.Find(filter).Project(Builders<PAMHistoricalPullTracker>.Projection.Exclude("_id")).ToList();
                if (doc.Count > 0)
                {
                    return BsonSerializer.Deserialize<List<PAMHistoricalPullTracker>>(doc.ToJson());
                }
                else
                    return null;
            }
            catch (Exception Ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(SaaSTicketPullService), "fetchPAMHistoricalPullTracker", Ex.Message, Ex.InnerException, string.Empty, string.Empty, string.Empty, string.Empty);
                throw Ex;
            }
        }

        public void UpdatePAMHistoricalPullTracker(string ClientUId, string EndToEndUId, string entityType, string pullType, int flag, string statustext, int recordCount, DateTime? maxLastDFUpdatedDate)
        {
            try
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(SaaSTicketPullService), nameof(UpdatePAMHistoricalPullTracker), "START", Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
                var collection = _database.GetCollection<PAMHistoricalPullTracker>(CONSTANTS.DB_PAMHistoricalAndDeltaPullTracker);
                var filterBuilder = Builders<PAMHistoricalPullTracker>.Filter;
                var filter = FilterDefinition<PAMHistoricalPullTracker>.Empty; ;
                filter = filterBuilder.Eq("ClientUId", ClientUId) & filterBuilder.Eq("EndToEndUId", EndToEndUId) & filterBuilder.Eq("PullType", pullType) & filterBuilder.Eq("EntityType", entityType);
                var update = Builders<PAMHistoricalPullTracker>.Update.Set("Flag", flag).Set("Status", statustext).Set("ProcessingEndTime", DateTime.UtcNow).Set("RecordCount", recordCount).Set("dfLastUpdatedDate", maxLastDFUpdatedDate); //completed 
                var updateResult = collection.UpdateMany(filter, update);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(SaaSTicketPullService), nameof(UpdatePAMHistoricalPullTracker), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                throw ex;
            }
        }

        public void InsertPAMHistoricalPullFailedTracker(PAMHistoricalPullTracker tracker)
        {
            try
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(SaaSTicketPullService), nameof(InsertPAMHistoricalPullFailedTracker), "START", Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
                var collection = _database.GetCollection<PAMHistoricalPullTracker>(CONSTANTS.DB_PAMHistoricalAndDeltaPullFailedTracker);
                PAMHistoricalPullTracker objPAMTrackerData = new PAMHistoricalPullTracker();
                objPAMTrackerData.ClientUId = tracker.ClientUId;
                objPAMTrackerData.EndToEndUId = tracker.EndToEndUId;
                objPAMTrackerData.EntityType = tracker.EntityType;
                objPAMTrackerData.Flag = 3;
                objPAMTrackerData.PullType = tracker.PullType;
                objPAMTrackerData.StartDate = tracker.StartDate;
                objPAMTrackerData.EndDate = tracker.EndDate;
                objPAMTrackerData.Status = "Data Pull is failed";
                objPAMTrackerData.ProcessingStartTime = tracker.ProcessingStartTime;
                objPAMTrackerData.ProcessingEndTime = DateTime.UtcNow;
                objPAMTrackerData.RecordCount = tracker.RecordCount;
                collection.InsertOne(objPAMTrackerData);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(SaaSTicketPullService), nameof(InsertPAMHistoricalPullFailedTracker), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                throw ex;
            }
        }


        public void UpdatePAMHistoricalPullTracker(string ClientUId, string EndToEndUId, string entityType, string pullType, int flag, string statustext, int recordCount, DateTime startDate, DateTime endDate, DateTime processingStartTime)
        {
            try
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(SaaSTicketPullService), nameof(UpdatePAMHistoricalPullTracker), "START", Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
                var collection = _database.GetCollection<PAMHistoricalPullTracker>(CONSTANTS.DB_PAMHistoricalAndDeltaPullTracker);
                var filterBuilder = Builders<PAMHistoricalPullTracker>.Filter;
                var filter = FilterDefinition<PAMHistoricalPullTracker>.Empty; ;
                filter = filterBuilder.Eq("ClientUId", ClientUId) & filterBuilder.Eq("EndToEndUId", EndToEndUId) & filterBuilder.Eq("PullType", pullType) & filterBuilder.Eq("EntityType", entityType);
                var update = Builders<PAMHistoricalPullTracker>.Update.Set("Flag", flag).Set("Status", statustext)
                    .Set("RecordCount", recordCount)
                    .Set("StartDate", startDate)
                    .Set("EndDate", endDate)
                    .Set("ProcessingStartTime", processingStartTime);
                var updateResult = collection.UpdateMany(filter, update);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(SaaSTicketPullService), nameof(UpdatePAMHistoricalPullTracker), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                throw ex;
            }
        }

    
    }
}
