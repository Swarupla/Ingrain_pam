using Accenture.MyWizard.Shared.Helpers;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System;
using System.Configuration;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Accenture.MyWizard.Ingrain.DataAccess
{
    public class MonteCarloConnection
    {
        private IngrainAppSettings AppSettings { get; set; }

        public MonteCarloConnection(IOptions<IngrainAppSettings> settings)
        {
            AppSettings = settings.Value;
        }
        public MongoClient GetDatabaseConnection()
        {
            MongoClient client = new MongoClient();
            string connectionString = AppSettings.MonteCarloConnection;
            string ssl_ca_certs = Convert.ToString(AppSettings.certificatePath);
            string ssl_ca_certs_pwd = Convert.ToString(AppSettings.certificatePassKey);
            MongoClientSettings settings = MongoClientSettings.FromUrl(new MongoUrl(connectionString));
            if (settings.UseTls == true)
            {
                var cert = new X509Certificate2(ssl_ca_certs, ssl_ca_certs_pwd);
                if (cert != null)
                {
                    settings.SslSettings = new SslSettings
                    {
                        CheckCertificateRevocation = false,
                        ClientCertificates = new[] { cert },
                        ServerCertificateValidationCallback = delegate (object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
                        {
                            return true;
                        }
                    };
                    settings.SslSettings.EnabledSslProtocols = SslProtocols.Tls12;
                }
                else
                {
                    throw new Exception("CertificateNotFound");
                }
                client = new MongoClient(settings);
            }
            else
            {
                client = new MongoClient(connectionString);
            }
            return client;
        }
    }
}
