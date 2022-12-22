using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.WindowService.Services
{
    #region Namespace References
    using MongoDB.Driver;
    using System;
    using System.Collections.Generic;
    using System.Text;
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
    using Accenture.MyWizard.Ingrain.WindowService.Services;
    using Accenture.MyWizard.Ingrain.WindowService.Models.SaaS;
    using System.Security.Cryptography;
    using System.IO;
    using System.Xml.Linq;

    // using Microsoft.Extensions.DependencyInjection;
    #endregion
    public class PAMTicketPullService : IPAMTicketPull
    {
        private const string EncryptionKey = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private readonly AppSettings appSettings = null;
        private readonly IMongoDatabase _database;
        ATRTicketPullService _sTicketPullService;
        DataProviderService _dataProviderservice;
        private readonly DP _dataProvider;
        private readonly PSP _projectStructureProvider;        

        public PAMTicketPullService()
        {
            appSettings = AppSettingsJson.GetAppSettings().GetSection("AppSettings").Get<AppSettings>();
            DATAACCESS.DatabaseProvider databaseProvider = new DATAACCESS.DatabaseProvider();
            var dataBaseName = MongoUrl.Create(appSettings.connectionString).DatabaseName;
            MongoClient mongoClient = databaseProvider.GetDatabaseConnection();
            _database = mongoClient.GetDatabase(dataBaseName);
            _sTicketPullService = new ATRTicketPullService();
            _dataProviderservice = new DataProviderService();
            _projectStructureProvider = AppSettingsJson.GetAppSettings().GetSection("ProjectStructureProviders").Get<PSP>();
            _dataProvider = AppSettingsJson.GetAppSettings().GetSection("DataProviders").Get<DP>();
        }
        public async Task<Boolean> PAMTicketsPush(ClientInfo e2eIds, string entityType, string pullType, DateTime startDate, DateTime endDate)
        {

            int recordCount = 0;            
            string response = string.Empty;
            string dataprovidertype = "Tickets";
            try
            {
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    MissingMemberHandling = MissingMemberHandling.Ignore
                };

                List<PAMHistoricalPullTracker> tracker = new List<PAMHistoricalPullTracker>();
                tracker = _sTicketPullService.FetchPAMHistoricalPullTracker(e2eIds.SAASProvisionDetails.ClientUID, e2eIds.CAMConfigDetails.E2EUId, entityType, pullType);
                AIAAMiddleLayerResponse MiddleLayerresponse = new AIAAMiddleLayerResponse();
                DateTime fromDate = startDate;
                DateTime toDate = endDate;
                int ticketCount;
                DateTime maxLastUpdatedDate;
                DateTime loopStartDate = DateTime.UtcNow;
                int TotalTicketsCount = 0;
                string ticketid = string.Empty;
                DateTime dtdate = DateTime.UtcNow;
                DateTime? maxLastDFUpdatedDate = DateTime.UtcNow;

                do
                {
                    ticketCount = 0;
                    if (loopStartDate != fromDate || fromDate <= dtdate)
                    {
                        loopStartDate = fromDate;
                        if (string.Compare(pullType, "Historical Pull") == 0)
                        {
                            if (Convert.ToDateTime(fromDate.ToShortDateString()) >= Convert.ToDateTime(DateTime.UtcNow.ToShortDateString()))
                            {
                                break;
                            }
                            int intDays = Convert.ToInt32(appSettings.DaysInterval);
                            toDate = fromDate.AddDays(intDays);

                            //If calculated todate is greater than today then set to date as "Today"
                            if (toDate > dtdate)
                                toDate = dtdate;
                        }


                        AIAAMiddleLayerRequest request = CreatePAMDataProviderRequest(e2eIds.SAASProvisionDetails.ClientUID, entityType, dataprovidertype, fromDate, toDate);

                        if (appSettings.IsSaaSPlatform && e2eIds.CAMConfigDetails != null)
                        {
                            request.DataProviders[0].AuthProvider.UserName = e2eIds.CAMConfigDetails.Username;
                            request.DataProviders[0].AuthProvider.Password = Decrypt(e2eIds.CAMConfigDetails.Password);
                            request.DataProviders[0].ServiceUrl = e2eIds.CAMConfigDetails.DF_TicketPull_API;
                            request.DataProviders[0].AuthProvider.FederationUrl = e2eIds.CAMConfigDetails.API_Token_Generation;
                            request.ClientUId = e2eIds.SAASProvisionDetails.ClientUID;
                            request.EndToEndUId = e2eIds.CAMConfigDetails.E2EUId;

                            string logPayLoad = "clientUId: " + e2eIds.SAASProvisionDetails.ClientUID + ", UserName: " + request.DataProviders[0].AuthProvider.UserName + " Password: " + request.DataProviders[0].AuthProvider.Password
                                + " ServiceUrl: " + request.DataProviders[0].ServiceUrl + " FederationUrl:" + request.DataProviders[0].AuthProvider.FederationUrl;

                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(PAMTicketPullService), nameof(PAMTicketsPush), logPayLoad, Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
                        }
                        else
                        {
                            request.ClientUId = appSettings.ClientUID;
                            request.EndToEndUId = appSettings.EndToEndUId;
                        }

                        string MLreq = JsonConvert.SerializeObject(request);
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(PAMTicketPullService), nameof(PAMTicketsPush), "After createPAMDataProviderRequest======" + request.DataProviders.Count, Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
                        // Log.Information("Middle layer request for calling GetHistoricalTicketEntities method : " + MLreq);
                        //Log.Information("after createPAMDataProviderRequest======" + request.DataProviders.Count);

                        response = await GetHistoricalTicketEntities(JsonConvert.SerializeObject(request)).ConfigureAwait(false);//TODO
                                                                                                                                 //Log.Information("response " + response);
                        MiddleLayerresponse = JsonConvert.DeserializeObject<AIAAMiddleLayerResponse>(response, settings);
                        //Log.Information("MiddleLayerresponse" + MiddleLayerresponse);
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(PAMTicketPullService), nameof(PAMTicketsPush), "MiddleLayerresponse" + MiddleLayerresponse, Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
                        if (!string.IsNullOrEmpty(MiddleLayerresponse.Error)) // if any exception occurs at middle layer
                        {
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(PAMTicketPullService), nameof(PAMTicketsPush), "Before updatePAMHistoricalPullTracker", Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
                            //Log.Information("before updatePAMHistoricalPullTracker");
                            UpdatePAMHistoricalPullTracker(e2eIds.SAASProvisionDetails.ClientUID, e2eIds.CAMConfigDetails.E2EUId, entityType, pullType, 3, "Data Pull is failed", recordCount, null);
                            InsertPAMHistoricalPullFailedTracker(tracker[0]);
                            string logMessage = pullType + " failed for " + entityType + " for client " + e2eIds.SAASProvisionDetails.ClientUID + " EndToEndUId " + e2eIds.CAMConfigDetails.E2EUId + " between " + startDate.ToString() + " and "
                             + endDate.ToString() + " in middle layer : " + MiddleLayerresponse.Error;
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(PAMTicketPullService), nameof(PAMTicketsPush), logMessage, Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
                            break;
                        }

                        HistoricalTicketList ticketList = JsonConvert.DeserializeObject<HistoricalTicketList>(MiddleLayerresponse.Response, settings);
                        HistoricalIOTicketList ioTicketList = JsonConvert.DeserializeObject<HistoricalIOTicketList>(MiddleLayerresponse.Response, settings);
                        if (ticketList != null && ticketList.Tickets != null && ticketList.Tickets.Count > 0) //filter out tickets
                        {
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(PAMTicketPullService), nameof(PAMTicketsPush), pullType + " between " + startDate.ToString() + " and "
                            + endDate.ToString() + " in total count : " + ticketList.Tickets.Count, Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
                            recordCount = ticketList.Tickets.Count;
                            ticketCount = recordCount;
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(PAMTicketPullService), nameof(PAMTicketsPush), "Received response from middle layer with Record Count : " + recordCount.ToString(), Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(PAMTicketPullService), nameof(PAMTicketsPush), "Converting DeliveryConstructs to DeliveryConstruct object", Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
                            RemoveNullDeliveryConstructs(ticketList.Tickets);
                            RemoveNullDeliveryConstructsIO(ioTicketList.Tickets);
                            List<PAMTicket> incidentTickets = ticketList.Tickets.Where(x => (!string.IsNullOrEmpty(x.NormalisedTicketType) && x.NormalisedTicketType.ToLower().Equals("incidents") && !string.IsNullOrEmpty(x.Domain) && x.Domain.ToLower().Equals("ao"))).ToList();
                            List<PAMTicket> problemTickets = ticketList.Tickets.Where(x => (!string.IsNullOrEmpty(x.NormalisedTicketType) && x.NormalisedTicketType.ToLower().Equals("problemtickets") && !string.IsNullOrEmpty(x.Domain) && x.Domain.ToLower().Equals("ao"))).ToList();
                            List<PAMTicket> serviceRequestTickets = ticketList.Tickets.Where(x => (!string.IsNullOrEmpty(x.NormalisedTicketType) && x.NormalisedTicketType.ToLower().Equals("servicerequests") && !string.IsNullOrEmpty(x.Domain) && x.Domain.ToLower().Equals("ao"))).ToList();
                            List<PAMTicket> changeManagementTickets = ticketList.Tickets.Where(x => (!string.IsNullOrEmpty(x.NormalisedTicketType) && x.NormalisedTicketType.ToLower().Equals("changemanagement") && !string.IsNullOrEmpty(x.Domain) && x.Domain.ToLower().Equals("ao"))).ToList();
                            List<IOTicketCollection> ioTickets = ioTicketList.Tickets.Where(x => ((string.IsNullOrEmpty(x.Domain) || x.Domain.ToLower().Equals("io") || x.Domain.ToLower().Equals("na")) && ((!string.IsNullOrEmpty(x.NormalisedTicketType) && !(x.NormalisedTicketType.ToLower().Equals("Task")))))).ToList();
                            List<PAMTicket> AOTask = ticketList.Tickets.Where(x => (!string.IsNullOrEmpty(x.NormalisedTicketType) && x.NormalisedTicketType.ToLower().Equals("task") && !string.IsNullOrEmpty(x.Domain) && x.Domain.ToLower().Equals("ao"))).ToList();
                            List<PAMTicket> IOTask = ticketList.Tickets.Where(x => (!string.IsNullOrEmpty(x.NormalisedTicketType) && x.NormalisedTicketType.ToLower().Equals("task") && !string.IsNullOrEmpty(x.Domain) && x.Domain.ToLower().Equals("io"))).ToList();
                            //List<IOTicketCollection> ioTickets = ioTicketList.Tickets.ToList();

                            recordCount = incidentTickets.Count + problemTickets.Count + serviceRequestTickets.Count + changeManagementTickets.Count + ioTickets.Count;
                            TotalTicketsCount = TotalTicketsCount + recordCount;                         
                            string logRecordCount = "Incidents Record Count : " + incidentTickets.Count + "Problems Record Count : " + problemTickets.Count + "ServiceRequest Record Count : " + serviceRequestTickets.Count
                                + "ChangeManagement Record Count : " + changeManagementTickets.Count + "AOTask Record Count : " + AOTask.Count + "IOTask Record Count : " + IOTask.Count +
                                "IO Tickets Record Count : " + ioTickets.Count;
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(PAMTicketPullService), nameof(PAMTicketsPush), logRecordCount, Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
                            if (AOTask != null && AOTask.Count > 0)
                            {
                                PAMTicketEntitiesDeltaPush(AOTask, "AOTask", e2eIds.SAASProvisionDetails.ClientUID, e2eIds.CAMConfigDetails.E2EUId);
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(PAMTicketPullService), nameof(PAMTicketsPush), "Completed insertion of Task with Record Count : " + AOTask.Count, Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty);

                                if (AOTask.Max(x => x.LastModifiedTime) >= maxLastDFUpdatedDate)
                                    maxLastDFUpdatedDate = AOTask.Max(x => x.LastModifiedTime);
                            }
                            if (IOTask != null && IOTask.Count > 0)
                            {
                                PAMTicketEntitiesDeltaPush(IOTask, "IOTask", e2eIds.SAASProvisionDetails.ClientUID, e2eIds.CAMConfigDetails.E2EUId);
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(PAMTicketPullService), nameof(PAMTicketsPush), "Completed insertion of Task with Record Count : " + IOTask.Count, Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty);

                                if (IOTask.Max(x => x.LastModifiedTime) >= maxLastDFUpdatedDate)
                                    maxLastDFUpdatedDate = IOTask.Max(x => x.LastModifiedTime);
                            }
                            if (incidentTickets != null && incidentTickets.Count > 0)
                            {
                                PAMTicketEntitiesDeltaPush(incidentTickets, "Incident", e2eIds.SAASProvisionDetails.ClientUID, e2eIds.CAMConfigDetails.E2EUId);
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(PAMTicketPullService), nameof(PAMTicketsPush), "Completed insertion of Incidents with Record Count : " + incidentTickets.Count, Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty);

                                if (incidentTickets.Max(x => x.LastModifiedTime) >= maxLastDFUpdatedDate)
                                    maxLastDFUpdatedDate = incidentTickets.Max(x => x.LastModifiedTime);
                            }
                            if (problemTickets != null && problemTickets.Count > 0)
                            {
                                PAMTicketEntitiesDeltaPush(problemTickets, "Problem", e2eIds.SAASProvisionDetails.ClientUID, e2eIds.CAMConfigDetails.E2EUId);
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(PAMTicketPullService), nameof(PAMTicketsPush), "Completed insertion of Problems with Record Count : " + problemTickets.Count, Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty);

                                if (problemTickets.Max(x => x.LastModifiedTime) >= maxLastDFUpdatedDate)
                                    maxLastDFUpdatedDate = problemTickets.Max(x => x.LastModifiedTime);
                            }
                            if (serviceRequestTickets != null && serviceRequestTickets.Count > 0)
                            {
                                PAMTicketEntitiesDeltaPush(serviceRequestTickets, "Service_Request", e2eIds.SAASProvisionDetails.ClientUID, e2eIds.CAMConfigDetails.E2EUId);
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(PAMTicketPullService), nameof(PAMTicketsPush), "Completed insertion of ServiceRequest with Record Count : " + serviceRequestTickets.Count, Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty);

                                if (serviceRequestTickets.Max(x => x.LastModifiedTime) >= maxLastDFUpdatedDate)
                                    maxLastDFUpdatedDate = serviceRequestTickets.Max(x => x.LastModifiedTime);
                            }
                            if (changeManagementTickets != null && changeManagementTickets.Count > 0)
                            {
                                PAMTicketEntitiesDeltaPush(changeManagementTickets, "Change_Management", e2eIds.SAASProvisionDetails.ClientUID, e2eIds.CAMConfigDetails.E2EUId);
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(PAMTicketPullService), nameof(PAMTicketsPush), "Completed insertion of ChangeManagement with Record Count : " + changeManagementTickets.Count, Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty);

                                if (changeManagementTickets.Max(x => x.LastModifiedTime) >= maxLastDFUpdatedDate)
                                    maxLastDFUpdatedDate = changeManagementTickets.Max(x => x.LastModifiedTime);
                            }
                            if (ioTickets != null && ioTickets.Count > 0)
                            {
                                PAMIOTicketDeltaPush(ioTickets, e2eIds.SAASProvisionDetails.ClientUID, e2eIds.CAMConfigDetails.E2EUId);
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(PAMTicketPullService), nameof(PAMTicketsPush), "Completed insertion of IO Tickets with Record Count : " + ioTickets.Count, Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty);

                                if (ioTickets.Max(x => x.LastModifiedTime) >= maxLastDFUpdatedDate)
                                    maxLastDFUpdatedDate = ioTickets.Max(x => x.LastModifiedTime);
                            }

                            //if (string.Compare(pullType, "Historical Pull") == 0)
                            //{
                            //    PAMTicketEntitiesPush(ticketList.Tickets, entityType);
                            //}
                            //else
                            //{
                            //PAMTicketEntitiesDeltaPush(ticketList.Tickets, entityType);
                            //}


                            //COmmented below code as paging is handling during service call
                            //maxLastUpdatedDate = ticketList.Tickets.Max(x => x.LastModifiedTime);
                            //var maxticketid = ticketList.Tickets.Where(x => x.LastModifiedTime == maxLastUpdatedDate).Select(x => x.ID).FirstOrDefault();
                            //if (maxticketid != ticketid)
                            //{
                            //    fromDate = maxLastUpdatedDate;
                            //    ticketid = maxticketid;
                            //}
                            //else
                            //{
                            //    fromDate = toDate;
                            //}
                            //Comment end

                            fromDate = toDate;

                            //insertUpdatePAMDateFilterTracker(clientUId, entityType, pullType, maxLastUpdatedDate, endDate);

                            if (ticketList.Tickets.Count > 0)
                                maxLastDFUpdatedDate = ticketList.Tickets.Max(x => x.LastModifiedTime);
                        }
                        else
                        {

                            if (string.Compare(pullType, "Delta Pull") == 0)
                            {
                                break;
                            }
                            fromDate = toDate;
                        }
                    }
                    else
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(PAMTicketPullService), nameof(PAMTicketsPush), pullType + " between " + startDate.ToString() + " and "
                            + endDate.ToString() + " in total count : " + 0, Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
                        break;
                    }
                } while (ticketCount > 0 || toDate < DateTime.UtcNow);
                if (string.IsNullOrEmpty(MiddleLayerresponse.Error))
                {
                    UpdatePAMHistoricalPullTracker(e2eIds.SAASProvisionDetails.ClientUID, e2eIds.CAMConfigDetails.E2EUId, entityType, pullType, 1, "Data Pull is completed", TotalTicketsCount, maxLastDFUpdatedDate);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(PAMTicketPullService), nameof(PAMTicketsPush), pullType + " completed for " + entityType + " for client " + e2eIds.SAASProvisionDetails.ClientUID + " for EndToEndUId " + e2eIds.CAMConfigDetails.E2EUId + " between " + startDate.ToString() + " and "
                        + endDate.ToString() + " with Record Count : " + TotalTicketsCount.ToString(), Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(PAMTicketPullService), nameof(PAMTicketsPush), pullType + " for ClientUId:" + e2eIds.SAASProvisionDetails.ClientUID.ToString() + " for EndToEndUId " + e2eIds.CAMConfigDetails.E2EUId + " fetched Records: " + TotalTicketsCount, Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty);                    
                }
                return true;
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(PAMTicketPullService), nameof(PAMTicketsPush), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                throw ex;
            }

        }


        public void PAMIOTicketDeltaPush(List<IOTicketCollection> ticketList, string ClientUId, string EndToEndUId)
        {            
            try
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(PAMTicketPullService), nameof(PAMIOTicketDeltaPush), "START", Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
                if (ticketList != null && ticketList.Count > 0)
                {
                    ticketList.RemoveAll(e => e == null);
                }

                ticketList.ForEach(x => x.EffortInHours = Math.Round(x.Effort / 3600000, 2));

                //List<Data_Models.Insight.DeliveryConstruct> lstNullDC = new List<Data_Models.Insight.DeliveryConstruct>();
                //ticketList.ForEach(x => x.EffortInHours = Math.Round(x.Effort / 3600000, 2));

                //List<PAMTicket> ticketsWithTAT = calculateTAT(ticketList);

                PAMIOTicketDeltaPushByType(ticketList, CONSTANTS.DB_IOTicketCollection, ClientUId, EndToEndUId);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(PAMTicketPullService), nameof(PAMIOTicketDeltaPush), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                throw ex;
            }            
        }

        public void PAMTicketEntitiesDeltaPush(List<PAMTicket> ticketList, string entityType, string ClientUId, string EndToEndUId)
        {
            try
            {                
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(PAMTicketPullService), nameof(PAMTicketEntitiesDeltaPush), "START", Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
                if (ticketList != null && ticketList.Count > 0)
                {
                    ticketList.RemoveAll(e => e == null);
                }
                ticketList.ForEach(x => x.EffortInHours = Math.Round(x.Effort / 3600000, 2));
                List<PAMTicket> ticketsWithTAT = CalculateTAT(ticketList);//TODO
                switch (entityType)
                {
                    case "Incident":
                        PAMTicketEntitiesDeltaPushByType(ticketsWithTAT, CONSTANTS.DB_IncidentCollection, ClientUId, EndToEndUId);
                        break;
                    case "Problem":
                        PAMTicketEntitiesDeltaPushByType(ticketsWithTAT, CONSTANTS.DB_ProblemCollection, ClientUId, EndToEndUId);
                        break;
                    case "Service_Request":
                        PAMTicketEntitiesDeltaPushByType(ticketsWithTAT, CONSTANTS.DB_ServiceRequestCollection, ClientUId, EndToEndUId);
                        break;
                    case "Change_Management":
                        PAMTicketEntitiesDeltaPushByType(ticketsWithTAT, CONSTANTS.DB_ChangeManagementCollection, ClientUId, EndToEndUId);
                        break;
                    case "AOTask":
                        List<PAMTask> tasklist = ConvertTicketToTask(ticketList);
                        PAMTaskEntitiesDeltaPushByType(tasklist, CONSTANTS.DB_AOTaskCollection, ClientUId, EndToEndUId);
                        break;
                    case "IOTask":
                        List<PAMTask> tasklist1 = ConvertTicketToTask(ticketList);
                        PAMTaskEntitiesDeltaPushByType(tasklist1, CONSTANTS.DB_IOTaskCollection, ClientUId, EndToEndUId);
                        break;
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(PAMTicketPullService), nameof(PAMTicketEntitiesDeltaPush), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                throw ex;
            }
        }

        public List<PAMTicket> CalculateTAT(List<PAMTicket> ticketsData)
        {            
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(PAMTicketPullService), nameof(CalculateTAT), "START", Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
            #region //Config Settings
            //NameValueCollection _configuration = ConfigurationManager.GetSection("PAM.Configuration") as NameValueCollection;
            int workingDays = Convert.ToInt32(appSettings.NO_SUPPORT_DAYS);
            string shiftStartTime = appSettings.SHIFT_START_TIME;
            string shiftEndTime = appSettings.SHIFT_END_TIME;
            #endregion
            #region Variables
            TimeSpan startTime = TimeSpan.Parse(shiftStartTime);
            TimeSpan endTime = TimeSpan.Parse(shiftEndTime);
            int actualWorkingDays = 0;
            int WeekendDays = 0;
            double workingHours = 0.0;
            double result1 = 0.0;
            double result2 = 0.0;
            bool defaultResolutionScenraio = true;
            bool defaultResponseScenraio = true;
            List<PAMTicket> TicketsDataWithTAT = new List<PAMTicket>();
            List<PAMTask> TaskDataWithTAT = new List<PAMTask>();
            #endregion
            try
            {
                //Calculating Working hours
                //Case 1 : When SHift Start time is < End time - no change
                //Case 2 : When Shift Start time is > End time - 24-(End -Start)
                if (startTime < endTime)
                {
                    workingHours = endTime.Subtract(startTime).TotalHours;
                }
                else
                {
                    workingHours = 24 - Math.Abs((endTime.Subtract(startTime).TotalHours));
                }
                foreach (PAMTicket data in ticketsData)
                {
                    defaultResolutionScenraio = true;
                    defaultResponseScenraio = true;

                    #region TAT Resolution Scenraio's
                    if (data.LastResolvedDate != null && (data.LastResolvedDate != DateTime.MinValue))// if resolve date is not available then do not calulate the data.TATResolution
                    {
                        if (endTime > startTime)
                        {
                            #region Scenario 0 
                            //If LastResolvedDate is less than reportedDateTime then TAT negative values are stored as 0 
                            if (data.LastResolvedDate < data.ReportedDateTime)
                            {
                                data.TATResolution = 0M;
                                defaultResolutionScenraio = false;
                            }
                            #endregion

                            #region Scenario 1
                            if ((workingDays == 5) && (data.ReportedDateTime.DayOfWeek != DayOfWeek.Sunday && data.ReportedDateTime.DayOfWeek != DayOfWeek.Saturday)
                                && ((data.ReportedDateTime.TimeOfDay >= startTime) && (data.ReportedDateTime.TimeOfDay <= endTime))
                                && (data.LastResolvedDate.DayOfWeek != DayOfWeek.Saturday && data.LastResolvedDate.DayOfWeek != DayOfWeek.Sunday)
                                )
                            {
                                #region Next day within SW
                                if (data.ReportedDateTime.DayOfWeek == DayOfWeek.Friday && (data.LastResolvedDate.TimeOfDay >= startTime))
                                {
                                    if (data.LastResolvedDate.Date == data.ReportedDateTime.AddDays(3).Date)
                                    {
                                        if (data.LastResolvedDate.ToString() != null && data.LastResolvedDate.ToString() != "")
                                        {
                                            actualWorkingDays = ActualDays(data.ReportedDateTime, data.LastResolvedDate, workingDays);
                                            result1 = endTime.Subtract(data.ReportedDateTime.TimeOfDay).TotalHours;
                                            result2 = data.LastResolvedDate.TimeOfDay.Subtract(startTime).TotalHours;
                                            data.TATResolution = Math.Round(Convert.ToDecimal(result1 + actualWorkingDays * workingHours + result2), 2, MidpointRounding.AwayFromZero);
                                            defaultResolutionScenraio = false;
                                        }
                                    }
                                }
                                else if (data.LastResolvedDate.Date == data.ReportedDateTime.AddDays(1).Date && (data.LastResolvedDate.TimeOfDay >= startTime))
                                {
                                    if (data.LastResolvedDate.ToString() != null && data.LastResolvedDate.ToString() != "")
                                    {
                                        actualWorkingDays = ActualDays(data.ReportedDateTime, data.LastResolvedDate, workingDays);
                                        result1 = endTime.Subtract(data.ReportedDateTime.TimeOfDay).TotalHours;
                                        result2 = data.LastResolvedDate.TimeOfDay.Subtract(startTime).TotalHours;
                                        data.TATResolution = Math.Round(Convert.ToDecimal(result1 + actualWorkingDays * workingHours + result2), 2, MidpointRounding.AwayFromZero);
                                        defaultResolutionScenraio = false;
                                    }

                                }

                                #endregion

                                #region Resolved on some other day within SW

                                if (data.ReportedDateTime.DayOfWeek == DayOfWeek.Friday && (data.LastResolvedDate.TimeOfDay >= startTime))
                                {
                                    if (data.LastResolvedDate.Date > data.ReportedDateTime.AddDays(3).Date)
                                    {
                                        if (data.LastResolvedDate.ToString() != null && data.LastResolvedDate.ToString() != "")
                                        {
                                            actualWorkingDays = ActualDays(data.ReportedDateTime, data.LastResolvedDate, workingDays);
                                            result1 = endTime.Subtract(data.ReportedDateTime.TimeOfDay).TotalHours;
                                            result2 = data.LastResolvedDate.TimeOfDay.Subtract(startTime).TotalHours;
                                            data.TATResolution = Math.Round(Convert.ToDecimal(result1 + actualWorkingDays * workingHours + result2), 2, MidpointRounding.AwayFromZero);
                                            defaultResolutionScenraio = false;
                                        }
                                    }
                                }
                                else
                                {
                                    if (data.LastResolvedDate.Date > data.ReportedDateTime.AddDays(1).Date && (data.LastResolvedDate.TimeOfDay >= startTime))
                                    {
                                        if (data.LastResolvedDate.ToString() != null && data.LastResolvedDate.ToString() != "")
                                        {
                                            actualWorkingDays = ActualDays(data.ReportedDateTime, data.LastResolvedDate, workingDays);
                                            result1 = endTime.Subtract(data.ReportedDateTime.TimeOfDay).TotalHours;
                                            result2 = data.LastResolvedDate.TimeOfDay.Subtract(startTime).TotalHours;
                                            data.TATResolution = Math.Round(Convert.ToDecimal(result1 + actualWorkingDays * workingHours + result2), 2, MidpointRounding.AwayFromZero);
                                            defaultResolutionScenraio = false;
                                        }
                                    }
                                }

                                #endregion
                            }
                            else if ((workingDays == 6) && (data.ReportedDateTime.DayOfWeek != DayOfWeek.Sunday)
                                && ((data.ReportedDateTime.TimeOfDay >= startTime) && (data.ReportedDateTime.TimeOfDay <= endTime))
                                && (data.LastResolvedDate.DayOfWeek != DayOfWeek.Sunday)
                                )
                            {
                                #region Next day within SW
                                if (data.ReportedDateTime.DayOfWeek == DayOfWeek.Saturday && (data.LastResolvedDate.TimeOfDay >= startTime))
                                {
                                    if (data.LastResolvedDate.Date == data.ReportedDateTime.AddDays(2).Date)
                                    {
                                        if (data.LastResolvedDate.ToString() != null && data.LastResolvedDate.ToString() != "")
                                        {
                                            actualWorkingDays = ActualDays(data.ReportedDateTime, data.LastResolvedDate, workingDays);
                                            result1 = endTime.Subtract(data.ReportedDateTime.TimeOfDay).TotalHours;
                                            result2 = data.LastResolvedDate.TimeOfDay.Subtract(startTime).TotalHours;
                                            data.TATResolution = Math.Round(Convert.ToDecimal(result1 + actualWorkingDays * workingHours + result2), 2, MidpointRounding.AwayFromZero);
                                            defaultResolutionScenraio = false;
                                        }
                                    }
                                }
                                else
                                {
                                    if (data.LastResolvedDate.Date == data.ReportedDateTime.AddDays(1).Date && (data.LastResolvedDate.TimeOfDay >= startTime))
                                    {
                                        if (data.LastResolvedDate.ToString() != null && data.LastResolvedDate.ToString() != "")
                                        {
                                            actualWorkingDays = ActualDays(data.ReportedDateTime, data.LastResolvedDate, workingDays);
                                            result1 = endTime.Subtract(data.ReportedDateTime.TimeOfDay).TotalHours;
                                            result2 = data.LastResolvedDate.TimeOfDay.Subtract(startTime).TotalHours;
                                            data.TATResolution = Math.Round(Convert.ToDecimal(result1 + actualWorkingDays * workingHours + result2), 2, MidpointRounding.AwayFromZero);
                                            defaultResolutionScenraio = false;
                                        }
                                    }
                                }

                                #endregion

                                #region Resolved on some other day within SW

                                if (data.ReportedDateTime.DayOfWeek == DayOfWeek.Saturday && (data.LastResolvedDate.TimeOfDay >= startTime))
                                {
                                    if (data.LastResolvedDate.Date > data.ReportedDateTime.AddDays(2).Date)
                                    {
                                        if (data.LastResolvedDate.ToString() != null && data.LastResolvedDate.ToString() != "")
                                        {
                                            actualWorkingDays = ActualDays(data.ReportedDateTime, data.LastResolvedDate, workingDays);
                                            result1 = endTime.Subtract(data.ReportedDateTime.TimeOfDay).TotalHours;
                                            result2 = data.LastResolvedDate.TimeOfDay.Subtract(startTime).TotalHours;
                                            data.TATResolution = Math.Round(Convert.ToDecimal(result1 + actualWorkingDays * workingHours + result2), 2, MidpointRounding.AwayFromZero);
                                            defaultResolutionScenraio = false;
                                        }
                                    }
                                }
                                else
                                {
                                    if (data.LastResolvedDate.Date > data.ReportedDateTime.AddDays(1).Date && (data.LastResolvedDate.TimeOfDay >= startTime))
                                    {
                                        if (data.LastResolvedDate.ToString() != null && data.LastResolvedDate.ToString() != "")
                                        {
                                            actualWorkingDays = ActualDays(data.ReportedDateTime, data.LastResolvedDate, workingDays);
                                            result1 = endTime.Subtract(data.ReportedDateTime.TimeOfDay).TotalHours;
                                            result2 = data.LastResolvedDate.TimeOfDay.Subtract(startTime).TotalHours;
                                            data.TATResolution = Math.Round(Convert.ToDecimal(result1 + actualWorkingDays * workingHours + result2), 2, MidpointRounding.AwayFromZero);
                                            defaultResolutionScenraio = false;
                                        }
                                    }
                                }

                                #endregion

                            }
                            else if ((workingDays == 7) && ((data.ReportedDateTime.TimeOfDay >= (startTime) && (data.ReportedDateTime.TimeOfDay <= endTime))
                                ))
                            {
                                #region Next day within SW

                                if (data.LastResolvedDate.Date == data.ReportedDateTime.AddDays(1).Date && data.LastResolvedDate.TimeOfDay >= startTime)
                                {
                                    if (data.LastResolvedDate.ToString() != null && data.LastResolvedDate.ToString() != "")
                                    {
                                        actualWorkingDays = ActualDays(data.ReportedDateTime, data.LastResolvedDate, workingDays);
                                        result1 = endTime.Subtract(data.ReportedDateTime.TimeOfDay).TotalHours;
                                        result2 = data.LastResolvedDate.TimeOfDay.Subtract(startTime).TotalHours;
                                        data.TATResolution = Math.Round(Convert.ToDecimal(result1 + actualWorkingDays * workingHours + result2), 2, MidpointRounding.AwayFromZero);
                                        defaultResolutionScenraio = false;
                                    }

                                }

                                #endregion

                                #region Resolved on some other day within SW

                                if (data.LastResolvedDate.Date > data.ReportedDateTime.AddDays(1).Date && (data.LastResolvedDate.TimeOfDay >= startTime))
                                {
                                    if (data.LastResolvedDate.ToString() != null && data.LastResolvedDate.ToString() != "")
                                    {
                                        actualWorkingDays = ActualDays(data.ReportedDateTime, data.LastResolvedDate, workingDays);
                                        result1 = endTime.Subtract(data.ReportedDateTime.TimeOfDay).TotalHours;
                                        result2 = data.LastResolvedDate.TimeOfDay.Subtract(TimeSpan.Parse((shiftStartTime))).TotalHours;
                                        data.TATResolution = Math.Round(Convert.ToDecimal(result1 + actualWorkingDays * workingHours + result2), 2, MidpointRounding.AwayFromZero);
                                        defaultResolutionScenraio = false;
                                    }
                                }

                                #endregion

                            }

                            #endregion Scenraio 1

                            #region Scenario 2 and 9 are same except end time
                            //Scenario 2 : If an Incident is reported in Service Window on Support Days and Resolved on same day
                            //TAT = Last Resolved Date - Reported Date
                            if ((workingDays == 5) && (data.ReportedDateTime.DayOfWeek != DayOfWeek.Sunday && data.ReportedDateTime.DayOfWeek != DayOfWeek.Saturday)
                            && (data.LastResolvedDate.Date == data.ReportedDateTime.Date) && ((data.ReportedDateTime.TimeOfDay >= startTime) && (data.ReportedDateTime.TimeOfDay <= endTime))
                                && (data.LastResolvedDate.TimeOfDay >= startTime))
                            {
                                if (data.LastResolvedDate.ToString() != null && data.LastResolvedDate.ToString() != "")
                                {
                                    data.TATResolution = Math.Round(Convert.ToDecimal(data.LastResolvedDate.TimeOfDay.Subtract(data.ReportedDateTime.TimeOfDay).TotalHours), 2, MidpointRounding.AwayFromZero);
                                    defaultResolutionScenraio = false;
                                }
                            }
                            else if ((workingDays == 6) && (data.ReportedDateTime.DayOfWeek != DayOfWeek.Sunday)
                                && (data.LastResolvedDate.Date == data.ReportedDateTime.Date) && ((data.ReportedDateTime.TimeOfDay >= startTime && (data.ReportedDateTime.TimeOfDay <= endTime))
                                && (data.LastResolvedDate.TimeOfDay >= startTime)))
                            {
                                if (data.LastResolvedDate.ToString() != null && data.LastResolvedDate.ToString() != "")
                                {
                                    data.TATResolution = Math.Round(Convert.ToDecimal(data.LastResolvedDate.TimeOfDay.Subtract(data.ReportedDateTime.TimeOfDay).TotalHours), 2, MidpointRounding.AwayFromZero);
                                    defaultResolutionScenraio = false;
                                }
                            }
                            else if ((workingDays == 7) && (data.LastResolvedDate.Date == data.ReportedDateTime.Date) &&
                                ((data.ReportedDateTime.TimeOfDay >= startTime && (data.ReportedDateTime.TimeOfDay <= endTime))
                                && (data.LastResolvedDate.TimeOfDay >= startTime)))
                            {
                                if (data.LastResolvedDate.ToString() != null && data.LastResolvedDate.ToString() != "")
                                {
                                    data.TATResolution = Math.Round(Convert.ToDecimal(data.LastResolvedDate.TimeOfDay.Subtract(data.ReportedDateTime.TimeOfDay).TotalHours), 2, MidpointRounding.AwayFromZero);
                                    defaultResolutionScenraio = false;
                                }
                            }
                            #endregion Scenraio2

                            #region Scenario 3
                            //Scenario 3 : If an Incident is reported non-Support Hour on Support Days (before Shift Start Time) and Resolved on same day
                            //TAT = Resolved Time - Shift Start Time
                            if ((workingDays == 5) && (data.ReportedDateTime.DayOfWeek != DayOfWeek.Sunday && data.ReportedDateTime.DayOfWeek != DayOfWeek.Saturday)
                                && (data.LastResolvedDate.DayOfWeek != DayOfWeek.Sunday && data.LastResolvedDate.DayOfWeek != DayOfWeek.Saturday)
                                && (data.LastResolvedDate.Date == data.ReportedDateTime.Date) && (data.ReportedDateTime.TimeOfDay < startTime)
                                )
                            {
                                if (data.LastResolvedDate.TimeOfDay >= startTime)
                                {
                                    if (data.LastResolvedDate.ToString() != null && data.LastResolvedDate.ToString() != "")
                                    {
                                        data.TATResolution = Math.Round(Convert.ToDecimal(data.LastResolvedDate.TimeOfDay.Subtract(startTime).TotalHours), 2, MidpointRounding.AwayFromZero);
                                        defaultResolutionScenraio = false;
                                    }

                                }
                            }
                            else if ((workingDays == 6) && (data.ReportedDateTime.DayOfWeek != DayOfWeek.Sunday)
                                && (data.LastResolvedDate.DayOfWeek != DayOfWeek.Sunday)
                                && (data.LastResolvedDate.Date == data.ReportedDateTime.Date) && ((data.ReportedDateTime.TimeOfDay < startTime))
                                )
                            {
                                if (data.LastResolvedDate.TimeOfDay >= startTime)
                                {
                                    if (data.LastResolvedDate.ToString() != null && data.LastResolvedDate.ToString() != "")
                                    {
                                        data.TATResolution = Math.Round(Convert.ToDecimal(data.LastResolvedDate.TimeOfDay.Subtract(startTime).TotalHours), 2, MidpointRounding.AwayFromZero);
                                        defaultResolutionScenraio = false;
                                    }

                                }
                            }
                            else if ((workingDays == 7) && (data.LastResolvedDate.Date == data.ReportedDateTime.Date)
                                && ((data.ReportedDateTime.TimeOfDay < startTime))
                                )
                            {
                                if (data.LastResolvedDate.TimeOfDay >= startTime)
                                {
                                    if (data.LastResolvedDate.ToString() != null && data.LastResolvedDate.ToString() != "")
                                    {
                                        data.TATResolution = Math.Round(Convert.ToDecimal(data.LastResolvedDate.TimeOfDay.Subtract(startTime).TotalHours), 2, MidpointRounding.AwayFromZero);
                                        defaultResolutionScenraio = false;
                                    }

                                }

                            }
                            #endregion

                            #region Scenario 4
                            //If an Incident is reported non-Support Hour on Support Days (before Shift Start Time) and Resolved on some other day
                            //TAT = Active duration on Reported day + Duration in other Support Days + Active Duration on Reolved Day
                            //TAT = Service Window + X * Service Window + (Resolved Time - Shift Start Time)
                            if ((workingDays == 5) && (data.ReportedDateTime.DayOfWeek != DayOfWeek.Sunday && data.ReportedDateTime.DayOfWeek != DayOfWeek.Saturday)
                                && (data.LastResolvedDate.DayOfWeek != DayOfWeek.Sunday && data.LastResolvedDate.DayOfWeek != DayOfWeek.Saturday)
                                && (data.ReportedDateTime.TimeOfDay < startTime)
                               )
                            {
                                if (data.ReportedDateTime.DayOfWeek == DayOfWeek.Friday)
                                {
                                    if (data.LastResolvedDate.Date >= data.ReportedDateTime.AddDays(3).Date && (data.LastResolvedDate.TimeOfDay >= startTime))
                                    {
                                        if (data.LastResolvedDate.ToString() != null && data.LastResolvedDate.ToString() != "")
                                        {
                                            actualWorkingDays = ActualDays(data.ReportedDateTime, data.LastResolvedDate, workingDays);
                                            result2 = data.LastResolvedDate.TimeOfDay.Subtract(startTime).TotalHours;
                                            data.TATResolution = Math.Round(Convert.ToDecimal(workingHours + actualWorkingDays * workingHours + result2), 2, MidpointRounding.AwayFromZero);
                                            defaultResolutionScenraio = false;
                                        }
                                    }
                                }
                                if (data.ReportedDateTime.DayOfWeek != DayOfWeek.Friday)
                                {
                                    if (data.LastResolvedDate.Date >= data.ReportedDateTime.AddDays(1).Date && (data.LastResolvedDate.TimeOfDay >= startTime))
                                    {
                                        if (data.LastResolvedDate.ToString() != null && data.LastResolvedDate.ToString() != "")
                                        {
                                            actualWorkingDays = ActualDays(data.ReportedDateTime, data.LastResolvedDate, workingDays);
                                            result2 = data.LastResolvedDate.TimeOfDay.Subtract(startTime).TotalHours;
                                            data.TATResolution = Math.Round(Convert.ToDecimal(workingHours + actualWorkingDays * workingHours + result2), 2, MidpointRounding.AwayFromZero);
                                            defaultResolutionScenraio = false;
                                        }
                                    }
                                }
                            }
                            else if ((workingDays == 6) && (data.ReportedDateTime.DayOfWeek != DayOfWeek.Sunday)
                                && (data.LastResolvedDate.DayOfWeek != DayOfWeek.Sunday)
                                && (data.ReportedDateTime.TimeOfDay < startTime)
                                )
                            {
                                if (data.ReportedDateTime.DayOfWeek == DayOfWeek.Saturday)
                                {
                                    if (data.LastResolvedDate.Date >= data.ReportedDateTime.AddDays(2).Date && (data.LastResolvedDate.TimeOfDay >= startTime))
                                    {
                                        if (data.LastResolvedDate.ToString() != null && data.LastResolvedDate.ToString() != "")
                                        {
                                            actualWorkingDays = ActualDays(data.ReportedDateTime, data.LastResolvedDate, workingDays);
                                            result2 = data.LastResolvedDate.TimeOfDay.Subtract(startTime).TotalHours;
                                            data.TATResolution = Math.Round(Convert.ToDecimal(workingHours + actualWorkingDays * workingHours + result2), 2, MidpointRounding.AwayFromZero);
                                            defaultResolutionScenraio = false;
                                        }
                                    }
                                }
                                if (data.ReportedDateTime.DayOfWeek != DayOfWeek.Saturday)
                                {
                                    if (data.LastResolvedDate.Date >= data.ReportedDateTime.AddDays(1).Date && (data.LastResolvedDate.TimeOfDay >= startTime))
                                    {
                                        if (data.LastResolvedDate.ToString() != null && data.LastResolvedDate.ToString() != "")
                                        {
                                            actualWorkingDays = ActualDays(data.ReportedDateTime, data.LastResolvedDate, workingDays);
                                            result2 = data.LastResolvedDate.TimeOfDay.Subtract(startTime).TotalHours;
                                            data.TATResolution = Math.Round(Convert.ToDecimal(workingHours + actualWorkingDays * workingHours + result2), 2, MidpointRounding.AwayFromZero);
                                            defaultResolutionScenraio = false;
                                        }
                                    }
                                }
                            }
                            else if ((workingDays == 7) && (data.LastResolvedDate.Date >= data.ReportedDateTime.Date)
                                && (data.ReportedDateTime.TimeOfDay < startTime)
                                && (data.ReportedDateTime.TimeOfDay < startTime))
                            {
                                if (data.LastResolvedDate.Date >= data.ReportedDateTime.AddDays(1).Date && (data.LastResolvedDate.TimeOfDay >= startTime))
                                {
                                    if (data.LastResolvedDate.ToString() != null && data.LastResolvedDate.ToString() != "")
                                    {
                                        actualWorkingDays = ActualDays(data.ReportedDateTime, data.LastResolvedDate, workingDays);
                                        result2 = data.LastResolvedDate.TimeOfDay.Subtract(startTime).TotalHours;
                                        data.TATResolution = Math.Round(Convert.ToDecimal(workingHours + actualWorkingDays * workingHours + result2), 2, MidpointRounding.AwayFromZero);
                                        defaultResolutionScenraio = false;
                                    }
                                }

                            }
                            #endregion

                            #region Scenario 5
                            //If an Incident is reported non-Support Hour on Support Days (after Shift End Time) and Resolved on next day
                            //TAT = Resolved Time - 'Shift Start Time' of next day

                            if ((workingDays == 5) && (data.ReportedDateTime.DayOfWeek != DayOfWeek.Sunday && data.ReportedDateTime.DayOfWeek != DayOfWeek.Saturday)
                            && (data.ReportedDateTime.TimeOfDay > endTime) && (data.LastResolvedDate.DayOfWeek != DayOfWeek.Sunday && data.LastResolvedDate.DayOfWeek != DayOfWeek.Saturday)
                            && ((data.LastResolvedDate.TimeOfDay >= startTime)))
                            {
                                if (data.ReportedDateTime.DayOfWeek == DayOfWeek.Friday)
                                {
                                    if (data.LastResolvedDate.Date == data.ReportedDateTime.AddDays(3).Date)
                                    {
                                        if (data.LastResolvedDate.ToString() != null && data.LastResolvedDate.ToString() != "")
                                        {
                                            data.TATResolution = Math.Round(Convert.ToDecimal(data.LastResolvedDate.TimeOfDay.Subtract(startTime).TotalHours), 2, MidpointRounding.AwayFromZero);
                                            defaultResolutionScenraio = false;
                                        }
                                    }
                                }
                                else
                                {
                                    if (data.LastResolvedDate.Date == data.ReportedDateTime.AddDays(1).Date)
                                    {
                                        if (data.LastResolvedDate.ToString() != null && data.LastResolvedDate.ToString() != "")
                                        {
                                            data.TATResolution = Math.Round(Convert.ToDecimal(data.LastResolvedDate.TimeOfDay.Subtract(startTime).TotalHours), 2, MidpointRounding.AwayFromZero);
                                            defaultResolutionScenraio = false;
                                        }
                                    }
                                }
                            }

                            else if ((workingDays == 6) && (data.ReportedDateTime.DayOfWeek != DayOfWeek.Sunday)
                             && (data.LastResolvedDate.DayOfWeek != DayOfWeek.Sunday)
                             && ((data.ReportedDateTime.TimeOfDay > endTime))
                             && ((data.LastResolvedDate.TimeOfDay >= startTime)))
                            {
                                if (data.ReportedDateTime.DayOfWeek == DayOfWeek.Saturday)
                                {
                                    if (data.LastResolvedDate.Date == data.ReportedDateTime.AddDays(2).Date)
                                    {
                                        if (data.LastResolvedDate.ToString() != null && data.LastResolvedDate.ToString() != "")
                                        {
                                            data.TATResolution = Math.Round(Convert.ToDecimal(data.LastResolvedDate.TimeOfDay.Subtract(startTime).TotalHours), 2, MidpointRounding.AwayFromZero);
                                            defaultResolutionScenraio = false;
                                        }

                                    }
                                }
                                else
                                {
                                    if (data.LastResolvedDate.Date == data.ReportedDateTime.AddDays(1).Date)
                                    {
                                        if (data.LastResolvedDate.ToString() != null && data.LastResolvedDate.ToString() != "")
                                        {
                                            data.TATResolution = Math.Round(Convert.ToDecimal(data.LastResolvedDate.TimeOfDay.Subtract(startTime).TotalHours), 2, MidpointRounding.AwayFromZero);
                                        }
                                        defaultResolutionScenraio = false;
                                    }
                                }
                            }

                            else if ((workingDays == 7) && (data.ReportedDateTime.TimeOfDay > endTime) && (data.LastResolvedDate.TimeOfDay >= startTime))
                            {
                                if (data.LastResolvedDate.Date == data.ReportedDateTime.AddDays(1).Date)
                                {
                                    if (data.LastResolvedDate.ToString() != null && data.LastResolvedDate.ToString() != "")
                                    {
                                        data.TATResolution = Math.Round(Convert.ToDecimal(data.LastResolvedDate.TimeOfDay.Subtract(startTime).TotalHours), 2, MidpointRounding.AwayFromZero);
                                        defaultResolutionScenraio = false;
                                    }
                                }
                            }
                            #endregion

                            #region Scenario 6
                            //Scenario 6 : If an Incident is reported non-Support Hour on Support Days (after Shift End Time) and Resolved on some otherday
                            //TAT =  Duration in other Support Days + Active Duration on Resolved Day
                            //TAT = X * Service Window + (Resolved Time - Shift Start Time)

                            if ((workingDays == 5) && (data.ReportedDateTime.DayOfWeek != DayOfWeek.Sunday && data.ReportedDateTime.DayOfWeek != DayOfWeek.Saturday)
                            && ((data.ReportedDateTime.TimeOfDay > endTime))
                            && (data.LastResolvedDate.DayOfWeek != DayOfWeek.Saturday && data.LastResolvedDate.DayOfWeek != DayOfWeek.Sunday)
                            && (data.LastResolvedDate.TimeOfDay >= startTime))
                            {
                                if (data.ReportedDateTime.DayOfWeek == DayOfWeek.Friday)
                                {
                                    if (data.LastResolvedDate.Date > data.ReportedDateTime.AddDays(3).Date)
                                    {
                                        if (data.LastResolvedDate.ToString() != null && data.LastResolvedDate.ToString() != "")
                                        {
                                            actualWorkingDays = ActualDays(data.ReportedDateTime, data.LastResolvedDate, workingDays);
                                            result2 = data.LastResolvedDate.TimeOfDay.Subtract(startTime).TotalHours;
                                            data.TATResolution = Math.Round(Convert.ToDecimal(actualWorkingDays * workingHours + result2), 2, MidpointRounding.AwayFromZero);
                                            defaultResolutionScenraio = false;
                                        }

                                    }
                                }
                                else
                                {
                                    if (data.LastResolvedDate.Date > data.ReportedDateTime.AddDays(1).Date)
                                    {
                                        if (data.LastResolvedDate.ToString() != null && data.LastResolvedDate.ToString() != "")
                                        {
                                            actualWorkingDays = ActualDays(data.ReportedDateTime, data.LastResolvedDate, workingDays);
                                            result2 = data.LastResolvedDate.TimeOfDay.Subtract(startTime).TotalHours;
                                            data.TATResolution = Math.Round(Convert.ToDecimal(actualWorkingDays * workingHours + result2), 2, MidpointRounding.AwayFromZero);
                                            defaultResolutionScenraio = false;
                                        }

                                    }
                                }
                            }
                            else if ((workingDays == 6) && (data.ReportedDateTime.DayOfWeek != DayOfWeek.Sunday)
                                && ((data.ReportedDateTime.TimeOfDay > endTime))
                                && (data.LastResolvedDate.DayOfWeek != DayOfWeek.Sunday)
                                && (data.LastResolvedDate.TimeOfDay >= startTime))
                            {
                                if (data.ReportedDateTime.DayOfWeek == DayOfWeek.Saturday)
                                {
                                    if (data.LastResolvedDate.Date > data.ReportedDateTime.AddDays(2).Date)
                                    {
                                        if (data.LastResolvedDate.ToString() != null && data.LastResolvedDate.ToString() != "")
                                        {
                                            actualWorkingDays = ActualDays(data.ReportedDateTime, data.LastResolvedDate, workingDays);
                                            result2 = data.LastResolvedDate.TimeOfDay.Subtract(startTime).TotalHours;
                                            data.TATResolution = Math.Round(Convert.ToDecimal(actualWorkingDays * workingHours + result2), 2, MidpointRounding.AwayFromZero);
                                            defaultResolutionScenraio = false;
                                        }

                                    }
                                }
                                else
                                {
                                    if (data.LastResolvedDate.Date > data.ReportedDateTime.AddDays(1).Date)
                                    {
                                        if (data.LastResolvedDate.ToString() != null && data.LastResolvedDate.ToString() != "")
                                        {
                                            actualWorkingDays = ActualDays(data.ReportedDateTime, data.LastResolvedDate, workingDays);
                                            result2 = data.LastResolvedDate.TimeOfDay.Subtract(startTime).TotalHours;
                                            data.TATResolution = Math.Round(Convert.ToDecimal(actualWorkingDays * workingHours + result2), 2, MidpointRounding.AwayFromZero);
                                            defaultResolutionScenraio = false;
                                        }

                                    }
                                }
                            }
                            else if ((workingDays == 7) && ((data.ReportedDateTime.TimeOfDay > endTime))
                                && (data.LastResolvedDate.TimeOfDay >= startTime))
                            {
                                if (data.LastResolvedDate.Date > data.ReportedDateTime.AddDays(1).Date)
                                {
                                    if (data.LastResolvedDate.ToString() != null && data.LastResolvedDate.ToString() != "")
                                    {
                                        actualWorkingDays = ActualDays(data.ReportedDateTime, data.LastResolvedDate, workingDays);
                                        result2 = data.LastResolvedDate.TimeOfDay.Subtract(startTime).TotalHours;
                                        data.TATResolution = Math.Round(Convert.ToDecimal(actualWorkingDays * workingHours + result2), 2, MidpointRounding.AwayFromZero);
                                        defaultResolutionScenraio = false;
                                    }

                                }
                            }
                            #endregion

                            #region Scenario 7
                            //Scenario 7 : If an Incident is raised during non-Support Days and Resolved on next Support Day
                            //TAT = Resolve Time - 'Shift Start Time' of next Support Day

                            if ((workingDays == 5) && (data.ReportedDateTime.DayOfWeek == DayOfWeek.Sunday || data.ReportedDateTime.DayOfWeek == DayOfWeek.Saturday)
                                && (data.LastResolvedDate.TimeOfDay >= startTime)
                                && (data.LastResolvedDate.DayOfWeek != DayOfWeek.Saturday && data.LastResolvedDate.DayOfWeek != DayOfWeek.Sunday)
                                )
                            {
                                if (data.ReportedDateTime.DayOfWeek == DayOfWeek.Saturday)
                                {
                                    if (data.LastResolvedDate.Date == data.ReportedDateTime.AddDays(2).Date)
                                    {
                                        if (data.LastResolvedDate.ToString() != null && data.LastResolvedDate.ToString() != "")
                                        {
                                            data.TATResolution = Math.Round(Convert.ToDecimal(data.LastResolvedDate.TimeOfDay.Subtract(startTime).TotalHours), 2, MidpointRounding.AwayFromZero);
                                            defaultResolutionScenraio = false;
                                        }

                                    }
                                }
                                else if (data.ReportedDateTime.DayOfWeek == DayOfWeek.Sunday)
                                {
                                    if (data.LastResolvedDate.Date == data.ReportedDateTime.AddDays(1).Date)
                                    {
                                        if (data.LastResolvedDate.ToString() != null && data.LastResolvedDate.ToString() != "")
                                        {
                                            data.TATResolution = Math.Round(Convert.ToDecimal(data.LastResolvedDate.TimeOfDay.Subtract(startTime).TotalHours), 2, MidpointRounding.AwayFromZero);
                                            defaultResolutionScenraio = false;
                                        }

                                    }
                                }
                            }
                            else if ((workingDays == 6) && (data.ReportedDateTime.DayOfWeek == DayOfWeek.Sunday) && (data.LastResolvedDate.DayOfWeek != DayOfWeek.Sunday)
                                && (data.LastResolvedDate.TimeOfDay >= startTime))
                            {
                                if (data.ReportedDateTime.DayOfWeek == DayOfWeek.Sunday)
                                {
                                    if (data.LastResolvedDate.Date == data.ReportedDateTime.AddDays(1).Date)
                                    {
                                        if (data.LastResolvedDate.ToString() != null && data.LastResolvedDate.ToString() != "")
                                        {
                                            data.TATResolution = Math.Round(Convert.ToDecimal(data.LastResolvedDate.TimeOfDay.Subtract(startTime).TotalHours), 2, MidpointRounding.AwayFromZero);
                                            defaultResolutionScenraio = false;
                                        }
                                    }
                                }
                            }
                            #endregion

                            #region Scenario 8
                            //Scenario 8 : If an Incident is raised during non-Support Days and Resolved on other Support Day
                            //Find number of Support Days between Last Resolved Date and Reported Days (Exclude Resolved Day)
                            //TAT = Active duration in other Support Days + Active Duration on Reolved Day
                            //TAT = X * Service Window + (Resolved Time - Shift Start Time)
                            if ((workingDays == 5) && (data.ReportedDateTime.DayOfWeek == DayOfWeek.Sunday || data.ReportedDateTime.DayOfWeek == DayOfWeek.Saturday)
                               && (data.LastResolvedDate.TimeOfDay >= startTime)
                               && (data.LastResolvedDate.DayOfWeek != DayOfWeek.Saturday && data.LastResolvedDate.DayOfWeek != DayOfWeek.Sunday)
                               )
                            {
                                if (data.ReportedDateTime.DayOfWeek == DayOfWeek.Saturday)
                                {
                                    if (data.LastResolvedDate.Date > data.ReportedDateTime.AddDays(2).Date)
                                    {

                                        if (data.LastResolvedDate.ToString() != null && data.LastResolvedDate.ToString() != "")
                                        {
                                            actualWorkingDays = ActualDays(data.ReportedDateTime, data.LastResolvedDate, workingDays);
                                            result2 = data.LastResolvedDate.TimeOfDay.Subtract(startTime).TotalHours;
                                            data.TATResolution = Math.Round(Convert.ToDecimal(actualWorkingDays * workingHours + result2), 2, MidpointRounding.AwayFromZero);
                                            defaultResolutionScenraio = false;
                                        }
                                    }
                                }
                                else if (data.ReportedDateTime.DayOfWeek == DayOfWeek.Sunday)
                                {
                                    if (data.LastResolvedDate.Date > data.ReportedDateTime.AddDays(1).Date)
                                    {
                                        if (data.LastResolvedDate.ToString() != null && data.LastResolvedDate.ToString() != "")
                                        {
                                            actualWorkingDays = ActualDays(data.ReportedDateTime, data.LastResolvedDate, workingDays);
                                            result2 = data.LastResolvedDate.TimeOfDay.Subtract(startTime).TotalHours;
                                            data.TATResolution = Math.Round(Convert.ToDecimal(actualWorkingDays * workingHours + result2), 2, MidpointRounding.AwayFromZero);
                                            defaultResolutionScenraio = false;
                                        }

                                    }
                                }
                            }
                            else if ((workingDays == 6) && (data.ReportedDateTime.DayOfWeek == DayOfWeek.Sunday) && (data.LastResolvedDate.DayOfWeek != DayOfWeek.Sunday)
                                && (data.LastResolvedDate.TimeOfDay >= startTime))
                            {
                                if (data.ReportedDateTime.DayOfWeek == DayOfWeek.Sunday)
                                {
                                    if (data.LastResolvedDate.Date > data.ReportedDateTime.AddDays(1).Date)
                                    {
                                        if (data.LastResolvedDate.ToString() != null && data.LastResolvedDate.ToString() != "")
                                        {
                                            actualWorkingDays = ActualDays(data.ReportedDateTime, data.LastResolvedDate, workingDays);
                                            result2 = data.LastResolvedDate.TimeOfDay.Subtract(startTime).TotalHours;
                                            data.TATResolution = Math.Round(Convert.ToDecimal(actualWorkingDays * workingHours + result2), 2, MidpointRounding.AwayFromZero);
                                            defaultResolutionScenraio = false;
                                        }
                                    }
                                }
                            }

                            #endregion

                            #region Scenario 10

                            //Scenario 10 : If an Incident is reported in Service Window on Support Days and Resolved next day whch is a non-support day (May happen for P1)
                            //TAT = Last Resolved Date - Reported Date
                            if ((workingDays == 5) && (data.ReportedDateTime.DayOfWeek != DayOfWeek.Sunday && data.ReportedDateTime.DayOfWeek != DayOfWeek.Saturday)
                                && (data.LastResolvedDate.DayOfWeek == DayOfWeek.Saturday)
                            && ((data.ReportedDateTime.TimeOfDay >= startTime) && (data.ReportedDateTime.TimeOfDay <= endTime)))
                            {
                                if (data.LastResolvedDate.Date == data.ReportedDateTime.AddDays(1).Date)
                                {
                                    if (data.LastResolvedDate.DayOfWeek == DayOfWeek.Saturday)
                                    {
                                        data.TATResolution = Math.Round(Convert.ToDecimal(data.LastResolvedDate.Subtract(data.ReportedDateTime).TotalHours), 2, MidpointRounding.AwayFromZero);
                                        defaultResolutionScenraio = false;
                                    }
                                }
                            }
                            else if ((workingDays == 6) && (data.ReportedDateTime.DayOfWeek != DayOfWeek.Sunday) && (data.LastResolvedDate.DayOfWeek == DayOfWeek.Sunday)
                                && ((data.ReportedDateTime.TimeOfDay >= startTime) && (data.ReportedDateTime.TimeOfDay <= endTime)))
                            {
                                if (data.LastResolvedDate.Date == data.ReportedDateTime.AddDays(1).Date)
                                {
                                    if (data.LastResolvedDate.DayOfWeek == DayOfWeek.Sunday)
                                    {
                                        data.TATResolution = Math.Round(Convert.ToDecimal(data.LastResolvedDate.Subtract(data.ReportedDateTime).TotalHours), 2, MidpointRounding.AwayFromZero);
                                        defaultResolutionScenraio = false;
                                    }
                                }
                            }
                            #endregion
                            // Scenario 11 & 12 are duplicates
                            #region Scenario 13
                            // reported day on support day and reported time is within sw and resolved on support day and day of resolution is next day 
                            //and resolved time is before sw
                            if (workingDays == 5 && (data.ReportedDateTime.DayOfWeek != DayOfWeek.Sunday && data.ReportedDateTime.DayOfWeek != DayOfWeek.Saturday)
                                && (data.ReportedDateTime.TimeOfDay >= startTime && data.ReportedDateTime.TimeOfDay <= endTime)
                                && (data.LastResolvedDate.DayOfWeek != DayOfWeek.Saturday && data.LastResolvedDate.DayOfWeek != DayOfWeek.Sunday)
                                )
                            {
                                if (data.ReportedDateTime.DayOfWeek == DayOfWeek.Friday)
                                {
                                    if ((data.LastResolvedDate.Date == data.ReportedDateTime.AddDays(3).Date) && (data.LastResolvedDate.TimeOfDay < startTime))
                                    {
                                        data.TATResolution = Math.Round(Convert.ToDecimal(data.LastResolvedDate.Subtract(data.ReportedDateTime).TotalHours), 2, MidpointRounding.AwayFromZero);
                                        defaultResolutionScenraio = false;
                                    }

                                }
                                else
                                {
                                    if ((data.LastResolvedDate.Date == data.ReportedDateTime.AddDays(1).Date) && (data.LastResolvedDate.TimeOfDay < startTime))
                                    {
                                        data.TATResolution = Math.Round(Convert.ToDecimal(data.LastResolvedDate.Subtract(data.ReportedDateTime).TotalHours), 2, MidpointRounding.AwayFromZero);
                                        defaultResolutionScenraio = false;
                                    }

                                }
                            }
                            else if (workingDays == 6 && (data.ReportedDateTime.DayOfWeek != DayOfWeek.Sunday)
                                && (data.ReportedDateTime.TimeOfDay >= startTime && data.ReportedDateTime.TimeOfDay <= endTime)
                                && (data.LastResolvedDate.DayOfWeek != DayOfWeek.Sunday))
                            {
                                if (data.ReportedDateTime.DayOfWeek == DayOfWeek.Saturday)
                                {
                                    if ((data.LastResolvedDate.Date == data.ReportedDateTime.AddDays(2).Date) && (data.LastResolvedDate.TimeOfDay < startTime))
                                    {
                                        data.TATResolution = Math.Round(Convert.ToDecimal(data.LastResolvedDate.Subtract(data.ReportedDateTime).TotalHours), 2, MidpointRounding.AwayFromZero);
                                        defaultResolutionScenraio = false;
                                    }

                                }
                                else
                                {
                                    if ((data.LastResolvedDate.Date == data.ReportedDateTime.AddDays(1).Date) && (data.LastResolvedDate.TimeOfDay < startTime))
                                    {
                                        data.TATResolution = Math.Round(Convert.ToDecimal(data.LastResolvedDate.Subtract(data.ReportedDateTime).TotalHours), 2, MidpointRounding.AwayFromZero);
                                        defaultResolutionScenraio = false;
                                    }

                                }
                            }
                            else if (workingDays == 7 && (data.ReportedDateTime.TimeOfDay >= startTime && data.ReportedDateTime.TimeOfDay <= endTime))
                            {
                                if ((data.LastResolvedDate.Date == data.ReportedDateTime.AddDays(1).Date) && (data.LastResolvedDate.TimeOfDay < startTime))
                                {
                                    data.TATResolution = Math.Round(Convert.ToDecimal(data.LastResolvedDate.Subtract(data.ReportedDateTime).TotalHours), 2, MidpointRounding.AwayFromZero);
                                    defaultResolutionScenraio = false;
                                }

                            }
                            #endregion Scenraio 13

                            #region Scenario 17

                            if ((workingDays == 5) && (data.ReportedDateTime.DayOfWeek != DayOfWeek.Sunday && data.ReportedDateTime.DayOfWeek != DayOfWeek.Saturday)
                            && (data.ReportedDateTime.TimeOfDay > endTime) && (data.LastResolvedDate.DayOfWeek != DayOfWeek.Sunday && data.LastResolvedDate.DayOfWeek != DayOfWeek.Saturday)
                            && ((data.LastResolvedDate.TimeOfDay < startTime)))
                            {
                                if (data.ReportedDateTime.DayOfWeek == DayOfWeek.Friday)
                                {
                                    if (data.LastResolvedDate.Date == data.ReportedDateTime.AddDays(3).Date)
                                    {
                                        if (data.LastResolvedDate.ToString() != null && data.LastResolvedDate.ToString() != "")
                                        {
                                            data.TATResolution = Math.Round(Convert.ToDecimal(data.LastResolvedDate.Subtract(data.ReportedDateTime).TotalHours), 2, MidpointRounding.AwayFromZero);
                                            defaultResolutionScenraio = false;
                                        }
                                    }
                                }
                                else
                                {
                                    if (data.LastResolvedDate.Date == data.ReportedDateTime.AddDays(1).Date)
                                    {
                                        if (data.LastResolvedDate.ToString() != null && data.LastResolvedDate.ToString() != "")
                                        {
                                            data.TATResolution = Math.Round(Convert.ToDecimal(data.LastResolvedDate.Subtract(data.ReportedDateTime).TotalHours), 2, MidpointRounding.AwayFromZero);
                                            defaultResolutionScenraio = false;
                                        }
                                    }
                                }
                            }

                            else if ((workingDays == 6) && (data.ReportedDateTime.DayOfWeek != DayOfWeek.Sunday)
                             && (data.LastResolvedDate.DayOfWeek != DayOfWeek.Sunday)
                             && ((data.ReportedDateTime.TimeOfDay > endTime))
                             && ((data.LastResolvedDate.TimeOfDay < startTime)))
                            {
                                if (data.ReportedDateTime.DayOfWeek == DayOfWeek.Saturday)
                                {
                                    if (data.LastResolvedDate.Date == data.ReportedDateTime.AddDays(2).Date)
                                    {
                                        if (data.LastResolvedDate.ToString() != null && data.LastResolvedDate.ToString() != "")
                                        {
                                            data.TATResolution = Math.Round(Convert.ToDecimal(data.LastResolvedDate.Subtract(data.ReportedDateTime).TotalHours), 2, MidpointRounding.AwayFromZero);
                                            defaultResolutionScenraio = false;
                                        }
                                    }
                                }
                                else
                                {
                                    if (data.LastResolvedDate.Date == data.ReportedDateTime.AddDays(1).Date)
                                    {
                                        if (data.LastResolvedDate.ToString() != null && data.LastResolvedDate.ToString() != "")
                                        {
                                            data.TATResolution = Math.Round(Convert.ToDecimal(data.LastResolvedDate.Subtract(data.ReportedDateTime).TotalHours), 2, MidpointRounding.AwayFromZero);
                                            defaultResolutionScenraio = false;
                                        }
                                    }
                                }
                            }

                            else if ((workingDays == 7) && (data.ReportedDateTime.TimeOfDay > endTime) && (data.LastResolvedDate.TimeOfDay < startTime))
                            {
                                if (data.LastResolvedDate.Date == data.ReportedDateTime.AddDays(1).Date)
                                {
                                    if (data.LastResolvedDate.ToString() != null && data.LastResolvedDate.ToString() != "")
                                    {
                                        data.TATResolution = Math.Round(Convert.ToDecimal(data.LastResolvedDate.Subtract(data.ReportedDateTime).TotalHours), 2, MidpointRounding.AwayFromZero);
                                        defaultResolutionScenraio = false;
                                    }
                                }
                            }

                            #endregion
                        }

                        #region Scenario Default
                        //When Incident Does Not Falls in the any of the Above Scenarios then 
                        //TAT=Last Resolved Date-Reported Date-B*24 
                        if (defaultResolutionScenraio) //default case
                        {
                            if (workingDays == 5)
                            {
                                WeekendDays = WeekEndDays(data.ReportedDateTime, data.LastResolvedDate, workingDays);
                                if (data.LastResolvedDate.ToString() != null && data.LastResolvedDate.ToString() != "")
                                {
                                    result1 = data.LastResolvedDate.Subtract(data.ReportedDateTime).TotalDays;
                                    result2 = WeekendDays;
                                    if (data.ReportedDateTime.Date == data.LastResolvedDate.Date)
                                    {
                                        data.TATResolution = Math.Round(Convert.ToDecimal((result1 - result2) * 24), 2, MidpointRounding.AwayFromZero);
                                    }
                                    else
                                    {
                                        data.TATResolution = Math.Round(Convert.ToDecimal((result1 - result2) * 24), 2, MidpointRounding.AwayFromZero);
                                    }
                                }
                            }
                            else if (workingDays == 6)
                            {
                                WeekendDays = WeekEndDays(data.ReportedDateTime, data.LastResolvedDate, workingDays);
                                if (data.LastResolvedDate.ToString() != null && data.LastResolvedDate.ToString() != "")
                                {
                                    result1 = data.LastResolvedDate.Subtract(data.ReportedDateTime).TotalDays;
                                    result2 = WeekendDays;
                                    if (data.ReportedDateTime.Date == data.LastResolvedDate.Date)
                                    {
                                        data.TATResolution = Math.Round(Convert.ToDecimal((result1 - result2) * 24), 2, MidpointRounding.AwayFromZero);
                                    }
                                    else
                                    {
                                        data.TATResolution = Math.Round(Convert.ToDecimal((result1 - result2) * 24), 2, MidpointRounding.AwayFromZero);
                                    }
                                }
                            }
                            else if (workingDays == 7)
                            {
                                if (data.LastResolvedDate.ToString() != null && data.LastResolvedDate.ToString() != "")
                                {

                                    result1 = data.LastResolvedDate.Subtract(data.ReportedDateTime).TotalDays;
                                    //result2 = WeekendDays;
                                    if (data.ReportedDateTime.Date == data.LastResolvedDate.Date)
                                    {
                                        data.TATResolution = Math.Round(Convert.ToDecimal((result1) * 24), 2, MidpointRounding.AwayFromZero);
                                    }
                                    else
                                    {
                                        data.TATResolution = Math.Round(Convert.ToDecimal((result1) * 24), 2, MidpointRounding.AwayFromZero);
                                    }
                                }
                            }

                        }
                        #endregion

                    }
                    #endregion

                    #region TAT Response
                    if (data.RespondedDate != null && (data.RespondedDate != DateTime.MinValue))// if responded date is not available then do not calulate the data.TATResolution
                    {
                        if (endTime > startTime)
                        {
                            #region Scenario 0 
                            //If respondedDate is less than reportedDateTime then TAT negative values are stored as 0 
                            if (data.RespondedDate < data.ReportedDateTime)
                            {
                                data.TATResponse = 0M;
                                defaultResponseScenraio = false;
                            }
                            #endregion
                            #region Scenario 1
                            if ((workingDays == 5) && (data.ReportedDateTime.DayOfWeek != DayOfWeek.Sunday && data.ReportedDateTime.DayOfWeek != DayOfWeek.Saturday)
                                && ((data.ReportedDateTime.TimeOfDay >= startTime) && (data.ReportedDateTime.TimeOfDay <= endTime))
                                && (data.RespondedDate.DayOfWeek != DayOfWeek.Saturday && data.RespondedDate.DayOfWeek != DayOfWeek.Sunday)
                                )
                            {
                                #region Next day within SW
                                if (data.ReportedDateTime.DayOfWeek == DayOfWeek.Friday && (data.RespondedDate.TimeOfDay >= startTime))
                                {
                                    if (data.RespondedDate.Date == data.ReportedDateTime.AddDays(3).Date)
                                    {
                                        if (data.RespondedDate.ToString() != null && data.RespondedDate.ToString() != "")
                                        {
                                            actualWorkingDays = ActualDays(data.ReportedDateTime, data.RespondedDate, workingDays);
                                            result1 = endTime.Subtract(data.ReportedDateTime.TimeOfDay).TotalHours;
                                            result2 = data.RespondedDate.TimeOfDay.Subtract(startTime).TotalHours;
                                            data.TATResponse = Math.Round(Convert.ToDecimal(result1 + actualWorkingDays * workingHours + result2), 2, MidpointRounding.AwayFromZero);
                                            defaultResponseScenraio = false;
                                        }
                                    }
                                }
                                else if (data.RespondedDate.Date == data.ReportedDateTime.AddDays(1).Date && (data.RespondedDate.TimeOfDay >= startTime))
                                {
                                    if (data.RespondedDate.ToString() != null && data.RespondedDate.ToString() != "")
                                    {
                                        actualWorkingDays = ActualDays(data.ReportedDateTime, data.RespondedDate, workingDays);
                                        result1 = endTime.Subtract(data.ReportedDateTime.TimeOfDay).TotalHours;
                                        result2 = data.RespondedDate.TimeOfDay.Subtract(startTime).TotalHours;
                                        data.TATResponse = Math.Round(Convert.ToDecimal(result1 + actualWorkingDays * workingHours + result2), 2, MidpointRounding.AwayFromZero);
                                        defaultResponseScenraio = false;
                                    }

                                }

                                #endregion

                                #region Resolved on some other day within SW

                                if (data.ReportedDateTime.DayOfWeek == DayOfWeek.Friday && (data.RespondedDate.TimeOfDay >= startTime))
                                {
                                    if (data.RespondedDate.Date > data.ReportedDateTime.AddDays(3).Date)
                                    {
                                        if (data.RespondedDate.ToString() != null && data.RespondedDate.ToString() != "")
                                        {
                                            actualWorkingDays = ActualDays(data.ReportedDateTime, data.RespondedDate, workingDays);
                                            result1 = endTime.Subtract(data.ReportedDateTime.TimeOfDay).TotalHours;
                                            result2 = data.RespondedDate.TimeOfDay.Subtract(startTime).TotalHours;
                                            data.TATResponse = Math.Round(Convert.ToDecimal(result1 + actualWorkingDays * workingHours + result2), 2, MidpointRounding.AwayFromZero);
                                            defaultResponseScenraio = false;
                                        }
                                    }
                                }
                                else
                                {
                                    if (data.RespondedDate.Date > data.ReportedDateTime.AddDays(1).Date && (data.RespondedDate.TimeOfDay >= startTime))
                                    {
                                        if (data.RespondedDate.ToString() != null && data.RespondedDate.ToString() != "")
                                        {
                                            actualWorkingDays = ActualDays(data.ReportedDateTime, data.RespondedDate, workingDays);
                                            result1 = endTime.Subtract(data.ReportedDateTime.TimeOfDay).TotalHours;
                                            result2 = data.RespondedDate.TimeOfDay.Subtract(startTime).TotalHours;
                                            data.TATResponse = Math.Round(Convert.ToDecimal(result1 + actualWorkingDays * workingHours + result2), 2, MidpointRounding.AwayFromZero);
                                            defaultResponseScenraio = false;
                                        }
                                    }
                                }

                                #endregion
                            }
                            else if ((workingDays == 6) && (data.ReportedDateTime.DayOfWeek != DayOfWeek.Sunday)
                                && ((data.ReportedDateTime.TimeOfDay >= startTime) && (data.ReportedDateTime.TimeOfDay <= endTime))
                                && (data.RespondedDate.DayOfWeek != DayOfWeek.Sunday)
                                )
                            {
                                #region Next day within SW
                                if (data.ReportedDateTime.DayOfWeek == DayOfWeek.Saturday && (data.RespondedDate.TimeOfDay >= startTime))
                                {
                                    if (data.RespondedDate.Date == data.ReportedDateTime.AddDays(2).Date)
                                    {
                                        if (data.RespondedDate.ToString() != null && data.RespondedDate.ToString() != "")
                                        {
                                            actualWorkingDays = ActualDays(data.ReportedDateTime, data.RespondedDate, workingDays);
                                            result1 = endTime.Subtract(data.ReportedDateTime.TimeOfDay).TotalHours;
                                            result2 = data.RespondedDate.TimeOfDay.Subtract(startTime).TotalHours;
                                            data.TATResponse = Math.Round(Convert.ToDecimal(result1 + actualWorkingDays * workingHours + result2), 2, MidpointRounding.AwayFromZero);
                                            defaultResponseScenraio = false;
                                        }
                                    }
                                }
                                else
                                {
                                    if (data.RespondedDate.Date == data.ReportedDateTime.AddDays(1).Date && (data.RespondedDate.TimeOfDay >= startTime))
                                    {
                                        if (data.RespondedDate.ToString() != null && data.RespondedDate.ToString() != "")
                                        {
                                            actualWorkingDays = ActualDays(data.ReportedDateTime, data.RespondedDate, workingDays);
                                            result1 = endTime.Subtract(data.ReportedDateTime.TimeOfDay).TotalHours;
                                            result2 = data.RespondedDate.TimeOfDay.Subtract(startTime).TotalHours;
                                            data.TATResponse = Math.Round(Convert.ToDecimal(result1 + actualWorkingDays * workingHours + result2), 2, MidpointRounding.AwayFromZero);
                                            defaultResponseScenraio = false;
                                        }
                                    }
                                }

                                #endregion

                                #region Resolved on some other day within SW

                                if (data.ReportedDateTime.DayOfWeek == DayOfWeek.Saturday && (data.RespondedDate.TimeOfDay >= startTime))
                                {
                                    if (data.RespondedDate.Date > data.ReportedDateTime.AddDays(2).Date)
                                    {
                                        if (data.RespondedDate.ToString() != null && data.RespondedDate.ToString() != "")
                                        {
                                            actualWorkingDays = ActualDays(data.ReportedDateTime, data.RespondedDate, workingDays);
                                            result1 = endTime.Subtract(data.ReportedDateTime.TimeOfDay).TotalHours;
                                            result2 = data.RespondedDate.TimeOfDay.Subtract(startTime).TotalHours;
                                            data.TATResponse = Math.Round(Convert.ToDecimal(result1 + actualWorkingDays * workingHours + result2), 2, MidpointRounding.AwayFromZero);
                                            defaultResponseScenraio = false;
                                        }
                                    }
                                }
                                else
                                {
                                    if (data.RespondedDate.Date > data.ReportedDateTime.AddDays(1).Date && (data.RespondedDate.TimeOfDay >= startTime))
                                    {
                                        if (data.RespondedDate.ToString() != null && data.RespondedDate.ToString() != "")
                                        {
                                            actualWorkingDays = ActualDays(data.ReportedDateTime, data.RespondedDate, workingDays);
                                            result1 = endTime.Subtract(data.ReportedDateTime.TimeOfDay).TotalHours;
                                            result2 = data.RespondedDate.TimeOfDay.Subtract(startTime).TotalHours;
                                            data.TATResponse = Math.Round(Convert.ToDecimal(result1 + actualWorkingDays * workingHours + result2), 2, MidpointRounding.AwayFromZero);
                                            defaultResponseScenraio = false;
                                        }
                                    }
                                }

                                #endregion

                            }
                            else if ((workingDays == 7) && ((data.ReportedDateTime.TimeOfDay >= (startTime) && (data.ReportedDateTime.TimeOfDay <= endTime))
                                ))
                            {
                                #region Next day within SW

                                if (data.RespondedDate.Date == data.ReportedDateTime.AddDays(1).Date && data.RespondedDate.TimeOfDay >= startTime)
                                {
                                    if (data.RespondedDate.ToString() != null && data.RespondedDate.ToString() != "")
                                    {
                                        actualWorkingDays = ActualDays(data.ReportedDateTime, data.RespondedDate, workingDays);
                                        result1 = endTime.Subtract(data.ReportedDateTime.TimeOfDay).TotalHours;
                                        result2 = data.RespondedDate.TimeOfDay.Subtract(startTime).TotalHours;
                                        data.TATResponse = Math.Round(Convert.ToDecimal(result1 + actualWorkingDays * workingHours + result2), 2, MidpointRounding.AwayFromZero);
                                    }
                                    defaultResponseScenraio = false;
                                }

                                #endregion

                                #region Resolved on some other day within SW

                                if (data.RespondedDate.Date > data.ReportedDateTime.AddDays(1).Date && (data.RespondedDate.TimeOfDay >= startTime))
                                {
                                    if (data.RespondedDate.ToString() != null && data.RespondedDate.ToString() != "")
                                    {
                                        actualWorkingDays = ActualDays(data.ReportedDateTime, data.RespondedDate, workingDays);
                                        result1 = endTime.Subtract(data.ReportedDateTime.TimeOfDay).TotalHours;
                                        result2 = data.RespondedDate.TimeOfDay.Subtract(TimeSpan.Parse((shiftStartTime))).TotalHours;
                                        data.TATResponse = Math.Round(Convert.ToDecimal(result1 + actualWorkingDays * workingHours + result2), 2, MidpointRounding.AwayFromZero);
                                    }
                                    defaultResponseScenraio = false;
                                }

                                #endregion

                            }

                            #endregion Scenraio 1

                            #region Scenraio 2 and 9 are same except end time
                            //Scenario 2 : If an Incident is reported in Service Window on Support Days and Resolved on same day
                            //TAT = Last Resolved Date - Reported Date
                            if ((workingDays == 5) && (data.ReportedDateTime.DayOfWeek != DayOfWeek.Sunday && data.ReportedDateTime.DayOfWeek != DayOfWeek.Saturday)
                            && (data.RespondedDate.Date == data.ReportedDateTime.Date) && ((data.ReportedDateTime.TimeOfDay >= startTime) && (data.ReportedDateTime.TimeOfDay <= endTime))
                                && (data.RespondedDate.TimeOfDay >= startTime))
                            {
                                if (data.RespondedDate.ToString() != null && data.RespondedDate.ToString() != "")
                                {
                                    data.TATResponse = Math.Round(Convert.ToDecimal(data.RespondedDate.TimeOfDay.Subtract(data.ReportedDateTime.TimeOfDay).TotalHours), 2, MidpointRounding.AwayFromZero);
                                    defaultResponseScenraio = false;
                                }
                            }
                            else if ((workingDays == 6) && (data.ReportedDateTime.DayOfWeek != DayOfWeek.Sunday)
                                && (data.RespondedDate.Date == data.ReportedDateTime.Date) && ((data.ReportedDateTime.TimeOfDay >= startTime && (data.ReportedDateTime.TimeOfDay <= endTime))
                                && (data.RespondedDate.TimeOfDay >= startTime)))
                            {
                                if (data.RespondedDate.ToString() != null && data.RespondedDate.ToString() != "")
                                {
                                    data.TATResponse = Math.Round(Convert.ToDecimal(data.RespondedDate.TimeOfDay.Subtract(data.ReportedDateTime.TimeOfDay).TotalHours), 2, MidpointRounding.AwayFromZero);
                                    defaultResponseScenraio = false;
                                }
                            }
                            else if ((workingDays == 7) && (data.RespondedDate.Date == data.ReportedDateTime.Date) &&
                                ((data.ReportedDateTime.TimeOfDay >= startTime && (data.ReportedDateTime.TimeOfDay <= endTime))
                                && (data.RespondedDate.TimeOfDay >= startTime)))
                            {
                                if (data.RespondedDate.ToString() != null && data.RespondedDate.ToString() != "")
                                {
                                    data.TATResponse = Math.Round(Convert.ToDecimal(data.RespondedDate.TimeOfDay.Subtract(data.ReportedDateTime.TimeOfDay).TotalHours), 2, MidpointRounding.AwayFromZero);
                                    defaultResponseScenraio = false;
                                }
                            }
                            #endregion Scenraio2

                            #region Scenraio 3
                            //Scenario 3 : If an Incident is reported non-Support Hour on Support Days (before Shift Start Time) and Resolved on same day
                            //TAT = Resolved Time - Shift Start Time
                            if ((workingDays == 5) && (data.ReportedDateTime.DayOfWeek != DayOfWeek.Sunday && data.ReportedDateTime.DayOfWeek != DayOfWeek.Saturday)
                                && (data.RespondedDate.DayOfWeek != DayOfWeek.Sunday && data.RespondedDate.DayOfWeek != DayOfWeek.Saturday)
                                && (data.RespondedDate.Date == data.ReportedDateTime.Date) && (data.ReportedDateTime.TimeOfDay < startTime)
                                )
                            {
                                if (data.RespondedDate.TimeOfDay >= startTime)
                                {
                                    if (data.RespondedDate.ToString() != null && data.RespondedDate.ToString() != "")
                                    {
                                        data.TATResponse = Math.Round(Convert.ToDecimal(data.RespondedDate.TimeOfDay.Subtract(startTime).TotalHours), 2, MidpointRounding.AwayFromZero);
                                        defaultResponseScenraio = false;
                                    }

                                }
                            }
                            else if ((workingDays == 6) && (data.ReportedDateTime.DayOfWeek != DayOfWeek.Sunday)
                                && (data.RespondedDate.DayOfWeek != DayOfWeek.Sunday)
                                && (data.RespondedDate.Date == data.ReportedDateTime.Date) && ((data.ReportedDateTime.TimeOfDay < startTime))
                                )
                            {
                                if (data.RespondedDate.TimeOfDay >= startTime)
                                {
                                    if (data.RespondedDate.ToString() != null && data.RespondedDate.ToString() != "")
                                    {
                                        data.TATResponse = Math.Round(Convert.ToDecimal(data.RespondedDate.TimeOfDay.Subtract(startTime).TotalHours), 2, MidpointRounding.AwayFromZero);
                                        defaultResponseScenraio = false;
                                    }

                                }
                            }
                            else if ((workingDays == 7) && (data.RespondedDate.Date == data.ReportedDateTime.Date)
                                && ((data.ReportedDateTime.TimeOfDay < startTime))
                                )
                            {
                                if (data.RespondedDate.TimeOfDay >= startTime)
                                {
                                    if (data.RespondedDate.ToString() != null && data.RespondedDate.ToString() != "")
                                    {
                                        data.TATResponse = Math.Round(Convert.ToDecimal(data.RespondedDate.TimeOfDay.Subtract(startTime).TotalHours), 2, MidpointRounding.AwayFromZero);
                                        defaultResponseScenraio = false;
                                    }

                                }

                            }
                            #endregion

                            #region Scenraio 4
                            //If an Incident is reported non-Support Hour on Support Days (before Shift Start Time) and Resolved on some other day
                            //TAT = Active duration on Reported day + Duration in other Support Days + Active Duration on Reolved Day
                            //TAT = Service Window + X * Service Window + (Resolved Time - Shift Start Time)
                            if ((workingDays == 5) && (data.ReportedDateTime.DayOfWeek != DayOfWeek.Sunday && data.ReportedDateTime.DayOfWeek != DayOfWeek.Saturday)
                                && (data.RespondedDate.DayOfWeek != DayOfWeek.Sunday && data.RespondedDate.DayOfWeek != DayOfWeek.Saturday)
                                && (data.ReportedDateTime.TimeOfDay < startTime)
                               )
                            {
                                if (data.ReportedDateTime.DayOfWeek == DayOfWeek.Friday)
                                {
                                    if (data.RespondedDate.Date >= data.ReportedDateTime.AddDays(3).Date && (data.RespondedDate.TimeOfDay >= startTime))
                                    {
                                        if (data.RespondedDate.ToString() != null && data.RespondedDate.ToString() != "")
                                        {
                                            actualWorkingDays = ActualDays(data.ReportedDateTime, data.RespondedDate, workingDays);
                                            result2 = data.RespondedDate.TimeOfDay.Subtract(startTime).TotalHours;
                                            data.TATResponse = Math.Round(Convert.ToDecimal(workingHours + actualWorkingDays * workingHours + result2), 2, MidpointRounding.AwayFromZero);
                                            defaultResponseScenraio = false;
                                        }
                                    }
                                }
                                if (data.ReportedDateTime.DayOfWeek != DayOfWeek.Friday)
                                {
                                    if (data.RespondedDate.Date >= data.ReportedDateTime.AddDays(1).Date && (data.RespondedDate.TimeOfDay >= startTime))
                                    {
                                        if (data.RespondedDate.ToString() != null && data.RespondedDate.ToString() != "")
                                        {
                                            actualWorkingDays = ActualDays(data.ReportedDateTime, data.RespondedDate, workingDays);
                                            result2 = data.RespondedDate.TimeOfDay.Subtract(startTime).TotalHours;
                                            data.TATResponse = Math.Round(Convert.ToDecimal(workingHours + actualWorkingDays * workingHours + result2), 2, MidpointRounding.AwayFromZero);
                                            defaultResponseScenraio = false;
                                        }
                                    }
                                }
                            }
                            else if ((workingDays == 6) && (data.ReportedDateTime.DayOfWeek != DayOfWeek.Sunday)
                                && (data.RespondedDate.DayOfWeek != DayOfWeek.Sunday)
                                && (data.ReportedDateTime.TimeOfDay < startTime)
                                )
                            {
                                if (data.ReportedDateTime.DayOfWeek == DayOfWeek.Saturday)
                                {
                                    if (data.RespondedDate.Date >= data.ReportedDateTime.AddDays(2).Date && (data.RespondedDate.TimeOfDay >= startTime))
                                    {
                                        if (data.RespondedDate.ToString() != null && data.RespondedDate.ToString() != "")
                                        {
                                            actualWorkingDays = ActualDays(data.ReportedDateTime, data.RespondedDate, workingDays);
                                            result2 = data.RespondedDate.TimeOfDay.Subtract(startTime).TotalHours;
                                            data.TATResponse = Math.Round(Convert.ToDecimal(workingHours + actualWorkingDays * workingHours + result2), 2, MidpointRounding.AwayFromZero);
                                            defaultResponseScenraio = false;
                                        }
                                    }
                                }
                                if (data.ReportedDateTime.DayOfWeek != DayOfWeek.Saturday)
                                {
                                    if (data.RespondedDate.Date >= data.ReportedDateTime.AddDays(1).Date && (data.RespondedDate.TimeOfDay >= startTime))
                                    {
                                        if (data.RespondedDate.ToString() != null && data.RespondedDate.ToString() != "")
                                        {
                                            actualWorkingDays = ActualDays(data.ReportedDateTime, data.RespondedDate, workingDays);
                                            result2 = data.RespondedDate.TimeOfDay.Subtract(startTime).TotalHours;
                                            data.TATResponse = Math.Round(Convert.ToDecimal(workingHours + actualWorkingDays * workingHours + result2), 2, MidpointRounding.AwayFromZero);
                                            defaultResponseScenraio = false;
                                        }
                                    }
                                }
                            }
                            else if ((workingDays == 7) && (data.RespondedDate.Date >= data.ReportedDateTime.Date)
                                && (data.ReportedDateTime.TimeOfDay < startTime)
                                && (data.ReportedDateTime.TimeOfDay < startTime))
                            {
                                if (data.RespondedDate.Date >= data.ReportedDateTime.AddDays(1).Date && (data.RespondedDate.TimeOfDay >= startTime))
                                {
                                    if (data.RespondedDate.ToString() != null && data.RespondedDate.ToString() != "")
                                    {
                                        actualWorkingDays = ActualDays(data.ReportedDateTime, data.RespondedDate, workingDays);
                                        result2 = data.RespondedDate.TimeOfDay.Subtract(startTime).TotalHours;
                                        data.TATResponse = Math.Round(Convert.ToDecimal(workingHours + actualWorkingDays * workingHours + result2), 2, MidpointRounding.AwayFromZero);
                                        defaultResponseScenraio = false;
                                    }
                                }

                            }
                            #endregion

                            #region Scenraio 5
                            //If an Incident is reported non-Support Hour on Support Days (after Shift End Time) and Resolved on next day
                            //TAT = Resolved Time - 'Shift Start Time' of next day

                            if ((workingDays == 5) && (data.ReportedDateTime.DayOfWeek != DayOfWeek.Sunday && data.ReportedDateTime.DayOfWeek != DayOfWeek.Saturday)
                            && (data.ReportedDateTime.TimeOfDay > endTime) && (data.RespondedDate.DayOfWeek != DayOfWeek.Sunday && data.RespondedDate.DayOfWeek != DayOfWeek.Saturday)
                            && ((data.RespondedDate.TimeOfDay >= startTime)))
                            {
                                if (data.ReportedDateTime.DayOfWeek == DayOfWeek.Friday)
                                {
                                    if (data.RespondedDate.Date == data.ReportedDateTime.AddDays(3).Date)
                                    {
                                        if (data.RespondedDate.ToString() != null && data.RespondedDate.ToString() != "")
                                        {
                                            data.TATResponse = Math.Round(Convert.ToDecimal(data.RespondedDate.TimeOfDay.Subtract(startTime).TotalHours), 2, MidpointRounding.AwayFromZero);
                                        }
                                        defaultResponseScenraio = false;
                                    }
                                }
                                else
                                {
                                    if (data.RespondedDate.Date == data.ReportedDateTime.AddDays(1).Date)
                                    {
                                        if (data.RespondedDate.ToString() != null && data.RespondedDate.ToString() != "")
                                        {
                                            data.TATResponse = Math.Round(Convert.ToDecimal(data.RespondedDate.TimeOfDay.Subtract(startTime).TotalHours), 2, MidpointRounding.AwayFromZero);
                                        }
                                        defaultResponseScenraio = false;
                                    }
                                }
                            }

                            else if ((workingDays == 6) && (data.ReportedDateTime.DayOfWeek != DayOfWeek.Sunday)
                             && (data.RespondedDate.DayOfWeek != DayOfWeek.Sunday)
                             && ((data.ReportedDateTime.TimeOfDay > endTime))
                             && ((data.RespondedDate.TimeOfDay >= startTime)))
                            {
                                if (data.ReportedDateTime.DayOfWeek == DayOfWeek.Saturday)
                                {
                                    if (data.RespondedDate.Date == data.ReportedDateTime.AddDays(2).Date)
                                    {
                                        if (data.RespondedDate.ToString() != null && data.RespondedDate.ToString() != "")
                                        {
                                            data.TATResponse = Math.Round(Convert.ToDecimal(data.RespondedDate.TimeOfDay.Subtract(startTime).TotalHours), 2, MidpointRounding.AwayFromZero);
                                        }
                                        defaultResponseScenraio = false;
                                    }
                                }
                                else
                                {
                                    if (data.RespondedDate.Date == data.ReportedDateTime.AddDays(1).Date)
                                    {
                                        if (data.RespondedDate.ToString() != null && data.RespondedDate.ToString() != "")
                                        {
                                            data.TATResponse = Math.Round(Convert.ToDecimal(data.RespondedDate.TimeOfDay.Subtract(startTime).TotalHours), 2, MidpointRounding.AwayFromZero);
                                        }
                                        defaultResponseScenraio = false;
                                    }
                                }
                            }

                            else if ((workingDays == 7) && (data.ReportedDateTime.TimeOfDay > endTime) && (data.RespondedDate.TimeOfDay >= startTime))
                            {
                                if (data.RespondedDate.Date == data.ReportedDateTime.AddDays(1).Date)
                                {
                                    if (data.RespondedDate.ToString() != null && data.RespondedDate.ToString() != "")
                                    {
                                        data.TATResponse = Math.Round(Convert.ToDecimal(data.RespondedDate.TimeOfDay.Subtract(startTime).TotalHours), 2, MidpointRounding.AwayFromZero);
                                    }
                                    defaultResponseScenraio = false;
                                }
                            }
                            #endregion

                            #region Scenraio 6
                            //Scenario 6 : If an Incident is reported non-Support Hour on Support Days (after Shift End Time) and Resolved on some otherday
                            //TAT =  Duration in other Support Days + Active Duration on Resolved Day
                            //TAT = X * Service Window + (Resolved Time - Shift Start Time)

                            if ((workingDays == 5) && (data.ReportedDateTime.DayOfWeek != DayOfWeek.Sunday && data.ReportedDateTime.DayOfWeek != DayOfWeek.Saturday)
                            && ((data.ReportedDateTime.TimeOfDay > endTime))
                            && (data.RespondedDate.DayOfWeek != DayOfWeek.Saturday && data.RespondedDate.DayOfWeek != DayOfWeek.Sunday)
                            && (data.RespondedDate.TimeOfDay >= startTime))
                            {
                                if (data.ReportedDateTime.DayOfWeek == DayOfWeek.Friday)
                                {
                                    if (data.RespondedDate.Date > data.ReportedDateTime.AddDays(3).Date)
                                    {
                                        if (data.RespondedDate.ToString() != null && data.RespondedDate.ToString() != "")
                                        {
                                            actualWorkingDays = ActualDays(data.ReportedDateTime, data.RespondedDate, workingDays);
                                            result2 = data.RespondedDate.TimeOfDay.Subtract(startTime).TotalHours;
                                            data.TATResponse = Math.Round(Convert.ToDecimal(actualWorkingDays * workingHours + result2), 2, MidpointRounding.AwayFromZero);
                                            defaultResponseScenraio = false;
                                        }

                                    }
                                }
                                else
                                {
                                    if (data.RespondedDate.Date > data.ReportedDateTime.AddDays(1).Date)
                                    {
                                        if (data.RespondedDate.ToString() != null && data.RespondedDate.ToString() != "")
                                        {
                                            actualWorkingDays = ActualDays(data.ReportedDateTime, data.RespondedDate, workingDays);
                                            result2 = data.RespondedDate.TimeOfDay.Subtract(startTime).TotalHours;
                                            data.TATResponse = Math.Round(Convert.ToDecimal(actualWorkingDays * workingHours + result2), 2, MidpointRounding.AwayFromZero);
                                            defaultResponseScenraio = false;
                                        }

                                    }
                                }
                            }
                            else if ((workingDays == 6) && (data.ReportedDateTime.DayOfWeek != DayOfWeek.Sunday)
                                && ((data.ReportedDateTime.TimeOfDay > endTime))
                                && (data.RespondedDate.DayOfWeek != DayOfWeek.Sunday)
                                && (data.RespondedDate.TimeOfDay >= startTime))
                            {
                                if (data.ReportedDateTime.DayOfWeek == DayOfWeek.Saturday)
                                {
                                    if (data.RespondedDate.Date > data.ReportedDateTime.AddDays(2).Date)
                                    {
                                        if (data.RespondedDate.ToString() != null && data.RespondedDate.ToString() != "")
                                        {
                                            actualWorkingDays = ActualDays(data.ReportedDateTime, data.RespondedDate, workingDays);
                                            result2 = data.RespondedDate.TimeOfDay.Subtract(startTime).TotalHours;
                                            data.TATResponse = Math.Round(Convert.ToDecimal(actualWorkingDays * workingHours + result2), 2, MidpointRounding.AwayFromZero);
                                            defaultResponseScenraio = false;
                                        }

                                    }
                                }
                                else
                                {
                                    if (data.RespondedDate.Date > data.ReportedDateTime.AddDays(1).Date)
                                    {
                                        if (data.RespondedDate.ToString() != null && data.RespondedDate.ToString() != "")
                                        {
                                            actualWorkingDays = ActualDays(data.ReportedDateTime, data.RespondedDate, workingDays);
                                            result2 = data.RespondedDate.TimeOfDay.Subtract(startTime).TotalHours;
                                            data.TATResponse = Math.Round(Convert.ToDecimal(actualWorkingDays * workingHours + result2), 2, MidpointRounding.AwayFromZero);
                                            defaultResponseScenraio = false;
                                        }

                                    }
                                }
                            }
                            else if ((workingDays == 7) && ((data.ReportedDateTime.TimeOfDay > endTime))
                                && (data.RespondedDate.TimeOfDay >= startTime))
                            {
                                if (data.RespondedDate.Date > data.ReportedDateTime.AddDays(1).Date)
                                {
                                    if (data.RespondedDate.ToString() != null && data.RespondedDate.ToString() != "")
                                    {
                                        actualWorkingDays = ActualDays(data.ReportedDateTime, data.RespondedDate, workingDays);
                                        result2 = data.RespondedDate.TimeOfDay.Subtract(startTime).TotalHours;
                                        data.TATResponse = Math.Round(Convert.ToDecimal(actualWorkingDays * workingHours + result2), 2, MidpointRounding.AwayFromZero);
                                        defaultResponseScenraio = false;
                                    }

                                }
                            }
                            #endregion

                            #region Scenraio 7
                            //Scenario 7 : If an Incident is raised during non-Support Days and Resolved on next Support Day
                            //TAT = Resolve Time - 'Shift Start Time' of next Support Day

                            if ((workingDays == 5) && (data.ReportedDateTime.DayOfWeek == DayOfWeek.Sunday || data.ReportedDateTime.DayOfWeek == DayOfWeek.Saturday)
                                && (data.RespondedDate.TimeOfDay >= startTime)
                                && (data.RespondedDate.DayOfWeek != DayOfWeek.Saturday && data.RespondedDate.DayOfWeek != DayOfWeek.Sunday)
                                )
                            {
                                if (data.ReportedDateTime.DayOfWeek == DayOfWeek.Saturday)
                                {
                                    if (data.RespondedDate.Date == data.ReportedDateTime.AddDays(2).Date)
                                    {
                                        if (data.RespondedDate.ToString() != null && data.RespondedDate.ToString() != "")
                                        {
                                            data.TATResponse = Math.Round(Convert.ToDecimal(data.RespondedDate.TimeOfDay.Subtract(startTime).TotalHours), 2, MidpointRounding.AwayFromZero);
                                        }
                                        defaultResponseScenraio = false;
                                    }
                                }
                                else if (data.ReportedDateTime.DayOfWeek == DayOfWeek.Sunday)
                                {
                                    if (data.RespondedDate.Date == data.ReportedDateTime.AddDays(1).Date)
                                    {
                                        if (data.RespondedDate.ToString() != null && data.RespondedDate.ToString() != "")
                                        {
                                            data.TATResponse = Math.Round(Convert.ToDecimal(data.RespondedDate.TimeOfDay.Subtract(startTime).TotalHours), 2, MidpointRounding.AwayFromZero);
                                        }
                                        defaultResponseScenraio = false;
                                    }
                                }
                            }
                            else if ((workingDays == 6) && (data.ReportedDateTime.DayOfWeek == DayOfWeek.Sunday) && (data.RespondedDate.DayOfWeek != DayOfWeek.Sunday)
                                && (data.RespondedDate.TimeOfDay >= startTime))
                            {
                                if (data.ReportedDateTime.DayOfWeek == DayOfWeek.Sunday)
                                {
                                    if (data.RespondedDate.Date == data.ReportedDateTime.AddDays(1).Date)
                                    {
                                        if (data.RespondedDate.ToString() != null && data.RespondedDate.ToString() != "")
                                        {
                                            data.TATResponse = Math.Round(Convert.ToDecimal(data.RespondedDate.TimeOfDay.Subtract(startTime).TotalHours), 2, MidpointRounding.AwayFromZero);
                                        }
                                        defaultResponseScenraio = false;
                                    }
                                }
                            }
                            #endregion

                            #region Scenraio 8
                            //Scenario 8 : If an Incident is raised during non-Support Days and Resolved on other Support Day
                            //Find number of Support Days between Last Resolved Date and Reported Days (Exclude Resolved Day)
                            //TAT = Active duration in other Support Days + Active Duration on Reolved Day
                            //TAT = X * Service Window + (Resolved Time - Shift Start Time)
                            if ((workingDays == 5) && (data.ReportedDateTime.DayOfWeek == DayOfWeek.Sunday || data.ReportedDateTime.DayOfWeek == DayOfWeek.Saturday)
                               && (data.RespondedDate.TimeOfDay >= startTime)
                               && (data.RespondedDate.DayOfWeek != DayOfWeek.Saturday && data.RespondedDate.DayOfWeek != DayOfWeek.Sunday)
                               )
                            {
                                if (data.ReportedDateTime.DayOfWeek == DayOfWeek.Saturday)
                                {
                                    if (data.RespondedDate.Date > data.ReportedDateTime.AddDays(2).Date)
                                    {

                                        if (data.RespondedDate.ToString() != null && data.RespondedDate.ToString() != "")
                                        {
                                            actualWorkingDays = ActualDays(data.ReportedDateTime, data.RespondedDate, workingDays);
                                            result2 = data.RespondedDate.TimeOfDay.Subtract(startTime).TotalHours;
                                            data.TATResponse = Math.Round(Convert.ToDecimal(actualWorkingDays * workingHours + result2), 2, MidpointRounding.AwayFromZero);
                                        }
                                        defaultResponseScenraio = false;
                                    }
                                }
                                else if (data.ReportedDateTime.DayOfWeek == DayOfWeek.Sunday)
                                {
                                    if (data.RespondedDate.Date > data.ReportedDateTime.AddDays(1).Date)
                                    {
                                        if (data.RespondedDate.ToString() != null && data.RespondedDate.ToString() != "")
                                        {
                                            actualWorkingDays = ActualDays(data.ReportedDateTime, data.RespondedDate, workingDays);
                                            result2 = data.RespondedDate.TimeOfDay.Subtract(startTime).TotalHours;
                                            data.TATResponse = Math.Round(Convert.ToDecimal(actualWorkingDays * workingHours + result2), 2, MidpointRounding.AwayFromZero);
                                        }
                                        defaultResponseScenraio = false;
                                    }
                                }
                            }
                            else if ((workingDays == 6) && (data.ReportedDateTime.DayOfWeek == DayOfWeek.Sunday) && (data.RespondedDate.DayOfWeek != DayOfWeek.Sunday)
                                && (data.RespondedDate.TimeOfDay >= startTime))
                            {
                                if (data.ReportedDateTime.DayOfWeek == DayOfWeek.Sunday)
                                {
                                    if (data.RespondedDate.Date > data.ReportedDateTime.AddDays(1).Date)
                                    {
                                        if (data.RespondedDate.ToString() != null && data.RespondedDate.ToString() != "")
                                        {
                                            actualWorkingDays = ActualDays(data.ReportedDateTime, data.RespondedDate, workingDays);
                                            result2 = data.RespondedDate.TimeOfDay.Subtract(startTime).TotalHours;
                                            data.TATResponse = Math.Round(Convert.ToDecimal(actualWorkingDays * workingHours + result2), 2, MidpointRounding.AwayFromZero);
                                        }
                                        defaultResponseScenraio = false;
                                    }
                                }
                            }

                            #endregion

                            #region Scenraio 10

                            //Scenario 10 : If an Incident is reported in Service Window on Support Days and Resolved next day whch is a non-support day (May happen for P1)
                            //TAT = Last Resolved Date - Reported Date
                            if ((workingDays == 5) && (data.ReportedDateTime.DayOfWeek != DayOfWeek.Sunday && data.ReportedDateTime.DayOfWeek != DayOfWeek.Saturday)
                                && (data.RespondedDate.DayOfWeek == DayOfWeek.Saturday)
                            && ((data.ReportedDateTime.TimeOfDay >= startTime) && (data.ReportedDateTime.TimeOfDay <= endTime)))
                            {
                                if (data.RespondedDate.Date == data.ReportedDateTime.AddDays(1).Date)
                                {
                                    if (data.RespondedDate.DayOfWeek == DayOfWeek.Saturday)
                                    {
                                        data.TATResponse = Math.Round(Convert.ToDecimal(data.RespondedDate.Subtract(data.ReportedDateTime).TotalHours), 2, MidpointRounding.AwayFromZero);
                                        defaultResponseScenraio = false;
                                    }
                                }
                            }
                            else if ((workingDays == 6) && (data.ReportedDateTime.DayOfWeek != DayOfWeek.Sunday) && (data.RespondedDate.DayOfWeek == DayOfWeek.Sunday)
                                && ((data.ReportedDateTime.TimeOfDay >= startTime) && (data.ReportedDateTime.TimeOfDay <= endTime)))
                            {
                                if (data.RespondedDate.Date == data.ReportedDateTime.AddDays(1).Date)
                                {
                                    if (data.RespondedDate.DayOfWeek == DayOfWeek.Sunday)
                                    {
                                        data.TATResponse = Math.Round(Convert.ToDecimal(data.RespondedDate.Subtract(data.ReportedDateTime).TotalHours), 2, MidpointRounding.AwayFromZero);
                                        defaultResponseScenraio = false;
                                    }
                                }
                            }
                            #endregion
                            // Scenario 11 & 12 are duplicates
                            #region Scenraio 13
                            // reported day on support day and reported time is within sw and resolved on support day and day of resolution is next day 
                            //and resolved time is before sw
                            if (workingDays == 5 && (data.ReportedDateTime.DayOfWeek != DayOfWeek.Sunday && data.ReportedDateTime.DayOfWeek != DayOfWeek.Saturday)
                                && (data.ReportedDateTime.TimeOfDay >= startTime && data.ReportedDateTime.TimeOfDay <= endTime)
                                && (data.RespondedDate.DayOfWeek != DayOfWeek.Saturday && data.RespondedDate.DayOfWeek != DayOfWeek.Sunday)
                                )
                            {
                                if (data.ReportedDateTime.DayOfWeek == DayOfWeek.Friday)
                                {
                                    if ((data.RespondedDate.Date == data.ReportedDateTime.AddDays(3).Date) && (data.RespondedDate.TimeOfDay < startTime))
                                    {
                                        data.TATResponse = Math.Round(Convert.ToDecimal(data.RespondedDate.Subtract(data.ReportedDateTime).TotalHours), 2, MidpointRounding.AwayFromZero);
                                        defaultResponseScenraio = false;
                                    }

                                }
                                else
                                {
                                    if ((data.RespondedDate.Date == data.ReportedDateTime.AddDays(1).Date) && (data.RespondedDate.TimeOfDay < startTime))
                                    {
                                        data.TATResponse = Math.Round(Convert.ToDecimal(data.RespondedDate.Subtract(data.ReportedDateTime).TotalHours), 2, MidpointRounding.AwayFromZero);
                                        defaultResponseScenraio = false;
                                    }

                                }
                            }
                            else if (workingDays == 6 && (data.ReportedDateTime.DayOfWeek != DayOfWeek.Sunday)
                                && (data.ReportedDateTime.TimeOfDay >= startTime && data.ReportedDateTime.TimeOfDay <= endTime)
                                && (data.RespondedDate.DayOfWeek != DayOfWeek.Sunday))
                            {
                                if (data.ReportedDateTime.DayOfWeek == DayOfWeek.Saturday)
                                {
                                    if ((data.RespondedDate.Date == data.ReportedDateTime.AddDays(2).Date) && (data.RespondedDate.TimeOfDay < startTime))
                                    {
                                        data.TATResponse = Convert.ToDecimal(data.RespondedDate.Subtract(data.ReportedDateTime).TotalHours);
                                        defaultResponseScenraio = false;
                                    }

                                }
                                else
                                {
                                    if ((data.RespondedDate.Date == data.ReportedDateTime.AddDays(1).Date) && (data.RespondedDate.TimeOfDay < startTime))
                                    {
                                        data.TATResponse = Convert.ToDecimal(data.RespondedDate.Subtract(data.ReportedDateTime).TotalHours);
                                        defaultResponseScenraio = false;
                                    }

                                }
                            }
                            else if (workingDays == 7 && (data.ReportedDateTime.TimeOfDay >= startTime && data.ReportedDateTime.TimeOfDay <= endTime))
                            {
                                if ((data.RespondedDate.Date == data.ReportedDateTime.AddDays(1).Date) && (data.RespondedDate.TimeOfDay < startTime))
                                {
                                    data.TATResponse = Convert.ToDecimal(data.RespondedDate.Subtract(data.ReportedDateTime).TotalHours);
                                    defaultResponseScenraio = false;
                                }

                            }
                            #endregion Scenraio 13

                            #region Scenario 17

                            if ((workingDays == 5) && (data.ReportedDateTime.DayOfWeek != DayOfWeek.Sunday && data.ReportedDateTime.DayOfWeek != DayOfWeek.Saturday)
                            && (data.ReportedDateTime.TimeOfDay > endTime) && (data.RespondedDate.DayOfWeek != DayOfWeek.Sunday && data.RespondedDate.DayOfWeek != DayOfWeek.Saturday)
                            && ((data.RespondedDate.TimeOfDay < startTime)))
                            {
                                if (data.ReportedDateTime.DayOfWeek == DayOfWeek.Friday)
                                {
                                    if (data.RespondedDate.Date == data.ReportedDateTime.AddDays(3).Date)
                                    {
                                        if (data.RespondedDate.ToString() != null && data.RespondedDate.ToString() != "")
                                        {
                                            data.TATResponse = Math.Round(Convert.ToDecimal(data.RespondedDate.Subtract(data.ReportedDateTime).TotalHours), 2, MidpointRounding.AwayFromZero);
                                        }
                                        defaultResponseScenraio = false;
                                    }
                                }
                                else
                                {
                                    if (data.RespondedDate.Date == data.ReportedDateTime.AddDays(1).Date)
                                    {
                                        if (data.RespondedDate.ToString() != null && data.RespondedDate.ToString() != "")
                                        {
                                            data.TATResponse = Math.Round(Convert.ToDecimal(data.RespondedDate.Subtract(data.ReportedDateTime).TotalHours), 2, MidpointRounding.AwayFromZero);
                                        }
                                        defaultResponseScenraio = false;
                                    }
                                }
                            }

                            else if ((workingDays == 6) && (data.ReportedDateTime.DayOfWeek != DayOfWeek.Sunday)
                             && (data.RespondedDate.DayOfWeek != DayOfWeek.Sunday)
                             && ((data.ReportedDateTime.TimeOfDay > endTime))
                             && ((data.RespondedDate.TimeOfDay < startTime)))
                            {
                                if (data.ReportedDateTime.DayOfWeek == DayOfWeek.Saturday)
                                {
                                    if (data.RespondedDate.Date == data.ReportedDateTime.AddDays(2).Date)
                                    {
                                        if (data.RespondedDate.ToString() != null && data.RespondedDate.ToString() != "")
                                        {
                                            data.TATResponse = Math.Round(Convert.ToDecimal(data.RespondedDate.Subtract(data.ReportedDateTime).TotalHours), 2, MidpointRounding.AwayFromZero);
                                        }
                                        defaultResponseScenraio = false;
                                    }
                                }
                                else
                                {
                                    if (data.RespondedDate.Date == data.ReportedDateTime.AddDays(1).Date)
                                    {
                                        if (data.RespondedDate.ToString() != null && data.RespondedDate.ToString() != "")
                                        {
                                            data.TATResponse = Math.Round(Convert.ToDecimal(data.RespondedDate.Subtract(data.ReportedDateTime).TotalHours), 2, MidpointRounding.AwayFromZero);
                                        }
                                        defaultResponseScenraio = false;
                                    }
                                }
                            }

                            else if ((workingDays == 7) && (data.ReportedDateTime.TimeOfDay > endTime) && (data.RespondedDate.TimeOfDay < startTime))
                            {
                                if (data.RespondedDate.Date == data.ReportedDateTime.AddDays(1).Date)
                                {
                                    if (data.RespondedDate.ToString() != null && data.RespondedDate.ToString() != "")
                                    {
                                        data.TATResponse = Math.Round(Convert.ToDecimal(data.RespondedDate.Subtract(data.ReportedDateTime).TotalHours), 2, MidpointRounding.AwayFromZero);
                                    }
                                    defaultResponseScenraio = false;
                                }
                            }

                            #endregion


                            #region Scenario Default
                            //When Incident Does Not Falls in the any of the Above Scenarios then 
                            //TAT=Last Resolved Date-Reported Date-B*24 
                            if (defaultResponseScenraio) //default case
                            {
                                if (workingDays == 5)
                                {
                                    WeekendDays = WeekEndDays(data.ReportedDateTime, data.RespondedDate, workingDays);
                                    if (data.RespondedDate.ToString() != null && data.RespondedDate.ToString() != "")
                                    {
                                        result1 = data.RespondedDate.Subtract(data.ReportedDateTime).TotalDays;
                                        result2 = WeekendDays;
                                        if (data.ReportedDateTime.Date == data.RespondedDate.Date)
                                        {
                                            data.TATResponse = Math.Round(Convert.ToDecimal((result1 - result2) * 24), 2, MidpointRounding.AwayFromZero);
                                        }
                                        else
                                        {
                                            data.TATResponse = Math.Round(Convert.ToDecimal((result1 - result2) * 24), 2, MidpointRounding.AwayFromZero);
                                        }
                                    }
                                }
                                else if (workingDays == 6)
                                {
                                    WeekendDays = WeekEndDays(data.ReportedDateTime, data.RespondedDate, workingDays);
                                    if (data.RespondedDate.ToString() != null && data.RespondedDate.ToString() != "")
                                    {
                                        result1 = data.RespondedDate.Subtract(data.ReportedDateTime).TotalDays;
                                        result2 = WeekendDays;
                                        if (data.ReportedDateTime.Date == data.RespondedDate.Date)
                                        {
                                            data.TATResponse = Math.Round(Convert.ToDecimal((result1 - result2) * 24), 2, MidpointRounding.AwayFromZero);
                                        }
                                        else
                                        {
                                            data.TATResponse = Math.Round(Convert.ToDecimal((result1 - result2) * 24), 2, MidpointRounding.AwayFromZero);
                                        }
                                    }
                                }
                                else if (workingDays == 7)
                                {
                                    if (data.RespondedDate.ToString() != null && data.RespondedDate.ToString() != "")
                                    {
                                        // data.TATResolution = data.LastResolvedDate.Subtract(data.ReportedDateTime).TotalHours;
                                        result1 = data.RespondedDate.Subtract(data.ReportedDateTime).TotalDays;
                                        //result2 = WeekendDays;
                                        if (data.ReportedDateTime.Date == data.RespondedDate.Date)
                                        {
                                            data.TATResponse = Math.Round(Convert.ToDecimal((result1) * 24), 2, MidpointRounding.AwayFromZero);
                                        }
                                        else
                                        {
                                            data.TATResponse = Math.Round(Convert.ToDecimal((result1) * 24), 2, MidpointRounding.AwayFromZero);
                                        }
                                    }
                                }

                            }

                            #endregion

                        }
                    }
                    #endregion
                    TicketsDataWithTAT.Add(data);
                }
            }
            catch (Exception ex)
            {                
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(PAMTicketPullService), nameof(CalculateTAT), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                throw ex;
            }
            return TicketsDataWithTAT;
        }

        private int ActualDays(DateTime startDate, DateTime endDate, int weekDays)
        {
            int countDays = 0;
            DateTime dateIterator = startDate;
            try
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(PAMTicketPullService), nameof(ActualDays), "START", Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty);

                while (dateIterator.Date < endDate.AddDays(1).Date)
                {
                    if (weekDays == 5)
                    {
                        if (dateIterator.DayOfWeek != DayOfWeek.Saturday && dateIterator.DayOfWeek != DayOfWeek.Sunday)
                            countDays++;
                        dateIterator = dateIterator.AddDays(1);
                    }
                    else if (weekDays == 6)
                    {
                        if (dateIterator.DayOfWeek != DayOfWeek.Sunday)
                            countDays++;
                        dateIterator = dateIterator.AddDays(1);
                    }
                    else if (weekDays == 7)
                    {
                        countDays++;
                        dateIterator = dateIterator.AddDays(1);
                    }
                }
                if (weekDays == 5 && (startDate.DayOfWeek != DayOfWeek.Saturday && startDate.DayOfWeek != DayOfWeek.Sunday))
                {
                    countDays = countDays - 1;
                }
                if (weekDays == 5 && (endDate.DayOfWeek != DayOfWeek.Saturday && endDate.DayOfWeek != DayOfWeek.Sunday))
                {
                    countDays = countDays - 1;
                }

                if (weekDays == 6 && startDate.DayOfWeek != DayOfWeek.Sunday)
                {
                    countDays = countDays - 1;
                }
                if (weekDays == 6 && endDate.DayOfWeek != DayOfWeek.Sunday)
                {
                    countDays = countDays - 1;
                }
                if (weekDays == 7)
                {
                    countDays = countDays - 2;
                }

                if (countDays < 0)
                {
                    countDays = 0;
                }

                LOGGING.LogManager.Logger.LogProcessInfo(typeof(PAMTicketPullService), nameof(ActualDays), "END", Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(PAMTicketPullService), nameof(ActualDays), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }

            return countDays;
        }

        private int WeekEndDays(DateTime startDate, DateTime endDate, int weekDays)
        {
            int countDays = 0;
            DateTime dateIterator = startDate;
            try
            {
                //  Log.Information("PAMDemographics_WeekEndDays method is called");
                while (dateIterator.Date < endDate.AddDays(1).Date)
                {
                    if (weekDays == 5)
                    {
                        if (dateIterator.DayOfWeek == DayOfWeek.Saturday || dateIterator.DayOfWeek == DayOfWeek.Sunday)
                            countDays++;
                        dateIterator = dateIterator.AddDays(1);
                    }
                    else if (weekDays == 6)
                    {
                        if (dateIterator.DayOfWeek == DayOfWeek.Sunday)
                            countDays++;
                        dateIterator = dateIterator.AddDays(1);
                    }
                }
                if (weekDays == 5 && endDate.DayOfWeek == DayOfWeek.Saturday)
                {
                    countDays = countDays - 1;
                }
                if (weekDays == 5 && endDate.DayOfWeek == DayOfWeek.Sunday)
                {
                    countDays = countDays - 1;
                }
                if (weekDays == 5 && startDate.DayOfWeek == DayOfWeek.Saturday)
                {
                    countDays = countDays - 1;
                }
                if (weekDays == 5 && startDate.DayOfWeek == DayOfWeek.Sunday)
                {
                    countDays = countDays - 1;
                }
                if (weekDays == 6 && endDate.DayOfWeek == DayOfWeek.Sunday)
                {
                    countDays = countDays - 1;
                }
                if (weekDays == 6 && startDate.DayOfWeek == DayOfWeek.Sunday)
                {
                    countDays = countDays - 1;
                }
                if (countDays < 0)
                    countDays = 0;
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(PAMTicketPullService), nameof(WeekEndDays), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
            return countDays;
        }

        public List<PAMTask> ConvertTicketToTask(List<PAMTicket> TicketList)
        {
            //List<PAMTask> TaskResult = TicketList.Cast<PAMTask>().ToList();
            List<PAMTask> PamTask = new List<PAMTask>();
            if (TicketList != null)
            {
                var serialize1 = Newtonsoft.Json.JsonConvert.SerializeObject(TicketList);
                //PAMTask task = Newtonsoft.Json.JsonConvert.DeserializeObject<PAMTask>(serialize);
                List<PAMTask> task = JsonConvert.DeserializeObject<List<PAMTask>>(serialize1);
                //task.TaskId = data.IncidentNumber;

                //foreach (PAMTicket data in TicketList)
                //{
                //    var serialize = Newtonsoft.Json.JsonConvert.SerializeObject(data);
                //    PAMTask task = Newtonsoft.Json.JsonConvert.DeserializeObject<PAMTask>(serialize);
                //    //List<PAMTask> task = JsonConvert.DeserializeObject<List<PAMTask>>(serialize);
                //    //task.TaskId = data.IncidentNumber;
                //    PamTask.Add(task);
                //}

                return task;
            }
            
            return PamTask;
        }

        public void PAMTaskEntitiesDeltaPushByType(List<PAMTask> TaskLists, string collectionName, string ClientUId, string EndToEndUId)
        {            
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(PAMTicketPullService), nameof(PAMTaskEntitiesDeltaPushByType), "START", Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
            List<string> ticketIDList = TaskLists.Select(p => p.ID).ToList();
            bool status = false;
            try
            {
                if (TaskLists != null && TaskLists.Count > 0)
                {
                    //Removing duplicate records from list
                    TaskLists = TaskLists.GroupBy(x => new { x.ID, x.ClientUId, x.EndToEndUId }).Select(x => x.FirstOrDefault()).ToList();

                    #region check if Ticket Type is changed, delete from existing collection

                    var delfilterBuilder = Builders<PAMTask>.Filter;
                    var uniqueTicketIds = TaskLists.Select(p => p.ID).ToArray();
                    var delFilter = delfilterBuilder.In("ID", uniqueTicketIds) & delfilterBuilder.Eq("ClientUId", ClientUId) & delfilterBuilder.Eq("EndToEndUId", EndToEndUId);

                    //Remove Tickets from Problem Ticket If Exists with same Incident Number and Different Service Type
                    if (collectionName != CONSTANTS.DB_AOTaskCollection)
                    {
                        var TaskCollection = _database.GetCollection<PAMTask>(CONSTANTS.DB_AOTaskCollection);
                        var filterDuplicateTickets = delfilterBuilder.In("ID", uniqueTicketIds);
                        List<string> toBeDelIncDoc = TaskCollection.Find(delFilter).ToList().Select(a => a.ID).ToList();

                        if (toBeDelIncDoc != null && toBeDelIncDoc.Count > 0)
                        {
                            var delDuplicFilter = delfilterBuilder.In("ID", toBeDelIncDoc) & delfilterBuilder.Eq("ClientUId", ClientUId) & delfilterBuilder.Eq("EndToEndUId", EndToEndUId);
                            TaskCollection.DeleteMany(delDuplicFilter);
                        }
                    }
                    if (collectionName != CONSTANTS.DB_IOTaskCollection)
                    {
                        var IOTaskCollection = _database.GetCollection<PAMTask>(CONSTANTS.DB_IOTaskCollection);
                        var filterDuplicateTickets = delfilterBuilder.In("ID", uniqueTicketIds) & delfilterBuilder.Eq("ClientUId", ClientUId) & delfilterBuilder.Eq("EndToEndUId", EndToEndUId);
                        List<string> toBeDelIncDoc = IOTaskCollection.Find(delFilter).ToList().Select(a => a.ID).ToList();

                        if (toBeDelIncDoc != null && toBeDelIncDoc.Count > 0)
                        {
                            var delDuplicFilter = delfilterBuilder.In("ID", toBeDelIncDoc) & delfilterBuilder.Eq("ClientUId", ClientUId) & delfilterBuilder.Eq("EndToEndUId", EndToEndUId);
                            IOTaskCollection.DeleteMany(delDuplicFilter);
                        }
                    }



                    #endregion

                    #region check if Domain is changed, delete from existing collection
                    var delfilterBuilderDomain = Builders<IOTicketCollection>.Filter;
                    var uniqueTicketIdList = TaskLists.Select(p => p.ID).ToArray();
                    var delFilterDomain = delfilterBuilderDomain.In("ID", uniqueTicketIdList) & delfilterBuilderDomain.Eq("ClientUId", ClientUId) & delfilterBuilderDomain.Eq("EndToEndUId", EndToEndUId);

                    //Remove Tickets from ioTicket collection If Exists with same Incident Number                
                    var ioTicketcollection = _database.GetCollection<IOTicketCollection>(CONSTANTS.DB_IOTicketCollection);
                    List<string> toBeDelIODoc = ioTicketcollection.Find(delFilterDomain).ToList().Select(a => a.ID).ToList();

                    if (toBeDelIODoc != null && toBeDelIODoc.Count > 0)
                    {
                        var delDuplicFilter = delfilterBuilderDomain.In("ID", toBeDelIODoc) & delfilterBuilderDomain.Eq("ClientUId", ClientUId) & delfilterBuilderDomain.Eq("EndToEndUId", EndToEndUId);
                        ioTicketcollection.DeleteMany(delDuplicFilter);
                    }
                    #endregion

                    var IDList = TaskLists.Select(p => p.ID).ToArray();
                    var filterBuilder = Builders<PAMTask>.Filter;

                    var ticketcollection = _database.GetCollection<PAMTask>(collectionName);
                    var filter = filterBuilder.In("ID", IDList) & filterBuilder.Eq("ClientUId", ClientUId) & filterBuilder.Eq("EndToEndUId", EndToEndUId);
                    List<PAMTask> toBeUpdatedDoc = ticketcollection.Find(filter).ToList();
                    List<object> updatedID = new List<object>();
                    //getting records which have endtoendid as null
                    //var clientLevelList = ticketList.Where(x => x.EndToEndUId == null).ToList();
                    //Updating records based on certain conditions if ticket already exists
                    foreach (var oldDoc in toBeUpdatedDoc)
                    {
                        var newUpsertData = TaskLists.FirstOrDefault(p => p.ID == oldDoc.ID && p.ClientUId == oldDoc.ClientUId && p.EndToEndUId == oldDoc.EndToEndUId && ((p.LastModifiedTime >= oldDoc.LastModifiedTime) || (p.LastModifiedTime == DateTime.MinValue && oldDoc.LastModifiedTime == DateTime.MinValue) || (p.ModifiedOn >= oldDoc.ModifiedOn)));
                        if (newUpsertData != null)
                        {
                            updatedID.Add(oldDoc.ID);
                            newUpsertData._id = oldDoc._id;
                            ticketcollection.ReplaceOne(c => c._id == oldDoc._id, newUpsertData);
                        }
                        // if lastmodified date is less than existing record in db
                        var lastmodifiedUpsertData = TaskLists.FirstOrDefault(p => p.ID == oldDoc.ID && p.ClientUId == oldDoc.ClientUId && p.EndToEndUId == oldDoc.EndToEndUId && ((p.LastModifiedTime < oldDoc.LastModifiedTime) || (p.LastModifiedTime == DateTime.MinValue && oldDoc.LastModifiedTime > DateTime.MinValue) || (p.ModifiedOn < oldDoc.ModifiedOn)));
                        if (lastmodifiedUpsertData != null)
                        {
                            updatedID.Add(lastmodifiedUpsertData.ID);
                        }
                    }                    
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(PAMTicketPullService), nameof(PAMTaskEntitiesDeltaPushByType), "UPDATE-" + toBeUpdatedDoc.Count + " tickets updated in " + collectionName, Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
                    //remove those tickets from ticketentitycollection list which are updated 
                    TaskLists.RemoveAll(x => updatedID.Contains(x.ID) && x.ClientUId == ClientUId && x.EndToEndUId == EndToEndUId);
                    List<PAMTask> toBeInsertedDoc = TaskLists.ToList();
                    //Insert if new documents
                    if (toBeInsertedDoc.Count > 0)
                    {
                        toBeInsertedDoc.ForEach(item => { item._id = ObjectId.GenerateNewId(); });
                        var document = BsonSerializer.Deserialize<List<PAMTask>>(toBeInsertedDoc.ToJson());
                        ticketcollection.InsertMany(document.AsEnumerable());
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(PAMTicketPullService), nameof(PAMTaskEntitiesDeltaPushByType), "INSERT-" + toBeInsertedDoc.Count + " tickets inserted in " + collectionName, Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty);                        
                    }
                }
                status = true;
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(PAMTicketPullService), nameof(PAMTaskEntitiesDeltaPushByType), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);                
                throw ex;
            }            
        }

        public void PAMIOTicketDeltaPushByType(List<IOTicketCollection> ticketList, string collectionName, string ClientUId, string EndToEndUId)
        {            
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(PAMTicketPullService), nameof(PAMIOTicketDeltaPushByType), "START", Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
            List<string> ticketIDList = ticketList.Select(p => p.ID).ToList();
            try
            {
                if (ticketList != null && ticketList.Count > 0)
                {
                    //Removing duplicate records from list
                    ticketList = ticketList.GroupBy(x => new { x.ID, x.ClientUId, x.EndToEndUId }).Select(x => x.FirstOrDefault()).ToList();

                    #region check if Domain is changed, delete from existing collection
                    var delfilterBuilderDomain = Builders<PAMTicket>.Filter;
                    var uniqueTicketIdList = ticketList.Select(p => p.ID).ToArray();
                    var delFilterDomain = delfilterBuilderDomain.In("ID", uniqueTicketIdList) & delfilterBuilderDomain.Eq("ClientUId", ClientUId) & delfilterBuilderDomain.Eq("EndToEndUId", EndToEndUId);

                    //Remove Tickets from ioTicket collection If Exists with same Incident Number                
                    var incidentCollection = _database.GetCollection<PAMTicket>(CONSTANTS.DB_IncidentCollection);
                    var problemCollection = _database.GetCollection<PAMTicket>(CONSTANTS.DB_ProblemCollection);
                    var serviceRequestCollection = _database.GetCollection<PAMTicket>(CONSTANTS.DB_ServiceRequestCollection);
                    var changeManagementCollection = _database.GetCollection<PAMTicket>(CONSTANTS.DB_ChangeManagementCollection);
                    List<string> toBeDelIncidentDoc = incidentCollection.Find(delFilterDomain).ToList().Select(a => a.ID).ToList();
                    List<string> toBeDelProblemDoc = problemCollection.Find(delFilterDomain).ToList().Select(a => a.ID).ToList();
                    List<string> toBeDelSRDoc = serviceRequestCollection.Find(delFilterDomain).ToList().Select(a => a.ID).ToList();
                    List<string> toBeDelCMDoc = changeManagementCollection.Find(delFilterDomain).ToList().Select(a => a.ID).ToList();

                    if (toBeDelIncidentDoc != null && toBeDelIncidentDoc.Count > 0)
                    {
                        var delDuplicFilter = delfilterBuilderDomain.In("ID", toBeDelIncidentDoc) & delfilterBuilderDomain.Eq("ClientUId", ClientUId) & delfilterBuilderDomain.Eq("EndToEndUId", EndToEndUId);
                        incidentCollection.DeleteMany(delDuplicFilter);
                    }

                    if (toBeDelProblemDoc != null && toBeDelProblemDoc.Count > 0)
                    {
                        var delDuplicFilter = delfilterBuilderDomain.In("ID", toBeDelProblemDoc) & delfilterBuilderDomain.Eq("ClientUId", ClientUId) & delfilterBuilderDomain.Eq("EndToEndUId", EndToEndUId);
                        problemCollection.DeleteMany(delDuplicFilter);
                    }

                    if (toBeDelSRDoc != null && toBeDelSRDoc.Count > 0)
                    {
                        var delDuplicFilter = delfilterBuilderDomain.In("ID", toBeDelSRDoc) & delfilterBuilderDomain.Eq("ClientUId", ClientUId) & delfilterBuilderDomain.Eq("EndToEndUId", EndToEndUId);
                        serviceRequestCollection.DeleteMany(delDuplicFilter);
                    }

                    if (toBeDelCMDoc != null && toBeDelCMDoc.Count > 0)
                    {
                        var delDuplicFilter = delfilterBuilderDomain.In("ID", toBeDelCMDoc) & delfilterBuilderDomain.Eq("ClientUId", ClientUId) & delfilterBuilderDomain.Eq("EndToEndUId", EndToEndUId);
                        changeManagementCollection.DeleteMany(delDuplicFilter);
                    }
                    #endregion

                    var IDList = ticketList.Select(p => p.ID).ToArray();
                    var filterBuilder = Builders<IOTicketCollection>.Filter;

                    var ticketcollection = _database.GetCollection<IOTicketCollection>(collectionName);
                    var filter = filterBuilder.In("ID", IDList) & filterBuilder.Eq("ClientUId", ClientUId) & filterBuilder.Eq("EndToEndUId", EndToEndUId);
                    List<IOTicketCollection> toBeUpdatedDoc = ticketcollection.Find(filter).ToList();
                    List<object> updatedID = new List<object>();
                    //getting records which have endtoendid as null
                    //var clientLevelList = ticketList.Where(x => x.EndToEndUId == null).ToList();
                    //Updating records based on certain conditions if ticket already exists
                    foreach (var oldDoc in toBeUpdatedDoc)
                    {
                        var newUpsertData = ticketList.FirstOrDefault(p => p.ID == oldDoc.ID && p.ClientUId == oldDoc.ClientUId && p.EndToEndUId == oldDoc.EndToEndUId && ((p.LastModifiedTime >= oldDoc.LastModifiedTime) || (p.LastModifiedTime == DateTime.MinValue && oldDoc.LastModifiedTime == DateTime.MinValue) || (p.ModifiedOn >= oldDoc.ModifiedOn)));
                        if (newUpsertData != null)
                        {
                            updatedID.Add(oldDoc.ID);
                            newUpsertData._id = oldDoc._id;
                            ticketcollection.ReplaceOne(c => c._id == oldDoc._id, newUpsertData);
                        }
                        // if lastmodified date is less than existing record in db
                        var lastmodifiedUpsertData = ticketList.FirstOrDefault(p => p.ID == oldDoc.ID && p.ClientUId == oldDoc.ClientUId && p.EndToEndUId == oldDoc.EndToEndUId && ((p.LastModifiedTime < oldDoc.LastModifiedTime) || (p.LastModifiedTime == DateTime.MinValue && oldDoc.LastModifiedTime > DateTime.MinValue) || (p.ModifiedOn < oldDoc.ModifiedOn)));
                        if (lastmodifiedUpsertData != null)
                        {
                            updatedID.Add(lastmodifiedUpsertData.ID);
                        }
                    }                    
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(PAMTicketPullService), nameof(PAMIOTicketDeltaPushByType), toBeUpdatedDoc.Count + " tickets updated in " + collectionName, Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
                    //remove those tickets from ticketentitycollection list which are updated 
                    ticketList.RemoveAll(x => updatedID.Contains(x.ID) && x.ClientUId == ClientUId && x.EndToEndUId == EndToEndUId);
                    List<IOTicketCollection> toBeInsertedDoc = ticketList.ToList();
                    //Insert if new documents
                    if (toBeInsertedDoc.Count > 0)
                    {
                        toBeInsertedDoc.ForEach(item => { item._id = ObjectId.GenerateNewId(); });
                        var document = BsonSerializer.Deserialize<List<IOTicketCollection>>(toBeInsertedDoc.ToJson());
                        ticketcollection.InsertMany(document.AsEnumerable());
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(PAMTicketPullService), nameof(PAMIOTicketDeltaPushByType), toBeInsertedDoc.Count + " tickets inserted in " + collectionName, Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty);                        
                    }
                }                
            }
            catch (Exception ex)
            {                
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(PAMTicketPullService), nameof(RemoveNullDeliveryConstructs), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                throw ex;
            }            
        }

        public static void RemoveNullDeliveryConstructs(List<PAMTicket> tickets)
        {
            try
            {
                if (tickets != null && tickets.Count > 0)
                {
                    tickets.RemoveAll(e => e == null);
                    tickets.ForEach(t =>
                    {
                        if (t != null)
                        {
                            if (t.DeliveryConstruct != null)
                            {
                                t.DeliveryConstruct.RemoveAll(d => d == null);
                            }
                        }
                    });
                }
            }
            catch (Exception ex)
            {                
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(PAMTicketPullService), nameof(RemoveNullDeliveryConstructs), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                throw ex;
            }
        }

        public static void RemoveNullDeliveryConstructsIO(List<IOTicketCollection> tickets)
        {
            try
            {
                if (tickets != null && tickets.Count > 0)
                {
                    tickets.RemoveAll(e => e == null);
                    tickets.ForEach(t =>
                    {
                        if (t != null)
                        {
                            if (t.DeliveryConstruct != null)
                            {
                                t.DeliveryConstruct.RemoveAll(d => d == null);
                            }
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(PAMTicketPullService), nameof(RemoveNullDeliveryConstructsIO), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                throw ex;
            }
        }

        public bool UpdatePAMHistoricalPullTracker(string ClientUId, string EndToEndUId, string entityType, string pullType, int flag, string statustext, int recordCount, DateTime? maxLastDFUpdatedDate)
        {
            try
            {                
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(PAMTicketPullService), nameof(UpdatePAMHistoricalPullTracker), "Inside PAMDemographicsDA_updatePAMHistoricalPullTracker method", Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
                var collection = _database.GetCollection<PAMHistoricalPullTracker>(CONSTANTS.DB_PAMHistoricalAndDeltaPullTracker);
                var filterBuilder = Builders<PAMHistoricalPullTracker>.Filter;
                var filter = FilterDefinition<PAMHistoricalPullTracker>.Empty; ;
                filter = filterBuilder.Eq("ClientUId", ClientUId) & filterBuilder.Eq("EndToEndUId", EndToEndUId) & filterBuilder.Eq("PullType", pullType) & filterBuilder.Eq("EntityType", entityType);
                var update = Builders<PAMHistoricalPullTracker>.Update.Set("Flag", flag).Set("Status", statustext).Set("ProcessingEndTime", DateTime.UtcNow).Set("RecordCount", recordCount).Set("dfLastUpdatedDate", maxLastDFUpdatedDate); //completed 
                var updateResult = collection.UpdateMany(filter, update);
            }
            catch (Exception ex)
            {                                
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(PAMTicketPullService), nameof(UpdatePAMHistoricalPullTracker), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return false;
            }

            return true;
        }

        public bool InsertPAMHistoricalPullFailedTracker(PAMHistoricalPullTracker tracker)
        {
            try
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(PAMTicketPullService), nameof(InsertPAMHistoricalPullFailedTracker), "START", Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty);                
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
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(PAMTicketPullService), nameof(InsertPAMHistoricalPullFailedTracker), "END", Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
            }
            catch (Exception ex)
            {                
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(PAMTicketPullService), nameof(InsertPAMHistoricalPullFailedTracker), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return false;
            }

            return true;
        }

        public void PAMTicketEntitiesDeltaPushByType(List<PAMTicket> ticketList, string collectionName, string ClientUId, string EndToEndUId)
        {            
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(PAMTicketPullService), nameof(PAMTicketEntitiesDeltaPushByType), "START", Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
            List<string> ticketIDList = ticketList.Select(p => p.ID).ToList();            
            try
            {
                if (ticketList != null && ticketList.Count > 0)
                {
                    //Removing duplicate records from list
                    ticketList = ticketList.GroupBy(x => new { x.ID, x.ClientUId, x.EndToEndUId }).Select(x => x.FirstOrDefault()).ToList();

                    #region check if Ticket Type is changed, delete from existing collection

                    var delfilterBuilder = Builders<PAMTicket>.Filter;
                    var uniqueTicketIds = ticketList.Select(p => p.ID).ToArray();
                    var delFilter = delfilterBuilder.In("ID", uniqueTicketIds) & delfilterBuilder.Eq("ClientUId", ClientUId) & delfilterBuilder.Eq("EndToEndUId", EndToEndUId);

                    //Remove Tickets from Problem Ticket If Exists with same Incident Number and Different Service Type
                    if (collectionName != CONSTANTS.DB_IncidentCollection)
                    {
                        var incidentcollection = _database.GetCollection<PAMTicket>(CONSTANTS.DB_IncidentCollection);
                        var filterDuplicateTickets = delfilterBuilder.In("ID", uniqueTicketIds);
                        List<string> toBeDelIncDoc = incidentcollection.Find(delFilter).ToList().Select(a => a.ID).ToList();

                        if (toBeDelIncDoc != null && toBeDelIncDoc.Count > 0)
                        {
                            var delDuplicFilter = delfilterBuilder.In("ID", toBeDelIncDoc) & delfilterBuilder.Eq("ClientUId", ClientUId) & delfilterBuilder.Eq("EndToEndUId", EndToEndUId);
                            incidentcollection.DeleteMany(delDuplicFilter);
                        }
                    }

                    if (collectionName != CONSTANTS.DB_ProblemCollection)
                    {
                        var problemcollection = _database.GetCollection<PAMTicket>(CONSTANTS.DB_ProblemCollection);
                        var filterDuplicateTickets = delfilterBuilder.In("ID", uniqueTicketIds) & delfilterBuilder.Eq("ClientUId", ClientUId) & delfilterBuilder.Eq("EndToEndUId", EndToEndUId);
                        List<string> toBeDelPRDoc = problemcollection.Find(delFilter).ToList().Select(a => a.ID).ToList();

                        if (toBeDelPRDoc != null && toBeDelPRDoc.Count > 0)
                        {
                            var delDuplicFilter = delfilterBuilder.In("ID", toBeDelPRDoc) & delfilterBuilder.Eq("ClientUId", ClientUId) & delfilterBuilder.Eq("EndToEndUId", EndToEndUId);
                            problemcollection.DeleteMany(delDuplicFilter);
                        }
                    }

                    if (collectionName != CONSTANTS.DB_ServiceRequestCollection)
                    {
                        var serviceRequestcollection = _database.GetCollection<PAMTicket>(CONSTANTS.DB_ServiceRequestCollection);
                        var filterDuplicateTickets = delfilterBuilder.In("ID", uniqueTicketIds) & delfilterBuilder.Eq("ClientUId", ClientUId) & delfilterBuilder.Eq("EndToEndUId", EndToEndUId);
                        List<string> toBeDelSRDoc = serviceRequestcollection.Find(delFilter).ToList().Select(a => a.ID).ToList();

                        if (toBeDelSRDoc != null && toBeDelSRDoc.Count > 0)
                        {
                            var delDuplicFilter = delfilterBuilder.In("ID", toBeDelSRDoc) & delfilterBuilder.Eq("ClientUId", ClientUId) & delfilterBuilder.Eq("EndToEndUId", EndToEndUId);
                            serviceRequestcollection.DeleteMany(delDuplicFilter);
                        }
                    }

                    if (collectionName != CONSTANTS.DB_ChangeManagementCollection)
                    {
                        var changeManagementCollection = _database.GetCollection<PAMTicket>(CONSTANTS.DB_ChangeManagementCollection);
                        var filterDuplicateTickets = delfilterBuilder.In("ID", uniqueTicketIds) & delfilterBuilder.Eq("ClientUId", ClientUId) & delfilterBuilder.Eq("EndToEndUId", EndToEndUId);
                        List<string> toBeDelSRDoc = changeManagementCollection.Find(delFilter).ToList().Select(a => a.ID).ToList();

                        if (toBeDelSRDoc != null && toBeDelSRDoc.Count > 0)
                        {
                            var delDuplicFilter = delfilterBuilder.In("ID", toBeDelSRDoc) & delfilterBuilder.Eq("ClientUId", ClientUId) & delfilterBuilder.Eq("EndToEndUId", EndToEndUId);
                            changeManagementCollection.DeleteMany(delDuplicFilter);
                        }
                    }

                    #endregion

                    #region check if Domain is changed, delete from existing collection
                    var delfilterBuilderDomain = Builders<IOTicketCollection>.Filter;
                    var uniqueTicketIdList = ticketList.Select(p => p.ID).ToArray();
                    var delFilterDomain = delfilterBuilderDomain.In("ID", uniqueTicketIdList) & delfilterBuilderDomain.Eq("ClientUId", ClientUId) & delfilterBuilderDomain.Eq("EndToEndUId", EndToEndUId);

                    //Remove Tickets from ioTicket collection If Exists with same Incident Number                
                    var ioTicketcollection = _database.GetCollection<IOTicketCollection>(CONSTANTS.DB_IOTicketCollection);
                    List<string> toBeDelIODoc = ioTicketcollection.Find(delFilterDomain).ToList().Select(a => a.ID).ToList();

                    if (toBeDelIODoc != null && toBeDelIODoc.Count > 0)
                    {
                        var delDuplicFilter = delfilterBuilderDomain.In("ID", toBeDelIODoc) & delfilterBuilderDomain.Eq("ClientUId", ClientUId) & delfilterBuilderDomain.Eq("EndToEndUId", EndToEndUId);
                        ioTicketcollection.DeleteMany(delDuplicFilter);
                    }
                    #endregion

                    var IDList = ticketList.Select(p => p.ID).ToArray();
                    var filterBuilder = Builders<PAMTicket>.Filter;

                    var ticketcollection = _database.GetCollection<PAMTicket>(collectionName);
                    var filter = filterBuilder.In("ID", IDList) & filterBuilder.Eq("ClientUId", ClientUId) & filterBuilder.Eq("EndToEndUId", EndToEndUId);
                    List<PAMTicket> toBeUpdatedDoc = ticketcollection.Find(filter).ToList();
                    List<object> updatedID = new List<object>();
                    //getting records which have endtoendid as null
                    //var clientLevelList = ticketList.Where(x => x.EndToEndUId == null).ToList();
                    //Updating records based on certain conditions if ticket already exists
                    foreach (var oldDoc in toBeUpdatedDoc)
                    {
                        var newUpsertData = ticketList.FirstOrDefault(p => p.ID == oldDoc.ID && p.ClientUId == oldDoc.ClientUId && p.EndToEndUId == oldDoc.EndToEndUId && ((p.LastModifiedTime >= oldDoc.LastModifiedTime) || (p.LastModifiedTime == DateTime.MinValue && oldDoc.LastModifiedTime == DateTime.MinValue) || (p.ModifiedOn >= oldDoc.ModifiedOn)));
                        if (newUpsertData != null)
                        {
                            updatedID.Add(oldDoc.ID);
                            newUpsertData._id = oldDoc._id;
                            ticketcollection.ReplaceOne(c => c._id == oldDoc._id, newUpsertData);
                        }
                        // if lastmodified date is less than existing record in db
                        var lastmodifiedUpsertData = ticketList.FirstOrDefault(p => p.ID == oldDoc.ID && p.ClientUId == oldDoc.ClientUId && p.EndToEndUId == oldDoc.EndToEndUId && ((p.LastModifiedTime < oldDoc.LastModifiedTime) || (p.LastModifiedTime == DateTime.MinValue && oldDoc.LastModifiedTime > DateTime.MinValue) || (p.ModifiedOn < oldDoc.ModifiedOn)));
                        if (lastmodifiedUpsertData != null)
                        {
                            updatedID.Add(lastmodifiedUpsertData.ID);
                        }
                    }                   
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(PAMTicketPullService), nameof(PAMTicketEntitiesDeltaPushByType), "UPDATE-" + toBeUpdatedDoc.Count + " tickets updated in " + collectionName, Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
                    //remove those tickets from ticketentitycollection list which are updated 
                    ticketList.RemoveAll(x => updatedID.Contains(x.ID) && x.ClientUId == ClientUId && x.EndToEndUId == EndToEndUId);
                    List<PAMTicket> toBeInsertedDoc = ticketList.ToList();
                    //Insert if new documents
                    if (toBeInsertedDoc.Count > 0)
                    {
                        toBeInsertedDoc.ForEach(item => { item._id = ObjectId.GenerateNewId(); });
                        var document = BsonSerializer.Deserialize<List<PAMTicket>>(toBeInsertedDoc.ToJson());
                        ticketcollection.InsertMany(document.AsEnumerable());                        
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(PAMTicketPullService), nameof(PAMTicketEntitiesDeltaPushByType), "INSERT-" + toBeInsertedDoc.Count + " tickets inserted in " + collectionName, Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
                    }
                }                
            }
            catch (Exception ex)
            {                
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(PAMTicketPullService), nameof(PAMTicketEntitiesDeltaPushByType), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                throw ex;
            }
        }    

        public AIAAMiddleLayerRequest CreatePAMDataProviderRequest(string clientUId, string entityType, string dataprovidertype, DateTime startDate, DateTime endDate)
        {
            AIAAMiddleLayerRequest MLRequest = new AIAAMiddleLayerRequest();
            MLRequest.SecurityProviders = null;
            MLRequest.ProjectStructureProviders = null;
            MLRequest.DataProviders = new List<Models.SaaS.DataProvider>();
            MLRequest.TicketTypes = new List<PAMTicketPullDateRange>();
            Models.DataProvider dataProvider = new Models.DataProvider();
            try
            {
                //Dictionary<string, string> userDetails = GetAuthTokenFromCookie();
                MLRequest.ProjectId = clientUId;
                MLRequest.UserId = ""; // "pravin.chandankhede@mywizard.com";
                                       // Shared.DataProviders dataProviders = (Shared.DataProviders)ConfigurationManager.GetSection("DataProviders");

                DP dataProviders = _dataProvider;

                if (dataprovidertype.Equals("Tickets"))
                {
                    dataProvider = dataProviders.DataProvider[2];
                }
                MLRequest.TicketType = dataProvider.TicketType;
                MLRequest.TicketPullType = dataProvider.TicketPullType;
                MLRequest.BatchSize = Convert.ToInt16(dataProvider.BatchSize);
                MLRequest.PageNumber = 0;
                MLRequest.DateTimeFormat = dataProvider.FilterDateFormat;
                MLRequest.TicketTypes.Add(new PAMTicketPullDateRange()
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    TicketType = entityType
                });
                MLRequest.DataProviders.Add(new Models.SaaS.DataProvider()
                {
                    Name = dataProvider.Name,
                    Accept = dataProvider.Accept,
                    DataFormatter = BuildDataFormatterObject(dataProvider.DataFormatter),
                    AuthProvider = BuildAuthproviderObject(dataProvider.AuthProvider, ""),
                    Method = dataProvider.Method,
                    MIMEMediaType = dataProvider.MIMEMediaType,
                    DataProviderTypeName = dataProvider.DataProviderTypeName,
                    ServiceUrl = dataProvider.ServiceUrl,
                    InputRequestType = dataProvider.InputRequestType,
                    InputRequestKeys = dataProvider.InputRequestKeys,
                    InputRequestValues = dataProvider.InputRequestValues,
                    JsonRootNode = dataProvider.JsonRootNode,
                    DefaultKeys = dataProvider.DefaultKeys,
                    DefaultValues = dataProvider.DefaultValues,
                    TicketType = dataProvider.TicketType,
                    TicketPullType = dataProvider.TicketPullType,
                    FilterDateFormat = dataProvider.FilterDateFormat,
                    BatchSize = Convert.ToInt16(dataProvider.BatchSize),
                    IntialBatchSize = Convert.ToInt16(dataProvider.IntialBatchSize)
                });
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return MLRequest;
        }

        public static Models.SaaS.DataFormatter BuildDataFormatterObject(Models.DataFormatter dataFormatter)
        {
            Models.SaaS.DataFormatter dFormatter = new Models.SaaS.DataFormatter();
            if (dataFormatter != null)
            {
                dFormatter.Json = "XPATH";
                dFormatter.Name = dataFormatter.Name;
                dFormatter.XsltFilePath = dataFormatter.XsltFilePath;
                dFormatter.DataFormatterTypeName = dataFormatter.DataFormatterTypeName;
                dFormatter.XsltArguments = dataFormatter.XsltArguments;
            }
            return dFormatter;
        }

        public  Models.SaaS.AuthProvider BuildAuthproviderObject(Models.AuthProvider authProvider, string appServiceUId)//, Dictionary<string, string> userDetails)
        {
            Models.SaaS.AuthProvider aProvider = new Models.SaaS.AuthProvider();
            var source = appSettings.ATRSource.ToString();
            if (authProvider != null)
            {
                aProvider.Name = authProvider.Name;
                aProvider.ClientId = authProvider.ClientId;
                aProvider.AuthProviderTypeName = authProvider.AuthProviderTypeName;
                aProvider.FederationUrl = authProvider.FederationUrl;
                aProvider.Secret = authProvider.Secret;
                aProvider.Scope = authProvider.Scope;
                aProvider.Resource = authProvider.Resource;
                aProvider.AppServiceUId = appServiceUId;
                aProvider.GrantType = authProvider.GrantType;
                if (source.Equals("Phoenix"))
                    aProvider.Token = GenerateTokenForServiceAccount();
                //if (userDetails != null && userDetails.Count > 0)
                //    aProvider.Token = userDetails["AuthToken"];

                try
                {
                    aProvider.Subject = authProvider.Subject;
                    aProvider.Issuer = authProvider.Issuer;
                    aProvider.Thumbprint = authProvider.Thumbprint;
                    aProvider.IsTLS12Enabled = authProvider.IsTLS12Enabled;
                    aProvider.CertType = authProvider.CertType;
                }
                catch
                {
                    throw;
                }
            }
            return aProvider;
        }

        public string GenerateTokenForServiceAccount()
        {
            #region Configuration Section
            // NameValueCollection _configuration = ConfigurationManager.GetSection("Phoenix.Configuration") as NameValueCollection;
            var authProviderType = appSettings.authProvider;
            var IsTokenGenerationTemporary = Convert.ToBoolean(appSettings.IsTokenGenerationTemporary);
            #endregion
            string token = string.Empty;
            string tokenendpointurl = string.Empty;
            try
            {
                if (authProviderType.ToLower() == "azuread")
                {
                    List<KeyValuePair<string, string>> pairs = new List<KeyValuePair<string, string>>();
                    if (IsTokenGenerationTemporary == false)
                    {
                        pairs = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string,string>("grant_type", appSettings.phoenix_credentials_granttype),
                    new KeyValuePair<string,string>("client_id", appSettings.phoenix_credentials_clientid),
                    new KeyValuePair<string,string> ("client_secret", appSettings.phoenix_credentials_clientsecret),
                    new KeyValuePair<string,string> ("resource", appSettings.phoenix_credentials_resource)
                };
                    }
                    else
                    {
                        pairs = new List<KeyValuePair<string, string>>
                        {
                     new KeyValuePair<string, string>("grant_type", appSettings.phoenix_credentials_granttypeforusertoken),
                    new KeyValuePair<string, string>("client_id", appSettings.phoenix_credentials_clientid),
                    new KeyValuePair<string, string>("client_secret", appSettings.phoenix_credentials_clientsecret),
                    new KeyValuePair<string, string>("resource", appSettings.phoenix_credentials_resource),
                    new KeyValuePair<string, string>("username", appSettings.phoenix_username),
                    new KeyValuePair<string, string>("password", appSettings.phoenix_password),
                    new KeyValuePair<string, string>("scope", appSettings.phoenix_credentials_scope)
                       };
                    }
                    tokenendpointurl = appSettings.phoenix_credentials_usertokenendpointurl;
                    var content = new FormUrlEncodedContent(pairs);
                    using (var client = new HttpClient())
                    {
                        var response = client.PostAsync(tokenendpointurl, content).Result;
                        var result = response.Content.ReadAsStringAsync().Result;
                        Dictionary<string, string> tokenDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(result);
                        if (tokenDictionary != null)
                        {
                            if (response.IsSuccessStatusCode)
                            {
                                token = tokenDictionary["access_token"].ToString();
                              //  Log.Information("Token generation is successful");
                            }
                            else
                            {
                                token = "";
                            }

                        }
                        else
                        {
                            token = "";
                        }

                    }
                }
                else
                {
                    tokenendpointurl = appSettings.phoenix_credentials_usertokenendpointurl;
                    using (var client = new HttpClient())
                    {
                        if (authProviderType == "Form")
                        {
                            var username = appSettings.phoenix_username;
                            var password = appSettings.phoenix_password;
                            client.DefaultRequestHeaders.Add("username", username);
                            client.DefaultRequestHeaders.Add("password", password);
                        }
                        var response = client.PostAsync(tokenendpointurl, null).Result;
                        var result = response.Content.ReadAsStringAsync().Result;
                        Dictionary<string, string> tokenDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(result);
                        if (tokenDictionary != null)
                        {
                            if (response.IsSuccessStatusCode)
                            {
                                token = tokenDictionary["access_token"].ToString();
                            }
                            else
                            {
                                token = "";
                            }

                        }
                        else
                        {
                            token = "";
                        }

                    }
                }
                return token;
            }
            catch (Exception ex)
            {
               // Log.Error("Error in GenerateTokenForServiceAccount" + ex.Message);
                return token;
            }
            finally
            {
               // Log.Information("Token generated:" + token);
            }

        }

        public static string Decrypt(string inputText)
        {            
            try
            {
                inputText = inputText.Replace(" ", "+").Replace("\"", "");
                byte[] stringBytes = Convert.FromBase64String(inputText);
                using (Aes IOMEncryptor = Aes.Create())
                {
                    Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] {
                    0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                    IOMEncryptor.Key = pdb.GetBytes(32);
                    IOMEncryptor.IV = pdb.GetBytes(16);
                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (CryptoStream cs = new CryptoStream(ms, IOMEncryptor.CreateDecryptor(), CryptoStreamMode.Write))
                        {
                            cs.Write(stringBytes, 0, stringBytes.Length);
                            cs.Close();
                        }
                        inputText = Encoding.Unicode.GetString(ms.ToArray());
                    }
                }
            }

            catch (FormatException ex)
            {
                inputText = "";
                throw ex;
            }
            return inputText;
        }

        public async Task<string> GetHistoricalTicketEntities(string middleLayerRequest)
        {
            var xDoc = new XDocument();
            var logFilePath = String.Empty;
            List<string> faults = new List<string>();
            Models.SaaS.AIAAMiddleLayerRequest request = new Models.SaaS.AIAAMiddleLayerRequest();
            Models.SaaS.AIAAMiddleLayerResponse middleLayerResponse = new Models.SaaS.AIAAMiddleLayerResponse();
            string response = string.Empty;
            if (!string.IsNullOrEmpty(middleLayerRequest))
            {
                try
                {
                   // Log.Information("before Deserialization of middleLayerRequest");
                    request = JsonConvert.DeserializeObject< Models.SaaS.AIAAMiddleLayerRequest >(middleLayerRequest);
                   // Log.Information("after Deserialization of middleLayerRequest");
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
            }
            else
                throw new Exception("The request is empty");
            try
            {
                #region Call data provider
                //logFilePath = HttpContext.Current.Server.MapPath(_logFilePath);
                if (request.DataProviders != null && request.DataProviders.Count > 0)
                {
                    //BusinessDomain.Entities.IDataProvider _dataProvider = kernal.Get<BusinessDomain.Entities.IDataProvider>(BusinessDomain.Entities.BusinessDomainEntityType.DataProvider.ToString());
                   
                    response = await ExecuteDataProvider(request, faults).ConfigureAwait(false);
                 
                    //if (xDoc != null && xDoc.DescendantNodes().Any())
                    //{
                    //    response = JsonConvert.SerializeXNode(xDoc);
                    //}

                }
                else
                {
                    throw new Exception("There is no vaid data providers found in GetHistoricalTicketEntities method - Home Controller");
                }
                //if (xDoc != null && xDoc.DescendantNodes().Any())
                //    middleLayerResponse.Response = JsonConvert.SerializeXNode(xDoc, Formatting.None, true);
                //else
                middleLayerResponse.Response = response;
                #endregion
            }
            catch (Exception ex)
            {
                faults.Add(ex.Message);
                middleLayerResponse.Error = String.Format("Exception while executing GetHistoricalTicketEntities flow and the Exception is {0}", GetFaultString(faults));
            }
            return middleLayerResponse != null ? JsonConvert.SerializeObject(middleLayerResponse) : string.Empty;
            //return response;
        }

        private string GetFaultString(List<string> faults)
        {
            StringBuilder sb = new StringBuilder();

            faults.ForEach((fault) =>
            {
                sb.AppendLine($"{fault}");
            });

            return sb.ToString();
        }
        public async Task<string> ExecuteDataProvider(AIAAMiddleLayerRequest request, List<string> faults)
        {            
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(PAMTicketPullService), nameof(ExecuteDataProvider), "START", Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
            string readResponse = string.Empty;
            if (request != null)
            {
                if (request.DataProviders != null)
                {
                    //DataProviders.IDataProviders dataProvider = this.NinjectKernel.Get<DataProviders.IDataProviders>(request.TicketPullType.ToString());
                    try
                    {
                        if (request.TicketType == "WorkItems")
                        {
                            readResponse = await _dataProviderservice.GetHistoricalAgileDetails(faults, request).ConfigureAwait(false);
                        }
                        else if (request.TicketType == "Iterations")
                        {
                            readResponse = await _dataProviderservice.GetHistoricalIterationDetails(faults, request).ConfigureAwait(false);
                        }
                        else if (request.TicketType == "WorkItemsFromENS")
                        {
                           readResponse = await _dataProviderservice.GetWorkItemsFromENS(faults, request).ConfigureAwait(false);
                        }
                        else if (request.TicketType == "IterationsFromENS")
                        {
                          readResponse = await _dataProviderservice.GetIterationsFromENS(faults, request).ConfigureAwait(false);
                        }
                        else if (request.TicketType == "Tickets")
                        {                       
                          readResponse = await _dataProviderservice.GetHistoricalTicketDetails(faults, request).ConfigureAwait(false);                         
                        }
                        else if (request.TicketType == "TestResults")
                        {
                           readResponse = await _dataProviderservice.GetHistoricalTestResultDetails(faults, request).ConfigureAwait(false);
                        }
                        else if (request.TicketType == "Deployment")
                        {
                           readResponse = await _dataProviderservice.GetHistoricalDeploymentDetails(faults, request).ConfigureAwait(false);
                        }

                        if (readResponse != null && (faults != null || faults.Count > 0))
                            faults.AddRange(faults);
                    }
                    catch (Exception ex)
                    {
                        LOGGING.LogManager.Logger.LogErrorMessage(typeof(PAMTicketPullService), nameof(ExecuteDataProvider), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                    }
                }
            }
            else
            {
                #region Add Validation Fault
                faults.Add("Invalid or Unsupported DataProvider ," +
                                                      String.Format(@"{0}: Event {1} has Invalid or unsupported DataProvider ""{1}""."
                                                                   , this.GetType()
                                                                   , "ExecuteDataProvider"));
                #endregion
            }
            return readResponse;
        }

    }
}
