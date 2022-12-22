using Accenture.MyWizard.Ingrain.WindowService.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Accenture.MyWizard.Ingrain.WindowService.Interfaces
{
    interface IPAMTicketPull
    {
       Task<Boolean> PAMTicketsPush(ClientInfo clientDetail, string entityType, string pullType, DateTime startDate, DateTime endDate);
    }
}
