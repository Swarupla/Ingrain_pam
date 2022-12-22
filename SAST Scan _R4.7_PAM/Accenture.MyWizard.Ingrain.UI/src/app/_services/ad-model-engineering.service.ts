import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { ApiService } from './api.service';

@Injectable({
  providedIn: 'root'
})
export class AdModelEngineeringService {
  private applyFlagSubscription: BehaviorSubject<boolean> = new BehaviorSubject(false);
  public applyFlag$ = this.applyFlagSubscription.asObservable();

  constructor(private api: ApiService) { }

  getFeaturesAD(correlationId, userId, pageInfo) {
    return this.api.get('GetFeatureSelectionAD', { 'correlationId': correlationId, 'userId': userId, 'pageInfo': pageInfo });
  }

  postFeaturesAD(correlationId, postCheckedFeature, postTrainingData, postKfoldValidation, postSwitchState, AllData_Flag, isCascadingEnabled) {
    const data = {
      'CorrelationId': correlationId,
      'FeatureImportance': postCheckedFeature,
      'Train_Test_Split': postTrainingData,
      'KFoldValidation': postKfoldValidation,
      'StratifiedSampling': postSwitchState,
      'AllData_Flag' :AllData_Flag,
      'IsCascadingButton' :isCascadingEnabled

    };
    return this.api.post('PostFeatureSelectionAD', data);
  }

  fixFeatures(correlationId, fixFeatures ) {
    return this.api.get('RemoveFeatureSelectionAttributes',
    {'correlationId': correlationId, 'prescriptionColumns': Array.from(fixFeatures)});
  }

  setApplyFlag(applyFlag) {
    this.applyFlagSubscription.next(applyFlag);
  }
}
