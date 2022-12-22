using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.DataModels.Models
{
    public class Velocity
    {
        public string UseCaseUID { get; set; }
        public string ClientUID { get; set; }
        public string AppServiceUID { get; set; }
        public string UserID { get; set; }
        public string DeliveryConstructUID { get; set; }
        public string ResponseCallbackUrl { get; set; }
        public dynamic QueryData { get; set; }
        public dynamic TeamAreaUId { get; set; }

        public string IsTeamLevelData { get; set; }

        public string RetrainRequired { get; set; }
    }
    public class VelocityTrainingStatus
    {
        public string CorrelationId { get; set; }
        public string Message { get; set; }
        public string ErrorMessage { get; set; }
        public string Status { get; set; }
        public string UniqueId { get; set; }
        public string PredictedData { get; set; }
    }
    public class VelocityTraining
    {
        public string CorrelationId { get; set; }
        public string Message { get; set; }
        public string ErrorMessage { get; set; }
        public string Status { get; set; }
        public long DataPointsCount { get; set; }
        public string DataPointsWarning { get; set; }

    }

    public class SPAInfo
    {
        public string ClientUID { get; set; }
        public string DeliveryConstructUID { get; set; }
        public string UseCaseUID { get; set; }
        public string AppServiceUID { get; set; }
        //public List<Data> Data { get; set; }

        public dynamic Data { get; set; }

        public string UserId { get; set; }
        public List<string> StartDates { get; set; }
        public string CorrelationId { get; set; }
    }
    public class Data
    {
        public double ActualEffort { get; set; }
        public double PlannedEffort { get; set; }
    }
    public class VelocityPrediction
    {
        public string CorrelationId { get; set; }
        public string PredictedData { get; set; }
        public string UseCaseUID { get; set; }
        public string UniqueId { get; set; }
        public string Message { get; set; }
        public string ErrorMessage { get; set; }
        public string Status { get; set; }
        public long DataPointsCount { get; set; }
        public string DataPointsWarning { get; set; }
    }
}
