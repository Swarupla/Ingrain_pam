using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;


namespace Accenture.MyWizard.Ingrain.WindowService.Models
{  
    public class HistoricalTicketList
    {
        public List<PAMTicket> Tickets { get; set; }
        public List<PAMTask> TaskList { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class PAMTicket
    {
        #region Members
        // private string _serviceType;
        #endregion

        #region Properties
        [BsonId]
        public object _id { get; set; }
        public List<DeliveryConstruct> DeliveryConstruct { get; set; }
        public string ClientUId { get; set; }
        public string EndToEndUId { get; set; }
        public string ToolInstanceUId { get; set; }
        public string EntityUId { get; set; }
        public string IncidentNumber { get; set; }
        public string AssigneeLoginId { get; set; }
        public string ExternalAssigneeLoginId { get; set; }
        public string ServiceType { get; set; }
        public string NormalisedTicketType { get; set; }

        public string TicketDescription { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime AssignedDateTime { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime ReportedDateTime { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime LastModifiedTime { get; set; }
        public string Priority { get; set; }
        public string Status { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime DueDate { get; set; }
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
        public DateTime RespondedDate { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime LastResolvedDate { get; set; }
        public string StatusReason { get; set; }
        public string SLAResponse { get; set; }
        public string SLAResolution { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime ReopenedDate { get; set; }
        public string SupportOrgName { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime ResponseDate { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime ResolutionDate { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime ResolutionDueDate { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime ResponseDueDate { get; set; }
        public string Issue { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime ClosedDate { get; set; }
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
        public DateTime CreatedOn { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime ModifiedOn { get; set; }
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

        public string Domain { get; set; }
        public double EffortInHours { get; set; }
        public string MYWStatus { get; set; }
        public string MYWImpact { get; set; }
        public string MYWUrgency { get; set; }
        public string MYWPriority { get; set; }
        public string RequestorLocation { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Ticket" /> class.
        /// </summary>
        /// <remarks>
        /// Ticket Default Constructor.
        /// </remarks>
        /// 
        public PAMTicket()
        {
        }

        #endregion

        #region Methods

        #endregion
    }

    public class DeliveryConstruct
    {
        public string DeliveryConstructUId { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class PAMTask
    {
        #region Members
        // private string _serviceType;
        #endregion

        #region Properties
        [BsonId]
        public object _id { get; set; }
        public List<DeliveryConstruct> DeliveryConstruct { get; set; }
        public string ClientUId { get; set; }
        public string EndToEndUId { get; set; }
        public string ToolInstanceUId { get; set; }
        public string EntityUId { get; set; }
        [JsonProperty(PropertyName = "IncidentNumber")]
        public string TaskId { get; set; }
        public string AssigneeLoginId { get; set; }
        public string ExternalAssigneeLoginId { get; set; }
        public string ServiceType { get; set; }
        public string NormalisedTicketType { get; set; }
        public string RequestorLocation { get; set; }
        public string TicketDescription { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime AssignedDateTime { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime ReportedDateTime { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime LastModifiedTime { get; set; }
        public string Priority { get; set; }
        public string Status { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime DueDate { get; set; }
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
        public DateTime RespondedDate { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime LastResolvedDate { get; set; }
        public string StatusReason { get; set; }
        public string SLAResponse { get; set; }
        public string SLAResolution { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime ReopenedDate { get; set; }
        public string SupportOrgName { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime ResponseDate { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime ResolutionDate { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime ResolutionDueDate { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime ResponseDueDate { get; set; }
        public string Issue { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime ClosedDate { get; set; }
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
        public DateTime CreatedOn { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime ModifiedOn { get; set; }
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

        public string Domain { get; set; }
        public double EffortInHours { get; set; }
        public string MYWStatus { get; set; }
        public string MYWImpact { get; set; }
        public string MYWUrgency { get; set; }
        public string MYWPriority { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Ticket" /> class.
        /// </summary>
        /// <remarks>
        /// Ticket Default Constructor.
        /// </remarks>
        /// 
        public PAMTask()
        {
        }

        #endregion

        #region Methods

        #endregion
    }
}
