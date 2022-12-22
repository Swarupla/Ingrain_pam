#region Copyright (c) 2019 Accenture . All rights reserved.

// <copyright company="Accenture">
// Copyright (c) 2019 Accenture.  All rights reserved.
// Reproduction or transmission in whole or in part, in any form or by any means, 
// electronic, mechanical or otherwise, is prohibited without the prior written 
// consent of the copyright owner.
// </copyright>

#endregion

#region LoginController Information
/********************************************************************************************************\
Module Name     :   LoginController
Project         :   Accenture.MyWizard.SelfServiceAI.LoginController
Organisation    :   Accenture Technologies Ltd.
Created By      :   RaviA
Created Date    :   30-Jan-2019
Revision History :
Revision No             Initial             Modified Date           Description
1.0                     An                  30-Jan-2019             
\********************************************************************************************************/
#endregion

namespace Accenture.MyWizard.SelfServiceAI.LoginService
{
    using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
    using Accenture.MyWizard.Ingrain.DataModels.Models;
    using Accenture.MyWizard.Ingrain.WebService;
    #region Namespace
    using Microsoft.AspNetCore.Mvc;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;

    using System;

    using System.IO;
  
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.AspNetCore.Hosting;
    using Accenture.MyWizard.Shared.Helpers;
    using Microsoft.Extensions.Options;
    using System.Runtime.InteropServices;
    using Accenture.MyWizard.Ingrain.BusinessDomain.Common;
    //using iText.Html2pdf;
    #endregion

    public class LoginController : MyWizardControllerBase
    {
        #region Members
        private IngrainMarketLoginsData _IngrainMarketLoginsService = null;

        private static ILoginService loginService { set; get; }

        private readonly IHostingEnvironment _hostingEnvironment;

        private IngrainAppSettings appSettings { get; set; }
        #endregion

        public LoginController(IServiceProvider serviceProvider, IHostingEnvironment environment, IOptions<IngrainAppSettings> settings)
        {
            _IngrainMarketLoginsService = new IngrainMarketLoginsData();
            loginService = serviceProvider.GetService<ILoginService>();
            _hostingEnvironment = environment;
            appSettings = settings.Value;
        }

        /// <summary>
        /// User Login Details Validating
        /// </summary>
        /// <returns>User Details</returns>
        [HttpPost]
        [Route("api/ValidateUser")]
        public IActionResult ValidateUser([FromBody]dynamic requestBody)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(LoginController), nameof(ValidateUser), "Start", string.Empty, string.Empty, string.Empty, string.Empty);
            UserDTO userDTO = null;
            try
            {
                if (Convert.ToString(requestBody) != null)
                {
                    var loginData = JObject.Parse(Convert.ToString(requestBody));
                    string userName = loginData["userName"].ToString();
                    string password = loginData["password"].ToString();

                    #region VALIDATIONS
                    if (!CommonUtility.GetValidUser(Convert.ToString(loginData["userName"])))
                        return GetFaultResponse(Resource.IngrainResx.InValidUser);                    
                    #endregion


                    if (!string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(password))
                    {
                        userDTO = loginService.ValidateUser(userName, password);
                        if (userDTO == null || userDTO.IsValidUser == false)
                        {
                            return NotFound(Resource.IngrainResx.UserNotExist);
                        }
                    }
                    else
                    {
                        return NotFound(Resource.IngrainResx.InputData);
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(LoginController), nameof(ValidateUser), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(LoginController), nameof(ValidateUser), "End", string.Empty, string.Empty, string.Empty, string.Empty);
            return Ok(userDTO);
        }

        /// <summary>
        /// Save Ingrain Market Place Login Details and trigger email
        /// </summary>
        /// <returns>Status Code</returns>
        [Route("api/SaveIngrainMarketLoginData")]
        [HttpPost]
        public IActionResult SaveIngrainMarketLoginData([FromBody]dynamic requestBody)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(LoginController), nameof(SaveIngrainMarketLoginData), "Start", string.Empty, string.Empty, string.Empty, string.Empty);
            string ingrainMarketPlaceBodyData = Convert.ToString(requestBody); 
            try
            {
                if (!string.IsNullOrEmpty(ingrainMarketPlaceBodyData))
                {
                    _IngrainMarketLoginsService = JsonConvert.DeserializeObject<IngrainMarketLoginsData>(ingrainMarketPlaceBodyData);
                    if (!CommonUtility.GetValidUser(_IngrainMarketLoginsService.UserEnterPriseId))
                    {
                        return GetFaultResponse(Resource.IngrainResx.InValidUser);
                    }
                    _IngrainMarketLoginsService._id = Guid.NewGuid().ToString();
                    
                    bool duplicateUser = loginService.GetMarketLoginData(_IngrainMarketLoginsService.UserEnterPriseId);
                    if (duplicateUser)
                    {
                        return Conflict(Resource.IngrainResx.DuplicateUser);
                    }
                    else
                    {
                        loginService.InsertMarketPlaceData(_IngrainMarketLoginsService);
                        bool mailSuccess = loginService.SendEMail(_IngrainMarketLoginsService);
                        if (mailSuccess)
                        {
                            return Ok(Resource.IngrainResx.Created);
                        }
                        else
                        {
                            return GetFaultResponse(Resource.IngrainResx.MailNotSent);
                        }
                    }
                }
                else
                {
                    return GetFaultResponse(Resource.IngrainResx.EmptyData);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(LoginController), nameof(SaveIngrainMarketLoginData), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(Resource.IngrainResx.InternalServererror);
            }
        }

        /// <summary>
        /// Check if user exists in market place user collection
        /// </summary>
        /// <returns></returns>
        [Route("api/GetRegisteredMarketPlaceUser")]
        [HttpGet]
        public IActionResult GetRegisteredMarketPlaceUser(string userId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(LoginController), nameof(GetRegisteredMarketPlaceUser), "Start", string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                if (!string.IsNullOrEmpty(userId))
                {
                    if (!CommonUtility.GetValidUser(userId))
                    {
                        return GetFaultResponse(Resource.IngrainResx.InValidUser);
                    }
                    bool isUserExists = loginService.GetRegisteredUser(userId);
                    if (isUserExists)
                    {
                        return Ok(true);
                    }
                    else
                    {
                        return Ok(false);
                    }
                }
                else
                {
                    return GetFaultResponse(Resource.IngrainResx.EmptyData);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(LoginController), nameof(GetRegisteredMarketPlaceUser), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(Resource.IngrainResx.InternalServererror + ex.Message + ex.StackTrace);
            }
        }

        /// <summary>
        /// Register User to market place
        /// </summary>
        /// <returns></returns>
        [Route("api/RegisterMarketPlaceUser")]
        [HttpPost]
        public IActionResult RegisterMarketPlaceUser([FromBody]dynamic requestBody)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(LoginController), nameof(RegisterMarketPlaceUser), "Start", string.Empty, string.Empty, string.Empty, string.Empty);
            string userInfo = Convert.ToString(requestBody);
            try
            {
                if (!string.IsNullOrEmpty(userInfo))
                {
                    var marketPlaceUserDetails = new MarketPlaceUserModel();
                    marketPlaceUserDetails = JsonConvert.DeserializeObject<MarketPlaceUserModel>(userInfo);
                    if (!CommonUtility.GetValidUser(marketPlaceUserDetails.UserId))
                    {
                        return GetFaultResponse(Resource.IngrainResx.InValidUser);
                    }
                    marketPlaceUserDetails._id = Guid.NewGuid().ToString();
                    marketPlaceUserDetails.CreatedByUser = marketPlaceUserDetails.UserId;
                    marketPlaceUserDetails.CreatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    marketPlaceUserDetails.ModifiedByUser = marketPlaceUserDetails.UserId;
                    marketPlaceUserDetails.ModifiedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    // Check if user already has requested for template
                    bool isTemplateExists = loginService.GetUserTemplateDetails(marketPlaceUserDetails);
                    if (isTemplateExists)
                    {
                        return Conflict("User already has Template");
                    }
                    else
                    {
                        //New Template request for user
                        string categoryName = loginService.RegisterUserToMarketPlace(marketPlaceUserDetails);
                        if (!string.IsNullOrEmpty(categoryName))
                        {
                            return Ok(categoryName);
                        }
                        else
                        {
                            return GetFaultResponse(Resource.IngrainResx.EmptyData);
                        }
                    }
                }
                else
                {
                    return GetFaultResponse(Resource.IngrainResx.EmptyData);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(LoginController), nameof(RegisterMarketPlaceUser), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(Resource.IngrainResx.InternalServererror + ex.Message + ex.StackTrace);
            }
        }

        /// <summary>
        /// Get all column data
        /// </summary>
        /// <returns></returns>
        [Route("api/GetExcelData")]
        [HttpGet]
        public IActionResult GetExcelData(string correlationId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(LoginController), nameof(GetExcelData), "Start", string.IsNullOrEmpty(correlationId) ? default(Guid) : new Guid(correlationId), string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                if (!string.IsNullOrEmpty(correlationId))
                {
                    var data = loginService.GetAllData(correlationId);
                    return Ok(data);
                }
                else
                {
                    return GetFaultResponse(Resource.IngrainResx.EmptyData);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(LoginController), nameof(GetExcelData), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(Resource.IngrainResx.InternalServererror + ex.Message + ex.StackTrace);
            }

        }

        /// <summary>
        /// Get Registered User information for market place user
        /// </summary>
        /// <returns></returns>
        [Route("api/GetMarketPlaceUserInfo")]
        [HttpGet]
        public IActionResult GetMarketPlaceUserInfo(string userId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(LoginController), nameof(GetMarketPlaceUserInfo), "Start", string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                if (!string.IsNullOrEmpty(userId))
                {
                    if (!CommonUtility.GetValidUser(userId))
                    {
                        return GetFaultResponse(Resource.IngrainResx.InValidUser);
                    }
                    var data = loginService.GetRegisteredUserInfo(userId);
                    return Ok(data);
                }
                else
                {
                    return GetFaultResponse(Resource.IngrainResx.EmptyData);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(LoginController), nameof(GetMarketPlaceUserInfo), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(Resource.IngrainResx.InternalServererror + ex.Message + ex.StackTrace);
            }

        }


        /// <summary>
        /// Get market place trail User
        /// </summary>
        /// <returns></returns>
        [Route("api/MarketPlaceTrialUserData")]
        [HttpGet]
        public IActionResult MarketPlaceTrialUserData(string userId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(LoginController), nameof(MarketPlaceTrialUserData), "Start", string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                if (!string.IsNullOrEmpty(userId))
                {
                    if (!CommonUtility.GetValidUser(userId))
                    {
                        return GetFaultResponse(Resource.IngrainResx.InValidUser);
                    }
                    var data = loginService.MarketPlaceTrialUserData(userId);
                    return Ok(data);
                }
                else
                {
                    return GetFaultResponse(Resource.IngrainResx.EmptyData);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(LoginController), nameof(MarketPlaceTrialUserData), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(Resource.IngrainResx.InternalServererror + ex.Message + ex.StackTrace);
            }

        }
        /// <summary>
        /// Update User provisioned Template for redirection
        /// </summary>
        /// <returns></returns>
        [Route("api/UpdateUserTemplateFlag")]
        [HttpPost]
        public IActionResult UpdateUserTemplateFlag(string userId, string templateName)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(LoginController), nameof(GetMarketPlaceUserInfo), "Start", string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                if (!string.IsNullOrEmpty(userId))
                {
                    if (!CommonUtility.GetValidUser(userId))
                    {
                        return GetFaultResponse(Resource.IngrainResx.InValidUser);
                    }
                    
                    var data = loginService.UpdateTemplateFlag(userId, templateName);
                    return Ok(data);
                }
                else
                {
                    return GetFaultResponse(Resource.IngrainResx.EmptyData);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(LoginController), nameof(GetMarketPlaceUserInfo), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(Resource.IngrainResx.InternalServererror + ex.Message + ex.StackTrace);
            }

        }

        /// <summary>
        /// Get Market place provisioned Templates for user
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="category"></param>
        /// <returns></returns>
        [Route("api/GetMarketPlaceUserTemplate")]
        [HttpGet]
        public IActionResult GetMarketPlaceUserTemplate(string userId, string category)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(LoginController), nameof(GetMarketPlaceUserTemplate), "Start", string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                if (!string.IsNullOrEmpty(userId))
                {
                    if (!CommonUtility.GetValidUser(userId))
                    {
                        return GetFaultResponse(Resource.IngrainResx.InValidUser);
                    }
                    CommonUtility.ValidateInputFormData(category, CONSTANTS.Category, false);

                    var data = loginService.GetMarketPlaceUserTemplate(userId, category);
                    return Ok(data);
                }
                else
                {
                    return GetFaultResponse(Resource.IngrainResx.EmptyData);
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(LoginController), nameof(GetMarketPlaceUserTemplate), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(Resource.IngrainResx.InternalServererror + ex.Message + ex.StackTrace);
            }

        }

        /// <summary>
        /// Configure Certification Flag
        /// </summary>
        /// <param name="flag">flag</param>
        /// <returns></returns>
        [Route("api/ConfigureCertificationFlag")]
        [HttpPost]
        public IActionResult ConfigureCertificationFlag(bool flag)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(LoginController), nameof(ConfigureCertificationFlag), "Start", string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                var data = loginService.ConfigureCertificationFlag(flag);
                return Ok(data);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(LoginController), nameof(ConfigureCertificationFlag), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(Resource.IngrainResx.InternalServererror + ex.Message + ex.StackTrace);
            }

        }

        /// <summary>
        /// Get Certification Flag Value
        /// </summary>
        /// <returns></returns>
        [Route("api/GetCertificationFlag")]
        [HttpGet]
        public IActionResult GetCertificationFlag()
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(LoginController), nameof(GetCertificationFlag), "Start", string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                var data = loginService.GetCertificationFlag();
                return Ok(data);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(LoginController), nameof(GetCertificationFlag), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(Resource.IngrainResx.InternalServererror + ex.Message + ex.StackTrace);
            }

        }


        /// <summary>
        /// Certificate User Email
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="userEmail"></param>
        /// <returns></returns>
        [Route("api/CertificateUserEmail")]
        [HttpPost]
        public IActionResult CertificateUserEmail(string userName, string userEmail)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(LoginController), nameof(CertificateUserEmail), "Start- Certificate Email  Trigger" + "user Name:" + userName + "user Email:" + userEmail, string.Empty, string.Empty, string.Empty, string.Empty);
            var certificateFilepath = Path.Combine(_hostingEnvironment.ContentRootPath + CONSTANTS.CertificateFilePath);
            if (!string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(userEmail))
            {
                try
                {
                    if (!CommonUtility.GetValidUser(userEmail))
                    {
                        return GetFaultResponse(Resource.IngrainResx.InValidUser);
                    }
                    loginService.ReplaceTextInFile(userName);
                    //this.ConvertHTMlTPDF();
                    // this.convertHTMLTOPDFHIQ();
                  //  this.convertHtmlToPdfiText();
                    bool success = loginService.SendEmailForCertificate(userName, userEmail);
                    if (success)
                    {
                        if (System.IO.File.Exists(certificateFilepath + "Certification_New.html"))
                        {
                            System.IO.File.Delete(certificateFilepath + "Certification_New.html");
                        }

                        if (System.IO.File.Exists(certificateFilepath + "Certification.pdf"))
                        {
                            System.IO.File.Delete(certificateFilepath + "Certification.pdf");
                        }
                        return Ok("Success");

                    }
                    else
                    {
                        return GetFaultResponse(Resource.IngrainResx.MailNotSent);
                    }
                }
                catch (Exception ex)
                {
                    LOGGING.LogManager.Logger.LogErrorMessage(typeof(LoginController), nameof(CertificateUserEmail), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                    return GetFaultResponse(Resource.IngrainResx.InternalServererror + ex.Message + ex.StackTrace);
                }
            }
            return Ok("Success");
        }

        //[NonAction]
        //public void convertHtmlToPdfiText()
        //{
        //    string certificateFilePath = Path.Combine(_hostingEnvironment.ContentRootPath + CONSTANTS.CertificateFilePath);
        //    using (FileStream htmlSource = System.IO.File.Open(certificateFilePath + "Certification_New.html", FileMode.Open))
        //    using (FileStream pdfDest = System.IO.File.Open(certificateFilePath + "Certification.pdf", FileMode.OpenOrCreate))
        //    {
        //        ConverterProperties converterProperties = new ConverterProperties();
        //        string path = Path.Combine(_hostingEnvironment.ContentRootPath + "/Certificate");
        //        converterProperties.SetBaseUri(path);
        //        HtmlConverter.ConvertToPdf(htmlSource, pdfDest, converterProperties);
        //    }

        //    //string ORIG = certificateFilePath + "Certification_New.html";
        //    //string pdfDest = certificateFilePath + "Certification.pdf";
        //    //HtmlConverter.ConvertToPdf(new FileStream(ORIG, FileMode.Open), new FileStream(pdfDest, FileMode.Create));


        //}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filepath"></param>
        /// <returns></returns>
        [Route("api/EncryptMarketplaceFile")]
        [HttpPost]
        public IActionResult EncryptMarketplaceFile(string filepath)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(LoginController), nameof(EncryptMarketplaceFile), "Start", string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                var fileCollection = HttpContext.Request.Form.Files;
                var data = loginService.EncryptMarketplaceFile(HttpContext, filepath);
                return Ok(data);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(LoginController), nameof(EncryptMarketplaceFile), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(Resource.IngrainResx.InternalServererror + ex.Message + ex.StackTrace);
            }

        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="filepath"></param>
        /// <returns></returns>
        [Route("api/DecryptMarketplaceFile")]
        [HttpGet]
        public IActionResult DecryptMarketplaceFile(string filepath)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(LoginController), nameof(DecryptMarketplaceFile), "Start", string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                string fileName = string.Empty;
                var data = loginService.DecryptMarketplaceFile(filepath);
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    fileName = filepath.Split("/")[filepath.Split("/").Length - 1];
                }
                else
                {
                    fileName = filepath.Split("\\")[filepath.Split("\\").Length - 1];
                }
                return File(data, "application/octet-stream", fileName);
               
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(LoginController), nameof(DecryptMarketplaceFile), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(Resource.IngrainResx.InternalServererror + ex.Message + ex.StackTrace);
            }

        }


    }
}
