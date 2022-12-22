using System;

namespace Accenture.MyWizard.SelfServiceAI.WindowService.Models
{
    public class InstaLog
    {
        public string _id { get; set; }
        public string InstaId { get; set; }
        public string ModelName { get; set; }
        public string CorrelationId { get; set; }
        public string Status { get; set; }
        public string DeliveryConstructUID { get; set; }
        public string SourceName { get; set; }
        public string ModelVersion { get; set; }
        public string ClientUId { get; set; }
        public string PageInfo { get; set; }
        public string Message { get; set; }
        public string ErrorMessage { get; set; }
        public string StartDate { get; set; }
        public string CreatedOn { get; set; }
        public string CreatedByUser { get; set; }
        public string ModifiedOn { get; set; }
        public string ModifiedByUser { get; set; }

        public InstaLog()
        {

        }

        public InstaLog(string correlationId, string modelName, string pageInfo, string message, string errorMessage, string userId)
        {
            this._id = Guid.NewGuid().ToString();
            this.CorrelationId = correlationId;
            this.ModelName = modelName;
            this.PageInfo = pageInfo;
            this.Message = message;
            this.ErrorMessage = errorMessage;
            this.CreatedByUser = userId;
            this.CreatedOn = DateTime.UtcNow.ToString();
            this.ModifiedByUser = userId;
            this.ModifiedOn = DateTime.UtcNow.ToString();

        }
    }


   
}
