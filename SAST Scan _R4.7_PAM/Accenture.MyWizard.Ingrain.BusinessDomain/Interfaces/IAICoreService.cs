using Accenture.MyWizard.Ingrain.DataModels.AICore;
using AIGENERICSERVICE = Accenture.MyWizard.Ingrain.DataModels.AICore.GenericService;
using USECASE = Accenture.MyWizard.Ingrain.DataModels.AICore.UseCase;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Accenture.MyWizard.Ingrain.DataModels.Models;

namespace Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces
{
    public interface IAICoreService
    {
        List<Service> GetAllAICoreServices();
        string AddAICoreService(Service service);
        bool CreateAICoreModel(string clientid, string deliveryconstructid, string serviceid, string corrid, string uniId, string modelName, string pythonModelName, string modelStatus, string statusMessage, string userid, string dataSource, string pageInfo,string datasetUId,int maxDataPull);
        List<AICoreModels> GetAllAICoreModels();
        Service GetAiCoreServiceDetails(string serviceid);
        AICoreModels GetAICoreModelPath(string correlationid);

        AIModelStatus GetAIModelTrainingStatus(string correlationid);
        ModelsList GetAICoreModels(string clientid, string dcid, string serviceid, string userid);
        MethodReturn<object> RouteGETRequest(string token, Uri baseUrl, string apiPath, bool isReturnArray);
        MethodReturn<object> RoutePOSTRequest(string token, Uri baseUrl, string apiPath, IFormFileCollection fileCollection,
           JObject requestPayload, string[] fileKeys, bool isReturnArray, string correlationid);
        MethodReturn<object> RoutePOSTRequest(string token, Uri baseUrl, string apiPath, JObject requestPayload, bool isReturnArray);
        MethodReturn<object> AISeriveIngestData(string ModelName, string userId, string clientUID, string deliveryUID, string ParentFileName, string MappingFlag, string Source, string Category, HttpContext httpContext,string Uploadtype, bool DBEncryption, string serviceId, string Language, string E2EUID);
        AIServiceRequestStatus AIServiceIngestStatus(string correlationId, string pageInfo);

        void UpdateSelectedColumn(dynamic selectedColumns, string correlationId);
        string InsertTrainrequest(dynamic selectedColumns, string correlationid, dynamic filterAttribute);
        string InsertTrainrequestBulkMultiple(dynamic selectedColumns, string correlationid, dynamic filterAttribute, string scoreUniqueName, dynamic threshold_TopnRecords, dynamic stopWords, int MaxDataPull);
       AIGENERICSERVICE.BulkPrediction.BulkPredictionData GetBulkPredictionDetails(string correlationId);
        AIGENERICSERVICE.TrainingResponse TrainAIServiceModel(HttpContext httpContext, string resourceId);
        //AIGENERICSERVICE.TrainingResponse TrainAIServiceModel(AIGENERICSERVICE.TrainingRequest trainingRequest, HttpContext httpContext,string resourceId);
        AIGENERICSERVICE.BulkTraining.BulkTrainingResponse TrainAIServiceModelBulkTrain(HttpContext httpContext);
        object SaveUsecase(USECASE.UsecaseDetails usecaseDetails);

        List<USECASE.UsecaseDetails> FetchUseCaseDetails(string serviceId);


        JObject RetrainModelDetails(string correlationId);

        MethodReturn<Response> DeveloperPredictEvaluate(DeveloperPredictRequest developerPredictRequest);
        USECASE.UsecaseDetails GetUsecaseDetails(string usecaseId);
        bool CheckAICoreModelByUsecaseId(string clientId, string deliveryConstructId, string applicationId, string serviceId, string usecaseId);

        MethodReturn<Response> DeveloperPredictionTraining(string clientId, string deliveryConstructId, string serviceId, string applicationId, string usecaseId, string modelName, string userId, bool isTrain, string correlationId, bool retrain);
        string InsertAIServiceRequest(AIServiceRequestStatus aIServiceRequestStatus);
        void InsertAIServicePredictionRequest(AICoreModels aICoreModels);
        string DeleteAIModel(string correlationId);
        string DeleteAIUsecase(string usecaseId);

        void UpdateTokenInAppIntegration(string resourceId, string applicationId);
        AICoreModels GetUseCaseDetails(string clientId, string deliverConstructId, string usecaseId, string serviceId, string userId);

        void InsertTextSummary(AIGetSummaryModel aIServicesPrediction);

        AIGetSummaryModel GetTextSummaryStatus(string correlationid, bool isSource);

        void UpdateAICoreModels(string correlationId, string status, string message);
        void UpdateAIServiceRequestStatus(string corrId, string uniId, string status, string message);
        SimilarityPredictionResponse GetSimilarityPredictions(SimilarityPredictionRequest similarityPredictionRequest);
        AIGENERICSERVICE.BulkTraining.AIServiceStatusResponse GetAIServiceRequestStatus(AIGENERICSERVICE.BulkTraining.AIServiceStatusRequest aIServiceRequestStatus);
        void SendAppNotification(AppNotificationLog appNotificationLog);
        void SendTONotifications(AICoreModels aICoreModels, string status, string message);

        string GetSimilarityRecordCount(string CorrelationId);
        SAPredictionStatusResponse GetSAMultipleBulkPredictionStatus(SAPredictionStatus SAPredictionStatus);
        
        ModelsList GetAICoreModelsAESKeyVault(string clientid, string dcid, string serviceid, string userid);
    }
}
