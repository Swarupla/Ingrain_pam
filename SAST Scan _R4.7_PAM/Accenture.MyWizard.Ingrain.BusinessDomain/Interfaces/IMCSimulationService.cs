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
Module Name     :   IMCSimulationService
Project         :   Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces
Organisation    :   Accenture Technologies Ltd.
Created By      :   RaviA
Created Date    :   15-JUN-2020
Revision History :
Revision No             Initial             Modified Date           Description
1.0                     An                  15-JUN-2020             
\********************************************************************************************************/
#endregion
using Accenture.MyWizard.Ingrain.DataModels.Models;
using Accenture.MyWizard.Ingrain.DataModels.MonteCarlo;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces
{
    public interface IMCSimulationService
    {
        TemplateInfo LoadData(IFormFileCollection fileCollection, IFormCollection formCollection, out string message);
        TemplateInfo RRPLoadData(IFormCollection formCollection, out string message);
        GenericFile GenericLoadData(IFormFileCollection fileCollection, IFormCollection formCollection, out string message);

        GetInputInfo GetTemplateData(string ClientUID, string DeliveryConstructUID, string UserId, string TemplateID, string UseCaseName, string ProblemType);
        GetOutputInfo GetSimulationData(string ClientUID, string DeliveryConstructUID, string UserId, string TemplateID, string SimulationID, string UseCaseID, string ProblemType);

        void updateVersionSimulationName(string ClientUID, string DeliveryConstructUID, string UseCaseID, string UserId, string TemplateId, string simulationId, string NewName);
        List<TemplateVersion> InputVersionList(string ClientUID, string DeliveryConstructUID, string UseCaseID);
        List<SimulationVersion> SimulationVersionList(string ClientUID, string DeliveryConstructUID, string UseCaseID, string TemplateId);
        bool IsVSNameExist(string UseCaseID, string VSName, string collectionName, string TemplateId);
        string UpdateTemplateInfo(dynamic data, string TemplateId);
        SimulationPrediction RunSimulation(string templateID, string simulationID, string ProblemType, bool PythonCall, string ClientUID, string DCUID, string UserId, string UseCaseID, string UseCaseName, bool RRPFlag, string SelectedCurrentRelease,JObject selectionUpdate);
        whatIfPrediction GetwhatIfData(string templateID, string simulationID, JObject inputs);
        string CloneTemplateInfo(string ProblemType, string UserId, string TemplateId, string NewName, JObject Features, string TargetColumn);
        string CloneSimulation(string ProblemType, string UserId, string TemplateId, string SimulationID, string NewName, JObject inputs, string TargetColumn);
        UpdateFeatures GenericUpdate(string TemplateID, string TargetColumn, string UniqueIdentifier, string UseCaseName, string UseCaseDescription);
        string UpdateSimulation(string templateID, string simulationID, string UseCaseID, JObject influencers, double targetCertainty, double targetVariable, string Observation,JObject IncrementFlags, JObject PercentChange,string SelectedCurrentRelease);
        UseCaseModelsList GetUseCaseData(string ClientUID, string DCUID, string DeliveryTypeName, string UserID);
        //JObject GetUseCaseSimulatedData(string ClientUID, string DCUID, string UseCaseID, string UserID);

        VDSSimulatedData GetUseCaseSimulatedData(string ClientUID, string DCUID, string UseCaseID, string UserID);

        DeletedUseCaseDetails DeletedUseCase(string ClientUID, string DCUID, string TemplateID, string UserID);
        string Incremented_VersionName();
        bool IsUseCaseUnique(string ClientUID, string DCUID, string usecasename);
        void UpdateSelection(string templateID, string userId, string useCaseID, JObject selectionUpdate, string clientUID, string deliveryConstructUID, string useCaseName, string selection);
        Task<WeeklyPrediction> WeeklySimulation(string clientUID, string dCUID, string userId, string useCaseID, string useCaseName);

        Task<bool> WeeklySimulationCounter(bool firstCall);
    }
}
