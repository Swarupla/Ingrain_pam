using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
using Accenture.MyWizard.Ingrain.WebService;
using Accenture.MyWizard.Shared.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Ninject;
using RestSharp;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
using Accenture.MyWizard.Ingrain.WebService.Controllers;

namespace Accenture.MyWizard.SelfServiceAI.WebService.Controllers
{
    public class TokenController : MyWizardControllerBase
    {

        private readonly IOptions<IngrainAppSettings> appSettings;

        public static IPhoenixTokenService _iPhoenixTokenService { get; set; }
        public TokenController(IOptions<IngrainAppSettings> settings, IServiceProvider serviceProvider)
        {
            appSettings = settings;
            _iPhoenixTokenService = serviceProvider.GetService<IPhoenixTokenService>();
        }

        //------------------------------------------------------------
        //***COMMENTED BELOW CODE FOR VULNERABILITY ISSUE***
        //------------------------------------------------------------

        //[HttpGet]
        //[Route("api/GetToken")]
        //public IActionResult GetToken()
        //{
        //    LOGGING.LogManager.Logger.LogProcessInfo(typeof(TokenController), nameof(GetToken), "START", string.Empty, string.Empty, string.Empty, string.Empty);
        //    try
        //    {
        //        var client = new RestClient(appSettings.Value.token_Url);
        //        var request = new RestRequest(Method.POST);
        //        request.AddHeader("cache-control", "no-cache");
        //        request.AddHeader("content-type", "application/x-www-form-urlencoded");
        //        request.AddParameter("application/x-www-form-urlencoded", "grant_type=" + appSettings.Value.Grant_Type +
        //            "&client_id=" + appSettings.Value.clientId +
        //            "&client_secret=" + appSettings.Value.clientSecret +
        //            "&resource=" + appSettings.Value.resourceId,
        //            ParameterType.RequestBody);

        //        IRestResponse response = client.Execute(request);
        //        string json = response.Content;
        //        // Retrieve and Return the Access Token
        //        //JavaScriptSerializer ser = new JavaScriptSerializer();
        //        Dictionary<string, object> x = (Dictionary<string, object>)(Newtonsoft.Json.JsonConvert.DeserializeObject(json));
        //        string accessToken = x["access_token"].ToString();
        //        return Ok(accessToken);
        //    }
        //    catch (Exception ex)
        //    {
        //        LOGGING.LogManager.Logger.LogErrorMessage(typeof(TokenController), nameof(GetToken), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);

        //    }
        //    LOGGING.LogManager.Logger.LogProcessInfo(typeof(TokenController), nameof(GetToken), "END", string.Empty, string.Empty, string.Empty, string.Empty);

        //    return Ok(Resource.IngrainResx.InputData);
        //}
        /// <summary>
        /// API gets called from UI for PAM Environment
        /// </summary>
        /// <returns></returns>

        /*[HttpGet]
        [Route("api/GetToken")]
        public IActionResult GetToken()
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(TokenController), nameof(GetToken), "START", string.Empty, string.Empty, string.Empty, string.Empty);
            string token = string.Empty;
            try
            {
                token = _iPhoenixTokenService.GenerateToken();
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(TokenController), nameof(GetToken), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return BadRequest(ex.Message + ex.StackTrace);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(TokenController), nameof(GetToken), "END", string.Empty, string.Empty, string.Empty, string.Empty);
            return Ok(token);
        }*/

        [HttpGet]
        [Route("api/GeneratePAMToken")]
        public IActionResult GeneratePAMToken()
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(TokenController), nameof(GeneratePAMToken), "Start", string.Empty, string.Empty, string.Empty, string.Empty);
            dynamic token = null;
            try
            {
                token = _iPhoenixTokenService.GeneratePAMToken();
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(TokenController), nameof(GeneratePAMToken), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return BadRequest(ex.Message + ex.StackTrace);
            }

            LOGGING.LogManager.Logger.LogProcessInfo(typeof(TokenController), nameof(GeneratePAMToken), "END", string.Empty, string.Empty, string.Empty, string.Empty);
            return Ok(token);
        }

        [HttpGet]
        [Route("api/ValidatePAMToken")]
        public IActionResult ValidatePAMToken()
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(TokenController), nameof(ValidatePAMToken), "START", string.Empty, string.Empty, string.Empty, string.Empty);
            dynamic token = null;
            try
            {
                //python is passing Token as bearer token , if authorisation is successful while calling API , then we need to return OK as response
                //no other code required at this point 
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(TokenController), nameof(ValidatePAMToken), "Token Validation successful", string.Empty, string.Empty, string.Empty, string.Empty);
                return Ok("Token is Valid");
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(TokenController), nameof(ValidatePAMToken), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return BadRequest(ex.Message + ex.StackTrace);
            }
        }
    }
}