using Accenture.MyWizard.Ingrain.WindowService.Models;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.WindowService.Interfaces
{
    interface IDeployModelService
    {
        void ArchiveRecords(bool ManualTrigger, List<string> corrIds, int archive_days);      
        void PurgeRecords();      
    }
}
