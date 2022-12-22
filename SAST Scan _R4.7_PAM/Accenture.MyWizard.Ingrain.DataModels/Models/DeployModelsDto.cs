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
    public class DeployModelViewModel
    {
        public List<DeployModelsDto> DeployModels { get; set; }
        public string ModelName { get; set; }
        public string DataSource { get; set; }
        public string BusinessProblem { get; set; }
        public string ModelType { get; set; }
        public bool InstaFlag { get; set; }
        public string Category { get; set; }
        public bool IsException { get; set; }
        public bool IsCascadeModel { get; set; }
        public string ErrorMessage { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class DeployModelsDto
    {
        public string _id { get; set; }
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
        public PublishModelFrequency Training { get; set; }
        public PublishModelFrequency Prediction { get; set; }
        public PublishModelFrequency Retraining { get; set; }
        public int RetrainingFrequencyInDays { get; set; }
        public int TrainingFrequencyInDays { get; set; }
        public int PredictionFrequencyInDays { get; set; }

        public bool IsTemplateDataEncyptionUpdated { get; set; }
        public int ArchivalDays { get; set; }

        public string offlineRunDate { get; set; }

        public string ExceptionDate { get; set; }

        public string ExceptionMessage { get; set; }
        public string offlineTrainingRunDate { get; set; }

        public string offlinePredRunDate { get; set; }
        public int Threshold { get; set; }

    }

    public class PublishModelFrequency
    {
        public int Hourly { get; set; }
        public int Daily { get; set; }
        public int Weekly { get; set; }
        public int Monthly { get; set; }
        public int Fortnightly { get; set; }

        public int RetryCount { get; set; }
    }

    public class ModelRequestStatus
    {
        public string _id { get; set; }
        public string ModelTemplateName { get; set; }
        public string CorrelationId { get; set; }
        public string TrainingStatus { get; set; }
        public string PredictionStatus { get; set; }
        public string RetrainingStatus { get; set; }
        public string CreatedOn { get; set; }
        public string ModifiedOn { get; set; }
        public string CreatedByUser { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class DeployedModel
    {
        public string CorrelationId { get; set; }
        public string ModelName { get; set; }
        public string ModelVersion { get; set; }
        public string Category { get; set; }
        public string[] LinkedApps { get; set; }
        public string UserId { get; set; }
        public string TrainedModelId { get; set; }
        public string Url { get; set; }
        public string VdsLink { get; set; }
        public string ModelType { get; set; }
        public Double Accuracy { get; set; }
        public bool IsPrivate { get; set; }
        public string[] App { get; set; }
        public string AppId { get; set; }
        public string Status { get; set; }
        public string WebServices { get; set; }
        public string DeployedDate { get; set; }
        public string Frequency { get; set; }
        public bool IsModelTemplate { get; set; }
        public bool IsCascadeModel { get; set; }
        public bool IsIncludedInCascade { get; set; }
        public bool IsIncludedinCustomCascade { get; set; }
        public bool IsCascadingButton { get; set; }
        public string CascadeId { get; set; }
        public string CustomCascadeId { get; set; }
        public int MaxDataPull { get; set; }
        public bool IsCarryOutRetraining { get; set; }
        public bool IsOnline { get; set; }
        public bool IsOffline { get; set; }
        public PublishModelFrequency Training { get; set; }
        public PublishModelFrequency Prediction { get; set; }
        public PublishModelFrequency Retraining { get; set; }
        public int RetrainingFrequencyInDays { get; set; }
        public int TrainingFrequencyInDays { get; set; }
        public int PredictionFrequencyInDays { get; set; }
        public int ArchivalDays { get; set; }
        public int Threshold { get; set; }
    }

    public class VisualDataModel
    {
        public List<object> Forecast { get; set; }
        public List<object> RangeTime { get; set; }

        public List<object> xlabel { get; set; }
        public List<object> predictionproba { get; set; }

        public List<object> legend { get; set; }

        public object target { get; set; }

        public object ProblemType { get; set; }

        public object Target { get; set; }

        public object Frequency { get; set; }

        public object xlabelname { get; set; }

        public object ylabelname { get; set; }

        public string BusinessProblems { get; set; }

        public string ModelName { get; set; }

        public string DataSource { get; set; }

        public string Category { get; set; }
    }
    public class CascadeViz
    {
        public string ModelName { get; set; }
        public string CorrelationId { get; set; }
        public string Category { get; set; }
    }
 
    public class ArchiveModel
    {
        public string CollectionName { get; set; }
        public BsonDocument CollectionValue { get; set; }
        public string ArchiveDate { get; set; }
    }
    public class ArchiveModels
    {
        public string CollectionName { get; set; }
        public DeployModelsDto CollectionValue { get; set; }
        public string ArchiveDate { get; set; }
    }
}
