using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
using System.Net.Http;
using Microsoft.Extensions.Options;
using Accenture.MyWizard.Shared.Helpers;
using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Accenture.MyWizard.Ingrain.DataModels.InferenceEngineModel;
using Accenture.MyWizard.Ingrain.DataAccess;
using Accenture.MyWizard.Ingrain.BusinessDomain.Common;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;

namespace Accenture.MyWizard.Ingrain.WebService.Controllers
{
    public class InferenceEngineController : MyWizardControllerBase
    {
        #region Members 
        private readonly IOptions<IngrainAppSettings> appSettings;
        private static IInferenceService inferenceService { set; get; }
        IERequestQueue requestQueue = new IERequestQueue();
        IEPythonCategory pythonCategory = new IEPythonCategory();
        IEPythonInfo pythonInfo = new IEPythonInfo();

        private IInferenceEngineDBContext _inferenceEngineDBContext;
        #endregion
        public InferenceEngineController(IServiceProvider serviceProvider, IOptions<IngrainAppSettings> settings, IInferenceEngineDBContext inferenceEngineDBContext)
        {
            appSettings = settings;
            inferenceService = serviceProvider.GetService<IInferenceService>();

            _inferenceEngineDBContext = inferenceEngineDBContext;
        }

        [HttpPost]
        [Route("api/IEIngestData")]
        public IActionResult IEIngestData(string ModelName, string userId, string clientUID, string deliveryUID, string ParentFileName,
   string MappingFlag, string Source, string UploadFileType, string Category, string Uploadtype, bool DBEncryption, string E2EUID,
   [Optional] string ClusterFlag, [Optional] string EntityStartDate, [Optional] string EntityEndDate, [Optional] string oldCorrelationID,
   string Language, bool IsModelTemplateDataSource, string CorrelationId_status, string usecaseId)
        {

            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InferenceEngineController), nameof(IEIngestData), "IEIngestData Start", string.Empty, string.Empty,clientUID,deliveryUID);
            List<string> ColumnsData = new List<string>();
            var fileUploadColums = new IEFileUploadColums();
            string MappingColumns = string.Empty;
            string requestQueueStatus;
            try
            {
                if (string.IsNullOrEmpty(ModelName)
                    || string.IsNullOrEmpty(clientUID)
                    || string.IsNullOrEmpty(deliveryUID))
                {
                    return GetFaultResponse(Resource.IngrainResx.InputFieldsAreNull);
                }
                if (!CommonUtility.GetValidUser(userId))
                {
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);
                }
                string correlationId = Guid.NewGuid().ToString();
                var fileCollection = HttpContext.Request.Form.Files;
                if (CommonUtility.ValidateFileUploaded(fileCollection))
                {
                    return GetFaultResponse(Resource.IngrainResx.InValidFileName);
                }
                #region VALIDATIONS
                CommonUtility.ValidateInputFormData(clientUID, "clientUID", true);
                CommonUtility.ValidateInputFormData(deliveryUID, "deliveryUID", true);
                CommonUtility.ValidateInputFormData(E2EUID, "E2EUID", true);
                CommonUtility.ValidateInputFormData(oldCorrelationID, "oldCorrelationID", true);
                CommonUtility.ValidateInputFormData(usecaseId, "usecaseId", true);
                IFormCollection collection = HttpContext.Request.Form;
                CommonUtility.ValidateInputFormData(Convert.ToString(collection[CONSTANTS.pad]), CONSTANTS.pad, false);
                CommonUtility.ValidateInputFormData(Convert.ToString(collection[CONSTANTS.metrics]), CONSTANTS.metrics, false);
                CommonUtility.ValidateInputFormData(Convert.ToString(collection[CONSTANTS.InstaMl]), CONSTANTS.InstaMl, false);
                CommonUtility.ValidateInputFormData(Convert.ToString(collection[CONSTANTS.EntitiesName]), CONSTANTS.EntitiesName, false);
                CommonUtility.ValidateInputFormData(Convert.ToString(collection[CONSTANTS.MetricNames]), CONSTANTS.MetricNames, false);
                CommonUtility.ValidateInputFormData(Convert.ToString(collection["Custom"]), "Custom", false);
                CommonUtility.ValidateInputFormData(Convert.ToString(collection["DataSetUId"]), "DataSetUId", true);
                CommonUtility.ValidateInputFormData(Convert.ToString(collection["FileEntityName"]), "FileEntityName", false);
                #endregion

                var type = typeof(DataProcessingController);
                string result = inferenceService.IEUploadFiles(correlationId, ModelName, userId, clientUID, deliveryUID, ParentFileName, MappingFlag, Source, Category, type, E2EUID, HttpContext, ClusterFlag, EntityStartDate, EntityEndDate, DBEncryption, oldCorrelationID, Language, IsModelTemplateDataSource, CorrelationId_status, out requestQueueStatus, usecaseId, UploadFileType);
                switch (result)
                {

                    case CONSTANTS.FileNotExist:
                        return GetSuccessWithMessageResponse(Resource.IngrainResx.FileNotExist);
                    case CONSTANTS.FileEmpty:
                        return GetSuccessWithMessageResponse(Resource.IngrainResx.FileEmpty);
                    case CONSTANTS.Success:
                        string CorrelationID;
                        if (CorrelationId_status == "" || CorrelationId_status == "undefined")
                        {
                            CorrelationID = correlationId;
                        }
                        else
                        {
                            CorrelationID = CorrelationId_status;
                        }

                        if (UploadFileType == CONSTANTS.multiple && requestQueueStatus == "M")
                        {
                            fileUploadColums = inferenceService.IEGetFilesColumns(CorrelationID, ParentFileName, ModelName);
                            if (string.IsNullOrEmpty(fileUploadColums.CorrelationId))
                            {
                                fileUploadColums.CorrelationId = correlationId;
                                fileUploadColums.Flag = "flag5";
                                fileUploadColums.ModelName = ModelName;
                                fileUploadColums.ParentFileName = ParentFileName;

                            }
                        }
                        else
                        {
                            ColumnsData.Add(CorrelationID);
                            ColumnsData.Add(Resource.IngrainResx.UploadFile);
                        }
                        break;
                    case CONSTANTS.InputDataEmpty:
                        return GetSuccessWithMessageResponse(Resource.IngrainResx.InputData);

                    case CONSTANTS.PhythonError:
                        string CorrelationID_E;
                        if (CorrelationId_status == "" || CorrelationId_status == "undefined")
                        {
                            CorrelationID_E = correlationId;
                        }
                        else
                        {
                            CorrelationID_E = CorrelationId_status;
                        }
                        requestQueue = _inferenceEngineDBContext.IERequestQueueRepository.IEGetFileRequestStatus(CorrelationID_E, "IngestData");//inferenceService.IEGetFileRequestStatus(CorrelationID_E, "IngestData");
                        pythonCategory.Category = "Error";
                        pythonCategory.Message = requestQueue.Message;
                        pythonInfo.Category = pythonCategory;
                        pythonInfo.Status = requestQueue.Status;
                        return GetFaultResponse(pythonInfo);

                    case CONSTANTS.PhythonInfo:
                        string CorrelationID_I;
                        if (CorrelationId_status == "" || CorrelationId_status == "undefined")
                        {
                            CorrelationID_I = correlationId;
                        }
                        else
                        {
                            CorrelationID_I = CorrelationId_status;
                        }
                        requestQueue = _inferenceEngineDBContext.IERequestQueueRepository.IEGetFileRequestStatus(CorrelationID_I, "IngestData");//inferenceService.IEGetFileRequestStatus(CorrelationID_I, "IngestData");
                        pythonCategory.Category = "Error";
                        pythonCategory.Message = requestQueue.Message;
                        pythonInfo.Category = pythonCategory;
                        pythonInfo.Status = requestQueue.Status;
                        return GetFaultResponse(pythonInfo);
                    case CONSTANTS.PhythonProgress:
                        if (CorrelationId_status == "" || CorrelationId_status == "undefined")
                        {
                            requestQueue = _inferenceEngineDBContext.IERequestQueueRepository.IEGetFileRequestStatus(correlationId, "IngestData");//inferenceService.IEGetFileRequestStatus(correlationId, "IngestData");
                        }
                        else
                        {
                            requestQueue = _inferenceEngineDBContext.IERequestQueueRepository.IEGetFileRequestStatus(CorrelationId_status, "IngestData");//inferenceService.IEGetFileRequestStatus(CorrelationId_status, "IngestData");
                        }
                        pythonCategory.Category = "Progress";
                        pythonCategory.Message = requestQueue.Message;
                        pythonInfo.Category = pythonCategory;
                        pythonInfo.Status = requestQueue.Status;
                        pythonInfo.correlationId = requestQueue.CorrelationId;
                        return GetSuccessWithMessageResponse(pythonInfo);
                    case CONSTANTS.New:
                        if (CorrelationId_status == "" || CorrelationId_status == "undefined")
                        {
                            requestQueue = _inferenceEngineDBContext.IERequestQueueRepository.IEGetFileRequestStatus(correlationId, "IngestData");//inferenceService.IEGetFileRequestStatus(correlationId, "IngestData");
                        }
                        else
                        {
                            requestQueue = _inferenceEngineDBContext.IERequestQueueRepository.IEGetFileRequestStatus(CorrelationId_status, "IngestData");//inferenceService.IEGetFileRequestStatus(CorrelationId_status, "IngestData");
                        }
                        pythonCategory.Category = "New";
                        pythonCategory.Message = requestQueue.Message;
                        pythonInfo.Category = pythonCategory;
                        pythonInfo.Status = "P";
                        pythonInfo.correlationId = requestQueue.CorrelationId;
                        return GetSuccessWithMessageResponse(pythonInfo);
                    case CONSTANTS.FmUseCaseFail:
                        if (CorrelationId_status == "" || CorrelationId_status == "undefined")
                        {
                            CorrelationID_E = correlationId;
                        }
                        else
                        {
                            CorrelationID_E = CorrelationId_status;
                        }
                        pythonCategory.Category = "Error";
                        pythonCategory.Message = CONSTANTS.FMErrorMessage;
                        pythonInfo.Category = pythonCategory;
                        pythonInfo.Status = CONSTANTS.E;
                        return GetFaultResponse(pythonInfo);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InferenceEngineController), nameof(IEIngestData), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, clientUID, deliveryUID);
                return GetFaultResponse(ex.Message);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InferenceEngineController), nameof(IEIngestData), "End", string.Empty, string.Empty, clientUID, deliveryUID);
            if (UploadFileType == CONSTANTS.multiple && requestQueueStatus == "M")
                return GetSuccessWithMessageResponse(fileUploadColums);
            else
                return GetSuccessWithMessageResponse(ColumnsData);
            //return Ok();
        }



        [HttpPost]
        [Route("api/IEUploadMappingColumns")]
        public IActionResult IEUploadMappingColumns(string userId, string uploadfiletype, string mappingflag, string correlationid,
          string modelname, string clientUID, string deliveryUID, string category, string uploadtype, string statusFlag)
        {
            bool flag = true;
            string PageInfo = "IngestData";
            List<string> UpdateColumnsData = new List<string>();
            IEPythonCategory pythonCategory = new IEPythonCategory();
            IEPythonInfo pythonInfo = new IEPythonInfo();
            try
            {
                if (!CommonUtility.GetValidUser(userId))
                {
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);
                }
                if (mappingflag == "True")
                {
                    #region VALIDATIONS
                    CommonUtility.ValidateInputFormData(clientUID, "clientUID", true);
                    CommonUtility.ValidateInputFormData(deliveryUID, "deliveryUID", true);
                    CommonUtility.ValidateInputFormData(correlationid, "correlationid", true);
                    IFormCollection collection = HttpContext.Request.Form;
                    CommonUtility.ValidateInputFormData(Convert.ToString(collection[CONSTANTS.mappingPayload]), CONSTANTS.mappingPayload, false);
                    CommonUtility.ValidateInputFormData(Convert.ToString(collection[CONSTANTS.EntitiesName]), CONSTANTS.EntitiesName, false);
                    CommonUtility.ValidateInputFormData(Convert.ToString(collection[CONSTANTS.MetricNames]), CONSTANTS.MetricNames, false);
                    #endregion

                    string result = inferenceService.IEGetIngrainRequestCollection(userId, uploadfiletype, mappingflag, correlationid, PageInfo, modelname,
                          clientUID, deliveryUID, HttpContext, category, uploadtype, statusFlag);
                    switch (result)
                    {
                        case CONSTANTS.Success:
                            UpdateColumnsData.Add(correlationid);
                            UpdateColumnsData.Add(Resource.IngrainResx.UploadFile);
                            break;

                        case CONSTANTS.PhythonError:
                            requestQueue = _inferenceEngineDBContext.IERequestQueueRepository.IEGetFileRequestStatus(correlationid, "IngestData");//inferenceService.IEGetFileRequestStatus(correlationid, "IngestData");
                            pythonCategory.Category = "Error";
                            pythonCategory.Message = requestQueue.Message;
                            pythonInfo.Category = pythonCategory;
                            pythonInfo.Status = requestQueue.Status;
                            return GetFaultResponse(pythonInfo);

                        case CONSTANTS.PhythonInfo:
                            requestQueue = _inferenceEngineDBContext.IERequestQueueRepository.IEGetFileRequestStatus(correlationid, "IngestData");//inferenceService.IEGetFileRequestStatus(correlationid, "IngestData");
                            pythonCategory.Category = "Error";
                            pythonCategory.Message = requestQueue.Message;
                            pythonInfo.Category = pythonCategory;
                            pythonInfo.Status = requestQueue.Status;
                            return GetFaultResponse(pythonInfo);

                        case CONSTANTS.PhythonProgress:
                            requestQueue = _inferenceEngineDBContext.IERequestQueueRepository.IEGetFileRequestStatus(correlationid, "IngestData");//inferenceService.IEGetFileRequestStatus(correlationid, "IngestData");
                            pythonCategory.Category = "Progress";
                            pythonCategory.Message = requestQueue.Message;
                            pythonInfo.Category = pythonCategory;
                            pythonInfo.Status = requestQueue.Status;
                            return GetSuccessWithMessageResponse(pythonInfo);

                        case CONSTANTS.New:
                            requestQueue = _inferenceEngineDBContext.IERequestQueueRepository.IEGetFileRequestStatus(correlationid, "IngestData");//inferenceService.IEGetFileRequestStatus(correlationid, "IngestData");
                            pythonCategory.Category = "New";
                            pythonCategory.Message = requestQueue.Message;
                            pythonInfo.Category = pythonCategory;
                            pythonInfo.Status = "P";
                            pythonInfo.correlationId = requestQueue.CorrelationId;
                            return GetSuccessWithMessageResponse(pythonInfo);
                    }

                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InferenceEngineController), nameof(IEUploadMappingColumns), ex.Message, ex,string.Empty, string.Empty,clientUID,deliveryUID);
                return GetFaultResponse(ex.Message);
            }
            return Ok(UpdateColumnsData);
        }

        [HttpGet]
        [Route("api/GetIEModel")]
        public IActionResult GetIEModel(string clientId, string dCId, string userId , string FunctionalArea)
        {
            try
            {
                if (!CommonUtility.GetValidUser(userId))
                {
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);
                }
                var data = inferenceService.GetIEModelData(clientId, dCId, userId, FunctionalArea);
                return Ok(data);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InferenceEngineController), nameof(GetIEModel), ex.Message, ex,string.Empty, string.Empty,clientId,dCId);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }

        }

        [HttpGet]
        [Route("api/GetModelInferences")]
        public IActionResult GetModelInferences(string correlationId, string applicationId, string inferenceConfigId, bool autogenerated)
        {
            try
            {
                var data = inferenceService.GetModelInferences(correlationId, applicationId, inferenceConfigId, autogenerated);
                return Ok(data);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InferenceEngineController), nameof(GetModelInferences), ex.Message, ex, applicationId, string.Empty, string.Empty,string.Empty);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }

        }

        [HttpGet]
        [Route("api/DeleteIEModel")]
        public IActionResult DeleteIEModel(string correlationId)
        {
            try
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(InferenceEngineController), nameof(DeleteIEModel), "Start Delete IE Model", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                var data = inferenceService.DeleteIEModel(correlationId);
                return Ok(data);
            }
            catch (Exception ex)
            {

                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InferenceEngineController), nameof(DeleteIEModel), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }

        }
        [HttpPost]
        [Route("api/SaveIEConfig")]
        public IActionResult SaveIEConfig(IESaveConfigInput iESaveConfigInput)
        {
            try
            {
                if (!CommonUtility.GetValidUser(iESaveConfigInput.UserId))
                {
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);
                }
                CommonUtility.ValidateInputFormData(iESaveConfigInput.CorrelationId, "CorrelationId", true);
                CommonUtility.ValidateInputFormData(iESaveConfigInput.InferenceConfigId, "InferenceConfigId", true);
                CommonUtility.ValidateInputFormData(iESaveConfigInput.ConfigName, "ConfigName", false);
                CommonUtility.ValidateInputFormData(Convert.ToString(iESaveConfigInput.VolumetricConfigInput?.DateColumn), "VolumetricConfigInputDateColumn", false);
                CommonUtility.ValidateInputFormData(Convert.ToString(iESaveConfigInput.VolumetricConfigInput?.TrendForecast), "VolumetricConfigInputTrendForecast", false);
                if (iESaveConfigInput.VolumetricConfigInput?.Frequency != null)
                    iESaveConfigInput.VolumetricConfigInput?.Frequency.ForEach(x => CommonUtility.ValidateInputFormData(x, "iESaveConfigInputVolumetricConfigInputFrequency", false));
                if (iESaveConfigInput.VolumetricConfigInput?.Dimensions != null)
                    iESaveConfigInput.VolumetricConfigInput?.Dimensions.ForEach(x => CommonUtility.ValidateInputFormData(x, "iESaveConfigInputVolumetricConfigInputDimensions", false));
                if (iESaveConfigInput.VolumetricConfigInput?.DeselectedDimensions != null)
                    iESaveConfigInput.VolumetricConfigInput?.DeselectedDimensions.ForEach(x => CommonUtility.ValidateInputFormData(x, "iESaveConfigInputVolumetricConfigInputDeselectedDimensions", false));
                CommonUtility.ValidateInputFormData(Convert.ToString(iESaveConfigInput.MetricConfigInput?.DateColumn), "MetricConfigInputDateColumn", false);
                //CommonUtility.ValidateInputFormData(Convert.ToString(iESaveConfigInput.MetricConfigInput?.FilterValues), "MetricConfigInput.FilterValues)", false);
                CommonUtility.ValidateInputFormData(Convert.ToString(iESaveConfigInput.MetricConfigInput?.MetricColumn), "MetricConfigInput.MetricColumn", false);
                if (iESaveConfigInput.MetricConfigInput?.Features != null)
                    iESaveConfigInput.MetricConfigInput?.Features.ForEach(x => CommonUtility.ValidateInputFormData(x, "iESaveConfigInputMetricConfigInput?Features", false));
                if (iESaveConfigInput.MetricConfigInput?.DeselectedFeatures != null)
                    iESaveConfigInput.MetricConfigInput?.DeselectedFeatures.ForEach(x => CommonUtility.ValidateInputFormData(x, "iESaveConfigInputMetricConfigInput?DeselectedFeatures", false));

                LOGGING.LogManager.Logger.LogProcessInfo(typeof(InferenceEngineController), nameof(SaveIEConfig), "Start SaveIEConfig", string.IsNullOrEmpty(iESaveConfigInput.CorrelationId) ? default(Guid) : new Guid(iESaveConfigInput.CorrelationId), string.Empty, string.Empty, string.Empty, string.Empty);
                var response = inferenceService.SaveInferenceConfiguration(iESaveConfigInput);
                return Ok(response);
            }
            catch (Exception ex)
            {

                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InferenceEngineController), nameof(SaveIEConfig), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }

        }



        [HttpGet]
        [Route("api/DeleteIEConfig")]
        public IActionResult DeleteIEConfig(string inferenceConfigId)
        {
            try
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(InferenceEngineController), nameof(DeleteIEConfig), "Start DeleteIEConfig", string.Empty, string.Empty, string.Empty, string.Empty);
                if (!string.IsNullOrEmpty(inferenceConfigId))
                {
                    var response = inferenceService.DeleteConfig(inferenceConfigId);
                    return Ok(response);
                }
                else
                {
                    return GetFaultResponse("Invalid input");
                }

            }
            catch (Exception ex)
            {

                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InferenceEngineController), nameof(DeleteIEConfig), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }

        }

        [HttpGet]
        [Route("api/GetDateMeasureAttribute")]
        public IActionResult GetDateMeasureAttribute(string correlationId)
        {
            try
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(InferenceEngineController), nameof(GetDateMeasureAttribute), "Start Add Config", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                var data = inferenceService.GetDateMeasureAttribute(correlationId);
                return Ok(data);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InferenceEngineController), nameof(GetDateMeasureAttribute), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
        }

        [HttpGet]
        [Route("api/AutoGenerateInferences")]
        public IActionResult AutoGenerateInferences(string correlationId, bool regenerate)
        {
            try
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(InferenceEngineController), nameof(AutoGenerateInferences), "Start", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                var data = inferenceService.AutoGenerateInferences(correlationId, regenerate);
                return Ok(data);
            }
            catch (Exception ex)
            {

                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InferenceEngineController), nameof(AutoGenerateInferences), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
        }


        [HttpPost]
        [Route("api/TriggerFeatureCombination")]
        public IActionResult TriggerFeatureCombination(string correlationId, string userId, string inferenceConfigId, [FromBody] dynamic requestBody, bool isNewRequest)
        {
            try
            {
                #region VALIDATIONS
                CommonUtility.ValidateInputFormData(inferenceConfigId, "inferenceConfigId", true);
                CommonUtility.ValidateInputFormData(correlationId, "correlationId", true);
                if (!CommonUtility.GetValidUser(userId))
                {
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);
                }
                #endregion
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(InferenceEngineController), nameof(GetDateMeasureAttribute), "Start Add Config", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                string columnsData = Convert.ToString(requestBody);
                var dynamicColumns = Newtonsoft.Json.JsonConvert.DeserializeObject(columnsData);

                var columns = JObject.Parse(columnsData);
                CommonUtility.ValidateInputFormData(Convert.ToString(columns["Metric"]), "Metric", false);
                CommonUtility.ValidateInputFormData(Convert.ToString(columns["date"]), "date", false);                //CommonUtility.ValidateInputFormData(Convert.ToString(columns["FilterValues"]), "FilterValues", false);

                var data = inferenceService.TriggerFeatureCombination(correlationId, userId, inferenceConfigId, dynamicColumns, isNewRequest);
                return Ok(data);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InferenceEngineController), nameof(GetDateMeasureAttribute), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
        }

        [HttpGet]
        [Route("api/GetFeatureCombination")]
        public IActionResult GetFeatureCombination(string correlationId, string requestId)
        {
            try
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(InferenceEngineController), nameof(GetDateMeasureAttribute), "Start Add Config", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                var data = inferenceService.GetFeatureCombinationOnId(correlationId, requestId);
                return Ok(data);
            }
            catch (Exception ex)
            {

                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InferenceEngineController), nameof(GetDateMeasureAttribute), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
        }

        [HttpGet]
        [Route("api/ViewConfiguration")]
        public IActionResult ViewConfiguration(string correlationId, string InferenceConfigId)
        {
            try
            {

                LOGGING.LogManager.Logger.LogProcessInfo(typeof(InferenceEngineController), nameof(ViewConfiguration), "Start View Config", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                var data = inferenceService.ViewConfiguration(correlationId, InferenceConfigId);
                return Ok(data);
            }
            catch (Exception ex)
            {

                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InferenceEngineController), nameof(ViewConfiguration), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
        }

        [HttpPost]
        [Route("api/GenerateInference")]
        public IActionResult GenerateInference(string correlationId, string inferenceConfigId, string userId, bool isNewRequest)
        {
            try
            {

                #region VALIDATIONS
                CommonUtility.ValidateInputFormData(inferenceConfigId, "inferenceConfigId", true);
                CommonUtility.ValidateInputFormData(correlationId, "correlationId", true);
                if (!CommonUtility.GetValidUser(userId))
                {
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);
                }
                #endregion
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(InferenceEngineController), nameof(GenerateInference), "Start Generate Inference", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                var data = inferenceService.GenerateInference(correlationId, inferenceConfigId, userId, isNewRequest);
                return Ok(data);
            }
            catch (Exception ex)
            {

                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InferenceEngineController), nameof(GenerateInference), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
        }

        [HttpGet]
        [Route("api/GetApplications")]
        public IActionResult GetApplications(string environment)
        {
            try
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(InferenceEngineController), nameof(GetApplications), "Start Get All AppDetails", string.Empty, string.Empty, string.Empty, string.Empty);
                var data = inferenceService.GetAllAPPDetails(environment);
                return Ok(data);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InferenceEngineController), nameof(GetApplications), ex.Message + $"   StackTrace = {ex.StackTrace}", ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
        }

        [HttpPost]
        [Route("api/PublishInference")]
        public IActionResult PublishInference([FromBody] dynamic requestBody)
        {
            try
            {

                LOGGING.LogManager.Logger.LogProcessInfo(typeof(InferenceEngineController), nameof(PublishInference), "Start Publish Inference", string.Empty, string.Empty, string.Empty, string.Empty);
                string columnsData = Convert.ToString(requestBody);
                var dynamicColumns = Newtonsoft.Json.JsonConvert.DeserializeObject<List<IEPublishedConfigs>>(columnsData);
                var data = inferenceService.PublishInference(dynamicColumns);
                return Ok(data);
            }
            catch (Exception ex)
            {

                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InferenceEngineController), nameof(PublishInference), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
        }

        [HttpGet]
        [Route("api/GetAPPInferences")]
        public IActionResult GetAPPInferences(string correlationId, string applicationId, string inferenceConfigId, bool rawresponse, bool isGenericAPI)
        {
            try
            {
                #region VALIDATIONS
                CommonUtility.ValidateInputFormData(inferenceConfigId, "inferenceConfigId", true);
                CommonUtility.ValidateInputFormData(correlationId, "correlationId", true);
                CommonUtility.ValidateInputFormData(applicationId, "applicationId", true);
                if (string.IsNullOrEmpty(correlationId))
                    return GetFaultResponse(Resource.IngrainResx.InputsEmpty);
                if (isGenericAPI && string.IsNullOrEmpty(applicationId))
                    return GetFaultResponse(Resource.IngrainResx.InputsEmpty);
                #endregion

                var data = inferenceService.GetInferences(correlationId, applicationId, inferenceConfigId, rawresponse, isGenericAPI);
                if(data.Count <= 0)
                    return GetFaultResponse("Training is not yet completed for the provided input fields, Please try once Training is completed");
                return Ok(data);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InferenceEngineController), nameof(GetPublishedInferences), ex.Message, ex,applicationId, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }

        }

        [HttpGet]
        [Route("api/GetPublishedInferences")]
        public IActionResult GetPublishedInferences(string correlationId, string applicationId, string inferenceConfigId)
        {
            try
            {
                var data = inferenceService.GetPublishedInferences(correlationId, applicationId, inferenceConfigId);
                return Ok(data);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InferenceEngineController), nameof(GetPublishedInferences), ex.Message, ex, applicationId, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }

        }

        [HttpPost]
        [Route("api/AddApplication")]
        public IActionResult AddApplication(IEAppIntegration appIntegrations)
        {
            try
            {
                if (!CommonUtility.GetValidUser(appIntegrations.CreatedByUser))
                {
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);
                }
                if (!CommonUtility.GetValidUser(appIntegrations.ModifiedByUser))
                {
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);
                }
                if (!string.IsNullOrEmpty(appIntegrations.ApplicationName) && !string.IsNullOrEmpty(appIntegrations.BaseURL) && !string.IsNullOrEmpty(appIntegrations.CreatedByUser))
                {
                    CommonUtility.ValidateInputFormData(appIntegrations.ApplicationName, "ApplicationName", false);
                    CommonUtility.ValidateInputFormData(appIntegrations.BaseURL, "BaseURL", false);
                    CommonUtility.ValidateInputFormData(appIntegrations.Environment, "Environment", false);
                    CommonUtility.ValidateInputFormData(appIntegrations.TokenGenerationURL, "TokenGenerationURL", false);
                    CommonUtility.ValidateInputFormData(Convert.ToString(appIntegrations.Credentials), "Credentials", false);
                    CommonUtility.ValidateInputFormData(appIntegrations.ClientUId, "ClientUId", true);
                    CommonUtility.ValidateInputFormData(appIntegrations.deliveryConstructUID, "deliveryConstructUID", true);
                    CommonUtility.ValidateInputFormData(appIntegrations.TrainingDataRangeInMonths, "TrainingDataRangeInMonths", false);
                    CommonUtility.ValidateInputFormData(appIntegrations.Authentication, "Authentication", false);
                    CommonUtility.ValidateInputFormData(appIntegrations.chunkSize, "chunkSize", false);
                    CommonUtility.ValidateInputFormData(appIntegrations.PredictionQueueLimit, "PredictionQueueLimit", false);
                    CommonUtility.ValidateInputFormData(appIntegrations.AppNotificationUrl, "AppNotificationUrl", false);

                    return Ok(inferenceService.AddNewApp(appIntegrations));
                }
                else
                    throw new Exception("Application name and URL are mandatory");

            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InferenceEngineController), nameof(AddApplication), ex.Message + $"   StackTrace = {ex.StackTrace}", ex,appIntegrations.ApplicationID, string.Empty, appIntegrations.ClientUId, appIntegrations.deliveryConstructUID);
                return GetFaultResponse(ex.Message);
            }
        }

        [HttpPost]
        [Route("api/CreateUseCase")]
        public IActionResult CreateUseCase(AddUseCaseInput addUseCaseInput)
        {
            try
            {
                if (!CommonUtility.GetValidUser(addUseCaseInput.UserId))
                {
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);
                }
                CommonUtility.ValidateInputFormData(addUseCaseInput.UseCaseDescription, "UseCaseDescription", false);
                CommonUtility.ValidateInputFormData(addUseCaseInput.UseCaseName, "UseCaseName", false);
                CommonUtility.ValidateInputFormData(addUseCaseInput.ApplicationId, "ApplicationId", true);
                CommonUtility.ValidateInputFormData(addUseCaseInput.CorrelationId, "CorrelationId", true);


                var data = inferenceService.CreateUseCase(addUseCaseInput);
                return Ok(data);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InferenceEngineController), nameof(CreateUseCase), ex.Message + ex.StackTrace, ex, addUseCaseInput.ApplicationId, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }

        }


        [HttpGet]
        [Route("api/DeleteIEUseCase")]
        public IActionResult DeleteIEUseCase(string useCaseId)
        {
            try
            {
                var data = inferenceService.DeleteIEUseCase(useCaseId);
                return Ok(data);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InferenceEngineController), nameof(DeleteIEUseCase), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }

        }


        [HttpGet]
        [Route("api/GetUseCaseList")]
        public IActionResult GetUseCaseList()
        {
            try
            {
                var data = inferenceService.GetAllUseCases();
                return Ok(data);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InferenceEngineController), nameof(GetUseCaseList), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }

        }



        [HttpPost]
        [Route("api/TrainUseCase")]
        public IActionResult TrainUseCase([FromBody] TrainUseCaseInput trainUseCaseInput)
        {
            try
            {
                if (!CommonUtility.GetValidUser(trainUseCaseInput.UserId))
                {
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);
                }
                if (ModelState.IsValid)
                {
                    if (trainUseCaseInput.DataSource == "VDS")
                    {
                        if (appSettings.Value.Environment != CONSTANTS.FDSEnvironment && appSettings.Value.Environment != CONSTANTS.PAMEnvironment)
                            return GetFaultResponse("Invalid Environment, this API with provided input parameter is applicable only in FDS and PAM Environments.");
                        List<string> AMEntities = new List<string>() { "Incidents", "ProblemTickets", "ServiceRequests", "CHANGE_REQUEST" };
                        List<string> IOEntities = new List<string>() { "Incidents", "ProblemTickets", "ServiceRequests", "ChangeRequests" };
                        List<string> FunctionalAreas = new List<string>() { "AM", "IO" };
                        if (Convert.ToString(trainUseCaseInput.DataSourceDetails) != null && Convert.ToString(trainUseCaseInput.DataSourceDetails) != "{}")
                        {
                            if (string.IsNullOrEmpty(Convert.ToString(trainUseCaseInput.DataSourceDetails.RequestType)))
                                return BadRequest("The RequestType field is required.");
                           else if(string.IsNullOrEmpty(Convert.ToString(trainUseCaseInput.DataSourceDetails.ServiceType)))
                                return BadRequest("The ServiceType field is required.");
                            else if (string.IsNullOrEmpty(Convert.ToString(trainUseCaseInput.DataSourceDetails.E2EUID)))
                                return BadRequest("The E2EUID field is required.");
                        }
                        else
                            return BadRequest("The DataSourceDetails field is required.");

                        if (!FunctionalAreas.Contains(Convert.ToString(trainUseCaseInput.DataSourceDetails.RequestType)))
                            return GetFaultResponse("Incorrect Request type, please try again");
                        if (Convert.ToString(trainUseCaseInput.DataSourceDetails.RequestType) == "AM" && !AMEntities.Contains(Convert.ToString(trainUseCaseInput.DataSourceDetails.ServiceType)))
                            return GetFaultResponse("Incorrect Service Type for Functional Area: AM, please try again");
                        if (Convert.ToString(trainUseCaseInput.DataSourceDetails.RequestType) == "IO" && !IOEntities.Contains(Convert.ToString(trainUseCaseInput.DataSourceDetails.ServiceType)))
                            return GetFaultResponse("Incorrect Service Type for Functional Area: IO, please try again");
                        CommonUtility.ValidateInputFormData(Convert.ToString(trainUseCaseInput.DataSourceDetails.E2EUID), CONSTANTS.E2EUID, true);
                    }
                    CommonUtility.ValidateInputFormData(trainUseCaseInput.ClientUId, "ClientUId", true);
                    CommonUtility.ValidateInputFormData(trainUseCaseInput.DeliveryConstructUId, "DeliveryConstructUId", true);
                    CommonUtility.ValidateInputFormData(trainUseCaseInput.ApplicationId, "ApplicationId", true);
                    CommonUtility.ValidateInputFormData(trainUseCaseInput.UseCaseId, "UseCaseId", true);
                    CommonUtility.ValidateInputFormData(trainUseCaseInput.DataSource, "DataSource", false);
                    CommonUtility.ValidateInputFormData(Convert.ToString(trainUseCaseInput.DataSourceDetails), "DataSourceDetails", false);
                    var data = inferenceService.TrainUseCase(trainUseCaseInput);
                    return Ok(data);
                }
                else
                {
                    return BadRequest(ModelState);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InferenceEngineController), nameof(TrainUseCase), ex.Message + ex.StackTrace, ex, trainUseCaseInput.ApplicationId, string.Empty,trainUseCaseInput.ClientUId, trainUseCaseInput.DeliveryConstructUId);
                return GetFaultResponse(ex.Message);
            }

        }

        [HttpGet]
        [Route("api/GetTrainingStatus")]
        public IActionResult GetTrainingStatus(string correlationId)
        {
            try
            {
                var data = inferenceService.GetTrainingStatus(correlationId);
                return Ok(data);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InferenceEngineController), nameof(GetTrainingStatus), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }

        }

        [HttpGet]
        [Route("api/CheckIEModelName")]
        public IActionResult CheckIEModelName(string ModelName, string clientUID, string deliveryUID, string userId)
        {
            try
            {
                if (!CommonUtility.GetValidUser(userId))
                {
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);
                }
                return Ok(_inferenceEngineDBContext.IEModelRepository.GetIEModelName(ModelName, clientUID, deliveryUID, userId, appSettings));
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InferenceEngineController), nameof(GetTrainingStatus), ex.Message, ex, string.Empty, string.Empty, clientUID, deliveryUID);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }

        }

        [HttpPost]
        [Route("api/GetIEIngestedData")]
        public IActionResult GetIEIngestedData(dynamic Args)
        {
            try
            {
                string correlationid = Convert.ToString(Args.correlationid);
                int noOfRecord = Convert.ToInt32(Args.noOfRecord);
                string datecolumn = Convert.ToString(Args.DateColumn);
                CommonUtility.ValidateInputFormData(Args.correlationid, "correlationid", true);
                CommonUtility.ValidateInputFormData(Args.DateColumn, "DateColumn", false);
                return Ok(inferenceService.GetIEIngestedData(correlationid, noOfRecord, datecolumn));
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(GenericInstaModelController), nameof(GetIEIngestedData), ex.Message + $"   StackTrace = {ex.StackTrace}", ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }

        }

        [HttpGet]
        [Route("api/ViewIEData")]
        public IActionResult ViewIEData(string correlationId, int DecimalPlaces)
        {
            try
            {
               
                return Ok(inferenceService.ViewUploadedData(correlationId, DecimalPlaces));
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(GenericInstaModelController), nameof(GetIEIngestedData), ex.Message + $"   StackTrace = {ex.StackTrace}", ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }

        }



    }
}
