using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Accenture.MyWizard.Ingrain.DataModels.Models
{
    [BsonIgnoreExtraElements]
    public class VDSModelDTO
    {
        public string CorrelationId { get; set; }
        public string EntityName { get; set; }
        public string ModelName { get; set; }
        public string ModelType { get; set; }
        public string CreatedDateTime { get; set; }
        public string LastModifiedDateTime { get; set; }
        public string ClientID { get; set; }
        public string DCID { get; set; }
        public string WebServiceURL { get; set; }
        public string TargetIdentifier { get; set; }
        public string TargetName { get; set; }
        public JObject DataType { get; set; }
        public JObject UnitDateFormat { get; set; }
        public Dictionary<string,string> DataRoleScale { get; set; }
        public JObject ValidValues { get; set; }

        public string TimeSeriesColumn { get; set; }
    }

    public class VdsUseCaseDto
    {
        public string UseCaseId { get; set; }
        public string EntityName { get; set; }
        public string UseCaseName { get; set; }
        public string Description { get; set; }
        public string FunctionalArea { get; set; }
        public string ModelType { get; set; }
        public string ProblemType { get; set; }
        public string CreatedDateTime { get; set; }
        public string LastModifiedDateTime { get; set; }
        public string ClientUId { get; set; }
        public string DeliveryConstructUId { get; set; }
        public string WebServiceURL { get; set; }
        public string TargetIdentifier { get; set; }
        public string TargetName { get; set; }
        public string DateColumn { get; set; }
        public string TargetAggregate { get; set; }
        public List<string> Frequency { get; set; }
        public JObject DataType { get; set; }
        public JObject UnitDateFormat { get; set; }
        public Dictionary<string, string> DataRoleScale { get; set; }
        public JObject ValidValues { get; set; }
    }
}
