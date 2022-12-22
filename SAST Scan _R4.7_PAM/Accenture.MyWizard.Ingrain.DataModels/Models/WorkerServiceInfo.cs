using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.DataModels.Models
{
    [BsonIgnoreExtraElements]
    public class WorkerServiceInfo
    {
        [BsonElement("Message")]
        public string Message { get; set; }

        [BsonElement("ErrorMessage")]
        public string ErrorMessage { get; set; }

        [BsonElement("Status")]
        public string Status { get; set; }

        [BsonElement("CreatedBy")]
        public string CreatedBy { get; set; }

        [BsonElement("CreatedOn")]
        public string CreatedOn { get; set; }

        [BsonElement("ModifiedBy")]
        public string ModifiedBy { get; set; }

        [BsonElement("ModifiedOn")]
        public string ModifiedOn { get; set; }

        [BsonElement("LastStartedOn")]
        public string LastStartedOn { get; set; }

        [BsonElement("LastStoppedOn")]
        public string LastStoppedOn { get; set; }

        [BsonElement("Environment")]
        public string Environment { get; set; }

        [BsonElement("ProcessName")]
        public string ProcessName { get; set; }

    }

}
