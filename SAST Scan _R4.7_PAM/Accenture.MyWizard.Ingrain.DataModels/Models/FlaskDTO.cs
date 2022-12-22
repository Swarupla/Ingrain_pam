using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.DataModels.Models
{
    public class FlaskDTO
    {
        public string CorrelationId { get; set; }
        public string UniqueId { get; set; }
        public string PageInfo { get; set; }
        public string UserId { get; set; }

    }
    public class FlaskForServiceDTO
    {
        public string correlationId { get; set; }
        public string requestId { get; set; }
        public string pageInfo { get; set; }
        public string userId { get; set; }
    }
}
