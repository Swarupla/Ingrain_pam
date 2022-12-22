#region Copyright (c) 2019 Accenture . All rights reserved.

// <copyright company="Accenture">
// Copyright (c) 2019 Accenture.  All rights reserved.
// Reproduction or transmission in whole or in part, in any form or by any means, 
// electronic, mechanical or otherwise, is prohibited without the prior written 
// consent of the copyright owner.
// </copyright>

#endregion

#region RecommendedAIViewModelDTO Information
/********************************************************************************************************\
Module Name     :   RecommendedAIViewModelDTO
Project         :   Accenture.MyWizard.Ingrain.DataModels.Models.RecommendedAIViewModelDTO
Organisation    :   Accenture Technologies Ltd.
Created By      :   Swetha C
Created Date    :   30-Jan-2019
Revision History :
Revision No             Initial             Modified Date           Description
1.0                     An                  10-Apr-2019             
\********************************************************************************************************/
#endregion

namespace Accenture.MyWizard.Ingrain.DataModels.Models
{
    #region Namespace
    using MongoDB.Bson.Serialization.Attributes;
    using Newtonsoft.Json.Linq;
    using System.Collections.Generic;
    #endregion

    public class RecommendedAIViewModelDTO : Common
    {
        public string CorrelationId { get; set; }

        [BsonElement]
        public JObject SelectedModels { get; set; }

        public string Message { get; set; }

        public bool InstaFlag { get; set; }
        public string Category { get; set; }
    }

    public class RecommedAITrainedModel : Common
    {
        public List<JObject> TrainedModel { get; set; }
        public List<IngrainRequestQueue> UseCaseList { get; set; }
        public double CurrentProgress { get; set; }
        public bool IsCascadingButton { get; set; }
        public bool IsModelTemplateDataSource { get; set; }
        public double EstimatedRunTime { get; set; }
        public new double CPUUsage { get; set; }
    }
    public class UseCase
    {
        public string Status { get; set; }
        public string Progress { get; set; }
        public string ModelName { get; set; }
        public string ProblemType { get; set; }
        public string Message { get; set; }
        public string EstimatedRunTime { get; set; }
    }

    public class Common
    {
        public string ModelName { get; set; }

        public string DataSource { get; set; }

        public string BusinessProblems { get; set; }
        public string ModelType { get; set; }
        public List<DeployedModelVersions> DeployedModelVersions { get; set; }

        public string SelectedModel { get; set; }

        public bool IsModelTrained { get; set; }

        public double CPUUsage { get; set; }

        public double MemoryUsageInMB { get; set; }
        public bool Retrain { get; set; }
        public bool IsInitiateRetrain { get; set; }
        public bool InstaFlag { get; set; }
        public string Category { get; set; }
        public bool IsFmModel { get; set; }
        public string  DataPointsWarning { get; set; }
        public long DataPointsCount { get; set; }
    }
    public class DeployedModelVersions
    {
        public string ModelVersion { get; set; }
    }
    public class RetraingStatus
    {
        public bool Retrain { get; set; }

        public bool IsIntiateRetrain { get; set; }
    }
    public class RecommendedTrainedModelAD 
    {
        public string CorrelationId { get; set; }
        public string CreatedByUser { get; set; }
        public string CreatedOn { get; set; }
        public string ModifiedByUser { get; set; }
        public string ModifiedOn { get; set; }
        public string ProblemType { get; set; }
        [BsonElement]
        public JObject SelectedModels { get; set; }
        public string pageInfo { get; set; }
        public bool retrain { get; set; }
    }
}
