using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Configuration;

namespace Accenture.MyWizard.Ingrain.WebService.Models
{
  public class PhoenixToken
    {
        public  string GenerateToken()
        {

            string UserID = ConfigurationManager.AppSettings["username"].ToString();
            string Password = ConfigurationManager.AppSettings["password"].ToString();
            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(ConfigurationManager.AppSettings["tokenAPIUrl"].ToString());
                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Add("UserName", UserID);
                httpClient.DefaultRequestHeaders.Add("Password", Password);
                HttpContent content = new StringContent(string.Empty);
                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                //ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                var result = httpClient.PostAsync("", content).Result;

                if (result.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    throw new Exception(string.Format("Unable to process your request for generating token. Please check your credentials and try again. Status Code: {0}", result.StatusCode));
                }

                var result1 = result.Content.ReadAsStringAsync().Result;
                var tokenObj = JsonConvert.DeserializeObject(result1) as dynamic;

                var token = Convert.ToString(tokenObj.access_token);

                return token;

            }
        }
    }
}
