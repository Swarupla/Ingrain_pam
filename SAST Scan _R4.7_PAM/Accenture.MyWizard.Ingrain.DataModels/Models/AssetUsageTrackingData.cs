namespace Accenture.MyWizard.Ingrain.DataModels.Models
{
    using MongoDB.Bson.Serialization.Attributes;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class AssetUsageTrackingData
    {
        [BsonElement("UserUniqueId")]
        public string UserUniqueId { get; set; }

        [BsonElement("ApplicationName")]
        public string ApplicationName { get; set; }

        [BsonElement("Description")]
        public string Description { get; set; }

        [BsonElement("ClientUId")]
        public Guid ClientUId { get; set; }

        [BsonElement("DeliveryConstructUId")]
        public Guid DeliveryConstructUId { get; set; }

        [BsonElement("FeatureName")]
        public string FeatureName { get; set; }

        [BsonElement("SubFeatureName")]
        public string SubFeatureName { get; set; }

        [BsonElement("UserName")]
        public string UserName { get; set; }

        [BsonElement("AppServiceUId")]
        public string AppServiceUId { get; set; }

        [BsonElement("Tags")]
        public string Tags { get; set; }

        [BsonElement("ApplicationURL")]
        public string ApplicationURL { get; set; }

        [BsonElement("IPAddress")]
        public string IPAddress { get; set; }

        [BsonElement("Browser")]
        public string Browser { get; set; }

        [BsonElement("ScreenResolution")]
        public string ScreenResolution { get; set; }

        [BsonElement("UsageType")]
        public string UsageType { get; set; }

        [BsonElement("Language")]
        public string Language { get; set; }

        [BsonElement("RowStatusUId")]
        public string RowStatusUId { get; set; }

        [BsonElement("CreatedByUser")]
        public string CreatedByUser { get; set; }

        [BsonElement("CreatedByApp")]
        public string CreatedByApp { get; set; }

        [BsonElement("CreatedOn")]
        public string CreatedOn { get; set; }

        [BsonElement("ModifiedByUser")]
        public string ModifiedByUser { get; set; }

        [BsonElement("ModifiedByApp")]
        public string ModifiedByApp { get; set; }

        [BsonElement("ModifiedOn")]
        public string ModifiedOn { get; set; }

        [BsonElement("Status")]
        public string Status { get; set; }

        [BsonElement("RetryCount")]
        public int RetryCount { get; set; }
        [BsonElement("Environment")]
        public string Environment { get; set; }
        [BsonElement("End2EndId")]
        public string End2EndId { get; set; }
    }

    public class FeatureAssetUsage
    {
        public Dictionary<string, int> VDSFeatureWise { get; set; }
        public Dictionary<string, int> ApplicationWise { get; set; }
        public Dictionary<string, Dictionary<string, int>> AppIntegrationWise { get; set; }

        public Dictionary<string, Dictionary<string, int>> VDSClientWise { get; set; }

        public Dictionary<string, Dictionary<string, int>> AppClientWise { get; set; }

        public Dictionary<string, Dictionary<string, int>> AppFeatureClientWise { get; set; }

        public Dictionary<string, int> TrainingWise { get; set; }

        public Dictionary<string, int> PredictionWise { get; set; }

        public Dictionary<string, Dictionary<string, Dictionary<string, int>>> AppClientDCWise { get; set; }

        public Dictionary<string, Dictionary<string, int>> AppTrainingDCWise { get; set; }

        public Dictionary<string, Dictionary<string, int>> AppPredictionDCWise { get; set; }
    }

    public class IngrainCoreFeature
    {
        public Dictionary<string, int> FeatureCount { get; set; }

        public Dictionary<string, Dictionary<string, int>> FeatureClientCount { get; set; }

        public Dictionary<string, Dictionary<string, Dictionary<string, int>>> FeatureClientDCWise { get; set; }

        public Dictionary<string, Dictionary<string, int>> SubFeatureCount { get; set; }

    }


}
