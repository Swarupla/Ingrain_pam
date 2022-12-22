//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Ninject.Modules;
//using Ninject;
////using Accenture.MyWizard.SelfServiceAI.BusinessDomain;
////using Accenture.MyWizard.SelfServiceAI.BusinessDomain.Interfaces;
////using Accenture.MyWizard.SelfServiceAI.BusinessDomain.Services;

//namespace Accenture.MyWizard.Shared
//{
//    public class NinjectCoreBinding1 : NinjectModule
//    {
//        #region Members

//        private static Ninject.IKernel _kernal = null;

//        #endregion

//        #region Properties       
//        /// <summary>
//        /// Gets or sets the ninject kernel.
//        /// </summary>
//        /// <value>
//        /// The ninject kernel.
//        /// </value>
//        public static Ninject.IKernel NinjectKernel
//        {
//            get
//            {
//                return _kernal;
//            }
//            set
//            {
//                _kernal = value;
//            }
//        }

//        #endregion

//        public override void Load()
//        {

//            //Bind<IIngestedData>().To<IngestedDataService>();
//            //Bind<ILoginService>().To<LoginService>();
//            //Bind<IProcessDataService>().To<ProcessDataService>();
//            //Bind<ICloneService>().To<CloneDataService>();
//            //Bind<IFlushService>().To<FlushModelService>();
//            //Bind<IPhoenixTokenService>().To<PhoenixTokenService>();
//            //Bind<IScopeSelectorService>().To<ScopeSelectorService>();
//            //Bind<IModelEngineering>().To<ModelEngineeringService>();
//            //Bind<IDeployedModelService>().To<DeployModelServices>();
//            //Bind<IBusinessService>().To<BusinessService>();
//            //Bind<IHyperTune>().To<HyperTuneService>();
//            //Bind<IDataTransformation>().To<DataTransformationService>();
//            //Bind<IVdsService>().To<VDSDataService>();
//            //Bind<IInstaModel>().To<InstaModelService>();
//        }
//    }
//}
