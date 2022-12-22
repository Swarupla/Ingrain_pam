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
using CryptographyHelper = Accenture.MyWizard.Cryptography;

namespace Accenture.MyWizard.Ingrain.WindowService.Services
{
    public class AssetUsageTrackingService : IAssetUsageTrackingService
    {

        private readonly WINSERVICEMODELS.AppSettings appSettings = null;
        private readonly IMongoDatabase _database;

        private IMongoCollection<WINSERVICEMODELS.AssetUsageTrackingData> _assetCollection;
        private readonly int _AssetdataArchieveDays;
        private readonly int _AssetTrackingThreadtime;
        private enum assetStatus { New, Inprogess, Completed, Retry };
        static CryptographyHelper.Utility.CryptographyUtility CryptographyUtility = null;


        public AssetUsageTrackingService()
        {
            appSettings = AppSettingsJson.GetAppSettings().GetSection("AppSettings").Get<WINSERVICEMODELS.AppSettings>();
            DATAACCESS.DatabaseProvider databaseProvider = new DATAACCESS.DatabaseProvider();
            var dataBaseName = MongoUrl.Create(appSettings.connectionString).DatabaseName;
            MongoClient mongoClient = databaseProvider.GetDatabaseConnection();
            _database = mongoClient.GetDatabase(dataBaseName);
            _assetCollection = _database.GetCollection<WINSERVICEMODELS.AssetUsageTrackingData>("AssetUsageTracking");

            _AssetdataArchieveDays = Convert.ToInt32(appSettings.AssetdataArchieveDays);
            _AssetTrackingThreadtime = Convert.ToInt32(appSettings.AssetTrackingThreadtime);
            CryptographyUtility = new CryptographyHelper.Utility.CryptographyUtility();
        }



        /// <summary>
        /// Asset Usage Tracking process.
        /// </summary>
        public void AssetUsageTracking()
        {
            try
            {
                //LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "AssetUsageTracking", "AssetUsageTracking service started -" + DateTime.Now.ToString());
                var token = getToken();

                var filterBuilder = Builders<WINSERVICEMODELS.AssetUsageTrackingData>.Filter;
                var Filter = filterBuilder.Eq("Status", assetStatus.New.ToString()) | filterBuilder.Eq("Status", assetStatus.Retry.ToString());
                var assetData = _assetCollection.Find(Filter).ToList();

                if (assetData.Count > 0)
                {
                    int i = 0;
                    int skip = 0;
                    do
                    {
                        i += 20;
                        //for (int item = 0; item < assetData.Count; item++)
                        Parallel.ForEach(assetData.Skip(skip).Take(20).ToList(), item =>
                        {
                            //WINSERVICEMODELS.AssetUsageTrackingData result = assetData[item];
                            WINSERVICEMODELS.AssetUsageTrackingData result = item;
                            string contentType = "application/json";
                            var Request = JsonConvert.SerializeObject(result);
                            string baseAddress = appSettings.AssetUsageUrl + "clientUId=" + result.ClientUId + "&deliveryConstructUId=" + result.DeliveryConstructUId;

                            if (appSettings.authProvider.ToUpper() == "FORM" || appSettings.authProvider.ToUpper() == "AZUREAD")
                            {
                                using (var Client = new HttpClient())
                                {
                                    Client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                                    Client.DefaultRequestHeaders.Add("CorrelationUId", appSettings.CorrelationUId);
                                    Client.DefaultRequestHeaders.Add("AppServiceUId", appSettings.AppServiceUId);
                                    Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(contentType));

                                    HttpContent httpContent = new StringContent(Request, Encoding.UTF8, "application/json");
                                    HttpResponseMessage httpResponse = Client.PostAsync(baseAddress, httpContent).Result;
                                    var statuscode = httpResponse.StatusCode;
                                    string modifiedDate = DateTime.UtcNow.ToString("o");
                                    if (httpResponse.IsSuccessStatusCode)
                                    {

                                        var filterStatus = Builders<WINSERVICEMODELS.AssetUsageTrackingData>.Filter;
                                        var Filter1 = filterStatus.Eq("UserUniqueId", result.UserUniqueId);

                                        var update = Builders<WINSERVICEMODELS.AssetUsageTrackingData>.Update.Set("Status", "Completed").Set("ModifiedOn", modifiedDate);
                                        var isUpdated = _assetCollection.UpdateOne(Filter1, update);
                                    }
                                    else
                                    {
                                        var filter = Builders<WINSERVICEMODELS.AssetUsageTrackingData>.Filter;
                                        var Filter2 = filter.Eq("UserUniqueId", result.UserUniqueId);
                                        if (result.RetryCount <= 3)
                                        {
                                            var update = Builders<WINSERVICEMODELS.AssetUsageTrackingData>.Update.Set("Status", "Retry").Set("ModifiedOn", modifiedDate).Set("RetryCount", result.RetryCount + 1);
                                            var isUpdated = _assetCollection.UpdateOne(Filter2, update);
                                        }
                                        else
                                        {
                                            var update = Builders<WINSERVICEMODELS.AssetUsageTrackingData>.Update.Set("Status", "Error").Set("ModifiedOn", modifiedDate);
                                            var isUpdated = _assetCollection.UpdateOne(Filter2, update);
                                        }
                                    }
                                    Thread.Sleep(_AssetTrackingThreadtime);
                                }
                            }
                            else
                            {
                                HttpClientHandler hnd = new HttpClientHandler();
                                hnd.UseDefaultCredentials = true;
                                using (var Client = new HttpClient(hnd))
                                {

                                    Client.DefaultRequestHeaders.Add("CorrelationUId", appSettings.CorrelationUId);
                                    Client.DefaultRequestHeaders.Add("AppServiceUId", appSettings.AppServiceUId);
                                    Client.DefaultRequestHeaders.Add("UserEmailId", CryptographyUtility.Encrypt(appSettings.username));
                                    Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(contentType));

                                    HttpContent httpContent = new StringContent(Request, Encoding.UTF8, "application/json");
                                    HttpResponseMessage httpResponse = Client.PostAsync(baseAddress, httpContent).Result;
                                    var statuscode = httpResponse.StatusCode;
                                    string modifiedDate = DateTime.UtcNow.ToString("o");
                                    if (httpResponse.IsSuccessStatusCode)
                                    {

                                        var filterStatus = Builders<WINSERVICEMODELS.AssetUsageTrackingData>.Filter;
                                        var Filter1 = filterStatus.Eq("UserUniqueId", result.UserUniqueId);

                                        var update = Builders<WINSERVICEMODELS.AssetUsageTrackingData>.Update.Set("Status", "Completed").Set("ModifiedOn", modifiedDate);
                                        var isUpdated = _assetCollection.UpdateOne(Filter1, update);
                                    }
                                    else
                                    {
                                        var filter = Builders<WINSERVICEMODELS.AssetUsageTrackingData>.Filter;
                                        var Filter2 = filter.Eq("UserUniqueId", result.UserUniqueId);
                                        if (result.RetryCount <= 3)
                                        {
                                            var update = Builders<WINSERVICEMODELS.AssetUsageTrackingData>.Update.Set("Status", "Retry").Set("ModifiedOn", modifiedDate).Set("RetryCount", result.RetryCount + 1);
                                            var isUpdated = _assetCollection.UpdateOne(Filter2, update);
                                        }
                                        else
                                        {
                                            var update = Builders<WINSERVICEMODELS.AssetUsageTrackingData>.Update.Set("Status", "Error").Set("ModifiedOn", modifiedDate);
                                            var isUpdated = _assetCollection.UpdateOne(Filter2, update);
                                        }
                                    }
                                    Thread.Sleep(_AssetTrackingThreadtime);
                                }
                            }
                        });
                        skip = i;
                    } while (i < assetData.Count);


                    // }
                    ArchieveAssetTrackingData();
                }
                else
                {
                    //LOGGING.LogManager.Logger.LogProcessInfo(typeof(Worker), "AssetUsageTracking", "AssetUsageTracking- Token value is Null,Please check the Token Generation details." + DateTime.Now.ToString());

                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AssetUsageTrackingService), nameof(AssetUsageTracking), ex.StackTrace + "Exception" + ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
        }

        private string getToken()
        {
            dynamic token = string.Empty;
            if (appSettings.authProvider.ToUpper() == "FORM")
            {                
                using (var httpClient = new HttpClient())
                {
                    if (appSettings.Environment == CONSTANTS.PAMEnvironment)
                    {
                        httpClient.BaseAddress = new Uri(appSettings.tokenAPIUrl);
                        httpClient.DefaultRequestHeaders.Accept.Clear();
                        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                        string json = JsonConvert.SerializeObject(new
                        {
                            username = Convert.ToString(appSettings.username),
                            password = Convert.ToString(appSettings.password)
                        });
                        var requestOptions = new StringContent(json, Encoding.UTF8, "application/json");

                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                        var result = httpClient.PostAsync("", requestOptions).Result;
                        if (result.StatusCode != HttpStatusCode.OK)
                        {
                            throw new Exception(string.Format(CONSTANTS.TokenError, result.StatusCode));
                        }
                        var result1 = result.Content.ReadAsStringAsync().Result;
                        var tokenObj = JsonConvert.DeserializeObject(result1) as dynamic;
                        if (appSettings.Environment == CONSTANTS.PAMEnvironment)
                            token = tokenObj != null ? Convert.ToString(tokenObj.token) : CONSTANTS.InvertedComma;
                        else
                            token = tokenObj != null ? Convert.ToString(tokenObj.access_token) : CONSTANTS.InvertedComma;
                        return token;

                    }
                    else
                    {
                        httpClient.BaseAddress = new Uri(appSettings.tokenAPIUrl);
                        httpClient.DefaultRequestHeaders.Accept.Clear();
                        httpClient.DefaultRequestHeaders.Add("UserName", appSettings.username);
                        httpClient.DefaultRequestHeaders.Add("Password", appSettings.password);
                        HttpContent content = new StringContent(string.Empty);
                        System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                        var result = httpClient.PostAsync("", content).Result;

                        if (result.StatusCode != System.Net.HttpStatusCode.OK)
                        {
                            throw new Exception(string.Format("Unable to process your request for generating token. Please check your credentials and try again. Status Code: {0}", result.StatusCode));
                        }

                        var result1 = result.Content.ReadAsStringAsync().Result;
                        var tokenObj = JsonConvert.DeserializeObject(result1) as dynamic;

                        token = Convert.ToString(tokenObj.access_token);

                    }
                    //httpClient.BaseAddress = new Uri(appSettings.tokenAPIUrl);
                    //httpClient.DefaultRequestHeaders.Accept.Clear();
                    //httpClient.DefaultRequestHeaders.Add("UserName", appSettings.username);
                    //httpClient.DefaultRequestHeaders.Add("Password", appSettings.password);
                    //HttpContent content = new StringContent(string.Empty);
                    //System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                    //var result = httpClient.PostAsync("", content).Result;

                    //if (result.StatusCode != System.Net.HttpStatusCode.OK)
                    //{
                    //    throw new Exception(string.Format("Unable to process your request for generating token. Please check your credentials and try again. Status Code: {0}", result.StatusCode));
                    //}

                    //var result1 = result.Content.ReadAsStringAsync().Result;
                    //var tokenObj = JsonConvert.DeserializeObject(result1) as dynamic;

                    //token = Convert.ToString(tokenObj.access_token);

                   
                }
            }
            else if (appSettings.authProvider.ToUpper() == "AZUREAD")
            {
                var client = new RestClient(appSettings.token_Url);
                var request = new RestRequest(Method.POST);
                request.AddHeader("cache-control", "no-cache");
                request.AddHeader("content-type", "application/x-www-form-urlencoded");
                request.AddParameter("application/x-www-form-urlencoded", "grant_type=" + appSettings.Grant_Type +
                   "&client_id=" + appSettings.clientId +
                   "&client_secret=" + appSettings.clientSecret +
                   "&resource=" + appSettings.resourceId,
                   ParameterType.RequestBody);

                IRestResponse response = client.Execute(request);
                string json = response.Content;
                // Retrieve and Return the Access Token                
                var tokenObj = JsonConvert.DeserializeObject(json) as dynamic;
                token = Convert.ToString(tokenObj.access_token);
            }
            return token;
        }

        private void ArchieveAssetTrackingData()
        {
            DateTime aDay = DateTime.UtcNow;
            TimeSpan aMonth = new System.TimeSpan(_AssetdataArchieveDays, 0, 0, 0);
            DateTime aDayBeforeAMonth = aDay.Subtract(aMonth);
            string deletedate = aDayBeforeAMonth.ToString("o");

            var filterBuilder = Builders<WINSERVICEMODELS.AssetUsageTrackingData>.Filter;
            var Filter = filterBuilder.Eq("Status", assetStatus.Completed.ToString()) & filterBuilder.Lt("CreatedOn", deletedate);
            var result = _assetCollection.DeleteMany(Filter);
            // return result.ToString();
        }

        public async Task PushAssetUsageTrackingToSaaS()
        {
            GetSaaSTimeFormate(DateTime.UtcNow);
            var filterBuilder = Builders<BsonDocument>.Filter;
            FilterDefinition<BsonDocument> timeFilter = null;
            FilterDefinition<BsonDocument> timeFilter2 = null;
            var currentTime = DateTime.UtcNow;
            DateTime usageStartTime;
            DateTime usageEndTime;
            if(appSettings.isProd == true)
            {
                if(currentTime.Hour == 23 && currentTime.Minute >= 30) // 11:30 PM 
                {
                    usageStartTime = currentTime.AddDays(-1); // 7 to 8
                    usageEndTime = currentTime;
                }
                else
                {
                    usageStartTime = currentTime.AddDays(-2); 
                    usageEndTime = currentTime.AddDays(-1);  // 6 to 7 11:30 pm t0 11:30 pm 
                }

                usageStartTime = new DateTime(usageStartTime.Year, usageStartTime.Month, usageStartTime.Day, 23, 30, 0);
                usageEndTime = new DateTime(usageEndTime.Year, usageEndTime.Month, usageEndTime.Day, 23, 30, 0);
            }
            else
            {
                usageStartTime = currentTime.AddHours(-1);
                usageStartTime = new DateTime(usageStartTime.Year, usageStartTime.Month, usageStartTime.Day, usageStartTime.Hour, 0, 0);
                usageEndTime = currentTime;
                usageEndTime = new DateTime(usageEndTime.Year, usageEndTime.Month, usageEndTime.Day, usageEndTime.Hour, 0, 0);
            }
                        
            timeFilter = filterBuilder.Gte("CreatedOn", usageStartTime.ToString("o"))
                & filterBuilder.Lt("CreatedOn", usageEndTime.ToString("o"));

            var saasProvisionDetailsCollection = _database.GetCollection<SaaSProvisionDetailAssetUsageModel>("SAASProvisionDetails");
            //var saasProvisionData = (await saasProvisionDetailsCollection.Find(Builders<SaaSProvisionDetailAssetUsageModel>.Filter.Empty)
            //    .ToListAsync<SaaSProvisionDetailAssetUsageModel>())
            //    .GroupBy(p=>p.E2EUID, p=>p)
            //    .ToDictionary(p=>p.Key, p=>p.ToList());


        var saasProvisionData = (saasProvisionDetailsCollection.Aggregate().Group(new BsonDocument() {
                    {"_id", new BsonDocument()
                    {
                        {nameof(SaaSProvisionDetailAssetUsageModel.ServiceName), $"${nameof(SaaSProvisionDetailAssetUsageModel.ServiceName)}" },
                        {nameof(SaaSProvisionDetailAssetUsageModel.ServiceUId), $"${nameof(SaaSProvisionDetailAssetUsageModel.ServiceUId)}" },
                        {nameof(SaaSProvisionDetailAssetUsageModel.E2EUID), $"${nameof(SaaSProvisionDetailAssetUsageModel.E2EUID)}" },
                        {nameof(SaaSProvisionDetailAssetUsageModel.ClientUID), $"${nameof(SaaSProvisionDetailAssetUsageModel.ClientUID)}" },
                        {nameof(SaaSProvisionDetailAssetUsageModel.DeliveryConstructUID), $"${nameof(SaaSProvisionDetailAssetUsageModel.DeliveryConstructUID)}" },
                     {nameof(SaaSProvisionDetailAssetUsageModel.InstanceType), $"${nameof(SaaSProvisionDetailAssetUsageModel.InstanceType)}" },
                     {nameof(SaaSProvisionDetailAssetUsageModel.CorrelationUId), $"${nameof(SaaSProvisionDetailAssetUsageModel.CorrelationUId)}" }
                    } }
                    }).Project<SaaSProvisionDetailAssetUsageModel>(new BsonDocument() {
                     {"_id",0 },
                     {nameof(SaaSProvisionDetailAssetUsageModel.ServiceName), $"$_id.{nameof(SaaSProvisionDetailAssetUsageModel.ServiceName)}" },
                      {nameof(SaaSProvisionDetailAssetUsageModel.ServiceUId), $"$_id.{nameof(SaaSProvisionDetailAssetUsageModel.ServiceUId)}" },
                     {nameof(SaaSProvisionDetailAssetUsageModel.E2EUID), $"$_id.{nameof(SaaSProvisionDetailAssetUsageModel.E2EUID)}" },
                     {nameof(SaaSProvisionDetailAssetUsageModel.ClientUID), $"$_id.{nameof(SaaSProvisionDetailAssetUsageModel.ClientUID)}" },
                     {nameof(SaaSProvisionDetailAssetUsageModel.DeliveryConstructUID), $"$_id.{nameof(SaaSProvisionDetailAssetUsageModel.DeliveryConstructUID)}" },
                     {nameof(SaaSProvisionDetailAssetUsageModel.InstanceType),  $"$_id.{nameof(SaaSProvisionDetailAssetUsageModel.InstanceType)}" },
                     {nameof(SaaSProvisionDetailAssetUsageModel.CorrelationUId),  $"$_id.{nameof(SaaSProvisionDetailAssetUsageModel.CorrelationUId)}" }
                    }).ToList());


            var requestData = new List<AssetUsageSaasSubmitModel>();
            
            foreach (var service in saasProvisionData)            
            {
                FilterDefinition<BsonDocument> deliveryFilter = null;
                // List<FilterDefinition<BsonDocument>> filter = new List<FilterDefinition<BsonDocument>>();
                //foreach (var item in service.)
                //{
                //    if(deliveryFilter == null)
                //    {
                //        deliveryFilter = filterBuilder.Eq("ClientUId", item.ClientUID) &
                //            filterBuilder.Eq("End2EndId", item.E2EUID);
                //    }
                //    else
                //    {
                //        deliveryFilter = deliveryFilter | (filterBuilder.Eq("ClientUId", item.ClientUID) &
                //            filterBuilder.Eq("End2EndId", item.E2EUID));
                //    }
                //}



                    if (deliveryFilter == null)
                    {
                        deliveryFilter = filterBuilder.Eq("ClientUId", service.ClientUID) &
                            filterBuilder.Eq("End2EndId", service.E2EUID);
                    }
                    else
                    {
                        deliveryFilter = deliveryFilter | (filterBuilder.Eq("ClientUId", service.ClientUID) &
                            filterBuilder.Eq("End2EndId", service.E2EUID));
                    }
                


                // timeFilter = timeFilter & deliveryFilter;

                timeFilter = timeFilter & ((filterBuilder.Eq("FeatureName", "Landing Page") & filterBuilder.Eq("SubFeatureName", "Page Load")) | (filterBuilder.Eq("FeatureName", "Create Custom Model") & filterBuilder.Eq("SubFeatureName", "Models Created"))); 
                timeFilter2 = timeFilter & deliveryFilter;               
                var assetCollection = _database.GetCollection<BsonDocument>("AssetUsageTracking");               
                var usageData = (assetCollection.Aggregate().Match(timeFilter2)
                    .Group(new BsonDocument() {
                    {"_id", new BsonDocument()
                    {
                        {nameof(AssetUsageTrackingGroupModel.ClientUId), $"${nameof(AssetUsageTrackingGroupModel.ClientUId)}" },
                        {nameof(AssetUsageTrackingGroupModel.End2EndId), $"${nameof(AssetUsageTrackingGroupModel.End2EndId)}" },
                        {nameof(AssetUsageTrackingGroupModel.UsageType), $"${nameof(AssetUsageTrackingGroupModel.UsageType)}" },
                        {nameof(AssetUsageTrackingGroupModel.FeatureName), $"${nameof(AssetUsageTrackingGroupModel.FeatureName)}" }
                    } },
                    {nameof(AssetUsageTrackingGroupModel.Count), new BsonDocument()
                    {
                        {"$sum",1 }
                    } }
                    }).Project<AssetUsageTrackingGroupModel>(new BsonDocument() {
                     {"_id",0 },
                     {nameof(AssetUsageTrackingGroupModel.ClientUId), $"$_id.{nameof(AssetUsageTrackingGroupModel.ClientUId)}" },
                     {nameof(AssetUsageTrackingGroupModel.End2EndId), $"$_id.{nameof(AssetUsageTrackingGroupModel.End2EndId)}" },
                     {nameof(AssetUsageTrackingGroupModel.UsageType), $"$_id.{nameof(AssetUsageTrackingGroupModel.UsageType)}" },
                     {nameof(AssetUsageTrackingGroupModel.FeatureName), $"$_id.{nameof(AssetUsageTrackingGroupModel.FeatureName)}" },
                     {nameof(AssetUsageTrackingGroupModel.Count), $"${nameof(AssetUsageTrackingGroupModel.Count)}" },
                    }).ToList());
                    //.GroupBy(u => $"{u.ClientUId}_{u.DeliveryConstructUId}", u => u)
                    //.ToDictionary(u => u.Key, u => u.ToList());

                var sessionUsageEntities = new Dictionary<string, List<AssetUsageTrackingGroupModel>>();
                var modelUsageEntities = new Dictionary<string, List<AssetUsageTrackingGroupModel>>();
                foreach(var item in usageData)
                {
                    string key = $"{item.ClientUId}_{item.End2EndId}";
                    if(item.FeatureName.ToLower().Contains("model"))
                    {
                        if(modelUsageEntities.ContainsKey(key))
                        {
                            modelUsageEntities[key].Add(item);
                        }
                        else
                        {
                            modelUsageEntities.Add(key, new List<AssetUsageTrackingGroupModel>() { item });
                        }
                       
                    }
                    else
                    {
                        if(sessionUsageEntities.ContainsKey(key))
                        {
                            sessionUsageEntities[key].Add(item);
                        }
                        else
                        {
                            sessionUsageEntities.Add(key, new List<AssetUsageTrackingGroupModel>() { item });
                        }                       
                    }
                }

                var sessionUsageEntityCount = new Dictionary<string, Dictionary<string, int>>();
                var modelUsageEntityCount = new Dictionary<string, Dictionary<string, int>>();

                foreach(var item in sessionUsageEntities)
                {
                    var countData = item.Value.GroupBy(i => i.UsageType, i => i).ToDictionary(i => i.Key, i => i.Sum(l => l.Count));
                    sessionUsageEntityCount.Add(item.Key, countData);
                }

                foreach(var item in modelUsageEntities)
                {
                    var countData = item.Value.GroupBy(i => i.UsageType, i => i).ToDictionary(i => i.Key, i => i.Sum(l => l.Count));
                    modelUsageEntityCount.Add(item.Key, countData);
                }

                var entities = new Dictionary<string, string>()
                {
                    {"User Session","00100001-0090-0000-0000-000000000008" },
                    {"Models","00100001-0090-0000-0000-000000000034" }
                };               

                foreach(var entity in entities)
                {
                    var serviceData = new AssetUsageSaasSubmitModel();
                    serviceData.Usages = new List<AssetUsageSaasSubmitUsageModel>();

                    //foreach(var client in service.Value)
                    //{
                        serviceData.ServiceUId = service.ServiceUId;
                        serviceData.ServiceName = service.ServiceName;
                        serviceData.InstanceType = service.InstanceType;
                        serviceData.CorrelationUId = service.CorrelationUId;
                        Dictionary<string, int> usages = null;
                        if(entity.Key == "Models")
                        {
                            modelUsageEntityCount.TryGetValue($"{service.ClientUID}_{service.E2EUID}", out usages);
                        }
                        else
                        {
                            sessionUsageEntityCount.TryGetValue($"{service.ClientUID}_{service.E2EUID}", out usages);
                        }

                    if (appSettings.isProd == false)
                    {
                        if (usages == null)
                        {
                            usages = new Dictionary<string, int>()
                                {
                                    {"Test", 0 }
                                };
                        }
                    }

                    if (usages != null)
                        {
                            foreach(var item in usages)
                            {
                                var usage = new AssetUsageSaasSubmitUsageModel();
                                usage.ClientUId = service.ClientUID;
                                usage.DeliveryConstructUId = service.DeliveryConstructUID;
                                usage.E2EUId = service.E2EUID;
                                usage.Usage = new UsageModel()
                                {
                                    UsageType = item.Key,
                                    Count = item.Value,
                                    UsageFrom = GetSaaSTimeFormate(usageStartTime),
                                    UsageTo = GetSaaSTimeFormate(usageEndTime),
                                    UsageExtensions = new List<UsageExtensionModel>()
                        {
                            new UsageExtensionModel()
                            {
                                Key = "AppServiceUId",
                                Value = appSettings.AppServiceUId
                            }
                        }

                                };
                                serviceData.Usages.Add(usage);
                            }
                        }

                   // }

                    serviceData.LogTime = GetSaaSTimeFormate(DateTime.UtcNow);
                    serviceData.EntityUId = entity.Value;
                    serviceData.Entity = entity.Key;
                    if(serviceData.Usages.Count > 0)
                    {
                        var client = new RestClient(appSettings.token_Url);
                        var request = new RestRequest(Method.POST);
                        request.AddHeader("cache-control", "no-cache");
                        request.AddHeader("content-type", "application/x-www-form-urlencoded");
                        request.AddParameter("application/x-www-form-urlencoded", "grant_type=" + appSettings.Grant_Type +
                           "&client_id=" + appSettings.SaaS_clientId +
                           "&client_secret=" + appSettings.SaaS_clientSecret +
                           "&resource=" + appSettings.saasResourceId,
                           ParameterType.RequestBody);

                        IRestResponse response = client.Execute(request);
                        string json = response.Content;
                        // Retrieve and Return the Access Token                
                        var tokenObj = JsonConvert.DeserializeObject(json) as dynamic;
                        string token = Convert.ToString(tokenObj.access_token);

                        using(var httpClient = new HttpClient())
                        {
                            httpClient.BaseAddress = new Uri($"{appSettings.MyWizardSaaSUrl}saas/v1/SubmitUsage");
                            httpClient.DefaultRequestHeaders.Accept.Clear();
                            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                            var usageContent = new StringContent(JsonConvert.SerializeObject(serviceData), Encoding.UTF8, "application/json");
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AssetUsageTrackingService), nameof(PushAssetUsageTrackingToSaaS), $"Asset usage data push for Service UID : {JsonConvert.SerializeObject(serviceData)} for start time {usageStartTime} and end time {usageEndTime}", string.Empty, string.Empty, string.Empty, string.Empty);
                            var httpResponse = await httpClient.PostAsync(string.Empty, usageContent);

                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AssetUsageTrackingService), nameof(PushAssetUsageTrackingToSaaS), JsonConvert.SerializeObject(serviceData), string.Empty, string.Empty, string.Empty, string.Empty);
                            if (httpResponse.IsSuccessStatusCode)
                            {
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AssetUsageTrackingService), nameof(PushAssetUsageTrackingToSaaS), $"Asset usage data push for Service UID : {serviceData.ServiceUId} for start time {usageStartTime} and end time {usageEndTime}", string.Empty, string.Empty, string.Empty, string.Empty);
                            }
                            else
                            {
                                var errorContent = httpResponse.Content.ReadAsStringAsync().Result;
                                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AssetUsageTrackingService), nameof(PushAssetUsageTrackingToSaaS), $"Failed push asset usage for the service UID {serviceData.ServiceUId} - {httpResponse.ReasonPhrase}" + $"for start time {usageStartTime} and end time {usageEndTime}", null, string.Empty, string.Empty, string.Empty, string.Empty);
                            }
                        }
                    }
                }                                
            }
        }

        private string GetSaaSTimeFormate(DateTime dateTime)
        {
            var format = dateTime.ToString("o");
            return format;
        }
    }
}
