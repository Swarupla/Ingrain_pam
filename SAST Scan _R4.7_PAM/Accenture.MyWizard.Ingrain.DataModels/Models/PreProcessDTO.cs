using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;

namespace Accenture.MyWizard.Ingrain.DataModels.Models
{
    public class PreProcessModelDTO
    {
        public JObject PreprocessedData { get; set; }
        public string ModelName { get; set; }
        public string DataSource { get; set; }
        public string BusinessProblem { get; set; }
        public string CorrelationId { get; set; }
        public string Status { get; set; }
        public string Progress { get; set; }
        public string Message { get; set; }
        public string ModelType { get; set; }        
        public bool InstaFlag { get; set; }
        public string UniqueTargetMessage { get; set; }
        public string Category { get; set; }

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
        /// silhouette_graph - for NLP
        /// </summary>
        public string silhouette_graph { get; set; }

        /// <summary>
        /// AutoBinningDisableColumns - for UI purpose. Enable/Disable columns
        /// </summary>
        public Dictionary<string, string> AutoBinningDisableColumns { get; set; }

        /// <summary>
        /// AutoBinningDisableColumns - for UI purpose. Enable numerical columns
        /// </summary>
        public Dictionary<string, string> AutoBinningNumericalColumns { get; set; }

        /// <summary>
        /// IsMutiClass
        /// </summary>
        public bool IsMultiClass { get; set; }

        /// <summary>
        /// IsModelTrained
        /// </summary>
        public bool IsModelTrained { get; set; }
        public string DataPointsWarning { get; set; }
        public long DataPointsCount { get; set; }
    }
    [BsonIgnoreExtraElements]
    public class PreProcessDTO
    {
        public string PreprocessedData { get; set; }
        public Dictionary<string, string> Prescriptions { get; set; }
        public Dictionary<string, Dictionary<string, Dictionary<string, string>>> ColumnBinning { get; set; }
        public Dictionary<string, Dictionary<string, Dictionary<string, string>>> RecommendedColumns { get; set; }
        public Dictionary<string, Dictionary<string, string>> CategoricalData { get; set; }
        public Dictionary<string, Dictionary<string, string>> MisingValuesData { get; set; }
        public Dictionary<string, Dictionary<string, string>> NumericalData { get; set; }
        public Dictionary<string, Dictionary<string, string>> DataEncodeData { get; set; }
        public string CorrelationId { get; set; }
        public string Flag { get; set; }
        public string TargetColumn { get; set; }
        public bool DataTransformationApplied { get; set; }

        public Dictionary<string, string> AutoBinning { get; set; }

        public bool IsMultiClass { get; set; }
    }
    public class InstaMLPreProcessDTO
    {
        public string PreprocessedData { get; set; }
        public Dictionary<string, string> Prescriptions { get; set; }
        public Dictionary<string, Dictionary<string, Dictionary<string, string>>> ColumnBinning { get; set; }
        public Dictionary<string, Dictionary<string, Dictionary<string, string>>> RecommendedColumns { get; set; }
        public Dictionary<string, Dictionary<string, string>> CategoricalData { get; set; }
        public Dictionary<string, Dictionary<string, dynamic>> MisingValuesData { get; set; }
        public Dictionary<string, Dictionary<string, dynamic>> NumericalData { get; set; }
        public Dictionary<string, Dictionary<string, string>> DataEncodeData { get; set; }
        public string CorrelationId { get; set; }
        public string Flag { get; set; }
        public string TargetColumn { get; set; }
        public bool DataTransformationApplied { get; set; }

        public Dictionary<string, string> AutoBinning { get; set; }

        public bool IsMultiClass { get; set; }
    }

    public class ColumnBinning
    {
        public string Binning { get; set; }
        public string NewName { get; set; }
        public string SubCatName { get; set; }
    }
    public class DataEncoding
    {
        public string attribute { get; set; }
        public string encoding { get; set; }
        public string ChangeReques { get; set; }
        public string PChangeRequest { get; set; }
    }
    public class Skewness
    {
        public string Skeweness { get; set; }
        public string Standardization { get; set; }
        public string Normalization { get; set; }
        public string Log { get; set; }
    }
}
