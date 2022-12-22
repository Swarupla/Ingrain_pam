using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Accenture.MyWizard.Ingrain.UI.DotNetWrapper.Models
{
    public class EnvironmentCs
    {
        public string authProvider { get; set; }

        public string ingrainDomain { get; set; }

        public string hostingUrl { get; set; }

        public string applicationSuffix { get; set; }

        public string token_Url { get; set; }

        public string grant_type { get; set; }
        public string client_id { get; set; }
        public string resourceId { get; set; }
        public string client_secret { get; set; }

        public string username { get; set; }

        public string password { get; set; }

        public string Environment { get; set; }

        public string fortressUrl { get; set; }
        public string ingrainAPIURL { get; set; }
        public string mywizardHomeUrl { get; set; }
        public string myWizardWebConsoleUrl { get; set; }

        public string myWizardAPIUrl { get; set; }

        public string AppServiceUID { get; set; }

        public string modelTrainingStatusText { get; set; }

        public int sessionTimeout { get; set; }
        public int warningPopupTime { get; set; }
        public int pingInterval { get; set; }
        public string grantType { get; set; }

        public string clientId { get; set; }
        public string clientSecret { get; set; }
        public string scope { get; set; }

        public string ingrainsignoutUrl { get; set; }

        public string TenantId { get; set; }

        public int expireOffsetSeconds { get; set; }

        public string redirectUri { get; set; }
        public string myConcertoHomeURL { get; set; }
        public string ingrainappUrl { get; set; }
        public bool IsAzureTokenRefresh { get; set; }

        public int AzureTokenRefreshTime { get; set; }

        public string PhoenixResourceId { get; set; }

        public string FDSbaseURL { get; set; }
       
    }
}
