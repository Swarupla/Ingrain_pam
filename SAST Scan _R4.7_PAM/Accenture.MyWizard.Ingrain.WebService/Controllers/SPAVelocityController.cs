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
Module Name     :   SPAVelocityController
Project         :  Accenture.MyWizard.Ingrain.WebService.Controllers.SPAVelocityController
Organisation    :   Accenture Technologies Ltd.
Created By      :   Ravi A
Created Date    :   28-OCT-2020
Revision History :
Revision No             Initial             Modified Date           Description
1.0                     An                  28-OCT-2020             
\********************************************************************************************************/
#endregion

using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
using Accenture.MyWizard.Ingrain.BusinessDomain.Common;
using Accenture.MyWizard.Ingrain.DataAccess;
using Accenture.MyWizard.Ingrain.DataModels;
using Accenture.MyWizard.Ingrain.DataModels.InstaModels;
using Accenture.MyWizard.Ingrain.DataModels.Models;
using Accenture.MyWizard.Shared.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
using JsonConvert = Newtonsoft.Json.JsonConvert;
using System.Collections;
using Accenture.MyWizard.Cryptography.EncryptionProviders;
using Microsoft.AspNetCore.Mvc;

namespace Accenture.MyWizard.Ingrain.WebService.Controllers
{
    public class SPAVelocityController : MyWizardControllerBase
    {
        CallBackErrorLog auditTrailLog;
        private ISPAVelocityService _VelocityService { get; set; }

        private readonly IngrainAppSettings appSettings;

        string CorrelationId = string.Empty;

        public SPAVelocityController(IServiceProvider serviceProvider, IOptions<IngrainAppSettings> settings)
        {
            _VelocityService = serviceProvider.GetService<ISPAVelocityService>();
            auditTrailLog = new CallBackErrorLog();
            appSettings = settings.Value;
        }
        [HttpPost]
        [Route("api/IngrainTrainingCallbackAPI")]
        public IActionResult IngrainTrainingCallbackAPI(Velocity velocity)
        {
           
            try
            {
                #region VALIDATION                
                CommonUtility.ValidateInputFormData(velocity.AppServiceUID, CONSTANTS.AppServiceUID, true);
                CommonUtility.ValidateInputFormData(velocity.ClientUID, CONSTANTS.ClientUID, true);
                CommonUtility.ValidateInputFormData(velocity.DeliveryConstructUID, CONSTANTS.DeliveryConstructUID, true);
                CommonUtility.ValidateInputFormData(velocity.IsTeamLevelData, "IsTeamLevelData", false, velocity.AppServiceUID);
                CommonUtility.ValidateInputFormData(velocity.ResponseCallbackUrl, CONSTANTS.ResponseCallbackUrl, false, velocity.AppServiceUID);
                CommonUtility.ValidateInputFormData(velocity.RetrainRequired, CONSTANTS.RetrainRequired, false, velocity.AppServiceUID);
                CommonUtility.ValidateInputFormData(velocity.UseCaseUID, CONSTANTS.UseCaseID, true);
                CommonUtility.ValidateInputFormData(Convert.ToString(velocity.QueryData), "QueryData/IterationUID", false, velocity.AppServiceUID);
                CommonUtility.ValidateInputFormData(Convert.ToString(velocity.TeamAreaUId), CONSTANTS.TeamAreaUId, false, velocity.AppServiceUID);      
                if (!CommonUtility.GetValidUser(velocity.UserID))
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);
                #endregion

                IsTrainingEnabled(appSettings.EnableTraining);
                GetAppService();
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericInstaModelController),nameof(IngrainTrainingCallbackAPI),"IngrainAPI Started",velocity.AppServiceUID, string.Empty,velocity.ClientUID,velocity.DeliveryConstructUID);

                //Asset Tracking
                auditTrailLog.ApplicationID = velocity.AppServiceUID;
                auditTrailLog.BaseAddress = velocity.ResponseCallbackUrl;
                auditTrailLog.httpResponse = null;
                auditTrailLog.ClientId = velocity.ClientUID;
                auditTrailLog.DCID = velocity.DeliveryConstructUID;
                auditTrailLog.ApplicationID = velocity.AppServiceUID;
                auditTrailLog.UseCaseId = velocity.UseCaseUID;
                auditTrailLog.RequestId = null;
                auditTrailLog.CreatedBy = velocity.UserID;
                auditTrailLog.ProcessName = CONSTANTS.TrainingName;
                auditTrailLog.UsageType = CONSTANTS.AssetUsage;
                if (!CommonUtility.GetValidUser(velocity.UserID))
                {
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);
                }
                _VelocityService.AuditTrailLog(auditTrailLog);
                if (!string.IsNullOrEmpty(velocity.UseCaseUID) && !string.IsNullOrEmpty(velocity.AppServiceUID) && !string.IsNullOrEmpty(velocity.ClientUID) 
                    && !string.IsNullOrEmpty(velocity.UserID) && !string.IsNullOrEmpty(velocity.DeliveryConstructUID) && !string.IsNullOrEmpty(velocity.ResponseCallbackUrl))
                {
                    if (_VelocityService.IsAmbulanceLane(velocity.UseCaseUID))
                    {
                        //if (velocity.QueryData == null || velocity.QueryData["IterationUID"] == null)
                        //{
                        //    LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericInstaModelController), nameof(IngrainTrainingCallbackAPI), "IngrainAPI Ended");
                        //    return GetFaultResponse(CONSTANTS.IterationUIDMandatory);
                        //}
                        if (velocity.QueryData != null && velocity.QueryData["IterationUID"] != null)
                        {
                            if (velocity.RetrainRequired == "AutoRetrain")
                            {
                                JArray items = (JArray)velocity.QueryData["IterationUID"];
                                if (items != null && items.Count < 1)
                                {                                   
                                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericInstaModelController),nameof(IngrainTrainingCallbackAPI), "IngrainAPI Ended", velocity.AppServiceUID, string.Empty, velocity.ClientUID, velocity.DeliveryConstructUID);

                                    return GetFaultResponse(CONSTANTS.IterationUIDMandatory);
                                }
                            }
                            else
                            {
                                JArray items = (JArray)velocity.QueryData["IterationUID"];
                                long dataPoints = _VelocityService.GetDatapoints(velocity.UseCaseUID, velocity.AppServiceUID);
                                if (items != null && items.Count < dataPoints)
                                {                                    
                                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericInstaModelController),nameof(IngrainTrainingCallbackAPI), "IngrainAPI Ended", velocity.AppServiceUID, string.Empty, velocity.ClientUID, velocity.DeliveryConstructUID);

                                    return GetFaultResponse(String.Format(CONSTANTS.MinIterationUIDMandatory, dataPoints));                                  
                                }
                            }
                        }
                        else
                        {                            
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericInstaModelController),nameof(IngrainTrainingCallbackAPI), "IngrainAPI Ended", velocity.AppServiceUID, string.Empty, velocity.ClientUID, velocity.DeliveryConstructUID);                           
                            return GetFaultResponse(CONSTANTS.IterationUIDMandatory);
                        }
                    }
                    var data = _VelocityService.StartVelocityTraining(velocity);                    
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericInstaModelController),nameof(IngrainTrainingCallbackAPI), "IngrainAPI Ended", velocity.AppServiceUID, string.Empty, velocity.ClientUID, velocity.DeliveryConstructUID);
                    return Ok(data);
                }
                else
                {                   
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericInstaModelController),nameof(IngrainTrainingCallbackAPI), "IngrainAPI Ended", velocity.AppServiceUID, string.Empty, velocity.ClientUID, velocity.DeliveryConstructUID);
                    return GetFaultResponse(CONSTANTS.InputDataEmpty);
                }
            }
            catch (Exception ex)
            {                
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(GenericInstaModelController), nameof(IngrainTrainingCallbackAPI), ex.Message + $"   StackTrace = {ex.StackTrace}", ex, velocity.AppServiceUID, string.Empty, velocity.ClientUID, velocity.DeliveryConstructUID);
                auditTrailLog.ApplicationID = velocity.AppServiceUID;
                auditTrailLog.BaseAddress = velocity.ResponseCallbackUrl;
                auditTrailLog.httpResponse = null;
                auditTrailLog.ClientId = velocity.ClientUID;
                auditTrailLog.DCID = velocity.DeliveryConstructUID;
                auditTrailLog.ApplicationID = velocity.AppServiceUID;
                auditTrailLog.UseCaseId = velocity.UseCaseUID;
                auditTrailLog.RequestId = null;
                auditTrailLog.CreatedBy = velocity.UserID;
                auditTrailLog.ProcessName = CONSTANTS.TrainingName;
                auditTrailLog.UsageType = CONSTANTS.AssetUsage;
                auditTrailLog.Status = "Error - Exception";
                auditTrailLog.ErrorMessage = ex.Message;
                auditTrailLog.CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                auditTrailLog.CorrelationId = CorrelationId;
                _VelocityService.AuditTrailLog(auditTrailLog);
                return GetFaultResponse(ex.Message);
            }
        }

        /// <summary>
        /// Validate based on the usecase ID that this use case for Ambulance Lane
        /// </summary>
        /// <param name="useCaseId"></param>
        /// <returns></returns>
        private bool IsAmbulanceLane(string useCaseId)
        {
            string[] ambulanceLane = new string[] { CONSTANTS.CRHigh, CONSTANTS.CRCritical, CONSTANTS.SRHigh, CONSTANTS.SRCritical, CONSTANTS.PRHigh, CONSTANTS.PRCritical };
            if (ambulanceLane.Contains(useCaseId))
                return true;
            else
                return false;
        }

        [HttpPost]
        [Route("api/GetPrediction")]
        public IActionResult GetPrediction(SPAInfo info)
        {           
            try
            {
                #region VALIDATIONS                
                if (!CommonUtility.GetValidUser(info.UserId))
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);
                CommonUtility.ValidateInputFormData(info.AppServiceUID, CONSTANTS.AppServiceUID, true);
                CommonUtility.ValidateInputFormData(info.ClientUID, CONSTANTS.ClientUID, true);
                CommonUtility.ValidateInputFormData(info.DeliveryConstructUID, CONSTANTS.DeliveryConstructUID, true);
                CommonUtility.ValidateInputFormData(info.CorrelationId, CONSTANTS.CorrelationId, true);                
                CommonUtility.ValidateInputFormData(info.UseCaseUID, CONSTANTS.UseCaseID, true);
                CommonUtility.ValidateInputFormData(Convert.ToString(info.Data), "Data", false, info.AppServiceUID);
                if (info.StartDates != null)
                    info.StartDates.ForEach(x => CommonUtility.ValidateInputFormData(x, "StartDates", false));
                #endregion


                IsPredictionEnabled(appSettings.EnablePrediction);
                GetAppService();                
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericInstaModelController),nameof(GetPrediction), "GetPrediction Started",
                string.IsNullOrEmpty(info.CorrelationId) ? default(Guid) : new Guid(info.CorrelationId), info.AppServiceUID, string.Empty, info.ClientUID, info.DeliveryConstructUID);

                //Asset Tracking
                auditTrailLog.CorrelationId = info.CorrelationId;
                auditTrailLog.ApplicationID = info.AppServiceUID;
                auditTrailLog.BaseAddress = null;
                auditTrailLog.httpResponse = null;
                auditTrailLog.ClientId = info.ClientUID;
                auditTrailLog.DCID = info.DeliveryConstructUID;
                auditTrailLog.ApplicationID = info.AppServiceUID;
                auditTrailLog.UseCaseId = info.UseCaseUID;
                auditTrailLog.RequestId = null;
                auditTrailLog.CreatedBy = info.UserId;
                auditTrailLog.ProcessName = CONSTANTS.PredictionName;
                auditTrailLog.UsageType = CONSTANTS.AssetUsage;
                
                _VelocityService.AuditTrailLog(auditTrailLog);
                var data = _VelocityService.GetPrediction(info);     
              
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(GenericInstaModelController),nameof(GetPrediction), "GetPrediction Ended",
                    string.IsNullOrEmpty(info.CorrelationId) ? default(Guid) : new Guid(info.CorrelationId), auditTrailLog.ApplicationID, string.Empty, auditTrailLog.ClientId, auditTrailLog.DCID);

                return Ok(data);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(GenericInstaModelController), nameof(GetPrediction), ex.Message + $"   StackTrace = {ex.StackTrace}", ex,info.AppServiceUID, string.Empty, info.ClientUID, info.DeliveryConstructUID);
                return GetFaultResponse(ex.Message);
            }
        }
    }
}