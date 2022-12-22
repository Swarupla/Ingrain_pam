#region Copyright (c) 2018 Accenture . All rights reserved.

// <copyright company="Accenture">
// Copyright (c) 2018 Accenture.  All rights reserved.
// Reproduction or transmission in whole or in part, in any form or by any means, 
// electronic, mechanical or otherwise, is prohibited without the prior written 
// consent of the copyright owner.
// </copyright>

#endregion

#region AssetUsageTrackingService Information
/********************************************************************************************************\
Module Name     :   AssetUsageTrackingService
Project         :   Accenture.MyWizard.SelfServiceAI.BusinessDomain
Organisation    :   Accenture Technologies Ltd.
Created By      :   Thanyaasri Manickam
Created Date    :   20-Jan-2020
Revision History :
Revision No             Initial             Modified Date           Description
1.0                     An                  20-Jan-2020             
\********************************************************************************************************/
#endregion


namespace Accenture.MyWizard.Ingrain.BusinessDomain.Services
{
    #region Namespace References
    using Accenture.MyWizard.Ingrain.BusinessDomain.Common;
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
    using System.Configuration;
    using System.Linq;
    using System.Text;
    using System.Web;
    using Microsoft.AspNetCore.Http;
    using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
    using System.IO;
    using System.Collections.Specialized;
    using System.Threading;
    using System.Net.Http;
    using System.Net;
    using RestSharp;
    using System.Net.Http.Headers;
    using Accenture.MyWizard.Cryptography.EncryptionProviders;
    using CryptographyHelper = Accenture.MyWizard.Cryptography;

    // using Microsoft.Extensions.DependencyInjection;
    #endregion

    public class AssetUsageTrackingService : IAssetUsageTrackingData
    {
        #region Members
        private MongoClient _mongoClient;
        private IMongoDatabase _database;
        private readonly DatabaseProvider databaseProvider;

        private AssetUsageTrackingData data = new AssetUsageTrackingData();

        //        public static IScopeSelectorService _iScopeSelectorService { set; get; }

        //       public static IPhoenixTokenService _iPhoenixTokenService { get; set; }
        private IngrainAppSettings appSettings { get; set; }
        static CryptographyHelper.Utility.CryptographyUtility CryptographyUtility = null;
        #endregion

        #region Constructors
        public AssetUsageTrackingService(DatabaseProvider db, IOptions<IngrainAppSettings> settings, IServiceProvider serviceProvider)
        {
            databaseProvider = db;
            appSettings = settings.Value;
            _mongoClient = databaseProvider.GetDatabaseConnection();
            var dataBaseName = MongoUrl.Create(appSettings.connectionString).DatabaseName;
            _database = _mongoClient.GetDatabase(dataBaseName);
            //  _iScopeSelectorService = serviceProvider.GetService<IScopeSelectorService>();
            //  _iPhoenixTokenService = serviceProvider.GetService<IPhoenixTokenService>();
            CryptographyUtility = new CryptographyHelper.Utility.CryptographyUtility();
        }
        #endregion

        #region Methods
        public AssetUsageTrackingData GetUserTrackingDetails(Guid clientID, string userId, Guid dcID, string dcName, string features, string subFeatures, string ApplicationURL, string UserUniqueId, string Environment, string End2EndId, string IPAddress = "", string Browser = "", string ScreenResolution = "")
        {

            Dictionary<int, string> UsageType = new Dictionary<int, string>() { { 1, CONSTANTS.CreateNewModel },{2,CONSTANTS.Browse },{3,CONSTANTS.Usecasedefinition},
                                                                                {4,CONSTANTS.Datatransformation },{5,CONSTANTS.FeatureSelection},{6,CONSTANTS.RecommendedAIs},{7,CONSTANTS.TeachandTest},{8,CONSTANTS.PublishModels},
                                                                                {9,CONSTANTS.SaveAs },{10,CONSTANTS.DeploySource},{11,CONSTANTS.instaML},{12,CONSTANTS.DerivedFeaturesAddFeatures},{13,CONSTANTS.instaMLRegression},{14,CONSTANTS.GenericInstaML}
                                                                         , {15,CONSTANTS.CreateCustomModel}    };
            string AppUsageType = "";

            if ((UsageType.ContainsValue(features.ToUpper()) == true) || (UsageType.ContainsValue(subFeatures.ToUpper()) == true))
            {
                AppUsageType = CONSTANTS.Extended;
            }
            else
            {
                AppUsageType = CONSTANTS.Basic;
            }
            data.Environment = Environment;
            //if (data.Environment == "FDS")
            if (data.Environment != "PAD")
            {
                data.UserUniqueId = UserUniqueId;
                data.End2EndId = End2EndId;
                data.Status = CONSTANTS.Completed;
            }
            else
            {
                data.Status = CONSTANTS.New;
                data.UserUniqueId = Guid.NewGuid().ToString();
            }
            data.ApplicationName = CONSTANTS.MyWizardIngrain;
            data.Description = CONSTANTS.Null;
            data.ClientUId = clientID;
            data.DeliveryConstructUId = dcID;
            data.FeatureName = features;
            data.SubFeatureName = subFeatures;
            data.UserName = userId;
            data.AppServiceUId = CONSTANTS.AppServiceUId;
            data.Tags = CONSTANTS.Null;
            data.ApplicationURL = ApplicationURL;
            data.IPAddress = IPAddress;
            data.Browser = Browser;
            data.ScreenResolution = ScreenResolution;
            data.UsageType = AppUsageType;
            data.Language = CONSTANTS.English;
            data.RowStatusUId = CONSTANTS.RowStatusUId;
            data.CreatedByUser = appSettings.isForAllData ? (appSettings.IsAESKeyVault ? CryptographyUtility.Encrypt(CONSTANTS.SYSTEM) : AesProvider.Encrypt(CONSTANTS.SYSTEM, appSettings.aesKey, appSettings.aesVector)) : CONSTANTS.SYSTEM;
            data.CreatedByApp = CONSTANTS.MyWizardIngrain;
            data.CreatedOn = DateTime.UtcNow.ToString("o");
            data.ModifiedByUser = appSettings.isForAllData ? (appSettings.IsAESKeyVault ? CryptographyUtility.Encrypt(CONSTANTS.SYSTEM) : AesProvider.Encrypt(CONSTANTS.SYSTEM, appSettings.aesKey, appSettings.aesVector)) : CONSTANTS.SYSTEM;
            data.ModifiedByApp = CONSTANTS.MyWizardIngrain;
            data.ModifiedOn = DateTime.UtcNow.ToString("o");
            // data.Status = CONSTANTS.New;
            data.RetryCount = 0;


            if (clientID != default(Guid) && dcID != default(Guid))
            {
                if (appSettings.Environment == CONSTANTS.PADEnvironment)
                {
                    try
                    {
                        HttpResponseMessage httpResponse = UpdateIntoPhoenixDB(data);

                        if (httpResponse.IsSuccessStatusCode)
                        {
                            data.ModifiedOn = DateTime.UtcNow.ToString("o");
                            data.Status = CONSTANTS.Completed;
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(ex.Message);
                    }
                }

                var collection = _database.GetCollection<BsonDocument>(CONSTANTS.AssetUsageTracking);
                var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(data);
                var insertDocument = BsonSerializer.Deserialize<BsonDocument>(jsonData);
                collection.InsertOne(insertDocument);
            }

            data.CreatedByUser = CONSTANTS.SYSTEM;
            data.ModifiedByUser = CONSTANTS.SYSTEM;
            return (data);
        }

        private HttpResponseMessage UpdateIntoPhoenixDB(AssetUsageTrackingData data)
        {
            var token = getToken();
            if (appSettings.Environment == CONSTANTS.PAMEnvironment)
            {
                token = token["token"].ToString();
            }

            string contentType = "application/json";
            string baseAddress = appSettings.AssetUsageUrl + "clientUId=" + data.ClientUId + "&deliveryConstructUId=" + data.DeliveryConstructUId;
            var Request = JsonConvert.SerializeObject(data);
            if (appSettings.authProvider.ToUpper() == "FORM" || appSettings.authProvider.ToUpper() == "AZUREAD")
            {
                using (var Client = new HttpClient())
                {
                    Client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                    Client.DefaultRequestHeaders.Add("CorrelationUId", appSettings.CorrelationUId);
                    Client.DefaultRequestHeaders.Add("AppServiceUId", appSettings.AppServiceUID);
                    Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(contentType));

                    HttpContent httpContent = new StringContent(Request, Encoding.UTF8, "application/json");
                    HttpResponseMessage httpResponse = Client.PostAsync(baseAddress, httpContent).Result;

                    return httpResponse;
                }
            }
            else
            {
                HttpClientHandler hnd = new HttpClientHandler();
                hnd.UseDefaultCredentials = true;
                using (var Client = new HttpClient(hnd))
                {

                    Client.DefaultRequestHeaders.Add("CorrelationUId", appSettings.CorrelationUId);
                    Client.DefaultRequestHeaders.Add("AppServiceUId", appSettings.AppServiceUID);
                    Client.DefaultRequestHeaders.Add("UserEmailId", appSettings.username);
                    Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(contentType));

                    HttpContent httpContent = new StringContent(Request, Encoding.UTF8, "application/json");
                    HttpResponseMessage httpResponse = Client.PostAsync(baseAddress, httpContent).Result;
                    return httpResponse;
                }
            }
        }

        private dynamic getToken()
        {
            dynamic token = string.Empty;
            if (appSettings.Environment == CONSTANTS.PAMEnvironment)
            {
                token = this.GeneratePAMToken();
            }
            else
            {
                if (appSettings.authProvider.ToUpper() == "FORM")
                {
                    using (var httpClient = new HttpClient())
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
            }
            return token;
        }

        #endregion

        /// <summary>
        /// Asset Usage DashBoard
        /// </summary>
        /// <param name="fromDate"></param>
        /// <param name="todate"></param>
        /// <returns></returns>
        public FeatureAssetUsage AssetUsageDashBoard(string fromDate, string todate)
        {
            //fromdate
            var fromdateFormat = fromDate;
            DateTime fromDateDateFormat = DateTime.Parse(fromdateFormat);
            //fromDateDateFormat = fromDateDateFormat.AddDays(-1);
            var fromDateDBFormat = fromDateDateFormat.ToString(CONSTANTS.DateFormat);

            //todate
            var todateFormat = todate;
            DateTime todateDateFormat = DateTime.Parse(todateFormat);
            todateDateFormat = todateDateFormat.AddDays(1);
            var todateDBFormat = todateDateFormat.ToString(CONSTANTS.DateFormat);
            FeatureAssetUsage featureAssetUsage = new FeatureAssetUsage();
            var logCollection = _database.GetCollection<CallBackErrorLog>(CONSTANTS.AuditTrailLog);
            var filterBuilder = Builders<CallBackErrorLog>.Filter;
            var filter = filterBuilder.Eq(CONSTANTS.UsageType, CONSTANTS.AssetUsage) &
                         filterBuilder.Gt(CONSTANTS.CreatedOn, fromDateDBFormat) &
                         filterBuilder.Lt(CONSTANTS.CreatedOn, todateDBFormat);
            var Projection = Builders<CallBackErrorLog>.Projection.Exclude("_id");
            Dictionary<string, int> featureList = new Dictionary<string, int>();
            Dictionary<string, int> featureCountList = new Dictionary<string, int>();
            Dictionary<string, int> appCountList = new Dictionary<string, int>();
            Dictionary<string, Dictionary<string, int>> appwiseFeatureCount = new Dictionary<string, Dictionary<string, int>>();
            List<string> VDSFetureList = new List<string> { CONSTANTS.AutomatedWorkFlow, CONSTANTS.InstaMLRegressionFeature, CONSTANTS.InstaMLTimeseriesFeature, CONSTANTS.SimulationAnalytics };
            Dictionary<string, int> trainingList = new Dictionary<string, int>();
            Dictionary<string, int> predictionList = new Dictionary<string, int>();
            Dictionary<string, int> clientList = new Dictionary<string, int>();
            Dictionary<string, int> dcList = new Dictionary<string, int>();
            Dictionary<string, int> vdsClientList = new Dictionary<string, int>();
            Dictionary<string, int> appClientList = new Dictionary<string, int>();
            Dictionary<string, int> appFeatureClientList = new Dictionary<string, int>();
            Dictionary<string, Dictionary<string, int>> vdsAppwiseClientCount = new Dictionary<string, Dictionary<string, int>>();
            Dictionary<string, Dictionary<string, int>> appwiseClienteCount = new Dictionary<string, Dictionary<string, int>>();
            Dictionary<string, Dictionary<string, int>> appwiseFeatureClientCount = new Dictionary<string, Dictionary<string, int>>();
            Dictionary<string, int> DCsList = new Dictionary<string, int>();
            Dictionary<string, Dictionary<string, int>> clientDCList = new Dictionary<string, Dictionary<string, int>>();
            Dictionary<string, Dictionary<string, Dictionary<string, int>>> appClientDCCount = new Dictionary<string, Dictionary<string, Dictionary<string, int>>>();
            int DCCount = 1;
            Dictionary<string, int> TrainingDCList = new Dictionary<string, int>();
            Dictionary<string, int> PredictionDCList = new Dictionary<string, int>();
            Dictionary<string, Dictionary<string, int>> AppTrainingDCList = new Dictionary<string, Dictionary<string, int>>();
            Dictionary<string, Dictionary<string, int>> AppPredictionDCList = new Dictionary<string, Dictionary<string, int>>();
            var logResult = logCollection.Find(filter).Project<CallBackErrorLog>(Projection).ToList();
            if (logResult.Count > 0)
            {
                var applicationName = logResult.Select(x => x.ApplicationName).Distinct().ToList();
                var featureName = logResult.Select(x => x.FeatureName).Distinct().ToList();

                for (int i = 0; i < featureName.Count(); i++)
                {
                    //var count = 0;
                    if (featureName[i] != null)
                    {
                        if (featureName[i] == CONSTANTS.AutomatedWorkFlow || featureName[i] == CONSTANTS.InstaMLRegressionFeature || featureName[i] == CONSTANTS.InstaMLTimeseriesFeature || featureName[i] == CONSTANTS.SimulationAnalytics)
                        {
                            vdsClientList = new Dictionary<string, int>();
                            var count = logResult.Where(item => item.FeatureName == featureName[i]);//.Count();
                            featureCountList.Add(featureName[i], count.Count());
                            var clientCount = count.GroupBy(i => i.ClientId);
                            var featureClientList = count.GroupBy(i => i.ClientId).ToList();
                            foreach (var client in clientCount)
                            {
                                if (client.Key != null)
                                {

                                    var data = featureClientList;
                                    //   var dc = data.Find(i => i.Key == client.Key).FirstOrDefault();
                                    // var token = _iPhoenixTokenService.GenerateToken();
                                    var clientName = string.Empty;
                                    //try
                                    //{
                                    //    clientName = _iScopeSelectorService.ClientNameByClientUId(token, new Guid(client.Key), dc.DCID, dc.CreatedBy);
                                    //}
                                    //catch (Exception)
                                    //{

                                    clientName = client.Key;
                                    // }
                                    if (!vdsClientList.ContainsKey(clientName))
                                    {
                                        vdsClientList.Add(clientName, client.Count());
                                    }
                                }
                            }
                            vdsAppwiseClientCount.Add(featureName[i], vdsClientList);
                        }
                        else
                        {
                            appFeatureClientList = new Dictionary<string, int>();
                            var count = logResult.Where(item => item.FeatureName == featureName[i]);//.Count();
                                                                                                    //  featureCountList.Add(featureName[i], count.Count());
                            var clientCount = count.GroupBy(i => i.ClientId);
                            var featureClientList = count.GroupBy(i => i.ClientId).ToList();
                            foreach (var client in clientCount)
                            {
                                if (client.Key != null)
                                {

                                    // var data = featureClientList;
                                    //   var dc = data.Find(i => i.Key == client.Key).FirstOrDefault();
                                    //  var token = _iPhoenixTokenService.GenerateToken();
                                    var clientName = string.Empty;
                                    //try
                                    //{
                                    //    clientName = _iScopeSelectorService.ClientNameByClientUId(token, new Guid(client.Key), dc.DCID, dc.CreatedBy);
                                    //}
                                    //catch (Exception)
                                    //{

                                    clientName = client.Key;
                                    //  }
                                    if (!appFeatureClientList.ContainsKey(clientName))
                                    {
                                        appFeatureClientList.Add(clientName, client.Count());
                                    }
                                }
                            }
                            appwiseFeatureClientCount.Add(featureName[i], appFeatureClientList);
                        }
                    }
                }
                var VDSFeature = featureCountList.Keys;
                var featureListVDS = VDSFetureList.Except(VDSFeature).ToList();
                foreach (var item in featureListVDS)
                {
                    featureCountList.Add(item, 0);
                }

                for (int i = 0; i < applicationName.Count(); i++)
                {
                    if (applicationName[i] != null)
                    {
                        clientDCList = new Dictionary<string, Dictionary<string, int>>();
                        DCsList = new Dictionary<string, int>();
                        TrainingDCList = new Dictionary<string, int>();
                        PredictionDCList = new Dictionary<string, int>();
                        appClientList = new Dictionary<string, int>();
                        var appCount = logResult.Where(item => item.ApplicationName == applicationName[i]);//.Count();
                        appCountList.Add(applicationName[i], appCount.Count());
                        var clientCount = appCount.GroupBy(i => i.ClientId);

                        var featureClientList = appCount.GroupBy(i => i.ClientId).ToList();
                        foreach (var client in clientCount)
                        {
                            if (client.Key != null)
                            {

                                //var data = featureClientList;
                                // var dc = data.Find(i => i.Key == client.Key).FirstOrDefault();

                                // var token = _iPhoenixTokenService.GenerateToken();
                                var clientName = string.Empty;
                                //try
                                //{
                                //    clientName = _iScopeSelectorService.ClientNameByClientUId(token, new Guid(client.Key), dc.DCID, dc.CreatedBy);
                                //}
                                //catch (Exception)
                                //{

                                clientName = client.Key;
                                // }
                                if (!appClientList.ContainsKey(clientName))
                                {
                                    appClientList.Add(clientName, client.Count());
                                }
                                DCsList = new Dictionary<string, int>();
                                foreach (var value in client)
                                {
                                    if (value.DCID != null)
                                    {
                                        if (DCsList.Count == 0)
                                        {
                                            DCCount = 1;
                                        }
                                        if (DCsList.ContainsKey(value.DCID))
                                        {
                                            DCCount++;
                                            DCsList[value.DCID] = DCCount;
                                        }
                                        else
                                        {
                                            DCsList.Add(value.DCID, DCCount);

                                        }
                                    }
                                }
                                foreach (var dcData in DCsList)
                                {

                                    if (dcData.Key != null && client.Key != null)
                                    {
                                        var trainingCount = appCount.Count(item => (item.ProcessName != null && item.ProcessName.Contains(CONSTANTS.TrainingName)) && (item.DCID != null && item.DCID.Contains(dcData.Key)) && (item.ClientId != null && item.ClientId.Contains(client.Key)));
                                        if (!TrainingDCList.ContainsKey(dcData.Key))
                                        {
                                            TrainingDCList.Add(dcData.Key, trainingCount);
                                        }

                                        var predictionCount = appCount.Count(item => (item.ProcessName != null && item.ProcessName.Contains(CONSTANTS.PredictionName)) && (item.DCID != null && item.DCID.Contains(dcData.Key)) && (item.ClientId != null && item.ClientId.Contains(client.Key)));
                                        if (!PredictionDCList.ContainsKey(dcData.Key))
                                        {
                                            PredictionDCList.Add(dcData.Key, predictionCount);
                                        }
                                    }
                                }
                                if (!clientDCList.ContainsKey(client.Key))
                                {
                                    clientDCList.Add(client.Key, DCsList);
                                }

                            }
                        }
                        appClientDCCount.Add(applicationName[i], clientDCList);
                        appwiseClienteCount.Add(applicationName[i], appClientList);
                        AppTrainingDCList.Add(applicationName[i], TrainingDCList);
                        AppPredictionDCList.Add(applicationName[i], PredictionDCList);


                    }
                }
                trainingList = new Dictionary<string, int>();
                predictionList = new Dictionary<string, int>();
                for (int i = 0; i < applicationName.Count(); i++)
                {
                    if (applicationName[i] != null)
                    {
                        appFeatureClientList = new Dictionary<string, int>();
                        featureList = new Dictionary<string, int>();

                        clientList = new Dictionary<string, int>();
                        dcList = new Dictionary<string, int>();
                        var featureNewList = logResult.Where(item => item.ApplicationName == applicationName[i]).ToList();
                        var featureGroup = featureNewList.GroupBy(i => i.FeatureName);
                        foreach (var feature in featureGroup)
                        {
                            if (feature.Key != null)
                            {
                                featureList.Add(feature.Key, feature.Count());
                            }
                        }
                        appwiseFeatureCount.Add(applicationName[i], featureList);
                        var trainingCount = featureNewList.Count(item => item.ProcessName.Contains(CONSTANTS.TrainingName));//featureNewList.GroupBy(i => i.ProcessName).Select(train=> train.Key == CONSTANTS.TrainingName).Count();
                        trainingList.Add(applicationName[i], trainingCount);
                        var predictionCount = featureNewList.Count(item => item.ProcessName.Contains(CONSTANTS.Prediction));//featureNewList.GroupBy(i => i.ProcessName == CONSTANTS.PredictionName).Count();
                        predictionList.Add(applicationName[i], predictionCount);


                    }

                }

            }
            featureAssetUsage.ApplicationWise = appCountList;
            featureAssetUsage.VDSFeatureWise = featureCountList;
            featureAssetUsage.AppIntegrationWise = appwiseFeatureCount;
            featureAssetUsage.VDSClientWise = vdsAppwiseClientCount;
            featureAssetUsage.AppClientWise = appwiseClienteCount;
            featureAssetUsage.AppFeatureClientWise = appwiseFeatureClientCount;
            featureAssetUsage.TrainingWise = trainingList;
            featureAssetUsage.PredictionWise = predictionList;
            featureAssetUsage.AppClientDCWise = appClientDCCount;
            featureAssetUsage.AppTrainingDCWise = AppTrainingDCList;
            featureAssetUsage.AppPredictionDCWise = AppPredictionDCList;
            return featureAssetUsage;
        }

        /// <summary>
        /// Custom Model Activity
        /// </summary>
        /// <param name="fromDate"></param>
        /// <param name="todate"></param>
        /// <returns></returns>
        public IngrainCoreFeature CustomModelActivity(string fromDate, string todate)
        {
            List<BsonDocument> bsonElements = new List<BsonDocument>();
            //fromdate
            var fromdateFormat = fromDate;
            DateTime fromDateDateFormat = DateTime.Parse(fromdateFormat);
            fromDateDateFormat = fromDateDateFormat.AddDays(-1);
            var fromDateDBFormat = fromDateDateFormat.ToString(CONSTANTS.DateFormat);

            //todate
            var todateFormat = todate;
            DateTime todateDateFormat = DateTime.Parse(todateFormat);
            todateDateFormat = todateDateFormat.AddDays(1);
            var todateDBFormat = todateDateFormat.ToString(CONSTANTS.DateFormat);
            IngrainCoreFeature ingrainCoreFeature = new IngrainCoreFeature();
            ingrainCoreFeature.FeatureClientDCWise = new Dictionary<string, Dictionary<string, Dictionary<string, int>>>();
            var logCollection = _database.GetCollection<AssetUsageTrackingData>(CONSTANTS.AssetUsageTracking);
            var filterBuilder = Builders<AssetUsageTrackingData>.Filter;
            var filter =
                         filterBuilder.Gt(CONSTANTS.CreatedOn, fromDateDBFormat) &
                         filterBuilder.Lt(CONSTANTS.CreatedOn, todateDBFormat);
            var Projection = Builders<AssetUsageTrackingData>.Projection.Exclude("_id").Exclude("UserUniqueId");
            var logResult = logCollection.Find(filter).Project<AssetUsageTrackingData>(Projection).ToList();
            Dictionary<string, Dictionary<string, Dictionary<string, int>>> featureClientDCCount = new Dictionary<string, Dictionary<string, Dictionary<string, int>>>();
            Dictionary<string, Dictionary<string, Dictionary<string, int>>> subfeatureClientDCCount = new Dictionary<string, Dictionary<string, Dictionary<string, int>>>();
            Dictionary<string, Dictionary<string, int>> subActivityCount = new Dictionary<string, Dictionary<string, int>>();
            Dictionary<string, int> subFeatureCount = new Dictionary<string, int>();
            Dictionary<string, int> featureClientCount = new Dictionary<string, int>();
            ingrainCoreFeature.FeatureCount = new Dictionary<string, int>();
            ingrainCoreFeature.FeatureClientCount = new Dictionary<string, Dictionary<string, int>>();
            int DCCount = 1;
            if (logResult.Count > 0)
            {
                var featureName = logResult.Select(x => x.FeatureName).Distinct().ToList();
                var createCustomFeature = logResult.Where(x => x.FeatureName == CONSTANTS.Create_CustomModel).ToList();
                var createCustomFeatureCount = createCustomFeature.Count(); // Create Custom Model Total Count
                ingrainCoreFeature.FeatureCount.Add(CONSTANTS.Create_CustomModel, createCustomFeatureCount);
                var subFeatureName = logResult.Select(x => x.SubFeatureName).Distinct().ToList();
                for (int i = 0; i < createCustomFeature.Count(); i++)
                {
                    if (createCustomFeature[i] != null)
                    {
                        if (createCustomFeature[i].SubFeatureName == CONSTANTS.ModelsCreation || createCustomFeature[i].SubFeatureName == CONSTANTS.ModelsTrained || createCustomFeature[i].SubFeatureName == CONSTANTS.ModelsPublished || createCustomFeature[i].SubFeatureName == CONSTANTS.TotalPrediction)
                        {
                            if (!subFeatureCount.ContainsKey(createCustomFeature[i].SubFeatureName))
                            {
                                var subFeatureList = createCustomFeature.Count(item => item.SubFeatureName.Contains(createCustomFeature[i].SubFeatureName));
                                subFeatureCount.Add(createCustomFeature[i].SubFeatureName, subFeatureList);
                            }
                        }


                        if (!featureClientDCCount.ContainsKey(createCustomFeature[i].FeatureName))
                        {
                            var featureClient = createCustomFeature.GroupBy(i => i.ClientUId);
                            var clientDCList = new Dictionary<string, Dictionary<string, int>>();
                            var DCsList = new Dictionary<string, int>();
                            foreach (var client in featureClient)
                            {
                                if (client.Key != null)
                                {
                                    if (!featureClientCount.ContainsKey(client.Key.ToString()))
                                    {
                                        featureClientCount.Add(client.Key.ToString(), client.Count());
                                    }
                                    DCsList = new Dictionary<string, int>();
                                    foreach (var value in client)
                                    {
                                        if (value.DeliveryConstructUId != null)
                                        {
                                            if (DCsList.Count == 0 || (!DCsList.ContainsKey(value.DeliveryConstructUId.ToString())))
                                            {
                                                DCCount = 1;
                                            }
                                            if (DCsList.ContainsKey(value.DeliveryConstructUId.ToString()))
                                            {
                                                DCCount++;
                                                DCsList[value.DeliveryConstructUId.ToString()] = DCCount;
                                            }
                                            else
                                            {
                                                DCsList.Add(value.DeliveryConstructUId.ToString(), DCCount);

                                            }
                                        }
                                    }
                                    clientDCList.Add(client.Key.ToString(), DCsList);
                                }

                            }
                            featureClientDCCount.Add(createCustomFeature[i].FeatureName, clientDCList);
                            subActivityCount.Add(createCustomFeature[i].FeatureName, subFeatureCount);
                            ingrainCoreFeature.FeatureClientCount.Add(createCustomFeature[i].FeatureName, featureClientCount);
                        }
                    }

                }

            }
            ingrainCoreFeature.FeatureClientDCWise = featureClientDCCount;
            ingrainCoreFeature.SubFeatureCount = subActivityCount;
            return ingrainCoreFeature;
        }

        public dynamic GeneratePAMToken()
        {
            dynamic tokenObj = null;
            if (appSettings.authProvider.ToUpper() == CONSTANTS.FORM)
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.BaseAddress = new Uri(appSettings.PAMTokenUrl);
                    httpClient.DefaultRequestHeaders.Accept.Clear();
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    string json = JsonConvert.SerializeObject(new
                    {
                        username = appSettings.UserNamePAM,
                        password = appSettings.PasswordPAM
                    });
                    var requestOptions = new StringContent(json, Encoding.UTF8, "application/json");


                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                    var result = httpClient.PostAsync("", requestOptions).Result;
                    if (result.StatusCode != HttpStatusCode.OK)
                    {
                        throw new Exception(string.Format(CONSTANTS.TokenError, result.StatusCode));
                    }
                    var result1 = result.Content.ReadAsStringAsync().Result;
                    tokenObj = JsonConvert.DeserializeObject(result1) as dynamic;
                    //var token = Convert.ToString(tokenObj.token);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(PhoenixTokenService), nameof(GeneratePAMToken), "GenerateToken- END", string.Empty, string.Empty, string.Empty, string.Empty);
                    return tokenObj;
                }
            }
            else if (appSettings.authProvider.ToUpper() == CONSTANTS.AZUREAD)
            {
                var client = new RestClient(appSettings.token_Url);
                var request = new RestRequest(Method.POST);
                request.AddHeader("cache-control", "no-cache");
                request.AddHeader("content-type", "application/x-www-form-urlencoded");
                request.AddParameter("application/x-www-form-urlencoded",
                "grant_type=" + appSettings.Grant_Type +
                "&client_id=" + appSettings.clientId +
                "&client_secret=" + appSettings.clientSecret +
                "&scope=" + appSettings.scopeStatus +
                "&resource=" + appSettings.resourceId,
                ParameterType.RequestBody);
                IRestResponse response = client.Execute(request);
                tokenObj = JsonConvert.DeserializeObject(response.Content) as dynamic;
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(PhoenixTokenService), nameof(GeneratePAMToken), "GenerateToken- END", string.Empty, string.Empty, string.Empty, string.Empty);
                return tokenObj;
            }

            return tokenObj;
        }

    }
}
