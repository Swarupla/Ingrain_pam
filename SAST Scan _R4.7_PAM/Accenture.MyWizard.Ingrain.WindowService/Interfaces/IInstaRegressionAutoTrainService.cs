using Accenture.MyWizard.Ingrain.DataModels.Models;
using Accenture.MyWizard.SelfServiceAI.WindowService.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.WindowService.Interfaces
{
    interface IInstaRegressionAutoTrainService
    {
        RegressionRetrain StartRegressionModelTraining(List<DeployModelsDto> deployModel);        
    }
}
