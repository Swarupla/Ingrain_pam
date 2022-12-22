using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.DataModels.AICore.GenericService
{
    public class PredictionResponse
    {
        public string ApplicationId { get; set; }
        public string ClientId { get; set; }
        public string DeliveryConstructId { get; set; }
        public string ServiceId { get; set; }
        public string UsecaseId { get; set; }
        public string ModelName { get; set; }
        public string UserId { get; set; }
        public string CorrelationId { get; set; }
        public string ModelStatus { get; set; }
        public string StatusMessage { get; set; }
        public dynamic Response { get; set; }
    }
}
