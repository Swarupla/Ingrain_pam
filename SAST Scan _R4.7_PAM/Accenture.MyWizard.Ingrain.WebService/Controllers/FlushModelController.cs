using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
using Accenture.MyWizard.Ingrain.WebService;
using Microsoft.AspNetCore.Mvc;
using System;
using Microsoft.Extensions.DependencyInjection;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
using Accenture.MyWizard.Ingrain.DataModels.Models;
using Accenture.MyWizard.Ingrain.BusinessDomain.Common;

namespace Accenture.MyWizard.Ingrain
{
    public class FlushModelController : MyWizardControllerBase
    {
        public static IFlushService _iFlushService { set; get; }

        #region Constructors
        public FlushModelController(IServiceProvider serviceProvider)
        {
            _iFlushService = serviceProvider.GetService<IFlushService>();
        }
        #endregion

        #region Methods 
        /// <summary>
        /// Flush the data based on correlation id
        /// </summary>
        /// <param name="correlationId"></param>
        /// <returns>flush status</returns>
        [HttpGet]
        [Route("api/FlushModel")]
        public IActionResult FlushModel(string correlationId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(FlushModelController), nameof(FlushModel), "Start", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                if (!string.IsNullOrEmpty(correlationId))
                {
                    string flushFlag = string.Empty;                    
                    var userid = this.User.Identity.Name;
                    //Imp do not remove this log
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(FlushModelController), nameof(FlushModel), "--USERID--" + userid, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                    string userrole = _iFlushService.userRole(correlationId, userid);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(FlushModelController), nameof(FlushModel), "--USERROLE--" + userrole, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                    if (userrole == "Valid")
                    {
                        string flushStatus = _iFlushService.FlushModel(correlationId, flushFlag);
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
                    return NotFound(Resource.IngrainResx.InputData);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(FlushModelController), nameof(FlushModel), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
        }


        /// <summary>
        /// Flush all the data based on correlation id
        /// </summary>
        /// <param name="clientuid,deliveryconstructuid,userid"></param>
        /// <returns>flush status</returns>
        [HttpGet]
        [Route("api/FlushAllModelsOfClient")]
        public IActionResult FlushAllModelsOfClient(string clientuid, string deliveryconstructuid = null, string userid = null)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(FlushModelController), nameof(FlushModel), "Start", string.Empty, string.Empty, clientuid, null);
            try
            {
                if (!CommonUtility.GetValidUser(userid))
                {
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);
                }
                if (!string.IsNullOrEmpty(clientuid))
                {
                    string flushFlag = string.Empty;
                    string flushStatus = _iFlushService.FlushAllModels(clientuid, deliveryconstructuid, userid, flushFlag);

                    if (!string.IsNullOrEmpty(flushStatus))
                        return GetSuccessResponse(flushStatus);
                    else
                        return GetSuccessResponse("");
                }
                else
                {
                    return NotFound(Resource.IngrainResx.InputData);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(FlushModelController), nameof(FlushModel), ex.Message, ex, string.Empty, string.Empty, clientuid, deliveryconstructuid);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }

        }


        [HttpPost]
        [Route("api/FlushDBByCorrelationIds")]
        public IActionResult FlushDBByCorrelationIds([FromBody] corrIds correlationIds)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(FlushModelController), nameof(FlushDBByCorrelationIds), "Start", string.Empty, string.Empty, string.Empty, string.Empty);
            try
            { 
                if (correlationIds.CorrelationId.Length > 0)
                {
                    string flushStatus = _iFlushService.DeleteCorrelationIds(correlationIds.CorrelationId);
                    if (!string.IsNullOrEmpty(flushStatus))
                        return GetSuccessResponse(flushStatus);
                    else
                        return GetSuccessResponse("");
                }
                else
                {
                    return NotFound(Resource.IngrainResx.InputData);
                }                
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(FlushModelController), nameof(FlushDBByCorrelationIds), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }            
        }

        [HttpPost]
        [Route("api/FlushDBByDateRange")]
        public IActionResult FlushDBByDateRange([FromBody]dateRange date)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(FlushModelController), nameof(FlushDBByDateRange), "Start", string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                if (!String.IsNullOrEmpty(date.StartDate) && !String.IsNullOrEmpty(date.EndDate))
                {
                    if (_iFlushService.Validate(date.StartDate) == CONSTANTS.Invalid || _iFlushService.Validate(date.EndDate) == CONSTANTS.Invalid)
                    {
                        return GetSuccessResponse(CONSTANTS.InvalidDateFormat);
                    }
                    if (Convert.ToDateTime(date.EndDate) < Convert.ToDateTime(date.StartDate))
                    {
                        return GetSuccessResponse(CONSTANTS.EndDateGreaterThanStartDate);
                    }

                    #region VALIDATIONS
                    CommonUtility.ValidateInputFormData(Convert.ToString(date.StartDate), "StartDate", false);
                    CommonUtility.ValidateInputFormData(Convert.ToString(date.EndDate), "EndDate", false);
                    #endregion
                    string flushStatus = _iFlushService.DeleteDateRange(date.StartDate, date.EndDate);

                    if (!string.IsNullOrEmpty(flushStatus))
                        return GetSuccessResponse(flushStatus);
                    else
                        return GetSuccessResponse("");
                }
                else
                {
                    return NotFound(Resource.IngrainResx.InputData);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(FlushModelController), nameof(FlushDBByDateRange), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }           
        }

        [HttpPost]
        [Route("api/FlushDBByClientAndDC")]
        public IActionResult FlushDBByClientAndDC([FromBody] clientDCs clientDC)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(FlushModelController), nameof(FlushDBByClientAndDC), "Start", string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                if (clientDC.ClientUId.Length > 0 && clientDC.DCId.Length > 0)
                {
                    string flushStatus = _iFlushService.DeleteClientDCIds(clientDC.ClientUId, clientDC.DCId);
                    if (!string.IsNullOrEmpty(flushStatus))
                        return GetSuccessResponse(flushStatus);
                    else
                        return GetSuccessResponse("");
                }
                else
                {
                    return NotFound(Resource.IngrainResx.InputData);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(FlushModelController), nameof(FlushDBByClientAndDC), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }            
        }

        [HttpPost]
        [Route("api/FlushDBByDateAndClientDCs")]
        public IActionResult FlushDBByDateAndClientDCs([FromBody] dateClientDCs dateClientDC)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(FlushModelController), nameof(FlushDBByDateAndClientDCs), "Start", string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                if (!string.IsNullOrEmpty(dateClientDC.Date) && dateClientDC.ClientUId.Length > 0 && dateClientDC.DCId.Length > 0)
                {
                    if (_iFlushService.Validate(dateClientDC.Date) == CONSTANTS.Invalid)
                    {
                        return GetSuccessResponse(CONSTANTS.InvalidDateFormat);
                    }
                    CommonUtility.ValidateInputFormData(dateClientDC.Date, "Date", false);

                    string flushStatus = _iFlushService.DeleteDateClientDCIds(dateClientDC.Date, dateClientDC.ClientUId, dateClientDC.DCId);
                    if (!string.IsNullOrEmpty(flushStatus))
                        return GetSuccessResponse(flushStatus);
                    else
                        return GetSuccessResponse("");
                }
                else
                {
                    return NotFound(Resource.IngrainResx.InputData);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(FlushModelController), nameof(FlushDBByDateAndClientDCs), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message );
            }
        }

        #endregion 
    }
}
