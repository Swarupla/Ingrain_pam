namespace Accenture.MyWizard.Ingrain
{
    #region Namespace References
    using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
    using Accenture.MyWizard.Ingrain.WebService;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Collections.Generic;
    using LOGGING = Accenture.MyWizard.LOGGING;
    using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
    using Microsoft.Extensions.DependencyInjection;
    using System.Threading.Tasks;
    using Accenture.MyWizard.Ingrain.DataModels.Models;
    using Newtonsoft.Json;
    using System.Net.Http;
    using System.Text;
    using Microsoft.Extensions.Options;
    using Accenture.MyWizard.Shared.Helpers;

    #endregion
    public class SAASController : MyWizardControllerBase
    {
        #region Members      
        public static ISAASService saasService { set; get; }
        private readonly IOptions<IngrainAppSettings> appSettings;
        #endregion

        #region Constructor
        public SAASController(IServiceProvider serviceProvider, IOptions<IngrainAppSettings> settings)
        {
            saasService = serviceProvider.GetService<ISAASService>();
            appSettings = settings;
        }
        #endregion

        #region Methods         

        [HttpPost]
        [Route("api/SAASProvision")]
        public dynamic SAASProvision(SAASProvisioningRequest request)
        {
            bool PAMFDSInstance = false;
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(SAASController), nameof(SAASProvision), "START", string.Empty, string.Empty, string.Empty, string.Empty);
            SAASProvisionResponse response = new SAASProvisionResponse();
            SAASProvisionErrorResponse errorresponse = new SAASProvisionErrorResponse();
            
            errorresponse.OrderItemUId = request.OrderItemUId;
            errorresponse.OrderUId = request.OrderUId;
            errorresponse.ServiceUId = request.ServiceUId;
            errorresponse.Requestorid = request.RequestorID;
            errorresponse.ProvisionStatusUId = "00100000-0200-0000-0000-000000000000";
            errorresponse.Payload = JsonConvert.SerializeObject(request);
            List<ProvisonExtensionsSAAS> provisonExtensions_saas = new List<ProvisonExtensionsSAAS>();
            ProvisonExtensionsSAAS provisonExtensions = new ProvisonExtensionsSAAS();
            if (request.Applications.Find(x => x.Name == "IngrAIn") != null)
            {
                provisonExtensions.Value = request.Applications.Find(x => x.Name == "IngrAIn").ApplicationUId;
                provisonExtensions.Key = "ApplicationUId";
            }        
            provisonExtensions_saas.Add(provisonExtensions);
            errorresponse.ProvisonExtensions = provisonExtensions_saas;
            if (request.InstanceType.ToLower() == "dedicated/pam")
            {
                if (request.Applications.Find(x => x.Name == "Virtual Data Scientist") != null)
                {
                    PAMFDSInstance = true;
                }
            }
            var status = "";
            response.ClientUId = request.ClientUID;
            response.DeliveryConstructUId = request.DeliveryConstructUID;
            response.MySaaSServiceOrderUId = request.OrderUId;
            response.InstanceType = request.InstanceType;
            
            try
            {
                if ((request != null) && (request.ClientUID != null) && (request.DeliveryConstructUID != null) && (request.E2EUID != null) && (request.OrderUId != null) && (request.OrderItemUId != null) && (request.InstanceType != string.Empty) && (request.ServiceUId != null) && (request.SourceID != string.Empty) && (request.ClientName != string.Empty) && (request.E2EName != null) && (request.RequestorID != string.Empty) && (request.WBSE != string.Empty) && (request.Region != string.Empty))
                {
                    if (request.InstanceType.ToLower() == "file upload" || request.InstanceType.ToLower() == "dedicated/pam" || request.InstanceType.ToLower() == "container/pam" || request.InstanceType.ToLower() == "fds")
                    {
                        response = saasService.ProvisionSAAS(request);

                        if (PAMFDSInstance == true)
                        {
                            status = "Success";
                            //var saasResponse = JsonConvert.SerializeObject(response);
                            //var message = UpdateSAASProvisionResponse(saasResponse, errorresponse, request.UpdateStatusCallbackURL, request.ErrorCallbackURL);
                        }
                        if (response.StatusReason == SAASProvisionReason.StatusFailed)
                        {
                            status = response.StatusReason; 
                        }
                        //else
                        //{
                        //    status = "Success";
                        //}
                    }
                    else
                    {
                        errorresponse.Errordetails = "InstanceType value is incorrect";
                        var errormessage = SAASErrorResponse(errorresponse, request.ErrorCallbackURL);

                        status = "Failure";
                    }

                }
                else
                {

                    errorresponse.Errordetails = "Some fields are missing in the input payload";
                    var errormessage = SAASErrorResponse(errorresponse, request.ErrorCallbackURL);

                    status = "Failure";
                }
            }
            catch (Exception ex)
            {
                #region Generic Exception Handler
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(SAASController), nameof(SAASProvision), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message + ex.StackTrace);
                #endregion
            }
            return status;
        }
        [NonAction]
        public async Task<string> UpdateSAASProvisionResponse(string SAASprovisionResponse, SAASProvisionErrorResponse errorresponse, string UpdateStatusCallbackURL, string ErrorCallbackURL)
        {
            HttpClient authClient = new HttpClient();
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(SAASController), nameof(UpdateSAASProvisionResponse), "START", "json: SAASProvisionResponse - " + SAASprovisionResponse, string.Empty, string.Empty, string.Empty);
            try
            {
                authClient = saasService.GetOAuthAsyncFds(appSettings.Value.clientid_saas, appSettings.Value.clientsecret_saas, appSettings.Value.tokenendpoint_saas, appSettings.Value.scopes_saas, appSettings.Value.scope_saas, appSettings.Value.isazureadenabled,
                     appSettings.Value.azureclientid, appSettings.Value.azureclientsecret, appSettings.Value.azuretokenendpoint, appSettings.Value.azureresource);
                var URI = new Uri(appSettings.Value.SAASProvisionResponseApi);

                LOGGING.LogManager.Logger.LogProcessInfo(typeof(SAASController), nameof(UpdateSAASProvisionResponse), "START", "SAASProvisionResponseApi - " + URI, string.Empty, string.Empty, string.Empty);

                authClient.BaseAddress = URI;
                var httpContent = new StringContent(SAASprovisionResponse, Encoding.UTF8, "application/json");

                using (authClient)
                {

                    var httpResponse = await authClient.PostAsync(URI, httpContent).ConfigureAwait(false);

                    if (httpResponse.IsSuccessStatusCode)
                    {
                        return "Success";
                    }

                    else
                    {
                        errorresponse.Errordetails = "Getting error while posting provision response:" + httpResponse.IsSuccessStatusCode;
                        var errormessage = SAASErrorResponse(errorresponse, ErrorCallbackURL);
                        return "Failure";
                    }

                }
            }
            catch (Exception ex)
            {
                // Log.Error("Controller: TicketController, Method: UpdateSAASProvisionResponse, Exception: " + ex.ToString());
                return "Failure";
            }

        }
        [NonAction]
        public async Task<string> SAASErrorResponse(SAASProvisionErrorResponse request, string ErrorCallbackURL)
        {
            string response = string.Empty;
            string errorresponse = JsonConvert.SerializeObject(request);

            HttpClient authClient = new HttpClient();
            try
            {
                authClient = saasService.GetOAuthAsyncFds(appSettings.Value.clientid_saas, appSettings.Value.clientsecret_saas, appSettings.Value.tokenendpoint_saas, appSettings.Value.scopes_saas, appSettings.Value.scope_saas, appSettings.Value.isazureadenabled,
                     appSettings.Value.azureclientid, appSettings.Value.azureclientsecret, appSettings.Value.azuretokenendpoint, appSettings.Value.azureresource);


                var URI = new Uri(appSettings.Value.SAASProvisionErrorResponseApi);

                LOGGING.LogManager.Logger.LogProcessInfo(typeof(SAASController), nameof(UpdateSAASProvisionResponse), "START", "SAASProvisionErrorResponseApi - " + URI, string.Empty, string.Empty, string.Empty);

                authClient.BaseAddress = URI;
                var httpContent = new StringContent(errorresponse, Encoding.UTF8, "application/json");

                using (authClient)
                {

                    var httpResponse = await authClient.PostAsync(URI, httpContent).ConfigureAwait(false);
                    LOGGING.LogManager.Logger.LogProcessInfo(typeof(SAASController), nameof(UpdateSAASProvisionResponse), "START", "SAASProvisionErrorResponseStatuscode - " + URI, string.Empty, string.Empty, string.Empty);

                    if (httpResponse.IsSuccessStatusCode)
                        return "Success";
                    else
                        return "Failure";
                }
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(SAASController), nameof(SAASController), ex.Message + $" StackTrace = {ex.StackTrace}", ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return "Failure";
            }

        }

        [HttpPost]
        [Route("api/VDSProvisioning")]
        public dynamic VDSProvisioning(VDSRequestPayload request)
        {

            LOGGING.LogManager.Logger.LogProcessInfo(typeof(VDSRequestPayload), nameof(VDSProvisioning), "START", string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                dynamic status = "";
                if ((request != null) && (request.ClientUID != null) && (request.DeliveryConstructUID != null) && (request.E2EUID != null))
                {
                    status = saasService.updateVDSProvisioning(request);
                }
                else
                {
                    status = "Some fields are missing in request payload";
                }
                return status;

            }
            catch (Exception ex)
            {
                #region Generic Exception Handler
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(SAASController), nameof(SAASProvision), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message + ex.StackTrace);
                #endregion
            }
        }
    }
    #endregion
}

