using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Accenture.MyWizard.Ingrain.DataModels.Models
{
    //public class PythonInfo
    //{
    //    public PythonCategory Category { get; set; }
    //    public string Status { get; set; }
    //}

    public class PythonInfo
    {
        public PythonCategory Category { get; set; }
        public string Status { get; set; }

        public string correlationId { get; set; }
    }
    public class PythonCategory
    {
        public string Category { get; set; }
        public string Message { get; set; }
    }
}
