using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Accenture.MyWizard.Ingrain.DataModels.AICore.UseCase
{
    [BsonIgnoreExtraElements]
    public class UsecaseDetails
    {
        public string offlineTrainingRunDate { get; set; }
        public string UsecaseId { get; set; }
        public string UsecaseName { get; set; }
        public string CorrelationId { get; set; }
        public string ServiceId { get; set; }
        public string ModelName { get; set; }
        public string Description { get; set; }
        public string ApplicationName { get; set; }
        public string ApplicationId { get; set; }
        public string SourceName { get; set; }
        public dynamic SourceDetails { get; set; }
        public List<dynamic> InputColumns { get; set; }
        public List<string> StopWords { get; set; }
        public string ScoreUniqueName { get; set; }
        public dynamic Threshold_TopnRecords { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public string ModifiedOn { get; set; }
        public string SourceURL { get; set; }
        public string DataSetUID { get; set; }
        public int MaxDataPull { get; set; }
        public int[] Ngram { get; set; }

        public bool IsCarryOutRetraining { get; set; }
        public bool IsOnline { get; set; }
        public bool IsOffline { get; set; }
        public UsecaseFrequency Training { get; set; }
        public UsecaseFrequency Prediction { get; set; }
        public UsecaseFrequency Retraining { get; set; }
        public int RetrainingFrequencyInDays { get; set; }
        public int TrainingFrequencyInDays { get; set; }
        public int PredictionFrequencyInDays { get; set; }
        public string offlineRunDate { get; set; }
        public string ExceptionDate { get; set; }
        public string ExceptionMessage { get; set; }

    }
    public class UsecaseFrequency
    {
        public int Hourly { get; set; }
        public int Daily { get; set; }
        public int Weekly { get; set; }
        public int Monthly { get; set; }
        public int Fortnightly { get; set; }
        public int RetryCount { get; set; }
    }
}
