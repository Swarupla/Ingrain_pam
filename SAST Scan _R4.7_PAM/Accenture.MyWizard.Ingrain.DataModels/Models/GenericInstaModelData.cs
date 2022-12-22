using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Accenture.MyWizard.Ingrain.DataModels.Models
{
    public class GenericInstaModelData
    {
        public string ApplicationName { get; set; }
        public string ApplicationID { get; set; }
        public string CorrelationId { get; set; }
        public string InstaId { get; set; }
        public string UserId { get; set; }
        public string ClientUId { get; set; }
        public string DeliveryConstructUID { get; set; }
        public string CreatedByUser { get; set; }
        public string ModifiedByUser { get; set; }
        public string ProblemType { get; set; }
        public string ProcessName { get; set; }
        public string ModelName { get; set; }
        public string TargetColumn { get; set; }
        public string[] InputColumns { get; set; }
        public string[] AvailableColumns { get; set; }
        public List<GenericProblemTypeDetail> ProblemTypeDetails { get; set; }
        public string UniqueIdentifier { get; set; }
        public string URL { get; set; }
        public string DataSource { get; set; }
        public string Aggregation { get; set; }
        public string IsTimeSeries { get; set; }
        public string Frequency { get; set; }
        public string FrequencySteps { get; set; }
        public string SelectedFeatures { get; set; }
        public string UseCaseID { get; set; }
        public string Dimension { get; set; }
        public string ActualData { get; set; }
    }

    //public class InstaModel
    //{
    //    public string CorrelationId { get; set; }
    //    public string InstaID { get; set; }
    //    public string Message { get; set; }
    //    public string Status { get; set; }
    //    public string ErrorMessage { get; set; }
    //    public string PredictionData { get; set; }
    //}

    public class GenericInstaModelResponse
    {
        public string CorrelationId { get; set; }
        public string InstaID { get; set; }
        public string Message { get; set; }
        public string Status { get; set; }
        public string ErrorMessage { get; set; }
        public string PredictionData { get; set; }

        public string UseCaseID { get; set; }
        public List<GenericInstaMLResponses> instaMLResponse { get; set; }
    }

    public class GenericInstaMLResponses
    {
        public string InstaID { get; set; }
        public string CorrelationId { get; set; }
        public string Message { get; set; }
        public string ErrorMessage { get; set; }
        public string Status { get; set; }
        public string ActualData { get; set; }
        public string PredictedData { get; set; }
    }
    public class AppRegression
    {
        public string UseCaseID { get; set; }
        public string ClientUID { get; set; }
        public string DCID { get; set; }
        public string ProcessName { get; set; }
        public string CreatedByUser { get; set; }
        public List<Models.ProblemTypeDetails> ProblemTypeDetails { get; set; }
    }

    public class TimeSeriesData
    {
        public string InstaId { get; set; }
        public string CorrelationId { get; set; }
        public string ProblemType { get; set; }
        public string TargetColumn { get; set; }
        public string Dimension { get; set; }
        public string UserId { get; set; }
        public string Frequency { get; set; }
        public string UseCaseID { get; set; }
        public Int32 FrequencySteps { get; set; }
        public string URL { get; set; }
        public string DataSource { get; set; }
        public string ModelName { get; set; }
        public string ClientUId { get; set; }
        public string DeliveryConstructUID { get; set; }
        public string CreatedByUser { get; set; }
        public string ModifiedByUser { get; set; }
        public string AppID { get; set; }
    }

    public class IngestData
    {
        public string InstaId { get; set; }
        public string AppID { get; set; }
        public string CorrelationId { get; set; }
        public string ProblemType { get; set; }
        public string TargetColumn { get; set; }
        public string[] InputColumns { get; set; }
        public string[] AvailableColumns { get; set; }
        public string UserId { get; set; }
        public List<GenericProblemTypeDetail> ProblemTypeDetails { get; set; }
        public string UniqueIdentifier { get; set; }
        public string URL { get; set; }
        public string DataSource { get; set; }
        public string ModelName { get; set; }
        public string Aggregation { get; set; }
        public string ClientUId { get; set; }
        public string DeliveryConstructUID { get; set; }
        public string CreatedByUser { get; set; }
        public string ModifiedByUser { get; set; }
    }

    public class GenericProblemTypeDetail
    {
        public string CorrelationId { get; set; }
        public string InstaID { get; set; }
        public string ProblemType { get; set; }
        public string TargetColumn { get; set; }
        public string[] SelectedFeatures { get; set; }
    }


}
