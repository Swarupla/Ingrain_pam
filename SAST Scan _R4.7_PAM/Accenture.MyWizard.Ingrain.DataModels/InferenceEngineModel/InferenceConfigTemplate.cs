using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.DataModels.InferenceEngineModel
{
    public class InferenceConfigTemplate
    {
        [BsonId]
        public ObjectId _id { get; set; }
        public string DefaultTemplateId { get; set; }
        public string Environment { get; set; } // PAD, FDS, PAM
        public string Source { get; set; }  // File, Entity, Custom
        public string FunctionalArea { get; set; } //AD,Agile ...
        public string EntityName { get; set; } //Defect,Task...
        public string InferenceConfigType { get; set; } // MeasureAnalysis or VolumetricAnalysis
        public string MetricColumn { get; set; }
        public string DateColumn { get; set; }
        public string TrendForecast { get; set; }
        public List<string> Frequency { get; set; }
        public List<string> Dimensions { get; set; }
        public List<string> Features { get; set; }
        public List<FeatureCombinations> FeatureCombinations { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }

    }



    public class IEDefaultConfigTemplateResults
    {
        public ObjectId _id { get; set; }
        public string DefaultTemplateId { get; set; }
        public string InferenceConfigType { get; set; }
        public List<InferenceResultOuput> InferenceResults { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
    }
}
