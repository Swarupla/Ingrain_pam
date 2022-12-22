using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces
{
    public interface IPhoenixTokenService
    {
        dynamic GenerateToken();

        //string GeneratePamToken();
        // VDS Token
        //string GenerateVDSToken();
        /// <summary>
        /// Generate token for marketplace
        /// </summary>
        /// <returns></returns>
        string GenerateMarketPlaceToken();

        //string GeneratestageToken();

        dynamic GeneratePAMToken();

        public dynamic GeneratePAMTokenFromCookies();
    }
}
