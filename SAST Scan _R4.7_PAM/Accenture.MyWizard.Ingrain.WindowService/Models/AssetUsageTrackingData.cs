using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.WindowService.Models
{
    [BsonIgnoreExtraElements]
    public class AssetUsageTrackingData
    {
        public string UserUniqueId { get; set; }
        public string ApplicationName { get; set; }

        public string Description { get; set; }

        public Guid ClientUId { get; set; }

        public Guid DeliveryConstructUId { get; set; }

        public string FeatureName { get; set; }

        public string SubFeatureName { get; set; }

        public string UserName { get; set; }

        public string AppServiceUId { get; set; }

        public string Tags { get; set; }

        public string ApplicationURL { get; set; }

        public string IPAddress { get; set; }

        public string Browser { get; set; }

        public string ScreenResolution { get; set; }

        public string UsageType { get; set; }

        public string Language { get; set; }

        public string RowStatusUId { get; set; }

        public string CreatedByUser { get; set; }

        public string CreatedByApp { get; set; }

        public string CreatedOn { get; set; }

        public string ModifiedByUser { get; set; }

        public string ModifiedByApp { get; set; }

        public string ModifiedOn { get; set; }

        public string Status { get; set; }

        public int RetryCount { get; set; }
    }

    public class AssetUsageTrackingGroupModel
    {
        public string ClientUId { get; set; }

        public string End2EndId { get; set; }

        public string UsageType { get; set; }

        public string FeatureName { get; set; }

        public int Count { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class SaaSProvisionDetailAssetUsageModel
    {
        public string ServiceUId { get; set; }

        public string ServiceName { get; set; }

        public string E2EUID { get; set; }

        public string ClientUID { get; set; }

        public string DeliveryConstructUID { get; set; }

        public string InstanceType { get; set; }

        public string CorrelationUId { get; set; }
    }

    public class AssetUsageSaasSubmitModel
    {
        public string ServiceUId { get; set; }

        public string ServiceName { get; set; }

        public string InstanceType { get; set; }

        public string LogTime { get; set; }

        public string EntityUId { get; set; }

        public string Entity { get; set; }

        public string CorrelationUId { get; set; }

        public string CallbackUrl { get; set; }

        public List<AssetUsageSaasSubmitUsageModel> Usages { get; set; }
    }

    public class AssetUsageSaasSubmitUsageModel
    {
        public string ClientUId { get; set; }

        public string DeliveryConstructUId { get; set; }

        public string E2EUId { get; set; }

        public string DeliveryConstructName { get; set; }

        public UsageModel Usage { get; set; }
    }

    public class UsageModel
    {
        public string UsageType { get; set; }

        public int Count { get; set; }

        public string UsageFrom { get; set; }

        public string UsageTo { get; set; }

        public List<UsageExtensionModel> UsageExtensions { get; set; }
    }

    public class UsageExtensionModel
    {
        public string Key { get; set; }

        public object Value { get; set; }
    }
}
