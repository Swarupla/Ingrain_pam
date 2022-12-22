#region Copyright (c) 2019 Accenture . All rights reserved.

// <copyright company="Accenture">
// Copyright (c) 2019 Accenture.  All rights reserved.
// Reproduction or transmission in whole or in part, in any form or by any means, 
// electronic, mechanical or otherwise, is prohibited without the prior written 
// consent of the copyright owner.
// </copyright>

#endregion

#region DataTransformationController Information
/********************************************************************************************************\
Module Name     :   DataTransformationController
Project         :   Accenture.MyWizard.SelfServiceAI.DataTransformationController
Organisation    :   Accenture Technologies Ltd.
Created By      :   Swetha Chandrasekar
Created Date    :   10-June-2019
Revision History :
Revision No             Initial             Modified Date           Description
1.0                     An                               
\********************************************************************************************************/
#endregion

#region Namespace
using Accenture.MyWizard.Ingrain.BusinessDomain.Common;
using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
using Accenture.MyWizard.Ingrain.DataModels.Models;
using Accenture.MyWizard.Ingrain.WebService;
using Accenture.MyWizard.Shared.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Web;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
#endregion

namespace Accenture.MyWizard.Ingrain
{


    public class DataTransformationController : MyWizardControllerBase
    {
        #region Members 
        private DataTransformationDTO _dataTransformation;

        private DataTransformationViewData _viewData;

        public static IDataTransformation _iDataTransformation { set; get; }

        public static IProcessDataService _iProcessDataService { set; get; }

        public static IIngestedData _iIngestedData { set; get; }

        public static ICascadingService _cascadingService { set; get; }
        private readonly IOptions<IngrainAppSettings> _appSettings;

        public static IProcessDataService processDataService { set; get; }
        public IFlushService _flushService { get; set; }
        #endregion

        #region Constructors
        public DataTransformationController(IServiceProvider serviceProvider, IOptions<IngrainAppSettings> settings)
        {
            _appSettings = settings;
            _iDataTransformation = serviceProvider.GetService<IDataTransformation>();
            _iProcessDataService = serviceProvider.GetService<IProcessDataService>();
            _iIngestedData = serviceProvider.GetService<IIngestedData>();
            processDataService = serviceProvider.GetService<IProcessDataService>();
            _cascadingService = serviceProvider.GetService<ICascadingService>();
            _flushService = serviceProvider.GetService<IFlushService>();
        }
        #endregion

        #region Methods  
        /// <summary>
        /// Gets the pre processded data for Data Transformation
        /// </summary>
        /// <param name="correlationId">The correlation identifier</param>
        /// <param name="noOfRecord">The no of records</param>
        /// <param name="showAllRecord">The show all records</param>
        /// <returns>Returns the result.</returns>
        [HttpGet]
        [Route("api/GetPreProcessedData")]
        public IActionResult GetPreProcessedData(string correlationId, int noOfRecord, bool showAllRecord, string problemType, int DecimalPlaces)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataTransformationController), nameof(GetPreProcessedData), "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            _dataTransformation = new DataTransformationDTO();
            try
            {
                if (!string.IsNullOrEmpty(correlationId) && !string.IsNullOrEmpty(correlationId))
                {
                    if ((noOfRecord == 0 && showAllRecord) || (noOfRecord > 0 && !showAllRecord) && !string.IsNullOrEmpty(problemType))
                    {
                        _dataTransformation = _iDataTransformation.GetPreProcessedData(correlationId, noOfRecord, showAllRecord, problemType, DecimalPlaces);
                        if (_dataTransformation == null || _dataTransformation.CorrelationId == null)
                        {
                            return Accepted(new { response = Resource.IngrainResx.CorrelatioUIdNotMatch });
                        }
                    }
                    else
                    {
                        return Accepted(new { response = Resource.IngrainResx.InvalidInputData });
                    }
                }
                else
                {
                    return Accepted(new { response = Resource.IngrainResx.CorrelatioUIDEmpty });
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(DataTransformationController), nameof(GetPreProcessedData), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(Resource.IngrainResx.InternalServererror);
            }

            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataTransformationController), nameof(GetPreProcessedData), "END", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            return GetSuccessResponse(_dataTransformation);
        }

        /// <summary>
        /// Gets the data for view data quality
        /// </summary>
        /// <param name="correlationId">The correlation identifier</param>
        /// <param name="userId">The user identifier</param>
        /// <param name="pageInfo">The page info</param>
        /// <returns>Returns the result.</returns>
        [HttpGet]
        [Route("api/GetViewData")]
        public IActionResult GetViewData(string correlationId, string userId, string pageInfo)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataTransformationController), "GetViewData - CorrelationId - " + correlationId + userId + pageInfo, "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            _viewData = new DataTransformationViewData();
            string PythonResult = string.Empty;
            try
            {
                if (userId == "undefined")
                {
                    return StatusCode((int)HttpStatusCode.OK, Resource.IngrainResx.EmptyData);
                }

                if (!string.IsNullOrEmpty(correlationId) || !string.IsNullOrEmpty(userId) || !string.IsNullOrEmpty(pageInfo))
                {
                    if (!CommonUtility.GetValidUser(userId))
                    {
                        return GetFaultResponse(Resource.IngrainResx.InValidUser);
                    }
                    var useCaseData = _iProcessDataService.CheckPythonProcess(correlationId, pageInfo);

                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataTransformationController), "GetViewData - useCaseData - " + useCaseData, "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                    _viewData.UseCaseDetails = useCaseData;
                    if (!string.IsNullOrEmpty(useCaseData))
                    {
                        JObject queueData = JObject.Parse(useCaseData);
                        string status = (string)queueData["Status"];
                        string progress = (string)queueData["Progress"];
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataTransformationController), "UseCaseTableData", queueData.ToString(), string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                        if (status == "C" && progress == "100")
                        {
                            _viewData = _iDataTransformation.GetViewData(correlationId);
                            _viewData.UseCaseDetails = useCaseData;
                            if (_viewData.ViewData != null)
                            {
                                var deleted = processDataService.RemoveQueueRecords(correlationId, pageInfo);
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataTransformationController), nameof(GetViewData), "RemoveQueueRecords" + deleted, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataTransformationController), nameof(GetViewData), "Status C", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                                return GetSuccessResponse(_viewData);
                            }
                            else
                            {
                                return GetSuccessWithMessageResponse(Resource.IngrainResx.EmptyData);
                            }

                        }
                        else
                        {
                            if (string.IsNullOrEmpty(status))
                            {
                                JObject queueData2 = JObject.Parse(useCaseData);
                                queueData2["Status"] = "P";
                                queueData2["Progress"] = "1";
                                _viewData.UseCaseDetails = queueData2.ToString();
                            }

                            return Accepted(_viewData);
                        }

                    }
                    else
                    {
                        IngrainRequestQueue ingrainRequest = new IngrainRequestQueue
                        {
                            _id = Guid.NewGuid().ToString(),
                            CorrelationId = correlationId,
                            RequestId = Guid.NewGuid().ToString(),
                            ProcessId = null,
                            Status = null,
                            ModelName = null,
                            RequestStatus = "New",
                            RetryCount = 0,
                            ProblemType = null,
                            Message = null,
                            UniId = null,
                            Progress = null,
                            pageInfo = pageInfo,//"ViewDataQuality",
                            ParamArgs = "{}",
                            Function = pageInfo,
                            CreatedByUser = userId,
                            CreatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                            ModifiedByUser = userId,
                            ModifiedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                            LastProcessedOn = null,
                        };
                        _iIngestedData.InsertRequests(ingrainRequest);
                        Thread.Sleep(2000);
                        PythonResult resultPython = new PythonResult();
                        resultPython.message = "success";
                        resultPython.status = "true";
                        PythonResult = resultPython.ToJson();
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataTransformationController), nameof(GetViewData), "End", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);

                        return GetSuccessResponse(PythonResult);

                    }
                }
                else
                {
                    return NotFound(Resource.IngrainResx.InputData);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(DataTransformationController), nameof(GetViewData), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
        }


        [HttpPost]
        [Route("api/ManualAddFeature")]
        public IActionResult ManualAddFeature(string correlationid)
        {
            //var result = NinjectCoreBinding.NinjectKernel.Get<IDataTransformation>();
            try
            {
                List<string> AddfeatureSuccess = new List<string>();
                string NewFeatures = string.Empty;
                IFormCollection collection = HttpContext.Request.Form;
                //var items = collection.AllKeys.SelectMany(collection.GetValues);
                //if (items.Count() > 0)
                //{
                //    foreach (var item in items)
                //    {
                //        NewFeatures += item;
                //    }
                //}
                foreach (var key in collection.Keys)
                {
                    var item = collection[key.ToString()];
                    if (item.Count > 0)
                    {
                        NewFeatures += item;
                    }
                }
                #region VALIDATIONS
                CommonUtility.ValidateInputFormData(Convert.ToString(NewFeatures), "NewFeatures", false);
                CommonUtility.ValidateInputFormData(correlationid, CONSTANTS.CorrelationId, true);
                #endregion
                _iDataTransformation.UpdateNewFeatures(correlationid, NewFeatures);
                AddfeatureSuccess.Add(correlationid);
                AddfeatureSuccess.Add(Resource.IngrainResx.AddFeatureSuccess);
                return Ok(AddfeatureSuccess);
                //return Content(HttpStatusCode.OK, AddfeatureSuccess);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(DataTransformationController), nameof(ManualAddFeature), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                //return Content(HttpStatusCode.InternalServerError, ex.Message);
                return GetFaultResponse(ex.Message);
            }

        }
        [HttpGet]
        [Route("api/GetUniqueValues")]
        public IActionResult GetUniqueValues(string correlationid)
        {
            //var result = NinjectCoreBinding.NinjectKernel.Get<IDataTransformation>();
            try
            {
                if (!string.IsNullOrEmpty(correlationid) && correlationid != "undefined")
                {
                    var columns = _iDataTransformation.FetchUniqueColumns(correlationid);
                    return GetSuccessResponse(columns);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(DataTransformationController), nameof(GetUniqueValues), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(Resource.IngrainResx.InternalServererror);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataTransformationController), nameof(GetUniqueValues), "END", string.IsNullOrEmpty(correlationid) ? default(Guid) : new Guid(correlationid), string.Empty, string.Empty, string.Empty, string.Empty);
            return NotFound(Resource.IngrainResx.CorrelatioUIdNotMatch);
        }

        [HttpGet]
        [Route("api/GetCascadingModels")]
        public IActionResult GetCascadingModels(string clientUid, string dcUid, string userId, string category, string cascadedId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataTransformationController), nameof(GetCascadingModels), "START", string.Empty, string.Empty,clientUid,dcUid);
            CascadeModel cascadeModel = new CascadeModel();
            try
            {
                if (clientUid != CONSTANTS.undefined & dcUid != CONSTANTS.undefined & userId != CONSTANTS.undefined & category != CONSTANTS.undefined)
                {
                    if (!string.IsNullOrEmpty(clientUid) || !string.IsNullOrEmpty(dcUid) || !string.IsNullOrEmpty(userId) || !string.IsNullOrEmpty(category))
                    {
                        if (!CommonUtility.GetValidUser(userId))
                        {
                            return GetFaultResponse(Resource.IngrainResx.InValidUser);
                        }
                        cascadeModel = _cascadingService.GetCascadingModels(clientUid, dcUid, userId, category, cascadedId);
                        if (cascadeModel.IsException)
                        {
                            return GetFaultResponse(cascadeModel);
                        }
                    }
                    else
                    {
                        return GetFaultResponse(CONSTANTS.InputDataEmpty);
                    }
                }
                else
                {
                    return GetFaultResponse(CONSTANTS.InutFieldsUndefined);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(DataTransformationController), nameof(GetCascadingModels), ex.Message, ex, string.Empty, string.Empty, clientUid, dcUid);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataTransformationController), nameof(GetCascadingModels), "END", string.Empty, string.Empty, clientUid, dcUid);
            return GetSuccessResponse(cascadeModel);
        }

        [HttpPost]
        [Route("api/SaveCascadeModels")]
        public IActionResult SaveCascadeModels(CascadeCollection data)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataTransformationController), nameof(SaveCascadeModels), "START", string.Empty, string.Empty, data.ClientUId, data.DeliveryConstructUID);
            CascadeSaveModel model = new CascadeSaveModel();
            try
            {
                if (string.IsNullOrWhiteSpace(data.ModelName) || string.IsNullOrWhiteSpace(data.CreatedByUser)
                    || string.IsNullOrWhiteSpace(data.ClientUId)
                    || string.IsNullOrWhiteSpace(data.DeliveryConstructUID)
                    || string.IsNullOrWhiteSpace(data.Category))
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataTransformationController), nameof(SaveCascadeModels), "END", string.Empty, string.Empty, data.ClientUId, data.DeliveryConstructUID);
                    return GetFaultResponse(CONSTANTS.InputDataEmpty);
                }
                else if (data.ModelName == CONSTANTS.undefined || data.CreatedByUser == CONSTANTS.undefined
                    || data.Category == CONSTANTS.undefined
                    || data.DeliveryConstructUID == CONSTANTS.undefined
                    || data.ClientUId == CONSTANTS.undefined)
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataTransformationController), nameof(SaveCascadeModels), "END", string.Empty, string.Empty, data.ClientUId, data.DeliveryConstructUID);
                    return GetFaultResponse(CONSTANTS.InutFieldsUndefined);
                }
                else
                {
                    if (!CommonUtility.GetValidUser(data.CreatedByUser))
                    {
                        return GetFaultResponse(Resource.IngrainResx.InValidUser);
                    }
                    else if (!CommonUtility.GetValidUser(data.ModifiedByUser))
                    {
                        return GetFaultResponse(Resource.IngrainResx.InValidUser);
                    }
                    CommonUtility.ValidateInputFormData(data.ClientUId, "ClientUId", true);
                    CommonUtility.ValidateInputFormData(data.DeliveryConstructUID, "DeliveryConstructUID", true);
                    CommonUtility.ValidateInputFormData(data.CascadedId, "CascadedId", true);
                    CommonUtility.ValidateInputFormData(data.ModelName, "ModelName", false);
                    CommonUtility.ValidateInputFormData(data.Category, "Category", false);
                    CommonUtility.ValidateInputFormData(Convert.ToString(data.ModelList), "ModelList", false);
                    model = _cascadingService.SaveCascadeModels(data);
                    if (model.IsException)
                    {
                        return GetFaultResponse(model);
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(DataTransformationController), nameof(SaveCascadeModels), ex.Message, ex, string.Empty, string.Empty, data.ClientUId, data.DeliveryConstructUID);
                return GetFaultResponse(ex.Message);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataTransformationController), nameof(SaveCascadeModels), "END", string.Empty, string.Empty, data.ClientUId, data.DeliveryConstructUID);
            return Ok(model);
        }
        [HttpGet]
        [Route("api/GetCascadeModelMapping")]
        public IActionResult GetCascadeModelMapping(string cascadedId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataTransformationController), nameof(GetCascadeModelMapping), "START", string.Empty, string.Empty, string.Empty, string.Empty);
            CascadeModelMapping cascadeModel = new CascadeModelMapping();
            try
            {
                if (string.IsNullOrEmpty(cascadedId) || cascadedId == CONSTANTS.Null || cascadedId == CONSTANTS.undefined)
                {
                    return GetFaultResponse(CONSTANTS.InputDataEmpty);
                }
                cascadeModel = _cascadingService.GetMappingModels(cascadedId);
                if (cascadeModel.IsError)
                {
                    return GetFaultResponse(cascadeModel);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(DataTransformationController), nameof(GetCascadeModelMapping), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataTransformationController), nameof(GetCascadeModelMapping), "END", string.Empty, string.Empty, string.Empty, string.Empty);
            return Ok(cascadeModel);
        }
        [HttpPost]
        [Route("api/UpdateCascadeMapping")]
        public IActionResult UpdateCascadeMapping(UpdateCasecadeModel data)
        {
            UpdateCascadeModelMapping model = new UpdateCascadeModelMapping();
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataTransformationController), nameof(UpdateCascadeMapping), "START", string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                if (!string.IsNullOrEmpty(data.CascadedId) & data.Mappings != null)
                {
                    CommonUtility.ValidateInputFormData(data.CascadedId, "CascadedId", true);
                    CommonUtility.ValidateInputFormData(Convert.ToString(data.Mappings), "Mappings", false);

                    model = _cascadingService.UpdateCascadeMapping(data);
                    if (model.IsException)
                    {
                        return GetFaultResponse(model);
                    }
                }
                else
                {
                    return GetFaultResponse(CONSTANTS.InputDataEmpty + "--" + data);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(DataTransformationController), nameof(UpdateCascadeMapping), ex.Message+ ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataTransformationController), nameof(UpdateCascadeMapping), "END", string.Empty, string.Empty, string.Empty, string.Empty);
            return Ok(model);
        }
        [HttpGet]
        [Route("api/GetCascadeDeployedModel")]
        public IActionResult GetCascadeDeployedModel(string cascadedId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataTransformationController), nameof(GetCascadeDeployedModel), "START", string.Empty, string.Empty, string.Empty, string.Empty);
            CascadeDeployViewModel deployModel = new CascadeDeployViewModel();
            try
            {
                if (string.IsNullOrEmpty(cascadedId) || cascadedId == CONSTANTS.Null || cascadedId == CONSTANTS.undefined)
                {
                    return GetFaultResponse(CONSTANTS.InputDataEmpty);
                }
                else
                {
                    deployModel = _cascadingService.GetDeployedModel(cascadedId);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(DataTransformationController), nameof(GetCascadeDeployedModel), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataTransformationController), nameof(GetCascadeDeployedModel), "END", string.Empty, string.Empty, string.Empty, string.Empty);
            return Ok(deployModel);
        }

        [Route("api/GetCustomCascadeModels")]
        [HttpGet]
        public IActionResult GetCustomCascadeModels(string clientUid, string dcUid, string userId, string category)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataTransformationController), nameof(GetCustomCascadeModels), "START", string.Empty, string.Empty, clientUid, dcUid);
            CustomCascadeModel customModels = new CustomCascadeModel();
            try
            {
                if (clientUid != CONSTANTS.undefined & dcUid != CONSTANTS.undefined & userId != CONSTANTS.undefined & category != CONSTANTS.undefined)
                {
                    if (string.IsNullOrEmpty(clientUid) || string.IsNullOrEmpty(dcUid)
                         || string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(category)
                         || clientUid == CONSTANTS.Null || dcUid == CONSTANTS.Null
                         || category == CONSTANTS.Null || userId == CONSTANTS.Null)
                    {
                        return GetFaultResponse(CONSTANTS.EmptyData);
                    }
                    else
                    {
                        if (!CommonUtility.GetValidUser(userId))
                        {
                            return GetFaultResponse(Resource.IngrainResx.InValidUser);
                        }
                        customModels = _cascadingService.GetCustomCascadeModels(clientUid, dcUid, userId, category);
                    }
                }
                else
                {
                    return GetFaultResponse(CONSTANTS.EmptyData);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(DataTransformationController), nameof(GetCustomCascadeModels), ex.Message, ex, string.Empty, string.Empty, clientUid, dcUid);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataTransformationController), nameof(GetCustomCascadeModels), "END", string.Empty, string.Empty, clientUid, dcUid);
            return Ok(customModels);
        }

        [HttpPost]
        [Route("api/SaveCustomCascadeModels")]
        public IActionResult SaveCustomCascadeModels(CascadeCollection data)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataTransformationController), nameof(SaveCustomCascadeModels), "START",string.Empty,string.Empty,data.ClientUId,data.DeliveryConstructUID);
            UpdateCascadeModelMapping model = new UpdateCascadeModelMapping();
            try
            {
                if (string.IsNullOrWhiteSpace(data.ModelName) || string.IsNullOrWhiteSpace(data.CreatedByUser)
                    || string.IsNullOrWhiteSpace(data.ClientUId)
                    || string.IsNullOrWhiteSpace(data.DeliveryConstructUID)
                    || string.IsNullOrWhiteSpace(data.Category) || string.IsNullOrWhiteSpace(data.UniqIdName)
                    || string.IsNullOrWhiteSpace(data.UniqDatatype) || string.IsNullOrWhiteSpace(data.TargetColumn))
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataTransformationController), nameof(SaveCustomCascadeModels), "END", string.Empty, string.Empty, data.ClientUId, data.DeliveryConstructUID);
                    return GetFaultResponse(CONSTANTS.InputDataEmpty);
                }
                else if (data.ModelName == CONSTANTS.undefined || data.CreatedByUser == CONSTANTS.undefined
                    || data.Category == CONSTANTS.undefined
                    || data.DeliveryConstructUID == CONSTANTS.undefined
                    || data.ClientUId == CONSTANTS.undefined || data.UniqIdName == CONSTANTS.undefined
                    || data.UniqDatatype == CONSTANTS.undefined || data.TargetColumn == CONSTANTS.undefined)
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataTransformationController), nameof(SaveCustomCascadeModels), "END",string.Empty, string.Empty, data.ClientUId, data.DeliveryConstructUID);
                    return GetFaultResponse(CONSTANTS.InutFieldsUndefined);
                }
                else if (data.ModelName == CONSTANTS.Null || data.CreatedByUser == CONSTANTS.Null
                    || data.Category == CONSTANTS.Null
                    || data.DeliveryConstructUID == CONSTANTS.Null
                    || data.ClientUId == CONSTANTS.Null)
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataTransformationController), nameof(SaveCustomCascadeModels), "END", string.Empty, string.Empty, data.ClientUId, data.DeliveryConstructUID);
                    return GetFaultResponse(CONSTANTS.InputFieldsAreNull);
                }
                else
                {
                    if (!CommonUtility.GetValidUser(data.CreatedByUser))
                    {
                        return GetFaultResponse(Resource.IngrainResx.InValidUser);
                    }
                    else if (!CommonUtility.GetValidUser(data.ModifiedByUser))
                    {
                        return GetFaultResponse(Resource.IngrainResx.InValidUser);
                    }
                    CommonUtility.ValidateInputFormData(data.CascadedId, "CascadedId", true);
                    CommonUtility.ValidateInputFormData(data.ClientUId, "ClientUId", true);
                    CommonUtility.ValidateInputFormData(data.DeliveryConstructUID, "DeliveryConstructUID", true);
                    CommonUtility.ValidateInputFormData(data.UniqDatatype, "UniqDatatype", false);
                    CommonUtility.ValidateInputFormData(data.ModelName, "ModelName", false);
                    CommonUtility.ValidateInputFormData(data.Category, "Category", false);
                    CommonUtility.ValidateInputFormData(data.UniqIdName, "UniqIdName", false);
                    CommonUtility.ValidateInputFormData(data.TargetColumn, "TargetColumn", false);
                    CommonUtility.ValidateInputFormData(Convert.ToString(data.ModelList), "ModelList", false);
                    CommonUtility.ValidateInputFormData(Convert.ToString(data.Mappings), "Mappings", false); 
                    CommonUtility.ValidateInputFormData(Convert.ToString(data.RemovedModels), "RemovedModels", false);

                    model = _cascadingService.SaveCustomCascadeModels(data);
                    if (model.IsException)
                    {
                        return GetFaultResponse(model);
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(DataTransformationController), nameof(SaveCustomCascadeModels), ex.Message, ex, string.Empty, string.Empty, data.ClientUId, data.DeliveryConstructUID);
                return GetFaultResponse(model);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataTransformationController), nameof(SaveCustomCascadeModels), "END", string.Empty, string.Empty, data.ClientUId, data.DeliveryConstructUID);
            return Ok(model);
        }

        [HttpGet]
        [Route("api/GetCascadeIdDetails")]
        public IActionResult GetCascadeIdDetails(string sourceCorid, string targetCorid, string cascadeId, string UniqIdName, string UniqDatatype, string TargetColumn)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataTransformationController), nameof(GetCascadeIdDetails), "START", string.Empty, string.Empty, string.Empty, string.Empty);
            CustomMapping data = new CustomMapping();
            try
            {
                if (cascadeId == CONSTANTS.undefined || string.IsNullOrEmpty(sourceCorid)
                    || string.IsNullOrEmpty(targetCorid) || sourceCorid == CONSTANTS.undefined
                    || targetCorid == CONSTANTS.undefined || sourceCorid == CONSTANTS.Null || targetCorid == CONSTANTS.Null
                    || sourceCorid == null || targetCorid == null)
                    return GetFaultResponse(CONSTANTS.InputFieldsAreNull);
                else
                    data = _cascadingService.GetCascadeIdDetails(sourceCorid, targetCorid, cascadeId, UniqIdName, UniqDatatype, TargetColumn);
                if (data.IsException)
                {
                    return GetFaultResponse(data);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(DataTransformationController), nameof(GetCascadeIdDetails), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataTransformationController), nameof(GetCascadeIdDetails), "END", string.Empty, string.Empty, string.Empty, string.Empty);
            return Ok(data);
        }

        [Route("api/GetCustomCascadeDetails")]
        [HttpGet]
        public IActionResult GetCustomCascadeDetails(string cascadeId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataTransformationController), nameof(GetCustomCascadeDetails), "START", string.Empty, string.Empty, string.Empty, string.Empty);
            CustomModelViewDetails data = new CustomModelViewDetails();
            try
            {
                if (string.IsNullOrEmpty(cascadeId) || cascadeId == CONSTANTS.undefined || cascadeId == CONSTANTS.Null)
                {
                    return GetFaultResponse(CONSTANTS.InputDataEmpty);
                }
                data = _cascadingService.GetCustomCascadeDetails(cascadeId);
                if (data.IsException)
                {
                    return GetFaultResponse(data);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(DataTransformationController), nameof(GetCustomCascadeDetails), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataTransformationController), nameof(GetCustomCascadeDetails), "END", string.Empty, string.Empty, string.Empty, string.Empty);
            return Ok(data);
        }
        [Route("api/GetCascadeVDSModels")]
        [HttpGet]
        public IActionResult GetCascadeVDSModels(string ClientUID, string DCUID, string UserID, string Category)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataTransformationController), nameof(GetCascadeVDSModels), "START",string.Empty,string.Empty,ClientUID,DCUID);
            CascadeVDSModels cascadeModels = new CascadeVDSModels();
            try
            {
                if (string.IsNullOrEmpty(ClientUID) || string.IsNullOrEmpty(DCUID) || string.IsNullOrEmpty(UserID))
                {
                    return GetFaultResponse(CONSTANTS.InputDataEmpty);
                }
                if (!CommonUtility.GetValidUser(UserID))
                {
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);
                }
                cascadeModels = _cascadingService.GetCascadeVDSModels(ClientUID, DCUID, UserID, Category, out bool isException, out string ErrorMessage);
                if (isException)
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataTransformationController), nameof(GetCascadeVDSModels), "END", string.Empty, string.Empty, cascadeModels.ClientUID, cascadeModels.DCUID);
                    return GetFaultResponse(CONSTANTS.VisualizationError + ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(DataTransformationController), nameof(GetCascadeVDSModels), ex.Message, ex, string.Empty, string.Empty, cascadeModels.ClientUID, cascadeModels.DCUID);
                return GetFaultResponse(ex.Message);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataTransformationController), nameof(GetCascadeVDSModels), "END", string.Empty, string.Empty, ClientUID, DCUID);
            return Ok(cascadeModels);
        }
        [Route("api/GetCascadeInfluencers")]
        [HttpGet]
        public IActionResult GetCascadeInfluencers(string CascadedId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataTransformationController), nameof(GetCascadeInfluencers), "START", string.Empty, string.Empty, string.Empty, string.Empty);
            CascadeInfluencers cascadeInfluencers = new CascadeInfluencers();
            try
            {
                if (string.IsNullOrEmpty(CascadedId) || CascadedId == CONSTANTS.undefined || CascadedId == CONSTANTS.Null)
                {
                    return GetFaultResponse(CONSTANTS.InputDataEmpty);
                }
                cascadeInfluencers = _cascadingService.GetInfluencers(CascadedId, out bool isException, out string ErrorMessage);
                if (isException)
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataTransformationController), nameof(GetCascadeInfluencers), "END", string.Empty, string.Empty, string.Empty, string.Empty);
                    return GetFaultResponse(CONSTANTS.VisualizationError + ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(DataTransformationController), nameof(GetCascadeInfluencers), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataTransformationController), nameof(GetCascadeInfluencers), "END", string.Empty, string.Empty, string.Empty, string.Empty);
            return Ok(cascadeInfluencers);
        }

        [Route("api/UploadData")]
        [HttpPost]
        public IActionResult UploadData([FromForm]VisulizationUpload visulization)
        {
            var files = Request.Form.Files;
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataTransformationController), nameof(UploadData), "START",string.Empty,string.Empty,visulization.ClientUID,visulization.DCUID);
            UploadResponse uploadResponse = new UploadResponse();
            try
            {

                if (visulization.CascadedId == CONSTANTS.undefined
                    || visulization.ClientUID == CONSTANTS.undefined
                    || visulization.DCUID == CONSTANTS.undefined
                    || visulization.ModelName == CONSTANTS.undefined
                    || visulization.CascadedId == CONSTANTS.Null
                    || visulization.ClientUID == CONSTANTS.Null
                    || visulization.DCUID == CONSTANTS.Null
                    || visulization.ModelName == CONSTANTS.Null)
                {
                    uploadResponse.ValidatonMessage = CONSTANTS.InputFieldsAreNull;
                    uploadResponse.ErrorMessage = CONSTANTS.InputFieldsAreNull;
                    uploadResponse.IsUploaded = false;
                    uploadResponse.Status = CONSTANTS.E;
                    return Ok(uploadResponse);
                }
                if (!CommonUtility.GetValidUser(visulization.UserId))
                {
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);
                }
                if (visulization.CascadedId != null && visulization.IsFileUpload.ToString() != null
                    && visulization.ClientUID != null
                    && visulization.DCUID != null && visulization.ModelName != null)
                {
                    var fileCollection = HttpContext.Request.Form.Files;
                    if (CommonUtility.ValidateFileUploaded(fileCollection))
                    {
                        return GetFaultResponse(Resource.IngrainResx.InValidFileName);
                    }
                    CommonUtility.ValidateInputFormData(visulization.CascadedId, "CascadedId", true);
                    CommonUtility.ValidateInputFormData(visulization.ClientUID, "ClientUID", true);
                    CommonUtility.ValidateInputFormData(visulization.DCUID, "DCUID", true);
                    CommonUtility.ValidateInputFormData(visulization.ModelName, "ModelName", false);
                    uploadResponse = _cascadingService.UploadData(visulization, fileCollection, out bool isException, out string errorMessage);
                    if (isException)
                    {
                        uploadResponse.ErrorMessage = errorMessage;
                        uploadResponse.IsException = true;
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataTransformationController), nameof(UploadData), "END", string.Empty, string.Empty, visulization.ClientUID, visulization.DCUID);
                        return GetFaultResponse(uploadResponse);
                    }
                }
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataTransformationController), nameof(UploadData), "END", string.Empty, string.Empty, visulization.ClientUID, visulization.DCUID);
                return Ok(uploadResponse);
            }
            catch (Exception ex)
            {
                uploadResponse.Status = CONSTANTS.E;
                uploadResponse.IsException = true;
                uploadResponse.ErrorMessage = ex.Message + "***STACKTRACE***" + ex.StackTrace;
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(DataTransformationController), nameof(UploadData), ex.Message, ex, string.Empty, string.Empty, visulization.ClientUID, visulization.DCUID);
                return GetFaultResponse(uploadResponse);
            }
        }

        [Route("api/GetCascadeVisualization")]
        [HttpGet]
        public IActionResult GetCascadeVisualization(string CascadedId, string UniqueId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataTransformationController), nameof(GetCascadeVisualization), "START", string.Empty, string.Empty, string.Empty, string.Empty);
            VisualizationViewModel predictionData = new VisualizationViewModel();
            try
            {
                if (CascadedId == CONSTANTS.Null || CascadedId == CONSTANTS.undefined || UniqueId == CONSTANTS.undefined || UniqueId == CONSTANTS.Null)
                {
                    return GetFaultResponse(CONSTANTS.InputFieldsAreNull);
                }
                if (!string.IsNullOrEmpty(CascadedId) && !string.IsNullOrEmpty(UniqueId))
                {
                    predictionData = _cascadingService.GetCascadePrediction(CascadedId, UniqueId);
                    if (predictionData.IsException)
                    {
                        return GetFaultResponse(predictionData);
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(DataTransformationController), nameof(GetCascadeVisualization), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataTransformationController), nameof(GetCascadeVisualization), "END", string.Empty, string.Empty, string.Empty, string.Empty);
            return Ok(predictionData);
        }
        [Route("api/ShowCascadeData")]
        [HttpGet]
        public IActionResult ShowCascadeData(string CascadedId, string UniqueId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataTransformationController), nameof(ShowCascadeData), "END", string.Empty, string.Empty, string.Empty, string.Empty);
            ShowData data = new ShowData();
            try
            {
                if (CascadedId == CONSTANTS.Null || CascadedId == CONSTANTS.undefined || UniqueId == CONSTANTS.undefined || UniqueId == CONSTANTS.Null)
                {
                    return GetFaultResponse(CONSTANTS.InputFieldsAreNull);
                }
                data = _cascadingService.ShowData(CascadedId, UniqueId);
                if (data.IsException)
                {
                    return GetFaultResponse(data);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(DataTransformationController), nameof(ShowCascadeData), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataTransformationController), nameof(ShowCascadeData), "END", string.Empty, string.Empty, string.Empty, string.Empty);
            return Ok(data);
        }
        [Route("api/GetFMVisualizationDetails")]
        [HttpGet]
        public IActionResult GetFMVisualizationDetails(string ClientUID, string DCUID, string UserID, string Category)
        {
            FMVisualizationDTO fMVisualizationDTO = new FMVisualizationDTO();
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataTransformationController), nameof(GetFMVisualizationDetails), "START", string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                if (string.IsNullOrEmpty(ClientUID) || string.IsNullOrEmpty(DCUID) || string.IsNullOrEmpty(UserID))
                {
                    return GetFaultResponse(CONSTANTS.InputDataEmpty);
                }
                if (ClientUID == CONSTANTS.undefined || DCUID == CONSTANTS.undefined || UserID == CONSTANTS.undefined || Category == CONSTANTS.undefined)
                {
                    return GetFaultResponse(CONSTANTS.InutFieldsUndefined);
                }
                if (!CommonUtility.GetValidUser(UserID))
                {
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);
                }
                fMVisualizationDTO = _cascadingService.GetFmVisualizeDetails(ClientUID, DCUID, UserID, Category);
            }
            catch (Exception ex)
            {
                fMVisualizationDTO.IsException = true;
                fMVisualizationDTO.ErrorMessage = ex.Message + "-STACKTRACE-" + ex.StackTrace;
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(DataTransformationController), nameof(GetFMVisualizationDetails), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataTransformationController), nameof(ShowCascadeData), "END", string.Empty, string.Empty, string.Empty, string.Empty);
            return Ok(fMVisualizationDTO);
        }

        [Route("api/GetFMVisualizationinProgress")]
        [HttpGet]
        public IActionResult GetFMVisualizationinProgress(string ClientUID, string DCUID, string UserID, string Category)
        {
            FMVisualizationinProgress fMVisualizationinProgress= new FMVisualizationinProgress();
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataTransformationController), nameof(GetFMVisualizationinProgress), "START", string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                if (string.IsNullOrEmpty(ClientUID) || string.IsNullOrEmpty(DCUID) || string.IsNullOrEmpty(UserID))
                {
                    return GetFaultResponse(CONSTANTS.InputDataEmpty);
                }
                if (ClientUID == CONSTANTS.undefined || DCUID == CONSTANTS.undefined || UserID == CONSTANTS.undefined || Category == CONSTANTS.undefined)
                {
                    return GetFaultResponse(CONSTANTS.InutFieldsUndefined);
                }
                if (!CommonUtility.GetValidUser(UserID))
                {
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);
                }
                fMVisualizationinProgress = _cascadingService.GetFMVisualizationinProgress(ClientUID, DCUID, UserID, Category);
            }
            catch (Exception ex)
            {
                fMVisualizationinProgress.IsException = true;
                fMVisualizationinProgress.ErrorMessage = ex.Message + "-STACKTRACE-" + ex.StackTrace;
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(DataTransformationController), nameof(GetFMVisualizationinProgress), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataTransformationController), nameof(GetFMVisualizationinProgress), "END", string.Empty, string.Empty, string.Empty, string.Empty);
            return Ok(fMVisualizationinProgress);
        }
        [Route("api/FMFileUpload")]
        [HttpPost]
        public IActionResult FMFileUpload([FromForm]FMFileUpload visulization)
        {
            var files = Request.Form.Files;
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataTransformationController), nameof(UploadData), "START", string.Empty, string.Empty, visulization.ClientUID, visulization.DCUID);
            FMUploadResponse uploadResponse = new FMUploadResponse();
            try
            {

                if (visulization.ClientUID == CONSTANTS.undefined
                    || visulization.DCUID == CONSTANTS.undefined
                    || visulization.UserId == CONSTANTS.undefined
                    || visulization.Category == CONSTANTS.undefined
                    || visulization.ClientUID == CONSTANTS.Null
                    || visulization.UserId == CONSTANTS.Null
                    || visulization.Category == CONSTANTS.Null
                    || visulization.DCUID == CONSTANTS.Null)
                {
                    uploadResponse.ValidatonMessage = CONSTANTS.InputFieldsAreNull;
                    uploadResponse.ErrorMessage = CONSTANTS.InputFieldsAreNull;
                    uploadResponse.IsUploaded = false;
                    uploadResponse.Status = CONSTANTS.E;
                    return Ok(uploadResponse);
                }
                if (visulization.IsRefresh)
                {
                    if (string.IsNullOrEmpty(visulization.CorrelationId) || visulization.CorrelationId == CONSTANTS.undefined || visulization.CorrelationId == CONSTANTS.Null)
                    {
                        uploadResponse.ValidatonMessage = CONSTANTS.CorrelatioUIDEmpty;
                        uploadResponse.ErrorMessage = CONSTANTS.CorrelatioUIDEmpty;
                        uploadResponse.IsUploaded = false;
                        uploadResponse.Status = CONSTANTS.E;
                        return Ok(uploadResponse);
                    }
                }
                if (visulization.ClientUID != null && visulization.DCUID != null && visulization.UserId != null)
                {
                    if (!CommonUtility.GetValidUser(visulization.UserId))
                    {
                        return GetFaultResponse(Resource.IngrainResx.InValidUser);
                    }
                    var fileCollection = HttpContext.Request.Form.Files;
                    if (CommonUtility.ValidateFileUploaded(fileCollection))
                    {
                        return GetFaultResponse(Resource.IngrainResx.InValidFileName);
                    }

                    if (!string.IsNullOrEmpty(visulization.CorrelationId) &&  visulization.CorrelationId.Trim() != null && visulization.CorrelationId.Trim() != "null" && !visulization.CorrelationId.Contains("null"))
                    {
                        CommonUtility.ValidateInputFormData(visulization.CorrelationId, "CorrelationId", true);
                    }
                    CommonUtility.ValidateInputFormData(visulization.FMCorrelationId, "FMCorrelationId", true);
                    CommonUtility.ValidateInputFormData(visulization.ClientUID, "ClientUID", true);
                    CommonUtility.ValidateInputFormData(visulization.DCUID, "DCUID", true);
                    CommonUtility.ValidateInputFormData(visulization.ModelName, "ModelName", false);
                    CommonUtility.ValidateInputFormData(visulization.Category, "Category", false);

                    uploadResponse = _cascadingService.FmFileUpload(visulization, fileCollection, out bool isException, out string errorMessage);
                    if (isException)
                    {
                        uploadResponse.ErrorMessage = errorMessage;
                        uploadResponse.IsException = true;
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataTransformationController), nameof(UploadData), "END", string.Empty, string.Empty, visulization.ClientUID, visulization.DCUID);
                        return GetFaultResponse(uploadResponse);
                    }
                }
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataTransformationController), nameof(UploadData), "END", string.Empty, string.Empty, visulization.ClientUID, visulization.DCUID);
                return Ok(uploadResponse);
            }
            catch (Exception ex)
            {
                uploadResponse.Status = CONSTANTS.E;
                uploadResponse.IsException = true;
                uploadResponse.ErrorMessage = ex.Message + "***STACKTRACE***" + ex.StackTrace;
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(DataTransformationController), nameof(UploadData), ex.Message, ex, string.Empty, string.Empty, visulization.ClientUID, visulization.DCUID);
                return GetFaultResponse(uploadResponse);
            }
        }

        [Route("api/FMModelTrainingStatus")]
        [HttpGet]
        public IActionResult FMModelTrainingStatus(string correlationId, string FMCorrelationId, string userId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataTransformationController), nameof(FMModelTrainingStatus), "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            FMVisualizeModelTraining visualizeModelTraining = new FMVisualizeModelTraining();
            if (correlationId == CONSTANTS.undefined
                    || FMCorrelationId == CONSTANTS.undefined
                    || userId == CONSTANTS.undefined
                    || correlationId == CONSTANTS.Null
                    || FMCorrelationId == CONSTANTS.Null
                    || userId == CONSTANTS.Null)
            {
                visualizeModelTraining.ErrorMessage = CONSTANTS.InputFieldsAreNull;
                visualizeModelTraining.Status = CONSTANTS.E;
                return Ok(visualizeModelTraining);
            }
            if (!CommonUtility.GetValidUser(userId))
            {
                return GetFaultResponse(Resource.IngrainResx.InValidUser);
            }
            try
            {
                visualizeModelTraining = _cascadingService.GetFMModelTrainingStatus(correlationId, FMCorrelationId, userId);
                if (visualizeModelTraining.Status == CONSTANTS.E)
                {
                    return GetFaultResponse(visualizeModelTraining);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(DataTransformationController), nameof(FMModelTrainingStatus), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(visualizeModelTraining);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataTransformationController), nameof(FMModelTrainingStatus), "END", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            return Ok(visualizeModelTraining);
        }

        [Route("api/GetFMVisualizationPrediction")]
        [HttpGet]
        public IActionResult GetFMVisualizationPrediction(string correlationId, string uniqId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataTransformationController), nameof(FMModelTrainingStatus), "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            FMPredictionResult visualizeModelTraining = new FMPredictionResult();
            if (correlationId == CONSTANTS.undefined
                    || uniqId == CONSTANTS.undefined
                    || correlationId == CONSTANTS.Null
                    || uniqId == CONSTANTS.Null)
            {
                visualizeModelTraining.ErrorMessage = CONSTANTS.InputFieldsAreNull;
                visualizeModelTraining.Status = CONSTANTS.E;
                return Ok(visualizeModelTraining);
            }
            try
            {
                visualizeModelTraining = _cascadingService.GetFMPrediction(correlationId, uniqId);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(DataTransformationController), nameof(FMModelTrainingStatus), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(visualizeModelTraining);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataTransformationController), nameof(FMModelTrainingStatus), "END", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            return Ok(visualizeModelTraining);
        }
        [Route("api/FMModelsDelete")]
        [HttpGet]
        public IActionResult FMModelsDelete(string correlationId, string fmCorrelationId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataTransformationController), nameof(FMModelsDelete), "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            bool IsSuccess = false;
            if (correlationId == CONSTANTS.undefined || correlationId == CONSTANTS.Null)
            {
                string message = CONSTANTS.InputFieldsAreNull;
                return Ok(message);
            }
            try
            {
                string flushStatus = _flushService.FlushModel(correlationId, string.Empty);
                string flushStatus2 = _flushService.FlushModel(fmCorrelationId, string.Empty);
                if (!string.IsNullOrEmpty(flushStatus) && !string.IsNullOrEmpty(flushStatus2))
                {
                    IsSuccess = true;
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataTransformationController), nameof(FMModelsDelete), "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                    return GetSuccessResponse(IsSuccess);
                }
                else
                    return GetSuccessResponse(IsSuccess);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(DataTransformationController), nameof(FMModelTrainingStatus), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
        }

        #endregion
    }
}