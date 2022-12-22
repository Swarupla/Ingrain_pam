using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
using Accenture.MyWizard.Ingrain.DataModels.Models;
using Accenture.MyWizard.Ingrain.WebService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson.IO;
using Ninject;
using System;
using Newtonsoft.Json;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
using Accenture.MyWizard.Ingrain.BusinessDomain.Common;
using System.Threading.Tasks;

namespace Accenture.MyWizard.Ingrain
{

    public class ScopeSelectorController : MyWizardControllerBase
    {
        private IngrainDeliveryConstruct _ingrainDeliveryConstruct = null;

        public static IScopeSelectorService _iScopeSelectorService { set; get; }

        public static IPhoenixTokenService _iPhoenixTokenService { get; set; }
        public static IInstaModel _instaModelService { get; set; }

        #region Constructor
        public ScopeSelectorController(IServiceProvider serviceProvider)
        {
            _iScopeSelectorService = serviceProvider.GetService<IScopeSelectorService>();
            _iPhoenixTokenService = serviceProvider.GetService<IPhoenixTokenService>();
            _instaModelService = serviceProvider.GetService<IInstaModel>();
        }
        #endregion

        [HttpGet]
        [Route("api/UserStoryclientStructures")]
        public IActionResult UserStoryclientStructures(Guid ClientUID, string UserEmail)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ScopeSelectorController), nameof(UserStoryclientStructures), "Start", string.Empty, string.Empty, Convert.ToString(ClientUID), string.Empty);
            try
            {
                if (ClientUID == default(Guid) || string.IsNullOrEmpty(UserEmail))
                    return BadRequest(new { respone = Resource.IngrainResx.FailedStatusCode });
                if (!CommonUtility.GetValidUser(UserEmail))
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);

                var token = getToken();
                string clientStructureResult = _iScopeSelectorService.UserStoryclientStructureGetOuth(token, ClientUID, UserEmail);

                LOGGING.LogManager.Logger.LogProcessInfo(typeof(ScopeSelectorController), nameof(UserStoryclientStructures), "END", string.Empty, string.Empty, Convert.ToString(ClientUID), string.Empty);
                return GetSuccessResponse(clientStructureResult);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ScopeSelectorController), nameof(UserStoryclientStructures), ex.Message, ex, string.Empty, string.Empty, Convert.ToString(ClientUID), string.Empty);
                return GetFaultResponse(ex.Message);
            }
        }


        [HttpGet]
        [Route("api/UserStoryclientStructuresdetails")]
        public IActionResult UserStoryclientStructuresdetails(Guid ClientUID, string UserEmail)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ScopeSelectorController), nameof(UserStoryclientStructures), "Start", string.Empty, string.Empty, Convert.ToString(ClientUID), string.Empty);
            try
            {
                //if (ClientUID == default(Guid))
                //    return BadRequest(new { respone = Resource.IngrainResx.FailedStatusCode });
                if (!CommonUtility.GetValidUser(UserEmail))
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);

                var token = getToken();
                dynamic clientStructureResult = _iScopeSelectorService.UserStoryclientStructureGetOuthNew(token, ClientUID, UserEmail);

                LOGGING.LogManager.Logger.LogProcessInfo(typeof(ScopeSelectorController), nameof(UserStoryclientStructures), "END", string.Empty, string.Empty, Convert.ToString(ClientUID), string.Empty);
                return GetSuccessResponse(clientStructureResult);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ScopeSelectorController), nameof(UserStoryclientStructures), ex.Message, ex, string.Empty, string.Empty, Convert.ToString(ClientUID), string.Empty);
                return GetFaultResponse(ex.Message);
            }
        }

        [HttpGet]
        [Route("api/AppBuildInfo")]
        public IActionResult AppBuildInfo(Guid ClientUID, string UserEmail)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ScopeSelectorController), nameof(AppBuildInfo), "Start", string.Empty, string.Empty, Convert.ToString(ClientUID), string.Empty);
            try
            {
                if (ClientUID == null)
                    return BadRequest(new { respone = Resource.IngrainResx.FailedStatusCode });
                if (!CommonUtility.GetValidUser(UserEmail))
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);

                var token = getToken();
                dynamic appBuildResult = _iScopeSelectorService.getAppBuildInfo(token, ClientUID, UserEmail);

                LOGGING.LogManager.Logger.LogProcessInfo(typeof(ScopeSelectorController), nameof(AppBuildInfo), "END", string.Empty, string.Empty, Convert.ToString(ClientUID), string.Empty);
                return GetSuccessResponse(appBuildResult);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ScopeSelectorController), nameof(AppBuildInfo), ex.Message, ex, string.Empty, string.Empty, Convert.ToString(ClientUID), string.Empty);
                return GetFaultResponse(ex.Message);
            }
        }

        [HttpPost]
        [Route("api/GetDemographicTreeForUser")]
        public IActionResult GetDemographicTreeForUser(UserSecurityAcessAgile userSecurityAcessAgile)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ScopeSelectorController), nameof(GetDemographicTreeForUser), "Start", string.Empty, string.Empty, Convert.ToString(userSecurityAcessAgile.ClientUID), Convert.ToString(userSecurityAcessAgile.DeliveryConstructUId));
            try
            {
                #region VALIDATIONS
                if (userSecurityAcessAgile.ClientUID == default(Guid) || string.IsNullOrEmpty(userSecurityAcessAgile.UserID))
                    return BadRequest(new { respone = Resource.IngrainResx.FailedStatusCode });
                if (!CommonUtility.GetValidUser(userSecurityAcessAgile.UserID))
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);

                //CommonUtility.ValidateInputFormData(userSecurityAcessAgile.Token, CONSTANTS.Token, false);
                CommonUtility.ValidateInputFormData(Convert.ToString(userSecurityAcessAgile.ClientUID), CONSTANTS.ClientUID, true);
                CommonUtility.ValidateInputFormData(Convert.ToString(userSecurityAcessAgile.DeliveryConstructUId), CONSTANTS.DeliveryConstructUID, true);
                #endregion

                var token = getToken();
                var deliveryStructResults = "";

                deliveryStructResults = _iScopeSelectorService.GetDeliveryStructuerFromPhoenix(token, Convert.ToString(userSecurityAcessAgile.ClientUID), Convert.ToString(userSecurityAcessAgile.DeliveryConstructUId), userSecurityAcessAgile.UserID);

                LOGGING.LogManager.Logger.LogProcessInfo(typeof(ScopeSelectorController), nameof(GetDemographicTreeForUser), "END", string.Empty, string.Empty, Convert.ToString(userSecurityAcessAgile.ClientUID), Convert.ToString(userSecurityAcessAgile.DeliveryConstructUId));
                return GetSuccessResponse(deliveryStructResults);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ScopeSelectorController), nameof(GetDemographicTreeForUser), ex.Message, ex, string.Empty, string.Empty, Convert.ToString(userSecurityAcessAgile.ClientUID), Convert.ToString(userSecurityAcessAgile.DeliveryConstructUId));
                return GetFaultResponse(ex.Message);
            }
        }


        [HttpPost]
        [Route("api/PrivilegesForAccountorUser")]
        public IActionResult PrivilegesForAccountorUser(UserSecurityAcessAgile userSecurityAcessAgile)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ScopeSelectorController), nameof(PrivilegesForAccountorUser), "Start", string.Empty, string.Empty, Convert.ToString(userSecurityAcessAgile.ClientUID), Convert.ToString(userSecurityAcessAgile.DeliveryConstructUId));
            try
            {
                #region VALIDATIONS
                if (userSecurityAcessAgile.ClientUID == default(Guid) || string.IsNullOrEmpty(userSecurityAcessAgile.UserID))
                    return BadRequest(new { respone = Resource.IngrainResx.FailedStatusCode });
                if (!CommonUtility.GetValidUser(userSecurityAcessAgile.UserID))
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);

                //CommonUtility.ValidateInputFormData(userSecurityAcessAgile.Token, CONSTANTS.Token, false);
                CommonUtility.ValidateInputFormData(Convert.ToString(userSecurityAcessAgile.ClientUID), CONSTANTS.ClientUID, true);
                CommonUtility.ValidateInputFormData(Convert.ToString(userSecurityAcessAgile.DeliveryConstructUId), CONSTANTS.DeliveryConstructUID, true);
                #endregion
                var token = getToken();
                userSecurityAcessAgile.Token = token;
                var JsonStringResult = _iScopeSelectorService.fetchSecurityAcessAgilePhinix(userSecurityAcessAgile.Token, userSecurityAcessAgile);

                LOGGING.LogManager.Logger.LogProcessInfo(typeof(ScopeSelectorController), nameof(PrivilegesForAccountorUser), "END", string.Empty, string.Empty, Convert.ToString(userSecurityAcessAgile.ClientUID), Convert.ToString(userSecurityAcessAgile.DeliveryConstructUId));
                return GetSuccessResponse(JsonStringResult);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Unauthorized"))
                {
                    return Unauthorized("Unauthorized");
                }
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ScopeSelectorController), nameof(PrivilegesForAccountorUser), ex.Message, ex, string.Empty, string.Empty, Convert.ToString(userSecurityAcessAgile.ClientUID), Convert.ToString(userSecurityAcessAgile.DeliveryConstructUId));
                return GetFaultResponse(ex.Message);
            }
        }

        [HttpGet]
        [Route("api/GetDeliveryConstructs")]
        public IActionResult GetDeliveryConstructs(string UserId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ScopeSelectorController), "GetDeliveryConstructs - parameters-" + UserId, "START", string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                if (!CommonUtility.GetValidUser(UserId))
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);

                var columns = _iScopeSelectorService.GetDeliveryConstruct(UserId);
                return GetSuccessResponse(columns);

            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ScopeSelectorController), nameof(GetDeliveryConstructs), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ScopeSelectorController), nameof(GetDeliveryConstructs), "END", string.Empty, string.Empty, string.Empty, string.Empty);
            return GetSuccessResponse(Resource.IngrainResx.CorrelatioUIdNotMatch);
        }


        /// <summary>
        /// Set the Cookie value in IngrainDeliveryConstruct Collection 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/SetUserCookieDetails")]
        public IActionResult SetUserCookieDetails(bool value,string userId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ScopeSelectorController), "SetUserCookieDetails - parameters-" + value, "START", string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                if (!CommonUtility.GetValidUser(userId))
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);

                var columns = _iScopeSelectorService.SetUserCookieDetails(value, userId);
                return GetSuccessResponse(columns);

            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ScopeSelectorController), nameof(SetUserCookieDetails), ex.Message + ex.StackTrace, ex, "END", string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
        }


        [HttpGet]
        [Route("api/DeliveryConstructName")]
        public IActionResult DeliveryConstructName(Guid ClientUID, string UserEmail, Guid DeliveryConstructUId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ScopeSelectorController), nameof(DeliveryConstructName), "Start", string.Empty, string.Empty, Convert.ToString(ClientUID), Convert.ToString(DeliveryConstructUId));
            try
            {
                if (ClientUID == default(Guid))
                    return BadRequest(new { respone = Resource.IngrainResx.FailedStatusCode });
                if (!CommonUtility.GetValidUser(UserEmail))
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);

                var token = getToken();

                dynamic deliveryStructName = _iScopeSelectorService.DeliveryConstructName(token, ClientUID, DeliveryConstructUId, UserEmail);

                LOGGING.LogManager.Logger.LogProcessInfo(typeof(ScopeSelectorController), nameof(DeliveryConstructName), "END", string.Empty, string.Empty, Convert.ToString(ClientUID), Convert.ToString(DeliveryConstructUId));
                return GetSuccessResponse(deliveryStructName);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ScopeSelectorController), nameof(DeliveryConstructName), ex.Message, ex, string.Empty, string.Empty, Convert.ToString(ClientUID), Convert.ToString(DeliveryConstructUId));
                return GetFaultResponse(ex.Message);
            }
        }

        [HttpGet]
        [Route("api/GetMetricData")]
        public IActionResult GetMetricData(string ClientUID, string DeliveryConstructUId, [Optional] string userId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ScopeSelectorController), nameof(DeliveryConstructName), "Start", string.Empty, string.Empty,ClientUID,DeliveryConstructUId);
            try
            {
                //var scopeSelectorService = NinjectCoreBinding.NinjectKernel.Get<IScopeSelectorService>();
                //var token = _iScopeSelectorService.VDSSecurityTokenForPAD();

                if (!CommonUtility.GetValidUser(userId))
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);

                var token = getToken();
                dynamic deliveryStructName = _iScopeSelectorService.GetMetricData(token, ClientUID, DeliveryConstructUId, userId);

                LOGGING.LogManager.Logger.LogProcessInfo(typeof(ScopeSelectorController), nameof(DeliveryConstructName), "END", string.Empty, string.Empty, ClientUID, DeliveryConstructUId);
                return GetSuccessResponse(deliveryStructName);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ScopeSelectorController), nameof(DeliveryConstructName), ex.Message, ex, string.Empty, string.Empty, ClientUID, DeliveryConstructUId);
                return GetFaultResponse(ex.Message);

            }
        }

        /// <summary>
        /// If userId exist delete that record and insert as new record. If userId does not exist, insert as new record.
        /// </summary>
        /// <param name="ingrainDeliveryConstruct">The ingrain delivery construct</param> 
        /// <returns>Returns the message</returns>
        [HttpPost]
        [Route("api/PostDeliveryConstruct")]
        public IActionResult PostDeliveryConstruct([FromBody]dynamic requestBody)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ScopeSelectorController), nameof(PostDeliveryConstruct), "START", string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                string columnsData = Convert.ToString(requestBody);
                if (!string.IsNullOrEmpty(columnsData))
                {
                    _ingrainDeliveryConstruct = Newtonsoft.Json.JsonConvert.DeserializeObject<IngrainDeliveryConstruct>(columnsData);

                    #region VALIDATIONS
                    if (!CommonUtility.GetValidUser(_ingrainDeliveryConstruct.UserId))
                        return GetFaultResponse(Resource.IngrainResx.InValidUser);

                    CommonUtility.ValidateInputFormData(_ingrainDeliveryConstruct.ClientUId, CONSTANTS.ClientUId, true);
                    CommonUtility.ValidateInputFormData(_ingrainDeliveryConstruct.DeliveryConstructUID, CONSTANTS.DeliveryConstructUID, true);
                    CommonUtility.ValidateInputFormData(_ingrainDeliveryConstruct.AccessPrivilegeCode, "AccessPrivilegeCode", false);
                    CommonUtility.ValidateInputFormData(_ingrainDeliveryConstruct.AccessRoleName, "AccessRoleName", false);
                    #endregion

                    _iScopeSelectorService.PostDeliveryConstruct(_ingrainDeliveryConstruct);
                }
                else
                {
                    return GetSuccessResponse(Resource.IngrainResx.InputData);
                }

            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ScopeSelectorController), nameof(PostDeliveryConstruct), ex.Message + ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ScopeSelectorController), nameof(PostDeliveryConstruct), "END", string.Empty, string.Empty, string.Empty, string.Empty);
            return GetSuccessResponse("Success");
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ClientUID"></param>
        /// <param name="UserEmail"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("api/ClientNameByClientUId")]
        public IActionResult ClientNameByClientUId(Guid ClientUID, string DeliveryConstructUId, [Optional] string UserId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ScopeSelectorController), nameof(ClientNameByClientUId), "Start", string.Empty, string.Empty, Convert.ToString(ClientUID), Convert.ToString(DeliveryConstructUId));
            try
            {
                if (ClientUID == default(Guid))
                    return BadRequest(new { respone = Resource.IngrainResx.FailedStatusCode });

                if (!CommonUtility.GetValidUser(UserId))
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);

                var token = getToken();

                dynamic clientsResult = _iScopeSelectorService.ClientNameByClientUId(token, ClientUID, DeliveryConstructUId, UserId);


                LOGGING.LogManager.Logger.LogProcessInfo(typeof(ScopeSelectorController), nameof(ClientNameByClientUId), "END", string.Empty, string.Empty, Convert.ToString(ClientUID), Convert.ToString(DeliveryConstructUId));
                return GetSuccessResponse(clientsResult);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ScopeSelectorController), nameof(ClientNameByClientUId), ex.Message, ex, string.Empty, string.Empty, Convert.ToString(ClientUID), Convert.ToString(DeliveryConstructUId));
                return GetFaultResponse(ex.Message);
            }
        }


        [HttpGet]
        [Route("api/GetAppExecutionContext")]
        public IActionResult GetAppExecutionContext(Guid ClientUID, Guid DeliveryConstructUId, [Optional] string UserEmail)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ScopeSelectorController), nameof(GetAppExecutionContext), "Start", string.Empty, string.Empty, Convert.ToString(ClientUID), Convert.ToString(DeliveryConstructUId));
            try
            {
                if (ClientUID == default(Guid))
                    return BadRequest(new { respone = Resource.IngrainResx.FailedStatusCode });

                if (!CommonUtility.GetValidUser(UserEmail))
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);

                var token = getToken();

                dynamic deliveryStructName = _iScopeSelectorService.appExecutionContext(token, ClientUID, DeliveryConstructUId, UserEmail);

                LOGGING.LogManager.Logger.LogProcessInfo(typeof(ScopeSelectorController), nameof(GetAppExecutionContext), "END", string.Empty, string.Empty, Convert.ToString(ClientUID), Convert.ToString(DeliveryConstructUId));
                return GetSuccessResponse(deliveryStructName);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ScopeSelectorController), nameof(GetAppExecutionContext), ex.Message, ex, string.Empty, string.Empty, Convert.ToString(ClientUID), Convert.ToString(DeliveryConstructUId));
                return GetFaultResponse(ex.Message);
            }
        }

        [HttpGet]
        [Route("api/GetClientDetails")]
        public IActionResult GetClientDetails(Guid ClientUID, Guid DeliveryConstructUId, [Optional] string UserEmail)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ScopeSelectorController), nameof(GetClientDetails), "Start", string.Empty, string.Empty, Convert.ToString(ClientUID), Convert.ToString(DeliveryConstructUId));
            try
            {
                if (ClientUID == default(Guid))
                    return BadRequest(new { respone = Resource.IngrainResx.FailedStatusCode });

                if (!CommonUtility.GetValidUser(UserEmail))
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);

                var token = getToken();

                var ClientDetails = _iScopeSelectorService.ClientDetails(token, ClientUID, DeliveryConstructUId, UserEmail);

                LOGGING.LogManager.Logger.LogProcessInfo(typeof(ScopeSelectorController), nameof(GetClientDetails), "END", string.Empty, string.Empty, Convert.ToString(ClientUID), Convert.ToString(DeliveryConstructUId));
                return GetSuccessResponse(ClientDetails);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ScopeSelectorController), nameof(GetClientDetails), ex.Message, ex, string.Empty, string.Empty, Convert.ToString(ClientUID), Convert.ToString(DeliveryConstructUId));
                return GetFaultResponse(ex.Message);
            }
        }

        [HttpGet]
        [Route("api/PheonixTokenForVirtualAgent")]
        public IActionResult VirtualAgentPheonixToken()
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ScopeSelectorController), nameof(VirtualAgentPheonixToken), "Start", string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                dynamic token = getToken();

                LOGGING.LogManager.Logger.LogProcessInfo(typeof(ScopeSelectorController), nameof(VirtualAgentPheonixToken), "END", string.Empty, string.Empty, string.Empty, string.Empty);
                return GetSuccessResponse(token);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ScopeSelectorController), nameof(VirtualAgentPheonixToken), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);
            }
        }


        [HttpGet]
        [Route("api/PamDeliveryConstructName")]
        public IActionResult PamDeliveryConstructName(Guid DeliveryConstructUId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ScopeSelectorController), nameof(PamDeliveryConstructName), "Start", string.Empty, string.Empty, string.Empty, Convert.ToString(DeliveryConstructUId));
            try
            {
                if (DeliveryConstructUId == default(Guid))
                    return BadRequest(new { respone = Resource.IngrainResx.FailedStatusCode });

                var token = getPamToken();
                var deliveryStructName = _iScopeSelectorService.PAMDeliveryConstructName(token, DeliveryConstructUId);

                LOGGING.LogManager.Logger.LogProcessInfo(typeof(ScopeSelectorController), nameof(PamDeliveryConstructName), "END", string.Empty, string.Empty, string.Empty, Convert.ToString(DeliveryConstructUId));

                return GetSuccessResponse(deliveryStructName);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ScopeSelectorController), nameof(PamDeliveryConstructName), ex.Message, ex, string.Empty, string.Empty, string.Empty, Convert.ToString(DeliveryConstructUId));
                return GetFaultResponse(ex.Message);
            }
        }

        [HttpGet]
        [Route("api/multilanguage")]
        public IActionResult multilanguage(string ClientUID, string DeliveryConstructUId, [Optional] string UserEmail)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ScopeSelectorController), nameof(multilanguage), "Start", string.Empty, string.Empty, ClientUID, DeliveryConstructUId);
            try
            {
                if (DeliveryConstructUId == "undefined" || ClientUID == "undefined")
                    return BadRequest(new { respone = Resource.IngrainResx.FailedStatusCode });

                if (!CommonUtility.GetValidUser(UserEmail))
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);

                var token = getToken();
                dynamic languageInfo = _iScopeSelectorService.getLanguage(token, ClientUID, DeliveryConstructUId, UserEmail);

                LOGGING.LogManager.Logger.LogProcessInfo(typeof(ScopeSelectorController), nameof(multilanguage), "END", string.Empty, string.Empty, ClientUID, DeliveryConstructUId);

                return GetSuccessResponse(languageInfo);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ScopeSelectorController), nameof(multilanguage), ex.Message, ex, string.Empty, string.Empty, ClientUID, DeliveryConstructUId);
                return GetFaultResponse(ex.Message);
            }
        }

        [HttpGet]
        [Route("api/ForcedSignIn")]
        public IActionResult ForcedSigninAPI([Optional] string userId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ScopeSelectorController), nameof(ForcedSigninAPI), "Start", string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                //var scopeSelectorService = NinjectCoreBinding.NinjectKernel.Get<IScopeSelectorService>();
                //var token = _iScopeSelectorService.VDSSecurityTokenForPAD();
                if (!CommonUtility.GetValidUser(userId))
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);

                var token = getToken();
                dynamic deliveryStructName = _iScopeSelectorService.ForcedSignin(token, userId);

                LOGGING.LogManager.Logger.LogProcessInfo(typeof(ScopeSelectorController), nameof(ForcedSigninAPI), "END", string.Empty, string.Empty, string.Empty, string.Empty);
                return GetSuccessResponse(deliveryStructName);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ScopeSelectorController), nameof(ForcedSigninAPI), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message);

            }
        }

        private dynamic getToken()
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ScopeSelectorController), nameof(_iPhoenixTokenService.GenerateToken), "Start", string.Empty, string.Empty, string.Empty, string.Empty);

            dynamic token = _iPhoenixTokenService.GenerateToken();
            string environment = _instaModelService.GetEnvironment();
            if (environment.Equals(CONSTANTS.PAMEnvironment))
            {
                token = token["token"].ToString();
            }

            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ScopeSelectorController), nameof(_iPhoenixTokenService.GenerateToken), "END", string.Empty, string.Empty, string.Empty, string.Empty);

            return token;
        }

        //private string getstageToken()
        //{
        //    var token = _iPhoenixTokenService.GeneratestageToken();
        //    return token;
        //}
        private string getVDSToken()
        {
            //LOGGING.LogManager.Logger.LogProcessInfo(typeof(ScopeSelectorController), nameof(_iPhoenixTokenService.GenerateVDSToken), "Start", string.Empty, string.Empty, string.Empty, string.Empty);

            string token = null;
            string environment = _instaModelService.GetEnvironment();
            if(environment.Equals(CONSTANTS.PAMEnvironment))
            {
                token = _instaModelService.VDSSecurityTokenForPAM();
            }
            else
            {
                token = _instaModelService.VDSSecurityTokenForManagedInstance();
            }
            //LOGGING.LogManager.Logger.LogProcessInfo(typeof(ScopeSelectorController), nameof(_iPhoenixTokenService.GenerateVDSToken), "END", string.Empty, string.Empty, string.Empty, string.Empty);

            return token;
        }
        private string getPamToken()
        {
            //LOGGING.LogManager.Logger.LogProcessInfo(typeof(ScopeSelectorController), nameof(_iPhoenixTokenService.GeneratePamToken), "Start", string.Empty, string.Empty, string.Empty, string.Empty);

            //var token = _iPhoenixTokenService.GeneratePamToken();
            //LOGGING.LogManager.Logger.LogProcessInfo(typeof(ScopeSelectorController), nameof(_iPhoenixTokenService.GeneratePamToken), "END", string.Empty, string.Empty, string.Empty, string.Empty);

            //return token;
            return null;
        }

        [HttpGet]
        [Route("api/GetDemographicsName")]
        public IActionResult GetDemographicsName(string ClientUID, string DeliveryConstructUID, string E2EUID, string UserId)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ScopeSelectorController), nameof(GetDemographicsName), "Start", string.Empty, string.Empty, ClientUID, DeliveryConstructUID);
            try
            {
                if (ClientUID == null)
                    return BadRequest(new { respone = Resource.IngrainResx.FailedStatusCode });

                if (!CommonUtility.GetValidUser(UserId))
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);

                var token = getVDSToken();

                dynamic VDSInfo = _iScopeSelectorService.GetVDSDetail(token, ClientUID, DeliveryConstructUID, E2EUID, UserId);
                if(VDSInfo == null)
                    return GetFaultResponse("Response is null, Please check.");
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(ScopeSelectorController), nameof(GetDemographicsName), "END", string.Empty, string.Empty, ClientUID, DeliveryConstructUID);
                return GetSuccessResponse(VDSInfo);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ScopeSelectorController), nameof(GetDemographicsName), ex.Message, ex, string.Empty, string.Empty, ClientUID, DeliveryConstructUID);
                return GetFaultResponse(ex.Message);
            }
        }

        [HttpGet]
        [Route("api/GetDynamicEntity")]
        public IActionResult GetDynamicEntity(string ClientUID, string DeliveryConstructUId, [Optional] string UserEmail)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ScopeSelectorController), nameof(DeliveryConstructName), "Start", string.Empty, string.Empty,ClientUID,DeliveryConstructUId);
            try
            {
                //var scopeSelectorService = NinjectCoreBinding.NinjectKernel.Get<IScopeSelectorService>();
                //var token = _iScopeSelectorService.VDSSecurityTokenForPAD();

                if (!CommonUtility.GetValidUser(UserEmail))
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);

                var token = getToken();
                dynamic deliveryStructName = _iScopeSelectorService.GetDynamicEntity(token, ClientUID, DeliveryConstructUId, UserEmail);

                LOGGING.LogManager.Logger.LogProcessInfo(typeof(ScopeSelectorController), nameof(DeliveryConstructName), "END", string.Empty, string.Empty, ClientUID, DeliveryConstructUId);
                return GetSuccessResponse(deliveryStructName);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ScopeSelectorController), nameof(DeliveryConstructName), ex.Message, ex, string.Empty, string.Empty, ClientUID, DeliveryConstructUId);
                return GetFaultResponse(ex.Message);

            }
        }

        [HttpPost]
        [Route("api/IngrainENSEntityNotification")]
        public IActionResult IngrainENSEntityNotification([FromBody]ENSEntityNotification entityNotification)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ScopeSelectorController), nameof(IngrainENSEntityNotification),"START-"+entityNotification.EntityUId,string.Empty,entityNotification.EntityUId,entityNotification.ClientUId,entityNotification.DeliveryConstructUId);
            try
            {
                #region VALIDATIONS
                CommonUtility.ValidateInputFormData(entityNotification.ClientUId, CONSTANTS.ClientUId, true);
                CommonUtility.ValidateInputFormData(entityNotification.DeliveryConstructUId, CONSTANTS.DeliveryConstructUID, true);
                CommonUtility.ValidateInputFormData(entityNotification.EntityEventMessageStatusUId, "EntityEventMessageStatusUId", true);
                CommonUtility.ValidateInputFormData(entityNotification.EntityEventMessageUId, "EntityEventMessageUId", true);
                CommonUtility.ValidateInputFormData(entityNotification.EntityEventUId, "EntityEventUId", true);
                CommonUtility.ValidateInputFormData(entityNotification.EntityUId, "EntityUId", true);
                CommonUtility.ValidateInputFormData(entityNotification.Message, CONSTANTS.Message, false);
                CommonUtility.ValidateInputFormData(entityNotification.SenderApp, "SenderApp", false);
                CommonUtility.ValidateInputFormData(entityNotification.StatusReason, "StatusReason", false);
                CommonUtility.ValidateInputFormData(entityNotification.WorkItemTypeUId, "WorkItemTypeUId", true);
                #endregion

                _iScopeSelectorService.SaveENSNotification(entityNotification);

                return Ok();
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ScopeSelectorController), nameof(IngrainENSEntityNotification), ex.Message + ex.StackTrace, ex,string.Empty, entityNotification.EntityUId, entityNotification.ClientUId, entityNotification.DeliveryConstructUId);
                return GetFaultResponse(ex.Message);
            }
        }


        [HttpGet]
        [Route("api/GetENSNotificationsDetails")]

        public IActionResult GetENSNotificationsDetails(string clientUId,string entityUId,string fromDate)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ScopeSelectorController), nameof(GetENSNotificationsDetails), "START",string.Empty,entityUId,clientUId,string.Empty);
            try
            {
                if (string.IsNullOrEmpty(clientUId))
                    throw new ArgumentNullException(nameof(clientUId));


                return Ok(_iScopeSelectorService.GetENSNotification(clientUId, entityUId, fromDate));

            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ScopeSelectorController), nameof(IngrainENSEntityNotification), ex.Message, ex, string.Empty, entityUId, clientUId, string.Empty);
                return GetFaultResponse(ex.Message);
            }
        }
        [HttpGet]
        [Route("api/GetAccountClientDCSearch")]
        public IActionResult GetAccountClientDCSearch(string ClientUId, string DeliveryConstructUId, string Email, string SearchStr)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ScopeSelectorController), nameof(GetAccountClientDCSearch), "Start", string.Empty, string.Empty, Convert.ToString(ClientUId), Convert.ToString(DeliveryConstructUId));
            try
            {
                if (string.IsNullOrEmpty(ClientUId) || string.IsNullOrEmpty(Email))
                    return BadRequest(new { respone = Resource.IngrainResx.FailedStatusCode });

                if (!CommonUtility.GetValidUser(Email))
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);

                var token = getToken();

                dynamic ClientDCSearchResult = _iScopeSelectorService.GetAccountClientDeliveryConstructsSearch(token,  Convert.ToString(ClientUId), Convert.ToString(DeliveryConstructUId), Email, SearchStr);

                LOGGING.LogManager.Logger.LogProcessInfo(typeof(ScopeSelectorController), nameof(GetAccountClientDCSearch), "END", string.Empty, string.Empty, Convert.ToString(ClientUId), Convert.ToString(DeliveryConstructUId));
                return GetSuccessResponse(ClientDCSearchResult);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ScopeSelectorController), nameof(GetAccountClientDCSearch), ex.Message, ex, string.Empty, string.Empty, Convert.ToString(ClientUId), Convert.ToString(DeliveryConstructUId));
                return GetFaultResponse(ex.Message);
            }
        }

        [HttpGet]
        [Route("api/GetDecimalPointPlacesValue")]
        public IActionResult GetDecimalPointPlacesValue(string ClientUId, string DeliveryConstructUId, string UserEmail)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ScopeSelectorController), nameof(GetDecimalPointPlacesValue), "Start", string.Empty, string.Empty, Convert.ToString(ClientUId), Convert.ToString(DeliveryConstructUId));
            try
            {
                if (string.IsNullOrEmpty(UserEmail))
                    return BadRequest(new { respone = Resource.IngrainResx.UserIdEmpty });

                if (!CommonUtility.GetValidUser(UserEmail))
                    return GetFaultResponse(Resource.IngrainResx.InValidUser);

                var token = getToken();
                var columns = _iScopeSelectorService.GetDecimalPointPlacesValue(token,ClientUId, DeliveryConstructUId , UserEmail);
                return GetSuccessResponse(columns);
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ScopeSelectorController), nameof(GetDecimalPointPlacesValue), ex.Message, ex, string.Empty, string.Empty, Convert.ToString(ClientUId), Convert.ToString(DeliveryConstructUId));
                return GetFaultResponse(ex.Message);
            }
        }

        [HttpGet]
        [Route("api/GetClientScopeSelector")]
        public async Task<IActionResult> GetClientScopeSelector (Guid ClientUID,string DeliveryConstructUID, string UserEmail)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(ScopeSelectorController), nameof(GetClientScopeSelector), "Start", string.Empty, string.Empty, Convert.ToString(ClientUID), string.Empty);
            try
            {
                if(ClientUID == null)
                    return BadRequest(new { respone = Resource.IngrainResx.FailedStatusCode });

                //var token = getToken();
                dynamic appBuildResult = await _iScopeSelectorService.GetScopeSelectorData(ClientUID.ToString(),DeliveryConstructUID, UserEmail);

                LOGGING.LogManager.Logger.LogProcessInfo(typeof(ScopeSelectorController), nameof(GetClientScopeSelector), "END", string.Empty, string.Empty, Convert.ToString(ClientUID), string.Empty);
                return GetSuccessResponse(appBuildResult);
            }
            catch(Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(ScopeSelectorController), nameof(GetClientScopeSelector), ex.Message, ex, string.Empty, string.Empty, Convert.ToString(ClientUID), string.Empty);
                return GetFaultResponse(ex.ToString());
            }
        }
    }
}
