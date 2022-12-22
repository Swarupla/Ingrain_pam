using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Accenture.MyWizard.Ingrain.DataModels.Models
{
    public class FeaturePredictionTestDTO
    {
        public List<JObject> PredictionData { get; set; }
        public string Status { get; set; }
        public string Progress { get; set; }
        public string Message { get; set; }
        public string ModelName { get; set; }
        public string DataSource { get; set; }
        public string BusinessProblem { get; set; }
        public string ModelType { get; set; }
        public string WFId { get; set; }
        public string CorrelationId { get; set; }
        public string Category { get; set; }

    }
}
