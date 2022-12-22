using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;
using AIGENERICSERVICE = Accenture.MyWizard.Ingrain.DataModels.AICore.GenericService;

namespace Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces
{
    public interface IAIModelPredictionsService
    {
        AIGENERICSERVICE.AIModelPredictionResponse InitiatePrediction(HttpContext httpContext);
        AIGENERICSERVICE.AIModelPredictionResponse GetModelPredictionResults(AIGENERICSERVICE.AIModelPredictionRequest aIModelPredictionRequest);
        AIGENERICSERVICE.AIModelPredictionResponse InitiateTrainAndPrediction(HttpContext httpContext,string resourceId);
    }
}
