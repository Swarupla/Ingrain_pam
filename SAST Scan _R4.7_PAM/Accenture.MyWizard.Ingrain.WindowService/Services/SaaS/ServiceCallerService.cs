using Accenture.MyWizard.Ingrain.WindowService.Interfaces;
using MongoDB.Bson.IO;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Accenture.MyWizard.Ingrain.WindowService.Models.SaaS;
using Accenture.MyWizard.Ingrain.WindowService.Models;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Accenture.MyWizard.Ingrain.WindowService.Services
{
   public class ServiceCallerService : IServiceCaller
    {
        public async Task<XDocument> GetDocUsingService(ServiceCallerRequest serviceCallerRequest, List<string> faults)
        {
           // Log.Information("Inside ServiceCaller.cs -> Method Name: GetDocUsingService");
            var xDoc = new XDocument();
            faults = new List<string>();
            try
            {
              //  Log.Information("Before calling ReadDataFromService");
                var readTask = await ReadDataFromService(serviceCallerRequest).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(readTask) && readTask != "[]")
                {
                    var str = "{ \"" + serviceCallerRequest.JsonRootNode + "\" : " + readTask.Trim(new char[] { '\"' }) + " } ";

                    if (String.IsNullOrEmpty(serviceCallerRequest.JsonRootNode))
                    {
                        throw new Exception("JsonRootnode is null");
                    }
                    else
                    {
                      
                        xDoc = Newtonsoft.Json.JsonConvert.DeserializeXNode(str, serviceCallerRequest.JsonRootNode);
                    }
                }
                else
                {
                    throw new Exception(string.Format("Respone is null from the service URl : {0}", serviceCallerRequest.ServiceUrl));
                }
            }
            catch (Exception ex)
            {
                faults.Add("Message : " + ex.Message + ", Stacktrace : " + (string.IsNullOrEmpty(ex.StackTrace) ? "" : ex.StackTrace));
            }
            //Log.Information("End of ServiceCaller.cs -> Method Name: GetDocUsingService");

            return xDoc;
        }

        private async Task<String> ReadDataFromService(ServiceCallerRequest serviceCallerRequest)
        {
          //  Log.Information("Inside ServiceCaller.cs -> Method Name : ReadDataFromService");
            var content = String.Empty;
            try
            {
                content = serviceCallerRequest.Content;
                serviceCallerRequest.Content = content;
                //Log.Information("Before calling CallExternalService");
                var response = await CallExternalService(serviceCallerRequest).ConfigureAwait(false);
                //Log.Information("After calling CallExternalService" + response.StatusCode);
                if (response != null)
                {
                    switch (response.StatusCode)
                    {
                        case HttpStatusCode.OK:
                        case HttpStatusCode.Created:
                        case HttpStatusCode.Accepted:
                            content = await response.Content.ReadAsStringAsync();
                            break;
                        case HttpStatusCode.Moved:
                        case HttpStatusCode.InternalServerError:
                        case HttpStatusCode.BadGateway:
                        case HttpStatusCode.ServiceUnavailable:
                        case HttpStatusCode.GatewayTimeout:
                        case HttpStatusCode.BadRequest:
                        case HttpStatusCode.Unauthorized:
                        case HttpStatusCode.Forbidden:
                        case HttpStatusCode.NotFound:
                        case HttpStatusCode.MethodNotAllowed:
                        case HttpStatusCode.NotAcceptable:
                        case HttpStatusCode.RequestTimeout:
                        case HttpStatusCode.NotImplemented:
                        case HttpStatusCode.UnsupportedMediaType:
                        case HttpStatusCode.ExpectationFailed:
                        default:
                            break;
                    }
                }
                else
                {
                    throw new Exception(string.Format("Respone is null from the service URl : {0}", serviceCallerRequest.ServiceUrl));
                }
            }
            catch (Exception ex)
            {
              //  Log.Information("Exception of ServiceCaller.cs -> Method Name : ReadDataFromService" + ex.Message);
                throw ex;
            }
            //Log.Information("End of ServiceCaller.cs -> Method Name : ReadDataFromService" + content);

            return content;
        }

        private async Task<HttpResponseMessage> CallExternalService(ServiceCallerRequest serviceCallerRequest)
        {
            var response = new HttpResponseMessage();
            var serviceResourceURL = serviceCallerRequest.ServiceUrl;
            try
            {
                var client = await GetHttpClient(serviceCallerRequest).ConfigureAwait(false);
                var content = serviceCallerRequest.Content;
                if (!String.IsNullOrWhiteSpace(serviceCallerRequest.Accept))
                {
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(serviceCallerRequest.Accept));
                }
                var httpContent = new StringContent(content ?? String.Empty);
                httpContent.Headers.ContentType = new MediaTypeHeaderValue(serviceCallerRequest.MIMEMediaType);
                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                //ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                switch (serviceCallerRequest.HttpVerbName)
                {
                    case "POST":
                        response = await client.PostAsync(serviceResourceURL, httpContent).ConfigureAwait(false);
                        break;
                    case "PUT":
                        response = await client.PutAsync(serviceResourceURL, httpContent).ConfigureAwait(false);
                        break;
                    case "GET":
                        response = await client.GetAsync(serviceResourceURL).ConfigureAwait(false);
                        break;
                    case "DELETE":
                        response = await client.DeleteAsync(serviceResourceURL).ConfigureAwait(false);
                        break;
                    default:
                        throw new Exception(String.Format(@"Http Verb Name {0} is not supported at this moment.", serviceCallerRequest.HttpVerbName));
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = HttpStatusCode.ExpectationFailed;
                throw ex;
            }
            return response;
        }

        private async Task<HttpClient> GetHttpClient(ServiceCallerRequest serviceCallerRequest)
        {
            Uri federationUrl = null;
            tempSSL temp = new tempSSL();
            temp.IsTLS12Enabled = serviceCallerRequest.AuthProvider.IsTLS12Enabled;
            temp.Subject = serviceCallerRequest.AuthProvider.Subject;
            temp.Issuer = serviceCallerRequest.AuthProvider.Issuer;
            temp.Thumbprint = serviceCallerRequest.AuthProvider.Thumbprint;
            temp.CertType = serviceCallerRequest.AuthProvider.CertType;

            SSLHelper sslhelper = new SSLHelper(temp.Subject, temp.Issuer, temp.Thumbprint, temp.IsTLS12Enabled, temp.CertType);

            var httpClient = new HttpClient(sslhelper.httpHandler);
          
            if (serviceCallerRequest.AuthProvider.Name == "Phoenix")
            {
                httpClient.DefaultRequestHeaders.Add("AppServiceUId", serviceCallerRequest.AuthProvider.AppServiceUId);
                httpClient.DefaultRequestHeaders.Add("UserEmailId", serviceCallerRequest.AuthProvider.ClientId);
            }
            if (serviceCallerRequest.AuthProvider != null && !string.IsNullOrEmpty(serviceCallerRequest.AuthProvider.Token))
            {
                if (serviceCallerRequest.AuthProvider.AuthProviderType == AuthProviderType.PAM)
                    httpClient.DefaultRequestHeaders.Add("requestverification", serviceCallerRequest.AuthProvider.Token);
                else
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", serviceCallerRequest.AuthProvider.Token);
            }
            else
            {
                var token = String.Empty;
                try
                {
                    if (serviceCallerRequest.AuthProvider != null)
                    {
                        switch (serviceCallerRequest.AuthProvider.AuthProviderType)
                        {
                            case AuthProviderType.oAuth1:
                                federationUrl = new Uri(serviceCallerRequest.AuthProvider.FederationUrl);
                                token = await GetTokenAsync(federationUrl, serviceCallerRequest.AuthProvider.ClientId, serviceCallerRequest.AuthProvider.Secret,
                                    serviceCallerRequest.AuthProvider.Scope).ConfigureAwait(false);
                                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                                break;
                            case AuthProviderType.oAuth2:
                                federationUrl = new Uri(serviceCallerRequest.AuthProvider.FederationUrl);
                                token = await GetTokenAsync(federationUrl, serviceCallerRequest.AuthProvider.ClientId, serviceCallerRequest.AuthProvider.Secret,
                                    serviceCallerRequest.AuthProvider.Scope, "OAuthV2").ConfigureAwait(false);
                                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                                break;
                            case AuthProviderType.Phoenix:
                                token = await GetPhoenixTokenAsync(serviceCallerRequest.AuthProvider.FederationUrl, serviceCallerRequest.AuthProvider.ClientId, serviceCallerRequest.AuthProvider.Secret).ConfigureAwait(false);
                                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                                break;
                            case AuthProviderType.PAM:
                                //token = await GetPAMTokenAsyncoAuth2(serviceCallerRequest.AuthProvider.FederationUrl, serviceCallerRequest.AuthProvider.ClientId, serviceCallerRequest.AuthProvider.Secret, temp, serviceCallerRequest.AuthProvider.Scope, serviceCallerRequest.AuthProvider.GrantType).ConfigureAwait(false);
                                //httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                                token = await GetPAMTokenAsync(serviceCallerRequest.AuthProvider.FederationUrl, serviceCallerRequest.AuthProvider.UserName, serviceCallerRequest.AuthProvider.Password, temp).ConfigureAwait(false);
                                httpClient.DefaultRequestHeaders.Add("apiToken", token);
                                break;
                            case AuthProviderType.VDSPhoenix:
                                token = await GetVDSPhoenixTokenAsync(serviceCallerRequest.AuthProvider.FederationUrl, serviceCallerRequest.AuthProvider.ClientId, serviceCallerRequest.AuthProvider.Secret, serviceCallerRequest.AuthProvider.Resource, "OAuthV2").ConfigureAwait(false);
                                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                                break;
                            default:
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            return httpClient;
        }

        public class tempSSL
        {

            public string Subject { get; set; }
            public string Issuer { get; set; }
            public string Thumbprint { get; set; }
            public string IsTLS12Enabled { get; set; }
            public string CertType { get; set; }
        }

        public class SSLHelper
        {
            public static string SSLSubject { get; set; }
            public static string SSLIssuer { get; set; }
            public static string SSLThumbprint { get; set; }

            public HttpClientHandler httpHandler = new HttpClientHandler();

            private List<CertificateAttributes> __knownSelfSignedCertificates { get; set; }

            /// <summary>
            /// 
            /// </summary>
            public SSLHelper(string Subject, string Issuer, string Thumbprint, string IsTLS12Enabled, string CertType)
            {
                SSLSubject = Subject;
                SSLIssuer = Issuer;
                SSLThumbprint = Thumbprint;

                __knownSelfSignedCertificates = new List<CertificateAttributes> {
           new CertificateAttributes(  // can paste values from "view cert" dialog
            Decrypt(Convert.ToString(SSLSubject)),
            Decrypt(Convert.ToString(SSLIssuer)),
            SSLThumbprint)
    };
                string isTLSEnabled = Convert.ToString(IsTLS12Enabled);
                if (!string.IsNullOrEmpty(isTLSEnabled) && isTLSEnabled.ToLower() == "tlsenabled")
                {
                    System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    //ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                }
                string isSelfSigned = Convert.ToString(CertType);
                if (!string.IsNullOrEmpty(isSelfSigned) && isSelfSigned.ToLower() == "selfsigned")
                {
                    // Hook in validation of SSL server certificates.  
                    //ServicePointManager.ServerCertificateValidationCallback += ValidateServerCertficate;
                    httpHandler.ServerCertificateCustomValidationCallback += ValidateServerCertficate;
                }
            }



            private class CertificateAttributes
            {
                public string Subject { get; private set; }
                public string Issuer { get; private set; }
                public string Thumbprint { get; private set; }


                public CertificateAttributes(string subject, string issuer, string thumbprint)
                {
                    Subject = subject;
                    Issuer = issuer;
                    Thumbprint = thumbprint;
                    //.Trim(
                    //    new char[] { '\u200e', '\u200f' } // strip any lrt and rlt markers from copy/paste
                    //    );
                }

                public bool IsMatch(X509Certificate cert)
                {
                    bool subjectMatches = Subject.ToLower().Replace(" ", "").Equals(cert.Subject.ToLower().Replace(" ", ""), StringComparison.InvariantCulture);
                    bool issuerMatches = Issuer.ToLower().Replace(" ", "").Equals(cert.Issuer.ToLower().Replace(" ", ""), StringComparison.InvariantCulture);
                    //bool thumbprintMatches = Thumbprint.ToLower() == String.Join(" ", cert.GetCertHash().Select(h => h.ToString("x2")));
                    return subjectMatches && issuerMatches; // && thumbprintMatches;
                }
            }




            /// <summary>
            /// Validates the SSL server certificate.
            /// </summary>
            /// <param name="sender">An object that contains state information for this
            /// validation.</param>
            /// <param name="cert">The certificate used to authenticate the remote party.</param>
            /// <param name="chain">The chain of certificate authorities associated with the
            /// remote certificate.</param>
            /// <param name="sslPolicyErrors">One or more errors associated with the remote
            /// certificate.</param>
            /// <returns>Returns a boolean value that determines whether the specified
            /// certificate is accepted for authentication; true to accept or false to
            /// reject.</returns>
            private bool ValidateServerCertficate(
                object sender,
                X509Certificate cert,
                X509Chain chain,
                SslPolicyErrors sslPolicyErrors)
            {
                if (sslPolicyErrors == SslPolicyErrors.None)
                    return true;   // Good certificate.
                return __knownSelfSignedCertificates.Any(c => c.IsMatch(cert));
            }
            private const string IOMEncryptionKey = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            /// <summary>
            /// 
            /// </summary>
            /// <param name="inputText"></param>
            /// <returns></returns>
            public static string Decrypt(string inputText)
            {
                try
                {
                    if (string.IsNullOrEmpty(inputText))
                    {
                        return inputText;
                    }
                    inputText = inputText.Replace(" ", "+").Replace("\"", "");
                    byte[] stringBytes = Convert.FromBase64String(inputText);
                    using (Aes IOMEncryptor = Aes.Create())
                    {
                        Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(IOMEncryptionKey, new byte[] {
            0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76
        });
                        IOMEncryptor.Key = pdb.GetBytes(32);
                        IOMEncryptor.IV = pdb.GetBytes(16);
                        using (MemoryStream ms = new MemoryStream())
                        {
                            using (CryptoStream cs = new CryptoStream(ms, IOMEncryptor.CreateDecryptor(), CryptoStreamMode.Write))
                            {
                                cs.Write(stringBytes, 0, stringBytes.Length);
                                cs.Close();
                            }
                            inputText = Encoding.Unicode.GetString(ms.ToArray());
                        }
                    }
                }

                catch (FormatException ex)
                {
                    inputText = "";
                    throw ex;
                }
                return inputText;
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="inputString"></param>
            /// <returns></returns>
            //public static string Encrypt(string inputString)
            //{
            //    try
            //    {
            //        byte[] inputBytes = Encoding.Unicode.GetBytes(inputString.Replace("\"", ""));
            //        using (Aes IOMEncryptor = Aes.Create())
            //        {
            //            Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(IOMEncryptionKey, new byte[] {
            //    0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76
            //});
            //            IOMEncryptor.Key = pdb.GetBytes(32);
            //            IOMEncryptor.IV = pdb.GetBytes(16);
            //            using (MemoryStream mstream = new MemoryStream())
            //            {
            //                using (CryptoStream cstream = new CryptoStream(mstream, IOMEncryptor.CreateEncryptor(), CryptoStreamMode.Write))
            //                {
            //                    cstream.Write(inputBytes, 0, inputBytes.Length);
            //                    cstream.Close();
            //                }
            //                inputString = Convert.ToBase64String(mstream.ToArray());
            //            }
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        inputString = "";
            //        throw ex;
            //    }
            //    return inputString;
            //}
        }

        private async Task<String> GetTokenAsync(Uri fedAuthURL, String userName, String password, String scope, String grantType = "")
        {
            var token = String.Empty;
            try
            {

                if (string.IsNullOrEmpty(token))
                {
                    using (var client = new HttpClient())
                    {
                        client.BaseAddress = fedAuthURL;
                        client.DefaultRequestHeaders.Accept.Clear();
                        var postData = new List<KeyValuePair<String, String>>();

                        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                        if (grantType == "OAuthV2")
                        {
                            postData.Add(new KeyValuePair<String, String>("grant_type", "client_credentials"));
                            postData.Add(new KeyValuePair<String, String>("client_id", userName));
                            postData.Add(new KeyValuePair<String, String>("client_secret", password));
                        }
                        else
                        {
                            postData.Add(new KeyValuePair<String, String>("grant_type", "password"));
                            postData.Add(new KeyValuePair<String, String>("username", userName));
                            postData.Add(new KeyValuePair<String, String>("password", password));
                        }

                        postData.Add(new KeyValuePair<String, String>("scope", scope));
                        var content = new FormUrlEncodedContent(postData);
                        var response = await client.PostAsync("", content).ConfigureAwait(false);

                        if (response.StatusCode != HttpStatusCode.OK)
                        {
                            throw new Exception(String.Format("Unable to process your request for OAUTH token. Please check your credentials and try again. Status Code: {0}", response.StatusCode));
                        }

                        var result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        var tokenObj = Newtonsoft.Json.JsonConvert.DeserializeObject(result) as dynamic;
                        token = Convert.ToString(tokenObj.access_token);
                    }
                }
                else
                {
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return token;
        }
        private static async Task<string> GetPhoenixTokenAsync(string federaionUrl, string userName, string password)
        {
            string token = string.Empty;

            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.BaseAddress = new Uri(federaionUrl);
                    httpClient.DefaultRequestHeaders.Accept.Clear();
                    httpClient.DefaultRequestHeaders.Add("UserName", userName);
                    httpClient.DefaultRequestHeaders.Add("Password", password);

                    HttpContent content = new StringContent(string.Empty);
                    System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                    //ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                    HttpResponseMessage httpResponse = await httpClient.PostAsync("", content).ConfigureAwait(false);

                    if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        throw new Exception(string.Format("Unable to process your request for generating token. Please check your credentials and try again. Status Code: {0}", httpResponse.StatusCode));
                    }

                    var result = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var tokenObj = Newtonsoft.Json.JsonConvert.DeserializeObject(result) as dynamic;
                    //var tokenObj = JsonConvert.DeserializeObject<OAuthClient.TokenInfo>(result);
                    token = Convert.ToString(tokenObj.access_token);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }
            return token;
        }
        private static async Task<string> GetPAMTokenAsync(string federaionUrl, string userName, string password, tempSSL authProvider)
        {
            string token = string.Empty;

            try
            {
                //Log.Information("Get PAM Token Start- GetPAMTokenAsync");
                ServiceCallerRequest serviceCallerRequest = new ServiceCallerRequest();
                //Log.Information("Config" + authProvider.Subject + "," + authProvider.Issuer + "," + authProvider.Thumbprint + "," + authProvider.IsTLS12Enabled + "," + authProvider.CertType);

                SSLHelper sslhelper = new SSLHelper(authProvider.Subject, authProvider.Issuer, authProvider.Thumbprint, authProvider.IsTLS12Enabled, authProvider.CertType);

                using (var httpClient = new HttpClient(sslhelper.httpHandler))
                {
                    httpClient.BaseAddress = new Uri(federaionUrl);
                    httpClient.DefaultRequestHeaders.Accept.Clear();
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    JObject jsonObject = new JObject(
                                                 new JProperty("username", userName),
                                                 new JProperty("password", password)
                                             );

                    HttpContent content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(jsonObject), Encoding.UTF8, "application/json");
                    System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                    //ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                    HttpResponseMessage httpResponse = await httpClient.PostAsync("", content).ConfigureAwait(false);

                    if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        throw new Exception(string.Format("Unable to process your request for generating token. Please check your credentials and try again. Status Code: {0}", httpResponse.StatusCode));
                    }

                    var result = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var tokenObj = Newtonsoft.Json.JsonConvert.DeserializeObject(result) as dynamic;
                    //var tokenObj = JsonConvert.DeserializeObject<OAuthClient.TokenInfo>(result);
                    token = Convert.ToString(tokenObj.token);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }
            return token;
        }
        private static async Task<string> GetPAMTokenAsyncoAuth2(string federaionUrl, string ClientId, string Secret, tempSSL authProvider, string scope, string grantType)
        {
            string token = string.Empty;
            var postData = new List<KeyValuePair<String, String>>();

            try
            {
               // Log.Error("Get PAM Token Start- GetPAMTokenAsync");
                ServiceCallerRequest serviceCallerRequest = new ServiceCallerRequest();
                //Log.Error("Config" + authProvider.Subject + "," + authProvider.Issuer + "," + authProvider.Thumbprint + "," + authProvider.IsTLS12Enabled + "," + authProvider.CertType);

                SSLHelper sslhelper = new SSLHelper(authProvider.Subject, authProvider.Issuer, authProvider.Thumbprint, authProvider.IsTLS12Enabled, authProvider.CertType);

                using (var httpClient = new HttpClient(sslhelper.httpHandler))
                {
                    httpClient.BaseAddress = new Uri(federaionUrl);
                    httpClient.DefaultRequestHeaders.Accept.Clear();
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    //httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                    //    AuthenticationSchemes.Basic.ToString(),
                    //    Convert.ToBase64String(Encoding.ASCII.GetBytes($"{ClientId}:{Secret}"))
                    //    );

                    postData.Add(new KeyValuePair<String, String>("client_id", ClientId));
                    postData.Add(new KeyValuePair<String, String>("client_secret", Secret));


                    postData.Add(new KeyValuePair<String, String>("grant_type", grantType));
                    postData.Add(new KeyValuePair<String, String>("scope", scope));

                    var content = new FormUrlEncodedContent(postData);
                    //HttpContent content = new StringContent(JsonConvert.SerializeObject(jsonObject), Encoding.UTF8, "application/json");
                    System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                    //ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                    HttpResponseMessage httpResponse = await httpClient.PostAsync("", content).ConfigureAwait(false);

                    if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        throw new Exception(string.Format("Unable to process your request for generating token. Please check your credentials and try again. Status Code: {0}", httpResponse.StatusCode));
                    }

                    var result = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var tokenObj = Newtonsoft.Json.JsonConvert.DeserializeObject(result) as dynamic;
                    //var tokenObj = JsonConvert.DeserializeObject<OAuthClient.TokenInfo>(result);
                    token = Convert.ToString(tokenObj.access_token);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }
            return token;
        }

        private async Task<String> GetVDSPhoenixTokenAsync(String fedAuthURL, String userName, String password, String resource, String grantType = "")
        {
            var token = String.Empty;
            try
            {

                if (string.IsNullOrEmpty(token))
                {
                    using (var client = new HttpClient())
                    {
                        client.BaseAddress = new Uri(fedAuthURL);
                        client.DefaultRequestHeaders.Accept.Clear();
                        var postData = new List<KeyValuePair<String, String>>();

                        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                        if (grantType == "OAuthV2")
                        {
                            postData.Add(new KeyValuePair<String, String>("grant_type", "client_credentials"));
                            postData.Add(new KeyValuePair<String, String>("client_id", userName));
                            postData.Add(new KeyValuePair<String, String>("client_secret", password));
                        }
                        else
                        {
                            postData.Add(new KeyValuePair<String, String>("grant_type", "password"));
                            postData.Add(new KeyValuePair<String, String>("username", userName));
                            postData.Add(new KeyValuePair<String, String>("password", password));
                        }

                        postData.Add(new KeyValuePair<String, String>("resource", resource));
                        var content = new FormUrlEncodedContent(postData);
                        var response = await client.PostAsync("", content).ConfigureAwait(false);

                        if (response.StatusCode != HttpStatusCode.OK)
                        {
                            throw new Exception(String.Format("Unable to process your request for OAUTH token. Please check your credentials and try again. Status Code: {0}", response.StatusCode));
                        }

                        var result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        var tokenObj = Newtonsoft.Json.JsonConvert.DeserializeObject(result) as dynamic;
                        token = Convert.ToString(tokenObj.access_token);
                    }
                }
                else
                {
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return token;
        }

    }

}
