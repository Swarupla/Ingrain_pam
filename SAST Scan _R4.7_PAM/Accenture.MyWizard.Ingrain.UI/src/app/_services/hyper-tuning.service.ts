import { Injectable } from '@angular/core';
import { ApiService } from '../_services/api.service';

@Injectable({
  providedIn: 'root'
})
export class HyperTuningService {

  constructor(private _apiService: ApiService) { }

  getHyperTuningData(modelName, correlationId) {
    return this._apiService.get('GetHyperTuneData', { modelName: modelName, correlationId: correlationId });
  }

  hyperTuningStartTrainring(correlationId, htId, modelName, userId, pageInfo) {
    return this._apiService.get('HyperTuningStartTraining', {
      'correlationId': correlationId, 'hyperTuneId': htId, 'userId': userId, 'modelName': modelName, 'pageInfo': pageInfo
    });
  }

  saveHyperTuningVersion(correlationId, HTId, temp, modelName) {
    const postDataToSave = {
      'CorrelationId': correlationId,
      'HTId': HTId,
      'Temp': temp,
      'VersionName': modelName
    };
    return this._apiService.post('SaveHyperTuningVersion', postDataToSave);
  }
  postHyperTuning(correlationId, modelParams) {
    const data = {
      'CorrelationId': correlationId,
      'ModelParams': modelParams,
      'Temp': 'False',
      'VersionName': null
    };
    return this._apiService.post('PostHyperTuning', data);
  }

}
