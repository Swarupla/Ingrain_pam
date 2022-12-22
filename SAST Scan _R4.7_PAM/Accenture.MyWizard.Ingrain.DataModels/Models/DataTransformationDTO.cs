#region Copyright (c) 2019 Accenture . All rights reserved.

// <copyright company="Accenture">
// Copyright (c) 2019 Accenture.  All rights reserved.
// Reproduction or transmission in whole or in part, in any form or by any means, 
// electronic, mechanical or otherwise, is prohibited without the prior written 
// consent of the copyright owner.
// </copyright>

#endregion

namespace Accenture.MyWizard.Ingrain.DataModels.Models
{
    using MongoDB.Bson.Serialization.Attributes;
    using Newtonsoft.Json.Linq;
    using System.Collections.Generic;

    public class DataTransformationDTO
    {
        public string CorrelationId { get; set; }

        public List<object> InputData { get; set; }

        public string ColumnList { get; set; }

        public string ColumnUniqueValues { get; set; }

        public string CreatedOn { get; set; }

        public string CreatedByUser { get; set; }

        public string PageInfo { get; set; }

        public TimeSeriesFrequencyAttributes TimeSeriesInputData { get; set; }
    }

    public class TimeSeriesFrequencyAttributes
    {
        public List<object> Hourly { get; set; } = new List<object>();

        public List<object> Daily { get; set; } = new List<object>();

        public List<object> Weekly { get; set; } = new List<object>();        

        public List<object> Monthly { get; set; } = new List<object>();        

        public List<object> Quarterly { get; set; } = new List<object>();

        public List<object> HalfYearly { get; set; } = new List<object>();

        public List<object> Yearly { get; set; } = new List<object>();        

        public List<object> Fortnightly { get; set; } = new List<object>();

        public List<object> CustomDays { get; set; } = new List<object>();
    }

    public class DataTransformationViewData
    {
        public JObject ViewData { get; set; }        

        public string UseCaseDetails { get; set; }

        public string ModelName { get; set; }

        public string DataSource { get; set; }

        public string BusinessProblem { get; set; }    

        public string ModelType { get; set; }
        public string Category { get; set; }
    }
    public class ColumnUniqueValue
    {
        public string correlationId { get; set; }

        public List<UniqueValues> ColumnsUniqueValues { get; set; }

    }
    public class UniqueValues
    {
        public string ColumnName { get; set; }

        public Dictionary<string, string> UniqueValue { get; set; }

    }
}