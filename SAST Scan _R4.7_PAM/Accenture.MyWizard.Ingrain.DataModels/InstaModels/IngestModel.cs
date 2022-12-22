using System;
using System.Collections.Generic;
using System.Text;
using Models= Accenture.MyWizard.Ingrain.DataModels.Models;

namespace Accenture.MyWizard.Ingrain.DataModels.InstaModels
{
    public class IngestModel
    {
        public string InstaId { get; set; }
        public string CorrelationId { get; set; }
        public string ProblemType { get; set; }
        public string TargetColumn { get; set; }
        public string[] InputColumns { get; set; }
        public string[] AvailableColumns { get; set; }
        public string UserId { get; set; }
        public List<ProblemTypeDetails> ProblemTypeDetails { get; set; }
        public string UniqueIdentifier { get; set; }
        public string URL { get; set; }
        public string DataSource { get; set; }
        public string ModelName { get; set; }
        public string UseCaseID { get; set; }
        public string Aggregation { get; set; }
        public string ClientUId { get; set; }
        public string DeliveryConstructUID { get; set; }
        public string CreatedByUser { get; set; }
        public string ModifiedByUser { get; set; }
    }
    public class ProblemTypeDetails
    {
        public string InstaID { get; set; }
        public string ProblemType { get; set; }
        public string TargetColumn { get; set; }
        public string[] SelectedFeatures { get; set; }
    }
    public class TimeSeriesModel
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
    }
    public class TimeSeries
    {
        public string Name { get; set; }
        public string Steps { get; set; }
    }
    public class VdsData
    {
        public string InstaId { get; set; }
        public string CorrelationId { get; set; }
        public string CreatedByUser { get; set; }
        public string ProblemType { get; set; }
        public string ProcessName { get; set; }
        public string ClientUId { get; set; }
        public string DeliveryConstructUID { get; set; }
        public string Dimension { get; set; }
        public string DCID { get; set; }
        public string ActualData { get; set; }
        public string PredictData { get; set; }
        public string URL { get; set; }
        public string TargetColumn { get; set; }
        public string Frequency { get; set; }
        public string UseCaseName { get; set; }
        public string UseCaseDescription { get; set; }
        public string Source { get; set; }
    }
    public class VDSRegression
    {
        public string UseCaseID { get; set; }
        public string FrequencySteps { get; set; }
        public string Frequency { get; set; }
        public string Dimension { get; set; }
        public string ClientUID { get; set; }
        public string URL { get; set; }
        public string Source { get; set; }
        public string DCID { get; set; }
        public string ProcessName { get; set; }
        public string CreatedByUser { get; set; }
        public List<Models.ProblemTypeDetails> ProblemTypeDetails { get; set; }
    }    
    public class FitModel
    {
        public string InstaID { get; set; }
        public string ClientUID { get; set; }
        public string DCID { get; set; }
        public string ProblemType { get; set; }
        public string TargetColumn { get; set; }
        public string Dimension { get; set; }
        public string LastFitDate { get; set; }
        public string ProcessFlow { get; set; }
    }   
    public class RegressionTimeSeriesModel
    {
        public string InstaID { get; set; }
        public string ClientUID { get; set; }
        public string DCID { get; set; }
        public string UseCaseID { get; set; }
        public string lastFitDate { get; set; }        
        public string ProcessFlow { get; set; }
    }
    public class PAMData
    {
        public string username { get; set; }
        public string password { get; set; }
    }
    public class InstaMLData
    {
        public string UseCaseID { get; set; }
        public string ClientUID { get; set; }
        public string DCID { get; set; }
        public string lastFitDate { get; set; }
        public string ProcessFlow { get; set; }
    }
}
