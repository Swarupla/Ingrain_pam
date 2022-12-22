using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Accenture.MyWizard.Ingrain.DataModels.Models
{
    [BsonIgnoreExtraElements]
    public class IngrainDeliveryConstruct
    {
        public string UserId { get; set; }

        public string ClientUId { get; set; }

        public string DeliveryConstructUID { get; set; }

        public bool Cookie { get; set; }

        public string AccessPrivilegeCode { get; set; }

        public string AccessRoleName { get; set; }
    }
}
