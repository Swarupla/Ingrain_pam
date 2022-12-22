using System;
using System.Collections.Generic;
using System.Text;
using WINSERVICEMODELS = Accenture.MyWizard.Ingrain.WindowService.Models;
using DATAACCESS = Accenture.MyWizard.Ingrain.WindowService;
using DATAMODELS = Accenture.MyWizard.Ingrain.DataModels.Models;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
using MongoDB.Driver;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using Accenture.MyWizard.Cryptography.EncryptionProviders;
using MongoDB.Bson;
using RestSharp;
using System.Net;
using Accenture.MyWizard.Ingrain.DataModels.AICore;
using CryptographyHelper = Accenture.MyWizard.Cryptography;

namespace Accenture.MyWizard.Ingrain.WindowService.Services
{
    public class PushNotificationService
    {
        private readonly WINSERVICEMODELS.AppSettings appSettings = null;
        private IMongoDatabase _database;
        private DATAACCESS.DatabaseProvider databaseProvider;
        private TokenService _tokenService;
        static CryptographyHelper.Utility.CryptographyUtility CryptographyUtility = null;
        List<string> VDSUseCaseIds = new List<string>() { "f97739d7-d3b1-491b-8af1-876485cd3d30", "49d56fe0-1eca-4406-8b52-38724ac3b705", "7848b5c2-5167-49ea-9148-00be0da491c6" };

        public PushNotificationService()
        {
            appSettings = AppSettingsJson.GetAppSettings().GetSection("AppSettings").Get<WINSERVICEMODELS.AppSettings>();
            databaseProvider = new DATAACCESS.DatabaseProvider();
            var dataBaseName = MongoUrl.Create(appSettings.connectionString).DatabaseName;
            MongoClient mongoClient = databaseProvider.GetDatabaseConnection();
            _database = mongoClient.GetDatabase(dataBaseName);
            _tokenService = new TokenService();
            CryptographyUtility = new CryptographyHelper.Utility.CryptographyUtility();
        }

        public List<DATAMODELS.AppNotificationLog> GetNotificationLog()
        {
            var notificationLogCol = _database.GetCollection<DATAMODELS.AppNotificationLog>("AppNotificationLog");
            var filter = Builders<DATAMODELS.AppNotificationLog>.Filter.Where(x => !x.IsNotified)
                         & Builders<DATAMODELS.AppNotificationLog>.Filter.Where(x => x.RetryCount <= appSettings.NotificationMaxRetryCount);
            var projection = Builders<DATAMODELS.AppNotificationLog>.Projection.Exclude("_id");
            return notificationLogCol.Find(filter).Project<DATAMODELS.AppNotificationLog>(projection).ToList();
        }

        public void SendNotification(DATAMODELS.AppNotificationLog appNotificationLog)
        {
            try
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(PushNotificationService), nameof(SendNotification), "Sending Notifications - " + DateTime.UtcNow.ToString(), appNotificationLog.ApplicationId, "", appNotificationLog.ClientUId, appNotificationLog.DeliveryConstructUId);
                bool notify = true;

                if (!string.IsNullOrEmpty(appNotificationLog.ModifiedOn))
                {
                    DateTime lastExetime = DateTime.Parse(appNotificationLog.ModifiedOn);
                    DateTime curDate = DateTime.UtcNow;
                    int mins = (int)(curDate - lastExetime).TotalMinutes;

                    if (mins < appSettings.NotificationRetryFrequencyinMnts)
                    {
                        notify = false;
                    }
                }


                if (appNotificationLog.RetryCount > appSettings.NotificationMaxRetryCount)
                    notify = false;

                LOGGING.LogManager.Logger.LogProcessInfo(typeof(PushNotificationService), nameof(SendNotification), "notify: " + notify.ToString(), appNotificationLog.ApplicationId, "", appNotificationLog.ClientUId, appNotificationLog.DeliveryConstructUId);
                if (notify)
                {
                    DATAMODELS.NotificationDetails notificationDetails = new DATAMODELS.NotificationDetails();
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
                    //for lessDatapoints
                    notificationDetails.DataPointsCount = appNotificationLog.DataPointsCount;
                    notificationDetails.DataPointsWarning = appNotificationLog.DataPointsWarning;
                    //end
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

                    notificationDetails.ErrorMessage = appNotificationLog.ErrorMessage;

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
                   // LOGGING.LogManager.Logger.LogProcessInfo(typeof(PushNotificationService), nameof(SendNotification), "Ingrain to VDS Testing purpose", default(Guid), "token:"+ token, "AppNotificationUrl:"+ appNotificationLog.AppNotificationUrl,"Payload:"+ JsonConvert.SerializeObject(notificationDetails) , string.Empty);
                    HttpResponseMessage httpResponseMessage = new HttpResponseMessage();
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(PushNotificationService), nameof(SendNotification), "token: " + token.ToString(), appNotificationLog.ApplicationId, "", appNotificationLog.ClientUId, appNotificationLog.DeliveryConstructUId);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(PushNotificationService), nameof(SendNotification), "AppNotificationUrl: " + appNotificationLog.AppNotificationUrl.ToString(), appNotificationLog.ApplicationId, "", appNotificationLog.ClientUId, appNotificationLog.DeliveryConstructUId);
                    if (appNotificationLog.IsCascade)
                    {
                        httpResponseMessage = InvokePOSTRequestPAM(token, appNotificationLog.AppNotificationUrl, content, true);
                    }
                    else
                    {
                        httpResponseMessage = InvokePOSTRequest(token, appNotificationLog.AppNotificationUrl, content, false);
                    }
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(PushNotificationService), nameof(SendNotification), "VDS Callback Response", default(Guid), "Payload:" + JsonConvert.SerializeObject(notificationDetails), "AppNotificationUrl:" + appNotificationLog.AppNotificationUrl, "VDSResponse:" + httpResponseMessage.Content.ReadAsStringAsync().Result, "token:" + token);
                    if (httpResponseMessage.StatusCode == HttpStatusCode.OK)
                    {
                        if(appNotificationLog.IsCascade)
                        {
                            var res = httpResponseMessage.Content.ReadAsStringAsync();
                            DATAMODELS.NotificationResponse notificationResponse = JsonConvert.DeserializeObject<DATAMODELS.NotificationResponse>(res.Result);
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
            var notificationLogCollection = _database.GetCollection<DATAMODELS.AppNotificationLog>("AppNotificationLog");
            var filter = Builders<DATAMODELS.AppNotificationLog>.Filter.Where(x => x.RequestId == requestId);
            var updateDoc = Builders<DATAMODELS.AppNotificationLog>.Update.Set(x => x.IsNotified, isSuccess)
                                                               .Set(x => x.RetryCount, retryCount)
                                                               .Set(x => x.NotificationResponseMessage, message)
                                                               .Set(x => x.ModifiedOn, DateTime.UtcNow.ToString());
            notificationLogCollection.UpdateOne(filter, updateDoc);


        }

        /// <summary>
        /// SPA AI usecase send Notifications
        /// </summary>
        public void SendAITrainingNotifications()
        {
            var ingrainRequestQueueCollection = _database.GetCollection<AICoreModels>("AICoreModels");
            //add for userId if common for all env from SPA
            var filter = Builders<AICoreModels>.Filter.Where(x => x.SendNotification == "True")
                         & Builders<AICoreModels>.Filter.Where(x => x.IsNotificationSent == "False")
                         & Builders<AICoreModels>.Filter.Where(x => x.UsecaseId == "6665e35b-b2d1-40cc-b28f-3b795780f34f")
                         & (Builders<AICoreModels>.Filter.Where(x => x.ApplicationId == "a3798931-4028-4f72-8bcd-8bb368cc71a9"));
            //5354e9b8-0eb1-4275-b666-6eba15c12b2c & (Builders<AICoreModels>.Filter.Where(x => x.ApplicationId == "a3798931-4028-4f72-8bcd-8bb368cc71a9"));
            var projection = Builders<AICoreModels>.Projection.Exclude("_id");
            var requestQueue = ingrainRequestQueueCollection.Find(filter).Project<AICoreModels>(projection).ToList();

            if (requestQueue.Count > 0)
            {
                var appNotificationLogCollection = _database.GetCollection<DATAMODELS.AppNotificationLog>(nameof(DATAMODELS.AppNotificationLog));
                foreach (var request in requestQueue)
                {
                    DateTime createdTime = DateTime.Parse(request.CreatedOn);
                    DateTime curDate = DateTime.UtcNow;

                    int elapsedMin = (int)(curDate - createdTime).TotalMinutes;


                    if (request.ModelStatus == "Completed" || request.ModelStatus == "Error" || request.ModelStatus == "Warning" || elapsedMin > 15)
                    {
                        DATAMODELS.AppNotificationLog appNotificationLog = new DATAMODELS.AppNotificationLog();
                        //var deployModel = _database.GetCollection<DATAMODELS.DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
                        //var filter2 = Builders<DATAMODELS.DeployModelsDto>.Filter.Where(x => x.CorrelationId == request.CorrelationId);
                        //var projection1 = Builders<DATAMODELS.DeployModelsDto>.Projection.Exclude("_id");
                        //var model = deployModel.Find(filter2).Project<DATAMODELS.DeployModelsDto>(projection1).FirstOrDefault();

                        if (!string.IsNullOrEmpty(request.UsecaseId))
                        {
                            appNotificationLog.UseCaseId = request.UsecaseId;
                        }


                        appNotificationLog.ClientUId = request.ClientId;
                        appNotificationLog.DeliveryConstructUId = request.DeliveryConstructId;
                        //  appNotificationLog.ProblemType = model.ModelType;
                        //   appNotificationLog.FunctionalArea = model.Category;
                        //if (model.IsModelTemplate)
                        //    appNotificationLog.ModelType = "ModelTemplate";
                        //else
                        //{
                        //    if (model.IsPrivate)
                        //        appNotificationLog.ModelType = "Private";
                        //    else
                        //        appNotificationLog.ModelType = "Public";
                        //}
                        appNotificationLog.OperationType = "Created";

                        appNotificationLog.UniqueId = request.UniId;
                        if (request.ModelStatus == "Warning" || request.ModelStatus == "Error")
                        {
                            request.ModelStatus = "Error";
                            appNotificationLog.ErrorMessage = request.StatusMessage;
                        }
                        if (request.ModelStatus == "Completed")
                        {
                            appNotificationLog.Message = request.StatusMessage;
                        }
                        appNotificationLog.ModelStatus = request.ModelStatus;
                        appNotificationLog.Status = request.ModelStatus;
                        appNotificationLog.StatusMessage = request.StatusMessage;
                        appNotificationLog.CorrelationId = request.CorrelationId;
                        appNotificationLog.NotificationEventType = "Training";
                        appNotificationLog.IsNotified = false;
                        appNotificationLog.RetryCount = 0;
                        appNotificationLog.ApplicationId = request.ApplicationId;
                        if (elapsedMin > 15 && request.ModelStatus != "Completed")
                        {
                            appNotificationLog.Status = "Error";
                            appNotificationLog.StatusMessage = "Request is taking more time to complete in Ingrain";
                            appNotificationLog.ErrorMessage = "Request is taking more time to complete in Ingrain";
                            appNotificationLog.ModelStatus = "Error";
                        }
                        appNotificationLog.RequestId = Guid.NewGuid().ToString();
                        appNotificationLog.CreatedOn = DateTime.UtcNow.ToString();


                        var appIntegrationCollection = _database.GetCollection<DATAMODELS.AppIntegration>(CONSTANTS.AppIntegration);
                        var filter3 = Builders<DATAMODELS.AppIntegration>.Filter.Where(x => x.ApplicationID == request.ApplicationId);
                        var app = appIntegrationCollection.Find(filter3).FirstOrDefault();

                        if (app != null)
                        {
                            appNotificationLog.Environment = app.Environment;
                        }
                        appNotificationLog.AppNotificationUrl = request.ResponsecallbackUrl;
                        //if (app.Environment == "PAD")
                        //{
                        //    Uri apiUri = new Uri(appSettings.myWizardAPIUrl);
                        //    string host = apiUri.GetLeftPart(UriPartial.Authority);
                        //    appNotificationLog.AppNotificationUrl = host + "/" + app.AppNotificationUrl;
                        //}
                        //else
                        //{
                        //    appNotificationLog.AppNotificationUrl = app.AppNotificationUrl;
                        //}



                        appNotificationLogCollection.InsertOne(appNotificationLog);
                        UpdateAIModel(request.CorrelationId, "True");
                    }
                }
            }
        }
        public void SendPredictionNotifications()
        {          
            var ingrainRequestQueueCollection = _database.GetCollection<DATAMODELS.IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
            var filter = Builders<DATAMODELS.IngrainRequestQueue>.Filter.Where(x => x.SendNotification == "True")
                         & Builders<DATAMODELS.IngrainRequestQueue>.Filter.Where(x => x.IsNotificationSent == "False")
                         & (Builders<DATAMODELS.IngrainRequestQueue>.Filter.Where(x => x.pageInfo == CONSTANTS.PublishModel)
                         | Builders<DATAMODELS.IngrainRequestQueue>.Filter.Where(x => x.pageInfo == CONSTANTS.ForecastModel));
            var projection = Builders<DATAMODELS.IngrainRequestQueue>.Projection.Exclude("_id");
            var requestQueue = ingrainRequestQueueCollection.Find(filter).Project<DATAMODELS.IngrainRequestQueue>(projection).ToList();

            if (requestQueue.Count > 0)
            {
                var appNotificationLogCollection = _database.GetCollection<DATAMODELS.AppNotificationLog>(nameof(DATAMODELS.AppNotificationLog));
                foreach (var request in requestQueue)
                {
                    DateTime createdTime = DateTime.Parse(request.CreatedOn);
                    DateTime curDate = DateTime.UtcNow;

                    int elapsedMin = (int)(curDate - createdTime).TotalMinutes;


                    if (request.Status == "C" || request.Status == "E" || elapsedMin > 10)
                    {
                        DATAMODELS.AppNotificationLog appNotificationLog = new DATAMODELS.AppNotificationLog();
                        var deployModel = _database.GetCollection<DATAMODELS.DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
                        var filter2 = Builders<DATAMODELS.DeployModelsDto>.Filter.Where(x => x.CorrelationId == request.CorrelationId);
                        var projection1 = Builders<DATAMODELS.DeployModelsDto>.Projection.Exclude("_id");
                        var model = deployModel.Find(filter2).Project<DATAMODELS.DeployModelsDto>(projection1).FirstOrDefault();

                        if (!string.IsNullOrEmpty(model.TemplateUsecaseId))
                        {
                            appNotificationLog.UseCaseId = model.TemplateUsecaseId;
                        }
                        else
                        {
                            appNotificationLog.UseCaseId = model.CorrelationId;
                        }

                        appNotificationLog.ClientUId = model.ClientUId;
                        appNotificationLog.DeliveryConstructUId = model.DeliveryConstructUID;
                        appNotificationLog.ProblemType = model.ModelType;
                        if (appNotificationLog.ProblemType == "Multi_Class")
                            appNotificationLog.ProblemType = "Classification";
                        appNotificationLog.FunctionalArea = model.Category;
                        if (model.IsModelTemplate)
                            appNotificationLog.ModelType = "ModelTemplate";
                        else
                        {
                            if (model.IsPrivate)
                                appNotificationLog.ModelType = "Private";
                            else
                                appNotificationLog.ModelType = "Public";
                        }
                        appNotificationLog.OperationType = "Created";

                        appNotificationLog.UniqueId = request.UniId;
                        appNotificationLog.Status = request.Status;
                        appNotificationLog.StatusMessage = request.Message;
                        appNotificationLog.NotificationEventType = "Prediction";
                        appNotificationLog.IsNotified = false;
                        appNotificationLog.RetryCount = 0;
                        appNotificationLog.ApplicationId = request.AppID;
                        if (elapsedMin > 10)
                        {
                            appNotificationLog.Status = "E";
                            appNotificationLog.StatusMessage = "Request is taking more time to complete in Ingrain";
                        }
                        appNotificationLog.RequestId = Guid.NewGuid().ToString();
                        appNotificationLog.CreatedOn = DateTime.UtcNow.ToString();

                        if (VDSUseCaseIds.Contains(request.TemplateUseCaseID))
                        {
                            var ingrainRequestQueue = _database.GetCollection<DATAMODELS.IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
                            var filter1 = Builders<DATAMODELS.IngrainRequestQueue>.Filter.Where(x => x.pageInfo == CONSTANTS.TrainAndPredict)
                                         & Builders<DATAMODELS.IngrainRequestQueue>.Filter.Where(x => x.Function == CONSTANTS.AutoTrain)
                                         & Builders<DATAMODELS.IngrainRequestQueue>.Filter.Where(x => x.CorrelationId == model.CorrelationId); 
                            var Rprojection = Builders<DATAMODELS.IngrainRequestQueue>.Projection.Exclude("_id");
                            var result = ingrainRequestQueue.Find(filter1).Project<DATAMODELS.IngrainRequestQueue>(Rprojection).FirstOrDefault();
                            if (result != null)
                                appNotificationLog.Entity = result.ServiceType;
                            switch (request.TemplateUseCaseID)
                            {
                                case "f97739d7-d3b1-491b-8af1-876485cd3d30":
                                    appNotificationLog.UseCaseName = "SLA Prediction";
                                    appNotificationLog.UseCaseDescription = "SLA Prediction";
                                    break;
                                case "49d56fe0-1eca-4406-8b52-38724ac3b705":
                                    appNotificationLog.UseCaseName = "Ticket Inflow Prediction";
                                    appNotificationLog.UseCaseDescription = "Ticket Inflow Prediction";
                                    break;
                                case "7848b5c2-5167-49ea-9148-00be0da491c6":
                                    appNotificationLog.UseCaseName = "Turn Around Time";
                                    appNotificationLog.UseCaseDescription = "Turn Around Time";
                                    break;
                                default:
                                    appNotificationLog.UseCaseName = "";
                                    appNotificationLog.UseCaseDescription = "";
                                    break;
                            }
                            appNotificationLog.ModelStatus = model.Status;
                            appNotificationLog.Message = request.Message;
                            appNotificationLog.UserId= appSettings.IsAESKeyVault? CryptographyUtility.Decrypt(request.CreatedByUser) : AesProvider.Decrypt(request.CreatedByUser, appSettings.aesKey, appSettings.aesVector);
                            appNotificationLog.CreatedDateTime = request.ModifiedOn;
                            appNotificationLog.CorrelationId = model.CorrelationId;
                            if (appNotificationLog.Status == "C")
                                appNotificationLog.Progress = "100%";
                            else
                                appNotificationLog.Progress = "0%";
                        }

                        var appIntegrationCollection = _database.GetCollection<DATAMODELS.AppIntegration>(CONSTANTS.AppIntegration);
                        var filter3 = Builders<DATAMODELS.AppIntegration>.Filter.Where(x => x.ApplicationID == request.AppID);
                        var app = appIntegrationCollection.Find(filter3).FirstOrDefault();

                        if (app != null)
                        {
                            appNotificationLog.Environment = app.Environment;
                            if (app.Environment == "PAD")
                            {
                                Uri apiUri = new Uri(appSettings.myWizardAPIUrl);
                                string host = apiUri.GetLeftPart(UriPartial.Authority);
                                appNotificationLog.AppNotificationUrl = host + "/" + app.AppNotificationUrl;
                            }
                            else
                            {
                                if (VDSUseCaseIds.Contains(request.TemplateUseCaseID))
                                {
                                    appNotificationLog.AppNotificationUrl = request.AppURL;
                                }
                                else
                                    appNotificationLog.AppNotificationUrl = app.AppNotificationUrl;
                            }
                        }


                        appNotificationLogCollection.InsertOne(appNotificationLog);
                        UpdateRequestQueue(request.RequestId, "True");
                    }
                }
            }
        }

        public void SendTrainingNotifications()
        {
            var requestCollection = _database.GetCollection<DATAMODELS.IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
            var filter = Builders<DATAMODELS.IngrainRequestQueue>.Filter.Where(x => x.SendNotification == "True")
                         & Builders<DATAMODELS.IngrainRequestQueue>.Filter.Where(x => x.IsNotificationSent == "False")
                         & Builders<DATAMODELS.IngrainRequestQueue>.Filter.Where(x => x.pageInfo == "AutoTrain");
            var projection = Builders<DATAMODELS.IngrainRequestQueue>.Projection.Exclude("_id");
            var requestQueue = requestCollection.Find(filter).Project<DATAMODELS.IngrainRequestQueue>(projection).ToList();
            if (requestQueue.Count > 0)
            {
                var appNotificationLogCollection = _database.GetCollection<DATAMODELS.AppNotificationLog>(nameof(DATAMODELS.AppNotificationLog));
                foreach (var request in requestQueue)
                {
                    DateTime createdTime = DateTime.Parse(request.CreatedOn);
                    DateTime curDate = DateTime.UtcNow;

                    int elapsedMin = (int)(curDate - createdTime).TotalMinutes;

                    if (request.Status == "C" || request.Status == "E" || elapsedMin > 10)
                    {
                        DATAMODELS.AppNotificationLog appNotificationLog = new DATAMODELS.AppNotificationLog();
                        appNotificationLog.ClientUId = request.ClientID;
                        appNotificationLog.DeliveryConstructUId = request.DeliveryconstructId;
                        appNotificationLog.UseCaseId = request.TemplateUseCaseID;
                        appNotificationLog.ModelType = "ModelTemplate";
                        appNotificationLog.OperationType = "Created";
                        appNotificationLog.NotificationEventType = "Training";
                        appNotificationLog.Status = request.Status;
                        appNotificationLog.StatusMessage = request.Message;
                        appNotificationLog.IsNotified = false;
                        appNotificationLog.RetryCount = 0;
                        appNotificationLog.ApplicationId = request.AppID;
                        if (elapsedMin > 15)
                        {
                            appNotificationLog.Status = "E";
                            appNotificationLog.StatusMessage = "Request is taking more time to complete in Ingrain";
                        }
                        appNotificationLog.RequestId = Guid.NewGuid().ToString();
                        appNotificationLog.CreatedOn = DateTime.UtcNow.ToString();


                        var appIntegrationCollection = _database.GetCollection<DATAMODELS.AppIntegration>(CONSTANTS.AppIntegration);
                        var filter3 = Builders<DATAMODELS.AppIntegration>.Filter.Where(x => x.ApplicationID == request.AppID);
                        var app = appIntegrationCollection.Find(filter3).FirstOrDefault();

                        if (app != null)
                        {
                            appNotificationLog.Environment = app.Environment;
                            if (app.Environment == "PAD")
                            {
                                Uri apiUri = new Uri(appSettings.myWizardAPIUrl);
                                string host = apiUri.GetLeftPart(UriPartial.Authority);
                                appNotificationLog.AppNotificationUrl = host + "/" + app.AppNotificationUrl;
                            }
                            else
                            {
                                appNotificationLog.AppNotificationUrl = app.AppNotificationUrl;
                            }
                        }


                        appNotificationLogCollection.InsertOne(appNotificationLog);
                        UpdateRequestQueue(request.RequestId, "True");
                    }
                }
            }
        }
        public void UpdateRequestQueue(string requestId, string isNotificationSent)
        {
            var requestCollection = _database.GetCollection<DATAMODELS.IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
            var filter = Builders<DATAMODELS.IngrainRequestQueue>.Filter.Where(x => x.RequestId == requestId);
            var update = Builders<DATAMODELS.IngrainRequestQueue>.Update.Set(x => x.IsNotificationSent, isNotificationSent);
            requestCollection.UpdateOne(filter, update);
        }

        public void UpdateAIModel(string correlationId, string isNotificationSent)
        {
            var requestCollection = _database.GetCollection<AICoreModels>("AICoreModels");
            var filter = Builders<AICoreModels>.Filter.Where(x => x.CorrelationId == correlationId);
            var update = Builders<AICoreModels>.Update.Set(x => x.IsNotificationSent, isNotificationSent);
            requestCollection.UpdateOne(filter, update);
        }
       
        public HttpResponseMessage InvokePOSTRequest(string token, string apiUrl, StringContent content, bool isCascade)
        {
            //LOGGING.LogManager.Logger.LogProcessInfo(typeof(PushNotificationService), nameof(InvokePOSTRequest), "Ingrain to VDS Testing purpose InvokePOSTRequest Start", default(Guid), "token:"+ token, "apiUrl:"+ apiUrl, string.Empty, "InvokePOSTRequest CONTENT---" + content.ReadAsStringAsync().Result);
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
                        if(appSettings.Environment.Equals(CONSTANTS.PAMEnvironment))
                            client.DefaultRequestHeaders.Add("Authorization", token);
                        else
                            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                    }
                }
                else
                {
                    //LOGGING.LogManager.Logger.LogProcessInfo(typeof(PushNotificationService), nameof(InvokePOSTRequest), "Ingrain to VDS Testing purpose InvokePOSTRequest AWF", default(Guid), "token:" + token, "apiUrl:" + apiUrl, appSettings.Environment, "InvokePOSTRequest CONTENT---" + content.ReadAsStringAsync().Result);
                    if (!string.IsNullOrWhiteSpace(token))
                    {
                        if (appSettings.Environment.Equals(CONSTANTS.PAMEnvironment))
                            client.DefaultRequestHeaders.Add("Authorization", token);
                        else
                            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                    }
                }
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(PushNotificationService), "InvokePOSTRequest CONTENT---" + content.ReadAsStringAsync().Result, "END", "", "", "", "");
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                return client.PostAsync(uri, content).Result;
            }
        }
        //implemented only for MinSpec
        public HttpResponseMessage InvokePOSTRequestPAM(string token, string apiUrl, StringContent content, bool isCascade)
        {
            using (var client = new HttpClient())
            {
                string uri = apiUrl;
                client.Timeout = new TimeSpan(0, 30, 0);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                if (isCascade)
                {

                    if (!string.IsNullOrWhiteSpace(token))
                    {
                        client.DefaultRequestHeaders.Add("Authorization", token);
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(token))
                    {
                        client.DefaultRequestHeaders.Add("Authorization", token);
                    }
                }
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(PushNotificationService), "InvokePOSTRequest CONTENT---" + content.ReadAsStringAsync().Result, "END", "", "", "", "");
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                return client.PostAsync(uri, content).Result;
            }
        }
        public List<DATAMODELS.IngrainRequestQueue> GetSPPPredictionRequests()
        {
            var requestCollection = _database.GetCollection<DATAMODELS.IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
            var filter = Builders<DATAMODELS.IngrainRequestQueue>.Filter.Eq(CONSTANTS.pageInfo, CONSTANTS.PublishModel)
                         & Builders<DATAMODELS.IngrainRequestQueue>.Filter.Eq("AppID", "fa36e811-a59f-48c0-94a6-9a7ffc8bc8ab")
                         & Builders<DATAMODELS.IngrainRequestQueue>.Filter.Where(x => x.IsNotificationSent == null)
                         & (Builders<DATAMODELS.IngrainRequestQueue>.Filter.Eq("Status", "null")
                          | Builders<DATAMODELS.IngrainRequestQueue>.Filter.Eq("Status", "I")
                          | Builders<DATAMODELS.IngrainRequestQueue>.Filter.Eq("Status", "P"));
            var projection = Builders<DATAMODELS.IngrainRequestQueue>.Projection.Exclude("_id");
            return requestCollection.Find(filter).Project<DATAMODELS.IngrainRequestQueue>(projection).ToList();

        }




        public void SendSPPPredNotification(List<DATAMODELS.IngrainRequestQueue> ingrainRequestQueues)
        {
            if (ingrainRequestQueues.Count > 0)
            {
                //LOGGING.LogManager.Logger.LogProcessInfo(typeof(PushNotificationService), nameof(SendSPPPredNotification), "No of null prediction records-"+ingrainRequestQueues.Count);
                foreach (var request in ingrainRequestQueues)
                {
                    DateTime currentTime = DateTime.Now;
                    DateTime createdTime = DateTime.Parse(request.CreatedOn);
                    TimeSpan span = currentTime.Subtract(createdTime);
                    if (span.TotalMinutes > Convert.ToDouble(appSettings.PredictionTimeoutMinutes) + 2)
                    {
                        //LOGGING.LogManager.Logger.LogProcessInfo(typeof(PushNotificationService), nameof(SendSPPPredNotification), "Sending Notification for - " + request.UniId);
                        DATAMODELS.IngrainPredictionData ingrainPrediction = new DATAMODELS.IngrainPredictionData();
                        ingrainPrediction.CorrelationId = request.CorrelationId;
                        ingrainPrediction.UniqueId = request.UniId;
                        ingrainPrediction.Message = CONSTANTS.PredictionTimeOut;
                        ingrainPrediction.ErrorMessage = "Prediction Taking long time - PredictionTimeOut Error";
                        ingrainPrediction.Status = "E";
                        if (string.IsNullOrEmpty(request.AppURL))
                        {

                        }
                        var result2 = IngrainPredictionCallback(ingrainPrediction, request.AppID, request.AppURL);
                        UpdateRequestQueue(request.RequestId, "True", result2);
                    }

                }
            }
        }


        public void UpdateRequestQueue(string requestId, string isNotificationSent, string result)
        {
            var requestCollection = _database.GetCollection<DATAMODELS.IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
            var filter = Builders<DATAMODELS.IngrainRequestQueue>.Filter.Where(x => x.RequestId == requestId);
            var update = Builders<DATAMODELS.IngrainRequestQueue>.Update.Set(x => x.IsNotificationSent, isNotificationSent).Set(x => x.NotificationMessage, result);
            requestCollection.UpdateOne(filter, update);
        }

        private string IngrainPredictionCallback(DATAMODELS.IngrainPredictionData CallBackResponse, string applicationId, string baseAddress)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(PushNotificationService), nameof(IngrainPredictionCallback), "INGRAINPREDICTIONCALLBACK INITIATED Data--" + JsonConvert.SerializeObject(CallBackResponse) + "--baseAddress--" + baseAddress, string.IsNullOrEmpty(CallBackResponse.CorrelationId) ? default(Guid) : new Guid(CallBackResponse.CorrelationId), applicationId, string.Empty, string.Empty, string.Empty);
            string returnMessage = "Error";
            try
            {
                string token = CustomUrlTokenAppId(applicationId);
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(PushNotificationService), nameof(IngrainPredictionCallback), "INGRAINPREDICTIONCALLBACK TOKEN--" + token, string.IsNullOrEmpty(CallBackResponse.CorrelationId) ? default(Guid) : new Guid(CallBackResponse.CorrelationId), applicationId, string.Empty, string.Empty, string.Empty);
                string contentType = "application/json";
                var Request = JsonConvert.SerializeObject(CallBackResponse);
                using (var Client = new HttpClient())
                {
                    Client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                    Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(contentType));
                    HttpContent httpContent = new StringContent(Request, Encoding.UTF8, "application/json");
                    HttpResponseMessage httpResponse = Client.PostAsync(baseAddress, httpContent).Result;
                    var statuscode = httpResponse.StatusCode;
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(PushNotificationService), nameof(IngrainPredictionCallback), "INGRAINPREDICTIONCALLBACK STATUSCODE :" + httpResponse.StatusCode + "--URL--" + baseAddress + "--INGRAIN PAYLOAD--" + JsonConvert.SerializeObject(CallBackResponse), string.IsNullOrEmpty(CallBackResponse.CorrelationId) ? default(Guid) : new Guid(CallBackResponse.CorrelationId), applicationId, string.Empty, string.Empty, string.Empty);
                    if (httpResponse.IsSuccessStatusCode)
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(PushNotificationService), nameof(IngrainPredictionCallback), "IngrainPredictionCallback SUCCESS- TOKEN--" + token + "BASE_ADDERESS: " + baseAddress + "-HTTPCONTENT-" + httpResponse.Content.ReadAsStringAsync().Result, string.IsNullOrEmpty(CallBackResponse.CorrelationId) ? default(Guid) : new Guid(CallBackResponse.CorrelationId), applicationId, string.Empty, string.Empty, string.Empty);
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(PushNotificationService), nameof(IngrainPredictionCallback), "END SUCCESS", string.IsNullOrEmpty(CallBackResponse.CorrelationId) ? default(Guid) : new Guid(CallBackResponse.CorrelationId), applicationId, string.Empty, string.Empty, string.Empty);
                        returnMessage = "Success";
                    }
                    else
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(PushNotificationService), nameof(IngrainPredictionCallback), "IngrainPredictionCallback SUCCESS- TOKEN--" + token + "BASE_ADDERESS: " + baseAddress + "-HTTPCONTENT-" + httpResponse.Content.ReadAsStringAsync().Result, string.IsNullOrEmpty(CallBackResponse.CorrelationId) ? default(Guid) : new Guid(CallBackResponse.CorrelationId), applicationId, string.Empty, string.Empty, string.Empty);
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(PushNotificationService), nameof(IngrainPredictionCallback), "END ERROR", string.IsNullOrEmpty(CallBackResponse.CorrelationId) ? default(Guid) : new Guid(CallBackResponse.CorrelationId), applicationId, string.Empty, string.Empty, string.Empty);
                        returnMessage = "Error";
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(GenericAutotrainService), nameof(IngrainPredictionCallback), ex.Message + "- STACKTRACE - " + ex.StackTrace + ex.InnerException, ex, applicationId, string.Empty, string.Empty, string.Empty);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(IngrainPredictionCallback), "END", string.IsNullOrEmpty(CallBackResponse.CorrelationId) ? default(Guid) : new Guid(CallBackResponse.CorrelationId), applicationId, string.Empty, string.Empty, string.Empty);
            return returnMessage;

        }
     
        private string CustomUrlTokenAppId(string appId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(CustomUrlTokenAppId), "CUSTOMURLTOKENAPPID FOR APPLICATIONID--" + appId, appId, string.Empty, string.Empty, string.Empty);
            var AppIntegCollection = _database.GetCollection<DATAMODELS.AppIntegration>(CONSTANTS.AppIntegration);
            var filterBuilder = Builders<DATAMODELS.AppIntegration>.Filter;
            var AppFilter = filterBuilder.Eq("ApplicationID", appId);

            var Projection = Builders<DATAMODELS.AppIntegration>.Projection.Include("BaseURL").Include("Authentication").Include("TokenGenerationURL").Include("Credentials").Exclude("_id");
            var AppData = AppIntegCollection.Find(AppFilter).Project<DATAMODELS.AppIntegration>(Projection).FirstOrDefault();

            dynamic token = string.Empty;
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(CustomUrlTokenAppId), "CUSTOMURLTOKENAPPID for Application" + AppData.ApplicationName, AppData.ApplicationID, string.Empty, AppData.clientUId, AppData.deliveryConstructUID);
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
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericAutotrainService), nameof(CustomUrlTokenAppId), "Application TOKEN PARAMS -- " + requestBuilder, AppData.ApplicationID, string.Empty, AppData.clientUId, AppData.deliveryConstructUID);
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
            return token;
        }
    }
}
