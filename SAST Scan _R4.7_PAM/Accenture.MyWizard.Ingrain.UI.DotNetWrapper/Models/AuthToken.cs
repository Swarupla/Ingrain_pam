using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.UI.DotNetWrapper.Models
{
    public class AuthToken
    {
        public string ExpirationDate { get; set; }

        public string Token { get; set; }
    }

    public class AuthorizeToken
    {
        public string UserName { get; set; }
    }
}
