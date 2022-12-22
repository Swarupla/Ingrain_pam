using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.DataModels.Models
{
    public class IAPredictionRequest
    {
        public string CorrelationId { get; set; }
        public List<string> ReleaseUId { get; set; }
    }
    public class IAUseCasePredictionRequest
    {
        public string ClientUId { get; set; }
        public string DeliveryConstructUId { get; set; }
        public string UseCaseId { get; set; }
        public List<string> ReleaseUId { get; set; }
        public string UserId { get; set; }
    }

    public class IAUseCasePredictionResponse
    {
        public string ClientUId { get; set; }
        public string DeliveryConstructUId { get; set; }
        public string UseCaseId { get; set; }
        public string UserId { get; set; }
        public List<string> ReleaseUId { get; set; }
        public string UniqueId { get; set; }

        public string CorrelationId { get; set; }

        public string Message { get; set; }

        public string PredictedData { get; set; }

        public string Status { get; set; }

        public string Progress { get; set; }

       
    }
}
