#region Copyright (c) 2019 Accenture . All rights reserved.

// <copyright company="Accenture">
// Copyright (c) 2019 Accenture.  All rights reserved.
// Reproduction or transmission in whole or in part, in any form or by any means, 
// electronic, mechanical or otherwise, is prohibited without the prior written 
// consent of the copyright owner.
// </copyright>

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Accenture.MyWizard.Ingrain.DataModels;
using Accenture.MyWizard.Ingrain.DataModels.Models;
using MongoDB.Bson;
using static Accenture.MyWizard.Ingrain.BusinessDomain.Services.ModelEngineeringService;

namespace Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces
{
    public interface IModelEngineering
    {
        FeatureEngineeringDTO GetFeatureAttributes(string correlationId, string ServiceName = "");
        TeachAndTestDTO GetFeatureForTest(string correlationId,string modelName);
        TeachAndTestDTOforTS GetFeatureForTestforTS(string correlationId, string modelType, string timeSeriesSteps,string modelName);

        TeachAndTestDTO GetScenariosforTeach(string correlationId);
        TeachAndTestDTO GetTeachModels(string correlationId, string WFId, string IstimeSeries, string scenario);
        void UpdateFeatures(dynamic data, string correlationId, string ServiceName = "");

        RecommendedAIViewModelDTO GetRecommendedAI(string correlationId, string ServiceName = "");

        void UpdateRecommendedModelTypes(dynamic data, string correlationId, string ServiceName = "");

        RecommedAITrainedModel GetRecommendedTrainedModels(string correlationId, string approach, string ServiceName = "");
        void InsertUsage(double CurrentProgress, double CPUUsage, string CorrelationId, string ServiceName = "");
        void DeleteTrainedModel(string correlationId);
        TestScenarioModelDTO GetTestScenarios(string correlationID, string testName);

        void InsertColumns(WhatIFAnalysis data);
        string RemoveColumns(string correlationId, string[] prescriptionColumns);
        TeachAndTestDTO GetIngestedData(string correlationId);
        FeaturePredictionTestDTO GetFeaturePredictionForTest(string correlationId, string WFId, string steps);

        string SaveTestResults(dynamic data, string correlationId, string wfId);

        SystemUsageDetails GetSystemUsageDetails();
        string DeleteExistingModels(string correlationId, string pageInfo);
        string UpdateExistingModels(string correlationId, string pageInfo);
        RetraingStatus GetRetrain(string correlationId, string ServiceName = "");
        List<IngrainRequestQueue> GetMultipleRequestStatus(string correlationId, string pageInfo, string ServiceName = "");
        Tuple<List<string>, string> GetModelNames(string correlationId, string ServiceName = "");

        string GetSelectedModel(string correlationId);

        string PrescriptiveAnalytics(dynamic data, out PrescriptiveAnalyticsResult prescriptiveAnalytics);

        bool DeletePrescriptiveAnalytics(string correlationId, string wFId);

        string RunTest(dynamic dynamicColumns, out FeaturePredictionTestDTO featurPrediction);
        void UpdateIsRetrainFlag(string correlationId, string ServiceName = "");
        bool IsInitiateRetrain(string correlationId, string ServiceName = "");
        bool IsModelsTrained(string correlationId, string ServiceName = "");
        void TerminateModelsTrainingRequests(string correlationId, List<IngrainRequestQueue> requests, string ServiceName = "");
    }
}
