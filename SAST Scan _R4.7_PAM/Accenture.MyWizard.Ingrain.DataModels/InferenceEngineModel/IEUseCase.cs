using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Accenture.MyWizard.Ingrain.DataModels.InferenceEngineModel
{
    public class IEUseCase
    {
        [BsonId]
        public ObjectId _id { get; set; }
        public string UseCaseId { get; set; }
        public string UseCaseName { get; set; }
        public string UseCaseDescription { get; set; }
        public string CorrelationId { get; set; }
        public string ApplicationId { get; set; }
        public string TrainingDataRangeInMonths { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime ModifiedOn { get; set; }

        public string isSavedConfigEncrypted { get; set; }
    }


    public class AddUseCaseInput
    {
        public string UseCaseName { get; set; }
        public string UseCaseDescription { get; set; }
        public string CorrelationId { get; set; }
        public string ApplicationId { get; set; }
        public string UserId { get; set; }
    }


    public class UseCaseDetails
    {
        public string UseCaseId { get; set; }
        public string UseCaseName { get; set; }
        public string UseCaseDescription { get; set; }
        public string CorrelationId { get; set; }
        public string ApplicationId { get; set; }
        public string ApplicationName { get; set; }
        public string SourceName { get; set; }
        public string Entity { get; set; }

        public List<Config> InferenceConfigurationsDetails { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public string ModifiedOn { get; set; }
    }

    public class Config
    {
        public string InferenceConfigId { get; set; }
        public string ConfigName { get; set; }
      
        public VolumetricConfig VolumetricConfig { get; set; }
        public MetricConfig MetricConfig { get; set; }
    }

    public class VolumetricConfig
    {
        public string DateColumn { get; set; }
        public string TrendForecast { get; set; }
        public List<string> Frequency { get; set; }
        public List<string> Dimensions { get; set; }
    }

    public class MetricConfig
    {
        public string MetricColumn { get; set; }
        public string DateColumn { get; set; }
        public List<string> Features { get; set; }
        public List<FeatureCombinations> FeatureCombinations { get; set; }
    }


    public class TrainUseCaseInput
    {
        [Required]
        public string ClientUId { get; set; }
        [Required]
        public string DeliveryConstructUId { get; set; }
        [Required]
        public string UseCaseId { get; set; }
        [Required]
        public string ApplicationId { get; set; }
        [Required]
        public string DataSource { get; set; }
        public dynamic DataSourceDetails { get; set; }
        [Required]
        public string UserId { get; set; }
    }


    public class TrainUseCaseOutput
    {

        public string ClientUId { get; set; }
        public string DeliveryConstructUId { get; set; }
        public string UseCaseId { get; set; }
        public string ApplicationId { get; set; }

        public string CorrelationId { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
        public string Progress { get; set; }

        public string UserId { get; set; }


        public TrainUseCaseOutput()
        {

        }
        public TrainUseCaseOutput(string clientUId, string deliveryConstructUId, string useCaseId, string applicationId, string userId)
        {
            this.ClientUId = clientUId;
            this.DeliveryConstructUId = deliveryConstructUId;
            this.UseCaseId = useCaseId;
            this.ApplicationId = applicationId;
            this.UserId = userId;
        }
    }
}
