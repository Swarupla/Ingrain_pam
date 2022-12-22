#region Copyright (c) 2019 Accenture . All rights reserved.

// <copyright company="Accenture">
// Copyright (c) 2019 Accenture.  All rights reserved.
// Reproduction or transmission in whole or in part, in any form or by any means, 
// electronic, mechanical or otherwise, is prohibited without the prior written 
// consent of the copyright owner.
// </copyright>

#endregion
using Accenture.MyWizard.Ingrain.DataModels.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces
{
    public interface IHyperTune
    {
        HyperParametersDTO GetHyperTuneData(string modelName,string correlationId);

        void PostHyperTuning(HyperTuningDTO data);

        HyperTuningTrainedModel GetHyperTunedTrainedModels(string correlationId, string hyperTuneId, string versionName);
        void InsertUsage(double CPUUsage, string CorrelationId, string HTId);

        void SaveHyperTuneVersion(dynamic data, string correlationId, string hyperTuneId);

        HyperTuningDTO GetHyperTuningVersions(string correlationId, string hyperTuneId);
    }
}
