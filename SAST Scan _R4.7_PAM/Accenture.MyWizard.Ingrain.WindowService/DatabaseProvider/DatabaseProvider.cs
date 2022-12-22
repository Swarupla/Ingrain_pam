using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Accenture.MyWizard.Ingrain.WindowService
{
    public class DatabaseProvider
    {
        public MongoClient GetDatabaseConnection(string ServiceName = "")
        {
            var appSettings = AppSettingsJson.GetAppSettings();
            MongoClient client = new MongoClient();
            try
            {
                string connectionString = string.Empty;
                if (ServiceName == "Anomaly")
                    connectionString = appSettings.GetSection("AppSettings").GetSection("AnomalyDetectionCS").Value;
                else
                    connectionString = appSettings.GetSection("AppSettings").GetSection("connectionString").Value;
                //string connectionString = appSettings.GetSection("AppSettings").GetSection("connectionString").Value;
                string ssl_ca_certs = appSettings.GetSection("AppSettings").GetSection("certificatePath").Value;
                string ssl_ca_certs_pwd = appSettings.GetSection("AppSettings").GetSection("certificatePassKey").Value;
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
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(DatabaseProvider), nameof(GetDatabaseConnection), ex.StackTrace + "--" + ex.Message, ex, "ConnectionString: " + appSettings.GetSection("AppSettings").GetSection("connectionString").Value, ex.StackTrace, "ERROR1", string.Empty);
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(DatabaseProvider), nameof(GetDatabaseConnection), ex.StackTrace + "--" + ex.Message, ex, "CertificatePath: " + appSettings.GetSection("AppSettings").GetSection("certificatePath").Value, ex.StackTrace, "ERROR2", string.Empty);
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(DatabaseProvider), nameof(GetDatabaseConnection), ex.StackTrace + "--" + ex.Message, ex, "CertificatePassKey: " + appSettings.GetSection("AppSettings").GetSection("certificatePassKey").Value, ex.StackTrace, "ERROR3", string.Empty);
            }

            return client;
        }
    }
}
