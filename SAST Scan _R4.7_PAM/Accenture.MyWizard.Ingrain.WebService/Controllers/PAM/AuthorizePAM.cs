using Accenture.MyWizard.Shared.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System;

namespace Accenture.MyWizard.Ingrain.WebService.Controllers
{
    public class AuthorizePAM : IAuthorizationFilter
    {
        #region Members
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContext;
        private readonly IOptions<IngrainAppSettings> _appSettings;
        #endregion

        #region Constructors
        public AuthorizePAM(IConfiguration configuration, IHttpContextAccessor httpContext, IOptions<IngrainAppSettings> settings)
        {
            _configuration = configuration;
            _httpContext = httpContext;
            _appSettings = settings;
        }
        #endregion

        #region Methods
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            PAMAuth obj = new PAMAuth(_configuration, _appSettings);
            //SessionManagement session = new SessionManagement(_httpContext);
            try
            {
                string token = string.Empty;
                StringValues isAuthorization;

                if (context.HttpContext.Request.Headers["Authorization"].Count > 0)
                {
                    Console.WriteLine("IsAuthorization is true");
                    context.HttpContext.Request.Headers.TryGetValue("Authorization", out isAuthorization);
                    if (isAuthorization.Count != 0)
                    {
                        if (context.HttpContext.Request.Headers.Count > 0 && context.HttpContext.Request.Headers["Authorization"].Count > 0)
                        {
                            Console.WriteLine("Token to be generated");
                            var tokens = context.HttpContext.Request.Headers["Authorization"][0].Substring("Bearer ".Length);
                            token = tokens.ToString();
                        }
                    }
                    if (string.IsNullOrEmpty(token))
                    {
                        //actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.Unauthorized);
                        context.Result = new UnauthorizedResult();
                    }
                    else
                    {
                        if (!obj.ValidatePAMToken(token))//|| !session.ValidateSession())
                        {
                            context.Result = new UnauthorizedResult();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                context.Result = new UnauthorizedResult();
                throw ex;
            }
            //base.OnAuthorization(actionContext);
        }
        #endregion
    }
}
