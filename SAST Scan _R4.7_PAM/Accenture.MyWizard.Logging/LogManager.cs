#region Copyright (c) 2018 Accenture . All rights reserved.

// <copyright company="Accenture">
// Copyright (c) 2018 Accenture.  All rights reserved.
// Reproduction or transmission in whole or in part, in any form or by any means, 
// electronic, mechanical or otherwise, is prohibited without the prior written 
// consent of the copyright owner.
// </copyright>

#endregion

namespace Accenture.MyWizard.LOGGING
{
    #region Namespace References

    using log4net;
    using log4net.Appender;
    using log4net.Config;
    using Newtonsoft.Json;
    using System;
    using System.Configuration;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Net.Sockets;
    using System.Reflection;
	using System.Threading;
	#endregion

    /// <summary>
    /// Represents Log Manager. 
    /// </summary>
    /// <remarks>
    /// Represents Log Manager. 
    /// </remarks>
    [ExcludeFromCodeCoverage]
    public sealed class LogManager
    {
        private static readonly string[] loggers = { "", "CustomConstraintsLog" };

        #region Members

        private static readonly Lazy<LogManager> _instance = new Lazy<LogManager>(() => new LogManager());
        private static ILog _logger = null;
        private static ILog _SSAIlogger = null;
        private static ILog _CustomConstraintslogger = null;
        //private FormattableString _logTemplate = (FormattableString)$@"{{""Type"":""{{type}}"",""DateTime"":""{{datetime}}"", ""Title"":""{{title}}"", ""Message"":{{message}}, ""CorrelationUId"":""{{correlationUId}}"", ""Source"":""{LogAppSettings.AppName}"", ""User"":""{LogAppSettings.User}"", ""HostName"":""{LogAppSettings.MachineFQDN}"", ""HostIP"":""{LogAppSettings.MachineIP}"", ""Product"":""{{product}}"", ""AppService"":""{{appService}}"", ""Component"":""{{component}}"", ""SubComponent"":""{{subComponent}}"", ""RequestedByApp"":""{{requestedByApp}}"", ""EntityName"":""{{entityName}}"", ""Exception"":{{exceptionMessage}}, ""StackTrace"":{{jsonStacktrace}}, ""ClientUId"":""{{clientUId}}"", ""DeliveryConstructUId"":""{{deliveryConstructUId}}""}}";
        private FormattableString _logTemplate = (FormattableString)$@"{{""LogType"":""{{type}}"",""DateTime"":""{{datetime}}"", ""Title"":""{{title}}"", ""Message"":{{message}}, ""CorrelationUId"":""{{correlationUId}}"", ""Source"":""{LogAppSettings.AppName}"", ""Product"":""{{product}}"", ""AppService"":""{{appService}}"", ""Component"":""{{component}}"", ""SubComponent"":""{{subComponent}}"", ""RequestedByApp"":""{{requestedByApp}}"", ""EntityName"":""{{entityName}}"", ""Exception"":{{exceptionMessage}}, ""StackTrace"":{{jsonStacktrace}}, ""ClientUId"":""{{clientUId}}"", ""DeliveryConstructUId"":""{{deliveryConstructUId}}""}}";
        private String _empty = LogConstants.EmptyMessage;
        private string _info = LogConstants.Info;
        private string _error = LogConstants.Error;
        private string _product = LogConstants.Product;
        private string _appService = LogConstants.AppService;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <value>
        /// The logger.
        /// </value>
        public static LogManager Logger
        {
            get
            {
                return _instance.Value;
            }
        }

        /// <summary>
        /// Gets the log folder path.
        /// </summary>
        /// <value>
        /// The log folder path.
        /// </value>
        public static String LogFolderPath
        {
            get
            {
                return new FileInfo(_logger.Logger.Repository.GetAppenders().OfType<RollingFileAppender>().FirstOrDefault().File).Directory.FullName;
            }
        }

        /// <summary>
        /// Gets or sets the json serializer settings.
        /// </summary>
        /// <value>
        /// The json serializer settings.
        /// </value>
        public JsonSerializerSettings JsonSerializerSettings { get; set; } = new JsonSerializerSettings
        {
            StringEscapeHandling = StringEscapeHandling.EscapeHtml,
            NullValueHandling = NullValueHandling.Include
        };

        #endregion

        #region Constructors

        /// <summary>
        /// Prevents a default instance of the <see cref="LogManager"/> class from being created.
        /// </summary>
        /// <remarks>
        /// Prevents a default instance of the <see cref="LogManager"/> class from being created.
        /// </remarks>
        private LogManager()
        {
        }

        #endregion

        #region Methods

        /// <summary>
        /// Sets the log folder path.
        /// </summary>
        /// <remarks>
        /// Sets the log folder path.
        /// </remarks>
        /// <param name="logFolder">The log folder.</param>
        public static void SetLogFolderPath(String logFolder)
        {
            log4net.GlobalContext.Properties[LogConstants.LogFolder] = logFolder;
            /// Change by Hariom
            /// commenting out this method and adding an alternate as parameterless configure is not available.
            ///  
            /// 

            // XmlConfigurator.Configure();

            var logRepository = log4net.LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo(LogConstants.LogFileName));


            if (Assembly.GetEntryAssembly().GetName().Name == "Accenture.MyWizard.Ingrain.WindowService")
            {
                foreach (string logger in loggers)
                {
                    switch (logger)
                    {
                        case "CustomConstraintsLog":
                            _CustomConstraintslogger = log4net.LogManager.GetLogger(logger);
                            break;
                        default:
                            _SSAIlogger = log4net.LogManager.GetLogger("SSAIWorkerLog");
                            break;
                    }
                }
            }
            else
            {
                _logger = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
            }

        }

        /// <summary>
        /// Logs the specified exception.
        /// </summary>
        /// <remarks>
        /// Logs the specified exception.
        /// </remarks>
        /// <param name="title">The title.</param>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        public void LogErrorMessage(Type type, String title
                  , String message
                  , Exception exception
                  , String requestedByApp
                  , String entityName
                  , String clientUId
                  , String deliveryConstructUId)
        {
            LogErrorMessage(type, title, message, exception, requestedByApp, entityName, clientUId, deliveryConstructUId, string.Empty);
        }

        /// <summary>
        /// Logs the specified exception.
        /// </summary>
        /// <remarks>
        /// Logs the specified exception.
        /// </remarks>
        /// <param name="title">The title.</param>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        public void LogErrorMessage(Type type, String title
              , String message
              , Exception exception
              , String requestedByApp
              , String entityName
              , String clientUId
              , String deliveryConstructUId
              , String loggerName)
        {
            if (Assembly.GetEntryAssembly().GetName().Name == "Accenture.MyWizard.Ingrain.WindowService")
            {
                initiateLogger(loggerName);
            }

            var messageFormat = (FormattableString)$@"{(type != null ? type.FullName : String.Empty)}: { message}";
            var jsonMessage = JsonConvert.SerializeObject(messageFormat.ToString(CultureInfo.InvariantCulture), JsonSerializerSettings);
            if (exception != null)
            {
                var exceptionMessage = JsonConvert.SerializeObject(exception.Message, JsonSerializerSettings);
                var jsonStacktrace = JsonConvert.SerializeObject(exception.StackTrace, JsonSerializerSettings);
                jsonMessage = JsonConvert.SerializeObject(message, JsonSerializerSettings);
                var log = GetLog(_error, title, jsonMessage.Replace("\\u0027", "'"), null, _product, _appService, (type != null ? type.Name : String.Empty), title, requestedByApp, entityName, exceptionMessage, jsonStacktrace, clientUId, deliveryConstructUId);

                _logger.Error(log);

                //// Log InnerException if any
                if (exception.InnerException != null)
                {
                    exceptionMessage = JsonConvert.SerializeObject(exception.InnerException.Message, JsonSerializerSettings);
                    jsonStacktrace = JsonConvert.SerializeObject(exception.InnerException.StackTrace, JsonSerializerSettings);
                    log = GetLog(_error, title, jsonMessage.Replace("\\u0027", "'"), null, _product, _appService, (type != null ? type.Name : String.Empty), title, requestedByApp, entityName, exceptionMessage, jsonStacktrace, clientUId, deliveryConstructUId);

                    _logger.Error(log);
                }
            }
        }
        /// <summary>
        /// Logs the info message
        /// </summary>
        /// <param name="title"></param>
        /// <param name="message"></param>
        public void LogInfo(String title
                         , String message)
        {
            var jsonMessage = JsonConvert.SerializeObject(message, JsonSerializerSettings);
            var log = GetLog(_info, title, jsonMessage.Replace("\\u0027", "'"), null, null, null, null, null, null, null, _empty, _empty, null, null);
            _logger.Info(log);
        }

        public void LogProcessInfo(Type type
                              , String title
                              , String message
                              , String requestedByApp
                              , String entityName
                              , String clientUId
                              , String deliveryConstructUId)
        {
            LogProcessInfo(type, title, message, requestedByApp, entityName, clientUId, deliveryConstructUId, string.Empty);
        }

        /// <summary>
        /// Logs the process information.
        /// </summary>
        /// <remarks>
        /// Logs the process information.
        /// </remarks>
        /// <param name="type">The type.</param>
        /// <param name="title">The title.</param>
        /// <param name="message">The message.</param>
        public void LogProcessInfo(Type type
                               , String title
                               , String message
                               , String requestedByApp
                               , String entityName
                               , String clientUId
                               , String deliveryConstructUId
                               , String loggerName)
        {

            if (Assembly.GetEntryAssembly().GetName().Name == "Accenture.MyWizard.Ingrain.WindowService")
            {
                initiateLogger(loggerName);
            }
            var messageFormat = (FormattableString)$@"{(type != null ? type.FullName : String.Empty)}: { message}";
            var jsonMessage = JsonConvert.SerializeObject(messageFormat.ToString(CultureInfo.InvariantCulture), JsonSerializerSettings);
            var log = GetLog(_info, title, jsonMessage.Replace("\\u0027", "'"), null, _product, _appService, (type != null ? type.Name : String.Empty), title, requestedByApp, entityName, _empty, _empty, clientUId, deliveryConstructUId);

            _logger.Info(log);
        }

        /// <summary>
        /// Logs the process information with CorrelationUId.
        /// </summary>
        /// <remarks>
        /// Logs the process information with CorrelationUId.
        /// </remarks>
        /// <param name="type">The type.</param>
        /// <param name="title">The title.</param>
        /// <param name="message">The message.</param>
        /// <param name="correlationUId">The correlationUId.</param>


        public void LogProcessInfo(Type type
                             , String title
                             , String message
                             , Guid correlationUId
                             , String requestedByApp
                             , String entityName
                             , String clientUId
                             , String deliveryConstructUId)
        {
            LogProcessInfo(type, title, message, correlationUId, requestedByApp, entityName, clientUId, deliveryConstructUId, string.Empty);
        }

        public void LogProcessInfo(Type type
                          , String title
                          , String message
                          , Guid correlationUId
                          , String requestedByApp
                          , String entityName
                          , String clientUId
                          , String deliveryConstructUId
                          , string loggerName)
        {
            if (Assembly.GetEntryAssembly().GetName().Name == "Accenture.MyWizard.Ingrain.WindowService")
            {
                initiateLogger(loggerName);
            }
            var messageFormat = (FormattableString)$@"{(type != null ? type.FullName : String.Empty)}: { message}";
            var jsonMessage = JsonConvert.SerializeObject(messageFormat.ToString(CultureInfo.InvariantCulture), JsonSerializerSettings);
            var log = GetLog(_info, title, jsonMessage.Replace("\\u0027", "'"), correlationUId, _product, _appService,
                (type != null ? type.Name : String.Empty), title, requestedByApp, entityName,
                _empty, _empty, clientUId, deliveryConstructUId);

            _logger.Info(log);
        }


        public void initiateLogger(string loggerName)
        {
            switch (loggerName)
            {
                case "CustomConstraintsLog":
                    _logger = _CustomConstraintslogger;
                    break;
                default:
                    _logger = _SSAIlogger;
                    break;
            }
        }


        /// <summary>
        /// Logs the information for debug purpose.
        /// </summary>
        /// <remarks>
        /// Logs the information for debug purpose.
        /// </remarks>
        /// <param name="title">The title</param>
        /// <param name="parameters">The parameters</param>
        public void LogDiagnosticsInfo(String title
                                    , params Object[] parameters)
        {
            if (parameters != null)
            {
                var jsonMessage = String.Empty;
                var log = String.Empty;

                foreach (var param in parameters)
                {
                    jsonMessage = JsonConvert.SerializeObject(param, JsonSerializerSettings);
                    log = GetLog(_info, title, jsonMessage.Replace("\\u0027", "'"), null, null, null, null, null, null, null, _empty, _empty, null, null);

                    _logger.Debug(log);
                }
            }
        }

        /// <summary>
        /// Logs the information for debug purpose with CorrelationUId.
        /// </summary>
        /// <remarks>
        /// Logs the information for debug purpose with CorrelationUId.
        /// </remarks>
        /// <param name="title">The title</param>
        /// <param name="correlationUId">The correlationUId</param>
        /// <param name="parameters">The parameters</param>
        public void LogDiagnosticsInfo(String title
                                      , Guid correlationUId
                                      , params Object[] parameters)
        {
            if (parameters != null)
            {
                var jsonMessage = String.Empty;
                var log = String.Empty;
                var titleFormat = (FormattableString)$@"Fault: {title}";

                foreach (var param in parameters)
                {
                    jsonMessage = JsonConvert.SerializeObject(param, JsonSerializerSettings);
                    log = GetLog(_info, titleFormat.ToString(CultureInfo.InvariantCulture), jsonMessage.Replace("\\u0027", "'"), correlationUId, null, null, null, null, null, null, _empty, _empty, null, null);

                    _logger.Debug(log);
                }
            }
        }

        /// <summary>
        /// Get the log string post macro replacement
        /// </summary>
        /// <remarks>
        /// Get the log string post macro replacement
        /// </remarks>
        /// <param name="title">The title</param>
        /// <param name="message">The message</param>
        /// <param name="correlationUId">The correlationUId</param>
        /// <param name="exceptionMessage">The exceptionMessage</param>
        /// <param name="jsonStacktrace">The jsonStacktrace</param>
        /// <returns>Log String</returns>
        private String GetLog(string info, String title
                           , String message
                           , Guid? correlationUId
                           , String product
                           , String appService
                           , String component
                           , String subComponent
                           , String requestedByApp
                           , String entityName
                           , String exceptionMessage
                           , String jsonStacktrace
                           , String clientUId
                           , String deliveryConstructUId)
        {


            var log = _logTemplate.ToString(CultureInfo.InvariantCulture)
                                  .Replace(LogConstants.LogType, info)
                                  .Replace(LogConstants.LogDateTime, DateTime.UtcNow.ToString(LogConstants.LogDateTimeFormat, CultureInfo.InvariantCulture))
                                  .Replace(LogConstants.LogTitle, title)
                                  .Replace(LogConstants.LogMessage, message)
                                  .Replace(LogConstants.LogCorrelationId, correlationUId.HasValue ? correlationUId.ToString() : String.Empty)
                                  .Replace(LogConstants.LogProduct, product)
                                  .Replace(LogConstants.LogAppService, appService)
                                  .Replace(LogConstants.LogComponent, component)
                                  .Replace(LogConstants.LogSubComponent, subComponent)
                                  .Replace(LogConstants.LogRequestedByApp, requestedByApp)
                                  .Replace(LogConstants.LogEntityName, entityName)
                                  .Replace(LogConstants.LogExceptionMessage, exceptionMessage)
                                  .Replace(LogConstants.LogStackTrace, jsonStacktrace)
                                  .Replace(LogConstants.LogClientUId, clientUId)
                                  .Replace(LogConstants.LogDeliveryConstructUId, deliveryConstructUId)
                                  .Replace(LogConstants.LogThreadId, Convert.ToString(Thread.CurrentThread.ManagedThreadId))
                                  .Replace(LogConstants.LogProcessId, Convert.ToString(System.Diagnostics.Process.GetCurrentProcess().Id));

            return log.ToString(CultureInfo.InvariantCulture);
        }


        #endregion
    }

    public static class LogConstants
    {
        public const string LogFolder = "LogFolder";
        public const string LogFileName = "log4net.config";
        public const string LogType = "{type}";
        public const string LogDateTime = "{datetime}";
        public const string LogDateTimeFormat = "yyyy-MM-dd HH:mm:ss:ffff";
        public const string LogTitle = "{title}";
        public const string LogMessage = "{message}";
        public const string LogCorrelationId = "{correlationUId}";
        public const string LogExceptionMessage = "{exceptionMessage}";
        public const string LogStackTrace = "{jsonStacktrace}";
        public const string EmptyMessage = "\"\"";
        public const string Info = "INFO";//"INFO :"
        public const string Error = "ERROR";
        public const string AppName = "AppName";
        public const string MyWizardIngrain = "myWizard-Ingrain";
        public const string User = "User";
        public const string LogProduct = "{product}";
        public const string LogAppService = "{appService}";
        public const string Product = "myWizard";
        public const string AppService = "myWizard-Ingrain";
        public const string LogComponent = "{component}";
        public const string LogSubComponent = "{subComponent}";
        public const string LogRequestedByApp = "{requestedByApp}";
        public const string LogEntityName = "{entityName}";
        public const string LogClientUId = "{clientUId}";
        public const string LogDeliveryConstructUId = "{deliveryConstructUId}";
        public const string LogProcessId = "{processid}";
        public const string LogThreadId = "{threadid}";
    }


    [ExcludeFromCodeCoverage]
    public static class LogAppSettings
    {
        #region Private Static Properties
        #endregion

        #region Public Static Properties

        /// <summary>
        /// Gets the machine FQDN.
        /// </summary>
        /// <value>
        /// The machine FQDN.
        /// </value>
        public static String MachineFQDN
        {
            get
            {
                var domainName = IPGlobalProperties.GetIPGlobalProperties().DomainName;
                var hostName = Dns.GetHostName();
                var fqdn = "";

                fqdn = !hostName.Contains(domainName) ? hostName + "." + domainName : hostName;

                return fqdn;
            }
        }

        /// <summary>
        /// Gets the machine ip.
        /// </summary>
        /// <value>
        /// The machine ip.
        /// </value>
        public static String MachineIP
        {
            get
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                var localIP = String.Empty;

                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        localIP = ip.ToString();
                    }
                }

                return localIP;
            }
        }

        /// <summary>
        /// Gets the name of the application.
        /// </summary>
        /// <value>
        /// The name of the application.
        /// </value>
        public static String AppName
        {
            get
            {
                var value = ConfigurationManager.AppSettings.Get(LogConstants.AppName);

                if (String.IsNullOrEmpty(value))
                {
                    value = LogConstants.MyWizardIngrain;
                }

                return value;
            }
        }

        /// <summary>
        /// Gets the user.
        /// </summary>
        /// <value>
        /// The user.
        /// </value>
        public static String User
        {
            get
            {
                var value = ConfigurationManager.AppSettings.Get(LogConstants.User);

                if (String.IsNullOrEmpty(value))
                {
                    // Linux & Windows 
                    value = Environment.UserName;
                    //value = WindowsIdentity.GetCurrent().Name;
                }

                if (!String.IsNullOrEmpty(value))
                {
                    value = value.Replace(@"\", "\\\\");
                }

                return value;
            }
        }

        #endregion Public Static Properties
    }
}
