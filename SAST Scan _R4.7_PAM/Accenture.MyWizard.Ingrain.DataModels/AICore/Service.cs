using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.DataModels.AICore
{
    public class Service 
    {
        public string ServiceId { get; set; }
        public string ServiceCode { get; set; }
        public string ServiceName { get; set; }
        public string Description { get; set; }
        public string LongDescription { get; set; }
        public string ServiceMethod { get; set; }
        public string OperationMethod { get; set; }
        public string ApiUrl { get; set; }
        public string PrimaryTrainApiUrl { get; set; }
        public string SecondaryTrainApiUrl { get; set; }
        public string Category { get; set; } 
        public string TemplateFlag { get; set; }
        public string ApiCallInput { get; set; }
        public string ServiceInputType { get; set; }
        public bool Active { get; set; }        
        public bool IsReturnArray { get; set; }       
        public List<ServiceConfiguration> ServiceConfigurations { get; set; }
        public List<AdditionalPayloadField> AdditionalPayloadFields { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }
       
        public DateTime CreatedOn { get; set; }
        
        public DateTime ModifiedOn { get; set; }
    }

    public class IngestData
    {
        public string ServiceCode { get; set; }
        public bool IsIngestionCompleted { get; set; }
    }
   
    
    public class ServiceConfiguration
    {
        public string AlgorithmCode { get; set; }
        public string Algorithm { get; set; }
        public bool Active { get; set; }
        public string IsDefault { get; set; }
        public List<Model> Models { get; set; }
    }

   
    public class Model
    {
        public bool IsArchive { get; set; }
        public bool IsActive { get; set; }
        public bool IsCustomModel { get; set; }
        public bool IsDefault { get; set; }
        public string ModelName { get; set; }
        public string ModelPath { get; set; }
        public string ScriptPath { get; set; }
        public string CreatedBy { get; set; }
        public string Description { get; set; }
        
        public DateTime CreatedOn { get; set; }
        public string ModelId { get; set; }
        public string Algorithm { get; set; }
        public string AlgorithmCode { get; set; }
    }

  
    public class AdditionalPayloadField
    {
        public string Field { get; set; }
        public string PayloadField { get; set; }
        public string ScriptPath { get; set; }
        public bool IsCustomModel { get; set; }
        public string ModelPath { get; set; }
    }

   
    
}
