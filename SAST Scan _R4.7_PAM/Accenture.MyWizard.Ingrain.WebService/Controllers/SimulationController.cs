#region Copyright (c) 2019 Accenture . All rights reserved.

// <copyright company="Accenture">
// Copyright (c) 2019 Accenture.  All rights reserved.
// Reproduction or transmission in whole or in part, in any form or by any means, 
// electronic, mechanical or otherwise, is prohibited without the prior written 
// consent of the copyright owner.
// </copyright>

#endregion

#region SimulationController Information
/********************************************************************************************************\
Module Name     :   SimulationController
Project         :   Accenture.MyWizard.Ingrain.WebService.Controllers
Organisation    :   Accenture Technologies Ltd.
Created By      :   RaviA
Created Date    :   15-JUN-2020
Revision History :
Revision No             Initial             Modified Date           Description
1.0                     An                  15-JUN-2020             
\********************************************************************************************************/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
using Accenture.MyWizard.Ingrain.BusinessDomain.Services;
using Accenture.MyWizard.Shared.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Accenture.MyWizard.Ingrain.DataModels.MonteCarlo;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using Newtonsoft.Json.Linq;
using Microsoft.CodeAnalysis.Editing;
using Accenture.MyWizard.Ingrain.DataModels.Models;
using Accenture.MyWizard.Ingrain.BusinessDomain.Common;

namespace Accenture.MyWizard.Ingrain.WebService.Controllers
{
    public class SimulationController : MyWizardControllerBase
    {
        #region Members
        private static IMCSimulationService _mCSimulationService { get; set; }
        private readonly IOptions<IngrainAppSettings> appSettings;
        private IWebHostEnvironment _hostingEnvironment { get; set; }
        private IEncryptionDecryption _encryptionDecryption;
        private TemplateInfo templateInfo = null;
        private GenericFile genericFile = null;
        private GetInputInfo getInputInfo = null;
        SimulationPrediction simulationPrediction = null;
        WeeklyPrediction weeklyPrediction = null;
        whatIfPrediction whatIfPrediction = null;
        private CallBackErrorLog auditTrailLog;
        #endregion

        #region Constructor
        public SimulationController(IServiceProvider serviceProvider, IOptions<IngrainAppSettings> settings, IWebHostEnvironment environment)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(SimulationController), "SimulationControllerStart", "START", string.Empty, string.Empty, string.Empty, string.Empty);
            appSettings = settings;
            _mCSimulationService = serviceProvider.GetService<IMCSimulationService>();
            _hostingEnvironment = environment;
            _encryptionDecryption = serviceProvider.GetService<IEncryptionDecryption>();
            templateInfo = new TemplateInfo();
            getInputInfo = new GetInputInfo();
            simulationPrediction = new SimulationPrediction();
            genericFile = new GenericFile();
            whatIfPrediction = new whatIfPrediction();
            auditTrailLog = new CallBackErrorLog();
        }

        #endregion

        #region Methods
        [HttpPost]
        [Route("api/TemplateUpload")]
        public IActionResult TemplateUpload(IFormCollection formCollection)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(SimulationController), nameof(TemplateUpload), "START", string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                var fileCollection = HttpContext.Request.Form.Files;

                #region VALIDATION
                if (CommonUtility.ValidateFileUploaded(fileCollection))
                    return GetFaultResponse(Resource.IngrainResx.InValidFileName);

                if (!CommonUtility.GetValidUser(Convert.ToString(formCollection[CONSTANTS.CreatedByUser]).Trim()))
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);

                CommonUtility.ValidateInputFormData(Convert.ToString(formCollection[CONSTANTS.ClientUID]), CONSTANTS.ClientUID, true);
                CommonUtility.ValidateInputFormData(Convert.ToString(formCollection[CONSTANTS.DeliveryConstructUID]), CONSTANTS.DeliveryConstructUID, true);
                CommonUtility.ValidateInputFormData(Convert.ToString(formCollection[CONSTANTS.ProblemType]), CONSTANTS.ProblemType, false);
                CommonUtility.ValidateInputFormData(Convert.ToString(formCollection[CONSTANTS.TargetColumn]), CONSTANTS.TargetColumn, false);
                CommonUtility.ValidateInputFormData(Convert.ToString(formCollection[CONSTANTS.DeliveryTypeID]), CONSTANTS.DeliveryTypeID, true);
                CommonUtility.ValidateInputFormData(Convert.ToString(formCollection[CONSTANTS.DeliveryTypeName]), CONSTANTS.DeliveryTypeName, false);
                CommonUtility.ValidateInputFormData(Convert.ToString(formCollection[CONSTANTS.UseCaseName]), CONSTANTS.UseCaseName, false);
                CommonUtility.ValidateInputFormData(Convert.ToString(formCollection[CONSTANTS.UseCaseDescription]), CONSTANTS.UseCaseDescription, false);
                CommonUtility.ValidateInputFormData(Convert.ToString(formCollection[CONSTANTS.DeliveryTypeName]), CONSTANTS.DeliveryTypeName, false);
                #endregion

                string message = string.Empty;
                templateInfo = _mCSimulationService.LoadData(fileCollection, formCollection, out message);
                if (!string.IsNullOrEmpty(message))
                {
                    return Ok(templateInfo);
                }
                if (templateInfo.IsException)
                    return GetFaultResponse(templateInfo);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(SimulationController), nameof(TemplateUpload), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }           
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(SimulationController), nameof(TemplateUpload), "END", string.Empty, string.Empty, string.Empty, string.Empty);
            return Ok(templateInfo);
        }
        [HttpPost]
        [Route("api/GenericFileUpload")]
        public IActionResult GenericFileUpload(IFormCollection formCollection)
        {            
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(SimulationController), nameof(GenericFileUpload), "START", string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                var fileCollection = HttpContext.Request.Form.Files;

                #region VALIDATION
                if (CommonUtility.ValidateFileUploaded(fileCollection))
                    return GetFaultResponse(Resource.IngrainResx.InValidFileName);

                if (!CommonUtility.GetValidUser(Convert.ToString(formCollection[CONSTANTS.CreatedByUser]).Trim()))
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);

                CommonUtility.ValidateInputFormData(Convert.ToString(formCollection[CONSTANTS.ClientUID]), CONSTANTS.ClientUID, true);
                CommonUtility.ValidateInputFormData(Convert.ToString(formCollection[CONSTANTS.DeliveryConstructUID]), CONSTANTS.DeliveryConstructUID, true);
                CommonUtility.ValidateInputFormData(Convert.ToString(formCollection[CONSTANTS.ProblemType]), CONSTANTS.ProblemType, false);
                CommonUtility.ValidateInputFormData(Convert.ToString(formCollection[CONSTANTS.TargetColumn]), CONSTANTS.TargetColumn, false);
                CommonUtility.ValidateInputFormData(Convert.ToString(formCollection[CONSTANTS.DeliveryTypeID]), CONSTANTS.DeliveryTypeID, true);
                CommonUtility.ValidateInputFormData(Convert.ToString(formCollection[CONSTANTS.DeliveryTypeName]), CONSTANTS.DeliveryTypeName, false);
                CommonUtility.ValidateInputFormData(Convert.ToString(formCollection[CONSTANTS.UseCaseName]), CONSTANTS.UseCaseName, false);
                CommonUtility.ValidateInputFormData(Convert.ToString(formCollection[CONSTANTS.UseCaseDescription]), CONSTANTS.UseCaseDescription, false);
                CommonUtility.ValidateInputFormData(Convert.ToString(formCollection[CONSTANTS.DeliveryTypeName]), CONSTANTS.DeliveryTypeName, false);
                #endregion               

                string message = string.Empty;
                genericFile = _mCSimulationService.GenericLoadData(fileCollection, formCollection, out message);
                if (!string.IsNullOrEmpty(message))
                {
                    return Ok(genericFile);
                }
                if (genericFile.IsException)
                    return GetFaultResponse(genericFile);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(SimulationController), nameof(GenericFileUpload), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(SimulationController), nameof(TemplateUpload), "END", string.Empty, string.Empty, string.Empty, string.Empty);
            return Ok(genericFile);
        }

        [HttpPost]
        [Route("api/RRPUpload")]
        public IActionResult RRPUpload(IFormCollection formCollection)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(SimulationController), nameof(RRPUpload), CONSTANTS.START, string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                string message = string.Empty;

                #region VALIDATION
                if (!CommonUtility.GetValidUser(Convert.ToString(formCollection[CONSTANTS.CreatedByUser]).Trim()))
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);

                CommonUtility.ValidateInputFormData(Convert.ToString(formCollection[CONSTANTS.ClientUID]), CONSTANTS.ClientUID, true);
                CommonUtility.ValidateInputFormData(Convert.ToString(formCollection[CONSTANTS.DeliveryConstructUID]), CONSTANTS.DeliveryConstructUID, true);               
                #endregion   

                templateInfo = _mCSimulationService.RRPLoadData(formCollection, out message);
                if (!string.IsNullOrEmpty(message))
                {
                    return Ok(templateInfo);
                }
                if (templateInfo.IsException)
                    return GetFaultResponse(templateInfo);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(SimulationController), nameof(RRPUpload), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(SimulationController), nameof(RRPUpload), CONSTANTS.END, string.Empty, string.Empty, string.Empty, string.Empty);
            return Ok(templateInfo);
        }

        [HttpGet]
        [Route("api/GetGenericUpdate")]
        public IActionResult GetGenericUpdate(string TemplateID, string TargetColumn, string UniqueIdentifier, string UseCaseName, string UseCaseDescription)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(SimulationController), nameof(GetGenericUpdate), CONSTANTS.START, string.Empty, string.Empty, string.Empty, string.Empty);
            UpdateFeatures updateFeatures = new UpdateFeatures();
            try
            {
                if (string.IsNullOrEmpty(TargetColumn)
                    || string.IsNullOrEmpty(TemplateID)
                    || string.IsNullOrEmpty(UniqueIdentifier)
                    || string.IsNullOrEmpty(UseCaseName)
                    || string.IsNullOrEmpty(UseCaseDescription))
                {
                    updateFeatures.ErrorMessage = Resource.IngrainResx.InputFieldsAreNull;
                    updateFeatures.Status = false;
                    return Ok(updateFeatures);
                }
                if (TargetColumn == CONSTANTS.undefined
                    || TemplateID == CONSTANTS.undefined
                    || UniqueIdentifier == CONSTANTS.undefined
                    || UseCaseName == CONSTANTS.undefined
                    || UseCaseDescription == CONSTANTS.undefined)
                {
                    updateFeatures.ErrorMessage = CONSTANTS.InutFieldsUndefined;
                    updateFeatures.Status = false;
                    return Ok(updateFeatures);
                }
                updateFeatures = _mCSimulationService.GenericUpdate(TemplateID, TargetColumn, UniqueIdentifier, UseCaseName, UseCaseDescription);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(SimulationController), nameof(GetGenericUpdate), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(SimulationController), nameof(GetGenericUpdate), CONSTANTS.END, string.Empty, string.Empty, string.Empty, string.Empty);
            return Ok(updateFeatures);
        }

        [HttpGet]
        [Route("api/GetTemplateData")]
        public IActionResult GetTemplateData(string ClientUID, string DeliveryConstructUID, string UserId, string TemplateID, string UseCaseName, string ProblemType)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(SimulationController), nameof(GetTemplateData), "START", string.Empty, string.Empty,ClientUID, DeliveryConstructUID);
            try
            {
                if (string.IsNullOrEmpty(ClientUID)
                    || string.IsNullOrEmpty(DeliveryConstructUID)
                    || string.IsNullOrEmpty(UserId)
                    || string.IsNullOrEmpty(UseCaseName)
                    || string.IsNullOrEmpty(ProblemType))
                {
                    return GetSuccessWithMessageResponse(Resource.IngrainResx.InputFieldsAreNull);
                }
                if (ClientUID.Contains(CONSTANTS.undefined)
                    || DeliveryConstructUID.Contains(CONSTANTS.undefined)
                    || UseCaseName.Contains(CONSTANTS.undefined)
                    || UserId.Contains(CONSTANTS.undefined)
                    || ProblemType.Contains(CONSTANTS.undefined))
                {
                    return GetSuccessWithMessageResponse(CONSTANTS.InutFieldsUndefined);
                }
                if (ClientUID == CONSTANTS.Null
                    || DeliveryConstructUID == CONSTANTS.Null
                    || UseCaseName == CONSTANTS.Null
                    || UserId == CONSTANTS.Null
                    || ProblemType == CONSTANTS.Null)
                {
                    return GetSuccessWithMessageResponse(CONSTANTS.InputFieldsAreNull);
                }
                if (!CommonUtility.GetValidUser(UserId))
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);

                getInputInfo = _mCSimulationService.GetTemplateData(ClientUID, DeliveryConstructUID, UserId, TemplateID, UseCaseName, ProblemType);
                if (getInputInfo == null)
                    return GetSuccessWithMessageResponse("No Records found");
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(SimulationController), nameof(GetTemplateData), ex.Message, ex, string.Empty, string.Empty,ClientUID,DeliveryConstructUID);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(SimulationController), nameof(GetTemplateData), "END", string.Empty, string.Empty,ClientUID,DeliveryConstructUID);
            return Ok(getInputInfo);
        }

        [HttpGet]
        [Route("api/UpdateVSName")]
        public IActionResult UpdateVSName(string ClientUID, string DeliveryConstructUID, string UseCaseID, string UserId, string TemplateId, string simulationId, string NewName)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(SimulationController), nameof(UpdateVSName), "START", string.Empty, string.Empty,ClientUID,DeliveryConstructUID);
            VersionList versionList = new VersionList();
            try
            {
                if (TemplateId == CONSTANTS.undefined || ClientUID == CONSTANTS.undefined || NewName == CONSTANTS.undefined || UseCaseID == CONSTANTS.undefined)
                    return GetSuccessWithMessageResponse(Resource.IngrainResx.InputFieldsAreNull);
                if (!CommonUtility.GetValidUser(UserId))
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);
                if (string.IsNullOrEmpty(TemplateId) || string.IsNullOrEmpty(ClientUID) || string.IsNullOrEmpty(NewName))
                    return GetSuccessWithMessageResponse(Resource.IngrainResx.InputEmpty);
                else
                {
                    bool isNameExist = false;
                    bool isSimulated = false;
                    if (!string.IsNullOrEmpty(simulationId) && simulationId != CONSTANTS.Null)
                        isSimulated = true;
                    if (isSimulated)
                        isNameExist = _mCSimulationService.IsVSNameExist(UseCaseID, NewName, CONSTANTS.SimulationResults, TemplateId);
                    else
                        isNameExist = _mCSimulationService.IsVSNameExist(UseCaseID, NewName, CONSTANTS.TemplateData, null);
                    if (!isNameExist)
                    {
                        _mCSimulationService.updateVersionSimulationName(ClientUID, DeliveryConstructUID, UseCaseID, UserId, TemplateId, simulationId, NewName);
                        versionList.TemplateVersions = _mCSimulationService.InputVersionList(ClientUID, DeliveryConstructUID, UseCaseID);
                        if (isSimulated)
                        {
                            versionList.SimulationVersions = _mCSimulationService.SimulationVersionList(ClientUID, DeliveryConstructUID, UseCaseID, TemplateId);
                            versionList.SimulationVersion = NewName;
                            versionList.SimulationID = simulationId;
                            versionList.TemplateID = TemplateId;
                        }
                        else
                        {
                            versionList.TemplateID = TemplateId;
                            versionList.TemplateVersion = NewName;
                        }
                    }
                    else
                        return GetSuccessWithMessageResponse(CONSTANTS.VersionNameExists);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(SimulationController), nameof(UpdateVSName), ex.Message, ex, string.Empty, string.Empty,ClientUID,DeliveryConstructUID);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(SimulationController), nameof(UpdateVSName), "END", string.Empty, string.Empty,ClientUID,DeliveryConstructUID);
            return Ok(versionList);
        }

        [HttpPost]
        [Route("api/UpdateTemplateInfo")]
        public IActionResult UpdateTemplateInfo([FromBody] dynamic requestBody)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(SimulationController), nameof(UpdateTemplateInfo), "START", string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                string templateData = Convert.ToString(requestBody);
                if (!string.IsNullOrEmpty(templateData))
                {
                    dynamic data = Newtonsoft.Json.JsonConvert.DeserializeObject(templateData);
                    var columns = JObject.Parse(templateData);

                    #region VALIDATIONS
                    if (data.ClientUID == CONSTANTS.undefined || data.DeliveryConstructUID == CONSTANTS.undefined || data.UserId == CONSTANTS.undefined || data.TemplateId == CONSTANTS.undefined || data.UseCaseName == CONSTANTS.undefined || data.TargetColumn == CONSTANTS.undefined)
                        return GetSuccessWithMessageResponse(Resource.IngrainResx.InputFieldsAreNull);
                    if (data.ClientUID == CONSTANTS.Null || data.DeliveryConstructUID == CONSTANTS.Null || data.UserId == CONSTANTS.Null || data.TemplateId == CONSTANTS.Null || data.UseCaseName == CONSTANTS.Null || data.TargeColumn == CONSTANTS.Null)
                        return GetSuccessWithMessageResponse(Resource.IngrainResx.InputEmpty);
                    if (!CommonUtility.GetValidUser(Convert.ToString(data.UserId)))
                        return GetFaultResponse(Resource.IngrainResx.InValidUser);

                    CommonUtility.ValidateInputFormData(Convert.ToString(columns[CONSTANTS.ClientUID]), CONSTANTS.ClientUID, true);
                    CommonUtility.ValidateInputFormData(Convert.ToString(columns[CONSTANTS.DeliveryConstructUID]), CONSTANTS.DeliveryConstructUID, true);                    
                    CommonUtility.ValidateInputFormData(Convert.ToString(columns[CONSTANTS.TemplateId]), CONSTANTS.TemplateId, true);
                    CommonUtility.ValidateInputFormData(Convert.ToString(columns[CONSTANTS.UseCaseName]), CONSTANTS.UseCaseName, false);
                    CommonUtility.ValidateInputFormData(Convert.ToString(columns[CONSTANTS.ProblemType]), CONSTANTS.ProblemType, false);
                    CommonUtility.ValidateInputFormData(Convert.ToString(columns[CONSTANTS.TargetColumn]), CONSTANTS.TargetColumn, false);
                    #endregion

                    if (data.ClientUID != null & data.DeliveryConstructUID != null & data.UserId != null & data.TemplateId != null & data.ProblemType != null & data.UseCaseName != null & data.TargetColumn != null)
                    {
                        string ClientUID = columns["ClientUID"].ToString();
                        string DeliveryConstructUID = columns["DeliveryConstructUID"].ToString();
                        string UserId = columns["UserId"].ToString();
                        string TemplateID = columns["TemplateId"].ToString();
                        string UseCaseName = columns["UseCaseName"].ToString();
                        string ProblemType = columns["ProblemType"].ToString();
                        string TargetColumn = columns["TargetColumn"].ToString();
                        if (ProblemType == CONSTANTS.ADSP)
                        {
                            if (data.SelectedCurrentRelease == CONSTANTS.undefined || data.SelectedCurrentRelease == CONSTANTS.Null || data.SelectedCurrentRelease == null)
                                return GetSuccessWithMessageResponse(Resource.IngrainResx.InputEmpty);
                        }
                        _mCSimulationService.UpdateTemplateInfo(data, TemplateID);
                        getInputInfo = _mCSimulationService.GetTemplateData(ClientUID, DeliveryConstructUID, UserId, TemplateID, UseCaseName, ProblemType);

                        if (getInputInfo == null)
                            return Ok("No Data found");
                    }
                    else
                    {
                        return GetFaultResponse(Resource.IngrainResx.InputsEmpty);
                    }
                }

            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(SimulationController), nameof(UpdateTemplateInfo), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty) ;
                return GetFaultResponse(ex.Message);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(SimulationController), nameof(UpdateTemplateInfo), "END", string.Empty, string.Empty, string.Empty, string.Empty);
            return Ok(getInputInfo);
        }

        [HttpPost]
        [Route("api/CloneTemplateInfo")]
        public IActionResult CloneTemplateInfo([FromBody] dynamic requestBody)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(SimulationController), nameof(CloneTemplateInfo), "START", string.Empty, string.Empty, string.Empty, string.Empty);
            CloneTemplateData cloneTemplateData = new CloneTemplateData();
            try
            {
                string templateData = Convert.ToString(requestBody);
                if (!string.IsNullOrEmpty(templateData))
                {
                    dynamic data = Newtonsoft.Json.JsonConvert.DeserializeObject(templateData);
                    var columns = JObject.Parse(templateData);

                    #region VALIDATIONS
                    if (data.ClientUID == CONSTANTS.undefined || data.DeliveryConstructUID == CONSTANTS.undefined || data.UserId == CONSTANTS.undefined || data.TemplateID == CONSTANTS.undefined || data.UseCaseID == CONSTANTS.undefined || data.NewName == CONSTANTS.undefined)
                        return GetSuccessWithMessageResponse(Resource.IngrainResx.InputFieldsAreNull);
                    if (data.ClientUID == CONSTANTS.Null || data.DeliveryConstructUID == CONSTANTS.Null || data.UserId == CONSTANTS.Null || data.TemplateID == CONSTANTS.Null || data.UseCaseID == CONSTANTS.Null || data.NewName == CONSTANTS.Null)
                        return GetSuccessWithMessageResponse(Resource.IngrainResx.InputEmpty);
                    if (!CommonUtility.GetValidUser(Convert.ToString(data.UserId)))
                        return GetFaultResponse(Resource.IngrainResx.InValidUser);

                    CommonUtility.ValidateInputFormData(Convert.ToString(columns[CONSTANTS.UseCaseID]), CONSTANTS.UseCaseID, true);
                    CommonUtility.ValidateInputFormData(Convert.ToString(columns[CONSTANTS.NewName]), CONSTANTS.NewName, false);
                    CommonUtility.ValidateInputFormData(Convert.ToString(columns[CONSTANTS.ClientUID]), CONSTANTS.ClientUID, true);
                    CommonUtility.ValidateInputFormData(Convert.ToString(columns[CONSTANTS.DeliveryConstructUID]), CONSTANTS.DeliveryConstructUID, true);                   
                    CommonUtility.ValidateInputFormData(Convert.ToString(columns[CONSTANTS.TemplateID]), CONSTANTS.TemplateID, true);
                    CommonUtility.ValidateInputFormData(Convert.ToString(columns[CONSTANTS.UseCaseName]), CONSTANTS.UseCaseName, false);
                    CommonUtility.ValidateInputFormData(Convert.ToString(columns[CONSTANTS.ProblemType]), CONSTANTS.ProblemType, false);
                    CommonUtility.ValidateInputFormData(Convert.ToString(columns[CONSTANTS.TargetColumn]), CONSTANTS.TargetColumn, false);                   
                    #endregion
                    if (data.ClientUID != null & data.DeliveryConstructUID != null & data.UserId != null & data.TemplateID != null & data.UseCaseID != null & data.NewName != null)
                    {
                        string UseCaseID = columns["UseCaseID"].ToString();
                        string NewName = columns["NewName"].ToString();
                        bool isNameExist = _mCSimulationService.IsVSNameExist(UseCaseID, NewName.Trim(), CONSTANTS.TemplateData, null);
                        if (!isNameExist)
                        {
                            string ClientUID = columns["ClientUID"].ToString();
                            string DeliveryConstructUID = columns["DeliveryConstructUID"].ToString();
                            string UserId = columns["UserId"].ToString();
                            string TemplateID = columns["TemplateID"].ToString();
                            string UseCaseName = columns["UseCaseName"].ToString();
                            string ProblemType = columns["ProblemType"].ToString();
                            string TargetColumn = null;
                            JObject features = JObject.Parse(columns["Features"].ToString());
                            if (ProblemType.Trim() == CONSTANTS.Generic)
                                TargetColumn = columns["TargetColumn"].ToString();
                            cloneTemplateData.TemplateID = _mCSimulationService.CloneTemplateInfo(ProblemType, UserId, TemplateID, NewName, features, TargetColumn);
                            if (string.IsNullOrEmpty(cloneTemplateData.TemplateID))
                            {
                                cloneTemplateData.ErrorMessage = CONSTANTS.Error;
                                cloneTemplateData.Status = CONSTANTS.E;
                            }
                            else
                            {
                                cloneTemplateData.TemplateVersion = NewName;
                                cloneTemplateData.TemplateData = _mCSimulationService.GetTemplateData(ClientUID, DeliveryConstructUID, UserId, cloneTemplateData.TemplateID, UseCaseName, ProblemType);
                                cloneTemplateData.Message = CONSTANTS.Success;
                                cloneTemplateData.Status = CONSTANTS.C;
                            }
                        }
                        else
                            return GetSuccessWithMessageResponse(CONSTANTS.VersionNameExists);

                    }
                    else
                    {
                        return GetFaultResponse(Resource.IngrainResx.InputsEmpty);
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(SimulationController), nameof(CloneTemplateInfo), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(SimulationController), nameof(CloneTemplateInfo), "END", string.Empty, string.Empty, string.Empty, string.Empty);
            return Ok(cloneTemplateData);
        }

        [HttpPost]
        [Route("api/CloneSimulation")]
        public IActionResult CloneSimulation([FromBody] dynamic requestBody)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(SimulationController), nameof(CloneSimulation), "START", string.Empty, string.Empty, string.Empty, string.Empty);
            CloneSimulation cloneSimulation = new CloneSimulation();
            try
            {
                string templateData = Convert.ToString(requestBody);
                if (!string.IsNullOrEmpty(templateData))
                {
                    dynamic data = Newtonsoft.Json.JsonConvert.DeserializeObject(templateData);
                    var columns = JObject.Parse(templateData);

                    #region VALIDATIONS
                    if (data.ClientUID == CONSTANTS.undefined || data.DeliveryConstructUID == CONSTANTS.undefined || data.UserId == CONSTANTS.undefined || data.TemplateID == CONSTANTS.undefined || data.UseCaseID == CONSTANTS.undefined || data.NewName == CONSTANTS.undefined)
                        return GetSuccessWithMessageResponse(Resource.IngrainResx.InputFieldsAreNull);
                    if (data.ClientUID == CONSTANTS.Null || data.DeliveryConstructUID == CONSTANTS.Null || data.UserId == CONSTANTS.Null || data.TemplateID == CONSTANTS.Null || data.UseCaseID == CONSTANTS.Null || data.NewName == CONSTANTS.Null)
                        return GetSuccessWithMessageResponse(Resource.IngrainResx.InputEmpty);
                    if (!CommonUtility.GetValidUser(Convert.ToString(data.UserId)))
                        return GetFaultResponse(Resource.IngrainResx.InValidUser);

                    CommonUtility.ValidateInputFormData(Convert.ToString(columns[CONSTANTS.UseCaseID]), CONSTANTS.UseCaseID, true);
                    CommonUtility.ValidateInputFormData(Convert.ToString(columns[CONSTANTS.NewName]), CONSTANTS.NewName, false);
                    CommonUtility.ValidateInputFormData(Convert.ToString(columns[CONSTANTS.TemplateID]), CONSTANTS.TemplateID, true);
                    CommonUtility.ValidateInputFormData(Convert.ToString(columns[CONSTANTS.ClientUID]), CONSTANTS.ClientUID, true);
                    CommonUtility.ValidateInputFormData(Convert.ToString(columns[CONSTANTS.DeliveryConstructUID]), CONSTANTS.DeliveryConstructUID, true);
                    CommonUtility.ValidateInputFormData(Convert.ToString(columns[CONSTANTS.SimulationID]), CONSTANTS.SimulationID, true);                    
                    CommonUtility.ValidateInputFormData(Convert.ToString(columns[CONSTANTS.ProblemType]), CONSTANTS.ProblemType, false);
                    CommonUtility.ValidateInputFormData(Convert.ToString(columns[CONSTANTS.TargetColumn]), CONSTANTS.TargetColumn, false);
                    #endregion

                    if (data.ClientUID != null & data.DeliveryConstructUID != null & data.UserId != null & data.TemplateID != null & data.UseCaseID != null & data.NewName != null)
                    {
                        string UseCaseID = columns["UseCaseID"].ToString();
                        string NewName = columns["NewName"].ToString();
                        string TemplateID = columns["TemplateID"].ToString();
                        bool isNameExist = _mCSimulationService.IsVSNameExist(UseCaseID, NewName, CONSTANTS.SimulationResults, TemplateID);
                        if (!isNameExist)
                        {
                            string ClientUID = columns["ClientUID"].ToString();
                            string DeliveryConstructUID = columns["DeliveryConstructUID"].ToString();
                            string UserId = columns["UserId"].ToString();
                            string SimulationID = columns["SimulationID"].ToString();
                            string ProblemType = columns["ProblemType"].ToString();
                            string TargetColumn = null;
                            JObject inputs = JObject.Parse(columns["inputs"].ToString());
                            if (ProblemType == CONSTANTS.Generic)
                                TargetColumn = columns["TargetColumn"].ToString();
                            string newTemplateID = string.Empty;
                            cloneSimulation.SimulationID = _mCSimulationService.CloneSimulation(ProblemType, UserId, TemplateID, SimulationID, NewName, inputs, TargetColumn);
                            if (string.IsNullOrEmpty(cloneSimulation.SimulationID))
                            {
                                cloneSimulation.ErrorMessage = CONSTANTS.Updatedfor + CONSTANTS.Failed;
                                cloneSimulation.Status = CONSTANTS.E;
                            }
                            else
                            {
                                cloneSimulation.SimulationVersion = NewName;
                                cloneSimulation.TemplateID = TemplateID;
                                cloneSimulation.SimulationData = _mCSimulationService.GetSimulationData(ClientUID, DeliveryConstructUID, UserId, TemplateID, cloneSimulation.SimulationID, UseCaseID, ProblemType);
                                cloneSimulation.Message = CONSTANTS.Success;
                                cloneSimulation.Status = CONSTANTS.C;
                            }
                        }
                        else
                            return GetSuccessWithMessageResponse(CONSTANTS.VersionNameExists);

                    }
                    else
                    {
                        return GetFaultResponse(Resource.IngrainResx.InputsEmpty);
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(SimulationController), nameof(CloneSimulation), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(SimulationController), nameof(CloneSimulation), "END", string.Empty, string.Empty, string.Empty, string.Empty);
            return Ok(cloneSimulation);
        }

        [HttpGet]
        [Route("api/RunSimulation")]
        public IActionResult RunSimulation(string TemplateID, string SimulationID, string ProblemType, bool IsPythonCall, string ClientUID, string DCUID, string UserId, string UseCaseID, string UseCaseName, bool RRPFlag, string SelectedCurrentRelease, string InputSelection)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(SimulationController), nameof(RunSimulation), CONSTANTS.START,string.Empty,string.Empty,ClientUID,DCUID);
            try
            {
                if (TemplateID == CONSTANTS.undefined || SimulationID == CONSTANTS.undefined || ProblemType == CONSTANTS.undefined || UseCaseID == CONSTANTS.undefined)
                    return GetSuccessWithMessageResponse(Resource.IngrainResx.InputFieldsAreNull);
                if (string.IsNullOrEmpty(TemplateID) || string.IsNullOrEmpty(ProblemType) || string.IsNullOrEmpty(UseCaseID))
                    return GetSuccessWithMessageResponse(Resource.IngrainResx.InputEmpty);
                if (!CommonUtility.GetValidUser(UserId))
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);
                JObject selectionUpdate = new JObject();
                if (!string.IsNullOrEmpty(InputSelection) && InputSelection != "null" && InputSelection != null && InputSelection != CONSTANTS.undefined)
                    selectionUpdate = JObject.Parse(InputSelection);
                simulationPrediction = _mCSimulationService.RunSimulation(TemplateID, SimulationID, ProblemType, IsPythonCall, ClientUID, DCUID, UserId, UseCaseID, UseCaseName, RRPFlag, SelectedCurrentRelease, selectionUpdate);
                if (simulationPrediction.Status == CONSTANTS.E)
                {
                    return GetFaultResponse(simulationPrediction);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(SimulationController), nameof(RunSimulation), ex.Message, ex, string.Empty, string.Empty,ClientUID,DCUID);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(SimulationController), nameof(RunSimulation), CONSTANTS.END, string.Empty, string.Empty,ClientUID,DCUID);
            return Ok(simulationPrediction);
        }

        [HttpPost]
        [Route("api/UpdateSimulation")]
        public IActionResult UpdateSimulation([FromBody] dynamic requestBody)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(SimulationController), nameof(UpdateSimulation), CONSTANTS.START, string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                string message = null;
                string templateData = Convert.ToString(requestBody);
                if (!string.IsNullOrEmpty(templateData))
                {
                    dynamic data = Newtonsoft.Json.JsonConvert.DeserializeObject(templateData);

                    #region VALIDATIONS
                    var columns = JObject.Parse(templateData);
                    if (Convert.ToString(data.TemplateID) == CONSTANTS.undefined || Convert.ToString(data.SimulationID) == CONSTANTS.undefined || Convert.ToString(data.UseCaseID) == CONSTANTS.undefined || Convert.ToString(data.inputs.Influencers) == CONSTANTS.undefined || Convert.ToString(data.inputs.TargetCertainty) == CONSTANTS.undefined || Convert.ToString(data.inputs.TargetVariable) == CONSTANTS.undefined || Convert.ToString(data.inputs.IncrementFlags) == CONSTANTS.undefined || Convert.ToString(data.inputs.PercentChange) == CONSTANTS.undefined)
                        return GetSuccessWithMessageResponse(Resource.IngrainResx.InputFieldsAreNull);
                    if (Convert.ToString(data.TemplateID) == CONSTANTS.Null || Convert.ToString(data.SimulationID) == CONSTANTS.Null || Convert.ToString(data.UseCaseID) == CONSTANTS.Null || Convert.ToString(data.inputs.Influencers) == CONSTANTS.Null || Convert.ToString(data.inputs.TargetCertainty) == CONSTANTS.Null || Convert.ToString(data.inputs.TargetVariable) == CONSTANTS.Null || Convert.ToString(data.inputs.IncrementFlags) == CONSTANTS.Null || Convert.ToString(data.inputs.PercentChange) == CONSTANTS.Null)
                        return GetSuccessWithMessageResponse(Resource.IngrainResx.InputEmpty);

                    CommonUtility.ValidateInputFormData(Convert.ToString(columns[CONSTANTS.TemplateID]), CONSTANTS.TemplateID, true);
                    CommonUtility.ValidateInputFormData(Convert.ToString(columns[CONSTANTS.SimulationID]), CONSTANTS.SimulationID, true);
                    CommonUtility.ValidateInputFormData(Convert.ToString(columns[CONSTANTS.UseCaseID]), CONSTANTS.UseCaseID, true);
                    CommonUtility.ValidateInputFormData(Convert.ToString(columns[CONSTANTS.SelectedCurrentRelease]), CONSTANTS.SelectedCurrentRelease, false);
                    CommonUtility.ValidateInputFormData(Convert.ToString(columns[CONSTANTS.Observation]), CONSTANTS.Observation, false);
                    #endregion

                    if (data.TemplateID != null & data.SimulationID != null & data.UseCaseID != null & data.inputs.Influencers != null & data.inputs.TargetCertainty != null & data.inputs.TargetVariable != null & data.inputs.IncrementFlags != null & data.inputs.PercentChange != null)
                    {
                        string TemplateID = columns["TemplateID"].ToString();
                        string SimulationID = columns["SimulationID"].ToString();
                        string UseCaseID = columns["UseCaseID"].ToString();
                        string SelectedCurrentRelease = columns["SelectedCurrentRelease"].ToString();
                        JObject Influencers = JObject.Parse(columns["inputs"]["Influencers"].ToString());
                        double TargetCertainty = Convert.ToDouble(columns["inputs"]["TargetCertainty"]);
                        double TargetVariable = Convert.ToDouble(columns["inputs"]["TargetVariable"]);
                        string Observation = columns["Observation"].ToString();
                        JObject IncrementFlags = JObject.Parse(columns["inputs"]["IncrementFlags"].ToString());
                        JObject PercentChange = JObject.Parse(columns["inputs"]["PercentChange"].ToString());
                        message = _mCSimulationService.UpdateSimulation(TemplateID, SimulationID, UseCaseID, Influencers, TargetCertainty, TargetVariable, Observation, IncrementFlags, PercentChange, SelectedCurrentRelease);
                        if (message == CONSTANTS.Success)
                            return GetSuccessWithMessageResponse(CONSTANTS.Success);
                        else
                            return GetFaultResponse(message);
                    }
                    else
                        return GetSuccessWithMessageResponse(Resource.IngrainResx.InputEmpty);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(SimulationController), nameof(UpdateSimulation), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(SimulationController), nameof(UpdateSimulation), CONSTANTS.END, string.Empty, string.Empty, string.Empty, string.Empty);
            return GetSuccessWithMessageResponse(CONSTANTS.Success);
        }

        [HttpPost]
        [Route("api/UpdateSelection")]
        public IActionResult UpdateSelection([FromBody] dynamic requestBody)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(SimulationController), nameof(UpdateSelection), CONSTANTS.START, string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                string phaseData = Convert.ToString(requestBody);
                if (!string.IsNullOrEmpty(phaseData))
                {
                    dynamic data = Newtonsoft.Json.JsonConvert.DeserializeObject(phaseData);
                    var columns = JObject.Parse(phaseData);

                    #region VALIDATIONS
                    if (Convert.ToString(data.TemplateID) == CONSTANTS.undefined || data.UserId == CONSTANTS.undefined || Convert.ToString(data.UseCaseID) == CONSTANTS.undefined || Convert.ToString(data.ColSelection) == CONSTANTS.undefined || Convert.ToString(data.ClientUID) == CONSTANTS.undefined || Convert.ToString(data.DeliveryConstructUID) == CONSTANTS.undefined || Convert.ToString(data.UseCaseName) == CONSTANTS.undefined)
                        return GetSuccessWithMessageResponse(Resource.IngrainResx.InputFieldsAreNull);
                    if (Convert.ToString(data.TemplateID) == CONSTANTS.Null || Convert.ToString(data.UserId) == CONSTANTS.Null || Convert.ToString(data.UseCaseID) == CONSTANTS.Null || Convert.ToString(data.ColSelection) == CONSTANTS.Null || Convert.ToString(data.ClientUID) == CONSTANTS.Null || Convert.ToString(data.DeliveryConstructUID) == CONSTANTS.Null || Convert.ToString(data.UseCaseName) == CONSTANTS.Null)
                        return GetSuccessWithMessageResponse(Resource.IngrainResx.InputEmpty);
                    if (!CommonUtility.GetValidUser(Convert.ToString(data.UserId)))
                        return GetFaultResponse(Resource.IngrainResx.InValidUser);

                    CommonUtility.ValidateInputFormData(Convert.ToString(columns[CONSTANTS.TemplateID]), CONSTANTS.TemplateID, true);
                    CommonUtility.ValidateInputFormData(Convert.ToString(columns[CONSTANTS.UseCaseID]), CONSTANTS.UseCaseID, true);
                    CommonUtility.ValidateInputFormData(Convert.ToString(columns[CONSTANTS.ColSelection]), CONSTANTS.ColSelection, false);
                    CommonUtility.ValidateInputFormData(Convert.ToString(columns[CONSTANTS.ClientUID]), CONSTANTS.ClientUID, true);
                    CommonUtility.ValidateInputFormData(Convert.ToString(columns[CONSTANTS.DeliveryConstructUID]), CONSTANTS.DeliveryConstructUID, true);
                    CommonUtility.ValidateInputFormData(Convert.ToString(columns[CONSTANTS.UseCaseName]), CONSTANTS.UseCaseName, false);
                    #endregion
                    if (data.TemplateID != null & data.UserId != null & data.UseCaseID != null & data.ColSelection != null & data.ClientUID != null & data.DeliveryConstructUID != null & data.UseCaseName != null)
                    {
                        string TemplateID = columns["TemplateID"].ToString();
                        string UserId = columns["UserId"].ToString();
                        string UseCaseID = columns["UseCaseID"].ToString();
                        string ColSelection = columns["ColSelection"].ToString();

                        string ClientUID = columns["ClientUID"].ToString();
                        string DeliveryConstructUID = columns["DeliveryConstructUID"].ToString();
                        string UseCaseName = columns["UseCaseName"].ToString();
                        JObject selectionUpdate = null;
                        if (ColSelection == "Influencer")
                        {
                            if (Convert.ToString(data.MainSelection) == CONSTANTS.undefined || Convert.ToString(data.MainSelection) == CONSTANTS.Null || data.MainSelection == null)
                                return GetSuccessWithMessageResponse(Resource.IngrainResx.InputFieldsAreNull);
                            else
                                selectionUpdate = JObject.Parse(columns["MainSelection"].ToString());
                        }
                        else if (ColSelection == "Phase")
                        {
                            if (Convert.ToString(data.InputSelection) == CONSTANTS.undefined || Convert.ToString(data.InputSelection) == CONSTANTS.Null || data.InputSelection == null)
                                return GetSuccessWithMessageResponse(Resource.IngrainResx.InputFieldsAreNull);
                            else
                                selectionUpdate = JObject.Parse(columns["InputSelection"].ToString());
                        }
                        _mCSimulationService.UpdateSelection(TemplateID, UserId, UseCaseID, selectionUpdate, ClientUID, DeliveryConstructUID, UseCaseName, ColSelection);
                        return GetSuccessWithMessageResponse(CONSTANTS.Success);
                    }
                }
                else
                    return GetSuccessWithMessageResponse(Resource.IngrainResx.InputFieldsAreNull);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(SimulationController), nameof(UpdateSelection), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(SimulationController), nameof(UpdateSelection), CONSTANTS.END, string.Empty, string.Empty, string.Empty, string.Empty);
            return GetSuccessWithMessageResponse(CONSTANTS.Success);
        }

        [HttpPost]
        [Route("api/WhatIfAnalysis")]
        public IActionResult WhatIfAnalysis([FromBody] dynamic requestBody)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(SimulationController), nameof(WhatIfAnalysis), CONSTANTS.START, string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                string templateData = Convert.ToString(requestBody);
                if (!string.IsNullOrEmpty(templateData))
                {
                    dynamic data = Newtonsoft.Json.JsonConvert.DeserializeObject(templateData);
                    var columns = JObject.Parse(templateData);

                    #region VALIDATIONS
                    if (Convert.ToString(data.TemplateID) == CONSTANTS.undefined || Convert.ToString(data.SimulationID) == CONSTANTS.undefined || Convert.ToString(data.inputs) == CONSTANTS.undefined)
                        return GetSuccessWithMessageResponse(Resource.IngrainResx.InputFieldsAreNull);
                    if (Convert.ToString(data.TemplateID) == CONSTANTS.Null || Convert.ToString(data.SimulationID) == CONSTANTS.Null || Convert.ToString(data.inputs) == CONSTANTS.Null)
                        return GetSuccessWithMessageResponse(Resource.IngrainResx.InputEmpty);

                    CommonUtility.ValidateInputFormData(Convert.ToString(columns[CONSTANTS.TemplateID]), CONSTANTS.TemplateID, true);
                    CommonUtility.ValidateInputFormData(Convert.ToString(columns[CONSTANTS.SimulationID]), CONSTANTS.SimulationID, true);
                    #endregion
                    if (data.TemplateID != null & data.SimulationID != null & data.inputs != null)
                    {
                        string TemplateID = columns["TemplateID"].ToString();
                        string SimulationID = columns["SimulationID"].ToString();
                        JObject inputs = JObject.Parse(columns["inputs"].ToString());
                        whatIfPrediction = _mCSimulationService.GetwhatIfData(TemplateID, SimulationID, inputs);
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(SimulationController), nameof(WhatIfAnalysis), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(SimulationController), nameof(WhatIfAnalysis), CONSTANTS.END, string.Empty, string.Empty, string.Empty, string.Empty);
            return Ok(whatIfPrediction);
        }

        [HttpGet]
        [Route("api/GetUseCaseData")]
        public IActionResult GetUseCaseData(string ClientUID, string DCUID, string DeliveryTypeName, string UserID)
        {
            GetAppService();
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(SimulationController), nameof(GetUseCaseData), CONSTANTS.START, string.Empty, string.Empty, ClientUID,DCUID);
            try
            {
                auditTrailLog.ClientId = ClientUID;
                auditTrailLog.DCID = DCUID;
                auditTrailLog.CreatedBy = UserID;
                auditTrailLog.ProcessName = CONSTANTS.Other;
                auditTrailLog.FeatureName = CONSTANTS.SimulationAnalytics;
                auditTrailLog.UsageType = CONSTANTS.AssetUsage;
                auditTrailLog.ApplicationName = CONSTANTS.VDS_SI;
                CommonUtility.AuditTrailLog(auditTrailLog, appSettings);
                if (string.IsNullOrEmpty(ClientUID)
                   || string.IsNullOrEmpty(DCUID)
                   || string.IsNullOrEmpty(DeliveryTypeName)
                   || string.IsNullOrEmpty(UserID))
                {
                    return GetSuccessWithMessageResponse(Resource.IngrainResx.InputFieldsAreNull);
                }
                if (ClientUID.Contains(CONSTANTS.undefined)
                    || DCUID.Contains(CONSTANTS.undefined)
                    || DeliveryTypeName.Contains(CONSTANTS.undefined)
                    || UserID.Contains(CONSTANTS.undefined))
                {
                    return GetSuccessWithMessageResponse(CONSTANTS.InutFieldsUndefined);
                }
                if (!CommonUtility.GetValidUser(UserID))
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);
                var useCaseModels = _mCSimulationService.GetUseCaseData(ClientUID, DCUID, DeliveryTypeName, UserID);
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(SimulationController), nameof(GetUseCaseData), CONSTANTS.END, string.Empty, string.Empty, ClientUID, DCUID);
                return Ok(useCaseModels);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(SimulationController), nameof(GetUseCaseData), ex.Message, ex, string.Empty, string.Empty, ClientUID, DCUID);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }

        }

        [HttpGet]
        [Route("api/GetUseCaseSimulatedData")]
        public IActionResult GetUseCaseSimulatedData(string ClientUID, string DCUID, string UseCaseID, string UserID)
        {
            GetAppService();
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(SimulationController), nameof(GetUseCaseSimulatedData), CONSTANTS.START, string.Empty, string.Empty, ClientUID, DCUID);
            try
            {
                if (string.IsNullOrEmpty(ClientUID)
                   || string.IsNullOrEmpty(DCUID)
                   || string.IsNullOrEmpty(UseCaseID)
                   || string.IsNullOrEmpty(UserID))
                {
                    return GetSuccessWithMessageResponse(Resource.IngrainResx.InputFieldsAreNull);
                }
                if (ClientUID.Contains(CONSTANTS.undefined)
                    || DCUID.Contains(CONSTANTS.undefined)
                    || UseCaseID.Contains(CONSTANTS.undefined)
                    || UserID.Contains(CONSTANTS.undefined))
                {
                    return GetSuccessWithMessageResponse(CONSTANTS.InutFieldsUndefined);
                }
                if (!CommonUtility.GetValidUser(UserID))
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);
                var useCaseModels = _mCSimulationService.GetUseCaseSimulatedData(ClientUID, DCUID, UseCaseID, UserID);
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(SimulationController), nameof(GetUseCaseSimulatedData), CONSTANTS.END, string.Empty, string.Empty, ClientUID, DCUID);
                return Ok(useCaseModels);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(SimulationController), nameof(GetUseCaseSimulatedData), ex.Message, ex, string.Empty, string.Empty, ClientUID, DCUID);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }

        }

        [HttpGet]
        [Route("api/DeleteUseCase")]
        public IActionResult DeleteUseCase(string ClientUID, string DCUID, string UseCaseID, string UserID)
        {
            GetAppService();
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(SimulationController), nameof(DeleteUseCase), CONSTANTS.START, string.Empty, string.Empty, ClientUID, DCUID);
            try
            {
                if (string.IsNullOrEmpty(ClientUID)
                  || string.IsNullOrEmpty(DCUID)
                  || string.IsNullOrEmpty(UseCaseID)
                  || string.IsNullOrEmpty(UserID))
                {
                    return GetSuccessWithMessageResponse(Resource.IngrainResx.InputFieldsAreNull);
                }
                if (ClientUID.Contains(CONSTANTS.undefined)
                    || DCUID.Contains(CONSTANTS.undefined)
                    || UseCaseID.Contains(CONSTANTS.undefined)
                    || UserID.Contains(CONSTANTS.undefined))
                {
                    return GetSuccessWithMessageResponse(CONSTANTS.InutFieldsUndefined);
                }
                if (!CommonUtility.GetValidUser(UserID))
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);
                var useCaseDetails = _mCSimulationService.DeletedUseCase(ClientUID, DCUID, UseCaseID, UserID);
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(SimulationController), nameof(DeleteUseCase), CONSTANTS.END, string.Empty, string.Empty, ClientUID, DCUID);
                return Ok(useCaseDetails);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(SimulationController), nameof(DeleteUseCase), ex.Message, ex, string.Empty, string.Empty, ClientUID, DCUID);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }

        }
        [HttpGet]
        [Route("api/IsUseCaseExists")]
        public IActionResult IsUseCaseExists(string ClientUID, string DCUID, string UseCaseName)
        {
            GetAppService();
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(SimulationController), nameof(IsUseCaseExists), CONSTANTS.START, string.Empty, string.Empty, ClientUID, DCUID);
            bool isUseCaseExist = false;
            try
            {
                if (string.IsNullOrEmpty(UseCaseName) || UseCaseName == CONSTANTS.undefined || string.IsNullOrEmpty(ClientUID) || ClientUID == CONSTANTS.undefined || string.IsNullOrEmpty(DCUID) || DCUID == CONSTANTS.undefined)
                    return Ok(Resource.IngrainResx.InputFieldsAreNull);
                isUseCaseExist = _mCSimulationService.IsUseCaseUnique(ClientUID, DCUID, UseCaseName);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(SimulationController), nameof(IsUseCaseExists), ex.Message, ex, string.Empty, string.Empty, ClientUID, DCUID);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(SimulationController), nameof(IsUseCaseExists), CONSTANTS.END, string.Empty, string.Empty, ClientUID, DCUID);
            return Ok(isUseCaseExist);
        }

        [HttpGet]
        [Route("api/WeeklySimulation")]
        public async Task<IActionResult> WeeklySimulation(string ClientUID, string DCUID, string UserId, string UseCaseID, string UseCaseName)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(SimulationController), nameof(WeeklySimulation), CONSTANTS.START, string.Empty, string.Empty, ClientUID, DCUID);
            try
            {
                if (ClientUID == CONSTANTS.undefined || DCUID == CONSTANTS.undefined || UserId == CONSTANTS.undefined || UseCaseID == CONSTANTS.undefined)
                    return GetSuccessWithMessageResponse(Resource.IngrainResx.InputFieldsAreNull);
                if (string.IsNullOrEmpty(ClientUID) || string.IsNullOrEmpty(DCUID) || string.IsNullOrEmpty(UseCaseID))
                    return GetSuccessWithMessageResponse(Resource.IngrainResx.InputEmpty);
                if (!CommonUtility.GetValidUser(UserId))
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);
                bool firstCall = true;
                weeklyPrediction = await _mCSimulationService.WeeklySimulation(ClientUID, DCUID, UserId, UseCaseID, UseCaseName);
                firstCall = await _mCSimulationService.WeeklySimulationCounter(false);
                while (!firstCall)
                {
                    weeklyPrediction = await _mCSimulationService.WeeklySimulation(ClientUID, DCUID, UserId, UseCaseID, UseCaseName);
                    firstCall = await _mCSimulationService.WeeklySimulationCounter(false);
                }
                if (weeklyPrediction.Status == CONSTANTS.E)
                {
                    return GetFaultResponse(weeklyPrediction);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(SimulationController), nameof(WeeklySimulation), ex.Message, ex, string.Empty, string.Empty, ClientUID, DCUID);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(SimulationController), nameof(WeeklySimulation), CONSTANTS.END, string.Empty, string.Empty, ClientUID, DCUID);
            return Ok(weeklyPrediction);
        }

        #endregion
    }
}