using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using System.Timers;
using System.Collections.Generic;
using System.Text;
using Accenture.MyWizard.Ingrain.WindowService.Interfaces;
using Accenture.MyWizard.Ingrain.WindowService.Services;
using WINSERVICEMODELS = Accenture.MyWizard.Ingrain.WindowService.Models;
using DATAMODELS = Accenture.MyWizard.Ingrain.DataModels.Models;
using Microsoft.Extensions.Configuration;

namespace Accenture.MyWizard.Ingrain.WindowService
{
    public class TrainModelsWorker : IHostedService, IDisposable
    {
        private System.Timers.Timer _timer;
        private System.Timers.Timer _notfTimer;
        private System.Timers.Timer _ensPredictionTimer;
        private System.Timers.Timer _ensPredictionCRTimer;

        private System.Timers.Timer _iterationTimer;
        private ITrainGenericModels _trainGenericModels = null;
        private readonly WINSERVICEMODELS.AppSettings appSettings = null;
        private PredictionSchedulerService _predictionSchedulerService = null;
        private PushNotificationService _notificationService = null;

        public TrainModelsWorker()
        {
            _timer = new System.Timers.Timer();
            _notfTimer = new System.Timers.Timer();
            _ensPredictionTimer = new System.Timers.Timer();
            _ensPredictionCRTimer = new System.Timers.Timer();
            _iterationTimer = new System.Timers.Timer();
            _trainGenericModels = new TrainGenericModels();
            appSettings = AppSettingsJson.GetAppSettings().GetSection("AppSettings").Get<WINSERVICEMODELS.AppSettings>();
            _predictionSchedulerService = new PredictionSchedulerService();
            _notificationService = new PushNotificationService();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer.Elapsed += new ElapsedEventHandler(OnElapsedTime);
            _timer.Interval = Convert.ToDouble(100000);
            _timer.Enabled = true;
            _timer.AutoReset = false;


            _ensPredictionTimer.Elapsed += new ElapsedEventHandler(ensElapsedTime);
            _ensPredictionTimer.Interval = Convert.ToDouble(30000);
            _ensPredictionTimer.Enabled = true;
            _ensPredictionTimer.AutoReset = false;

            _ensPredictionCRTimer.Elapsed += new ElapsedEventHandler(ensCRElapsedTime);
            _ensPredictionCRTimer.Interval = Convert.ToDouble(900000);//15 mins
            _ensPredictionCRTimer.Enabled = true;
            _ensPredictionCRTimer.AutoReset = false;


            _notfTimer.Elapsed += new ElapsedEventHandler(notfTimeElapsed);
            _notfTimer.Interval = Convert.ToDouble(1000);
            _notfTimer.Enabled = true;
            _notfTimer.AutoReset = false;

            //_iterationTimer.Elapsed += new ElapsedEventHandler(iterationElapsedTime);
            //_iterationTimer.Interval = Convert.ToDouble(10000);
            //_iterationTimer.Enabled = true;
            //_iterationTimer.AutoReset = false;


            return Task.CompletedTask;
        }
        private void OnElapsedTime(object source, ElapsedEventArgs e)
        {
            StartTraining();
        }
        private void ensElapsedTime(object source, ElapsedEventArgs e)
        {
            StartENSPredictions();

        }
        private void ensCRElapsedTime(object source, ElapsedEventArgs e)
        {
            StartENSCRPredictions();

        }

        //private void iterationElapsedTime(object source, ElapsedEventArgs e)
        //{
        //    StartNewIterationPredictions();

        //}
        private void notfTimeElapsed(object source, ElapsedEventArgs e)
        {
            //file to execute
            SendNotifications();
        }
        public void StartTraining()
        {
            try
            {
                List<WINSERVICEMODELS.AutoTraintask> taskList = _trainGenericModels.CheckAutoTraintaskStatus();
                int taskInterval = !string.IsNullOrEmpty(appSettings.TaskIntervalInDays) ? Convert.ToInt32(appSettings.TaskIntervalInDays) : 1;

                if (taskList.Count == appSettings.TaskCount)
                {
                    foreach (var task in taskList)
                    {
                        switch (task.TaskCode)
                        {
                            case "FETCHCLIENT":
                                {
                                    DateTime lastExetime = string.IsNullOrEmpty(task.LastExecutedDate) ? DateTime.UtcNow.AddDays(-1) : DateTime.Parse(task.LastExecutedDate);
                                    DateTime curDate = DateTime.UtcNow;
                                    int days = (int)(curDate - lastExetime).TotalDays;
                                    if (days >= taskInterval && task.IsActive)
                                    {
                                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(TrainModelsWorker), nameof(StartTraining), "Executing -" + task.TaskCode, "", "", "", "");
                                        _trainGenericModels.FetchListOfClients();
                                    }
                                }
                                break;
                            case "FETCHDC":
                                {
                                    DateTime lastExetime = string.IsNullOrEmpty(task.LastExecutedDate) ? DateTime.UtcNow.AddDays(-1) : DateTime.Parse(task.LastExecutedDate);
                                    DateTime curDate = DateTime.UtcNow;
                                    int days = (int)(curDate - lastExetime).TotalDays;
                                    if (days >= taskInterval && task.IsActive)
                                    {
                                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(TrainModelsWorker), nameof(StartTraining), "Executing -" + task.TaskCode, "", "", "", "");
                                        _trainGenericModels.FetchListofDeliveryConstructs();
                                    }
                                }

                                break;
                            case "ADDPRODUCTSTODC":
                                {
                                    DateTime lastExetime = string.IsNullOrEmpty(task.LastExecutedDate) ? DateTime.UtcNow.AddDays(-1) : DateTime.Parse(task.LastExecutedDate);
                                    DateTime curDate = DateTime.UtcNow;
                                    int days = (int)(curDate - lastExetime).TotalDays;
                                    if (days >= taskInterval && task.IsActive)
                                    {
                                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(TrainModelsWorker), nameof(StartTraining), "Executing -" + task.TaskCode, "", "", "", "");
                                        _trainGenericModels.UpdateProductConfigStatus();
                                    }

                                }

                                break;
                            case "TRAINMODEL":
                                {
                                    DateTime lastExetime = string.IsNullOrEmpty(task.LastExecutedDate) ? DateTime.UtcNow.AddDays(-1) : DateTime.Parse(task.LastExecutedDate);
                                    DateTime curDate = DateTime.UtcNow;
                                    int days = (int)(curDate - lastExetime).TotalDays;
                                    if (days >= taskInterval && task.IsActive)
                                    {
                                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(TrainModelsWorker), nameof(StartTraining), "Executing -" + task.TaskCode, "", "", "", "");
                                        _trainGenericModels.TrainModels();
                                    }
                                }
                                break;
                            case "IATRAININGSCHEDULER"://IA service SSAI Model Traning
                                {

                                    DateTime lastExetime = string.IsNullOrEmpty(task.LastExecutedDate) ? DateTime.UtcNow.AddDays(-1) : DateTime.Parse(task.LastExecutedDate);
                                    DateTime currentDate = DateTime.UtcNow;
                                    int dayInterval = (int)(currentDate - lastExetime).TotalDays;
                                    if (dayInterval >= taskInterval && task.IsActive)
                                    {
                                        var deployedOfflineModels = _predictionSchedulerService.CheckIASSAITrainFrequency();
                                        if (task.IsActive)
                                        {
                                            foreach (var item in deployedOfflineModels)
                                            {
                                                DateTime offlineRunDate = string.IsNullOrEmpty(item.offlineRunDate) ? DateTime.UtcNow.AddDays(-1) : DateTime.Parse(item.offlineRunDate);
                                                DateTime curDate = DateTime.UtcNow;
                                                int days = (int)(curDate - offlineRunDate).TotalDays;
                                                if (days >= item.TrainingFrequencyInDays)
                                                {
                                                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(TrainModelsWorker), nameof(StartTraining), "Executing -" + task.TaskCode, "", "", "", "");
                                                    _predictionSchedulerService.StartModelTraining(item);
                                                }
                                            }
                                        }
                                    }

                                }
                                break;

                            case "IASIMILARTRAININGSCHEDULER"://IA IA_AIUseCaseIds Model Traning
                                {
                                    DateTime lastExetime = string.IsNullOrEmpty(task.LastExecutedDate) ? DateTime.UtcNow.AddDays(-1) : DateTime.Parse(task.LastExecutedDate);
                                    DateTime curDate = DateTime.UtcNow;
                                    int days = (int)(curDate - lastExetime).TotalDays;
                                    if (days >= taskInterval && task.IsActive)
                                    {
                                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(TrainModelsWorker), nameof(StartTraining), "Executing -" + task.TaskCode, "", "", "", "");
                                        _predictionSchedulerService.StartIASimilarTraining();
                                    }
                                }
                                break;

                            //case "IASIMILARSCHEDULER":
                            //    {
                            //        DateTime lastExetime = string.IsNullOrEmpty(task.LastExecutedDate) ? DateTime.UtcNow.AddDays(-1) : DateTime.Parse(task.LastExecutedDate);

                            //        var deployedOfflineModels = _predictionSchedulerService.CheckIASSAITrainFrequency();
                            //        if (task.IsActive)
                            //        {
                            //            foreach (var item in deployedOfflineModels)
                            //            {
                            //                DateTime offlineRunDate = string.IsNullOrEmpty(item.offlineRunDate) ? DateTime.UtcNow.AddDays(-1) : DateTime.Parse(item.offlineRunDate);
                            //                DateTime curDate = DateTime.UtcNow;
                            //                int days = (int)(curDate - offlineRunDate).TotalDays;
                            //                if (days >= item.TrainingFrequencyInDays)
                            //                {
                            //                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(TrainModelsWorker), nameof(StartTraining), "Executing -" + task.TaskCode, "", "", "", "");
                            //                  //  _predictionSchedulerService.StartIASimilarTraining(item);
                            //                }
                            //            }
                            //        }
                            //    }
                            //    break;

                            case "IARETRAINSCHEDULER":
                                {
                                    //        DateTime lastExetime = string.IsNullOrEmpty(task.LastExecutedDate) ? DateTime.UtcNow.AddDays(-1) : DateTime.Parse(task.LastExecutedDate);
                                    //        DateTime curDate = DateTime.UtcNow;
                                    //        int hours = (int)(curDate - lastExetime).TotalDays;
                                    //        if (hours >= taskInterval && task.IsActive)
                                    //        {
                                    //            LOGGING.LogManager.Logger.LogProcessInfo(typeof(TrainModelsWorker), nameof(StartTraining), "Executing -" + task.TaskCode, "", "", "", "");
                                    //            _predictionSchedulerService.ReTrainAIServiceModels(Convert.ToInt32(appSettings.AIAutoTrainDays));
                                    //        }
                                }
                                break;
                            case "IAPREDICTIONSCHEDULER":
                                {
                                    //WINSERVICEMODELS.AutoTraintask autoTraintask = _trainGenericModels.GetTaskStatus("IATRAININGSCHEDULER");
                                    //DateTime lastExetime = string.IsNullOrEmpty(autoTraintask.LastExecutedDate) ? DateTime.UtcNow.AddDays(-1) : DateTime.Parse(autoTraintask.LastExecutedDate);
                                    //DateTime curDate = DateTime.UtcNow;
                                    //int hours = (int)(curDate - lastExetime).TotalHours;
                                    //if (string.IsNullOrEmpty(autoTraintask.LastExecutedDate))
                                    //{
                                    //    hours = 0;
                                    //}

                                    //WINSERVICEMODELS.AutoTraintask tsk = _trainGenericModels.GetTaskStatus("IAPREDICTIONSCHEDULER");
                                    //DateTime let = string.IsNullOrEmpty(tsk.LastExecutedDate) ? DateTime.UtcNow.AddDays(-1) : DateTime.Parse(tsk.LastExecutedDate);
                                    //DateTime cD = DateTime.UtcNow;
                                    //int days = (int)(cD - let).TotalDays;

                                    //if (hours >= 1 && days >= 1)
                                    //{
                                    //    LOGGING.LogManager.Logger.LogProcessInfo(typeof(TrainModelsWorker), nameof(StartTraining), "Executing -" + task.TaskCode);
                                    //    _predictionSchedulerService.UpdatePredictionsForHistoricalEntities();
                                    //}
                                }
                                break;
                            case "DATASETSINCREMENTALPULL":
                                {
                                    DateTime lastExetime = string.IsNullOrEmpty(task.LastExecutedDate) ? DateTime.UtcNow.AddDays(-1) : DateTime.Parse(task.LastExecutedDate);
                                    DateTime curDate = DateTime.UtcNow;
                                    int days = (int)(curDate - lastExetime).TotalDays;
                                    if (days >= taskInterval && task.IsActive)
                                    {
                                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(TrainModelsWorker), nameof(StartTraining), "Executing -" + task.TaskCode, "", "", "", "");
                                        _trainGenericModels.UpdateDataSetsWithIncrementalData();
                                    }

                                }
                                break;
                            case "IAITERATIONSPULL": //Pull all the active waterfall releases on daily basis amd insert to pheinx iteration collection	
                                {
                                    DateTime lastExetime = string.IsNullOrEmpty(task.LastExecutedDate) ? DateTime.UtcNow.AddDays(-1) : DateTime.Parse(task.LastExecutedDate);
                                    DateTime curDate = DateTime.UtcNow;
                                    int days = (int)(curDate - lastExetime).TotalDays;
                                    var predictionFrequncy = _predictionSchedulerService.CheckSPIDefectRateFrequency();
                                    if (days >= predictionFrequncy && task.IsActive)
                                    {
                                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(TrainModelsWorker), nameof(StartTraining), "Executing -" + task.TaskCode, "", "", "", "");
                                        _predictionSchedulerService.FetchIterationsList();
                                    }

                                    //    }
                                    //    break;
                                }
                                break;
                        }
                    }

                }
                else
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(TrainModelsWorker), nameof(StartTraining), "Adding Tasks", "", "", "", "");
                    _trainGenericModels.CreateAutoTrainTask();
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(TrainModelsWorker), nameof(StartTraining), ex.Message + "- STACKTRACE - " + ex.StackTrace + ex.InnerException, ex, "", "", "", "");
            }
            //LOGGING.LogManager.Logger.LogProcessInfo(typeof(TrainModelsWorker), nameof(StartTraining), "TIMER RESET-"+DateTime.UtcNow.ToString());
            _timer.Start();
        }

        /*  public void StartNewIterationPredictions()
            {
                try
                {
                    if (appSettings.EnableENSPredictions == "True")
                    {
                        _predictionSchedulerService.UpdateCredentialsInAppIntegration();
                        _predictionSchedulerService.UpdateNewIterationRecommendationsInPhoenix();
                    }
                }
                catch (Exception ex)
                {
                    LOGGING.LogManager.Logger.LogErrorMessage(typeof(TrainModelsWorker), nameof(StartNewIterationPredictions), ex.Message + "- STACKTRACE - " + ex.StackTrace + ex.InnerException, ex, "", "", "", "");
                }

                //_iterationTimer.Start();
            } */

        /*public void StartENSPredictions()
        {
            try
            {
                if (appSettings.EnableENSPredictions == "True")
                {
                    _predictionSchedulerService.RemoveAIServiceModels();
                    _predictionSchedulerService.ClearENSNotifications();
                    _predictionSchedulerService.TriggerIterationENSPredictions();
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(TrainModelsWorker), nameof(StartNewIterationPredictions), ex.Message + "- STACKTRACE - " + ex.StackTrace + ex.InnerException, ex, "", "", "", "");
            }
            _iterationTimer.Start();
        } */

        //For Errored and already predicted models iterations
        public void StartENSPredictions()
        {
            try
            {
                _predictionSchedulerService.RemoveAIServiceModels();
                _predictionSchedulerService.ClearENSNotifications();

                if (appSettings.EnableENSPredictions == "True")
                {
                    _predictionSchedulerService.UpdateCredentialsInAppIntegration();
                    _predictionSchedulerService.UpdateIterationRecommendationsInPhoenix();
                    //_predictionSchedulerService.TriggerIterationENSPredictions();
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(TrainModelsWorker), nameof(StartENSPredictions), ex.Message + "- STACKTRACE - " + ex.StackTrace + ex.InnerException, ex, "", "", "", "");
            }
            _ensPredictionTimer.Start();
        }

        public void StartENSCRPredictions()
        {
            try
            {
                //_predictionSchedulerService.UpdateIterationPredictions("00100000-0000-0000-0000-000000000000", "14e2c0e0-8212-4da9-b910-f8bd88c2ec38", "8e8ecad3-6b63-0c6c-abfd-22333c56470e");
                if (appSettings.EnableENSPredictions == "True")
                {
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(TrainModelsWorker), nameof(StartENSCRPredictions), "START - ENS CR PREDICTION", string.Empty, string.Empty, string.Empty, string.Empty);
                    _predictionSchedulerService.TriggerCRENSPredictions();
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(TrainModelsWorker), nameof(StartENSCRPredictions), ex.Message + "- STACKTRACE - " + ex.StackTrace + ex.InnerException, ex, "", "", "", "");
            }
            _ensPredictionCRTimer.Start();
        }

        public void SendNotifications()
        {
            try
            {
                //Added for SPA Similary usecase -start
                _notificationService.SendAITrainingNotifications();
                //Added for SPA Similary usecase -end
                SendSPPNotifications();
                _notificationService.SendPredictionNotifications();
                _notificationService.SendTrainingNotifications();

                List<DATAMODELS.AppNotificationLog> appNotificationLogs = _notificationService.GetNotificationLog();

                if (appNotificationLogs.Count > 0)
                {
                    foreach (var notification in appNotificationLogs)
                    {
                        _notificationService.SendNotification(notification);
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(TrainModelsWorker), nameof(SendNotifications), ex.Message + "- STACKTRACE - " + ex.StackTrace + ex.InnerException, ex, "", "", "", "");
            }


            _notfTimer.Start();

        }

        public void SendSPPNotifications()
        {
            try
            {
                List<DATAMODELS.IngrainRequestQueue> ingrainRequestQueues = _notificationService.GetSPPPredictionRequests();
                if (ingrainRequestQueues != null)
                {
                    _notificationService.SendSPPPredNotification(ingrainRequestQueues);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(TrainModelsWorker), nameof(SendSPPNotifications), ex.Message + "- STACKTRACE - " + ex.StackTrace + ex.InnerException, ex, "", "", "", "");
            }

        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
            _notfTimer?.Dispose();
            _ensPredictionTimer?.Dispose();
            _ensPredictionCRTimer?.Dispose();
            //_iterationTimer?.Dispose();
        }


    }
}
