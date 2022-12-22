using Accenture.MyWizard.Ingrain.DataModels.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces
{
    public interface IIAService
    {
        GenericDataResponse InitiateTrainingRequest(TrainingRequestDetails trainingRequestDetails, string resourceId);
        PredictionResultDTO InitiatePrediction(IAPredictionRequest iAPredictionRequest);
        IAUseCasePredictionResponse GetUseCasePrediction(IAUseCasePredictionRequest iAUseCasePredictionRequest);
    }
}
