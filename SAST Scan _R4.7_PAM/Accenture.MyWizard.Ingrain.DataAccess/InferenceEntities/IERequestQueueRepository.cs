using Accenture.MyWizard.Ingrain.DataModels.InferenceEngineModel;
using Accenture.MyWizard.Shared.Helpers;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;

namespace Accenture.MyWizard.Ingrain.DataAccess.InferenceEntities
{

    public class IERequestQueueRepository
    {        
        private IMongoDatabase _database;
        public string QueueCollection { get; set; }
        public IERequestQueueRepository(IMongoDatabase database)
        {
            _database = database;
            QueueCollection = CONSTANTS.IE_RequestQueue;            
        }

        public string GetIELastDataDict(string CorrelationId)
        {
            string lastDateDict = string.Empty;
            var collection = _database.GetCollection<BsonDocument>("IE_IngestData");
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, CorrelationId);
            var Projection = Builders<BsonDocument>.Projection.Include("lastDateDict").Exclude("_id");
            var Ingestdata = collection.Find(filter).Project<BsonDocument>(Projection).ToList();
            if (Ingestdata.Count > 0)
            {
                if (Ingestdata[0].Contains("lastDateDict") && Ingestdata[0]["lastDateDict"].ToString() != "{ }")
                {
                    lastDateDict = Ingestdata[0]["lastDateDict"]["Custom"]["DateColumn"].ToString();
                }
            }
            return lastDateDict;
        }

        public void IEInsertRequests(IERequestQueue ingrainRequest)
        {
            var collection = _database.GetCollection<IERequestQueue>(QueueCollection);
            collection.InsertOne(ingrainRequest);
        }

        public IERequestQueue IEGetFileRequestStatus(string correlationId, string pageInfo)
        {
            var collection = _database.GetCollection<IERequestQueue>(QueueCollection);
            var builder = Builders<IERequestQueue>.Filter;
            var filter = builder.Eq(CONSTANTS.CorrelationId, correlationId) & builder.Eq(CONSTANTS.pageInfo, pageInfo);
            var projection = Builders<IERequestQueue>.Projection.Exclude("_id");
            return collection.Find(filter).Project<IERequestQueue>(projection).ToList().FirstOrDefault();
        }

        public List<IERequestQueue> GetRequestStatusByConfigId(string inferenceConfigId, string pageInfo)
        {
            var collection = _database.GetCollection<IERequestQueue>(QueueCollection);
            var builder = Builders<IERequestQueue>.Filter;
            var filter = builder.Where(x => x.InferenceConfigId == inferenceConfigId);
            filter = string.IsNullOrEmpty(pageInfo) ? filter : filter & builder.Where(x => x.pageInfo == pageInfo);
            var projection = Builders<IERequestQueue>.Projection.Exclude("_id");
            return collection.Find(filter).Project<IERequestQueue>(projection).ToList();
        }

        public void UpdateIERequestQueue(string correlationid, string pageInfo, IEFileUpload fileUpload, string userId)
        {
            string Empty = null;
            var collection = _database.GetCollection<BsonDocument>(QueueCollection);
            var filterProjection = Builders<BsonDocument>.Projection.Exclude("_id");
            var filterbuilder = Builders<BsonDocument>.Filter;
            var filter = filterbuilder.Eq("CorrelationId", correlationid) & filterbuilder.Eq("pageInfo", pageInfo);
            var builder = Builders<BsonDocument>.Update;
            var update = builder
                  .Set("Status", "N")
                  .Set("ModelName", Empty)
                  .Set("RequestStatus", CONSTANTS.New)
                  .Set("RetryCount", 0)
                  .Set("ProblemType", Empty)
                  .Set("Message", Empty)
                  .Set("UniId", Empty)
                  .Set("Progress", Empty)
                  .Set("ParamArgs", fileUpload.ToJson())
               .Set("ModifiedBy", userId)
                          .Set("ModifiedOn", DateTime.UtcNow);
            collection.UpdateMany(filter, update);
        }



        public List<IERequestQueue> GetIERequestQueueByCorrId(string correlationId)
        {
            var collection = _database.GetCollection<IERequestQueue>(QueueCollection);
            var builder = Builders<IERequestQueue>.Filter;
            var filter = builder.Where(x => x.CorrelationId == correlationId);
            var projection = Builders<IERequestQueue>.Projection.Exclude("_id");
            var result = collection.Find(filter).Project<IERequestQueue>(projection).ToList();
            
            return result;
        }

        public List<IERequestQueue> GetIERequests(int requestBatchLimit)
        {
            var requestsCollection = _database.GetCollection<IERequestQueue>(QueueCollection);
            var filter = Builders<IERequestQueue>.Filter.Where(x => x.Status == "N");
            var projection = Builders<IERequestQueue>.Projection.Exclude("_id");
            return requestsCollection.Find(filter).Project<IERequestQueue>(projection).Limit(requestBatchLimit).ToList();
        }

        public List<IERequestQueue> GetAllInCompleteIERequest()
        {
            var requestsCollection = _database.GetCollection<IERequestQueue>(QueueCollection);
            var filter = (Builders<IERequestQueue>.Filter.Where(x => x.Status != "C")
                         & Builders<IERequestQueue>.Filter.Where(x => x.Status != "E"));
            var projection = Builders<IERequestQueue>.Projection.Exclude("_id");
            return requestsCollection.Find(filter).Project<IERequestQueue>(projection).ToList();
        }


        public void UpdateIEServiceRequestStatus(IERequestQueue aIServiceRequestStatus)
        {
            var requestsCollection = _database.GetCollection<IERequestQueue>(QueueCollection);
            var filter = Builders<IERequestQueue>.Filter.Where(x => x.CorrelationId == aIServiceRequestStatus.CorrelationId)
                         & Builders<IERequestQueue>.Filter.Where(x => x.RequestId == aIServiceRequestStatus.RequestId);
            var update = Builders<IERequestQueue>.Update.Set(x => x.Status, aIServiceRequestStatus.Status)
                                                                             .Set(x => x.Message, aIServiceRequestStatus.Message)
                                                                             .Set(x => x.ModifiedOn, DateTime.Now);
            requestsCollection.UpdateOne(filter, update);


        }


        public bool DeleteQueueRequestByConfig(string inferenceConfigId, string inferenceConfigType)
        {
            var requestsCollection = _database.GetCollection<IERequestQueue>(QueueCollection);
            var filter = Builders<IERequestQueue>.Filter.Where(x => x.InferenceConfigId == inferenceConfigId)
                         & Builders<IERequestQueue>.Filter.Where(x => x.InferenceConfigType == inferenceConfigType);
            requestsCollection.DeleteOne(filter);
            return true;
        }

        public bool DeleteQueueRequestById(string correlationId, string pageInfo, string requestId)
        {
            var requestsCollection = _database.GetCollection<IERequestQueue>(QueueCollection);
            var filter = Builders<IERequestQueue>.Filter.Where(x => x.CorrelationId == correlationId)
                         & Builders<IERequestQueue>.Filter.Where(x => x.pageInfo == pageInfo)
            & Builders<IERequestQueue>.Filter.Where(x => x.RequestId == requestId);
            requestsCollection.DeleteOne(filter);
            return true;
        }


        public bool DeleteQueueRequestByPageInfo(string correlationId, string pageInfo)
        {
            var requestsCollection = _database.GetCollection<IERequestQueue>(QueueCollection);
            var filter = Builders<IERequestQueue>.Filter.Where(x => x.CorrelationId == correlationId)
                         & Builders<IERequestQueue>.Filter.Where(x => x.pageInfo == pageInfo);
           
            requestsCollection.DeleteOne(filter);
            return true;
        }

        public IERequestQueue GetRequestQueuebyequestId(string correlationId, string pageInfo, string requestId)
        {
            var collection = _database.GetCollection<IERequestQueue>(QueueCollection);
            var builder = Builders<IERequestQueue>.Filter;
            var filter = Builders<IERequestQueue>.Filter.Where(x => x.CorrelationId == correlationId)
                         & Builders<IERequestQueue>.Filter.Where(x => x.pageInfo == pageInfo)
                         & Builders<IERequestQueue>.Filter.Where(x => x.RequestId == requestId);
            var projection = Builders<IERequestQueue>.Projection.Exclude("_id");
            return collection.Find(filter).Project<IERequestQueue>(projection).ToList().FirstOrDefault();
        }

        public IERequestQueue GetRequestByPageInfo(string correlationId, string pageInfo)
        {
            var collection = _database.GetCollection<IERequestQueue>(QueueCollection);
            var builder = Builders<IERequestQueue>.Filter;
            var filter = Builders<IERequestQueue>.Filter.Where(x => x.CorrelationId == correlationId)
                         & Builders<IERequestQueue>.Filter.Where(x => x.pageInfo == pageInfo);
            var projection = Builders<IERequestQueue>.Projection.Exclude("_id");
            return collection.Find(filter).Project<IERequestQueue>(projection).ToList().FirstOrDefault();
        }

        public IERequestQueue GetRequestQueueOnConfigId(string correlationId, string InferenceConfigId, string inferenceConfigType)
        {
            var collection = _database.GetCollection<IERequestQueue>(QueueCollection);
            var builder = Builders<IERequestQueue>.Filter;
            var filter = Builders<IERequestQueue>.Filter.Where(x => x.CorrelationId == correlationId)
                         & Builders<IERequestQueue>.Filter.Where(x => x.InferenceConfigId == InferenceConfigId)
                         & Builders<IERequestQueue>.Filter.Where(x => x.InferenceConfigType == inferenceConfigType);
            var projection = Builders<IERequestQueue>.Projection.Exclude("_id");
            return collection.Find(filter).Project<IERequestQueue>(projection).ToList().FirstOrDefault();
        }


        public void UpdateAutoTrainRequest(string requestId, string correlationId,string pageInfo, string status, string message, string progress)
        {
            var collection = _database.GetCollection<IERequestQueue>(QueueCollection);           
            var filter = Builders<IERequestQueue>.Filter.Where(x => x.CorrelationId == correlationId)
                         & Builders<IERequestQueue>.Filter.Where(x => x.pageInfo == pageInfo)
                         & Builders<IERequestQueue>.Filter.Where(x => x.RequestId == requestId);
            var update = Builders<IERequestQueue>.Update.Set(x => x.Status, status)
                                                        .Set(x => x.Progress, progress)
                                                        .Set(x => x.Message, message)
                                                        .Set(x => x.ModifiedOn, DateTime.UtcNow);
            collection.UpdateOne(filter, update);
        }


        public bool DeleteQueueRequest(string correlationId)
        {
            var requestsCollection = _database.GetCollection<IERequestQueue>(QueueCollection);
            var filter = Builders<IERequestQueue>.Filter.Where(x => x.CorrelationId == correlationId);
            requestsCollection.DeleteMany(filter);
            return true;
        }

        public bool DeleteAutoTrainQueueRequest(string correlationId)
        {
            var requestsCollection = _database.GetCollection<IERequestQueue>(QueueCollection);
            var filter = Builders<IERequestQueue>.Filter.Where(x => x.CorrelationId == correlationId)
                         & Builders<IERequestQueue>.Filter.Where(x => x.pageInfo != "AutoTrain");
            requestsCollection.DeleteMany(filter);
            return true;
        }


        public void BackupRecords(string correlationId, string pageInfo)
        {
            var requestsCollection = _database.GetCollection<IERequestQueue>(QueueCollection);
            var filter = Builders<IERequestQueue>.Filter.Where(x => x.CorrelationId == correlationId)
                         & Builders<IERequestQueue>.Filter.Where(x => x.pageInfo == pageInfo);
            var update = Builders<IERequestQueue>.Update.Set(x => x.CorrelationId, correlationId + "_backup");
            requestsCollection.UpdateMany(filter, update);
        }

        public void RestoreRecords(string correlationId, string pageInfo)
        {
            var requestsCollection = _database.GetCollection<IERequestQueue>(QueueCollection);
            var filter = Builders<IERequestQueue>.Filter.Where(x => x.CorrelationId == correlationId + "_backup")
                         & Builders<IERequestQueue>.Filter.Where(x => x.pageInfo == pageInfo);
            var update = Builders<IERequestQueue>.Update.Set(x => x.CorrelationId, correlationId);
            requestsCollection.UpdateMany(filter, update);
        }
    }
}
