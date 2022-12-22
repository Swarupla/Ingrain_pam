using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.WindowService.Models.SaaS
{
    public enum AuthProviderType
    {
        /// <summary>
        /// The windows basic
        /// </summary>
        WindowsBasic,

        /// <summary>
        /// The o auth1
        /// </summary>
        oAuth1,

        /// <summary>
        /// The o auth2
        /// </summary>
        oAuth2,

        /// <summary>
        /// The Phoenix
        /// </summary>
        Phoenix,
        /// <summary>
        /// PAM
        /// </summary>
        PAM,
        /// <summary>
        /// VDSPhoenix
        /// </summary>
        VDSPhoenix
    }
}
