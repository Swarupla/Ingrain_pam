using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Accenture.MyWizard.Ingrain.DataModels.Models
{
    class PhoenixDTO
    {
    }

    public class DeliveryConstructResponse
    {
        public Int64 TotalPageCount { get; set; }
        public Int64 CurrentPage { get; set; }
        public Int64 BatchSize { get; set; }
        public List<DeliveryConstruct> DeliveryConstructs { get; set; }

    }
        public class DeliveryConstruct
    {
        public string AccessRoleUId { get; set; }
        public string AccessRoleName { get; set; }
        public List<DeliveryConstruct> DeliveryConstructs { get; set; }
        public string DecimalPlaces { get; set; }
        public string DeliveryConstructUId { get; set; }
        public long? DeliveryConstructId { get; set; }
        public string ClientUId { get; set; }
        public string DeliveryStructureTypeUId { get; set; }
        public string DeliveryConstructTypeUId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ImageBinary { get; set; }
        public string DeliveryConstructStatusUId { get; set; }
        public string RowStatusUId { get; set; }
        public string CorrelationUId { get; set; }
        public int? CorrelationBatchId { get; set; }
        public object RowVersion { get; set; }
        public object Client { get; set; }
        public object DeliveryStructureType { get; set; }
        public DeliveryConstructType DeliveryConstructType { get; set; }
        public object DeliveryConstructStatus { get; set; }
        public object RowStatus { get; set; }
        public object DeliveryConstructEntityRuleQueues { get; set; }
        public object AccountServiceListenerEntityEventProcessedMessages { get; set; }
        public object ServiceRequestDeliveryConstructs { get; set; }
        public object ProblemDeliveryConstructs { get; set; }
        public object AccountAccessRoles { get; set; }
        public object AccountStorageListenerEntityEventMessages { get; set; }
        public object AccountDashboards { get; set; }
        public object DeliveryStructures { get; set; }
        public object DeliveryConstructAssociateds { get; set; }
        public object AccountStorageListenerEntityEventProcessedMessages { get; set; }
        public object AccountEmailListenerEntityEventMessages { get; set; }
        public object TaskDeliveryConstructs { get; set; }
        public object AccountEmailListenerEntityEventProcessedMessages { get; set; }
        public object ProductInstanceClientDeliveryConstructs { get; set; }
        public object TeamAreas { get; set; }
        public object ProductInstanceClientDeliveryConstructEntityProperties { get; set; }
        public object ProductInstanceClientDeliveryConstructEntityPropertyValues { get; set; }
        public object AppServiceClientDeliveryConstructs { get; set; }
        public object AccountMobileListenerEntityEventMessages { get; set; }
        public object AccountMobileListenerEntityEventProcessedMessages { get; set; }
        public object Dashboards { get; set; }
        public object WorkItemDeliveryConstructs { get; set; }
        public object AccountObjectModelListenerEntityEventMessages { get; set; }
        public object IncidentDeliveryConstructs { get; set; }
        public object AccountObjectModelListenerEntityEventProcessedMessages { get; set; }
        public List<object> DeliveryConstructAttributes { get; set; }
        public object AccountPermissions { get; set; }
        public object IterationDeliveryConstructs { get; set; }
        public List<object> DeliveryConstructAttributeExtensions { get; set; }
        public object ResourceDeliveryConstructs { get; set; }
        public object DeliveryConstructEntityRules { get; set; }
        public object DeliveryConstructEntityRuleProcessedQueues { get; set; }
        public object AccountServiceListenerEntityEventMessages { get; set; }
        public string CreatedByUser { get; set; }
        public string CreatedByApp { get; set; }
        public DateTime CreatedOn { get; set; }
        public string ModifiedByUser { get; set; }
        public string ModifiedByApp { get; set; }
        public DateTime ModifiedOn { get; set; }
        public object UserUId { get; set; }
        public int? ItemState { get; set; }
    }

    public class Client
    {
        public string AccessRoleUId { get; set; }
        public object AccessRoleName { get; set; }
        public string ClientUId { get; set; }
        public long? ClientId { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public object ClientParentUId { get; set; }
        public object ClientStatusUId { get; set; }
        public string Description { get; set; }
        public object ImageBinary { get; set; }
        public string RowStatusUId { get; set; }
        public int? DisplayOrder { get; set; }
        public string CorrelationUId { get; set; }
        public object RowVersion { get; set; }
        public object ClientParent { get; set; }
        public object RowStatus { get; set; }
        public object DeliveryConstructEntityRuleQueues { get; set; }
        public object AccountServiceListenerEntityEventProcessedMessages { get; set; }
        public object Problems { get; set; }
        public object AccessRoles { get; set; }
        public object DeliveryConstructTypes { get; set; }
        public object AccountDefaultClients { get; set; }
        public object DeliveryConstructTypeAttributes { get; set; }
        public object AccountAccessRoles { get; set; }
        public object DeliveryConstructTypeAttributeValues { get; set; }
        public object AccountStorageListenerEntityEventMessages { get; set; }
        public object Tasks { get; set; }
        public object Products { get; set; }
        public object DeliveryConstructTypeStructures { get; set; }
        public object AccountDashboards { get; set; }
        public object DeliveryMethodologies { get; set; }
        public object DeliveryStructures { get; set; }
        public object AccountStorageListenerEntityEventProcessedMessages { get; set; }
        public object AccountEmailListenerEntityEventMessages { get; set; }
        public object DeliveryStructureTypes { get; set; }
        public object AccountEmailListenerEntityEventProcessedMessages { get; set; }
        public object ProductInstanceClientDeliveryConstructs { get; set; }
        public object TeamAreas { get; set; }
        public object ProductInstanceClientDeliveryConstructEntityProperties { get; set; }
        public object ProductInstanceClientDeliveryConstructEntityPropertyValues { get; set; }
        public object WorkItems { get; set; }
        public object AppServiceClientDeliveryConstructs { get; set; }
        public object AccountMobileListenerEntityEventMessages { get; set; }
        public object ClientParents { get; set; }
        public object WorkItemAttributes { get; set; }
        public object ClientEnvironmentConfigurationItems { get; set; }
        public object AccountMobileListenerEntityEventProcessedMessages { get; set; }
        public object Incidents { get; set; }
        public object Dashboards { get; set; }
        public object AccountObjectModelListenerEntityEventMessages { get; set; }
        public object WorkItemTypes { get; set; }
        public object WorkItemTypeAttributes { get; set; }
        public object AccountObjectModelListenerEntityEventProcessedMessages { get; set; }
        public object WorkItemTypeStructures { get; set; }
        public List<DeliveryConstruct> DeliveryConstructs { get; set; }
        public object DeliveryConstructAttributes { get; set; }
        public object AccountPermissions { get; set; }
        public object DeliveryConstructAttributeValues { get; set; }
        public object DeliveryConstructEntityRules { get; set; }
        public object DeliveryConstructEntityRuleProcessedQueues { get; set; }
        public object AccountServiceListenerEntityEventMessages { get; set; }
        public object ServiceRequests { get; set; }
        public object CreatedByUser { get; set; }
        public object CreatedByApp { get; set; }
        public DateTime? CreatedOn { get; set; }
        public object ModifiedByUser { get; set; }
        public object ModifiedByApp { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public object UserUId { get; set; }
        public int? ItemState { get; set; }
    }
    public class Clients
    {
        public string ClientUId { get; set; }
        public object ClientParentUId { get; set; }
        public object ClientStatusUId { get; set; }
        public string Description { get; set; }
        public object ClientParent { get; set; }
        public object DeliveryStructures { get; set; }
        public List<DeliveryConstruct> DeliveryConstructs { get; set; }
    }

    public class RootObject
    {
        public Client Client { get; set; }
        public object Faults { get; set; }
        public int StatusCode { get; set; }
        public object MergeResult { get; set; }
    }

    public class RootObjectOne
    {
        public List<Client> Clients { get; set; }

        public object Faults { get; set; }
        public int StatusCode { get; set; }
        public object MergeResult { get; set; }
    }
    public class DeliveryConstructType
    {
        public string DeliveryConstructTypeUId { get; set; }
        public long? DeliveryConstructTypeId { get; set; }
        public object DeliveryConstructTypeSystemUId { get; set; }
        public object ClientUId { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public bool IsSystemDeliveryConstructType { get; set; }
        public string Code { get; set; }
        public object DeliveryConstructTypeParentUId { get; set; }
        public string Description { get; set; }
        public object Family { get; set; }
        public object ImageUrl { get; set; }
        public string ImageBinary { get; set; }
        public int? HierarchyLevel { get; set; }
        public string DeliveryConstructTypeStatusUId { get; set; }
        public bool IsHierarchySupported { get; set; }
        public object DisplayOrder { get; set; }
        public string RowStatusUId { get; set; }
        public string CorrelationUId { get; set; }
        public int? CorrelationBatchId { get; set; }
        public object RowVersion { get; set; }
        public object Client { get; set; }
        public object DeliveryConstructTypeParent { get; set; }
        public object DeliveryConstructTypeStatus { get; set; }
        public object RowStatus { get; set; }
        public object DeliveryConstructTypeParents { get; set; }
        public object DeliveryConstructTypeAttributes { get; set; }
        public List<DeliveryConstructTypeStructure> DeliveryConstructTypeStructures { get; set; }
        public object DeliveryConstructTypeAssociateds { get; set; }
        public object DeliveryConstructs { get; set; }
        public object CreatedByUser { get; set; }
        public object CreatedByApp { get; set; }
        public DateTime? CreatedOn { get; set; }
        public object ModifiedByUser { get; set; }
        public object ModifiedByApp { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public object UserUId { get; set; }
        public int? ItemState { get; set; }
    }

    public class DeliveryConstructTypeStructure
    {
        public string DeliveryConstructTypeStructureUId { get; set; }
        public int? DeliveryConstructTypeStructureId { get; set; }
        public string Name { get; set; }
        public object ClientUId { get; set; }
        public string DeliveryStructureTypeUId { get; set; }
        public string DeliveryConstructTypeUId { get; set; }
        public string DeliveryConstructTypeAssociatedUId { get; set; }
        public string UIdHierarchy { get; set; }
        public int? HierarchyLevel { get; set; }
        public string RowStatusUId { get; set; }
        public string CorrelationUId { get; set; }
        public object RowVersion { get; set; }
        public object Client { get; set; }
        public object DeliveryStructureType { get; set; }
        public object DeliveryConstructType { get; set; }
        public object DeliveryConstructTypeAssociated { get; set; }
        public object RowStatus { get; set; }
        public object CreatedByUser { get; set; }
        public object CreatedByApp { get; set; }
        public DateTime CreatedOn { get; set; }
        public object ModifiedByUser { get; set; }
        public object ModifiedByApp { get; set; }
        public DateTime ModifiedOn { get; set; }
        public object UserUId { get; set; }
        public int? ItemState { get; set; }
    }

    public class DeliveryConstructTree
    {
        public string CleintUID;
        public string DeliveryConstructUId;
        public string ParentDeliveryConstructUId;
        public string DeliveryConstrcutName;
        public string DeliveryConstrcutType;
    }


    public class Node
    {

        public string DeliveryConstructUID { get; set; }
        public string DeliveryConstructType { get; set; }

        public string ParentDeliveryConstructUID { get; set; }
        public string StartDate { get; set; }
        public string Name { get; set; }

        public string AcessRole { get; set; }

        public string SelectedIndex { get; set; }
        public string Status { get; set; }

        public string ImageBinary { get; set; }

        public List<Node> Children { get; set; }
        public List<Ticket> Tickets { get; set; }

        public Node()
        {
            Children = new List<Node>();
            Tickets = new List<Ticket>();
        }
    }

    public class EndToEndNode
    {

        public string DeliveryConstructUID { get; set; }
        public string DeliveryConstructType { get; set; }

        public string Name { get; set; }
        public string SelectedIndex { get; set; }
        public List<Node> Children { get; set; }
        public List<Ticket> Tickets { get; set; }

        public EndToEndNode()
        {
            Children = new List<Node>();
            Tickets = new List<Ticket>();
        }
    }

    public class Ticket
    {
        public string TicketNum { get; set; }
        public System.DateTime ReportedDate { get; set; }
        public string SupportGroup { get; set; }
        public string TicketType { get; set; }
        public string Priority { get; set; }
        public string Status { get; set; }
    }
}
