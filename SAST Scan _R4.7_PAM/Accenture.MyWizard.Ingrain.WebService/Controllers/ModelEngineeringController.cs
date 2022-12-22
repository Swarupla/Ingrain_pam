#region Copyright (c) 2019 Accenture . All rights reserved.

// <copyright company="Accenture">
// Copyright (c) 2019 Accenture.  All rights reserved.
// Reproduction or transmission in whole or in part, in any form or by any means, 
// electronic, mechanical or otherwise, is prohibited without the prior written 
// consent of the copyright owner.
// </copyright>

#endregion

#region ModelEngineeringController Information
/********************************************************************************************************\
Module Name     :   ModelEngineeringController
Project         :   Accenture.MyWizard.SelfServiceAI.ModelEngineeringController
Organisation    :   Accenture Technologies Ltd.
Created By      :   RaviA
Created Date    :   30-Jan-2019
Revision History :
Revision No             Initial             Modified Date           Description
1.0                     An                  29-Mar-2019             
\********************************************************************************************************/
#endregion

namespace Accenture.MyWizard.SelfServiceAI.WebService.Controllers
{
    #region Namespace
    using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
    using Accenture.MyWizard.Ingrain.DataModels.Models;
    using Accenture.MyWizard.Ingrain.WebService;
    using Microsoft.AspNetCore.Mvc;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using Microsoft.Extensions.DependencyInjection;
    using MongoDB.Bson;
    using Accenture.MyWizard.Ingrain.WebService.Controllers;
    using Microsoft.Extensions.Options;
    using Accenture.MyWizard.Shared.Helpers;
    using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
    using System.Diagnostics;
    using Accenture.MyWizard.Ingrain.BusinessDomain.Common;
    #endregion

    public class ModelEngineeringController : MyWizardControllerBase
    {
        #region Members        

        private FeatureEngineeringDTO _featureEngineering = null;
        private RecommedAITrainedModel _recommendedAI = null;

        private TeachAndTestDTO _teachAndTestDTO = null;
        private static IModelEngineering modelEngineeringService { set; get; }
        private IEncryptionDecryption _encryptionDecryption;
        private static IIngestedData ingestedDataService { set; get; }

        private IServiceProvider _serviceProvider;

        private readonly IOptions<IngrainAppSettings> appSettings;
        private double _timeDiffInMinutes = 0;


        #endregion

        #region Constructors
        public ModelEngineeringController(IServiceProvider serviceProvider, IOptions<IngrainAppSettings> settings)
        {
            modelEngineeringService = serviceProvider.GetService<IModelEngineering>();
            _encryptionDecryption = serviceProvider.GetService<IEncryptionDecryption>();
            ingestedDataService = serviceProvider.GetService<IIngestedData>();
            appSettings = settings;
            _serviceProvider = serviceProvider;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Get the AvailableColumns and user Target and Input Columns
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="IsTemplate"></param>
        /// <returns>All the Columns Including Target or Input Columns</returns>
        [HttpGet]
        [Route("api/GetFeatureSelection")]
        public IActionResult GetFeatureSelection(string correlationId, string userId, string pageInfo)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ModelEngineeringController), nameof(GetFeatureSelection), "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                _featureEngineering = new FeatureEngineeringDTO();
                if (!string.IsNullOrEmpty(correlationId))
                {
                    if (!CommonUtility.GetValidUser(userId))
                    {
                        return GetFaultResponse(Resource.IngrainResx.InValidUser);
                    }
                    _featureEngineering = modelEngineeringService.GetFeatureAttributes(correlationId);
                    RetraingStatus retrain = new RetraingStatus();
                    retrain = modelEngineeringService.GetRetrain(correlationId);
                    _featureEngineering.Retrain = retrain.Retrain;
                    if (!string.IsNullOrEmpty(retrain.IsIntiateRetrain.ToString()))
                        _featureEngineering.IsIntiateRetrain = retrain.IsIntiateRetrain;

                }
                else
                {
                    return GetFaultResponse(Resource.IngrainResx.CorrelatioUIDEmpty);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ModelEngineeringController), nameof(GetFeatureSelection), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ModelEngineeringController), nameof(GetFeatureSelection), "END", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            return GetSuccessWithMessageResponse(_featureEngineering);
        }


        /// <summary>
        /// ColumnsPostAPI
        /// </summary>
        /// <returns>All the columns</returns>
        [HttpPost]
        [Route("api/PostFeatureSelection")]     //DataCleanUP PostAPI
        public IActionResult PostFeatureSelection([FromBody]dynamic requestBody)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ModelEngineeringController), nameof(PostFeatureSelection), "START", string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                string columnsData = Convert.ToString(requestBody);
                if (!string.IsNullOrEmpty(columnsData))
                {
                    var dynamicColumns = Newtonsoft.Json.JsonConvert.DeserializeObject(columnsData);
                    var columns = JObject.Parse(columnsData);
                    string correlationId = columns["CorrelationId"].ToString();

                    #region VALIDATIONS
                    CommonUtility.ValidateInputFormData(correlationId, CONSTANTS.CorrelationId, true);
                    CommonUtility.ValidateInputFormData(Convert.ToString(columns[CONSTANTS.FeatureImportance]), CONSTANTS.FeatureImportance, false);
                    CommonUtility.ValidateInputFormData(Convert.ToString(columns[CONSTANTS.Train_Test_Split]), CONSTANTS.Train_Test_Split, false);
                    CommonUtility.ValidateInputFormData(Convert.ToString(columns[CONSTANTS.KFoldValidation]), CONSTANTS.KFoldValidation, false);
                    #endregion

                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(ModelEngineeringController), nameof(PostFeatureSelection) + correlationId, "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                    modelEngineeringService.UpdateFeatures(dynamicColumns, correlationId);
                }
                else
                {
                    return GetFaultResponse(Resource.IngrainResx.EmptyReqBody);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ModelEngineeringController), nameof(PostFeatureSelection), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ModelEngineeringController), nameof(PostFeatureSelection), "END", string.Empty, string.Empty, string.Empty, string.Empty);
            return Ok("Success");
        }

        /// <summary>
        /// Get the recommended AI
        /// </summary>
        /// <param name="correlationId">The correlation identifier</param>
        /// <returns>Returns recommended AI data.</returns>
        [HttpGet]
        [Route("api/GetRecommendedAI")]
        public IActionResult GetRecommendedAI(string correlationId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ModelEngineeringController), nameof(GetRecommendedAI), "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            RetraingStatus retrain = new RetraingStatus();
            try
            {
                var trainedData = modelEngineeringService.GetRecommendedTrainedModels(correlationId, null);
                retrain = modelEngineeringService.GetRetrain(correlationId);
                //Check retrain models triggered
                if (trainedData.IsInitiateRetrain)
                {
                    if (!string.IsNullOrEmpty(correlationId) && correlationId != "undefined")
                    {
                        var data = modelEngineeringService.GetRecommendedAI(correlationId);
                        if (data.SelectedModels != null)
                        {
                            data.Retrain = retrain.Retrain;
                            if (!string.IsNullOrEmpty(retrain.IsIntiateRetrain.ToString()))
                                data.IsInitiateRetrain = retrain.IsIntiateRetrain;
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ModelEngineeringController), nameof(GetRecommendedAI), "END", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                            return GetSuccessWithMessageResponse(data);

                        }
                        else
                        {
                            data.Message = Resource.IngrainResx.CorrelatioUIdNotMatch;
                            return GetSuccessWithMessageResponse(data);
                        }
                    }
                    else
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(ModelEngineeringController), nameof(GetRecommendedAI), "trainedData Empty", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                        return Accepted(Resource.IngrainResx.CorrelatioUIDEmpty);
                    }
                }
                else
                {
                    //Check whether model already trained
                    if (trainedData.TrainedModel != null && trainedData.TrainedModel.Count > 0)
                    {
                        trainedData.IsModelTrained = true;
                        trainedData.Retrain = retrain.Retrain;
                        if (!string.IsNullOrEmpty(retrain.IsIntiateRetrain.ToString()))
                            trainedData.IsInitiateRetrain = retrain.IsIntiateRetrain;
                        trainedData.SelectedModel = trainedData.TrainedModel[0]["ProblemType"].ToString();
                        return GetSuccessWithMessageResponse(trainedData);
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(correlationId) && correlationId != "undefined")
                        {
                            var data = modelEngineeringService.GetRecommendedAI(correlationId);
                            if (data.SelectedModels != null)
                            {
                                data.Retrain = retrain.Retrain;
                                if (!string.IsNullOrEmpty(retrain.IsIntiateRetrain.ToString()))
                                    data.IsInitiateRetrain = retrain.IsIntiateRetrain;
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(ModelEngineeringController), nameof(GetRecommendedAI), "END", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                                return GetSuccessWithMessageResponse(data);

                            }
                            else
                            {
                                data.Message = Resource.IngrainResx.CorrelatioUIdNotMatch;
                                return GetSuccessWithMessageResponse(data);
                            }
                        }
                        else
                        {
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ModelEngineeringController), nameof(GetRecommendedAI), "trainedData Empty", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                            return Accepted(Resource.IngrainResx.CorrelatioUIDEmpty);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ModelEngineeringController), nameof(GetRecommendedAI), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }

        }

        /// <summary>
        /// Updates the recommended model AI
        /// </summary>
        /// <returns>Updates the data</returns>       
        [HttpPost]
        [Route("api/PostRecommendedAI")]
        public IActionResult PostRecommendedAI([FromBody]dynamic requestBody)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ModelEngineeringController), nameof(PostRecommendedAI), "START", string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                string columnsData = Convert.ToString(requestBody);
                if (!string.IsNullOrEmpty(columnsData))
                {
                    dynamic dynamicColumns = Newtonsoft.Json.JsonConvert.DeserializeObject(columnsData);
                    if (dynamicColumns.CorrelationId != null && dynamicColumns.CorrelationId != "undefined")
                    {
                        var columns = JObject.Parse(columnsData);
                        string correlationId = columns["CorrelationId"].ToString();

                        #region VALIDATIONS
                        CommonUtility.ValidateInputFormData(correlationId, CONSTANTS.CorrelationId, true);
                        CommonUtility.ValidateInputFormData(Convert.ToString(columns[CONSTANTS.SelectedModels]), CONSTANTS.SelectedModels, false);
                        #endregion

                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(ModelEngineeringController), nameof(PostRecommendedAI) + correlationId, "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                        modelEngineeringService.UpdateRecommendedModelTypes(dynamicColumns, correlationId);
                    }
                    else
                    {
                        return GetSuccessWithMessageResponse(Resource.IngrainResx.CorrelatioUIDEmpty);
                    }
                }

            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ModelEngineeringController), nameof(PostRecommendedAI), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ModelEngineeringController), nameof(PostRecommendedAI), "END", string.Empty, string.Empty, string.Empty, string.Empty);
            return Ok("Success");
        }

        /// <summary>
        /// Get the recommended AI
        /// </summary>
        /// <param name="correlationId">The correlation identifier</param>
        /// <returns>Returns recommended AI data.</returns>
        [HttpGet]
        [Route("api/GetRecommendedTrainedModels")]
        public IActionResult GetRecommendedTrainedModels(string correlationId, string userId, string pageInfo, int noOfModelsSelected)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ModelEngineeringController), nameof(GetRecommendedTrainedModels), "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            bool flag = true;
            int count = 0;
            int errorCount = 0;
            bool retrain = false;
            bool modelTrained = false;
            bool isInitiatedRetrain = false;
            int inProgressModels = 0;
            int completedModels = 0;
            var cPUMemoryCount = new CPUMemoryUtilizeCount();
            try
            {
                if (!string.IsNullOrEmpty(correlationId) && !string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(pageInfo))
                {
                    if (!CommonUtility.GetValidUser(userId))
                    {
                        return GetFaultResponse(Resource.IngrainResx.InValidUser);
                    }

                    _recommendedAI = new RecommedAITrainedModel();
                    int executeCount = 0;
                ExecuteQueueTable:
                    IngrainRequestQueue requestQueue = new IngrainRequestQueue();
                    isInitiatedRetrain = modelEngineeringService.IsInitiateRetrain(correlationId);
                    List<IngrainRequestQueue> useCaseDetails = new List<IngrainRequestQueue>();
                    if (!isInitiatedRetrain)
                        useCaseDetails = modelEngineeringService.GetMultipleRequestStatus(correlationId, pageInfo);
                    if (useCaseDetails.Count > 0)
                    {
                        DateTime dateTime = DateTime.Parse(useCaseDetails[0].CreatedOn);
                        DateTime currentTime = DateTime.Parse(DateTime.Now.ToString(CONSTANTS.DateHoursFormat));
                        _timeDiffInMinutes = (currentTime - dateTime).TotalMinutes;
                        RetraingStatus Retrain_status = new RetraingStatus();
                        Retrain_status = modelEngineeringService.GetRetrain(correlationId);
                        retrain = Retrain_status.Retrain;
                        List<int> progressList = new List<int>();
                        for (int i = 0; i < useCaseDetails.Count; i++)
                        {
                            string queueStatus = useCaseDetails[i].Status;
                            if (queueStatus != "C" && queueStatus != "E")
                            {
                                flag = false;
                            }
                            if (queueStatus == "C" || queueStatus == "P")
                            {
                                if (queueStatus == "P")
                                    inProgressModels++;
                                if (queueStatus == "C")
                                    completedModels++;
                                progressList.Add(Convert.ToInt32(useCaseDetails[i].Progress));
                                count++;
                            }
                            if (queueStatus == "E")
                            {
                                errorCount++;
                            }
                        }
                        _recommendedAI.UseCaseList = useCaseDetails;
                        int val = 0;
                        foreach (var item in progressList)
                        {
                            val += item;
                        }
                        if (count > 0)
                        {
                            _recommendedAI.CurrentProgress = (val / (noOfModelsSelected - errorCount));
                        }
                        _recommendedAI.IsModelTrained = false;
                        _recommendedAI.Retrain = retrain;
                        if (!string.IsNullOrEmpty(Retrain_status.IsIntiateRetrain.ToString()))
                            _recommendedAI.IsInitiateRetrain = Retrain_status.IsIntiateRetrain;
                        //System configuration code
                        var result = new SystemUsageDetails();
                        if (!appSettings.Value.Environment.Equals(CONSTANTS.PAMEnvironment))
                        {
                             result = cPUMemoryCount.GetMetrics(appSettings.Value.Environment, appSettings.Value.IsSaaSPlatform);
                            _recommendedAI.CPUUsage = result.CPUUsage;
                            _recommendedAI.MemoryUsageInMB = result.MemoryUsageInMB;
                        }
                        if (flag)
                        {
                            Thread.Sleep(1000);
                            if (noOfModelsSelected != useCaseDetails.Count && executeCount == 0)
                            {
                                executeCount++;
                                errorCount = 0;
                                inProgressModels = 0;
                                completedModels = 0;
                                goto ExecuteQueueTable;
                            }
                            else
                            {
                                _recommendedAI = modelEngineeringService.GetRecommendedTrainedModels(correlationId, null);
                                _recommendedAI.UseCaseList = useCaseDetails;
                                if (count > 0)
                                {
                                    _recommendedAI.CurrentProgress = (val / (noOfModelsSelected - errorCount));
                                }
                                _recommendedAI.IsModelTrained = true;
                                if (!appSettings.Value.Environment.Equals(CONSTANTS.PAMEnvironment))
                                {
                                    _recommendedAI.CPUUsage = result.CPUUsage;
                                    _recommendedAI.MemoryUsageInMB = result.MemoryUsageInMB;
                                    modelEngineeringService.InsertUsage(_recommendedAI.CurrentProgress, _recommendedAI.CPUUsage, correlationId);
                                }
                                _recommendedAI.Retrain = retrain;
                                if (!string.IsNullOrEmpty(Retrain_status.IsIntiateRetrain.ToString()))
                                    _recommendedAI.IsInitiateRetrain = Retrain_status.IsIntiateRetrain;
                                modelEngineeringService.InsertUsage(_recommendedAI.CurrentProgress, 0, correlationId);
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(ModelEngineeringController), nameof(GetRecommendedTrainedModels), "END" , string.IsNullOrEmpty(correlationId) ? Guid.Empty : new Guid(correlationId), "", "", "", "");
                                return GetSuccessWithMessageResponse(_recommendedAI);
                            }
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(useCaseDetails[0].Status))
                            {
                                for (int i = 0; i < useCaseDetails.Count; i++)
                                {
                                    useCaseDetails[i].Status = "P";
                                    useCaseDetails[i].Progress = "0";
                                    _recommendedAI.CurrentProgress = 1;
                                    break;
                                }
                                _recommendedAI.UseCaseList = useCaseDetails;
                            }
                            //If appsettings "ModelsTrainingTimeLimit" time exceeds the current model training and atleast one model training completed
                            //than we can kill remaining processes and we can show the completed models at UI
                            if (_timeDiffInMinutes > appSettings.Value.ModelsTrainingTimeLimit && completedModels > 0)
                            {
                                _recommendedAI = modelEngineeringService.GetRecommendedTrainedModels(correlationId, null);
                                _recommendedAI.UseCaseList = useCaseDetails;
                                if (count > 0)
                                {
                                    _recommendedAI.CurrentProgress = 100;
                                }
                                _recommendedAI.IsModelTrained = true;
                                if (!appSettings.Value.Environment.Equals(CONSTANTS.PAMEnvironment))
                                {
                                    _recommendedAI.CPUUsage = result.CPUUsage;
                                    _recommendedAI.MemoryUsageInMB = result.MemoryUsageInMB;
                                    modelEngineeringService.InsertUsage(_recommendedAI.CurrentProgress, _recommendedAI.CPUUsage, correlationId);
                                }
                                _recommendedAI.Retrain = retrain;
                                if (!string.IsNullOrEmpty(Retrain_status.IsIntiateRetrain.ToString()))
                                    _recommendedAI.IsInitiateRetrain = Retrain_status.IsIntiateRetrain;                               

                                //Kill the already existing process of the python
                                //insert the request to terminate the python process for the remaining inprgress models.
                                modelEngineeringService.TerminateModelsTrainingRequests(correlationId, useCaseDetails);
                                //End
                            }
                            //end
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ModelEngineeringController), nameof(GetRecommendedTrainedModels), "END", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                            return GetSuccessWithMessageResponse(_recommendedAI);
                        }
                    }
                    else
                    {
                        var recommendedModels = modelEngineeringService.GetModelNames(correlationId);
                        foreach (var modelName in recommendedModels.Item1)
                        {
                            IngrainRequestQueue ingrainRequest = new IngrainRequestQueue
                            {
                                _id = Guid.NewGuid().ToString(),
                                CorrelationId = correlationId,
                                RequestId = Guid.NewGuid().ToString(),
                                ProcessId = null,
                                Status = null,
                                ModelName = modelName,
                                RequestStatus = "New",
                                RetryCount = 0,
                                ProblemType = recommendedModels.Item2,
                                Message = null,
                                UniId = null,
                                Progress = null,
                                pageInfo = pageInfo,
                                ParamArgs = "{}",
                                Function = pageInfo,
                                CreatedByUser = userId,
                                CreatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                                ModifiedByUser = userId,
                                ModifiedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                                LastProcessedOn = null,
                            };
                            modelTrained = modelEngineeringService.IsModelsTrained(correlationId);
                            if (isInitiatedRetrain && modelTrained)
                            {
                                ingrainRequest.Function = CONSTANTS.RetrainRecommendedAI;
                            }
                            ingestedDataService.InsertRequests(ingrainRequest);
                        }
                        //Change the IsInitiateRetrain flag to false.
                        modelEngineeringService.UpdateIsRetrainFlag(correlationId);
                        Thread.Sleep(2000);
                        PythonResult pythonResult2 = new PythonResult();
                        pythonResult2.message = "Success..";
                        pythonResult2.status = "True";
                        string pythonResult = pythonResult2.ToJson();
                        return GetSuccessWithMessageResponse(pythonResult);
                    }
                }
                else
                {
                    return Accepted(Resource.IngrainResx.InputData);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ModelEngineeringController), nameof(GetRecommendedTrainedModels), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
        }

        [HttpGet]
        [Route("api/StartRetrainModels")]
        public IActionResult StartRetrainModels(string correlationId, string userId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ModelEngineeringController), nameof(StartRetrainModels), "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            string result = string.Empty;
            try
            {
                if (!string.IsNullOrEmpty(correlationId) && !string.IsNullOrEmpty(userId))
                {
                    if (!CommonUtility.GetValidUser(userId))
                    {
                        return GetFaultResponse(Resource.IngrainResx.InValidUser);
                    }
                    //result = modelEngineeringService.DeleteExistingModels(correlationId, "RecommendedAI");
                    //not deleting old models but creating new flag
                    result = modelEngineeringService.UpdateExistingModels(correlationId, "RecommendedAI");
                }
                else
                {
                    return GetSuccessWithMessageResponse(Resource.IngrainResx.CorrelatioUIDEmpty);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ModelEngineeringController), nameof(StartRetrainModels), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }

            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ModelEngineeringController), nameof(StartRetrainModels), "END", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            return GetSuccessWithMessageResponse(result);


        }

        /// <summary>
        /// Get the model names in Teach and test
        /// </summary>
        /// <param name="correlationId">The correlation identifier</param>
        /// /// <param name="WFId">The WhatIfAnalysisId identifier</param>
        /// <returns>Returns model Names and Scenario name</returns>
        [HttpGet]
        [Route("api/GetTeachModels")]
        public IActionResult GetTeachModels(string correlationId, string WFId, string IstimeSeries, string scenario)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ModelEngineeringController), nameof(GetTeachModels), "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            TeachAndTestDTO teachAndTestDTO = new TeachAndTestDTO();
            try
            {
                if (!string.IsNullOrEmpty(correlationId) && !string.IsNullOrEmpty(WFId))
                {
                    teachAndTestDTO = modelEngineeringService.GetTeachModels(correlationId, WFId, IstimeSeries, scenario);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(ModelEngineeringController), nameof(GetTeachModels), "END", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                    return GetSuccessWithMessageResponse(teachAndTestDTO);
                }
                else
                {
                    return Accepted(Resource.IngrainResx.InputData);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ModelEngineeringController), nameof(GetTeachModels), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
        }

        /// <summary>
        /// Deletes the record in collection based on correlation Id
        /// </summary>
        /// <param name="correlationId">The correlation identifier</param>
        /// <returns>Returns the result.</returns>
        [HttpPost]
        [Route("api/DeleteTrainedModel")]
        public IActionResult DeleteTrainedModel(string correlationId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ModelEngineeringController), nameof(DeleteTrainedModel), "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                if (!string.IsNullOrEmpty(correlationId) && correlationId != "undefined")
                {
                    #region VALIDATIONS
                    CommonUtility.ValidateInputFormData(correlationId, CONSTANTS.CorrelationId, true);                    
                    #endregion

                    modelEngineeringService.DeleteTrainedModel(correlationId);
                }
                else
                {
                    return Accepted(Resource.IngrainResx.CorrelatioUIDEmpty);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ModelEngineeringController), nameof(DeleteTrainedModel), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ModelEngineeringController), nameof(DeleteTrainedModel), "END", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            return Ok("Success");
        }

        ///// <summary>
        ///// Get the Ingested Columns
        ///// </summary>
        ///// <returns>All the columns</returns>
        //[HttpPost]
        //[Route("api/RunTest")]     //ColumnsPostAPI
        //public IActionResult RunTest([FromBody]dynamic requestBody)
        //{
        //    LOGGING.LogManager.Logger.LogProcessInfo(typeof(ModelEngineeringController), nameof(RunTest), "START");
        //    FeaturePredictionTestDTO featurePredictionTest = new FeaturePredictionTestDTO();
        //    var pythonResult = string.Empty;
        //    try
        //    {
        //        string columnsData = Convert.ToString(requestBody);
        //        if (!string.IsNullOrEmpty(columnsData))
        //        {
        //            var dataContent = JObject.Parse(columnsData.ToString());
        //            var dynamicColumns = Newtonsoft.Json.JsonConvert.DeserializeObject<WhatIFAnalysis>(columnsData);
        //            if (dynamicColumns.CorrelationId != null && dynamicColumns.CorrelationId != "undefined")
        //            {
        //                int i = 0;
        //                bool flag = true;
        //                while (flag)
        //                {
        //                    string WFId = string.Empty;
        //                    if (i == 0)
        //                    {
        //                        WFId = Guid.NewGuid().ToString();
        //                        dynamicColumns.WFId = WFId;
        //                    }
        //                    i++;
        //                    IngrainRequestQueue requestQueue = ingestedDataService.GetFileRequestStatus(dynamicColumns.CorrelationId, "WFTeachTest", dynamicColumns.WFId);
        //                    if (requestQueue == null)
        //                    {
        //                        modelEngineeringService.InsertColumns(dynamicColumns);
        //                    }
        //                    if (requestQueue != null)
        //                    {
        //                        //Checking the Queue Collection and Invoking the Python API.
        //                        if (requestQueue != null)
        //                        {
        //                            featurePredictionTest.Status = requestQueue.Status;
        //                            featurePredictionTest.Progress = requestQueue.Progress;
        //                            featurePredictionTest.Message = requestQueue.Message;
        //                            featurePredictionTest.WFId = dynamicColumns.WFId;
        //                            if (requestQueue.Status == "C" & requestQueue.Progress == "100")
        //                            {
        //                                flag = false;
        //                                featurePredictionTest = modelEngineeringService.GetFeaturePredictionForTest(dynamicColumns.CorrelationId, dynamicColumns.WFId, dynamicColumns.Steps);
        //                                featurePredictionTest.Status = requestQueue.Status;
        //                                featurePredictionTest.Progress = requestQueue.Progress;
        //                                featurePredictionTest.Message = requestQueue.Message;
        //                                featurePredictionTest.WFId = dynamicColumns.WFId;
        //                                return GetSuccessWithMessageResponse(featurePredictionTest);
        //                            }
        //                            else if (requestQueue.Status == "E")
        //                            {
        //                                return GetFaultResponse(featurePredictionTest);
        //                            }
        //                            else
        //                            {
        //                                flag = true;
        //                                Thread.Sleep(1000);
        //                            }
        //                        }
        //                    }
        //                    else
        //                    {
        //                        IngrainRequestQueue ingrainRequest = new IngrainRequestQueue
        //                        {
        //                            _id = Guid.NewGuid().ToString(),
        //                            CorrelationId = dynamicColumns.CorrelationId,
        //                            RequestId = Guid.NewGuid().ToString(),
        //                            ProcessId = null,
        //                            Status = null,
        //                            ModelName = dynamicColumns.model,
        //                            RequestStatus = "New",
        //                            RetryCount = 0,
        //                            ProblemType = null,
        //                            Message = null,
        //                            UniId = dynamicColumns.WFId,
        //                            Progress = null,
        //                            pageInfo = "WFTeachTest",
        //                            ParamArgs = null,
        //                            Function = "WFAnalysis",
        //                            CreatedByUser = dynamicColumns.CreatedByUser,
        //                            CreatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
        //                            ModifiedByUser = dynamicColumns.CreatedByUser,
        //                            ModifiedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
        //                            LastProcessedOn = null,
        //                        };
        //                        WfAnalysisParams wfAnalysis = new WfAnalysisParams
        //                        {
        //                            WfId = WFId,
        //                            Bulk = dataContent["bulkData"].ToString()
        //                        };
        //                        ingrainRequest.ParamArgs = wfAnalysis.ToJson();
        //                        ingestedDataService.InsertRequests(ingrainRequest);
        //                        Thread.Sleep(2000);
        //                    }
        //                }
        //            }
        //            else
        //            {
        //                return GetSuccessWithMessageResponse(Resource.IngrainResx.CorrelatioUIDEmpty);
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        LOGGING.LogManager.Logger.LogErrorMessage(typeof(ModelEngineeringController), nameof(RunTest), ex.Message, ex);
        //        return GetFaultResponse(ex.Message + ex.StackTrace);
        //    }
        //    LOGGING.LogManager.Logger.LogProcessInfo(typeof(ModelEngineeringController), nameof(RunTest), "END");
        //    return GetSuccessWithMessageResponse(pythonResult);
        //}



        /// <summary>
        /// Get the Ingested Columns
        /// </summary>
        /// <returns>All the columns</returns>
        [HttpPost]
        [Route("api/RunTest")]     //ColumnsPostAPI
        public IActionResult RunTest([FromBody]dynamic requestBody)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ModelEngineeringController), nameof(RunTest), "START", string.Empty, string.Empty, string.Empty, string.Empty);
            FeaturePredictionTestDTO featurePredictionTest = null;
            try
            {
                string columnsData = Convert.ToString(requestBody);
                if (!string.IsNullOrEmpty(columnsData))
                {
                    var dataContent = JObject.Parse(columnsData.ToString());
                    var dynamicColumns = Newtonsoft.Json.JsonConvert.DeserializeObject<WhatIFAnalysis>(columnsData);

                    #region VALIDATIONS
                    if (!CommonUtility.GetValidUser(dynamicColumns.CreatedByUser) || !CommonUtility.GetValidUser(dynamicColumns.ModifiedByUser))
                        return GetFaultResponse(Resource.IngrainResx.InValidUser);

                    CommonUtility.ValidateInputFormData(dynamicColumns.CorrelationId, CONSTANTS.CorrelationId, true);
                    CommonUtility.ValidateInputFormData(dynamicColumns.WFId, CONSTANTS.WFId, true);
                    CommonUtility.ValidateInputFormData(Convert.ToString(dynamicColumns.Features), CONSTANTS.Features, false);
                    CommonUtility.ValidateInputFormData(dynamicColumns.model, CONSTANTS.model, false);
                    CommonUtility.ValidateInputFormData(dynamicColumns.bulkData, CONSTANTS.BulkData, false);
                    #endregion

                    var result = modelEngineeringService.RunTest(dynamicColumns, out featurePredictionTest);
                    switch (result)
                    {
                        case CONSTANTS.C: return GetSuccessWithMessageResponse(featurePredictionTest);
                        case CONSTANTS.P: return GetSuccessWithMessageResponse(featurePredictionTest);
                        case CONSTANTS.Success: return GetSuccessWithMessageResponse(featurePredictionTest);
                        case CONSTANTS.PhythonError: return GetFaultResponse(featurePredictionTest);
                    }
                    return GetSuccessWithMessageResponse(featurePredictionTest);
                }
                else
                {
                    return GetSuccessWithMessageResponse(Resource.IngrainResx.EmptyData);
                }

            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ModelEngineeringController), nameof(RunTest), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
        }

        /// <summary>
        /// Get all data for teach and test
        /// </summary>
        /// <param name="correlationId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("api/GetTeachTestData")]
        public IActionResult GetTeachTestData(string correlationId, string modelName)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ModelEngineeringController), nameof(GetTeachTestData), "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            TeachAndTestDTO teachAndTestDTO = new TeachAndTestDTO();
            try
            {
                if (!string.IsNullOrEmpty(correlationId))
                {
                    //Added new parameter to filter the saved scenario based on selected trained model
                    teachAndTestDTO = modelEngineeringService.GetFeatureForTest(correlationId, modelName);
                    return GetSuccessWithMessageResponse(teachAndTestDTO);
                }
                else
                {
                    return Accepted(Resource.IngrainResx.InputData);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ModelEngineeringController), nameof(GetTeachTestData), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
        }

        /// <summary>
        /// Get all data for teach and test
        /// </summary>
        /// <param name="correlationId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("api/GetTeachTestDataforTS")]
        public IActionResult GetTeachTestDataforTS(string correlationId, string ModelType, string TimeSeriesSteps, string modelName)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ModelEngineeringController), nameof(GetTeachTestDataforTS), "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            TeachAndTestDTOforTS teachAndTestDTO = new TeachAndTestDTOforTS();
            try
            {
                if (!string.IsNullOrEmpty(correlationId))
                {
                    //Added new parameter to filter the saved scenario based on selected trained model
                    teachAndTestDTO = modelEngineeringService.GetFeatureForTestforTS(correlationId, ModelType, TimeSeriesSteps, modelName);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(ModelEngineeringController), nameof(GetTeachTestDataforTS), "END", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                    return GetSuccessWithMessageResponse(teachAndTestDTO);
                }
                else
                {
                    return Accepted(Resource.IngrainResx.InputData);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ModelEngineeringController), nameof(GetTeachTestDataforTS), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }

        }

        [HttpGet]
        [Route("api/RemoveFeatureSelectionAttributes")]
        public IActionResult RemoveFeatureSelectionAttributes(string correlationId, [FromQuery] string[] prescriptionColumns)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ModelEngineeringController), nameof(RemoveFeatureSelectionAttributes), "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            string updateResult = string.Empty;
            try
            {
                if (prescriptionColumns != null)
                {
                    updateResult = modelEngineeringService.RemoveColumns(correlationId, prescriptionColumns);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ModelEngineeringController), nameof(RemoveFeatureSelectionAttributes), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ModelEngineeringController), nameof(RemoveFeatureSelectionAttributes), "END", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            return Ok("Success. " + updateResult);
        }

        /// <summary>
        /// Compare Test Scenarios
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="testName"></param>
        /// <returns>Test Scenarios</returns>
        [HttpGet]
        [Route("api/GetCompareTestScenarios")]
        public IActionResult GetCompareTestScenarios(string correlationId, string modelName)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ModelEngineeringController), nameof(GetCompareTestScenarios), "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            TestScenarioModelDTO data = null;
            try
            {
                if (!string.IsNullOrEmpty(correlationId) && correlationId != "undefined" && !string.IsNullOrEmpty(modelName))
                {
                    data = modelEngineeringService.GetTestScenarios(correlationId, modelName);
                }
                else
                {
                    return GetSuccessWithMessageResponse(Resource.IngrainResx.CorrelatioUIDEmpty);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ModelEngineeringController), nameof(GetCompareTestScenarios), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
            return GetSuccessWithMessageResponse(data);
        }

        /// <summary>
        /// Uploads the file to the server, invokes the python call & save data to WF_Ingested collection backend.  
        /// </summary>
        /// <param name="correlationId">The correlation identifier</param>
        /// <param name="pageInfo">The page information</param>
        /// <param name="userId">The user identifier</param>
        /// <returns>Returns the Features data based on ColumnUnique values from WF_Ingested & ME_FeatureSelection</returns>
        [HttpPost]
        [Route("api/UploadTestData")]
        public IActionResult UploadTestData(string correlationId, string pageInfo, string userId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ModelEngineeringController), nameof(UploadTestData), "Start", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                var fileCollection = HttpContext.Request.Form.Files;

                #region VALIDATIONS
                if (CommonUtility.ValidateFileUploaded(fileCollection))
                    return GetFaultResponse(Resource.IngrainResx.InValidFileName);

                if (!CommonUtility.GetValidUser(userId))
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);

                CommonUtility.ValidateInputFormData(correlationId, CONSTANTS.CorrelationId, true);
                CommonUtility.ValidateInputFormData(pageInfo, CONSTANTS.PageInfo, false);                
                #endregion


                string filePath = string.Empty;
                filePath = appSettings.Value.UploadFilePath;//ConfigurationManager.AppSettings["UploadFilePath"];
                System.IO.Directory.CreateDirectory(filePath);

                Directory.CreateDirectory(Path.Combine(filePath, appSettings.Value.SavedModels));
                filePath = System.IO.Path.Combine(filePath, appSettings.Value.AppData);
                System.IO.Directory.CreateDirectory(filePath);
               
                if (fileCollection.Count <= 0)
                    return GetSuccessWithMessageResponse(Resource.IngrainResx.FileNotExist);

                var postedFile = fileCollection[0];
                if (postedFile.Length <= 0)
                    return NoContent();

                DataProcessingController pythonCall = new DataProcessingController(_serviceProvider);
                string uploadId = Guid.NewGuid().ToString();
                string filePathNameFormat = filePath + correlationId + "_" + uploadId + "_" + postedFile.FileName;
                //using (var fileStream = new FileStream(filePathNameFormat, FileMode.Create))
                //{
                //    postedFile.CopyTo(fileStream);
                //}

                _encryptionDecryption.EncryptFile(postedFile, filePathNameFormat);
                if (appSettings.Value.IsAESKeyVault && appSettings.Value.Environment == CONSTANTS.PADEnvironment && appSettings.Value.EncryptUploadedFiles)
                    filePathNameFormat = filePathNameFormat + ".enc";
                _teachAndTestDTO = new TeachAndTestDTO();
                bool flag = true;

                IngrainRequestQueue ingrainRequest = new IngrainRequestQueue
                {
                    _id = Guid.NewGuid().ToString(),
                    CorrelationId = correlationId,
                    RequestId = Guid.NewGuid().ToString(),
                    ProcessId = null,
                    Status = null,
                    ModelName = null,
                    RequestStatus = CONSTANTS.TrackingStatus,
                    RetryCount = 0,
                    ProblemType = null,
                    Message = null,
                    UniId = uploadId,
                    Progress = null,
                    pageInfo = pageInfo,
                    ParamArgs = CONSTANTS.CurlyBraces,
                    Function = CONSTANTS.WFIngestData,
                    CreatedByUser = userId,
                    CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                    ModifiedByUser = userId,
                    ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                    LastProcessedOn = null,
                };

                WfFileUpload fileUpload = new WfFileUpload
                {
                    CorrelationId = correlationId,
                    UserId = userId,
                    PageInfo = pageInfo,
                    FilePath = filePathNameFormat
                };
                ingrainRequest.ParamArgs = fileUpload.ToJson();
                ingestedDataService.InsertRequests(ingrainRequest);
                Thread.Sleep(2000);

                while (flag)
                {
                    IngrainRequestQueue requestQueue = new IngrainRequestQueue();
                    requestQueue = ingestedDataService.GetFileRequestStatus(correlationId, pageInfo, uploadId);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(ModelEngineeringController), nameof(UploadTestData), "UploadTestData Result" + requestQueue,
                        string.IsNullOrEmpty(requestQueue.CorrelationId) ? default(Guid) : new Guid(requestQueue.CorrelationId), string.Empty, string.Empty,requestQueue.ClientID,requestQueue.DeliveryconstructId);
                    if (requestQueue != null)
                    {

                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(ModelEngineeringController), "requestQueueData", requestQueue.ToString(), string.IsNullOrEmpty(requestQueue.CorrelationId) ? default(Guid) : new Guid(requestQueue.CorrelationId), string.Empty, string.Empty, requestQueue.ClientID, requestQueue.DeliveryconstructId);

                        if (requestQueue.Status == CONSTANTS.C & requestQueue.Progress == CONSTANTS.Hundred)
                        {
                            flag = false;
                            _teachAndTestDTO = modelEngineeringService.GetIngestedData(correlationId);
                            _teachAndTestDTO.CorrelationId = correlationId;
                            _teachAndTestDTO.UploadId = uploadId;
                            _teachAndTestDTO.Message = Resource.IngrainResx.UploadFile;
                        }
                        else if (requestQueue.Status == CONSTANTS.I)
                        {
                            return GetFaultWithValidationMessageResponse(requestQueue.Message);
                        }
                        else if (requestQueue.Status == CONSTANTS.E)
                        {
                            return GetFaultResponse(requestQueue.Message);
                        }
                        else
                        {
                            flag = true;
                            Thread.Sleep(1000);
                        }
                    }
                    else
                    {
                        flag = true;
                        Thread.Sleep(2000);
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ModelEngineeringController), nameof(UploadTestData), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ModelEngineeringController), nameof(UploadTestData), "End", string.Empty, string.Empty, string.Empty, string.Empty);
            return GetSuccessWithMessageResponse(_teachAndTestDTO);
        }

        /// <summary>
        /// Save the data
        /// </summary>
        /// <param name="data">The data</param>
        /// <param name="correlationId">The correlation identifier</param>
        /// <param name="wfId">The wf identifier</param>
        /// <param name="temp">The temp</param>
        [HttpPost]
        [Route("api/SaveTestResults")]
        public IActionResult SaveTestResults([FromBody]dynamic requestBody)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ModelEngineeringController), nameof(SaveTestResults), "START", string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                TeachAndTestDTO teachAndTestDTO = new TeachAndTestDTO();
                string columnsData = Convert.ToString(requestBody);
                if (!string.IsNullOrEmpty(columnsData))
                {
                    dynamic dynamicColumns = Newtonsoft.Json.JsonConvert.DeserializeObject(columnsData);
                    if (dynamicColumns.CorrelationId != null && dynamicColumns.CorrelationId != CONSTANTS.undefined)
                    {
                        if (dynamicColumns.WFId != null && dynamicColumns.WFId != CONSTANTS.undefined)
                        {
                            var columns = JObject.Parse(columnsData);
                            string correlationId = columns[CONSTANTS.CorrelationId].ToString();
                            string wfId = columns[CONSTANTS.WFId].ToString();

                            #region VALIDATIONS                            
                            CommonUtility.ValidateInputFormData(correlationId, CONSTANTS.CorrelationId, true);
                            CommonUtility.ValidateInputFormData(wfId, CONSTANTS.WFId, true);
                            CommonUtility.ValidateInputFormData(Convert.ToString(dynamicColumns.SenarioName), CONSTANTS.SenarioName, false);
                            CommonUtility.ValidateInputFormData(Convert.ToString(dynamicColumns.scenario), CONSTANTS.Senario, false);
                            #endregion

                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ModelEngineeringController), nameof(SaveTestResults) + correlationId, "START", string.Empty, string.Empty, string.Empty, string.Empty);
                            string message = modelEngineeringService.SaveTestResults(dynamicColumns, correlationId, wfId);
                            switch (message)
                            {
                                case CONSTANTS.Success:
                                    teachAndTestDTO = modelEngineeringService.GetScenariosforTeach(correlationId);
                                    return GetSuccessWithMessageResponse(teachAndTestDTO);
                                case CONSTANTS.Duplicate:
                                    return GetFaultResponse(CONSTANTS.DuplicateScenarioName);
                                case CONSTANTS.EmptyScenario:
                                    return GetFaultResponse(CONSTANTS.PassScenarioName);
                            }
                            teachAndTestDTO = modelEngineeringService.GetScenariosforTeach(correlationId);
                            return GetSuccessWithMessageResponse(teachAndTestDTO);
                        }

                        else
                        {
                            return GetSuccessWithMessageResponse(Resource.IngrainResx.WFIDEmpty);
                        }
                    }
                    else
                    {
                        return GetSuccessWithMessageResponse(Resource.IngrainResx.CorrelatioUIDEmpty);
                    }
                }
                else
                {
                    return GetFaultResponse(Resource.IngrainResx.EmptyReqBody);
                }

            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ModelEngineeringController), nameof(SaveTestResults), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
        }

        /// <summary>
        /// Prescriptive Analytics
        /// </summary>
        /// <param name="requestBody">Request Body</param>
        /// <returns>Response</returns>
        [HttpPost]
        [Route("api/PrescriptiveAnalytics")]
        public IActionResult PrescriptiveAnalytics([FromBody]dynamic requestBody)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ModelEngineeringController), nameof(PrescriptiveAnalytics), CONSTANTS.START, string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                string columnsData = Convert.ToString(requestBody);
                PrescriptiveAnalyticsResult prescriptiveAnalytics = null;
                if (!string.IsNullOrEmpty(columnsData))
                {
                    dynamic dynamicColumns = Newtonsoft.Json.JsonConvert.DeserializeObject(columnsData);
                    if (dynamicColumns.CorrelationId != null && dynamicColumns.CorrelationId != CONSTANTS.undefined)
                    {
                        if (dynamicColumns.WFId != null && dynamicColumns.WFId != CONSTANTS.undefined)
                        {
                            #region VALIDATIONS
                            if (!CommonUtility.GetValidUser(Convert.ToString(dynamicColumns[CONSTANTS.CreatedByUser])))
                                return GetFaultResponse(Resource.IngrainResx.InValidUser);
                            #endregion

                            string result = modelEngineeringService.PrescriptiveAnalytics(dynamicColumns, out prescriptiveAnalytics);
                            switch (result)
                            {
                                case CONSTANTS.Success: return Ok(prescriptiveAnalytics);
                                case CONSTANTS.C:
                                    return Ok(prescriptiveAnalytics);
                                case CONSTANTS.P:
                                    return Ok(prescriptiveAnalytics);
                                case CONSTANTS.PhythonInfo:
                                    return GetFaultResponse(prescriptiveAnalytics.Message);
                                case CONSTANTS.PhythonError:
                                    return GetFaultResponse(prescriptiveAnalytics);
                            }
                        }
                        else
                        {
                            return GetSuccessWithMessageResponse(Resource.IngrainResx.WFIDEmpty);
                        }
                    }
                    else
                    {
                        return GetSuccessWithMessageResponse(Resource.IngrainResx.CorrelatioUIDEmpty);
                    }
                }
                else
                {
                    return GetFaultResponse(Resource.IngrainResx.EmptyReqBody);
                }
                return Ok(prescriptiveAnalytics);

            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ModelEngineeringController), nameof(PrescriptiveAnalytics), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }


        }

        /// <summary>
        /// Prescriptive Analytics
        /// </summary>
        /// <param name="requestBody">Request Body</param>
        /// <returns>Response</returns>
        [HttpDelete]
        [Route("api/DeletePrescriptiveAnalytics")]
        public IActionResult DeletePrescriptiveAnalytics(string correlationId, string wFId)
        {
            var result = modelEngineeringService.DeletePrescriptiveAnalytics(correlationId, wFId);
            if (result)
            {
                return Ok(CONSTANTS.Success);
            }
            else
            {
                return Ok(CONSTANTS.Failed);
            }

        }

        [HttpGet]
        [Route("api/RunNotePadProcess")]
        public IActionResult RunNotePadProcess()
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ModelEngineeringController), nameof(RunNotePadProcess), CONSTANTS.START, string.Empty, string.Empty, string.Empty, string.Empty);
            Int32 procId = 0;
            bool started = true;
            try
            {               
                var p = new Process();
                p.StartInfo.FileName = "notepad.exe";
                started = p.Start();                
                procId = p.Id;
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(ModelEngineeringController), "Notepad started", CONSTANTS.End, string.Empty, string.Empty, string.Empty, string.Empty);
            }
            catch (InvalidOperationException ex)
            {
                started = false;
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ModelEngineeringController), nameof(RunNotePadProcess), ex.StackTrace + "--" + ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
            catch (Exception ex)
            {
                started = false;
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ModelEngineeringController), nameof(RunNotePadProcess), ex.StackTrace + "--" + ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ModelEngineeringController), nameof(RunNotePadProcess), CONSTANTS.End, string.Empty, string.Empty, string.Empty, string.Empty);
            return Ok(procId);
        }

        [HttpGet]
        [Route("api/StopProcessById")]
        public IActionResult StopProcessById(int processId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ModelEngineeringController), nameof(StopProcessById), CONSTANTS.START + "-PROCESSID-" + processId, string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                Process processes = Process.GetProcessById(processId);
                processes.Kill();
            }
            catch (ArgumentException ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ModelEngineeringController), nameof(StopProcessById), ex.StackTrace + "--" + ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
            catch (InvalidOperationException ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ModelEngineeringController), nameof(StopProcessById), ex.StackTrace + "--" + ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ModelEngineeringController), nameof(StopProcessById), ex.StackTrace + "--" + ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ModelEngineeringController), nameof(StopProcessById), CONSTANTS.End, string.Empty, string.Empty, string.Empty, string.Empty);
            return Ok("Success");
        }
        #endregion
    }
}