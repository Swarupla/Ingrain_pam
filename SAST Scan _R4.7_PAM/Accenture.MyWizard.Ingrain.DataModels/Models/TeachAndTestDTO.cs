using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Accenture.MyWizard.Ingrain.DataModels.Models
{
    [BsonIgnoreExtraElements]
    public class TeachAndTestDTO
    {
        public string _id { get; set; }
        public string CorrelationId { get; set; }
        [BsonElement]
        public object InputData { get; set; }
        public string scenarioName { get; set; }
        [BsonElement]
        public object WhatIFAnalysis { get; set; }
        [BsonElement]
        public object PredictionOutCome { get; set; }
        [BsonElement]
        public object HyperTuning { get; set; }
        [BsonElement]
        public object FeatureWeights { get; set; }
        [BsonElement]
        public object StatusGraphJson { get; set; }
        [BsonElement]
        public object AccuracyGraphJson { get; set; }
        [BsonElement]
        public object ROCAUCCurvesGraphJson { get; set; }
        [BsonElement]
        public object GraphJson { get; set; }
        public string CreatedOn { get; set; }
        public int CreatedByUser { get; set; }
        public string ModifiedOn { get; set; }
        public int ModifiedByUser { get; set; }
        public List<JObject> TeachtestModelData { get; set; }
        public JObject TeachtestData { get; set; }
        public JObject FeaturePredictionData { get; set; }
        public List<FeatureImportanceModel> FeatureImportance { get; set; }
        public string ModelName { get; set; }
        public string DataSource { get; set; }
        public string BusinessProblem { get; set; }
        public string ModelType { get; set; }
        public string steps { get; set; }
        public string Message { get; set; }
        public string Category { get; set; }
        public string UploadId { get; set; }

        /// <summary>
        /// New Feature - Added to get all Columns Data  - By Shreya
        /// </summary>
        public List<object> FeatureNameList { get; set; }

        /// <summary>
        /// New Feature - Added to get all Columns Data  - By Shreya
        /// </summary>
        public List<object> TargetColUniqueValues { get; set; }
        public string NLP_Flag { get; set; }

        public string Clustering_Flag { get; set; }
    }

    public class FeatureImportanceModel
    {
        public string modelName { get; set; }
        public Dictionary<string, string> featureImportance { get; set; }
    }
    public class TeachAndTestDTOforTS
    {
        public string ModelName { get; set; }
        public string DataSource { get; set; }
        public string BusinessProblem { get; set; }
        public string ModelType { get; set; }
        public bool InstaFLag { get; set; }
        public string Category { get; set; }
        public JObject TeachtestData { get; set; }
        public List<JObject> TeachtestModelData { get; set; }

    }
}
