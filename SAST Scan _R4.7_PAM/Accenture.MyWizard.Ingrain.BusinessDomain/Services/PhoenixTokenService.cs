
#region Copyright (c) 2018 Accenture . All rights reserved.

// <copyright company="Accenture">
// Copyright (c) 2018 Accenture.  All rights reserved.
// Reproduction or transmission in whole or in part, in any form or by any means, 
// electronic, mechanical or otherwise, is prohibited without the prior written 
// consent of the copyright owner.
// </copyright>

#endregion

#region PhoenixTokenService Information
/********************************************************************************************************\
Module Name     :   PhoenixTokenService
Project         :   Accenture.MyWizard.SelfServiceAI.BusinessDomain
Organisation    :   Accenture Technologies Ltd.
Created By      :   Ravi Kumar
Created Date    :   28-March-2019
Revision History :
Revision No             Initial             Modified Date           Description
1.0                     An                  28-March-2019           
\********************************************************************************************************/
#endregion
namespace Accenture.MyWizard.Ingrain.BusinessDomain.Services
{
    using Accenture.MyWizard.Fortress.Core.Utilities;
    using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
    using Accenture.MyWizard.Ingrain.DataModels.Models;
    using Accenture.MyWizard.Shared.Helpers;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Options;
    using Newtonsoft.Json;
    using RestSharp;
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;

    public class PhoenixTokenService : IPhoenixTokenService
    {
        private IngrainAppSettings appSettings { get; set; }
        private readonly IHttpContextAccessor _httpContext;
        public PhoenixTokenService(IOptions<IngrainAppSettings> settings)
        {
            appSettings = settings.Value;
        }

        public dynamic GenerateToken()
        {
            dynamic token = string.Empty;

            if (appSettings.Environment == CONSTANTS.PAMEnvironment)
            {
                token = this.GeneratePAMToken();
            }
            else
            {
                if (appSettings.authProvider.ToUpper() == CONSTANTS.FORM)
                {
                    using (var httpClient = new HttpClient())
                    {
                        httpClient.BaseAddress = new Uri(appSettings.tokenAPIUrl);
                        httpClient.DefaultRequestHeaders.Accept.Clear();
                        httpClient.DefaultRequestHeaders.Add(CONSTANTS.UserName, appSettings.username);
                        httpClient.DefaultRequestHeaders.Add(CONSTANTS.Password, appSettings.password);
                        HttpContent content = new StringContent(string.Empty);
                        System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                        var result = httpClient.PostAsync(string.Empty, content).Result;
                        if (result.StatusCode != System.Net.HttpStatusCode.OK)
                        {
                            throw new Exception(string.Format(CONSTANTS.TokenError, result.StatusCode));
                        }

                        var tokenObj = JsonConvert.DeserializeObject(result.Content.ReadAsStringAsync().Result) as dynamic;
                        token = Convert.ToString(tokenObj.access_token);
                    }
                }
                else if (appSettings.authProvider.ToUpper() == CONSTANTS.AZUREAD)
                {
                    var client = new RestClient(appSettings.token_Url);
                    var request = new RestRequest(Method.POST);
                    request.AddHeader("cache-control", "no-cache");
                    request.AddHeader("content-type", "application/x-www-form-urlencoded");
                    request.AddParameter("application/x-www-form-urlencoded",
                       "grant_type=" + appSettings.Grant_Type +
                       "&client_id=" + appSettings.clientId +
                       "&client_secret=" + appSettings.clientSecret +
                       "&scope=" + appSettings.scopeStatus +
                       "&resource=" + appSettings.resourceId,
                       ParameterType.RequestBody);
                    IRestResponse response = client.Execute(request);
                    var tokenObj = JsonConvert.DeserializeObject(response.Content) as dynamic;
                    token = Convert.ToString(tokenObj.access_token);
                }
            }
            return token;
        }

        //public string GeneratestageToken()
        //{
        //    dynamic token = string.Empty;
        //    var client = new RestClient("https://login.microsoftonline.com/e0793d39-0939-496d-b129-198edd916feb/oauth2/token");
        //    var request = new RestRequest(Method.POST);
        //    request.AddHeader("cache-control", "no-cache");
        //    request.AddHeader("content-type", "application/x-www-form-urlencoded");
        //    request.AddParameter("application/x-www-form-urlencoded",
        //       "grant_type=client_credentials" +
        //       "&client_id=6a87813a-8368-4be7-9d6e-ec0a0b98e10f" +
        //       "&client_secret=djhqU0AqeDdyNyphdlhaNA==" +
        //       "&scope=openid" +
        //       "&resource=6a87813a-8368-4be7-9d6e-ec0a0b98e10f",
        //       ParameterType.RequestBody);

        //    IRestResponse response = client.Execute(request);
        //    string json = response.Content;
        //    // Retrieve and Return the Access Token                
        //    var tokenObj = JsonConvert.DeserializeObject(json) as dynamic;
        //    token = Convert.ToString(tokenObj.access_token);
        //    return token;
        //}

        //public string GenerateVDSToken()
        //{
        //    dynamic token = string.Empty;
        //    var client = new RestClient(appSettings.token_Url_VDS);
        //    var request = new RestRequest(Method.POST);
        //    request.AddHeader("cache-control", "no-cache");
        //    request.AddHeader("content-type", "application/x-www-form-urlencoded");
        //    request.AddParameter("application/x-www-form-urlencoded", "grant_type=" + appSettings.Grant_Type_VDS +
        //       "&client_id=" + appSettings.clientId_VDS +
        //       "&client_secret=" + appSettings.clientSecret_VDS +
        //       "&scope=" + appSettings.scopeStatus_VDS +
        //       "&resource=" + appSettings.resourceId_VDS,
        //       ParameterType.RequestBody);

        //    IRestResponse response = client.Execute(request);
        //    string json = response.Content;
        //    // Retrieve and Return the Access Token                
        //    var tokenObj = JsonConvert.DeserializeObject(json) as dynamic;
        //    token = Convert.ToString(tokenObj.access_token);
        //    return token;

        //}


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public dynamic GeneratePAMToken()
        {
            dynamic tokenObj = null;
            if (appSettings.authProvider.ToUpper() == CONSTANTS.FORM)
            {
                using (var httpClient = new HttpClient())
                {



                    httpClient.BaseAddress = new Uri(appSettings.PAMTokenUrl);
                    httpClient.DefaultRequestHeaders.Accept.Clear();
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    string json = JsonConvert.SerializeObject(new
                    {
                        username = appSettings.UserNamePAM,
                        password = appSettings.PasswordPAM
                    }) ;
                var requestOptions = new StringContent(json, Encoding.UTF8, "application/json");


                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                    var result = httpClient.PostAsync("", requestOptions).Result;
                    if (result.StatusCode != HttpStatusCode.OK)
                    {
                        throw new Exception(string.Format(CONSTANTS.TokenError, result.StatusCode));
                    }
                    var result1 = result.Content.ReadAsStringAsync().Result;
                    tokenObj = JsonConvert.DeserializeObject(result1) as dynamic;
                    //var token = Convert.ToString(tokenObj.token);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(PhoenixTokenService), nameof(GeneratePAMToken), "GenerateToken- END", string.Empty, string.Empty, string.Empty, string.Empty);
                    return tokenObj;
                }
            }
            else if (appSettings.authProvider.ToUpper() == CONSTANTS.AZUREAD)
            {
                var client = new RestClient(appSettings.token_Url);
                var request = new RestRequest(Method.POST);
                request.AddHeader("cache-control", "no-cache");
                request.AddHeader("content-type", "application/x-www-form-urlencoded");
                request.AddParameter("application/x-www-form-urlencoded",
                "grant_type=" + appSettings.Grant_Type +
                "&client_id=" + appSettings.clientId +
                "&client_secret=" + appSettings.clientSecret +
                "&scope=" + appSettings.scopeStatus +
                "&resource=" + appSettings.resourceId,
                ParameterType.RequestBody);
                IRestResponse response = client.Execute(request);
                tokenObj = JsonConvert.DeserializeObject(response.Content) as dynamic;
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(PhoenixTokenService), nameof(GeneratePAMToken), "GenerateToken- END", string.Empty, string.Empty, string.Empty, string.Empty);
                return Convert.ToString(tokenObj.token);
            }



            return tokenObj;
        }

        /// <summary>
        /// Generate token for marketplace
        /// </summary>
        /// <returns></returns>
        public string GenerateMarketPlaceToken()
        {
            dynamic token = string.Empty;
            if (appSettings.authProvider.ToUpper() == "FORM")
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.BaseAddress = new Uri(appSettings.tokenAPIUrl);
                    httpClient.DefaultRequestHeaders.Accept.Clear();
                    httpClient.DefaultRequestHeaders.Add("UserName", appSettings.username);
                    httpClient.DefaultRequestHeaders.Add("Password", appSettings.password);
                    HttpContent content = new StringContent(string.Empty);
                    System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                    var result = httpClient.PostAsync("", content).Result;

                    if (result.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        throw new Exception(string.Format("Unable to process your request for generating token. Please check your credentials and try again. Status Code: {0}", result.StatusCode));
                    }

                    var result1 = result.Content.ReadAsStringAsync().Result;
                    var tokenObj = JsonConvert.DeserializeObject(result1) as dynamic;

                    token = Convert.ToString(tokenObj.access_token);

                }
            }
            else
            {
                var usertoken = Accenture.MyWizard.Fortress.Core.Configurations.AuthProvider.GetToken(appSettings.market_ClientId, appSettings.market_ClientSecret, appSettings.market_ResourceId);

                if (usertoken != null)
                    token = usertoken.AccessToken;
                if (string.IsNullOrEmpty(token))
                {
                    var client = new RestClient(appSettings.token_Url);
                    var request = new RestRequest(Method.POST);
                    request.AddHeader("cache-control", "no-cache");
                    request.AddHeader("content-type", "application/x-www-form-urlencoded");
                    request.AddParameter("application/x-www-form-urlencoded",
                       "grant_type=" + appSettings.Grant_Type +
                       "&client_id=" + appSettings.market_ClientId +
                       "&client_secret=" + appSettings.market_ClientSecret +
                       "&resource=" + appSettings.market_ResourceId,
                       ParameterType.RequestBody);

                    IRestResponse response = client.Execute(request);
                    string json = response.Content;
                    // Retrieve and Return the Access Token                
                    var tokenObj = JsonConvert.DeserializeObject(json) as dynamic;
                    token = Convert.ToString(tokenObj.access_token);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(PhoenixTokenService), nameof(GenerateMarketPlaceToken), "grant_type=" + appSettings.Grant_Type +
                       "&client_id=" + appSettings.market_ClientId +
                       "&client_secret=" + appSettings.market_ClientSecret +
                       "&resource=" + appSettings.market_ResourceId, string.Empty, string.Empty, string.Empty, string.Empty);
                }
            }
            return token;
        }

        public dynamic GeneratePAMTokenFromCookies()
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(PhoenixTokenService), nameof(GeneratePAMToken), "START", string.Empty, string.Empty, string.Empty, string.Empty);
            if (appSettings.authProvider.ToUpper() == "FORM")
            {
                string tokenCookie = string.Empty;
                //Dictionary<string, string> response = new Dictionary<string, string>();
                if (_httpContext.HttpContext.Request != null && _httpContext.HttpContext.Request.Cookies["AUTH_SESSION"] != null)
                    tokenCookie = _httpContext.HttpContext.Request.Cookies["AUTH_SESSION"];

                if (!string.IsNullOrEmpty(tokenCookie))
                {
                    AuthToken authToken = new AuthToken();
                    authToken = GenerateToken(tokenCookie);
                    AuthorizeToken tokenClaims = new AuthorizeToken();
                    if (authToken != null)
                    {
                        tokenClaims = ValidateToken(authToken.Token);
                        if (tokenClaims != null)
                        {
                            authToken.UserId = tokenClaims.UserName;
                            return authToken;
                        }
                    }
                }
            }

            return null;
        }

        private AuthToken GenerateToken(string authSession)
        {
            AuthToken response = new AuthToken();
            if (!string.IsNullOrEmpty(authSession))
            {
                HttpClientHandler handler = new HttpClientHandler();
                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                handler.CookieContainer = new CookieContainer();
                string url = appSettings.token_Url;
                handler.CookieContainer.Add(new Uri(url), new Cookie("AUTH_SESSION", authSession)); // Adding a Cookie
                using (HttpClient httpClient = new HttpClient(handler))
                {
                    var httpResponse = httpClient.PostAsync(url, null).Result;
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(PhoenixTokenService), nameof(GenerateToken), "GenerateToken- httpResponse: " + httpResponse.IsSuccessStatusCode, string.Empty, string.Empty, string.Empty, string.Empty);
                    if (!httpResponse.IsSuccessStatusCode)
                    {
                        response = null;
                    }
                    else
                    {
                        var result = httpResponse.Content.ReadAsStringAsync().Result;
                        if (!string.IsNullOrEmpty(result))
                        {
                            response = JsonConvert.DeserializeObject<AuthToken>(result);
                        }
                        else
                        {
                            response = null;
                        }
                    }
                }
            }
            else
            {
                return null;
            }

            return response;
        }

        private AuthorizeToken ValidateToken(string token)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(PhoenixTokenService), nameof(ValidateToken), "ValidateToken- START", string.Empty, string.Empty, string.Empty, string.Empty);
            AuthorizeToken response = new AuthorizeToken();
            try
            {
                if (string.IsNullOrEmpty(token))
                    return null;

                HttpClient httpClient = new HttpClient();
                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                System.Uri url = new System.Uri(appSettings.PamTokenValidationURL);
                HttpResponseMessage httpResponse = httpClient.PostAsJsonAsync(url, new { token = token }).Result;
                if (!httpResponse.IsSuccessStatusCode)
                {
                    response = null;
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

                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(PhoenixTokenService), nameof(ValidateToken), "ValidateToken- Response: " + response, string.Empty, string.Empty, string.Empty, string.Empty);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(PhoenixTokenService), nameof(ValidateToken), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                //UpdateCookieInfoInSession(null);
                return response = null;
            }

            LOGGING.LogManager.Logger.LogProcessInfo(typeof(PhoenixTokenService), nameof(ValidateToken), "END", string.Empty, string.Empty, string.Empty, string.Empty);
            return response;
        }
    }
}