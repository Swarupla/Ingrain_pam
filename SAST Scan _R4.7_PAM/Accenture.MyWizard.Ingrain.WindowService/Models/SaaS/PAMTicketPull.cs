using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;
using Accenture.MyWizard.Ingrain.DataModels.Models;
using Newtonsoft.Json;
using WINSERVICEMODELS = Accenture.MyWizard.Ingrain.WindowService.Models;

namespace Accenture.MyWizard.Ingrain.WindowService.Models.SaaS
{
    public class AIAAMiddleLayerResponse
    {
        /// <summary>
        /// 
        /// </summary>
        public string Response { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public object ProjectId { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public List<string> faults { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public UploadHistoricalTicketRequest request { get; set; }
        public WorkItemsList WorkItems { get; set; }
        public IterationsList Iterations { get; set; }
        public TicketList Tickets { get; set; }

        public TaskList Tasks { get; set; }
        public CodeCommitList CodeCommits { get; set; }
        public CodeBranchList CodeBranches { get; set; }
        public BuildList Builds { get; set; }
        public TestResultList TestResults { get; set; }
        public DeploymentList Deployment { get; set; }
    }

    ///<summary>Represents Message (Request) Contract for the "UploadHistoricalTicketRequest" Service Operation.</summary>
    ///<remarks>Represents Message (Request) Contract for the "UploadHistoricalTicketRequest" Service Operation.</remarks>

    public class UploadHistoricalTicketRequest
    {
        /// <summary>
        /// DDName
        /// </summary>
        public string DDName { get; set; }
        /// <summary>
        /// <summary>
        /// 
        /// </summary>
        public List<DeliveryStructureAttribute> DeliveryStructureAttribute { get; set; }
        /// <summary>
        /// HistoricalTicketData
        /// </summary>
        public List<HistoricalTicketData> HistoricalTicketData { get; set; }
        /// <summary>
        /// DeliveryConstructVersions
        /// </summary>
        public List<DeliveryConstructVersion> DeliveryConstructVersions { get; set; }
        /// <summary>
        /// SupportGroupName
        /// </summary>
        public List<string> SupportGroupName { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string E2EUID { get; set; }
        /// <summary>
        /// ParentUID
        /// </summary>
        public string ParentUID { get; set; }
        /// <summary>
        /// ChildUID
        /// </summary>
        public string ChildUID { get; set; }
        /// <summary>
        /// StartDate
        /// </summary>
        public DateTime StartDate { get; set; }
        /// <summary>
        /// EndDate
        /// </summary>
        public DateTime EndDate { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string TicketType { get; set; }
        /// <summary>
        /// IsSAPUser
        /// </summary>
        public int IsSAPUser { get; set; }
        /// <summary>
        /// IndustrySegment
        /// </summary>
        public string IndustrySegment { get; set; }
        /// <summary>
        /// UserId
        /// </summary>
        public string UserId { get; set; }
        /// <summary>
        /// BatchId
        /// </summary>
        public int BatchID { get; set; }
        /// <summary>
        /// InsightBy
        /// </summary>
        public string InsightBy { get; set; }
        /// <summary>
        /// OpportunityBy
        /// </summary>
        public string OpportunityBy { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int ProjectId
        {
            get;
            set;
        }
        /// <summary>
        /// CreatedByApp
        /// </summary>
        public string CreatedByApp { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string SupportedPlatform { get; set; }
    }
    /// <summary>
    /// 
    /// </summary>
    public class DeliveryConstructVersion
    {
        /// <summary>
        /// VersionId
        /// </summary>
        public int VersionId { get; set; }
        /// <summary>
        /// DeliveryConstructUID
        /// </summary>
        public string DeliveryConstructUID { get; set; }
        /// <summary>
        /// DeliveryConstructType
        /// </summary>
        public string DeliveryConstructType { get; set; }
    }
    /// <summary>
    /// 
    /// </summary>
    public class HistoricalTicketData
    {
        /// <summary>
        /// 
        /// </summary>
        public string IncidentNumber { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string AssigneeLoginId { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string TicketDescription { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public DateTime? ReportedDateTime { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public DateTime? LastModifiedTime { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Priority { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Status { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string SupportGroupName { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Company { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string ResolutionDescription { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string ReportedSource { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string SLAResolution { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public DateTime? ClosedDate { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string TicketType { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Source { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Subproject { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string DeliveryCenter { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string TicketCategory { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int? Effort { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string DeliveryConstructUId { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string EndToEndUId { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string ClientUId { get; set; }
        ///// <summary>
        ///// 
        ///// </summary>
        //public string EntityUId { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string ServiceType { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Category { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string ApplicationName { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int DMSProjectID { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string ProjectName { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string LOBName { get; set; }
    }
    /// <summary>
    /// 
    /// </summary>
    public class DeliveryStructureAttribute
    {
        /// <summary>
        /// DeliveryConstruct
        /// </summary>
        public string DeliveryConstruct { get; set; }
        /// <summary>
        /// DeliveryConstructUId
        /// </summary>
        public string DeliveryConstructUId { get; set; }
        /// <summary>
        /// DeliveryConstructType 
        /// </summary>
        public string DeliveryConstructType { get; set; }
        /// <summary>
        /// ParentDeliveryConstructType
        /// </summary>
        public string ParentDeliveryConstructType { get; set; }
        /// <summary>
        /// ParentDeliveryConstructUId
        /// </summary>
        public string ParentDeliveryConstructUId { get; set; }
        /// <summary>
        /// Status
        /// </summary>
        public string Status { get; set; }
        /// <summary>
        /// Level
        /// </summary>
        public string Level { get; set; }
    }

    public class WorkItemsList
    {
        public List<WorkItem> WorkItems { get; set; }
    }

    public class TaskList
    {
        public List<Tasks> Tasks { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class Tasks
    {
        #region Members
        private string _serviceType;
        #endregion

        #region Properties

        public List<DeliveryConstruct> DeliveryConstruct { get; set; }
        public string ClientUId { get; set; }
        public string EndToEndUId { get; set; }
        public string ToolInstanceUId { get; set; }
        public string EntityUId { get; set; }
        public string TaskId { get; set; }
        public string IncidentNumber { get; set; }
        public string AssigneeLoginId { get; set; }
        public string ExternalAssigneeLoginId { get; set; }
        public string ServiceType { get; set; }
        public string NormalisedTicketType { get; set; }
        public string TicketDescription { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public long XsltAssignedDateTime { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? AssignedDateTime { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public long XsltReportedDateTime { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? ReportedDateTime { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public long XsltLastModifiedTime { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? LastModifiedTime { get; set; }
        public string Priority { get; set; }
        public string Status { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public long XsltDueDate { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
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
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public decimal EstimatedDaystoResolution { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int BusinessProcessId { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int BusinessProcessStepId { get; set; }
        public string ResolutionMechanism { get; set; }
        public string DeepLink { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public double Holdtime { get; set; }
        public string ResolutionType { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public long XsltRespondedDate { get; set; }
        //[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        // public DateTime? RespondedDate { get; set; }
        public string RespondedDate { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public long XsltLastResolvedDate { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? LastResolvedDate { get; set; }
        public string StatusReason { get; set; }
        public string SLAResponse { get; set; }
        public string SLAResolution { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public long XsltReopenedDate { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? ReopenedDate { get; set; }
        public string SupportOrgName { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public long XsltResponseDate { get; set; }
        //[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? ResponseDate { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public long XsltResolutionDate { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? ResolutionDate { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public long XsltResolutionDueDate { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? ResolutionDueDate { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public long XsltResponseDueDate { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? ResponseDueDate { get; set; }

        public string Issue { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public long XsltClosedDate { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? ClosedDate { get; set; }
        public string TicketCreator { get; set; }
        public string Source { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int TeamSize { get; set; }
        public string Subproject { get; set; }
        public string DeliveryCenter { get; set; }
        public string ExternalCreatedBy { get; set; }
        public string CreatedBy { get; set; }
        public string TicketCategory { get; set; }
        public string BaseInstanceUrl { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public double Effort { get; set; }
        public string Impact { get; set; }
        public string Urgency { get; set; }
        public string CorrelationId { get; set; }
        public string IncidentState { get; set; }
        public string CategoryPath { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int StatusId { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int PriorityId { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int StatusReasonId { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int SLAResponseId { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int ImpactId { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int UrgencyId { get; set; }
        public string ClosureCode { get; set; }
        public string SLABreachReason { get; set; }
        public string RelatedIncidents { get; set; }
        public string RelatedProblems { get; set; }
        public string RelatedRequests { get; set; }
        public string RelatedEvents { get; set; }
        public string RelatedChanges { get; set; }
        public string RelatedTasks { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int ReopenCount { get; set; }
        public string Outage { get; set; }
        public string ApplicationName { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public long XsltCreatedOn { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? CreatedOn { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public long XsltModifiedOn { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? ModifiedOn { get; set; }
        public string TicketExtensions { get; set; }
        public string DeliveryType { get; set; }
        public string EntityType { get; set; }
        public string Technology { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public decimal TATResponse { get; set; } = 0;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public decimal TATResolution { get; set; } = 0;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int flag { get; set; } = 0;
        public bool IsEntityUpdated { get; set; } = false;
        public string VDSSource { get; set; }
        public string ID { get; set; }
        public string MYWStatus { get; set; }
        public string MYWImpact { get; set; }
        public string MYWUrgency { get; set; }
        public string MYWPriority { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string XsltMYWPriority { get; set; }

        //added for IO
        public string Domain { get; set; }

        public string NotificationMethod { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? StartOn { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string CRType { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? ActualStartOn { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public long XsltActualStartOn { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? ActualEndOn { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public long XsltActualEndOn { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? PlannedStartOn { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public long XsltPlannedStartOn { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? PlannedEndOn { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public long XsltPlannedEndOn { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Reason { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool Expedited { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string BusinessJustification { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]

        public string WorkNotes { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]

        public string Reference { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]

        public string OpenedBy { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]

        public string Risk { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]

        public int? OutageDuration { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]

        public bool MajorIncident { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]

        public string RequestorCountry { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]

        public string RequestorCity { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]

        public string ConfigurationItemStatus { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]

        public string ConfigurationItemLocation { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]

        public bool FCR { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]

        public string ClosedBy { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]

        public string ResolvedBy { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]

        public string Knowledge { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]

        public DateTime? RCICompleteOn { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]

        public DateTime? RCIConfirmationOn { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string ProblemSource { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Owner { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? HourlyResolutionBucket { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string HourClassification { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? ReportedHourBin { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string FourHourlyClassification { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? AgeingBucketInDays { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Ageing { get; set; }

        public int? SLAResolutionId { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string RequestorLocation { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string ResponseSLAExclusion { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string ResolutionSLAExclusion { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]

        public string ExternalCallerId { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string LineofBusiness { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string ApprovalStatus { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? ApprovedDate { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public long XsltApprovedDate { get; set; }
        public DateTime? StatusChangeOn { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Ticket" /> class.
        /// </summary>
        /// <remarks>
        /// Ticket Default Constructor.
        /// </remarks>
        /// 
        public Tasks()
        {
        }

        #endregion

        #region Methods

        #endregion
    }
    public class TestResultList
    {
        public List<TestResults> TestResults { get; set; }
    }
    public class DeploymentList
    {
        public List<Deployment> Deployment { get; set; }
    }

    public class IterationsList
    {
        public List<Iteration> Iterations { get; set; }
    }
    public class WorkItem
    {
        public string ClientUId { get; set; }
        public string WorkItemUId { get; set; }
        public string WorkItemTypeUId { get; set; }
        public string WorkItemExternalId { get; set; }
        public string WorkItemExternalUrl { get; set; }
        public string CreatedAtSourceByUser { get; set; }
        public string ModifiedAtSourceByUser { get; set; }
        public DateTime? CreatedAtSourceOn { get; set; }
        public DateTime? ModifiedAtSourceOn { get; set; }
        public List<DeliveryConstruct> DeliveryConstructs { get; set; }
        public List<WorkItemAssociation> WorkItemAssociations { get; set; }
        public string Source { get; set; }
        public string Summary { get; set; }
        public string Description { get; set; }
        public string AcceptanceCriteria { get; set; }
        public string StoryPoints { get; set; }
        public string SourcePriority { get; set; }
        public string SourceStatus { get; set; }
        public string SourcePriorityUId { get; set; }
        public string SourceStatusUId { get; set; }
        public string SourceOwner { get; set; }
        public string SourceTeam { get; set; }
        public string BusinessValue { get; set; }
        public string SourceRelease { get; set; }
        public string SourceIteration { get; set; }
        public string Type { get; set; }
        public string TypeUId { get; set; }
        public string ReleaseUId { get; set; }
        public string IterationUId { get; set; }
        public string PlannedStartDate { get; set; }
        public string PlannedFinishDate { get; set; }
        public string ActualFinishDate { get; set; }
        public string ActualEffort { get; set; }
        public string PlannedEffort { get; set; }
        public string RemainingEffort { get; set; }
        public string SourceSeverity { get; set; }
        public string SourceSeverityUId { get; set; }
        public DateTime? VDSCreatedOn { get; set; }
        public DateTime? VDSModifiedOn { get; set; }
        public string AssignedAtSourceToUser { get; set; }
        public string StackRank { get; set; }
        public string RequirementId { get; set; }
        public int DefectId { get; set; }
        public string IterationId { get; set; }
    }

    public class Iteration
    {
        public string ClientUId { get; set; }
        public string IterationId { get; set; }
        public string IterationExternalId { get; set; }
        public string IterationExternalUrl { get; set; }
        public string CreatedAtSourceByUser { get; set; }
        public string ModifiedAtSourceByUser { get; set; }
        public DateTime? CreatedAtSourceOn { get; set; }
        public DateTime? ModifiedAtSourceOn { get; set; }
        public string IterationTypeUId { get; set; }
        public string IterationType { get; set; }
        public string Source { get; set; }
        public string IterationName { get; set; }
        public string Description { get; set; }
        public DateTime? PlannedStartDate { get; set; }
        public DateTime? PlannedFinishDate { get; set; }
        public List<DeliveryConstruct> DeliveryConstructs { get; set; }
        public DateTime? VDSCreatedOn { get; set; }
        public DateTime? VDSModifiedOn { get; set; }
        public string MethodologyUId { get; set; }
    }
    //public class DeliveryConstruct
    //{
    //    public string DeliveryConstructUId { get; set; }
    //}
    public class WorkItemAssociation
    {
        public string AssociationTypeUId { get; set; }
        public string WorkItemUId { get; set; }
        public string WorkItemTypeUId { get; set; }
        public string ItemExternalId { get; set; }
    }

    //public class TaskList
    //{
    //    public List<System.Threading.Tasks> Tasks { get; set; }
    //}
    public class TicketList
    {
        public List<Ticket> Tickets { get; set; }
    }
    public class Ticket
    {
        #region Members
        //private string _serviceType;
        #endregion

        //#region Properties

        ////public List<DeliveryConstruct> DeliveryConstruct { get; set; }
        ////public string ClientUId { get; set; }
        ////public string EndToEndUId { get; set; }
        ////public string ToolInstanceUId { get; set; }
        ////public string EntityUId { get; set; }
        ////public string IncidentNumber { get; set; }
        ////public string AssigneeLoginId { get; set; }
        ////public string ExternalAssigneeLoginId { get; set; }
        ////public string ServiceType { get; set; }
        ////public string NormalisedTicketType { get; set; }
        ////public string TicketDescription { get; set; }
        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        ////public DateTime AssignedDateTime { get; set; }
        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        ////public DateTime ReportedDateTime { get; set; }
        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        ////public DateTime LastModifiedTime { get; set; }
        ////public string Priority { get; set; }
        ////public string Status { get; set; }
        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        ////public DateTime DueDate { get; set; }
        ////public string SupportGroupName { get; set; }
        ////public string Company { get; set; }
        ////public string ResolutionDescription { get; set; }
        ////public string Category { get; set; }
        ////public string ReportedSource { get; set; }
        ////public string LongDescription { get; set; }
        ////public string CreatedByUser { get; set; }
        ////public string CreatedByApp { get; set; }
        ////public string ModifiedByApp { get; set; }
        ////public string ConfigurationItem { get; set; }
        ////public string ConfigurationItemClass { get; set; }
        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        ////public decimal EstimatedDaystoResolution { get; set; }
        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        ////public int BusinessProcessId { get; set; }
        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        ////public int BusinessProcessStepId { get; set; }
        ////public string ResolutionMechanism { get; set; }
        ////public string DeepLink { get; set; }
        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        ////public double Holdtime { get; set; }
        ////public string ResolutionType { get; set; }
        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        ////public DateTime RespondedDate { get; set; }
        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        ////public DateTime LastResolvedDate { get; set; }
        ////public string StatusReason { get; set; }
        ////public string SLAResponse { get; set; }
        ////public string SLAResolution { get; set; }
        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        ////public DateTime ReopenedDate { get; set; }
        ////public string SupportOrgName { get; set; }
        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        ////public DateTime ResponseDate { get; set; }
        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        ////public DateTime ResolutionDate { get; set; }
        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        ////public DateTime ResolutionDueDate { get; set; }
        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        ////public DateTime ResponseDueDate { get; set; }
        ////public string Issue { get; set; }
        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        ////public DateTime ClosedDate { get; set; }
        ////public string TicketCreator { get; set; }
        ////public string Source { get; set; }
        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        ////public int TeamSize { get; set; }
        ////public string Subproject { get; set; }
        ////public string DeliveryCenter { get; set; }
        ////public string ExternalCreatedBy { get; set; }
        ////public string CreatedBy { get; set; }
        ////public string TicketCategory { get; set; }
        ////public string BaseInstanceUrl { get; set; }
        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        ////public double Effort { get; set; }
        ////public string Impact { get; set; }
        ////public string Urgency { get; set; }
        ////public string CorrelationId { get; set; }
        ////public string IncidentState { get; set; }
        ////public string CategoryPath { get; set; }
        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        ////public int StatusId { get; set; }
        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        ////public int PriorityId { get; set; }
        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        ////public int StatusReasonId { get; set; }
        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        ////public int SLAResponseId { get; set; }
        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        ////public int ImpactId { get; set; }
        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        ////public int UrgencyId { get; set; }
        ////public string ClosureCode { get; set; }
        ////public string SLABreachReason { get; set; }
        ////public string RelatedIncidents { get; set; }
        ////public string RelatedProblems { get; set; }
        ////public string RelatedRequests { get; set; }
        ////public string RelatedEvents { get; set; }
        ////public string RelatedChanges { get; set; }
        ////public string RelatedTasks { get; set; }
        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        ////public int ReopenCount { get; set; }
        ////public string Outage { get; set; }
        ////public string ApplicationName { get; set; }
        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        ////public DateTime CreatedOn { get; set; }
        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        ////public DateTime ModifiedOn { get; set; }
        ////public string TicketExtensions { get; set; }
        ////public string DeliveryType { get; set; }
        ////public string EntityType { get; set; }
        ////public string Technology { get; set; }
        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        ////public decimal TATResponse { get; set; } = 0;
        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        ////public decimal TATResolution { get; set; } = 0;
        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        ////public int flag { get; set; } = 0;
        ////public bool IsEntityUpdated { get; set; } = false;
        ////public string VDSSource { get; set; }
        ////public string ID { get; set; }

        ////public string MYWStatus { get; set; }
        ////public string MYWImpact { get; set; }
        ////public string MYWUrgency { get; set; }
        ////public string MYWPriority { get; set; }
        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        ////public string XsltMYWPriority { get; set; }

        //////added for IO
        ////public string Domain { get; set; }

        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        ////public DateTime? StartOn { get; set; }

        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        ////public string CRType { get; set; }

        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        ////public DateTime? ActualStartOn { get; set; }

        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        ////public DateTime? ActualEndOn { get; set; }

        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        ////public DateTime? PlannedStartOn { get; set; }

        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        ////public DateTime? PlannedEndOn { get; set; }

        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        ////public string Reason { get; set; }

        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        ////public bool Expedited { get; set; }
        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]

        ////public string BusinessJustification { get; set; }
        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]

        ////public string WorkNotes { get; set; }
        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]

        ////public string Reference { get; set; }
        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]

        ////public string OpenedBy { get; set; }
        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]

        ////public string Risk { get; set; }
        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]

        ////public int? OutageDuration { get; set; }
        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]

        ////public bool MajorIncident { get; set; }
        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]

        ////public string RequestorCountry { get; set; }
        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]

        ////public string RequestorCity { get; set; }
        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]

        ////public string ConfigurationItemStatus { get; set; }
        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]

        ////public string ConfigurationItemLocation { get; set; }
        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]

        ////public bool FCR { get; set; }
        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]

        ////public string ClosedBy { get; set; }
        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]

        ////public string ResolvedBy { get; set; }
        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]

        ////public string Knowledge { get; set; }
        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]

        ////public DateTime? RCICompleteOn { get; set; }
        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]

        ////public DateTime? RCIConfirmationOn { get; set; }
        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        ////public string ProblemSource { get; set; }
        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        ////public string Owner { get; set; }
        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        ////public int? HourlyResolutionBucket { get; set; }
        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        ////public string HourClassification { get; set; }
        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        ////public int? ReportedHourBin { get; set; }
        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        ////public string FourHourlyClassification { get; set; }
        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        ////public int? AgeingBucketInDays { get; set; }
        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        ////public string Ageing { get; set; }

        ////public int? SLAResolutionId { get; set; }
        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        ////public string RequestorLocation { get; set; }
        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        ////public string ResponseSLAExclusion { get; set; }
        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        ////public string ResolutionSLAExclusion { get; set; }
        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]

        ////public string ExternalCallerId { get; set; }
        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        ////public string LineofBusiness { get; set; }
        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        ////public string ApprovalStatus { get; set; }

        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        ////public DateTime? ApprovedDate { get; set; }
        ////public DateTime? StatusChangeOn { get; set; }

        ////[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        ////public int? ReassignmentCount { get; set; }




        //#endregion

        #region Properties

        public List<WINSERVICEMODELS.DeliveryConstruct> DeliveryConstruct { get; set; }
        public string ClientUId { get; set; }
        public string EndToEndUId { get; set; }
        public string ToolInstanceUId { get; set; }
        public string EntityUId { get; set; }
        public string TaskId { get; set; }
        public string IncidentNumber { get; set; }
        public string AssigneeLoginId { get; set; }
        public string ExternalAssigneeLoginId { get; set; }
        public string ServiceType { get; set; }
        public string NormalisedTicketType { get; set; }
        public string TicketDescription { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public long XsltAssignedDateTime { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? AssignedDateTime { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public long XsltReportedDateTime { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? ReportedDateTime { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public long XsltLastModifiedTime { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? LastModifiedTime { get; set; }
        public string Priority { get; set; }
        public string Status { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public long XsltDueDate { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
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
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public decimal EstimatedDaystoResolution { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int BusinessProcessId { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int BusinessProcessStepId { get; set; }
        public string ResolutionMechanism { get; set; }
        public string DeepLink { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public double Holdtime { get; set; }
        public string ResolutionType { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public long XsltRespondedDate { get; set; }
        //[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        // public DateTime? RespondedDate { get; set; }
        public string RespondedDate { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public long XsltLastResolvedDate { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? LastResolvedDate { get; set; }
        public string StatusReason { get; set; }
        public string SLAResponse { get; set; }
        public string SLAResolution { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public long XsltReopenedDate { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? ReopenedDate { get; set; }
        public string SupportOrgName { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public long XsltResponseDate { get; set; }
        //[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? ResponseDate { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public long XsltResolutionDate { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? ResolutionDate { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public long XsltResolutionDueDate { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? ResolutionDueDate { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public long XsltResponseDueDate { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? ResponseDueDate { get; set; }

        public string Issue { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public long XsltClosedDate { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? ClosedDate { get; set; }
        public string TicketCreator { get; set; }
        public string Source { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int TeamSize { get; set; }
        public string Subproject { get; set; }
        public string DeliveryCenter { get; set; }
        public string ExternalCreatedBy { get; set; }
        public string CreatedBy { get; set; }
        public string TicketCategory { get; set; }
        public string BaseInstanceUrl { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public double Effort { get; set; }
        public string Impact { get; set; }
        public string Urgency { get; set; }
        public string CorrelationId { get; set; }
        public string IncidentState { get; set; }
        public string CategoryPath { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int StatusId { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int PriorityId { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int StatusReasonId { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int SLAResponseId { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int ImpactId { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int UrgencyId { get; set; }
        public string ClosureCode { get; set; }
        public string SLABreachReason { get; set; }
        public string RelatedIncidents { get; set; }
        public string RelatedProblems { get; set; }
        public string RelatedRequests { get; set; }
        public string RelatedEvents { get; set; }
        public string RelatedChanges { get; set; }
        public string RelatedTasks { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int ReopenCount { get; set; }
        public string Outage { get; set; }
        public string ApplicationName { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public long XsltCreatedOn { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? CreatedOn { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public long XsltModifiedOn { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? ModifiedOn { get; set; }
        public string TicketExtensions { get; set; }
        public string DeliveryType { get; set; }
        public string EntityType { get; set; }
        public string Technology { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public decimal TATResponse { get; set; } = 0;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public decimal TATResolution { get; set; } = 0;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int flag { get; set; } = 0;
        public bool IsEntityUpdated { get; set; } = false;
        public string VDSSource { get; set; }
        public string ID { get; set; }
        public string MYWStatus { get; set; }
        public string MYWImpact { get; set; }
        public string MYWUrgency { get; set; }
        public string MYWPriority { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string XsltMYWPriority { get; set; }

        //added for IO
        public string Domain { get; set; }

        public string NotificationMethod { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? StartOn { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string CRType { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? ActualStartOn { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public long XsltActualStartOn { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? ActualEndOn { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public long XsltActualEndOn { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? PlannedStartOn { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public long XsltPlannedStartOn { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? PlannedEndOn { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public long XsltPlannedEndOn { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Reason { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool Expedited { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string BusinessJustification { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]

        public string WorkNotes { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]

        public string Reference { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]

        public string OpenedBy { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]

        public string Risk { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]

        public int? OutageDuration { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]

        public bool MajorIncident { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]

        public string RequestorCountry { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]

        public string RequestorCity { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]

        public string ConfigurationItemStatus { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]

        public string ConfigurationItemLocation { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]

        public bool FCR { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]

        public string ClosedBy { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]

        public string ResolvedBy { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]

        public string Knowledge { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]

        public DateTime? RCICompleteOn { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]

        public DateTime? RCIConfirmationOn { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string ProblemSource { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Owner { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? HourlyResolutionBucket { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string HourClassification { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? ReportedHourBin { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string FourHourlyClassification { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? AgeingBucketInDays { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Ageing { get; set; }

        public int? SLAResolutionId { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string RequestorLocation { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string ResponseSLAExclusion { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string ResolutionSLAExclusion { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]

        public string ExternalCallerId { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string LineofBusiness { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string ApprovalStatus { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? ApprovedDate { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public long XsltApprovedDate { get; set; }
        public DateTime? StatusChangeOn { get; set; }
        public int? ReassignmentCount { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Ticket" /> class.
        /// </summary>
        /// <remarks>
        /// Ticket Default Constructor.
        /// </remarks>
        /// 
        public Ticket()
        {
        }

        #endregion

        #region Methods

        #endregion
    }
    public class CodeCommitList
    {
        public List<CodeCommit> CodeCommits { get; set; }
    }
    public class CodeCommit
    {
        public int CodeCommitId { get; set; }
        public string CodeCommitUId { get; set; }
        public string CodeCommitExternalId { get; set; }
        public string CreatedByProductInstanceUId { get; set; }
        public string ClientUId { get; set; }
        public string CodeCommitExternalUrl { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Details { get; set; }
        public string StateUId { get; set; }
        public string ReleaseUId { get; set; }
        public List<DevOpsEntitiesAssociation> CodeCommitAssociations { get; set; }
        public List<DeliveryConstruct> CodeCommitDeliveryConstructs { get; set; }
        public string ProductInstanceUId { get; set; }
        public string CreatedByUser { get; set; }
        public string CreatedAtSourceByUser { get; set; }
        public string ModifiedByUser { get; set; }
        public string ModifiedAtSourceByUser { get; set; }
        public string CreatedOn { get; set; }
        public string CreatedAtSourceOn { get; set; }
        public string ModifiedOn { get; set; }
        public string ModifiedAtSourceOn { get; set; }
        public string CreatedByApp { get; set; }
        public string ModifiedByApp { get; set; }
        public string CodeCommitTypeUId { get; set; }
        public int ActualDuration { get; set; }
        public int CodeCommitCount { get; set; }
        public string VersionControlType { get; set; }
        public int CognitiveComplexity { get; set; }
        public int Complexity { get; set; }
        public int DuplicatedLineCount { get; set; }
        public int CodeSmellCount { get; set; }
        public string SQALERatingPercentage { get; set; }
        public int SQALEIndex { get; set; }
        public int SQALEDebtRatio { get; set; }
        public int BugCount { get; set; }
        public int ReliabilityRemediationEffort { get; set; }
        public int VulnerabilityCount { get; set; }
        public string SecurityRatingUId { get; set; }
        public int SecurityRemediationEffort { get; set; }
        public int ClassCount { get; set; }
        public int NonCommentedLinesOfCode { get; set; }
        public int FunctionCount { get; set; }
        public int Coverage { get; set; }
        public int LineCoveragePercentage { get; set; }
        public int UnCoveredConditionCount { get; set; }
        public int TestCount { get; set; }
        public int TestExecutionTime { get; set; }
        public int TestErrorCount { get; set; }
        public int TestSuccessDensity { get; set; }
        public int ViolationCount { get; set; }
        public int OpenIssueCount { get; set; }
        public int BlockerIssueCount { get; set; }
        public int CriticalIssueCount { get; set; }
        public int MajorIssueCount { get; set; }
        public int MinorIssueCount { get; set; }
        public string ToolInstanceUrl { get; set; }
        public string VDSSource { get; set; }
        public DateTime VDSCreatedOn { get; set; }
        public DateTime VDSModifiedOn { get; set; }
    }
    public class DevOpsEntitiesAssociation
    {
        public string ItemExternalId { get; set; }
        public string EntityUId { get; set; }
        public string AssociationTypeUId { get; set; }
    }
    public class CodeBranchList
    {
        public List<CodeBranch> CodeBranches { get; set; }
    }
    public class CodeBranch
    {
        public int CodeBranchId { get; set; }
        public string CodeBranchUId { get; set; }
        public string CodeBranchExternalId { get; set; }
        public string CreatedByProductInstanceUId { get; set; }
        public string ClientUId { get; set; }
        public string CodeBranchExternalUrl { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Details { get; set; }
        public string StateUId { get; set; }
        public List<DevOpsEntitiesAssociation> CodeBranchAssociations { get; set; }
        public List<DeliveryConstruct> CodeBranchDeliveryConstructs { get; set; }
        public string ProductInstanceUId { get; set; }
        public string CreatedByUser { get; set; }
        public string CreatedAtSourceByUser { get; set; }
        public string ModifiedByUser { get; set; }
        public string ModifiedAtSourceByUser { get; set; }
        public string CreatedOn { get; set; }
        public string CreatedAtSourceOn { get; set; }
        public string ModifiedOn { get; set; }
        public string ModifiedAtSourceOn { get; set; }
        public string CreatedByApp { get; set; }
        public string ModifiedByApp { get; set; }
        public string CodeBranchTypeUId { get; set; }
        public int ActualDuration { get; set; }
        public int CognitiveComplexity { get; set; }
        public int Complexity { get; set; }
        public int DuplicatedLineCount { get; set; }
        public int CodeSmellCount { get; set; }
        public string SQALERatingPercentage { get; set; }
        public int SQALEIndex { get; set; }
        public int SQALEDebtRatio { get; set; }
        public int BugCount { get; set; }
        public int ReliabilityRemediationEffort { get; set; }
        public int VulnerabilityCount { get; set; }
        public string SecurityRatingUId { get; set; }
        public int SecurityRemediationEffort { get; set; }
        public int ClassCount { get; set; }
        public int NonCommentedLinesOfCode { get; set; }
        public int FunctionCount { get; set; }
        public int Coverage { get; set; }
        public int LineCoveragePercentage { get; set; }
        public int UnCoveredConditionCount { get; set; }
        public int TestCount { get; set; }
        public int TestExecutionTime { get; set; }
        public int TestErrorCount { get; set; }
        public int TestSuccessDensity { get; set; }
        public int ViolationCount { get; set; }
        public int OpenIssueCount { get; set; }
        public int BlockerIssueCount { get; set; }
        public int CriticalIssueCount { get; set; }
        public int MajorIssueCount { get; set; }
        public int MinorIssueCount { get; set; }
        public string VDSSource { get; set; }
        public DateTime VDSCreatedOn { get; set; }
        public DateTime VDSModifiedOn { get; set; }
    }
    public class BuildList
    {
        public List<Build> Builds { get; set; }
    }
    public class Build
    {
        public int BuildId { get; set; }
        public string BuildUId { get; set; }
        public string BuildExternalId { get; set; }
        public string CreatedByProductInstanceUId { get; set; }
        public string ClientUId { get; set; }
        public string BuildExternalUrl { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Details { get; set; }
        public string StateUId { get; set; }
        public List<DevOpsEntitiesAssociation> BuildAssociations { get; set; }
        public List<DeliveryConstruct> BuildDeliveryConstructs { get; set; }
        public string ProductInstanceUId { get; set; }
        public string CreatedByUser { get; set; }
        public string CreatedAtSourceByUser { get; set; }
        public string ModifiedByUser { get; set; }
        public string ModifiedAtSourceByUser { get; set; }
        public string CreatedOn { get; set; }
        public string CreatedAtSourceOn { get; set; }
        public string ModifiedOn { get; set; }
        public string ModifiedAtSourceOn { get; set; }
        public string CreatedByApp { get; set; }
        public string ModifiedByApp { get; set; }
        public string BuildTypeUId { get; set; }
        public int ActualDuration { get; set; }
        public int CognitiveComplexity { get; set; }
        public int Complexity { get; set; }
        public int DuplicatedLineCount { get; set; }
        public int CodeSmellCount { get; set; }
        public string SQALERatingPercentage { get; set; }
        public int SQALEIndex { get; set; }
        public int SQALEDebtRatio { get; set; }
        public int BugCount { get; set; }
        public int ReliabilityRemediationEffort { get; set; }
        public int VulnerabilityCount { get; set; }
        public string SecurityRatingUId { get; set; }
        public int SecurityRemediationEffort { get; set; }
        public int ClassCount { get; set; }
        public int NonCommentedLinesOfCode { get; set; }
        public int FunctionCount { get; set; }
        public int Coverage { get; set; }
        public int LineCoveragePercentage { get; set; }
        public int UnCoveredConditionCount { get; set; }
        public int TestCount { get; set; }
        public int TestExecutionTime { get; set; }
        public int TestErrorCount { get; set; }
        public int TestSuccessDensity { get; set; }
        public int ViolationCount { get; set; }
        public int OpenIssueCount { get; set; }
        public int BlockerIssueCount { get; set; }
        public int CriticalIssueCount { get; set; }
        public int MajorIssueCount { get; set; }
        public int MinorIssueCount { get; set; }
        public string JobName { get; set; }
        public int SuccessRatePercentage { get; set; }
        public string Environment { get; set; }
        public int FailureRatePercentage { get; set; }
        public int MeanTimeBetweenFailures { get; set; }
        public int MeanTimeToRecover { get; set; }
        public string BuildError { get; set; }
        public string VDSSource { get; set; }
        public DateTime VDSCreatedOn { get; set; }
        public DateTime VDSModifiedOn { get; set; }
    }

    public class TestResults
    {
        public string ClientUId { get; set; }
        public int TestResultId { get; set; }

        public string TestResultUId { get; set; }
        public string TestResultExternalId { get; set; }
        public string CreatedByProductInstanceUId { get; set; }
        public string TestResultExternalUrl { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string StateUId { get; set; }
        public string StateExternalValue { get; set; }
        public string StateExternalId { get; set; }

        public List<TestResultAssociations> TestResultAssociations { get; set; }

        public List<TestResultDeliveryConstructs> TestResultDeliveryConstructs { get; set; }
        public string CreatedAtSourceByUser { get; set; }
        public string ModifiedAtSourceByUser { get; set; }
        public string CreatedOn { get; set; }
        public string CreatedAtSourceOn { get; set; }
        public string ModifiedOn { get; set; }
        public string ModifiedAtSourceOn { get; set; }
        public string CreatedByApp { get; set; }
        public string ModifiedByApp { get; set; }
        public int TotalFunctionalTest { get; set; }
        public int FunctionalTestFailed { get; set; }
        public int FunctionalTestPassed { get; set; }
        public int FunctionalTestDuration { get; set; }
        public int TotalNonFunctionalTest { get; set; }
        public int NonFunctionalTestFailed { get; set; }
        public int NonFunctionalTestPassed { get; set; }
        public int NonFunctionalTestDuration { get; set; }
        public int PerformanceTestFailed { get; set; }
        public int PerformanceTestPassed { get; set; }
        public int TotalPerformanceTest { get; set; }
        public int PerformanceTestMean { get; set; }
        public int PerformanceTestResponseTime { get; set; }
        public int PerformanceTestStandardDeviation { get; set; }
        public int PerformanceTestMaxResponseTIme { get; set; }
        public int PerformanceTestMinResponseTIme { get; set; }
        public string Project { get; set; }
        public string VDSSource { get; set; }
        public DateTime VDSCreatedOn { get; set; }
        public DateTime VDSModifiedOn { get; set; }


    }

    public class TestResultAssociations
    {
        public string ItemExternalId { get; set; }
        public string EntityUId { get; set; }
        public string AssociationTypeUId { get; set; }
        public string ProductInstanceUId { get; set; }
    }

    public class TestResultDeliveryConstructs
    {

    }

    public class Deployment
    {
        public string ClientUId { get; set; }
        public int DeploymentId { get; set; }
        public string DeploymentUId { get; set; }
        public string DeploymentExternalId { get; set; }
        public string CreatedByProductInstanceUId { get; set; }
        public string DeploymentExternalUrl { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string StateUId { get; set; }
        public string StateExternalValue { get; set; }
        public string StateExternalId { get; set; }
        public string SeverityUId { get; set; }
        public string SeverityExternalValue { get; set; }
        public string SeverityIdExternalValue { get; set; }
        public List<DeploymentAssociations> DeploymentAssociations { get; set; }
        public List<DeploymentConstructs> DeploymentConstructs { get; set; }
        public List<DeploymentProductInstances> DeploymentProductInstances { get; set; }
        public string CreatedAtSourceByUser { get; set; }
        public string ModifiedAtSourceByUser { get; set; }
        public string CreatedOn { get; set; }
        public string CreatedAtSourceOn { get; set; }
        public string ModifiedOn { get; set; }
        public string ModifiedAtSourceOn { get; set; }
        public string CreatedByApp { get; set; }
        public string ModifiedByApp { get; set; }
        public int ActualDuration { get; set; }
        public int DeploymentFrequency { get; set; }
        public int ReleaseCycleTime { get; set; }
        public int DeploymentFailure { get; set; }
        public int DeploymentVolume { get; set; }
        public int CPUCores { get; set; }
        public int CPUIOWaitPercentage { get; set; }
        public int CPUTotalPercentage { get; set; }
        public int DiskIOReadCount { get; set; }
        public int DiskIOWriteCount { get; set; }
        public int FreeSpace { get; set; }
        public int TotalSpace { get; set; }
        public int LoadOverOneMinute { get; set; }
        public int LoadOverFiveMinutes { get; set; }
        public int LoadOverFifteenMinutes { get; set; }
        public int AverageLoadOverOneMinute { get; set; }
        public int AverageLoadOverFiveMinutes { get; set; }
        public int AverageLoadOverFifteenMinutes { get; set; }
        public int LoadCPUCores { get; set; }
        public int TotalMemory { get; set; }
        public int UsedMemoryInBytes { get; set; }
        public int FreeMemoryInBytes { get; set; }
        public int ActualUsedMemoryInBytes { get; set; }
        public int BytesSent { get; set; }
        public int BytesReceived { get; set; }
        public int UptimeDuration { get; set; }
        public int Vulnerabilities { get; set; }
        public int UnapprovedImages { get; set; }

        public int ReadBytes { get; set; }
        public int ReadOperationsPerSecond { get; set; }
        public int ReadRate { get; set; }
        public int WriteOperationsPerSecond { get; set; }
        public int WriteRate { get; set; }

        public string HealthCheckStatus { get; set; }
        public int ImageSize { get; set; }
        public int ContainersRunning { get; set; }
        public int ContainersStopped { get; set; }

        public int MemoryLimit { get; set; }
        public int FailedCount { get; set; }
        public int MaximumMemoryUsage { get; set; }
        public int MemoryUsagePercentage { get; set; }
        public int TotalMemoryUsage { get; set; }
        public int ContainerBytesReceived { get; set; }
        public int ContainerBytesDropped { get; set; }
        public int InboundPacketsReceived { get; set; }
        public int InboundPacketsDropped { get; set; }
        public int OutboundPacketsDropped { get; set; }
        public string NodeName { get; set; }
        public int NodeCapacity { get; set; }
        public int NodeTotalCapacity { get; set; }
        public string Namespace { get; set; }
        public int Image { get; set; }
        public string PhaseStatus { get; set; }
        public int Nanocores { get; set; }
        public int MemoryLimitInBytes { get; set; }
        public int MemoryRequestInBytes { get; set; }
        public string Project { get; set; }
        public string VDSSource { get; set; }
        public DateTime VDSCreatedOn { get; set; }
        public DateTime VDSModifiedOn { get; set; }
    }

    public class DeploymentAssociations
    {

    }

    public class DeploymentConstructs
    {

    }

    public class DeploymentProductInstances
    {

    }

}
