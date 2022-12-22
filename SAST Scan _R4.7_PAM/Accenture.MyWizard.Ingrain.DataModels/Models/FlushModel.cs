using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.DataModels.Models
{
    public class FlushModel
    {
        public string Date { get; set; }
        public string[] ClientUId { get; set; }
        public string[] DCId { get; set; }
        public string[] CorrelationId { get; set; }
    }
    public class corrIds
    {
        public string[] CorrelationId { get; set; }
    }
    public class clientDCs
    {
        public string[] ClientUId { get; set; }
        public string[] DCId { get; set; }
    }
    public class dateClientDCs
    {
        public string Date { get; set; }
        public string[] ClientUId { get; set; }
        public string[] DCId { get; set; }
    }
    public class dateRange
    {
        public string StartDate { get; set; }
        public string EndDate { get; set; }
    }

}
