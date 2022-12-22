using Accenture.MyWizard.Ingrain.DataModels.InferenceEngineModel;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.DataAccess.InferenceEntities
{
    public class IEConfigTemplateRepository
    {
        private IMongoDatabase _database;
        public IEConfigTemplateRepository(IMongoDatabase database)
        {
            _database = database;
        }


        public List<InferenceConfigTemplate> GetConfigTemplates(string environment, string sourceName, string functionalArea,  string entityName)
        {
            var collection = _database.GetCollection<InferenceConfigTemplate>("IE_DefaultConfigTemplates");
            var filter = Builders<InferenceConfigTemplate>.Filter.Where(x => x.Environment == environment)
                         & Builders<InferenceConfigTemplate>.Filter.Where(x => x.Source == sourceName);
            filter = !string.IsNullOrEmpty(functionalArea) ? filter & Builders<InferenceConfigTemplate>.Filter.Where(x => x.FunctionalArea == functionalArea) : filter;
            filter = !string.IsNullOrEmpty(entityName) ? filter & Builders<InferenceConfigTemplate>.Filter.Where(x => x.EntityName == entityName) : filter;
            var projection = Builders<InferenceConfigTemplate>.Projection.Exclude("_id");
            return collection.Find(filter).Project<InferenceConfigTemplate>(projection).ToList();
        }



        public IEDefaultConfigTemplateResults GetDefaultConfigTemplateResult(string templateId, string configType)
        {
            var collection = _database.GetCollection<IEDefaultConfigTemplateResults>("IE_DefaultConfigTemplateResults");
            var filter = Builders<IEDefaultConfigTemplateResults>.Filter.Where(x => x.DefaultTemplateId == templateId)
                         & Builders<IEDefaultConfigTemplateResults>.Filter.Where(x => x.InferenceConfigType == configType);
            var projection = Builders<IEDefaultConfigTemplateResults>.Projection.Exclude("_id");
            return collection.Find(filter).Project<IEDefaultConfigTemplateResults>(projection).FirstOrDefault();
        }
    }
}
