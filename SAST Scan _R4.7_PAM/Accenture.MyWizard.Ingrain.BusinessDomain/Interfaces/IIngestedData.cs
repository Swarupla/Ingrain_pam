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
using Accenture.MyWizard.Ingrain.DataModels.Models;
using MongoDB.Bson;
using static Accenture.MyWizard.Ingrain.BusinessDomain.Services.IngestedDataService;
using Microsoft.AspNetCore.Http;

namespace Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces
{
    public interface IIngestedData
    {
        void InsertData(IngestedDataDTO ingestedData);
        void AddFiledToSSAIBAL(string correlationId, dynamic jsonColumns);
        string GetPytonProcessData(string correlationId, string pageInfo);
        UserColumns GetColumns(string correlationId, bool IsTemplate, bool newModel, string ServiceName = "");
        List<PublishDeployedModel> GetPublishModels(string userId, string dateFilter, string DeliveryConstructUID, string ClientUId, string ServiceName = "");
        void ColumnsFlushforBusinessProblem(string correlationId, string userId, string clientUID, string deliveryUID);
        UseCaseSave InsertColumns(BusinessProblemDataDTO data, string ServiceName = "");
        PublicTemplateModel GetPublicTemplates(string searchText, string ServiceName = "");
        void InsertDeployModels(IngestedDataDTO data, bool encryptionFlag, bool IsModelTemplateDataSource, string dataSetUId, string usecaseId);
        void DeleteDeployedModel(string correlationid);
        void AssignValues(IngestedDataDTO ingestedData, dynamic dynaimcFileData, double rowsPerDocument, List<string> columns, IFormFile postedFile);
        void DataSourceAssignValues(IngestedDataDTO ingestedData, dynamic dynaimcFileData, double rowsPerDocument, List<string> columns, IFormFile postedFile, string correlationId);
        void InsertPreProcessData(PreProcessDataDto data);
        void InsertDataSource(IngestedDataDTO ingestedData);
        void InsertDataSourceDeployModels(IngestedDataDTO data, bool encryptionFlag,string dataSetUId);
        string RemoveColumns(string correlationId, string[] prescriptionColumns);
        void DeleteIngestUseCase(string correlationId);
        bool IsModelNameExist(string modelName, string ServiceName = "");
        void InsertRequests(IngrainRequestQueue ingrainRequest, string ServiceName = "");
        IngrainRequestQueue GetFileRequestStatus(string correlationId, string pageInfo);
        IngrainRequestQueue GetFileRequestStatus(string correlationId, string pageInfo, string wfId);
        List<IngrainRequestQueue> GetMultipleRequestStatus(string correlationId, string pageInfo);
        string GetRequestUsecase(string correlationId, string pageInfo, string ServiceName = "");
        FileUploadColums GetFilesColumns(string correlationId, string ParentFileName, string ModelName, string ServiceName = "");
        string GetIngrainRequestCollection(string userId, string uploadfiletype, string mappingflag, string correlationid, string pageInfo,
            string modelname, string clientUID, string deliveryUID, HttpContext httpContext, string category,string uploadtype, string statusFlag);
        string DataSourceUploadFiles(string correlationId, string ModelName, string userId, string clientUID, string deliveryUID, string ParentFileName, string MappingFlag, string Source, string Category, Type type, string E2EUID, HttpContext httpContext, string ClusterFlag, string EntityStartDate, string EntityEndDate, bool DBEncryption, string oldCorrelationID, string Language, string CorrelationId_status, string usecaseId, string ServiceName = "");
        //string UploadFiles(string CorrelationId, string ModelName, string userId, string clientUID, string deliveryUID, string ParentFileName, string MappingFlag, string Source, string Category, Type type, string E2EUID, HttpContext httpContext,string ClusterFlag,string EntityStartDate, string EntityEndDate, bool DBEncryption, string oldCorrelationID, string Language, bool IsModelTemplateDataSource);
        string UploadFiles(string CorrelationId, string ModelName, string userId, string clientUID, string deliveryUID, string ParentFileName, string MappingFlag, string Source, string Category, Type type, string E2EUID, HttpContext httpContext, string ClusterFlag, string EntityStartDate, string EntityEndDate, bool DBEncryption, string oldCorrelationID, string Language, bool IsModelTemplateDataSource,string CorrelationId_status, out string requestQueueStatus, string useCaseId, string ServiceName = "");
        string GetDefaultEntityName(string correlationid);
        /// <summary>
        /// View Uploaded Excel Data
        /// </summary>
        /// <param name="correlationId"></param>
        /// <returns></returns>
        List<object> ViewUploadedData(string correlationId, int DecimalPlaces, string ServiceName = "");
        dynamic DownloadTemplateFun(string correlationId);
        
        Inputvalidation GetInputvalidation(string correlationId, string pageInfo, string userId, string deliveryConstructUID, string clientUId, bool isTemplateModel);
        ValidRecordsDetailsModel GetDataPoints(string correlationId);
    }
}
