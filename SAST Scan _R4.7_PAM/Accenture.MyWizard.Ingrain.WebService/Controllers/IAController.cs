using System;
using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
using Accenture.MyWizard.Ingrain.DataModels.Models;
using Accenture.MyWizard.Shared.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
using Microsoft.Extensions.DependencyInjection;
using System.Drawing.Printing;
using Accenture.MyWizard.Ingrain.BusinessDomain.Common;

namespace Accenture.MyWizard.Ingrain.WebService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IAController : MyWizardControllerBase
    {
        #region Private Members
        private IngrainAppSettings appSettings { get; set; }
        private IIAService _iAService;


        #endregion

        #region Constructor
        public IAController(IServiceProvider serviceProvider, IOptions<IngrainAppSettings> settings)
        {
            appSettings = settings.Value;
            _iAService = serviceProvider.GetService<IIAService>();
            
        }
        #endregion

        [HttpPost]
        [Route("InitiateTraining")]
        public IActionResult InitiateTraining([FromBody] dynamic requestBody)
        {
            GetAppService();
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(IAController)
                                                     , nameof(InitiateTraining)
                                                     , "START", string.Empty, string.Empty, string.Empty, string.Empty);
            GenericDataResponse trainingResponse = new GenericDataResponse();

            try
            {
                IsTrainingEnabled(appSettings.EnableTraining);
                string data = Convert.ToString(requestBody);
                TrainingRequestDetails trainingRequestDetails = JsonConvert.DeserializeObject<TrainingRequestDetails>(data);
                
                string resourceId = Request.Headers["resourceId"];

                if (!CommonUtility.GetValidUser(trainingRequestDetails.UserId))
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);

                if (trainingRequestDetails != null && (!string.IsNullOrEmpty(resourceId) || appSettings.authProvider.ToUpper() != CONSTANTS.AzureAD.ToUpper()))
                {
                    CommonUtility.ValidateInputFormData(trainingRequestDetails.ClientUId, "ClientUId", true);
                    CommonUtility.ValidateInputFormData(trainingRequestDetails.DeliveryConstructUId, "DeliveryConstructUId", true);
                    CommonUtility.ValidateInputFormData(trainingRequestDetails.UseCaseId, "UseCaseId", true);
                    CommonUtility.ValidateInputFormData(trainingRequestDetails.CorrelationId, "CorrelationId", true);
                    CommonUtility.ValidateInputFormData(trainingRequestDetails.ApplicationId, "ApplicationId", true);
                    CommonUtility.ValidateInputFormData(trainingRequestDetails.DataSource, "DataSource", false);
                    CommonUtility.ValidateInputFormData(trainingRequestDetails.IngrainTrainingResponseCallBackUrl, "IngrainTrainingResponseCallBackUrl", false);                    
                    CommonUtility.ValidateInputFormData(trainingRequestDetails.Data, "Data", false);
                    CommonUtility.ValidateInputFormData(trainingRequestDetails.DataFlag, "DataFlag", false);
                    if (trainingRequestDetails.DataSourceDetails != null)
                    {
                        CommonUtility.ValidateInputFormData(Convert.ToString(trainingRequestDetails.DataSourceDetails.Url), "DataSourceDetailsUrl", false);
                        CommonUtility.ValidateInputFormData(Convert.ToString(trainingRequestDetails.DataSourceDetails.HttpMethod), "DataSourceDetailsHttpMethod", false);
                        CommonUtility.ValidateInputFormData(Convert.ToString(trainingRequestDetails.DataSourceDetails.FetchType), "DataSourceDetailsFetchType", false);
                        CommonUtility.ValidateInputFormData(Convert.ToString(trainingRequestDetails.DataSourceDetails.BatchSize), "DataSourceDetailsBatchSize", false);
                        CommonUtility.ValidateInputFormData(Convert.ToString(trainingRequestDetails.DataSourceDetails.TotalNoOfRecords), "DataSourceDetailsTotalNoOfRecords", false);
                        CommonUtility.ValidateInputFormData(Convert.ToString(trainingRequestDetails.DataSourceDetails.BodyParams), "DataSourceDetailsBodyParams", false);
                    }

                    return GetSuccessResponse(_iAService.InitiateTrainingRequest(trainingRequestDetails, resourceId));
                }
                else
                {
                    trainingResponse.Message = CONSTANTS.IncompleteTrainingRequest;
                    trainingResponse.Status = CONSTANTS.E;
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(IAController)
                                                              , nameof(InitiateTraining)
                                                              , ex.Message + $"   StackTrace = {ex.StackTrace}", ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }

            LOGGING.LogManager.Logger.LogProcessInfo(typeof(IAController), nameof(InitiateTraining), "END", string.Empty, string.Empty, string.Empty, string.Empty);
            return GetSuccessResponse(trainingResponse);
        }


        [HttpPost]
        [Route("PredictionRequest")]
        public IActionResult PredictionRequest(IAPredictionRequest iAPredictionRequest)
        {
            PredictionResultDTO predictionResultDTO = new PredictionResultDTO();
            predictionResultDTO.CorrelationId = iAPredictionRequest.CorrelationId;
            try
            {
                IsPredictionEnabled(appSettings.EnablePrediction);
                CommonUtility.ValidateInputFormData(iAPredictionRequest.CorrelationId, "CorrelationId", true);
                if (iAPredictionRequest.ReleaseUId != null)
                    iAPredictionRequest.ReleaseUId.ForEach(x => CommonUtility.ValidateInputFormData(x, "ReleaseUId", true));
                return GetSuccessResponse(_iAService.InitiatePrediction(iAPredictionRequest));
            }
            catch(Exception ex)
            {
                predictionResultDTO.Status = "E";
                predictionResultDTO.Message = ex.Message;
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(IAController)
                                                              , nameof(InitiateTraining)
                                                              , ex.Message + $"   StackTrace = {ex.StackTrace}", ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(predictionResultDTO);

            }
        }


        [HttpPost]
        [Route("UseCasePredictionRequest")]
        public IActionResult UseCasePredictionRequest(IAUseCasePredictionRequest iAUseCasePredictionRequest)
        {
            GetAppService();
            IAUseCasePredictionResponse iAUseCasePredictionResponse = new IAUseCasePredictionResponse();
            iAUseCasePredictionResponse.ClientUId = iAUseCasePredictionRequest.ClientUId;
            iAUseCasePredictionResponse.DeliveryConstructUId = iAUseCasePredictionRequest.DeliveryConstructUId;
            iAUseCasePredictionResponse.UseCaseId = iAUseCasePredictionRequest.UseCaseId;
            iAUseCasePredictionResponse.ReleaseUId = iAUseCasePredictionRequest.ReleaseUId;
            iAUseCasePredictionResponse.UserId = iAUseCasePredictionRequest.UserId;
            try
            {
                #region VALIDATIONS
                if (!CommonUtility.GetValidUser(iAUseCasePredictionRequest.UserId))
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);

                CommonUtility.ValidateInputFormData(iAUseCasePredictionRequest.ClientUId, "ClientUId", true);
                CommonUtility.ValidateInputFormData(iAUseCasePredictionRequest.DeliveryConstructUId, "DeliveryConstructUId", true);
                CommonUtility.ValidateInputFormData(iAUseCasePredictionRequest.UseCaseId, "UseCaseId", true);
                if (iAUseCasePredictionRequest.ReleaseUId != null)
                    iAUseCasePredictionRequest.ReleaseUId.ForEach(x => CommonUtility.ValidateInputFormData(x, "ReleaseUId", true));
                #endregion

                GetAppService();               
                iAUseCasePredictionResponse.ClientUId = iAUseCasePredictionRequest.ClientUId;
                iAUseCasePredictionResponse.DeliveryConstructUId = iAUseCasePredictionRequest.DeliveryConstructUId;
                iAUseCasePredictionResponse.UseCaseId = iAUseCasePredictionRequest.UseCaseId;
                iAUseCasePredictionResponse.ReleaseUId = iAUseCasePredictionRequest.ReleaseUId;
                iAUseCasePredictionResponse.UserId = iAUseCasePredictionRequest.UserId;
                IsPredictionEnabled(appSettings.EnablePrediction);
                return GetSuccessResponse(_iAService.GetUseCasePrediction(iAUseCasePredictionRequest));
            }
            catch (Exception ex)
            {
                iAUseCasePredictionResponse.Status = "E";
                iAUseCasePredictionResponse.Message = ex.Message;
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(IAController)
                                                              , nameof(InitiateTraining)
                                                              , ex.Message + $"   StackTrace = {ex.StackTrace}", ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(iAUseCasePredictionResponse);

            }
        }



    }
}
