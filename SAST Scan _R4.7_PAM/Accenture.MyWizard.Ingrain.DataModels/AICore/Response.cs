using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.DataModels.AICore
{
    public class Response
    {
        public Guid RequestId { get; }
        public string ClientId { get; private set; }
        public string DeliveryConstructID { get; private set; }
        public string ServiceID { get; private set; }
        public string CorrelationId { get; set; }

        public DateTime RequestDateTime { get; private set; }
        public DateTime ResponseDateTime { get; private set; }
        public object ResponseData { get; set; }

        public Response(string clientId, string dcid, string serviceid)
        {
            RequestId = Guid.NewGuid();
            ClientId = clientId;
            DeliveryConstructID = dcid;
            ServiceID = serviceid;
            RequestDateTime = DateTime.UtcNow;
        }

        public void SetResponseDate(DateTime responseDate)
        {
            this.ResponseDateTime = responseDate;
        }



    }


    public class TrainingResponse
    {
        public string is_success { get; set; }
        public string message { get; set; }
        public string result { get; set; }
    }
}
