using Accenture.MyWizard.Ingrain.DataModels.Models;
using Microsoft.AspNetCore.Http;
using AIGENERICSERVICE = Accenture.MyWizard.Ingrain.DataModels.AICore.GenericService;

namespace Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces
{
    public interface ICustomConfigService
    {
        object GetCustomConfigurations(string serviceType, string serviceLevel);
        void SaveCustomConfigurations(HttpContext httpContext, string ServiceLevel, dynamic dynamicData);
        GenericTraining StartTraining(TrainingRequestDTO RequestPayload);
        AIGENERICSERVICE.TrainingResponse TrainAIServiceModel(HttpContext httpContext, string resourceId);
        void AuditTrailLog(CallBackErrorLog auditTrailLog);
        bool IsAmbulanceLane(string useCaseId);
        long GetDatapoints(string useCaseId, string applicationId);
    }
}
