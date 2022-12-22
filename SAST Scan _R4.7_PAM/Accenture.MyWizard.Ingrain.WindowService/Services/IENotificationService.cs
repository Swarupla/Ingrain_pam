using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using WINSERVICEMODELS = Accenture.MyWizard.Ingrain.WindowService.Models;
using DATAACCESS = Accenture.MyWizard.Ingrain.DataAccess;
using DATAMODELS = Accenture.MyWizard.Ingrain.DataModels.InferenceEngineModel;
using System.Net.Http;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http.Headers;
using RestSharp;
using MongoDB.Bson;
using Accenture.MyWizard.Cryptography.EncryptionProviders;
using CryptographyHelper = Accenture.MyWizard.Cryptography;

namespace Accenture.MyWizard.Ingrain.WindowService.Services
{
    public class IENotificationService
    {
        private readonly WINSERVICEMODELS.AppSettings appSettings = null;
        private DATAACCESS.IInferenceEngineDBContext databaseProvider;
        private TokenService _tokenService;
        static CryptographyHelper.Utility.CryptographyUtility CryptographyUtility = null;

        public IENotificationService()
        {
            appSettings = AppSettingsJson.GetAppSettings().GetSection("AppSettings").Get<WINSERVICEMODELS.AppSettings>();
            databaseProvider = new DATAACCESS.InferenceEngineDBContext(appSettings.IEConnectionString, appSettings.certificatePath, appSettings.certificatePassKey,appSettings.aesKey,appSettings.aesVector, appSettings.IsAESKeyVault);
            _tokenService = new TokenService();
            CryptographyUtility = new CryptographyHelper.Utility.CryptographyUtility();
        }

        public List<DATAMODELS.IEAppNotificationLog> GetNotificationLog(int notificationMaxRetryCount)
        {
            return databaseProvider.IEAppIngerationRepository.GetAppNoficationLog(appSettings.NotificationMaxRetryCount);
        }

        public void SendNotification(DATAMODELS.IEAppNotificationLog appNotificationLog)
        {
            try
            {
                //LOGGING.LogManager.Logger.LogProcessInfo(typeof(PushNotificationService), nameof(SendNotification), "Sending Notifications - " + DateTime.UtcNow.ToString());
                bool notify = true;

                //if (!string.IsNullOrEmpty(appNotificationLog.ModifiedOn))
                //{
                //    DateTime lastExetime = DateTime.Parse(appNotificationLog.ModifiedOn);
                //    DateTime curDate = DateTime.UtcNow;
                //    int mins = (int)(curDate - lastExetime).TotalMinutes;

                //    if (mins < appSettings.NotificationRetryFrequencyinMnts)
                //    {
                //        notify = false;
                //    }
                //}
                if (appNotificationLog.ModifiedOn != null)
                {
                    DateTime lastExetime = appNotificationLog.ModifiedOn;
                    DateTime curDate = DateTime.Now;// for FDS
                    int mins = (int)(curDate - lastExetime).TotalMinutes;

                    if (mins < appSettings.NotificationRetryFrequencyinMnts)
                    {
                        notify = false;
                    }
                }

                if (appNotificationLog.RetryCount > appSettings.NotificationMaxRetryCount)
                    notify = false;


                if (notify)
                {
                    DATAMODELS.IENotificationDetails notificationDetails = new DATAMODELS.IENotificationDetails();
                    notificationDetails.RequestId = appNotificationLog.RequestId;
                    notificationDetails.ClientUId = appNotificationLog.ClientUId;
                    notificationDetails.DeliveryConstructUId = appNotificationLog.DeliveryConstructUId;
                    notificationDetails.ApplicationId = appNotificationLog.ApplicationId;
                    notificationDetails.NotificationEventType = appNotificationLog.NotificationEventType;
                    notificationDetails.OperationType = appNotificationLog.OperationType;
                    notificationDetails.CorrelationId = appNotificationLog.CorrelationId;
                    notificationDetails.UseCaseId = appNotificationLog.UseCaseId;
                    notificationDetails.ModelType = appNotificationLog.ModelType;
                    notificationDetails.ProblemType = appNotificationLog.ProblemType;
                    notificationDetails.CallBackLink = appNotificationLog.CallBackLink;
                    notificationDetails.UniqueId = appNotificationLog.UniqueId;
                    notificationDetails.UserId = appNotificationLog.UserId;
                    notificationDetails.FunctionalArea = appNotificationLog.FunctionalArea;
                    notificationDetails.CreatedDateTime = appNotificationLog.CreatedDateTime;
                    notificationDetails.Entity = appNotificationLog.Entity;
                    notificationDetails.UseCaseName = appNotificationLog.UseCaseName;
                    notificationDetails.UseCaseDescription = appNotificationLog.UseCaseDescription;
                    notificationDetails.Status = appNotificationLog.Status;
                    notificationDetails.StatusMessage = appNotificationLog.StatusMessage;
                    notificationDetails.ModelStatus = appNotificationLog.ModelStatus;
                    notificationDetails.Progress = appNotificationLog.Progress;
                    notificationDetails.Message = appNotificationLog.Message;
                    if (notificationDetails.UseCaseId == "6665e35b-b2d1-40cc-b28f-3b795780f34f")
                    {
                        notificationDetails.Message = appNotificationLog.StatusMessage;
                        if (notificationDetails.Status == "Error" || notificationDetails.Status == "Warning")
                        {
                            notificationDetails.ErrorMessage = appNotificationLog.StatusMessage;
                        }
                    }
                    if (notificationDetails.ApplicationId == "fa36e811-a59f-48c0-94a6-9a7ffc8bc8ab")
                        notificationDetails.Message = appNotificationLog.Message;



                    StringContent content = new StringContent(JsonConvert.SerializeObject(notificationDetails), Encoding.UTF8, "application/json");
                    string token = null;
                    if (appNotificationLog.Environment == "FDS")
                    {
                        token = _tokenService.GenerateAppToken(appNotificationLog.ApplicationId);
                    }
                    else if (appNotificationLog.Environment == "PAM")
                    {
                        token = _tokenService.GenerateVDSPAMToken(appNotificationLog.ApplicationId);
                    }
                    else
                    {
                        token = _tokenService.GeneratePADToken();
                    }
                    HttpResponseMessage httpResponseMessage = new HttpResponseMessage();

                    if (appNotificationLog.IsCascade)
                    {
                        httpResponseMessage = InvokePOSTRequest(token, appNotificationLog.AppNotificationUrl, content, true);
                    }
                    else
                    {
                        httpResponseMessage = InvokePOSTRequest(token, appNotificationLog.AppNotificationUrl, content, false);
                    }
                    if (httpResponseMessage.StatusCode == HttpStatusCode.OK)
                    {
                        if (appNotificationLog.IsCascade)
                        {
                            var res = httpResponseMessage.Content.ReadAsStringAsync();
                            DATAMODELS.IENotificationResponse notificationResponse = JsonConvert.DeserializeObject<DATAMODELS.IENotificationResponse>(res.Result);
                            if (notificationResponse.IsSuccess)
                                UpdateAppNotificationLog(appNotificationLog.RequestId, true, appNotificationLog.RetryCount + 1, notificationResponse.StatusMessage);
                            else
                                UpdateAppNotificationLog(appNotificationLog.RequestId, false, appNotificationLog.RetryCount + 1, httpResponseMessage.StatusCode.ToString() + "-" + notificationResponse.StatusMessage);
                        }
                        else
                            UpdateAppNotificationLog(appNotificationLog.RequestId, true, appNotificationLog.RetryCount + 1, httpResponseMessage.StatusCode.ToString() + "-" + "SUCCESS" + "_" + httpResponseMessage.Content.ReadAsStringAsync().Result);
                    }
                    else
                    {
                        UpdateAppNotificationLog(appNotificationLog.RequestId, false, appNotificationLog.RetryCount + 1, httpResponseMessage.StatusCode.ToString() + "-" + httpResponseMessage.ReasonPhrase);
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateAppNotificationLog(appNotificationLog.RequestId, false, appNotificationLog.RetryCount + 1, ex.Message);
            }



        }
        public void UpdateAppNotificationLog(string requestId, bool isSuccess, int retryCount, string message)
        {
            databaseProvider.IEAppIngerationRepository.UpdateAppNotificationLog(requestId, isSuccess, retryCount, message);
        }

        public HttpResponseMessage InvokePOSTRequest(string token, string apiUrl, StringContent content, bool isCascade)
        {
            using (var client = new HttpClient())
            {
                string uri = apiUrl;
                client.Timeout = new TimeSpan(0, 30, 0);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                if (isCascade)
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(PushNotificationService), "InvokePOSTRequest CONTENT---" + content.ReadAsStringAsync().Result, "END", string.Empty, string.Empty, string.Empty, string.Empty);
                    if (!string.IsNullOrWhiteSpace(token))
                    {
                        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(token))
                    {
                        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                    }
                }

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                return client.PostAsync(uri, content).Result;
            }
        }


        private string CustomUrlTokenAppId(string appId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(CustomUrlTokenAppId), "CUSTOMURLTOKENAPPID FOR APPLICATIONID--" + appId,appId,string.Empty, string.Empty, string.Empty);
            //var AppIntegCollection = _database.GetCollection<DATAMODELS.AppIntegration>(CONSTANTS.AppIntegration);
            //var filterBuilder = Builders<DATAMODELS.AppIntegration>.Filter;
            //var AppFilter = filterBuilder.Eq("ApplicationID", appId);

            //var Projection = Builders<DATAMODELS.AppIntegration>.Projection.Include("BaseURL").Include("Authentication").Include("TokenGenerationURL").Include("Credentials").Exclude("_id");
            //var AppData = AppIntegCollection.Find(AppFilter).Project<DATAMODELS.AppIntegration>(Projection).FirstOrDefault();
            var AppData = databaseProvider.IEAppIngerationRepository.GetAppDetailsOnId(appId);
            dynamic token = string.Empty;
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(CustomUrlTokenAppId), "CUSTOMURLTOKENAPPID for Application" + AppData.ApplicationName, appId, string.Empty, string.Empty, string.Empty);
            if (AppData.Authentication == "AzureAD" || AppData.Authentication == "Azure")
            {
                string GrantType = string.Empty, ClientSecert = string.Empty, ClientId = string.Empty, Resource = string.Empty;
                if (appSettings.IsAESKeyVault)
                {
                    AppData.TokenGenerationURL = CryptographyUtility.Decrypt(AppData.TokenGenerationURL.ToString());
                    AppData.Credentials = BsonDocument.Parse(CryptographyUtility.Decrypt(AppData.Credentials));
                }
                else
                {
                    AppData.TokenGenerationURL = AesProvider.Decrypt(AppData.TokenGenerationURL.ToString(), appSettings.aesKey, appSettings.aesVector);
                    AppData.Credentials = BsonDocument.Parse(AesProvider.Decrypt(AppData.Credentials, appSettings.aesKey, appSettings.aesVector));
                }

                GrantType = AppData.Credentials.GetValue("grant_type").AsString;
                ClientSecert = AppData.Credentials.GetValue("client_secret").AsString;
                ClientId = AppData.Credentials.GetValue("client_id").AsString;
                Resource = AppData.Credentials.GetValue("resource").AsString;

                var client = new RestClient(AppData.TokenGenerationURL.ToString().Trim());
                var request = new RestRequest(Method.POST);
                request.AddHeader("cache-control", "no-cache");
                request.AddHeader("content-type", "application/x-www-form-urlencoded");
                request.AddParameter("application/x-www-form-urlencoded", "grant_type=" + GrantType +
                "&client_id=" + ClientId +
                "&client_secret=" + ClientSecert +
                "&resource=" + Resource,
                ParameterType.RequestBody);
                var requestBuilder = new StringBuilder();
                foreach (var param in request.Parameters)
                {
                    requestBuilder.AppendFormat("{0}: {1}\r\n", param.Name, param.Value);
                }
                requestBuilder.ToString();
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(CustomUrlTokenAppId), "Application TOKEN PARAMS -- " + requestBuilder, appId, string.Empty, string.Empty, string.Empty);
                IRestResponse response1 = client.Execute(request);
                string json1 = response1.Content;
                // Retrieve and Return the Access Token                
                var tokenObj = JsonConvert.DeserializeObject(json1) as dynamic;
                token = Convert.ToString(tokenObj.access_token);
                return token;
            }
            else if (AppData.Authentication == "FORM")
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
            return token;
        }
    }
}
