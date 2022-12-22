#region Namespaces
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Accenture.MyWizard.Cryptography;
using Accenture.MyWizard.Cryptography.EncryptionProviders;
using Accenture.MyWizard.LOGGING;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using CryptographyHelper = Accenture.MyWizard.Cryptography;
#endregion

namespace Accenture.MyWizard.Ingrain.Utility
{
    class Program
    {
        #region Members
        static IMongoDatabase _database = null;
        static IMongoClient _mongoClient = null;
        static AppSettings appSettings = null;
        static CryptographyHelper.Utility.CryptographyUtility CryptographyUtility = null;
        #endregion

        #region Main
        static void Main(string[] args)
        {
            try
            {
                appSettings = AppSettingsJson.GetAppSettings().GetSection("AppSettings").Get<AppSettings>();
                IMongoClient _mongoClient = MongoDataContext.GetDatabaseConnection(appSettings.connectionString, appSettings.certificatePath, appSettings.certificatePassKey);
                var dataBaseName = MongoUrl.Create(appSettings.connectionString).DatabaseName;
                _database = _mongoClient.GetDatabase(dataBaseName);
                Console.WriteLine("DB Connection established successfully.");
                //get the Encrypted docuements and proceed for decrpt and encrypt with new key               
                RunOnIngrainDb();
                Thread.Sleep(1000);
                RunOnClusteringServiceDb();
                Thread.Sleep(1000);
                RunOnAIServiceDb();
                Thread.Sleep(1000);
                RunOnMonteCarloDB();
                Thread.Sleep(1000);
                RunOnInferenceEngineDb();
                Thread.Sleep(1000);
                Console.WriteLine("Completed Encryption for all the Services In Ingrain Application, Please press enter to close");
                Console.ReadLine();
                CryptographyUtility = new CryptographyHelper.Utility.CryptographyUtility();

            }
            catch (Exception ex)
            {
                LogManager.Logger.LogErrorMessage(typeof(Program), "Error in main", ex.StackTrace, ex, string.Empty, string.Empty, string.Empty, string.Empty);
            }
        }
        #endregion

        #region Methods
        private static void RunOnIngrainDb()
        {
            try
            {
                LogManager.Logger.LogProcessInfo(typeof(Program), nameof(RunOnIngrainDb), "Starting encryption for IngrAInDB" + "-STARTED -" + DateTime.Now.ToString(), string.Empty, string.Empty, string.Empty, string.Empty);
                Console.WriteLine("Starting encryption for IngrAInDB - " + DateTime.Now.ToString());
                var deployModelCollection = _database.GetCollection<BsonDocument>("SSAI_DeployedModels");
                //var deploFilter = Builders<BsonDocument>.Filter.Eq("DBEncryptionRequired", false);
                var deploFilter = Builders<BsonDocument>.Filter.Empty;
                var deploymodelProjection = Builders<BsonDocument>.Projection.Include("_id").Include("CorrelationId").Include("DBEncryptionRequired");
                var deployModelResults = deployModelCollection.Find(deploFilter).Project<BsonDocument>(deploymodelProjection).Skip(4768).ToList();
                int count = 0;
                if (deployModelResults.Count > 0)
                {
                    foreach (var model in deployModelResults)
                    {
                        try
                        {
                            count++;
                            Console.WriteLine("IngrainDB executing record " + count + "/" + deployModelResults.Count + " CorrelationId: " + model["CorrelationId"].ToString() + " DBEncryptionRequired: " + Convert.ToBoolean(model["DBEncryptionRequired"]));
                            //if (!Convert.ToString(model["CorrelationId"]).Equals("f1b89086-3603-4b42-b62a-e31ce5b48b12"))
                            {
                                foreach (var c in appSettings.DbCollections)
                                {
                                    try
                                    {
                                        var attributes = c.Attributes.Split(",");
                                        var projection = Builders<BsonDocument>.Projection.Include("_id");
                                        foreach (var filedName in attributes)
                                        {
                                            projection = projection.Include(filedName.Trim());
                                        }
                                        var collection = _database.GetCollection<BsonDocument>(c.Name);
                                        var results = collection.Find(Builders<BsonDocument>.Filter.Eq("CorrelationId", model["CorrelationId"].ToString())).Project<BsonDocument>(projection).ToList();
                                        if (results.Count > 0)
                                        {
                                            foreach (var r in results)
                                            {
                                                try
                                                {
                                                    foreach (var item in attributes)
                                                    {
                                                        string key = item;
                                                        BsonElement element;
                                                        var exists = r.TryGetElement(key, out element);
                                                        if (exists)
                                                        {
                                                            try
                                                            {
                                                                string fieldType = r[key].GetType().ToString();
                                                                if (fieldType == "MongoDB.Bson.BsonString")
                                                                {
                                                                    if (!string.IsNullOrEmpty(Convert.ToString(r[key])) && Convert.ToString(r[key]) != "null")
                                                                    {
                                                                        var value = DecryptEncrypt(r[key].AsString, appSettings);
                                                                        var builder = Builders<BsonDocument>.Filter;
                                                                        FilterDefinition<BsonDocument> filter = builder.Eq("_id", r["_id"]);
                                                                        UpdateDefinition<BsonDocument> updateDefinition = Builders<BsonDocument>.Update.Set<dynamic>(key, value);
                                                                        //for (var i = 1; i < attributes.Length; i++)
                                                                        //{
                                                                        //    iteration = i;
                                                                        //    BsonElement elementKey;
                                                                        //    var existsKey = r.TryGetElement(attributes[i], out elementKey);
                                                                        //    if (existsKey)
                                                                        //    {
                                                                        //        if (r[attributes[i]].ToString() == "BsonString" && r[attributes[i]].ToString() != "null" && r[attributes[i]].ToString() != null)
                                                                        //            updateDefinition = updateDefinition.Set(attributes[i], DecryptEncrypt(r[attributes[i]].AsString, appSettings));
                                                                        //    }
                                                                        //}
                                                                        collection.UpdateOne(filter, updateDefinition);
                                                                    }
                                                                }
                                                            }
                                                            catch (Exception ex)
                                                            {
                                                                if (attributes.Length > 1)
                                                                    LogManager.Logger.LogErrorMessage(typeof(Program), nameof(RunOnIngrainDb), "Error - " + "CorrelationId -" + model["CorrelationId"].ToString() + "- Collection name -" + c.Name + "--ATTRIBUTENAME--", ex, string.Empty, string.Empty, string.Empty, string.Empty);
                                                                else
                                                                    LogManager.Logger.LogErrorMessage(typeof(Program), nameof(RunOnIngrainDb), "Error -" + "CorrelationId -" + model["CorrelationId"].ToString() + "- Collection name -" + c.Name + "--ATTRIBUTENAME--" + attributes[0], ex, string.Empty, string.Empty, string.Empty, string.Empty);
                                                            }

                                                        }
                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    LogManager.Logger.LogErrorMessage(typeof(Program), nameof(RunOnIngrainDb), "Error - forech var r in results" + "CorrelationId -" + model["CorrelationId"].ToString() + "- Collection name -" + c.Name + "--ATTRIBUTENAME--", ex, string.Empty, string.Empty, string.Empty, string.Empty);
                                                    Console.WriteLine(ex.StackTrace);
                                                }
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        LogManager.Logger.LogErrorMessage(typeof(Program), nameof(RunOnIngrainDb), "Error - foreach c in appSettings.DbCollections" + "CorrelationId -" + model["CorrelationId"].ToString() + "- Collection name -" + c.Name, ex, string.Empty, string.Empty, string.Empty, string.Empty);
                                        Console.WriteLine(ex.StackTrace);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            LogManager.Logger.LogErrorMessage(typeof(Program), nameof(RunOnIngrainDb), "Error - model in deployModelResults", ex, string.Empty, string.Empty, string.Empty, string.Empty);
                            Console.WriteLine(ex.StackTrace);
                        }
                    }
                }
                Console.WriteLine("Updated encryption for IngrAInDB - " + DateTime.Now.ToString());
                LogManager.Logger.LogProcessInfo(typeof(Program), nameof(RunOnIngrainDb), "Updated encryption for IngrAInDB Completed", string.Empty, string.Empty, string.Empty, string.Empty);
            }
            catch (Exception ex)
            {
                LogManager.Logger.LogErrorMessage(typeof(Program), nameof(RunOnIngrainDb), "Error - Main Catch", ex, string.Empty, string.Empty, string.Empty, string.Empty);
                Console.WriteLine(ex.StackTrace);
            }
        }

        private static void RunOnMonteCarloDB()
        {
            Console.WriteLine("\n----------------------------------------------------------------------------"); ;
            Console.WriteLine("Starting encryption for MonteCarloDB- " + DateTime.Now.ToString());
            _mongoClient = MongoDataContext.GetDatabaseConnection(appSettings.MonteCarloConnection, appSettings.certificatePath, appSettings.certificatePassKey);
            string dataBaseName = MongoUrl.Create(appSettings.MonteCarloConnection).DatabaseName;
            _database = _mongoClient.GetDatabase(dataBaseName);
            string corelationId = string.Empty;
            LogManager.Logger.LogProcessInfo(typeof(Program), nameof(RunOnMonteCarloDB), "Starting encryption for MonteCarloDB" + "-STARTED -" + DateTime.Now.ToString(), string.Empty, string.Empty, string.Empty, string.Empty);
            //get the Encrypted docuemnts and proceed for decrpt and encrypt with new key.            
            foreach (var c in appSettings.MonteCarlodbCollections)
            {
                int iteration = 0;
                var attributes = c.Attributes.Split(",");
                var projection = Builders<BsonDocument>.Projection.Include("_id");
                foreach (var filedName in attributes)
                {
                    projection = projection.Include(filedName);
                }
                projection = projection.Include("TemplateID");
                var collection = _database.GetCollection<BsonDocument>(c.Name);
                //LogManager.Logger.LogProcessInfo(typeof(Program), nameof(RunOnMonteCarloDB), "Starting for collection-" + "CollectionName -" + c.Name);
                var results = collection.Find(Builders<BsonDocument>.Filter.Empty).Project<BsonDocument>(projection).ToList();
                if (results.Count > 0)
                {
                    try
                    {
                        foreach (var r in results)
                        {
                            try
                            {
                                foreach (var item in attributes)
                                {
                                    try
                                    {
                                        corelationId = r["TemplateID"].ToString();
                                        var builder = Builders<BsonDocument>.Filter;
                                        //var key = attributes[0];
                                        string key = item;
                                        BsonElement element;
                                        var exists = r.TryGetElement(key, out element);
                                        if (exists)
                                        {
                                            string fieldType = r[key].GetType().ToString();
                                            if (fieldType == "MongoDB.Bson.BsonString")
                                            {
                                                if (!string.IsNullOrEmpty(Convert.ToString(r[key])) && Convert.ToString(r[key]) != "null")
                                                {
                                                    var value = DecryptEncrypt(r[key].AsString, appSettings);
                                                    FilterDefinition<BsonDocument> filter = builder.Eq("_id", r["_id"]);
                                                    UpdateDefinition<BsonDocument> updateDefinition = Builders<BsonDocument>.Update.Set<dynamic>(key, value);
                                                    //for (var i = 1; i < attributes.Length; i++)
                                                    //{
                                                    //    iteration = i;
                                                    //    BsonElement elementKey;
                                                    //    var existsKey = r.TryGetElement(attributes[i], out elementKey);
                                                    //    if (existsKey)
                                                    //    {
                                                    //        if (r[attributes[i]].ToString() == "BsonString" && r[attributes[i]].ToString() != "null" && r[attributes[i]].ToString() != null)
                                                    //            updateDefinition = updateDefinition.Set(attributes[i], DecryptEncrypt(r[attributes[i]].AsString, appSettings));
                                                    //    }
                                                    //}
                                                    collection.UpdateOne(filter, updateDefinition);
                                                }
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {

                                        LogManager.Logger.LogErrorMessage(typeof(Program), nameof(RunOnMonteCarloDB), "Error - forech var r in results" + "CorrelationId -" + corelationId + "- Collection name -" + c.Name + "--ATTRIBUTENAME--", ex, string.Empty, string.Empty, string.Empty, string.Empty);
                                        Console.WriteLine(ex.StackTrace);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                LogManager.Logger.LogErrorMessage(typeof(Program), nameof(RunOnMonteCarloDB), "Error - forech var r in results" + "CorrelationId -" + corelationId + "- Collection name -" + c.Name + "--ATTRIBUTENAME--", ex, string.Empty, string.Empty, string.Empty, string.Empty);
                                Console.WriteLine(ex.StackTrace);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (attributes.Length > 1)
                            LogManager.Logger.LogErrorMessage(typeof(Program), nameof(RunOnMonteCarloDB), "Error -" + "- Collection name -" + c.Name + "--CorrelationID--" + corelationId + "--ATTRIBUTENAME--" + attributes[iteration], ex, string.Empty, string.Empty, string.Empty, string.Empty);
                        else
                            LogManager.Logger.LogErrorMessage(typeof(Program), nameof(RunOnMonteCarloDB), "Error -" + "- Collection name -" + c.Name + "--CorrelationID--" + corelationId + "--ATTRIBUTENAME--" + attributes[0], ex, string.Empty, string.Empty, string.Empty, string.Empty);
                    }
                }

                //Console.WriteLine("Executed collection " + c.Name);
            }
            Console.WriteLine("Updated encryption for MonteCarloDB- " + DateTime.Now.ToString());
            LogManager.Logger.LogProcessInfo(typeof(Program), nameof(RunOnMonteCarloDB), "Updated encryption for MonteCarloDB- Completed", string.Empty, string.Empty, string.Empty, string.Empty);
        }

        private static void RunOnAIServiceDb()
        {
             Console.WriteLine("\n----------------------------------------------------------------------------");;
            LogManager.Logger.LogProcessInfo(typeof(Program), nameof(RunOnAIServiceDb), "Started encryption for AIServices-" + "STARTED -" + DateTime.Now.ToString(), string.Empty, string.Empty, string.Empty, string.Empty);
            Console.WriteLine("Starting encryption for AIServices- " + DateTime.Now.ToString());
            bool flag = true;
            int skip = 0;
            int count = 0;
            while (flag)
            {
                var Collection = _database.GetCollection<BsonDocument>("AIServiceIngestData");
                var Filter = Builders<BsonDocument>.Filter.Empty;
                var modelProjection = Builders<BsonDocument>.Projection.Include("_id").Include("CorrelationId");
                var ModelCount = Collection.Find(Filter).Project<BsonDocument>(modelProjection).ToList().Count;
                var ModelResults = Collection.Find(Filter).Project<BsonDocument>(modelProjection).Skip(skip).Limit(appSettings.ModelsProcessCount).ToList();
                if (ModelResults.Count > 0)
                {
                    Console.WriteLine("AI Services Parallel Count--" + skip + "---StartedON--" + DateTime.Now.ToString());
                    //LogManager.Logger.LogProcessInfo(typeof(Program), nameof(RunOnAIServiceDb), "STARTED -" + DateTime.Now.ToString() + "MODELCOUNT--" + ModelResults.Count);
                    Parallel.ForEach(ModelResults, model =>
                    {
                        int iteration = 0;
                        count++;
                        Console.WriteLine("AIServicesDB executing record " + count + "/" + ModelCount + " CorrelationId: " + model["CorrelationId"].ToString());
                        //LogManager.Logger.LogProcessInfo(typeof(Program), nameof(RunOnAIServiceDb), "COLLECTION STARTED -" + DateTime.Now.ToString());
                        foreach (var c in appSettings.AIServicedbcollections)
                        {
                            try
                            {
                                //LogManager.Logger.LogProcessInfo(typeof(Program), nameof(RunOnAIServiceDb), "AIService Starting for collection-" + "CollectionName -" + c.Name + "--CORRELATIONID---" + model["CorrelationId"].ToString());
                                var attributes = c.Attributes.Split(",");
                                var projection = Builders<BsonDocument>.Projection.Include("_id");
                                foreach (var filedName in attributes)
                                {
                                    projection = projection.Include(filedName);
                                }
                                var collection = _database.GetCollection<BsonDocument>(c.Name);
                                var results = collection.Find(Builders<BsonDocument>.Filter.Eq("CorrelationId", model["CorrelationId"].ToString())).Project<BsonDocument>(projection).ToList();
                                if (results.Count > 0)
                                {
                                    foreach (var r in results)
                                    {
                                        try
                                        {
                                            foreach (var item in attributes)
                                            {                                                
                                                var builder = Builders<BsonDocument>.Filter;
                                                //var key = attributes[0];
                                                string key = item;
                                                try
                                                {
                                                    BsonElement element;
                                                    var exists = r.TryGetElement(key, out element);
                                                    if (exists)
                                                    {
                                                        string fieldType = r[key].GetType().ToString();
                                                        if (fieldType == "MongoDB.Bson.BsonString")
                                                        {
                                                            if (!string.IsNullOrEmpty(Convert.ToString(r[key])) && Convert.ToString(r[key]) != "null")
                                                            {
                                                                var value = DecryptEncrypt(r[key].AsString, appSettings);
                                                                FilterDefinition<BsonDocument> filter = builder.Eq("_id", r["_id"]);
                                                                UpdateDefinition<BsonDocument> updateDefinition = Builders<BsonDocument>.Update.Set<dynamic>(key, value);
                                                                //for (var i = 1; i < attributes.Length; i++)
                                                                //{
                                                                //    iteration = i;
                                                                //    BsonElement elementKey;
                                                                //    var existsKey = r.TryGetElement(attributes[i], out elementKey);
                                                                //    if (existsKey)
                                                                //    {
                                                                //        if (r[attributes[i]].ToString() == "BsonString" && r[attributes[i]].ToString() != "null" && r[attributes[i]].ToString() != null)
                                                                //            updateDefinition = updateDefinition.Set(attributes[i], DecryptEncrypt(r[attributes[i]].AsString, appSettings));
                                                                //    }
                                                                //}
                                                                collection.UpdateOne(filter, updateDefinition);
                                                            }
                                                        }
                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    if (attributes.Length > 1)
                                                        LogManager.Logger.LogErrorMessage(typeof(Program), nameof(RunOnAIServiceDb), "Error -" + "CorrelationId -" + model["CorrelationId"].ToString() + "- Collection name -" + c.Name + "--ATTRIBUTENAME--" + attributes[iteration], ex, string.Empty, string.Empty, string.Empty, string.Empty);
                                                    else
                                                        LogManager.Logger.LogErrorMessage(typeof(Program), nameof(RunOnAIServiceDb), "Error -" + "CorrelationId -" + model["CorrelationId"].ToString() + "- Collection name -" + c.Name + "--ATTRIBUTENAME--" + attributes[0], ex, string.Empty, string.Empty, string.Empty, string.Empty);
                                                }
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            LogManager.Logger.LogErrorMessage(typeof(Program), nameof(RunOnAIServiceDb), "Error - forech var r in results" + "CorrelationId -" + model["CorrelationId"].ToString() + "- Collection name -" + c.Name + "--ATTRIBUTENAME--", ex, string.Empty, string.Empty, string.Empty, string.Empty);
                                            Console.WriteLine(ex.StackTrace);
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {

                                LogManager.Logger.LogErrorMessage(typeof(Program), nameof(RunOnAIServiceDb), "Error - forech var r in results" + "CorrelationId -" + model["CorrelationId"].ToString() + "- Collection name -" + c.Name + "--ATTRIBUTENAME--", ex, string.Empty, string.Empty, string.Empty, string.Empty);
                                Console.WriteLine(ex.StackTrace);
                            }
                        }
                        //LogManager.Logger.LogProcessInfo(typeof(Program), nameof(RunOnClusteringServiceDb), "END -" + DateTime.Now.ToString() + "--CORRELATIONID--" + model["CorrelationId"].ToString());
                    });
                    skip = skip + appSettings.ModelsProcessCount;
                    //Thread.Sleep(1000);
                }
                else
                {
                    flag = false;
                }
            }
            Console.WriteLine("Updated encryption for AIServices- " + DateTime.Now.ToString());
            LogManager.Logger.LogProcessInfo(typeof(Program), nameof(RunOnAIServiceDb), "Updated encryption for AIServicesDB-" + "Completed", string.Empty, string.Empty, string.Empty, string.Empty);
        }

        private static void RunOnClusteringServiceDb()
        {
             Console.WriteLine("\n----------------------------------------------------------------------------");;
            LogManager.Logger.LogProcessInfo(typeof(Program), nameof(RunOnClusteringServiceDb), "Started encryption for ClusteringServices-" + "STARTED -" + DateTime.Now.ToString(), string.Empty, string.Empty, string.Empty, string.Empty);
            Console.WriteLine("Starting encryption for Clustering Services- " + DateTime.Now.ToString());
            var Collection = _database.GetCollection<BsonDocument>("Clustering_IngestData");
            //var Filter = Builders<BsonDocument>.Filter.Eq("DBEncryptionRequired", false);
            var Filter = Builders<BsonDocument>.Filter.Empty;
            var modelProjection = Builders<BsonDocument>.Projection.Include("_id").Include("CorrelationId").Include("DBEncryptionRequired");
            var ModelResults = Collection.Find(Filter).Project<BsonDocument>(modelProjection).ToList();
            int count = 0;
            if (ModelResults.Count > 0)
            {
                foreach (var model in ModelResults)
                {
                    try
                    {
                        count++;
                        Console.WriteLine("ClusteringDB executing record " + count + "/" + ModelResults.Count + " CorrelationId: " + model["CorrelationId"].ToString() + " DBEncryptionRequired: " + Convert.ToBoolean(model["DBEncryptionRequired"]));
                        //LogManager.Logger.LogProcessInfo(typeof(Program), nameof(RunOnClusteringServiceDb), "STARTED -" + DateTime.Now.ToString());
                        int iteration = 0;
                        foreach (var c in appSettings.ClusteringDbColections)
                        {
                            //LogManager.Logger.LogProcessInfo(typeof(Program), nameof(RunOnClusteringServiceDb), "RunOnClusteringServiceDb Starting for collection-" + "CollectionName -" + c.Name);
                            var attributes = c.Attributes.Split(",");
                            var projection = Builders<BsonDocument>.Projection.Include("_id");
                            foreach (var filedName in attributes)
                            {
                                projection = projection.Include(filedName);
                            }
                            var collection = _database.GetCollection<BsonDocument>(c.Name);
                            var results = collection.Find(Builders<BsonDocument>.Filter.Eq("CorrelationId", model["CorrelationId"].ToString())).Project<BsonDocument>(projection).ToList();
                            if (results.Count > 0)
                            {
                                foreach (var r in results)
                                {                                   
                                    foreach (var item in attributes)
                                    {
                                        try
                                        {
                                            var builder = Builders<BsonDocument>.Filter;
                                            //var key = attributes[0];
                                            string key = item;
                                            BsonElement element;
                                            var exists = r.TryGetElement(key, out element);
                                            if (exists)
                                            {
                                                string fieldType = r[key].GetType().ToString();
                                                if (fieldType == "MongoDB.Bson.BsonString")
                                                {
                                                    if (!string.IsNullOrEmpty(Convert.ToString(r[key])) && Convert.ToString(r[key]) != "null")
                                                    {
                                                        var value = DecryptEncrypt(r[key].AsString, appSettings);
                                                        FilterDefinition<BsonDocument> filter = builder.Eq("_id", r["_id"]);
                                                        UpdateDefinition<BsonDocument> updateDefinition = Builders<BsonDocument>.Update.Set<dynamic>(key, value);
                                                        //for (var i = 1; i < attributes.Length; i++)
                                                        //{
                                                        //    iteration = i;
                                                        //    BsonElement elementKey;
                                                        //    var existsKey = r.TryGetElement(attributes[i], out elementKey);
                                                        //    if (existsKey)
                                                        //    {
                                                        //        if (r[attributes[i]].ToString() == "BsonString" && r[attributes[i]].ToString() != "null" && r[attributes[i]].ToString() != null)
                                                        //            updateDefinition = updateDefinition.Set(attributes[i], DecryptEncrypt(r[attributes[i]].AsString, appSettings));
                                                        //    }
                                                        //}
                                                        collection.UpdateOne(filter, updateDefinition);
                                                    }
                                                }
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            if (attributes.Length > 1)
                                            {
                                                LogManager.Logger.LogErrorMessage(typeof(Program), nameof(RunOnClusteringServiceDb), "Error -" + "CorrelationId -" + model["CorrelationId"].ToString() + "- Collection name -" + c.Name + "--ATTRIBUTENAME--" + attributes[iteration], ex, string.Empty, string.Empty, string.Empty, string.Empty);
                                            }
                                            else
                                                LogManager.Logger.LogErrorMessage(typeof(Program), nameof(RunOnClusteringServiceDb), "Error -" + "CorrelationId -" + model["CorrelationId"].ToString() + "- Collection name -" + c.Name + "--ATTRIBUTENAME--" + attributes[0], ex, string.Empty, string.Empty, string.Empty, string.Empty);
                                        }
                                    }
                                }
                            }
                        }
                        //LogManager.Logger.LogProcessInfo(typeof(Program), nameof(RunOnClusteringServiceDb), "END -" + DateTime.Now.ToString());
                    }
                    catch (Exception ex)
                    {
                        LogManager.Logger.LogErrorMessage(typeof(Program), nameof(RunOnClusteringServiceDb), "Error - forech var model in results" + "CorrelationId -" + model["CorrelationId"].ToString(), ex, string.Empty, string.Empty, string.Empty, string.Empty);
                        Console.WriteLine(ex.StackTrace);
                    }
                }
            }
            Console.WriteLine("Updated encryption for Clustering Services- " + DateTime.Now.ToString());
            LogManager.Logger.LogProcessInfo(typeof(Program), nameof(RunOnClusteringServiceDb), "Updated encryption for Clustering Services-" + "Completed", string.Empty, string.Empty, string.Empty, string.Empty);
        }

        private static void RunOnInferenceEngineDb()
        {
             Console.WriteLine("\n----------------------------------------------------------------------------");;
            LogManager.Logger.LogProcessInfo(typeof(Program), nameof(RunOnIngrainDb), "Starting encryption for InferenceEngineDB" + "-STARTED -" + DateTime.Now.ToString(), string.Empty, string.Empty, string.Empty, string.Empty);
            Console.WriteLine("Starting encryption for InferenceEngineDB - " + DateTime.Now.ToString());
            _mongoClient = MongoDataContext.GetDatabaseConnection(appSettings.IEConnectionString, appSettings.certificatePath, appSettings.certificatePassKey);
            string dataBaseName = MongoUrl.Create(appSettings.IEConnectionString).DatabaseName;
            _database = _mongoClient.GetDatabase(dataBaseName);
            var modelsCollection = _database.GetCollection<BsonDocument>("IEModels");
            //var modelsFilter = Builders<BsonDocument>.Filter.Eq("DBEncryptionRequired", false);
            var modelsFilter = Builders<BsonDocument>.Filter.Empty;
            var modelsProjection = Builders<BsonDocument>.Projection.Include("_id").Include("CorrelationId").Include("DBEncryptionRequired");
            var modelsResults = modelsCollection.Find(modelsFilter).Project<BsonDocument>(modelsProjection).ToList();
            int count = 0;
            if (modelsResults.Count > 0)
            {
                foreach (var model in modelsResults)
                {
                    count++;
                    Console.WriteLine("InferenceEngineDB executing record " + count + "/" + modelsResults.Count + " CorrelationId: " + model["CorrelationId"].ToString() + " DBEncryptionRequired: " + Convert.ToBoolean(model["DBEncryptionRequired"]));
                    foreach (var c in appSettings.InfereneEngineDBCollections)
                    {
                        var attributes = c.Attributes.Split(",");
                        var projection = Builders<BsonDocument>.Projection.Include("_id");
                        foreach (var filedName in attributes)
                        {
                            projection = projection.Include(filedName.Trim());
                        }
                        var collection = _database.GetCollection<BsonDocument>(c.Name);
                        var results = collection.Find(Builders<BsonDocument>.Filter.Eq("CorrelationId", model["CorrelationId"].ToString())).Project<BsonDocument>(projection).ToList();
                        if (results.Count > 0)
                        {
                            foreach (var r in results)
                            {
                                foreach (var item in attributes)
                                {
                                    string key = item;
                                    BsonElement element;
                                    var exists = r.TryGetElement(key, out element);
                                    if (exists)
                                    {
                                        try
                                        {
                                            string fieldType = r[key].GetType().ToString();
                                            if (fieldType == "MongoDB.Bson.BsonString")
                                            {
                                                if (!string.IsNullOrEmpty(Convert.ToString(r[key])) && Convert.ToString(r[key]) != "null")
                                                {
                                                    var value = DecryptEncrypt(r[key].AsString, appSettings);
                                                    var builder = Builders<BsonDocument>.Filter;
                                                    FilterDefinition<BsonDocument> filter = builder.Eq("_id", r["_id"]);
                                                    UpdateDefinition<BsonDocument> updateDefinition = Builders<BsonDocument>.Update.Set<dynamic>(key, value);
                                                    //for (var i = 1; i < attributes.Length; i++)
                                                    //{
                                                    //    iteration = i;
                                                    //    BsonElement elementKey;
                                                    //    var existsKey = r.TryGetElement(attributes[i], out elementKey);
                                                    //    if (existsKey)
                                                    //    {
                                                    //        if (r[attributes[i]].ToString() == "BsonString" && r[attributes[i]].ToString() != "null" && r[attributes[i]].ToString() != null)
                                                    //            updateDefinition = updateDefinition.Set(attributes[i], DecryptEncrypt(r[attributes[i]].AsString, appSettings));
                                                    //    }
                                                    //}
                                                    collection.UpdateOne(filter, updateDefinition);
                                                }
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            if (attributes.Length > 1)
                                                LogManager.Logger.LogErrorMessage(typeof(Program), nameof(RunOnIngrainDb), "Error -" + "CorrelationId -" + model["CorrelationId"].ToString() + "- Collection name -" + c.Name + "--ATTRIBUTENAME--", ex, string.Empty, string.Empty, string.Empty, string.Empty);
                                            else
                                                LogManager.Logger.LogErrorMessage(typeof(Program), nameof(RunOnIngrainDb), "Error -" + "CorrelationId -" + model["CorrelationId"].ToString() + "- Collection name -" + c.Name + "--ATTRIBUTENAME--" + attributes[0], ex, string.Empty, string.Empty, string.Empty, string.Empty);
                                        }

                                    }
                                }
                            }
                        }
                    }
                }
            }

            Console.WriteLine("Updated encryption for InferenceEngineDB - " + DateTime.Now.ToString());
            LogManager.Logger.LogProcessInfo(typeof(Program), nameof(RunOnIngrainDb), "Updated encryption for InferenceEngineDB Completed", string.Empty, string.Empty, string.Empty, string.Empty);
        }

        static string DecryptEncrypt(string encryptedValue, AppSettings o)
        {
            string dValue = null;
            try
            {
                //Decrypt with Old Key
                dValue = o.IsAESKeyVault ? CryptographyUtility.Decrypt(encryptedValue) : AesProvider.Decrypt(encryptedValue, o.aesKey, o.aesVector);
            }
            catch (Exception ex)
            {
                LogManager.Logger.LogErrorMessage(typeof(Program), nameof(RunOnClusteringServiceDb), "Error - Data not encrypted or Encryption failed with wrong Key/Vector", ex, "EncryptedValue: " + encryptedValue, string.Empty, string.Empty, string.Empty);
                dValue = encryptedValue;
            }

            //Encrypt with New Key
            return AesProvider.Encrypt(dValue, o.aesKeyNew, o.aesVectorNew);
            //var dValue = o.IsAESKeyVault? CryptographyUtility.Decrypt(encryptedValue) : AesProvider.Decrypt(encryptedValue, o.aesKey, o.aesVector);
            //return AesProvider.Encrypt(dValue, o.aesKeyNew, o.aesVectorNew);
        }

        #endregion

        #region Class
        public static class AppSettingsJson
        {
            public static IConfigurationRoot GetAppSettings()
            {
                var repo = log4net.LogManager.CreateRepository(Assembly.GetEntryAssembly(),
                 typeof(log4net.Repository.Hierarchy.Hierarchy));
                var assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                Console.WriteLine("Assembly Folder -" + assemblyFolder);
                log4net.Config.XmlConfigurator.Configure(repo, new FileInfo(Path.Combine(assemblyFolder, "log4net.config")));

                LogManager.SetLogFolderPath(string.Empty);
                string applicationExeDirectory = ApplicationExeDirectory();

                var builder = new ConfigurationBuilder()
                .SetBasePath(applicationExeDirectory)
                .UseCryptography("appsettings")
                .AddJsonFile("appsettings.json");

                return builder.Build();
            }

            private static string ApplicationExeDirectory()
            {
                var location = System.Reflection.Assembly.GetExecutingAssembly().Location;
                Console.WriteLine("Assembly Location -" + location);
                var appRoot = Path.GetDirectoryName(location);

                return appRoot;
            }
        }
        #endregion
    }
}
