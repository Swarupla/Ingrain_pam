using System;
using System.Collections.Generic;
using System.Text;
using MongoDB.Bson.Serialization.Attributes;

namespace Accenture.MyWizard.Ingrain.DataModels.AICore
{
    [BsonIgnoreExtraElements]
    public class AICoreModels
    {
        public string ClientId { get; set; }
        public string LastTrainedBy { get; set; }
        public string UniId { get; set; }
        public string DeliveryConstructId { get; set; }
        public string CorrelationId { get; set; }
        public string ApplicationId { get; set; }
        public string UsecaseId { get; set; }
        public string ModelName { get; set; }
        public string ServiceId { get; set; }
        public string PythonModelName { get; set; }
        public string ModelPath { get; set; }
        public string ModelStatus { get; set; }
        public string StatusMessage { get; set; }
        public string PredictionURL { get; set; }     
        public string CreatedOn { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedOn { get; set; }
        public string ModifiedBy { get; set; }

        public string DataSource { get; set; }

        public string SendNotification { get; set; }

        public string IsNotificationSent { get; set; }
        public string DataSetUId { get; set; }
        public string ResponsecallbackUrl { get; set; }
        public int MaxDataPull { get; set; }
        public bool IsCarryOutRetraining { get; set; }
        public bool IsOnline { get; set; }
        public bool IsOffline { get; set; }
        public AIModelFrequency Training { get; set; }
        public AIModelFrequency Prediction { get; set; }
        public AIModelFrequency Retraining { get; set; }
        public int RetrainingFrequencyInDays { get; set; }
        public int TrainingFrequencyInDays { get; set; }
        public int PredictionFrequencyInDays { get; set; }
    }


    public class AIModelFrequency
    {
        public int Hourly { get; set; }
        public int Daily { get; set; }
        public int Weekly { get; set; }
        public int Monthly { get; set; }
        public int Fortnightly { get; set; }
        public int RetryCount { get; set; }
    }
    public class ModelsList 
    {
        public List<AICoreModels> ModelStatus { get; set; }
        public List<ModelColDetails> ModelColumns { get; set; }
    }

    public class ModelColDetails
    {
        public string CorrelationId { get; set; }
        public List<string> SelectedColumnNames { get; set; }
        public string ScoreUniqueName { get; set; }
        public string DataSource { get; set; }
    }

    public class AIModelStatus
    {
        public string CorrelationId { get; set; }
        public string ModelStatus { get; set; }

        public string StatusMessage { get; set; }
    }

}
