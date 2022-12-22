using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.WindowService.Models
{
    public class QueueMonitor
    {
        public string QueueName { get; set; }
        public string TotalQueueLimit { get; set; }
        public string CurrentInprogressCount { get; set; }
        public string QueueStatus { get; set; } //Occupied or Available
        public List<AppQueue> AppWiseQueueDetails { get; set; }
        public string CreatedOn { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedOn { get; set; }
        public string ModifiedBy { get; set; }
    }

    public class AppQueue
    {
        public string AppId { get; set; }
        public string QueueLimit { get; set; }
        public string CurrentInprogressCount { get; set; }
        public string QueueStatus { get; set; } //Occupied or Available
    }
}
