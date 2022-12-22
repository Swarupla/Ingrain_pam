using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.WindowService.Models
{
    public class DP
    {
        public List<DataProvider> DataProvider { get; set; }
    }
    public class DataProvider
    {

        public string Name { get; set; }

        public string DataProviderTypeName { get; set; }

        public string ServiceUrl { get; set; }

        public string TicketStatusToFilter { get; set; }

        public string JsonRootNode { get; set; }

        public string TicketPullType { get; set; }

        public string FilterDateFormat { get; set; }

        public int BatchSize { get; set; }


        public int IncrementDateRangeBy { get; set; }

        public int IntialBatchSize { get; set; }

        public string Method { get; set; }

        public string MIMEMediaType { get; set; }

        public string Accept { get; set; }

        public DataFormatter DataFormatter { get; set; }

        public AuthProvider AuthProvider { get; set; }

        public string DefaultKeys { get; set; }

        public string DefaultValues { get; set; }

        public string InputRequestType { get; set; }

        public string InputRequestKeys { get; set; }

        public string InputRequestValues { get; set; }

        public string TicketType { get; set; }
    }
}
