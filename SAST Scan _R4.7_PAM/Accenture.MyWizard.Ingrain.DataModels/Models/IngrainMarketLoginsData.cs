using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Accenture.MyWizard.Ingrain.DataModels.Models
{
    public class IngrainMarketLoginsData
    {
        [BsonId]
        public string _id { get; set; }

        [BsonElement("UserEnterPriseId")]
        public string UserEnterPriseId { get; set; }

        [BsonElement("ClientID")]
        public string ClientID { get; set; }

        [BsonElement("EngagementID")]
        public string EngagementID { get; set; }

        [BsonElement("ClientName")]
        public string ClientName { get; set; }

        [BsonElement("ProjectName")]
        public string ProjectName { get; set; }

        [BsonElement("BusinessReason")]
        public string BusinessReason { get; set; }

        [BsonElement("InformationSource")]
        public string InformationSource { get; set; }

        [BsonElement("UserAzureId")]
        public string UserAzureId { get; set; }
    }

    /// <summary>
    /// MarketPlaceUserModel
    /// </summary>
    public class MarketPlaceUserModel
    {
        [BsonId]
        public string _id { get; set; }

        [BsonElement("UserId")]
        public string UserId { get; set; }

        [BsonElement("TemplateName")]
        public string TemplateName { get; set; }
        public string CreatedOn { get; set; }
        public string CorrelationId { get; set; }
        public string CreatedByUser { get; set; }
        public string ModifiedOn { get; set; }
        public string ModifiedByUser { get; set; }
        public string AccessRight { get; set; }
        public string Category { get; set; }
        public string UserAzureId { get; set; }
    }
    public class ConfigureCertificationFlag
    {
        [BsonId]
        public string _id { get; set; }

        public bool flag { get; set; }
    }
}
