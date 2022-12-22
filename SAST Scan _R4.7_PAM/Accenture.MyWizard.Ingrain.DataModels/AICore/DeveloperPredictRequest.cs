using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.DataModels.AICore
{
    public class DeveloperPredictRequest
    {
        public string ClientId { get; set; }
        public string DeliveryConstructId { get; set; }
        public string ApplicationId { get; set; }
        public string ServiceId { get; set; }
        public string UsecaseId { get; set; }
        public string WorkItemExternalId { get; set; }
        public string WorkItemType { get; set; }
        public string UserId { get; set; }
    }
}
