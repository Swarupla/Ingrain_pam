import { Injectable, OnInit } from '@angular/core';
import { ServiceTypes } from '../_enums/service-types.enum';
import { ApiService } from './api.service';
// import { template } from '@angular/core/src/render3';

@Injectable({
  providedIn: 'root'
})
export class DeployModelService implements OnInit {

  templateName: String;
  templateflag: boolean;
  ConfigStorageKey = 'CustomConfiguration';

  constructor(private api: ApiService) {
  }

  ngOnInit() {
    this.templateflag = false;
  }

  getTrainedModel(correlationId) {
    return this.api.get('GetPublishModel', { 'correlationId': correlationId });
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
  postDeployModels(correlationId, saveselectedVersion, saveSelectedApps, saveTypeOfDeployModel,
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
    return this.api.post('PostDeployModel', data);
  }

  getIsModelDeployed(correlationId) {
    return this.api.get('GetDeployedModel', { 'correlationId': correlationId });
  }

  certificateForWorkshop(username, useremail) {
    return this.api.post('CertificateUserEmail', '', { 'userName': username, 'userEmail': useremail });
  }

  // code to save custom configuration Region - starts.
  saveCustomConfiguration(data, serviceLevel) {
    const trainingCustomConfig = JSON.parse(sessionStorage.getItem(`${ServiceTypes.Training}${this.ConfigStorageKey}`));
    const predictionCustomConfig = JSON.parse(sessionStorage.getItem(`${ServiceTypes.Prediction}${this.ConfigStorageKey}`));
    const reTrainingCustomConfig = JSON.parse(sessionStorage.getItem(`${ServiceTypes.ReTraining}${this.ConfigStorageKey}`));
    const param = { ServiceLevel: serviceLevel };
    const payload = {
      "ApplicationID": data.ApplicationID,
      "CorrelationID": data.CorrelationID,
      "userId": data.userId,
      "clientUID": data.clientUID,
      "deliveryUID": data.deliveryUID,
      "ModelName": data.ModelName,
      "ModelVersion": data.ModelVersion,
      "ModelType": data.ModelType,
      "DataSource": data.DataSource,
      "ServiceId" : data.ServiceId,
      "UsecaseName": data.UsecaseName,
      "TemplateUseCaseID": data.TemplateUseCaseID,
      "UseCaseID" : data.UseCaseID,

      "Training": trainingCustomConfig ? { "SelectedConstraints": trainingCustomConfig } : {},
      "Prediction": predictionCustomConfig ? { "SelectedConstraints": predictionCustomConfig } : {},
      "Retraining": reTrainingCustomConfig ? { "SelectedConstraints": reTrainingCustomConfig } : {}
    }

    return this.api.post('PostCustomConfiguration', payload, param);
  }
  // code to save custom configuration Region - ends.

  //clearing local storage data of custom config Region - starts.
  clearCustomConfigStorage() {
    sessionStorage.removeItem(`${ServiceTypes.Training}${this.ConfigStorageKey}`);
    sessionStorage.removeItem(`${ServiceTypes.Prediction}${this.ConfigStorageKey}`);
    sessionStorage.removeItem(`${ServiceTypes.ReTraining}${this.ConfigStorageKey}`);
  }
  //clearing local storage data of custom config Region - ends.
}
