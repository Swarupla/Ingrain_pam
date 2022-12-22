#region Copyright (c) 2019 Accenture . All rights reserved.

// <copyright company="Accenture">
// Copyright (c) 2019 Accenture.  All rights reserved.
// Reproduction or transmission in whole or in part, in any form or by any means, 
// electronic, mechanical or otherwise, is prohibited without the prior written 
// consent of the copyright owner.
// </copyright>

#endregion

#region DeployModelController Information
/********************************************************************************************************\
Module Name     :   DeployModelController
Project         :   Accenture.MyWizard.SelfServiceAI.DeployModelController
Organisation    :   Accenture Technologies Ltd.
Created By      :   Chandrashekhar Bali
Created Date    :   30-Jan-2019
Revision History :
Revision No             Initial             Modified Date           Description
1.0                     An                  25-APR-2019             
\********************************************************************************************************/
#endregion
#region Namespace
using Accenture.MyWizard.Ingrain.BusinessDomain.Common;
using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
using Accenture.MyWizard.Ingrain.DataModels.Models;
using Accenture.MyWizard.Ingrain.WebService;
using Accenture.MyWizard.Shared.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
#endregion

namespace Accenture.MyWizard.Ingrain
{

    public class DeployModelController : MyWizardControllerBase
    {
        #region Members        
        private RecommedAITrainedModel _recommendedAIViewModel = null;

        private PredictionResultDTO _predictionresult = null;

        public static IDeployedModelService _iDeployedModelService { set; get; }
        public static IModelEngineering _iModelEngineering { set; get; }

        public static IIngestedData _iIngestedData { set; get; }

        private readonly IOptions<IngrainAppSettings> _appSettings;

        private IEncryptionDecryption _encryptionDecryption;       
        #endregion

        #region Constructors
        public DeployModelController(IServiceProvider serviceProvider, IOptions<IngrainAppSettings> settings)
        {
            _iDeployedModelService = serviceProvider.GetService<IDeployedModelService>();
            _iModelEngineering = serviceProvider.GetService<IModelEngineering>();

            _iIngestedData = serviceProvider.GetService<IIngestedData>();
            _appSettings = settings;
            _encryptionDecryption = serviceProvider.GetService<IEncryptionDecryption>();

        }
        #endregion


        /// <summary>
        /// Get the AvailableColumns and user Target and Input Columns
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="IsTemplate"></param>
        /// <returns>All the Columns Including Target or Input Columns</returns>
        [HttpGet]
        [Route("api/GetPublishModel")]
        public IActionResult GetPublishModel(string correlationId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DeployModelController), nameof(GetPublishModel), "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                _recommendedAIViewModel = new RecommedAITrainedModel();
                if (!string.IsNullOrEmpty(correlationId))
                {
                    _recommendedAIViewModel = _iDeployedModelService.GetPublishedModels(correlationId);
                }
                else
                {
                    return GetFaultResponse(Resource.IngrainResx.CorrelatioUIDEmpty);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(DeployModelController), nameof(GetPublishModel), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DeployModelController), nameof(GetPublishModel), "END",string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId) , string.Empty, string.Empty, string.Empty, string.Empty);
            return GetSuccessResponse(_recommendedAIViewModel);
        }

        /// <summary>
        /// ColumnsPostAPI
        /// </summary>
        /// <returns>All the columns</returns>
        [HttpPost]
        [Route("api/PostPublishModel")]
        public IActionResult PostPublishModel([FromBody]dynamic requestBody)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DeployModelController), nameof(PostPublishModel), "START", string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                string columnsData = Convert.ToString(requestBody);
                var dynamicColumns = JsonConvert.DeserializeObject(requestBody);
                var columns = JObject.Parse(columnsData);

                CommonUtility.ValidateInputFormData(Convert.ToString(columns["correlationId"]), "correlationId", true);
                CommonUtility.ValidateInputFormData(Convert.ToString(columns["FeatureImportance"]), "FeatureImportance", false);
                CommonUtility.ValidateInputFormData(Convert.ToString(columns["Train_Test_Split"]), "Train_Test_Split", false);
                CommonUtility.ValidateInputFormData(Convert.ToString(columns["KFoldValidation"]), "KFoldValidation", false);
                CommonUtility.ValidateInputFormData(Convert.ToString(columns["StratifiedSampling"]), "StratifiedSampling", false);

                string correlationId = columns["correlationId"].ToString();
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(DeployModelController), nameof(PostPublishModel) + correlationId, "START", string.Empty, string.Empty, string.Empty, string.Empty);
                _iModelEngineering.UpdateFeatures(dynamicColumns, correlationId);

            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(DeployModelController), nameof(PostPublishModel), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DeployModelController), nameof(PostPublishModel), "END", string.Empty, string.Empty, string.Empty, string.Empty);
            return GetSuccessResponse("Success");
        }

        [HttpPost]
        [Route("api/PublishModels")]
        public IActionResult PublishModels(string CorrelationId)
        {
            GetAppService();
            string predictionResult = string.Empty;
            string result = string.Empty;
            string URI = string.Empty;
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DeployModelController), nameof(PublishModels), "START",string.IsNullOrEmpty(CorrelationId) ? default(Guid) : new Guid(CorrelationId), string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                IsPredictionEnabled(_appSettings.Value.EnablePrediction);
                if (!string.IsNullOrEmpty(CorrelationId))
                {
                    string Data = HttpContext.Request.Form["Data"];
                    CommonUtility.ValidateInputFormData(CorrelationId, "CorrelationId", true);
                    CommonUtility.ValidateInputFormData(Data, "Data", false);
                    predictionResult = _iDeployedModelService.PredictionModel(CorrelationId, Data);
                }
                else
                {
                    return Accepted(Resource.IngrainResx.CorrelatioUIDEmpty);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(DeployModelController), nameof(PublishModels), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DeployModelController), nameof(PublishModels), "END", string.IsNullOrEmpty(CorrelationId) ? default(Guid) : new Guid(CorrelationId), string.Empty, string.Empty, string.Empty, string.Empty);
            return GetSuccessResponse(predictionResult);
        }

        [HttpPost]
        [Route("api/PublishModelsTest")]
        public IActionResult PublishModelsTest(string CorrelationId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DeployModelController), nameof(PublishModelsTest), "START", string.IsNullOrEmpty(CorrelationId) ? default(Guid) : new Guid(CorrelationId), string.Empty, string.Empty, string.Empty, string.Empty);
            _predictionresult = new PredictionResultDTO();
            string response = string.Empty;
            try
            {
                IsPredictionEnabled(_appSettings.Value.EnablePrediction);
                if (!string.IsNullOrEmpty(CorrelationId))
                {
                    string data = HttpContext.Request.Form["Data"];
                    #region VALIDATIONS
                    CommonUtility.ValidateInputFormData(CorrelationId, CONSTANTS.CorrelationId, true);
                    CommonUtility.ValidateInputFormData(data, "Data", false);
                    #endregion
                    _predictionresult = _iDeployedModelService.PredictionModelPerformance(CorrelationId, data);
                }
                else
                {
                    return Accepted(Resource.IngrainResx.CorrelatioUIDEmpty);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(DeployModelController), nameof(PublishModelsTest), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
            response = JsonConvert.SerializeObject(_predictionresult);
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DeployModelController), nameof(PublishModelsTest), CONSTANTS.InstaMLResponse + response.Replace(CONSTANTS.slash, string.Empty), string.IsNullOrEmpty(CorrelationId) ? default(Guid) : new Guid(CorrelationId), string.Empty, string.Empty, string.Empty, string.Empty);
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DeployModelController), nameof(PublishModelsTest), "END", string.IsNullOrEmpty(CorrelationId) ? default(Guid) : new Guid(CorrelationId), string.Empty, string.Empty, string.Empty, string.Empty);
            return GetSuccessResponse(_predictionresult);
        }        

        [HttpGet]
        [Route("api/GetPredictionResult")]
        public IActionResult GetPredictionResult(string CorrelationId, string uniqueId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DeployModelController), nameof(GetPredictionResult), "START UniqueId-" + uniqueId, string.IsNullOrEmpty(CorrelationId) ? default(Guid) : new Guid(CorrelationId), string.Empty, string.Empty, string.Empty, string.Empty);
            Thread.Sleep(1000);
            bool isPrediction = true;
            _predictionresult = new PredictionResultDTO();
            string response = string.Empty;
            try
            {
                PredictionDTO predictionDTO = new PredictionDTO
                {
                    UniqueId = uniqueId,
                    CorrelationId = CorrelationId,
                };
                if (!string.IsNullOrWhiteSpace(CorrelationId) && !string.IsNullOrWhiteSpace(uniqueId))
                {
                    while (isPrediction)
                    {
                        var predictionData = _iDeployedModelService.GetPrediction(predictionDTO);
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(DeployModelController), nameof(GetPredictionResult), "API Result - " + predictionData.Status, string.IsNullOrEmpty(CorrelationId) ? default(Guid) : new Guid(CorrelationId), string.Empty, string.Empty, string.Empty, string.Empty);
                        _predictionresult.CorrelationId = predictionData.CorrelationId;
                        _predictionresult.UniqueId = predictionData.UniqueId;
                        _predictionresult.Status = predictionData.Status;
                        _predictionresult.Progress = predictionData.Progress;
                        if (predictionData.Status == "I" || predictionData.Status == "P")
                        {
                            isPrediction = false;
                            _predictionresult.Status = "P";
                            _predictionresult.Message = "Request is Pending...";
                        }
                        else if (predictionData.Status == "C")
                        {
                            _predictionresult.PredictedData = predictionData.PredictedData;
                            ValidRecordsDetailsModel validRecordsDetailModel = CommonUtility.GetModelDataPointdDetails(CorrelationId, _appSettings);
                            if (validRecordsDetailModel != null)
                            {
                                if (validRecordsDetailModel.ValidRecordsDetails != null)
                                {
                                    if (validRecordsDetailModel.ValidRecordsDetails.Records[0] >= 4 && validRecordsDetailModel.ValidRecordsDetails.Records[0] < 20)
                                    {
                                        _predictionresult.DataPointsWarning = CONSTANTS.DataPointsMinimum;
                                        _predictionresult.DataPointsCount = validRecordsDetailModel.ValidRecordsDetails.Records[0];
                                    }
                                }
                            }
                            isPrediction = false;
                        }
                        else if (predictionData.Status == "E")
                        {
                            isPrediction = false;
                            response = JsonConvert.SerializeObject(_predictionresult);
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DeployModelController), nameof(GetPredictionResult), CONSTANTS.InstaMLResponse + response.Replace(CONSTANTS.slash, string.Empty), string.IsNullOrEmpty(CorrelationId) ? default(Guid) : new Guid(CorrelationId), string.Empty, string.Empty, string.Empty, string.Empty);
                            _predictionresult.ErrorMessage = "Python: Error While Prediction : " + predictionData.ErrorMessage + " Status:" + predictionData.Status;
                            return GetFaultResponse(_predictionresult);
                        }
                        else
                        {
                            Thread.Sleep(2000);
                            isPrediction = true;
                        }
                    }
                }
                else
                {
                    return Accepted(Resource.IngrainResx.CorrelatioUIDEmpty);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(DeployModelController), nameof(GetPredictionResult), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(_predictionresult);
            }
            response = JsonConvert.SerializeObject(_predictionresult);
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DeployModelController), nameof(GetPredictionResult), CONSTANTS.InstaMLResponse + response.Replace(CONSTANTS.slash, string.Empty), string.IsNullOrEmpty(CorrelationId) ? default(Guid) : new Guid(CorrelationId), string.Empty, string.Empty, string.Empty, string.Empty);
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DeployModelController), nameof(GetPredictionResult), "END", string.IsNullOrEmpty(CorrelationId) ? default(Guid) : new Guid(CorrelationId), string.Empty, string.Empty, string.Empty, string.Empty);
            return GetSuccessResponse(_predictionresult);
        }


        [HttpPost]
        [Route("api/ForecastModel")]
        public IActionResult ForecastModel(string CorrelationId, string Frequency)
        {
            GetAppService();
            ForeCastModel predictionResult = new ForeCastModel();
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DeployModelController), nameof(ForecastModel), "START", string.IsNullOrEmpty(CorrelationId) ? default(Guid) : new Guid(CorrelationId), string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                IsPredictionEnabled(_appSettings.Value.EnablePrediction);
                if (!string.IsNullOrEmpty(CorrelationId))
                {
                    string Data = HttpContext.Request.Form["Data"];
                    if (string.IsNullOrEmpty(Data))
                    {
                        return Accepted(Resource.IngrainResx.InputData);
                    }
                    #region VALIDATIONS
                    CommonUtility.ValidateInputFormData(CorrelationId, "CorrelationId", true);
                    CommonUtility.ValidateInputFormData(Data, "Data", false);
                    #endregion
                    predictionResult = _iDeployedModelService.ForeCastModel(CorrelationId, Frequency, Data);
                    if(predictionResult.Status == CONSTANTS.C)
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(DeployModelController), nameof(ForecastModel), "END", string.IsNullOrEmpty(CorrelationId) ? default(Guid) : new Guid(CorrelationId), string.Empty, string.Empty, string.Empty, string.Empty);
                        return GetSuccessResponse(predictionResult.PredictionResult);
                    }
                    if(predictionResult.Status == CONSTANTS.E)
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(DeployModelController), nameof(ForecastModel), "END", string.IsNullOrEmpty(CorrelationId) ? default(Guid) : new Guid(CorrelationId), string.Empty, string.Empty, string.Empty, string.Empty);
                        return GetFaultResponse(predictionResult.Message);
                    }                    
                }
                else
                {
                    return Accepted(Resource.IngrainResx.CorrelatioUIDEmpty);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(DeployModelController), nameof(ForecastModel), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
            return GetSuccessResponse(predictionResult.PredictionResult);
        }
        [HttpPost]
        [Route("api/ForecastModelTest")]
        public IActionResult ForecastModeltest(string CorrelationId, string Frequency)
        {
            string URI = string.Empty;
            string predictionResult = string.Empty;
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DeployModelController), nameof(ForecastModel), "START", string.IsNullOrEmpty(CorrelationId) ? default(Guid) : new Guid(CorrelationId), string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                IsPredictionEnabled(_appSettings.Value.EnablePrediction);
                if (!string.IsNullOrEmpty(CorrelationId))
                {
                    string Data = HttpContext.Request.Form["Data"];
                    if (string.IsNullOrEmpty(Data))
                    {
                        return Accepted(Resource.IngrainResx.InputData);
                    }
                    #region VALIDATIONS
                    CommonUtility.ValidateInputFormData(CorrelationId, "CorrelationId", true);
                    CommonUtility.ValidateInputFormData(Data, "Data", false);
                    #endregion
                    PredictionDTO predictionDTO = new PredictionDTO
                    {
                        _id = Guid.NewGuid().ToString(),
                        UniqueId = Guid.NewGuid().ToString(),
                        ActualData = Data,
                        CorrelationId = CorrelationId,
                        PredictedData = null,
                        Status = "I",
                        ErrorMessage = null,
                        CreatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        ModifiedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        CreatedByUser = "System",
                        ModifiedByUser = "System"
                    };
                    _iDeployedModelService.SavePrediction(predictionDTO);

                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(DeployModelController), nameof(PostPublishModel), "GetPythonResult Start--" + URI, string.IsNullOrEmpty(CorrelationId) ? default(Guid) : new Guid(CorrelationId), string.Empty, string.Empty, string.Empty, string.Empty);
                    //var result = _iDeployedModelService.GetPythonResult(URI, "POST", CorrelationId, Data, Frequency);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(DeployModelController), nameof(PostPublishModel), " GetPythonResult Content END", string.IsNullOrEmpty(CorrelationId) ? default(Guid) : new Guid(CorrelationId), string.Empty, string.Empty, string.Empty, string.Empty);

                    Thread.Sleep(1000);
                    bool isPrediction = true;
                    while (isPrediction)
                    {
                        var predictionData = _iDeployedModelService.GetPrediction(predictionDTO);
                        if (predictionData.Status == "C")
                        {
                            predictionResult = predictionData.PredictedData;
                            isPrediction = false;
                        }
                        else if (predictionData.Status == "E")
                        {
                            isPrediction = false;
                            string Error = "Error While Prediction : " + predictionData.ErrorMessage;
                            return GetFaultResponse(Error);
                        }
                        else
                        {
                            Thread.Sleep(500);
                            isPrediction = true;
                        }
                    }
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(DeployModelController), nameof(ForecastModel), "END", string.IsNullOrEmpty(CorrelationId) ? default(Guid) : new Guid(CorrelationId), string.Empty, string.Empty, string.Empty, string.Empty);
                    return GetSuccessResponse(predictionResult);
                }
                else
                {
                    return Accepted(Resource.IngrainResx.CorrelatioUIDEmpty);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(DeployModelController), nameof(ForecastModel), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }

        }

        [HttpPost]
        [Route("api/PostDeployModel")]
        public IActionResult PostDeployModel([FromBody]dynamic requestBody)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DeployModelController), nameof(PostDeployModel), "START", string.Empty, string.Empty, string.Empty, string.Empty);
            DeployModelViewModel modelsDto = new DeployModelViewModel();
            try
            {
                string columnsData = Convert.ToString(requestBody);
                dynamic dynamicData = Newtonsoft.Json.JsonConvert.DeserializeObject(columnsData);
                JObject inputData = JObject.Parse(columnsData);
                if (!string.IsNullOrEmpty(inputData["ModelType"].ToString()) && !string.IsNullOrEmpty(inputData["ModelVersion"].ToString()) && !string.IsNullOrEmpty(inputData["userid"].ToString()))
                {
                    if (!CommonUtility.GetValidUser(Convert.ToString(inputData["userid"])))
                    {
                        return GetFaultResponse(Resource.IngrainResx.InValidUser);
                    }

                    CommonUtility.ValidateInputFormData(Convert.ToString(dynamicData.LinkedApps), "LinkedApps", false);
                    CommonUtility.ValidateInputFormData(Convert.ToString(dynamicData.correlationId), "correlationId", true);
                    CommonUtility.ValidateInputFormData(Convert.ToString(dynamicData.AppId), "AppId", true);
                    CommonUtility.ValidateInputFormData(Convert.ToString(inputData["ModelName"]), "ModelName", false);
                    CommonUtility.ValidateInputFormData(Convert.ToString(inputData["ModelVersion"]), "ModelVersion", false);
                    CommonUtility.ValidateInputFormData(Convert.ToString(inputData["Category"]), "Category", false);
                    CommonUtility.ValidateInputFormData(Convert.ToString(inputData["ModelType"]), "ModelType", false); 
                    //PRIVATE MODEL 
                    if (Convert.ToBoolean(dynamicData.IsPrivate))
                    {
                        modelsDto = _iDeployedModelService.DeployModel(dynamicData);
                    }
                    //MODEL TEMPLATE
                    else if (Convert.ToBoolean(dynamicData.IsModelTemplate))
                    {
                        if (inputData["Category"] != null)
                        {
                            modelsDto = _iDeployedModelService.DeployModel(dynamicData);
                        }
                        else
                        {
                            return GetSuccessResponse(Resource.IngrainResx.InputData);
                        }
                    }
                    //PUBLIC MODEL             
                    else
                    {
                        modelsDto = _iDeployedModelService.DeployModel(dynamicData);
                    }                    
                }
                else
                {
                    return GetSuccessResponse(Resource.IngrainResx.InputData);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(DeployModelController), nameof(PostDeployModel), ex.Message + "StackTrace=" + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DeployModelController), nameof(PostDeployModel), "END", string.Empty, string.Empty, string.Empty, string.Empty);
            return GetSuccessResponse(modelsDto);
        }

        /// <summary>
        /// GetDeployed Model
        /// </summary>
        /// <param name="correlationId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("api/GetDeployedModel")]
        public IActionResult GetDeployedModel(string correlationId)
        {
            DeployModelViewModel deployModel = new DeployModelViewModel();
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DeployModelController), nameof(GetDeployedModel), "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                deployModel = _iDeployedModelService.GetDeployModel(correlationId);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(DeployModelController), nameof(GetDeployedModel), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DeployModelController), nameof(GetDeployedModel), "END", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            return GetSuccessResponse(deployModel);
        }


        /// <summary>
        /// GetDeployed Model
        /// </summary>
        /// <param name="correlationId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("api/GetVisualization")]
        public IActionResult GetVisualization(string correlationId, string modelName, bool isPrediction)
        {
            var data = new object();
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DeployModelController), nameof(GetDeployedModel), "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                data = _iDeployedModelService.GetVisualizationData(correlationId, modelName, isPrediction);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(DeployModelController), nameof(GetDeployedModel), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DeployModelController), nameof(GetDeployedModel), "END", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            return GetSuccessResponse(data);
        }

        [HttpGet]
        [Route("api/RetrieveArchiveModel")]
        public IActionResult RetrieveArchiveModel(string correlationId)
        {            
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DeployModelController), nameof(RetrieveArchiveModel), "START", string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                _iDeployedModelService.RetrieveModel(correlationId);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(DeployModelController), nameof(RetrieveArchiveModel), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DeployModelController), nameof(RetrieveArchiveModel), "END", string.Empty, string.Empty, string.Empty, string.Empty);
            return Ok();
        }

        [HttpGet]
        [Route("api/GetArchivedRecords")]
        public IActionResult GetArchivedRecords(string userId, string DeliveryConstructUID, string ClientUId)
        {
            List<DeployModelsDto> archivedModels;
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DeployModelController), nameof(GetArchivedRecords), "START", string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                if (!string.IsNullOrEmpty(userId))
                {
                    if (!CommonUtility.GetValidUser(userId))
                    {
                        return GetFaultResponse(Resource.IngrainResx.InValidUser);
                    }
                    archivedModels = _iDeployedModelService.GetArchivedRecordList(userId, DeliveryConstructUID, ClientUId);
                }
                else
                {
                    return GetSuccessWithMessageResponse(Resource.IngrainResx.InputData);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(DeployModelController), nameof(GetArchivedRecords), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DeployModelController), nameof(GetArchivedRecords), "END", string.Empty, string.Empty, string.Empty, string.Empty);
            return Ok(archivedModels);
        }

    }
}