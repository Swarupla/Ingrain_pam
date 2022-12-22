using System;
using MongoDB.Driver;
using Microsoft.Extensions.Configuration;
using WINSERVICEMODELS = Accenture.MyWizard.Ingrain.WindowService.Models;
using DATAACCESS = Accenture.MyWizard.Ingrain.WindowService;
using Accenture.MyWizard.Ingrain.WindowService.Services;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net;
namespace Accenture.MyWizard.Ingrain.WindowService.Services.ConstraintsHelperMethods
{
    class HttpMethodService
    {
        private readonly WINSERVICEMODELS.AppSettings appSettings = null;
        private IMongoDatabase _database;
        private DATAACCESS.DatabaseProvider databaseProvider;
        private TokenService _TokenService = null;

        public HttpMethodService()
        {
            appSettings = AppSettingsJson.GetAppSettings().GetSection("AppSettings").Get<WINSERVICEMODELS.AppSettings>();
            databaseProvider = new DATAACCESS.DatabaseProvider();
            var dataBaseName = MongoUrl.Create(appSettings.connectionString).DatabaseName;
            MongoClient mongoClient = databaseProvider.GetDatabaseConnection();
            _database = mongoClient.GetDatabase(dataBaseName);
            _TokenService = new TokenService();
        }

        public string InvokeGetMethod(string routeUrl, string appServiceUId)
        {
            string token = _TokenService.GenerateToken();
            if (!string.IsNullOrEmpty(token))
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.BaseAddress = new Uri(appSettings.myWizardAPIUrl);
                    httpClient.DefaultRequestHeaders.Accept.Clear();
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                    httpClient.DefaultRequestHeaders.Add("AppServiceUID", appServiceUId);
                    var ClinetStruct = String.Format(routeUrl);
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                    var result = httpClient.GetAsync(ClinetStruct).Result;
                    if (result.StatusCode != HttpStatusCode.OK)
                    {
                        throw new Exception("StatusCode : " + $"{result.StatusCode}" + ", Content : " + $"{result.Content}" + ", BaseAddress: " + appSettings.myWizardAPIUrl + " " + ", AppServiceUID:" + appServiceUId);
                    }

                    return result.Content.ReadAsStringAsync().Result;
                }
            }

            return token;
        }
        public HttpResponseMessage InvokePOSTRequest(string token, string baseURI, string apiPath, StringContent content, string resourceId)
        {
            return InvokePOSTRequest(token, baseURI, apiPath, content, resourceId, string.Empty);
        }
        public HttpResponseMessage InvokePOSTRequest(string token, string baseURI, string apiPath, StringContent content, string resourceId, string ProvisionedAppServiceUID)
        {
            using (var client = new HttpClient())
            {
                string uri = baseURI + apiPath;
                client.Timeout = new TimeSpan(0, 30, 0);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("AppServiceUId", appSettings.AppServiceUId);
                //if (!string.IsNullOrEmpty(ProvisionedAppServiceUID))
                //{
                //    client.DefaultRequestHeaders.Add("AppServiceUId", ProvisionedAppServiceUID);
                //}
                //else
                //{
                //    client.DefaultRequestHeaders.Add("AppServiceUId", appSettings.AppServiceUId);
                //}
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
        public HttpResponseMessage InvokePOSTRequestFromData(string token, string baseURI, string apiPath, FormUrlEncodedContent content, string resourceId)
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
                if (!string.IsNullOrEmpty(resourceId))
                {
                    client.DefaultRequestHeaders.Add("resourceId", resourceId);
                }
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                return client.PostAsync(uri, content).Result;
            }
        }
    }
}
