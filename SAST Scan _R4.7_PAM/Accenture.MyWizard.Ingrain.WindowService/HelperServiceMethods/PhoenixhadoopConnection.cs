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
using Accenture.MyWizard.Ingrain.WindowService.Services;
using System.Net.Http;
using System.Net;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;

namespace Accenture.MyWizard.Ingrain.WindowService.HelperServiceMethods
{
    class PhoenixhadoopConnection
    {
        private readonly WINSERVICEMODELS.AppSettings appSettings = null;
        private IMongoDatabase _database;
        private DATAACCESS.DatabaseProvider databaseProvider;
        private readonly string SchedulerUserName = "SYSTEM";

        private DBConnection _DBConnection = null;
        private DBLoggerService _DatabaseLoggerService = null;
        private TokenService _TokenService = null;
        private HttpMethodService _HttpMethodService = null;

        public string IA_DefectRateUseCaseId { get; set; }
        public string IA_SPIUseCaseId { get; set; }
        public string IA_CRUseCaseId { get; set; }
        public string IA_UserStoryUseCaseId { get; set; }
        public string IA_RequirementUseCaseId { get; set; }
        public string IterationEntityUId { get; set; }
        public string CREntityUId { get; set; }
        public List<string> SSAIUseCaseIds { get; set; }
        public List<string> AIUseCaseIds { get; set; }
        private string _aesKey;
        private string _aesVector;

        private readonly string UserStoryEntityUId = "00020040-0200-0000-0000-000000000000";
        private readonly string RequirementEntityUId = "00020070-0700-0000-0000-000000000000";

        private readonly string _iterationQueryAPI = "Iterations/Query?clientUId={0}&deliveryConstructUId={1}&includeCompleteHierarchy=true";

        private readonly string _hadoopEntityApi = "/bi/V1/Entity/?ClientUId={0}&DeliveryConstructUId={1}";
        private readonly string _hadoopSPIApi = "/bi/ChangeImpactScheduleCalculationData";
        private readonly string _hadoopDefectRateApi = "/bi/ChangeImpactDefectCalculationData";

        public PhoenixhadoopConnection()
        {
            appSettings = AppSettingsJson.GetAppSettings().GetSection("AppSettings").Get<WINSERVICEMODELS.AppSettings>();
            databaseProvider = new DATAACCESS.DatabaseProvider();
            var dataBaseName = MongoUrl.Create(appSettings.connectionString).DatabaseName;
            MongoClient mongoClient = databaseProvider.GetDatabaseConnection();
            _database = mongoClient.GetDatabase(dataBaseName);
            SSAIUseCaseIds = appSettings.IA_SSAIUseCaseIds;
            SSAIUseCaseIds = appSettings.IA_SSAIUseCaseIds; // set usecaseids in order
            AIUseCaseIds = appSettings.IA_AIUseCaseIds;
            IterationEntityUId = appSettings.IterationEntityUId;
            CREntityUId = appSettings.CREntityUId;
            _aesKey = AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("aesKey").Value;
            _aesVector = AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("aesVector").Value;
            IA_DefectRateUseCaseId = string.IsNullOrEmpty(SSAIUseCaseIds.ElementAtOrDefault(0)) ? null : SSAIUseCaseIds[0];
            IA_SPIUseCaseId = string.IsNullOrEmpty(SSAIUseCaseIds.ElementAtOrDefault(1)) ? null : SSAIUseCaseIds[1];
            IA_CRUseCaseId = string.IsNullOrEmpty(AIUseCaseIds.ElementAtOrDefault(0)) ? null : AIUseCaseIds[0];
            IA_UserStoryUseCaseId = string.IsNullOrEmpty(AIUseCaseIds.ElementAtOrDefault(1)) ? null : AIUseCaseIds[1];
            IA_RequirementUseCaseId = string.IsNullOrEmpty(AIUseCaseIds.ElementAtOrDefault(2)) ? null : AIUseCaseIds[2];


            _DBConnection = new DBConnection();
            _DatabaseLoggerService = new DBLoggerService();
            _TokenService = new TokenService();
            _HttpMethodService = new HttpMethodService();
        }
        public WINSERVICEMODELS.AppDeliveryConstructs FetchClientsDeliveryConstructs(string ClientUID, string DeliveryConstructUID, string appserviceUId)
        {
            WINSERVICEMODELS.AppDeliveryConstructs appDeliveryConstructs = null;
            string routeUrl = "AppService?clientUId=" + ClientUID + "&deliveryConstructUId=" + DeliveryConstructUID + "&appServiceUId=" + appserviceUId + "&languageUId=null";
            string jsonResult = _HttpMethodService.InvokeGetMethod(routeUrl, appserviceUId);
            if (!string.IsNullOrEmpty(jsonResult))
            {
                appDeliveryConstructs = JsonConvert.DeserializeObject<WINSERVICEMODELS.AppDeliveryConstructs>(jsonResult);

            }
            return appDeliveryConstructs;
        }
        public WINSERVICEMODELS.ConstructsDTO FetchTeamAreaUID(string ClientUID, string DeliveryConstructUID, string AppserviceUID)
        {
            WINSERVICEMODELS.ConstructsDTO TeamAreaUIdAtDCLevel = null;
            try
            {
                string apiPath = "TeamsByDeliveryConstruct?clientUId=" + ClientUID + "&deliveryConstructUId=" + DeliveryConstructUID + "&includeCompleteHierarchy=Yes";
                var jsonStringResult = _HttpMethodService.InvokeGetMethod(apiPath, AppserviceUID);

                if (!string.IsNullOrEmpty(jsonStringResult))
                {
                    TeamAreaUIdAtDCLevel = JsonConvert.DeserializeObject<WINSERVICEMODELS.ConstructsDTO>(jsonStringResult);

                    if (TeamAreaUIdAtDCLevel != null && TeamAreaUIdAtDCLevel.DeliveryConstructs.Count > 0)
                    {

                        return TeamAreaUIdAtDCLevel;
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ConstraintsSchedularService), nameof(FetchTeamAreaUID), ex.Message, ex, "", string.Empty, ClientUID, DeliveryConstructUID);
            }
            return TeamAreaUIdAtDCLevel;
        }

       
        #region Phoenix Call - Sprint & Iterations Data
        public Dictionary<string, List<string>> FetchClosedSprintandIterations(string ClientUID, string DeliveryConstructUID, string ProvisionedAppserviceUID, string ServiceType)
        {
            Dictionary<string, List<string>> oIterationUID = null;
            List<string> sIterationUID = null;
            int count = 0;

            try
            {
                string result = string.Empty;
                result = InvokeIterationsQueryAPI(ClientUID, DeliveryConstructUID, 1); // endon shld be futuredata

                JObject response = JObject.Parse(result);

                WINSERVICEMODELS.IterationsArrayDTO oIterations = JsonConvert.DeserializeObject<WINSERVICEMODELS.IterationsArrayDTO>(response.ToString());

                sIterationUID = new List<string>();

                foreach (WINSERVICEMODELS.IterationsDTO oIteration in oIterations.Iterations)
                {
                    if (ServiceType == CONSTANTS.TrainingConstraint)
                    {
                        if (Convert.ToDateTime(oIteration.EndOn) < DateTime.Now)
                        {
                            // if endOn Date is Less than Today's Data that means the Iteration/Sprint = Closed
                            sIterationUID.Add(oIteration.IterationUId);
                            count += 1;
                        }
                    }
                    else if (ServiceType == CONSTANTS.ReTrainingConstraint)
                    {
                        if (Convert.ToDateTime(oIteration.EndOn) < DateTime.Now.AddDays(-1))
                        {
                            // if endOn Date is Less than Today's Data that means the Iteration/Sprint = Closed
                            sIterationUID.Add(oIteration.IterationUId);
                            count += 1;
                        }
                    }
                }

                if (sIterationUID.Count > 0)
                {
                    if (oIterationUID == null)
                    {
                        oIterationUID = new Dictionary<string, List<string>>();
                    }
                    oIterationUID.Add("IterationUID", sIterationUID);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ConstraintsSchedularService), nameof(FetchClosedSprintandIterations), ex.Message, ex, string.Empty, string.Empty, "ClientId : " + ClientUID, "DeliveryConstructUID : " + DeliveryConstructUID);
            }
            finally
            {
                sIterationUID = null;
            }
            return oIterationUID;
        }
        public void FetchIterationsList(DATAMODELS.DeployModelsDto item)
        {
            try
            {
                List<DATAMODELS.DeployModelsDto> deployModels = _DBConnection.CheckPredictConstraintModels(item);
                if (deployModels.Count > 0)
                {
                    var uniqueClientsDCs = deployModels.Select(x => new
                    {
                        x.ClientUId,
                        x.DeliveryConstructUID
                    }).Distinct().ToList();


                    if (uniqueClientsDCs.Count > 0)
                    {
                        foreach (var dc in uniqueClientsDCs)
                        {
                            string result = string.Empty;
                            WINSERVICEMODELS.PhoenixPayloads.IterationQueryResponse iterationQueryResponse = null;

                            result = InvokeIterationsQueryAPI(dc.ClientUId, dc.DeliveryConstructUID, 1); // endon shld be futuredata

                            if (!string.IsNullOrEmpty(result))
                            {
                                iterationQueryResponse = JsonConvert.DeserializeObject<WINSERVICEMODELS.PhoenixPayloads.IterationQueryResponse>(result);
                                _DBConnection.UpdateIterationstoDB(iterationQueryResponse, dc.ClientUId, dc.DeliveryConstructUID);
                                if (iterationQueryResponse.TotalPageCount > 1)
                                {
                                    for (int i = 2; i < iterationQueryResponse.TotalPageCount + 1; i++)
                                    {
                                        string result1 = InvokeIterationsQueryAPI(dc.ClientUId, dc.DeliveryConstructUID, i);
                                        if (!string.IsNullOrEmpty(result1))
                                        {
                                            WINSERVICEMODELS.PhoenixPayloads.IterationQueryResponse iterationQueryResponse1
                                            = JsonConvert.DeserializeObject<WINSERVICEMODELS.PhoenixPayloads.IterationQueryResponse>(result1);
                                            _DBConnection.UpdateIterationstoDB(iterationQueryResponse1, dc.ClientUId, dc.DeliveryConstructUID);
                                        }
                                        else
                                        {
                                            break;
                                        }

                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ConstraintsSchedularService), nameof(FetchIterationsList), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }

        }
        public string InvokeIterationsQueryAPI(string clientUId, string deliveryConstructUId, int pageNumber)
        {
            string jsonResult = null;
            string token = _TokenService.GenerateToken();
            string apiPath = String.Format(_iterationQueryAPI, clientUId, deliveryConstructUId);
            var postContent = new
            {
                ClientUId = clientUId,
                DeliveryConstructUId = deliveryConstructUId,
                PageNumber = pageNumber,
                BatchSize = 100
            };
            StringContent content = new StringContent(JsonConvert.SerializeObject(postContent), Encoding.UTF8, "application/json");
            HttpResponseMessage httpResponseMessage = _HttpMethodService.InvokePOSTRequest(token, appSettings.myWizardAPIUrl, apiPath, content, null);
            if (httpResponseMessage.StatusCode == HttpStatusCode.OK)
            {
                jsonResult = httpResponseMessage.Content.ReadAsStringAsync().Result;
            }
            else
            {
                // add logger

            }
            return jsonResult;
        }
        #endregion

        #region Hadoop Call - Closed User Stories
        public Dictionary<string, List<string>> FetchClosedUserStory(string ClientUID, string DeliveryConstructUID, string ProvisionedAppServiceUID)
        {
            string startDate = (DateTime.UtcNow.AddYears(-2)).ToString();
            string endDate = (DateTime.UtcNow).ToString();
            int count = 0;
            Dictionary<string, List<string>> oClosedUserStoryUID = null;
            List<string> sClosedUserStoryUID = null;
            try
            {
                var jsonResult = InvokeClosedUserStoryCall(ClientUID, DeliveryConstructUID, startDate, endDate, ProvisionedAppServiceUID, 1);

                if (jsonResult != null) {
                    if (!string.IsNullOrEmpty(jsonResult))
                    {
                        WINSERVICEMODELS.ClosedUserStoryDTO ClosedUserStoryQueryResponse = JsonConvert.DeserializeObject<WINSERVICEMODELS.ClosedUserStoryDTO>(jsonResult);

                        if (ClosedUserStoryQueryResponse.Entity != null)
                        {
                            sClosedUserStoryUID = new List<string>();
                            foreach (WINSERVICEMODELS.EntityDTO oEntity in ClosedUserStoryQueryResponse.Entity)
                            {
                                if (oEntity.stateuid.ToUpper() == "CLOSED")
                                {
                                    sClosedUserStoryUID.Add(oEntity.Iterationuid);
                                    count += 1;
                                }
                            }

                            if (ClosedUserStoryQueryResponse.TotalPageCount > 1)
                            {
                                for (int i = 2; i < ClosedUserStoryQueryResponse.TotalPageCount + 1; i++)
                                {
                                    jsonResult = InvokeClosedUserStoryCall(ClientUID, DeliveryConstructUID, startDate, endDate, ProvisionedAppServiceUID, i);
                                    if (jsonResult != null)
                                    {
                                        ClosedUserStoryQueryResponse = JsonConvert.DeserializeObject<WINSERVICEMODELS.ClosedUserStoryDTO>(jsonResult);

                                        if (ClosedUserStoryQueryResponse.Entity != null)
                                        {
                                            sClosedUserStoryUID = new List<string>();
                                            foreach (WINSERVICEMODELS.EntityDTO oEntity in ClosedUserStoryQueryResponse.Entity)
                                            {
                                                if (oEntity.stateuid.ToUpper() == "CLOSED")
                                                {
                                                    sClosedUserStoryUID.Add(oEntity.Iterationuid);
                                                    count += 1;
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            if (sClosedUserStoryUID != null && sClosedUserStoryUID.Count > 0)
                            {
                                oClosedUserStoryUID = new Dictionary<string, List<string>>();
                                oClosedUserStoryUID.Add("IterationUID", sClosedUserStoryUID);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ConstraintsSchedularService), nameof(FetchClosedUserStory), ex.Message, ex, string.Empty, string.Empty, "ClientId : " + ClientUID, "DeliveryConstructUID : " + DeliveryConstructUID);
            }
            return oClosedUserStoryUID;
        }
        public string InvokeClosedUserStoryCall(string ClientUID, string DeliveryConstructUID, string StartDate, string EndDate, string ProvisionedAppServiceUID, int currentPageNumber)
        {
            string jsonResult = null;
            var requestPayload = new
            {
                ClientUID = ClientUID,
                DeliveryConstructUId = DeliveryConstructUID,
                EntityUId = UserStoryEntityUId,
                ColumnList = "stateuid,iterationuididvalue,iterationuid",
                WorkItemTypeUId = "00020040020000100040000000000000",
                RowStatusUId = "00100000-0000-0000-0000-000000000000",
                PageNumber = currentPageNumber,
                TotalRecordCount = "5000",
                BatchSize = "5000",
                FromDate = StartDate,
                ToDate = EndDate
            };

            StringContent content = new StringContent(JsonConvert.SerializeObject(requestPayload), Encoding.UTF8, "application/json");
            string token = _TokenService.GenerateToken();

            Uri baseUri = new Uri(appSettings.myWizardAPIUrl);
            string host = baseUri.GetLeftPart(UriPartial.Authority);
            string apiPath = String.Format(_hadoopEntityApi, ClientUID, DeliveryConstructUID);
            var response = _HttpMethodService.InvokePOSTRequest(token, host, apiPath, content, null, ProvisionedAppServiceUID);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                jsonResult = response.Content.ReadAsStringAsync().Result;
            }
            return jsonResult;
        }
        #endregion

        #region Other hadoop Methods
        public bool CheckSPIData(string clientUId, string deliveryConstructUId, string startDate, string endDate)
        {
            bool haveSPIData = false;
            try
            {
                var requestPayload = new
                {
                    ClientUID = clientUId,
                    DeliveryConstructUId = new List<string>() { deliveryConstructUId },
                    Measure_metrics = new List<string>() { "SPI", "EV", "PV", "EDV", "AD121", "ReleaseUId", "processedondate", "ModifiedOn", "complexityuid" },
                    PageNumber = 1,
                    TotalRecordCount = 0,
                    BatchSize = 20,
                    StartDate = startDate,
                    EndDate = endDate
                };

                StringContent content = new StringContent(JsonConvert.SerializeObject(requestPayload), Encoding.UTF8, "application/json");
                string token = _TokenService.GenerateToken();

                Uri baseUri = new Uri(appSettings.myWizardAPIUrl);
                string host = baseUri.GetLeftPart(UriPartial.Authority);
                var response = _HttpMethodService.InvokePOSTRequest(token, host, _hadoopSPIApi, content, null);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var jsonResult = response.Content.ReadAsStringAsync().Result;
                    JObject result = JObject.Parse(jsonResult);
                    if (result.ContainsKey("TotalRecordCount"))
                    {
                        int recCount = Convert.ToInt32(result["TotalRecordCount"].ToString());
                        if (recCount > 20)
                        {
                            haveSPIData = true;
                        }
                        else
                        {
                            _DatabaseLoggerService.LogInfoMessageToDB(clientUId, deliveryConstructUId, IA_SPIUseCaseId, IterationEntityUId, null, "Training", "Total Record count in SPI API is -" + recCount, "");
                        }
                    }
                }
                else
                {
                    _DatabaseLoggerService.LogErrorMessageToDB(clientUId, deliveryConstructUId, IA_SPIUseCaseId, IterationEntityUId, null, "Training", "Hadoop SPI API returned-" + response.ReasonPhrase + "-" + response.Content.ReadAsStringAsync().Result, "E");
                }
            }
            catch (Exception ex)
            {
                _DatabaseLoggerService.LogErrorMessageToDB(clientUId, deliveryConstructUId, IA_SPIUseCaseId, IterationEntityUId, null, "Training", "Exception checking SPI Data-" + ex.Message, "E");
            }
            return haveSPIData;
        }
        public bool CheckDefectRateDate(string clientUId, string deliveryConstructUId, string startDate, string endDate)
        {
            bool haveDefectRateData = false;
            try
            {
                var requestPayload = new
                {
                    ClientUID = clientUId,
                    DeliveryConstructUId = new List<string>() { deliveryConstructUId },
                    Measure_metrics = new List<string>() { "DR", "EV", "PV", "AD033", "AD149", "AD058", "ReleaseUId", "processedondate", "complexityuid", "modifiedon", "clientuid" },
                    PageNumber = 1,
                    TotalRecordCount = 0,
                    BatchSize = 20,
                    StartDate = startDate,
                    EndDate = endDate
                };

                StringContent content = new StringContent(JsonConvert.SerializeObject(requestPayload), Encoding.UTF8, "application/json");
                string token = _TokenService.GenerateToken();

                Uri baseUri = new Uri(appSettings.myWizardAPIUrl);
                string host = baseUri.GetLeftPart(UriPartial.Authority);
                var response = _HttpMethodService.InvokePOSTRequest(token, host, _hadoopDefectRateApi, content, null);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var jsonResult = response.Content.ReadAsStringAsync().Result;
                    JObject result = JObject.Parse(jsonResult);
                    if (result.ContainsKey("TotalRecordCount"))
                    {
                        int recCount = Convert.ToInt32(result["TotalRecordCount"].ToString());
                        if (recCount > 20)
                        {
                            haveDefectRateData = true;
                        }
                        else
                        {
                            _DatabaseLoggerService.LogInfoMessageToDB(clientUId, deliveryConstructUId, IA_DefectRateUseCaseId, IterationEntityUId, null, "Training", "Total Record count in Defect Rate API is -" + recCount, "");
                        }
                    }
                }
                else
                {
                    _DatabaseLoggerService.LogErrorMessageToDB(clientUId, deliveryConstructUId, IA_DefectRateUseCaseId, IterationEntityUId, null, "Training", "Hadoop DefectRate API returned-" + response.ReasonPhrase + "-" + response.Content.ReadAsStringAsync().Result, "E");
                }
            }
            catch (Exception ex)
            {
                _DatabaseLoggerService.LogErrorMessageToDB(clientUId, deliveryConstructUId, IA_DefectRateUseCaseId, IterationEntityUId, null, "Training", "Exception checking DefectRate Data-" + ex.Message, "E");
            }
            return haveDefectRateData;
        }
        public bool CheckCRData(string clientUId, string deliveryConstructUId, string startDate, string endDate)
        {
            bool haveCRData = false;
            try
            {
                var requestPayload = new
                {
                    ClientUID = clientUId,
                    DeliveryConstructUId = deliveryConstructUId,
                    EntityUId = CREntityUId,
                    ColumnList = "comments,clientuid,createdbyproductinstanceuid,modifiedon,rowstatusuid,createdbyuser,actualcost,owner,reference,createdbyapp,description,typeuid,createdatsourcebyuser,delegatedto,stateuid,priorityuid,committedeffort,details,modifiedbyuser,title,createdon,requestor,severityuid,committedcost,changerequestexternalid,modifiedbyapp,createdatsourceon,rowversion,changerequestid,modifiedatsourcebyuser,changerequestuid,benefits,reasonforchangerequest,externalid,nextapprover,impactonbusiness,requirementid,requestowner,approverlist,stateexternalid,resourceemailaddress,plannedenddate,errorcallbacklink,changerequestextensions,changerequestassociations,changerequestdeliveryconstructs,deliveryconstructuid,TeamAreaExternalId,TeamAreaName",
                    RowStatusUId = "00100000-0000-0000-0000-000000000000",
                    PageNumber = "1",
                    TotalRecordCount = "0",
                    BatchSize = "20",
                    FromDate = startDate,
                    ToDate = endDate
                };

                StringContent content = new StringContent(JsonConvert.SerializeObject(requestPayload), Encoding.UTF8, "application/json");
                string token = _TokenService.GenerateToken();

                Uri baseUri = new Uri(appSettings.myWizardAPIUrl);
                string host = baseUri.GetLeftPart(UriPartial.Authority);
                string apiPath = String.Format(_hadoopEntityApi, clientUId, deliveryConstructUId);
                var response = _HttpMethodService.InvokePOSTRequest(token, host, apiPath, content, null);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var jsonResult = response.Content.ReadAsStringAsync().Result;
                    JObject result = JObject.Parse(jsonResult);
                    if (result.ContainsKey("TotalRecordCount"))
                    {
                        int recCount = Convert.ToInt32(result["TotalRecordCount"].ToString());
                        if (recCount > 20)
                        {
                            haveCRData = true;
                        }
                        else
                        {
                            _DatabaseLoggerService.LogInfoMessageToDB(clientUId, deliveryConstructUId, null, CREntityUId, null, "Training", "Total Record count in CR API is -" + recCount, "");
                        }
                    }
                }
                else
                {
                    _DatabaseLoggerService.LogErrorMessageToDB(clientUId, deliveryConstructUId, null, CREntityUId, null, "Training", "Hadoop CR API returned-" + response.ReasonPhrase + "-" + response.Content.ReadAsStringAsync().Result, "E");
                }
            }
            catch (Exception ex)
            {
                _DatabaseLoggerService.LogErrorMessageToDB(clientUId, deliveryConstructUId, null, CREntityUId, null, "Training", "Exception checking CR Data-" + ex.Message, "E");
            }
            return haveCRData;



        }
        public bool CheckUserStoryData(string clientUId, string deliveryConstructUId, string startDate, string endDate)
        {
            bool haveUserStoryData = false;
            try
            {
                var requestPayload = new
                {
                    ClientUID = clientUId,
                    DeliveryConstructUId = deliveryConstructUId,
                    EntityUId = UserStoryEntityUId,
                    ColumnList = "costofdelay,AssignedAtSourceToUser,iterationuidexternalvalue,currencyuididvalue,storypointestimated,businesscriticality,risk,starton,releaseuididvalue,stateuid,categoryuid,identifiedby,teamarea,storypointcompleted,iterationuididvalue,project,probabilityuididvalue,reference,EffortCompleted,summary,completedon,typeuid,valuearea,stateuididvalue,iterationuid,targetstarton,priorityuid,priorityuididvalue,severityuididvalue,comments,severityuid,effortestimated,businessvalue,statereason,releaseuidexternalvalue,riskreduction,assignedatsourceuser,description,effortremaining,currencyuid,targetendon,title,acceptancecriteria,teamareauid,commentsfieldvalue,identifiedon,probabilityuid,workitemuid,workitemexternalid,createdon,modifiedon,createdbyproductinstanceuid,workitemassociations",
                    WorkItemTypeUId = "00020040020000100040000000000000",
                    RowStatusUId = "00100000-0000-0000-0000-000000000000",
                    PageNumber = "1",
                    TotalRecordCount = "0",
                    BatchSize = "20",
                    FromDate = startDate,
                    ToDate = endDate
                };

                StringContent content = new StringContent(JsonConvert.SerializeObject(requestPayload), Encoding.UTF8, "application/json");
                string token = _TokenService.GenerateToken();

                Uri baseUri = new Uri(appSettings.myWizardAPIUrl);
                string host = baseUri.GetLeftPart(UriPartial.Authority);
                string apiPath = String.Format(_hadoopEntityApi, clientUId, deliveryConstructUId);
                var response = _HttpMethodService.InvokePOSTRequest(token, host, apiPath, content, null);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var jsonResult = response.Content.ReadAsStringAsync().Result;
                    JObject result = JObject.Parse(jsonResult);
                    if (result.ContainsKey("TotalRecordCount"))
                    {
                        int recCount = Convert.ToInt32(result["TotalRecordCount"].ToString());
                        if (recCount > 20)
                        {
                            haveUserStoryData = true;
                        }
                        else
                        {
                            _DatabaseLoggerService.LogInfoMessageToDB(clientUId, deliveryConstructUId, null, UserStoryEntityUId, null, "Training", "Total Record count in userstory API is -" + recCount, "");
                        }
                    }
                }
                else
                {
                    _DatabaseLoggerService.LogErrorMessageToDB(clientUId, deliveryConstructUId, null, UserStoryEntityUId, null, "Training", "Hadoop Userstory API returned-" + response.ReasonPhrase + "-" + response.Content.ReadAsStringAsync().Result, "E");
                }
            }
            catch (Exception ex)
            {
                _DatabaseLoggerService.LogErrorMessageToDB(clientUId, deliveryConstructUId, null, UserStoryEntityUId, null, "Training", "Exception checking userstory Data-" + ex.Message, "E");
            }
            return haveUserStoryData;

        }
        public bool CheckRequirementData(string clientUId, string deliveryConstructUId, string startDate, string endDate)
        {
            bool haveRequirementData = false;
            try
            {
                var requestPayload = new
                {
                    ClientUID = clientUId,
                    DeliveryConstructUId = deliveryConstructUId,
                    EntityUId = RequirementEntityUId,
                    ColumnList = "clientuid,actualendon,actualstarton,ascore,assignedatsourcetouser,assignedtoresourceuid,assignedtouser,businessvalue,comments,complexityuid,createdatsourcebyuser,createdatsourceon,createdbyapp,createdbyproductinstanceuid,createdbyuser,createdon,delegatedtoatsource,description,effortestimated,escore,externalreviewer,externalrevieweratsource,forecastendon,forecaststarton,internalreviewer,internalrevieweratsource,iscore,modifiedatsourcebyuser,modifiedatsourceon,modifiedbyapp,modifiedbyuser,modifiedon,nscore,project,qualityscore,releaseuid,requirementexternalid,requirementid,requirementtypeuid,requirementuid,riskreduction,sscore,stateuid,title,tscore,vscore,wsjf,rowstatusuid,phaseuid,workstream,commentsfieldvalue,impactedvalue,assignedtouserfieldvalue,businessvaluefieldvalue,complexityuidfieldvalue,riskreductionfieldvalue,priorityuid,qualityscorefieldvalue,identifiedon,requestowner,qscore,reference,actualstartonfieldvalue,details,escalationlevel,actualendonfieldvalue,resourceemailaddress,effortestimatedfieldvalue,externalreviewerfieldvalue,internalreviewerfieldvalue,identifiedby,requirementassociations,requirementextensions,deliveryconstructuid",
                    RowStatusUId = "00100000-0000-0000-0000-000000000000",
                    PageNumber = "1",
                    TotalRecordCount = "0",
                    BatchSize = "20",
                    FromDate = startDate,
                    ToDate = endDate
                };

                StringContent content = new StringContent(JsonConvert.SerializeObject(requestPayload), Encoding.UTF8, "application/json");
                string token = _TokenService.GenerateToken();

                Uri baseUri = new Uri(appSettings.myWizardAPIUrl);
                string host = baseUri.GetLeftPart(UriPartial.Authority);
                string apiPath = String.Format(_hadoopEntityApi, clientUId, deliveryConstructUId);
                var response = _HttpMethodService.InvokePOSTRequest(token, host, apiPath, content, null);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var jsonResult = response.Content.ReadAsStringAsync().Result;
                    JObject result = JObject.Parse(jsonResult);
                    if (result.ContainsKey("TotalRecordCount"))
                    {
                        int recCount = Convert.ToInt32(result["TotalRecordCount"].ToString());
                        if (recCount > 20)
                        {
                            haveRequirementData = true;
                        }
                        else
                        {
                            _DatabaseLoggerService.LogInfoMessageToDB(clientUId, deliveryConstructUId, null, RequirementEntityUId, null, "Training", "Total Record count in requirement API is -" + recCount, "");
                        }
                    }
                }
                else
                {
                    _DatabaseLoggerService.LogErrorMessageToDB(clientUId, deliveryConstructUId, null, RequirementEntityUId, null, "Training", "Hadoop requirement API returned-" + response.ReasonPhrase + "-" + response.Content.ReadAsStringAsync().Result, "E");
                }
            }
            catch (Exception ex)
            {
                _DatabaseLoggerService.LogErrorMessageToDB(clientUId, deliveryConstructUId, null, RequirementEntityUId, null, "Training", "Exception checking requirement Data-" + ex.Message, "E");
            }
            return haveRequirementData;
        }
        #endregion

        public string GetValidClientId()
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ConstraintsSchedularService), nameof(FetchClosedSprintandIterations), "FETCH CLIENTS START", "", "", "", "");
            string clientUId = null;
            string routeUrl = "AccountClients?clientUId=" + appSettings.ClientUID + "&deliveryConstructUId=null";

            var jsonStringResult = _HttpMethodService.InvokeGetMethod(routeUrl, appSettings.AppServiceUId);

            if (!string.IsNullOrEmpty(jsonStringResult))
            {
                List<WINSERVICEMODELS.ClientDetails> clientDetails = JsonConvert.DeserializeObject<List<WINSERVICEMODELS.ClientDetails>>(jsonStringResult);

                if (clientDetails.Count > 0)
                {
                    clientUId = clientDetails[0].ClientUId;
                }
            }
            return clientUId;
        }

    }
}
