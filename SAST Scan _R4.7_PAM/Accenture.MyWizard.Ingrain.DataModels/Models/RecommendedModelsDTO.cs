#region Copyright (c) 2019 Accenture . All rights reserved.

// <copyright company="Accenture">
// Copyright (c) 2019 Accenture.  All rights reserved.
// Reproduction or transmission in whole or in part, in any form or by any means, 
// electronic, mechanical or otherwise, is prohibited without the prior written 
// consent of the copyright owner.
// </copyright>

#endregion

#region RecommendedAIViewModelDTO Information
/********************************************************************************************************\
Module Name     :   RecommendedModelsDTO
Project         :   Accenture.MyWizard.Ingrain.DataModels.Models.RecommendedModelsDTO
Organisation    :   Accenture Technologies Ltd.
Created By      :   Chandra
Created Date    :   30-Jan-2019
Revision History :
Revision No             Initial             Modified Date           Description
1.0                     An                  29-Mar-2019             
\********************************************************************************************************/
#endregion

namespace Accenture.MyWizard.Ingrain.DataModels.Models
{
    #region Namespace
    using MongoDB.Bson.Serialization.Attributes;
    using System;
    #endregion

    [BsonIgnoreExtraElements]
    public class RecommendedModelsDTO
    {
        public string _id { get; set; }

        public string DeliveryConstructUID { get; set; }

        public int AppId { get; set; }

        public string ClientUId { get; set; }

        public string CorrelationId { get; set; }

        [BsonElement]
        public object RecommendedModeltypes { get; set; }       

        public string CreatedOn { get; set; }

        public Int32 CreatedByUser { get; set; }

        public string ModifiedOn { get; set; }

        public Int32 ModifiedByUser { get; set; }   
    }   
}
