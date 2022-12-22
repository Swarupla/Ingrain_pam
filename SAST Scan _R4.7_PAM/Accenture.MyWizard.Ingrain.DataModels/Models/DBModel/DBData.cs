using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.DataModels.Models
{
    public class DBData
    {
        public string CollectionName { get; set; }

        public List<Filters> FilterBy { get; set; }

        public SortBy SortBy { get; set; }

        public int Limit { get; set; }
    }

    public class DBDataInfo
    {
        public dynamic Data { get; set; }
        //public List<BsonDocument> Data { get;set;}

        public int Count { get; set; }
        
    }

    public class Filters
    {
        public string Field { get; set; }

        public string Type { get; set; }

        public dynamic Value { get; set; }

    }

    public class SortBy
    {
        public string Field { get; set; }

        public int Order { get; set; }
    }
}
