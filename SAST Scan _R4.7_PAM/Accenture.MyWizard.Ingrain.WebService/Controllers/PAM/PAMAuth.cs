using Accenture.MyWizard.Fortress.Core.Utilities;
using Accenture.MyWizard.Shared.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.Runtime.Caching;

namespace Accenture.MyWizard.Ingrain.WebService.Controllers
{
    public class PAMAuth
    {
        #region Members    
        private readonly IConfiguration _configuration;
        private readonly IOptions<IngrainAppSettings> _appSettings;
        #endregion

        #region Constructors
        public PAMAuth(IConfiguration config, IOptions<IngrainAppSettings> settings)
        {
            _configuration = config;
            _appSettings = settings;
        }
        #endregion
        ObjectCache cache = MemoryCache.Default;
        public bool ValidatePAMToken(string atrAuthToken)
        {
            try
            {
                if (cache.Contains(atrAuthToken))
                {
                    var temp = cache.Get(atrAuthToken) as string;
                    if (temp == "ExpiredToken")
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }

                }
                else
                {
                    if (ValidateTokenWithATR(atrAuthToken))
                    {
                        CacheItemPolicy policy = new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(5) };
                        cache.Add(atrAuthToken, "ATRToken", policy);
                        return true;
                    }
                    else
                        return false;
                }
            }
            catch (Exception ex)
            {
                return true;
            }
        }

        public bool ValidateTokenWithATR(string atrToken)
        {
            bool response;
            try
            {
                if (string.IsNullOrEmpty(atrToken))
                    return false;
                HttpClient httpClient = new HttpClient();
                System.Uri url = new System.Uri(Convert.ToString(_appSettings.Value.PamTokenValidationURL));
                HttpResponseMessage httpResponse = httpClient.PostAsJsonAsync(url, new { token = atrToken }).Result;
                if (!httpResponse.IsSuccessStatusCode)
                {
                    response = false;
                }
                else
                    response = true;
            }
            catch (Exception ex)
            {
                response = false;
            }

            return response;
        }
    }
}
