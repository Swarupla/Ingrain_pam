using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.DataModels.InferenceEngineModel
{
    public class IEReTrainTask
    {
        public string TaskUId { get; set; }
        public string TaskCode { get; set; }
        public string TaskName { get; set; }
        public bool ManualTrain { get; set; }
        public string CorrelationIds { get; set; }
        public string TimeToRun { get; set; }
        public string TimePeriod { get; set; }
        public string FrequencyInDays { get; set; }
        public string ConfigRefreshDays { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string ModifiedBy { get; set; }
    }
}
