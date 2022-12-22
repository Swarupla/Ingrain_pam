using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.DataModels.Models
{
    [BsonIgnoreExtraElements]
    public class SSAIIngrainRequest
    {
        public string RequestStatus;

        public int NewCount;

        public int OccupiedCount;

        public int InProgressCount;
    }
}
