using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using DATAACCESS = Accenture.MyWizard.Ingrain.WindowService;
using WINSERVICEMODELS = Accenture.MyWizard.Ingrain.WindowService.Models;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
using DATAMODELS = Accenture.MyWizard.Ingrain.DataModels.Models;
using Accenture.MyWizard.Cryptography.EncryptionProviders;
using CryptographyHelper = Accenture.MyWizard.Cryptography;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Accenture.MyWizard.Ingrain.WindowService.Services
{

    public class TokenService
    {
        private readonly WINSERVICEMODELS.AppSettings appSettings = null;
        private IMongoDatabase _database;
        private DATAACCESS.DatabaseProvider databaseProvider;
        static CryptographyHelper.Utility.CryptographyUtility CryptographyUtility = null;

        public TokenService()
        {
            appSettings = AppSettingsJson.GetAppSettings().GetSection("AppSettings").Get<WINSERVICEMODELS.AppSettings>();
            databaseProvider = new DATAACCESS.DatabaseProvider();
            var dataBaseName = MongoUrl.Create(appSettings.connectionString).DatabaseName;
            MongoClient mongoClient = databaseProvider.GetDatabaseConnection();
            _database = mongoClient.GetDatabase(dataBaseName);
            CryptographyUtility = new CryptographyHelper.Utility.CryptographyUtility();
        }


        public string GeneratePADToken()
        {
            dynamic token = string.Empty;
            if (appSettings.authProvider.ToUpper() == "FORM")
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(TokenService), nameof(GeneratePADToken), CONSTANTS.START + " authProvider :FORM ", string.Empty, string.Empty, string.Empty, string.Empty);
                using (var httpClient = new HttpClient())
                {

                    //New Code
                    httpClient.BaseAddress = new Uri(appSettings.tokenAPIUrl);
                    httpClient.DefaultRequestHeaders.Accept.Clear();
                    var bodyparams = new
                    {
                        username = appSettings.UserNamePAM,
                        password = appSettings.PasswordPAM
                    };
                    string json = JsonConvert.SerializeObject(bodyparams);
                    HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
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
            else
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

        public string GenerateAppToken(string appId)
        {
            var collection = _database.GetCollection<DATAMODELS.AppIntegration>(CONSTANTS.AppIntegration);
            var filter = Builders<DATAMODELS.AppIntegration>.Filter.Eq("ApplicationID", appId);
            var projectionScenario = Builders<DATAMODELS.AppIntegration>.Projection.Include("Authentication").Include("TokenGenerationURL").Include("Credentials").Exclude("_id");
            var dbData = collection.Find(filter).Project<DATAMODELS.AppIntegration>(projectionScenario).FirstOrDefault();
            if (dbData != null)
            {
                if (dbData.Authentication == "Azure" || dbData.Authentication == "AzureAD")
                {
                    string tokenUrl = appSettings.IsAESKeyVault ? CryptographyUtility.Decrypt(dbData.TokenGenerationURL) : AesProvider.Decrypt(dbData.TokenGenerationURL, appSettings.aesKey, appSettings.aesVector);
                    dynamic credentials = JsonConvert.DeserializeObject<dynamic>(appSettings.IsAESKeyVault ? CryptographyUtility.Decrypt(dbData.Credentials) : AesProvider.Decrypt(dbData.Credentials, appSettings.aesKey, appSettings.aesVector));
                    using (var client = new HttpClient())
                    {
                        client.DefaultRequestHeaders.Accept.Clear();
                        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        var formContent = new FormUrlEncodedContent(new[]
                        {
                            new KeyValuePair<string, string>("grant_type", credentials.grant_type.ToString()),
                            new KeyValuePair<string, string>("client_id", credentials.client_id.ToString()),
                            new KeyValuePair<string, string>("client_secret", credentials.client_secret.ToString()),
                            new KeyValuePair<string, string>("resource", credentials.resource.ToString())
                        });

                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                        var tokenResult = client.PostAsync(tokenUrl, formContent).Result;
                        Dictionary<string, string> tokenDictionary = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(tokenResult.Content.ReadAsStringAsync().Result);
                        return tokenDictionary[CONSTANTS.access_token].ToString();
                    }

                }
                else if (dbData.Authentication.ToUpper() == "FORM")
                {
                    using (var httpClient = new HttpClient())
                    {
                        if (appSettings.Environment.Equals(CONSTANTS.PAMEnvironment))
                        {
                            httpClient.BaseAddress = new Uri(appSettings.TokenURLVDS);
                            httpClient.DefaultRequestHeaders.Accept.Clear();
                            var bodyparams = new
                            {
                                username = appSettings.username,
                                password = appSettings.password
                            };
                            string json = JsonConvert.SerializeObject(bodyparams);
                            HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
                            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                            var result = httpClient.PostAsync("", content).Result;

                            if (result.StatusCode != System.Net.HttpStatusCode.OK)
                            {
                                throw new Exception(string.Format("Unable to process your request for generating token. Please check your credentials and try again. Status Code: {0}", result.StatusCode));
                            }

                            var result1 = result.Content.ReadAsStringAsync().Result;
                            var tokenObj = JsonConvert.DeserializeObject(result1) as dynamic;

                            return Convert.ToString(tokenObj.token);
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

                            return Convert.ToString(tokenObj.access_token);
                        }
                    }
                }

            }

            return null;
        }

        public string GenerateToken()
        {
            dynamic token = string.Empty;
            if (appSettings.authProvider.ToUpper() == "FORM")
            {
                using (var httpClient = new HttpClient())
                {
                    if (appSettings.Environment.Equals(CONSTANTS.PAMEnvironment))
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
                }
            }
            else
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

        public string GenerateVDSPAMToken(string appId)
        {
            ////changed only for MinSpec
            dynamic token = string.Empty;
            if (appSettings.authProvider.ToUpper() == "FORM")
            {
                DataModels.InstaModels.PAMData pAMData = new DataModels.InstaModels.PAMData
                {
                    username = Convert.ToString(appSettings.UserNamePAM),
                    password = Convert.ToString(appSettings.PasswordPAM)
                };
                var tokenendpointurl = Convert.ToString(appSettings.PAMTokenUrl);
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(TokenService), nameof(GenerateVDSPAMToken), "tokenendpointurl: " + tokenendpointurl.ToString(), "", "", "", "");
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(TokenService), nameof(GenerateVDSPAMToken), "username: " + pAMData.username.ToString(), "", "", "", "");
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(TokenService), nameof(GenerateVDSPAMToken), "password: " + pAMData.password.ToString(), "", "", "", "");
                var postData = Newtonsoft.Json.JsonConvert.SerializeObject(pAMData, Formatting.None, new JsonSerializerSettings()
                {
                    DateFormatHandling = DateFormatHandling.MicrosoftDateFormat,
                    DateParseHandling = DateParseHandling.DateTimeOffset
                });
                var stringContent = new StringContent(postData, UnicodeEncoding.UTF8, CONSTANTS.APPLICATION_JSON);
                using (var client = new HttpClient())
                {
                    var tokenResponse = client.PostAsync(tokenendpointurl, stringContent).Result;
                    var tokenResult = tokenResponse.Content.ReadAsStringAsync().Result;
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(TokenService), nameof(GenerateVDSPAMToken), "tokenResult: " + tokenResult.ToString(), "", "", "", "");
                    Dictionary<string, string> tokenDictionary = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(tokenResult);
                    if (tokenDictionary != null)
                    {
                        if (tokenResponse.IsSuccessStatusCode)
                        {
                            token = tokenDictionary[CONSTANTS.token].ToString();
                        }
                        else
                        {
                            token = CONSTANTS.InvertedComma;
                        }
                    }
                    else
                    {
                        token = CONSTANTS.InvertedComma;
                    }
                    return token;
                }
            }
            else
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
                return token;
            }
        }

    }
}