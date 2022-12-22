using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
using Accenture.MyWizard.Ingrain.BusinessDomain.Services;
using Accenture.MyWizard.Ingrain.DataAccess;
using Accenture.MyWizard.Ingrain.WebService.Controllers;
using Accenture.MyWizard.SelfServiceAI.BusinessDomain.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Accenture.MyWizard.Ingrain.WebService
{
    public static class Utility
    {
        public static void LoadAssemblyDependencies(IServiceCollection services)
        {

            services.AddTransient<DatabaseProvider>();
            services.AddTransient<MonteCarloConnection>();
            services.AddTransient<IBusinessService, BusinessService>();
            services.AddTransient<IIngestedData, IngestedDataService>();
            services.AddTransient<ILoginService, LoginService>();
            services.AddTransient<IProcessDataService, ProcessDataService>();
            services.AddTransient<ICloneService, CloneDataService>();
            services.AddTransient<IFlushService, FlushModelService>();
            services.AddTransient<IPhoenixTokenService, PhoenixTokenService>();
            services.AddTransient<IScopeSelectorService, ScopeSelectorService>();
            services.AddTransient<IModelEngineering, ModelEngineeringService>();
            services.AddTransient<IDeployedModelService, DeployModelServices>();
            services.AddTransient<IBusinessService, BusinessService>();
            services.AddTransient<IHyperTune, HyperTuneService>();
            services.AddTransient<IDataTransformation, DataTransformationService>();
            services.AddTransient<IVdsService, VDSDataService>();
            services.AddTransient<IInstaModel, InstaModelService>();
            services.AddTransient<IAssetUsageTrackingData, AssetUsageTrackingService>();
            services.AddTransient<IAICoreService, AICoreService>();
            services.AddTransient<IGenericSelfservice, GenericSelfservice>();
            services.AddTransient<IEncryptionDecryption, EncryptionDecryptionService>();
            services.AddTransient<IMCSimulationService, MCSimulationService>();
            services.AddTransient<IClusteringAPIService, ClusteringAPIService>();
            services.AddTransient<IModelMonitorService, ModelMonitorService>();
            services.AddTransient<IAIModelPredictionsService, AIModelPredictionsService>();
            services.AddTransient<ISPAVelocityService, SPAVelocityService>();
            services.AddTransient<IIAService, IAService>();
            services.AddTransient<ICascadingService, CascadingService>();
            services.AddTransient<IDataSetsService, DataSetsService>();
            services.AddTransient<IInferenceService, InferenceService>();
            services.AddTransient<IInferenceEngineDBContext, InferenceEngineDBContext>();
            services.AddTransient<IWorkerService, WorkerService>();
            services.AddTransient<IFlaskAPI, FlaskAPIService>();
            services.AddTransient<ICustomDataService, CustomDataService>();
            services.AddTransient<ICustomConfigService, CustomConfigService>();
            services.AddTransient<ITrainedModelService, TrainedModelService>();
            services.AddTransient<ISAASService, SAASService>();
            services.AddTransient<IDBService, DBService>();
            services.AddTransient<PhoenixConnection>();
            services.AddTransient<IAnomalyDetection, AnomalyDetectionServices>();
            services.AddScoped<AuthorizePAM>();
            services.AddHttpContextAccessor();
        }
    }
}
