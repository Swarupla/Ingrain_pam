using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.WindowService.Models.SaaS
{
    public class ServiceCallerRequest
    {
        #region Members
        #endregion

        

        /// <summary>
        /// Represents Name of servicecaller.
        /// </summary>
        /// <remarks>
        /// Represents Name of servicecaller.
        /// </remarks>
        public String Name { get; set; }

        /// <summary>
        /// Gets or sets the name of the data formatter type.
        /// </summary>
        /// <value>
        /// The name of the data formatter type.
        /// </value>
        public String DataFormatterTypeName { get; set; }

        /// <summary>
        /// Gets the DataFormatterType.
        /// </summary>
        /// <value>
        /// The DataFormatterType.
        /// </value>
        public DataFormatterType DataFormatterType
        {
            get
            {
                DataFormatterType dataFormatterType = DataFormatterType.Xml;

                if (!String.IsNullOrEmpty(DataFormatterTypeName))
                {
                    Enum.TryParse(DataFormatterTypeName, out dataFormatterType);
                }

                return dataFormatterType;
            }
        }
        /// <summary>
        /// Gets or sets the service data provider u identifier.
        /// </summary>
        /// <value>
        /// The service data provider u identifier.
        /// </value>
        public Guid ServiceDataProviderUId { get; set; }

        /// <summary>
        /// Gets or sets the service URL.
        /// </summary>
        /// <value>The service URL.</value>
        public String ServiceUrl { get; set; }
        /// <summary>
        /// Gets or sets the type of the content.
        /// </summary>
        /// <value>The type of the content.</value>
        public String ContentType { get; set; }

        /// <summary>
        /// Gets or sets the name of the HTTP verb.
        /// </summary>
        /// <value>The name of the HTTP verb.</value>
        public String HttpVerbName { get; set; }

        /// <summary>
        /// Gets the HTTP verb.
        /// </summary>
        /// <value>The HTTP verb.</value>
        public HttpVerb HttpVerb
        {
            get
            {
                HttpVerb httpVerb = HttpVerb.GET;

                if (!String.IsNullOrEmpty(HttpVerbName))
                {
                    Enum.TryParse(HttpVerbName.ToUpper(), out httpVerb);
                }

                return httpVerb;
            }
        }

        /// <summary>
        /// Gets or sets the name of the data formatter type.
        /// </summary>
        /// <value>The name of the data formatter type.</value>
        public String MIMEMediaType { get; set; }
        /// <summary>
        /// Gets or sets the content.
        /// </summary>
        /// <value>
        /// The content.
        /// </value>
        public String Content { get; set; }
        /// <summary>
        /// Gets or sets the Accept.
        /// </summary>
        /// <value>
        /// The Accept.
        /// </value>
        public String Accept { get; set; }
        /// <summary>
        /// Gets or sets the json root node.
        /// </summary>
        /// <value>
        /// The json root node.
        /// </value>
        public String JsonRootNode { get; set; }
        /// <summary>
        /// Gets or sets the authentication provider.
        /// </summary>
        /// <value>The authentication provider.</value>
        public AuthProvider AuthProvider { get; set; }

    }
    public enum HttpVerb
    {
        /// <summary>
        /// The get
        /// </summary>
        GET,

        /// <summary>
        /// The post
        /// </summary>
        POST,

        /// <summary>
        /// The put
        /// </summary>
        PUT,

        /// <summary>
        /// The delete
        /// </summary>
        DELETE
    }

}
