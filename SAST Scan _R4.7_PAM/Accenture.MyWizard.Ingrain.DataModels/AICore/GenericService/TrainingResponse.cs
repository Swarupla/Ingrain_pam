using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Text;

namespace Accenture.MyWizard.Ingrain.DataModels.AICore.GenericService
{
    public class TrainingResponse
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
        public string Response { get; set; }

        public string TeamAreaUID { get; set; }
        public TrainingResponse(string clientId,
            string deliveryConstructId,
            string serviceId,
            string usecaseId,            
            string modelName,
            string userId)
        {
            ClientId = clientId;
            DeliveryConstructId = deliveryConstructId;
            ServiceId = serviceId;
            UsecaseId = usecaseId;
            ModelName = modelName;
            UserId = userId;

        }

        public TrainingResponse(string clientId,
          string deliveryConstructId,
          string serviceId,
          string usecaseId,
          string modelName,
          string userId,
          string teamAreaUID,
          string correlationId)
        {
            ClientId = clientId;
            DeliveryConstructId = deliveryConstructId;
            ServiceId = serviceId;
            UsecaseId = usecaseId;
            ModelName = modelName;
            UserId = userId;
            TeamAreaUID = teamAreaUID;
            CorrelationId = correlationId;
        }
    }
}

