using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.WindowService.Interfaces
{
    interface IQueueManagerService
    {
        int CheckPredictionsQueue(string correlationId, string requestId);
    }
}
