namespace Accenture.MyWizard.Ingrain
{
    #region Namespace References
    using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
    using Accenture.MyWizard.Ingrain.WebService;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Collections.Generic;
    using LOGGING = Accenture.MyWizard.LOGGING;
    using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
    using Microsoft.Extensions.DependencyInjection;
    using Accenture.MyWizard.Ingrain.BusinessDomain.Common;

    #endregion
    public class CloneDataController : MyWizardControllerBase
    {
        #region Members      
        public static ICloneService cloneService { set; get; }
        string cloneSucess = string.Empty;
        string cloneStatus = string.Empty;
        List<string> data = new List<string>();
        IModelEngineering _modelEngineering;
        #endregion

        #region Constructor
        public CloneDataController(IServiceProvider serviceProvider)
        {
            cloneService = serviceProvider.GetService<ICloneService>();
            _modelEngineering = serviceProvider.GetService<IModelEngineering>();
        }
        #endregion

        #region Methods 
        /// <summary>
        /// Clone the data
        /// </summary>
        /// <param name="correlationId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("api/CloneData")]
        public IActionResult CloneData(string correlationId, string modelName, string userId, string deliveryConstructUID, string clientUId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(CloneDataController),nameof(CloneData), "Start", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, clientUId, deliveryConstructUID);

            string cloneSucess = string.Empty;
            string cloneStatus = string.Empty;
            List<string> data = new List<string>();
            try
            {
                if (!string.IsNullOrEmpty(correlationId) && !string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(deliveryConstructUID) && !string.IsNullOrEmpty(clientUId))
                {
                    if (!CommonUtility.GetValidUser(userId))
                    {
                        return GetFaultResponse(Resource.IngrainResx.InValidUser);
                    }
                    string id = Guid.NewGuid().ToString();
                    string NewCorrid = Guid.NewGuid().ToString();

                    //for SSAI_IngrainRequests                    
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(CloneDataController), nameof(CloneData),"SSAI_IngrainRequests", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, clientUId, deliveryConstructUID);

                    cloneService.ColumnsforClone(correlationId, NewCorrid, id, modelName, CONSTANTS.SSAIIngrainRequests, userId, deliveryConstructUID, clientUId, out cloneStatus);
                    if (!String.IsNullOrEmpty(cloneStatus))
                    {
                        cloneSucess = cloneSucess + cloneStatus;
                    }

                    //for SSAI_DeployedModels                    
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(CloneDataController), nameof(CloneData),"SSAI_DeployedModels", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, clientUId, deliveryConstructUID);

                    cloneService.ColumnsforClone(correlationId, NewCorrid, id, modelName, "SSAI_DeployedModels", userId, deliveryConstructUID, clientUId, out cloneStatus);                    
                    if (!String.IsNullOrEmpty(cloneStatus))
                    {
                        cloneSucess = cloneSucess + cloneStatus;
                    }

                    //for PS_BusinessProblem                    
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(CloneDataController), nameof(CloneData),"PS_BusinessProblem", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, clientUId, deliveryConstructUID);

                    cloneService.ColumnsforClone(correlationId, NewCorrid, id, modelName, "PS_BusinessProblem", userId, deliveryConstructUID, clientUId, out cloneStatus);
                    if (!String.IsNullOrEmpty(cloneStatus))
                    {
                        cloneSucess = cloneSucess + cloneStatus;
                    }

                    //for PS_IngestedData                   
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(CloneDataController), nameof(CloneData),"PS_IngestedData", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, clientUId, deliveryConstructUID);

                    cloneService.ColumnsforClone(correlationId, NewCorrid, id, modelName, "PS_IngestedData", userId, deliveryConstructUID, clientUId, out cloneStatus);
                    if (!String.IsNullOrEmpty(cloneStatus))
                    {
                        cloneSucess = cloneSucess + cloneStatus;
                    }

                    //for DataCleanUP_FilteredData
                    cloneService.ColumnsforClone(correlationId, NewCorrid, id, modelName, "DataCleanUP_FilteredData", userId, deliveryConstructUID, clientUId, out cloneStatus);
                    if (!String.IsNullOrEmpty(cloneStatus))
                    {
                        cloneSucess = cloneSucess + cloneStatus;
                    }

                    //for DE_DataCleanup
                    cloneService.ColumnsforClone(correlationId, NewCorrid, id, modelName, "DE_DataCleanup", userId, deliveryConstructUID, clientUId, out cloneStatus);
                    if (!String.IsNullOrEmpty(cloneStatus))
                    {
                        cloneSucess = cloneSucess + cloneStatus;
                    }

                    //for DE_DataVisualization
                    cloneService.ColumnsforClone(correlationId, NewCorrid, id, modelName, "DE_DataVisualization", userId, deliveryConstructUID, clientUId, out cloneStatus);
                    if (!String.IsNullOrEmpty(cloneStatus))
                    {
                        cloneSucess = cloneSucess + cloneStatus;
                    }

                    //for DE_PreProcessedData
                    cloneService.ColumnsforClone(correlationId, NewCorrid, id, modelName, "DE_PreProcessedData", userId, deliveryConstructUID, clientUId, out cloneStatus);
                    if (!String.IsNullOrEmpty(cloneStatus))
                    {
                        cloneSucess = cloneSucess + cloneStatus;
                    }

                    //for ME_FeatureSelection
                    cloneService.ColumnsforClone(correlationId, NewCorrid, id, modelName, "ME_FeatureSelection", userId, deliveryConstructUID, clientUId, out cloneStatus);
                    if (!String.IsNullOrEmpty(cloneStatus))
                    {
                        cloneSucess = cloneSucess + cloneStatus;
                    }

                    //for ME_RecommendedModels
                    cloneService.ColumnsforClone(correlationId, NewCorrid, id, modelName, "ME_RecommendedModels", userId, deliveryConstructUID, clientUId, out cloneStatus);
                    if (!String.IsNullOrEmpty(cloneStatus))
                    {
                        cloneSucess = cloneSucess + cloneStatus;
                    }

                    //for ME_TeachAndTest
                    cloneService.ColumnsforClone(correlationId, NewCorrid, id, modelName, "ME_TeachAndTest", userId, deliveryConstructUID, clientUId, out cloneStatus);
                    if (!String.IsNullOrEmpty(cloneStatus))
                    {
                        cloneSucess = cloneSucess + cloneStatus;
                    }

                    //for SSAI_PublishModel
                    cloneService.ColumnsforClone(correlationId, NewCorrid, id, modelName, "SSAI_PublishModel", userId, deliveryConstructUID, clientUId, out cloneStatus);
                    if (!String.IsNullOrEmpty(cloneStatus))
                    {
                        cloneSucess = cloneSucess + cloneStatus;
                    }

                    //for SSAI_UseCase
                    cloneService.ColumnsforClone(correlationId, NewCorrid, id, modelName, "SSAI_UseCase", userId, deliveryConstructUID, clientUId, out cloneStatus);
                    if (!String.IsNullOrEmpty(cloneStatus))
                    {
                        cloneSucess = cloneSucess + cloneStatus;
                    }

                    //for DE_DataProcessing
                    cloneService.ColumnsforClone(correlationId, NewCorrid, id, modelName, "DE_DataProcessing", userId, deliveryConstructUID, clientUId, out cloneStatus);
                    if (!String.IsNullOrEmpty(cloneStatus))
                    {
                        cloneSucess = cloneSucess + cloneStatus;
                    }

                    //for DeployedPublishModel
                    cloneService.ColumnsforClone(correlationId, NewCorrid, id, modelName, "DeployedPublishModel", userId, deliveryConstructUID, clientUId, out cloneStatus);
                    if (!String.IsNullOrEmpty(cloneStatus))
                    {
                        cloneSucess = cloneSucess + cloneStatus;
                    }

                    //for IngrainDeliveryConstruct
                    cloneService.ColumnsforClone(correlationId, NewCorrid, id, modelName, "IngrainDeliveryConstruct", userId, deliveryConstructUID, clientUId, out cloneStatus);
                    if (!String.IsNullOrEmpty(cloneStatus))
                    {
                        cloneSucess = cloneSucess + cloneStatus;
                    }

                    //for ME_TeachAndTest
                    cloneService.ColumnsforClone(correlationId, NewCorrid, id, modelName, "ME_TeachAndTest", userId, deliveryConstructUID, clientUId, out cloneStatus);
                    if (!String.IsNullOrEmpty(cloneStatus))
                    {
                        cloneSucess = cloneSucess + cloneStatus;
                    }

                    //for MLDL_ModelsMaster
                    cloneService.ColumnsforClone(correlationId, NewCorrid, id, modelName, "MLDL_ModelsMaster", userId, deliveryConstructUID, clientUId, out cloneStatus);
                    if (!String.IsNullOrEmpty(cloneStatus))
                    {
                        cloneSucess = cloneSucess + cloneStatus;
                    }

                    //for SSAI_DeliveryConstructStructures
                    cloneService.ColumnsforClone(correlationId, NewCorrid, id, modelName, "SSAI_DeliveryConstructStructures", userId, deliveryConstructUID, clientUId, out cloneStatus);
                    if (!String.IsNullOrEmpty(cloneStatus))
                    {
                        cloneSucess = cloneSucess + cloneStatus;
                    }

                    //for SSAI_PublicTemplates
                    cloneService.ColumnsforClone(correlationId, NewCorrid, id, modelName, "SSAI_PublicTemplates", userId, deliveryConstructUID, clientUId, out cloneStatus);
                    if (!String.IsNullOrEmpty(cloneStatus))
                    {
                        cloneSucess = cloneSucess + cloneStatus;
                    }

                    //for SSAI_RecommendedTrainedModels
                    cloneService.ColumnsforClone(correlationId, NewCorrid, id, modelName, "SSAI_RecommendedTrainedModels", userId, deliveryConstructUID, clientUId, out cloneStatus);
                    if (!String.IsNullOrEmpty(cloneStatus))
                    {
                        cloneSucess = cloneSucess + cloneStatus;
                    }

                    //for SSAI_UserDetails
                    cloneService.ColumnsforClone(correlationId, NewCorrid, id, modelName, "SSAI_UserDetails", userId, deliveryConstructUID, clientUId, out cloneStatus);
                    if (!String.IsNullOrEmpty(cloneStatus))
                    {
                        cloneSucess = cloneSucess + cloneStatus;
                    }

                    //for TestUniq
                    cloneService.ColumnsforClone(correlationId, NewCorrid, id, modelName, "TestUniq", userId, deliveryConstructUID, clientUId, out cloneStatus);
                    if (!String.IsNullOrEmpty(cloneStatus))
                    {
                        cloneSucess = cloneSucess + cloneStatus;
                    }

                    //for WF_IngestedData
                    cloneService.ColumnsforClone(correlationId, NewCorrid, id, modelName, "WF_IngestedData", userId, deliveryConstructUID, clientUId, out cloneStatus);
                    if (!String.IsNullOrEmpty(cloneStatus))
                    {
                        cloneSucess = cloneSucess + cloneStatus;
                    }

                    //for WF_TestResults
                    cloneService.ColumnsforClone(correlationId, NewCorrid, id, modelName, "WF_TestResults", userId, deliveryConstructUID, clientUId, out cloneStatus);
                    if (!String.IsNullOrEmpty(cloneStatus))
                    {
                        cloneSucess = cloneSucess + cloneStatus;
                    }

                    //for WhatIfAnalysis
                    cloneService.ColumnsforClone(correlationId, NewCorrid, id, modelName, "WhatIfAnalysis", userId, deliveryConstructUID, clientUId, out cloneStatus);
                    if (!String.IsNullOrEmpty(cloneStatus))
                    {
                        cloneSucess = cloneSucess + cloneStatus;
                    }

                    //HyperTune
                    cloneService.ColumnsforClone(correlationId, NewCorrid, id, modelName, "ME_HyperTuneVersion", userId, deliveryConstructUID, clientUId, out cloneStatus);
                    if (!String.IsNullOrEmpty(cloneStatus))
                    {
                        cloneSucess = cloneSucess + cloneStatus;
                    }

                    //SSAI_savedModels
                    cloneService.ColumnsforClone(correlationId, NewCorrid, id, modelName, "SSAI_savedModels", userId, deliveryConstructUID, clientUId, out cloneStatus);
                    if (!String.IsNullOrEmpty(cloneStatus))
                    {
                        cloneSucess = cloneSucess + cloneStatus;
                    }

                    //PrescriptiveAnalyticsResults
                    cloneService.ColumnsforClone(correlationId, NewCorrid, id, modelName, "PrescriptiveAnalyticsResults", userId, deliveryConstructUID, clientUId, out cloneStatus);
                    if (!String.IsNullOrEmpty(cloneStatus))
                    {
                        cloneSucess = cloneSucess + cloneStatus;
                    }

                    //PS_MultiFileColumn
                    cloneService.ColumnsforClone(correlationId, NewCorrid, id, modelName, "PS_MultiFileColumn", userId, deliveryConstructUID, clientUId, out cloneStatus);
                    if (!String.IsNullOrEmpty(cloneStatus))
                    {
                        cloneSucess = cloneSucess + cloneStatus;
                    }

                    //PS_UsecaseDefinition
                    cloneService.ColumnsforClone(correlationId, NewCorrid, id, modelName, "PS_UsecaseDefinition", userId, deliveryConstructUID, clientUId, out cloneStatus);
                    if (!String.IsNullOrEmpty(cloneStatus))
                    {
                        cloneSucess = cloneSucess + cloneStatus;
                    }

                    if (!String.IsNullOrEmpty(cloneSucess))
                    {
                        _modelEngineering.UpdateExistingModels(NewCorrid, "RecommendedAI");
                        cloneService.UpdateDeployedModels(NewCorrid);
                        data.Add(NewCorrid);
                        data.Add(cloneSucess);
                        return Ok(data);
                    }

                    else
                        return Ok("");
                }
                else
                {
                    return NotFound(new { response = Resource.IngrainResx.InputData });
                }
            }
            catch (Exception ex)
            {                
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(CloneDataController), nameof(CloneData),ex.Message + cloneSucess, ex, string.Empty, string.Empty, clientUId, deliveryConstructUID);
                return GetFaultResponse(ex.Message + cloneSucess);
            }
        }
    }
    #endregion
}

