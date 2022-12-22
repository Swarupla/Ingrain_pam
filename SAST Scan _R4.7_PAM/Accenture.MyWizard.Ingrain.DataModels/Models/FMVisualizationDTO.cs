using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.DataModels.Models
{
    public class FMVisualizationDTO
    {
        public string ClientUID { get; set; }
        public string DCUID { get; set; }
        public string Category { get; set; }
        public string CorrelationId { get; set; }
        public string UserId { get; set; }
        public string[] ColumnsList { get; set; }
        public JArray FMVisualizeData { get; set; }
        public bool IsFMDataAvaialble { get; set; }
        public string FMCorrelationId { get; set; }
        public bool IsException { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class FMVisualizationinProgress
    {
        public string ClientUID { get; set; }
        public string DCUID { get; set; }
        public string Category { get; set; }
        public string CorrelationId { get; set; }
        public string UserId { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
        public string Progress { get; set; }
        public string FMCorrelationId { get; set; }
        public bool IsException { get; set; }
        public string ErrorMessage { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class FMVisualizationData
    {
        public string CorrelationId { get; set; }
        public string UniqueId { get; set; }
        public string CreatedByUser { get; set; }
        public string ClientUID { get; set; }
        public string DCUID { get; set; }
        public string CreatedOn { get; set; }        
        public BsonArray Visualization { get; set; }
        public string Category { get; set; }
    }
    public class FMFileUpload
    {
        public string UserId { get; set; }
        public bool IsRefresh { get; set; }
        public string CorrelationId { get; set; }
        public string FMCorrelationId { get; set; }
        public string ModelName { get; set; }        
        public string ClientUID { get; set; }
        public string DCUID { get; set; }   
        public string Category { get; set; }
    }
    public class FMUploadResponse
    {
        public string CorrelationId { get; set; }
        public string FMCorrelationId { get; set; }
        public string RequestId { get; set; }
        public string UniqueId { get; set; }
        public string Message { get; set; }
        public string ClinetUID { get; set; }
        public string DCUID { get; set; }
        public string ValidatonMessage { get; set; }
        public string Status { get; set; }
        public bool IsUploaded { get; set; }
        public string ErrorMessage { get; set; }
        public bool IsRefresh { get; set; }
        public bool IsException { get; set; }
        public string Category { get; set; }        
    }
    public class FMVisualizeModelTraining
    {
        public string CorrelationId { get; set; }
        public string FmCorrelationId { get; set; }
        public string ModelName { get; set; }
        public bool IsModel1Completed { get; set; }
        public bool IsModel2Completed { get; set; }
        public string ProcessName { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }        
        public string Progress { get; set; }
        public string Category { get; set; }
        public string ClinetUID { get; set; }
        public string DCUID { get; set; }
        public string UserId { get; set; }
        public string ErrorMessage { get; set; }
        public string UniqueId { get; set; }
        public JArray FMVisualizationData { get; set; }
    }
    public class FMPredictionResult
    {
        public string CorrelationId { get; set; }
        public string Category { get; set; }
        public string Status { get; set; }
        public string Progress { get; set; }
        public string Message { get; set; }
        public string ErrorMessage { get; set; }
        public string FmCorrelationId { get; set; }
        public JArray FMVisualizationData { get; set; }
        public string UniqueId { get; set; }
        public string ClinetUID { get; set; }
        public string DCUID { get; set; }
        public string UserId { get; set; }
    }
    [BsonIgnoreExtraElements]
    public class FMPredictionData
    {
        public string CorrelationId { get; set; }
        public BsonArray Visualization { get; set; }
    }
    public class FMVisualization
    {
        public string CorrelationId { get; set; }
        public string UniqueId { get; set; }
        public string Category { get; set; }
        public bool IsIncremental { get; set; }
        public string ClientUID { get; set; }
        public string DCUID { get; set; }
        public string CreatedByUser { get; set; }
    }
}
