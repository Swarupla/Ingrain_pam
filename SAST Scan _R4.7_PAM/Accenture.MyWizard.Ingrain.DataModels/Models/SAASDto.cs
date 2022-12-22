using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.DataModels.Models
{
    public class SAASProvisioningRequest
    {
        /// <summary>
        /// ClientUID
        /// </summary>

        public string ClientUID { get; set; }
        /// <summary>
        /// DeliveryConstructUID
        /// </summary>
        public string DeliveryConstructUID { get; set; }

        /// <summary>
        /// E2EUID
        /// </summary>
        public string E2EUID { get; set; }
        /// <summary>
        /// OrderUId
        /// </summary>
        public string OrderUId { get; set; }
        /// <summary>
        /// OrderItemUId
        /// </summary>
        public string OrderItemUId { get; set; }
        /// <summary>
        /// InstanceType
        /// </summary>
        /// 

        public string RedirectCallbackURL { get; set; }

        public string ErrorCallbackURL { get; set; }

        public string UpdateStatusCallbackURL { get; set; }

        public string InstanceType { get; set; }

        /// <summary>
        /// ServiceUId
        /// </summary>
        public string ServiceUId { get; set; }
        /// <summary>
        /// SourceID
        /// </summary>

        public string ServiceName { get; set; }

        public string SourceID { get; set; }
        /// <summary/// <summary>
        /// ClientName
        /// </summary>
        public string ClientName { get; set; }
        /// <summary>
        /// E2EName
        /// </summary>
        public string E2EName { get; set; }
        /// <summary>
        /// Market
        /// </summary>
        public string Market { get; set; }
        /// <summary>
        /// MarketUnit
        /// </summary>
        public string MarketUnit { get; set; }
        /// <summary>
        /// RequestorID
        /// </summary>
        public string RequestorID { get; set; }
        /// <summary>
        /// WBSE
        /// </summary>
        public string WBSE { get; set; }
        /// <summary>
        /// Region
        /// </summary>
        public string Region { get; set; }
        /// <summary>
        /// ClientGroup
        /// </summary>

        public string ClientGroup { get; set; }

        public string CorrelationUId { get; set; }

        public List<Applications> Applications { get; set; }

        public List<Applications> ProvisionedApplications { get; set; }


    }
    public class Applications
    {
        public string Name { get; set; }
        public string ApplicationUId { get; set; }
    }

    public class SAASProvisionResponse
    {
        /// <summary>
        /// ClientUId
        /// </summary>
        public string ClientUId { get; set; }
        /// <summary>
        /// DeliveryConstructUId
        /// </summary>
        public string DeliveryConstructUId { get; set; }
        /// <summary>
        /// MySaaSServiceOrderUId
        /// </summary>
        public string MySaaSServiceOrderUId { get; set; }
        /// <summary>
        /// InstanceType
        /// </summary>
        public string InstanceType { get; set; }
        /// <summary>
        /// InstanceName
        /// </summary>
        public string InstanceName { get; set; }
        /// <summary>
        /// Service
        /// </summary>
        public string ServiceUId { get; set; }
        /// <summary>
        /// ProvisioningStatus
        /// </summary>
        public string ProvisionStatusUId { get; set; }
        /// <summary>
        /// StatusReason
        /// </summary>
        public string StatusReason { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string ProvisionedUrl { get; set; }
        /// <summary>
        /// InstanceTypeUID
        /// </summary>
        public string InstanceTypeUID { get; set; }

        public string MSApplication { get; set; }

        public string ProvisionedApplications { get; set; }

        public List<ProvisonExtensionsSAAS> ProvisonExtensions { get; set; }
    }

    public class ProvisonExtensionsSAAS
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }
    public class SAASProvisionStatus
    {

        public const string StatusNew = "00100000-0100-0000-0000-000000000000";
        public const string StatusFailed = "00100000-0200-0000-0000-000000000000";
        public const string StatusInprogress = "00100000-0300-0000-0000-000000000000";
        public const string StatusRejected = "00100000-0400-0000-0000-000000000000";
        public const string StatusPending = "00100000-0500-0000-0000-000000000000";
        public const string StatusCompleted = "00100000-0600-0000-0000-000000000000";

    }

    public class SAASProvisionReason
    {

        public const string StatusNew = "New";
        public const string StatusFailed = "Failed";
        public const string ConfigurationMissing = "ATR Configuration values are missing";
        public const string StatusInprogress = "";
        public const string StatusRejected = "Already Provisioned";
        public const string StatusPending = "";
        public const string StatusCompleted = "Provisioned";

    }

    public class SAASProvisionErrorResponse
    {
        /// <summary>
        /// OrderUId
        /// </summary>
        public string OrderUId { get; set; }
        /// <summary>
        /// DeliveryConstructUId
        /// </summary>
        public string OrderItemUId { get; set; }
        /// <summary>
        /// MySaaSServiceOrderUId
        /// </summary>
        public string ServiceUId { get; set; }
        /// <summary>
        /// MySaaSServiceOrderUId
        /// </summary>
        public string Requestorid { get; set; }
        /// <summary>
        /// MySaaSServiceOrderUId
        /// </summary>
        public string Errordetails { get; set; }
        /// <summary>
        /// MySaaSServiceOrderUId
        /// </summary>
        public string Payload { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string ProvisionStatusUId { get; set; }

        public List<ProvisonExtensionsSAAS> ProvisonExtensions { get; set; }

    }
    public class SAASProvisionDetails
    {
        public BsonObjectId _id { get; set; }
        /// <summary>
        /// ClientUID
        /// </summary>
        public string ClientUID { get; set; }
        /// <summary>
        /// DeliveryConstructUID
        /// </summary>
        public string DeliveryConstructUID { get; set; }

        /// <summary>
        /// E2EUID
        /// </summary>
        public string E2EUID { get; set; }
        /// <summary>
        /// OrderUId
        /// </summary>
        public string OrderUId { get; set; }
        /// <summary>
        /// OrderItemUId
        /// </summary>
        public string OrderItemUId { get; set; }
        /// <summary>
        /// InstanceType
        /// </summary>

        public string RedirectCallbackURL { get; set; }

        public string ErrorCallbackURL { get; set; }

        public string UpdateStatusCallbackURL { get; set; }

        public string InstanceType { get; set; }

        /// <summary>
        /// ServiceUId
        /// </summary>
        public string ServiceUId { get; set; }
        /// <summary>
        /// SourceID
        /// </summary>
        /// 
        public string ServiceName { get; set; }
        public string SourceID { get; set; }
        /// <summary/// <summary>
        /// ClientName
        /// </summary>
        public string ClientName { get; set; }
        /// <summary>
        /// E2EName
        /// </summary>
        public string E2EName { get; set; }
        /// <summary>
        /// Market
        /// </summary>
        public string Market { get; set; }
        /// <summary>
        /// MarketUnit
        /// </summary>
        public string MarketUnit { get; set; }
        /// <summary>
        /// RequestorID
        /// </summary>
        public string RequestorID { get; set; }
        /// <summary>
        /// WBSE
        /// </summary>
        public string WBSE { get; set; }
        /// <summary>
        /// Region
        /// </summary>
        public string Region { get; set; }

        /// <summary>
        /// ClientGroup
        /// </summary>
        public string ClientGroup { get; set; }

        /// <summary>
        /// CreatedOn
        /// </summary>

        public string CorrelationUId { get; set; }

        public List<Applications> Applications { get; set; }


        public string isVDSProvisioned { get; set; }

        public DateTime CreatedOn { get; set; }

        /// <summary>
        /// CreatedOn
        /// </summary>
        public DateTime ModifiedOn { get; set; }

        /// <summary>
        /// BillingInfo
        /// </summary>
        //public int BillingInfo { get; set; }
    }

    public class VDSRequestPayload
    {
        public string ClientUID { get; set; }
        public string DeliveryConstructUID { get; set; }
        public string E2EUID { get; set; }

    }

}

