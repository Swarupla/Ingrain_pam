using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Accenture.MyWizard.Ingrain.DataModels.AICore
{
    [BsonIgnoreExtraElements]
    public class AIServiceRequestStatus
    {       
        public string CorrelationId { get; set; }
        public string ServiceId { get; set; }
        public string UniId { get; set; }
        public string PageInfo { get; set; }

        public string TeamAreaUID { get; set; }
        public string ClientId { get; set; }
        public string DeliveryconstructId { get; set; }
        public string Status { get; set; }
        public string ModelName { get; set; }
        public string RequestStatus { get; set; }
        public int? PyCallCount { get; set; }
        public string Message { get; set; }
        public string Progress { get; set; }
        public string Flag { get; set; }
        public string[] ColumnNames { get; set; }
        public List<dynamic> SelectedColumnNames { get; set; }
        public string DataSetUId { get; set; }
        public string SourceDetails { get; set; }
        public string SourceName { get; set; }
        public string DataSource { get; set; }
        public string CreatedByUser { get; set; }
        public string CreatedOn { get; set; }
        public string ModifiedByUser { get; set; }
        public string ModifiedOn { get; set; }
        public JObject FilterAttribute { get; set; }

        public string Language { get; set; }


        public string ResponsecallbackUrl { get; set; }

        public string UsecaseId { get; set; }

        public string ApplicationId { get; set; }
        public JObject Payload { get; set; }
        public string baseUrl { get; set; }
        public string apiPath { get; set; }
        public string token { get; set; }
        public string ScoreUniqueName { get; set; }
        public dynamic Threshold_TopnRecords { get; set; }
        public List<string> StopWords { get; set; }
        public int MaxDataPull { get; set; }
        public string[] UniqueColumns { get; set; }
    }
}
