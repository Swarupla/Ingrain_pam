using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.DataModels.AICore
{
    public class MethodReturn<T>
    {
        public T ReturnValue { get; set; }
        public string ReturnType { get; private set; }
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public string CorrelationId { get; set; }

        public MethodReturn()
        {
            ReturnType = typeof(T).FullName;
            Message = string.Empty;
        }
    }
}
