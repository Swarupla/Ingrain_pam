using System;
using System.Collections.Generic;
using System.Text;
using Accenture.MyWizard.Ingrain.DataModels.Models;
using Accenture.MyWizard.Ingrain.WindowService.Models;
using Accenture.MyWizard.SelfServiceAI.WindowService.Models;
using MongoDB.Bson;
using System.Net.Http;

namespace Accenture.MyWizard.Ingrain.WindowService.Interfaces
{
   public  interface IGenericAutoTrainService
    {
        GenericAutoTrain PrivateModelTraining(IngrainRequestQueue result);

        string CallbackResponse(IngrainResponseData CallBackResponse, string ApplicationName, string baseAddress, string clientId, string DCId, string applicationId, string usecaseId, string requestId, string errorTrace, string userId);

        void CallBackErrorLog(IngrainResponseData CallBackResponse, string AppName, string baseAddress, HttpResponseMessage httpResponseMessage, string clientId, string DCId, string applicationId, string usecaseId, string requestId, string errorTrace, string userId);
    }
}
