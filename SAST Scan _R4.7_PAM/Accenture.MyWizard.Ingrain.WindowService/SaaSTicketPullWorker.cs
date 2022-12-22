using Accenture.MyWizard.Ingrain.BusinessDomain.Services;
using Accenture.MyWizard.Ingrain.WindowService.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using WINSERVICEMODELS = Accenture.MyWizard.Ingrain.WindowService.Models;

namespace Accenture.MyWizard.Ingrain.WindowService
{
   public class SaaSTicketPullWorker : IHostedService, IDisposable //BackgroundService
    {
        #region Private Members
        //Timers
        System.Timers.Timer timer = new System.Timers.Timer();

        private System.ComponentModel.IContainer components = null;

        private readonly WINSERVICEMODELS.AppSettings appSettings = null;

        private DatabaseProvider databaseProvider;

        private IMongoDatabase _database;

        private Services.ATRTicketPullService _saasticketPullService;

        private Services.PAMTicketPullService _pamticketPullService;
        private readonly DP _dataProvider;
        private readonly PSP _projectStructureProvider;

        #endregion

        public SaaSTicketPullWorker()
        {
            appSettings = AppSettingsJson.GetAppSettings().GetSection("AppSettings").Get<WINSERVICEMODELS.AppSettings>();
            //Mongo connection
            databaseProvider = new DatabaseProvider();
            string connectionString = appSettings.connectionString;
            var dataBaseName = MongoUrl.Create(connectionString).DatabaseName;
            MongoClient mongoClient = databaseProvider.GetDatabaseConnection();
            _database = mongoClient.GetDatabase(dataBaseName);
            _saasticketPullService = new Services.ATRTicketPullService();
            _pamticketPullService = new Services.PAMTicketPullService();
            
            //end
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            System.Timers.Timer historicalPull = new System.Timers.Timer();
            historicalPull.AutoReset = true;
            historicalPull.Interval = TimeSpan.FromMinutes(Convert.ToDouble(appSettings.HistoricPullIntervalMinutes)).TotalMilliseconds;
            historicalPull.Elapsed += new ElapsedEventHandler(PAMHistoricalPull);
            historicalPull.Start();
            //this.PAMHistoricalPull();

            //Delta pull happens for every 10 mins
            System.Timers.Timer deltaPull = new System.Timers.Timer();
            deltaPull.AutoReset = true;
            deltaPull.Interval = TimeSpan.FromMinutes(Convert.ToDouble(Convert.ToDouble(appSettings.DeltaPullIntervalMinutes))).TotalMilliseconds;
            deltaPull.Elapsed += new ElapsedEventHandler(PAMDeltaPull);
            deltaPull.Start();
            return Task.CompletedTask;

        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(SaaSTicketPullWorker), "StopAsync", "Asset usage tracker stop at " + DateTime.Now.ToString(), string.Empty, string.Empty, string.Empty, string.Empty);
            return Task.CompletedTask;
        }


        //protected void PAMHistoricalPull()
        protected void PAMHistoricalPull(object sender, ElapsedEventArgs e)
        {
            try
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(SaaSTicketPullWorker), "PAMHistoricalPull", "START at " + DateTime.Now.ToString(), string.Empty, string.Empty, string.Empty, string.Empty);

                // DateTime StartDate = Convert.ToDateTime(_configuration["StartDate"]);
               // PAMBusinessLayer objDatamigration = new PAMBusinessLayer(_dataAccess, _config);

               // ClientEntityMapping objClient = new ClientEntityMapping();
                ClientInfo clientInfo = new ClientInfo();
                string entityType = "Ticket";
                //If IsSaaSDeployment is false, then normal flow
                if (appSettings.IsSaaSPlatform)
                {
                    var provisionedE2EIDs = _saasticketPullService.FetchProvisionedE2EID();
                    Parallel.ForEach(provisionedE2EIDs, new ParallelOptions { MaxDegreeOfParallelism = appSettings.HistoricPullThreadLimit }, (e2eIDs) =>
                      {
                          if (!_saasticketPullService.CheckPAMHistoricalPullTracker(e2eIDs.SAASProvisionDetails.ClientUID, e2eIDs.CAMConfigDetails.E2EUId, entityType, "Historical Pull"))
                          {
                              Parallel.Invoke(() =>
                               {
                                   LOGGING.LogManager.Logger.LogProcessInfo(typeof(SaaSTicketPullWorker), nameof(PAMHistoricalPull), "---------PAMHistoricalPull Historical Pull Starts for ClientUId: " + e2eIDs.SAASProvisionDetails.ClientUID + " EndToEndUId: " + e2eIDs.CAMConfigDetails.E2EUId + "---------", Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
                                   PAMHistoricalPullFetch(e2eIDs);
                                   LOGGING.LogManager.Logger.LogProcessInfo(typeof(SaaSTicketPullWorker), nameof(PAMHistoricalPull), "---------PAMHistoric Pull Ends for ClientUId: " + e2eIDs.SAASProvisionDetails.ClientUID + " EndToEndUId: " + e2eIDs.CAMConfigDetails.E2EUId + "-------- - ", Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
                               });
                          }
                          else { }
                      });
                }
                //else if (appSettings.IsClientDeployment)
                //{
                //    objClient.ClientUId = _config["PAM.Configuration:ClientUID"];
                //    objClient.ClientName = _config["PAM.Configuration:ClientName"];
                //    objClient.EndToEndUId = _config["PAM.Configuration:EndToEndUId"];
                //    objClient.Entity = _config["PAM.Configuration:EntityList"];//"Incident,Problem,ServiceRequest";
                //    clientInfo.ClientEntityMapping = objClient;

                //    PAMHistoricalPullFetch(clientInfo);
                //}
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(SaaSTicketPullWorker), nameof(PAMHistoricalPull), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);                
            }

            LOGGING.LogManager.Logger.LogProcessInfo(typeof(SaaSTicketPullWorker), nameof(PAMHistoricalPull), "END", Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
        }


        public void PAMHistoricalPullFetch(ClientInfo provisionedE2EID)
        {
           // PAMBusinessLayer objDatamigration = new PAMBusinessLayer(_dataAccess, _config);
            List<PAMHistoricalPullTracker> tracker = new List<PAMHistoricalPullTracker>();
            List<String> entityTypes = new List<String>() { "Ticket" };
            DateTime StartDate = DateTime.Parse(DateTime.Now.AddYears(appSettings.PullInterval).ToString("dd-MMM-yyyy 00:00:00"));//TODO:Ignore time                     
            //var date = StartDate.Date;
            // int intDays = Convert.ToInt32(_config["PAM.Configuration:DaysInterval"]);
            DateTime EndDate = DateTime.UtcNow; 
          
            //entityTypes.Add("Ticket");
            if (provisionedE2EID != null && !string.IsNullOrEmpty(provisionedE2EID.SAASProvisionDetails.ClientUID))
            {
                //check if historical pull entry in PAMHistoricalAndDeltaPullTracker
                foreach (string entityType in entityTypes)
                {
                   // PAMBusinessLayer objBL = new PAMBusinessLayer(_dataAccess, _config);
                    string pullType = "Historical Pull";
                    try
                    {
                        var isHistoricalPulldone = _saasticketPullService.CheckPAMHistoricalPullTracker(provisionedE2EID.SAASProvisionDetails.ClientUID, provisionedE2EID.CAMConfigDetails.E2EUId, entityType, pullType);
                        if (!isHistoricalPulldone)
                        {

                            _saasticketPullService.DeleteHistory(provisionedE2EID.SAASProvisionDetails.ClientUID, provisionedE2EID.CAMConfigDetails.E2EUId, entityType);                          
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(SaaSTicketPullWorker), nameof(PAMHistoricalPullFetch), "Deleted old data & Historical Pull Started for " + entityType + " for client " + provisionedE2EID.SAASProvisionDetails.ClientUID + " EndToEndUId " + provisionedE2EID.CAMConfigDetails.E2EUId + " between " + StartDate.ToString() + " and " + EndDate.ToString(), Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
                            _saasticketPullService.InsertPAMHistoricalPullTracker(provisionedE2EID.SAASProvisionDetails.ClientUID, provisionedE2EID.CAMConfigDetails.E2EUId, entityType, pullType, StartDate, EndDate);
                            tracker = _saasticketPullService.FetchPAMHistoricalPullTracker(provisionedE2EID.SAASProvisionDetails.ClientUID, provisionedE2EID.CAMConfigDetails.E2EUId, entityType, pullType);                          
                            //PAMMiddleLayerDataPush dataPushBA = new PAMMiddleLayerDataPush(_dataAccess, _config, _projectStructureProvider, _dataProvider);
                            Task task = _pamticketPullService.PAMTicketsPush(provisionedE2EID, entityType, pullType, StartDate, EndDate);
                            task.Wait();                            
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(SaaSTicketPullWorker), nameof(PAMHistoricalPullFetch), "==> Inserted  Tickets for Client - " + provisionedE2EID.SAASProvisionDetails.ClientName + " EndToEnd - " + provisionedE2EID.CAMConfigDetails.E2EName, Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
                        }
                        else
                        {                          
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(SaaSTicketPullWorker), nameof(PAMHistoricalPullFetch), "Historical Pull is already completed for the entity type : " + entityType, Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
                        }
                    }
                    catch (Exception ex)
                    {                        
                        LOGGING.LogManager.Logger.LogErrorMessage(typeof(SaaSTicketPullWorker), nameof(PAMHistoricalPullFetch), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                        _saasticketPullService.UpdatePAMHistoricalPullTracker(provisionedE2EID.SAASProvisionDetails.ClientUID, provisionedE2EID.CAMConfigDetails.E2EUId, entityType, pullType, 3, "Data Pull is failed", 0, null);
                        _saasticketPullService.InsertPAMHistoricalPullFailedTracker(tracker[0]); 
                    }
                }
            }
        }

        protected void PAMDeltaPull(object sender, ElapsedEventArgs e)
        {
            /* 
             Historical pull flag
                0 – Not Started
                1 - Completed
                2 – In Progress
                3 - Failed
             */            
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(SaaSTicketPullWorker), nameof(PAMDeltaPull), "---------PAMDeltaPull method is called---------", string.Empty, string.Empty, string.Empty, string.Empty);
            //PAMBusinessLayer objBL = new PAMBusinessLayer(_dataAccess, _config);
            List<PAMHistoricalPullTracker> tracker = new List<PAMHistoricalPullTracker>();
            string deltaPullType = "Delta Pull";
            string historicalPullType = "Historical Pull";
            DateTime startDate, endDate;
            try
            {
                // NameValueCollection _configuration = ConfigurationManager.GetSection("PAM.Configuration") as NameValueCollection;
                //ClientEntityMapping objClient = new ClientEntityMapping();
                ClientInfo clientInfo = new ClientInfo();
                //PAMBusinessLayer objDatamigration = new PAMBusinessLayer(_dataAccess, _config);
                string entityType = "Ticket";
                if (appSettings.IsSaaSPlatform)
                {
                    //var clientList = objDatamigration.FetchClientEntityData();
                    var provisionedE2EIDs = _saasticketPullService.FetchProvisionedE2EID();
                    Parallel.ForEach(provisionedE2EIDs, new ParallelOptions { MaxDegreeOfParallelism = appSettings.DeltaPullThreadLimit }, (e2eIDs) =>
                    {                                                
                        Parallel.Invoke(() =>
                        {                            
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(SaaSTicketPullWorker), nameof(PAMDeltaPull), "---------Delta Pull Started for ClientUId: " + e2eIDs.SAASProvisionDetails.ClientUID + " EndToEndUId: " + e2eIDs.CAMConfigDetails.E2EUId + "---------", string.Empty, string.Empty, string.Empty, string.Empty);
                            PAMDeltaPullFetch(e2eIDs);                            
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(SaaSTicketPullWorker), nameof(PAMDeltaPull), "---------Delta Pull Ends for ClientUId: " + e2eIDs.SAASProvisionDetails.ClientUID + " EndToEndUId: " + e2eIDs.CAMConfigDetails.E2EUId + "---------", string.Empty, string.Empty, string.Empty, string.Empty);
                        });
                    });
                }
                //else if (_config["PAM.Configuration:isClientDeployment"] == "Y")
                //{
                //    objClient.ClientUId = _config["PAM.Configuration:ClientUID"];
                //    objClient.ClientName = _config["PAM.Configuration:ClientName"];
                //    objClient.EndToEndUId = _config["PAM.Configuration:EndToEndUId"];
                //    objClient.Entity = _config["PAM.Configuration:EntityList"];//"Incident,Problem,ServiceRequest";
                //    clientInfo.ClientEntityMapping = objClient;
                //    PAMDeltaPullFetch(clientInfo);
                //}                                
            }
            catch (Exception ex)
            {                
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(SaaSTicketPullWorker), nameof(PAMDeltaPull), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }

            LOGGING.LogManager.Logger.LogProcessInfo(typeof(SaaSTicketPullWorker), nameof(PAMDeltaPull), "END", Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
        }

        protected void PAMDeltaPullFetch(ClientInfo provisionedE2EID)
        {
            /* 
             Historical pull flag
                0 – Not Started
                1 - Completed
                2 – In Progress
                3 - Failed
             */            
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(SaaSTicketPullWorker), nameof(PAMDeltaPullFetch), "---------PAMDeltaPull method is called---------", Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
            //PAMBusinessLayer objBL = new PAMBusinessLayer(_dataAccess, _config);
            List<PAMHistoricalPullTracker> tracker = new List<PAMHistoricalPullTracker>();
            string deltaPullType = "Delta Pull";
            string historicalPullType = "Historical Pull";
            DateTime startDate, endDate;
            try
            {
                #region Configuration
                // NameValueCollection _configuration = ConfigurationManager.GetSection("PAM.Configuration") as NameValueCollection;
                //PAMBusinessLayer objDatamigration = new PAMBusinessLayer(_dataAccess, _config);
                List<String> entityTypes = new List<String>() { "Ticket" };
                //entityTypes.Add("Incident");
                //entityTypes.Add("Problem");
                //entityTypes.Add("Service_Request");
                entityTypes.Add("Ticket");
                #endregion

                #region Call Delta pull for each entity type
                foreach (string entityType in entityTypes)
                {
                    try
                    {
                        #region Check PAMHistoricalAndDeltaPullTracker for each entity type
                        tracker = _saasticketPullService.FetchPAMHistoricalPullTracker(provisionedE2EID.SAASProvisionDetails.ClientUID, provisionedE2EID.CAMConfigDetails.E2EUId, entityType, deltaPullType);
                        if (tracker == null || tracker.Count == 0) // if no record available for delta pull (i.e 1st time delta pull)
                        {
                            //fetch historical pull record for the entity type
                            tracker = _saasticketPullService.FetchPAMHistoricalPullTracker(provisionedE2EID.SAASProvisionDetails.ClientUID, provisionedE2EID.CAMConfigDetails.E2EUId, entityType, historicalPullType);
                            //insert record for delta pull only if historical pull is completed
                            if (tracker != null && tracker.Count > 0 && tracker[0].Flag == 1)
                            {
                                _saasticketPullService.InsertPAMHistoricalPullTracker(provisionedE2EID.SAASProvisionDetails.ClientUID, provisionedE2EID.CAMConfigDetails.E2EUId, entityType, deltaPullType,
                                    (tracker[0].dfLastUpdatedDate != null ? tracker[0].dfLastUpdatedDate.Value : tracker[0].EndDate), DateTime.UtcNow);
                            }
                        }
                        #region Initiate delta pull only if the previous delta pull is completed status or not started status or failed status
                        if (tracker != null && tracker.Count > 0 && tracker[0].Flag != 2)
                        {
                            //If Previous historical pull failed, consume data for the same duration
                            if (tracker[0].Flag == 3)
                                startDate = tracker[0].StartDate;
                            else
                                startDate = (tracker[0].dfLastUpdatedDate != null ? tracker[0].dfLastUpdatedDate.Value : tracker[0].EndDate); //tracker[0].EndDate;

                            endDate = DateTime.UtcNow;                            
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(SaaSTicketPullWorker), nameof(PAMDeltaPullFetch), "Delta Pull Started for " + entityType + " for client " + provisionedE2EID.SAASProvisionDetails.ClientUID + " EndToEndUId" + provisionedE2EID.CAMConfigDetails.E2EUId + " between " + startDate.ToString() + " and " + endDate.ToString(), Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
                            //update flag and status as in progress
                            _saasticketPullService.UpdatePAMHistoricalPullTracker(provisionedE2EID.SAASProvisionDetails.ClientUID, provisionedE2EID.CAMConfigDetails.E2EUId, entityType, deltaPullType, 2, "Data Pull is in progress", 0, startDate, endDate, DateTime.UtcNow);                            
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(SaaSTicketPullWorker), nameof(PAMDeltaPullFetch), "==> Inserting " + entityType + " Tickets for Client- " + provisionedE2EID.SAASProvisionDetails.ClientName + " EndToEndName- " + provisionedE2EID.CAMConfigDetails.E2EName, Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
                            //PAMMiddleLayerDataPush dataPushBA = new PAMMiddleLayerDataPush(_dataAccess, _config, _projectStructureProvider, _dataProvider);
                            Task task = _pamticketPullService.PAMTicketsPush(provisionedE2EID, entityType, deltaPullType, startDate, endDate);
                            task.Wait();                            
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(SaaSTicketPullWorker), nameof(PAMDeltaPullFetch), "==> Inserted Tickets for Client-  " + provisionedE2EID.SAASProvisionDetails.ClientName + " EndToEndName- " + provisionedE2EID.CAMConfigDetails.E2EName, Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
                        }
                        #endregion
                        #endregion
                    }
                    catch (Exception ex)
                    {                        
                        LOGGING.LogManager.Logger.LogErrorMessage(typeof(SaaSTicketPullWorker), nameof(PAMDeltaPullFetch), ex.Message, ex, "Error in PAMDeltaPull for the entity type : " + entityType, string.Empty, string.Empty, string.Empty);                        
                        _saasticketPullService.UpdatePAMHistoricalPullTracker(provisionedE2EID.SAASProvisionDetails.ClientUID, provisionedE2EID.CAMConfigDetails.E2EUId, entityType, deltaPullType, 3, "Data Pull is failed", 0, null);
                        _saasticketPullService.InsertPAMHistoricalPullFailedTracker(tracker[0]);
                    }
                }
                #endregion                
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(SaaSTicketPullWorker), nameof(PAMDeltaPullFetch), "END", Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
            }
            catch (Exception ex)
            {                
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(SaaSTicketPullWorker), nameof(PAMDeltaPullFetch), ex.Message, ex, "Error in PAMDeltaPull ", string.Empty, string.Empty, string.Empty);
            }
        }

        #region Dispose
        public void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
        }

        public void Dispose()
        {
            timer?.Dispose();
        }

        #endregion

    }
}
