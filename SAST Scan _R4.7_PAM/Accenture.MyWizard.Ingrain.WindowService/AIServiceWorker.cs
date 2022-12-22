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
using AICOREMODELS = Accenture.MyWizard.Ingrain.DataModels.AICore;
using Microsoft.Extensions.Configuration;
using Accenture.MyWizard.Ingrain.DataModels.InferenceEngineModel;

namespace Accenture.MyWizard.Ingrain.WindowService
{
    public class AIServiceWorker : IHostedService, IDisposable
    {
        private System.Timers.Timer _timer;
        private System.Timers.Timer _statusTimer;
        private readonly WINSERVICEMODELS.AppSettings appSettings = null;
        private AICOREMODELS.AIServiceRequestStatus _aIServiceRequestStatus = null;
        private AIModelsService _aIModelsService = null;
        private QueueManagerService _queueManagerService;

        public AIServiceWorker()
        {
            _timer = new System.Timers.Timer();
            _statusTimer = new System.Timers.Timer();
            appSettings = AppSettingsJson.GetAppSettings().GetSection("AppSettings").Get<WINSERVICEMODELS.AppSettings>();
            _aIModelsService = new AIModelsService();
            _queueManagerService = new QueueManagerService();
         
        }


        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer.Elapsed += new ElapsedEventHandler(OnElapsedTime);
            _timer.Interval = Convert.ToDouble(1000);
            _timer.Enabled = true;
            _timer.AutoReset = false;

            _statusTimer.Elapsed += new ElapsedEventHandler(OnStatusElapsedTime);
            _statusTimer.Interval = Convert.ToDouble(60000);
            _statusTimer.Enabled = true;
            _statusTimer.AutoReset = false;

            return Task.CompletedTask;
        }


        private void OnElapsedTime(object source, ElapsedEventArgs e)
        {
            InvokeAIServiceRequests();
        }
        private void OnStatusElapsedTime(object source, ElapsedEventArgs e)
        {
            InvokeTask();
        }

        public void InvokeAIServiceRequests()
        {
            try
            {
                List<AICOREMODELS.AIServiceRequestStatus> aIServiceRequests = _aIModelsService.GetAIServiceRequests();
                if(aIServiceRequests != null)
                {
                    foreach(var request in aIServiceRequests)
                    {
                        string status = string.Empty;
                        if (request.ServiceId == "72c38b39-c9fe-4fa6-97f5-6c3adc6ba355")
                        {
                            _queueManagerService.UpdateAITrainingQueueStatus("CLUSTERING");
                            status = _queueManagerService.GetAITrainingQueueStatus("CLUSTERING");
                        }
                        else if (request.ServiceId == "042468f4-db5b-403f-8fbc-e5378077449e")
                        {
                            _queueManagerService.UpdateAITrainingQueueStatus("WORDCLOUD");
                            status = _queueManagerService.GetAITrainingQueueStatus("WORDCLOUD");
                        }
                        else
                        {
                            _queueManagerService.UpdateAITrainingQueueStatus("AIService");
                            status = _queueManagerService.GetAITrainingQueueStatus("AIService");
                        }
                        if (status == "Available")
                        {
                            _aIModelsService.InvokeAIRequest(request);
                        }
                        
                    }
                }



                List<IERequestQueue> ingrainRequestQueue = _aIModelsService.GetIERequests();
                if (ingrainRequestQueue != null)
                {
                    foreach (var request in ingrainRequestQueue)
                    {
                        _queueManagerService.UpdateIETrainingQueueStatus();
                        string status = _queueManagerService.GetIETrainingQueueStatus();
                        if (status == "Available")
                        {
                            _aIModelsService.InvokeIERequest(request);
                        }

                    }
                }

            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AIServiceWorker), nameof(InvokeAIServiceRequests), ex.Message + "- STACKTRACE - " + ex.StackTrace + ex.InnerException, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }

            _timer.Start();
        }


        public void InvokeTask()
        {
            try
            {
                _aIModelsService.ChangeAIRequestStatustoError();
            }
            catch(Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AIServiceWorker), nameof(InvokeTask), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
            _statusTimer.Start();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
            _statusTimer?.Dispose();
        }


    }
}
