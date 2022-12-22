import { Injectable } from '@angular/core';
import { ApiService } from './api.service';
import { tap } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class CompareModelsService {

  constructor(private api: ApiService) { }
  getCompareTestScenarios(correlationId, modelName) {
    return this.api.get('GetCompareTestScenarios', { correlationId: correlationId, modelName: modelName });
  }

}
