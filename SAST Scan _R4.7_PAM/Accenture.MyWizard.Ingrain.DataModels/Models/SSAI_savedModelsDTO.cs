using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.DataModels.Models
{
    public class SSAI_savedModelsDTO
    {
        public string _id { get; set; }
        public string CorrelationId { get; set; }
        public string FileName { get; set; }
        public string Configuration { get; set; }
        public string CreatedByUser { get; set; }
        public string FilePath { get; set; }
        public string FileType { get; set; }
        public string HTId { get; set; }
        public string ModifiedOn { get; set; }
        public object ProblemType { get; set; }
        public object TrainCols { get; set; }
        public double Version { get; set; }
        public string inputSample { get; set; }
        public string pageInfo { get; set; }       
    }
   

}
