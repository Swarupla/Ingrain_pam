using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
using Accenture.MyWizard.Ingrain.DataModels.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using Microsoft.Extensions.DependencyInjection;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
using Accenture.MyWizard.Shared.Helpers;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using Accenture.MyWizard.Ingrain.BusinessDomain.Common;

namespace Accenture.MyWizard.Ingrain.WebService.Controllers
{
    public class VDSDataController : MyWizardControllerBase
    {
        #region Members
        private VDSViewModelDTO models;
        private static IVdsService vDSService { set; get; }
        private readonly IngrainAppSettings appSettings;
        private CallBackErrorLog auditTrailLog;
        private ISPAVelocityService _VelocityService { get; set; }
        #endregion

        public VDSDataController(IServiceProvider serviceProvider, IOptions<IngrainAppSettings> settings)
        {
            vDSService = serviceProvider.GetService<IVdsService>();
            appSettings = settings.Value;
            auditTrailLog = new CallBackErrorLog();
            _VelocityService = serviceProvider.GetService<ISPAVelocityService>();
        }


        #region Methods 
        /// <summary>
        /// Gets all VDS collections data
        /// </summary>
        /// <param name="VDS"></param>
        /// <returns>VDS Data</returns>
        [HttpGet]
        [Route("api/GetVDSModelDetails")]
        public IActionResult GetVDSModelDetails(string CorrelationId, string ModelType)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(VDSDataController), nameof(GetVDSModelDetails), "START", string.IsNullOrEmpty(CorrelationId) ? default(Guid) : new Guid(CorrelationId), string.Empty, string.Empty, string.Empty, string.Empty);

            try
            {
                if (!string.IsNullOrEmpty(CorrelationId))
                {
                    CommonUtility.ValidateInputFormData(CorrelationId, CONSTANTS.CorrelationId, true);
                    VDSModelDTO vdsModelDTOs = new VDSModelDTO();
                    vdsModelDTOs = vDSService.VDSModelDetails(CorrelationId, ModelType);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(VDSDataController), nameof(GetVDSModelDetails), "END", string.IsNullOrEmpty(CorrelationId) ? default(Guid) : new Guid(CorrelationId), string.Empty, string.Empty, string.Empty, string.Empty);
                    return GetSuccessWithMessageResponse(vdsModelDTOs);
                }
                else
                {
                    return GetSuccessWithMessageResponse(Resource.IngrainResx.InputData);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(VDSDataController), nameof(GetVDSModelDetails), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
        }

        /// <summary>
        /// Gets all VDS collections data
        /// </summary>
        /// <param name="VDS"></param>
        /// <returns>VDS Data</returns>
        [HttpGet]
        [Route("api/GetVDSModels")]
        public IActionResult GetVDSModels(string clientUID, string deliveryConstructUID, string modelType)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(VDSDataController), nameof(GetVDSModels), "START", string.Empty, string.Empty, clientUID, deliveryConstructUID);
            try
            {
                if (!string.IsNullOrEmpty(clientUID) && !string.IsNullOrEmpty(deliveryConstructUID) && !string.IsNullOrEmpty(modelType))
                {
                    CommonUtility.ValidateInputFormData(clientUID, CONSTANTS.ClientUID, true);
                    CommonUtility.ValidateInputFormData(deliveryConstructUID, CONSTANTS.DeliveryConstructUID, true);
                    models = new VDSViewModelDTO();
                    models = vDSService.GetVDSModels(clientUID, deliveryConstructUID, modelType);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(VDSDataController), nameof(GetVDSModels), "END", string.Empty, string.Empty, clientUID, deliveryConstructUID);
                    return GetSuccessWithMessageResponse(models);
                }
                else
                {
                    return GetSuccessWithMessageResponse(Resource.IngrainResx.InputData);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(VDSDataController), nameof(GetVDSModels), ex.Message, ex, string.Empty, string.Empty, clientUID, deliveryConstructUID);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
        }
        /// <summary>
        /// Gets all VDS collections data
        /// </summary>
        /// <param name="VDS"></param>
        /// <returns>VDS Data</returns>
        [HttpGet]
        [Route("api/GetVDSManagedInstanceModelDetails")]
        public IActionResult GetVDSManagedInstanceModelDetails(string CorrelationId, string ModelType)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(VDSDataController), nameof(GetVDSModelDetails), "START", string.IsNullOrEmpty(CorrelationId) ? default(Guid) : new Guid(CorrelationId), string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                if (!string.IsNullOrEmpty(CorrelationId))
                {
                    CommonUtility.ValidateInputFormData(CorrelationId, CONSTANTS.CorrelationId, true);
                    VDSModelDTO vdsModelDTOs = new VDSModelDTO();
                    vdsModelDTOs = vDSService.VDSManagedInstanceModelDetails(CorrelationId, ModelType);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(VDSDataController), nameof(GetVDSModelDetails), "END", string.IsNullOrEmpty(CorrelationId) ? default(Guid) : new Guid(CorrelationId), string.Empty, string.Empty, string.Empty, string.Empty);
                    return GetSuccessWithMessageResponse(vdsModelDTOs);
                }
                else
                {
                    return GetSuccessWithMessageResponse(Resource.IngrainResx.InputData);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(VDSDataController), nameof(GetVDSModelDetails), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
        }

        /// <summary>
        /// Gets all VDS collections data
        /// </summary>
        /// <param name="VDS"></param>
        /// <returns>VDS Data</returns>
        [HttpGet]
        [Route("api/GetVDSManagedInstanceModels")]
        public IActionResult GetVDSManagedInstanceModels(string clientUID, string deliveryConstructUID, string modelType, string Environment, string RequestType)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(VDSDataController), nameof(GetVDSModels), "Start", string.Empty, string.Empty, clientUID, deliveryConstructUID);
            try
            {
                if (!string.IsNullOrEmpty(clientUID) && !string.IsNullOrEmpty(deliveryConstructUID) && !string.IsNullOrEmpty(modelType) && !string.IsNullOrEmpty(Environment) && !string.IsNullOrEmpty(RequestType))
                {
                    #region VALIDATION
                    CommonUtility.ValidateInputFormData(clientUID, CONSTANTS.ClientUID, true);
                    CommonUtility.ValidateInputFormData(deliveryConstructUID, CONSTANTS.DeliveryConstructUID, true);
                    #endregion

                    models = new VDSViewModelDTO();
                    models = vDSService.GetVDSManagedInstanceModels(clientUID, deliveryConstructUID, modelType, Environment, RequestType);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(VDSDataController), nameof(GetVDSModels), "END", string.Empty, string.Empty, clientUID, deliveryConstructUID);
                    return GetSuccessWithMessageResponse(models);
                }
                else
                {
                    return GetSuccessWithMessageResponse(Resource.IngrainResx.InputData);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(VDSDataController), nameof(GetVDSModels), ex.Message, ex, string.Empty, string.Empty, clientUID, deliveryConstructUID);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
        }


        /// <summary>
        /// Gets VDS Usecase details
        /// </summary>
        /// <param name="UseCaseId"></param>
        /// <returns>VDS Data</returns>
        [HttpGet]
        [Route("api/GetUseCaseDetails")]
        public IActionResult GetUseCaseDetails(string UseCaseId)
        {
            GetAppService();
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(VDSDataController), nameof(GetUseCaseDetails), "START", string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                if (!string.IsNullOrEmpty(UseCaseId))
                {
                    CommonUtility.ValidateInputFormData(UseCaseId, CONSTANTS.UseCaseId, true);
                    VdsUseCaseDto vdsUseCase = null;
                    vdsUseCase = vDSService.VDSUseCaseDetails(UseCaseId);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(VDSDataController), nameof(GetUseCaseDetails), "END", string.Empty, string.Empty, string.Empty, string.Empty);
                    return GetSuccessWithMessageResponse(vdsUseCase);
                }
                else
                {
                    return GetSuccessWithMessageResponse(Resource.IngrainResx.InputData);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(VDSDataController), nameof(GetUseCaseDetails), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
        }



        /// <summary>
        ///  Gets the Prediction for the VDS data based on usecaseId
        /// </summary>
        /// <param name="requestBody"></param>
        /// <returns>Prediction Response</returns>
        [HttpPost]
        [Route("api/VDSChartsPredictionRequest")]
        public IActionResult VDSChartsPredictionRequest([FromBody] UseCasePredictionRequestInput useCasePredictionRequestInput)
        {
            GetAppService();
            UseCasePredictionRequestOutput useCasePredictionRequestOutput = new UseCasePredictionRequestOutput();
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(VDSDataController), nameof(VDSChartsPredictionRequest), "VDSChartsPredictionRequest Started", string.Empty, string.Empty, useCasePredictionRequestInput.ClientUId, useCasePredictionRequestInput.DeliveryConstructUId);
            try
            {
                IsPredictionEnabled(appSettings.EnablePrediction);

                #region VALIDATION
                CommonUtility.ValidateInputFormData(useCasePredictionRequestInput.ClientUId, CONSTANTS.ClientUID, true);
                CommonUtility.ValidateInputFormData(useCasePredictionRequestInput.DeliveryConstructUId, CONSTANTS.DeliveryConstructUID, true);
                CommonUtility.ValidateInputFormData(useCasePredictionRequestInput.UseCaseId, CONSTANTS.UseCaseID, true);
                CommonUtility.ValidateInputFormData(useCasePredictionRequestInput.Frequency, CONSTANTS.UseCaseID, false);
                CommonUtility.ValidateInputFormData(useCasePredictionRequestInput.ModelType, CONSTANTS.UseCaseID, false);
                #endregion

                if (useCasePredictionRequestInput != null)
                {
                    useCasePredictionRequestOutput = vDSService.GetUseCasePredictionRequest(useCasePredictionRequestInput);
                }
                else
                {
                    return GetFaultResponse(CONSTANTS.InputDataEmpty);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(VDSDataController), nameof(VDSChartsPredictionRequest), ex.Message + $"   StackTrace = {ex.StackTrace}", ex, string.Empty, string.Empty, useCasePredictionRequestInput.ClientUId, useCasePredictionRequestInput.DeliveryConstructUId);
                return GetFaultResponse(ex.Message);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(VDSDataController), nameof(VDSChartsPredictionRequest), "VDSChartsPredictionRequest Ended", string.Empty, string.Empty, useCasePredictionRequestInput.ClientUId, useCasePredictionRequestInput.DeliveryConstructUId);
            return Ok(useCasePredictionRequestOutput);
        }


        /// <summary>
        ///  Gets the Prediction for the VDS data based on usecaseId
        /// </summary>
        /// <param name="requestBody"></param>
        /// <returns>Prediction Response</returns>
        [HttpPost]
        [Route("api/VDSChartsPredictionResponse")]
        public IActionResult VDSChartsPredictionResponse([FromBody] UseCasePredictionResponseInput useCasePredictionResponseInput)
        {
            UseCasePredictionResponseOutput useCasePredictionResponseOutput = new UseCasePredictionResponseOutput();
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(VDSDataController), nameof(VDSChartsPredictionResponse), "VDSChartsPredictionResponse Started", string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                #region VALIDATION
                CommonUtility.ValidateInputFormData(useCasePredictionResponseInput.UniqueId, CONSTANTS.UniqueId, true);
                CommonUtility.ValidateInputFormData(useCasePredictionResponseInput.UseCaseId, CONSTANTS.UseCaseID, true);
                CommonUtility.ValidateInputFormData(useCasePredictionResponseInput.PageNumber, CONSTANTS.PageNumber, false);
                #endregion

                if (useCasePredictionResponseOutput != null)
                {
                    useCasePredictionResponseOutput = vDSService.GetUseCasePredictionResponse(useCasePredictionResponseInput);
                }
                else
                {
                    return GetFaultResponse(CONSTANTS.InputDataEmpty);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(VDSDataController), nameof(VDSChartsPredictionResponse), ex.Message + $"   StackTrace = {ex.StackTrace}", ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(VDSDataController), nameof(VDSChartsPredictionResponse), "VDSChartsPredictionResponse Ended", string.Empty, string.Empty, string.Empty, string.Empty);
            return Ok(useCasePredictionResponseOutput);
        }
        /// <summary>
        ///  Train VDS UseCase
        /// </summary>
        /// <param name="requestBody"></param>
        /// <returns>Prediction Response</returns>
        [HttpPost]
        [Route("api/VDSTrainUseCase")]
        public IActionResult VDSTrainUseCase([FromBody] VdsUseCaseTrainingRequest vDSUseCaseTrainingRequest)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(VDSDataController), nameof(VDSTrainUseCase), "VDSTrainUseCase Started", vDSUseCaseTrainingRequest.ApplicationId, string.Empty, vDSUseCaseTrainingRequest.ClientUId, vDSUseCaseTrainingRequest.DeliveryConstructUId);
            try
            {
                #region VALIDATION
                CommonUtility.ValidateInputFormData(vDSUseCaseTrainingRequest.ApplicationId, CONSTANTS.ApplicationId, true);
                CommonUtility.ValidateInputFormData(vDSUseCaseTrainingRequest.ClientUId, CONSTANTS.ClientUID, true);
                CommonUtility.ValidateInputFormData(vDSUseCaseTrainingRequest.DeliveryConstructUId, CONSTANTS.DeliveryConstructUID, true);
                CommonUtility.ValidateInputFormData(vDSUseCaseTrainingRequest.E2EUId, CONSTANTS.E2EUId, true);
                CommonUtility.ValidateInputFormData(vDSUseCaseTrainingRequest.Retrain, CONSTANTS.Retrain, false);
                CommonUtility.ValidateInputFormData(vDSUseCaseTrainingRequest.UseCaseId, CONSTANTS.UseCaseId, true);
                if (!CommonUtility.GetValidUser(vDSUseCaseTrainingRequest.UserId))
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);
                #endregion

                if (vDSUseCaseTrainingRequest != null)
                {
                    return GetSuccessResponse(vDSService.TrainVDSUseCase(vDSUseCaseTrainingRequest));
                }
                else
                {
                    return GetFaultResponse(CONSTANTS.InputDataEmpty);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(VDSDataController), nameof(VDSTrainUseCase), ex.Message + $"   StackTrace = {ex.StackTrace}", ex, vDSUseCaseTrainingRequest.ApplicationId, string.Empty, vDSUseCaseTrainingRequest.ClientUId, vDSUseCaseTrainingRequest.DeliveryConstructUId);
                return GetFaultResponse(ex.Message);
            }
        }
        #region Genric Model Training and Prediction for FDS and PAM
        /// <summary>
        ///  Initiate VDS Generic Model Training
        /// </summary>
        [HttpPost]
        [Route("api/IngrainGenericModelTrainingAPIForAIApps")]
        public IActionResult IngrainGenericModelTrainingAPIForAIApps(GenericModelTrainingRequest GenericRequest)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    List<string> AMEntities = new List<string>() { "Incidents", "ProblemTickets", "ServiceRequests", "WorkRequests", "CHANGE_REQUEST" };
                    List<string> IOEntities = new List<string>() { "Incidents", "ProblemTickets", "ServiceRequests", "ChangeRequests" };
                    List<string> FunctionalAreas = new List<string>() { "AM","IO" };
                    #region VALIDATION    
                    if (appSettings.Environment != CONSTANTS.FDSEnvironment && appSettings.Environment != CONSTANTS.PAMEnvironment)
                        return GetFaultResponse("Invalid Environment, this API with provided input parameter is applicable only in FDS and PAM Environments.");
                    CommonUtility.ValidateInputFormData(GenericRequest.ApplicationId, CONSTANTS.ApplicationId, true);
                    CommonUtility.ValidateInputFormData(GenericRequest.ClientUId, CONSTANTS.ClientUID, true);
                    CommonUtility.ValidateInputFormData(GenericRequest.DeliveryConstructUId, CONSTANTS.DeliveryConstructUID, true);
                    CommonUtility.ValidateInputFormData(GenericRequest.ResponseCallbackUrl, CONSTANTS.ResponseCallbackUrl, false, GenericRequest.ApplicationId);
                    CommonUtility.ValidateInputFormData(GenericRequest.UseCaseId, CONSTANTS.UseCaseID, true);
                    CommonUtility.ValidateInputFormData(Convert.ToString(GenericRequest.DataSourceDetails), "DataSourceDetails", false);
                    CommonUtility.ValidateInputFormData(Convert.ToString(GenericRequest.DataSourceDetails.E2EUID), CONSTANTS.E2EUID, true);
                    if (!CommonUtility.GetValidUser(GenericRequest.UserId))
                        return GetFaultResponse(Resource.IngrainResx.InValidUser);
                    if (GenericRequest.ResponseCallbackUrl == "")
                    {
                        return GetFaultResponse(CONSTANTS.ResponseCallbackUrlNull);
                    }
                    if (Convert.ToString(GenericRequest.DataSourceDetails) != null && Convert.ToString(GenericRequest.DataSourceDetails) != "{}")
                    {
                        if (string.IsNullOrEmpty(Convert.ToString(GenericRequest.DataSourceDetails.RequestType)))
                            return BadRequest("The RequestType field is required.");
                        else if (string.IsNullOrEmpty(Convert.ToString(GenericRequest.DataSourceDetails.ServiceType)))
                            return BadRequest("The ServiceType field is required.");
                        else if (string.IsNullOrEmpty(Convert.ToString(GenericRequest.DataSourceDetails.E2EUID)))
                            return BadRequest("The E2EUID field is required.");
                    }
                    else
                        return BadRequest("The DataSourceDetails field is required.");

                    if (!FunctionalAreas.Contains(Convert.ToString(GenericRequest.DataSourceDetails.RequestType)))
                        return GetFaultResponse("Incorrect Request type, please try again");
                    if (Convert.ToString(GenericRequest.DataSourceDetails.RequestType) == "AM" && !AMEntities.Contains(Convert.ToString(GenericRequest.DataSourceDetails.ServiceType)))
                        return GetFaultResponse("Incorrect Service Type for Functional Area: AM, please try again");
                    if (Convert.ToString(GenericRequest.DataSourceDetails.RequestType) == "IO" && !IOEntities.Contains(Convert.ToString(GenericRequest.DataSourceDetails.ServiceType)))
                        return GetFaultResponse("Incorrect Service Type for Functional Area: IO, please try again");

                    IsTrainingEnabled(appSettings.EnableTraining);
                    #endregion

                    GetAppService();
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(VDSDataController), nameof(IngrainGenericModelTrainingAPIForAIApps), "IngrainAPI Started", GenericRequest.ApplicationId, GenericRequest.UseCaseId, GenericRequest.ClientUId, GenericRequest.DeliveryConstructUId);

                    //Asset Tracking
                    auditTrailLog.ApplicationID = GenericRequest.ApplicationId;
                    auditTrailLog.BaseAddress = GenericRequest.ResponseCallbackUrl;
                    auditTrailLog.httpResponse = null;
                    auditTrailLog.ClientId = GenericRequest.ClientUId;
                    auditTrailLog.DCID = GenericRequest.DeliveryConstructUId;
                    auditTrailLog.ApplicationID = GenericRequest.ApplicationId;
                    auditTrailLog.UseCaseId = GenericRequest.UseCaseId;
                    auditTrailLog.RequestId = null;
                    auditTrailLog.CreatedBy = GenericRequest.UserId;
                    auditTrailLog.ProcessName = CONSTANTS.TrainingName;
                    auditTrailLog.UsageType = CONSTANTS.AssetUsage;
                    _VelocityService.AuditTrailLog(auditTrailLog);

                    var data = vDSService.StartGenericModelTraining(GenericRequest);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(VDSDataController), nameof(IngrainGenericModelTrainingAPIForAIApps), "IngrainAPI Ended with Success", GenericRequest.ApplicationId, string.Empty, GenericRequest.ClientUId, GenericRequest.DeliveryConstructUId);
                    return Ok(data);
                }
                else
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(VDSDataController), nameof(IngrainGenericModelTrainingAPIForAIApps), "IngrainAPI Ended as " + CONSTANTS.InputDataEmpty, GenericRequest.ApplicationId, string.Empty, GenericRequest.ClientUId, GenericRequest.DeliveryConstructUId);
                    return BadRequest(ModelState);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(VDSDataController), nameof(IngrainGenericModelTrainingAPIForAIApps), "IngrainAPI Ended with Error: " + ex.Message + $"   StackTrace = {ex.StackTrace}", ex, GenericRequest.ApplicationId, string.Empty, GenericRequest.ClientUId, GenericRequest.DeliveryConstructUId);
                return GetFaultResponse(ex.Message);
            }
        }
        /// <summary>
        ///  Get the response of VDS Generic Model Training based on CorrelationId
        /// </summary>
        [HttpGet]
        [Route("api/IngrainAIAppsGenericTrainingResponse")]
        public IActionResult IngrainAIAppsGenericTrainingResponse(string CorrelationId)
        {
            GetAppService();
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(VDSDataController), nameof(IngrainAIAppsGenericTrainingResponse), "START", string.IsNullOrEmpty(CorrelationId) ? default(Guid) : new Guid(CorrelationId), string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                if (!(string.IsNullOrEmpty(CorrelationId)))
                    return Ok(vDSService.IngrainAIAppsGenericTrainingResponse(CorrelationId));
                else
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(VDSDataController), nameof(IngrainAIAppsGenericTrainingResponse), "IngrainAIAppsGenericTrainingResponse Ended as " + CONSTANTS.InputDataEmpty, string.Empty, string.Empty, string.Empty, string.Empty);
                    return GetFaultResponse("Please provide CorrelationId.");
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(VDSDataController), nameof(IngrainAIAppsGenericTrainingResponse), ex.Message + $"   StackTrace = {ex.StackTrace}", ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
        }
        /// <summary>
        ///  Initiate Prediction for the VDS Generic Model based on usecaseId
        /// </summary>
        /// <param name="requestBody"></param>
        /// <returns>Prediction Response</returns>
        [HttpPost]
        [Route("api/IntiateVDSGenericModelPrediction")]
        public IActionResult IntiateVDSGenericModelPrediction([FromBody] VDSUseCasePredictionRequest VDSUseCasePredictionRequestInput)
        {
            GetAppService();
            VDSUseCasePredictionOutput VDSUseCasePredictionOutput = new VDSUseCasePredictionOutput();
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(VDSDataController), nameof(IntiateVDSGenericModelPrediction), "IntiateVDSGenericModelPrediction Started for Correlation Id: " + VDSUseCasePredictionRequestInput.CorrelationId, string.Empty, string.Empty, VDSUseCasePredictionRequestInput.ClientUID, VDSUseCasePredictionRequestInput.DeliveryConstructUID);
            try
            {
                if (ModelState.IsValid)
                {
                    #region VALIDATION
                    if (appSettings.Environment != CONSTANTS.FDSEnvironment && appSettings.Environment != CONSTANTS.PAMEnvironment)
                        return GetFaultResponse("Invalid Environment, this API with provided input parameter is applicable only in FDS and PAM Environments.");
                    IsPredictionEnabled(appSettings.EnablePrediction);
                    if (VDSUseCasePredictionRequestInput.UseCaseUID != "49d56fe0-1eca-4406-8b52-38724ac3b705" && (string.IsNullOrEmpty(Convert.ToString(VDSUseCasePredictionRequestInput.Data)) || Convert.ToString(VDSUseCasePredictionRequestInput.Data) == "[]" || Convert.ToString(VDSUseCasePredictionRequestInput.Data) == "{}"))//bypassing Data validation for Timeseries Model
                        return BadRequest("The Data field is required.");
                    if (VDSUseCasePredictionRequestInput.StartDates.Count <= 0)
                        return BadRequest("The StartDates field is required.");
                    CommonUtility.ValidateInputFormData(VDSUseCasePredictionRequestInput.AppServiceUID, CONSTANTS.AppServiceUID, true);
                    CommonUtility.ValidateInputFormData(VDSUseCasePredictionRequestInput.ClientUID, CONSTANTS.ClientUID, true);
                    CommonUtility.ValidateInputFormData(VDSUseCasePredictionRequestInput.DeliveryConstructUID, CONSTANTS.DeliveryConstructUID, true);
                    CommonUtility.ValidateInputFormData(VDSUseCasePredictionRequestInput.CorrelationId, CONSTANTS.CorrelationId, true);
                    CommonUtility.ValidateInputFormData(VDSUseCasePredictionRequestInput.UseCaseUID, CONSTANTS.UseCaseID, true);
                    CommonUtility.ValidateInputFormData(Convert.ToString(VDSUseCasePredictionRequestInput.Data), "Data", false, VDSUseCasePredictionRequestInput.AppServiceUID);
                    if (VDSUseCasePredictionRequestInput.StartDates != null)
                        VDSUseCasePredictionRequestInput.StartDates.ForEach(x => CommonUtility.ValidateInputFormData(x, "StartDates", false));
                    #endregion
                    VDSUseCasePredictionOutput = vDSService.IntiateVDSGenericModelPrediction(VDSUseCasePredictionRequestInput);
                }
                else
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(VDSDataController), nameof(IntiateVDSGenericModelPrediction), "IntiateVDSGenericModelPrediction Ended as " + CONSTANTS.InputDataEmpty, VDSUseCasePredictionRequestInput.AppServiceUID, string.Empty, VDSUseCasePredictionRequestInput.ClientUID, VDSUseCasePredictionRequestInput.DeliveryConstructUID);
                    return BadRequest(ModelState);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(VDSDataController), nameof(IntiateVDSGenericModelPrediction), "CorrelationId: " + VDSUseCasePredictionRequestInput.CorrelationId + ex.Message + $"   StackTrace = {ex.StackTrace}", ex, string.Empty, string.Empty, VDSUseCasePredictionRequestInput.ClientUID, VDSUseCasePredictionRequestInput.DeliveryConstructUID);
                return GetFaultResponse(ex.Message);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(VDSDataController), nameof(IntiateVDSGenericModelPrediction), "IntiateVDSGenericModelPrediction Ended for CorrelationId: " + VDSUseCasePredictionRequestInput.CorrelationId, string.Empty, string.Empty, VDSUseCasePredictionRequestInput.ClientUID, VDSUseCasePredictionRequestInput.DeliveryConstructUID);
            return Ok(VDSUseCasePredictionOutput);
        }
        /// <summary>
        ///  Gets the Prediction for the VDS Generic Model based on usecaseId
        /// </summary>
        /// <param name="requestBody"></param>
        /// <returns>Prediction Response</returns>
        [HttpPost]
        [Route("api/GetVDSGenericModelPrediction")]
        public IActionResult GetVDSGenericModelPrediction([FromBody] VDSPredictionResponseInput useCasePredictionResponseInput)
        {
            VDSPredictionResponseOutput useCasePredictionResponseOutput = new VDSPredictionResponseOutput();
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(VDSDataController), nameof(GetVDSGenericModelPrediction), "GetVDSGenericModelPrediction Started for " + useCasePredictionResponseInput.CorrelationId, string.Empty, string.Empty, string.Empty, useCasePredictionResponseInput.UniqueId);
            try
            {
                #region VALIDATION
                if (appSettings.Environment != CONSTANTS.FDSEnvironment && appSettings.Environment != CONSTANTS.PAMEnvironment)
                    return GetFaultResponse("Invalid Environment, this API with provided input parameter is applicable only in FDS and PAM Environments.");
                CommonUtility.ValidateInputFormData(useCasePredictionResponseInput.UniqueId, CONSTANTS.UniqueId, true);
                CommonUtility.ValidateInputFormData(useCasePredictionResponseInput.CorrelationId, CONSTANTS.CorrelationId, true);
                #endregion
                if (ModelState.IsValid)
                    useCasePredictionResponseOutput = vDSService.GetVDSGenericModelPrediction(useCasePredictionResponseInput);
                else
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(VDSDataController), nameof(GetVDSGenericModelPrediction), "GetVDSGenericModelPrediction Ended as " + CONSTANTS.InputDataEmpty, string.Empty, string.Empty, string.Empty, String.Empty);
                    return BadRequest(ModelState);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(VDSDataController), nameof(GetVDSGenericModelPrediction), ex.Message + $"   StackTrace = {ex.StackTrace}", ex, string.Empty, string.Empty, useCasePredictionResponseInput.CorrelationId, useCasePredictionResponseInput.UniqueId);
                return GetFaultResponse(ex.Message);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(VDSDataController), nameof(GetVDSGenericModelPrediction), "GetVDSGenericModelPrediction Ended for " + useCasePredictionResponseInput.CorrelationId, string.Empty, string.Empty, string.Empty, useCasePredictionResponseInput.UniqueId);
            return Ok(useCasePredictionResponseOutput);
        }
        #endregion Genric Model Training and Prediction for FDS and PAM
        #endregion
    }
}