using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.WindowService.Models
{
    public class PredictionSchedulerLog
    {
        [BsonId]
        public ObjectId _id { get; set; }
        public string ClientUId { get; set; }
        public string DeliveryConstructUId { get; set; }
        public string UseCaseUId { get; set; }
        public string EntityUId { get; set; }
        public string ItemUId { get; set; }
        public string LogType { get; set; } // Training or Prediction or Other
        public string LogLevel { get; set; } // Info or Error
        public string Status { get; set; }
        public string LogMessage { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public string ModifiedOn { get; set; }

        public string CorrelationId { get; set; }
        public string UniqueId { get; set; }

    }


    public class PhoenixIterations
    {
        [BsonId]
        public ObjectId _id { get; set; }
        public string ClientUId { get; set; }
        public string DeliveryConstructUId { get; set; }
        public string EntityUId { get; set; }
        public string ItemUId { get; set; }
        public string MethodologyUId { get; set; }
        public DateTime IterationEndOn { get; set; }
        public bool isRecommendationCompleted { get; set; }
        public DateTime? LastRecommendationUpdateTime { get; set; }
        public DateTime? LastRecordsPullUpdateTime { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime ModifiedOn { get; set; }
        public bool IsCustomConstraintModel { get; set; }
        public string TrainedModelCorrelationID { get; set; }
        public string TemplateUsecaseId { get; set; }

    }
}
