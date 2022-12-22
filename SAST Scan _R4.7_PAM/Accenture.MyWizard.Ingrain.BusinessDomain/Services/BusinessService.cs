#region Copyright (c) 2018 Accenture . All rights reserved.

// <copyright company="Accenture">
// Copyright (c) 2018 Accenture.  All rights reserved.
// Reproduction or transmission in whole or in part, in any form or by any means, 
// electronic, mechanical or otherwise, is prohibited without the prior written 
// consent of the copyright owner.
// </copyright>

#endregion

#region IngestedService Information
/********************************************************************************************************\
Module Name     :   BusinessProblemDataService
Project         :   Accenture.MyWizard.SelfServiceAI.BusinessDomain
Organisation    :   Accenture Technologies Ltd.
Created By      :   RaviA
Created Date    :   10-Jan-2019
Revision History :
Revision No             Initial             Modified Date           Description
1.0                     An                  10-Jan-2019             
\********************************************************************************************************/
#endregion
namespace Accenture.MyWizard.Ingrain.BusinessDomain.Services
{
    #region Namespace Reference
    using System;
    using System.Linq;
    using MongoDB.Driver;
    using MongoDB.Bson;
    using MongoDB.Bson.Serialization;
    using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
    using Accenture.MyWizard.Ingrain.DataModels.Models;
    using Accenture.MyWizard.Ingrain.DataAccess;
    using Accenture.MyWizard.Shared.Helpers;
    using Microsoft.Extensions.Options;
    using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
    #endregion
    public class BusinessService : IBusinessService
    {
        #region Members
        private MongoClient _client;
        private IMongoDatabase _database;
        private readonly DatabaseProvider databaseProvider;
        private IngrainAppSettings appSettings { get; set; }
        #endregion

        #region Methods

        #region Constructors
        /// <summary>
        /// Constructor to Initialize the MongoDB connection.
        /// </summary>
        public BusinessService(DatabaseProvider db, IOptions<IngrainAppSettings> settings)
        {
            databaseProvider = db;
            _client = databaseProvider.GetDatabaseConnection();
            appSettings = settings.Value;
            var dataBaseName = MongoUrl.Create(appSettings.connectionString).DatabaseName;
            _database = _client.GetDatabase(dataBaseName);

        }
        #endregion


        /// <summary>
        /// Save the BusinessProblem Data into the Mongo Database.
        /// </summary>
        /// <param name="businessProblemDataDTO"></param>
        /// <returns>Insertion Success or Failure</returns>
        public void InsertData(BusinessProblemDataDTO businessProblemDataDTO)
        {
            var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(businessProblemDataDTO);
            var insertDocument = BsonSerializer.Deserialize<BsonDocument>(jsonData);
            var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIBusinessProblem);
            collection.InsertOneAsync(insertDocument);
        }


        public string GetBusinessProblemData(string correlationId)
        {
            string resultData = string.Empty;

            try
            {
                var collection = _database.GetCollection<BsonDocument>(CONSTANTS.SSAIBusinessProblem);
                var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
                var resultdocument = collection.Find(filter).ToList();
                if (resultdocument.Count > 0)
                {
                    resultData = resultdocument[0].ToString();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return resultData;
        }

        #endregion


    }
}
