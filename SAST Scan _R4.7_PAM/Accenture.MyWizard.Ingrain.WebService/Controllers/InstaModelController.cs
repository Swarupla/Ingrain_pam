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
Module Name     :   InstaModelController
Project         :   Accenture.MyWizard.SelfServiceAI.InstaModelController
Organisation    :   Accenture Technologies Ltd.
Created By      :   RaviA
Created Date    :   08-AUG-2019
Revision History :
Revision No             Initial             Modified Date           Description
1.0                     An                  08-AUG-2019             
\********************************************************************************************************/
#endregion
using Accenture.MyWizard.Ingrain.BusinessDomain.Common;
using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
using Accenture.MyWizard.Ingrain.DataModels.InstaModels;
using Accenture.MyWizard.Shared.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;

namespace Accenture.MyWizard.Ingrain.WebService.Controllers
{
    public class InstaModelController : MyWizardControllerBase
    {
        #region Members
        InstaModel instaModel = null;
        InstaRegression instaRegression = null;
        private IInstaModel _instaModelService { get; set; }
        private IngrainAppSettings appSettings { get; set; }

        #endregion

        #region Constructor       
        public InstaModelController(IServiceProvider serviceProvider, IOptions<IngrainAppSettings> settings)
        {
            instaModel = new InstaModel();
            _instaModelService = serviceProvider.GetService<IInstaModel>();
            appSettings = settings.Value;
            instaRegression = new InstaRegression();
        }
        #endregion

        #region Methods
        [HttpPost]
        [Route("api/IngestData")]
        public IActionResult IngestData([FromBody]dynamic requestBody)
        {
            GetAppService();
            
            InstaModel instaModel = null;
            InstaRegression instaRegression = null;
            string response = string.Empty;
            string response2 = string.Empty;
            try
            {
                IsTrainingEnabled(appSettings.EnableTraining);
                string data = Convert.ToString(requestBody);
                if (data != null)
                {
                    VDSRegression vdsRegression = JsonConvert.DeserializeObject<VDSRegression>(data);

                    #region VALIDATIONS
                    if (!CommonUtility.GetValidUser(vdsRegression.CreatedByUser))
                        return GetFaultResponse(Resource.IngrainResx.InValidUser);

                    CommonUtility.ValidateInputFormData(vdsRegression.ClientUID, CONSTANTS.ClientUID, true);
                    CommonUtility.ValidateInputFormData(vdsRegression.DCID, CONSTANTS.DCID, true);
                    CommonUtility.ValidateInputFormData(vdsRegression.Dimension, CONSTANTS.Dimension, false);
                    CommonUtility.ValidateInputFormData(vdsRegression.Frequency, CONSTANTS.Frequency, false);
                    CommonUtility.ValidateInputFormData(vdsRegression.FrequencySteps, CONSTANTS.FrequencySteps, false);
                    CommonUtility.ValidateInputFormData(vdsRegression.ProcessName, CONSTANTS.ProcessName, false);
                    CommonUtility.ValidateInputFormData(vdsRegression.Source, CONSTANTS.Source, false);
                    CommonUtility.ValidateInputFormData(vdsRegression.URL, CONSTANTS.URL, false);
                    CommonUtility.ValidateInputFormData(vdsRegression.UseCaseID, CONSTANTS.UseCaseID, true);
                    #endregion

                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelController), nameof(IngestData), CONSTANTS.IngestPayload, string.Empty, string.Empty, string.Empty, string.Empty);
                    _instaModelService.IngestData(data, out instaModel, out instaRegression);
                    if (instaRegression.instaMLResponse != null)
                    {
                        if (instaRegression.instaMLResponse.Count > 0)
                        {
                            foreach (var item in instaRegression.instaMLResponse)
                            {
                                if (item.Status == CONSTANTS.I || item.Status == CONSTANTS.E)
                                {
                                    response2 = JsonConvert.SerializeObject(instaRegression);
                                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelController), nameof(IngestData), "--ERROR--" + CONSTANTS.InstaMLResponse + response2.Replace(CONSTANTS.slash, string.Empty), string.Empty, string.Empty, string.Empty, string.Empty);
                                    return GetFaultResponse(instaRegression);
                                }
                            }
                            response2 = JsonConvert.SerializeObject(instaRegression);
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelController), nameof(IngestData), CONSTANTS.InstaMLResponse + response2.Replace(CONSTANTS.slash, string.Empty), string.Empty, string.Empty, string.Empty, string.Empty);
                            return Ok(instaRegression);
                        }
                    }
                    if (instaModel.Status == CONSTANTS.E || instaModel.Status == CONSTANTS.I)
                    {
                        response = JsonConvert.SerializeObject(instaModel);
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelController), nameof(IngestData), CONSTANTS.InstaMLResponse + response.Replace(CONSTANTS.slash, string.Empty), string.Empty, string.Empty, string.Empty, string.Empty);
                        return GetFaultResponse(instaModel);
                    }
                    else
                    {
                        response = JsonConvert.SerializeObject(instaModel);
                    }
                }
                else
                {
                    return GetFaultResponse(Resource.IngrainResx.EmptyData);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InstaModelController), nameof(IngestData), ex.Message + $"****InstaId***** = {instaModel.InstaID}" + $"   StackTrace = {ex.StackTrace}", ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelController), nameof(IngestData), CONSTANTS.InstaMLResponse + response.Replace(CONSTANTS.slash, string.Empty), string.Empty, string.Empty, string.Empty, string.Empty);            
            return Ok(instaModel);
        }


        /// <summary>
        /// Start the Models training
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("api/StartModelTraining")]
        public IActionResult StartModelTraining([FromBody]dynamic requestBody)
        {           
            GetAppService();
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelController), nameof(StartModelTraining), CONSTANTS.START, string.Empty, string.Empty, string.Empty, string.Empty);
            InstaModel instaModel = null;
            InstaPrediction instaPrediction = null;
            VdsData vdsDataModel = new VdsData();        
            VDSRegression vdsRegression = new VDSRegression();
            InstaRegression regressionResponse = new InstaRegression();
            string timeSeriesResponse = string.Empty;
            string regResponse = string.Empty;
            try
            {
                IsTrainingEnabled(appSettings.EnableTraining);
                string data = Convert.ToString(requestBody);
                var vdsData = JObject.Parse(data);
                string ProblemType = string.Empty;
                //LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelController), nameof(StartModelTraining), CONSTANTS.TrainingPayload + vdsData, string.IsNullOrEmpty(vdsData[CONSTANTS.CorrelationId].ToString()) ? default(Guid) : new Guid(vdsData[CONSTANTS.CorrelationId].ToString()),"", "", vdsData[CONSTANTS.ClientUId].ToString(), vdsData[CONSTANTS.DeliveryConstructUID].ToString());
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelController), nameof(StartModelTraining), CONSTANTS.TrainingPayload + vdsData, string.Empty, string.Empty, string.Empty, string.Empty);
                if (vdsData[CONSTANTS.UseCaseID] != null)
                {
                    vdsRegression = JsonConvert.DeserializeObject<VDSRegression>(data);

                    #region VALIDATIONS
                    if (!CommonUtility.GetValidUser(vdsRegression.CreatedByUser))
                        return GetFaultResponse(Resource.IngrainResx.InValidUser);

                    CommonUtility.ValidateInputFormData(vdsRegression.ClientUID, CONSTANTS.ClientUID, true);
                    CommonUtility.ValidateInputFormData(vdsRegression.DCID, CONSTANTS.DCID, true);
                    CommonUtility.ValidateInputFormData(vdsRegression.Dimension, CONSTANTS.Dimension, false);
                    CommonUtility.ValidateInputFormData(vdsRegression.Frequency, CONSTANTS.Frequency, false);
                    CommonUtility.ValidateInputFormData(vdsRegression.FrequencySteps, CONSTANTS.FrequencySteps, false);
                    CommonUtility.ValidateInputFormData(vdsRegression.ProcessName, CONSTANTS.ProcessName, false);
                    CommonUtility.ValidateInputFormData(vdsRegression.Source, CONSTANTS.Source, false);
                    CommonUtility.ValidateInputFormData(vdsRegression.URL, CONSTANTS.URL, false);
                    CommonUtility.ValidateInputFormData(vdsRegression.UseCaseID, CONSTANTS.UseCaseID, true);
                    #endregion

                    ProblemType = CONSTANTS.Regression;
                    if (data != null)
                    {
                        if (vdsRegression.ProcessName == CONSTANTS.DataEngineering || vdsRegression.ProcessName == CONSTANTS.ModelEngineering || vdsRegression.ProcessName == CONSTANTS.DeployModel || vdsRegression.ProcessName == CONSTANTS.DeleteModel)
                        {
                            regressionResponse = _instaModelService.StartModelTraining(vdsRegression);
                            regResponse = JsonConvert.SerializeObject(regressionResponse);
                            foreach (var error in regressionResponse.instaMLResponse)
                            {
                                if (error.Status == CONSTANTS.E)
                                {
                                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelController), nameof(StartModelTraining), CONSTANTS.InstaMLTrainResponse + regResponse.Replace(CONSTANTS.slash, string.Empty), string.Empty, string.Empty, vdsRegression.ClientUID, vdsRegression.DCID);
                                    return GetFaultResponse(regressionResponse);
                                }
                            }
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelController), nameof(StartModelTraining), CONSTANTS.InstaMLTrainResponse + regResponse.Replace(CONSTANTS.slash, string.Empty), string.Empty, string.Empty, vdsRegression.ClientUID, vdsRegression.DCID);
                            return Ok(regressionResponse);
                        }
                        switch (vdsRegression.ProcessName)
                        {
                            case CONSTANTS.Prediction:
                                regressionResponse = _instaModelService.GetRegressionPrediction(vdsRegression);
                                regResponse = JsonConvert.SerializeObject(regressionResponse);
                                foreach (var error in regressionResponse.instaMLResponse)
                                {
                                    if (error.Status == CONSTANTS.E)
                                    {
                                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelController), nameof(StartModelTraining), CONSTANTS.InstaMLTrainResponse + regResponse.Replace(CONSTANTS.slash, string.Empty), string.Empty, string.Empty, vdsRegression.ClientUID, vdsRegression.DCID);
                                        return GetFaultResponse(regressionResponse);
                                    }
                                }
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelController), nameof(StartModelTraining), CONSTANTS.InstaMLTrainResponse + regResponse.Replace(CONSTANTS.slash, string.Empty), string.Empty, string.Empty, vdsRegression.ClientUID, vdsRegression.DCID);
                                return Ok(regressionResponse);

                            case CONSTANTS.RefitModel:
                                bool isFieldsValidated = this.ValidateInput(vdsRegression);
                                if (isFieldsValidated)
                                    return GetFaultResponse(instaRegression);
                                regressionResponse = _instaModelService.RegressionRefitModel(vdsRegression);
                                regResponse = JsonConvert.SerializeObject(regressionResponse);
                                foreach (var error in regressionResponse.instaMLResponse)
                                {
                                    if (error.Status == CONSTANTS.E)
                                    {
                                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelController), nameof(StartModelTraining), CONSTANTS.InstaMLTrainResponse + regResponse.Replace(CONSTANTS.slash, string.Empty), string.Empty, string.Empty, vdsRegression.ClientUID, vdsRegression.DCID);
                                        return GetFaultResponse(regressionResponse);
                                    }
                                }
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelController), nameof(StartModelTraining), CONSTANTS.InstaMLTrainResponse + regResponse.Replace(CONSTANTS.slash, string.Empty), string.Empty, string.Empty, vdsRegression.ClientUID, vdsRegression.DCID);
                                return Ok(regressionResponse);

                            case CONSTANTS.ModelStatus:
                                var modelStatus = _instaModelService.RegressionModelStatus(vdsRegression);
                                regResponse = JsonConvert.SerializeObject(modelStatus);
                                foreach (var error in modelStatus.instaMLResponse)
                                {
                                    if (error.Status == CONSTANTS.E)
                                    {
                                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelController), nameof(StartModelTraining), CONSTANTS.InstaMLTrainResponse + regResponse.Replace(CONSTANTS.slash, string.Empty), string.Empty, string.Empty, vdsRegression.ClientUID, vdsRegression.DCID);
                                        return GetFaultResponse(regressionResponse);
                                    }
                                }
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelController), nameof(StartModelTraining), CONSTANTS.InstaMLTrainResponse + regResponse.Replace(CONSTANTS.slash, string.Empty), string.Empty, string.Empty, vdsRegression.ClientUID, vdsRegression.DCID);
                                return Ok(modelStatus);
                        }
                    }
                    else
                        return Accepted(CONSTANTS.ProblemTypeNull);
                }
                else
                {
                    vdsDataModel = JsonConvert.DeserializeObject<VdsData>(data);

                    #region VALIDATIONS
                    if (!CommonUtility.GetValidUser(vdsDataModel.CreatedByUser))
                        return GetFaultResponse(Resource.IngrainResx.InValidUser);

                    CommonUtility.ValidateInputFormData(vdsDataModel.InstaId, CONSTANTS.InstaId, true);
                    CommonUtility.ValidateInputFormData(vdsDataModel.CorrelationId, CONSTANTS.CorrelationId, true);
                    CommonUtility.ValidateInputFormData(vdsDataModel.ClientUId, CONSTANTS.ClientUID, true);
                    CommonUtility.ValidateInputFormData(vdsDataModel.ProcessName, CONSTANTS.ProcessName, false);
                    CommonUtility.ValidateInputFormData(vdsDataModel.ProblemType, CONSTANTS.ProblemType, false);
                    CommonUtility.ValidateInputFormData(vdsDataModel.DeliveryConstructUID, CONSTANTS.DeliveryConstructUID, true);
                    CommonUtility.ValidateInputFormData(vdsDataModel.ActualData, CONSTANTS.ActualData, false);                                       
                    #endregion
                    if (data != null)
                    {
                        if (vdsDataModel.ProblemType != null)
                        {
                            instaModel = _instaModelService.StartModelTraining(vdsDataModel);
                            timeSeriesResponse = JsonConvert.SerializeObject(instaModel);
                            switch (vdsDataModel.ProcessName)
                            {
                                case CONSTANTS.Prediction:
                                    instaPrediction = _instaModelService.Prediction(vdsDataModel.InstaId, vdsDataModel.CorrelationId, vdsDataModel.CreatedByUser, vdsDataModel.ProblemType, vdsDataModel.ActualData);
                                    instaPrediction.ClientUID = vdsDataModel.ClientUId;
                                    instaPrediction.DCID = vdsDataModel.DeliveryConstructUID;
                                    instaPrediction.CreatedByUser = vdsDataModel.CreatedByUser;
                                    timeSeriesResponse = JsonConvert.SerializeObject(instaPrediction);
                                    if (instaPrediction.Status == CONSTANTS.E)
                                    {
                                        timeSeriesResponse = JsonConvert.SerializeObject(instaPrediction);
                                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelController), nameof(StartModelTraining), CONSTANTS.InstaMLTrainResponse + timeSeriesResponse.Replace(CONSTANTS.slash, string.Empty),
                                           string.IsNullOrEmpty(vdsDataModel.CorrelationId) ? default(Guid) : new Guid(vdsDataModel.CorrelationId) , string.Empty, string.Empty, vdsDataModel.ClientUId, vdsDataModel.DeliveryConstructUID);
                                        return GetFaultResponse(instaPrediction);
                                    }
                                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelController), nameof(StartModelTraining), CONSTANTS.InstaMLTrainResponse + timeSeriesResponse.Replace(CONSTANTS.slash, string.Empty),
                                       string.IsNullOrEmpty(vdsDataModel.CorrelationId) ? default(Guid) : new Guid(vdsDataModel.CorrelationId), string.Empty, string.Empty,vdsDataModel.ClientUId,vdsDataModel.DeliveryConstructUID);
                                    return Ok(instaPrediction);

                                case CONSTANTS.RefitModel:
                                    var refitModel = _instaModelService.RefitModel(vdsDataModel);
                                    refitModel.ClientUID = vdsDataModel.ClientUId;
                                    refitModel.DCID = vdsDataModel.DeliveryConstructUID;
                                    refitModel.CreatedByUser = vdsDataModel.CreatedByUser;
                                    timeSeriesResponse = JsonConvert.SerializeObject(refitModel);
                                    if (refitModel.Status == CONSTANTS.E)
                                    {
                                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelController), nameof(StartModelTraining), CONSTANTS.InstaMLTrainResponse + timeSeriesResponse.Replace(CONSTANTS.slash, string.Empty),
                                           string.IsNullOrEmpty(vdsDataModel.CorrelationId) ? default(Guid) : new Guid(vdsDataModel.CorrelationId), string.Empty, string.Empty, vdsDataModel.ClientUId, vdsDataModel.DeliveryConstructUID);
                                        return GetFaultResponse(refitModel);
                                    }
                                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelController), nameof(StartModelTraining), CONSTANTS.InstaMLTrainResponse + timeSeriesResponse.Replace(CONSTANTS.slash, string.Empty),
                                       string.IsNullOrEmpty(vdsDataModel.CorrelationId) ? default(Guid) : new Guid(vdsDataModel.CorrelationId), string.Empty, string.Empty, vdsDataModel.ClientUId, vdsDataModel.DeliveryConstructUID);
                                    return Ok(refitModel);


                                case CONSTANTS.ModelStatus:
                                    var modelStatus = _instaModelService.ModelStatus(vdsDataModel.InstaId, vdsDataModel.CorrelationId);
                                    timeSeriesResponse = JsonConvert.SerializeObject(modelStatus);
                                    if (instaModel.Status == CONSTANTS.E)
                                    {
                                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelController), nameof(StartModelTraining), CONSTANTS.InstaMLTrainResponse + timeSeriesResponse.Replace(CONSTANTS.slash, string.Empty),
                                            string.IsNullOrEmpty(vdsDataModel.CorrelationId) ? default(Guid) : new Guid(vdsDataModel.CorrelationId), string.Empty, string.Empty, vdsDataModel.ClientUId, vdsDataModel.DeliveryConstructUID);
                                        return GetFaultResponse(modelStatus);
                                    }
                                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelController), nameof(StartModelTraining), CONSTANTS.InstaMLTrainResponse + timeSeriesResponse.Replace(CONSTANTS.slash, string.Empty),
                                       string.IsNullOrEmpty(vdsDataModel.CorrelationId) ? default(Guid) : new Guid(vdsDataModel.CorrelationId), string.Empty, string.Empty, vdsDataModel.ClientUId, vdsDataModel.DeliveryConstructUID);
                                    return Ok(modelStatus);
                                case CONSTANTS.UpdateModel:
                                    var updateModel = _instaModelService.UpdateModel(vdsDataModel.CorrelationId, vdsDataModel.UseCaseName, vdsDataModel.UseCaseDescription);
                                    updateModel.InstaID = vdsDataModel.InstaId;
                                    timeSeriesResponse = JsonConvert.SerializeObject(updateModel);
                                    if (updateModel.Status == CONSTANTS.E)
                                    {
                                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelController), nameof(StartModelTraining), CONSTANTS.InstaMLTrainResponse + timeSeriesResponse.Replace(CONSTANTS.slash, string.Empty),
                                           string.IsNullOrEmpty(vdsDataModel.CorrelationId) ? default(Guid) : new Guid(vdsDataModel.CorrelationId), string.Empty, string.Empty, vdsDataModel.ClientUId, vdsDataModel.DeliveryConstructUID);
                                        updateModel.Message = CONSTANTS.UpdateModelError;
                                    }
                                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelController), nameof(StartModelTraining), CONSTANTS.InstaMLTrainResponse + timeSeriesResponse.Replace(CONSTANTS.slash, string.Empty),
                                       string.IsNullOrEmpty(vdsDataModel.CorrelationId) ? default(Guid) : new Guid(vdsDataModel.CorrelationId), string.Empty, string.Empty, vdsDataModel.ClientUId, vdsDataModel.DeliveryConstructUID);
                                    return Ok(updateModel);

                            }
                        }
                        else
                            return Accepted(CONSTANTS.ProblemTypeNull);
                    }
                    else
                    {
                        return GetFaultResponse(Resource.IngrainResx.EmptyData);
                    }
                }

            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InstaModelController), nameof(StartModelTraining), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message + "------" + ex.StackTrace);

            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelController), nameof(StartModelTraining), CONSTANTS.InstaMLTrainResponse + timeSeriesResponse.Replace(CONSTANTS.slash, string.Empty),
                string.IsNullOrEmpty(instaModel.CorrelationId) ? default(Guid) : new Guid(instaModel.CorrelationId), string.Empty, string.Empty, string.Empty, string.Empty);
            return Ok(instaModel);
        }
        private bool ValidateInput(VDSRegression vdsRegression)
        {
            bool isValidated = false;
            List<instaMLResponse> list = new List<instaMLResponse>();
            if (string.IsNullOrEmpty(vdsRegression.Source)
                                    || string.IsNullOrEmpty(vdsRegression.UseCaseID)
                                    || string.IsNullOrEmpty(vdsRegression.DCID)
                                    || string.IsNullOrEmpty(vdsRegression.ClientUID)
                                    || string.IsNullOrEmpty(vdsRegression.URL))
            {
                isValidated = true;
                instaMLResponse response = new instaMLResponse();
                response.CorrelationId = vdsRegression.ProblemTypeDetails[0].CorrelationId;
                response.InstaID = vdsRegression.ProblemTypeDetails[0].InstaID;
                response.Message = CONSTANTS.InputFieldsAreNull;
                response.Status = CONSTANTS.E;
                response.ErrorMessage = CONSTANTS.InputFieldsAreNull;
                list.Add(response);
                instaRegression.instaMLResponse = list;
            }
            return isValidated;
        }

        [HttpPost]
        [Route("api/GetInstaMLData")]
        public IActionResult GetInstaMLData(InstaMLData instaMLData)
        {
            string data = string.Empty;
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelController), nameof(GetInstaMLData), CONSTANTS.START, string.Empty, string.Empty,instaMLData.ClientUID,instaMLData.DCID);
            try
            {
                if (instaMLData.UseCaseID != null & !string.IsNullOrEmpty(instaMLData.UseCaseID))
                {
                    #region VALIDATIONS
                    CommonUtility.ValidateInputFormData(instaMLData.ClientUID, CONSTANTS.ClientUID, true);
                    CommonUtility.ValidateInputFormData(instaMLData.DCID, CONSTANTS.DCID, true);
                    CommonUtility.ValidateInputFormData(instaMLData.UseCaseID, CONSTANTS.UseCaseID, true);
                    CommonUtility.ValidateInputFormData(instaMLData.ProcessFlow, CONSTANTS.ProcessFlow, false);
                    CommonUtility.ValidateInputFormData(instaMLData.lastFitDate, "LastFitDate", false);
                    #endregion

                    data = _instaModelService.GetInstaMLData(instaMLData.UseCaseID);
                }
                else
                {
                    return GetFaultResponse(Resource.IngrainResx.InputFieldsAreNull);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InstaModelController), nameof(GetInstaMLData), ex.Message + $"   StackTrace = {ex.StackTrace}", ex, string.Empty, string.Empty,instaMLData.ClientUID,instaMLData.DCID);
                return GetFaultResponse(ex.Message);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelController), nameof(GetInstaMLData), CONSTANTS.END, string.Empty, string.Empty,instaMLData.ClientUID,instaMLData.DCID);
            return Ok(data);
        }
        #endregion
    }
}
