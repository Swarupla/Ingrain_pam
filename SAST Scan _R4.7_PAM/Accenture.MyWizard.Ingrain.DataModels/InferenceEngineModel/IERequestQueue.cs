using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace Accenture.MyWizard.Ingrain.DataModels.InferenceEngineModel
{
    [BsonIgnoreExtraElements]
    public class IERequestQueue
    {
        [BsonId]
        public ObjectId _id { get; set; }
        public string RequestId { get; set; }
        public string ApplicationId { get; set; }
        public string DataSetUId { get; set; }
        public string CorrelationId { get; set; }
        public string InferenceConfigId { get; set; }
        public string InferenceConfigType { get; set; }
        public string ClientUId { get; set; }
        public string DeliveryConstructUId { get; set; }
        public long DataPoints { get; set; }
        public string Status { get; set; }
        public string UseCaseId { get; set; }
        public string RequestStatus { get; set; }

        public string Message { get; set; }

        public string Progress { get; set; }
        public string pageInfo { get; set; }
        public string ParamArgs { get; set; }
        public string Function { get; set; }

        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime ModifiedOn { get; set; }

        public bool IsIEPublish { get; set; }

        public bool IsPublishUseCase { get; set; }
        public string SourceName { get; set; }   

    }

    public class IEPythonInfo
    {
        public IEPythonCategory Category { get; set; }
        public string Status { get; set; }

        public string correlationId { get; set; }
    }
    public class IEPythonCategory
    {
        public string Category { get; set; }
        public string Message { get; set; }
    }

    public class IEFileColumns
    {
        public string FileName { get; set; }

        public Dictionary<string, string> FileColumn { get; set; }

        public bool ParentFileFlag { get; set; }
    }

    public class IEFileUploadColums
    {
        public string CorrelationId { get; set; }

        public List<IEFileColumns> File { get; set; }

        public string Flag { get; set; }

        public string ParentFileName { get; set; }

        public string ModelName { get; set; }

        public bool Fileflag { get; set; }

    }

    public class IEFilepath
    {
        public string fileList { get; set; }
    }  
   
    public class IEFileUpload
    {
        public string CorrelationId { get; set; }
        public string ClientUId { get; set; }
        public string DeliveryConstructUId { get; set; }
        public IEParentFile Parent { get; set; }
        public string Flag { get; set; }
        public string mapping { get; set; }
        public string mapping_flag { get; set; }
        public string pad { get; set; }
        public string metric { get; set; }
        public string InstaMl { get; set; }
        public IEFilepath fileupload { get; set; }
        public dynamic Customdetails { get; set; }
    }   

   
    
    public class IEParentFile
    {
        public string Type { get; set; }
        public string Name { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class IEAppIntegration
    {
        public string ApplicationID { get; set; }
        public string ApplicationName { get; set; }
        public string Environment { get; set; }
        public int? AutoTrainDays { get; set; }

        public int? ConfigRefreshDays { get; set; }
        public bool isDefault { get; set; }
        public string BaseURL { get; set; }
        public string ClientUId { get; set; }
        public string deliveryConstructUID { get; set; }
        public string TrainingDataRangeInMonths { get; set; }
        public string Authentication { get; set; }
        public string TokenGenerationURL { get; set; }
        public dynamic Credentials { get; set; }
        public string chunkSize { get; set; }
        public string PredictionQueueLimit { get; set; }
        public string AppNotificationUrl { get; set; }
        public string CreatedByUser { get; set; }
        public string CreatedOn { get; set; }
        public string ModifiedByUser { get; set; }
        public string ModifiedOn { get; set; }

    }

    public class IEAppDetails
    {
        public string ApplicationName { get; set; }
        public string ApplicationID { get; set; }
    }



    public class IEInputParams
    {
        public string ClientID { get; set; }
        public string E2EUID { get; set; }
        public string DeliveryConstructID { get; set; }
        public string Environment { get; set; }
        public string RequestType { get; set; }
        public string ServiceType { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }

    }

    public class IECustomPayloads
    {
        public string AppId { get; set; }
        public string HttpMethod { get; set; }
        public string AppUrl { get; set; }
        public IEInputParams InputParameters { get; set; }

    }

    public class IEAppIntegrationsCredentials
    {
        public string grant_type { get; set; }
        public string client_secret { get; set; }
        public string client_id { get; set; }
        public string resource { get; set; }
    }


}

