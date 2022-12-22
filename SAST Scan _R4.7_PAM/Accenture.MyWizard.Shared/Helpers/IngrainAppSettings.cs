using System.Collections.Generic;

namespace Accenture.MyWizard.Shared.Helpers
{
    public class IngrainAppSettings
    {
        public string DsaAPIUrl { get; set; }
        public string PhoenixUrl { get; set; }
        public string PhoenixUserName { get; set; }
        public string PhoenixPassword { get; set; }
        public string AppServiceUID { get; set; }
        public string pythonApi { get; set; }
        public string pythonWeblog { get; set; }
        public string pythonapiwordcheck { get; set; }
        public string connectionString { get; set; }
        public string MonteCarloConnection { get; set; }
        public string uploadTxt { get; set; }
        public string uploadXML { get; set; }
        public string userStoryApi { get; set; }
        public string testCaseApi { get; set; }
        public string AccountAccess { get; set; }
        public string Roles { get; set; }
        public bool EnabledML { get; set; }
        public string SSAIDatabase { get; set; }
        public string certificatePath { get; set; }
        public string certificatePassKey { get; set; }
        public string authProvider { get; set; }
        public string VDSLink { get; set; }
        public string foreCastModel { get; set; }
        public string publishURL { get; set; }
        //public string TimeSeries_On { get; set; }
        //public string TimeSeries_Off { get; set; }
        //public string Regression_On { get; set; }
        //public string Regression_Off { get; set; }
        public string username { get; set; }
        public string password { get; set; }

        public string tokenAPIUrl { get; set; }
        public string PAMTokenUserName { get; set; }
        public string PAMTokenUserPWD { get; set; }
        public string PAMIAMTokenUrl { get; set; }
        public string pamDeliveryConstructsUrl { get; set; }
        public string DemographicsUser { get; set; }
        public string DemographicsPass { get; set; }
        public string myWizardFortressUrl { get; set; }
        public string PublishModelEntityUID { get; set; }
        public string TaskentityUID { get; set; }
        public string AccessRoleNames { get; set; }
        public string UploadFilePath { get; set; }
        public string SavedModels { get; set; }
        public string MarketPlaceFiles { get; set; }
        public string AppData { get; set; }
        public string DataSetPath { get; set; }
        public string TS_TurnOn { get; set; }
        public string aesKey { get; set; }
        public string aesVector { get; set; }
        public bool IsAESKeyVault { get; set; }
        public string SMTPServer { get; set; }
        public string PortNumber { get; set; }
        public string Recipient { get; set; }
        public string EmailSubject { get; set; }
        public string Insta_ModelsForTrainning { get; set; }
        public string VDSRawAPIUrl { get; set; }
        public string UserNamePAD { get; set; }
        public string PasswordPAD { get; set; }
        public string vdsTokenUrlPAD { get; set; }
        public string UserNamePAM { get; set; }
        public string PasswordPAM { get; set; }
        public string vdsTokenUrlPAM { get; set; }
        public string token_Url { get; set; }
        public string Grant_Type { get; set; }
        public string clientId { get; set; }
        public string clientSecret { get; set; }
        public string resourceId { get; set; }
        public string userName_Azure { get; set; }
        public string password_Azure { get; set; }
        public string AppServiceUID_Azure { get; set; }
        public string myWizardAPIUrl { get; set; }
        public string myWizardFortressUrl_Azure { get; set; }
        public string scopeStatus { get; set; }
        public string TimeInterval { get; set; }
        public string RequestNew { get; set; }
        public string RequestInProgress { get; set; }
        public string PythonExe { get; set; }
        public string PythonPy { get; set; }
        public string PythonBaseURI { get; set; }
        public string ingrainDomain { get; set; }
        public string Source { get; set; }
        public string Insta_AutoModels { get; set; }
        public string ForeCastModel { get; set; }
        public string PublishURL { get; set; }

        public bool DeleteModel { get; set; }
        public string AICoreFilespath { get; set; }
        public string AICorePythonURL { get; set; }
        public string AICorePredictionURL { get; set; }
        public string PredictionURL { get; set; }
        public string Grant_Type_VDS { get; set; }
        public string clientId_VDS { get; set; }
        public string clientSecret_VDS { get; set; }
        public string resourceId_VDS { get; set; }
        public string scopeStatus_VDS { get; set; }       
        public string ClusteringPythonURL { get; set; }
        public string MonteCarloPythonURL { get; set; }
        public string ClusteringFilespath { get; set; }
        public string token_Url_VDS { get; set; }
        public string CertificateEmailSub { get; set; }
        public string market_ResourceId { get; set; }
        public string market_ClientId { get; set; }
        public string market_ClientSecret { get; set; }
        public string GetVdsDataURL { get; set; }
        public string GetVdsPAMDataURL { get; set; }
        public string FileEncryptionKey { get; set; }
        public bool EncryptUploadedFiles { get; set; } //true or false
        public bool DBEncryption { get; set; }
        public string resource_ingrain { get; set; }
        public bool isForAllData { get; set; }
        public string clientId_clustering { get; set; }
        public string client_secret_clustering { get; set; }
        public string resource_clustering { get; set; }
        public string VdsURL { get; set; }
        public string pyLogsPath { get; set; }
        public string Environment { get; set; }
        public string EnableFeatureWeights { get; set; }
        public string languageURL { get; set; }
        public int PredictionTimeoutMinutes { get; set; }

        public int PredictionTimeoutMinute { get; set; }
        public int CascadeTargetPercentage { get; set; }
        public int CascadeIDPercentage { get; set; }
        public string Ingrain_TS_OffModels { get; set; }
        public string Ingrain_TS_OnModels { get; set; }
        public string Ingrain_Regresison_OffModels { get; set; }
        public string Ingrain_Regresison_OnModels { get; set; }
        public string Ingrain_Classification_OffModels { get; set; }
        public string Ingrain_Classification_OnModels { get; set; }
        public string Ingrain_MultiClass_OnModels { get; set; }
        public string Ingrain_MultiClass_OffModels { get; set; }
        public int ModelsTrainingTimeLimit { get; set; }
        public string IEConnectionString { get; set; }

        public int NotificationMaxRetryCount { get; set; }
        public string IngrainAPIUrl { get; set; }

        public bool EnableTraining { get; set; }
        public bool EnablePrediction { get; set; }

        public string DevtestAesKey { get; set; }

        public string AssetUsageUrl { get; set; }
        public bool IsAssetTrackingRequired { get; set; }
        public string CorrelationUId { get; set; }
        public string DevtestAesVector { get; set; }

        public bool IsFlaskCall { get; set; }
        public string FlaskAPIBaseURL { get; set; }
        public string FlaskApiPath { get; set; }
        public int CustomQueryLimit { get; set; }
        public string PhoenixConnectionString { get; set; }
        public string PerformCascadeOperationsInVDS { get; set; }

        public string ApplicationName { get; set; }
        public string PhoenixDBName { get; set; }
        public int SSAIRecsLimit { get; set; }
        public int AIRecsLimit { get; set; }
        public bool IsTestDrive { get; set; }

        #region PAM ENVIRONMENT
        public string ATRDataFabricURL { get; set; }
        public string PAMTokenUrl { get; set; }
        public string PAMClientUID { get; set; }
        public string PAMClientName { get; set; }
        public string PamTokenValidationURL { get; set; }
        public string tokenValidation_URL { get; set; }
        public string TokenURLVDS { get; set; }
        #endregion

        #region SAASConfig
        public string clientid_saas { get; set; }
        public string clientsecret_saas { get; set; }
        public string tokenendpoint_saas { get; set; }
        public string scopes_saas { get; set; }
        public string scope_saas { get; set; }
        public string isazureadenabled { get; set; }
        public string azureclientid { get; set; }
        public string azureclientsecret { get; set; }
        public string azureresource { get; set; }
        public string azuretokenendpoint { get; set; }
        public string SAASProvisionResponseApi { get; set; }
        public string SAASProvisionErrorResponseApi { get; set; }
        public string AESKey_saas { get; set; }
        public string EditProposalUrl { get; set; }
        public string SAASProvisionedUrl { get; set; }
        public string ATRProvisionedUrl { get; set; }
        public string AESVector_saas { get; set; }

        #endregion

        public string SaaSDSAPIPath { get; set; }
        public string SaaSDCAPIPath { get; set; }
        public string ATRTokenPath { get; set; }
        public bool IsSaaSPlatform { get; set; }
        public string PerformCascadeOperationsInVDSFDS { get; set; }
        public string VDSPredictionNotificationUrl { get; set; }
        public List<string> IEGenricVDSUsecases { get; set; }
        public string VDSIEGenericNotificationUrl { get; set; }
        #region Anomaly Detection
        public string AnomalyDetectionCS { get; set; }
        public string AnomalyDetectionRegressionModels { get; set; }
        public string AnomalyDetectionTimeseriesModels { get; set; }
        #endregion Anomaly Detection

    }
}
