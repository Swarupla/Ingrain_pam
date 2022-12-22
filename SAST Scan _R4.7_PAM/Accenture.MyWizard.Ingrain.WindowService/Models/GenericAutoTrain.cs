using Accenture.MyWizard.Ingrain.DataModels.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.WindowService.Models
{
    public class GenericAutoTrain
    {
        public string ApplicationName { get; set; }
        public string UseCaseName { get; set; }
        public string PageInfo { get; set; }
        public string Success { get; set; }
        public string Message { get; set; }
        public string ErrorMessage { get; set; }
        public string Status { get; set; }
        public long DataPointsCount { get; set; }
        public string DataPointsWarning { get; set; }
    }


    public class GenericTemplatemapping
    {
        public string ApplicationName { get; set; }
        public string AppicationID { get; set; }
        public string CorrelationID { get; set; }
        public string UsecaseName { get; set; }
        public string ClientUID { get; set; }
        public string DCID { get; set; }
        public string BusinessProblems { get; set; }
        public string ModelName { get; set; }
        public string SourceURL { get; set; }
        public string DataSource { get; set; }
        public string IsPrivate { get; set; }
        //public string ModelVersion { get; set; }
        public string SourceName { get; set; }
        //public string ModelType { get; set; }
        //  public string InputSample { get; set; }
        public object InputColumns { get; set; }
        public object AvailableColumns { get; set; }
        public string UseCaseID { get; set; }
        public string RequestStatus { get; set; }
        public string ProblemType { get; set; }
        public string pageInfo { get; set; }
        public string Function { get; set; }
        public string CreatedByUser { get; set; }
        public string CreatedOn { get; set; }
        public string ModifiedByUser { get; set; }
        public string ModifiedOn { get; set; }
        public string TargetColumn { get; set; }
        public string TargetUniqueIdentifier { get; set; }
        public string ParamArgs { get; set; }
        public object TimeSeries { get; set; }

    }

    public class CustomFileUpload
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
        public CustomPayload Customdetails { get; set; }
    }
    public class CustomSPAFileUpload
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
        public CustomSPAPayload Customdetails { get; set; }
    }

    public class CustomPayload
    {
        public string AppId { get; set; }
        public string HttpMethod { get; set; }
        public string AppUrl { get; set; }
        public dynamic InputParameters { get; set; }
        public string DateColumn { get; set; }
        public string UsecaseID { get; set; }
    }
    public class CustomSPAPayload
    {
        public string AppId { get; set; }
        public string HttpMethod { get; set; }
        public string AppUrl { get; set; }
        public SPAInputParams InputParameters { get; set; }
        public string DateColumn { get; set; }
        public string UsecaseID { get; set; }
    }


    public class preprocessData
    {
        public string Lemmitize { get; set; }
        public string Stemming { get; set; }
        public string Pos { get; set; }
        public string[] Stopwords { get; set; }
        public int Least_Frequent { get; set; }
        public int Most_Frequent { get; set; }
    }

    public class SPAInputParams
    {
        public string CorrelationId { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public double TotalRecordCount { get; set; }
        public double PageNumber { get; set; }
        public double BatchSize { get; set; }

        public string IterationUId { get; set; }
        public string TeamAreaUId { get; set; }
    }
    public class SPAFileUpload
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
        //public dynamic Customdetails { get; set; } // handle with dynamic only, since this is one code base (FDS PAM PAD) public CustomSPAPayload Customdetails { get; set; }
        public CustomSPAPayload Customdetails { get; set; }
    }

    public class ModelMetric
    {
        public string ModelName { get; set; }
        public object DeployedAccuracy { get; set; }
    }
    public class LogAutoTrainedFeatures
    {
        public string LogId { get; set; }
        public int ModelsCount { get; set; }
        public int Sequence { get; set; }
        public string[] ModelList { get; set; }
        public string FeatureName { get; set; }
        public string FunctionName { get; set; }
        public string ErrorMessage { get; set; }
        public string StartedOn { get; set; }
        public string EndedOn { get; set; }
    }

    public class LogAIServiceAutoTrain
    {
        public string CorrelationId { get; set; }
        public string ServiceId { get; set; }
        public string PageInfo { get; set; }
        public string FunctionName { get; set; }
        public string ErrorMessage { get; set; }
        public string Message { get; set; }
        public string Status { get; set; }
        public string Progress { get; set; }
        public string StartedOn { get; set; }
        public string EndedOn { get; set; }
    }
}
