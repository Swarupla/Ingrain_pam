using Accenture.MyWizard.Ingrain.DataModels.Models;

namespace Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces
{
    public interface IBusinessService
    {
        void InsertData(BusinessProblemDataDTO businessProblemDataDTO);
        string GetBusinessProblemData(string correlationId);
    }
}
