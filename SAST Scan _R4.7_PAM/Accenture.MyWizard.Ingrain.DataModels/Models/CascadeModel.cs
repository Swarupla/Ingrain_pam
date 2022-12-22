using Microsoft.AspNetCore.Http;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.DataModels.Models
{
    public class CascadeModel
    {
        public string ClientUid { get; set; }
        public string ModelName { get; set; }
        public string DeliveryConstructUID { get; set; }
        public string UserId { get; set; }
        public bool IsCustomModel { get; set; }
        public string Category { get; set; }
        public List<CascadeModelDictionary> Models { get; set; }
        public JObject ModelsList { get; set; }
        public JObject MappingList { get; set; }
        public string Status { get; set; }
        public bool IsException { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class CustomCascadeModel
    {
        public string ClientUid { get; set; }
        public string ModelName { get; set; }
        public string DeliveryConstructUID { get; set; }
        public string UserId { get; set; }
        public string Category { get; set; }
        public List<CustomCascadeModelDictionary> Models { get; set; }
        public JObject ModelsList { get; set; }
        public JObject MappingList { get; set; }
        public string Status { get; set; }
        public bool IsException { get; set; }
        public string ErrorMessage { get; set; }
    }
    public class CascadeModelDictionary
    {
        public string CorrelationId { get; set; }
        public string ModelName { get; set; }
        public string ProblemType { get; set; }
        public double Accuracy { get; set; }
        public string ModelType { get; set; }
        public string LinkedApps { get; set; }
        public string ApplicationID { get; set; }
        public string TargetColumn { get; set; }
    }
    public class CustomCascadeModelDictionary
    {
        public string CorrelationId { get; set; }
        public string ModelName { get; set; }
        public string ProblemType { get; set; }
        public double Accuracy { get; set; }
        public string ModelType { get; set; }
        public string LinkedApps { get; set; }
        public string ApplicationID { get; set; }
        public bool IsIncludedinCustomCascade { get; set; }
        public string CustomCascadeId { get; set; }
        public int CascadeModelCount { get; set; }
        public string TargetColumn { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class CascadeData
    {
        public string CascadedId { get; set; }
        public string ModelName { get; set; }
        public string ClientUId { get; set; }
        public BsonDocument ModelList { get; set; }
        public BsonDocument Mappings { get; set; }
        public string Status { get; set; }
        public bool IsCustomModel { get; set; }
        public string Category { get; set; }
    }
    public class CascadeCollection
    {
        public string _id { get; set; }
        public bool IsCustomModel { get; set; }
        public string CascadedId { get; set; }
        public string ModelName { get; set; }
        public string UniqIdName { get; set; }
        public string UniqDatatype { get; set; }
        public string TargetColumn { get; set; }
        public string ClientUId { get; set; }
        public string DeliveryConstructUID { get; set; }
        public string Category { get; set; }
        public dynamic ModelList { get; set; }
        public dynamic Mappings { get; set; }
        public string Status { get; set; }
        public bool isModelUpdated { get; set; }
        public string[] RemovedModels { get; set; }
        public string CreatedOn { get; set; }
        public string CreatedByUser { get; set; }
        public string ModifiedOn { get; set; }
        public string ModifiedByUser { get; set; }
    }
    public class GenericCascadeCollection
    {
        public string _id { get; set; }
        public string CascadedId { get; set; }
        public string ModelName { get; set; }
        public bool IsCustomModel { get; set; }
        public string ClientUId { get; set; }
        public string DeliveryConstructUID { get; set; }
        public string Category { get; set; }
        public dynamic ModelList { get; set; }
        public dynamic Mappings { get; set; }
        public dynamic MappingData { get; set; }
        public string Status { get; set; }
        public bool isModelUpdated { get; set; }
        public string CreatedOn { get; set; }
        public string CreatedByUser { get; set; }
        public string ModifiedOn { get; set; }
        public string ModifiedByUser { get; set; }
    }
    public class CascadeSaveModel
    {
        public bool IsInserted { get; set; }
        public string CascadedId { get; set; }
        public bool IsException { get; set; }
        public string ErrorMessage { get; set; }
        public string Status { get; set; }
    }
    public class UpdateCascadeModelMapping
    {
        public bool IsInserted { get; set; }
        public string CascadedId { get; set; }
        public bool IsCustomModel { get; set; }
        public double CascadeTargetPercentage { get; set; }
        public int CascadeIDPercentage { get; set; }
        public bool IsValidate { get; set; }
        public bool IsException { get; set; }
        public string Status { get; set; }
        public string ErrorMessage { get; set; }
    }
    public class UpdateCasecadeModel
    {
        public string CascadedId { get; set; }
        public dynamic Mappings { get; set; }
        public bool isModelUpdated { get; set; }
    }
    public class CascadeModelsCollectionList
    {
        public List<CascadeModelsCollection> ModelList { get; set; }
    }
    public class CascadeModelsCollection
    {
        public string CorrelationId { get; set; }
        public string ModelName { get; set; }
        public string ProblemType { get; set; }
        public string Accuracy { get; set; }
        public string ModelType { get; set; }
        public string TargetColumn { get; set; }    
    }
    public class CascadeModelMapping
    {
        public string ModelName { get; set; }
        public bool IsCustomModel { get; set; }
        public bool IsError { get; set; }
        public JObject MappingData { get; set; }
        public string ErrorMessage { get; set; }
        public JObject MappingList { get; set; }
        public string CascadedId { get; set; }
        public string Category { get; set; }
        public string Status { get; set; }
    }
    public class DatatypeDict
    {
        public string Datatype { get; set; }
        public dynamic UniqueValues { get; set; }
        public double Metric { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
    }
    public class CascadeBsonDocument
    {
        public List<BsonDocument> BusinessProblemData { get; set; }
        public List<BsonDocument> DataCleanupData { get; set; }
        public List<BsonDocument> FilteredData { get; set; }
    }
    public class CascadeDeployViewModel
    {
        public List<CascadeDeployModel> CascadeModel { get; set; }
        public string ModelName { get; set; }
        public string DataSource { get; set; }
        public string BusinessProblem { get; set; }
        public string ModelType { get; set; }
        public bool InstaFlag { get; set; }
        public string Category { get; set; }
        public bool IsIngrainModel { get; set; }
        public bool IsCascadeModel { get; set; }
        public bool IsCustomModel { get; set; }
    }
    [BsonIgnoreExtraElements]
    public class CascadeDeployModel
    {
        public string _id { get; set; }
        public string InstaId { get; set; }
        public string ModelName { get; set; }
        public string ModelURL { get; set; }
        public double Accuracy { get; set; }
        public string[] LinkedApps { get; set; }
        public string Status { get; set; }
        public string Category { get; set; }
        public bool IsCascadeModel { get; set; }
        public string FinalModel { get; set; }
        public string WebServices { get; set; }
        public string CorrelationId { get; set; }
        public string DeliveryConstructUID { get; set; }
        public string DataSource { get; set; }
        public string AppId { get; set; }
        public bool IsPrivate { get; set; }
        public bool DBEncryptionRequired { get; set; }
        public string ModelVersion { get; set; }
        public string SourceName { get; set; }
        public string ModelType { get; set; }
        public string UseCaseID { get; set; }
        public string TemplateUsecaseId { get; set; }
        public string TrainedModelId { get; set; }
        public string VDSLink { get; set; }
        public string InputSample { get; set; }
        public string Frequency { get; set; }
        public string ClientUId { get; set; }
        public string DeployedDate { get; set; }
        public string CreatedOn { get; set; }
        public string CreatedByUser { get; set; }
        public string ModifiedOn { get; set; }
        public string ModifiedByUser { get; set; }
        public string ModelHealth { get; set; }
        public string Language { get; set; }

        public bool IsModelTemplate { get; set; }

        public bool IsModelTemplateDataSource { get; set; }
    }
    public class DeployModelDetails
    {
        public string CorrelationId { get; set; }
        public string ModelName { get; set; }
        public string ModelType { get; set; }
        public string ProblemType { get; set; }
        public Double Accuracy { get; set; }
        public string LinkedApps { get; set; }
        public string ApplicationID { get; set; }
    }
    public class MappingAttributes
    {
        public string Source { get; set; }
        public string Target { get; set; }
    }
    public class ValidationMapping
    {
        public bool IsValidate { get; set; }
        public string ErrorMessage { get; set; }
        public bool IsException { get; set; }
    }
    public class ModelInclusion
    {
        public string CorrelationId { get; set; }
        public string ModelName { get; set; }
        public string ProblemType { get; set; }
    }
    public class CustomMapping
    {
        public JObject CascadeModel { get; set; }
        public JObject MappingData { get; set; }
        public string CascadedId { get; set; }
        public string ClientUid { get; set; }
        public bool IsCustomModel { get; set; }
        public string ModelName { get; set; }
        public string DeliveryConstructUID { get; set; }
        public string SourceId { get; set; }
        public string Category { get; set; }
        public string TargetId { get; set; }
        public bool IsException { get; set; }
        public string ErrorMessage { get; set; }
        public int CascadeModelsCount { get; set; }
    }
    [BsonIgnoreExtraElements]
    public class BusinessProblem
    {
        public string CorrelationId { get; set; }
        public string[] ColumnsList { get; set; }
        public string TargetColumn { get; set; }
        public string[] InputColumns { get; set; }
        public object Frequency { get; set; }
        public object DataType { get; set; }
        public string ModelName { get; set; }
        public string DataSource { get; set; }
        public string BusinessProblems { get; set; }
        public string[] AvailableColumns { get; set; }
        public string TimeSeriesColumn { get; set; }
        public string TargetUniqueIdentifier { get; set; }
        public string Aggregation { get; set; }
        public JObject FrequencyList { get; set; }
        public BsonDocument TimeSeries { get; set; }
        public bool IsModelTrained { get; set; }
        public bool IsModelDeployed { get; set; }
    }
    public class CustomModelViewDetails
    {
        public JObject ModelList { get; set; }
        public string CascadeId { get; set; }
        public string Category { get; set; }
        public bool IsCustomModel { get; set; }
        public bool IsException { get; set; }
        public string ErrorMessage { get; set; }
    }
    public class CustomModelDetails
    {
        public string ModelName { get; set; }
        public string[] InputClumns { get; set; }
    }
    public class CascadeVDSModels
    {
        public string ClientUID { get; set; }
        public string DCUID { get; set; }
        public string Category { get; set; }
        public List<CascadeModels> CascadeModels { get; set; }
    }
    public class CascadeModels
    {
        public string CascadedId { get; set; }
        public string ModelName { get; set; }
        public string Description { get; set; }
        public string UserID { get; set; }
    }
    [BsonIgnoreExtraElements]
    public class CascadeDocument
    {
        public string CascadedId { get; set; }
        public string ClientUId { get; set; }
        public string ModelName { get; set; }
        public string DeliveryConstructUID { get; set; }
        public string CreatedByUser { get; set; }
        public bool IsCustomModel { get; set; }
        public string Category { get; set; }        
        public BsonDocument ModelList { get; set; }
        public BsonDocument Mappings { get; set; }
        public string Status { get; set; }       
    }
    public class CombinedModel
    {
        public string CascadedId { get; set; }
        public string CorrelationId { get; set; }
    }

    public class CascadeInfluencers
    {
        public string CascadedId { get; set; }
        public string UniqueId { get; set; }
        public string ModelCreatedDate { get; set; }
        public string ModelLastPredictionTime { get; set; }
        public string ModelType { get; set; }
        public string Category { get; set; }
        public string ModelVersion { get; set; }
        public bool IsVisualizationAvaialble { get; set; }
        public bool IsonlyFileupload { get; set; }
        public bool IsonlySingleEntity { get; set; }
        public bool IsMultipleEntities { get; set; }
        public bool IsBoth { get; set; }
        public List<string> InfluencersList { get; set; }
        public JArray InputSample { get; set; }
        public List<JObject> Visualization { get; set; }
    }
    public class VisulizationUpload
    {
        public string UserId { get; set; }
        public bool IsFileUpload { get; set; }
        public string CascadedId { get; set; }
        public string ModelName { get; set; }
        public string ClientUID { get; set; }
        public string DCUID { get; set; }        

    }
    public class UploadResponse
    {
        public string CascadedId { get; set; }
        public string UniqueId { get; set; }
        public string Message { get; set; }
        public string ClinetUID { get; set; }
        public string DCUID { get; set; }
        public string ValidatonMessage { get; set; }
        public string Status { get; set; }        
        public bool IsUploaded { get; set; }
        public string ErrorMessage { get; set; }
        public bool IsException { get; set; }
    }
    [BsonIgnoreExtraElements]
    public class VisualizationViewModel
    {
        public string CascadedId { get; set; }
        public string ModelCreatedDate { get; set; }
        public string ModelLastPredictionTime { get; set; }
        public string ModelType { get; set; }
        public string ModelVersion { get; set; }
        public string Category { get; set; }
        public string Status { get; set; }
        public string Progress { get; set; }
        public string Message { get; set; }
        public string ErrorMessage { get; set; }
        public string UniqueId { get; set; }
        public bool IsException { get; set; }
        public List<JObject> Visualization { get; set; }        
    }

    [BsonIgnoreExtraElements]
    public class VisulizationPrediction
    {       
        public BsonDocument Visualization { get; set; }
        public string CreatedOn { get; set; }
        public string UniqueId { get; set; }
    }
    public class ShowData
    {        
        public string CascadedId { get; set; }
        public string UniqueId { get; set; }
        public bool IsException { get; set; }
        public string ErrorMessage { get; set; }
        public List<JObject> Data { get; set; }        
    }
    public class VDSCascadeModel
    {
        public string UseCaseName { get; set; }
        public string UseCaseDescription { get; set; }
        public string ModelType { get; set; }
        public string ProblemType { get; set; }
        public string Entity { get; set; }
        public string FunctionalArea { get; set; }
        public string ClientUId { get; set; }
        public string DeliveryConstructUId { get; set; }
        public string CorrelationId { get; set; }
        public string UserId { get; set; }        
        public string OperationType { get; set; }
        public string NotificationEventType { get; set; }        
        public string CreatedDateTime { get; set; }        
    }
    public enum OperationTypes
    {
        Deleted,
        Created,
        Updated
    }
}
