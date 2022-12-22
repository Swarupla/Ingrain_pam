using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.WindowService.Models.PhoenixPayloads
{
    public class EntityRecommendation
    {
        public string EntityRecommendationUId { get; set; }
        public string EntityRecommendationId { get; set; }
        public string EntityUId { get; set; }
        public string WorkitemTypeUId { get; set; }
        public string ItemUId { get; set; }
        public string ItemExternalId { get; set; }
        public string ProductInstanceUId { get; set; }
        public string CreatedAtSourceOn { get; set; }
        public string ModifiedAtSourceOn { get; set; }
        public string CreatedByApp { get; set; }
        public string CreatedOn { get; set; }
        public string ModifiedByApp { get; set; }
        public string ModifiedOn { get; set; }
        public List<IntelligentRecommendations> IntelligentRecommendations { get; set; }
    }


    public class IntelligentRecommendations
    {        
        public string IntelligentRecommendationTypeUId { get; set; }
        public string StateUId { get; set; }       
        public List<IntelligentRecommendationAttributes> IntelligentRecommendationAttributes { get; set; }
    }

    public class IntelligentRecommendationAttributes
    {        
        public string FieldName { get; set; }
        public string FieldValue { get; set; }       
    }

    public class IterationPredictions
    {
        public string UsecaseId { get; set; }
        public string IterationUid { get; set; }

        public IntelligentRecommendations IntelligentRecommendations { get; set; }
    }

}
