
#region Copyright (c) 2019 Accenture . All rights reserved.

// <copyright company="Accenture">
// Copyright (c) 2019 Accenture.  All rights reserved.
// Reproduction or transmission in whole or in part, in any form or by any means, 
// electronic, mechanical or otherwise, is prohibited without the prior written 
// consent of the copyright owner.
// </copyright>

#endregion

#region ModelMonitoringController Information
/********************************************************************************************************\
Module Name     :   ModelEngineeringController
Project         :   Accenture.MyWizard.WebService.ModelMonitoringController
Organisation    :   Accenture Technologies Ltd.
Created By      :   Shreya
Created Date    :   02-Sep-2020
Revision History :
\********************************************************************************************************/
#endregion


namespace Accenture.MyWizard.Ingrain.WebService.Controllers
{
    using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
    using Accenture.MyWizard.Shared.Helpers;
    #region Namespace
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Options;
    using System;
    using Microsoft.Extensions.DependencyInjection;
    #endregion

    public class ModelMonitoringController : MyWizardControllerBase
    {
        public static IModelMonitorService _iModelMonitorService { set; get; }

        private readonly IOptions<IngrainAppSettings> _appSettings;
        public ModelMonitoringController(IServiceProvider serviceProvider, IOptions<IngrainAppSettings> settings)
        {
            _appSettings = settings;
            _iModelMonitorService = serviceProvider.GetService<IModelMonitorService>();
        }

        [HttpGet]
        [Route("api/ModelMetrics")]
        public IActionResult ModelMetrics(string clientid, string dcid, string correlationId)
        {
            try
            {
                    return Ok(_iModelMonitorService.ModelMetrics(clientid, dcid,correlationId));
            }
            catch (Exception ex)
            {
                return GetFaultResponse(ex.Message);
            }
        }

        [HttpGet]
        [Route("api/TrainedModelHistroy")]
        public IActionResult TrainedModelHistory(string clientid, string dcid, string correlationId)
        {
            try
            {
                return Ok(_iModelMonitorService.TrainedModelHistory(correlationId,clientid,dcid));
            }
            catch (Exception ex)
            {
                return GetFaultResponse(ex.Message);
            }
        }
    }
}
