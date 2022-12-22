using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.DataModels.AICore
{
    public class AIServicesPrediction :ParentEntity
    {
        public string CorrelationId { get; set; }
        public string UniId { get; set; }
        public string ActualData { get; set; }
        public string PredictedData { get; set; }
        public dynamic SourceDetails { get; set; }
        public dynamic ColumnUniqueValues { get; set; }
        public dynamic lastDateDict { get; set; }
        public string PageInfo { get; set; }
        public string Status { get; set; }
        public string Progress { get; set; }
        public string Chunk_number { get; set; }
        public string ErrorMessage { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public string ModifiedOn { get; set; }

    }


    public class SimilarityPredictionRequest
    {
        public string CorrelationId { get; set; }
        public string UniqueId { get; set; }
        public int PageNumber { get; set; }
        public string Bulk { get; set; }
    }


    public class SimilarityPredictionResponse
    {
        public string CorrelationId { get; set; }
        public string UniqueId { get; set; }
        public int PageNumber { get; set; }
        public int TotalPageCount { get; set; }
        public int TotalRecordCount { get; set; }
        public string Status { get; set; }
        public string StatusMessage { get; set; }
        public dynamic PredictedData { get; set; }
    }
    public class SAPredictionStatus
    {
        public string CorrelationId { get; set; }
        public string UniqueId { get; set; }
    }
    public class SAPredictionStatusResponse
    {
        public string CorrelationId { get; set; }
        public string UniqueId { get; set; }
        public string Status { get; set; }
        public string StatusMessage { get; set; }
    }
}
