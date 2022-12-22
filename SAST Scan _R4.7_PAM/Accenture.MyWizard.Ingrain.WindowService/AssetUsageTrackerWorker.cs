using Accenture.MyWizard.Ingrain.BusinessDomain.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using WINSERVICEMODELS = Accenture.MyWizard.Ingrain.WindowService.Models;

namespace Accenture.MyWizard.Ingrain.WindowService
{
    public class AssetUsageTrackerWorker : IHostedService, IDisposable //BackgroundService
    {
        #region Private Members
        //Timers
        System.Timers.Timer timer = new System.Timers.Timer();

        private System.ComponentModel.IContainer components = null;

        private readonly WINSERVICEMODELS.AppSettings appSettings = null;

        private DatabaseProvider databaseProvider;

        private IMongoDatabase _database;

        private Services.AssetUsageTrackingService _assetUsageTrackingService;

        #endregion


        #region Worker

        public AssetUsageTrackerWorker()
        {
            appSettings = AppSettingsJson.GetAppSettings().GetSection("AppSettings").Get<WINSERVICEMODELS.AppSettings>();
            //Mongo connection
            databaseProvider = new DatabaseProvider();
            string connectionString = appSettings.connectionString;
            var dataBaseName = MongoUrl.Create(connectionString).DatabaseName;
            MongoClient mongoClient = databaseProvider.GetDatabaseConnection();
            _database = mongoClient.GetDatabase(dataBaseName);
            _assetUsageTrackingService = new Services.AssetUsageTrackingService();
            //end
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            //_assetUsageTrackingService.PushAssetUsageTrackingToSaaS().Wait();
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AssetUsageTrackerWorker), "StartAsync", "Asset usage tracker Start at " + DateTime.Now.ToString(), string.Empty, string.Empty, string.Empty, string.Empty);
            timer.Elapsed += new ElapsedEventHandler(OnElapsedTime);
            timer.Interval = GetTimerInterval(false);
            timer.Enabled = true;
            timer.AutoReset = false;
            OnElapsedTime(null, null);
            // _assetUsageTrackingService.PushAssetUsageTrackingToSaaS().Wait();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AssetUsageTrackerWorker), "StopAsync", "Asset usage tracker stop at " + DateTime.Now.ToString(), string.Empty, string.Empty, string.Empty, string.Empty);
            return Task.CompletedTask;
        }

        private void OnElapsedTime(object source, ElapsedEventArgs e)
        {
            try
            {
                _assetUsageTrackingService.PushAssetUsageTrackingToSaaS().Wait();               
            }
            catch(Exception exception)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AssetUsageTrackerWorker), nameof(OnElapsedTime), exception.Message, exception, string.Empty, string.Empty, string.Empty, string.Empty);
            }
            finally
            {
                timer.Interval = GetTimerInterval(true);
                timer.Start();
            }
        }

        private double GetTimerInterval(bool isRenewal)
        {
            if(appSettings.isProd == true)
            {
                int targetHour = 23;
                int targetMinute = 32;
                if(isRenewal)
                {
                    var currentTime = DateTime.UtcNow;
                    if(currentTime.Hour == targetHour)
                    {
                        var targetTime = currentTime.AddDays(1);
                        targetTime = new DateTime(targetTime.Year, targetTime.Month, targetTime.Day, targetHour, targetMinute, 0);
                        var intervalTime = targetTime - currentTime;
                        return intervalTime.TotalMilliseconds;
                    }
                    else
                    {
                        var targetTime = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, targetHour, targetMinute, 0);
                        var intervalTime = targetTime - currentTime;
                        return intervalTime.TotalMilliseconds;
                    }
                }
                else
                {
                    var currentTime = DateTime.UtcNow;
                    if((currentTime.Hour == targetHour && currentTime.Minute <= (targetMinute - 2)) || (currentTime.Hour != targetHour))
                    {
                        var targetTime = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, targetHour, targetMinute, 0);
                        var intervalTime = targetTime - currentTime;
                        return intervalTime.TotalMilliseconds;
                    }
                    else
                    {
                        var targetTime = currentTime.AddDays(1);
                        targetTime = new DateTime(targetTime.Year, targetTime.Month, targetTime.Day, targetHour, targetMinute, 0);
                        var intervalTime = targetTime - currentTime;
                        return intervalTime.TotalMilliseconds;
                    }
                }
            }
            else
            {
                var currentTime = DateTime.UtcNow;
                var targetTime = currentTime.AddHours(1);
                targetTime = new DateTime(targetTime.Year, targetTime.Month, targetTime.Day, targetTime.Hour, 2, 0);
                var intervalTime = targetTime - currentTime;
                return intervalTime.TotalMilliseconds;
            }
           
        }

        #endregion

        #region Dispose
        public void Dispose(bool disposing)
        {
            if(disposing && (components != null))
            {
                components.Dispose();
            }
        }

        public void Dispose()
        {
            timer?.Dispose();
        }

        #endregion
    }
}
