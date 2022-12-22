using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Accenture.MyWizard.Ingrain.DataModels.Models
{
    public class FeatureEngineeringDTO
    {
        public JObject ProcessData { get; set; }
        public string Status { get; set; }
        public string Progress { get; set; }
        public string ModelName { get; set; }
        public string DataSource { get; set; }
        public string ModelType { get; set; }
        public string BusinessProblem { get; set; }
        public bool InstaFlag { get; set; }
        public string Category { get; set; }
        public bool Retrain { get; set; }
        public bool IsIntiateRetrain { get; set; }
        public bool IsModelTrained { get; set; }
        public bool IsCascadingButton { get; set; }
        public bool IsFMModel { get; set; }
        public bool IsIncludedInNormalCascade { get; set; }
        public string TargetUniqueIdentifier { get; set; }
    }
}
