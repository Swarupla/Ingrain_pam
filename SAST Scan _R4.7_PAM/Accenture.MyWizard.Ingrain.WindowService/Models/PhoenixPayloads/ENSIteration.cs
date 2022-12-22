using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.WindowService.Models.PhoenixPayloads
{
    public class ENSIteration
    {
        public string IterationUId { get; set; }
        public string Name { get; set; }

        public List<IterationDeliveryConstructs> IterationDeliveryConstructs { get; set; }
    }

    public class IterationDeliveryConstructs
    {
        public string DeliveryConstructUId { get; set; }
        public string DeliveryConstructTypeUId { get; set; }
    }

    public class IterationPredictionResult
    {
        public string IterationUId { get; set; }
        public string PredictedValue { get; set; }
    }
}
