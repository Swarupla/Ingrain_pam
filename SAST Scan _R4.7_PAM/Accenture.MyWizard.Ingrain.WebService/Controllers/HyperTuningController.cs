#region Copyright (c) 2019 Accenture . All rights reserved.

// <copyright company="Accenture">
// Copyright (c) 2019 Accenture.  All rights reserved.
// Reproduction or transmission in whole or in part, in any form or by any means, 
// electronic, mechanical or otherwise, is prohibited without the prior written 
// consent of the copyright owner.
// </copyright>

#endregion

#region HyperTuningController Information
/********************************************************************************************************\
Module Name     :   HyperTuningController
Project         :   Accenture.MyWizard.SelfServiceAI.WebService.Controllers
Organisation    :   Accenture Technologies Ltd.
Created By      :   RaviA
Created Date    :   24-May-2019
Revision History :
Revision No             Initial             Modified Date           Description
1.0                     An                  24-May-2019             
\********************************************************************************************************/
#endregion

#region Namespace References
using Accenture.MyWizard.Ingrain.BusinessDomain.Common;
using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
using Accenture.MyWizard.Ingrain.DataModels.Models;
using Accenture.MyWizard.Ingrain.WebService;
using Accenture.MyWizard.Shared.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Web;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
#endregion

namespace Accenture.MyWizard.Ingrain
{
    public class HyperTuningController : MyWizardControllerBase
    {
        #region Members

        private HyperTuningTrainedModel _hyperTunedTrainedModel;

        private HyperTuningDTO _hyperTuning;
        public static IHyperTune _iHyperTune { set; get; }

        public static IIngestedData _iIngestedData { set; get; }

        public static IModelEngineering _iModelEngineering { set; get; }

        private readonly IOptions<IngrainAppSettings> _appSettings;
        #endregion

        #region Constructors
        public HyperTuningController(IServiceProvider serviceProvider, IOptions<IngrainAppSettings> settings)
        {
            _appSettings = settings;
            _iHyperTune = serviceProvider.GetService<IHyperTune>();
            _iIngestedData = serviceProvider.GetService<IIngestedData>();

            _iModelEngineering = serviceProvider.GetService<IModelEngineering>();
        }
        #endregion

        #region Methods
        [HttpGet]
        [Route("api/GetHyperTuneData")]
        public IActionResult GetHyperTuneData(string modelName, string correlationId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(HyperTuningController), nameof(GetHyperTuneData), "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            HyperParametersDTO hyperParameters = new HyperParametersDTO();
            try
            {
                if (!string.IsNullOrEmpty(modelName) && !string.IsNullOrEmpty(correlationId))
                {
                    hyperParameters = _iHyperTune.GetHyperTuneData(modelName, correlationId);
                }
                else
                {
                    return GetSuccessResponse(Resource.IngrainResx.CorrelatioUIDEmpty);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(HyperTuningController), nameof(GetHyperTuneData), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(Resource.IngrainResx.InternalServererror);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(HyperTuningController), nameof(GetHyperTuneData), "END", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            return GetSuccessResponse(hyperParameters);
        }

        /// <summary>
        /// Saves and invokes python call to get the trained hyper tuned data
        /// </summary>
        /// <returns>Returns the list of trained hyper tuned models</returns>
        [HttpGet]
        [Route("api/HyperTuningStartTraining")]
        public IActionResult HyperTuningStartTraining(string correlationId, string hyperTuneId, string userId, string modelName, string pageInfo)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(HyperTuningController), nameof(HyperTuningStartTraining), "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                if (!string.IsNullOrEmpty(correlationId) && !string.IsNullOrEmpty(hyperTuneId) && !string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(modelName) && !string.IsNullOrEmpty(pageInfo))
                {
                    if (!CommonUtility.GetValidUser(userId))
                    {
                        return GetFaultResponse(Resource.IngrainResx.InValidUser);
                    }
                    _hyperTunedTrainedModel = new HyperTuningTrainedModel();
                    IngrainRequestQueue requestQueue = new IngrainRequestQueue();
                    var cPUMemoryCount = new CPUMemoryUtilizeCount();
                    requestQueue = _iIngestedData.GetFileRequestStatus(correlationId, pageInfo, hyperTuneId);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(HyperTuningController), nameof(HyperTuningStartTraining), "requestQueue-HyperTuningStartTraining" + requestQueue, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                    if (requestQueue != null)
                    {
                        if (_appSettings.Value.Environment != CONSTANTS.PAMEnvironment)
                        {
                            //var usageDetails = _iModelEngineering.GetSystemUsageDetails();
                            var usageDetails = cPUMemoryCount.GetMetrics(_appSettings.Value.Environment, _appSettings.Value.IsSaaSPlatform);//TODO: OCB- Commented for PAM
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(HyperTuningController), nameof(HyperTuningStartTraining), "GetSystemUsageDetails" + usageDetails, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty); //TODO: OCB- Commented for PAM
                            _hyperTunedTrainedModel.CPUUsage = usageDetails.CPUUsage;//TODO: OCB-  Commented for PAM
                            _hyperTunedTrainedModel.MemoryUsageInMB = usageDetails.MemoryUsageInMB;//TODO: OCB-  Commented for PAM
                        }
                        _hyperTunedTrainedModel.Status = requestQueue.Status;
                        _hyperTunedTrainedModel.Progress = requestQueue.Progress;
                        _hyperTunedTrainedModel.Message = requestQueue.Message;
                        _hyperTunedTrainedModel.CorrelationId = correlationId;
                        _hyperTunedTrainedModel.HTId = hyperTuneId;

                        if (requestQueue.Status == "C" && requestQueue.Progress == "100")
                        {
                            _hyperTunedTrainedModel = _iHyperTune.GetHyperTunedTrainedModels(correlationId, hyperTuneId, string.Empty);
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(HyperTuningController), nameof(HyperTuningStartTraining), "GetHyperTunedTrainedModels" + _hyperTunedTrainedModel, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                            _hyperTunedTrainedModel.Status = requestQueue.Status;
                            _hyperTunedTrainedModel.Progress = requestQueue.Progress;
                            _hyperTunedTrainedModel.Message = requestQueue.Message;
                            _hyperTunedTrainedModel.CorrelationId = correlationId;
                            _hyperTunedTrainedModel.HTId = hyperTuneId;
                            _iHyperTune.InsertUsage(_hyperTunedTrainedModel.CPUUsage, correlationId, hyperTuneId);
                            return GetSuccessResponse(_hyperTunedTrainedModel);
                        }
                        else if (requestQueue.Status == "E")
                        {
                            string errorInfo = "Status=" + _hyperTunedTrainedModel.Status + " " + "&" + " " + "Progress=" + _hyperTunedTrainedModel.Progress;
                            return GetFaultResponse(_hyperTunedTrainedModel);
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(requestQueue.Status))
                            {
                                _hyperTunedTrainedModel.Status = "P";
                                _hyperTunedTrainedModel.Progress = "1";
                            }
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(HyperTuningController), nameof(HyperTuningStartTraining), "End", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                            return GetSuccessResponse(_hyperTunedTrainedModel);
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
                            ModelName = modelName,
                            RequestStatus = "New",
                            RetryCount = 0,
                            ProblemType = null,
                            Message = null,
                            UniId = hyperTuneId,
                            Progress = null,
                            pageInfo = pageInfo,
                            ParamArgs = "{}",
                            Function = "HyperTune",
                            CreatedByUser = userId,
                            CreatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                            ModifiedByUser = userId,
                            ModifiedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                            LastProcessedOn = null,
                        };
                        HyperTuneWSParams hyperParams = new HyperTuneWSParams
                        {
                            HTId = hyperTuneId,
                            IsHyperTuned = "True"
                        };
                        ingrainRequest.ParamArgs = hyperParams.ToJson();
                        _iIngestedData.InsertRequests(ingrainRequest);
                        Thread.Sleep(2000);
                        PythonResult pythonResult2 = new PythonResult();
                        pythonResult2.message = "Success..";
                        pythonResult2.status = "True";
                        string pythonResult = pythonResult2.ToJson();
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(HyperTuningController), nameof(HyperTuningStartTraining), "END", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);

                        return GetSuccessResponse(pythonResult);
                    }
                }
                else
                {
                    return GetFaultResponse(Resource.IngrainResx.InputData);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(HyperTuningController), nameof(HyperTuningStartTraining), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
        }

        /// <summary>
        /// Saves the hyper tuned data
        /// </summary>
        /// <param name="columnsData">The columns data</param>
        /// <param name="dynamicColumns">The dynamic columns</param>
        /// <returns>Returns the result.</returns>
        [HttpPost]
        [Route("api/PostHyperTuning")]
        public IActionResult PostHyperTuning([FromBody] dynamic requestBody)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(HyperTuningController), nameof(PostHyperTuning), "START", string.Empty, string.Empty, string.Empty, string.Empty);
            string columnsData = Convert.ToString(requestBody);
            dynamic dynamicColumns = Newtonsoft.Json.JsonConvert.DeserializeObject<HyperTuningDTO>(columnsData);
            string htId = Guid.NewGuid().ToString();
            dynamicColumns.HTId = htId;
            try
            {
                if (!CommonUtility.GetValidUser(dynamicColumns.CreatedByUser))
                {
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);
                }
                if (!CommonUtility.GetValidUser(dynamicColumns.ModifiedByUser))
                {
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);
                }
                if (dynamicColumns.CorrelationId != null && dynamicColumns.CorrelationId != "undefined")
                {
                    if (dynamicColumns.ModelParams.ToString() != null && dynamicColumns.ModelParams.ToString() != "{}")
                    {
                        CommonUtility.ValidateInputFormData(dynamicColumns.CorrelationId, "CorrelationId", true);
                        CommonUtility.ValidateInputFormData(dynamicColumns.HTId, "HTId", true);
                        CommonUtility.ValidateInputFormData(dynamicColumns.VersionName, "VersionName", false);
                        CommonUtility.ValidateInputFormData(Convert.ToString(dynamicColumns.ModelParams), "ModelParams", false);
                        CommonUtility.ValidateInputFormData(dynamicColumns.ProblemType, "ProblemType", false);
                        CommonUtility.ValidateInputFormData(dynamicColumns.PageInfo, "PageInfo", false);
                        CommonUtility.ValidateInputFormData(dynamicColumns.ModelName, "ModelName", false);
                        CommonUtility.ValidateInputFormData(dynamicColumns.CreatedOn, "CreatedOn", false);
                        CommonUtility.ValidateInputFormData(dynamicColumns.ModifiedOn, "ModifiedOn", false);
                        var columns = JObject.Parse(columnsData);
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(HyperTuningController), nameof(PostHyperTuning) + Convert.ToString(columns["CorrelationId"]), "START", string.Empty, string.Empty, string.Empty, string.Empty);
                        _iHyperTune.PostHyperTuning(dynamicColumns);
                    }
                    else
                    {
                        return GetSuccessResponse(CONSTANTS.ModelParams);
                    }
                }
                else
                {
                    return GetSuccessResponse(Resource.IngrainResx.CorrelatioUIDEmpty);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(HyperTuningController), nameof(PostHyperTuning), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(HyperTuningController), nameof(PostHyperTuning), "END", string.Empty, string.Empty, string.Empty, string.Empty);
            return GetSuccessResponse(htId);
        }

        [NonAction]
        /// <summary>
        /// Python call
        /// </summary>
        public string InvokePython(string correlationId, string hyperTuneId, string modelName, string pageInfo, string userId, bool isHyperTuned)
        {
            string resultString = string.Empty;
            string URI = string.Empty;
            try
            {
                //Python call                
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(HyperTuningController), nameof(InvokePython), "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                string requestPythonURI = string.Format(@"{0}?correlationId={1}&HTId={2}&HyperTune={3}&Model={4}&pageInfo={5}&userId={6}", "RecommendedAI", correlationId, hyperTuneId, isHyperTuned, modelName, pageInfo, userId);

                string basePytonURI = _appSettings.Value.PythonBaseURI;
                URI = basePytonURI + requestPythonURI;
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(HyperTuningController), "InvokePython URL" + HttpUtility.UrlDecode(URI), URI, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);


                HttpWebRequest request = WebRequest.Create(HttpUtility.UrlDecode(URI)) as HttpWebRequest;
                request.Method = "GET";
                request.KeepAlive = false;

                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                StreamReader reader = new StreamReader(response.GetResponseStream());
                resultString = reader.ReadToEnd();
                response.Close();
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(HyperTuningController), nameof(InvokePython), "PythonResult : " + resultString, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);

            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(HyperTuningController), nameof(InvokePython), ex.Message, ex, "", "", "", "");
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(HyperTuningController), nameof(InvokePython), URI, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(HyperTuningController), nameof(InvokePython), "END", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            return resultString;
        }

        /// <summary>
        /// Saves the hyper tune version
        /// </summary>        
        [HttpPost]
        [Route("api/SaveHyperTuningVersion")]
        public IActionResult SaveHyperTuningVersion([FromBody] dynamic requestBody)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(HyperTuningController), nameof(SaveHyperTuningVersion), "START", string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                _hyperTuning = new HyperTuningDTO();
                //HttpContent content = Request.Content;
                //string columnsData = content.ReadAsStringAsync().Result;
                string columnsData = Convert.ToString(requestBody);
                dynamic dynamicColumns = Newtonsoft.Json.JsonConvert.DeserializeObject(columnsData);
                if (dynamicColumns.CorrelationId != null && dynamicColumns.CorrelationId != "undefined")
                {
                    if (dynamicColumns.HTId != null && dynamicColumns.HTId != "undefined")
                    {
                        var columns = JObject.Parse(columnsData);
                        string correlationId = columns["CorrelationId"].ToString();
                        string htId = columns["HTId"].ToString();
                        CommonUtility.ValidateInputFormData(correlationId, "CorrelationId", true);
                        CommonUtility.ValidateInputFormData(htId, "HTId", true);
                        CommonUtility.ValidateInputFormData(Convert.ToString(dynamicColumns.VersionName), "VersionName", false);
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(HyperTuningController), nameof(SaveHyperTuningVersion) + correlationId, "START", string.Empty, string.Empty, string.Empty, string.Empty);
                        _iHyperTune.SaveHyperTuneVersion(dynamicColumns, correlationId, htId);
                        _hyperTuning = _iHyperTune.GetHyperTuningVersions(correlationId, htId);
                        return GetSuccessResponse(_hyperTuning);
                    }
                    else
                    {
                        return GetSuccessResponse(Resource.IngrainResx.HTIDEmpty);

                    }
                }
                else
                {
                    return GetSuccessResponse(Resource.IngrainResx.CorrelatioUIDEmpty);
                }

            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(HyperTuningController), nameof(SaveHyperTuningVersion), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
        }

        /// <summary>
        /// Gets the hyper tuned record based on version name
        /// </summary>
        /// <param name="correlationId">The correlation identifier</param>
        /// <param name="hyperTuneId">The hyper tune identifier</param>
        /// <param name="versionName">The version name</param>
        /// <returns>Returns the resultant data</returns>
        [HttpGet]
        [Route("api/GetHyperTunedDataByVersion")]
        public IActionResult GetHyperTunedDataByVersion(string correlationId, string hyperTuneId, string versionName)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(HyperTuningController), nameof(GetHyperTunedDataByVersion), "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            _hyperTunedTrainedModel = new HyperTuningTrainedModel();
            try
            {
                if (!string.IsNullOrEmpty(correlationId) && !string.IsNullOrEmpty(hyperTuneId))
                {
                    _hyperTunedTrainedModel = _iHyperTune.GetHyperTunedTrainedModels(correlationId, hyperTuneId, versionName);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(HyperTuningController), nameof(GetHyperTunedDataByVersion), "END", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                    return GetSuccessResponse(_hyperTunedTrainedModel);
                }
                else
                {
                    return Accepted(Resource.IngrainResx.InputData);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(HyperTuningController), nameof(GetHyperTunedDataByVersion), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }
        }
        #endregion
    }
}