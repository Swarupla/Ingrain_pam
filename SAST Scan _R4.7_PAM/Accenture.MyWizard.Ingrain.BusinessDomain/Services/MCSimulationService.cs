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
Module Name     :   MCSimulationService
Project         :   Accenture.MyWizard.Ingrain.BusinessDomain.Services
Organisation    :   Accenture Technologies Ltd.
Created By      :   RaviA
Created Date    :   15-JUN-2020
Revision History :
Revision No             Initial             Modified Date           Description
1.0                     An                  15-JUN-2020             
\********************************************************************************************************/
#endregion

using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
using Accenture.MyWizard.Ingrain.DataAccess;
using Accenture.MyWizard.Ingrain.DataModels.MonteCarlo;
using Accenture.MyWizard.Shared.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
using OfficeOpenXml.Table;
using System.IO;
using System.Data;
using OfficeOpenXml;
using System.Linq;
using System.Text.RegularExpressions;
using System.Net.Http;
using RestSharp;
using System.Net.Http.Headers;
using System.Net;
using System.Globalization;
using MongoDB.Bson.Serialization.Attributes;
using Accenture.MyWizard.Cryptography;
using Accenture.MyWizard.Cryptography.EncryptionProviders;
using System.Runtime.Intrinsics.X86;
using System.Threading.Tasks;
using Accenture.MyWizard.Ingrain.DataModels.Models;
using System.Threading;
using CryptographyHelper = Accenture.MyWizard.Cryptography;

namespace Accenture.MyWizard.Ingrain.BusinessDomain.Services
{
    public class MCSimulationService : IMCSimulationService
    {
        #region Members
        private MongoClient _mongoClient;
        private IMongoDatabase _database;
        MonteCarloConnection databaseProvider;
        private readonly IOptions<IngrainAppSettings> appSettings;
        TemplateData templateData = null;
        TemplateInfo templateInfo = null;
        GetInputInfo getInputInfo = null;
        GetOutputInfo getOutputInfo = null;
        SimulationPrediction simulationPrediction = null;
        WeeklyPrediction weeklyPrediction = null;
        GenericFile genericFile = null;
        whatIfPrediction whatIfPrediction = null;
        VDSUseCaseModels VDSUseCaseModels = null;
        DeletedUseCaseDetails UseCaseDetails = null;
        JObject Features = null;
        JArray ReleaseState = null;
        JArray ReleaseName = null;
        JArray StartDate = null;
        JArray EndDate = null;
        List<DateTime> endList = null;
        List<DateTime> startList = null;
        List<string> MainColumns = null;
        List<string> falseInputColumns = null;
        List<string> falsePhaseRows = null;
        DataTable dataTable = null;
        DataTable dataTable2 = null;
        string[] SubColumns = null;
        int TotalColumns = 0;
        int TotalRows = 0;
        string filePath = string.Empty;
        static CryptographyHelper.Utility.CryptographyUtility CryptographyUtility = null;
        #endregion

        #region Constructor
        public MCSimulationService(MonteCarloConnection db, IOptions<IngrainAppSettings> settings, IServiceProvider serviceProvider)
        {
            Features = new JObject();
            ReleaseState = new JArray();
            ReleaseName = new JArray();
            StartDate = new JArray();
            EndDate = new JArray();
            startList = new List<DateTime>();
            endList = new List<DateTime>();
            MainColumns = new List<string>();
            falseInputColumns = new List<string>();
            falsePhaseRows = new List<string>();
            databaseProvider = db;
            appSettings = settings;
            dataTable = new DataTable();
            dataTable2 = new DataTable();
            templateData = new TemplateData();
            templateInfo = new TemplateInfo();
            simulationPrediction = new SimulationPrediction();
            VDSUseCaseModels = new VDSUseCaseModels();
            UseCaseDetails = new DeletedUseCaseDetails();
            genericFile = new GenericFile();
            whatIfPrediction = new whatIfPrediction();
            weeklyPrediction = new WeeklyPrediction();
            _mongoClient = databaseProvider.GetDatabaseConnection();
            var dataBaseName = MongoUrl.Create(appSettings.Value.MonteCarloConnection).DatabaseName;
            _database = _mongoClient.GetDatabase(dataBaseName);
            CryptographyUtility = new CryptographyHelper.Utility.CryptographyUtility();
        }
        #endregion

        #region Methods
        private string AssignANDValidate(IFormFileCollection fileCollection, IFormCollection formCollection, bool RRPFlag)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(AssignANDValidate), "START", string.Empty, string.Empty, string.Empty, string.Empty);
            string validationMessage = string.Empty;
            if (formCollection.Keys.Count > 0)
            {
                templateData.ClientUID = Convert.ToString(formCollection[CONSTANTS.ClientUID]).Trim();
                templateData.DeliveryConstructUID = Convert.ToString(formCollection[CONSTANTS.DeliveryConstructUID]).Trim();

                templateData.TemplateID = Guid.NewGuid().ToString();
                //templateData.Version = CONSTANTS.Version;
                templateData.ProblemType = Convert.ToString(formCollection[CONSTANTS.ProblemType]).Trim();
                if (templateData.ProblemType.Trim() == CONSTANTS.ADSP || templateData.ProblemType.Trim() == CONSTANTS.RRP)
                {
                    templateData.MainColumns = CONSTANTS.MainColumns.Split(CONSTANTS.comma);
                    templateData.InputColumns = JsonConvert.DeserializeObject<string[]>(Convert.ToString(formCollection[CONSTANTS.InputColumns]));
                    templateData.TargetColumn = Convert.ToString(formCollection[CONSTANTS.TargetColumn]).Trim();
                }
                templateData.DeliveryTypeID = Convert.ToString(formCollection[CONSTANTS.DeliveryTypeID]).Trim();
                templateData.DeliveryTypeName = Convert.ToString(formCollection[CONSTANTS.DeliveryTypeName]).Trim();
                templateData.UseCaseName = Convert.ToString(formCollection[CONSTANTS.UseCaseName]).Trim();
                templateData.UseCaseDescription = Convert.ToString(formCollection[CONSTANTS.UseCaseDescription]).Trim();
                templateData.CreatedByUser = Convert.ToString(formCollection[CONSTANTS.CreatedByUser]).Trim();
                templateData.ModifiedByUser = Convert.ToString(formCollection[CONSTANTS.CreatedByUser]).Trim();
                templateData.CreatedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                templateData.ModifiedOn = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
            }
            if (templateData.ProblemType.Trim() == CONSTANTS.ADSP || templateData.ProblemType.Trim() == CONSTANTS.RRP)
            {
                //check if base version needs to be created
                templateInfo.InsertBase = Convert.ToBoolean(formCollection["InsertBase"]);
                if (templateData.ProblemType.Trim() == CONSTANTS.ADSP || templateData.ProblemType.Trim() == CONSTANTS.RRP)
                    templateData.UseCaseID = CONSTANTS.ADSPID;
                if (string.IsNullOrEmpty(templateData.ClientUID)
                                    || string.IsNullOrEmpty(templateData.DeliveryConstructUID)
                                    || (templateData.ProblemType.Trim() == CONSTANTS.ADSP && templateData.InputColumns.Length < 1)
                                    || string.IsNullOrEmpty(templateData.TargetColumn)
                                    || string.IsNullOrEmpty(templateData.DeliveryTypeID)
                                    || string.IsNullOrEmpty(templateData.DeliveryTypeName)
                                    || string.IsNullOrEmpty(templateData.UseCaseDescription)
                                    || string.IsNullOrEmpty(templateData.UseCaseName))
                {
                    validationMessage = Resource.IngrainResx.InputEmpty;
                    templateInfo.IsUploaded = false;
                    templateInfo.ErrorMessage = Resource.IngrainResx.InputEmpty;
                    templateInfo.Status = CONSTANTS.E;
                }
                if (templateData.ClientUID.Contains(CONSTANTS.undefined)
                                    || templateData.DeliveryConstructUID.Contains(CONSTANTS.undefined)
                                    || templateData.TargetColumn.Contains(CONSTANTS.undefined)
                                    || templateData.DeliveryTypeName.Contains(CONSTANTS.undefined)
                                    || templateData.UseCaseDescription.Contains(CONSTANTS.undefined)
                                    || templateData.UseCaseName.Contains(CONSTANTS.undefined))
                {
                    validationMessage = CONSTANTS.InutFieldsUndefined;
                    templateInfo.IsUploaded = false;
                    templateInfo.ErrorMessage = CONSTANTS.InutFieldsUndefined;
                    templateInfo.Status = CONSTANTS.E;
                }
            }
            else
            {
                templateData.UseCaseID = Guid.NewGuid().ToString();
                if (string.IsNullOrEmpty(templateData.ClientUID)
                    || string.IsNullOrEmpty(templateData.DeliveryConstructUID)
                    || string.IsNullOrEmpty(templateData.DeliveryTypeID)
                    || string.IsNullOrEmpty(templateData.DeliveryTypeName))
                {
                    validationMessage = Resource.IngrainResx.InputEmpty;
                    templateInfo.IsUploaded = false;
                    templateInfo.ErrorMessage = Resource.IngrainResx.InputEmpty;
                    templateInfo.Status = CONSTANTS.E;
                }
            }
            if (RRPFlag != true)
            {
                if (fileCollection.Count != 0)
                {
                    var postedFile = fileCollection[0];
                    if (postedFile.Length <= 0)
                    {
                        validationMessage = CONSTANTS.FileEmpty;
                        templateInfo.IsUploaded = false;
                        templateInfo.ErrorMessage = CONSTANTS.FileEmpty;
                        templateInfo.Status = CONSTANTS.E;
                    }
                }
                else
                {
                    validationMessage = CONSTANTS.FileNotExist;
                    templateInfo.IsUploaded = false;
                    templateInfo.ErrorMessage = CONSTANTS.FileNotExist;
                    templateInfo.Status = CONSTANTS.E;
                }
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(AssignANDValidate), "end" + validationMessage, string.Empty, string.Empty, string.Empty, string.Empty);
            return validationMessage;
        }
        public TemplateInfo LoadData(IFormFileCollection fileCollection, IFormCollection formCollection, out string message)
        {
            bool inserted = false;
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(LoadData), CONSTANTS.START, string.Empty, string.Empty, string.Empty, string.Empty);
            message = this.AssignANDValidate(fileCollection, formCollection, false);
            try
            {
                if (!string.IsNullOrEmpty(message))
                {
                    return templateInfo;
                }
                if (templateData.ProblemType.Trim() == CONSTANTS.ADSP)
                {
                    SubColumns = templateData.InputColumns;
                    templateInfo.InputColumns = templateData.InputColumns;
                }
                IFormFile file = fileCollection[0];
                filePath = appSettings.Value.UploadFilePath + "\\" + templateData.TemplateID + "_" + file.FileName;
                var Extension = Path.GetExtension(filePath);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    file.CopyTo(fileStream);
                }
                if (Extension != ".xlsx" & Extension != ".csv")
                {
                    message = CONSTANTS.FileNotSupport;
                    templateInfo.IsUploaded = false;
                    templateInfo.IsSimulationExist = false;
                    templateInfo.ErrorMessage = CONSTANTS.FileNotSupport;
                    templateInfo.Status = CONSTANTS.E;
                    return templateInfo; ;
                }
                if (Extension == ".xlsx")
                    dataTable = ConvertExceltoDataTable(filePath, out message);
                if (Extension == ".csv")
                    dataTable = ConvertCSVtoDataTable(filePath, out message);
                File.Delete(filePath);
                if (!string.IsNullOrEmpty(message))
                {
                    templateInfo.IsUploaded = false;
                    templateInfo.IsSimulationExist = false;
                    templateInfo.ErrorMessage = message;
                    templateInfo.Status = CONSTANTS.E;
                    return templateInfo;
                }
                if (dataTable != null && dataTable.Rows.Count > 0)
                {
                    bool checkPastRelease = createNewFeatures(dataTable);
                    bool isNumeric = CreateFeatutes(dataTable);
                    dataTable.Dispose();
                    if (!isNumeric)
                    {
                        templateInfo.IsUploaded = false;
                        templateInfo.IsSimulationExist = false;
                        templateInfo.Status = CONSTANTS.E;
                        return templateInfo;
                    }
                    else
                    {
                        templateData.UniqueIdentifierName = CONSTANTS.ReleaseName;
                        inserted = insertFeatutes(false);
                    }
                }
                if (inserted)
                {
                    templateInfo.TemplateId = templateData.TemplateID;
                    templateInfo.Version = templateData.Version;
                    templateInfo.User = templateData.CreatedByUser;
                    templateInfo.ProblemType = templateData.ProblemType;
                    if (appSettings.Value.DBEncryption)
                        templateInfo.Features = JObject.Parse(appSettings.Value.IsAESKeyVault ? CryptographyUtility.Decrypt(templateData.Features) : AesProvider.Decrypt(templateData.Features, appSettings.Value.aesKey, appSettings.Value.aesVector));
                    else
                        templateInfo.Features = templateData.Features;
                    templateInfo.DeliveryTypeID = templateData.DeliveryTypeID;
                    templateInfo.DeliveryTypeName = templateData.DeliveryTypeName;
                    templateInfo.UseCaseName = templateData.UseCaseName;
                    templateInfo.UseCaseDescription = templateData.UseCaseDescription;
                    templateInfo.UniqueIdentifierName = CONSTANTS.ReleaseName;
                    templateInfo.IsSimulationExist = false;
                    templateInfo.IsUploaded = true;
                    templateInfo.Status = CONSTANTS.C;
                    templateInfo.MainSelection = templateData.MainSelection;
                    templateInfo.InputSelection = templateData.InputSelection;
                    genericFile.UseCaseID = templateData.UseCaseID;
                    templateInfo.isDBEncryption = templateData.isDBEncryption;
                    templateInfo.SelectedCurrentRelease = templateData.SelectedCurrentRelease;
                    GetVersionsList(templateData.ClientUID, templateData.DeliveryConstructUID, templateData.UseCaseID);
                }
                else
                {
                    templateInfo.ProblemType = templateData.ProblemType;
                    templateInfo.TemplateId = templateData.TemplateID;
                    templateInfo.Version = templateData.Version;
                    templateInfo.User = templateData.CreatedByUser;
                    templateInfo.IsSimulationExist = false;
                    templateInfo.Features = null;
                    templateInfo.IsUploaded = false;
                }
            }
            catch (Exception ex)
            {
                File.Delete(filePath);
                templateInfo.ErrorMessage = ex.Message + "-STACKTRACE--" + ex.StackTrace;
                templateInfo.IsException = true;
                dataTable.Dispose();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(MCSimulationService), nameof(LoadData), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(LoadData), CONSTANTS.END, string.IsNullOrEmpty(templateData.TemplateID) ? default(Guid) : new Guid(templateData.TemplateID) , string.Empty, string.Empty, string.Empty, string.Empty);
            return templateInfo;
        }
        public GenericFile GenericLoadData(IFormFileCollection fileCollection, IFormCollection formCollection, out string message)
        {
            bool inserted = false;
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(GenericLoadData), CONSTANTS.START, string.Empty, string.Empty, string.Empty, string.Empty);
            message = this.AssignANDValidate(fileCollection, formCollection, false);
            try
            {
                if (!string.IsNullOrEmpty(message))
                {
                    genericFile.IsUploaded = templateInfo.IsUploaded;
                    genericFile.ErrorMessage = templateInfo.ErrorMessage;
                    genericFile.Status = templateInfo.Status;
                    return genericFile;
                }

                IFormFile file = fileCollection[0];
                filePath = appSettings.Value.UploadFilePath + "\\" + templateData.TemplateID + "_" + file.FileName;
                var Extension = Path.GetExtension(filePath);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    file.CopyTo(fileStream);
                }
                if (Extension != ".xlsx" & Extension != ".csv")
                {
                    message = CONSTANTS.FileNotSupport;
                    genericFile.IsUploaded = templateInfo.IsUploaded;
                    genericFile.ErrorMessage = CONSTANTS.FileNotSupport; //templateInfo.ErrorMessage;
                    genericFile.Status = CONSTANTS.E;//templateInfo.Status;
                    return genericFile;
                }
                if (Extension == ".xlsx")
                    dataTable = ConvertExceltoDataTable(filePath, out message);
                if (Extension == ".csv")
                    dataTable = ConvertCSVtoDataTable(filePath, out message);
                File.Delete(filePath);
                if (!string.IsNullOrEmpty(message))
                {
                    genericFile.IsUploaded = templateInfo.IsUploaded;
                    genericFile.ErrorMessage = templateInfo.ErrorMessage;
                    genericFile.Status = templateInfo.Status;
                    return genericFile;
                }
                if (dataTable != null)
                {
                    string testNumber = GenericFeatures(dataTable);
                    if (testNumber != string.Empty)
                    {
                        genericFile.IsUploaded = false;
                        genericFile.ErrorMessage = testNumber;
                        genericFile.Status = CONSTANTS.E;
                        return genericFile;
                    }
                    if (templateData.TargetColumnList.Count == 0)
                    {
                        genericFile.IsUploaded = false;
                        genericFile.ErrorMessage = CONSTANTS.IncompatibleFile;
                        genericFile.Status = CONSTANTS.E;
                        return genericFile;
                    }
                    if (templateData.TargetColumnList.Count < 3)
                    {
                        genericFile.IsUploaded = false;
                        genericFile.ErrorMessage = CONSTANTS.NumericDatatypeValidation;
                        genericFile.Status = CONSTANTS.E;
                        return genericFile;
                    }
                    //new validation msg
                    if (templateData.TargetColumnList.Count < 3)
                    {
                        genericFile.IsUploaded = false;
                        genericFile.ErrorMessage = CONSTANTS.NumericDatatypeValidation;
                        genericFile.Status = CONSTANTS.E;
                        return genericFile;
                    }
                    dataTable.Dispose();
                    inserted = insertFeatutes(false);
                }
                if (inserted)
                {
                    genericFile.TemplateId = templateData.TemplateID;
                    genericFile.User = templateData.CreatedByUser;
                    genericFile.ProblemType = templateData.ProblemType;
                    genericFile.DeliveryTypeID = templateData.DeliveryTypeID;
                    genericFile.DeliveryTypeName = templateData.DeliveryTypeName;
                    genericFile.DeliveryConstructUID = templateData.DeliveryConstructUID;
                    genericFile.ClientUID = templateData.ClientUID;
                    genericFile.TargetColumnList = templateData.TargetColumnList;
                    genericFile.UniqueColumnList = templateData.UniqueColumnList;
                    genericFile.IsUploaded = true;
                    genericFile.UseCaseID = templateData.UseCaseID;
                    genericFile.UseCaseName = templateData.UseCaseName;
                    genericFile.UseCaseDescription = templateData.UseCaseDescription;
                    genericFile.Status = CONSTANTS.C;
                    GetVersionsList(templateData.ClientUID, templateData.DeliveryConstructUID, templateData.UseCaseID);
                }
                else
                {
                    genericFile.ProblemType = templateData.ProblemType;
                    genericFile.TemplateId = templateData.TemplateID;
                    genericFile.User = templateData.CreatedByUser;
                    genericFile.IsUploaded = false;
                    genericFile.Status = CONSTANTS.E;
                }
            }
            catch (Exception ex)
            {
                File.Delete(filePath);
                genericFile.ErrorMessage = ex.Message;
                genericFile.IsException = true;
                dataTable.Dispose();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(MCSimulationService), nameof(GenericLoadData), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(GenericLoadData), CONSTANTS.END, string.Empty, string.Empty, string.Empty, string.Empty);
            return genericFile;
        }
        public TemplateInfo RRPLoadData(IFormCollection formCollection, out string message)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(RRPLoadData), CONSTANTS.START, string.Empty, string.Empty, string.Empty, string.Empty);
            bool baseExist = CheckBaseVersion(CONSTANTS.Base_Version, Convert.ToString(formCollection[CONSTANTS.ClientUID]).Trim(), Convert.ToString(formCollection[CONSTANTS.DeliveryConstructUID]).Trim());
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(CheckBaseVersion), "baseExist : "+ baseExist.ToString(), string.Empty, string.Empty, string.Empty, string.Empty);
            if (baseExist)
            {
                message = "Base_Version exists";
                templateInfo.Message = message;
                var templateDataCollection = _database.GetCollection<BsonDocument>(CONSTANTS.TemplateData);
                var builder = Builders<BsonDocument>.Filter;
                var filter = builder.Eq(CONSTANTS.ClientUID, Convert.ToString(formCollection[CONSTANTS.ClientUID]).Trim()) & builder.Eq(CONSTANTS.DeliveryConstructUID, Convert.ToString(formCollection[CONSTANTS.DeliveryConstructUID]).Trim()) & builder.Eq(CONSTANTS.VersionAttribute, CONSTANTS.Base_Version);
                var templateProjection = Builders<BsonDocument>.Projection.Include(CONSTANTS.Status).Include(CONSTANTS.Progress).Include(CONSTANTS.Message).Include(CONSTANTS.Features).Include(CONSTANTS.InputColumns).Include(CONSTANTS.InputSelection).Include(CONSTANTS.SelectedCurrentRelease).Include(CONSTANTS.TemplateID).Exclude(CONSTANTS.Id);
                var templateResult = templateDataCollection.Find(filter).Project<BsonDocument>(templateProjection).ToList();
                if (templateResult.Count > 0)
                {
                    templateInfo.TemplateId = templateResult[0][CONSTANTS.TemplateID].ToString();
                    templateData.Features = templateResult[0][CONSTANTS.Features];
                    if (templateResult[0][CONSTANTS.Features].ToString() != CONSTANTS.BsonNull)
                    {
                        templateInfo.InputColumns = templateResult[0][CONSTANTS.InputColumns].ToString().Replace("[", "").Replace("]", "").Split(CONSTANTS.comma);
                        templateInfo.InputSelection = JObject.Parse(templateResult[0][CONSTANTS.InputSelection].ToString());
                        if (appSettings.Value.DBEncryption)
                            templateInfo.Features = JObject.Parse(appSettings.Value.IsAESKeyVault ? CryptographyUtility.Decrypt(templateResult[0][CONSTANTS.Features].ToString()): AesProvider.Decrypt(templateResult[0][CONSTANTS.Features].ToString(), appSettings.Value.aesKey, appSettings.Value.aesVector));
                        else
                            templateInfo.Features = JsonConvert.DeserializeObject<JObject>(templateResult[0][CONSTANTS.Features].ToString());
                    }
                    templateInfo.Status = templateResult[0][CONSTANTS.Status].ToString();
                    templateInfo.Progress = templateResult[0][CONSTANTS.Progress].ToString();
                    templateInfo.Message = templateResult[0][CONSTANTS.Message].ToString();
                }
            }
            else
            {
                bool inserted = false;

                //except features other feilds from api insertion
                message = this.AssignANDValidate(null, formCollection, true);
                try
                {
                    if (!string.IsNullOrEmpty(message))
                    {
                        return templateInfo;
                    }
                    if (templateData.ProblemType.Trim() == CONSTANTS.ADSP)
                    {
                        SubColumns = templateData.InputColumns;
                        templateInfo.InputColumns = templateData.InputColumns;
                    }
                    inserted = insertFeatutes(true);
                    if (inserted)
                    {
                        templateInfo.TemplateId = templateData.TemplateID;
                        templateInfo.Version = templateData.Version;
                        templateInfo.User = templateData.CreatedByUser;
                        templateInfo.ProblemType = templateData.ProblemType;
                        templateInfo.DeliveryTypeID = templateData.DeliveryTypeID;
                        templateInfo.DeliveryTypeName = templateData.DeliveryTypeName;
                        templateInfo.UseCaseName = templateData.UseCaseName;
                        templateInfo.UseCaseDescription = templateData.UseCaseDescription;
                        templateInfo.UniqueIdentifierName = CONSTANTS.ReleaseName;
                        templateInfo.IsSimulationExist = false;
                        genericFile.UseCaseID = templateData.UseCaseID;
                        templateInfo.isDBEncryption = templateData.isDBEncryption;
                        templateInfo.VersionMessage = templateData.VersionMessage;
                        templateInfo.MainSelection = templateData.MainSelection;
                        GetVersionsList(templateData.ClientUID, templateData.DeliveryConstructUID, templateData.UseCaseID);

                        HttpResponseMessage httpResponse = null;
                        string pythonResult = string.Empty;
                        //pythonCall
                        httpResponse = GetRRPResult(templateData.TemplateID, templateData.Version, templateData.ClientUID, templateData.DeliveryConstructUID);
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(RRPLoadData), "httpResponse.StatusCode :" + httpResponse.StatusCode.ToString(), string.Empty, string.Empty, string.Empty, string.Empty);
                        if (httpResponse.StatusCode.ToString() != "InternalServerError" && httpResponse.StatusCode.ToString() != "BadGateway" && httpResponse.StatusCode != HttpStatusCode.Unauthorized && httpResponse.StatusCode.ToString() != "401")
                        {
                            if (httpResponse.StatusCode != HttpStatusCode.InternalServerError)
                            {
                                var response = JObject.Parse(httpResponse.Content.ReadAsStringAsync().Result);
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(RRPLoadData), "response :" + response, string.Empty, string.Empty, string.Empty, string.Empty);
                                if (httpResponse == null)
                                {
                                    templateInfo.ErrorMessage = "Token Generation Error";
                                    templateInfo.Status = CONSTANTS.E;
                                    return templateInfo;
                                }

                                if (response != null && response.ContainsKey(CONSTANTS.Error))
                                {
                                    templateInfo.ErrorMessage = "Authorization Fail or Python Error " + httpResponse.StatusCode;
                                    templateInfo.Status = CONSTANTS.E;
                                    return templateInfo;
                                }

                                if (httpResponse.StatusCode == HttpStatusCode.InternalServerError || httpResponse.StatusCode == HttpStatusCode.Unauthorized)
                                {
                                    templateInfo.ErrorMessage = "Authorization Fail or Python Error " + httpResponse.StatusCode;
                                    templateInfo.Status = CONSTANTS.E;
                                    return templateInfo;
                                }

                                if (httpResponse.StatusCode == HttpStatusCode.OK)
                                {
                                    JObject pyResponse = JsonConvert.DeserializeObject<JObject>(httpResponse.Content.ReadAsStringAsync().Result);
                                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(RRPLoadData), "pyResponse :" + pyResponse.ToString(), string.Empty, string.Empty, string.Empty, string.Empty);
                                    var templateDataCollection = _database.GetCollection<BsonDocument>(CONSTANTS.TemplateData);
                                    var builder = Builders<BsonDocument>.Filter;
                                    var filter = builder.Eq(CONSTANTS.ClientUID, templateData.ClientUID) & builder.Eq(CONSTANTS.DeliveryConstructUID, templateData.DeliveryConstructUID) & builder.Eq(CONSTANTS.TemplateID, templateData.TemplateID);
                                    var templateProjection = Builders<BsonDocument>.Projection.Include(CONSTANTS.Status).Include(CONSTANTS.Progress).Include(CONSTANTS.Message).Include(CONSTANTS.Features).Include(CONSTANTS.InputColumns).Include(CONSTANTS.InputSelection).Include(CONSTANTS.ReleaseState).Include(CONSTANTS.ReleaseName).Include(CONSTANTS.ReleaseStartDate).Include(CONSTANTS.SelectedCurrentRelease).Exclude(CONSTANTS.Id);
                                    var templateResult = templateDataCollection.Find(filter).Project<BsonDocument>(templateProjection).ToList();
                                    if (templateResult.Count > 0)
                                    {
                                        templateInfo.Status = templateResult[0][CONSTANTS.Status].ToString();
                                        templateInfo.Progress = templateResult[0][CONSTANTS.Progress].ToString();
                                        templateInfo.Message = templateResult[0][CONSTANTS.Message].ToString();
                                        if (templateInfo.Status == CONSTANTS.C)
                                        {
                                            templateData.Features = templateResult[0][CONSTANTS.Features];
                                            templateInfo.InputColumns = templateResult[0][CONSTANTS.InputColumns].ToString().Replace("[", "").Replace("]", "").Split(CONSTANTS.comma);
                                            templateInfo.InputSelection = JObject.Parse(templateResult[0][CONSTANTS.InputSelection].ToString());
                                            if (appSettings.Value.DBEncryption)
                                                templateInfo.Features = JObject.Parse(appSettings.Value.IsAESKeyVault ? CryptographyUtility.Decrypt(templateResult[0][CONSTANTS.Features].ToString()): AesProvider.Decrypt(templateResult[0][CONSTANTS.Features].ToString(), appSettings.Value.aesKey, appSettings.Value.aesVector));
                                            else
                                                templateInfo.Features = JsonConvert.DeserializeObject<JObject>(templateResult[0][CONSTANTS.Features].ToString());

                                            templateInfo.IsUploaded = true;

                                        }
                                        else if (templateInfo.Status == CONSTANTS.E)
                                        {
                                            templateInfo.Status = CONSTANTS.E;
                                            templateInfo.ErrorMessage = CONSTANTS.PythonError;
                                        }
                                        else if (templateInfo.Status.ToString() == CONSTANTS.BsonNull || templateResult[0][CONSTANTS.Features].ToString() == CONSTANTS.BsonNull)
                                            templateInfo.Message = "Features not updated still in Record by python";

                                    }
                                }
                                else
                                {
                                    templateInfo.ErrorMessage = httpResponse.StatusCode.ToString();
                                    templateInfo.Status = CONSTANTS.E;
                                }
                            }
                            else
                            {
                                templateInfo.ErrorMessage = "Python Error: " + httpResponse.StatusCode;
                                templateInfo.Status = CONSTANTS.E;
                                return templateInfo;
                            }
                        }
                        else
                        {
                            templateInfo.ErrorMessage = "Python Error " + httpResponse.StatusCode;
                            templateInfo.Status = CONSTANTS.E;
                            return templateInfo;
                        }
                    }
                    else
                    {
                        templateInfo.ProblemType = templateData.ProblemType;
                        templateInfo.TemplateId = templateData.TemplateID;
                        templateInfo.Version = templateData.Version;
                        templateInfo.User = templateData.CreatedByUser;
                        templateInfo.IsSimulationExist = false;
                        templateInfo.Features = null;
                        templateInfo.IsUploaded = false;
                    }
                }
                catch (Exception ex)
                {
                    templateInfo.ErrorMessage = ex.Message + "-STACKTRACE--" + ex.StackTrace;
                    templateInfo.IsException = true;
                    LOGGING.LogManager.Logger.LogErrorMessage(typeof(MCSimulationService), nameof(RRPLoadData), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                }
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(RRPLoadData), CONSTANTS.END, string.Empty, string.Empty, string.Empty, string.Empty);
            return templateInfo;
        }
        private DataTable ConvertExceltoDataTable(string path, out string message)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(ConvertExceltoDataTable), "Start", string.Empty, string.Empty, string.Empty, string.Empty);
            message = string.Empty;
            OfficeOpenXml.ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var excelPack = new ExcelPackage())
            {
                //Load excel stream
                OfficeOpenXml.ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var stream = File.OpenRead(path))
                {
                    excelPack.Load(stream);
                }
                File.Delete(path);
                //Lets Deal with first worksheet.(You may iterate here if dealing with multiple sheets)
                var ws = excelPack.Workbook.Worksheets[0];
                //check for empty excel/csv
                if (ws.Dimension != null)
                {
                    TotalColumns = ws.Dimension.End.Column;
                    TotalRows = ws.Dimension.End.Row;
                }
                else
                {
                    templateInfo.IsUploaded = false;
                    templateInfo.ErrorMessage = CONSTANTS.FileEmpty;
                    templateInfo.Status = CONSTANTS.E;
                    return dataTable;
                }

                //check row and col count
                bool checkCount = IsRowColCount(TotalColumns, TotalRows, templateData.ProblemType, out message);
                if (!checkCount)
                {
                    templateInfo.ErrorMessage = message;
                    templateInfo.IsUploaded = false;
                    templateInfo.Status = CONSTANTS.E;
                    return dataTable;
                }
                for (int i = 1; i <= ws.Dimension.End.Column; i++)
                {
                    string column = Convert.ToString(ws.Cells[1, i].Value);
                    if (!string.IsNullOrEmpty(column) && column != CONSTANTS.Null)
                        MainColumns.Add(column);
                    dataTable.Columns.Add(column);
                }
                if (templateData.ProblemType == CONSTANTS.ADSP)
                {
                    if (MainColumns.Count != 4)
                    {
                        message = CONSTANTS.MaxMainColumns;
                        return dataTable;
                    }

                }
                for (var rownumber = 2; rownumber <= ws.Dimension.End.Row; rownumber++)
                {
                    var row = ws.Cells[rownumber, 1, rownumber, ws.Dimension.End.Column];
                    DataRow newRow = dataTable.NewRow();
                    foreach (var cell in row)
                    {
                        newRow[cell.Start.Column - 1] = cell.Text;
                    }
                    dataTable.Rows.Add(newRow);
                }
                excelPack.Dispose();
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(ConvertExceltoDataTable), "end", string.Empty, string.Empty, string.Empty, string.Empty);
            return dataTable;
        }
        private DataTable ConvertCSVtoDataTable(string strFilePath, out string message)
        {
            message = string.Empty;
            using (StreamReader sr = new StreamReader(strFilePath))
            {
                string[] headers = sr.ReadLine().Split(CONSTANTS.comma_);
                foreach (string header in headers)
                {
                    if (!string.IsNullOrEmpty(header) && header != CONSTANTS.Null)
                    {
                        MainColumns.Add(header);
                    }
                    dataTable.Columns.Add(header);
                }
                if (templateData.ProblemType == CONSTANTS.ADSP)
                {
                    if (MainColumns.Count > 4)
                    {
                        message = CONSTANTS.MaxMainColumns;
                        return dataTable;
                    }
                }
                while (!sr.EndOfStream)
                {
                    string[] rows = Regex.Split(sr.ReadLine(), ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
                    DataRow dr = dataTable.NewRow();
                    for (int i = 0; i < headers.Length; i++)
                    {
                        dr[i] = rows[i];
                    }
                    dataTable.Rows.Add(dr);
                }

                TotalColumns = dataTable.Columns.Count;
                TotalRows = dataTable.Rows.Count;
                bool checkCount = IsRowColCount(TotalColumns, TotalRows, templateData.ProblemType, out message);
                if (!checkCount)
                {
                    templateInfo.ErrorMessage = message;
                    templateInfo.IsUploaded = false;
                    templateInfo.Status = CONSTANTS.E;
                    return dataTable;
                }
            }
            return dataTable;
        }
        private bool AssignValues(List<object> values, string mainColumn)
        {
            bool isNumeric = false;
            string col = Convert.ToString(values[0]);
            JArray cellItems = new JArray();
            if (col == CONSTANTS.ReleaseStartDate || col == CONSTANTS.ReleaseEndDate)
            {
                values.RemoveAt(0);
                cellItems = JArray.Parse(JsonConvert.SerializeObject(values));

                //date validation                
                DateTime parsed;
                foreach (string date in cellItems)
                {
                    if (!string.IsNullOrEmpty(date) && date != CONSTANTS.Null && date != CONSTANTS.undefined && date != null)
                    {
                        bool valid = DateTime.TryParseExact(date, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed);
                        if (!valid)
                        {
                            templateInfo.ErrorMessage = CONSTANTS.MC_DateFormat;
                            return true;
                        }
                    }
                    else
                    {
                        templateInfo.ErrorMessage = CONSTANTS.MC_DateNull;
                        return true;
                    }
                }
            }
            else
            {
                values.RemoveAt(0);
                foreach (string item in values)
                {
                    if (string.IsNullOrEmpty(item))
                    {
                        cellItems.Add(0);
                        templateInfo.blankData = true;
                        isNumeric = true;
                    }
                    else
                    {
                        isNumeric = IsNumeric(item.ToString());
                        if (isNumeric)
                        {
                            if (Convert.ToDouble(item) < 0)
                            {
                                templateInfo.ErrorMessage = CONSTANTS.NegativeNumber;
                                return false;
                            }
                            else if (Convert.ToDouble(item) > 99999999)
                            {
                                templateInfo.ErrorMessage = CONSTANTS.LargeNumber;
                                return false;
                            }
                            else
                                cellItems.Add(Convert.ToDouble(item));
                        }
                        else
                        {
                            templateInfo.ErrorMessage = CONSTANTS.NumericNumber;
                            return false;
                        }
                    }
                }
            }
            if (Features.Count > 0)
            {
                if (Features[mainColumn.Trim()] == null)
                {
                    JObject j2 = new JObject();
                    j2[col] = cellItems;
                    Features[mainColumn.Trim()] = JObject.FromObject(j2);
                }
                Features[mainColumn.Trim()][col] = cellItems;
            }
            else
            {
                JObject j2 = new JObject();
                j2[col] = cellItems;
                Features[mainColumn.Trim()] = JObject.FromObject(j2);
            }
            return isNumeric;
        }
        private static bool IsNumeric(object value)
        {
            decimal outputValue;
            return decimal.TryParse(value.ToString(), out outputValue);
        }
        private bool IsRowColCount(int totalCol, int totalRow, string problemType, out string message)
        {
            bool checkCount = true;
            message = string.Empty;
            if (problemType.Trim() == CONSTANTS.ADSP && totalRow > 22)
            {
                message = CONSTANTS.MaxRows;
                return false;
            }
            else if (problemType.Trim() == CONSTANTS.Generic && totalRow > 21)
            {
                message = CONSTANTS.MaxRows;
                return false;
            }
            else if (totalRow < 7)
            {
                message = CONSTANTS.MinRows;
                return false;
            }
            else if (totalCol < 3)
            {
                message = CONSTANTS.MinCols;
                return false;
            }
            return checkCount;
        }
        private string GenericFeatures(DataTable dt)
        {
            string testNumber = string.Empty;
            Dictionary<string, string> targetColList = new Dictionary<string, string>();
            Dictionary<string, string> categoricalColList = new Dictionary<string, string>();
            int rows = dt.Rows.Count;
            JArray Status = new JArray();
            for (int i = 0; i < rows; i++)
            {
                if (i == 0)
                    Status.Add("Current");
                else
                    Status.Add("Past");
            }

            Features.Add(new JProperty("Status", Status));
            foreach (DataColumn col in dt.Columns)
            {
                var values = dt.AsEnumerable().Select(r => r.Field<object>(col.ColumnName)).ToList();
                //bool numeric = false;
                JArray valuesArray = JArray.Parse(JsonConvert.SerializeObject(values));
                JArray subValues = new JArray();
                foreach (JToken item in valuesArray.Children())
                {
                    bool numericDatatype = false;
                    if (!string.IsNullOrEmpty(item.ToString()))
                        numericDatatype = IsNumeric(item.ToString());
                    if (numericDatatype)
                    {
                        //numeric = true;
                        if (Convert.ToDecimal(item) > 99999999)
                            testNumber = CONSTANTS.LargeNumber;
                        subValues.Add(Convert.ToDecimal(item));
                        if (!categoricalColList.ContainsKey(col.ColumnName))
                        {
                            if (!targetColList.ContainsKey(col.ColumnName))
                            {
                                targetColList.Add(col.ColumnName, "double");
                            }
                        }
                        if (!categoricalColList.ContainsKey(col.ColumnName))
                        {
                            categoricalColList.Add(col.ColumnName, "double");
                        }
                    }
                    else
                    {
                        subValues.Add(item);
                        //numeric = false;
                        if (!categoricalColList.ContainsKey(col.ColumnName))
                        {
                            categoricalColList.Add(col.ColumnName, "string");
                        }
                    }
                }
                if (Features.Count > 0)
                {
                    if (Features[col.ColumnName] == null)
                    {
                        Features.Add(new JProperty(col.ColumnName, subValues));
                    }
                }
                else
                {
                    Features.Add(new JProperty(col.ColumnName, subValues));
                }
            }
            templateData.TargetColumnList = targetColList;
            templateData.UniqueColumnList = categoricalColList;
            return testNumber;
        }
        private bool createNewFeatures(DataTable dt1)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(CreateFeatutes), "Start", "", "", "", "");
            dataTable2 = dt1;
            int rowCounter = 0;
            List<int> removeRow = new List<int>();
            foreach (DataRow row in dataTable2.Rows)
            {
                rowCounter++;
                if (row[0].ToString() == "Past")
                {
                    int phaseCounter = 8;
                    int pastValuecount = 0;
                    for (int i = 2; i < i + phaseCounter; i++)
                    {
                        if (i != 10 && i != 19)
                        {
                            if (!string.IsNullOrEmpty(row[i].ToString()) && Convert.ToInt32(row[i]) != 0)
                                pastValuecount = pastValuecount + 1;
                            if (i == 27)
                                break;
                        }
                    }
                    if (pastValuecount < 10)
                    {
                        string RemovephaseName = row[1].ToString().Trim();
                        if (!falsePhaseRows.Contains(RemovephaseName))
                            falsePhaseRows.Add(RemovephaseName);
                        if (!removeRow.Contains(rowCounter))
                            removeRow.Add(rowCounter);
                        //dataTable2.Rows.Remove(row);
                    }
                    //dt.Rows.Remove(row);
                }
                //2-9,11-18,20-27
                //var rowValues = dt.AsEnumerable().Select(r => r.Field<object>(row.nam.ColumnName)).ToList(); 
            }
            foreach (int i in removeRow)
                dataTable2.Rows.RemoveAt(i - 1);
            dataTable = dataTable2;
            TotalRows = dataTable.Rows.Count + 1;//ws.Dimension.End.Row;
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(CreateFeatutes), "end  : ", "", "", "", "");
            return true;
        }
        private bool CreateFeatutes(DataTable dt)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(CreateFeatutes), "Start", string.Empty, string.Empty, string.Empty, string.Empty);
            bool isNumeric = true;
            List<string> file = new List<string>();
            int counter = 1;
            string releaseName = "";
            int currentIndex = 0;
            int currentCount = 0; int pastCount = 0;

            foreach (DataColumn col in dt.Columns)
            {
                var ColumnValues = dt.AsEnumerable().Select(r => r.Field<object>(col.ColumnName)).ToList();

                if (counter < 3)
                {
                    if (ColumnValues[0].ToString() == CONSTANTS.ReleaseState)
                    {
                        ColumnValues.RemoveAt(0);
                        ReleaseState = JArray.Parse(JsonConvert.SerializeObject(ColumnValues));
                        //checking crrent and past count

                        foreach (var state in ReleaseState)
                        {
                            if (state.ToString() == "Current")
                                currentCount++;
                            if (state.ToString() == "Past")
                                pastCount++;
                        }
                        if (currentCount < 1 || pastCount < 4)
                        {
                            templateInfo.ErrorMessage = "Please upload file with 1 current release and more than 4 past release";
                            return false;
                        }

                        currentIndex = ColumnValues.IndexOf("Current");
                    }
                    if (ColumnValues[0].ToString() == CONSTANTS.ReleaseName)
                    {
                        ColumnValues.RemoveAt(0);
                        releaseName = ColumnValues[currentIndex].ToString();
                        ReleaseName = JArray.Parse(JsonConvert.SerializeObject(ColumnValues));
                    }
                }

                int percentCount = ((int)(0.75 * pastCount));
                if (counter > 2 & counter < 12)
                {
                    if (Array.Exists(SubColumns, element => element == ColumnValues[0].ToString()))
                    {
                        isNumeric = AssignValues(ColumnValues, MainColumns[0].Trim());
                        int pastPercentCount = 0;
                        for (int i = 0; i < ColumnValues.Count(); i++)
                        {
                            if (!string.IsNullOrEmpty(ColumnValues[i].ToString()) && Convert.ToDouble(ColumnValues[i].ToString()) > 0)
                            {
                                if (ReleaseState[i].ToString() == "Past")
                                    pastPercentCount++;
                            }
                        }

                        if (pastPercentCount < percentCount)
                        {
                            string PhaseName = dt.Rows[0][counter - 1].ToString();
                            if (!falseInputColumns.Contains(PhaseName))
                                falseInputColumns.Add(PhaseName);
                        }
                        if (!isNumeric)
                        {
                            return isNumeric;
                        }
                    }
                }
                if (counter >= 12 & counter < 21)
                {
                    if (Array.Exists(SubColumns, element => element == ColumnValues[0].ToString()))
                    {
                        isNumeric = AssignValues(ColumnValues, MainColumns[1].Trim());
                        int pastPercentCount = 0;
                        for (int i = 0; i < ColumnValues.Count(); i++)
                        {
                            if (!string.IsNullOrEmpty(ColumnValues[i].ToString()) && Convert.ToDouble(ColumnValues[i].ToString()) > 0)
                            {
                                if (ReleaseState[i].ToString() == "Past")
                                    pastPercentCount++;
                            }
                        }
                        if (pastPercentCount < percentCount)
                        {
                            string PhaseName = dt.Rows[0][counter - 1].ToString();
                            if (!falseInputColumns.Contains(PhaseName))
                                falseInputColumns.Add(PhaseName);
                        }
                        if (!isNumeric)
                        {
                            return isNumeric;
                        }
                    }
                }
                if (counter >= 21 & counter < 32)
                {
                    if (Array.Exists(SubColumns, element => element == ColumnValues[0].ToString()))
                    {
                        isNumeric = AssignValues(ColumnValues, MainColumns[2].Trim());
                        int pastPercentCount = 0;
                        for (int i = 0; i < ColumnValues.Count(); i++)
                        {
                            if (!string.IsNullOrEmpty(ColumnValues[i].ToString()) && Convert.ToDouble(ColumnValues[i].ToString()) > 0)
                            {
                                if (ReleaseState[i].ToString() == "Past")
                                    pastPercentCount++;
                            }
                        }
                        if (pastPercentCount < percentCount)
                        {
                            string PhaseName = dt.Rows[0][counter - 1].ToString();
                            if (!falseInputColumns.Contains(PhaseName))
                                falseInputColumns.Add(PhaseName);
                        }
                        if (!isNumeric)
                        {
                            return isNumeric;
                        }
                    }
                    if (ColumnValues[0].ToString() == "Release Start Date (dd/mm/yyyy)" || ColumnValues[0].ToString() == "Release End Date (dd/mm/yyyy)")
                    {
                        isNumeric = AssignValues(ColumnValues, MainColumns[2].Trim());
                        if (isNumeric)
                        {
                            return false;
                        }
                    }
                }
                if (counter > 31)
                {
                    if (Array.Exists(SubColumns, element => element == ColumnValues[0].ToString()))
                    {
                        isNumeric = AssignValues(ColumnValues, MainColumns[3].Trim());
                        if (!isNumeric)
                        {
                            return isNumeric;
                        }
                    }
                }
                counter++;
            }
            Features[CONSTANTS.ReleaseState] = ReleaseState;
            Features[CONSTANTS.ReleaseName] = ReleaseName;
            //adding SelectedCurrentRelease
            templateData.SelectedCurrentRelease = releaseName;
            //Total Count
            isNumeric = TotalCount();
            //check 37%
            if (falsePhaseRows.Count > 0)
            {
                string allPName = "";
                foreach (string pname in falsePhaseRows)
                    if (allPName != "")
                        allPName = allPName + ", " + pname;
                    else
                        allPName = pname;
                templateInfo.Message = allPName + " is removed due to insufficient data ";
            }
            //check 75% data
            if (falseInputColumns.Count > 0)
            {
                string allPName = "";
                foreach (string pname in falseInputColumns)
                    if (allPName != "")
                        allPName = allPName + ", " + pname;
                    else
                        allPName = pname;
                templateInfo.Message = templateInfo.Message + allPName + " phase is removed due to insufficient data";
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(CreateFeatutes), "end  : " + Convert.ToString(isNumeric), string.Empty, string.Empty, string.Empty, string.Empty);
            return isNumeric;

        }
        private bool TotalCount()
        {
            bool totalCheck = true;
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(TotalCount), "start", string.Empty, string.Empty, string.Empty, string.Empty);
            foreach (var maincolumn in MainColumns)
            {
                if (maincolumn != "Schedule (Days)")
                {
                    JArray j2 = new JArray();
                    for (int i = 0; i < TotalRows - 2; i++)
                    {
                        double counter22 = 0;
                        foreach (var subColumn in SubColumns)
                        {
                            if (Features[maincolumn.Trim()][subColumn] != null)
                            {
                                var item = Features[maincolumn.Trim()][subColumn][i];
                                if (!string.IsNullOrEmpty(Convert.ToString(item)) && item.ToString() != CONSTANTS.Null)
                                    counter22 += Convert.ToDouble(item);
                            }
                        }
                        j2.Add(counter22);
                    }
                    string mainColumn = string.Format("Overall " + maincolumn);
                    Features[maincolumn.Trim()][mainColumn] = j2;
                }
                else
                {
                    JArray j2 = new JArray();
                    for (int i = 0; i < TotalRows - 2; i++)
                    {
                        string[] dates = new string[] { "Release Start Date (dd/mm/yyyy)", "Release End Date (dd/mm/yyyy)" };
                        foreach (var subColumn in dates)
                        {
                            var item = Features[maincolumn.Trim()][subColumn][i];

                            if (!string.IsNullOrEmpty(Convert.ToString(item)) & item != null & subColumn == "Release Start Date (dd/mm/yyyy)")
                                startList.Add(Convert.ToDateTime(DateTime.ParseExact(item.ToString(), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None)));
                            if (!string.IsNullOrEmpty(Convert.ToString(item)) & item != null & subColumn == "Release End Date (dd/mm/yyyy)")
                                endList.Add(Convert.ToDateTime(DateTime.ParseExact(item.ToString(), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None)));
                        }
                    }
                    for (int j = 0; j < endList.Count; j++)
                    {
                        int compareDate = DateTime.Compare(endList[j], startList[j]);
                        if (compareDate > 0 || compareDate == 0)
                        {
                            double diff2 = (endList[j] - startList[j]).TotalDays;
                            j2.Add(diff2);
                        }
                        else
                        {
                            templateInfo.ErrorMessage = CONSTANTS.MC_EndDateGreater;
                            totalCheck = false;
                            return totalCheck;
                        }
                    }
                    string mainColumn = string.Format("Overall " + maincolumn);
                    Features[maincolumn.Trim()][mainColumn] = j2;
                }
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(TotalCount), "end  : " + Convert.ToString(totalCheck), string.Empty, string.Empty, string.Empty, string.Empty);
            return totalCheck;
        }
        private bool insertFeatutes(bool RRPFlag)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(insertFeatutes), "start", string.Empty, string.Empty, string.Empty, string.Empty);
            bool inserted = false;
            bool file_baseExists = this.CheckBaseVersion(CONSTANTS.Base_Version_FileUpload, templateData.ClientUID, templateData.DeliveryConstructUID);
            //overwrite base version
            if (templateInfo.InsertBase == true && file_baseExists == true)
            {
                var tempcollection = _database.GetCollection<BsonDocument>(CONSTANTS.TemplateData);
                var simulationData = _database.GetCollection<BsonDocument>(CONSTANTS.SimulationData);
                var simulationResult = _database.GetCollection<BsonDocument>(CONSTANTS.SimulationResults);
                var builder = Builders<BsonDocument>.Filter;
                var version_filter = builder.Eq(CONSTANTS.VersionAttribute, CONSTANTS.Base_Version_FileUpload) & builder.Eq(CONSTANTS.ClientUID, templateData.ClientUID) & builder.Eq(CONSTANTS.DeliveryConstructUID, templateData.DeliveryConstructUID);
                var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.TemplateID).Exclude(CONSTANTS.Id);
                var result = tempcollection.Find(version_filter).Project<BsonDocument>(projection).ToList();
                if (result.Count > 0)
                {
                    tempcollection.DeleteMany(version_filter);
                    simulationData.DeleteMany(version_filter);
                    simulationResult.DeleteMany(version_filter);
                }
                file_baseExists = false;
            }
            templateData._id = Guid.NewGuid().ToString();
            string[] columnsMain = CONSTANTS.MainColumns.Split(CONSTANTS.comma);
            JObject maincols = new JObject();
            JObject inputcols = new JObject();
            foreach (string cols in columnsMain)
            {
                maincols.Add(cols, "True");
            }

            templateData.MainSelection = maincols;
            if (RRPFlag != true && templateData.ProblemType.Trim() == CONSTANTS.ADSP)
            {
                if (appSettings.Value.DBEncryption)
                {
                    templateData.Features = appSettings.Value.IsAESKeyVault ? CryptographyUtility.Encrypt(Features.ToString(Formatting.None)): AesProvider.Encrypt(Features.ToString(Formatting.None), appSettings.Value.aesKey, appSettings.Value.aesVector);
                }
                else
                    templateData.Features = Features;
                string VersionName = Incremented_VersionName();
                if ((templateData.ProblemType == CONSTANTS.ADSP & VersionName == "Input Version 1") || templateInfo.InsertBase == true)
                {
                    if (!file_baseExists)
                    {
                        VersionName = CONSTANTS.Base_Version_FileUpload;
                        templateData.VersionMessage = "Base Version created for file upload.";
                    }
                }
                templateData.Version = VersionName;
                foreach (string cols in templateInfo.InputColumns)
                {
                    if (!falseInputColumns.Contains(cols))
                        inputcols.Add(cols, "True");
                    //    inputcols.Add(cols, "False");
                    //else

                }
                templateData.InputSelection = inputcols;
            }
            else if (templateData.ProblemType.Trim() == CONSTANTS.Generic)
            {
                if (appSettings.Value.DBEncryption)
                {
                    templateData.Features = appSettings.Value.IsAESKeyVault ? CryptographyUtility.Encrypt(Features.ToString(Formatting.None)): AesProvider.Encrypt(Features.ToString(Formatting.None), appSettings.Value.aesKey, appSettings.Value.aesVector);
                }
                else
                    templateData.Features = Features;

                templateData.Version = Incremented_VersionName();
            }
            else
            {
                templateData.Features = null;
                bool baseExists = this.CheckBaseVersion(CONSTANTS.Base_Version, templateData.ClientUID, templateData.DeliveryConstructUID);
                if (baseExists)
                {
                    templateData.Version = Incremented_VersionName();
                    templateData.VersionMessage = "Base Version already present.";
                }
                else
                {
                    templateData.Version = CONSTANTS.Base_Version;
                    templateData.VersionMessage = "Base Version created.";
                }
            }

            templateData.isDBEncryption = appSettings.Value.DBEncryption;
            if (appSettings.Value.DBEncryption)
            {
                if (!string.IsNullOrEmpty(Convert.ToString(templateData.CreatedByUser)))
                    templateData.CreatedByUser = appSettings.Value.IsAESKeyVault ? CryptographyUtility.Encrypt(Convert.ToString(templateData.CreatedByUser)): AesProvider.Encrypt(Convert.ToString(templateData.CreatedByUser), appSettings.Value.aesKey, appSettings.Value.aesVector);
                if (!string.IsNullOrEmpty(Convert.ToString(templateData.ModifiedByUser)))
                    templateData.ModifiedByUser = appSettings.Value.IsAESKeyVault ? CryptographyUtility.Encrypt(Convert.ToString(templateData.ModifiedByUser)):AesProvider.Encrypt(Convert.ToString(templateData.ModifiedByUser), appSettings.Value.aesKey, appSettings.Value.aesVector);
            }
            var inputQueue = JsonConvert.SerializeObject(templateData);
            var insertInputQueue = BsonSerializer.Deserialize<BsonDocument>(inputQueue);
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.TemplateData);
            collection.InsertOne(insertInputQueue);
            inserted = true;
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(insertFeatutes), "end  : " + Convert.ToString(inserted), string.Empty, string.Empty, string.Empty, string.Empty);
            return inserted;
        }
        public UpdateFeatures GenericUpdate(string TemplateID, string TargetColumn, string UniqueIdentifier, string UseCaseName, string UseCaseDescription)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(GenericUpdate), CONSTANTS.START, string.Empty, string.Empty, string.Empty, string.Empty);
            UpdateFeatures updateFeatures = new UpdateFeatures();
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.TemplateData);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.TemplateID, TemplateID);
            var projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
            var resultData = collection.Find(filter).Project<BsonDocument>(projection).FirstOrDefault();
            if (resultData != null)
            {
                JObject Features = new JObject();
                if (resultData[CONSTANTS.isDBEncryption] == true)
                    Features = JObject.Parse(appSettings.Value.IsAESKeyVault ? CryptographyUtility.Decrypt(resultData[CONSTANTS.Features].ToString()): AesProvider.Decrypt(resultData[CONSTANTS.Features].ToString(), appSettings.Value.aesKey, appSettings.Value.aesVector));
                else
                    Features = JObject.Parse(resultData[CONSTANTS.Features].AsBsonDocument.ToString());
                var columnlist = JsonConvert.DeserializeObject<Dictionary<string, string>>(resultData["UniqueColumnList"].AsBsonDocument.ToString());
                foreach (var item in columnlist)
                {
                    if (item.Key != UniqueIdentifier & item.Value != "double")
                    {
                        if (Features.Property(item.Key) != null)
                            Features.Property(item.Key).Remove();
                    }
                }
                if (resultData[CONSTANTS.isDBEncryption] == true)
                {
                    var features = appSettings.Value.IsAESKeyVault ? CryptographyUtility.Encrypt(Features.ToString()): AesProvider.Encrypt(Features.ToString(), appSettings.Value.aesKey, appSettings.Value.aesVector);//(BsonSerializer.Deserialize<BsonDocument>(Features.ToString()
                    var update = Builders<BsonDocument>.Update.Set(CONSTANTS.TargetColumn, TargetColumn)
                     .Set(CONSTANTS.UniqueIdentifierName, UniqueIdentifier)
                     .Set(CONSTANTS.UseCaseName, UseCaseName)
                     .Set(CONSTANTS.Features, features)
                     .Set(CONSTANTS.UseCaseDescription, UseCaseDescription);
                    var result = collection.UpdateMany(filter, update);
                    if (result.ModifiedCount > 0)
                    {
                        updateFeatures.Message = CONSTANTS.success;
                        updateFeatures.Status = true;
                    }
                }
                else
                {
                    var features = BsonSerializer.Deserialize<BsonDocument>(Features.ToString());
                    var update = Builders<BsonDocument>.Update.Set(CONSTANTS.TargetColumn, TargetColumn)
                     .Set(CONSTANTS.UniqueIdentifierName, UniqueIdentifier)
                     .Set(CONSTANTS.UseCaseName, UseCaseName)
                     .Set(CONSTANTS.Features, features)
                     .Set(CONSTANTS.UseCaseDescription, UseCaseDescription);
                    var result = collection.UpdateMany(filter, update);
                    if (result.ModifiedCount > 0)
                    {
                        updateFeatures.Message = CONSTANTS.success;
                        updateFeatures.Status = true;
                    }
                }

            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(GenericUpdate), CONSTANTS.END, string.Empty, string.Empty, string.Empty, string.Empty);
            return updateFeatures;
        }
        public GetInputInfo GetTemplateData(string ClientUID, string DeliveryConstructUID, string UserId, string TemplateID, string UseCaseName, string ProblemType)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(GetTemplateData), CONSTANTS.START, string.Empty, string.Empty, ClientUID,DeliveryConstructUID);
            getInputInfo = new GetInputInfo();
            var inputData = new BsonDocument();
            try
            {
                var collection = _database.GetCollection<BsonDocument>(CONSTANTS.TemplateData);
                var simulationCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SimulationResults);
                var builder = Builders<BsonDocument>.Filter;
                var projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
                if (!string.IsNullOrEmpty(TemplateID) & TemplateID != CONSTANTS.Null & TemplateID != "undefined")
                {
                    var filterTemplate = builder.Eq(CONSTANTS.ClientUID, ClientUID) & builder.Eq(CONSTANTS.DeliveryConstructUID, DeliveryConstructUID) & builder.Eq(CONSTANTS.UseCaseName, UseCaseName) & builder.Eq(CONSTANTS.TemplateID, TemplateID);
                    inputData = collection.Find(filterTemplate).Project<BsonDocument>(projection).SortByDescending(bson => bson[CONSTANTS.CreatedOn]).FirstOrDefault();
                }
                else
                {
                    if (ProblemType == CONSTANTS.ADSP || ProblemType == CONSTANTS.RRP)
                    {
                        var simfilterAll = builder.Eq(CONSTANTS.ClientUID, ClientUID) & builder.Eq(CONSTANTS.DeliveryConstructUID, DeliveryConstructUID) & builder.Eq(CONSTANTS.UseCaseID, CONSTANTS.ADSPID);
                        var outputData = simulationCollection.Find(simfilterAll).Project<BsonDocument>(projection).SortByDescending(bson => bson[CONSTANTS.CreatedOn]).FirstOrDefault();
                        if (outputData != null)
                        {
                            TemplateID = outputData[CONSTANTS.TemplateID].ToString();
                            var filterTemplate = builder.Eq(CONSTANTS.ClientUID, ClientUID) & builder.Eq(CONSTANTS.DeliveryConstructUID, DeliveryConstructUID) & builder.Eq(CONSTANTS.TemplateID, TemplateID);
                            inputData = collection.Find(filterTemplate).Project<BsonDocument>(projection).FirstOrDefault();
                        }
                        else
                        {
                            var filterAll = builder.Eq(CONSTANTS.ClientUID, ClientUID) & builder.Eq(CONSTANTS.DeliveryConstructUID, DeliveryConstructUID) & builder.Eq(CONSTANTS.UseCaseName, UseCaseName);
                            inputData = collection.Find(filterAll).Project<BsonDocument>(projection).SortByDescending(bson => bson[CONSTANTS.CreatedOn]).FirstOrDefault();
                        }
                    }
                    else
                    {
                        var filterAll = builder.Eq(CONSTANTS.ClientUID, ClientUID) & builder.Eq(CONSTANTS.DeliveryConstructUID, DeliveryConstructUID) & builder.Eq(CONSTANTS.UseCaseName, UseCaseName);
                        inputData = collection.Find(filterAll).Project<BsonDocument>(projection).SortByDescending(bson => bson[CONSTANTS.CreatedOn]).FirstOrDefault();
                    }
                }
                if (inputData != null)
                {
                    if (inputData[CONSTANTS.isDBEncryption] == true && !inputData[CONSTANTS.Features].IsBsonNull)
                        inputData[CONSTANTS.Features] = BsonDocument.Parse(appSettings.Value.IsAESKeyVault ? CryptographyUtility.Decrypt(inputData[CONSTANTS.Features].AsString):AesProvider.Decrypt(inputData[CONSTANTS.Features].AsString, appSettings.Value.aesKey, appSettings.Value.aesVector));
                    if (inputData[CONSTANTS.isDBEncryption] == true)
                    {
                        try
                        {
                            if (inputData.Contains(CONSTANTS.CreatedByUser) && !string.IsNullOrEmpty(Convert.ToString(inputData[CONSTANTS.CreatedByUser])))
                                inputData[CONSTANTS.CreatedByUser] = appSettings.Value.IsAESKeyVault ? CryptographyUtility.Decrypt(Convert.ToString(inputData[CONSTANTS.CreatedByUser])):AesProvider.Decrypt(Convert.ToString(inputData[CONSTANTS.CreatedByUser]), appSettings.Value.aesKey, appSettings.Value.aesVector);
                        }
                        catch (Exception ex) { LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(GetTemplateData) + "User is already decrypted....", ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty); }
                        try
                        {
                            if (inputData.Contains(CONSTANTS.ModifiedByUser) && !string.IsNullOrEmpty(Convert.ToString(inputData[CONSTANTS.ModifiedByUser])))
                                inputData[CONSTANTS.ModifiedByUser] = appSettings.Value.IsAESKeyVault ? CryptographyUtility.Decrypt(Convert.ToString(inputData[CONSTANTS.ModifiedByUser])): AesProvider.Decrypt(Convert.ToString(inputData[CONSTANTS.ModifiedByUser]), appSettings.Value.aesKey, appSettings.Value.aesVector);
                        }
                        catch (Exception ex) { LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(GetTemplateData) + "User is already decrypted....", ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty); }
                    }
                    getInputInfo.TemplateInfo = JObject.Parse(inputData.ToString());
                    getInputInfo.IsTemplates = true;
                }
                getInputInfo.TemplateVersions = VersionsList(ClientUID, DeliveryConstructUID, UseCaseName);
                getInputInfo.IsSimulationExist = false;
                //SimulationAvaiable Checking
                if (getInputInfo.TemplateInfo != null)
                {
                    string templateID = getInputInfo.TemplateInfo[CONSTANTS.TemplateID].ToString();
                    var simulationResult = simulationCollection.Find(Builders<BsonDocument>.Filter.Eq(CONSTANTS.TemplateID, templateID)).ToList();
                    if (simulationResult.Count > 0)
                    {
                        getInputInfo.IsSimulationExist = true;
                        getInputInfo.SimulationID = simulationResult[0][CONSTANTS.SimulationID].ToString();
                        getInputInfo.SimulationVersion = simulationResult[0][CONSTANTS.SimulationVersion].ToString();
                    }
                }
                return getInputInfo;
            }
            catch (Exception ex)
            {

                LOGGING.LogManager.Logger.LogErrorMessage(typeof(MCSimulationService), nameof(GetTemplateData), ex.Message, ex, string.Empty, string.Empty, ClientUID,DeliveryConstructUID);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(GetTemplateData), CONSTANTS.END, string.Empty, string.Empty, ClientUID, DeliveryConstructUID);
            return getInputInfo;
        }
        public GetOutputInfo GetSimulationData(string ClientUID, string DeliveryConstructUID, string UserId, string TemplateID, string SimulationID, string UseCaseID, string ProblemType)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(GetSimulationData), CONSTANTS.START, string.Empty, string.Empty, ClientUID, DeliveryConstructUID);
            getOutputInfo = new GetOutputInfo();
            var outputData = new BsonDocument();
            try
            {
                var simulationCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SimulationResults);
                var builder = Builders<BsonDocument>.Filter;
                var projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
                if (!string.IsNullOrEmpty(SimulationID) & SimulationID != CONSTANTS.Null & SimulationID != "undefined")
                {
                    var filterTemplate = builder.Eq(CONSTANTS.ClientUID, ClientUID) & builder.Eq(CONSTANTS.DeliveryConstructUID, DeliveryConstructUID) & builder.Eq(CONSTANTS.UseCaseID, UseCaseID) & builder.Eq(CONSTANTS.TemplateID, TemplateID) & builder.Eq(CONSTANTS.SimulationID, SimulationID);
                    outputData = simulationCollection.Find(filterTemplate).Project<BsonDocument>(projection).FirstOrDefault();
                }
                else
                {
                    var filterAll = builder.Eq(CONSTANTS.ClientUID, ClientUID) & builder.Eq(CONSTANTS.DeliveryConstructUID, DeliveryConstructUID) & builder.Eq(CONSTANTS.UseCaseID, UseCaseID) & builder.Eq(CONSTANTS.TemplateID, TemplateID);
                    outputData = simulationCollection.Find(filterAll).Project<BsonDocument>(projection).SortByDescending(bson => bson[CONSTANTS.CreatedOn]).FirstOrDefault();
                }
                if (outputData != null)
                {
                    if (outputData[CONSTANTS.isDBEncryption] == true)
                    {
                        if (ProblemType != CONSTANTS.Generic)
                        {
                            if (outputData.Contains(CONSTANTS.CertaintyValues))
                                outputData[CONSTANTS.CertaintyValues] = BsonDocument.Parse(appSettings.Value.IsAESKeyVault ? CryptographyUtility.Decrypt(outputData[CONSTANTS.CertaintyValues].ToString()):AesProvider.Decrypt(outputData[CONSTANTS.CertaintyValues].ToString(), appSettings.Value.aesKey, appSettings.Value.aesVector));
                            outputData[CONSTANTS.Observation] = appSettings.Value.IsAESKeyVault ? CryptographyUtility.Decrypt(outputData[CONSTANTS.Observation].ToString()):AesProvider.Decrypt(outputData[CONSTANTS.Observation].ToString(), appSettings.Value.aesKey, appSettings.Value.aesVector);
                            outputData["InfluencerDistributions"] = BsonDocument.Parse(appSettings.Value.IsAESKeyVault ? CryptographyUtility.Decrypt(outputData["InfluencerDistributions"].ToString()):AesProvider.Decrypt(outputData["InfluencerDistributions"].ToString(), appSettings.Value.aesKey, appSettings.Value.aesVector));
                        }
                        outputData[CONSTANTS.Influencers] = BsonDocument.Parse(appSettings.Value.IsAESKeyVault ? CryptographyUtility.Decrypt(outputData[CONSTANTS.Influencers].ToString()):AesProvider.Decrypt(outputData[CONSTANTS.Influencers].ToString(), appSettings.Value.aesKey, appSettings.Value.aesVector));
                        outputData[CONSTANTS.TargetCertainty] = appSettings.Value.IsAESKeyVault ? CryptographyUtility.Decrypt(outputData[CONSTANTS.TargetCertainty].ToString()):AesProvider.Decrypt(outputData[CONSTANTS.TargetCertainty].ToString(), appSettings.Value.aesKey, appSettings.Value.aesVector);
                        outputData[CONSTANTS.TargetVariable] = appSettings.Value.IsAESKeyVault ? CryptographyUtility.Decrypt(outputData[CONSTANTS.TargetVariable].ToString()):AesProvider.Decrypt(outputData[CONSTANTS.TargetVariable].ToString(), appSettings.Value.aesKey, appSettings.Value.aesVector);
                        outputData["Target_Distribution"] = BsonDocument.Parse(appSettings.Value.IsAESKeyVault ? CryptographyUtility.Decrypt(outputData["Target_Distribution"].ToString()):AesProvider.Decrypt(outputData["Target_Distribution"].ToString(), appSettings.Value.aesKey, appSettings.Value.aesVector));
                        outputData["ModelParams"] = appSettings.Value.IsAESKeyVault ? CryptographyUtility.Decrypt(outputData["ModelParams"].ToString()):AesProvider.Decrypt(outputData["ModelParams"].ToString(), appSettings.Value.aesKey, appSettings.Value.aesVector);
                        outputData["CurrentInfluencers"] = BsonDocument.Parse(appSettings.Value.IsAESKeyVault ? CryptographyUtility.Decrypt(outputData["CurrentInfluencers"].ToString()):AesProvider.Decrypt(outputData["CurrentInfluencers"].ToString(), appSettings.Value.aesKey, appSettings.Value.aesVector));
                        try
                        {
                            if (outputData.Contains(CONSTANTS.CreatedByUser) && !string.IsNullOrEmpty(Convert.ToString(outputData[CONSTANTS.CreatedByUser])))
                                outputData[CONSTANTS.CreatedByUser] = appSettings.Value.IsAESKeyVault ? CryptographyUtility.Decrypt(outputData[CONSTANTS.CreatedByUser].ToString()) : AesProvider.Decrypt(outputData[CONSTANTS.CreatedByUser].ToString(), appSettings.Value.aesKey, appSettings.Value.aesVector);
                        }
                        catch (Exception ex) { LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(GetSimulationData) + "User is already decrypted....", ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty); }
                        try
                        {
                            if (outputData.Contains(CONSTANTS.ModifiedByUser) && !string.IsNullOrEmpty(Convert.ToString(outputData[CONSTANTS.ModifiedByUser])))
                                outputData[CONSTANTS.ModifiedByUser] = appSettings.Value.IsAESKeyVault ? CryptographyUtility.Decrypt(outputData[CONSTANTS.ModifiedByUser].ToString()) : AesProvider.Decrypt(outputData[CONSTANTS.ModifiedByUser].ToString(), appSettings.Value.aesKey, appSettings.Value.aesVector) ;
                        }
                        catch (Exception ex) { LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(GetSimulationData) + "User is already decrypted....", ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty); }
                    }
                    getOutputInfo.SimulationInfo = JObject.Parse(outputData.ToString());
                    getOutputInfo.IsSimulationExist = true;
                }
                getOutputInfo.TemplateVersions = InputVersionList(ClientUID, DeliveryConstructUID, UseCaseID);
                getOutputInfo.SimulationVersions = SimulationVersionList(ClientUID, DeliveryConstructUID, UseCaseID, TemplateID);

                return getOutputInfo;
            }
            catch (Exception ex)
            {

                LOGGING.LogManager.Logger.LogErrorMessage(typeof(MCSimulationService), nameof(GetSimulationData), ex.Message, ex, string.Empty,  string.Empty, ClientUID, DeliveryConstructUID);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(GetSimulationData), CONSTANTS.END, string.Empty, string.Empty, ClientUID, DeliveryConstructUID);
            return getOutputInfo;
        }
        public VDSSimulatedData GetUseCaseSimulatedData(string ClientUID, string DeliveryConstructUID, string UseCaseID, string UserId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(GetUseCaseSimulatedData), CONSTANTS.START, string.Empty, string.Empty, ClientUID, DeliveryConstructUID);
            getOutputInfo = new GetOutputInfo();
            VDSSimulatedData data = new VDSSimulatedData();
            try
            {
                var simulationCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SimulationResults);
                var builder = Builders<BsonDocument>.Filter;
                var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.TargetCertainty).Include(CONSTANTS.TargetVariable).Include(CONSTANTS.Observation).Include(CONSTANTS.isDBEncryption).Exclude(CONSTANTS.Id);
                var filterAll = builder.Eq(CONSTANTS.ClientUID, ClientUID) & builder.Eq(CONSTANTS.DeliveryConstructUID, DeliveryConstructUID) & builder.Eq(CONSTANTS.UseCaseID, CONSTANTS.ADSPID);
                var outputData = simulationCollection.Find(filterAll).Project<BsonDocument>(projection).SortByDescending(bson => bson[CONSTANTS.ModifiedOn]).FirstOrDefault();
                if (outputData != null)
                {
                    if (outputData[CONSTANTS.isDBEncryption] == true)
                    {
                        data.TargetCertainty = Convert.ToDouble(appSettings.Value.IsAESKeyVault ? CryptographyUtility.Decrypt(outputData[CONSTANTS.TargetCertainty].ToString()): AesProvider.Decrypt(outputData[CONSTANTS.TargetCertainty].ToString(), appSettings.Value.aesKey, appSettings.Value.aesVector));
                        data.TargetVariable = Convert.ToDouble(appSettings.Value.IsAESKeyVault ? CryptographyUtility.Decrypt(outputData[CONSTANTS.TargetVariable].ToString()): AesProvider.Decrypt(outputData[CONSTANTS.TargetVariable].ToString(), appSettings.Value.aesKey, appSettings.Value.aesVector));
                        data.Observation = appSettings.Value.IsAESKeyVault ? CryptographyUtility.Decrypt(outputData[CONSTANTS.Observation].ToString()): AesProvider.Decrypt(outputData[CONSTANTS.Observation].ToString(), appSettings.Value.aesKey, appSettings.Value.aesVector);

                        data.TargetCertainty = Convert.ToDouble(AesProvider.Decrypt(outputData[CONSTANTS.TargetCertainty].ToString(), appSettings.Value.aesKey, appSettings.Value.aesVector));
                        data.TargetVariable = Convert.ToDouble(AesProvider.Decrypt(outputData[CONSTANTS.TargetVariable].ToString(), appSettings.Value.aesKey, appSettings.Value.aesVector));
                        data.Observation = AesProvider.Decrypt(outputData[CONSTANTS.Observation].ToString(), appSettings.Value.aesKey, appSettings.Value.aesVector);
                    }
                    else
                    {
                        data.TargetCertainty = Convert.ToDouble(outputData[CONSTANTS.TargetCertainty].ToString());
                        data.TargetVariable = Convert.ToDouble(outputData[CONSTANTS.TargetVariable].ToString());
                        data.Observation = outputData[CONSTANTS.Observation].ToString();
                    }
                    //data.TemplateID = outputData[CONSTANTS.TemplateID].ToString();
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(MCSimulationService), nameof(GetUseCaseSimulatedData), ex.Message, ex, string.Empty,  string.Empty, ClientUID, DeliveryConstructUID);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(GetUseCaseSimulatedData), CONSTANTS.END, string.Empty, string.Empty, ClientUID, DeliveryConstructUID);
            return data;
        }
        private List<TemplateVersion> VersionsList(string ClientUID, string DeliveryConstructUID, string useCaseName)
        {
            List<TemplateVersion> templateVersions = new List<TemplateVersion>();
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.TemplateData);
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq(CONSTANTS.ClientUID, ClientUID) & builder.Eq(CONSTANTS.DeliveryConstructUID, DeliveryConstructUID) & builder.Eq(CONSTANTS.UseCaseName, useCaseName);
            var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.TemplateID).Include(CONSTANTS.VersionAttribute).Exclude(CONSTANTS.Id);
            var versionData = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (versionData.Count > 0)
            {
                for (int i = 0; i < versionData.Count; i++)
                {
                    TemplateVersion template = new TemplateVersion();
                    template.TemplateID = versionData[i][CONSTANTS.TemplateID].ToString();
                    template.Version = versionData[i][CONSTANTS.VersionAttribute].ToString();
                    templateVersions.Add(template);
                }
            }
            return templateVersions;
        }
        public List<TemplateVersion> InputVersionList(string ClientUID, string DeliveryConstructUID, string UseCaseID)
        {
            List<TemplateVersion> templateVersions = new List<TemplateVersion>();
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.TemplateData);
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq(CONSTANTS.ClientUID, ClientUID) & builder.Eq(CONSTANTS.DeliveryConstructUID, DeliveryConstructUID) & builder.Eq(CONSTANTS.UseCaseID, UseCaseID);
            var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.TemplateID).Include(CONSTANTS.VersionAttribute).Exclude(CONSTANTS.Id);
            var versionData = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (versionData.Count > 0)
            {
                for (int i = 0; i < versionData.Count; i++)
                {
                    TemplateVersion template = new TemplateVersion();
                    template.TemplateID = versionData[i][CONSTANTS.TemplateID].ToString();
                    template.Version = versionData[i][CONSTANTS.VersionAttribute].ToString();
                    templateVersions.Add(template);
                }
            }
            return templateVersions;
        }
        public List<SimulationVersion> SimulationVersionList(string ClientUID, string DeliveryConstructUID, string UseCaseID, string TemplateId)
        {
            List<SimulationVersion> SimulationsList = new List<SimulationVersion>();
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SimulationResults);
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq(CONSTANTS.ClientUID, ClientUID) & builder.Eq(CONSTANTS.DeliveryConstructUID, DeliveryConstructUID) & builder.Eq(CONSTANTS.UseCaseID, UseCaseID) & builder.Eq(CONSTANTS.TemplateID, TemplateId);
            var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.SimulationID).Include(CONSTANTS.SimulationVersion).Include(CONSTANTS.TemplateID).Exclude(CONSTANTS.Id);
            var simualtionsData = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (simualtionsData.Count > 0)
            {
                for (int i = 0; i < simualtionsData.Count; i++)
                {
                    SimulationVersion simulationVersion = new SimulationVersion();
                    simulationVersion.SimulationID = Convert.ToString(simualtionsData[i][CONSTANTS.SimulationID]);
                    simulationVersion.Version = Convert.ToString(simualtionsData[i][CONSTANTS.SimulationVersion]);
                    simulationVersion.TemplateID = Convert.ToString(simualtionsData[i][CONSTANTS.TemplateID]);
                    SimulationsList.Add(simulationVersion);
                }
            }
            return SimulationsList;
        }
        public bool IsVSNameExist(string UseCaseID, string VSName, string collectionName, string TemplateId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(IsVSNameExist), CONSTANTS.START, string.Empty, string.Empty, string.Empty, string.Empty);
            bool isExist = false;
            try
            {
                var columnCollection = _database.GetCollection<BsonDocument>(collectionName);
                var builder = Builders<BsonDocument>.Filter;
                if (collectionName == CONSTANTS.TemplateData)
                {
                    var filter = builder.Eq(CONSTANTS.VersionAttribute, VSName) & builder.Eq(CONSTANTS.UseCaseID, UseCaseID);
                    var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.TemplateID).Include(CONSTANTS.VersionAttribute).Exclude(CONSTANTS.Id);
                    var result = columnCollection.Find(filter).Project<BsonDocument>(projection).ToList();
                    isExist = result.Count() > 0;
                }
                else if (collectionName == CONSTANTS.SimulationResults)
                {
                    var filter = builder.Eq(CONSTANTS.SimulationVersion, VSName) & builder.Eq(CONSTANTS.UseCaseID, UseCaseID) & builder.Eq(CONSTANTS.TemplateID, TemplateId);
                    var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.SimulationID).Include(CONSTANTS.SimulationVersion).Exclude(CONSTANTS.Id);
                    var result = columnCollection.Find(filter).Project<BsonDocument>(projection).ToList();
                    isExist = result.Count() > 0;
                }
                return isExist;
            }
            catch (Exception ex)
            { LOGGING.LogManager.Logger.LogErrorMessage(typeof(MCSimulationService), nameof(IsVSNameExist), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty); }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(IsVSNameExist), CONSTANTS.END, string.Empty, string.Empty, string.Empty, string.Empty);
            return isExist;
        }
        public void updateVersionSimulationName(string ClientUID, string DeliveryConstructUID, string UseCaseID, string UserId, string TemplateId, string simulationID, string NewName)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(updateVersionSimulationName), CONSTANTS.START, string.Empty, string.Empty, ClientUID, DeliveryConstructUID);
            try
            {
                if (!string.IsNullOrEmpty(simulationID) && simulationID != CONSTANTS.Null)
                {
                    var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SimulationResults);
                    var builder = Builders<BsonDocument>.Filter;
                    var filter = builder.Eq(CONSTANTS.SimulationID, simulationID) & builder.Eq(CONSTANTS.UseCaseID, UseCaseID) & builder.Eq(CONSTANTS.ClientUID, ClientUID) & builder.Eq(CONSTANTS.DeliveryConstructUID, DeliveryConstructUID) & builder.Eq(CONSTANTS.TemplateID, TemplateId);
                    var updateVersion = Builders<BsonDocument>.Update.Set(CONSTANTS.SimulationVersion, NewName);
                    collection.UpdateOne(filter, updateVersion);
                }
                else
                {
                    var collection = _database.GetCollection<BsonDocument>(CONSTANTS.TemplateData);
                    var builder = Builders<BsonDocument>.Filter;
                    var filter = builder.Eq(CONSTANTS.TemplateID, TemplateId) & builder.Eq(CONSTANTS.UseCaseID, UseCaseID) & builder.Eq(CONSTANTS.ClientUID, ClientUID) & builder.Eq(CONSTANTS.DeliveryConstructUID, DeliveryConstructUID);
                    var updateVersion = Builders<BsonDocument>.Update.Set(CONSTANTS.VersionAttribute, NewName);
                    collection.UpdateOne(filter, updateVersion);
                    collection = _database.GetCollection<BsonDocument>(CONSTANTS.SimulationResults);
                    updateVersion = Builders<BsonDocument>.Update.Set(CONSTANTS.TemplateVersion, NewName);
                    collection.UpdateMany(filter, updateVersion);
                }
            }
            catch (Exception ex)
            { LOGGING.LogManager.Logger.LogErrorMessage(typeof(MCSimulationService), nameof(updateVersionSimulationName), ex.Message, ex, string.Empty,  string.Empty, ClientUID, DeliveryConstructUID); }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(updateVersionSimulationName), CONSTANTS.END, string.IsNullOrEmpty(TemplateId) ? default(Guid) : new Guid(TemplateId), string.Empty, string.Empty, ClientUID, DeliveryConstructUID);

        }
        public string UpdateTemplateInfo(dynamic data, string TemplateId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(UpdateTemplateInfo), CONSTANTS.START, string.Empty, string.Empty, string.Empty, string.Empty);
            string resultData = string.Empty;
            try
            {
                var collection = _database.GetCollection<BsonDocument>(CONSTANTS.TemplateData);
                var simulationData = _database.GetCollection<BsonDocument>(CONSTANTS.SimulationData);
                var simulationResult = _database.GetCollection<BsonDocument>(CONSTANTS.SimulationResults);
                var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.TemplateID, TemplateId);
                var projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
                var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();
                if (data.Features != null)
                {
                    if (data.ProblemType == CONSTANTS.Generic)
                    {
                        var targetUpdate = Builders<BsonDocument>.Update.Set(CONSTANTS.TargetColumn, data[CONSTANTS.TargetColumn].ToString());
                        collection.UpdateOne(filter, targetUpdate);
                    }
                    //BsonDocument doc2 = BsonDocument.Parse(data[CONSTANTS.Features].ToString());
                    if (result[0][CONSTANTS.isDBEncryption] == true)
                    {
                        var doc2 = appSettings.Value.IsAESKeyVault ? CryptographyUtility.Encrypt(data[CONSTANTS.Features].ToString()): AesProvider.Encrypt(data[CONSTANTS.Features].ToString(), appSettings.Value.aesKey, appSettings.Value.aesVector);
                        var featureUpdate = Builders<BsonDocument>.Update.Set(CONSTANTS.Features, doc2);
                        collection.UpdateOne(filter, featureUpdate);
                    }
                    else
                    {
                        BsonDocument doc2 = BsonDocument.Parse(data[CONSTANTS.Features].ToString());
                        var featureUpdate = Builders<BsonDocument>.Update.Set(CONSTANTS.Features, doc2);
                        collection.UpdateOne(filter, featureUpdate);
                    }
                    //update release selection
                    if (!string.IsNullOrEmpty(Convert.ToString(data[CONSTANTS.SelectedCurrentRelease])))
                    {
                        var releaseUpdate = Builders<BsonDocument>.Update.Set(CONSTANTS.SelectedCurrentRelease, data[CONSTANTS.SelectedCurrentRelease].ToString());
                        collection.UpdateOne(filter, releaseUpdate);
                    }
                    //update input selection
                    if (!string.IsNullOrEmpty(Convert.ToString(data[CONSTANTS.InputSelection])))
                    {
                        BsonDocument inputDoc = BsonDocument.Parse(data[CONSTANTS.InputSelection].ToString());
                        var inputUpdate = Builders<BsonDocument>.Update.Set(CONSTANTS.InputSelection, inputDoc);
                        collection.UpdateOne(filter, inputUpdate);
                    }
                    //update main columns selection
                    if (!string.IsNullOrEmpty(Convert.ToString(data[CONSTANTS.MainSelection])))
                    {
                        BsonDocument mainColdoc = BsonDocument.Parse(data[CONSTANTS.MainSelection].ToString());
                        var mainColUpdate = Builders<BsonDocument>.Update.Set(CONSTANTS.MainSelection, mainColdoc);
                        collection.UpdateOne(filter, mainColUpdate);
                    }
                    simulationData.DeleteMany(filter);
                    simulationResult.DeleteMany(filter);
                }

                //return resultData = CONSTANTS.Success;
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(MCSimulationService), nameof(UpdateTemplateInfo), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return resultData = CONSTANTS.Error + "StackTrace= " + ex.StackTrace;
            }

            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(UpdateTemplateInfo), CONSTANTS.END, string.IsNullOrEmpty(TemplateId) ? default(Guid) : new Guid(TemplateId), string.Empty, string.Empty, string.Empty, string.Empty);
            return resultData = CONSTANTS.Success;
        }
        public string CloneTemplateInfo(string ProblemType, string UserId, string TemplateId, string NewName, JObject Features, string TargetColumn)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(CloneTemplateInfo), CONSTANTS.START, string.Empty, string.Empty, string.Empty, string.Empty);
            string newTemplateID = string.Empty;
            try
            {
                var collection = _database.GetCollection<BsonDocument>(CONSTANTS.TemplateData);
                var projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
                var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.TemplateID, TemplateId);
                var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();
                if (result.Count > 0)
                {
                    JObject data = new JObject();
                    data = JObject.Parse(result[0].ToString());
                    if (data.ContainsKey(CONSTANTS.TemplateID))
                    {
                        data[CONSTANTS.TemplateID] = Guid.NewGuid().ToString();
                        newTemplateID = data[CONSTANTS.TemplateID].ToString();
                    }
                    if (data.ContainsKey(CONSTANTS.VersionAttribute))
                    {
                        data[CONSTANTS.VersionAttribute] = NewName;
                    }
                    if (data.ContainsKey(CONSTANTS.Features))
                    {
                        var features = appSettings.Value.IsAESKeyVault ? CryptographyUtility.Encrypt(Convert.ToString(Features)): AesProvider.Encrypt(Convert.ToString(Features), appSettings.Value.aesKey, appSettings.Value.aesVector);
                        data[CONSTANTS.Features] = features;
                    }
                    if (ProblemType == CONSTANTS.Generic)
                    {
                        if (data.ContainsKey(CONSTANTS.TargetColumn))
                        {
                            data[CONSTANTS.TargetColumn] = TargetColumn;
                        }
                    }
                    InsertClone(data, UserId, CONSTANTS.TemplateData);
                }
                return newTemplateID;
            }
            catch (Exception ex)
            { LOGGING.LogManager.Logger.LogErrorMessage(typeof(MCSimulationService), nameof(CloneTemplateInfo), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty); }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(CloneTemplateInfo), CONSTANTS.END, string.IsNullOrEmpty(TemplateId) ? default(Guid) : new Guid(TemplateId), string.Empty, string.Empty, string.Empty, string.Empty);
            return newTemplateID;
        }
        public string CloneSimulation(string ProblemType, string UserId, string TemplateId, string SimulationID, string NewName, JObject inputs, string TargetColumn)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(CloneSimulation), CONSTANTS.START, string.Empty, string.Empty, string.Empty, string.Empty);
            string newSimulationID = string.Empty;
            try
            {
                var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SimulationResults);
                var projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
                var builder = Builders<BsonDocument>.Filter;
                var filter = builder.Eq(CONSTANTS.SimulationID, SimulationID) & builder.Eq(CONSTANTS.TemplateID, TemplateId);
                var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();
                if (result.Count > 0)
                {
                    JObject data = new JObject();
                    data = JObject.Parse(result[0].ToString());
                    if (data.ContainsKey(CONSTANTS.SimulationID))
                    {
                        data[CONSTANTS.SimulationID] = Guid.NewGuid().ToString();
                        newSimulationID = data[CONSTANTS.SimulationID].ToString();
                    }
                    if (data.ContainsKey(CONSTANTS.SimulationVersion))
                    {
                        data[CONSTANTS.SimulationVersion] = NewName;
                    }
                    if (result[0][CONSTANTS.isDBEncryption] == true)
                    {
                        data[CONSTANTS.Influencers] = appSettings.Value.IsAESKeyVault ? CryptographyUtility.Encrypt(JObject.Parse(inputs[CONSTANTS.Influencers].ToString()).ToString()): AesProvider.Encrypt(JObject.Parse(inputs[CONSTANTS.Influencers].ToString()).ToString(), appSettings.Value.aesKey, appSettings.Value.aesVector);
                        data[CONSTANTS.TargetCertainty] = appSettings.Value.IsAESKeyVault ? CryptographyUtility.Encrypt(Convert.ToDouble(inputs[CONSTANTS.TargetCertainty]).ToString()) : AesProvider.Encrypt(Convert.ToDouble(inputs[CONSTANTS.TargetCertainty]).ToString(), appSettings.Value.aesKey, appSettings.Value.aesVector);
                        data[CONSTANTS.TargetVariable] = appSettings.Value.IsAESKeyVault ? CryptographyUtility.Encrypt(Convert.ToDouble(inputs[CONSTANTS.TargetVariable]).ToString()) : AesProvider.Encrypt(Convert.ToDouble(inputs[CONSTANTS.TargetVariable]).ToString(), appSettings.Value.aesKey, appSettings.Value.aesVector);
                        data[CONSTANTS.IncrementFlags] = appSettings.Value.IsAESKeyVault ? CryptographyUtility.Encrypt(JObject.Parse(inputs["IncrementFlags"].ToString()).ToString()) : AesProvider.Encrypt(JObject.Parse(inputs["IncrementFlags"].ToString()).ToString(), appSettings.Value.aesKey, appSettings.Value.aesVector);
                        data[CONSTANTS.PercentChange] = appSettings.Value.IsAESKeyVault ? CryptographyUtility.Encrypt(JObject.Parse(inputs["PercentChange"].ToString()).ToString()) : AesProvider.Encrypt(JObject.Parse(inputs["PercentChange"].ToString()).ToString(), appSettings.Value.aesKey, appSettings.Value.aesVector);
                        if (data.ContainsKey(CONSTANTS.CreatedByUser) && !string.IsNullOrEmpty(Convert.ToString(inputs[CONSTANTS.CreatedByUser])))
                            data[CONSTANTS.CreatedByUser] = appSettings.Value.IsAESKeyVault ? CryptographyUtility.Encrypt(inputs[CONSTANTS.CreatedByUser].ToString()): AesProvider.Encrypt(inputs[CONSTANTS.CreatedByUser].ToString(), appSettings.Value.aesKey, appSettings.Value.aesVector);
                        if (data.ContainsKey(CONSTANTS.ModifiedByUser) && !string.IsNullOrEmpty(Convert.ToString(inputs[CONSTANTS.ModifiedByUser])))
                            data[CONSTANTS.ModifiedByUser] = appSettings.Value.IsAESKeyVault ? CryptographyUtility.Encrypt(inputs[CONSTANTS.ModifiedByUser].ToString()): AesProvider.Encrypt(inputs[CONSTANTS.ModifiedByUser].ToString(), appSettings.Value.aesKey, appSettings.Value.aesVector);
                    }
                    else
                    {
                        data[CONSTANTS.Influencers] = JObject.Parse(inputs[CONSTANTS.Influencers].ToString());
                        data[CONSTANTS.TargetCertainty] = Convert.ToDouble(inputs[CONSTANTS.TargetCertainty]);
                        data[CONSTANTS.TargetVariable] = Convert.ToDouble(inputs[CONSTANTS.TargetVariable]);
                        data[CONSTANTS.IncrementFlags] = JObject.Parse(inputs["IncrementFlags"].ToString());
                        data[CONSTANTS.PercentChange] = JObject.Parse(inputs["PercentChange"].ToString());
                        if (data.ContainsKey(CONSTANTS.ModifiedByUser))
                            data[CONSTANTS.ModifiedByUser] = Convert.ToString(inputs[CONSTANTS.ModifiedByUser]);
                        if (data.ContainsKey(CONSTANTS.CreatedByUser))
                            data[CONSTANTS.CreatedByUser] = Convert.ToString(inputs[CONSTANTS.CreatedByUser]);
                    }
                    if (ProblemType == CONSTANTS.ADSP || ProblemType == CONSTANTS.RRP)
                    {
                        if (result[0][CONSTANTS.isDBEncryption] == true)
                        {
                            data[CONSTANTS.Observation] = appSettings.Value.IsAESKeyVault ? CryptographyUtility.Encrypt(inputs[CONSTANTS.Observation].ToString()) : AesProvider.Encrypt(inputs[CONSTANTS.Observation].ToString(), appSettings.Value.aesKey, appSettings.Value.aesVector);
                        }
                        else
                        {
                            data[CONSTANTS.Observation] = inputs[CONSTANTS.Observation].ToString();
                        }
                    }

                    if (ProblemType == CONSTANTS.Generic)
                    {
                        if (data.ContainsKey(CONSTANTS.TargetColumn))
                        {
                            data[CONSTANTS.TargetColumn] = TargetColumn;
                        }
                    }
                    InsertClone(data, UserId, CONSTANTS.SimulationResults);
                }
                return newSimulationID;
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(MCSimulationService), nameof(CloneSimulation), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                newSimulationID = string.Empty;
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(CloneSimulation), CONSTANTS.END, string.IsNullOrEmpty(TemplateId) ? default(Guid) : new Guid(TemplateId), string.Empty, string.Empty, string.Empty, string.Empty);
            return newSimulationID;
        }
        private void InsertClone(JObject data, string userId, string collectionName)
        {
            if (data.ContainsKey(CONSTANTS.CreatedOn))
            {
                data[CONSTANTS.CreatedOn] = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
            }
            if (data.ContainsKey(CONSTANTS.CreatedByUser))
            {
                if (appSettings.Value.DBEncryption)
                {
                    if(!string.IsNullOrEmpty(Convert.ToString(userId)))
                        data[CONSTANTS.CreatedByUser] = appSettings.Value.IsAESKeyVault ? CryptographyUtility.Encrypt(Convert.ToString(userId)): AesProvider.Encrypt(Convert.ToString(userId), appSettings.Value.aesKey, appSettings.Value.aesVector);
                }
                else
                    data[CONSTANTS.CreatedByUser] = userId;
            }
            if (data.ContainsKey(CONSTANTS.ModifiedByUser))
            {
                data[CONSTANTS.ModifiedByUser] = null;
            }
            var jsonColumns = Newtonsoft.Json.JsonConvert.SerializeObject(data);
            var insertBsonColumns = BsonSerializer.Deserialize<BsonDocument>(jsonColumns);
            var collection = _database.GetCollection<BsonDocument>(collectionName);
            collection.InsertOne(insertBsonColumns);
        }
        private string GenerateToken()
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), nameof(GenerateToken), CONSTANTS.START + appSettings.Value.authProvider.ToUpper(), string.Empty, string.Empty, string.Empty, string.Empty);
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
            else if (appSettings.Value.authProvider.ToUpper() == "AZUREAD")
            {
                var client = new RestClient(appSettings.Value.token_Url);
                var request = new RestRequest(Method.POST);
                request.AddHeader("cache-control", "no-cache");
                request.AddHeader("content-type", "application/x-www-form-urlencoded");
                request.AddParameter("application/x-www-form-urlencoded", "grant_type=" + appSettings.Value.Grant_Type.Trim() +
                    "&client_id=" + appSettings.Value.clientId_clustering.Trim() +
                    "&client_secret=" + appSettings.Value.client_secret_clustering.Trim() +
                    "&resource=" + appSettings.Value.resource_clustering.Trim(),
                    ParameterType.RequestBody);

                IRestResponse response = client.Execute(request);
                string json = response.Content;
                var x = (Newtonsoft.Json.JsonConvert.DeserializeObject(json)) as dynamic;
                if (x != null)
                    token = Convert.ToString(x.access_token);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InstaModelService), nameof(GenerateToken), "END -" + appSettings.Value.authProvider.ToUpper() + "token start: " + token + " token end", string.Empty, string.Empty, string.Empty, string.Empty);
            return token;
        }
        private bool CheckBaseVersion(string VersionName, string ClientUID, string DCUID)
        {
            var columnCollection = _database.GetCollection<BsonDocument>(CONSTANTS.TemplateData);
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq(CONSTANTS.VersionAttribute, VersionName) & builder.Eq(CONSTANTS.ClientUID, ClientUID) & builder.Eq(CONSTANTS.DeliveryConstructUID, DCUID);
            var outcome = columnCollection.Find(filter).ToList();
            return outcome.Count > 0 ? true : false;
        }
        private bool CheckSimulationExists(string templateID)
        {
            var columnCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SimulationData);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.TemplateID, templateID);
            var outcome = columnCollection.Find(filter).ToList();
            return outcome.Count > 0 ? true : false;
        }
        public HttpResponseMessage GetPythonResult(string TemplateID)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(GetPythonResult), CONSTANTS.START, string.Empty, string.Empty, string.Empty, string.Empty);
            Dictionary<string, string> template = new Dictionary<string, string>();
            template.Add(CONSTANTS.TemplateID, TemplateID);
            string url = appSettings.Value.MonteCarloPythonURL + CONSTANTS.RunSimulationAPI;
            HttpResponseMessage httpResponse = null;
            string token = GenerateToken();
            if (!string.IsNullOrEmpty(token))
            {
                using (var client = new HttpClient())
                {
                    var content = new StringContent(JsonConvert.SerializeObject(template), Encoding.UTF8, "application/json");
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(GetPythonResult), "url: " + url + "content: " + content, string.Empty, string.Empty, string.Empty, string.Empty);
                    httpResponse = client.PostAsync(url, content).Result;
                }
            }
            else
            {
                HttpClientHandler hnd = new HttpClientHandler();
                hnd.UseDefaultCredentials = true;

                using (var client = new HttpClient(hnd))
                {
                    var content = new StringContent(JsonConvert.SerializeObject(template), Encoding.UTF8, "application/json");
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(GetPythonResult), "url: " + url + "content: " + content, string.Empty, string.Empty, string.Empty, string.Empty);
                    httpResponse = client.PostAsync(url, content).Result;
                }
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(GetPythonResult), CONSTANTS.End + " httpresponse" + httpResponse, string.Empty, string.Empty, string.Empty, string.Empty);
            return httpResponse;
        }
        public HttpResponseMessage GetwhatIfResult(string TemplateID, string simulationId, JObject inputs)
        {
            Dictionary<string, string> template = new Dictionary<string, string>();
            template.Add(CONSTANTS.TemplateID, TemplateID);
            template.Add(CONSTANTS.SimulationID, simulationId);
            template.Add("inputs", inputs.ToString());
            string url = appSettings.Value.MonteCarloPythonURL + CONSTANTS.whatIfAnalysisAPI;
            HttpResponseMessage httpResponse = null;
            string token = GenerateToken();
            if (!string.IsNullOrEmpty(token))
            {
                using (var client = new HttpClient())
                {
                    var content = new StringContent(JsonConvert.SerializeObject(template), Encoding.UTF8, "application/json");
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
                    httpResponse = client.PostAsync(url, content).Result;
                }
            }
            else
            {
                HttpClientHandler hnd = new HttpClientHandler();
                hnd.UseDefaultCredentials = true;

                using (var client = new HttpClient(hnd))
                {
                    var content = new StringContent(JsonConvert.SerializeObject(template), Encoding.UTF8, "application/json");
                    httpResponse = client.PostAsync(url, content).Result;
                }
            }
            return httpResponse;
        }
        public HttpResponseMessage GetRRPResult(string TemplateID, string Version, string ClientUID, string DeliveryConstructUID)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(GetRRPResult), CONSTANTS.START, string.Empty, string.Empty, ClientUID, DeliveryConstructUID);
            Dictionary<string, string> template = new Dictionary<string, string>();
            template.Add(CONSTANTS.TemplateID, TemplateID);
            template.Add(CONSTANTS.VersionAttribute, Version);
            template.Add(CONSTANTS.ClientUID, ClientUID);
            template.Add(CONSTANTS.DeliveryConstructUID, DeliveryConstructUID);
            string url = appSettings.Value.MonteCarloPythonURL + CONSTANTS.IngestRRPDataAPI;
            HttpResponseMessage httpResponse = null;
            string token = GenerateToken();
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(GetRRPResult), "token :" + token, string.Empty, string.Empty, ClientUID, DeliveryConstructUID);
            if (token != null)
            {
                using (var client = new HttpClient())
                {
                    var content = new StringContent(JsonConvert.SerializeObject(template), Encoding.UTF8, "application/json");
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(GetRRPResult), "url :" + url + " and content :" + JsonConvert.SerializeObject(template), string.Empty,string.Empty, ClientUID, DeliveryConstructUID);
                    httpResponse = client.PostAsync(url, content).Result;
                }
            }
            else
            {
                HttpClientHandler hnd = new HttpClientHandler();
                hnd.UseDefaultCredentials = true;
                hnd.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
                try
                {
                    using (var client = new HttpClient(hnd))
                    {
                        var content = new StringContent(JsonConvert.SerializeObject(template), Encoding.UTF8, "application/json");
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(GetRRPResult), "url :" + url + " and content :" + JsonConvert.SerializeObject(template), string.Empty, string.Empty, ClientUID, DeliveryConstructUID);
                        try
                        {
                            httpResponse = client.PostAsync(url, content).Result;
                        }
                        catch (Exception ex)
                        {
                            LOGGING.LogManager.Logger.LogErrorMessage(typeof(MCSimulationService), nameof(GetRRPResult), "call to py failed : " + ex.Message, ex, string.Empty, string.Empty, ClientUID, DeliveryConstructUID);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LOGGING.LogManager.Logger.LogErrorMessage(typeof(MCSimulationService), nameof(GetRRPResult), "call to py failed : " + ex.Message, ex, string.Empty,  string.Empty, ClientUID, DeliveryConstructUID);
                }
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(GetRRPResult), "httpResponse : " + httpResponse, string.Empty,  string.Empty, ClientUID, DeliveryConstructUID);
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(GetRRPResult), CONSTANTS.END, string.Empty, string.Empty, ClientUID, DeliveryConstructUID);
            return httpResponse;
        }
        public SimulationPrediction RunSimulation(string templateID, string simulationID, string ProblemType, bool PythonCall, string ClientUID, string DCUID, string UserId, string UseCaseID, string UseCaseName, bool RRPFlag, string SelectedCurrentRelease, JObject selectionUpdate)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(RunSimulation), CONSTANTS.START, string.Empty, string.Empty, ClientUID, DCUID);
            if (RRPFlag == true)
            {
                UpdateSelectedCurrentRelease(templateID, SelectedCurrentRelease);
            }

            HttpResponseMessage httpResponse = null;
            string pythonResult = string.Empty;
            if (PythonCall)
            {
                if (ProblemType == CONSTANTS.ADSP && selectionUpdate.ToString() != "{}")
                    UpdateSelection(templateID, UserId, UseCaseID, selectionUpdate, ClientUID, DCUID, UseCaseName, "Phase");
                httpResponse = GetPythonResult(templateID);
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(RunSimulation), "httpResponse: " + httpResponse, string.Empty, string.Empty, ClientUID, DCUID);
            }
            if (PythonCall == true & httpResponse == null)
            {
                simulationPrediction.ErrorMessage = "Token Generation Error";
                simulationPrediction.Status = CONSTANTS.E;
                return simulationPrediction;
            }
            if (PythonCall)
            {
                if (httpResponse.StatusCode.ToString() != "InternalServerError" && httpResponse.StatusCode.ToString() != "BadGateway")
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(RunSimulation), "Python Response -" + httpResponse.Content.ReadAsStringAsync().Result, string.Empty, string.Empty, ClientUID, DCUID);
                    if (httpResponse.StatusCode.ToString() != "Unauthorized")
                    {
                        JObject pythonResponse = (JObject)JsonConvert.DeserializeObject(httpResponse.Content.ReadAsStringAsync().Result);
                        if (pythonResponse["Error"] != null)
                        {
                            simulationPrediction.ErrorMessage = "Authorization Fail or Python Error " + pythonResponse["Error"].ToString(); ;
                            simulationPrediction.Status = CONSTANTS.E;
                            return simulationPrediction;
                        }
                        if (pythonResponse["Status"] != null)
                        {
                            GetSimulatedData(templateID, simulationID, ProblemType);
                            GetSimulationResults(templateID, simulationID, ProblemType);
                            if (simulationPrediction.Status == CONSTANTS.E)
                            {
                                return simulationPrediction;
                            }
                            GetVersionsList(ClientUID, DCUID, UseCaseID);
                            simulationPrediction.Status = CONSTANTS.C;
                        }
                    }
                    else
                    {
                        simulationPrediction.ErrorMessage = "Authorization Fail for python";
                        simulationPrediction.Status = CONSTANTS.E;
                        return simulationPrediction;
                    }

                }
                else
                {
                    simulationPrediction.ErrorMessage = "Python Error :" + httpResponse.StatusCode.ToString() + " Content : " + httpResponse.Content.ReadAsStringAsync().Result;
                    simulationPrediction.Status = CONSTANTS.E;
                    return simulationPrediction;
                }
            }
            else
            {
                GetSimulatedData(templateID, simulationID, ProblemType);
                GetSimulationResults(templateID, simulationID, ProblemType);
                if (simulationPrediction.Status == CONSTANTS.E)
                {
                    return simulationPrediction;
                }
                if (UseCaseName == CONSTANTS.Generic)
                    GetVersionsList(ClientUID, DCUID, UseCaseID, UseCaseName);
                else
                    GetVersionsList(ClientUID, DCUID, UseCaseID);
                simulationPrediction.Status = CONSTANTS.C;
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(RunSimulation), CONSTANTS.END, string.Empty, string.Empty, ClientUID, DCUID);
            return simulationPrediction;
        }
        private void GetSimulatedData(string templateID, string simulationID, string ProblemType)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(GetSimulatedData), CONSTANTS.START, string.Empty, string.Empty, string.Empty, string.Empty); 
            BsonDocument data = null;
            simulationPrediction.ProblemType = ProblemType;
            string[] columnsMain = CONSTANTS.MainColumns.Split(CONSTANTS.comma);
            var SimulationCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SimulationData);
            var builder = Builders<BsonDocument>.Filter;
            if (ProblemType == CONSTANTS.ADSP || ProblemType == CONSTANTS.RRP)
            {
                var filter2 = Builders<BsonDocument>.Filter.Eq(CONSTANTS.TemplateID, templateID);
                var projection = Builders<BsonDocument>.Projection.Include(columnsMain[0]).Include(columnsMain[1]).Include(columnsMain[2]).Include(columnsMain[3]).Include("TargetVariable").Include("SensitivityReport").Include(CONSTANTS.isDBEncryption).Include("FlagTeamSize").Exclude(CONSTANTS.Id);
                data = SimulationCollection.Find(filter2).Project<BsonDocument>(projection).FirstOrDefault();
                if (data != null)
                {
                    if (data.Contains(CONSTANTS.isDBEncryption))
                    {
                        if (data[CONSTANTS.isDBEncryption] == true)
                        {
                            simulationPrediction.Effort = JObject.Parse(appSettings.Value.IsAESKeyVault ? CryptographyUtility.Decrypt(data[columnsMain[0]].ToString()): AesProvider.Decrypt(data[columnsMain[0]].ToString(), appSettings.Value.aesKey, appSettings.Value.aesVector));
                            if (data.Contains(columnsMain[1]))
                                simulationPrediction.TeamSize = JObject.Parse(appSettings.Value.IsAESKeyVault ? CryptographyUtility.Decrypt(data[columnsMain[1]].ToString()): AesProvider.Decrypt(data[columnsMain[1]].ToString(), appSettings.Value.aesKey, appSettings.Value.aesVector));
                            simulationPrediction.Schedule = JObject.Parse(appSettings.Value.IsAESKeyVault ? CryptographyUtility.Decrypt(data[columnsMain[2]].ToString()): AesProvider.Decrypt(data[columnsMain[2]].ToString(), appSettings.Value.aesKey, appSettings.Value.aesVector));
                            simulationPrediction.Defect = JObject.Parse(appSettings.Value.IsAESKeyVault ? CryptographyUtility.Decrypt(data[columnsMain[3]].ToString()): AesProvider.Decrypt(data[columnsMain[3]].ToString(), appSettings.Value.aesKey, appSettings.Value.aesVector));
                            simulationPrediction.SensitivityReport = JObject.Parse(appSettings.Value.IsAESKeyVault ? CryptographyUtility.Decrypt(data["SensitivityReport"].ToString()): AesProvider.Decrypt(data["SensitivityReport"].ToString(), appSettings.Value.aesKey, appSettings.Value.aesVector));
                        }
                        else
                        {
                            simulationPrediction.Effort = JObject.Parse(data[columnsMain[0]].AsBsonDocument.ToString());
                            if (data.Contains(columnsMain[1]))
                                simulationPrediction.TeamSize = JObject.Parse(data[columnsMain[1]].AsBsonDocument.ToString());
                            simulationPrediction.Schedule = JObject.Parse(data[columnsMain[2]].AsBsonDocument.ToString());
                            simulationPrediction.Defect = JObject.Parse(data[columnsMain[3]].AsBsonDocument.ToString());
                            simulationPrediction.SensitivityReport = JObject.Parse(data["SensitivityReport"].AsBsonDocument.ToString());
                        }
                        simulationPrediction.TargetColumn = Convert.ToString(data["TargetVariable"]);
                        simulationPrediction.FlagTeamSize = Convert.ToDouble(data["FlagTeamSize"]);
                    }
                    else
                        simulationPrediction.ErrorMessage = "isDBEncryption attribute not present";

                }
            }
            else
            {
                var filter2 = builder.Eq(CONSTANTS.TemplateID, templateID);
                var genericProj2 = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
                data = SimulationCollection.Find(filter2).Project<BsonDocument>(genericProj2).FirstOrDefault();
                if (data != null)
                {
                    simulationPrediction.TargetColumn = Convert.ToString(data["TargetVariable"]);
                    if (data.Contains(CONSTANTS.isDBEncryption))
                    {
                        if (data[CONSTANTS.isDBEncryption] == true)
                        {
                            simulationPrediction.TargetColumnData = JObject.Parse(appSettings.Value.IsAESKeyVault ? CryptographyUtility.Decrypt(data[simulationPrediction.TargetColumn].ToString()): AesProvider.Decrypt(data[simulationPrediction.TargetColumn].ToString(), appSettings.Value.aesKey, appSettings.Value.aesVector));
                            simulationPrediction.SensitivityReport = JObject.Parse(appSettings.Value.IsAESKeyVault ? CryptographyUtility.Decrypt(data["SensitivityReport"].ToString()): AesProvider.Decrypt(data["SensitivityReport"].ToString(), appSettings.Value.aesKey, appSettings.Value.aesVector));
                        }
                        else
                        {
                            simulationPrediction.TargetColumnData = JObject.Parse(data[simulationPrediction.TargetColumn].AsBsonDocument.ToString());
                            simulationPrediction.SensitivityReport = JObject.Parse(data["SensitivityReport"].AsBsonDocument.ToString());
                        }
                    }
                    else
                        simulationPrediction.ErrorMessage = "isDBEncryption attribute not present";

                }
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(GetSimulatedData), CONSTANTS.END, string.Empty, string.Empty, string.Empty, string.Empty);
        }
        private void GetSimulationResults(string templateID, string simulationID, string ProblemType)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(GetSimulationResults), CONSTANTS.START, string.Empty, string.Empty, string.Empty, string.Empty);
            BsonDocument data = null;
            var SimulationCollection = _database.GetCollection<BsonDocument>(CONSTANTS.SimulationResults);
            var builder = Builders<BsonDocument>.Filter;
            if (ProblemType == CONSTANTS.ADSP || ProblemType == CONSTANTS.RRP)
            {
                if (!string.IsNullOrEmpty(simulationID) && simulationID != CONSTANTS.Null)
                {
                    var filter2 = builder.Eq(CONSTANTS.TemplateID, templateID) & builder.Eq(CONSTANTS.SimulationID, simulationID);
                    var projection = Builders<BsonDocument>.Projection.Include("Influencers").Include("TargetCertainty").Include("TemplateVersion").Include(CONSTANTS.SimulationID).Include("SimulationVersion").Include("TargetVariable").Include(CONSTANTS.Observation).Include("PercentChange").Include("IncrementFlags").Include(CONSTANTS.SelectedCurrentRelease).Include(CONSTANTS.isDBEncryption).Exclude(CONSTANTS.Id);
                    data = SimulationCollection.Find(filter2).Project<BsonDocument>(projection).FirstOrDefault();
                }
                else
                {
                    var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.TemplateID, templateID);
                    var projection = Builders<BsonDocument>.Projection.Include("Influencers").Include("TargetCertainty").Include("TemplateVersion").Include(CONSTANTS.SimulationID).Include("SimulationVersion").Include("TargetVariable").Include("PercentChange").Include("IncrementFlags").Include(CONSTANTS.SelectedCurrentRelease).Include(CONSTANTS.isDBEncryption).Exclude(CONSTANTS.Id);
                    data = SimulationCollection.Find(filter).Project<BsonDocument>(projection).FirstOrDefault();
                }
                if (data != null)
                {
                    string[] outliers = null;
                    simulationPrediction.TemplateID = templateID;
                    if (data.Contains("outliers"))
                    {
                        if (data["outliers"].ToString() == CONSTANTS.BsonNull)
                        {
                            simulationPrediction.Warning = null;
                            data["outliers"] = "";
                        }
                        if (!string.IsNullOrEmpty(data["outliers"].ToString()))
                        {
                            outliers = data["outliers"].AsBsonArray.Select(p => p.AsString).ToArray();
                        }
                    }
                    simulationPrediction.SimulationID = Convert.ToString(data[CONSTANTS.SimulationID]);
                    simulationPrediction.TemplateVersion = Convert.ToString(data["TemplateVersion"]);
                    simulationPrediction.SimulationVersion = Convert.ToString(data["SimulationVersion"]);
                    simulationPrediction.SelectedCurrentRelease = Convert.ToString(data[CONSTANTS.SelectedCurrentRelease]);
                    if (data[CONSTANTS.isDBEncryption] == true)
                    {
                        simulationPrediction.TargetVariable = Convert.ToDouble(appSettings.Value.IsAESKeyVault ? CryptographyUtility.Decrypt(data["TargetVariable"].ToString()): AesProvider.Decrypt(data["TargetVariable"].ToString(), appSettings.Value.aesKey, appSettings.Value.aesVector));
                        simulationPrediction.TargetCertainty = Convert.ToDouble(appSettings.Value.IsAESKeyVault ? CryptographyUtility.Decrypt(data["TargetCertainty"].ToString()): AesProvider.Decrypt(data["TargetCertainty"].ToString(), appSettings.Value.aesKey, appSettings.Value.aesVector));
                        if (data.Contains("Observation"))
                        {
                            string observation = appSettings.Value.IsAESKeyVault ? CryptographyUtility.Decrypt(data["Observation"].ToString()): AesProvider.Decrypt(data["Observation"].ToString(), appSettings.Value.aesKey, appSettings.Value.aesVector);
                            if (!string.IsNullOrEmpty(observation))
                                simulationPrediction.Observation = observation;
                        }
                        simulationPrediction.Influencers = JObject.Parse(appSettings.Value.IsAESKeyVault ? CryptographyUtility.Decrypt(data["Influencers"].ToString()): AesProvider.Decrypt(data["Influencers"].ToString(), appSettings.Value.aesKey, appSettings.Value.aesVector));
                        simulationPrediction.PercentChange = JObject.Parse(appSettings.Value.IsAESKeyVault ? CryptographyUtility.Decrypt(data["PercentChange"].ToString()): AesProvider.Decrypt(data["PercentChange"].ToString(), appSettings.Value.aesKey, appSettings.Value.aesVector));
                        simulationPrediction.IncrementFlags = JObject.Parse(appSettings.Value.IsAESKeyVault ? CryptographyUtility.Decrypt(data["IncrementFlags"].ToString()): AesProvider.Decrypt(data["IncrementFlags"].ToString(), appSettings.Value.aesKey, appSettings.Value.aesVector));
                    }
                    else
                    {
                        simulationPrediction.TargetVariable = Convert.ToDouble(data["TargetVariable"]);
                        simulationPrediction.TargetCertainty = Convert.ToDouble(data["TargetCertainty"]);
                        if (data.Contains("Observation"))
                        {
                            string observation = Convert.ToString(data["Observation"]);
                            if (!string.IsNullOrEmpty(observation))
                                simulationPrediction.Observation = observation;
                        }
                        simulationPrediction.Influencers = JObject.Parse(data["Influencers"].AsBsonDocument.ToString());
                        simulationPrediction.PercentChange = JObject.Parse(data["PercentChange"].ToString());
                        simulationPrediction.IncrementFlags = JObject.Parse(data["IncrementFlags"].ToString());
                    }
                    if (outliers != null)
                    {
                        if (outliers.Length > 0)
                        {
                            StringBuilder stringBuilder = new StringBuilder();
                            foreach (var item in outliers)
                            {
                                stringBuilder.Append("'" + item + "'" + ",");
                            }
                            if (outliers.Length == 1)
                            {
                                simulationPrediction.Warning = string.Format(CONSTANTS.WarningSingle, stringBuilder.ToString().Remove(stringBuilder.ToString().Length - 1, 1));
                            }
                            else
                            {
                                simulationPrediction.Warning = string.Format(CONSTANTS.WarningMultiple, stringBuilder.ToString().Remove(stringBuilder.ToString().Length - 1, 1));
                            }
                        }
                    }
                    if (simulationPrediction.ConvergenceAlert == CONSTANTS.BsonNull)
                        simulationPrediction.ConvergenceAlert = null;

                    if (!string.IsNullOrEmpty(simulationPrediction.ConvergenceAlert))
                    {
                        simulationPrediction.Status = CONSTANTS.E;
                        simulationPrediction.ErrorMessage = CONSTANTS.Converge;
                    }
                    else
                    {
                        simulationPrediction.ConvergenceAlert = null;
                    }
                    simulationPrediction.ProblemType = ProblemType;
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(simulationID) && simulationID != CONSTANTS.Null)
                {
                    var filter = builder.Eq(CONSTANTS.TemplateID, templateID) & builder.Eq(CONSTANTS.SimulationID, simulationID);
                    var genericProjection = Builders<BsonDocument>.Projection.Include("Influencers").Include("TopInfluencers").Include("TargetCertainty").Include("TemplateVersion").Include(CONSTANTS.SimulationID).Include("SimulationVersion").Include("TargetVariable").Include(CONSTANTS.Observation).Include("PercentChange").Include("IncrementFlags").Include(CONSTANTS.isDBEncryption).Exclude(CONSTANTS.Id);
                    data = SimulationCollection.Find(filter).Project<BsonDocument>(genericProjection).FirstOrDefault();
                }
                else
                {
                    var filter2 = Builders<BsonDocument>.Filter.Eq(CONSTANTS.TemplateID, templateID);
                    var genericProjection = Builders<BsonDocument>.Projection.Include("Influencers").Include("TopInfluencers").Include("TargetCertainty").Include("TemplateVersion").Include(CONSTANTS.SimulationID).Include("SimulationVersion").Include("TargetVariable").Include("PercentChange").Include("IncrementFlags").Include(CONSTANTS.isDBEncryption).Exclude(CONSTANTS.Id);
                    data = SimulationCollection.Find(filter2).Project<BsonDocument>(genericProjection).FirstOrDefault();
                }
                if (data != null)
                {
                    string[] outliers = null;
                    simulationPrediction.TemplateID = templateID;
                    if (data.Contains("outliers"))
                    {
                        if (data["outliers"].ToString() == CONSTANTS.BsonNull)
                        {
                            simulationPrediction.Warning = null;
                            data["outliers"] = "";
                        }
                        if (!string.IsNullOrEmpty(data["outliers"].ToString()))
                        {
                            outliers = data["outliers"].AsBsonArray.Select(p => p.AsString).ToArray();
                        }
                    }
                    simulationPrediction.SimulationID = Convert.ToString(data[CONSTANTS.SimulationID]);
                    simulationPrediction.TemplateVersion = Convert.ToString(data["TemplateVersion"]);
                    simulationPrediction.SimulationVersion = Convert.ToString(data["SimulationVersion"]);
                    if (data[CONSTANTS.isDBEncryption] == true)
                    {
                        simulationPrediction.TargetVariable = Convert.ToDouble(appSettings.Value.IsAESKeyVault ? CryptographyUtility.Decrypt(data["TargetVariable"].ToString()): AesProvider.Decrypt(data["TargetVariable"].ToString(), appSettings.Value.aesKey, appSettings.Value.aesVector));
                        simulationPrediction.TargetCertainty = Convert.ToDouble(appSettings.Value.IsAESKeyVault ? CryptographyUtility.Decrypt(data["TargetCertainty"].ToString()): AesProvider.Decrypt(data["TargetCertainty"].ToString(), appSettings.Value.aesKey, appSettings.Value.aesVector));
                        if (data.Contains("Observation"))
                        {
                            string observation = appSettings.Value.IsAESKeyVault ? CryptographyUtility.Decrypt(data["Observation"].ToString()): AesProvider.Decrypt(data["Observation"].ToString(), appSettings.Value.aesKey, appSettings.Value.aesVector);
                            if (!string.IsNullOrEmpty(observation))
                                simulationPrediction.Observation = observation;
                        }
                        simulationPrediction.Influencers = JObject.Parse(appSettings.Value.IsAESKeyVault ? CryptographyUtility.Decrypt(data["Influencers"].ToString()): AesProvider.Decrypt(data["Influencers"].ToString(), appSettings.Value.aesKey, appSettings.Value.aesVector));
                        simulationPrediction.PercentChange = JObject.Parse(appSettings.Value.IsAESKeyVault ? CryptographyUtility.Decrypt(data["PercentChange"].ToString()): AesProvider.Decrypt(data["PercentChange"].ToString(), appSettings.Value.aesKey, appSettings.Value.aesVector));
                        simulationPrediction.IncrementFlags = JObject.Parse(appSettings.Value.IsAESKeyVault ? CryptographyUtility.Decrypt(data["IncrementFlags"].ToString()): AesProvider.Decrypt(data["IncrementFlags"].ToString(), appSettings.Value.aesKey, appSettings.Value.aesVector));
                    }
                    else
                    {
                        simulationPrediction.TargetVariable = Convert.ToDouble(data["TargetVariable"]);
                        simulationPrediction.TargetCertainty = Convert.ToDouble(data["TargetCertainty"]);
                        if (data.Contains("Observation"))
                        {
                            string observation = Convert.ToString(data["Observation"]);
                            if (!string.IsNullOrEmpty(observation))
                                simulationPrediction.Observation = observation;
                        }
                        simulationPrediction.Influencers = JObject.Parse(data["Influencers"].AsBsonDocument.ToString());
                        simulationPrediction.PercentChange = JObject.Parse(data["PercentChange"].ToString());
                        simulationPrediction.IncrementFlags = JObject.Parse(data["IncrementFlags"].ToString());
                    }
                    if (outliers != null)
                    {
                        if (outliers.Length > 0)
                        {
                            StringBuilder stringBuilder = new StringBuilder();
                            foreach (var item in outliers)
                            {
                                stringBuilder.Append("'" + item + "'" + ",");
                            }
                            if (outliers.Length == 1)
                            {
                                simulationPrediction.Warning = string.Format(CONSTANTS.WarningSingle, stringBuilder.ToString().Remove(stringBuilder.ToString().Length - 1, 1));
                            }
                            else
                            {
                                simulationPrediction.Warning = string.Format(CONSTANTS.WarningMultiple, stringBuilder.ToString().Remove(stringBuilder.ToString().Length - 1, 1));
                            }
                        }
                    }
                    if (simulationPrediction.ConvergenceAlert == CONSTANTS.BsonNull)
                        simulationPrediction.ConvergenceAlert = null;

                    if (!string.IsNullOrEmpty(simulationPrediction.ConvergenceAlert))
                    {
                        simulationPrediction.Status = CONSTANTS.E;
                        simulationPrediction.ErrorMessage = CONSTANTS.Converge;
                    }
                    else
                    {
                        simulationPrediction.ConvergenceAlert = null;
                    }
                }
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(GetSimulationResults), CONSTANTS.START, string.Empty, string.Empty, string.Empty, string.Empty);
        }
        private void GetVersionsList(string ClientUID, string DCUID, string UseCaseID)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(GetVersionsList), CONSTANTS.START + "-USECASEID -" + UseCaseID, string.Empty, string.Empty, ClientUID, DCUID);
            List<TemplateVersion> templateVersions = new List<TemplateVersion>();
            List<SimulationVersion> simulationVersions = new List<SimulationVersion>();
            var Collection = _database.GetCollection<BsonDocument>(CONSTANTS.SimulationResults);
            var templateDataCollection = _database.GetCollection<BsonDocument>(CONSTANTS.TemplateData);
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq(CONSTANTS.ClientUID, ClientUID) & builder.Eq(CONSTANTS.DeliveryConstructUID, DCUID) & builder.Eq(CONSTANTS.UseCaseID, UseCaseID);
            var simulationProjection = Builders<BsonDocument>.Projection.Include(CONSTANTS.SimulationID).Include(CONSTANTS.SimulationVersion).Include(CONSTANTS.TemplateID).Exclude(CONSTANTS.Id);
            var templateProjection = Builders<BsonDocument>.Projection.Include(CONSTANTS.TemplateID).Include(CONSTANTS.VersionAttribute).Include(CONSTANTS.UseCaseName).Include(CONSTANTS.UseCaseDescription).Exclude(CONSTANTS.Id);
            var result = Collection.Find(filter).Project<BsonDocument>(simulationProjection).ToList();
            var templateResult = templateDataCollection.Find(filter).Project<BsonDocument>(templateProjection).ToList();
            if (templateResult.Count > 0)
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(GetVersionsList), CONSTANTS.START + "-TEMPLATE--RESULT--COUNT -" + templateResult.Count, string.Empty, string.Empty, ClientUID, DCUID);
                for (int i = 0; i < templateResult.Count; i++)
                {
                    TemplateVersion templateVersion = new TemplateVersion();
                    templateVersion.TemplateID = Convert.ToString(templateResult[i][CONSTANTS.TemplateID]);
                    templateVersion.Version = Convert.ToString(templateResult[i][CONSTANTS.VersionAttribute]);
                    templateVersions.Add(templateVersion);
                }
                simulationPrediction.TemplateVersions = templateVersions;
                templateInfo.TemplateVersions = templateVersions;
                genericFile.TemplateVersions = templateVersions;
                simulationPrediction.UseCaseName = templateResult[0][CONSTANTS.UseCaseName].ToString() != "bsonNull" ? null : templateResult[0][CONSTANTS.UseCaseName].ToString();
                simulationPrediction.UseCaseDescription = templateResult[0][CONSTANTS.UseCaseDescription].ToString() != "bsonNull" ? null : templateResult[0][CONSTANTS.UseCaseDescription].ToString();
                templateInfo.UseCaseName = templateResult[0][CONSTANTS.UseCaseName].ToString() != "bsonNull" ? null : templateResult[0][CONSTANTS.UseCaseName].ToString();
                templateInfo.UseCaseDescription = templateResult[0][CONSTANTS.UseCaseDescription].ToString() != "bsonNull" ? null : templateResult[0][CONSTANTS.UseCaseDescription].ToString();
                genericFile.UseCaseName = templateResult[0][CONSTANTS.UseCaseName].ToString() != "bsonNull" ? null : templateResult[0][CONSTANTS.UseCaseName].ToString();
                genericFile.UseCaseDescription = templateResult[0][CONSTANTS.UseCaseDescription].ToString() != "bsonNull" ? null : templateResult[0][CONSTANTS.UseCaseDescription].ToString();
                templateInfo.UseCaseID = UseCaseID;
                genericFile.UseCaseID = UseCaseID;
                simulationPrediction.UseCaseID = UseCaseID;
            }
            if (result.Count > 0)
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(GetVersionsList), CONSTANTS.START + "-SIMULATION--RESULT--COUNT -" + result.Count, string.Empty,string.Empty, ClientUID, DCUID);
                for (int i = 0; i < result.Count; i++)
                {
                    SimulationVersion simulationVersion = new SimulationVersion();
                    simulationVersion.SimulationID = Convert.ToString(result[i][CONSTANTS.SimulationID]);
                    simulationVersion.Version = Convert.ToString(result[i][CONSTANTS.SimulationVersion]);
                    simulationVersion.TemplateID = Convert.ToString(result[i][CONSTANTS.TemplateID]);
                    simulationVersions.Add(simulationVersion);
                }
            }

            simulationPrediction.SimulationVersions = simulationVersions;
            templateInfo.SimulationVersions = simulationVersions;
            genericFile.SimulationVersions = simulationVersions;
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(GetVersionsList), CONSTANTS.END, string.Empty,  string.Empty, ClientUID, DCUID);
        }
        private void GetVersionsList(string ClientUID, string DCUID, string UseCaseID, string UseCaseName)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(GetVersionsList), CONSTANTS.START + "-USECASEID -" + UseCaseID, string.Empty, string.Empty, ClientUID, DCUID);
            List<TemplateVersion> templateVersions = new List<TemplateVersion>();
            List<SimulationVersion> simulationVersions = new List<SimulationVersion>();
            var Collection = _database.GetCollection<BsonDocument>(CONSTANTS.SimulationResults);
            var templateDataCollection = _database.GetCollection<BsonDocument>(CONSTANTS.TemplateData);
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq(CONSTANTS.ClientUID, ClientUID) & builder.Eq(CONSTANTS.DeliveryConstructUID, DCUID) & builder.Eq(CONSTANTS.UseCaseName, UseCaseName);
            var simulationfilter = builder.Eq(CONSTANTS.ClientUID, ClientUID) & builder.Eq(CONSTANTS.DeliveryConstructUID, DCUID) & builder.Eq(CONSTANTS.UseCaseID, UseCaseID);
            var simulationProjection = Builders<BsonDocument>.Projection.Include(CONSTANTS.SimulationID).Include(CONSTANTS.SimulationVersion).Include(CONSTANTS.TemplateID).Exclude(CONSTANTS.Id);
            var templateProjection = Builders<BsonDocument>.Projection.Include(CONSTANTS.TemplateID).Include(CONSTANTS.VersionAttribute).Include(CONSTANTS.UseCaseName).Include(CONSTANTS.UseCaseDescription).Exclude(CONSTANTS.Id);
            var result = Collection.Find(simulationfilter).Project<BsonDocument>(simulationProjection).ToList();
            var templateResult = templateDataCollection.Find(filter).Project<BsonDocument>(templateProjection).ToList();
            if (templateResult.Count > 0)
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(GetVersionsList), CONSTANTS.START + "-TEMPLATE--RESULT--COUNT -" + templateResult.Count, string.Empty, string.Empty, ClientUID, DCUID);
                for (int i = 0; i < templateResult.Count; i++)
                {
                    TemplateVersion templateVersion = new TemplateVersion();
                    templateVersion.TemplateID = Convert.ToString(templateResult[i][CONSTANTS.TemplateID]);
                    templateVersion.Version = Convert.ToString(templateResult[i][CONSTANTS.VersionAttribute]);
                    templateVersions.Add(templateVersion);
                }
                simulationPrediction.TemplateVersions = templateVersions;
                templateInfo.TemplateVersions = templateVersions;
                genericFile.TemplateVersions = templateVersions;
                simulationPrediction.UseCaseName = UseCaseName;//templateResult[0][CONSTANTS.UseCaseName].ToString() != "bsonNull" ? null : templateResult[0][CONSTANTS.UseCaseName].ToString();
                simulationPrediction.UseCaseDescription = templateResult[0][CONSTANTS.UseCaseDescription].ToString() != "bsonNull" ? null : templateResult[0][CONSTANTS.UseCaseDescription].ToString();
                templateInfo.UseCaseName = UseCaseName;//templateResult[0][CONSTANTS.UseCaseName].ToString() != "bsonNull" ? null : templateResult[0][CONSTANTS.UseCaseName].ToString();
                templateInfo.UseCaseDescription = templateResult[0][CONSTANTS.UseCaseDescription].ToString() != "bsonNull" ? null : templateResult[0][CONSTANTS.UseCaseDescription].ToString();
                genericFile.UseCaseName = UseCaseName;//templateResult[0][CONSTANTS.UseCaseName].ToString() != "bsonNull" ? null : templateResult[0][CONSTANTS.UseCaseName].ToString();
                genericFile.UseCaseDescription = templateResult[0][CONSTANTS.UseCaseDescription].ToString() != "bsonNull" ? null : templateResult[0][CONSTANTS.UseCaseDescription].ToString();
                templateInfo.UseCaseID = UseCaseID;
                genericFile.UseCaseID = UseCaseID;
                simulationPrediction.UseCaseID = UseCaseID;
            }
            if (result.Count > 0)
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(GetVersionsList), CONSTANTS.START + "-SIMULATION--RESULT--COUNT -" + result.Count, string.Empty, string.Empty, ClientUID, DCUID);
                for (int i = 0; i < result.Count; i++)
                {
                    SimulationVersion simulationVersion = new SimulationVersion();
                    simulationVersion.SimulationID = Convert.ToString(result[i][CONSTANTS.SimulationID]);
                    simulationVersion.Version = Convert.ToString(result[i][CONSTANTS.SimulationVersion]);
                    simulationVersion.TemplateID = Convert.ToString(result[i][CONSTANTS.TemplateID]);
                    simulationVersions.Add(simulationVersion);
                }
            }

            simulationPrediction.SimulationVersions = simulationVersions;
            templateInfo.SimulationVersions = simulationVersions;
            genericFile.SimulationVersions = simulationVersions;
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(GetVersionsList), CONSTANTS.END, string.Empty,string.Empty, ClientUID, DCUID);
        }
        public whatIfPrediction GetwhatIfData(string templateID, string simulationID, JObject inputs)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(GetwhatIfData), CONSTANTS.START, string.Empty, string.Empty, string.Empty, string.Empty);
            HttpResponseMessage httpResponse = null;
            string pythonResult = string.Empty;
            //pythonCall
            httpResponse = GetwhatIfResult(templateID, simulationID, inputs);
            if (httpResponse.StatusCode.ToString() != "InternalServerError" && httpResponse.StatusCode.ToString() != "BadGateway")
            {
                if (httpResponse.StatusCode != HttpStatusCode.InternalServerError)
                {
                    var response = JObject.Parse(httpResponse.Content.ReadAsStringAsync().Result);
                    if (httpResponse == null)
                    {
                        whatIfPrediction.ErrorMessage = "Token Generation Error";
                        whatIfPrediction.Status = CONSTANTS.E;
                        return whatIfPrediction;
                    }

                    if (response != null && response.ContainsKey(CONSTANTS.Error))
                    {
                        whatIfPrediction.ErrorMessage = "Authorization Fail or Python Error " + httpResponse.StatusCode;
                        whatIfPrediction.Status = CONSTANTS.E;
                        return whatIfPrediction;
                    }

                    if (httpResponse.StatusCode == HttpStatusCode.InternalServerError || httpResponse.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        whatIfPrediction.ErrorMessage = "Authorization Fail or Python Error " + httpResponse.StatusCode;
                        whatIfPrediction.Status = CONSTANTS.E;
                        return whatIfPrediction;
                    }

                    if (httpResponse.StatusCode == HttpStatusCode.OK)
                    {
                        whatIfPrediction.Influencers = JObject.Parse(response[CONSTANTS.Influencers].ToString());
                        whatIfPrediction.TargetCertainty = Convert.ToDouble(response[CONSTANTS.TargetCertainty]);
                        whatIfPrediction.TargetVariable = Convert.ToDouble(response[CONSTANTS.TargetVariable]);
                        whatIfPrediction.PercentChange = JObject.Parse(response["PercentChange"].ToString());
                        whatIfPrediction.IncrementFlags = JObject.Parse(response["IncrementFlags"].ToString());
                        whatIfPrediction.TargetColumn = response[CONSTANTS.TargetColumn].ToString();
                        whatIfPrediction.Observation = response[CONSTANTS.Observation].ToString();
                        whatIfPrediction.Status = CONSTANTS.C;
                    }
                    else
                    {
                        whatIfPrediction.ErrorMessage = httpResponse.StatusCode.ToString();
                        whatIfPrediction.Status = CONSTANTS.E;
                    }
                }
                else
                {
                    whatIfPrediction.ErrorMessage = "Python Error: " + httpResponse.StatusCode;
                    whatIfPrediction.Status = CONSTANTS.E;
                    return whatIfPrediction;
                }
            }
            else
            {
                whatIfPrediction.ErrorMessage = "Python Error " + httpResponse.StatusCode;
                whatIfPrediction.Status = CONSTANTS.E;
                return whatIfPrediction;
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(GetwhatIfData), CONSTANTS.END, string.Empty, string.Empty, string.Empty, string.Empty);
            return whatIfPrediction;
        }
        public string UpdateSimulation(string templateID, string simulationID, string UseCaseID, JObject influencers, double targetCertainty, double targetVariable, string Observation, JObject IncrementFlags, JObject PercentChange, string SelectedCurrentRelease)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(UpdateSimulation), CONSTANTS.START, string.Empty, string.Empty, string.Empty, string.Empty);
            string resultData = string.Empty;
            try
            {
                var simulationResult = _database.GetCollection<BsonDocument>(CONSTANTS.SimulationResults);
                var builder = Builders<BsonDocument>.Filter;
                var filter = builder.Eq(CONSTANTS.TemplateID, templateID) & builder.Eq(CONSTANTS.SimulationID, simulationID) & builder.Eq(CONSTANTS.UseCaseID, UseCaseID);
                //adding encrypt check
                var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.isDBEncryption).Exclude(CONSTANTS.Id);
                BsonDocument result = simulationResult.Find(filter).Project<BsonDocument>(projection).FirstOrDefault();

                if (result[CONSTANTS.isDBEncryption] == true)
                {
                    //BsonDocument doc = BsonDocument.Parse(CryptographyUtility.Encrypt(influencers.ToString()));
                    var influencersUpdate = Builders<BsonDocument>.Update.Set(CONSTANTS.Influencers,(appSettings.Value.IsAESKeyVault ? CryptographyUtility.Encrypt(influencers.ToString()): AesProvider.Encrypt(influencers.ToString(), appSettings.Value.aesKey, appSettings.Value.aesVector)));
                    simulationResult.UpdateOne(filter, influencersUpdate);
                    var targetCertainUpdate = Builders<BsonDocument>.Update.Set(CONSTANTS.TargetCertainty, appSettings.Value.IsAESKeyVault ? CryptographyUtility.Encrypt(targetCertainty.ToString()): AesProvider.Encrypt(targetCertainty.ToString(), appSettings.Value.aesKey, appSettings.Value.aesVector));
                    simulationResult.UpdateOne(filter, targetCertainUpdate);
                    var targetVarUpdate = Builders<BsonDocument>.Update.Set(CONSTANTS.TargetVariable, appSettings.Value.IsAESKeyVault ? CryptographyUtility.Encrypt(targetVariable.ToString()): AesProvider.Encrypt(targetVariable.ToString(), appSettings.Value.aesKey, appSettings.Value.aesVector));
                    simulationResult.UpdateOne(filter, targetVarUpdate);
                    var Observation_update = Builders<BsonDocument>.Update.Set(CONSTANTS.Observation, appSettings.Value.IsAESKeyVault ? CryptographyUtility.Encrypt(Observation): AesProvider.Encrypt(Observation, appSettings.Value.aesKey, appSettings.Value.aesVector));
                    simulationResult.UpdateOne(filter, Observation_update);
                    var flagsUpdate = Builders<BsonDocument>.Update.Set(CONSTANTS.IncrementFlags, appSettings.Value.IsAESKeyVault ? CryptographyUtility.Encrypt(IncrementFlags.ToString()): AesProvider.Encrypt(IncrementFlags.ToString(), appSettings.Value.aesKey, appSettings.Value.aesVector));
                    simulationResult.UpdateOne(filter, flagsUpdate);
                    var percentUpdate = Builders<BsonDocument>.Update.Set(CONSTANTS.PercentChange, appSettings.Value.IsAESKeyVault ? CryptographyUtility.Encrypt(PercentChange.ToString()): AesProvider.Encrypt(PercentChange.ToString(), appSettings.Value.aesKey, appSettings.Value.aesVector));
                    simulationResult.UpdateOne(filter, percentUpdate);
                }
                else
                {
                    //BsonDocument doc = BsonDocument.Parse(influencers.ToString());
                    var influencersUpdate = Builders<BsonDocument>.Update.Set(CONSTANTS.Influencers, influencers.ToString());
                    simulationResult.UpdateOne(filter, influencersUpdate);
                    var targetCertainUpdate = Builders<BsonDocument>.Update.Set(CONSTANTS.TargetCertainty, targetCertainty);
                    simulationResult.UpdateOne(filter, targetCertainUpdate);
                    var targetVarUpdate = Builders<BsonDocument>.Update.Set(CONSTANTS.TargetVariable, targetVariable);
                    simulationResult.UpdateOne(filter, targetVarUpdate);
                    var Observation_update = Builders<BsonDocument>.Update.Set(CONSTANTS.Observation, Observation);
                    simulationResult.UpdateOne(filter, Observation_update);
                    var flagsUpdate = Builders<BsonDocument>.Update.Set(CONSTANTS.IncrementFlags, IncrementFlags);
                    simulationResult.UpdateOne(filter, flagsUpdate);
                    var percentUpdate = Builders<BsonDocument>.Update.Set(CONSTANTS.PercentChange, PercentChange);
                    simulationResult.UpdateOne(filter, percentUpdate);
                }
                string modifiedOnDate = DateTime.Now.ToString(CONSTANTS.DateHoursFormat);
                var modifiedOnUpdate = Builders<BsonDocument>.Update.Set(CONSTANTS.ModifiedOn, modifiedOnDate);
                simulationResult.UpdateOne(filter, modifiedOnUpdate);
                var selectedRelease_update = Builders<BsonDocument>.Update.Set(CONSTANTS.SelectedCurrentRelease, SelectedCurrentRelease);
                simulationResult.UpdateOne(filter, selectedRelease_update);
                return resultData = CONSTANTS.Success;
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(MCSimulationService), nameof(UpdateSimulation), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                resultData = ex.Message;
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(UpdateSimulation), CONSTANTS.END, string.IsNullOrEmpty(templateID) ? default(Guid) : new Guid(templateID), string.Empty, string.Empty, string.Empty, string.Empty);
            return resultData;
        }

        private long GetVersionCount()
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(GetVersionCount), CONSTANTS.START, string.Empty, string.Empty, string.Empty, string.Empty);
            long count = 0;
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.VersionCounter);
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq(CONSTANTS.ClientUID, templateData.ClientUID) & builder.Eq(CONSTANTS.DeliveryConstructUID, templateData.DeliveryConstructUID);
            //var result = collection.Find(new BsonDocument()).ToList();
            var result = collection.Find(filter).ToList();
            VersionCounter versionCounter = new VersionCounter();
            versionCounter.VersionCount = CONSTANTS.VersionCount;
            versionCounter.ClientUID = templateData.ClientUID;
            versionCounter.DeliveryConstructUID = templateData.DeliveryConstructUID;
            versionCounter.UseCaseName = templateData.UseCaseName;
            versionCounter.UseCaseID = templateData.UseCaseID;
            if (result.Count == 0)
            {
                versionCounter.Seq = 1;
                var inputQueue = JsonConvert.SerializeObject(versionCounter);
                var insertInputQueue1 = BsonSerializer.Deserialize<BsonDocument>(inputQueue);
                collection.InsertOne(insertInputQueue1);
                count = versionCounter.Seq;
            }
            else
                count = getNextSequence(versionCounter.VersionCount);
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(GetVersionCount), CONSTANTS.END, string.Empty, string.Empty, string.Empty, string.Empty);
            return count;
        }

        private long getNextSequence(string name)
        {
            long seq = 0;
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.VersionCounter);
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq("VersionCount", name) & builder.Eq(CONSTANTS.ClientUID, templateData.ClientUID) & builder.Eq(CONSTANTS.DeliveryConstructUID, templateData.DeliveryConstructUID);
            //var filter = Builders<BsonDocument>.Filter.Eq("VersionCount", name);
            var update = Builders<BsonDocument>.Update.Inc("Seq", 1);
            var result = collection.FindOneAndUpdate(filter, update, new FindOneAndUpdateOptions<BsonDocument> { IsUpsert = true, ReturnDocument = ReturnDocument.After }).Elements.ToArray();
            seq = Convert.ToInt64(result[2].Value);
            return seq;
        }

        public string Incremented_VersionName()
        {
            long versionCount = GetVersionCount();
            string VersionName = CONSTANTS.Version + versionCount;
            return VersionName;
        }

        public UseCaseModelsList GetUseCaseData(string ClientUID, string DCUID, string DeliveryTypeName, string UserID)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(GetUseCaseData), CONSTANTS.START, string.Empty, string.Empty, ClientUID, DCUID);
            string inputData = "ClientUID=" + ClientUID + ", " + "DCUID=" + DCUID + ", " + "DeliveryTypeName=" + DeliveryTypeName + ", " + "UserID=" + UserID;
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(GetUseCaseData), "InputData: " + inputData, string.Empty, string.Empty, ClientUID, DCUID);
            UseCaseModelsList useCaseModelsList = new UseCaseModelsList();
            List<VDSUseCaseModels> UseCaseList = new List<VDSUseCaseModels>();
            try
            {
                string value = null;
                var collection = _database.GetCollection<VDSUseCaseDBModels>(CONSTANTS.TemplateData);
                var builder = Builders<VDSUseCaseDBModels>.Filter;
                var projection = Builders<VDSUseCaseDBModels>.Projection.Exclude(CONSTANTS.Id);
                var filter = builder.Eq(CONSTANTS.ClientUID, ClientUID) & builder.Eq(CONSTANTS.DeliveryConstructUID, DCUID) & builder.Eq(CONSTANTS.DeliveryTypeName, DeliveryTypeName) & builder.Ne(CONSTANTS.UseCaseName, BsonNull.Value) & builder.Ne(CONSTANTS.UseCaseName, string.Empty) & builder.Ne(CONSTANTS.UseCaseName, value);
                var result = collection.Find(filter).Project<VDSUseCaseDBModels>(projection).ToList();
                if (result.Count > 0)
                {
                    //List<VDSUseCaseDBModels> distinctModels = result.GroupBy(name => name.ProblemType).Select(x=>x.OrderByDescending(y=>y.CreatedOn)).Select(g => g.First()).ToList();
                    List<VDSUseCaseDBModels> distinctModels2 = result.OrderByDescending(y => y.CreatedOn).ToList();
                    var lstRRPADSPProblemType = result.Where(x => (x.ProblemType.Equals(CONSTANTS.RRP) || x.ProblemType.Equals(CONSTANTS.ADSP)) && x.Status != CONSTANTS.E).OrderByDescending(y => y.CreatedOn).ToList();
                    var genericProblemType = result.Where(x => (x.ProblemType.Equals(CONSTANTS.Generic))).GroupBy(name => name.UseCaseName).Select(x => x.OrderByDescending(y => y.CreatedOn)).Select(g => g.First()).ToList();
                    //var genericProblemType = result.Where(x => !(x.ProblemType.Equals(CONSTANTS.RRP) || x.ProblemType.Equals(CONSTANTS.ADSP))).ToList();
                    List<VDSUseCaseDBModels> distinctModels = lstRRPADSPProblemType.Concat(genericProblemType).ToList();

                    foreach (var item in distinctModels)
                    {
                        VDSUseCaseModels useCaseModels = new VDSUseCaseModels();
                        useCaseModels.TemplateVersion = item.Version;
                        useCaseModels.TemplateID = item.TemplateID;
                        useCaseModels.UserID = item.CreatedByUser;
                        useCaseModels.ClientUID = item.ClientUID;
                        useCaseModels.DeliveryConstructUID = item.DeliveryConstructUID;
                        useCaseModels.DeliveryTypeID = item.DeliveryTypeID;
                        useCaseModels.DeliveryTypeName = item.DeliveryTypeName;
                        useCaseModels.UseCaseID = item.UseCaseID;
                        //useCaseModels.UseCaseDescription = item.UseCaseDescription;
                        useCaseModels.IsSimulationExist = this.CheckSimulationExists(item.TemplateID);
                        if (item.ProblemType == CONSTANTS.RRP || item.ProblemType == CONSTANTS.ADSP)
                        {
                            useCaseModels.ProblemType = CONSTANTS.RRP;
                            useCaseModels.UseCaseName = CONSTANTS.RRP;
                            useCaseModels.UseCaseDescription = CONSTANTS.RRPUseCaseDescription;
                        }
                        else
                        {
                            useCaseModels.ProblemType = item.ProblemType;
                            useCaseModels.UseCaseName = item.UseCaseName;
                            useCaseModels.UseCaseDescription = item.UseCaseDescription;
                        }
                        if (UserID == useCaseModels.UserID)
                            useCaseModels.IsLoggedInUser = true;
                        UseCaseList.Add(useCaseModels);
                    }
                    useCaseModelsList.UseCaseModels = UseCaseList;
                    useCaseModelsList.Status = CONSTANTS.C;
                    useCaseModelsList.Message = CONSTANTS.success;
                }
                else
                {
                    useCaseModelsList.Status = CONSTANTS.C;
                    useCaseModelsList.Message = "No UseCaseModels found";
                }
            }
            catch (Exception ex)
            {
                useCaseModelsList.Status = CONSTANTS.E;
                useCaseModelsList.ErrorMessage = ex.Message + "--STACKTRACE--" + ex.StackTrace;
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(MCSimulationService), nameof(GetUseCaseData), ex.Message + "-STACKTRACE-" + ex.StackTrace, ex, string.Empty, string.Empty, ClientUID, DCUID);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(GetUseCaseData), CONSTANTS.END, string.Empty,  string.Empty, ClientUID, DCUID);
            return useCaseModelsList;
        }

        public DeletedUseCaseDetails DeletedUseCase(string ClientUID, string DCUID, string UseCaseID, string UserID)
        {
            string encryptedUser = UserID;
            if (!string.IsNullOrEmpty(Convert.ToString(UserID)))
                encryptedUser = appSettings.Value.IsAESKeyVault ? CryptographyUtility.Encrypt(Convert.ToString(UserID)): AesProvider.Encrypt(Convert.ToString(UserID), appSettings.Value.aesKey, appSettings.Value.aesVector);

            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(DeletedUseCase), CONSTANTS.START, string.Empty, string.Empty, ClientUID, DCUID);
            DeletedUseCaseDetails deletedUseCaseDetails = new DeletedUseCaseDetails();
            try
            {
                var collection1 = _database.GetCollection<BsonDocument>(CONSTANTS.TemplateData);
                var collection2 = _database.GetCollection<BsonDocument>(CONSTANTS.SimulationResults);
                var collection3 = _database.GetCollection<BsonDocument>(CONSTANTS.SimulationData);
                var builder = Builders<BsonDocument>.Filter;
                var filter = builder.Eq(CONSTANTS.ClientUID, ClientUID) & builder.Eq(CONSTANTS.DeliveryConstructUID, DCUID) & builder.Eq(CONSTANTS.UseCaseID, UseCaseID) & (builder.Eq(CONSTANTS.CreatedByUser, UserID) | builder.Eq(CONSTANTS.CreatedByUser, encryptedUser));
                var result1 = collection1.DeleteMany(filter);
                if (result1.DeletedCount > 0)
                {
                    var result2 = collection2.DeleteMany(filter);
                    var result3 = collection3.DeleteMany(filter);
                    deletedUseCaseDetails.UseCaseID = UseCaseID;
                    deletedUseCaseDetails.Status = CONSTANTS.C;
                    deletedUseCaseDetails.Message = CONSTANTS.success;
                }
            }
            catch (Exception ex)
            {
                deletedUseCaseDetails.UseCaseID = UseCaseID;
                deletedUseCaseDetails.Status = CONSTANTS.E;
                deletedUseCaseDetails.ErrorMessage = ex.Message + "-STACKTRACE-" + ex.StackTrace;
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(MCSimulationService), nameof(DeletedUseCase), ex.Message, ex, string.Empty,  string.Empty, ClientUID, DCUID);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(DeletedUseCase), CONSTANTS.END, string.Empty, string.Empty, ClientUID, DCUID);
            return deletedUseCaseDetails;
        }

        public bool IsUseCaseUnique(string ClientUID, string DCUID, string usecasename)
        {
            bool isUseCaseExists = false;
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.TemplateData);
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq(CONSTANTS.ClientUID, ClientUID) & builder.Eq(CONSTANTS.DeliveryConstructUID, DCUID) & builder.Eq(CONSTANTS.UseCaseName, usecasename);
            var result = collection.Find(filter).ToList();
            if (result.Count > 0)
            {
                isUseCaseExists = true;
            }
            return isUseCaseExists;
        }

        public void UpdateSelection(string templateID, string userId, string useCaseID, JObject selectionUpdate, string clientUID, string deliveryConstructUID, string useCaseName, string selection)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(UpdateSelection), CONSTANTS.START, string.Empty, string.Empty, clientUID, deliveryConstructUID);
            try
            {
                var collection = _database.GetCollection<BsonDocument>(CONSTANTS.TemplateData);
                var builder = Builders<BsonDocument>.Filter;
                var filter = builder.Eq(CONSTANTS.TemplateID, templateID) & builder.Eq(CONSTANTS.UseCaseID, useCaseID) & builder.Eq(CONSTANTS.ClientUID, clientUID) & builder.Eq(CONSTANTS.DeliveryConstructUID, deliveryConstructUID) & builder.Eq(CONSTANTS.UseCaseName, useCaseName);
                var projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
                var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();
                if (selection == "Influencer")
                {
                    var updateVersion = Builders<BsonDocument>.Update.Set(CONSTANTS.MainSelection, BsonDocument.Parse(selectionUpdate.ToString()));
                    collection.UpdateOne(filter, updateVersion);
                }
                else if (selection == "Phase")
                {
                    var updateVersion = Builders<BsonDocument>.Update.Set(CONSTANTS.InputSelection, BsonDocument.Parse(selectionUpdate.ToString()));
                    collection.UpdateOne(filter, updateVersion);
                }
            }
            catch (Exception ex)
            { LOGGING.LogManager.Logger.LogErrorMessage(typeof(MCSimulationService), nameof(UpdateSelection), ex.Message, ex, string.Empty,  string.Empty, clientUID, deliveryConstructUID); }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(UpdateSelection), CONSTANTS.END, string.IsNullOrEmpty(templateID) ? default(Guid) : new Guid(templateID), string.Empty, string.Empty, clientUID, deliveryConstructUID);
        }

        public async Task<WeeklyPrediction> WeeklySimulation(string clientUID, string dCUID, string userId, string useCaseID, string useCaseName)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(WeeklySimulation), CONSTANTS.START, string.Empty, string.Empty, clientUID,dCUID);

            //get base version id
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.TemplateData);
            var builder = Builders<BsonDocument>.Filter;
            var projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
            var filterTemplate = builder.Eq(CONSTANTS.ClientUID, clientUID) & builder.Eq(CONSTANTS.DeliveryConstructUID, dCUID) & builder.Eq(CONSTANTS.VersionAttribute, CONSTANTS.Base_Version);
            var inputData = collection.Find(filterTemplate).Project<BsonDocument>(projection).FirstOrDefault();
            if (inputData != null)
            {

                HttpResponseMessage httpResponse = null;
                string pythonResult = string.Empty;
                //update db
                var filter = builder.Eq(CONSTANTS.TemplateID, inputData[CONSTANTS.TemplateID].ToString());
                var status_update = Builders<BsonDocument>.Update.Set(CONSTANTS.Status, "");
                var progress_update = Builders<BsonDocument>.Update.Set(CONSTANTS.Progress, "");
                var message_update = Builders<BsonDocument>.Update.Set(CONSTANTS.Message, "Weekly simulating");
                collection.UpdateOne(filter, status_update);
                collection.UpdateOne(filter, progress_update);
                collection.UpdateOne(filter, message_update);
                //pythonCall
                httpResponse = GetRRPResult(inputData[CONSTANTS.TemplateID].ToString(), CONSTANTS.Base_Version, clientUID, dCUID);

                if (httpResponse.StatusCode.ToString() != "InternalServerError" && httpResponse.StatusCode.ToString() != "BadGateway")
                {
                    if (httpResponse.StatusCode != HttpStatusCode.InternalServerError)
                    {
                        var response = JObject.Parse(httpResponse.Content.ReadAsStringAsync().Result);
                        if (httpResponse == null)
                        {
                            weeklyPrediction.ErrorMessage = "Token Generation Error";
                            weeklyPrediction.Status = CONSTANTS.E;
                            return weeklyPrediction;
                        }

                        if (response != null && response.ContainsKey(CONSTANTS.Error))
                        {
                            weeklyPrediction.ErrorMessage = "Authorization Fail or Python Error " + httpResponse.StatusCode;
                            weeklyPrediction.Status = CONSTANTS.E;
                            return weeklyPrediction;
                        }

                        if (httpResponse.StatusCode == HttpStatusCode.InternalServerError || httpResponse.StatusCode == HttpStatusCode.Unauthorized)
                        {
                            weeklyPrediction.ErrorMessage = "Authorization Fail or Python Error " + httpResponse.StatusCode;
                            weeklyPrediction.Status = CONSTANTS.E;
                            return weeklyPrediction;
                        }

                        if (httpResponse.StatusCode == HttpStatusCode.OK)
                        {
                            JObject pyResponse = JsonConvert.DeserializeObject<JObject>(httpResponse.Content.ReadAsStringAsync().Result);
                            var templateProjection = Builders<BsonDocument>.Projection.Include(CONSTANTS.Status).Include(CONSTANTS.Progress).Include(CONSTANTS.Message).Exclude(CONSTANTS.Id);
                            var templateResult = collection.Find(filter).Project<BsonDocument>(templateProjection).ToList();
                            if (templateResult.Count > 0)
                            {
                                weeklyPrediction.Status = templateResult[0][CONSTANTS.Status].ToString();
                                weeklyPrediction.Progress = templateResult[0][CONSTANTS.Progress].ToString();
                                weeklyPrediction.Message = templateResult[0][CONSTANTS.Message].ToString();

                            }
                        }
                        else
                        {
                            weeklyPrediction.ErrorMessage = httpResponse.StatusCode.ToString();
                            weeklyPrediction.Status = CONSTANTS.E;
                        }
                    }
                    else
                    {
                        weeklyPrediction.ErrorMessage = "Python Error: " + httpResponse.StatusCode;
                        weeklyPrediction.Status = CONSTANTS.E;
                        return weeklyPrediction;
                    }
                }
                else
                {
                    weeklyPrediction.ErrorMessage = "Python Error " + httpResponse.StatusCode;
                    weeklyPrediction.Status = CONSTANTS.E;
                    return weeklyPrediction;
                }
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(MCSimulationService), nameof(WeeklySimulation), CONSTANTS.END, string.Empty, string.Empty, clientUID,dCUID);
            return weeklyPrediction;
        }

        public async Task<bool> WeeklySimulationCounter(bool firstCall)
        {
            //await Task.Delay(TimeSpan.FromMinutes(30));
            await Task.Delay(TimeSpan.FromDays(7));
            return false;
        }

        private void UpdateSelectedCurrentRelease(string TemplateId, string SelectedCurrentRelease)
        {
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.TemplateData);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.TemplateID, TemplateId);
            var projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
            var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (result.Count > 0)
            {
                var currentUpdate = Builders<BsonDocument>.Update.Set(CONSTANTS.SelectedCurrentRelease, SelectedCurrentRelease);
                collection.UpdateOne(filter, currentUpdate);
            }
        }

        #endregion

    }
}
