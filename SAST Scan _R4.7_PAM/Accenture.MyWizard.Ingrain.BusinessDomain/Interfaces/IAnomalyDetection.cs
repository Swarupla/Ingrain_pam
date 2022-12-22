using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Accenture.MyWizard.Ingrain.DataModels.Models;
using MongoDB.Bson;

namespace Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces
{
    public interface IAnomalyDetection
    {
        UseCaseSave InsertColumns(BusinessProblemDataDTO data);
        PythonResult GetStatusForDEAndDTProcess(string correlationId, string pageInfo, string userId);
        string InsertRecommendedModelDtls(string correlationId, string modelType, string userId);
        dynamic GetEncryptedDecryptedValue(string Value, string AesKey, string AesVector, bool IsEncryption);
        IngrainRequestQueue GetRequestStatusbyCoridandPageInfo(string correlationId, string pageInfo);
       // RecommedAITrainedModel GetPublishedModels(string correlationId);
    }
}
