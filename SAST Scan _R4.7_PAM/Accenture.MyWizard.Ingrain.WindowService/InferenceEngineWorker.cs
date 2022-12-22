using Accenture.MyWizard.Ingrain.DataModels.InferenceEngineModel;
using Accenture.MyWizard.Ingrain.WindowService.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using WINSERVICEMODELS = Accenture.MyWizard.Ingrain.WindowService.Models;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
//using Accenture.MyWizard.Ingrain.WindowService.Services;

namespace Accenture.MyWizard.Ingrain.WindowService
{
    public class InferenceEngineWorker : IHostedService, IDisposable
    {
        private System.Timers.Timer _timer;
        private System.Timers.Timer _retrainTimer;
        private System.Timers.Timer _notfTimer;
        private readonly WINSERVICEMODELS.AppSettings appSettings = null;
        private QueueManagerService _queueManagerService;
        private InferenceEngineService _inferenceEngineService = null;
        private IENotificationService _notificationService = null;

        public InferenceEngineWorker()
        {
            _timer = new System.Timers.Timer();
            _retrainTimer = new System.Timers.Timer();
            _notfTimer = new System.Timers.Timer();
            appSettings = AppSettingsJson.GetAppSettings().GetSection("AppSettings").Get<WINSERVICEMODELS.AppSettings>();
            _queueManagerService = new QueueManagerService();
            _inferenceEngineService = new InferenceEngineService();
            _notificationService = new IENotificationService();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            //exclude for devtest env
            if (!appSettings.IngrainAPIUrl.Contains("devtest"))
            {
                _inferenceEngineService.DecryptDevDBIE();
            }
            _timer.Elapsed += new ElapsedEventHandler(OnElapsedTime);
            _timer.Interval = appSettings.Environment.Equals(CONSTANTS.PADEnvironment)? Convert.ToDouble(1000) : Convert.ToDouble(2000);
            _timer.Enabled = true;
            _timer.AutoReset = false;

            _notfTimer.Elapsed += new ElapsedEventHandler(OnElapsedNotfTime);
            _notfTimer.Interval = appSettings.Environment.Equals(CONSTANTS.PADEnvironment) ? Convert.ToDouble(1000) : Convert.ToDouble(5000);
            _notfTimer.Enabled = true;
            _notfTimer.AutoReset = false;

            _retrainTimer.Elapsed += new ElapsedEventHandler(OnReTrainElapsedTime);
            _retrainTimer.Interval = appSettings.Environment.Equals(CONSTANTS.PADEnvironment) ? Convert.ToDouble(1000) : Convert.ToDouble(8000);
            _retrainTimer.Enabled = true;
            _retrainTimer.AutoReset = false;

            return Task.CompletedTask;
        }

        private void OnElapsedTime(object source, ElapsedEventArgs e)
        {
            InvokeIEServiceRequests();
        }
        private void OnElapsedNotfTime(object source, ElapsedEventArgs e)
        {
            IENoficationLog();
        }

        private void OnReTrainElapsedTime(object source, ElapsedEventArgs e)
        {
            ReTrainIEModels();
        }

        public void InvokeIEServiceRequests()
        {
            try
            {
                List<IERequestQueue> ingrainRequestQueue = _inferenceEngineService.GetIERequests();
                if (ingrainRequestQueue != null)
                {
                    if (ingrainRequestQueue.Count > 0)
                        _queueManagerService.RequestBatchLimitInsert(ingrainRequestQueue.Count, CONSTANTS.IERequestBatchLimitMonitor);


                    foreach (var request in ingrainRequestQueue)
                    {
                        _queueManagerService.UpdateIETrainingQueueStatus();
                        string status = _queueManagerService.GetIETrainingQueueStatus();
                        if (status == "Available")
                        {
                            _inferenceEngineService.InvokeIERequest(request);

                        }

                    }
                }
            }
            catch (Exception ex)
            {

                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InferenceEngineWorker), nameof(InvokeIEServiceRequests), ex.Message + "- STACKTRACE - " + ex.StackTrace + ex.InnerException, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
            _timer.Start();
        }

        public void IENoficationLog()
        {
            try
            {

                List<IEAppNotificationLog> appNotificationLogs = _notificationService.GetNotificationLog(appSettings.NotificationMaxRetryCount);

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

                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InferenceEngineWorker), nameof(InvokeIEServiceRequests), ex.Message + "- STACKTRACE - " + ex.StackTrace + ex.InnerException, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
            _notfTimer.Start();
        }



        public void ReTrainIEModels()
        {
            try
            {

                var task = _inferenceEngineService.GetReTrainTask();
                if (task != null)
                {
                    var ieTimeToRun = task.TimeToRun + " " + task.TimePeriod;
                    if (ieTimeToRun == DateTime.UtcNow.ToString("hh:mm:ss tt"))
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(InferenceEngineWorker), nameof(ReTrainIEModels), "AutoReTrainTasks STARTED -" + DateTime.Now.ToString("hh:mm:ss tt") + "*****" + ieTimeToRun, string.Empty, string.Empty, string.Empty, string.Empty);
                        Thread ieThread = new Thread(_inferenceEngineService.ReTrainIEModels);
                        ieThread.Start();
                    }
                }

            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(InferenceEngineWorker), nameof(ReTrainIEModels), ex.Message + "- STACKTRACE - " + ex.StackTrace + ex.InnerException, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
            _retrainTimer.Start();
        }



        //public void InvokeInProgressIERequest()
        //{
        //    try
        //    {
        //        _inferenceEngineService.UpdateIERequestToError();
        //    }
        //    catch (Exception ex)
        //    {
        //        LOGGING.LogManager.Logger.LogErrorMessage(typeof(AIServiceWorker), nameof(InvokeInProgressIERequest), ex.Message, ex);
        //    }
        //    _statusTimer.Start();
        //}
        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
            _retrainTimer?.Dispose();
            _notfTimer?.Dispose();
        }
    }
}
