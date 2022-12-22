using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.DataModels.AICore.GenericService
{
    public class BulkPrediction
    {
        public class BulkPredictionResponse
        {
            public string ApplicationId { get; set; }
            public string ClientId { get; set; }
            public string DeliveryConstructId { get; set; }
            public string ServiceId { get; set; }
            public string UsecaseId { get; set; }
            public string ModelName { get; set; }
            public string UserId { get; set; }
            public string CorrelationId { get; set; }
            public string UniqueId { get; set; }
            public string ModelStatus { get; set; }
            public string StatusMessage { get; set; }
            public dynamic PredictionData{ get; set; }
        }
        public class BulkPredictionData
        {
            
            public string CorrelationId { get; set; }
            public string InputData { get; set; }
            public string Status { get; set; }
            public string StatusMessage { get; set; }
            public string Progress { get; set; }
            public int Page_number { get; set; }
            public int Total_pages { get; set; }
            public int TotalRecordCount { get; set; }
            
            public string PageInfo { get; set; }
            public dynamic UniqueColumn { get; set; }
        }
    }
}
