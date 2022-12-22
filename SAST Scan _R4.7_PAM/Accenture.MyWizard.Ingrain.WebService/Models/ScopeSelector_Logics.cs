using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using log4net;
using Newtonsoft.Json;
using System.Threading.Tasks;


namespace Accenture.MyWizard.Ingrain.WebService.Models
{
    public class ScopeSelector_Logics
    {
        //Type speLogic = "SPE_Logic";
        //ILog log = LogManager.GetLogger()


        //static RootObject rootObject = new RootObject();
        //static int counter = 0;
        //static RootObjectOne RootObjectOne = new RootObjectOne();
        //static List<DeliveryConstructTree> tree = new List<DeliveryConstructTree>();
        //string WinServicsResultsSP;
        //string WinServicsResultsRETrainEst;


        //public string UserStoryclientStructureGetOuth(Guid ClientUID, string UserEmail)
        //{

        //    try
        //    {
        //        var result1 = "";
        //        string JsonStringResult = "";
        //        log.Info("UserStoryclientStructure" + ClientUID);
        //        PhoenixToken objToken = new PhoenixToken();
        //        string token = objToken.GenerateToken();
        //        // string UserEmail = ConfigurationManager.AppSettings["DemographicsUserEmail"].ToString();
        //        if (token != "")
        //        {
        //            string UserID = ConfigurationManager.AppSettings["DemographicsUser"].ToString();
        //            string Password = ConfigurationManager.AppSettings["DemographicsPass"].ToString();

        //            using (var httpClient = new HttpClient())
        //            {
        //                httpClient.BaseAddress = new Uri(ConfigurationManager.AppSettings["myWizardFortressWebAPIUrl"].ToString());
        //                httpClient.DefaultRequestHeaders.Accept.Clear();

        //                httpClient.DefaultRequestHeaders.Add("UserName", UserID);
        //                httpClient.DefaultRequestHeaders.Add("Password", Password);
        //                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        //                httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
        //                // var ClinetStruct = String.Format("AccountClients?clientUId=" + ClientUID + "&deliveryConstructUId=null");

        //                var ClinetStruct = String.Format("GetAccountDeliveryConstructs?clientUId=" + ClientUID + "&deliveryConstructUId=null&email=" + UserEmail + "&queryMode=null");
        //                HttpContent content = new StringContent(string.Empty);
        //                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
        //                var result = httpClient.GetAsync(ClinetStruct).Result;
        //                if (result.StatusCode != System.Net.HttpStatusCode.OK)
        //                {
        //                    log.Error("GetAPAData" + "Unable to process your request for generating token. Please check your credentials and try again. Status Code: {0}" + result.StatusCode);

        //                    return "Status Code Not 200 OK";
        //                }
        //                result1 = result.Content.ReadAsStringAsync().Result;
        //                if (result1 != null)
        //                {
        //                    if (result1.ToList().Count > 0)
        //                    {
        //                        var data = JsonConvert.DeserializeObject<ClientStructure>(result1);
        //                        string ClientResult = JsonConvert.SerializeObject(data);
        //                        return ClientResult;
        //                    }
        //                }
        //                else
        //                {
        //                    return "";
        //                }

        //            }
        //        }

        //        else
        //        {
        //            log.Error("UserStoryclientStructure" + "   Token is null");
        //            return "Error While Genrating Token";
        //        }

        //        return result1;
        //    }

        //    catch (Exception ex)
        //    {
        //        if (ex.InnerException != null)
        //        {
        //            log.Error("UserStoryclientStructure" + String.Concat(ex.InnerException.StackTrace, ex.InnerException.Message, ex.InnerException));
        //            return String.Concat(ex.InnerException.StackTrace, ex.InnerException.Message, ex.InnerException);
        //        }
        //        else
        //        {
        //            log.Error("UserStoryclientStructure" + String.Concat(ex.Message));
        //            return ex.Message;
        //        }
        //    }

        //}

        //public string GetSecurityAcessAgilePhinix(UserSecurityAcessAgile objpost)
        //{


        //    try
        //    {
        //        // log.Info("GetSecurityAcessClient" + ClientUID);

        //        ILog log = log4net.LogManager.GetLogger('UserSecurityAcessAgile');
        //        string JsonStringResult = "";
        //        PhoenixToken objToken = new PhoenixToken();
        //        // string Token = objToken.GenerateToken();

        //        string Token = objpost.Token.ToString();
        //        string UserID = ConfigurationManager.AppSettings["DemographicsUser"].ToString();
        //        // string UserEmail = ConfigurationManager.AppSettings["DemographicsUserEmail"].ToString();
        //        string Password = ConfigurationManager.AppSettings["DemographicsPass"].ToString();

        //        string UserStoryentityUID = ConfigurationManager.AppSettings["UserStoryentityUID"].ToString();
        //        string taskentityUID = ConfigurationManager.AppSettings["taskentityUID"].ToString();
        //        using (var httpClient = new HttpClient())
        //        {
        //            httpClient.BaseAddress = new Uri(ConfigurationManager.AppSettings["myWizardFortressWebAPIUrl"].ToString());
        //            httpClient.DefaultRequestHeaders.Accept.Clear();
        //            //httpClient.DefaultRequestHeaders.Add("UserName", UserID);
        //            //httpClient.DefaultRequestHeaders.Add("Password", Password);
        //            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        //            httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + Token);
        //            var tipAMURL = String.Format("AccountAccessPrivileges?clientUId=" + objpost.ClientUID.ToString() + "&deliveryConstructUId=" + objpost.DeliveryConstructUId + "");
        //            HttpContent content = new StringContent(string.Empty);
        //            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
        //            //ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
        //            var result = httpClient.GetAsync(tipAMURL).Result;
        //            if (result.StatusCode != System.Net.HttpStatusCode.OK)
        //            {
        //                log.Error("UserSecurityAcessAgile" + "Unable to process your request for generating token. Please check your credentials and try again. Status Code: {0}" + result.StatusCode);
        //                JsonStringResult = JsonConvert.SerializeObject(result.StatusCode.ToString());
        //                return JsonStringResult;
        //                // throw new Exception(string.Format("Unable to process your request for generating token. Please check your credentials and try again. Status Code: {0}", result.StatusCode));
        //            }
        //            var result1 = result.Content.ReadAsStringAsync().Result;
        //            if (result1 != null)
        //            {
        //                if (result1.ToList().Count > 0)
        //                {
        //                    var rootObject = JsonConvert.DeserializeObject<SecurityAcess>(result1);
        //                    // var data  = JsonConvert.DeserializeObject<RootObject>(result1);


        //                    //var resutsone = data.Client.DeliveryConstructs.ToList();
        //                    if (rootObject.AccountPermissionViews.Count > 0)
        //                    {
        //                        // JsonStringResult = "True";

        //                        var tempone = rootObject.AccountPermissionViews.Where(x => x.EntityUId == UserStoryentityUID).FirstOrDefault();


        //                        JsonStringResult = JsonConvert.SerializeObject(tempone);
        //                    }
        //                    else
        //                    {
        //                        JsonStringResult = "";
        //                    }

        //                    // JsonStringResult = JsonConvert.SerializeObject(JsonStringResult);
        //                    return JsonStringResult;
        //                }
        //            }

        //        }

        //        return JsonStringResult;

        //    }
        //    catch (Exception ex)
        //    {
        //        if (ex.InnerException != null)
        //        {
        //            log.Error("UserStoryPrediction" + String.Concat(ex.InnerException.StackTrace, ex.InnerException.Message, ex.InnerException));
        //            return String.Concat(ex.InnerException.StackTrace, ex.InnerException.Message, ex.InnerException);
        //        }
        //        else
        //        {
        //            log.Error("UserStoryClientNameGet" + String.Concat(ex.Message));
        //            return ex.Message;
        //        }
        //    }
        //    return "";
        //}

        //public string GetSecurityAcessPhinix(Guid ClientUID, Guid DeliveryConstructUId, string UserEmail)
        //{

        //    try
        //    {
        //        ILog log = log4net.LogManager.GetLogger("GetDeliveryStructuerFromPhoenix Wind Service");
        //        string JsonStringResult = "";
        //        PhoenixToken objToken = new PhoenixToken();
        //        string Token = objToken.GenerateToken();
        //        string UserID = ConfigurationManager.AppSettings["DemographicsUser"].ToString();
        //        string Password = ConfigurationManager.AppSettings["DemographicsPass"].ToString();
        //        using (var httpClient = new HttpClient())
        //        {
        //            httpClient.BaseAddress = new Uri(ConfigurationManager.AppSettings["myWizardFortressWebAPIUrl"].ToString());
        //            httpClient.DefaultRequestHeaders.Accept.Clear();
        //            httpClient.DefaultRequestHeaders.Add("UserName", UserID);
        //            httpClient.DefaultRequestHeaders.Add("Password", Password);
        //            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        //            httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + Token);
        //            var tipAMURL = String.Format("GetAccountClientDeliveryConstructs?clientUId=" + ClientUID.ToString() + "&deliveryConstructUId=null&includeCompleteHierarchy=true&email=" + UserEmail + "&queryMode=null");
        //            HttpContent content = new StringContent(string.Empty);
        //            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
        //            var result = httpClient.GetAsync(tipAMURL).Result;
        //            if (result.StatusCode != System.Net.HttpStatusCode.OK)
        //            {
        //                log.Error("GetAPAData" + "Unable to process your request for generating token. Please check your credentials and try again. Status Code: {0}" + result.StatusCode);
        //                // throw new Exception(string.Format("Unable to process your request for generating token. Please check your credentials and try again. Status Code: {0}", result.StatusCode));
        //            }
        //            var result1 = result.Content.ReadAsStringAsync().Result;
        //            if (result1 != null)
        //            {

        //            }

        //        }

        //        return JsonStringResult;
        //    }
        //    catch (Exception ex)
        //    {
        //        log.Error("GetDeliveryStructuerFromPhoenix" + ex.Message);
        //    }
        //    return "";
        //}

        //public string GetDemographicUserAutorizePhoenix(Guid ClientUID, string DeliveryConstructUId, string UserEmail)
        //{

        //    try
        //    {
        //        ILog log = log4net.LogManager.GetLogger("GetDeliveryStructuerFromPhoenix Wind Service");
        //        string JsonStringResult = "";
        //        PhoenixToken objToken = new PhoenixToken();
        //        string Token = objToken.GenerateToken();
        //        string UserID = ConfigurationManager.AppSettings["DemographicsUser"].ToString();
        //        // string UserEmail = ConfigurationManager.AppSettings["DemographicsUserEmail"].ToString();
        //        string Password = ConfigurationManager.AppSettings["DemographicsPass"].ToString();
        //        using (var httpClient = new HttpClient())
        //        {
        //            httpClient.BaseAddress = new Uri(ConfigurationManager.AppSettings["myWizardFortressWebAPIUrl"].ToString());
        //            httpClient.DefaultRequestHeaders.Accept.Clear();
        //            httpClient.DefaultRequestHeaders.Add("UserName", UserID);
        //            httpClient.DefaultRequestHeaders.Add("Password", Password);
        //            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        //            httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + Token);
        //            var tipAMURL = String.Format("GetAccountClientDeliveryConstructs?clientUId=" + ClientUID.ToString() + "&deliveryConstructUId=null&includeCompleteHierarchy=true&email=" + UserEmail + "&queryMode=null");
        //            HttpContent content = new StringContent(string.Empty);
        //            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
        //            //ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
        //            var result = httpClient.GetAsync(tipAMURL).Result;
        //            if (result.StatusCode != System.Net.HttpStatusCode.OK)
        //            {
        //                log.Error("GetAPAData" + "Unable to process your request for generating token. Please check your credentials and try again. Status Code: {0}" + result.StatusCode);
        //                // throw new Exception(string.Format("Unable to process your request for generating token. Please check your credentials and try again. Status Code: {0}", result.StatusCode));
        //            }
        //            var result1 = result.Content.ReadAsStringAsync().Result;
        //            if (result1 != null)
        //            {

        //            }

        //        }

        //        return JsonStringResult;
        //    }
        //    catch (Exception ex)
        //    {
        //        log.Error("GetDeliveryStructuerFromPhoenix" + ex.Message);
        //    }
        //    return "";
        //}

    }
}