using Accenture.MyWizard.Ingrain.WindowService.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Accenture.MyWizard.Ingrain.WindowService.Interfaces
{
    public interface ISaaSTicketPullService
    {
        List<ClientInfo> FetchProvisionedE2EID();

        bool CheckPAMHistoricalPullTracker(string ClientUId, string EndToEndUId, string entityType, string pullType);
        bool DeleteHistory(string clientUId, string e2eUId, string entityType);
        void InsertPAMHistoricalPullTracker(string clientUId, string EndToEndUId, string entityType, string pullType, DateTime startDate, DateTime endDate);
        List<PAMHistoricalPullTracker> FetchPAMHistoricalPullTracker(string clientUId, string EndToEndUId, string entityType, string pullType);
        void UpdatePAMHistoricalPullTracker(string ClientUId, string EndToEndUId, string entityType, string pullType, int flag, string statustext, int recordCount, DateTime? maxLastDFUpdatedDate);
        void UpdatePAMHistoricalPullTracker(string ClientUId, string EndToEndUId, string entityType, string pullType, int flag, string statustext, int recordCount, DateTime startDate, DateTime endDate, DateTime processingStartTime);
        void InsertPAMHistoricalPullFailedTracker(PAMHistoricalPullTracker tracker);
    }
}
