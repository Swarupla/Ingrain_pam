using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Accenture.MyWizard.Ingrain.DataModels.Models
{
    public class FeatureSelectionDTO
    {
        public string _id { get; set; }
        public string CorrelationId { get; set; }
        [BsonElement]
        public object Features { get; set; }
        [BsonElement]
        public Object Split { get; set; }
        [BsonElement]
        public object TrainingDataJson { get; set; }
        [BsonElement]
        public Object ApplyKFoldvalidation { get; set; }
        public string StratifiedSampling { get; set; }
        public string CreatedOn { get; set; }
        public int CreatedByUser { get; set; }
        public string ModifiedOn { get; set; }
        public int ModifiedByUser { get; set; }
    }
}
