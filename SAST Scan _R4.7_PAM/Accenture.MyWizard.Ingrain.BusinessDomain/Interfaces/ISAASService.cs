using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Accenture.MyWizard.Ingrain.DataModels.Models;
using Newtonsoft.Json.Linq;

namespace Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces
{
    public interface ISAASService
    {
        SAASProvisionResponse ProvisionSAAS(SAASProvisioningRequest request);

        HttpClient GetOAuthAsyncFds(string clientid, string clientsecret, string tokenendpoint, string scope, string scopes, string isazureadenabled, string azureclientid, string azureclientsecret, string azuretokenendpoint, string azureresource);

        string updateVDSProvisioning(VDSRequestPayload vDSRequestPayload);
    }
}
