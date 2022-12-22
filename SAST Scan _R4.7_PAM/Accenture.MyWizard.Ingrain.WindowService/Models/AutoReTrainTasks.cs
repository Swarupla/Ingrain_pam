using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.WindowService.Models
{
    public class AutoReTrainTasks
    {
        [BsonId]
        public ObjectId _id { get; set; }
        public string TaskUId { get; set; }
        public string TaskCode { get; set; }
        public string TaskName { get; set; }

        public bool ManualTrain { get; set; }
        public string CorrelationIds { get; set; }
        public string TimeToRun { get; set; }
        public string TimePeriod { get; set; }
        public string FrequencyInDays { get; set; }
        public string CreatedOn { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedOn { get; set; }
        public string ModifiedBy { get; set; }
    }
    public class PythonInput
    {
        public string ChangeRequest { get; set; }
        public string PChangeRequest { get; set; }
    }
}
