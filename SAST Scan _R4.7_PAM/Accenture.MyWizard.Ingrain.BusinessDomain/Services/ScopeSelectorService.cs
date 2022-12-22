

#region Copyright (c) 2018 Accenture . All rights reserved.

// <copyright company="Accenture">
// Copyright (c) 2018 Accenture.  All rights reserved.
// Reproduction or transmission in whole or in part, in any form or by any means, 
// electronic, mechanical or otherwise, is prohibited without the prior written 
// consent of the copyright owner.
// </copyright>

#endregion

#region ProcessDataService Information
/********************************************************************************************************\
Module Name     :   ScopeSelectorService
Project         :   Accenture.MyWizard.SelfServiceAI.BusinessDomain
Organisation    :   Accenture Technologies Ltd.
Created By      :   Ravi Kumar
Created Date    :   28-March-2019
Revision History :
Revision No             Initial             Modified Date           Description
1.0                     An                  28-March-2019           
\********************************************************************************************************/
#endregion

namespace Accenture.MyWizard.Ingrain.BusinessDomain.Services
{
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
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Security.Cryptography;
    using Microsoft.Extensions.DependencyInjection;
    using System.Text;
    using System.Threading.Tasks;
    using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;

    public class ScopeSelectorService : IScopeSelectorService
    {
        private string appServiceUID;
        private string userEmail;
        private string userID;
        private string password;
        private string vdsTokenURL;
        private string myWizardFortressWebAPIUrl;
        private string myWizardFortressWebAPIUrl_Azure;
        private IngrainDeliveryConstruct ingrainDeliveryConstruct = null;
        private MongoClient _mongoClient;
        private IMongoDatabase _database;
        PreProcessDTO preProcessDTO;
        private string PublishModelEntityUID;
        private string taskentityUID;
        private string AccessRoleNames;
        private string pamDeliveryConstructsUrl;
        private string ClientID = "";
        private string DCID = "";
        private readonly DatabaseProvider databaseProvider;
        private IEncryptionDecryption _encryptionDecryption;
        private IngrainAppSettings appSettings { get; set; }

        public Dictionary<string, string> AgileWorkItemTypes = new Dictionary<string, string>() {
            {"00020040020000100040000000000000", "Story" },
            { "00020040020000100020000000000000", "Epic" },
            { "00020040020000100030000000000000", "Feature" },
            {"00020040020000100060000000000000", "Defect" },
            { "00020040020000100050000000000000", "Task" },
            { "00020040020000100070000000000000", "Impediment" },
            { "00020040020000100100000000000000", "Issue" },
            { "00020040020000100110000000000000", "Risk" },
            {"00020040020000100090000000000000", "Board"}
            };

        public Dictionary<string, string> predefinedEntityList = new Dictionary<string, string>() {
            {"story", "Agile" },
            { "epic", "Agile" },
            { "feature", "Agile" },
            {"defect", "AD/Agile" },
            { "iteration", "ALL" },
            { "task", "AD/Agile" },
            { "impediment", "Agile" },
            { "issue", "Agile/PPM" },
             {"test", "AD"},
              {"milestone", "AD/PPM"},
             {"requirement",  "AD"},
        {"deliverable","AD/PPM"},
     {"risk", "Agile/PPM"},
     {"codecommit", "Devops"},
     {"codebranch", "Devops"},
     {"build","Devops"},
     {"deployment", "Devops"},
     {"environment","Devops"},
     {"testresult", "AD/Devops"},
            { "action","PPM"},
            { "decision","PPM"},
            {"resource","PPM" },
            { "assignment","PPM"},
            { "changerequest","PPM"},
            { "observation","PPM"},
            { "bphnode","PPM"},
            { "conversation","PPM"}
   };

        public ScopeSelectorService(DatabaseProvider db, IOptions<IngrainAppSettings> settings, IServiceProvider serviceProvider)
        {
            appSettings = settings.Value;
            appServiceUID = appSettings.AppServiceUID;//ConfigurationManager.AppSettings["AppServiceUID"];
            userEmail = appSettings.username;
            userID = appSettings.username;//ConfigurationManager.AppSettings["DemographicsUser"];
            password = appSettings.password;//ConfigurationManager.AppSettings["DemographicsPass"];
            vdsTokenURL = appSettings.tokenAPIUrl;
            myWizardFortressWebAPIUrl = appSettings.myWizardAPIUrl;//ConfigurationManager.AppSettings["myWizardFortressWebAPIUrl"];
            myWizardFortressWebAPIUrl_Azure = appSettings.myWizardAPIUrl;
            databaseProvider = db;
            _mongoClient = databaseProvider.GetDatabaseConnection();
            var dataBaseName = MongoUrl.Create(appSettings.connectionString).DatabaseName;
            _database = _mongoClient.GetDatabase(dataBaseName);
            preProcessDTO = new PreProcessDTO();
            PublishModelEntityUID = appSettings.PublishModelEntityUID;//ConfigurationManager.AppSettings["PublishModelEntityUID"];
            taskentityUID = appSettings.TaskentityUID;//ConfigurationManager.AppSettings["taskentityUID"];
            AccessRoleNames = appSettings.AccessRoleNames;//ConfigurationManager.AppSettings["AccessRoleNames"];
            pamDeliveryConstructsUrl = appSettings.pamDeliveryConstructsUrl;//ConfigurationManager.AppSettings["PAMDeliveryConstructsUrl"];
            _encryptionDecryption = serviceProvider.GetService<IEncryptionDecryption>();
        }

        private void SetHeader(string token, HttpClient httpClient)
        {

            if (appSettings.authProvider.ToUpper() == "FORM")
            {
                httpClient.BaseAddress = new Uri(myWizardFortressWebAPIUrl);
                httpClient.DefaultRequestHeaders.Accept.Clear();

                httpClient.DefaultRequestHeaders.Add("UserName", userID);
                httpClient.DefaultRequestHeaders.Add("Password", password);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                httpClient.DefaultRequestHeaders.Add("AppServiceUID", appServiceUID);
            }
            else if (appSettings.authProvider.ToUpper() == "AZUREAD")
            {
                httpClient.BaseAddress = new Uri(myWizardFortressWebAPIUrl);
                httpClient.DefaultRequestHeaders.Accept.Clear();

                httpClient.DefaultRequestHeaders.Add("UserEmailId", _encryptionDecryption.Encrypt(userEmail));
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                httpClient.DefaultRequestHeaders.Add("AppServiceUID", appSettings.AppServiceUID);
            }
            else
            {
                httpClient.BaseAddress = new Uri(myWizardFortressWebAPIUrl);
                httpClient.DefaultRequestHeaders.Accept.Clear();

                httpClient.DefaultRequestHeaders.Add("UserEmailId", _encryptionDecryption.Encrypt(userEmail));
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.Add("AppServiceUID", appSettings.AppServiceUID);

            }
        }

        private void SetPAMHeader(string token, HttpClient httpClient, string baseAddress)
        {
            httpClient.BaseAddress = new Uri(baseAddress);
            httpClient.DefaultRequestHeaders.Accept.Clear();

            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Add("apiToken", token);
        }
        
        private string GetSecurityAcessAgilePhinix(string token, UserSecurityAcessAgile objpost)
        {
            userEmail = objpost.UserID;
            string JsonStringResult = "";
            HttpClient httpClient;
            if (appSettings.authProvider.ToUpper() == "FORM" || appSettings.authProvider.ToUpper() == "AZUREAD")
            {
                httpClient = new HttpClient();
            }
            else
            {
                HttpClientHandler hnd = new HttpClientHandler();
                hnd.UseDefaultCredentials = true;
                httpClient = new HttpClient(hnd);
            }
            SetHeader(token, httpClient);
            // var tipAMURL = String.Format("AccountAccessPrivileges?clientUId=" + Convert.ToString(objpost.ClientUID) + "&deliveryConstructUId=" + objpost.DeliveryConstructUId + "&entityUId=" + PublishModelEntityUID + "");

            var tipAMURL = String.Format("AccountAccessPrivileges?clientUId=" + Convert.ToString(objpost.ClientUID) + "&deliveryConstructUId=" + objpost.DeliveryConstructUId + "&EntityUId=" + PublishModelEntityUID);
            HttpContent content = new StringContent(string.Empty);
            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            var result = httpClient.GetAsync(tipAMURL).Result;
            if (result.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception($"{result.Content}" + "" + $"{result.StatusCode}");
                //throw new Exception(string.Format("GetSecurityAcessAgilePhinix: Unable to process your request for generating token. Please check your credentials and try again. Status Code: {0}", result.StatusCode));
            }
            var result1 = result.Content.ReadAsStringAsync().Result;
            if (result1 != null && result1.ToList().Count > 0)
            {
                var rootObject = JsonConvert.DeserializeObject<SecurityAcess>(result1);
                if (rootObject != null && rootObject.AccountPermissionViews.Count > 0)
                {
                    var tempone = rootObject.AccountPermissionViews.FirstOrDefault();
                    JsonStringResult = JsonConvert.SerializeObject(tempone);
                }
                return JsonStringResult;
            }

            return JsonStringResult;
        }

        private string getSecurityAcessPhinix(string token, Guid ClientUID, Guid DeliveryConstructUId, string UserEmail)
        {
            string JsonStringResult = "False";
            int counter;
            using (var httpClient = new HttpClient())
            {
                userEmail = UserEmail;
                SetHeader(token, httpClient);

                var tipAMURL = String.Format("GetAccountClientDeliveryConstructs?clientUId=" + Convert.ToString(ClientUID) + "&deliveryConstructUId=null&includeCompleteHierarchy=true&email=" + UserEmail + "&queryMode=basic");
                HttpContent content = new StringContent(string.Empty);
                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                var result = httpClient.GetAsync(tipAMURL).Result;
                if (result.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    throw new Exception($"{result.Content}" + "" + $"{result.StatusCode}");
                    //throw new Exception(string.Format("GetAccountClientDeliveryConstructs: Unable to get data. Please check your credentials and try again. Status Code: {0}", result.StatusCode));
                }
                var result1 = result.Content.ReadAsStringAsync().Result;
                if (result1 != null)
                {
                    var rootObject = JsonConvert.DeserializeObject<RootObject>(result1);
                    DeliveryConstructTree firstObject = new DeliveryConstructTree();
                    counter = 0;
                    counter = ReadClientAoth(rootObject.Client.DeliveryConstructs, firstObject.DeliveryConstructUId, Convert.ToString(DeliveryConstructUId), counter);
                    if (counter > 0)
                        JsonStringResult = "True";

                    return JsonConvert.SerializeObject(JsonStringResult);
                }
            }
            return JsonStringResult;
        }

        private int ReadClientAoth(List<DeliveryConstruct> dc, string parentdeliveryConstructUId, string deliveryConstructUId, int counter)
        {
            if (dc != null)
            {
                foreach (DeliveryConstruct ds in dc)
                {
                    if (ds.DeliveryConstructUId.ToUpper() == deliveryConstructUId.ToUpper())
                    {
                        counter++;
                        return counter;
                    }
                    if (ds.DeliveryConstructs != null)
                        counter = ReadClientAoth(ds.DeliveryConstructs, ds.DeliveryConstructUId, deliveryConstructUId, counter);
                }
            }

            return counter;
        }

        private List<Node> ReadClient(List<DeliveryConstruct> dc, string parentdeliveryConstructUId)
        {
            List<Node> node = new List<Node>();
            foreach (DeliveryConstruct ds in dc)
            {
                var v1 = new Node
                {
                    DeliveryConstructType = ds.DeliveryConstructType.Name,
                    DeliveryConstructUID = ds.DeliveryConstructUId.ToString(),
                    Name = ds.Name,
                    AcessRole = ds.AccessRoleName,
                    ParentDeliveryConstructUID = parentdeliveryConstructUId,
                    ImageBinary = ds.ImageBinary,
                    SelectedIndex = "False"
                };
                if (ds.DeliveryConstructs != null)
                {
                    v1.Children = ReadClient(ds.DeliveryConstructs, ds.DeliveryConstructUId.ToString());
                }
                node.Add(v1);
            }
            return node;
        }

        public string UserStoryclientStructureGetOuth(string token, Guid ClientUID, string UserEmail)
        {
            string JsonStringResult = "";
            if (token == "" || string.IsNullOrEmpty(token))
                return null;
            using (var httpClient = new HttpClient())
            {
                userEmail = UserEmail;
                SetHeader(token, httpClient);
                List<Node> nodeList = new List<Node>();
                var ClinetStruct = String.Format("GetAccountDeliveryConstructs?clientUId=" + ClientUID + "&deliveryConstructUId=null&email=" + UserEmail + "&queryMode=basic");
                HttpContent content = new StringContent(string.Empty);
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                var result = httpClient.GetAsync(ClinetStruct).Result;
                if (result.StatusCode != HttpStatusCode.OK)
                {
                    throw new Exception($"{result.Content}" + "" + $"{result.StatusCode}");
                    // throw new Exception(string.Format("GetAccountDeliveryConstructs: Unable to get data. Please check your credentials and try again. Status Code: {0}", result.StatusCode));
                }
                var result1 = result.Content.ReadAsStringAsync().Result;
                if (result1 != null && result1.ToList().Count > 0)
                {
                    var data = JsonConvert.DeserializeObject<ClientStructure>(result1);


                    string ClientResult = JsonConvert.SerializeObject(data);
                    return ClientResult;
                }
            }
            return JsonStringResult;
        }



        public dynamic UserStoryclientStructureGetOuthNew(string token, Guid ClientUID, string UserEmail)
        {
            string JsonStringResult = "";
            //if (token == "" || string.IsNullOrEmpty(token))
            //    return null;
            HttpClient httpClient;
            if (appSettings.authProvider.ToUpper() == "FORM" || appSettings.authProvider.ToUpper() == "AZUREAD")
            {
                httpClient = new HttpClient();
            }
            else
            {
                HttpClientHandler hnd = new HttpClientHandler();
                hnd.UseDefaultCredentials = true;
                httpClient = new HttpClient(hnd);
            }
            userEmail = UserEmail;
            SetHeader(token, httpClient);
            List<Node> nodeList = new List<Node>();
            var ClinetStruct = String.Format("AccountClients?clientUId=" + ClientUID + "&deliveryConstructUId=null");
            HttpContent content = new StringContent(string.Empty);
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            var result = httpClient.GetAsync(ClinetStruct).Result;
            if (result.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception($"{result.Content}" + "" + $"{result.StatusCode}");
                //throw new Exception(string.Format("AccountClients: Unable to get data. Please check your credentials and try again. Status Code: {0}", result.StatusCode));
            }

            var result1 = result.Content.ReadAsStringAsync().Result;
            if (result1 != null && result1.ToList().Count > 0)
            {
                return JsonConvert.DeserializeObject(result1);
            }
            return JsonStringResult;
        }

        public dynamic getAppBuildInfo(string token, Guid clientUId, string UserEmail)
        {
            string JsonStringResult = "";
            HttpClient httpClient;
            if (appSettings.authProvider.ToUpper() == "FORM" || appSettings.authProvider.ToUpper() == "AZUREAD")
            {
                httpClient = new HttpClient();
            }
            else
            {
                HttpClientHandler hnd = new HttpClientHandler();
                hnd.UseDefaultCredentials = true;
                httpClient = new HttpClient(hnd);
            }
            userEmail = UserEmail;
            SetHeader(token, httpClient);
            List<Node> nodeList = new List<Node>();
            var ClinetStruct = String.Format("getAppBuildInfo?clientUId=" + clientUId + "&deliveryConstructUId=null");
            HttpContent content = new StringContent(string.Empty);
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            var result = httpClient.GetAsync(ClinetStruct).Result;
            if (result.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception($"{result.Content}" + " : " + $"{result.StatusCode}");
            }

            var result1 = result.Content.ReadAsStringAsync().Result;
            if (result1 != null && result1.ToList().Count > 0)
            {
                return JsonConvert.DeserializeObject(result1);
            }
            return JsonStringResult;
        }

        public string GetDeliveryStructuerFromPhoenix(string token, string ClientUID, string DeliveryConstructUId, string UserEmail)
        {
            string ClientResult = "";
            HttpClient httpClient;
            if (appSettings.authProvider.ToUpper() == "FORM" || appSettings.authProvider.ToUpper() == "AZUREAD")
            {
                httpClient = new HttpClient();
            }
            else
            {
                HttpClientHandler hnd = new HttpClientHandler();
                hnd.UseDefaultCredentials = true;
                httpClient = new HttpClient(hnd);
            }
            userEmail = UserEmail;
            SetHeader(token, httpClient);
            var tipAMURL = String.Format("GetAccountClientDeliveryConstructs?clientUId=" + ClientUID + "&deliveryConstructUId=null&includeCompleteHierarchy=true&email=" + UserEmail + "&queryMode=basic");
            HttpContent content = new StringContent(string.Empty);
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            var result = httpClient.GetAsync(tipAMURL).Result;
            if (result.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception($"{result.Content}" + "" + $"{result.StatusCode}");
                // throw new Exception(string.Format("GetDeliveryStructuerFromPhoenix: Unable to get data. Please check your credentials and try again. Status Code: {0}", result.StatusCode));
            }
            var result1 = result.Content.ReadAsStringAsync().Result;
            if (result1 != null && result1.ToList().Count > 0)
            {
                var rootObject = JsonConvert.DeserializeObject<RootObject>(result1);
                DeliveryConstructTree deliveryConstructTree = new DeliveryConstructTree();

                var list = ReadClient(rootObject.Client.DeliveryConstructs, deliveryConstructTree.DeliveryConstructUId);

                ClientResult = JsonConvert.SerializeObject(list);
                return ClientResult;
            }
            return ClientResult;
        }

        public dynamic getLanguage(string token, string ClientUID, string DeliveryConstructUId, string UserEmail)
        {
            string JsonStringResult = "False";

            HttpClient httpClient;
            if (appSettings.authProvider.ToUpper() == "FORM" || appSettings.authProvider.ToUpper() == "AZUREAD")
            {
                httpClient = new HttpClient();
            }
            else
            {
                HttpClientHandler hnd = new HttpClientHandler();
                hnd.UseDefaultCredentials = true;
                httpClient = new HttpClient(hnd);
            }

            userEmail = UserEmail;
            setHeaderAgile(token, httpClient);
            var tipAMURL = String.Format("?ClientUId=" + Convert.ToString(ClientUID) + "&DeliveryConstructUId=" + DeliveryConstructUId);
            HttpContent content = new StringContent(string.Empty);
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            var result = httpClient.PostAsync(tipAMURL, null).Result;
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ScopeSelectorService), nameof(getLanguage), "language token :" + token, string.Empty, string.Empty, string.Empty, string.Empty);
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ScopeSelectorService), nameof(getLanguage), "language URl :" + tipAMURL, string.Empty, string.Empty, string.Empty, string.Empty);
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ScopeSelectorService), nameof(getLanguage), "language response : Status Code - " + result.StatusCode + ", Content - " + result.Content, string.Empty, string.Empty, string.Empty, string.Empty);
            if (result.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception($"{result.Content}" + "" + $"{result.StatusCode}");
                // throw new Exception(string.Format("GetMetricData: Unable to get data. Please check your credentials and try again. Status Code: {0}", result.StatusCode));
            }
            var result1 = result.Content.ReadAsStringAsync().Result;
            if (result1 != null)
            {
                return JsonConvert.DeserializeObject(result1);
            }

            return JsonStringResult;
        }

        public dynamic DeliveryConstructName(string token, Guid ClientUID, Guid DeliveryConstructUId, string UserEmail)
        {
            string JsonStringResult = "False";
            HttpClient httpClient;
            if (appSettings.authProvider.ToUpper() == "FORM" || appSettings.authProvider.ToUpper() == "AZUREAD")
            {
                httpClient = new HttpClient();
            }
            else
            {
                HttpClientHandler hnd = new HttpClientHandler();
                hnd.UseDefaultCredentials = true;
                httpClient = new HttpClient(hnd);
            }
            userEmail = UserEmail;
            SetHeader(token, httpClient);

            var tipAMURL = String.Format("GetAccountClientDeliveryConstructs?clientUId=" + Convert.ToString(ClientUID) + "&deliveryConstructUId=" + DeliveryConstructUId + "&includeCompleteHierarchy=true&email=" + UserEmail + "&queryMode=basic");
            HttpContent content = new StringContent(string.Empty);
            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            var result = httpClient.GetAsync(tipAMURL).Result;
            if (result.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception($"{result.Content}" + "" + $"{result.StatusCode}");
                //throw new Exception(string.Format("GetAccountClientDeliveryConstructs: Unable to get data. Please check your credentials and try again. Status Code: {0}", result.StatusCode));
            }
            var result1 = result.Content.ReadAsStringAsync().Result;
            if (result1 != null)
            {
                var rootObject = JsonConvert.DeserializeObject<RootObject>(result1);
                return rootObject.Client.DeliveryConstructs;
            }
            return JsonStringResult;
        }

        public string fetchSecurityAcessAgilePhinix(string token, UserSecurityAcessAgile objpost)
        {
            var agiledata = GetSecurityAcessAgilePhinix(token, objpost);
            var accountPermissionView = JsonConvert.DeserializeObject<AccountPermissionView>(agiledata);

            List<UserSecurityAcessRole> objmain = new List<UserSecurityAcessRole>();

            UserSecurityAcessRole obj = new UserSecurityAcessRole();
            obj.ClientUID = objpost.ClientUID;
            obj.DeliveryConstructUId = objpost.DeliveryConstructUId;

            if (accountPermissionView != null && AccessRoleNames.Contains(accountPermissionView.AccessRoleName))
            {
                obj.AccessRoleName = accountPermissionView.AccessRoleName;
                obj.AccessPrivilegeCode = accountPermissionView.AccessPrivilegeCode;
                var collection = _database.GetCollection<BsonDocument>("IngrainDeliveryConstruct");
                var filter = Builders<BsonDocument>.Filter.Eq("UserId", objpost.UserID);
                var result = collection.Find(filter).ToList();
                if (result.Count() > 0)
                {
                    var builder = Builders<BsonDocument>.Update;
                    var update = builder.Set("AccessPrivilegeCode", obj.AccessPrivilegeCode)
                          .Set("AccessRoleName", obj.AccessRoleName)
                          .Set("ClientUId", objpost.ClientUID.ToString())
                          .Set("DeliveryConstructUID", objpost.DeliveryConstructUId.ToString());

                    collection.UpdateMany(filter, update);
                }
                else
                {
                    ingrainDeliveryConstruct = new IngrainDeliveryConstruct();
                    ingrainDeliveryConstruct.UserId = objpost.UserID;
                    ingrainDeliveryConstruct.ClientUId = objpost.ClientUID.ToString();
                    ingrainDeliveryConstruct.DeliveryConstructUID = objpost.DeliveryConstructUId.ToString();
                    ingrainDeliveryConstruct.Cookie = false;
                    ingrainDeliveryConstruct.AccessRoleName = obj.AccessRoleName;
                    ingrainDeliveryConstruct.AccessPrivilegeCode = obj.AccessPrivilegeCode;
                    var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(ingrainDeliveryConstruct);
                    var insertDocument = BsonSerializer.Deserialize<BsonDocument>(jsonData);
                    collection.InsertOne(insertDocument);
                }

                objmain.Add(obj);
            }
            else
            {
                obj.AccessPrivilegeCode = "R";
                obj.AccessRoleName = null;
                objmain.Add(obj);
            }
            string JsonStringResult = JsonConvert.SerializeObject(objmain);
            return JsonStringResult;
        }

        /// <summary>
        /// Gets the delivery Construct recod based on User Id
        /// </summary>
        /// <param name="userId">The user identifier</param>
        /// <returns>Returns the result.</returns>
        public IngrainDeliveryConstruct GetDeliveryConstruct(string userId)
        {
            var deliveryConstructCollection = _database.GetCollection<IngrainDeliveryConstruct>("IngrainDeliveryConstruct");
            var filter = Builders<IngrainDeliveryConstruct>.Filter.Eq("UserId", userId);
            var projection = Builders<IngrainDeliveryConstruct>.Projection.Include("UserId").Include("ClientUId").Include("DeliveryConstructUID").Include("Cookie").Exclude("_id");
            var result = deliveryConstructCollection.Find(filter).Project<IngrainDeliveryConstruct>(projection).ToList();
            if (result.Count() > 0)
            {
                ingrainDeliveryConstruct = new IngrainDeliveryConstruct();
                ingrainDeliveryConstruct.UserId = result[0].UserId;
                ingrainDeliveryConstruct.ClientUId = result[0].ClientUId;
                ingrainDeliveryConstruct.DeliveryConstructUID = result[0].DeliveryConstructUID;
                ingrainDeliveryConstruct.Cookie = result[0].Cookie;
            }

            return ingrainDeliveryConstruct;
        }


        /// <summary>
        /// Set the Cookie value based on UserId
        /// </summary>
        /// <param name="userId">The user identifier</param>
        /// <returns>Returns the result.</returns>
        public string SetUserCookieDetails(bool value, string userId)
        {
            var collection = _database.GetCollection<BsonDocument>("IngrainDeliveryConstruct");
            var filter = Builders<BsonDocument>.Filter.Eq("UserId", userId);
            var projection = Builders<BsonDocument>.Projection.Include("UserId").Include("ClientUId").Include("DeliveryConstructUID").Include("Cookie").Exclude("_id");
            var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            var newFieldUpdate = Builders<BsonDocument>.Update.Set("Cookie", value);
            var outcome = collection.UpdateOne(filter, newFieldUpdate);
            return "Success";
        }

        public dynamic ForcedSignin(string token, string userId)
        {
            string JsonStringResult = "";
            HttpClient httpClient;
            if (appSettings.authProvider.ToUpper() == "FORM" || appSettings.authProvider.ToUpper() == "AZUREAD")
            {
                httpClient = new HttpClient();
            }
            else
            {
                HttpClientHandler hnd = new HttpClientHandler();
                hnd.UseDefaultCredentials = true;
                httpClient = new HttpClient(hnd);
            }
            userEmail = userId;
            SetHeader(token, httpClient);
            var tipAMURL = String.Format("ForcedSignIn");
            HttpContent content = new StringContent(string.Empty);
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            var result = httpClient.PostAsync(tipAMURL, null).Result;
            if (result.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception($"{result.Content}" + "" + $"{result.StatusCode}");
                // throw new Exception(string.Format("GetMetricData: Unable to get data. Please check your credentials and try again. Status Code: {0}", result.StatusCode));
            }
            var result1 = result.Content.ReadAsStringAsync().Result;
            if (result1 != null && result1 != "")
            {
                return JsonConvert.DeserializeObject(result1);
            }

            return JsonStringResult;
        }

        /// <summary>
        /// If userId exist delete that record and insert as new record. If userId does not exist, insert as new record.
        /// </summary>
        /// <param name="ingrainDeliveryConstruct">The ingrain delivery construct</param>        
        public void PostDeliveryConstruct(IngrainDeliveryConstruct ingrainDeliveryConstruct)
        {
            var collection = _database.GetCollection<BsonDocument>("IngrainDeliveryConstruct");
            var filter = Builders<BsonDocument>.Filter.Eq("UserId", ingrainDeliveryConstruct.UserId);
            var projection = Builders<BsonDocument>.Projection.Include("UserId").Include("ClientUId").Include("DeliveryConstructUID").Include("Cookie").Exclude("_id");
            var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(ingrainDeliveryConstruct);
            var updateDocument = BsonSerializer.Deserialize<BsonDocument>(jsonData);
            if (result.Count > 0)
            {
                var data = JObject.Parse(updateDocument.ToString());

                foreach (var item in data.Children())
                {
                    JObject serializeData = new JObject();
                    JProperty jProperty = item as JProperty;
                    if (jProperty != null)
                    {
                        string propertyname = jProperty.Name;
                        switch (propertyname)
                        {
                            case "ClientUId":
                                ClientID = jProperty.Value.ToString();
                                break;
                            case "DeliveryConstructUID":
                                DCID = jProperty.Value.ToString();
                                break;
                        }
                    }
                }

                if (ClientID != "" && DCID != "")
                {
                    var builder = Builders<BsonDocument>.Update;
                    var update = builder.Set("ClientUId", ClientID)
                          .Set("DeliveryConstructUID", DCID);
                    collection.UpdateMany(filter, update);
                }
            }
        }

        public dynamic ClientNameByClientUId(string token, Guid ClientUID, string DeliveryConstructUId, string UserId)
        {
            Dictionary<string, string> di = new Dictionary<string, string>();
            List<Dictionary<string, string>> dictList = new List<Dictionary<string, string>>();
            string JsonStringResult = "";
            HttpClient httpClient;
            if (appSettings.authProvider.ToUpper() == "FORM" || appSettings.authProvider.ToUpper() == "AZUREAD")
            {
                httpClient = new HttpClient();
            }
            else
            {
                HttpClientHandler hnd = new HttpClientHandler();
                hnd.UseDefaultCredentials = true;
                httpClient = new HttpClient(hnd);
            }

            if (UserId != null)
            {
                userEmail = UserId;
            }
            SetHeader(token, httpClient);
            List<Node> nodeList = new List<Node>();
            var ClinetStruct = String.Format("GetAccountClientDeliveryConstructs?clientUId=" + Convert.ToString(ClientUID) + "&deliveryConstructUId=" + DeliveryConstructUId + "&includeCompleteHierarchy=true&email=" + userEmail + "&queryMode=basic");
            HttpContent content = new StringContent(string.Empty);
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            //var result = httpClient.GetAsync(ClinetStruct).Result;
            //var result1 = result.Content.ReadAsStringAsync().Result;
            var result = httpClient.GetAsync(ClinetStruct).Result.Content.ReadAsStringAsync();
            var result1 = result.Result;
            if (result1 != null)
            {
                var rootObject = JsonConvert.DeserializeObject<RootObject>(result1);
                di.Add("Name", rootObject.Client.Name);
                dictList.Add(di);
            }
            return dictList;
        }

        public string ClientNameByClientUIdNew(string token, Guid ClientUID)
        {
            string JsonStringResult = "";
            if (token == "" || string.IsNullOrEmpty(token))
                return null;
            using (var httpClient = new HttpClient())
            {
                SetHeader(token, httpClient);
                List<Node> nodeList = new List<Node>();
                var ClinetStruct = String.Format("VirtualAssistantAlertMessagesByGroup?clientUId=00100000-0000-0000-0000-000000000000&deliveryConstructUId=ebd6bf97-5588-e811-a9ca-00155da6d537&virtualAssistantAlertMessageUId=null&VirtualAssistantAlertMessageTypeUId=null");
                HttpContent content = new StringContent(string.Empty);
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                var result = httpClient.GetAsync(ClinetStruct).Result;

                if (result.StatusCode != HttpStatusCode.OK)
                {
                    throw new Exception($"{result.Content}" + "" + $"{result.StatusCode}");
                    //throw new Exception(string.Format("Clients: Unable to get data. Please check your credentials and try again. Status Code: {0}", result.StatusCode));
                }
                var result1 = result.Content.ReadAsStringAsync().Result;
                if (result1 != null && result1.ToList().Count > 0)
                {
                    var clientResponse = JsonConvert.DeserializeObject<ClientNameResponse>(result1);
                    var clientResult = clientResponse != null ? clientResponse.Clients : null;
                    string clientName = clientResult != null && clientResult.Count > 0 ? clientResult.FirstOrDefault().Name : null;

                    return JsonConvert.SerializeObject(clientName);
                }
            }
            return JsonStringResult;
        }

        public string PAMDeliveryConstructName(string token, Guid DeliveryConstructUId)
        {
            string JsonStringResult = "";
            if (token == "" || string.IsNullOrEmpty(token))
                return null;
            using (var httpClient = new HttpClient())
            {
                SetPAMHeader(token, httpClient, pamDeliveryConstructsUrl);
                List<Node> nodeList = new List<Node>();
                var ClinetStruct = String.Format("delivery-constructs?page=" + 0 + "&size=1000");
                HttpContent content = new StringContent(string.Empty);
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                var result = httpClient.GetAsync(ClinetStruct).Result;
                if (result.StatusCode != HttpStatusCode.OK)
                {
                    throw new Exception($"{result.Content}" + "" + $"{result.StatusCode}");
                    // throw new Exception(string.Format("PAMDeliveryConstructName: Unable to get data. Please check your credentials and try again. Status Code: {0}", result.StatusCode));
                }
                var result1 = result.Content.ReadAsStringAsync().Result;
                if (result1 != null && result1.ToList().Count > 0)
                {
                    var allDeliveryStcuctPAM = JsonConvert.DeserializeObject<AllDeliveryStcuctPAM>(result1);
                    var deliveryStrcutList = allDeliveryStcuctPAM.content.ToList();
                    var filteredDeliveryStrcut = deliveryStrcutList.Where(deliveryStructs => deliveryStructs.id == Convert.ToString(DeliveryConstructUId));

                    string clientName = filteredDeliveryStrcut == null || filteredDeliveryStrcut.Count() == 0 ? string.Empty : filteredDeliveryStrcut.FirstOrDefault().name;

                    return JsonConvert.SerializeObject(clientName);
                }
            }
            return JsonStringResult;
        }

        public dynamic appExecutionContext(string token, Guid ClientUID, Guid DeliveryConstructUId, string UserEmail)
        {
            string appExecution = null;
            HttpClient httpClient;
            if (appSettings.authProvider.ToUpper() == "FORM" || appSettings.authProvider.ToUpper() == "AZUREAD")
            {
                httpClient = new HttpClient();
            }
            else
            {
                HttpClientHandler hnd = new HttpClientHandler();
                hnd.UseDefaultCredentials = true;
                httpClient = new HttpClient(hnd);
            }

            if (UserEmail != null)
            {
                userEmail = UserEmail;
            }
            SetHeader(token, httpClient);
            var tipAMURL = String.Format("appExecutionContext?clientUId=" + Convert.ToString(ClientUID) + "&deliveryConstructUId=" + Convert.ToString(DeliveryConstructUId));
            HttpContent content = new StringContent(string.Empty);
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            var result = httpClient.GetAsync(tipAMURL).Result;
            if (result.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception($"{result.Content}" + "" + $"{result.StatusCode}");
                // throw new Exception(string.Format("appExecutionContext: Unable to get data. Please check your credentials and try again. Status Code: {0}", result.StatusCode));
            }
            var result1 = result.Content.ReadAsStringAsync().Result;
            if (result1 != null && result1.ToList().Count > 0)
            {
                return JsonConvert.DeserializeObject(result1);
            }
            return appExecution;
        }


        public dynamic ClientDetails(string token, Guid ClientUID, Guid DeliveryConstructUId, string UserEmail)
        {

            string clientsdetails = null;
            HttpClient httpClient;
            if (appSettings.authProvider.ToUpper() == "FORM" || appSettings.authProvider.ToUpper() == "AZUREAD")
            {
                httpClient = new HttpClient();
            }
            else
            {
                HttpClientHandler hnd = new HttpClientHandler();
                hnd.UseDefaultCredentials = true;
                httpClient = new HttpClient(hnd);
            }
            if (UserEmail != null)
            {
                userEmail = UserEmail;
            }
            SetHeader(token, httpClient);

            var tipAMURL = String.Format("client?clientUId=" + Convert.ToString(ClientUID) + "&deliveryConstructUId=" + Convert.ToString(DeliveryConstructUId));
            HttpContent content = new StringContent(string.Empty);
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            var result = httpClient.GetAsync(tipAMURL).Result;
            if (result.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception($"{result.Content}" + "" + $"{result.StatusCode}");
                //throw new Exception(string.Format("client: Unable to get data. Please check your credentials and try again. Status Code: {0}", result.StatusCode));
            }
            var result1 = result.Content.ReadAsStringAsync().Result;
            if (result1 != null && result1.ToList().Count > 0)
            {

                return JsonConvert.DeserializeObject(result1);
            }

            return clientsdetails;
        }

        public dynamic GetMetricData(string token, string ClientUID, string DeliveryConstructUId, string userId)
        {
            HttpClient httpClient;
            if (appSettings.authProvider.ToUpper() == "FORM" || appSettings.authProvider.ToUpper() == "AZUREAD")
            {
                httpClient = new HttpClient();
            }
            else
            {
                HttpClientHandler hnd = new HttpClientHandler();
                hnd.UseDefaultCredentials = true;
                httpClient = new HttpClient(hnd);
            }

            if (userId != null)
            {
                userEmail = userId;
            }
            SetHeader(token, httpClient);
            var tipAMURL = String.Format("GetMetricData?clientUId=" + Convert.ToString(ClientUID) + "&deliveryConstructUId=" + DeliveryConstructUId);
            HttpContent content = new StringContent(string.Empty);
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            var result = httpClient.GetAsync(tipAMURL).Result;
            if (result.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception($"{result.Content}" + "" + $"{result.StatusCode}");
                // throw new Exception(string.Format("GetMetricData: Unable to get data. Please check your credentials and try again. Status Code: {0}", result.StatusCode));
            }
            var result1 = result.Content.ReadAsStringAsync().Result;
            return JsonConvert.DeserializeObject(result1);
        }

        /// <summary>
        /// Get Dynamic Entity List
        /// </summary>
        /// <param name="token"></param>
        /// <param name="ClientUID"></param>
        /// <param name="DeliveryConstructUId"></param>
        /// <returns></returns>
        public dynamic GetDynamicEntity(string token, string ClientUID, string DeliveryConstructUId, string UserEmail)
        {
            //EntityModel entityModel = new EntityModel();
            //entityModel.allEntityList = new Dictionary<string, List<string>>();
            //entityModel.agileList = new List<string>();
            //entityModel.aD_AgileList = new List<string>();
            //entityModel.allList = new List<string>();
            //entityModel.aDList = new List<string>();
            //entityModel.devopsList = new List<string>();
            //entityModel.aD_devopsList = new List<string>();
            //entityModel.otherList = new List<string>();
            Dictionary<string, List<string>> allEntityList = new Dictionary<string, List<string>>();
            List<string> nameList = new List<string>();
            List<string> agileList = new List<string>();
            List<string> aD_AgileList = new List<string>();
            List<string> allList = new List<string>();
            List<string> aDList = new List<string>();
            List<string> devopsList = new List<string>();
            List<string> aD_devopsList = new List<string>();
            List<string> otherList = new List<string>();
            List<string> aD_ppmList = new List<string>();
            List<string> agile_ppmList = new List<string>();
            List<string> ppmList = new List<string>();
            //if (string.IsNullOrEmpty(token))
            //    return null;
            HttpClient httpClient;
            if (appSettings.authProvider.ToUpper() == "FORM" || appSettings.authProvider.ToUpper() == "AZUREAD")
            {
                httpClient = new HttpClient();
            }
            else
            {
                HttpClientHandler hnd = new HttpClientHandler();
                hnd.UseDefaultCredentials = true;
                httpClient = new HttpClient(hnd);
            }
            if (UserEmail != null)
            {
                userEmail = UserEmail;
            }
            SetHeader(token, httpClient);
            //call pheonix API 1 to get ProductInstanceUid's
            var tipAMURL = String.Format("ProductInstancesByDeliveryConstruct?clientUId=" + Convert.ToString(ClientUID) + "&deliveryConstructUId=" + DeliveryConstructUId);
            HttpContent content = new StringContent(string.Empty);
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            var result = httpClient.GetAsync(tipAMURL).Result;
            if (result.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception($"{result.Content}" + "" + $"{result.StatusCode}");
            }
            var productInstance = result.Content.ReadAsStringAsync().Result;
            var productInstanceUidList = JsonConvert.DeserializeObject<List<dynamic>>(productInstance);
            //get unique ProductInstanceUid's
            productInstanceUidList = productInstanceUidList.Select(x => x.ProductInstanceUId).Distinct().ToList();
            for (int item = 0; item < productInstanceUidList.Count; item++)
            {
                var productInstanceUId = productInstanceUidList[item].ToString();
                //call pheonix API 2 to get entityList
                var tipAMURL_1 = String.Format("EntitiesByProductInstance?clientUID=" + Convert.ToString(ClientUID) + "&deliveryConstructUId=" + DeliveryConstructUId + "&productInstanceUId=" + productInstanceUId);
                HttpContent content_1 = new StringContent(string.Empty);
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                var result_1 = httpClient.GetAsync(tipAMURL_1).Result;
                if (result_1.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    throw new Exception($"{result_1.Content}" + "" + $"{result_1.StatusCode}");
                }
                var entitiesByProductInstance = result_1.Content.ReadAsStringAsync().Result;
                var entityList = JsonConvert.DeserializeObject<List<dynamic>>(entitiesByProductInstance);
                for (int i = 0; i < entityList.Count; i++)
                {
                    string entityData = "";
                    var name = entityList[i].Name.ToString();
                    if (name != "WorkItem")
                    {
                        nameList.Add(name);
                        string entityName = Convert.ToString(name);
                        entityName = entityName.ToLower();
                        predefinedEntityList.TryGetValue(entityName, out entityData);
                        if (!string.IsNullOrEmpty(entityData))
                        {
                            switch (entityData)
                            {
                                case "AD/Agile":
                                    if (!aD_AgileList.Contains(name))
                                    {
                                        aD_AgileList.Add(name);
                                    }
                                    break;
                                case "ALL":
                                    if (!allList.Contains(name))
                                    {
                                        allList.Add(name);
                                    }
                                    break;
                                case "AD":
                                    if (!aDList.Contains(name))
                                    {
                                        aDList.Add(name);
                                    }
                                    break;
                                case "Devops":
                                    if (!devopsList.Contains(name))
                                    {
                                        devopsList.Add(name);
                                    }
                                    break;
                                case "AD/Devops":
                                    if (!aD_devopsList.Contains(name))
                                    {
                                        aD_devopsList.Add(name);
                                    }
                                    break;
                                case "AD/PPM":
                                    if (!aD_ppmList.Contains(name))
                                    {
                                        aD_ppmList.Add(name);
                                    }
                                    break;
                                case "Agile/PPM":
                                    if (!agile_ppmList.Contains(name))
                                    {
                                        agile_ppmList.Add(name);
                                    }
                                    break;
                                case "PPM":
                                    if (!ppmList.Contains(name))
                                    {
                                        ppmList.Add(name);
                                    }
                                    break;
                            }
                        }
                        else
                        {
                            if (!otherList.Contains(name))
                            {
                                if (name != "DeliveryPlan" && name != "DeliveryTask")
                                {
                                    otherList.Add(name);
                                }
                            }
                        }
                    }
                    else
                    {
                        var entityProperties = entityList[i]["EntityProperties"].ToString();
                        var entityPropertiesList = JsonConvert.DeserializeObject<List<dynamic>>(entityProperties);
                        entityPropertiesList = ((List<dynamic>)entityPropertiesList).Select(x => (string)x["WorkItemTypeUId"]).Distinct().ToList();
                        string value = "";
                        for (int j = 0; j < entityPropertiesList.Count; j++)
                        {
                            var workItemTypeUId = entityPropertiesList[j];
                            if (!string.IsNullOrEmpty(workItemTypeUId))
                            {
                                workItemTypeUId = workItemTypeUId.Replace("-", string.Empty);
                                AgileWorkItemTypes.TryGetValue(workItemTypeUId, out value);
                                if (!string.IsNullOrEmpty(value))
                                {
                                    // nameList.Add(value);
                                    if (value.ToLower() == "defect" || value.ToLower() == "task")
                                    {
                                        if (!aD_AgileList.Contains(value))
                                        {
                                            aD_AgileList.Add(value);
                                        }
                                    }
                                    else if (value.ToLower() == "risk" || value.ToLower() == "issue")
                                    {
                                        if (!agile_ppmList.Contains(value))
                                        {
                                            agile_ppmList.Add(value);
                                        }
                                    }
                                    else
                                    {
                                        agileList.Add(value);
                                    }

                                }
                            }
                            //AgileWorkItemTypes.Where(d => d.Key.Contains(workItemTypeUId));
                            //var entityProperties = entityList[i].Name.ToString();
                        }

                    }
                }
            }
            agileList = agileList.Distinct().ToList();
            allEntityList.Add("Agile", agileList);
            allEntityList.Add("AD/Agile", aD_AgileList);
            allEntityList.Add("ALL", allList);
            allEntityList.Add("AD", aDList);
            allEntityList.Add("Devops", devopsList);
            allEntityList.Add("AD/Devops", aD_devopsList);
            allEntityList.Add("Others", otherList);
            allEntityList.Add("AD/PPM", aD_ppmList);
            allEntityList.Add("Agile/PPM", agile_ppmList);
            allEntityList.Add("PPM", ppmList);
            // string entityData = "";
            // List<string> valueList = new List<string>();
            //foreach (var item in nameList)
            // {
            //   predefinedEntityList.TryGetValue(item, out entityData);
            // if(!string.IsNullOrEmpty(entityData))
            //{
            //allEntityList.Add(entityData,item);
            //}

            //}


            // return nameList;//JsonConvert.DeserializeObject(result1);
            return allEntityList;
        }
        public dynamic GetVDSDetail(string token, string ClientUID, string DeliveryConstructUID, string E2EUID, string UserId)
        {
            if (string.IsNullOrEmpty(token))
                return null;

            string VDSInfo = null;
            using (var httpClient = new HttpClient())
            {
                if (UserId != null)
                {
                    userEmail = UserId;
                }

                string tipAMURL = string.Empty;
                if (appSettings.Environment == CONSTANTS.PAMEnvironment)
                {
                    this.SetPAMHeader(token, httpClient, appSettings.ATRDataFabricURL);
                    tipAMURL = String.Format("delivery-constructs/" + Convert.ToString(DeliveryConstructUID));
                }
                else if (appSettings.IsSaaSPlatform)
                {
                    this.SetVDSHeader(token, httpClient);                  
                    tipAMURL = String.Format("PAM/GetDeliveryConstructByIdSaaS?ClientUID=" + Convert.ToString(ClientUID) + "&E2EUID=" + Convert.ToString(E2EUID) + "&DeliveryConstructUID=" + Convert.ToString(DeliveryConstructUID));
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(ScopeSelectorService), nameof(GetVDSDetail), "VDS URL :" + tipAMURL, string.Empty, string.Empty, string.Empty, string.Empty);
                }
                else
                {
                    this.SetVDSHeader(token, httpClient);
                    tipAMURL = String.Format("DemographicsFetch/GetDemographicsName?ClientUID=" + Convert.ToString(ClientUID) + "&E2EUID=" + Convert.ToString(E2EUID) + "&DeliveryConstructUID=" + Convert.ToString(DeliveryConstructUID));
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(ScopeSelectorService), nameof(GetVDSDetail), "VDS URL :" + tipAMURL, string.Empty, string.Empty, string.Empty, string.Empty);
                }              

                //SetVDSHeader(token, httpClient);
                //var tipAMURL = String.Format("DemographicsFetch/GetDemographicsName?ClientUID=" + Convert.ToString(ClientUID) + "&E2EUID=" + Convert.ToString(E2EUID) + "&DeliveryConstructUID=" + Convert.ToString(DeliveryConstructUID));
                HttpContent content = new StringContent(string.Empty);
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                var result = httpClient.GetAsync(tipAMURL).Result;
                if (result.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    throw new Exception($"{result.Content}" + " " + $"{result.StatusCode}");
                }
                var result1 = result.Content.ReadAsStringAsync().Result;
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(ScopeSelectorService), nameof(GetVDSDetail), "result :" + result1, string.Empty, string.Empty, string.Empty, string.Empty);

                if (appSettings.IsSaaSPlatform)
                {
                    SaaSclientInfo scopeinfo = new SaaSclientInfo();
                    var collection = _database.GetCollection<SAASProvisionDetails>("SAASProvisionDetails");
                    var builder = Builders<SAASProvisionDetails>.Filter;
                    var filter = builder.Eq("ClientUID", Convert.ToString(ClientUID)) & builder.Eq("E2EUID", Convert.ToString(E2EUID));

                    var SAASCollection = collection.Find(filter).ToList();
                    if (SAASCollection.Count > 0)
                    {
                        scopeinfo.ClientName = SAASCollection[0].ClientName;
                        scopeinfo.E2EName = SAASCollection[0].E2EName;
                        var result_DC = JsonConvert.DeserializeObject(result1);
                        var resultparse = JObject.Parse(result_DC.ToString());
                        JObject serialize = new JObject();
                        string parentvalue = string.Empty;
                        foreach (var item in resultparse.Children())
                        {
                            var prop = item as JProperty;
                            if (prop.Name == "name")
                            {
                                scopeinfo.DeliveryConstructName = prop.Value.ToString();
                            }
                        }

                        return scopeinfo;
                    }
                    else
                    {
                        throw new Exception("No data found in SaaSProvision for ClientUID:" + ClientUID + " ,DeliveryConstructUID:" + DeliveryConstructUID + " ,E2EUID:" + E2EUID);
                    }
                   
                }
                else
                {
                    if (result1 != null && result1.ToList().Count > 0)
                    {

                        return JsonConvert.DeserializeObject(result1);
                    }
                }
            }
            return VDSInfo;

        }       


        private void setHeaderAgile(string token, HttpClient httpClient)
        {
            if (appSettings.authProvider.ToUpper() == "FORM")
            {
                httpClient.BaseAddress = new Uri(myWizardFortressWebAPIUrl);
                httpClient.DefaultRequestHeaders.Accept.Clear();

                httpClient.DefaultRequestHeaders.Add("UserName", userID);
                httpClient.DefaultRequestHeaders.Add("Password", password);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                httpClient.DefaultRequestHeaders.Add("AppServiceUID", appServiceUID);
            }
            else if (appSettings.authProvider.ToUpper() == "AZUREAD")
            {
                httpClient.BaseAddress = new Uri(appSettings.languageURL);
                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Add("AppServiceUID", appServiceUID);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
            }
            else
            {
                httpClient.BaseAddress = new Uri(appSettings.languageURL);
                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Add("AppServiceUID", appServiceUID);
                httpClient.DefaultRequestHeaders.Add("UserEmailId", userEmail);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }
        }
        private void SetHeaderMetrices(string token, HttpClient httpClient)
        {
            httpClient.BaseAddress = new Uri(myWizardFortressWebAPIUrl);
            httpClient.DefaultRequestHeaders.Accept.Clear();

            httpClient.DefaultRequestHeaders.Add("UserName", userID);
            httpClient.DefaultRequestHeaders.Add("Password", password);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
            httpClient.DefaultRequestHeaders.Add("AppServiceUID", appServiceUID);
        }
        private void SetVDSHeader(string token, HttpClient httpClient)
        {
            httpClient.BaseAddress = new Uri(appSettings.VdsURL);
            httpClient.DefaultRequestHeaders.Accept.Clear();

            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
        }

        /// <summary>
        /// To fetch default client details
        /// </summary>
        /// <returns>Returns the Client Id & Name</returns>
        public PAMClientScope ClientDetails()
        {
            PAMClientScope clientDetails = new PAMClientScope
            {
                ClientUID = appSettings.PAMClientUID,
                ClientName = appSettings.PAMClientName
            };

            return clientDetails;
        }

        public string VDSSecurityTokenForPAD()
        {
            string token = string.Empty;
            var username = userID;
            var Password = password;
            var tokenendpointurl = vdsTokenURL;
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("username", username);
                client.DefaultRequestHeaders.Add("password", Password);

                var tokenResponse = client.PostAsync(tokenendpointurl, null).Result;
                var tokenResult = tokenResponse.Content.ReadAsStringAsync().Result;
                Dictionary<string, string> tokenDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(tokenResult);
                if (tokenDictionary != null)
                {
                    if (tokenResponse.IsSuccessStatusCode)
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
            return token;
        }

        public List<IngrainDeliveryConstruct> GetUserRole(string userEmail)
        {
            List<IngrainDeliveryConstruct> userRole = new List<IngrainDeliveryConstruct>();
            var deliveryConstructCollection = _database.GetCollection<IngrainDeliveryConstruct>("IngrainDeliveryConstruct");
            var filter = Builders<IngrainDeliveryConstruct>.Filter.Eq("UserId", userEmail);
            var projection = Builders<IngrainDeliveryConstruct>.Projection.Include("AccessRoleName").Include("AccessPrivilegeCode").Exclude("_id");
            var result = deliveryConstructCollection.Find(filter).Project<IngrainDeliveryConstruct>(projection).ToList();
            if (result.Count() > 0)
            {
                return result;
            }
            return userRole;
        }

        //public dynamic GetUserRole(string userEmail)
        //{
        //    List<dynamic> userRole = new List<dynamic>();
        //    var deliveryConstructCollection = _database.GetCollection<IngrainDeliveryConstruct>("IngrainDeliveryConstruct");
        //    var filter = Builders<IngrainDeliveryConstruct>.Filter.Eq("UserId", userEmail);
        //    var projection = Builders<IngrainDeliveryConstruct>.Projection.Include("AccessRoleName").Include("AccessPrivilegeCode").Exclude("_id");
        //    var result = deliveryConstructCollection.Find(filter).Project<IngrainDeliveryConstruct>(projection).ToList();
        //    if (result.Count() > 0)
        //    {
        //        //  userRole = result[0].AccessRoleName.ToString();
        //        userRole = JsonConvert.DeserializeObject<List<dynamic>>(result.ToJson());
        //    }
        //    return userRole;
        //}


        public void SaveENSNotification(ENSEntityNotification entityNotification)
        {
            var ensCollection = _database.GetCollection<ENSEntityNotificationLog>(CONSTANTS.ENSEntityNotificationLog);
            ENSEntityNotificationLog entityNotificationLog = new ENSEntityNotificationLog(entityNotification);
            entityNotificationLog.isProcessed = false;
            ensCollection.InsertOne(entityNotificationLog);
        }

        public string GetENSNotification(string clientUId, string entityUId, string fromDate)
        {
            List<ENSEntityNotificationLog> eNSEntityNotificationLogs = new List<ENSEntityNotificationLog>();
            var ensCollection = _database.GetCollection<ENSEntityNotificationLog>(CONSTANTS.ENSEntityNotificationLog);

            var filterBuilder = Builders<ENSEntityNotificationLog>.Filter;

            var filter = filterBuilder.Where(x => x.ClientUId == clientUId);

            if (entityUId != null)
                filter = filter & filterBuilder.Where(x => x.EntityUId == entityUId);

            //if(fromDate != null)
            //{                
            //    string date = DateTime.Parse(fromDate).ToString();
            //    filter = filter & filterBuilder.Gte(x => x.CreatedOn, date);
            //}

            var projection = Builders<ENSEntityNotificationLog>.Projection.Exclude("_id");

            var result = ensCollection.Find(filter).Project<ENSEntityNotificationLog>(projection).ToList();

            if (result.Count > 0)
            {
                if (fromDate != null)
                {
                    DateTime date = DateTime.Parse(fromDate);
                    foreach (var rec in result)
                    {
                        DateTime recDate = DateTime.Parse(rec.CreatedOn);
                        if (recDate >= date)
                        {
                            eNSEntityNotificationLogs.Add(rec);
                        }
                    }
                }
                if (eNSEntityNotificationLogs.Count > 0)
                {
                    return JsonConvert.SerializeObject(eNSEntityNotificationLogs);
                }
                else
                {
                    return "No Records Found";
                }
            }

            return "No Records Found";

        }
        public dynamic GetAccountClientDeliveryConstructsSearch(string token, string ClientUId, string DeliveryConstructUId, string Email, string SearchStr)
        {
            string ClientResult = "";
            HttpClient httpClient;
            if (appSettings.authProvider.ToUpper() == "FORM" || appSettings.authProvider.ToUpper() == "AZUREAD")
            {
                httpClient = new HttpClient();
            }
            else
            {
                HttpClientHandler hnd = new HttpClientHandler();
                hnd.UseDefaultCredentials = true;
                httpClient = new HttpClient(hnd);
            }
            userEmail = Email;
            SetHeader(token, httpClient);
            var tipAMURL = String.Format("GetAccountClientDeliveryConstructsSearch?clientUId=" + ClientUId + "&deliveryConstructUId=null&email=" + Email + "&searchStr=" + SearchStr + "&queryMode=LIMIT");
            HttpContent content = new StringContent(string.Empty);
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            var result = httpClient.GetAsync(tipAMURL).Result;

            if (result.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception($"{result.Content}" + "" + $"{result.StatusCode}");
            }
            var result1 = result.Content.ReadAsStringAsync().Result;
            if (result1 != null && result1.ToList().Count > 0)
            {
                var rootObject = JsonConvert.DeserializeObject<dynamic>(result1);
                return rootObject;
            }
            return ClientResult;
        }

        public string GetDecimalPointPlacesValue(string token, string ClientUID, string DeliveryConstructUId,string UserEmail)
        {
            string ClientResult = "";
            HttpClient httpClient;
            if (appSettings.authProvider.ToUpper() == "FORM" || appSettings.authProvider.ToUpper() == "AZUREAD")
            {
                httpClient = new HttpClient();
            }
            else
            {
                HttpClientHandler hnd = new HttpClientHandler();
                hnd.UseDefaultCredentials = true;
                httpClient = new HttpClient(hnd);
            }

            userEmail = UserEmail;
            SetHeader(token, httpClient);
          
            var tipAMURL = String.Format("DeliveryConstructs?clientUId=" + ClientUID + "&deliveryConstructUId="+ DeliveryConstructUId );
            HttpContent content = new StringContent(string.Empty);
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            var result = httpClient.GetAsync(tipAMURL).Result;
            if (result.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception($"{result.Content}" + "" + $"{result.StatusCode}");
            }

            var result1 = result.Content.ReadAsStringAsync().Result;
            if (result1 != null && result1.ToList().Count > 0)
            {
                var rootObject = JsonConvert.DeserializeObject<DeliveryConstructResponse>(result1);
                List<object> oDeliveryConstructs = new List<object>();
                foreach (DeliveryConstruct dc in rootObject.DeliveryConstructs) 
                {
                    dynamic dc1 = new System.Dynamic.ExpandoObject();
                    dc1.DeliveryConstructUId = dc.DeliveryConstructId;
                    dc1.ClientUId = dc.ClientUId;
                    dc1.Name = dc.Name;
                    dc1.DecimalPlaces = dc.DecimalPlaces;
                    oDeliveryConstructs.Add(dc1);           
                }
                ClientResult = JsonConvert.SerializeObject(oDeliveryConstructs);
                return ClientResult;
            }
            return ClientResult;
        }

        public async Task<dynamic> GetScopeSelectorData(string clientUId,string deliveryConstructUID, string userEmail)
        {
            return await GetScopeSelectorDataFromSaaS(clientUId,deliveryConstructUID, userEmail);
        }

        private async Task<dynamic> GetScopeSelectorDataFromSaaS(string clientUId,string deliveryConstructUID, string userEmail)
        { 
            if(string.IsNullOrEmpty(clientUId) || string.IsNullOrWhiteSpace(clientUId))
            {
                throw new Exception("Empty client id to get scope selector data");
            }

            var result = new Dictionary<string, string>();         

            var filterBuilder = Builders<BsonDocument>.Filter;
            var saasProvisionDetailsCollection = _database.GetCollection<BsonDocument>("SAASProvisionDetails");
            var saasProvisoinFilter = filterBuilder.Eq(nameof(SAASProvisionDetails.ClientUID), clientUId) &
                filterBuilder.Eq(nameof(SAASProvisionDetails.DeliveryConstructUID), deliveryConstructUID);
            var saasProvisionData = await saasProvisionDetailsCollection.Find(saasProvisoinFilter).Project<SAASProvisionDetails>(null).FirstOrDefaultAsync();
            
            if(saasProvisionData == null)
            {
                throw new Exception("No Client and DC data found for the given id");
            }

            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.DB_CAMCONFIGURATION);
           // var filter = filterBuilder.Eq(nameof(ATRProvisionRequestDto.E2EUId), saasProvisionData.E2EUID);
            var filter = filterBuilder.Eq(nameof(ATRProvisionRequestDto.E2EUId), saasProvisionData.E2EUID);
            var client = await collection.Find(filter).Project<ATRProvisionRequestDto>(null).FirstOrDefaultAsync();

            string baseUrl = string.Empty;
            if(client != null)
            {
                var baseUri = new Uri(client.API_Token_Generation);
                baseUrl = $"{baseUri.Scheme}://{baseUri.Host}/";
            }
            else
            {
                throw new Exception("No CAM data found for the given id");
            }
           
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            string token = string.Empty;
            using(var httpTokenClient = new HttpClient())
            {
                httpTokenClient.BaseAddress = new Uri(client.API_Token_Generation);
                httpTokenClient.DefaultRequestHeaders.Add("username", client.Username);
                string decryptedPass = DecryptDatafabricPassword(client.Password);
                httpTokenClient.DefaultRequestHeaders.Add("password", decryptedPass);
                //httpTokenClient.DefaultRequestHeaders.Add("password", client.Password);
                var tokenContent = new StringContent(JsonConvert.SerializeObject(null), Encoding.UTF8, "application/json");
                var httpTokenResponse = await httpTokenClient.PostAsync("", tokenContent);
                if(httpTokenResponse.IsSuccessStatusCode)
                {
                    var response = await httpTokenResponse.Content.ReadAsStringAsync();
                    token = JsonConvert.DeserializeObject<Dictionary<string, string>>(response)["token"];
                }
                else
                {
                    throw new Exception($"{httpTokenResponse.Content}" + "" + $"{httpTokenResponse.StatusCode}");
                }
            }

            if(!string.IsNullOrWhiteSpace(token) && !string.IsNullOrEmpty(token))
            {   
                string dcDataUrl = $"{baseUrl}{appSettings.SaaSDCAPIPath}/{deliveryConstructUID}";
                using(var httpDCClient = new HttpClient())
                {
                    SetPAMHeader(token, httpDCClient, dcDataUrl);
                    var httpDCResponse = await httpDCClient.GetAsync(string.Empty);
                    if(httpDCResponse.IsSuccessStatusCode)
                    {
                        var responseContent = await httpDCResponse.Content.ReadAsStringAsync();
                        var content = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseContent);
                        result.Add("ClientName", saasProvisionData.ClientName);
                        result.Add("E2EName", saasProvisionData.E2EName);
                        result.Add("DeliveryConstructName", content["name"].ToString());
                        return result;
                    }
                    else
                    {
                        throw new Exception($"{httpDCResponse.Content}" + "" + $"{httpDCResponse.StatusCode}");
                    }
                }
            }
            else
            {
                throw new Exception("Token is empty to pull scope selector data");
            }            
        }

        private string DecryptDatafabricPassword(string inputText)
        {
            try
            {
                inputText = inputText.Replace(" ", "+").Replace("\"", "");
                byte[] stringBytes = Convert.FromBase64String(inputText);
                using(Aes IOMEncryptor = Aes.Create())
                {
                    Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes("0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ", new byte[] {
                    0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                    IOMEncryptor.Key = pdb.GetBytes(32);
                    IOMEncryptor.IV = pdb.GetBytes(16);
                    using(MemoryStream ms = new MemoryStream())
                    {
                        using(CryptoStream cs = new CryptoStream(ms, IOMEncryptor.CreateDecryptor(), CryptoStreamMode.Write))
                        {
                            cs.Write(stringBytes, 0, stringBytes.Length);
                            cs.Close();
                        }
                        inputText = Encoding.Unicode.GetString(ms.ToArray());
                    }
                }
            }

            catch(FormatException ex)
            {
                inputText = "";
                throw ex;
            }
            return inputText;
        }

    }
}
