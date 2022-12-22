using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.DataModels.InferenceEngineModel
{
    public class IEModeConfigInference
    {
        public string CorrelationId { get; set; }

        public string ApplicationId { get; set; }
        public string InferenceConfigType { get; set; }

        public string ClientUId { get; set; }

        public string DeliveryConstructUId { get; set; }

        public string ModelName { get; set; }

        public string FunctionalName { get; set; }
    }
}
