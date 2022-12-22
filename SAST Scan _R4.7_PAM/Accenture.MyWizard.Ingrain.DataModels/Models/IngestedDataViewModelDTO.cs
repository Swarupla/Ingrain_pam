using MongoDB.Bson.Serialization.Attributes;

namespace Accenture.MyWizard.Ingrain.DataModels.Models
{
    public class IngestedDataViewModelDTO
    {      
        public string CorrelationId { get; set; }
        
        public double Upload { get; set; }
        
        public string InputData { get; set; }

        [BsonElement]
        public object ColumnUniqueValues { get; set; }
    }
}
