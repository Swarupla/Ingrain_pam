using System;
using System.Collections.Generic;
using System.Text;
using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
using Accenture.MyWizard.Ingrain.DataAccess;
using Accenture.MyWizard.Shared.Helpers;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using MongoDB.Bson;
using System.Threading;
using MongoDB.Bson.Serialization;
using Newtonsoft.Json.Linq;
using Accenture.MyWizard.Ingrain.DataModels.InferenceEngineModel;
using Accenture.MyWizard.Cryptography.EncryptionProviders;
using Accenture.MyWizard.Ingrain.BusinessDomain.Common;
using Accenture.MyWizard.Ingrain.DataModels.Models;
//using Accenture.MyWizard.Ingrain.DataModels.Models;

namespace Accenture.MyWizard.Ingrain.BusinessDomain.Services
{
    public class InferenceService : IInferenceService
    {
        private readonly IOptions<IngrainAppSettings> appSettings;
        private IERequestQueue ingrainRequest;
        IEFilepath _filepath = null;
        IEParentFile parentFile = null;
        IEFileUpload fileUpload = null;
        private IEncryptionDecryption _encryptionDecryption;

        private IInferenceEngineDBContext _inferenceEngineDBContext;
        public List<string> IEGenricVDSUsecases { get; set; }
        public InferenceService(IOptions<IngrainAppSettings> settings, IServiceProvider serviceProvider, IInferenceEngineDBContext inferenceEngineDBContext)
        {
            appSettings = settings;
            _encryptionDecryption = serviceProvider.GetService<IEncryptionDecryption>();
            _inferenceEngineDBContext = inferenceEngineDBContext;
            IEGenricVDSUsecases = appSettings.Value.IEGenricVDSUsecases;
        }

        public string IEUploadFiles(string CorrelationId, string ModelName, string userId, string clientUID, string deliveryUID, string ParentFileName, string MappingFlag, string Source, string Category, Type type, string E2EUID, HttpContext httpContext, string ClusterFlag, string EntityStartDate, string EntityEndDate, bool DBEncryption, string oldCorrelationID, string Language, bool IsModelTemplateDataSource, string CorrelationId_status, out string requestQueueStatus, string usecaseId, string UploadFileType)
        {
            bool encryptDB = false;

            if (appSettings.Value.isForAllData == true)
            {
                if (appSettings.Value.DBEncryption == true)
                {
                    encryptDB = true;
                }
                else
                {
                    encryptDB = false;
                }
            }
            else
            {
                if (appSettings.Value.DBEncryption == true && DBEncryption == true)
                {

                    encryptDB = true;
                }
                else
                {
                    encryptDB = false;
                }

            }

            encryptDB = appSettings.Value.isForAllData;
            string MappingColumns = string.Empty;
            string filePath = string.Empty;
            string category = string.Empty;
            string sourceName = string.Empty;
            string startDate = string.Empty;
            string endDate = string.Empty;

            bool flag = true;
            int counter = 0;
            string ParentFileNamePath = string.Empty, FilePath = string.Empty, postedFileName = string.Empty, DataSourceFilePath = string.Empty, FileName = string.Empty, SaveFileName = string.Empty;

            requestQueueStatus = string.Empty;
            if (CorrelationId_status == "" || CorrelationId_status == "undefined")
            {
                var fileCollection = httpContext.Request.Form.Files;
                string correlationId = CorrelationId;
                string dataSource = string.Empty;
                string Entities = string.Empty, Metrices = string.Empty, InstaML = string.Empty, Entity_Names = string.Empty, Metric_Names = string.Empty, Customdata = string.Empty;
                if (!string.IsNullOrEmpty(ModelName) && !string.IsNullOrEmpty(deliveryUID) && !string.IsNullOrEmpty(clientUID))
                {

                    filePath = appSettings.Value.UploadFilePath;
                    Directory.CreateDirectory(Path.Combine(filePath, appSettings.Value.SavedModels));
                    filePath = System.IO.Path.Combine(filePath, appSettings.Value.AppData);
                    System.IO.Directory.CreateDirectory(filePath);

                    IFormCollection collection = httpContext.Request.Form;
                    var entityitems = collection[CONSTANTS.pad];
                    var metricitems = collection[CONSTANTS.metrics];
                    var InstaMl = collection[CONSTANTS.InstaMl];
                    var EntitiesNames = collection[CONSTANTS.EntitiesName];
                    var MetricsNames = collection[CONSTANTS.MetricNames];
                    var Customdetails = collection["Custom"];
                    var dataSetUId = collection["DataSetUId"];
                    var maxDataPull = collection["MaxDataPull"];
                    int MaxDataPull = maxDataPull != CONSTANTS.Null ? Convert.ToInt32(maxDataPull) : 0;
                    var fileEntityName = collection["FileEntityName"];
                    dataSource = fileEntityName;
                    if (dataSetUId == "undefined" || dataSetUId == "null")
                        dataSetUId = string.Empty;

                    if (Customdetails.Count() > 0)
                    {
                        foreach (var item in Customdetails)
                        {
                            if (item != "{}")
                            {
                                Customdata += item;
                                if(!appSettings.Value.Environment.Equals(CONSTANTS.PADEnvironment))
                                    dataSource = "Custom";
                            }
                            else
                                Customdata = CONSTANTS.Null;
                        }
                    }

                    if (entityitems.Count() > 0)
                    {
                        foreach (var item in entityitems)
                        {
                            if (item != "{}")
                            {
                                Entities += item;
                                dataSource = "Entity";
                            }
                            else
                                Entities = CONSTANTS.Null;
                        }
                    }

                    if (metricitems.Count() > 0)
                    {
                        foreach (var item in metricitems)
                        {
                            if (item != "{}")
                            {
                                Metrices += item;
                            }
                            else
                                Metrices = CONSTANTS.Null;
                        }
                    }

                    if (InstaMl.Count() > 0)
                    {
                        foreach (var item in InstaMl)
                        {
                            if (item != "{}")
                            {
                                InstaML += item;
                                DataSourceFilePath += "InstaMl,";
                            }
                            else
                                InstaML = CONSTANTS.Null;
                        }
                    }
                    if (EntitiesNames.Count > 0)
                    {
                        foreach (var item in EntitiesNames)
                        {
                            if (item != "{}")
                            {
                                Entity_Names += item;
                                DataSourceFilePath = Entity_Names + ",";
                            }
                        }
                    }
                    if (MetricsNames.Count() > 0)
                    {
                        foreach (var item in MetricsNames)
                        {
                            if (item != "{}")
                            {
                                Metric_Names += item;
                                DataSourceFilePath += Metric_Names + ",";
                            }
                        }
                    }

                    // _ingestedData = new IngestedDataDTO();
                    //_ingestedData.CorrelationId = correlationId;
                    if (fileCollection.Count != 0)
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(IngestedDataService), nameof(IEUploadFiles), "fileCollection START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, clientUID, deliveryUID); //TODO: Remove log
                        for (int i = 0; i < fileCollection.Count; i++)
                        {
                            var postedFile = fileCollection[i];
                            if (postedFile.Length <= 0)
                                return CONSTANTS.FileEmpty;
                            if (File.Exists(filePath + Path.GetFileName(correlationId + "_" + postedFile.FileName)))
                            {
                                counter++;
                                FileName = postedFile.FileName;
                                string[] strfileName = FileName.Split('.');
                                FileName = strfileName[0] + "_" + counter;
                                SaveFileName = FileName + "." + strfileName[1];
                                _encryptionDecryption.EncryptFile(postedFile, filePath + Path.GetFileName(correlationId + "_" + SaveFileName));
                            }
                            else
                            {
                                SaveFileName = postedFile.FileName;
                                _encryptionDecryption.EncryptFile(postedFile, filePath + Path.GetFileName(correlationId + "_" + SaveFileName));
                            }
                            if (DataSourceFilePath != "")
                            {
                                DataSourceFilePath += "" + postedFile.FileName + ",";
                            }
                            else
                                DataSourceFilePath = DataSourceFilePath + "" + postedFile.FileName + ",";
                            if (ParentFileName != CONSTANTS.undefined)
                            {
                                if (appSettings.Value.IsAESKeyVault && appSettings.Value.Environment == CONSTANTS.PADEnvironment && appSettings.Value.EncryptUploadedFiles)
                                    FilePath = FilePath + "" + filePath + correlationId + "_" + SaveFileName + ".enc" + @"""" + @",""";
                                else
                                    FilePath = FilePath + "" + filePath + correlationId + "_" + SaveFileName + @"""" + @",""";
                                if (postedFile.FileName == ParentFileName)
                                {
                                    ParentFileNamePath = filePath + correlationId + "_" + SaveFileName;
                                }
                            }
                            else
                            {
                                if (appSettings.Value.IsAESKeyVault && appSettings.Value.Environment == CONSTANTS.PADEnvironment && appSettings.Value.EncryptUploadedFiles)
                                    FilePath = FilePath + "" + filePath + correlationId + "_" + SaveFileName + ".enc" + @"""" + @",""";
                                else
                                    FilePath = FilePath + "" + filePath + correlationId + "_" + SaveFileName + @"""" + @",""";
                                ParentFileNamePath = ParentFileName;
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(IngestedDataService), nameof(IEUploadFiles), "FILECOLLECTION FILEPATH: " + FilePath + ", PARENTFILENAME: " + ParentFileNamePath + ", CORRELATIONID: " + correlationId, string.Empty, string.Empty, clientUID, deliveryUID); //TODO: Remove log                                
                            }
                            if (fileCollection.Count > 0)
                            {
                                postedFileName = "[" + @"""" + FilePath.Remove(FilePath.Length - 2, 2) + "]";
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(IngestedDataService), nameof(IEUploadFiles), "FILECOLLECTION POSTEDFILENAME: " + postedFileName + ", CORRELATIONID: " + correlationId, string.Empty, string.Empty, clientUID, deliveryUID); //TODO: Remove log
                            }
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(IngestedDataService), nameof(IEUploadFiles), "fileCollection END", string.Empty, string.Empty, clientUID, deliveryUID); //TODO: Remove log
                        }
                    }
                    flag = true;
                    userId = _encryptionDecryption.Encrypt(userId);
                    ingrainRequest = new IERequestQueue
                    {
                        CorrelationId = correlationId,
                        DataSetUId = dataSetUId,                       
                        RequestId = Guid.NewGuid().ToString(),
                        ClientUId = clientUID,
                        DeliveryConstructUId = deliveryUID,
                        Status = "N",
                        RequestStatus = CONSTANTS.New,
                        Message = null,
                        Progress = null,
                        pageInfo = CONSTANTS.IngestData,
                        ParamArgs = null,
                        Function = CONSTANTS.FileUpload,
                        CreatedBy = userId,//encryptDB ? _encryptionDecryption.Encrypt(userId) : userId,
                        CreatedOn = DateTime.Now,
                        ModifiedBy = userId,//encryptDB ? _encryptionDecryption.Encrypt(userId) : userId,
                        ModifiedOn = DateTime.Now

                    };


                    _filepath = new IEFilepath();
                    if (postedFileName != "")
                        _filepath.fileList = postedFileName;
                    else
                        _filepath.fileList = "null";
                    parentFile = new IEParentFile();

                    if (ParentFileName != "undefined")
                    {
                        parentFile.Type = Source;
                        if (Source == "file")
                        {
                            parentFile.Name = ParentFileNamePath;
                        }
                        else
                            parentFile.Name = ParentFileName;
                    }
                    else
                    {
                        parentFile.Type = "null";
                        parentFile.Name = "null";
                    }

                    if (Customdata != CONSTANTS.Null && Customdata != string.Empty)
                    {
                        string AppName = string.Empty;
                        if (appSettings.Value.Environment == CONSTANTS.PAMEnvironment)
                        {
                            AppName = "VDS";
                        }
                        else if (appSettings.Value.Environment == CONSTANTS.FDSEnvironment)
                        {
                            AppName = "VDS(AIOPS)";
                        }
                        else {
                            AppName = "VDS(SI)";
                        }

                        var AppData = _inferenceEngineDBContext.IEAppIngerationRepository.GetAppDetailsOnAppName(AppName);
                        var fileParams = JsonConvert.DeserializeObject<IEInputParams>(Customdata);                       
                        startDate = fileParams.StartDate;
                        endDate = fileParams.EndDate;
                        IEInputParams param = new IEInputParams
                        {
                            ClientID = clientUID,
                            E2EUID = E2EUID,
                            DeliveryConstructID = deliveryUID,
                            Environment = fileParams.Environment,
                            RequestType = fileParams.RequestType,
                            ServiceType = fileParams.ServiceType,
                            StartDate = fileParams.StartDate,
                            EndDate = fileParams.EndDate
                        };
                        IECustomPayloads AppPayload = new IECustomPayloads
                        {
                            AppId = AppData.ApplicationID,
                            HttpMethod = CONSTANTS.POST,
                            AppUrl = appSettings.Value.GetVdsPAMDataURL,
                            InputParameters = param
                        };
                        IEFileUpload Customfile = new IEFileUpload
                        {
                            CorrelationId = correlationId,
                            ClientUId = clientUID,
                            DeliveryConstructUId = deliveryUID,
                            Parent = parentFile,
                            Flag = CONSTANTS.Null,
                            mapping = CONSTANTS.Null,
                            mapping_flag = CONSTANTS.False,
                            pad = CONSTANTS.Null,
                            metric = CONSTANTS.Null,
                            InstaMl = CONSTANTS.Null,
                            fileupload = _filepath,
                            Customdetails = AppPayload,

                        };
                        DataSourceFilePath = DataSourceFilePath + param.ServiceType + ",";
                        //_ingestedData.DataSource = param.ServiceType;
                        category = param.RequestType;

                        ingrainRequest.ParamArgs = Customfile.ToJson();
                    }
                    else
                    {
                        fileUpload = new IEFileUpload
                        {
                            CorrelationId = correlationId,
                            ClientUId = clientUID,
                            DeliveryConstructUId = deliveryUID,
                            Parent = parentFile,
                            Flag = CONSTANTS.Null,
                            mapping = MappingColumns,
                            mapping_flag = MappingFlag,
                            pad = Entities,
                            metric = Metrices,
                            InstaMl = InstaML,
                            fileupload = _filepath,
                            Customdetails = CONSTANTS.Null

                        };
                        ingrainRequest.ParamArgs = fileUpload.ToJson();
                    }
                    if (string.IsNullOrEmpty(sourceName))
                    {
                        if (Entities != CONSTANTS.Null)
                        {
                            sourceName = "Entity";
                        }
                        else if (Metrices != CONSTANTS.Null)
                        {
                            sourceName = "Metric";
                        }
                        else if (!string.IsNullOrEmpty(postedFileName))
                        {
                            sourceName = "File";
                        }

                        if (Source == "Custom")
                        {
                            sourceName = "Custom";
                        }
                    }

                    if (!string.IsNullOrEmpty(dataSetUId))
                        sourceName = "DataSet";

                    if (Customdata == CONSTANTS.Null || Customdata == string.Empty)
                    {
                        category = Category;
                    }
                    if (DataSourceFilePath != "")
                    {
                        if (dataSource == "Entity" || ( appSettings.Value.Environment!=CONSTANTS.PADEnvironment && (dataSource == "Entity" || sourceName == "Custom" || dataSource == "Custom")))
                            dataSource = DataSourceFilePath.Remove(DataSourceFilePath.Length - 1, 1);

                    }

                    IEModel iEModel = new IEModel
                    {
                        CorrelationId = correlationId,
                        FunctionalArea = category,
                        ClientUId = clientUID,
                        CreatedBy = ingrainRequest.CreatedBy,
                        CreatedOn = ingrainRequest.CreatedOn,
                        DBEncryptionRequired = encryptDB,
                        DeliveryConstructUId = deliveryUID,
                        Entity = dataSource,
                        MaxDataPull = MaxDataPull,
                        IsPrivate = true,
                        //LastDeployedDate = 
                        ModelName = ModelName,
                        IsMultiSource = UploadFileType == "multiple",
                        ModifiedBy = ingrainRequest.CreatedBy,
                        ModifiedOn = ingrainRequest.CreatedOn,
                        SourceName = sourceName
                    };
                    if (iEModel.SourceName == "Entity")
                    {
                        var entObj = JObject.Parse(Entities);
                        iEModel.StartDate = entObj["startDate"].ToString();
                        iEModel.EndDate = entObj["endDate"].ToString();
                    }
                    if (!appSettings.Value.Environment.Equals(CONSTANTS.PADEnvironment) && iEModel.SourceName == "Custom")
                    {
                        iEModel.StartDate = startDate;
                        iEModel.EndDate = endDate;
                    }

                    _inferenceEngineDBContext.IEModelRepository.InsertIEModel(iEModel);
                    _inferenceEngineDBContext.IERequestQueueRepository.IEInsertRequests(ingrainRequest);
                    Thread.Sleep(1000);

                    IERequestQueue requestQueue = new IERequestQueue();
                    requestQueue = _inferenceEngineDBContext.IERequestQueueRepository.IEGetFileRequestStatus(correlationId, "IngestData");//IEGetFileRequestStatus(correlationId, "IngestData");
                    if (requestQueue != null)
                    {
                        IEPythonCategory pythonCategory = new IEPythonCategory();
                        IEPythonInfo pythonInfo = new IEPythonInfo();
                        requestQueueStatus = requestQueue.Status;
                        if (requestQueue.Status == "C" && requestQueue.Progress == "100")
                        {
                            _inferenceEngineDBContext.IEModelRepository.UpdateIEModel(correlationId, requestQueue.Status, requestQueue.Message, requestQueue.Progress);
                            return CONSTANTS.Success;

                        }
                        else if (requestQueue.Status == "M" && requestQueue.Progress == "100")
                        {
                            return CONSTANTS.Success;
                        }
                        else if (requestQueue.Status == "E")
                        {
                            _inferenceEngineDBContext.IEModelRepository.UpdateIEModel(correlationId, requestQueue.Status, requestQueue.Message, requestQueue.Progress);
                            return CONSTANTS.PhythonError;

                        }
                        else if (requestQueue.Status == "I")
                        {
                            _inferenceEngineDBContext.IEModelRepository.UpdateIEModel(correlationId, requestQueue.Status, requestQueue.Message, requestQueue.Progress);
                            return CONSTANTS.PhythonInfo;
                        }
                        else if (requestQueue.Status == "P")
                        {
                            return CONSTANTS.PhythonProgress;
                        }
                        else
                        {
                            return CONSTANTS.New;
                        }
                    }
                    else
                    {
                        Thread.Sleep(2000);
                    }
                }

                return string.Empty;
            }
            else
            {
                IERequestQueue requestQueue = new IERequestQueue();
                requestQueue = _inferenceEngineDBContext.IERequestQueueRepository.IEGetFileRequestStatus(CorrelationId_status, "IngestData"); //IEGetFileRequestStatus(CorrelationId_status, "IngestData");
                if (requestQueue != null)
                {
                    IEPythonCategory pythonCategory = new IEPythonCategory();
                    IEPythonInfo pythonInfo = new IEPythonInfo();
                    requestQueueStatus = requestQueue.Status;
                    if (requestQueue.Status == "C" & requestQueue.Progress == "100")
                    {
                        _inferenceEngineDBContext.IEModelRepository.UpdateIEModel(CorrelationId_status, requestQueue.Status, requestQueue.Message, requestQueue.Progress);
                        return CONSTANTS.Success;
                    }
                    else if (requestQueue.Status == "M" & requestQueue.Progress == "100")
                    {
                        return CONSTANTS.Success;
                    }
                    else if (requestQueue.Status == "E")
                    {
                        _inferenceEngineDBContext.IEModelRepository.UpdateIEModel(CorrelationId_status, requestQueue.Status, requestQueue.Message, requestQueue.Progress);
                        return CONSTANTS.PhythonError;

                    }
                    else if (requestQueue.Status == "I")
                    {
                        _inferenceEngineDBContext.IEModelRepository.UpdateIEModel(CorrelationId_status, requestQueue.Status, requestQueue.Message, requestQueue.Progress);
                        return CONSTANTS.PhythonInfo;
                    }
                    else if (requestQueue.Status == "P")
                    {
                        return CONSTANTS.PhythonProgress;
                    }
                    else
                    {
                        return CONSTANTS.New;
                    }
                }

                return string.Empty;
            }

        }

        public IEFileUploadColums IEGetFilesColumns(string correlationId, string ParentFileName, string ModelName)
        {
            IEFileUploadColums fileUploadColums = new IEFileUploadColums();
            var UploadColums = _inferenceEngineDBContext.IEModelRepository.GetMultiFileColumn(correlationId);
            if (UploadColums.Count > 0)
            {
                var file = JObject.Parse(UploadColums[0]["File"].ToString());
                fileUploadColums.CorrelationId = UploadColums[0]["CorrelationId"].ToString();
                fileUploadColums.Flag = UploadColums[0]["Flag"].ToString();
                fileUploadColums.ParentFileName = ParentFileName;
                fileUploadColums.ModelName = ModelName;
                List<IEFileColumns> fileColumns = new List<IEFileColumns>();
                bool ParentFlag = false;
                int flagfile_entity = 0;
                int flagmetric = 0;
                if (file != null)
                {
                    foreach (var item in file.Children())
                    {
                        JObject serializeData = new JObject();
                        JProperty jProperty = item as JProperty;
                        JObject serializeDataCols = new JObject();
                        if (jProperty != null)
                        {
                            //string filename = jProperty.Name.Remove(0, correlationId.Length);
                            Dictionary<string, string> DiColumn = new Dictionary<string, string>();
                            string column = jProperty.Value.ToString();
                            serializeData = JObject.Parse(column.ToString());
                            List<string> colsname = new List<string>();
                            colsname.Add(serializeData["Columns"].ToString());
                            string fileExtension = serializeData["FileExtensionOrig"].ToString();
                            string filename = string.Empty;
                            if (fileExtension == "csv" || fileExtension == "xlsx" || fileExtension == "Entity")
                            {
                                flagfile_entity = 1;
                            }
                            else
                            {
                                flagmetric = 1;
                            }

                            if (fileExtension != "Custom")
                            {
                                filename = jProperty.Name.Remove(0, correlationId.Length + 1);
                                if (filename + "." + fileExtension == ParentFileName)
                                {
                                    ParentFlag = true;
                                }
                                else
                                {
                                    ParentFlag = false;
                                }
                            }
                            else
                            {
                                filename = jProperty.Name;
                                if (filename == ParentFileName)
                                {
                                    ParentFlag = true;
                                }
                                else
                                {
                                    ParentFlag = false;
                                }
                            }
                            foreach (var item1 in serializeData["Columns"].Children())
                            {
                                JProperty jProperty1 = item1 as JProperty;
                                DiColumn.Add(jProperty1.Name, jProperty1.Value.ToString());

                            }
                            fileColumns.Add(new IEFileColumns
                            {
                                FileName = filename,//.Remove(0, 1),
                                FileColumn = DiColumn,
                                ParentFileFlag = ParentFlag
                            });
                        }
                    }
                    if (flagmetric == 1)
                        fileUploadColums.Fileflag = false;
                    else
                        fileUploadColums.Fileflag = true;

                    fileUploadColums.File = fileColumns;
                }
            }
            return fileUploadColums;
        }

        public string IEGetIngrainRequestCollection(string userId, string uploadfiletype, string mappingflag, string correlationid, string pageInfo,
        string modelname, string clientUID, string deliveryUID, HttpContext httpContext, string category, string uploadtype, string statusFlag)
        {
            bool flag = true;
            string mappingcolumns = string.Empty, DataSourceFilePath = string.Empty;

            if (statusFlag == "" || statusFlag == "undefined")
            {
                var ingrainrequestCollection = _inferenceEngineDBContext.IERequestQueueRepository.IEGetFileRequestStatus(correlationid, pageInfo);
                IFormCollection formcollection = httpContext.Request.Form;

                var mapping = formcollection[CONSTANTS.mappingPayload];
                if (mapping.Count > 0)
                {
                    foreach (var item in mapping)
                    {
                        if (item != "{}")
                            mappingcolumns = item;
                    }
                }
                //if (ingrainrequestCollection.Count() > 0)
                if (ingrainrequestCollection != null)
                {
                    var paramArgs = JObject.Parse(ingrainrequestCollection.ParamArgs.ToString());//JObject.Parse(ingrainrequestCollection["ParamArgs"].ToString());
                    IEFileUpload fileUpload = new IEFileUpload();
                    JObject serialize = new JObject();
                    string parentvalue = string.Empty;
                    foreach (var item in paramArgs.Children())
                    {
                        JObject serializeData = new JObject();
                        JProperty jProperty = item as JProperty;
                        if (jProperty != null)
                        {
                            string propertyname = jProperty.Name;
                            switch (propertyname)
                            {
                                case "mapping":
                                    fileUpload.mapping = mappingcolumns;
                                    break;
                                case "mapping_flag":
                                    fileUpload.mapping_flag = mappingflag;
                                    break;
                                case "CorrelationId":
                                    fileUpload.CorrelationId = jProperty.Value.ToString();
                                    break;
                                case "Flag":
                                    fileUpload.Flag = CONSTANTS.Null;
                                    break;
                                case "fileupload":
                                    IEFilepath filepath = new IEFilepath();
                                    string filepathvalue = jProperty.Value.ToString();
                                    if (filepathvalue != "")
                                    {
                                        serialize = JObject.Parse(filepathvalue);
                                        foreach (var child in serialize.Children())
                                        {
                                            JProperty jProperty1 = child as JProperty;
                                            if (jProperty1 != null)
                                            {
                                                filepath.fileList = jProperty1.Value.ToString();
                                                fileUpload.fileupload = filepath;
                                            }
                                        }
                                    }
                                    else
                                        fileUpload = null;
                                    break;
                                case "ClientUId":
                                    fileUpload.ClientUId = jProperty.Value.ToString();
                                    break;
                                case "DeliveryConstructUId":
                                    fileUpload.DeliveryConstructUId = jProperty.Value.ToString();
                                    break;
                                case "pad":
                                    fileUpload.pad = jProperty.Value.ToString();
                                    break;
                                case "Parent":
                                    IEParentFile parentFile = new IEParentFile();
                                    parentvalue = jProperty.Value.ToString();
                                    if (parentvalue != "")
                                    {
                                        serialize = JObject.Parse(parentvalue);
                                        foreach (var child in serialize.Children())
                                        {
                                            JProperty jProperty1 = child as JProperty;
                                            if (jProperty1 != null)
                                            {
                                                if (jProperty1.Name == "Type")
                                                    parentFile.Type = jProperty1.Value.ToString();
                                                else
                                                    parentFile.Name = jProperty1.Value.ToString();
                                                fileUpload.Parent = parentFile;
                                            }
                                        }
                                    }
                                    else
                                        parentFile = null;
                                    break;
                                case "metric":
                                    fileUpload.metric = jProperty.Value.ToString();
                                    break;
                                case "InstaMl":
                                    fileUpload.InstaMl = jProperty.Value.ToString();
                                    break;
                                case "Customdetails":
                                    fileUpload.Customdetails = CONSTANTS.Null;
                                    break;

                            }
                        }
                    }
                    string Empty = null;
                    var builder = Builders<BsonDocument>.Update;
                    var update = builder
                          .Set("Status", "N")
                          .Set("ModelName", Empty)
                          .Set("RequestStatus", CONSTANTS.New)
                          .Set("RetryCount", 0)
                          .Set("ProblemType", Empty)
                          .Set("Message", Empty)
                          .Set("UniId", Empty)
                          .Set("Progress", Empty)
                          .Set("ParamArgs", fileUpload.ToJson())
                     .Set("ModifiedBy", userId)
                          .Set("ModifiedOn", DateTime.UtcNow);
                    //collection.UpdateMany(filter, update);
                    var encrypted = _inferenceEngineDBContext.IEModelRepository.GetIEModel(correlationid);
                    _inferenceEngineDBContext.IERequestQueueRepository.UpdateIERequestQueue(correlationid, pageInfo, fileUpload, encrypted.DBEncryptionRequired? _encryptionDecryption.Encrypt(userId) : userId);

                    var EntitiesNames = formcollection[CONSTANTS.EntitiesName];
                    var MetricsNames = formcollection[CONSTANTS.MetricNames];
                    if (EntitiesNames.Count > 0)
                    {
                        foreach (var item in EntitiesNames)
                        {
                            if (item != "{}")
                                DataSourceFilePath = item + ",";
                        }
                    }
                    if (MetricsNames.Count() > 0)
                    {
                        foreach (var item in MetricsNames)
                        {
                            if (item != "{}")
                            {
                                DataSourceFilePath += item + ",";
                            }
                        }
                    }
                    var fileCollection = httpContext.Request.Form.Files;
                    if (fileCollection.Count() > 0)
                    {
                        if (CommonUtility.ValidateFileUploaded(fileCollection))
                        {
                            throw new FormatException(Resource.IngrainResx.InValidFileName);
                        }

                        for (int i = 0; i < fileCollection.Count; i++)
                        {
                            var postedfile = fileCollection[i];
                            DataSourceFilePath += "" + postedfile.FileName + ",";
                        }
                    }

                    IERequestQueue requestQueue = new IERequestQueue();
                    requestQueue = _inferenceEngineDBContext.IERequestQueueRepository.IEGetFileRequestStatus(correlationid, "IngestData");//IEGetFileRequestStatus(correlationid, "IngestData");


                    if (requestQueue != null)
                    {
                        IEPythonCategory pythonCategory = new IEPythonCategory();
                        IEPythonInfo pythonInfo = new IEPythonInfo();
                        if (requestQueue.Status == "C" & requestQueue.Progress == "100")
                        {
                            _inferenceEngineDBContext.IEModelRepository.UpdateIEModel(correlationid, requestQueue.Status, requestQueue.Message, requestQueue.Progress);
                            return CONSTANTS.Success;
                        }
                        else if (requestQueue.Status == "E")
                        {
                            _inferenceEngineDBContext.IEModelRepository.UpdateIEModel(correlationid, requestQueue.Status, requestQueue.Message, requestQueue.Progress);
                            return CONSTANTS.PhythonError;
                        }
                        else if (requestQueue.Status == "P")
                        {
                            return CONSTANTS.PhythonProgress;
                        }
                        else if (requestQueue.Status == "I")
                        {
                            _inferenceEngineDBContext.IEModelRepository.UpdateIEModel(correlationid, requestQueue.Status, requestQueue.Message, requestQueue.Progress);
                            return CONSTANTS.PhythonInfo;
                        }
                        else
                        {
                            return CONSTANTS.New;
                        }
                    }
                }
                return string.Empty;
            }
            else
            {
                IERequestQueue requestQueue = new IERequestQueue();
                requestQueue = _inferenceEngineDBContext.IERequestQueueRepository.IEGetFileRequestStatus(correlationid, "IngestData");//IEGetFileRequestStatus(correlationid, "IngestData");
                if (requestQueue != null)
                {
                    IEPythonCategory pythonCategory = new IEPythonCategory();
                    IEPythonInfo pythonInfo = new IEPythonInfo();
                    if (requestQueue.Status == "C" & requestQueue.Progress == "100")
                    {
                        _inferenceEngineDBContext.IEModelRepository.UpdateIEModel(correlationid, requestQueue.Status, requestQueue.Message, requestQueue.Progress);
                        return CONSTANTS.Success;
                    }
                    else if (requestQueue.Status == "E")
                    {
                        _inferenceEngineDBContext.IEModelRepository.UpdateIEModel(correlationid, requestQueue.Status, requestQueue.Message, requestQueue.Progress);
                        return CONSTANTS.PhythonError;
                    }
                    else if (requestQueue.Status == "P")
                    {
                        return CONSTANTS.PhythonProgress;
                    }
                    else if (requestQueue.Status == "I")
                    {
                        _inferenceEngineDBContext.IEModelRepository.UpdateIEModel(correlationid, requestQueue.Status, requestQueue.Message, requestQueue.Progress);
                        return CONSTANTS.PhythonInfo;
                    }
                    else
                    {
                        return CONSTANTS.New;
                    }
                }
                return string.Empty;
            }

        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="inferenceConfigId"></param>
        /// <param name="inferenceConfigType"></param>
        /// <returns></returns>
        public InferenceResultOuput FetchInferenceResult(string inferenceConfigId, string inferenceConfigType, string InferenceSourceType, bool isAutoGenerated)
        {
            var result = _inferenceEngineDBContext.InferenceConfigRepository.GetInferenceResults(inferenceConfigId, inferenceConfigType);
            //  List<InferenceResultOuput> output = new List<InferenceResultOuput>();
            InferenceResultOuput res = new InferenceResultOuput();
            if (result != null)
            {
                //InferenceResultOuput res = new InferenceResultOuput();
                if (result.InferenceConfigType == "MeasureAnalysis")
                {
                    res.MeasureAnalysisInferences = new List<Inference>();
                    List<Inference> outlierNarrative = new List<Inference>();
                    var autoSelectedMeasure = new List<Inference>();
                    var autoSelectedOutlier = new List<Inference>();
                    var narrativeValue = new List<Inference>();
                    var serializeData = JObject.Parse(result.InferenceResults.ToString());//result.InferenceResults;
                    foreach (var narrative in serializeData.Children())
                    {

                        JProperty j = narrative as JProperty;
                        if (j.Name == "level1_narratives")
                        {
                            var value = new List<JObject>();
                            if (!string.IsNullOrEmpty(j.Value.ToString()))
                            {
                                var limitedNarratives = new List<Inference>();
                                var data = JArray.Parse(j.Value.ToString());
                                Inference inference = new Inference();
                                inference.DisplayName = "Level 1 Narratives";
                                foreach (var item in data.Children())
                                {
                                    foreach (var nar in item.Children())
                                    {
                                        JProperty val = nar as JProperty;
                                        if (val.Name == "narratives")
                                        {
                                            value.Add(new JObject(new JProperty(val.Name, val.Value.ToString())));
                                        }
                                    }
                                }
                                inference.Value = value;
                                narrativeValue.Add(inference);

                                if (isAutoGenerated)
                                {
                                    List<JObject> topBottom2 = new List<JObject>();
                                    var selectedTop = value.Take(2).ToList();
                                    var selectedButtom = value.TakeLast(2).ToList();
                                    topBottom2.AddRange(selectedTop);
                                    topBottom2.AddRange(selectedButtom);
                                    inference.Value = topBottom2;
                                    limitedNarratives.Add(inference);
                                    autoSelectedMeasure.AddRange(limitedNarratives);                                    
                                }
                                else
                                {
                                    autoSelectedMeasure.AddRange(narrativeValue);
                                }
                                narrativeValue = new List<Inference>();

                            }
                            else
                            {
                                Inference inference = new Inference();
                                inference.DisplayName = "Level 1 Narratives";
                                inference.Value = null;//data;
                                narrativeValue.Add(inference);
                            }
                        }

                        if (j.Name == "level2_narratives")
                        {
                            var limitedNarratives = new List<Inference>();
                            var value = new List<JObject>();
                            if (!string.IsNullOrEmpty(j.Value.ToString()))
                            {
                                var data = JArray.Parse(j.Value.ToString());
                                Inference inference = new Inference();
                                inference.DisplayName = "Level 2 Narratives";
                                foreach (var item in data.Children())
                                {
                                    foreach (var nar in item.Children())
                                    {
                                        JProperty val = nar as JProperty;
                                        if (val.Name == "narratives")
                                        {
                                            value.Add(new JObject(new JProperty(val.Name, val.Value.ToString())));
                                        }
                                    }
                                }
                                inference.Value = value;
                                narrativeValue.Add(inference);
                                if (isAutoGenerated)
                                {
                                    if (value.Count > 4)
                                    {
                                        List<JObject> topBottom2 = new List<JObject>();
                                        var selectedTop = value.Take(2).ToList();
                                        var selectedButtom = value.TakeLast(2).ToList();
                                        topBottom2.AddRange(selectedTop);
                                        topBottom2.AddRange(selectedButtom);
                                        inference.Value = topBottom2;
                                        limitedNarratives.Add(inference);

                                        autoSelectedMeasure.AddRange(limitedNarratives);
                                    }
                                    else
                                    {
                                        autoSelectedMeasure.AddRange(narrativeValue);
                                    }
                                    narrativeValue = new List<Inference>();
                                }
                            }
                            else
                            {
                                Inference inference = new Inference();
                                inference.DisplayName = "Level 2 Narratives";
                                inference.Value = null;//data;
                                narrativeValue.Add(inference);
                            }
                        }

                        if (j.Name == "level3_narratives")
                        {
                            var limitedNarratives = new List<Inference>();
                            var value = new List<JObject>();
                            if (!string.IsNullOrEmpty(j.Value.ToString()))
                            {
                                var data = JArray.Parse(j.Value.ToString());
                                Inference inference = new Inference();
                                inference.DisplayName = "Level 3 Narratives";
                                foreach (var item in data.Children())
                                {
                                    foreach (var nar in item.Children())
                                    {
                                        JProperty val = nar as JProperty;
                                        if (val.Name == "narratives")
                                        {
                                            value.Add(new JObject(new JProperty(val.Name, val.Value.ToString())));
                                        }
                                    }
                                }
                                inference.Value = value;
                                narrativeValue.Add(inference);
                                if (isAutoGenerated)
                                {
                                    if (value.Count > 4)
                                    {
                                        List<JObject> topBottom2 = new List<JObject>();
                                        var selectedTop = value.Take(2).ToList();
                                        var selectedButtom = value.TakeLast(2).ToList();
                                        topBottom2.AddRange(selectedTop);
                                        topBottom2.AddRange(selectedButtom);
                                        inference.Value = topBottom2;
                                        limitedNarratives.Add(inference);

                                        autoSelectedMeasure.AddRange(limitedNarratives);
                                    }
                                    else
                                    {
                                        autoSelectedMeasure.AddRange(narrativeValue);
                                    }
                                    narrativeValue = new List<Inference>();
                                }
                            }
                            else
                            {
                                Inference inference = new Inference();
                                inference.DisplayName = "Level 3 Narratives";
                                inference.Value = null;
                                narrativeValue.Add(inference);
                            }
                        }

                        if (j.Name == "level1_outliers_narratives")
                        {
                            var limitedNarratives = new List<Inference>();
                            var value = new List<JObject>();
                            if (!string.IsNullOrEmpty(j.Value.ToString()))
                            {
                                var data = JArray.Parse(j.Value.ToString()); //JsonConvert.DeserializeObject(j1.Value.ToString());
                                Inference inference = new Inference();
                                inference.DisplayName = "Level 1 Outliers Narratives";
                                foreach (var item in data.Children())
                                {
                                    foreach (var nar in item.Children())
                                    {
                                        JProperty val = nar as JProperty;
                                        if (val.Name == "narratives")
                                        {
                                            value.Add(new JObject(new JProperty(val.Name, val.Value.ToString())));
                                        }
                                    }
                                }
                                inference.Value = value;
                                outlierNarrative.Add(inference);
                                if (isAutoGenerated)
                                {
                                    if (value.Count > 4)
                                    {
                                        List<JObject> topBottom2 = new List<JObject>();
                                        var selectedTop = value.Take(2).ToList();
                                        var selectedButtom = value.TakeLast(2).ToList();
                                        topBottom2.AddRange(selectedTop);
                                        topBottom2.AddRange(selectedButtom);
                                        inference.Value = topBottom2;
                                        limitedNarratives.Add(inference);
                                        autoSelectedOutlier.AddRange(limitedNarratives);
                                    }                                    
                                    else
                                    {
                                        autoSelectedOutlier.AddRange(outlierNarrative);
                                    }
                                    outlierNarrative = new List<Inference>();
                                }

                            }
                            else
                            {
                                Inference inference = new Inference();
                                inference.DisplayName = "Level 1 Outliers Narratives";
                                inference.Value = null;
                                outlierNarrative.Add(inference);
                            }
                        }

                        if (j.Name == "level2_outliers_narratives")
                        {
                            var limitedNarratives = new List<Inference>();
                            var value = new List<JObject>();
                            if (!string.IsNullOrEmpty(j.Value.ToString()))
                            {
                                var data = JArray.Parse(j.Value.ToString());
                                Inference inference = new Inference();
                                inference.DisplayName = "Level 2 Outliers Narratives";
                                foreach (var item in data.Children())
                                {
                                    foreach (var nar in item.Children())
                                    {
                                        JProperty val = nar as JProperty;
                                        if (val.Name == "narratives")
                                        {
                                            value.Add(new JObject(new JProperty(val.Name, val.Value.ToString())));
                                        }
                                    }
                                }
                                inference.Value = value;
                                outlierNarrative.Add(inference);
                                if (isAutoGenerated)
                                {
                                    if (value.Count > 4)
                                    {
                                        List<JObject> topBottom2 = new List<JObject>();
                                        var selectedTop = value.Take(2).ToList();
                                        var selectedButtom = value.TakeLast(2).ToList();
                                        topBottom2.AddRange(selectedTop);
                                        topBottom2.AddRange(selectedButtom);
                                        inference.Value = topBottom2;
                                        limitedNarratives.Add(inference);
                                        autoSelectedOutlier.AddRange(limitedNarratives);
                                    }
                                    else
                                    {
                                        autoSelectedOutlier.AddRange(outlierNarrative);
                                    }
                                    outlierNarrative = new List<Inference>();
                                }
                            }
                            else
                            {
                                Inference inference = new Inference();
                                inference.DisplayName = "Level 2 Outliers Narratives";
                                inference.Value = null;
                                outlierNarrative.Add(inference);
                            }
                        }

                        if (j.Name == "level3_outliers_narratives")
                        {
                            var limitedNarratives = new List<Inference>();
                            var value = new List<JObject>();
                            if (!string.IsNullOrEmpty(j.Value.ToString()))
                            {
                                var data = JArray.Parse(j.Value.ToString());
                                Inference inference = new Inference();
                                inference.DisplayName = "Level 3 Outliers Narratives";
                                foreach (var item in data.Children())
                                {
                                    foreach (var nar in item.Children())
                                    {
                                        JProperty val = nar as JProperty;
                                        if (val.Name == "narratives")
                                        {
                                            value.Add(new JObject(new JProperty(val.Name, val.Value.ToString())));
                                        }
                                    }
                                }
                                inference.Value = value;
                                outlierNarrative.Add(inference);
                                if (isAutoGenerated)
                                {
                                    if (value.Count > 4)
                                    {
                                        List<JObject> topBottom2 = new List<JObject>();
                                        var selectedTop = value.Take(2).ToList();
                                        var selectedButtom = value.TakeLast(2).ToList();
                                        topBottom2.AddRange(selectedTop);
                                        topBottom2.AddRange(selectedButtom);
                                        inference.Value = topBottom2;
                                        limitedNarratives.Add(inference);
                                        autoSelectedOutlier.AddRange(limitedNarratives);
                                    }
                                    else
                                    {
                                        autoSelectedOutlier.AddRange(outlierNarrative);
                                    }
                                    outlierNarrative = new List<Inference>();
                                }
                            }
                            else
                            {
                                Inference inference = new Inference();
                                inference.DisplayName = "Level 3 Outliers Narratives";
                                inference.Value = null;
                                outlierNarrative.Add(inference);
                            }
                        }

                    }
                    Inference measure = new Inference();
                    measure.DisplayName = "Measure Analysis";
                    if (isAutoGenerated)
                    {
                        measure.Value = autoSelectedMeasure;
                    }
                    else
                    {
                        measure.Value = narrativeValue;
                    }

                    res.MeasureAnalysisInferences.Add(measure);
                    Inference outlier = new Inference();
                    outlier.DisplayName = "Outlier Analysis";
                    if (isAutoGenerated)
                    {
                        outlier.Value = autoSelectedOutlier;
                    }
                    else
                    {
                        outlier.Value = outlierNarrative;
                    }

                    res.MeasureAnalysisInferences.Add(outlier);

                }
                else if (result.InferenceConfigType == "VolumetricAnalysis")
                {
                    res.VolumetricInferences = new List<Inference>();
                    var serializeData = JObject.Parse(result.InferenceResults.ToString());//result.InferenceResults;
                    foreach (var narrative in serializeData.Children())
                    {
                        JProperty j = narrative as JProperty;
                        if (j.Name == "distribution_narratives")
                        {
                            Inference distribution = new Inference();
                            distribution.DisplayName = "Distributive Narratives";
                            distribution.Value = new JArray();
                            var narrativeValue = new List<Inference>();

                            var serializedatasort = JObject.Parse(j.Value.ToString());
                            foreach (var sort in serializedatasort.Children())
                            {
                                JProperty j1 = sort as JProperty;
                                if (j1.Name == "Level1_Distribution_Narratives")
                                {
                                    var value = new List<JObject>();
                                    if (!string.IsNullOrEmpty(j1.Value.ToString()))
                                    {
                                        var data = JArray.Parse(j1.Value.ToString()); //JsonConvert.DeserializeObject(j1.Value.ToString());
                                        Inference inference = new Inference();
                                        inference.DisplayName = "Level - 1 Distributive Narratives";
                                        foreach (var item in data.Children())
                                        {
                                            foreach (var nar in item.Children())
                                            {
                                                JProperty val = nar as JProperty;
                                                if (val.Name == "narratives")
                                                {
                                                    value.Add(new JObject(new JProperty(val.Name, val.Value.ToString())));
                                                }
                                            }
                                        }
                                        inference.Value = value;//data;
                                        narrativeValue.Add(inference);
                                    }
                                    else
                                    {
                                        Inference inference = new Inference();
                                        inference.DisplayName = "Level - 1 Distributive Narratives";
                                        inference.Value = null;
                                        narrativeValue.Add(inference);
                                    }
                                }


                                if (j1.Name == "Level2_Distribution_Narratives")
                                {
                                    var value = new List<JObject>();
                                    if (!string.IsNullOrEmpty(j1.Value.ToString()))
                                    {
                                        var data = JArray.Parse(j1.Value.ToString());
                                        Inference inference = new Inference();
                                        inference.DisplayName = "Level - 2 Distributive Narratives";
                                        foreach (var item in data.Children())
                                        {
                                            foreach (var nar in item.Children())
                                            {
                                                JProperty val = nar as JProperty;
                                                if (val.Name == "narratives")
                                                {
                                                    value.Add(new JObject(new JProperty(val.Name, val.Value.ToString())));
                                                }
                                            }
                                        }
                                        inference.Value = value;
                                        narrativeValue.Add(inference);
                                    }
                                    else
                                    {
                                        Inference inference = new Inference();
                                        inference.DisplayName = "Level - 2 Distributive Narratives";
                                        inference.Value = null;//data;
                                        narrativeValue.Add(inference);
                                    }
                                }
                            }
                            distribution.Value = JArray.FromObject(narrativeValue);
                            res.VolumetricInferences.Add(distribution);

                        }

                        if (j.Name == "inflow_narratives")
                        {
                            Inference dataInflow = new Inference();
                            dataInflow.DisplayName = "Data Volume Analysis";
                            dataInflow.Value = new JArray();
                            var narrativeValue = new List<Inference>();

                            var serializedatasort = JObject.Parse(j.Value.ToString());
                            foreach (var sort in serializedatasort.Children())
                            {
                                JProperty j1 = sort as JProperty;

                                if (j1.Name == "quarterly_narratives")
                                {
                                    var value = new List<JObject>();
                                    if (!string.IsNullOrEmpty(j1.Value.ToString()))
                                    {
                                        var data = JArray.Parse(j1.Value.ToString());
                                        Inference inference = new Inference();
                                        inference.DisplayName = "Year Quarter - Year Quarter Comparison";
                                        foreach (var item in data.Children())
                                        {
                                            foreach (var nar in item.Children())
                                            {
                                                JProperty val = nar as JProperty;
                                                if (val.Name == "narratives")
                                                {
                                                    value.Add(new JObject(new JProperty(val.Name, val.Value.ToString())));
                                                }
                                            }
                                        }
                                        inference.Value = value;
                                        narrativeValue.Add(inference);
                                    }
                                    else
                                    {
                                        Inference inference = new Inference();
                                        inference.DisplayName = "Year Quarter - Year Quarter Comparison";
                                        inference.Value = null;//data;
                                        narrativeValue.Add(inference);
                                    }
                                }

                                if (j1.Name == "monthly_narratives")
                                {
                                    var value = new List<JObject>();
                                    if (!string.IsNullOrEmpty(j1.Value.ToString()))
                                    {
                                        var data = JArray.Parse(j1.Value.ToString());
                                        Inference inference = new Inference();
                                        inference.DisplayName = "Year Month - Year Month Comparison";
                                        foreach (var item in data.Children())
                                        {
                                            foreach (var nar in item.Children())
                                            {
                                                JProperty val = nar as JProperty;
                                                if (val.Name == "narratives")
                                                {
                                                    value.Add(new JObject(new JProperty(val.Name, val.Value.ToString())));
                                                }
                                            }
                                        }
                                        inference.Value = value;//data;
                                        narrativeValue.Add(inference);
                                    }
                                    else
                                    {
                                        Inference inference = new Inference();
                                        inference.DisplayName = "Year Month - Year Month Comparison";
                                        inference.Value = null;//data;
                                        narrativeValue.Add(inference);
                                    }
                                }
                            }
                            dataInflow.Value = JArray.FromObject(narrativeValue);
                            res.VolumetricInferences.Add(dataInflow);

                        }

                        if (j.Name == "trend_forecasting")
                        {
                            Inference tredForeCast = new Inference();
                            tredForeCast.DisplayName = "Trend Forecast";
                            tredForeCast.Value = new JArray();
                            var narrativeValue = new List<Inference>();

                            var serializedatasort = JObject.Parse(j.Value.ToString());
                            foreach (var sort in serializedatasort.Children())
                            {
                                JProperty j1 = sort as JProperty;
                                if (j1.Name == "prediction_quarter")
                                {
                                    var value = new List<JObject>();
                                    if (!string.IsNullOrEmpty(j1.Value.ToString()))
                                    {
                                        var data = JArray.Parse(j1.Value.ToString());
                                        Inference inference = new Inference();
                                        inference.DisplayName = "Quarterly";
                                        foreach (var item in data.Children())
                                        {
                                            foreach (var nar in item.Children())
                                            {
                                                JProperty val = nar as JProperty;
                                                if (val.Name == "narratives")
                                                {
                                                    value.Add(new JObject(new JProperty(val.Name, val.Value.ToString())));
                                                }
                                            }
                                        }
                                        inference.Value = value;//data;
                                        narrativeValue.Add(inference);
                                    }
                                    else
                                    {
                                        Inference inference = new Inference();
                                        inference.DisplayName = "Quarterly";
                                        inference.Value = null;
                                        narrativeValue.Add(inference);

                                    }
                                }

                                if (j1.Name == "prediction_month")
                                {
                                    var value = new List<JObject>();
                                    if (!string.IsNullOrEmpty(j1.Value.ToString()))
                                    {
                                        var data = JArray.Parse(j1.Value.ToString());
                                        Inference inference = new Inference();
                                        inference.DisplayName = "Monthly";
                                        foreach (var item in data.Children())
                                        {
                                            foreach (var nar in item.Children())
                                            {
                                                JProperty val = nar as JProperty;
                                                if (val.Name == "narratives")
                                                {
                                                    value.Add(new JObject(new JProperty(val.Name, val.Value.ToString())));
                                                }
                                            }
                                        }
                                        inference.Value = value;
                                        narrativeValue.Add(inference);
                                    }
                                    else
                                    {
                                        Inference inference = new Inference();
                                        inference.DisplayName = "Monthly";
                                        inference.Value = null;
                                        narrativeValue.Add(inference);

                                    }
                                }

                                if (j1.Name == "prediction_week")
                                {
                                    var value = new List<JObject>();
                                    if (!string.IsNullOrEmpty(j1.Value.ToString()))
                                    {
                                        var data = JArray.Parse(j1.Value.ToString());

                                        Inference inference = new Inference();
                                        inference.DisplayName = "Weekly";
                                        foreach (var item in data.Children())
                                        {
                                            foreach (var nar in item.Children())
                                            {
                                                JProperty val = nar as JProperty;
                                                if (val.Name == "narratives")
                                                {
                                                    value.Add(new JObject(new JProperty(val.Name, val.Value.ToString())));
                                                }
                                            }
                                        }
                                        inference.Value = value;
                                        narrativeValue.Add(inference);
                                    }
                                    else
                                    {
                                        Inference inference = new Inference();
                                        inference.DisplayName = "Weekly";
                                        inference.Value = null;
                                        narrativeValue.Add(inference);

                                    }
                                }

                                if (j1.Name == "prediction_day")
                                {
                                    var value = new List<JObject>();
                                    if (!string.IsNullOrEmpty(j1.Value.ToString()))
                                    {
                                        var data = JArray.Parse(j1.Value.ToString());
                                        Inference inference = new Inference();
                                        inference.DisplayName = "Daily";
                                        foreach (var item in data.Children())
                                        {
                                            foreach (var nar in item.Children())
                                            {
                                                JProperty val = nar as JProperty;
                                                if (val.Name == "narratives")
                                                {
                                                    value.Add(new JObject(new JProperty(val.Name, val.Value.ToString())));
                                                }
                                            }
                                        }

                                        inference.Value = value;
                                        narrativeValue.Add(inference);
                                    }
                                    else
                                    {
                                        Inference inference = new Inference();
                                        inference.DisplayName = "Daily";
                                        inference.Value = null;
                                        narrativeValue.Add(inference);

                                    }
                                }

                            }
                            tredForeCast.Value = JArray.FromObject(narrativeValue);
                            res.VolumetricInferences.Add(tredForeCast);

                        }



                    }
                }
            }
            return res;
        }

        public List<IEModel> GetIEModelData(string clientId, string DCId, string userId , string FunctionalArea)
        {
            List<IEModel> iEModel = new List<IEModel>();

            return _inferenceEngineDBContext.IEModelRepository.GetIEModel(clientId, DCId, userId, FunctionalArea, appSettings);
        }

        public List<ModelInferences> GetModelInferences(string correlationId, string applicationId, string inferenceConfigId, bool autogenerated)
        {
            List<ModelInferences> result = new List<ModelInferences>();
            List<IESavedConfig> data = null;
            IEModel model = _inferenceEngineDBContext.IEModelRepository.GetIEModel(correlationId);
            if (model != null)
            {
                if (model.SourceName.ToLower() == "file")
                {
                    List<InferenceConfigTemplate> inferenceConfigTemplates =
                        _inferenceEngineDBContext.IEConfigTemplateRepository.GetConfigTemplates(appSettings.Value.Environment, "File", null, null);
                    if (inferenceConfigTemplates.Count > 0)
                    {
                        foreach (var inferenceConfigTemplate in inferenceConfigTemplates)
                        {
                            var templateResult =
                                _inferenceEngineDBContext.IEConfigTemplateRepository.GetDefaultConfigTemplateResult(inferenceConfigTemplate.DefaultTemplateId, inferenceConfigTemplate.InferenceConfigType);
                            ModelInferences modelInference = new ModelInferences()
                            {
                                CorrelationId = model.CorrelationId,
                                ClientUId = model.ClientUId,
                                DeliveryConstructUId = model.DeliveryConstructUId,
                                ModelName = model.ModelName,
                                FunctionalArea = model.FunctionalArea,
                                InferenceName = "AutoGenerated",
                                InferenceSourceType = "AutoGenerated",
                                Status = "C",
                                Progress = "100",
                                InferenceConfigType = inferenceConfigTemplate.InferenceConfigType,
                                InferenceConfigDetails = new
                                {
                                    MetricColumn = inferenceConfigTemplate.MetricColumn,
                                    DateColumn = inferenceConfigTemplate.DateColumn,
                                    TrendForecast = inferenceConfigTemplate.TrendForecast,
                                    Frequency = inferenceConfigTemplate.Frequency,
                                    Dimensions = inferenceConfigTemplate.Dimensions,
                                    Features = inferenceConfigTemplate.Features,
                                    FeatureCombinations = inferenceConfigTemplate.FeatureCombinations

                                },
                                InferenceResults = templateResult.InferenceResults,
                                CreatedOn = templateResult.CreatedOn.ToString(),
                                CreatedByUser = templateResult.CreatedBy,
                                ModifiedOn = templateResult.ModifiedOn.ToString(),
                                ModifiedByUser = templateResult.ModifiedBy

                            };
                            result.Add(modelInference);
                        }
                    }

                }
                else if (model.IsMultiSource)
                {
                    List<InferenceConfigTemplate> inferenceConfigTemplates =
                        _inferenceEngineDBContext.IEConfigTemplateRepository.GetConfigTemplates(appSettings.Value.Environment, "multidatasource", model.FunctionalArea, null);
                    if (inferenceConfigTemplates.Count > 0)
                    {
                        foreach (var inferenceConfigTemplate in inferenceConfigTemplates)
                        {
                            var templateResult =
                                _inferenceEngineDBContext.IEConfigTemplateRepository.GetDefaultConfigTemplateResult(inferenceConfigTemplate.DefaultTemplateId, inferenceConfigTemplate.InferenceConfigType);
                            ModelInferences modelInference = new ModelInferences()
                            {
                                CorrelationId = model.CorrelationId,
                                ClientUId = model.ClientUId,
                                DeliveryConstructUId = model.DeliveryConstructUId,
                                ModelName = model.ModelName,
                                FunctionalArea = model.FunctionalArea,
                                InferenceName = "AutoGenerated",
                                InferenceSourceType = "AutoGenerated",
                                Status = "C",
                                Progress = "100",
                                InferenceConfigType = inferenceConfigTemplate.InferenceConfigType,
                                InferenceConfigDetails = new
                                {
                                    MetricColumn = inferenceConfigTemplate.MetricColumn,
                                    DateColumn = inferenceConfigTemplate.DateColumn,
                                    TrendForecast = inferenceConfigTemplate.TrendForecast,
                                    Frequency = inferenceConfigTemplate.Frequency,
                                    Dimensions = inferenceConfigTemplate.Dimensions,
                                    Features = inferenceConfigTemplate.Features,
                                    FeatureCombinations = inferenceConfigTemplate.FeatureCombinations

                                },
                                InferenceResults = templateResult.InferenceResults,
                                CreatedOn = templateResult.CreatedOn.ToString(),
                                CreatedByUser = templateResult.CreatedBy,
                                ModifiedOn = templateResult.ModifiedOn.ToString(),
                                ModifiedByUser = templateResult.ModifiedBy

                            };
                            result.Add(modelInference);
                        }
                    }

                }

                data = _inferenceEngineDBContext.InferenceConfigRepository.GetIEConfig(correlationId, inferenceConfigId, applicationId, autogenerated);

                foreach (var item in data)
                {
                    bool autoGenerate = false;
                    if (item.InferenceSourceType == "AutoGenerated")
                    {
                        autoGenerate = true;
                    }
                    string pageInfo = item.InferenceConfigType == "MeasureAnalysis" ? "GenerateNarratives" : "GenerateVolumetric";
                    string status = string.Empty;
                    string progress = string.Empty;
                    string message = string.Empty;
                    List<InferenceResultOuput> inferenceResultOuputs = new List<InferenceResultOuput>();
                    InferenceResultOuput inferenceResultData = new InferenceResultOuput();
                    List<IERequestQueue> requestQueue = _inferenceEngineDBContext.IERequestQueueRepository.GetRequestStatusByConfigId(item.InferenceConfigId, pageInfo);
                    if (requestQueue.Count > 0)
                    {
                        status = requestQueue[0].Status;
                        progress = requestQueue[0].Progress;
                        message = requestQueue[0].Message;
                        if (status == "C")
                        {
                            inferenceResultData = FetchInferenceResult(item.InferenceConfigId, item.InferenceConfigType, item.InferenceSourceType, autoGenerate);
                            inferenceResultOuputs.Add(inferenceResultData);
                        }
                    }
                    ModelInferences modelInference = new ModelInferences()
                    {
                        CorrelationId = model.CorrelationId,
                        ClientUId = model.ClientUId,
                        DeliveryConstructUId = model.DeliveryConstructUId,
                        ModelName = model.ModelName,
                        FunctionalArea = model.FunctionalArea,
                        InferenceName = item.InferenceConfigName,
                        InferenceConfigId = item.InferenceConfigId,
                        InferenceSourceType = item.InferenceSourceType,
                        InferenceConfigType = item.InferenceConfigType,
                        InferenceConfigDetails = new
                        {
                            MetricColumn = item.MetricColumn,
                            DateColumn = item.DateColumn,
                            TrendForecast = item.TrendForecast,
                            Frequency = item.Frequency,
                            Dimensions = item.Dimensions,
                            Features = item.Features,
                            FeatureCombinations = item.FeatureCombinations

                        },
                        InferenceResults = inferenceResultOuputs,
                        Status = status,
                        Progress = progress,
                        CreatedOn = item.CreatedOn.ToString(),
                        CreatedByUser = item.CreatedBy,
                        ModifiedOn = item.ModifiedOn.ToString(),
                        ModifiedByUser = item.ModifiedBy
                    };
                    result.Add(modelInference);
                }
            }

            return result;
        }

        public CustomResponse AutoGenerateInferences(string correlationId, bool regenerate)
        {
            var model = _inferenceEngineDBContext.IEModelRepository.GetIEModel(correlationId);
            if (model != null)
            {
                if (model.SourceName.ToLower() == "file" || model.IsMultiSource)
                {
                    return new CustomResponse("C", "Completed");
                }
                else
                {
                    var config = _inferenceEngineDBContext.InferenceConfigRepository.GetIEConfig(correlationId, null, null, true);
                    bool insertRequests = true;
                    bool isError = false;
                    if (config.Count > 0)
                    {
                        var autoGenConfig = config.FirstOrDefault(x => x.InferenceSourceType == "AutoGenerated");

                        if (autoGenConfig != null)
                        {
                            List<string> pageInfos = new List<string>() { "GenerateVolumetric", "GenerateNarratives" };
                            foreach (var pageinfo in pageInfos)
                            {
                                var request = _inferenceEngineDBContext.IERequestQueueRepository.GetRequestStatusByConfigId(autoGenConfig.InferenceConfigId, pageinfo);

                                if (request != null)
                                {
                                    if (request[0].Status == "I" || request[0].Status == "P" || request[0].Status == "N")
                                    {
                                        return new CustomResponse("P", "InProgress");
                                    }
                                    else if (request[0].Status == "E" || request[0].Status == "C")
                                    {
                                        if (request[0].Status == "E")
                                            isError = true;
                                        insertRequests = false;
                                    }

                                }
                                else
                                {
                                    insertRequests = true;
                                    break;
                                }

                            }


                        }

                    }

                    if (!insertRequests)
                    {
                        if (regenerate)
                        {
                            DeleteAutoGenerateInferences(model.CorrelationId);
                            return InsertAutoGenerateRequests(model) ? new CustomResponse("P", "InProgress")
                                : new CustomResponse("E", "Something went wrong with default configs");
                        }
                        else
                        {
                            if (isError)
                                return new CustomResponse("E", "Something went wrong with Auto Generate Inferences");
                            else
                                return new CustomResponse("C", "Completed");
                        }

                    }
                    else
                    {
                        DeleteAutoGenerateInferences(model.CorrelationId);
                        return InsertAutoGenerateRequests(model) ? new CustomResponse("P", "InProgress")
                                : new CustomResponse("E", "Something went wrong with default configs");
                    }
                }


            }
            else
            {
                return new CustomResponse("E", "Invalid correlationId");
            }






        }

        public void DeleteAutoGenerateInferences(string correlationId)
        {
            var config = _inferenceEngineDBContext.InferenceConfigRepository.GetIEConfig(correlationId, null, null, true);
            if (config.Count > 0)
            {
                var autoGenConfigs = config.Where(x => x.InferenceSourceType == "AutoGenerated").ToList();
                if (autoGenConfigs.Count > 0)
                {
                    foreach (var doc in autoGenConfigs)
                    {
                        // delete saved config
                        _inferenceEngineDBContext.InferenceConfigRepository.DeleteIEConfig(doc.InferenceConfigId, doc.InferenceConfigType);
                        // delete saved results
                        _inferenceEngineDBContext.InferenceConfigRepository.DeleteInferenceResults(doc.InferenceConfigId, doc.InferenceConfigType);
                        // delete queue records
                        _inferenceEngineDBContext.IERequestQueueRepository.DeleteQueueRequestByConfig(doc.InferenceConfigId, doc.InferenceConfigType);

                    }
                }
            }

        }

        public bool InsertAutoGenerateRequests(IEModel model)
        {
            bool isComplete = false;
            if (model != null)
            {
                var configTemplates =
                _inferenceEngineDBContext.IEConfigTemplateRepository.GetConfigTemplates(appSettings.Value.Environment, model.SourceName, model.FunctionalArea, model.Entity);

                if (configTemplates.Count <= 0)
                    return false;
                var metricConfig = configTemplates.FirstOrDefault(x => x.InferenceConfigType == "MeasureAnalysis");

                var col = new JObject();

                col.Add("Metric", metricConfig.MetricColumn);
                col.Add("date", metricConfig.DateColumn);

                bool flag = true;
                List<string> features = new List<string>();
                List<FeatureCombinations> featureCombinations = new List<FeatureCombinations>();

                while (flag)
                {
                    IEREsponse iEREsponse = TriggerFeatureCombination(model.CorrelationId, model.CreatedBy, null, col, false);
                    if (iEREsponse.Status == "C")
                    {
                        var featureCombinationsData = _inferenceEngineDBContext.InferenceConfigRepository.GetFeatureCombination(model.CorrelationId, metricConfig.MetricColumn, metricConfig.DateColumn);
                        features = featureCombinationsData.Features;
                        featureCombinations = featureCombinationsData.FeatureCombinations;
                        flag = false;
                    }
                    else if (iEREsponse.Status == "E")
                    {
                        return false;
                    }
                    else
                    {
                        Thread.Sleep(1000);
                    }

                }


                var ingestedColumns = _inferenceEngineDBContext.InferenceConfigRepository.GetConfig(model.CorrelationId);
                if (configTemplates.Count > 0)
                {
                    var inferenceConfigId = Guid.NewGuid().ToString();
                    foreach (var template in configTemplates)
                    {
                        IESavedConfig inferenceConfig = new IESavedConfig()
                        {
                            CorrelationId = model.CorrelationId,
                            InferenceConfigId = inferenceConfigId,
                            InferenceConfigName = "AutoGenerated",
                            InferenceConfigType = template.InferenceConfigType,
                            InferenceSourceType = "AutoGenerated",
                            MetricColumn = template.MetricColumn,
                            DateColumn = template.DateColumn,
                            TrendForecast = template.TrendForecast,
                            Frequency = template.Frequency,
                            Dimensions = template.InferenceConfigType == "VolumetricAnalysis" ? ingestedColumns.DimensionsList : template.Dimensions,
                            Features = template.InferenceConfigType != "VolumetricAnalysis" ? features : template.Features,
                            FeatureCombinations = template.InferenceConfigType != "VolumetricAnalysis" ? featureCombinations : template.FeatureCombinations,
                            CreatedBy = model.CreatedBy,
                            CreatedOn = DateTime.UtcNow,
                            ModifiedBy = model.ModifiedBy,
                            ModifiedOn = model.ModifiedOn
                        };
                        _inferenceEngineDBContext.InferenceConfigRepository.InsertIEConfig(inferenceConfig);

                        var newRequest = new IERequestQueue()
                        {
                            RequestId = Guid.NewGuid().ToString(),
                            CorrelationId = model.CorrelationId,
                            InferenceConfigId = inferenceConfigId,
                            InferenceConfigType = inferenceConfig.InferenceConfigType,
                            Status = "N",
                            pageInfo = inferenceConfig.InferenceConfigType == "VolumetricAnalysis" ? "GenerateVolumetric" : "GenerateNarratives",
                            Function = inferenceConfig.InferenceConfigType == "VolumetricAnalysis" ? "GenerateVolumetric" : "GenerateNarratives",
                            CreatedBy = model.CreatedBy,
                            CreatedOn = DateTime.UtcNow,
                            ModifiedBy = model.ModifiedBy,
                            ModifiedOn = model.ModifiedOn
                        };
                        _inferenceEngineDBContext.IERequestQueueRepository.IEInsertRequests(newRequest);

                    }
                    isComplete = true;
                }

            }
            return isComplete;
        }

        /// <summary>
        /// Delete Model API
        /// </summary>
        /// <param name="correlationId"></param>
        public string DeleteIEModel(string correlationId)
        {
            var publisedConfig = _inferenceEngineDBContext.InferenceConfigRepository.GetPublishedConfigs(correlationId, null, null);
            var appIds = new List<string>();

            var useCaseMapping = _inferenceEngineDBContext.IEUseCaseRepository.GetUseCaseByCorrelationId(correlationId);

            if (useCaseMapping != null)
                throw new InvalidOperationException("Model cannot be deleted since a Use Case has been created using this model");

            if (publisedConfig.Count > 0)
            {
                appIds = publisedConfig.Select(x => x.ApplicationId).Distinct().ToList();
            }

            foreach (var item in appIds)
            {
                if (!string.IsNullOrEmpty(item))
                {
                    try
                    {
                        var res = SendIEPublishNofication(item, correlationId, "Deleted");
                    }
                    catch (Exception ex)
                    {
                        LOGGING.LogManager.Logger.LogErrorMessage(typeof(InferenceService), nameof(SendIEPublishNofication), ex.Message + "--" + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                        return ex.Message;
                    }
                }
            }
            _inferenceEngineDBContext.IEModelRepository.DeleteIEModel(correlationId);
            _inferenceEngineDBContext.InferenceConfigRepository.DeleteFeatureCombination(correlationId);
            _inferenceEngineDBContext.InferenceConfigRepository.DeleteConfig(correlationId);
            _inferenceEngineDBContext.InferenceConfigRepository.DeleteIEConfig(correlationId);
            _inferenceEngineDBContext.InferenceConfigRepository.DeleteInferenceResults(correlationId);
            _inferenceEngineDBContext.IERequestQueueRepository.DeleteQueueRequest(correlationId);
            _inferenceEngineDBContext.InferenceConfigRepository.DeletePublishedConfig(correlationId);


            return "Success";
        }


        public IEConfig GetDateMeasureAttribute(string correlationId)
        {
            return _inferenceEngineDBContext.InferenceConfigRepository.GetConfig(correlationId);
            //var data = _inferenceEngineDBContext.InferenceConfigRepository.GetConfig(correlationId);
            //if (data != null)
            //{
            //    var encryption = _inferenceEngineDBContext.IEModelRepository.GetIEModel(correlationId);
            //    if (encryption.DBEncryptionRequired)
            //    {

            //        data.FilterValues = _encryptionDecryption.Decrypt(data.FilterValues);
            //    }
            //}
            //return data;
        }

        public IEREsponse TriggerFeatureCombination(string correlationId, string userId, string inferenceConfigId, dynamic dynamicColumns, bool isNewRequest)
        {
            IEREsponse iEREsponse = new IEREsponse();
            IERequestQueue ieRequest = new IERequestQueue();
            IERequestQueue requestQueue = new IERequestQueue();
            IEFeatureCombination featureCombination = new IEFeatureCombination();
            var selectedMetric = string.Empty;
            var selectedDate = string.Empty;
            dynamic selectedData = dynamicColumns;
            dynamic selectedFilter = null;
            foreach (var item in JObject.Parse(dynamicColumns.ToString()))
            {
                JProperty j = item as JProperty;
                if (j.Name == "Metric")
                {
                    selectedMetric = j.Value.ToString();
                }
                if (j.Name == "date")
                {
                    selectedDate = j.Value.ToString();
                }
                if (j.Name == "FilterValues")
                {
                    var values = JsonConvert.DeserializeObject<object>(selectedData.ToString());
                    foreach (var key in new string[] { "Metric", "date" })
                    {

                        values.Remove(key);
                    }
                    selectedFilter = ((values["FilterValues"].ToString(Formatting.None)));
                }
            }
            //var encrytion = _inferenceEngineDBContext.IEModelRepository.GetIEModel(correlationId);
            //if (encrytion.DBEncryptionRequired)
            //{
            //    selectedFilter = _encryptionDecryption.Encrypt(selectedFilter);
            //}

            List<IESavedConfig> iESavedConfigs = _inferenceEngineDBContext.InferenceConfigRepository.GetAllIEConfigs(correlationId, false);
            foreach (var item in iESavedConfigs)
            {
                if (item.DateColumn == selectedDate && item.MetricColumn == selectedMetric && (string.IsNullOrEmpty(inferenceConfigId) || item.InferenceConfigId != inferenceConfigId))
                {
                    throw new Exception("Configuration already created with selected Metric and Date column. Please select a different combination");
                }
            }
            var data = _inferenceEngineDBContext.InferenceConfigRepository.GetFeatureCombination(correlationId, selectedMetric, selectedDate);
            if (data != null && data.FeatureCombinations != null && data.Features.Count > 0)
            {
                requestQueue = _inferenceEngineDBContext.IERequestQueueRepository.GetRequestQueuebyequestId(correlationId, "GetFeatures", data.RequestId);
                if (requestQueue.Status == "C" && isNewRequest)
                {
                    var isDelete = _inferenceEngineDBContext.IERequestQueueRepository.DeleteQueueRequestById(correlationId, "GetFeatures", requestQueue.RequestId);
                    var isFeatureDelte = _inferenceEngineDBContext.InferenceConfigRepository.DeleteFeatureCombination(correlationId, requestQueue.RequestId);
                    if (isDelete && isFeatureDelte)
                    {
                        ieRequest.CorrelationId = correlationId;
                        ieRequest.RequestId = Guid.NewGuid().ToString();
                        ieRequest.RequestStatus = CONSTANTS.New;
                        ieRequest.pageInfo = "GetFeatures";
                        ieRequest.ParamArgs = "{}";
                        ieRequest.Function = "GetFeatures";
                        ieRequest.CreatedBy = userId;
                        ieRequest.CreatedOn = DateTime.Now;
                        ieRequest.CreatedBy = userId;
                        ieRequest.ModifiedOn = DateTime.Now;
                        ieRequest.Status = "N";
                        ieRequest.ParamArgs = dynamicColumns.ToString(Formatting.None);
                        _inferenceEngineDBContext.IERequestQueueRepository.IEInsertRequests(ieRequest);


                        featureCombination.CorrelationId = correlationId;
                        featureCombination.RequestId = ieRequest.RequestId;
                        featureCombination.MetricColumn = selectedMetric;
                        featureCombination.DateColumn = selectedDate;
                        featureCombination.FilterValues = selectedFilter;
                        _inferenceEngineDBContext.InferenceConfigRepository.InsertFeatureCombination(featureCombination);
                        requestQueue = _inferenceEngineDBContext.IERequestQueueRepository.GetRequestQueuebyequestId(correlationId, ieRequest.pageInfo, ieRequest.RequestId);

                    }
                }
            }
            else if (data == null)
            {
                ieRequest.CorrelationId = correlationId;
                ieRequest.RequestId = Guid.NewGuid().ToString();
                ieRequest.RequestStatus = CONSTANTS.New;
                ieRequest.pageInfo = "GetFeatures";
                ieRequest.ParamArgs = "{}";
                ieRequest.Function = "GetFeatures";
                ieRequest.CreatedBy = userId;
                ieRequest.CreatedOn = DateTime.Now;
                ieRequest.CreatedBy = userId;
                ieRequest.ModifiedOn = DateTime.Now;
                ieRequest.Status = "N";
                ieRequest.ParamArgs = dynamicColumns.ToString().Replace(CONSTANTS.r_n, string.Empty);
                _inferenceEngineDBContext.IERequestQueueRepository.IEInsertRequests(ieRequest);


                featureCombination.CorrelationId = correlationId;
                featureCombination.RequestId = ieRequest.RequestId;
                featureCombination.MetricColumn = selectedMetric;
                featureCombination.DateColumn = selectedDate;
                featureCombination.FilterValues = selectedFilter;
                _inferenceEngineDBContext.InferenceConfigRepository.InsertFeatureCombination(featureCombination);
                requestQueue = _inferenceEngineDBContext.IERequestQueueRepository.GetRequestQueuebyequestId(correlationId, ieRequest.pageInfo, ieRequest.RequestId);

            }
            else if (data.FeatureCombinations == null || data.Features.Count == 0)
            {
                requestQueue = _inferenceEngineDBContext.IERequestQueueRepository.GetRequestQueuebyequestId(correlationId, "GetFeatures", data.RequestId);
                if (requestQueue.Status == "E" && isNewRequest)
                {
                    var isDelete = _inferenceEngineDBContext.IERequestQueueRepository.DeleteQueueRequestById(correlationId, "GetFeatures", requestQueue.RequestId);
                    var isFeatureDelte = _inferenceEngineDBContext.InferenceConfigRepository.DeleteFeatureCombination(correlationId, requestQueue.RequestId);
                    if (isDelete && isFeatureDelte)
                    {
                        ieRequest.CorrelationId = correlationId;
                        ieRequest.RequestId = Guid.NewGuid().ToString();
                        ieRequest.RequestStatus = CONSTANTS.New;
                        ieRequest.pageInfo = "GetFeatures";
                        ieRequest.ParamArgs = "{}";
                        ieRequest.Function = "GetFeatures";
                        ieRequest.CreatedBy = userId;
                        ieRequest.CreatedOn = DateTime.Now;
                        ieRequest.CreatedBy = userId;
                        ieRequest.ModifiedOn = DateTime.Now;
                        ieRequest.Status = "N";
                        ieRequest.ParamArgs = dynamicColumns.ToString().Replace(CONSTANTS.r_n, string.Empty);
                        _inferenceEngineDBContext.IERequestQueueRepository.IEInsertRequests(ieRequest);


                        featureCombination.CorrelationId = correlationId;
                        featureCombination.RequestId = ieRequest.RequestId;
                        featureCombination.MetricColumn = selectedMetric;
                        featureCombination.DateColumn = selectedDate;
                        featureCombination.FilterValues = selectedFilter;
                        _inferenceEngineDBContext.InferenceConfigRepository.InsertFeatureCombination(featureCombination);
                        requestQueue = _inferenceEngineDBContext.IERequestQueueRepository.GetRequestQueuebyequestId(correlationId, ieRequest.pageInfo, ieRequest.RequestId);

                    }
                }

            }
            iEREsponse.CorrelationId = requestQueue.CorrelationId;
            iEREsponse.Status = requestQueue.Status;
            iEREsponse.Message = requestQueue.Message;
            iEREsponse.Progress = requestQueue.Progress;
            iEREsponse.RequestId = requestQueue.RequestId;
            iEREsponse.MetricColumn = selectedMetric;
            iEREsponse.DateColumn = selectedDate;
            return iEREsponse;

        }


        public IEFeatureCombination GetFeatureCombination(string correlationId, string metricColumn, string dateColumn)
        {
            IEFeatureCombination iEFeatureCombination = _inferenceEngineDBContext.InferenceConfigRepository.GetFeatureCombination(correlationId, metricColumn, dateColumn);

            if (iEFeatureCombination != null)
            {
                if (iEFeatureCombination.FeatureCombinations.Count > 0)
                {
                    //removing null feature combinations
                    iEFeatureCombination.FeatureCombinations.RemoveAll(x => x.ConnectedFeatures == null);
                }
            }
            return iEFeatureCombination;
        }

        public IEFeatureCombination GetFeatureCombinationOnId(string correlationId, string requestId)
        {
            // var encrypted = _inferenceEngineDBContext.IEModelRepository.GetIEModel(correlationId);

            IEFeatureCombination iEFeatureCombination = _inferenceEngineDBContext.InferenceConfigRepository.GetFeatureCombinationOnId(correlationId, requestId);

            if (iEFeatureCombination != null)
            {
                //if (encrypted.DBEncryptionRequired)
                //{
                //    iEFeatureCombination.FilterValues = _encryptionDecryption.Decrypt(iEFeatureCombination.FilterValues);
                //}
                if (iEFeatureCombination.FeatureCombinations.Count > 0)
                {
                    //removing null feature combinations
                    iEFeatureCombination.FeatureCombinations.RemoveAll(x => x.ConnectedFeatures == null);
                }
            }
            return iEFeatureCombination;
        }

        public IEViewConfigResponse ViewConfiguration(string correlationId, string InferenceConfigId)
        {
            List<IESavedConfig> iESavedConfig = _inferenceEngineDBContext.InferenceConfigRepository.GetSAvedIConfigOnId(correlationId, InferenceConfigId);
            IEViewConfigResponse iEViewConfigResponse = new IEViewConfigResponse();
            string metricCol = string.Empty;
            string dateCol = string.Empty;
            if (iESavedConfig.Count > 0)
            {
                iEViewConfigResponse.SavedConfigValues = new List<IESavedConfig>();
                foreach (var config in iESavedConfig)
                {
                    var encrypted = _inferenceEngineDBContext.IEModelRepository.GetIEModel(correlationId);
                    if (encrypted.DBEncryptionRequired)
                    {
                        if (config.FilterValues != null)
                        {
                            config.FilterValues = _encryptionDecryption.Decrypt(config.FilterValues);
                        }
                        try
                        {
                            if (!string.IsNullOrEmpty(Convert.ToString(config.CreatedBy)))
                            {
                                config.CreatedBy = _encryptionDecryption.Decrypt(Convert.ToString(config.CreatedBy));
                            }
                        }
                        catch (Exception ex) { LOGGING.LogManager.Logger.LogProcessInfo(typeof(InferenceService), nameof(ViewConfiguration) + "User is already decrypted....", ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty); }
                        try
                        {
                            if (!string.IsNullOrEmpty(Convert.ToString(config.ModifiedBy)))
                            {
                                config.ModifiedBy = _encryptionDecryption.Decrypt(Convert.ToString(config.ModifiedBy));
                            }
                        }
                        catch (Exception ex) { LOGGING.LogManager.Logger.LogProcessInfo(typeof(InferenceService), nameof(ViewConfiguration) + "User is already decrypted....", ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty); }
                    }
                    iEViewConfigResponse.SavedConfigValues.Add(config);
                    if (config.InferenceConfigType == "MeasureAnalysis")
                    {
                        metricCol = config.MetricColumn;
                        dateCol = config.DateColumn;
                    }
                }
            }


            IEConfig iEConfig = _inferenceEngineDBContext.InferenceConfigRepository.GetConfig(correlationId);

            iEViewConfigResponse.AllConfigValues = new AllConfigValues();
            if (iEConfig != null)
            {
                iEViewConfigResponse.AllConfigValues.DateColumnList = iEConfig.DateColumnList;
                iEViewConfigResponse.AllConfigValues.DimensionsList = iEConfig.DimensionsList;
                iEViewConfigResponse.AllConfigValues.MetricColumnList = iEConfig.MetricColumnList;

                IEFeatureCombination iEFeatureCombination = _inferenceEngineDBContext.InferenceConfigRepository.GetFeatureCombination(correlationId, metricCol, dateCol);

                iEViewConfigResponse.AllConfigValues.Features = iEFeatureCombination.Features;
                if (iEFeatureCombination != null)
                {
                    if (iEFeatureCombination.FeatureCombinations.Count > 0)
                    {
                        //removing null feature combinations
                        iEFeatureCombination.FeatureCombinations.RemoveAll(x => x.ConnectedFeatures == null);
                    }
                }
                iEViewConfigResponse.AllConfigValues.FeatureCombinations = iEFeatureCombination.FeatureCombinations;

            }

            return iEViewConfigResponse;

        }

        public CustomResponse SaveInferenceConfiguration(IESaveConfigInput iESaveConfigInput)
        {
            dynamic selectedFilter = null;
            if (iESaveConfigInput.MetricConfigInput.FilterValues != null)
            {
                var values = JsonConvert.DeserializeObject<object>(iESaveConfigInput.MetricConfigInput.FilterValues.ToString());

                selectedFilter = ((values.ToString(Formatting.None)));
            }

            List<IESavedConfig> iESavedConfigs = _inferenceEngineDBContext.InferenceConfigRepository.GetAllIEConfigs(iESaveConfigInput.CorrelationId, false);
            bool flag = true;
            bool? firstConfig = false;
            DateTime createdOn = DateTime.UtcNow;
            if (!string.IsNullOrEmpty(iESaveConfigInput.InferenceConfigId))
            {
                var savedIEConfig = iESavedConfigs.Where(x => x.InferenceConfigId == iESaveConfigInput.InferenceConfigId).FirstOrDefault();
                var containsConfig = iESavedConfigs.Where(x => x.InferenceConfigId == iESaveConfigInput.InferenceConfigId).ToList().Count > 0;
                if (containsConfig)
                {
                    List<IERequestQueue> requestQueue = _inferenceEngineDBContext.IERequestQueueRepository.GetRequestStatusByConfigId(iESaveConfigInput.InferenceConfigId, null);
                    if (requestQueue.Count > 0)
                    {
                        bool checkInprogress = requestQueue.Where(x => x.Status != "C" && x.Status != "E").ToList().Count > 0;
                        if (checkInprogress)
                            return new CustomResponse("E", "Cannot edit config as inferences generation is in progress");
                        //delete request queue records
                        _inferenceEngineDBContext.IERequestQueueRepository.DeleteQueueRequestByConfig(iESaveConfigInput.InferenceConfigId, "VolumetricAnalysis");
                        _inferenceEngineDBContext.IERequestQueueRepository.DeleteQueueRequestByConfig(iESaveConfigInput.InferenceConfigId, "MeasureAnalysis");
                    }

                    createdOn = savedIEConfig.CreatedOn;
                    //delete existing config and results
                    firstConfig = iESavedConfigs.Where(x => x.InferenceConfigId == iESaveConfigInput.InferenceConfigId).Select(x => x.isFirstConfig).FirstOrDefault();
                    _inferenceEngineDBContext.InferenceConfigRepository.DeleteIEConfig(iESaveConfigInput.InferenceConfigId, "VolumetricAnalysis");
                    _inferenceEngineDBContext.InferenceConfigRepository.DeleteIEConfig(iESaveConfigInput.InferenceConfigId, "MeasureAnalysis");

                    _inferenceEngineDBContext.InferenceConfigRepository.DeleteInferenceResults(iESaveConfigInput.InferenceConfigId, "VolumetricAnalysis");
                    _inferenceEngineDBContext.InferenceConfigRepository.DeleteInferenceResults(iESaveConfigInput.InferenceConfigId, "MeasureAnalysis");

                }
                else
                {
                    return new CustomResponse("E", "Invalid config Id to edit");
                }
            }
            else
            {
                if (iESavedConfigs.Count > 0)
                {
                    var uniqueConfigIds = iESavedConfigs.Select(x => x.InferenceConfigId).Distinct().ToList();
                    if (uniqueConfigIds.Count >= 5)
                        flag = false;
                    firstConfig = uniqueConfigIds.Count > 1 ? false : true;
                    var nameExist = iESavedConfigs.Where(x => x.InferenceConfigName == iESaveConfigInput.ConfigName).ToList().Count > 0;
                    if (nameExist)
                        return new CustomResponse("E", "InferenceConfig name already exist");
                }
            }






            if (flag)
            {
                var inferenceConfigId = Guid.NewGuid().ToString();
                if (iESaveConfigInput.VolumetricConfigInput != null)
                {
                    var iESavedConfig = new IESavedConfig()
                    {
                        CorrelationId = iESaveConfigInput.CorrelationId,
                        InferenceConfigId = inferenceConfigId,
                        InferenceConfigName = iESaveConfigInput.ConfigName,
                        InferenceConfigType = "VolumetricAnalysis",
                        InferenceSourceType = "Manual",
                        isFirstConfig = firstConfig,
                        DateColumn = iESaveConfigInput.VolumetricConfigInput.DateColumn,
                        TrendForecast = iESaveConfigInput.VolumetricConfigInput.TrendForecast,
                        Frequency = iESaveConfigInput.VolumetricConfigInput.Frequency,
                        Dimensions = iESaveConfigInput.VolumetricConfigInput.Dimensions,
                        DeselectedDimensions = iESaveConfigInput.VolumetricConfigInput.DeselectedDimensions,
                        CreatedBy = iESaveConfigInput.UserId,
                        CreatedOn = createdOn,
                        ModifiedBy = iESaveConfigInput.UserId,
                        ModifiedOn = createdOn
                    };
                    _inferenceEngineDBContext.InferenceConfigRepository.InsertIEConfig(iESavedConfig);
                }

                if (iESaveConfigInput.MetricConfigInput != null)
                {
                    var iESavedConfig = new IESavedConfig()
                    {
                        CorrelationId = iESaveConfigInput.CorrelationId,
                        InferenceConfigId = inferenceConfigId,
                        InferenceConfigName = iESaveConfigInput.ConfigName,
                        InferenceConfigType = "MeasureAnalysis",
                        InferenceSourceType = "Manual",
                        isFirstConfig = firstConfig,
                        MetricColumn = iESaveConfigInput.MetricConfigInput.MetricColumn,
                        DateColumn = iESaveConfigInput.MetricConfigInput.DateColumn,
                        Features = iESaveConfigInput.MetricConfigInput.Features,
                        DeselectedFeatures = iESaveConfigInput.MetricConfigInput.DeselectedFeatures,
                        FeatureCombinations = iESaveConfigInput.MetricConfigInput.FeatureCombinations,
                        DeselectedFeatureCombinations = iESaveConfigInput.MetricConfigInput.DeselectedFeatureCombinations,
                        FilterValues = selectedFilter,
                        CreatedBy = iESaveConfigInput.UserId,
                        CreatedOn = createdOn,
                        ModifiedBy = iESaveConfigInput.UserId,
                        ModifiedOn = createdOn
                    };
                    _inferenceEngineDBContext.InferenceConfigRepository.InsertIEConfig(iESavedConfig);
                }

                return new CustomResponse("C", "Success");
            }
            else
            {
                return new CustomResponse("E", "Cannot add more than 5 configs");
            }



        }

        public CustomResponse DeleteConfig(string inferenceConfigId)
        {
            //delete existing config and results

            _inferenceEngineDBContext.InferenceConfigRepository.DeleteIEConfig(inferenceConfigId, "VolumetricAnalysis");
            _inferenceEngineDBContext.InferenceConfigRepository.DeleteIEConfig(inferenceConfigId, "MeasureAnalysis");

            _inferenceEngineDBContext.InferenceConfigRepository.DeleteInferenceResults(inferenceConfigId, "VolumetricAnalysis");
            _inferenceEngineDBContext.InferenceConfigRepository.DeleteInferenceResults(inferenceConfigId, "MeasureAnalysis");

            //delete request queue records
            _inferenceEngineDBContext.IERequestQueueRepository.DeleteQueueRequestByConfig(inferenceConfigId, "VolumetricAnalysis");
            _inferenceEngineDBContext.IERequestQueueRepository.DeleteQueueRequestByConfig(inferenceConfigId, "MeasureAnalysis");
            //Delte published configs
            _inferenceEngineDBContext.InferenceConfigRepository.DeletePublishedConfigOnId(inferenceConfigId);
            return new CustomResponse("C", "Success");

        }

        public IEREsponse GenerateInference(string correlationId, string inferenceConfigId, string userId, bool isNewRequest)
        {
            int inProgress = 0;
            int completed = 0;
            int error = 0;
            string errorMessage = string.Empty;
            string status = string.Empty;
            string pageInfo = string.Empty;
            IEREsponse iEREsponse = new IEREsponse();
            IERequestQueue ieRequest = new IERequestQueue();
            IERequestQueue requestQueue = new IERequestQueue();
            List<IESavedConfig> data = _inferenceEngineDBContext.InferenceConfigRepository.GetSAvedIConfigOnId(correlationId, inferenceConfigId);
            foreach (var item in data)
            {
                pageInfo = item.InferenceConfigType == "MeasureAnalysis" ? "GenerateNarratives" : "GenerateVolumetric";
                requestQueue = _inferenceEngineDBContext.IERequestQueueRepository.GetRequestQueueOnConfigId(correlationId, inferenceConfigId, item.InferenceConfigType);

                if (requestQueue == null)
                {
                    ieRequest._id = ObjectId.GenerateNewId();
                    ieRequest.CorrelationId = correlationId;
                    ieRequest.RequestId = Guid.NewGuid().ToString();
                    ieRequest.RequestStatus = CONSTANTS.New;
                    ieRequest.pageInfo = pageInfo;
                    ieRequest.ParamArgs = "{}";
                    ieRequest.Function = pageInfo;
                    ieRequest.CreatedBy = userId;
                    ieRequest.CreatedOn = DateTime.Now;
                    ieRequest.CreatedBy = userId;
                    ieRequest.ModifiedOn = DateTime.Now;
                    ieRequest.Status = "N";
                    ieRequest.InferenceConfigId = inferenceConfigId;
                    ieRequest.InferenceConfigType = item.InferenceConfigType;
                    _inferenceEngineDBContext.IERequestQueueRepository.IEInsertRequests(ieRequest);
                    requestQueue = _inferenceEngineDBContext.IERequestQueueRepository.GetRequestQueueOnConfigId(correlationId, inferenceConfigId, item.InferenceConfigType);
                    if (requestQueue.Status == "N" || requestQueue.Status == "O" || requestQueue.Status == "P")
                    {
                        inProgress++;
                    }
                }

                else
                {
                    requestQueue = _inferenceEngineDBContext.IERequestQueueRepository.GetRequestQueueOnConfigId(correlationId, inferenceConfigId, item.InferenceConfigType);
                    if (requestQueue.Status == "N" || requestQueue.Status == "O" || requestQueue.Status == "P")
                    {
                        inProgress++;
                        status = requestQueue.Message;

                    }
                    if (requestQueue.Status == "C")
                    {
                        completed++;
                    }
                    if (requestQueue.Status == "E")
                    {
                        error++;
                        errorMessage = requestQueue.Message;
                    }
                }
            }
            if ((completed == 2 && data.Count == 2) || (completed == 1 && data.Count == 1))
            {
                requestQueue.Status = CONSTANTS.C;
            }
            if (((inProgress == 2 || completed == 1) && data.Count == 2) || (inProgress == 1 && data.Count == 1))
            {
                requestQueue.Status = CONSTANTS.P;
                requestQueue.Message = status;
            }
            if ((error >= 1 && data.Count == 2) || (error == 1 && data.Count == 1))
            {
                requestQueue.Status = CONSTANTS.E;
                requestQueue.Message = errorMessage;
            }
            if ((requestQueue.Status == CONSTANTS.C || requestQueue.Status == CONSTANTS.E) && isNewRequest)
            {
                foreach (var item in data)
                {
                    pageInfo = item.InferenceConfigType == "MeasureAnalysis" ? "GenerateNarratives" : "GenerateVolumetric";

                    var isDelete = _inferenceEngineDBContext.IERequestQueueRepository.DeleteQueueRequestByConfig(item.InferenceConfigId, item.InferenceConfigType);
                    _inferenceEngineDBContext.InferenceConfigRepository.DeleteInferenceResults(item.InferenceConfigId, item.InferenceConfigType);
                    if (isDelete)
                    {
                        ieRequest._id = ObjectId.GenerateNewId();
                        ieRequest.CorrelationId = correlationId;
                        ieRequest.RequestId = Guid.NewGuid().ToString();
                        ieRequest.RequestStatus = CONSTANTS.New;
                        ieRequest.pageInfo = pageInfo;
                        ieRequest.ParamArgs = "{}";
                        ieRequest.Function = pageInfo;
                        ieRequest.CreatedBy = userId;
                        ieRequest.CreatedOn = DateTime.Now;
                        ieRequest.CreatedBy = userId;
                        ieRequest.ModifiedOn = DateTime.Now;
                        ieRequest.Status = "N";
                        ieRequest.InferenceConfigId = inferenceConfigId;
                        ieRequest.InferenceConfigType = item.InferenceConfigType;
                        _inferenceEngineDBContext.IERequestQueueRepository.IEInsertRequests(ieRequest);
                        requestQueue = _inferenceEngineDBContext.IERequestQueueRepository.GetRequestQueueOnConfigId(correlationId, inferenceConfigId, item.InferenceConfigType);
                    }
                }

            }

            iEREsponse.CorrelationId = requestQueue.CorrelationId;
            iEREsponse.Status = requestQueue.Status;
            iEREsponse.Message = requestQueue.Message;
            iEREsponse.Progress = requestQueue.Progress;
            iEREsponse.RequestId = requestQueue.RequestId;
            return iEREsponse;

        }

        public List<IEAppDetails> GetAllAPPDetails(string environment)
        {
            return _inferenceEngineDBContext.IEAppIngerationRepository.GetAllAppDetails(environment);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="iEPublishedConfigs"></param>
        /// <returns></returns>
        public string PublishInference(List<IEPublishedConfigs> iEPublishedConfigs)
        {
            string operationType = "Created";
            if (iEPublishedConfigs.Count() > 0)
            {
                foreach (var data in iEPublishedConfigs)
                {
                    var encrypted = _inferenceEngineDBContext.IEModelRepository.GetIEModel(data.CorrelationId);
                    if (encrypted.DBEncryptionRequired)
                    {
                        try
                        {
                            if (!string.IsNullOrEmpty(Convert.ToString(data.CreatedBy)))
                                data.CreatedBy = _encryptionDecryption.Decrypt(Convert.ToString(data.CreatedBy));
                        }
                        catch (Exception ex)
                        {
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InferenceService), nameof(PublishInference), ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty);
                        }
                        try
                        {
                            if (!string.IsNullOrEmpty(Convert.ToString(data.ModifiedBy)))
                                data.ModifiedBy = _encryptionDecryption.Decrypt(Convert.ToString(data.ModifiedBy));
                        }
                        catch (Exception ex)
                        {
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InferenceService), nameof(PublishInference), ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty);
                        }
                    }
                    if (!CommonUtility.GetValidUser(data.CreatedBy))
                    {
                        throw new Exception("UserName/UserId is Invalid");
                    }
                    if (!CommonUtility.GetValidUser(data.ModifiedBy))
                    {
                        throw new Exception("UserName/UserId is Invalid");
                    }
                    CommonUtility.ValidateInputFormData(data.ApplicationId, "ApplicationId", true);
                    CommonUtility.ValidateInputFormData(data.CorrelationId, "CorrelationId", true);
                    CommonUtility.ValidateInputFormData(data.InferenceConfigId, "InferenceConfigId", true);
                    CommonUtility.ValidateInputFormData(data.InferenceConfigType, "InferenceConfigType", false);
                    if (data.InferenceConfigSubTypes != null)
                        data.InferenceConfigSubTypes.ForEach(x => CommonUtility.ValidateInputFormData(x, "InferenceConfigSubTypes", false));
                }
            }
            iEPublishedConfigs.ForEach(x =>
           { x.CreatedOn = DateTime.Now; x.ModifiedOn = DateTime.Now; x.ModifiedBy = x.CreatedBy; });

            var isExist = _inferenceEngineDBContext.InferenceConfigRepository.GetPublishedConfigOnId(iEPublishedConfigs[0].CorrelationId, iEPublishedConfigs[0].ApplicationId);

            if (isExist)
            {
                var isDelete = _inferenceEngineDBContext.InferenceConfigRepository.DeletePublishedConfig(iEPublishedConfigs[0].CorrelationId);
                if (isDelete)
                {
                    operationType = "Updated";
                }
            }
            _inferenceEngineDBContext.InferenceConfigRepository.InsertIEPublishedConfigs(iEPublishedConfigs);

            var mesg = SendIEPublishNofication(iEPublishedConfigs[0].ApplicationId, iEPublishedConfigs[0].CorrelationId, operationType);
            return mesg;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="applicationId"></param>
        /// <param name="correlationId"></param>
        /// <param name="operation"></param>
        public string SendIEPublishNofication(string applicationId, string correlationId, string operation)
        {
            try
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(DeployModelServices), "SendIEPublishNofication OPERATION-" + operation, "START", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), applicationId, string.Empty, string.Empty, string.Empty);
                var ieModel = _inferenceEngineDBContext.IEModelRepository.GetIEModel(correlationId);
                if (ieModel != null)
                {
                    IEAppNotificationLog appNotificationLog = new IEAppNotificationLog();
                    appNotificationLog.ClientUId = ieModel.ClientUId;
                    appNotificationLog.DeliveryConstructUId = ieModel.DeliveryConstructUId;
                    appNotificationLog.CorrelationId = correlationId;
                    appNotificationLog.CreatedDateTime = ieModel.CreatedOn;
                    appNotificationLog.Entity = ieModel.Entity;
                    appNotificationLog.UseCaseName = string.Empty;
                    appNotificationLog.UserId = ieModel.CreatedBy;
                    appNotificationLog.FunctionalArea = ieModel.FunctionalArea;


                    appNotificationLog.ApplicationId = applicationId;
                    appNotificationLog.OperationType = operation;

                    appNotificationLog.NotificationEventType = "Inferences";
                    string host = appSettings.Value.IngrainAPIUrl;
                    string apiPath = String.Format(CONSTANTS.IECallBackURL, correlationId, applicationId, null);
                    appNotificationLog.CallBackLink = host + apiPath;
                    this.SendAppNotification(appNotificationLog);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(InferenceService), "SendIEPublishNofication", "END", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), applicationId, string.Empty, string.Empty, string.Empty);
                    return "Success";
                }
                return "Model Doesn't exist";
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InferenceService), nameof(SendIEPublishNofication), ex.Message, ex, applicationId, string.Empty, string.Empty, string.Empty);
                return ex.Message;
            }
        }

        public void SendAppNotification(IEAppNotificationLog appNotificationLog)
        {
            appNotificationLog.RequestId = Guid.NewGuid().ToString();
            appNotificationLog.CreatedOn = DateTime.UtcNow.ToString();
           // appNotificationLog.ModifiedOn = DateTime.UtcNow;
            appNotificationLog.RetryCount = 0;
            appNotificationLog.IsNotified = false;

            var app = _inferenceEngineDBContext.IEAppIngerationRepository.GetAppDetailsOnId(appNotificationLog.ApplicationId);

            if (app != null)
            {
                appNotificationLog.Environment = app.Environment;
                if (app.Environment == "PAD")
                {
                    Uri apiUri = new Uri(appSettings.Value.myWizardAPIUrl);
                    string host = apiUri.GetLeftPart(UriPartial.Authority);

                    appNotificationLog.AppNotificationUrl = host + "/" + app.AppNotificationUrl;
                }
                else
                {
                    if (IEGenricVDSUsecases.Contains(appNotificationLog.UseCaseId))// for generic VDS flow in FDS and PAM
                    {
                        Uri apiUri = new Uri(appSettings.Value.VdsURL);
                        string host = apiUri.GetLeftPart(UriPartial.Authority);
                        appNotificationLog.AppNotificationUrl = host + Convert.ToString(appSettings.Value.VDSIEGenericNotificationUrl);
                    }
                    else
                        appNotificationLog.AppNotificationUrl = app.AppNotificationUrl;
                }
            }
            else
            {
                throw new KeyNotFoundException("ApplicationId not found");
            }


            _inferenceEngineDBContext.IEAppIngerationRepository.InsertAppNotification(appNotificationLog);
        }


        public List<ModelInferences> GetInferences(string correlationId, string applicationId, string inferenceConfigId, bool rawresponse = false, bool isGenericAPI=false)
        {
            List<ModelInferences> result = new List<ModelInferences>();
            List<IESavedConfig> data = null;
            IEModel model = _inferenceEngineDBContext.IEModelRepository.GetIEModel(correlationId);
            if (model != null)
            {
                data = _inferenceEngineDBContext.InferenceConfigRepository.GetIEConfig(correlationId, inferenceConfigId, applicationId, false);
                List<IEPublishedConfigs> publishedConfigs = _inferenceEngineDBContext.InferenceConfigRepository.GetPublishedConfigs(correlationId, inferenceConfigId, applicationId);

                if (appSettings.Value.Environment.Equals(CONSTANTS.PADEnvironment))
                {
                    foreach (var item in data)
                    {
                        string pageInfo = item.InferenceConfigType == "MeasureAnalysis" ? "GenerateNarratives" : "GenerateVolumetric";
                        string status = string.Empty;
                        string progress = string.Empty;
                        string message = string.Empty;
                        string rawResult = null;
                        List<InferenceResultOuput> inferenceResultOuputs = new List<InferenceResultOuput>();
                        InferenceResultOuput inferenceResultData = new InferenceResultOuput();
                        List<IERequestQueue> requestQueue = _inferenceEngineDBContext.IERequestQueueRepository.GetRequestStatusByConfigId(item.InferenceConfigId, pageInfo);
                        if (requestQueue.Count > 0)
                        {
                            status = requestQueue[0].Status;
                            progress = requestQueue[0].Progress;
                            message = requestQueue[0].Message;
                            if (status == "C")
                            {

                                if (rawresponse)
                                {
                                    var infresult = _inferenceEngineDBContext.InferenceConfigRepository.GetInferenceResults(item.InferenceConfigId, item.InferenceConfigType);
                                    if (!model.DBEncryptionRequired)
                                        rawResult = infresult.InferenceResults.ToJson(new MongoDB.Bson.IO.JsonWriterSettings() { Indent = false });
                                    else
                                        rawResult = infresult.InferenceResults;


                                }
                                else
                                {
                                    inferenceResultData = FetchInferenceResult(item.InferenceConfigId, item.InferenceConfigType, item.InferenceSourceType, false);
                                    foreach (var publish in publishedConfigs)
                                    {

                                        if (item.InferenceConfigType == publish.InferenceConfigType && item.InferenceConfigId == publish.InferenceConfigId)
                                        {
                                            if (inferenceResultData.MeasureAnalysisInferences != null)
                                            {
                                                var subTypes = publish.InferenceConfigSubTypes;
                                                var measure = inferenceResultData.MeasureAnalysisInferences;
                                                var vol = inferenceResultData.VolumetricInferences;
                                                if (publish.InferenceConfigType == "MeasureAnalysis")
                                                {
                                                    List<Inference> inferences = new List<Inference>();
                                                    foreach (var subTp in subTypes)
                                                    {
                                                        var da = measure.Find(x => x.DisplayName == subTp);
                                                        inferences.Add(da);
                                                    }
                                                    inferenceResultData.MeasureAnalysisInferences = inferences;
                                                }
                                            }
                                            if (inferenceResultData.VolumetricInferences != null)
                                            {
                                                var subTypes = publish.InferenceConfigSubTypes;
                                                var measure = inferenceResultData.MeasureAnalysisInferences;
                                                var vol = inferenceResultData.VolumetricInferences;
                                                if (publish.InferenceConfigType == "VolumetricAnalysis")
                                                {

                                                    List<Inference> inferences = new List<Inference>();
                                                    foreach (var subTp in subTypes)
                                                    {
                                                        var volumetric = vol.Find(x => x.DisplayName == subTp);
                                                        inferences.Add(volumetric);
                                                    }
                                                    inferenceResultData.VolumetricInferences = inferences;
                                                }
                                            }

                                        }
                                    }
                                    inferenceResultOuputs.Add(inferenceResultData);
                                }
                            }
                        }
                        ModelInferences modelInference = new ModelInferences()
                        {
                            CorrelationId = model.CorrelationId,
                            ClientUId = model.ClientUId,
                            DeliveryConstructUId = model.DeliveryConstructUId,
                            ModelName = model.ModelName,
                            FunctionalArea = model.FunctionalArea,
                            InferenceName = item.InferenceConfigName,
                            InferenceConfigId = item.InferenceConfigId,
                            InferenceSourceType = item.InferenceSourceType,
                            InferenceConfigType = item.InferenceConfigType,
                            InferenceConfigDetails = new
                            {
                                MetricColumn = item.MetricColumn,
                                DateColumn = item.DateColumn,
                                TrendForecast = item.TrendForecast,
                                Frequency = item.Frequency,
                                Dimensions = item.Dimensions,
                                Features = item.Features,
                                FeatureCombinations = item.FeatureCombinations

                            },
                            InferenceResults = inferenceResultOuputs,
                            InferenceRawResults = rawResult,
                            Status = status,
                            Progress = progress,
                            CreatedOn = item.CreatedOn.ToString(),
                            CreatedByUser = item.CreatedBy,
                            ModifiedOn = item.ModifiedOn.ToString(),
                            ModifiedByUser = item.ModifiedBy
                        };
                        result.Add(modelInference);
                    }
                }
                else if (isGenericAPI)
                {
                    if (!string.IsNullOrEmpty(applicationId))
                    {
                        if (applicationId != model.ApplicationId)
                            return result;
                    }
                    foreach (var item in data)
                    {
                        string pageInfo = item.InferenceConfigType == "MeasureAnalysis" ? "GenerateNarratives" : "GenerateVolumetric";
                        string status = string.Empty;
                        string progress = string.Empty;
                        string message = string.Empty;
                        string rawResult = null;
                        List<InferenceResultOuput> inferenceResultOuputs = new List<InferenceResultOuput>();
                        InferenceResultOuput inferenceResultData = new InferenceResultOuput();
                        List<IERequestQueue> requestQueue = _inferenceEngineDBContext.IERequestQueueRepository.GetRequestStatusByConfigId(item.InferenceConfigId, pageInfo);
                        if (requestQueue.Count > 0)
                        {
                            status = requestQueue[0].Status;
                            progress = requestQueue[0].Progress;
                            message = requestQueue[0].Message;
                            if (status == "C")
                            {

                                if (rawresponse)
                                {
                                    var infresult = _inferenceEngineDBContext.InferenceConfigRepository.GetInferenceResults(item.InferenceConfigId, item.InferenceConfigType);
                                    if (!model.DBEncryptionRequired)
                                        rawResult = infresult.InferenceResults.ToJson(new MongoDB.Bson.IO.JsonWriterSettings() { Indent = false });
                                    else
                                        rawResult = infresult.InferenceResults;


                                }
                                else
                                {
                                    inferenceResultData = FetchInferenceResult(item.InferenceConfigId, item.InferenceConfigType, item.InferenceSourceType, false);
                                    foreach (var publish in publishedConfigs)
                                    {

                                        if (item.InferenceConfigType == publish.InferenceConfigType && item.InferenceConfigId == publish.InferenceConfigId)
                                        {
                                            if (inferenceResultData.MeasureAnalysisInferences != null)
                                            {
                                                var subTypes = publish.InferenceConfigSubTypes;
                                                var measure = inferenceResultData.MeasureAnalysisInferences;
                                                var vol = inferenceResultData.VolumetricInferences;
                                                if (publish.InferenceConfigType == "MeasureAnalysis")
                                                {
                                                    List<Inference> inferences = new List<Inference>();
                                                    foreach (var subTp in subTypes)
                                                    {
                                                        var da = measure.Find(x => x.DisplayName == subTp);
                                                        inferences.Add(da);
                                                    }
                                                    inferenceResultData.MeasureAnalysisInferences = inferences;
                                                }
                                            }
                                            if (inferenceResultData.VolumetricInferences != null)
                                            {
                                                var subTypes = publish.InferenceConfigSubTypes;
                                                var measure = inferenceResultData.MeasureAnalysisInferences;
                                                var vol = inferenceResultData.VolumetricInferences;
                                                if (publish.InferenceConfigType == "VolumetricAnalysis")
                                                {

                                                    List<Inference> inferences = new List<Inference>();
                                                    foreach (var subTp in subTypes)
                                                    {
                                                        var volumetric = vol.Find(x => x.DisplayName == subTp);
                                                        inferences.Add(volumetric);
                                                    }
                                                    inferenceResultData.VolumetricInferences = inferences;
                                                }
                                            }

                                        }
                                    }
                                    inferenceResultOuputs.Add(inferenceResultData);
                                }
                            }
                        }
                        ModelInferences modelInference = new ModelInferences()
                        {
                            CorrelationId = model.CorrelationId,
                            ClientUId = model.ClientUId,
                            DeliveryConstructUId = model.DeliveryConstructUId,
                            ModelName = model.ModelName,
                            FunctionalArea = model.FunctionalArea,
                            InferenceName = item.InferenceConfigName,
                            InferenceConfigId = item.InferenceConfigId,
                            InferenceSourceType = item.InferenceSourceType,
                            InferenceConfigType = item.InferenceConfigType,
                            StartDate = model.StartDate,
                            EndDate = model.EndDate,
                            EntityName = model.Entity,
                            InferenceConfigDetails = new
                            {
                                MetricColumn = item.MetricColumn,
                                DateColumn = item.DateColumn,
                                TrendForecast = item.TrendForecast,
                                Frequency = item.Frequency,
                                Dimensions = item.Dimensions,
                                Features = item.Features,
                                FeatureCombinations = item.FeatureCombinations

                            },
                            InferenceResults = inferenceResultOuputs,
                            InferenceRawResults = rawResult,
                            Status = status,
                            Progress = progress,
                            CreatedOn = item.CreatedOn.ToString(),
                            CreatedByUser = item.CreatedBy,
                            ModifiedOn = item.ModifiedOn.ToString(),
                            ModifiedByUser = item.ModifiedBy
                        };
                        result.Add(modelInference);
                    }
                }
                else
                {
                    foreach (var item in data)
                    {
                        string pageInfo = item.InferenceConfigType == "MeasureAnalysis" ? "GenerateNarratives" : "GenerateVolumetric";
                        string status = string.Empty;
                        string progress = string.Empty;
                        string message = string.Empty;
                        string rawResult = null;
                        List<InferenceResultOuput> inferenceResultOuputs = new List<InferenceResultOuput>();
                        InferenceResultOuput inferenceResultData = new InferenceResultOuput();
                        dynamic publishedData = null;
                        List<IERequestQueue> requestQueue = _inferenceEngineDBContext.IERequestQueueRepository.GetRequestStatusByConfigId(item.InferenceConfigId, pageInfo);
                        if (requestQueue.Count > 0)
                        {
                            status = requestQueue[0].Status;
                            progress = requestQueue[0].Progress;
                            message = requestQueue[0].Message;
                            if (status == "C")
                            {

                                if (rawresponse)
                                {
                                    var infresult = _inferenceEngineDBContext.InferenceConfigRepository.GetInferenceResults(item.InferenceConfigId, item.InferenceConfigType);
                                    if (!model.DBEncryptionRequired)
                                        rawResult = infresult.InferenceResults.ToJson(new MongoDB.Bson.IO.JsonWriterSettings() { Indent = false });
                                    else
                                        rawResult = infresult.InferenceResults;


                                }
                                else
                                {
                                    inferenceResultData = FetchInferenceResult(item.InferenceConfigId, item.InferenceConfigType, item.InferenceSourceType, false);
                                    foreach (var publish in publishedConfigs)
                                    {

                                        if (item.InferenceConfigType == publish.InferenceConfigType && item.InferenceConfigId == publish.InferenceConfigId)
                                        {
                                            if (inferenceResultData.MeasureAnalysisInferences != null)
                                            {
                                                var subTypes = publish.InferenceConfigSubTypes;
                                                var measure = inferenceResultData.MeasureAnalysisInferences;
                                                var vol = inferenceResultData.VolumetricInferences;
                                                if (publish.InferenceConfigType == "MeasureAnalysis")
                                                {
                                                    List<Inference> inferences = new List<Inference>();
                                                    foreach (var subTp in subTypes)
                                                    {
                                                        var da = measure.Find(x => x.DisplayName == subTp);
                                                        inferences.Add(da);
                                                    }
                                                    inferenceResultData.MeasureAnalysisInferences = inferences;
                                                }
                                            }
                                            if (inferenceResultData.VolumetricInferences != null)
                                            {
                                                var subTypes = publish.InferenceConfigSubTypes;
                                                var measure = inferenceResultData.MeasureAnalysisInferences;
                                                var vol = inferenceResultData.VolumetricInferences;
                                                if (publish.InferenceConfigType == "VolumetricAnalysis")
                                                {

                                                    List<Inference> inferences = new List<Inference>();
                                                    foreach (var subTp in subTypes)
                                                    {
                                                        var volumetric = vol.Find(x => x.DisplayName == subTp);
                                                        inferences.Add(volumetric);
                                                    }
                                                    inferenceResultData.VolumetricInferences = inferences;
                                                }
                                            }

                                        }
                                    }
                                    publishedData = publishedConfigs.FirstOrDefault(i => i.InferenceConfigId == item.InferenceConfigId && i.InferenceConfigType == item.InferenceConfigType);
                                    if (publishedData != null)
                                    {
                                        inferenceResultOuputs.Add(inferenceResultData);
                                    }
                                 }

                                if (publishedData != null)
                                {
                                    ModelInferences modelInference = new ModelInferences()
                                    {
                                        CorrelationId = model.CorrelationId,
                                        ClientUId = model.ClientUId,
                                        DeliveryConstructUId = model.DeliveryConstructUId,
                                        ModelName = model.ModelName,
                                        FunctionalArea = model.FunctionalArea,
                                        InferenceName = item.InferenceConfigName,
                                        InferenceConfigId = item.InferenceConfigId,
                                        InferenceSourceType = item.InferenceSourceType,
                                        InferenceConfigType = item.InferenceConfigType,
                                        StartDate = model.StartDate,
                                        EndDate = model.EndDate,
                                        EntityName = model.Entity,
                                        InferenceConfigDetails = new
                                        {
                                            MetricColumn = item.MetricColumn,
                                            DateColumn = item.DateColumn,
                                            TrendForecast = item.TrendForecast,
                                            Frequency = item.Frequency,
                                            Dimensions = item.Dimensions,
                                            Features = item.Features,
                                            FeatureCombinations = item.FeatureCombinations

                                        },
                                        InferenceResults = inferenceResultOuputs,
                                        InferenceRawResults = rawResult,
                                        Status = status,
                                        Progress = progress,
                                        CreatedOn = item.CreatedOn.ToString(),
                                        CreatedByUser = item.CreatedBy,
                                        ModifiedOn = publishedData.ModifiedOn.ToString(),
                                        ModifiedByUser = item.ModifiedBy
                                    };
                                    result.Add(modelInference);
                                }
                            }
                        }

                    }
                }
            }

            return result;
        }


        public List<IEPublishedConfigs> GetPublishedInferences(string correlationId, string applicationId, string inferenceConfigId)
        {
            List<IEPublishedConfigs> publishedConfigs = _inferenceEngineDBContext.InferenceConfigRepository.GetPublishedConfigs(correlationId, inferenceConfigId, applicationId);
            return publishedConfigs;
        }


        public string AddNewApp(IEAppIntegration appIntegrations)
        {
            appIntegrations.ApplicationID = Guid.NewGuid().ToString();
            appIntegrations.CreatedOn = DateTime.UtcNow.ToString();
            appIntegrations.ModifiedOn = DateTime.UtcNow.ToString();
            appIntegrations.ModifiedByUser = appIntegrations.CreatedByUser;
            if (string.IsNullOrEmpty(appIntegrations.Environment))
            {
                appIntegrations.Environment = "PAD";
            }
            appIntegrations.isDefault = true;
            var result = _inferenceEngineDBContext.IEAppIngerationRepository.GetAppName(appIntegrations.ApplicationName, appIntegrations.Environment);
            if (result.Count > 0)
            {
                throw new Exception(CONSTANTS.ApplicationStatus);
            }

            if (!string.IsNullOrEmpty(appIntegrations.TokenGenerationURL))
            {
                appIntegrations.TokenGenerationURL = _encryptionDecryption.Encrypt(appIntegrations.TokenGenerationURL);
            }
            if (appIntegrations.Credentials != null)
            {
                appIntegrations.Credentials = _encryptionDecryption.Encrypt(JsonConvert.SerializeObject(appIntegrations.Credentials, Formatting.None));
            }
            if (!string.IsNullOrEmpty(appIntegrations.CreatedByUser))
            {
                appIntegrations.CreatedByUser = _encryptionDecryption.Encrypt(appIntegrations.CreatedByUser);
            }
            if (!string.IsNullOrEmpty(appIntegrations.ModifiedByUser))
            {
                appIntegrations.ModifiedByUser = _encryptionDecryption.Encrypt(appIntegrations.ModifiedByUser);
            }
            _inferenceEngineDBContext.IEAppIngerationRepository.InsertAppNotification(appIntegrations);
            return appIntegrations.ApplicationID;

        }

        public CustomResponse CreateUseCase(AddUseCaseInput addUseCaseInput)
        {
            if (string.IsNullOrEmpty(addUseCaseInput.ApplicationId))
            {
                return new CustomResponse("E", "ApplicationId is Mandatory");
            }

            var useCase = _inferenceEngineDBContext.IEUseCaseRepository.GetUseCaseByCorrelationId(addUseCaseInput.CorrelationId);
            if (useCase == null)
            {
                var encrypted = _inferenceEngineDBContext.IEModelRepository.GetIEModel(addUseCaseInput.CorrelationId);
                var newUseCase = new IEUseCase()
                {
                    CorrelationId = addUseCaseInput.CorrelationId,
                    UseCaseName = addUseCaseInput.UseCaseName,
                    UseCaseDescription = addUseCaseInput.UseCaseDescription,
                    ApplicationId = addUseCaseInput.ApplicationId,
                    UseCaseId = Guid.NewGuid().ToString(),
                    CreatedBy = encrypted.DBEncryptionRequired ? _encryptionDecryption.Encrypt(Convert.ToString(addUseCaseInput.UserId)) : addUseCaseInput.UserId,
                    CreatedOn = DateTime.UtcNow,
                    ModifiedBy = encrypted.DBEncryptionRequired ? _encryptionDecryption.Encrypt(Convert.ToString(addUseCaseInput.UserId)) : addUseCaseInput.UserId,
                    ModifiedOn = DateTime.UtcNow
                };

                _inferenceEngineDBContext.IEUseCaseRepository.InsertUseCase(newUseCase);

                return new CustomResponse("C", "Success");
            }
            else
            {
                return new CustomResponse("E", "UseCase already created using this model");
            }

        }


        public CustomResponse DeleteIEUseCase(string useCaseId)
        {
            _inferenceEngineDBContext.IEUseCaseRepository.DeleteUseCase(useCaseId);

            return new CustomResponse("C", "Success");
        }


        public List<UseCaseDetails> GetAllUseCases()
        {
            var useCaseList = new List<UseCaseDetails>();
            var useCases = _inferenceEngineDBContext.IEUseCaseRepository.GetAllUseCases();
            if (useCases.Count > 0)
            {
                foreach (var useCase in useCases)
                {
                    var appDetail = _inferenceEngineDBContext.IEAppIngerationRepository.GetAppDetailsOnId(useCase.ApplicationId);
                    if (appDetail != null)
                    {
                        var encrypted = _inferenceEngineDBContext.IEModelRepository.GetIEModel(useCase.CorrelationId);
                        if (encrypted.DBEncryptionRequired)
                        {
                            try
                            {
                                if (!string.IsNullOrEmpty(Convert.ToString(useCase.CreatedBy)))
                                    useCase.CreatedBy = _encryptionDecryption.Decrypt(Convert.ToString(useCase.CreatedBy));
                            }
                            catch (Exception ex) { LOGGING.LogManager.Logger.LogProcessInfo(typeof(InferenceService), nameof(GetAllUseCases) + "User is already decrypted....", ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty); }
                            try
                            {
                                if (!string.IsNullOrEmpty(Convert.ToString(useCase.ModifiedBy)))
                                    useCase.ModifiedBy = _encryptionDecryption.Decrypt(Convert.ToString(useCase.ModifiedBy));
                            }
                            catch (Exception ex) { LOGGING.LogManager.Logger.LogProcessInfo(typeof(InferenceService), nameof(GetAllUseCases) + "User is already decrypted....", ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty); }
                        }
                        //  var appDetail = _inferenceEngineDBContext.IEAppIngerationRepository.GetAppDetailsOnId(useCase.ApplicationId);
                        var modelDetail = _inferenceEngineDBContext.IEModelRepository.GetIEModel(useCase.CorrelationId);
                        var useCaseDetail = new UseCaseDetails()
                        {
                            UseCaseId = useCase.UseCaseId,
                            UseCaseName = useCase.UseCaseName,
                            UseCaseDescription = useCase.UseCaseDescription,
                            CorrelationId = useCase.CorrelationId,
                            ApplicationId = useCase.ApplicationId,
                            ApplicationName = appDetail.ApplicationName,
                            SourceName = modelDetail.SourceName,
                            Entity = modelDetail.Entity,
                            CreatedBy = useCase.CreatedBy,
                            CreatedOn = useCase.CreatedOn.ToString(),
                            ModifiedBy = useCase.ModifiedBy,
                            ModifiedOn = useCase.ModifiedOn.ToString()
                        };
                        var useCaseConfigs = new List<Config>();

                        var configDetails = _inferenceEngineDBContext.InferenceConfigRepository.GetAllIEConfigs(useCase.CorrelationId, false);
                        var confidIds = configDetails.Select(x => x.InferenceConfigId).Distinct().ToList();
                        foreach (var configId in confidIds)
                        {
                            var useCaseConfig = new Config();
                            var eachConfigItems = configDetails.Where(x => x.InferenceConfigId == configId).ToList();
                            if (eachConfigItems.Count > 0)
                            {
                                foreach (var item in eachConfigItems)
                                {
                                    useCaseConfig.ConfigName = item.InferenceConfigName;
                                    useCaseConfig.InferenceConfigId = item.InferenceConfigId;
                                    if (item.InferenceConfigType == "VolumetricAnalysis")
                                    {
                                        var volConfig = new VolumetricConfig();
                                        volConfig.DateColumn = item.DateColumn;
                                        volConfig.TrendForecast = item.TrendForecast;
                                        volConfig.Dimensions = item.Dimensions;
                                        volConfig.Frequency = item.Frequency;
                                        useCaseConfig.VolumetricConfig = volConfig;
                                    }
                                    if (item.InferenceConfigType == "MeasureAnalysis")
                                    {
                                        var metricConfig = new MetricConfig();
                                        metricConfig.DateColumn = item.DateColumn;
                                        metricConfig.FeatureCombinations = item.FeatureCombinations;
                                        metricConfig.Features = item.Features;
                                        metricConfig.MetricColumn = item.MetricColumn;
                                        useCaseConfig.MetricConfig = metricConfig;
                                    }


                                }
                            }
                            useCaseConfigs.Add(useCaseConfig);
                        }
                        useCaseDetail.InferenceConfigurationsDetails = useCaseConfigs;
                        useCaseList.Add(useCaseDetail);
                    }
                }
            }

            return useCaseList;
        }


        public TrainUseCaseOutput TrainUseCase(TrainUseCaseInput trainUseCaseInput)
        {
            var response = new TrainUseCaseOutput(
                              trainUseCaseInput.ClientUId,
                              trainUseCaseInput.DeliveryConstructUId,
                              trainUseCaseInput.UseCaseId,
                              trainUseCaseInput.ApplicationId,
                              trainUseCaseInput.UserId);
            var useCase = _inferenceEngineDBContext.IEUseCaseRepository.GetUseCase(trainUseCaseInput.UseCaseId);

            if (useCase != null)
            {
                var model = _inferenceEngineDBContext.IEModelRepository.GetIEModel(useCase.CorrelationId);
                if (model != null)
                {
                    var newCorrId = Guid.NewGuid().ToString();
                    var requestQueue = _inferenceEngineDBContext.IERequestQueueRepository.GetRequestByPageInfo(model.CorrelationId, "IngestData");
                    var paramArgs = JsonConvert.DeserializeObject<IEFileUpload>(requestQueue.ParamArgs);
                    var appDetails = _inferenceEngineDBContext.IEAppIngerationRepository.GetAppDetailsOnId(trainUseCaseInput.ApplicationId);
                    if (appDetails == null)
                    {
                        response.Message = CONSTANTS.IsApplicationExist;
                        response.Status = "Error";
                        return response;
                    }
                    if (trainUseCaseInput.DataSource == "Phoenix")
                    {
                        paramArgs.ClientUId = trainUseCaseInput.ClientUId;
                        paramArgs.DeliveryConstructUId = trainUseCaseInput.DeliveryConstructUId;
                        paramArgs.CorrelationId = newCorrId;



                        var dateRange = string.IsNullOrEmpty(useCase.TrainingDataRangeInMonths) ?
                            (string.IsNullOrEmpty(appDetails.TrainingDataRangeInMonths) ? "6" : appDetails.TrainingDataRangeInMonths) : useCase.TrainingDataRangeInMonths;
                        int range = Convert.ToInt32("-" + dateRange);

                        if (paramArgs.metric != "null")
                        {
                            JObject metric = JObject.Parse(paramArgs.metric);

                            metric["startDate"] = DateTime.UtcNow.AddMonths(range).ToString("MM/dd/yyyy");
                            metric["endDate"] = DateTime.UtcNow.ToString("MM/dd/yyyy");

                            paramArgs.metric = JsonConvert.SerializeObject(metric, Formatting.None);
                        }
                        else if (paramArgs.pad != "null")
                        {
                            JObject pad = JObject.Parse(paramArgs.pad);

                            pad["startDate"] = DateTime.UtcNow.AddMonths(range).ToString("MM/dd/yyyy");
                            pad["endDate"] = DateTime.UtcNow.ToString("MM/dd/yyyy");

                            paramArgs.pad = JsonConvert.SerializeObject(pad, Formatting.None);
                        }

                    }
                    else if (trainUseCaseInput.DataSource == "Entity")
                    {

                        paramArgs.ClientUId = trainUseCaseInput.ClientUId;
                        paramArgs.DeliveryConstructUId = trainUseCaseInput.DeliveryConstructUId;
                        paramArgs.CorrelationId = newCorrId;

                        if (paramArgs.metric != "null")
                        {
                            JObject metric = JObject.Parse(paramArgs.metric);

                            metric["startDate"] = trainUseCaseInput.DataSourceDetails.StartDate;
                            metric["endDate"] = trainUseCaseInput.DataSourceDetails.EndDate;

                            paramArgs.metric = JsonConvert.SerializeObject(metric, Formatting.None);
                        }
                        else if (paramArgs.pad != "null")
                        {
                            JObject pad = JObject.Parse(paramArgs.pad);

                            pad["startDate"] = trainUseCaseInput.DataSourceDetails.StartDate;
                            pad["endDate"] = trainUseCaseInput.DataSourceDetails.EndDate;

                            paramArgs.pad = JsonConvert.SerializeObject(pad, Formatting.None);
                        }
                        else if (paramArgs.Customdetails != null)
                        {
                            JObject pad = JObject.Parse(paramArgs.Customdetails.ToString());

                            pad["InputParameters"]["StartDate"] = trainUseCaseInput.DataSourceDetails.StartDate;
                            pad["InputParameters"]["EndDate"] = trainUseCaseInput.DataSourceDetails.EndDate;
                            paramArgs.Customdetails = pad;
                        }
                    }
                    else if (trainUseCaseInput.DataSource == "Custom")
                    {
                        IEAppIntegrationsCredentials appIntegrationsCredentials = new IEAppIntegrationsCredentials()
                        {
                            grant_type = appSettings.Value.Grant_Type,
                            client_id = appSettings.Value.clientId,
                            client_secret = appSettings.Value.clientSecret,
                            resource = appSettings.Value.resourceId
                        };

                        IEAppIntegration appIntegrations = new IEAppIntegration()
                        {
                            ApplicationID = trainUseCaseInput.ApplicationId,
                            Credentials = JsonConvert.SerializeObject(appIntegrationsCredentials)
                        };
                        var tokenurl = _encryptionDecryption.Encrypt(appSettings.Value.token_Url);
                        var credentials = _encryptionDecryption.Encrypt(appIntegrations.Credentials);
                        _inferenceEngineDBContext.IEAppIngerationRepository.UpdateAppIntegration(appIntegrations, appSettings.Value.authProvider, tokenurl, credentials, _encryptionDecryption.Encrypt(appSettings.Value.username));
                        IEParentFile parentDetail = new IEParentFile();
                        var AppPayload = new
                        {
                            AppId = trainUseCaseInput.ApplicationId,
                            HttpMethod = CONSTANTS.POST,
                            AppUrl = trainUseCaseInput.DataSourceDetails.Url,
                            FetchType = trainUseCaseInput.DataSourceDetails.FetchType,
                            InputParameters = trainUseCaseInput.DataSourceDetails.BodyParams
                        };
                        IEFileUpload custom = new IEFileUpload
                        {
                            CorrelationId = newCorrId,
                            ClientUId = trainUseCaseInput.ClientUId,
                            DeliveryConstructUId = trainUseCaseInput.DeliveryConstructUId,
                            Parent = parentFile,
                            Flag = CONSTANTS.Null,
                            mapping = CONSTANTS.Null,
                            mapping_flag = CONSTANTS.False,
                            pad = CONSTANTS.Null,
                            metric = CONSTANTS.Null,
                            InstaMl = CONSTANTS.Null,
                            fileupload = _filepath,
                            Customdetails = AppPayload,

                        };

                        parentDetail.Type = CONSTANTS.Null;
                        parentDetail.Name = CONSTANTS.Null;
                        custom.Parent = parentDetail;
                        _filepath = new IEFilepath();
                        _filepath.fileList = "null";
                        custom.fileupload = _filepath;
                        paramArgs = custom;
                    }
                    else if (trainUseCaseInput.DataSource == "VDS")
                    {
                        Uri apiUri = new Uri(appSettings.Value.VdsURL);
                        string host = apiUri.GetLeftPart(UriPartial.Authority);
                        string SourceURL = host;
                        Uri apiUri1 = new Uri(appSettings.Value.GetVdsDataURL);
                        string apiPath = apiUri1.AbsolutePath;
                        SourceURL = host + apiPath;

                        VDSInputParams VDSInputParams = null;
                        VDSGenericPayloads VDSGenericPayloads = null;
                        IEAppIntegrationsCredentials appIntegrationsCredentials = new IEAppIntegrationsCredentials()
                        {
                            grant_type = appSettings.Value.Grant_Type,
                            client_id = appSettings.Value.clientId,
                            client_secret = appSettings.Value.clientSecret,
                            resource = appSettings.Value.resourceId
                        };

                        IEAppIntegration appIntegrations = new IEAppIntegration()
                        {
                            ApplicationID = trainUseCaseInput.ApplicationId,
                            Credentials = JsonConvert.SerializeObject(appIntegrationsCredentials)
                        };
                        var tokenurl = _encryptionDecryption.Encrypt(appSettings.Value.token_Url);
                        var credentials = _encryptionDecryption.Encrypt(appIntegrations.Credentials);
                        _inferenceEngineDBContext.IEAppIngerationRepository.UpdateAppIntegration(appIntegrations, appSettings.Value.authProvider, tokenurl, credentials, _encryptionDecryption.Encrypt(appSettings.Value.username));

                        IEParentFile parentDetail = new IEParentFile();
                        parentDetail.Type = CONSTANTS.Null;
                        parentDetail.Name = CONSTANTS.Null;
                        _filepath = new IEFilepath();
                        _filepath.fileList = "null";

                        VDSInputParams = new VDSInputParams
                        {
                            ClientID = trainUseCaseInput.ClientUId,
                            E2EUID = trainUseCaseInput.DataSourceDetails.E2EUID,
                            DeliveryConstructID = trainUseCaseInput.DeliveryConstructUId,
                            Environment = appSettings.Value.Environment,
                            RequestType = trainUseCaseInput.DataSourceDetails.RequestType,
                            ServiceType = trainUseCaseInput.DataSourceDetails.ServiceType,
                            StartDate = DateTime.Now.AddYears(-2).ToString(CONSTANTS.DateFormat),
                            EndDate = DateTime.Now.ToString(CONSTANTS.DateFormat),
                        };
                        VDSGenericPayloads = new VDSGenericPayloads
                        {
                            AppId = trainUseCaseInput.ApplicationId,
                            UsecaseID = trainUseCaseInput.UseCaseId,
                            HttpMethod = CONSTANTS.POST,
                            AppUrl = SourceURL,
                            InputParameters = VDSInputParams,
                            AICustom = "False"
                        };
                        IEFileUpload custom = new IEFileUpload
                        {
                            CorrelationId = newCorrId,
                            ClientUId = trainUseCaseInput.ClientUId,
                            DeliveryConstructUId = trainUseCaseInput.DeliveryConstructUId,
                            Parent = parentDetail,
                            Flag = CONSTANTS.Null,
                            mapping = CONSTANTS.Null,
                            mapping_flag = CONSTANTS.False,
                            pad = CONSTANTS.Null,
                            metric = CONSTANTS.Null,
                            InstaMl = CONSTANTS.Null,
                            fileupload = _filepath,
                            Customdetails = VDSGenericPayloads,
                        };
                        paramArgs = custom;
                    }
                    else
                    {
                        throw new InvalidDataException("Invalid datasource");
                    }




                    if (appSettings.Value.isForAllData)
                    {
                        if(!string.IsNullOrEmpty(trainUseCaseInput.UserId))
                            trainUseCaseInput.UserId = _encryptionDecryption.Encrypt(trainUseCaseInput.UserId);
                    }
                    IERequestQueue newRequest = new IERequestQueue()
                    {
                        CorrelationId = newCorrId,
                        RequestId = Guid.NewGuid().ToString(),
                        pageInfo = "AutoTrain",
                        Function = "AutoTrain",
                        Status = "N",
                        ClientUId = trainUseCaseInput.ClientUId,
                        DeliveryConstructUId = trainUseCaseInput.DeliveryConstructUId,
                        ParamArgs = JsonConvert.SerializeObject(paramArgs, Formatting.None),
                        ApplicationId = trainUseCaseInput.ApplicationId,
                        UseCaseId = trainUseCaseInput.UseCaseId,
                        CreatedBy = trainUseCaseInput.UserId,
                        CreatedOn = DateTime.UtcNow,
                        ModifiedBy = trainUseCaseInput.UserId,
                        ModifiedOn = DateTime.UtcNow,
                        IsIEPublish = true,
                        SourceName = trainUseCaseInput.DataSource//for VDS generic Flow
                    };

                    _inferenceEngineDBContext.IERequestQueueRepository.IEInsertRequests(newRequest);

                    response.CorrelationId = newCorrId;
                    response.Status = "I";
                    response.Message = "Training Initiated";
                    response.Progress = "0%";



                }
                else
                {
                    response.Message = CONSTANTS.UsecaseNotAvailable;
                    response.Status = "Error";
                }
            }
            else
            {
                response.Message = CONSTANTS.UsecaseNotAvailable;
                response.Status = "Error";
            }

            return response;


        }


        public TrainUseCaseOutput GetTrainingStatus(string correlationId)
        {
            var request = _inferenceEngineDBContext.IERequestQueueRepository.GetRequestByPageInfo(correlationId, "AutoTrain");

            if (request != null)
            {
                //var encrypted = _inferenceEngineDBContext.IEModelRepository.GetIEModel(correlationId);
                if (appSettings.Value.isForAllData)
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(Convert.ToString(request.CreatedBy)))
                            request.CreatedBy = _encryptionDecryption.Decrypt(Convert.ToString(request.CreatedBy));
                    }
                    catch (Exception ex) { LOGGING.LogManager.Logger.LogProcessInfo(typeof(InferenceService), nameof(GetTrainingStatus) + "User is already decrypted....", ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty); }
                }
                var response = new TrainUseCaseOutput()
                {
                    ClientUId = request.ClientUId,
                    DeliveryConstructUId = request.DeliveryConstructUId,
                    CorrelationId = correlationId,
                    UseCaseId = request.UseCaseId,
                    UserId = request.CreatedBy,
                    Status = request.Status,
                    Message = request.Message,
                    Progress = request.Progress
                };

                return response;
            }
            else
            {
                throw new InvalidDataException("CorrelationId not found");
            }
        }


        public List<object> GetIEIngestedData(string correlationId, int noOfRecord, string datecolumn)
        {
            List<dynamic> lstInputData = new List<dynamic>();
            List<dynamic> incrementalData = new List<dynamic>();
            var dbData = _inferenceEngineDBContext.IEModelRepository.GetIEIngestedRecords(correlationId);
            var model = _inferenceEngineDBContext.IEModelRepository.GetIEModel(correlationId);
            bool DBEncryptionRequired = model.DBEncryptionRequired;
            if (dbData.Count > 0)
            {
                for (int i = 0; i < dbData.Count; i++)
                {
                    try
                    {
                        if (DBEncryptionRequired)
                        {
                            dbData[i][CONSTANTS.Input_Data] = _encryptionDecryption.Decrypt(dbData[i][CONSTANTS.Input_Data].ToString());
                        }
                    }
                    catch (Exception ex)
                    {
                        if (DBEncryptionRequired)
                        {
                            dbData[i][CONSTANTS.Input_Data] = AesProvider.Decrypt(dbData[i][CONSTANTS.Input_Data].ToString(), "YmFzZTY0ZW5jb2Rlc3RyaQ==", "YmFzZTY0ZW5jb2Rlc3RyaQ==");
                        }

                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(InferenceService), nameof(GetIEIngestedData) + dbData[i][CONSTANTS.Input_Data].ToString(), ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty);
                    }
                    //if (DBEncryptionRequired)
                    var json = dbData[i][CONSTANTS.Input_Data].ToString();
                    lstInputData.AddRange(JsonConvert.DeserializeObject<List<object>>(json));
                }
                if (!string.IsNullOrEmpty(datecolumn))
                {
                    DateTime fromDt = DateTime.Parse(datecolumn);
                    foreach (var data in lstInputData)
                    {
                        DateTime dateVal = DateTime.Parse(data.DateColumn.ToObject<string>());
                        if (dateVal >= fromDt && dateVal < DateTime.Now)
                        {
                            incrementalData.Add(data);
                        }
                    }

                    return incrementalData.Take(noOfRecord).ToList();
                }
                else
                {
                    return lstInputData.Take(noOfRecord).ToList();
                }

            }
            return lstInputData;

        }


        /// <summary>
        /// View Uploaded Excel Data
        /// </summary>
        /// <param name="correlationId">Correlation Id</param>
        /// <returns></returns>
        public List<object> ViewUploadedData(string correlationId, int Precision)
        {
            BsonArray inputD = new BsonArray();
            var inputData = _inferenceEngineDBContext.InferenceConfigRepository.GetIEPreprocessedData(correlationId);
            var encryption = _inferenceEngineDBContext.InferenceConfigRepository.GetIEModelEncryption(correlationId);
            bool DBEncryptionRequired = encryption.DBEncryptionRequired;
            var data = "";
            var excelData = new List<object>();
            if (inputData.Count > 0)
            {
                for (int i = 0; i < inputData.Count; i++)
                {
                    if (DBEncryptionRequired)
                    {
                        try
                        {
                            data = _encryptionDecryption.Decrypt(inputData[i][CONSTANTS.Input_Data].AsString);
                        }
                        catch (Exception ex)
                        {
                            data= inputData[i][CONSTANTS.Input_Data].ToString();
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(InferenceService), nameof(ViewConfiguration) + data, ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty);

                        }
                    }
                    else
                    {
                        data = inputData[i][CONSTANTS.Input_Data].ToString();
                    }
                    //List<object> count = JsonConvert.DeserializeObject<List<object>>(data);
                    //excelData.AddRange(count);
                    //if (excelData.Count > 100)
                    //    break;
                    inputD.AddRange(BsonSerializer.Deserialize<BsonArray>(data));
                    if (inputD.Count > 100)
                        break;
                }
            }
            excelData = CommonUtility.GetDataAfterDecimalPrecision(inputD, Precision, 100, false);
            return excelData.Take(100).ToList();
        }

    }




}



