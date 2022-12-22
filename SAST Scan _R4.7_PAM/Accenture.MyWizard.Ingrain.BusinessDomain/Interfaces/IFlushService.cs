using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces
{
    public interface IFlushService
    {
        string FlushModel(string CorrelationId, string flushFlag, string ServiceName = "");       
        string FlushAllModels(string clientuid, string deliveryconstructid, string userid, string flushFlag);

        string InstaMLDeleteModel(string CorrelationId);
        void DeleteBaseData(string correlationId);
        string DataSourceDelete(string correlationId, string ServiceName = "");
        string FlushModelSPP(string CorrelationId, string flushFlag);
        string userRole(string correlationId, string userid, string ServiceName = "");
        string Validate(string date);
        string DeleteDateRange(string StartDate, string EndDate);
        string DeleteCorrelationIds(string[] correlationId);
        string DeleteClientDCIds(string[] clientId, string[] dcId);
        string DeleteDateClientDCIds(string date, string[] clientId, string[] dcId);
    }
}
