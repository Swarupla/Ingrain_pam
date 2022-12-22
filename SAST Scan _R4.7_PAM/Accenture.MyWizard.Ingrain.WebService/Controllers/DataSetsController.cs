using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Accenture.MyWizard.Ingrain.BusinessDomain.Common;
using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
using Accenture.MyWizard.Ingrain.DataModels.Models;
using Accenture.MyWizard.Ingrain.WebService.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Accenture.MyWizard.Ingrain.WebService.Controllers
{
    public class DataSetsController : MyWizardControllerBase
    {
        private IEncryptionDecryption encryptionDecryption;
        private IDataSetsService _dataSetsService;
        public DataSetsController(IServiceProvider serviceProvider)
        {
            _dataSetsService = serviceProvider.GetService<IDataSetsService>();
            encryptionDecryption = serviceProvider.GetService<IEncryptionDecryption>();
        }




        [HttpPost]
        [Route("api/UploadDataSet")]
        public async Task<IActionResult> UploadDataSet()
        {
            try
            {
                IFormCollection collection = HttpContext.Request.Form;
                CommonUtility.ValidateInputFormData(Convert.ToString(collection["DataSetUId"]), "DataSetUId", true);
                CommonUtility.ValidateInputFormData(Convert.ToString(collection["ClientUId"]), "ClientUId", true);
                CommonUtility.ValidateInputFormData(Convert.ToString(collection["DeliveryConstructUId"]), "DeliveryConstructUId", true);
                CommonUtility.ValidateInputFormData(Convert.ToString(collection["DataSetName"]), "DataSetName", false);
                CommonUtility.ValidateInputFormData(Convert.ToString(collection["Category"]), "Category", false);
                CommonUtility.ValidateInputFormData(Convert.ToString(collection["SourceName"]), "SourceName", false);
                var response = await _dataSetsService.SaveFileChunks(HttpContext);

                if (response.Status != "E")
                {
                    return GetSuccessResponse(response);
                }
                else
                {
                    return GetFaultResponse(response);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(DataSetsController), nameof(UploadDataSet), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }


        }


        [HttpPost]
        [Route("api/UploadExternalAPIDataSet")]
        public async Task<IActionResult> UploadExternalAPIDataSet([FromBody] dynamic request)
        {
            try
            {
                string json = JsonConvert.SerializeObject(request);
                JObject requestPayload = JObject.Parse(json);
                if (!CommonUtility.GetValidUser(Convert.ToString(requestPayload["UserId"])))
                {
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);
                }
                CommonUtility.ValidateInputFormData(Convert.ToString(requestPayload["ClientUId"]), "ClientUId", true);
                CommonUtility.ValidateInputFormData(Convert.ToString(requestPayload["DeliveryConstructUId"]), "DeliveryConstructUId", true);
                CommonUtility.ValidateInputFormData(Convert.ToString(requestPayload["DataSetName"]), "DataSetName", false);
                CommonUtility.ValidateInputFormData(Convert.ToString(requestPayload["Category"]), "Category", false);
                CommonUtility.ValidateInputFormData(Convert.ToString(requestPayload["HttpMethod"]), "HttpMethod", false);
                CommonUtility.ValidateInputFormData(Convert.ToString(requestPayload["Url"]), "Url", false);
                CommonUtility.ValidateInputFormData(Convert.ToString(requestPayload["AuthType"]), "AuthType", false);
                CommonUtility.ValidateInputFormData(Convert.ToString(requestPayload["Headers"]), "Headers", false);
                CommonUtility.ValidateInputFormData(Convert.ToString(requestPayload["Body"]), "Body", false);
                CommonUtility.ValidateInputFormData(Convert.ToString(requestPayload["AzureUrl"]), "AzureUrl", false);
                CommonUtility.ValidateInputFormData(Convert.ToString(requestPayload["AzureCredentials"]), "AzureCredentials", false);
                CommonUtility.ValidateInputFormData(Convert.ToString(requestPayload["StartDate"]), "StartDate", false);
                CommonUtility.ValidateInputFormData(Convert.ToString(requestPayload["EndDate"]), "EndDate", false);
                CommonUtility.ValidateInputFormData(Convert.ToString(requestPayload["Token"]), "Token", false);

                var response = await _dataSetsService.InsertExternalAPIRequest(requestPayload);
                return GetSuccessResponse(response);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(DataSetsController), nameof(UploadExternalAPIDataSet), ex.Message+ ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }

        }



        [HttpGet]
        [Route("api/DownloadDataSetFile")]
        public async Task<IActionResult> DownloadDataSetFile(string dataSetUId)
        {
            List<object> response = null;
            try
            {
                if (string.IsNullOrEmpty(dataSetUId))
                    throw new ArgumentNullException(nameof(dataSetUId));

                response = await _dataSetsService.GetDataSetResponse(dataSetUId);

            }
            catch (Exception ex)
            {
                return GetFaultResponse(ex.Message);
            }
            return GetSuccessResponse(response);

        }



        [HttpGet]
        [Route("api/GetDataSets")]
        public async Task<IActionResult> GetDataSets(string clientUId, string deliveryConstructUId, string userId, string sourceName)
        {
            List<DataSetInfo> response = null;
            try
            {
                if (string.IsNullOrEmpty(clientUId))
                    throw new ArgumentNullException(nameof(clientUId));
                if (string.IsNullOrEmpty(deliveryConstructUId))
                    throw new ArgumentNullException(nameof(deliveryConstructUId));
                if (string.IsNullOrEmpty(userId))
                    throw new ArgumentNullException(nameof(userId));
                if (string.IsNullOrEmpty(sourceName))
                    throw new ArgumentNullException(nameof(sourceName));
                if (!CommonUtility.GetValidUser(userId))
                {
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);
                }

                response = await _dataSetsService.GetDataSetsList(clientUId, deliveryConstructUId, userId, sourceName);

            }
            catch (Exception ex)
            {

                return GetFaultResponse(ex.Message);
            }
            return GetSuccessResponse(response);
        }
        [HttpGet]
        [Route("api/GetCompletedDataSets")]
        public async Task<IActionResult> GetCompletedDataSets(string clientUId, string deliveryConstructUId, string userId)
        {
            List<DataSetInfo> response = null;
            try
            {
                if (string.IsNullOrEmpty(clientUId))
                    throw new ArgumentNullException(nameof(clientUId));
                if (string.IsNullOrEmpty(deliveryConstructUId))
                    throw new ArgumentNullException(nameof(deliveryConstructUId));
                if (string.IsNullOrEmpty(userId))
                    throw new ArgumentNullException(nameof(userId));

                if (!CommonUtility.GetValidUser(userId))
                {
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);
                }
                response = await _dataSetsService.GetCompletedDataSetsList(clientUId, deliveryConstructUId, userId);

            }
            catch (Exception ex)
            {

                return GetFaultResponse(ex.Message);
            }
            return GetSuccessResponse(response);
        }

        [HttpGet]
        [Route("api/GetDataSetDetails")]
        public async Task<IActionResult> GetDataSetDetails(string dataSetUId)
        {
            DataSetInfo response = null;
            try
            {
                if (string.IsNullOrEmpty(dataSetUId))
                    throw new ArgumentNullException(nameof(dataSetUId));

                response = await _dataSetsService.GetDataSetDetails(dataSetUId);

            }
            catch (Exception ex)
            {
                return GetFaultResponse(ex.Message);
            }
            return GetSuccessResponse(response);
        }




        [HttpGet]
        [Route("api/ViewDataSet")]
        public async Task<IActionResult> ViewDataSet(string dataSetUId, int DecimalPlaces)
        {
            string response = null;
            try
            {
                if (string.IsNullOrEmpty(dataSetUId))
                    throw new ArgumentNullException(nameof(dataSetUId));

                response = await _dataSetsService.GetDataSetData(dataSetUId, DecimalPlaces);

            }
            catch (Exception ex)
            {
                return GetFaultResponse(ex.Message);
            }
            return GetSuccessResponse(response);
        }

        [HttpGet]
        [Route("api/DeleteDataSet")]
        public IActionResult DeleteDataSet(string dataSetUId, string userId)
        {
            string resp = null;
            try
            {
                if (string.IsNullOrEmpty(dataSetUId))
                    throw new ArgumentNullException(nameof(dataSetUId));
                if (string.IsNullOrEmpty(userId))
                    throw new ArgumentNullException(nameof(userId));

                if (!CommonUtility.GetValidUser(userId))
                {
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);
                }
                resp = _dataSetsService.DeleteDataSet(dataSetUId, userId);

            }
            catch (Exception ex)
            {
                return GetFaultResponse(ex.Message);
            }
            return GetSuccessResponse(resp);
        }




        [HttpPost]
        [Route("api/SplitFile")]
        public IActionResult SplitFile()
        {
            try
            {
                _dataSetsService.SplitFile();
                return Ok();
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(DataSetsController), nameof(SplitFile), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.StackTrace);
            }


        }


        [HttpPost]
        [Route("api/GetSampleData")]
        public IActionResult GetSampleData([FromBody] dynamic requestPayload)
        {
            try
            {
                JObject payload = JObject.Parse(JsonConvert.SerializeObject(requestPayload));
                if (payload.ContainsKey("DataSetUId"))
                {
                    CommonUtility.ValidateInputFormData(Convert.ToString(payload["DataSetUId"]), "DataSetUId", true);
                }
                if (payload.ContainsKey("PageNumber"))
                {
                    CommonUtility.ValidateInputFormData(Convert.ToString(requestPayload["PageNumber"]), "PageNumber", false);
                }
                if (payload.ContainsKey("StartDate") && payload.ContainsKey("EndDate"))
                {
                    CommonUtility.ValidateInputFormData(Convert.ToString(requestPayload["StartDate"]), "StartDate", false);
                    CommonUtility.ValidateInputFormData(Convert.ToString(requestPayload["EndDate"]), "EndDate", false);
                }
                return GetSuccessResponse(_dataSetsService.GetSampleData(payload));

            }
            catch (Exception ex)
            {
                return GetFaultResponse(ex.StackTrace);
            }


        }

        [HttpPost]
        [Route("api/TrainGenericDataSet")]
        public IActionResult TrainGenericDataSet([FromBody] TrainGenericDataSetInput trainGenericDataSetInput)
        {
            try
            {
                if (!CommonUtility.GetValidUser(trainGenericDataSetInput.UserId))
                {
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);
                }
                CommonUtility.ValidateInputFormData(trainGenericDataSetInput.DeliveryConstructUId, "DeliveryConstructUId", true);
                CommonUtility.ValidateInputFormData(trainGenericDataSetInput.ClientUId, "ClientUId", true);
                CommonUtility.ValidateInputFormData(trainGenericDataSetInput.UseCaseId, "UseCaseId", true);
                CommonUtility.ValidateInputFormData(trainGenericDataSetInput.DataSetUId, "DataSetUId", true);
                CommonUtility.ValidateInputFormData(trainGenericDataSetInput.ApplicationId, "ApplicationId", true);
                if (ModelState.IsValid)
                {
                    return GetSuccessResponse(_dataSetsService.TrainGenericDataSets(trainGenericDataSetInput));
                }
                else
                {
                    return BadRequest(ModelState);
                }

            }
            catch(Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(DataSetsController), nameof(TrainGenericDataSet), ex.Message, ex,
                    trainGenericDataSetInput.ApplicationId, string.Empty,trainGenericDataSetInput.ClientUId, trainGenericDataSetInput.DeliveryConstructUId);
                return GetFaultResponse(ex.Message);
            }

        }

    }
}
