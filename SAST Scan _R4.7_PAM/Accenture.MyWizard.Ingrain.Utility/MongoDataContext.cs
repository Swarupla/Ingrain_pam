using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Accenture.MyWizard.Ingrain.Utility
{
    public class MongoDataContext
    {
        public static MongoClient GetDatabaseConnection(string connectionString, string certificatePath, string certificatePassKey)
        {
            MongoClient client = new MongoClient();
            string ssl_ca_certs = Convert.ToString(certificatePath);
            string ssl_ca_certs_pwd = Convert.ToString(certificatePassKey);
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
