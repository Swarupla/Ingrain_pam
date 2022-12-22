using Accenture.MyWizard.Ingrain.DataModels.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces
{
  public interface IScopeSelectorService
    {
        string UserStoryclientStructureGetOuth(string token, Guid ClientUID, string UserEmail);
        dynamic UserStoryclientStructureGetOuthNew(string token, Guid ClientUID, string UserEmail);
        dynamic getAppBuildInfo(string token, Guid clientUId, string UserEmail);
        string GetDeliveryStructuerFromPhoenix(string token, string Endtoend, string DeliveryConstructUId, string UserEmail);

        string fetchSecurityAcessAgilePhinix(string token, UserSecurityAcessAgile objpost);

        IngrainDeliveryConstruct GetDeliveryConstruct(string userId);        

        void PostDeliveryConstruct(IngrainDeliveryConstruct ingrainDeliveryConstruct);

        dynamic DeliveryConstructName(string token, Guid ClientUID, Guid DeliveryConstructUId, string UserEmail);

        dynamic ClientNameByClientUId(string token, Guid ClientUID, string DeliveryConstructUId, string UserId);

        string PAMDeliveryConstructName(string token, Guid DeliveryConstructUId);
        dynamic GetMetricData(string token, string ClientUID, string DeliveryConstructUId, string userId);
        string VDSSecurityTokenForPAD();

        dynamic appExecutionContext(string token, Guid ClientUID, Guid DeliveryConstructUId, string UserEmail);
        dynamic GetVDSDetail(string token, string ClientUID, string DeliveryConstructUID, string E2EUID, string UserId);

        dynamic ClientDetails(string token, Guid ClientUID, Guid DeliveryConstructUId, string UserEmail);

        /// <summary>
        /// Set User Cookie value
        /// </summary>
        /// <param name="value"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        string SetUserCookieDetails(bool value, string userId);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="token"></param>
        /// <param name="ClientUID"></param>
        /// <param name="DeliveryConstructUId"></param>
        /// <returns></returns>
        dynamic GetDynamicEntity(string token, string ClientUID, string DeliveryConstructUId, string UserEmail);
        dynamic getLanguage(string token, string ClientUID, string DeliveryConstructUId, string UserEmail);
        List<IngrainDeliveryConstruct> GetUserRole(string userEmail);
        void SaveENSNotification(ENSEntityNotification entityNotification);
        string GetENSNotification(string clientUId, string entityUId, string fromDate);
		dynamic ForcedSignin(string token, string userId);

        PAMClientScope ClientDetails();
        dynamic GetAccountClientDeliveryConstructsSearch(string token, string ClientUId, string DeliveryConstructUId, string Email, string SearchStr);

        string GetDecimalPointPlacesValue(string token, string ClientUId, string DeliveryConstructUId, string UserEmail);

        Task<dynamic> GetScopeSelectorData(string clientUId,string DeliveryConstructUID, string userEmail);
    }
}
