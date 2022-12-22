import { Injectable } from '@angular/core';
import { ApiService } from './api.service';
import { AppUtilsService } from './app-utils.service';

@Injectable({
  providedIn: 'root'
})
export class AdRecommendedService {
  userId;
  userCookie;

  constructor(private _apiService : ApiService,private aus: AppUtilsService) { }

  getRecommendedAIAD(correlationId) {
    return this._apiService.get('GetRecommendedAIAD', { 'correlationId': correlationId });
  }

  saveModelPrefrences(trainningModelToSave: {}) {
    return this._apiService.post('PostRecommendedAIAD', trainningModelToSave);
  }

  getRecommendedTrainedModelsAD(correlationId, userId, pageInfo, noOfModelsSelected) {
    return this._apiService.get('GetRecommendedTrainedModelsAD', {
      'correlationId': correlationId,
      'userId': userId, 'pageInfo': pageInfo, 'noOfModelsSelected': noOfModelsSelected
    });
  }

  retrainTrainedModelAD(correlationId) {
    return this._apiService.delete('DeleteTrainedModelAD', { 'correlationId': correlationId });
  }

  ValidateInput(correlationId, pageInfo) {
    this.userCookie = this.aus.getCookies();
    this.userId = this.userCookie.UserId;
      return this._apiService.get('ValidateInput', {
        'correlationId': correlationId,
        'pageInfo': pageInfo
      });
  }

  retrainTrainedModel(correlationId, userId) {
    return this._apiService.get('StartRetrainModelsAD', { 'correlationId': correlationId, 'userId': userId });
  }
}
