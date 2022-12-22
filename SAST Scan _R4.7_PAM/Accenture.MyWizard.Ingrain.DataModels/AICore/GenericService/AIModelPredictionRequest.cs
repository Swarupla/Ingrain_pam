using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.DataModels.AICore.GenericService
{
    public class AIModelPredictionRequest
    {
        public string CorrelationId { get; set; }
        public string UniqueId { get; set; }
        public string PageNumber { get; set; }
        
    }
}
