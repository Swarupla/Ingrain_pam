using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;

namespace Accenture.MyWizard.Ingrain.DataModels.Models
{
    [BsonIgnoreExtraElements]
    public class UserColumns
    {
        public string[] ColumnsList { get; set; }
        public string TargetColumn { get; set; }
        public string[] InputColumns { get; set; }
        public Dictionary<string, string> DataTypeColumns { get; set; }
        public object Frequency { get; set; }
        public object DataType { get; set; }
        public string ModelName { get; set; }
        public string DataSource { get; set; }
        public string BusinessProblems { get; set; }
        public string[] AvailableColumns { get; set; }
        public string TimeSeriesColumn { get; set; }
        public string TargetUniqueIdentifier { get; set; }
        public bool IsEntityModel { get; set; }
        public string Aggregation { get; set; }
        public JObject FrequencyList { get; set; }
        public BsonDocument TimeSeries { get; set; }

        public bool IsModelTrained { get; set; }

        public bool IsModelDeployed { get; set; }
        public string InstaId { get; set; }
        public bool InstaFLag { get; set; }

        public string Category { get; set; }

        public JObject UniquenessDetails { get; set; }

        public JObject ValidRecordsDetails { get; set; }
        public bool IsCascadeModel { get; set; }
        public bool IsIncludedinCustomCascade { get; set; }
        public int CascadeModelsCount { get; set; }
        public string CustomCascadeId { get; set; }
        public string PreviousModelName { get; set; }
        public string PreviousTargetColumn { get; set; }
        public bool IsFMModel { get; set; }        
        public bool IsModelTemplateDataSource { get; set; }
        public string CustomDataPullType { get; set; }
    }   
    public class Frequency
    {
        public string Name { get; set; }
        public string Steps { get; set; }
    }
    public class CommonAttributes
    {
        public string ModelName { get; set; }
        public string DataSource { get; set; }
        public string ProblemType { get; set; }
        public string BusinessProblems { get; set; }
        public string InstaId { get; set; }
        public string Category { get; set; }
    }
    [BsonIgnoreExtraElements]
    public class ProblemTypeDetails2
    {
        public string ModelType { get; set; }
        public string AppId { get; set; }
        public string[]LinkedApps { get; set; }
        public string CorrelationId { get; set; }
        public string FMModel1CorId { get; set; }
    }

}

