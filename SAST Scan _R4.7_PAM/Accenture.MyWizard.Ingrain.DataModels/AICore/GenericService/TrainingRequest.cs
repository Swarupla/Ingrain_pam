using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.DataModels.AICore.GenericService
{
    public class TrainingRequest
    {
        public string ApplicationId { get; set; }
        public string ClientId { get; set; }
        public string DeliveryConstructId { get; set; }
        public string ServiceId { get; set; }
        public string UsecaseId { get; set; }
        public string ModelName { get; set; }
        public string UserId { get; set; }
        public string DataSource { get; set; }
        public dynamic DataSourceDetails { get; set; }
        public string DataSetUId { get; set; }
        public string ResponseCallBackUrl { get; set; }
        public string CustomDataSourceDetails { get; set; }
        public string TeamAreaUID { get; set; }
        public string CorrelationId { get; set; }
    }


}
