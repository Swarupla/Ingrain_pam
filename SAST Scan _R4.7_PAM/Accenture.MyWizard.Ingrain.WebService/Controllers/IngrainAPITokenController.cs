using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Accenture.MyWizard.Shared.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RestSharp;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;

namespace Accenture.MyWizard.Ingrain.WebService.Controllers
{
    /// <summary>
    /// Generate token from API
    /// This class & method is to support UI to run from local. Dont Delete
    /// </summary>
    public class IngrainAPITokenController : ControllerBase
    {
        private readonly IOptions<IngrainAppSettings> appSettings;
        public IngrainAPITokenController(IOptions<IngrainAppSettings> settings)
        {
            appSettings = settings;
        }

        //------------------------------------------------------------
        //***COMMENTED BELOW CODE FOR VULNERABILITY ISSUE***
        //------------------------------------------------------------

        /// <summary>
        /// Generate Token 
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="clientSecret"></param>
        /// <param name="resourceId"></param>
        /// <returns></returns>
        /*[HttpGet]
        [Route("api/IngrainAPIToken")]
        public IActionResult IngrainAPIToken(string clientId, string clientSecret, string resourceId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(IngrainAPITokenController), nameof(IngrainAPIToken), "START", string.Empty, string.Empty, clientId, string.Empty);
            try
            {
                if (appSettings.Value.authProvider.ToUpper() == "FORM")
                {
                    using (var httpClient = new HttpClient())
                    {
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(IngrainAPITokenController), nameof(IngrainAPIToken), "GenerateToken- START", string.Empty, string.Empty, string.Empty, string.Empty);
                        httpClient.BaseAddress = new Uri(Program.AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("PAMTokenUrl").Value);
                        httpClient.DefaultRequestHeaders.Accept.Clear();
                        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(IngrainAPITokenController), nameof(IngrainAPIToken), "SerializeObject - START", string.Empty, string.Empty, string.Empty, string.Empty);
                        string json = JsonConvert.SerializeObject(new
                        {
                            username = Program.AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("UserNamePAM").Value,
                            password = Program.AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("PasswordPAM").Value
                        });
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(IngrainAPITokenController), nameof(IngrainAPIToken), "SerializeObject - END", string.Empty, string.Empty, string.Empty, string.Empty);
                        var requestOptions = new StringContent(json, Encoding.UTF8, "application/json");

                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                        var result = httpClient.PostAsync("", requestOptions).Result;
                        if (result.StatusCode != HttpStatusCode.OK)
                        {
                            throw new Exception(string.Format("Unable to process your request for generating token. Please check your credentials and try again. Status Code: {0}", result.StatusCode));
                        }
                        var result1 = result.Content.ReadAsStringAsync().Result;
                        var tokenObj = JsonConvert.DeserializeObject(result1) as dynamic;
                        //var token = Convert.ToString(tokenObj.token);                        
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(IngrainAPITokenController), nameof(IngrainAPIToken), "GenerateToken- END", string.Empty, string.Empty, string.Empty, string.Empty);

                            return Ok(tokenObj);
                        }

                        return StatusCode((int)HttpStatusCode.InternalServerError, "Token not generated");
                    }
                }
                else if (appSettings.Value.authProvider.ToUpper() == "AZUREAD" || appSettings.Value.authProvider.ToUpper() == "AZURE")
                {
                    var client = new RestClient(appSettings.Value.token_Url);
                    var request = new RestRequest(Method.POST);
                    request.AddHeader("cache-control", "no-cache");
                    request.AddHeader("content-type", "application/x-www-form-urlencoded");
                    request.AddParameter("application/x-www-form-urlencoded", "grant_type=" + appSettings.Value.Grant_Type +
                        "&client_id=" + clientId +
                        "&client_secret=" + clientSecret +
                        "&resource=" + resourceId,
                        ParameterType.RequestBody);

                    IRestResponse response = client.Execute(request);
                    string json = response.Content;
                    var x = (Newtonsoft.Json.JsonConvert.DeserializeObject(json)) as dynamic;
                    var accessToken = x;
                    return Ok(accessToken);
                }

                LOGGING.LogManager.Logger.LogProcessInfo(typeof(IngrainAPITokenController), nameof(IngrainAPIToken), "ERROR Auth Provider is not Forms/Azure", string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultWithValidationMessageResponse("Token not generated");
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(IngrainAPITokenController), nameof(IngrainAPIToken), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
            }
        }*/

    }
}
