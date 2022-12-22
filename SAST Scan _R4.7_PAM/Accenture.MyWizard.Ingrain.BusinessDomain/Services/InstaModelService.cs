#region Copyright (c) 2018 Accenture . All rights reserved.

// <copyright company="Accenture">
// Copyright (c) 2018 Accenture.  All rights reserved.
// Reproduction or transmission in whole or in part, in any form or by any means, 
// electronic, mechanical or otherwise, is prohibited without the prior written 
// consent of the copyright owner.
// </copyright>

#endregion

#region ModelEngineeringService Information
/********************************************************************************************************\
Module Name     :   InstaModelService
Project         :   Accenture.MyWizard.SelfServiceAI.BusinessDomain
Organisation    :   Accenture Technologies Ltd.
Created By      :   RaviA
Created Date    :   30-Jan-2019
Revision History :
Revision No             Initial             Modified Date           Description
1.0                     An                  29-Mar-2019             
\********************************************************************************************************/
#endregion

using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
using Accenture.MyWizard.Ingrain.DataAccess;
using Accenture.MyWizard.Ingrain.DataModels;
using Accenture.MyWizard.Ingrain.DataModels.InstaModels;
using Accenture.MyWizard.Ingrain.DataModels.Models;
using Accenture.MyWizard.Shared.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
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
using Accenture.MyWizard.Ingrain.BusinessDomain.Common;
using JsonConvert = Newtonsoft.Json.JsonConvert;
using System.Diagnostics;
using System.Net.Http.Headers;

namespace Accenture.MyWizard.Ingrain.BusinessDomain.Services
{
    public class InstaModelService : IInstaModel
    {
        #region Members
        private MongoClient _mongoClient;
        private IMongoDatabase _database;
        private InstaMLPreProcessDTO _preProcessDTO;
        bool insertSuccess = false;
        private readonly IOptions<IngrainAppSettings> appSettings;
        DatabaseProvider databaseProvider;
        InstaModel instaModel = null;
        TimeSeriesModel timeSeriesModel = null;
        IngestModel ingestModel = null;
        InstaRegression instaRegression = null;
        List<instaMLResponse> regressionResponse = null;

        private RecommedAITrainedModel _recommendedAI;

        private DeployModelViewModel _deployModelViewModel;

        private IIngestedData _ingestedDataService { get; set; }

        private IModelEngineering _modelEngineering { get; set; }

        private IProcessDataService _processDataService { get; set; }

        private IDeployedModelService _deployedModelService { get; set; }
        private IFlushService _flushService { get; set; }

        public static IAssetUsageTrackingData _iassetUsageTrackingData { set; get; }

        private IEncryptionDecryption _encryptionDecryption;
        private bool _DBEncryptionRequired;
        private CallBackErrorLog auditTrailLog;
        private IFlaskAPI _iFlaskAPIService;

        #endregion
        #region Constructor
        public InstaModelService(DatabaseProvider db, IOptions<IngrainAppSettings> settings, IServiceProvider serviceProvider)
        {
            databaseProvider = db;
            appSettings = settings;
            _mongoClient = databaseProvider.GetDatabaseConnection();
            var dataBaseName = MongoUrl.Create(appSettings.Value.connectionString).DatabaseName;
            _database = _mongoClient.GetDatabase(dataBaseName);
            _preProcessDTO = new InstaMLPreProcessDTO();
            _ingestedDataService = serviceProvider.GetService<IIngestedData>();
            _processDataService = serviceProvider.GetService<IProcessDataService>();
            _deployedModelService = serviceProvider.GetService<IDeployedModelService>();
            _flushService = serviceProvider.GetService<IFlushService>();
            _iassetUsageTrackingData = serviceProvider.GetService<IAssetUsageTrackingData>();
            regressionResponse = new List<instaMLResponse>(); ;

            instaModel = new InstaModel();
            instaRegression = new InstaRegression();
            _modelEngineering = serviceProvider.GetService<IModelEngineering>();
            _encryptionDecryption = serviceProvider.GetService<IEncryptionDecryption>();
            auditTrailLog = new CallBackErrorLog();
            _iFlaskAPIService = serviceProvider.GetService<IFlaskAPI>();
        }
        #endregion


        public void IngestData(string data, out InstaModel timeSeriesModel, out InstaRegression regressionModel)
        {
            VDSRegression vdsRegression = JsonConvert.DeserializeObject<VDSRegression>(data);
            var vdsData = JObject.Parse(data);
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), nameof(IngestData), CONSTANTS.IngestPayload + data.Replace("\r\n ", string.Empty).Replace(CONSTANTS.slash, string.Empty).Trim(), string.Empty, string.Empty, string.Empty, string.Empty);
            string ProblemType = string.Empty;
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), nameof(IngestData), CONSTANTS.IngestPayload + data.Replace("\r\n ", string.Empty).Replace(CONSTANTS.slash, string.Empty).Trim(), string.Empty, string.Empty, string.Empty, string.Empty);
            if (vdsData[CONSTANTS.UseCaseID] != null)
            {
                ProblemType = CONSTANTS.Regression;
            }
            else
            {
                ProblemType = vdsData[CONSTANTS.ProblemType].ToString();
            }
            if (ProblemType != null && ProblemType == CONSTANTS.TimeSeries)
            {
                bool isTimeSeries = IngestingTimeSeries(vdsData, ProblemType);

            }
            if (ProblemType != null && ProblemType == CONSTANTS.Regression)
            {
                bool isRegressionCompleted = IngestingRegression(vdsData, ProblemType);
            }
            timeSeriesModel = instaModel;
            regressionModel = instaRegression;
        }

        private bool IngestingTimeSeries(JObject vdsData, string ProblemType)
        {
            string source = string.Empty;
            timeSeriesModel = new TimeSeriesModel
            {
                InstaId = vdsData["InstaID"].ToString(),
                CorrelationId = Guid.NewGuid().ToString(),
                ProblemType = ProblemType,
                UserId = vdsData["CreatedByUser"].ToString(),
                Frequency = vdsData["Frequency"].ToString(),
                FrequencySteps = Convert.ToInt32(vdsData["FrequencySteps"]),
                DataSource = "InstaML",
                URL = vdsData["URL"].ToString(),
                ClientUId = vdsData["ClientUID"].ToString(),
                DeliveryConstructUID = vdsData["DCID"].ToString(),
                CreatedByUser = vdsData["CreatedByUser"].ToString(),
                ModifiedByUser = vdsData["CreatedByUser"].ToString()
            };
            if (vdsData.ContainsKey("Source"))
            {
                if (!string.IsNullOrEmpty(vdsData["Source"].ToString()))
                {
                    if (vdsData["Source"].ToString() == CONSTANTS.FDSEnvironment)
                    {
                        source = CONSTANTS.VDS_AIOPS;
                        auditTrailLog.Environment = CONSTANTS.FDSEnvironment;
                        auditTrailLog.ApplicationName = CONSTANTS.VDS_AIOPS;
                    }
                    else if (vdsData["Source"].ToString() == CONSTANTS.PAMEnvironment)
                    {
                        source = CONSTANTS.VDS;
                        auditTrailLog.Environment = CONSTANTS.PAMEnvironment;
                        auditTrailLog.ApplicationName = CONSTANTS.VDS;
                    }
                    else
                    {
                        source = vdsData["Source"].ToString();
                        auditTrailLog.Environment = CONSTANTS.PADEnvironment;
                        auditTrailLog.ApplicationName = CONSTANTS.VDS_SI;
                    }

                }
            }

            timeSeriesModel.TargetColumn = "value";
            timeSeriesModel.Dimension = "Date";
            if (string.IsNullOrEmpty(timeSeriesModel.InstaId)
                || string.IsNullOrEmpty(timeSeriesModel.Dimension)
                || string.IsNullOrEmpty(timeSeriesModel.TargetColumn)
                || string.IsNullOrEmpty(timeSeriesModel.UserId)
                || string.IsNullOrEmpty(timeSeriesModel.Frequency)
                || string.IsNullOrEmpty(timeSeriesModel.ClientUId)
                || string.IsNullOrEmpty(timeSeriesModel.DeliveryConstructUID))
            {
                instaModel.ErrorMessage = Resource.IngrainResx.InputEmpty;
                instaModel.Status = CONSTANTS.E;
                instaModel.Message = Resource.IngrainResx.AttributesNull;
            }
            if (instaModel.Status == CONSTANTS.E)
                return false;
            timeSeriesModel.ModelName = timeSeriesModel.InstaId + "_" + timeSeriesModel.TargetColumn;

            if ((!string.IsNullOrEmpty(timeSeriesModel.ClientUId)) && (!string.IsNullOrEmpty(timeSeriesModel.DeliveryConstructUID)))
            {
                _iassetUsageTrackingData.GetUserTrackingDetails(new Guid(timeSeriesModel.ClientUId), timeSeriesModel.CreatedByUser, new Guid(timeSeriesModel.DeliveryConstructUID), "", CONSTANTS.instaML, "TimeSeries", "api/IngestData", "", "", "", "", "", "");
                auditTrailLog.CorrelationId = timeSeriesModel.CorrelationId;
                auditTrailLog.ClientId = timeSeriesModel.ClientUId;
                auditTrailLog.DCID = timeSeriesModel.DeliveryConstructUID;
                auditTrailLog.CreatedBy = timeSeriesModel.UserId;
                auditTrailLog.FeatureName = CONSTANTS.InstaMLTimeseriesFeature;
                auditTrailLog.ProcessName = CONSTANTS.TrainingName;
                auditTrailLog.UsageType = CONSTANTS.AssetUsage;
                if (string.IsNullOrEmpty(auditTrailLog.ApplicationName))
                {
                    auditTrailLog.ApplicationName = CONSTANTS.VDS_SI;
                }
                if (string.IsNullOrEmpty(auditTrailLog.Environment))
                {
                    auditTrailLog.Environment = CONSTANTS.PADEnvironment;
                }
                CommonUtility.AuditTrailLog(auditTrailLog, appSettings);

            }
            InsertBusinessProblem(timeSeriesModel);
            CreateInstaModel(timeSeriesModel.ProblemType, timeSeriesModel.CorrelationId, source);
            List<InstaPayload> instaPayloads = new List<InstaPayload>();

            InstaPayload instaPayload = new InstaPayload();

            instaPayload.InstaId = timeSeriesModel.InstaId;
            instaPayload.CorrelationId = timeSeriesModel.CorrelationId;
            instaPayload.ProblemType = timeSeriesModel.ProblemType;
            instaPayload.TargetColumn = timeSeriesModel.TargetColumn;
            instaPayload.Dimension = "Date";
            instaPayload.UseCaseId = CONSTANTS.Null;
            instaPayload.Source = source;

            instaPayloads.Add(instaPayload);
            IngestDataInsertRequests(timeSeriesModel.CorrelationId, timeSeriesModel.CreatedByUser, timeSeriesModel.InstaId, timeSeriesModel.ClientUId, timeSeriesModel.DeliveryConstructUID, timeSeriesModel.ProblemType, timeSeriesModel.TargetColumn, CONSTANTS.Null, timeSeriesModel.InstaId, source, instaPayloads);
            Thread.Sleep(2000);

            ValidatingIngestDataCompletion(CONSTANTS.TimeSeries, source, instaPayloads);
            return true;
        }

        private bool IngestingRegression(JObject vdsData, string ProblemType)
        {
            string parentUID = string.Empty;
            string instaID = string.Empty;
            string source = string.Empty;
            ingestModel = new IngestModel()
            {
                CorrelationId = Guid.NewGuid().ToString(),
                ProblemType = ProblemType,
                ProblemTypeDetails = JsonConvert.DeserializeObject<List<Accenture.MyWizard.Ingrain.DataModels.InstaModels.ProblemTypeDetails>>(Convert.ToString(vdsData["ProblemTypeDetails"])),
                UserId = vdsData["CreatedByUser"].ToString(),
                URL = vdsData["URL"].ToString(),
                DataSource = "InstaML",
                ClientUId = vdsData["ClientUID"].ToString(),
                DeliveryConstructUID = vdsData["DCID"].ToString(),
                CreatedByUser = vdsData["CreatedByUser"].ToString(),
                ModifiedByUser = vdsData["CreatedByUser"].ToString(),
            };
            if (vdsData.ContainsKey("Source"))
            {
                if (!string.IsNullOrEmpty(vdsData["Source"].ToString()))
                {
                    if (vdsData["Source"].ToString() == CONSTANTS.FDSEnvironment)
                    {
                        source = CONSTANTS.VDS_AIOPS;
                        auditTrailLog.Environment = CONSTANTS.FDSEnvironment;
                        auditTrailLog.ApplicationName = CONSTANTS.VDS_AIOPS;
                    }
                    else if (vdsData["Source"].ToString() == CONSTANTS.PAMEnvironment)
                    {
                        source = CONSTANTS.VDS;
                        auditTrailLog.Environment = CONSTANTS.PAMEnvironment;
                        auditTrailLog.ApplicationName = CONSTANTS.VDS;
                    }
                    else
                    {
                        source = vdsData["Source"].ToString();
                        auditTrailLog.Environment = CONSTANTS.PADEnvironment;
                        auditTrailLog.ApplicationName = CONSTANTS.VDS_SI;
                    }
                }
            }
            parentUID = Convert.ToString(vdsData["UseCaseID"]);
            instaRegression.UseCaseID = parentUID;
            ingestModel.UseCaseID = Convert.ToString(vdsData["UseCaseID"]);
            ingestModel.TargetColumn = ingestModel.ProblemTypeDetails[0].TargetColumn;
            ingestModel.UniqueIdentifier = vdsData["Dimension"].ToString();
            ingestModel.InstaId = ingestModel.ProblemTypeDetails[0].InstaID;
            ingestModel.ModelName = ingestModel.InstaId + "_" + ingestModel.TargetColumn;
            if (string.IsNullOrEmpty(ingestModel.InstaId)
                || string.IsNullOrEmpty(ingestModel.TargetColumn)
                || string.IsNullOrEmpty(ingestModel.UserId)
                || string.IsNullOrEmpty(ingestModel.ClientUId)
                || string.IsNullOrEmpty(ingestModel.DeliveryConstructUID))
            {
                instaModel.ErrorMessage = Resource.IngrainResx.InputEmpty;
                instaModel.Status = CONSTANTS.E;
                instaModel.Message = Resource.IngrainResx.AttributesNull;
            }
            if (instaModel.Status == CONSTANTS.E)
                return false;

            ingestModel.InputColumns = ingestModel.ProblemTypeDetails[0].SelectedFeatures;

            if ((!string.IsNullOrEmpty(ingestModel.ClientUId)) && (!string.IsNullOrEmpty(ingestModel.DeliveryConstructUID)))
            {
                _iassetUsageTrackingData.GetUserTrackingDetails(new Guid(ingestModel.ClientUId), ingestModel.CreatedByUser, new Guid(ingestModel.DeliveryConstructUID), "", CONSTANTS.instaML, "Regression", "api/IngestData", "", "", "", "", "", "");
                auditTrailLog.CorrelationId = ingestModel.CorrelationId;
                auditTrailLog.ClientId = ingestModel.ClientUId;
                auditTrailLog.DCID = ingestModel.DeliveryConstructUID;
                auditTrailLog.CreatedBy = ingestModel.UserId;
                auditTrailLog.FeatureName = CONSTANTS.InstaMLRegressionFeature;
                auditTrailLog.ProcessName = CONSTANTS.TrainingName;
                auditTrailLog.UseCaseId = ingestModel.UseCaseID;
                auditTrailLog.UsageType = CONSTANTS.AssetUsage;
                if (string.IsNullOrEmpty(auditTrailLog.ApplicationName))
                {
                    auditTrailLog.ApplicationName = CONSTANTS.VDS_SI;
                }
                if (string.IsNullOrEmpty(auditTrailLog.Environment))
                {
                    auditTrailLog.Environment = CONSTANTS.PADEnvironment;
                }
                CommonUtility.AuditTrailLog(auditTrailLog, appSettings);
            }
            DeleteRegressionIngestData(ingestModel.UseCaseID, CONSTANTS.IngestData);
            InsertBusinessProblem(ingestModel);
            CreateInstaModel(ingestModel.ProblemType, ingestModel.CorrelationId, source);

            List<InstaPayload> instaPayloads = new List<InstaPayload>();

            InstaPayload instaRegPayload = new InstaPayload();

            instaRegPayload.InstaId = ingestModel.InstaId;
            instaRegPayload.CorrelationId = ingestModel.CorrelationId;
            instaRegPayload.ProblemType = ingestModel.ProblemType;
            instaRegPayload.TargetColumn = ingestModel.TargetColumn;
            instaRegPayload.Dimension = "Date";
            instaRegPayload.UseCaseId = parentUID;
            instaRegPayload.Source = source;

            instaPayloads.Add(instaRegPayload);

            foreach (var item in ingestModel.ProblemTypeDetails)
            {
                InstaPayload instaPayload = new InstaPayload();
                if (item.ProblemType != CONSTANTS.Regression)
                {
                    timeSeriesModel = new TimeSeriesModel
                    {
                        CorrelationId = Guid.NewGuid().ToString(),
                        ProblemType = item.ProblemType,
                        UserId = vdsData["CreatedByUser"].ToString(),
                        Frequency = vdsData["Frequency"].ToString(),
                        FrequencySteps = Convert.ToInt32(vdsData["FrequencySteps"]),
                        DataSource = "InstaML",
                        UseCaseID = parentUID,
                        URL = vdsData["URL"].ToString(),
                        InstaId = item.InstaID,
                        TargetColumn = item.TargetColumn,
                        ModelName = item.InstaID + "_" + item.TargetColumn,
                        ClientUId = vdsData["ClientUID"].ToString(),
                        Dimension = vdsData["Dimension"].ToString(),
                        DeliveryConstructUID = vdsData["DCID"].ToString(),
                        CreatedByUser = vdsData["CreatedByUser"].ToString(),
                        ModifiedByUser = vdsData["CreatedByUser"].ToString()
                    };
                    InsertBusinessProblem(timeSeriesModel);
                    CreateInstaModel(timeSeriesModel.ProblemType, timeSeriesModel.CorrelationId, source);


                    instaPayload.InstaId = timeSeriesModel.InstaId;
                    instaPayload.CorrelationId = timeSeriesModel.CorrelationId;
                    instaPayload.ProblemType = timeSeriesModel.ProblemType;
                    instaPayload.TargetColumn = timeSeriesModel.TargetColumn;
                    instaPayload.Dimension = "Date";
                    instaPayload.UseCaseId = parentUID;
                    instaPayload.Source = source;

                    instaPayloads.Add(instaPayload);
                    //IngestDataInsertRequests(timeSeriesModel.CorrelationId, timeSeriesModel.CreatedByUser, timeSeriesModel.InstaId, timeSeriesModel.ClientUId, timeSeriesModel.DeliveryConstructUID, timeSeriesModel.ProblemType, timeSeriesModel.TargetColumn, parentUID, timeSeriesModel.InstaId,source);
                    //Thread.Sleep(1000);
                }

            }
            IngestDataInsertRequests(ingestModel.CorrelationId, ingestModel.CreatedByUser, ingestModel.InstaId, ingestModel.ClientUId, ingestModel.DeliveryConstructUID, ingestModel.ProblemType, ingestModel.TargetColumn, parentUID, ingestModel.InstaId, source, instaPayloads);
            Thread.Sleep(2000);
            ValidatingIngestDataCompletion(CONSTANTS.Regression, source, instaPayloads);
            instaModel.InstaID = ingestModel.InstaId;
            return true;
        }

        private void IngestDataInsertRequests(string correlationId, string createdByUser, string instaId, string clientUId, string deliveryConstructUID, string problemType, string targetColumn, string parentUID, string instaID, string source, List<InstaPayload> instaPayloads)
        {
            bool DBEncryptionRequired = CommonUtility.EncryptDB(correlationId, appSettings);
            if (DBEncryptionRequired)
            {
                if (!string.IsNullOrEmpty(Convert.ToString(createdByUser)))
                {
                    createdByUser = _encryptionDecryption.Encrypt(Convert.ToString(createdByUser));
                }
            }
            IngrainRequestQueue ingrainRequest = new IngrainRequestQueue
            {
                _id = Guid.NewGuid().ToString(),
                CorrelationId = correlationId,
                RequestId = Guid.NewGuid().ToString(),
                ProcessId = null,
                Status = null,
                ModelName = null,
                RequestStatus = "New",
                RetryCount = 0,
                ProblemType = problemType,
                Message = null,
                UniId = null,
                InstaID = instaID,
                Progress = null,
                pageInfo = "IngestData",
                ParamArgs = null,
                UseCaseID = parentUID,
                Function = CONSTANTS.FileUpload,
                CreatedByUser = createdByUser,
                CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                ModifiedByUser = createdByUser,
                ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                LastProcessedOn = null,
                IsForAutoTrain = true
            };
            string entityitems = CONSTANTS.Null;
            var metricitems = CONSTANTS.Null;
            string Customdata = CONSTANTS.Null;
            Filepath filepath = new Filepath();
            filepath.fileList = CONSTANTS.Null;
            //InstaPayload instaPayload = new InstaPayload
            //{
            //    InstaId = instaId,
            //    ProblemType = problemType,
            //    TargetColumn = targetColumn,
            //    Dimension = "Date",
            //    UseCaseId = parentUID,
            //    Source = source
            //};
            ParentFile parentFile = new ParentFile();
            parentFile.Name = CONSTANTS.Null;
            parentFile.Type = CONSTANTS.Null;
            InstaMLFileUpload fileUpload = new InstaMLFileUpload
            {
                CorrelationId = correlationId,
                ClientUID = clientUId,
                DeliveryConstructUId = deliveryConstructUID,
                Parent = parentFile,
                Flag = CONSTANTS.Null,
                mapping = CONSTANTS.Null,
                mapping_flag = CONSTANTS.False,
                pad = entityitems,
                metric = metricitems,
                InstaMl = instaPayloads,
                fileupload = filepath,
                Customdetails = Customdata
            };
            ingrainRequest.ParamArgs = fileUpload.ToJson();
            InsertRequests(ingrainRequest);
        }

        private void ValidatingIngestDataCompletion(string problemType, string source, List<InstaPayload> instaPayloads)
        {
            bool flag = true;
            while (flag)
            {
                List<instaMLResponse> instaMLResponse = new List<instaMLResponse>();
                List<IngrainRequestQueue> result = new List<IngrainRequestQueue>();
                if (problemType == CONSTANTS.TimeSeries)
                    result = this.GetMultipleRequestStatus(timeSeriesModel.CorrelationId, CONSTANTS.IngestData, timeSeriesModel.ProblemType, null);

                if (problemType == CONSTANTS.Regression)
                {
                    result = this.GetMultipleRequestStatus(ingestModel.CorrelationId, CONSTANTS.IngestData, ingestModel.ProblemType, timeSeriesModel.UseCaseID);
                }


                if (result.Count > 0)
                {
                    if (problemType == CONSTANTS.Regression)
                    {
                        switch (result[0].Status)
                        {
                            case "C":
                                if (result[0].Progress == "100")
                                {
                                    //_DBEncryptionRequired = CommonUtility.EncryptDB(ingestModel.CorrelationId, appSettings);
                                    instaModel.Message = "Ingest Data Success";
                                    flag = false;
                                }
                                break;

                            case "I":
                                instaModel.ErrorMessage = "Ingest Data is lessthan 20 Data Points";
                                string queueStatus = result[0].Status;
                                if (queueStatus == CONSTANTS.E)
                                {
                                    instaMLResponse intsaML = new instaMLResponse();
                                    flag = false;
                                }
                                break;

                            case CONSTANTS.E:
                                instaModel.Message = CONSTANTS.IngestDataError;
                                instaModel.ErrorMessage = CONSTANTS.IngestDataError + "-" + result[0].Message;
                                flag = false;
                                break;

                            default:
                                Thread.Sleep(1000);
                                flag = true;
                                break;
                        }
                        if (result[0].Status == "C" || result[0].Status == "I")
                        {
                            for (int i = 0; i < instaPayloads.Count; i++)
                            {
                                instaMLResponse intsaML = new instaMLResponse();

                                intsaML.Status = result[0].Status;
                                intsaML.Message = instaModel.Message;
                                intsaML.ErrorMessage = instaModel.ErrorMessage;
                                intsaML.CorrelationId = instaPayloads[i].CorrelationId;
                                intsaML.InstaID = instaPayloads[i].InstaId;

                                instaMLResponse.Add(intsaML);

                            }
                            instaRegression.instaMLResponse = instaMLResponse;
                        }
                        else if (result[0].Status == "E")
                        {
                            bool parse = false;
                            JObject errObj = new JObject();
                            try
                            {
                                errObj = JObject.Parse(result[0].Message);
                                if (errObj.Count == instaPayloads.Count)
                                {
                                    parse = true;
                                }

                            }
                            catch (Exception ex)
                            {
                                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InstaModelService), nameof(ValidatingIngestDataCompletion), "parse error-" + ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                                parse = false;
                            }
                            for (int i = 0; i < instaPayloads.Count; i++)
                            {
                                instaMLResponse intsaML = new instaMLResponse();
                                if (parse)
                                {
                                    string err = errObj[instaPayloads[i].CorrelationId].ToString();
                                    intsaML.Status = result[0].Status;
                                    intsaML.Message = instaModel.Message;
                                    intsaML.ErrorMessage = CONSTANTS.IngestDataError + "-" + err;
                                    intsaML.CorrelationId = instaPayloads[i].CorrelationId;
                                    intsaML.InstaID = instaPayloads[i].InstaId;
                                }
                                else
                                {
                                    intsaML.Status = result[0].Status;
                                    intsaML.Message = instaModel.Message;
                                    intsaML.ErrorMessage = instaModel.ErrorMessage;
                                    intsaML.CorrelationId = instaPayloads[i].CorrelationId;
                                    intsaML.InstaID = instaPayloads[i].InstaId;
                                }
                                instaMLResponse.Add(intsaML);

                            }
                            instaRegression.instaMLResponse = instaMLResponse;



                        }
                    }

                    //timeseries
                    else
                    {
                        instaModel.Status = result[0].Status;
                        instaModel.Message = result[0].Message;
                        instaModel.InstaID = timeSeriesModel.InstaId;
                        instaModel.CorrelationId = timeSeriesModel.CorrelationId;
                        switch (result[0].Status)
                        {
                            case "C":
                                if (result[0].Progress == "100")
                                {
                                    _DBEncryptionRequired = CommonUtility.EncryptDB(timeSeriesModel.CorrelationId, appSettings);
                                    instaModel.Message = "Ingest Data Success";
                                    flag = false;
                                }
                                break;

                            case "I":
                                instaModel.ErrorMessage = "Ingest Data is lessthan 20 Data Points";
                                flag = false;
                                break;

                            case CONSTANTS.E:
                                instaModel.Message = CONSTANTS.IngestDataError;
                                string err;
                                try
                                {
                                    JObject errObj = JObject.Parse(result[0].Message);
                                    err = errObj[timeSeriesModel.CorrelationId].ToString();
                                }
                                catch (Exception ex)
                                {
                                   LOGGING.LogManager.Logger.LogErrorMessage(typeof(InstaModelService), nameof(ValidatingIngestDataCompletion), "parse error-" + ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                                    err = result[0].Message;
                                }
                                instaModel.ErrorMessage = CONSTANTS.IngestDataError + "-" + err;
                                flag = false;
                                break;

                            default:
                                Thread.Sleep(1000);
                                flag = true;
                                break;
                        }
                    }


                    #region oldcode


                    //if (result.Count > 1)
                    //{
                    //    for (int i = 0; i < result.Count; i++)
                    //    {
                    //        string queueStatus = result[i].Status;
                    //        if (queueStatus == CONSTANTS.E)
                    //        {
                    //            instaMLResponse intsaML = new instaMLResponse();
                    //            flag = false;
                    //            intsaML.Status = result[i].Status;
                    //            intsaML.Message = CONSTANTS.IngestDataError;
                    //            intsaML.ErrorMessage = CONSTANTS.IngestDataError + " -" + result[i].Message;
                    //            intsaML.CorrelationId = result[i].CorrelationId;
                    //            intsaML.InstaID = result[i].InstaID;
                    //            errorCount++;
                    //            instaMLResponse.Add(intsaML);

                    //        }
                    //        if (queueStatus == "I")
                    //        {
                    //            instaMLResponse intsaML = new instaMLResponse();
                    //            flag = false;
                    //            intsaML.Status = result[i].Status;
                    //            intsaML.ErrorMessage = "Ingest Data is lessthan 20 Data Points";
                    //            intsaML.CorrelationId = result[i].CorrelationId;
                    //            intsaML.InstaID = result[i].InstaID;
                    //            datapointsCount++;
                    //            instaMLResponse.Add(intsaML);
                    //        }
                    //        if (queueStatus == "C")
                    //        {
                    //            instaMLResponse intsaML = new instaMLResponse();
                    //            regressionSuccessCount++;
                    //            intsaML.InstaID = result[i].InstaID;
                    //            intsaML.Status = result[i].Status;
                    //            intsaML.Message = "Ingest Data Success";
                    //            flag = false;
                    //            intsaML.CorrelationId = result[i].CorrelationId;
                    //            instaMLResponse.Add(intsaML);
                    //        }
                    //    }
                    //    if (regressionSuccessCount + datapointsCount + errorCount == result.Count)
                    //    {
                    //        instaRegression.instaMLResponse = instaMLResponse;
                    //        flag = false;
                    //    }
                    //    else
                    //    {
                    //        regressionSuccessCount = 0;
                    //        errorCount = 0;
                    //        datapointsCount = 0;
                    //        flag = true;
                    //    }
                    //}
                    ////TimeSeries
                    //else
                    //{
                    //    instaModel.Status = result[0].Status;
                    //    instaModel.Message = result[0].Message;
                    //    instaModel.InstaID = timeSeriesModel.InstaId;
                    //    instaModel.CorrelationId = timeSeriesModel.CorrelationId;
                    //    switch (result[0].Status)
                    //    {
                    //        case "C":
                    //            if (result[0].Progress == "100")
                    //            {

                    //                _DBEncryptionRequired = CommonUtility.EncryptDB(timeSeriesModel.CorrelationId, appSettings);
                    //                instaModel.Message = "Ingest Data Success";
                    //                flag = false;
                    //            }
                    //            break;

                    //        case "I":
                    //            instaModel.ErrorMessage = "Ingest Data is lessthan 20 Data Points";
                    //            flag = false;
                    //            break;

                    //        case CONSTANTS.E:
                    //            instaModel.Message = CONSTANTS.IngestDataError;
                    //            instaModel.ErrorMessage = CONSTANTS.IngestDataError + "-" + result[0].Message;
                    //            flag = false;
                    //            break;

                    //        default:
                    //            Thread.Sleep(1000);
                    //            flag = true;
                    //            break;
                    //    }
                    //}
                    #endregion
                }
            }
        }
        public InstaModel StartModelTraining(VdsData vdsDataModel)
        {                       
            instaModel.InstaID = vdsDataModel.InstaId;
            instaModel.CorrelationId = vdsDataModel.CorrelationId;
            vdsDataModel.DeliveryConstructUID = vdsDataModel.DCID;
            //_DBEncryptionRequired = CommonUtility.EncryptDB(vdsDataModel.CorrelationId, appSettings);
            if (string.IsNullOrEmpty(vdsDataModel.InstaId) || string.IsNullOrEmpty(vdsDataModel.CorrelationId) || string.IsNullOrEmpty(vdsDataModel.ClientUId) || string.IsNullOrEmpty(vdsDataModel.DeliveryConstructUID) || string.IsNullOrEmpty(vdsDataModel.CreatedByUser) || string.IsNullOrEmpty(vdsDataModel.ProcessName))
            {
                instaModel.ErrorMessage = Resource.IngrainResx.InputEmpty;
                instaModel.Status = CONSTANTS.E;
                instaModel.Message = Resource.IngrainResx.AttributesNull;
            }
            if (instaModel.Status != CONSTANTS.E)
            {
                switch (vdsDataModel.ProcessName)
                {
                    case CONSTANTS.DataEngineering:
                        StartDataEngineering(vdsDataModel.InstaId, vdsDataModel.CorrelationId, vdsDataModel.CreatedByUser, vdsDataModel.ProblemType, null);
                        if (instaModel.Status == CONSTANTS.E)
                            instaModel.Message = CONSTANTS.ErrorDE;
                        break;

                    case CONSTANTS.ModelEngineering:
                        StartModelEngineering(vdsDataModel.InstaId, vdsDataModel.CorrelationId, vdsDataModel.CreatedByUser, vdsDataModel.ProblemType, null);
                        if (instaModel.Status == CONSTANTS.E)
                            instaModel.Message = CONSTANTS.ErrorME;
                        break;

                    case CONSTANTS.DeployModel:
                        DeployModel(vdsDataModel.InstaId, vdsDataModel.CorrelationId, vdsDataModel.CreatedByUser, vdsDataModel.ProblemType, null);
                        if (instaModel.Status == CONSTANTS.E)
                            instaModel.Message = CONSTANTS.ErrorDM;
                        break;

                    case CONSTANTS.DeleteModel:
                        instaModel = DeleteModel(vdsDataModel.InstaId, vdsDataModel.CorrelationId, vdsDataModel.CreatedByUser);
                        if (instaModel.Status == CONSTANTS.E)
                            instaModel.Message = CONSTANTS.ErrorDeleteModel;
                        break;
                }
            }
            return instaModel;
        }
        public InstaRegression StartModelTraining(VDSRegression vdsRegressionModel)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            instaRegression.UseCaseID = vdsRegressionModel.UseCaseID;
            if (string.IsNullOrEmpty(vdsRegressionModel.UseCaseID) || string.IsNullOrEmpty(vdsRegressionModel.CreatedByUser) || string.IsNullOrEmpty(vdsRegressionModel.ProcessName)
                || string.IsNullOrEmpty(vdsRegressionModel.ClientUID) || string.IsNullOrEmpty(vdsRegressionModel.DCID))
            {
                instaModel.ErrorMessage = Resource.IngrainResx.InputEmpty;
                instaModel.Status = CONSTANTS.E;
                instaModel.Message = Resource.IngrainResx.AttributesNull;
            }
            foreach (var models in vdsRegressionModel.ProblemTypeDetails)
            {
                _DBEncryptionRequired = CommonUtility.EncryptDB(models.CorrelationId, appSettings);
                switch (vdsRegressionModel.ProcessName)
                {
                    //case "DE":
                    //    StartDataEngineering(models.InstaID, models.CorrelationId, vdsRegressionModel.CreatedByUser, models.ProblemType, vdsRegressionModel.UseCaseID);
                    //    break;

                    //case "ME":
                    //    StartModelEngineering(models.InstaID, models.CorrelationId, vdsRegressionModel.CreatedByUser, models.ProblemType, vdsRegressionModel.UseCaseID);
                    //    break;

                    case "DM":
                        DeployModel(models.InstaID, models.CorrelationId, vdsRegressionModel.CreatedByUser, models.ProblemType, vdsRegressionModel.UseCaseID);
                        break;

                    case "DeleteModel":
                        instaModel = DeleteModel(models.InstaID, models.CorrelationId, vdsRegressionModel.CreatedByUser);
                        if (instaModel.Status == CONSTANTS.E)
                            instaModel.Message = "Error at Delete Model";
                        break;
                }
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), nameof(StartModelTraining), CONSTANTS.START + vdsRegressionModel.ProcessName + "--STARTING TIME--" + DateTime.Now.ToString(), string.Empty, string.Empty, vdsRegressionModel.ClientUID,vdsRegressionModel.DCID);
            if (vdsRegressionModel.ProcessName == CONSTANTS.DataEngineering)
                StartRegressionDataEngineering(vdsRegressionModel);
            if (vdsRegressionModel.ProcessName == CONSTANTS.ModelEngineering)
                StartRegressionModelEngineering(vdsRegressionModel);
            instaRegression.instaMLResponse = regressionResponse;
            stopwatch.Stop();
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), nameof(StartModelTraining), CONSTANTS.START + vdsRegressionModel.ProcessName + "--END TIME--" + stopwatch.ElapsedMilliseconds, string.Empty, string.Empty, vdsRegressionModel.ClientUID, vdsRegressionModel.DCID);
            return instaRegression;
        }
        public bool IsDataCurationComplete(string correlationId)
        {
            bool IsCompleted = false;
            List<string> columnsList = new List<string>();
            List<string> noDatatypeList = new List<string>();
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataCleanup);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.FeatureName).Exclude(CONSTANTS.Id);
            var resultData = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            JObject dataCuration = new JObject();
            if (resultData.Count > 0)
            {
                //decrypt db data
                if (_DBEncryptionRequired)
                    resultData[0][CONSTANTS.FeatureName] = BsonDocument.Parse(_encryptionDecryption.Decrypt(resultData[0][CONSTANTS.FeatureName].AsString));

                dataCuration = JObject.Parse(resultData[0].ToString());
                foreach (var column in dataCuration[CONSTANTS.FeatureName].Children())
                {
                    JProperty property = column as JProperty;
                    columnsList.Add(property.Name.ToString());
                }
                foreach (var column in columnsList)
                {
                    bool datatypeExist = false;
                    foreach (var item in dataCuration[CONSTANTS.FeatureName][column][CONSTANTS.Datatype].Children())
                    {
                        if (item != null)
                        {
                            JProperty property = item as JProperty;
                            if (property.Name != CONSTANTS.Select_Option)
                            {
                                if (property.Value.ToString() == CONSTANTS.True)
                                {
                                    datatypeExist = true;
                                    IsCompleted = true;
                                }
                            }
                            else
                            {
                                //string columnToUpdate = string.Format(CONSTANTS.SelectOption, column);
                                //var updateField = Builders<BsonDocument>.Update.Set(columnToUpdate, CONSTANTS.False);
                                //var updateResult = collection.UpdateOne(filter, updateField);

                                dataCuration[CONSTANTS.FeatureName][column][CONSTANTS.Datatype]["Select Option"] = CONSTANTS.False;
                                if (_DBEncryptionRequired)
                                {
                                    var updateField = Builders<BsonDocument>.Update.Set(CONSTANTS.FeatureName, _encryptionDecryption.Encrypt(dataCuration[CONSTANTS.FeatureName].ToString(Formatting.None)));
                                    var updateResult = collection.UpdateOne(filter, updateField);
                                }
                                else
                                {
                                    var Featuredata = BsonDocument.Parse(dataCuration[CONSTANTS.FeatureName].ToString());
                                    var updateField = Builders<BsonDocument>.Update.Set(CONSTANTS.FeatureName, Featuredata);
                                    var updateResult = collection.UpdateOne(filter, updateField);
                                }

                            }

                        }
                    }
                    if (!datatypeExist)
                    {
                        //string columnToUpdate = string.Format(CONSTANTS.DatatypeCategory, column);
                        //var updateField = Builders<BsonDocument>.Update.Set(columnToUpdate, CONSTANTS.True);
                        //var updateResult = collection.UpdateOne(filter, updateField);

                        dataCuration[CONSTANTS.FeatureName][column][CONSTANTS.Datatype][CONSTANTS.category] = CONSTANTS.True;
                        if (_DBEncryptionRequired)
                        {
                            var updateField = Builders<BsonDocument>.Update.Set(CONSTANTS.FeatureName, _encryptionDecryption.Encrypt(dataCuration[CONSTANTS.FeatureName].ToString(Formatting.None)));
                            var updateResult = collection.UpdateOne(filter, updateField);
                        }
                        else
                        {
                            var Featuredata = BsonDocument.Parse(dataCuration[CONSTANTS.FeatureName].ToString());
                            var updateField = Builders<BsonDocument>.Update.Set(CONSTANTS.FeatureName, Featuredata);
                            var updateResult = collection.UpdateOne(filter, updateField);
                        }
                        IsCompleted = true;
                    }
                }
            }
            return IsCompleted;
        }
        private List<BsonDocument> GetPreprocessExistData(string correlationId)
        {
            _preProcessDTO.CorrelationId = correlationId;
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var prePropcessProjection = Builders<BsonDocument>.Projection.Include(CONSTANTS.Filters).Include(CONSTANTS.MissingValues).Include(CONSTANTS.DataEncoding).Include(CONSTANTS.DataModification).Include(CONSTANTS.DataTransformationApplied).Include(CONSTANTS.TargetColumn).Exclude(CONSTANTS.Id);
            var dataPreprocessCollection = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataProcessing);
            var preprocessDataExist = dataPreprocessCollection.Find(filter).Project<BsonDocument>(prePropcessProjection).ToList();
            return preprocessDataExist;
        }
        public bool CreatePreprocess(string correlationId, string userId, string problemType, string instaId)
        {
            PreProcessModelDTO preProcessModel = new PreProcessModelDTO
            {
                CorrelationId = correlationId,
                ModelType = problemType
            };
            preProcessModel.ModelType = problemType;
            var preprocessDataExist = GetPreprocessExistData(correlationId);
            if (preprocessDataExist.Count > 0)
            {
                return insertSuccess = true;
            }
            _preProcessDTO.DataTransformationApplied = true;
            _preProcessDTO.CorrelationId = correlationId;
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            string processData = string.Empty;

            var dataCleanupCollection = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataCleanup);
            var filterCollection = _database.GetCollection<BsonDocument>(CONSTANTS.DataCleanUPFilteredData);
            var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.FeatureName).Exclude(CONSTANTS.Id);
            var filteredData = new List<BsonDocument>();
            List<string> columnsList = new List<string>();
            List<string> categoricalColumns = new List<string>();
            List<string> missingColumns = new List<string>();
            List<string> numericalColumns = new List<string>();
            JObject serializeData = new JObject();
            bool DBEncryptionRequired = CommonUtility.EncryptDB(correlationId, appSettings);
            filteredData = dataCleanupCollection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (filteredData.Count > 0)
            {
                if (DBEncryptionRequired)
                    filteredData[0][CONSTANTS.FeatureName] = BsonDocument.Parse(_encryptionDecryption.Decrypt(filteredData[0][CONSTANTS.FeatureName].AsString));

                serializeData = JObject.Parse(filteredData[0].ToString());
                //Taking all the Columns
                foreach (var features in serializeData[CONSTANTS.FeatureName].Children())
                {
                    JProperty j = features as JProperty;
                    columnsList.Add(j.Name);
                }
                //Get the Categorical Columns and Numerical Columns
                foreach (var item in columnsList)
                {
                    foreach (JToken attributes in serializeData[CONSTANTS.FeatureName][item][CONSTANTS.Datatype].Children())
                    {
                        var property = attributes as JProperty;
                        var missingValues = serializeData[CONSTANTS.FeatureName][item][CONSTANTS.Missing_Values];
                        double value = (double)missingValues;
                        if (property != null && property.Name == CONSTANTS.category && property.Value.ToString() == CONSTANTS.True)
                        {
                            categoricalColumns.Add(item);
                            if (value > 0)
                                missingColumns.Add(item);
                        }
                        if (property != null && (property.Name == CONSTANTS.float64 || property.Name == CONSTANTS.int64) && property.Value.ToString() == CONSTANTS.True)
                        {
                            if (value > 0)
                                numericalColumns.Add(item);
                        }
                    }
                }
                //Get DataModificationData
                GetModifications(correlationId);
                //Getting the Data Encoding Data
                GetDataEncodingValues(categoricalColumns, serializeData);

                //This code for filters to be applied
                var uniqueValueProjection = Builders<BsonDocument>.Projection.Include(CONSTANTS.ColumnUniqueValues).Include(CONSTANTS.target_variable).Exclude(CONSTANTS.Id);
                var filteredResult = filterCollection.Find(filter).Project<BsonDocument>(uniqueValueProjection).ToList();
                JObject uniqueData = new JObject();
                if (filteredResult.Count > 0)
                {
                    if (DBEncryptionRequired)
                        filteredResult[0][CONSTANTS.ColumnUniqueValues] = BsonDocument.Parse(_encryptionDecryption.Decrypt(filteredResult[0][CONSTANTS.ColumnUniqueValues].AsString));

                    _preProcessDTO.TargetColumn = filteredResult[0][CONSTANTS.target_variable].ToString();
                    uniqueData = JObject.Parse(filteredResult[0].ToString());
                    //Getting the Missing Values and Filters Data
                    GetMissingAndFiltersData(missingColumns, categoricalColumns, numericalColumns, uniqueData);
                    InsertToPreprocess(preProcessModel, instaId);
                }
            }
            return insertSuccess;
        }
        public bool IsDataTransformationComplete(string correlationId)
        {
            bool IsCompleted = false;
            List<string> columnsList = new List<string>();
            List<string> noDatatypeList = new List<string>();
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataCleanup);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.FeatureName).Exclude(CONSTANTS.Id);
            var resultData = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            JObject dataCuration = new JObject();
            bool DBEncryptionRequired = CommonUtility.EncryptDB(correlationId, appSettings);
            if (resultData.Count > 0)
            {
                //decrypt db values
                if (DBEncryptionRequired)
                    resultData[0][CONSTANTS.FeatureName] = BsonDocument.Parse(_encryptionDecryption.Decrypt(resultData[0][CONSTANTS.FeatureName].AsString));

                dataCuration = JObject.Parse(resultData[0].ToString());
                foreach (var column in dataCuration[CONSTANTS.FeatureName].Children())
                {
                    JProperty property = column as JProperty;
                    columnsList.Add(property.Name.ToString());
                }
                foreach (var column in columnsList)
                {
                    bool datatypeExist = false;
                    foreach (var item in dataCuration[CONSTANTS.FeatureName][column][CONSTANTS.Datatype].Children())
                    {
                        if (item != null)
                        {
                            JProperty property = item as JProperty;
                            if (property.Value.ToString() == CONSTANTS.True)
                            {
                                datatypeExist = true;
                            }
                        }
                    }
                    if (!datatypeExist)
                    {
                        string columnToUpdate = string.Format(CONSTANTS.DatatypeCategory, column);
                        var updateField = Builders<BsonDocument>.Update.Set(columnToUpdate, CONSTANTS.True);
                        var updateResult = collection.UpdateOne(filter, updateField);
                    }
                }
            }
            return IsCompleted;
        }
        private void GetModifications(string correlationId)
        {

            List<string> binningcolumnsList = new List<string>();
            List<string> recommendedcolumnsList = new List<string>();
            List<string> columnsList = new List<string>();
            Dictionary<string, Dictionary<string, Dictionary<string, string>>> recommendedColumns = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();

            Dictionary<string, Dictionary<string, Dictionary<string, string>>> columnBinning = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();
            Dictionary<string, string> prescriptionData = new Dictionary<string, string>();
            JObject serializeData = new JObject();


            var dataCleanupCollection = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataCleanup);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.FeatureName).Exclude(CONSTANTS.Id);
            var filteredData = dataCleanupCollection.Find(filter).Project<BsonDocument>(projection).ToList();
            bool DBEncryptionRequired = CommonUtility.EncryptDB(correlationId, appSettings);
            if (filteredData.Count > 0)
            {
                //decrypt db values
                if (DBEncryptionRequired)
                    filteredData[0][CONSTANTS.FeatureName] = BsonDocument.Parse(_encryptionDecryption.Decrypt(filteredData[0][CONSTANTS.FeatureName].AsString));

                serializeData = JObject.Parse(filteredData[0].ToString());
                //Taking all the Columns
                var featureExist = serializeData[CONSTANTS.FeatureName];
                if (featureExist != null)
                {
                    foreach (var features in serializeData[CONSTANTS.FeatureName].Children())
                    {
                        JProperty j = features as JProperty;
                        columnsList.Add(j.Name);
                    }
                    foreach (var item in columnsList)
                    {
                        Dictionary<string, Dictionary<string, string>> binningColumns2 = new Dictionary<string, Dictionary<string, string>>();
                        Dictionary<string, Dictionary<string, string>> binningColumns3 = new Dictionary<string, Dictionary<string, string>>();
                        Dictionary<string, string> removeImbalancedColumns = new Dictionary<string, string>();

                        Dictionary<string, string> outlier = new Dictionary<string, string>();
                        Dictionary<string, string> skeweness = new Dictionary<string, string>();
                        Dictionary<string, Dictionary<string, string>> fields = new Dictionary<string, Dictionary<string, string>>();
                        var outData = serializeData[CONSTANTS.FeatureName][item][CONSTANTS.Outlier];
                        var skewData = serializeData[CONSTANTS.FeatureName][item][CONSTANTS.Skewness];
                        float outValue = (float)outData;
                        string skewValue = (string)skewData;
                        var imbalanced = serializeData[CONSTANTS.FeatureName][item][CONSTANTS.ImBalanced];
                        string imbalancedValue = (string)imbalanced;
                        if (imbalancedValue == CONSTANTS.One)
                        {
                            JProperty jProperty1 = null;
                            string recommendation = string.Format(CONSTANTS.Recommendation, item);
                            var imbalancedColumns = serializeData[CONSTANTS.FeatureName][item][CONSTANTS.BinningValues];

                            foreach (var child1 in imbalancedColumns.Children())
                            {
                                Dictionary<string, string> binningColumns1 = new Dictionary<string, string>();
                                jProperty1 = child1 as JProperty;
                                foreach (var child2 in jProperty1.Children())
                                {
                                    if (child2 != null)
                                    {
                                        binningColumns1.Add(CONSTANTS.SubCatName, child2[CONSTANTS.SubCatName].ToString().Trim());
                                        binningColumns1.Add(CONSTANTS.Value, child2[CONSTANTS.Value].ToString().Trim());
                                        List<string> list = new List<string> { CONSTANTS.Binning, CONSTANTS.NewName };
                                        foreach (var binning in list)
                                        {
                                            binningColumns1.Add(binning, CONSTANTS.False);
                                        }
                                        binningColumns2.Add(jProperty1.Name, binningColumns1);
                                    }
                                }
                            }
                            if (binningColumns2.Count > 0)
                            {
                                for (int i = 0; i < 2; i++)
                                {
                                    Dictionary<string, string> dict = new Dictionary<string, string>();
                                    if (i == 0)
                                    {
                                        dict.Add(CONSTANTS.ChangeRequest, CONSTANTS.InvertedComma);
                                        binningColumns2.Add(CONSTANTS.ChangeRequest, dict);
                                    }
                                    else
                                    {
                                        dict.Add(CONSTANTS.PChangeRequest, CONSTANTS.InvertedComma);
                                        binningColumns2.Add(CONSTANTS.PChangeRequest, dict);
                                    }
                                }
                            }
                            columnBinning.Add(item, binningColumns2);
                        }
                        else if (imbalancedValue == CONSTANTS.Two)
                        {
                            string removeColumndesc = string.Format(CONSTANTS.StringFormat, item);
                            removeImbalancedColumns.Add(item, removeColumndesc);
                        }
                        else if (imbalancedValue == CONSTANTS.Three)
                        {
                            string prescription = string.Format(CONSTANTS.StringFormat1, item);
                            prescriptionData.Add(item, prescription);
                        }
                        if (prescriptionData.Count > 0)
                            _preProcessDTO.Prescriptions = prescriptionData;

                        if (outValue > 0)
                        {
                            string strForm = string.Format(CONSTANTS.StringFormat2, item, outValue);
                            outlier.Add(CONSTANTS.Text, strForm);
                            string[] outliers = { CONSTANTS.Mean, CONSTANTS.Median, CONSTANTS.Mode, CONSTANTS.CustomValue, CONSTANTS.ChangeRequest, CONSTANTS.PChangeRequest };
                            for (int i = 0; i < outliers.Length; i++)
                            {
                                if (i == 3)
                                {
                                    outlier.Add(outliers[i], CONSTANTS.InvertedComma);
                                }
                                else if (i == 4 || i == 5)
                                {
                                    outlier.Add(outliers[i], CONSTANTS.InvertedComma);
                                }
                                else
                                {
                                    outlier.Add(outliers[i], CONSTANTS.False);
                                }
                            }
                        }
                        if (skewValue == CONSTANTS.Yes)
                        {
                            string strForm = string.Format(CONSTANTS.StringFormat3, item);
                            skeweness.Add(CONSTANTS.Skeweness, strForm);
                            string[] skewnessArray = { CONSTANTS.BoxCox, CONSTANTS.Reciprocal, CONSTANTS.Log, CONSTANTS.ChangeRequest, CONSTANTS.PChangeRequest };
                            for (int i = 0; i < skewnessArray.Length; i++)
                            {
                                if (i == 3 || i == 4)
                                {
                                    skeweness.Add(skewnessArray[i], CONSTANTS.InvertedComma);
                                }
                                else
                                {
                                    skeweness.Add(skewnessArray[i], CONSTANTS.False);
                                }
                            }
                        }
                        if (outlier.Count > 0)
                            fields.Add(CONSTANTS.Outlier, outlier);
                        if (skeweness.Count > 0)
                            fields.Add(CONSTANTS.Skewness, skeweness);
                        if (removeImbalancedColumns.Count > 0)
                            fields.Add(CONSTANTS.RemoveColumn, removeImbalancedColumns);

                        if (fields.Count > 0)
                        {
                            recommendedColumns.Add(item, fields);
                        }
                    }
                }
                if (columnBinning.Count > 0)
                    _preProcessDTO.ColumnBinning = columnBinning;
                if (recommendedColumns.Count > 0)
                    _preProcessDTO.RecommendedColumns = recommendedColumns;
            }
        }
        private void InsertToPreprocess(PreProcessModelDTO preProcessModel, string instaId)
        {
            string categoricalJson = string.Empty;
            string missingValuesJson = string.Empty;
            string numericJson = string.Empty;
            string dataEncodingJson = string.Empty;

            if (_preProcessDTO.CategoricalData != null || _preProcessDTO.NumericalData != null || _preProcessDTO.DataEncodeData != null || _preProcessDTO.ColumnBinning != null)
            {
                JObject outlierData = new JObject();
                JObject prescriptionData = new JObject();
                JObject binningData = new JObject();
                //DataModification Insertion Format Start
                var recommendedColumnsData = Newtonsoft.Json.JsonConvert.SerializeObject(_preProcessDTO.RecommendedColumns);
                if (!string.IsNullOrEmpty(recommendedColumnsData) && recommendedColumnsData != CONSTANTS.Null)
                    outlierData = JObject.Parse(recommendedColumnsData);
                var columnBinning = Newtonsoft.Json.JsonConvert.SerializeObject(_preProcessDTO.ColumnBinning);
                if (!string.IsNullOrEmpty(columnBinning) && columnBinning != CONSTANTS.Null)
                    binningData = JObject.Parse(columnBinning);
                JObject binningObject = new JObject();
                if (binningData != null)
                    binningObject[CONSTANTS.ColumnBinning] = JObject.FromObject(binningData);

                var prescription = Newtonsoft.Json.JsonConvert.SerializeObject(_preProcessDTO.Prescriptions);
                if (!string.IsNullOrEmpty(prescription) && prescription != CONSTANTS.Null)
                    prescriptionData = JObject.Parse(prescription);
                //DataModification Insertion Format End

                categoricalJson = Newtonsoft.Json.JsonConvert.SerializeObject(_preProcessDTO.CategoricalData);
                missingValuesJson = Newtonsoft.Json.JsonConvert.SerializeObject(_preProcessDTO.MisingValuesData);
                numericJson = Newtonsoft.Json.JsonConvert.SerializeObject(_preProcessDTO.NumericalData);
                dataEncodingJson = Newtonsoft.Json.JsonConvert.SerializeObject(_preProcessDTO.DataEncodeData);

                JObject missingValuesObject = new JObject();
                JObject categoricalObject = new JObject();
                JObject numericObject = new JObject();
                JObject encodedData = new JObject();
                if (!string.IsNullOrEmpty(categoricalJson) && categoricalJson != CONSTANTS.Null)
                    categoricalObject = JObject.Parse(categoricalJson);
                if (!string.IsNullOrEmpty(numericJson) && numericJson != CONSTANTS.Null)
                    numericObject = JObject.Parse(numericJson);
                if (!string.IsNullOrEmpty(missingValuesJson) && missingValuesJson != null)
                    missingValuesObject = JObject.Parse(missingValuesJson);
                missingValuesObject.Merge(numericObject, new Newtonsoft.Json.Linq.JsonMergeSettings
                {
                    MergeArrayHandling = Newtonsoft.Json.Linq.MergeArrayHandling.Union
                });
                if (!string.IsNullOrEmpty(dataEncodingJson))
                    encodedData = JObject.Parse(dataEncodingJson);

                Dictionary<string, string> smoteFlags = new Dictionary<string, string>();
                smoteFlags.Add(CONSTANTS.Flag, CONSTANTS.False);
                smoteFlags.Add(CONSTANTS.ChangeRequest, CONSTANTS.False);
                smoteFlags.Add(CONSTANTS.PChangeRequest, CONSTANTS.False);

                var smoteTest = Newtonsoft.Json.JsonConvert.SerializeObject(smoteFlags);
                JObject smoteData = new JObject();
                smoteData = JObject.Parse(smoteTest);

                JObject processData = new JObject
                {
                    [CONSTANTS.Id] = Guid.NewGuid(),
                    [CONSTANTS.CorrelationId] = _preProcessDTO.CorrelationId
                };
                if (!string.IsNullOrEmpty(_preProcessDTO.Flag))
                    _preProcessDTO.Flag = CONSTANTS.False;
                processData[CONSTANTS.Flag] = _preProcessDTO.Flag;
                //Removing the Target column having lessthan 2 values..important
                bool removeTargetColumn = false;
                if (categoricalObject != null && categoricalObject.ToString() != CONSTANTS.CurlyBraces)
                {
                    if (categoricalObject[_preProcessDTO.TargetColumn] != null)
                    {
                        if (categoricalObject[_preProcessDTO.TargetColumn].Children().Count() <= 4)
                        {
                            removeTargetColumn = true;
                        }
                        if (removeTargetColumn)
                        {
                            JObject header = (JObject)categoricalObject;
                            header.Property(_preProcessDTO.TargetColumn).Remove();
                        }
                    }
                }

                processData[CONSTANTS.Filters] = JObject.FromObject(categoricalObject);
                if (missingValuesObject != null)
                    processData[CONSTANTS.MissingValues] = JObject.FromObject(missingValuesObject);
                if (encodedData != null)
                    processData[CONSTANTS.DataEncoding] = JObject.FromObject(encodedData);
                if (binningObject != null)
                    processData[CONSTANTS.DataModification] = JObject.FromObject(binningObject);
                if (outlierData != null)
                    processData[CONSTANTS.DataModification][CONSTANTS.Features] = JObject.FromObject(outlierData);
                if (prescriptionData != null)
                    processData[CONSTANTS.DataModification][CONSTANTS.Prescriptions] = JObject.FromObject(prescriptionData);
                processData[CONSTANTS.TargetColumn] = _preProcessDTO.TargetColumn;
                JObject InterpolationObject = new JObject();
                processData[CONSTANTS.DataModification][CONSTANTS.Features][CONSTANTS.Interpolation] = JObject.FromObject(InterpolationObject);
                if (preProcessModel.ModelType == CONSTANTS.TimeSeries)
                    processData[CONSTANTS.DataModification][CONSTANTS.Features][CONSTANTS.Interpolation] = CONSTANTS.Linear;
                processData[CONSTANTS.Smote] = smoteData;
                processData[CONSTANTS.InstaId] = instaId;
                processData[CONSTANTS.DataTransformationApplied] = _preProcessDTO.DataTransformationApplied;
                processData[CONSTANTS.CreatedOn] = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                processData[CONSTANTS.ModifiedOn] = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);

                bool DBEncryptionRequired = CommonUtility.EncryptDB(_preProcessDTO.CorrelationId, appSettings);
                if (_DBEncryptionRequired)
                {
                    processData[CONSTANTS.DataModification] = _encryptionDecryption.Encrypt(processData[CONSTANTS.DataModification].ToString(Formatting.None));
                    processData[CONSTANTS.MissingValues] = _encryptionDecryption.Encrypt(processData[CONSTANTS.MissingValues].ToString(Formatting.None));
                    processData[CONSTANTS.Filters] = _encryptionDecryption.Encrypt(processData[CONSTANTS.Filters].ToString(Formatting.None));
                    //if (processData.ContainsKey(CONSTANTS.CreatedByUser) && !string.IsNullOrEmpty(Convert.ToString(processData[CONSTANTS.CreatedByUser])))
                    //    processData[CONSTANTS.CreatedByUser] = _encryptionDecryption.Encrypt(Convert.ToString(processData[CONSTANTS.CreatedByUser]));
                    //if (processData.ContainsKey(CONSTANTS.ModifiedByUser) && !string.IsNullOrEmpty(Convert.ToString(processData[CONSTANTS.ModifiedByUser])))
                    //    processData[CONSTANTS.ModifiedByUser] = _encryptionDecryption.Encrypt(Convert.ToString(processData[CONSTANTS.ModifiedByUser]));
                }
                var collection = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataProcessing);
                var insertdoc = BsonSerializer.Deserialize<BsonDocument>(processData.ToString());
                collection.InsertOne(insertdoc);
                insertSuccess = true;
            }
        }

        /// <summary>
        /// Get the Missing Values and Filter Values
        /// </summary>
        /// <param name="categoricalColumns"></param>
        /// <param name="numericalColumns"></param>
        /// <param name="uniqueData"></param>
        /// <param name="categoricalData"></param>
        /// <param name="numericalData"></param>
        private void GetMissingAndFiltersData(List<string> missingColumns, List<string> categoricalColumns, List<string> numericalColumns, JObject uniqueData)
        {
            var missingData = new Dictionary<string, Dictionary<string, dynamic>>();
            var categoricalDictionary = new Dictionary<string, Dictionary<string, string>>();
            var missingDictionary = new Dictionary<string, Dictionary<string, string>>();
            var dataNumerical = new Dictionary<string, Dictionary<string, dynamic>>();

            foreach (var column in categoricalColumns)
            {
                Dictionary<string, string> fieldDictionary = new Dictionary<string, string>();
                foreach (JToken value in uniqueData[CONSTANTS.ColumnUniqueValues][column].Children())
                {
                    if (!string.IsNullOrEmpty(Convert.ToString(value)))
                        fieldDictionary.Add(value.ToString().Replace(CONSTANTS.Dot, CONSTANTS.u2024).Replace(CONSTANTS.r_n, CONSTANTS.EmptySpace).Replace(CONSTANTS.slash, CONSTANTS.InvertedComma).Replace(CONSTANTS.t, CONSTANTS.EmptySpace), CONSTANTS.False);
                }
                if (fieldDictionary.Count > 0)
                {
                    fieldDictionary.Add(CONSTANTS.ChangeRequest, CONSTANTS.InvertedComma);
                    fieldDictionary.Add(CONSTANTS.PChangeRequest, CONSTANTS.InvertedComma);
                    categoricalDictionary.Add(column, fieldDictionary);
                }
            }
            _preProcessDTO.CategoricalData = categoricalDictionary;

            foreach (var column in missingColumns)
            {
                Dictionary<string, dynamic> fieldDictionary = new Dictionary<string, dynamic>();
                foreach (JToken value in uniqueData[CONSTANTS.ColumnUniqueValues][column].Children())
                {
                    if (!string.IsNullOrEmpty(Convert.ToString(value)))
                        fieldDictionary.Add(value.ToString().Replace(CONSTANTS.Dot, CONSTANTS.u2024).Replace(CONSTANTS.r_n, CONSTANTS.EmptySpace).Replace(CONSTANTS.slash, CONSTANTS.InvertedComma).Replace(CONSTANTS.t, CONSTANTS.EmptySpace), CONSTANTS.False);
                }
                if (fieldDictionary.Count > 0)
                {
                    fieldDictionary.Add(CONSTANTS.ChangeRequest, CONSTANTS.True);
                    fieldDictionary.Add(CONSTANTS.PChangeRequest, CONSTANTS.InvertedComma);
                    fieldDictionary.Add(CONSTANTS.CustomValue, CONSTANTS.NumericZero);
                    missingData.Add(column, fieldDictionary);
                }

            }
            _preProcessDTO.MisingValuesData = missingData;

            //Numerical Columns Fetching data

            Dictionary<string, string> numericalDictionary = new Dictionary<string, string>();
            string[] numericalValues = { CONSTANTS.Mean, CONSTANTS.Median, CONSTANTS.Mode, CONSTANTS.CustomValue };
            foreach (var column in numericalColumns)
            {
                var value = uniqueData[CONSTANTS.ColumnUniqueValues][column];
                var numericDictionary = new Dictionary<string, dynamic>();
                if (!string.IsNullOrEmpty(Convert.ToString(value)))
                {
                    foreach (var numericColumnn in numericalValues)
                    {
                        if (numericColumnn == CONSTANTS.CustomValue)
                        {
                            numericDictionary.Add(numericColumnn, CONSTANTS.NumericZero);
                        }
                        else
                        {
                            numericDictionary.Add(numericColumnn, CONSTANTS.False);
                            //if (numericColumnn == CONSTANTS.Mean)
                            //{
                            //    numericDictionary.Add(numericColumnn, CONSTANTS.True);
                            //}
                            //else
                            //{
                            //    numericDictionary.Add(numericColumnn, CONSTANTS.False);
                            //}

                        }
                    }
                    if (numericDictionary.Count > 0)
                    {
                        numericDictionary.Add(CONSTANTS.ChangeRequest, CONSTANTS.True);
                        numericDictionary.Add(CONSTANTS.PChangeRequest, CONSTANTS.InvertedComma);
                        dataNumerical.Add(column, numericDictionary);
                    }
                }
            }
            _preProcessDTO.NumericalData = dataNumerical;
        }

        /// <summary>
        /// Get preprocess Data Encode Values
        /// </summary>
        /// <param name="categoricalColumns"></param>
        /// <param name="serializeData"></param>
        /// <param name="encodedData"></param>
        private void GetDataEncodingValues(List<string> categoricalColumns, JObject serializeData)
        {
            var encodingData = new Dictionary<string, Dictionary<string, string>>();
            foreach (var column in categoricalColumns)
            {
                var dataEncodingData = new Dictionary<string, string>();
                foreach (JToken scale in serializeData[CONSTANTS.FeatureName][column][CONSTANTS.Scale].Children())
                {
                    if (scale is JProperty property && property.Value.ToString() == CONSTANTS.True)
                    {
                        dataEncodingData.Add(CONSTANTS.Attribute, property.Name);
                        dataEncodingData.Add(CONSTANTS.encoding, CONSTANTS.LabelEncoding);
                    }
                }
                if (dataEncodingData.Count > 0)
                {
                    dataEncodingData.Add(CONSTANTS.ChangeRequest, CONSTANTS.True);
                    dataEncodingData.Add(CONSTANTS.PChangeRequest, CONSTANTS.InvertedComma);
                    encodingData.Add(column, dataEncodingData);
                }

            }
            _preProcessDTO.DataEncodeData = encodingData;
        }
        private bool IsDeployModelComplete(string correlationId, RecommedAITrainedModel trainedModel, string problemType)
        {
            bool IsCompleted = false;
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var resultData = collection.Find(filter).ToList();
            if (resultData.Count > 0)
            {
                JObject data = JObject.Parse(trainedModel.TrainedModel[0].ToString());
                string probTypeTS = CONSTANTS.TimeSeries;
                bool trainedProblemType = probTypeTS.Equals(data[CONSTANTS.ProblemType].ToString(), StringComparison.InvariantCultureIgnoreCase);
                //string[] linkedApps = new string[] { CONSTANTS.VDS_SI };
                string modelURL = trainedProblemType ? string.Format(appSettings.Value.foreCastModel, correlationId, data[CONSTANTS.Frequency].ToString()) :
                    string.Format(appSettings.Value.publishURL + CONSTANTS.Zero, correlationId);
                var builder = Builders<BsonDocument>.Update;
                var update = builder.Set(CONSTANTS.Accuracy, problemType == CONSTANTS.Classification ? (double)data[CONSTANTS.Accuracy] : (double)data[CONSTANTS.r2ScoreVal][CONSTANTS.error_rate])
                    .Set(CONSTANTS.ModelURL, modelURL)
                    //.Set(CONSTANTS.VDSLink, appSettings.Value.VDSLink)
                    //.Set(CONSTANTS.LinkedApps, linkedApps)
                    .Set(CONSTANTS.Status, CONSTANTS.Deployed)
                    .Set(CONSTANTS.WebServices, CONSTANTS.LinkWithWebApp)
                    .Set(CONSTANTS.DeployedDate, DateTime.Now.ToString(CONSTANTS.DateHoursFormat))
                    .Set(CONSTANTS.IsPrivate, true)
                    .Set(CONSTANTS.IsModelTemplate, false)
                    .Set(CONSTANTS.ModelVersion, data[CONSTANTS.modelName].ToString());
                if (trainedProblemType)
                {
                    update = update.Set(CONSTANTS.Frequency, data[CONSTANTS.Frequency].ToString()).Set(CONSTANTS.TrainedModelId, data[CONSTANTS.Id].ToString());
                }
                collection.UpdateMany(filter, update);
                IsCompleted = true;
            }

            return IsCompleted;
        }


        private AppIntegration GetVDSAppID(string appName)
        {
            var appCollection = _database.GetCollection<AppIntegration>(CONSTANTS.AppIntegration);
            var appFilter = Builders<AppIntegration>.Filter.Eq("ApplicationName", appName) & Builders<AppIntegration>.Filter.Eq("isDefault", true);
            var projection = Builders<AppIntegration>.Projection.Exclude("_id");
            var response = appCollection.Find(appFilter).Project<AppIntegration>(projection).FirstOrDefault();
            if (response != null)
                return response;
            else
                return null;
        }

        private void CreateInstaModel(string problemType, string correlationId, string source)
        {
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
            string[] arr = null;
            string appId = null;
            string category = null;
            string appLink = null;
            if (source == CONSTANTS.VDS_AIOPS)
            {
                arr = new string[] { CONSTANTS.VDS_AIOPS };
                category = "AM";
                var app = GetVDSAppID(CONSTANTS.VDS_AIOPS);
                appId = app.ApplicationID;
                appLink = app.BaseURL;

            }
            else if (source == CONSTANTS.VDS)
            {
                arr = new string[] { CONSTANTS.VDS };
                category = "AM";
                var app = GetVDSAppID(CONSTANTS.VDS);
                appId = app.ApplicationID;
                appLink = app.BaseURL;

            }
            else
            {
                arr = new string[] { CONSTANTS.VDS_SI };
                var app = GetVDSAppID(CONSTANTS.VDS_SI);
                appId = app.ApplicationID;
                appLink = appSettings.Value.VDSLink;
            }
            if (problemType == CONSTANTS.Regression)
            {
                DeployModelsDto deployModel = new DeployModelsDto
                {
                    _id = Guid.NewGuid().ToString(),
                    CorrelationId = correlationId,
                    ModelName = ingestModel.ModelName,
                    Status = CONSTANTS.InProgress,
                    ClientUId = ingestModel.ClientUId,
                    DeliveryConstructUID = ingestModel.DeliveryConstructUID,
                    DataSource = ingestModel.DataSource,
                    SourceName = CONSTANTS.InstaML,
                    DeployedDate = null,
                    LinkedApps = arr,
                    AppId = appId,
                    Category = category,
                    ModelVersion = null,
                    ModelType = ingestModel.ProblemType,
                    VDSLink = appLink,
                    InputSample = null,
                    UseCaseID = ingestModel.UseCaseID,
                    IsPrivate = true,
                    IsModelTemplate = false,
                    DBEncryptionRequired = appSettings.Value.DBEncryption,
                    TrainedModelId = null,
                    Frequency = null,
                    InstaId = ingestModel.InstaId,
                    CreatedByUser = appSettings.Value.DBEncryption ? _encryptionDecryption.Encrypt(Convert.ToString(ingestModel.CreatedByUser)) : ingestModel.CreatedByUser,
                    CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                    ModifiedByUser = appSettings.Value.DBEncryption ? _encryptionDecryption.Encrypt(Convert.ToString(ingestModel.ModifiedByUser)) : ingestModel.ModifiedByUser,
                    ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat)
                };
                var jsonColumns = JsonConvert.SerializeObject(deployModel);
                var insertBsonColumns = BsonSerializer.Deserialize<BsonDocument>(jsonColumns);
                collection.InsertOne(insertBsonColumns);
            }
            else
            {
                DeployModelsDto deployModel = new DeployModelsDto
                {
                    _id = Guid.NewGuid().ToString(),
                    CorrelationId = correlationId,
                    ModelName = timeSeriesModel.ModelName,
                    Status = CONSTANTS.InProgress,
                    ClientUId = timeSeriesModel.ClientUId,
                    DeliveryConstructUID = timeSeriesModel.DeliveryConstructUID,
                    DataSource = timeSeriesModel.DataSource,
                    DeployedDate = null,
                    LinkedApps = arr,
                    AppId = appId,
                    Category = category,
                    InstaId = timeSeriesModel.InstaId,
                    ModelVersion = null,
                    SourceName = CONSTANTS.InstaML,
                    ModelType = timeSeriesModel.ProblemType,
                    VDSLink = appLink,
                    UseCaseID = timeSeriesModel.UseCaseID,
                    InputSample = null,
                    IsPrivate = true,
                    IsModelTemplate = false,
                    DBEncryptionRequired = appSettings.Value.DBEncryption,
                    TrainedModelId = null,
                    Frequency = timeSeriesModel.Frequency,
                    CreatedByUser = appSettings.Value.DBEncryption ? _encryptionDecryption.Encrypt(Convert.ToString(timeSeriesModel.CreatedByUser)) : timeSeriesModel.CreatedByUser,
                    CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                    ModifiedByUser = appSettings.Value.DBEncryption ? _encryptionDecryption.Encrypt(Convert.ToString(timeSeriesModel.ModifiedByUser)) : timeSeriesModel.ModifiedByUser,
                    ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat)
                };
                var jsonColumns = JsonConvert.SerializeObject(deployModel);
                var insertBsonColumns = BsonSerializer.Deserialize<BsonDocument>(jsonColumns);
                collection.InsertOne(insertBsonColumns);
            }
        }
        public void InsertBusinessProblem(IngestModel ingestModel)
        {
            bool DBEncryptionRequired = CommonUtility.EncryptDB(ingestModel.CorrelationId, appSettings);
            if (DBEncryptionRequired)
            {
                if (!string.IsNullOrEmpty(Convert.ToString(ingestModel.UserId)))
                {
                    ingestModel.UserId = _encryptionDecryption.Encrypt(Convert.ToString(ingestModel.UserId));
                }
            }
            //JObject jObject = new JObject();                 
            BusinessProblemInstaDTO businessProblemData = new BusinessProblemInstaDTO
            {
                BusinessProblems = ingestModel.ModelName,
                TargetColumn = ingestModel.TargetColumn,
                InputColumns = ingestModel.InputColumns,
                AvailableColumns = ingestModel.AvailableColumns,
                CorrelationId = ingestModel.CorrelationId,
                TimeSeries = "{}",
                TargetUniqueIdentifier = ingestModel.UniqueIdentifier,
                ProblemType = ingestModel.ProblemType,
                CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                CreatedByUser = ingestModel.UserId,
                ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                ModifiedByUser = ingestModel.UserId,
                _id = Guid.NewGuid().ToString(),
                UseCaseID = ingestModel.UseCaseID,
                InstaId = ingestModel.InstaId,
                InstaURL = ingestModel.URL,
                ClientUId = ingestModel.ClientUId,
                DeliveryConstructUID = ingestModel.DeliveryConstructUID

            };
            var jsonColumns = Newtonsoft.Json.JsonConvert.SerializeObject(businessProblemData);
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.PSBusinessProblem);
            var insertBsonColumns = BsonSerializer.Deserialize<BsonDocument>(jsonColumns);
            collection.InsertOne(insertBsonColumns);
        }
        public void InsertBusinessProblem(TimeSeriesModel timeSeriesModel)
        {
            string[] allColumns = new string[] { timeSeriesModel.TargetColumn, timeSeriesModel.Dimension };
            string data = CONSTANTS.Data;
            JObject TimeSeries = JObject.Parse(data);
            TimeSeries[CONSTANTS.TimeSeriesColumn] = timeSeriesModel.Dimension;
            JObject jObject = new JObject();
            int i = 0;
            foreach (var item in TimeSeries[CONSTANTS.Frequency].Children())
            {
                if (item != null)
                {
                    JProperty jProperty = item as JProperty;
                    TimeSeries timeSeries = Newtonsoft.Json.JsonConvert.DeserializeObject<TimeSeries>(jProperty.Value.ToString());
                    if (timeSeries.Name == timeSeriesModel.Frequency)
                    {
                        TimeSeries[CONSTANTS.Frequency][i.ToString()][CONSTANTS.Steps] = timeSeriesModel.FrequencySteps;
                        jObject = TimeSeries;
                    }
                }
                i++;
            }
            bool DBEncryptionRequired = CommonUtility.EncryptDB(timeSeriesModel.CorrelationId, appSettings);
            if (DBEncryptionRequired)
            {
                if (!string.IsNullOrEmpty(Convert.ToString(timeSeriesModel.UserId)))
                {
                    timeSeriesModel.UserId = _encryptionDecryption.Encrypt(Convert.ToString(timeSeriesModel.UserId));
                }
            }
            BusinessProblemInstaDTO businessProblemData = new BusinessProblemInstaDTO
            {
                BusinessProblems = timeSeriesModel.ModelName,
                TargetColumn = timeSeriesModel.TargetColumn,
                InputColumns = null,
                AvailableColumns = null,
                CorrelationId = timeSeriesModel.CorrelationId,
                TimeSeries = jObject,
                InstaId = timeSeriesModel.InstaId,
                InstaURL = timeSeriesModel.URL,
                ProblemType = timeSeriesModel.ProblemType,
                CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                CreatedByUser = timeSeriesModel.UserId,
                ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                ModifiedByUser = timeSeriesModel.UserId,
                _id = Guid.NewGuid().ToString(),
                UseCaseID = timeSeriesModel.UseCaseID,
                ClientUId = timeSeriesModel.ClientUId,
                DeliveryConstructUID = timeSeriesModel.DeliveryConstructUID
            };
            var jsonColumns = JsonConvert.SerializeObject(businessProblemData);
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.PSBusinessProblem);
            var insertBsonColumns = BsonSerializer.Deserialize<BsonDocument>(jsonColumns);
            collection.InsertOne(insertBsonColumns);
        }
        public string GetModelStatus(string instaId, string correlationId)
        {
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.InstaId, instaId) & Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.Status).Exclude(CONSTANTS.Id);
            var result = collection.Find(filter).Project(projection).ToList();
            string status = string.Empty;
            if (result.Count > 0)
            {
                status = result[0][CONSTANTS.Status].ToString();
            }
            return status;
        }
        public string GetFitDate(string correlationId, string instaId)
        {
            string lastFitDate = string.Empty;
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIRecommendedTrainedModels);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            //var filter = builder.Eq("CorrelationId", correlationId) & builder.Eq("InstaId", instaId);
            var outcome = collection.Find(filter).Project<BsonDocument>(Builders<BsonDocument>.
               Projection.Include(CONSTANTS.LastDataRecorded).Exclude(CONSTANTS.Id)).ToList();
            if (outcome.Count > 0)
            {
                lastFitDate = Convert.ToString(outcome[0][CONSTANTS.LastDataRecorded]);
            }
            return lastFitDate;
        }

        /// <summary>
        /// Gets the trained model data
        /// </summary>
        /// <param name="correlationId">The correlation identifier</param>
        /// <returns>Returns the result.</returns>
        public RecommedAITrainedModel GetRecommendedTrainedModels(string correlationId)
        {
            List<JObject> trainModelsList = new List<JObject>();
            RecommedAITrainedModel trainedModels = new RecommedAITrainedModel();
            var columnCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIRecommendedTrainedModels);
            var projection = Builders<BsonDocument>.Projection.Exclude("visualization");
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var trainedModel = columnCollection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (trainedModel.Count() > 0)
            {
                for (int i = 0; i < trainedModel.Count; i++)
                {
                    trainModelsList.Add(JObject.Parse(trainedModel[i].ToString()));
                }
                trainedModels.TrainedModel = trainModelsList;
            }

            return trainedModels;
        }

        //private string[] UpdateRecommendedModels(string correlationId, string problemType)
        //{
        //    string[] turnOffModels = null;
        //    string[] turnOnModels = null;
        //    var collection = _database.GetCollection<BsonDocument>(CONSTANTS.MERecommendedModels);
        //    var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
        //    var res = collection.Find(filter).ToList();

        //    if (problemType == CONSTANTS.Regression)
        //    {
        //        turnOffModels = appSettings.Value.Regression_Off.Split(CONSTANTS.comma);
        //        turnOnModels = appSettings.Value.Regression_On.Split(CONSTANTS.comma);
        //    }
        //    if (problemType == CONSTANTS.TimeSeries)
        //    {
        //        turnOffModels = appSettings.Value.TimeSeries_Off.Split(CONSTANTS.comma);
        //        turnOnModels = appSettings.Value.TimeSeries_On.Split(CONSTANTS.comma);
        //    }

        //    foreach (var model in turnOffModels)
        //    {
        //        string columnToUpdate = string.Format(CONSTANTS.SelectedModels, model, CONSTANTS.Train_model);
        //        var newFieldUpdate = Builders<BsonDocument>.Update.Set(columnToUpdate, CONSTANTS.False);
        //        var result = collection.UpdateOne(filter, newFieldUpdate);
        //    }
        //    foreach (var model in turnOnModels)
        //    {
        //        string columnToUpdate = string.Format(CONSTANTS.SelectedModels, model, CONSTANTS.Train_model);
        //        var newFieldUpdate = Builders<BsonDocument>.Update.Set(columnToUpdate, CONSTANTS.True);
        //        var result = collection.UpdateOne(filter, newFieldUpdate);
        //    }
        //    return turnOnModels;
        //}

        /// <summary>
        /// Regression TimeSeries Data Engineering
        /// </summary>
        /// <param name="regressionModel"></param>
        /// <returns></returns>
        private bool StartRegressionDataEngineering(VDSRegression regressionModel)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), nameof(StartRegressionDataEngineering), CONSTANTS.START, string.Empty, string.Empty, regressionModel.ClientUID,regressionModel.DCID);
            bool isDataTransformationCompleted = false;
            DataEngineering dataEngineering = new DataEngineering();
            try
            {
                //Inserting DataTransformation Requests for Python
                //foreach (var model in regressionModel.ProblemTypeDetails)
                //{
                //    _DBEncryptionRequired = CommonUtility.EncryptDB(model.CorrelationId, appSettings);
                //    //dataEngineering = GetDataCuration(model.CorrelationId, CONSTANTS.DataCleanUp, regressionModel.CreatedByUser);
                //    RemoveQueueRecords(model.CorrelationId, CONSTANTS.DataCleanUp);
                //    IngrainRequestQueue ingrainRequest = new IngrainRequestQueue
                //    {
                //        _id = Guid.NewGuid().ToString(),
                //        CorrelationId = model.CorrelationId,
                //        RequestId = Guid.NewGuid().ToString(),
                //        ProcessId = null,
                //        Status = null,
                //        ModelName = null,
                //        RequestStatus = CONSTANTS.New,
                //        RetryCount = 0,
                //        ProblemType = null,
                //        Message = null,
                //        UniId = null,
                //        Progress = null,
                //        pageInfo = CONSTANTS.DataCleanUp,
                //        ParamArgs = CONSTANTS.CurlyBraces,
                //        Function = CONSTANTS.DataCleanUp,
                //        CreatedByUser = regressionModel.CreatedByUser,
                //        CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                //        ModifiedByUser = regressionModel.CreatedByUser,
                //        ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                //        LastProcessedOn = null,
                //    };
                //    _ingestedDataService.InsertRequests(ingrainRequest);
                //    Thread.Sleep(1000);
                //}
                bool callMethod = true;
                Dictionary<string, bool> datacurationComplete = new Dictionary<string, bool>();
                //checking all DataTransformation Requests completed from Python
                foreach (var model in regressionModel.ProblemTypeDetails)
                {
                    callMethod = true;

                    while (callMethod)
                    {
                        var useCaseData = _processDataService.CheckPythonProcess(model.CorrelationId, CONSTANTS.DataCleanUp);
                        if (!string.IsNullOrEmpty(useCaseData))
                        {
                            JObject queueData = JObject.Parse(useCaseData);
                            dataEngineering.Status = (string)queueData[CONSTANTS.Status];
                            dataEngineering.Progress = (string)queueData[CONSTANTS.Progress];
                            dataEngineering.Message = (string)queueData[CONSTANTS.Message];
                            if (dataEngineering.Status == CONSTANTS.C & dataEngineering.Progress == CONSTANTS.Hundred)
                            {
                                datacurationComplete.Add(model.CorrelationId, true);
                                callMethod = false;
                            }
                            else if (dataEngineering.Status == CONSTANTS.E)
                            {
                                instaMLResponse instaML = new instaMLResponse();
                                instaML.CorrelationId = model.CorrelationId;
                                instaML.InstaID = model.InstaID;
                                instaML.Status = CONSTANTS.E;
                                instaML.ErrorMessage = Resource.IngrainResx.ErrorDataEngineering + dataEngineering.Message;
                                regressionResponse.Add(instaML);                              
                                CommonUtility.TerminatePythonProcess((int)queueData[CONSTANTS.PythonProcessID]);
                                this.UpdateStatus(instaML.CorrelationId, Resource.IngrainResx.ErrorDataEngineering);
                                callMethod = false;
                                return false;
                            }
                            else
                            {
                                callMethod = true;
                                Thread.Sleep(1000);
                            }
                        }
                    }
                }
                //int failedModel = datacurationComplete.Where(x => x.Value == false).Count();
                //if (failedModel < 1)
                {
                    Dictionary<string, bool> datatransformComplete = new Dictionary<string, bool>();
                    //Datatransformation forming to Collection
                    //foreach (var model in regressionModel.ProblemTypeDetails)
                    //{
                    //    PreProcessModelDTO preProcessModelDTO = new PreProcessModelDTO();
                    //    isDataTransformationCompleted = this.CreatePreprocess(model.CorrelationId, regressionModel.CreatedByUser, model.ProblemType, model.InstaID);
                    //    if (isDataTransformationCompleted)
                    //        datatransformComplete.Add(model.CorrelationId, true);
                    //    else
                    //        datatransformComplete.Add(model.CorrelationId, false);
                    //}
                }
                //else
                //    return false;
                //int DatatransformCount = datacurationComplete.Where(x => x.Value == false).Count();
                 //if (DatatransformCount < 1)
                //{
                    //Inserting DataTransformation Requests for Python
                    //foreach (var model in regressionModel.ProblemTypeDetails)
                    //{
                    //    RemoveQueueRecords(model.CorrelationId, CONSTANTS.DataPreprocessing);
                    //    IngrainRequestQueue ingrainRequest = new IngrainRequestQueue
                    //    {
                    //        _id = Guid.NewGuid().ToString(),
                    //        CorrelationId = model.CorrelationId,
                    //        RequestId = Guid.NewGuid().ToString(),
                    //        ProcessId = null,
                    //        Status = null,
                    //        ModelName = null,
                    //        RequestStatus = CONSTANTS.New,
                    //        RetryCount = 0,
                    //        ProblemType = null,
                    //        Message = null,
                    //        UniId = null,
                    //        Progress = null,
                    //        pageInfo = CONSTANTS.DataPreprocessing,
                    //        ParamArgs = CONSTANTS.CurlyBraces,
                    //        Function = CONSTANTS.DataTransform,
                    //        CreatedByUser = regressionModel.CreatedByUser,
                    //        CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                    //        ModifiedByUser = regressionModel.CreatedByUser,
                    //        ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                    //        LastProcessedOn = null,
                    //    };
                    //    _ingestedDataService.InsertRequests(ingrainRequest);
                    //    Thread.Sleep(1000);
                    //}

                    //checking all DataTransformation Requests completed from Python
                    foreach (var model in regressionModel.ProblemTypeDetails)
                    {
                        bool callMethodDataTransformation = true;
                        while (callMethodDataTransformation)
                        {
                            var useCaseData = _processDataService.CheckPythonProcess(model.CorrelationId, CONSTANTS.DataPreprocessing);
                            if (!string.IsNullOrEmpty(useCaseData))
                            {
                                JObject queueData = JObject.Parse(useCaseData);
                                dataEngineering.Status = (string)queueData[CONSTANTS.Status];
                                dataEngineering.Progress = (string)queueData[CONSTANTS.Progress];
                                dataEngineering.Message = (string)queueData[CONSTANTS.Message];
                                if (dataEngineering.Status == CONSTANTS.C & dataEngineering.Progress == CONSTANTS.Hundred)
                                {
                                    callMethodDataTransformation = false;
                                    instaMLResponse instaML1 = new instaMLResponse();
                                    instaML1.CorrelationId = model.CorrelationId;
                                    instaML1.InstaID = model.InstaID;
                                    instaML1.Status = CONSTANTS.C;
                                    instaML1.Message = Resource.IngrainResx.DataEngineering;
                                    regressionResponse.Add(instaML1);
                                }
                                else if (dataEngineering.Status == CONSTANTS.E)
                                {
                                    callMethodDataTransformation = false;
                                    instaMLResponse instaML = new instaMLResponse();
                                    instaML.CorrelationId = model.CorrelationId;
                                    instaML.InstaID = model.InstaID;
                                    instaML.Status = CONSTANTS.E;
                                    instaML.ErrorMessage = Resource.IngrainResx.ErrorDataEngineering + dataEngineering.Message;
                                    CommonUtility.TerminatePythonProcess((int)queueData[CONSTANTS.PythonProcessID]);
                                    this.UpdateStatus(instaML.CorrelationId, Resource.IngrainResx.ErrorDataTransformation);
                                    regressionResponse.Add(instaML);
                                    return false;
                                }
                                else
                                {
                                    callMethodDataTransformation = true;
                                    Thread.Sleep(1000);
                                }
                            }
                        }
                    }
                //}
                //else
                //{
                //    return false;
                //}

            }
            catch (Exception ex)
            {
               LOGGING.LogManager.Logger.LogErrorMessage(typeof(InstaModelService), nameof(StartRegressionDataEngineering), ex.Message, ex, string.Empty,  string.Empty, regressionModel.ClientUID, regressionModel.DCID);
                instaMLResponse instaML1 = new instaMLResponse();
                instaML1.CorrelationId = regressionModel.ProblemTypeDetails[0].CorrelationId;
                instaML1.InstaID = regressionModel.ProblemTypeDetails[0].InstaID;
                instaML1.Status = CONSTANTS.E;
                instaML1.Message = Resource.IngrainResx.DataEngineering;
                instaML1.ErrorMessage = ex.Message + CONSTANTS.ThreeDots + ex.StackTrace;
                regressionResponse.Add(instaML1);
                return false;
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), nameof(StartRegressionDataEngineering), CONSTANTS.END, string.Empty, string.Empty, regressionModel.ClientUID, regressionModel.DCID);
            return true;
        }

        private void UpdateStatus(string correlationId, string processNameMessage)
        {           
            var collection = _database.GetCollection<IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
            var builder = Builders<IngrainRequestQueue>.Filter;
            var filter = builder.Eq(CONSTANTS.CorrelationId, correlationId) & builder.Eq(CONSTANTS.Status, "P");
            var updateBuilder = Builders<IngrainRequestQueue>.Update;
            var update = updateBuilder.Set(CONSTANTS.Status, "E")
                .Set(CONSTANTS.Message, "Status changed to E because" + processNameMessage)              
                .Set(CONSTANTS.ModifiedOn, DateTime.Now.ToString(CONSTANTS.DateHoursFormat));
            collection.UpdateMany(filter, update);
        }

        /// <summary>
        /// Data Engineering Starts
        /// </summary>
        /// <param name="instaId"></param>
        /// <param name="correlationId"></param>
        /// <param name="userId"></param>
        /// <param name="ProblemType"></param>
        /// <returns></returns>
        public void StartDataEngineering(string instaId, string correlationId, string userId, string ProblemType, string useCaseID)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), nameof(StartDataEngineering), CONSTANTS.START, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            //bool isDataCurationCompleted = false;
            //bool isDataTransformationCompleted = false;
            //_DBEncryptionRequired = CommonUtility.EncryptDB(correlationId, appSettings);
            DataEngineering dataEngineering = new DataEngineering();
            try
            {
                dataEngineering = GetDataCuration(correlationId, CONSTANTS.DataCleanUp, userId);
                //if (useCaseID != null)
                //{
                //    if (dataEngineering.Status == CONSTANTS.E)
                //    {
                //        instaMLResponse instaML = new instaMLResponse();
                //        instaML.CorrelationId = correlationId;
                //        instaML.InstaID = instaId;
                //        instaML.Status = CONSTANTS.E;
                //        instaML.ErrorMessage = Resource.IngrainResx.ErrorDataEngineering + dataEngineering.Message;
                //        regressionResponse.Add(instaML);
                //    }
                //    if (dataEngineering.Status == CONSTANTS.C)
                //    {
                //        isDataCurationCompleted = this.IsDataCurationComplete(correlationId);
                //    }
                //    if (isDataCurationCompleted)
                //    {
                //        PreProcessModelDTO preProcessModelDTO = new PreProcessModelDTO();
                //        isDataTransformationCompleted = this.CreatePreprocess(correlationId, userId, ProblemType, instaId);
                //        if (isDataTransformationCompleted)
                //        {
                //            dataEngineering = GetDatatransformation(correlationId, CONSTANTS.DataPreprocessing, userId);
                //            switch (dataEngineering.Status)
                //            {
                //                case CONSTANTS.E:
                //                    instaMLResponse instaML = new instaMLResponse();
                //                    instaML.CorrelationId = correlationId;
                //                    instaML.InstaID = instaId;
                //                    instaML.Status = CONSTANTS.E;
                //                    instaML.ErrorMessage = Resource.IngrainResx.ErrorDataEngineering + dataEngineering.Message;
                //                    regressionResponse.Add(instaML);
                //                    break;
                //                case CONSTANTS.C:
                //                    instaMLResponse instaML1 = new instaMLResponse();
                //                    instaML1.CorrelationId = correlationId;
                //                    instaML1.InstaID = instaId;
                //                    instaML1.Status = CONSTANTS.C;
                //                    instaML1.Message = Resource.IngrainResx.DataEngineering;
                //                    regressionResponse.Add(instaML1);
                //                    break;
                //            }
                //        }
                //    }
                //}
                //else
                // {
                instaModel.Status = dataEngineering.Status;
                if (dataEngineering.Status == CONSTANTS.E)
                {
                    instaModel.CorrelationId = correlationId;
                    instaModel.InstaID = instaId;
                    instaModel.ErrorMessage = Resource.IngrainResx.ErrorDataEngineering + dataEngineering.Message;
                    //return instaModel;
                }
                //if (dataEngineering.Status == CONSTANTS.C)
                //{
                //    isDataCurationCompleted = this.IsDataCurationComplete(correlationId);
                //}
                else if (dataEngineering.Status == CONSTANTS.C)
                {
                    //PreProcessModelDTO preProcessModelDTO = new PreProcessModelDTO();
                    //isDataTransformationCompleted = this.CreatePreprocess(correlationId, userId, ProblemType, instaId);
                    //if (isDataTransformationCompleted)
                    {
                        dataEngineering = GetDatatransformation(correlationId, CONSTANTS.DataPreprocessing, userId);
                        instaModel.Status = dataEngineering.Status;
                        switch (dataEngineering.Status)
                        {
                            case CONSTANTS.E:
                                instaModel.CorrelationId = correlationId;
                                instaModel.InstaID = instaId;
                                instaModel.ErrorMessage = Resource.IngrainResx.ErrorDataEngineering + dataEngineering.Message;
                                break;
                            case CONSTANTS.C:
                                instaModel.CorrelationId = correlationId;
                                instaModel.InstaID = instaId;
                                instaModel.Message = Resource.IngrainResx.DataEngineering;
                                break;
                        }
                    }
                }
                // }

            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InstaModelService), nameof(StartDataEngineering), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                instaModel.ErrorMessage = ex.Message + CONSTANTS.ThreeDots + ex.StackTrace;
                instaModel.Status = CONSTANTS.E;
                instaModel.ErrorMessage = Resource.IngrainResx.EngineeringDataError;
                //Regression Model Error
                instaMLResponse instaML1 = new instaMLResponse();
                instaML1.CorrelationId = correlationId;
                instaML1.InstaID = instaId;
                instaML1.Status = CONSTANTS.E;
                instaML1.Message = Resource.IngrainResx.DataEngineering;
                instaML1.ErrorMessage = ex.Message + CONSTANTS.ThreeDots + ex.StackTrace;
                regressionResponse.Add(instaML1);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), nameof(StartDataEngineering), CONSTANTS.END, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
        }
        private void StartRegressionModelEngineering(VDSRegression regressionModel)
        {
             LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), nameof(StartRegressionModelEngineering), CONSTANTS.START, string.Empty, string.Empty, regressionModel.ClientUID,regressionModel.DCID);
            int noOfModelsSelected = 0;
            try
            {
                if (regressionModel.UseCaseID != null)
                {
                    string pythonResult = string.Empty;
                    //Inserting all Model Requests for Model Training
                    //foreach (var model in regressionModel.ProblemTypeDetails)
                    //{
                    //    string[] models = null;
                    //    IngrainRequestQueue requestQueue = new IngrainRequestQueue();
                    //    models = this.UpdateRecommendedModels(model.CorrelationId, model.ProblemType);
                    //    RemoveQueueRecords(model.CorrelationId, CONSTANTS.RecommendedAI);
                    //    foreach (var modelName in models)
                    //    {
                    //        IngrainRequestQueue ingrainRequest = new IngrainRequestQueue
                    //        {
                    //            _id = Guid.NewGuid().ToString(),
                    //            CorrelationId = model.CorrelationId,
                    //            RequestId = Guid.NewGuid().ToString(),
                    //            ProcessId = null,
                    //            Status = null,
                    //            ModelName = modelName,
                    //            RequestStatus = CONSTANTS.New,
                    //            RetryCount = 0,
                    //            ProblemType = model.ProblemType,
                    //            Message = null,
                    //            UniId = null,
                    //            Progress = null,
                    //            pageInfo = CONSTANTS.RecommendedAI,
                    //            ParamArgs = CONSTANTS.CurlyBraces,
                    //            Function = CONSTANTS.RecommendedAI,
                    //            CreatedByUser = regressionModel.CreatedByUser,
                    //            CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                    //            ModifiedByUser = regressionModel.CreatedByUser,
                    //            ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                    //            LastProcessedOn = null,
                    //        };
                    //         LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), nameof(StartModelEngineering), CONSTANTS.BeforeInsertRequest, string.Empty, string.Empty, regressionModel.ClientUID, regressionModel.DCID);
                    //        _ingestedDataService.InsertRequests(ingrainRequest);
                    //    }
                    //    //Thread.Sleep(2000);
                    //}
                    //Checking all the models completed Model training
                    foreach (var trainedModel in regressionModel.ProblemTypeDetails)
                    {
                        int errorCount = 0;
                        bool isModelTrained = true;
                        if (trainedModel.ProblemType == CONSTANTS.TimeSeries)
                        {
                            //string[] timeSeriesCount = appSettings.Value.TimeSeries_On.Split(CONSTANTS.comma);
                            //noOfModelsSelected = timeSeriesCount.Length;
                            noOfModelsSelected = this.GetDefaultModels(CONSTANTS.ModelMasterTimeSeriesKey);
                        }
                        if (trainedModel.ProblemType == CONSTANTS.Regression)
                        {
                            //string[] regressionCount = appSettings.Value.Regression_On.Split(CONSTANTS.comma);
                            //noOfModelsSelected = regressionCount.Length;
                            noOfModelsSelected = this.GetDefaultModels(CONSTANTS.ModelMasterRegressionKey);
                        }
                        while (isModelTrained)
                        {
                            int modelsCount = 0;
                            ExecuteQueueTable:
                            var useCaseDetails = _modelEngineering.GetMultipleRequestStatus(trainedModel.CorrelationId, CONSTANTS.RecommendedAI);
                            if (useCaseDetails.Count > 0)
                            {
                                List<int> progressList = new List<int>();
                                for (int i = 0; i < useCaseDetails.Count; i++)
                                {
                                    string queueStatus = useCaseDetails[i].Status;
                                    if (queueStatus == CONSTANTS.C)
                                    {
                                        modelsCount++;
                                    }
                                    if (queueStatus == CONSTANTS.E)
                                    {
                                        modelsCount++;
                                        errorCount++;
                                    }
                                }
                                if (errorCount == 2)
                                {
                                    isModelTrained = false;
                                    instaMLResponse response = new instaMLResponse();
                                    response.CorrelationId = trainedModel.CorrelationId;
                                    response.InstaID = trainedModel.InstaID;
                                    response.Status = CONSTANTS.E;
                                    response.Message = Resource.IngrainResx.ModelsFailedTraining;
                                    regressionResponse.Add(response);
                                    //return instaModel;
                                }
                                if (errorCount < 2)
                                {
                                    if (modelsCount == noOfModelsSelected)
                                    {
                                        isModelTrained = false;
                                        instaMLResponse response = new instaMLResponse();
                                        response.CorrelationId = trainedModel.CorrelationId;
                                        response.InstaID = trainedModel.InstaID;
                                        response.Status = CONSTANTS.C;
                                        response.Message = Resource.IngrainResx.ModelEngineeringcompleted;
                                        regressionResponse.Add(response);
                                    }
                                    else
                                    {
                                        modelsCount = 0;
                                        errorCount = 0;
                                        isModelTrained = true;
                                        Thread.Sleep(1000);
                                        goto ExecuteQueueTable;
                                    }
                                    Thread.Sleep(1000);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InstaModelService), nameof(StartRegressionModelEngineering), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                instaMLResponse instaML1 = new instaMLResponse();
                instaML1.CorrelationId = regressionModel.ProblemTypeDetails[0].CorrelationId;
                instaML1.InstaID = regressionModel.ProblemTypeDetails[0].InstaID;
                instaML1.Status = CONSTANTS.E;
                instaML1.Message = Resource.IngrainResx.ErrorModelEngineering;
                instaML1.ErrorMessage = ex.Message + CONSTANTS.ThreeDots + ex.StackTrace;
                regressionResponse.Add(instaML1);
            }
         LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), nameof(StartRegressionModelEngineering), CONSTANTS.END, string.Empty, string.Empty, regressionModel.ClientUID,regressionModel.DCID);
        }
        /// <summary>
        /// Generate Models
        /// </summary>
        /// <param name="instaId"></param>
        /// <param name="correlationId"></param>
        /// <param name="userId"></param>
        /// <param name="ProblemType"></param>
        /// <returns></returns>
        private void StartModelEngineering(string instaId, string correlationId, string userId, string ProblemType, string useCaseId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), nameof(StartModelEngineering), CONSTANTS.START, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            int noOfModelsSelected = 0;            
            if (ProblemType == CONSTANTS.TimeSeries)
            {
                //string[] timeSeriesCount = appSettings.Value.TimeSeries_On.Split(CONSTANTS.comma);
                //noOfModelsSelected = timeSeriesCount.Length;
                noOfModelsSelected = this.GetDefaultModels(CONSTANTS.ModelMasterTimeSeriesKey);
            }
            if (ProblemType == CONSTANTS.Regression)
            {
                //string[] regressionCount = appSettings.Value.Regression_On.Split(CONSTANTS.comma);
                //noOfModelsSelected = regressionCount.Length;
                noOfModelsSelected = this.GetDefaultModels(CONSTANTS.ModelMasterRegressionKey);
            }
            string pythonResult = string.Empty;
            int errorCount = 0;
            bool isModelTrained = true;
            try
            {
                //if (useCaseId != null)
                //{
                //    while (isModelTrained)
                //    {
                //        int modelsCount = 0;
                //    ExecuteQueueTable:
                //        var useCaseDetails = _modelEngineering.GetMultipleRequestStatus(correlationId, CONSTANTS.RecommendedAI);
                //        if (useCaseDetails.Count > 0)
                //        {
                //            List<int> progressList = new List<int>();
                //            for (int i = 0; i < useCaseDetails.Count; i++)
                //            {
                //                string queueStatus = useCaseDetails[i].Status;
                //                if (queueStatus == CONSTANTS.C)
                //                {
                //                    modelsCount++;
                //                }
                //                if (queueStatus == CONSTANTS.E)
                //                {
                //                    modelsCount++;
                //                    errorCount++;
                //                }
                //            }
                //            if (errorCount == 2)
                //            {
                //                isModelTrained = false;
                //                instaMLResponse response = new instaMLResponse();
                //                response.CorrelationId = correlationId;
                //                response.InstaID = instaId;
                //                response.Status = CONSTANTS.E;
                //                response.Message = Resource.IngrainResx.ModelsFailedTraining;
                //                regressionResponse.Add(response);
                //                //return instaModel;
                //            }
                //            if (errorCount < 2)
                //            {
                //                if (modelsCount == noOfModelsSelected)
                //                {
                //                    isModelTrained = false;
                //                    instaMLResponse response = new instaMLResponse();
                //                    response.CorrelationId = correlationId;
                //                    response.InstaID = instaId;
                //                    response.Status = CONSTANTS.C;
                //                    response.Message = Resource.IngrainResx.ModelEngineeringcompleted;
                //                    regressionResponse.Add(response);
                //                }
                //                else
                //                {
                //                    modelsCount = 0;
                //                    errorCount = 0;
                //                    isModelTrained = true;
                //                    goto ExecuteQueueTable;
                //                }
                //                Thread.Sleep(1000);
                //            }

                //        }
                //        else
                //        {
                //            string[] models = null;
                //            IngrainRequestQueue requestQueue = new IngrainRequestQueue();
                //            models = this.UpdateRecommendedModels(correlationId, ProblemType);
                //            foreach (var modelName in models)
                //            {
                //                IngrainRequestQueue ingrainRequest = new IngrainRequestQueue
                //                {
                //                    _id = Guid.NewGuid().ToString(),
                //                    CorrelationId = correlationId,
                //                    RequestId = Guid.NewGuid().ToString(),
                //                    ProcessId = null,
                //                    Status = null,
                //                    ModelName = modelName,
                //                    RequestStatus = CONSTANTS.New,
                //                    RetryCount = 0,
                //                    ProblemType = ProblemType,
                //                    Message = null,
                //                    UniId = null,
                //                    Progress = null,
                //                    pageInfo = CONSTANTS.RecommendedAI,
                //                    ParamArgs = CONSTANTS.CurlyBraces,
                //                    Function = CONSTANTS.RecommendedAI,
                //                    CreatedByUser = userId,
                //                    CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                //                    ModifiedByUser = userId,
                //                    ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                //                    LastProcessedOn = null,
                //                };
                //                LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), nameof(StartModelEngineering), CONSTANTS.BeforeInsertRequest, new Guid(correlationId));
                //                _ingestedDataService.InsertRequests(ingrainRequest);
                //            }
                //            Thread.Sleep(2000);
                //        }
                //    }
                //}
                //else
                //{
                while (isModelTrained)
                {
                    int modelsCount = 0;
                    ExecuteQueueTable:
                    var useCaseDetails = _modelEngineering.GetMultipleRequestStatus(correlationId, CONSTANTS.RecommendedAI);
                    if (useCaseDetails.Count > 0)
                    {
                        List<int> progressList = new List<int>();
                        for (int i = 0; i < useCaseDetails.Count; i++)
                        {
                            string queueStatus = useCaseDetails[i].Status;
                            if (queueStatus == CONSTANTS.C)
                            {
                                modelsCount++;
                            }
                            if (queueStatus == CONSTANTS.E)
                            {
                                modelsCount++;
                                errorCount++;
                            }
                        }
                        if (errorCount == 2)
                        {
                            isModelTrained = false;
                            instaModel.CorrelationId = correlationId;
                            instaModel.InstaID = instaId;
                            instaModel.Status = CONSTANTS.E;
                            instaModel.Message = Resource.IngrainResx.ModelsFailedTraining;
                            //return instaModel;
                        }
                        if (modelsCount == noOfModelsSelected)
                        {
                            isModelTrained = false;
                            instaModel.CorrelationId = correlationId;
                            instaModel.InstaID = instaId;
                            instaModel.Status = CONSTANTS.C;
                            instaModel.Message = Resource.IngrainResx.ModelEngineeringcompleted;
                        }
                        else
                        {
                            modelsCount = 0;
                            errorCount = 0;
                            isModelTrained = true;
                            goto ExecuteQueueTable;
                        }
                        Thread.Sleep(1000);
                    }
                    //else
                    //{
                    //    IngrainRequestQueue requestQueue = new IngrainRequestQueue();
                    //    string[] models = null;
                    //    models = this.UpdateRecommendedModels(correlationId, ProblemType);
                    //    RemoveQueueRecords(correlationId, CONSTANTS.RecommendedAI);
                    //    foreach (var modelName in models)
                    //    {
                    //        IngrainRequestQueue ingrainRequest = new IngrainRequestQueue
                    //        {
                    //            _id = Guid.NewGuid().ToString(),
                    //            CorrelationId = correlationId,
                    //            RequestId = Guid.NewGuid().ToString(),
                    //            ProcessId = null,
                    //            Status = null,
                    //            ModelName = modelName,
                    //            RequestStatus = CONSTANTS.New,
                    //            RetryCount = 0,
                    //            ProblemType = ProblemType,
                    //            Message = null,
                    //            UniId = null,
                    //            Progress = null,
                    //            pageInfo = CONSTANTS.RecommendedAI,
                    //            ParamArgs = CONSTANTS.CurlyBraces,
                    //            Function = CONSTANTS.RecommendedAI,
                    //            CreatedByUser = userId,
                    //            CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                    //            ModifiedByUser = userId,
                    //            ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                    //            LastProcessedOn = null,
                    //        };
                    //        LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), nameof(StartModelEngineering), CONSTANTS.BeforeInsertRequest, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
                    //        _ingestedDataService.InsertRequests(ingrainRequest);
                    //    }
                    //    Thread.Sleep(2000);
                    //}
                }
              
            }
            catch (Exception ex)
            {
              LOGGING.LogManager.Logger.LogErrorMessage(typeof(InstaModelService), nameof(StartModelEngineering), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                instaModel.ErrorMessage = ex.Message + CONSTANTS.ThreeDots + ex.StackTrace;
                instaModel.Status = CONSTANTS.E;
                instaModel.Message = Resource.IngrainResx.ErrorModelEngineering;
            }
             LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), nameof(StartModelEngineering), CONSTANTS.END, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            //return instaModel;
        }

        /// <summary>
        /// Get the default ON models from MLDL_ModelsMaster collection
        /// </summary>
        /// <param name="key">The key</param>
        /// <returns>Returns the count of ON models</returns>
        private int GetDefaultModels(string key)
        {
            var modelCollection = _database.GetCollection<BsonDocument>(CONSTANTS.MLDL_ModelsMaster);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.Id, CONSTANTS.MLDLModelMasterId);
            var projection = Builders<BsonDocument>.Projection.Include("Models").Exclude(CONSTANTS.Id);
            var result = modelCollection.Find(filter).Project<BsonDocument>(projection).FirstOrDefault();
            List<string> timeSeriesCount = new List<string>();
            if (result != null)
            {
                var parse = JObject.Parse(result.ToString());
                string[] keyValues = key.Split('.');
                string modelType = keyValues[1].Trim();

                foreach (var i in parse["Models"][modelType].Children())
                {
                    if (Convert.ToString(((Newtonsoft.Json.Linq.JProperty)i).Value) == CONSTANTS.True)
                    {
                        timeSeriesCount.Add(((Newtonsoft.Json.Linq.JProperty)i).Name);
                    }
                }
            }

            return timeSeriesCount.Count;
        }

        

        public DataEngineering GetDataCuration(string correlationId, string pageInfo, string userId)
        {
            bool callMethod = true;
            DataEngineering dataEngineering = new DataEngineering();
            string PythonResult = string.Empty;
            while (callMethod)
            {
                var useCaseData = _processDataService.CheckPythonProcess(correlationId, CONSTANTS.DataCleanUp);
                if (!string.IsNullOrEmpty(useCaseData))
                {
                    JObject queueData = JObject.Parse(useCaseData);
                    dataEngineering.Status = (string)queueData[CONSTANTS.Status];
                    dataEngineering.Progress = (string)queueData[CONSTANTS.Progress];
                    dataEngineering.Message = (string)queueData[CONSTANTS.Message];
                    if (dataEngineering.Status == CONSTANTS.C & dataEngineering.Progress == CONSTANTS.Hundred)
                    {
                        callMethod = false;
                        dataEngineering.IsComplete = true;
                        return dataEngineering;
                    }
                    else if (dataEngineering.Status == CONSTANTS.E)
                    {
                        callMethod = false;
                        dataEngineering.IsComplete = false;
                        return dataEngineering;
                    }
                }                

                //else
                //{
                //    IngrainRequestQueue ingrainRequest = new IngrainRequestQueue
                //    {
                //        _id = Guid.NewGuid().ToString(),
                //        CorrelationId = correlationId,
                //        RequestId = Guid.NewGuid().ToString(),
                //        ProcessId = null,
                //        Status = null,
                //        ModelName = null,
                //        RequestStatus = CONSTANTS.New,
                //        RetryCount = 0,
                //        ProblemType = null,
                //        Message = null,
                //        UniId = null,
                //        Progress = null,
                //        pageInfo = CONSTANTS.DataCleanUp,
                //        ParamArgs = CONSTANTS.CurlyBraces,
                //        Function = CONSTANTS.DataCleanUp,
                //        CreatedByUser = userId,
                //        CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                //        ModifiedByUser = userId,
                //        ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                //        LastProcessedOn = null,
                //    };
                //    _ingestedDataService.InsertRequests(ingrainRequest);
                //    Thread.Sleep(1000);
                //}
            }
            return dataEngineering;
        }
        public DataEngineering GetDatatransformation(string correlationId, string pageInfo, string userId)
        {
            bool callMethod = true;
            DataEngineering dataEngineering = new DataEngineering();
            string PythonResult = string.Empty;
            while (callMethod)
            {
                var useCaseData = _processDataService.CheckPythonProcess(correlationId, CONSTANTS.DataPreprocessing);
                if (!string.IsNullOrEmpty(useCaseData))
                {
                    JObject queueData = JObject.Parse(useCaseData);
                    dataEngineering.Status = (string)queueData[CONSTANTS.Status];
                    dataEngineering.Progress = (string)queueData[CONSTANTS.Progress];
                    dataEngineering.Message = (string)queueData[CONSTANTS.Message];
                    if (dataEngineering.Status == CONSTANTS.C & dataEngineering.Progress == CONSTANTS.Hundred)
                    {
                        callMethod = false;
                        dataEngineering.IsComplete = true;
                        return dataEngineering;
                    }
                    if (dataEngineering.Status == CONSTANTS.E)
                    {
                        callMethod = false;
                        dataEngineering.IsComplete = false;
                        return dataEngineering;
                    }
                }
                //else
                //{
                //    IngrainRequestQueue ingrainRequest = new IngrainRequestQueue
                //    {
                //        _id = Guid.NewGuid().ToString(),
                //        CorrelationId = correlationId,
                //        RequestId = Guid.NewGuid().ToString(),
                //        ProcessId = null,
                //        Status = null,
                //        ModelName = null,
                //        RequestStatus = CONSTANTS.New,
                //        RetryCount = 0,
                //        ProblemType = null,
                //        Message = null,
                //        UniId = null,
                //        Progress = null,
                //        pageInfo = CONSTANTS.DataPreprocessing,
                //        ParamArgs = CONSTANTS.CurlyBraces,
                //        Function = CONSTANTS.DataTransform,
                //        CreatedByUser = userId,
                //        CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                //        ModifiedByUser = userId,
                //        ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                //        LastProcessedOn = null,
                //    };
                //    _ingestedDataService.InsertRequests(ingrainRequest);
                //    Thread.Sleep(1000);
                //}
            }
            return dataEngineering;
        }

        /// <summary>
        /// Deploy the Model
        /// </summary>
        /// <param name="instaId"></param>
        /// <param name="correlationId"></param>
        /// <param name="userId"></param>
        /// <param name="ProblemType"></param>
        /// <returns></returns>
        private void DeployModel(string instaId, string correlationId, string userId, string ProblemType, string useCaseId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), nameof(DeployModel), CONSTANTS.START, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                //Gets the records from SSAI_RecommendedTrainedModels collection for frequency & accuracy in order to update in deployed model collection
                _recommendedAI = this.GetTrainedModel(correlationId, ProblemType);
                if (useCaseId != null)
                {
                    instaMLResponse response = new instaMLResponse();
                    response.CorrelationId = correlationId;
                    response.InstaID = instaId;
                    if (_recommendedAI != null && _recommendedAI.TrainedModel != null && _recommendedAI.TrainedModel.Count > 0)
                    {
                        var result = this.IsDeployModelComplete(correlationId, _recommendedAI, ProblemType);
                        if (result)
                        {
                            response.Status = CONSTANTS.C;
                            response.Message = Resource.IngrainResx.DeployModelcompleted;
                        }
                        else
                        {
                            response.Status = CONSTANTS.V;
                            response.Message = Resource.IngrainResx.NoRecordFound;
                        }
                    }
                    else
                    {
                        response.Status = CONSTANTS.V;
                        response.Message = Resource.IngrainResx.Nomodelstrained;
                    }
                    regressionResponse.Add(response);
                }
                else
                {
                    ////Gets the records from SSAI_RecommendedTrainedModels collection for frequency & accuracy in order to update in deployed model collection
                    //_recommendedAI = this.GetTrainedModel(correlationId, ProblemType);
                    instaModel.CorrelationId = correlationId;
                    instaModel.InstaID = instaId;
                    if (_recommendedAI != null && _recommendedAI.TrainedModel != null && _recommendedAI.TrainedModel.Count > 0)
                    {
                        var result = this.IsDeployModelComplete(correlationId, _recommendedAI, ProblemType);
                        if (result)
                        {
                            instaModel.Status = CONSTANTS.C;
                            instaModel.Message = Resource.IngrainResx.DeployModelcompleted;
                        }
                        else
                        {
                            instaModel.Status = CONSTANTS.V;
                            instaModel.Message = Resource.IngrainResx.NoRecordFound;
                        }
                    }
                    else
                    {
                        instaModel.Status = CONSTANTS.V;
                        instaModel.Message = Resource.IngrainResx.Nomodelstrained;
                    }
                }

            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InstaModelService), nameof(DeployModel), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                instaModel.ErrorMessage = ex.Message + CONSTANTS.ThreeDots + ex.StackTrace;
                instaModel.Status = CONSTANTS.E;
                instaMLResponse instaML1 = new instaMLResponse();
                instaML1.CorrelationId = correlationId;
                instaML1.InstaID = instaId;
                instaML1.Status = CONSTANTS.E;
                instaML1.Message = CONSTANTS.Failed; ;
                instaML1.ErrorMessage = ex.Message + CONSTANTS.ThreeDots + ex.StackTrace;
                regressionResponse.Add(instaML1);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), nameof(DeployModel), CONSTANTS.END, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            //return instaModel;
        }

        /// <summary>
        /// Gets the trained model data
        /// </summary>
        /// <param name="correlationId">The correlation identifier</param>
        /// <param name="problemType">The problem type</param>
        /// <returns></returns>
        public RecommedAITrainedModel GetTrainedModel(string correlationId, string problemType)
        {
            _recommendedAI = new RecommedAITrainedModel();
            _recommendedAI = this.GetRecommendedTrainedModels(correlationId);
            ////Gets the max accuracy from list of trained model based on problem type
            if (_recommendedAI != null && _recommendedAI.TrainedModel != null && _recommendedAI.TrainedModel.Count > 0)
            {
                double? maxAccuracy = null;
                switch (problemType)
                {
                    case CONSTANTS.Classification:
                        maxAccuracy = _recommendedAI.TrainedModel.Max(x => (double)x[CONSTANTS.Accuracy]);
                        _recommendedAI.TrainedModel = _recommendedAI.TrainedModel.Where(p => (double)p[CONSTANTS.Accuracy] == maxAccuracy).ToList();
                        break;
                    case CONSTANTS.Multi_Class:
                        maxAccuracy = _recommendedAI.TrainedModel.Max(x => (double)x[CONSTANTS.Accuracy]);
                        _recommendedAI.TrainedModel = _recommendedAI.TrainedModel.Where(p => (double)p[CONSTANTS.Accuracy] == maxAccuracy).ToList();
                        break;

                    case CONSTANTS.Regression:
                        maxAccuracy = _recommendedAI.TrainedModel.Max(x => (double)x[CONSTANTS.r2ScoreVal][CONSTANTS.error_rate]);
                        _recommendedAI.TrainedModel = _recommendedAI.TrainedModel.Where(p => (double)p[CONSTANTS.r2ScoreVal][CONSTANTS.error_rate] == maxAccuracy).ToList();
                        break;
                    case CONSTANTS.TimeSeries:
                        maxAccuracy = _recommendedAI.TrainedModel.Max(x => (double)x[CONSTANTS.r2ScoreVal][CONSTANTS.error_rate]);
                        _recommendedAI.TrainedModel = _recommendedAI.TrainedModel.Where(p => (double)p[CONSTANTS.r2ScoreVal][CONSTANTS.error_rate] == maxAccuracy).ToList();
                        break;
                }
            }
            return _recommendedAI;
        }
        private void DeleteRegressionIngestData(string useCaseID, string pageInfo)
        {
            var collection = _database.GetCollection<IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
            var builder = Builders<IngrainRequestQueue>.Filter;
            var filter = builder.Eq(CONSTANTS.pageInfo, pageInfo) & builder.Eq(CONSTANTS.UseCaseID, useCaseID);
            collection.DeleteMany(filter);
        }
        private List<IngrainRequestQueue> GetMultipleRequestStatus(string correlationId, string pageInfo, string problemType, string ParentUID)
        {
            List<IngrainRequestQueue> ingrainRequest = new List<IngrainRequestQueue>();
            var collection = _database.GetCollection<IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
            var builder = Builders<IngrainRequestQueue>.Filter;
            if (problemType == CONSTANTS.Regression)
            {
                var filter = builder.Eq(CONSTANTS.pageInfo, pageInfo) & (builder.Eq(CONSTANTS.CorrelationId, correlationId) | builder.Eq("UseCaseID", ParentUID));
                return ingrainRequest = collection.Find(filter).ToList();
            }
            else
            {
                var filter = builder.Eq(CONSTANTS.pageInfo, pageInfo) & builder.Eq(CONSTANTS.CorrelationId, correlationId);
                return ingrainRequest = collection.Find(filter).ToList();
            }
        }

        /// <summary>
        /// Get Regression Model Prediction Output
        /// </summary>
        /// <param name="regressionModel"></param>
        /// <returns>Regression Model Prediction</returns>
        public InstaRegression GetRegressionPrediction(VDSRegression regressionModel)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), nameof(GetRegressionPrediction), CONSTANTS.START, string.IsNullOrEmpty(regressionModel.ProblemTypeDetails[0].CorrelationId) ? default(Guid) : new Guid(regressionModel.ProblemTypeDetails[0].CorrelationId), string.Empty, string.Empty, string.Empty, string.Empty);
            List<PredictionDTO> predictions = new List<PredictionDTO>();
            string regresionCorelationId = string.Empty;
            string instaId = string.Empty;
            List<string> uniqueIds = new List<string>();
            try
            {
                foreach (var item in regressionModel.ProblemTypeDetails)
                {
                    if (item.ProblemType == CONSTANTS.TimeSeries)
                    {
                        _deployModelViewModel = _deployedModelService.GetDeployModel(item.CorrelationId);
                        if (_deployModelViewModel != null && _deployModelViewModel.DeployModels.Count > 0)
                        {
                            string data = CONSTANTS.Null;
                            string frequency = _deployModelViewModel.DeployModels[0].Frequency;
                            PredictionDTO predictionDTO = new PredictionDTO
                            {
                                _id = Guid.NewGuid().ToString(),
                                UniqueId = Guid.NewGuid().ToString(),

                                CorrelationId = item.CorrelationId,
                                Frequency = frequency,
                                PredictedData = null,
                                InstaId = item.InstaID,
                                UseCaseId = regressionModel.UseCaseID,
                                Status = CONSTANTS.I,
                                ErrorMessage = null,
                                Progress = null,
                                CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                                ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                                CreatedByUser = regressionModel.CreatedByUser,
                                ModifiedByUser = regressionModel.CreatedByUser
                            };
                            bool DBEncryptionRequired = CommonUtility.EncryptDB(predictionDTO.CorrelationId, appSettings);
                            // db data encrypt
                            if (DBEncryptionRequired)
                            {
                                predictionDTO.ActualData = _encryptionDecryption.Encrypt(data);
                            }
                            else
                            {
                                predictionDTO.ActualData = data;
                            }
                            _deployedModelService.SavePrediction(predictionDTO);
                            uniqueIds.Add(predictionDTO.UniqueId);
                            DeployModelsDto mdl = _deployedModelService.GetDeployModelDetails(item.CorrelationId);
                            IngrainRequestQueue ingrainRequest = new IngrainRequestQueue
                            {
                                _id = Guid.NewGuid().ToString(),
                                AppID = mdl.AppId,
                                CorrelationId = item.CorrelationId,
                                RequestId = Guid.NewGuid().ToString(),
                                ProcessId = null,
                                Status = null,
                                ModelName = null,
                                RequestStatus = appSettings.Value.IsFlaskCall ? CONSTANTS.Occupied : CONSTANTS.New,
                                RetryCount = 0,
                                UseCaseID = regressionModel.UseCaseID,
                                ProblemType = null,
                                Message = null,
                                UniId = predictionDTO.UniqueId,
                                Progress = null,
                                pageInfo = CONSTANTS.ForecastModel, // pageInfo 
                                ParamArgs = CONSTANTS.CurlyBraces,
                                Function = CONSTANTS.ForecastModel,
                                CreatedByUser = regressionModel.CreatedByUser,
                                CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                                ModifiedByUser = regressionModel.CreatedByUser,
                                ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                                LastProcessedOn = null,
                                IsForAPI = appSettings.Value.IsFlaskCall ? true : false
                            };
                            _ingestedDataService.InsertRequests(ingrainRequest);
                            if (appSettings.Value.IsFlaskCall)
                            {
                                _iFlaskAPIService.CallPython(ingrainRequest.CorrelationId, ingrainRequest.UniId, ingrainRequest.pageInfo);
                            }
                            Thread.Sleep(1000);
                            //Asset usage
                            auditTrailLog.CorrelationId = ingrainRequest.CorrelationId;
                            auditTrailLog.ClientId = mdl.ClientUId;
                            auditTrailLog.DCID = mdl.DeliveryConstructUID;
                            auditTrailLog.CreatedBy = regressionModel.CreatedByUser;
                            auditTrailLog.FeatureName = CONSTANTS.InstaMLTimeseriesFeature;
                            auditTrailLog.ProcessName = CONSTANTS.PredictionName;
                            auditTrailLog.RequestId = ingrainRequest.RequestId;
                            auditTrailLog.UseCaseId = regressionModel.UseCaseID;
                            auditTrailLog.ApplicationID = mdl.AppId;
                            auditTrailLog.UsageType = CONSTANTS.AssetUsage;
                            CommonUtility.AuditTrailLog(auditTrailLog, appSettings);

                        }
                    }
                    else
                    {
                        if (item.ProblemType == CONSTANTS.Regression)
                        {
                            regresionCorelationId = item.CorrelationId;
                            instaId = item.InstaID;
                            //Asset usage
                            DeployModelsDto mdl = _deployedModelService.GetDeployModelDetails(regresionCorelationId);
                            if (mdl != null)
                            {
                                auditTrailLog.ClientId = mdl.ClientUId;
                                auditTrailLog.DCID = mdl.DeliveryConstructUID;
                                auditTrailLog.ApplicationID = mdl.AppId;
                            }
                            auditTrailLog.CorrelationId = regresionCorelationId;
                            auditTrailLog.CreatedBy = regressionModel.CreatedByUser;
                            auditTrailLog.FeatureName = CONSTANTS.InstaMLRegressionFeature;
                            auditTrailLog.ProcessName = CONSTANTS.PredictionName;

                            auditTrailLog.UsageType = CONSTANTS.AssetUsage;
                            CommonUtility.AuditTrailLog(auditTrailLog, appSettings);
                        }
                    }
                }
                bool isPrediction = true;
                int errorCount = 0;
                while (isPrediction)
                {
                    int successCount = 0;
                    int progressCount = 0;
                    Thread.Sleep(1000);
                    predictions = GetRegressionResult(uniqueIds);
                    foreach (var prediction in predictions)
                    {
                        if (errorCount == 0)
                        {
                            if (prediction.Status == CONSTANTS.I || prediction.Status == CONSTANTS.P)
                            {
                                progressCount++;
                                isPrediction = true;
                                errorCount = 0;
                            }
                        }
                        if (prediction.Status == CONSTANTS.E)
                        {
                            errorCount++;
                            instaMLResponse response = new instaMLResponse();
                            response.CorrelationId = prediction.CorrelationId;
                            response.InstaID = prediction.InstaId;
                            response.Message = CONSTANTS.IngestDataError;
                            response.Status = prediction.Status;
                            response.ErrorMessage = CONSTANTS.IngestDataError + "-" + prediction.ErrorMessage;
                            regressionResponse.Add(response);
                        }
                    }
                    if (errorCount > 0)
                    {
                        isPrediction = false;
                    }
                    else if (progressCount == 0)
                    {
                        foreach (var prediction in predictions)
                        {
                            if (prediction.Status == CONSTANTS.C)
                            {
                                bool DBEncryption = CommonUtility.EncryptDB(prediction.CorrelationId, appSettings);
                                string PredictedData;
                                if (DBEncryption)
                                    PredictedData = _encryptionDecryption.Decrypt(prediction.PredictedData);
                                else
                                    PredictedData = prediction.PredictedData;

                                isPrediction = false;
                                instaMLResponse response = new instaMLResponse();
                                response.CorrelationId = prediction.CorrelationId;
                                response.InstaID = prediction.InstaId;
                                response.Status = prediction.Status;
                                response.Message = Resource.IngrainResx.Predictioncompleted;
                                response.PredictedData = JsonConvert.DeserializeObject<List<dynamic>>(PredictedData);
                                regressionResponse.Add(response);
                                successCount++;
                            }
                        }
                    }
                }

                if (errorCount < 1)
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), CONSTANTS.RegressionPrediction, CONSTANTS.START, string.IsNullOrEmpty(regresionCorelationId) ? default(Guid) : new Guid(regresionCorelationId), string.Empty, string.Empty, string.Empty, string.Empty);
                    List<JArray> timeSeriesList = new List<JArray>();
                    foreach (var data in predictions)
                    {
                        bool DBEncryption = CommonUtility.EncryptDB(data.CorrelationId, appSettings);
                        string PredictedData;
                        if (DBEncryption)
                            PredictedData = _encryptionDecryption.Decrypt(data.PredictedData);
                        else
                            PredictedData = data.PredictedData;

                        JArray array = JArray.Parse(PredictedData);
                        foreach (JObject elem in array)
                        {
                            foreach (var elementToRemove in new List<string>() { CONSTANTS.UpperBound, CONSTANTS.LowerBound, CONSTANTS.ConfidenceInterval })
                            {
                                elem.Property(elementToRemove).Remove();
                            }
                        }
                        timeSeriesList.Add(array);
                    }
                    StringBuilder stringBuilder = new StringBuilder();
                    if (timeSeriesList.Count > 0)
                    {
                        for (int j = 0; j < timeSeriesList[0].Count; j++)
                        {
                            for (int i = 0; i < timeSeriesList.Count; i++)
                            {
                                if (i == 0)
                                {
                                    if (stringBuilder.Length > 0)
                                    {
                                        stringBuilder.Append("},{");
                                    }
                                    var j4 = JObject.Parse(timeSeriesList[i][j].ToString());
                                    foreach (JToken item in j4.Children())
                                    {
                                        JProperty prop = item as JProperty;
                                        if (prop != null)
                                        {
                                            stringBuilder.Append(item + ",");
                                        }
                                    }
                                }
                                else
                                {
                                    var j4 = JObject.Parse(timeSeriesList[i][j].ToString());
                                    foreach (JToken item in j4.Children())
                                    {
                                        JProperty prop = item as JProperty;
                                        if (prop != null & prop.Name != "Date")
                                        {
                                            stringBuilder.Append(CONSTANTS.slash + prop.Name + CONSTANTS.slash + ":" + prop.Value.ToObject<dynamic>());
                                        }
                                    }
                                    if (i != timeSeriesList.Count - 1)
                                    {
                                        stringBuilder.Append(",");
                                    }
                                }
                            }
                        }
                        string regression = "[{" + stringBuilder.ToString() + "}]";
                        PredictionDTO regressionPrediction = new PredictionDTO
                        {
                            _id = Guid.NewGuid().ToString(),
                            UniqueId = Guid.NewGuid().ToString(),
                            // ActualData = regression,
                            CorrelationId = regresionCorelationId,
                            PredictedData = null,
                            Status = CONSTANTS.I,
                            InstaId = instaId,
                            ErrorMessage = null,
                            Progress = null,
                            CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                            ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                            CreatedByUser = regressionModel.CreatedByUser,
                            ModifiedByUser = regressionModel.CreatedByUser
                        };
                        bool DBEncryptionRequired = CommonUtility.EncryptDB(regressionPrediction.CorrelationId, appSettings);
                        // db data encrypt
                        if (DBEncryptionRequired)
                        {
                            regressionPrediction.ActualData = _encryptionDecryption.Encrypt(regression);
                        }
                        else
                        {
                            regressionPrediction.ActualData = regression;
                        }
                        _deployedModelService.SavePrediction(regressionPrediction);
                        DeployModelsDto mdl = _deployedModelService.GetDeployModelDetails(regresionCorelationId);
                        IngrainRequestQueue ingrainRequest = new IngrainRequestQueue
                        {
                            _id = Guid.NewGuid().ToString(),
                            AppID = mdl.AppId,
                            CorrelationId = regresionCorelationId,
                            RequestId = Guid.NewGuid().ToString(),
                            ProcessId = null,
                            Status = CONSTANTS.I,//null,
                            ModelName = null,
                            RequestStatus = appSettings.Value.IsFlaskCall ? CONSTANTS.Occupied : CONSTANTS.New,
                            RetryCount = 0,
                            ProblemType = null,
                            Message = null,
                            UniId = regressionPrediction.UniqueId,
                            Progress = null,
                            pageInfo = CONSTANTS.PublishModel, // pageInfo 
                            ParamArgs = CONSTANTS.CurlyBraces,
                            Function = CONSTANTS.PublishModel,
                            CreatedByUser = CONSTANTS.System,
                            CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                            ModifiedByUser = CONSTANTS.System,
                            ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                            LastProcessedOn = null,
                            IsForAPI = appSettings.Value.IsFlaskCall ? true : false
                        };
                        _ingestedDataService.InsertRequests(ingrainRequest);
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), CONSTANTS.InsertRequest, CONSTANTS.START, string.IsNullOrEmpty(regresionCorelationId) ? default(Guid) : new Guid(regresionCorelationId), string.Empty, string.Empty, string.Empty, string.Empty);
                        if (appSettings.Value.IsFlaskCall)
                        {
                            _iFlaskAPIService.CallPython(ingrainRequest.CorrelationId, ingrainRequest.UniId, ingrainRequest.pageInfo);
                        }
                        Thread.Sleep(2000);
                        bool isregressionPrediction = true;
                        PredictionDTO regressionData = new PredictionDTO();
                        while (isregressionPrediction)
                        {
                            regressionData = _deployedModelService.GetPrediction(regressionPrediction);
                            if (regressionData.Status == CONSTANTS.C)
                            {
                                instaMLResponse instaMLResponse = new instaMLResponse();
                                instaMLResponse.Message = Resource.IngrainResx.Predictioncompleted;
                                instaMLResponse.PredictedData = JsonConvert.DeserializeObject<List<dynamic>>(regressionData.PredictedData);
                                instaMLResponse.Status = CONSTANTS.C;
                                instaMLResponse.CorrelationId = regresionCorelationId;
                                instaMLResponse.InstaID = instaId;
                                regressionResponse.Add(instaMLResponse);
                                isregressionPrediction = false;
                            }
                            else if (regressionData.Status == CONSTANTS.E)
                            {
                                instaMLResponse instaMLResponse = new instaMLResponse();
                                instaMLResponse.ActualData = regressionData.ActualData;
                                instaMLResponse.Message = CONSTANTS.IngestDataError;
                                instaMLResponse.CorrelationId = regresionCorelationId;
                                instaMLResponse.InstaID = instaId;
                                instaMLResponse.ErrorMessage = CONSTANTS.IngestDataError + "-" + regressionData.ErrorMessage;
                                instaMLResponse.Status = CONSTANTS.E;
                                regressionResponse.Add(instaMLResponse);
                                isregressionPrediction = false;
                            }
                            else
                            {
                                Thread.Sleep(1000);
                                isPrediction = true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InstaModelService), nameof(Prediction), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                instaMLResponse instaML1 = new instaMLResponse();
                instaML1.CorrelationId = predictions[0].CorrelationId;
                instaML1.InstaID = predictions[0].InstaId;
                instaML1.Status = CONSTANTS.E;
                instaML1.Message = Resource.IngrainResx.PredictionFailed;
                instaML1.ErrorMessage = ex.Message + CONSTANTS.ThreeDots + ex.StackTrace;
                regressionResponse.Add(instaML1);
            }
            instaRegression.UseCaseID = regressionModel.UseCaseID;
            instaRegression.instaMLResponse = regressionResponse;
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), nameof(GetRegressionPrediction), CONSTANTS.END, string.IsNullOrEmpty(regressionModel.ProblemTypeDetails[0].CorrelationId) ? default(Guid) : new Guid(regressionModel.ProblemTypeDetails[0].CorrelationId), string.Empty, string.Empty, string.Empty, string.Empty);
            return instaRegression;
        }

        private List<PredictionDTO> GetRegressionResult(List<string> uniqueIds)
        {
            List<PredictionDTO> predictionDTOs = new List<PredictionDTO>();
            PredictionDTO prediction = new PredictionDTO();
            var collection = _database.GetCollection<PredictionDTO>(CONSTANTS.SSAI_PublishModel);
            foreach (var id in uniqueIds)
            {
                var filter = Builders<PredictionDTO>.Filter.Eq(CONSTANTS.UniqueId, id);
                var result = collection.Find(filter).ToList();
                if (result.Count > 0)
                {
                    predictionDTOs.Add(result[0]);
                }
            }
            return predictionDTOs;
        }
        /// <summary>
        /// Get Prediction Data
        /// </summary>
        /// <param name="instaId"></param>
        /// <param name="correlationId"></param>
        /// <param name="userId"></param>
        /// <param name="ProblemType"></param>
        /// <returns></returns>
        public InstaPrediction Prediction(string instaId, string correlationId, string userId, string ProblemType, string ActualData)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), nameof(Prediction), CONSTANTS.START, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            InstaPrediction instaPrediction = new InstaPrediction();
            try
            {

                instaPrediction.InstaID = instaId;
                instaPrediction.CorrelationId = correlationId;
                instaPrediction.ProcessName = CONSTANTS.Prediction;
                instaPrediction.ProblemType = ProblemType;
                instaPrediction.ActualData = ActualData;
                _deployModelViewModel = _deployedModelService.GetDeployModel(correlationId);
                if (_deployModelViewModel != null && _deployModelViewModel.DeployModels.Count > 0)
                {
                    string data = CONSTANTS.Null;
                    PredictionDTO predictionData = new PredictionDTO();
                    string frequency = _deployModelViewModel.DeployModels[0].Frequency;
                    PredictionDTO predictionDTO = new PredictionDTO
                    {
                        _id = Guid.NewGuid().ToString(),
                        UniqueId = Guid.NewGuid().ToString(),
                        //ActualData = data,
                        CorrelationId = correlationId,
                        Frequency = frequency,
                        PredictedData = null,
                        Status = CONSTANTS.I,
                        ErrorMessage = null,
                        Progress = null,
                        CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                        ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                        CreatedByUser = userId,
                        ModifiedByUser = userId
                    };

                    // db data encrypt
                    bool DBEncryptionRequired = CommonUtility.EncryptDB(correlationId, appSettings);
                    if (DBEncryptionRequired)
                    {
                        predictionDTO.ActualData = _encryptionDecryption.Encrypt(data);
                    }
                    else
                    {
                        predictionDTO.ActualData = data;
                    }

                    if (ProblemType == CONSTANTS.TimeSeries)
                    {
                        _deployedModelService.SavePrediction(predictionDTO);
                        DeployModelsDto mdl = _deployedModelService.GetDeployModelDetails(correlationId);

                        IngrainRequestQueue ingrainRequest = new IngrainRequestQueue
                        {
                            _id = Guid.NewGuid().ToString(),
                            AppID = mdl.AppId,
                            CorrelationId = correlationId,
                            RequestId = Guid.NewGuid().ToString(),
                            ProcessId = null,
                            Status = null,
                            ModelName = null,
                            RequestStatus = appSettings.Value.IsFlaskCall ? "Occupied" : CONSTANTS.New,
                            RetryCount = 0,
                            ProblemType = null,
                            Message = null,
                            UniId = predictionDTO.UniqueId,
                            Progress = null,
                            pageInfo = CONSTANTS.ForecastModel, // pageInfo 
                            ParamArgs = CONSTANTS.CurlyBraces,
                            Function = CONSTANTS.ForecastModel,
                            CreatedByUser = userId,
                            CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                            ModifiedByUser = userId,
                            ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                            LastProcessedOn = null,
                            IsForAPI = appSettings.Value.IsFlaskCall ? true : false,
                        };
                        _ingestedDataService.InsertRequests(ingrainRequest);
                        if (appSettings.Value.IsFlaskCall)
                        {
                            _iFlaskAPIService.CallPython(ingrainRequest.CorrelationId, ingrainRequest.UniId, ingrainRequest.pageInfo);
                        }
                        Thread.Sleep(2000);
                        bool isPrediction = true;
                        while (isPrediction)
                        {
                            predictionData = _deployedModelService.GetPrediction(predictionDTO);
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), nameof(Prediction), CONSTANTS.APIResult + predictionData.Status, string.Empty, string.Empty, string.Empty, string.Empty);

                            if (predictionData.Status == CONSTANTS.C)
                            {
                                instaPrediction.Message = Resource.IngrainResx.Predictioncompleted;
                                instaPrediction.PredictedData = predictionData.PredictedData;
                                instaPrediction.Status = CONSTANTS.C;
                                isPrediction = false;
                            }
                            else if (predictionData.Status == CONSTANTS.E)
                            {
                                isPrediction = false;
                                instaPrediction.Message = predictionData.ErrorMessage;
                                instaPrediction.Status = CONSTANTS.E;
                                return instaPrediction;
                            }
                            else
                            {
                                Thread.Sleep(1000);
                                isPrediction = true;
                            }
                        }
                    }
                    else
                    {
                        _deployedModelService.SavePrediction(predictionDTO);
                        DeployModelsDto mdl = _deployedModelService.GetDeployModelDetails(correlationId);
                        IngrainRequestQueue ingrainRequest = new IngrainRequestQueue
                        {
                            _id = Guid.NewGuid().ToString(),
                            AppID = mdl.AppId,
                            CorrelationId = correlationId,
                            RequestId = Guid.NewGuid().ToString(),
                            ProcessId = null,
                            Status = CONSTANTS.I,//null,
                            ModelName = null,
                            RequestStatus = appSettings.Value.IsFlaskCall ? "Occupied" : CONSTANTS.New,
                            RetryCount = 0,
                            ProblemType = null,
                            Message = null,
                            UniId = predictionDTO.UniqueId,
                            Progress = null,
                            pageInfo = CONSTANTS.PublishModel, // pageInfo 
                            ParamArgs = CONSTANTS.CurlyBraces,
                            Function = CONSTANTS.PublishModel,
                            CreatedByUser = CONSTANTS.System,
                            CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                            ModifiedByUser = CONSTANTS.System,
                            ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                            LastProcessedOn = null,
                            IsForAPI = appSettings.Value.IsFlaskCall ? true : false,
                        };
                        _ingestedDataService.InsertRequests(ingrainRequest);
                        if (appSettings.Value.IsFlaskCall)
                        {
                            _iFlaskAPIService.CallPython(ingrainRequest.CorrelationId, ingrainRequest.UniId, ingrainRequest.pageInfo);
                        }
                        Thread.Sleep(2000);
                        bool isPrediction = true;
                        while (isPrediction)
                        {
                            predictionData = _deployedModelService.GetPrediction(predictionDTO);
                            if (predictionData.Status == CONSTANTS.C)
                            {
                                instaPrediction.Message = Resource.IngrainResx.Predictioncompleted;
                                instaPrediction.PredictedData = predictionData.PredictedData;
                                instaPrediction.Status = CONSTANTS.C;
                                isPrediction = false;
                            }
                            else if (predictionData.Status == CONSTANTS.E)
                            {
                                isPrediction = false;
                                instaPrediction.Message = Resource.IngrainResx.PredictionFailed;
                                instaPrediction.Status = CONSTANTS.E;
                                return instaPrediction;
                            }
                            else
                            {
                                Thread.Sleep(1000);
                                isPrediction = true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InstaModelService), nameof(Prediction), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                instaPrediction.ErrorMessage = ex.Message;
                instaPrediction.Status = CONSTANTS.E;
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), nameof(Prediction), CONSTANTS.End, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            return instaPrediction;
        }

        /// <summary>
        /// Delete the Model
        /// </summary>
        /// <param name="instaId"></param>
        /// <param name="correlationId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public InstaModel DeleteModel(string instaId, string correlationId, string userId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), nameof(DeleteModel), CONSTANTS.START, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                string flushStatus = _flushService.InstaMLDeleteModel(correlationId);
                if (!string.IsNullOrEmpty(flushStatus))
                {
                    instaModel.CorrelationId = correlationId;
                    instaModel.InstaID = instaId;
                    instaModel.Status = CONSTANTS.C;
                    instaModel.Message = Resource.IngrainResx.DeleteModelcompleted;
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InstaModelService), nameof(DeleteModel), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                instaModel.ErrorMessage = ex.Message;
                instaModel.Status = CONSTANTS.E;
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), nameof(DeleteModel), CONSTANTS.END, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            return instaModel;
        }

        private void RemoveQueueRecords(string correlationId, string pageInfo)
        {
            var builder = Builders<BsonDocument>.Filter;
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
            var filter = builder.Eq(CONSTANTS.CorrelationId, correlationId) & builder.Eq(CONSTANTS.pageInfo, pageInfo);
            var result = collection.DeleteMany(filter);
        }
        /// <summary>
        /// Refit the Model
        /// </summary>
        /// <param name="vdsData"></param>
        /// <returns>Model Prediction</returns>
        public InstaRegression RegressionRefitModel(VDSRegression regressionModel)
        {
            bool IsActualData = false;
            List<PredictionDTO> predictions = new List<PredictionDTO>();
            string regresionCorelationId = string.Empty;
            string regresionInstaId = string.Empty;
            List<string> uniqueIds = new List<string>();
            string token = string.Empty;
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), nameof(RegressionRefitModel), CONSTANTS.START, string.Empty, string.Empty, regressionModel.ClientUID,regressionModel.DCID);
            string resultString = string.Empty;
            bool DBEncryptionRequired = CommonUtility.EncryptDB(regressionModel.ProblemTypeDetails[0].CorrelationId, appSettings);
            string correlationId = string.Empty;
            string instaId = string.Empty;
            try
            {
                foreach (var item in regressionModel.ProblemTypeDetails)
                {
                    correlationId = item.CorrelationId;
                    instaId = item.InstaID;
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), nameof(RegressionRefitModel), CONSTANTS.START, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty,string.Empty, regressionModel.ClientUID, regressionModel.DCID);
                    if (item.ProblemType == CONSTANTS.TimeSeries)
                    {
                        RegressionTimeSeriesModel fitModel = new RegressionTimeSeriesModel();
                        if (!IsActualData)
                        {
                            fitModel.InstaID = item.InstaID;
                            fitModel.ClientUID = regressionModel.ClientUID;
                            fitModel.DCID = regressionModel.DCID;
                            fitModel.ProcessFlow = CONSTANTS.IncrementalLoad;
                            fitModel.UseCaseID = regressionModel.UseCaseID;
                            string lastFitDate = this.GetFitDate(item.CorrelationId, item.InstaID);
                            string responseModel1 = JsonConvert.SerializeObject(fitModel);
                            fitModel.lastFitDate = DateTime.Parse(lastFitDate).ToString("dd-MM-yyyy HH:mm:ss");
                             LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), nameof(RegressionRefitModel), CONSTANTS.RefitModelParams + responseModel1.Replace(CONSTANTS.slash, string.Empty), string.Empty, string.Empty, regressionModel.ClientUID, regressionModel.DCID);

                        }
                        //PAD
                        if (regressionModel.Source == CONSTANTS.Source)
                        {
                            if (!IsActualData)
                            {
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), nameof(RefitModel), CONSTANTS.VDSTOKENSTART, string.Empty,  string.Empty, regressionModel.ClientUID, regressionModel.DCID);
                                token = VDSSecurityTokenForPAD();
                                var postData = JsonConvert.SerializeObject(fitModel, Formatting.None, new JsonSerializerSettings()
                                {
                                    DateFormatHandling = DateFormatHandling.MicrosoftDateFormat,
                                    DateParseHandling = DateParseHandling.DateTimeOffset
                                });
                                var postDataByte = Encoding.ASCII.GetBytes(postData);
                                HttpWebRequest request = WebRequest.Create(regressionModel.URL) as HttpWebRequest;
                                request.Method = CONSTANTS.POST;
                                request.ContentType = CONSTANTS.APPLICATION_JSON;
                                request.ContentLength = postDataByte.Length;
                                request.Headers.Add(CONSTANTS.Authorization, CONSTANTS.bearer + token);
                                string loadBuilder = Newtonsoft.Json.JsonConvert.SerializeObject(request, Formatting.Indented).Replace(CONSTANTS.r_n, string.Empty).Replace(CONSTANTS.slash, string.Empty);
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), nameof(VDSSecurityTokenForPAD), CONSTANTS.VdsParams + loadBuilder, string.Empty,  string.Empty, regressionModel.ClientUID, regressionModel.DCID);
                                using (var Stream = request.GetRequestStream())
                                {
                                    Stream.Write(postDataByte, 0, postDataByte.Length);
                                }
                                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                                StreamReader reader = new StreamReader(response.GetResponseStream());
                                resultString = reader.ReadToEnd();
                                response.Close();
                            }
                              LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), nameof(RegressionRefitModel), CONSTANTS.VdsEnd, string.Empty,  string.Empty, regressionModel.ClientUID, regressionModel.DCID);
                        }
                        //FDS
                        else if (regressionModel.Source == CONSTANTS.FDS)
                        {
                            if (!IsActualData)
                            {
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), nameof(RefitModel), CONSTANTS.VDSTOKENSTART, string.Empty, string.Empty, regressionModel.ClientUID, regressionModel.DCID);
                                token = VDSSecurityTokenForManagedInstance();
                                var postData = JsonConvert.SerializeObject(fitModel, Formatting.None, new JsonSerializerSettings()
                                {
                                    DateFormatHandling = DateFormatHandling.MicrosoftDateFormat,
                                    DateParseHandling = DateParseHandling.DateTimeOffset
                                });
                                var postDataByte = Encoding.ASCII.GetBytes(postData);
                                HttpWebRequest request = WebRequest.Create(regressionModel.URL) as HttpWebRequest;
                                request.Method = CONSTANTS.POST;
                                request.ContentType = CONSTANTS.APPLICATION_JSON;
                                request.ContentLength = postDataByte.Length;
                                request.Headers.Add(CONSTANTS.Authorization, CONSTANTS.bearer + token);
                                string loadBuilder = Newtonsoft.Json.JsonConvert.SerializeObject(request, Formatting.Indented).Replace(CONSTANTS.r_n, string.Empty).Replace(CONSTANTS.slash, string.Empty);
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), nameof(VDSSecurityTokenForPAD), CONSTANTS.VdsParams + loadBuilder, string.Empty, string.Empty, regressionModel.ClientUID, regressionModel.DCID);
                                using (var Stream = request.GetRequestStream())
                                {
                                    Stream.Write(postDataByte, 0, postDataByte.Length);
                                }
                                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                                StreamReader reader = new StreamReader(response.GetResponseStream());
                                resultString = reader.ReadToEnd();
                                response.Close();
                            }
                             LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), nameof(RegressionRefitModel), CONSTANTS.VdsEnd, string.Empty, string.Empty, regressionModel.ClientUID, regressionModel.DCID);
                        }
                        //PAM
                        else
                        {
                            if (!IsActualData)
                            {
                                token = VDSSecurityTokenForPAM();
                                var postData = JsonConvert.SerializeObject(fitModel, Formatting.None, new JsonSerializerSettings()
                                {
                                    DateFormatHandling = DateFormatHandling.MicrosoftDateFormat,
                                    DateParseHandling = DateParseHandling.DateTimeOffset
                                });
                                var postDataByte = Encoding.ASCII.GetBytes(postData);
                                HttpWebRequest request = WebRequest.Create(regressionModel.URL) as HttpWebRequest;
                                request.Method = CONSTANTS.POST;
                                request.ContentType = CONSTANTS.APPLICATION_JSON;
                                request.ContentLength = postDataByte.Length;
                                if (appSettings.Value.Environment == CONSTANTS.PAMEnvironment)
                                {
                                    request.Headers.Add(CONSTANTS.Authorization, token);
                                }
                                else //This block is for SaaS-PAM
                                {
                                    request.Headers.Add(CONSTANTS.Authorization, CONSTANTS.bearer + token);
                                }
                                using (var Stream = request.GetRequestStream())
                                {
                                    Stream.Write(postDataByte, 0, postDataByte.Length);
                                }
                                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                                StreamReader reader = new StreamReader(response.GetResponseStream());
                                resultString = reader.ReadToEnd();
                                response.Close();
                            }
                        }
                        //Prediction start                        

                        _deployModelViewModel = _deployedModelService.GetDeployModel(item.CorrelationId);
                        if (_deployModelViewModel != null && _deployModelViewModel.DeployModels.Count > 0)
                        {
                            string frequency = _deployModelViewModel.DeployModels[0].Frequency;
                            PredictionDTO predictionDTO = new PredictionDTO
                            {
                                _id = Guid.NewGuid().ToString(),
                                UniqueId = Guid.NewGuid().ToString(),
                                CorrelationId = item.CorrelationId,
                                Frequency = frequency,
                                PredictedData = null,
                                InstaId = item.InstaID,
                                UseCaseId = regressionModel.UseCaseID,
                                Status = CONSTANTS.I,
                                ErrorMessage = null,
                                Progress = null,
                                CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                                ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                                CreatedByUser = regressionModel.CreatedByUser,
                                ModifiedByUser = regressionModel.CreatedByUser
                            };
                            if (DBEncryptionRequired)
                            {
                                predictionDTO.ActualData = _encryptionDecryption.Encrypt(CONSTANTS.Null);
                            }
                            else
                            {
                                predictionDTO.ActualData = CONSTANTS.Null;
                            }
                            if (!string.IsNullOrEmpty(resultString))
                            {
                                JObject Jdata = JObject.Parse(resultString);
                                if (Jdata["IntsaMLTrainingDataResponse"] != null)
                                {
                                    foreach (var child in Jdata["IntsaMLTrainingDataResponse"].Children())
                                    {
                                        if (child["ActualData"] != null & child["ActualData"].ToString() != "[]")
                                        {
                                            JArray Jdata2 = JArray.Parse(child["ActualData"].ToString());
                                            string jsonString = JsonConvert.SerializeObject(Jdata2);
                                            string data = jsonString.Replace("null", @"""""");
                                            IsActualData = true;
                                            if (item.InstaID == child[CONSTANTS.InstaID].ToString())
                                            {
                                                if (DBEncryptionRequired)
                                                {
                                                    predictionDTO.ActualData = _encryptionDecryption.Encrypt(data);
                                                }
                                                else
                                                {
                                                    predictionDTO.ActualData = data;
                                                }
                                                break;
                                            }
                                        }
                                    }
                                }

                                _deployedModelService.SavePrediction(predictionDTO);
                                uniqueIds.Add(predictionDTO.UniqueId);
                                DeployModelsDto mdl = _deployedModelService.GetDeployModelDetails(item.CorrelationId);
                                IngrainRequestQueue ingrainRequest = new IngrainRequestQueue
                                {
                                    _id = Guid.NewGuid().ToString(),
                                    AppID = mdl.AppId,
                                    CorrelationId = item.CorrelationId,
                                    RequestId = Guid.NewGuid().ToString(),
                                    ProcessId = null,
                                    Status = null,
                                    ModelName = null,
                                    RequestStatus = appSettings.Value.IsFlaskCall ? CONSTANTS.Occupied : CONSTANTS.New,
                                    RetryCount = 0,
                                    UseCaseID = regressionModel.UseCaseID,
                                    ProblemType = null,
                                    Message = null,
                                    UniId = predictionDTO.UniqueId,
                                    Progress = null,
                                    pageInfo = CONSTANTS.ForecastModel,
                                    ParamArgs = CONSTANTS.CurlyBraces,
                                    Function = CONSTANTS.ForecastModel,
                                    CreatedByUser = regressionModel.CreatedByUser,
                                    CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                                    ModifiedByUser = regressionModel.CreatedByUser,
                                    ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                                    LastProcessedOn = null,
                                    IsForAPI = appSettings.Value.IsFlaskCall ? true : false
                                };
                                _ingestedDataService.InsertRequests(ingrainRequest);
                                if (appSettings.Value.IsFlaskCall)
                                {
                                    _iFlaskAPIService.CallPython(ingrainRequest.CorrelationId, ingrainRequest.UniId, ingrainRequest.pageInfo);
                                }
                                Thread.Sleep(1000);
                            }
                        }
                        else
                        {
                            instaMLResponse response = new instaMLResponse();
                            response.CorrelationId = item.CorrelationId;
                            response.InstaID = item.InstaID;
                            response.Message = CONSTANTS.ModelNotAvailable;
                            response.Status = CONSTANTS.E;
                            response.ErrorMessage = CONSTANTS.ModelNotAvailable;
                            regressionResponse.Add(response);
                        }
                    }
                    else
                    {
                        regresionCorelationId = item.CorrelationId;
                        regresionInstaId = item.InstaID;
                    }
                }
                bool isPrediction = true;
                int errorCount = 0;
                while (isPrediction)
                {
                    int successCount = 0;
                    int progressCount = 0;
                    Thread.Sleep(1000);
                    predictions = GetRegressionResult(uniqueIds);
                    foreach (var prediction in predictions)
                    {
                        if (errorCount == 0)
                        {
                            if (prediction.Status == CONSTANTS.I || prediction.Status == CONSTANTS.P)
                            {
                                progressCount++;
                                isPrediction = true;
                                errorCount = 0;
                            }
                        }
                        if (prediction.Status == CONSTANTS.E)
                        {
                            errorCount++;
                            instaMLResponse response = new instaMLResponse();
                            response.CorrelationId = prediction.CorrelationId;
                            response.InstaID = prediction.InstaId;
                            response.Message = CONSTANTS.IngestDataError;
                            response.Status = prediction.Status;
                            response.ErrorMessage = CONSTANTS.IngestDataError + "-" + prediction.ErrorMessage;
                            regressionResponse.Add(response);
                        }
                    }
                    if (errorCount > 0)
                    {
                        isPrediction = false;
                    }
                    else if (progressCount == 0)
                    {
                        foreach (var prediction in predictions)
                        {
                            if (prediction.Status == CONSTANTS.C)
                            {
                                bool DBEncryption = CommonUtility.EncryptDB(prediction.CorrelationId, appSettings);
                                string PredictedData;
                                if (DBEncryption)
                                    PredictedData = _encryptionDecryption.Decrypt(prediction.PredictedData);
                                else
                                    PredictedData = prediction.PredictedData;

                                isPrediction = false;
                                instaMLResponse response = new instaMLResponse();
                                response.CorrelationId = prediction.CorrelationId;
                                response.InstaID = prediction.InstaId;
                                response.Status = prediction.Status;
                                response.Message = Resource.IngrainResx.Predictioncompleted;
                                response.PredictedData = JsonConvert.DeserializeObject<List<dynamic>>(PredictedData);
                                regressionResponse.Add(response);
                                successCount++;
                            }
                        }
                    }
                }
                if (errorCount < 1)
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), CONSTANTS.RegressionPrediction, CONSTANTS.START, string.IsNullOrEmpty(regresionCorelationId) ? default(Guid) : new Guid(regresionCorelationId) , string.Empty, string.Empty, regressionModel.ClientUID, regressionModel.DCID);
                    List<JArray> timeSeriesList = new List<JArray>();
                    foreach (var data in predictions)
                    {
                        bool DBEncryption = CommonUtility.EncryptDB(data.CorrelationId, appSettings);
                        string PredictedData;
                        if (DBEncryption)
                            PredictedData = _encryptionDecryption.Decrypt(data.PredictedData);
                        else
                            PredictedData = data.PredictedData;

                        JArray array = JArray.Parse(PredictedData);
                        foreach (JObject elem in array)
                        {
                            foreach (var elementToRemove in new List<string>() { CONSTANTS.UpperBound, CONSTANTS.LowerBound, CONSTANTS.ConfidenceInterval })
                            {
                                elem.Property(elementToRemove).Remove();
                            }
                        }
                        timeSeriesList.Add(array);
                    }
                    StringBuilder stringBuilder = new StringBuilder();
                    if (timeSeriesList.Count > 0)
                    {
                        for (int j = 0; j < timeSeriesList[0].Count; j++)
                        {
                            for (int i = 0; i < timeSeriesList.Count; i++)
                            {
                                if (i == 0)
                                {
                                    if (stringBuilder.Length > 0)
                                    {
                                        stringBuilder.Append("},{");
                                    }
                                    var j4 = JObject.Parse(timeSeriesList[i][j].ToString());
                                    foreach (JToken item in j4.Children())
                                    {
                                        JProperty prop = item as JProperty;
                                        if (prop != null)
                                        {
                                            stringBuilder.Append(item + ",");
                                        }
                                    }
                                }
                                else
                                {
                                    var j4 = JObject.Parse(timeSeriesList[i][j].ToString());
                                    foreach (JToken item in j4.Children())
                                    {
                                        JProperty prop = item as JProperty;
                                        if (prop != null & prop.Name != "Date")
                                        {
                                            stringBuilder.Append(CONSTANTS.slash + prop.Name + CONSTANTS.slash + ":" + prop.Value.ToObject<dynamic>());
                                        }
                                    }
                                    if (i != timeSeriesList.Count - 1)
                                    {
                                        stringBuilder.Append(",");
                                    }
                                }
                            }
                        }
                        string regression = "[{" + stringBuilder.ToString() + "}]";
                        PredictionDTO regressionPrediction = new PredictionDTO
                        {
                            _id = Guid.NewGuid().ToString(),
                            UniqueId = Guid.NewGuid().ToString(),
                            CorrelationId = regresionCorelationId,
                            PredictedData = null,
                            Status = CONSTANTS.I,
                            InstaId = regresionInstaId,
                            ErrorMessage = null,
                            Progress = null,
                            CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                            ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                            CreatedByUser = regressionModel.CreatedByUser,
                            ModifiedByUser = regressionModel.CreatedByUser
                        };
                        DBEncryptionRequired = CommonUtility.EncryptDB(regressionPrediction.CorrelationId, appSettings);
                        // db data encrypt
                        if (DBEncryptionRequired)
                        {
                            regressionPrediction.ActualData = _encryptionDecryption.Encrypt(regression);
                        }
                        else
                        {
                            regressionPrediction.ActualData = regression;
                        }
                        _deployedModelService.SavePrediction(regressionPrediction);
                        DeployModelsDto mdl = _deployedModelService.GetDeployModelDetails(regresionCorelationId);
                        IngrainRequestQueue ingrainRequest = new IngrainRequestQueue
                        {
                            _id = Guid.NewGuid().ToString(),
                            AppID = mdl.AppId,
                            CorrelationId = regresionCorelationId,
                            RequestId = Guid.NewGuid().ToString(),
                            ProcessId = null,
                            Status = CONSTANTS.I,//null,
                            ModelName = null,
                            RequestStatus = appSettings.Value.IsFlaskCall ? CONSTANTS.Occupied : CONSTANTS.New,
                            RetryCount = 0,
                            ProblemType = null,
                            Message = null,
                            UniId = regressionPrediction.UniqueId,
                            Progress = null,
                            pageInfo = CONSTANTS.PublishModel,
                            ParamArgs = CONSTANTS.CurlyBraces,
                            Function = CONSTANTS.PublishModel,
                            CreatedByUser = CONSTANTS.System,
                            CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                            ModifiedByUser = CONSTANTS.System,
                            ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                            LastProcessedOn = null,
                            IsForAPI = appSettings.Value.IsFlaskCall ? true : false
                        };
                        _ingestedDataService.InsertRequests(ingrainRequest);
                        if (appSettings.Value.IsFlaskCall)
                        {
                            _iFlaskAPIService.CallPython(ingrainRequest.CorrelationId, ingrainRequest.UniId, ingrainRequest.pageInfo);
                        }
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), CONSTANTS.InsertRequest, CONSTANTS.START, string.IsNullOrEmpty(regresionCorelationId) ? default(Guid) : new Guid(regresionCorelationId), string.Empty, string.Empty, regressionModel.ClientUID, regressionModel.DCID);
                        Thread.Sleep(2000);
                        bool isregressionPrediction = true;
                        PredictionDTO regressionData = new PredictionDTO();
                        while (isregressionPrediction)
                        {
                            regressionData = _deployedModelService.GetPrediction(regressionPrediction);
                            if (regressionData.Status == CONSTANTS.C)
                            {
                                instaMLResponse instaMLResponse = new instaMLResponse();
                                instaMLResponse.Message = Resource.IngrainResx.Predictioncompleted;
                                instaMLResponse.PredictedData = JsonConvert.DeserializeObject<List<dynamic>>(regressionData.PredictedData);
                                instaMLResponse.Status = CONSTANTS.C;
                                instaMLResponse.CorrelationId = regresionCorelationId;
                                instaMLResponse.InstaID = regresionInstaId;
                                regressionResponse.Add(instaMLResponse);
                                isregressionPrediction = false;
                            }
                            else if (regressionData.Status == CONSTANTS.E)
                            {
                                instaMLResponse instaMLResponse = new instaMLResponse();
                                instaMLResponse.ActualData = regressionData.ActualData;
                                instaMLResponse.Message = CONSTANTS.IngestDataError;
                                instaMLResponse.CorrelationId = regresionCorelationId;
                                instaMLResponse.InstaID = regresionInstaId;
                                instaMLResponse.ErrorMessage = CONSTANTS.IngestDataError + "-" + regressionData.ErrorMessage;
                                instaMLResponse.Status = CONSTANTS.E;
                                regressionResponse.Add(instaMLResponse);
                                isregressionPrediction = false;
                            }
                            else
                            {
                                Thread.Sleep(1000);
                                isPrediction = true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InstaModelService), nameof(Prediction), ex.Message, ex, string.Empty, string.Empty, regressionModel.ClientUID, regressionModel.DCID);
                instaMLResponse instaML1 = new instaMLResponse();
                instaML1.CorrelationId = regressionModel.ProblemTypeDetails[0].CorrelationId;
                instaML1.InstaID = regressionModel.ProblemTypeDetails[0].InstaID;
                instaML1.Status = CONSTANTS.E;
                instaML1.Message = Resource.IngrainResx.PredictionFailed;
                instaML1.ErrorMessage = ex.Message + CONSTANTS.ThreeDots + ex.StackTrace;
                regressionResponse.Add(instaML1);
            }
            instaRegression.UseCaseID = regressionModel.UseCaseID;
            instaRegression.instaMLResponse = regressionResponse;
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), nameof(GetRegressionPrediction), CONSTANTS.END, string.IsNullOrEmpty(regressionModel.ProblemTypeDetails[0].CorrelationId) ? default(Guid) : new Guid(regressionModel.ProblemTypeDetails[0].CorrelationId), string.Empty,  string.Empty, regressionModel.ClientUID, regressionModel.DCID);
            return instaRegression;
        }

        /// <summary>
        /// Refit the Model
        /// </summary>
        /// <param name="vdsData"></param>
        /// <returns>Model Prediction</returns>
        public InstaPrediction RefitModel(VdsData vdsData)
        {
            InstaPrediction instaPrediction = new InstaPrediction();
            string token = string.Empty;
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), nameof(RefitModel), CONSTANTS.START, string.IsNullOrEmpty(vdsData.CorrelationId) ? default(Guid) : new Guid(vdsData.CorrelationId), string.Empty, string.Empty, vdsData.ClientUId, vdsData.DCID);
            string resultString = string.Empty;
            bool DBEncryptionRequired = CommonUtility.EncryptDB(vdsData.CorrelationId, appSettings);
            try
            {
                FitModel fitModel = new FitModel()
                {
                    InstaID = vdsData.InstaId,
                    ClientUID = vdsData.ClientUId,
                    DCID = vdsData.DeliveryConstructUID,
                    ProblemType = vdsData.ProblemType,
                    TargetColumn = vdsData.TargetColumn,
                    Dimension = vdsData.Dimension,
                    LastFitDate = null,
                    ProcessFlow = CONSTANTS.IncrementalLoad
                };
                string lastFitDate = this.GetFitDate(vdsData.CorrelationId, vdsData.InstaId);
                if (lastFitDate != string.Empty)
                {
                    if (vdsData.Frequency == CONSTANTS.Hourly)
                    {
                        if (lastFitDate != string.Empty)
                        {
                            fitModel.LastFitDate = DateTime.Parse(lastFitDate).ToString("yyyy-MM-dd HH:mm:ss");
                        }
                    }
                    else
                    {

                        fitModel.LastFitDate = DateTime.Parse(lastFitDate).ToString("yyyy-MM-dd");
                    }
                }
                else
                {
                    fitModel.LastFitDate = DateTime.Now.ToString("yyyy-MM-dd");
                }
                string responseModel = JsonConvert.SerializeObject(fitModel);
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), nameof(RefitModel), CONSTANTS.RefitModelParams + responseModel.Replace(CONSTANTS.slash, string.Empty), string.Empty, string.Empty, vdsData.ClientUId, vdsData.DCID);
                string inputUrl = new Uri(vdsData.URL.Trim()).Host;
                string vdsUrl = new Uri(appSettings.Value.GetVdsDataURL.ToString()).Host; //For FDS & PAM
                string vdsPadUrl = new Uri(appSettings.Value.VDSRawAPIUrl.ToString()).Host;
              
                string vdsEnvironment = appSettings.Value.Environment.ToString();
                if (inputUrl == vdsPadUrl && vdsEnvironment != "PAM")
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), nameof(RefitModel), CONSTANTS.VDSTOKENSTART, string.Empty, string.Empty, vdsData.ClientUId, vdsData.DCID);
                    token = VDSSecurityTokenForPAD();
                    var postData = JsonConvert.SerializeObject(fitModel, Formatting.None, new JsonSerializerSettings()
                    {
                        DateFormatHandling = DateFormatHandling.MicrosoftDateFormat,
                        DateParseHandling = DateParseHandling.DateTimeOffset
                    });
                    var postDataByte = Encoding.ASCII.GetBytes(postData);
                    HttpWebRequest request = WebRequest.Create(vdsData.URL) as HttpWebRequest;
                    request.Method = CONSTANTS.POST;
                    request.ContentType = CONSTANTS.APPLICATION_JSON;
                    request.ContentLength = postDataByte.Length;
                    request.Headers.Add(CONSTANTS.Authorization, CONSTANTS.bearer + token);
                    string loadBuilder = Newtonsoft.Json.JsonConvert.SerializeObject(request, Formatting.Indented).Replace(CONSTANTS.r_n, string.Empty).Replace(CONSTANTS.slash, string.Empty);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), nameof(VDSSecurityTokenForPAD), CONSTANTS.VdsParams + loadBuilder, string.Empty, string.Empty, vdsData.ClientUId, vdsData.DCID);
                    using (var Stream = request.GetRequestStream())
                    {
                        Stream.Write(postDataByte, 0, postDataByte.Length);
                    }
                    HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                    StreamReader reader = new StreamReader(response.GetResponseStream());
                    resultString = reader.ReadToEnd();
                    response.Close();

                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), CONSTANTS.VdsEnd, CONSTANTS.VdsEnd, string.IsNullOrEmpty(vdsData.CorrelationId) ? default(Guid) : new Guid(vdsData.CorrelationId), string.Empty, string.Empty, vdsData.ClientUId, vdsData.DCID);
                }
                //FDS
                else if (inputUrl == vdsUrl && appSettings.Value.Environment==CONSTANTS.FDSEnvironment)
                {
                     LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), nameof(RefitModel), "VDS Managed Instance call", string.Empty,  string.Empty, vdsData.ClientUId, vdsData.DCID);
                    token = VDSSecurityTokenForManagedInstance();
                    var postData = JsonConvert.SerializeObject(fitModel, Formatting.None, new JsonSerializerSettings()
                    {
                        DateFormatHandling = DateFormatHandling.MicrosoftDateFormat,
                        DateParseHandling = DateParseHandling.DateTimeOffset
                    });
                    var postDataByte = Encoding.ASCII.GetBytes(postData);
                    HttpWebRequest request = WebRequest.Create(vdsData.URL) as HttpWebRequest;
                    request.Method = CONSTANTS.POST;
                    request.ContentType = CONSTANTS.APPLICATION_JSON;
                    request.ContentLength = postDataByte.Length;
                    request.Headers.Add(CONSTANTS.Authorization, CONSTANTS.bearer + token);
                    string loadBuilder = Newtonsoft.Json.JsonConvert.SerializeObject(request, Formatting.Indented).Replace(CONSTANTS.r_n, string.Empty).Replace(CONSTANTS.slash, string.Empty);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), nameof(VDSSecurityTokenForManagedInstance), CONSTANTS.VdsParams + loadBuilder, string.Empty,  string.Empty, vdsData.ClientUId, vdsData.DCID);
                    using (var Stream = request.GetRequestStream())
                    {
                        Stream.Write(postDataByte, 0, postDataByte.Length);
                    }
                    HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                    StreamReader reader = new StreamReader(response.GetResponseStream());
                    resultString = reader.ReadToEnd();
                    response.Close();

                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), CONSTANTS.VdsEnd, CONSTANTS.VdsEnd, string.IsNullOrEmpty(vdsData.CorrelationId) ? default(Guid) : new Guid(vdsData.CorrelationId), string.Empty, string.Empty, vdsData.ClientUId, vdsData.DCID);
                }
                //PAM
                else
                {
                    token = VDSSecurityTokenForPAM();                   
                    //token = VDSSecurityTokenForManagedInstance();
                    var postData = JsonConvert.SerializeObject(fitModel, Formatting.None, new JsonSerializerSettings()
                    {
                        DateFormatHandling = DateFormatHandling.MicrosoftDateFormat,
                        DateParseHandling = DateParseHandling.DateTimeOffset
                    });
                    var postDataByte = Encoding.ASCII.GetBytes(postData);
                    HttpWebRequest request = WebRequest.Create(vdsData.URL) as HttpWebRequest;
                    request.Method = CONSTANTS.POST;
                    request.ContentType = CONSTANTS.APPLICATION_JSON;
                    request.ContentLength = postDataByte.Length;
                    request.Headers.Add(CONSTANTS.Authorization, token);
                    using (var Stream = request.GetRequestStream())
                    {
                        Stream.Write(postDataByte, 0, postDataByte.Length);
                    }
                    HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                    StreamReader reader = new StreamReader(response.GetResponseStream());
                    resultString = reader.ReadToEnd();
                    response.Close();

                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), CONSTANTS.VdsEnd, CONSTANTS.VdsEnd, vdsData.CorrelationId, string.Empty, string.Empty, string.Empty);                    
                }

                JObject Jdata = JObject.Parse(resultString);
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), nameof(RefitModel), CONSTANTS.Actual_Data + Jdata, string.Empty, string.Empty, vdsData.ClientUId, vdsData.DCID);
                //Prediction start
                 LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), nameof(RefitModel), CONSTANTS.Actual_Data + Convert.ToString(Jdata[CONSTANTS.ActualData]), string.Empty, string.Empty, vdsData.ClientUId, vdsData.DCID);
                if (string.IsNullOrEmpty(Convert.ToString(Jdata[CONSTANTS.ActualData])))
                {
                    PredictionDTO predictionDTO = new PredictionDTO
                    {
                        _id = Guid.NewGuid().ToString(),
                        UniqueId = Guid.NewGuid().ToString(),
                        // ActualData = CONSTANTS.Null,
                        CorrelationId = vdsData.CorrelationId,
                        Frequency = vdsData.Frequency,
                        PredictedData = null,
                        Status = CONSTANTS.I,
                        ErrorMessage = null,
                        Progress = null,
                        CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                        ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                        CreatedByUser = CONSTANTS.System,
                        ModifiedByUser = CONSTANTS.System
                    };
                    // db data encrypt
                    if (DBEncryptionRequired)
                    {
                        predictionDTO.ActualData = _encryptionDecryption.Encrypt(CONSTANTS.Null);
                    }
                    else
                    {
                        predictionDTO.ActualData = CONSTANTS.Null;
                    }
                    _deployedModelService.SavePrediction(predictionDTO);
                    DeployModelsDto mdl = _deployedModelService.GetDeployModelDetails(vdsData.CorrelationId);

                    IngrainRequestQueue ingrainRequest = new IngrainRequestQueue
                    {
                        _id = Guid.NewGuid().ToString(),
                        AppID = mdl.AppId,
                        CorrelationId = vdsData.CorrelationId,
                        RequestId = Guid.NewGuid().ToString(),
                        ProcessId = null,
                        Status = null,
                        ModelName = null,
                        RequestStatus = appSettings.Value.IsFlaskCall ? CONSTANTS.Occupied : CONSTANTS.New,
                        RetryCount = 0,
                        ProblemType = null,
                        Message = null,
                        UniId = predictionDTO.UniqueId,
                        Progress = null,
                        pageInfo = CONSTANTS.ForecastModel, // pageInfo 
                        ParamArgs = CONSTANTS.CurlyBraces,
                        Function = CONSTANTS.ForecastModel,
                        CreatedByUser = CONSTANTS.System,
                        CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                        ModifiedByUser = CONSTANTS.System,
                        ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                        LastProcessedOn = null,
                        IsForAPI = appSettings.Value.IsFlaskCall ? true : false
                    };
                    _ingestedDataService.InsertRequests(ingrainRequest);
                    if (appSettings.Value.IsFlaskCall)
                    {
                        _iFlaskAPIService.CallPython(ingrainRequest.CorrelationId, ingrainRequest.UniId, ingrainRequest.pageInfo);
                    }
                    Thread.Sleep(2000);

                    bool isPrediction = true;
                    while (isPrediction)
                    {
                        var predictionData = _deployedModelService.GetPrediction(predictionDTO);
                         LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), nameof(RefitModel), CONSTANTS.APIResult + predictionData.Status, string.Empty,  string.Empty, vdsData.ClientUId, vdsData.DCID);
                        if (predictionData.PredictedData != null && predictionData.PredictedData.Contains(CONSTANTS.Havedatatill))
                        {
                            isPrediction = false;
                            instaPrediction.CorrelationId = vdsData.CorrelationId;
                            instaPrediction.InstaID = vdsData.InstaId;
                            instaPrediction.Message = predictionData.ErrorMessage;
                            instaPrediction.Status = CONSTANTS.E;
                            instaPrediction.PredictedData = null;
                        }
                        if (predictionData.Status == CONSTANTS.C)
                        {
                            isPrediction = false;
                            instaPrediction.CorrelationId = vdsData.CorrelationId;
                            instaPrediction.InstaID = vdsData.InstaId;
                            instaPrediction.Message = Resource.IngrainResx.RefitModelcompleted;
                            instaPrediction.Status = CONSTANTS.C;
                            instaPrediction.PredictedData = predictionData.PredictedData;
                        }
                        else if (predictionData.Status == CONSTANTS.E)
                        {
                            isPrediction = false;
                            instaPrediction.CorrelationId = vdsData.CorrelationId;
                            instaPrediction.InstaID = vdsData.InstaId;
                            instaPrediction.Message = predictionData.ErrorMessage;
                            instaPrediction.Status = CONSTANTS.E;
                            instaPrediction.PredictedData = null;
                        }
                        else
                        {
                            isPrediction = true;
                            Thread.Sleep(1000);
                        }
                    }

                LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), nameof(RefitModel), CONSTANTS.ActualDataLastFitDate + fitModel.LastFitDate, string.Empty,string.Empty, vdsData.ClientUId, vdsData.DCID);
                }
                else
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), nameof(RefitModel), CONSTANTS.HasActualData, string.Empty,  string.Empty, vdsData.ClientUId, vdsData.DCID);
                    PredictionDTO predictionDTO = new PredictionDTO
                    {
                        _id = Guid.NewGuid().ToString(),
                        UniqueId = Guid.NewGuid().ToString(),
                        //ActualData = Jdata[CONSTANTS.ActualData].ToString(),
                        CorrelationId = vdsData.CorrelationId,
                        Frequency = vdsData.Frequency,
                        PredictedData = null,
                        Status = CONSTANTS.I,
                        ErrorMessage = null,
                        Progress = null,
                        CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                        ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                        CreatedByUser = CONSTANTS.System,
                        ModifiedByUser = CONSTANTS.System
                    };
                    JArray Jdata2 = JArray.Parse(Jdata[CONSTANTS.ActualData].ToString());
                    string jsonString = JsonConvert.SerializeObject(Jdata2);
                    string data = jsonString.Replace("null", @"""""");
                    // db data encrypt
                    if (DBEncryptionRequired)
                    {
                        predictionDTO.ActualData = _encryptionDecryption.Encrypt(data);
                    }
                    else
                    {
                        predictionDTO.ActualData = data;
                    }
                    _deployedModelService.SavePrediction(predictionDTO);
                    DeployModelsDto mdl = _deployedModelService.GetDeployModelDetails(vdsData.CorrelationId);

                    IngrainRequestQueue ingrainRequest = new IngrainRequestQueue
                    {
                        _id = Guid.NewGuid().ToString(),
                        AppID = mdl.CorrelationId,
                        CorrelationId = vdsData.CorrelationId,
                        RequestId = Guid.NewGuid().ToString(),
                        ProcessId = null,
                        Status = null,
                        ModelName = null,
                        RequestStatus = appSettings.Value.IsFlaskCall ? CONSTANTS.Occupied : CONSTANTS.New,
                        RetryCount = 0,
                        ProblemType = null,
                        Message = null,
                        UniId = predictionDTO.UniqueId,
                        Progress = null,
                        pageInfo = CONSTANTS.ForecastModel, // pageInfo 
                        ParamArgs = CONSTANTS.CurlyBraces,
                        Function = CONSTANTS.ForecastModel,
                        CreatedByUser = CONSTANTS.System,
                        CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                        ModifiedByUser = CONSTANTS.System,
                        ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat),
                        LastProcessedOn = null,
                        IsForAPI = appSettings.Value.IsFlaskCall ? true : false
                    };
                    _ingestedDataService.InsertRequests(ingrainRequest);
                    if (appSettings.Value.IsFlaskCall)
                    {
                        _iFlaskAPIService.CallPython(ingrainRequest.CorrelationId, ingrainRequest.UniId, ingrainRequest.pageInfo);
                    }
                    Thread.Sleep(2000);

                    bool isPrediction = true;
                    while (isPrediction)
                    {
                        var predictionData = _deployedModelService.GetPrediction(predictionDTO);
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), nameof(RefitModel), CONSTANTS.APIResult + predictionData.Status, string.Empty,string.Empty, vdsData.ClientUId, vdsData.DCID);
                        if (predictionData.PredictedData != null && predictionData.PredictedData.Contains(CONSTANTS.Havedatatill))
                        {
                            isPrediction = false;
                            instaPrediction.CorrelationId = vdsData.CorrelationId;
                            instaPrediction.InstaID = vdsData.InstaId;
                            instaPrediction.Message = predictionData.ErrorMessage;
                            instaPrediction.Status = CONSTANTS.E;
                            instaPrediction.PredictedData = null;
                        }
                        if (predictionData.Status == CONSTANTS.C)
                        {
                            isPrediction = false;
                            instaPrediction.CorrelationId = vdsData.CorrelationId;
                            instaPrediction.InstaID = vdsData.InstaId;
                            instaPrediction.Message = Resource.IngrainResx.RefitModelcompleted;
                            instaPrediction.Status = CONSTANTS.C;
                            instaPrediction.PredictedData = predictionData.PredictedData;
                        }
                        else if (predictionData.Status == CONSTANTS.E)
                        {
                            isPrediction = false;
                            instaPrediction.CorrelationId = vdsData.CorrelationId;
                            instaPrediction.InstaID = vdsData.InstaId;
                            instaPrediction.Message = predictionData.ErrorMessage;
                            instaPrediction.Status = CONSTANTS.E;
                            instaPrediction.PredictedData = null;
                        }
                        else
                        {
                            isPrediction = true;
                            Thread.Sleep(1000);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InstaModelService), nameof(RefitModel), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, vdsData.ClientUId, vdsData.DCID);
                instaPrediction.ErrorMessage = ex.Message + ex.StackTrace;
                instaPrediction.Status = CONSTANTS.E;
                instaPrediction.InstaID = vdsData.InstaId;
                instaPrediction.CorrelationId = vdsData.CorrelationId;
            }
             LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), nameof(RefitModel), CONSTANTS.END, string.IsNullOrEmpty(vdsData.CorrelationId) ? default(Guid) : new Guid(vdsData.CorrelationId), string.Empty,  string.Empty, vdsData.ClientUId, vdsData.DCID);
            return instaPrediction;
        }

        /// <summary>
        /// Gets the model status for instaML model
        /// </summary>
        /// <param name="instaId">The insta identifier</param>
        /// <param name="correlationId">The correlation identifier</param>
        /// <returns>Returns the result</returns>
        public InstaRegression RegressionModelStatus(VDSRegression vdsRegression)
        {
         LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), nameof(RegressionModelStatus), CONSTANTS.START, string.Empty, string.Empty, vdsRegression.ClientUID, vdsRegression.DCID);
            try
            {
                string modelStatus = string.Empty;
                foreach (var item in vdsRegression.ProblemTypeDetails)
                {
                    modelStatus = this.GetModelStatus(item.InstaID, item.CorrelationId);
                    instaMLResponse response = new instaMLResponse();
                    if (!string.IsNullOrEmpty(modelStatus))
                    {
                        if (modelStatus == CONSTANTS.Deployed)
                        {
                            response.Message = modelStatus;
                            response.Status = CONSTANTS.C;
                        }
                        else
                        {
                            response.Message = modelStatus;
                            response.ErrorMessage = CONSTANTS.ModelInprogress;
                            response.Status = CONSTANTS.E;
                        }
                    }
                    else
                    {
                        response.Status = CONSTANTS.E;
                        response.Message = Resource.IngrainResx.NorecordfoundforID;
                    }
                    response.CorrelationId = item.CorrelationId;
                    response.InstaID = item.InstaID;
                    regressionResponse.Add(response);
                }
                instaRegression.UseCaseID = vdsRegression.UseCaseID;
                instaRegression.instaMLResponse = regressionResponse;
            }
            catch (Exception ex)
            {
                instaMLResponse response = new instaMLResponse();
                response.ErrorMessage = ex.Message;
                response.Status = CONSTANTS.E;
                regressionResponse.Add(response);
                instaRegression.instaMLResponse = regressionResponse;
                 LOGGING.LogManager.Logger.LogErrorMessage(typeof(InstaModelService), nameof(RegressionModelStatus), ex.Message, ex, string.Empty,  string.Empty, vdsRegression.ClientUID, vdsRegression.DCID);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), nameof(RegressionModelStatus), CONSTANTS.END, string.Empty,  string.Empty, vdsRegression.ClientUID, vdsRegression.DCID);
            return instaRegression;
        }

        public InstaModel ModelStatus(string instaId, string correlationId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), nameof(ModelStatus), CONSTANTS.START, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                string modelStatus = this.GetModelStatus(instaId, correlationId);
                instaModel.CorrelationId = correlationId;
                instaModel.InstaID = instaId;
                if (!string.IsNullOrEmpty(modelStatus))
                {
                    if (modelStatus == CONSTANTS.Deployed)
                    {
                        instaModel.Message = modelStatus;
                        instaModel.Status = CONSTANTS.C;
                    }
                    else
                    {
                        instaModel.Message = modelStatus;
                        instaModel.ErrorMessage = CONSTANTS.ModelInprogress;
                        instaModel.Status = CONSTANTS.E;
                    }

                }
                else
                {
                    instaModel.Status = CONSTANTS.E;
                    instaModel.Message = Resource.IngrainResx.NorecordfoundforID;
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InstaModelService), nameof(ModelStatus), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                instaModel.ErrorMessage = ex.Message;
                instaModel.Status = CONSTANTS.E;
            }

            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), nameof(ModelStatus), CONSTANTS.END, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            return instaModel;
        }
        public InstaModel UpdateModel(string correlationId, string modelName, string modelDescription)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), nameof(UpdateModel), CONSTANTS.START, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            InstaModel instaModel = new InstaModel();
            try
            {
                var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIDeployedModels);
                var update = Builders<BsonDocument>.Update.Set(CONSTANTS.ModelName, modelName);
                var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
                var result = collection.UpdateOne(filter, update);
                var collection2 = _database.GetCollection<BsonDocument>(CONSTANTS.PSBusinessProblem);
                var update2 = Builders<BsonDocument>.Update.Set(CONSTANTS.BusinessProblems, modelDescription);
                var result2 = collection2.UpdateOne(filter, update2);
                if (result.ModifiedCount > 0)
                {
                    instaModel.CorrelationId = correlationId;
                    instaModel.Status = CONSTANTS.C;
                    instaModel.Message = CONSTANTS.ModelUpdationSuccess;
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InstaModelService), nameof(UpdateModel), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                instaModel.ErrorMessage = ex.Message;
                instaModel.Status = CONSTANTS.E;
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), nameof(UpdateModel), CONSTANTS.End, string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            return instaModel;
        }

        public string GetEnvironment()
        {
            return appSettings.Value.Environment;
        }


        public string VDSSecurityTokenForManagedInstance()
        {
            string Appvalue ="";
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.AppIntegration);
            if(appSettings.Value.Environment == "FDS")
            {
                Appvalue = CONSTANTS.VDS_AIOPS;
            }

            //if (appSettings.Value.IsSaaSPlatform == true)
            //{
                
            //} else
            //{
            //    Appvalue = CONSTANTS.VDS_AIOPS;
            //}
            var filter = Builders<BsonDocument>.Filter.Eq("ApplicationName", Appvalue);
            var projectionScenario = Builders<BsonDocument>.Projection.Include("Authentication").Include("TokenGenerationURL").Include("Credentials").Exclude("_id");
            var dbData = collection.Find(filter).Project<BsonDocument>(projectionScenario).FirstOrDefault();
            if (dbData != null)
            {
                if (dbData["Authentication"].AsString == "Azure" || dbData["Authentication"].AsString == "AzureAD")
                {
                      string tokenUrl = _encryptionDecryption.Decrypt(dbData["TokenGenerationURL"].AsString);
                     // string tokenUrl = appSettings.Value.token_Url_VDS;
                      dynamic credentials = JsonConvert.DeserializeObject<dynamic>(_encryptionDecryption.Decrypt(dbData["Credentials"].AsString));
                    using (var client = new HttpClient())
                    {

                        client.DefaultRequestHeaders.Accept.Clear();
                        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        var formContent = new FormUrlEncodedContent(new[]
                        {
                            new KeyValuePair<string, string>("grant_type", credentials.grant_type.ToString()),
                            new KeyValuePair<string, string>("client_id", credentials.client_id.ToString()),
                            new KeyValuePair<string, string>("client_secret", credentials.client_secret.ToString()),
                            new KeyValuePair<string, string>("resource", credentials.resource.ToString())

                            // new KeyValuePair<string, string>("grant_type", appSettings.Value.Grant_Type_VDS),
                            //new KeyValuePair<string, string>("client_id", appSettings.Value.clientId_VDS),
                            //new KeyValuePair<string, string>("client_secret", appSettings.Value.clientSecret_VDS),
                            //new KeyValuePair<string, string>("resource", appSettings.Value.resourceId_VDS)
                        });

                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                        var tokenResult = client.PostAsync(tokenUrl, formContent).Result;
                        Dictionary<string, string> tokenDictionary = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(tokenResult.Content.ReadAsStringAsync().Result);
                        return tokenDictionary[CONSTANTS.access_token].ToString();
                    }

                }
            }

            return null;


        }
        public string VDSSecurityTokenForPAD()
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), nameof(VDSSecurityTokenForPAD), CONSTANTS.START + appSettings.Value.authProvider.ToUpper(), string.Empty, string.Empty, string.Empty, string.Empty);
            dynamic token = string.Empty;
            if (appSettings.Value.authProvider.ToUpper() == "FORM")
            {
                var username = Convert.ToString(appSettings.Value.username);
                var password = Convert.ToString(appSettings.Value.password);
                var tokenendpointurl = Convert.ToString(appSettings.Value.tokenAPIUrl);
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add(CONSTANTS.username, username);
                    client.DefaultRequestHeaders.Add(CONSTANTS.password, password);

                    var tokenResponse = client.PostAsync(tokenendpointurl, null).Result;
                    var tokenResult = tokenResponse.Content.ReadAsStringAsync().Result;
                    Dictionary<string, string> tokenDictionary = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(tokenResult);
                    if (tokenDictionary != null)
                    {
                        if (tokenResponse.IsSuccessStatusCode)
                        {
                            token = tokenDictionary[CONSTANTS.access_token].ToString();
                        }
                        else
                        {
                            token = CONSTANTS.InvertedComma;
                        }
                    }
                    else
                    {
                        token = CONSTANTS.InvertedComma;
                    }
                }

            }
            else
            {
                var client = new RestClient(appSettings.Value.token_Url_VDS);
                var request = new RestRequest(Method.POST);
                request.AddHeader("cache-control", "no-cache");
                request.AddHeader("content-type", "application/x-www-form-urlencoded");
                request.AddParameter("application/x-www-form-urlencoded", "grant_type=" + appSettings.Value.Grant_Type_VDS +
                   "&client_id=" + appSettings.Value.clientId_VDS +
                   "&client_secret=" + appSettings.Value.clientSecret_VDS +
                   "&scope=" + appSettings.Value.scopeStatus_VDS +
                   "&resource=" + appSettings.Value.resourceId_VDS,
                   ParameterType.RequestBody);
                var requestBuilder = new StringBuilder();
                foreach (var param in request.Parameters)
                {
                    requestBuilder.AppendFormat("{0}: {1}\r\n", param.Name, param.Value);
                }
                requestBuilder.ToString();
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), nameof(VDSSecurityTokenForPAD), "VDS TOKEN PARAMS -- " + requestBuilder, string.Empty, string.Empty, string.Empty, string.Empty);
                IRestResponse response = client.Execute(request);
                string json = response.Content;
                // Retrieve and Return the Access Token                
                var tokenObj = JsonConvert.DeserializeObject(json) as dynamic;
                token = Convert.ToString(tokenObj.access_token);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), nameof(VDSSecurityTokenForPAD), "END -" + appSettings.Value.authProvider.ToUpper(), string.Empty, string.Empty, string.Empty, string.Empty);
            return token;
        }

        public string VDSSecurityTokenForPAM()
        {
            dynamic token = string.Empty;
            if (appSettings.Value.authProvider.ToUpper() == "FORM")
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.BaseAddress = new Uri(appSettings.Value.TokenURLVDS);
                    httpClient.DefaultRequestHeaders.Accept.Clear();
                    var bodyparams = new
                    {
                        username = appSettings.Value.PAMTokenUserName,
                        password = appSettings.Value.PAMTokenUserPWD
                    };
                    string json = JsonConvert.SerializeObject(bodyparams);
                    HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
                    System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                    var result = httpClient.PostAsync("", content).Result;

                    if (result.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        throw new Exception(string.Format("Unable to process your request for generating token. Please check your credentials and try again. Status Code: {0}", result.StatusCode));
                    }

                    var result1 = result.Content.ReadAsStringAsync().Result;
                    var tokenObj = JsonConvert.DeserializeObject(result1) as dynamic;

                    return Convert.ToString(tokenObj.token);

                }
            }

           

            //PAMData pAMData = new PAMData();
            //pAMData.username = Convert.ToString(appSettings.Value.UserNamePAM);
            //pAMData.password = Convert.ToString(appSettings.Value.PasswordPAM);
            //var tokenendpointurl = Convert.ToString(appSettings.Value.PAMTokenUrl);
            //var postData = Newtonsoft.Json.JsonConvert.SerializeObject(pAMData, Formatting.None, new JsonSerializerSettings()
            //{
            //    DateFormatHandling = DateFormatHandling.MicrosoftDateFormat,
            //    DateParseHandling = DateParseHandling.DateTimeOffset
            //});
            //var stringContent = new StringContent(postData, UnicodeEncoding.UTF8, CONSTANTS.APPLICATION_JSON);
            //using (var client = new HttpClient())
            //{
            //    var tokenResponse = client.PostAsync(tokenendpointurl, stringContent).Result;
            //    var tokenResult = tokenResponse.Content.ReadAsStringAsync().Result;
            //    Dictionary<string, string> tokenDictionary = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(tokenResult);
            //    if (tokenDictionary != null)
            //    {
            //        if (tokenResponse.IsSuccessStatusCode)
            //        {
            //            token = tokenDictionary[CONSTANTS.token].ToString();
            //        }
            //        else
            //        {
            //            token = CONSTANTS.InvertedComma;
            //        }
            //    }
            //    else
            //    {
            //        token = CONSTANTS.InvertedComma;
            //    }
            //}

        
            else
            {
                var client = new RestClient(appSettings.Value.token_Url_VDS);
                var request = new RestRequest(Method.POST);
                request.AddHeader("cache-control", "no-cache");
                request.AddHeader("content-type", "application/x-www-form-urlencoded");
                request.AddParameter("application/x-www-form-urlencoded", "grant_type=" + appSettings.Value.Grant_Type_VDS +
                   "&client_id=" + appSettings.Value.clientId_VDS +
                   "&client_secret=" + appSettings.Value.clientSecret_VDS +
                   "&scope=" + appSettings.Value.scopeStatus_VDS +
                   "&resource=" + appSettings.Value.resourceId_VDS,
                   ParameterType.RequestBody);

                var requestBuilder = new StringBuilder();
                foreach (var param in request.Parameters)
                {
                    requestBuilder.AppendFormat("{0}: {1}\r\n", param.Name, param.Value);
                }
                requestBuilder.ToString();
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), nameof(VDSSecurityTokenForPAD), "VDS PAM TOKEN PARAMS -- " + requestBuilder, string.Empty, string.Empty, string.Empty, string.Empty);
                IRestResponse response = client.Execute(request);
                string json = response.Content;
                // Retrieve and Return the Access Token                
                var tokenObj = JsonConvert.DeserializeObject(json) as dynamic;
                token = Convert.ToString(tokenObj.access_token);
            }

            return token;
        }
    
        public void InsertRequests(IngrainRequestQueue ingrainRequest)
        {
            var requestQueue = JsonConvert.SerializeObject(ingrainRequest);
            var insertRequestQueue = BsonSerializer.Deserialize<BsonDocument>(requestQueue);
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIIngrainRequests);
            collection.InsertOne(insertRequestQueue);
        }

        public string GetInstaMLData(string usecaseID)
        {
            string data = string.Empty;
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.PSBusinessProblem);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.UseCaseID, usecaseID);
            var projection = Builders<BsonDocument>.Projection.Include("Data").Exclude(CONSTANTS.Id);
            var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                data = result[0]["Data"].ToString();
            }
            return data;
        }
    }
}
