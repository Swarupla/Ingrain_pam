using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Accenture.MyWizard.Ingrain.DataModels.Models
{
    public class PreProcessDataDto
    {
        public string _id { get; set; }
        public string CorrelationId { get; set; }
        [BsonElement]
        public object MissingValues { get; set; }
        [BsonElement]
        public Object Filters { get; set; }
        [BsonElement]
        public object DataModification { get; set; }
        [BsonElement]
        public Object DataEncoding { get; set; }
        public string CreatedOn { get; set; }
        public int CreatedByUser { get; set; }
        public string ModifiedOn { get; set; }
        public int ModifiedByUser { get; set; }
    }
}
