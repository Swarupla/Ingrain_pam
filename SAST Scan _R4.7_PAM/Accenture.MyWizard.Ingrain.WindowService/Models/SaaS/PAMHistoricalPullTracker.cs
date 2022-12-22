using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;
using Accenture.MyWizard.Ingrain.DataModels.Models;

namespace Accenture.MyWizard.Ingrain.WindowService.Models
{
    public class PAMHistoricalPullTracker
    {
        #region Properties
        public string ClientUId { get; set; }

        public string EndToEndUId { get; set; }
        public string EntityType { get; set; }
        public string PullType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime ProcessingStartTime { get; set; }
        public DateTime ProcessingEndTime { get; set; }
        public int Flag { get; set; }
        public string Status { get; set; }
        public int RecordCount { get; set; }
        public DateTime? dfLastUpdatedDate { get; set; }

        #endregion
    }

    public class ClientInfo
    {
        public SAASProvisionDetails SAASProvisionDetails { get; set; }
        public ATRProvisionRequestDto CAMConfigDetails { get; set; }
    }

}
