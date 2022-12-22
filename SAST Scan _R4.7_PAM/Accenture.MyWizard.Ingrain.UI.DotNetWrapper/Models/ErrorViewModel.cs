using System;

namespace Accenture.MyWizard.Ingrain.UI.DotNetWrapper.Models
{
    public class ErrorViewModel
    {
        public string RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}