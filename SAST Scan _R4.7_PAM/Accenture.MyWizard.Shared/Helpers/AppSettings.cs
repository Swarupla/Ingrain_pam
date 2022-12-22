#region Copyright (c) 2018 Accenture . All rights reserved.

// <copyright company="Accenture">
// Copyright (c) 2018 Accenture.  All rights reserved.
// Reproduction or transmission in whole or in part, in any form or by any means, 
// electronic, mechanical or otherwise, is prohibited without the prior written 
// consent of the copyright owner.
// </copyright>

#endregion

namespace Accenture.MyWizard.Shared
{
    #region Namespace References

    using System;
    using System.Configuration;
    using System.Diagnostics.CodeAnalysis;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Net.Sockets;
    using System.Security.Principal;
    using CONSTANTS= Constants;
    #endregion

    /// <summary>
    /// Represents AppSettings.
    /// </summary>
    /// <remarks>
    /// Represents AppSettings.
    /// </remarks>
    [ExcludeFromCodeCoverage]
    public static class AppSettings
    {
        #region Private Static Properties
        #endregion

        #region Public Static Properties

        /// <summary>
        /// Gets the machine FQDN.
        /// </summary>
        /// <value>
        /// The machine FQDN.
        /// </value>
        public static String MachineFQDN
        {
            get
            {
                var domainName = IPGlobalProperties.GetIPGlobalProperties().DomainName;
                var hostName = Dns.GetHostName();
                var fqdn = "";

                fqdn = !hostName.Contains(domainName) ? hostName + "." + domainName : hostName;

                return fqdn;
            }
        }

        /// <summary>
        /// Gets the machine ip.
        /// </summary>
        /// <value>
        /// The machine ip.
        /// </value>
        public static String MachineIP
        {
            get
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                var localIP = String.Empty;

                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        localIP = ip.ToString();
                    }
                }

                return localIP;
            }
        }

        /// <summary>
        /// Gets the name of the application.
        /// </summary>
        /// <value>
        /// The name of the application.
        /// </value>
        public static String AppName
        {
            get
            {
                var value = ConfigurationManager.AppSettings.Get(CONSTANTS.IngrainAppConstants.AppName);

                if (String.IsNullOrEmpty(value))
                {
                    value = CONSTANTS.IngrainAppConstants.MyWizardIngrain;
                }

                return value;
            }
        }

        /// <summary>
        /// Gets the user.
        /// </summary>
        /// <value>
        /// The user.
        /// </value>
        public static String User
        {
            get
            {
                var value = ConfigurationManager.AppSettings.Get(CONSTANTS.IngrainAppConstants.User);

                if (String.IsNullOrEmpty(value))
                {
                    // Linux & Windows 
                    value = Environment.UserName;
                    //value = WindowsIdentity.GetCurrent().Name;
                }

                if (!String.IsNullOrEmpty(value))
                {
                    value = value.Replace(@"\", "\\\\");
                }

                return value;
            }
        }

        #endregion Public Static Properties
    }
}