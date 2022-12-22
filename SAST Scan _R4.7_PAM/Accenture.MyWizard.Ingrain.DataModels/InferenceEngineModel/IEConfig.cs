using MongoDB.Bson;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.DataModels.InferenceEngineModel
{
    public class IEConfig
    {
        public string CorrelationId { get; set; }

        public string ApplicationId { get; set; }

        public string CreatedBy { get; set; }

        public DateTime CreatedOn { get; set; }

        public string ModifiedBy { get; set; }

        public DateTime ModifiedOn { get; set; }

        public List<FeatureCombinations> FeaturesAndCombinations { get; set; }

        public List<string> DateColumnList { get; set; }

        public List<string> DimensionsList { get; set; }

        public List<string> MetricColumnList { get; set; }

        public dynamic FilterValues { get; set; }

        public List<string> SuggestedDimensionsList { get; set; }

        public List<string> NumericalColumns { get; set; }

        public List<string> BinColumns { get; set; }

    }

    public class IEREsponse
    {
        public string Status { get; set; }

        public string Message { get; set; }

        public string Progress { get; set; }

        public string CorrelationId { get; set; }

        public string RequestId { get; set; }

        public string MetricColumn { get; set; }

        public string DateColumn { get; set; }

    }


    public class FeatureCombinations
    {
        public string FeatureName { get; set; }
        public string ConnectedFeatures { get; set; }
    }




    public class IEViewConfigResponse
    {
        public List<IESavedConfig> SavedConfigValues { get; set; }
        public AllConfigValues AllConfigValues { get; set; }
    }


    public class AllConfigValues
    {
        public List<string> DateColumnList { get; set; }
        public List<string> DimensionsList { get; set; }
        public List<string> MetricColumnList { get; set; }
        public List<string> Features { get; set; }
        public List<FeatureCombinations> FeatureCombinations { get; set; }
    }
}
