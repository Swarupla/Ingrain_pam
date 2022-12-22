using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.WindowService.Models.PhoenixPayloads
{
    public class IterationQueryResponse
    {
        public int TotalRecordCount { get; set; }
        public int TotalPageCount { get; set; }
        public int CurrentPage { get; set; }
        public int BatchSize { get; set; }
        public List<Iterations> Iterations { get; set; }
    }

    public class Iterations
    {
        public string IterationUId { get; set; }
        public string EndOn { get; set; }
        public string MethodologyUId { get; set; }
    }
}
