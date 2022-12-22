using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Accenture.MyWizard.Ingrain.DataModels.Models
{
    public class PredictionDTO
    {
        public string _id { get; set; }
        public string Chunk_number { get; set; }
        public string UniqueId { get; set; }
        public string AppID { get; set; }
        public string CorrelationId { get; set; }
        public string Frequency { get; set; }
        public string ActualData { get; set; }
        public string PredictedData { get; set; }
        public string PreviousData { get; set; }
        public string UseCaseId { get; set; }
        public string TempalteUseCaseId { get; set; }
        public string Status { get; set; }
        public string DataPointsWarning { get; set; }
        public long DataPointsCount { get; set; }
        public string InstaId { get; set; }
        public string ErrorMessage { get; set; }
        public string Progress { get; set; }
        public string CreatedOn { get; set; }
        public string CreatedByUser { get; set; }
        public string ModifiedOn { get; set; }
        public string ModifiedByUser { get; set; }

        public List<string> StartDates { get; set; }

    }

    public class PredictionResultDTO
    {
        public string CorrelationId { get; set; }

        public string UniqueId { get; set; }

        public string Message { get; set; }

        public string PredictedData { get; set; }

        public string Status { get; set; }
        public long DataPointsCount { get; set; }
        public string DataPointsWarning { get; set; }
        public string Progress { get; set; }

        public string ErrorMessage { get; set; }
    }
    public class SPAPredictionDTO
    {
        public Newtonsoft.Json.Linq.JObject PredictedData { get; set; }
        public string Status { get; set; }
        public string ErrorMessage { get; set; }
        public string Message { get; set; }        
    }
    public class ForeCastModel
    {
        public string Status { get; set; }
        public string PredictionResult { get; set; }
        public string Message { get; set; }
        public long DataPointsCount { get; set; }
        public string DataPointsWarning { get; set; }
    }
}
