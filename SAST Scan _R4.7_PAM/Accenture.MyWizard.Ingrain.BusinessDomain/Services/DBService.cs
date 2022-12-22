#region Namespace
using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
using Accenture.MyWizard.Ingrain.DataAccess;
using Accenture.MyWizard.Ingrain.DataModels.Models;
using Accenture.MyWizard.Shared.Helpers;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
#endregion

namespace Accenture.MyWizard.Ingrain.BusinessDomain.Services
{
    public class DBService : IDBService
    {
        #region Members
        private MongoClient _mongoClient;

        private IMongoDatabase _database;

        private readonly DatabaseProvider databaseProvider;
        private IOptions<IngrainAppSettings> appSettings { get; set; }

        FilterDefinition<BsonDocument> filters = null;
        #endregion

        #region Constructor
        public DBService(DatabaseProvider db, IOptions<IngrainAppSettings> settings, IServiceProvider serviceProvider)
        {
            databaseProvider = db;
            appSettings = settings;
            _mongoClient = databaseProvider.GetDatabaseConnection();
            var dataBaseName = MongoUrl.Create(appSettings.Value.connectionString).DatabaseName;
            _database = _mongoClient.GetDatabase(dataBaseName);
        }
        #endregion

        #region Methods
        public SSAIIngrainRequest GetSelfServiceRequestStatusCount()
        {
            SSAIIngrainRequest request = new SSAIIngrainRequest();
            var collection = _database.GetCollection<SSAIIngrainRequest>(CONSTANTS.SSAIIngrainRequests);            
            var filter = Builders<SSAIIngrainRequest>.Filter.Eq(CONSTANTS.RequestStatus, CONSTANTS.New) | Builders<SSAIIngrainRequest>.Filter.Eq(CONSTANTS.RequestStatus, CONSTANTS.Occupied) | Builders<SSAIIngrainRequest>.Filter.Eq(CONSTANTS.RequestStatus, CONSTANTS.In_Progress);
            var projection = Builders<SSAIIngrainRequest>.Projection.Include(CONSTANTS.RequestStatus).Exclude(CONSTANTS.Id);
            var result = collection.Find(filter).Project<SSAIIngrainRequest>(projection).ToList();
            if (result.Count > 0)
            {
                request.NewCount = result.Where(x => x.RequestStatus.Equals(CONSTANTS.New)).Count();
                request.OccupiedCount = result.Where(x => x.RequestStatus.Equals(CONSTANTS.Occupied)).Count();
                request.InProgressCount = result.Where(x => x.RequestStatus.Equals(CONSTANTS.In_Progress)).Count();
            }

            return request;
        }

        public void GetAIServiceRequestStatus()
        {
            throw new NotImplementedException();
        }

        public void GetIERequestStatus()
        {
            throw new NotImplementedException();
        }

        public DBDataInfo GetData(DBData dbData)
        {
            DBDataInfo dbDataInfo = new DBDataInfo();
            var collection = _database.GetCollection<BsonDocument>(dbData.CollectionName);
            this.SetFilters(dbData.FilterBy);            
            var projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
            var result = new List<BsonDocument>();
            if(dbData.SortBy != null)
            {
                if(dbData.SortBy.Order == 1)//Ascending
                    result = collection.Find(filters).Project<BsonDocument>(projection).Sort(Builders<BsonDocument>.Sort.Ascending(dbData.SortBy.Field)).ToList();
                else if(dbData.SortBy.Order == -1)//Descending
                    result = collection.Find(filters).Project<BsonDocument>(projection).Sort(Builders<BsonDocument>.Sort.Descending(dbData.SortBy.Field)).ToList();
            }
            else
                result = collection.Find(filters).Project<BsonDocument>(projection).ToList();            

            if (result.Count > 0)
            {                
                dbDataInfo.Count = collection.Find(filters).Project<BsonDocument>(projection).ToList().Count;
                dbDataInfo.Data = dbData.Limit > 0 ? JsonConvert.DeserializeObject<dynamic>(result.Take(dbData.Limit).ToJson()) :
                    JsonConvert.DeserializeObject<dynamic>(result.ToJson());
            }

            return dbDataInfo;
        }

        private void SetFilters(List<Filters> dataFilters)
        {
            var filterBuilder = Builders<BsonDocument>.Filter;
            foreach (var data in dataFilters)
            {
                FilterDefinition<BsonDocument> filter = null;
                switch (data.Type)
                {
                    case CONSTANTS.DType_Int:
                        filter = filterBuilder.Eq(data.Field, Convert.ToInt32(data.Value));
                        break;

                    case CONSTANTS.DType_String:
                        filter = filterBuilder.Eq(data.Field, Convert.ToString(data.Value));
                        break;

                    case CONSTANTS.DType_Double:
                    case CONSTANTS.DType_Float:
                        filter = filterBuilder.Eq(data.Field, Convert.ToDouble(data.Value));
                        break;

                    case CONSTANTS.DType_Bool:
                        filter = filterBuilder.Eq(data.Field, Convert.ToBoolean(data.Value));
                        break;
                }

                if (filters != null)
                    filters = filters & filter;
                else
                    filters = filter;
                //filters = filters != null ? filters &= filter : filter;
            }
        }
        #endregion
    }
}
