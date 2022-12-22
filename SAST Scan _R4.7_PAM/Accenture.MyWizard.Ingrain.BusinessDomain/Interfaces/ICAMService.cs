using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Accenture.MyWizard.Ingrain.DataModels.Models;
using Newtonsoft.Json.Linq;

namespace Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces
{
    public interface ICAMService
    {
        Task<string> PushATRProvisionToDBAsync(ATRProvisionRequestDto atr);
    }
}
