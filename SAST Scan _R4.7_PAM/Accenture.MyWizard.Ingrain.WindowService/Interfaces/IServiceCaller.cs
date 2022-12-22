using Accenture.MyWizard.Ingrain.WindowService.Models.SaaS;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Accenture.MyWizard.Ingrain.WindowService.Interfaces
{
   public interface IServiceCaller
    {
        Task<XDocument> GetDocUsingService(ServiceCallerRequest serviceCallerRequest, List<string> faults);
    }
}
