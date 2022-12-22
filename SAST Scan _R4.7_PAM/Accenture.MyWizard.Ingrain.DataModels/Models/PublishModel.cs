using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Accenture.MyWizard.Ingrain.DataModels.Models
{
    [BsonIgnoreExtraElements]
    public class PublishModelDTO
    {
        public string _id { get; set; }
        public string ModelVersion { get; set; }
        public string ModelName { get; set; }
        public string ModelURL { get; set; }
        public double Accuracy { get; set; }
        public string LinkedApps { get; set; }
        public string Status { get; set; }
        public string FinalModel { get; set; }
        public string WebServices { get; set; }
        public string CorrelationId { get; set; }
        public string DeliveryConstructUID { get; set; }
        public int AppId { get; set; }
        public bool IsPrivate { get; set; }
        public string InputSample { get; set; }
        public string ClientUId { get; set; }
        public string CreatedOn { get; set; }
        public string CreatedByUser { get; set; }
        public string ModifiedOn { get; set; }
        public string ModifiedByUser { get; set; }
        public string SourceName { get; set; }

        public string DataSource { get; set; }

        public string ModelHealth { get; set; }

        public string DeployedDate { get; set; }

        public bool IsModelTemplate { get; set; }

        public bool IsReadOnlyAccess { get; set; }
        public bool IsCascadeModel { get; set; }
        public bool IsIncludedInCascade { get; set; }
        public string Category { get; set; }
        public bool IsFMModel { get; set; }
        public bool HideFMModel { get; set; }

    }
    [BsonIgnoreExtraElements]
    public class PublishDeployedModel
    {
        public string _id { get; set; }
        public string ModelVersion { get; set; }
        public string ModelName { get; set; }
        public string ModelURL { get; set; }
        public double Accuracy { get; set; }
        public string[] LinkedApps { get; set; }
        public string LinkedApp { get; set; }
        public string Status { get; set; }
        public string FinalModel { get; set; }
        public string WebServices { get; set; }
        public string CorrelationId { get; set; }
        public string DeliveryConstructUID { get; set; }
        public string AppId { get; set; }
        public bool IsPrivate { get; set; }
        public string InputSample { get; set; }
        public string ClientUId { get; set; }
        public string CreatedOn { get; set; }
        public string CreatedByUser { get; set; }
        public string ModifiedOn { get; set; }
        public string ModifiedByUser { get; set; }
        public string SourceName { get; set; }

        public string DataSource { get; set; }

        public string ModelHealth { get; set; }

        public string DeployedDate { get; set; }

        public bool IsModelTemplate { get; set; }

        public bool IsReadOnlyAccess { get; set; }
        public bool IsCascadeModel { get; set; }
        public bool IsIncludedInCascade { get; set; }
        public string Category { get; set; }
        public bool IsFMModel { get; set; }
        public bool HideFMModel { get; set; }
        public string FMCorrelationId { get; set; }
        public RequestStatus FmModelStaus { get; set; }

    }
    public class RequestStatus
    {
        public string Status { get; set; }
        public string Progress { get; set; }
    }
}
