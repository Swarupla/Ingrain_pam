using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.DataModels.Models
{
    public class AuthToken
    {
        public string ExpirationDate { get; set; }

        public string Token { get; set; }

        public string TimeZone { get; set; }

        public string UserId { get; set; }
    }

    public class AuthorizeToken
    {
        public string UserName { get; set; }
    }
}