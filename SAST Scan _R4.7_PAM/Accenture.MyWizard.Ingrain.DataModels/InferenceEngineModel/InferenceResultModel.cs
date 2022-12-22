using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.DataModels.InferenceEngineModel
{

    

    public class InferenceResultOuput
    {
        public List<Inference> MeasureAnalysisInferences { get; set; }
        public List<Inference> VolumetricInferences { get; set; }
    }  

    public class Inference
    {
        public string DisplayName { get; set; }

        public dynamic Value { get; set; }
    }

    //public class VolumetricInference
    //{
    //    public string DisplayName { get; set; }

    //    public List<Inference> Value { get; set; }
    //}



    public class ModelInferences
    {
        public string CorrelationId { get; set; }
        public string ClientUId { get; set; }
        public string DeliveryConstructUId { get; set; }
        public string ApplicationId { get; set; }
        public string ModelName { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string FunctionalArea { get; set; }
        public string EntityName { get; set; }
        public string InferenceConfigId { get; set; }
        public string InferenceSourceType { get; set; }
        public string InferenceConfigType { get; set; }
        public string InferenceName { get; set; }

        public string Status { get; set; }
        public string Progress { get; set; }
        public dynamic InferenceConfigDetails { get; set; }
        public string CreatedOn { get; set; }
        public string CreatedByUser { get; set; }
        public string ModifiedOn { get; set; }
        public string ModifiedByUser { get; set; }
        public List<InferenceResultOuput> InferenceResults { get; set; }
        public string InferenceRawResults { get; set; }
    }





}
