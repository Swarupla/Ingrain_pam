#region Copyright (c) 2019 Accenture . All rights reserved.

// <copyright company="Accenture">
// Copyright (c) 2019 Accenture.  All rights reserved.
// Reproduction or transmission in whole or in part, in any form or by any means, 
// electronic, mechanical or otherwise, is prohibited without the prior written 
// consent of the copyright owner.
// </copyright>

#endregion

#region Controller Information
/********************************************************************************************************\
Module Name     :   ProblemStatementController
Project         :   Accenture.MyWizard.SelfServiceAI.ProblemStatementService
Organisation    :   Accenture Technologies Ltd.
Created By      :   RaviA
Created Date    :   02-Jan-2019
Revision History :
Revision No             Initial             Modified Date           Description
1.0                     An                  02-Jan-2019             
\********************************************************************************************************/
#endregion


namespace Accenture.MyWizard.SelfServiceAI.ProblemStatementService
{
    #region Namespace References
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using LOGGING = Accenture.MyWizard.LOGGING;
    using MongoDB.Bson;
    using Accenture.MyWizard.Ingrain.WebService;
    using Accenture.MyWizard.Ingrain.DataModels.Models;
    using Microsoft.AspNetCore.Mvc;
    using Accenture.MyWizard.Ingrain.WebService.Controllers;
    using Microsoft.Extensions.Options;
    using Accenture.MyWizard.Shared.Helpers;
    using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.AspNetCore.Http;
    using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
    using System.Net.Http;
    using Ninject;
    using System.Text.RegularExpressions;
    using Accenture.MyWizard.Ingrain.BusinessDomain.Common;
    #endregion
    public class ProblemStatementController : MyWizardControllerBase
    {
        #region Members      
        private IngestedDataDTO _ingestedData = null;
        private List<PublishDeployedModel> _publishModelDTOs = null;
        private BusinessProblemDataDTO _businessProblemData = null;
        private readonly IOptions<IngrainAppSettings> appSettings;
        private static IIngestedData ingestedDataService { set; get; }
        private static IProcessDataService processDataService { set; get; }
        IngrainRequestQueue requestQueue = new IngrainRequestQueue();
        PythonCategory pythonCategory = new PythonCategory();
        PythonInfo pythonInfo = new PythonInfo();
        #endregion

        #region Constructors    
        /// <summary>
        /// Constructor to Initialize the objects
        /// </summary>
        public ProblemStatementController(IServiceProvider serviceProvider, IOptions<IngrainAppSettings> settings)
        {
            _ingestedData = new IngestedDataDTO();
            appSettings = settings;
            ingestedDataService = serviceProvider.GetService<IIngestedData>();
            processDataService = serviceProvider.GetService<IProcessDataService>();
        }
        #endregion

        #region Methods 

        [HttpPost]
        [Route("api/UploadMappingColumns")]
        public IActionResult UploadMappingColumns(string userId, string uploadfiletype, string mappingflag, string correlationid,
            string modelname, string clientUID, string deliveryUID, string category, string uploadtype, string statusFlag)
        {
            bool flag = true;
            string PageInfo = "IngestData";
            List<string> UpdateColumnsData = new List<string>();
            PythonCategory pythonCategory = new PythonCategory();
            PythonInfo pythonInfo = new PythonInfo();
            try
            {
                if (mappingflag == "True")
                {
                    #region VALIDATIONS
                    if (!CommonUtility.GetValidUser(userId))
                        return GetFaultResponse(Resource.IngrainResx.InValidUser);

                    CommonUtility.ValidateInputFormData(uploadfiletype, CONSTANTS.UploadFileType, false);
                    CommonUtility.ValidateInputFormData(mappingflag, CONSTANTS.MappingFlag, false);
                    CommonUtility.ValidateInputFormData(correlationid, CONSTANTS.CorrelationId, true);
                    CommonUtility.ValidateInputFormData(modelname, CONSTANTS.ModelName, false);
                    CommonUtility.ValidateInputFormData(clientUID, CONSTANTS.ClientUID, true);
                    CommonUtility.ValidateInputFormData(deliveryUID, CONSTANTS.DeliveryConstructUID, true);
                    CommonUtility.ValidateInputFormData(category, CONSTANTS.Category, false);
                    CommonUtility.ValidateInputFormData(uploadtype, CONSTANTS.Uploadtype, false);
                    CommonUtility.ValidateInputFormData(statusFlag, CONSTANTS.StatusFlag, false);
                    #endregion

                    string result = ingestedDataService.GetIngrainRequestCollection(userId, uploadfiletype, mappingflag, correlationid, PageInfo, modelname,
                          clientUID, deliveryUID, HttpContext, category, uploadtype, statusFlag);
                    switch (result)
                    {
                        case CONSTANTS.Success:
                            UpdateColumnsData.Add(correlationid);
                            UpdateColumnsData.Add(Resource.IngrainResx.UploadFile);
                            break;

                        case CONSTANTS.PhythonError:
                            //requestQueue = IngestedData.GetFileRequestStatus(correlationId, Constant.IngestData);
                            requestQueue = ingestedDataService.GetFileRequestStatus(correlationid, "IngestData");
                            pythonCategory.Category = "Error";
                            pythonCategory.Message = requestQueue.Message;
                            pythonInfo.Category = pythonCategory;
                            pythonInfo.Status = requestQueue.Status;
                            return GetFaultResponse(pythonInfo);

                        case CONSTANTS.PhythonInfo:
                            requestQueue = ingestedDataService.GetFileRequestStatus(correlationid, "IngestData");
                            pythonCategory.Category = "Error";
                            pythonCategory.Message = requestQueue.Message;
                            pythonInfo.Category = pythonCategory;
                            pythonInfo.Status = requestQueue.Status;
                            return GetFaultResponse(pythonInfo);

                        case CONSTANTS.PhythonProgress:
                            requestQueue = ingestedDataService.GetFileRequestStatus(correlationid, "IngestData");
                            pythonCategory.Category = "Progress";
                            pythonCategory.Message = requestQueue.Message;
                            pythonInfo.Category = pythonCategory;
                            pythonInfo.Status = requestQueue.Status;
                            return GetSuccessWithMessageResponse(pythonInfo);

                        case CONSTANTS.New:
                            requestQueue = ingestedDataService.GetFileRequestStatus(correlationid, "IngestData");
                            pythonCategory.Category = "New";
                            pythonCategory.Message = requestQueue.Message;
                            pythonInfo.Category = pythonCategory;
                            pythonInfo.Status = "P";
                            pythonInfo.correlationId = requestQueue.CorrelationId;
                            return GetSuccessWithMessageResponse(pythonInfo);
                    }

                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ProblemStatementController), nameof(Upload), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, clientUID, deliveryUID);
                return GetFaultResponse(ex.Message);
            }
            //return Content(HttpStatusCode.OK, UpdateColumnsData);
            return Ok(UpdateColumnsData);
        }

        [HttpPost]
        [Route("api/FileUpload")]
        public IActionResult Upload(string ModelName, string userId, string clientUID, string deliveryUID, string ParentFileName,
    string MappingFlag, string Source, string UploadFileType, string Category, string Uploadtype, bool DBEncryption, string E2EUID,
    [Optional] string ClusterFlag, [Optional] string EntityStartDate, [Optional] string EntityEndDate, [Optional] string oldCorrelationID,
    string Language, bool IsModelTemplateDataSource, string CorrelationId_status, string usecaseId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ProblemStatementController), nameof(Upload), "Start", string.Empty, string.Empty, clientUID, deliveryUID);
            List<string> ColumnsData = new List<string>();
            var fileUploadColums = new FileUploadColums();
            string MappingColumns = string.Empty;
            string requestQueueStatus;
            try
            {
                #region VALIDATIONS
                if (string.IsNullOrEmpty(ModelName)
                    || string.IsNullOrEmpty(clientUID)
                    || string.IsNullOrEmpty(deliveryUID))
                {
                    return GetFaultResponse(Resource.IngrainResx.InputFieldsAreNull);
                }
                if (ModelName == CONSTANTS.undefined || clientUID == CONSTANTS.undefined || deliveryUID == CONSTANTS.undefined)
                    return GetFaultResponse(CONSTANTS.InutFieldsUndefined);

                if (CommonUtility.IsNameContainSpecialCharacters(ModelName))
                    return BadRequest(Resource.IngrainResx.SpecialCharacterNotAllowed);

                var fileCollection = HttpContext.Request.Form.Files;
                if (CommonUtility.ValidateFileUploaded(fileCollection))
                    return GetFaultResponse(Resource.IngrainResx.InValidFileName);

                if (!CommonUtility.GetValidUser(userId))
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);

                CommonUtility.ValidateInputFormData(clientUID, CONSTANTS.ClientUID, true);
                CommonUtility.ValidateInputFormData(deliveryUID, CONSTANTS.DeliveryConstructUID, true);
                CommonUtility.ValidateInputFormData(ModelName, CONSTANTS.ModelName, false);
                CommonUtility.ValidateInputFormData(ParentFileName, CONSTANTS.ParentFileName, false);
                CommonUtility.ValidateInputFormData(Source, CONSTANTS.Source, false);
                CommonUtility.ValidateInputFormData(UploadFileType, CONSTANTS.UploadFileType, false);
                CommonUtility.ValidateInputFormData(Category, CONSTANTS.Category, false);
                CommonUtility.ValidateInputFormData(Uploadtype, CONSTANTS.Uploadtype, false);
                CommonUtility.ValidateInputFormData(E2EUID, CONSTANTS.E2EUID, true);
                CommonUtility.ValidateInputFormData(ClusterFlag, CONSTANTS.ClusterFlag, false);
                CommonUtility.ValidateInputFormData(EntityStartDate, CONSTANTS.EntityStartDate, false);
                CommonUtility.ValidateInputFormData(EntityEndDate, CONSTANTS.EntityEndDate, false);
                CommonUtility.ValidateInputFormData(oldCorrelationID, CONSTANTS.oldCorrelationID, false);
                CommonUtility.ValidateInputFormData(Language, CONSTANTS.Language, false);
                CommonUtility.ValidateInputFormData(CorrelationId_status, CONSTANTS.CorrelationId_status, false);
                CommonUtility.ValidateInputFormData(usecaseId, CONSTANTS.UsecaseId, true);
                #endregion

                string correlationId = Guid.NewGuid().ToString();
                var type = typeof(DataProcessingController);
                string result = ingestedDataService.UploadFiles(correlationId, ModelName, userId, clientUID, deliveryUID, ParentFileName, MappingFlag, Source, Category, type, E2EUID, HttpContext, ClusterFlag, EntityStartDate, EntityEndDate, DBEncryption, oldCorrelationID, Language, IsModelTemplateDataSource, CorrelationId_status, out requestQueueStatus, usecaseId);
                switch (result)
                {

                    case CONSTANTS.FileNotExist:
                        return GetSuccessWithMessageResponse(Resource.IngrainResx.FileNotExist);
                    case CONSTANTS.FileEmpty:
                        return GetSuccessWithMessageResponse(Resource.IngrainResx.FileEmpty);
                    case CONSTANTS.Success:
                        string CorrelationID;
                        if (CorrelationId_status == "" || CorrelationId_status == "undefined")
                        {
                            CorrelationID = correlationId;
                        }
                        else
                        {
                            CorrelationID = CorrelationId_status;
                        }

                        if (UploadFileType == CONSTANTS.multiple && requestQueueStatus == "M")
                        {
                            //var Filecolumns = NinjectCoreBinding.NinjectKernel.Get<IIngestedData>();
                            fileUploadColums = ingestedDataService.GetFilesColumns(CorrelationID, ParentFileName, ModelName);
                            if (string.IsNullOrEmpty(fileUploadColums.CorrelationId))
                            {
                                fileUploadColums.CorrelationId = correlationId;
                                fileUploadColums.Flag = "flag5";
                                fileUploadColums.ModelName = ModelName;
                                fileUploadColums.ParentFileName = ParentFileName;

                            }
                        }
                        else
                        {
                            ColumnsData.Add(CorrelationID);
                            ColumnsData.Add(Resource.IngrainResx.UploadFile);
                        }
                        break;
                    case CONSTANTS.InputDataEmpty:
                        return GetSuccessWithMessageResponse(Resource.IngrainResx.InputData);

                    case CONSTANTS.PhythonError:
                        string CorrelationID_E;
                        if (CorrelationId_status == "" || CorrelationId_status == "undefined")
                        {
                            CorrelationID_E = correlationId;
                        }
                        else
                        {
                            CorrelationID_E = CorrelationId_status;
                        }
                        //requestQueue = IngestedData.GetFileRequestStatus(correlationId, Constant.IngestData);
                        requestQueue = ingestedDataService.GetFileRequestStatus(CorrelationID_E, "IngestData");
                        pythonCategory.Category = "Error";
                        pythonCategory.Message = requestQueue.Message;
                        pythonInfo.Category = pythonCategory;
                        pythonInfo.Status = requestQueue.Status;
                        return GetFaultResponse(pythonInfo);

                    case CONSTANTS.PhythonInfo:
                        string CorrelationID_I;
                        if (CorrelationId_status == "" || CorrelationId_status == "undefined")
                        {
                            CorrelationID_I = correlationId;
                        }
                        else
                        {
                            CorrelationID_I = CorrelationId_status;
                        }
                        //requestQueue = IngestedData.GetFileRequestStatus(correlationId, Constant.IngestData);
                        requestQueue = ingestedDataService.GetFileRequestStatus(CorrelationID_I, "IngestData");
                        pythonCategory.Category = "Error";
                        pythonCategory.Message = requestQueue.Message;
                        pythonInfo.Category = pythonCategory;
                        pythonInfo.Status = requestQueue.Status;
                        return GetFaultResponse(pythonInfo);
                    case CONSTANTS.PhythonProgress:
                        if (CorrelationId_status == "" || CorrelationId_status == "undefined")
                        {
                            requestQueue = ingestedDataService.GetFileRequestStatus(correlationId, "IngestData");
                        }
                        else
                        {
                            requestQueue = ingestedDataService.GetFileRequestStatus(CorrelationId_status, "IngestData");
                        }
                        pythonCategory.Category = "Progress";
                        pythonCategory.Message = requestQueue.Message;
                        pythonInfo.Category = pythonCategory;
                        pythonInfo.Status = requestQueue.Status;
                        pythonInfo.correlationId = requestQueue.CorrelationId;
                        return GetSuccessWithMessageResponse(pythonInfo);
                    case CONSTANTS.New:
                        if (CorrelationId_status == "" || CorrelationId_status == "undefined")
                        {
                            requestQueue = ingestedDataService.GetFileRequestStatus(correlationId, "IngestData");
                        }
                        else
                        {
                            requestQueue = ingestedDataService.GetFileRequestStatus(CorrelationId_status, "IngestData");
                        }
                        pythonCategory.Category = "New";
                        pythonCategory.Message = requestQueue.Message;
                        pythonInfo.Category = pythonCategory;
                        pythonInfo.Status = "P";
                        pythonInfo.correlationId = requestQueue.CorrelationId;
                        return GetSuccessWithMessageResponse(pythonInfo);
                    case CONSTANTS.FmUseCaseFail:
                        if (CorrelationId_status == "" || CorrelationId_status == "undefined")
                        {
                            CorrelationID_E = correlationId;
                        }
                        else
                        {
                            CorrelationID_E = CorrelationId_status;
                        }
                        pythonCategory.Category = "Error";
                        pythonCategory.Message = CONSTANTS.FMErrorMessage;
                        pythonInfo.Category = pythonCategory;
                        pythonInfo.Status = CONSTANTS.E;
                        return GetFaultResponse(pythonInfo);
                    case CONSTANTS.IngrainTokenBlank:
                        if (CorrelationId_status == "" || CorrelationId_status == "undefined")
                        {
                            CorrelationID_E = correlationId;
                        }
                        else
                        {
                            CorrelationID_E = CorrelationId_status;
                        }
                        pythonCategory.Category = "Error";
                        pythonCategory.Message = CONSTANTS.IngrainTokenNotFound;
                        pythonInfo.Category = pythonCategory;
                        pythonInfo.Status = CONSTANTS.E;
                        return GetFaultResponse(pythonInfo);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ProblemStatementController), nameof(Upload), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, clientUID, deliveryUID);
                return GetFaultResponse(ex.Message);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ProblemStatementController), nameof(Upload), "End", string.Empty, string.Empty, clientUID, deliveryUID);
            if (UploadFileType == CONSTANTS.multiple && requestQueueStatus == "M")
                return GetSuccessWithMessageResponse(fileUploadColums);
            else
                return GetSuccessWithMessageResponse(ColumnsData);

        }

        private string CheckQueueTable(string correlationId)
        {
            var queueTableData = processDataService.CheckPythonProcess(correlationId, "IngestData");
            return queueTableData;
        }

        [HttpPost]
        [Route("api/DataSourceFileUpload")]
        public IActionResult DataSourceUpload(string correlationId, string ModelName, string userId, string clientUID, string deliveryUID, string ParentFileName, string MappingFlag, string Source, string UploadFileType, string Category, string Uploadtype,
            bool DBEncryption, string E2EUID, [Optional] string ClusterFlag, [Optional] string EntityStartDate,
            [Optional] string EntityEndDate, [Optional] string oldCorrelationID, string Language, string CorrelationId_status, string usecaseId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ProblemStatementController), nameof(DataSourceUpload), "Start", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, clientUID, deliveryUID);
            List<string> ColumnsData = new List<string>();
            var fileUploadColums = new FileUploadColums();
            string MappingColumns = string.Empty;
            try
            {
                var fileCollection = HttpContext.Request.Form.Files;

                #region VALIDATIONS
                if (CommonUtility.ValidateFileUploaded(fileCollection))
                    return GetFaultResponse(Resource.IngrainResx.InValidFileName);

                if (!CommonUtility.GetValidUser(userId))
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);

                CommonUtility.ValidateInputFormData(correlationId, CONSTANTS.CorrelationId, true);
                CommonUtility.ValidateInputFormData(ModelName, CONSTANTS.ModelName, false);
                CommonUtility.ValidateInputFormData(clientUID, CONSTANTS.ClientUID, true);
                CommonUtility.ValidateInputFormData(deliveryUID, CONSTANTS.DeliveryConstructUID, true);
                CommonUtility.ValidateInputFormData(ParentFileName, CONSTANTS.ParentFileName, false);
                CommonUtility.ValidateInputFormData(MappingFlag, CONSTANTS.MappingFlag, false);
                CommonUtility.ValidateInputFormData(Source, CONSTANTS.Source, false);
                CommonUtility.ValidateInputFormData(UploadFileType, CONSTANTS.UploadFileType, false);
                CommonUtility.ValidateInputFormData(Category, CONSTANTS.Category, false);
                CommonUtility.ValidateInputFormData(Uploadtype, CONSTANTS.Uploadtype, false);
                CommonUtility.ValidateInputFormData(E2EUID, CONSTANTS.E2EUID, true);
                CommonUtility.ValidateInputFormData(ClusterFlag, CONSTANTS.ClusterFlag, false);
                CommonUtility.ValidateInputFormData(EntityStartDate, CONSTANTS.EntityStartDate, false);
                CommonUtility.ValidateInputFormData(EntityEndDate, CONSTANTS.EntityEndDate, false);
                CommonUtility.ValidateInputFormData(oldCorrelationID, CONSTANTS.oldCorrelationID, true);
                CommonUtility.ValidateInputFormData(Language, CONSTANTS.Language, false);
                CommonUtility.ValidateInputFormData(CorrelationId_status, CONSTANTS.CorrelationId_status, false);
                CommonUtility.ValidateInputFormData(usecaseId, CONSTANTS.UsecaseId, true);
                #endregion

                Type type = typeof(DataProcessingController);
                // var DataSource = NinjectCoreBinding.NinjectKernel.Get<IIngestedData>();
                // string result = DataSource.DataSourceUploadFiles(ModelName, correlationId, userId, clientUID, deliveryUID, UploadFileType, ParentFileName, MappingFlag, type);

                string result = ingestedDataService.DataSourceUploadFiles(correlationId, ModelName, userId, clientUID, deliveryUID, ParentFileName, MappingFlag, Source, Category, type, E2EUID, HttpContext, ClusterFlag, EntityStartDate, EntityEndDate, DBEncryption, oldCorrelationID, Language, CorrelationId_status, usecaseId);
                switch (result)
                {
                    case CONSTANTS.FileNotExist:
                        return GetSuccessWithMessageResponse(Resource.IngrainResx.FileNotExist);

                    case CONSTANTS.FileEmpty:
                        return GetSuccessWithMessageResponse(Resource.IngrainResx.FileEmpty);

                    case CONSTANTS.Success:
                        string CorrelationID;
                        if (CorrelationId_status == "" || CorrelationId_status == "undefined")
                        {
                            CorrelationID = correlationId;
                        }
                        else
                        {
                            CorrelationID = CorrelationId_status;
                        }
                        if (UploadFileType == CONSTANTS.multiple)
                        {
                            //var Filecolumns = NinjectCoreBinding.NinjectKernel.Get<IIngestedData>();
                            fileUploadColums = ingestedDataService.GetFilesColumns(CorrelationID, ParentFileName, ModelName);
                            if (string.IsNullOrEmpty(fileUploadColums.CorrelationId))
                            {
                                fileUploadColums.CorrelationId = correlationId;
                                fileUploadColums.Flag = "flag5";
                                fileUploadColums.ModelName = ModelName;
                                fileUploadColums.ParentFileName = ParentFileName;
                            }
                        }
                        else
                        {
                            ColumnsData.Add(correlationId);
                            ColumnsData.Add(Resource.IngrainResx.UploadFile);
                        }
                        break;
                    case CONSTANTS.InputDataEmpty:
                        return GetSuccessWithMessageResponse(Resource.IngrainResx.InputData);
                    case CONSTANTS.PhythonError:
                        string CorrelationID_E;
                        if (CorrelationId_status == "" || CorrelationId_status == "undefined")
                        {
                            CorrelationID_E = correlationId;
                        }
                        else
                        {
                            CorrelationID_E = CorrelationId_status;
                        }
                        //requestQueue = IngestedData.GetFileRequestStatus(correlationId, Constant.IngestData);
                        requestQueue = ingestedDataService.GetFileRequestStatus(CorrelationID_E, "IngestData");
                        pythonCategory.Category = "Error";
                        pythonCategory.Message = requestQueue.Message;
                        pythonInfo.Category = pythonCategory;
                        pythonInfo.Status = requestQueue.Status;
                        return GetFaultResponse(pythonInfo);
                    case CONSTANTS.PhythonInfo:
                        string CorrelationID_I;
                        if (CorrelationId_status == "" || CorrelationId_status == "undefined")
                        {
                            CorrelationID_I = correlationId;
                        }
                        else
                        {
                            CorrelationID_I = CorrelationId_status;
                        }
                        //requestQueue = IngestedData.GetFileRequestStatus(correlationId, Constant.IngestData);
                        requestQueue = ingestedDataService.GetFileRequestStatus(correlationId, "IngestData");
                        pythonCategory.Category = "Error";
                        pythonCategory.Message = requestQueue.Message;
                        pythonInfo.Category = pythonCategory;
                        pythonInfo.Status = requestQueue.Status;
                        return GetFaultResponse(pythonInfo);
                    case CONSTANTS.PhythonProgress:
                        if (CorrelationId_status == "" || CorrelationId_status == "undefined")
                        {
                            requestQueue = ingestedDataService.GetFileRequestStatus(correlationId, "IngestData");
                        }
                        else
                        {
                            requestQueue = ingestedDataService.GetFileRequestStatus(CorrelationId_status, "IngestData");
                        }
                        pythonCategory.Category = "Progress";
                        pythonCategory.Message = requestQueue.Message;
                        pythonInfo.Category = pythonCategory;
                        pythonInfo.Status = requestQueue.Status;
                        pythonInfo.correlationId = requestQueue.CorrelationId;
                        return GetSuccessWithMessageResponse(pythonInfo);
                    case CONSTANTS.New:
                        if (CorrelationId_status == "" || CorrelationId_status == "undefined")
                        {
                            requestQueue = ingestedDataService.GetFileRequestStatus(correlationId, "IngestData");
                        }
                        else
                        {
                            requestQueue = ingestedDataService.GetFileRequestStatus(CorrelationId_status, "IngestData");
                        }
                        pythonCategory.Category = "New";
                        pythonCategory.Message = requestQueue.Message;
                        pythonInfo.Category = pythonCategory;
                        pythonInfo.Status = "P";
                        pythonInfo.correlationId = requestQueue.CorrelationId;
                        return GetSuccessWithMessageResponse(pythonInfo);
                    case CONSTANTS.FmUseCaseFail:
                        if (CorrelationId_status == "" || CorrelationId_status == "undefined")
                        {
                            CorrelationID_E = correlationId;
                        }
                        else
                        {
                            CorrelationID_E = CorrelationId_status;
                        }
                        pythonCategory.Category = "Error";
                        pythonCategory.Message = CONSTANTS.FMErrorMessage;
                        pythonInfo.Category = pythonCategory;
                        pythonInfo.Status = CONSTANTS.E;
                        return GetFaultResponse(pythonInfo);

                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ProblemStatementController), nameof(DataSourceUpload), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, clientUID, deliveryUID);
                return GetFaultResponse(ex.Message);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ProblemStatementController), nameof(DataSourceUpload), "End", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, clientUID, deliveryUID);

            if (UploadFileType == CONSTANTS.multiple)
                return GetSuccessWithMessageResponse(fileUploadColums);
            else
                return GetSuccessWithMessageResponse(ColumnsData);

        }


        /// <summary>
        /// Post the Target and Input columns
        /// </summary>
        /// <returns>success</returns>
        [HttpPost]
        [Route("api/PostColumns")]     //ColumnsPostAPI
        public IActionResult PostColumns([FromBody] dynamic requestBody)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ProblemStatementController), nameof(PostColumns) + "-Data-" + requestBody, "START", string.Empty, string.Empty, string.Empty, string.Empty);
            UseCaseSave useCase = new UseCaseSave();
            try
            {
                string columnsData = Convert.ToString(requestBody);
                var dynamicColumns = JsonConvert.DeserializeObject(columnsData);
                var columns = JObject.Parse(columnsData);

                #region VALIDATIONS
                if (!CommonUtility.GetValidUser(Convert.ToString(columns[CONSTANTS.CreatedByUser])))
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);

                CommonUtility.ValidateInputFormData(Convert.ToString(columns[CONSTANTS.TargetColumn]), CONSTANTS.TargetColumn, false);
                CommonUtility.ValidateInputFormData(Convert.ToString(columns[CONSTANTS.CorrelationId]), CONSTANTS.CorrelationId, true);
                CommonUtility.ValidateInputFormData(Convert.ToString(columns[CONSTANTS.TargetUniqueIdentifier]), CONSTANTS.TargetUniqueIdentifier, false);
                CommonUtility.ValidateInputFormData(Convert.ToString(columns[CONSTANTS.InputColumns]), CONSTANTS.InputColumns, false);
                CommonUtility.ValidateInputFormData(Convert.ToString(columns[CONSTANTS.AvailableColumns]), CONSTANTS.AvailableColumns, false);
                CommonUtility.ValidateInputFormData(Convert.ToString(columns[CONSTANTS.ProblemType]), CONSTANTS.ProblemType, false);
                CommonUtility.ValidateInputFormData(Convert.ToString(columns[CONSTANTS.ParentCorrelationId]), CONSTANTS.ParentCorrelationId, true);
                CommonUtility.ValidateInputFormData(Convert.ToString(columns[CONSTANTS.IsDataTransformationRetained]), CONSTANTS.IsDataTransformationRetained, false);
                CommonUtility.ValidateInputFormData(Convert.ToString(columns[CONSTANTS.IsCustomColumnSelected]), CONSTANTS.IsCustomColumnSelected, false);                                
                #endregion

                _businessProblemData = new BusinessProblemDataDTO();
                BindBusinessProblemDataValues(_businessProblemData, columns);
                useCase = ingestedDataService.InsertColumns(_businessProblemData);
                if (!useCase.IsInserted)
                {
                    return GetFaultResponse(useCase.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ProblemStatementController), nameof(PostColumns), ex.Message + ex.StackTrace,ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ProblemStatementController), nameof(PostColumns), "END", string.Empty, string.Empty, string.Empty, string.Empty);
            return Ok("Success");
        }

        /// <summary>
        /// Get the Public Templates and MY Models of user
        /// </summary>
        /// <param name="Templates"></param>
        /// <param name="Models"></param>
        /// <param name="userId"></param>
        /// <param name="category"></param>
        /// <returns>Public Templates or Models of user</returns>
        [HttpGet]
        [Route("api/GetTemplateModels")]
        public IActionResult GetTemplateModels(bool Templates, string userId, string category, string dateFilter, string DeliveryConstructUID, string ClientUId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ProblemStatementController), nameof(GetTemplateModels), "Start", string.Empty, string.Empty, ClientUId, DeliveryConstructUID);
            PublicTemplateModel publicTemplateModel = new PublicTemplateModel();
            try
            {
                if (Templates)
                {
                    if (string.IsNullOrEmpty(category))
                        category = "It Services";
                    publicTemplateModel = ingestedDataService.GetPublicTemplates(category);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(ProblemStatementController), nameof(GetTemplateModels), "END", string.Empty, string.Empty, ClientUId, DeliveryConstructUID);
                    return GetSuccessWithMessageResponse(publicTemplateModel);
                }
                else
                {
                    if (!string.IsNullOrEmpty(userId))
                    {
                        if (!CommonUtility.GetValidUser(userId))
                        {
                            return GetFaultResponse(Resource.IngrainResx.InValidUser);
                        }
                        _publishModelDTOs = ingestedDataService.GetPublishModels(userId, dateFilter, DeliveryConstructUID, ClientUId);
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(ProblemStatementController), nameof(GetTemplateModels), "END", string.Empty, string.Empty, ClientUId, DeliveryConstructUID);
                        return GetSuccessWithMessageResponse(_publishModelDTOs);
                    }
                    else
                    {
                        return GetSuccessWithMessageResponse(Resource.IngrainResx.InputData);
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ProblemStatementController), nameof(GetTemplateModels), ex.Message, ex, string.Empty, string.Empty, ClientUId, DeliveryConstructUID);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
        }

        /// <summary>
        /// Get the AvailableColumns and user Target and Input Columns
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="IsTemplate"></param>
        /// <returns>All the Columns Including Target or Input Columns</returns>
        [HttpGet]
        [Route("api/GetColumns")]
        public IActionResult GetColumns(string correlationId, bool IsTemplate, bool newModel)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ProblemStatementController), "GetColumns - parameters-" + correlationId + "_" + IsTemplate + "_" + newModel, "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                if (!string.IsNullOrEmpty(correlationId) && correlationId != "undefined")
                {
                    var columns = ingestedDataService.GetColumns(correlationId, IsTemplate, newModel);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(ProblemStatementController), nameof(GetColumns), "END", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                    return GetSuccessWithMessageResponse(columns);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ProblemStatementController), nameof(GetColumns), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ProblemStatementController), nameof(GetColumns), "END", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            return GetSuccessWithMessageResponse(Resource.IngrainResx.CorrelatioUIdNotMatch);
        }

        [NonAction]
        public void BindBusinessProblemDataValues(BusinessProblemDataDTO businessProblemData, JObject columns)
        {
            try
            {
                businessProblemData.ClientUId = Guid.NewGuid().ToString();
                if (string.IsNullOrEmpty(columns["ProblemStatement"].ToString()))
                {
                    var businessProblems = "testBusinessProblem";
                    businessProblemData.BusinessProblems = businessProblems;
                }
                else
                {
                    businessProblemData.BusinessProblems = columns["ProblemStatement"].ToString();
                }
                businessProblemData.TargetColumn = columns["TargetColumn"].ToString();
                var inputColumns = JsonConvert.DeserializeObject(columns["InputColumns"].ToString());
                businessProblemData.InputColumns = inputColumns;
                businessProblemData.AvailableColumns = JsonConvert.DeserializeObject(columns["AvailableColumns"].ToString());
                if (columns.ContainsKey("CorrelationId"))
                {
                    businessProblemData.CorrelationId = columns["CorrelationId"].ToString();
                }
                businessProblemData.TimeSeries = columns["TimeSeries"];
                if (string.IsNullOrEmpty(Convert.ToString(columns["TargetUniqueIdentifier"])))
                {
                    businessProblemData.TargetUniqueIdentifier = null;
                }
                else
                {
                    businessProblemData.TargetUniqueIdentifier = columns["TargetUniqueIdentifier"].ToString();
                }
                businessProblemData.ProblemType = columns["ProblemType"].ToString();
                businessProblemData.CreatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                businessProblemData.CreatedByUser = columns["CreatedByUser"].ToString();
                businessProblemData.ModifiedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                businessProblemData.ModifiedByUser = columns["CreatedByUser"].ToString();
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(ProblemStatementController), nameof(PostColumns) + "-Data2-", "START", businessProblemData.AppId, string.Empty, businessProblemData.ClientUId, businessProblemData.DeliveryConstructUID);
                if (!string.IsNullOrEmpty(Convert.ToString(columns["ParentCorrelationId"])))
                {
                    businessProblemData.ParentCorrelationId = Convert.ToString(columns["ParentCorrelationId"]);
                }
                if (!string.IsNullOrEmpty(Convert.ToString(columns["IsDataTransformationRetained"])))
                {
                    businessProblemData.IsDataTransformationRetained = Convert.ToBoolean(columns["IsDataTransformationRetained"]);
                }
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(ProblemStatementController), nameof(PostColumns) + "-Data3-", "START", businessProblemData.AppId, string.Empty, businessProblemData.ClientUId, businessProblemData.DeliveryConstructUID);
                if (!string.IsNullOrEmpty(Convert.ToString(columns["IsCustomColumnSelected"])))
                {
                    businessProblemData.IsCustomColumnSelected = Convert.ToString(columns["IsCustomColumnSelected"]);
                }
                businessProblemData.IsCustomColumnSelected = (!string.IsNullOrEmpty(Convert.ToString(columns["IsCustomColumnSelected"]))) ?
                    Convert.ToString(columns["IsCustomColumnSelected"]) : CONSTANTS.False;
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ProblemStatementController), nameof(BindBusinessProblemDataValues), ex.Message + "StackTrace-" + ex.StackTrace, ex, businessProblemData.AppId, string.Empty, businessProblemData.ClientUId, businessProblemData.DeliveryConstructUID);
            }
        }

        /// <summary>
        /// Get the existing model name
        /// </summary>        
        /// <param name="modelName">The model name</param>
        /// <returns>Returns message if model name already exist based on input parameter</returns>
        [HttpGet]
        [Route("api/GetExistingModelName")]
        public IActionResult GetExistingModelName(string modelName)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ProblemStatementController), nameof(GetExistingModelName), "START", string.Empty, string.Empty, string.Empty, string.Empty);
            bool ModelStatus = false;
            try
            {
                if (!string.IsNullOrEmpty(modelName) && modelName != "undefined")
                {

                    if (CommonUtility.IsNameContainSpecialCharacters(modelName))
                    {
                        return BadRequest(Resource.IngrainResx.SpecialCharacterNotAllowed);
                    }
                    ModelStatus = ingestedDataService.IsModelNameExist(modelName);
                }
                else
                {
                    return BadRequest(Resource.IngrainResx.InputData);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ProblemStatementController), nameof(GetExistingModelName), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ProblemStatementController), nameof(GetExistingModelName), "END", string.Empty, string.Empty, string.Empty, string.Empty);
            return GetSuccessWithMessageResponse(ModelStatus);
        }
        /// <summary>
        /// Get the default entity name
        /// </summary>        
        /// <param name="correlationid">CorrelationID</param>
        /// <returns>Returns default entity name</returns>
        [HttpGet]
        [Route("api/GetDefaultEntityName")]
        public IActionResult GetDefaultEntityName(string correlationid)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ProblemStatementController), nameof(GetDefaultEntityName), "START", string.IsNullOrEmpty(correlationid) ? default(Guid) : new Guid(correlationid), string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                if (correlationid == null)
                    throw new ArgumentNullException(nameof(correlationid));

                return Ok(ingestedDataService.GetDefaultEntityName(correlationid));
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ProblemStatementController), nameof(GetDefaultEntityName), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
        }


        [Route("api/DownloadTemplate")]
        [HttpGet]
        public IActionResult DownloadTemplate(string correlationId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ProblemStatementController), nameof(ViewUploadedData), "Start", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                if (!string.IsNullOrEmpty(correlationId))
                {
                    var data = ingestedDataService.DownloadTemplateFun(correlationId);
                    return Ok(data);
                }
                else
                {
                    return GetFaultResponse(Resource.IngrainResx.EmptyData);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ProblemStatementController), nameof(ViewUploadedData), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(Resource.IngrainResx.InternalServererror + ex.Message + ex.StackTrace);
            }
        }

        /// <summary>
        /// View Uploaded Excel Data
        /// </summary>
        /// <returns></returns>
        [Route("api/ViewUploadedData")]
        [HttpGet]
        public IActionResult ViewUploadedData(string correlationId, int DecimalPlaces)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ProblemStatementController), nameof(ViewUploadedData), "Start", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                if (!string.IsNullOrEmpty(correlationId))
                {
                    var data = ingestedDataService.ViewUploadedData(correlationId, DecimalPlaces);
                    return Ok(data);
                }
                else 
                {
                    return GetFaultResponse(Resource.IngrainResx.EmptyData);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ProblemStatementController), nameof(ViewUploadedData), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(Resource.IngrainResx.InternalServererror + ex.Message + ex.StackTrace);
            }
        }

        [Route("api/ValidateInput")]
        [HttpGet]
        public IActionResult ValidateInput(string correlationId, string pageInfo, string userId, string deliveryConstructUID, string clientUId, bool isTemplateModel)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ProblemStatementController), nameof(ValidateInput), CONSTANTS.START, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, clientUId, deliveryConstructUID);
            try
            {
                if (correlationId == CONSTANTS.undefined || pageInfo == CONSTANTS.undefined)
                    return GetFaultResponse(CONSTANTS.InutFieldsUndefined);
                if (!CommonUtility.GetValidUser(userId))
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);
                if (string.IsNullOrEmpty(correlationId) || string.IsNullOrEmpty(pageInfo))
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(ProblemStatementController), nameof(ValidateInput), CONSTANTS.END, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, clientUId, deliveryConstructUID);
                    return GetFaultResponse(Resource.IngrainResx.InputFieldsAreNull);
                }
                else
                {
                    var data = ingestedDataService.GetInputvalidation(correlationId, pageInfo, userId, deliveryConstructUID, clientUId, isTemplateModel);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(ProblemStatementController), nameof(ValidateInput), CONSTANTS.END, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, clientUId, deliveryConstructUID);
                    return Ok(data);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ProblemStatementController), nameof(ValidateInput), ex.Message, ex, string.Empty, string.Empty, clientUId, deliveryConstructUID);
                return GetFaultResponse(Resource.IngrainResx.InternalServererror + ex.Message + ex.StackTrace);
            }
        }

        private bool IsNameContainSpecialCharacters(string name)
        {
            return Regex.IsMatch(name, "[^A-Za-z0-9\\s]");
        }

        #endregion


    }
}

