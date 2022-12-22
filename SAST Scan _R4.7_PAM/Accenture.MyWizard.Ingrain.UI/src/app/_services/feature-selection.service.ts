import { Injectable } from '@angular/core';
import { ApiService } from '../_services/api.service';
import { BehaviorSubject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class FeatureSelectionService {

  private applyFlagSubscription: BehaviorSubject<boolean> = new BehaviorSubject(false);
  public applyFlag$ = this.applyFlagSubscription.asObservable();


  constructor(private api: ApiService) {}

  getFeatures(correlationId, userId, pageInfo) {
    return this.api.get('GetFeatureSelection', { 'correlationId': correlationId, 'userId': userId, 'pageInfo': pageInfo });
  }

  postFeatures(correlationId, postCheckedFeature, postTrainingData, postKfoldValidation, postSwitchState, AllData_Flag, isCascadingEnabled) {
    const data = {
      'CorrelationId': correlationId,
      'FeatureImportance': postCheckedFeature,
      'Train_Test_Split': postTrainingData,
      'KFoldValidation': postKfoldValidation,
      'StratifiedSampling': postSwitchState,
      'AllData_Flag' :AllData_Flag,
      'IsCascadingButton' :isCascadingEnabled

    };
    return this.api.post('PostFeatureSelection', data);
  }

  fixFeatures(correlationId, fixFeatures ) {
    return this.api.get('RemoveFeatureSelectionAttributes',
    {'correlationId': correlationId, 'prescriptionColumns': Array.from(fixFeatures)});
  }

  setApplyFlag(applyFlag) {
    this.applyFlagSubscription.next(applyFlag);
  }
}
