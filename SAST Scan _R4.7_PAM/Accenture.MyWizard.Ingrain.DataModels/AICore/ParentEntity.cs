using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.DataModels.AICore
{
    public abstract class ParentEntity
    {
        [BsonId]
        public ObjectId _id { get; set; }
    }
}
