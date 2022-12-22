#region Copyright (c) 2019 Accenture . All rights reserved.

// <copyright company="Accenture">
// Copyright (c) 2019 Accenture.  All rights reserved.
// Reproduction or transmission in whole or in part, in any form or by any means, 
// electronic, mechanical or otherwise, is prohibited without the prior written 
// consent of the copyright owner.
// </copyright>

#endregion

#region Controller Information
/********************************************************************************************************\
Module Name     :   AssetUsageTrackingController
Project         :   Accenture.MyWizard.SelfServiceAI.AssetUsageTrackingController
Organisation    :   Accenture Technologies Ltd.
Created By      :   Thanyaasri Manickam
Created Date    :   20-Jan-2020
Revision History :
Revision No             Initial             Modified Date           Description
1.0                     An                  20-Jan-2020            
\********************************************************************************************************/
#endregion

namespace Accenture.MyWizard.SelfServiceAI.AssetUsageTrackingController
{
    #region Namespace References
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using LOGGING = Accenture.MyWizard.LOGGING;
    using MongoDB.Bson;
    using Accenture.MyWizard.Ingrain.WebService;
    using Accenture.MyWizard.Ingrain.DataModels.Models;
    using Microsoft.AspNetCore.Mvc;
    using Accenture.MyWizard.Ingrain.WebService.Controllers;
    using Microsoft.Extensions.Options;
    using Accenture.MyWizard.Shared.Helpers;
    using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.AspNetCore.Http;
    using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
    using Accenture.MyWizard.Ingrain.BusinessDomain.Common;
    #endregion

    public class AssetUsageTrackingController : MyWizardControllerBase
    {
        #region Members 
        public static IAssetUsageTrackingData _iassetUsageTrackingData { set; get; }
        private AssetUsageTrackingData _userTrackingData = null;
        // private string UserUniqueId = null;
        private Guid clientID = default(Guid);
        private string userId = null;
        private Guid dcID = default(Guid);
        private string dcName = null;
        private string features = null;
        private string subFeatures = null;
        private string ApplicationURL = null;
        private string IpAddress = null;
        private string BrowserName = null;
        private string ScreenResolution = null;
        private string UserUniqueId = null;
        private string Environment = null;
        private string End2EndId = null;
        #endregion

        #region Constructors
        public AssetUsageTrackingController(IServiceProvider serviceProvider)
        {
            _iassetUsageTrackingData = serviceProvider.GetService<IAssetUsageTrackingData>();
        }
        #endregion

        #region Methods 

        [HttpPost]
        [Route("api/UsageTracking")]
        public IActionResult UsageTracking([FromBody] dynamic requestBody, [FromHeader] string authorization)
        {            
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(AssetUsageTrackingController),nameof(UsageTracking), "Start", string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                string columnsData = Convert.ToString(requestBody);
                if (!string.IsNullOrEmpty(columnsData))
                {
                    dynamic dynamicColumns = Newtonsoft.Json.JsonConvert.DeserializeObject(columnsData);
                    var columns = JObject.Parse(columnsData);
                    //if (!(string.IsNullOrEmpty(columns["clientID"].ToString())) && !(string.IsNullOrEmpty(columns["dcID"].ToString())) && !(string.IsNullOrEmpty(columns["userId"].ToString())) &&
                    //      columns["clientID"].ToString() != "null" && columns["dcID"].ToString() != "null" && columns["userId"].ToString() != "null" &&
                    //      columns["clientID"].ToString() != "undefined" && columns["dcID"].ToString() != "undefined" && columns["userId"].ToString() != "undefined")
                    //{
                    //    return GetFaultResponse(string.Format(CONSTANTS.InValidData, "Environment"));
                    //}

                    if (!CommonUtility.IsDataValid(Convert.ToString(columns["Environment"])))
                    {
                        return GetFaultResponse(string.Format(CONSTANTS.InValidData, "Environment"));
                    }
                    
                    //Validating logged in User. PAM authoize is forms authentication
                    bool isPAMEnvironment = false;
                    string loggedInUser = string.Empty;
                    if (Convert.ToString(columns["Environment"]) == "PAM")
                    {
                        isPAMEnvironment = Convert.ToString(columns["Environment"]) == CONSTANTS.PAMEnvironment;
                        loggedInUser = !isPAMEnvironment ? CommonUtility.GetLoggedInUserFromToken(authorization) : string.Empty;
                        if (!isPAMEnvironment && string.IsNullOrEmpty(loggedInUser))
                        {
                            return GetFaultResponse(Resource.IngrainResx.InValidToken);
                        }
                    }

                    if (!(string.IsNullOrEmpty(Convert.ToString(columns["clientID"]))) && !(string.IsNullOrEmpty(Convert.ToString(columns["dcID"]))) && !(string.IsNullOrEmpty(Convert.ToString(columns["userId"]))) &&
                          Convert.ToString(columns["clientID"]) != "null" && Convert.ToString(columns["dcID"]) != "null" && Convert.ToString(columns["userId"]) != "null" &&
                          Convert.ToString(columns["clientID"]) != "undefined" && Convert.ToString(columns["dcID"]) != "undefined" && Convert.ToString(columns["userId"]) != "undefined")
                    {
                        if (!CommonUtility.GetValidUser(Convert.ToString(columns["userId"])))
                        {
                            return GetFaultResponse(Resource.IngrainResx.InValidUser);
                        }
                        if (Convert.ToString(columns["Environment"]) == "PAM")
                        {
                            if (!isPAMEnvironment && loggedInUser != Convert.ToString(columns["userId"]))
                            {
                                return GetFaultResponse(Resource.IngrainResx.InValidLoggedInUser);
                            }
                        }
                        if (!CommonUtility.IsBrowserValid(Convert.ToString(columns["Browser"])))
                        {
                            return GetFaultResponse(Resource.IngrainResx.InValidBrowser);
                        }

                        if (!CommonUtility.IsValidGuid(Convert.ToString(columns["clientID"])))
                        {
                            return GetFaultResponse(string.Format(CONSTANTS.InValidGUID, "clientID"));
                        }
                        if (!CommonUtility.IsValidGuid(Convert.ToString(columns["dcID"])))
                        {
                            return GetFaultResponse(string.Format(CONSTANTS.InValidGUID, "dcID"));
                        }

                        if (!CommonUtility.IsDataValid(Convert.ToString(columns["dcName"])))
                        {
                            return GetFaultResponse(string.Format(CONSTANTS.InValidData, "dcName"));
                        }
                        if (!CommonUtility.IsDataValid(Convert.ToString(columns["features"])))
                        {
                            return GetFaultResponse(string.Format(CONSTANTS.InValidData, "features"));
                        }
                        if (!CommonUtility.IsDataValid(Convert.ToString(columns["subFeatures"])))
                        {
                            return GetFaultResponse(string.Format(CONSTANTS.InValidData, "subFeatures"));
                        }
                        if (!CommonUtility.IsDataValid(Convert.ToString(columns["ApplicationURL"])))
                        {
                            return GetFaultResponse(string.Format(CONSTANTS.InValidData, "ApplicationURL"));
                        }
                        if (!CommonUtility.IsDataValid(Convert.ToString(columns["ScreenResolution"])))
                        {
                            return GetFaultResponse(string.Format(CONSTANTS.InValidData, "ScreenResolution"));
                        }
                        // UserUniqueId = columns["UserUniqueId"].ToString(); 
                        clientID = new Guid(columns["clientID"].ToString());
                        userId = columns["userId"].ToString();
                        dcID = new Guid(columns["dcID"].ToString());
                        dcName = columns["dcName"].ToString();
                        features = columns["features"].ToString();
                        subFeatures = columns["subFeatures"].ToString();
                        ApplicationURL = columns["ApplicationURL"].ToString();
                        IpAddress = "";
                        BrowserName = columns["Browser"].ToString();
                        ScreenResolution = columns["ScreenResolution"].ToString();
                        if (columns.ContainsKey("UserUniqueId"))
                        {
                            if (!CommonUtility.IsValidGuid(Convert.ToString(columns["UserUniqueId"])))
                            {
                                return GetFaultResponse(string.Format(CONSTANTS.InValidGUID, "UserUniqueId"));
                            }
                            UserUniqueId = columns["UserUniqueId"].ToString();
                        }
                        if (columns.ContainsKey("Environment"))
                        {
                            if (!CommonUtility.IsDataValid(Convert.ToString(columns["Environment"])))
                            {
                                return GetFaultResponse(string.Format(CONSTANTS.InValidData, "Environment"));
                            }
                            Environment = columns["Environment"].ToString();
                        }
                        if (columns.ContainsKey("End2EndId"))
                        {
                            if (!CommonUtility.IsValidGuid(Convert.ToString(columns["End2EndId"])))
                            {
                                return GetFaultResponse(string.Format(CONSTANTS.InValidGUID, "End2EndId"));
                            }
                            End2EndId = columns["End2EndId"].ToString();
                        }

                        _userTrackingData = _iassetUsageTrackingData.GetUserTrackingDetails(clientID, userId, dcID, dcName, features, subFeatures, ApplicationURL, UserUniqueId, Environment, End2EndId, IpAddress, BrowserName, ScreenResolution);                        
                        LOGGING.LogManager.Logger.LogProcessInfo(typeof(AssetUsageTrackingController),nameof(UsageTracking), "END", string.Empty, string.Empty, Convert.ToString(clientID), Convert.ToString(dcID));
                        
                        return Ok(_userTrackingData);
                    }
                    else
                    {
                        return Ok("ClientID, DCID and User Id Values are null");
                    }
                }
                return Ok(_userTrackingData);
            }
            catch (Exception ex)
            {                
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AssetUsageTrackingController),nameof(UsageTracking), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);                
                return GetFaultResponse(ex.Message);
            }
        }


        [HttpGet]
        [Route("api/AssetUsageDashBoard")]

        public IActionResult AssetUsageDashBoard(string fromDate, string todate)
        {
            try
            {
                var _userTrackingData = _iassetUsageTrackingData.AssetUsageDashBoard(fromDate, todate);
                return Ok(_userTrackingData);
            }
            catch (Exception ex)
            {                
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AssetUsageTrackingController),nameof(AssetUsageDashBoard), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }

        }


        [HttpGet]
        [Route("api/CustomModelActivity")]

        public IActionResult CustomModelActivity(string fromDate, string todate)
        {
            try
            {
                var _userTrackingData = _iassetUsageTrackingData.CustomModelActivity(fromDate, todate);
                return Ok(_userTrackingData);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(AssetUsageTrackingController),nameof(AssetUsageDashBoard), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }

        }
        #endregion
    }
}
