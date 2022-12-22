#region Copyright (c) 2019 Accenture . All rights reserved.

// <copyright company="Accenture">
// Copyright (c) 2019 Accenture.  All rights reserved.
// Reproduction or transmission in whole or in part, in any form or by any means, 
// electronic, mechanical or otherwise, is prohibited without the prior written 
// consent of the copyright owner.
// </copyright>

#endregion

#region LoginService Information
/********************************************************************************************************\
Module Name     :   LoginService
Project         :   Accenture.MyWizard.SelfServiceAI.LoginService
Organisation    :   Accenture Technologies Ltd.
Created By      :   RaviA
Created Date    :   30-Jan-2019
Revision History :
Revision No             Initial             Modified Date           Description
1.0                     An                  30-Jan-2019             
\********************************************************************************************************/
#endregion

namespace Accenture.MyWizard.Ingrain.BusinessDomain.Services
{
    #region Namespaces
    using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
    using Accenture.MyWizard.Ingrain.DataAccess;
    using Accenture.MyWizard.Ingrain.DataModels.Models;
    using Accenture.MyWizard.Shared.Helpers;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Options;
    using MongoDB.Bson;
    using MongoDB.Bson.Serialization;
    using MongoDB.Driver;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Mail;
    using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.AspNetCore.Http;
    using Accenture.MyWizard.Ingrain.BusinessDomain.Common;
    #endregion

    public class LoginService : ILoginService
    {
        #region Members
        private MongoClient _mongoClient;
        private IMongoDatabase _database;
        private readonly IOptions<IngrainAppSettings> appSettings;
        DatabaseProvider databaseProvider;
        private static IScopeSelectorService _scopeSelectorService { set; get; }
        private IEncryptionDecryption _encryptionDecryption;
        private IHostingEnvironment _hostingEnvironment;

        #endregion

        #region Constructor
        public LoginService(DatabaseProvider db, IOptions<IngrainAppSettings> settings, IHostingEnvironment environment, IServiceProvider serviceProvider)
        {
            databaseProvider = db;
            appSettings = settings;
            _mongoClient = databaseProvider.GetDatabaseConnection();
            var dataBaseName = MongoUrl.Create(appSettings.Value.connectionString).DatabaseName;
            _database = _mongoClient.GetDatabase(dataBaseName);
            _hostingEnvironment = environment;
            _scopeSelectorService = serviceProvider.GetService<IScopeSelectorService>();
            _encryptionDecryption = serviceProvider.GetService<IEncryptionDecryption>();
        }
        #endregion

        #region Methods
        public UserDTO ValidateUser(string userName, string password)
        {
            UserDTO userDTO = new UserDTO();
            var userFilter = Builders<UserDTO>.Filter.Eq(CONSTANTS.UserName, userName);
            var passwordFilter = Builders<UserDTO>.Filter.Eq(CONSTANTS.Password, password);
            var filter = userFilter & passwordFilter;
            var collection = _database.GetCollection<UserDTO>(CONSTANTS.SSAIUserDetails);
            var result = collection.Find(filter);
            var results = collection.Find(filter).ToList();
            if (results.Count > 0)
            {
                foreach (var item in results)
                {
                    userDTO.UserId = item.UserId;
                    userDTO.UserName = item.UserName;
                    userDTO.FirstName = item.FirstName;
                    userDTO.LastName = item.LastName;
                    userDTO.IsValidUser = item.IsValidUser;
                    userDTO.Email = item.Email;
                    userDTO.CompanyId = item.CompanyId;
                }
            }
            else
            {
                userDTO.IsValidUser = false;
            }
            return userDTO;
        }

        /// <summary>
        /// Ingrain Market Logins Data 
        /// </summary>
        /// <param name="ingrainMarketLoginData">IngrainMarketLoginsData Model</param>
        public void InsertMarketPlaceData(IngrainMarketLoginsData ingrainMarketLoginData)
        {
            IngrainDeliveryConstruct ingrainDeliveryConstruct = new IngrainDeliveryConstruct();
            var userId = ingrainMarketLoginData.UserEnterPriseId;
            string[] splitId = userId.Split('@');
            ingrainMarketLoginData.UserAzureId = splitId[0] + CONSTANTS.AzureADDomain;//"@mwphoenix.onmicrosoft.com";
            var jsonData = JsonConvert.SerializeObject(ingrainMarketLoginData);
            var insertDocument = BsonSerializer.Deserialize<BsonDocument>(jsonData);
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.IngrainMarketLogins);
            collection.InsertOneAsync(insertDocument);
            //set default client and DC - market place user
            ingrainDeliveryConstruct.UserId = ingrainMarketLoginData.UserEnterPriseId;
            ingrainDeliveryConstruct.ClientUId = "6985f401-35ce-875a-15c1-f6c44e8ca233";
            ingrainDeliveryConstruct.DeliveryConstructUID = "78ddb609-dfe7-4fc1-a1d3-90d979e8d68c";
            _scopeSelectorService.PostDeliveryConstruct(ingrainDeliveryConstruct);
        }

        /// <summary>
        /// Get Market Login Data
        /// </summary>
        /// <param name="enterpriseId"></param>
        /// <returns></returns>
        public bool GetMarketLoginData(string enterpriseId)
        {
            bool isExists = false;

            try
            {
                var collection = _database.GetCollection<BsonDocument>(CONSTANTS.IngrainMarketLogins);
                var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.UserEnterPriseId, enterpriseId);
                var resultdocument = collection.Find(filter).ToList();
                if (resultdocument.Count > 0)
                {
                    isExists = true;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return isExists;
        }

        /// <summary>
        /// Send Email feature
        /// </summary>
        /// <param name="recipient"></param>
        /// <param name="subject"></param>
        /// <param name="emailBody"></param>
        /// <returns></returns>
        public bool SendEMail(IngrainMarketLoginsData emailBody)
        {
            //Fetching Settings from WEB.CONFIG file.  
            string recipient = appSettings.Value.Recipient;
            string emailSenderHost = appSettings.Value.SMTPServer;
            int emailSenderPort = Convert.ToInt16(appSettings.Value.PortNumber);
            bool isMessageSent = false;
            //Intialise Parameters  
            SmtpClient client = new SmtpClient(emailSenderHost);
            client.Port = emailSenderPort;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.UseDefaultCredentials = false;

            try
            {
                var mail = new MailMessage();
                mail.From = new MailAddress(emailBody.UserEnterPriseId);
                mail.To.Add(new MailAddress(recipient));
                mail.Subject = appSettings.Value.EmailSubject;
                mail.Body = this.PopulateBody(emailBody);
                mail.IsBodyHtml = true;
                mail.DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure;
                client.Send(mail);
                isMessageSent = true;
            }
            catch (Exception ex)
            {
                isMessageSent = false;
               LOGGING.LogManager.Logger.LogErrorMessage(typeof(LoginService), nameof(SendEMail), ex.StackTrace + ex.InnerException + CONSTANTS.Exception + ex.Message, ex,string.Empty, string.Empty, emailBody.ClientID,string.Empty);
            }
            return isMessageSent;
        }

        /// <summary>
        /// Populate Email Body Content
        /// </summary>
        /// <param name="emailBody">Email Body</param>
        /// <returns>Email Body</returns>
        public string PopulateBody(IngrainMarketLoginsData emailBody)
        {
            string body = string.Empty;
            using (StreamReader reader = new StreamReader(Path.Combine(_hostingEnvironment.ContentRootPath + CONSTANTS.EmailBodyTemplate)))
            {
                body = reader.ReadToEnd();
            }
            var userId = emailBody.UserEnterPriseId;
            string[] splitId = userId.Split('@');
            emailBody.UserAzureId = splitId[0] + CONSTANTS.AzureADDomain;//"@mwphoenix.onmicrosoft.com";
            body = body.Replace(CONSTANTS.EnterpriseId, emailBody.UserAzureId);
            body = body.Replace(CONSTANTS.UserEnterpriseId, emailBody.UserAzureId);
            body = body.Replace(CONSTANTS.clientId, emailBody.ClientID);
            body = body.Replace(CONSTANTS.engagementId, emailBody.EngagementID);
            body = body.Replace(CONSTANTS.clientName, emailBody.ClientName);
            body = body.Replace(CONSTANTS.projectName, emailBody.ProjectName);
            body = body.Replace(CONSTANTS.BusinessReason, emailBody.BusinessReason);
            body = body.Replace(CONSTANTS.InformationSource, emailBody.InformationSource);

            return body;
        }


        /// <summary>
        /// Get Registered User
        /// </summary>
        /// <param name="marketPlaceUser"></param>
        /// <returns></returns>
        public bool GetRegisteredUser(string userId)
        {
            bool isExists = false;
            try
            {
                var collection = _database.GetCollection<BsonDocument>(CONSTANTS.IngrainMarketLogins);
                var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.UserEnterPriseId, userId);
                var resultdocument = collection.Find(filter).ToList();
                if (resultdocument.Count > 0)
                {
                    isExists = true;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return isExists;
        }

        /// <summary>
        /// Check if User has already requested for Template
        /// </summary>
        /// <param name="marketPlaceUser"></param>
        /// <returns></returns>
        public bool GetUserTemplateDetails(MarketPlaceUserModel marketPlaceUser)
        {
            bool isExists = false;

            try
            {
                var collection = _database.GetCollection<BsonDocument>(CONSTANTS.RegisterMarketPlaceUser);
                var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.UserId, marketPlaceUser.UserId) & Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, marketPlaceUser.CorrelationId);
                var resultdocument = collection.Find(filter).ToList();
                if (resultdocument.Count > 0)
                {
                    isExists = true;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return isExists;
        }

        /// <summary>
        /// Register User To Market Place
        /// </summary>
        /// <param name="marketPlaceUser"></param>
        /// <returns></returns>
        public string RegisterUserToMarketPlace(MarketPlaceUserModel marketPlaceUser)
        {
            string category = string.Empty;
            var publicTemplate = _database.GetCollection<BsonDocument>(CONSTANTS.SSAI_PublicTemplates);
            var publicTemplateFilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, marketPlaceUser.CorrelationId);
            var publicTemplateDocument = publicTemplate.Find(publicTemplateFilter).ToList();
            if (publicTemplateDocument.Count > 0)
            {
                var modelName = publicTemplateDocument[0][CONSTANTS.ModelName].ToString();
                string[] modelList = modelName.Split(',');
                if (modelList.Length > 0)
                {
                    if (modelList[1] != null)
                    {
                        category = modelList[1].ToString().Replace("]", "").Trim();
                    }
                }

                if (!string.IsNullOrEmpty(category))
                {
                    marketPlaceUser.Category = category;
                    var userId = marketPlaceUser.UserId;
                    string[] splitId = userId.Split('@');
                    marketPlaceUser.UserAzureId = splitId[0] + CONSTANTS.AzureADDomain;//"@mwphoenix.onmicrosoft.com";
                    var jsonData = JsonConvert.SerializeObject(marketPlaceUser);
                    var insertDocument = BsonSerializer.Deserialize<BsonDocument>(jsonData);
                    var collection = _database.GetCollection<BsonDocument>(CONSTANTS.RegisterMarketPlaceUser);
                    collection.InsertOneAsync(insertDocument);
                }
            }
            else
            {
                var services = _database.GetCollection<BsonDocument>("Services");
                var servicesFilter = Builders<BsonDocument>.Filter.Eq("ServiceId", marketPlaceUser.CorrelationId);
                var projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
                var servicesDocument = services.Find(servicesFilter).ToList();
                if (servicesDocument.Count > 0)
                {
                    marketPlaceUser.Category = "others";
                    category = "others";
                    var userId = marketPlaceUser.UserId;
                    string[] splitId = userId.Split('@');
                    marketPlaceUser.UserAzureId = splitId[0] + CONSTANTS.AzureADDomain;//"@mwphoenix.onmicrosoft.com";
                    var jsonData = JsonConvert.SerializeObject(marketPlaceUser);
                    var insertDocument = BsonSerializer.Deserialize<BsonDocument>(jsonData);
                    var collection = _database.GetCollection<BsonDocument>(CONSTANTS.RegisterMarketPlaceUser);
                    collection.InsertOneAsync(insertDocument);
                }
            }
            return category;
        }

        /// <summary>
        /// Get Data for Excel
        /// </summary>
        /// <param name="correlationId"></param>
        /// <returns></returns>
        public List<object> GetAllData(string correlationId)
        {
            var excelData = new List<object>();
            var columnCollection = _database.GetCollection<BsonDocument>(CONSTANTS.PS_IngestedData);
            var filterPS = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var projection = Builders<BsonDocument>.Projection.Include(CONSTANTS.Input_Data).Exclude(CONSTANTS.Id);
            var inputData = columnCollection.Find(filterPS).Project<BsonDocument>(projection).ToList();
            if (inputData.Count > 0)
            {
                var data = inputData[0][CONSTANTS.Input_Data].ToString();
                excelData.AddRange(JsonConvert.DeserializeObject<List<object>>(data));

            }
            return excelData.Take(150).ToList();
        }

        /// <summary>
        /// Get all the user information for Market place user
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public object GetRegisteredUserInfo(string userId)
        {
            var userInfo = string.Empty;
            string[] splitId = userId.Split('@');
            var userAzureId = splitId[0] + CONSTANTS.AzureADDomain;//"@mwphoenix.onmicrosoft.com";
            var data = new JObject();
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.RegisterMarketPlaceUser);
            //  var filter = (Builders<BsonDocument>.Filter.Eq("UserId", userId) | Builders<BsonDocument>.Filter.Eq("UserAzureId", userAzureId)) & Builders<BsonDocument>.Filter.Eq("AccessRight", "true");
            var filter = (Builders<BsonDocument>.Filter.Eq(CONSTANTS.UserId, userId) | Builders<BsonDocument>.Filter.Eq(CONSTANTS.UserAzureId, userAzureId)) & Builders<BsonDocument>.Filter.Eq(CONSTANTS.AccessRight, CONSTANTS.True_Value);
            var projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
            var resultdocument = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (resultdocument.Count > 0)
            {
                userInfo = resultdocument[0].ToString();
                data = JObject.Parse(userInfo);
            }
            return data;
        }

        /// <summary>
        /// Get market place trail User
        /// </summary>
        /// <returns></returns>
        public object MarketPlaceTrialUserData(string userId)
        {
            var userInfo = string.Empty;
            var data = new JObject();
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.IngrainMarketLogins);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.UserEnterPriseId, userId);
            var projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
            var resultdocument = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (resultdocument.Count > 0)
            {
                userInfo = resultdocument[0].ToString();
                data = JObject.Parse(userInfo);
            }
            return data;
        }
        /// <summary>
        /// Update User provisioned Template for redirection
        /// </summary>
        /// <returns></returns>
        public string UpdateTemplateFlag(string userId, string templateName)
        {
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.RegisterMarketPlaceUser);
            var userFilter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.UserId, userId);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.UserId, userId) & Builders<BsonDocument>.Filter.Eq(CONSTANTS.TemplateName, templateName);
            var userData = collection.Find(userFilter).ToList();
            if (userData.Count > 0)
            {
                var templateUpdate = Builders<BsonDocument>.Update.Set(CONSTANTS.AccessRight, CONSTANTS.False_value);
                var updateResult = collection.UpdateMany(userFilter, templateUpdate);
            }
            var resultdocument = collection.Find(filter).ToList();
            if (resultdocument.Count > 0)
            {
                var templateUpdate = Builders<BsonDocument>.Update.Set(CONSTANTS.AccessRight, CONSTANTS.True_Value);
                var updateResult = collection.UpdateOne(filter, templateUpdate);
            }
            return CONSTANTS.success;
        }

        /// <summary>
        /// Get Market place provisioned Templates for user
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="category"></param>
        /// <returns></returns>
        public List<JObject> GetMarketPlaceUserTemplate(string userId, string category)
        {
            var userInfo = string.Empty;
            var data = new List<JObject>();
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.RegisterMarketPlaceUser);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.UserId, userId) & Builders<BsonDocument>.Filter.Eq(CONSTANTS.Category, category);
            var projection = Builders<BsonDocument>.Projection.Exclude(CONSTANTS.Id);
            var resultdocument = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            if (resultdocument.Count > 0)
            {
                for (int i = 0; i < resultdocument.Count; i++)
                {
                    data.Add(JObject.Parse(resultdocument[i].ToString()));
                }

            }
            return data;
        }

        /// <summary>
        /// Configure Certification Flag
        /// </summary>
        /// <param name="flag">flag</param>
        /// <returns></returns>
        public string ConfigureCertificationFlag(bool flag)
        {
            ConfigureCertificationFlag publicTemplate = new ConfigureCertificationFlag
            {
                _id = Guid.NewGuid().ToString(),
                flag = flag
            };
            var certificateFlag = _database.GetCollection<BsonDocument>(CONSTANTS.ConfigureCertificationFlag);
            var certificateFlagResult = certificateFlag.Find(new BsonDocument()).
               Project<BsonDocument>(Builders<BsonDocument>.
               Projection.Exclude(CONSTANTS.Id)).ToList();
            if (certificateFlagResult.Count > 0)
            {
                var templateUpdate = Builders<BsonDocument>.Update.Set(CONSTANTS.flag, flag);
                var updateResult = certificateFlag.UpdateOne(new BsonDocument(), templateUpdate);
            }
            else
            {

                var jsonData = JsonConvert.SerializeObject(publicTemplate);
                var insertDocument = BsonSerializer.Deserialize<BsonDocument>(jsonData);
                certificateFlag.InsertOne(insertDocument);
            }
            return CONSTANTS.success;
        }

        /// <summary>
        /// Get Certification Flag Value
        /// </summary>
        /// <returns></returns>
        public bool GetCertificationFlag()
        {
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.ConfigureCertificationFlag);
            var resultdocument = collection.Find(new BsonDocument()).
               Project<BsonDocument>(Builders<BsonDocument>.
               Projection.Exclude(CONSTANTS.Id)).ToList();
            if (resultdocument.Count > 0)
            {
                var data = resultdocument[0][CONSTANTS.flag].ToBoolean();
                return data;
            }
            return false;
        }

        public void ReplaceTextInFile(string userId)
        {
            System.IO.StreamReader objReader;
            var certificateFilepath = Path.Combine(_hostingEnvironment.ContentRootPath + CONSTANTS.CertificateFilePath);//appSettings.Value.CertificateFilePath;//ConfigurationManager.AppSettings["CertificateFilePath"];
            objReader = new System.IO.StreamReader(certificateFilepath + "Certification.html");
            string content = objReader.ReadToEnd();
            objReader.Close();
            content = content.Replace("{{fullName}}", userId);
            content = content.Replace("{{sysDate}}", DateTime.Today.ToString("dd MMM, yyyy"));
            var filePath = certificateFilepath + "Certification_New.html";
            if (!Directory.Exists(certificateFilepath))
                Directory.CreateDirectory(certificateFilepath);
            StreamWriter writer = new StreamWriter(filePath);
            writer.Write(content);
            writer.Close();
        }

        /// <summary>
        /// Send Email feature
        /// </summary>
        /// <param name="recipient"></param>
        /// <param name="subject"></param>
        /// <param name="emailBody"></param>
        /// <returns></returns>
        public bool SendEmailForCertificate(string userId, string userEmailId)
        {
            //Fetching Settings from WEB.CONFIG file. 
            var certificateFilepath = Path.Combine(_hostingEnvironment.ContentRootPath + CONSTANTS.CertificateFilePath);//appSettings.Value.CertificateFilePath; // ConfigurationManager.AppSettings["CertificateFilePath"];
            string recipient = appSettings.Value.Recipient;//ConfigurationManager.AppSettings["recipient"].ToString();
            string emailSenderHost = appSettings.Value.SMTPServer;//ConfigurationManager.AppSettings["smtpserver"].ToString();
            int emailSenderPort = Convert.ToInt16(appSettings.Value.PortNumber);
            bool isMessageSent = false;
            //Intialise Parameters  
            SmtpClient client = new SmtpClient(emailSenderHost);
            client.Port = emailSenderPort;
            client.DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network;
            client.UseDefaultCredentials = false;
            var mail = new MailMessage();
            mail.From = new MailAddress(recipient);
            mail.To.Add(new MailAddress(userEmailId));
            mail.Subject = "Congratulations – myWizard ingrAIn";//appSettings.Value.CertificateEmailSub; //ConfigurationManager.AppSettings["CertificateEmailSub"].ToString();
            mail.Body = this.CertificateTemplate(userId);
            System.Net.Mail.Attachment attachment;
            attachment = new System.Net.Mail.Attachment(certificateFilepath + "Certification.pdf");
            attachment.Name = "Certification.pdf";
            mail.Attachments.Add(attachment);
            mail.IsBodyHtml = true;
            mail.DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure;
            client.Send(mail);
            mail.Attachments.Dispose();
            isMessageSent = true;
            return isMessageSent;
        }



        public string CertificateTemplate(string userId)
        {
            string body = string.Empty;
            using (StreamReader reader = new StreamReader(Path.Combine(_hostingEnvironment.ContentRootPath + CONSTANTS.CertificateTemplat)))
            {
                body = reader.ReadToEnd();
            }
            body = body.Replace("{EnterpriseId}", userId);
            return body;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public string EncryptMarketplaceFile(HttpContext file, string filePlace)
        {
            try
            {
                var fileCollection = file.Request.Form.Files;               
                if (!string.IsNullOrEmpty(filePlace) && fileCollection.Count>0)
                {
                    if (CommonUtility.ValidateFileUploaded(fileCollection))
                    {
                        throw new FormatException(Resource.IngrainResx.InValidFileName);
                    }
                    for (int i = 0; i < fileCollection.Count; i++)
                    {
                        var postedFile = fileCollection[i];
                        if (postedFile.Length <= 0)
                            return CONSTANTS.FileEmpty;
                        string filePath = System.IO.Path.Combine(appSettings.Value.UploadFilePath, appSettings.Value.MarketPlaceFiles, filePlace);
                        if(!Directory.Exists(filePath))
                        System.IO.Directory.CreateDirectory(filePath);
                        string newFilePath = Path.Combine(filePath, Path.GetFileName(postedFile.FileName)); 
                        if (File.Exists(newFilePath))
                        {

                            _encryptionDecryption.EncryptFile(postedFile, newFilePath);
                        }
                        else
                        {

                            _encryptionDecryption.EncryptFile(postedFile, newFilePath);
                        }
                    }
                    return CONSTANTS.Success;
                }
                else
                {
                    return "No file path specified or No File is attached.";
                }
            }

            catch (Exception ex)
            {
               LOGGING.LogManager.Logger.LogProcessInfo(typeof(LoginService), nameof(EncryptMarketplaceFile), "EncryptMarketplaceFile - ERROR END :" + ex.Message + ex.StackTrace, string.Empty, string.Empty, string.Empty, string.Empty);
                return ex.Message;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public dynamic DecryptMarketplaceFile(string filePlace)
        {
            try
            {
                if (!string.IsNullOrEmpty(filePlace))
                {
                    string filePath = System.IO.Path.Combine(appSettings.Value.UploadFilePath, appSettings.Value.MarketPlaceFiles, filePlace);
                    var data = _encryptionDecryption.DecryptFiletoStream(filePath, null);
                    return data;
                }
                else
                {
                    return "No file path specified.";
                }  
            }
            catch (Exception ex)
            {
               LOGGING.LogManager.Logger.LogProcessInfo(typeof(LoginService), nameof(EncryptMarketplaceFile), "EncryptMarketplaceFile - ERROR END :" + ex.Message + ex.StackTrace, string.Empty, string.Empty, string.Empty, string.Empty);
                return ex.Message;
            }
        }


        #endregion
    }
}
