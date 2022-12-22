using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Accenture.MyWizard.Ingrain.DataModels.Models
{
    public class HyperParametersDTO
    {
        public List<FloatHyperParameters> FloatHyperParameters { get; set; }
        public List<IntegerHyperParameters> IntegerHyperParameters { get; set; }
        public Dictionary<string, Dictionary<string, string>> StringHyperParameters { get; set; }
        public List<SavedHyperVersions> SavedHyperVersions { get; set; }
        public string ModelName { get; set; }
        public string DataSource { get; set; }
        public string BusinessProblem { get; set; }
        public string ModelType { get; set; }
        public string Category { get; set; }
    }
    public class FloatHyperParameters
    {
        public string AttributeName { get; set; }
        public float MinValue { get; set; }
        public float MaxValue { get; set; }
        public float DefaultValue { get; set; }
    }
    public class IntegerHyperParameters
    {
        public string AttributeName { get; set; }
        public float MinValue { get; set; }
        public float MaxValue { get; set; }
        public float DefaultValue { get; set; }
    }
    public class SavedHyperVersions
    {
        public string CorrelationId { get; set; }
        public string HTId { get; set; }
        public string VersionName { get; set; }
    }
}
