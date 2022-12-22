using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Accenture.MyWizard.Ingrain.WindowService.Interfaces
{
    interface IAssetUsageTrackingService
    {
        void AssetUsageTracking();

        Task PushAssetUsageTrackingToSaaS();
    }
}
