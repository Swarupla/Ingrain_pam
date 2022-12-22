using Accenture.MyWizard.Ingrain.DataModels.InferenceEngineModel;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.DataAccess.InferenceEntities
{
    public class IEAutoReTrainRepository
    {
        private IMongoDatabase _database;
        public string IEAutoReTrainCollection { get; set; }
        public IEAutoReTrainRepository(IMongoDatabase database)
        {
            _database = database;
            IEAutoReTrainCollection = "IE_AutoReTrainTask";
        }



        public IEReTrainTask GetTaskDetails(string taskCode)
        {
            var collection = _database.GetCollection<IEReTrainTask>(IEAutoReTrainCollection);
            var filter = Builders<IEReTrainTask>.Filter.Where(x => x.TaskCode == taskCode);
            var projection = Builders<IEReTrainTask>.Projection.Exclude("_id");
            return collection.Find(filter).Project<IEReTrainTask>(projection).FirstOrDefault();
        }
    }
}
