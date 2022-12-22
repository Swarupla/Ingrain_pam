using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Accenture.MyWizard.Ingrain.DataModels.Models
{
    public class TestScenarioModelDTO
    {
        public List<JObject> TestData { get; set; }
        public string ModelName { get; set; }
        public string DataSource { get; set; }
        public string BusinessProblem { get; set; }
        public string ModelType { get; set; }
        public List<List<string>> steps { get; set; }
        public List<string> TestScenarios { get; set; }
        public string Category { get; set; }
    }
}
