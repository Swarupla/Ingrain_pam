using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Accenture.MyWizard.Ingrain.DataModels.Models
{
    public class SSAI_UseCaseDTO
    {
        public string _id { get; set; }
        public string Status { get; set; }
        public int Progress { get; set; }
        public string CorrelationId { get; set; }
        public string pageInfo { get; set; }
        public string CreatedOn { get; set; }
        public int CreatedByUser { get; set; }
        public string ModifiedOn { get; set; }
        public int ModifiedByUser { get; set; }
    }
}
