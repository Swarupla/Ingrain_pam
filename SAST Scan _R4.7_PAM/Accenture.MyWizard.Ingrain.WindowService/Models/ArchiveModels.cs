using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.WindowService.Models
{
    public class ArchiveModels
    {
        public string CollectionName { get; set; }
        public ArchiveDeployModelsDto CollectionValue { get; set; }
        public string ArchiveDate { get; set; }
    }

    public class ArchiveModel
    {
        public string CollectionName { get; set; }
        public BsonDocument CollectionValue { get; set; }
        public string ArchiveDate { get; set; }
    }
    

    [BsonIgnoreExtraElements]
    public class ArchiveDeployModelsDto
    {
        [BsonId]
        public ObjectId _id { get; set; }
        public string InstaId { get; set; }
        public string ModelName { get; set; }
        public string CorrelationId { get; set; }
        public string DeliveryConstructUID { get; set; }
        public double Accuracy { get; set; }
        public string[] LinkedApps { get; set; }
        public string Status { get; set; }
        public string Category { get; set; }
        public bool IsCascadeModel { get; set; }
        public string AppId { get; set; }
        public string FinalModel { get; set; }
        public string WebServices { get; set; }
        public string ModelURL { get; set; }
        public bool IsIncludedInCascade { get; set; }
        public string ClientUId { get; set; }
        public string DataSetUId { get; set; }
        public string DataSource { get; set; }
        public string SourceName { get; set; }
        public bool IsPrivate { get; set; }
        public bool DBEncryptionRequired { get; set; }
        public string ModelVersion { get; set; }
        public string ModelType { get; set; }
        public string UseCaseID { get; set; }
        public string TemplateUsecaseId { get; set; }
        public string TrainedModelId { get; set; }
        public string VDSLink { get; set; }
        public string InputSample { get; set; }
        public string Frequency { get; set; }
        public string DeployedDate { get; set; }
        public string CreatedOn { get; set; }
        public string CreatedByUser { get; set; }
        public string ModifiedOn { get; set; }
        public string ModifiedByUser { get; set; }
        public string IsUpdated { get; set; }
        public string ModelHealth { get; set; }
        public string Language { get; set; }
        public string[] CascadeIdList { get; set; }
        public string CustomCascadeId { get; set; }
        public bool IsIncludedinCustomCascade { get; set; }
        public bool IsModelTemplate { get; set; }
        public bool IsModelTemplateDataSource { get; set; }
        public string DataCurationName { get; set; }
        public bool IsCascadingButton { get; set; }
        public bool IsFMModel { get; set; }
        public bool HideFMModel { get; set; }
        public string FMCorrelationId { get; set; }

        public bool IsMutipleApp { get; set; }

        public int MaxDataPull { get; set; }
        public bool IsCarryOutRetraining { get; set; }
        public bool IsOnline { get; set; }
        public bool IsOffline { get; set; }
        public ArchivePublishModelFrequency Training { get; set; }
        public ArchivePublishModelFrequency Prediction { get; set; }
        public ArchivePublishModelFrequency Retraining { get; set; }
        public int RetrainingFrequencyInDays { get; set; }
        public int TrainingFrequencyInDays { get; set; }
        public int PredictionFrequencyInDays { get; set; }

        public bool IsTemplateDataEncyptionUpdated { get; set; }
        public int ArchivalDays { get; set; }
    }
    public class ArchivePublishModelFrequency
    {
        public int Hourly { get; set; }
        public int Daily { get; set; }
        public int Weekly { get; set; }
        public int Monthly { get; set; }
        public int Fortnightly { get; set; }

        public int RetryCount { get; set; }
    }
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
    public class ManualArchivalTasks
    {
        [BsonId]
        public ObjectId _id { get; set; }
        public string TaskUId { get; set; }
        public string TaskCode { get; set; }
        public string TaskName { get; set; }

        public bool ManualTrigger { get; set; }
        public string CorrelationIds { get; set; }
        public string TimeToRun { get; set; }
        public string TimePeriod { get; set; }
        public string FrequencyInDays { get; set; }
        public string RunFrequencyInDays { get; set; }
        public string CreatedOn { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedOn { get; set; }
        public string ModifiedBy { get; set; }
    }
}
