using Accenture.MyWizard.Ingrain.DataAccess.InferenceEntities;
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
    public class InferenceEngineDBContext : IInferenceEngineDBContext
    {

        private IOptions<IngrainAppSettings> appSettings { get; set; }
        private MongoClient _mongoClient;
        private IMongoDatabase _database;
        string _connectionString = null;
        string _certificatePath = null;
        string _certificatePassKey = null;
        private readonly string AesKay;
        private readonly string Vector;
        private readonly bool _IsAESKeyVault;

        public IERequestQueueRepository IERequestQueueRepository
        {
            get
            {
                return new IERequestQueueRepository(_database);
            }
        }
        public IEAppIngerationRepository IEAppIngerationRepository
        {
            get
            {
                return new IEAppIngerationRepository(_database);
            }
        }

        public IEModelRepository IEModelRepository
        {
            get
            {
                return new IEModelRepository(_database);
            }
        }

        public InferenceConfigRepository InferenceConfigRepository
        {
            get
            {
                return new InferenceConfigRepository(_database, AesKay, Vector, _IsAESKeyVault);
            }
        }


        public IEConfigTemplateRepository IEConfigTemplateRepository
        {
            get
            {
                return new IEConfigTemplateRepository(_database);
            }
        }

        public IEUseCaseRepository IEUseCaseRepository
        {
            get
            {
                return new IEUseCaseRepository(_database);
            }
        }


        public IEAutoReTrainRepository IEAutoReTrainRepository
        {
            get
            {
                return new IEAutoReTrainRepository(_database);
            }
        }



        public InferenceEngineDBContext(IOptions<IngrainAppSettings> settings)
        {
            appSettings = settings;
            _connectionString = appSettings.Value.IEConnectionString;
            _certificatePath = appSettings.Value.certificatePath;
            _certificatePassKey = appSettings.Value.certificatePassKey;
            AesKay = appSettings.Value.aesKey;
            Vector = appSettings.Value.aesVector;
            _IsAESKeyVault = appSettings.Value.IsAESKeyVault;
            _mongoClient = this.GetDatabaseConnection();
            var dataBaseName = MongoUrl.Create(appSettings.Value.IEConnectionString).DatabaseName;
            _database = _mongoClient.GetDatabase(dataBaseName);
        
        }
        public InferenceEngineDBContext(string connectionString, string certificatePath, string certificatePassKey, string aes, string vector, bool IsAESKeyVault)
        {
            _connectionString = connectionString;
            _certificatePath = certificatePath;
            _certificatePassKey = certificatePassKey;
            AesKay = aes;
            Vector = vector;
            _IsAESKeyVault = IsAESKeyVault;
            _mongoClient = this.GetDatabaseConnection();
            var dataBaseName = MongoUrl.Create(connectionString).DatabaseName;
            _database = _mongoClient.GetDatabase(dataBaseName);

        }

        public MongoClient GetDatabaseConnection()
        {
            MongoClient client = new MongoClient();

            string ssl_ca_certs = Convert.ToString(_certificatePath);
            string ssl_ca_certs_pwd = Convert.ToString(_certificatePassKey);
            MongoClientSettings settings = MongoClientSettings.FromUrl(new MongoUrl(_connectionString));
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
                client = new MongoClient(_connectionString);
            }
            return client;
        }
    }
}
