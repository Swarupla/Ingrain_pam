using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.DataModels.InferenceEngineModel
{
    public class IEModel
    {
        [BsonId]
        public ObjectId _id { get; set; }
        public string ModelName { get; set; }

        public string CorrelationId { get; set; }

        public string ClientUId { get; set; }

        public string DeliveryConstructUId { get; set; }
        public string UseCaseId { get; set; }
        public string ApplicationId { get; set; }

        public bool IsPrivate { get; set; }
        public bool IsMultiSource { get; set; }

        public string FunctionalArea { get; set; }

        public string Entity { get; set; }

        public string StartDate { get; set; }
        public string EndDate { get; set; }

        public bool DBEncryptionRequired { get; set; }

        public string SourceName { get; set; }   //File or Entity or Custom

        public string CreatedBy { get; set; }

        public DateTime CreatedOn { get; set; }

        public string ModifiedBy { get; set; }

        public DateTime ModifiedOn { get; set; }

        public string Status { get; set; }

        public string Progress { get; set; }

        public string Message { get; set; }
        public int MaxDataPull { get; set; }

        public bool IsIEPublish { get; set; }

        public bool IsPublishUseCase { get; set; }
    }




    public class CustomResponse
    {
        public string Status { get; set; }
        public string Message { get; set; }

        public CustomResponse(string status, string message)
        {
            this.Status = status;
            this.Message = message;
        }
    }
}
