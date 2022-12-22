using Microsoft.VisualBasic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.DataModels.Models
{
    public class ENSEntityNotification
    {
        public string EntityEventMessageUId { get; set; }
        public string EntityEventUId { get; set; }
        public string WorkItemTypeUId { get; set; }
        public string EntityUId { get; set; }
        public string ClientUId { get; set; }
        public string DeliveryConstructUId { get; set; }       
        public string SenderApp { get; set; }
        public string Message { get; set; }     
        public string CallbackLink { get; set; }
        public string EntityEventMessageStatusUId { get; set; }
        public string StatusReason { get; set; }
        public string TemplateUseCaseId { get; set; }
        public string ServiceID { get; set; }
        public string AppServiceUID { get; set; }
 
    }

    public class ENSEntityNotificationLog : ENSEntityNotification
    {
        [BsonId]
        public ObjectId _id { get; set; }
        public bool isProcessed { get; set; }
        public string ProcessedStatus { get; set; }

        public int? RetryCount { get; set; }
        public string ProcessedStatusMessage { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public string ModifiedOn { get; set; }

        public ENSEntityNotificationLog(ENSEntityNotification entityNotification)
        {
            this.EntityEventMessageStatusUId = entityNotification.EntityEventMessageStatusUId;
            this.EntityEventUId = entityNotification.EntityEventUId;
            this.WorkItemTypeUId = entityNotification.WorkItemTypeUId;
            this.EntityUId = entityNotification.EntityUId;
            this.ClientUId = entityNotification.ClientUId;
            this.DeliveryConstructUId = entityNotification.DeliveryConstructUId;
            this.SenderApp = entityNotification.SenderApp;
            this.Message = entityNotification.Message;
            this.CallbackLink = entityNotification.CallbackLink;
            this.EntityEventMessageStatusUId = entityNotification.EntityEventMessageStatusUId;
            this.StatusReason = entityNotification.StatusReason;
            this.CreatedBy = entityNotification.SenderApp;
            this.ModifiedBy = entityNotification.SenderApp;
            this.CreatedOn = DateTime.UtcNow.ToString();
            this.ModifiedOn = DateTime.UtcNow.ToString();
            this.TemplateUseCaseId = entityNotification.TemplateUseCaseId;
            this.ServiceID = entityNotification.ServiceID;
            this.AppServiceUID = entityNotification.AppServiceUID;
            this.RetryCount = 0;

        }

    }

}
