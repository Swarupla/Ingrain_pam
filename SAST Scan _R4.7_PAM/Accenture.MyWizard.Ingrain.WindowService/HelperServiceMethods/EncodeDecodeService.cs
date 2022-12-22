using System.Text;

namespace Accenture.MyWizard.Ingrain.WindowService.HelperServiceMethods
{
    class EncodeDecodeService
    {
        public string Base64Encode(string text)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(text);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        public string Base64Decode(string base64Data)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64Data);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }
    }
}
