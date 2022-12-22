using Accenture.MyWizard.Ingrain.DataModels;
using Accenture.MyWizard.Ingrain.DataModels.InstaModels;
using Accenture.MyWizard.Ingrain.DataModels.Models;

namespace Accenture.MyWizard.Ingrain.BusinessDomain.Interfaces
{
    public interface IInstaModel
    {
        bool IsDataCurationComplete(string correlationId);
        bool CreatePreprocess(string correlationId, string userId, string problemType, string instaId);
        bool IsDataTransformationComplete(string correlationId);        
        //bool IsDeployModelComplete(string correlaionId, RecommedAITrainedModel trainedModel, string problemType);
        void InsertBusinessProblem(IngestModel timeSeriesModel);
        void InsertBusinessProblem(TimeSeriesModel timeSeriesModel);
        string GetFitDate(string correlationId, string instaId);
        string GetModelStatus(string instaId, string correlationId);
        RecommedAITrainedModel GetRecommendedTrainedModels(string correlationId);
        //void UpdateRecommendedModels(string correlationId);
        void StartDataEngineering(string instaId, string correlationId, string userId, string ProblemType,string useCaseID);
        //void StartModelEngineering(string instaId, string correlationId, string userId, string ProblemType, string useCaseId);
        DataEngineering GetDataCuration(string correlationId, string pageInfo, string userId);
        DataEngineering GetDatatransformation(string correlationId, string pageInfo, string userId);
        //InstaModel DeployModel(string instaId, string correlationId, string userId, string ProblemType);
        RecommedAITrainedModel GetTrainedModel(string correlationId, string problemType);        
        InstaPrediction Prediction(string instaId, string correlationId, string userId, string ProblemType, string ActualData);
        InstaModel DeleteModel(string instaId, string correlationId, string userId);
        InstaPrediction RefitModel(VdsData vdsData);
        InstaModel ModelStatus(string instaId, string correlationId);
        string VDSSecurityTokenForPAD();
        string VDSSecurityTokenForPAM();
        string VDSSecurityTokenForManagedInstance();    
        void InsertRequests(IngrainRequestQueue ingrainRequest);
        void IngestData(string data, out InstaModel timeSeriesModel, out InstaRegression regressionModel);
        InstaModel StartModelTraining(VdsData data);
        InstaRegression StartModelTraining(VDSRegression vDSRegressionModel);
        InstaRegression GetRegressionPrediction(VDSRegression regressionModel);
        InstaModel UpdateModel(string correlationId, string modelName, string modelDescription);
        InstaRegression RegressionRefitModel(VDSRegression regressionModel);
        InstaRegression RegressionModelStatus(VDSRegression vdsRegression);
        string GetInstaMLData(string correlationId);
        string GetEnvironment();
    }
}
