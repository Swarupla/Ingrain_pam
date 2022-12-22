#region Copyright (c) 2019 Accenture . All rights reserved.

// <copyright company="Accenture">
// Copyright (c) 2019 Accenture.  All rights reserved.
// Reproduction or transmission in whole or in part, in any form or by any means, 
// electronic, mechanical or otherwise, is prohibited without the prior written 
// consent of the copyright owner.
// </copyright>

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Accenture.MyWizard.Ingrain.DataModels;
using Accenture.MyWizard.Ingrain.DataModels.Models;
using MongoDB.Bson;
using static Accenture.MyWizard.Ingrain.BusinessDomain.Services.ModelEngineeringService;


namespace Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces
{
    public interface IDeployedModelService
    {
        RecommedAITrainedModel GetPublishedModels(string correlationId, string ServiceName = "");
        DeployModelViewModel DeployModel(dynamic data, string ServiceName = "");
        DeployModelViewModel GetDeployModel(string correlationId, string ServiceName = "");
        DeployModelsDto GetDeployModelDetails(string correlationId);
        void SavePrediction(PredictionDTO predictionDTO);
        PredictionDTO GetPrediction(PredictionDTO predictionDTO);
        object GetVisualizationData(string correlationId, string modelName,bool isPrediction);

        string PredictionModel(string correlationId, string actualData);

        PredictionResultDTO PredictionModelPerformance(string correlationId, string actualData);

        ForeCastModel ForeCastModel(string CorrelationId, string frequency, string Data);
              
        void RetrieveModel(string correlationId);
        List<DeployModelsDto> GetArchivedRecordList(string userId, string DeliveryConstructUID, string ClientUId);


    }
}
