using Accenture.MyWizard.Fortress.Core.Utilities;
using Accenture.MyWizard.Ingrain.UI.DotNetWrapper.Models;
using Accenture.MyWizard.Shared.Helpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;

namespace Accenture.MyWizard.Ingrain.UI.DotNetWrapper.Controllers
{
    //[Authorize]
    public class landingPageController : Controller
    {
        #region Members
        private IOptions<IngrainAppSettings> appSettings { get; set; }
        private readonly IHttpContextAccessor _httpContext;
        private string environment;


        #endregion

        /// <summary>
        /// Home Controller
        /// </summary>
        /// <param name="settings">appSettings</param>
        public landingPageController(IOptions<IngrainAppSettings> settings, IHttpContextAccessor httpContext)
        {
            appSettings = settings;
            _httpContext = httpContext;
            environment = Program.AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("Environment").Value;
        }

        public ActionResult Index()
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(landingPageController), nameof(Index), "DotNetWrapper Home-Index Started", string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                var authprovider = Program.AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("authProvider").Value;
                string userId = string.Empty;
                if (authprovider.ToUpper() == "FORM" || authprovider.ToUpper() == "AZUREAD")
                {
                    //if (this.User.Identity.IsAuthenticated)
                    //{
                    string token = string.Empty;
                    if (environment.Equals(CONSTANTS.PAMEnvironment))
                    {
                        environment = "PAM";
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(landingPageController), nameof(Index), "DotNetWrapper Inside PAM condition", string.Empty, string.Empty, string.Empty, string.Empty);
                        if (authprovider.ToUpper() == "FORM")
                        {
                            string tokenCookie = string.Empty;
                            string expirationDate = string.Empty;
                            Dictionary<string, string> response = new Dictionary<string, string>();

                            if (_httpContext.HttpContext.Request != null && _httpContext.HttpContext.Request.Cookies["AUTH_SESSION"] != null)
                                tokenCookie = _httpContext.HttpContext.Request.Cookies["AUTH_SESSION"];

                            if (!string.IsNullOrEmpty(tokenCookie))
                            {
                                AuthToken authToken = new AuthToken();
                                authToken = GenerateToken(tokenCookie);

                                if (authToken != null)
                                {
                                    expirationDate = authToken.ExpirationDate;
                                    token = authToken.Token;
                                }

                                dynamic pamTokenObj = GeneratePAMToken();
                                string pamToken = pamTokenObj["token"];

                                AuthorizeToken tokenClaims = new AuthorizeToken();
                                tokenClaims = ValidateToken(authToken.Token);
                                if (tokenClaims != null)
                                {
                                    userId = tokenClaims.UserName;
                                    ViewBag.UserEmailId = tokenClaims.UserName;
                                    ViewBag.AuthToken = pamToken;
                                    ViewBag.ExpirationDate = expirationDate;
                                    ViewBag.AuthSession = tokenCookie;
                                }
                                else
                                {
                                    response = null;
                                }

                            }
                            else
                            {
                                response = null;
                            }
                        }
                    }
                    else
                    {
                        if (this.User.Identity.IsAuthenticated)
                        {
                            token = string.Empty;
                            environment = "FDS";
                            if (this.Request.QueryString.Value.Contains("type") && this.Request.QueryString.Value.Contains("CategoryType"))
                            {
                                ViewBag.MonteCarlo = true; // setting this flag for header title
                            }

                            userId = this.User.Identity.Name;
                            ViewBag.UserEmailId = userId;
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(landingPageController), nameof(Index), "Inside isAuthenticated - environment", string.Empty, string.Empty, string.Empty, Convert.ToString(environment));
                            LOGGING.LogManager.Logger.LogProcessInfo(typeof(landingPageController), nameof(Index), "viewBag UserID", string.Empty, string.Empty, string.Empty, Convert.ToString(ViewBag.UserEmailId));

                        }
                    }
                }
                else
                {
                    if (this.Request.QueryString.Value.Contains("type") && this.Request.QueryString.Value.Contains("CategoryType"))
                    {
                        ViewBag.MonteCarlo = true; // setting this flag for header title
                    }
                    userId = this.User.Identity.Name;
                    ViewBag.UserEmailId = userId.Replace(@"\", @"\\");
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(landingPageController), nameof(Index), "userId" + userId, string.Empty, string.Empty, string.Empty, string.Empty);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(landingPageController), nameof(Index), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }

            return View();
        }


        public dynamic GeneratePAMToken()
        {
            dynamic tokenObj = null;

            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(Program.AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("PAMTokenUrl").Value);
                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                string json = JsonConvert.SerializeObject(new
                {
                    username = Program.AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("UserNamePAM").Value,
                    password = Program.AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("PasswordPAM").Value
                });
                var requestOptions = new StringContent(json, Encoding.UTF8, "application/json");


                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                var result = httpClient.PostAsync("", requestOptions).Result;
                if (result.StatusCode != HttpStatusCode.OK)
                {
                    throw new Exception(string.Format(CONSTANTS.TokenError, result.StatusCode));
                }
                var result1 = result.Content.ReadAsStringAsync().Result;
                tokenObj = JsonConvert.DeserializeObject(result1) as dynamic;
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(landingPageController), nameof(GeneratePAMToken), "GenerateToken- END", string.Empty, string.Empty, string.Empty, string.Empty);
            }
            return tokenObj;
        }

        public static String Decrypt(String base64EncryptedInput, String keyVal, String vectorVal)
        {
            var cipherText = Convert.FromBase64String(base64EncryptedInput);

            // Declare the string used to hold the decrypted text.
            var plaintext = String.Empty;

            // Create an AesManaged object with the specified key and IV.
            using (var aesAlg = new AesManaged())
            {
                aesAlg.Key = Convert.FromBase64String(keyVal);
                aesAlg.IV = Convert.FromBase64String(vectorVal);

                // Create a decryptor to perform the stream transform.
                var decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for decryption.


                var msDecrypt = new MemoryStream(cipherText);

                var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);

                var srDecrypt = new StreamReader(csDecrypt);

                // Read the decrypted bytes from the decrypting stream and place them in a string.
                plaintext = srDecrypt.ReadToEnd();

            }

            return plaintext;
        }

        private void SetCookieAsDefault(string cookieKey, string value)
        {
            CookieOptions option = new CookieOptions();

            if (Request.Cookies[cookieKey] != null)
            {
                DeleteCookie(cookieKey, value);
            }

            var cookie = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                Path = "/",
                Domain = Program.AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("ingrainDomain").Value,
                Expires = DateTime.Now.AddMinutes(300)
            };

            Response.Cookies.Append(cookieKey, value, cookie);
        }

        private void DeleteCookie(string cookieKey, string value)
        {
            Response.Cookies.Delete(cookieKey);
        }

        //private string GenerateToken()
        //{
        //    var token = string.Empty;
        //    var client = new RestClient(Program.AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("token_Url").Value);
        //    var request = new RestRequest(Method.POST);
        //    request.AddHeader("cache-control", "no-cache");
        //    request.AddHeader("content-type", "application/x-www-form-urlencoded");
        //    request.AddParameter("application/x-www-form-urlencoded",
        //       "grant_type=" + Program.AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("grant_type").Value +
        //       "&client_id=" + Program.AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("client_id").Value +
        //       "&client_secret=" + Program.AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("client_secret").Value +
        //       "&resource=" + Program.AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("resourceId").Value,
        //       ParameterType.RequestBody);

        //    IRestResponse response = client.Execute(request);
        //    string json = response.Content;
        //    // Retrieve and Return the Access Token                
        //    var tokenObj = JsonConvert.DeserializeObject(json) as dynamic;
        //    token = Convert.ToString(tokenObj.access_token);
        //    return token;
        //}

        /// <summary>
        /// For PAM Environment
        /// </summary>
        /// <param name="authSession">The authSession parameter</param>
        /// <returns>Generates the token using Auth_Session</returns>
        private AuthToken GenerateToken(string authSession)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(landingPageController), nameof(GenerateToken), "GenerateToken- START", string.Empty, string.Empty, string.Empty, string.Empty);
            AuthToken response = new AuthToken();
            try
            {
                if (!string.IsNullOrEmpty(authSession))
                {
                    HttpClientHandler handler = new HttpClientHandler();
                    System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                    handler.CookieContainer = new CookieContainer();
                    string url = Program.AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("token_Url").Value;
                    handler.CookieContainer.Add(new Uri(url), new Cookie(CONSTANTS.AuthSession, authSession)); // Adding a Cookie
                    using (HttpClient httpClient = new HttpClient(handler))
                    {
                        var httpResponse = httpClient.PostAsync(url, null).Result;

                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(landingPageController), nameof(GenerateToken), "GenerateToken- httpResponse: " + httpResponse.IsSuccessStatusCode, string.Empty, string.Empty, string.Empty, string.Empty);
                        if (!httpResponse.IsSuccessStatusCode)
                        {
                            throw new Exception(string.Format(CONSTANTS.TokenError, httpResponse.StatusCode));
                        }
                        else
                        {
                            var result = httpResponse.Content.ReadAsStringAsync().Result;
                            if (!string.IsNullOrEmpty(result))
                            {
                                response = JsonConvert.DeserializeObject<AuthToken>(result);
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(landingPageController), nameof(GenerateToken), "GenerateToken- Token: " + response.Token, string.Empty, string.Empty, string.Empty, string.Empty);
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(landingPageController), nameof(GenerateToken), "GenerateToken- ExpirationDate: " + response.ExpirationDate, string.Empty, string.Empty, string.Empty, string.Empty);
                            }
                            else
                            {
                                response = null;
                                LOGGING.LogManager.Logger.LogProcessInfo(typeof(landingPageController), nameof(GenerateToken), "GenerateToken- Response: " + response, string.Empty, string.Empty, string.Empty, string.Empty);
                            }
                        }
                    }
                }
                else
                {
                    return null;
                }

            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(landingPageController), nameof(GenerateToken), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return response = null;
            }

            LOGGING.LogManager.Logger.LogProcessInfo(typeof(landingPageController), nameof(GenerateToken), "END", string.Empty, string.Empty, string.Empty, string.Empty);
            return response;
        }

        /// <summary>
        /// For PAM Environment
        /// </summary>
        /// <param name="token">The token parameter</param>
        /// <returns>Validates the token and returns the result</returns>
        private AuthorizeToken ValidateToken(string token)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(landingPageController), nameof(ValidateToken), "ValidateToken- START", string.Empty, string.Empty, string.Empty, string.Empty);
            AuthorizeToken response = new AuthorizeToken();
            try
            {
                if (string.IsNullOrEmpty(token))
                    return null;

                HttpClient httpClient = new HttpClient();
                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                string URL = Program.AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("tokenValidation_URL").Value;
                System.Uri url = new System.Uri(URL);
                HttpResponseMessage httpResponse = httpClient.PostAsJsonAsync(url, new { token = token }).Result;
                if (!httpResponse.IsSuccessStatusCode)
                {
                    throw new Exception(string.Format(CONSTANTS.TokenError, httpResponse.StatusCode));
                }
                else
                {
                    var result = httpResponse.Content.ReadAsStringAsync().Result;
                    if (!string.IsNullOrEmpty(result))
                    {
                        response = JsonConvert.DeserializeObject<AuthorizeToken>(result);
                    }
                    else
                        response = null;

                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(landingPageController), nameof(ValidateToken), "ValidateToken- Response: " + response, string.Empty, string.Empty, string.Empty, string.Empty);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(landingPageController), nameof(ValidateToken), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return response = null;
            }

            LOGGING.LogManager.Logger.LogProcessInfo(typeof(landingPageController), nameof(ValidateToken), "END", string.Empty, string.Empty, string.Empty, string.Empty);
            return response;
        }

        [AllowAnonymous]
        public async Task<ActionResult> Signout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return View("Signout");
        }

        public IActionResult RefreshCookie()
        {
            return Ok();
        }
		
		[Route("landingPage/environment")]
        [HttpGet]
        public JsonResult environmentLoad()
        {
            if (this.User.Identity.IsAuthenticated)
            {
                EnvironmentCs environment = new EnvironmentCs();
                environment.authProvider = Program.AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("authProvider").Value;
                environment.ingrainDomain = Program.AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("ingrainDomain").Value;
                environment.hostingUrl = Program.AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("hostingUrl").Value;
                environment.token_Url = Program.AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("token_Url").Value;
                environment.grant_type = Program.AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("grant_type").Value;
                environment.resourceId = Program.AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("resourceId").Value;
                environment.client_secret = Program.AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("client_secret").Value;
                environment.username = Program.AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("username").Value;
                environment.password = Program.AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("password").Value;
                environment.Environment = Program.AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("Environment").Value;
                environment.ingrainAPIURL = Program.AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("ingrainAPIURL").Value;
                environment.mywizardHomeUrl = Program.AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("mywizardHomeUrl").Value;
                environment.myWizardWebConsoleUrl = Program.AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("myWizardWebConsoleUrl").Value;
                environment.myWizardAPIUrl = Program.AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("myWizardAPIUrl").Value;
                environment.AppServiceUID = Program.AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("AppServiceUID").Value;
                environment.modelTrainingStatusText = Program.AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("modelTrainingStatusText").Value;
                environment.sessionTimeout = Convert.ToInt16(Program.AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("sessionTimeout").Value);
                environment.warningPopupTime = Convert.ToInt16(Program.AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("warningPopupTime").Value);
                environment.pingInterval = Convert.ToInt16(Program.AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("pingInterval").Value);
                environment.grantType = Program.AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("grantType").Value;

                environment.clientId = Program.AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("clientId").Value;
                environment.clientSecret = Program.AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("clientSecret").Value;
                environment.scope = Program.AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("scope").Value;
                environment.ingrainsignoutUrl = Program.AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("ingrainsignoutUrl").Value;
                environment.TenantId = Program.AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("TenantId").Value;
                environment.expireOffsetSeconds = Convert.ToInt16(Program.AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("expireOffsetSeconds").Value);
                environment.redirectUri = Program.AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("redirectUri").Value;
                environment.myConcertoHomeURL = Program.AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("myConcertoHomeURL").Value;

                environment.ingrainappUrl = Program.AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("ingrainappUrl").Value;
                environment.IsAzureTokenRefresh = Convert.ToBoolean(Program.AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("IsAzureTokenRefresh").Value);
                environment.AzureTokenRefreshTime = Convert.ToInt16(Program.AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("AzureTokenRefreshTime").Value);
                environment.PhoenixResourceId = Program.AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("PhoenixResourceId").Value;
                environment.FDSbaseURL = Program.AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("FDSbaseURL").Value;

                return Json(environment);

            } else
            {
                return new JsonResult("");
            }

        }


    }
}



