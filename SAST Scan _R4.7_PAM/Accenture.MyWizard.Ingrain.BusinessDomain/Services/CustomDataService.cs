#region Namespace References
using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
using Accenture.MyWizard.Ingrain.DataAccess;
using Accenture.MyWizard.Ingrain.DataModels.Models;
using Accenture.MyWizard.Shared.Helpers;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Linq;
using System.IO;
using Accenture.MyWizard.Cryptography.EncryptionProviders;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using System.Text;
using RestSharp;
using System.Collections;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
#endregion

namespace Accenture.MyWizard.Ingrain.BusinessDomain.Services
{
    public class CustomDataService : ICustomDataService
    {
        #region Members
        private MongoClient _mongoClient;
        private IMongoDatabase _database;
        private readonly IOptions<IngrainAppSettings> appSettings;
        DatabaseProvider databaseProvider;
        private IEncryptionDecryption _encryptionDecryption;
        private IScopeSelectorService _ScopeSelector;
        //for Phoenix DB connection
        private MongoClient _PhoenixmongoClient;
        private IMongoDatabase _Phoenixdatabase;
        PhoenixConnection PhoenixdatabaseProvider;
        #endregion

        #region Constructors
        /// <summary>
        /// Constructor to Initialize mongoDB Connection
        /// </summary>
        public CustomDataService(DatabaseProvider db, PhoenixConnection Pdb, IOptions<IngrainAppSettings> settings, IServiceProvider serviceProvider)
        {
            databaseProvider = db;
            appSettings = settings;
            _mongoClient = databaseProvider.GetDatabaseConnection();
            var dataBaseName = MongoUrl.Create(appSettings.Value.connectionString).DatabaseName;
            _database = _mongoClient.GetDatabase(dataBaseName);
            _encryptionDecryption = serviceProvider.GetService<IEncryptionDecryption>();
            _ScopeSelector = serviceProvider.GetService<IScopeSelectorService>();
            //for Phoenix DB connection
            if (appSettings.Value.Environment == "PAD")
                _Phoenixdatabase = _mongoClient.GetDatabase(appSettings.Value.PhoenixDBName);
        }
        #endregion
        #region Members
        public object TestQueryData(string clientUID, string deliveryUID, string userId, HttpContext httpContext, string category, string ServiceLevel, out bool isError)
        {
            int RecsLimit = 1;
            if (ServiceLevel == "SSAI")
            {
                RecsLimit = Convert.ToInt32(appSettings.Value.SSAIRecsLimit);
            }
            else if (ServiceLevel == "AI")
            {
                RecsLimit = Convert.ToInt32(appSettings.Value.AIRecsLimit);
            }
            isError = false;
            object JResult = new object();
            JObject ErrorResponse = new JObject();
            BsonArray ResponseArray = new BsonArray();
            bool encryptDB = appSettings.Value.isForAllData;
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CustomDataService), nameof(TestQueryData), CONSTANTS.START, string.Empty, string.Empty, string.Empty, string.Empty);
            IFormCollection collection = httpContext.Request.Form;
            var Query = Convert.ToString(collection["CustomDataPull"]);
            List<Datecolumnlst> Columns = new List<Datecolumnlst>();
            List<string> UUIDlst = new List<string>();
            List<string> Objectlist = new List<string>();
            try
            {
                string[] Allowedkeywords = new string[] { " cursor ", " cursor", "cursor:", "cursor ", @"""cursor""", "'cursor'" };
                if (!(Allowedkeywords.Any(a => Query.IndexOf(a, StringComparison.InvariantCultureIgnoreCase) >= 0)))
                {
                    isError = true;
                    ErrorResponse["message"] = @"Query must contain the ""cursor"" key. Please validate the query.";
                    JResult = ErrorResponse;
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(CustomDataService), nameof(TestQueryData), CONSTANTS.END,
                    default(Guid), string.Empty, string.Empty, clientUID, deliveryUID);
                    return JResult;
                }
                string[] Notallowedkeys = new string[] { " cursor ", " cursor", "cursor " };
                if (Notallowedkeys.Any(a => Query.IndexOf(a, StringComparison.InvariantCultureIgnoreCase) >= 0))
                {
                    isError = true;
                    ErrorResponse["message"] = @"""cursor"" key is not in correct format. Please validate the query.";
                    JResult = ErrorResponse;
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(CustomDataService), nameof(TestQueryData), CONSTANTS.END,
                    default(Guid), string.Empty, string.Empty, clientUID, deliveryUID);
                    return JResult;
                }
                //Query = AppendQueryLimit(Query);
                if (ValidateQuery(Query))
                {
                    var queryNew = Query.Replace("\"", "'");
                    var cmd = new JsonCommand<BsonDocument>(queryNew);
                    BsonDocument result;
                    try
                    {
                        result = _Phoenixdatabase.RunCommand<BsonDocument>(cmd);//Phoenix DB
                    }
                    catch (Exception ex)
                    {
                        isError = true;
                        ErrorResponse["message"] = "Syntax Error (" + ex.Message + ") has been found in the Query. Please validate the query";
                        JResult = ErrorResponse;
                        LOGGING.LogManager.Logger.LogErrorMessage(typeof(CustomDataService), nameof(TestQueryData), ex.Message + $"   StackTrace = " + ex.StackTrace, ex, string.Empty, string.Empty, clientUID, deliveryUID);
                        return JResult;
                    }
                    if (result["ok"] == 1)
                    {
                        var finalResults = result["cursor"]["firstBatch"].AsBsonArray;
                        if (finalResults != null && finalResults.Count != 0)
                        {
                            if (finalResults.Count >= Convert.ToInt32(RecsLimit))
                            {
                                int FetchCount = finalResults.Count < Convert.ToInt32(appSettings.Value.CustomQueryLimit) ? finalResults.Count : Convert.ToInt32(appSettings.Value.CustomQueryLimit);
                                for (var i = 0; i < FetchCount; i++)
                                {
                                    BsonDocument result1 = finalResults[i].AsBsonDocument;
                                    if (i == 0)
                                    {
                                        foreach (var elements in result1.Elements)
                                        {
                                            if (elements.Value.IsBsonDateTime || elements.Value.IsValidDateTime)
                                            {
                                                Columns.Add(new Datecolumnlst { Name = elements.Name, Type = Convert.ToString(elements.Value.BsonType) });
                                            }
                                            else if (elements.Value.IsBsonDocument)
                                            {
                                                var ele = result1[elements.Name].AsBsonDocument.Elements;
                                                if (ele.Where(a => a.Name == "DateTime").Count() > 0)
                                                {
                                                    Columns.Add(new Datecolumnlst { Name = elements.Name, Type = Convert.ToString(MongoDB.Bson.BsonType.DateTime) });
                                                }
                                                else
                                                {
                                                    Objectlist.Add(elements.Name);
                                                }
                                            }
                                            else if (elements.Value.IsBsonArray || (elements.Value.IsBsonBinaryData && !elements.Value.IsGuid))
                                            {
                                                Objectlist.Add(elements.Name);
                                            }
                                            //else if (elements.Value.IsGuid)
                                            //{
                                            //    UUIDlst.Add(elements.Name);
                                            //}
                                        }
                                        if (Columns.Count <= 0)
                                        {
                                            isError = true;
                                            ErrorResponse["message"] = "Please re-visit the query. Date columns are missing. Atleast one date column is required in the data to proceed for Training, Retraining & Prediction.";
                                            JResult = ErrorResponse;
                                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CustomDataService), nameof(TestQueryData), CONSTANTS.END,
                                            default(Guid), string.Empty, string.Empty, clientUID, deliveryUID);
                                            return JResult;
                                        }
                                        if (Objectlist.Count > 0)
                                        {
                                            isError = true;
                                            ErrorResponse["message"] = "Query fetching column(s) having unsupported data type: " + Convert.ToString(Objectlist.ToJson()) + ", Please re-visit the query.";
                                            JResult = ErrorResponse;
                                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CustomDataService), nameof(TestQueryData), CONSTANTS.END,
                                            default(Guid), string.Empty, string.Empty, clientUID, deliveryUID);
                                            return JResult;
                                        }
                                    }
                                    foreach (var element in Columns)
                                    {
                                        if (Convert.ToString(element.Type) == Convert.ToString(MongoDB.Bson.BsonType.DateTime))
                                        {
                                            var ele = result1[element.Name].AsBsonDocument.Elements;
                                            foreach (var item in ele)
                                            {
                                                if (item.Name == "DateTime")
                                                {
                                                    result1[element.Name] = item.Value;
                                                }
                                            }
                                        }
                                    }
                                    ResponseArray.Add(result1);
                                }
                                object Final = JsonConvert.SerializeObject(BsonTypeMapper.MapToDotNetValue(ResponseArray));
                                JObject FinalResult = new JObject();
                                FinalResult["DateColumns"] = JsonConvert.DeserializeObject(Columns.ToJson().ToString()) as dynamic;
                                //FinalResult["UUIDColumns"] = JsonConvert.DeserializeObject(Convert.ToString(UUIDlst.ToJson())) as dynamic;
                                FinalResult["Result"] = JsonConvert.DeserializeObject(Final.ToString()) as dynamic;
                                JResult = FinalResult;
                            }
                            else
                            {
                                isError = true;
                                if (ServiceLevel == "SSAI")
                                {
                                    ErrorResponse["message"] = "The min limit on data pull is 4 records. Please validate the query.";
                                }
                                else if (ServiceLevel == "AI")
                                {
                                    ErrorResponse["message"] = "The min limit on data pull is 20 records. Please validate the query.";
                                }
                                JResult = ErrorResponse;
                            }
                        }
                        else
                        {
                            isError = true;
                            ErrorResponse["message"] = "Unable to fetch record with the Query:" + Convert.ToString(collection["CustomDataPull"]).Replace("\"", "'") + " from " + result["cursor"]["ns"] + ". Please validate the query.";
                            JResult = ErrorResponse;
                        }
                    }
                    else
                    {
                        isError = true;
                        ErrorResponse["message"] = "Unable to execute the Query: " + Convert.ToString(collection["CustomDataPull"]).Replace("\"", "'") + ". Please validate the query.";
                        JResult = ErrorResponse;
                    }
                }
                else
                {
                    isError = true;
                    ErrorResponse["message"] = "Database Modification commands are not allowed. Please validate the query";
                    JResult = ErrorResponse;
                }
            }
            catch (Exception ex)
            {
                isError = true;
                ErrorResponse["message"] = ex.Message;
                JResult = ErrorResponse;
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(CustomDataService), nameof(TestQueryData), ex.Message + $"   StackTrace = " + ex.StackTrace, ex, string.Empty, string.Empty, clientUID, deliveryUID);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CustomDataService), nameof(TestQueryData), CONSTANTS.END,
                    default(Guid), string.Empty, string.Empty, clientUID, deliveryUID);
            return JResult;
        }

        public Dictionary<string, string> CheckCustomAPIResponse(string clientUID, string deliveryUID, string userId, HttpContext httpContext, string category)
        {
            Dictionary<string, string> oResponse = null;
            List<string> oPropertyList = null;
            IFormCollection collection = httpContext.Request.Form;
            var CustomDataSourceDetails = collection["CustomDataPull"];
            string CustomSourceItems = string.Empty;
            string token = string.Empty;
            string TokenUrl = string.Empty;

            if (CustomDataSourceDetails.Count() > 0)
            {
                foreach (var item in CustomDataSourceDetails)
                {
                    if (item.Trim() != "{}")
                    {
                        CustomSourceItems += item;

                    }
                    else
                        CustomSourceItems = CONSTANTS.Null;
                }
            }

            var fileParams = JsonConvert.DeserializeObject<CustomInputData>(CustomSourceItems);
            if (fileParams.Data != null)
            {
                //Setting the BatchSize = "1" ,Only One document required for checking Node Structure
                foreach (JProperty oProperty in fileParams.Data.BodyParam)
                {
                    if (oProperty.Name.ToUpper() == "BATCHSIZE")
                    {
                        oProperty.Value = 1;
                        break;
                    }
                }

                Uri apiUri = new Uri(fileParams.Data.ApiUrl);
                string host = apiUri.GetLeftPart(UriPartial.Path);

                if (host.Substring(host.Length - 1) == "/")
                {
                    host = host.Remove(host.Length - 1);
                }

                //forming QueryString 
                StringBuilder querystring = new StringBuilder("?");
                string sAppend = string.Empty;

                //Checking if ClientUID & DCUID passed as KeyValue pair from UI, if not then using Querystring Parameters
                Dictionary<string, string> keyValues = new Dictionary<string, string>();
                keyValues = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(fileParams.Data.KeyValues));
                var ClienUIDCount = keyValues.Where(x => x.Key.ToUpper() == "CLIENTUID").Select(x => x.Value).ToList().Count;
                var DCUIDCount = keyValues.Where(x => x.Key.ToUpper() == "DELIVERYCONSTRUCTUID").Select(x => x.Value).ToList().Count;

                if (ClienUIDCount == 0)
                {
                    querystring.Append("ClientUId=" + clientUID);
                }

                if (DCUIDCount == 0)
                {
                    querystring.Append("&DeliveryConstructUId=" + deliveryUID);
                }

                foreach (var key in fileParams.Data.KeyValues)
                {
                    string KeyName = ((Newtonsoft.Json.Linq.JProperty)key).Name;
                    JToken KeyValue = ((Newtonsoft.Json.Linq.JProperty)key).Value;

                    if (querystring.Length > 1 && string.IsNullOrEmpty(sAppend))
                    {
                        sAppend = "&";
                    }

                    querystring.Append(sAppend + KeyName + "=" + KeyValue);
                }

                host = host + querystring;
                if (fileParams.Data.Authentication.Type.ToUpper() == "TOKEN")
                {
                    token = fileParams.Data.Authentication.Token;
                }
                else if (fileParams.Data.Authentication.Type.ToUpper() == "AZUREAD")
                {
                    AzureDetails oAuthCredentials = null;
                    if (fileParams.Data.Authentication.UseIngrainAzureCredentials)
                    {
                        TokenUrl = appSettings.Value.token_Url;
                        oAuthCredentials = new AzureDetails
                        {
                            grant_type = appSettings.Value.Grant_Type,
                            client_secret = appSettings.Value.clientSecret,
                            client_id = appSettings.Value.clientId,
                            resource = appSettings.Value.resourceId
                        };
                    }
                    else
                    {
                        TokenUrl = fileParams.Data.Authentication.AzureUrl;
                        oAuthCredentials = new AzureDetails
                        {

                            grant_type = fileParams.Data.Authentication.AzureCredentials.grant_type,
                            client_secret = fileParams.Data.Authentication.AzureCredentials.client_secret,
                            client_id = fileParams.Data.Authentication.AzureCredentials.client_id,
                            resource = fileParams.Data.Authentication.AzureCredentials.resource
                        };
                    }
                    token = CustomUrlToken("Ingrain", oAuthCredentials, TokenUrl);
                }

                StringContent content = new StringContent(JsonConvert.SerializeObject(fileParams.Data.BodyParam), Encoding.UTF8, "application/json");

                var response = InvokePOSTRequest(token, host, content, string.Empty);
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    switch (response.StatusCode)
                    {
                        case HttpStatusCode.Unauthorized:
                            {
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(CustomDataService), nameof(CheckCustomAPIResponse), HttpStatusCode.Unauthorized.ToString() + "Call to Ingrain API" + fileParams.Data.ApiUrl + "Failed", "", "", "", "");
                                throw new Exception(HttpStatusCode.Unauthorized.ToString());
                            }

                        case HttpStatusCode.BadRequest:
                            {
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(CustomDataService), nameof(CheckCustomAPIResponse), HttpStatusCode.BadRequest.ToString() + "Call to Ingrain API" + fileParams.Data.ApiUrl + "Failed", "", "", "", "");
                                throw new Exception(HttpStatusCode.BadRequest.ToString());
                            }

                        case HttpStatusCode.InternalServerError:
                            {
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(CustomDataService), nameof(CheckCustomAPIResponse), HttpStatusCode.InternalServerError.ToString() + "Call to Ingrain API" + fileParams.Data.ApiUrl + "Failed", "", "", "", "");
                                throw new Exception(HttpStatusCode.InternalServerError.ToString());
                            }

                        case HttpStatusCode.NotFound:
                            {
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(CustomDataService), nameof(CheckCustomAPIResponse), HttpStatusCode.NotFound.ToString() + "Call to Ingrain API" + fileParams.Data.ApiUrl + "Failed", "", "", "", "");
                                throw new Exception(HttpStatusCode.NotFound.ToString());
                            }

                        default:
                            {
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(CustomDataService), nameof(CheckCustomAPIResponse), "Call to Ingrain API" + fileParams.Data.ApiUrl + "Failed", "", "", "", "");
                                throw new Exception("Call to API Failed");
                            }
                    }
                }
                else
                {
                    var jsonResult = response.Content.ReadAsStringAsync().Result;
                    JObject result = JObject.Parse(jsonResult);
                    if (result.Count > 0)
                    {
                        if (oPropertyList == null)
                        {
                            oPropertyList = new List<string>();
                        }

                        foreach (JProperty oProperty in result.Properties().ToArray())
                        {
                            AddtoListIfArray(oProperty, null, null, oPropertyList);
                        }
                    }

                    oResponse = new Dictionary<string, string>();
                    if (oPropertyList.Count > 0)
                    {
                        string TargetNodes = JsonConvert.SerializeObject(oPropertyList);
                        string JsonResponse = result.Property(oPropertyList[0]).ToString();
                        oResponse.Add("JsonResponse", JsonResponse);
                        oResponse.Add("TargetNodes", TargetNodes);
                    }
                    else
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(CustomDataService), nameof(CheckCustomAPIResponse), "No Reponse returned for API :" + fileParams.Data.ApiUrl, "", "", "", "");
                        throw new Exception("Call to API Successful , But no Data Returned");
                    }
                }
            }

            return oResponse;
        }

        public void AddtoListIfArray(JProperty oProperty, JToken oToken, JObject oObject, List<string> oPropertyList)
        {
            if (oProperty != null && oProperty.ToArray()[0].Type == JTokenType.Array)
            {
                oPropertyList.Add(oProperty.Name);

                foreach (JToken ochild in oProperty.ToArray())
                {
                    if (ochild.Type == JTokenType.Array)
                    {
                        //oPropertyList.Add(ochild.Path);

                        foreach (JObject oItem in ochild)
                        {
                            AddtoListIfArray(null, null, oItem, oPropertyList);
                        }
                    }
                }
            }
            else if (oObject != null)
            {
                foreach (var x in oObject)
                {
                    if (x.Value.Type == JTokenType.Array)
                    {
                        oPropertyList.Add(oObject.Parent.Path + "." + x.Key);
                    }
                }
            }
        }

        public HttpResponseMessage InvokePOSTRequest(string token, string apiPath, StringContent content, string resourceId)
        {
            using (var client = new HttpClient())
            {
                string uri = apiPath;
                client.Timeout = new TimeSpan(0, 30, 0);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("AppServiceUId", appSettings.Value.AppServiceUID);
                if (!string.IsNullOrWhiteSpace(token))
                {
                    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                }
                if (!string.IsNullOrEmpty(resourceId))
                {
                    client.DefaultRequestHeaders.Add("resourceId", resourceId);
                }
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                return client.PostAsync(uri, content).Result;
            }
        }

        public bool ValidateQuery(string Query)
        {
            if (string.IsNullOrEmpty(Query))
                return false;
            string[] ignoreList = new string[] { " Insert ", " Insert", "Insert:", "Insert ", @"""Insert""", " Delete ", " Delete", "delete:", "delete ", @"""Delete""", " Update ", " Update", "Update:", "Update ", @"""Update""", " findAndModify ", " findAndModify", "findAndModify:", "findAndModify ", @"""findAndModify""" };
            if (ignoreList.Any(a => Query.IndexOf(a, StringComparison.InvariantCultureIgnoreCase) >= 0))
                return false;
            else
                return true;
        }
        public void InsertCustomDataSource(CustomDataSourceModel CustomDataSource, string CollectionName)
        {
            CustomDataSource.CreatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            CustomDataSource.ModifiedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            CustomDataSource.ModifiedByUser = CustomDataSource.CreatedByUser;
            var Final = JsonConvert.SerializeObject(CustomDataSource);
            var FinalCustomDataSource = BsonSerializer.Deserialize<BsonDocument>(Final);
            var CDScollection = _database.GetCollection<BsonDocument>(CollectionName);
            CDScollection.InsertOne(FinalCustomDataSource);
        }
        public void InsertUpdateCustomDataSource(CustomDataSourceModel CustomDataSource, string CollectionName)
        {
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, CustomDataSource.CorrelationId);
            var CDScollection = _database.GetCollection<BsonDocument>(CollectionName);

            var correaltionExist = CDScollection.Find(filter).ToList();
            if (correaltionExist.Count > 0)
            {
                CDScollection.DeleteMany(filter);
            }
            CustomDataSource.CreatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            CustomDataSource.ModifiedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            CustomDataSource.ModifiedByUser = CustomDataSource.CreatedByUser;
            var Final = JsonConvert.SerializeObject(CustomDataSource);
            var FinalCustomDataSource = BsonSerializer.Deserialize<BsonDocument>(Final);
            CDScollection.InsertOne(FinalCustomDataSource);
        }
        public object GetCustomSourceDetails(string correlationid, string CustomSourceType, string CollectionName)
        {
            if (CustomSourceType.ToUpper() == CONSTANTS.CustomDbQuery.ToUpper())
            {
                CustomSourceType = CONSTANTS.CustomDbQuery;
            }
            object oCustomData = new object();
            var CDScollection = _database.GetCollection<BsonDocument>(CollectionName);
            var builder = Builders<BsonDocument>.Filter;

            if (CustomSourceType.ToUpper() == CONSTANTS.CustomDataApi.ToUpper())
            {
                CustomSourceType = CONSTANTS.CustomDataApi;
            }

            var filter = builder.Eq(CONSTANTS.CorrelationId, correlationid) & builder.Eq(CONSTANTS.CustomDataPullType, CustomSourceType);
            var projection = Builders<BsonDocument>.Projection.Include("CustomSourceDetails");
            var result = CDScollection.Find(filter).Project<BsonDocument>(projection).FirstOrDefault();
            if (result != null && result.Count() > 0)
            {
                if (CustomSourceType.ToUpper() == CONSTANTS.CustomDbQuery.ToUpper())
                {
                    if (result["CustomSourceDetails"] != BsonNull.Value)
                    {
                        oCustomData = Convert.ToString(result["CustomSourceDetails"]).Replace("\"", "'").Replace("\r\n", "");
                    }
                }
                else
                {
                    oCustomData = JsonConvert.DeserializeObject<object>(result["CustomSourceDetails"].ToString());
                }
            }
            return oCustomData;
        }

        public string AppendQueryLimit(string Query)
        {
            var query = Query;
            if (query.Count() > 0)
            {
                if (query.Contains("find"))
                {
                    var Custom_DataParams = JsonConvert.DeserializeObject<dynamic>(query);
                    Custom_DataParams["limit"] = appSettings.Value.CustomQueryLimit;
                    query = Convert.ToString(Custom_DataParams);
                }
                else if (query.Contains("aggregate"))
                {
                    var sb = new StringBuilder(query);
                    if (query.ToLower().Trim().Contains("cursor"))
                    {
                        int positioncur = query.ToLower().Trim().IndexOf("cursor");
                        int pos1 = query.ToLower().Trim().IndexOf("{", positioncur);
                        int pos2 = query.ToLower().Trim().IndexOf("}", pos1);
                        if (pos2 - pos1 < 10)
                        {
                            sb.Insert(pos1 + 1, "batchSize:" + Convert.ToString(appSettings.Value.CustomQueryLimit));
                        }
                        else
                        {
                            sb.Remove(pos1 + 1, (pos2 - pos1) - 1);
                            sb.Insert(pos1 + 1, "batchSize:" + Convert.ToString(appSettings.Value.CustomQueryLimit));
                        }
                    }
                    else
                    {
                        int positionlast = query.Length - 1;
                        sb.Insert(positionlast, ",cursor:{batchSize:" + Convert.ToString(appSettings.Value.CustomQueryLimit) + "}");
                    }
                    query = Convert.ToString(sb);
                }
            }
            return query;
        }

        public string CustomUrlToken(string ApplicationName, AzureDetails oAuthCredentials, string TokenURL)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CustomDataService), nameof(CustomUrlToken), "CustomUrlToken for Application--" + ApplicationName, string.Empty, string.Empty, string.Empty, string.Empty);

            dynamic token = string.Empty;

            var client = new RestClient(TokenURL);
            var request = new RestRequest(Method.POST);
            request.AddHeader("cache-control", "no-cache");
            request.AddHeader("content-type", "application/x-www-form-urlencoded");
            request.AddParameter("application/x-www-form-urlencoded", "grant_type=" + oAuthCredentials.grant_type +
            "&client_id=" + oAuthCredentials.client_id +
            "&client_secret=" + oAuthCredentials.client_secret +
            "&resource=" + oAuthCredentials.resource,
            ParameterType.RequestBody);
            var requestBuilder = new StringBuilder();
            foreach (var param in request.Parameters)
            {
                requestBuilder.AppendFormat("{0}: {1}\r\n", param.Name, param.Value);
            }
            requestBuilder.ToString();
            IRestResponse response1 = client.Execute(request);
            string json1 = response1.Content;

            // Retrieve and Return the Access Token                
            var tokenObj = JsonConvert.DeserializeObject(json1) as dynamic;
            token = Convert.ToString(tokenObj.access_token);

            if (!string.IsNullOrEmpty(token))
            {
                var AppIntegCollection = _database.GetCollection<AppIntegration>(CONSTANTS.AppIntegration);
                var filterBuilder = Builders<AppIntegration>.Filter;
                var AppFilter = filterBuilder.Eq("ApplicationName", ApplicationName);
                var Projection = Builders<AppIntegration>.Projection.Include("BaseURL").Include("Authentication").Include("TokenGenerationURL").Include("Credentials").Exclude("_id");
                var AppData = AppIntegCollection.Find(AppFilter).Project<AppIntegration>(Projection).FirstOrDefault();

                if (AppData != null)
                {
                    AppIntegration appIntegrations = new AppIntegration();
                    appIntegrations.TokenGenerationURL = _encryptionDecryption.Encrypt(appSettings.Value.token_Url.ToString());
                    appIntegrations.Credentials = _encryptionDecryption.Encrypt(oAuthCredentials.ToJson());

                    var update = Builders<AppIntegration>.Update
                    .Set(x => x.Authentication, appSettings.Value.authProvider)
                    .Set(x => x.TokenGenerationURL, appIntegrations.TokenGenerationURL)
                    .Set(x => x.Credentials, (IEnumerable)(appIntegrations.Credentials))
                    .Set(x => x.ModifiedByUser, _encryptionDecryption.Encrypt(Convert.ToString(appSettings.Value.username)))
                    .Set(x => x.ModifiedOn, DateTime.UtcNow.ToString());
                    AppIntegCollection.UpdateOne(AppFilter, update);
                }
            }
            return token;
        }

        #endregion
    }
}
