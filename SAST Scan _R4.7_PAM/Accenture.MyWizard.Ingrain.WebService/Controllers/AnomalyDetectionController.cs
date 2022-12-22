using Accenture.MyWizard.Shared.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading;
using MongoDB.Bson;
using Accenture.MyWizard.Ingrain.DataModels.Models;
using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
using Ninject;
using Accenture.MyWizard.Ingrain.BusinessDomain.Common;

namespace Accenture.MyWizard.Ingrain.WebService.Controllers
{
    public class AnomalyDetectionController : MyWizardControllerBase
    {
        #region Members  

        IngrainRequestQueue requestQueue = new IngrainRequestQueue();
        PythonCategory pythonCategory = new PythonCategory();
        PythonInfo pythonInfo = new PythonInfo();
        private BusinessProblemDataDTO _businessProblemData = null;
        string ServiceName = "Anomaly";
        private readonly IOptions<IngrainAppSettings> appSettings;
        private static IAnomalyDetection AnomalyDetectionService { set; get; }
        private static IIngestedData ingestedDataService { set; get; }
        private static IModelEngineering modelEngineeringService { set; get; }
        public static IDeployedModelService _iDeployedModelService { set; get; }
        private IGenericSelfservice _GenericSelfservice { get; set; }
        public static IFlushService _iFlushService { set; get; }
        #endregion

        #region Constructors    
        /// <summary>
        /// Constructor to Initialize the objects
        /// </summary>
        public AnomalyDetectionController(IServiceProvider serviceProvider, IOptions<IngrainAppSettings> settings)
        {
            AnomalyDetectionService = serviceProvider.GetService<IAnomalyDetection>();
            ingestedDataService = serviceProvider.GetService<IIngestedData>();
            modelEngineeringService = serviceProvider.GetService<IModelEngineering>();
            _iDeployedModelService = serviceProvider.GetService<IDeployedModelService>();
            _GenericSelfservice = serviceProvider.GetService<IGenericSelfservice>();
            _iFlushService = serviceProvider.GetService<IFlushService>();
            appSettings = settings;
            ServiceName = "Anomaly";
        }
        #endregion

        #region Methods 
        /// <summary>
        /// Get the existing model name
        /// </summary>        
        /// <param name="modelName">The model name</param>
        /// <returns>Returns true if model name already exist</returns>
        [HttpGet]
        [Route("api/GetExistingModelNameAD")]
        public IActionResult GetExistingModelNameAD(string modelName)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionController), nameof(GetExistingModelNameAD), "START", string.Empty, string.Empty, string.Empty, string.Empty);
            bool ModelStatus = false;
            try
            {
                if (!string.IsNullOrEmpty(modelName) && modelName != "undefined")
                {
                    #region VALIDATIONS
                    if (CommonUtility.IsNameContainSpecialCharacters(modelName))
                        return BadRequest(Resource.IngrainResx.SpecialCharacterNotAllowed);
                    CommonUtility.ValidateInputFormData(modelName, CONSTANTS.modelName, false);
                    #endregion VALIDATIONS
                    ModelStatus = ingestedDataService.IsModelNameExist(modelName, ServiceName);
                }
                else
                {
                    return BadRequest(Resource.IngrainResx.InputsEmpty);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AnomalyDetectionController), nameof(GetExistingModelNameAD), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionController), nameof(GetExistingModelNameAD), "END", string.Empty, string.Empty, string.Empty, string.Empty);
            return GetSuccessWithMessageResponse(ModelStatus);
        }
        [HttpPost]
        [Route("api/FileUploadAD")]
        public IActionResult FileUploadAD(string ModelName, string userId, string clientUID, string deliveryUID, string ParentFileName,
        string MappingFlag, string Source, string UploadFileType, string Category, string Uploadtype, bool DBEncryption, string E2EUID,
        [Optional] string ClusterFlag, [Optional] string EntityStartDate, [Optional] string EntityEndDate, [Optional] string oldCorrelationID,
        string Language, bool IsModelTemplateDataSource, string CorrelationId_status, string usecaseId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionController), nameof(FileUploadAD), "Start", string.Empty, string.Empty, clientUID, deliveryUID);
            List<string> ColumnsData = new List<string>();
            var fileUploadColums = new FileUploadColums();
            string MappingColumns = string.Empty;
            string requestQueueStatus;
            try
            {
                #region VALIDATIONS
                if (string.IsNullOrEmpty(ModelName)
                    || string.IsNullOrEmpty(clientUID)
                    || string.IsNullOrEmpty(deliveryUID))
                {
                    return GetFaultResponse(Resource.IngrainResx.InputFieldsAreNull);
                }
                if (ModelName == CONSTANTS.undefined || clientUID == CONSTANTS.undefined || deliveryUID == CONSTANTS.undefined)
                    return GetFaultResponse(CONSTANTS.InutFieldsUndefined);

                if (CommonUtility.IsNameContainSpecialCharacters(ModelName))
                    return BadRequest(Resource.IngrainResx.SpecialCharacterNotAllowed);

                var fileCollection = HttpContext.Request.Form.Files;
                if (CommonUtility.ValidateFileUploaded(fileCollection))
                    return GetFaultResponse(Resource.IngrainResx.InValidFileName);

                if (!CommonUtility.GetValidUser(userId))
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);

                CommonUtility.ValidateInputFormData(clientUID, CONSTANTS.ClientUID, true);
                CommonUtility.ValidateInputFormData(deliveryUID, CONSTANTS.DeliveryConstructUID, true);
                CommonUtility.ValidateInputFormData(ModelName, CONSTANTS.ModelName, false);
                CommonUtility.ValidateInputFormData(ParentFileName, CONSTANTS.ParentFileName, false);
                CommonUtility.ValidateInputFormData(Source, CONSTANTS.Source, false);
                CommonUtility.ValidateInputFormData(UploadFileType, CONSTANTS.UploadFileType, false);
                CommonUtility.ValidateInputFormData(Category, CONSTANTS.Category, false);
                CommonUtility.ValidateInputFormData(Uploadtype, CONSTANTS.Uploadtype, false);
                CommonUtility.ValidateInputFormData(E2EUID, CONSTANTS.E2EUID, true);
                CommonUtility.ValidateInputFormData(ClusterFlag, CONSTANTS.ClusterFlag, false);
                CommonUtility.ValidateInputFormData(EntityStartDate, CONSTANTS.EntityStartDate, false);
                CommonUtility.ValidateInputFormData(EntityEndDate, CONSTANTS.EntityEndDate, false);
                CommonUtility.ValidateInputFormData(oldCorrelationID, CONSTANTS.oldCorrelationID, false);
                CommonUtility.ValidateInputFormData(Language, CONSTANTS.Language, false);
                CommonUtility.ValidateInputFormData(CorrelationId_status, CONSTANTS.CorrelationId_status, false);
                CommonUtility.ValidateInputFormData(usecaseId, CONSTANTS.UsecaseId, true);
                #endregion

                string correlationId = Guid.NewGuid().ToString();
                var type = typeof(DataProcessingController);
                string result = ingestedDataService.UploadFiles(correlationId, ModelName, userId, clientUID, deliveryUID, ParentFileName, MappingFlag, Source, Category, type, E2EUID, HttpContext, ClusterFlag, EntityStartDate, EntityEndDate, DBEncryption, oldCorrelationID, Language, IsModelTemplateDataSource, CorrelationId_status, out requestQueueStatus, usecaseId, ServiceName);
                switch (result)
                {

                    case CONSTANTS.FileNotExist:
                        return GetSuccessWithMessageResponse(Resource.IngrainResx.FileNotExist);
                    case CONSTANTS.FileEmpty:
                        return GetSuccessWithMessageResponse(Resource.IngrainResx.FileEmpty);
                    case CONSTANTS.Success:
                        string CorrelationID;
                        if (CorrelationId_status == "" || CorrelationId_status == "undefined")
                            CorrelationID = correlationId;
                        else
                            CorrelationID = CorrelationId_status;

                        if (UploadFileType == CONSTANTS.multiple && requestQueueStatus == "M")
                        {
                            fileUploadColums = ingestedDataService.GetFilesColumns(CorrelationID, ParentFileName, ModelName, ServiceName);
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
                            CorrelationID_E = correlationId;
                        else
                            CorrelationID_E = CorrelationId_status;
                        requestQueue = AnomalyDetectionService.GetRequestStatusbyCoridandPageInfo(CorrelationID_E, "IngestData");
                        pythonCategory.Category = "Error";
                        pythonCategory.Message = requestQueue.Message;
                        pythonInfo.Category = pythonCategory;
                        pythonInfo.Status = requestQueue.Status;
                        pythonInfo.correlationId = CorrelationID_E;
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
                        requestQueue = AnomalyDetectionService.GetRequestStatusbyCoridandPageInfo(CorrelationID_I, "IngestData");
                        pythonCategory.Category = "Error";
                        pythonCategory.Message = requestQueue.Message;
                        pythonInfo.Category = pythonCategory;
                        pythonInfo.Status = requestQueue.Status;
                        pythonInfo.correlationId = CorrelationID_I;
                        return GetFaultResponse(pythonInfo);
                    case CONSTANTS.PhythonProgress:
                        if (CorrelationId_status == "" || CorrelationId_status == "undefined")
                        {
                            requestQueue = AnomalyDetectionService.GetRequestStatusbyCoridandPageInfo(correlationId, "IngestData");
                        }
                        else
                        {
                            requestQueue = AnomalyDetectionService.GetRequestStatusbyCoridandPageInfo(CorrelationId_status, "IngestData");
                        }
                        pythonCategory.Category = "Progress";
                        pythonCategory.Message = requestQueue.Message;
                        pythonInfo.Category = pythonCategory;
                        pythonInfo.Status = requestQueue.Status;
                        pythonInfo.correlationId = requestQueue.CorrelationId;
                        return GetSuccessWithMessageResponse(pythonInfo);
                    case CONSTANTS.New:
                        if (CorrelationId_status == "" || CorrelationId_status == "undefined")
                            requestQueue = AnomalyDetectionService.GetRequestStatusbyCoridandPageInfo(correlationId, "IngestData");
                        else
                            requestQueue = AnomalyDetectionService.GetRequestStatusbyCoridandPageInfo(CorrelationId_status, "IngestData");
                        pythonCategory.Category = "New";
                        pythonCategory.Message = requestQueue.Message;
                        pythonInfo.Category = pythonCategory;
                        pythonInfo.Status = "P";
                        pythonInfo.correlationId = requestQueue.CorrelationId;
                        return GetSuccessWithMessageResponse(pythonInfo);
                    case CONSTANTS.FmUseCaseFail:
                        if (CorrelationId_status == "" || CorrelationId_status == "undefined")
                            CorrelationID_E = correlationId;
                        else
                            CorrelationID_E = CorrelationId_status;
                        pythonCategory.Category = "Error";
                        pythonCategory.Message = CONSTANTS.FMErrorMessage;
                        pythonInfo.Category = pythonCategory;
                        pythonInfo.Status = CONSTANTS.E;
                        pythonInfo.correlationId = CorrelationID_E;
                        return GetFaultResponse(pythonInfo);
                    case CONSTANTS.IngrainTokenBlank:
                        if (CorrelationId_status == "" || CorrelationId_status == "undefined")
                            CorrelationID_E = correlationId;
                        else
                            CorrelationID_E = CorrelationId_status;
                        pythonCategory.Category = "Error";
                        pythonCategory.Message = CONSTANTS.IngrainTokenNotFound;
                        pythonInfo.Category = pythonCategory;
                        pythonInfo.Status = CONSTANTS.E;
                        pythonInfo.correlationId = CorrelationID_E;
                        return GetFaultResponse(pythonInfo);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AnomalyDetectionController), nameof(FileUploadAD), ex.Message , ex, string.Empty, string.Empty, clientUID, deliveryUID);
                return GetFaultResponse(ex.Message);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionController), nameof(FileUploadAD), "End", string.Empty, string.Empty, clientUID, deliveryUID);
            if (UploadFileType == CONSTANTS.multiple && requestQueueStatus == "M")
                return GetSuccessWithMessageResponse(fileUploadColums);
            else
                return GetSuccessWithMessageResponse(ColumnsData);
        }
        /// <summary>
        /// Get the AvailableColumns and user Target and Input Columns
        /// </summary>
        /// <returns>All the Columns Including Target or Input Columns</returns>
        [HttpGet]
        [Route("api/GetColumnsAD")]
        public IActionResult GetColumnsAD(string correlationId, bool IsTemplate, bool newModel)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionController), "GetColumns - parameters-" + correlationId + "_" + IsTemplate + "_" + newModel, "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                if (!string.IsNullOrEmpty(correlationId) && correlationId != "undefined")
                {
                    #region VALIDATIONS
                    CommonUtility.ValidateInputFormData(correlationId, CONSTANTS.CorrelationId, true);
                    #endregion VALIDATIONS
                    var columns = ingestedDataService.GetColumns(correlationId, IsTemplate, newModel, ServiceName);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionController), nameof(GetColumnsAD), "END", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                    return GetSuccessWithMessageResponse(columns);
                }
                else
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionController), nameof(GetColumnsAD), "END", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                    return GetSuccessWithMessageResponse(Resource.IngrainResx.CorrelatioUIDEmpty);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AnomalyDetectionController), nameof(GetColumnsAD), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
            
        }
        [Route("api/ViewUploadedDataAD")]
        [HttpGet]
        public IActionResult ViewUploadedDataAD(string correlationId, int DecimalPlaces)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionController), nameof(ViewUploadedDataAD), "Start", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                if (!string.IsNullOrEmpty(correlationId))
                {
                    #region VALIDATIONS
                    CommonUtility.ValidateInputFormData(correlationId, CONSTANTS.CorrelationId, true);
                    #endregion VALIDATIONS
                    var data = ingestedDataService.ViewUploadedData(correlationId, DecimalPlaces, ServiceName);
                    return Ok(data);
                }
                else
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionController), nameof(ViewUploadedDataAD), "END", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                    return GetFaultResponse(Resource.IngrainResx.CorrelatioUIDEmpty);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AnomalyDetectionController), nameof(ViewUploadedDataAD), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(Resource.IngrainResx.InternalServererror + ex.Message);
            }
        }
        /// <summary>
        /// update Target and Input columns
        /// </summary>
        /// <returns>success</returns>
        [HttpPost]
        [Route("api/PostColumnsAD")]
        public IActionResult PostColumnsAD([FromBody] dynamic requestBody)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionController), nameof(PostColumnsAD) + "-Data-" + requestBody, "START", string.Empty, string.Empty, string.Empty, string.Empty);
            UseCaseSave useCase = new UseCaseSave();
            try
            {
                string columnsData = Convert.ToString(requestBody);
                var dynamicColumns = JsonConvert.DeserializeObject(columnsData);
                var columns = JObject.Parse(columnsData);

                #region VALIDATIONS
                if (!CommonUtility.GetValidUser(Convert.ToString(columns[CONSTANTS.CreatedByUser])))
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);

                CommonUtility.ValidateInputFormData(Convert.ToString(columns[CONSTANTS.TargetColumn]), CONSTANTS.TargetColumn, false);
                CommonUtility.ValidateInputFormData(Convert.ToString(columns[CONSTANTS.CorrelationId]), CONSTANTS.CorrelationId, true);
                CommonUtility.ValidateInputFormData(Convert.ToString(columns[CONSTANTS.TargetUniqueIdentifier]), CONSTANTS.TargetUniqueIdentifier, false);
                CommonUtility.ValidateInputFormData(Convert.ToString(columns[CONSTANTS.InputColumns]), CONSTANTS.InputColumns, false);
                CommonUtility.ValidateInputFormData(Convert.ToString(columns[CONSTANTS.AvailableColumns]), CONSTANTS.AvailableColumns, false);
                CommonUtility.ValidateInputFormData(Convert.ToString(columns[CONSTANTS.ProblemType]), CONSTANTS.ProblemType, false);
                CommonUtility.ValidateInputFormData(Convert.ToString(columns[CONSTANTS.ParentCorrelationId]), CONSTANTS.ParentCorrelationId, true);
                CommonUtility.ValidateInputFormData(Convert.ToString(columns[CONSTANTS.IsDataTransformationRetained]), CONSTANTS.IsDataTransformationRetained, false);
                CommonUtility.ValidateInputFormData(Convert.ToString(columns[CONSTANTS.IsCustomColumnSelected]), CONSTANTS.IsCustomColumnSelected, false);
                #endregion

                _businessProblemData = new BusinessProblemDataDTO();
                BindBusinessProblemDataValuesAD(_businessProblemData, columns);
                useCase = AnomalyDetectionService.InsertColumns(_businessProblemData);
                if (!useCase.IsInserted)
                {
                    return GetFaultResponse(useCase.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AnomalyDetectionController), nameof(PostColumnsAD), ex.Message , ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionController), nameof(PostColumnsAD), "END", string.Empty, string.Empty, string.Empty, string.Empty);
            return Ok("Success");
        }
        [NonAction]
        public void BindBusinessProblemDataValuesAD(BusinessProblemDataDTO businessProblemData, JObject columns)
        {
            try
            {
                businessProblemData.ClientUId = Guid.NewGuid().ToString();
                if (string.IsNullOrEmpty(columns["ProblemStatement"].ToString()))
                {
                    var businessProblems = "testBusinessProblem";
                    businessProblemData.BusinessProblems = businessProblems;
                }
                else
                {
                    businessProblemData.BusinessProblems = columns["ProblemStatement"].ToString();
                }
                businessProblemData.TargetColumn = columns["TargetColumn"].ToString();
                var inputColumns = JsonConvert.DeserializeObject(columns["InputColumns"].ToString());
                businessProblemData.InputColumns = inputColumns;
                businessProblemData.AvailableColumns = JsonConvert.DeserializeObject(columns["AvailableColumns"].ToString());
                if (columns.ContainsKey("CorrelationId"))
                {
                    businessProblemData.CorrelationId = columns["CorrelationId"].ToString();
                }
                businessProblemData.TimeSeries = columns["TimeSeries"];
                if (string.IsNullOrEmpty(Convert.ToString(columns["TargetUniqueIdentifier"])))
                {
                    businessProblemData.TargetUniqueIdentifier = null;
                }
                else
                {
                    businessProblemData.TargetUniqueIdentifier = columns["TargetUniqueIdentifier"].ToString();
                }
                businessProblemData.ProblemType = columns["ProblemType"].ToString();
                businessProblemData.CreatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                businessProblemData.CreatedByUser = columns["CreatedByUser"].ToString();
                businessProblemData.ModifiedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                businessProblemData.ModifiedByUser = columns["CreatedByUser"].ToString();
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionController), nameof(BindBusinessProblemDataValuesAD) + "-Data2-", "START", businessProblemData.AppId, string.Empty, businessProblemData.ClientUId, businessProblemData.DeliveryConstructUID);
                if (!string.IsNullOrEmpty(Convert.ToString(columns["ParentCorrelationId"])))
                {
                    businessProblemData.ParentCorrelationId = Convert.ToString(columns["ParentCorrelationId"]);
                }
                if (!string.IsNullOrEmpty(Convert.ToString(columns["IsDataTransformationRetained"])))
                {
                    businessProblemData.IsDataTransformationRetained = Convert.ToBoolean(columns["IsDataTransformationRetained"]);
                }
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionController), nameof(BindBusinessProblemDataValuesAD) + "-Data3-", "START", businessProblemData.AppId, string.Empty, businessProblemData.ClientUId, businessProblemData.DeliveryConstructUID);
                if (!string.IsNullOrEmpty(Convert.ToString(columns["IsCustomColumnSelected"])))
                {
                    businessProblemData.IsCustomColumnSelected = Convert.ToString(columns["IsCustomColumnSelected"]);
                }
                businessProblemData.IsCustomColumnSelected = (!string.IsNullOrEmpty(Convert.ToString(columns["IsCustomColumnSelected"]))) ?
                    Convert.ToString(columns["IsCustomColumnSelected"]) : CONSTANTS.False;
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AnomalyDetectionController), nameof(BindBusinessProblemDataValuesAD), ex.Message , ex, businessProblemData.AppId, string.Empty, businessProblemData.ClientUId, businessProblemData.DeliveryConstructUID);
            }
        }
        /// <summary>
        /// Change Data source functionality
        /// </summary>
        [HttpPost]
        [Route("api/DataSourceFileUploadAD")]
        public IActionResult DataSourceFileUploadAD(string correlationId, string ModelName, string userId, string clientUID, string deliveryUID, string ParentFileName, string MappingFlag, string Source, string UploadFileType, string Category, string Uploadtype,
           bool DBEncryption, string E2EUID, [Optional] string ClusterFlag, [Optional] string EntityStartDate,
           [Optional] string EntityEndDate, [Optional] string oldCorrelationID, string Language, string CorrelationId_status, string usecaseId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionController), nameof(DataSourceFileUploadAD), "Start", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, clientUID, deliveryUID);
            List<string> ColumnsData = new List<string>();
            var fileUploadColums = new FileUploadColums();
            string MappingColumns = string.Empty;
            try
            {
                var fileCollection = HttpContext.Request.Form.Files;

                #region VALIDATIONS
                if (CommonUtility.ValidateFileUploaded(fileCollection))
                    return GetFaultResponse(Resource.IngrainResx.InValidFileName);

                if (!CommonUtility.GetValidUser(userId))
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);

                CommonUtility.ValidateInputFormData(correlationId, CONSTANTS.CorrelationId, true);
                CommonUtility.ValidateInputFormData(ModelName, CONSTANTS.ModelName, false);
                CommonUtility.ValidateInputFormData(clientUID, CONSTANTS.ClientUID, true);
                CommonUtility.ValidateInputFormData(deliveryUID, CONSTANTS.DeliveryConstructUID, true);
                CommonUtility.ValidateInputFormData(ParentFileName, CONSTANTS.ParentFileName, false);
                CommonUtility.ValidateInputFormData(MappingFlag, CONSTANTS.MappingFlag, false);
                CommonUtility.ValidateInputFormData(Source, CONSTANTS.Source, false);
                CommonUtility.ValidateInputFormData(UploadFileType, CONSTANTS.UploadFileType, false);
                CommonUtility.ValidateInputFormData(Category, CONSTANTS.Category, false);
                CommonUtility.ValidateInputFormData(Uploadtype, CONSTANTS.Uploadtype, false);
                CommonUtility.ValidateInputFormData(E2EUID, CONSTANTS.E2EUID, true);
                CommonUtility.ValidateInputFormData(ClusterFlag, CONSTANTS.ClusterFlag, false);
                CommonUtility.ValidateInputFormData(EntityStartDate, CONSTANTS.EntityStartDate, false);
                CommonUtility.ValidateInputFormData(EntityEndDate, CONSTANTS.EntityEndDate, false);
                CommonUtility.ValidateInputFormData(oldCorrelationID, CONSTANTS.oldCorrelationID, true);
                CommonUtility.ValidateInputFormData(Language, CONSTANTS.Language, false);
                CommonUtility.ValidateInputFormData(CorrelationId_status, CONSTANTS.CorrelationId_status, false);
                CommonUtility.ValidateInputFormData(usecaseId, CONSTANTS.UsecaseId, true);
                #endregion

                Type type = typeof(DataProcessingController);
                string result = ingestedDataService.DataSourceUploadFiles(correlationId, ModelName, userId, clientUID, deliveryUID, ParentFileName, MappingFlag, Source, Category, type, E2EUID, HttpContext, ClusterFlag, EntityStartDate, EntityEndDate, DBEncryption, oldCorrelationID, Language, CorrelationId_status, usecaseId, ServiceName);
                switch (result)
                {
                    case CONSTANTS.FileNotExist:
                        return GetSuccessWithMessageResponse(Resource.IngrainResx.FileNotExist);

                    case CONSTANTS.FileEmpty:
                        return GetSuccessWithMessageResponse(Resource.IngrainResx.FileEmpty);

                    case CONSTANTS.Success:
                        string CorrelationID;
                        if (CorrelationId_status == "" || CorrelationId_status == "undefined")
                            CorrelationID = correlationId;
                        else
                            CorrelationID = CorrelationId_status;
                        if (UploadFileType == CONSTANTS.multiple)
                        {
                            fileUploadColums = ingestedDataService.GetFilesColumns(CorrelationID, ParentFileName, ModelName, ServiceName);
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
                            ColumnsData.Add(correlationId);
                            ColumnsData.Add(Resource.IngrainResx.UploadFile);
                        }
                        break;
                    case CONSTANTS.InputDataEmpty:
                        return GetSuccessWithMessageResponse(Resource.IngrainResx.InputData);
                    case CONSTANTS.PhythonError:
                        string CorrelationID_E;
                        if (CorrelationId_status == "" || CorrelationId_status == "undefined")
                            CorrelationID_E = correlationId;
                        else
                            CorrelationID_E = CorrelationId_status;
                        requestQueue = AnomalyDetectionService.GetRequestStatusbyCoridandPageInfo(CorrelationID_E, "IngestData");
                        pythonCategory.Category = "Error";
                        pythonCategory.Message = requestQueue.Message;
                        pythonInfo.Category = pythonCategory;
                        pythonInfo.Status = requestQueue.Status;
                        return GetFaultResponse(pythonInfo);
                    case CONSTANTS.PhythonInfo:
                        string CorrelationID_I;
                        if (CorrelationId_status == "" || CorrelationId_status == "undefined")
                            CorrelationID_I = correlationId;
                        else
                            CorrelationID_I = CorrelationId_status;
                        requestQueue = AnomalyDetectionService.GetRequestStatusbyCoridandPageInfo(correlationId, "IngestData");
                        pythonCategory.Category = "Error";
                        pythonCategory.Message = requestQueue.Message;
                        pythonInfo.Category = pythonCategory;
                        pythonInfo.Status = requestQueue.Status;
                        return GetFaultResponse(pythonInfo);
                    case CONSTANTS.PhythonProgress:
                        if (CorrelationId_status == "" || CorrelationId_status == "undefined")
                        {
                            requestQueue = AnomalyDetectionService.GetRequestStatusbyCoridandPageInfo(correlationId, "IngestData");
                        }
                        else
                        {
                            requestQueue = AnomalyDetectionService.GetRequestStatusbyCoridandPageInfo(CorrelationId_status, "IngestData");
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
                            requestQueue = AnomalyDetectionService.GetRequestStatusbyCoridandPageInfo(correlationId, "IngestData");
                        }
                        else
                        {
                            requestQueue = AnomalyDetectionService.GetRequestStatusbyCoridandPageInfo(CorrelationId_status, "IngestData");
                        }
                        pythonCategory.Category = "New";
                        pythonCategory.Message = requestQueue.Message;
                        pythonInfo.Category = pythonCategory;
                        pythonInfo.Status = "P";
                        pythonInfo.correlationId = requestQueue.CorrelationId;
                        return GetSuccessWithMessageResponse(pythonInfo);
                    case CONSTANTS.FmUseCaseFail:
                        if (CorrelationId_status == "" || CorrelationId_status == "undefined")
                            CorrelationID_E = correlationId;
                        else
                            CorrelationID_E = CorrelationId_status;
                        pythonCategory.Category = "Error";
                        pythonCategory.Message = CONSTANTS.FMErrorMessage;
                        pythonInfo.Category = pythonCategory;
                        pythonInfo.Status = CONSTANTS.E;
                        return GetFaultResponse(pythonInfo);

                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AnomalyDetectionController), nameof(DataSourceFileUploadAD), ex.Message , ex, string.Empty, string.Empty, clientUID, deliveryUID);
                return GetFaultResponse(ex.Message);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionController), nameof(DataSourceFileUploadAD), "End", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, clientUID, deliveryUID);

            if (UploadFileType == CONSTANTS.multiple)
                return GetSuccessWithMessageResponse(fileUploadColums);
            else
                return GetSuccessWithMessageResponse(ColumnsData);

        }
        #region Data Engineering and Data Transformation
        /// <summary>
        /// Check the Queue status for DE and DT and Add entry for DT
        /// </summary>
        [HttpGet]
        [Route("api/GetStatusForDEAndDTProcess")]
        public IActionResult GetStatusForDEAndDTProcess(string correlationId, string pageInfo, string userId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionController), "GetStatusForDEAndDTProcess" + correlationId + pageInfo, CONSTANTS.START, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                if (!string.IsNullOrEmpty(correlationId) && !string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(pageInfo))
                {
                    #region VALIDATIONS
                    if (!CommonUtility.GetValidUser(userId))
                        return GetFaultResponse(Resource.IngrainResx.InValidUser);
                    CommonUtility.ValidateInputFormData(correlationId, CONSTANTS.CorrelationId, true);
                    CommonUtility.ValidateInputFormData(pageInfo, CONSTANTS.pageInfo, false);
                    #endregion
                    var result = AnomalyDetectionService.GetStatusForDEAndDTProcess(correlationId, pageInfo, userId);
                    return Ok(result);
                }
                else
                {
                    return GetFaultResponse(Resource.IngrainResx.InputsEmpty);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AnomalyDetectionController), nameof(GetStatusForDEAndDTProcess), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
        }
        #endregion Data Engineering and Data Transformation
        #region ME
        [HttpGet]
        [Route("api/GetFeatureSelectionAD")]
        public IActionResult GetFeatureSelectionAD(string correlationId, string userId, string pageInfo)
        {
            FeatureEngineeringDTO _featureEngineering = null;
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionController), nameof(GetFeatureSelectionAD), "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                _featureEngineering = new FeatureEngineeringDTO();
                if (!string.IsNullOrEmpty(correlationId))
                {
                    #region VALIDATIONS
                    if (!CommonUtility.GetValidUser(userId))
                        return GetFaultResponse(Resource.IngrainResx.InValidUser);
                    CommonUtility.ValidateInputFormData(correlationId, CONSTANTS.CorrelationId, true);
                    CommonUtility.ValidateInputFormData(pageInfo, CONSTANTS.pageInfo, false);
                    #endregion
                    _featureEngineering = modelEngineeringService.GetFeatureAttributes(correlationId, ServiceName);
                    if (_featureEngineering.ModelType == null)
                        return GetFaultResponse("ProblemType is Empty.");
                    string result= AnomalyDetectionService.InsertRecommendedModelDtls(correlationId, _featureEngineering.ModelType, userId);
                    if(result != "Success")
                        return GetFaultResponse(result);
                    RetraingStatus retrain = new RetraingStatus();
                    retrain = modelEngineeringService.GetRetrain(correlationId, ServiceName);
                    _featureEngineering.Retrain = retrain.Retrain;
                    if (!string.IsNullOrEmpty(retrain.IsIntiateRetrain.ToString()))
                        _featureEngineering.IsIntiateRetrain = retrain.IsIntiateRetrain;
                }
                else
                {
                    return GetFaultResponse(Resource.IngrainResx.CorrelatioUIDEmpty);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AnomalyDetectionController), nameof(GetFeatureSelectionAD), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionController), nameof(GetFeatureSelectionAD), "END", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            return GetSuccessWithMessageResponse(_featureEngineering);
        }
        /// <summary>
        /// save updated feature selection
        /// </summary>
        [HttpPost]
        [Route("api/PostFeatureSelectionAD")]
        public IActionResult PostFeatureSelectionAD([FromBody] dynamic requestBody)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionController), nameof(PostFeatureSelectionAD), "START", string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                string columnsData = Convert.ToString(requestBody);
                if (!string.IsNullOrEmpty(columnsData))
                {
                    var dynamicColumns = Newtonsoft.Json.JsonConvert.DeserializeObject(columnsData);
                    var columns = JObject.Parse(columnsData);
                    string correlationId = columns["CorrelationId"].ToString();

                    #region VALIDATIONS
                    CommonUtility.ValidateInputFormData(correlationId, CONSTANTS.CorrelationId, true);
                    CommonUtility.ValidateInputFormData(Convert.ToString(columns[CONSTANTS.FeatureImportance]), CONSTANTS.FeatureImportance, false);
                    CommonUtility.ValidateInputFormData(Convert.ToString(columns[CONSTANTS.Train_Test_Split]), CONSTANTS.Train_Test_Split, false);
                    CommonUtility.ValidateInputFormData(Convert.ToString(columns[CONSTANTS.KFoldValidation]), CONSTANTS.KFoldValidation, false);
                    #endregion

                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionController), nameof(PostFeatureSelectionAD) + correlationId, "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                    modelEngineeringService.UpdateFeatures(dynamicColumns, correlationId, ServiceName);
                }
                else
                {
                    return GetFaultResponse(Resource.IngrainResx.EmptyReqBody);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AnomalyDetectionController), nameof(PostFeatureSelectionAD), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionController), nameof(PostFeatureSelectionAD), "END", string.Empty, string.Empty, string.Empty, string.Empty);
            return Ok("Success");
        }
        /// <summary>
        /// Get the recommended AI
        /// </summary>
        /// <param name="correlationId">The correlation identifier</param>
        /// <returns>Returns recommended AI data.</returns>
        [HttpGet]
        [Route("api/GetRecommendedAIAD")]
        public IActionResult GetRecommendedAIAD(string correlationId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionController), nameof(GetRecommendedAIAD), "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            RetraingStatus retrain = new RetraingStatus();
            try
            {
                if (string.IsNullOrEmpty(correlationId) || correlationId == "undefined")
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionController), nameof(GetRecommendedAIAD), "trainedData Empty", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                    return Accepted(Resource.IngrainResx.CorrelatioUIDEmpty);
                }
                #region VALIDATIONS
                CommonUtility.ValidateInputFormData(correlationId, CONSTANTS.CorrelationId, true);
                #endregion
                var trainedData = modelEngineeringService.GetRecommendedTrainedModels(correlationId, null, ServiceName);
                retrain = modelEngineeringService.GetRetrain(correlationId, ServiceName);
                //Check retrain models triggered
                if (trainedData.IsInitiateRetrain)
                {
                    var data = modelEngineeringService.GetRecommendedAI(correlationId, ServiceName);
                    if (data.SelectedModels != null)
                    {
                        data.Retrain = retrain.Retrain;
                        if (!string.IsNullOrEmpty(retrain.IsIntiateRetrain.ToString()))
                            data.IsInitiateRetrain = retrain.IsIntiateRetrain;
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionController), nameof(GetRecommendedAIAD), "END", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                        return GetSuccessWithMessageResponse(data);
                    }
                    else
                    {
                        data.Message = Resource.IngrainResx.CorrelatioUIdNotMatch;
                        return GetSuccessWithMessageResponse(data);
                    }
                }
                else
                {
                    //Check whether model already trained
                    if (trainedData.TrainedModel != null && trainedData.TrainedModel.Count > 0)
                    {
                        trainedData.IsModelTrained = true;
                        trainedData.Retrain = retrain.Retrain;
                        if (!string.IsNullOrEmpty(retrain.IsIntiateRetrain.ToString()))
                            trainedData.IsInitiateRetrain = retrain.IsIntiateRetrain;
                        trainedData.SelectedModel = trainedData.TrainedModel[0]["ProblemType"].ToString();
                        return GetSuccessWithMessageResponse(trainedData);
                    }
                    else
                    {
                        var data = modelEngineeringService.GetRecommendedAI(correlationId, ServiceName);
                        if (data.SelectedModels != null)
                        {
                            data.Retrain = retrain.Retrain;
                            if (!string.IsNullOrEmpty(retrain.IsIntiateRetrain.ToString()))
                                data.IsInitiateRetrain = retrain.IsIntiateRetrain;
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionController), nameof(GetRecommendedAIAD), "END", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                            return GetSuccessWithMessageResponse(data);
                        }
                        else
                        {
                            data.Message = Resource.IngrainResx.CorrelatioUIdNotMatch;
                            return GetSuccessWithMessageResponse(data);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AnomalyDetectionController), nameof(GetRecommendedAIAD), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }

        }
        /// <summary>
        /// Initiate Training and Returns recommended AI data.
        /// </summary>
        [HttpGet]
        [Route("api/GetRecommendedTrainedModelsAD")]
        public IActionResult GetRecommendedTrainedModelsAD(string correlationId, string userId, string pageInfo, int noOfModelsSelected)
        {
            RecommedAITrainedModel _recommendedAI = null;
            double _timeDiffInMinutes = 0;
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionController), nameof(GetRecommendedTrainedModelsAD), "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            bool flag = true;
            int count = 0;
            int errorCount = 0;
            bool retrain = false;
            bool modelTrained = false;
            bool isInitiatedRetrain = false;
            int inProgressModels = 0;
            int completedModels = 0;
            var cPUMemoryCount = new CPUMemoryUtilizeCount();
            try
            {
                if (!string.IsNullOrEmpty(correlationId) && !string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(pageInfo))
                {
                    #region VALIDATIONS
                    CommonUtility.ValidateInputFormData(correlationId, CONSTANTS.CorrelationId, true);
                    CommonUtility.ValidateInputFormData(pageInfo, CONSTANTS.pageInfo, false);
                    if (!CommonUtility.GetValidUser(userId))
                        return GetFaultResponse(Resource.IngrainResx.InValidUser);
                    #endregion

                    _recommendedAI = new RecommedAITrainedModel();
                    int executeCount = 0;
                ExecuteQueueTable:
                    IngrainRequestQueue requestQueue = new IngrainRequestQueue();
                    isInitiatedRetrain = modelEngineeringService.IsInitiateRetrain(correlationId, ServiceName);
                    List<IngrainRequestQueue> useCaseDetails = new List<IngrainRequestQueue>();
                    if (!isInitiatedRetrain)
                        useCaseDetails = modelEngineeringService.GetMultipleRequestStatus(correlationId, pageInfo, ServiceName);
                    if (useCaseDetails.Count > 0)
                    {
                        DateTime dateTime = DateTime.Parse(useCaseDetails[0].CreatedOn);
                        DateTime currentTime = DateTime.Parse(DateTime.Now.ToString(CONSTANTS.DateHoursFormat));
                        _timeDiffInMinutes = (currentTime - dateTime).TotalMinutes;
                        RetraingStatus Retrain_status = new RetraingStatus();
                        Retrain_status = modelEngineeringService.GetRetrain(correlationId, ServiceName);
                        retrain = Retrain_status.Retrain;
                        List<int> progressList = new List<int>();
                        for (int i = 0; i < useCaseDetails.Count; i++)
                        {
                            string queueStatus = useCaseDetails[i].Status;
                            if (queueStatus != "C" && queueStatus != "E")
                            {
                                flag = false;
                            }
                            if (queueStatus == "C" || queueStatus == "P")
                            {
                                if (queueStatus == "P")
                                    inProgressModels++;
                                if (queueStatus == "C")
                                    completedModels++;
                                progressList.Add(Convert.ToInt32(useCaseDetails[i].Progress));
                                count++;
                            }
                            if (queueStatus == "E")
                            {
                                errorCount++;
                            }
                        }
                        _recommendedAI.UseCaseList = useCaseDetails;
                        int val = 0;
                        foreach (var item in progressList)
                        {
                            val += item;
                        }
                        if (count > 0)
                        {
                            _recommendedAI.CurrentProgress = (val / (noOfModelsSelected - errorCount));
                        }
                        _recommendedAI.IsModelTrained = false;
                        _recommendedAI.Retrain = retrain;
                        if (!string.IsNullOrEmpty(Retrain_status.IsIntiateRetrain.ToString()))
                            _recommendedAI.IsInitiateRetrain = Retrain_status.IsIntiateRetrain;
                        //System configuration code
                        var result = new SystemUsageDetails();
                        if (!appSettings.Value.Environment.Equals(CONSTANTS.PAMEnvironment))
                        {
                            result = cPUMemoryCount.GetMetrics(appSettings.Value.Environment, appSettings.Value.IsSaaSPlatform);
                            _recommendedAI.CPUUsage = result.CPUUsage;
                            _recommendedAI.MemoryUsageInMB = result.MemoryUsageInMB;
                        }
                        if (flag)
                        {
                            Thread.Sleep(1000);
                            if (noOfModelsSelected != useCaseDetails.Count && executeCount == 0)
                            {
                                executeCount++;
                                errorCount = 0;
                                inProgressModels = 0;
                                completedModels = 0;
                                goto ExecuteQueueTable;
                            }
                            else
                            {
                                _recommendedAI = modelEngineeringService.GetRecommendedTrainedModels(correlationId, null, ServiceName);
                                _recommendedAI.UseCaseList = useCaseDetails;
                                if (count > 0)
                                {
                                    _recommendedAI.CurrentProgress = (val / (noOfModelsSelected - errorCount));
                                }
                                _recommendedAI.IsModelTrained = true;
                                if (!appSettings.Value.Environment.Equals(CONSTANTS.PAMEnvironment))
                                {
                                    _recommendedAI.CPUUsage = result.CPUUsage;
                                    _recommendedAI.MemoryUsageInMB = result.MemoryUsageInMB;
                                    modelEngineeringService.InsertUsage(_recommendedAI.CurrentProgress, _recommendedAI.CPUUsage, correlationId, ServiceName);
                                }
                                _recommendedAI.Retrain = retrain;
                                if (!string.IsNullOrEmpty(Retrain_status.IsIntiateRetrain.ToString()))
                                    _recommendedAI.IsInitiateRetrain = Retrain_status.IsIntiateRetrain;
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionController), nameof(GetRecommendedTrainedModelsAD), "END", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                                return GetSuccessWithMessageResponse(_recommendedAI);
                            }
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(useCaseDetails[0].Status))
                            {
                                for (int i = 0; i < useCaseDetails.Count; i++)
                                {
                                    useCaseDetails[i].Status = "P";
                                    useCaseDetails[i].Progress = "0";
                                    _recommendedAI.CurrentProgress = 1;
                                    break;
                                }
                                _recommendedAI.UseCaseList = useCaseDetails;
                            }
                            //If appsettings "ModelsTrainingTimeLimit" time exceeds the current model training and atleast one model training completed
                            //than we can kill remaining processes and we can show the completed models at UI
                            if (_timeDiffInMinutes > appSettings.Value.ModelsTrainingTimeLimit && completedModels > 0)
                            {
                                _recommendedAI = modelEngineeringService.GetRecommendedTrainedModels(correlationId, null, ServiceName);
                                _recommendedAI.UseCaseList = useCaseDetails;
                                if (count > 0)
                                {
                                    _recommendedAI.CurrentProgress = 100;
                                }
                                _recommendedAI.IsModelTrained = true;
                                if (!appSettings.Value.Environment.Equals(CONSTANTS.PAMEnvironment))
                                {
                                    _recommendedAI.CPUUsage = result.CPUUsage;
                                    _recommendedAI.MemoryUsageInMB = result.MemoryUsageInMB;
                                    modelEngineeringService.InsertUsage(_recommendedAI.CurrentProgress, _recommendedAI.CPUUsage, correlationId, ServiceName);
                                }
                                _recommendedAI.Retrain = retrain;
                                if (!string.IsNullOrEmpty(Retrain_status.IsIntiateRetrain.ToString()))
                                    _recommendedAI.IsInitiateRetrain = Retrain_status.IsIntiateRetrain;

                                //Kill the already existing process of the python
                                //insert the request to terminate the python process for the remaining inprgress models.
                                modelEngineeringService.TerminateModelsTrainingRequests(correlationId, useCaseDetails, ServiceName);
                                //End
                            }
                            //end
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionController), nameof(GetRecommendedTrainedModelsAD), "END", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                            return GetSuccessWithMessageResponse(_recommendedAI);
                        }
                    }
                    else
                    {
                        var recommendedModels = modelEngineeringService.GetModelNames(correlationId, ServiceName);
                        foreach (var modelName in recommendedModels.Item1)
                        {
                            IngrainRequestQueue ingrainRequest = new IngrainRequestQueue
                            {
                                _id = Guid.NewGuid().ToString(),
                                CorrelationId = correlationId,
                                RequestId = Guid.NewGuid().ToString(),
                                ProcessId = null,
                                Status = null,
                                ModelName = modelName,
                                RequestStatus = "New",
                                RetryCount = 0,
                                ProblemType = recommendedModels.Item2,
                                Message = null,
                                UniId = null,
                                Progress = null,
                                pageInfo = pageInfo,
                                ParamArgs = "{}",
                                Function = pageInfo,
                                CreatedByUser = userId,
                                CreatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                                ModifiedByUser = userId,
                                ModifiedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                                LastProcessedOn = null,
                            };
                            modelTrained = modelEngineeringService.IsModelsTrained(correlationId, ServiceName);
                            if (isInitiatedRetrain && modelTrained)
                            {
                                ingrainRequest.Function = CONSTANTS.RetrainRecommendedAI;
                            }
                            ingestedDataService.InsertRequests(ingrainRequest, ServiceName);
                        }
                        //Change the IsInitiateRetrain flag to false.
                        modelEngineeringService.UpdateIsRetrainFlag(correlationId, ServiceName);
                        Thread.Sleep(2000);
                        PythonResult pythonResult2 = new PythonResult();
                        pythonResult2.message = "Success..";
                        pythonResult2.status = "True";
                        string pythonResult = pythonResult2.ToJson();
                        return GetSuccessWithMessageResponse(pythonResult);
                    }
                }
                else
                {
                    return Accepted(Resource.IngrainResx.InputsEmpty);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AnomalyDetectionController), nameof(GetRecommendedTrainedModelsAD), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
        }
        #endregion ME
        #region Deploy Model
        /// <summary>
        /// Get all Available Published Models for selection
        /// </summary>
        [HttpGet]
        [Route("api/GetPublishModelAD")]
        public IActionResult GetPublishModelAD(string correlationId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionController), nameof(GetPublishModelAD), "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            RecommedAITrainedModel _recommendedAIViewModel = null;
            try
            {
                _recommendedAIViewModel = new RecommedAITrainedModel();
                if (!string.IsNullOrEmpty(correlationId))
                {
                    #region VALIDATIONS
                    CommonUtility.ValidateInputFormData(correlationId, CONSTANTS.CorrelationId, true);
                    #endregion
                    _recommendedAIViewModel = _iDeployedModelService.GetPublishedModels(correlationId, ServiceName);
                }
                else
                {
                    return GetFaultResponse(Resource.IngrainResx.CorrelatioUIDEmpty);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AnomalyDetectionController), nameof(GetPublishModelAD), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message );
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionController), nameof(GetPublishModelAD), "END", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            return GetSuccessResponse(_recommendedAIViewModel);
        }

        /// <summary>
        /// Gets All the Application Details
        /// </summary>
        [HttpGet]
        [Route("api/GetAllAppDetailsAD")]
        public IActionResult GetAllAppDetailsAD(string clientUId, string deliveryConstructUID, string CorrelationId, string Environment)
        {
            try
            {
                if (!(string.IsNullOrEmpty(clientUId)) && !(string.IsNullOrEmpty(deliveryConstructUID)))
                {
                    #region VALIDATIONS
                    CommonUtility.ValidateInputFormData(CorrelationId, CONSTANTS.CorrelationId, true);
                    CommonUtility.ValidateInputFormData(clientUId, CONSTANTS.ClientUId, true);
                    CommonUtility.ValidateInputFormData(deliveryConstructUID, CONSTANTS.DeliveryConstructUId, true);
                    CommonUtility.ValidateInputFormData(Environment, "Environment", false);
                    #endregion
                    return Ok(_GenericSelfservice.AllAppDetails(clientUId, deliveryConstructUID, CorrelationId, Environment, ServiceName));
                }
                else
                {
                    return Ok("Client Id & DCID Values are Null. Please try again");
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AnomalyDetectionController), nameof(GetAllAppDetailsAD), ex.Message , ex, string.Empty, string.Empty, clientUId, deliveryConstructUID);
                return GetFaultResponse(ex.Message);
            }
        }

        /// <summary>
        /// To Add New Application details to Collection
        /// </summary>
        /// <returns>Application details</returns>
        [HttpPost]
        [Route("api/AddNewAppAD")]
        public IActionResult AddNewAppAD(AppIntegration appIntegrations)
        {
            try
            {
                #region VALIDATIONS
                if (!CommonUtility.GetValidUser(appIntegrations.CreatedByUser))
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);
                else if (!CommonUtility.GetValidUser(appIntegrations.ModifiedByUser))
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);
                CommonUtility.ValidateInputFormData(appIntegrations.ApplicationName, "ApplicationName", false);
                CommonUtility.ValidateInputFormData(appIntegrations.BaseURL, "BaseURL", false);
                CommonUtility.ValidateInputFormData(appIntegrations.Environment, "Environment", false);
                CommonUtility.ValidateInputFormData(appIntegrations.clientUId, "clientUId", true);
                CommonUtility.ValidateInputFormData(appIntegrations.TokenGenerationURL, "TokenGenerationURL", false);
                CommonUtility.ValidateInputFormData(Convert.ToString(appIntegrations.Credentials), "Credentials", false);
                CommonUtility.ValidateInputFormData(appIntegrations.deliveryConstructUID, "deliveryConstructUID", true);
                CommonUtility.ValidateInputFormData(appIntegrations.Authentication, "Authentication", false);
                CommonUtility.ValidateInputFormData(appIntegrations.chunkSize, "chunkSize", false);
                CommonUtility.ValidateInputFormData(appIntegrations.PredictionQueueLimit, "PredictionQueueLimit", false);
                CommonUtility.ValidateInputFormData(appIntegrations.AppNotificationUrl, "AppNotificationUrl", false);
                #endregion

                if (!string.IsNullOrEmpty(appIntegrations.ApplicationName) && !string.IsNullOrEmpty(appIntegrations.BaseURL) && !string.IsNullOrEmpty(appIntegrations.CreatedByUser))
                    return Ok(_GenericSelfservice.AddNewApp(appIntegrations, ServiceName));
                else
                    throw new Exception("Application name and URL are mandatory");

            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AnomalyDetectionController), nameof(AddNewAppAD), ex.Message , ex,
                    appIntegrations.ApplicationID, string.Empty, appIntegrations.clientUId, appIntegrations.deliveryConstructUID);
                return GetFaultResponse(ex.Message);
            }
        }
        /// <summary>
        /// Deploy selected Model
        /// </summary>
        [HttpPost]
        [Route("api/PostDeployModelAD")]
        public IActionResult PostDeployModelAD([FromBody] dynamic requestBody)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionController), nameof(PostDeployModelAD), "START", string.Empty, string.Empty, string.Empty, string.Empty);
            DeployModelViewModel modelsDto = new DeployModelViewModel();
            try
            {
                string columnsData = Convert.ToString(requestBody);
                dynamic dynamicData = Newtonsoft.Json.JsonConvert.DeserializeObject(columnsData);
                JObject inputData = JObject.Parse(columnsData);
                if (!string.IsNullOrEmpty(inputData["ModelType"].ToString()) && !string.IsNullOrEmpty(inputData["ModelVersion"].ToString()) && !string.IsNullOrEmpty(inputData["userid"].ToString()))
                {
                    #region VALIDATIONS
                    if (!CommonUtility.GetValidUser(Convert.ToString(inputData["userid"])))
                        return GetFaultResponse(Resource.IngrainResx.InValidUser);
                    CommonUtility.ValidateInputFormData(Convert.ToString(dynamicData.LinkedApps), "LinkedApps", false);
                    CommonUtility.ValidateInputFormData(Convert.ToString(dynamicData.correlationId), "correlationId", true);
                    CommonUtility.ValidateInputFormData(Convert.ToString(dynamicData.AppId), "AppId", true);
                    CommonUtility.ValidateInputFormData(Convert.ToString(inputData["ModelName"]), "ModelName", false);
                    CommonUtility.ValidateInputFormData(Convert.ToString(inputData["ModelVersion"]), "ModelVersion", false);
                    CommonUtility.ValidateInputFormData(Convert.ToString(inputData["Category"]), "Category", false);
                    CommonUtility.ValidateInputFormData(Convert.ToString(inputData["ModelType"]), "ModelType", false);
                    #endregion
                    if (Convert.ToBoolean(dynamicData.IsModelTemplate))//MODEL TEMPLATE
                    {
                        if (inputData["Category"] != null)
                        {
                            modelsDto = _iDeployedModelService.DeployModel(dynamicData, ServiceName);
                        }
                        else
                        {
                            return GetSuccessResponse(Resource.IngrainResx.InputData);
                        }
                    }
                    else//PUBLIC MODEL  //PRIVATE MODEL
                    {
                        modelsDto = _iDeployedModelService.DeployModel(dynamicData, ServiceName);
                    }
                }
                else
                {
                    return GetSuccessResponse(Resource.IngrainResx.EmptyReqBody);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AnomalyDetectionController), nameof(PostDeployModelAD), ex.Message , ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionController), nameof(PostDeployModelAD), "END", string.Empty, string.Empty, string.Empty, string.Empty);
            return GetSuccessResponse(modelsDto);
        }
        /// <summary>
        /// Get Deployed Model
        /// </summary>
        /// <param name="correlationId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("api/GetDeployedModelAD")]
        public IActionResult GetDeployedModelAD(string correlationId)
        {
            DeployModelViewModel deployModel = new DeployModelViewModel();
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionController), nameof(GetDeployedModelAD), "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                #region VALIDATIONS
                CommonUtility.ValidateInputFormData(correlationId, CONSTANTS.CorrelationId, true);
                #endregion
                deployModel = _iDeployedModelService.GetDeployModel(correlationId, ServiceName);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AnomalyDetectionController), nameof(GetDeployedModelAD), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionController), nameof(GetDeployedModelAD), "END", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            return GetSuccessResponse(deployModel);
        }
        #endregion Depoly Model
        #region My Model Section
        /// <summary>
        /// Get all the Models based on the input parameters
        /// </summary>
        [HttpGet]
        [Route("api/GetTemplateModelsAD")]
        public IActionResult GetTemplateModelsAD(bool Templates, string userId, string category, string dateFilter, string DeliveryConstructUID, string ClientUId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionController), nameof(GetTemplateModelsAD), "Start", string.Empty, string.Empty, ClientUId, DeliveryConstructUID);
            List<PublishDeployedModel> _publishModelDTOs = null;
            PublicTemplateModel publicTemplateModel = new PublicTemplateModel();
            try
            {
                if (Templates)
                {
                    if (string.IsNullOrEmpty(category))
                        category = "It Services";
                    publicTemplateModel = ingestedDataService.GetPublicTemplates(category, ServiceName);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionController), nameof(GetTemplateModelsAD), "For template END", string.Empty, string.Empty, ClientUId, DeliveryConstructUID);
                    return GetSuccessWithMessageResponse(publicTemplateModel);
                }
                else
                {
                    if (!string.IsNullOrEmpty(userId))
                    {
                        #region VALIDATIONS
                        if (!CommonUtility.GetValidUser(userId))
                            return GetFaultResponse(Resource.IngrainResx.InValidUser);
                        CommonUtility.ValidateInputFormData(DeliveryConstructUID, CONSTANTS.DeliveryConstructUID, true);
                        CommonUtility.ValidateInputFormData(ClientUId, CONSTANTS.ClientUId, true);
                        #endregion
                        _publishModelDTOs = ingestedDataService.GetPublishModels(userId, dateFilter, DeliveryConstructUID, ClientUId, ServiceName);
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionController), nameof(GetTemplateModelsAD), "END", string.Empty, string.Empty, ClientUId, DeliveryConstructUID);
                        return GetSuccessWithMessageResponse(_publishModelDTOs);
                    }
                    else
                    {
                        return GetSuccessWithMessageResponse(Resource.IngrainResx.InputsEmpty);
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AnomalyDetectionController), nameof(GetTemplateModelsAD), ex.Message, ex, string.Empty, string.Empty, ClientUId, DeliveryConstructUID);
                return GetFaultResponse(ex.Message );
            }
        }
        /// <summary>
        /// Flush the Model based on correlation id
        /// </summary>
        /// <param name="correlationId"></param>
        /// <returns>flush status</returns>
        [HttpGet]
        [Route("api/FlushModelAD")]
        public IActionResult FlushModelAD(string correlationId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionController), nameof(FlushModelAD), "Start", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                if (!string.IsNullOrEmpty(correlationId))
                {
                    #region VALIDATIONS
                    CommonUtility.ValidateInputFormData(correlationId, CONSTANTS.CorrelationId, true);
                    #endregion
                    string flushFlag = string.Empty;
                    var userid = this.User.Identity.Name;
                    //Imp do not remove this log
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionController), nameof(FlushModelAD), "--USERID--" + userid, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                    string userrole = _iFlushService.userRole(correlationId, userid, ServiceName);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(AnomalyDetectionController), nameof(FlushModelAD), "--USERROLE--" + userrole, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                    if (userrole == "Valid")
                    {
                        string flushStatus = _iFlushService.FlushModel(correlationId, flushFlag, ServiceName);
                        if (!string.IsNullOrEmpty(flushStatus))
                            return GetSuccessResponse(flushStatus);
                        else
                            return GetSuccessResponse("");
                    }
                    else
                    {
                        return GetFaultResponse("User has no right to delete this model");
                    }
                }
                else
                {
                    return NotFound(Resource.IngrainResx.CorrelatioUIDEmpty);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AnomalyDetectionController), nameof(FlushModelAD), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message );
            }
        }
        #endregion My Model Section
        #region Encryption and Decryption
        [HttpGet]
        [Route("api/GetEncryptedDecryptedValue")]
        public IActionResult GetEncryptedDecryptedValue(string Value, string AesKey, string AesVector, bool IsEncryption)
        {
            try
            {
                dynamic Result = AnomalyDetectionService.GetEncryptedDecryptedValue(Value, AesKey, AesVector, IsEncryption);
                return GetSuccessResponse(Result);
            }
            catch (Exception ex)
            {
                return GetFaultResponse(ex.Message);
            }
        }
        #endregion Encryption and Decryption
        #endregion

    }
}
