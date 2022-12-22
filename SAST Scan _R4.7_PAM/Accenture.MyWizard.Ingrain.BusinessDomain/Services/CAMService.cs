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
    using System.Threading.Tasks;
    using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
    #endregion
    public class CAMService : ICAMService
    {
        #region Members
        private MongoClient _mongoClient;
        private IMongoDatabase _database;
        private readonly IOptions<IngrainAppSettings> appSettings;
        DatabaseProvider databaseProvider;        
        #endregion

        #region Constructors
        /// <summary>
        /// Constructor to Initialize mongoDB Connection
        /// </summary>
        public CAMService(DatabaseProvider db, IOptions<IngrainAppSettings> settings)
        {
            databaseProvider = db;
            appSettings = settings;
            _mongoClient = databaseProvider.GetDatabaseConnection();
            var dataBaseName = MongoUrl.Create(appSettings.Value.connectionString).DatabaseName;
            _database = _mongoClient.GetDatabase(dataBaseName);     
        }
        #endregion

        #region Methods
        public async Task<string> PushATRProvisionToDBAsync(ATRProvisionRequestDto atr)
        {
            string response = string.Empty;

            var collection = _database.GetCollection<ATRProvisionRequestDto>(CONSTANTS.DB_CAMCONFIGURATION);
            var filterBuilder = Builders<ATRProvisionRequestDto>.Filter;
            var filter = filterBuilder.Eq(nameof(ATRProvisionRequestDto.E2EUId), atr.E2EUId);
            var countResult = await collection.CountDocumentsAsync(filter);
            if (countResult == 0)
            {
                await collection.InsertOneAsync(atr);
                response = "Success";
            } else
            {
                var update = Builders<ATRProvisionRequestDto>.Update.Set(x => x.DeliveryConstructUId, atr.DeliveryConstructUId)
                                                                                 .Set(x => x.E2EName, atr.E2EName)
                                                                                 .Set(x => x.DF_TicketPull_API, atr.DF_TicketPull_API)
                                                                                 .Set(x => x.API_Token_Generation, atr.API_Token_Generation)
                                                                                 .Set(x => x.Username, atr.Username)
                                                                                 .Set(x => x.Password, atr.Password);
                collection.UpdateOne(filter, update);
                response = "Success";
            }
          
            return response;
        }
        #endregion

    }
}
