using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.WindowService.Models
{
    public class ClientDetails
    {
        public string ClientUId { get; set; }
        public string Name { get; set; }
        public string CreatedOn { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedOn { get; set; }
        public string ModifiedBy { get; set; }
    }

    public class DeliveryConstructDetails
    {
        public string ClientUId { get; set; }
        public string ClientName { get; set; }
        public string DeliveryConstructUId { get; set; }
        public string DeliveryConstructName { get; set; }
        public List<ProductDetails> Products { get; set; }
        public string CreatedOn { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedOn { get; set; }
        public string ModifiedBy { get; set; }
    }


    public class AppDeliveryConstructs
    {
        public string AppServiceUId { get; set; }
        public string Name { get; set; }

        public List<ProvisionedClientDeliveryConstruct> AppServiceClientDeliveryConstructs { get; set; }
    }

    public class ProvisionedClientDeliveryConstruct
    {
        public string ClientUId { get; set; }
        public string DeliveryConstructUId { get; set; }
    }


    public class ProductDetails
    {
        public string ProductUId { get; set; }
        public string Name { get; set; }
    }

    public class AutoTraintask
    {
        public string TaskUId { get; set; }
        public string TaskCode { get; set; }
        public string TaskName { get; set; }
        public bool IsCompleted { get; set; }
        public int UpdateRecords { get; set; }
        public bool IsActive { get; set; }
        public string LastExecutedDate { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
        public string CreatedOn { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedOn { get; set; }
        public string ModifiedBy { get; set; }
    }
}
