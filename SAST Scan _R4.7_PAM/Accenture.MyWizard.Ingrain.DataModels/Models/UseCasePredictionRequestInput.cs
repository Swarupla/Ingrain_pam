using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.DataModels.Models
{
    public class UseCasePredictionRequestInput
    {
        public string ClientUId { get; set; }
        public string DeliveryConstructUId { get; set; }
        public string UseCaseId { get; set; }
        public string ModelType { get; set; }
        public string Frequency { get; set; }
    }

    public class UseCasePredictionRequestOutput
    {
        public string UseCaseId { get; set; }
        public string ModelType { get; set; }
        public string UniqueId { get; set; }
        public string Status { get; set; }
        public string StatusMessage { get; set; }

    }

    public class UseCasePredictionResponseInput
    {
        public string UniqueId { get; set; }
        public string UseCaseId { get; set; }
        public string PageNumber { get; set; }
    }
    public class UseCasePredictionResponseOutput
    {
        public string UniqueId { get; set; }
        public string UseCaseId { get; set; }
        public string ModelType { get; set; }
        public string PageNumber { get; set; }
        public string Status { get; set; }
        public string ErrorMessage { get; set; }
        public string ActualData { get; set; }
        public string PredictedData { get; set; }
        public List<string> AvailablePages { get; set; }

    }



    public class VdsUseCaseTrainingRequest
    {
        public string ClientUId { get; set; }
        public string E2EUId { get; set; }
        public string DeliveryConstructUId { get; set; }
        public string ApplicationId { get; set; }
        public string UseCaseId { get; set; }
        public string UserId { get; set; }
        public string Retrain { get; set; }
    }

    public class VdsUseCaseTrainingResponse
    {
        public string ClientUId { get; set; }
        public string DeliveryConstructUId { get; set; }
        public string ApplicationId { get; set; }
        public string UseCaseId { get; set; }
        public string Status { get; set; }
        public string StatusMessage { get; set; }

        public VdsUseCaseTrainingResponse(string clientUId, string deliveryConstructUId, string applicationId, string useCaseId)
        {
            this.ClientUId = clientUId;
            this.DeliveryConstructUId = deliveryConstructUId;
            this.ApplicationId = applicationId;
            this.UseCaseId = useCaseId;
        }

    }
}