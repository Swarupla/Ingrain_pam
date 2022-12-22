using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;

namespace Accenture.MyWizard.Ingrain.DataModels.Models
{
    public class SSAICustomConfiguration
    {
        private bool _IsTrainingEnabled = false;
        private bool _IsPredictionEnabled = false;
        private bool _IsRetrainingEnabled = false;
        public string _id { get; set; }
        public string CorrelationID { get; set; }
        public string UseCaseID { get; set; }
        public string ApplicationID { get; set; }
        public string ModelVersion { get; set; }
        public string ModelType { get; set; }
        public bool IsTrainingEnabled 
        { 
            get => _IsTrainingEnabled; 
            set => _IsTrainingEnabled = value; 
        }
        public SelectedConfiguration Training { get; set; }
        public bool IsPredictionEnabled
        {
            get => _IsPredictionEnabled;
            set => _IsPredictionEnabled = value;
        }
        public SelectedConfiguration Prediction { get; set; }
        public bool IsRetrainingEnabled
        {
            get => _IsRetrainingEnabled;
            set => _IsRetrainingEnabled = value;
        }
        public SelectedConfiguration Retraining { get; set; }
        public string CreatedOn { get; set; }
        public string CreatedByUser { get; set; }
        public string ModifiedOn { get; set; }
        public string ModifiedByUser { get; set; }
    }
    public class SelectedConfiguration
    {
        public string[] SelectedConstraints { get; set; }
    }

    public class MasterConfigurationDTO
    {
        public string ApplicationName { get; set; }
        public ConstraintsDTO[] Constraints { get; set; }
    }

    public class ConstraintsDTO
    {
        public string ConstraintCode { get; set; }
        public string ConstraintName { get; set; }
        public string Condition { get; set; }
        public dynamic ServiceLevel { get; set; }

    }

    public class TrainingRequestDTO
    {
        public string UseCaseId { get; set; }
        public string ClientUID { get; set; }
        public string ApplicationId { get; set; }
        public string ProvisionedAppServiceUID { get; set; }
        public string UserID { get; set; }
        public string DeliveryConstructUID { get; set; }
        public string ResponseCallbackUrl { get; set; }
        public dynamic QueryData { get; set; }
        public dynamic TeamAreaUId { get; set; }
        public string IsTeamLevelData { get; set; }
        public string RetrainRequired { get; set; }
        public string IsAmbulanceLane { get; set; }
    }

    public class GenericTraining
    {
        public string CorrelationId { get; set; }
        public string Message { get; set; }
        public string ErrorMessage { get; set; }
        public string Status { get; set; }
        public long DataPointsCount { get; set; }
        public string DataPointsWarning { get; set; }

    }

    public class AICustomConfiguration
    {
        private bool _IsTrainingEnabled = false;
        private bool _IsPredictionEnabled = false;
        private bool _IsRetrainingEnabled = false;
        public string _id { get; set; }
        public string CorrelationID { get; set; }
        public string UseCaseID { get; set; }
        public string TemplateUseCaseID { get; set; }
        public string ApplicationID { get; set; }
        public bool IsTrainingEnabled
        {
            get => _IsTrainingEnabled;
            set => _IsTrainingEnabled = value;
        }
        public SelectedConfiguration Training { get; set; }
        public bool IsPredictionEnabled
        {
            get => _IsPredictionEnabled;
            set => _IsPredictionEnabled = value;
        }
        public SelectedConfiguration Prediction { get; set; }
        public bool IsRetrainingEnabled
        {
            get => _IsRetrainingEnabled;
            set => _IsRetrainingEnabled = value;
        }
        public SelectedConfiguration Retraining { get; set; }
        public string CreatedOn { get; set; }
        public string CreatedByUser { get; set; }
        public string ModifiedOn { get; set; }
        public string ModifiedByUser { get; set; }
    }

    public class AIGenericTrainingResponse
    {
        public string ApplicationId { get; set; }
        public string ClientId { get; set; }
        public string DeliveryConstructId { get; set; }
        public string ServiceId { get; set; }
        public string UsecaseId { get; set; }
        public string ModelName { get; set; }
        public string UserId { get; set; }
        public string CorrelationId { get; set; }
        public string ModelStatus { get; set; }
        public string StatusMessage { get; set; }
        public string Response { get; set; }

        public AIGenericTrainingResponse(string clientId,
            string deliveryConstructId,
            string serviceId,
            string usecaseId,
            string modelName,
            string userId)
        {
            ClientId = clientId;
            DeliveryConstructId = deliveryConstructId;
            ServiceId = serviceId;
            UsecaseId = usecaseId;
            ModelName = modelName;
            UserId = userId;

        }
    }

}
