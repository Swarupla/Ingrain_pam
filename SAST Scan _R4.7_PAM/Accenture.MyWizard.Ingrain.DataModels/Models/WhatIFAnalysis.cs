using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Accenture.MyWizard.Ingrain.DataModels.Models
{
    public class WhatIFAnalysis
    {
        public string _id { get; set; }
        public string WFId { get; set; }
        public bool bulk { get; set; }
        public string model { get; set; }
        public string CorrelationId { get; set; }
        [BsonElement]
        public object Features { get; set; }
        [BsonElement]
        public string CreatedOn { get; set; }
        public string CreatedByUser { get; set; }
        public string ModifiedOn { get; set; }
        public string ModifiedByUser { get; set; }
        public string bulkData { get; set; }
        public string Steps { get; set; }
    }
}
