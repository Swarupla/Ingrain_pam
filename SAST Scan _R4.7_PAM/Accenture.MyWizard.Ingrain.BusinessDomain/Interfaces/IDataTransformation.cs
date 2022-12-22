#region Copyright (c) 2019 Accenture . All rights reserved.

// <copyright company="Accenture">
// Copyright (c) 2019 Accenture.  All rights reserved.
// Reproduction or transmission in whole or in part, in any form or by any means, 
// electronic, mechanical or otherwise, is prohibited without the prior written 
// consent of the copyright owner.
// </copyright>

#endregion


namespace Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces
{
    using Accenture.MyWizard.Ingrain.DataModels.Models;

    public interface IDataTransformation
    {
        DataTransformationDTO GetPreProcessedData(string correlationId, int noOfRecord, bool showAllRecord, string problemType, int DecimalPlaces);

        DataTransformationViewData GetViewData(string correlationId);

        ColumnUniqueValue FetchUniqueColumns(string correlationId);

        void UpdateNewFeatures(string correlationId, string NewFeatures);
    }
}