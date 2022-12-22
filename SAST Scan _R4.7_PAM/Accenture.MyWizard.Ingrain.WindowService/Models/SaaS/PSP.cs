using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.WindowService.Models
{
    public class PSP
    {
        public List<ProjectStructure> ProjectStructureProvider { get; set; }
    }

    public class ProjectStructure
    {

        public string Name { get; set; }

        public string ProjectStructureProviderTypeName { get; set; }

        public string ServiceUrl { get; set; }

        public string Method { get; set; }

        public string MIMEMediaType { get; set; }

        public string Accept { get; set; }

        public DataFormatter DataFormatter { get; set; }

        public AuthProvider AuthProvider { get; set; }

        public string InputRequestType { get; set; }

        public string InputRequestValues { get; set; }

        public string DefaultKeys { get; set; }

        public string DefaultValues { get; set; }

        public string ReadElementfromResponse { get; set; }

        public string IsProjectIdCanbeNull { get; set; }

        public string ExpectedResultfromResponse { get; set; }

        public string InputRequestKeys { get; set; }

        public string JsonRootNode { get; set; }
    }

    public class DataFormatter
    {
        public string XsltFilePath { get; set; }

        public string Name { get; set; }

        public string XsltArguments { get; set; }

        public string DataFormatterTypeName { get; set; }
    }

    public class AuthProvider
    {

        public string AuthProviderTypeName { get; set; }

        public string Name { get; set; }

        public string FederationUrl { get; set; }

        public string ClientId { get; set; }

        public string Secret { get; set; }

        public string Scope { get; set; }
        public string Resource { get; set; }

        public string Subject { get; set; }
        public string Issuer { get; set; }
        public string Thumbprint { get; set; }
        public string IsTLS12Enabled { get; set; }
        public string CertType { get; set; }
        public string GrantType { get; set; }
    }
}
