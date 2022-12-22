using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
using Accenture.MyWizard.Ingrain.DataModels.AICore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Microsoft.Extensions.Primitives;
using System.Threading;
using Microsoft.Extensions.Options;
using Accenture.MyWizard.Shared.Helpers;
using Accenture.MyWizard.Ingrain.DataModels.Models;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
using Accenture.MyWizard.Ingrain.DataModels.AICore.GenericService;
using Accenture.MyWizard.Ingrain.DataModels.AICore.UseCase;
using System.Runtime.CompilerServices;
using MongoDB.Bson.Serialization.IdGenerators;
using Accenture.MyWizard.Ingrain.BusinessDomain.Common;
using MongoDB.Bson;
using Accenture.MyWizard.Cryptography.EncryptionProviders;

namespace Accenture.MyWizard.Ingrain.WebService.Controllers
{
    public class AICoreController : MyWizardControllerBase
    {

        #region Members
        private IAICoreService _aiCoreService;
        private IngrainAppSettings appSettings;
        PythonInfo pythonInfo = new PythonInfo();
        private CallBackErrorLog auditTrailLog;
        private readonly IOptions<IngrainAppSettings> configSetting;
        private static ICustomDataService _customDataService { set; get; }
        private static IEncryptionDecryption _encryptionDecryption { set; get; }
        #endregion

        #region Constructor
        public AICoreController(IServiceProvider serviceProvider, IOptions<IngrainAppSettings> settings)
        {
            appSettings = settings.Value;
            _aiCoreService = serviceProvider.GetService<IAICoreService>();
            auditTrailLog = new CallBackErrorLog();
            configSetting = settings;
            _customDataService = serviceProvider.GetService<ICustomDataService>();
            _encryptionDecryption = serviceProvider.GetService<IEncryptionDecryption>();
        }

        #endregion


        #region Methods
        /// <summary>
        /// To get List of AI Core services
        /// </summary>
        /// <param></param>
        /// <returns>List of AI Core services</returns>
        [HttpGet]
        [Route("api/GetAllAIServices")]
        public IActionResult GetAllAIServices()
        {
            try
            {
                return Ok(_aiCoreService.GetAllAICoreServices());
            }
            catch (Exception ex)
            {
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
        }


        /// <summary>
        /// To get List of AI Core Models
        /// </summary>
        /// <param></param>
        /// <returns>List of AI Core Models</returns>

        [HttpGet]
        [Route("api/GetAllAICoreModels")]
        public IActionResult GetAllAICoreModels()
        {
            try
            {
                return Ok(_aiCoreService.GetAllAICoreModels());
            }
            catch (Exception ex)
            {
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
        }
        /// <summary>
        /// To get List of AI Core Models based on clientid,dcid,serviceid,userid
        /// </summary>
        /// <param></param>
        /// <returns>List of AI Core Models</returns>

        [HttpGet]
        [Route("api/GetAICoreModels")]
        public IActionResult GetAICoreModels(string clientid, string dcid, string serviceid, string userid)
        {
            try
            {
                #region VALIDATIONS
                if (!CommonUtility.GetValidUser(userid))
                {
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);
                }
                #endregion

                return Ok(_aiCoreService.GetAICoreModels(clientid, dcid, serviceid, userid));
            }
            catch (Exception ex)
            {
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
        }

        /// <summary>
        /// To get AI Core service using service id
        /// </summary>
        /// <param name="serviceid"></param>
        /// <returns>Service details of the servcieid</returns>

        [HttpGet]
        [Route("api/GetServiceById")]
        public IActionResult GetServiceById(string serviceid)
        {
            try
            {
                return Ok(_aiCoreService.GetAiCoreServiceDetails(serviceid));
            }
            catch (Exception ex)
            {
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
        }

        /// <summary>
        /// To Add new AI Core services
        /// </summary>
        /// <param></param>
        /// <returns>Success if added</returns>
        [HttpPost]
        [Route("api/AddAICoreService")]
        public IActionResult AddAICoreService(Service service)
        {
            try
            {
                #region VALIDATIONS
                if (!CommonUtility.GetValidUser(service.CreatedBy))
                {
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);
                }
                else if (!CommonUtility.GetValidUser(service.ModifiedBy))
                {
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);
                }
                CommonUtility.ValidateInputFormData(service.ServiceId, "ServiceId", true);
                CommonUtility.ValidateInputFormData(service.ServiceCode, "ServiceCode", false);
                CommonUtility.ValidateInputFormData(service.ServiceName, "ServiceName", false);
                CommonUtility.ValidateInputFormData(service.Description, "Description", false);
                CommonUtility.ValidateInputFormData(service.LongDescription, "LongDescription", false);
                CommonUtility.ValidateInputFormData(service.ServiceMethod, "ServiceMethod", false);
                CommonUtility.ValidateInputFormData(service.OperationMethod, "OperationMethod", false);
                CommonUtility.ValidateInputFormData(service.ApiUrl, "ApiUrl", false);
                CommonUtility.ValidateInputFormData(service.PrimaryTrainApiUrl, "PrimaryTrainApiUrl", false);
                CommonUtility.ValidateInputFormData(service.SecondaryTrainApiUrl, "SecondaryTrainApiUrl", false);
                CommonUtility.ValidateInputFormData(service.Category, "Category", false);
                CommonUtility.ValidateInputFormData(service.TemplateFlag, "TemplateFlag", false);
                CommonUtility.ValidateInputFormData(service.ApiCallInput, "ApiCallInput", false);
                CommonUtility.ValidateInputFormData(service.ServiceInputType, "ServiceInputType", false);
                #endregion

                return Ok(_aiCoreService.AddAICoreService(service));
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AICoreController), nameof(AddAICoreService), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
        }


        /// <summary>
        /// To get AI Core Model details by ID
        /// </summary>
        /// <param name="correlationid">correlationid of the model</param>
        /// <returns>Returns model details</returns>
        [HttpGet]
        [Route("api/GetAICoreModelDetails")]
        public IActionResult GetAICoreModelDetails(string correlationid)
        {
            try
            {
                return Ok(_aiCoreService.GetAICoreModelPath(correlationid));
            }
            catch (Exception ex)
            {
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
        }
        /// <summary>
        /// To valdiate python token
        /// </summary>   
        [HttpGet]
        [Route("api/ValidatePyToken")]
        public IActionResult ValidatePyToken()
        {
            try
            {
                return Ok();
            }
            catch (Exception ex)
            {
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
        }
        /// <summary>
        /// To Invoke AI Core service
        /// </summary>
        /// <param></param>
        /// <returns>Returns prediction response</returns>

        [HttpPost]
        [Route("api/InvokeAICoreService")]
        public IActionResult InvokeAICoreService(Request request)
        {
            var response = new Response(request.ClientID, request.DeliveryConstructID, request.ServiceID);
            var returnResponse = new MethodReturn<Response>();
            var serviceResponse = new MethodReturn<object>();

            try
            {
                #region VALIDATIONS
                if (!CommonUtility.GetValidUser(request.UserID))
                {
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);
                }
                CommonUtility.ValidateInputFormData(request.ClientID, "ClientID", true);
                CommonUtility.ValidateInputFormData(request.DeliveryConstructID, "DeliveryConstructID", true);
                CommonUtility.ValidateInputFormData(request.ServiceID, "ServiceID", true);
                CommonUtility.ValidateInputFormData(Convert.ToString(request.CustomParam?.CorrelationId), "CustomParam-CorrelationId", true);
                //CommonUtility.ValidateInputFormData(Convert.ToString(request.Payload), "Payload", false);
                //CommonUtility.ValidateInputFormData(Convert.ToString(request.FileKeys), "FileKeys", false);
                #endregion

                Service service = _aiCoreService.GetAiCoreServiceDetails(request.ServiceID);
                //string baseUrl = new Uri(service.ApiUrl).GetLeftPart(UriPartial.Authority);
                //string apiPath = new Uri(service.ApiUrl).PathAndQuery;
                string baseUrl = appSettings.AICorePythonURL;
                string apiPath = service.ApiUrl;

                switch (service.ServiceMethod)
                {
                    case "GET":
                        serviceResponse = _aiCoreService.RouteGETRequest(string.Empty, new Uri(baseUrl), apiPath,
                            service.IsReturnArray);
                        break;
                    case "POST":
                        serviceResponse = _aiCoreService.RoutePOSTRequest(string.Empty, new Uri(baseUrl), apiPath,
                            request.Payload, service.IsReturnArray);
                        break;
                }
                response.ResponseData = serviceResponse.ReturnValue;
                returnResponse.Message = serviceResponse.Message;
                returnResponse.IsSuccess = serviceResponse.IsSuccess;
                response.SetResponseDate(DateTime.UtcNow);
                returnResponse.ReturnValue = response;
                return Ok(returnResponse);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreController), nameof(InvokeAICoreService), ex.Message, string.Empty, string.Empty, string.Empty, string.Empty);
                returnResponse.Message = ex.Message;
                returnResponse.IsSuccess = false;
                return GetFaultResponse(returnResponse);
            }
        }

        [HttpPost]
        [Route("api/InvokeLangTranslationService")]
        public IActionResult InvokeLangTranslationService(Request request)
        {
            var response = new Response(request.ClientID, request.DeliveryConstructID, request.ServiceID);
            var returnResponse = new MethodReturn<Response>();
            var serviceResponse = new MethodReturn<object>();

            try
            {
                #region VALIDATIONS
                if (!CommonUtility.GetValidUser(request.UserID))
                {
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);
                }
                CommonUtility.ValidateInputFormData(request.ClientID, "ClientID", true);
                CommonUtility.ValidateInputFormData(request.DeliveryConstructID, "DeliveryConstructID", true);
                CommonUtility.ValidateInputFormData(request.ServiceID, "ServiceID", true);
                CommonUtility.ValidateInputFormData(request.CustomParam?.CorrelationId, "CustomParamCorrelationId", true);
                CommonUtility.ValidateInputFormData(Convert.ToString(request.Payload), "Payload", false);
                CommonUtility.ValidateInputFormData(Convert.ToString(request.FileKeys), "FileKeys", false);
                #endregion

                Service service = _aiCoreService.GetAiCoreServiceDetails(request.ServiceID);
                //string baseUrl = new Uri(service.ApiUrl).GetLeftPart(UriPartial.Authority);
                //string apiPath = new Uri(service.ApiUrl).PathAndQuery;
                string baseUrl = appSettings.AICorePythonURL;
                string apiPath = service.ApiUrl;

                switch (service.ServiceMethod)
                {
                    case "GET":
                        serviceResponse = _aiCoreService.RouteGETRequest(string.Empty, new Uri(baseUrl), apiPath,
                            service.IsReturnArray);
                        break;
                    case "POST":
                        serviceResponse = _aiCoreService.RoutePOSTRequest(string.Empty, new Uri(baseUrl), apiPath,
                            request.Payload, service.IsReturnArray);
                        break;
                }
                response.ResponseData = serviceResponse.ReturnValue;
                returnResponse.Message = serviceResponse.Message;
                returnResponse.IsSuccess = serviceResponse.IsSuccess;
                response.SetResponseDate(DateTime.UtcNow);
                returnResponse.ReturnValue = response;
                return Ok(returnResponse);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreController), nameof(InvokeAICoreService), ex.Message, string.Empty, string.Empty, request.ClientID, request.DeliveryConstructID);
                returnResponse.Message = ex.Message;
                returnResponse.IsSuccess = false;
                return GetFaultResponse(returnResponse);
            }
        }
        /// <summary>
        /// To train AI Core Model
        /// </summary>
        /// <param></param>
        /// <returns>Returns status as training initiated successfully</returns>
        [HttpPost]
        [Route("api/InvokeAICoreServiceWithFile")]
        public IActionResult InvokeAICoreServiceWithFile()
        {
            Request request = new Request();
            var invokeResponse = new MethodReturn<Response>();
            var nvc = Request.Form;
            var clientId = string.Empty;
            var deliveryConstructId = string.Empty;
            var serviceId = string.Empty;
            var modelName = string.Empty;
            string correlationid = null;
            var userid = string.Empty;
            string pyModelName = string.Empty;
            string modelStatus = string.Empty;
            string baseUrl = string.Empty;
            string apiPath = string.Empty;
            string uniId = null;
            int maxDataPull = 0;

            CustomParam customParam = null;
            JObject payload = new JObject();
            var returnResponse = new MethodReturn<Response>();
            var serviceResponse = new MethodReturn<object>();
            string source = string.Empty;
            List<object> selectedColumns = new List<object>();
            JObject filterAttribute = new JObject();
            string datasetUId = string.Empty;
            //Read request params
            try
            {

                foreach (var key in nvc.Keys)
                {
                    nvc.TryGetValue(key, out StringValues stringVal);
                    switch (key)
                    {
                        case "CorrelationId":
                            correlationid = stringVal.ToString();
                            break;
                        case "ClientId":
                            clientId = stringVal.ToString();
                            break;
                        case "DeliveryConstructId":
                            deliveryConstructId = stringVal.ToString();
                            break;
                        case "ServiceId":
                            serviceId = stringVal.ToString();
                            break;
                        case "UserId":
                            userid = stringVal.ToString();
                            break;
                        case "RequestPayload":
                            payload = JObject.Parse(stringVal.ToString());
                            break;
                        case "ModelName":
                            modelName = stringVal.ToString();
                            break;
                        case "CustomParam":
                            if (!string.IsNullOrWhiteSpace(stringVal.ToString()))
                                customParam = JsonConvert.DeserializeObject<CustomParam>(stringVal.ToString());
                            break;
                        case "Source":
                            source = stringVal.ToString();
                            break;
                        case "selectedColumns":
                            selectedColumns = JsonConvert.DeserializeObject<List<object>>(stringVal.ToString());
                            break;
                        case "FilterAttribute":
                            filterAttribute = JsonConvert.DeserializeObject<JObject>(stringVal.ToString());
                            break;
                        case "DataSetUId":
                            datasetUId = stringVal.ToString();
                            break;
                        case "MaxDataPull":
                            maxDataPull = !string.IsNullOrEmpty(stringVal.ToString()) ? Convert.ToInt32(stringVal) : 0;
                            break;
                    }
                }
                #region VALIDATIONS
                CommonUtility.ValidateInputFormData(correlationid, "Correlationid", true);
                CommonUtility.ValidateInputFormData(Convert.ToString(clientId), "ClientId", true);
                CommonUtility.ValidateInputFormData(Convert.ToString(deliveryConstructId), "DeliveryConstructId", true);
                CommonUtility.ValidateInputFormData(Convert.ToString(serviceId), "ServiceId", true);
                CommonUtility.ValidateInputFormData(Convert.ToString(payload), "RequestPayload", false);
                CommonUtility.ValidateInputFormData(Convert.ToString(modelName), "ModelName", false);
                CommonUtility.ValidateInputFormData(Convert.ToString(customParam?.CorrelationId), "CustomParamCorrelationId", true);
                CommonUtility.ValidateInputFormData(Convert.ToString(source), "Source", false);
                CommonUtility.ValidateInputFormData(Convert.ToString(selectedColumns), "selectedColumns", false);
                CommonUtility.ValidateInputFormData(Convert.ToString(filterAttribute), "FilterAttribute", false);
                CommonUtility.ValidateInputFormData(Convert.ToString(datasetUId), "DatasetUId", true);
                #endregion
                if (!CommonUtility.GetValidUser(userid))
                {
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);
                }
                int MaxDataPull = maxDataPull != null ? Convert.ToInt32(maxDataPull) : 0;
                Service service = _aiCoreService.GetAiCoreServiceDetails(serviceId);
                if (customParam.ReTrain == true)
                {
                    if (customParam.CorrelationId != null && customParam.CorrelationId != "")
                    {
                        correlationid = customParam.CorrelationId;
                        var corrDetails = _aiCoreService.GetAICoreModelPath(correlationid);
                        if (corrDetails == null)
                        {
                            throw new Exception("Correlation id not found");
                        }
                        else
                        {
                            if (corrDetails.ModelStatus == "InProgress" || corrDetails.ModelStatus == "Upload InProgress")
                            {
                                throw new Exception("Unable to initiate training, as dataupload/training is already in progress");
                            }
                            else
                            {
                                clientId = corrDetails.ClientId;
                                deliveryConstructId = corrDetails.DeliveryConstructId;
                                serviceId = corrDetails.ServiceId;
                                if (modelName == null)
                                    modelName = corrDetails.ModelName;
                                pyModelName = corrDetails.PythonModelName;
                                modelStatus = "InProgress";

                                if (selectedColumns.Count > 0)
                                {
                                    _aiCoreService.UpdateSelectedColumn(selectedColumns, correlationid);
                                }
                                if (service.ServiceCode == "NLPINTENT")
                                {
                                    corrDetails.DataSource = "File";
                                }
                                _aiCoreService.CreateAICoreModel(clientId, deliveryConstructId, serviceId, correlationid, corrDetails.UniId, modelName, pyModelName, modelStatus, "ReTrain is in Progress", userid, corrDetails.DataSource, null, datasetUId, MaxDataPull);
                            }

                        }
                    }
                    else
                    {
                        throw new Exception("Correlationid is mandatory");
                    }
                }
                else
                {
                    if (selectedColumns.Count == 0)
                    {
                        correlationid = Guid.NewGuid().ToString();
                    }
                    var corrDetails = _aiCoreService.GetAICoreModelPath(correlationid);
                    if (corrDetails.ModelStatus == "Upload InProgress")
                    {
                        throw new Exception("Data Upload is in Progress");
                    }
                    pyModelName = null;
                    modelStatus = "InProgress";
                    ModelsList model = _aiCoreService.GetAICoreModels(clientId, deliveryConstructId, serviceId, userid);
                    if (selectedColumns.Count == 0)
                    {
                        if (model.ModelStatus != null)
                        {
                            var isModelNameExist = model.ModelStatus.Where(x => x.ModelName.ToLower() == modelName.ToLower()).ToList();

                            if (isModelNameExist.Count > 0)
                            {
                                throw new Exception("Model Name already exist");
                            }
                            else
                            {
                                if (service.ServiceCode == "NLPINTENT")
                                {
                                    corrDetails.DataSource = "File";
                                }
                                _aiCoreService.CreateAICoreModel(clientId, deliveryConstructId, serviceId, correlationid, uniId, modelName, pyModelName, modelStatus, "Training is in Progress", userid, corrDetails.DataSource, null, datasetUId, MaxDataPull);
                            }
                        }
                        else
                        {
                            if (service.ServiceCode == "NLPINTENT")
                            {
                                corrDetails.DataSource = "File";
                            }
                            _aiCoreService.CreateAICoreModel(clientId, deliveryConstructId, serviceId, correlationid, uniId, modelName, pyModelName, modelStatus, "Training is in Progress", userid, corrDetails.DataSource, null, datasetUId, MaxDataPull);
                        }
                    }
                    else
                    {

                        if (selectedColumns.Count > 0)
                        {
                            uniId = _aiCoreService.InsertTrainrequest(selectedColumns, correlationid, filterAttribute);
                        }
                        if (service.ServiceCode == "NLPINTENT")
                        {
                            corrDetails.DataSource = "File";
                        }
                        _aiCoreService.CreateAICoreModel(clientId, deliveryConstructId, serviceId, correlationid, uniId, modelName, pyModelName, modelStatus, "Training is in Progress", userid, corrDetails.DataSource, "TrainModel", datasetUId, MaxDataPull);
                    }

                }


                payload["client_id"] = clientId;
                payload["dc_id"] = deliveryConstructId;
                payload["correlation_id"] = correlationid;
                if (service.ServiceCode == "NLPINTENT")
                {
                    payload["DataSource"] = "File";
                    payload["ApplicationId"] = "";
                }
                var response = new Response(clientId, deliveryConstructId, serviceId);

                var httpRequest = Request;
                request.FileCollection = httpRequest.Form.Files;
                request.FileKeys = new string[request.FileCollection.Count];
                for (var count = 0; count < request.FileCollection.Count; count++)
                {
                    request.FileKeys[count] = request.FileCollection[count].Name;
                }
                if (service != null)
                {
                    if (!string.IsNullOrWhiteSpace(service.PrimaryTrainApiUrl))
                    {
                        baseUrl = appSettings.AICorePythonURL;
                        apiPath = service.PrimaryTrainApiUrl;
                    }
                }

                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreController), nameof(InvokeAICoreServiceWithFile), baseUrl + apiPath, string.IsNullOrEmpty(correlationid) ? default(Guid) : new Guid(correlationid), string.Empty, string.Empty, clientId, deliveryConstructId);
                string encrypteduser = userid;
                if (appSettings.isForAllData)
                {
                    if (!string.IsNullOrEmpty(Convert.ToString(userid)))
                        encrypteduser = _encryptionDecryption.Encrypt(Convert.ToString(userid));
                }
                switch (service.ServiceMethod)
                {
                    case "GET":
                        apiPath = apiPath + "?" + "correlationId=" + correlationid + "&userId=" + encrypteduser + "&pageInfo=" + "TrainModel" + "&UniqueId=" + uniId;
                        serviceResponse = _aiCoreService.RouteGETRequest(string.Empty, new Uri(baseUrl), apiPath,
                            service.IsReturnArray);
                        break;
                    case "POST":
                        if (request.FileCollection != null)
                            serviceResponse = _aiCoreService.RoutePOSTRequest(string.Empty, new Uri(baseUrl), apiPath,
                                request.FileCollection, payload, request.FileKeys, service.IsReturnArray, correlationid);
                        else
                            serviceResponse = _aiCoreService.RoutePOSTRequest(string.Empty, new Uri(baseUrl), apiPath,
                                payload, service.IsReturnArray);
                        break;
                }
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreController), nameof(InvokeAICoreServiceWithFile), serviceResponse.ToString(), string.IsNullOrEmpty(correlationid) ? default(Guid) : new Guid(correlationid), string.Empty, string.Empty, request.ClientID, request.DeliveryConstructID);

                response.ResponseData = serviceResponse.ReturnValue;
                returnResponse.Message = serviceResponse.Message;
                returnResponse.IsSuccess = serviceResponse.IsSuccess;
                response.CorrelationId = correlationid;
                response.SetResponseDate(DateTime.UtcNow);
                returnResponse.ReturnValue = response;

                if (serviceId == "93df37dc-cc72-4105-9ad2-fd08509bc823" || serviceId == "e6f8243b-e00f-4d05-b16c-8c6a449b5a4c")
                {
                    if (!serviceResponse.IsSuccess)
                    {
                        _aiCoreService.UpdateAIServiceRequestStatus(correlationid, uniId, "E", returnResponse.Message);
                        _aiCoreService.UpdateAICoreModels(response.CorrelationId, "Error", returnResponse.Message);
                    }
                }
            }

            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AICoreController), nameof(InvokeAICoreServiceWithFile), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, request.ClientID, request.DeliveryConstructID);
                returnResponse.Message = ex.Message;
                returnResponse.IsSuccess = false;
                return BadRequest(ex.Message);
            }


            return Ok(returnResponse);
        }

        /// <summary>
        /// To train AI Core Model
        /// </summary>
        /// <param></param>
        /// <returns>Returns status as training initiated successfully</returns>
        [HttpPost]
        [Route("api/InvokeAICoreServiceSingleBulkMultipleWithFile")]
        public IActionResult InvokeAICoreServiceBulkMultipleWithFile()
        {
            Request request = new Request();
            var invokeResponse = new MethodReturn<Response>();
            var nvc = Request.Form;
            var clientId = string.Empty;
            var deliveryConstructId = string.Empty;
            var serviceId = string.Empty;
            var modelName = string.Empty;
            string correlationid = null;
            var userid = string.Empty;
            string pyModelName = string.Empty;
            string modelStatus = string.Empty;
            string baseUrl = string.Empty;
            string apiPath = string.Empty;
            string uniId = null;
            string scoreUniqueName = string.Empty;
            JObject threshold_TopnRecords = new JObject();
            List<string> stopWords = new List<string>();
            string datasetUId = string.Empty;
            int maxDataPull = 0;

            CustomParam customParam = null;
            JObject payload = new JObject();
            var returnResponse = new MethodReturn<Response>();
            var serviceResponse = new MethodReturn<object>();
            string source = string.Empty;
            List<object> selectedColumns = new List<object>();
            JObject filterAttribute = new JObject();
            //Read request params
            try
            {

                foreach (var key in nvc.Keys)
                {
                    nvc.TryGetValue(key, out StringValues stringVal);
                    switch (key)
                    {
                        case "CorrelationId":
                            correlationid = stringVal.ToString();
                            break;
                        case "ClientId":
                            clientId = stringVal.ToString();
                            break;
                        case "DeliveryConstructId":
                            deliveryConstructId = stringVal.ToString();
                            break;
                        case "ServiceId":
                            serviceId = stringVal.ToString();
                            break;
                        case "UserId":
                            userid = stringVal.ToString();
                            break;
                        case "RequestPayload":
                            CommonUtility.ValidateInputFormData(Convert.ToString(stringVal), "RequestPayload", false);
                            payload = JObject.Parse(stringVal.ToString());
                            break;
                        case "ModelName":
                            modelName = stringVal.ToString();
                            break;
                        case "CustomParam":
                            if (!string.IsNullOrWhiteSpace(stringVal.ToString()))
                                customParam = JsonConvert.DeserializeObject<CustomParam>(stringVal.ToString());
                            break;
                        case "Source":
                            source = stringVal.ToString();
                            break;
                        case "selectedColumns":
                            CommonUtility.ValidateInputFormData(Convert.ToString(stringVal), "selectedColumns", false);
                            selectedColumns = JsonConvert.DeserializeObject<List<object>>(stringVal.ToString());
                            break;
                        case "FilterAttribute":
                            CommonUtility.ValidateInputFormData(Convert.ToString(stringVal), "FilterAttribute", false);
                            filterAttribute = JsonConvert.DeserializeObject<JObject>(stringVal.ToString());
                            break;
                        case "ScoreUniqueName":
                            scoreUniqueName = stringVal.ToString();
                            break;
                        case "Threshold_TopnRecords":
                            if (!string.IsNullOrWhiteSpace(stringVal.ToString()))
                            {
                                CommonUtility.ValidateInputFormData(Convert.ToString(stringVal), "Threshold_TopnRecords", false);
                                threshold_TopnRecords = JsonConvert.DeserializeObject<JObject>(stringVal.ToString());
                            }
                            break;
                        case "StopWords":
                            CommonUtility.ValidateInputFormData(Convert.ToString(stringVal), "StopWords", false);
                            stopWords = JsonConvert.DeserializeObject<List<string>>(stringVal.ToString());
                            break;
                        case "DataSetUId":
                            datasetUId = stringVal.ToString();
                            break;
                        case "MaxDataPull":
                            maxDataPull = !string.IsNullOrEmpty(stringVal.ToString()) ? Convert.ToInt32(stringVal) : 0;
                            break;
                    }
                }
                #region VALIDATIONS
                if (!CommonUtility.GetValidUser(userid))
                {
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);
                }
                CommonUtility.ValidateInputFormData(correlationid, "Correlationid", true);
                CommonUtility.ValidateInputFormData(Convert.ToString(clientId), "ClientId", true);
                CommonUtility.ValidateInputFormData(Convert.ToString(deliveryConstructId), "DeliveryConstructId", true);
                CommonUtility.ValidateInputFormData(Convert.ToString(serviceId), "ServiceId", true);
                CommonUtility.ValidateInputFormData(Convert.ToString(modelName), "ModelName", false);
                CommonUtility.ValidateInputFormData(Convert.ToString(customParam?.CorrelationId), "CustomParamCorrelationId", true);
                CommonUtility.ValidateInputFormData(Convert.ToString(source), "Source", false);
                CommonUtility.ValidateInputFormData(Convert.ToString(scoreUniqueName), "ScoreUniqueName", false);
                CommonUtility.ValidateInputFormData(Convert.ToString(datasetUId), "DatasetUId", true);
                #endregion

                int MaxDataPull = maxDataPull != null ? Convert.ToInt32(maxDataPull) : 0;
                Service service = _aiCoreService.GetAiCoreServiceDetails(serviceId);
                if (customParam.ReTrain == true)
                {
                    if (customParam.CorrelationId != null && customParam.CorrelationId != "")
                    {
                        correlationid = customParam.CorrelationId;
                        var corrDetails = _aiCoreService.GetAICoreModelPath(correlationid);
                        if (corrDetails == null)
                        {
                            throw new Exception("Correlation id not found");
                        }
                        else
                        {
                            if (corrDetails.ModelStatus == "InProgress" || corrDetails.ModelStatus == "Upload InProgress")
                            {
                                throw new Exception("Unable to initiate training, as dataupload/training is already in progress");
                            }
                            else
                            {
                                clientId = corrDetails.ClientId;
                                deliveryConstructId = corrDetails.DeliveryConstructId;
                                serviceId = corrDetails.ServiceId;
                                if (modelName == null)
                                    modelName = corrDetails.ModelName;
                                pyModelName = corrDetails.PythonModelName;
                                modelStatus = "InProgress";

                                if (selectedColumns.Count > 0)
                                {
                                    _aiCoreService.UpdateSelectedColumn(selectedColumns, correlationid);
                                }
                                if (service.ServiceCode == "NLPINTENT")
                                {
                                    corrDetails.DataSource = "File";
                                }
                                _aiCoreService.CreateAICoreModel(clientId, deliveryConstructId, serviceId, correlationid, corrDetails.UniId, modelName, pyModelName, modelStatus, "ReTrain is in Progress", userid, corrDetails.DataSource, null, datasetUId, MaxDataPull);
                            }

                        }
                    }
                    else
                    {
                        throw new Exception("Correlationid is mandatory");
                    }
                }
                else
                {
                    if (selectedColumns.Count == 0)
                    {
                        correlationid = Guid.NewGuid().ToString();
                    }
                    var corrDetails = _aiCoreService.GetAICoreModelPath(correlationid);
                    if (corrDetails.ModelStatus == "Upload InProgress")
                    {
                        throw new Exception("Data Upload is in Progress");
                    }
                    pyModelName = null;
                    modelStatus = "InProgress";
                    ModelsList model = _aiCoreService.GetAICoreModels(clientId, deliveryConstructId, serviceId, userid);
                    if (selectedColumns.Count == 0)
                    {
                        if (model.ModelStatus != null)
                        {
                            var isModelNameExist = model.ModelStatus.Where(x => x.ModelName.ToLower() == modelName.ToLower()).ToList();

                            if (isModelNameExist.Count > 0)
                            {
                                throw new Exception("Model Name already exist");
                            }
                            else
                            {
                                if (service.ServiceCode == "NLPINTENT")
                                {
                                    corrDetails.DataSource = "File";
                                }
                                _aiCoreService.CreateAICoreModel(clientId, deliveryConstructId, serviceId, correlationid, uniId, modelName, pyModelName, modelStatus, "Training is in Progress", userid, corrDetails.DataSource, null, datasetUId, MaxDataPull);
                            }
                        }
                        else
                        {
                            if (service.ServiceCode == "NLPINTENT")
                            {
                                corrDetails.DataSource = "File";
                            }
                            _aiCoreService.CreateAICoreModel(clientId, deliveryConstructId, serviceId, correlationid, uniId, modelName, pyModelName, modelStatus, "Training is in Progress", userid, corrDetails.DataSource, null, datasetUId, MaxDataPull);
                        }
                    }
                    else
                    {

                        if (selectedColumns.Count > 0)
                        {
                            uniId = _aiCoreService.InsertTrainrequestBulkMultiple(selectedColumns, correlationid, filterAttribute, scoreUniqueName, threshold_TopnRecords, stopWords, maxDataPull);
                        }
                        if (service.ServiceCode == "NLPINTENT")
                        {
                            corrDetails.DataSource = "File";
                        }
                        _aiCoreService.CreateAICoreModel(clientId, deliveryConstructId, serviceId, correlationid, uniId, modelName, pyModelName, modelStatus, "Training is in Progress", userid, corrDetails.DataSource, "TrainModel", datasetUId, MaxDataPull);
                    }

                }


                payload["client_id"] = clientId;
                payload["dc_id"] = deliveryConstructId;
                payload["correlation_id"] = correlationid;
                if (service.ServiceCode == "NLPINTENT")
                {
                    payload["DataSource"] = "File";
                    payload["ApplicationId"] = "";
                }
                var response = new Response(clientId, deliveryConstructId, serviceId);

                var httpRequest = Request;
                request.FileCollection = httpRequest.Form.Files;
                request.FileKeys = new string[request.FileCollection.Count];
                for (var count = 0; count < request.FileCollection.Count; count++)
                {
                    request.FileKeys[count] = request.FileCollection[count].Name;
                }
                if (service != null)
                {
                    if (!string.IsNullOrWhiteSpace(service.PrimaryTrainApiUrl))
                    {
                        baseUrl = appSettings.AICorePythonURL;
                        apiPath = service.PrimaryTrainApiUrl;
                    }
                }

                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreController), nameof(InvokeAICoreServiceWithFile), baseUrl + apiPath, string.IsNullOrEmpty(correlationid) ? default(Guid) : new Guid(correlationid), string.Empty, string.Empty, clientId, deliveryConstructId);
                string encrypteduser = userid;
                if (appSettings.isForAllData)
                {
                    if (!string.IsNullOrEmpty(Convert.ToString(userid)))
                        encrypteduser = _encryptionDecryption.Encrypt(Convert.ToString(userid));
                }
                switch (service.ServiceMethod)
                {
                    case "GET":
                        apiPath = apiPath + "?" + "correlationId=" + correlationid + "&userId=" + encrypteduser + "&pageInfo=" + "TrainModel" + "&UniqueId=" + uniId;
                        serviceResponse = _aiCoreService.RouteGETRequest(string.Empty, new Uri(baseUrl), apiPath,
                            service.IsReturnArray);
                        break;
                    case "POST":
                        if (request.FileCollection != null)
                            serviceResponse = _aiCoreService.RoutePOSTRequest(string.Empty, new Uri(baseUrl), apiPath,
                                request.FileCollection, payload, request.FileKeys, service.IsReturnArray, correlationid);
                        else
                            serviceResponse = _aiCoreService.RoutePOSTRequest(string.Empty, new Uri(baseUrl), apiPath,
                                payload, service.IsReturnArray);
                        break;
                }
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreController), nameof(InvokeAICoreServiceWithFile), serviceResponse.ToString(), string.IsNullOrEmpty(correlationid) ? default(Guid) : new Guid(correlationid), string.Empty, string.Empty, request.ClientID, request.DeliveryConstructID);

                response.ResponseData = serviceResponse.ReturnValue;
                returnResponse.Message = serviceResponse.Message;
                returnResponse.IsSuccess = serviceResponse.IsSuccess;
                response.CorrelationId = correlationid;
                response.SetResponseDate(DateTime.UtcNow);
                returnResponse.ReturnValue = response;

                if (serviceId == "93df37dc-cc72-4105-9ad2-fd08509bc823" || serviceId == "e6f8243b-e00f-4d05-b16c-8c6a449b5a4c")
                {
                    if (!serviceResponse.IsSuccess)
                    {
                        _aiCoreService.UpdateAIServiceRequestStatus(correlationid, uniId, "E", returnResponse.Message);
                        _aiCoreService.UpdateAICoreModels(response.CorrelationId, "Error", returnResponse.Message);
                    }
                }
            }

            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreController), nameof(InvokeAICoreServiceWithFile), ex.Message + ex.StackTrace, string.Empty, string.Empty, request.ClientID, request.DeliveryConstructID);
                returnResponse.Message = ex.Message;
                returnResponse.IsSuccess = false;
                return BadRequest(ex.Message);
            }


            return Ok(returnResponse);
        }



        /// <summary>
        /// To predict/evaluate model based on correlation id
        /// </summary>
        /// <param></param>
        /// <returns>Returns model prediction</returns>
        [HttpPost]
        [Route("api/Evaluate")]
        public IActionResult Evaluate(string correlationid, int DecimalPlaces, [FromBody] Request request)
        {
            var returnResponse = new MethodReturn<Response>();
            try
            {
                #region VALIDATIONS
                if (!CommonUtility.GetValidUser(request.UserID))
                {
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);
                }

                var serviceResponse = new MethodReturn<object>();
                CommonUtility.ValidateInputFormData(request.ClientID, "ClientID", true);
                CommonUtility.ValidateInputFormData(request.DeliveryConstructID, "DeliveryConstructID", true);
                CommonUtility.ValidateInputFormData(request.ServiceID, "ServiceID", true);
                CommonUtility.ValidateInputFormData(Convert.ToString(request.CustomParam?.CorrelationId), "CustomParamCorrelationId", true);
                CommonUtility.ValidateInputFormData(Convert.ToString(request.Payload), "Payload", false);
                CommonUtility.ValidateInputFormData(Convert.ToString(request.FileKeys), "FileKeys", false);
                CommonUtility.ValidateInputFormData(correlationid, CONSTANTS.CorrelationId, true);
                #endregion

                AICoreModels aICoreModels = _aiCoreService.GetAICoreModelPath(correlationid);
                var response = new Response(aICoreModels.ClientId, aICoreModels.DeliveryConstructId, aICoreModels.ServiceId);
                Service service = _aiCoreService.GetAiCoreServiceDetails(aICoreModels.ServiceId);
                //string baseUrl = new Uri(service.ApiUrl).GetLeftPart(UriPartial.Authority);
                //string apiPath = new Uri(service.ApiUrl).PathAndQuery;
                string baseUrl = appSettings.AICorePythonURL;
                string apiPath = service.ApiUrl;
                request.Payload["model_name"] = aICoreModels.PythonModelName;
                request.Payload["client_id"] = aICoreModels.ClientId;
                request.Payload["dc_id"] = aICoreModels.DeliveryConstructId;
                request.Payload["correlation_id"] = aICoreModels.CorrelationId;
                response.CorrelationId = aICoreModels.CorrelationId;
                //Asset Usage
                auditTrailLog.CorrelationId = aICoreModels.CorrelationId;
                auditTrailLog.ApplicationID = aICoreModels.ApplicationId;
                auditTrailLog.ClientId = aICoreModels.ClientId;
                auditTrailLog.DCID = aICoreModels.DeliveryConstructId;
                auditTrailLog.UseCaseId = aICoreModels.UsecaseId;
                auditTrailLog.CreatedBy = aICoreModels.CreatedBy;
                auditTrailLog.ProcessName = CONSTANTS.PredictionName;
                auditTrailLog.UsageType = CONSTANTS.AssetUsage;
                auditTrailLog.FeatureName = service.ServiceName;
                CommonUtility.AuditTrailLog(auditTrailLog, configSetting);

                switch (service.ServiceMethod)
                {
                    case "GET":
                        serviceResponse = _aiCoreService.RouteGETRequest(string.Empty, new Uri(baseUrl), apiPath,
                            service.IsReturnArray);
                        break;
                    case "POST":
                        serviceResponse = _aiCoreService.RoutePOSTRequest(string.Empty, new Uri(baseUrl), apiPath,
                            request.Payload, service.IsReturnArray);
                        break;
                }
                #region Decimal Place
                //if (serviceResponse.ReturnValue != null)
                //{
                //    BsonArray inputD = new BsonArray();
                //    dynamic datas = JObject.Parse(Convert.ToString(serviceResponse.ReturnValue));
                //    inputD = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonArray>(Convert.ToString(datas["result"]["entities"]));
                //    BsonArray resultD = CommonUtility.GetDataAfterDecimalPrecisionIntentEntity(inputD, DecimalPlaces, 0, true, out bool RequiredFlag);
                //    bool Flag = false;
                //    if (RequiredFlag)
                //    {
                //        Flag = true;
                //        datas["result"]["entities"] = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(BsonTypeMapper.MapToDotNetValue(resultD)));
                //    }
                //    //inputD = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonArray>(Convert.ToString(datas["result"]["intent"]));
                //    //resultD = CommonUtility.GetDataAfterDecimalPrecisionIntentEntity(inputD, DecimalPlaces, 0, true, out RequiredFlag);
                //    //if (RequiredFlag)
                //    //{
                //    //    Flag = true;
                //    //    datas["result"]["intent"] = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(BsonTypeMapper.MapToDotNetValue(resultD)));
                //    //}
                //    inputD = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonArray>(Convert.ToString(datas["result"]["intent_ranking"]));
                //    resultD = CommonUtility.GetDataAfterDecimalPrecisionIntentEntity(inputD, DecimalPlaces, 0, true, out RequiredFlag);
                //    if (RequiredFlag)
                //    {
                //        Flag = true;
                //        datas["result"]["intent_ranking"] = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(BsonTypeMapper.MapToDotNetValue(resultD)));
                //    }
                //    if (Flag)
                //    {
                //        if (service.IsReturnArray)
                //            serviceResponse.ReturnValue = JsonConvert.DeserializeObject<List<JObject>>(Convert.ToString(datas));
                //        else
                //            serviceResponse.ReturnValue = JsonConvert.DeserializeObject<JObject>(Convert.ToString(datas));
                //    }
                //}
                #endregion Decimal Place
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreController), nameof(Evaluate), serviceResponse.ToString(), string.IsNullOrEmpty(auditTrailLog.CorrelationId) ? default(Guid) : new Guid(auditTrailLog.CorrelationId), auditTrailLog.ApplicationID, string.Empty, auditTrailLog.ClientId, auditTrailLog.DCID);
                response.ResponseData = serviceResponse.ReturnValue;
                returnResponse.Message = serviceResponse.Message;
                returnResponse.IsSuccess = serviceResponse.IsSuccess;
                response.SetResponseDate(DateTime.UtcNow);
                returnResponse.ReturnValue = response;
                return Ok(returnResponse);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AICoreController), nameof(Evaluate), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, request.ClientID, request.DeliveryConstructID);
                returnResponse.Message = ex.Message;
                returnResponse.IsSuccess = false;
                return GetFaultResponse(returnResponse);
            }
        }

        //for generic api
        [HttpPost]
        [Route("api/AIModelTraining")]
        public IActionResult AIModelTraining()
        {
            try
            {
                IsTrainingEnabled(appSettings.EnableTraining);
                GetAppService();
                string resourceId = Request.Headers["resourceId"];
                IFormCollection collection = HttpContext.Request.Form;
                #region VALIDATIONS
                CommonUtility.ValidateInputFormData(Convert.ToString(collection["ApplicationId"]), "ApplicationId", true);
                CommonUtility.ValidateInputFormData(Convert.ToString(collection["ClientId"]), "ClientId", true);
                CommonUtility.ValidateInputFormData(Convert.ToString(collection["DeliveryConstructId"]), "DeliveryConstructId", true);
                CommonUtility.ValidateInputFormData(Convert.ToString(collection["ServiceId"]), "ServiceId", true);
                CommonUtility.ValidateInputFormData(Convert.ToString(collection["UsecaseId"]), "UsecaseId", true);
                CommonUtility.ValidateInputFormData(Convert.ToString(collection["ModelName"]), "ModelName", false);
                CommonUtility.ValidateInputFormData(Convert.ToString(collection["DataSource"]), "DataSource", false);
                CommonUtility.ValidateInputFormData(Convert.ToString(collection["DataSourceDetails"]), "DataSourceDetails", false);
                CommonUtility.ValidateInputFormData(Convert.ToString(collection["ResponseCallbackUrl"]), "ResponseCallbackUrl", false);
                CommonUtility.ValidateInputFormData(Convert.ToString(collection["CorrelationId"]), "CorrelationId", true);
                CommonUtility.ValidateInputFormData(Convert.ToString(collection["Language"]), "Language", false);
                CommonUtility.ValidateInputFormData(Convert.ToString(collection["DataSetUId"]), "DataSetUId", true);
                CommonUtility.ValidateInputFormData(Convert.ToString(collection[CONSTANTS.pad]), CONSTANTS.pad, false);
                CommonUtility.ValidateInputFormData(Convert.ToString(collection[CONSTANTS.metrics]), CONSTANTS.metrics, false);
                CommonUtility.ValidateInputFormData(Convert.ToString(collection[CONSTANTS.InstaMl]), CONSTANTS.InstaMl, false);
                CommonUtility.ValidateInputFormData(Convert.ToString(collection[CONSTANTS.EntitiesName]), CONSTANTS.EntitiesName, false);
                CommonUtility.ValidateInputFormData(Convert.ToString(collection[CONSTANTS.MetricNames]), CONSTANTS.MetricNames, false);
                #endregion

                return Ok(_aiCoreService.TrainAIServiceModel(HttpContext, resourceId));

            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AICoreController), nameof(AIModelTraining), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
        }

        [HttpPost]
        [Route("api/AIModelBulkTraining")]
        public IActionResult AIModelBulkTraining()
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreController), nameof(AIModelBulkTraining), CONSTANTS.START, string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                #region VALIDATIONS
                IFormCollection collection = HttpContext.Request.Form;
                CommonUtility.ValidateInputFormData(Convert.ToString(collection["ApplicationId"]), "ApplicationId", true);
                CommonUtility.ValidateInputFormData(Convert.ToString(collection["ClientId"]), "ClientId", true);
                CommonUtility.ValidateInputFormData(Convert.ToString(collection["DeliveryConstructId"]), "DeliveryConstructId", true);
                CommonUtility.ValidateInputFormData(Convert.ToString(collection["ServiceId"]), "ServiceId", true);
                CommonUtility.ValidateInputFormData(Convert.ToString(collection["UsecaseId"]), "UsecaseId", true);
                CommonUtility.ValidateInputFormData(Convert.ToString(collection["ModelName"]), "ModelName", false);
                CommonUtility.ValidateInputFormData(Convert.ToString(collection["DataSource"]), "DataSource", false);
                CommonUtility.ValidateInputFormData(Convert.ToString(collection["DataSourceDetails"]), "DataSourceDetails", false);
                CommonUtility.ValidateInputFormData(Convert.ToString(collection["ConfigurationDetails"]), "ConfigurationDetails", false);
                CommonUtility.ValidateInputFormData(Convert.ToString(collection["ResponseCallbackUrl"]), "ResponseCallbackUrl", false);
                #endregion

                return Ok(_aiCoreService.TrainAIServiceModelBulkTrain(HttpContext));
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AICoreController), nameof(AIModelBulkTraining), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreController), nameof(AIModelBulkTraining), CONSTANTS.END, string.Empty, string.Empty, string.Empty, string.Empty);
        }

        [HttpPost]
        [Route("api/AIModelBulkPredict")]
        public IActionResult AIModelBulkPredict()
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreController), nameof(AIModelBulkPredict), "START", string.Empty, string.Empty, string.Empty, string.Empty);

            try
            {
                string pageInfo = string.Empty;
                IFormCollection formCollection = HttpContext.Request.Form;
                string fco = formCollection.ToString();
                string cid = formCollection[CONSTANTS.CorrelationId].ToString();
                string uid = formCollection[CONSTANTS.UniId].ToString();
                string pi = formCollection[CONSTANTS.pageInfo].ToString();
                string sta = formCollection[CONSTANTS.Status].ToString();

                #region VALIDATIONS
                CommonUtility.ValidateInputFormData(Convert.ToString(formCollection[CONSTANTS.CorrelationId]), CONSTANTS.CorrelationId, true);
                CommonUtility.ValidateInputFormData(Convert.ToString(formCollection[CONSTANTS.UniId]), CONSTANTS.UniId, true);
                CommonUtility.ValidateInputFormData(Convert.ToString(formCollection[CONSTANTS.pageInfo]), CONSTANTS.pageInfo, false);
                CommonUtility.ValidateInputFormData(Convert.ToString(formCollection[CONSTANTS.Status]), CONSTANTS.Status, false);
                CommonUtility.ValidateInputFormData(Convert.ToString(formCollection[CONSTANTS.Message]), CONSTANTS.Message, false);
                #endregion

                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreController), nameof(AIModelBulkPredict), "formCollection :" + fco, string.Empty, string.Empty, string.Empty, string.Empty);
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreController), nameof(AIModelBulkPredict), "cid :" + cid, string.Empty, string.Empty, string.Empty, string.Empty);
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreController), nameof(AIModelBulkPredict), "uid :" + uid, string.Empty, string.Empty, string.Empty, string.Empty);
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreController), nameof(AIModelBulkPredict), "pi :" + pi, string.Empty, string.Empty, string.Empty, string.Empty);
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreController), nameof(AIModelBulkPredict), "sta :" + sta, string.Empty, string.Empty, string.Empty, string.Empty);
                AICoreModels aICoreModels = _aiCoreService.GetAICoreModelPath(formCollection[CONSTANTS.CorrelationId].ToString());

                Service service = _aiCoreService.GetAiCoreServiceDetails(aICoreModels.ServiceId);
                switch (service.ServiceCode)
                {
                    case "SIMILARITYANALYTICS":
                        pageInfo = "EvaluateSimilarityAanalytics";
                        break;
                }

                if (formCollection[CONSTANTS.pageInfo].ToString() == CONSTANTS.TrainingName)
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreController), nameof(AIModelBulkPredict), "Call Prediction API - " + cid,
                         string.IsNullOrEmpty(aICoreModels.CorrelationId) ? default(Guid) : new Guid(aICoreModels.CorrelationId), aICoreModels.ApplicationId, string.Empty, aICoreModels.ClientId, aICoreModels.DeliveryConstructId);

                    string status = formCollection[CONSTANTS.Status].ToString();
                    string message = formCollection[CONSTANTS.Message].ToString();
                    if (status == CONSTANTS.C)
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreController), nameof(AIModelBulkPredict), "Calling Prediction API - " + formCollection[CONSTANTS.CorrelationId].ToString(),
                            string.IsNullOrEmpty(aICoreModels.CorrelationId) ? default(Guid) : new Guid(aICoreModels.CorrelationId), aICoreModels.ApplicationId, string.Empty, aICoreModels.ClientId, aICoreModels.DeliveryConstructId);

                        //insert AIServicePrediction to track prediction 
                        _aiCoreService.InsertAIServicePredictionRequest(aICoreModels);
                        string user = aICoreModels.CreatedBy;
                        if (appSettings.isForAllData)
                        {
                            if (!string.IsNullOrEmpty(Convert.ToString(aICoreModels.CreatedBy)))
                                user = _encryptionDecryption.Encrypt(Convert.ToString(aICoreModels.CreatedBy));
                        }
                        string baseUrl = appSettings.AICorePythonURL;
                        string apiPath = service.ApiUrl;
                        JObject Payload = new JObject();
                        Payload["PageInfo"] = pageInfo;
                        Payload["CorrelationId"] = formCollection[CONSTANTS.CorrelationId].ToString();
                        Payload["UniqueId"] = aICoreModels.UniId;
                        Payload["Bulk"] = CONSTANTS.BulkforTO;
                        Payload["UserId"] = user;
                        Payload["Params"] = "";
                        Payload["noOfResults"] = 5;
                        var returnResponse = new MethodReturn<Response>();
                        var serviceResponse = new MethodReturn<object>();

                        var response = new Response(aICoreModels.ClientId, aICoreModels.DeliveryConstructId, aICoreModels.ServiceId);
                        serviceResponse = _aiCoreService.RoutePOSTRequest(string.Empty, new Uri(baseUrl), apiPath, Payload, service.IsReturnArray);
                        if (!serviceResponse.IsSuccess)
                        {
                            _aiCoreService.SendTONotifications(aICoreModels, "Error", "Python prediction api call failed -" + serviceResponse.Message);

                            return Ok();
                        }
                        else
                        {
                            return Ok();
                        }
                    }
                    else
                    {
                        _aiCoreService.SendTONotifications(aICoreModels, "Error", "Training error -" + message);

                        return Ok();
                    }
                }
                else if (formCollection[CONSTANTS.pageInfo].ToString() == CONSTANTS.Prediction)
                {
                    string status = formCollection[CONSTANTS.Status].ToString();
                    string message = formCollection[CONSTANTS.Message].ToString();
                    _aiCoreService.SendTONotifications(aICoreModels, status, message);

                    return Ok();
                }
                else
                {
                    return GetFaultResponse("Invalid PageInfo");
                }

            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AICoreController), nameof(AIModelBulkPredict), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);

                return GetFaultResponse(ex.Message);
            }
        }
        /// <summary>
        /// Save AI Model Usecase
        /// </summary>
        /// <param></param>
        /// <returns>Returns training response</returns>
        [HttpPost]
        [Route("api/SaveUsecase")]
        public IActionResult SaveUsecase(UsecaseDetails usecaseDetails)
        {
            try
            {
                if (usecaseDetails == null)
                {
                    return GetFaultResponse(Resource.IngrainResx.InputFieldsAreNull);
                }
                #region VALIDATIONS
                if (!CommonUtility.GetValidUser(usecaseDetails.CreatedBy))
                {
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);
                }
                else if (!CommonUtility.GetValidUser(usecaseDetails.ModifiedBy))
                {
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);
                }
                CommonUtility.ValidateInputFormData(usecaseDetails.UsecaseName, "UsecaseName", false);
                CommonUtility.ValidateInputFormData(usecaseDetails.CorrelationId, "CorrelationId", true);
                CommonUtility.ValidateInputFormData(usecaseDetails.ServiceId, "ServiceId", true);
                CommonUtility.ValidateInputFormData(usecaseDetails.ModelName, "ModelName", false);
                CommonUtility.ValidateInputFormData(usecaseDetails.Description, "Description", false);
                CommonUtility.ValidateInputFormData(usecaseDetails.ApplicationName, "ApplicationName", false);
                CommonUtility.ValidateInputFormData(usecaseDetails.ApplicationId, "ApplicationId", true);
                CommonUtility.ValidateInputFormData(usecaseDetails.SourceName, "SourceName", false);
                CommonUtility.ValidateInputFormData(Convert.ToString(usecaseDetails.SourceDetails), "SourceDetails", false);
                if (usecaseDetails.InputColumns != null)
                    CommonUtility.ValidateInputFormData(Convert.ToString(usecaseDetails.InputColumns), "InputColumns", false);
                if (usecaseDetails.StopWords != null)
                    usecaseDetails.StopWords.ForEach(x => CommonUtility.ValidateInputFormData(x, "StopWords", false));
                CommonUtility.ValidateInputFormData(usecaseDetails.ScoreUniqueName, "ScoreUniqueName", false);
                CommonUtility.ValidateInputFormData(Convert.ToString(usecaseDetails.Threshold_TopnRecords), "Threshold_TopnRecords", false);
                CommonUtility.ValidateInputFormData(usecaseDetails.SourceURL, "SourceURL", false);
                CommonUtility.ValidateInputFormData(usecaseDetails.DataSetUID, "DataSetUID", true);
                #endregion

                return Ok(_aiCoreService.SaveUsecase(usecaseDetails));

            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AICoreController), nameof(SaveUsecase), ex.Message + ex.StackTrace, ex, usecaseDetails.ApplicationId, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
        }

        /// <summary>
        /// AI service Ingest Data for Similarity Analytics and Next word Prediction
        /// </summary>
        /// <param name="ModelName"></param>
        /// <param name="userId"></param>
        /// <param name="clientUID"></param>
        /// <param name="deliveryUID"></param>
        /// <param name="ParentFileName"></param>
        /// <param name="MappingFlag"></param>
        /// <param name="Source"></param>
        /// <param name="UploadFileType"></param>
        /// <param name="Category"></param>
        /// <param name="Uploadtype"></param>
        /// <param name="DBEncryption"></param>
        /// <param name="ServiceId"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/AIServiceIngestData")]
        [DisableRequestSizeLimit]
        public IActionResult AIServiceIngestData(string ModelName, string userId, string clientUID, string deliveryUID, string ParentFileName, string MappingFlag, string Source, string UploadFileType, string Category, string Uploadtype, bool DBEncryption, string ServiceId, string Language, string E2EUID)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreController), nameof(AIServiceIngestData), "START", string.Empty, string.Empty, clientUID, deliveryUID);

            try
            {
                if (string.IsNullOrEmpty(ModelName)
                    || string.IsNullOrEmpty(clientUID)
                    || string.IsNullOrEmpty(deliveryUID))
                {
                    return GetFaultResponse(Resource.IngrainResx.InputFieldsAreNull);
                }
                #region VALIDATIONS
                if (!CommonUtility.GetValidUser(userId))
                {
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);
                }
                var fileCollection = HttpContext.Request.Form.Files;
                if (CommonUtility.ValidateFileUploaded(fileCollection))
                {
                    return GetFaultResponse(Resource.IngrainResx.InValidFileName);
                }
                IFormCollection requestPayload = HttpContext.Request.Form;
                CommonUtility.ValidateInputFormData(Convert.ToString(requestPayload["CorrelationId"]), "CorrelationId", true);
                CommonUtility.ValidateInputFormData(Convert.ToString(requestPayload["DataSetUId"]), "DataSetUId", true);
                CommonUtility.ValidateInputFormData(Convert.ToString(requestPayload[CONSTANTS.pad]), CONSTANTS.pad, false);
                CommonUtility.ValidateInputFormData(Convert.ToString(requestPayload[CONSTANTS.metrics]), CONSTANTS.metrics, false);
                CommonUtility.ValidateInputFormData(Convert.ToString(requestPayload[CONSTANTS.instaML]), CONSTANTS.instaML, false);
                CommonUtility.ValidateInputFormData(Convert.ToString(requestPayload[CONSTANTS.EntitiesName]), CONSTANTS.EntitiesName, false);
                CommonUtility.ValidateInputFormData(Convert.ToString(requestPayload[CONSTANTS.MetricNames]), CONSTANTS.MetricNames, false);
                CommonUtility.ValidateInputFormData(Convert.ToString(requestPayload["Custom"]), "Custom", false);
                CommonUtility.ValidateInputFormData(Convert.ToString(requestPayload["Training"]), "Training", false);
                CommonUtility.ValidateInputFormData(Convert.ToString(requestPayload["Prediction"]), "Prediction", false);
                CommonUtility.ValidateInputFormData(Convert.ToString(requestPayload["Retraining"]), "Retraining", false);
                CommonUtility.ValidateInputFormData(clientUID, CONSTANTS.ClientId, true);
                CommonUtility.ValidateInputFormData(deliveryUID, "deliveryUID", true);
                CommonUtility.ValidateInputFormData(ServiceId, CONSTANTS.ServiceID, true);
                CommonUtility.ValidateInputFormData(E2EUID, CONSTANTS.E2EUID, true);
                #endregion

                var result = _aiCoreService.AISeriveIngestData(ModelName, userId, clientUID, deliveryUID, ParentFileName, MappingFlag, Source, Category, HttpContext, Uploadtype, DBEncryption, ServiceId, Language, E2EUID);
                return GetSuccessWithMessageResponse(result);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AICoreController), nameof(AIServiceIngestData), ex.Message, ex, string.Empty, string.Empty, clientUID, deliveryUID);

                return GetFaultResponse(ex.Message);
            }
        }

        /// <summary>
        /// Monitor DB 
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="pageInfo"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("api/AIServiceIngestStatus")]
        public IActionResult AIServiceIngestStatus(string correlationId, string pageInfo)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreController), nameof(AIServiceIngestStatus), "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                if (!string.IsNullOrEmpty(correlationId))
                {
                    var result = _aiCoreService.AIServiceIngestStatus(correlationId, pageInfo);
                    return GetSuccessWithMessageResponse(result);
                }
                else
                {
                    return GetSuccessWithMessageResponse(Resource.IngrainResx.EmptyData);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AICoreController), nameof(AIServiceIngestStatus), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
        }



        [HttpGet]
        [Route("api/RetrainModelDetails")]
        public IActionResult RetrainModelDetails(string correlationId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreController), nameof(AIServiceIngestStatus), "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                if (!string.IsNullOrEmpty(correlationId))
                {
                    //string columnsData = Convert.ToString(request);
                    //var dataContent = JObject.Parse(columnsData.ToString());
                    //var dynamicColumns = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(columnsData);
                    var result = _aiCoreService.RetrainModelDetails(correlationId); //modelEngineeringService.RunTest(dynamicColumns, out featurePredictionTest);
                    return GetSuccessWithMessageResponse(result);
                }
                else
                {
                    return GetSuccessWithMessageResponse(Resource.IngrainResx.EmptyData);
                }

            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AICoreController), nameof(AIServiceIngestStatus), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
        }

        //[HttpGet]
        //[Route("api/GetBulkPrediction")]
        //public IActionResult GetBulkPrediction(string correlationId)
        //{
        //    LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreController), nameof(GetBulkPrediction), "START");
        //    try
        //    {
        //        if (!string.IsNullOrEmpty(correlationId))
        //        {
        //            BulkPrediction.BulkPredictionData predictData = _aiCoreService.GetBulkPredictionDetails(correlationId);
        //            return GetSuccessWithMessageResponse(predictData);
        //        }
        //        else
        //        {
        //            return GetSuccessWithMessageResponse(Resource.IngrainResx.EmptyData);
        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        LOGGING.LogManager.Logger.LogErrorMessage(typeof(AICoreController), nameof(GetBulkPrediction), ex.Message, ex);
        //        return GetFaultResponse(ex.Message + ex.StackTrace);
        //    }
        //}

        /// <summary>
        /// To predict/evaluate model based on correlation id
        /// </summary>
        /// <param></param>
        /// <returns>Returns model prediction</returns>
        [HttpPost]
        [Route("api/EvaluateModel")]
        public IActionResult EvaluateModel([FromBody] dynamic requestBody)
        {
            string columnsData = Convert.ToString(requestBody);
            var dataContent = JObject.Parse(columnsData.ToString());

            try
            {
                IsPredictionEnabled(appSettings.EnablePrediction);
                #region VALIDATIONS
                CommonUtility.ValidateInputFormData(Convert.ToString(dataContent["CorrelationId"]), "CorrelationId", true);
                CommonUtility.ValidateInputFormData(Convert.ToString(dataContent["ClientID"]), CONSTANTS.ClientID, true);
                CommonUtility.ValidateInputFormData(Convert.ToString(dataContent["DCUID"]), CONSTANTS.DCUID, true);
                CommonUtility.ValidateInputFormData(Convert.ToString(dataContent["UniId"]), CONSTANTS.UniId, true);
                CommonUtility.ValidateInputFormData(Convert.ToString(dataContent["CreatedDate"]), CONSTANTS.CreatedDate, false);
                CommonUtility.ValidateInputFormData(Convert.ToString(dataContent["Data"]), "Data", false);
                if (!CommonUtility.GetValidUser(Convert.ToString(dataContent["UserId"])))
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);
                #endregion
            }
            catch (Exception ex)
            {
                return GetFaultResponse(ex.Message);
            }
            GetAppService();
            string pageInfo = string.Empty;
            Request request = new Request();
            request.Payload = new JObject();
            var returnResponse = new MethodReturn<Response>();
            var serviceResponse = new MethodReturn<object>();
            //string columnsData = Convert.ToString(requestBody);
            //var dataContent = JObject.Parse(columnsData.ToString());
            AICoreModels aICoreModels = _aiCoreService.GetAICoreModelPath(dataContent["CorrelationId"].ToString());

            var response = new Response(aICoreModels.ClientId, aICoreModels.DeliveryConstructId, aICoreModels.ServiceId);
            Service service = _aiCoreService.GetAiCoreServiceDetails(aICoreModels.ServiceId);
            //Asset Usage
            auditTrailLog.CorrelationId = aICoreModels.CorrelationId;
            auditTrailLog.ApplicationID = aICoreModels.ApplicationId;
            auditTrailLog.ClientId = aICoreModels.ClientId;
            auditTrailLog.DCID = aICoreModels.DeliveryConstructId;
            auditTrailLog.UseCaseId = aICoreModels.UsecaseId;
            auditTrailLog.CreatedBy = aICoreModels.CreatedBy;
            auditTrailLog.ProcessName = CONSTANTS.PredictionName;
            auditTrailLog.UsageType = CONSTANTS.AssetUsage;
            auditTrailLog.FeatureName = service.ServiceName;

            CommonUtility.AuditTrailLog(auditTrailLog, configSetting);
            switch (service.ServiceCode)
            {
                case "SIMILARITYANALYTICS":
                    pageInfo = "EvaluateSimilarityAanalytics";
                    break;
                case "NEXTWORD":
                    pageInfo = "EvaluateNextWord";
                    break;

            }

            string noOfResults = string.Empty;
            if (dataContent.ContainsKey("noOfResults"))
            {
                CommonUtility.ValidateInputFormData(Convert.ToString(dataContent["noOfResults"]), "noOfResults", false);
                noOfResults = dataContent["noOfResults"].ToString();
                if (Convert.ToInt32(noOfResults) <= 0)
                {
                    return GetFaultResponse("Please provide noOfResults more than 0");
                }
            }
            string encrypteduser = aICoreModels.CreatedBy;
            if (appSettings.isForAllData)
            {
                if (!string.IsNullOrEmpty(Convert.ToString(aICoreModels.CreatedBy)))
                    encrypteduser = _encryptionDecryption.Encrypt(Convert.ToString(aICoreModels.CreatedBy));
            }
            request.Payload["noOfResults"] = noOfResults;
            string baseUrl = appSettings.AICorePythonURL;
            string apiPath = service.ApiUrl;
            request.Payload["UserId"] = encrypteduser;
            request.Payload["PageInfo"] = pageInfo;
            request.Payload["CorrelationId"] = aICoreModels.CorrelationId;
            request.Payload["UniqueId"] = aICoreModels.UniId;
            request.Payload["Params"] = dataContent["Data"];
            response.CorrelationId = aICoreModels.CorrelationId;

            try
            {

                serviceResponse = _aiCoreService.RoutePOSTRequest(string.Empty, new Uri(baseUrl), apiPath,
                    request.Payload, service.IsReturnArray);
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreController), nameof(Evaluate), Convert.ToString(serviceResponse),
                    string.IsNullOrEmpty(aICoreModels.CorrelationId) ? default(Guid) : new Guid(aICoreModels.CorrelationId), aICoreModels.ApplicationId, string.Empty, aICoreModels.ClientId, aICoreModels.DeliveryConstructId);

                response.ResponseData = serviceResponse.ReturnValue;
                returnResponse.Message = serviceResponse.Message;
                returnResponse.IsSuccess = serviceResponse.IsSuccess;
                response.SetResponseDate(DateTime.UtcNow);
                returnResponse.ReturnValue = response;
                return Ok(returnResponse);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AICoreController), nameof(Evaluate), ex.Message + ex.StackTrace, ex,
                   aICoreModels.ApplicationId, string.Empty, aICoreModels.ClientId, aICoreModels.DeliveryConstructId);

                returnResponse.Message = ex.Message;
                returnResponse.IsSuccess = false;
                return GetFaultResponse(returnResponse);
            }
        }

        /// <summary>
        /// To predict/evaluate model based on correlation id
        /// </summary>
        /// <param></param>
        /// <returns>Returns model prediction</returns>
        [HttpPost]
        [Route("api/EvaluateSingleBulkMultipleModel")]
        public IActionResult EvaluateSingleBulkMultipleModel([FromBody] dynamic requestBody)
        {
            GetAppService();
            string pageInfo = string.Empty;
            RequestBulk request = new RequestBulk();
            request.Payload = new JObject();
            var returnResponse = new MethodReturn<Response>();
            var serviceResponse = new MethodReturn<object>();
            string columnsData = Convert.ToString(requestBody);
            var dataContent = JObject.Parse(columnsData.ToString());
            int DecimalPlaces = !string.IsNullOrEmpty(Convert.ToString(dataContent["DecimalPlaces"])) ? Convert.ToInt32(dataContent["DecimalPlaces"]) : 2;
            #region VALIDATIONS
            CommonUtility.ValidateInputFormData(Convert.ToString(dataContent["CorrelationId"]), "CorrelationId", true);
            //CommonUtility.ValidateInputFormData(Convert.ToString(dataContent["Data"]), "Data", false);
            #endregion

            if (dataContent["CorrelationId"] == null || string.IsNullOrEmpty(dataContent["CorrelationId"].ToString()))
            {
                return GetFaultResponse("Please provide CorrelationId");
            }
            AICoreModels aICoreModels = _aiCoreService.GetAICoreModelPath(dataContent["CorrelationId"].ToString());
            if (aICoreModels != null)
            {
                if(aICoreModels.ModelStatus != "Completed")
                    return GetFaultResponse("Model Status is not Completed, Please proceed with another Model.");
                var response = new Response(aICoreModels.ClientId, aICoreModels.DeliveryConstructId, aICoreModels.ServiceId);
                Service service = _aiCoreService.GetAiCoreServiceDetails(aICoreModels.ServiceId);
                //Asset Usage
                auditTrailLog.CorrelationId = aICoreModels.CorrelationId;
                auditTrailLog.ApplicationID = aICoreModels.ApplicationId;
                auditTrailLog.ClientId = aICoreModels.ClientId;
                auditTrailLog.DCID = aICoreModels.DeliveryConstructId;
                auditTrailLog.UseCaseId = aICoreModels.UsecaseId;
                auditTrailLog.CreatedBy = aICoreModels.CreatedBy;
                auditTrailLog.ProcessName = CONSTANTS.PredictionName;
                auditTrailLog.UsageType = CONSTANTS.AssetUsage;
                auditTrailLog.FeatureName = service.ServiceName;

                CommonUtility.AuditTrailLog(auditTrailLog, configSetting);
                switch (service.ServiceCode)
                {
                    case "SIMILARITYANALYTICS":
                        pageInfo = "EvaluateSimilarityAanalytics";
                        break;
                    case "NEXTWORD":
                        pageInfo = "EvaluateNextWord";
                        break;
                }

                string noOfResults = string.Empty;
                if (dataContent.ContainsKey("noOfResults"))
                {
                    //CommonUtility.ValidateInputFormData(Convert.ToString(dataContent["noOfResults"]), "noOfResults", false);
                    noOfResults = dataContent["noOfResults"].ToString();
                    if (Convert.ToInt32(noOfResults) <= 0)
                    {
                        return GetFaultResponse("Please provide noOfResults more than 0");
                    }
                }
                string encrypteduser = aICoreModels.CreatedBy;
                if (appSettings.isForAllData)
                {
                    if (!string.IsNullOrEmpty(Convert.ToString(aICoreModels.CreatedBy)))
                        encrypteduser = _encryptionDecryption.Encrypt(Convert.ToString(aICoreModels.CreatedBy));
                }
                request.Payload["noOfResults"] = noOfResults;
                string baseUrl = appSettings.AICorePythonURL;
                string apiPath = service.ApiUrl;
                request.Payload["UserId"] = encrypteduser;
                request.Payload["PageInfo"] = pageInfo;
                request.Payload["CorrelationId"] = aICoreModels.CorrelationId;
                request.Payload["UniqueId"] = aICoreModels.UniId;
                request.Payload["Params"] = dataContent["Data"];
                if (dataContent.ContainsKey("Bulk"))
                {
                    //CommonUtility.ValidateInputFormData(Convert.ToString(dataContent["Bulk"]), "Bulk", false);
                    request.Payload["Bulk"] = dataContent["Bulk"].ToString();
                }
                response.CorrelationId = aICoreModels.CorrelationId;

                try
                {

                    serviceResponse = _aiCoreService.RoutePOSTRequest(string.Empty, new Uri(baseUrl), apiPath,
                        request.Payload, service.IsReturnArray);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreController), nameof(EvaluateSingleBulkMultipleModel), serviceResponse.ToString(),
                        string.IsNullOrEmpty(aICoreModels.CorrelationId) ? default(Guid) : new Guid(aICoreModels.CorrelationId), aICoreModels.ApplicationId, string.Empty, aICoreModels.ClientId, aICoreModels.DeliveryConstructId);

                    if (serviceResponse.ReturnValue != null)
                    {
                        BsonArray inputD = new BsonArray();
                        dynamic datas = JObject.Parse(Convert.ToString(serviceResponse.ReturnValue));
                        if (datas["Predictions"] != null)
                        {
                            if (datas["Predictions"].GetType().Name == "JArray")
                            {
                                inputD = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonArray>(Convert.ToString(datas["Predictions"]));
                                BsonArray resultD = CommonUtility.GetDataAfterDecimalPrecisionAIService(inputD, DecimalPlaces, 0, true, out bool RequiredFlag);
                                if (RequiredFlag)
                                {
                                    datas["Predictions"] = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(BsonTypeMapper.MapToDotNetValue(resultD)));
                                }
                                if (service.IsReturnArray)
                                    serviceResponse.ReturnValue = JsonConvert.DeserializeObject<List<JObject>>(Convert.ToString(datas));
                                else
                                    serviceResponse.ReturnValue = JsonConvert.DeserializeObject<JObject>(Convert.ToString(datas));
                            }
                        }
                    }
                    response.ResponseData = serviceResponse.ReturnValue;
                    returnResponse.Message = serviceResponse.Message;
                    returnResponse.IsSuccess = serviceResponse.IsSuccess;
                    response.SetResponseDate(DateTime.UtcNow);
                    returnResponse.ReturnValue = response;
                    return Ok(returnResponse);
                }
                catch (Exception ex)
                {
                    LOGGING.LogManager.Logger.LogErrorMessage(typeof(AICoreController), nameof(EvaluateSingleBulkMultipleModel), ex.Message + ex.StackTrace, ex, aICoreModels.ApplicationId, string.Empty, aICoreModels.ClientId, aICoreModels.DeliveryConstructId);
                    returnResponse.Message = ex.Message;
                    returnResponse.IsSuccess = false;
                    return GetFaultResponse(returnResponse);
                }
            }
            else
            {
                return GetFaultResponse("GUID for CorrelationID is invalid");
            }
        }
        /// <summary>
        /// To predict/evaluate usecase based on correlation id
        /// </summary>
        /// <param></param>
        /// <returns>Returns model prediction</returns>
        [HttpPost]
        [Route("api/EvaluateUseCase")]
        public IActionResult EvaluateUseCase([FromBody] dynamic requestBody)
        {
            try
            {
                IsPredictionEnabled(appSettings.EnablePrediction);
            }
            catch (Exception ex)
            {
                return GetFaultResponse(ex.Message);
            }
            string pageInfo = string.Empty;
            Request request = new Request();
            request.Payload = new JObject();
            var returnResponse = new MethodReturn<Response>();


            try
            {
                var serviceResponse = new MethodReturn<object>();
                string columnsData = Convert.ToString(requestBody);
                var dynamicColumns = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(columnsData);
                #region VALIDATIONS
                if (!CommonUtility.GetValidUser(Convert.ToString(dynamicColumns["UserId"])))
                {
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);
                }
                CommonUtility.ValidateInputFormData(Convert.ToString(dynamicColumns["ClientUId"]), "ClientUId", true);
                CommonUtility.ValidateInputFormData(Convert.ToString(dynamicColumns["DeliveryConstructUId"]), "DeliveryConstructUId", true);
                CommonUtility.ValidateInputFormData(Convert.ToString(dynamicColumns["UseCaseId"]), "UseCaseId", true);
                CommonUtility.ValidateInputFormData(Convert.ToString(dynamicColumns["ServiceId"]), "ServiceId", true);
                CommonUtility.ValidateInputFormData(Convert.ToString(dynamicColumns["Data"]), "Data", false);
                #endregion

                AICoreModels aICoreModels = _aiCoreService.GetUseCaseDetails(dynamicColumns["ClientUId"].ToString(), dynamicColumns["DeliveryConstructUId"].ToString(), dynamicColumns["UseCaseId"].ToString(), dynamicColumns["ServiceId"].ToString(), dynamicColumns["UserId"].ToString());
                if (aICoreModels == null)
                    throw new KeyNotFoundException("Model not trained");
                var response = new Response(aICoreModels.ClientId, aICoreModels.DeliveryConstructId, aICoreModels.ServiceId);
                Service service = _aiCoreService.GetAiCoreServiceDetails(aICoreModels.ServiceId);
                switch (service.ServiceCode)
                {
                    case "SIMILARITYANALYTICS":
                        pageInfo = "EvaluateSimilarityAanalytics";
                        break;
                    case "NEXTWORD":
                        pageInfo = "EvaluateNextWord";
                        break;

                }
                string encrypteduser = aICoreModels.CreatedBy;
                if (appSettings.isForAllData)
                {
                    if (!string.IsNullOrEmpty(Convert.ToString(aICoreModels.CreatedBy)))
                        encrypteduser = _encryptionDecryption.Encrypt(Convert.ToString(aICoreModels.CreatedBy));
                }
                string baseUrl = appSettings.AICorePythonURL;
                string apiPath = service.ApiUrl;
                request.Payload["noOfResults"] = "3";
                request.Payload["UserId"] = encrypteduser;
                request.Payload["PageInfo"] = pageInfo;
                request.Payload["CorrelationId"] = aICoreModels.CorrelationId;
                request.Payload["UniqueId"] = aICoreModels.UniId;
                request.Payload["Params"] = dynamicColumns["Data"];
                response.CorrelationId = aICoreModels.CorrelationId;
                serviceResponse = _aiCoreService.RoutePOSTRequest(string.Empty, new Uri(baseUrl), apiPath,
                request.Payload, service.IsReturnArray);

                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreController), nameof(Evaluate), serviceResponse.ToString(),
                    string.IsNullOrEmpty(aICoreModels.CorrelationId) ? default(Guid) : new Guid(aICoreModels.CorrelationId), aICoreModels.ApplicationId, string.Empty, aICoreModels.ClientId, aICoreModels.DeliveryConstructId);

                response.ResponseData = serviceResponse.ReturnValue;
                returnResponse.Message = serviceResponse.Message;
                returnResponse.IsSuccess = serviceResponse.IsSuccess;
                response.SetResponseDate(DateTime.UtcNow);
                returnResponse.ReturnValue = response;
                return Ok(returnResponse);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AICoreController), nameof(Evaluate), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                returnResponse.Message = ex.Message;
                returnResponse.IsSuccess = false;
                return GetFaultResponse(returnResponse);
            }
        }



        /// <summary>
        /// Fetch AI Model Usecases
        /// </summary>
        /// <param></param>
        /// <returns>Returns training response</returns>
        [HttpGet]
        [Route("api/FetchUsecase")]
        public IActionResult FetchUsecase(string serviceId)
        {
            try
            {
                if (string.IsNullOrEmpty(serviceId))
                    throw new Exception("Invalid Input");

                return Ok(_aiCoreService.FetchUseCaseDetails(serviceId));

            }
            catch (Exception ex)
            {
                return GetFaultResponse(ex.Message);
            }
        }

        /// <summary>
        /// Start Developer Prediction training
        /// </summary>
        /// <param></param>
        /// <returns>Returns training response</returns>
        [HttpGet]
        [Route("api/DeveloperPredictionTrain")]
        public IActionResult DeveloperPredictionTrain(string clientId, string deliveryConstructId, string serviceId, string applicationId, string usecaseId, string modelName, string userId, bool isManual, string correlationId, bool retrain)
        {
            try
            {
                if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(deliveryConstructId) || string.IsNullOrEmpty(serviceId) || string.IsNullOrEmpty(applicationId) || string.IsNullOrEmpty(usecaseId) || string.IsNullOrEmpty(modelName) || string.IsNullOrEmpty(userId))
                    throw new Exception("Invalid Input");

                if (!CommonUtility.GetValidUser(userId))
                {
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);
                }
                return Ok(_aiCoreService.DeveloperPredictionTraining(clientId, deliveryConstructId, serviceId, applicationId, usecaseId, modelName, userId, isManual, correlationId, retrain));

            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AICoreController), nameof(DeveloperPredictionTrain), ex.Message + ex.StackTrace, ex, applicationId, string.Empty, clientId, deliveryConstructId);
                return GetFaultResponse(ex.Message);
            }
        }



        /// <summary>
        /// Start Developer Prediction training
        /// </summary>
        /// <param></param>
        /// <returns>Returns training response</returns>
        [HttpPost]
        [Route("api/PredictDeveloper")]
        public IActionResult PredictDeveloper(DeveloperPredictRequest developerPredictRequest)
        {
            try
            {
                #region VALIDATIONS
                if (!CommonUtility.GetValidUser(developerPredictRequest.UserId))
                {
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);
                }
                CommonUtility.ValidateInputFormData(developerPredictRequest.ClientId, "ClientId", true);
                CommonUtility.ValidateInputFormData(developerPredictRequest.DeliveryConstructId, "DeliveryConstructId", true);
                CommonUtility.ValidateInputFormData(developerPredictRequest.ApplicationId, "ApplicationId", true);
                CommonUtility.ValidateInputFormData(developerPredictRequest.ServiceId, "ServiceId", true);
                CommonUtility.ValidateInputFormData(developerPredictRequest.UsecaseId, "UsecaseId", true);
                CommonUtility.ValidateInputFormData(developerPredictRequest.WorkItemExternalId, "WorkItemExternalId", false);
                CommonUtility.ValidateInputFormData(developerPredictRequest.WorkItemType, "WorkItemType", false);
                #endregion

                return Ok(_aiCoreService.DeveloperPredictEvaluate(developerPredictRequest));
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AICoreController), nameof(PredictDeveloper), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
        }
        /// <summary>
        /// Delete AI Service Model based on correlationid
        /// </summary>
        /// <param></param>
        /// <returns>Returns status of deletion</returns>
        [HttpGet]
        [Route("api/DeleteAIModel")]
        public IActionResult DeleteAIModel(string correlationId)
        {
            try
            {
                if (string.IsNullOrEmpty(correlationId))
                {
                    throw new ArgumentNullException(correlationId);
                }
                else
                {
                    return Ok(_aiCoreService.DeleteAIModel(correlationId));
                }

            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AICoreController), nameof(DeleteAIModel), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
        }

        /// <summary>
        /// Delete AI Service Model based on correlationid
        /// </summary>
        /// <param></param>
        /// <returns>Returns status of deletion</returns>
        [HttpGet]
        [Route("api/DeleteAIUsecase")]
        public IActionResult DeleteAIUsecase(string usecaseId)
        {
            try
            {
                if (string.IsNullOrEmpty(usecaseId))
                {
                    throw new ArgumentNullException(usecaseId);
                }
                else
                {
                    return Ok(_aiCoreService.DeleteAIUsecase(usecaseId));
                }

            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AICoreController), nameof(DeleteAIUsecase), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
        }

        /// <summary>
        /// AI Model Training Status
        /// </summary>
        /// <param name="correlationid"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("api/AIModelTrainingStatus")]
        public IActionResult AIModelTrainingStatus(string correlationid)
        {
            try
            {
                return Ok(_aiCoreService.GetAIModelTrainingStatus(correlationid));
            }
            catch (Exception ex)
            {
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
        }

        /// <summary>
        /// GetTextSummary
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("api/GetTextSummary")]
        public IActionResult GetTextSummary()
        {
            try
            {
                var returnResponse = new MethodReturn<Response>();
                var serviceResponse = new MethodReturn<object>();
                AIGetSummaryModel aIServicesPrediction = new AIGetSummaryModel();
                IFormCollection collection = HttpContext.Request.Form;
                string clientId = collection["ClientID"];
                string deliveryConstructId = collection["DeliveryConstructID"];
                string serviceId = collection["ServiceID"];
                string userID = collection["UserID"];
                dynamic data = collection["Payload"];

                #region VALIDATIONS
                CommonUtility.ValidateInputFormData(Convert.ToString(collection["ClientID"]), "ClientID", true);
                CommonUtility.ValidateInputFormData(Convert.ToString(collection["DeliveryConstructID"]), "DeliveryConstructID", true);
                CommonUtility.ValidateInputFormData(Convert.ToString(collection["ServiceID"]), "ServiceID", true);
                //CommonUtility.ValidateInputFormData(Convert.ToString(collection["Payload"]), "Payload", false);

                if (!CommonUtility.GetValidUser(Convert.ToString(collection["UserID"])))
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);
                #endregion
                Service service = _aiCoreService.GetAiCoreServiceDetails(serviceId);
                string baseUrl = appSettings.AICorePythonURL;
                string apiPath = service.ApiUrl;
                JObject payload = new JObject();
                JObject sourceDetails = new JObject();
                var correlationid = Guid.NewGuid().ToString();
                var uniqueId = Guid.NewGuid().ToString();
                if (appSettings.isForAllData)
                {
                    if (!string.IsNullOrEmpty(Convert.ToString(userID)))
                        userID = _encryptionDecryption.Encrypt(Convert.ToString(userID));
                }
                payload["CorrelationId"] = correlationid;
                payload["PageInfo"] = "TextSummary";
                payload["UserId"] = userID;
                payload["UniqueId"] = uniqueId;
                aIServicesPrediction.CorrelationId = correlationid;
                aIServicesPrediction.PageInfo = "TextSummary";
                aIServicesPrediction.UniId = uniqueId;
                aIServicesPrediction.ActualData = data;
                aIServicesPrediction.CreatedBy = userID;
                aIServicesPrediction.ModifiedBy = userID;
                sourceDetails["CID"] = clientId;
                sourceDetails["DUID"] = deliveryConstructId;
                sourceDetails["Entity"] = null;
                sourceDetails["Source"] = null;
                var jsondata = sourceDetails.ToString();
                var response = new Response(clientId, deliveryConstructId, serviceId);
                var sourceDataForm = JsonConvert.DeserializeObject<JObject>(sourceDetails.ToString());
                aIServicesPrediction.SourceDetails = BsonDocument.Parse(sourceDataForm.ToString());//sourceDataForm;
                _aiCoreService.InsertTextSummary(aIServicesPrediction);
                switch (service.ServiceMethod)
                {
                    case "GET":
                        serviceResponse = _aiCoreService.RouteGETRequest(string.Empty, new Uri(baseUrl), apiPath,
                            service.IsReturnArray);
                        break;
                    case "POST":
                        serviceResponse = _aiCoreService.RoutePOSTRequest(string.Empty, new Uri(baseUrl), apiPath,
                            payload, service.IsReturnArray);
                        break;
                }
                response.CorrelationId = correlationid;
                response.ResponseData = serviceResponse.ReturnValue;
                returnResponse.Message = serviceResponse.Message;
                returnResponse.IsSuccess = serviceResponse.IsSuccess;
                response.SetResponseDate(DateTime.UtcNow);
                returnResponse.ReturnValue = response;
                returnResponse.CorrelationId = correlationid;
                return Ok(returnResponse);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AICoreController), nameof(GetTextSummary), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
        }

        /// <summary>
        /// AI Model Training Status
        /// </summary>
        /// <param name="correlationid"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("api/GetTextSummaryStatus")]
        public IActionResult GetTextSummaryStatus(string correlationid)
        {
            try
            {
                var data = _aiCoreService.GetTextSummaryStatus(correlationid, false);

                return GetSuccessResponse(data);

            }
            catch (Exception ex)
            {
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
        }


        [HttpGet]
        [Route("api/GetTextSummaryResult")]
        public IActionResult GetTextSummaryResult(string correlationid)
        {
            try
            {
                var returnResponse = new MethodReturn<Response>();
                var serviceResponse = new MethodReturn<object>();
                var clientId = "";
                var dcId = "";
                var data = _aiCoreService.GetTextSummaryStatus(correlationid, true);
                if (data.Status == "C" || data.Status == "E")
                {
                    if (data.SourceDetails != null)
                    {
                        clientId = data.SourceDetails["CID"].ToString();
                        dcId = data.SourceDetails["DUID"].ToString();

                    }
                    var response = new Response(clientId, dcId, "");
                    response.CorrelationId = correlationid;
                    response.ResponseData = data.PredictedData;
                    returnResponse.Message = data.ErrorMessage;
                    returnResponse.IsSuccess = data.Status == "E" ? false : true;
                    response.SetResponseDate(DateTime.UtcNow);
                    returnResponse.ReturnValue = response;
                    returnResponse.CorrelationId = correlationid;
                }
                return Ok(returnResponse);
            }
            catch (Exception ex)
            {
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
        }

        [HttpPost]
        [Route("api/GetSimilarityPredictions")]
        public IActionResult GetSimilarityPredictions(SimilarityPredictionRequest similarityPredictionRequest)
        {
            try
            {
                if (similarityPredictionRequest.CorrelationId == null || similarityPredictionRequest.UniqueId == null || similarityPredictionRequest.PageNumber.ToString() == null
                    || similarityPredictionRequest.CorrelationId == CONSTANTS.undefined || similarityPredictionRequest.UniqueId == CONSTANTS.undefined || similarityPredictionRequest.PageNumber.ToString() == CONSTANTS.undefined
                    || similarityPredictionRequest.CorrelationId == CONSTANTS.Null || similarityPredictionRequest.UniqueId == CONSTANTS.Null || similarityPredictionRequest.PageNumber.ToString() == CONSTANTS.Null)
                {
                    return Ok(CONSTANTS.IncompleteTrainingRequest);
                }
                #region VALIDATIONS
                CommonUtility.ValidateInputFormData(similarityPredictionRequest.CorrelationId, "CorrelationId", true);
                CommonUtility.ValidateInputFormData(similarityPredictionRequest.UniqueId, "UniqueId", true);
                CommonUtility.ValidateInputFormData(similarityPredictionRequest.Bulk, "Bulk", false);
                #endregion

                return Ok(_aiCoreService.GetSimilarityPredictions(similarityPredictionRequest));
            }
            catch (Exception ex)
            {
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
        }

        [HttpPost]
        [Route("api/GetAIServiceRequestStatus")]
        public IActionResult GetAIServiceRequestStatus(BulkTraining.AIServiceStatusRequest aIServiceStatusRequest)
        {
            try
            {
                if (aIServiceStatusRequest.CorrelationId == null || aIServiceStatusRequest.UniqueId == null || aIServiceStatusRequest.PageInfo == null
                    || aIServiceStatusRequest.CorrelationId == CONSTANTS.undefined || aIServiceStatusRequest.UniqueId == CONSTANTS.undefined || aIServiceStatusRequest.PageInfo == CONSTANTS.undefined
                    || aIServiceStatusRequest.CorrelationId == CONSTANTS.Null || aIServiceStatusRequest.UniqueId == CONSTANTS.Null || aIServiceStatusRequest.PageInfo == CONSTANTS.Null)
                {
                    return Ok(CONSTANTS.InputFieldsAreNull);
                }
                #region VALIDATIONS
                CommonUtility.ValidateInputFormData(aIServiceStatusRequest.CorrelationId, "CorrelationId", true);
                CommonUtility.ValidateInputFormData(aIServiceStatusRequest.UniqueId, "UniqueId", true);
                CommonUtility.ValidateInputFormData(aIServiceStatusRequest.PageInfo, "PageInfo", false);
                #endregion

                return Ok(_aiCoreService.GetAIServiceRequestStatus(aIServiceStatusRequest));
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AICoreController), nameof(GetAIServiceRequestStatus), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
        }

        [HttpGet]
        [Route("api/GetAICustomSourceDetails")]
        public IActionResult GetAICustomSourceDetails(string correlationid, string CustomSourceType)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreController), nameof(GetAICustomSourceDetails), "START", string.IsNullOrEmpty(correlationid) ? default(Guid) : new Guid(correlationid), string.Empty, string.Empty, string.Empty, string.Empty);
            var message = string.Empty;
            try
            {
                if (string.IsNullOrEmpty(correlationid))
                    message = "Correlation Id is Null, Please provide Correlation Id.";
                else if (correlationid == CONSTANTS.undefined)
                    message = CONSTANTS.InutFieldsUndefined;
                else
                {
                    var Result = _customDataService.GetCustomSourceDetails(correlationid, CustomSourceType, CONSTANTS.AICustomDataSource);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreController), nameof(GetAICustomSourceDetails), "END", string.IsNullOrEmpty(correlationid) ? default(Guid) : new Guid(correlationid), string.Empty, string.Empty, string.Empty, string.Empty);
                    return GetSuccessWithMessageResponse(Result);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AICoreController), nameof(GetAICustomSourceDetails), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreController), nameof(GetAICustomSourceDetails), "END", string.IsNullOrEmpty(correlationid) ? default(Guid) : new Guid(correlationid), string.Empty, string.Empty, string.Empty, string.Empty);
            return GetSuccessWithMessageResponse(message);
        }


        /// <summary>
        /// Get Similarity Record Count
        /// </summary>
        /// <param name="correlationid"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("api/GetSimilarityRecordCount")]
        public IActionResult GetSimilarityRecordCount(string correlationid)
        {
            try
            {
                CommonUtility.ValidateInputFormData(correlationid, CONSTANTS.CorrelationId, true);
                var data = _aiCoreService.GetSimilarityRecordCount(correlationid);

                return GetSuccessResponse(data);

            }
            catch (Exception ex)
            {
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
        }
        /// <summary>
        /// To get Multiple and Bulk prediction Status for similarity analytics(SA)
        /// </summary>
        /// <param></param>
        /// <returns>return prediction response status</returns>
        [HttpPost]
        [Route("api/GetSAMultipleBulkPredictionStatus")]
        public IActionResult GetSAMultipleBulkPredictionStatus(SAPredictionStatus SAPredictionStatus)
        {
            try
            {
                #region VALIDATIONS
                if (string.IsNullOrEmpty(SAPredictionStatus.UniqueId))
                    return Ok(string.Format(CONSTANTS.NullInputField, CONSTANTS.UniqueId));
                if (string.IsNullOrEmpty(SAPredictionStatus.CorrelationId))
                    return Ok(string.Format(CONSTANTS.NullInputField, CONSTANTS.CorrelationId));
                else if (SAPredictionStatus.CorrelationId == CONSTANTS.undefined || SAPredictionStatus.UniqueId == CONSTANTS.undefined)
                    return Ok(CONSTANTS.InutFieldsUndefined);

                CommonUtility.ValidateInputFormData(SAPredictionStatus.CorrelationId, "CorrelationId", true);
                CommonUtility.ValidateInputFormData(SAPredictionStatus.UniqueId, "UniqueId", true);
                #endregion

                return Ok(_aiCoreService.GetSAMultipleBulkPredictionStatus(SAPredictionStatus));
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AICoreController), nameof(GetSAMultipleBulkPredictionStatus), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
        }
        #endregion

    }
}