using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Accenture.MyWizard.Ingrain.DataModels.Models
{
    public class ClusteringAPIModel
    {
        [BsonId]
        public string _id { get; set; }
        public string ClientID { get; set; }
        public string DCUID { get; set; }
        public string ServiceID { get; set; }
        public string UserId { get; set; }
        public string ParamArgs { get; set; }
        public string TeamAreaUID { get; set; }
        public string CorrelationId { get; set; }

        public string UsecaseId { get; set; }
        public string DataSetUId { get; set; }
        public string PageInfo { get; set; }
        public string UniId { get; set; }
        // public string CreatedDate { get; set; }
        public string Token { get; set; }
        public JObject ProblemType { get; set; }
        public JObject SelectedModels { get; set; }
        public string ModelName { get; set; }
        //     public string PredictionURL { get; set; }
        public string CreatedOn { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedOn { get; set; }
        public string ModifiedBy { get; set; }
        //     public string ModelStatus { get; set; }
        //   public string PythonModelName { get; set; }
        //public string StatusMessage { get; set; }
        public bool retrain { get; set; }
        public bool DBEncryptionRequired { get; set; }

        public List<dynamic> Columnsselectedbyuser { get; set; }
        public List<string> StopWords { get; set; }
        public string SourceName { get; set; }
        public string DataSource { get; set; }
        public string Language { get; set; }
        public int[] Ngram { get; set; }
        public string ScoreUniqueName { get; set; }
        public dynamic Threshold_TopnRecords { get; set; }
        public int MaxDataPull { get; set; }
        public bool IsCarryOutRetraining { get; set; }
        public bool IsOnline { get; set; }
        public bool IsOffline { get; set; }
        public ClustModelFrequency Training { get; set; }
        public ClustModelFrequency Prediction { get; set; }
        public ClustModelFrequency Retraining { get; set; }
        public int RetrainingFrequencyInDays { get; set; }
        public int TrainingFrequencyInDays { get; set; }
        public int PredictionFrequencyInDays { get; set; }
    }
    public class ClustModelFrequency
    {
        public int Hourly { get; set; }
        public int Daily { get; set; }
        public int Weekly { get; set; }
        public int Monthly { get; set; }
        public int Fortnightly { get; set; }
        public int RetryCount { get; set; }
    }

    public class ClusteringStatus
    {
        public string CorrelationId { get; set; }
        public string Status { get; set; }
        public string Progress { get; set; }
        public string Message { get; set; }
        public string UniId { get; set; }
        public string pageInfo { get; set; }
        public string CreatedByUser { get; set; }
        public string ModifiedOn { get; set; }
        public string ModifiedByUser { get; set; }
        public string RequestStatus { get; set; }
        public string ServiceID { get; set; }
        public string PredictionURL { get; set; }
        public string ModelType { get; set; }
        public string ModelName { get; set; }
        public string Clustering_type { get; set; }
        public double Silhouette_Coefficient { get; set; }
        public double Clusters { get; set; }
        public JObject ingestData { get; set; }
        public JObject Suggestion { get; set; }
        public string DataSetUId { get; set; }
        public List<dynamic> ColumnNames { get; set; }

        public List<dynamic> Columnsselectedbyuser { get; set; }
    }


    public class ClusteringModel
    {
        public List<ClusteringStatus> clusteringStatus { get; set; }
        public List<JObject> ClusteredColumns { get; set; }
        public List<VisulalisationData> VisulalisationDatas { get; set; }
    }


    public class DownloadMappedDataStatus
    {
        public string CorrelationId { get; set; }
        public string Status { get; set; }
        public string Progress { get; set; }
        public string Message { get; set; }

        public string UniId { get; set; }

        public List<object> InputData { get; set; }

    }

    public class VisulalisationDataStatus
    {
        public string ClientID { get; set; }
        public string DCUID { get; set; }
        public string CorrelationId { get; set; }
        public string Status { get; set; }
        public string Progress { get; set; }
        public string Message { get; set; }
        public string UniId { get; set; }
        public JObject Visualization_Response { get; set; }
        public JObject Frequency_Count{get;set;}
    }

    public class VisulalisationData
    {
        public string ClientID { get; set; }
        public string DCUID { get; set; }
        public string CorrelationId { get; set; }
        public string ModelName { get; set; }
        public string Clustering_type { get; set; }
        public JObject Visualization_Response { get; set; }
        public JObject Frequency_Count { get; set; }
    }
    public class WordCloudRequest
    {
        public string CorrelationId { get; set; }
        public List<string> SelectedColumns { get; set; }
        public List<string> StopWords { get; set; }
    }


    public class WordCloudResponse
    {
        public string CorrelationId { get; set; }
        public string MyProperty { get; set; }
    }
    [BsonIgnoreExtraElements]
    public class ClusterStatusModel
    {
        public string CorrelationId { get; set; }
        public string Status { get; set; }
        public string ModifiedOn { get; set; }
    }
}
