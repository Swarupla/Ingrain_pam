using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


namespace Accenture.MyWizard.Ingrain.DataModels.Models
{
    #region Generic Model Training
    public class GenericModelTrainingRequest
    {
        [Required]
        public string UseCaseId { get; set; }
        [Required]
        public string ClientUId { get; set; }
        [Required]
        public string ApplicationId { get; set; }
        [Required]
        public string UserId { get; set; }
        [Required]
        public string DeliveryConstructUId { get; set; }
        [Required]
        public string ResponseCallbackUrl { get; set; }
        [Required]
        public dynamic DataSourceDetails { get; set; }
    }
    public class GenericModelTrainingResponse
    {
        public string CorrelationId { get; set; }
        public string Message { get; set; }
        public string ErrorMessage { get; set; }
        public string Status { get; set; }
        public string Progress { get; set; }
        //public long DataPointsCount { get; set; }
        //public string DataPointsWarning { get; set; }

    }
    public class SSAIIngrainTrainingStatus
    {
        public string CorrelationId { get; set; }
        public string Message { get; set; }
        public string ErrorMessage { get; set; }
        public string Status { get; set; }
        public string Progress { get; set; }
        public string UniqueId { get; set; }
        public string PredictedData { get; set; }
        public string CreatedOn { get; set; }
        public string ModifiedOn { get; set; }
    }
    public class VDSGenericParamArgs
    {
        public string CorrelationId { get; set; }
        public string ClientUID { get; set; }
        public string DeliveryConstructUId { get; set; }
        public ParentFile Parent { get; set; }
        public string Flag { get; set; }
        public string mapping { get; set; }
        public string mapping_flag { get; set; }
        public string pad { get; set; }
        public string metric { get; set; }
        public string InstaMl { get; set; }
        public Filepath fileupload { get; set; }
        public VDSGenericPayloads Customdetails { get; set; }
    }
    public class VDSGenericPayloads
    {
        private string _AICustom = "null";
        public string AppId { get; set; }
        public string UsecaseID { get; set; }
        public string HttpMethod { get; set; }
        public string AppUrl { get; set; }
        public VDSInputParams InputParameters { get; set; }
        public string AICustom { get => _AICustom; set => _AICustom = value; }
    }
    public class VDSInputParams
    {
        // public string CorrelationId { get; set; }
        public string ClientID { get; set; }
        public string E2EUID { get; set; }
        public string DeliveryConstructID { get; set; }
        public string Environment { get; set; }
        public string RequestType { get; set; }
        public string ServiceType { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        //public double TotalRecordCount { get; set; }
        //public double PageNumber { get; set; }
        //public double BatchSize { get; set; }
    }
    public class IngrainToVDSNotification
    {
        public string CorrelationId { get; set; }
        public string Message { get; set; }
        public string ErrorMessage { get; set; }
        public string Status { get; set; }
        public string ClientUId { get; set; }
        public string DeliveryConstructUId { get; set; }
        public string UseCaseId { get; set; }
        public string RequestType { get; set; }
        public string ServiceType { get; set; }
        public string E2EUID { get; set; }
        public string Progress { get; set; }
        public string ProcessingStartTime { get; set; }
        public string ProcessingEndTime { get; set; }
    }
    #endregion Generic Model Training
    #region Prediction Model
    public class VDSUseCasePredictionRequest
    {
        [Required]
        public string ClientUID { get; set; }
        [Required]
        public string DeliveryConstructUID { get; set; }
        [Required]
        public string UseCaseUID { get; set; }
        [Required]
        public string AppServiceUID { get; set; }
        public dynamic Data { get; set; }
        [Required]
        public List<string> StartDates { get; set; }
        [Required]
        public string CorrelationId { get; set; }
    }
    public class VDSUseCasePredictionOutput
    {
        public string UseCaseId { get; set; }
        public string CorrelationId { get; set; }
        public string UniqueId { get; set; }
        public string Status { get; set; }
        public string StatusMessage { get; set; }
        public long DataPointsCount { get; set; }
        public string DataPointsWarning { get; set; }
        public string StartTime { get; set; }
    }
    public class VDSPredictionResponseInput
    {
        [Required]
        public string UniqueId { get; set; }
        [Required]
        public string CorrelationId { get; set; }
    }
    public class VDSPredictionResponseOutput
    {
        public string CorrelationId { get; set; }
        public string Message { get; set; }
        public string ErrorMessage { get; set; }
        public string Status { get; set; }
        public string UniqueId { get; set; }
        public string PredictedData { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public string ModelType { get; set; }
    }
    #endregion Prediction Model
}

