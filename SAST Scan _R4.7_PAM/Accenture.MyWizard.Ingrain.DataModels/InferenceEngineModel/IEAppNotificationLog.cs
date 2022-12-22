using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.DataModels.InferenceEngineModel
{
   public class IEAppNotificationLog
    {
        [BsonId]
        public ObjectId _id { get; set; }
        public string RequestId { get; set; }
        public string UseCaseName { get; set; }
        public string UseCaseDescription { get; set; }
        public string ClientUId { get; set; }
        public string DeliveryConstructUId { get; set; }
        public string ApplicationId { get; set; }
        public string Entity { get; set; }
        public string NotificationEventType { get; set; } //Training or Prediction
        public string OperationType { get; set; }// Created or Updated or Deleted
        public string CorrelationId { get; set; }
        public string UseCaseId { get; set; }
        public string ModelType { get; set; }// Model Template or Public or Private
        public string ProblemType { get; set; }// Classification or Regression or Timeseries
        public string FunctionalArea { get; set; }
        public string CallBackLink { get; set; }
        public string UniqueId { get; set; }
        public string UserId { get; set; }
        public string AppNotificationUrl { get; set; }
        public string Environment { get; set; }
        public bool IsNotified { get; set; }
        public int RetryCount { get; set; }
        public string Status { get; set; }
        public bool IsCascade { get; set; }
        public string Progress { get; set; }
        public string StatusMessage { get; set; }
        public string NotificationResponseMessage { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedOn { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime ModifiedOn { get; set; }
        public string Message { get; set; }
        public string ErrorMessage { get; set; }
        public string ModelStatus { get; set; }
    }

    public class IENotificationDetails
    {
        public string RequestId { get; set; }
        public string UseCaseName { get; set; }
        public string UseCaseDescription { get; set; }
        public string ClientUId { get; set; }
        public string DeliveryConstructUId { get; set; }
        public string ApplicationId { get; set; }
        public string NotificationEventType { get; set; } //Training or Prediction
        public string OperationType { get; set; } // Created or Updated or Deleted
        public string CorrelationId { get; set; }
        public string UseCaseId { get; set; }
        public string ModelType { get; set; } // Model Template or Public or Private
        public string ProblemType { get; set; } // Classification or Regression or Timeseries
        public string FunctionalArea { get; set; }
        public string Status { get; set; }
        public string Progress { get; set; }
        public string CallBackLink { get; set; }
        public string UniqueId { get; set; }
        public string UserId { get; set; }
        public string StatusMessage { get; set; }
        public string Message { get; set; }
        public string ErrorMessage { get; set; }
        public string ModelStatus { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public string Entity { get; set; }
    }
    public class IENotificationResponse
    {
        public bool IsSuccess { get; set; }
        public dynamic StatusCode { get; set; }
        public string CorrelationId { get; set; }
        public string StatusMessage { get; set; }
    }

    public class IEPredictionData
    {
        public string CorrelationId { get; set; }
        public string UniqueId { get; set; }
        public string PredictedData { get; set; }
        public string Message { get; set; }
        public string ErrorMessage { get; set; }
        public string Status { get; set; }
    }
}
