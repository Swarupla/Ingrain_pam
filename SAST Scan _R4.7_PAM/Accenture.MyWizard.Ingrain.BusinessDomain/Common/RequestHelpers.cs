using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;

namespace Accenture.MyWizard.Ingrain.BusinessDomain.Common
{
    public static class RequestHelpers
    {
        public static string GetBoundary(string contentType)
        {
            var elements = contentType.Split(' ');
            var element = elements.First(e => e.StartsWith("boundary="));
            var boundary = element.Substring("boundary=".Length);
            if (boundary.Length >= 2 && boundary[0] == '"' && boundary[^1] == '"')
                boundary = boundary[1..^1];
            return boundary;
        }
        public static bool HasFileContentDisposition(ContentDispositionHeaderValue contentDisposition)
        {
            // Content-Disposition: form-data; name="myfile1"; filename="Misc 002.jpg"
            return contentDisposition != null
                && contentDisposition.DispositionType.Equals("form-data")
                && (!string.IsNullOrEmpty(contentDisposition.FileName)
                    || !string.IsNullOrEmpty(contentDisposition.FileNameStar));
        }
        public static bool IsMultipartContentType(string contentType) => !string.IsNullOrEmpty(contentType) && contentType.IndexOf("multipart/", StringComparison.OrdinalIgnoreCase) >= 0;
    }

}
