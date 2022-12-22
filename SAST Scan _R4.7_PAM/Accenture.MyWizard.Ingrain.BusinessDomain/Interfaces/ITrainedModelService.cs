using DATAMODELS = Accenture.MyWizard.Ingrain.DataModels.Models;
using System;
using System.Collections.Generic;
using System.Text;
using MongoDB.Bson;
using Newtonsoft.Json.Linq;
using Accenture.MyWizard.Ingrain.DataModels.InferenceEngineModel;
using Accenture.MyWizard.Ingrain.DataModels.Models;

namespace Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces
{
    public interface ITrainedModelService
    {
        dynamic GetTrainedModels(ModelTemplateInput oModelTemplateInput, string serviceType);

        //List<JObject> GetClusteringTrainedModel(string CorrelationId, string serviceid);
        //List<JObject> GetIETrainedModel(TrainUseCaseInput trainUseCaseInput);
    }
}
