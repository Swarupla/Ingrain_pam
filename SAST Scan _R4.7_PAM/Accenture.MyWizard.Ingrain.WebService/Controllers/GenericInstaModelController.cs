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
Module Name     :   GenericInstaModelController
Project         :   Accenture.MyWizard.SelfServiceAI.GenericInstaModelController
Organisation    :   Accenture Technologies Ltd.
Created By      :   Thanyaasri Manickam
Created Date    :   03-FEB-2020
Revision History :
Revision No             Initial             Modified Date           Description
1.0                     An                  03-FEB-2020             
\********************************************************************************************************/
#endregion
using Accenture.MyWizard.Ingrain.BusinessDomain.Common;
using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
using Accenture.MyWizard.Ingrain.DataModels.AICore;
using Accenture.MyWizard.Ingrain.DataModels.Models;
using Accenture.MyWizard.Shared.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;

namespace Accenture.MyWizard.Ingrain.WebService.Controllers
{

    public class GenericInstaModelController : MyWizardControllerBase
    {
        #region Members
        //GenericInstaModelResponse _instaModel = null;
        private IGenericSelfservice _GenericSelfservice { get; set; }
        private IngrainAppSettings appSettings { get; set; }
        //InstaRegression regressionResponse = new InstaRegression();
        private GenericApiData _genericApiData;
        private GenericDataResponse _trainingResponse;
        private TemplateModelPrediction _modelPrediction;
        private PredictionResultDTO _predictionresult;
        private CallBackErrorLog auditTrailLog;
        #endregion

        #region Constructor       
        public GenericInstaModelController(IServiceProvider serviceProvider, IOptions<IngrainAppSettings> settings)
        {
            //  _instaModel = new GenericInstaModelResponse();
            _GenericSelfservice = serviceProvider.GetService<IGenericSelfservice>();
            appSettings = settings.Value;
            _genericApiData = new GenericApiData();
            _trainingResponse = new GenericDataResponse();
            _modelPrediction = new TemplateModelPrediction();
            _predictionresult = new PredictionResultDTO();
            auditTrailLog = new CallBackErrorLog();
        }
        #endregion

        #region Methods

        /// <summary>   
        /// Using Public template data train and deploy as private model
        /// </summary>
        /// <param name="ClientID"></param>
        /// <param name="DCID"></param>
        /// <returns>Training Response</returns>
        [HttpPost]
        [Route("api/PrivateModelTraning")]
        public IActionResult PrivateModelTraning(string ClientID, string DCID, string UserId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericInstaModelController), nameof(PrivateModelTraning), "START",string.Empty,string.Empty, ClientID, DCID);
            try
            {
                IsTrainingEnabled(appSettings.EnableTraining);
                if (!(string.IsNullOrEmpty(ClientID)) && !(string.IsNullOrEmpty(DCID)) && !(string.IsNullOrEmpty(UserId)))
                {
                    #region VALIDATIONS
                    if (!CommonUtility.GetValidUser(UserId))
                    {
                        return GetFaultResponse(Resource.IngrainResx.InValidUser);
                    }
                    CommonUtility.ValidateInputFormData(ClientID, "ClientID", true);
                    CommonUtility.ValidateInputFormData(DCID, "DCID", true);
                    #endregion
                    _trainingResponse = _GenericSelfservice.PublicTemplateModelTraning(ClientID, DCID, UserId);
                }
                else
                {
                    _trainingResponse.Message = "Client Id, DCID and User id Values is Null. Please try again";
                    _trainingResponse.Status = "I";
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(GenericInstaModelController), nameof(PrivateModelTraning), ex.Message + $"   StackTrace = {ex.StackTrace}", ex, string.Empty, string.Empty, ClientID, DCID);
                return GetFaultResponse(ex.Message);
            }

            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericInstaModelController), nameof(PrivateModelTraning), "END", string.Empty, string.Empty, ClientID, DCID);
            return Ok(_trainingResponse);


        }
        /// <summary>
        ///  Gets the Prediction Data for the AppicationId and UseCaseId
        /// </summary>
        /// <param name="requestBody"></param>
        /// <returns>Prediction Response</returns>
        [HttpPost]
        [Route("api/IngrainGenericInterface")]
        public IActionResult IngrainGenericAPI([FromBody] dynamic requestBody)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericInstaModelController), nameof(IngrainGenericAPI), "IngrainGenericAPI Started", string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                IsTrainingEnabled(appSettings.EnableTraining);
                string AppData = Convert.ToString(requestBody);
                if (!string.IsNullOrEmpty(AppData))
                {
                    _genericApiData = Newtonsoft.Json.JsonConvert.DeserializeObject<GenericApiData>(AppData);
                    //_genericApiData.ApplicationName = data.ApplicationName.ToString();
                    //_genericApiData.ApplicationID = data.ApplicationID.ToString();
                    //_genericApiData.ClientID = data.ClientID.ToString();
                    //_genericApiData.DCID = data.DCID.ToString();
                    //_genericApiData.UseCaseName = data.UseCaseName.ToString();
                    //_genericApiData.UseCaseID = data.UseCaseID.ToString();
                    //_genericApiData.ProcessName = data.ProcessName.ToString();
                    //_genericApiData.DataSource = data.DataSource.ToString();
                    //_genericApiData.DataSourceDetails.URL = data.DataSourceDetails.URL.ToString();
                    //_genericApiData.DataSourceDetails.BodyParams = data.DataSourceDetails.BodyParams;
                    CommonUtility.ValidateInputFormData(_genericApiData.ApplicationName, "ApplicationName", false);
                    CommonUtility.ValidateInputFormData(_genericApiData.ClientID, "ClientID", true);
                    CommonUtility.ValidateInputFormData(_genericApiData.DCID, "DCID", true);
                    CommonUtility.ValidateInputFormData(_genericApiData.ApplicationID, "ApplicationID", true);
                    CommonUtility.ValidateInputFormData(_genericApiData.UseCaseID, "UseCaseID", true);
                    CommonUtility.ValidateInputFormData(_genericApiData.UseCaseName, "UseCaseName", false);
                    CommonUtility.ValidateInputFormData(_genericApiData.ProcessName, "ProcessName", false);
                    CommonUtility.ValidateInputFormData(_genericApiData.Frequency, "Frequency", false);
                    CommonUtility.ValidateInputFormData(_genericApiData.DataSource, "DataSource", false);
                    CommonUtility.ValidateInputFormData(_genericApiData.DataSourceDetails?.URL, "DataSourceDetailsURL", false);
                    CommonUtility.ValidateInputFormData(Convert.ToString(_genericApiData.DataSourceDetails?.BodyParams), "DataSourceDetailsBodyParams", false);


                    _modelPrediction = _GenericSelfservice.GetPublicTemplatesModelsPredictions(_genericApiData);
                }
                else
                {
                    return GetFaultResponse(CONSTANTS.InputDataEmpty);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(GenericInstaModelController), nameof(IngrainGenericAPI), ex.Message + $"   StackTrace = {ex.StackTrace}", ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericInstaModelController), nameof(IngrainGenericAPI), "IngrainGenericAPI Ended", string.Empty, string.Empty, string.Empty, string.Empty);
            return Ok(_modelPrediction);
        }

        /// <summary>
        ///  Gets the Prediction for the phoenix data based on usecaseId
        /// </summary>
        /// <param name="requestBody"></param>
        /// <returns>Prediction Request</returns>
        [HttpPost]
        [Route("api/PhoenixPredictionRequest")]
        public IActionResult PhoenixPredictionRequest([FromBody] dynamic requestBody)
        {
            PhoenixPredictionStatus phoenixPredictionStatus = new PhoenixPredictionStatus();
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericInstaModelController), nameof(PhoenixPredictionRequest), "PhoenixPredictionRequest Started", string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                IsPredictionEnabled(appSettings.EnablePrediction);
                string AppData = Convert.ToString(requestBody);
                if (!string.IsNullOrEmpty(AppData))
                {
                    _genericApiData = Newtonsoft.Json.JsonConvert.DeserializeObject<GenericApiData>(AppData);
                    CommonUtility.ValidateInputFormData(_genericApiData.ApplicationName, "ApplicationName", false);
                    CommonUtility.ValidateInputFormData(_genericApiData.ClientID, "ClientID", true);
                    CommonUtility.ValidateInputFormData(_genericApiData.DCID, "DCID", true);
                    CommonUtility.ValidateInputFormData(_genericApiData.ApplicationID, "ApplicationID", true);
                    CommonUtility.ValidateInputFormData(_genericApiData.UseCaseID, "UseCaseID", true);
                    CommonUtility.ValidateInputFormData(_genericApiData.UseCaseName, "UseCaseName", false);
                    CommonUtility.ValidateInputFormData(_genericApiData.ProcessName, "ProcessName", false);
                    CommonUtility.ValidateInputFormData(_genericApiData.Frequency, "Frequency", false);
                    CommonUtility.ValidateInputFormData(_genericApiData.DataSource, "DataSource", false);
                    CommonUtility.ValidateInputFormData(_genericApiData.DataSourceDetails?.URL, "DataSourceDetailsURL", false);
                    CommonUtility.ValidateInputFormData(Convert.ToString(_genericApiData.DataSourceDetails?.BodyParams), "DataSourceDetailsBodyParams", false);

                    phoenixPredictionStatus = _GenericSelfservice.PhoenixPredictionRequest(_genericApiData);
                }
                else
                {
                    return GetFaultResponse(CONSTANTS.InputDataEmpty);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(GenericInstaModelController), nameof(IngrainGenericAPI), ex.Message + $"   StackTrace = {ex.StackTrace}", ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericInstaModelController), nameof(IngrainGenericAPI), "IngrainGenericAPI Ended", string.Empty, string.Empty, string.Empty, string.Empty);
            return Ok(phoenixPredictionStatus);
        }

        /// <summary>
        ///  Gets the Prediction for the phoenix data based on usecaseId
        /// </summary>
        /// <param name="requestBody"></param>
        /// <returns>Prediction Response</returns>
        [HttpPost]
        [Route("api/PhoenixPredictionResponse")]
        public IActionResult PhoenixPredictionResponse([FromBody] PhoenixPredictionsInput phoenixPredictionsInput)
        {
            PhoenixPredictionsOutput phoenixPredictionsOutput = new PhoenixPredictionsOutput();
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericInstaModelController), nameof(PhoenixPredictionResponse), "PhoenixPredictionResponse Started", string.IsNullOrEmpty(phoenixPredictionsInput.CorrelationId) ? default(Guid) : new Guid(phoenixPredictionsInput.CorrelationId), string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                if (phoenixPredictionsInput != null)
                {
                    CommonUtility.ValidateInputFormData(phoenixPredictionsInput.CorrelationId, "CorrelationId", true);
                    CommonUtility.ValidateInputFormData(phoenixPredictionsInput.UniqueId, "UniqueId", true);
                    CommonUtility.ValidateInputFormData(phoenixPredictionsInput.PageNumber, "PageNumber", false);

                    phoenixPredictionsOutput = _GenericSelfservice.GetPhoenixPrediction(phoenixPredictionsInput);
                }
                else
                {
                    return GetFaultResponse(CONSTANTS.InputDataEmpty);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(GenericInstaModelController), nameof(PhoenixPredictionResponse), ex.Message + $"   StackTrace = {ex.StackTrace}", ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericInstaModelController), nameof(PhoenixPredictionResponse), "PhoenixPredictionResponse Ended", string.IsNullOrEmpty(phoenixPredictionsInput.CorrelationId) ? default(Guid) : new Guid(phoenixPredictionsInput.CorrelationId), string.Empty, string.Empty, string.Empty, string.Empty);
            return Ok(phoenixPredictionsOutput);
        }

        /// <summary>
        /// Gets All the Application Details
        /// </summary>
        /// <returns>Application Details</returns>
        [HttpGet]
        [Route("api/GetAllAppDetails")]
        public IActionResult GetAllAppDetails(string clientUId, string deliveryConstructUID, string CorrelationId, string Environment)
        {
            try
            {
                if (!(string.IsNullOrEmpty(clientUId)) && !(string.IsNullOrEmpty(deliveryConstructUID)))
                {
                    return Ok(_GenericSelfservice.AllAppDetails(clientUId, deliveryConstructUID, CorrelationId, Environment));
                }
                else
                {
                    return Ok("Client Id & DCID Values are Null. Please try again");
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(GenericInstaModelController), nameof(GetAllAppDetails), ex.Message + $"   StackTrace = {ex.StackTrace}", ex,string.Empty,string.Empty, clientUId, deliveryConstructUID);
                return GetFaultResponse(ex.Message);
            }
        }

        /// <summary>
        /// To check the status of all the models is Trained or not.
        /// </summary>
        /// <param name="clientid"></param>
        /// <param name="dcid"></param>
        /// <param name="userid"></param>
        /// <returns>Model Training Status</returns>
        [HttpGet]
        [Route("api/GetDefaultModelsTrainingStatus")]
        public IActionResult GetDefaultModelsTrainingStatus(string clientid, string dcid, string userid)
        {
            try
            {
                if (!(string.IsNullOrEmpty(clientid)) && !(string.IsNullOrEmpty(dcid)) && !(string.IsNullOrEmpty(userid)))
                {
                    if (!CommonUtility.GetValidUser(userid))
                    {
                        return GetFaultResponse(Resource.IngrainResx.InValidUser);
                    }
                    return Ok(_GenericSelfservice.GetDefaultModelsTrainingStatus(clientid, dcid, userid));
                }
                else
                {
                    return Ok("Client Id, DCID and User id Values are Null. Please try again");
                }
            }

            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(GenericInstaModelController), nameof(GetDefaultModelsTrainingStatus), ex.Message + $"   StackTrace = {ex.StackTrace}", ex, string.Empty, string.Empty,clientid,dcid);
                return GetFaultResponse(ex.Message);
            }
        }
        /// <summary>
        /// To Check the Model Training is started for this ClientID and DCID.
        /// </summary>
        /// <param name="clientid"></param>
        /// <param name="dcid"></param>
        /// <param name="userid"></param>
        /// <returns>Training Status</returns>
        [HttpGet]
        [Route("api/GetAppModelsTrainingStatus")]
        public IActionResult GetAppModelsTrainingStatus(string clientid, string dcid, string userid)
        {
            try
            {
                if (!(string.IsNullOrEmpty(clientid)) && !(string.IsNullOrEmpty(dcid)) && !(string.IsNullOrEmpty(userid)))
                {
                    if (!CommonUtility.GetValidUser(userid))
                    {
                        return GetFaultResponse(Resource.IngrainResx.InValidUser);
                    }
                    return Ok(_GenericSelfservice.GetAppModelsTrainingStatus(clientid, dcid, userid));
                }
                else
                {
                    return Ok("Client Id, DCID and User id Values are Null. Please try again");
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(GenericInstaModelController), nameof(GetAppModelsTrainingStatus), ex.Message + $"   StackTrace = {ex.StackTrace}", ex,string.Empty,string.Empty,clientid,dcid);
                return GetFaultResponse(ex.Message);
            }
        }

        /// <summary>
        /// To Add New Application details to Collection
        /// </summary>
        /// <param name="appIntegrations"></param>
        /// <returns>Application details</returns>
        [HttpPost]
        [Route("api/AddNewApp")]
        public IActionResult AddNewApp(AppIntegration appIntegrations)
        {
            try
            {
                if (!CommonUtility.GetValidUser(appIntegrations.CreatedByUser))
                {
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);
                }
                else if (!CommonUtility.GetValidUser(appIntegrations.ModifiedByUser))
                {
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);
                }
                CommonUtility.ValidateInputFormData(appIntegrations.ApplicationName, "ApplicationName", false);
                CommonUtility.ValidateInputFormData(appIntegrations.BaseURL, "BaseURL", false);
                CommonUtility.ValidateInputFormData(appIntegrations.Environment, "Environment", false);
                CommonUtility.ValidateInputFormData(appIntegrations.clientUId, "clientUId", true);
                CommonUtility.ValidateInputFormData(appIntegrations.TokenGenerationURL, "TokenGenerationURL", false);
                CommonUtility.ValidateInputFormData(Convert.ToString(appIntegrations.Credentials), "Credentials", false);
                CommonUtility.ValidateInputFormData(appIntegrations.deliveryConstructUID, "deliveryConstructUID", true);
                CommonUtility.ValidateInputFormData(appIntegrations.Authentication, "Authentication", false);
                CommonUtility.ValidateInputFormData(appIntegrations.chunkSize, "chunkSize", false);
                CommonUtility.ValidateInputFormData(appIntegrations.PredictionQueueLimit, "PredictionQueueLimit", false);
                CommonUtility.ValidateInputFormData(appIntegrations.AppNotificationUrl, "AppNotificationUrl", false);

                if (!string.IsNullOrEmpty(appIntegrations.ApplicationName) && !string.IsNullOrEmpty(appIntegrations.BaseURL) && !string.IsNullOrEmpty(appIntegrations.CreatedByUser))
                    return Ok(_GenericSelfservice.AddNewApp(appIntegrations));
                else
                    throw new Exception("Application name and URL are mandatory");

            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(GenericInstaModelController), nameof(GetAllAppDetails), ex.Message + $"   StackTrace = {ex.StackTrace}", ex,
                    appIntegrations.ApplicationID, string.Empty,appIntegrations.clientUId, appIntegrations.deliveryConstructUID);
                return GetFaultResponse(ex.Message);
            }
        }
        /// <summary>
        /// Custom API to test Generic Self Service.
        /// </summary>
        /// <param name="Args"></param>
        /// <returns>Actual data</returns>
        [HttpPost]
        [Route("api/GetIngestedData")]
        public IActionResult GetIngestedData(dynamic Args)
        {
            try
            {
                string correlationid = Convert.ToString(Args.correlationid);
                string datecolumn = Convert.ToString(Args.DateColumn);
                CommonUtility.ValidateInputFormData(correlationid, "correlationid", true);
                CommonUtility.ValidateInputFormData(datecolumn, "DateColumn", false);
                CommonUtility.ValidateInputFormData(Convert.ToString(Args.noOfRecord), "noOfRecord", false);
                int noOfRecord = Convert.ToInt32(Args.noOfRecord);
                //string datecolumn = Convert.ToString(Args.DateColumn);
                return Ok(_GenericSelfservice.GetIngestedData(correlationid, noOfRecord, datecolumn));
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(GenericInstaModelController), nameof(GetIngestedData), ex.Message + $"   StackTrace = {ex.StackTrace}", ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }

        }

        [HttpPost]
        [Route("api/GetAIServiceIngestedData")]
        public IActionResult GetAIServiceIngestedData(dynamic Args)
        {
            try
            {
                string correlationid = Convert.ToString(Args.correlationid);
                string datecolumn = Convert.ToString(Args.DateColumn);
                CommonUtility.ValidateInputFormData(Convert.ToString(Args.noOfRecord), "noOfRecord", false);
                CommonUtility.ValidateInputFormData(correlationid, "correlationid", true);
                CommonUtility.ValidateInputFormData(datecolumn, "DateColumn", false);
                int noOfRecord = Convert.ToInt32(Args.noOfRecord);
                return Ok(_GenericSelfservice.GetAIServiceIngestedData(correlationid, noOfRecord, datecolumn));
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(GenericInstaModelController), nameof(GetAIServiceIngestedData), ex.Message + $"   StackTrace = {ex.StackTrace}", ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }

        }


        [HttpPost]
        [Route("api/GetClusteringIngestedData")]
        public IActionResult GetClusteringIngestedData(dynamic Args)
        {
            try
            {
                string correlationid = Convert.ToString(Args.correlationid);
                string datecolumn = Convert.ToString(Args.DateColumn);
                CommonUtility.ValidateInputFormData(correlationid, "correlationid", true);
                CommonUtility.ValidateInputFormData(datecolumn, "DateColumn", false);
                CommonUtility.ValidateInputFormData(Convert.ToString(Args.noOfRecord), "noOfRecord", false);
                int noOfRecord = Convert.ToInt32(Args.noOfRecord);
                return Ok(_GenericSelfservice.GetClusteringIngestedData(correlationid, noOfRecord, datecolumn));
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(GenericInstaModelController), nameof(GetClusteringIngestedData), ex.Message + $"   StackTrace = {ex.StackTrace}", ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }

        }


        /// <summary>
        /// Gets the failed model data, for creating Private model again.
        /// </summary>
        /// <param name="requestBody"></param>
        /// <returns>TrainingStatus</returns>

        [HttpPost]
        [Route("api/ModelReTraning")]
        public IActionResult ModelReTraning([FromBody] dynamic requestBody)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericInstaModelController), nameof(ModelReTraning), "START", string.Empty, string.Empty, string.Empty, string.Empty);

            List<PrivateModelDetails> models = new List<PrivateModelDetails>();
            string data = Convert.ToString(requestBody);
            models = JsonConvert.DeserializeObject<List<PrivateModelDetails>>(data);
            try
            {
                IsTrainingEnabled(appSettings.EnableTraining);
                if (models != null)
                {
                    _trainingResponse = _GenericSelfservice.ModelReTraning(models);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(GenericInstaModelController), nameof(PrivateModelTraning), ex.Message + $"   StackTrace = {ex.StackTrace}", ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }

            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericInstaModelController), nameof(ModelReTraning), "END", string.Empty, string.Empty, string.Empty, string.Empty);
            return Ok(_trainingResponse);


        }

        /// <summary>
        /// Gets the failed model data, for creating Private model again.
        /// </summary>
        /// <param name="requestBody"></param>
        /// <returns>TrainingStatus</returns>

        [HttpPost]
        [Route("api/TrainGenericModels")]
        public IActionResult TrainGenericModels([FromBody] dynamic requestBody)
        {
            GetAppService();

            try
            {
                IsTrainingEnabled(appSettings.EnableTraining);
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericInstaModelController), nameof(TrainGenericModels), "START", string.Empty, string.Empty, string.Empty, string.Empty);
                List<TemplateTrainingInput> models = new List<TemplateTrainingInput>();
                string data = Convert.ToString(requestBody);
                string status = string.Empty;
                models.Add(JsonConvert.DeserializeObject<TemplateTrainingInput>(data));

                CommonUtility.ValidateInputFormData(models[0].ApplicationId, "ApplicationId", true);
                CommonUtility.ValidateInputFormData(models[0].ClientId, "ClientId", true);
                CommonUtility.ValidateInputFormData(models[0].DeliveryConstructId, "DeliveryConstructId", true);
                CommonUtility.ValidateInputFormData(models[0].UsecaseId, "UsecaseId", true);
                CommonUtility.ValidateInputFormData(models[0].ModelName, "ModelName", false);
                CommonUtility.ValidateInputFormData(models[0].DataSource, "DataSource", false);
                CommonUtility.ValidateInputFormData(models[0].DataSourceDetails?.Url, "DataSourceDetailsUrl", false);
                CommonUtility.ValidateInputFormData(models[0].DataSourceDetails?.HttpMethod, "DataSourceDetailsHttpMethod", false);
                CommonUtility.ValidateInputFormData(models[0].DataSourceDetails?.FetchType, "DataSourceDetailsFetchType", false);
                CommonUtility.ValidateInputFormData(models[0].DataSourceDetails?.BatchSize, "DataSourceDetailsBatchSize", false);
                CommonUtility.ValidateInputFormData(models[0].DataSourceDetails?.TotalNoOfRecords, "DataSourceDetailsTotalNoOfRecords", false);
                CommonUtility.ValidateInputFormData(Convert.ToString(models[0].DataSourceDetails?.BodyParams), "DataSourceDetailsBodyParams", false);

                //Asset Tracking

                auditTrailLog.ApplicationID = models[0].ApplicationId;
                auditTrailLog.ClientId = models[0].ClientId;
                auditTrailLog.DCID = models[0].DeliveryConstructId;
                auditTrailLog.ApplicationID = models[0].ApplicationId;
                auditTrailLog.UseCaseId = models[0].UsecaseId;
                auditTrailLog.CreatedBy = models[0].UserId;
                auditTrailLog.ProcessName = CONSTANTS.TrainingName;
                auditTrailLog.UsageType = CONSTANTS.AssetUsage;
                if (!CommonUtility.GetValidUser(Convert.ToString(models[0].UserId)))
                {
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);
                }
                _GenericSelfservice.AuditTrailLog(auditTrailLog);
                if (models != null)
                {
                    string resourceId = Request.Headers["resourceId"];
                    if (models[0].DataSource != "Phoenix")
                    {
                        if (string.IsNullOrEmpty(resourceId))
                            throw new ArgumentNullException(resourceId);

                        status = _GenericSelfservice.UpdateMappingCollections(models[0], resourceId);
                    }
                    string correlationId = _GenericSelfservice.GetGenericModelStatus(models[0].ClientId, models[0].DeliveryConstructId, models[0].ApplicationId, models[0].UsecaseId);
                    models[0].CorrelationId = correlationId;
                    if (models[0].DataSource == "Phoenix" || status == "Success")
                    {
                        _trainingResponse = _GenericSelfservice.TemplateModelTraining(models);
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(GenericInstaModelController), nameof(TrainGenericModels), ex.Message + $"   StackTrace = {ex.StackTrace}", ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }

            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericInstaModelController), nameof(TrainGenericModels), "END", string.Empty, string.Empty, string.Empty, string.Empty);
            return Ok(_trainingResponse);


        }
        [HttpGet]
        [Route("api/IsAppEditable")]
        public IActionResult IsAppEditable(string CorrelationId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericInstaModelController), nameof(IsAppEditable), "START", string.IsNullOrEmpty(CorrelationId) ? default(Guid) : new Guid(CorrelationId), string.Empty, string.Empty, string.Empty, string.Empty);
            return Ok(_GenericSelfservice.IsAppEditable(CorrelationId));
        }

        [HttpPost]
        [Route("api/FetchVDSData")]
        public IActionResult FetchVDSData(VDSParams inputParams)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericInstaModelController), nameof(IsAppEditable), "START", string.Empty, string.Empty, inputParams.ClientID, inputParams.DeliveryConstructID);
            try
            {
                CommonUtility.ValidateInputFormData(inputParams.ClientID, "ClientID", true);
                CommonUtility.ValidateInputFormData(inputParams.DeliveryConstructID, "DeliveryConstructID", true);
                CommonUtility.ValidateInputFormData(inputParams.RequestType, "RequestType", false);
                CommonUtility.ValidateInputFormData(inputParams.StartDate, "StartDate", false);
                CommonUtility.ValidateInputFormData(inputParams.ServiceType, "ServiceType", false);
                CommonUtility.ValidateInputFormData(inputParams.EndDate, "EndDate", false);
                return Ok(_GenericSelfservice.GetVDSData(inputParams));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("api/AppDelete")]
        public IActionResult AppDelete(string applicationId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericInstaModelController), nameof(AppDelete), "START", applicationId, string.Empty, string.Empty, string.Empty);
            try
            {
                return Ok(_GenericSelfservice.deleteAppName(applicationId));
            }
            catch (Exception ex)
            {
                return GetFaultResponse(ex.Message);
            }

        }


        /// <summary>
        /// Gets the failed model data, for creating Private model again.
        /// </summary>
        /// <param name="requestBody"></param>
        /// <returns>TrainingStatus</returns>

        [HttpPost]
        [Route("api/IngrainGenericTrainingRequest")]
        public IActionResult IngrainGenericTrainingRequest([FromBody] dynamic requestBody)
        {
            try
            {
                IsTrainingEnabled(appSettings.EnableTraining);
                GetAppService();
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericInstaModelController)
                                                         , nameof(IngrainGenericTrainingRequest)
                                                         , "START", string.Empty, string.Empty, string.Empty, string.Empty);
                string data = Convert.ToString(requestBody);
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericInstaModelController)
                                                     , nameof(IngrainGenericTrainingRequest)
                                                     , "START SPE DATA--" + data, string.Empty, string.Empty, string.Empty, string.Empty);
                TrainingRequestDetails trainingRequestDetails = JsonConvert.DeserializeObject<TrainingRequestDetails>(data);
                CommonUtility.ValidateInputFormData(trainingRequestDetails.ClientUId, "ClientUId", true);
                CommonUtility.ValidateInputFormData(trainingRequestDetails.DeliveryConstructUId, "DeliveryConstructUId", true);
                CommonUtility.ValidateInputFormData(trainingRequestDetails.UseCaseId, "UseCaseId", true);
                CommonUtility.ValidateInputFormData(trainingRequestDetails.CorrelationId, "CorrelationId", true);
                CommonUtility.ValidateInputFormData(trainingRequestDetails.ApplicationId, "ApplicationId", true);
                CommonUtility.ValidateInputFormData(trainingRequestDetails.DataSource, "DataSource", false, trainingRequestDetails.ApplicationId);
                CommonUtility.ValidateInputFormData(trainingRequestDetails.IngrainTrainingResponseCallBackUrl, "IngrainTrainingResponseCallBackUrl", false);
                CommonUtility.ValidateInputFormData(trainingRequestDetails.DataSourceDetails?.Url, "DataSourceDetails-Url", false);
                CommonUtility.ValidateInputFormData(trainingRequestDetails.DataSourceDetails?.HttpMethod, "DataSourceDetails-HttpMethod", false);
                CommonUtility.ValidateInputFormData(trainingRequestDetails.DataSourceDetails?.BodyParams?.PageNumber, "DataSourceDetails-BodyParams-PageNumber", false);
                CommonUtility.ValidateInputFormData(trainingRequestDetails.DataSourceDetails?.BodyParams?.UniqueId, "DataSourceDetails-BodyParams-UniqueId", true);
                CommonUtility.ValidateInputFormData(trainingRequestDetails.DataSourceDetails?.FetchType, "DataSourceDetails-FetchType", false);
                CommonUtility.ValidateInputFormData(trainingRequestDetails.DataSourceDetails?.BatchSize, "DataSourceDetails-BatchSize", false);
                CommonUtility.ValidateInputFormData(trainingRequestDetails.DataSourceDetails?.TotalNoOfRecords, "DataSourceDetails-TotalNoOfRecords", false);
                CommonUtility.ValidateInputFormData(trainingRequestDetails.Data, "Data", false, trainingRequestDetails.ApplicationId);
                CommonUtility.ValidateInputFormData(trainingRequestDetails.DataFlag, "DataFlag", false, trainingRequestDetails.ApplicationId);

                //Asset Tracking
                auditTrailLog.CorrelationId = trainingRequestDetails.CorrelationId;
                //auditTrailLog.ApplicationID = trainingRequestDetails.ApplicationId;
                auditTrailLog.ClientId = trainingRequestDetails.ClientUId;
                auditTrailLog.DCID = trainingRequestDetails.DeliveryConstructUId;
                auditTrailLog.ApplicationID = trainingRequestDetails.ApplicationId;
                auditTrailLog.UseCaseId = trainingRequestDetails.UseCaseId;
                auditTrailLog.CreatedBy = trainingRequestDetails.UserId;
                auditTrailLog.ProcessName = CONSTANTS.TrainingName;
                auditTrailLog.UsageType = CONSTANTS.AssetUsage;
                auditTrailLog.BaseAddress = trainingRequestDetails.IngrainTrainingResponseCallBackUrl;
                if (!CommonUtility.GetValidUser(trainingRequestDetails.UserId))
                {
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);
                }
                _GenericSelfservice.AuditTrailLog(auditTrailLog);
                string resourceId = Request.Headers["resourceId"];

                if (trainingRequestDetails != null && (!string.IsNullOrEmpty(resourceId) || appSettings.authProvider.ToUpper() != CONSTANTS.AzureAD.ToUpper()))
                {
                    _trainingResponse = _GenericSelfservice.IngrainGenericTrainingRequest(trainingRequestDetails, resourceId);
                }
                else
                {
                    _trainingResponse.Message = CONSTANTS.IncompleteTrainingRequest;
                    _trainingResponse.Status = CONSTANTS.E;
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(GenericInstaModelController)
                                                              , nameof(IngrainGenericTrainingRequest)
                                                              , ex.Message + $"   StackTrace = {ex.StackTrace}", ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }

            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericInstaModelController), nameof(IngrainGenericTrainingRequest), "END", string.Empty, string.Empty, string.Empty, string.Empty);
            return Ok(_trainingResponse);
        }


        [HttpGet]
        [Route("api/IngrainGenericTrainingResponse")]
        public IActionResult IngrainGenericTrainingResponse(string CorrelationId)
        {
            GetAppService();
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericInstaModelController), nameof(IngrainGenericTrainingResponse), "START", string.IsNullOrEmpty(CorrelationId) ? default(Guid) : new Guid(CorrelationId), string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                if (!(string.IsNullOrEmpty(CorrelationId)))
                    return Ok(_GenericSelfservice.IngrainGenericTrainingResponse(CorrelationId));
                else
                    return Ok(CONSTANTS.ClientDCUserIDareNull);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(GenericInstaModelController), nameof(IngrainGenericTrainingResponse), ex.Message + $"   StackTrace = {ex.StackTrace}", ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
        }

        [HttpPost]
        [Route("api/IngrainGenericPredictionRequest")]
        public IActionResult IngrainGenericPredictionRequest(string CorrelationId)
        {

            try
            {
                IsPredictionEnabled(appSettings.EnablePrediction);
                GetAppService();
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericInstaModelController), nameof(IngrainGenericPredictionRequest), "START", string.IsNullOrEmpty(CorrelationId) ? default(Guid) : new Guid(CorrelationId), string.Empty, string.Empty, string.Empty, string.Empty);
                //Asset Tracking
                auditTrailLog.CorrelationId = CorrelationId;
                auditTrailLog.ProcessName = CONSTANTS.PredictionName;
                auditTrailLog.UsageType = CONSTANTS.AssetUsage;
                _GenericSelfservice.AuditTrailLog(auditTrailLog);
                if (!string.IsNullOrEmpty(CorrelationId))
                {
                    CommonUtility.ValidateInputFormData(CorrelationId, CONSTANTS.CorrelationId, true);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericInstaModelController), nameof(IngrainGenericPredictionRequest), "START", string.IsNullOrEmpty(CorrelationId) ? default(Guid) : new Guid(CorrelationId), string.Empty, string.Empty, string.Empty, string.Empty);
                    string actualData = HttpContext.Request.Form["PredictionRequestData"];
                    string predictionCallbackUrl = HttpContext.Request.Form["IngrainPredictionResponseCallBackUrl"];
                    CommonUtility.ValidateInputFormData(predictionCallbackUrl, "predictionCallbackUrl", false);
                    CommonUtility.ValidateInputFormData(actualData, "actualData", false);
                    //Asset Tracking
                    auditTrailLog.CorrelationId = CorrelationId;
                    auditTrailLog.ProcessName = CONSTANTS.PredictionName;
                    auditTrailLog.UsageType = CONSTANTS.AssetUsage;
                    _GenericSelfservice.AuditTrailLog(auditTrailLog);
                    _modelPrediction = _GenericSelfservice.IngrainGenericPredictionRequest(CorrelationId, actualData, predictionCallbackUrl);
                }
                else
                    return Accepted(Resource.IngrainResx.CorrelatioUIDEmpty);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(GenericInstaModelController), nameof(IngrainGenericPredictionRequest), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericInstaModelController), nameof(IngrainGenericPredictionRequest), "END", string.IsNullOrEmpty(CorrelationId) ? default(Guid) : new Guid(CorrelationId), string.Empty, string.Empty, string.Empty, string.Empty);
            return GetSuccessResponse(_modelPrediction);
        }


        [HttpGet]
        [Route("api/IngrainGenericPredictionResponse")]
        public IActionResult IngrainGenericPredictionResponse(string CorrelationId, string UniqueId)
        {
            GetAppService();
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericInstaModelController), nameof(IngrainGenericPredictionResponse), "START UniqueId - " + UniqueId, string.IsNullOrEmpty(CorrelationId) ? default(Guid) : new Guid(CorrelationId), string.Empty, string.Empty, string.Empty, string.Empty);

            string response = string.Empty;
            try
            {
                if (!string.IsNullOrWhiteSpace(CorrelationId) && !string.IsNullOrWhiteSpace(UniqueId))
                    _predictionresult = _GenericSelfservice.IngrainGenericPredictionResponse(CorrelationId, UniqueId);
                else
                    return Accepted(Resource.IngrainResx.CorrelatioUIDEmpty);

                if (_predictionresult.Status == CONSTANTS.E)
                    return GetFaultResponse(_predictionresult);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(GenericInstaModelController), nameof(IngrainGenericPredictionResponse), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(_predictionresult);
            }
            response = JsonConvert.SerializeObject(_predictionresult);
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericInstaModelController), nameof(IngrainGenericPredictionResponse), "END - " + CONSTANTS.Prediction + response.Replace(CONSTANTS.slash, string.Empty), string.IsNullOrEmpty(CorrelationId) ? default(Guid) : new Guid(CorrelationId), string.Empty, string.Empty, string.Empty, string.Empty);
            return GetSuccessResponse(_predictionresult);
        }


        [HttpPost]
        [Route("api/ResponseCallBackUrl")]
        public IActionResult ResponseCallBackUrl([FromBody] dynamic requestBody)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericInstaModelController), nameof(ResponseCallBackUrl), "ResponseCallBackUrl Started", string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                string ResponseData = Convert.ToString(requestBody);
                if (!string.IsNullOrEmpty(ResponseData))
                {

                    CommonUtility.ValidateInputFormData(ResponseData, "ResponseData", false);
                    _GenericSelfservice.UpdateResponseData(ResponseData);
                }
                else
                {
                    return GetFaultResponse(CONSTANTS.InputDataEmpty);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(GenericInstaModelController), nameof(ResponseCallBackUrl), ex.Message + $"   StackTrace = {ex.StackTrace}", ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericInstaModelController), nameof(ResponseCallBackUrl), "ResponseCallBackUrl Ended", string.Empty, string.Empty, string.Empty, string.Empty);
            return Ok();
        }
        [HttpGet]
        [Route("api/BulkPredictionsTest")]
        public IActionResult BulkPredictionsTest(string correlationId, int noOfRequest = 1, int recordsPerRequest = 1)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericInstaModelController), nameof(BulkPredictionsTest), "START BUlK - Prediction for " + correlationId + " and no of requests " + noOfRequest, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);

            string response = string.Empty;
            try
            {
                if (noOfRequest > 50)
                    throw new ArgumentOutOfRangeException(nameof(noOfRequest), "Value should be less than or equal to 50");
                if (recordsPerRequest > 50)
                    throw new ArgumentOutOfRangeException(nameof(recordsPerRequest), "Value should be less than or equal to 50");

                _GenericSelfservice.BulkPredictionsTest(correlationId, noOfRequest, recordsPerRequest);
            }

            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(GenericInstaModelController), nameof(BulkPredictionsTest), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }

            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericInstaModelController), nameof(BulkPredictionsTest), "END - " + CONSTANTS.Prediction + response.Replace(CONSTANTS.slash, string.Empty), string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            return Ok("Success");
        }

        [HttpPost]
        [Route("api/IngrainAPICallBack")]
        public IActionResult IngrainAPICallBack([FromBody] dynamic requestBody)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericInstaModelController), nameof(IngrainAPICallBack), "IngrainAPI Started", string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                string ResponseData = Convert.ToString(requestBody);
                if (!string.IsNullOrEmpty(ResponseData))
                {
                    RequestData RequestPayload  = JsonConvert.DeserializeObject<RequestData>(ResponseData);
                    CommonUtility.ValidateInputFormData(RequestPayload.AppServiceUId, "AppServiceUId", true);
                    CommonUtility.ValidateInputFormData(RequestPayload.ClientUId, "ClientUId", true);
                    CommonUtility.ValidateInputFormData(RequestPayload.DeliveryConstructUId, "DeliveryConstructUId", true);
                    CommonUtility.ValidateInputFormData(RequestPayload.ResponseCallbackUrl, "ResponseCallbackUrl", false);
                    CommonUtility.ValidateInputFormData(RequestPayload.UseCaseUId, "UseCaseUId", true);
                    var data = _GenericSelfservice.InitiateTraining(ResponseData);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericInstaModelController), nameof(IngrainAPICallBack), "IngrainAPI Ended", string.IsNullOrEmpty(data.CorrelationId) ? default(Guid) : new Guid(data.CorrelationId), string.Empty, string.Empty, data.ClientUId, data.DeliveryConstructUId);
                    return Ok(data);
                }
                else
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericInstaModelController), nameof(IngrainAPICallBack), "IngrainAPI Ended",null,null,null, null);
                    return GetFaultResponse(CONSTANTS.InputDataEmpty);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(GenericInstaModelController), nameof(IngrainAPICallBack), ex.Message + $"   StackTrace = {ex.StackTrace}", ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
        }

        [HttpPost]
        [Route("api/UpdateGenericModelMandatoryDetails")]
        public IActionResult UpdateGenericModelMandatoryDetails([FromBody] dynamic requestBody)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericInstaModelController), nameof(UpdateGenericModelMandatoryDetails), "UpdateGenericModelMandatoryDetails Started", string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                string UpdateData = Convert.ToString(requestBody);
                if (!string.IsNullOrEmpty(UpdateData))
                {
                    UpdatePublicTemplatedata RequestPayload = JsonConvert.DeserializeObject<UpdatePublicTemplatedata>(UpdateData);
                    CommonUtility.ValidateInputFormData(RequestPayload.ApplicationName, "ApplicationName", false);
                    CommonUtility.ValidateInputFormData(RequestPayload.UsecaseName, "UsecaseName", false);
                    CommonUtility.ValidateInputFormData(RequestPayload.Resource, "Resource", false);
                    CommonUtility.ValidateInputFormData(RequestPayload.SourceURL, "SourceURL", false);
                    CommonUtility.ValidateInputFormData(Convert.ToString(RequestPayload.InputParameters), "InputParameters", false);
                    CommonUtility.ValidateInputFormData(RequestPayload.DateColumn, "DateColumn", false);

                    return Ok(_GenericSelfservice.UpdateGenericModelMandatoryDetails(UpdateData));
                }
                else
                {
                    return GetFaultResponse(CONSTANTS.InputDataEmpty);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(GenericInstaModelController), nameof(UpdateGenericModelMandatoryDetails), ex.Message + $"   StackTrace = {ex.StackTrace}", ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }

        }

        [HttpPost]
        [Route("api/GetCallBackUrlData")]
        public IActionResult GetCallBackUrlData(string ApplicationId, string UseCaseId, string ClientID, string DCID)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericInstaModelController), nameof(GetCallBackUrlData), "GetCallBackUrlData Started",ApplicationId, string.Empty,ClientID,DCID);
            try
            {
                #region VALIDATIONS
                CommonUtility.ValidateInputFormData(ApplicationId, "ApplicationId", true);
                CommonUtility.ValidateInputFormData(UseCaseId, "UseCaseId", true);
                CommonUtility.ValidateInputFormData(ClientID, "ClientID", true);
                CommonUtility.ValidateInputFormData(DCID, "DCID", true);
                #endregion
                return Ok(_GenericSelfservice.GetCallBackUrlData(ApplicationId, UseCaseId, ClientID, DCID));
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(GenericInstaModelController), nameof(GetCallBackUrlData), ex.Message + $"   StackTrace = {ex.StackTrace}", ex, ApplicationId, string.Empty, ClientID, DCID);
                return GetFaultResponse(ex.Message);
            }
        }
        [HttpPost]
        [Route("api/PrivateCascadeModelTraining")]
        public IActionResult PrivateCascadeModelTraining(string ClientID, string DCID, string UserId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericInstaModelController), nameof(PrivateCascadeModelTraining), "Cascade ModelTemplate Training  STARTED", string.Empty, string.Empty, ClientID, DCID);
            try
            {
                #region VALIDATIONS
                CommonUtility.ValidateInputFormData(ClientID, "ClientID", true);
                CommonUtility.ValidateInputFormData(DCID, "DCID", true);
                if (!CommonUtility.GetValidUser(UserId))
                {
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);
                }
                #endregion
                IsTrainingEnabled(appSettings.EnableTraining);
                var result = _GenericSelfservice.GetCascadeModelTemplateTraining(ClientID, DCID, UserId);
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericInstaModelController), nameof(PrivateCascadeModelTraining), "Cascade ModelTemplate Training  END ClientID-" + ClientID + "-DCID-" + DCID + "-UserId-" + UserId, string.Empty, string.Empty, ClientID, DCID);
                return Ok(result);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(GenericInstaModelController), nameof(PrivateCascadeModelTraining), ex.Message + $"   StackTrace = {ex.StackTrace}", ex, string.Empty, string.Empty, ClientID, DCID);
                return GetFaultResponse(ex.Message);
            }
        }

        /// <summary>
        /// Audit Trail Log
        /// </summary>
        /// <param name="correlationId">CorrelationId</param>
        /// <returns></returns>
        [HttpGet]
        [Route("api/AuditTrailLog")]
        public IActionResult AuditTrailLog(string correlationId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericInstaModelController), nameof(AuditTrailLog), "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                if (string.IsNullOrEmpty(correlationId))
                    throw new Exception("CorrelationId is null");
                return Ok(_GenericSelfservice.GetAuditTrailLog(correlationId));
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(GenericInstaModelController), nameof(AuditTrailLog), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
        }
        [HttpGet]
        [Route("api/GetFMModelsStatus")]
        public IActionResult GetFMModelsStatus(string clientid, string dcid, string userid)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericInstaModelController), nameof(GetFMModelsStatus), "START", string.Empty, string.Empty, clientid, dcid);
            try
            {
                if (!(string.IsNullOrEmpty(clientid)) && !(string.IsNullOrEmpty(dcid)) && !(string.IsNullOrEmpty(userid)))
                {
                    if (!CommonUtility.GetValidUser(userid))
                    {
                        return GetFaultResponse(Resource.IngrainResx.InValidUser);
                    }
                    var result = _GenericSelfservice.GetFMModelStatus(clientid, dcid, userid);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericInstaModelController), nameof(GetFMModelsStatus), "END", string.Empty, string.Empty, clientid, dcid);
                    return Ok(result);
                }
                else
                {
                    return Ok("Client Id, DCID and User id Values are Null. Please try again");
                }
            }

            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(GenericInstaModelController), nameof(GetFMModelsStatus), ex.Message + $"   StackTrace = {ex.StackTrace}", ex, string.Empty, string.Empty, clientid, dcid);
                return GetFaultResponse(ex.Message);
            }
        }
        [HttpGet]
        [Route("api/UpdateDataPoints")]
        public IActionResult UpdateDataPoints(long UsecasedataPoints, long AppDataPoints, string UsecaseId, string ApplicationId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericInstaModelController), nameof(UpdateDataPoints), "START",ApplicationId, string.Empty, string.Empty, string.Empty);
            DataPoints dataPoints = new DataPoints();
            try
            {
                Int64 i;
                bool success = Int64.TryParse(UsecasedataPoints.ToString(), out i);
                if(success)
                {
                    if (UsecasedataPoints > 0)
                    {
                        dataPoints = _GenericSelfservice.UpdateDataPoints(UsecasedataPoints, AppDataPoints, UsecaseId, ApplicationId);
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericInstaModelController), nameof(UpdateDataPoints), "END", ApplicationId, string.Empty, string.Empty, string.Empty);
                    }
                }
                else
                {
                    dataPoints.ErrorMessage = "Please provide numeric DataPoints";
                    dataPoints.IsUpdated = false;
                    return Ok(dataPoints);
                }
            }

            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(GenericInstaModelController), nameof(UpdateDataPoints), ex.Message + $"   StackTrace = {ex.StackTrace}", ex, ApplicationId, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
            return Ok(dataPoints);
        }
        #endregion
    }
}