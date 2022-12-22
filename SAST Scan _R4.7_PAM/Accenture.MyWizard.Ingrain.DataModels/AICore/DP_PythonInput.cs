using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.DataModels.AICore
{
    public class DP_PythonInput
    {
        public string CorrelationId { get; set; }
        public string PageInfo { get; set; }
        public string UserId { get; set; }
        public string UniqueId { get; set; }
        public string Params { get; set; }
    }
    public class BodyParams
    {
        public string ClientId { get; set; }
        public string DeliveryConstructId { get; set; }
        public string WorkItemExternalId { get; set; }
        public string WorkItemType { get; set; }
    }






}
