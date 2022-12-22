import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, Subscription } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class LocalStorageService {

  private localData: {} = {};
  private localDataSubject: BehaviorSubject<{}>;
  correlationId = '';
  localData$: Observable<{}>;
  private correlationIdSubscription: Subscription;
  private count = 0;
  modelName: string;
  targetColumn = '';
  uniqueIdentifier = '';
  timeSeriesColumn = '';
  token = '';
  modelCategory = '';
  lessdataquality = '';

  // MarketPlace Code
  private localMPData: {} = {};
  private localMPDataSubject: BehaviorSubject<{}>;
  correlationIdMP = '';
  localMPData$: Observable<{}>;
  private correlationIdSubscriptionMP: Subscription;
  modelNameMP: string;
  targetColumnMP = '';
  uniqueIdentifierMP = '';
  timeSeriesColumnMP = '';
  tokenMP = '';
  modelCategoryMP = '';

  constructor() {
    const data = localStorage.getItem('localData');
    if (data) {
      this.localData = JSON.parse(data);
    } else {
      localStorage.setItem('localData', JSON.stringify(this.localData));
    }

    this.localDataSubject = new BehaviorSubject(this.localData);
    this.localData$ = this.localDataSubject.asObservable();
    // tslint:disable: no-shadowed-variable
    this.correlationIdSubscription =
      this.localData$.subscribe(data => {
        this.correlationId = data['correlationId'];
        this.modelName = data['modelName'];
        this.modelCategory = data['modelCategory'];
      });

    // for setting data of marketplace redirected users : start
    const mpData = sessionStorage.getItem('localMPData'); // localStorage.getItem('localMPData');
    if (mpData) {
      this.localMPData = JSON.parse(mpData);
    } else {
      // localStorage.setItem('localMPData', JSON.stringify(this.localMPData));
      sessionStorage.setItem('localMPData', JSON.stringify(this.localMPData));
    }

    this.localMPDataSubject = new BehaviorSubject(this.localMPData);
    this.localMPData$ = this.localMPDataSubject.asObservable();
    // tslint:disable: no-shadowed-variable
    this.correlationIdSubscriptionMP =
      this.localMPData$.subscribe(mpData => {
        this.correlationIdMP = mpData['correlationIdMP'];
        this.modelNameMP = mpData['modelNameMP'];
        this.modelCategoryMP = mpData['modelCategoryMP'];
      });

    // for setting data of marketplace redirected users : end	
  }

  setLocalStorageData(key, value) {
    this.localData[key] = value;
    this.localDataSubject.next(this.localData);
    localStorage.setItem('localData', JSON.stringify(this.localData));
  }


  getCorrelationId() {
    return this.correlationId;
  }

  getModelName() {
    return this.modelName;
  }

  getTargetColumn() {
    let data = localStorage.getItem('localData');
    data = JSON.parse(data);
    return data['targetcolumn'];
    // return this.targetColumn;
  }

  getUniqueIdentifier() {
    return this.uniqueIdentifier;
  }
  getTimeSeriesColumn() {
    return this.timeSeriesColumn;
  }

  getToken() {
    return this.token;
  }

  getModelCategory() {
    return this.modelCategory;
  }

  getFeatureNameDataQualityLow() {
    let data = localStorage.getItem('localData');
    data = JSON.parse(data);
    return data['lessdataquality'];
  }

  getClusteringFlag() {
    let data = localStorage.getItem('localData');
    data = JSON.parse(data);
    return data['clusteringFlag'];
  }

  getCascadedId() {
    let data = localStorage.getItem('localData');
    data = JSON.parse(data);
    return data['cascadedId'];
  }

  // for setting data of marketplace redirected users : start
  setMPLocalStorageData(key, value) {
    this.localMPData[key] = value;
    this.localMPDataSubject.next(this.localMPData);
    // localStorage.setItem('localMPData', JSON.stringify(this.localMPData));
    sessionStorage.setItem('localMPData', JSON.stringify(this.localMPData));
  }

  getMPModelName() {
    return this.modelNameMP;
  }

  getMPModelCategory() {
    return this.modelCategoryMP;
  }

  getMPCorrelationid() {
    return this.correlationIdMP;
  }
  // for setting data of marketplace redirected users : end
}
