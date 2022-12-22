using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Accenture.MyWizard.Ingrain.DataModels.Models
{
    public class DataSetInfo
    {
        public string DataSetUId { get; set; }
        public string DataSetName { get; set; }
        public string ClientUId { get; set; }
        public string DeliveryConstructUId { get; set; }
        public bool IsPrivate { get; set; }
        public bool? EnableIncrementalFetch { get; set; }
        public string Category { get; set; }
        public string DataSizeInMB { get; set; }
        public string RecordCount { get; set; }

        public bool DBEncryptionRequired { get; set; }
        public string SourceName { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
        public string Progress { get; set; }
        public dynamic UniqueValues { get; set; }
        public dynamic UniquenessDetails { get; set; }
        public dynamic ValidRecordsDetails { get; set; }


        public DataSetSourceDetails SourceDetails { get; set; }
        public string LastModifiedDate { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public string ModifiedOn { get; set; }

    }

    public class DataSetInfoDto : DataSetInfo
    {
        [BsonId]
        public ObjectId _id { get; set; }

    }

    public class DataSetSourceDetails
    {
        public List<FilesInfo> FileDetail { get; set; }
        public ExternalAPIInfo ExternalAPIDetail { get; set; }
    }

    public class FilesInfo
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
    }



    public class ExternalAPIInfo
    {
        public string HttpMethod { get; set; }
        public string Url { get; set; }
        public BsonDocument Headers { get; set; }
        public BsonDocument Body { get; set; }
        public string AuthType { get; set; }
        public string Token { get; set; }
        public string AzureUrl { get; set; }
        public string AzureCredentials { get; set; }
    }

    public class FileDetails
    {
        public string FileName { get; set; }
        public int FileOrder { get; set; }
    }





    public class DSIngestDataDto
    {
        [BsonId]
        public ObjectId _id { get; set; }
        public string DataSetUId { get; set; }
        public string InputData { get; set; }
        public List<string> ColumnsList { get; set; }
        public string DataType { get; set; }
        public JObject lastDateDict { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public string ModifiedOn { get; set; }
    }


    public class TrainGenericDataSetInput
    {
        [Required]
        public string ClientUId { get; set; }
        [Required]
        public string DeliveryConstructUId { get; set; }
        [Required]
        public string UseCaseId { get; set; }
        [Required]
        public string DataSetUId { get; set; }
        [Required]
        public string ApplicationId { get; set; }
        [Required]
        public string UserId { get; set; }
    }


    public class TrainGenericDataSetOutput
    {
        public string ClientUId { get; set; }
        public string DeliveryConstructUId { get; set; }
        public string CorrelationId { get; set; }
        public string UseCaseId { get; set; }
        public string DataSetUId { get; set; }
        public string ApplicationId { get; set; }
        public string UserId { get; set; }
       
        public string Status { get; set; }
        public string StatusMessage { get; set; }
        public string Progress { get; set; }

        public TrainGenericDataSetOutput()
        {

        }
        public TrainGenericDataSetOutput(string clientUId, 
                                         string deliveryConstructUId,
                                         string useCaseId,
                                         string dataSetUId,
                                         string userId,
                                         string applicationId)
        {
            this.ClientUId = clientUId;
            this.DeliveryConstructUId = deliveryConstructUId;
            this.UseCaseId = useCaseId;
            this.DataSetUId = dataSetUId;
            this.UserId = userId;
            this.ApplicationId = applicationId;
        }
    }

}
