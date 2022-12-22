using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces
{
    public interface IModelMonitorService
    {
        public JObject ModelMetrics(string clientid, string dcid,string correlationId);

        public List<JObject> TrainedModelHistory(string correlationId, string clientid, string dcid);
    }
}
