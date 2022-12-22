using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.WindowService.Models
{
    public class AppSettings
    {
        public string connectionString { get; set; }
        public string certificatePath { get; set; }
        public string certificatePassKey { get; set; }
        public string pythonExe { get; set; }
        public string pythonPy { get; set; }
        public int pythonProgressStatus { get; set; }
        public string sleepTime { get; set; }
        public string timeInterval { get; set; }
        public string AutoRetrainTimeInterval { get; set; }
        public string NotificationTimeInterval { get; set; }

        public string TerminateModelElapsedTimeInterval { get; set; }

        public int NotificationUpdateQueueLimit { get; set; }

        public int TerminateModelTimeInterval { get; set; }

        public string requestNew { get; set; }
        public string requestInProgress { get; set; }
        public string Insta_AutoModels { get; set; }
        public string Source { get; set; }
        public string TimePeriod { get; set; }
        public string TimeToRun { get; set; }
        public string InstaAutoDays { get; set; }
        public string foreCastModel { get; set; }
        public string publishURL { get; set; }
        public string VDSLink { get; set; }
        public string UsageTimeInterval { get; set; }
        public string tokenAPIUrl { get; set; }
        public string AssetUsageUrl { get; set; }
        public string AssetdataArchieveDays { get; set; }
        public string CorrelationUId { get; set; }
        public string AppServiceUId { get; set; }
        public string aesKey { get; set; }
        public string aesVector { get; set; }
        public bool IsAESKeyVault { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public string authProvider { get; set; }
        public string resourceId { get; set; }
        public string token_Url { get; set; }
        public string Grant_Type { get; set; }
        public string clientId { get; set; }
        public string clientSecret { get; set; }
        public string scopeStatus { get; set; }
        public string PredictionQueueLimit { get; set; }
        public string TrainingQueueLimit { get; set; }
        public string AITrainingQueueLimit { get; set; }
        public string PredictionTimeoutMinutes { get; set; }

        public string myWizardAPIUrl { get; set; }
        public string FortressAppServiceUId { get; set; }
        public string UserEmail { get; set; }
        public string ClientUID { get; set; }
        public string TaskCodes { get; set; }
        public int TaskCount { get; set; }
        public string TaskIntervalInDays { get; set; }
        public string DevOpsProductId { get; set; }
        public string Developerpred_UsecaseIds { get; set; }
        public string CRENSfieldvalues { get; set; }

        public string IngrainAPIUrl { get; set; }
        public string AIServicePythonUrl { get; set; }
        public string AppServiceUID_Azure { get; set; }
        public string IsAssetTrackingRequired { get; set; }
        public int AssetTrackingThreadtime { get; set; }

        public int NotificationRetryFrequencyinMnts { get; set; }
        public int NotificationMaxRetryCount { get; set; }

        public string ModelMonitorAutoDays { get; set; }
        public string MonitorTimeToRun { get; set; }
        public string MonitorTimePeriod { get; set; }
        public string APPAutoTimeToRun { get; set; }
        public string APPAutoTimePeriod { get; set; }
        public string AIAutoTimeToRun { get; set; }
        public string AIAutoTimePeriod { get; set; }
        public string AIAutoTrainDays { get; set; }

        //IA usecase Details
        public string EnableENSPredictions { get; set; }
        public string IA_AppServiceUId { get; set; }
        public string IA_ApplicationId { get; set; }
        public string IA_ServiceId { get; set; }
        public List<string> IA_SSAIUseCaseIds { get; set; }
        public List<string> IA_AIUseCaseIds { get; set; }
        public string IterationEntityUId { get; set; }
        public string CREntityUId { get; set; }
        public List<string> AIAutoTestCorIds { get; set; }
        public List<string> AIAutoClusterCorIds { get; set; }
        public string ManualTrigger { get; set; }

        public string AIManualTrigger { get; set; }
        public string SPETemplateUseCaseID { get; set; }
        public string[] SPAAutoTestCorIds { get; set; }
        public bool IsRetrainErrorModelsWithService { get; set; }
        public string RetrainErrorModelsTime_TimeInterval { get; set; }
        public int NoofModelToRetrain { get; set; }
        public int ModelsTrainingTimeLimit { get; set; }
        public int WSRetrainModelDays { get; set; }
        public string IEConnectionString { get; set; }

        public string IEPythonURL { get; set; }
        public string ClusteringPythonURL { get; set; }
        public string ManualSPAAutoTrain { get; set; }
        public bool EnableAutoReTrain { get; set; }
        public int ProcessTimeout { get; set; }
        public int ArchiveTimeInterval { get; set; }

        public bool isForAllData { get; set; }

        public string DevtestAesKey { get; set; }
        public string DevtestAesVector { get; set; }
        public string Environment { get; set; }
        public string UserNamePAM { get; set; }
        public string PasswordPAM { get; set; }
        public string PAMTokenUrl { get; set; }

        public bool IsDockerPlatform { get; set; }

        public string RetrainTimeInterval { get; set; }

        public bool IsRetrainModelEnabled { get; set; }

        #region Simulation SPI
        public bool SPPSimulation { get; set; }
        public string SPPIntervalForGenericAPICall { get; set; }
        public int SPPRequestLimit { get; set; }
        public string SPPGenericAPIUrl { get; set; }
        public string SPPPredictionCorrelationId { get; set; }
        public string SPPPredictionCallBackUrl { get; set; }
        #endregion Simulation SPI

        public string IngrainClientId { get; set; }
        public string IngrainResourceId { get; set; }
        public string IngrainClientSecret { get; set; }

        public string AIScrumAutoTrainDays { get; set; }

        public string IASSAIRetrainFrequency { get; set; }

        public int RequestBatchLimit { get; set; }

        #region PAM
        public string ServerMemoryUsageLimit { get; set; }
        public string TokenURLVDS { get; set; }
        #endregion

        #region ArchivalPurging
        public string archivalDays { get; set; }
        public string ArchivalPurgingIntervalInDays { get; set; }
        #endregion
        public string RetrainTriggerTime { get; set; }

        public string RetrainTriggerRun { get; set; }

        #region SaaS/ATR


        public string MyWizardSaaSUrl { get; set; }

        public string saasResourceId { get; set; }

        public string HistoricPullIntervalMinutes { get; set; }

        public string DeltaPullIntervalMinutes { get; set; }

        public bool IsSaaSPlatform { get; set; }

        public bool isProd { get; set; }

        public int HistoricPullThreadLimit { get; set; }

        public int PullInterval { get; set; }

        public string DaysInterval { get; set; }

        public string ATRSource { get; set; }

        public bool IsTokenGenerationTemporary { get; set; }

        public string phoenix_credentials_granttype { get; set; }

        public string phoenix_credentials_clientid { get; set; }

        public string phoenix_credentials_clientsecret { get; set; }
        public string phoenix_credentials_resource { get; set; }
        public string phoenix_credentials_granttypeforusertoken { get; set; }
        public string phoenix_username { get; set; }
        public string phoenix_password { get; set; }
        public string phoenix_credentials_scope { get; set; }
        public string phoenix_credentials_usertokenendpointurl { get; set; }

        public string EndToEndUId { get; set; }

        public string SHIFT_START_TIME { get; set; }

        public string SHIFT_END_TIME { get; set; }

        public string NO_SUPPORT_DAYS { get; set; }

        public bool IsClientDeployment { get; set; }

        public int DeltaPullThreadLimit { get; set; }

        public string SaaS_clientId { get; set; }

        public string SaaS_clientSecret { get; set; }

        #endregion
        public List<string> IEGenricVDSUsecases { get; set; }
        public string VDSIEGenericNotificationUrl { get; set; }
        public string VdsURL { get; set; }
        #region Anomaly Detection
        public string AnomalyDetectionCS { get; set; }
        #endregion Anomaly Detection

    }
}
