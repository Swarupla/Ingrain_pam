#region Copyright (c) 2018 Accenture . All rights reserved.

// <copyright company="Accenture">
// Copyright (c) 2018 Accenture.  All rights reserved.
// Reproduction or transmission in whole or in part, in any form or by any means, 
// electronic, mechanical or otherwise, is prohibited without the prior written 
// consent of the copyright owner.
// </copyright>

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;


namespace Accenture.MyWizard.Ingrain.DataModels.Models
{
    public class IngestedDataDTO
    {
        [BsonId]
        public string _id { get; set; }
        [BsonElement("Sourcetype")]
        public string Sourcetype { get; set; }
        [BsonElement("DataSource")]
        public string PageInfo { get; set; }
        [BsonElement("PageInfo")]

        public string DataSource { get; set; }
        public string SourceName { get; set; }
        [BsonElement("ModelName")]
        public string ModelName { get; set; }
        [BsonElement]
        public string ClientUID { get; set; }
        [BsonElement("userRole")]
        public string userRole { get; set; }
        [BsonElement("ShortDescription")]
        public string ShortDescription { get; set; }

        [BsonElement("DeliveryConstructUID")]
        public string DeliveryConstructUID { get; set; }

        [BsonElement("AppId")]
        public string AppId { get; set; }

        [BsonElement("Size")]
        public long Size { get; set; }
        [BsonElement("Inputdata")]
        public object Inputdata { get; set; }
        [BsonElement("CorrelationId")]
        public string CorrelationId { get; set; }
        [BsonElement]
        public string[] ColumnsList { get; set; }
        [BsonElement("CreatedOn")]
        public string CreatedOn { get; set; }
        [BsonElement("CreatedByUser")]
        public string CreatedByUser { get; set; }
        [BsonElement("ModifiedOn")]
        public string ModifiedOn { get; set; }
        [BsonElement("ModifiedByUser")]
        public string ModifiedByUser { get; set; }
        public string Category { get; set; }

        public string Language { get; set; }
    }
    public class Inputvalidation
    {
        public string Message { get; set; }
        public string Status { get; set; }
    }
    public class ColumnList
    {
        public List<object> ColumnListDetails { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class IngestDataColumn
    {
        public string _id { get; set; }
        public string[] ColumnsList { get; set; }
        public string CorrelationId { get; set; }
        public dynamic InputData { get; set; }
    }
    public class ValidRecordsDetailsModel
    {
        public ValidRecordsDetails ValidRecordsDetails { get; set; }
    }
    public class ValidRecordsDetails
    {
        public string[] EmptyColumns { get; set; }
        public string Msg { get; set; }
        public long[] Records { get; set; }
    }
    public class DataPoints
    {
        public bool IsUpdated { get; set; }
        public string Message { get; set; }
        public string ErrorMessage { get; set; }
    }
}
