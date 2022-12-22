#region Copyright (c) 2018 Accenture . All rights reserved.

// <copyright company="Accenture">
// Copyright (c) 2018 Accenture.  All rights reserved.
// Reproduction or transmission in whole or in part, in any form or by any means, 
// electronic, mechanical or otherwise, is prohibited without the prior written 
// consent of the copyright owner.
// </copyright>

#endregion

#region DataTransformationService Information
/********************************************************************************************************\
Module Name     :   DataTransformationService
Project         :   Accenture.MyWizard.SelfServiceAI.BusinessDomain
Organisation    :   Accenture Technologies Ltd.
Created By      :   Swetha Chandrasekar
Created Date    :   10-June-2019
Revision History :
Revision No             Initial             Modified Date           Description
1.0                     An                               
\********************************************************************************************************/
#endregion

namespace Accenture.MyWizard.Ingrain.BusinessDomain.Services
{
    #region Namespace 
    using Accenture.MyWizard.Ingrain.BusinessDomain.Common;
    using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
    using Accenture.MyWizard.Ingrain.DataAccess;
    using Accenture.MyWizard.Ingrain.DataModels.Models;
    using Accenture.MyWizard.Shared.Helpers;
    using Microsoft.Extensions.Options;
    using MongoDB.Bson;
    using MongoDB.Driver;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using Microsoft.Extensions.DependencyInjection;
    using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;
    using MongoDB.Bson.Serialization;
    #endregion

    public class DataTransformationService : IDataTransformation
    {
        #region Members
        private MongoClient _mongoClient;
        private IMongoDatabase _database;
        DatabaseProvider databaseProvider;
        private readonly IOptions<IngrainAppSettings> appSettings;
        int Precision;
        public static IEncryptionDecryption _encryptionDecryption { set; get; }
        #endregion

        #region Constructors
        public DataTransformationService(DatabaseProvider db, IOptions<IngrainAppSettings> settings, IServiceProvider serviceProvider)
        {
            databaseProvider = db;
            appSettings = settings;
            _mongoClient = databaseProvider.GetDatabaseConnection();
            var dataBaseName = MongoUrl.Create(appSettings.Value.connectionString).DatabaseName;
            _database = _mongoClient.GetDatabase(dataBaseName);
            _encryptionDecryption = serviceProvider.GetService<IEncryptionDecryption>();
        }
        #endregion

        #region Methods
        /// <summary>
        /// Gets the pre processded data for Data Transformation
        /// </summary>
        /// <param name="correlationId">The correlation identifier</param>
        /// <param name="noOfRecord">The no of records</param>
        /// <param name="showAllRecord">The show all records</param>
        /// <param name="problemType">The problemType</param>
        /// <returns>Returns the result.</returns>
        public DataTransformationDTO GetPreProcessedData(string correlationId, int noOfRecord, bool showAllRecord, string problemType, int DecimalPlaces)
        {
            BsonArray inputD = new BsonArray();
            Precision = DecimalPlaces;
            DataTransformationDTO data = new DataTransformationDTO();
            var dbCollection = _database.GetCollection<BsonDocument>(CONSTANTS.DEPreProcessedData);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var projectionScenario = Builders<BsonDocument>.Projection.Include(CONSTANTS.CorrelationId).Include(CONSTANTS.Input_Data).Include(CONSTANTS.ColumnsList).Exclude(CONSTANTS.Id);
            var dbData = dbCollection.Find(filter).Project<BsonDocument>(projectionScenario).ToList();
           bool DBEncryptionRequired = CommonUtility.EncryptDB(correlationId,appSettings);
            if (dbData.Count > 0)
            {
                if (problemType == CONSTANTS.TimeSeries)
                {
                    BsonArray Arrayyearly =  new BsonArray();
                    BsonArray Arrayhourly =  new BsonArray();
                    BsonArray Arraydaily = new BsonArray();
                    BsonArray Arrayweekly =  new BsonArray();
                    BsonArray Arraymonthly = new BsonArray();
                    BsonArray Arrayquarterly = new BsonArray();
                    BsonArray ArrayhalfYearly = new BsonArray();
                    BsonArray Arrayfortnightly = new BsonArray();
                    BsonArray ArraycustomDays = new BsonArray();

                    TimeSeriesFrequencyAttributes attributes = new TimeSeriesFrequencyAttributes();
                    for (int i = 0; i < dbData.Count; i++)
                    {
                        JObject datas = new JObject();
                        if (DBEncryptionRequired)
                        {
                            string decryptedInput = _encryptionDecryption.Decrypt(dbData[i][CONSTANTS.Input_Data].ToString());
                            datas = JObject.Parse(decryptedInput);
                        }
                        else
                        {
                            datas = JObject.Parse(dbData[i][CONSTANTS.Input_Data].ToString());
                        }
                        
                        //TimeSeries - Frequencies
                        var yearly = Convert.ToString(datas[CONSTANTS.Yearly]);
                        var hourly = Convert.ToString(datas[CONSTANTS.Hourly]);
                        var daily = Convert.ToString(datas[CONSTANTS.Daily]);
                        var weekly = Convert.ToString(datas[CONSTANTS.Weekly]);
                        var monthly = Convert.ToString(datas[CONSTANTS.Monthly]);
                        var quarterly = Convert.ToString(datas[CONSTANTS.Quarterly]);
                        var halfYearly = Convert.ToString(datas[CONSTANTS.HalfYear]);
                        var fortnightly = Convert.ToString(datas[CONSTANTS.Fortnightly]);
                        var customDays = Convert.ToString(datas[CONSTANTS.CustomDays]);

                        if (!string.IsNullOrEmpty(yearly))
                        {
                            // attributes.Yearly.AddRange(JsonConvert.DeserializeObject<List<object>>(yearly));
                            Arrayyearly.AddRange(BsonSerializer.Deserialize<BsonArray>(yearly));
                        }
                        if (!string.IsNullOrEmpty(hourly))
                        {
                            //attributes.Hourly.AddRange(JsonConvert.DeserializeObject<List<object>>(hourly));
                            Arrayhourly.AddRange(BsonSerializer.Deserialize<BsonArray>(hourly));
                        }
                        if (!string.IsNullOrEmpty(daily))
                        {
                            //attributes.Daily.AddRange(JsonConvert.DeserializeObject<List<object>>(daily));
                            Arraydaily.AddRange(BsonSerializer.Deserialize<BsonArray>(daily));
                        }
                        if (!string.IsNullOrEmpty(weekly))
                        {
                            //attributes.Weekly.AddRange(JsonConvert.DeserializeObject<List<object>>(weekly));
                            Arrayweekly.AddRange(BsonSerializer.Deserialize<BsonArray>(weekly));
                        }
                        if (!string.IsNullOrEmpty(monthly))
                        {
                            //attributes.Monthly.AddRange(JsonConvert.DeserializeObject<List<object>>(monthly));
                            Arraymonthly.AddRange(BsonSerializer.Deserialize<BsonArray>(monthly));
                        }
                        if (!string.IsNullOrEmpty(quarterly))
                        {
                            //attributes.Quarterly.AddRange(JsonConvert.DeserializeObject<List<object>>(quarterly));
                            Arrayquarterly.AddRange(BsonSerializer.Deserialize<BsonArray>(quarterly));
                        }
                        if (!string.IsNullOrEmpty(halfYearly))
                        {
                            //attributes.HalfYearly.AddRange(JsonConvert.DeserializeObject<List<object>>(halfYearly));
                            ArrayhalfYearly.AddRange(BsonSerializer.Deserialize<BsonArray>(halfYearly));
                        }
                        if (!string.IsNullOrEmpty(fortnightly))
                        {
                            //attributes.Fortnightly.AddRange(JsonConvert.DeserializeObject<List<object>>(fortnightly));
                            Arrayfortnightly.AddRange(BsonSerializer.Deserialize<BsonArray>(fortnightly));
                        }
                        if (!string.IsNullOrEmpty(customDays))
                        {
                            //attributes.CustomDays.AddRange(JsonConvert.DeserializeObject<List<object>>(customDays));
                            ArraycustomDays.AddRange(BsonSerializer.Deserialize<BsonArray>(customDays));
                        }
                    }
                    if (Arrayyearly != null && Arrayyearly.Count != 0)
                    {
                        attributes.Yearly =CommonUtility.GetDataAfterDecimalPrecision(Arrayyearly, Precision, noOfRecord, showAllRecord);
                    }
                    if (Arrayhourly != null && Arrayhourly.Count != 0)
                    {
                        attributes.Hourly = CommonUtility.GetDataAfterDecimalPrecision(Arrayhourly, Precision, noOfRecord, showAllRecord);
                    }
                    if (Arraydaily != null && Arraydaily.Count != 0)
                    {
                        attributes.Yearly = CommonUtility.GetDataAfterDecimalPrecision(Arraydaily, Precision, noOfRecord, showAllRecord);
                    }
                    if (Arrayweekly != null && Arrayweekly.Count != 0)
                    {
                        attributes.Yearly = CommonUtility.GetDataAfterDecimalPrecision(Arrayweekly, Precision, noOfRecord, showAllRecord);
                    }
                    if (Arraymonthly != null && Arraymonthly.Count != 0)
                    {
                        attributes.Monthly = CommonUtility.GetDataAfterDecimalPrecision(Arraymonthly, Precision, noOfRecord, showAllRecord);
                    }
                    if (Arrayquarterly != null && Arrayquarterly.Count != 0)
                    {
                        attributes.Quarterly = CommonUtility.GetDataAfterDecimalPrecision(Arrayquarterly, Precision, noOfRecord, showAllRecord);
                    }
                    if (ArrayhalfYearly != null && ArrayhalfYearly.Count != 0)
                    {
                        attributes.HalfYearly = CommonUtility.GetDataAfterDecimalPrecision(ArrayhalfYearly, Precision, noOfRecord, showAllRecord);
                    }
                    if (Arrayfortnightly != null && Arrayfortnightly.Count != 0)
                    {
                        attributes.Fortnightly = CommonUtility.GetDataAfterDecimalPrecision(Arrayfortnightly, Precision, noOfRecord, showAllRecord);
                    }
                    if (ArraycustomDays != null && ArraycustomDays.Count != 0)
                    {
                        attributes.CustomDays = CommonUtility.GetDataAfterDecimalPrecision(ArraycustomDays, Precision, noOfRecord, showAllRecord);
                    }

                    if (!showAllRecord)
                    {
                        attributes.Yearly = attributes.Yearly.Take(noOfRecord).ToList();
                        attributes.Hourly = attributes.Hourly.Take(noOfRecord).ToList();
                        attributes.Daily = attributes.Daily.Take(noOfRecord).ToList();
                        attributes.Weekly = attributes.Weekly.Take(noOfRecord).ToList();
                        attributes.Monthly = attributes.Monthly.Take(noOfRecord).ToList();
                        attributes.Quarterly = attributes.Quarterly.Take(noOfRecord).ToList();
                        attributes.HalfYearly = attributes.HalfYearly.Take(noOfRecord).ToList();
                        attributes.Fortnightly = attributes.Fortnightly.Take(noOfRecord).ToList();
                        attributes.CustomDays = attributes.CustomDays.Take(noOfRecord).ToList();
                    }

                    data.TimeSeriesInputData = attributes;
                }
                else
                {
                    List<object> lstInputData = new List<object>();
                    for (int i = 0; i < dbData.Count; i++)
                    {
                        string json = string.Empty;
                        if (DBEncryptionRequired)
                        {
                            json = _encryptionDecryption.Decrypt(dbData[i][CONSTANTS.Input_Data].ToString());
                        }
                        else
                        {
                            json = dbData[i][CONSTANTS.Input_Data].ToString();
                        }
                        inputD.AddRange(BsonSerializer.Deserialize<BsonArray>(json));
                        //lstInputData.AddRange(JsonConvert.DeserializeObject<List<object>>(json));
                    }

                    lstInputData = CommonUtility.GetDataAfterDecimalPrecision(inputD, Precision, noOfRecord, showAllRecord);
                    data.InputData = lstInputData;
                    //data.InputData = !showAllRecord ? lstInputData.Take(noOfRecord).ToList() : lstInputData;
                }

                data.CorrelationId = dbData[0][CONSTANTS.CorrelationId].ToString();
                data.ColumnList = dbData[0][CONSTANTS.ColumnsList].ToString();
            }

            return data;
        }

        /// <summary>
        /// Gets the data for view data quality
        /// </summary>
        /// <param name="correlationId">The correlation identifier</param>
        /// <returns>Returns the list of records</returns>
        public DataTransformationViewData GetViewData(string correlationId)
        {
            DataTransformationViewData data = new DataTransformationViewData();
            var dbCollection = _database.GetCollection<BsonDocument>(CONSTANTS.DEDataCleanup);
            var filter = Builders<BsonDocument>.Filter.Eq(CONSTANTS.CorrelationId, correlationId);
            var projectionScenario = Builders<BsonDocument>.Projection.Include(CONSTANTS.CorrelationId).Include(CONSTANTS.ViewDataQuality).Exclude(CONSTANTS.Id);
            var dbData = dbCollection.Find(filter).Project<BsonDocument>(projectionScenario).ToList();
            bool DBEncryptionRequired = CommonUtility.EncryptDB(correlationId, appSettings);
            if (dbData.Count > 0) 
            {
                if (DBEncryptionRequired)
                    dbData[0][CONSTANTS.ViewDataQuality] = BsonDocument.Parse(_encryptionDecryption.Decrypt(dbData[0][CONSTANTS.ViewDataQuality].AsString));
                JObject viewDataQuality = JObject.Parse(dbData[0][CONSTANTS.ViewDataQuality].ToString());
                string[] removeItems = new string[] { CONSTANTS.BinningValues, CONSTANTS.ImBalanced, CONSTANTS.OrdinalNominal, CONSTANTS.ProblemType };
                foreach (var attribute in viewDataQuality.Children())
                {
                    JProperty childAttribute = attribute as JProperty;
                    foreach (string column in removeItems)
                    {
                        JObject header = (JObject)viewDataQuality[childAttribute.Name];
                        header.Property(column).Remove();
                    }
                }

                data.ViewData = viewDataQuality;
            }

            List<string> datasource = new List<string>();
            datasource = CommonUtility.GetDataSourceModel(correlationId, appSettings);
            if (datasource.Count > 0)
            {
                data.ModelName = datasource[0];
                data.DataSource = datasource[1];
                data.ModelType = datasource[2];
                data.BusinessProblem = datasource.Count > 3 ? datasource[3] : null;
                data.Category = datasource[5];
            }

            return data;


        }
        public ColumnUniqueValue FetchUniqueColumns(string correlationId)
        {
            ColumnUniqueValue columnUniqueValue = new ColumnUniqueValue();
            var filter = Builders<BsonDocument>.Filter.Eq("CorrelationId", correlationId);
            var collection = _database.GetCollection<BsonDocument>("DataCleanUP_FilteredData");
            var projection = Builders<BsonDocument>.Projection.Include("CorrelationId").Include("ColumnUniqueValues").Exclude("_id");
            var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();
            bool DBEncryptionRequired = CommonUtility.EncryptDB(correlationId,appSettings);
            if (DBEncryptionRequired)
                result[0]["ColumnUniqueValues"] = BsonDocument.Parse(_encryptionDecryption.Decrypt(result[0]["ColumnUniqueValues"].AsString));

            var UniqueValues = JObject.Parse(result[0]["ColumnUniqueValues"].ToString());
            columnUniqueValue.correlationId = result[0]["CorrelationId"].ToString();

            List<UniqueValues> uniqueValues = new List<UniqueValues>();

            foreach (var item in UniqueValues.Children())
            {
                JObject serializeData = new JObject();
                JProperty jProperty = item as JProperty;
                JObject serializeDataCols = new JObject();
                if (jProperty != null)
                {
                    int i = 0;
                    Dictionary<string, string> DiValues = new Dictionary<string, string>();
                    for (int j = 0; j < jProperty.Value.Count(); j++)
                    {
                        i++;
                        DiValues.Add(i.ToString(), jProperty.Value[j].ToString());
                    }
                    uniqueValues.Add(new UniqueValues
                    {
                        ColumnName = jProperty.Name,
                        UniqueValue = DiValues
                    });

                }
            }

            columnUniqueValue.ColumnsUniqueValues = uniqueValues;
            return columnUniqueValue;

        }
        public void UpdateNewFeatures(string correlationId, string NewFeatures)
        {
            var newfeatures = JObject.Parse(NewFeatures.ToString());
            JObject serializeDataRemove = new JObject();
            string resultData = string.Empty;
            var filter = Builders<BsonDocument>.Filter.Eq("CorrelationId", correlationId);
            var collection = _database.GetCollection<BsonDocument>("DE_DataProcessing");
            var projection = Builders<BsonDocument>.Projection.Include("DataModification").Exclude("_id");
            var result = collection.Find(filter).Project<BsonDocument>(projection).ToList();

            foreach (var item in newfeatures.Children())
            {
                JObject serializeData = new JObject();
                JObject serializeDatanew = new JObject();
                JObject serializeDatavalues = new JObject();
                JObject serializeChildvalues = new JObject();
                JProperty jProperty = item as JProperty;
                serializeData = JObject.Parse(jProperty.Value.ToString());
                foreach (var item1 in serializeData.Children())
                {
                    JProperty jProperty1 = item1 as JProperty;
                    if (jProperty1.Name == "value_check" || jProperty1.Name == "value")
                    {
                        if (jProperty1.Name == "value")
                        {
                            serializeDatanew = JObject.Parse(jProperty1.Value.ToString());
                            foreach (var data in serializeDatanew.Children())
                            {
                                JProperty jProperty2 = data as JProperty;

                                serializeChildvalues = JObject.Parse(jProperty2.Value.ToString());
                                foreach (var child in serializeChildvalues.Children())
                                {
                                    JProperty jProperty3 = child as JProperty;
                                    string updateField2 = string.Format("DataModification.NewAddFeatures.{0}.{1}.{2}.{3}", jProperty.Name, jProperty1.Name, jProperty2.Name, jProperty3.Name);
                                    var existfieldUpdate2 = Builders<BsonDocument>.Update.Set(updateField2, jProperty3.Value.ToString());
                                    var existResults2 = collection.UpdateManyAsync(filter, existfieldUpdate2);
                                }

                            }
                        }
                        else
                        {
                            string updateField1 = string.Format("DataModification.NewAddFeatures.{0}.{1}", jProperty.Name, jProperty1.Name);
                            var existfieldUpdate1 = Builders<BsonDocument>.Update.Set(updateField1, jProperty1.Value.ToString());
                            var existResults1 = collection.UpdateManyAsync(filter, existfieldUpdate1);
                        }
                    }
                    else
                    {
                        serializeDatanew = JObject.Parse(jProperty1.Value.ToString());
                        foreach (var data in serializeDatanew.Children())
                        {
                            JProperty jProperty2 = data as JProperty;
                            string updateField1 = string.Format("DataModification.NewAddFeatures.{0}.{1}.{2}", jProperty.Name, jProperty1.Name, jProperty2.Name);
                            var existfieldUpdate1 = Builders<BsonDocument>.Update.Set(updateField1, jProperty2.Value.ToString());
                            var existResults1 = collection.UpdateManyAsync(filter, existfieldUpdate1);
                        }

                    }

                }
            }
        }
        #endregion
    }
}
