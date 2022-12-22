using Accenture.MyWizard.Ingrain.DataModels.AICore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
using Accenture.MyWizard.Shared.Helpers;
using Accenture.MyWizard.Ingrain.DataAccess;
using Microsoft.Extensions.Options;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
using Accenture.MyWizard.Ingrain.DataModels.Models;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Accenture.MyWizard.Ingrain.BusinessDomain.Services
{
    public class FlaskAPIService : IFlaskAPI
    {
        private MongoClient _mongoClient;
        private IMongoDatabase _database;
        private readonly WebHelper webHelper;
        private readonly IngrainAppSettings appSettings;
        private readonly IOptions<IngrainAppSettings> configSetting;
        public static IPhoenixTokenService _iPhoenixTokenService { get; set; }
        public FlaskAPIService(DatabaseProvider db, IOptions<IngrainAppSettings> settings, IServiceProvider serviceProvider)
        {
            _mongoClient = db.GetDatabaseConnection();
            var dataBaseName = MongoUrl.Create(settings.Value.connectionString).DatabaseName;
            _database = _mongoClient.GetDatabase(dataBaseName);
            webHelper = new WebHelper();
            appSettings = settings.Value;
            configSetting = settings;
            _iPhoenixTokenService = serviceProvider.GetService<IPhoenixTokenService>();
        }

        public void CallPython(string CorrelationId, string UniqueId, string pageInfo)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(FlaskAPIService), nameof(CallPython), "CallPython Triggered", string.Empty, string.Empty, string.Empty, string.Empty);
            var serviceResponse = new MethodReturn<object>();
            string baseUrl = appSettings.FlaskAPIBaseURL;
            string apiPath = appSettings.FlaskApiPath;
            dynamic tokenobj  =     _iPhoenixTokenService.GenerateToken();
            string token = string.Empty;
            if (appSettings.Environment == CONSTANTS.PAMEnvironment)
            {
                token = tokenobj["token"].ToString();
            }
            else
                token = tokenobj;
          
              FlaskDTO _flaskDTO = new FlaskDTO();
            _flaskDTO.CorrelationId = CorrelationId;
            _flaskDTO.UniqueId = UniqueId;
            _flaskDTO.PageInfo = pageInfo;
            _flaskDTO.UserId = CONSTANTS.System;

            JObject payload = JObject.Parse(JsonConvert.SerializeObject(_flaskDTO));
            try
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(FlaskAPIService), nameof(CallPython), "Python Call - Triggered " + UniqueId, string.Empty, string.Empty, string.Empty, string.Empty);
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(FlaskAPIService), nameof(CallPython), "token: " + token + " ,pageInfo: " + pageInfo, string.Empty, string.Empty, string.Empty, string.Empty);
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(FlaskAPIService), nameof(CallPython), "baseUrl: " + baseUrl.ToString() + "apiPath: " + apiPath.ToString() + "payload: " + payload.ToString() + " ,pageInfo: " + pageInfo, string.Empty, string.Empty, string.Empty, string.Empty);
                serviceResponse = RoutePOSTRequest(token, new Uri(baseUrl), apiPath, payload, true);
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(FlaskAPIService), nameof(CallPython), "Python - SUCCESS END. serviceResponse: " + serviceResponse + ",pageInfo: " + pageInfo, string.Empty, string.Empty, string.Empty, string.Empty);
                if (!serviceResponse.IsSuccess)
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(FlaskAPIService), nameof(CallPython), "Python - Not SUCCESS--END. serviceResponse: " + serviceResponse + ",pageInfo: " + pageInfo, string.Empty, string.Empty, string.Empty, string.Empty);
                    UpdateIngrainRequest(_flaskDTO.CorrelationId, _flaskDTO.UniqueId, serviceResponse.Message);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(FlaskAPIService), nameof(CallPython), "Exception - FlaskAPI Call : " + ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
        }

        public void UpdateIngrainRequest(string CorrelationId, string UniqueId, string message)
        {
            var requestCollection = _database.GetCollection<IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
            var filter = Builders<IngrainRequestQueue>.Filter.Where(x => x.CorrelationId == CorrelationId) & Builders<IngrainRequestQueue>.Filter.Where(x => x.UniId == UniqueId);
            var update = Builders<IngrainRequestQueue>.Update.Set(x => x.Status, "E").Set(x => x.RequestStatus, "Task Complete").Set(x => x.Message, "Flask Call to Python Failed" + message).Set(x => x.ModifiedOn, DateTime.Now.ToString(CONSTANTS.DateHoursFormat));
            requestCollection.UpdateOne(filter, update);
        }

        public MethodReturn<object> RoutePOSTRequest(string token, Uri baseUrl, string apiPath, JObject requestPayload, bool isReturnArray)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(RoutePOSTRequest), "RoutePOSTRequest - Called", default(Guid), string.Empty, string.Empty, string.Empty, string.Empty);
            MethodReturn<object> returnValue = new MethodReturn<object>();
            try
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(RoutePOSTRequest), "requestPayload : " + Convert.ToString(requestPayload), string.Empty, string.Empty, string.Empty, string.Empty);
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreService), nameof(RoutePOSTRequest), "token : " + token + " apiPath : " + apiPath + " baseUrl : " + baseUrl, string.Empty, string.Empty, string.Empty, string.Empty);
                StringContent content = new StringContent(requestPayload.ToString(), Encoding.UTF8, "application/json");
                HttpResponseMessage message = webHelper.InvokePOSTRequest(token, baseUrl.ToString(), apiPath, content);
                if (message != null && message.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    if (isReturnArray)
                        returnValue.ReturnValue = message.Content.ReadAsStringAsync().Result;
                    else
                        returnValue.ReturnValue = message.Content.ReadAsStringAsync().Result;
                    returnValue.IsSuccess = true;
                }
                else
                {
                    returnValue.IsSuccess = false;
                    returnValue.Message = message.ReasonPhrase + "_" + message.Content.ReadAsStringAsync().Result;
                    throw new Exception(returnValue.Message);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericSelfservice), nameof(RoutePOSTRequest), ex.StackTrace, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty);
                returnValue.IsSuccess = false;
                returnValue.Message = ex.Message;

            }
            return returnValue;
        }
    }
}
