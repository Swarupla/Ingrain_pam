using Accenture.MyWizard.Ingrain.BusinessDomain.Common;
using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
using Accenture.MyWizard.Ingrain.DataAccess;
using Accenture.MyWizard.Ingrain.DataModels.Models;
using Accenture.MyWizard.Shared.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;


namespace Accenture.MyWizard.Ingrain.BusinessDomain.Services
{
    public class DataSetsService : IDataSetsService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IngrainAppSettings appSettings;
        private MongoClient _mongoClient;
        private IMongoDatabase _database;
        DatabaseProvider databaseProvider;
        private IEncryptionDecryption _encryptionDecryption;
        public DataSetsService(DatabaseProvider db,
                               IHttpContextAccessor httpContextAccessor,
                               IOptions<IngrainAppSettings> settings,
                               IServiceProvider serviceProvider)
        {
            appSettings = settings.Value;
            _httpContextAccessor = httpContextAccessor;

            databaseProvider = db;
            _mongoClient = databaseProvider.GetDatabaseConnection();
            var dataBaseName = MongoUrl.Create(appSettings.connectionString).DatabaseName;
            _database = _mongoClient.GetDatabase(dataBaseName);
            _encryptionDecryption = serviceProvider.GetService<IEncryptionDecryption>();
        }



        public async Task SaveFileAsync()
        {
            string DataSetName = string.Empty;
            string ClientUId = string.Empty;
            string DeliveryConstructUId = string.Empty;
            string UserId = string.Empty;
            string Category = string.Empty;
            string EncryptionRequired = string.Empty;
            string SourceName = string.Empty;
            string IsPrivate = string.Empty;
            double sizeInMB = 0;
            string filePath = string.Empty;
            DataSetInfo dataSetInfo = new DataSetInfo();
            dataSetInfo.DataSetUId = Guid.NewGuid().ToString();



            var boundary = RequestHelpers.GetBoundary(_httpContextAccessor.HttpContext.Request.ContentType);
            var reader = new MultipartReader(boundary, _httpContextAccessor.HttpContext.Request.Body);
            var section = await reader.ReadNextSectionAsync();
            while (section != null)
            {
                var hasContentDispositionHeader = ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition);

                if (hasContentDispositionHeader)
                {
                    if (RequestHelpers.HasFileContentDisposition(contentDisposition))
                    {
                        var fileSection = section.AsFileSection();
                        var originalFilename = fileSection.FileName;

                        filePath = System.IO.Path.Combine(filePath, appSettings.DataSetPath);
                        System.IO.Directory.CreateDirectory(filePath);
                        var tempFilename = Path.Combine(filePath, dataSetInfo.DataSetUId + "_" + originalFilename);
                        using (var stream = new FileStream(tempFilename, FileMode.CreateNew))
                        {
                            const int chunkSize = 1024;
                            var buffer = new byte[chunkSize];
                            var bytesRead = 0;
                            do
                            {
                                bytesRead = await fileSection.FileStream.ReadAsync(buffer, 0, buffer.Length);
                                await stream.WriteAsync(buffer, 0, bytesRead);
                                sizeInMB += 0.001;
                            } while (bytesRead > 0);
                        }
                        dataSetInfo.DataSizeInMB = Convert.ToString(sizeInMB);
                        dataSetInfo.SourceName = "File";
                        dataSetInfo.SourceDetails = new DataSetSourceDetails();
                        //dataSetInfo.SourceDetails.FilePath = new List<string>();
                        //dataSetInfo.SourceDetails.FilePath.Add(tempFilename);
                    }
                    else
                    {
                        var fileSection = section.AsFormDataSection();

                        switch (fileSection.Name)
                        {
                            case "ClientUId":
                                ClientUId = await fileSection.GetValueAsync();
                                break;
                            case "DeliveryConstructUId":
                                DeliveryConstructUId = await fileSection.GetValueAsync();
                                break;
                            case "UserId":
                                UserId = await fileSection.GetValueAsync();
                                break;
                            case "DataSetName":
                                DataSetName = await fileSection.GetValueAsync();
                                break;
                            case "IsPrivate":
                                IsPrivate = await fileSection.GetValueAsync();
                                break;
                            case "Category":
                                Category = await fileSection.GetValueAsync();
                                break;
                            case "EncryptionRequired":
                                EncryptionRequired = await fileSection.GetValueAsync();
                                break;
                            case "SourceName":
                                SourceName = await fileSection.GetValueAsync();
                                break;
                            default:
                                break;
                        }







                    }

                }

                section = await reader.ReadNextSectionAsync();
            }

        }

        public void InsertDataSetInfo(DataSetInfoDto dataSetInfo)
        {
            var dataSetCollection = _database.GetCollection<DataSetInfoDto>("DataSetInfo");
            var filter = Builders<DataSetInfoDto>.Filter.Where(x => x.DataSetUId == dataSetInfo.DataSetUId);
            var result = dataSetCollection.Find(filter).FirstOrDefault();
            if (result != null)
            {
                dataSetCollection.DeleteOne(filter);
            }
            else
            {
                dataSetInfo.CreatedOn = DateTime.UtcNow.ToString();
                dataSetInfo.ModifiedOn = DateTime.UtcNow.ToString();
                dataSetCollection.InsertOne(dataSetInfo);
            }
        }

        public void InsertIngestDataSetRequest(DataSetInfoDto dataSetInfoDto)
        {
            IngrainRequestQueue ingrainRequestQueue = new IngrainRequestQueue
            {
                _id = Guid.NewGuid().ToString(),
                CorrelationId = null,
                DataSetUId = dataSetInfoDto.DataSetUId,
                ClientId = dataSetInfoDto.ClientUId,
                DeliveryconstructId = dataSetInfoDto.DeliveryConstructUId,
                RequestId = Guid.NewGuid().ToString(),
                ProcessId = null,
                Status = null,
                ModelName = null,
                RequestStatus = CONSTANTS.New,
                RetryCount = 0,
                ProblemType = null,
                Message = null,
                UniId = null,
                Progress = null,
                pageInfo = "IngestDataSet",
                ParamArgs = null,
                Function = "IngestDataSet",
                CreatedByUser = dataSetInfoDto.CreatedBy,
                CreatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                ModifiedByUser = dataSetInfoDto.ModifiedBy,
                ModifiedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                LastProcessedOn = null
            };

            InsertSSAIRequest(ingrainRequestQueue);
        }
        public void InsertSSAIRequest(IngrainRequestQueue ingrainRequestQueue)
        {
            var requestCollection = _database.GetCollection<IngrainRequestQueue>(CONSTANTS.SSAIIngrainRequests);
            requestCollection.InsertOne(ingrainRequestQueue);
        }

        public void UpdateDataSetInfo(DataSetInfoDto dataSetInfoDto)
        {
            var dataSetCollection = _database.GetCollection<DataSetInfoDto>(CONSTANTS.DataSetInfo);
            var filter = Builders<DataSetInfoDto>.Filter.Where(x => x.DataSetUId == dataSetInfoDto.DataSetUId);
            var result = dataSetCollection.Find(filter).FirstOrDefault();

            if (result != null)
            {
                var updateBuilder = Builders<DataSetInfoDto>.Update.Set(x => x.Progress, dataSetInfoDto.Progress)
                                                                   .Set(x => x.Status, dataSetInfoDto.Status)
                                                                   .Set(x => x.Message, dataSetInfoDto.Message)
                                                                   .Set(x => x.SourceDetails, dataSetInfoDto.SourceDetails)
                                                                   .Set(x => x.ModifiedOn, DateTime.UtcNow.ToString());
                dataSetCollection.UpdateOne(filter, updateBuilder);
            }

        }



        public DataSetInfoDto GetDataSetInfo(string dataSetUId)
        {
            var dataSetCollection = _database.GetCollection<DataSetInfoDto>(CONSTANTS.DataSetInfo);
            var filter = Builders<DataSetInfoDto>.Filter.Where(x => x.DataSetUId == dataSetUId);
            var result = dataSetCollection.Find(filter).FirstOrDefault();
            if (result != null)
            {
                return result;
            }
            else
            {
                return null;
            }
        }


        public async Task<DataSetInfoDto> SaveFileChunks(HttpContext httpContext)
        {
            IFormCollection collection = httpContext.Request.Form;
            var DBEncryption = Convert.ToBoolean(collection["EncryptionRequired"]);
            bool encryptDB = false;

            if (appSettings.isForAllData == true)
            {
                if (appSettings.DBEncryption == true)
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
                if (appSettings.DBEncryption == true && DBEncryption == true)
                {

                    encryptDB = true;
                }
                else
                {
                    encryptDB = false;
                }

            }
            DataSetInfoDto dataSetInfo = new DataSetInfoDto();

            if (!CommonUtility.GetValidUser(Convert.ToString(collection["UserId"])))
            {
                throw new Exception("UserName/UserId is Invalid");
            }
            if (string.IsNullOrWhiteSpace(Convert.ToString(collection["DataSetUId"])))
            {
                List<DataSetInfo> dataSetInfos = await GetDataSetsList(collection["ClientUId"], collection["DeliveryConstructUId"], collection["UserId"], "File");
                List<string> dataSetNames = dataSetInfos.Select(x => x.DataSetName).ToList();
                bool nameAlreadyPresent = dataSetNames.Contains(collection["DataSetName"].ToString(), StringComparer.OrdinalIgnoreCase);
                if (nameAlreadyPresent)
                    throw new InvalidDataException("DataSet name already present");
            }

            try
            {
                bool firstChunk = true;

                if (string.IsNullOrWhiteSpace(Convert.ToString(collection["DataSetUId"])))
                {
                    string UserId = collection["UserId"];
                    if (encryptDB)
                    {
                        if(!string.IsNullOrEmpty(Convert.ToString(collection["UserId"])))
                            UserId = _encryptionDecryption.Encrypt(Convert.ToString(collection["UserId"]));
                    }
                    dataSetInfo.DataSetUId = Guid.NewGuid().ToString();
                    dataSetInfo.DataSetName = collection["DataSetName"];
                    dataSetInfo.IsPrivate = Convert.ToBoolean(collection["IsPrivate"]);
                    dataSetInfo.ClientUId = collection["ClientUId"];
                    dataSetInfo.DeliveryConstructUId = collection["DeliveryConstructUId"];
                    dataSetInfo.CreatedBy = UserId;
                    dataSetInfo.ModifiedBy = UserId;
                    dataSetInfo.Category = collection["Category"];
                    dataSetInfo.DBEncryptionRequired = encryptDB;//Convert.ToBoolean(collection["EncryptionRequired"]);
                    dataSetInfo.SourceName = collection["SourceName"];
                    dataSetInfo.Status = "I";
                    dataSetInfo.Progress = "10";
                    dataSetInfo.Message = "File upload is in progress";

                }
                else
                {
                    firstChunk = false;
                    dataSetInfo = GetDataSetInfo(Convert.ToString(collection["DataSetUId"]));
                    if (dataSetInfo == null)
                    {
                        throw new KeyNotFoundException("DataSetUId not found");
                    }
                }


                string filePath = appSettings.UploadFilePath;
                var fileCollection = httpContext.Request.Form.Files;
               //var filename = fileCollection[0].FileName + ".part_1.1";
                if (CommonUtility.ValidateMyDataSourceFileUploaded(fileCollection))
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataSetsService), nameof(SaveFileChunks), CONSTANTS.START, "FILE NAME", fileCollection[0].FileName, string.Empty, string.Empty);
                    throw new FormatException(Resource.IngrainResx.InValidFileName);
                }

                foreach (IFormFile file in fileCollection)
                {
                    var FileDataContent = file;
                    if (FileDataContent != null && FileDataContent.Length > 0)
                    {

                        filePath = System.IO.Path.Combine(filePath, appSettings.DataSetPath);
                        System.IO.Directory.CreateDirectory(filePath);

                        // take the input stream, and save it to a temp folder using  
                        // the original file.part name posted  

                        var fileName = Path.GetFileName(FileDataContent.FileName);
                        bool isValid = ValidateFilePattern(fileName, firstChunk);
                        if (!isValid)
                        {
                            throw new FormatException("Invalid File Pattern");
                        }


                        string path = Path.Combine(filePath, dataSetInfo.DataSetUId + "_" + fileName);
                        try
                        {
                            using (var fileStream = new FileStream(path, FileMode.Create))
                            {
                                await file.CopyToAsync(fileStream);
                            }
                            // Once the file part is saved, see if we have enough to merge it  

                            var ismerged = await MergeFile(path);
                            if (firstChunk)
                            {
                                InsertDataSetInfo(dataSetInfo);
                            }
                            if (ismerged)
                            {
                                string partToken = ".part_";
                                string baseFilePath = path.Substring(0, path.IndexOf(partToken));
                                dataSetInfo.SourceDetails = new DataSetSourceDetails();
                                dataSetInfo.SourceDetails.FileDetail = new List<FilesInfo>();
                                var fileInfo = new FilesInfo();
                                //fileInfo.FilePath = encryptDB ? Path.Combine(baseFilePath + ".enc") : baseFilePath;
                                fileInfo.FilePath = appSettings.IsAESKeyVault &&  encryptDB && appSettings.Environment == CONSTANTS.PADEnvironment ? Path.Combine(baseFilePath + ".enc") : baseFilePath;
                                fileInfo.FileName = fileName.Substring(0, fileName.IndexOf(partToken));
                                if (appSettings.IsAESKeyVault && appSettings.Environment == CONSTANTS.PADEnvironment && encryptDB)
                                    fileInfo.FileName = fileInfo.FileName + ".enc";
                                dataSetInfo.SourceDetails.FileDetail.Add(fileInfo);
                                dataSetInfo.Progress = "50";
                                dataSetInfo.Status = "P";
                                dataSetInfo.Message = "File upload completed";
                                UpdateDataSetInfo(dataSetInfo);
                                InsertIngestDataSetRequest(dataSetInfo);
                            }
                            else
                            {
                                string partToken = ".part_";
                                string baseFilePath = path.Substring(0, path.IndexOf(partToken));
                                dataSetInfo.SourceDetails = new DataSetSourceDetails();
                                dataSetInfo.SourceDetails.FileDetail = new List<FilesInfo>();
                                var fileInfo = new FilesInfo();
                                //fileInfo.FilePath = encryptDB ? Path.Combine(baseFilePath + ".enc") : baseFilePath;
                                fileInfo.FilePath = appSettings.IsAESKeyVault && encryptDB && appSettings.Environment == CONSTANTS.PADEnvironment ? Path.Combine(baseFilePath + ".enc") : baseFilePath;
                                fileInfo.FileName = fileName.Substring(0, fileName.IndexOf(partToken));
                                if (appSettings.IsAESKeyVault && appSettings.Environment == CONSTANTS.PADEnvironment && encryptDB)
                                    fileInfo.FileName = fileInfo.FileName + ".enc";
                                dataSetInfo.SourceDetails.FileDetail.Add(fileInfo);


                                string trailingTokens = fileName.Substring(fileName.IndexOf(partToken) + partToken.Length);
                                int FileIndex = 0;
                                int FileCount = 0;
                                int.TryParse(trailingTokens.Substring(0, trailingTokens.IndexOf(".")), out FileIndex);
                                int.TryParse(trailingTokens.Substring(trailingTokens.IndexOf(".") + 1), out FileCount);
                                int perc = (int)(((double)FileIndex / FileCount) * 50);
                                dataSetInfo.Progress = Convert.ToString(perc);
                                dataSetInfo.Status = "I";
                                dataSetInfo.Message = "File upload in progress";
                                UpdateDataSetInfo(dataSetInfo);
                            }
                        }
                        catch (IOException ex)
                        {
                            dataSetInfo.Progress = "50";
                            dataSetInfo.Status = "E";
                            dataSetInfo.Message = "Error uploading file -" + ex.Message;
                            UpdateDataSetInfo(dataSetInfo);
                            LOGGING.LogManager.Logger.LogErrorMessage(typeof(DataSetsService), nameof(SaveFileChunks), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                dataSetInfo.Progress = "50";
                dataSetInfo.Status = "E";
                dataSetInfo.Message = "Error uploading file -" + ex.Message;
                UpdateDataSetInfo(dataSetInfo);
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(DataSetsService), nameof(SaveFileChunks), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
            dataSetInfo.CreatedBy = Convert.ToString(collection["UserId"]);
            dataSetInfo.ModifiedBy = Convert.ToString(collection["UserId"]);
            dataSetInfo.SourceDetails = null;// dont return filepath
            return dataSetInfo;

        }

        public bool ValidateFilePattern(string fileName, bool firstChunk)
        {
            try
            {
                string partToken = ".part_";
                string baseFileName = fileName.Substring(0, fileName.IndexOf(partToken));
                string trailingTokens = fileName.Substring(fileName.IndexOf(partToken) + partToken.Length);
                int FileIndex = 0;
                int FileCount = 0;
                int.TryParse(trailingTokens.Substring(0, trailingTokens.IndexOf(".")), out FileIndex);
                int.TryParse(trailingTokens.Substring(trailingTokens.IndexOf(".") + 1), out FileCount);
                if (firstChunk)
                {
                    if (FileIndex != 1)
                    {
                        return false;
                    }
                }
                else
                {
                    if (FileIndex > FileCount)
                    {
                        return false;
                    }
                }


            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(DataSetsService), nameof(ValidateFilePattern), ex.Message + "-FileName-" + fileName, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return false;

            }

            return true;
        }


        /// <summary>  
        /// original name + ".part_N.X" (N = file part number, X = total files)  
        /// Objective = enumerate files in folder, look for all matching parts of  
        /// split file. If found, merge and return true.  
        /// </summary>  
        /// <param name="FileName"></param>  
        /// <returns></returns>  
        public async Task<bool> MergeFile(string FileName)
        {
            bool result = false;
            // parse out the different tokens from the filename according to the convention  
            string partToken = ".part_";
            string baseFileName = FileName.Substring(0, FileName.IndexOf(partToken));
            string trailingTokens = FileName.Substring(FileName.IndexOf(partToken) + partToken.Length);
            int FileIndex = 0;
            int FileCount = 0;
            int.TryParse(trailingTokens.Substring(0, trailingTokens.IndexOf(".")), out FileIndex);
            int.TryParse(trailingTokens.Substring(trailingTokens.IndexOf(".") + 1), out FileCount);
            // get a list of all file parts in the temp folder  
            string Searchpattern = Path.GetFileName(baseFileName) + partToken + "*";
            string[] FilesList = Directory.GetFiles(Path.GetDirectoryName(FileName), Searchpattern);

            // only proceed if we have received all the file chunks  
            if (FilesList.Length == FileCount)
            {
                if (File.Exists(baseFileName))
                    File.Delete(baseFileName);

                List<FileDetails> MergeList = new List<FileDetails>();
                foreach (string File in FilesList)
                {
                    FileDetails sFile = new FileDetails();
                    sFile.FileName = File;
                    baseFileName = File.Substring(0, File.IndexOf(partToken));
                    trailingTokens = File.Substring(File.IndexOf(partToken) + partToken.Length);
                    int.TryParse(trailingTokens.
                       Substring(0, trailingTokens.IndexOf(".")), out FileIndex);
                    sFile.FileOrder = FileIndex;
                    MergeList.Add(sFile);
                }
                // sort by the file-part number to ensure we merge back in the correct order  
                var MergeOrder = MergeList.OrderBy(s => s.FileOrder).ToList();
                //using (FileStream FS = new FileStream(baseFileName, FileMode.Create))
                //{

                //    // merge each file chunk back into one contiguous file stream  
                //    foreach (var chunk in MergeOrder)
                //    {
                //        using (FileStream fileChunk =
                //              new FileStream(chunk.FileName, FileMode.Open))
                //        {
                //            await fileChunk.CopyToAsync(FS);
                //            filestream = FS;
                //        }



                //    }



                //}
                await _encryptionDecryption.EncryptDataSetFile(MergeOrder, baseFileName);
                foreach (var chunk in MergeOrder)
                {
                    if (File.Exists(chunk.FileName))
                        File.Delete(chunk.FileName);
                }

                // var file = @"C:\mnt\myWizard-Phoenix\IngrAIn_Shared\App_Data\0eb12b45-081c-41b5-8a49-711b5acac535_insurance.xlsx";


                //_encryptionDecryption.DecryptFiles(baseFileName, @"C:\Users\shreya.keshava\Downloads\123_" + Guid.NewGuid().ToString() + ".csv");


                result = true;
            }
            return result;
        }


        public async Task<List<DataSetInfo>> GetDataSetsList(string clientUId, string deliveryConstructUId, string userId, string sourceName)
        {
            List<DataSetInfo> dataSetInfos = await GetDataSets(clientUId, deliveryConstructUId, userId, sourceName);

            //remove file paths from payload
            if (dataSetInfos.Count > 0)
            {
                foreach (var dataset in dataSetInfos)
                {
                    if (dataset.DBEncryptionRequired)
                    {
                        try
                        {
                            if (!string.IsNullOrEmpty(Convert.ToString(dataset.CreatedBy)))
                                dataset.CreatedBy = _encryptionDecryption.Decrypt(Convert.ToString(dataset.CreatedBy));
                        }
                        catch (Exception ex) { LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataSetsService), nameof(GetDataSetsList) + "User is already decrypted....", ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty); }
                        try
                        {
                            if (!string.IsNullOrEmpty(Convert.ToString(dataset.ModifiedBy)))
                                dataset.ModifiedBy = _encryptionDecryption.Decrypt(Convert.ToString(dataset.ModifiedBy));
                        }
                        catch (Exception ex) { LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataSetsService), nameof(GetDataSetsList) + "User is already decrypted....", ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty); }
                    }
                    if (dataset.SourceDetails != null)
                    {
                        if (dataset.SourceDetails.FileDetail != null)
                        {
                            if (dataset.SourceDetails.FileDetail.Count > 0)
                            {
                                for (int i = 0; i < dataset.SourceDetails.FileDetail.Count; i++)
                                {
                                    dataset.SourceDetails.FileDetail[0].FilePath = null;
                                }
                            }
                        }
                    }
                }
            }

            return dataSetInfos;
        }

        public async Task<List<DataSetInfo>> GetCompletedDataSetsList(string clientUId, string deliveryConstructUId, string userId)
        {
            List<DataSetInfo> dataSetInfos = await GetCompletedDataSets(clientUId, deliveryConstructUId, userId);

            //remove file paths from payload
            if (dataSetInfos.Count > 0)
            {
                foreach (var dataset in dataSetInfos)
                {
                    if (dataset.SourceDetails != null)
                    {
                        if (dataset.SourceDetails.FileDetail != null)
                        {
                            if (dataset.SourceDetails.FileDetail.Count > 0)
                            {
                                for (int i = 0; i < dataset.SourceDetails.FileDetail.Count; i++)
                                {
                                    dataset.SourceDetails.FileDetail[0].FilePath = null;
                                }
                            }
                        }
                    }
                }
            }

            return dataSetInfos;
        }





        public async Task<List<DataSetInfo>> GetDataSets(string clientUId, string deliveryConstructUId, string userId, string sourceName)
        {
            string encryptedUser = userId;
            if (!string.IsNullOrEmpty(Convert.ToString(encryptedUser)))
            {
                encryptedUser = _encryptionDecryption.Encrypt(Convert.ToString(encryptedUser));
            }
            var dataSetCollection = _database.GetCollection<DataSetInfo>(CONSTANTS.DataSetInfo);
            var filter = Builders<DataSetInfo>.Filter.Where(x => x.ClientUId == clientUId)
                         & Builders<DataSetInfo>.Filter.Where(x => x.DeliveryConstructUId == deliveryConstructUId)
                         & Builders<DataSetInfo>.Filter.Where(x => x.SourceName == sourceName)
                         & (Builders<DataSetInfo>.Filter.Where(x => !x.IsPrivate) | Builders<DataSetInfo>.Filter.Where(x => (x.CreatedBy ==  userId || x.CreatedBy == encryptedUser)));
            var projection = Builders<DataSetInfo>.Projection.Exclude("_id").Exclude("UniquenessDetails").Exclude("ValidRecordsDetails").Exclude("UniqueValues");
            var result = await dataSetCollection.Find(filter).Project<DataSetInfo>(projection).ToListAsync();
            return result;
        }
        public async Task<List<DataSetInfo>> GetCompletedDataSets(string clientUId, string deliveryConstructUId, string userId)
        {
            string encryptedUser = userId;
            if (!string.IsNullOrEmpty(Convert.ToString(encryptedUser)))
            {
                encryptedUser = _encryptionDecryption.Encrypt(Convert.ToString(encryptedUser));
            }
            var dataSetCollection = _database.GetCollection<DataSetInfo>(CONSTANTS.DataSetInfo);
            var filter = Builders<DataSetInfo>.Filter.Where(x => x.ClientUId == clientUId)
                         & Builders<DataSetInfo>.Filter.Where(x => x.DeliveryConstructUId == deliveryConstructUId)
                         & Builders<DataSetInfo>.Filter.Where(x => x.Status == "C")
                         & (Builders<DataSetInfo>.Filter.Where(x => !x.IsPrivate) | Builders<DataSetInfo>.Filter.Where(x => (x.CreatedBy == userId || x.CreatedBy ==encryptedUser)));
            var projection = Builders<DataSetInfo>.Projection.Exclude("_id").Exclude("UniquenessDetails").Exclude("ValidRecordsDetails").Exclude("UniqueValues");
            var result = await dataSetCollection.Find(filter).Project<DataSetInfo>(projection).ToListAsync();
            for (int i = 0; i < result.Count; i++)
            {
                if (result[i].DBEncryptionRequired)
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(Convert.ToString(result[i].CreatedBy)))
                            result[i].CreatedBy = _encryptionDecryption.Decrypt(Convert.ToString(result[i].CreatedBy));
                    }
                    catch (Exception ex) { LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataSetsService), nameof(GetCompletedDataSets) + "User is already decrypted....", ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty); }
                    try
                    {
                        if (!string.IsNullOrEmpty(Convert.ToString(result[i].ModifiedBy)))
                            result[i].ModifiedBy = _encryptionDecryption.Decrypt(Convert.ToString(result[i].ModifiedBy));
                    }
                    catch (Exception ex) { LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataSetsService), nameof(GetCompletedDataSets) + "User is already decrypted....", ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty); }
                }
            }
            return result;
        }

        public bool GetDataSetInfoEncryption(string dataSetUId)
        {
            var dataSetCollection = _database.GetCollection<DataSetInfoDto>(CONSTANTS.DataSetInfo);
            var filter = Builders<DataSetInfoDto>.Filter.Where(x => x.DataSetUId == dataSetUId);
            var result = dataSetCollection.Find(filter).FirstOrDefault();
            if (result != null)
            {
                if (result.DBEncryptionRequired)
                {
                    return true;
                }
                else
                {
                    return false;
                }

            }
            return false;
        }

        public async Task<DataSetInfo> GetDataSetDetails(string dataSetUId)
        {
            bool dBEncryptionRequired = GetDataSetInfoEncryption(dataSetUId);
            var dataSetCollection = _database.GetCollection<DataSetInfo>(CONSTANTS.DataSetInfo);
            var filter = Builders<DataSetInfo>.Filter.Where(x => x.DataSetUId == dataSetUId);
            var projection = Builders<DataSetInfo>.Projection.Exclude("_id").Exclude("SourceDetails").Exclude("UniquenessDetails").Exclude("ValidRecordsDetails").Exclude("UniqueValues");
            var result = await dataSetCollection.Find(filter).Project<DataSetInfo>(projection).SingleOrDefaultAsync();
            if (dBEncryptionRequired)
            {
                if (result != null)
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(Convert.ToString(result.CreatedBy)))
                            result.CreatedBy = _encryptionDecryption.Decrypt(Convert.ToString(result.CreatedBy));
                    }
                    catch (Exception ex) { LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataSetsService), nameof(GetDataSetDetails) + "User is already decrypted....", ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty); }
                    try
                    {
                        if(!string.IsNullOrEmpty(Convert.ToString(result.ModifiedBy)))
                            result.ModifiedBy = _encryptionDecryption.Decrypt(Convert.ToString(result.ModifiedBy));
                    }
                    catch (Exception ex) { LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataSetsService), nameof(GetDataSetDetails) + "User is already decrypted....", ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty); }
                }
            }
            return result;
        }

        public bool CheckDataSetExists(string dataSetUId)
        {
            var dataSetCollection = _database.GetCollection<DataSetInfo>(CONSTANTS.DataSetInfo);
            var filter = Builders<DataSetInfo>.Filter.Where(x => x.DataSetUId == dataSetUId);
            var projection = Builders<DataSetInfo>.Projection.Exclude("_id").Exclude("SourceDetails").Exclude("UniquenessDetails").Exclude("ValidRecordsDetails").Exclude("UniqueValues");
            var result = dataSetCollection.Find(filter).Project<DataSetInfo>(projection).ToList();
            if (result.Count > 0)
                return true;
            else
                return false;
        }


        public async Task<string> GetDataSetData(string dataSetUId, int DecimalPlaces)
        {
            BsonArray inputD = new BsonArray();
            List<object> response = new List<object>();
            var dataSetCollection = _database.GetCollection<DSIngestDataDto>(CONSTANTS.DataSet_IngestData);
            var filter = Builders<DSIngestDataDto>.Filter.Where(x => x.DataSetUId == dataSetUId);
            var projection = Builders<DSIngestDataDto>.Projection.Exclude("_id").Include("InputData");
            var result = await dataSetCollection.Find(filter).Project<DSIngestDataDto>(projection).ToListAsync();
            var dBEncryptionRequired = GetDataSetInfoEncryption(dataSetUId);
            if (result.Count > 0)
            {
                var data = "";
                for (int i = 0; i < result.Count; i++)
                {
                    if (dBEncryptionRequired)
                    {
                        data = _encryptionDecryption.Decrypt(result[i].InputData);
                    }
                    else
                    {
                        data = result[i].InputData;
                    }
                    inputD.AddRange(BsonSerializer.Deserialize<BsonArray>(data));
                    if (inputD.Count > 100)
                        break;
                    //var inputData = JsonConvert.DeserializeObject<List<object>>(data);
                    //response.AddRange(inputData);
                    //if (response.Count > 100)
                    //    break;
                }
            }

            response = CommonUtility.GetDataAfterDecimalPrecision(inputD, DecimalPlaces, 100, false);
            return JsonConvert.SerializeObject(response.Take(100).ToList());
        }

        public async Task<List<object>> GetDataSetResponse(string dataSetUId)
        {
            List<object> response = new List<object>();
            var dataSetCollection = _database.GetCollection<DSIngestDataDto>(CONSTANTS.DataSet_IngestData);
            var filter = Builders<DSIngestDataDto>.Filter.Where(x => x.DataSetUId == dataSetUId);
            var projection = Builders<DSIngestDataDto>.Projection.Exclude("_id").Include("InputData");
            var result = await dataSetCollection.Find(filter).Project<DSIngestDataDto>(projection).ToListAsync();
            var dBEncryptionRequired = GetDataSetInfoEncryption(dataSetUId);
            if (result.Count > 0)
            {
                var data = "";
                for (int i = 0; i < result.Count; i++)
                {
                    if (dBEncryptionRequired)
                    {
                        data = _encryptionDecryption.Decrypt(result[i].InputData);
                    }
                    else
                    {
                        data = result[i].InputData;
                    }
                    var inputData = JsonConvert.DeserializeObject<List<object>>(data);
                    response.AddRange(inputData);
                }
            }

            return response.ToList();
        }

        public async Task<string> DownloadData(string dataSetUId)
        {
            List<object> response = new List<object>();
            var dataSetCollection = _database.GetCollection<DSIngestDataDto>(CONSTANTS.DataSet_IngestData);
            var filter = Builders<DSIngestDataDto>.Filter.Where(x => x.DataSetUId == dataSetUId);
            var projection = Builders<DSIngestDataDto>.Projection.Exclude("_id").Include("InputData");
            var result = await dataSetCollection.Find(filter).Project<DSIngestDataDto>(projection).ToListAsync();
            var dBEncryptionRequired = GetDataSetInfoEncryption(dataSetUId);
            if (result.Count > 0)
            {
                var data = "";
                for (int i = 0; i < result.Count; i++)
                {
                    if (dBEncryptionRequired)
                    {
                        data = _encryptionDecryption.Decrypt(result[i].InputData);
                    }
                    else
                    {
                        data = result[i].InputData;
                    }
                    var inputData = JsonConvert.DeserializeObject<List<object>>(data);
                    response.AddRange(inputData);
                }
            }

            return JsonConvert.SerializeObject(response.ToList());
        }


        public string DownloadDataSet(string dataSetUId)
        {
            string filePath = null;
            DataSetInfo dataSetInfo = GetDataSetInfo(dataSetUId);
            if (dataSetInfo.Status == "C" || dataSetInfo.Status == "P")
            {
                if (dataSetInfo.SourceDetails != null)
                {
                    if (dataSetInfo.SourceDetails.FileDetail != null)
                    {
                        filePath = dataSetInfo.SourceDetails.FileDetail[0].FilePath;
                    }
                }
            }


            return filePath;
        }

        public string DeleteDataSet(string dataSetUId, string userId)
        {
            DataSetInfoDto dataSetInfoDto = GetDataSetInfo(dataSetUId);
            bool dBEncryptionRequired = GetDataSetInfoEncryption(dataSetUId);
            if (dataSetInfoDto != null)
            {
                if (dBEncryptionRequired)
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(Convert.ToString(dataSetInfoDto.CreatedBy)))
                        {
                            dataSetInfoDto.CreatedBy = _encryptionDecryption.Decrypt(Convert.ToString(dataSetInfoDto.CreatedBy));
                        }
                    }
                    catch (Exception ex) { LOGGING.LogManager.Logger.LogProcessInfo(typeof(DataSetsService), nameof(DeleteDataSet) + "User is already decrypted....", ex.Message, default(Guid), string.Empty, string.Empty, string.Empty, string.Empty); }
                }
                if (dataSetInfoDto.CreatedBy != userId)
                    throw new AccessViolationException("Cannot delete the dataset created by another user");
                var deployModelCollection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
                var filter = Builders<DeployModelsDto>.Filter.Where(x => x.DataSetUId == dataSetUId);
                var projection = Builders<DeployModelsDto>.Projection.Exclude("_id");
                var result = deployModelCollection.Find(filter).Project<DeployModelsDto>(projection).ToList();
                if (result.Count > 0)
                {
                    throw new OperationCanceledException("DataSet cannot be deleted, as it has association with models");
                }
                else
                {
                    //remove file
                    if (dataSetInfoDto.SourceDetails != null)
                    {
                        if (dataSetInfoDto.SourceDetails.FileDetail != null)
                        {
                            if (dataSetInfoDto.SourceDetails.FileDetail.Count > 0)
                            {
                                string filePath = dataSetInfoDto.SourceDetails.FileDetail[0].FilePath;
                                DirectoryInfo dir = new DirectoryInfo(Path.GetDirectoryName(filePath));
                                string fileName = Path.GetFileName(filePath);
                                List<FileInfo> tempFilespath = dir.GetFiles(fileName + "*").ToList();
                                if (tempFilespath.Count > 0)
                                {
                                    foreach (var file in tempFilespath)
                                    {
                                        if (File.Exists(file.FullName))
                                            File.Delete(file.FullName);
                                    }
                                }
                                if (File.Exists(filePath))
                                    File.Delete(filePath);
                            }

                        }
                    }


                    //remove from datasetinfo collection
                    var dataSetCollection = _database.GetCollection<DataSetInfoDto>(CONSTANTS.DataSetInfo);
                    var filter2 = Builders<DataSetInfoDto>.Filter.Where(x => x.DataSetUId == dataSetUId);
                    dataSetCollection.DeleteOne(filter2);

                    //remove from dataset_ingestdata collection
                    var ds_ingestdata = _database.GetCollection<DSIngestDataDto>(CONSTANTS.DataSet_IngestData);
                    var filter3 = Builders<DSIngestDataDto>.Filter.Where(x => x.DataSetUId == dataSetUId);
                    ds_ingestdata.DeleteMany(filter3);

                }
            }
            else
            {
                throw new KeyNotFoundException("DataSetUId not found");
            }

            return "Deleted Successfully";

        }


        public async Task<DataSetInfo> InsertExternalAPIRequest(JObject requestPayload)
        {
            var DBEncryption = Convert.ToBoolean(requestPayload["EncryptionRequired"]);
            DataSetInfoDto dataSetInfoDto = new DataSetInfoDto();
            bool encryptDB = false;

            if (appSettings.isForAllData == true)
            {
                if (appSettings.DBEncryption == true)
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
                if (appSettings.DBEncryption == true && DBEncryption == true)
                {

                    encryptDB = true;
                }
                else
                {
                    encryptDB = false;
                }

            }
            dataSetInfoDto.DataSetName = requestPayload["DataSetName"].ToString();
            dataSetInfoDto.SourceName = "ExternalAPI";
            dataSetInfoDto.DataSetUId = Guid.NewGuid().ToString();
            dataSetInfoDto.ClientUId = requestPayload["ClientUId"].ToString();
            dataSetInfoDto.DeliveryConstructUId = requestPayload["DeliveryConstructUId"].ToString();
            dataSetInfoDto.DBEncryptionRequired = encryptDB;//Convert.ToBoolean(requestPayload["EncryptionRequired"]); //false;
            dataSetInfoDto.Category = requestPayload["Category"].ToString();
            dataSetInfoDto.IsPrivate = Convert.ToBoolean(requestPayload["IsPrivate"].ToString());
            dataSetInfoDto.EnableIncrementalFetch = Convert.ToBoolean(requestPayload["EnableIncrementalFetch"].ToString());
            dataSetInfoDto.Status = "I";
            dataSetInfoDto.Progress = "10";
            dataSetInfoDto.SourceDetails = new DataSetSourceDetails();
            dataSetInfoDto.SourceDetails.ExternalAPIDetail = new ExternalAPIInfo();
            dataSetInfoDto.SourceDetails.ExternalAPIDetail.HttpMethod = requestPayload["HttpMethod"].ToString();
            dataSetInfoDto.SourceDetails.ExternalAPIDetail.Url = requestPayload["Url"].ToString();
            dataSetInfoDto.SourceDetails.ExternalAPIDetail.AuthType = requestPayload["AuthType"].ToString();
            dataSetInfoDto.SourceDetails.ExternalAPIDetail.Token = requestPayload["Token"].ToString();

            if (!string.IsNullOrEmpty(requestPayload["Headers"].ToString()) && requestPayload["Headers"].ToString() != "{}")
            {
                dataSetInfoDto.SourceDetails.ExternalAPIDetail.Headers = BsonDocument.Parse(requestPayload["Headers"].ToString());
            }
            BsonDocument bodyparams = BsonDocument.Parse(requestPayload["Body"].ToString());
            bodyparams["PageNumber"] = "1";

            List<DataSetInfo> dataSetInfos = await GetDataSetsList(dataSetInfoDto.ClientUId, dataSetInfoDto.DeliveryConstructUId, requestPayload["UserId"].ToString(), "ExternalAPI");
            List<string> dataSetNames = dataSetInfos.Select(x => x.DataSetName).ToList();
            bool nameAlreadyPresent = dataSetNames.Contains(dataSetInfoDto.DataSetName, StringComparer.OrdinalIgnoreCase);
            if (nameAlreadyPresent)
                throw new InvalidDataException("DataSet name already present");

            if (dataSetInfoDto.SourceDetails.ExternalAPIDetail.AuthType == "AzureAD")
            {
                dataSetInfoDto.SourceDetails.ExternalAPIDetail.AzureUrl = _encryptionDecryption.Encrypt(requestPayload["AzureUrl"].ToString());
                dataSetInfoDto.SourceDetails.ExternalAPIDetail.AzureCredentials = _encryptionDecryption.Encrypt(requestPayload["AzureCredentials"].ToString());
            }
            if (dataSetInfoDto.EnableIncrementalFetch == true)
            {
                bodyparams["StartDate"] = requestPayload["StartDate"].ToString();
                bodyparams["EndDate"] = requestPayload["EndDate"].ToString();
            }

            if (requestPayload["Body"]["PageNumber"] != null && !string.IsNullOrEmpty(requestPayload["Body"]["PageNumber"].ToString()))
            {
                bodyparams["PageNumber"] = requestPayload["Body"]["PageNumber"].ToString();
            }

            dataSetInfoDto.SourceDetails.ExternalAPIDetail.Body = bodyparams;
            string UserId = Convert.ToString(requestPayload["UserId"]);
            if (encryptDB)
            {
                if (!string.IsNullOrEmpty(Convert.ToString(requestPayload["UserId"])))
                    UserId = _encryptionDecryption.Encrypt(Convert.ToString(requestPayload["UserId"]));
            }

            if (string.IsNullOrEmpty(Convert.ToString(requestPayload["PageNumber"])))
            { 
                bodyparams["PageNumber"] = "1"; 
            }

            dataSetInfoDto.CreatedBy = UserId;
            dataSetInfoDto.ModifiedBy = UserId;
            dataSetInfoDto.CreatedOn = DateTime.UtcNow.ToString();
            dataSetInfoDto.ModifiedOn = DateTime.UtcNow.ToString();

            InsertDataSetInfo(dataSetInfoDto);
            InsertIngestDataSetRequest(dataSetInfoDto);

            return await GetDataSetDetails(dataSetInfoDto.DataSetUId);
        }
        public bool SplitFile()
        {
            string FileName = "";
            //var datas = _encryptionDecryption.DecryptFiletoStream(@"C:\mnt\myWizard-Phoenix\IngrAIn_Shared\DataSets\e19c4c42-c542-4c2a-83e3-00c6e350a731_Melbourne_housing_data_test-1st RowFull.csv",null);
            //var nullEncr = _encryptionDecryption.Encrypt("null");
            //var nullDecry = _encryptionDecryption.Decrypt(nullEncr);
            //var data = _encryptionDecryption.Encrypt("{\"acceptancecriteria_story\": {\"\\n\": \"False\", \"Inbound with SP value 4.50PM\": \"False\", \"Inbound without SP value\": \"False\", \"acc\": \"False\", \"acc\\n\": \"False\", \"new story 5 08 2021\": \"False\", \"new story assigned to 1\": \"False\", \"new story inbound Release planner\": \"False\", \"new story inbound Release planner in outside scope 1\": \"False\", \"new story inbound Release planner in outside scope 2\\n\": \"False\", \"new story inbound Release planner in outside team \": \"False\", \"new story inbound Release planner in outside team 2\": \"False\", \"new story inbound Release planner in same team\\n\": \"False\"}, \"businessvalue_story\": {\"10\": \"False\", \"13\": \"False\", \"14\": \"False\", \"15\": \"False\", \"16\": \"False\", \"17\": \"False\", \"5\": \"False\"}, \"description_story\": {\"<p>CR delete scenario 22</p>\": \"False\", \"<p>Inbound with SP value 4.50PM</p>\": \"False\", \"<p>Inbound without SP value</p>\": \"False\", \"<p>Testing user story test 543</p>\": \"False\", \"<p>Testing user story test 7</p>\": \"False\", \"<p>US adop del</p>\": \"False\", \"<p>cr del 1221</p>\": \"False\", \"<p>desc</p>\": \"False\", \"<p>new story 5 08 2021</p>\": \"False\", \"<p>new story assigned to </p>\": \"False\", \"<p>new story inbound Release planner in outside scope 1</p>\": \"False\", \"<p>new story inbound Release planner in outside scope 2</p>\": \"False\", \"<p>new story inbound Release planner in outside team </p>\": \"False\", \"<p>new story inbound Release planner in same team</p>\": \"False\", \"<p>us adop delete scenario</p>\": \"False\", \"<p>us del 22</p>\": \"False\", \"Userstory7.14\": \"False\"}, \"itemexternalid_story\": {\"3bd8a60b-00ba-4bd1-bb66-bd0c8a6f64be\": \"False\", \"PHNXCN2-6346\": \"False\", \"PHNXCN2-6347\": \"False\", \"PHNXCN2-6348\": \"False\", \"PHNXCN2-6349\": \"False\", \"PHNXCN2-6350\": \"False\", \"PHNXCN2-6351\": \"False\", \"de89031d-8e6b-4c21-8f72-fc55f32f0b41\": \"False\"}, \"iterationuid_story\": {\"a6b5f647-8a2c-8a49-9ef1-3434ba39b94e\": \"False\", \"a882977b-f0eb-429a-93bb-8af8a95b542b\": \"False\", \"da2a2e66-c2aa-4b40-bf6e-1c74f492b1b3\": \"False\"}, \"iterationuidexternalvalue_story\": {\"ASA_ADOP Sprint 1\": \"False\", \"ASA_ADOP Sprint 2\": \"False\", \"Sprint_20jul21\": \"False\"}, \"iterationuididvalue_story\": {\"00000000000000000000000000000000\": \"False\", \"A6B5F6478A2C8A499EF13434BA39B94E\": \"False\", \"A882977BF0EB429A93BB8AF8A95B542B\": \"False\", \"DA2A2E66C2AA4B40BF6E1C74F492B1B3\": \"False\"}, \"prdctid_itemexternalid_story\": {\"00000019000000000000000000000000\": \"False\", \"000000190000000000000000000000003bd8a60b-00ba-4bd1-bb66-bd0c8a6f64be\": \"False\", \"00000019000000000000000000000000PHNXCN2-6346\": \"False\", \"00000019000000000000000000000000PHNXCN2-6347\": \"False\", \"00000019000000000000000000000000PHNXCN2-6348\": \"False\", \"00000019000000000000000000000000PHNXCN2-6349\": \"False\", \"00000019000000000000000000000000PHNXCN2-6350\": \"False\", \"00000019000000000000000000000000PHNXCN2-6351\": \"False\", \"00000019000000000000000000000000de89031d-8e6b-4c21-8f72-fc55f32f0b41\": \"False\", \"00000021000000000000000000000000\": \"False\"}, \"priorityuid_story\": {\"Critical\": \"False\", \"Low\": \"False\", \"Urgent\": \"False\"}, \"priorityuididvalue_story\": {\"00000019000000000000000000000034\": \"False\", \"00000019000000000000000000000040\": \"False\", \"00000019000000000000000000000126\": \"False\", \"00000190001000100000000000000033\": \"False\"}, \"productinstanceuid_story\": {\"00000019000000000000000000000000\": \"False\", \"00000021000000000000000000000000\": \"False\"}, \"project_story\": {\"PHNXCN2\": \"False\", \"Phoenix_ Client-Native  _Project2\": \"False\"}, \"releaseuididvalue_story\": {\"00000000000000000000000000000000\": \"False\", \"1BF73E2E417F17D975F78F51284E16B4\": \"False\", \"7ABBF370D1B513317446C805CBC1002E\": \"False\"}, \"storypointestimated_story\": {\"1\": \"False\", \"10\": \"False\", \"11\": \"False\", \"12\": \"False\", \"13\": \"False\", \"14\": \"False\", \"16\": \"False\", \"20\": \"False\", \"21\": \"False\", \"24\": \"False\", \"3\": \"False\", \"43\": \"False\", \"54\": \"False\", \"6\": \"False\", \"8\": \"False\"}, \"teamareauid_story\": {\"40041cb6-e5e4-48b9-ba3f-9919de9582b7\": \"False\", \"66bd5698-10c3-4752-94bd-cbab60c11b2c\": \"False\", \"877abee3-3fa0-4e45-a103-de6a3f10fba2\": \"False\", \"f014d496-ba96-4ff1-a0e0-457dac7deb4b\": \"False\"}}");
            //var res = _encryptionDecryption.Decrypt("+jpD48M6LMw+kk4uNYQXtw==");
            //string FileName = @"C:\Users\shreya.keshava\Downloads\Melbourne_housing_data_test-1st RowFull.csv";
            List<string> FileParts = new List<string>();
            int MaxFileSizeMB = 10;
            bool rslt = false;
            string BaseFileName = Path.GetFileName(FileName);
            // set the size of file chunk we are going to split into  
            int BufferChunkSize = MaxFileSizeMB * (1024 * 1024);
            // set a buffer size and an array to store the buffer data as we read it  
            const int READBUFFER_SIZE = 1024;
            byte[] FSBuffer = new byte[READBUFFER_SIZE];
            // open the file to read it into chunks  
            using (FileStream FS = new FileStream(FileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                // calculate the number of files that will be created  
                int TotalFileParts = 0;
                if (FS.Length < BufferChunkSize)
                {
                    TotalFileParts = 1;
                }
                else
                {
                    float PreciseFileParts = ((float)FS.Length / (float)BufferChunkSize);
                    TotalFileParts = (int)Math.Ceiling(PreciseFileParts);
                }

                int FilePartCount = 0;
                // scan through the file, and each time we get enough data to fill a chunk, write out that file  
                while (FS.Position < FS.Length)
                {
                    string FilePartName = String.Format("{0}.part_{1}.{2}",
                    BaseFileName, (FilePartCount + 1).ToString(), TotalFileParts.ToString());
                    FilePartName = Path.Combine(appSettings.UploadFilePath, FilePartName);
                    FileParts.Add(FilePartName);
                    using (FileStream FilePart = new FileStream(FilePartName, FileMode.Create))
                    {
                        int bytesRemaining = BufferChunkSize;
                        int bytesRead = 0;
                        while (bytesRemaining > 0 && (bytesRead = FS.Read(FSBuffer, 0,
                         Math.Min(bytesRemaining, READBUFFER_SIZE))) > 0)
                        {
                            FilePart.Write(FSBuffer, 0, bytesRead);
                            bytesRemaining -= bytesRead;
                        }
                    }
                    // file written, loop for next chunk  
                    FilePartCount++;
                }

            }
            return rslt;
        }

        public JObject GetSampleData(JObject inputRequest)
        {
            string datasetUId = string.Empty;
            int pageNumber = 0;
            int previousCount = 0;
            int totalRecordCount = 0;
            int totalPageCount = 0;
            int batchSize = 200;
            DateTime? startDate = null;
            DateTime? endDate = null;

            List<object> resultData = new List<object>();
            JObject finalResult = new JObject();



            if (inputRequest.ContainsKey("DataSetUId"))
            {
                datasetUId = inputRequest["DataSetUId"].ToString();
            }

            finalResult["DataSetUId"] = datasetUId;

            if (inputRequest.ContainsKey("PageNumber"))
            {
                pageNumber = Convert.ToInt32(inputRequest["PageNumber"].ToString());
                previousCount = (pageNumber - 1) * batchSize;
            }
            else
            {
                throw new KeyNotFoundException("PageNumber is not present");
            }

            if (inputRequest.ContainsKey("StartDate") && inputRequest.ContainsKey("EndDate"))
            {
                startDate = DateTime.Parse(inputRequest["StartDate"].ToString());
                endDate = DateTime.Parse(inputRequest["EndDate"].ToString());
            }
            finalResult["PageNumber"] = pageNumber;

            if (!string.IsNullOrEmpty(datasetUId))
            {
                var ingestedData = _database.GetCollection<DSIngestDataDto>(CONSTANTS.DataSet_IngestData);
                var filter = Builders<DSIngestDataDto>.Filter.Where(x => x.DataSetUId == datasetUId);
                var projection = Builders<DSIngestDataDto>.Projection.Include("InputData");
                var result = ingestedData.Find(filter).Project<DSIngestDataDto>(projection).ToList();
                var encrytion = GetDataSetInfoEncryption(datasetUId);
                if (result.Count > 0)
                {
                    int size = batchSize;

                    foreach (var doc in result)
                    {
                        if (encrytion)
                        {
                                doc.InputData = _encryptionDecryption.Decrypt(doc.InputData);
                        }
                        JArray data = JArray.Parse(doc.InputData);
                        JArray filteredData = new JArray();
                        if (startDate != null && endDate != null)
                        {
                            foreach (var obj in data)
                            {
                                DateTime recDate = new DateTime();

                                if (obj["ModifiedOn"] != null)
                                {
                                    recDate = DateTime.Parse(obj["ModifiedOn"].ToString());
                                }
                                else if (obj["LastModifiedTime"] != null)
                                {
                                    recDate = DateTime.Parse(obj["LastModifiedTime"].ToString());
                                }
                                else if (obj["DateColumn"] != null)
                                {
                                    recDate = DateTime.Parse(obj["DateColumn"].ToString());
                                }
                                else
                                {
                                    filteredData.Add(obj);
                                    continue;
                                }

                                if (recDate != null && recDate >= startDate && recDate <= endDate)
                                {
                                    filteredData.Add(obj);
                                }
                            }

                        }
                        else
                        {
                            filteredData = data;
                        }
                        totalRecordCount += filteredData.Count;



                        resultData.AddRange(filteredData.Skip(previousCount).Take(size).ToList());
                        if (resultData.Count <= batchSize)
                        {
                            size = size - result.Count; // 1.600
                            previousCount = previousCount - (filteredData.Count - resultData.Count); //1.300
                        }
                    }


                    totalPageCount = (totalRecordCount + batchSize - 1) / batchSize;


                }



            }
            finalResult["TotalPageCount"] = totalPageCount;
            finalResult["TotalRecordCount"] = totalRecordCount;
            finalResult["ActualData"] = JArray.Parse(JsonConvert.SerializeObject(resultData));

            return finalResult;

        }


        public TrainGenericDataSetOutput TrainGenericDataSets(TrainGenericDataSetInput trainGenericDataSetInput)
        {
            TrainGenericDataSetOutput trainGenericDataSetOutput
                = new TrainGenericDataSetOutput(trainGenericDataSetInput.ClientUId
                                                , trainGenericDataSetInput.DeliveryConstructUId
                                                , trainGenericDataSetInput.UseCaseId
                                                , trainGenericDataSetInput.DataSetUId
                                                , trainGenericDataSetInput.UserId
                                                , trainGenericDataSetInput.ApplicationId);

            DeployModelsDto template = GetTemplateDetails(trainGenericDataSetInput.UseCaseId);
            if (template != null)
            {
                string correlationId = Guid.NewGuid().ToString();
                FileUpload paramArgs = CreateParamArgs(correlationId, trainGenericDataSetInput.ClientUId, trainGenericDataSetInput.DeliveryConstructUId);
                bool dBEncryptionRequired = GetDataSetInfoEncryption(trainGenericDataSetInput.DataSetUId);
                string userId = trainGenericDataSetInput.UserId;
                if (dBEncryptionRequired)
                {
                    if (!string.IsNullOrEmpty(Convert.ToString(trainGenericDataSetInput.UserId)))
                    {
                        userId = _encryptionDecryption.Encrypt(Convert.ToString(trainGenericDataSetInput.UserId));
                    }
                }
                IngrainRequestQueue ingrainRequestQueue = new IngrainRequestQueue
                {
                    _id = Guid.NewGuid().ToString(),
                    AppID = trainGenericDataSetInput.ApplicationId,
                    CorrelationId = correlationId,
                    DataSetUId = trainGenericDataSetInput.DataSetUId,
                    ClientId = trainGenericDataSetInput.ClientUId,
                    DeliveryconstructId = trainGenericDataSetInput.DeliveryConstructUId,
                    RequestId = Guid.NewGuid().ToString(),
                    ProcessId = null,
                    Status = null,
                    ModelName = template.ModelName + "_" + correlationId,
                    RequestStatus = CONSTANTS.New,
                    RetryCount = 0,
                    ProblemType = null,
                    Message = null,
                    UniId = null,
                    Progress = null,
                    pageInfo = "AutoTrain",
                    ParamArgs = JsonConvert.SerializeObject(paramArgs),
                    Function = "AutoTrain",
                    TemplateUseCaseID = trainGenericDataSetInput.UseCaseId,
                    CreatedByUser = userId,
                    CreatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    ModifiedByUser = userId,
                    ModifiedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    LastProcessedOn = null
                };

                InsertSSAIRequest(ingrainRequestQueue);
                trainGenericDataSetOutput.CorrelationId = correlationId;
                trainGenericDataSetOutput.Status = "I";
                trainGenericDataSetOutput.StatusMessage = "Training initiated";
            }
            else
            {
                trainGenericDataSetOutput.Status = "E";
                trainGenericDataSetOutput.StatusMessage = "Invalid UseCaseId";
            }
            return trainGenericDataSetOutput;
        }


        private FileUpload CreateParamArgs(string correlationId, string clientUId, string deliveryConstructUId)
        {
            ParentFile parent = new ParentFile()
            {
                Type = CONSTANTS.Null,
                Name = CONSTANTS.Null
            };

            Filepath fileupload = new Filepath()
            {
                fileList = CONSTANTS.Null
            };

            FileUpload paramArgs = new FileUpload()
            {
                CorrelationId = correlationId,
                ClientUID = clientUId,
                DeliveryConstructUId = deliveryConstructUId,
                Parent = parent,
                Flag = CONSTANTS.Null,
                mapping = CONSTANTS.Null,
                mapping_flag = "False",
                pad = CONSTANTS.Null,
                metric = CONSTANTS.Null,
                InstaMl = CONSTANTS.Null,
                fileupload = fileupload,
                Customdetails = "null"

            };

            return paramArgs;
        }
        public DeployModelsDto GetTemplateDetails(string useCaseId)
        {
            var deployModelCollection = _database.GetCollection<DeployModelsDto>(CONSTANTS.SSAIDeployedModels);
            var filter = Builders<DeployModelsDto>.Filter.Where(x => x.CorrelationId == useCaseId)
                         & Builders<DeployModelsDto>.Filter.Where(x => x.IsModelTemplate);
            var projection = Builders<DeployModelsDto>.Projection.Exclude(x => x._id);
            var result = deployModelCollection.Find(filter).Project<DeployModelsDto>(projection).ToList();
            if (result.Count > 0)
            {
                return result[0];
            }
            else
            {
                return null;
            }
        }
        
    }
}
