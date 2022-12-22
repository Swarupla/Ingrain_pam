using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.WindowService.Models
{
    [BsonIgnoreExtraElements]
    public class CustomAppsActivityLog
    {
        [BsonId]
        public ObjectId _id { get; set; }
        public string CorrelationId { get; set; }
        public string RequestId { get; set; }
        public string UseCaseId { get; set; }
        public string UniqueId { get; set; }
        public string BaseAddress { get; set; }
        public string Message { get; set; }
        public string ErrorMessage { get; set; }

        public string ErrorMethod { get; set; }

        public string StackTrace { get; set; }
        public string Status { get; set; }
        //public string CallbackURLResponse { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public string ModifiedOn { get; set; }
        public string ApplicationID { get; set; }
        public string ClientId { get; set; }
        public string DCID { get; set; }
        public string FeatureName { get; set; }
        public string Environment { get; set; }
        public string ProcessName { get; set; }

        public string UsageType { get; set; }

        //public HttpResponseMessage httpResponse { get; set; }

        public string ApplicationName { get; set; }
        public string AppServiceUID { get; set; }

    }
}
