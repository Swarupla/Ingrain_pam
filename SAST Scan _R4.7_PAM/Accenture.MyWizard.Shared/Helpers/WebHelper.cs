using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace Accenture.MyWizard.Shared.Helpers
{
    public class WebHelper
    {
        public HttpResponseMessage InvokeGETRequest(string token, string baseURI, string apiPath)
        {
            if (!string.IsNullOrWhiteSpace(token))
            {
                using (var client = new HttpClient())
                {
                    string uri = baseURI + apiPath;
                    client.Timeout = new TimeSpan(0, 30, 0);
                    //client.BaseAddress = new Uri(baseURI);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    if (!string.IsNullOrWhiteSpace(token))
                    {
                        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                    }
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                    return client.GetAsync(uri).Result;
                }
            }
            else
            {
                HttpClientHandler hnd = new HttpClientHandler();
                hnd.UseDefaultCredentials = true;
                using (var client = new HttpClient(hnd))
                {
                    string uri = baseURI + apiPath;
                    client.Timeout = new TimeSpan(0, 30, 0);
                    //client.BaseAddress = new Uri(baseURI);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                    return client.GetAsync(uri).Result;
                }
            }
        }

        public HttpResponseMessage InvokePOSTRequest(string token, string baseURI, string apiPath, StringContent content)
        {
            if (!string.IsNullOrWhiteSpace(token))
            {
                using (var client = new HttpClient())
                {
                    string uri = baseURI + apiPath;
                    client.Timeout = new TimeSpan(0, 30, 0);
                    //client.BaseAddress = new Uri(baseURI);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    if (!string.IsNullOrWhiteSpace(token))
                    {
                        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                    }
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                    return client.PostAsync(uri, content).Result;
                }
            }
            else
            {
                HttpClientHandler hnd = new HttpClientHandler();
                hnd.UseDefaultCredentials = true;
                using (var client = new HttpClient(hnd))
                {
                    string uri = baseURI + apiPath;
                    client.Timeout = new TimeSpan(0, 30, 0);
                    //client.BaseAddress = new Uri(baseURI);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                    return client.PostAsync(uri, content).Result;
                }
            }
        }

        public HttpResponseMessage InvokePOSTRequestWithFiles(string token, Uri baseURI, string apiPath, List<string> lstFilePath, string[] fileKeys,
            StringContent payloadContent)
        {
            if (!string.IsNullOrWhiteSpace(token))
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = new TimeSpan(0, 120, 0);
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Add("payload", "multipart/form-data");
                    if (!string.IsNullOrWhiteSpace(token))
                    {
                        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                    }
                    using (var content = new MultipartFormDataContent())
                    {
                        for (int count = 0; count < lstFilePath.Count; count++)
                        {
                            var filePath = lstFilePath[count];
                            var fileKey = fileKeys[count];
                            var fileContent = new ByteArrayContent(File.ReadAllBytes(filePath));
                            fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                            {
                                FileName = Path.GetFileName(filePath),
                                Name = fileKey
                            };
                            content.Add(fileContent);
                        }
                        content.Add(payloadContent, "payload");
                        //Uri postUri = new Uri(baseURI, apiPath);
                        string postUri = baseURI + apiPath;
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                        return client.PostAsync(postUri, content).Result;
                    }
                }
            } else 
            {
                HttpClientHandler hnd = new HttpClientHandler();
                hnd.UseDefaultCredentials = true;
                using (var client = new HttpClient(hnd))
                {
                    client.Timeout = new TimeSpan(0, 120, 0);
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Add("payload", "multipart/form-data");

                    using (var content = new MultipartFormDataContent())
                    {
                        for (int count = 0; count < lstFilePath.Count; count++)
                        {
                            var filePath = lstFilePath[count];
                            var fileKey = fileKeys[count];
                            var fileContent = new ByteArrayContent(File.ReadAllBytes(filePath));
                            fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                            {
                                FileName = Path.GetFileName(filePath),
                                Name = fileKey
                            };
                            content.Add(fileContent);
                        }
                        content.Add(payloadContent, "payload");
                        //Uri postUri = new Uri(baseURI, apiPath);
                        string postUri = baseURI + apiPath;
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                        return client.PostAsync(postUri, content).Result;
                    }
                }
            
        }
        }
    }
}
