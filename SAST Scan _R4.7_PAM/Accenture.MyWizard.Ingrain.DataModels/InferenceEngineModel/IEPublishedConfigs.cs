using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.DataModels.InferenceEngineModel
{
    public class IEPublishedConfigs
    {
        public string ApplicationId { get; set; }

        public string CorrelationId { get; set; }

        public string InferenceConfigId { get; set; }

        public string InferenceConfigType { get; set; }

        public List<string> InferenceConfigSubTypes { get; set; }

        public DateTime CreatedOn { get; set; }

        public DateTime ModifiedOn { get; set; }

        public string CreatedBy { get; set; }

        public string ModifiedBy { get; set; }
    }
}
