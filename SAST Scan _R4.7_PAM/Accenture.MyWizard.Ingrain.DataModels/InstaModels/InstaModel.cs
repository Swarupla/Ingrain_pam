using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.DataModels.InstaModels
{
    public class InstaModel
    {
        public string CorrelationId { get; set; }
        public string InstaID { get; set; }
        public string Message { get; set; }
        public string Status { get; set; }
        public string ErrorMessage { get; set; }
        public string PredictionData { get; set; }
    }
    public class InstaPrediction
    {
        public string InstaID { get; set; }
        public string CorrelationId { get; set; }
        public string ProblemType { get; set; }
        public string ProcessName { get; set; }
        public string Message { get; set; }
        public string ActualData { get; set; }
        public object PredictedData { get; set; }
        public string ClientUID { get; set; }
        public string DCID { get; set; }
        public string CreatedByUser { get; set; }
        public string ErrorMessage { get; set; }
        public string Status { get; set; }

    }
    public class InstaRegression
    {
        public string UseCaseID { get; set; }
        public List<instaMLResponse> instaMLResponse { get; set; }
    }
    public class instaMLResponse
    {
        public string InstaID { get; set; }
        public string CorrelationId { get; set; }
        public string Message { get; set; }
        public string ErrorMessage { get; set; }
        public string Status { get; set; }
        public string ActualData { get; set; }
        public List<dynamic> PredictedData { get; set; }
    }
}
