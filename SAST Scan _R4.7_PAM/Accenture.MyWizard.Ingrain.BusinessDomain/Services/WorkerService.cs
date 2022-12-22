using System;
using System.Collections.Generic;
using System.Text;
using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
using Accenture.MyWizard.Ingrain.DataAccess;
using Accenture.MyWizard.Ingrain.DataModels.Models;
using Accenture.MyWizard.Shared.Helpers;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using MongoDB.Bson;
using System.Linq;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
using Newtonsoft.Json;


namespace Accenture.MyWizard.Ingrain.BusinessDomain.Services
{
    public class WorkerService : IWorkerService
    {
        private MongoClient _mongoClient;
        private IMongoDatabase _database;
        private readonly DatabaseProvider databaseProvider;
        private readonly IOptions<IngrainAppSettings> appSettings;
        private IIngestedData _ingestedDataService { get; set; }
        public static IEncryptionDecryption _encryptionDecryption { set; get; }
        public IGenericSelfservice _genericSelfservice { get; set; }
        public IFlushService _flushService { get; set; }

        private CallBackErrorLog auditTrailLog;

        private string userID;
        private string password;
        private string tokenURL;

        private string pamTokenUserName;
        private string pamTokenUserPWD;
        private string pamIAMTokenUrl;
        private string pamDeliveryConstructsUrl;
        
        #region Constructors

        public WorkerService(DatabaseProvider db, IOptions<IngrainAppSettings> settings, IServiceProvider serviceProvider)
        {

            databaseProvider = db;
            appSettings = settings;
            _mongoClient = databaseProvider.GetDatabaseConnection();
            var dataBaseName = MongoUrl.Create(appSettings.Value.connectionString).DatabaseName;
            _database = _mongoClient.GetDatabase(dataBaseName);
            _encryptionDecryption = serviceProvider.GetService<IEncryptionDecryption>();
            _ingestedDataService = serviceProvider.GetService<IIngestedData>();
            _genericSelfservice = serviceProvider.GetService<IGenericSelfservice>();
            _flushService = serviceProvider.GetService<IFlushService>();
            auditTrailLog = new CallBackErrorLog();
        }
        #endregion
        public WorkerServiceInfo GetWorkerServiceStatus()
        {
            WorkerServiceInfo workerServiceData = new WorkerServiceInfo();
            var Workerservicecollection = _database.GetCollection<WorkerServiceInfo>(CONSTANTS.SSAIWorkerServiceJobs);
            var filter = Builders<WorkerServiceInfo>.Filter.Eq(CONSTANTS.Status, CONSTANTS.WorkerServiceRunning) | Builders<WorkerServiceInfo>.Filter.Eq(CONSTANTS.Status, CONSTANTS.WorkerServiceStopped);
            var inputData = Workerservicecollection.Find(filter).ToList();
            if (inputData.Count > 0)
               {
                workerServiceData  = JsonConvert.DeserializeObject<WorkerServiceInfo>(inputData[0].ToJson());
               }
                return workerServiceData;

        }
    }
}
