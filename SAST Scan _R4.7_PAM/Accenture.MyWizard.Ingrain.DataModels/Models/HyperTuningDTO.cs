
namespace Accenture.MyWizard.Ingrain.DataModels.Models
{
    using MongoDB.Bson.Serialization.Attributes;
    using Newtonsoft.Json.Linq;
    using System.Collections.Generic;


    public class HyperTuningDTO
    {
        public string _id { get; set; }

        public string CorrelationId { get; set; }

        public string HTId { get; set; }

        public bool Temp { get; set; }

        public string VersionName { get; set; }

        [BsonElement]
        public object ModelParams { get; set; }

        public string ProblemType { get; set; }

        public string CreatedOn { get; set; }

        public string CreatedByUser { get; set; }

        public string ModifiedOn { get; set; }

        public string ModifiedByUser { get; set; }

        public string PageInfo { get; set; }

        public string ModelName { get; set; }

        public List<JObject> HyperTunedVersionData
        {
            get; set;
        }
    }

    public class HyperTuningTrainedModel : HyperTuningHeaders
    {
        public List<JObject> TrainedModel { get; set; }

        public string CorrelationId { get; set; }

        public string HTId { get; set; }

        public string UseCaseDetails { get; set; }
        public new double CPUUsage { get; set; }
    }

    public class HyperTuningHeaders : HyperTuningSystemUsage
    {
        public string DataSource { get; set; }

        public string BusinessProblem { get; set; }

        public string Status { get; set; }

        public string Progress { get; set; }

        public string Message { get; set; }

        public string ModelName { get; set; }
        public string ModelType { get; set; }
        public string Category { get; set; }
    }

    public class HyperTuningSystemUsage
    {
        public double CPUUsage { get; set; }

        public double MemoryUsageInMB { get; set; }

        public double EstimatedRunTime { get; set; }
    }
    public class HyperTuneWSParams
    {
        public string HTId { get; set; }

        public string IsHyperTuned { get; set; }
    }
}
