﻿using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.DataModels.InferenceEngineModel
{
    public class IESavedConfig
    {

        [BsonId]
        public ObjectId _id { get; set; }
        public string CorrelationId { get; set; }
        public List<string> ApplicationId { get; set; }
        public string InferenceConfigId { get; set; }
        public string InferenceConfigName { get; set; }
        public string InferenceSourceType { get; set; } //AutoGenerated or Manual
        public string InferenceConfigType { get; set; } // MeasureAnalysis or VolumetricAnalysis
        public bool? isFirstConfig { get; set; }
        public string MetricColumn { get; set; }
        public string DateColumn { get; set; }
        public string TrendForecast { get; set; }
        public List<string> Frequency { get; set; }
        public List<string> Dimensions { get; set; }
        public List<string> DeselectedDimensions { get; set; }
        public List<string> Features { get; set; }
        public List<string> DeselectedFeatures { get; set; }
        public List<FeatureCombinations> FeatureCombinations { get; set; }
        public List<FeatureCombinations> DeselectedFeatureCombinations { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime ModifiedOn { get; set; }

        public dynamic FilterValues { get; set; }
    }

    public class IESavedConfigResults
    {
        [BsonId]
        public ObjectId _id { get; set; }
        public string CorrelationId { get; set; }
        public string InferenceConfigId { get; set; }
        public string InferenceConfigType { get; set; }
        //public dynamic InferenceResults { get; set; }
        public string InferenceResults { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }

    }


    public class IESaveConfigInput
    {
        public string CorrelationId { get; set; }
        public string InferenceConfigId { get; set; }
        public string ConfigName { get; set; }
        public string UserId { get; set; }
        public IESaveVolumetricConfigInput VolumetricConfigInput { get; set; }
        public IESaveMetricConfigInput MetricConfigInput { get; set; }
    }


    public class IESaveVolumetricConfigInput
    {
        public string DateColumn { get; set; }
        public string TrendForecast { get; set; }
        public List<string> Frequency { get; set; }
        public List<string> Dimensions { get; set; }
        public List<string> DeselectedDimensions { get; set; }
    }

    public class IESaveMetricConfigInput
    {
        public string MetricColumn { get; set; }
        public string DateColumn { get; set; }
        public List<string> Features { get; set; }
        public List<string> DeselectedFeatures { get; set; }
        public List<FeatureCombinations> FeatureCombinations { get; set; }
        public List<FeatureCombinations> DeselectedFeatureCombinations { get; set; }
        public dynamic FilterValues { get; set; }
    }
}