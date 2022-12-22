using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Accenture.MyWizard.Ingrain.DataModels.Models
{
    public class DataEngineeringDTO
    {
        public string processData { get; set; }
        public string useCaseDetails { get; set; }
        public string ModelName { get; set; }
        public string DataSource { get; set; }
        public string BusinessProblem { get; set; }
        public string ModelType { get; set; }
        public string Upd { get; set; }
        public string Status { get; set; }
        public string Progress { get; set; }
        public string Message { get; set; }
        public bool InstaFlag { get; set; }
        public string TargetColumn { get; set; }
        public string Category { get; set; }
        public string TargetUniqueIdentifier { get; set; }

        /// <summary>
        /// New Feature - Text Type Columns added by Shreya
        /// </summary>
        public List<string> TextTypeColumnList { get; set; }

        /// <summary>
        /// New Feature - Feature Selection Data added by Shreya
        /// </summary>
        public JObject FeatureSelectionData { get; set; }

        /// <summary>
        /// Cleaned Up Column List
        /// </summary>
        public List<string> CleanedUpColumnList { get; set; }

        /// <summary>
        /// Features data type
        /// </summary>
        public JObject FeatureDataTypes { get; set; }

        /// <summary>
        /// IsCustomColumnSelected
        /// </summary>
        public string IsCustomColumnSelected { get; set; }

        /// <summary>
        /// IsModelTrained
        /// </summary>
        public bool IsModelTrained { get; set; }
    }
    public class PythonResult
    {
        public string message { get; set; }
        public string status { get; set; }
    }
}
