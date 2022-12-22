using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.WindowService.Services
{
    #region Namespace References
    using MongoDB.Driver;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using WINSERVICEMODELS = Accenture.MyWizard.Ingrain.WindowService.Models;
    using DATAACCESS = Accenture.MyWizard.Ingrain.WindowService;
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Net;
    using RestSharp;
    using Accenture.MyWizard.Ingrain.WindowService.Interfaces;
    using System.Threading.Tasks;
    using System.Linq;
    using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
    using MongoDB.Bson;
    using Accenture.MyWizard.Ingrain.WindowService.Models;
    using Accenture.MyWizard.Ingrain.DataModels.Models;
    using MongoDB.Bson.Serialization;
    using Accenture.MyWizard.Ingrain.WindowService.Models.SaaS;
    using System.Xml.Linq;
    using System.Xml;
    using System.IO;
    using System.Xml.XPath;
    using System.Xml.Xsl;
    using Formatting = Newtonsoft.Json.Formatting;


    // using Microsoft.Extensions.DependencyInjection;
    #endregion
    public class DataProviderService : IDataProvider
    {
        private readonly WINSERVICEMODELS.AppSettings appSettings = null;
        ServiceCallerService _serviceCallerService;
        private readonly IMongoDatabase _database;
        List<HistoricalTicketData> Singleticketdata = new List<HistoricalTicketData>();
        List<HistoricalTicketData> Multiticketdata = new List<HistoricalTicketData>();
        int isBreak = 0;
        Models.SaaS.WorkItemsList WorkItemsListt = new Models.SaaS.WorkItemsList();
        Models.SaaS.IterationsList IterationsListt = new Models.SaaS.IterationsList();
        Models.SaaS.TicketList TicketsList = new Models.SaaS.TicketList();
        Models.SaaS.TaskList TaskList = new Models.SaaS.TaskList();
        Models.SaaS.TestResultList TestResultList = new Models.SaaS.TestResultList();
        Models.SaaS.DeploymentList DeploymentList = new Models.SaaS.DeploymentList();
        public DataProviderService()
        {
            appSettings = AppSettingsJson.GetAppSettings().GetSection("AppSettings").Get<WINSERVICEMODELS.AppSettings>();
            DATAACCESS.DatabaseProvider databaseProvider = new DATAACCESS.DatabaseProvider();
            var dataBaseName = MongoUrl.Create(appSettings.connectionString).DatabaseName;
            MongoClient mongoClient = databaseProvider.GetDatabaseConnection();
            _database = mongoClient.GetDatabase(dataBaseName);
            _serviceCallerService = new ServiceCallerService();
        }

        public async Task<string> GetHistoricalAgileDetails(List<string> faults, AIAAMiddleLayerRequest request)
        {
            var xDoc = new XDocument();
            var clonedXDoc = new XDocument(); int currentprovidercount = 0;
            AIAAMiddleLayerResponse middleLayerResponse = new AIAAMiddleLayerResponse();
            List<Models.SaaS.WorkItem> workItems = new List<Models.SaaS.WorkItem>();
            WorkItemsListt.WorkItems = new List<Models.SaaS.WorkItem>();
            string result = string.Empty;
           // BusinessDomain.Entities.IServiceCaller serviceCaller = this.NinjectKernel.Get<BusinessDomain.Entities.IServiceCaller>(BusinessDomain.Entities.BusinessDomainEntityType.ServiceCaller.ToString());
            try
            {
                int providercount = request.DataProviders.Count;
                if (request != null && request.DataProviders != null)
                {
                    foreach (Models.SaaS.DataProvider dataProvider in request.DataProviders)
                    {
                        currentprovidercount++;
                        var cloneddataProvider = dataProvider;
                        if (string.IsNullOrEmpty(Convert.ToString(dataProvider.TicketType)))
                        {
                            faults.Add(CreateValidationFault("Missing validation at dataProvider Read method", "ProjetId cannot be null"));
                        }
                        else
                        {
                            try
                            {
                                cloneddataProvider = FilldataProvider(dataProvider, request);
                                if (!string.IsNullOrEmpty(dataProvider.InputRequestValues))
                                {
                                    var inputRequestValues = dataProvider.InputRequestValues.Split(',').ToList();
                                    dataProvider.InputRequestValues = string.Join(",", inputRequestValues);
                                }
                                ServiceCallerRequest serviceCallerRequest = new ServiceCallerRequest()
                                {
                                    Name = cloneddataProvider.Name,
                                    Content = cloneddataProvider.InputRequestValues,
                                    AuthProvider = cloneddataProvider.AuthProvider,
                                    JsonRootNode = cloneddataProvider.JsonRootNode,
                                    Accept = cloneddataProvider.Accept,
                                    HttpVerbName = cloneddataProvider.Method,
                                    MIMEMediaType = cloneddataProvider.MIMEMediaType,
                                    ServiceUrl = cloneddataProvider.ServiceUrl,

                                };
                                xDoc = await _serviceCallerService.GetDocUsingService(serviceCallerRequest, faults).ConfigureAwait(false);
                                if (xDoc != null && xDoc.Root.Elements().Any())
                                {
                                    try
                                    {
                                        #region Call DataFormatter
                                        if (dataProvider.DataFormatter != null)
                                        {
                                            dataProvider.DataFormatter.XDocument = xDoc;
                                           // IDataFormatter dataFormatter = this.NinjectKernel.Get<IDataFormatter>(BusinessDomainEntityType.DataFormatter.ToString());
                                            xDoc = await ExecuteDataFormatter(dataProvider.DataFormatter, request, faults).ConfigureAwait(false);
                                        }
                                        #endregion
                                        if (xDoc != null && xDoc.Root.Elements().Any() && Convert.ToInt32(xDoc.Descendants("TotalRecordCount").ToList()[0].Value) != 0 && !string.IsNullOrEmpty(xDoc.Descendants("TotalRecordCount").ToList()[0].Value) && !string.IsNullOrEmpty(xDoc.Descendants("TotalPageCount").ToList()[0].Value))
                                        {
                                            if (Convert.ToInt32(xDoc.Descendants("TotalRecordCount").ToList()[0].Value) > 0) // && (Convert.ToInt32(xDoc.Descendants("CurrentPage").ToList()[0].Value) == Convert.ToInt32(xDoc.Descendants("TotalPageCount").ToList()[0].Value)))
                                            {
                                                if (xDoc != null && xDoc.Root.Elements().Any())
                                                {
                                                    if (xDoc != null && xDoc.DescendantNodes().Any())
                                                        middleLayerResponse.Response = JsonConvert.SerializeXNode(xDoc, Newtonsoft.Json.Formatting.None, true);
                                                    else
                                                        middleLayerResponse.Response = string.Empty;
                                                    WorkItemsList workitemsresult = JsonConvert.DeserializeObject<WorkItemsList>(middleLayerResponse.Response);
                                                    WorkItemsListt.WorkItems.AddRange(workitemsresult.WorkItems);

                                                    if (Convert.ToInt32(xDoc.Descendants("TotalPageCount").ToList()[0].Value) > 1)
                                                    {
                                                        xDoc = await InitiateMultipleBatchCall(request, dataProvider, WorkItemsListt);
                                                    }

                                                    middleLayerResponse.WorkItems = WorkItemsListt;
                                                    result = middleLayerResponse != null ? JsonConvert.SerializeObject(middleLayerResponse.WorkItems) : string.Empty;
                                                }
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        throw new Exception("Error occurred while converting response to AIAA format using data formatter - " + GetFaultString(faults));
                                    }
                                }
                                else
                                    faults.Add("XML document is empty.Error Occured while fetching tickets.Please try again.");
                            }
                            catch (Exception ee)
                            {
                                string s = ee.Message.ToString();
                                xDoc = new XDocument();
                                throw new Exception("Error occurred while reading data from Service Provider - " + GetFaultString(faults));
                            }
                        }
                        if (isBreak == 1)
                        {
                            break;
                        }
                    }
                }
                else
                {
                    faults.Add(CreateValidationFault("Missing validation at DataProcider Read method", "No Data providers found"));
                }

            }
            catch (Exception ex)
            {
                //logger.Info(" GetHistoricalTicketDetails : DDName: " + request.uploadTicketRequest.DDName + " Exception" + ex.Message.ToString());
                throw ex;

            }
            return result;
        }

        private string GetFaultString(List<string> faults)
        {
            StringBuilder sb = new StringBuilder();

            faults.ForEach((fault) =>
            {
                sb.AppendLine($"{fault}");
            });

            return sb.ToString();
        }
        protected string CreateValidationFault(String title, String message)
        {
            return "Title = " + title +
                ", Message = " + message +
                ", FaultType = DataModels.FaultType.ValidationError" +
                ", Severity = DataModels.Severity.NonCritical" +
                ", ApplicationTier = DATAMODELS.ApplicationTier.BusinessDomain";
        }

        private async Task<XDocument> InitiateMultipleBatchCall(AIAAMiddleLayerRequest request, Models.SaaS.DataProvider dataProvider, WorkItemsList WorkItemsListt)
        {
            //WorkItemsListt.WorkItems = new List<DataModels.Common.WorkItem>();
            //logger.Info("Ticket Pull Started Multibatch : Method : InitiateMultipleBatchCall : Ticket Type" + request.TicketType + " DDName: " + request.uploadTicketRequest.DDName);
            List<Models.SaaS.WorkItem> workItems = new List<Models.SaaS.WorkItem>();
           // BusinessDomain.Entities.IServiceCaller serviceCaller = this.NinjectKernel.Get<BusinessDomain.Entities.IServiceCaller>(BusinessDomain.Entities.BusinessDomainEntityType.ServiceCaller.ToString());
            List<string> faults = new List<string>();
            AIAAMiddleLayerResponse middleLayerResponse = new AIAAMiddleLayerResponse();
            //Task.Factory.StartNew(() =>
            //{
            var xDoc1 = new XDocument();
            var xDoc = new XDocument();
            var result = new XDocument();
            try
            {
                int pagenumber = 2;
                var cloneddataProvider = dataProvider;
                do
                {
                    if (dataProvider.InputRequestValues == "ProjectId,WorkItemTypeUId,BatchSize,PageNumber")
                    {
                        cloneddataProvider = FilldataProvider(dataProvider, request);
                    }
                    else
                    {
                        cloneddataProvider = dataProvider;
                    }
                    if (!string.IsNullOrEmpty(dataProvider.InputRequestValues))
                    {
                        var inputRequestValues = dataProvider.InputRequestValues.Split(',').ToList();
                        //inputRequestValues[2] = "\"ReportedOnAtSourceStart\":\"" + request.StartDate.ToString(dataProvider.FilterDateFormat) + request.StartTime + "\"";
                        //inputRequestValues[3] = "\"ReportedOnAtSourceEnd\":\"" + request.EndDate.ToString(dataProvider.FilterDateFormat) + request.EndTime + "\"";
                        inputRequestValues[3] = "\"PageNumber\":\"" + pagenumber + "\"}";
                        dataProvider.InputRequestValues = string.Join(",", inputRequestValues);
                    }
                    ServiceCallerRequest serviceCallerRequest = new ServiceCallerRequest()
                    {
                        Name = cloneddataProvider.Name,
                        Content = cloneddataProvider.InputRequestValues,
                        AuthProvider = cloneddataProvider.AuthProvider,
                        JsonRootNode = cloneddataProvider.JsonRootNode,
                        Accept = cloneddataProvider.Accept,
                        HttpVerbName = cloneddataProvider.Method,
                        MIMEMediaType = cloneddataProvider.MIMEMediaType,
                        ServiceUrl = cloneddataProvider.ServiceUrl
                    };
                    pagenumber++;
                    //var task = Task.Run(async () => await serviceCaller.GetDocUsingService(serviceCallerRequest, faults).ConfigureAwait(false));
                    //task.Wait();
                    //xDoc = task.Result;
                    xDoc = await _serviceCallerService.GetDocUsingService(serviceCallerRequest, faults).ConfigureAwait(false);
                    if (xDoc != null && xDoc.Root.Elements().Any())
                    {
                        //dataProvider.DataFormatter.XDocument = xDoc;
                        //IDataFormatter dataFormatter = this.NinjectKernel.Get<IDataFormatter>(BusinessDomainEntityType.DataFormatter.ToString());
                        //var task1 = Task.Run(async () => await dataFormatter.ExecuteDataFormatter(dataProvider.DataFormatter, request, faults).ConfigureAwait(false));
                        //task1.Wait();
                        //xDoc = task1.Result;
                        #region Call DataFormatter
                        if (dataProvider.DataFormatter != null)
                        {
                            dataProvider.DataFormatter.XDocument = xDoc;
                           // IDataFormatter dataFormatter = this.NinjectKernel.Get<IDataFormatter>(BusinessDomainEntityType.DataFormatter.ToString());
                            xDoc = await ExecuteDataFormatter(dataProvider.DataFormatter, request, faults).ConfigureAwait(false);
                        }
                        #endregion
                        if (xDoc != null && xDoc.Root.Elements().Any())
                        {
                            if (xDoc != null && xDoc.DescendantNodes().Any())
                                middleLayerResponse.Response = JsonConvert.SerializeXNode(xDoc, Newtonsoft.Json.Formatting.None, true);
                            else
                                middleLayerResponse.Response = string.Empty;
                            //string respone = middleLayerResponse != null ? JsonConvert.SerializeObject(middleLayerResponse) : string.Empty;
                            Models.SaaS.WorkItemsList workitemsresult = JsonConvert.DeserializeObject<Models.SaaS.WorkItemsList>(middleLayerResponse.Response);
                            WorkItemsListt.WorkItems.AddRange(workitemsresult.WorkItems);
                        }
                    }

                } while (pagenumber <= Convert.ToInt32(xDoc.Descendants("TotalPageCount").ToList()[0].Value));
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return xDoc;
            //});
        }
        public async Task<XDocument> ExecuteDataFormatter(Models.SaaS.DataFormatter dataFormatterModel, AIAAMiddleLayerRequest request, List<string> faults)
        {
          //  Log.Information("Inside DataFormatter.cs ->Method Name: ExecuteDataFormatter");
            XDocument doc = new XDocument();
            if (faults == null)
                faults = new List<string>();
            if (dataFormatterModel != null)
            {
              // IDataFormatter dataFormatter = this.NinjectKernel.Get<DATAFORMATTERS.IDataFormatter>(dataFormatterModel.DataFormatterType.ToString());

                //if (dataFormatter != null)
                //{
                    try
                    {
                       // Log.Information("Before calling Format");
                        doc = await Format(dataFormatterModel, request).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        #region Add Fault Exception
                        //Log.Error("Exception occured at DataFormatter_ExecuteDataFormatter method- " + ex.Message + ex.StackTrace, ex);
                        faults.Add(CreateExceptionFault(ex));
                        faults.Add(ex.Message);

                        #endregion
                    }
             //   }
                //else
                //{
                //    #region Add Validation Fault

                //    faults.Add(this.CreateValidationFault(@"Invalid or Unsupported DatFormatter or null Source data", ""));
                //    #endregion
                //}
            }
            else
            {
                #region Add Validation Fault

                faults.Add(this.CreateValidationFault("Validation failed at dataformatter component", "dataformatter model is empty"));
                #endregion
            }
         //   Log.Information("End of DataFormatter.cs ->Method Name: ExecuteDataFormatter");
            return doc;
        }

        protected string CreateExceptionFault(Exception ex)
        {
            return "Title = Exception has occurred" +
                ", Message = " + ex.Message +
                ", FaultType = DataModels.FaultType.Exception" +
                ", Severity = DataModels.Severity.Critical" +
                ", ApplicationTier = DATAMODELS.ApplicationTier.BusinessDomain" +
                ", StackTrace = " + ex.StackTrace +
                ". InnerException = " + ex.InnerException == null ? null : ex.InnerException.ToString();
        }


        public Models.SaaS.DataProvider FilldataProvider(Models.SaaS.DataProvider dataProvider, Models.SaaS.AIAAMiddleLayerRequest request)
        {
            var dataProviderToBeFilled = dataProvider;
            InputRequestType inputRequestType = GetInputRequestType(dataProvider.InputRequestType);
            switch (inputRequestType)
            {
                case InputRequestType.JsonObject:
                    dataProvider.InputRequestValues = CreateJsonRequest(dataProvider, request);
                    break;
                case InputRequestType.URL:
                    dataProvider = FillServiceUrl(dataProvider, request);
                    break;
                case InputRequestType.JSONandURL:
                    dataProvider = FillServiceUrl(dataProvider, request);
                    dataProvider.InputRequestValues = CreateJsonRequest(dataProvider, request);

                    break;
                default:
                    break;
            }
            return dataProviderToBeFilled;
        }

        public Models.SaaS.DataProvider FillServiceUrl(Models.SaaS.DataProvider dataProvider, Models.SaaS.AIAAMiddleLayerRequest request)
        {
            string url = dataProvider.ServiceUrl;
            List<string> defaultValues = new List<string>();
            List<string> defaultKeys = new List<string>();
            if (!string.IsNullOrEmpty(dataProvider.InputRequestValues))
            {
                List<string> inputRequestValues = dataProvider.InputRequestValues.Split(',').ToList();
                if (!string.IsNullOrEmpty(dataProvider.DefaultKeys))
                {
                    defaultValues = dataProvider.DefaultValues.Split(',').ToList();
                    defaultKeys = dataProvider.DefaultKeys.Split(',').ToList();
                }
                for (int i = 0; i < inputRequestValues.Count; i++)
                {
                    if (dataProvider.DefaultKeys.Contains(Convert.ToString(i)) && string.IsNullOrEmpty(Convert.ToString(request[inputRequestValues[i]])))
                        url = url.Replace("{" + i + "}", Convert.ToString(defaultValues[i]));
                    else
                        url = url.Replace("{" + i + "}", Convert.ToString(request[inputRequestValues[i]]));
                }
            }
            dataProvider.ServiceUrl = url;
            return dataProvider;
        }

        public string CreateJsonRequest(Models.SaaS.DataProvider dataProvider, Models.SaaS.AIAAMiddleLayerRequest request)
        {
            Dictionary<string, string> dictObject = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(dataProvider.InputRequestKeys) && !string.IsNullOrEmpty(dataProvider.InputRequestValues))
            {
                List<string> inputRequestKeys = dataProvider.InputRequestKeys.Split(',').ToList();
                List<string> inputRequestValues = dataProvider.InputRequestValues.Split(',').ToList();
                for (int i = 0; i < inputRequestKeys.Count; i++)
                {
                    dictObject.Add(inputRequestKeys[i], Convert.ToString(request[inputRequestValues[i]]));
                }
            }
            return JsonConvert.SerializeObject(dictObject);
            //return new JavaScriptSerializer().Serialize(dictObject.ToDictionary(item => Convert.ToString(item.Key), item => Convert.ToString(item.Value)));
        }

        public InputRequestType GetInputRequestType(string requestType)
        {
            InputRequestType inputRequestType = InputRequestType.None;

            Enum.TryParse(requestType, out inputRequestType);

            return inputRequestType;
        }
        public enum InputRequestType
        {
            /// <summary>
            /// The None
            /// </summary>
            None,
            /// <summary>
            /// The JsonObject
            /// </summary>
            JsonObject,

            /// <summary>
            /// The URL
            /// </summary>
            URL,
            /// <summary>
            /// JSONandURL
            /// </summary>
            JSONandURL
        }

        public async Task<XDocument> Format(Models.SaaS.DataFormatter dataFormatter, AIAAMiddleLayerRequest request)
        {
           // Log.Information("Inside XsltDataFormatter.cs -> Method Name : Format");
            var settings = new XsltSettings();
            settings.EnableScript = true;
            XDocument outputXDoc = new XDocument();
            XslCompiledTransform xslt = new XslCompiledTransform();
            XsltArgumentList args = new XsltArgumentList();

            if (dataFormatter != null)
            {
                if (dataFormatter.Json == "XPATH")
                {
                    FileInfo xsltFile = new FileInfo(dataFormatter.XsltFilePath);
                    #region Transform XML
                    try
                    {
                        await Task.Factory.StartNew(() =>
                        {
                            if (!string.IsNullOrEmpty(dataFormatter.XsltArguments) && request != null)
                            {
                                var XsltArguments = dataFormatter.XsltArguments.Split(',');
                                foreach (var XsltArgument in XsltArguments)
                                {
                                    if (!string.IsNullOrEmpty(Convert.ToString(request[XsltArgument])))
                                        args.AddParam(XsltArgument, "", request[XsltArgument]);
                                }
                            }

                            XPathDocument mydata = new XPathDocument((dataFormatter.XsltFilePath));
                           // Log.Information("xslt file path: " + dataFormatter.XsltFilePath);

                            xslt.Load(mydata, settings, null);
                            XmlDocument sourceXDoc = new XmlDocument();
                            if (dataFormatter.XDocument != null)
                            {
                                sourceXDoc.Load(dataFormatter.XDocument.CreateReader());
                                using (var writer = outputXDoc.CreateWriter())
                                {
                                    xslt.Transform(sourceXDoc, args, writer);
                                }
                            }
                        }).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        #region Throw Exception
                       // Log.Information("Exception of XsltDataFormatter.cs -> Method Name : Format " + ex.Message);
                        throw ex;
                        #endregion
                    }
                    #endregion
                }
                else if (File.Exists((dataFormatter.XsltFilePath)))
                {
                    FileInfo xsltFile = new FileInfo(dataFormatter.XsltFilePath);
                    //string dataproviderName = Path.GetFileNameWithoutExtension(xsltFile.Name);
                    //if(!Directory.Exists(source.inputRequest.LogFilePath + @"\" + dataproviderName))
                    //    Directory.CreateDirectory(source.inputRequest.LogFilePath + @"\" + dataproviderName);
                    //string LogPath = @"C:\AIAA\AITD_2.0\myWizardAITD\AITD_1.20-branch\mywizardauth_InputJson_" + inputRequest.ProjectId + "_" + DateTime.Now.ToString("mm_dd_yyyy_mm_ss") + ".txt";
                    //string logPath = source.inputRequest.LogFilePath + @"\" + dataproviderName + @"\outputXSLT_" + DateTime.Now.ToString("mm_dd_yyyy_mm_ss") + ".txt";                    
                    #region Transform XML
                    try
                    {
                        await Task.Factory.StartNew(() =>
                        {
                            if (!string.IsNullOrEmpty(dataFormatter.XsltArguments) && request != null)
                            {
                                var XsltArguments = dataFormatter.XsltArguments.Split(',');
                                foreach (var XsltArgument in XsltArguments)
                                {
                                    if (!string.IsNullOrEmpty(Convert.ToString(request[XsltArgument])))
                                        args.AddParam(XsltArgument, "", request[XsltArgument]);
                                }
                            }
                            xslt.Load((dataFormatter.XsltFilePath), settings, null);
                            XmlDocument sourceXDoc = new XmlDocument();
                            if (dataFormatter.XDocument != null)
                            {
                                sourceXDoc.Load(dataFormatter.XDocument.CreateReader());
                                using (var writer = outputXDoc.CreateWriter())
                                {
                                    xslt.Transform(sourceXDoc, args, writer);
                                }
                                //File.CreateText(logPath).Close();
                                //File.WriteAllText(logPath, outputXDoc.ToString());
                            }
                            //var str = JsonConvert.SerializeObject(outputXDoc);
                            //var doc = JsonConvert.DeserializeXmlNode(str);
                        }).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        #region Throw Exception

                        //File.CreateText(logPath).Close();
                        //File.WriteAllText(logPath, outputXDoc.ToString());
                        //return outputXDoc;
                        throw ex;

                        #endregion
                    }

                    #endregion
                }
                else
                {
                    #region Throw Exception

                    throw new Exception(String.Format(@"Looks like some issues with DataFormatter {0}. Did you configure correct Xslt FilePath {1} "
                                                    , dataFormatter.Name
                                                    , AppDomain.CurrentDomain.BaseDirectory + (dataFormatter.XsltFilePath)));

                    #endregion
                }
            }
            else
            {
                #region Log Info

                #endregion
            }
          //  Log.Information("End of XsltDataFormatter.cs -> Method Name : Format");

            return outputXDoc;
        }

        public async Task<string> GetHistoricalIterationDetails(List<string> faults, AIAAMiddleLayerRequest request)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(PAMTicketPullService), nameof(GetHistoricalIterationDetails), "START", Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
            var xDoc = new XDocument();
            var clonedXDoc = new XDocument(); int currentprovidercount = 0;
            AIAAMiddleLayerResponse middleLayerResponse = new AIAAMiddleLayerResponse();
            List<Iteration> iterations = new List<Iteration>();
            string result = string.Empty;
            //DATACOMMON.UploadHistoricalTicketRequest ticketRequest = new DATAMODELS.Common.UploadHistoricalTicketRequest();
            //List<DATACOMMON.DeliveryConstructVersion> construct = new List<DATACOMMON.DeliveryConstructVersion>();
            //BusinessDomain.Entities.IServiceCaller serviceCaller = this.NinjectKernel.Get<BusinessDomain.Entities.IServiceCaller>(BusinessDomain.Entities.BusinessDomainEntityType.ServiceCaller.ToString());
            try
            {

                int providercount = request.DataProviders.Count;
                if (request != null && request.DataProviders != null)
                {
                    foreach (Models.SaaS.DataProvider dataProvider in request.DataProviders)
                    {
                        currentprovidercount++;
                        var cloneddataProvider = dataProvider;
                        if (string.IsNullOrEmpty(Convert.ToString(dataProvider.TicketType)))
                        {
                            faults.Add(this.CreateValidationFault("Missing validation at dataProvider Read method", "ProjetId cannot be null"));
                        }
                        else
                        {
                            try
                            {
                                cloneddataProvider = FilldataProvider(dataProvider, request);
                                if (!string.IsNullOrEmpty(dataProvider.InputRequestValues))
                                {
                                    var inputRequestValues = dataProvider.InputRequestValues.Split(',').ToList();
                                    //inputRequestValues[2] = "\"ReportedOnAtSourceStart\":\"" + request.StartDate.ToString(dataProvider.FilterDateFormat) + request.StartTime + "\"";
                                    //inputRequestValues[3] = "\"ReportedOnAtSourceEnd\":\"" + request.EndDate.ToString(dataProvider.FilterDateFormat) + request.EndTime + "\"";
                                    dataProvider.InputRequestValues = string.Join(",", inputRequestValues);
                                }
                               ServiceCallerRequest serviceCallerRequest = new ServiceCallerRequest()
                                {
                                    Name = cloneddataProvider.Name,
                                    Content = cloneddataProvider.InputRequestValues,
                                    AuthProvider = cloneddataProvider.AuthProvider,
                                    JsonRootNode = cloneddataProvider.JsonRootNode,
                                    Accept = cloneddataProvider.Accept,
                                    HttpVerbName = cloneddataProvider.Method,
                                    MIMEMediaType = cloneddataProvider.MIMEMediaType,
                                    ServiceUrl = cloneddataProvider.ServiceUrl,

                                };
                                xDoc = await _serviceCallerService.GetDocUsingService(serviceCallerRequest, faults).ConfigureAwait(false);
                                if (xDoc != null && xDoc.Root.Elements().Any())
                                {
                                    try
                                    {
                                        #region Call DataFormatter
                                        if (dataProvider.DataFormatter != null)
                                        {
                                            dataProvider.DataFormatter.XDocument = xDoc;
                                            //Models.SaaS.IDataFormatter dataFormatter = this.NinjectKernel.Get<IDataFormatter>(BusinessDomainEntityType.DataFormatter.ToString());
                                            xDoc = await ExecuteDataFormatter(dataProvider.DataFormatter, request, faults).ConfigureAwait(false);
                                        }
                                        #endregion
                                        if (xDoc != null && xDoc.Root.Elements().Any() && Convert.ToInt32(xDoc.Descendants("TotalRecordCount").ToList()[0].Value) != 0 && !string.IsNullOrEmpty(xDoc.Descendants("TotalRecordCount").ToList()[0].Value) && !string.IsNullOrEmpty(xDoc.Descendants("TotalPageCount").ToList()[0].Value))
                                        {
                                            if (Convert.ToInt32(xDoc.Descendants("TotalRecordCount").ToList()[0].Value) > 0) // && (Convert.ToInt32(xDoc.Descendants("CurrentPage").ToList()[0].Value) == Convert.ToInt32(xDoc.Descendants("TotalPageCount").ToList()[0].Value)))
                                            {
                                                if (xDoc != null && xDoc.Root.Elements().Any())
                                                {
                                                    if (xDoc != null && xDoc.DescendantNodes().Any())
                                                        middleLayerResponse.Response = JsonConvert.SerializeXNode(xDoc, Newtonsoft.Json.Formatting.None, true);
                                                    else
                                                        middleLayerResponse.Response = string.Empty;
                                                    IterationsList iterationsresult = JsonConvert.DeserializeObject<IterationsList>(middleLayerResponse.Response);
                                                    IterationsListt.Iterations = new List<Iteration>();
                                                    IterationsListt.Iterations.AddRange(iterationsresult.Iterations);

                                                    if (Convert.ToInt32(xDoc.Descendants("TotalPageCount").ToList()[0].Value) > 1)
                                                    {
                                                        xDoc = await InitiateMultipleBatchCall(request, dataProvider, IterationsListt);
                                                    }

                                                    foreach (var item in IterationsListt.Iterations)
                                                    {
                                                        if (item.IterationTypeUId == "00200390-0010-0000-0000-000000000000")
                                                        {
                                                            item.IterationType = "Release";
                                                        }
                                                        if (item.IterationTypeUId == "00200390-0020-0000-0000-000000000000")
                                                        {
                                                            item.IterationType = "Sprint";
                                                        }
                                                    }
                                                    middleLayerResponse.Iterations = IterationsListt;
                                                    result = middleLayerResponse != null ? JsonConvert.SerializeObject(middleLayerResponse.Iterations) : string.Empty;
                                                }
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        //logger.Info(" GetHistoricalTicketDetails : DDName: " + request.uploadTicketRequest.DDName + " Exception" +ex.Message.ToString());
                                        throw new Exception("Error occurred while converting response to AIAA format using data formatter - " + GetFaultString(faults));
                                    }
                                }
                                else
                                    faults.Add("XML document is empty.Error Occured while fetching tickets.Please try again.");
                            }
                            catch (Exception ee)
                            {
                                string s = ee.Message.ToString();
                                xDoc = new XDocument();
                                //logger.Info(" GetHistoricalTicketDetails : DDName: " + request.uploadTicketRequest.DDName + " Exception" + ee.Message.ToString());
                                throw new Exception("Error occurred while reading data from Service Provider - " + GetFaultString(faults));
                            }
                        }
                        if (isBreak == 1)
                        {
                            break;
                        }
                    }
                }
                else
                {
                    faults.Add(this.CreateValidationFault("Missing validation at DataProcider Read method", "No Data providers found"));
                }

            }
            catch (Exception ex)
            {
                xDoc = new XDocument();
                //logger.Info(" GetHistoricalTicketDetails : DDName: " + request.uploadTicketRequest.DDName + " Exception" + ex.Message.ToString());
                throw ex;

            }
            xDoc = new XDocument(
                                    //new XElement("myWizardDataProvider",
                                    new XElement("ResponseMessage", GetFaultString(faults)));
            return result;
        }

        public async Task<string> GetWorkItemsFromENS(List<string> faults, AIAAMiddleLayerRequest request)
        {
            {
                var xDoc = new XDocument();
                var clonedXDoc = new XDocument(); int currentprovidercount = 0;
                AIAAMiddleLayerResponse middleLayerResponse = new AIAAMiddleLayerResponse();
                List<Models.SaaS.WorkItem> workItems = new List<Models.SaaS.WorkItem>();
                WorkItemsListt.WorkItems = new List<Models.SaaS.WorkItem>();
                string result = string.Empty;
                //BusinessDomain.Entities.IServiceCaller serviceCaller = this.NinjectKernel.Get<BusinessDomain.Entities.IServiceCaller>(BusinessDomain.Entities.BusinessDomainEntityType.ServiceCaller.ToString());
                try
                {

                    int providercount = request.DataProviders.Count;
                    if (request != null && request.DataProviders != null)
                    {
                        foreach (Models.SaaS.DataProvider dataProvider in request.DataProviders)
                        {
                            currentprovidercount++;
                            var cloneddataProvider = dataProvider;
                            if (string.IsNullOrEmpty(Convert.ToString(dataProvider.TicketType)))
                            {
                                faults.Add(this.CreateValidationFault("Missing validation at dataProvider Read method", "ProjetId cannot be null"));
                            }
                            else
                            {
                                try
                                {
                                    cloneddataProvider = FilldataProvider(dataProvider, request);
                                    if (!string.IsNullOrEmpty(dataProvider.InputRequestValues))
                                    {
                                        var inputRequestValues = dataProvider.InputRequestValues.Split(',').ToList();
                                        dataProvider.InputRequestValues = string.Join(",", inputRequestValues);
                                    }
                                    ServiceCallerRequest serviceCallerRequest = new ServiceCallerRequest()
                                    {
                                        Name = cloneddataProvider.Name,
                                        Content = cloneddataProvider.InputRequestValues,
                                        AuthProvider = cloneddataProvider.AuthProvider,
                                        JsonRootNode = cloneddataProvider.JsonRootNode,
                                        Accept = cloneddataProvider.Accept,
                                        HttpVerbName = cloneddataProvider.Method,
                                        MIMEMediaType = cloneddataProvider.MIMEMediaType,
                                        ServiceUrl = cloneddataProvider.ServiceUrl,

                                    };
                                    xDoc = await _serviceCallerService.GetDocUsingService(serviceCallerRequest, faults).ConfigureAwait(false);
                                    if (xDoc != null && xDoc.Root.Elements().Any())
                                    {
                                        try
                                        {
                                            #region Call DataFormatter
                                            if (dataProvider.DataFormatter != null)
                                            {
                                                dataProvider.DataFormatter.XDocument = xDoc;
                                                //IDataFormatter dataFormatter = this.NinjectKernel.Get<IDataFormatter>(BusinessDomainEntityType.DataFormatter.ToString());
                                                xDoc = await ExecuteDataFormatter(dataProvider.DataFormatter, request, faults).ConfigureAwait(false);
                                            }
                                            #endregion
                                            if (xDoc != null && xDoc.Root.Elements().Any())
                                            {
                                                if (xDoc != null && xDoc.DescendantNodes().Any())
                                                    middleLayerResponse.Response = JsonConvert.SerializeXNode(xDoc, Formatting.None, true);
                                                else
                                                    middleLayerResponse.Response = string.Empty;
                                                WorkItemsList workitemsresult = JsonConvert.DeserializeObject<WorkItemsList>(middleLayerResponse.Response);
                                                WorkItemsListt.WorkItems.AddRange(workitemsresult.WorkItems);
                                                middleLayerResponse.WorkItems = WorkItemsListt;
                                                result = middleLayerResponse != null ? JsonConvert.SerializeObject(middleLayerResponse.WorkItems) : string.Empty;
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            throw new Exception("Error occurred while converting response to AIAA format using data formatter - " + GetFaultString(faults));
                                        }
                                    }
                                    else
                                        faults.Add("XML document is empty.Error Occured while fetching tickets.Please try again.");
                                }
                                catch (Exception ee)
                                {
                                    string s = ee.Message.ToString();
                                    xDoc = new XDocument();
                                    throw new Exception("Error occurred while reading data from Service Provider - " + GetFaultString(faults));
                                }
                            }
                            if (isBreak == 1)
                            {
                                break;
                            }
                        }
                    }
                    else
                    {
                        faults.Add(this.CreateValidationFault("Missing validation at DataProcider Read method", "No Data providers found"));
                    }

                }
                catch (Exception ex)
                {
                    //logger.Info(" GetHistoricalTicketDetails : DDName: " + request.uploadTicketRequest.DDName + " Exception" + ex.Message.ToString());
                    throw ex;

                }
                return result;
            }
        }

        public async Task<string> GetIterationsFromENS(List<string> faults, AIAAMiddleLayerRequest request)
        {
            var xDoc = new XDocument();
            var clonedXDoc = new XDocument(); int currentprovidercount = 0;
            AIAAMiddleLayerResponse middleLayerResponse = new AIAAMiddleLayerResponse();
            List<Iteration> iterations = new List<Iteration>();
            string result = string.Empty;
            //DATACOMMON.UploadHistoricalTicketRequest ticketRequest = new DATAMODELS.Common.UploadHistoricalTicketRequest();
            //List<DATACOMMON.DeliveryConstructVersion> construct = new List<DATACOMMON.DeliveryConstructVersion>();
            //BusinessDomain.Entities.IServiceCaller serviceCaller = this.NinjectKernel.Get<BusinessDomain.Entities.IServiceCaller>(BusinessDomain.Entities.BusinessDomainEntityType.ServiceCaller.ToString());
            try
            {

                int providercount = request.DataProviders.Count;
                if (request != null && request.DataProviders != null)
                {
                    foreach (Models.SaaS.DataProvider dataProvider in request.DataProviders)
                    {
                        currentprovidercount++;
                        var cloneddataProvider = dataProvider;
                        if (string.IsNullOrEmpty(Convert.ToString(dataProvider.TicketType)))
                        {
                            faults.Add(this.CreateValidationFault("Missing validation at dataProvider Read method", "ProjetId cannot be null"));
                        }
                        else
                        {
                            try
                            {
                                cloneddataProvider = FilldataProvider(dataProvider, request);
                                if (!string.IsNullOrEmpty(dataProvider.InputRequestValues))
                                {
                                    var inputRequestValues = dataProvider.InputRequestValues.Split(',').ToList();
                                    //inputRequestValues[2] = "\"ReportedOnAtSourceStart\":\"" + request.StartDate.ToString(dataProvider.FilterDateFormat) + request.StartTime + "\"";
                                    //inputRequestValues[3] = "\"ReportedOnAtSourceEnd\":\"" + request.EndDate.ToString(dataProvider.FilterDateFormat) + request.EndTime + "\"";
                                    dataProvider.InputRequestValues = string.Join(",", inputRequestValues);
                                }
                                ServiceCallerRequest serviceCallerRequest = new ServiceCallerRequest()
                                {
                                    Name = cloneddataProvider.Name,
                                    Content = cloneddataProvider.InputRequestValues,
                                    AuthProvider = cloneddataProvider.AuthProvider,
                                    JsonRootNode = cloneddataProvider.JsonRootNode,
                                    Accept = cloneddataProvider.Accept,
                                    HttpVerbName = cloneddataProvider.Method,
                                    MIMEMediaType = cloneddataProvider.MIMEMediaType,
                                    ServiceUrl = cloneddataProvider.ServiceUrl,

                                };
                                xDoc = await _serviceCallerService.GetDocUsingService(serviceCallerRequest, faults).ConfigureAwait(false);
                                if (xDoc != null && xDoc.Root.Elements().Any())
                                {
                                    try
                                    {
                                        #region Call DataFormatter
                                        if (dataProvider.DataFormatter != null)
                                        {
                                            dataProvider.DataFormatter.XDocument = xDoc;
                                            //IDataFormatter dataFormatter = this.NinjectKernel.Get<IDataFormatter>(BusinessDomainEntityType.DataFormatter.ToString());
                                            xDoc = await ExecuteDataFormatter(dataProvider.DataFormatter, request, faults).ConfigureAwait(false);
                                        }
                                        #endregion

                                        if (xDoc != null && xDoc.Root.Elements().Any())
                                        {
                                            if (xDoc != null && xDoc.DescendantNodes().Any())
                                                middleLayerResponse.Response = JsonConvert.SerializeXNode(xDoc, Formatting.None, true);
                                            else
                                                middleLayerResponse.Response = string.Empty;
                                            IterationsList iterationsresult = JsonConvert.DeserializeObject<IterationsList>(middleLayerResponse.Response);
                                            IterationsListt.Iterations = new List<Iteration>();
                                            IterationsListt.Iterations.AddRange(iterationsresult.Iterations);

                                            foreach (var item in IterationsListt.Iterations)
                                            {
                                                if (item.IterationTypeUId == "00200390-0010-0000-0000-000000000000")
                                                {
                                                    item.IterationType = "Release";
                                                }
                                                if (item.IterationTypeUId == "00200390-0020-0000-0000-000000000000")
                                                {
                                                    item.IterationType = "Sprint";
                                                }
                                            }
                                            middleLayerResponse.Iterations = IterationsListt;
                                            result = middleLayerResponse != null ? JsonConvert.SerializeObject(middleLayerResponse.Iterations) : string.Empty;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        //logger.Info(" GetHistoricalTicketDetails : DDName: " + request.uploadTicketRequest.DDName + " Exception" +ex.Message.ToString());
                                        throw new Exception("Error occurred while converting response to AIAA format using data formatter - " + GetFaultString(faults));
                                    }
                                }
                                else
                                    faults.Add("XML document is empty.Error Occured while fetching tickets.Please try again.");
                            }
                            catch (Exception ee)
                            {
                                string s = ee.Message.ToString();
                                xDoc = new XDocument();
                                //logger.Info(" GetHistoricalTicketDetails : DDName: " + request.uploadTicketRequest.DDName + " Exception" + ee.Message.ToString());
                                throw new Exception("Error occurred while reading data from Service Provider - " + GetFaultString(faults));
                            }
                        }
                        if (isBreak == 1)
                        {
                            break;
                        }
                    }
                }
                else
                {
                    faults.Add(this.CreateValidationFault("Missing validation at DataProcider Read method", "No Data providers found"));
                }

            }
            catch (Exception ex)
            {
                xDoc = new XDocument();
                //logger.Info(" GetHistoricalTicketDetails : DDName: " + request.uploadTicketRequest.DDName + " Exception" + ex.Message.ToString());
                throw ex;

            }
            xDoc = new XDocument(
                                    //new XElement("myWizardDataProvider",
                                    new XElement("ResponseMessage", GetFaultString(faults)));
            return result;
        }

        public async Task<string> GetHistoricalTicketDetails(List<string> faults, AIAAMiddleLayerRequest request)
        {            
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(PAMTicketPullService), nameof(GetHistoricalTicketDetails), "START", Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
            var xDoc = new XDocument();
            var clonedXDoc = new XDocument(); int currentprovidercount = 0;
            AIAAMiddleLayerResponse middleLayerResponse = new AIAAMiddleLayerResponse();
            List<Models.SaaS.Ticket> tickets = new List<Models.SaaS.Ticket>();
            TicketsList.Tickets = new List<Models.SaaS.Ticket>();
            TaskList.Tasks = new List<Models.SaaS.Tasks>();
            string result = string.Empty;
            //BusinessDomain.Entities.IServiceCaller serviceCaller = this.NinjectKernel.Get<BusinessDomain.Entities.IServiceCaller>(BusinessDomain.Entities.BusinessDomainEntityType.ServiceCaller.ToString());
            int i = 0, j = 0;
            try
            {
                int providercount = request.DataProviders.Count;
                if (request != null && request.DataProviders != null)
                {
                    foreach (Models.SaaS.DataProvider dataProvider in request.DataProviders)
                    {
                        currentprovidercount++;
                        var cloneddataProvider = dataProvider;

                        try
                        {
                            cloneddataProvider = FilldataProvider(dataProvider, request);
                            cloneddataProvider.ServiceUrl += "per_page=" + request.BatchSize;
                            var serviceUrl1 = cloneddataProvider.ServiceUrl;
                            foreach (var ticketType in request.TicketTypes)
                            {
                                cloneddataProvider.ServiceUrl = serviceUrl1;
                                // start date and end date, ticket type null check
                                if (ticketType.StartDate != null && ticketType.EndDate != null && ticketType.TicketType != null)
                                {
                                    cloneddataProvider.ServiceUrl += "&date_start=" + ToUnixTime(Convert.ToDateTime(ticketType.StartDate));
                                    cloneddataProvider.ServiceUrl += "&date_end=" + ToUnixTime(Convert.ToDateTime(ticketType.EndDate));
                                    //cloneddataProvider.ServiceUrl += "&ticketType=" + ticketType.TicketType;
                                    var serviceUrl2 = cloneddataProvider.ServiceUrl;
                                    int page = 0;
                                    int ticketCount = 0;
                                    int taskCount = 0;
                                    //TicketsList = new DATACOMMON.TicketList();
                                    do
                                    {
                                        ticketCount = 0;
                                        cloneddataProvider.ServiceUrl = serviceUrl2;
                                        cloneddataProvider.ServiceUrl += "&page=" + page;

                                        ServiceCallerRequest serviceCallerRequest = new ServiceCallerRequest()
                                        {
                                            Name = cloneddataProvider.Name,
                                            Content = cloneddataProvider.InputRequestValues,
                                            AuthProvider = cloneddataProvider.AuthProvider,
                                            JsonRootNode = cloneddataProvider.JsonRootNode,
                                            Accept = cloneddataProvider.Accept,
                                            HttpVerbName = cloneddataProvider.Method,
                                            MIMEMediaType = cloneddataProvider.MIMEMediaType,
                                            ServiceUrl = cloneddataProvider.ServiceUrl,

                                        };                                        
                                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(PAMTicketPullService), nameof(GetHistoricalTicketDetails), "Before calling GetDocUsingService - URL" + cloneddataProvider.ServiceUrl, Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
                                        xDoc = await _serviceCallerService.GetDocUsingService(serviceCallerRequest, faults).ConfigureAwait(false);
                                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(PAMTicketPullService), nameof(GetHistoricalTicketDetails), "After Calling GetDocUsingService", Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty);                                        

                                        if (xDoc != null && xDoc.Root != null && xDoc.Root.Elements().Any())
                                        {
                                            try
                                            {
                                                #region Call DataFormatter
                                                if (dataProvider.DataFormatter != null)
                                                {
                                                    dataProvider.DataFormatter.XDocument = xDoc;
                                                    //IDataFormatter dataFormatter = this.NinjectKernel.Get<IDataFormatter>(BusinessDomainEntityType.DataFormatter.ToString());
                                                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(PAMTicketPullService), nameof(GetHistoricalTicketDetails), "Before ExecuteDataFormatter", Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty);                                                    
                                                    xDoc = await ExecuteDataFormatter(dataProvider.DataFormatter, request, faults).ConfigureAwait(false);
                                                    //Log.Information("after ExecuteDataFormatter" + xDoc);

                                                }
                                                #endregion
                                                if (xDoc != null && xDoc.Root != null && xDoc.Root.Elements().Any())
                                                {
                                                    if (xDoc != null && xDoc.DescendantNodes().Any())
                                                        middleLayerResponse.Response = JsonConvert.SerializeXNode(xDoc, Formatting.None, true);
                                                    else
                                                        middleLayerResponse.Response = string.Empty;                                                    
                                                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(PAMTicketPullService), nameof(GetHistoricalTicketDetails), "Before Deserialization ticket list result", Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty);

                                                    TicketList ticketlistresult = JsonConvert.DeserializeObject<TicketList>(middleLayerResponse.Response);
                                                    // DATACOMMON.TaskList tasklistresult = JsonConvert.DeserializeObject<DATACOMMON.TaskList>(middleLayerResponse.Response);
                                                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(PAMTicketPullService), nameof(GetHistoricalTicketDetails), "After Deserialization ticket list result", Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty);                                                   

                                                    #region Ticket
                                                    ticketlistresult.Tickets.RemoveAll(item => item == null);

                                                    List<Models.SaaS.Ticket> ticketResult = ticketlistresult.Tickets;
                                                    ticketResult.ForEach(k => k.ClientUId = request.ClientUId);
                                                    ticketResult.ForEach(k => k.EndToEndUId = request.EndToEndUId);

                                                    foreach (var item in ticketResult)
                                                    {
                                                        if (item?.NormalisedTicketType == "INCIDENT")
                                                        {
                                                            item.NormalisedTicketType = "Incidents";
                                                            continue;
                                                        }
                                                        if (item?.NormalisedTicketType == "PROBLEM")
                                                        {
                                                            item.NormalisedTicketType = "ProblemTickets";
                                                            continue;
                                                        }
                                                        if (item?.NormalisedTicketType == "SERVICEREQUEST")
                                                        {
                                                            item.NormalisedTicketType = "ServiceRequests";
                                                            continue;
                                                        }
                                                        if (item?.NormalisedTicketType == "CHANGEREQUEST")
                                                        {
                                                            item.NormalisedTicketType = "ChangeManagement";
                                                            continue;
                                                        }
                                                        if (item?.NormalisedTicketType == "TASK")
                                                        {
                                                            item.NormalisedTicketType = "Task";
                                                            continue;
                                                        }
                                                    }
                                                    //int i = 27;
                                                    foreach (var tkt in ticketResult)
                                                    {
                                                        // DateTime dtReport = System.DateTime.Now.AddDays(-i);
                                                        tkt.AssignedDateTime = FromUnixTime(tkt.XsltAssignedDateTime);
                                                        tkt.ReportedDateTime = FromUnixTime(tkt.XsltReportedDateTime);
                                                        tkt.LastModifiedTime = FromUnixTime(tkt.XsltLastModifiedTime);
                                                        tkt.DueDate = FromUnixTime(tkt.XsltDueDate);
                                                        tkt.RespondedDate = FromUnixTime(tkt.XsltRespondedDate).ToString();
                                                        tkt.ReopenedDate = FromUnixTime(tkt.XsltReopenedDate);
                                                        tkt.ResponseDate = FromUnixTime(tkt.XsltResponseDate);
                                                        //tkt.ResponseDate = FromUnixTime(tkt.ResponseDate).ToString();
                                                        tkt.ResolutionDate = FromUnixTime(tkt.XsltResolutionDate);
                                                        tkt.ResolutionDueDate = FromUnixTime(tkt.XsltResolutionDueDate);
                                                        tkt.ResponseDueDate = FromUnixTime(tkt.XsltResponseDueDate);
                                                        //tkt.ResponseDueDate = FromUnixTime(tkt.ResponseDueDate);
                                                        tkt.ClosedDate = FromUnixTime(tkt.XsltClosedDate);
                                                        tkt.CreatedOn = FromUnixTime(tkt.XsltCreatedOn);
                                                        tkt.ModifiedOn = FromUnixTime(tkt.XsltModifiedOn);
                                                        tkt.LastResolvedDate = FromUnixTime(tkt.XsltLastResolvedDate);
                                                        tkt.ActualStartOn = FromUnixTime(tkt.XsltActualStartOn);
                                                        tkt.ActualEndOn = FromUnixTime(tkt.XsltActualEndOn);
                                                        tkt.PlannedStartOn = FromUnixTime(tkt.XsltPlannedStartOn);
                                                        tkt.PlannedEndOn = FromUnixTime(tkt.XsltPlannedEndOn);
                                                        tkt.ApprovedDate = FromUnixTime(tkt.XsltApprovedDate);


                                                        if (!(string.IsNullOrEmpty(tkt.SLAResolution)) && (tkt.SLAResolution.Contains("false")))
                                                        {
                                                            tkt.SLAResolution = "Met";
                                                        }
                                                        else if (string.IsNullOrEmpty(tkt.SLAResolution))
                                                        {
                                                            tkt.SLAResolution = null;
                                                        }
                                                        else
                                                        {
                                                            tkt.SLAResolution = "Missed";
                                                        }

                                                        if (tkt.DeliveryConstruct == null)
                                                            tkt.DeliveryConstruct = new List<WINSERVICEMODELS.DeliveryConstruct>();
                                                        //Priority

                                                        if (tkt.Domain.ToLower() == "ao")
                                                        {
                                                            tkt.MYWPriority = MapPriority(tkt.MYWPriority);
                                                        }
                                                        if (tkt.ReassignmentCount < 0)
                                                        {
                                                            tkt.ReassignmentCount = 0;
                                                        }


                                                        //i--;
                                                        //if (i <= 0)
                                                        //{
                                                        //    i = 26;
                                                        //}    
                                                    }

                                                    TicketsList.Tickets.AddRange(ticketResult);

                                                    #endregion
                                                    page++;
                                                    ticketCount = TicketsList.Tickets.Count;


                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                throw new Exception("Error occurred while converting response to AIAA format using data formatter - " + GetFaultString(faults));
                                            }
                                        }
                                        else
                                            faults.Add("XML document is empty.Error Occured while fetching tickets.Please try again.");
                                        j++;
                                    } while (ticketCount > 0); //|| (taskCount > 0) add this for task
                                }
                                i++;
                            }
                            middleLayerResponse.Tickets = TicketsList;
                            // middleLayerResponse.Tasks = TaskList;
                            result = middleLayerResponse != null ? JsonConvert.SerializeObject(middleLayerResponse.Tickets) : string.Empty;
                        }
                        catch (Exception ee)
                        {
                            string s = ee.Message.ToString();
                            xDoc = new XDocument();
                            throw new Exception("Error occurred while reading data from Service Provider - " + GetFaultString(faults));
                        }
                        if (isBreak == 1)
                        {
                            break;
                        }
                    }
                }
                else
                {
                    faults.Add(this.CreateValidationFault("Missing validation at DataProcider Read method", "No Data providers found"));
                }

            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(PAMTicketPullService), nameof(GetHistoricalTicketDetails), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);                
                throw ex;

            }
            return result;
        }

        public DateTime? FromUnixTime(long unixTime)
        {
            if (unixTime != 0)
            {
                var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                //return epoch.AddSeconds(Convert.ToDouble(unixTime));
                return epoch.AddMilliseconds(Convert.ToDouble(unixTime));
            }
            else
            {
                return new DateTime();
            }
        }

        public Int64 ToUnixTime(DateTime dateTime)
        {
            //var dateTime = new DateTime(2015, 05, 24, 10, 2, 0, DateTimeKind.Local);
            var dateTimeOffset = new DateTimeOffset(dateTime);
            Int64 unixDateTime = dateTimeOffset.ToUnixTimeMilliseconds();
            return unixDateTime;
        }

        public async Task<string> GetHistoricalTestResultDetails(List<string> faults, AIAAMiddleLayerRequest request)
        {
            var xDoc = new XDocument();
            var clonedXDoc = new XDocument(); int currentprovidercount = 0;
            AIAAMiddleLayerResponse middleLayerResponse = new AIAAMiddleLayerResponse();
            List<TestResults> testResult = new List<TestResults>();
            TestResultList.TestResults = new List<TestResults>();
            string result = string.Empty;
            //BusinessDomain.Entities.IServiceCaller serviceCaller = this.NinjectKernel.Get<BusinessDomain.Entities.IServiceCaller>(BusinessDomain.Entities.BusinessDomainEntityType.ServiceCaller.ToString());
            try
            {
                int providercount = request.DataProviders.Count;
                if (request != null && request.DataProviders != null)
                {
                    foreach (Models.SaaS.DataProvider dataProvider in request.DataProviders)
                    {
                        currentprovidercount++;
                        var cloneddataProvider = dataProvider;
                        if (string.IsNullOrEmpty(Convert.ToString(dataProvider.TicketType)))
                        {
                            faults.Add(this.CreateValidationFault("Missing validation at dataProvider Read method", "ProjetId cannot be null"));
                        }
                        else
                        {
                            try
                            {
                                cloneddataProvider = FilldataProvider(dataProvider, request);
                                if (!string.IsNullOrEmpty(dataProvider.InputRequestValues))
                                {
                                    var inputRequestValues = dataProvider.InputRequestValues.Split(',').ToList();
                                    dataProvider.InputRequestValues = string.Join(",", inputRequestValues);
                                }
                                ServiceCallerRequest serviceCallerRequest = new ServiceCallerRequest()
                                {
                                    Name = cloneddataProvider.Name,
                                    Content = cloneddataProvider.InputRequestValues,
                                    AuthProvider = cloneddataProvider.AuthProvider,
                                    JsonRootNode = cloneddataProvider.JsonRootNode,
                                    Accept = cloneddataProvider.Accept,
                                    HttpVerbName = cloneddataProvider.Method,
                                    MIMEMediaType = cloneddataProvider.MIMEMediaType,
                                    ServiceUrl = cloneddataProvider.ServiceUrl,

                                };
                                xDoc = await _serviceCallerService.GetDocUsingService(serviceCallerRequest, faults).ConfigureAwait(false);
                                if (xDoc != null && xDoc.Root.Elements().Any())
                                {
                                    try
                                    {
                                        #region Call DataFormatter
                                        if (dataProvider.DataFormatter != null)
                                        {
                                            dataProvider.DataFormatter.XDocument = xDoc;
                                            //IDataFormatter dataFormatter = this.NinjectKernel.Get<IDataFormatter>(BusinessDomainEntityType.DataFormatter.ToString());
                                            xDoc = await ExecuteDataFormatter(dataProvider.DataFormatter, request, faults).ConfigureAwait(false);
                                        }
                                        #endregion
                                        if (xDoc != null && xDoc.Root.Elements().Any() && Convert.ToInt32(xDoc.Descendants("TotalRecordCount").ToList()[0].Value) != 0 && !string.IsNullOrEmpty(xDoc.Descendants("TotalRecordCount").ToList()[0].Value) && !string.IsNullOrEmpty(xDoc.Descendants("TotalPageCount").ToList()[0].Value))
                                        {
                                            if (Convert.ToInt32(xDoc.Descendants("TotalRecordCount").ToList()[0].Value) > 0) // && (Convert.ToInt32(xDoc.Descendants("CurrentPage").ToList()[0].Value) == Convert.ToInt32(xDoc.Descendants("TotalPageCount").ToList()[0].Value)))
                                            {
                                                if (xDoc != null && xDoc.Root.Elements().Any())
                                                {
                                                    if (xDoc != null && xDoc.DescendantNodes().Any())
                                                        middleLayerResponse.Response = JsonConvert.SerializeXNode(xDoc, Formatting.None, true);
                                                    else
                                                        middleLayerResponse.Response = string.Empty;
                                                    TestResultList testResultList = JsonConvert.DeserializeObject<TestResultList>(middleLayerResponse.Response);
                                                    TestResultList.TestResults.AddRange(testResultList.TestResults);

                                                    if (Convert.ToInt32(xDoc.Descendants("TotalPageCount").ToList()[0].Value) > 1)
                                                    {
                                                        xDoc = await InitiateMultipleBatchCall(request, dataProvider, TestResultList);
                                                    }

                                                    middleLayerResponse.TestResults = TestResultList;
                                                    result = middleLayerResponse != null ? JsonConvert.SerializeObject(middleLayerResponse.TestResults) : string.Empty;
                                                }
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        throw new Exception("Error occurred while converting response to AIAA format using data formatter - " + GetFaultString(faults));
                                    }
                                }
                                else
                                    faults.Add("XML document is empty.Error Occured while fetching tickets.Please try again.");
                            }
                            catch (Exception ee)
                            {
                                string s = ee.Message.ToString();
                                xDoc = new XDocument();
                                throw new Exception("Error occurred while reading data from Service Provider - " + GetFaultString(faults));
                            }
                        }
                        if (isBreak == 1)
                        {
                            break;
                        }
                    }
                }
                else
                {
                    faults.Add(this.CreateValidationFault("Missing validation at DataProvider Read method", "No Data providers found"));
                }

            }
            catch (Exception ex)
            {
                //logger.Info(" GetHistoricalTestResultDetails : DDName: " + request.uploadTicketRequest.DDName + " Exception" + ex.Message.ToString());
                throw ex;

            }
            return result;
        }

        public async Task<string> GetHistoricalDeploymentDetails(List<string> faults, AIAAMiddleLayerRequest request)
        {
            var xDoc = new XDocument();
            var clonedXDoc = new XDocument(); int currentprovidercount = 0;
            AIAAMiddleLayerResponse middleLayerResponse = new AIAAMiddleLayerResponse();
            List<Deployment> deployment = new List<Deployment>();
            DeploymentList.Deployment = new List<Deployment>();
            string result = string.Empty;
            //BusinessDomain.Entities.IServiceCaller serviceCaller = this.NinjectKernel.Get<BusinessDomain.Entities.IServiceCaller>(BusinessDomain.Entities.BusinessDomainEntityType.ServiceCaller.ToString());
            try
            {
                int providercount = request.DataProviders.Count;
                if (request != null && request.DataProviders != null)
                {
                    foreach (Models.SaaS.DataProvider dataProvider in request.DataProviders)
                    {
                        currentprovidercount++;
                        var cloneddataProvider = dataProvider;
                        if (string.IsNullOrEmpty(Convert.ToString(dataProvider.TicketType)))
                        {
                            faults.Add(this.CreateValidationFault("Missing validation at dataProvider Read method", "ProjetId cannot be null"));
                        }
                        else
                        {
                            try
                            {
                                cloneddataProvider = FilldataProvider(dataProvider, request);
                                if (!string.IsNullOrEmpty(dataProvider.InputRequestValues))
                                {
                                    var inputRequestValues = dataProvider.InputRequestValues.Split(',').ToList();
                                    dataProvider.InputRequestValues = string.Join(",", inputRequestValues);
                                }
                                ServiceCallerRequest serviceCallerRequest = new ServiceCallerRequest()
                                {
                                    Name = cloneddataProvider.Name,
                                    Content = cloneddataProvider.InputRequestValues,
                                    AuthProvider = cloneddataProvider.AuthProvider,
                                    JsonRootNode = cloneddataProvider.JsonRootNode,
                                    Accept = cloneddataProvider.Accept,
                                    HttpVerbName = cloneddataProvider.Method,
                                    MIMEMediaType = cloneddataProvider.MIMEMediaType,
                                    ServiceUrl = cloneddataProvider.ServiceUrl,

                                };
                                xDoc = await _serviceCallerService.GetDocUsingService(serviceCallerRequest, faults).ConfigureAwait(false);
                                if (xDoc != null && xDoc.Root.Elements().Any())
                                {
                                    try
                                    {
                                        #region Call DataFormatter
                                        if (dataProvider.DataFormatter != null)
                                        {
                                            dataProvider.DataFormatter.XDocument = xDoc;
                                            //IDataFormatter dataFormatter = this.NinjectKernel.Get<IDataFormatter>(BusinessDomainEntityType.DataFormatter.ToString());
                                            xDoc = await ExecuteDataFormatter(dataProvider.DataFormatter, request, faults).ConfigureAwait(false);
                                        }
                                        #endregion
                                        if (xDoc != null && xDoc.Root.Elements().Any() && Convert.ToInt32(xDoc.Descendants("TotalRecordCount").ToList()[0].Value) != 0 && !string.IsNullOrEmpty(xDoc.Descendants("TotalRecordCount").ToList()[0].Value) && !string.IsNullOrEmpty(xDoc.Descendants("TotalPageCount").ToList()[0].Value))
                                        {
                                            if (Convert.ToInt32(xDoc.Descendants("TotalRecordCount").ToList()[0].Value) > 0) // && (Convert.ToInt32(xDoc.Descendants("CurrentPage").ToList()[0].Value) == Convert.ToInt32(xDoc.Descendants("TotalPageCount").ToList()[0].Value)))
                                            {
                                                if (xDoc != null && xDoc.Root.Elements().Any())
                                                {
                                                    if (xDoc != null && xDoc.DescendantNodes().Any())
                                                        middleLayerResponse.Response = JsonConvert.SerializeXNode(xDoc, Formatting.None, true);
                                                    else
                                                        middleLayerResponse.Response = string.Empty;
                                                    DeploymentList deploymentList = JsonConvert.DeserializeObject<DeploymentList>(middleLayerResponse.Response);
                                                    DeploymentList.Deployment.AddRange(deploymentList.Deployment);

                                                    if (Convert.ToInt32(xDoc.Descendants("TotalPageCount").ToList()[0].Value) > 1)
                                                    {
                                                        xDoc = await InitiateMultipleBatchCall(request, dataProvider, DeploymentList);
                                                    }

                                                    middleLayerResponse.Deployment = DeploymentList;
                                                    result = middleLayerResponse != null ? JsonConvert.SerializeObject(middleLayerResponse.Deployment) : string.Empty;
                                                }
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        throw new Exception("Error occurred while converting response to AIAA format using data formatter - " + GetFaultString(faults));
                                    }
                                }
                                else
                                    faults.Add("XML document is empty.Error Occured while fetching tickets.Please try again.");
                            }
                            catch (Exception ee)
                            {
                                string s = ee.Message.ToString();
                                xDoc = new XDocument();
                                throw new Exception("Error occurred while reading data from Service Provider - " + GetFaultString(faults));
                            }
                        }
                        if (isBreak == 1)
                        {
                            break;
                        }
                    }
                }
                else
                {
                    faults.Add(this.CreateValidationFault("Missing validation at DataProvider Read method", "No Data providers found"));
                }

            }
            catch (Exception ex)
            {
                //logger.Info(" GetHistoricalDeploymentDetails : DDName: " + request.uploadTicketRequest.DDName + " Exception" + ex.Message.ToString());
                throw ex;

            }
            return result;
        }

        private string MapPriority(string mYWPriority)
        {
            if (mYWPriority == "Very Low")
            {
                return "P5";
            }
            else if (mYWPriority == "Low")
            {
                return "P4";
            }
            else if (mYWPriority == "Medium")
            {
                return "P3";
            }
            else if (mYWPriority == "High")
            {
                return "P2";
            }
            else if (mYWPriority == "Very High")
            {
                return "P1";
            }
            else
            {
                return string.Empty;
            }
        }
        private async Task<XDocument> InitiateMultipleBatchCall(AIAAMiddleLayerRequest request, Models.SaaS.DataProvider dataProvider, IterationsList IterationsListt)
        {
            //IterationsListt.Iterations = new List<DataModels.Common.Iteration>();
            //logger.Info("Ticket Pull Started Multibatch : Method : InitiateMultipleBatchCall : Ticket Type" + request.TicketType + " DDName: " + request.uploadTicketRequest.DDName);
            //List<DATAMODELS.Common.WorkItem> workItems = new List<DATAMODELS.Common.WorkItem>();
            //BusinessDomain.Entities.IServiceCaller serviceCaller = this.NinjectKernel.Get<BusinessDomain.Entities.IServiceCaller>(BusinessDomain.Entities.BusinessDomainEntityType.ServiceCaller.ToString());
            List<string> faults = new List<string>();
            AIAAMiddleLayerResponse middleLayerResponse = new AIAAMiddleLayerResponse();
            //Task.Factory.StartNew(() =>
            //{
            var xDoc1 = new XDocument();
            var xDoc = new XDocument();
            var result = new XDocument();
            try
            {
                int pagenumber = 2;
                var cloneddataProvider = dataProvider;
                do
                {
                    if (dataProvider.InputRequestValues == "ProjectId,BatchSize,PageNumber")
                    {
                        cloneddataProvider = FilldataProvider(dataProvider, request);
                    }
                    else
                    {
                        cloneddataProvider = dataProvider;
                    }
                    if (!string.IsNullOrEmpty(dataProvider.InputRequestValues))
                    {
                        var inputRequestValues = dataProvider.InputRequestValues.Split(',').ToList();
                        //inputRequestValues[2] = "\"ReportedOnAtSourceStart\":\"" + request.StartDate.ToString(dataProvider.FilterDateFormat) + request.StartTime + "\"";
                        //inputRequestValues[3] = "\"ReportedOnAtSourceEnd\":\"" + request.EndDate.ToString(dataProvider.FilterDateFormat) + request.EndTime + "\"";
                        inputRequestValues[2] = "\"PageNumber\":\"" + pagenumber + "\"}";
                        dataProvider.InputRequestValues = string.Join(",", inputRequestValues);
                    }
                    ServiceCallerRequest serviceCallerRequest = new ServiceCallerRequest()
                    {
                        Name = cloneddataProvider.Name,
                        Content = cloneddataProvider.InputRequestValues,
                        AuthProvider = cloneddataProvider.AuthProvider,
                        JsonRootNode = cloneddataProvider.JsonRootNode,
                        Accept = cloneddataProvider.Accept,
                        HttpVerbName = cloneddataProvider.Method,
                        MIMEMediaType = cloneddataProvider.MIMEMediaType,
                        ServiceUrl = cloneddataProvider.ServiceUrl
                    };
                    pagenumber++;
                    //var task = Task.Run(async () => await serviceCaller.GetDocUsingService(serviceCallerRequest, faults).ConfigureAwait(false));
                    //task.Wait();
                    //xDoc = task.Result;
                    xDoc = await _serviceCallerService.GetDocUsingService(serviceCallerRequest, faults).ConfigureAwait(false);
                    if (xDoc != null && xDoc.Root.Elements().Any())
                    {
                        //dataProvider.DataFormatter.XDocument = xDoc;
                        //IDataFormatter dataFormatter = this.NinjectKernel.Get<IDataFormatter>(BusinessDomainEntityType.DataFormatter.ToString());
                        //var task1 = Task.Run(async () => await dataFormatter.ExecuteDataFormatter(dataProvider.DataFormatter, request, faults).ConfigureAwait(false));
                        //task1.Wait();
                        //xDoc = task1.Result;
                        #region Call DataFormatter
                        if (dataProvider.DataFormatter != null)
                        {
                            dataProvider.DataFormatter.XDocument = xDoc;
                            //IDataFormatter dataFormatter = this.NinjectKernel.Get<IDataFormatter>(BusinessDomainEntityType.DataFormatter.ToString());
                            xDoc = await ExecuteDataFormatter(dataProvider.DataFormatter, request, faults).ConfigureAwait(false);
                        }
                        #endregion
                        if (xDoc != null && xDoc.Root.Elements().Any())
                        {
                            if (xDoc != null && xDoc.DescendantNodes().Any())
                                middleLayerResponse.Response = JsonConvert.SerializeXNode(xDoc, Formatting.None, true);
                            else
                                middleLayerResponse.Response = string.Empty;
                            //string respone = middleLayerResponse != null ? JsonConvert.SerializeObject(middleLayerResponse) : string.Empty;
                            IterationsList iterationsresult = JsonConvert.DeserializeObject<IterationsList>(middleLayerResponse.Response);
                            IterationsListt.Iterations.AddRange(iterationsresult.Iterations);
                        }
                    }

                } while (pagenumber <= Convert.ToInt32(xDoc.Descendants("TotalPageCount").ToList()[0].Value));
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return xDoc;
            //});
        }

        private async Task<XDocument> InitiateMultipleBatchCall(AIAAMiddleLayerRequest request, Models.SaaS.DataProvider dataProvider, TestResultList TestResultList)
        {
            //WorkItemsListt.WorkItems = new List<DataModels.Common.WorkItem>();
            //logger.Info("Ticket Pull Started Multibatch : Method : InitiateMultipleBatchCall : Ticket Type" + request.TicketType + " DDName: " + request.uploadTicketRequest.DDName);
            List<TestResults> testresult = new List<TestResults>();
            //BusinessDomain.Entities.IServiceCaller serviceCaller = this.NinjectKernel.Get<BusinessDomain.Entities.IServiceCaller>(BusinessDomain.Entities.BusinessDomainEntityType.ServiceCaller.ToString());
            List<string> faults = new List<string>();
            AIAAMiddleLayerResponse middleLayerResponse = new AIAAMiddleLayerResponse();
            //Task.Factory.StartNew(() =>
            //{
            var xDoc1 = new XDocument();
            var xDoc = new XDocument();
            var result = new XDocument();
            try
            {
                int pagenumber = 2;
                var cloneddataProvider = dataProvider;
                do
                {
                    if (dataProvider.InputRequestValues == "ProjectId,WorkItemTypeUId,BatchSize,PageNumber")
                    {
                        cloneddataProvider = FilldataProvider(dataProvider, request);
                    }
                    else
                    {
                        cloneddataProvider = dataProvider;
                    }
                    if (!string.IsNullOrEmpty(dataProvider.InputRequestValues))
                    {
                        var inputRequestValues = dataProvider.InputRequestValues.Split(',').ToList();
                        //inputRequestValues[2] = "\"ReportedOnAtSourceStart\":\"" + request.StartDate.ToString(dataProvider.FilterDateFormat) + request.StartTime + "\"";
                        //inputRequestValues[3] = "\"ReportedOnAtSourceEnd\":\"" + request.EndDate.ToString(dataProvider.FilterDateFormat) + request.EndTime + "\"";
                        inputRequestValues[3] = "\"PageNumber\":\"" + pagenumber + "\"}";
                        dataProvider.InputRequestValues = string.Join(",", inputRequestValues);
                    }
                    ServiceCallerRequest serviceCallerRequest = new ServiceCallerRequest()
                    {
                        Name = cloneddataProvider.Name,
                        Content = cloneddataProvider.InputRequestValues,
                        AuthProvider = cloneddataProvider.AuthProvider,
                        JsonRootNode = cloneddataProvider.JsonRootNode,
                        Accept = cloneddataProvider.Accept,
                        HttpVerbName = cloneddataProvider.Method,
                        MIMEMediaType = cloneddataProvider.MIMEMediaType,
                        ServiceUrl = cloneddataProvider.ServiceUrl
                    };
                    pagenumber++;
                    //var task = Task.Run(async () => await serviceCaller.GetDocUsingService(serviceCallerRequest, faults).ConfigureAwait(false));
                    //task.Wait();
                    //xDoc = task.Result;
                    xDoc = await _serviceCallerService.GetDocUsingService(serviceCallerRequest, faults).ConfigureAwait(false);
                    if (xDoc != null && xDoc.Root.Elements().Any())
                    {
                        //dataProvider.DataFormatter.XDocument = xDoc;
                        //IDataFormatter dataFormatter = this.NinjectKernel.Get<IDataFormatter>(BusinessDomainEntityType.DataFormatter.ToString());
                        //var task1 = Task.Run(async () => await dataFormatter.ExecuteDataFormatter(dataProvider.DataFormatter, request, faults).ConfigureAwait(false));
                        //task1.Wait();
                        //xDoc = task1.Result;
                        #region Call DataFormatter
                        if (dataProvider.DataFormatter != null)
                        {
                            dataProvider.DataFormatter.XDocument = xDoc;
                            //IDataFormatter dataFormatter = this.NinjectKernel.Get<IDataFormatter>(BusinessDomainEntityType.DataFormatter.ToString());
                            xDoc = await ExecuteDataFormatter(dataProvider.DataFormatter, request, faults).ConfigureAwait(false);
                        }
                        #endregion
                        if (xDoc != null && xDoc.Root.Elements().Any())
                        {
                            if (xDoc != null && xDoc.DescendantNodes().Any())
                                middleLayerResponse.Response = JsonConvert.SerializeXNode(xDoc, Formatting.None, true);
                            else
                                middleLayerResponse.Response = string.Empty;
                            //string respone = middleLayerResponse != null ? JsonConvert.SerializeObject(middleLayerResponse) : string.Empty;
                            TestResultList testresults = JsonConvert.DeserializeObject<TestResultList>(middleLayerResponse.Response);
                            TestResultList.TestResults.AddRange(testresults.TestResults);
                        }
                    }

                } while (pagenumber <= Convert.ToInt32(xDoc.Descendants("TotalPageCount").ToList()[0].Value));
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return xDoc;
            //});
        }
        private async Task<XDocument> InitiateMultipleBatchCall(AIAAMiddleLayerRequest request, Models.SaaS.DataProvider dataProvider, DeploymentList DeploymentList)
        {
            //WorkItemsListt.WorkItems = new List<DataModels.Common.WorkItem>();
            //logger.Info("Ticket Pull Started Multibatch : Method : InitiateMultipleBatchCall : Ticket Type" + request.TicketType + " DDName: " + request.uploadTicketRequest.DDName);
            List<Deployment> testresult = new List<Deployment>();
            //BusinessDomain.Entities.IServiceCaller serviceCaller = this.NinjectKernel.Get<BusinessDomain.Entities.IServiceCaller>(BusinessDomain.Entities.BusinessDomainEntityType.ServiceCaller.ToString());
            List<string> faults = new List<string>();
            AIAAMiddleLayerResponse middleLayerResponse = new AIAAMiddleLayerResponse();
            //Task.Factory.StartNew(() =>
            //{
            var xDoc1 = new XDocument();
            var xDoc = new XDocument();
            var result = new XDocument();
            try
            {
                int pagenumber = 2;
                var cloneddataProvider = dataProvider;
                do
                {
                    if (dataProvider.InputRequestValues == "ProjectId,WorkItemTypeUId,BatchSize,PageNumber")
                    {
                        cloneddataProvider = FilldataProvider(dataProvider, request);
                    }
                    else
                    {
                        cloneddataProvider = dataProvider;
                    }
                    if (!string.IsNullOrEmpty(dataProvider.InputRequestValues))
                    {
                        var inputRequestValues = dataProvider.InputRequestValues.Split(',').ToList();
                        //inputRequestValues[2] = "\"ReportedOnAtSourceStart\":\"" + request.StartDate.ToString(dataProvider.FilterDateFormat) + request.StartTime + "\"";
                        //inputRequestValues[3] = "\"ReportedOnAtSourceEnd\":\"" + request.EndDate.ToString(dataProvider.FilterDateFormat) + request.EndTime + "\"";
                        inputRequestValues[3] = "\"PageNumber\":\"" + pagenumber + "\"}";
                        dataProvider.InputRequestValues = string.Join(",", inputRequestValues);
                    }
                    ServiceCallerRequest serviceCallerRequest = new ServiceCallerRequest()
                    {
                        Name = cloneddataProvider.Name,
                        Content = cloneddataProvider.InputRequestValues,
                        AuthProvider = cloneddataProvider.AuthProvider,
                        JsonRootNode = cloneddataProvider.JsonRootNode,
                        Accept = cloneddataProvider.Accept,
                        HttpVerbName = cloneddataProvider.Method,
                        MIMEMediaType = cloneddataProvider.MIMEMediaType,
                        ServiceUrl = cloneddataProvider.ServiceUrl
                    };
                    pagenumber++;
                    //var task = Task.Run(async () => await serviceCaller.GetDocUsingService(serviceCallerRequest, faults).ConfigureAwait(false));
                    //task.Wait();
                    //xDoc = task.Result;
                    xDoc = await _serviceCallerService.GetDocUsingService(serviceCallerRequest, faults).ConfigureAwait(false);
                    if (xDoc != null && xDoc.Root.Elements().Any())
                    {
                        //dataProvider.DataFormatter.XDocument = xDoc;
                        //IDataFormatter dataFormatter = this.NinjectKernel.Get<IDataFormatter>(BusinessDomainEntityType.DataFormatter.ToString());
                        //var task1 = Task.Run(async () => await dataFormatter.ExecuteDataFormatter(dataProvider.DataFormatter, request, faults).ConfigureAwait(false));
                        //task1.Wait();
                        //xDoc = task1.Result;
                        #region Call DataFormatter
                        if (dataProvider.DataFormatter != null)
                        {
                            dataProvider.DataFormatter.XDocument = xDoc;
                            //IDataFormatter dataFormatter = this.NinjectKernel.Get<IDataFormatter>(BusinessDomainEntityType.DataFormatter.ToString());
                            xDoc = await ExecuteDataFormatter(dataProvider.DataFormatter, request, faults).ConfigureAwait(false);
                        }
                        #endregion
                        if (xDoc != null && xDoc.Root.Elements().Any())
                        {
                            if (xDoc != null && xDoc.DescendantNodes().Any())
                                middleLayerResponse.Response = JsonConvert.SerializeXNode(xDoc, Formatting.None, true);
                            else
                                middleLayerResponse.Response = string.Empty;
                            //string respone = middleLayerResponse != null ? JsonConvert.SerializeObject(middleLayerResponse) : string.Empty;
                            TestResultList testresults = JsonConvert.DeserializeObject<TestResultList>(middleLayerResponse.Response);
                            TestResultList.TestResults.AddRange(testresults.TestResults);
                        }
                    }

                } while (pagenumber <= Convert.ToInt32(xDoc.Descendants("TotalPageCount").ToList()[0].Value));
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return xDoc;
            //});
        }
    }
}
