using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Accenture.MyWizard.Ingrain.DataModels.Models
{
    public class PublicTemplateModel
    {
        public List<PublicTemplates> publicTemplates { get; set; }
        public string Categories { get; set; }
    }
    public class PublicTemplates
    {
        public string CorrelationId { get; set; }
        public string ModelName { get; set; }

        // Added for Market place
        public string BusinessProblem { get; set; }
        public bool IsCascadeModelTemplate { get; set; }

        public string LinkedApp { get; set; }
        public string Category { get; set; }
        

        public string Custom { get; set; }

        public bool IsMarketPlaceTemplate { get; set; }
    }
    public class publicTemplateDTO
    {
        public string _id { get; set; }
        public string CorrelationId { get; set; }
        public string[] Category { get; set; }
        public string[] ModelName { get; set; }
        public string ApplicationName { get; set; }
        public string UseCaseName { get; set; }
        public bool IsCascadeModelTemplate { get;set;}
        public string CreatedOn { get; set; }
        public string CreatedByUser { get; set; }
        public string ModifiedOn { get; set; }
        public string ModifiedByUser { get; set; }
        public int ArchivalDays { get; set; }
    }
}
