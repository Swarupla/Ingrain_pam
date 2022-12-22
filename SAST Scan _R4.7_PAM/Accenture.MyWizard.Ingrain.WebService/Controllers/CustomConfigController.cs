#region Namespace References
using System;
using System.Collections.Generic;
using Accenture.MyWizard.Ingrain.DataModels.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Accenture.MyWizard.Shared.Helpers;
using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
#endregion
namespace Accenture.MyWizard.Ingrain.WebService.Controllers
{
    public class CustomConfigController : MyWizardControllerBase
    {

        #region Members      
        CallBackErrorLog auditTrailLog;
        private readonly IOptions<IngrainAppSettings> appSettings;
        private static ICustomConfigService _customConfigService { set; get; }
        private ISPAVelocityService _VelocityService { get; set; }
        #endregion

        #region Constructors    
        /// <summary>
        /// Constructor to Initialize the objects
        /// </summary>
        public CustomConfigController(IServiceProvider serviceProvider, IOptions<IngrainAppSettings> settings)
        {
            appSettings = settings;
            _customConfigService = serviceProvider.GetService<ICustomConfigService>();
            _VelocityService = serviceProvider.GetService<ISPAVelocityService>();
            auditTrailLog = new CallBackErrorLog();
        }
        #endregion

        [HttpGet]
        [Route("api/GetCustomConfiguration")]
        public IActionResult GetCustomConfiguration(string serviceType, string serviceLevel)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CustomConfigController), nameof(GetCustomConfiguration), "Start", string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                return Ok(_customConfigService.GetCustomConfigurations(serviceType, serviceLevel));

            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(CustomConfigController), nameof(GetCustomConfiguration), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
        }

        [HttpPost]
        [Route("api/PostCustomConfiguration")]
        public IActionResult PostCustomConfiguration([FromBody] dynamic requestBody, string ServiceLevel)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CustomConfigController), nameof(PostCustomConfiguration), "Start", string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                string columnsData = Convert.ToString(requestBody);
                dynamic dynamicData = Newtonsoft.Json.JsonConvert.DeserializeObject(columnsData);
                JObject inputData = JObject.Parse(columnsData);
                if (!string.IsNullOrEmpty(inputData["userId"].ToString()))
                {
                    _customConfigService.SaveCustomConfigurations(HttpContext , ServiceLevel, dynamicData);
                    return Ok();
                }
                else
                {
                    return GetSuccessResponse(Resource.IngrainResx.InputData);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(CustomConfigController), nameof(PostCustomConfiguration), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
        }

        //for SSAI Flow
        [HttpPost]
        [Route("api/InitiateModelTraining")]
        public IActionResult InitiateModelTraining(TrainingRequestDTO oTrainingRequest)
        {
            try
            {
                IsTrainingEnabled(appSettings.Value.EnableTraining);
                GetAppService();
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(CustomConfigController), nameof(InitiateModelTraining), "InitiateModelTraining Started", oTrainingRequest.ProvisionedAppServiceUID, string.Empty, oTrainingRequest.ClientUID, oTrainingRequest.DeliveryConstructUID);

                //Asset Tracking
                auditTrailLog.ApplicationID = oTrainingRequest.ApplicationId;
                auditTrailLog.BaseAddress = oTrainingRequest.ResponseCallbackUrl;
                auditTrailLog.httpResponse = null;
                auditTrailLog.ClientId = oTrainingRequest.ClientUID;
                auditTrailLog.DCID = oTrainingRequest.DeliveryConstructUID;
                auditTrailLog.AppServiceUID = oTrainingRequest.ProvisionedAppServiceUID;
                auditTrailLog.UseCaseId = oTrainingRequest.UseCaseId;
                auditTrailLog.RequestId = null;
                auditTrailLog.CreatedBy = oTrainingRequest.UserID;
                auditTrailLog.ProcessName = CONSTANTS.TrainingName;
                auditTrailLog.UsageType = CONSTANTS.AssetUsage;
                _customConfigService.AuditTrailLog(auditTrailLog);
                if (!string.IsNullOrEmpty(oTrainingRequest.UseCaseId) && !string.IsNullOrEmpty(oTrainingRequest.ProvisionedAppServiceUID) && !string.IsNullOrEmpty(oTrainingRequest.ClientUID)
                    && !string.IsNullOrEmpty(oTrainingRequest.UserID) && !string.IsNullOrEmpty(oTrainingRequest.DeliveryConstructUID))
                {
                   if(oTrainingRequest.IsAmbulanceLane != null && oTrainingRequest.IsAmbulanceLane.ToUpper() == "TRUE")
                    {
                        if (oTrainingRequest.QueryData != null && oTrainingRequest.QueryData["IterationUID"] != null)
                        {
                            if (oTrainingRequest.RetrainRequired == "AutoRetrain")
                            {
                                JArray items = (JArray)oTrainingRequest.QueryData["IterationUID"];
                                if (items != null && items.Count < 1)
                                {
                                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(CustomConfigController), nameof(InitiateModelTraining), "IngrainAPI Ended", oTrainingRequest.ProvisionedAppServiceUID, string.Empty, oTrainingRequest.ClientUID, oTrainingRequest.DeliveryConstructUID);

                                    return GetFaultResponse(CONSTANTS.IterationUIDMandatory);
                                }
                            }
                            else
                            {
                                JArray items = (JArray)oTrainingRequest.QueryData["IterationUID"];
                                long dataPoints = _customConfigService.GetDatapoints(oTrainingRequest.UseCaseId, oTrainingRequest.ProvisionedAppServiceUID);
                                if (items != null && items.Count < dataPoints)
                                {
                                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(CustomConfigController), nameof(InitiateModelTraining), "IngrainAPI Ended", oTrainingRequest.ProvisionedAppServiceUID, string.Empty, oTrainingRequest.ClientUID, oTrainingRequest.DeliveryConstructUID);

                                    return GetFaultResponse(String.Format(CONSTANTS.MinIterationUIDMandatory, dataPoints));
                                }
                            }
                        }
                        else
                        {
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CustomConfigController), nameof(InitiateModelTraining), "IngrainAPI Ended", oTrainingRequest.ProvisionedAppServiceUID, string.Empty, oTrainingRequest.ClientUID, oTrainingRequest.DeliveryConstructUID);

                            return GetFaultResponse(CONSTANTS.IterationUIDMandatory);
                        }
                    }
                    var data = _customConfigService.StartTraining(oTrainingRequest);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(CustomConfigController), nameof(InitiateModelTraining), "IngrainAPI Ended", oTrainingRequest.ProvisionedAppServiceUID, string.Empty, oTrainingRequest.ClientUID, oTrainingRequest.DeliveryConstructUID);
                    return Ok(data);
                }
                else
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(CustomConfigController), nameof(InitiateModelTraining), "IngrainAPI Ended", oTrainingRequest.ProvisionedAppServiceUID, string.Empty, oTrainingRequest.ClientUID, oTrainingRequest.DeliveryConstructUID);
                    return GetFaultResponse(CONSTANTS.InputDataEmpty);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(CustomConfigController), nameof(InitiateModelTraining), ex.Message + $"   StackTrace = {ex.StackTrace}", ex, oTrainingRequest.ProvisionedAppServiceUID, string.Empty, oTrainingRequest.ClientUID, oTrainingRequest.DeliveryConstructUID);
                return GetFaultResponse(ex.Message);
            }
        }

        //for AI Flow
        [HttpPost]
        [Route("api/InitiateAIModelTraining")]
        public IActionResult InitiateAIModelTraining()
        {
            try
            {
                IsTrainingEnabled(appSettings.Value.EnableTraining);
                GetAppService();
                string resourceId = Request.Headers["resourceId"];
                return Ok(_customConfigService.TrainAIServiceModel(HttpContext, resourceId));

            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AICoreController), nameof(InitiateAIModelTraining), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
        }

    }
}
