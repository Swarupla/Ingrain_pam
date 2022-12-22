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
Module Name     :   MyWizardControllerBase
Project         :   Accenture.MyWizard.SelfServiceAI.MyWizardControllerBase
Organisation    :   Accenture Technologies Ltd.
Created By      :   Shreya
Created Date    :   24-Mar-2020
Revision History :
Revision No             Initial             Modified Date           Description
1.0                     An                  24-Mar-2020            
\********************************************************************************************************/
#endregion

namespace Accenture.MyWizard.Ingrain.WebService
{
    #region Namespace
    using System.Net;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Authorization;
    using System.Diagnostics.CodeAnalysis;
    using System;
    using Microsoft.Extensions.Primitives;
    using System.Linq;
    using Accenture.MyWizard.Ingrain.WebService.Controllers;
    #endregion Namespace

    /// <summary>
    /// Base Controller for Ingrain
    /// </summary>  
    [ServiceFilter(typeof(AuthorizePAM))]
    //[Authorize]
    [ApiController]
    public class MyWizardControllerBase : ControllerBase
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MyWizardControllerBase" /> class.
        /// </summary>
        [ExcludeFromCodeCoverage]
        public MyWizardControllerBase()
        {          
        }

        #endregion

        /// <summary>
        /// Gets the success response.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value">The value.</param>
        /// <returns>Success Response</returns>
        protected virtual IActionResult GetSuccessResponse<T>(T value)
        {
            return Ok(value);
        }

        /// <summary>
        /// Gets the Error response.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value">The value.</param>
        /// <returns>Error Response</returns>
        protected virtual IActionResult GetFaultResponse<T>(T value)
        {
            return StatusCode((int)HttpStatusCode.InternalServerError, value);
        }

        /// <summary>
        /// Gets the Success Response with Message.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value">The value.</param>
        /// <returns>Success Message</returns>
        protected virtual IActionResult GetSuccessWithMessageResponse<T>(T value)
        {
            return StatusCode((int)HttpStatusCode.OK, value);
        }

        /// <summary>
        /// Gets the Validation message response with Status Code 401
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns>Error Message</returns>
        protected virtual IActionResult GetFaultWithValidationMessageResponse<T>(T value)
        {
            return StatusCode((int)HttpStatusCode.Unauthorized, value);
        }

        /// <summary>
        /// Gets the app service for this request
        /// </summary>
        /// <returns></returns>
        protected virtual Guid GetAppService()
        {
            var appService = GetHeaderValue("AppServiceUId");

            if (String.IsNullOrEmpty(appService))
            {
                return default(Guid);
                //throw new ArgumentNullException("AppServiceUId is null");
            }
            else
            {
                return Guid.Parse(appService);
            }
        }

       
        /// <summary>
        /// Get the request header value
        /// </summary>
        /// <remarks>
        /// Get the request header value
        /// </remarks>
        /// <returns>Request header value</returns>
        protected String GetHeaderValue(String headerName)
        {
            var headerValue = String.Empty;
            StringValues headerValues;

            if (Request.Headers.TryGetValue(headerName, out headerValues))
            {
                headerValue = headerValues.FirstOrDefault();
            }

            return headerValue;
        }


        /// <summary>
        /// Enable or disable training 
        /// </summary>
        /// <returns></returns>
        protected virtual void IsTrainingEnabled(bool trainingFlag)
        {
            if (!trainingFlag)
                throw new InvalidOperationException("Model training is temporarily disabled in Ingrain");
        }

        /// <summary>
        /// Enable or disable prediction
        /// </summary>
        /// <returns></returns>
        protected virtual void IsPredictionEnabled(bool predictionFlag)
        {
            if (!predictionFlag)
                throw new InvalidOperationException("Prediction is temporarily disabled in Ingrain");
        }

    }
}