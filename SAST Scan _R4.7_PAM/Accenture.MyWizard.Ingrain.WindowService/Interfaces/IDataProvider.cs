using Accenture.MyWizard.Ingrain.WindowService.Models.SaaS;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Accenture.MyWizard.Ingrain.WindowService.Interfaces
{
   public interface IDataProvider
    {
        Task<string> GetHistoricalAgileDetails(List<string> faults, AIAAMiddleLayerRequest request);
        Task<string> GetHistoricalIterationDetails(List<string> faults, AIAAMiddleLayerRequest request);
        Task<string> GetWorkItemsFromENS(List<string> faults, AIAAMiddleLayerRequest request);
        Task<string> GetIterationsFromENS(List<string> faults, AIAAMiddleLayerRequest request);
        Task<string> GetHistoricalTicketDetails(List<string> faults, AIAAMiddleLayerRequest request);
        Task<string> GetHistoricalTestResultDetails(List<string> faults, AIAAMiddleLayerRequest request);
        Task<string> GetHistoricalDeploymentDetails(List<string> faults, AIAAMiddleLayerRequest request);
    }
}
