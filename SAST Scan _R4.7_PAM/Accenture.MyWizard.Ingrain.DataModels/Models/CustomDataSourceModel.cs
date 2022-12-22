using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.DataModels.Models
{
    #region - Custom Data Source - Query
    public class CustomDataInputParams
    {
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string SourceType { get; set; }
        public dynamic Data { get; set; }
    }
    public class QueryDTO
    {
        public string Type { get; set; }
        public dynamic Query { get; set; }
        public string DateColumn { get; set; }
    }
    public class Datecolumnlst
    {
        public string Name { get; set; }
        public string Type { get; set; }
    }
    
    public class CustomQueryParamArgs
    {
        public string CorrelationId { get; set; }
        public string ClientUID { get; set; }
        public string E2EUID { get; set; }
        public string DeliveryConstructUId { get; set; }
        public ParentFile Parent { get; set; }
        public string Flag { get; set; }
        public string mapping { get; set; }
        public string mapping_flag { get; set; }
        public string pad { get; set; }
        public string metric { get; set; }
        public string InstaMl { get; set; }
        public Filepath fileupload { get; set; }
        public string Customdetails { get; set; }
        public string CustomSource { get; set; }
    }
    public class CustomDataSourceModel
    {
        [BsonId]
        public string _id { get; set; }
        public string CorrelationId { get; set; }
        public string CustomDataPullType { get; set; }
        public string CustomSourceDetails { get; set; }
        public string CreatedByUser { get; set; }
        public string CreatedOn { get; set; }
        public string ModifiedByUser { get; set; }
        public string ModifiedOn { get; set; }
    }
    //public class CustomDataSourceDTO
    //{
    //    public dynamic Query { get; set; }
    //    public JObject Data { get; set; }
    //}
    #endregion

    #region - Custom Data Source - APi
    public class CustomInputData
    {
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public bool DbEncryption { get; set; }
        public ApiDTO Data { get; set; }
    }

    public class ApiDTO
    {
        public string Type { get; set; }
        public string MethodType { get; set; }
        public string ApiUrl { get; set; }
        public dynamic KeyValues { get; set; }
        public dynamic BodyParam { get; set; }
        public string fetchType { get; set; }
        public string TargetNode { get; set; }
        public AuthenticationDTO Authentication { get; set; }
    }

    public class AuthenticationDTO
    {
        public string Type { get; set; }
        public bool UseIngrainAzureCredentials { get; set; }
        public string Token { get; set; }
        public string AzureUrl { get; set; }
        public AzureDetails AzureCredentials { get; set; }
    }

    public class AzureDetails
    {
        public string client_id { get; set; }
        public string client_secret { get; set; }
        public string resource { get; set; }
        public string grant_type { get; set; }
    }

    public class CustomSourceDTO
    {
        private string _EntitiesName = "null";
        private string _MetricNames = "null";

        public string CorrelationId { get; set; }
        public string ClientUID { get; set; }
        public string E2EUID { get; set; }
        public string DeliveryConstructUId { get; set; }
        public ParentFile Parent { get; set; }
        public string Flag { get; set; }
        public string mapping { get; set; }
        public string mapping_flag { get; set; }
        public string pad { get; set; }
        public string metric { get; set; }
        public string InstaMl { get; set; }
        public Filepath fileupload { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string Customdetails { get; set; }
        public string CustomSource { get; set; }
        public string EntitiesName { get => _EntitiesName; set => _EntitiesName = value; }
        public string MetricNames { get => _MetricNames; set => _MetricNames = value; }
        public string TargetNode { get; set; }
    }
}
#endregion