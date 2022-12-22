using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
using Accenture.MyWizard.Shared.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Accenture.MyWizard.Ingrain.WebService.Controllers
{
    public class WorkerServiceController : MyWizardControllerBase
    {

        private readonly IOptions<IngrainAppSettings> appSettings;

        public static IWorkerService _iWorkerService { get; set; }
        public WorkerServiceController(IOptions<IngrainAppSettings> settings, IServiceProvider serviceProvider)
        {
            appSettings = settings;
            _iWorkerService = serviceProvider.GetService<IWorkerService>();
        }
        /// <summary>
        /// GetWindowServiceStatus 
        /// </summary>
        /// <returns>WS Status - RUNNING/STOPPED </returns>
        [Route("api/GetWindowServiceStatus")]
        [HttpGet]
        public IActionResult GetWindowServiceStatus()
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(WorkerServiceController), nameof(GetWindowServiceStatus), "Start", string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                return Ok(_iWorkerService.GetWorkerServiceStatus());
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(WorkerServiceController), nameof(GetWindowServiceStatus), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return BadRequest(ex.Message + ex.StackTrace);
            }
        }

    }
}
