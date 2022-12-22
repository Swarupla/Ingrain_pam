using Accenture.MyWizard.Ingrain.DataModels.AICore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.WindowService.Interfaces
{
    interface IAIServicesRetrain
    {
        IngestData IngestData(string correlationId);
        bool AIServicesTraining(string correlationId);
    }
}
