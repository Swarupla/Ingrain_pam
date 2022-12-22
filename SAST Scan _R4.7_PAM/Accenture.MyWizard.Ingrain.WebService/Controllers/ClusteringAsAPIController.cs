using System;
using Accenture.MyWizard.Ingrain.DataModels.Models;
using Accenture.MyWizard.Shared.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.DependencyInjection;
using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
using Accenture.MyWizard.Ingrain.DataModels.AICore;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
using MongoDB.Bson;
using Accenture.MyWizard.Ingrain.BusinessDomain.Common;

namespace Accenture.MyWizard.Ingrain.WebService.Controllers
{
    /// <summary>
    /// Clustering as API Controller
    /// </summary>
    public class ClusteringAsAPIController : MyWizardControllerBase
    {
        #region Members
        private IngrainAppSettings appSettings;
        private static IClusteringAPIService _clusteringAPIService { set; get; }
        public static IPhoenixTokenService _iPhoenixTokenService { get; set; }
        //   private static IAICoreService _aICoreService { get; set; }
        #endregion

        #region Constructor
        /// <summary>
        /// ClusteringAsAPIController Constructor
        /// </summary>
        /// <param name="settings">IngrainAppSettings</param>
        /// <param name="serviceProvider">serviceProvider</param>
        public ClusteringAsAPIController(IOptions<IngrainAppSettings> settings, IServiceProvider serviceProvider)
        {
            appSettings = settings.Value;
            _clusteringAPIService = serviceProvider.GetService<IClusteringAPIService>();
            // _aICoreService = serviceProvider.GetService<IAICoreService>();
            _iPhoenixTokenService = serviceProvider.GetService<IPhoenixTokenService>();
        }
        #endregion

        #region public
        /// <summary>
        /// Get all Clustering Model Details
        /// </summary>
        /// <param name="clientid">clientid</param>
        /// <param name="dcid">dcid</param>
        /// <param name="serviceid">serviceid</param>
        /// <param name="userid">userid</param>
        /// <returns>Status of all the clustering models</returns>
        [HttpGet]
        [Route("api/GetCusteringModels")]
        public IActionResult GetCusteringModels(string clientid, string dcid, string serviceid, string userid)
        {
            try
            {
                if (!CommonUtility.GetValidUser(userid))
                {
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);
                }
                return Ok(_clusteringAPIService.GetAllCusteringModels(clientid, dcid, serviceid, userid));
            }
            catch (Exception ex)
            {
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
        }


        /// <summary>
        /// Ingest Clustering Data to initiate Training
        /// </summary>
        /// <returns>Initiate training</returns>
        [HttpPost]
        [Route("api/ClusteringIngestData")]
        public IActionResult ClusteringIngestData()
        {
            JObject payload = new JObject();
            var returnResponse = new MethodReturn<Response>();
            var clusterData = Request.Form;
            Request request = new Request();
            ClusteringAPIModel clusteringAPI = new ClusteringAPIModel();
            string baseUrl = string.Empty;
            string apiPath = string.Empty;
            var serviceResponse = new MethodReturn<object>();
            try
            {
                returnResponse = _clusteringAPIService.ClusteringAsAPI(Request.Form, HttpContext);
            }

            catch (Exception ex)
            {                
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ClusteringAsAPIController), nameof(ClusteringIngestData), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, clusteringAPI.ClientID, clusteringAPI.DCUID);
                returnResponse.Message = ex.Message;
                returnResponse.IsSuccess = false;
                return BadRequest(ex.Message);
            }
            return Ok(returnResponse);
        }


        [HttpPost]
        [Route("api/ClusterServiceIngestData")]
        public IActionResult ClusterServiceIngestData
            (string ModelName, string userId, string clientUID,
            string deliveryUID, string ParentFileName, string MappingFlag,
            string Source, string UploadFileType, string Category,
            string Uploadtype, bool DBEncryption, string ServiceId,
            string Language, string pageInfo, string E2EUID)
        {           
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreController), nameof(ClusterServiceIngestData), "Start", string.Empty, string.Empty, clientUID, deliveryUID);

            try
            {
                #region VALIDATIONS
                if (!CommonUtility.GetValidUser(userId))
                {
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);
                }
                if (string.IsNullOrEmpty(ModelName)
                    || string.IsNullOrEmpty(clientUID)
                    || string.IsNullOrEmpty(deliveryUID))
                {
                    return GetFaultResponse(Resource.IngrainResx.InputFieldsAreNull);
                }
                var fileCollection = HttpContext.Request.Form.Files;
                if (CommonUtility.ValidateFileUploaded(fileCollection))
                {
                    return GetFaultResponse(Resource.IngrainResx.InValidFileName);
                }
                CommonUtility.ValidateInputFormData(clientUID, CONSTANTS.ClientId, true);
                CommonUtility.ValidateInputFormData(deliveryUID, "deliveryUID", true);
                CommonUtility.ValidateInputFormData(ServiceId, CONSTANTS.ServiceID, true);
                CommonUtility.ValidateInputFormData(E2EUID, CONSTANTS.E2EUID, true);
                #endregion

                var result = _clusteringAPIService.AISeriveIngestData(ModelName, userId, clientUID, deliveryUID, ParentFileName, MappingFlag, Source, Category, HttpContext, Uploadtype, DBEncryption, ServiceId, Language, pageInfo, E2EUID);
                return GetSuccessWithMessageResponse(result);
            }
            catch (Exception ex)
            {                
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AICoreController), nameof(ClusterServiceIngestData), ex.Message, ex, string.Empty, string.Empty, clientUID, deliveryUID);
                return GetFaultResponse(ex.Message);
            }
        }

        [HttpGet]
        [Route("api/ClusteringServiceIngestStatus")]
        public IActionResult ClusteringServiceIngestStatus(string correlationId, string pageInfo)
        {            
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AICoreController), nameof(ClusteringServiceIngestStatus), "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId),string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                if (!string.IsNullOrEmpty(correlationId) && correlationId != "undefined")
                {
                    //string columnsData = Convert.ToString(request);
                    //var dataContent = JObject.Parse(columnsData.ToString());
                    //var dynamicColumns = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(columnsData);
                    var result = _clusteringAPIService.ClusteringServiceIngestStatus(correlationId, pageInfo); //modelEngineeringService.RunTest(dynamicColumns, out featurePredictionTest);
                    return GetSuccessWithMessageResponse(result);
                }
                else
                {
                    return GetFaultResponse("Correlation Id is null");
                }
            }
            catch (Exception ex)
            {                
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AICoreController), nameof(ClusteringServiceIngestStatus), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
        }

        /// <summary>
        /// To predict/evaluate model based on correlation id
        /// </summary>
        /// <param></param>
        /// <returns>Returns model prediction</returns>
        [HttpPost]
        [Route("api/ClusteringEvaluate")]
        public IActionResult ClusteringEvaluate([FromBody] dynamic request)
        {
            var returnResponse = new JObject();
            try
            {
                JObject payload = new JObject();
                // var serviceResponse = new MethodReturn<object>();

                string columnsData = Convert.ToString(request);
                var dataContent = JObject.Parse(columnsData.ToString());
                var dynamicColumns = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(columnsData);
                if (!CommonUtility.GetValidUser(Convert.ToString(dynamicColumns["UserId"])))
                {
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);
                }

                if (!CommonUtility.IsValidGuid(Convert.ToString(dynamicColumns["CorrelationId"])))
                {
                    return GetFaultResponse(string.Format(CONSTANTS.InValidGUID, "CorrelationId"));
                }
                if (!CommonUtility.IsValidGuid(Convert.ToString(dynamicColumns["UniId"])))
                {
                    return GetFaultResponse(string.Format(CONSTANTS.InValidGUID, "UniId"));
                }

                //if (!CommonUtility.IsDataValid(Convert.ToString(dynamicColumns["PageInfo"])))
                //{
                //    return GetFaultResponse(string.Format(CONSTANTS.InValidData, "PageInfo"));
                //}
                //if (!CommonUtility.IsDataValid(Convert.ToString(dynamicColumns["Data"])))
                //{
                //    return GetFaultResponse(string.Format(CONSTANTS.InValidData, "Data"));
                //}
                string correlationId = dynamicColumns["CorrelationId"].ToString();
                string userId = dynamicColumns["UserId"].ToString();
                string pageInfo = dynamicColumns["PageInfo"].ToString();
                dynamicColumns["UniId"] = Guid.NewGuid().ToString();
                string eval_UniId = dynamicColumns["UniId"].ToString();

                dynamic PamToken;
                if (appSettings.Environment == CONSTANTS.PAMEnvironment)
                {
                    PamToken = _iPhoenixTokenService.GeneratePAMToken();
                    if (PamToken != null && PamToken["token"] != string.Empty)
                    {
                        dynamicColumns["Token"] =  Convert.ToString(PamToken["token"]);
                    }
                }
                else
                {
                    dynamicColumns["Token"] = _clusteringAPIService.PythonAIServiceToken();
                }

                dynamicColumns["CreatedDate"] = DateTime.Now.ToString();
                Service service = _clusteringAPIService.GetAiCoreServiceDetails("72c38b39-c9fe-4fa6-97f5-6c3adc6ba355");
                ////string baseUrl = new Uri(service.ApiUrl).GetLeftPart(UriPartial.Authority);
                ////string apiPath = new Uri(service.ApiUrl).PathAndQuery;
                string baseUrl = appSettings.ClusteringPythonURL;
                string apiPath = service.ApiUrl;//"clustering/EvalTraining";//service.ApiUrl;
                _clusteringAPIService.Evaluate(dynamicColumns);
                payload["CorrelationId"] = correlationId;
                payload["UniId"] = eval_UniId;

                returnResponse = _clusteringAPIService.EvaluatePythonCall(string.Empty, new Uri(baseUrl), apiPath,
                            payload, correlationId, eval_UniId);

                return Ok(returnResponse);
            }
            catch (Exception ex)
            {                
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ClusteringAsAPIController), nameof(ClusteringEvaluate), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(returnResponse);
            }
        }

        [HttpGet]
        [Route("api/ClusteringViewData")]
        public IActionResult ClusteringViewData(string correlationId, string modelType)
        {
            try
            {
                var response = _clusteringAPIService.ClusteringViewData(correlationId, modelType);
                return Ok(response);
            }

            catch (Exception ex)
            {                
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ClusteringAsAPIController), nameof(ClusteringViewData), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return BadRequest(ex.Message);
            }

        }

        [HttpPost]
        [Route("api/DownloadMappedData")]
        public IActionResult DownloadMappedData([FromBody] dynamic request)
        {
            JObject requestPayload = new JObject();
            string columnsData = Convert.ToString(request);
            var dataContent = JObject.Parse(columnsData.ToString());
            var dynamicColumns = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(columnsData);
            string baseUrl = appSettings.ClusteringPythonURL;
            string apiPath = CONSTANTS.Clustering_DownloadMap;
            requestPayload = Newtonsoft.Json.JsonConvert.DeserializeObject<JObject>(dynamicColumns.ToString());
            //requestPayload["CorrelationId"] = dynamicColumns["CorrelationId"].ToString();
            //requestPayload["pageInfo"] = dynamicColumns["pageInfo"].ToString();
            //requestPayload["UserId"] = dynamicColumns["UserId"].ToString();
            requestPayload["UniId"] = Guid.NewGuid().ToString();

            //requestPayload["MappingData"] = dynamicColumns["MappingData"].ToJson();
            if (!CommonUtility.GetValidUser(Convert.ToString(dynamicColumns["UserId"])))
            {
                return GetFaultResponse(Resource.IngrainResx.InValidUser);
            }

            if (!CommonUtility.IsValidGuid(Convert.ToString(dynamicColumns["CorrelationId"])))
            {
                return GetFaultResponse(string.Format(CONSTANTS.InValidGUID, "CorrelationId"));
            }

            if (!CommonUtility.IsDataValid(Convert.ToString(dynamicColumns["pageInfo"])))
            {
                return GetFaultResponse(string.Format(CONSTANTS.InValidData, "pageInfo"));
            }
            if (!CommonUtility.IsDataValid(Convert.ToString(dynamicColumns["MappingData"])))
            {
                return GetFaultResponse(string.Format(CONSTANTS.InValidData, "MappingData"));
            }
            try
            {
                var data = _clusteringAPIService.DownloadPythonCall(string.Empty, new Uri(baseUrl), apiPath, requestPayload);
                return Ok(data);
            }
            catch (Exception ex)
            {                
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ClusteringAsAPIController), nameof(ClusteringIngestData), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return BadRequest(ex.Message);
            }
        }


        [HttpGet]
        [Route("api/DownloadMappedDataStatus")]
        public IActionResult DownloadMappedDataStatus(string correlationId, string modelType, string pageInfo)
        {
            try
            {
                var response = _clusteringAPIService.DownloadMappedDataStatus(correlationId, modelType, pageInfo);
                return Ok(response);
            }
            catch (Exception ex)
            {                
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ClusteringAsAPIController), nameof(ClusteringViewData), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("api/GenerateWordCloud")]
        public IActionResult GenerateWordCloud([FromBody] WordCloudRequest wordCloudRequest)
        {
            var returnResponse = new MethodReturn<Response>();
            try
            {
                if (!CommonUtility.IsValidGuid(Convert.ToString(wordCloudRequest.CorrelationId)))
                {
                    return GetFaultResponse(string.Format(CONSTANTS.InValidGUID, "CorrelationId"));
                }
                if (wordCloudRequest.SelectedColumns != null)
                    wordCloudRequest.SelectedColumns.ForEach(x => CommonUtility.ValidateInputFormData(x, "SelectedColumns", false));
                if (wordCloudRequest.StopWords != null)
                    wordCloudRequest.StopWords.ForEach(x => CommonUtility.ValidateInputFormData(x, "StopWords", false));
                returnResponse = _clusteringAPIService.GenerateWordCloud(wordCloudRequest);

                return GetSuccessResponse(returnResponse);
            }
            catch (Exception ex)
            {
                returnResponse.Message = ex.Message;
                returnResponse.IsSuccess = false;                
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ClusteringAsAPIController), nameof(ClusteringViewData), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(returnResponse);
            }
        }

        [HttpGet]
        [Route("api/DeleteWordCloud")]
        public IActionResult DeleteWordCloud(string correlationId)
        {
            string response = _clusteringAPIService.DeleteWordCloud(correlationId);
            if (response == "Success")
            {
                return GetSuccessResponse(response);
            }
            else
            {
                return GetFaultResponse(response);
            }


        }

        [HttpPost]
        [Route("api/VisualizationData")]
        public IActionResult VisualizationData([FromBody] dynamic request)
        {            
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ClusteringAsAPIController), nameof(VisualizationData), CONSTANTS.START, string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                string mappeddata = Convert.ToString(request);
                if (!string.IsNullOrEmpty(mappeddata))
                {
                    dynamic data = Newtonsoft.Json.JsonConvert.DeserializeObject(mappeddata);
                    var columns = JObject.Parse(mappeddata);
                    if (Convert.ToString(data.mapping) == CONSTANTS.undefined || Convert.ToString(data.SelectedModel) == CONSTANTS.undefined || Convert.ToString(data.CorrelationId) == CONSTANTS.undefined || Convert.ToString(data.ClientID) == CONSTANTS.undefined || Convert.ToString(data.DCUID) == CONSTANTS.undefined)
                        return GetSuccessWithMessageResponse(Resource.IngrainResx.InputFieldsAreNull);
                    if (Convert.ToString(data.mapping) == CONSTANTS.Null || Convert.ToString(data.SelectedModel) == CONSTANTS.Null || Convert.ToString(data.CorrelationId) == CONSTANTS.undefined || Convert.ToString(data.ClientID) == CONSTANTS.Null || Convert.ToString(data.DCUID) == CONSTANTS.Null)
                        return GetSuccessWithMessageResponse(Resource.IngrainResx.InputEmpty);
                    if (data.mapping != null & data.SelectedModel != null & data.CorrelationId != null & data.ClientID != null & data.DCUID != null)
                    {
                        if (!CommonUtility.IsValidGuid(Convert.ToString(columns[CONSTANTS.ClientID])))
                        {
                            return GetFaultResponse(string.Format(CONSTANTS.InValidGUID, CONSTANTS.ClientID));
                        }
                        if (!CommonUtility.IsValidGuid(Convert.ToString(columns[CONSTANTS.DCUID])))
                        {
                            return GetFaultResponse(string.Format(CONSTANTS.InValidGUID, CONSTANTS.DCUID));
                        }
                        if (!CommonUtility.IsValidGuid(Convert.ToString(columns[CONSTANTS.CorrelationId])))
                        {
                            return GetFaultResponse(string.Format(CONSTANTS.InValidGUID, CONSTANTS.CorrelationId));
                        }

                        if (!CommonUtility.IsDataValid(Convert.ToString(columns["SelectedModel"])))
                        {
                            return GetFaultResponse(string.Format(CONSTANTS.InValidData, "SelectedModel"));
                        }
                        if (!CommonUtility.IsDataValid(Convert.ToString(columns["mapping"])))
                        {
                            return GetFaultResponse(string.Format(CONSTANTS.InValidData, "mapping"));
                        }
                        if (!CommonUtility.IsDataValid(Convert.ToString(columns["mapping"])))
                        {
                            return GetFaultResponse(string.Format(CONSTANTS.InValidData, "mapping"));
                        }

                        string ClientID = columns[CONSTANTS.ClientID].ToString();
                        string DCUID = columns[CONSTANTS.DCUID].ToString();
                        string CorrelationId = columns[CONSTANTS.CorrelationId].ToString();
                        string SelectedModel = columns["SelectedModel"].ToString();
                        JObject mappingUpdate = JObject.Parse(columns["mapping"].ToString());
                        _clusteringAPIService.UpdateMapping(CorrelationId, ClientID, DCUID, mappingUpdate);
                        _clusteringAPIService.Delete_oldVisualization(CorrelationId, ClientID, DCUID, SelectedModel);

                        string baseUrl = appSettings.ClusteringPythonURL;
                        string apiPath = CONSTANTS.Clustering_Visualisation;
                        JObject pyData = new JObject();
                        pyData.Add(CONSTANTS.CorrelationId, CorrelationId);
                        pyData.Add(CONSTANTS.pageInfo, "Clustering_Visualization");
                        pyData.Add(CONSTANTS.UniId, Guid.NewGuid().ToString());
                        pyData.Add("SelectedModel", SelectedModel);
                        var mappingdata = _clusteringAPIService.DownloadPythonCall(string.Empty, new Uri(baseUrl), apiPath, pyData);
                        return Ok(mappingdata);
                    }
                    else
                        return GetSuccessWithMessageResponse(Resource.IngrainResx.InputEmpty);
                }
                else
                    return GetSuccessWithMessageResponse(Resource.IngrainResx.InputFieldsAreNull);
            }
            catch (Exception ex)
            {                
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ClusteringAsAPIController), nameof(VisualizationData), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("api/VisulalisationDataStatus")]
        public IActionResult VisulalisationDataStatus(string correlationId, string modelType, string pageInfo)
        {
            try
            {
                var response = _clusteringAPIService.VisulalisationDataStatus(correlationId, modelType, pageInfo);
                return Ok(response);
            }
            catch (Exception ex)
            {                
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ClusteringAsAPIController), nameof(ClusteringViewData), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return BadRequest(ex.Message);
            }
        }
        [HttpPost]
        [Route("api/UpdateMappingData")]
        public IActionResult UpdateMappingData([FromBody] dynamic request)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ClusteringAsAPIController), nameof(UpdateMappingData), CONSTANTS.START, string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                string mappeddata = Convert.ToString(request);
                if (!string.IsNullOrEmpty(mappeddata))
                {
                    dynamic data = Newtonsoft.Json.JsonConvert.DeserializeObject(mappeddata);
                    var columns = JObject.Parse(mappeddata);
                    if (Convert.ToString(data.mapping) == CONSTANTS.undefined || Convert.ToString(data.CorrelationId) == CONSTANTS.undefined || Convert.ToString(data.ClientID) == CONSTANTS.undefined || Convert.ToString(data.DCUID) == CONSTANTS.undefined)
                        return GetSuccessWithMessageResponse(Resource.IngrainResx.InputFieldsAreNull);
                    if (Convert.ToString(data.mapping) == CONSTANTS.Null || Convert.ToString(data.CorrelationId) == CONSTANTS.undefined || Convert.ToString(data.ClientID) == CONSTANTS.Null || Convert.ToString(data.DCUID) == CONSTANTS.Null)
                        return GetSuccessWithMessageResponse(Resource.IngrainResx.InputEmpty);
                    if (data.mapping != null & data.CorrelationId != null & data.ClientID != null & data.DCUID != null)
                    {
                        if (!CommonUtility.IsValidGuid(Convert.ToString(columns[CONSTANTS.ClientID])))
                        {
                            return GetFaultResponse(string.Format(CONSTANTS.InValidGUID, CONSTANTS.ClientID));
                        }
                        if (!CommonUtility.IsValidGuid(Convert.ToString(columns[CONSTANTS.CorrelationId])))
                        {
                            return GetFaultResponse(string.Format(CONSTANTS.InValidGUID, CONSTANTS.CorrelationId));
                        }
                        if (!CommonUtility.IsValidGuid(Convert.ToString(columns[CONSTANTS.DCUID])))
                        {
                            return GetFaultResponse(string.Format(CONSTANTS.InValidGUID, CONSTANTS.DCUID));
                        }

                        if (!CommonUtility.IsDataValid(Convert.ToString(columns["mapping"])))
                        {
                            return GetFaultResponse(string.Format(CONSTANTS.InValidData, "mapping"));
                        }
                        string ClientID = columns[CONSTANTS.ClientID].ToString();
                        string DCUID = columns[CONSTANTS.DCUID].ToString();
                        string CorrelationId = columns[CONSTANTS.CorrelationId].ToString();
                        JObject mappingUpdate = JObject.Parse(columns["mapping"].ToString());
                        _clusteringAPIService.UpdateMapping(CorrelationId, ClientID, DCUID, mappingUpdate);
                        return Ok();
                    }
                    else
                        return GetSuccessWithMessageResponse(Resource.IngrainResx.InputEmpty);
                }
                else
                    return GetSuccessWithMessageResponse(Resource.IngrainResx.InputFieldsAreNull);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ClusteringAsAPIController), nameof(UpdateMappingData), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Get Clustering Record Count
        /// </summary>
        /// <param name="correlationid"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("api/GetClusteringRecordCount")]
        public IActionResult GetClusteringRecordCount(string correlationid, string UploadType, string DataSetUId)
        {
            try
            {
                if (!string.IsNullOrEmpty(UploadType) && string.IsNullOrEmpty(DataSetUId))
                    return GetSuccessWithMessageResponse("DataSetUId is null");
                CommonUtility.ValidateInputFormData(correlationid, CONSTANTS.CorrelationId, true);
                CommonUtility.ValidateInputFormData(DataSetUId, "DataSetUId", true);
              
                var data = _clusteringAPIService.GetClusteringRecordCount(correlationid, UploadType, DataSetUId);

                return GetSuccessResponse(data);

            }
            catch (Exception ex)
            {
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
        }
        #endregion public

    }
}
