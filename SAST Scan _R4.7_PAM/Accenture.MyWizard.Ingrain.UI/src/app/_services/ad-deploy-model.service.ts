import { Injectable } from '@angular/core';
import { ApiService } from './api.service';

@Injectable({
  providedIn: 'root'
})
export class AdDeployModelService {
  templateName: String;
  templateflag: boolean;

  constructor(private api : ApiService) { }

  getTrainedModelAD(correlationId) {
    return this.api.get('GetPublishModelAD', { 'correlationId': correlationId });
  }

  setTemplateName(templateName: any) {
    this.templateflag = true;
    this.templateName = templateName;
  }

  getTemplateName() {
    if (this.templateflag === true) {
      this.templateflag = false;
      return this.templateName;
    }
  }
  // , saveTypeOfDeployModel, saveServiceType, //'WebServices': saveServiceType, // 'WebServices': saveServiceType,
  postDeployModelsAD(correlationId, saveselectedVersion, saveSelectedApps, saveTypeOfDeployModel,
    accuracy, isPrivate, publicTemplateName, userId, modelname, problemType, selectedModelFrequency, appId, isModelTemplate,
    provideTrainingDataVolume, isOfflineSelected, isOnlineSelected, selectedOfflineRetrain, trainingObj, predictionObj, retraining,
    archivalMonth) {
    let data = {};
    if (problemType === 'TimeSeries') {
      data = {
        'correlationId': correlationId,
        'LinkedApps': saveSelectedApps,
        'ModelVersion': saveselectedVersion,
        'DeployModel': saveTypeOfDeployModel,
        'IsPrivate': isPrivate,
        'userid': userId,
        'ModelName': modelname,
        'ModelType': problemType,
        'Category': publicTemplateName,
        'Frequency': selectedModelFrequency,
        'AppId': appId,
        'IsModelTemplate': isModelTemplate,
        'MaxDataPull': provideTrainingDataVolume,
        'IsCarryOutRetraining': selectedOfflineRetrain,
        "IsOnline": isOnlineSelected,
        "IsOffline": isOfflineSelected,
        'Training': trainingObj,
        'Prediction': predictionObj,
        'Retraining': retraining,
        'ArchivalDays': archivalMonth
      };
    } else {
      data = {
        'correlationId': correlationId,
        'Accuracy': accuracy,
        'LinkedApps': saveSelectedApps,
        'ModelVersion': saveselectedVersion,
        'DeployModel': saveTypeOfDeployModel,
        'IsPrivate': isPrivate,
        'Category': publicTemplateName,
        'userid': userId,
        'ModelName': modelname,
        'ModelType': problemType,
        'AppId': appId,
        'IsModelTemplate': isModelTemplate,
        'MaxDataPull': provideTrainingDataVolume,
        'IsCarryOutRetraining': selectedOfflineRetrain,
        "IsOnline": isOnlineSelected,
        "IsOffline": isOfflineSelected,
        'Training': trainingObj,
        'Prediction': predictionObj,
        'Retraining': retraining,
        'ArchivalDays': archivalMonth
      };
    }
    return this.api.post('PostDeployModelAD', data);
  }

  getIsModelDeployedAD(correlationId) {
    return this.api.get('GetDeployedModelAD', { 'correlationId': correlationId });
  }
}
