using Accenture.MyWizard.Ingrain.DataModels.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces
{
    public interface ISPAVelocityService
    {
        VelocityTraining StartVelocityTraining(Velocity RequestPayload);
        VelocityPrediction GetPrediction(SPAInfo RequestPayload);

        public void AuditTrailLog(CallBackErrorLog auditTrailLog);

        bool IsAmbulanceLane(string useCaseId);

        long GetDatapoints(string useCaseId, string applicationId);
    }
}
