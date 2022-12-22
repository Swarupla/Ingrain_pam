using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.DataModels.AICore.GenericService
{
    public class BulkTraining
    {
        public class BulkTrainingRequest
        {
            public string ClientID { get; set; }
            public string DeliveryConstructID { get; set; }
            public string ServiceID { get; set; }
            public string ApplicationID { get; set; }
            public string UsecaseID { get; set; }
            public string ModelName { get; set; }
            public string DataSource { get; set; }
            public TODataSourceDetails DataSourceDetails { get; set; }
            public TOConfigurationDetails ConfigurationDetails { get; set; }
            public string UserID { get; set; }
            public string ResponseCallBackUrl { get; set; }
        }

        public class TODataSourceDetails
        {
            public List<dynamic> ReleaseID { get; set; }
            public string StartDate { get; set; }
            public string EndDate { get; set; }
            public string ColumnList { get; set; }

        }
        public class TOConfigurationDetails
        {
            public dynamic Threshold_TopnRecords { get; set; }
            public List<string> StopWords { get; set; }

        }
        public class BulkTrainingResponse
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
        }

        public class BulkTrainingNotify
        {
            public string CorrelationId { get; set; }
            public string UniId { get; set; }
            public string pageInfo { get; set; }
            public string Status { get; set; }
            public string Message { get; set; }
        }
        public class AIServiceStatusRequest
        {
            public string CorrelationId { get; set; }
            public string UniqueId { get; set; }
            public string PageInfo { get; set; }
        }
        public class AIServiceStatusResponse
        {
            public string ModelName { get; set; }
            public string ClientId { get; set; }
            public string DeliveryconstructId { get; set; }
            public string ServiceId { get; set; }
            public string UsecaseId { get; set; }
            public string CorrelationId { get; set; }
            public string UniqueId { get; set; }            
            public string PageInfo { get; set; }            
            public string Status { get; set; }
            public string Progress { get; set; }
            public string Message { get; set; }           
            
        }
    }
}
