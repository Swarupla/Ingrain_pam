using Accenture.MyWizard.Ingrain.DataModels.InferenceEngineModel;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.DataAccess.InferenceEntities
{
    public class IEUseCaseRepository
    {
        private IMongoDatabase _database;
        public string IEUseCaseCollection { get; set; }        
        public IEUseCaseRepository(IMongoDatabase database)
        {
            _database = database;
            IEUseCaseCollection = "IE_UseCase";
        }


        public void InsertUseCase(IEUseCase iEUseCase)
        {
            var collection = _database.GetCollection<IEUseCase>(IEUseCaseCollection);
            collection.InsertOne(iEUseCase);
        }


        public IEUseCase GetUseCase(string useCaseId)
        {
            var collection = _database.GetCollection<IEUseCase>(IEUseCaseCollection);
            var filter = Builders<IEUseCase>.Filter.Where(x => x.UseCaseId == useCaseId);
            var projection = Builders<IEUseCase>.Projection.Exclude("_id");
            var result = collection.Find(filter).Project<IEUseCase>(projection).FirstOrDefault();
            return result;
            
        }

        public IEUseCase GetUseCaseByCorrelationId(string correlationId)
        {
            var collection = _database.GetCollection<IEUseCase>(IEUseCaseCollection);
            var filter = Builders<IEUseCase>.Filter.Where(x => x.CorrelationId == correlationId);
            var projection = Builders<IEUseCase>.Projection.Exclude("_id");
            var result = collection.Find(filter).Project<IEUseCase>(projection).FirstOrDefault();
            return result;

        }

        public void DeleteUseCase(string useCaseId)
        {
            var collection = _database.GetCollection<IEUseCase>(IEUseCaseCollection);
            var filter = Builders<IEUseCase>.Filter.Where(x => x.UseCaseId == useCaseId);
            collection.DeleteOne(filter);
        }


        public List<IEUseCase> GetAllUseCases()
        {
            var collection = _database.GetCollection<IEUseCase>(IEUseCaseCollection);
            var filter = Builders<IEUseCase>.Filter.Empty;
            var projection = Builders<IEUseCase>.Projection.Exclude("_id");
            var result = collection.Find(filter).Project<IEUseCase>(projection).ToList();
            return result;

        }

        public void UpdateUsecaseFlag(IEUseCase usecase)
        {
            var collection = _database.GetCollection<IEUseCase>(IEUseCaseCollection);
            var builder = Builders<IEUseCase>.Filter;
            var filter = builder.Where(x => x.CorrelationId == usecase.CorrelationId);
            var update = Builders<IEUseCase>.Update.Set("isSavedConfigEncrypted", "no");
            collection.UpdateOne(filter, update);
        }


    }
}
