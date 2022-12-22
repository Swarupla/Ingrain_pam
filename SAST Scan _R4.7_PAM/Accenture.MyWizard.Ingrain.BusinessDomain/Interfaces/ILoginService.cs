using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Accenture.MyWizard.Ingrain.DataModels.Models;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;

namespace Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces
{
    public interface ILoginService
    {
        UserDTO ValidateUser(string userName, string password);

        /// <summary>
        /// Ingrain Market Place Login Details
        /// </summary>
        /// <param name="ingrainMarketLoginsData"></param>
        void InsertMarketPlaceData(IngrainMarketLoginsData ingrainMarketLoginsData);

        /// <summary>
        /// Populate Email Body Content
        /// </summary>
        /// <param name="emailBody">Email Body</param>
        /// <returns></returns>
        string PopulateBody(IngrainMarketLoginsData emailBody);

        /// <summary>
        /// Send Email feature
        /// </summary>
        /// <param name="recipient"></param>
        /// <param name="subject"></param>
        /// <param name="emailBody"></param>
        /// <returns></returns>
        bool SendEMail(IngrainMarketLoginsData emailBody);

        /// <summary>
        /// Get Market Login Data
        /// </summary>
        /// <param name="enterpriseId"></param>
        /// <returns></returns>
        bool GetMarketLoginData(string enterpriseId);

        /// <summary>
        /// Get Registered Market place User
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        bool GetRegisteredUser(string userId);

        /// <summary>
        /// Check if User has already requested for Template
        /// </summary>
        /// <param name="marketPlaceUser"></param>
        /// <returns></returns>
        bool GetUserTemplateDetails(MarketPlaceUserModel marketPlaceUser);

        /// <summary>
        /// Register User To Market Place
        /// </summary>
        /// <param name="marketPlaceUser"></param>
        /// <returns></returns>
        string RegisterUserToMarketPlace(MarketPlaceUserModel marketPlaceUser);

        /// <summary>
        /// Get Data for Excel
        /// </summary>
        /// <param name="correlationId"></param>
        /// <returns></returns>
        List<object> GetAllData(string correlationId);


        /// <summary>
        /// Get all the user information for Market place user
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        object GetRegisteredUserInfo(string userId);

        /// <summary>
        /// Get market place trail User
        /// </summary>
        /// <returns></returns>
        object MarketPlaceTrialUserData(string userId);
        /// <summary>
        /// Update User provisioned Template for redirection
        /// </summary>
        /// <returns></returns>
        string UpdateTemplateFlag(string userId, string templateName);

        /// <summary>
        /// Get Market place provisioned Templates for user
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="category"></param>
        /// <returns></returns>
        List<JObject> GetMarketPlaceUserTemplate(string userId, string category);

        /// <summary>
        /// Configure Certification Flag
        /// </summary>
        /// <param name="flag">flag</param>
        /// <returns></returns>
        string ConfigureCertificationFlag(bool flag);

        /// <summary>
        /// Get Certification Flag Value
        /// </summary>
        /// <returns></returns>
        bool GetCertificationFlag();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userId"></param>
        void ReplaceTextInFile(string userId);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="userEmailId"></param>
        /// <returns></returns>
        bool SendEmailForCertificate(string userId, string userEmailId);

        string EncryptMarketplaceFile(HttpContext files,string filePath);

        dynamic DecryptMarketplaceFile(string filePath);

    }
}
