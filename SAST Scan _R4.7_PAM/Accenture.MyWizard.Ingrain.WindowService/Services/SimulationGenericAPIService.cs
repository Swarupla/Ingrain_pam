using MongoDB.Driver;
using System;
using System.Collections.Generic;
using WINSERVICEMODELS = Accenture.MyWizard.Ingrain.WindowService.Models;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
using DATAACCESS = Accenture.MyWizard.Ingrain.WindowService;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net;
using RestSharp;
using Accenture.MyWizard.Ingrain.WindowService.Interfaces;
using Accenture.MyWizard.Shared.Helpers;
using MongoDB.Bson;
using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Accenture.MyWizard.Cryptography.EncryptionProviders;
using Newtonsoft.Json.Linq;
using System.Linq;
using JsonConvert = Newtonsoft.Json.JsonConvert;
using CryptographyHelper = Accenture.MyWizard.Cryptography;

namespace Accenture.MyWizard.Ingrain.WindowService.Services
{
    public class SimulationGenericAPIService : ISimulationGenericAPICall
    {

        private readonly WINSERVICEMODELS.AppSettings appSettings = null;
        private IMongoDatabase _database;
        private DATAACCESS.DatabaseProvider databaseProvider;
        private WebHelper webHelper;
        private TokenService _tokenService;
        private string _aesKey;
        private string _aesVector;
        static CryptographyHelper.Utility.CryptographyUtility CryptographyUtility = null;

        public SimulationGenericAPIService()
        {
            appSettings = AppSettingsJson.GetAppSettings().GetSection("AppSettings").Get<WINSERVICEMODELS.AppSettings>();
            databaseProvider = new DATAACCESS.DatabaseProvider();
            var dataBaseName = MongoUrl.Create(appSettings.connectionString).DatabaseName;
            MongoClient mongoClient = databaseProvider.GetDatabaseConnection();
            _database = mongoClient.GetDatabase(dataBaseName);
            _tokenService = new TokenService();
            webHelper = new WebHelper();
            _aesKey = AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("aesKey").Value;
            _aesVector = AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("aesVector").Value;
            CryptographyUtility = new CryptographyHelper.Utility.CryptographyUtility();
        }

        public void SimulationGenericAPICall()
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(SimulationGenericAPIService), "SimulationGenericAPICall", "Simulation - Call to Generic API" + DateTime.Now.ToString(), string.Empty, string.Empty, string.Empty, string.Empty);
            var token = getToken();
            try
            {
                string contentType = "application/json";
                string SPPPredictionRequestData = GetPredictionData(appSettings.SPPPredictionCorrelationId);

                string baseAddress = appSettings.SPPGenericAPIUrl + "CorrelationId=" + appSettings.SPPPredictionCorrelationId;
                var formContent = new FormUrlEncodedContent(new[]
                {
                new KeyValuePair<string,string>("PredictionRequestData",SPPPredictionRequestData),
                new KeyValuePair<string,string>("IngrainPredictionResponseCallBackUrl",appSettings.SPPPredictionCallBackUrl)
            });

                if (appSettings.authProvider.ToUpper() == "FORM" || appSettings.authProvider.ToUpper() == "AZUREAD")
                {
                    using (var Client = new HttpClient())
                    {
                        Client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                        Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(contentType));
                        HttpResponseMessage httpResponse = Client.PostAsync(baseAddress, formContent).Result;
                        if (httpResponse.IsSuccessStatusCode)
                        {
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(SimulationGenericAPIService), "SimulationGenericAPICall", "Call to Generic API SUCCESSFUL" + DateTime.Now.ToString(), string.Empty, string.Empty, string.Empty, string.Empty);
                        }
                        else
                        {
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(SimulationGenericAPIService), "SimulationGenericAPICall", "Call to Generic API is UNSUCCESSFUL" + DateTime.Now.ToString(), string.Empty, string.Empty, string.Empty, string.Empty);
                        }
                    }
                }
                else
                {
                    HttpClientHandler hnd = new HttpClientHandler();
                    hnd.UseDefaultCredentials = true;
                    using (var Client = new HttpClient(hnd))
                    {

                        Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(contentType));
                        HttpResponseMessage httpResponse = Client.PostAsync(baseAddress, null).Result;
                        if (httpResponse.IsSuccessStatusCode)
                        {
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(SimulationGenericAPIService), "SimulationGenericAPICall", "Call to Generic API SUCCESSFUL" + DateTime.Now.ToString(), string.Empty, string.Empty, string.Empty, string.Empty);
                        }
                        else
                        {
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(SimulationGenericAPIService), "SimulationGenericAPICall", "Call to Generic API is UNSUCCESSFUL" + DateTime.Now.ToString(), string.Empty, string.Empty, string.Empty, string.Empty);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(SimulationGenericAPIService), nameof(SimulationGenericAPICall), ex.StackTrace + "Exception" + ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
        }

        private string getToken()
        {
            dynamic token = string.Empty;
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
            return token;
        }

        public string GetPredictionData(string CorrelationId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(SimulationGenericAPIService), "GetPredictionData", "GetPredictionData For FlaskAPI Call", string.Empty, string.Empty, string.Empty, string.Empty);
            string SPPPredictionRequestData = string.Empty;
            var DeployedModelsCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, CorrelationId) & Builders<BsonDocument>.Filter.Eq(CONSTANTS.Status, "Deployed");
            var Projection = Builders<BsonDocument>.Projection.Include("InputSample").Exclude("_id");
            var Response = DeployedModelsCollection.Find(filter).Project<BsonDocument>(Projection).ToList();
            if (Response.Count > 0)
            {
                if(appSettings.IsAESKeyVault)
                    SPPPredictionRequestData = CryptographyUtility.Decrypt(Response[0][CONSTANTS.InputSample].AsString);
                else
                    SPPPredictionRequestData = AesProvider.Decrypt(Response[0][CONSTANTS.InputSample].AsString, _aesKey, _aesVector);
            }
            return SPPPredictionRequestData;
        }
    }
}