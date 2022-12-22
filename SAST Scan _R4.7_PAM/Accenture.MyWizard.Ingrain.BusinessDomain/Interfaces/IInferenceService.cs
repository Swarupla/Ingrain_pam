﻿using Accenture.MyWizard.Ingrain.DataModels.InferenceEngineModel;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces
{
    public interface IInferenceService
    {
        string IEUploadFiles(string CorrelationId, string ModelName, string userId, string clientUID, string deliveryUID, string ParentFileName, string MappingFlag, string Source, string Category, Type type, string E2EUID, HttpContext httpContext, string ClusterFlag, string EntityStartDate, string EntityEndDate, bool DBEncryption, string oldCorrelationID, string Language, bool IsModelTemplateDataSource, string CorrelationId_status, out string requestQueueStatus, string usecaseId, string UploadFileType);

        IEFileUploadColums IEGetFilesColumns(string correlationId, string ParentFileName, string ModelName);

        string IEGetIngrainRequestCollection(string userId, string uploadfiletype, string mappingflag, string correlationid, string pageInfo,
        string modelname, string clientUID, string deliveryUID, HttpContext httpContext, string category, string uploadtype, string statusFlag);

        List<IEModel> GetIEModelData(string clientId, string DCId, string userId, string FunctionalArea) ;

        List<ModelInferences> GetModelInferences(string correlationId, string applicationId, string inferenceConfigId, bool autogenerated);

        string DeleteIEModel(string correlationId);

        IEConfig GetDateMeasureAttribute(string correlationId);
        CustomResponse AutoGenerateInferences(string correlationId, bool regenerate);

        IEREsponse TriggerFeatureCombination(string correlationId, string userId, string inferenceConfigId, dynamic dynamicColumns, bool isNewRequest);

        IEFeatureCombination GetFeatureCombination(string correlationId, string metricColumn, string dateColumn);
        CustomResponse SaveInferenceConfiguration(IESaveConfigInput iESaveConfigInput);

        IEViewConfigResponse ViewConfiguration(string correlationId, string InferenceConfigId);

        CustomResponse DeleteConfig(string inferenceConfigId);

        IEREsponse GenerateInference(string correlationId, string inferenceConfigId, string userId, bool isNewRequest);

        IEFeatureCombination GetFeatureCombinationOnId(string correlationId, string requestId);

        string PublishInference(List<IEPublishedConfigs> iEPublishedConfigs);

        List<ModelInferences> GetInferences(string correlationId, string applicationId, string inferenceConfigId, bool rawresponse=false, bool isGenericAPI=false);
        List<IEAppDetails> GetAllAPPDetails(string environment);

        List<IEPublishedConfigs> GetPublishedInferences(string correlationId, string applicationId, string inferenceConfigId);
        string AddNewApp(IEAppIntegration appIntegrations);
        CustomResponse CreateUseCase(AddUseCaseInput addUseCaseInput);

        CustomResponse DeleteIEUseCase(string useCaseId);
        List<UseCaseDetails> GetAllUseCases();

        TrainUseCaseOutput TrainUseCase(TrainUseCaseInput trainUseCaseInput);

        TrainUseCaseOutput GetTrainingStatus(string correlationId);

        List<object> GetIEIngestedData(string correlationId, int noOfRecord, string datecolumn);

        List<object> ViewUploadedData(string correlationId, int DecimalPlaces);


    }
}
