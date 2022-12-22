namespace Accenture.MyWizard.Ingrain
{
    #region Namespace References
    using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
    using Accenture.MyWizard.Ingrain.WebService;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Collections.Generic;
    using LOGGING = Accenture.MyWizard.LOGGING;
    using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
    using Microsoft.Extensions.DependencyInjection;
    using System.Threading.Tasks;
    using Accenture.MyWizard.Ingrain.DataModels.Models;

    #endregion
    public class CAMController : MyWizardControllerBase
    {
        #region Members      
        public static ICAMService camService { set; get; }
        #endregion

        #region Constructor
        public CAMController(IServiceProvider serviceProvider)
        {
            camService = serviceProvider.GetService<ICAMService>();
       
        }
        #endregion

        #region Methods 
        /// <summary>
        /// Clone the data
        /// </summary>
        /// <param name="correlationId"></param>
        /// <returns></returns>
        //[AcceptVerbs("Get", "Post")]
        [HttpPost]
        [Route("api/ATRProvision")]
        public async Task<IActionResult> ATRProvision(ATRProvisionRequestDto atr)
        {
            string response = null;
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CAMController), nameof(ATRProvision), "START", string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                response = await camService.PushATRProvisionToDBAsync(atr);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(CAMController), nameof(ATRProvision), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CAMController), nameof(ATRProvision), "END", string.Empty, string.Empty, string.Empty, string.Empty);
            return Ok(response);
        }
    }
    #endregion
}

