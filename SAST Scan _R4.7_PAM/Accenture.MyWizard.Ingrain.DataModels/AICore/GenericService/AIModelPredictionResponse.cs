using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.DataModels.AICore.GenericService
{
    public class AIModelPredictionResponse
    {
        public string CorrelationId { get; set; }
        public string UniqueId { get; set; }
        public string Status { get; set; }
        public string Progress { get; set; }
        public string Message { get; set; }
        public string PredictedData { get; set; }
        public List<string> AvailablePages { get; set; }

        public AIModelPredictionResponse(string correlationId,string uniqueId)
        {
            this.CorrelationId = correlationId;
            this.UniqueId = uniqueId;
        }
    }
    
}
