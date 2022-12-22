#region Copyright (c) 2019 Accenture . All rights reserved.

// <copyright company="Accenture">
// Copyright (c) 2019 Accenture.  All rights reserved.
// Reproduction or transmission in whole or in part, in any form or by any means, 
// electronic, mechanical or otherwise, is prohibited without the prior written 
// consent of the copyright owner.
// </copyright>

#endregion

#region DataProcessingController Information
/********************************************************************************************************\
Module Name     :   DataProcessingController
Project         :   Accenture.MyWizard.SelfServiceAI.DataModellingController
Organisation    :   Accenture Technologies Ltd.
Created By      :   RaviA
Created Date    :   30-Jan-2019
Revision History :
Revision No             Initial             Modified Date           Description
1.0                     An                  30-Jan-2019             
\********************************************************************************************************/
#endregion

namespace Accenture.MyWizard.Ingrain.WebService.Controllers
{
    #region Namespace
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Threading;
    using LOGGING = Accenture.MyWizard.LOGGING;
    using MongoDB.Bson;
    using Accenture.MyWizard.Ingrain.WebService;
    using Microsoft.AspNetCore.Mvc;
    using Accenture.MyWizard.Ingrain.DataModels.Models;
    using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
    using Microsoft.Extensions.DependencyInjection;
    using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
    using Accenture.MyWizard.Ingrain.BusinessDomain.Common;
    #endregion

    /// <summary>
    /// Data Processing Controller for Data Cleanup and Trasformation 
    /// </summary>
    public class DataProcessingController : MyWizardControllerBase
    {
        #region Members
        public static IProcessDataService processDataService { set; get; }
        public static IIngestedData ingestedDataService { set; get; }
        DataEngineeringDTO dataCleanUpData = null;
        string PythonResult = string.Empty;
        #endregion

        #region Constructors
        public DataProcessingController(IServiceProvider serviceProvider)
        {
            processDataService = serviceProvider.GetService<IProcessDataService>();
            ingestedDataService = serviceProvider.GetService<IIngestedData>();
        }
        #endregion

        #region Methods

        /// <summary>
        /// Check the Queue table status and get the Processed Model Data.
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="userId"></param>
        /// <param name="pageInfo"></param>
        /// <returns>success with progress or Processed Model Data</returns>
        [HttpGet]
        [Route("api/ProcessDataForModelling")]
        public IActionResult ProcessDataForModelling(string correlationId, string userId, string pageInfo)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataProcessingController), CONSTANTS.ProcessDataForModelling + correlationId + userId + pageInfo, CONSTANTS.START, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);

            try
            {
                if (!string.IsNullOrEmpty(correlationId) || !string.IsNullOrEmpty(userId) || !string.IsNullOrEmpty(pageInfo))
                {
                    if (!CommonUtility.GetValidUser(userId))
                    {
                        return GetFaultResponse(Resource.IngrainResx.InValidUser);
                    }
                    var result = processDataService.GetDataForModelProcessing(correlationId, userId, pageInfo, out dataCleanUpData);
                    switch (result)
                    {
                        case CONSTANTS.Success:
                            PythonResult resultPython = new PythonResult();
                            resultPython.message = CONSTANTS.success;
                            resultPython.status = CONSTANTS.True_Value;
                            PythonResult = resultPython.ToJson();
                            return Ok(PythonResult);
                        case CONSTANTS.Empty:
                            return Ok(new { response = Resource.IngrainResx.EmptyData });
                        case CONSTANTS.P: return Accepted(dataCleanUpData);
                        case CONSTANTS.C: return Ok(dataCleanUpData);
                    }
                    return Ok(dataCleanUpData);
                }
                else
                {
                    return NotFound(new { response = Resource.IngrainResx.CorrelatioUIDEmpty });
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(DataProcessingController), nameof(ProcessDataForModelling), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return BadRequest(new { respone = ex.Message + ex.StackTrace });
            }

        }

        /// <summary>
        /// ColumnsPostAPI
        /// </summary>
        /// <returns>All the columns</returns>
        [HttpPost]
        [Route("api/PostCleanedData")]     //DataCleanUP PostAPI
        public IActionResult PostCleanedData([FromBody]dynamic requestBody)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataProcessingController), nameof(PostCleanedData), "START", string.Empty, string.Empty, string.Empty, string.Empty);
            string PythonResult = string.Empty;
            DataEngineeringDTO dataEngineeringDTO = new DataEngineeringDTO();
            try
            {
                string columnsData = Convert.ToString(requestBody);
                if (!string.IsNullOrEmpty(columnsData))
                {
                    dynamic dynamicColumns = JsonConvert.DeserializeObject(columnsData);
                    if (dynamicColumns.correlationId != null && dynamicColumns.correlationId != "undefined")
                    {
                        //var result = processDataService.PostCleanedData(dynamicColumns, out dataEngineeringDTO);
                        // return Ok(dataEngineeringDTO);
                        var columns = JObject.Parse(columnsData);
                        string correlationId = columns["correlationId"].ToString();
                        string userId = columns["userId"].ToString();
                        string pageInfo = columns["pageInfo"].ToString();
                        if (!CommonUtility.GetValidUser(userId))
                        {
                            return GetFaultResponse(Resource.IngrainResx.InValidUser);
                        }
                        if (!CommonUtility.IsValidGuid(correlationId))
                        {
                            return GetFaultResponse(string.Format(CONSTANTS.InValidGUID, "correlationId"));
                        }
                        if (!CommonUtility.IsDataValid(pageInfo))
                        {
                            return GetFaultResponse(string.Format(CONSTANTS.InValidData, "pageInfo"));
                        }
                        var useCaseData = ingestedDataService.GetRequestUsecase(correlationId, pageInfo);
                        if (string.IsNullOrEmpty(useCaseData))
                        {
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataProcessingController), nameof(PostCleanedData) + "Empty--" + columns, "START-PostCleanedData", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                            processDataService.InsertDataCleanUp(dynamicColumns, correlationId);
                        }
                        //Checking Queue Table and Calling the python.
                        if (!string.IsNullOrEmpty(useCaseData))
                        {
                            JObject queueData = JObject.Parse(useCaseData);
                            string status = (string)queueData["Status"];
                            string progress = (string)queueData["Progress"];
                            string message = (string)queueData["Message"];
                            dataEngineeringDTO.Status = status;
                            dataEngineeringDTO.Progress = progress;
                            dataEngineeringDTO.Message = message;
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataProcessingController), "UseCaseTableData", queueData.ToString(), string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                            if (status == "C" & progress == "100")
                            {
                                dataEngineeringDTO = processDataService.ProcessDataForModelling(correlationId, userId, "DataCleanUp");
                                dataEngineeringDTO.useCaseDetails = useCaseData;
                                dataEngineeringDTO.Status = status;
                                dataEngineeringDTO.Progress = progress;
                                dataEngineeringDTO.Message = message;
                                var deleted = processDataService.RemoveQueueRecords(correlationId, pageInfo);
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataProcessingController), nameof(PostCleanedData), "RemoveQueueRecords" + deleted, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataProcessingController), nameof(PostCleanedData), "END", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                                return Ok(dataEngineeringDTO);
                            }
                            else
                            {
                                if (dataEngineeringDTO.Status == null)
                                {
                                    dataEngineeringDTO.Status = "P";
                                    dataEngineeringDTO.Progress = "0";
                                    dataEngineeringDTO.Message = null;
                                }
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataProcessingController), nameof(PostCleanedData), "END", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                                return Accepted(dataEngineeringDTO);
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
                                pageInfo = pageInfo,
                                ParamArgs = "{}",
                                Function = "DataCleanUp",
                                CreatedByUser = userId,
                                CreatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                                ModifiedByUser = userId,
                                ModifiedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                                LastProcessedOn = null,
                            };
                            ingestedDataService.InsertRequests(ingrainRequest);
                            Thread.Sleep(2000);
                            PythonResult resultPython = new PythonResult();
                            resultPython.message = "success";
                            resultPython.status = "true";
                            PythonResult = resultPython.ToJson();
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataProcessingController), nameof(PostCleanedData), "End", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                            // return Ok(new { response = PythonResult });
                            return Ok(PythonResult);
                        }
                    }
                    else
                    {
                        return GetFaultResponse(Resource.IngrainResx.CorrelatioUIDEmpty);
                    }
                    //}
                    //    else
                    //    {
                    //        return GetFaultResponse(Resource.IngrainResx.CorrelatioUIDEmpty);
                    //    }
                }
                else
                {
                    return GetFaultResponse(Resource.IngrainResx.InputData);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(DataProcessingController), nameof(PostCleanedData), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
                //return BadRequest(new { respone = ex.Message + ex.StackTrace });
            }
        }

        /// <summary>
        /// <returns>All the columns</returns>
        [HttpGet]
        [Route("api/RemovePrescriptionColumns")]     //ColumnsPostAPI
        public IActionResult RemovePrecripationColumns(string correlationId, [FromQuery] string[] prescriptionColumns)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataProcessingController), nameof(RemovePrecripationColumns), "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            string updateResult = string.Empty;
            try
            {
                if (prescriptionColumns != null)
                {
                    updateResult = ingestedDataService.RemoveColumns(correlationId, prescriptionColumns);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(DataProcessingController), nameof(RemovePrecripationColumns), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataProcessingController), nameof(RemovePrecripationColumns), "END", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            return Ok("Success. " + updateResult);
        }

        /// <summary>
        /// Get the Missing Values, Filters and Data Encoding.
        /// </summary>
        /// <param name="correlationId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("api/GetPreProcessData")]            ///PreProcess GetAPI
        public IActionResult GetPreProcessData(string correlationId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataProcessingController), nameof(GetPreProcessData), "Start", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            PreProcessModelDTO preProcessDTO = new PreProcessModelDTO();
            try
            {
                if (!string.IsNullOrEmpty(correlationId))
                {
                    preProcessDTO = processDataService.GetProcessingData(correlationId);
                }
                else
                {
                    return Accepted(Resource.IngrainResx.CorrelatioUIDEmpty);
                }

            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(DataProcessingController), "CorrelationID - " + correlationId, ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataProcessingController), nameof(GetPreProcessData), "END", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            return Ok(preProcessDTO);
        }

        [Route("api/PostPreProcessingData")]
        [HttpPost]                                 //Preprocess PostAPI
        public IActionResult PostPreProcessingData([FromBody]dynamic requestBody)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataProcessingController), nameof(PostPreProcessingData), "START", string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {

                string preProcessData = Convert.ToString(requestBody);
                if (!string.IsNullOrEmpty(preProcessData))
                {
                    dynamic data = Newtonsoft.Json.JsonConvert.DeserializeObject(preProcessData);
                    var columns = JObject.Parse(preProcessData);
                    if (data.correlationId != null && data.correlationId != "undefined")
                    {
                        if (!CommonUtility.IsValidGuid(Convert.ToString(columns["correlationId"])))
                        {
                            return GetFaultResponse(string.Format(CONSTANTS.InValidGUID, "correlationId"));
                        }

                        if (!CommonUtility.IsDataValid(Convert.ToString(data)))
                        {
                            return GetFaultResponse(string.Format(CONSTANTS.InValidData, "PreProcessData"));
                        }

                        string correlationId = columns["correlationId"].ToString();
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataProcessingController), nameof(PostPreProcessingData) + correlationId, "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                        string result = processDataService.PostPreprocessData(data, correlationId);

                        if (string.IsNullOrEmpty(result))
                            return Ok(Resource.IngrainResx.CorrelatioUIdNotMatch);
                    }
                    else
                    {
                        return GetFaultResponse(Resource.IngrainResx.CorrelatioUIDEmpty);
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(DataProcessingController), nameof(PostPreProcessingData), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataProcessingController), nameof(PostPreProcessingData), "END", string.Empty, string.Empty, string.Empty, string.Empty);
            return Ok("Success");
        }

        /// <summary>
        /// PostAddFeatures
        /// </summary>
        /// <param name="requestBody"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/PostAddFeatures")]     //DataCleanUP PostAddFeatureAPI
        public IActionResult PostAddFeatures([FromBody] dynamic requestBody)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataProcessingController), nameof(PostAddFeatures), "START", string.Empty, string.Empty, string.Empty, string.Empty);
            string PythonResult = string.Empty;
            DataEngineeringDTO dataEngineeringDTO = new DataEngineeringDTO();
            try
            {
                string columnsData = Convert.ToString(requestBody);
                if (!string.IsNullOrEmpty(columnsData))
                {
                    dynamic dynamicColumns = JsonConvert.DeserializeObject(columnsData);
                    if (dynamicColumns.correlationId != null && dynamicColumns.correlationId != "undefined")
                    {
                        var columns = JObject.Parse(columnsData);
                        string correlationId = columns["correlationId"].ToString();
                        string userId = columns["userId"].ToString();
                        string pageInfo = columns["pageInfo"].ToString();
                        if (!CommonUtility.GetValidUser(userId))
                        {
                            return GetFaultResponse(Resource.IngrainResx.InValidUser);
                        }

                        if (!CommonUtility.IsValidGuid(correlationId))
                        {
                            return GetFaultResponse(string.Format(CONSTANTS.InValidGUID, "correlationId"));
                        }

                        if (!CommonUtility.IsDataValid(pageInfo))
                        {
                            return GetFaultResponse(string.Format(CONSTANTS.InValidData, "pageInfo"));
                        }

                        var useCaseData = ingestedDataService.GetRequestUsecase(correlationId, pageInfo);
                        if (string.IsNullOrEmpty(useCaseData))
                        {
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataProcessingController), nameof(PostAddFeatures) + "Empty--" + columns, "START-PostAddFeatures", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                            processDataService.InsertAddFeatures(dynamicColumns, correlationId);
                        }
                        //Checking Queue Table and Calling the python.
                        if (!string.IsNullOrEmpty(useCaseData))
                        {
                            JObject queueData = JObject.Parse(useCaseData);
                            string status = (string)queueData["Status"];
                            string progress = (string)queueData["Progress"];
                            string message = (string)queueData["Message"];
                            dataEngineeringDTO.Status = status;
                            dataEngineeringDTO.Progress = progress;
                            dataEngineeringDTO.Message = message;                           
                            if (status == "C" & progress == "100")
                            {
                                dataEngineeringDTO = processDataService.ProcessDataForModelling(correlationId, userId, "DataCleanUp");
                                dataEngineeringDTO.useCaseDetails = useCaseData;
                                dataEngineeringDTO.Status = status;
                                dataEngineeringDTO.Progress = progress;
                                dataEngineeringDTO.Message = message;
                                var deleted = processDataService.RemoveQueueRecords(correlationId, pageInfo);
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataProcessingController), nameof(PostAddFeatures), "RemoveQueueRecords" + deleted, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataProcessingController), nameof(PostAddFeatures), "END", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                                return Ok(dataEngineeringDTO);
                            }
                            else
                            {
                                if (dataEngineeringDTO.Status == null)
                                {
                                    dataEngineeringDTO.Status = "P";
                                    dataEngineeringDTO.Progress = "0";
                                    dataEngineeringDTO.Message = null;
                                }
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataProcessingController), nameof(PostAddFeatures), "END", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                                return Accepted(dataEngineeringDTO);
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
                                pageInfo = "AddFeature",
                                ParamArgs = "{}",
                                Function = "AddFeature",
                                CreatedByUser = userId,
                                CreatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                                ModifiedByUser = userId,
                                ModifiedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                                LastProcessedOn = null,
                            };
                            ingestedDataService.InsertRequests(ingrainRequest);
                            Thread.Sleep(1000);
                            PythonResult resultPython = new PythonResult();
                            resultPython.message = "success";
                            resultPython.status = "true";
                            PythonResult = resultPython.ToJson();
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataProcessingController), nameof(PostAddFeatures), "End", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);                          
                            return Ok(PythonResult);
                        }
                    }
                    else
                    {
                        return GetFaultResponse(Resource.IngrainResx.CorrelatioUIDEmpty);
                    }                  
                }
                else
                {
                    return GetFaultResponse(Resource.IngrainResx.InputData);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(DataProcessingController), nameof(PostAddFeatures), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
                //return BadRequest(new { respone = ex.Message + ex.StackTrace });
            }
        }

        [Route("api/ApplyPreProcessingData")]
        [HttpPost]                                 // Apply Preprocess
        public IActionResult ApplyPreProcessingData(string correlationId, string userId, string pageInfo, [FromBody]dynamic requestBody)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataProcessingController), nameof(ApplyPreProcessingData), "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            var useCaseDetails = string.Empty;
            PreProcessModelDTO preProcessDTO = new PreProcessModelDTO();
            try
            {
                if (!string.IsNullOrEmpty(correlationId) && !string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(pageInfo))
                {
                    #region VALIDATIONS
                    if (!CommonUtility.GetValidUser(userId))
                    {
                        return GetFaultResponse(Resource.IngrainResx.InValidUser);
                    }
                    string processData = Convert.ToString(requestBody);
                    dynamic data = JsonConvert.DeserializeObject(processData);
                    var columns = JObject.Parse(processData);

                    if (!CommonUtility.IsDataValid(Convert.ToString(processData)))
                    {
                        return GetFaultResponse(string.Format(CONSTANTS.InValidData, "processData"));
                    }
                    CommonUtility.ValidateInputFormData(correlationId, CONSTANTS.CorrelationId, true);
                    #endregion
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataProcessingController), nameof(PostPreProcessingData) + correlationId, "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                    string result = processDataService.PostPreprocessData(data, correlationId);

                    if (string.IsNullOrEmpty(result))
                        return Ok(Resource.IngrainResx.CorrelatioUIdNotMatch);

                    IngrainRequestQueue requestQueue = new IngrainRequestQueue();
                    requestQueue = ingestedDataService.GetFileRequestStatus(correlationId, "DataPreprocessing");
                    if (requestQueue != null)
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataProcessingController), "requestQueue", requestQueue.ToString(), string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);                        
                        preProcessDTO.Status = requestQueue.Status;
                        preProcessDTO.Progress = requestQueue.Progress;
                        preProcessDTO.Message = requestQueue.Message;
                        if (requestQueue.Status == "C" && requestQueue.Progress == "100")
                        {
                            processDataService.updateDataTransformationApplied(correlationId);
                            preProcessDTO = processDataService.GetProcessingData(correlationId);
                            preProcessDTO.Status = requestQueue.Status;
                            preProcessDTO.Progress = requestQueue.Progress;
                            preProcessDTO.Message = requestQueue.Message;                            
                            bool deleted = processDataService.RemoveQueueRecords(correlationId, pageInfo);
                            ValidRecordsDetailsModel validRecordsDetailModel = ingestedDataService.GetDataPoints(correlationId);
                            if (validRecordsDetailModel != null)
                            {
                                if (validRecordsDetailModel.ValidRecordsDetails != null)
                                {
                                    preProcessDTO.DataPointsCount = validRecordsDetailModel.ValidRecordsDetails.Records[0];
                                    if (validRecordsDetailModel.ValidRecordsDetails.Records[0] >= 4 && validRecordsDetailModel.ValidRecordsDetails.Records[0] < 20)
                                    {
                                        preProcessDTO.DataPointsWarning = CONSTANTS.DataTransformationMinimum;
                                    }
                                }
                            }
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataProcessingController), nameof(ApplyPreProcessingData), "END", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                            return Ok(preProcessDTO);
                        }
                        else if (requestQueue.Status == "E")
                        {
                            bool deleted = processDataService.RemoveQueueRecords(correlationId, pageInfo);
                            return Ok(preProcessDTO);
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(requestQueue.Status))
                            {
                                preProcessDTO.Status = "P";
                                preProcessDTO.Progress = "0";
                                preProcessDTO.Message = requestQueue.Message;
                            }
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataProcessingController), nameof(ApplyPreProcessingData), "END", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                            return Ok(preProcessDTO);
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
                            pageInfo = "DataPreprocessing",
                            ParamArgs = "{}",
                            Function = "DataTransform",
                            CreatedByUser = userId,
                            CreatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                            ModifiedByUser = userId,
                            ModifiedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                            LastProcessedOn = null,
                        };
                        ingestedDataService.InsertRequests(ingrainRequest);
                        Thread.Sleep(2000);
                        PythonResult pythonResult = new PythonResult();
                        pythonResult.message = "Success";
                        pythonResult.status = "True";
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataProcessingController), nameof(ApplyPreProcessingData), "END", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                        return Ok(pythonResult);
                    }
                }
                else
                {
                    return GetFaultResponse(Resource.IngrainResx.InputData);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(DataProcessingController), nameof(ApplyPreProcessingData), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }

        }

        [HttpPost]
        [Route("api/SmoteTestTechnique")]
        public IActionResult SmoteTestTechnique(string correlationId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataProcessingController), nameof(SmoteTestTechnique), "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                if(!string.IsNullOrEmpty(correlationId))
                {
                    #region VALIDATIONS
                    CommonUtility.ValidateInputFormData(correlationId, CONSTANTS.CorrelationId, true);
                    #endregion
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataProcessingController), nameof(SmoteTestTechnique), "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                    processDataService.SmoteTechnique(correlationId);
                }
                else
                {
                    return GetFaultResponse(Resource.IngrainResx.InputFieldsAreNull);
                }
                
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(DataProcessingController), nameof(SmoteTestTechnique), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataProcessingController), nameof(SmoteTestTechnique), "END", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            return Ok("Success");
        }

        #endregion
    }
}
