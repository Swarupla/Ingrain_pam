import { Component, EventEmitter, OnInit, Output } from '@angular/core';
import { Router } from '@angular/router';
import { CookieService } from 'ngx-cookie-service';
import { AppUtilsService } from 'src/app/_services/app-utils.service';
import { CoreUtilsService } from 'src/app/_services/core-utils.service';
import { CustomRoutingService } from 'src/app/_services/custom-routing.service';
import { LocalStorageService } from 'src/app/_services/local-storage.service';
import { NotificationService } from 'src/app/_services/notification-service.service';
import { catchError, switchMap } from 'rxjs/operators';
import { empty, of, throwError } from 'rxjs';
import { AdModelEngineeringService } from 'src/app/_services/ad-model-engineering.service';
import { UsageTrackingService } from 'src/app/_services/usage-tracking.service';

@Component({
  selector: 'app-ad-model-engineering',
  templateUrl: './ad-model-engineering.component.html',
  styleUrls: ['./ad-model-engineering.component.scss']
})
export class AdModelEngineeringComponent implements OnInit {
  @Output() isNextBtnDisabled : EventEmitter<boolean> = new EventEmitter<boolean>();

  features: {};
  fixFeatureImportnaceofZero: Set<string> = new Set();
  postCheckedFeature = {};
  featurenames = [];
  importanceValue = {};
  featureForSplit: any;
  decimalPoint: any;
  correlationId: string;
  userId;
  pageInfo = 'DataPreprocessing';
  timerSubscripton: any;
  noData: string;
  featureSelectionSubscription: any;
  isLoading: boolean;
  statusForPrescription: boolean;
  readOnly;
  isNextDisabled: boolean;
  modelType: string;
  currentIndex = 1;
  breadcrumbIndex = 0;
  isTimeSeriesModelType: boolean;
  isFeatureSelectionSaved = true;
  postKfoldValidation: { KFoldValidation: { ApplyKFold: any; SelectedKFold: any }; };
  postTrainingData: { Train_Test_Split: { TrainingData: any; }; };
  isAllData_Flag: boolean = false;
  SelectedKFold: any;
  maxKfoldValidation: any;
  postSwitchState: { StratifiedSampling: string; };
  isCascadingEnabled = false;

  constructor(private fs: AdModelEngineeringService, private _appUtilsService: AppUtilsService,
    private ls: LocalStorageService, private cu: CoreUtilsService, private ns: NotificationService,
    private router: Router, private customRouter: CustomRoutingService, private cookieService: CookieService,
    private uts : UsageTrackingService
  ) {
    this.decimalPoint = (sessionStorage.getItem('decimalPoint') != null && sessionStorage.getItem('decimalPoint') != 'null') ? parseInt(sessionStorage.getItem('decimalPoint')) : 2;
  }

  ngOnInit(): void {
    if (this.router.url.includes('FeatureSelection')) {
      this.breadcrumbIndex = 0;
    }
    this.readOnly = sessionStorage.getItem('viewEditAccess');
    this.isLoading = true;
    this.statusForPrescription = true;
    this.correlationId = this.ls.getCorrelationId();
    this.uts.usageTracking('Anomaly Model Engineering', 'Feature Selection');
    this.getDataOfFeatureSelection();
  }

  isTrueForCheckbox(featurename) {
    if (this.features[featurename].Selection === 'True') {
      if (this.features[featurename].Value === 0) {
        this.fixFeatureImportnaceofZero.add(featurename);
      }
      return true;
    } else {
      return false;
    }
  }

  onCheckChanged(key, elementRef) {
    if (elementRef.checked === true) {
      this.features[key].Selection = 'True'
      this.postCheckedFeature[key] = {};
      this.postCheckedFeature[key] = {
        Selection: 'True'
      };
    } else if (elementRef.checked === false) {
      this.features[key].Selection = 'False'
      this.postCheckedFeature[key] = {};
      this.postCheckedFeature[key] = {
        Selection: 'False'
      };
    }
  }

  getFeatureImportance() {
    //  Bug raised by North American team 
    // Desc :- Clusters keep added in the feature selection page as the save button is clicked.
    // Fix :- empty the featurenames array
    this.featurenames = [];
    for (const i in this.features) {
      if (i !== this.featureForSplit) {
        if (i !== 'All_Text') {
          this.featurenames.push(i);
          this.importanceValue[i] = parseFloat(this.features[i].Value).toFixed(4);
          this.importanceValue[i] = parseFloat(this.features[i].Value).toFixed(this.decimalPoint);
        }
      }
    }
  }

  setAttributes(data) {

    if (this.cu.isNil(data.ProcessData)) {
      this.ns.error(`Error occurred: Due
        to some backend data process
         the relevant data could not be produced.
          Please try again while we troubleshoot the error`);
    } else {
      if (!this.cu.isNil(data.ModelType) && data.ModelType === 'TimeSeries') {
        this.isTimeSeriesModelType = true;
      } else {
        this.isTimeSeriesModelType = false;
      }
      this.isLoading = false;
      if (!this.cu.isNil(data.ProcessData)) {
        //this.processData = data.ProcessData;
      }
      if (!this.cu.isNil(data.ProcessData.FeatureImportance)) {
        this.features = data.ProcessData.FeatureImportance;
      }
      this.getFeatureImportance();
    }
  }

  getDataOfFeatureSelection() {
    this.userId = this._appUtilsService.getCookies().UserId;
    this.featureSelectionSubscription =
      this.fs.getFeaturesAD(this.correlationId, this.userId, this.pageInfo)
        .pipe(
          switchMap(
            data => {
              let msg = 'Error occurred: Due to some backend data process the relevant data could not be produced.';
              msg = msg + ' Please try again while we troubleshoot the error';
              if (this.IsJsonString(data)) {
                const parsedData = JSON.parse(data);
                if (parsedData.hasOwnProperty('message')) {
                  return this.fs.getFeaturesAD(this.correlationId, this.userId, this.pageInfo);
                } else if (parsedData.hasOwnProperty('useCaseDetails')) {
                  return of(parsedData);
                } else {
                  this.noData = 'Format from server is not Recognised';
                  this.ns.error(msg);
                  this.unsubscribe();
                  // tslint:disable-next-line: deprecation
                  return empty();
                }
              } else if (data.constructor === Object) {
                this.setAttributes(data);
                // tslint:disable-next-line: deprecation
                return empty();
              } else if (data.constructor === String) {
                this.noData = data.toString();
                this.ns.success(data);
                this.unsubscribe();
                // tslint:disable-next-line: deprecation
                return empty();

              } else {
                this.noData = 'Format from server is not Recognised';
                this.ns.error(msg);
                this.unsubscribe();
              }
            }
          ),
          catchError(error => {
            return throwError(error);
          })
        )
        .subscribe(data => { this.setAttributes(data); });

  }

  IsJsonString(str) {
    try {
      JSON.parse(str);
    } catch (e) {
      return false;
    }
    return true;
  }

  unsubscribe() {
    if (!this.cu.isNil(this.timerSubscripton)) {
      this.timerSubscripton.unsubscribe();
    }
    if (!this.cu.isNil(this.featureSelectionSubscription)) {
      this.featureSelectionSubscription.unsubscribe();
    }
    this.isLoading = false;
  }

  next() {
    this.modelType = this.cookieService.get('ModelTypeForInstaML');
    if (this.modelType === 'TimeSeries'
      && this.router.url.indexOf('/anomaly-detection/modelengineering/TeachAndTest/WhatIfAnalysis') > -1) {
      this.customRouter.nextUrl = 'anomaly-detection/modelengineering/CompareModels';
      this.cu.disableADTabs(this.currentIndex, 3);
    }
    
    if (!this.cu.isEmptyObject(this.postCheckedFeature)) {
      this.ns.warning('Changes found, Please save the changes and try again.');
    } 
    else if(this.isNextDisabled){
      this.ns.warning('Please click on Start Training and try again.');
    }else {
      this.cu.disableADTabs(this.currentIndex, this.breadcrumbIndex, null, true);
      this.customRouter.redirectToNext();
    }
  }

  previous() {
    this.modelType = this.cookieService.get('ModelTypeForInstaML');
    if (this.modelType === 'TimeSeries'
      && this.router.url.indexOf('anomaly-detection/modelengineering/CompareModels') > -1) {
      this.customRouter.previousUrl = '/anomaly-detection/modelengineering/TeachAndTest/WhatIfAnalysis';
    }
    if (this.customRouter.urlAfterRedirects === '/anomaly-detection/modelengineering/FeatureSelection' || this.cu.isNil(this.customRouter.urlAfterRedirects)) {
      this.customRouter.previousUrl = 'anomaly-detection/problemstatement/usecasedefinition?modelCategory='+this.ls.getModelCategory()+'&displayUploadandDataSourceBlock=false&modelName='+this.ls.getModelName();
      this.cu.disableADTabs(this.currentIndex, this.breadcrumbIndex, true, null);
    }
    this.customRouter.redirectToPrevious();
  }

  postSelectedFeatures(isSaveClicked?) {
    if (this.isTimeSeriesModelType == true && isSaveClicked == true) {
      this.ns.warning('No changes found to save, click on Start Training and continue');
    } else if (this.cu.isEmptyObject(this.postCheckedFeature)) {
      this.ns.warning('No Changes found.');
    } else {
      this._appUtilsService.loadingStarted();
      if (!this.readOnly) {
        this.fs.postFeaturesAD(this.correlationId, this.postCheckedFeature, this.postTrainingData,
          this.postKfoldValidation, this.postSwitchState, this.isAllData_Flag, this.isCascadingEnabled).subscribe(
            data => {
              this.postCheckedFeature = {};
              this._appUtilsService.loadingEnded();
              this.ns.success('Data Saved Successfully.');
              this.isFeatureSelectionSaved = true;
              if (isSaveClicked == true) {
                this.ngOnInit();
              } else {
                this.customRouter.redirectToNext();
              }
            }, error => {
              this._appUtilsService.loadingEnded();
              this.ns.error('Something went wrong while saving selected features.');
            });
      }
    }
  }

  changedValueForValidation(value) {
    this.SelectedKFold = value;
    this.postKfoldValidation = {
      KFoldValidation: {
        ApplyKFold: this.maxKfoldValidation + "",
        SelectedKFold: value
      }
    };
  }

  toDisableNext(value){
    this.isNextDisabled = value;
    this.emitDisableNext(value);
  }

  emitDisableNext(flag){
    this.isNextBtnDisabled.emit(flag);
  }

}
