import { Injectable } from '@angular/core';
import { ApiService } from 'src/app/_services/api.service';

@Injectable({
  providedIn: 'root'
})
export class MyVisualizationService {

  constructor(private _apiService: ApiService) { }

  getVisualizationDetails(correlationId, modelName, isPrediction?) {
    const request = {
      'correlationId': correlationId,
      'modelName': modelName
    }
    if (isPrediction) { request['isPrediction'] = isPrediction };
    return this._apiService.get('GetVisualization', request);
    // http://localhost:62042/api/GetVisualization?correlationId=446df8dc-5f02-4304-8d9d-defbbd095722&modelName=Random%20Forest%20Classifier

  }
}
