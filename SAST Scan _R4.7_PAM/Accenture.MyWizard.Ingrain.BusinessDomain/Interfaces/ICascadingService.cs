using Accenture.MyWizard.Ingrain.DataModels.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces
{
    public interface ICascadingService
    {
        CascadeModel GetCascadingModels(string clientUid, string dcUid, string userId, string category, string cascadeId);
        CascadeSaveModel SaveCascadeModels(CascadeCollection data);
        CascadeModelMapping GetMappingModels(string cascadeId);
        UpdateCascadeModelMapping UpdateCascadeMapping(UpdateCasecadeModel data);
        CascadeDeployViewModel GetDeployedModel(string correlationId);
        CustomCascadeModel GetCustomCascadeModels(string clientUid, string dcUid, string userId, string category);
        UpdateCascadeModelMapping SaveCustomCascadeModels(CascadeCollection data);
        CustomMapping GetCascadeIdDetails(string sourceCorid, string targetCorid, string cascadeId, string UniqIdName, string UniqDatatype, string TargetColumn);
        CustomModelViewDetails GetCustomCascadeDetails(string cascadeId);
        CascadeVDSModels GetCascadeVDSModels(string ClientUID, string DCUID, string UserID, string Category, out bool isException, out string ErrorMessage);
        CascadeInfluencers GetInfluencers(string CascadedId, out bool isException, out string ErrorMessage);
        UploadResponse UploadData(VisulizationUpload visulization, IFormFileCollection fileCollection, out bool isException, out string ErrorMessage);
        VisualizationViewModel GetCascadePrediction(string correlationId, string uniqId);
        ShowData ShowData(string cascadeId, string uniqueId);
        FMVisualizationDTO GetFmVisualizeDetails(string ClientUID, string DCUID, string UserID, string Category);
        FMVisualizationinProgress GetFMVisualizationinProgress(string ClientUID, string DCUID, string UserID, string Category);
        FMUploadResponse FmFileUpload(FMFileUpload fMFileUpload, IFormFileCollection fileCollection, out bool isException, out string ErrorMessage);
        FMVisualizeModelTraining GetFMModelTrainingStatus(string correlationId, string FMCorrelationId, string userId);
        FMPredictionResult GetFMPrediction(string correlationId, string UniqId);
    }
}
