using Accenture.MyWizard.Ingrain.DataModels.Models;
using Accenture.MyWizard.SelfServiceAI.WindowService.Models;
using MongoDB.Bson;

namespace Accenture.MyWizard.SelfServiceAI.WindowService
{
    public interface IInstaAutoRetrainService
    {
        InstaRetrain IngestData(BsonDocument result);
        InstaRetrain GetInstaAutoDataEngineering(BsonDocument result);
        InstaRetrain GetInstaAutoModelEngineering(BsonDocument result);
        InstaRetrain GetInstaAutoDeployPrediction(BsonDocument result);
        InstaRetrain InitiateModelMonitor(BsonDocument result);
        string UpdateDeployedModelHealth(BsonDocument result);
        InstaRetrain SPAIngestData(BsonDocument result);
        InstaRetrain GetSPADeployPrediction(BsonDocument result, string modelType);

        InstaRetrain SPAAmbulanceIngestData(BsonDocument result);

        void UpdateRetrainRequestStatus(string progressPercentage, string requestId);

        void UpdateReTrainingStatus(string Status, string Message, string RequestStatus, string requestId);
    }
}
