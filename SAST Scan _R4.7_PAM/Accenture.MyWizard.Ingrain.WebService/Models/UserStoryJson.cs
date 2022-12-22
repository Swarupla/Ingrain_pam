using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Accenture.MyWizard.Ingrain.WebService.Models
{



    public class Estimate
        {
            public string ID { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
            public string Acceptance_Citeria { get; set; }
            public string priority { get; set; }
            public string priority_color { get; set; }
            public string Confdence_Score { get; set; }
            public string StoryPoints { get; set; }
            public string helptext { get; set; }
        }

        public class SPData
        {
            public string Criticality { get; set; }
            public string Color { get; set; }
            public int Count { get; set; }
        }

        public class donutChartData
        {
            public object SpValue { get; set; }
            public List<SPData> SPData { get; set; }
        }


    public class donutMain
    {
        public List<donutChartData> donutChartData { get; set; }
    }

    public class UserStoryProcess
    {
        public Guid ClientUID { get; set; }
        public Guid UserStoryUID { get; set; }

        public string DeliveryConstructUId { get; set; }
    }
    public class DefaultDeleveryConst
    {
        public string ClientUID { get; set; }
        public string DeliveryConstructUId { get; set; }

        public string userID { get; set; }
    }

    public class StoryProcess
    {
        public List<UserStoryProcess> UserStoryProcess { get; set; }
    }
    public class TempConfigurationDetail
    {
        public int? Config_ID { get; set; }
        public string ClientUID { get; set; }
        public string DeliveryConstructUId { get; set; }
        public int? ConfigStartValue { get; set; }
        public int? ConfigEndValue { get; set; }
        public string SPValue { get; set; }

    }

    


    public class ClientStruct
    {
        public string ClientUId { get; set; }
        public string Name { get; set; }
        public string DCUId { get; set; }
        public string ImageBinary { get; set; }


    }
    public class DefalutDeleveryStruct
    {
        public string ClientUId { get; set; }
        public string Name { get; set; }

        public string DCUId { get; set; }

    }

    public class DefalutClient
    {
        public List<DefalutDeleveryStruct> Clients { get; set; }

    }
    public class ClientStructure
    {
        public List<ClientStruct> Clients { get; set; }

    }

    public class StoryInfoIntoPhoenix
    {
        public Guid clientUID { get; set; }



        public Guid DeliveryConstructUId { get; set; }
        public string workExternalId { get; set; }

    }

    public class StoryInfo
    {
        public List<StoryInfoIntoPhoenix> StoryInfoIntoPhoenix { get; set; }

    }

    public class POSTUID
    {
        public Guid ClientUID { get; set; }

    }

    #region ENS

    public class ENSMessage
    {
        public string EntityEventMessageUId { get; set; }
        public string EntityEventUId { get; set; }
        public string EntityUId { get; set; }
        public string EntityEventMessageStatusUId { get; set; }
        public DateTime SentOn { get; set; }
        public object SenderUser { get; set; }
        public string SenderApp { get; set; }
        public string Message { get; set; }
        public object MessageDataFileLink { get; set; }
        public object MessageDataFileDataSourceType { get; set; }
        public string CallbackLink { get; set; }
        public object ErrorCallbackLink { get; set; }
        public object AssociatedItemId { get; set; }
        public object AssociatedItemUId { get; set; }
        public string ClientUId { get; set; }
        public object DeliveryConstructUId { get; set; }
        public object ProcessUId { get; set; }
        public string RowStatusUId { get; set; }
        public object CreatedByUser { get; set; }
        public string CreatedByApp { get; set; }
        public DateTime CreatedOn { get; set; }
        public object ModifiedByUser { get; set; }
        public string ModifiedByApp { get; set; }
        public DateTime ModifiedOn { get; set; }
        public string CorrelationUId { get; set; }
        public List<object> EventMessageParams { get; set; }
    }


        public class WorkItemAssociation
        {
            public string WorkItemAssociationUId { get; set; }
            public object WorkItemAssociationId { get; set; }
            public string WorkItemUId { get; set; }
            public string ProductInstanceUId { get; set; }
            public string AssociationTypeUId { get; set; }
            public string EntityUId { get; set; }
            public string WorkItemTypeUId { get; set; }
            public string ItemAssociatedUId { get; set; }
            public string ItemExternalId { get; set; }
            public string RowStatusUId { get; set; }
            public string CorrelationUId { get; set; }
            public object RowVersion { get; set; }
            public object WorkItem { get; set; }
            public object ProductInstance { get; set; }
            public object Entity { get; set; }
            public object WorkItemType { get; set; }
            public object RowStatus { get; set; }
            public string CreatedByUser { get; set; }
            public string CreatedByApp { get; set; }
            public DateTime CreatedOn { get; set; }
            public string ModifiedByUser { get; set; }
            public string ModifiedByApp { get; set; }
            public DateTime ModifiedOn { get; set; }
            public object UserUId { get; set; }
            public int ItemState { get; set; }
        }

        public class WorkItemAttribute
        {
            public string WorkItemAttributeUId { get; set; }
            public int WorkItemAttributeId { get; set; }
            public string ClientUId { get; set; }
            public string WorkItemUId { get; set; }
            public string WorkItemTypeAttributeUId { get; set; }
            public string Name { get; set; }
            public string DisplayName { get; set; }
            public string DisplayGroup { get; set; }
            public string DataTypeUId { get; set; }
            public string WorkItemAttributeDataSourceUId { get; set; }
            public bool IsSystem { get; set; }
            public bool IsMandatory { get; set; }
            public bool IsVisible { get; set; }
            public bool IsReadOnly { get; set; }
            public object IdValue { get; set; }
            public string IdExternalValue { get; set; }
            public string Value { get; set; }
            public string ExternalValue { get; set; }
            public object ValidationRegEx { get; set; }
            public object ValidationMessage { get; set; }
            public object Description { get; set; }
            public int DisplayOrder { get; set; }
            public string RowStatusUId { get; set; }
            public string CorrelationUId { get; set; }
            public object RowVersion { get; set; }
            public object Client { get; set; }
            public object WorkItem { get; set; }
            public object WorkItemTypeAttribute { get; set; }
            public object DataType { get; set; }
            public object WorkItemAttributeDataSource { get; set; }
            public object RowStatus { get; set; }
            public string CreatedByUser { get; set; }
            public string CreatedByApp { get; set; }
            public DateTime CreatedOn { get; set; }
            public string ModifiedByUser { get; set; }
            public string ModifiedByApp { get; set; }
            public DateTime ModifiedOn { get; set; }
            public object UserUId { get; set; }
            public int ItemState { get; set; }
        }

        public class WorkItemDeliveryConstruct
        {
            public string WorkItemDeliveryConstructUId { get; set; }
            public object WorkItemDeliveryConstructId { get; set; }
            public string WorkItemUId { get; set; }
            public string DeliveryConstructUId { get; set; }
            public string RowStatusUId { get; set; }
            public string CorrelationUId { get; set; }
            public object RowVersion { get; set; }
            public object WorkItem { get; set; }
            public object DeliveryConstruct { get; set; }
            public object RowStatus { get; set; }
            public string CreatedByUser { get; set; }
            public string CreatedByApp { get; set; }
            public DateTime CreatedOn { get; set; }
            public string ModifiedByUser { get; set; }
            public string ModifiedByApp { get; set; }
            public DateTime ModifiedOn { get; set; }
            public object UserUId { get; set; }
            public int ItemState { get; set; }
        }

        public class WorkItemProductInstance
        {
            public string WorkItemProductInstanceUId { get; set; }
            public object WorkItemProductInstanceId { get; set; }
            public string WorkItemUId { get; set; }
            public string ProductInstanceUId { get; set; }
            public string WorkItemExternalId { get; set; }
            public string RowStatusUId { get; set; }
            public string CorrelationUId { get; set; }
            public object RowVersion { get; set; }
            public object WorkItem { get; set; }
            public object ProductInstance { get; set; }
            public object RowStatus { get; set; }
            public string CreatedByUser { get; set; }
            public string CreatedByApp { get; set; }
            public DateTime CreatedOn { get; set; }
            public string ModifiedByUser { get; set; }
            public string ModifiedByApp { get; set; }
            public DateTime ModifiedOn { get; set; }
            public object UserUId { get; set; }
            public int ItemState { get; set; }
        }

        public class WorkItem
        {
            public string WorkItemUId { get; set; }
            public int WorkItemId { get; set; }
            public int StackRank { get; set; }
            public string WorkItemTypeUId { get; set; }
            public string WorkItemExternalId { get; set; }
            public string WorkItemExternalUrl { get; set; }
            public object ReleaseUId { get; set; }
            public object IterationUId { get; set; }
            public object TeamAreaUId { get; set; }
            public string ClientUId { get; set; }
            public string CreatedByProductInstanceUId { get; set; }
            public string CreatedAtSourceByUser { get; set; }
            public DateTime CreatedAtSourceOn { get; set; }
            public string ModifiedAtSourceByUser { get; set; }
            public DateTime ModifiedAtSourceOn { get; set; }
            public object Tags { get; set; }
            public string RowStatusUId { get; set; }
            public string CorrelationUId { get; set; }
            public int CorrelationBatchId { get; set; }
            public object RowVersion { get; set; }
            public object WorkItemType { get; set; }
            public object Release { get; set; }
            public object Iteration { get; set; }
            public object TeamArea { get; set; }
            public object Client { get; set; }
            public object CreatedByProductInstance { get; set; }
            public object RowStatus { get; set; }
            public List<WorkItemAssociation> WorkItemAssociations { get; set; }
            public object WorkItemAttachments { get; set; }
            public List<WorkItemAttribute> WorkItemAttributes { get; set; }
            public List<WorkItemDeliveryConstruct> WorkItemDeliveryConstructs { get; set; }
            public object WorkItemExtensions { get; set; }
            public List<WorkItemProductInstance> WorkItemProductInstances { get; set; }
            public string CreatedByUser { get; set; }
            public string CreatedByApp { get; set; }
            public DateTime CreatedOn { get; set; }
            public string ModifiedByUser { get; set; }
            public string ModifiedByApp { get; set; }
            public DateTime ModifiedOn { get; set; }
            public object UserUId { get; set; }
            public int ItemState { get; set; }
        }

        public class WorkItemMain
        {
            public int TotalRecordCount { get; set; }
            public int TotalPageCount { get; set; }
            public int CurrentPage { get; set; }
            public int BatchSize { get; set; }
            public List<WorkItem> WorkItems { get; set; }
            public List<object> Faults { get; set; }
            public int StatusCode { get; set; }
            public object MergeResult { get; set; }
        }

    #endregion


    public class AccountPermissionView
    {
       // public string EmailId { get; set; }
       // public string DisplayName { get; set; }
       // public string AccessRoleScopeName { get; set; }
       // public string AccessRoleName { get; set; }
       // public object AppServiceName { get; set; }
       // public object ClientName { get; set; }
       // public object DeliveryConstructName { get; set; }
       // public object DeliveryConstructTypeName { get; set; }
       // public object AccessRoleWorkItemTypeName { get; set; }
        public string EntityName { get; set; }
       // public object WorkItemTypeName { get; set; }
       // public object AccessLevelName { get; set; }
       // public object AccessLevelNamePropertyName { get; set; }
       // public object AccessLevelPropertyName { get; set; }
       // public string AccessPrivilegeName { get; set; }
       // public object EntityAccessLevelAccessPrivilege { get; set; }
       // public string EntityAccessLevelAccessPrivilegeName { get; set; }
       // public bool EntityAccessLevelAccessPrivilegeAccessLevel { get; set; }
       // public bool EntityAccessLevelAccessPrivilegeWorkItemTypes { get; set; }
       // public object AccountStatusName { get; set; }
       // public string AccountUId { get; set; }
       // public int AccountId { get; set; }
       // public string AccessRoleUId { get; set; }
       // public object AppServiceUId { get; set; }
       // public object ClientUId { get; set; }
       // public object DeliveryConstructUId { get; set; }
       // public string AccessRoleScopeUId { get; set; }
       // public object DeliveryConstructTypeUId { get; set; }
      //  public object AccessRoleWorkItemTypeUId { get; set; }
     //   public object AccessLevelUId { get; set; }
        public string EntityUId { get; set; }
        public string WorkItemTypeUId { get; set; }
       // public string AccessPrivilegeUId { get; set; }
        public string AccessPrivilegeCode { get; set; }
      //  public string AccountStatusUId { get; set; }
    }

    public class SecurityAcess
    {
        public List<AccountPermissionView> AccountPermissionViews { get; set; }
        public object Faults { get; set; }
        public int StatusCode { get; set; }
        public object MergeResult { get; set; }
    }
    public class UserSecurityAcess
    {


      public Guid ClientUID { get; set; }
        public string Token { get; set; }
    }

    public class UserSecurityAcessAgile
    {


        public Guid ClientUID { get; set; }
        public Guid DeliveryConstructUId { get; set; }
        public string UserID { get; set; }
        public string Token { get; set; }
    }

    public class UserSecurityAcessRole
    {


        public Guid ClientUID { get; set; }
        public Guid DeliveryConstructUId { get; set; }
        public string AccessPrivilegeCode { get; set; }
        public string AccessRoolName{ get; set; }
    }

    public class SPEClients
    {
        public string ClientUID { get; set; }
        public string DeliveryConstructUId { get; set; }
    }
}