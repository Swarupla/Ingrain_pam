using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.DataModels.MonteCarlo
{
    public class TemplateInfo
    {
        public string TemplateId { get; set; }
        public string Version { get; set; }
        public string User { get; set; }
        public JObject Features { get; set; }
        public string SelectedCurrentRelease { get; set; }
        public Dictionary<string, string> ColumnList { get; set; }
        public string ProblemType { get; set; }
        public string UniqueIdentifierName { get; set; }
        public string DeliveryTypeID { get; set; }
        public string DeliveryTypeName { get; set; }
        public string UseCaseName { get; set; }
        public string UseCaseID { get; set; }
        public string UseCaseDescription { get; set; }
        public bool IsUploaded { get; set; }
        public List<TemplateVersion> TemplateVersions { get; set; }
        public List<SimulationVersion> SimulationVersions { get; set; }
        public bool IsSimulationExist { get; set; }
        public string ErrorMessage { get; set; }
        public string Status { get; set; }
        public bool IsException { get; set; }
        public string[] InputColumns { get; set; }
        public JObject InputSelection { get; set; }
        public JObject MainSelection { get; set; }
        public bool isDBEncryption { get; set; }
        public string Progress { get; set; }
        public string Message { get; set; }
        public string VersionMessage { get; set; }
        public bool InsertBase { get; set; }
        public bool blankData { get; set; }
    }
    public class TemplateData
    {
        public string _id { get; set; }
        public string Version { get; set; }
        public string TemplateID { get; set; }
        public string ClientUID { get; set; }
        public string DeliveryConstructUID { get; set; }
        public string ProblemType { get; set; }
        public string[] MainColumns { get; set; }
        public dynamic MainSelection { get; set; }
        public string TargetColumn { get; set; }
        public string UniqueIdentifierName { get; set; }
        public string[] InputColumns { get; set; }
        public dynamic InputSelection { get; set; }
        public dynamic Features { get; set; }
        public string DeliveryTypeID { get; set; }
        public string DeliveryTypeName { get; set; }
        public string UseCaseName { get; set; }
        public string UseCaseID { get; set; }
        public string UseCaseDescription { get; set; }
        public Dictionary<string, string> TargetColumnList { get; set; }
        public Dictionary<string, string> UniqueColumnList { get; set; }
        public string CreatedOn { get; set; }
        public string CreatedByUser { get; set; }
        public string ModifiedOn { get; set; }
        public string ModifiedByUser { get; set; }
        public bool isDBEncryption { get; set; }
        public string Status { get; set; }
        public string Progress { get; set; }
        public string Message { get; set; }
        public string VersionMessage { get; set; }
        public string SelectedCurrentRelease { get; set; }
    }
    public class InputTemplateInfo
    {
        public string TemplateID { get; set; }
        public string Version { get; set; }
        public string User { get; set; }
        public JObject Features { get; set; }
        public string ReleaseState { get; set; }
        public string ReleaseName { get; set; }
        public string ReleaseStartDate { get; set; }
        public string ReleaseEndDate { get; set; }
    }
    public class GetInputInfo
    {
        public JObject TemplateInfo { get; set; }
        public string SimulationID { get; set; }
        public string SimulationVersion { get; set; }
        public List<TemplateVersion> TemplateVersions { get; set; }
        public bool IsTemplates { get; set; }
        public bool IsSimulationExist { get; set; }
    }
    public class GetOutputInfo
    {
        public JObject SimulationInfo { get; set; }
        public List<TemplateVersion> TemplateVersions { get; set; }
        public List<SimulationVersion> SimulationVersions { get; set; }
        public bool IsSimulationExist { get; set; }
    }
    public class UpdateFeatures
    {
        public bool Status { get; set; }
        public string Message { get; set; }
        public string ErrorMessage { get; set; }
    }
    public class GenericFile
    {
        public string TemplateId { get; set; }
        public string ClientUID { get; set; }
        public string DeliveryConstructUID { get; set; }
        public string ProblemType { get; set; }
        public string DeliveryTypeID { get; set; }
        public string DeliveryTypeName { get; set; }
        public string User { get; set; }
        public Dictionary<string, string> TargetColumnList { get; set; }
        public Dictionary<string, string> UniqueColumnList { get; set; }
        public bool IsUploaded { get; set; }
        public string UseCaseID { get; set; }
        public string UseCaseName { get; set; }
        public string UseCaseDescription { get; set; }
        public List<TemplateVersion> TemplateVersions { get; set; }
        public List<SimulationVersion> SimulationVersions { get; set; }
        public string ErrorMessage { get; set; }
        public string Status { get; set; }
        public bool IsException { get; set; }
    }

    public class SimulationPrediction
    {
        public double TargetVariable { get; set; }
        public string TemplateID { get; set; }
        public string SimulationID { get; set; }
        public string TemplateVersion { get; set; }
        public string SimulationVersion { get; set; }
        public string ProblemType { get; set; }
        public string TargetColumn { get; set; }
        public double TargetCertainty { get; set; }
        public string Warning { get; set; }
        public string ConvergenceAlert { get; set; }
        public JObject Influencers { get; set; }
        public JObject Defect { get; set; }
        public JObject Effort { get; set; }
        public JObject Schedule { get; set; }
        public JObject TeamSize { get; set; }
        public JObject TargetColumnData { get; set; }
        public JObject SensitivityReport { get; set; }
        public JObject PercentChange { get; set; }
        public JObject IncrementFlags { get; set; }
        public string Observation { get; set; }
        public List<TemplateVersion> TemplateVersions { get; set; }
        public List<SimulationVersion> SimulationVersions { get; set; }
        public string UseCaseID { get; set; }
        public string UseCaseName { get; set; }
        public string UseCaseDescription { get; set; }
        public string ErrorMessage { get; set; }
        public string Status { get; set; }
        public double FlagTeamSize { get; set; }
        public string SelectedCurrentRelease { get; set; }
    }

    public class WeeklyPrediction
    {
        public string Progress { get; set; }
        public string ErrorMessage { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
    }
    public class whatIfPrediction
    {
        public JObject Influencers { get; set; }
        public string Observation { get; set; }
        public double TargetCertainty { get; set; }
        public double TargetVariable { get; set; }
        public string TargetColumn { get; set; }
        public JObject PercentChange { get; set; }
        public JObject IncrementFlags { get; set; }
        public string ErrorMessage { get; set; }
        public string Status { get; set; }
    }
    public class TemplateVersion
    {
        public string Version { get; set; }
        public string TemplateID { get; set; }
    }
    public class SimulationVersion
    {
        public string Version { get; set; }
        public string TemplateID { get; set; }
        public string SimulationID { get; set; }
    }
    public class VersionList
    {
        public string TemplateVersion { get; set; }
        public string SimulationVersion { get; set; }
        public string TemplateID { get; set; }
        public string SimulationID { get; set; }
        public List<TemplateVersion> TemplateVersions { get; set; }
        public List<SimulationVersion> SimulationVersions { get; set; }
    }
    public class VersionCounter
    {
        public string VersionCount { get; set; }
        public long Seq { get; set; }
        public string ClientUID { get; set; }
        public string DeliveryConstructUID { get; set; }
        public string UseCaseName { get; set; }
        public string UseCaseID { get; set; }
    }
    public class UseCaseModelsList
    {
        public List<VDSUseCaseModels> UseCaseModels { get; set; }
        public string Message { get; set; }
        public string Status { get; set; }
        public string ErrorMessage { get; set; }
    }
    [BsonIgnoreExtraElements]
    public class VDSUseCaseDBModels
    {
        public string Version { get; set; }
        public string TemplateID { get; set; }
        public string CreatedByUser { get; set; }
        public string ClientUID { get; set; }
        public string DeliveryConstructUID { get; set; }
        public string ProblemType { get; set; }
        public bool IsLoggedInUser { get; set; }
        public string DeliveryTypeID { get; set; }
        public string DeliveryTypeName { get; set; }
        public string UseCaseName { get; set; }
        public string UseCaseID { get; set; }
        public string UseCaseDescription { get; set; }

        public string CreatedOn { get; set; }

        public string Status { get; set; }
    }
    public class VDSUseCaseModels
    {
        public string TemplateVersion { get; set; }
        public string TemplateID { get; set; }
        public string UserID { get; set; }
        public string ClientUID { get; set; }
        public string DeliveryConstructUID { get; set; }
        public string ProblemType { get; set; }
        public bool IsLoggedInUser { get; set; }
        public string DeliveryTypeID { get; set; }
        public string DeliveryTypeName { get; set; }
        public string UseCaseName { get; set; }
        public string UseCaseID { get; set; }
        public string UseCaseDescription { get; set; }
        public bool IsSimulationExist { get; set; }
    }
    public class DeletedUseCaseDetails
    {
        public string UseCaseID { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
        public string ErrorMessage { get; set; }
    }
    public class CloneTemplateData : DeletedUseCaseDetails
    {
        public string TemplateVersion { get; set; }
        public string TemplateID { get; set; }
        public GetInputInfo TemplateData { get; set; }
    }
    public class CloneSimulation
    {
        public string SimulationVersion { get; set; }
        public string TemplateID { get; set; }
        public string SimulationID { get; set; }
        public GetOutputInfo SimulationData { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
        public string ErrorMessage { get; set; }
    }
}
