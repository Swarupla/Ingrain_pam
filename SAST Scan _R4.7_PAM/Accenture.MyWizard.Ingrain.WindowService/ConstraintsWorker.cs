using System;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Accenture.MyWizard.Ingrain.WindowService.Services;
using WINSERVICEMODELS = Accenture.MyWizard.Ingrain.WindowService.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
using Accenture.MyWizard.Ingrain.WindowService.Services.ConstraintsHelperMethods;
using MongoDB.Driver;
using DATAMODELS = Accenture.MyWizard.Ingrain.DataModels.Models;
using AIDataModels = Accenture.MyWizard.Ingrain.DataModels.AICore;
using MongoDB.Bson;
using System.Collections.Generic;

namespace Accenture.MyWizard.Ingrain.WindowService
{
    public class ConstraintsWorker : IHostedService, IDisposable
    {
        private System.Timers.Timer _SSAITrainingtimer;
        private System.Timers.Timer _SSAIPredTimer;
        private System.Timers.Timer _SSAIRetraintimer;

        private System.Timers.Timer _AItimer;
        private System.Timers.Timer _AIPredTimer;
        private System.Timers.Timer _AIRetraintimer;

        private System.Timers.Timer _PredictionIterationtimer;
        private System.Timers.Timer _PredictionCRtimer;

        private readonly WINSERVICEMODELS.AppSettings appSettings = null;
        private ConstraintsSchedularService _ConstraintsSchedularService = null;
        DBConnection _DBConnection = null;

        public ConstraintsWorker()
        {
            _SSAITrainingtimer = new System.Timers.Timer();
            _SSAIPredTimer = new System.Timers.Timer();
            _SSAIRetraintimer = new System.Timers.Timer();

            _AItimer = new System.Timers.Timer();
            _AIPredTimer = new System.Timers.Timer();
            _AIRetraintimer = new System.Timers.Timer();

            _PredictionIterationtimer = new System.Timers.Timer();
            _PredictionCRtimer = new System.Timers.Timer();

            appSettings = AppSettingsJson.GetAppSettings().GetSection("AppSettings").Get<WINSERVICEMODELS.AppSettings>();
            _ConstraintsSchedularService = new ConstraintsSchedularService();
            _DBConnection = new DBConnection();
        }


        public Task StartAsync(CancellationToken cancellationToken)
        {

            #region SSAI Calls

            _SSAITrainingtimer.Elapsed += new ElapsedEventHandler(OnElapsedSSAIModelTraining);
            _SSAITrainingtimer.Interval = Convert.ToDouble(3600000);
            _SSAITrainingtimer.Enabled = true;
            _SSAITrainingtimer.AutoReset = true;

            _SSAIPredTimer.Elapsed += new ElapsedEventHandler(OnElapsedSSAIPrediction);
            _SSAIPredTimer.Interval = Convert.ToDouble(3600000);
            _SSAIPredTimer.Enabled = true;
            _SSAIPredTimer.AutoReset = true;

            _SSAIRetraintimer.Elapsed += new ElapsedEventHandler(OnElapsedSSAIReTraining);
            _SSAIRetraintimer.Interval = Convert.ToDouble(3600000);
            _SSAIRetraintimer.Enabled = true;
            _SSAIRetraintimer.AutoReset = true;

            #endregion SSAI Calls

            #region AI Calls

            _AItimer.Elapsed += new ElapsedEventHandler(OnElapsedAIModelTraining);
            _AItimer.Interval = Convert.ToDouble(3600000);
            _AItimer.Enabled = true;
            _AItimer.AutoReset = true;


            _AIPredTimer.Elapsed += new ElapsedEventHandler(OnElapsedAIPrediction);
            _AIPredTimer.Interval = Convert.ToDouble(3600000);
            _AIPredTimer.Enabled = true;
            _AIPredTimer.AutoReset = true;

            _AIRetraintimer.Elapsed += new ElapsedEventHandler(OnElapsedAIReTraining);
            _AIRetraintimer.Interval = Convert.ToDouble(3600000);
            _AIRetraintimer.Enabled = true;
            _AIRetraintimer.AutoReset = true;

            #endregion AI Calls

            _PredictionIterationtimer.Elapsed += new ElapsedEventHandler(iterationElapsedCRPredictionTime);
            _PredictionIterationtimer.Interval = Convert.ToDouble(3600000);
            _PredictionIterationtimer.Enabled = true;
            _PredictionIterationtimer.AutoReset = true;

            _PredictionCRtimer.Elapsed += new ElapsedEventHandler(iterationElapsedCRPredictionTime);
            _PredictionCRtimer.Interval = Convert.ToDouble(3600000);
            _PredictionCRtimer.Enabled = true;
            _PredictionCRtimer.AutoReset = true;

            return Task.CompletedTask;
        }

        #region SSAI
        private void OnElapsedSSAIModelTraining(object source, ElapsedEventArgs e)
        {
            StartModelTraining();
        }

        private void OnElapsedSSAIPrediction(object source, ElapsedEventArgs e)
        {
            StartModelPrediction();
        }

        private void OnElapsedSSAIReTraining(object source, ElapsedEventArgs e)
        {
            _ConstraintsSchedularService.SSAIReTrain();
        }

        #region Triggered methods
        public void StartModelTraining()
        {
            try
            {
                var filter = Builders<DATAMODELS.DeployModelsDto>.Filter.Eq(CONSTANTS.IsOffline, true)
                            & Builders<DATAMODELS.DeployModelsDto>.Filter.Eq(CONSTANTS.IsModelTemplate, true)
                            & (Builders<DATAMODELS.DeployModelsDto>.Filter.Where(x => x.SourceName != CONSTANTS.file))
                            & Builders<DATAMODELS.DeployModelsDto>.Filter.Where(x => x.Status == CONSTANTS.Deployed);

                var deployedOfflineModels = _DBConnection.GetCollectionData<DATAMODELS.DeployModelsDto>(filter, CONSTANTS.SSAIDeployedModels);
                foreach (var item in deployedOfflineModels)
                {
                    if (!item.LinkedApps[0].Equals("Ingrain"))
                    {
                        DATAMODELS.ModelRequestStatus oResponse = _DBConnection.GetModelStatus(item.ModelName, item.CorrelationId);

                        // Pick model for training , either when triggered for the First time 
                        // or the Previous training Request is Completed
                        if (oResponse == null || oResponse.TrainingStatus == string.Empty || oResponse.TrainingStatus == CONSTANTS.Completed || oResponse.TrainingStatus == CONSTANTS.WorkerServiceStopped)
                        {
                            DateTime offlineTrainingRunDate = string.IsNullOrEmpty(item.offlineTrainingRunDate) ? DateTime.UtcNow.AddDays(-1) : DateTime.Parse(item.offlineTrainingRunDate);
                            DateTime curDate = DateTime.UtcNow;
                            int days = (int)(curDate - offlineTrainingRunDate).TotalDays;
                            if (days >= item.TrainingFrequencyInDays)
                            {
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(ConstraintsWorker), nameof(StartModelTraining), "SSAI Model - '" + item.ModelName + "' - Training Started", "Ingrain Window Service", "", item.ClientUId, item.DeliveryConstructUID, CONSTANTS.CustomConstraintsLog);

                                //Set TrainingStatus = InProgress
                                _DBConnection.SetModelStatus(item.ModelName, item.CorrelationId, CONSTANTS.TrainingConstraint, CONSTANTS.InProgress);
                                _ConstraintsSchedularService.StartModelTraining(item);

                                //Mark the Model Training Status as Complete
                                _DBConnection.SetModelStatus(item.ModelName, item.CorrelationId, CONSTANTS.TrainingConstraint, CONSTANTS.Completed);
                            }
                        }
                    }
                }
            }

            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ConstraintsWorker), nameof(StartModelTraining), ex.Message + "- STACKTRACE - " + ex.StackTrace + ex.InnerException, ex, "Ingrain Window Service", "", "", "", CONSTANTS.CustomConstraintsLog);
            }
        }

        public void StartModelPrediction()
        {
            try
            {
                //fetching the list of Model Templates
                var filter = Builders<DATAMODELS.DeployModelsDto>.Filter.Eq(CONSTANTS.IsOffline, true)
                            & Builders<DATAMODELS.DeployModelsDto>.Filter.Eq(CONSTANTS.IsModelTemplate, true)
                            & (Builders<DATAMODELS.DeployModelsDto>.Filter.Where(x => x.SourceName != CONSTANTS.file))
                            & Builders<DATAMODELS.DeployModelsDto>.Filter.Where(x => x.Status == CONSTANTS.Deployed);

                var deployedOfflineModels = _DBConnection.GetCollectionData<DATAMODELS.DeployModelsDto>(filter, CONSTANTS.SSAIDeployedModels);

                foreach (var item in deployedOfflineModels)
                {
                    if (!item.LinkedApps[0].Equals("Ingrain"))
                    {
                        DATAMODELS.ModelRequestStatus oResponse = _DBConnection.GetModelStatus(item.ModelName, item.CorrelationId);

                        // Pick model for training , either when triggered for the First time 
                        // or the Previous training Request is Completed
                        if (oResponse == null || oResponse.PredictionStatus == string.Empty || oResponse.PredictionStatus == CONSTANTS.Completed || oResponse.PredictionStatus == CONSTANTS.WorkerServiceStopped)
                        {
                            DateTime offlineRunDate = string.IsNullOrEmpty(item.offlinePredRunDate) ? DateTime.UtcNow.AddDays(-1) : DateTime.Parse(item.offlinePredRunDate);
                            DateTime curDate = DateTime.UtcNow;
                            int days = (int)(curDate - offlineRunDate).TotalDays;
                            if (days >= item.PredictionFrequencyInDays && item.PredictionFrequencyInDays > 0)
                            {
                                //Set Prediction Status = InProgress
                                _DBConnection.SetModelStatus(item.ModelName, item.CorrelationId, CONSTANTS.PredictionConstraint, CONSTANTS.InProgress);

                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(ConstraintsWorker), nameof(StartModelPrediction), "SSAI Model '" + item.ModelName + "' - Prediction Process Initiated", "Ingrain Window Service", "", item.ClientUId, item.DeliveryConstructUID, CONSTANTS.CustomConstraintsLog);
                                _ConstraintsSchedularService.StartModelPrediction(item);

                                //Mark the Model Prediction Status as Complete
                                _DBConnection.SetModelStatus(item.ModelName, item.CorrelationId, CONSTANTS.PredictionConstraint, CONSTANTS.Completed);
                            }
                        }
                    }
                }
            }

            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ConstraintsWorker), nameof(StartModelPrediction), ex.Message + "- STACKTRACE - " + ex.StackTrace + ex.InnerException, ex, "Ingrain Window Service", "", "", "", CONSTANTS.CustomConstraintsLog);
            }
        }

        #endregion Triggered methods


        #endregion SSAI

        #region AI
        private void OnElapsedAIModelTraining(object source, ElapsedEventArgs e)
        {
            StartAIModelTraining();
        }

        private void OnElapsedAIPrediction(object source, ElapsedEventArgs e)
        {
            StartAIModelPrediction();
        }

        private void OnElapsedAIReTraining(object source, ElapsedEventArgs e)
        {
            _ConstraintsSchedularService.AIServicesReTrain();
        }

        #region Triggered methods

        public void StartAIModelTraining()
        {
            try
            {
                var filter = Builders<AIDataModels.UseCase.UsecaseDetails>.Filter.Eq(CONSTANTS.IsOffline, true)
                              & (Builders<AIDataModels.UseCase.UsecaseDetails>.Filter.Where(x => x.SourceName.ToLower() != CONSTANTS.file));

                var deployedOfflineModels = _DBConnection.GetCollectionData<AIDataModels.UseCase.UsecaseDetails>(filter, CONSTANTS.AISavedUsecases);
                foreach (var item in deployedOfflineModels)
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(ConstraintsWorker), nameof(StartAIModelTraining), "AI Model - '" + item.ModelName + "' ; CorrelationID : " + item.CorrelationId, "Ingrain Window Service", "", "", "", CONSTANTS.CustomConstraintsLog);
                    DateTime offlineTrainingRunDate = string.IsNullOrEmpty(item.offlineTrainingRunDate) ? DateTime.UtcNow.AddDays(-1) : DateTime.Parse(item.offlineTrainingRunDate);
                    DateTime curDate = DateTime.UtcNow;
                    int days = (int)(curDate - offlineTrainingRunDate).TotalDays;
                    if (days >= item.TrainingFrequencyInDays)
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(ConstraintsWorker), nameof(StartAIModelTraining), "AI Model - '" + item.ModelName + "' - Training Started", "Ingrain Window Service", "", "", "", CONSTANTS.CustomConstraintsLog);
                        _ConstraintsSchedularService.StartAIModelTraining(item);
                    }
                    else
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(ConstraintsWorker), nameof(StartAIModelTraining), "AI Model - '" + item.ModelName + "' - Training Conditions Not Met", "Ingrain Window Service", "", "", "", CONSTANTS.CustomConstraintsLog);
                    }
                }
            }

            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ConstraintsWorker), nameof(StartAIModelTraining), ex.Message + "- STACKTRACE - " + ex.StackTrace + ex.InnerException, ex, "Ingrain Window Service", "", "", "", CONSTANTS.CustomConstraintsLog);
            }
        }

        public void StartAIModelPrediction()
        {
            try
            {
                var filter = Builders<AIDataModels.UseCase.UsecaseDetails>.Filter.Eq(CONSTANTS.IsOffline, true)
                           & (Builders<AIDataModels.UseCase.UsecaseDetails>.Filter.Where(x => x.SourceName.ToLower() != CONSTANTS.file));

                var aIOfflineModels = _DBConnection.GetCollectionData<AIDataModels.UseCase.UsecaseDetails>(filter, CONSTANTS.AISavedUsecases);

                foreach (var item in aIOfflineModels)
                {
                    DateTime offlineRunDate = string.IsNullOrEmpty(item.offlineTrainingRunDate) ? DateTime.UtcNow.AddDays(-1) : DateTime.Parse(item.offlineTrainingRunDate);
                    DateTime curDate = DateTime.UtcNow;
                    int days = (int)(curDate - offlineRunDate).TotalDays;
                    if (days >= item.PredictionFrequencyInDays && item.PredictionFrequencyInDays > 0)
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(ConstraintsWorker), nameof(StartAIModelPrediction), "AI Model - '" + item.ModelName + "' - Prediction Initiated", "Ingrain Window Service", "", "", "", CONSTANTS.CustomConstraintsLog);
                        _ConstraintsSchedularService.StartAIModelPrediction(item);
                    }
                }
            }

            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ConstraintsWorker), nameof(StartAIModelPrediction), ex.Message + "- STACKTRACE - " + ex.StackTrace + ex.InnerException, ex, "Ingrain Window Service", "", "", "", CONSTANTS.CustomConstraintsLog);
            }
        }

        #endregion Triggered methods

        #endregion AI

        #region Prediction Methods 

        /// <summary>
        /// ActiveRelease Prediction Constraint
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void iterationElapsedTime(object source, ElapsedEventArgs e)
        {
            StartNewIterationPredictions();

        }

        public void StartNewIterationPredictions()
        {
            try
            {
                if (appSettings.EnableENSPredictions == "True")
                {
                    //_ConstraintsSchedularService.UpdateCredentialsInAppIntegration();
                    _ConstraintsSchedularService.UpdateNewIterationRecommendationsInPhoenix();
                }

            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ConstraintsWorker), nameof(StartNewIterationPredictions), ex.Message + "- STACKTRACE - " + ex.StackTrace + ex.InnerException, ex, "Ingrain Window Service", "", "", "", CONSTANTS.CustomConstraintsLog);
            }
        }

        /// <summary>
        /// CR Prediction Constraint
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void iterationElapsedCRPredictionTime(object source, ElapsedEventArgs e)
        {
            StartENSCRPredictions();

        }

        public void StartENSCRPredictions()
        {
            try
            {
                if (appSettings.EnableENSPredictions == "True")
                {
                    //_ConstraintsSchedularService.UpdateCredentialsInAppIntegration();
                    _ConstraintsSchedularService.StartENSCRPredictions();
                }

            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ConstraintsWorker), nameof(StartENSCRPredictions), ex.Message + "- STACKTRACE - " + ex.StackTrace + ex.InnerException, ex, "Ingrain Window Service", "", "", "", CONSTANTS.CustomConstraintsLog);
            }
        }

        #endregion Prediction Methods End

        public void Dispose()
        {
            _SSAITrainingtimer?.Dispose();
            _SSAIPredTimer?.Dispose();
            _SSAIRetraintimer?.Dispose();

            _AItimer?.Dispose();
            _AIPredTimer?.Dispose();
            _AIRetraintimer?.Dispose();

            _PredictionIterationtimer?.Dispose();
            _PredictionCRtimer?.Dispose();

        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            List<DATAMODELS.ModelRequestStatus> oTrainingStatusList = _DBConnection.GetModelStatusList();

            if (oTrainingStatusList != null && oTrainingStatusList.Count > 0)
            {
                foreach (DATAMODELS.ModelRequestStatus oRequest in oTrainingStatusList)
                {
                    _DBConnection.SetModelStatus(oRequest.ModelTemplateName, oRequest.CorrelationId,CONSTANTS.TrainingConstraint, CONSTANTS.WorkerServiceStopped);
                    _DBConnection.SetModelStatus(oRequest.ModelTemplateName, oRequest.CorrelationId, CONSTANTS.PredictionConstraint, CONSTANTS.WorkerServiceStopped);
                    _DBConnection.SetModelStatus(oRequest.ModelTemplateName, oRequest.CorrelationId, CONSTANTS.ReTrainingConstraint, CONSTANTS.WorkerServiceStopped);
                }
            }

            Dispose();
            return Task.CompletedTask;
        }
    }
}
