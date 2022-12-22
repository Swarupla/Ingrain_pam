#region Copyright (c) 2019 Accenture . All rights reserved.

// <copyright company="Accenture">
// Copyright (c) 2019 Accenture.  All rights reserved.
// Reproduction or transmission in whole or in part, in any form or by any means, 
// electronic, mechanical or otherwise, is prohibited without the prior written 
// consent of the copyright owner.
// </copyright>

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Accenture.MyWizard.Ingrain.DataModels;
using Accenture.MyWizard.Ingrain.DataModels.Models;
using MongoDB.Bson;
using Newtonsoft.Json.Linq;
using static Accenture.MyWizard.Ingrain.BusinessDomain.Services.ModelEngineeringService;

namespace Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces
{
    public interface IAssetUsageTrackingData
    {
        AssetUsageTrackingData GetUserTrackingDetails(Guid clientID, string userId, Guid dcID, string dcName, string features, string subFeatures, string ApplicationURL, string UserUniqueId, string Environment, string End2EndId, string IPAddress, string Browser, string ScreenResolution);
        FeatureAssetUsage AssetUsageDashBoard(string fromDate, string todate);

        IngrainCoreFeature CustomModelActivity(string fromDate, string todate);
    }
}
