using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization.Attributes;

namespace Accenture.MyWizard.Ingrain.DataModels.Models
{ 
        [BsonIgnoreExtraElements]
        public class ATRProvisionRequestDto
        {
            /// <summary>
            /// E2EUId
            /// </summary>
            [BsonElement("E2EUId")]
            public string E2EUId { get; set; }

        /// <summary>
        /// DeliveryConstructUId
        /// </summary>
        [BsonElement("DeliveryConstructUId")]
        public string DeliveryConstructUId { get; set; }

        /// <summary>
        /// E2EName
        /// 
        /// </summary>
        [BsonElement("E2EName")]
            public string E2EName { get; set; }

            /// <summary>
            /// DF_TicketPull_API
            /// </summary>
            [BsonElement("DF_TicketPull_API")]
            public string DF_TicketPull_API { get; set; }
            /// <summary>
            /// API_Token_Generation
            /// </summary>
            [BsonElement("API_Token_Generation")]
            public string API_Token_Generation { get; set; }
            /// <summary>
            /// UserName
            /// </summary>
            [BsonElement("Username")]
            public string Username { get; set; }
            /// <summary>
            /// Password
            /// </summary>
            [BsonElement("Password")]
            public string Password { get; set; }
        }

    public class SaaSclientInfo
    {
        public string ClientName { get; set; }
        public string DeliveryConstructName { get; set; }
        public string E2EName { get; set; }
    }
}
