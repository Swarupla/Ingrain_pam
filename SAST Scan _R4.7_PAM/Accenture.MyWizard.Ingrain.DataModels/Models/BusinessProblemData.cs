using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization.Attributes;

namespace Accenture.MyWizard.Ingrain.DataModels.Models
{
    [BsonIgnoreExtraElements]
    public class BusinessProblemDataDTO
    {
        [BsonId]
        public string _id { get; set; }

        [BsonElement("DeliveryConstructUID")]
        public string DeliveryConstructUID { get; set; }

        [BsonElement("AppId")]
        public string AppId { get; set; }

        [BsonElement("ClientUId")]
        public string ClientUId { get; set; }

        [BsonElement("BusinessProblems")]
        public Object BusinessProblems { get; set; }
        [BsonElement]
        public string TargetColumn { get; set; }
        [BsonElement]
        public object InputColumns { get; set; }
        [BsonElement("TimePeriod")]
        public string TimePeriod { get; set; }
        [BsonElement("CorrelationId")]
        public string CorrelationId { get; set; }
        [BsonElement("AvailableColumns")]
        public object AvailableColumns { get; set; }
        public object TimeSeries { get; set; }
        public string TargetUniqueIdentifier { get; set; }
        public string ProblemType { get; set; }
        [BsonElement("CreatedOn")]
        public string CreatedOn { get; set; }
        [BsonElement("CreatedByUser")]
        public string CreatedByUser { get; set; }
        [BsonElement("ModifiedOn")]
        public string ModifiedOn { get; set; }
        [BsonElement("ModifiedByUser")]
        public string ModifiedByUser { get; set; }

        [BsonElement("ParentCorrelationId")]
        public string ParentCorrelationId { get; set; }

        [BsonElement("IsDataTransformationRetained")]
        public bool IsDataTransformationRetained { get; set; }

        /// <summary>
        /// IsCustomColumnSelected
        /// </summary>
        public string IsCustomColumnSelected { get; set; }
    }

    
    public class BusinessProblemData
    {
        public List<BusinessProblemDataDTO> BusinessProblemDataDTOs { get; set; }
    }
    public class BusinessProblemInstaDTO
    {
        [BsonId]
        public string _id { get; set; }

        [BsonElement("DeliveryConstructUID")]
        public string DeliveryConstructUID { get; set; }

        [BsonElement("AppId")]
        public string AppId { get; set; }

        [BsonElement("ClientUId")]
        public string ClientUId { get; set; }

        [BsonElement("BusinessProblems")]
        public Object BusinessProblems { get; set; }
        [BsonElement("InstaURL")]
        public string InstaURL { get; set; }
        [BsonElement]
        public string TargetColumn { get; set; }
        [BsonElement]
        public object InputColumns { get; set; }        
        
        [BsonElement("CorrelationId")]
        public string CorrelationId { get; set; }
        [BsonElement("AvailableColumns")]
        public object AvailableColumns { get; set; }
        public string UseCaseID { get; set; }
        public object TimeSeries { get; set; }
        public string TargetUniqueIdentifier { get; set; }
        public string InstaId { get; set; }
        public string TemplateUsecaseID { get; set; }

        public string ProblemType { get; set; }
        [BsonElement("CreatedOn")]
        public string CreatedOn { get; set; }
        [BsonElement("CreatedByUser")]
        public string CreatedByUser { get; set; }
        [BsonElement("ModifiedOn")]
        public string ModifiedOn { get; set; }
        [BsonElement("ModifiedByUser")]
        public string ModifiedByUser { get; set; }
    }
    public class UseCaseSave
    {
        public string ErrorMessage { get; set; }
        public bool IsInserted { get; set; }
    }
}

