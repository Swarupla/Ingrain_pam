using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.WindowService.Models.PhoenixPayloads
{
    public class ENSChangeRequest
    {
        public string ChangeRequestUId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public List<ChangeRequestDeliveryConstructs> ChangeRequestDeliveryConstructs { get; set; }
        public List<ChangeRequestExtensions> ChangeRequestExtensions { get; set; }
    }

    public class ChangeRequestDeliveryConstructs
    {
        public string DeliveryConstructUId { get; set; }
        public string DeliveryConstructTypeUId { get; set; }
    }

    public class ChangeRequestExtensions
    {
        public string FieldName { get; set; }
        public string FieldValue { get; set; }
    }
}
