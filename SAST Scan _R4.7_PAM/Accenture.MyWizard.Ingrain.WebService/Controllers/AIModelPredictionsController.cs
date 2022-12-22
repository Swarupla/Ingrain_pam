using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
using Accenture.MyWizard.Shared.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using AIGENERICSERVICE = Accenture.MyWizard.Ingrain.DataModels.AICore.GenericService;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
using Accenture.MyWizard.Ingrain.BusinessDomain.Common;

namespace Accenture.MyWizard.Ingrain.WebService.Controllers
{
  
    public class AIModelPredictionsController : MyWizardControllerBase
    {
        #region members
        private IAIModelPredictionsService _aIModelPredictionsService;
        private IngrainAppSettings appSettings;
        #endregion


        #region constructor
        public AIModelPredictionsController(IServiceProvider serviceProvider, IOptions<IngrainAppSettings> settings)
        {
            appSettings = settings.Value;
            _aIModelPredictionsService = serviceProvider.GetService<IAIModelPredictionsService>();
        }

        #endregion

        #region http methods


        /// <summary>
        /// To initiate prediction request for similarity analytics
        /// </summary>
        /// <param></param>
        /// <returns>return prediction request status</returns>
        [HttpPost]
        [Route("api/AIPredictionRequest")]
        public IActionResult AIPredictionRequest()
        {
            try
            {               
                AIGENERICSERVICE.AIModelPredictionResponse aIModelPredictionResponse = _aIModelPredictionsService.InitiatePrediction(HttpContext);
                if(aIModelPredictionResponse.Status == "E")
                {
                    return GetFaultResponse(aIModelPredictionResponse);
                }
                
                return GetSuccessResponse(aIModelPredictionResponse);               
               
            }
            catch (Exception ex)
            {                
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AIModelPredictionsController), nameof(AIPredictionRequest),
                ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
        }


        /// <summary>
        /// To get prediction response for similarity analytics
        /// </summary>
        /// <param></param>
        /// <returns>return prediction response status/result</returns>
        [HttpPost]
        [Route("api/AIPredictionResponse")]
        public IActionResult AIPredictionResponse(AIGENERICSERVICE.AIModelPredictionRequest aIModelPredictionRequest)
        {
            try
            {
                if (!CommonUtility.IsValidGuid(aIModelPredictionRequest.CorrelationId))
                {
                    return GetFaultResponse(string.Format(CONSTANTS.InValidGUID, "CorrelationId"));
                }
                if (!CommonUtility.IsValidGuid(aIModelPredictionRequest.UniqueId))
                {
                    return GetFaultResponse(string.Format(CONSTANTS.InValidGUID, "UniqueId"));
                }
                if (!CommonUtility.IsDataValid(aIModelPredictionRequest.PageNumber))
                {
                    return GetFaultResponse(string.Format(CONSTANTS.InValidData, "PageNumber"));
                }
                AIGENERICSERVICE.AIModelPredictionResponse aIModelPredictionResponse = _aIModelPredictionsService.GetModelPredictionResults(aIModelPredictionRequest);
                if (aIModelPredictionResponse.Status == "E")
                {
                    return GetFaultResponse(aIModelPredictionResponse);
                }

                return GetSuccessResponse(aIModelPredictionResponse);

            }
            catch (Exception ex)
            {                
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AIModelPredictionsController), nameof(AIPredictionRequest), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
        }


        /// <summary>
        /// To initiate prediction request for similarity analytics
        /// </summary>
        /// <param></param>
        /// <returns>return prediction request status</returns>
        [HttpPost]
        [Route("api/SimilarityTrainAndPredict")]
        public IActionResult SimilarityTrainAndPredict()
        {
            try
            {
                AIGENERICSERVICE.AIModelPredictionResponse aIModelPredictionResponse = null;
                string resourceId = Request.Headers["resourceId"];
                if (!string.IsNullOrEmpty(resourceId) || appSettings.authProvider.ToUpper() != CONSTANTS.AzureAD.ToUpper())
                {
                    aIModelPredictionResponse = _aIModelPredictionsService.InitiateTrainAndPrediction(HttpContext,resourceId);
                }
                else
                {
                    throw new ArgumentNullException(nameof(resourceId));
                }
                   
                if (aIModelPredictionResponse.Status == "E")
                {
                    return GetFaultResponse(aIModelPredictionResponse);
                }

                return GetSuccessResponse(aIModelPredictionResponse);

            }
            catch (Exception ex)
            {                
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AIModelPredictionsController), nameof(SimilarityTrainAndPredict), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
        }


        #endregion
    }
}
