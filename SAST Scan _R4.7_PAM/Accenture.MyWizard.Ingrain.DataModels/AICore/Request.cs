using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;

namespace Accenture.MyWizard.Ingrain.DataModels.AICore
{
    public class Request
    {
        public string ClientID { get; set; }
        public string DeliveryConstructID { get; set; }
        public string ServiceID { get; set; }
        public string UserID { get; set; }
        public IFormFileCollection FileCollection { get; set; }
        public CustomParam CustomParam { get; set; }
        public string[] FileKeys { get; set; }
        public JObject Payload { get; set; }
    }
    public class RequestBulk
    {
        public string ClientID { get; set; }
        public string DeliveryConstructID { get; set; }
        public string ServiceID { get; set; }
        public string UserID { get; set; }
        public IFormFileCollection FileCollection { get; set; }
        public CustomParam CustomParam { get; set; }
        public string[] FileKeys { get; set; }
        public JObject Payload { get; set; }
        public string Bulk { get; set; }
    }

    public class CustomParam
    {
        public bool ReTrain { get; set; }
        public string CorrelationId { get; set; }
    }
}
