using Accenture.MyWizard.Fortress.Core.Configurations;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace Accenture.MyWizard.Ingrain.WindowService.Models.SaaS
{
    public class AIAAMiddleLayerRequest
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public object this[string propertyName]
        {
            get
            {
                Type type = typeof(AIAAMiddleLayerRequest);
                PropertyInfo propInfo = type.GetProperty(propertyName);
                return propInfo.GetValue(this, null);
            }
            set
            {
                Type type = typeof(AIAAMiddleLayerRequest);
                PropertyInfo propInfo = type.GetProperty(propertyName);
                propInfo.SetValue(this, value, null);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public string StartTime { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string EndTime { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<SecurityProvider> SecurityProviders { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<AIAASubscriber> Subscriber { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<TicketUpload> TicketUploads { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<ProjectStructureProvider> ProjectStructureProviders { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<DataProvider> DataProviders { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public object ProjectId { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public object ParentDeliveryConstructUId { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public object DeliveryConstructUId { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public object UserId { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public object foo1 { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public object foo2 { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public object foo3 { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public DateTime StartDate { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public DateTime EndDate { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public DateTime LastReportedDate { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int BatchSize { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int PageNumber { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public HistoricalPullRequest HistoricalPullRequest { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string TicketPullType { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string TicketType { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string DateTimeFormat { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<PAMTicketPullDateRange> TicketTypes { get; set; }
        /// <summary>
        /// 
        /// </summary>

        public UploadHistoricalTicketRequest uploadTicketRequest { get; set; }
        public string WorkItemTypeUId { get; set; }
        public string ClientUId { get; set; }
        public string EndToEndUId { get; set; }
    }
    /// <summary>
    /// 
    /// </summary>
    public class PAMTicketPullDateRange
    {
        /// <summary>
        /// 
        /// </summary>
        public DateTime? StartDate { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public DateTime? EndDate { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string TicketType { get; set; }
    }


    /// <summary>
    /// 
    /// </summary>
    public class SecurityProvider
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public object this[string propertyName]
        {
            get
            {
                Type type = typeof(SecurityProvider);
                PropertyInfo propInfo = type.GetProperty(propertyName);
                return propInfo.GetValue(this, null);
            }
            set
            {
                Type type = typeof(SecurityProvider);
                PropertyInfo propInfo = type.GetProperty(propertyName);
                propInfo.SetValue(this, value, null);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string SecurityProviderTypeName { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string ServiceUrl { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Method { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string MIMEMediaType { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Accept { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public DataFormatter DataFormatter { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public AuthProvider AuthProvider { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string ExpectedResultfromResponse { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Boolean IsProjectIdCanbeNull { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string ReadElementfromResponse { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string InputRequestValues { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string InputRequestType { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string DefaultKeys { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string DefaultValues { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string InputRequestKeys { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string JsonRootNode { get; set; }
    }
    /// <summary>
    /// 
    /// </summary>
    public class ProjectStructureProvider
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public object this[string propertyName]
        {
            get
            {
                Type type = typeof(ProjectStructureProvider);
                PropertyInfo propInfo = type.GetProperty(propertyName);
                return propInfo.GetValue(this, null);
            }
            set
            {
                Type type = typeof(ProjectStructureProvider);
                PropertyInfo propInfo = type.GetProperty(propertyName);
                propInfo.SetValue(this, value, null);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string ProjectStructureProviderTypeName { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string ServiceUrl { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Method { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string MIMEMediaType { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Accept { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public DataFormatter DataFormatter { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public AuthProvider AuthProvider { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string ExpectedResultfromResponse { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Boolean IsProjectIdCanbeNull { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string ReadElementfromResponse { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string InputRequestValues { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string DefaultKeys { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string DefaultValues { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string InputRequestType { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string InputRequestKeys { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string JsonRootNode { get; set; }
    }
    /// <summary>
    /// 
    /// </summary>
    public class DataProvider
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public object this[string propertyName]
        {
            get
            {
                Type type = typeof(DataProvider);
                PropertyInfo propInfo = type.GetProperty(propertyName);
                return propInfo.GetValue(this, null);
            }
            set
            {
                Type type = typeof(DataProvider);
                PropertyInfo propInfo = type.GetProperty(propertyName);
                propInfo.SetValue(this, value, null);
            }
        }
        /// <summary>
        /// Represents DataProviderType Enumerator.
        /// </summary>
        /// <remarks>
        /// Represents DataProviderType Enumerator.
        /// </remarks>
        public DataProviderType DataProviderType
        {
            get
            {
                DataProviderType DataProviderType;

                Enum.TryParse(DataProviderTypeName, out DataProviderType);

                return DataProviderType;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string TicketType { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string TicketStatusToFilter { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string DataProviderTypeName { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string ServiceUrl { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Method { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string TicketPullType { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int IntialBatchSize { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int BatchSize { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string FilterDateFormat { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int IncrementDateRangeBy { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string InputRequestType { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string InputRequestKeys { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string InputRequestValues { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string JsonRootNode { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string MIMEMediaType { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Accept { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public DataFormatter DataFormatter { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public AuthProvider AuthProvider { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string DefaultKeys { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string DefaultValues { get; set; }
        public string WorkItemTypeUId { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
    }
    /// <summary>
    /// 
    /// </summary>
    public class AIAASubscriber
    {
        /// <summary>
        /// 
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public AuthProvider AuthProvider { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string DefaultKeys { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string DefaultValues { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string InputRequestKeys { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string InputRequestValues { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string ServiceUrl { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Method { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string MIMEMediaType { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Accept { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string InputRequestType { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string JsonRootNode { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string TicketPullType { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public UploadHistoricalTicketRequest UploadTicketRequest { get; set; }


    }
    /// <summary>
    /// 
    /// </summary>
    public class TicketUpload
    {
        /// <summary>
        /// 
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string DataProviderTypeName { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string ServiceUrl { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Method { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string MIMEMediaType { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Accept { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public DataFormatter DataFormatter { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public AuthProvider AuthProvider { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string DefaultKeys { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string DefaultValues { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string TicketStatusToFilter { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string FilterDateFormat { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string TicketType { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int IncrementDateRangeBy { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string TicketPullType { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int IntialBatchSize { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int BatchSize { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string InputRequestType { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string InputRequestKeys { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string InputRequestValues { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string JsonRootNode { get; set; }


    }
    /// <summary>
    /// 
    /// </summary>
    public class DataFormatter
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public object this[string propertyName]
        {
            get
            {
                Type type = typeof(DataFormatter);
                PropertyInfo propInfo = type.GetProperty(propertyName);
                return propInfo.GetValue(this, null);
            }
            set
            {
                Type type = typeof(DataFormatter);
                PropertyInfo propInfo = type.GetProperty(propertyName);
                propInfo.SetValue(this, value, null);
            }
        }
        /// <summary>
        /// Represents DataFormatterType Enumerator.
        /// </summary>
        /// <remarks>
        /// Represents DataFormatterType Enumerator.
        /// </remarks>
        public DataFormatterType DataFormatterType
        {
            get
            {
                DataFormatterType DataFormatterType;

                Enum.TryParse(DataFormatterTypeName, out DataFormatterType);

                return DataFormatterType;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public string Json { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string XsltFilePath { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string XsltArguments { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public XDocument XDocument { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string DataFormatterTypeName { get; set; }
    }
    /// <summary>
    /// 
    /// </summary>
    public class AuthProvider
    {
        /// <summary>
        /// Represents AuthProviderType Enumerator.
        /// </summary>
        /// <remarks>
        /// Represents AuthProviderType Enumerator.
        /// </remarks>
        public AuthProviderType AuthProviderType
        {
            get
            {
                AuthProviderType authProviderType;

                Enum.TryParse(AuthProviderTypeName, out authProviderType);

                return authProviderType;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public object this[string propertyName]
        {
            get
            {
                Type type = typeof(AuthProvider);
                PropertyInfo propInfo = type.GetProperty(propertyName);
                return propInfo.GetValue(this, null);
            }
            set
            {
                Type type = typeof(AuthProvider);
                PropertyInfo propInfo = type.GetProperty(propertyName);
                propInfo.SetValue(this, value, null);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public string AuthProviderTypeName { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string FederationUrl { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string ClientId { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Secret { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Scope { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Token { get; set; }

        public string Resource { get; set; }
        public string AppServiceUId { get; set; }

        public string Subject { get; set; }
        public string Issuer { get; set; }
        public string Thumbprint { get; set; }
        public string IsTLS12Enabled { get; set; }
        public string CertType { get; set; }
        public string GrantType { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
    }
    /// <summary>
    /// 
    /// </summary>
    public class HistoricalPullRequest
    {
        #region Private Fields
        /// <summary>
        /// InsightBy
        /// </summary>
        public string InsightBy { get; set; }
        /// <summary>
        /// Status
        /// </summary>
        public string Status { get; set; }
        /// <summary>
        /// UserType
        /// </summary>
        public int UserType { get; set; }
        /// <summary>
        /// OpportunityBy
        /// </summary>
        public string OpportunityBy { get; set; }
        /// <summary>
        /// SupportGroupName
        /// </summary>
        public List<string> SupportGroupName { get; set; }
        /// <summary>
        /// ApplicationName
        /// </summary>
        public string ApplicationName { get; set; }
        /// <summary>
        /// DeepDiveStatus
        /// </summary>
        public string DeepDiveStatus { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string TicketType { get; set; }
        /// <summary>
        /// DDName
        /// </summary>
        public string DDName { get; set; }
        /// <summary>
        /// UserId
        /// </summary>
        public string UserId { get; set; }
        /// <summary>
        /// BatchId
        /// </summary>
        public int BatchID { get; set; }

        /// <summary>
        /// VersionId
        /// </summary>
        public int VersionId { get; set; }
        /// <summary>
        /// VersionId
        /// </summary>
        public List<DeliveryStructureAttribute> DeliveryStructureAttribute { get; set; }
        /// <summary>
        /// DeliveryConstructVersions
        /// </summary>
        public List<DeliveryConstructVersion> DeliveryConstructVersions { get; set; }
        /// <summary>
        /// ClientName
        /// </summary>
        public string ClientName { get; set; }
        /// <summary>
        /// DeliveryConstructStructure
        /// </summary>
        public string DeliveryConstructStructure { get; set; }
        /// <summary>
        /// CMSDeliveryStructure
        /// </summary>
        public List<DeliveryStructure> CMSDeliveryStructure { get; set; }
        /// <summary>
        /// TreeViewStructure
        /// </summary>
        public List<DeliveryStructure> TreeViewStructure { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string InsightId { get; set; }
        /// <summary>
        /// LastReportedEndDate
        /// </summary>
        public DateTime LastReportedEndDate { get; set; }
        /// <summary>
        /// IsBatchUpdated
        /// </summary>
        public bool IsBatchUpdated { get; set; }
        /// <summary>
        /// ReMigrationIndicator
        /// </summary>
        public string ReMigrationIndicator { get; set; }

        #endregion

    }
    /// <summary>
    /// DeliveryStructure
    /// </summary>
    public class DeliveryStructure
    {
        /// <summary>
        /// DeliveryConstructUId
        /// </summary>
        public string DeliveryConstructUId { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string DeliveryConstructType { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string ParentDeliveryConstructUId { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string DeliveryConstruct { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// ParentDeliveryConstructType
        /// </summary>
        public string ParentDeliveryConstructType { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public IList<DeliveryStructure> Children { get; set; } = new List<DeliveryStructure>();
    }

    public enum DataFormatterType
    {
        /// <summary>
        /// The none
        /// </summary>
        None,
        /// <summary>
        /// The XSLT
        /// </summary>
        Xslt,

        /// <summary>
        /// The json
        /// </summary>
        Json,

        /// <summary>
        /// The XML
        /// </summary>
        Xml
    }
}
