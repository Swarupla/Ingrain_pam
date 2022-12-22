using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;

namespace Accenture.MyWizard.Ingrain.DataModels.Models
{
    [BsonIgnoreExtraElements]
    public class IngrainRequestQueue
    {
        [BsonId]
        public string _id { get; set; }
        public  string AppID { get; set; }        
        public string CorrelationId { get; set; }
        public bool IsRetrainedWSErrorModel { get; set; }
        public int PythonProcessID { get; set; }
        public bool IsFMVisualize { get; set; }
        public string FMCorrelationId { get; set; }
        public string DataSetUId { get; set; }
        public string RequestId { get; set; }
        public string ApplicationName { get; set; }
        public string ClientId { get; set; }
        public string DeliveryconstructId { get; set; }
        public long DataPoints { get; set; }
        public string ProcessId { get; set; }
        public string Status { get; set; }
        public string ModelName { get; set; }
        public string UseCaseID { get; set; }
        public string RequestStatus { get; set; }
        public int RetryCount { get; set; }
        public string ProblemType { get; set; }
        public string Frequency { get; set; }
        public string InstaID { get; set; }
        public string Message { get; set; }
        public string UniId { get; set; }        
        public string Progress { get; set; }
        public string pageInfo { get; set; }
        public string ParamArgs { get; set; }
        public string Function { get; set; }
        public string TemplateUseCaseID { get; set; }
        public string Category { get; set; }
        public string CreatedByUser { get; set; }
        public string CreatedOn { get; set; }
        public string ModifiedByUser { get; set; }
        public string ModifiedOn { get; set; }
        public string PyTriggerTime { get; set; }
        public string LastProcessedOn { get; set; }
        public string EstimatedRunTime { get; set; }
        public string ClientID { get; set; }
        public string DCID { get; set; }
        public string TriggerType { get; set; }
        public string AppURL { get; set; }
        public string SendNotification { get; set; }
        public string IsNotificationSent { get; set; }
        public string NotificationMessage { get; set; }
        public string TeamAreaUId { get; set; }

        public string RetrainRequired { get; set; }
        public bool IsForAPI { get; set; }
        public string FunctionPageInfo { get; set; }
        public bool IsForAutoTrain { get; set; }

        public string AutoTrainRequestId { get; set; }
        public string ServiceType { get; set; }
        public string RequestType { get; set; }
    }
    public class CustomPayloadAuto
    {
        public string AppId { get; set; }
        public string HttpMethod { get; set; }
        public string AppUrl { get; set; }
        public dynamic InputParameters { get; set; }

    }
    public class FileUpload
    {
        public string CorrelationId { get; set; }
        public string ClientUID { get; set; }
        public string DeliveryConstructUId { get; set; }
        public ParentFile Parent { get; set; }
        public string Flag { get; set; }
        public string mapping { get; set; }
        public string mapping_flag { get; set; }
        public string pad { get; set; }
        public string metric { get; set; }
        public string InstaMl { get; set; }
        public Filepath fileupload { get; set; }
        public string Customdetails { get; set; }
    }
    public class CascadeFileUpload
    {
        public string CorrelationId { get; set; }
        public string ClientUID { get; set; }
        public string DeliveryConstructUId { get; set; }
        public ParentFile Parent { get; set; }
        public string Flag { get; set; }
        public string mapping { get; set; }
        public string mapping_flag { get; set; }
        public string pad { get; set; }
        public string metric { get; set; }
        public string InstaMl { get; set; }
        public Filepath fileupload { get; set; }
        public string Customdetails { get; set; }
    }
    public class pad
    {
        public string startDate { get; set; }
        public string endDate { get; set; }
        public string method { get; set; }
        public Entities Entities { get; set; }
    }
    public class pad2
    {
        public string startDate { get; set; }
        public string endDate { get; set; }
        public string method { get; set; }
        public JObject Entities { get; set; }
    }
    public class Entities
    {
        public object CodeCommit { get; set; }        
    }    

    public class AgileFileUpload
    {
        public string CorrelationId { get; set; }
        public string ClientUID { get; set; }
        public string DeliveryConstructUId { get; set; }
        public ParentFile Parent { get; set; }
        public string Flag { get; set; }
        public string mapping { get; set; }
        public string mapping_flag { get; set; }
        public string pad { get; set; }
        public string metric { get; set; }
        public string InstaMl { get; set; }
        public AgileFilepath fileupload { get; set; }
        public string Customdetails { get; set; }
    }

    public class AutoTrainFileUpload
    {
        public string CorrelationId { get; set; }
        public string ClientUID { get; set; }
        public string DeliveryConstructUId { get; set; }
        public ParentFile Parent { get; set; }
        public string Flag { get; set; }
        public string mapping { get; set; }
        public string mapping_flag { get; set; }
        public string pad { get; set; }
        public string metric { get; set; }
        public string InstaMl { get; set; }
        public Filepath fileupload { get; set; }
        public CustomPayloadAuto Customdetails { get; set; }
    }
    public class InstaMLFileUpload
    {
        public string CorrelationId { get; set; }
        public string ClientUID { get; set; }
        public string DeliveryConstructUId { get; set; }
        public ParentFile Parent { get; set; }
        public string Flag { get; set; }
        public string mapping { get; set; }
        public string mapping_flag { get; set; }
        public string pad { get; set; }
        public string metric { get; set; }
        public List<InstaPayload> InstaMl { get; set; }
        public Filepath fileupload { get; set; }
        public string Customdetails { get; set; }
    }
    public class ParentFile
    {
        public string Type { get; set; }
        public string Name { get; set; }
    }
    public class Filepath
    {
        public string fileList { get; set; }
    }

    public class AgileFilepath
    {
        public string fileList { get; set; }
        public AgileCase AgileUsecase { get; set; }
    }

    public class AgileCase
    {
        public string oldcorrelationid;
    } 

    public class InstaPayload
    {
        public string InstaId { get; set; }
        public string CorrelationId { get; set; }
        public string ProblemType { get; set; }
        public string Dimension { get; set; }
        public string TargetColumn { get; set; }    
        public string UseCaseId { get; set; }
        public string Source { get; set; }

    }
    public class InstaFileUpload
    {        
        public string pad { get; set; }
        public string metric { get; set; }
        public InstaPayload InstaMl { get; set; }
        public Filepath fileupload { get; set; }

    }
    public class WfAnalysisParams
    {
        public string WfId { get; set; }
        public string Bulk { get; set; }
    }
    public class WfFileUpload
    {
        public string CorrelationId { get; set; }
        public string UserId { get; set; }
        public string PageInfo { get; set; }
        public string FilePath { get; set; }
    }

    public class FileUploadColums
    {
        public string CorrelationId { get; set; }

        public List<FileColumns> File { get; set; }

        public string Flag { get; set; }

        public string ParentFileName { get; set; }

        public string ModelName { get; set; }

        public bool Fileflag { get; set; }

    }
    public class FileColumns
    {
        public string FileName { get; set; }

        public Dictionary<string, string> FileColumn { get; set; }

        public bool ParentFileFlag { get; set; }
    }
    public class ProblemTypeDetails
    {
        public string InstaID { get; set; }
        public string ProblemType { get; set; }
        public string CorrelationId { get; set; }
        public string PredictedData { get; set; }
        public string TargetColumn { get; set; }
        public string ActualData { get; set; }
        public string[] SelectedFeatures { get; set; }

    }

    public class PAModel
    {
        public string WfId { get; set; }
        public string desired_value { get; set; }
    }

    public class CustomUpload
    {
        public string CorrelationId { get; set; }
        public string ClientUID { get; set; }
        public string DeliveryConstructUId { get; set; }
        public ParentFile Parent { get; set; }
        public string Flag { get; set; }
        public string mapping { get; set; }
        public string mapping_flag { get; set; }
        public string pad { get; set; }
        public string metric { get; set; }
        public string InstaMl { get; set; }
        public Filepath fileupload { get; set; }
        public CustomInputPayload Customdetails { get; set; }
    }

    public class CustomInputPayload
    {
        public string AppId { get; set; }
        public string HttpMethod { get; set; }
        public string AppUrl { get; set; }
        public BsonDocument InputParameters { get; set; }

    }

    public class SPAAIInputParams
    {
        public string CorrelationId { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public double TotalRecordCount { get; set; }
        public double PageNumber { get; set; }
        public double BatchSize { get; set; }

    }

    public class TOAI_InputParams
    {
        public string CorrelationId { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public double TotalRecordCount { get; set; }
        public double PageNumber { get; set; }
        public double BatchSize { get; set; }      
        public List<dynamic> ReleaseID { get; set; }
    }

    public class CustomSPAAIPayload
    {
        public string AppId { get; set; }
        public string HttpMethod { get; set; }
        public string AppUrl { get; set; }
        public dynamic InputParameters { get; set; }
        public string DateColumn { get; set; }
        public string UsecaseID { get; set; }
    }

    public class CustomSPAAIFileUpload
    {
        public string CorrelationId { get; set; }
        public string ClientUID { get; set; }
        public string DeliveryConstructUId { get; set; }
        public ParentFile Parent { get; set; }
        public string Flag { get; set; }
        public string mapping { get; set; }
        public string mapping_flag { get; set; }
        public string pad { get; set; }
        public string metric { get; set; }
        public string InstaMl { get; set; }
        public Filepath fileupload { get; set; }
        public CustomSPAAIPayload Customdetails { get; set; }
    }

    public class AgileFileUploadCustom
    {
        public string CorrelationId { get; set; }
        public string ClientUID { get; set; }
        public string DeliveryConstructUId { get; set; }
        public ParentFile Parent { get; set; }
        public string Flag { get; set; }
        public string mapping { get; set; }
        public string mapping_flag { get; set; }
        public string pad { get; set; }
        public string metric { get; set; }
        public string InstaMl { get; set; }
        public Filepath fileupload { get; set; }
        public CustomDetails Customdetails { get; set; }
    }

    public class CustomDetails
    {
        public CustomFlag CustomFlags { get; set; }
        public string AppId { get; set; }
        public string HttpMethod { get; set; }
        public string AppUrl { get; set; }
        public InputParameter InputParameters { get; set; }
        public string AICustom { get; set; }

    }


}
