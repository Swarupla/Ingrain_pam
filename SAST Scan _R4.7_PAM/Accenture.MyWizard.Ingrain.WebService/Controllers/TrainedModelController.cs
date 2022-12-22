using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
using Accenture.MyWizard.Shared.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DATAMODELS = Accenture.MyWizard.Ingrain.DataModels.Models;
using Microsoft.Extensions.DependencyInjection;
using Accenture.MyWizard.Ingrain.DataModels.InferenceEngineModel;
using Accenture.MyWizard.Ingrain.DataModels.Models;

namespace Accenture.MyWizard.Ingrain.WebService.Controllers
{
    public class TrainedModelController : MyWizardControllerBase
    {

        private readonly IOptions<IngrainAppSettings> appSettings;

        public static ITrainedModelService _iITrainedModelService { get; set; }
        public TrainedModelController(IOptions<IngrainAppSettings> settings, IServiceProvider serviceProvider)
        {
            appSettings = settings;
            _iITrainedModelService = serviceProvider.GetService<ITrainedModelService>();
        }

        [HttpGet]
        [Route("api/GetSSAITrainedModels")]
        public IActionResult GetSSAITrainedModels([FromBody] ModelTemplateInput oModelTemplateInput)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(TrainedModelController), nameof(GetSSAITrainedModels), "GetSSAITrainedModels", string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                dynamic oResponse = _iITrainedModelService.GetTrainedModels(oModelTemplateInput, "SSAI");
                return GetSuccessResponse(oResponse);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(TrainedModelController), nameof(GetSSAITrainedModels), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
        }

        [HttpGet]
        [Route("api/GetAITrainedModels")]
        public IActionResult GetAITrainedModels([FromBody] ModelTemplateInput oModelTemplateInput)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(TrainedModelController), nameof(GetAITrainedModels), "GetAITrainedModels", string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                return Ok(_iITrainedModelService.GetTrainedModels(oModelTemplateInput, "AI"));
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(TrainedModelController), nameof(GetAITrainedModels), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
        }
       
    }
}
