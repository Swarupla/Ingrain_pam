﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization.Attributes;

namespace Accenture.MyWizard.Ingrain.DataModels.Models
{
   public class DataCleanUpDto
    {
        public string _id { get; set; }
        public string CorrelationId { get; set; }
        [BsonElement]
        public object FeatureName { get; set; }
        [BsonElement]
        public Object WhatIFAnalysis { get; set; }
        public string CreatedOn { get; set; }
        public string CreatedByUser { get; set; }
        public string ModelName { get; set; } 
        public string ModifiedOn { get; set; }
        public string ModifiedByUser { get; set; }

        public int Target_ProblemType { get; set; }
    }   
}