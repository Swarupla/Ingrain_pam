﻿using Accenture.MyWizard.Ingrain.DataModels.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces
{
    public interface IWorkerService
    {
        WorkerServiceInfo GetWorkerServiceStatus();
    }
}