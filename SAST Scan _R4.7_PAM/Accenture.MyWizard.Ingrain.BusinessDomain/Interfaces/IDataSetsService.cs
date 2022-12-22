using Accenture.MyWizard.Ingrain.DataModels.Models;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces
{
    public interface IDataSetsService
    {
        Task SaveFileAsync();
        Task<DataSetInfoDto> SaveFileChunks(HttpContext httpContext);
        bool SplitFile();
        Task<List<DataSetInfo>> GetDataSets(string clientUId, string deliveryConstructUId, string userId, string sourceName);
        Task<DataSetInfo> GetDataSetDetails(string dataSetUId);
        bool CheckDataSetExists(string dataSetUId);
        string DownloadDataSet(string dataSetUId);
        string DeleteDataSet(string dataSetUId, string userId);
        Task<List<DataSetInfo>> GetDataSetsList(string clientUId, string deliveryConstructUId, string userId, string sourceName);
        Task<string> GetDataSetData(string dataSetUId, int DecimalPlaces);
        Task<List<object>> GetDataSetResponse(string dataSetUId);
        Task<string> DownloadData(string dataSetUId);
        Task<List<DataSetInfo>> GetCompletedDataSetsList(string clientUId, string deliveryConstructUId, string userId);
        JObject GetSampleData(JObject inputRequest);
        Task<DataSetInfo> InsertExternalAPIRequest(JObject requestPayload);

        TrainGenericDataSetOutput TrainGenericDataSets(TrainGenericDataSetInput trainGenericDataSetInput);
    }
}
