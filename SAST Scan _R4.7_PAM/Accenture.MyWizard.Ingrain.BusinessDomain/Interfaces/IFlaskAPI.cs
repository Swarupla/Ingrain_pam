using Accenture.MyWizard.Ingrain.DataModels.AICore;
using Newtonsoft.Json.Linq;
using System;

namespace Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces
{
    public interface IFlaskAPI
    {
        void CallPython(string CorrelationID, string UniqueID,string pageInfo);
        
    }
}
