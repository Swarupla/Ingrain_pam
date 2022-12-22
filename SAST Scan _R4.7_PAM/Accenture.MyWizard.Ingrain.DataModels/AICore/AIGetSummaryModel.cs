using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Accenture.MyWizard.Ingrain.DataModels.AICore
{
    public class AIGetSummaryModel 
    {
        [BsonId]
        public ObjectId _id { get; set; }
        public string CorrelationId { get; set; }
        public string UniId { get; set; }
        public string ActualData { get; set; }
        public string PredictedData { get; set; }

        public BsonDocument SourceDetails { get; set; }
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
}
