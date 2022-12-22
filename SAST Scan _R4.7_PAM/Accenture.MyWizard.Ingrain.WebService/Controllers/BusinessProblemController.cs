
namespace Accenture.MyWizard.Ingrain
{
    #region Namespace References
    using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
    using Accenture.MyWizard.Ingrain.DataModels.Models;
    using Accenture.MyWizard.Ingrain.WebService;
    using System;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.DependencyInjection;
    using Accenture.MyWizard.Ingrain.BusinessDomain.Common;
    using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
    #endregion

    public class BusinessProblemController : MyWizardControllerBase
    {

        #region Members
        private BusinessProblemDataDTO _businessService = null;
        public static IBusinessService businessService { set; get; }
        #endregion

        #region Constructor
        public BusinessProblemController(IServiceProvider serviceProvider)
        {
            _businessService = new BusinessProblemDataDTO();
            businessService = serviceProvider.GetService<IBusinessService>();
        }
        #endregion

        #region Methods
        [Route("api/SaveBusinessData")]
        [HttpPost]
        public IActionResult SaveBusinessData([FromBody]dynamic requestBody)
        {            
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(BusinessProblemController),nameof(SaveBusinessData), "Start", string.Empty, string.Empty, string.Empty, string.Empty);
            string businessProblemBodyData = Convert.ToString(requestBody);
            try
            {
                if (!string.IsNullOrEmpty(businessProblemBodyData))
                {
                    _businessService = Newtonsoft.Json.JsonConvert.DeserializeObject<BusinessProblemDataDTO>(businessProblemBodyData);
                    if (!CommonUtility.GetValidUser(_businessService.CreatedByUser))
                    {
                        return GetFaultResponse(Resource.IngrainResx.InValidUser);
                    }
                    else if (!CommonUtility.GetValidUser(_businessService.ModifiedByUser))
                    {
                        return GetFaultResponse(Resource.IngrainResx.InValidUser);
                    }

                    if (!CommonUtility.IsValidGuid(_businessService.DeliveryConstructUID))
                    {
                        return GetFaultResponse(string.Format(CONSTANTS.InValidGUID, "DeliveryConstructUID"));
                    }
                    if (!CommonUtility.IsValidGuid(_businessService.AppId))
                    {
                        return GetFaultResponse(string.Format(CONSTANTS.InValidGUID, "AppId"));
                    }
                    if (!CommonUtility.IsValidGuid(_businessService.ClientUId))
                    {
                        return GetFaultResponse(string.Format(CONSTANTS.InValidGUID, "ClientUId"));
                    }
                    if (!CommonUtility.IsValidGuid(_businessService.ParentCorrelationId))
                    {
                        return GetFaultResponse(string.Format(CONSTANTS.InValidGUID, "ParentCorrelationId"));
                    }
                    if (!CommonUtility.IsValidGuid(_businessService.CorrelationId))
                    {
                        return GetFaultResponse(string.Format(CONSTANTS.InValidGUID, "CorrelationId"));
                    }

                    if (!CommonUtility.IsDataValid(_businessService.TargetColumn))
                    {
                        return GetFaultResponse(string.Format(CONSTANTS.InValidData, "TargetColumn"));
                    }
                    if (!CommonUtility.IsDataValid(_businessService.TimePeriod))
                    {
                        return GetFaultResponse(string.Format(CONSTANTS.InValidData, "TimePeriod"));
                    }
                    if (!CommonUtility.IsDataValid(_businessService.TargetUniqueIdentifier))
                    {
                        return GetFaultResponse(string.Format(CONSTANTS.InValidData, "TargetUniqueIdentifier"));
                    }
                    if (!CommonUtility.IsDataValid(_businessService.ProblemType))
                    {
                        return GetFaultResponse(string.Format(CONSTANTS.InValidData, "ProblemType"));
                    }
                    if (!CommonUtility.IsDataValid(_businessService.CreatedOn))
                    {
                        return GetFaultResponse(string.Format(CONSTANTS.InValidData, "CreatedOn"));
                    }
                    if (!CommonUtility.IsDataValid(_businessService.ModifiedOn))
                    {
                        return GetFaultResponse(string.Format(CONSTANTS.InValidData, "ModifiedOn"));
                    }
                    if (!CommonUtility.IsDataValid(Convert.ToString(_businessService.IsCustomColumnSelected)))
                    {
                        return GetFaultResponse(string.Format(CONSTANTS.InValidData, "IsCustomColumnSelected"));
                    }
                    _businessService._id = Guid.NewGuid().ToString();
                    _businessService.CorrelationId = Guid.NewGuid().ToString();
                    businessService.InsertData(_businessService);
                }
                else
                {
                    return GetFaultResponse(Resource.IngrainResx.EmptyData);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(BusinessProblemController), nameof(SaveBusinessData),ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                //return BadRequest(new { respone = Resource.IngrainResx.FailedStatusCode });
                return GetFaultResponse(ex.Message);
            }            
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(BusinessProblemController),nameof(SaveBusinessData), "End", string.Empty, string.Empty, string.Empty, string.Empty);
            return Ok(new { response = Resource.IngrainResx.Created });
        }

        [Route("api/GetBusinessProblemData")]
        [HttpGet]
        public IActionResult GetBusinessProblemData(string correlationId)
        {            
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(BusinessProblemController),nameof(GetBusinessProblemData), "Start", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            string businessData = string.Empty;
            try
            {
                if (!string.IsNullOrEmpty(correlationId))
                {
                    businessData = businessService.GetBusinessProblemData(correlationId);
                    if (string.IsNullOrEmpty(businessData))
                    {
                        return NotFound(new { response = Resource.IngrainResx.EmptyData });
                    }
                }
                else
                {
                    return NotFound(new { response = Resource.IngrainResx.CorrelatioUIDEmpty });
                }
            }
            catch (Exception ex)
            {                
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(BusinessProblemController), nameof(GetBusinessProblemData),ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return BadRequest(new { respone = Resource.IngrainResx.FailedStatusCode });
            }
            return Ok(businessData);
        }

        #endregion

    }
}
