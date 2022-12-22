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
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Accenture.MyWizard.Ingrain.DataModels;
using Accenture.MyWizard.Ingrain.DataModels.Models;
using MongoDB.Bson;

namespace Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces
{
    public interface IGenericSelfservice
    {
        TemplateModelPrediction GetPublicTemplatesModelsPredictions(GenericApiData genericApiData);
        GenericDataResponse PublicTemplateModelTraning(string ClientID, string DCID, string UserId);
        List<TrainingStatus> GetDefaultModelsTrainingStatus(string clientid, string dcid, string userid);
        string AddNewApp(AppIntegration appIntegrations, string ServiceName = "");
        List<AppDetails> AllAppDetails(string clientUId, string deliveryConstructUID, string CorrelationId, string Environment, string ServiceName = "");
        List<object> GetIngestedData(string correlationId, int noOfRecord, string datecolumn);

        List<object> GetAIServiceIngestedData(string correlationId, int noOfRecord, string datecolumn);
        string GetAppModelsTrainingStatus(string clientid, string dcid, string userid);
        string UpdateMappingCollections(TemplateTrainingInput trainingRequestDetails, string resourceId);
        string GetGenericModelStatus(string clientId, string dcId, string applicationId, string usecaseId);

        GenericDataResponse ModelReTraning(List<PrivateModelDetails> models);
        GenericDataResponse TemplateModelTraining(List<TemplateTrainingInput> ModelData);
        GenericDataResponse IngrainGenericTrainingRequest(TrainingRequestDetails trainingRequestDetails, string resourceId);
        GenericDataResponse IngrainGenericTrainingResponse(string CorrelationId);
        TemplateModelPrediction IngrainGenericPredictionRequest(string CorrelationId, string actualData, string predictionCallbackUrl);
        PredictionResultDTO IngrainGenericPredictionResponse(string CorrelationId, string UniqueId);


        bool IsAppEditable(string CorrelationId);
        List<dynamic> GetVDSData(VDSParams inputParams);
        //List<dynamic> GetVDSPAMData(VDSParams inputParams); //TODO: Check can be handled in GetVDSData
        DeleteAppResponse deleteAppName(string applicationId);

        List<object> GetClusteringIngestedData(string correlationId, int noOfRecord, string datecolumn);
        PhoenixPredictionStatus PhoenixPredictionRequest(GenericApiData genericApiData);
        PhoenixPredictionsOutput GetPhoenixPrediction(PhoenixPredictionsInput phoenixPredictionsInput);
        void BulkPredictionsTest(string correlationId, int noOfRequest, int recordsPerRequest);

        void UpdateResponseData(string data);

        ModelTrainingStatus InitiateTraining(string data);
        string UpdateGenericModelMandatoryDetails(string data);
        List<IngrainResponseData> GetCallBackUrlData(string AppId, string UsecaseId, string ClientID, string DCID);
        AppIntegration GetAppDetails(string applicationId);
        string UpdateAppIntegration(AppIntegration appIntegrations);
        bool CheckRequiredDetails(string ApplicationName, string UsecaseID);
        void InsertRequests(IngrainRequestQueue ingrainRequest);
        string UpdatePublicTemplateMappingWithoutEncryption(PublicTemplateMapping templateMapping);
        void IngrainGenericDeleteOldRecordsOnRetraining(TrainingRequestDetails trainingRequestDetails, string correlationId);
        IngrainRequestQueue GetFileRequestStatus(string correlationId, string pageInfo);
        IngrainRequestQueue GetFileRequestStatusByRequestId(string correlationId, string pageInfo, string requestId);
        PhoenixPredictionsOutput GetPhoenixPredictionsStatus(PhoenixPredictionsInput phoenixPredictionsInput, string ProblemType);
        GenericDataResponse GetCascadeModelTemplateTraining(string ClientUID, string DCID, string UserId);

        string UpdatePublicTemplateMapping(PublicTemplateMapping templateMapping);

        List<CallBackErrorLog> GetAuditTrailLog(string correlationId);

        void AuditTrailLog(CallBackErrorLog auditTrailLog);

        List<FMModelTrainingStatus> GetFMModelStatus(string clientid, string dcid, string userid);
        DataPoints UpdateDataPoints(long UsecasedataPoints, long AppDataPoints, string usecaseId, string ApplicationId);

    }
}
