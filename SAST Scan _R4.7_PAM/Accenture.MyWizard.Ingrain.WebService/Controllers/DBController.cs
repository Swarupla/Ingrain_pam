#region Namespace
using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
using Accenture.MyWizard.Ingrain.DataModels.Models;
using Accenture.MyWizard.Shared.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
#endregion

namespace Accenture.MyWizard.Ingrain.WebService
{
    public class DBController : MyWizardControllerBase
    {
        #region Members
        private static IDBService DbService { get; set; }
        private readonly IOptions<IngrainAppSettings> _appSettings;
        private IServiceProvider _serviceProvider;
        private SSAIIngrainRequest selfServiceRequest;
        //private DBDataInfo selfServiceRequest;
        #endregion

        #region Constructor
        public DBController(IServiceProvider serviceProvider, IOptions<IngrainAppSettings> settings)
        {
            DbService = serviceProvider.GetService<IDBService>();
            _appSettings = settings;
            _serviceProvider = serviceProvider;
        }
        #endregion

        #region Methods
        [HttpGet]
        [Route("api/GetSelfServiceRequestStatusCount")]
        public IActionResult GetSelfServiceRequestStatusCount()
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DBController), nameof(GetSelfServiceRequestStatusCount), "START", string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
            try
            {
                selfServiceRequest = DbService.GetSelfServiceRequestStatusCount();
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(DBController), nameof(GetSelfServiceRequestStatusCount), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }

            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DBController), nameof(GetSelfServiceRequestStatusCount), "END", string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
            return GetSuccessWithMessageResponse(selfServiceRequest);
        }

        [HttpPost]
        [Route("api/GetDBData")]
        public IActionResult GetDBData([FromBody] DBData data)
        {
            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DBController), nameof(GetDBData), "START", string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
            DBDataInfo dbDataInfo = new DBDataInfo();
            try
            {
                if (!string.IsNullOrEmpty(data.CollectionName) && data.FilterBy != null && data.FilterBy.Count > 0)
                {                   
                    if (data.SortBy == null || (data.SortBy != null && CONSTANTS.Sort.Contains(data.SortBy.Order)))
                    {
                        dbDataInfo = DbService.GetData(data);
                    }
                    else
                    {
                        return GetFaultResponse("Sort order should be 1 (or) -1");
                    }
                }
                else
                {
                    return GetFaultResponse("CollectionName/FilterBy is Empty");
                }               
            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(DBController), nameof(GetDBData), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                return GetFaultResponse(ex.Message + ex.StackTrace);
            }

            LOGGING.LogManager.Logger.LogProcessInfo(typeof(DBController), nameof(GetDBData), "END", string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
            return GetSuccessWithMessageResponse(dbDataInfo);
        }
        #endregion
    }
}
