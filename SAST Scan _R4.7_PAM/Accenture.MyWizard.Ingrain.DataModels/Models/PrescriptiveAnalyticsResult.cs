using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;

namespace Accenture.MyWizard.Ingrain.DataModels.Models
{
    [BsonIgnoreExtraElements]
    public class PrescriptiveAnalyticsResult
    {
        public JObject TeachtestData { get; set; }
        public string Status { get; set; }
        public string Progress { get; set; }
        public string Message { get; set; }
        public string ModelType { get; set; }
        public string WFId { get; set; }
        public string CorrelationId { get; set; }
        public string PageInfo { get; set; }
        public string UserId { get; set; }
        public string desired_value { get; set; }


    }
}
