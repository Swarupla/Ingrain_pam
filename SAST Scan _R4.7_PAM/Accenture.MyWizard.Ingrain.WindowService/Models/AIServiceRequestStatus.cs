using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.WindowService.Models
{
    [BsonIgnoreExtraElements]
    public class AIRequestStatus
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string _id { get; set; }
        public string CorrelationId { get; set; }
        public string ServiceId { get; set; }
        public string UniId { get; set; }
        public string PageInfo { get; set; }
        public string ClientId { get; set; }
        public string DeliveryconstructId { get; set; }
        public string Status { get; set; }
        public string ModelName { get; set; }
        public string RequestStatus { get; set; }
        public string Message { get; set; }
        public string Progress { get; set; }
        public string Flag { get; set; }
        public string[] ColumnNames { get; set; }
        public List<dynamic> SelectedColumnNames { get; set; }
        public string SourceDetails { get; set; }
        public string DataSource { get; set; }
        public string CreatedByUser { get; set; }
        public string CreatedOn { get; set; }
        public string ModifiedByUser { get; set; }
        public string ModifiedOn { get; set; }
    }
    public class ClusteringStatus
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string _id { get; set; }
        public string CorrelationId { get; set; }
        public string ModelName { get; set; }
        public string ModelType { get; set; }
        public string Message { get; set; }
        public string UniId { get; set; }
        public string ServiceID { get; set; }
        public string Status { get; set; }
        public string Progress { get; set; }
        public string Flag { get; set; }
        public string pageInfo { get; set; }
        public string ProblemType { get; set; }
        public string RequestStatus { get; set; }
        public string Clustering_type { get; set; }
        public bool retrain { get; set; }
        public string ClientID { get; set; }
        public string DCUID { get; set; }
        public string DataSource { get; set; }
        public string CreatedByUser { get; set; }
        public string CreatedOn { get; set; }
        public string ModifiedByUser { get; set; }
        public string ModifiedOn { get; set; }

    }

    public class ClusteringAPIModel
    {
        [BsonId]
        public string _id { get; set; }
        public string ClientID { get; set; }
        public string DCUID { get; set; }
        public string ServiceID { get; set; }
        public string UserId { get; set; }
        public string ParamArgs { get; set; }
        public string CorrelationId { get; set; }
        public string PageInfo { get; set; }
        public string UniId { get; set; }

        public string Token { get; set; }
        public JObject ProblemType { get; set; }
        public JObject SelectedModels { get; set; }
        public string ModelName { get; set; }

        public string CreatedOn { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedOn { get; set; }
        public string ModifiedBy { get; set; }

        public bool retrain { get; set; }
        public bool DBEncryptionRequired { get; set; }

        public List<dynamic> Columnsselectedbyuser { get; set; }

        public string DataSource { get; set; }
        public string Language { get; set; }

    }
}
