using System;
using System.Collections.Generic;

namespace Accenture.MyWizard.Ingrain.DataModels.InferenceEngineModel
{
    public class IEFeatureCombination
    {
        public string CorrelationId { get; set; }
        public string RequestId { get; set; }

        public string MetricColumn { get; set; }

        public string DateColumn { get; set; }

        public string MetricColumnType { get; set; }

        public List<string> Features { get; set; }

        public List<FeatureCombinations> FeatureCombinations { get; set; }

        public dynamic FilterValues { get; set; }

        public List<string> SuggestedCombinations { get; set; }

        public List<string> SuggestedFeatures { get; set; }


        public string ModifiedBy { get; set; }

        public DateTime ModifiedOn { get; set; }
    }
}
