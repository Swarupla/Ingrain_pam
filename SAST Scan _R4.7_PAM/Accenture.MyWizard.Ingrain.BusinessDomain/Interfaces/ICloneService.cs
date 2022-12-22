using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Accenture.MyWizard.Ingrain.DataModels.Models;
using Newtonsoft.Json.Linq;

namespace Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces
{
    public interface ICloneService
    {
        void ColumnsforClone(string correlationId, string newCorrId, string newId, string newModelName, string collectionName, string userId, string deliveryConstructUID, string clientUId, out string cloneStatus);
        void InsertClone(JObject data, string Collection);
        void UpdateDeployedModels(string correlationId);
    }
}
