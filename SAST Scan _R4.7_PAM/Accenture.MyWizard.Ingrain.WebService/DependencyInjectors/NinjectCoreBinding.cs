using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ninject.Modules;
using Ninject;
using Accenture.MyWizard.Ingrain.BusinessDomain;
using Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces;
using Accenture.MyWizard.Ingrain.BusinessDomain.Services;

namespace Accenture.MyWizard.Ingrain.WebService
{
    public class NinjectCoreBinding : NinjectModule
    {
        #region Members

        private static IKernel _kernal = null;

        #endregion

        public static IKernel NinjectKernel
        {
            get
            {
                return _kernal;
            }
            set
            {
                _kernal = value;
            }
        }

       
        //StandardKernel kernel;
        //public NinjectCoreBinding(StandardKernel kernel)
        //{
        //    this.kernel = kernel;
        //}

        public override void Load()
        {
            Bind<IIngestedData>().To<IngestedDataService>();
            Bind<ILoginService>().To<LoginService>();
            Bind<IProcessDataService>().To<ProcessDataService>();
            Bind<ICloneService>().To<CloneDataService>();
            Bind<IFlushService>().To<FlushModelService>();
            Bind<IPhoenixTokenService>().To<PhoenixTokenService>();
            Bind<IScopeSelectorService>().To<ScopeSelectorService>();
            Bind<IModelEngineering>().To<ModelEngineeringService>();
            Bind<IDeployedModelService>().To<DeployModelServices>();
            Bind<IBusinessService>().To<BusinessService>();
            Bind<IDataTransformation>().To<DataTransformationService>();
            Bind<IVdsService>().To<VDSDataService>();
            Bind<IInstaModel>().To<InstaModelService>();
            Bind<IMCSimulationService>().To<MCSimulationService>();
            Bind<ISPAVelocityService>().To<SPAVelocityService>();
            Bind<ICascadingService>().To<CascadingService>();
            Bind<IWorkerService>().To<WorkerService>();
            Bind<IFlaskAPI>().To<FlaskAPIService>();
            Bind<ICustomConfigService>().To<CustomConfigService>();
            Bind<ITrainedModelService>().To<TrainedModelService>();
        }
    }
}
