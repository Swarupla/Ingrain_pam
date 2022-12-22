using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;
using System.ComponentModel;

namespace Accenture.MyWizard.Ingrain.DataModels.Models
{
    public class GenericTemplateInfo
    {
        public string ApplicationName { get; set; }
        public string AppicationID { get; set; }
        public string CorrelationID { get; set; }
        public string UsecaseName { get; set; }
        public string ClientUID { get; set; }
        public string DCID { get; set; }
        public string BusinessProblems { get; set; }
        public string ModelName { get; set; }
        public string SourceURL { get; set; }
        public string DataSource { get; set; }
        public string IsPrivate { get; set; }

        public string SourceName { get; set; }

        public object InputColumns { get; set; }
        public object AvailableColumns { get; set; }
        public string TemplateUseCaseID { get; set; }
        public string RequestStatus { get; set; }
        public string ProblemType { get; set; }
        public string pageInfo { get; set; }
        public string CreatedByUser { get; set; }
        public string CreatedOn { get; set; }
        public string ModifiedByUser { get; set; }
        public string ModifiedOn { get; set; }
        public string TargetColumn { get; set; }
        public string TargetUniqueIdentifier { get; set; }
        public string ParamArgs { get; set; }
        public object TimeSeries { get; set; }

    }

    public class GenericDataResponse
    {
        public string Message { get; set; }
        public string ErrorMessage { get; set; }
        public string WarningMessage { get; set; }
        public long DataPointsCount { get; set; }
        public string Status { get; set; }
        public string Progress { get; set; }
        public string CorrelationId { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class PublicTemplateMapping
    {
        [BsonId]
        public string _id { get; set; }
        [BsonElement("ApplicationName")]
        public string ApplicationName { get; set; }

        [BsonElement("ApplicationID")]
        public string ApplicationID { get; set; }

        [BsonElement("UsecaseName")]
        public string UsecaseName { get; set; }

        [BsonElement("UsecaseID")]
        public string UsecaseID { get; set; }
        public string SourceFlagName { get; set; }

        [BsonElement("SourceName")]
        public string SourceName { get; set; }

        [BsonElement("SourceURL")]
        public string SourceURL { get; set; }

        [BsonElement("InputParameters")]
        public dynamic InputParameters { get; set; }
        public string DateColumn { get; set; }
        public bool IsCascadeModelTemplate { get; set; }

        [BsonElement("CreatedOn")]
        public string CreatedOn { get; set; }

        [BsonElement("CreatedByUser")]
        public string CreatedByUser { get; set; }
        public string ModifiedOn { get; set; }
        public string ModifiedByUser { get; set; }
        [BsonElement("IterationUID")]
        public string IterationUID { get; set; }

        [BsonElement("TeamAreaUId")]
        public string TeamAreaUId { get; set; }

        [BsonElement("UsecaseType")]
        public string UsecaseType { get; set; }

        [BsonElement("TimeOutValue")]
        public string TimeOutValue { get; set; }

        [BsonElement("FeatureName")]
        public string FeatureName { get; set; }

        [BsonElement("IsMultipleApp")]
        public string IsMultipleApp { get; set; }

        [BsonElement("ApplicationIDs")]
        public string ApplicationIDs { get; set; }
        [BsonElement("DataPoints")]
        public long DataPoints { get; set; }

    }
    public class GenericApiData
    {
        public string ApplicationName { get; set; }
        public string ClientID { get; set; }
        public string DCID { get; set; }
        public string ApplicationID { get; set; }
        public string UseCaseID { get; set; }
        public string UseCaseName { get; set; }
        public string ProcessName { get; set; }
        public string Frequency { get; set; }
        public string DataSource { get; set; }
        public SourceDetails DataSourceDetails { get; set; }
    }

    public class PhoenixPredictionsInput
    {
        public string CorrelationId { get; set; }
        public string UniqueId { get; set; }
        public string PageNumber { get; set; }
    }

    public class PhoenixPredictionsOutput
    {
        public string CorrelationId { get; set; }
        public string UniqueId { get; set; }
        public string PageNumber { get; set; }
        public string Status { get; set; }
        public string Progress { get; set; }
        public string PredictedData { get; set; }
        public string DataPointsWarning { get; set; }
        public long DataPointsCount { get; set; }
        public string ErrorMessage { get; set; }
        public List<string> AvailablePages { get; set; }

    }

    public class SourceDetails
    {
        public string URL { get; set; }
        public dynamic BodyParams { get; set; }
    }

    public class TemplateModelPrediction
    {

        public string ApplicationName { get; set; }
        public string UseCaseName { get; set; }
        public string UniqueId { get; set; }
        public string CorrelationId { get; set; }
        public string Message { get; set; }
        public string Status { get; set; }
        public string PredictedData { get; set; }
        public long DataPointsCount { get; set; }
        public string DataPointsWarning { get; set; }
        public string ErrorMessage { get; set; }

    }
    public class PhoenixPredictionStatus
    {
        public string ApplicationName { get; set; }
        public string UseCaseName { get; set; }
        public string UniqueId { get; set; }
        public string CorrelationId { get; set; }
        public List<string> AvailablePages { get; set; }
        public string Message { get; set; }
        public string Status { get; set; }
        public string ErrorMessage { get; set; }

    }


    [BsonIgnoreExtraElements]
    public class AppIntegration
    {
        public string ApplicationID { get; set; }
        public string ApplicationName { get; set; }
        public string ProvisionedAppServiceUID { get; set; }
        public string Environment { get; set; }
        public dynamic AutoTrainDays { get; set; }
        public bool isDefault { get; set; }
        public string BaseURL { get; set; }
        public string clientUId { get; set; }
        public string deliveryConstructUID { get; set; }
        public string Authentication { get; set; }
        public string TokenGenerationURL { get; set; }
        public dynamic Credentials { get; set; }
        public string chunkSize { get; set; }
        public long DataPoints { get; set; }
        public string PredictionQueueLimit { get; set; }
        public string AppNotificationUrl { get; set; }
        public string CreatedByUser { get; set; }
        public string CreatedOn { get; set; }
        public string ModifiedByUser { get; set; }
        public string ModifiedOn { get; set; }

    }


    public class TrainingStatus
    {
        public string ApplicationID { get; set; }
        public string UsecaseID { get; set; }
        public string CorrelationId { get; set; }

        public string ApplicationName { get; set; }
        public string UsecaseName { get; set; }
        public string ModelName { get; set; }

        public string ClientId { get; set; }
        public string DeliveryconstructId { get; set; }
        public bool IsCascadeModelTemplate { get; set; }

        public string Status { get; set; }
        public string Message { get; set; }
        public string Progress { get; set; }
        public string CreatedByUser { get; set; }
        public string CreatedOn { get; set; }

    }
    public class PrivateModelDetails
    {
        public string ModelName { get; set; }
        public string ApplicationName { get; set; }
        public string Status { get; set; }
        public string ApplicationId { get; set; }
        public string ClientID { get; set; }
        public string DCID { get; set; }
        public string UserId { get; set; }
        public string UsecaseID { get; set; }
        public string CorrelationId { get; set; }
        public string DataSource { get; set; }
        public CustomSourceDetails DataSourceDetails { get; set; }

    }


    public class TemplateTrainingInput
    {
        public string ModelName { get; set; }
        public string ApplicationId { get; set; }
        public string ClientId { get; set; }
        public string DeliveryConstructId { get; set; }
        public string UserId { get; set; }
        public string UsecaseId { get; set; }
        public string CorrelationId { get; set; }
        public string DataSource { get; set; }
        public CustomSourceDetails DataSourceDetails { get; set; }

    }

    public class AppDetails
    {
        public string ApplicationName { get; set; }
        public string ApplicationId { get; set; }
    }

    public class CustomUploadFile
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
        public CustomPayloads Customdetails { get; set; }
    }
    public class CustomPayloads
    {
        private string _AICustom = "null";
        public string AppId { get; set; }
        public string HttpMethod { get; set; }
        public string AppUrl { get; set; }
        public InputParams InputParameters { get; set; }
        public string AICustom { get => _AICustom; set => _AICustom = value; }
    }

    public class InputParams
    {
        public string ClientID { get; set; }
        public string E2EUID { get; set; }
        public string DeliveryConstructID { get; set; }
        public string Environment { get; set; }
        public string RequestType { get; set; }
        public string ServiceType { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }

    }

    public class InputParameter
    {
        public string correlationid { get; set; }
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public double noOfRecord { get; set; }

    }

    public class Customdetails
    {
        private string _AICustom = "null";
        public CustomFlag CustomFlags { get; set; }
        public string AppId { get; set; }
        public string HttpMethod { get; set; }
        public string AppUrl { get; set; }
        public InputParameter InputParameters { get; set; }
        public string AICustom { get => _AICustom; set => _AICustom = value; }

    }

    public class CustomFlag
    {
        public string FlagName { get; set; }
    }


    public class CustomUploadEntity
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
        public Customdetails Customdetails { get; set; }
    }

    public class BodyParams
    {
        public string UniqueId { get; set; }
        public string PageNumber { get; set; }
    }

    public class DataSourceDetails
    {
        public string HttpMethod { get; set; }
        public string Url { get; set; }
        public string FetchType { get; set; }
        public string BatchSize { get; set; }
        public string TotalNoOfRecords { get; set; }
        public BodyParams BodyParams { get; set; }
    }

    public class CustomSourceDetails
    {
        public string HttpMethod { get; set; }
        public string Url { get; set; }
        public string FetchType { get; set; }
        public string BatchSize { get; set; }
        public string TotalNoOfRecords { get; set; }
        public dynamic BodyParams { get; set; }
    }


    public class TrainingRequestDetails
    {
        public string ClientUId { get; set; }
        public string DeliveryConstructUId { get; set; }
        public string ApplicationId { get; set; }
        public string UseCaseId { get; set; }
        public string CorrelationId { get; set; }
        public string DataSource { get; set; }
        public string IngrainTrainingResponseCallBackUrl { get; set; }
        public DataSourceDetails DataSourceDetails { get; set; }
        public string UserId { get; set; }
        public string Data { get; set; }
        public string DataFlag { get; set; }
    }

    public class GenericTrainingRequestDetails
    {
        public string ClientUId { get; set; }
        public string DeliveryConstructUId { get; set; }
        public string TeamAreaUID { get; set; }
        public string ApplicationId { get; set; }
        public string UseCaseId { get; set; }
        public string ProvisionedAppServiceUID { get; set; }
        public string CorrelationId { get; set; }
        public string DataSource { get; set; }
        public string IngrainTrainingResponseCallBackUrl { get; set; }
        public DataSourceDetails DataSourceDetails { get; set; }
        public string UserId { get; set; }
        public string Data { get; set; }
        public string DataFlag { get; set; }
        public string IsAmbulanceLane { get; set; }
        public string ResponseCallbackUrl { get; set; }
        public dynamic QueryData { get; set; }
    }

    public class Parent
    {
        public string Type { get; set; }
        public string Name { get; set; }
    }

    public class Fileupload
    {
        public string fileList { get; set; }
    }

    public class ParamArgsCustomMultipleFetch
    {
        public string Customdetails { get; set; }
        public string CorrelationId { get; set; }
        public string ClientUID { get; set; }
        public string DeliveryConstructUId { get; set; }
        public Parent Parent { get; set; }
        public string Flag { get; set; }
        public string mapping { get; set; }
        public string mapping_flag { get; set; }
        public string pad { get; set; }
        public string metric { get; set; }
        public string InstaMl { get; set; }
        public Fileupload fileupload { get; set; }
        public CustomMultipleFetch CustomMultipleFetch { get; set; }
    }

    public class CustomMultipleFetch
    {
        public string ApplicationId { get; set; }

        public string AppServiceUId { get; set; }
        public string DataSource { get; set; }
        public string HttpMethod { get; set; }
        public string Url { get; set; }
        public string FetchType { get; set; }
        public string BatchSize { get; set; }
        public string TotalNoOfRecords { get; set; }
        public BodyParams BodyParams { get; set; }
        public string Data { get; set; }
        public string DataFlag { get; set; }
    }

    public class AppIntegrationsCredentials
    {
        public string grant_type { get; set; }
        public string client_secret { get; set; }
        public string client_id { get; set; }
        public string resource { get; set; }
    }

    public class RequestData
    {
        public string UseCaseUId { get; set; }
        public string AppServiceUId { get; set; }
        public string ClientUId { get; set; }
        public string DeliveryConstructUId { get; set; }
        public string ResponseCallbackUrl { get; set; }
    }

    public class IngrainResponseData
    {
        public string CorrelationId { get; set; }
        public string Message { get; set; }
        public string ErrorMessage { get; set; }
        public long DataPointsCount { get; set; }
        public string DataPointsWarning { get; set; }
        public string Status { get; set; }
    }
    public class DeleteAppResponse
    {
        public string Message { get; set; }
        public string Status { get; set; }
    }
    public class UpdatePublicTemplatedata
    {
        public string ApplicationName { get; set; }
        public string UsecaseName { get; set; }
        public string Resource { get; set; }
        public string SourceURL { get; set; }
        public dynamic InputParameters { get; set; }
        public string DateColumn { get; set; }
    }
    public class ModelTrainingStatus
    {
        public string CorrelationId { get; set; }
        public string ClientUId { get; set; }
        public string DeliveryConstructUId { get; set; }
        public string PageInfo { get; set; }
        public string Message { get; set; }
        public string ErrorMessage { get; set; }
        public string Status { get; set; }
        public bool IsPredicted { get; set; }
        public long DataPointsCount { get; set; }
        public string DataPointsWarning { get; set; }
    }

    public class ParamArgsWithCustomFlag
    {
        public string CorrelationId { get; set; }
        public string ClientUID { get; set; }
        public string DeliveryConstructUId { get; set; }
        public Parent Parent { get; set; }
        public string Flag { get; set; }
        public string mapping { get; set; }
        public string mapping_flag { get; set; }
        public string pad { get; set; }
        public string metric { get; set; }
        public string InstaMl { get; set; }
        public Fileupload fileupload { get; set; }
        public CustomPayloadDetails Customdetails { get; set; }
    }
    public class CustomPayloadDetails
    {
        public dynamic CustomFlags { get; set; }
        public string AppId { get; set; }
        public string HttpMethod { get; set; }
        public string AppUrl { get; set; }
        public dynamic InputParameters { get; set; }
        public string DateColumn { get; set; }
        public string UsecaseID { get; set; }

    }
    public class IngrainPredictionData
    {
        public string CorrelationId { get; set; }
        public string UniqueId { get; set; }
        public string PredictedData { get; set; }
        public string Message { get; set; }
        public string ErrorMessage { get; set; }
        public string Status { get; set; }
    }
    public class FMModelTrainingStatus
    {
        public string ApplicationID { get; set; }
        public string UsecaseID { get; set; }
        public string CorrelationId { get; set; }
        public string FMCorrelationId { get; set; }
        public string ApplicationName { get; set; }
        public string UsecaseName { get; set; }
        public string ModelName { get; set; }
        public string ClientId { get; set; }
        public string DeliveryconstructId { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
        public string Progress { get; set; }
        public string CreatedByUser { get; set; }
        public string CreatedOn { get; set; }

    }

    public class ModelTemplateInput
    {
        public string TemplateType { get; set; }
        public string ApplicationID { get; set; }
        public string TemplateUseCaseID { get; set; }
        public string UseCaseID { get; set; }
        public string serviceType { get; set; }
        public string ServiceId { get; set; }
        public string CorrelationId { get; set; }
        public string ModelName { get; set; }
        public string ClientId { get; set; }
        public string DeliveryconstructId { get; set; }
        public string CreatedByUser { get; set; }
        public string CreatedOn { get; set; }

    }


}
