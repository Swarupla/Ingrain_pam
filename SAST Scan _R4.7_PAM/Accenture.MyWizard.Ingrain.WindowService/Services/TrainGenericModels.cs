using Accenture.MyWizard.Ingrain.WindowService.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using AICORE = Accenture.MyWizard.Ingrain.DataModels.AICore;
using DATAMODELS=Accenture.MyWizard.Ingrain.DataModels.Models;
using WINSERVICEMODELS = Accenture.MyWizard.Ingrain.WindowService.Models;
using BUSINESSDOMAIN=Accenture.MyWizard.Ingrain.BusinessDomain;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
using DATAACCESS=Accenture.MyWizard.Ingrain.WindowService;
using Accenture.MyWizard.Ingrain.WindowService.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Threading;
using Accenture.MyWizard.Cryptography.EncryptionProviders;
using CryptographyHelper = Accenture.MyWizard.Cryptography;

namespace Accenture.MyWizard.Ingrain.WindowService.Services
{
    class TrainGenericModels : ITrainGenericModels
    {
        private readonly WINSERVICEMODELS.AppSettings appSettings = null;
        private IMongoDatabase _database;
        private DATAACCESS.DatabaseProvider databaseProvider;
        private BUSINESSDOMAIN.Interfaces.IAICoreService _aICoreService;
        static CryptographyHelper.Utility.CryptographyUtility CryptographyUtility = null;
        

        public TrainGenericModels()
        {
           appSettings = AppSettingsJson.GetAppSettings().GetSection("AppSettings").Get<WINSERVICEMODELS.AppSettings>();
            databaseProvider = new DATAACCESS.DatabaseProvider();
            var dataBaseName = MongoUrl.Create(appSettings.connectionString).DatabaseName;
           MongoClient mongoClient = databaseProvider.GetDatabaseConnection();
           _database = mongoClient.GetDatabase(dataBaseName);
            CryptographyUtility = new CryptographyHelper.Utility.CryptographyUtility();
        }

        private void SetHeader(HttpClient httpClient)
        {
            string token = GenerateToken();
            if (appSettings.authProvider.ToUpper() == "FORM")
            {
                httpClient.BaseAddress = new Uri(appSettings.myWizardAPIUrl);
                httpClient.DefaultRequestHeaders.Accept.Clear();

                httpClient.DefaultRequestHeaders.Add("UserName", appSettings.username);
                httpClient.DefaultRequestHeaders.Add("Password", appSettings.password);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);                
                httpClient.DefaultRequestHeaders.Add("AppServiceUID", appSettings.AppServiceUId);
            }
            else if (appSettings.authProvider.ToUpper() == "AZUREAD")
            {
                httpClient.BaseAddress = new Uri(appSettings.myWizardAPIUrl);
                httpClient.DefaultRequestHeaders.Accept.Clear();

                httpClient.DefaultRequestHeaders.Add("UserEmailId", CryptographyUtility.Encrypt(appSettings.UserEmail) );
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                httpClient.DefaultRequestHeaders.Add("AppServiceUID", appSettings.AppServiceUId);
            } else
            {
                httpClient.BaseAddress = new Uri(appSettings.myWizardAPIUrl);
                httpClient.DefaultRequestHeaders.Accept.Clear();

                httpClient.DefaultRequestHeaders.Add("UserEmailId", CryptographyUtility.Encrypt(appSettings.UserEmail));
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.Add("AppServiceUID", appSettings.AppServiceUId);
            }
        }
        public void CheckProductConfigurationForDeliveryConstruct(string useEmail)
        {
            throw new NotImplementedException();
        }

        public void FetchListOfClients()
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(TrainGenericModels), nameof(FetchListOfClients), "FETCH CLIENTS START", string.Empty, string.Empty, string.Empty, string.Empty);
            string jsonStringResult = null;
            try
            {
                UpdateTaskStatus(0, "FETCHCLIENT", false, "I", "Client details pull in progress");
                if (appSettings.authProvider.ToUpper() == "FORM" || appSettings.authProvider.ToUpper() == "AZUREAD")
                {
                    using (var httpClient = new HttpClient())
                    {
                        SetHeader(httpClient);
                        var ClinetStruct = String.Format("AccountClients?clientUId=" + appSettings.ClientUID + "&deliveryConstructUId=null");
                        HttpContent content = new StringContent(string.Empty);
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                        var result = httpClient.GetAsync(ClinetStruct).Result;
                        if (result.StatusCode != HttpStatusCode.OK)
                        {
                            throw new Exception($"{result.Content}" + "" + $"{result.StatusCode}");
                        }

                        jsonStringResult = result.Content.ReadAsStringAsync().Result;
                    }
                } else
                {
                    HttpClientHandler hnd = new HttpClientHandler();
                    hnd.UseDefaultCredentials = true;
                    using (var httpClient = new HttpClient(hnd))
                    {
                        SetHeader(httpClient);
                        var ClinetStruct = String.Format("AccountClients?clientUId=" + appSettings.ClientUID + "&deliveryConstructUId=null");
                        HttpContent content = new StringContent(string.Empty);
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                        var result = httpClient.GetAsync(ClinetStruct).Result;
                        if (result.StatusCode != HttpStatusCode.OK)
                        {
                            throw new Exception($"{result.Content}" + "" + $"{result.StatusCode}");
                        }

                        jsonStringResult = result.Content.ReadAsStringAsync().Result;
                    }
                }
                if (!string.IsNullOrEmpty(jsonStringResult))
                {
                    List<WINSERVICEMODELS.ClientDetails> clientDetails = JsonConvert.DeserializeObject<List<WINSERVICEMODELS.ClientDetails>>(jsonStringResult);

                    if (clientDetails.Count > 0)
                    {
                        foreach (var client in clientDetails)
                        {
                            if (appSettings.IsAESKeyVault)
                            {
                                client.ModifiedBy = CryptographyUtility.Encrypt("SYSTEM");
                                client.CreatedBy = CryptographyUtility.Encrypt("SYSTEM");
                            }
                            else
                            {
                                client.ModifiedBy = AesProvider.Encrypt("SYSTEM", appSettings.aesKey, appSettings.aesVector);
                                client.CreatedBy = AesProvider.Encrypt("SYSTEM", appSettings.aesKey, appSettings.aesVector);
                            }
                            client.CreatedOn = DateTime.UtcNow.ToString();
                            client.ModifiedBy = AesProvider.Encrypt("SYSTEM", appSettings.aesKey, appSettings.aesVector);
                            client.ModifiedOn = DateTime.UtcNow.ToString();
                        }
                        AddClientsinDB(clientDetails);
                        UpdateTaskStatus(clientDetails.Count, "FETCHCLIENT", true, "C", "Client details pull completed");
                    }
                }
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(TrainGenericModels), nameof(FetchListOfClients), "FETCH CLIENTS END", string.Empty, string.Empty, string.Empty, string.Empty);
            }
            catch(Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(TrainGenericModels), nameof(FetchListOfClients), ex.Message + "- STACKTRACE - " + ex.StackTrace + ex.InnerException, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                UpdateTaskStatus(0, "FETCHCLIENT", true, "E", ex.Message);
            }

        }

        public void FetchListofDeliveryConstructs()
        {
            try
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(TrainGenericModels), nameof(FetchListofDeliveryConstructs), "FETCH DELIVERYCONSTRUCTS START", string.Empty, string.Empty, string.Empty, string.Empty);
                List<WINSERVICEMODELS.ClientDetails> clientList = GetListofClients();
                int dcCount = 0;
                foreach (var client in clientList)
                {
                    if (appSettings.authProvider.ToUpper() == "FORM" || appSettings.authProvider.ToUpper() == "AZUREAD")
                    {
                        using (var httpClient = new HttpClient())
                        {
                            SetHeader(httpClient);
                            //var tipAMURL = String.Format("GetAccountClientDeliveryConstructs?clientUId=" + client.ClientUId + "&deliveryConstructUId=null&includeCompleteHierarchy=true&email=" + appSettings.UserEmail + "&queryMode=basic");
                            //Change for UserStory: 1916407
                            var tipAMURL = String.Format("GetAccountClientDeliveryConstructs?clientUId=" + client.ClientUId + "&deliveryConstructUId=null&includeCompleteHierarchy=true&email=" + appSettings.UserEmail + "&queryMode=LIMIT");
                            HttpContent content = new StringContent(string.Empty);
                            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                            var result = httpClient.GetAsync(tipAMURL).Result;
                            if (result.StatusCode != System.Net.HttpStatusCode.OK)
                            {
                                throw new Exception(result.StatusCode + "-" + result.Content);
                            }
                            var result1 = result.Content.ReadAsStringAsync().Result;
                            if (result1 != null && result1.ToList().Count > 0)
                            {
                                var rootObject = JsonConvert.DeserializeObject<DATAMODELS.RootObject>(result1);
                                DATAMODELS.DeliveryConstructTree deliveryConstructTree = new DATAMODELS.DeliveryConstructTree();

                                ReadClient(rootObject.Client.DeliveryConstructs, deliveryConstructTree.DeliveryConstructUId, rootObject.Client.Name);
                                //if (list.Count > 0)
                                //{
                                //    dcCount += list.Count;
                                //    AddDeliveryConstructinDB(list);
                                //}
                            }

                        }
                    } else
                    {
                        HttpClientHandler hnd = new HttpClientHandler();
                        hnd.UseDefaultCredentials = true;
                        using (var httpClient = new HttpClient(hnd))
                        {
                            SetHeader(httpClient);
                            //var tipAMURL = String.Format("GetAccountClientDeliveryConstructs?clientUId=" + client.ClientUId + "&deliveryConstructUId=null&includeCompleteHierarchy=true&email=" + appSettings.UserEmail + "&queryMode=basic");
                            //Change for UserStory: 1916407
                            var tipAMURL = String.Format("GetAccountClientDeliveryConstructs?clientUId=" + client.ClientUId + "&deliveryConstructUId=null&includeCompleteHierarchy=true&email=" + appSettings.UserEmail + "&queryMode=LIMIT");
                            HttpContent content = new StringContent(string.Empty);
                            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                            var result = httpClient.GetAsync(tipAMURL).Result;
                            if (result.StatusCode != System.Net.HttpStatusCode.OK)
                            {
                                throw new Exception(result.StatusCode + "-" + result.Content);
                            }
                            var result1 = result.Content.ReadAsStringAsync().Result;
                            if (result1 != null && result1.ToList().Count > 0)
                            {
                                var rootObject = JsonConvert.DeserializeObject<DATAMODELS.RootObject>(result1);
                                DATAMODELS.DeliveryConstructTree deliveryConstructTree = new DATAMODELS.DeliveryConstructTree();

                                ReadClient(rootObject.Client.DeliveryConstructs, deliveryConstructTree.DeliveryConstructUId, rootObject.Client.Name);
                                //if (list.Count > 0)
                                //{
                                //    dcCount += list.Count;
                                //    AddDeliveryConstructinDB(list);
                                //}
                            }
                        }
                    }
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(TrainGenericModels), nameof(FetchListofDeliveryConstructs), "FETCH CLIENTS END", string.Empty, string.Empty, client.ClientUId, string.Empty);
                }
                UpdateTaskStatus(dcCount, "FETCHDC", true, "C", "DC Fetch Completed");
            }
            catch(Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(TrainGenericModels), nameof(FetchListofDeliveryConstructs), ex.Message + "- STACKTRACE - " + ex.StackTrace + ex.InnerException, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                UpdateTaskStatus(0, "FETCHDC", true, "E", ex.Message);
            }
     
            
        }




        public void UpdateProductConfigStatus()
        {
            try
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(TrainGenericModels), nameof(UpdateProductConfigStatus), "CHECK DEVOPS CONFIG START", string.Empty, string.Empty, string.Empty, string.Empty);
                UpdateTaskStatus(0, "ADDPRODUCTSTODC", true, "I", "DevOps check in progress");
                var dcList = GetDeliveryConstructsListsFromDB();

                if (dcList.Count > 0)
                {
                    foreach (var dc in dcList)
                    {
                        UpdateProductConfig(dc.ClientUId, dc.DeliveryConstructUId);
                    }
                    UpdateTaskStatus(dcList.Count, "ADDPRODUCTSTODC", true,"C","DevOps check completed");

                }
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(TrainGenericModels), nameof(UpdateProductConfigStatus), "CHECK DEVOPS CONFIG END", string.Empty, string.Empty, string.Empty, string.Empty);
            }
            catch(Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(TrainGenericModels), nameof(UpdateProductConfigStatus), ex.Message + "- STACKTRACE - " + ex.StackTrace + ex.InnerException, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                UpdateTaskStatus(0, "ADDPRODUCTSTODC", true, "E", ex.Message);
            }
           
        }
        
        
        
        public async void UpdateProductConfig(string clientUId, string deliveryConstructUId)
        {
            var response = await CheckProductConfigurationForDeliveryConstruct(clientUId, deliveryConstructUId);
            if (response!=null)
            {
                UpdateProductConfiginDB(clientUId, deliveryConstructUId, response);
            }
           
        }

      

        public void VerifyIfModelTrainedForDC()
        {
            throw new NotImplementedException();
        }



        public string GenerateToken()
        {
            dynamic token = string.Empty;
            if (appSettings.authProvider.ToUpper() == "FORM")
            {
                using (var httpClient = new HttpClient())
                {
                    if(appSettings.Environment.Equals(CONSTANTS.PAMEnvironment))
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
            else if(appSettings.authProvider.ToUpper() == "AZUREAD")
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    var formContent = new FormUrlEncodedContent(new[]
                    {
                            new KeyValuePair<string, string>("grant_type", appSettings.Grant_Type),
                            new KeyValuePair<string, string>("client_id", appSettings.clientId),
                            new KeyValuePair<string, string>("client_secret",appSettings.clientSecret),
                            new KeyValuePair<string, string>("resource",appSettings.resourceId)
                        });

                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                    var tokenResult = client.PostAsync(appSettings.token_Url, formContent).Result;
                    Dictionary<string, string> tokenDictionary = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(tokenResult.Content.ReadAsStringAsync().Result);
                    return tokenDictionary[CONSTANTS.access_token].ToString();
                }
            }
            return token;
        }

        private void ReadClient(List<DATAMODELS.DeliveryConstruct> dc, string parentdeliveryConstructUId, string clientName)
        {
            List<WINSERVICEMODELS.DeliveryConstructDetails> dcList = new List<WINSERVICEMODELS.DeliveryConstructDetails>();
            foreach (DATAMODELS.DeliveryConstruct ds in dc)
            {
                var dcItem = new WINSERVICEMODELS.DeliveryConstructDetails
                {
                    ClientUId = ds.ClientUId,
                    ClientName = clientName,
                    DeliveryConstructUId = ds.DeliveryConstructUId,
                    DeliveryConstructName = ds.Name,
                    CreatedBy = appSettings.IsAESKeyVault? CryptographyUtility.Encrypt("SYSTEM") : AesProvider.Encrypt("SYSTEM", appSettings.aesKey, appSettings.aesVector),
                    CreatedOn = DateTime.UtcNow.ToString(),
                    ModifiedBy = appSettings.IsAESKeyVault ? CryptographyUtility.Encrypt("SYSTEM") : AesProvider.Encrypt("SYSTEM", appSettings.aesKey, appSettings.aesVector),
                    ModifiedOn = DateTime.UtcNow.ToString()
                };
                AddDCinDB(dcItem);
                if (ds.DeliveryConstructs != null)
                {
                    ReadClient(ds.DeliveryConstructs, ds.DeliveryConstructUId.ToString(),clientName);
                }
                //dcList.Add(dcItem);
            }
            //return dcList;
        }

        private string AddClientsinDB(List<WINSERVICEMODELS.ClientDetails> clientList)
        {
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.PhoenixClients);
            var filter = Builders<BsonDocument>.Filter.Empty;
            var result = collection.Find(filter).ToList();
            var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(clientList);
            var insertDocument = BsonSerializer.Deserialize<List<BsonDocument>>(jsonData);
            if (result.Count > 0)
            {
                collection.DeleteMany(filter);
            }

            collection.InsertMany(insertDocument);
            return "Success";
        }
        private List<WINSERVICEMODELS.ClientDetails> GetListofClients()
        {
            List<WINSERVICEMODELS.ClientDetails> clientList = new List<WINSERVICEMODELS.ClientDetails>();
            var serviceCollection = _database.GetCollection<BsonDocument>(CONSTANTS.PhoenixClients);
            var filter = Builders<BsonDocument>.Filter.Empty;
            var projection = Builders<BsonDocument>.Projection.Exclude("_id");
            var result = serviceCollection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                clientList = JsonConvert.DeserializeObject<List<WINSERVICEMODELS.ClientDetails>>(result.ToJson());
            }

            return clientList;
        }

        public string AddDeliveryConstructinDB(List<WINSERVICEMODELS.DeliveryConstructDetails> dcList)
        {            
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.PhoenixDeliveryConstructs);
            var filter = Builders<BsonDocument>.Filter.Eq("ClientUId",dcList[0].ClientUId);
            var result = collection.Find(filter).ToList();
            var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(dcList);
            var insertDocument = BsonSerializer.Deserialize<List<BsonDocument>>(jsonData);
            if (result.Count > 0)
            {
                collection.DeleteMany(filter);
            }

            collection.InsertMany(insertDocument);
            return "Success";
        }
        public string AddDCinDB(WINSERVICEMODELS.DeliveryConstructDetails dcList)
        {
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.PhoenixDeliveryConstructs);
            var filter = Builders<BsonDocument>.Filter.Eq("DeliveryConstructUId", dcList.DeliveryConstructUId);
            var result = collection.Find(filter).ToList();
            var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(dcList);
            var insertDocument = BsonSerializer.Deserialize<BsonDocument>(jsonData);
            if (result.Count > 0)
            {
                collection.DeleteMany(filter);
            }

            collection.InsertOne(insertDocument);
            return "Success";
        }
        public string UpdateProductConfiginDB(string clientUId,string deliveryConstructUId,List<WINSERVICEMODELS.ProductDetails> prodDetailsLst)
        {
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.PhoenixDeliveryConstructs);
            var filter = Builders<BsonDocument>.Filter.Eq("ClientUId", clientUId) & Builders<BsonDocument>.Filter.Eq("DeliveryConstructUId", deliveryConstructUId);
            var result = collection.Find(filter).ToList();
            if (result.Count > 0)
            {
                var update = Builders<BsonDocument>.Update.Set("Products", prodDetailsLst);
                collection.UpdateOne(filter, update);
            }
            
            return "Success";
        }
        public string UpdateTaskStatus(int updatedRecords,string taskCode,bool isCompleted,string status,string message)
        {
            var collection = _database.GetCollection<BsonDocument>("AutoTrainModelTasks");
            var filter = Builders<BsonDocument>.Filter.Eq("TaskCode", taskCode);
            var result = collection.Find(filter).ToList();
            if (result.Count > 0)
            {
                var update = Builders<BsonDocument>.Update.Set("IsCompleted",isCompleted)
                                                          .Set("UpdateRecords",updatedRecords)
                                                          .Set("LastExecutedDate",DateTime.UtcNow.ToString())
                                                          .Set("Status",status)
                                                          .Set("Message",message);
                collection.UpdateOne(filter, update);
            }

            return "Success";
        }
        public List<WINSERVICEMODELS.DeliveryConstructDetails> GetDeliveryConstructsListsFromDB()
        {
            List<WINSERVICEMODELS.DeliveryConstructDetails> dcList = new List<WINSERVICEMODELS.DeliveryConstructDetails>();
            var serviceCollection = _database.GetCollection<BsonDocument>(CONSTANTS.PhoenixDeliveryConstructs);
            var filter = Builders<BsonDocument>.Filter.Empty;
            var projection = Builders<BsonDocument>.Projection.Exclude("_id");
            var result = serviceCollection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                dcList = JsonConvert.DeserializeObject<List<WINSERVICEMODELS.DeliveryConstructDetails>>(result.ToJson());
            }

            return dcList;
        }

        public List<WINSERVICEMODELS.DeliveryConstructDetails> GetDeliveryConstructDetailsByProductId(string productId)
        {
            List<WINSERVICEMODELS.DeliveryConstructDetails> dcList = new List<WINSERVICEMODELS.DeliveryConstructDetails>();
            var serviceCollection = _database.GetCollection<BsonDocument>(CONSTANTS.PhoenixDeliveryConstructs);
            var filter = Builders<BsonDocument>.Filter.ElemMatch("Products", Builders<BsonDocument>.Filter.Eq("ProductUId", productId));
            var projection = Builders<BsonDocument>.Projection.Exclude("_id");
            var result = serviceCollection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                dcList = JsonConvert.DeserializeObject<List<WINSERVICEMODELS.DeliveryConstructDetails>>(result.ToJson());
            }

            return dcList;
        }


        public List<WINSERVICEMODELS.AutoTraintask> CheckAutoTraintaskStatus()
        {
            string[] taskCodes = appSettings.TaskCodes.Split(",");
            List<WINSERVICEMODELS.AutoTraintask> taskList = new List<AutoTraintask>();
            var serviceCollection = _database.GetCollection<BsonDocument>("AutoTrainModelTasks");
            var filter = Builders<BsonDocument>.Filter.AnyIn("TaskCode", taskCodes);
            var projection = Builders<BsonDocument>.Projection.Exclude("_id");
            var result = serviceCollection.Find(filter).Sort(Builders<BsonDocument>.Sort.Ascending("_id")).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                taskList = JsonConvert.DeserializeObject<List<WINSERVICEMODELS.AutoTraintask>>(result.ToJson());
            }
            
            return taskList;
        }
        public WINSERVICEMODELS.AutoTraintask GetTaskStatus(string taskCode)
        {
            WINSERVICEMODELS.AutoTraintask autoTraintask = null;
            var serviceCollection = _database.GetCollection<BsonDocument>("AutoTrainModelTasks");
            var filter = Builders<BsonDocument>.Filter.Eq("TaskCode", taskCode);
            var projection = Builders<BsonDocument>.Projection.Exclude("_id");
            var result = serviceCollection.Find(filter).Project<BsonDocument>(projection).FirstOrDefault();
            if (result != null)
            {
                autoTraintask = JsonConvert.DeserializeObject<WINSERVICEMODELS.AutoTraintask>(result.ToJson());
            }
            return autoTraintask;
        }

        public bool CreateAutoTrainTask()
        {
            try
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(TrainGenericModels), nameof(CreateAutoTrainTask), "AUTO TRAIN TASK CREATE START", string.Empty, string.Empty, string.Empty, string.Empty);
                List<WINSERVICEMODELS.AutoTraintask> taskList = new List<AutoTraintask>();
                string[] taskCodes = appSettings.TaskCodes.Split(",");
                for (int i = 0; i < taskCodes.Length; i++)
                {
                    WINSERVICEMODELS.AutoTraintask newTask = new WINSERVICEMODELS.AutoTraintask()
                    {
                        TaskUId = Guid.NewGuid().ToString(),
                        TaskCode = taskCodes[i],
                        TaskName = taskCodes[i],
                        IsCompleted = false,
                        IsActive = true,
                        Status ="N",
                        CreatedBy = "SYSTEM",
                        CreatedOn = DateTime.UtcNow.ToString(),
                        ModifiedBy = "SYSTEM",
                        ModifiedOn = DateTime.UtcNow.ToString()
                    };
                    var collection = _database.GetCollection<BsonDocument>("AutoTrainModelTasks");
                    var filter = Builders<BsonDocument>.Filter.Eq("TaskCode", taskCodes[i]);
                    var result = collection.Find(filter).ToList();
                    var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(newTask);
                    var insertDocument = BsonSerializer.Deserialize<BsonDocument>(jsonData);
                    if (result.Count <= 0)
                    {
                        collection.InsertOne(insertDocument);
                    }

                    
                }
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(TrainGenericModels), nameof(CreateAutoTrainTask), "AUTO TRAIN TASK CREATE END", string.Empty, string.Empty, string.Empty, string.Empty);
                return true;            
            }
            catch(Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(TrainGenericModels), nameof(CreateAutoTrainTask), ex.Message + "- STACKTRACE - " + ex.StackTrace + ex.InnerException, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return false;
            }
        }


        public async Task<List<WINSERVICEMODELS.ProductDetails>> CheckProductConfigurationForDeliveryConstruct(string clientUId,string deliveryConstructUId)
        {
            List<WINSERVICEMODELS.ProductDetails> response = new List<WINSERVICEMODELS.ProductDetails>();
            try
            {
                if (appSettings.authProvider.ToUpper() == "FORM" || appSettings.authProvider.ToUpper() == "AZUREAD")
                {
                    using (var httpClient = new HttpClient())
                    {
                        SetHeader(httpClient);
                        var tipAMURL = String.Format("ProductInstancesByDeliveryConstruct?clientUId=" + clientUId + "&deliveryConstructUId=" + deliveryConstructUId);
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                        var result = await httpClient.GetAsync(tipAMURL);
                        if (result.StatusCode != System.Net.HttpStatusCode.OK)
                        {
                            throw new Exception($"{result.Content}" + "" + $"{result.StatusCode}");
                        }
                        var result1 = result.Content.ReadAsStringAsync().Result;
                        if (result1 != null && result1.ToList().Count > 0)
                        {
                            var resList = JsonConvert.DeserializeObject<List<WINSERVICEMODELS.ProductDetails>>(result1);
                            response = resList;
                        }
                    }
                }
                else
                {
                    HttpClientHandler hnd = new HttpClientHandler();
                    hnd.UseDefaultCredentials = true;
                    using (var httpClient = new HttpClient(hnd))
                    {
                        SetHeader(httpClient);
                        var tipAMURL = String.Format("ProductInstancesByDeliveryConstruct?clientUId=" + clientUId + "&deliveryConstructUId=" + deliveryConstructUId);
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                        var result = await httpClient.GetAsync(tipAMURL);
                        if (result.StatusCode != System.Net.HttpStatusCode.OK)
                        {
                            throw new Exception($"{result.Content}" + "" + $"{result.StatusCode}");
                        }
                        var result1 = result.Content.ReadAsStringAsync().Result;
                        if (result1 != null && result1.ToList().Count > 0)
                        {
                            var resList = JsonConvert.DeserializeObject<List<WINSERVICEMODELS.ProductDetails>>(result1);
                            response = resList;
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(TrainGenericModels), nameof(CheckProductConfigurationForDeliveryConstruct), ex.Message + "- STACKTRACE - " + ex.StackTrace + ex.InnerException, ex, string.Empty, string.Empty, clientUId, deliveryConstructUId);
            }
            return response;
        }




        public void TrainModels()
        {
            try
            {
                UpdateTaskStatus(0, "TRAINMODEL", false, "I", "Training in progress");
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(TrainGenericModels), nameof(TrainModels), "TRAIN MODELS START", string.Empty, string.Empty, string.Empty, string.Empty);
                List<WINSERVICEMODELS.DeliveryConstructDetails> dcList = GetDeliveryConstructDetailsByProductId(appSettings.DevOpsProductId);
                string[] developer_usecaseIds = appSettings.Developerpred_UsecaseIds.Split(",");
                for (int i = 0; i < developer_usecaseIds.Length; i++)
                {
                    AICORE.UseCase.UsecaseDetails usecasedetails = GetUsecaseDetails(developer_usecaseIds[i]);
                    foreach (var dc in dcList)
                    {
                        string modelName = usecasedetails.ApplicationName + "_" + usecasedetails.UsecaseName;
                        string correlationId = Guid.NewGuid().ToString();
                        bool isExists = CheckAICoreModelByUsecaseId(dc.ClientUId,
                                                                                   dc.DeliveryConstructUId,
                                                                                   usecasedetails.ApplicationId,
                                                                                   usecasedetails.ServiceId,
                                                                                   usecasedetails.UsecaseId);
                       // DeleteModelByUsecaseId(dc.ClientUId, dc.DeliveryConstructUId, usecasedetails.ApplicationId, usecasedetails.ServiceId, usecasedetails.UsecaseId);
                        if (!isExists)
                        {
                            string token = GenerateToken();
                            string baseurl = appSettings.IngrainAPIUrl;
                            string apiPath =
                                "api/DeveloperPredictionTrain?"
                                + "clientId=" + dc.ClientUId
                                + "&deliveryConstructId=" + dc.DeliveryConstructUId
                                + "&serviceId=" + usecasedetails.ServiceId
                                + "&applicationId=" + usecasedetails.ApplicationId
                                + "&usecaseId=" + usecasedetails.UsecaseId
                                + "&modelName=" + modelName
                                + "&userId=" + appSettings.UserEmail
                                + "&isManual=" + false
                                + "&correlationId=" + correlationId
                                + "&retrain=" + false;
                            HttpResponseMessage message=InvokeGETRequest(token, baseurl, apiPath);
                            if (message.StatusCode != HttpStatusCode.OK)
                            {
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(TrainGenericModels), nameof(TrainModels), message.StatusCode.ToString() + "-" + "Error in calling ingrain .net api", usecasedetails.ApplicationId, string.Empty, dc.ClientUId, dc.DeliveryConstructUId);
                            }
                            //_aICoreService.DeveloperPredictionTraining(dc.ClientUId,
                            //                                       dc.DeliveryConstructUId,
                            //                                       usecasedetails.ServiceId,
                            //                                       usecasedetails.ApplicationId,
                            //                                       usecasedetails.UsecaseId,
                            //                                       modelName, appSettings.UserEmail, false, correlationId, false);
                        }

                    }

                }
                UpdateTaskStatus(0, "TRAINMODEL", true, "C", "Training Completed");
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(TrainGenericModels), nameof(TrainModels), "TRAIN MODELS END", string.Empty, string.Empty, string.Empty, string.Empty);
            }
            catch(Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(TrainGenericModels), nameof(TrainModels), ex.Message + "- STACKTRACE - " + ex.StackTrace + ex.InnerException, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
            
        }

        public void ReTrainModels()
        {
            try
            {
                string[] developer_usecaseIds = appSettings.Developerpred_UsecaseIds.Split(",");
                for(int i = 0; i < developer_usecaseIds.Length; i++)
                {
                    AICORE.UseCase.UsecaseDetails usecasedetails = GetUsecaseDetails(developer_usecaseIds[i]);
                    List<AICORE.AICoreModels> modelsList = GetAICoreModelByUsecaseId(usecasedetails.ApplicationId, usecasedetails.ServiceId, usecasedetails.UsecaseId);
                    foreach(var model in modelsList)
                    {
                        string token = GenerateToken();
                        string baseurl = appSettings.IngrainAPIUrl;
                        string apiPath =
                            "api/DeveloperPredictionTrain?"
                            + "clientId=" + model.ClientId
                            + "&deliveryConstructId=" + model.DeliveryConstructId
                            + "&serviceId=" + model.ServiceId
                            + "&applicationId=" + model.ApplicationId
                            + "&usecaseId=" + usecasedetails.UsecaseId
                            + "&modelName=" + model.ModelName
                            + "&userId=" + model.CreatedBy
                            + "&isManual=" + false
                            + "&correlationId=" + model.CorrelationId
                            + "&retrain=" + true;
                        HttpResponseMessage message = InvokeGETRequest(token, baseurl, apiPath);
                        if (message.StatusCode != HttpStatusCode.OK)
                        {
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(TrainGenericModels), nameof(ReTrainModels), message.StatusCode.ToString() + "-" + "Error in calling ingrain .net api",model.ApplicationId, string.Empty,model.ClientId,model.DeliveryConstructId);
                        }
                    }

                }
            }
            catch(Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(TrainGenericModels), nameof(ReTrainModels), ex.Message + "- STACKTRACE - " + ex.StackTrace + ex.InnerException, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }

        }

        //Fetch usecase details based on Id
        public AICORE.UseCase.UsecaseDetails GetUsecaseDetails(string usecaseId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(TrainGenericModels), nameof(GetUsecaseDetails), CONSTANTS.START, string.Empty, string.Empty, string.Empty, string.Empty);
            AICORE.UseCase.UsecaseDetails usecaseDetails = new AICORE.UseCase.UsecaseDetails();
            var useCaseCollection = _database.GetCollection<BsonDocument>("AISavedUsecases");
            var filter = Builders<BsonDocument>.Filter.Eq("UsecaseId", usecaseId);
            var projection = Builders<BsonDocument>.Projection.Exclude("_id");
            var result = useCaseCollection.Find(filter).Project<BsonDocument>(projection).FirstOrDefault();
            if (result != null)
            {
                usecaseDetails = JsonConvert.DeserializeObject<AICORE.UseCase.UsecaseDetails>(result.ToJson());
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(TrainGenericModels), nameof(GetUsecaseDetails), CONSTANTS.END, usecaseDetails.ApplicationId, string.Empty, string.Empty, string.Empty);
            return usecaseDetails;

        }
        public bool CheckAICoreModelByUsecaseId(string clientId, string deliveryConstructId, string applicationId, string serviceId, string usecaseId)
        {            
            var serviceCollection = _database.GetCollection<BsonDocument>("AICoreModels");
            var filter = Builders<BsonDocument>.Filter.Eq("ClientId", clientId)
                         & Builders<BsonDocument>.Filter.Eq("DeliveryConstructId", deliveryConstructId)
                          & Builders<BsonDocument>.Filter.Eq("ApplicationId", applicationId)
                           & Builders<BsonDocument>.Filter.Eq("ServiceId", serviceId)
                            & Builders<BsonDocument>.Filter.Eq("UsecaseId", usecaseId)
                            & (Builders<BsonDocument>.Filter.Eq("ModelStatus", "Completed") | Builders<BsonDocument>.Filter.Eq("ModelStatus", "InProgress"));
            var projection = Builders<BsonDocument>.Projection.Exclude("_id");
            var result = serviceCollection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                return true;               
            }
            else
            {
                return false;
            }
        }
        public void DeleteModelByUsecaseId(string clientId, string deliveryConstructId, string applicationId, string serviceId, string usecaseId)
        {
            var serviceCollection = _database.GetCollection<BsonDocument>("AICoreModels");
            var filter = Builders<BsonDocument>.Filter.Eq("ClientId", clientId)
                         & Builders<BsonDocument>.Filter.Eq("DeliveryConstructId", deliveryConstructId)
                          & Builders<BsonDocument>.Filter.Eq("ApplicationId", applicationId)
                           & Builders<BsonDocument>.Filter.Eq("ServiceId", serviceId)
                            & Builders<BsonDocument>.Filter.Eq("UsecaseId", usecaseId)
                            & (Builders<BsonDocument>.Filter.Eq("ModelStatus", "Warning") | Builders<BsonDocument>.Filter.Eq("ModelStatus", "Error"));
            var projection = Builders<BsonDocument>.Projection.Exclude("_id");
            var result = serviceCollection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                List<AICORE.AICoreModels> aICoreModels = JsonConvert.DeserializeObject<List<AICORE.AICoreModels>>(result.ToJson());
                foreach(var model in aICoreModels)
                {
                    var reqCollection = _database.GetCollection<BsonDocument>("AIServiceRequestStatus");
                    var filter2 = Builders<BsonDocument>.Filter.Eq("CorrelationId", model.CorrelationId);
                    var projection2 = Builders<BsonDocument>.Projection.Exclude("_id");
                    var result2 = serviceCollection.Find(filter).Project<BsonDocument>(projection2).ToList();
                    if (result2.Count > 0)
                    {
                        reqCollection.DeleteMany(filter2);
                    }
                }
                serviceCollection.DeleteMany(filter);
            }
           
        }
        public List<AICORE.AICoreModels> GetAICoreModelByUsecaseId(string applicationId, string serviceId, string usecaseId)
        {
            List<AICORE.AICoreModels> serviceList = new List<AICORE.AICoreModels>();
            var serviceCollection = _database.GetCollection<BsonDocument>("AICoreModels");
            var filter = Builders<BsonDocument>.Filter.Eq("ApplicationId", applicationId)
                           & Builders<BsonDocument>.Filter.Eq("ServiceId", serviceId)
                            & Builders<BsonDocument>.Filter.Eq("UsecaseId", usecaseId)
                            & Builders<BsonDocument>.Filter.Eq("ModelStatus", "Completed");
            var projection = Builders<BsonDocument>.Projection.Exclude("_id");
            var result = serviceCollection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                serviceList = JsonConvert.DeserializeObject<List<AICORE.AICoreModels>>(result.ToJson());
                if (appSettings.isForAllData)
                {
                    foreach (var model in serviceList)
                    {
                        try
                        {
                            if (!string.IsNullOrEmpty(Convert.ToString(model.CreatedBy)))
                            {
                                if (appSettings.IsAESKeyVault)
                                {
                                    model.CreatedBy = CryptographyUtility.Decrypt(Convert.ToString(model.CreatedBy));
                                }
                                else
                                {
                                    model.CreatedBy = AesProvider.Decrypt(Convert.ToString(model.CreatedBy), appSettings.aesKey, appSettings.aesVector);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(TrainGenericModels), nameof(GetAICoreModelByUsecaseId), ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty);
                        }
                        try
                        {
                            if (!string.IsNullOrEmpty(Convert.ToString(model.ModifiedBy)))
                            {
                                if (appSettings.IsAESKeyVault)
                                {
                                    model.ModifiedBy = CryptographyUtility.Decrypt(Convert.ToString(model.ModifiedBy));
                                }
                                else
                                {
                                    model.ModifiedBy = AesProvider.Decrypt(Convert.ToString(model.ModifiedBy), appSettings.aesKey, appSettings.aesVector);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(TrainGenericModels), nameof(GetAICoreModelByUsecaseId), ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty);
                        }
                    }
                }
            }

            return serviceList;
        }
        public HttpResponseMessage InvokeGETRequest(string token, string baseURI, string apiPath)
        {
            if (appSettings.authProvider.ToUpper() == "FORM" || appSettings.authProvider.ToUpper() == "AZUREAD")
            {
                using (var client = new HttpClient())
                {
                    string uri = baseURI + apiPath;
                    client.Timeout = new TimeSpan(0, 30, 0);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    if (!string.IsNullOrWhiteSpace(token))
                    {
                        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                    }
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                    return client.GetAsync(uri).Result;
                }
            } else
            {
                HttpClientHandler hnd = new HttpClientHandler();
                hnd.UseDefaultCredentials = true;
                using (var client = new HttpClient(hnd))
                {
                    string uri = baseURI + apiPath;
                    client.Timeout = new TimeSpan(0, 30, 0);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                    return client.GetAsync(uri).Result;
                }
            }
        }





        #region DataSets Incremental Pull
        public void UpdateDataSetsWithIncrementalData ()
        {
            DateTime curtime = DateTime.UtcNow;
            var dataSetCollection = _database.GetCollection<DATAMODELS.DataSetInfoDto>(CONSTANTS.DataSetInfo);
            var filter = Builders<DATAMODELS.DataSetInfoDto>.Filter.Where(x => x.SourceName == "ExternalAPI")
                         & Builders<DATAMODELS.DataSetInfoDto>.Filter.Where(x => x.EnableIncrementalFetch == true);
            var projection = Builders<DATAMODELS.DataSetInfoDto>.Projection
                                                                .Exclude(x => x._id)
                                                                .Exclude(x => x.SourceDetails)
                                                                .Exclude(x => x.ValidRecordsDetails)
                                                                .Exclude(x => x.UniqueValues)
                                                                .Exclude(x => x.UniquenessDetails);
            var result = dataSetCollection.Find(filter).Project<DATAMODELS.DataSetInfoDto>(projection).ToList();
            if(result.Count > 0)
            {
                foreach(var dataset in result)
                {
                    DateTime modifiedDate = DateTime.Parse(dataset.ModifiedOn);
                    int elapsedDays = (int)(curtime - modifiedDate).TotalDays;
                    if(elapsedDays > 1)
                    {
                        string requestId = InsertIngestDataSetRequest(dataset);
                        bool flag = true;
                        while (flag)
                        {
                            DATAMODELS.IngrainRequestQueue requestDetail = GetSSAIIngrainRequestDetails(requestId);
                            DateTime requestTime = DateTime.Parse(requestDetail.CreatedOn);
                            DateTime prsntTime = DateTime.UtcNow;
                            int elsTime = (int)(prsntTime - requestTime).TotalMinutes;
                            if (requestDetail.Status == "C" || requestDetail.Status == "E" || elsTime > 5)
                            {
                                flag = false;
                            }
                            else
                            {
                                Thread.Sleep(2000);
                            }
                        }
                        
                    }
                }
            }

        }

        public string InsertIngestDataSetRequest(DATAMODELS.DataSetInfoDto dataSetInfoDto)
        {
            JObject paramArgs = new JObject();
            paramArgs.Add("Incremental", "True");
            DATAMODELS.IngrainRequestQueue ingrainRequestQueue = new DATAMODELS.IngrainRequestQueue
            {
                _id = Guid.NewGuid().ToString(),
                CorrelationId = null,
                DataSetUId = dataSetInfoDto.DataSetUId,
                ClientId = dataSetInfoDto.ClientUId,
                DeliveryconstructId = dataSetInfoDto.DeliveryConstructUId,
                RequestId = Guid.NewGuid().ToString(),
                ProcessId = null,
                Status = null,
                ModelName = null,
                RequestStatus = CONSTANTS.New,
                RetryCount = 0,
                ProblemType = null,
                Message = null,
                UniId = null,
                Progress = null,
                pageInfo = "IngestDataSet",
                ParamArgs = JsonConvert.SerializeObject(paramArgs),
                Function = "IngestDataSet",
                CreatedByUser = dataSetInfoDto.CreatedBy,
                CreatedOn = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                ModifiedByUser = dataSetInfoDto.ModifiedBy,
                ModifiedOn = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                LastProcessedOn = null
            };

            InsertSSAIRequest(ingrainRequestQueue);
            return ingrainRequestQueue.RequestId;
        }
        public void InsertSSAIRequest(DATAMODELS.IngrainRequestQueue ingrainRequestQueue)
        {
            var requestCollection = _database.GetCollection<DATAMODELS.IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
            requestCollection.InsertOne(ingrainRequestQueue);
        }


        public DATAMODELS.IngrainRequestQueue GetSSAIIngrainRequestDetails(string requestId)
        {
            var requestCollection = _database.GetCollection<DATAMODELS.IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
            var filter = Builders<DATAMODELS.IngrainRequestQueue>.Filter.Where(x => x.RequestId == requestId);
            var result = requestCollection.Find(filter).FirstOrDefault();

            return result;
        }
        #endregion
    }
}
