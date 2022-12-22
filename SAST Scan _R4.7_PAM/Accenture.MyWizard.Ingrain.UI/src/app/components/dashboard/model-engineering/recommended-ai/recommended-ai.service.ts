import { Injectable } from '@angular/core';
import { ApiService } from 'src/app/_services/api.service';

@Injectable({
  providedIn: 'root'
})
export class RecommendedAiService {

  constructor(private _apiService: ApiService) { }

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

  retrainTrainedModel(correlationId) {
    return this._apiService.delete('DeleteTrainedModel', { 'correlationId': correlationId });
  }

}
