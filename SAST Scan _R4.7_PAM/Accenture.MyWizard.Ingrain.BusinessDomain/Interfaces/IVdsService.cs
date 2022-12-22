using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Accenture.MyWizard.Ingrain.DataModels.Models;
using System.Threading.Tasks;

namespace Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces
{
    public interface IVdsService
    {
        VDSModelDTO VDSModelDetails(string correlationId,string modelType);        

        VDSViewModelDTO GetVDSModels(string clientUID, string deliveryConstructUID, string modelType);
        VDSViewModelDTO GetVDSManagedInstanceModels(string clientUID, string deliveryConstructUID, string modelType, string environment, string requestType);
        VDSModelDTO VDSManagedInstanceModelDetails(string correlationId, string modelType);
        VdsUseCaseDto VDSUseCaseDetails(string usecaseId);
        UseCasePredictionRequestOutput GetUseCasePredictionRequest(UseCasePredictionRequestInput useCasePredictionRequestInput);
        UseCasePredictionResponseOutput GetUseCasePredictionResponse(UseCasePredictionResponseInput useCasePredictionResponseInput);
        VdsUseCaseTrainingResponse TrainVDSUseCase(VdsUseCaseTrainingRequest vDSUseCaseTrainingRequest);
        GenericModelTrainingResponse StartGenericModelTraining(GenericModelTrainingRequest GenericRequest);
        GenericModelTrainingResponse IngrainAIAppsGenericTrainingResponse(string CorrelationId);
        VDSUseCasePredictionOutput IntiateVDSGenericModelPrediction(VDSUseCasePredictionRequest VDSUseCasePredictionRequestInput);
        VDSPredictionResponseOutput GetVDSGenericModelPrediction(VDSPredictionResponseInput useCasePredictionResponseInput);
    }
}
