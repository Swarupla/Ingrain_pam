import { Injectable } from '@angular/core';
import { ApiService } from 'src/app/_services/api.service';
import { AppUtilsService } from 'src/app/_services/app-utils.service';
import { Subscription } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class RecommendedAiService {

  subscription: Subscription;
  userId;
  deliveryConstructUID;
  clientUId;
  userCookie;

  constructor(private _apiService: ApiService, private aus: AppUtilsService) {
    this.subscription = this.aus.getParamData().subscribe(paramData => {
      this.clientUId = paramData.clientUID;
      this.deliveryConstructUID = paramData.deliveryConstructUId;
    });
   }

  getRecommendedAI(correlationId) {
    return this._apiService.get('GetRecommendedAI', { 'correlationId': correlationId });
  }

  saveModelPrefrences(trainningModelToSave: {}) {
    return this._apiService.post('PostRecommendedAI', trainningModelToSave);
  }

  getRecommendedTrainedModels(correlationId, userId, pageInfo, noOfModelsSelected) {
    return this._apiService.get('GetRecommendedTrainedModels', {
      'correlationId': correlationId,
      'userId': userId, 'pageInfo': pageInfo, 'noOfModelsSelected': noOfModelsSelected
    });
  }

  retrainTrainedModel(correlationId, userId) {
    return this._apiService.get('StartRetrainModels', { 'correlationId': correlationId, 'userId': userId });
  }

  ValidateInput(correlationId, pageInfo) {
    this.userCookie = this.aus.getCookies();
    this.userId = this.userCookie.UserId;
      return this._apiService.get('ValidateInput', {
        'correlationId': correlationId,
        'pageInfo': pageInfo/* ,
        'userId': this.userId,
        'deliveryConstructUID': this.deliveryConstructUID,
        'clientUId': this.clientUId,
        'isTemplateModel': false */
      });
  }
}
