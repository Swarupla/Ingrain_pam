using Accenture.MyWizard.Ingrain.DataModels.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces
{
    public interface IProcessDataService
    {
        DataEngineeringDTO ProcessDataForModelling(string correlationId, string userId, string pageInfo);
        string CheckPythonProcess(string correlationId, string pageInfo);
        PreProcessModelDTO GetProcessingData(string correlationId);
        void InsertDataCleanUp(dynamic data, string correlationId);
        string PostPreprocessData(dynamic data, string correlationID);
        bool RemoveQueueRecords(string correlationId, string pageInfo);
        List<UseCase> RecommendedAIQueueDetails(string correlationId, string pageInfo);
        string CheckPythonProcess(string correlationId, string pageInfo, string uploadId);
        bool RemoveQueueRecords(string correlationId, string pageInfo, string WFId);
        void SmoteTechnique(string correlationId);
        string HyperTunedQueueDetails(string correlationId, string pageInfo, string hyperTuneId);
        void updateDataTransformationApplied(string correlationId);
        bool IsRecommendedAIPythonInvoked(string correlationId);

        /// <summary>
        /// Get Data for Model Processing 
        /// </summary>
        /// <param name="correlationId">correlationId</param>
        /// <param name="userId">userId</param>
        /// <param name="pageInfo">pageInfo</param>
        /// <param name="dataEngineeringDTO">dataEngineeringDTO</param>
        /// <returns></returns>
        string GetDataForModelProcessing(string correlationId, string userId, string pageInfo, out DataEngineeringDTO dataEngineeringDTO);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="columns"></param>
        /// <param name="dataEngineering"></param>
        /// <returns></returns>
        string PostCleanedData(dynamic columns, out DataEngineeringDTO dataEngineering);

        /// <summary>
        /// Insert/Update AddFeatures
        /// </summary>
        /// <param name="data"></param>
        /// <param name="correlationId"></param>
        void InsertAddFeatures(dynamic data, string correlationId);
    }
}
