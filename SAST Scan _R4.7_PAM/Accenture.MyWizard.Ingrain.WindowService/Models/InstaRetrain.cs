using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace Accenture.MyWizard.SelfServiceAI.WindowService.Models
{
    public class InstaRetrain
    {
        public string Status { get; set; }
        public bool Success { get; set; }
        public string PageInfo { get; set; }
        public bool IsPredictionSucess { get; set; }
        public string Message { get; set; }
        public string ErrorMessage { get; set; }
    }
    public class TrainedModels
    {
        public string CorrelationId { get; set; }
        public bool IsSuccess { get; set; }
        public string UserID { get; set; }
    }
    public class RegressionRetrain
    {
        public string Status { get; set; }
        public bool Success { get; set; }
        public string PageInfo { get; set; }
        public bool IsPredictionSucess { get; set; }
        public string Message { get; set; }
        public string ErrorMessage { get; set; }
    }
    [BsonIgnoreExtraElements]
    public class DeployModel
    {
        public string CorrelationId { get; set; }
        public string InstaId { get; set; }
        public string UseCaseID { get; set; }
        public string Frequency { get; set; }
        public string ClientUId { get; set; }
        public string DeliveryConstructUID { get; set; }
        public string ModelType { get; set; }
        public string SourceName { get; set; }
        public string DataSource { get; set; }
        public string ModelName { get; set; }
        public string ModelVersion { get; set; }       
        public string CreatedByUser { get; set; }
    }
}
