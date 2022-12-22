using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization.Attributes;

namespace Accenture.MyWizard.Ingrain.WindowService.Models.SaaS
{
    public class HistoricalIOTicketList
    {
        public List<IOTicketCollection> Tickets { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class IOTicketCollection
    {
        #region Properties

        [BsonId]
        public object _id { get; set; }

        [BsonIgnoreIfNull]
        public List<DeliveryConstruct> DeliveryConstruct { get; set; }

        public string ClientUId { get; set; }

        [BsonIgnoreIfNull]
        public string EndToEndUId { get; set; }

        public string ToolInstanceUId { get; set; }

        public string EntityUId { get; set; }

        public string IncidentNumber { get; set; }

        public string AssigneeLoginId { get; set; }

        public string ExternalAssigneeLoginId { get; set; }

        public string ServiceType { get; set; }

        public string NormalisedTicketType { get; set; }

        public string TicketDescription { get; set; }

        public DateTime ReportedDateTime { get; set; }

        public DateTime? LastModifiedTime { get; set; }

        public string Priority { get; set; }

        public string Status { get; set; }

        public DateTime? DueDate { get; set; }

        public string SupportGroupName { get; set; }

        public string Company { get; set; }

        public string ResolutionDescription { get; set; }

        public string Category { get; set; }

        public string ReportedSource { get; set; }

        public string LongDescription { get; set; }

        public string CreatedByUser { get; set; }

        public string CreatedByApp { get; set; }

        public string ModifiedByApp { get; set; }

        public string ConfigurationItem { get; set; }

        public string ConfigurationItemClass { get; set; }

        public decimal EstimatedDaystoResolution { get; set; }

        public int BusinessProcessId { get; set; }

        public int BusinessProcessStepId { get; set; }

        public string ResolutionMechanism { get; set; }

        public string DeepLink { get; set; }

        public double Holdtime { get; set; }

        public string ResolutionType { get; set; }

        public DateTime RespondedDate { get; set; }

        public DateTime? LastResolvedDate { get; set; }

        public string StatusReason { get; set; }

        public string SLAResponse { get; set; }

        public string SLAResolution { get; set; }

        public DateTime? ReopenedDate { get; set; }

        public string SupportOrgName { get; set; }

        public DateTime? ResponseDate { get; set; }

        public DateTime? ResolutionDate { get; set; }

        public DateTime? ResolutionDueDate { get; set; }

        public DateTime? ResponseDueDate { get; set; }

        public string Issue { get; set; }

        public DateTime? ClosedDate { get; set; }

        public string TicketCreator { get; set; }

        public string Source { get; set; }

        public int TeamSize { get; set; }

        public string Subproject { get; set; }

        public string DeliveryCenter { get; set; }

        public string ExternalCreatedBy { get; set; }

        public string CreatedBy { get; set; }

        public string TicketCategory { get; set; }

        public string BaseInstanceUrl { get; set; }

        public double Effort { get; set; }

        public string Impact { get; set; }

        public string Urgency { get; set; }

        public string CorrelationId { get; set; }

        public string IncidentState { get; set; }

        public string CategoryPath { get; set; }

        public int StatusId { get; set; }

        public int PriorityId { get; set; }

        public int StatusReasonId { get; set; }

        public int SLAResponseId { get; set; }

        public int ImpactId { get; set; }

        public int UrgencyId { get; set; }

        public string ClosureCode { get; set; }

        public string SLABreachReason { get; set; }

        public string RelatedIncidents { get; set; }

        public string RelatedProblems { get; set; }

        public string RelatedRequests { get; set; }

        public string RelatedEvents { get; set; }

        public string RelatedChanges { get; set; }

        public string RelatedTasks { get; set; }

        public int ReopenCount { get; set; }

        public string Outage { get; set; }

        public string ApplicationName { get; set; }

        public DateTime? CreatedOn { get; set; }

        public DateTime? ModifiedOn { get; set; }

        public string TicketExtensions { get; set; }

        public string DeliveryType { get; set; }

        public string EntityType { get; set; }

        public string Technology { get; set; }

        public decimal TATResponse { get; set; } = 0;

        public decimal TATResolution { get; set; } = 0;

        public int flag { get; set; } = 0;

        public string VDSSource { get; set; }

        public string ID { get; set; }

        public string MYWStatus { get; set; }
        public string MYWImpact { get; set; }
        public string MYWUrgency { get; set; }
        public string MYWPriority { get; set; }

        //added for io

        public string Domain { get; set; }

        public double EffortInHours { get; set; }

        public DateTime? StartOn { get; set; }

        public string CRType { get; set; }

        public DateTime? ActualStartOn { get; set; }

        public DateTime? ActualEndOn { get; set; }

        public DateTime? PlannedStartOn { get; set; }

        public DateTime? PlannedEndOn { get; set; }

        public string Reason { get; set; }

        public bool Expedited { get; set; }

        public string BusinessJustification { get; set; }

        public string WorkNotes { get; set; }

        public string Reference { get; set; }

        public string OpenedBy { get; set; }

        public string Risk { get; set; }

        public int? OutageDuration { get; set; }

        public bool MajorIncident { get; set; }

        public string RequestorCountry { get; set; }

        public string RequestorCity { get; set; }

        public string ConfigurationItemStatus { get; set; }

        public string ConfigurationItemLocation { get; set; }

        public bool FCR { get; set; }

        public string ClosedBy { get; set; }

        public string ResolvedBy { get; set; }

        public string Knowledge { get; set; }

        public DateTime? RCICompleteOn { get; set; }

        public DateTime? RCIConfirmationOn { get; set; }

        public string ProblemSource { get; set; }

        public string Owner { get; set; }

        [BsonIgnoreIfNull]
        public int? HourlyResolutionBucket { get; set; }

        public string HourClassification { get; set; }

        public int? ReportedHourBin { get; set; }

        public string FourHourlyClassification { get; set; }

        public int? AgeingBucketInDays { get; set; }
        public string TktAgeingBucketInDays { get; set; }

        public string Ageing { get; set; }

        public DateTime? AssignedDateTime { get; set; }

        public int? SLAResolutionId { get; set; }

        public string RequestorLocation { get; set; }

        public string ResponseSLAExclusion { get; set; }

        public string ResolutionSLAExclusion { get; set; }

        public string ExternalCallerId { get; set; }

        public string LineofBusiness { get; set; }

        public string ApprovalStatus { get; set; }

        public DateTime? ApprovedDate { get; set; }

        //Start : Nivedita
        public string EntityName { get; set; }

        //public string ResolvedBy { get; set; }

        //public DateTime? PlannedEndOn { get; set; }

        //public DateTime? ActualEndOn { get; set; }

        public DateTime? CalcClosedDate { get; set; }

        public string CalcTimeInterval { get; set; }

        public DateTime? CalTime { get; set; }

        public string CalcAutogenerated { get; set; }

        public double? CalcResolution { get; set; }

        public string CalcRepeatedCategory { get; set; }

        public string CalcCloseGroup { get; set; }

        public string CalcTower { get; set; }

        public string CalcRepeatedHostNameOfCI { get; set; }

        public string AdherenceToSchedule { get; set; }

        public string TaskLevelCompliance { get; set; }

        public string ChangeLevelCompliance { get; set; }
        //End: Nivedita

        //Start: Swati
        public string CalcTicketDescription { get; set; }
        public string CalcCombinedTag { get; set; }
        public string CalcTag1 { get; set; }
        public string CalcTag2 { get; set; }
        public string CalcTag3 { get; set; }
        public string CalcSelfHelp { get; set; }
        //End: Swati
        public string NotificationMethod { get; set; }
        public int? ReassignmentCount { get; set; }
        public string CalcReassignment { get; set; }
        #endregion

        #region Constructors
        public IOTicketCollection()
        {
        }
        #endregion
    }

    public class DeliveryConstruct
    {
        #region Properties
        public string DeliveryConstructUId { get; set; }

        public int TicketStatusId { get; set; }

        public string CalcRepeatedCI { get; set; }
        public string CalcRepeatedCategory { get; set; }
        #endregion
    }


    //[BsonIgnoreExtraElements]
    //public class IOAutogenerated
    //{
    //    public List<string> Channel { get; set; }
    //}

    [BsonIgnoreExtraElements]
    public class IOKeywords
    {
        public string key { get; set; }
        public List<string> value { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class IssueTags
    {
        public int RuleNo { get; set; }
        public List<Rules> Rule { get; set; }
        public List<Tags> AssociatedTagDetails { get; set; }
        public string CombinedIssueTag { get; set; }

    }

    [BsonIgnoreExtraElements]
    public class Rules
    {
        public int RuleOrder { get; set; }
        public string RuleKeyword { get; set; }
        public string RuleField { get; set; }

    }

    [BsonIgnoreExtraElements]
    public class Tags
    {
        public string CalcTag1 { get; set; }
        public string CalcTag2 { get; set; }
        public string CalcTag3 { get; set; }

    }
    [BsonIgnoreExtraElements]
    public class CMSList
    {
        #region Properties
        public string CMSId { get; set; }
        public string CMSName { get; set; }
        #endregion
    }

    [BsonIgnoreExtraElements]
    public class WorkStreamList
    {
        #region Properties
        public string WSId { get; set; }
        public string WSName { get; set; }
        #endregion
    }
    [BsonIgnoreExtraElements]
    public class RepeatedCIWS
    {
        public string WSId { get; set; }
        public string CIname { get; set; }
        public string CIvalue { get; set; }

    }
}
