using Accenture.MyWizard.Ingrain.DataModels.AICore;
using Accenture.MyWizard.Ingrain.DataModels.Models;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces
{
    /// <summary>
    /// ClusteringAPI Service
    /// </summary>
    public interface IClusteringAPIService
    {
        /// <summary>
        /// Get All Clustering model details
        /// </summary>
        /// <param name="clientid"></param>
        /// <param name="dcid"></param>
        /// <param name="serviceid"></param>
        /// <param name="userid"></param>
        /// <returns></returns>
        ClusteringModel GetAllCusteringModels(string clientid, string dcid, string serviceid, string userid);

        /// <summary>
        /// Ingest Clustering data for model training
        /// </summary>
        /// <param name="clusterData"></param>
        /// <param name="httpContext"></param>
        /// <returns></returns>
        MethodReturn<Response> ClusteringAsAPI(IFormCollection clusterData, HttpContext httpContext);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        bool Evaluate(dynamic data);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="token"></param>
        /// <param name="baseUrl"></param>
        /// <param name="apiPath"></param>
        /// <param name="requestPayload"></param>
        /// <param name="isReturnArray"></param>
        /// <param name="correlationid"></param>
        /// <returns></returns>
        MethodReturn<object> RoutePOSTRequest(string token, Uri baseUrl, string apiPath, JObject requestPayload, string correlationid, bool retrain, bool isIngest);


        JObject EvaluatePythonCall(string token, Uri baseUrl, string apiPath, JObject requestPayload, string correlationid, string uniId);

        string PythonAIServiceToken();

        List<object> ClusteringViewData(string correlationId, string modelType);

        JObject DownloadPythonCall(string token, Uri baseUrl, string apiPath, JObject requestPayload);

        DownloadMappedDataStatus DownloadMappedDataStatus(string correlationId, string modelType, string pageInfo);
        VisulalisationDataStatus VisulalisationDataStatus(string correlationId, string modelType, string pageInfo);

        MethodReturn<object> AISeriveIngestData(string ModelName, string userId, string clientUID, string deliveryUID, string ParentFileName, string MappingFlag, string Source, string Category, HttpContext httpContext, bool DBEncryption, string serviceId, string Language, string pageInfo);

        MethodReturn<object> AISeriveIngestData(string ModelName, string userId, string clientUID, string deliveryUID, string ParentFileName, string MappingFlag, string Source, string Category, HttpContext httpContext, bool DBEncryption, string serviceId, string Language, string pageInfo,string E2EUID);

        MethodReturn<object> AISeriveIngestData(string ModelName, string userId, string clientUID, string deliveryUID, string ParentFileName, string MappingFlag, string Source, string Category, HttpContext httpContext, string Uploadtype, bool DBEncryption, string serviceId, string Language, string pageInfo, string E2EUID);

        ClusteringStatus ClusteringServiceIngestStatus(string correlationId, string pageInfo);

        bool GetClusteringModelName(ClusteringAPIModel clusteringAPI);

        bool ClusteringIngestData(ClusteringAPIModel clusteringAPI);
        MethodReturn<Response> GenerateWordCloud(WordCloudRequest wordCloudRequest);
        string DeleteWordCloud(string correlationId);
        Service GetAiCoreServiceDetails(string serviceid);
        void UpdateMapping(string CorrelationId, string ClientID, string DCUID, JObject mapping);
        string InsertAIServiceRequest(AIServiceRequestStatus aIServiceRequestStatus);
        void Delete_oldVisualization(string CorrelationId, string ClientID, string DCUID, string SelectedModel);

        string GetClusteringRecordCount(string CorrelationId, string UploadType, string DataSetUId);
    }
}
