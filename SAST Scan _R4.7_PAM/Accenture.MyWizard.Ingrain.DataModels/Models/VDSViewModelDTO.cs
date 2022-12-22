using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Accenture.MyWizard.Ingrain.DataModels.Models
{

    public class VDSViewModelDTO
    {
        public string CliendUID { get; set; }

        public List<ModelDetails> ModelDetails { get; set; }
    }    

    public class ModelDetails
    {
        public string CorrelationId { get; set; }        

        public string DUID { get; set; }

        public string ModelType { get; set; }

        public string ModelName { get; set; }

        public string CreatedDateTime { get; set; }

        public string LastModifiedDateTime { get; set; }       

        public string LastTrainDateTime { get; set; }
    }
}
