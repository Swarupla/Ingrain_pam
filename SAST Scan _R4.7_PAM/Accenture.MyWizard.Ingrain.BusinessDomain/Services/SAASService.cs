#region Namespace References
using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
using Accenture.MyWizard.Ingrain.DataAccess;
using Accenture.MyWizard.Ingrain.DataModels.Models;
using Accenture.MyWizard.Shared.Helpers;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
#endregion

namespace Accenture.MyWizard.Ingrain.BusinessDomain.Services
{
    public class SAASService : ISAASService
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
        public SAASService(DatabaseProvider db, IOptions<IngrainAppSettings> settings)
        {
            databaseProvider = db;
            appSettings = settings;
            _mongoClient = databaseProvider.GetDatabaseConnection();
            var dataBaseName = MongoUrl.Create(appSettings.Value.connectionString).DatabaseName;
            _database = _mongoClient.GetDatabase(dataBaseName);
        }
        #endregion

        #region Methods
        public SAASProvisionResponse ProvisionSAAS(SAASProvisioningRequest request)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(SAASService), nameof(ProvisionSAAS), string.Empty, string.Empty, string.Empty,
                 string.Empty, string.Empty);
            bool PAMFDSInstance = false;
            SAASProvisionResponse response = new SAASProvisionResponse();
            SAASProvisionStatus provisionResponseStatus = new SAASProvisionStatus();
            response.ClientUId = request.ClientUID;
            response.DeliveryConstructUId = request.DeliveryConstructUID;
            response.MySaaSServiceOrderUId = request.OrderUId;
            response.InstanceType = request.InstanceType;
            response.ServiceUId = request.ServiceUId;
          // var editproposalUrl = string.Format(appSettings.Value.EditProposalUrl.ToString(), request.E2EUID, request.RequestorID);

            try
            {
                var SAASCollection = _database.GetCollection<SAASProvisionDetails>("SAASProvisionDetails").Find(new BsonDocument()).ToList().FindAll(c => c.ClientUID == request.ClientUID && c.E2EUID == request.E2EUID);
                var collection = _database.GetCollection<BsonDocument>("SAASProvisionDetails");
                //  var userinfocollection = _database.GetCollection<BsonDocument>("UserInfo");
                var ATRCollection = _database.GetCollection<ATRProvisionRequestDto>("CAMConfiguration").Find(new BsonDocument()).ToList().FindAll(c => c.E2EUId == request.E2EUID.ToString());
                if (!(SAASCollection.Count > 0))
                {
                    //if ((request.InstanceType.ToLower() == "file upload" || request.InstanceType.ToLower() == "fds") || (((request.InstanceType.ToLower() == "dedicated/pam") || (request.InstanceType.ToLower() == "container/pam")) && (ATRCollection.Count > 0)))
                    //{
                    if ((request.InstanceType.ToLower() == "file upload" || request.InstanceType.ToLower() == "fds") || (request.InstanceType.ToLower() == "dedicated/pam") || (request.InstanceType.ToLower() == "container/pam"))
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(SAASService), nameof(ProvisionSAAS), request.InstanceType.ToLower(), string.Empty, string.Empty,
                string.Empty, string.Empty);
                        var bson = new BsonDocument();
                        var userinfobson = new BsonDocument();
                        bson.Add("ClientUID", request.ClientUID);
                        bson.Add("DeliveryConstructUID", request.DeliveryConstructUID);
                        bson.Add("E2EUID", request.E2EUID);
                        bson.Add("OrderUId", request.OrderUId);
                        bson.Add("OrderItemUId", request.OrderItemUId);
                        bson.Add("RedirectCallbackURL", request.RedirectCallbackURL);
                        bson.Add("ErrorCallbackURL", request.ErrorCallbackURL);
                        bson.Add("UpdateStatusCallbackURL", request.UpdateStatusCallbackURL);
                        bson.Add("InstanceType", request.InstanceType);
                        bson.Add("ServiceUId", request.ServiceUId);
                        bson.Add("ServiceName", request.ServiceName);
                        bson.Add("SourceID", request.SourceID);
                        bson.Add("ClientName", request.ClientName);
                        bson.Add("E2EName", request.E2EName);
                        bson.Add("Market", request.Market);
                        bson.Add("MarketUnit", request.MarketUnit);
                        bson.Add("RequestorID", request.RequestorID);
                        bson.Add("WBSE", request.WBSE);
                        bson.Add("Region", request.Region);
                        bson.Add("ClientGroup", request.ClientGroup);
                        bson.Add("CorrelationUId", request.CorrelationUId);
                        //BsonDocument App = new BsonDocument();
                        BsonArray Applications = new BsonArray(request.Applications.Count);
                        if (request.Applications.Find(x => x.Name == "IngrAIn") != null)
                        {
                            BsonDocument doc = new BsonDocument { { "Name", request.Applications.Find(x => x.Name == "IngrAIn").Name }, { "ApplicationUId", request.Applications.Find(x => x.Name == "IngrAIn").ApplicationUId } };
                            Applications.Add(doc);
                            //provisonExtensions.Value = request.Applications.Find(x => x.Name == "IngrAIn").Name;
                            //provisonExtensions.Key = "ApplicationUId";
                        }
                        if (request.InstanceType.ToLower() == "dedicated/pam")
                        {
                            if (request.Applications.Find(x => x.Name == "Virtual Data Scientist") != null)
                            {
                                bson.Add("isVDSProvisioned", "Yes");
                                PAMFDSInstance = true;
                            }
                            else
                            {
                                bson.Add("isVDSProvisioned", "No");
                            }
                        }
                        else if (request.InstanceType.ToLower() == "fds")
                        {
                            if (request.Applications.Find(x => x.Name == "Virtual Data Scientist") != null)
                            {
                                bson.Add("isVDSProvisioned", "Yes");
                            }
                            else
                            {
                                bson.Add("isVDSProvisioned", "No");
                            }
                        }
                        else
                        {
                            if (request.ProvisionedApplications != null)
                            {
                                if (request.ProvisionedApplications.Find(x => x.Name == "Virtual Data Scientist") != null)
                                {
                                    bson.Add("isVDSProvisioned", "Yes");
                                }
                            }
                            else
                            {
                                bson.Add("isVDSProvisioned", "No");
                            }
                        }
                      

                        //for (int i = 0; i < request.Applications.Count; i++)
                        //{
                        //    BsonDocument doc = new BsonDocument { { "Name", request.Applications[i].Name }, { "ApplicationUId", request.Applications[i].ApplicationUId } };
                        //    Applications.Add(doc);
                        //}
                        bson.Add("Applications", Applications);
                        bson.Add("CreatedOn", DateTime.Now);
                        bson.Add("ModifiedOn", DateTime.Now);
                        //bson.Add("BillingInfo", 1);
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(SAASService), nameof(ProvisionSAAS), "before insert", string.Empty, string.Empty,
               string.Empty, string.Empty);
                        collection.InsertOne(bson);
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(SAASService), nameof(ProvisionSAAS), "after insert", string.Empty, string.Empty,
             string.Empty, string.Empty);
                        if (request.InstanceType.ToLower() != "fds" || PAMFDSInstance == true)
                        {
                            var colln = _database.GetCollection<SAASProvisionDetails>("SAASProvisionDetails").Find(c => c.E2EUID == request.E2EUID.ToString()).ToList();
                            response.InstanceTypeUID = colln.Find(c => c.ClientUID == request.ClientUID)._id.ToString();
                            //  response.InstanceName = request.InstanceType.ToLower() == "fileupload" ? "DEDICATED/MT" : request.InstanceType.ToUpper(); //NEED TO CONFIRM
                            response.InstanceName = request.InstanceType.ToUpper();
                            response.ProvisionStatusUId = SAASProvisionStatus.StatusCompleted;
                            response.StatusReason = SAASProvisionReason.StatusCompleted;
                            List<ProvisonExtensionsSAAS> provisonExtensions_saas = new List<ProvisonExtensionsSAAS>();
                            ProvisonExtensionsSAAS provisonExtensions = new ProvisonExtensionsSAAS();

                            if (request.Applications.Find(x => x.Name == "IngrAIn") != null)
                            {
                                provisonExtensions.Value = request.Applications.Find(x => x.Name == "IngrAIn").ApplicationUId;
                                provisonExtensions.Key = "ApplicationUId";
                            }
                            provisonExtensions_saas.Add(provisonExtensions);
                            response.ProvisonExtensions = provisonExtensions_saas;
                            if (request.InstanceType.ToLower() == "file upload" || request.InstanceType.ToLower() == "dedicated/pam" || request.InstanceType.ToLower() == "container/pam")
                                response.ProvisionedUrl = string.Format(appSettings.Value.ATRProvisionedUrl, request.ClientUID, request.DeliveryConstructUID, request.E2EUID);
                        }
                    }
                    else
                    {
                        if (request.InstanceType.ToLower() != "fds" || PAMFDSInstance == true)
                        {
                            response.ProvisionStatusUId = SAASProvisionStatus.StatusFailed;
                            response.StatusReason = SAASProvisionReason.ConfigurationMissing;
                        }
                        else
                        {
                            response.StatusReason = SAASProvisionReason.StatusFailed;
                        }
                    }

                }
                else
                {
                    if (request.InstanceType.ToLower() != "fds" || PAMFDSInstance == true)
                    {
                        List<ProvisonExtensionsSAAS> provisonExtensions_saas = new List<ProvisonExtensionsSAAS>();
                        ProvisonExtensionsSAAS provisonExtensions = new ProvisonExtensionsSAAS();
                        if (request.Applications.Find(x => x.Name == "IngrAIn") != null)
                        {
                            provisonExtensions.Value = request.Applications.Find(x => x.Name == "IngrAIn").ApplicationUId;
                            provisonExtensions.Key = "ApplicationUId";
                        }
                        response.InstanceName = request.InstanceType;
                        provisonExtensions_saas.Add(provisonExtensions);
                        response.ProvisonExtensions = provisonExtensions_saas;
                        //response.ProvisonExtensions.Add(provisonExtensions);
                        response.ProvisionStatusUId = SAASProvisionStatus.StatusRejected;
                        response.StatusReason = SAASProvisionReason.StatusRejected;
                        //if (request.InstanceType.ToLower() == "file upload" || request.InstanceType.ToLower() == "fds")
                        //    response.ProvisionedUrl = appSettings.Value.ATRProvisionedUrl;
                        if (request.InstanceType.ToLower() == "file upload" || request.InstanceType.ToLower() == "dedicated/pam" || request.InstanceType.ToLower() == "container/pam")
                            response.ProvisionedUrl = string.Format(appSettings.Value.ATRProvisionedUrl, request.ClientUID, request.DeliveryConstructUID, request.E2EUID);
                        // response.ProvisionedUrl = string.Join(",", response.ProvisionedUrl, editproposalUrl);
                    }
                    else
                    {
                        response.StatusReason = SAASProvisionReason.StatusRejected;
                    }
                }
            }
            catch (Exception ex)
            {
                #region Handle Exception
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(SAASService), nameof(ProvisionSAAS), ex.Message, string.Empty, string.Empty,
                string.Empty, string.Empty);
                #endregion Handle Exception

                LOGGING.LogManager.Logger.LogProcessInfo(typeof(SAASService), nameof(ProvisionSAAS), " SASProvisionResponse: " + response, string.Empty, string.Empty,
                string.Empty, string.Empty);

                response.ProvisionStatusUId = SAASProvisionStatus.StatusFailed;
                response.StatusReason = SAASProvisionReason.StatusFailed;
            }

            return response;
        }

        public HttpClient GetOAuthAsyncFds(string clientid, string clientsecret, string tokenendpoint, string scope, string scopes, 
            string isazureadenabled, string azureclientid, string azureclientsecret, string azuretokenendpoint, string azureresource)
        {
            string token = String.Empty;
            HttpClient authClient = new HttpClient();
            try
            {
                var task = Task.Run(
                    async () =>
                    {
                        token = await GetTokenAsyncV2Fds(clientid, clientsecret, tokenendpoint, scope, scopes, isazureadenabled, 
                            azureclientid, azureclientsecret, azuretokenendpoint, azureresource).ConfigureAwait(false);
                    });
                task.Wait();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }

            authClient = CreateHttpClientWithAuthorization(token);
            return authClient;
        }


        public static async Task<string> GetTokenAsyncV2Fds(string clientid, string clientsecret, string tokenendpoint, string scope, string scopes, string isazureadenabled,
                            string azureclientid, string azureclientsecret, string azuretokenendpoint, string azureresource)
        {
            
            //string token = GetTokenFromCache(system);
            try
            {
                string token = string.Empty;
                //ClientCredential credential = new ClientCredential(azureclientid, azureclientsecret);
                //AuthenticationContext authContext = new AuthenticationContext(azuretokenendpoint);
                //AuthenticationResult result = await authContext.AcquireTokenAsync(azureresource, credential);
                //return result != null ? result.AccessToken : null;      

                var client = new RestClient(azuretokenendpoint);
                var request = new RestRequest(Method.POST);
                request.AddHeader("cache-control", "no-cache");
                request.AddHeader("content-type", "application/x-www-form-urlencoded");
                request.AddParameter("application/x-www-form-urlencoded",
                   "grant_type=" + "client_credentials" +
                   "&client_id=" + azureclientid +
                   "&client_secret=" + azureclientsecret +
                   "&resource=" + azureresource,
                   ParameterType.RequestBody);
                IRestResponse response = client.Execute(request);
                var tokenObj = JsonConvert.DeserializeObject(response.Content) as dynamic;
                token = Convert.ToString(tokenObj.access_token);
                return token;

            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }
        }

        private static HttpClient CreateHttpClientWithAuthorization(string token)
        {
            HttpClient authClient = new HttpClient();

            authClient.DefaultRequestHeaders.Accept.Clear();
            authClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            authClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            authClient.Timeout = TimeSpan.FromMinutes(60);
            return authClient;
        }

        public string updateVDSProvisioning(VDSRequestPayload vDSRequestPayload)
        {
            string response = "";
            try
            {
                var collection = _database.GetCollection<SAASProvisionDetails>(CONSTANTS.DB_SAASProvisionDetails);
                var filterBuilder = Builders<SAASProvisionDetails>.Filter;
                var filter = filterBuilder.Eq("E2EUID", vDSRequestPayload.E2EUID) & filterBuilder.Eq("ClientUID", vDSRequestPayload.ClientUID)
                             & filterBuilder.Eq("DeliveryConstructUID", vDSRequestPayload.DeliveryConstructUID);
                var countResult =  collection.Find(filter).ToList();
                if (countResult.Count > 0)
                {
                    if (countResult[0].isVDSProvisioned == "No")
                    {
                        var update = Builders<SAASProvisionDetails>.Update.Set(x => x.isVDSProvisioned, "Yes");
                        collection.UpdateOne(filter, update);
                        response = "Success";
                    }
                    else
                    {
                        response = "VDS is already Provisioned";
                    }
                }
                else
                {
                    response = "E2EID is not Provisioned";
                }
                return response;

            }
            catch (Exception ex)
            {
                response = SAASProvisionReason.StatusFailed;
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(SAASProvisionDetails), nameof(updateVDSProvisioning), ex.Message + $" StackTrace = {ex.StackTrace}", ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return response;
            }
        }
        #endregion

    }
}
