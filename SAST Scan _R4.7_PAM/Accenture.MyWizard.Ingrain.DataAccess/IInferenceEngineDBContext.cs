using Accenture.MyWizard.Ingrain.DataAccess.InferenceEntities;
using MongoDB.Driver;

namespace Accenture.MyWizard.Ingrain.DataAccess
{
    public interface IInferenceEngineDBContext
    {
        IERequestQueueRepository IERequestQueueRepository { get; }

        IEAppIngerationRepository IEAppIngerationRepository { get; }

        IEModelRepository IEModelRepository { get; }


        IEConfigTemplateRepository IEConfigTemplateRepository { get; }
        InferenceConfigRepository InferenceConfigRepository { get; }
        IEUseCaseRepository IEUseCaseRepository { get; }
        IEAutoReTrainRepository IEAutoReTrainRepository { get; }

    }
}
