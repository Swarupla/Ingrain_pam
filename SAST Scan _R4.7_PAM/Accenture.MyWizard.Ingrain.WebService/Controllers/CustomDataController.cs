using Accenture.MyWizard.Shared.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
using Accenture.MyWizard.Ingrain.DataModels.Models;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace Accenture.MyWizard.Ingrain.WebService.Controllers
{
    public class CustomDataController : MyWizardControllerBase
    {
        #region Members
        private static ICustomDataService _customDataService { set; get; }
        private IngrainAppSettings appSettings;
        IngrainRequestQueue requestQueue = new IngrainRequestQueue();
        PythonCategory pythonCategory = new PythonCategory();
        PythonInfo pythonInfo = new PythonInfo();
        private readonly IIngestedData _IIngestedDataService;
        #endregion
        #region Constructor
        public CustomDataController(IServiceProvider serviceProvider, IOptions<IngrainAppSettings> settings)
        {
            appSettings = settings.Value;
            _customDataService = serviceProvider.GetService<ICustomDataService>();
            _IIngestedDataService = serviceProvider.GetService<IIngestedData>();
        }

        #endregion

        [HttpPost]
        [Route("api/TestQueryResponse")]
        public IActionResult TestQueryResponse(string userId, string clientId, string deliveryId, string category, string ServiceLevel )
        {
            object returnvalue = new object();
            JObject result = new JObject();
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CustomDataController), nameof(TestQueryResponse), "Start", string.Empty, string.Empty, clientId, deliveryId);
            try
            {
                if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(deliveryId) || string.IsNullOrEmpty(userId))
                {
                    return GetFaultResponse("Client Id, DC ID and User ID Values are Null. Please try again");
                }
                else if (clientId == CONSTANTS.undefined || deliveryId == CONSTANTS.undefined || userId == CONSTANTS.undefined)
                {
                    return GetFaultResponse(CONSTANTS.InutFieldsUndefined);
                }
                else if (string.IsNullOrEmpty(ServiceLevel))
                {
                    return GetFaultResponse("ServiceLevel is null. Please try again");
                }
                else
                {
                    returnvalue = _customDataService.TestQueryData(clientId, deliveryId, userId, HttpContext, category, ServiceLevel, out bool isError);
                    if (isError)
                    {
                        result = JObject.Parse(Convert.ToString(returnvalue));
                        if (result.ContainsKey("message"))
                        {
                            return GetFaultResponse(Convert.ToString(result["message"]));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(CustomDataController), nameof(TestQueryResponse), ex.Message + $"   StackTrace = {ex.StackTrace}", ex, string.Empty, string.Empty, clientId, deliveryId);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CustomDataController), nameof(TestQueryResponse), "End", string.Empty, string.Empty, clientId, deliveryId);
            return GetSuccessWithMessageResponse(returnvalue);
        }

        [HttpPost]
        [Route("api/CheckCustomAPIResponse")]
        public IActionResult CheckCustomAPIResponse(string userId, string clientUID, string deliveryUID, string category)
        {
            object returnvalue = new object();
            JObject result = new JObject();
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CustomDataController), nameof(CheckCustomAPIResponse), "Start", string.Empty, string.Empty, clientUID, deliveryUID);
            try
            {
                if (string.IsNullOrEmpty(clientUID) || string.IsNullOrEmpty(deliveryUID) || string.IsNullOrEmpty(userId))
                {
                    return GetFaultResponse("Client Id, DC ID and User ID Values are Null. Please try again");
                }
                else if (clientUID == CONSTANTS.undefined || deliveryUID == CONSTANTS.undefined || userId == CONSTANTS.undefined)
                {
                    return GetFaultResponse(CONSTANTS.InutFieldsUndefined);
                }
                else
                {
                   return Ok(_customDataService.CheckCustomAPIResponse(clientUID, deliveryUID, userId, HttpContext, category));
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(CustomDataController), nameof(CheckCustomAPIResponse), ex.Message + $"   StackTrace = {ex.StackTrace}", ex, string.Empty, string.Empty, clientUID, deliveryUID);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
            
            return GetSuccessWithMessageResponse(returnvalue);
        }


        [HttpGet]
        [Route("api/GetCustomSourceDetails")]
        public IActionResult GetCustomSourceDetails(string correlationid, string CustomSourceType)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CustomDataController), nameof(GetCustomSourceDetails), "START", string.IsNullOrEmpty(correlationid) ? default(Guid) : new Guid(correlationid), string.Empty, string.Empty, string.Empty, string.Empty);
            var message = string.Empty;
            try
            {
                if (string.IsNullOrEmpty(correlationid))
                    message = "Correlation Id is Null, Please provide Correlation Id.";
                else if (correlationid == CONSTANTS.undefined)
                    message = CONSTANTS.InutFieldsUndefined;
                else
                {
                    var Result = _customDataService.GetCustomSourceDetails(correlationid, CustomSourceType, CONSTANTS.SSAICustomDataSource);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(CustomDataController), nameof(GetCustomSourceDetails), "END", string.IsNullOrEmpty(correlationid) ? default(Guid) : new Guid(correlationid), string.Empty, string.Empty, string.Empty, string.Empty);
                    return GetSuccessWithMessageResponse(Result);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(CustomDataController), nameof(GetCustomSourceDetails), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CustomDataController), nameof(GetCustomSourceDetails), "END", string.IsNullOrEmpty(correlationid) ? default(Guid) : new Guid(correlationid), string.Empty, string.Empty, string.Empty, string.Empty);
            return GetSuccessWithMessageResponse(message);
        }
    }
}
